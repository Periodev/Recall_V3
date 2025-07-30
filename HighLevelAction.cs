// HighLevelAction.cs - 高階動作系統（清理版）
// ✅ 清理：移除舊的玩家輸入邏輯、意圖管理等重複功能
// ✅ 專注：HLA翻譯和基本HLA處理功能

using System;

namespace CombatCore
{
    // HLA翻譯引擎 - 核心功能保留
    public static class HLATranslator
    {
        // ✅ 保留：核心翻譯功能
        public static int TranslateHLA(HLA hla, byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            return hla switch
            {
                // 基礎動作 - 1:1映射
                HLA.BASIC_ATTACK => TranslateBasicAttack(srcId, targetId, buffer),
                HLA.BASIC_BLOCK => TranslateBasicBlock(srcId, buffer),
                HLA.BASIC_CHARGE => TranslateBasicCharge(srcId, buffer),
                
                // 組合動作 - 多命令序列
                HLA.HEAVY_STRIKE => TranslateHeavyStrike(srcId, targetId, buffer),
                HLA.SHIELD_BASH => TranslateShieldBash(srcId, targetId, buffer),
                HLA.COMBO_ATTACK => TranslateComboAttack(srcId, targetId, buffer),
                HLA.CHARGED_BLOCK => TranslateChargedBlock(srcId, buffer),
                HLA.POWER_CHARGE => TranslatePowerCharge(srcId, buffer),
                
                // 敵人動作
                HLA.ENEMY_AGGRESSIVE => TranslateEnemyAggressive(srcId, targetId, buffer),
                HLA.ENEMY_DEFENSIVE => TranslateEnemyDefensive(srcId, buffer),
                HLA.ENEMY_BERSERKER => TranslateEnemyBerserker(srcId, targetId, buffer),
                HLA.ENEMY_TURTLE => TranslateEnemyTurtle(srcId, buffer),
                
                // 未知HLA
                _ => 0
            };
        }
        
        // ==================== 基礎動作翻譯 ====================
        
        private static int TranslateBasicAttack(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 10);
            return 1;
        }
        
