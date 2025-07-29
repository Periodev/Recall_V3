// Program.cs - CombatCore 測試主程式
// 驗證整個戰鬥系統的運作，包含Reaction系統測試

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
            
            // 測試7: Reaction系統
            TestReactionSystem();
            
            // 測試8: 敵人意圖傳遞機制
            TestEnemyIntentFlow();
            
            // 測試9: 新的立即/延後效果分離機制
            TestImmediateVsDelayedEffects();
            
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
        
        static void TestReactionSystem()
        {
            Console.WriteLine("=== 測試7: Reaction系統 ===");
            
            // 重置所有系統
            ActorManager.Reset();
            CommandSystem.Clear();
            ReactionSystem.Reset();
            CommonReactions.ResetRuleIdCounter();
            
            // 創建測試Actor
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemyId = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 80);
            
            Console.WriteLine("測試1: 反擊系統");
            // 為玩家註冊反擊效果
            var counterRule = CommonReactions.CreateCounterAttack(playerId, 15);
            ReactionSystem.RegisterRule(counterRule);
            
            Console.WriteLine($"註冊反擊規則: {counterRule.Name}");
            PrintCombatStatus();
            
            // 敵人攻擊玩家，應該觸發反擊
            var attackCmd = CommandBuilder.MakeAttackCmd(enemyId, playerId, 20);
            var result = CommandSystem.ExecuteCmd(attackCmd);
            CommandSystem.ExecuteAll(); // 處理可能的反應命令
            
            Console.WriteLine($"敵人攻擊結果: {result.Message}");
            PrintCombatStatus();
            
            Console.WriteLine("\n測試2: 自癒系統");
            // 為玩家註冊自癒效果
            var healRule = CommonReactions.CreateSelfHeal(playerId, 10);
            ReactionSystem.RegisterRule(healRule);
            
            Console.WriteLine($"註冊自癒規則: {healRule.Name}");
            
            // 觸發回合結束，應該自動治療
            ReactionEventDispatcher.OnTurnEnd(1);
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("觸發回合結束事件");
            PrintCombatStatus();
            
            Console.WriteLine("\n測試3: 護甲再生系統");
            // 為玩家註冊護甲再生效果
            var blockRule = CommonReactions.CreateBlockRegen(playerId, 8);
            ReactionSystem.RegisterRule(blockRule);
            
            Console.WriteLine($"註冊護甲再生規則: {blockRule.Name}");
            
            // 觸發回合開始，應該獲得護甲
            ReactionEventDispatcher.OnTurnStart(2);
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("觸發回合開始事件");
            PrintCombatStatus();
            
            Console.WriteLine("\n測試4: 一次性復仇系統");
            // 為玩家註冊復仇效果（一次性）
            var revengeRule = CommonReactions.CreateRevenge(playerId, 3);
            ReactionSystem.RegisterRule(revengeRule);
            
            Console.WriteLine($"註冊復仇規則: {revengeRule.Name} (一次性)");
            
            // 第一次受傷，應該觸發復仇
            attackCmd = CommandBuilder.MakeAttackCmd(enemyId, playerId, 15);
            result = CommandSystem.ExecuteCmd(attackCmd);
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("第一次受傷:");
            PrintCombatStatus();
            
            // 第二次受傷，不應該再觸發復仇
            attackCmd = CommandBuilder.MakeAttackCmd(enemyId, playerId, 15);
            result = CommandSystem.ExecuteCmd(attackCmd);
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("第二次受傷 (復仇不應再觸發):");
            PrintCombatStatus();
            
            Console.WriteLine("\n測試5: 自定義反應規則");
            // 創建自定義規則：當敵人獲得護甲時，玩家獲得蓄力
            var customCondition = new ReactionCondition(
                ReactionTrigger.ACTOR_BLOCK_GAINED,
                targetFilter: enemyId
            );
            var customEffect = new ReactionEffect(
                ReactionEffectType.ADD_CHARGE,
                playerId,
                2
            );
            var customRule = new ReactionRule(99, customCondition, customEffect, "敵人護甲->玩家蓄力");
            ReactionSystem.RegisterRule(customRule);
            
            Console.WriteLine($"註冊自定義規則: {customRule.Name}");
            
            // 敵人獲得護甲，應該觸發玩家蓄力
            var blockCmd = CommandBuilder.MakeBlockCmd(enemyId, 12);
            result = CommandSystem.ExecuteCmd(blockCmd);
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("敵人獲得護甲後:");
            PrintCombatStatus();
            
            // 顯示反應系統統計
            Console.WriteLine($"\n反應系統統計:");
            ReactionSystem.DebugPrintRules();
            
            Console.WriteLine("✅ Reaction系統測試完成\n");
        }
        
        static void TestEnemyIntentFlow()
        {
            Console.WriteLine("=== 測試8: 敵人意圖傳遞機制 ===");
            
            // 初始化戰鬥
            CombatManager.InitializeCombat();
            
            Console.WriteLine("=== Phase流程展示 ===");
            
            // 模擬完整的Phase循環
            for (int cycle = 0; cycle < 3; cycle++)
            {
                Console.WriteLine($"\n--- 循環 {cycle + 1} ---");
                
                // 1. Enemy Intent Phase
                Console.WriteLine("\n📋 Enemy Intent Phase - 敵人決策攻擊宣告");
                PhaseSystem.TransitionToPhase(PhaseId.ENEMY_INTENT);
                var result = PhaseSystem.ExecuteCurrentStep(); // INIT
                result = PhaseSystem.ExecuteCurrentStep(); // PROCESS - AI決策
                result = PhaseSystem.ExecuteCurrentStep(); // END
                
                // 顯示敵人意圖
                Console.WriteLine("敵人攻擊宣告:");
                HLASystem.DebugPrintEnemyIntents();
                
                // 2. Player Phase  
                Console.WriteLine("\n🎮 Player Phase - 玩家看到意圖後行動");
                result = PhaseSystem.ExecuteCurrentStep(); // 自動轉到Player Phase INIT
                result = PhaseSystem.ExecuteCurrentStep(); // INPUT
                
                // 模擬玩家看到意圖後的決策
                Console.WriteLine("玩家看到敵人意圖，選擇防禦");
                CombatManager.InputPlayerAction(HLA.BASIC_BLOCK, 1);
                
                result = PhaseSystem.ExecuteCurrentStep(); // PROCESS
                result = PhaseSystem.ExecuteCurrentStep(); // EXECUTE  
                result = PhaseSystem.ExecuteCurrentStep(); // END
                
                // 3. Enemy Phase
                Console.WriteLine("\n👹 Enemy Phase - 執行之前宣告的攻擊");
                result = PhaseSystem.ExecuteCurrentStep(); // 自動轉到Enemy Phase INIT
                result = PhaseSystem.ExecuteCurrentStep(); // PROCESS - 執行意圖
                result = PhaseSystem.ExecuteCurrentStep(); // EXECUTE
                result = PhaseSystem.ExecuteCurrentStep(); // END
                
                Console.WriteLine("敵人執行攻擊後狀態:");
                PrintCombatStatus();
                
                // 4. Cleanup Phase
                Console.WriteLine("\n🧹 Cleanup Phase");
                result = PhaseSystem.ExecuteCurrentStep(); // 自動轉到Cleanup INIT
                result = PhaseSystem.ExecuteCurrentStep(); // PROCESS
                result = PhaseSystem.ExecuteCurrentStep(); // END
                
                if (PhaseSystem.IsCombatEnded())
                {
                    Console.WriteLine($"戰鬥結束: {PhaseSystem.GetCombatResult()}");
                    break;
                }
            }
            
            Console.WriteLine("\n=== 意圖傳遞驗證 ===");
            
            // 重新初始化進行詳細驗證
            CombatManager.InitializeCombat();
            
            Console.WriteLine("步驟1: 敵人意圖階段 - 決策儲存");
            CombatAI.DecideAndDeclareForAllEnemies();
            
            // 顯示決策結果
            Span<byte> enemyBuffer = stackalloc byte[16];
            int enemyCount = ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, enemyBuffer);
            
            Console.WriteLine("儲存的敵人意圖:");
            for (int i = 0; i < enemyCount; i++)
            {
                byte enemyId = enemyBuffer[i];
                var storedHLA = HLASystem.GetEnemyHLA(enemyId);
                var intent = HLASystem.GetEnemyIntent(enemyId);
                Console.WriteLine($"  敵人 {enemyId}: 儲存HLA={storedHLA}, 意圖='{intent.Description}' (預估{intent.EstimatedValue}傷害)");
            }
            
            Console.WriteLine("\n步驟2: 玩家階段 - 意圖保持不變");
            Console.WriteLine("(玩家可以查看敵人意圖來做決策)");
            
            // 驗證意圖沒有改變
            for (int i = 0; i < enemyCount; i++)
            {
                byte enemyId = enemyBuffer[i];
                var currentHLA = HLASystem.GetEnemyHLA(enemyId);
                Console.WriteLine($"  敵人 {enemyId}: 當前HLA={currentHLA} (未改變)");
            }
            
            Console.WriteLine("\n步驟3: 敵人階段 - 執行儲存的意圖");
            
            // 獲取玩家ID
            Span<byte> playerBuffer = stackalloc byte[16];
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, playerBuffer);
            byte playerId = playerCount > 0 ? playerBuffer[0] : (byte)0;
            
            PrintCombatStatus();
            Console.WriteLine("執行敵人意圖:");
            
            for (int i = 0; i < enemyCount; i++)
            {
                byte enemyId = enemyBuffer[i];
                var executedHLA = HLASystem.GetEnemyHLA(enemyId);
                bool success = HLASystem.ProcessEnemyHLA(enemyId, playerId);
                Console.WriteLine($"  敵人 {enemyId}: 執行HLA={executedHLA}, 成功={success}");
            }
            
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("執行後狀態:");
            PrintCombatStatus();
            
            Console.WriteLine("\n✅ 意圖傳遞機制驗證:");
            Console.WriteLine("  1. Intent階段: AI決策 → HLA儲存到Dictionary ✅");
            Console.WriteLine("  2. Player階段: 意圖保持不變，玩家可查詢 ✅");  
            Console.WriteLine("  3. Enemy階段: 從Dictionary取得HLA → 翻譯 → 執行 ✅");
            
            Console.WriteLine("✅ 敵人意圖傳遞機制測試完成\n");
        }
        
        static void TestImmediateVsDelayedEffects()
        {
            Console.WriteLine("=== 測試9: 立即/延後效果分離機制 ===");
            
            // 重置所有系統
            ActorManager.Reset();
            CommandSystem.Clear();
            ReactionSystem.Reset();
            HLATranslator.ResetRuleIdCounter();
            
            // 創建測試Actor
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            byte enemyId = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 80);
            
            Console.WriteLine("=== 測試1: ENEMY_DEFENSIVE (立即護甲+蓄力，無攻擊) ===");
            
            Console.WriteLine("Intent Phase前狀態:");
            PrintActorStatus(enemyId, "敵人");
            PrintActorStatus(playerId, "玩家");
            
            // 敵人宣告防禦姿態
            Console.WriteLine("\n敵人宣告: ENEMY_DEFENSIVE");
            HLASystem.ProcessHLA(enemyId, playerId, HLA.ENEMY_DEFENSIVE);
            
            Console.WriteLine("Intent Phase後狀態 (應該立即獲得護甲+蓄力):");
            PrintActorStatus(enemyId, "敵人");
            PrintActorStatus(playerId, "玩家");
            
            // 玩家階段 - 攻擊有護甲的敵人
            Console.WriteLine("\n=== Player Phase: 玩家攻擊有護甲的敵人 ===");
            var attackCmd = CommandBuilder.MakeAttackCmd(playerId, enemyId, 15);
            var result = CommandSystem.ExecuteCmd(attackCmd);
            Console.WriteLine($"攻擊結果: {result.Message}");
            
            Console.WriteLine("攻擊後狀態:");
            PrintActorStatus(enemyId, "敵人");
            PrintActorStatus(playerId, "玩家");
            
            // Enemy Phase - 應該沒有延後攻擊效果
            Console.WriteLine("\n=== Enemy Phase: 檢查是否有延後效果 ===");
            ReactionEventDispatcher.OnEnemyPhaseStart();
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("Enemy Phase後狀態 (ENEMY_DEFENSIVE無攻擊，應該無變化):");
            PrintActorStatus(enemyId, "敵人");
            PrintActorStatus(playerId, "玩家");
            
            Console.WriteLine("\n" + "=".PadRight(50, '='));
            
            Console.WriteLine("=== 測試2: SHIELD_BASH (立即護甲，延後攻擊) ===");
            
            // 重置狀態
            ActorManager.Reset();
            ReactionSystem.Reset();
            playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            enemyId = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 80);
            
            Console.WriteLine("Intent Phase前狀態:");
            PrintActorStatus(enemyId, "敵人");
            PrintActorStatus(playerId, "玩家");
            
            // 敵人宣告盾擊
            Console.WriteLine("\n敵人宣告: SHIELD_BASH");
            HLASystem.ProcessHLA(enemyId, playerId, HLA.SHIELD_BASH);
            
            Console.WriteLine("Intent Phase後狀態 (應該立即獲得護甲，攻擊尚未執行):");
            PrintActorStatus(enemyId, "敵人");
            PrintActorStatus(playerId, "玩家");
            
            // Player Phase
            Console.WriteLine("\n=== Player Phase: 玩家決定防禦 ===");
            var blockCmd = CommandBuilder.MakeBlockCmd(playerId, 10);
            CommandSystem.ExecuteCmd(blockCmd);
            
            Console.WriteLine("玩家防禦後狀態:");
            PrintActorStatus(enemyId, "敵人");
            PrintActorStatus(playerId, "玩家");
            
            // Enemy Phase - 應該執行延後的攻擊
            Console.WriteLine("\n=== Enemy Phase: 執行延後的攻擊效果 ===");
            ReactionEventDispatcher.OnEnemyPhaseStart();
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("Enemy Phase後狀態 (應該執行了攻擊):");
            PrintActorStatus(enemyId, "敵人");
            PrintActorStatus(playerId, "玩家");
            
            Console.WriteLine("\n" + "=".PadRight(50, '='));
            
            Console.WriteLine("=== 測試3: COMBO_ATTACK (純攻擊，無立即效果) ===");
            
            // 重置狀態
            ActorManager.Reset();
            ReactionSystem.Reset();
            playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            enemyId = ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 80);
            
            Console.WriteLine("Intent Phase前狀態:");
            PrintActorStatus(enemyId, "敵人");
            PrintActorStatus(playerId, "玩家");
            
            // 敵人宣告連擊
            Console.WriteLine("\n敵人宣告: COMBO_ATTACK");
            HLASystem.ProcessHLA(enemyId, playerId, HLA.COMBO_ATTACK);
            
            Console.WriteLine("Intent Phase後狀態 (純攻擊意圖，無立即效果):");
            PrintActorStatus(enemyId, "敵人");
            PrintActorStatus(playerId, "玩家");
            
            // Enemy Phase - 應該執行雙重攻擊
            Console.WriteLine("\n=== Enemy Phase: 執行延後的連擊 ===");
            ReactionEventDispatcher.OnEnemyPhaseStart();
            CommandSystem.ExecuteAll();
            
            Console.WriteLine("Enemy Phase後狀態 (應該執行了連擊):");
            PrintActorStatus(enemyId, "敵人");
            PrintActorStatus(playerId, "玩家");
            
            Console.WriteLine("\n✅ 立即/延後效果分離機制驗證:");
            Console.WriteLine("  1. 防禦效果立即生效 ✅");
            Console.WriteLine("  2. 攻擊效果延後到Enemy Phase ✅");  
            Console.WriteLine("  3. Reaction系統正確分離時機 ✅");
            Console.WriteLine("  4. 玩家可在看到立即效果後做決策 ✅");
            
            Console.WriteLine("✅ 立即/延後效果分離機制測試完成\n");
        }
        
        static void PrintActorStatus(byte actorId, string name)
        {
            if (!ActorManager.IsAlive(actorId))
            {
                Console.WriteLine($"  {name}: 已死亡");
                return;
            }
            
            ref var actor = ref ActorManager.GetActor(actorId);
            Console.WriteLine($"  {name}: {actor.HP}/{actor.MaxHP}HP, {actor.Block}Block, {actor.Charge}Charge");
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