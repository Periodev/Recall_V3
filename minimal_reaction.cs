// MinimalReaction.cs - 極簡版 Reaction 系統
// 只保留核心的事件觸發機制，移除複雜的規則系統

using System;
using System.Collections.Generic;

namespace CombatCore
{
    // 簡化的事件類型 - 只保留必要的
    public enum SimpleEventType : byte
    {
        ACTOR_DAMAGED = 1,      // 角色受傷
        ACTOR_DEATH = 2,        // 角色死亡
        TURN_END = 3,           // 回合結束
        CARD_PLAYED = 4,        // 卡牌使用
    }
    
    // 簡化的事件數據
    public readonly struct SimpleEvent
    {
        public readonly SimpleEventType Type;
        public readonly byte ActorId;
        public readonly byte SourceId;
        public readonly ushort Value;
        
        public SimpleEvent(SimpleEventType type, byte actorId, byte sourceId = 0, ushort value = 0)
        {
            Type = type;
            ActorId = actorId;
            SourceId = sourceId;
            Value = value;
        }
    }
    
    // 極簡事件系統 - 只做必要的事件通知
    public static class SimpleEventSystem
    {
        // 只用於特殊效果，不用於常規遊戲邏輯
        public static event Action<SimpleEvent> OnEvent;
        
        // 觸發事件
        public static void TriggerEvent(SimpleEvent eventData)
        {
            OnEvent?.Invoke(eventData);
        }
        
        // 便利方法
        public static void OnActorDamaged(byte targetId, byte sourceId, ushort damage)
        {
            TriggerEvent(new SimpleEvent(SimpleEventType.ACTOR_DAMAGED, targetId, sourceId, damage));
        }
        
        public static void OnActorDeath(byte actorId)
        {
            TriggerEvent(new SimpleEvent(SimpleEventType.ACTOR_DEATH, actorId));
        }
        
        public static void OnTurnEnd()
        {
            TriggerEvent(new SimpleEvent(SimpleEventType.TURN_END, 0));
        }
        
        public static void OnCardPlayed(byte playerId, BasicAction action)
        {
            TriggerEvent(new SimpleEvent(SimpleEventType.CARD_PLAYED, playerId, 0, (ushort)action));
        }
        
        // 重置
        public static void Reset()
        {
            OnEvent = null;
        }
    }
    
    // 使用範例：簡單的被動效果
    public static class SimplePassiveEffects
    {
        public static void Initialize()
        {
            SimpleEventSystem.OnEvent += HandleEvent;
        }
        
        private static void HandleEvent(SimpleEvent eventData)
        {
            switch (eventData.Type)
            {
                case SimpleEventType.ACTOR_DAMAGED:
                    // 範例：反傷效果
                    HandleCounterAttack(eventData);
                    break;
                    
                case SimpleEventType.TURN_END:
                    // 範例：回合結束治療
                    HandleTurnEndHealing();
                    break;
                    
                case SimpleEventType.CARD_PLAYED:
                    // 範例：使用卡牌時的獎勵
                    HandleCardPlayBonus(eventData);
                    break;
            }
        }
        
        private static void HandleCounterAttack(SimpleEvent eventData)
        {
            // 簡單範例：玩家受傷時，對攻擊者造成1點反傷
            if (ActorManager.GetType(eventData.ActorId) == ActorType.PLAYER && 
                eventData.SourceId != 0 && ActorManager.IsAlive(eventData.SourceId))
            {
                var counterCmd = CommandBuilder.MakeAttackCmd(eventData.ActorId, eventData.SourceId, 1);
                CommandSystem.PushCmd(counterCmd);
            }
        }
        
        private static void HandleTurnEndHealing()
        {
            // 簡單範例：回合結束時玩家回復1點生命
            Span<byte> playerBuffer = stackalloc byte[16];
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, playerBuffer);
            
            if (playerCount > 0)
            {
                var healCmd = CommandBuilder.MakeHealCmd(playerBuffer[0], playerBuffer[0], 1);
                CommandSystem.PushCmd(healCmd);
            }
        }
        
        private static void HandleCardPlayBonus(SimpleEvent eventData)
        {
            // 簡單範例：使用蓄力卡時額外獲得1點蓄力
            if ((BasicAction)eventData.Value == BasicAction.CHARGE)
            {
                var bonusCmd = CommandBuilder.MakeChargeCmd(eventData.ActorId, 1);
                CommandSystem.PushCmd(bonusCmd);
            }
        }
    }
}