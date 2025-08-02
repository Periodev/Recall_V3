// Phase.cs - 流程控制系統（整合卡牌系統版本）
// 回合制狀態機：Enemy Intent → Player Phase → Enemy Phase → Cleanup
// ✅ 修改：玩家輸入改為卡牌驅動，移除直接HLA輸入

using System;

namespace CombatCore
{
    // Phase上下文 - 流程控制狀態
    public struct PhaseContext
    {
        public PhaseId CurrentPhase;
        public PhaseStep CurrentStep;
        public bool WaitingForInput;
        public byte CurrentActorId;     // 當前行動的Actor
        public int TurnNumber;          // 回合數
        
        public void Reset()
        {
            CurrentPhase = PhaseId.ENEMY_INTENT;
            CurrentStep = PhaseStep.INIT;
            WaitingForInput = false;
            CurrentActorId = 0;
            TurnNumber = 0;
        }
    }
    
    // Phase系統 - 核心流程控制
    public static class PhaseSystem
    {
        private static PhaseContext s_context;
        
        // 初始化Phase系統
        public static void Initialize()
        {
            s_context.Reset();
        }
        
        // ✅ 核心執行函數 - stackalloc工作記憶體
        public static PhaseResult ExecuteCurrentStep()
        {
            // ✅ stackalloc工作記憶體 - 零GC分配
            Span<AtomicCmd> cmdBuffer = stackalloc AtomicCmd[CombatConstants.MAX_HLA_TRANSLATION];
            Span<byte> actorBuffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            
            return s_context.CurrentPhase switch
            {
                PhaseId.ENEMY_INTENT => ProcessEnemyIntentPhase(actorBuffer),
                PhaseId.PLAYER_PHASE => ProcessPlayerPhase(cmdBuffer, actorBuffer),
                PhaseId.ENEMY_PHASE => ProcessEnemyPhase(cmdBuffer, actorBuffer),
                PhaseId.CLEANUP => ProcessCleanupPhase(),
                _ => PhaseResult.ERROR
            };
        }
        
        // 強制轉換到指定Phase
        public static bool TransitionToPhase(PhaseId phase)
        {
            s_context.CurrentPhase = phase;
            s_context.CurrentStep = PhaseStep.INIT;
            s_context.WaitingForInput = false;
            return true;
        }
        
        // ✅ 新增：卡牌輸入接口（取代原本的HLA輸入）
        public static bool PlayCard(int handIndex, byte targetId = 0)
        {
            if (s_context.CurrentPhase != PhaseId.PLAYER_PHASE || !s_context.WaitingForInput)
            {
                Console.WriteLine($"當前不能使用卡牌：Phase={s_context.CurrentPhase}, WaitingForInput={s_context.WaitingForInput}");
                return false;
            }
            
            // 檢查手牌索引有效性
            var hand = SimpleDeckManager.GetHand();
            if (handIndex < 0 || handIndex >= hand.Length)
            {
                Console.WriteLine($"無效的卡牌索引：{handIndex}，手牌數量：{hand.Length}");
                return false;
            }
            
            // 使用卡牌系統
            bool success = SimpleDeckManager.UseCard(handIndex, targetId);

            if (success)
            {
                s_context.WaitingForInput = false;
                s_context.CurrentStep = PhaseStep.PROCESS;
                Console.WriteLine($"✅ 成功使用卡牌 {handIndex}: {hand[handIndex].Name}");
            }
            else
            {
                Console.WriteLine($"❌ 使用卡牌失敗：{handIndex}");
            }
            
            return success;
        }
        
        // ❌ 移除：直接HLA輸入接口
        // public static void SetPlayerInput(HLA playerHLA, byte targetId = 0)
        
        // 獲取當前狀態
        public static PhaseId GetCurrentPhase() => s_context.CurrentPhase;
        public static PhaseStep GetCurrentStep() => s_context.CurrentStep;
        public static bool IsWaitingForInput() => s_context.WaitingForInput;
        public static void SetWaitingForInput(bool waiting) => s_context.WaitingForInput = waiting;
        public static int GetTurnNumber() => s_context.TurnNumber;
        
        // ==================== Phase處理函數 ====================
        
        // Enemy Intent Phase - 敵人決策階段
        private static PhaseResult ProcessEnemyIntentPhase(Span<byte> actorBuffer)
        {
            return s_context.CurrentStep switch
            {
                PhaseStep.INIT => EnemyIntent_Init(),
                PhaseStep.PROCESS => EnemyIntent_Process(actorBuffer),
                PhaseStep.END => EnemyIntent_End(),
                _ => PhaseResult.ERROR
            };
        }
        
