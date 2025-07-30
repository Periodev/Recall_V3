// CombatSystemUsageGuide.cs - 統一戰鬥系統使用指南
// 展示如何正確使用現有的所有檔案

using System;
using CombatCore;

namespace CombatCoreUsage
{
    // 推薦的戰鬥管理器 - 統一所有系統
    public static class UnifiedCombatManager
    {
        // 戰鬥初始化 - 一次性設置
        public static void InitializeCombat()
        {
            Console.WriteLine("=== 初始化統一戰鬥系統 ===");
            
            // 1. 重置所有底層系統
            ActorManager.Reset();
            CommandSystem.Clear();
            ReactionSystem.Reset();
            PhaseSystem.Initialize();
            
            // 2. 初始化整合HLA管理器（關鍵！）
            IntegratedHLAManager.Initialize();
            
            // 3. 創建戰鬥參與者
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemy1 = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 60);
            byte enemy2 = ActorManager.AllocateActor(ActorType.ENEMY_ELITE, 80);
            
            // 4. 設置玩家牌組
            SimpleDeckManager.SetDeckConfig(DeckConfig.DEFAULT);
            SimpleDeckManager.StartCombat();
            
            Console.WriteLine($"✅ 戰鬥初始化完成");
            Console.WriteLine($"   玩家: {playerId}, 敵人: {enemy1}, {enemy2}");
        }
        
        // 執行完整回合
        public static CombatResult ExecuteTurn()
        {
            Console.WriteLine("\n" + "=".PadRight(50, '='));
            Console.WriteLine("開始新回合");
            
            // 檢查戰鬥是否結束
            if (IsCombatEnded())
            {
                return GetCombatResult();
            }
            
            // 1. 敵人意圖階段
            ExecuteEnemyIntentPhase();
            
            // 2. 玩家階段  
            ExecutePlayerPhase();
            
            // 3. 敵人執行階段
            ExecuteEnemyExecutionPhase();
            
            // 4. 清理階段
            ExecuteCleanupPhase();
            
            return CombatResult.ONGOING;
        }
        
        // 1. 敵人意圖階段 - 使用整合系統
        private static void ExecuteEnemyIntentPhase()
        {
            Console.WriteLine("\n📋 敵人意圖階段");
            
            // 🎯 關鍵：使用整合HLA管理器
            IntegratedHLAManager.DecideAndProcessAllEnemyIntents();
            
            // 顯示意圖給玩家
            IntegratedHLAManager.DebugPrintAllEnemyIntents();
        }
        
        // 2. 玩家階段 - 使用簡化卡牌系統
        private static void ExecutePlayerPhase()
        {
            Console.WriteLine("\n🎮 玩家階段");
            
            // 檢查是否需要重洗
            if (SimpleDeckManager.GetHandSize() == 0)
            {
                Console.WriteLine("手牌為空，重新洗牌");
                SimpleDeckManager.ShuffleAndDrawAll();
            }
            
            // 顯示手牌
            SimpleDeckManager.DebugPrintHand();
            
            // 🎯 關鍵：使用自動玩牌（或者手動選擇）
            AutoPlayPlayerTurn();
        }
        
        // 自動玩牌邏輯
        private static void AutoPlayPlayerTurn()
        {
            Console.WriteLine("自動玩牌開始...");
            
            // 簡單策略：優先攻擊，然後蓄力，最後格擋
            while (SimpleDeckManager.GetHandSize() > 0)
            {
                bool played = false;
                
                // 1. 優先攻擊
                byte target = GetFirstAliveEnemy();
                if (target != 0 && TryPlayCard(BasicAction.ATTACK, target))
                {
                    played = true;
                }
                // 2. 然後蓄力
                else if (TryPlayCard(BasicAction.CHARGE))
                {
                    played = true;
                }
                // 3. 最後格擋
                else if (TryPlayCard(BasicAction.BLOCK))
                {
                    played = true;
                }
                
                if (!played)
                {
                    Console.WriteLine("無法繼續玩牌，強制使用第一張");
                    SimpleCardHelper.PlayCard(0);
                }
                
                // 執行命令
                CommandSystem.ExecuteAll();
            }
        }
        
        // 嘗試使用指定類型的卡牌
        private static bool TryPlayCard(BasicAction action, byte targetId = 0)
        {
            return SimpleCardHelper.PlayCardByType(action, targetId);
        }
        
        // 3. 敵人執行階段 - 觸發延後效果
        private static void ExecuteEnemyExecutionPhase()
        {
            Console.WriteLine("\n👹 敵人執行階段");
            
            // 觸發延後效果（攻擊等）
            ReactionEventDispatcher.OnEnemyPhaseStart();
            CommandSystem.ExecuteAll();
        }
        