        private static int TranslateBasicBlock(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 5);
            return 1;
        }
        
        private static int TranslateBasicCharge(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 1);
            return 1;
        }
        
        // ==================== 組合動作翻譯 ====================
        
        private static int TranslateHeavyStrike(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 2);
            buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 15);
            return 2;
        }
        
        private static int TranslateShieldBash(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 3);
            buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);
            return 2;
        }
        
        private static int TranslateComboAttack(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);
            buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);
            return 2;
        }
        
        private static int TranslateChargedBlock(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 1);
            buffer[1] = CommandBuilder.MakeBlockCmd(srcId, 8);
            return 2;
        }
        
        private static int TranslatePowerCharge(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 2);
            buffer[1] = CommandBuilder.MakeChargeCmd(srcId, 1);
            return 2;
        }
        
        // ==================== 敵人動作翻譯 ====================
        
        private static int TranslateEnemyAggressive(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeChargeCmd(srcId, 1);
            buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 12);
            buffer[2] = CommandBuilder.MakeAttackCmd(srcId, targetId, 8);
            return 3;
        }
        
        private static int TranslateEnemyDefensive(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 10);
            buffer[1] = CommandBuilder.MakeChargeCmd(srcId, 2);
            return 2;
        }
        
        private static int TranslateEnemyBerserker(byte srcId, byte targetId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeAttackCmd(srcId, targetId, 6);
            buffer[1] = CommandBuilder.MakeAttackCmd(srcId, targetId, 6);
            buffer[2] = CommandBuilder.MakeAttackCmd(srcId, targetId, 6);
            return 3;
        }
        
        private static int TranslateEnemyTurtle(byte srcId, Span<AtomicCmd> buffer)
        {
            buffer[0] = CommandBuilder.MakeBlockCmd(srcId, 8);
            buffer[1] = CommandBuilder.MakeBlockCmd(srcId, 6);
            return 2;
        }
        
        // ✅ 新增：獲取HLA的動作類型（用於事件觸發等）
        public static int GetActionType(HLA hla)
        {
            return hla switch
            {
                HLA.BASIC_ATTACK => 1,
                HLA.BASIC_BLOCK => 2,
                HLA.BASIC_CHARGE => 3,
                HLA.HEAVY_STRIKE => 4,
                HLA.SHIELD_BASH => 5,
                HLA.COMBO_ATTACK => 6,
                HLA.CHARGED_BLOCK => 7,
                HLA.POWER_CHARGE => 8,
                HLA.ENEMY_AGGRESSIVE => 9,
                HLA.ENEMY_DEFENSIVE => 10,
                HLA.ENEMY_BERSERKER => 11,
                HLA.ENEMY_TURTLE => 12,
                _ => 0
            };
        }
        
        // ✅ 新增：檢查HLA是否需要目標
        public static bool RequiresTarget(HLA hla)
        {
            return hla switch
            {
                HLA.BASIC_ATTACK => true,
                HLA.HEAVY_STRIKE => true,
                HLA.SHIELD_BASH => true,
                HLA.COMBO_ATTACK => true,
                HLA.ENEMY_AGGRESSIVE => true,
                HLA.ENEMY_BERSERKER => true,
                _ => false
            };
        }
        
        // ✅ 新增：獲取HLA的預估效果（用於UI顯示）
        public static (string description, ushort estimatedValue) GetHLAInfo(HLA hla, byte actorId = 0)
        {
            // 如果有actorId，可以考慮當前狀態
            ushort chargeBonus = 0;
            if (actorId != 0 && ActorManager.IsAlive(actorId))
            {
                ref var actor = ref ActorManager.GetActor(actorId);
                chargeBonus = (ushort)(actor.Charge * CombatConstants.CHARGE_DAMAGE_BONUS);
            }
            
            return hla switch
            {
                HLA.BASIC_ATTACK => ("基礎攻擊", (ushort)(10 + chargeBonus)),
                HLA.BASIC_BLOCK => ("基礎格擋", 5),
                HLA.BASIC_CHARGE => ("基礎蓄力", 1),
                HLA.HEAVY_STRIKE => ("重擊", (ushort)(15 + 10 + chargeBonus)), // 15基礎 + 2蓄力*5
                HLA.SHIELD_BASH => ("盾擊", (ushort)(8 + chargeBonus)),
                HLA.COMBO_ATTACK => ("連擊", (ushort)(16 + chargeBonus)), // 8+8
                HLA.CHARGED_BLOCK => ("充能格擋", 8),
                HLA.POWER_CHARGE => ("強力蓄力", 3),
                HLA.ENEMY_AGGRESSIVE => ("敵人激進", (ushort)(20 + 5)), // 12+8，考慮蓄力
                HLA.ENEMY_DEFENSIVE => ("敵人防禦", 10),
                HLA.ENEMY_BERSERKER => ("敵人狂暴", (ushort)(18)), // 6+6+6
                HLA.ENEMY_TURTLE => ("敵人龜縮", 14), // 8+6護甲
                _ => ("未知", 0)
            };
        }
    }
    
    // ✅ 簡化：HLA處理系統（移除玩家輸入相關功能）
    public static class HLASystem
    {
        // ✅ 保留：核心HLA處理功能
        public static bool ProcessHLA(byte actorId, byte targetId, HLA hla)
        {
            if (!ActorManager.IsAlive(actorId))
            {
                Console.WriteLine($"❌ Actor {actorId} 已死亡，無法執行HLA {hla}");
                return false;
            }
            
            // 檢查是否需要目標
            if (HLATranslator.RequiresTarget(hla) && !ActorManager.IsAlive(targetId))
            {
                Console.WriteLine($"❌ 目標 {targetId} 無效，無法執行需要目標的HLA {hla}");
                return false;
            }
            
            // 翻譯為命令序列
            Span<AtomicCmd> buffer = stackalloc AtomicCmd[8];
            int cmdCount = HLATranslator.TranslateHLA(hla, actorId, targetId, buffer);
            
            if (cmdCount == 0)
            {
                Console.WriteLine($"❌ HLA {hla} 翻譯失敗");
                return false;
            }
            
            // 推入命令
            for (int i = 0; i < cmdCount; i++)
            {
                CommandSystem.PushCmd(buffer[i]);
            }
            
            // 立即執行（保持原有行為）
            CommandSystem.ExecuteAll();
            
            // ✅ 觸發事件：HLA執行完成
            SimpleEventSystem.OnCardPlayed(actorId, HLATranslator.GetActionType(hla));
            
            Console.WriteLine($"✅ Actor {actorId} 成功執行HLA {hla}，生成 {cmdCount} 個命令");
            return true;
        }
        
        // ✅ 保留：批次處理多個HLA
        public static int ProcessMultipleHLAs(ReadOnlySpan<(byte actorId, byte targetId, HLA hla)> hlaList)
        {
            int successCount = 0;
            
            foreach (var (actorId, targetId, hla) in hlaList)
            {
                if (ProcessHLA(actorId, targetId, hla))
                {
                    successCount++;
                }
            }
            
            return successCount;
        }
        
        // ✅ 新增：驗證HLA是否可執行
        public static bool CanExecuteHLA(byte actorId, byte targetId, HLA hla)
        {
            // 檢查執行者
            if (!ActorManager.IsAlive(actorId))
                return false;
                
            if (!ActorManager.CanAct(actorId))
                return false;
            
            // 檢查目標（如果需要）
            if (HLATranslator.RequiresTarget(hla) && !ActorManager.IsAlive(targetId))
                return false;
            
            // 可以添加更多檢查邏輯（如資源消耗等）
            
            return true;
        }
        
        // ✅ 新增：獲取HLA執行預覽
        public static bool GetHLAPreview(HLA hla, byte actorId, byte targetId, out string description, out ushort estimatedValue)
        {
            if (!CanExecuteHLA(actorId, targetId, hla))
            {
                description = "無法執行";
                estimatedValue = 0;
                return false;
            }
            
            (description, estimatedValue) = HLATranslator.GetHLAInfo(hla, actorId);
            return true;
        }
        
        // ✅ 重置系統
        public static void Reset()
        {
            // HLA系統本身是無狀態的，主要依賴其他系統
            // 這裡主要是為了保持接口一致性
            Console.WriteLine("🔄 HLA系統重置完成");
        }
        
        // ✅ 除錯功能
        public static void DebugPrintHLAInfo(HLA hla, byte actorId = 0)
        {
            Console.WriteLine($"=== HLA資訊: {hla} ===");
            
            var (description, estimatedValue) = HLATranslator.GetHLAInfo(hla, actorId);
            bool requiresTarget = HLATranslator.RequiresTarget(hla);
            int actionType = HLATranslator.GetActionType(hla);
            
            Console.WriteLine($"描述: {description}");
            Console.WriteLine($"預估數值: {estimatedValue}");
            Console.WriteLine($"需要目標: {(requiresTarget ? "是" : "否")}");
            Console.WriteLine($"動作類型: {actionType}");
            
            // 模擬翻譯
            Span<AtomicCmd> buffer = stackalloc AtomicCmd[8];
            int cmdCount = HLATranslator.TranslateHLA(hla, actorId, 1, buffer);
            Console.WriteLine($"生成命令數: {cmdCount}");
            
            for (int i = 0; i < cmdCount; i++)
            {
                var cmd = buffer[i];
                Console.WriteLine($"  命令 {i}: {cmd.Op} (Src:{cmd.SrcId}, Target:{cmd.TargetId}, Value:{cmd.Value})");
            }
        }
    }
    
    // ❌ 移除：原有的玩家輸入相關功能
    // - SetPlayerHLA()
    // - GetPlayerHLA()
    // - ProcessPlayerHLA()
    // - 玩家HLA暫存相關邏輯
    
    // ❌ 移除：敵人意圖管理功能（移到EnemyIntentSystem）
    // - SetEnemyHLA()
    // - GetEnemyHLA()
    // - GetEnemyIntent()
    // - GetAllEnemyIntents()
    // - DebugPrintEnemyIntents()
    
    // ❌ 移除：CombatAI類（移到EnemyIntentSystem）
    // - DecideForEnemy()
    // - DecideAndDeclareForAllEnemies()
    // - 各種AI決策邏輯
    
    // ✅ 保留但簡化：HLA常數和輔助功能
    public static class HLAConstants
    {
        // 基礎動作傷害/效果值
        public const ushort BASIC_ATTACK_DAMAGE = 10;
        public const ushort BASIC_BLOCK_AMOUNT = 5;
        public const byte BASIC_CHARGE_AMOUNT = 1;
        
        // 組合動作傷害/效果值
        public const ushort HEAVY_STRIKE_BASE_DAMAGE = 15;
        public const byte HEAVY_STRIKE_CHARGE = 2;
        public const ushort SHIELD_BASH_BLOCK = 3;
        public const ushort SHIELD_BASH_DAMAGE = 8;
        public const ushort COMBO_ATTACK_DAMAGE = 8;
        
        // 敵人動作基礎值
        public const ushort ENEMY_AGGRESSIVE_DAMAGE_1 = 12;
        public const ushort ENEMY_AGGRESSIVE_DAMAGE_2 = 8;
        public const ushort ENEMY_DEFENSIVE_BLOCK = 10;
        public const byte ENEMY_DEFENSIVE_CHARGE = 2;
        public const ushort ENEMY_BERSERKER_DAMAGE = 6;
        public const ushort ENEMY_TURTLE_BLOCK_1 = 8;
        public const ushort ENEMY_TURTLE_BLOCK_2 = 6;
        
        // ✅ 新增：獲取HLA的基礎傷害值
        public static ushort GetBaseDamage(HLA hla)
        {
            return hla switch
            {
                HLA.BASIC_ATTACK => BASIC_ATTACK_DAMAGE,
                HLA.HEAVY_STRIKE => HEAVY_STRIKE_BASE_DAMAGE,
                HLA.SHIELD_BASH => SHIELD_BASH_DAMAGE,
                HLA.COMBO_ATTACK => COMBO_ATTACK_DAMAGE * 2, // 兩次攻擊
                HLA.ENEMY_AGGRESSIVE => ENEMY_AGGRESSIVE_DAMAGE_1 + ENEMY_AGGRESSIVE_DAMAGE_2,
                HLA.ENEMY_BERSERKER => ENEMY_BERSERKER_DAMAGE * 3, // 三次攻擊
                _ => 0
            };
        }
        
        // ✅ 新增：檢查HLA是否為攻擊類型
        public static bool IsAttackType(HLA hla)
        {
            return hla switch
            {
                HLA.BASIC_ATTACK or HLA.HEAVY_STRIKE or HLA.SHIELD_BASH or 
                HLA.COMBO_ATTACK or HLA.ENEMY_AGGRESSIVE or HLA.ENEMY_BERSERKER => true,
                _ => false
            };
        }
        
        // ✅ 新增：檢查HLA是否為防禦類型
        public static bool IsDefensiveType(HLA hla)
        {
            return hla switch
            {
                HLA.BASIC_BLOCK or HLA.CHARGED_BLOCK or 
                HLA.ENEMY_DEFENSIVE or HLA.ENEMY_TURTLE => true,
                _ => false
            };
        }
        
        // ✅ 新增：檢查HLA是否為蓄力類型
        public static bool IsChargeType(HLA hla)
        {
            return hla switch
            {
                HLA.BASIC_CHARGE or HLA.POWER_CHARGE => true,
                _ => false
            };
        }
    }
}