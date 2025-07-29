// HLA.cs - 高階動作系統
// 戰術抽象：HLA → AtomicCmd[]序列翻譯

using System;

namespace CombatCore
{
    // HLA翻譯引擎 - 戰術意圖轉換為命令序列
    public static class HLATranslator
    {
        // ✅ Switch表達式分派 - HLA翻譯核心
        public static HLATranslationResult TranslateHLA(
            HLA hla, byte srcId, byte targetId, Span<AtomicCmd> buffer)  // ✅ Span<T>參數
        {
            return hla switch
            {
                // 基礎動作 (1:1翻譯)
                HLA.BASIC_ATTACK => TranslateBasicAttack(srcId, targetId, buffer),
                HLA.BASIC_BLOCK => TranslateBasicBlock(srcId, buffer),
                HLA.BASIC_CHARGE => TranslateBasicCharge(srcId, buffer),
                
                // 組合動作 (1:N翻譯)
                HLA.HEAVY_STRIKE => TranslateHeavyStrike(srcId, targetId, buffer),
                HLA.SHIELD_BASH => TranslateShieldBash(srcId, targetId, buffer),
                HLA.COMBO_ATTACK => TranslateComboAttack(srcId, targetId, buffer),
                HLA.CHARGED_BLOCK => TranslateChargedBlock(srcId, buffer),
                HLA.POWER_CHARGE => TranslatePowerCharge(srcId, buffer),
                
                // 敵人動作 (複雜組合)
                HLA.ENEMY_AGGRESSIVE => TranslateEnemyAggressive(srcId, targetId, buffer),
                HLA.ENEMY_DEFENSIVE => TranslateEnemyDefensive(srcId, buffer),
                HLA.ENEMY_BERSERKER => TranslateEnemyBerserker(srcId, targetId, buffer),
                HLA.ENEMY_TURTLE => TranslateEnemyTurtle(srcId, buffer),
                
                _ => HLATranslationResult.FAILED
            };
        }
        
        // ==================== 基礎動作翻譯 ====================
        
        private static HLATranslationResult TranslateBasicAttack(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 10);
            return new HLATranslationResult(true, 1);
        }
        
