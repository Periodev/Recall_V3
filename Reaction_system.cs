// Reaction.cs - 反應系統
// 事件驅動的被動效果系統，監聽戰鬥事件並觸發相應反應
// 支援立即效果和延後效果的分離機制

using System;
using System.Collections.Generic;

namespace CombatCore
{
    // 反應觸發條件類型
    public enum ReactionTrigger : byte
    {
        // Command相關觸發
        BEFORE_COMMAND = 1,     // 命令執行前
        AFTER_COMMAND = 2,      // 命令執行後
        
        // Actor相關觸發
        ACTOR_DAMAGED = 10,     // 受到傷害時
        ACTOR_HEALED = 11,      // 被治療時
        ACTOR_DEATH = 12,       // 死亡時
        ACTOR_BLOCK_GAINED = 13, // 獲得護甲時
        ACTOR_CHARGE_GAINED = 14, // 獲得蓄力時
        
        // Phase相關觸發
        TURN_START = 20,        // 回合開始
        TURN_END = 21,          // 回合結束
        PHASE_CHANGE = 22,      // Phase轉換
        
        // 狀態相關觸發
        STATUS_ADDED = 30,      // 狀態被添加
        STATUS_REMOVED = 31,    // 狀態被移除
        
        // 意圖相關觸發 ✅ 新增
        INTENT_DECLARED = 40,   // 意圖宣告時（立即效果）
        ENEMY_PHASE_START = 41, // 敵人階段開始（延後效果）
    }
    
    // 反應條件 - 定義觸發的具體條件
    public readonly struct ReactionCondition
    {
        public readonly ReactionTrigger Trigger;
        public readonly byte SourceFilter;     // 源Actor過濾（0=任意）
        public readonly byte TargetFilter;     // 目標Actor過濾（0=任意）
        public readonly ushort ValueFilter;    // 數值過濾（0=任意）
        public readonly CmdOp CommandFilter;   // 命令過濾（NOP=任意）
        public readonly ActorType ActorTypeFilter; // Actor類型過濾
        
        public ReactionCondition(ReactionTrigger trigger, byte sourceFilter = 0, 
            byte targetFilter = 0, ushort valueFilter = 0, CmdOp commandFilter = CmdOp.NOP,
            ActorType actorTypeFilter = ActorType.PLAYER)
        {
            Trigger = trigger;
            SourceFilter = sourceFilter;
            TargetFilter = targetFilter;
            ValueFilter = valueFilter;
            CommandFilter = commandFilter;
            ActorTypeFilter = actorTypeFilter;
        }
        
        // 檢查條件是否匹配
        public bool Matches(in ReactionEvent eventData)
        {
            if (Trigger != eventData.Trigger) return false;
            if (SourceFilter != 0 && SourceFilter != eventData.SourceId) return false;
            if (TargetFilter != 0 && TargetFilter != eventData.TargetId) return false;
            if (CommandFilter != CmdOp.NOP && CommandFilter != eventData.Command) return false;
            if (ValueFilter != 0 && ValueFilter > eventData.Value) return false; // 最小值條件
            
            return true;
        }
    }
    
    // 反應事件數據
    public readonly struct ReactionEvent
    {
        public readonly ReactionTrigger Trigger;
        public readonly byte SourceId;
        public readonly byte TargetId;
        public readonly ushort Value;
        public readonly CmdOp Command;
        public readonly PhaseId Phase;
        
        public ReactionEvent(ReactionTrigger trigger, byte sourceId = 0, byte targetId = 0, 
            ushort value = 0, CmdOp command = CmdOp.NOP, PhaseId phase = PhaseId.ENEMY_INTENT)
        {
            Trigger = trigger;
            SourceId = sourceId;
            TargetId = targetId;
            Value = value;
            Command = command;
            Phase = phase;
        }
    }
    
    // 反應效果類型
    public enum ReactionEffectType : byte
    {
        // 直接數值效果
        DEAL_DAMAGE = 1,        // 造成傷害
        HEAL = 2,               // 治療
        ADD_BLOCK = 3,          // 增加護甲
        ADD_CHARGE = 4,         // 增加蓄力
        
        // 狀態效果
        ADD_STATUS = 10,        // 添加狀態
        REMOVE_STATUS = 11,     // 移除狀態
        
        // 修正效果
        MODIFY_DAMAGE = 20,     // 修改傷害值
        MODIFY_HEAL = 21,       // 修改治療值
        MODIFY_BLOCK = 22,      // 修改護甲值
        
