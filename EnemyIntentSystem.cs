// 修正後的敵人意圖系統 - 真正的宣告與延後執行

using System;
using System.Collections.Generic;

namespace CombatCore
{
    // 敵人意圖數據結構
    public readonly struct EnemyIntent
    {
        public readonly byte EnemyId;
        public readonly HLA DeclaredAction;
        public readonly byte TargetId;
        public readonly string Description;
        public readonly ushort EstimatedDamage;
        
        public EnemyIntent(byte enemyId, HLA action, byte targetId, string description, ushort estimatedDamage)
        {
            EnemyId = enemyId;
            DeclaredAction = action;
            TargetId = targetId;
            Description = description;
            EstimatedDamage = estimatedDamage;
        }
    }
    
    // 敵人意圖管理系統
    public static class EnemyIntentSystem
    {
        // ✅ 純粹的意圖存儲，不執行任何動作
        private static readonly Dictionary<byte, EnemyIntent> s_declaredIntents = new();
        
        // Enemy Intent Phase：純粹決策和宣告
        public static void DeclareAllEnemyIntents()
        {
            s_declaredIntents.Clear();
            
            // 獲取所有活著的敵人
            Span<byte> enemyBuffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            int enemyCount = GetAllEnemies(enemyBuffer);
            
            // 獲取玩家作為攻擊目標
            byte playerTargetId = GetPlayerTarget();
            
            for (int i = 0; i < enemyCount; i++)
            {
                byte enemyId = enemyBuffer[i];
                
                // ✅ 只決策，不執行
                HLA decidedHLA = CombatAI.DecideForEnemy(enemyId);
                
                // ✅ 計算預期效果（用於UI顯示）
                var (description, estimatedDamage) = GetHLAIntentInfo(decidedHLA, enemyId);
                
                // ✅ 存儲意圖，不執行
                var intent = new EnemyIntent(enemyId, decidedHLA, playerTargetId, description, estimatedDamage);
                s_declaredIntents[enemyId] = intent;

                SimpleEventSystem.OnEnemyIntentDeclared(enemyId, decidedHLA);

                Console.WriteLine($"敵人 {enemyId} 宣告意圖: {description} (預計傷害: {estimatedDamage})");
            }
        }
        
        // Enemy Phase：執行之前宣告的意圖
        public static void ExecuteAllDeclaredIntents()
        {
            Console.WriteLine("執行敵人宣告的攻擊意圖:");
            
            foreach (var kvp in s_declaredIntents)
            {
                byte enemyId = kvp.Key;
                var intent = kvp.Value;
                
                // ✅ 檢查敵人是否還活著
                if (!ActorManager.IsAlive(enemyId))
                {
                    Console.WriteLine($"敵人 {enemyId} 已死亡，跳過執行");
                    continue;
                }
                
                // ✅ 檢查目標是否還活著
                if (!ActorManager.IsAlive(intent.TargetId))
                {
                    Console.WriteLine($"目標 {intent.TargetId} 已死亡，敵人 {enemyId} 攻擊失效");
                    continue;
                }
                
                // ✅ 現在才真正執行HLA
                Console.WriteLine($"敵人 {enemyId} 執行: {intent.Description}");
                HLASystem.ProcessHLA(enemyId, intent.TargetId, intent.DeclaredAction);

                SimpleEventSystem.OnEnemyIntentExecuted(enemyId, intent.DeclaredAction);
            }
        }
        
        // 獲取所有敵人意圖（供UI顯示）
        public static void GetAllIntents(Span<EnemyIntent> buffer, out int count)
        {
            count = 0;
            foreach (var kvp in s_declaredIntents)
            {
                if (count >= buffer.Length) break;
                
                // 只返回活著的敵人的意圖
                if (ActorManager.IsAlive(kvp.Key))
                {
                    buffer[count] = kvp.Value;
                    count++;
                }
            }
        }
        
        // 獲取特定敵人的意圖
        public static bool GetEnemyIntent(byte enemyId, out EnemyIntent intent)
        {
            return s_declaredIntents.TryGetValue(enemyId, out intent);
        }
        
        // 清理過期意圖
        public static void ClearIntents()
        {
            s_declaredIntents.Clear();
        }
        
