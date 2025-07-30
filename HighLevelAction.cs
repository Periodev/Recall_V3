// HLA.cs - 高階動作系統
// 戰術抽象：HLA → AtomicCmd[]序列翻譯

using System;

namespace CombatCore
{
    // HLA翻譯引擎 - 戰術意圖轉換為立即效果和延後效果
    public static class HLATranslator
    {
        // ✅ 新的翻譯機制：分離立即和延後效果
        public static void TranslateAndRegisterHLA(HLA hla, byte srcId, byte targetId)
        {
            // 1. 處理立即效果（BLOCK/CHARGE等）
            ProcessImmediateEffects(hla, srcId);
            
            // 2. 註冊延後效果（ATTACK等）
            RegisterDelayedEffects(hla, srcId, targetId);
        }
        
        // 處理立即效果（Intent Phase執行）
        private static void ProcessImmediateEffects(HLA hla, byte srcId)
        {
            switch (hla)
            {
                case HLA.BASIC_BLOCK:
                    CommandSystem.PushCmd(CommandBuilder.MakeBlockCmd(srcId, 5));
                    break;
                    
                case HLA.BASIC_CHARGE:
                    CommandSystem.PushCmd(CommandBuilder.MakeChargeCmd(srcId, 1));
                    break;
                    
                case HLA.HEAVY_STRIKE:
                    // 立即蓄力，攻擊延後
                    CommandSystem.PushCmd(CommandBuilder.MakeChargeCmd(srcId, 2));
                    break;
                    
                case HLA.SHIELD_BASH:
                    // 立即護甲，攻擊延後
                    CommandSystem.PushCmd(CommandBuilder.MakeBlockCmd(srcId, 3));
                    break;
                    
                case HLA.CHARGED_BLOCK:
                    // 立即蓄力和護甲
                    CommandSystem.PushCmd(CommandBuilder.MakeChargeCmd(srcId, 1));
                    CommandSystem.PushCmd(CommandBuilder.MakeBlockCmd(srcId, 8));
                    break;
                    
                case HLA.POWER_CHARGE:
                    // 立即強力蓄力
                    CommandSystem.PushCmd(CommandBuilder.MakeChargeCmd(srcId, 2));
                    CommandSystem.PushCmd(CommandBuilder.MakeChargeCmd(srcId, 1));
                    break;
                    
                case HLA.ENEMY_AGGRESSIVE:
                    // 立即蓄力，攻擊延後
                    CommandSystem.PushCmd(CommandBuilder.MakeChargeCmd(srcId, 1));
                    break;
                    
                case HLA.ENEMY_DEFENSIVE:
                    // 立即護甲和蓄力
                    CommandSystem.PushCmd(CommandBuilder.MakeBlockCmd(srcId, 10));
                    CommandSystem.PushCmd(CommandBuilder.MakeChargeCmd(srcId, 2));
                    break;
                    
                case HLA.ENEMY_TURTLE:
                    // 立即雙重護甲
                    CommandSystem.PushCmd(CommandBuilder.MakeBlockCmd(srcId, 8));
                    CommandSystem.PushCmd(CommandBuilder.MakeBlockCmd(srcId, 6));
                    break;
                    
                // 純攻擊型HLA無立即效果
                case HLA.BASIC_ATTACK:
                case HLA.COMBO_ATTACK:
                case HLA.ENEMY_BERSERKER:
                    // 這些HLA沒有立即效果，只有延後攻擊
                    break;
            }
        }
        
        // 註冊延後效果（Enemy Phase觸發）
        private static void RegisterDelayedEffects(HLA hla, byte srcId, byte targetId)
        {
            // 檢查HLA是否有攻擊效果
            if (!HasAttackEffect(hla)) return;
            
            // 創建延後攻擊的Reaction規則
            var condition = new ReactionCondition(
                ReactionTrigger.ENEMY_PHASE_START,
                sourceFilter: srcId
            );
            
            // 將HLA和目標打包到Value中
            ushort packedValue = (ushort)((byte)hla << 8 | targetId);
            
            var effect = new ReactionEffect(
                ReactionEffectType.EXECUTE_HLA,
                255, // 255表示使用事件目標
                packedValue
            );
            
            var rule = new ReactionRule(
                GetUniqueRuleId(),
                condition,
                effect,
                $"延後攻擊-{hla}",
                true // 一次性規則
            );
            
            ReactionSystem.RegisterRule(rule);
        }
        
        // 檢查HLA是否包含攻擊效果
        private static bool HasAttackEffect(HLA hla) => hla switch
        {
            HLA.BASIC_ATTACK => true,
            HLA.HEAVY_STRIKE => true,
            HLA.SHIELD_BASH => true,
            HLA.COMBO_ATTACK => true,
            HLA.ENEMY_AGGRESSIVE => true,
            HLA.ENEMY_BERSERKER => true,
            _ => false
        };
        
        // 生成唯一的規則ID
        private static byte s_nextRuleId = 100; // 從100開始避免與CommonReactions衝突
        private static byte GetUniqueRuleId() => s_nextRuleId++;
        
        // 重置規則ID計數器
        public static void ResetRuleIdCounter() => s_nextRuleId = 100;
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
            
            // ✅ 使用新的翻譯機制：立即效果 + 延後效果註冊
            HLATranslator.TranslateAndRegisterHLA(hla, actorId, targetId);
            
            // 觸發意圖宣告事件（處理立即效果）
            ReactionEventDispatcher.OnIntentDeclared(actorId, hla, targetId);
            
            // 執行立即效果
            CommandSystem.ExecuteAll();
            
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