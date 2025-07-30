// MinimalReaction.cs - 極簡事件系統（卡牌系統整合版）
// ✅ 修改：整合卡牌相關事件，確保與新架構一致
// ✅ 新增：敵人意圖事件、卡牌使用事件、回合轉換事件

using System;
using System.Collections.Generic;

namespace CombatCore
{
    // 簡單事件類型
    public enum SimpleEventType : byte
    {
        NONE = 0,
        
        // 戰鬥基礎事件
        ACTOR_DAMAGED = 1,
        ACTOR_DEATH = 2,
        TURN_END = 3,
        TURN_START = 4,
        
        // ✅ 新增：卡牌相關事件
        CARD_PLAYED = 5,              // 卡牌被使用
        HAND_SHUFFLED = 6,            // 手牌重洗
        HAND_EMPTY = 7,               // 手牌用盡
        
        // ✅ 新增：敵人意圖事件
        ENEMY_INTENT_DECLARED = 8,    // 敵人宣告意圖
        ENEMY_INTENT_EXECUTED = 9,    // 敵人執行意圖
        
        // ✅ 新增：Phase相關事件
        PHASE_CHANGED = 10,           // Phase轉換
        COMBAT_STARTED = 11,          // 戰鬥開始
        COMBAT_ENDED = 12,            // 戰鬥結束
        
        // ✅ 新增：HLA執行事件
        HLA_EXECUTED = 13,            // HLA執行完成
        HLA_FAILED = 14,              // HLA執行失敗
        
        // 預留擴展
        CUSTOM_EVENT_1 = 20,
        CUSTOM_EVENT_2 = 21,
    }

    // 簡單事件數據
    public struct SimpleEvent
    {
        public SimpleEventType Type;
        public byte ActorId;
        public byte TargetId;
        public int Value;
        public string Description;

        public SimpleEvent(SimpleEventType type, byte actorId, byte targetId, int value, string description = "")
        {
            Type = type;
            ActorId = actorId;
            TargetId = targetId;
            Value = value;
            Description = description;
        }
        
        // ✅ 新增：便捷建構方法
        public static SimpleEvent CardPlayed(byte actorId, int cardType, string cardName = "")
        {
            return new SimpleEvent(SimpleEventType.CARD_PLAYED, actorId, 0, cardType, cardName);
        }
        
        public static SimpleEvent EnemyIntentDeclared(byte enemyId, int hlaType, string intentDescription = "")
        {
            return new SimpleEvent(SimpleEventType.ENEMY_INTENT_DECLARED, enemyId, 0, hlaType, intentDescription);
        }
        
        public static SimpleEvent PhaseChanged(int fromPhase, int toPhase, string description = "")
        {
            return new SimpleEvent(SimpleEventType.PHASE_CHANGED, 0, 0, (fromPhase << 8) | toPhase, description);
        }
    }

    // 極簡事件系統
    public static class SimpleEventSystem
    {
        private static readonly List<SimpleEvent> s_pendingEvents = new();
        private static readonly List<SimpleEvent> s_processedEvents = new();
        private static readonly List<SimpleEvent> s_eventHistory = new(); // ✅ 新增：事件歷史記錄
        private static bool s_initialized = false;
        private static int s_maxHistorySize = 100; // ✅ 限制歷史記錄大小

        // 初始化
        public static void Initialize()
        {
            s_pendingEvents.Clear();
            s_processedEvents.Clear();
            s_eventHistory.Clear();
            s_initialized = true;
            Console.WriteLine("🎪 事件系統初始化完成");
        }

        // 重置
        public static void Reset()
        {
            s_pendingEvents.Clear();
            s_processedEvents.Clear();
            s_eventHistory.Clear();
            s_initialized = false;
            Console.WriteLine("🔄 事件系統已重置");
        }

        // 觸發事件
        public static void TriggerEvent(SimpleEventType type, byte actorId, byte targetId, int value, string description = "")
        {
            if (!s_initialized) 
            {
                Console.WriteLine("⚠️ 事件系統未初始化，自動初始化");
                Initialize();
            }
            
            var evt = new SimpleEvent(type, actorId, targetId, value, description);
            s_pendingEvents.Add(evt);
            
            // ✅ 即時處理重要事件（可選）
            if (IsImportantEvent(type))
            {
                Console.WriteLine($"🔥 重要事件觸發: {type} - {description}");
            }
        }
        
        // ✅ 新增：檢查是否為重要事件
        private static bool IsImportantEvent(SimpleEventType type)
        {
            return type switch
            {
                SimpleEventType.ACTOR_DEATH or 
                SimpleEventType.COMBAT_ENDED or 
                SimpleEventType.COMBAT_STARTED => true,
                _ => false
            };
        }

