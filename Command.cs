// Command.cs - 原子命令系統
// 最低限度C風格：readonly struct + switch分派 + 簡單Queue
// 已整合Reaction系統事件觸發

using System;
using System.Collections.Generic;

namespace CombatCore
{
    // ✅ readonly struct - 防錯且高效
    public readonly struct AtomicCmd
    {
        public readonly CmdOp Op;
        public readonly byte SrcId, TargetId;
        public readonly ushort Value;      // ✅ 單一通用數值欄位
        
        public AtomicCmd(CmdOp op, byte srcId, byte targetId, ushort value = 0)
        {
            Op = op;
            SrcId = srcId;
            TargetId = targetId;
            Value = value;
        }
        
        // 常用命令建構輔助方法
        public static AtomicCmd Attack(byte srcId, byte targetId, ushort damage) 
            => new(CmdOp.ATTACK, srcId, targetId, damage);
            
        public static AtomicCmd Block(byte srcId, ushort amount) 
            => new(CmdOp.BLOCK, srcId, srcId, amount);
            
        public static AtomicCmd Charge(byte srcId, byte amount) 
            => new(CmdOp.CHARGE, srcId, srcId, amount);
            
        public static AtomicCmd Heal(byte srcId, byte targetId, ushort amount) 
            => new(CmdOp.HEAL, srcId, targetId, amount);
            
        public static AtomicCmd AddStatus(byte srcId, byte targetId, StatusFlags status, byte duration)
            => new(CmdOp.STATUS_ADD, srcId, targetId, (ushort)((byte)status << 8 | duration));
            
        public static AtomicCmd RemoveStatus(byte targetId, StatusFlags status)
            => new(CmdOp.STATUS_REMOVE, 0, targetId, (ushort)status);
            
        public static AtomicCmd TurnEndCleanup()
            => new(CmdOp.TURN_END_CLEANUP, 0, 0, 0);
    }
    
    // 命令執行結果
    public readonly struct CommandResult
    {
        public readonly bool Success;
        public readonly ushort Value;       // 實際造成的傷害/治療量等
        public readonly string Message;     // 除錯訊息
        
        public CommandResult(bool success, ushort value = 0, string message = "")
        {
            Success = success;
            Value = value;
            Message = message;
        }
        
        public static readonly CommandResult SUCCESS = new(true);
        public static readonly CommandResult FAILED = new(false);
    }
    
    // 命令系統 - 核心執行引擎
    public static class CommandSystem
    {
        // ✅ 簡單Queue - 避免複雜Ring Buffer
        private static readonly Queue<AtomicCmd> s_commands = new();
        private static readonly Queue<AtomicCmd> s_delayedCommands = new();  // 延遲執行的命令
        
        // 推入命令到佇列
        public static void PushCmd(in AtomicCmd cmd) => s_commands.Enqueue(cmd);    // ✅ in參數
        public static void PushDelayedCmd(in AtomicCmd cmd) => s_delayedCommands.Enqueue(cmd);
        
        // 批次推入命令
        public static void PushCommands(ReadOnlySpan<AtomicCmd> commands)           // ✅ ReadOnlySpan
        {
            foreach (ref readonly var cmd in commands)
            {
                s_commands.Enqueue(cmd);
            }
        }
        
        // ✅ Switch表達式分派 - 編譯時優化
        public static CommandResult ExecuteCmd(in AtomicCmd cmd) 
        {
            var result = cmd.Op switch   // ✅ switch + in
            {
                CmdOp.NOP => CommandResult.SUCCESS,
                CmdOp.ATTACK => HandleAttack(in cmd),
                CmdOp.BLOCK => HandleBlock(in cmd),
                CmdOp.CHARGE => HandleCharge(in cmd),
                CmdOp.HEAL => HandleHeal(in cmd),
                CmdOp.STATUS_ADD => HandleStatusAdd(in cmd),
                CmdOp.STATUS_REMOVE => HandleStatusRemove(in cmd),
                CmdOp.DEFLECT => HandleDeflect(in cmd),
                CmdOp.TURN_END_CLEANUP => HandleTurnEndCleanup(in cmd),
                CmdOp.ACTOR_DEATH => HandleActorDeath(in cmd),
                _ => new CommandResult(false, 0, $"未知命令: {cmd.Op}")
            };
            
            // ✅ 觸發命令執行後事件
            // SimpleEventSystem 會在具體的 Handle 方法中觸發
            
            return result;
        }
        
