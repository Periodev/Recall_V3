// AbstractedHLA.cs - 抽象化的HLA系統
// 分離玩家行為和怪物行為，提供更清晰的行為模式

using System;
using System.Collections.Generic;

namespace CombatCore
{
    // 行為類別 - 區分不同Actor的行為模式
    public enum BehaviorCategory : byte
    {
        PLAYER_ACTION = 1,      // 玩家行動
        ENEMY_INTENT = 2,       // 敵人意圖
        SYSTEM_ACTION = 3,      // 系統行為
    }
    
    // 抽象行為接口
    public interface IBehavior
    {
        BehaviorCategory Category { get; }
        string Name { get; }
        string Description { get; }
        bool Execute(byte actorId, byte targetId);
        string GetIntentDescription(byte actorId); // 用於敵人意圖顯示
    }
    
    // 玩家基礎行為實現
    public class PlayerAttackBehavior : IBehavior
    {
        public BehaviorCategory Category => BehaviorCategory.PLAYER_ACTION;
        public string Name => "攻擊";
        public string Description => "對敵人造成傷害";
        
        public bool Execute(byte actorId, byte targetId)
        {
            if (!ActorManager.IsAlive(actorId) || !ActorManager.IsAlive(targetId))
                return false;
                
            // 使用原本的HLA系統
            return HLASystem.ProcessHLA(actorId, targetId, HLA.BASIC_ATTACK);
        }
        
        public string GetIntentDescription(byte actorId) => "攻擊";
    }
    
    public class PlayerBlockBehavior : IBehavior
    {
        public BehaviorCategory Category => BehaviorCategory.PLAYER_ACTION;
        public string Name => "格擋";
        public string Description => "獲得護甲值";
        
        public bool Execute(byte actorId, byte targetId)
        {
            if (!ActorManager.IsAlive(actorId))
                return false;
                
            return HLASystem.ProcessHLA(actorId, targetId, HLA.BASIC_BLOCK);
        }
        
        public string GetIntentDescription(byte actorId) => "格擋";
    }
    
    public class PlayerChargeBehavior : IBehavior
    {
        public BehaviorCategory Category => BehaviorCategory.PLAYER_ACTION;
        public string Name => "蓄力";
        public string Description => "增加蓄力值，提升下次攻擊";
        
        public bool Execute(byte actorId, byte targetId)
        {
            if (!ActorManager.IsAlive(actorId))
                return false;
                
            return HLASystem.ProcessHLA(actorId, targetId, HLA.BASIC_CHARGE);
        }
        
        public string GetIntentDescription(byte actorId) => "蓄力";
    }
    
    // 敵人行為實現 - 基於敵人類型和狀態的動態行為
    public class EnemyAggressiveBehavior : IBehavior
    {
        public BehaviorCategory Category => BehaviorCategory.ENEMY_INTENT;
        public string Name => "激進攻擊";
        public string Description => "敵人進行激進的攻擊模式";
        
        public bool Execute(byte actorId, byte targetId)
        {
            if (!ActorManager.IsAlive(actorId))
                return false;
                
            return HLASystem.ProcessHLA(actorId, targetId, HLA.ENEMY_AGGRESSIVE);
        }
        
        public string GetIntentDescription(byte actorId)
        {
            if (!ActorManager.IsAlive(actorId)) return "已死亡";
            
            ref var actor = ref ActorManager.GetActor(actorId);
            // 根據蓄力值計算預估傷害
            int estimatedDamage = 15 + (actor.Charge * 5);
            return $"激進攻擊 ({estimatedDamage}傷害)";
        }
    }
    
    public class EnemyDefensiveBehavior : IBehavior
    {
        public BehaviorCategory Category => BehaviorCategory.ENEMY_INTENT;
        public string Name => "防禦姿態";
        public string Description => "敵人採取防禦姿態";
        
        public bool Execute(byte actorId, byte targetId)
        {
            if (!ActorManager.IsAlive(actorId))
                return false;
                
            return HLASystem.ProcessHLA(actorId, targetId, HLA.ENEMY_DEFENSIVE);
        }
        
        public string GetIntentDescription(byte actorId)
        {
            return "防禦姿態 (獲得護甲+蓄力)";
        }
    }
    