        // 命令效果
        PUSH_COMMAND = 30,      // 推入額外命令
        CANCEL_COMMAND = 31,    // 取消當前命令
        
        // 特殊效果
        REFLECT_DAMAGE = 40,    // 反射傷害
        ABSORB_DAMAGE = 41,     // 吸收傷害
        
        // HLA執行效果 ✅ 新增
        EXECUTE_ATTACK = 50,    // 執行攻擊
        EXECUTE_HLA = 51,       // 執行完整HLA
    }
    
    // 反應效果 - 觸發後執行的效果
    public readonly struct ReactionEffect
    {
        public readonly ReactionEffectType Type;
        public readonly byte TargetId;         // 效果目標（0=事件源，255=事件目標）
        public readonly ushort Value;
        public readonly StatusFlags Status;    // 用於狀態效果
        public readonly CmdOp Command;         // 用於命令效果
        
        public ReactionEffect(ReactionEffectType type, byte targetId, ushort value, 
            StatusFlags status = StatusFlags.NONE, CmdOp command = CmdOp.NOP)
        {
            Type = type;
            TargetId = targetId;
            Value = value;
            Status = status;
            Command = command;
        }
    }
    
    // 反應規則 - 完整的條件->效果映射
    public readonly struct ReactionRule
    {
        public readonly byte Id;
        public readonly ReactionCondition Condition;
        public readonly ReactionEffect Effect;
        public readonly string Name;
        public readonly bool OneTime;          // 是否只觸發一次
        
        public ReactionRule(byte id, ReactionCondition condition, ReactionEffect effect, 
            string name, bool oneTime = false)
        {
            Id = id;
            Condition = condition;
            Effect = effect;
            Name = name;
            OneTime = oneTime;
        }
    }
    
    // 反應系統管理器
    public static class ReactionSystem
    {
        // ✅ 簡單靜態存儲
        private static readonly List<ReactionRule> s_activeRules = new();
        private static readonly HashSet<byte> s_triggeredOneTimeRules = new();
        private static readonly Queue<ReactionEvent> s_eventQueue = new();
        
        // 註冊反應規則
        public static void RegisterRule(ReactionRule rule)
        {
            s_activeRules.Add(rule);
        }
        
        // 移除反應規則
        public static bool UnregisterRule(byte ruleId)
        {
            for (int i = s_activeRules.Count - 1; i >= 0; i--)
            {
                if (s_activeRules[i].Id == ruleId)
                {
                    s_activeRules.RemoveAt(i);
                    s_triggeredOneTimeRules.Remove(ruleId);
                    return true;
                }
            }
            return false;
        }
        
        // 觸發事件 - 核心事件處理函數
        public static void TriggerEvent(in ReactionEvent eventData)
        {
            // 檢查所有活躍規則
            foreach (ref readonly var rule in s_activeRules.AsSpan())
            {
                // 檢查一次性規則是否已觸發
                if (rule.OneTime && s_triggeredOneTimeRules.Contains(rule.Id))
                    continue;
                
                // 檢查條件是否匹配
                if (rule.Condition.Matches(in eventData))
                {
                    // 執行效果
                    ExecuteEffect(in rule.Effect, in eventData);
                    
                    // 標記一次性規則
                    if (rule.OneTime)
                    {
                        s_triggeredOneTimeRules.Add(rule.Id);
                    }
                }
            }
        }
        
