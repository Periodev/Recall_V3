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
        
        // 意圖相關觸發
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
        
        // HLA執行效果
        EXECUTE_ATTACK = 50,    // 執行攻擊
        EXECUTE_HLA = 51,       // 執行完整HLA
    }
    
    // 反應效果
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
    
    // 反應規則
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
    
    // 反應系統核心
    public static class ReactionSystem
    {
        // 全域狀態
        private static readonly List<ReactionRule> s_activeRules = new();
        private static readonly HashSet<byte> s_triggeredOneTimeRules = new();
        private static readonly Queue<ReactionEvent> s_eventQueue = new();
        
        // 註冊反應規則
        public static void RegisterRule(ReactionRule rule)
        {
            s_activeRules.Add(rule);
        }
        
        // 取消註冊反應規則
        public static bool UnregisterRule(byte ruleId)
        {
            for (int i = 0; i < s_activeRules.Count; i++)
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
        
        // 觸發事件
        public static void TriggerEvent(in ReactionEvent eventData)
        {
            s_eventQueue.Enqueue(eventData);
            
            // 立即處理事件
            ProcessEventQueue();
        }
        
        // 處理事件佇列
        private static void ProcessEventQueue()
        {
            while (s_eventQueue.Count > 0)
            {
                var eventData = s_eventQueue.Dequeue();
                
                // 檢查所有規則
                for (int i = 0; i < s_activeRules.Count; i++)
                {
                    var rule = s_activeRules[i];
                    
                    // 跳過已觸發的一次性規則
                    if (rule.OneTime && s_triggeredOneTimeRules.Contains(rule.Id))
                        continue;
                    
                    // 檢查條件匹配
                    if (rule.Condition.Matches(eventData))
                    {
                        // 執行效果
                        ExecuteEffect(rule.Effect, eventData);
                        
                        // 標記一次性規則為已觸發
                        if (rule.OneTime)
                            s_triggeredOneTimeRules.Add(rule.Id);
                    }
                }
            }
        }
        
        // 執行效果
        private static void ExecuteEffect(in ReactionEffect effect, in ReactionEvent eventData)
        {
            byte targetId = effect.TargetId == 0 ? eventData.SourceId : 
                           effect.TargetId == 255 ? eventData.TargetId : effect.TargetId;
            
            switch (effect.Type)
            {
                case ReactionEffectType.DEAL_DAMAGE:
                    ActorOperations.DealDamage(targetId, effect.Value);
                    break;
                    
                case ReactionEffectType.HEAL:
                    ActorOperations.Heal(targetId, effect.Value);
                    break;
                    
                case ReactionEffectType.ADD_BLOCK:
                    ActorOperations.AddBlock(targetId, effect.Value);
                    break;
                    
                case ReactionEffectType.ADD_CHARGE:
                    ActorOperations.AddCharge(targetId, (byte)effect.Value);
                    break;
                    
                case ReactionEffectType.ADD_STATUS:
                    ActorOperations.AddStatus(targetId, effect.Status, 1);
                    break;
                    
                case ReactionEffectType.REMOVE_STATUS:
                    ActorOperations.RemoveStatus(targetId, effect.Status);
                    break;
                    
                case ReactionEffectType.PUSH_COMMAND:
                    var cmd = AtomicCmd.Attack(targetId, eventData.TargetId, effect.Value);
                    CommandSystem.PushCmd(cmd);
                    break;
                    
                case ReactionEffectType.EXECUTE_ATTACK:
                    ProcessDelayedHLA(targetId, eventData.TargetId, HLA.BASIC_ATTACK);
                    break;
                    
                case ReactionEffectType.EXECUTE_HLA:
                    ProcessDelayedHLA(targetId, eventData.TargetId, (HLA)effect.Value);
                    break;
            }
        }
        
        // 處理延後的HLA執行
        private static void ProcessDelayedHLA(byte srcId, byte targetId, HLA hla)
        {
            // 檢查Actor是否存活
            if (!ActorManager.IsAlive(srcId) || !ActorManager.IsAlive(targetId))
                return;
            
            // 直接處理HLA
            HLASystem.ProcessHLA(srcId, targetId, hla);
        }
        
        // 重置系統
        public static void Reset()
        {
            s_activeRules.Clear();
            s_triggeredOneTimeRules.Clear();
            s_eventQueue.Clear();
        }
        
        // 調試輸出
        public static void DebugPrintRules()
        {
            Console.WriteLine($"活躍反應規則數量: {s_activeRules.Count}");
            foreach (var rule in s_activeRules)
            {
                Console.WriteLine($"規則 {rule.Id}: {rule.Name}");
            }
        }
        
        // 獲取活躍規則數量
        public static int GetActiveRuleCount() => s_activeRules.Count;
    }
    
    // 事件分發器 - 提供統一的介面來觸發各種事件
    public static class ReactionEventDispatcher
    {
        // 命令執行前事件
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
        
        // 命令執行後事件
        public static void OnAfterCommand(in AtomicCmd cmd, in CommandResult result)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.AFTER_COMMAND,
                cmd.SrcId,
                cmd.TargetId,
                result.Value,
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
                actorId,
                actorId,
                0
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // 獲得護甲事件
        public static void OnBlockGained(byte actorId, ushort blockAmount)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.ACTOR_BLOCK_GAINED,
                actorId,
                actorId,
                blockAmount
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // 獲得蓄力事件
        public static void OnChargeGained(byte actorId, byte chargeAmount)
        {
            var eventData = new ReactionEvent(
                ReactionTrigger.ACTOR_CHARGE_GAINED,
                actorId,
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
                0,
                0,
                0,
                CmdOp.NOP,
                toPhase
            );
            ReactionSystem.TriggerEvent(in eventData);
        }
        
        // 意圖宣告事件
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
        
        // 敵人階段開始事件
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