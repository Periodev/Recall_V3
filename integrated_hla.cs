// IntegratedHLA.cs - 整合的HLA系統
// 結合原始HLA系統和抽象化行為系統，提供統一接口

using System;
using System.Collections.Generic;

namespace CombatCore
{
    // 統一的HLA管理器 - 整合兩套系統
    public static class IntegratedHLAManager
    {
        // 保留原始系統的HLA暫存
        private static HLA s_playerHLA = HLA.BASIC_ATTACK;
        private static readonly Dictionary<byte, HLA> s_enemyHLAs = new();
        
        // 新增行為系統的行為暫存
        private static readonly Dictionary<byte, IBehavior> s_actorBehaviors = new();
        
        // 初始化整合系統
        public static void Initialize()
        {
            // 確保行為註冊表已初始化
            BehaviorRegistry.EnsureInitialized();
            
            // 重置HLA翻譯器
            HLATranslator.ResetRuleIdCounter();
        }
        
        // ================== 玩家HLA處理 ==================
        
        // 設置玩家HLA（向後兼容）
        public static void SetPlayerHLA(HLA hla) 
        {
            s_playerHLA = hla;
            
            // 同時設置對應的行為
            var behavior = ConvertHLAToBehavior(hla);
            if (behavior != null)
            {
                byte playerId = GetPlayerId();
                if (playerId != 255)
                {
                    s_actorBehaviors[playerId] = behavior;
                }
            }
        }
        
        // 獲取玩家HLA（向後兼容）
        public static HLA GetPlayerHLA() => s_playerHLA;
        
        // 處理玩家HLA（優先使用行為系統）
        public static bool ProcessPlayerHLA(byte playerId, byte targetId)
        {
            // 優先使用行為系統
            if (s_actorBehaviors.TryGetValue(playerId, out var behavior))
            {
                return behavior.Execute(playerId, targetId);
            }
            
            // 降級到原始HLA系統
            return HLASystem.ProcessPlayerHLA(playerId, targetId);
        }
        
        // ================== 敵人HLA處理 ==================
        
        // 為敵人決策HLA和行為
        public static void DecideEnemyAction(byte enemyId)
        {
            if (!ActorManager.IsAlive(enemyId))
                return;
            
            // 使用行為系統決策
            var behavior = BehaviorBasedAI.SelectBehaviorForEnemy(enemyId);
            if (behavior != null)
            {
                s_actorBehaviors[enemyId] = behavior;
                
                // 同時設置對應的HLA（向後兼容）
                var hla = ConvertBehaviorToHLA(behavior);
                s_enemyHLAs[enemyId] = hla;
            }
            else
            {
                // 降級到原始AI系統
                var hla = CombatAI.DecideForEnemy(enemyId);
                s_enemyHLAs[enemyId] = hla;
            }
        }
        
        // 處理敵人行動（Intent階段的立即效果）
        public static bool ProcessEnemyIntent(byte enemyId, byte targetId)
        {
            // 優先使用行為系統
            if (s_actorBehaviors.TryGetValue(enemyId, out var behavior))
            {
                // 使用新的翻譯機制：分離立即和延後效果
                var hla = ConvertBehaviorToHLA(behavior);
                HLATranslator.TranslateAndRegisterHLA(hla, enemyId, targetId);
                return true;
            }
            
            // 降級到原始系統
            var storedHLA = s_enemyHLAs.GetValueOrDefault(enemyId, HLA.BASIC_ATTACK);
            HLATranslator.TranslateAndRegisterHLA(storedHLA, enemyId, targetId);
            return true;
        }
        
        // 獲取敵人意圖資訊（UI顯示用）
        public static EnemyIntent GetEnemyIntent(byte enemyId)
        {
            if (!ActorManager.IsAlive(enemyId))
                return new EnemyIntent(enemyId, HLA.BASIC_ATTACK, "死亡", 0);
            
            // 優先使用行為系統
            if (s_actorBehaviors.TryGetValue(enemyId, out var behavior))
            {
                string description = behavior.GetIntentDescription(enemyId);
                var hla = ConvertBehaviorToHLA(behavior);
                var (_, estimatedValue) = GetHLAIntentInfo(hla, enemyId);
                
                return new EnemyIntent(enemyId, hla, description, estimatedValue);
            }
            
            // 降級到原始系統
            return HLASystem.GetEnemyIntent(enemyId);
        }
        
        // ================== 批次處理 ==================
        
        // 為所有敵人決策並處理意圖
        public static void DecideAndProcessAllEnemyIntents()
        {
            Span<byte> enemyBuffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            int enemyCount = GetAllEnemies(enemyBuffer);
            byte playerTargetId = GetPlayerId();
            
            for (int i = 0; i < enemyCount; i++)
            {
                byte enemyId = enemyBuffer[i];
                
                // 決策
                DecideEnemyAction(enemyId);
                
                // 處理意圖（立即效果）
                ProcessEnemyIntent(enemyId, playerTargetId);
            }
            
            // 執行所有立即效果
            CommandSystem.ExecuteAll();
        }
        
        // ================== 卡牌系統整合 ==================
        
