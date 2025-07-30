// SimplifiedCardSystem.cs - 極度簡化的行動卡系統
// A/B/C 各最多3張，總共6張牌組，無棄牌堆，每次重洗

using System;
using System.Collections.Generic;

namespace CombatCore
{
    // 基礎行動類型 - 只有A/B/C三種
    public enum BasicAction : byte
    {
        ATTACK = 1,     // A - 攻擊
        BLOCK = 2,      // B - 格擋  
        CHARGE = 3,     // C - 蓄力
    }
    
    // 簡化的行動卡 - 只有基本資訊
    public readonly struct SimpleCard
    {
        public readonly BasicAction Action;
        public readonly char Symbol;
        
        public SimpleCard(BasicAction action)
        {
            Action = action;
            Symbol = action switch
            {
                BasicAction.ATTACK => 'A',
                BasicAction.BLOCK => 'B', 
                BasicAction.CHARGE => 'C',
                _ => '?'
            };
        }
        
        public string Name => Action switch
        {
            BasicAction.ATTACK => "攻擊",
            BasicAction.BLOCK => "格擋",
            BasicAction.CHARGE => "蓄力",
            _ => "未知"
        };
        
        public bool RequiresTarget => Action == BasicAction.ATTACK;
        
        // 轉換為HLA執行
        public HLA ToHLA() => Action switch
        {
            BasicAction.ATTACK => HLA.BASIC_ATTACK,
            BasicAction.BLOCK => HLA.BASIC_BLOCK,
            BasicAction.CHARGE => HLA.BASIC_CHARGE,
            _ => HLA.BASIC_ATTACK
        };
    }
    
    // 牌組配置
    public struct DeckConfig
    {
        public byte AttackCards;    // A卡數量 (0-3)
        public byte BlockCards;     // B卡數量 (0-3)  
        public byte ChargeCards;    // C卡數量 (0-3)
        
        public DeckConfig(byte attack = 1, byte block = 1, byte charge = 1)
        {
            AttackCards = Math.Min(attack, (byte)3);
            BlockCards = Math.Min(block, (byte)3);
            ChargeCards = Math.Min(charge, (byte)3);
        }
        
        public int TotalCards => AttackCards + BlockCards + ChargeCards;
        public bool IsValid => TotalCards <= 6 && TotalCards > 0;
        
        // 預設配置
        public static readonly DeckConfig DEFAULT = new(1, 1, 1);           // 初始：各1張
        public static readonly DeckConfig BALANCED = new(2, 2, 2);          // 平衡：各2張
        public static readonly DeckConfig AGGRESSIVE = new(3, 2, 1);        // 激進：3A2B1C
        public static readonly DeckConfig DEFENSIVE = new(1, 3, 2);         // 防禦：1A3B2C
    }
    
    // 簡化的牌組管理器
    public static class SimpleDeckManager
    {
        private static DeckConfig s_currentConfig = DeckConfig.DEFAULT;
        private static readonly List<SimpleCard> s_deck = new();
        private static readonly List<SimpleCard> s_hand = new();
        private static readonly Random s_random = new();
        
        public const int MAX_HAND_SIZE = 6;  // 最大手牌數等於牌組大小
        
        // 設置牌組配置
        public static bool SetDeckConfig(DeckConfig config)
        {
            if (!config.IsValid)
            {
                Console.WriteLine($"無效的牌組配置：總計{config.TotalCards}張卡，超過6張限制");
                return false;
            }
            
            s_currentConfig = config;
            RebuildDeck();
            return true;
        }
        
        // 重建牌組
        private static void RebuildDeck()
        {
            s_deck.Clear();
            
            // 添加A卡
            for (int i = 0; i < s_currentConfig.AttackCards; i++)
                s_deck.Add(new SimpleCard(BasicAction.ATTACK));
                
            // 添加B卡  
            for (int i = 0; i < s_currentConfig.BlockCards; i++)
                s_deck.Add(new SimpleCard(BasicAction.BLOCK));
                
            // 添加C卡
            for (int i = 0; i < s_currentConfig.ChargeCards; i++)
                s_deck.Add(new SimpleCard(BasicAction.CHARGE));
        }
        
        // 戰鬥開始時洗牌並抽滿手牌
        public static void StartCombat()
        {
            s_hand.Clear();
            ShuffleAndDrawAll();
        }
        
        // 洗牌並抽光所有卡牌（每回合重洗）
        public static void ShuffleAndDrawAll()
        {
            s_hand.Clear();
            
            // 複製牌組
            var tempDeck = new List<SimpleCard>(s_deck);
            
            // 洗牌（Fisher-Yates算法）
            for (int i = tempDeck.Count - 1; i > 0; i--)
            {
                int j = s_random.Next(i + 1);
                (tempDeck[i], tempDeck[j]) = (tempDeck[j], tempDeck[i]);
            }
            
            // 全部抽到手牌
            s_hand.AddRange(tempDeck);
        }
        
        // 使用卡牌（從手牌移除，不放入棄牌堆）
        public static bool UseCard(int handIndex, byte targetId = 0)
        {
            if (handIndex < 0 || handIndex >= s_hand.Count)
                return false;
            
            var card = s_hand[handIndex];
            
            // 執行卡牌效果
            var playerId = GetPlayerId();
            if (playerId == 255) return false;
            
            bool success = HLASystem.ProcessHLA(playerId, targetId, card.ToHLA());
            
            if (success)
            {
                s_hand.RemoveAt(handIndex);  // 直接移除，不進棄牌堆
            }
            
            return success;
        }
        