    public class EnemyBasicAttackBehavior : IBehavior
    {
        public BehaviorCategory Category => BehaviorCategory.ENEMY_INTENT;
        public string Name => "基礎攻擊";
        public string Description => "敵人進行基礎攻擊";
        
        public bool Execute(byte actorId, byte targetId)
        {
            if (!ActorManager.IsAlive(actorId))
                return false;
                
            return HLASystem.ProcessHLA(actorId, targetId, HLA.BASIC_ATTACK);
        }
        
        public string GetIntentDescription(byte actorId)
        {
            if (!ActorManager.IsAlive(actorId)) return "已死亡";
            
            ref var actor = ref ActorManager.GetActor(actorId);
            int estimatedDamage = 10 + (actor.Charge * 5);
            return $"攻擊 ({estimatedDamage}傷害)";
        }
    }
    
    // 行為註冊表 - 管理所有可用的行為
    public static class BehaviorRegistry
    {
        private static readonly Dictionary<string, IBehavior> s_behaviors = new();
        
        // 初始化所有行為
        static BehaviorRegistry()
        {
            RegisterBehavior("player_attack", new PlayerAttackBehavior());
            RegisterBehavior("player_block", new PlayerBlockBehavior());
            RegisterBehavior("player_charge", new PlayerChargeBehavior());
            
            RegisterBehavior("enemy_aggressive", new EnemyAggressiveBehavior());
            RegisterBehavior("enemy_defensive", new EnemyDefensiveBehavior());
            RegisterBehavior("enemy_basic_attack", new EnemyBasicAttackBehavior());
        }
        
        // 註冊行為
        public static void RegisterBehavior(string id, IBehavior behavior)
        {
            s_behaviors[id] = behavior;
        }
        
        // 獲取行為
        public static IBehavior GetBehavior(string id)
        {
            return s_behaviors.GetValueOrDefault(id, null);
        }
        
        // 獲取所有玩家行為
        public static IEnumerable<IBehavior> GetPlayerBehaviors()
        {
            foreach (var behavior in s_behaviors.Values)
            {
                if (behavior.Category == BehaviorCategory.PLAYER_ACTION)
                    yield return behavior;
            }
        }
        
        // 獲取所有敵人行為
        public static IEnumerable<IBehavior> GetEnemyBehaviors()
        {
            foreach (var behavior in s_behaviors.Values)
            {
                if (behavior.Category == BehaviorCategory.ENEMY_INTENT)
                    yield return behavior;
            }
        }
    }
    
    // 智能敵人AI - 基於行為的決策系統
    public static class BehaviorBasedAI
    {
        private static readonly Random s_random = new();
        
        // 為敵人選擇行為
        public static IBehavior SelectBehaviorForEnemy(byte enemyId)
        {
            if (!ActorManager.IsAlive(enemyId))
                return null;
            
            ref var enemy = ref ActorManager.GetActor(enemyId);
            
            // 根據敵人類型和狀態選擇行為
            return enemy.Type switch
            {
                ActorType.ENEMY_BASIC => SelectBasicEnemyBehavior(in enemy),
                ActorType.ENEMY_ELITE => SelectEliteEnemyBehavior(in enemy),
                ActorType.ENEMY_BOSS => SelectBossEnemyBehavior(in enemy),
                _ => BehaviorRegistry.GetBehavior("enemy_basic_attack")
            };
        }
        
        private static IBehavior SelectBasicEnemyBehavior(in Actor enemy)
        {
            // 血量低時傾向防禦
            if (enemy.HP < enemy.MaxHP / 3)
            {
                return s_random.NextDouble() < 0.6 
                    ? BehaviorRegistry.GetBehavior("enemy_defensive") 
                    : BehaviorRegistry.GetBehavior("enemy_basic_attack");
            }
            
            // 正常血量時隨機選擇
            return s_random.NextDouble() < 0.7
                ? BehaviorRegistry.GetBehavior("enemy_basic_attack")
                : BehaviorRegistry.GetBehavior("enemy_defensive");
        }
        
        private static IBehavior SelectEliteEnemyBehavior(in Actor enemy)
        {
            // 精英敵人更傾向激進
            if (enemy.HP < enemy.MaxHP / 2)
            {
                return s_random.NextDouble() < 0.8
                    ? BehaviorRegistry.GetBehavior("enemy_aggressive")
                    : BehaviorRegistry.GetBehavior("enemy_defensive");
            }
            
            return s_random.NextDouble() < 0.6
                ? BehaviorRegistry.GetBehavior("enemy_aggressive")
                : BehaviorRegistry.GetBehavior("enemy_basic_attack");
        }
        