        // 處理所有待處理事件
        public static void ProcessAllEvents()
        {
            if (!s_initialized) return;

            foreach (var evt in s_pendingEvents)
            {
                ProcessEvent(evt);
                s_processedEvents.Add(evt);
                
                // ✅ 添加到歷史記錄
                AddToHistory(evt);
            }
            
            s_pendingEvents.Clear();
        }
        
        // ✅ 新增：添加到歷史記錄
        private static void AddToHistory(SimpleEvent evt)
        {
            s_eventHistory.Add(evt);
            
            // 限制歷史記錄大小
            if (s_eventHistory.Count > s_maxHistorySize)
            {
                s_eventHistory.RemoveAt(0);
            }
        }

        // 處理單個事件
        private static void ProcessEvent(SimpleEvent evt)
        {
            switch (evt.Type)
            {
                case SimpleEventType.ACTOR_DAMAGED:
                    SimplePassiveEffects.OnActorDamaged(evt.TargetId, evt.ActorId, evt.Value);
                    break;
                    
                case SimpleEventType.ACTOR_DEATH:
                    SimplePassiveEffects.OnActorDeath(evt.ActorId);
                    break;
                    
                case SimpleEventType.CARD_PLAYED:
                    SimplePassiveEffects.OnCardPlayed(evt.ActorId, evt.Value);
                    break;
                    
                case SimpleEventType.TURN_END:
                    SimplePassiveEffects.OnTurnEnd();
                    break;
                    
                case SimpleEventType.TURN_START:
                    SimplePassiveEffects.OnTurnStart();
                    break;
                    
                // ✅ 新增：卡牌事件處理
                case SimpleEventType.HAND_SHUFFLED:
                    SimplePassiveEffects.OnHandShuffled(evt.ActorId);
                    break;
                    
                case SimpleEventType.HAND_EMPTY:
                    SimplePassiveEffects.OnHandEmpty(evt.ActorId);
                    break;
                    
                // ✅ 新增：敵人意圖事件處理
                case SimpleEventType.ENEMY_INTENT_DECLARED:
                    SimplePassiveEffects.OnEnemyIntentDeclared(evt.ActorId, evt.Value);
                    break;
                    
                case SimpleEventType.ENEMY_INTENT_EXECUTED:
                    SimplePassiveEffects.OnEnemyIntentExecuted(evt.ActorId, evt.Value);
                    break;
                    
                // ✅ 新增：Phase事件處理
                case SimpleEventType.PHASE_CHANGED:
                    int fromPhase = evt.Value >> 8;
                    int toPhase = evt.Value & 0xFF;
                    SimplePassiveEffects.OnPhaseChanged(fromPhase, toPhase);
                    break;
                    
                case SimpleEventType.COMBAT_STARTED:
                    SimplePassiveEffects.OnCombatStarted();
                    break;
                    
                case SimpleEventType.COMBAT_ENDED:
                    SimplePassiveEffects.OnCombatEnded(evt.Value); // Value = 勝負結果
                    break;
                    
                // ✅ 新增：HLA事件處理
                case SimpleEventType.HLA_EXECUTED:
                    SimplePassiveEffects.OnHLAExecuted(evt.ActorId, evt.TargetId, evt.Value);
                    break;
                    
                case SimpleEventType.HLA_FAILED:
                    SimplePassiveEffects.OnHLAFailed(evt.ActorId, evt.Value);
                    break;
            }
        }

        // ✅ 便捷方法 - 基礎事件
        public static void OnActorDamaged(byte targetId, byte srcId, int damage)
        {
            TriggerEvent(SimpleEventType.ACTOR_DAMAGED, srcId, targetId, damage, $"受到 {damage} 點傷害");
        }

        public static void OnActorDeath(byte actorId)
        {
            TriggerEvent(SimpleEventType.ACTOR_DEATH, actorId, 0, 0, "死亡");
        }

        public static void OnCardPlayed(byte actorId, int cardType)
        {
            TriggerEvent(SimpleEventType.CARD_PLAYED, actorId, 0, cardType, "使用卡片");
        }

        public static void OnTurnEnd()
        {
            TriggerEvent(SimpleEventType.TURN_END, 0, 0, 0, "回合結束");
        }

        public static void OnTurnStart()
        {
            TriggerEvent(SimpleEventType.TURN_START, 0, 0, 0, "回合開始");
        }
        