        // 執行佇列中的所有命令
        public static int ExecuteAll()
        {
            int executedCount = 0;

            // 反覆執行，直到所有命令佇列都為空
            while (s_commands.Count > 0 || s_delayedCommands.Count > 0)
            {
                // 執行主命令佇列
                while (s_commands.TryDequeue(out var cmd))
                {
                    var result = ExecuteCmd(in cmd);
                    executedCount++;

                    // 處理命令執行失敗的情況
                    if (!result.Success)
                    {
                        Console.WriteLine($"命令執行失敗: {result.Message}");
                    }
                }

                // 將延遲命令移至主佇列，下一輪繼續執行
                while (s_delayedCommands.TryDequeue(out var delayedCmd))
                {
                    s_commands.Enqueue(delayedCmd);
                }
            }

            Console.WriteLine($"總共執行 {executedCount} 個命令（包含延遲命令）");
            return executedCount;
        }
        
        // 清空所有命令佇列
        public static void Clear()
        {
            s_commands.Clear();
            s_delayedCommands.Clear();
        }
        
        // 取得佇列狀態
        public static int GetQueueCount() => s_commands.Count;
        public static int GetDelayedQueueCount() => s_delayedCommands.Count;
        
        // ==================== 命令處理函數 ====================
       private static CommandResult HandleAttack(in AtomicCmd cmd)
        {
            if (!ActorManager.IsAlive(cmd.SrcId) || !ActorManager.IsAlive(cmd.TargetId))
                return new CommandResult(false, 0, "攻擊者或目標已死亡");
                
            if (!ActorManager.CanAct(cmd.SrcId))
                return new CommandResult(false, 0, "攻擊者無法行動");

            ushort baseDamage = cmd.Value;
            
            // ✅ 純粹調用 Actor API：檢查並消耗 charge
            byte consumedCharge = ActorOperations.ConsumeCharge(cmd.SrcId); // 消耗全部 charge
            
            // ✅ 計算最終傷害
            ushort finalDamage = (ushort)(baseDamage + consumedCharge * CombatConstants.CHARGE_DAMAGE_BONUS);
            
            // ✅ 純粹調用 Actor API：造成傷害
            ushort actualDamage = ActorOperations.DealDamage(cmd.TargetId, finalDamage);
            
            // ✅ 觸發事件（不是狀態變更）
            SimpleEventSystem.OnActorDamaged(cmd.TargetId, cmd.SrcId, actualDamage);
            
            // ✅ 檢查死亡並推入後續命令
            if (!ActorManager.IsAlive(cmd.TargetId))
            {
                SimpleEventSystem.OnActorDeath(cmd.TargetId);
                PushDelayedCmd(new AtomicCmd(CmdOp.ACTOR_DEATH, 0, cmd.TargetId, 0));
            }
            
            string message = consumedCharge > 0 
                ? $"消耗{consumedCharge}蓄力，造成 {actualDamage} 點傷害" 
                : $"造成 {actualDamage} 點傷害";
            
            return new CommandResult(true, actualDamage, message);
        }
        private static CommandResult HandleBlock(in AtomicCmd cmd)
        {
            if (!ActorManager.IsAlive(cmd.SrcId))
                return new CommandResult(false, 0, "格擋者已死亡");
                
            if (!ActorManager.CanAct(cmd.SrcId))
                return new CommandResult(false, 0, "格擋者無法行動");

            ushort baseBlock = cmd.Value;
            
            // ✅ 純粹調用 Actor API：檢查並消耗 charge  
            byte consumedCharge = ActorOperations.ConsumeCharge(cmd.SrcId); // 消耗全部 charge
            
            // ✅ 計算最終護甲
            ushort finalBlock = (ushort)(baseBlock + consumedCharge * CombatConstants.CHARGE_DAMAGE_BONUS);
            
            // ✅ 純粹調用 Actor API：增加護甲
            ushort actualBlock = ActorOperations.AddBlock(cmd.SrcId, finalBlock);
            
            string message = consumedCharge > 0 
                ? $"消耗{consumedCharge}蓄力，獲得 {actualBlock} 點護甲" 
                : $"獲得 {actualBlock} 點護甲";
            
            return new CommandResult(true, actualBlock, message);
        }
        
        private static CommandResult HandleCharge(in AtomicCmd cmd)
        {
            if (!ActorManager.IsAlive(cmd.SrcId))
                return new CommandResult(false, 0, "蓄力者已死亡");
                
            if (!ActorManager.CanAct(cmd.SrcId))
                return new CommandResult(false, 0, "蓄力者無法行動");

            byte chargeAmount = (byte)Math.Min(cmd.Value, CombatConstants.MAX_CHARGE);
            
            // ✅ 純粹調用 Actor API：增加蓄力
            byte actualCharge = ActorOperations.AddCharge(cmd.SrcId, chargeAmount);
            
            return new CommandResult(true, actualCharge, $"獲得 {actualCharge} 點蓄力");
        }
        
        private static CommandResult HandleHeal(in AtomicCmd cmd)
        {
            if (!ActorManager.IsAlive(cmd.TargetId))
                return new CommandResult(false, 0, "治療目標已死亡");

            // ✅ 純粹調用 Actor API：治療
            ushort healAmount = ActorOperations.Heal(cmd.TargetId, cmd.Value);
            
            return new CommandResult(true, healAmount, $"治療 {healAmount} 點生命值");
        }
        