        // 獲取當前手牌
        public static ReadOnlySpan<SimpleCard> GetHand()
        {
            return s_hand.ToArray().AsSpan();
        }
        
        // 回合結束時重洗（如果手牌為空）
        public static void OnTurnEnd()
        {
            if (s_hand.Count == 0)
            {
                Console.WriteLine("手牌用完，重新洗牌");
                ShuffleAndDrawAll();
            }
        }
        
        // 獲取牌組資訊
        public static DeckConfig GetCurrentConfig() => s_currentConfig;
        public static int GetHandSize() => s_hand.Count;
        public static int GetDeckSize() => s_deck.Count;
        
        // 獲取玩家ID
        private static byte GetPlayerId()
        {
            Span<byte> playerBuffer = stackalloc byte[16];
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, playerBuffer);
            return playerCount > 0 ? playerBuffer[0] : (byte)255;
        }
        
        // 除錯顯示
        public static void DebugPrintDeckConfig()
        {
            Console.WriteLine($"牌組配置: {s_currentConfig.AttackCards}A + {s_currentConfig.BlockCards}B + {s_currentConfig.ChargeCards}C = {s_currentConfig.TotalCards}張");
        }
        
        public static void DebugPrintHand()
        {
            Console.WriteLine("=== 當前手牌 ===");
            if (s_hand.Count == 0)
            {
                Console.WriteLine("手牌為空");
                return;
            }
            
            // 統計手牌
            int attackCount = 0, blockCount = 0, chargeCount = 0;
            for (int i = 0; i < s_hand.Count; i++)
            {
                var card = s_hand[i];
                Console.WriteLine($"{i}: [{card.Symbol}] {card.Name}");
                
                switch (card.Action)
                {
                    case BasicAction.ATTACK: attackCount++; break;
                    case BasicAction.BLOCK: blockCount++; break;
                    case BasicAction.CHARGE: chargeCount++; break;
                }
            }
            
            Console.WriteLine($"手牌統計: {attackCount}A + {blockCount}B + {chargeCount}C = {s_hand.Count}張");
        }
        
        public static void DebugPrintDeck()
        {
            Console.WriteLine("=== 牌組構成 ===");
            int attackCount = 0, blockCount = 0, chargeCount = 0;
            
            foreach (var card in s_deck)
            {
                switch (card.Action)
                {
                    case BasicAction.ATTACK: attackCount++; break;
                    case BasicAction.BLOCK: blockCount++; break;
                    case BasicAction.CHARGE: chargeCount++; break;
                }
            }
            
            Console.WriteLine($"牌組: {attackCount}A + {blockCount}B + {chargeCount}C = {s_deck.Count}張");
        }
    }
    
    // 簡化的卡牌使用輔助
    public static class SimpleCardHelper
    {
        // 使用指定索引的卡牌
        public static bool PlayCard(int handIndex, byte targetId = 0)
        {
            var hand = SimpleDeckManager.GetHand();
            if (handIndex < 0 || handIndex >= hand.Length)
            {
                Console.WriteLine($"無效的卡牌索引: {handIndex}");
                return false;
            }
            
            var card = hand[handIndex];
            
            // 檢查是否需要目標
            if (card.RequiresTarget && targetId == 0)
            {
                Console.WriteLine($"卡牌 {card.Name} 需要選擇目標");
                return false;
            }
            
            bool success = SimpleDeckManager.UseCard(handIndex, targetId);
            if (success)
            {
                Console.WriteLine($"使用卡牌: [{card.Symbol}] {card.Name}");
            }
            
            return success;
        }
        
        // 使用第一張指定類型的卡牌
        public static bool PlayCardByType(BasicAction action, byte targetId = 0)
        {
            var hand = SimpleDeckManager.GetHand();
            
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i].Action == action)
                {
                    return PlayCard(i, targetId);
                }
            }
            
            Console.WriteLine($"手牌中沒有 {action} 類型的卡牌");
            return false;
        }
        
        // 自動選擇敵人目標
        public static byte GetDefaultEnemyTarget()
        {
            Span<byte> enemyBuffer = stackalloc byte[16];
            int enemyCount = 0;
            
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, enemyBuffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, enemyBuffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, enemyBuffer[enemyCount..]);
            
            return enemyCount > 0 ? enemyBuffer[0] : (byte)0;
        }
        
        // 簡單的自動玩牌AI
        public static void AutoPlayTurn()
        {
            var hand = SimpleDeckManager.GetHand();
            Console.WriteLine($"自動玩牌開始，手牌數: {hand.Length}");
            
            // 優先級：攻擊 > 蓄力 > 格擋
            while (SimpleDeckManager.GetHandSize() > 0)
            {
                bool played = false;
                
                // 1. 嘗試攻擊
                byte target = GetDefaultEnemyTarget();
                if (target != 0 && PlayCardByType(BasicAction.ATTACK, target))
                {
                    played = true;
                }
                // 2. 嘗試蓄力
                else if (PlayCardByType(BasicAction.CHARGE))
                {
                    played = true;
                }
                // 3. 嘗試格擋
                else if (PlayCardByType(BasicAction.BLOCK))
                {
                    played = true;
                }
                
                if (!played)
                {
                    Console.WriteLine("無法繼續自動玩牌");
                    break;
                }
                
                // 執行命令
                CommandSystem.ExecuteAll();
            }
            
            Console.WriteLine("自動玩牌結束");
        }
    }
}