        private static IBehavior SelectBossEnemyBehavior(in Actor enemy)
        {
            // BOSS有更複雜的行為模式
            if (enemy.HP < enemy.MaxHP / 4)
            {
                // 血量極低時狂暴
                return BehaviorRegistry.GetBehavior("enemy_aggressive");
            }
            
            // 根據回合數等因素調整行為...
            return s_random.NextDouble() switch
            {
                < 0.4 => BehaviorRegistry.GetBehavior("enemy_aggressive"),
                < 0.7 => BehaviorRegistry.GetBehavior("enemy_basic_attack"),
                _ => BehaviorRegistry.GetBehavior("enemy_defensive")
            };
        }
    }
    
    // 行為執行器 - 統一的行為執行接口
    public static class BehaviorExecutor
    {
        // 執行玩家行為（從卡牌系統調用）
        public static bool ExecutePlayerBehavior(BasicAction action, byte playerId, byte targetId)
        {
            string behaviorId = action switch
            {
                BasicAction.ATTACK => "player_attack",
                BasicAction.BLOCK => "player_block",
                BasicAction.CHARGE => "player_charge",
                _ => "player_attack"
            };
            
            var behavior = BehaviorRegistry.GetBehavior(behaviorId);
            return behavior?.Execute(playerId, targetId) ?? false;
        }
        
        // 執行敵人行為
        public static bool ExecuteEnemyBehavior(IBehavior behavior, byte enemyId, byte targetId)
        {
            if (behavior == null || !ActorManager.IsAlive(enemyId))
                return false;
                
            return behavior.Execute(enemyId, targetId);
        }
        
        // 為所有敵人決策並記錄意圖
        public static void DecideAllEnemyIntents()
        {
            Span<byte> enemyBuffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            int enemyCount = GetAllEnemies(enemyBuffer);
            
            for (int i = 0; i < enemyCount; i++)
            {
                byte enemyId = enemyBuffer[i];
                var behavior = BehaviorBasedAI.SelectBehaviorForEnemy(enemyId);
                
                if (behavior != null)
                {
                    // 將行為轉換為HLA並儲存（保持與現有系統兼容）
                    var hla = ConvertBehaviorToHLA(behavior);
                    HLASystem.SetEnemyHLA(enemyId, hla);
                    
                    // 處理立即效果
                    behavior.Execute(enemyId, GetPlayerTarget());
                }
            }
        }
        
        // 輔助函數：獲取所有敵人
        private static int GetAllEnemies(Span<byte> buffer)
        {
            int count = 0;
            count += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, buffer[count..]);
            count += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, buffer[count..]);
            count += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, buffer[count..]);
            return count;
        }
        
        // 輔助函數：獲取玩家目標
        private static byte GetPlayerTarget()
        {
            Span<byte> playerBuffer = stackalloc byte[16];
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, playerBuffer);
            return playerCount > 0 ? playerBuffer[0] : (byte)0;
        }
        
        // 輔助函數：行為轉HLA（保持兼容性）
        private static HLA ConvertBehaviorToHLA(IBehavior behavior)
        {
            return behavior.Name switch
            {
                "激進攻擊" => HLA.ENEMY_AGGRESSIVE,
                "防禦姿態" => HLA.ENEMY_DEFENSIVE,
                "基礎攻擊" => HLA.BASIC_ATTACK,
                _ => HLA.BASIC_ATTACK
            };
        }
        
        // 除錯：顯示所有敵人意圖
        public static void DebugPrintEnemyIntents()
        {
            Console.WriteLine("=== 敵人意圖 (行為系統) ===");
            
            Span<byte> enemyBuffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            int enemyCount = GetAllEnemies(enemyBuffer);
            
            for (int i = 0; i < enemyCount; i++)
            {
                byte enemyId = enemyBuffer[i];
                var behavior = BehaviorBasedAI.SelectBehaviorForEnemy(enemyId);
                
                if (behavior != null)
                {
                    string intent = behavior.GetIntentDescription(enemyId);
                    Console.WriteLine($"  敵人 {enemyId}: {intent}");
                }
            }
        }
    }
}