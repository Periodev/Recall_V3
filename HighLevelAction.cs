// HighLevelAction.cs - 高階動作系統（簡化版）
// 戰術抽象：HLA → AtomicCmd[]序列翻譯

using System;

namespace CombatCore
{
    // HLA翻譯引擎 - 簡化版本
    public static class HLATranslator
    {
        // 簡化的HLA翻譯方法
        public static int TranslateHLA(HLA hla, byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            switch (hla)
            {
                case HLA.BASIC_ATTACK:
                    buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 10);
                    return 1;
                    
                case HLA.BASIC_BLOCK:
                    buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 5);
                    return 1;
                    
                case HLA.BASIC_CHARGE:
                    buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 1);
                    return 1;
                    
                case HLA.HEAVY_STRIKE:
                    buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 2);
                    buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 15);
                    return 2;
                    
                case HLA.SHIELD_BASH:
                    buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 3);
                    buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);
                    return 2;
                    
                case HLA.COMBO_ATTACK:
                    buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);
                    buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);
                    return 2;
                    
                case HLA.ENEMY_AGGRESSIVE:
                    buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 1);
                    buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 12);
                    return 2;
                    
                case HLA.ENEMY_DEFENSIVE:
                    buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 10);
                    buffer[1] = CommandBuilder.MakeChargeCmd(srcId, 2);
                    return 2;
                    
                case HLA.ENEMY_BERSERKER:
                    buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 20);
                    return 1;
                    
                default:
                    return 0;
            }
        }
        
        // 獲取基本動作類型
        public static int GetBasicAction(HLA hla)
        {
            return hla switch
            {
                HLA.BASIC_ATTACK => 1,
                HLA.BASIC_BLOCK => 2,
                HLA.BASIC_CHARGE => 3,
                HLA.HEAVY_STRIKE => 4,
                HLA.SHIELD_BASH => 5,
                HLA.COMBO_ATTACK => 6,
                HLA.ENEMY_AGGRESSIVE => 7,
                HLA.ENEMY_DEFENSIVE => 8,
                HLA.ENEMY_BERSERKER => 9,
                _ => 0
            };
        }
    }
    
    // HLA處理系統 - 管理HLA的暫存和處理
    public static class HLASystem
    {
        // 玩家HLA暫存
        private static HLA s_playerHLA = HLA.BASIC_ATTACK;
        
        // 設置和獲取玩家HLA
        public static void SetPlayerHLA(HLA hla) => s_playerHLA = hla;
        public static HLA GetPlayerHLA() => s_playerHLA;
        
        // 處理單個HLA
        public static bool ProcessHLA(byte actorId, byte targetId, HLA hla)
        {
            if (!ActorManager.IsAlive(actorId)) return false;
            
            // 翻譯HLA為命令序列
            Span<AtomicCmd> buffer = stackalloc AtomicCmd[8];
            int cmdCount = HLATranslator.TranslateHLA(hla, actorId, targetId, buffer);
            
            // 推送所有命令到佇列
            for (int i = 0; i < cmdCount; i++)
            {
                CommandSystem.PushCmd(buffer[i]);
            }
            
            // 立即執行所有命令
            CommandSystem.ExecuteAll();
            
            // 觸發卡片使用事件
            SimpleEventSystem.OnCardPlayed(actorId, HLATranslator.GetBasicAction(hla));
            
            return true;
        }
        
        // 處理玩家HLA
        public static bool ProcessPlayerHLA(byte playerId, byte targetId)
        {
            return ProcessHLA(playerId, targetId, s_playerHLA);
        }
        
        // 處理敵人HLA
        public static bool ProcessEnemyHLA(byte enemyId, byte targetId)
        {
            // 使用 EnemyIntentSystem 中的 CombatAI
            HLA enemyHLA = CombatAI.DecideForEnemy(enemyId);
            return ProcessHLA(enemyId, targetId, enemyHLA);
        }
        
        // 處理所有敵人HLA
        public static int ProcessAllEnemyHLAs(ReadOnlySpan<byte> enemyIds, byte playerTargetId)
        {
            int processedCount = 0;
            
            for (int i = 0; i < enemyIds.Length; i++)
            {
                byte enemyId = enemyIds[i];
                if (ActorManager.IsAlive(enemyId))
                {
                    if (ProcessEnemyHLA(enemyId, playerTargetId))
                    {
                        processedCount++;
                    }
                }
            }
            
            return processedCount;
        }
        
        // 重置系統
        public static void Reset()
        {
            s_playerHLA = HLA.BASIC_ATTACK;
        }
        
        // 調試方法
        public static void DebugPrintHLAs()
        {
            Console.WriteLine($"當前玩家HLA: {s_playerHLA}");
        }
    }
}