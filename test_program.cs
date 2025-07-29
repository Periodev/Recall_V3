// Program.cs - CombatCore 測試主程式
// 驗證整個戰鬥系統的運作

using System;
using CombatCore;

namespace CombatCoreTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== CombatCore 系統測試 ===\n");
            
            // 測試1: 基本系統初始化
            TestBasicSystemInitialization();
            
            // 測試2: Actor系統
            TestActorSystem();
            
            // 測試3: Command系統
            TestCommandSystem();
            
            // 測試4: HLA翻譯系統
            TestHLASystem();
            
            // 測試5: 完整戰鬥流程
            TestFullCombatFlow();
            
            // 測試6: 手動控制戰鬥
            TestManualCombat();
            
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
            
            // 創建測試Actor
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
            
            // 測試命令佇列
            Console.WriteLine("\n測試命令佇列:");
            CommandSystem.PushCmd(CommandBuilder.MakeAttackCmd(playerId, enemyId, 10));
            CommandSystem.PushCmd(CommandBuilder.MakeBlockCmd(enemyId, 5));
            Console.WriteLine($"佇列中命令數: {CommandSystem.GetQueueCount()}");
            
            int executedCount = CommandSystem.ExecuteAll();
            Console.WriteLine($"執行了 {executedCount} 個命令");
            Console.WriteLine($"執行後佇列命令數: {CommandSystem.GetQueueCount()}");
            
            // 顯示Actor最終狀態
            ref var player = ref ActorManager.GetActor(playerId);
            ref var enemy = ref ActorManager.GetActor(enemyId);
            Console.WriteLine($"最終玩家狀態: HP={player.HP}, Block={player.Block}, Charge={player.Charge}");
            Console.WriteLine($"最終敵人狀態: HP={enemy.HP}, Block={enemy.Block}, Charge={enemy.Charge}");
            
            Console.WriteLine("✅ Command系統測試完成\n");
        }
        
        static void TestHLASystem()
        {
            Console.WriteLine("=== 測試4: HLA翻譯系統 ===");
            
            ActorManager.Reset();
            CommandSystem.Clear();
            HLASystem.Reset();
            
            // 創建測試Actor
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemyId = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 60);
            
            Console.WriteLine("測試各種HLA翻譯:");
            
            // 測試基礎HLA
            TestSingleHLA(HLA.BASIC_ATTACK, playerId, enemyId, "基礎攻擊");
            TestSingleHLA(HLA.BASIC_BLOCK, playerId, enemyId, "基礎格擋");
            TestSingleHLA(HLA.BASIC_CHARGE, playerId, enemyId, "基礎蓄力");
            
            // 測試組合HLA
            TestSingleHLA(HLA.HEAVY_STRIKE, playerId, enemyId, "重擊");
            TestSingleHLA(HLA.SHIELD_BASH, playerId, enemyId, "盾擊");
            TestSingleHLA(HLA.COMBO_ATTACK, playerId, enemyId, "連擊");
            
            // 測試敵人HLA
            TestSingleHLA(HLA.ENEMY_AGGRESSIVE, enemyId, playerId, "敵人激進");
            TestSingleHLA(HLA.ENEMY_DEFENSIVE, enemyId, playerId, "敵人防禦");
            
            Console.WriteLine("✅ HLA系統測試完成\n");
        }
        
        static void TestSingleHLA(HLA hla, byte srcId, byte targetId, string name)
        {
            ref var srcBefore = ref ActorManager.GetActor(srcId);
            ref var targetBefore = ref ActorManager.GetActor(targetId);
            
            Console.WriteLine($"\n--- 測試 {name} ({hla}) ---");
            Console.WriteLine($"執行前: 源={srcBefore.HP}HP/{srcBefore.Block}Block/{srcBefore.Charge}Charge, " +
                            $"目標={targetBefore.HP}HP/{targetBefore.Block}Block/{targetBefore.Charge}Charge");
            
            bool success = HLASystem.ProcessHLA(srcId, targetId, hla);
            CommandSystem.ExecuteAll();
            
            ref var srcAfter = ref ActorManager.GetActor(srcId);
            ref var targetAfter = ref ActorManager.GetActor(targetId);
            
            Console.WriteLine($"執行後: 源={srcAfter.HP}HP/{srcAfter.Block}Block/{srcAfter.Charge}Charge, " +
                            $"目標={targetAfter.HP}HP/{targetAfter.Block}Block/{targetAfter.Charge}Charge");
            Console.WriteLine($"HLA處理結果: {(success ? "成功" : "失敗")}");
        }
        
        static void TestFullCombatFlow()
        {
            Console.WriteLine("=== 測試5: 完整戰鬥流程 ===");
            
            Console.WriteLine("執行自動戰鬥測試...");
            string result = CombatManager.RunCombatToEnd(20);
            Console.WriteLine($"戰鬥結果: {result}");
            
            Console.WriteLine("✅ 完整戰鬥流程測試完成\n");
        }
        
        static void TestManualCombat()
        {
            Console.WriteLine("=== 測試6: 手動控制戰鬥 ===");
            
            // 初始化戰鬥
            CombatManager.InitializeCombat();
            
            // 顯示初始狀態
            PrintCombatStatus();
            
            int maxSteps = 50;
            for (int step = 0; step < maxSteps; step++)
            {
                if (PhaseSystem.IsCombatEnded())
                {
                    Console.WriteLine($"戰鬥結束! 結果: {PhaseSystem.GetCombatResult()}");
                    break;
                }
                
                Console.WriteLine($"\n--- 步驟 {step + 1} ---");
                PhaseSystem.DebugPrintPhaseInfo();
                
                var result = CombatManager.StepCombat();
                Console.WriteLine($"Phase執行結果: {result}");
                
                // 處理玩家輸入
                if (result == PhaseResult.WAIT_INPUT)
                {
                    // 選擇不同的HLA進行測試
                    HLA[] testHLAs = { HLA.BASIC_ATTACK, HLA.HEAVY_STRIKE, HLA.SHIELD_BASH, HLA.COMBO_ATTACK };
                    HLA selectedHLA = testHLAs[step % testHLAs.Length];
                    
                    Console.WriteLine($"玩家選擇: {selectedHLA}");
                    CombatManager.InputPlayerAction(selectedHLA, 1);
                }
                
                // 每個Phase結束後顯示狀態
                if (result == PhaseResult.NEXT_PHASE)
                {
                    PrintCombatStatus();
                }
                
                // 避免無限循環
                if (result == PhaseResult.ERROR)
                {
                    Console.WriteLine("錯誤: Phase執行失敗!");
                    break;
                }
            }
            
            Console.WriteLine("✅ 手動控制戰鬥測試完成\n");
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
    }
}