        private static CommandResult HandleStatusAdd(in AtomicCmd cmd)
        {
            if (!ActorManager.IsAlive(cmd.TargetId))
                return new CommandResult(false, 0, "狀態目標已死亡");

            // 從Value解包狀態和持續時間
            StatusFlags status = (StatusFlags)(cmd.Value >> 8);
            byte duration = (byte)(cmd.Value & 0xFF);
            
            // ✅ 純粹調用 Actor API：添加狀態
            ActorOperations.AddStatus(cmd.TargetId, status, duration);
            
            return new CommandResult(true, duration, $"添加狀態 {status}，持續 {duration} 回合");
        }
        
        private static CommandResult HandleStatusRemove(in AtomicCmd cmd)
        {
            if (!ActorManager.IsAlive(cmd.TargetId))
                return new CommandResult(false, 0, "狀態目標已死亡");

            StatusFlags status = (StatusFlags)cmd.Value;
            
            // ✅ 純粹調用 Actor API：移除狀態
            bool removed = ActorOperations.RemoveStatus(cmd.TargetId, status);
            
            return new CommandResult(removed, 0, removed ? $"移除狀態 {status}" : $"目標沒有狀態 {status}");
        }
        
        private static CommandResult HandleDeflect(in AtomicCmd cmd)
        {
            // 反彈傷害 - 將傷害返回給攻擊者
            if (!ActorManager.IsAlive(cmd.SrcId) || !ActorManager.IsAlive(cmd.TargetId))
                return new CommandResult(false, 0, "反彈參與者已死亡");
            
            ushort deflectedDamage = ActorOperations.DealDamage(cmd.SrcId, cmd.Value);

            SimpleEventSystem.OnActorDamaged(cmd.SrcId, cmd.TargetId, deflectedDamage);

            if (!ActorManager.IsAlive(cmd.SrcId))
            {
                PushDelayedCmd(new AtomicCmd(CmdOp.ACTOR_DEATH, 0, cmd.SrcId, 0));
            }
            
            return new CommandResult(true, deflectedDamage, $"反彈 {deflectedDamage} 點傷害");
        }
        
        private static CommandResult HandleTurnEndCleanup(in AtomicCmd cmd)
        {
            // 統一由 ActorManager 處理所有回合結束相關清理邏輯
            ActorManager.EndTurnCleanup();

            // 觸發回合結束事件
            SimpleEventSystem.OnTurnEnd();
            return new CommandResult(true, 0, "回合結束清理完成");
        }
        
        private static CommandResult HandleActorDeath(in AtomicCmd cmd)
        {
            if (ActorManager.IsAlive(cmd.TargetId))
            {
                ActorManager.RemoveActor(cmd.TargetId);
                return new CommandResult(true, 0, $"Actor {cmd.TargetId} 死亡");
            }
            return new CommandResult(false, 0, "目標已經死亡");
        }
    }
    
    // 常用命令建構輔助類
    public static class CommandBuilder
    {
        // 基礎操作命令
        public static AtomicCmd MakeAttackCmd(byte srcId, byte targetId, ushort baseDamage = 10)
            => AtomicCmd.Attack(srcId, targetId, baseDamage);
            
        public static AtomicCmd MakeBlockCmd(byte srcId, ushort blockAmount = 5)
            => AtomicCmd.Block(srcId, blockAmount);
            
        public static AtomicCmd MakeChargeCmd(byte srcId, byte chargeAmount = 1)
            => AtomicCmd.Charge(srcId, chargeAmount);
            
        public static AtomicCmd MakeHealCmd(byte srcId, byte targetId, ushort healAmount = 5)
            => AtomicCmd.Heal(srcId, targetId, healAmount);
            
        // 組合操作命令
        public static void BuildHeavyStrike(byte srcId, byte targetId, Span<AtomicCmd> buffer, out int count)
        {
            buffer[0] = MakeChargeCmd(srcId, 2);                    // 先蓄力
            buffer[1] = MakeAttackCmd(srcId, targetId, 15);         // 再重擊
            count = 2;
        }
        
        public static void BuildShieldBash(byte srcId, byte targetId, Span<AtomicCmd> buffer, out int count)
        {
            buffer[0] = MakeBlockCmd(srcId, 3);                     // 先格擋
            buffer[1] = MakeAttackCmd(srcId, targetId, 8);          // 再攻擊
            count = 2;
        }
        
        public static void BuildComboAttack(byte srcId, byte targetId, Span<AtomicCmd> buffer, out int count)
        {
            buffer[0] = MakeAttackCmd(srcId, targetId, 7);          // 第一擊
            buffer[1] = MakeAttackCmd(srcId, targetId, 7);          // 第二擊
            count = 2;
        }
    }
}