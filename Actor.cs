// Actor.cs - 角色數據管理
// 最低限度C風格：靜態陣列 + ref返回 + 簡化分配

using System;
using System.Collections.Generic;

namespace CombatCore
{
    // Actor結構 - 純數據容器，不包含行為
    public struct Actor
    {
        public byte Id;
        public ActorType Type;
        public ushort HP, MaxHP;
        public ushort Block;        // 當回合護甲，回合結束歸零
        public byte Charge;         // 蓄力值，影響下次攻擊
        
        // ✅ 簡化狀態管理：Dictionary而非位運算
        public Dictionary<StatusFlags, byte> StatusDurations;
        
        // 只讀屬性 - 狀態查詢
        public readonly bool IsAlive => HP > 0;
        public readonly bool CanAct => IsAlive && !HasStatus(StatusFlags.STUNNED) && !HasStatus(StatusFlags.FROZEN);
        public readonly bool HasBlock => Block > 0;
        public readonly bool HasCharge => Charge > 0;
        
        // 狀態查詢輔助方法
        public readonly bool HasStatus(StatusFlags status) 
        {
            return StatusDurations != null && StatusDurations.ContainsKey(status);
        }
        
        public readonly byte GetStatusDuration(StatusFlags status)
        {
            return StatusDurations?.GetValueOrDefault(status, (byte)0) ?? 0;
        }
    }
    
    // Actor管理器 - 靜態陣列記憶體池
    public static class ActorManager
    {
        // ✅ C風格：靜態陣列，固定大小池
        private static readonly Actor[] s_actors = new Actor[CombatConstants.MAX_ACTORS];
        private static int s_nextId = 0;                    // ✅ 簡化分配：遞增計數
        private static int s_count = 0;                     // 當前Actor數量
        
        // 分配新Actor
        public static byte AllocateActor(ActorType type, ushort maxHP)
        {
            if (s_nextId >= CombatConstants.MAX_ACTORS)
            {
                throw new InvalidOperationException($"Actor池已滿，最大容量：{CombatConstants.MAX_ACTORS}");
            }
            
            byte id = (byte)s_nextId++;
            s_actors[id] = new Actor
            {
                Id = id,
                Type = type,
                HP = maxHP,
                MaxHP = maxHP,
                Block = 0,
                Charge = 0,
                StatusDurations = new Dictionary<StatusFlags, byte>()
            };
            
            s_count++;
            return id;
        }
        
        // ✅ C風格：ref返回，避免結構體複製
        public static ref Actor GetActor(byte id)
        {
            if (id >= s_nextId)
            {
                throw new ArgumentOutOfRangeException(nameof(id), $"無效的Actor ID: {id}");
            }
            return ref s_actors[id];
        }
        
        // 快速狀態查詢 - 避免ref傳遞的簡單查詢
        public static bool IsAlive(byte id) => id < s_nextId && s_actors[id].IsAlive;
        public static bool CanAct(byte id) => id < s_nextId && s_actors[id].CanAct;
        public static ActorType GetType(byte id) => id < s_nextId ? s_actors[id].Type : ActorType.PLAYER;
        
        // 獲取所有活著的Actor ID
        public static int GetAliveActors(Span<byte> buffer)
        {
            int count = 0;
            for (byte i = 0; i < s_nextId && count < buffer.Length; i++)
            {
                if (s_actors[i].IsAlive)
                {
                    buffer[count++] = i;
                }
            }
            return count;
        }
        
        // 獲取特定類型的Actor ID
        public static int GetActorsByType(ActorType type, Span<byte> buffer)
        {
            int count = 0;
            for (byte i = 0; i < s_nextId && count < buffer.Length; i++)
            {
                if (s_actors[i].IsAlive && s_actors[i].Type == type)
                {
                    buffer[count++] = i;
                }
            }
            return count;
        }
        
        // 清理死亡Actor - 簡化版本，只重置HP
        public static void RemoveActor(byte id)
        {
            if (id < s_nextId)
            {
                ref var actor = ref s_actors[id];
                actor.HP = 0;
                actor.Block = 0;
                actor.Charge = 0;
                actor.StatusDurations?.Clear();
                s_count--;
            }
        }
        
        // 重置整個Actor池 - 用於戰鬥結束或重啟
        public static void Reset()
        {
            for (int i = 0; i < s_nextId; i++)
            {
                s_actors[i].StatusDurations?.Clear();
            }
            Array.Clear(s_actors, 0, s_nextId);
            s_nextId = 0;
            s_count = 0;
        }
        
        // 除錯資訊
        public static int GetActorCount() => s_count;
        public static int GetAllocatedCount() => s_nextId;
        
        // 回合結束清理 - 護甲歸零，狀態持續時間減少
        public static void EndTurnCleanup()
        {
            for (int i = 0; i < s_nextId; i++)
            {
                ref var actor = ref s_actors[i];
                if (!actor.IsAlive) continue;
                
                // 護甲歸零
                actor.Block = 0;
                
                // 狀態持續時間減少
                if (actor.StatusDurations != null && actor.StatusDurations.Count > 0)
                {
                    var statusesToRemove = new List<StatusFlags>();
                    var statusesToUpdate = new List<(StatusFlags status, byte duration)>();
                    
                    foreach (var kvp in actor.StatusDurations)
                    {
                        byte newDuration = (byte)(kvp.Value - 1);
                        if (newDuration <= 0)
                        {
                            statusesToRemove.Add(kvp.Key);
                        }
                        else
                        {
                            statusesToUpdate.Add((kvp.Key, newDuration));
                        }
                    }
                    
                    // 移除過期狀態
                    foreach (var status in statusesToRemove)
                    {
                        actor.StatusDurations.Remove(status);
                    }
                    
                    // 更新剩餘狀態
                    foreach (var (status, duration) in statusesToUpdate)
                    {
                        actor.StatusDurations[status] = duration;
                    }
                }
            }
        }
    }
    
