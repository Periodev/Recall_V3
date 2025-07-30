// MinimalReaction.cs - 極簡事件系統
// 提供基本的事件觸發和被動效果功能

using System;
using System.Collections.Generic;

namespace CombatCore
{
    // 簡單事件類型
    public enum SimpleEventType : byte
    {
        NONE = 0,
        ACTOR_DAMAGED = 1,
        ACTOR_DEATH = 2,
        CARD_PLAYED = 3,
        TURN_END = 4,
        TURN_START = 5
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
    }

    // 極簡事件系統
    public static class SimpleEventSystem
    {
        private static readonly List<SimpleEvent> s_pendingEvents = new();
        private static readonly List<SimpleEvent> s_processedEvents = new();
        private static bool s_initialized = false;

        // 初始化
        public static void Initialize()
        {
            s_pendingEvents.Clear();
            s_processedEvents.Clear();
            s_initialized = true;
        }

        // 重置
        public static void Reset()
        {
            s_pendingEvents.Clear();
            s_processedEvents.Clear();
            s_initialized = false;
        }

        // 觸發事件
        public static void TriggerEvent(SimpleEventType type, byte actorId, byte targetId, int value, string description = "")
        {
            if (!s_initialized) return;
            
            var evt = new SimpleEvent(type, actorId, targetId, value, description);
            s_pendingEvents.Add(evt);
        }

        // 處理所有待處理事件
        public static void ProcessAllEvents()
        {
            if (!s_initialized) return;

            foreach (var evt in s_pendingEvents)
            {
                ProcessEvent(evt);
                s_processedEvents.Add(evt);
            }
            
            s_pendingEvents.Clear();
        }

        // 處理單個事件
        private static void ProcessEvent(SimpleEvent evt)
        {
            switch (evt.Type)
            {
                case SimpleEventType.ACTOR_DAMAGED:
                    SimplePassiveEffects.OnActorDamaged(evt.ActorId, evt.TargetId, evt.Value);
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
            }
        }

        // 便捷方法
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

        // 獲取事件統計
        public static int GetPendingEventCount() => s_pendingEvents.Count;
        public static int GetProcessedEventCount() => s_processedEvents.Count;
    }

    // 簡單被動效果系統
    public static class SimplePassiveEffects
    {
        private static bool s_initialized = false;
        private static bool s_thornsEnabled = false;
        private static int s_thornsDamage = 5;
        private static bool s_healingEnabled = false;
        private static int s_healingAmount = 3;

        // 初始化
        public static void Initialize()
        {
            s_initialized = true;
            s_thornsEnabled = false;
            s_healingEnabled = false;
        }

        // 啟用反傷效果
        public static void EnableThorns(int damage = 5)
        {
            s_thornsEnabled = true;
            s_thornsDamage = damage;
        }

        // 啟用自癒效果
        public static void EnableHealing(int amount = 3)
        {
            s_healingEnabled = true;
            s_healingAmount = amount;
        }

        // 事件處理方法
        public static void OnActorDamaged(byte targetId, byte srcId, int damage)
        {
            if (!s_initialized) return;

            // 反傷效果
            if (s_thornsEnabled && IsPlayer(targetId) && srcId != 0)
            {
                var thornsCmd = CommandBuilder.MakeAttackCmd(targetId, srcId, (ushort)s_thornsDamage);
                CommandSystem.PushCmd(thornsCmd);
                Console.WriteLine($"反傷效果觸發！對敵人造成 {s_thornsDamage} 點傷害");
            }
        }

        public static void OnActorDeath(byte actorId)
        {
            if (!s_initialized) return;
            Console.WriteLine($"Actor {actorId} 死亡");
        }

        public static void OnCardPlayed(byte actorId, int cardType)
        {
            if (!s_initialized) return;
            Console.WriteLine($"Actor {actorId} 使用了卡片類型 {cardType}");
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
                    Console.WriteLine($"回合結束自癒效果觸發！恢復 {s_healingAmount} 點生命");
                }
            }
        }

        public static void OnTurnStart()
        {
            if (!s_initialized) return;
            Console.WriteLine("回合開始");
        }

        // 輔助方法
        private static byte GetPlayerId()
        {
            // 簡化的玩家ID獲取
            for (byte i = 0; i < 64; i++)
            {
                if (ActorManager.IsAlive(i))
                {
                    var actor = ActorManager.GetActor(i);
                    if (actor.Type == ActorType.PLAYER)
                        return i;
                }
            }
            return 255;
        }
        private static bool IsPlayer(byte actorId)
        {
            if (!ActorManager.IsAlive(actorId)) return false;
            return ActorManager.GetActor(actorId).Type == ActorType.PLAYER;
        }
    }


} 