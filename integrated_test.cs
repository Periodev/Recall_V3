// IntegratedSystemTest.cs - 整合測試：卡牌系統 + 抽象化HLA
// 展示新系統如何與現有戰鬥系統協同工作

using System;
using CombatCore;

namespace CombatCoreTest
{
    public static class IntegratedSystemTest
    {
        public static void RunCardBasedCombatTest()
        {
            Console.WriteLine("=== 整合測試：卡牌驅動戰鬥系統 ===\n");
            
            // 1. 初始化系統
            InitializeIntegratedSystems();
            
            // 2. 測試卡牌抽取和顯示
            TestCardDrawingSystem();
            
            // 3. 測試行為抽象化
            TestBehaviorAbstraction();
            
            // 4. 完整回合制戰鬥（卡牌驅動）
            TestCardBasedCombatLoop();
            
            Console.WriteLine("\n✅ 整合測試完成");
        }
        
        private static void InitializeIntegratedSystems()
        {
            Console.WriteLine("=== 初始化整合系統 ===");
            
            // 重置所有系統
            ActorManager.Reset();
            CommandSystem.Clear();
            ReactionSystem.Reset();
            PhaseSystem.Initialize();
            
            // 創建戰鬥參與者
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemy1 = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 60);
            byte enemy2 = ActorManager.AllocateActor(ActorType.ENEMY_ELITE, 80);
            
            Console.WriteLine($"玩家 ID: {playerId}");
            Console.WriteLine($"基礎敵人 ID: {enemy1}");
            Console.WriteLine($"精英敵人 ID: {enemy2}");
            
            // 設置初始牌組並開始戰鬥
            SimpleDeckManager.SetDeckConfig(DeckConfig.DEFAULT);
            SimpleDeckManager.StartCombat();
            
            Console.WriteLine("系統初始化完成\n");
        }
        
        private static void TestCardDrawingSystem()
        {
            Console.WriteLine("=== 測試簡化卡牌系統 ===");
            
            // 顯示預設牌組配置
            SimpleDeckManager.DebugPrintDeckConfig();
            SimpleDeckManager.DebugPrintDeck();
            
            // 戰鬥開始，洗牌抽滿
            Console.WriteLine("\n戰鬥開始，洗牌並抽滿手牌:");
            SimpleDeckManager.StartCombat();
            SimpleDeckManager.DebugPrintHand();
            
            // 測試不同牌組配置
            Console.WriteLine("\n=== 測試不同牌組配置 ===");
            
            // 激進配置
            Console.WriteLine("\n切換到激進配置 (3A2B1C):");
            SimpleDeckManager.SetDeckConfig(DeckConfig.AGGRESSIVE);
            SimpleDeckManager.StartCombat();
            SimpleDeckManager.DebugPrintHand();
            
            // 防禦配置
            Console.WriteLine("\n切換到防禦配置 (1A3B2C):");
            SimpleDeckManager.SetDeckConfig(DeckConfig.DEFENSIVE);
            SimpleDeckManager.StartCombat();
            SimpleDeckManager.DebugPrintHand();
            
            // 恢復預設配置
            Console.WriteLine("\n恢復預設配置 (1A1B1C):");
            SimpleDeckManager.SetDeckConfig(DeckConfig.DEFAULT);
            SimpleDeckManager.StartCombat();
            SimpleDeckManager.DebugPrintHand();
            
            Console.WriteLine("簡化卡牌系統測試完成\n");
        }
        
        private static void TestBehaviorAbstraction()
        {
            Console.WriteLine("=== 測試行為抽象化系統 ===");
            
            // 測試玩家行為
            Console.WriteLine("玩家可用行為:");
            foreach (var behavior in BehaviorRegistry.GetPlayerBehaviors())
            {
                Console.WriteLine($"  - {behavior.Name}: {behavior.Description}");
            }
            
            // 測試敵人行為決策
            Console.WriteLine("\n敵人行為決策:");
            Span<byte> enemyBuffer = stackalloc byte[16];
            int enemyCount = 0;
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, enemyBuffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, enemyBuffer[enemyCount..]);
            
            for (int i = 0; i < enemyCount; i++)
            {
                byte enemyId = enemyBuffer[i];
                var behavior = BehaviorBasedAI.SelectBehaviorForEnemy(enemyId);
                ref var enemy = ref ActorManager.GetActor(enemyId);
                
                Console.WriteLine($"  敵人 {enemyId} ({enemy.Type}): {behavior?.Name ?? "無行為"}");
                if (behavior != null)
                {
                    Console.WriteLine($"    意圖: {behavior.GetIntentDescription(enemyId)}");
                }
            }
            