        // ✅ 新增：卡牌相關便捷方法
        public static void OnHandShuffled(byte actorId, int cardCount = 0)
        {
            TriggerEvent(SimpleEventType.HAND_SHUFFLED, actorId, 0, cardCount, $"重洗手牌，抽到 {cardCount} 張");
        }
        
        public static void OnHandEmpty(byte actorId)
        {
            TriggerEvent(SimpleEventType.HAND_EMPTY, actorId, 0, 0, "手牌用盡");
        }
        
        // ✅ 新增：敵人意圖相關便捷方法
        public static void OnEnemyIntentDeclared(byte enemyId, HLA hla)
        {
            TriggerEvent(SimpleEventType.ENEMY_INTENT_DECLARED, enemyId, 0, (int)hla, $"宣告意圖: {hla}");
        }
        
        public static void OnEnemyIntentExecuted(byte enemyId, HLA hla)
        {
            TriggerEvent(SimpleEventType.ENEMY_INTENT_EXECUTED, enemyId, 0, (int)hla, $"執行意圖: {hla}");
        }
        
        // ✅ 新增：Phase相關便捷方法
        public static void OnPhaseChanged(PhaseId fromPhase, PhaseId toPhase)
        {
            TriggerEvent(SimpleEventType.PHASE_CHANGED, 0, 0, ((int)fromPhase << 8) | (int)toPhase, $"{fromPhase} → {toPhase}");
        }
        
        public static void OnCombatStarted()
        {
            TriggerEvent(SimpleEventType.COMBAT_STARTED, 0, 0, 0, "戰鬥開始");
        }
        
        public static void OnCombatEnded(string result)
        {
            int resultCode = result switch
            {
                "勝利" => 1,
                "敗北" => 2,
                _ => 0
            };
            TriggerEvent(SimpleEventType.COMBAT_ENDED, 0, 0, resultCode, $"戰鬥結束: {result}");
        }
        
        // ✅ 新增：HLA相關便捷方法
        public static void OnHLAExecuted(byte actorId, byte targetId, HLA hla)
        {
            TriggerEvent(SimpleEventType.HLA_EXECUTED, actorId, targetId, (int)hla, $"執行HLA: {hla}");
        }
        
        public static void OnHLAFailed(byte actorId, HLA hla, string reason = "")
        {
            TriggerEvent(SimpleEventType.HLA_FAILED, actorId, 0, (int)hla, $"HLA失敗: {hla} - {reason}");
        }

        // ✅ 新增：事件查詢功能
        public static int GetPendingEventCount() => s_pendingEvents.Count;
        public static int GetProcessedEventCount() => s_processedEvents.Count;
        public static int GetHistoryEventCount() => s_eventHistory.Count;
        
        // ✅ 新增：獲取最近的事件
        public static void GetRecentEvents(SimpleEventType eventType, Span<SimpleEvent> buffer, out int count)
        {
            count = 0;
            
            // 從歷史記錄中倒序查找
            for (int i = s_eventHistory.Count - 1; i >= 0 && count < buffer.Length; i--)
            {
                if (s_eventHistory[i].Type == eventType)
                {
                    buffer[count++] = s_eventHistory[i];
                }
            }
        }
        
        // ✅ 新增：事件統計
        public static void GetEventStats(Span<(SimpleEventType type, int count)> buffer, out int typeCount)
        {
            var eventCounts = new Dictionary<SimpleEventType, int>();
            
            foreach (var evt in s_eventHistory)
            {
                eventCounts[evt.Type] = eventCounts.GetValueOrDefault(evt.Type, 0) + 1;
            }
            
            typeCount = Math.Min(eventCounts.Count, buffer.Length);
            int index = 0;
            
            foreach (var kvp in eventCounts)
            {
                if (index >= typeCount) break;
                buffer[index++] = (kvp.Key, kvp.Value);
            }
        }
        
        // ✅ 新增：除錯功能
        public static void DebugPrintEventHistory()
        {
            Console.WriteLine("=== 事件歷史記錄 ===");
            
            if (s_eventHistory.Count == 0)
            {
                Console.WriteLine("沒有事件記錄");
                return;
            }
            
            // 顯示最近10個事件
            int startIndex = Math.Max(0, s_eventHistory.Count - 10);
            for (int i = startIndex; i < s_eventHistory.Count; i++)
            {
                var evt = s_eventHistory[i];
                Console.WriteLine($"  {i:D3}: {evt.Type} - {evt.Description}");
            }
            
            if (s_eventHistory.Count > 10)
            {
                Console.WriteLine($"（顯示最近10個，總計{s_eventHistory.Count}個事件）");
            }
        }
        