        // 4. 清理階段
        private static void ExecuteCleanupPhase()
        {
            Console.WriteLine("\n🧹 清理階段");
            
            // 回合結束處理
            ReactionEventDispatcher.OnTurnEnd(1);
            CommandSystem.PushCmd(AtomicCmd.TurnEndCleanup());
            CommandSystem.ExecuteAll();
            
            // 檢查牌組重洗
            SimpleDeckManager.OnTurnEnd();
        }
        
        // 輔助函數
        private static byte GetFirstAliveEnemy()
        {
            Span<byte> enemyBuffer = stackalloc byte[16];
            int enemyCount = 0;
            
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, enemyBuffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, enemyBuffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, enemyBuffer[enemyCount..]);
            
            return enemyCount > 0 ? enemyBuffer[0] : (byte)0;
        }
        
        private static bool IsCombatEnded()
        {
            Span<byte> buffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            
            // 檢查玩家
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, buffer);
            if (playerCount == 0) return true;
            
            // 檢查敵人
            int enemyCount = 0;
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, buffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, buffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, buffer[enemyCount..]);
            
            return enemyCount == 0;
        }
        
        private static CombatResult GetCombatResult()
        {
            Span<byte> buffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, buffer);
            if (playerCount == 0) return CombatResult.DEFEAT;
            
            int enemyCount = 0;
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, buffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, buffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, buffer[enemyCount..]);
            
            return enemyCount == 0 ? CombatResult.VICTORY : CombatResult.ONGOING;
        }
        
        // 顯示戰鬥狀態
        public static void PrintCombatStatus()
        {
            Console.WriteLine("\n=== 戰鬥狀態 ===");
            
            Span<byte> actorBuffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            
            // 玩家狀態
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, actorBuffer);
            Console.WriteLine("玩家:");
            for (int i = 0; i < playerCount; i++)
            {
                ref var player = ref ActorManager.GetActor(actorBuffer[i]);
                Console.WriteLine($"  ID{player.Id}: {player.HP}/{player.MaxHP}HP, {player.Block}Block, {player.Charge}Charge");
            }
            
            // 敵人狀態
            int enemyCount = 0;
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, actorBuffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, actorBuffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, actorBuffer[enemyCount..]);
            
            Console.WriteLine("敵人:");
            for (int i = 0; i < enemyCount; i++)
            {
                ref var enemy = ref ActorManager.GetActor(actorBuffer[i]);
                Console.WriteLine($"  ID{enemy.Id}({enemy.Type}): {enemy.HP}/{enemy.MaxHP}HP, {enemy.Block}Block, {enemy.Charge}Charge");
            }
            
            Console.WriteLine($"手牌: {SimpleDeckManager.GetHandSize()}/{SimpleDeckManager.GetDeckSize()}");
            Console.WriteLine("==================\n");
        }
    }
    
    // 戰鬥結果枚舉
    public enum CombatResult
    {
        ONGOING,    // 進行中
        VICTORY,    // 勝利
        DEFEAT,     // 敗北
        ERROR       // 錯誤
    }
    
    // 使用示例
    public static class ExampleUsage
    {
        public static void RunExampleCombat()
        {
            Console.WriteLine("=== 統一戰鬥系統使用示例 ===\n");
            
            // 1. 初始化戰鬥
            UnifiedCombatManager.InitializeCombat();
            UnifiedCombatManager.PrintCombatStatus();
            
            // 2. 執行戰鬥回合
            for (int turn = 1; turn <= 10; turn++)
            {
                Console.WriteLine($"\n### 回合 {turn} ###");
                
                var result = UnifiedCombatManager.ExecuteTurn();
                UnifiedCombatManager.PrintCombatStatus();
                
                if (result != CombatResult.ONGOING)
                {
                    Console.WriteLine($"戰鬥結束！結果: {result}");
                    break;
                }
            }
        }
        
        // 手動玩牌示例
        public static void ManualPlayExample()
        {
            Console.WriteLine("=== 手動玩牌示例 ===\n");
            
            UnifiedCombatManager.InitializeCombat();
            
            // 手動選擇卡牌
            SimpleDeckManager.DebugPrintHand();
            
            byte enemyTarget = ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, stackalloc byte[16]) > 0 
                ? (byte)1 : (byte)0;
            
            // 使用第一張攻擊卡
            bool success = SimpleCardHelper.PlayCardByType(BasicAction.ATTACK, enemyTarget);
            Console.WriteLine($"攻擊結果: {success}");
            CommandSystem.ExecuteAll();
            
            UnifiedCombatManager.PrintCombatStatus();
        }
    }
}