            Console.WriteLine("行為抽象化測試完成\n");
        }
        
        private static void TestCardBasedCombatLoop()
        {
            Console.WriteLine("=== 卡牌驅動戰鬥循環測試 ===");
            
            for (int turn = 1; turn <= 5; turn++)
            {
                Console.WriteLine($"\n--- 回合 {turn} ---");
                
                // 檢查戰鬥是否結束
                if (IsCombatEnded())
                {
                    Console.WriteLine("戰鬥結束!");
                    PrintCombatResult();
                    break;
                }
                
                // 1. 敵人意圖階段 (使用行為系統)
                ExecuteEnemyIntentPhase();
                
                // 2. 玩家階段 (使用卡牌系統)
                ExecutePlayerPhase();
                
                // 3. 敵人執行階段
                ExecuteEnemyExecutionPhase();
                
                // 4. 清理階段
                ExecuteCleanupPhase();
                
                // 顯示回合結束狀態
                PrintCombatStatus();
            }
            
            Console.WriteLine("戰鬥循環測試完成\n");
        }
        
        private static void ExecuteEnemyIntentPhase()
        {
            Console.WriteLine("\n📋 敵人意圖階段:");
            
            // 使用新的行為系統決策
            BehaviorExecutor.DecideAllEnemyIntents();
            
            // 顯示敵人意圖
            BehaviorExecutor.DebugPrintEnemyIntents();
            
            // 執行所有立即效果命令
            CommandSystem.ExecuteAll();
        }
        
        private static void ExecutePlayerPhase()
        {
            Console.WriteLine("\n🎮 玩家階段:");
            
            // 如果手牌為空，重新洗牌
            if (SimpleDeckManager.GetHandSize() == 0)
            {
                Console.WriteLine("手牌為空，重新洗牌");
                SimpleDeckManager.ShuffleAndDrawAll();
            }
            
            // 顯示當前手牌
            SimpleDeckManager.DebugPrintHand();
            
            // 自動玩牌AI
            SimpleCardHelper.AutoPlayTurn();
        }
        
        private static void AutoPlayPlayerCards()
        {
            // 簡單的自動玩家AI：優先攻擊，然後防禦
            var hand = HandManager.GetHand();
            
            if (hand.Length == 0)
            {
                Console.WriteLine("手牌為空，跳過玩家回合");
                return;
            }
            
            // 尋找攻擊卡
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i].Action == BasicAction.ATTACK)
                {
                    byte target = CardPlayHelper.GetDefaultEnemyTarget();
                    if (target != 0)
                    {
                        CardPlayHelper.PlayCard(i, target);
                        break;
                    }
                }
            }
            
            // 如果還有手牌，使用第一張非攻擊卡
            var remainingHand = HandManager.GetHand();
            if (remainingHand.Length > 0)
            {
                for (int i = 0; i < remainingHand.Length; i++)
                {
                    if (remainingHand[i].Action != BasicAction.ATTACK)
                    {
                        CardPlayHelper.PlayCard(i);
                        break;
                    }
                }
            }
        }
        
        private static void ExecuteEnemyExecutionPhase()
        {
            Console.WriteLine("\n👹 敵人執行階段:");
            
            // 觸發延後效果 (攻擊等)
            ReactionEventDispatcher.OnEnemyPhaseStart();
            CommandSystem.ExecuteAll();
        }
        
        private static void ExecuteCleanupPhase()
        {
            Console.WriteLine("\n🧹 清理階段:");
            
            // 觸發回合結束事件
            ReactionEventDispatcher.OnTurnEnd(1);
            CommandSystem.PushCmd(AtomicCmd.TurnEndCleanup());
            CommandSystem.ExecuteAll();
            
            // 檢查是否需要重洗牌組
            SimpleDeckManager.OnTurnEnd();
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
        
        private static void PrintCombatResult()
        {
            Span<byte> buffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, buffer);
            if (playerCount == 0)
            {
                Console.WriteLine("戰鬥結果: 玩家敗北");
                return;
            }
            
            int enemyCount = 0;
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, buffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, buffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, buffer[enemyCount..]);
            
            if (enemyCount == 0)
            {
                Console.WriteLine("戰鬥結果: 玩家勝利");
            }
            else
            {
                Console.WriteLine("戰鬥結果: 未決");
            }
        }
        
        private static void PrintCombatStatus()
        {
            Console.WriteLine("\n=== 戰鬥狀態 ===");
            
            // 使用stackalloc獲取Actor列表
            Span<byte> actorBuffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            
            // 顯示玩家狀態
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, actorBuffer);
            Console.WriteLine("玩家:");
            for (int i = 0; i < playerCount; i++)
            {
                ref var player = ref ActorManager.GetActor(actorBuffer[i]);
                Console.WriteLine($"  玩家{player.Id}: {player.HP}/{player.MaxHP}HP, {player.Block}Block, {player.Charge}Charge");
            }
            
            // 顯示敵人狀態
            int enemyCount = 0;
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, actorBuffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, actorBuffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, actorBuffer[enemyCount..]);
            
            Console.WriteLine("敵人:");
            for (int i = 0; i < enemyCount; i++)
            {
                ref var enemy = ref ActorManager.GetActor(actorBuffer[i]);
                Console.WriteLine($"  敵人{enemy.Id}: {enemy.HP}/{enemy.MaxHP}HP, {enemy.Block}Block, {enemy.Charge}Charge");
            }
            
            // 顯示手牌狀態
            Console.WriteLine($"手牌數: {SimpleDeckManager.GetHandSize()}/{SimpleDeckManager.GetDeckSize()}");
            Console.WriteLine("==================");
        }
        
        // 簡化卡牌系統的專門測試
        public static void TestSimplifiedCardSystem()
        {
            Console.WriteLine("=== 簡化卡牌系統專門測試 ===");
            
            // 初始化
            ActorManager.Reset();
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemyId = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 50);
            
            Console.WriteLine("=== 測試1: 預設配置戰鬥 ===");
            SimpleDeckManager.SetDeckConfig(DeckConfig.DEFAULT);
            SimpleDeckManager.StartCombat();
            
            Console.WriteLine("戰鬥前狀態:");
            PrintActorStatus(playerId, "玩家");
            PrintActorStatus(enemyId, "敵人");
            SimpleDeckManager.DebugPrintHand();
            
            // 手動使用卡牌
            Console.WriteLine("\n手動使用攻擊卡:");
            SimpleCardHelper.PlayCardByType(BasicAction.ATTACK, enemyId);
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("\n手動使用格擋卡:");
            SimpleCardHelper.PlayCardByType(BasicAction.BLOCK);
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("\n戰鬥後狀態:");
            PrintActorStatus(playerId, "玩家");
            PrintActorStatus(enemyId, "敵人");
            SimpleDeckManager.DebugPrintHand();
            
            Console.WriteLine("\n=== 測試2: 手牌用完重洗 ===");
            
            // 用完剩餘手牌
            while (SimpleDeckManager.GetHandSize() > 0)
            {
                SimpleCardHelper.PlayCard(0);
                CommandSystem.ExecuteAll();
            }
            
            Console.WriteLine("手牌用完:");
            SimpleDeckManager.DebugPrintHand();
            
            // 觸發重洗
            SimpleDeckManager.OnTurnEnd();
            Console.WriteLine("重洗後:");
            SimpleDeckManager.DebugPrintHand();
            
            Console.WriteLine("\n=== 測試3: 不同牌組配置 ===");
            
            string[] configNames = { "DEFAULT", "BALANCED", "AGGRESSIVE", "DEFENSIVE" };
            DeckConfig[] configs = { DeckConfig.DEFAULT, DeckConfig.BALANCED, DeckConfig.AGGRESSIVE, DeckConfig.DEFENSIVE };
            
            for (int i = 0; i < configs.Length; i++)
            {
                Console.WriteLine($"\n{configNames[i]} 配置:");
                SimpleDeckManager.SetDeckConfig(configs[i]);
                SimpleDeckManager.StartCombat();
                SimpleDeckManager.DebugPrintDeckConfig();
                SimpleDeckManager.DebugPrintHand();
            }
            
            Console.WriteLine("\n=== 測試4: 自動玩牌AI ===");
            SimpleDeckManager.SetDeckConfig(DeckConfig.BALANCED);
            SimpleDeckManager.StartCombat();
            
            Console.WriteLine("自動玩牌前:");
            PrintActorStatus(playerId, "玩家");
            PrintActorStatus(enemyId, "敵人");
            SimpleDeckManager.DebugPrintHand();
            
            SimpleCardHelper.AutoPlayTurn();
            
            Console.WriteLine("自動玩牌後:");
            PrintActorStatus(playerId, "玩家");
            PrintActorStatus(enemyId, "敵人");
            SimpleDeckManager.DebugPrintHand();
            
            Console.WriteLine("✅ 簡化卡牌系統測試完成\n");
        }
        
        private static void PrintActorStatus(byte actorId, string name)
        {
            if (!ActorManager.IsAlive(actorId))
            {
                Console.WriteLine($"  {name}: 已死亡");
                return;
            }
            
            ref var actor = ref ActorManager.GetActor(actorId);
            Console.WriteLine($"  {name}: {actor.HP}/{actor.MaxHP}HP, {actor.Block}Block, {actor.Charge}Charge");
        }
    }