        public static void DebugPrintEventStats()
        {
            Console.WriteLine("=== 事件統計 ===");
            
            Span<(SimpleEventType type, int count)> stats = stackalloc (SimpleEventType, int)[20];
            GetEventStats(stats, out int typeCount);
            
            for (int i = 0; i < typeCount; i++)
            {
                var (type, count) = stats[i];
                Console.WriteLine($"  {type}: {count} 次");
            }
        }
    }

    // ✅ 擴展：簡單被動效果系統
    public static class SimplePassiveEffects
    {
        private static bool s_initialized = false;
        private static bool s_thornsEnabled = false;
        private static int s_thornsDamage = 5;
        private static bool s_healingEnabled = false;
        private static int s_healingAmount = 3;
        
        // ✅ 新增：卡牌相關被動效果開關
        private static bool s_cardCountBonusEnabled = false;
        private static bool s_phaseTransitionEffectsEnabled = false;

        // 初始化
        public static void Initialize()
        {
            s_initialized = true;
            s_thornsEnabled = false;
            s_healingEnabled = false;
            s_cardCountBonusEnabled = false;
            s_phaseTransitionEffectsEnabled = false;
            Console.WriteLine("⚡ 被動效果系統初始化完成");
        }

        // 啟用各種被動效果
        public static void EnableThorns(int damage = 5)
        {
            s_thornsEnabled = true;
            s_thornsDamage = damage;
            Console.WriteLine($"🌹 啟用反傷效果: {damage} 點傷害");
        }

        public static void EnableHealing(int amount = 3)
        {
            s_healingEnabled = true;
            s_healingAmount = amount;
            Console.WriteLine($"💚 啟用自癒效果: {amount} 點治療");
        }
        
        // ✅ 新增：卡牌相關被動效果
        public static void EnableCardCountBonus(bool enabled = true)
        {
            s_cardCountBonusEnabled = enabled;
            Console.WriteLine($"🎴 卡牌數量加成效果: {(enabled ? "啟用" : "停用")}");
        }
        
        public static void EnablePhaseTransitionEffects(bool enabled = true)
        {
            s_phaseTransitionEffectsEnabled = enabled;
            Console.WriteLine($"🔄 Phase轉換效果: {(enabled ? "啟用" : "停用")}");
        }

        // 基礎事件處理方法
        public static void OnActorDamaged(byte targetId, byte srcId, int damage)
        {
            if (!s_initialized) return;

            // 反傷效果
            if (s_thornsEnabled && IsPlayer(targetId) && srcId != 0)
            {
                var thornsCmd = CommandBuilder.MakeAttackCmd(targetId, srcId, (ushort)s_thornsDamage);
                CommandSystem.PushCmd(thornsCmd);
                Console.WriteLine($"🌹 反傷效果觸發！對敵人造成 {s_thornsDamage} 點傷害");
            }
        }

        public static void OnActorDeath(byte actorId)
        {
            if (!s_initialized) return;
            Console.WriteLine($"💀 Actor {actorId} 死亡");
        }

        public static void OnCardPlayed(byte actorId, int cardType)
        {
            if (!s_initialized) return;
            
            Console.WriteLine($"🎴 Actor {actorId} 使用了卡片類型 {cardType}");
            
            // ✅ 新增：卡牌數量加成效果
            if (s_cardCountBonusEnabled && IsPlayer(actorId))
            {
                int handSize = SimpleDeckManager.GetHandSize();
                if (handSize <= 1) // 手牌快用完時的獎勵
                {
                    var healCmd = CommandBuilder.MakeHealCmd(actorId, actorId, 2);
                    CommandSystem.PushCmd(healCmd);
                    Console.WriteLine("🎴 手牌稀少獎勵：恢復2點生命");
                }
            }
        }

        public static void OnTurnEnd()
        {
            if (!s_initialized) return;

            // 自癒效果
            if (s_healingEnabled)
            {
                var playerId = GetPlayerId();
                if (playerId != 255)
                {
                    var healCmd = CommandBuilder.MakeHealCmd(playerId, playerId, (ushort)s_healingAmount);
                    CommandSystem.PushCmd(healCmd);
                    Console.WriteLine($"💚 回合結束自癒效果觸發！恢復 {s_healingAmount} 點生命");
                }
            }
        }

        public static void OnTurnStart()
        {
            if (!s_initialized) return;
            Console.WriteLine("🌅 回合開始");
        }
        