        // 執行反應效果
        private static void ExecuteEffect(in ReactionEffect effect, in ReactionEvent eventData)
        {
            // 確定實際目標ID
            byte actualTargetId = effect.TargetId switch
            {
                0 => eventData.SourceId,        // 0 = 事件源
                255 => eventData.TargetId,      // 255 = 事件目標
                _ => effect.TargetId            // 其他 = 指定ID
            };
            
            // ✅ Switch表達式分派
            switch (effect.Type)
            {
                case ReactionEffectType.DEAL_DAMAGE:
                    ActorOperations.DealDamage(actualTargetId, effect.Value);
                    break;
                    
                case ReactionEffectType.HEAL:
                    ActorOperations.Heal(actualTargetId, effect.Value);
                    break;
                    
                case ReactionEffectType.ADD_BLOCK:
                    ActorOperations.AddBlock(actualTargetId, effect.Value);
                    break;
                    
                case ReactionEffectType.ADD_CHARGE:
                    ActorOperations.AddCharge(actualTargetId, (byte)effect.Value);
                    break;
                    
                case ReactionEffectType.ADD_STATUS:
                    ActorOperations.AddStatus(actualTargetId, effect.Status, (byte)effect.Value);
                    break;
                    
                case ReactionEffectType.REMOVE_STATUS:
                    ActorOperations.RemoveStatus(actualTargetId, effect.Status);
                    break;
                    
                case ReactionEffectType.PUSH_COMMAND:
                    var cmd = new AtomicCmd(effect.Command, eventData.SourceId, actualTargetId, effect.Value);
                    CommandSystem.PushCmd(cmd);
                    break;
                    
                case ReactionEffectType.REFLECT_DAMAGE:
                    if (eventData.Command == CmdOp.ATTACK)
                    {
                        ActorOperations.DealDamage(eventData.SourceId, effect.Value);
                    }
                    break;
                    
                case ReactionEffectType.EXECUTE_ATTACK:
                    var attackCmd = CommandBuilder.MakeAttackCmd(eventData.SourceId, actualTargetId, effect.Value);
                    CommandSystem.PushCmd(attackCmd);
                    break;
                    
                case ReactionEffectType.EXECUTE_HLA:
                    // 從Value解包HLA和目標ID
                    HLA hla = (HLA)(effect.Value >> 8);
                    byte hlaTarget = (byte)(effect.Value & 0xFF);
                    
                    // 執行完整HLA（這裡應該用簡化版本，只執行攻擊部分）
                    ProcessDelayedHLA(eventData.SourceId, hlaTarget != 0 ? hlaTarget : actualTargetId, hla);
                    break;
                    
                // 其他效果類型...
            }
        }
        
        // 處理延後的HLA（只執行攻擊部分）
        private static void ProcessDelayedHLA(byte srcId, byte targetId, HLA hla)
        {
            // 只處理HLA中的攻擊部分，防禦部分已在Intent Phase處理
            switch (hla)
            {
                case HLA.BASIC_ATTACK:
                    CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(srcId, targetId, 10));
                    break;
                    
                case HLA.HEAVY_STRIKE:
                    // 只執行攻擊部分，蓄力已在Intent Phase給予
                    CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(srcId, targetId, 15));
                    break;
                    
                case HLA.SHIELD_BASH:
                    // 只執行攻擊部分，護甲已在Intent Phase給予
                    CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(srcId, targetId, 8));
                    break;
                    
