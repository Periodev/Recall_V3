// Phase.cs - 流程控制系統
// 回合制狀態機：Enemy Intent → Player Phase → Enemy Phase → Cleanup
// 已整合Reaction系統事件觸發

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
        public byte PlayerTargetId;     // 玩家選擇的目標
        public int TurnNumber;          // 回合數
        
        public void Reset()
        {
            CurrentPhase = PhaseId.ENEMY_INTENT;
            CurrentStep = PhaseStep.INIT;
            WaitingForInput = false;
            CurrentActorId = 0;
            PlayerTargetId = 0;
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
        
        // 設置玩家輸入
        public static void SetPlayerInput(HLA playerHLA, byte targetId = 0)
        {
            if (s_context.CurrentPhase == PhaseId.PLAYER_PHASE && s_context.WaitingForInput)
            {
                HLASystem.SetPlayerHLA(playerHLA);
                s_context.PlayerTargetId = targetId;
                s_context.WaitingForInput = false;
            }
        }
        
        // 獲取當前狀態
        public static PhaseId GetCurrentPhase() => s_context.CurrentPhase;
        public static PhaseStep GetCurrentStep() => s_context.CurrentStep;
        public static bool IsWaitingForInput() => s_context.WaitingForInput;
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
            // ✅ 修改：使用新的意圖宣告系統
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
            Console.WriteLine("敵人攻擊宣告完成，玩家可查看敵人意圖:");
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
            s_context.CurrentStep = PhaseStep.INPUT;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult PlayerPhase_Input()
        {
            // 等待玩家輸入
            s_context.WaitingForInput = true;
            
            // ✅ 除錯：提醒玩家可查看敵人意圖 (遊戲中UI會顯示)
            if (s_context.WaitingForInput)
            {
                Console.WriteLine("等待玩家輸入 - 可查看敵人意圖進行決策");
                return PhaseResult.WAIT_INPUT;
            }
            
            s_context.CurrentStep = PhaseStep.PROCESS;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult PlayerPhase_Process()
        {
            // 處理玩家HLA
            byte playerId = s_context.CurrentActorId;
            byte targetId = s_context.PlayerTargetId;
            
            if (!HLASystem.ProcessPlayerHLA(playerId, targetId))
            {
                // HLA處理失敗，使用基礎攻擊作為後備
                HLASystem.ProcessHLA(playerId, targetId, HLA.BASIC_ATTACK);
            }
            
            s_context.CurrentStep = PhaseStep.EXECUTE;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult PlayerPhase_Execute()
        {
            // 執行所有命令
            CommandSystem.ExecuteAll();
            
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
            // ✅ 修改：執行之前宣告的意圖
            Console.WriteLine("👹 Enemy Phase - 執行之前宣告的攻擊意圖:");
            
            EnemyIntentSystem.ExecuteAllDeclaredIntents();
            
            s_context.CurrentStep = PhaseStep.EXECUTE;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult EnemyPhase_Execute()
        {
            // 執行所有敵人命令
            CommandSystem.ExecuteAll();
            
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
            
            s_context.CurrentStep = PhaseStep.END;
            return PhaseResult.NEXT_STEP;
        }
        
        private static PhaseResult Cleanup_End()
        {
            // 增加回合數，回到敵人意圖階段
            s_context.TurnNumber++;
            
            // ✅ 觸發新回合開始事件
            SimpleEventSystem.OnTurnStart();
            
            var oldPhase = s_context.CurrentPhase;
            s_context.CurrentPhase = PhaseId.ENEMY_INTENT;
            s_context.CurrentStep = PhaseStep.INIT;
            
            // ✅ 觸發Phase轉換事件
            // SimpleEventSystem 會在具體的 Phase 處理中觸發
            
            return PhaseResult.NEXT_PHASE;
        }
        
        // ==================== 除錯與工具函數 ====================
        
        public static void DebugPrintPhaseInfo()
        {
            Console.WriteLine($"Phase: {s_context.CurrentPhase}, Step: {s_context.CurrentStep}");
            Console.WriteLine($"Turn: {s_context.TurnNumber}, WaitingInput: {s_context.WaitingForInput}");
            Console.WriteLine($"CurrentActor: {s_context.CurrentActorId}, Target: {s_context.PlayerTargetId}");
        }
        
        // 重置Phase系統
        public static void Reset()
        {
            s_context.Reset();
            HLASystem.Reset();
            CommandSystem.Clear();
            SimpleEventSystem.Reset();  // ✅ 重置簡化事件系統
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
    
    // 簡單的戰鬥管理器 - 整合所有系統
    public static class CombatManager
    {
        // 初始化戰鬥
        public static void InitializeCombat()
        {
            ActorManager.Reset();
            PhaseSystem.Initialize();
            
            // 創建玩家
            byte playerId = ActorManager.AllocateActor(ActorType.PLAYER, 100);
            
            // 創建敵人
            ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 50);
            ActorManager.AllocateActor(ActorType.ENEMY_BASIC, 40);
            
            // 觸發初始回合開始事件
            SimpleEventSystem.OnTurnStart();
        }
        
        // 執行一步戰鬥流程
        public static PhaseResult StepCombat()
        {
            return PhaseSystem.ExecuteCurrentStep();
        }
        
        // 輸入玩家動作
        public static void InputPlayerAction(HLA hla, byte targetId = 1)
        {
            PhaseSystem.SetPlayerInput(hla, targetId);
        }
        
        // 運行完整戰鬥循環直到結束
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
                
                // 如果需要玩家輸入，提供簡單的AI輸入
                if (result == PhaseResult.WAIT_INPUT)
                {
                    InputPlayerAction(HLA.BASIC_ATTACK, 1); // 攻擊第一個敵人
                    StepCombat(); // 處理輸入
                }
                
                if (result == PhaseResult.ERROR)
                {
                    return "錯誤";
                }
            }
            
            return PhaseSystem.GetCombatResult();
        }
    }
}