        // ✅ 新增：卡牌相關事件處理
        public static void OnHandShuffled(byte actorId)
        {
            if (!s_initialized) return;
            Console.WriteLine($"🔄 Actor {actorId} 重洗了手牌");
            
            // 可以添加重洗獎勵邏輯
            if (IsPlayer(actorId) && s_cardCountBonusEnabled)
            {
                var chargeCmd = CommandBuilder.MakeChargeCmd(actorId, 1);
                CommandSystem.PushCmd(chargeCmd);
                Console.WriteLine("🔄 重洗獎勵：獲得1點蓄力");
            }
        }
        
        public static void OnHandEmpty(byte actorId)
        {
            if (!s_initialized) return;
            Console.WriteLine($"🃏 Actor {actorId} 手牌用盡");
        }
        
        // ✅ 新增：敵人意圖事件處理
        public static void OnEnemyIntentDeclared(byte enemyId, int hlaValue)
        {
            if (!s_initialized) return;
            HLA hla = (HLA)hlaValue;
            Console.WriteLine($"👁️ 敵人 {enemyId} 宣告意圖: {hla}");
        }
        
        public static void OnEnemyIntentExecuted(byte enemyId, int hlaValue)
        {
            if (!s_initialized) return;
            HLA hla = (HLA)hlaValue;
            Console.WriteLine($"⚔️ 敵人 {enemyId} 執行意圖: {hla}");
        }
        
        // ✅ 新增：Phase事件處理
        public static void OnPhaseChanged(int fromPhase, int toPhase)
        {
            if (!s_initialized) return;
            
            Console.WriteLine($"🔄 Phase轉換: {fromPhase} → {toPhase}");
            
            if (s_phaseTransitionEffectsEnabled)
            {
                // Phase轉換時的特殊效果
                if (toPhase == (int)PhaseId.PLAYER_PHASE)
                {
                    Console.WriteLine("🎯 進入玩家回合，獲得決心加成");
                    // 可以添加玩家回合開始的加成
                }
            }
        }
        
        public static void OnCombatStarted()
        {
            if (!s_initialized) return;
            Console.WriteLine("⚔️ 戰鬥開始！");
        }
        
        public static void OnCombatEnded(int resultCode)
        {
            if (!s_initialized) return;
            string result = resultCode switch
            {
                1 => "勝利",
                2 => "敗北",
                _ => "平局"
            };
            Console.WriteLine($"🏁 戰鬥結束: {result}");
        }
        
        // ✅ 新增：HLA事件處理
        public static void OnHLAExecuted(byte actorId, byte targetId, int hlaValue)
        {
            if (!s_initialized) return;
            HLA hla = (HLA)hlaValue;
            Console.WriteLine($"⚡ Actor {actorId} 成功執行HLA: {hla}");
        }
        
        public static void OnHLAFailed(byte actorId, int hlaValue)
        {
            if (!s_initialized) return;
            HLA hla = (HLA)hlaValue;
            Console.WriteLine($"❌ Actor {actorId} HLA執行失敗: {hla}");
        }

        // 輔助方法
        private static byte GetPlayerId()
        {
            Span<byte> playerBuffer = stackalloc byte[16];
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, playerBuffer);
            return playerCount > 0 ? playerBuffer[0] : (byte)255;
        }
        
        private static bool IsPlayer(byte actorId)
        {
            if (!ActorManager.IsAlive(actorId)) return false;
            return ActorManager.GetActor(actorId).Type == ActorType.PLAYER;
        }
    }
    
    // ✅ 新增：事件驅動的反應系統（為未來擴展準備）
    public static class ReactionEventDispatcher
    {
        // 為了與現有代碼相容而保留的接口
        public static void OnAfterCommand(in AtomicCmd cmd, in CommandResult result)
        {
            // 可以根據命令結果觸發更細粒度的事件
            if (result.Success && cmd.Op == CmdOp.ATTACK && result.Value > 0)
            {
                SimpleEventSystem.OnActorDamaged(cmd.TargetId, cmd.SrcId, result.Value);
            }
        }
        
        public static void OnTurnEnd(int turnNumber)
        {
            SimpleEventSystem.OnTurnEnd();
        }
        
        public static void OnTurnStart(int turnNumber)
        {
            SimpleEventSystem.OnTurnStart();
        }
        
        public static void OnPhaseChange(PhaseId fromPhase, PhaseId toPhase)
        {
            SimpleEventSystem.OnPhaseChanged(fromPhase, toPhase);
        }
        
        // ✅ 新增：重置方法
        public static void Reset()
        {
            // 保持接口一致性，實際重置在SimpleEventSystem中
            SimpleEventSystem.Reset();
        }
    }
}