        private static PhaseResult EnemyIntent_Init()
        {
            s_context.CurrentStep = PhaseStep.PROCESS;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult EnemyIntent_Process(Span<byte> actorBuffer)
        {
            // ✅ 使用新的意圖宣告機制（純UI數據，不執行）
            EnemyIntentSystem.DeclareAllEnemyIntents();
            
            s_context.CurrentStep = PhaseStep.END;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult EnemyIntent_End()
        {
            // 轉換到玩家階段
            s_context.CurrentPhase = PhaseId.PLAYER_PHASE;
            s_context.CurrentStep = PhaseStep.INIT;
            
            // ✅ 除錯：顯示敵人意圖 (遊戲中UI會顯示)
            Console.WriteLine("敵人攻擊宣告完成，玩家可查看敵人意圖並選擇卡牌:");
            EnemyIntentSystem.DebugPrintIntents();
            
            return PhaseResult.NEXT_PHASE;
        }
        
        // Player Phase - 玩家行動階段
        private static PhaseResult ProcessPlayerPhase(Span<AtomicCmd> cmdBuffer, Span<byte> actorBuffer)
        {
            return s_context.CurrentStep switch
            {
                PhaseStep.INIT => PlayerPhase_Init(actorBuffer),
                PhaseStep.INPUT => PlayerPhase_Input(),
                PhaseStep.PROCESS => PlayerPhase_Process(),
                PhaseStep.EXECUTE => PlayerPhase_Execute(),
                PhaseStep.END => PlayerPhase_End(),
                _ => PhaseResult.ERROR
            };
        }
        
        private static PhaseResult PlayerPhase_Init(Span<byte> actorBuffer)
        {
            // 找到玩家Actor
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, actorBuffer);
            if (playerCount == 0)
            {
                return PhaseResult.COMBAT_END; // 玩家死亡，戰鬥結束
            }
            
            s_context.CurrentActorId = actorBuffer[0]; // 假設只有一個玩家
            
            // ✅ 新增：確保玩家有手牌可用
            if (SimpleDeckManager.GetHandSize() == 0)
            {
                Console.WriteLine("玩家手牌為空，重新洗牌");
                SimpleDeckManager.ShuffleAndDrawAll();
            }
            
            // ✅ 顯示當前手牌狀態
            Console.WriteLine($"玩家回合開始，當前手牌：");
            SimpleDeckManager.DebugPrintHand();
            
            s_context.CurrentStep = PhaseStep.INPUT;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult PlayerPhase_Input()
        {
            // 檢查是否已經有卡牌被使用
            if (s_context.WaitingForInput)
            {
                Console.WriteLine("⏳ 等待玩家選擇卡牌...");
                return PhaseResult.WAIT_INPUT;
            }
            
            // 設置等待狀態並返回
            s_context.WaitingForInput = true;
            Console.WriteLine("⏳ 等待玩家選擇卡牌...");
            return PhaseResult.WAIT_INPUT;
        }
        
        private static PhaseResult PlayerPhase_Process()
        {
            // ✅ 修改：不再需要處理HLA，因為卡牌使用時已經處理了
            // 卡牌系統的 UseCard() 會自動調用 HLASystem.ProcessHLA()
            
            Console.WriteLine("玩家卡牌效果已執行");
            s_context.CurrentStep = PhaseStep.EXECUTE;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult PlayerPhase_Execute()
        {
            // 執行所有命令
            int executedCount = CommandSystem.ExecuteAll();
            Console.WriteLine($"執行了 {executedCount} 個命令");
            
            s_context.CurrentStep = PhaseStep.END;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult PlayerPhase_End()
        {
            // 轉換到敵人階段
            s_context.CurrentPhase = PhaseId.ENEMY_PHASE;
            s_context.CurrentStep = PhaseStep.INIT;
            return PhaseResult.NEXT_PHASE;
        }
        
        // Enemy Phase - 敵人行動階段
        private static PhaseResult ProcessEnemyPhase(Span<AtomicCmd> cmdBuffer, Span<byte> actorBuffer)
        {
            return s_context.CurrentStep switch
            {
                PhaseStep.INIT => EnemyPhase_Init(actorBuffer),
                PhaseStep.PROCESS => EnemyPhase_Process(actorBuffer),
                PhaseStep.EXECUTE => EnemyPhase_Execute(),
                PhaseStep.END => EnemyPhase_End(),
                _ => PhaseResult.ERROR
            };
        }
        
        private static PhaseResult EnemyPhase_Init(Span<byte> actorBuffer)
        {
            // 檢查是否還有活著的敵人
            int enemyCount = 0;
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, actorBuffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, actorBuffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, actorBuffer[enemyCount..]);
            
            if (enemyCount == 0)
            {
                return PhaseResult.COMBAT_END; // 所有敵人死亡，戰鬥結束
            }
            
            s_context.CurrentStep = PhaseStep.PROCESS;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult EnemyPhase_Process(Span<byte> actorBuffer)
        {
            // ✅ 新機制：執行之前宣告的意圖
            Console.WriteLine("敵人執行之前宣告的攻擊意圖:");
            EnemyIntentSystem.ExecuteAllDeclaredIntents();
            
            s_context.CurrentStep = PhaseStep.EXECUTE;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult EnemyPhase_Execute()
        {
            // 執行所有敵人命令
            int executedCount = CommandSystem.ExecuteAll();
            Console.WriteLine($"敵人執行了 {executedCount} 個命令");
            
            s_context.CurrentStep = PhaseStep.END;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult EnemyPhase_End()
        {
            // 轉換到清理階段
            s_context.CurrentPhase = PhaseId.CLEANUP;
            s_context.CurrentStep = PhaseStep.INIT;
            return PhaseResult.NEXT_PHASE;
        }
        
        // Cleanup Phase - 清理階段
        private static PhaseResult ProcessCleanupPhase()
        {
            return s_context.CurrentStep switch
            {
                PhaseStep.INIT => Cleanup_Init(),
                PhaseStep.PROCESS => Cleanup_Process(),
                PhaseStep.END => Cleanup_End(),
                _ => PhaseResult.ERROR
            };
        }
        
        private static PhaseResult Cleanup_Init()
        {
            s_context.CurrentStep = PhaseStep.PROCESS;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult Cleanup_Process()
        {
            // ✅ 觸發回合結束事件
            SimpleEventSystem.OnTurnEnd();
            
            // 推入回合結束清理命令
            CommandSystem.PushCmd(AtomicCmd.TurnEndCleanup());
            CommandSystem.ExecuteAll();
            
            // ✅ 新增：卡牌系統回合結束處理
            SimpleDeckManager.OnTurnEnd();
            
            s_context.CurrentStep = PhaseStep.END;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult Cleanup_End()
        {
            // 增加回合數，回到敵人意圖階段
            s_context.TurnNumber++;
            
            // ✅ 觸發新回合開始事件
            SimpleEventSystem.OnTurnStart();
            
            s_context.CurrentPhase = PhaseId.ENEMY_INTENT;
            s_context.CurrentStep = PhaseStep.INIT;
            
            Console.WriteLine($"=== 回合 {s_context.TurnNumber} 開始 ===");
            
            return PhaseResult.NEXT_PHASE;
        }
        
        // ==================== 除錯與工具函數 ====================
        
        public static void DebugPrintPhaseInfo()
        {
            Console.WriteLine($"Phase: {s_context.CurrentPhase}, Step: {s_context.CurrentStep}");
            Console.WriteLine($"Turn: {s_context.TurnNumber}, WaitingInput: {s_context.WaitingForInput}");
            Console.WriteLine($"CurrentActor: {s_context.CurrentActorId}");
        }
        
        // 重置Phase系統
        public static void Reset()
        {
            s_context.Reset();
            HLASystem.Reset();
            CommandSystem.Clear();
            SimpleEventSystem.Reset();  // ✅ 重置事件系統
            EnemyIntentSystem.ClearIntents(); // ✅ 清理敵人意圖
        }
        
        // 檢查戰鬥是否結束
        public static bool IsCombatEnded()
        {
            // ✅ stackalloc檢查
            Span<byte> buffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, buffer);
            if (playerCount == 0) return true; // 玩家全死
            
            int enemyCount = 0;
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, buffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, buffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, buffer[enemyCount..]);
            
            return enemyCount == 0; // 敵人全死
        }
        
        // 獲取戰鬥勝負結果
        public static string GetCombatResult()
        {
            Span<byte> buffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, buffer);
            if (playerCount == 0) return "敗北";
            
            int enemyCount = 0;
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, buffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, buffer[enemyCount..]);  
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, buffer[enemyCount..]);
            
            if (enemyCount == 0) return "勝利";
            
            return "進行中";
        }
    }
    
    // ✅ 修改：卡牌驅動的戰鬥管理器
    public static class CombatManager
    {
        // 初始化戰鬥
        public static void InitializeCombat()
        {
            ActorManager.Reset();
            PhaseSystem.Initialize();
            SimpleEventSystem.Initialize(); // ✅ 初始化事件系統
            
            // ✅ 初始化卡牌系統
            SimpleDeckManager.SetDeckConfig(DeckConfig.DEFAULT);
            SimpleDeckManager.StartCombat();
            
            // 創建玩家
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            
            // 創建敵人
            ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 50);
            ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 40);
            
            Console.WriteLine("戰鬥初始化完成");
            Console.WriteLine("牌組配置:");
            SimpleDeckManager.DebugPrintDeckConfig();
        }
        
        // 執行一步戰鬥流程
        public static PhaseResult StepCombat()
        {
            return PhaseSystem.ExecuteCurrentStep();
        }
        
        // ❌ 移除：直接HLA輸入
        // public static void InputPlayerAction(HLA hla, byte targetId = 1)
        
        // ✅ 新增：卡牌輸入
        public static bool PlayPlayerCard(int handIndex, byte targetId = 1)
        {
            return PhaseSystem.PlayCard(handIndex, targetId);
        }
        
        // ✅ 修改：運行完整戰鬥循環使用卡牌AI
        public static string RunCombatToEnd(int maxTurns = 50)
        {
            InitializeCombat();
            
            for (int turn = 0; turn < maxTurns; turn++)
            {
                if (PhaseSystem.IsCombatEnded())
                {
                    break;
                }
                
                var result = StepCombat();
                
                // 如果需要玩家輸入，使用自動卡牌AI
                if (result == PhaseResult.WAIT_INPUT)
                {
                    Console.WriteLine($"🔄 回合 {turn}: 檢測到WAIT_INPUT，調用自動AI");
                    AutoPlayPlayerCards();
                    Console.WriteLine($"🔄 回合 {turn}: AI選擇完成，繼續執行");
                    StepCombat(); // 處理卡牌使用結果
                }
                
                if (result == PhaseResult.ERROR)
                {
                    return "錯誤";
                }
            }
            
            return PhaseSystem.GetCombatResult();
        }
        
        // ✅ 新增：自動卡牌AI
        private static void AutoPlayPlayerCards()
        {
            var hand = SimpleDeckManager.GetHand();
            Console.WriteLine($"🤖 自動AI選擇卡牌，手牌數: {hand.Length}");
            
            if (hand.Length == 0)
            {
                Console.WriteLine("❌ 手牌為空！");
                return;
            }
            
            // 簡單AI：優先攻擊 > 蓄力 > 格擋
            byte target = GetDefaultEnemyTarget();
            
            // 1. 尋找攻擊卡
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i].Action == BasicAction.ATTACK && target != 0)
                {
                    if (SimpleDeckManager.UseCard(i, target))
                    {
                        Console.WriteLine($"🤖 AI使用攻擊卡攻擊敵人{target}");
                        PhaseSystem.SetWaitingForInput(false); // 通過PhaseSystem設置狀態
                        return;
                    }
                }
            }
            
            // 2. 尋找蓄力卡
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i].Action == BasicAction.CHARGE)
                {
                    if (SimpleDeckManager.UseCard(i, 0))
                    {
                        Console.WriteLine("🤖 AI使用蓄力卡");
                        PhaseSystem.SetWaitingForInput(false); // 通過PhaseSystem設置狀態
                        return;
                    }
                }
            }
            
            // 3. 尋找格擋卡
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i].Action == BasicAction.BLOCK)
                {
                    if (SimpleDeckManager.UseCard(i, 0))
                    {
                        Console.WriteLine("🤖 AI使用格擋卡");
                        PhaseSystem.SetWaitingForInput(false); // 通過PhaseSystem設置狀態
                        return;
                    }
                }
            }
            
            Console.WriteLine("❌ AI無法找到可用的卡牌");
        }
        
        // 獲取預設敵人目標
        private static byte GetDefaultEnemyTarget()
        {
            Span<byte> enemyBuffer = stackalloc byte[16];
            int enemyCount = 0;
            
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, enemyBuffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, enemyBuffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, enemyBuffer[enemyCount..]);
            
            return enemyCount > 0 ? enemyBuffer[0] : (byte)0;
        }
    }
}