                case HLA.COMBO_ATTACK:
                    CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(srcId, targetId, 7));
                    CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(srcId, targetId, 7));
                    break;
                    
                case HLA.ENEMY_AGGRESSIVE:
                    // 只執行攻擊部分，蓄力已在Intent Phase給予
                    CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(srcId, targetId, 12));
                    CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(srcId, targetId, 8));
                    break;
                    
                case HLA.ENEMY_BERSERKER:
                    CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(srcId, targetId, 6));
                    CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(srcId, targetId, 6));
                    CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(srcId, targetId, 6));
                    break;
                    
                // 純防禦型HLA無攻擊部分
                case HLA.BASIC_BLOCK:
                case HLA.BASIC_CHARGE:
                case HLA.CHARGED_BLOCK:
                case HLA.POWER_CHARGE:
                case HLA.ENEMY_DEFENSIVE:
                case HLA.ENEMY_TURTLE:
                    // 這些HLA沒有延後的攻擊效果
                    break;
            }
        }
        }
        
        // 清理系統
        public static void Reset()
        {
            s_activeRules.Clear();
            s_triggeredOneTimeRules.Clear();
            s_eventQueue.Clear();
        }
        
        // 除錯資訊
        public static void DebugPrintRules()
        {
            Console.WriteLine($"活躍反應規則數量: {s_activeRules.Count}");
            foreach (var rule in s_activeRules)
            {
                Console.WriteLine($"  規則 {rule.Id}: {rule.Name} (觸發條件: {rule.Condition.Trigger})");
            }
        }
        
        public static int GetActiveRuleCount() => s_activeRules.Count;
    }
    
    // 反應系統事件發佈器 - 與Command系統整合
    public static class ReactionEventDispatcher
    {
        // Command執行前事件
        public static void OnBeforeCommand(in AtomicCmd cmd)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.BEFORE_COMMAND,
                cmd.SrcId,
                cmd.TargetId,
                cmd.Value,
                cmd.Op
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // Command執行後事件
        public static void OnAfterCommand(in AtomicCmd cmd, in CommandResult result)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.AFTER_COMMAND,
                cmd.SrcId,
                cmd.TargetId,
                result.Value, // 使用實際結果值
                cmd.Op
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // Actor受傷事件
        public static void OnActorDamaged(byte targetId, byte sourceId, ushort damage)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.ACTOR_DAMAGED,
                sourceId,
                targetId,
                damage
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // Actor治療事件
        public static void OnActorHealed(byte targetId, byte sourceId, ushort healAmount)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.ACTOR_HEALED,
                sourceId,
                targetId,
                healAmount
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // Actor死亡事件
        public static void OnActorDeath(byte actorId)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.ACTOR_DEATH,
                0,
                actorId,
                0
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // 護甲獲得事件
        public static void OnBlockGained(byte actorId, ushort blockAmount)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.ACTOR_BLOCK_GAINED,
                0,
                actorId,
                blockAmount
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // 蓄力獲得事件
        public static void OnChargeGained(byte actorId, byte chargeAmount)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.ACTOR_CHARGE_GAINED,
                0,
                actorId,
                chargeAmount
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // 回合開始事件
        public static void OnTurnStart(int turnNumber)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.TURN_START,
                0,
                0,
                (ushort)turnNumber
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // 回合結束事件
        public static void OnTurnEnd(int turnNumber)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.TURN_END,
                0,
                0,
                (ushort)turnNumber
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // Phase轉換事件
        public static void OnPhaseChange(PhaseId fromPhase, PhaseId toPhase)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.PHASE_CHANGE,
                (byte)fromPhase,
                (byte)toPhase,
                0,
                CmdOp.NOP,
                toPhase
            );
            ReactionSystem.TriggerEvent(in eventData);
            
            // 特別處理敵人階段開始
            if (toPhase == PhaseId.ENEMY_PHASE)
            {
                OnEnemyPhaseStart();
            }
        }
        
        // 意圖宣告事件 ✅ 新增
        public static void OnIntentDeclared(byte actorId, HLA hla, byte targetId = 0)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.INTENT_DECLARED,
                actorId,
                targetId,
                (ushort)hla
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // 敵人階段開始事件 ✅ 新增
        public static void OnEnemyPhaseStart()
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.ENEMY_PHASE_START,
                0,
                0,
                0
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
    }
    
    // 預定義的常用反應規則工廠
    public static class CommonReactions
    {
        private static byte s_nextRuleId = 1;
        
        // 反擊 - 受到攻擊時反擊
        public static ReactionRule CreateCounterAttack(byte actorId, ushort counterDamage)
        {
            var condition = new ReactionCondition(
                ReactionTrigger.ACTOR_DAMAGED,
                targetFilter: actorId,
                commandFilter: CmdOp.ATTACK
            );
            
            var effect = new ReactionEffect(
                ReactionEffectType.DEAL_DAMAGE,
                0, // 0 = 攻擊事件源
                counterDamage
            );
            
            return new ReactionRule(s_nextRuleId++, condition, effect, $"反擊({counterDamage})");
        }
        
        // 自癒 - 回合結束時治療
        public static ReactionRule CreateSelfHeal(byte actorId, ushort healAmount)
        {
            var condition = new ReactionCondition(
                ReactionTrigger.TURN_END,
                targetFilter: actorId
            );
            
            var effect = new ReactionEffect(
                ReactionEffectType.HEAL,
                actorId,
                healAmount
            );
            
            return new ReactionRule(s_nextRuleId++, condition, effect, $"自癒({healAmount})");
        }
        
        // 護甲再生 - 回合開始時獲得護甲
        public static ReactionRule CreateBlockRegen(byte actorId, ushort blockAmount)
        {
            var condition = new ReactionCondition(
                ReactionTrigger.TURN_START,
                targetFilter: actorId
            );
            
            var effect = new ReactionEffect(
                ReactionEffectType.ADD_BLOCK,
                actorId,
                blockAmount
            );
            
            return new ReactionRule(s_nextRuleId++, condition, effect, $"護甲再生({blockAmount})");
        }
        
        // 復仇 - 血量低於50%時攻擊力提升
        public static ReactionRule CreateRevenge(byte actorId, byte chargeBonus)
        {
            var condition = new ReactionCondition(
                ReactionTrigger.ACTOR_DAMAGED,
                targetFilter: actorId
            );
            
            var effect = new ReactionEffect(
                ReactionEffectType.ADD_CHARGE,
                actorId,
                chargeBonus
            );
            
            return new ReactionRule(s_nextRuleId++, condition, effect, $"復仇(+{chargeBonus}蓄力)", true);
        }
        
        // 重置規則ID計數器
        public static void ResetRuleIdCounter() => s_nextRuleId = 1;
    }
}