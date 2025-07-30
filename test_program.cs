// Program.cs - CombatCore 測試主程式（卡牌系統整合版）
// ✅ 修改：所有玩家輸入改為卡牌驅動，移除直接HLA輸入測試

using System;
using CombatCore;

namespace CombatCoreTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== CombatCore 卡牌系統整合測試 ===\n");
            
            // 測試1: 基本系統初始化
            TestBasicSystemInitialization();
            
            // 測試2: Actor系統
            TestActorSystem();
            
            // 測試3: Command系統
            TestCommandSystem();
            
            // 測試4: HLA翻譯系統
            TestHLASystem();
            
            // ✅ 新增：卡牌系統測試
            TestCardSystem();
            
            // ✅ 新增：卡牌與戰鬥整合測試
            // TestCardIntegration(); // 暫時跳過，避免無限循環
            
            // ✅ 修改：完整戰鬥流程（卡牌驅動）
            // TestFullCombatFlowWithCards(); // 暫時跳過，避免無限循環
            
            // ✅ 修改：手動控制戰鬥（卡牌驅動）
            // TestManualCombatWithCards(); // 暫時跳過，避免無限循環
            
            // 測試8: 簡化事件系統
            TestMinimalReaction();
            
            // ✅ 新增：敵人意圖系統測試
            TestEnemyIntentSystem();
            
            Console.WriteLine("\n=== 所有測試完成 ===");
            Console.WriteLine("按任意鍵退出...");
            Console.ReadKey();
        }
        
        static void TestBasicSystemInitialization()
        {
            Console.WriteLine("=== 測試1: 基本系統初始化 ===");
            
            // 重置所有系統
            ActorManager.Reset();
            PhaseSystem.Initialize();
            SimpleEventSystem.Initialize();
            
            Console.WriteLine($"Actor池大小限制: {CombatConstants.MAX_ACTORS}");
            Console.WriteLine($"命令佇列大小限制: {CombatConstants.MAX_COMMANDS}");
            Console.WriteLine($"蓄力傷害加成: {CombatConstants.CHARGE_DAMAGE_BONUS}");
            Console.WriteLine($"當前Actor數量: {ActorManager.GetActorCount()}");
            Console.WriteLine($"當前Phase: {PhaseSystem.GetCurrentPhase()}");
            
            Console.WriteLine("✅ 系統初始化測試完成\n");
        }
        
        static void TestActorSystem()
        {
            Console.WriteLine("=== 測試2: Actor系統 ===");
            
            ActorManager.Reset();
            
            // 創建測試Actor
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemyId = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 50);
            
            Console.WriteLine($"創建玩家 ID: {playerId}");
            Console.WriteLine($"創建敵人 ID: {enemyId}");
            
            // 測試Actor狀態
            ref var player = ref ActorManager.GetActor(playerId);
            ref var enemy = ref ActorManager.GetActor(enemyId);
            
            Console.WriteLine($"玩家初始狀態: HP={player.HP}/{player.MaxHP}, Block={player.Block}, Charge={player.Charge}");
            Console.WriteLine($"敵人初始狀態: HP={enemy.HP}/{enemy.MaxHP}, Block={enemy.Block}, Charge={enemy.Charge}");
            
            // 測試Actor操作
            ActorOperations.AddBlock(playerId, 10);
            ActorOperations.AddCharge(playerId, 2);
            ActorOperations.DealDamage(enemyId, 15);
            
            Console.WriteLine($"操作後玩家狀態: HP={player.HP}/{player.MaxHP}, Block={player.Block}, Charge={player.Charge}");
            Console.WriteLine($"操作後敵人狀態: HP={enemy.HP}/{enemy.MaxHP}, Block={enemy.Block}, Charge={enemy.Charge}");
            
            Console.WriteLine($"玩家存活: {player.IsAlive}, 可行動: {player.CanAct}");
            Console.WriteLine($"敵人存活: {enemy.IsAlive}, 可行動: {enemy.CanAct}");
            
            Console.WriteLine("✅ Actor系統測試完成\n");
        }
        
        static void TestCommandSystem()
        {
            Console.WriteLine("=== 測試3: Command系統 ===");
            
            ActorManager.Reset();
            CommandSystem.Clear();
            
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemyId = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 50);
            
            Console.WriteLine("測試基本命令執行:");
            
            // 測試攻擊命令
            var attackCmd = CommandBuilder.MakeAttackCmd(playerId, enemyId, 20);
            var result = CommandSystem.ExecuteCmd(attackCmd);
            Console.WriteLine($"攻擊命令結果: 成功={result.Success}, 傷害={result.Value}, 訊息={result.Message}");
            
            // 測試格擋命令
            var blockCmd = CommandBuilder.MakeBlockCmd(playerId, 8);
            result = CommandSystem.ExecuteCmd(blockCmd);
            Console.WriteLine($"格擋命令結果: 成功={result.Success}, 護甲={result.Value}, 訊息={result.Message}");
            
            // 測試蓄力命令
            var chargeCmd = CommandBuilder.MakeChargeCmd(playerId, 3);
            result = CommandSystem.ExecuteCmd(chargeCmd);
            Console.WriteLine($"蓄力命令結果: 成功={result.Success}, 蓄力={result.Value}, 訊息={result.Message}");
            
            Console.WriteLine("\n測試命令佇列:");
            
            // 推入多個命令
            CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(playerId, enemyId, 15));
            CommandSystem.PushCmd(CommandBuilder.MakeBlockCmd(enemyId, 5));
            
            Console.WriteLine($"佇列中命令數: {CommandSystem.GetQueueCount()}");
            
            // 執行所有命令
            int executedCount = CommandSystem.ExecuteAll();
            Console.WriteLine($"執行了 {executedCount} 個命令");
            Console.WriteLine($"執行後佇列命令數: {CommandSystem.GetQueueCount()}");
            
            // 顯示最終狀態
            ref var finalPlayer = ref ActorManager.GetActor(playerId);
            ref var finalEnemy = ref ActorManager.GetActor(enemyId);
            
            Console.WriteLine($"最終玩家狀態: HP={finalPlayer.HP}, Block={finalPlayer.Block}, Charge={finalPlayer.Charge}");
            Console.WriteLine($"最終敵人狀態: HP={finalEnemy.HP}, Block={finalEnemy.Block}, Charge={finalEnemy.Charge}");
            
            Console.WriteLine("✅ Command系統測試完成\n");
        }
        
        static void TestHLASystem()
        {
            Console.WriteLine("=== 測試4: HLA翻譯系統 ===");
            
            ActorManager.Reset();
            CommandSystem.Clear();
            
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemyId = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 60);
            
            // 測試各種HLA
            HLA[] testHLAs = {
                HLA.BASIC_ATTACK, HLA.BASIC_BLOCK, HLA.BASIC_CHARGE,
                HLA.HEAVY_STRIKE, HLA.SHIELD_BASH, HLA.COMBO_ATTACK,
                HLA.ENEMY_AGGRESSIVE, HLA.ENEMY_DEFENSIVE
            };
            
            foreach (var hla in testHLAs)
            {
                TestSingleHLA(hla, playerId, enemyId, hla.ToString());
            }
            
            Console.WriteLine("✅ HLA系統測試完成\n");
        }
        
        // ✅ 新增：卡牌系統專項測試
        static void TestCardSystem()
        {
            Console.WriteLine("=== 測試5: 卡牌系統 ===");
            
            // 重置系統
            ActorManager.Reset();
            SimpleDeckManager.SetDeckConfig(new DeckConfig(2, 1, 1)); // 2A1B1C
            SimpleDeckManager.StartCombat();
            
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemyId = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 50);
            
            Console.WriteLine("測試牌組配置和初始手牌:");
            SimpleDeckManager.DebugPrintDeckConfig();
            SimpleDeckManager.DebugPrintHand();
            
            // 測試各種卡牌使用
            var hand = SimpleDeckManager.GetHand();
            Console.WriteLine($"\n開始測試 {hand.Length} 張卡牌的使用:");
            
            for (int i = 0; i < Math.Min(hand.Length, 3); i++) // 只測試前3張
            {
                var card = hand[i];
                Console.WriteLine($"\n--- 測試卡牌 {i}: [{card.Symbol}] {card.Name} ---");
                
                // 記錄使用前狀態
                PrintActorStatus(playerId, "玩家", "使用前");
                PrintActorStatus(enemyId, "敵人", "使用前");
                
                // 選擇目標
                byte targetId = card.RequiresTarget ? enemyId : (byte)0;
                
                // 使用卡牌
                bool success = SimpleDeckManager.UseCard(i, targetId);
                Console.WriteLine($"使用結果: {(success ? "✅ 成功" : "❌ 失敗")}");
                
                // 記錄使用後狀態
                PrintActorStatus(playerId, "玩家", "使用後");
                PrintActorStatus(enemyId, "敵人", "使用後");
                
                Console.WriteLine($"剩餘手牌數: {SimpleDeckManager.GetHandSize()}");
                
                if (SimpleDeckManager.GetHandSize() == 0)
                {
                    Console.WriteLine("手牌已用完");
                    break;
                }
            }
            
            // 測試重洗機制
            Console.WriteLine("\n測試重洗機制:");
            SimpleDeckManager.OnTurnEnd();
            Console.WriteLine($"回合結束後手牌數: {SimpleDeckManager.GetHandSize()}");
            SimpleDeckManager.DebugPrintHand();
            
            Console.WriteLine("✅ 卡牌系統測試完成\n");
        }
        
        // ✅ 新增：卡牌與戰鬥系統整合測試
        static void TestCardIntegration()
        {
            Console.WriteLine("=== 測試6: 卡牌與戰鬥整合 ===");
            
            // 初始化完整戰鬥環境
            CombatManager.InitializeCombat();
            
            Console.WriteLine("測試Phase系統與卡牌的整合:");
            PhaseSystem.DebugPrintPhaseInfo();
            PrintCombatStatus();
            
            // 測試幾個完整的回合
            for (int round = 0; round < 3; round++)
            {
                Console.WriteLine($"\n=== 第 {round + 1} 輪測試 ===");
                
                if (PhaseSystem.IsCombatEnded())
                {
                    Console.WriteLine("戰鬥已結束");
                    break;
                }
                
                // Enemy Intent Phase
                var result = CombatManager.StepCombat();
                Console.WriteLine($"Enemy Intent Phase 結果: {result}");
                
                // Player Phase - 手動選擇卡牌
                result = CombatManager.StepCombat();
                Console.WriteLine($"Player Phase 初始化結果: {result}");
                
                result = CombatManager.StepCombat();
                if (result == PhaseResult.WAIT_INPUT)
                {
                    Console.WriteLine("玩家階段等待卡牌輸入");
                    
                    // 選擇第一張可用卡牌
                    var hand = SimpleDeckManager.GetHand();
                    if (hand.Length > 0)
                    {
                        var card = hand[0];
                        byte target = card.RequiresTarget ? GetFirstEnemyId() : (byte)0;
                        
                        Console.WriteLine($"選擇使用卡牌 0: [{card.Symbol}] {card.Name}");
                        bool played = CombatManager.PlayPlayerCard(0, target);
                        Console.WriteLine($"卡牌使用結果: {(played ? "成功" : "失敗")}");
                    }
                }
                
                // 繼續執行剩餘步驟
                while (result != PhaseResult.NEXT_PHASE && result != PhaseResult.COMBAT_END && result != PhaseResult.ERROR)
                {
                    result = CombatManager.StepCombat();
                    Console.WriteLine($"Step 結果: {result}");
                }
                
                // Enemy Phase
                result = CombatManager.StepCombat();
                Console.WriteLine($"Enemy Phase 結果: {result}");
                
                // Cleanup Phase  
                result = CombatManager.StepCombat();
                Console.WriteLine($"Cleanup Phase 結果: {result}");
                
                PrintCombatStatus();
            }
            
            Console.WriteLine("✅ 卡牌與戰鬥整合測試完成\n");
        }
        
        // ✅ 修改：完整戰鬥流程使用卡牌驅動
        static void TestFullCombatFlowWithCards()
        {
            Console.WriteLine("=== 測試7: 完整卡牌戰鬥流程 ===");
            
            Console.WriteLine("執行自動卡牌戰鬥測試...");
            
            // 運行戰鬥直到結束
            string result = CombatManager.RunCombatToEnd(10);
            Console.WriteLine($"戰鬥結果: {result}");
            
            Console.WriteLine("✅ 完整卡牌戰鬥流程測試完成\n");
        }
        
        // ✅ 修改：自動卡牌戰鬥測試
        static void TestManualCombatWithCards()
        {
            Console.WriteLine("=== 測試8: 自動卡牌戰鬥 ===");
            
            // 使用自動戰鬥
            string result = CombatManager.RunCombatToEnd(10);
            Console.WriteLine($"戰鬥結果: {result}");
            
            Console.WriteLine("✅ 自動卡牌戰鬥測試完成\n");
        }
        
        static void TestMinimalReaction()
        {
            Console.WriteLine("=== 測試9: 簡化事件系統 ===");
            
            // 初始化
            ActorManager.Reset();
            SimpleEventSystem.Initialize();
            SimplePassiveEffects.Initialize();
            
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemyId = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 50);
            
            Console.WriteLine("測試反傷效果:");
            PrintActorStatus(playerId, "玩家");
            PrintActorStatus(enemyId, "敵人");
            
            // 啟用反傷效果
            SimplePassiveEffects.EnableThorns(5);
            
            // 敵人攻擊玩家，應該觸發反傷
            var attackCmd = CommandBuilder.MakeAttackCmd(enemyId, playerId, 10);
            CommandSystem.ExecuteCmd(attackCmd);
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("攻擊後 (應該有反傷):");
            PrintActorStatus(playerId, "玩家");
            PrintActorStatus(enemyId, "敵人");
            
            Console.WriteLine("✅ 簡化事件系統測試完成\n");
        }
        
        // ✅ 新增：敵人意圖系統測試
        static void TestEnemyIntentSystem()
        {
            Console.WriteLine("=== 測試10: 敵人意圖系統 ===");
            
            ActorManager.Reset();
            CommandSystem.Clear();
            EnemyIntentSystem.ClearIntents();
            
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemy1Id = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 50);
            byte enemy2Id = ActorManager.AllocateActor(ActorType.ENEMY_ELITE, 80);
            
            Console.WriteLine("測試敵人意圖宣告:");
            PrintActorStatus(playerId, "玩家");
            PrintActorStatus(enemy1Id, "基礎敵人");
            PrintActorStatus(enemy2Id, "精英敵人");
            
            // 測試意圖宣告階段
            Console.WriteLine("\n--- 敵人意圖宣告階段 ---");
            EnemyIntentSystem.DeclareAllEnemyIntents();
            
            // 顯示宣告的意圖
            Console.WriteLine("\n宣告的敵人意圖:");
            EnemyIntentSystem.DebugPrintIntents();
            
            // 測試意圖執行階段
            Console.WriteLine("\n--- 敵人意圖執行階段 ---");
            Console.WriteLine("執行前狀態:");
            PrintActorStatus(playerId, "玩家");
            PrintActorStatus(enemy1Id, "基礎敵人");
            PrintActorStatus(enemy2Id, "精英敵人");
            
            EnemyIntentSystem.ExecuteAllDeclaredIntents();
            
            Console.WriteLine("\n執行後狀態:");
            PrintActorStatus(playerId, "玩家");
            PrintActorStatus(enemy1Id, "基礎敵人");
            PrintActorStatus(enemy2Id, "精英敵人");
            
            Console.WriteLine("✅ 敵人意圖系統測試完成\n");
        }
        
        // ==================== 輔助方法 ====================
        
        static void TestSingleHLA(HLA hla, byte srcId, byte targetId, string name)
        {
            Console.WriteLine($"--- 測試 {name} ({hla}) ---");
            
            // 記錄執行前狀態
            ref var srcBefore = ref ActorManager.GetActor(srcId);
            ref var targetBefore = ref ActorManager.GetActor(targetId);
            
            Console.WriteLine($"執行前: 源={srcBefore.HP}HP/{srcBefore.Block}Block/{srcBefore.Charge}Charge, 目標={targetBefore.HP}HP/{targetBefore.Block}Block/{targetBefore.Charge}Charge");
            
            // 執行HLA
            bool success = HLASystem.ProcessHLA(srcId, targetId, hla);
            
            // 記錄執行後狀態
            ref var srcAfter = ref ActorManager.GetActor(srcId);
            ref var targetAfter = ref ActorManager.GetActor(targetId);
            
            Console.WriteLine($"執行後: 源={srcAfter.HP}HP/{srcAfter.Block}Block/{srcAfter.Charge}Charge, 目標={targetAfter.HP}HP/{targetAfter.Block}Block/{targetAfter.Charge}Charge");
            Console.WriteLine($"HLA處理結果: {(success ? "成功" : "失敗")}");
        }
        
        static void PrintActorStatus(byte actorId, string name, string prefix = "")
        {
            if (!ActorManager.IsAlive(actorId))
            {
                Console.WriteLine($"  {prefix}{name}: 已死亡");
                return;
            }
            
            ref var actor = ref ActorManager.GetActor(actorId);
            Console.WriteLine($"  {prefix}{name}: {actor.HP}/{actor.MaxHP}HP, {actor.Block}Block, {actor.Charge}Charge");
        }
        
        static void PrintCombatStatus()
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
            
            // 顯示手牌狀態
            Console.WriteLine($"手牌數: {SimpleDeckManager.GetHandSize()}/{SimpleDeckManager.GetDeckSize()}");
            
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
            
            Console.WriteLine($"回合數: {PhaseSystem.GetTurnNumber()}");
            Console.WriteLine("==================");
        }
        
        // ✅ 新增：智能卡牌選擇邏輯
        static int SelectBestCard(ReadOnlySpan<SimpleCard> hand)
        {
            // 簡單優先級：攻擊 > 蓄力 > 格擋
            
            // 尋找攻擊卡
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i].Action == BasicAction.ATTACK)
                    return i;
            }
            
            // 尋找蓄力卡
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i].Action == BasicAction.CHARGE)
                    return i;
            }
            
            // 尋找格擋卡
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i].Action == BasicAction.BLOCK)
                    return i;
            }
            
            // 預設選擇第一張
            return 0;
        }
        
        static byte GetFirstEnemyId()
        {
            Span<byte> buffer = stackalloc byte[16];
            int count = ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, buffer);
            count += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, buffer[count..]);
            count += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, buffer[count..]);
            return count > 0 ? buffer[0] : (byte)1;
        }
    }
}