        // 輔助方法
        private static int GetAllEnemies(Span<byte> buffer)
        {
            int count = 0;
            count += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, buffer[count..]);
            count += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, buffer[count..]);
            count += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, buffer[count..]);
            return count;
        }
        
        private static byte GetPlayerTarget()
        {
            Span<byte> playerBuffer = stackalloc byte[16];
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, playerBuffer);
            return playerCount > 0 ? playerBuffer[0] : (byte)0;
        }
        
        private static (string description, ushort estimatedDamage) GetHLAIntentInfo(HLA hla, byte enemyId)
        {
            return hla switch
            {
                HLA.BASIC_ATTACK => ("攻擊", 10),
                HLA.BASIC_BLOCK => ("格擋", 5),
                HLA.BASIC_CHARGE => ("蓄力", 1),
                HLA.HEAVY_STRIKE => ("重擊", CalculateHeavyStrikeDamage(enemyId)),
                HLA.SHIELD_BASH => ("盾擊", 8),
                HLA.COMBO_ATTACK => ("連擊", 16), // 8+8
                HLA.ENEMY_AGGRESSIVE => ("激進攻擊", CalculateAggressiveDamage(enemyId)),
                HLA.ENEMY_DEFENSIVE => ("防禦姿態", 10),
                HLA.ENEMY_BERSERKER => ("狂暴", 20),
                _ => ("未知", 0)
            };
        }
        
        private static ushort CalculateHeavyStrikeDamage(byte enemyId)
        {
            ref var enemy = ref ActorManager.GetActor(enemyId);
            // 基礎15 + 當前蓄力*5 + 預計獲得的2蓄力*5
            return (ushort)(15 + enemy.Charge * 5 + 2 * 5);
        }
        
        private static ushort CalculateAggressiveDamage(byte enemyId)
        {
            ref var enemy = ref ActorManager.GetActor(enemyId);
            // 基礎12 + 當前蓄力*5 + 預計獲得的1蓄力*5
            return (ushort)(12 + enemy.Charge * 5 + 1 * 5);
        }
        
        // 調試方法
        public static void DebugPrintIntents()
        {
            Console.WriteLine("=== 敵人意圖 ===");
            var intents = new EnemyIntent[16];
            GetAllIntents(intents, out int count);
            
            for (int i = 0; i < count; i++)
            {
                var intent = intents[i];
                Console.WriteLine($"敵人 {intent.EnemyId}: {intent.Description} (預估{intent.EstimatedDamage}傷害)");
            }
        }
    }
    
    // 修正後的CombatAI
    public static class CombatAI
    {
        // 決策方法保持不變，但移除了執行邏輯
        public static HLA DecideForEnemy(byte enemyId)
        {
            if (!ActorManager.IsAlive(enemyId)) return HLA.BASIC_ATTACK;
            
            ref var enemy = ref ActorManager.GetActor(enemyId);
            
            return enemy.Type switch
            {
                ActorType.ENEMY_BASIC => DecideBasicEnemyHLA(in enemy),
                ActorType.ENEMY_ELITE => DecideEliteEnemyHLA(in enemy),
                ActorType.ENEMY_BOSS => DecideBossEnemyHLA(in enemy),
                _ => HLA.BASIC_ATTACK
            };
        }
        
        // 決策邏輯保持不變...
        private static HLA DecideBasicEnemyHLA(in Actor enemy)
        {
            // 血量低時防禦，否則攻擊
            if (enemy.HP < enemy.MaxHP / 3)
            {
                return Random.Shared.NextDouble() < 0.7 ? HLA.ENEMY_DEFENSIVE : HLA.BASIC_BLOCK;
            }
            
            // 有蓄力時使用重擊
            if (enemy.Charge > 0)
            {
                return HLA.HEAVY_STRIKE;
            }
            
            // 隨機選擇基礎動作
            return Random.Shared.NextDouble() switch
            {
                < 0.5 => HLA.BASIC_ATTACK,
                < 0.8 => HLA.BASIC_CHARGE,
                _ => HLA.BASIC_BLOCK
            };
        }
        
        private static HLA DecideEliteEnemyHLA(in Actor enemy)
        {
            if (enemy.HP < enemy.MaxHP / 2)
            {
                return Random.Shared.NextDouble() < 0.6 ? HLA.ENEMY_AGGRESSIVE : HLA.SHIELD_BASH;
            }
            
            return Random.Shared.NextDouble() switch
            {
                < 0.3 => HLA.ENEMY_AGGRESSIVE,
                < 0.5 => HLA.COMBO_ATTACK,
                < 0.7 => HLA.HEAVY_STRIKE,
                _ => HLA.CHARGED_BLOCK
            };
        }
        
        private static HLA DecideBossEnemyHLA(in Actor enemy)
        {
            if (enemy.HP < enemy.MaxHP / 4)
            {
                return HLA.ENEMY_BERSERKER;
            }
            
            if (enemy.HP < enemy.MaxHP / 2)
            {
                return Random.Shared.NextDouble() < 0.8 ? HLA.ENEMY_AGGRESSIVE : HLA.ENEMY_TURTLE;
            }
            
            return Random.Shared.NextDouble() switch
            {
                < 0.25 => HLA.ENEMY_AGGRESSIVE,
                < 0.4 => HLA.ENEMY_DEFENSIVE,
                < 0.6 => HLA.COMBO_ATTACK,
                < 0.8 => HLA.HEAVY_STRIKE,
                _ => HLA.POWER_CHARGE
            };
        }
    }
}

// 修正後的Phase.cs中的相關部分
/*
private static PhaseResult EnemyIntent_Process(Span<byte> actorBuffer)
{
    // ✅ 修正：只宣告意圖，不執行
    EnemyIntentSystem.DeclareAllEnemyIntents();
    
    s_context.CurrentStep = PhaseStep.END;
    return PhaseResult.NEXT_STEP;
}

private static PhaseResult EnemyPhase_Process(Span<byte> actorBuffer)
{
    // ✅ 修正：現在才執行之前宣告的意圖
    Console.WriteLine("敵人執行之前宣告的攻擊意圖:");
    EnemyIntentSystem.ExecuteAllDeclaredIntents();
    
    s_context.CurrentStep = PhaseStep.EXECUTE;
    return PhaseResult.NEXT_STEP;
}
*/