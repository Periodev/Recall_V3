// HighLevelAction.cs - 高階動作系統（簡化版）
// 戰術抽象：HLA → AtomicCmd[]序列翻譯

using System;

namespace CombatCore
{
    // HLA翻譯引擎 - 簡化版本
    public static class HLATranslator
    {
        // 簡化的HLA翻譯方法
        public static int TranslateHLA(HLA hla, byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            switch (hla)
            {
                case HLA.BASIC_ATTACK:
                    buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 10);
                    return 1;
                    
                case HLA.BASIC_BLOCK:
                    buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 5);
                    return 1;
                    
                case HLA.BASIC_CHARGE:
                    buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 1);
                    return 1;
                    
                case HLA.HEAVY_STRIKE:
                    buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 2);
                    buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 15);
                    return 2;
                    
                case HLA.SHIELD_BASH:
                    buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 3);
                    buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);
                    return 2;
                    
                case HLA.COMBO_ATTACK:
                    buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);
                    buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);
                    return 2;
                    
                case HLA.ENEMY_AGGRESSIVE:
                    buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 1);
                    buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 12);
                    return 2;
                    
                case HLA.ENEMY_DEFENSIVE:
                    buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 10);
                    buffer[1] = CommandBuilder.MakeChargeCmd(srcId, 2);
                    return 2;
                    
                case HLA.ENEMY_BERSERKER:
                    buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 20);
                    return 1;
                    
                default:
                    return 0;
            }
        }
        
        // 獲取基本動作類型
        public static int GetBasicAction(HLA hla)
        {
            return hla switch
            {
                HLA.BASIC_ATTACK => 1,
                HLA.BASIC_BLOCK => 2,
                HLA.BASIC_CHARGE => 3,
                HLA.HEAVY_STRIKE => 4,
                HLA.SHIELD_BASH => 5,
                HLA.COMBO_ATTACK => 6,
                HLA.ENEMY_AGGRESSIVE => 7,
                HLA.ENEMY_DEFENSIVE => 8,
                HLA.ENEMY_BERSERKER => 9,
                _ => 0
            };
        }
    }
    
    // 敵人意圖資訊 - 供UI顯示
    public readonly struct EnemyIntent
    {
        public readonly byte EnemyId;
        public readonly HLA Action;
        public readonly string Description;
        public readonly ushort EstimatedValue;
        
        public EnemyIntent(byte enemyId, HLA action, string description, ushort estimatedValue)
        {
            EnemyId = enemyId;
            Action = action;
            Description = description;
            EstimatedValue = estimatedValue;
        }
    }
    
    // HLA處理系統 - 管理HLA的暫存和處理
    public static class HLASystem
    {
        // 簡化的HLA暫存 - 避免複雜緩存機制
        private static HLA s_playerHLA = HLA.BASIC_ATTACK;
        private static readonly Dictionary<byte, HLA> s_enemyHLAs = new();
        
        // 設置玩家HLA
        public static void SetPlayerHLA(HLA hla) => s_playerHLA = hla;
        public static HLA GetPlayerHLA() => s_playerHLA;
        
        // 設置敵人HLA
        public static void SetEnemyHLA(byte enemyId, HLA hla) => s_enemyHLAs[enemyId] = hla;
        public static HLA GetEnemyHLA(byte enemyId) => s_enemyHLAs.GetValueOrDefault(enemyId, HLA.BASIC_ATTACK);
        
        // 處理HLA - 新的Reaction驅動機制
        public static bool ProcessHLA(byte actorId, byte targetId, HLA hla)
        {
            if (!ActorManager.IsAlive(actorId)) return false;
            
            // 直接翻譯為命令
            Span<AtomicCmd> buffer = stackalloc AtomicCmd[8];
            int cmdCount = HLATranslator.TranslateHLA(hla, actorId, targetId, buffer);
            
            // 推入命令
            for (int i = 0; i < cmdCount; i++)
                CommandSystem.PushCmd(buffer[i]);
            
            // 立即執行
            CommandSystem.ExecuteAll();
            
            // 觸發簡單事件
            SimpleEventSystem.OnCardPlayed(actorId, GetBasicAction(hla));
            
            return true;
        }
        
        // 處理玩家HLA
        public static bool ProcessPlayerHLA(byte playerId, byte targetId)
        {
            return ProcessHLA(playerId, targetId, s_playerHLA);
        }
        
        // 處理敵人HLA - 保留以維持兼容性，但實際上Enemy Phase不再手動調用
        public static bool ProcessEnemyHLA(byte enemyId, byte targetId)
        {
            var enemyHLA = GetEnemyHLA(enemyId);
            return ProcessHLA(enemyId, targetId, enemyHLA);
        }
        
        // 批次處理多個敵人HLA - 保留以維持兼容性
        public static int ProcessAllEnemyHLAs(ReadOnlySpan<byte> enemyIds, byte playerTargetId)
        {
            int processedCount = 0;
            foreach (byte enemyId in enemyIds)
            {
                if (ProcessEnemyHLA(enemyId, playerTargetId))
                {
                    processedCount++;
                }
            }
            return processedCount;
        }
        
        // 清理HLA暫存
        public static void Reset()
        {
            s_playerHLA = HLA.BASIC_ATTACK;
            s_enemyHLAs.Clear();
            HLATranslator.ResetRuleIdCounter(); // ✅ 重置新的規則ID計數器
        }
        
        // 獲取敵人意圖資訊 - 供UI顯示用
        public static EnemyIntent GetEnemyIntent(byte enemyId)
        {
            if (!ActorManager.IsAlive(enemyId)) 
                return new EnemyIntent(enemyId, HLA.BASIC_ATTACK, "死亡", 0);
                
            var hla = GetEnemyHLA(enemyId);
            var (description, estimatedValue) = GetHLAIntentInfo(hla, enemyId);
            
            return new EnemyIntent(enemyId, hla, description, estimatedValue);
        }
        
        // 獲取所有敵人意圖
        public static void GetAllEnemyIntents(Span<EnemyIntent> buffer, out int count)
        {
            count = 0;
            foreach (var kvp in s_enemyHLAs)
            {
                if (count >= buffer.Length) break;
                if (!ActorManager.IsAlive(kvp.Key)) continue;
                
                buffer[count] = GetEnemyIntent(kvp.Key);
                count++;
            }
        }
        
        // 根據HLA推測意圖資訊
        private static (string description, ushort estimatedValue) GetHLAIntentInfo(HLA hla, byte enemyId)
        {
            return hla switch
            {
                HLA.BASIC_ATTACK => ("攻擊", 10),
                HLA.BASIC_BLOCK => ("格擋", 5),
                HLA.BASIC_CHARGE => ("蓄力", 1),
                HLA.HEAVY_STRIKE => ("重擊", 25), // 15基礎 + 2蓄力*5加成
                HLA.SHIELD_BASH => ("盾擊", 8),
                HLA.COMBO_ATTACK => ("連擊", 14), // 7+7
                HLA.ENEMY_AGGRESSIVE => ("激進攻擊", 32), // 12 + 8，考慮蓄力
                HLA.ENEMY_DEFENSIVE => ("防禦姿態", 10),
                HLA.ENEMY_BERSERKER => ("狂暴", 18), // 6+6+6
                HLA.ENEMY_TURTLE => ("龜縮", 14), // 8+6護甲
                _ => ("未知", 0)
            };
        }
        
        // 除錯資訊
        public static void DebugPrintHLAs()
        {
            Console.WriteLine($"玩家HLA: {s_playerHLA}");
            foreach (var kvp in s_enemyHLAs)
            {
                Console.WriteLine($"敵人 {kvp.Key}: {kvp.Value}");
            }
        }
        
        public static void DebugPrintEnemyIntents()
        {
            Console.WriteLine("=== 敵人意圖 ===");
            var intents = new EnemyIntent[16];
            GetAllEnemyIntents(intents, out int count);
            
            for (int i = 0; i < count; i++)
            {
                var intent = intents[i];
                Console.WriteLine($"敵人 {intent.EnemyId}: {intent.Description} ({intent.EstimatedValue})");
            }
        }
    }
    
    // 簡單的戰鬥AI - 敵人HLA決策
    public static class CombatAI
    {
        private static readonly Random s_random = new();
        
        // 為敵人決策HLA
        public static HLA DecideForEnemy(byte enemyId)
        {
            if (!ActorManager.IsAlive(enemyId)) return HLA.BASIC_ATTACK;
            
            ref var enemy = ref ActorManager.GetActor(enemyId);
            
            // 簡單AI邏輯
            return enemy.Type switch
            {
                ActorType.ENEMY_BASIC => DecideBasicEnemyHLA(in enemy),
                ActorType.ENEMY_ELITE => DecideEliteEnemyHLA(in enemy),
                ActorType.ENEMY_BOSS => DecideBossEnemyHLA(in enemy),
                _ => HLA.BASIC_ATTACK
            };
        }
        
        private static HLA DecideBasicEnemyHLA(in Actor enemy)
        {
            // 血量低時防禦，否則攻擊
            if (enemy.HP < enemy.MaxHP / 3)
            {
                return s_random.NextDouble() < 0.7 ? HLA.ENEMY_DEFENSIVE : HLA.BASIC_BLOCK;
            }
            
            // 有蓄力時使用重擊，否則普通攻擊
            if (enemy.Charge > 0)
            {
                return HLA.HEAVY_STRIKE;
            }
            
            // 隨機選擇基礎動作
            return s_random.NextDouble() switch
            {
                < 0.5 => HLA.BASIC_ATTACK,
                < 0.8 => HLA.BASIC_CHARGE,
                _ => HLA.BASIC_BLOCK
            };
        }
        
        private static HLA DecideEliteEnemyHLA(in Actor enemy)
        {
            // 精英敵人使用更複雜的策略
            if (enemy.HP < enemy.MaxHP / 2)
            {
                return s_random.NextDouble() < 0.6 ? HLA.ENEMY_AGGRESSIVE : HLA.SHIELD_BASH;
            }
            
            return s_random.NextDouble() switch
            {
                < 0.3 => HLA.ENEMY_AGGRESSIVE,
                < 0.5 => HLA.COMBO_ATTACK,
                < 0.7 => HLA.HEAVY_STRIKE,
                _ => HLA.CHARGED_BLOCK
            };
        }
        
        private static HLA DecideBossEnemyHLA(in Actor enemy)
        {
            // BOSS使用最複雜的策略
            if (enemy.HP < enemy.MaxHP / 4)
            {
                // 血量極低時狂暴
                return HLA.ENEMY_BERSERKER;
            }
            
            if (enemy.HP < enemy.MaxHP / 2)
            {
                // 血量較低時激進
                return s_random.NextDouble() < 0.8 ? HLA.ENEMY_AGGRESSIVE : HLA.ENEMY_TURTLE;
            }
            
            // 血量健康時平衡策略
            return s_random.NextDouble() switch
            {
                < 0.25 => HLA.ENEMY_AGGRESSIVE,
                < 0.4 => HLA.ENEMY_DEFENSIVE,
                < 0.6 => HLA.COMBO_ATTACK,
                < 0.8 => HLA.HEAVY_STRIKE,
                _ => HLA.POWER_CHARGE
            };
        }
        
        // 批次為所有敵人決策HLA並處理意圖
        public static void DecideAndDeclareForAllEnemies()
        {
            // ✅ stackalloc工作記憶體
            Span<byte> enemyBuffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            int enemyCount = ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, enemyBuffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, enemyBuffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, enemyBuffer[enemyCount..]);
            
            // 獲取玩家作為攻擊目標
            Span<byte> playerBuffer = stackalloc byte[16];
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, playerBuffer);
            byte playerTargetId = playerCount > 0 ? playerBuffer[0] : (byte)0;
            
            for (int i = 0; i < enemyCount; i++)
            {
                byte enemyId = enemyBuffer[i];
                HLA decidedHLA = DecideForEnemy(enemyId);
                
                // 設定意圖（用於UI顯示）
                HLASystem.SetEnemyHLA(enemyId, decidedHLA);
                
                // 處理意圖宣告（立即效果 + 註冊延後效果）
                HLASystem.ProcessHLA(enemyId, playerTargetId, decidedHLA);
            }
        }
    }
}