    // Actor操作輔助方法 - 常用的Actor修改操作
    public static class ActorOperations
    {
        // ==================== 核心狀態變更 API ====================
        
        // 生命值操作
        public static ushort SetHP(byte actorId, ushort newHP)
        {
            ref var actor = ref ActorManager.GetActor(actorId);
            ushort oldHP = actor.HP;
            actor.HP = (ushort)Math.Min(newHP, actor.MaxHP);
            return (ushort)Math.Abs(actor.HP - oldHP);
        }
        
        public static ushort ReduceHP(byte actorId, ushort amount)
        {
            ref var actor = ref ActorManager.GetActor(actorId);
            ushort oldHP = actor.HP;
            actor.HP = (ushort)Math.Max(0, actor.HP - amount);
            return (ushort)(oldHP - actor.HP); // 實際減少量
        }
        
        public static ushort AddHP(byte actorId, ushort amount)
        {
            ref var actor = ref ActorManager.GetActor(actorId);
            ushort oldHP = actor.HP;
            actor.HP = (ushort)Math.Min(actor.MaxHP, actor.HP + amount);
            return (ushort)(actor.HP - oldHP); // 實際治療量
        }
        
        // 護甲操作
        public static ushort SetBlock(byte actorId, ushort amount)
        {
            ref var actor = ref ActorManager.GetActor(actorId);
            actor.Block = (ushort)Math.Min(CombatConstants.MAX_BLOCK, amount);
            return actor.Block;
        }
        
        public static ushort AddBlock(byte actorId, ushort amount)
        {
            ref var actor = ref ActorManager.GetActor(actorId);
            ushort oldBlock = actor.Block;
            actor.Block = (ushort)Math.Min(CombatConstants.MAX_BLOCK, actor.Block + amount);
            return (ushort)(actor.Block - oldBlock); // 實際增加量
        }
        
        public static ushort ReduceBlock(byte actorId, ushort amount)
        {
            ref var actor = ref ActorManager.GetActor(actorId);
            ushort oldBlock = actor.Block;
            actor.Block = (ushort)Math.Max(0, actor.Block - amount);
            return (ushort)(oldBlock - actor.Block); // 實際減少量
        }
        
        public static void ClearBlock(byte actorId)
        {
            ref var actor = ref ActorManager.GetActor(actorId);
            actor.Block = 0;
        }
        
        // ✅ 新增：Charge 專用操作
        public static byte AddCharge(byte actorId, byte amount)
        {
            ref var actor = ref ActorManager.GetActor(actorId);
            byte oldCharge = actor.Charge;
            actor.Charge = (byte)Math.Min(CombatConstants.MAX_CHARGE, actor.Charge + amount);
            return (byte)(actor.Charge - oldCharge); // 實際增加量
        }
        
        public static byte ConsumeCharge(byte actorId, byte amount = 255) // 255 = 消耗全部
        {
            ref var actor = ref ActorManager.GetActor(actorId);
            byte oldCharge = actor.Charge;
            
            if (amount == 255) // 消耗全部
            {
                actor.Charge = 0;
                return oldCharge; // 返回消耗的量
            }
            else // 消耗指定量
            {
                byte consumeAmount = (byte)Math.Min(actor.Charge, amount);
                actor.Charge -= consumeAmount;
                return consumeAmount; // 返回實際消耗量
            }
        }
        
        public static byte GetCharge(byte actorId)
        {
            ref var actor = ref ActorManager.GetActor(actorId);
            return actor.Charge;
        }
        
        public static void ClearCharge(byte actorId)
        {
            ref var actor = ref ActorManager.GetActor(actorId);
            actor.Charge = 0;
        }
        
        // ==================== 組合操作（基於基礎API構建）====================
        
        // 造成傷害 - 考慮護甲減免
        public static ushort DealDamage(byte targetId, ushort damage)
        {
            if (!ActorManager.IsAlive(targetId)) return 0;
            
            ushort actualDamage = damage;
            
            // 護甲減免
            ref var target = ref ActorManager.GetActor(targetId);
            if (target.Block > 0)
            {
                if (target.Block >= damage)
                {
                    ReduceBlock(targetId, damage);
                    return 0; // 完全格擋
                }
                else
                {
                    actualDamage = (ushort)(damage - target.Block);
                    ClearBlock(targetId); // 護甲完全消耗
                }
            }
            
            // 扣除生命值
            return ReduceHP(targetId, actualDamage);
        }
        
        // 治療
        public static ushort Heal(byte targetId, ushort amount)
        {
            if (!ActorManager.IsAlive(targetId)) return 0;
            return AddHP(targetId, amount);
        }
        
        // 狀態操作（保持原有邏輯）
        public static void AddStatus(byte targetId, StatusFlags status, byte duration)
        {
            ref var target = ref ActorManager.GetActor(targetId);
            if (!target.IsAlive) return;
            
            target.StatusDurations ??= new Dictionary<StatusFlags, byte>();
            
            if (target.StatusDurations.ContainsKey(status))
            {
                target.StatusDurations[status] = Math.Max(target.StatusDurations[status], duration);
            }
            else
            {
                target.StatusDurations[status] = duration;
            }
        }
        
        public static bool RemoveStatus(byte targetId, StatusFlags status)
        {
            ref var target = ref ActorManager.GetActor(targetId);
            if (!target.IsAlive || target.StatusDurations == null) return false;
            
            return target.StatusDurations.Remove(status);
        }
    }
}