        private static HLATranslationResult TranslateBasicBlock(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 5);
            return new HLATranslationResult(true, 1);
        }
        
        private static HLATranslationResult TranslateBasicCharge(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 1);
            return new HLATranslationResult(true, 1);
        }
        
        // ==================== 組合動作翻譯 ====================
        
        // 重擊：蓄力 + 強力攻擊
        private static HLATranslationResult TranslateHeavyStrike(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 2);      // 蓄力2點
            buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 15);  // 基礎攻擊(會被蓄力加成)
            return new HLATranslationResult(true, 2);
        }
        
        // 盾擊：格擋 + 攻擊
        private static HLATranslationResult TranslateShieldBash(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 3);       // 先獲得護甲
            buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);   // 較弱的攻擊
            return new HLATranslationResult(true, 2);
        }
        
        // 連擊：兩次攻擊
        private static HLATranslationResult TranslateComboAttack(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 7);   // 第一擊
            buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 7);   // 第二擊
            return new HLATranslationResult(true, 2);
        }
        
        // 充能護盾：蓄力 + 格擋
        private static HLATranslationResult TranslateChargedBlock(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 1);      // 蓄力為下回合準備
            buffer[1] = CommandBuilder.MakeBlockCmd(srcId, 8);       // 強力護盾
            return new HLATranslationResult(true, 2);
        }
        
        // 強力蓄力：雙重蓄力
        private static HLATranslationResult TranslatePowerCharge(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 2);      // 一次蓄力2點
            buffer[1] = CommandBuilder.MakeChargeCmd(srcId, 1);      // 再蓄力1點，總共3點
            return new HLATranslationResult(true, 2);
        }
        
        // ==================== 敵人動作翻譯 ====================
        
        // 敵人激進：蓄力 + 雙重攻擊
        private static HLATranslationResult TranslateEnemyAggressive(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 1);      // 蓄力
            buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 12);  // 強力攻擊
            buffer[2] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);   // 追加攻擊
            return new HLATranslationResult(true, 3);
        }
        
        // 敵人防禦：格擋 + 蓄力
        private static HLATranslationResult TranslateEnemyDefensive(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 10);      // 強力護盾
            buffer[1] = CommandBuilder.MakeChargeCmd(srcId, 2);      // 為下回合蓄力
            return new HLATranslationResult(true, 2);
        }
        
        // 敵人狂暴：三連擊
        private static HLATranslationResult TranslateEnemyBerserker(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 6);   // 第一擊
            buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 6);   // 第二擊  
            buffer[2] = CommandBuilder.MakeAttackCmd(srcId, targetId, 6);   // 第三擊
            return new HLATranslationResult(true, 3);
        }
        
        // 敵人龜縮：雙重格擋
        private static HLATranslationResult TranslateEnemyTurtle(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 8);       // 第一層護盾
            buffer[1] = CommandBuilder.MakeBlockCmd(srcId, 6);       // 第二層護盾
            return new HLATranslationResult(true, 2);
        }
    }
    
    // HLA處理系統 - 管理HLA的暫存和處理
    public static class HLASystem
    {
        // 簡化的HLA暫存 - 避免複雜緩存機制
        private static HLA s_playerHLA = HLA.BASIC_ATTACK;
        private static readonly Dictionary<byte, HLA> s_enemyHLAs = new();
        
        // 設置玩家HLA
        public static void SetPlayerHLA(HLA hla) => s_playerHLA = hla;
        public static HLA GetPlayerHLA() => s_playerHLA;
        
        // 設置敵人HLA
        public static void SetEnemyHLA(byte enemyId, HLA hla) => s_enemyHLAs[enemyId] = hla;
        public static HLA GetEnemyHLA(byte enemyId) => s_enemyHLAs.GetValueOrDefault(enemyId, HLA.BASIC_ATTACK);
        
        // 處理HLA - 翻譯並推入命令佇列
        public static bool ProcessHLA(byte actorId, byte targetId, HLA hla)
        {
            if (!ActorManager.IsAlive(actorId)) return false;
            
            // ✅ stackalloc工作記憶體
            Span<AtomicCmd> buffer = stackalloc AtomicCmd[CombatConstants.MAX_HLA_TRANSLATION];
            
            var result = HLATranslator.TranslateHLA(hla, actorId, targetId, buffer);
            
            if (!result.Success) return false;
            
            // 推入翻譯出的命令序列
            for (int i = 0; i < result.CommandCount; i++)
            {
                CommandSystem.PushCmd(buffer[i]);
            }
            
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
            var enemyHLA = GetEnemyHLA(enemyId);
            return ProcessHLA(enemyId, targetId, enemyHLA);
        }
        
        // 批次處理多個敵人HLA
        public static int ProcessAllEnemyHLAs(ReadOnlySpan<byte> enemyIds, byte playerTargetId)
        {
            int processedCount = 0;
            foreach (byte enemyId in enemyIds)
            {
                if (ProcessEnemyHLA(enemyId, playerTargetId))
                {
                    processedCount++;
                }
            }
            return processedCount;
        }
        
        // 清理HLA暫存
        public static void Reset()
        {
            s_playerHLA = HLA.BASIC_ATTACK;
            s_enemyHLAs.Clear();
        }
        
        // 除錯資訊
        public static void DebugPrintHLAs()
        {
            Console.WriteLine($"玩家HLA: {s_playerHLA}");
            foreach (var kvp in s_enemyHLAs)
            {
                Console.WriteLine($"敵人 {kvp.Key}: {kvp.Value}");
            }
        }
    }
    
    // 簡單的戰鬥AI - 敵人HLA決策
    public static class CombatAI
    {
        private static readonly Random s_random = new();
        
        // 為敵人決策HLA
        public static HLA DecideForEnemy(byte enemyId)
        {
            if (!ActorManager.IsAlive(enemyId)) return HLA.BASIC_ATTACK;
            
            ref var enemy = ref ActorManager.GetActor(enemyId);
            
            // 簡單AI邏輯
            return enemy.Type switch
            {
                ActorType.ENEMY_BASIC => DecideBasicEnemyHLA(in enemy),
                ActorType.ENEMY_ELITE => DecideEliteEnemyHLA(in enemy),
                ActorType.ENEMY_BOSS => DecideBossEnemyHLA(in enemy),
                _ => HLA.BASIC_ATTACK
            };
        }
        
        private static HLA DecideBasicEnemyHLA(in Actor enemy)
        {
            // 血量低時防禦，否則攻擊
            if (enemy.HP < enemy.MaxHP / 3)
            {
                return s_random.NextDouble() < 0.7 ? HLA.ENEMY_DEFENSIVE : HLA.BASIC_BLOCK;
            }
            
            // 有蓄力時使用重擊，否則普通攻擊
            if (enemy.Charge > 0)
            {
                return HLA.HEAVY_STRIKE;
            }
            
            // 隨機選擇基礎動作
            return s_random.NextDouble() switch
            {
                < 0.5 => HLA.BASIC_ATTACK,
                < 0.8 => HLA.BASIC_CHARGE,
                _ => HLA.BASIC_BLOCK
            };
        }
        
        private static HLA DecideEliteEnemyHLA(in Actor enemy)
        {
            // 精英敵人使用更複雜的策略
            if (enemy.HP < enemy.MaxHP / 2)
            {
                return s_random.NextDouble() < 0.6 ? HLA.ENEMY_AGGRESSIVE : HLA.SHIELD_BASH;
            }
            
            return s_random.NextDouble() switch
            {
                < 0.3 => HLA.ENEMY_AGGRESSIVE,
                < 0.5 => HLA.COMBO_ATTACK,
                < 0.7 => HLA.HEAVY_STRIKE,
                _ => HLA.CHARGED_BLOCK
            };
        }
        
        private static HLA DecideBossEnemyHLA(in Actor enemy)
        {
            // BOSS使用最複雜的策略
            if (enemy.HP < enemy.MaxHP / 4)
            {
                // 血量極低時狂暴
                return HLA.ENEMY_BERSERKER;
            }
            
            if (enemy.HP < enemy.MaxHP / 2)
            {
                // 血量較低時激進
                return s_random.NextDouble() < 0.8 ? HLA.ENEMY_AGGRESSIVE : HLA.ENEMY_TURTLE;
            }
            
            // 血量健康時平衡策略
            return s_random.NextDouble() switch
            {
                < 0.25 => HLA.ENEMY_AGGRESSIVE,
                < 0.4 => HLA.ENEMY_DEFENSIVE,
                < 0.6 => HLA.COMBO_ATTACK,
                < 0.8 => HLA.HEAVY_STRIKE,
                _ => HLA.POWER_CHARGE
            };
        }
        
        // 批次為所有敵人決策HLA
        public static void DecideForAllEnemies()
        {
            // ✅ stackalloc工作記憶體
            Span<byte> enemyBuffer = stackalloc byte[CombatConstants.MAX_ACTORS];
            int enemyCount = ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, enemyBuffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, enemyBuffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, enemyBuffer[enemyCount..]);
            
            for (int i = 0; i < enemyCount; i++)
            {
                byte enemyId = enemyBuffer[i];
                HLA decidedHLA = DecideForEnemy(enemyId);
                HLASystem.SetEnemyHLA(enemyId, decidedHLA);
            }
        }
    }
}