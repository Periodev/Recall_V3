// CombatConstants.cs - 系統基礎常數
// 最低限度C風格設計 - 簡潔實用優先

using System;

namespace CombatCore
{
    // 系統限制常數
    public static class CombatConstants
    {
        // 核心容量限制
        public const int MAX_ACTORS = 64;           // Actor池大小
        public const int MAX_COMMANDS = 256;        // 命令佇列大小
        public const int MAX_HLA_TRANSLATION = 16;  // HLA翻譯緩衝區大小
        
        // 遊戲平衡常數
        public const int CHARGE_DAMAGE_BONUS = 5;   // 每點蓄力的傷害加成
        public const int BASE_AP = 1;               // 每回合基礎行動點
        public const ushort MAX_HP = 999;           // 最大生命值
        public const ushort MAX_BLOCK = 999;        // 最大護甲值
        public const byte MAX_CHARGE = 10;          // 最大蓄力值
        
        // 狀態持續時間
        public const byte DEFAULT_STATUS_DURATION = 3;
        public const byte MAX_STATUS_DURATION = 10;
    }
    
    // 角色類型
    public enum ActorType : byte
    {
        PLAYER = 1,
        ENEMY_BASIC = 2,
        ENEMY_ELITE = 3,
        ENEMY_BOSS = 4
    }
    
    // 狀態標記 - 使用enum而非位運算，簡化管理
    public enum StatusFlags : byte
    {
        NONE = 0,
        STUNNED = 1,        // 暈眩，無法行動
        POISONED = 2,       // 中毒，回合結束扣血
        SHIELDED = 3,       // 護盾，減免傷害
        CHARGED = 4,        // 充能，攻擊力提升
        FROZEN = 5,         // 冰凍，無法行動且失去護甲
    }
    
    // 原子命令操作碼 - 控制在30個以內
    public enum CmdOp : byte
    {
        // 基礎操作 (1-10)
        NOP = 0,                // 空操作
        ATTACK = 1,             // 攻擊
        BLOCK = 2,              // 格擋
        CHARGE = 3,             // 蓄力
        HEAL = 4,               // 治療
        
        // 狀態操作 (11-15)
        STATUS_ADD = 11,        // 添加狀態
        STATUS_REMOVE = 12,     // 移除狀態
        DEFLECT = 13,           // 反彈傷害
        
        // 系統操作 (16-20)
        TURN_END_CLEANUP = 16,  // 回合結束清理
        ACTOR_DEATH = 17,       // 角色死亡
        PHASE_TRANSITION = 18,  // 階段轉換
        
        // Echo相關操作 (21-25) - 預留給未來Echo系統
        ECHO_GENERATE = 21,     // 生成Echo
        ECHO_EXECUTE = 22,      // 執行Echo
        REVERB_PUSH = 23,       // 推入回響
        
        // 特殊操作 (26-30) - 未來擴展
        TELEPORT = 26,          // 傳送
        MULTI_ATTACK = 27,      // 多段攻擊
        AREA_ATTACK = 28,       // 範圍攻擊
        DRAIN = 29,             // 吸血
        COUNTER = 30,           // 反擊
    }
    
    // HLA高階動作碼 - 戰術意圖表達
    public enum HLA : byte
    {
        // 基礎動作 (1-10)
        BASIC_ATTACK = 1,       // 基礎攻擊
        BASIC_BLOCK = 2,        // 基礎格擋  
        BASIC_CHARGE = 3,       // 基礎蓄力
        
        // 組合動作 (20-40)
        HEAVY_STRIKE = 20,      // 重擊 (CHARGE + ATTACK)
        SHIELD_BASH = 21,       // 盾擊 (BLOCK + ATTACK) 
        COMBO_ATTACK = 22,      // 連擊 (ATTACK + ATTACK)
        CHARGED_BLOCK = 23,     // 充能護盾 (CHARGE + BLOCK)
        POWER_CHARGE = 24,      // 強力蓄力 (CHARGE + CHARGE)
        
        // 敵人動作 (60-80)
        ENEMY_AGGRESSIVE = 60,  // 敵人激進 (CHARGE + ATTACK + ATTACK)
        ENEMY_DEFENSIVE = 61,   // 敵人防禦 (BLOCK + CHARGE)
        ENEMY_BERSERKER = 62,   // 敵人狂暴 (ATTACK + ATTACK + ATTACK)
        ENEMY_TURTLE = 63,      // 敵人龜縮 (BLOCK + BLOCK)
        
        // 特殊動作 (90-99) - 未來擴展
        SPECIAL_HEAL = 90,      // 特殊治療
        SPECIAL_BUFF = 91,      // 特殊增益
        SPECIAL_DEBUFF = 92,    // 特殊減益
    }
    
    // Phase階段ID
    public enum PhaseId : byte
    {
        ENEMY_INTENT = 1,       // 敵人意圖階段
        PLAYER_PHASE = 2,       // 玩家行動階段
        ENEMY_PHASE = 3,        // 敵人行動階段
        CLEANUP = 4,            // 清理階段
    }
    
    // Phase執行步驟
    public enum PhaseStep : byte
    {
        INIT = 1,               // 初始化
        INPUT = 2,              // 等待輸入
        PROCESS = 3,            // 處理行動
        EXECUTE = 4,            // 執行命令
        END = 5,                // 結束處理
    }
    
    // Phase執行結果
    public enum PhaseResult : byte
    {
        CONTINUE = 1,           // 繼續當前階段
        NEXT_STEP = 2,          // 進入下一步驟
        NEXT_PHASE = 3,         // 進入下一階段
        WAIT_INPUT = 4,         // 等待玩家輸入
        COMBAT_END = 5,         // 戰鬥結束
        ERROR = 99,             // 錯誤狀態
    }
    
    // HLA翻譯結果
    public readonly struct HLATranslationResult
    {
        public readonly bool Success;
        public readonly int CommandCount;
        
        public HLATranslationResult(bool success, int commandCount)
        {
            Success = success;
            CommandCount = commandCount;
        }
        
        public static readonly HLATranslationResult FAILED = new(false, 0);
    }
}