        // 從簡化卡牌系統使用HLA
        public static bool ProcessCardAction(BasicAction action, byte actorId, byte targetId)
        {
            // 將基礎行動轉換為HLA
            var hla = action switch
            {
                BasicAction.ATTACK => HLA.BASIC_ATTACK,
                BasicAction.BLOCK => HLA.BASIC_BLOCK,
                BasicAction.CHARGE => HLA.BASIC_CHARGE,
                _ => HLA.BASIC_ATTACK
            };
            
            // 優先使用行為系統
            var behaviorId = action switch
            {
                BasicAction.ATTACK => "player_attack",
                BasicAction.BLOCK => "player_block", 
                BasicAction.CHARGE => "player_charge",
                _ => "player_attack"
            };
            
            var behavior = BehaviorRegistry.GetBehavior(behaviorId);
            if (behavior != null)
            {
                return behavior.Execute(actorId, targetId);
            }
            
            // 降級到原始HLA系統
            return HLASystem.ProcessHLA(actorId, targetId, hla);
        }
        
        // ================== 轉換輔助函數 ==================
        
        // HLA轉行為
        private static IBehavior ConvertHLAToBehavior(HLA hla)
        {
            return hla switch
            {
                HLA.BASIC_ATTACK => BehaviorRegistry.GetBehavior("player_attack"),
                HLA.BASIC_BLOCK => BehaviorRegistry.GetBehavior("player_block"),
                HLA.BASIC_CHARGE => BehaviorRegistry.GetBehavior("player_charge"),
                HLA.ENEMY_AGGRESSIVE => BehaviorRegistry.GetBehavior("enemy_aggressive"),
                HLA.ENEMY_DEFENSIVE => BehaviorRegistry.GetBehavior("enemy_defensive"),
                _ => BehaviorRegistry.GetBehavior("player_attack")
            };
        }
        
        // 行為轉HLA
        private static HLA ConvertBehaviorToHLA(IBehavior behavior)
        {
            return behavior.Name switch
            {
                "攻擊" => HLA.BASIC_ATTACK,
                "格擋" => HLA.BASIC_BLOCK,
                "蓄力" => HLA.BASIC_CHARGE,
                "激進攻擊" => HLA.ENEMY_AGGRESSIVE,
                "防禦姿態" => HLA.ENEMY_DEFENSIVE,
                "基礎攻擊" => HLA.BASIC_ATTACK,
                _ => HLA.BASIC_ATTACK
            };
        }
        
        // ================== 除錯和工具函數 ==================
        
        // 顯示所有敵人意圖
        public static void DebugPrintAllEnemyIntents()
        {
            Console.WriteLine("=== 敵人意圖 (整合系統) ===");
            
            Span<byte> enemyBuffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            int enemyCount = GetAllEnemies(enemyBuffer);
            
            for (int i = 0; i < enemyCount; i++)
            {
                byte enemyId = enemyBuffer[i];
                var intent = GetEnemyIntent(enemyId);
                Console.WriteLine($"  敵人 {enemyId}: {intent.Description} (預估{intent.EstimatedValue}傷害)");
            }
        }
        
        // 重置系統
        public static void Reset()
        {
            s_playerHLA = HLA.BASIC_ATTACK;
            s_enemyHLAs.Clear();
            s_actorBehaviors.Clear();
            
            // 重置子系統
            HLASystem.Reset();
            ReactionSystem.Reset();
        }
        
        // ================== 私有輔助函數 ==================
        
        private static byte GetPlayerId()
        {
            Span<byte> playerBuffer = stackalloc byte[16];
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, playerBuffer);
            return playerCount > 0 ? playerBuffer[0] : (byte)255;
        }
        
        private static int GetAllEnemies(Span<byte> buffer)
        {
            int count = 0;
            count += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, buffer[count..]);
            count += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, buffer[count..]);
            count += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, buffer[count..]);
            return count;
        }
        
        private static (string description, ushort estimatedValue) GetHLAIntentInfo(HLA hla, byte enemyId)
        {
            return hla switch
            {
                HLA.BASIC_ATTACK => ("攻擊", 10),
                HLA.BASIC_BLOCK => ("格擋", 5),
                HLA.BASIC_CHARGE => ("蓄力", 1),
                HLA.HEAVY_STRIKE => ("重擊", 25),
                HLA.SHIELD_BASH => ("盾擊", 8),
                HLA.COMBO_ATTACK => ("連擊", 14),
                HLA.ENEMY_AGGRESSIVE => ("激進攻擊", 32),
                HLA.ENEMY_DEFENSIVE => ("防禦姿態", 10),
                HLA.ENEMY_BERSERKER => ("狂暴", 18),
                HLA.ENEMY_TURTLE => ("龜縮", 14),
                _ => ("未知", 0)
            };
        }
    }
    
    // 擴展行為註冊表，確保初始化
    public static partial class BehaviorRegistry
    {
        private static bool s_initialized = false;
        
        public static void EnsureInitialized()
        {
            if (s_initialized) return;
            
            // 這裡可以添加額外的行為註冊
            // RegisterBehavior("custom_behavior", new CustomBehavior());
            
            s_initialized = true;
        }
    }
}