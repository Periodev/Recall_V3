// SimplifiedCardSystem.cs - 極度簡化的行動卡系統（增強版）
// A/B/C 各最多3張，總共6張牌組，無棄牌堆，每次重洗
// ✅ 增強：更好的除錯信息、自動重洗、錯誤處理、統計功能

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
        
        // ✅ 增強：獲取卡牌描述
        public string Description => Action switch
        {
            BasicAction.ATTACK => "對敵人造成10點傷害",
            BasicAction.BLOCK => "獲得5點護甲",
            BasicAction.CHARGE => "獲得1點蓄力，增加下次攻擊傷害",
            _ => "未知效果"
        };
        
        // 轉換為HLA執行
        public HLA ToHLA() => Action switch
        {
            BasicAction.ATTACK => HLA.BASIC_ATTACK,
            BasicAction.BLOCK => HLA.BASIC_BLOCK,
            BasicAction.CHARGE => HLA.BASIC_CHARGE,
            _ => HLA.BASIC_ATTACK
        };
        
        // ✅ 增強：卡牌相等比較
        public bool Equals(SimpleCard other) => Action == other.Action;
        public override bool Equals(object obj) => obj is SimpleCard other && Equals(other);
        public override int GetHashCode() => (int)Action;
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
        public static readonly DeckConfig CHARGE_FOCUSED = new(2, 1, 3);    // 蓄力導向：2A1B3C
        
        // ✅ 增強：配置描述
        public string GetDescription()
        {
            return $"{AttackCards}A + {BlockCards}B + {ChargeCards}C = {TotalCards}張";
        }
        
        // ✅ 增強：配置驗證
        public bool TryValidate(out string errorMessage)
        {
            if (TotalCards == 0)
            {
                errorMessage = "牌組不能為空";
                return false;
            }
            
            if (TotalCards > 6)
            {
                errorMessage = $"牌組總數不能超過6張，當前{TotalCards}張";
                return false;
            }
            
            if (AttackCards > 3 || BlockCards > 3 || ChargeCards > 3)
            {
                errorMessage = "單一類型卡牌不能超過3張";
                return false;
            }
            
            errorMessage = "";
            return true;
        }
    }
    
    // ✅ 增強：卡牌使用統計
    public struct CardUsageStats
    {
        public int AttackUsed;
        public int BlockUsed;
        public int ChargeUsed;
        public int TotalUsed;
        public int ShuffleCount;
        
        public void RecordCardUse(BasicAction action)
        {
            switch (action)
            {
                case BasicAction.ATTACK: AttackUsed++; break;
                case BasicAction.BLOCK: BlockUsed++; break;
                case BasicAction.CHARGE: ChargeUsed++; break;
            }
            TotalUsed++;
        }
        
        public void RecordShuffle() => ShuffleCount++;
        
        public void Reset()
        {
            AttackUsed = BlockUsed = ChargeUsed = TotalUsed = ShuffleCount = 0;
        }
        
        public string GetSummary()
        {
            return $"使用統計: {AttackUsed}A + {BlockUsed}B + {ChargeUsed}C = {TotalUsed}張總計, 洗牌{ShuffleCount}次";
        }
    }
    
    // 簡化的牌組管理器
    public static class SimpleDeckManager
    {
        private static DeckConfig s_currentConfig = DeckConfig.DEFAULT;
        private static readonly List<SimpleCard> s_deck = new();
        private static readonly List<SimpleCard> s_hand = new();
        private static readonly Random s_random = new();
        private static CardUsageStats s_stats = new();
        private static bool s_isInitialized = false;
        
        public const int MAX_HAND_SIZE = 6;  // 最大手牌數等於牌組大小
        
        // ✅ 增強：初始化檢查
        private static void EnsureInitialized()
        {
            if (!s_isInitialized)
            {
                Console.WriteLine("⚠️ 卡牌系統未初始化，使用預設配置");
                SetDeckConfig(DeckConfig.DEFAULT);
            }
        }
        
        // 設置牌組配置
        public static bool SetDeckConfig(DeckConfig config)
        {
            if (!config.TryValidate(out string errorMessage))
            {
                Console.WriteLine($"❌ 無效的牌組配置：{errorMessage}");
                return false;
            }
            
            s_currentConfig = config;
            s_isInitialized = true;
            RebuildDeck();
            
            Console.WriteLine($"✅ 牌組配置設定完成：{config.GetDescription()}");
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
                
            Console.WriteLine($"📦 牌組重建完成：{s_deck.Count}張卡牌");
        }
        
        // 戰鬥開始時洗牌並抽滿手牌
        public static void StartCombat()
        {
            EnsureInitialized();
            s_hand.Clear();
            s_stats.Reset();
            ShuffleAndDrawAll();
            Console.WriteLine("🎯 戰鬥開始，卡牌系統就緒");
        }
        
        // 洗牌並抽光所有卡牌（每回合重洗）
        public static void ShuffleAndDrawAll()
        {
            EnsureInitialized();
            s_hand.Clear();
            
            if (s_deck.Count == 0)
            {
                Console.WriteLine("❌ 牌組為空，無法洗牌");
                return;
            }
            
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
            s_stats.RecordShuffle();
            
            Console.WriteLine($"🔄 洗牌完成，抽到{s_hand.Count}張卡牌");
        }
        
        // ✅ 增強：使用卡牌（添加詳細日誌和錯誤處理）
        public static bool UseCard(int handIndex, byte targetId = 0)
        {
            EnsureInitialized();
            
            // ✅ 增強：詳細的錯誤檢查
            if (s_hand.Count == 0)
            {
                Console.WriteLine("❌ 手牌為空，無法使用卡牌");
                return false;
            }
            
            if (handIndex < 0 || handIndex >= s_hand.Count)
            {
                Console.WriteLine($"❌ 無效的卡牌索引：{handIndex}，有效範圍：0-{s_hand.Count - 1}");
                return false;
            }
            
            var card = s_hand[handIndex];
            
            // ✅ 增強：目標驗證
            if (card.RequiresTarget && targetId == 0)
            {
                Console.WriteLine($"❌ 卡牌 [{card.Symbol}] {card.Name} 需要選擇目標");
                return false;
            }
            
            // 獲取玩家ID
            var playerId = GetPlayerId();
            if (playerId == 255)
            {
                Console.WriteLine("❌ 找不到玩家角色");
                return false;
            }
            
            // ✅ 增強：執行前日誌
            Console.WriteLine($"🎴 使用卡牌 {handIndex}: [{card.Symbol}] {card.Name}");
            if (card.RequiresTarget)
            {
                Console.WriteLine($"   目標：Actor {targetId}");
            }
            Console.WriteLine($"   效果：{card.Description}");
            
            // 執行卡牌效果（走HLA路徑）
            bool success = HLASystem.ProcessHLA(playerId, targetId, card.ToHLA());
            
            if (success)
            {
                // 從手牌移除
                s_hand.RemoveAt(handIndex);
                s_stats.RecordCardUse(card.Action);
                
                Console.WriteLine($"✅ 卡牌使用成功，剩餘手牌：{s_hand.Count}張");
            }
            else
            {
                Console.WriteLine($"❌ 卡牌使用失敗");
            }
            
            return success;
        }
        
        // 獲取當前手牌
        public static ReadOnlySpan<SimpleCard> GetHand()
        {
            EnsureInitialized();
            return CollectionsMarshal.AsSpan(s_hand);
        }
        
        // ✅ 增強：獲取可用卡牌資訊
        public static void GetAvailableCards(Span<(int index, SimpleCard card, bool canUse)> buffer, out int count)
        {
            EnsureInitialized();
            count = Math.Min(s_hand.Count, buffer.Length);
            
            for (int i = 0; i < count; i++)
            {
                var card = s_hand[i];
                bool canUse = true; // 基本卡牌通常都能使用
                
                // 可以添加更複雜的可用性檢查邏輯
                if (card.RequiresTarget)
                {
                    // 檢查是否有有效目標
                    Span<byte> enemyBuffer = stackalloc byte[16];
                    int enemyCount = GetEnemyCount(enemyBuffer);
                    canUse = enemyCount > 0;
                }
                
                buffer[i] = (i, card, canUse);
            }
        }
        
        // ✅ 增強：智能回合結束處理
        public static void OnTurnEnd()
        {
            EnsureInitialized();
            
            Console.WriteLine($"🔚 回合結束，當前手牌：{s_hand.Count}張");
            
            // 如果手牌為空或少於一定數量，自動重洗
            bool shouldReshuffle = s_hand.Count == 0;
            
            if (shouldReshuffle)
            {
                Console.WriteLine("📝 手牌不足，觸發自動重洗");
                ShuffleAndDrawAll();
            }
            else
            {
                Console.WriteLine("📝 手牌充足，保持當前狀態");
            }
        }
        
        // ✅ 增強：強制重洗（用於特殊情況）
        public static void ForceReshuffle()
        {
            EnsureInitialized();
            Console.WriteLine("🔄 強制重洗牌組");
            ShuffleAndDrawAll();
        }
        
        // 獲取牌組資訊
        public static DeckConfig GetCurrentConfig() => s_currentConfig;
        public static int GetHandSize()
        {
            EnsureInitialized();
            return s_hand.Count;
        }
        public static int GetDeckSize()
        {
            EnsureInitialized();
            return s_deck.Count;
        }
        public static CardUsageStats GetStats() => s_stats;
        
        // ✅ 增強：獲取手牌構成統計
        public static (int attackCount, int blockCount, int chargeCount) GetHandComposition()
        {
            EnsureInitialized();
            int attackCount = 0, blockCount = 0, chargeCount = 0;
            
            foreach (var card in s_hand)
            {
                switch (card.Action)
                {
                    case BasicAction.ATTACK: attackCount++; break;
                    case BasicAction.BLOCK: blockCount++; break;
                    case BasicAction.CHARGE: chargeCount++; break;
                }
            }
            
            return (attackCount, blockCount, chargeCount);
        }
        
        // 獲取玩家ID
        private static byte GetPlayerId()
        {
            Span<byte> playerBuffer = stackalloc byte[16];
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, playerBuffer);
            return playerCount > 0 ? playerBuffer[0] : (byte)255;
        }
        
        // ✅ 增強：獲取敵人數量（用於檢查攻擊卡可用性）
        private static int GetEnemyCount(Span<byte> buffer)
        {
            int count = 0;
            count += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, buffer[count..]);
            count += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, buffer[count..]);
            count += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, buffer[count..]);
            return count;
        }
        
        // ✅ 增強：除錯顯示（更詳細的信息）
        public static void DebugPrintDeckConfig()
        {
            EnsureInitialized();
            Console.WriteLine("=== 牌組配置 ===");
            Console.WriteLine($"配置：{s_currentConfig.GetDescription()}");
            Console.WriteLine($"狀態：{(s_isInitialized ? "已初始化" : "未初始化")}");
        }
        
        public static void DebugPrintHand()
        {
            EnsureInitialized();
            Console.WriteLine("=== 當前手牌 ===");
            
            if (s_hand.Count == 0)
            {
                Console.WriteLine("手牌為空");
                return;
            }
            
            // 顯示手牌列表
            for (int i = 0; i < s_hand.Count; i++)
            {
                var card = s_hand[i];
                string targetInfo = card.RequiresTarget ? " (需要目標)" : "";
                Console.WriteLine($"  {i}: [{card.Symbol}] {card.Name}{targetInfo} - {card.Description}");
            }
            
            // 顯示手牌統計
            var (attackCount, blockCount, chargeCount) = GetHandComposition();
            Console.WriteLine($"構成：{attackCount}A + {blockCount}B + {chargeCount}C = {s_hand.Count}張");
        }
        
        public static void DebugPrintDeck()
        {
            EnsureInitialized();
            Console.WriteLine("=== 牌組構成 ===");
            
            var (attackCount, blockCount, chargeCount) = (0, 0, 0);
            foreach (var card in s_deck)
            {
                switch (card.Action)
                {
                    case BasicAction.ATTACK: attackCount++; break;
                    case BasicAction.BLOCK: blockCount++; break;
                    case BasicAction.CHARGE: chargeCount++; break;
                }
            }
            
            Console.WriteLine($"牌組：{attackCount}A + {blockCount}B + {chargeCount}C = {s_deck.Count}張");
        }
        
        // ✅ 增強：完整狀態報告
        public static void DebugPrintFullStatus()
        {
            EnsureInitialized();
            Console.WriteLine("=== 卡牌系統完整狀態 ===");
            DebugPrintDeckConfig();
            DebugPrintHand();
            Console.WriteLine($"統計：{s_stats.GetSummary()}");
            Console.WriteLine($"初始化狀態：{(s_isInitialized ? "✅" : "❌")}");
        }
        
        // ✅ 增強：重置系統
        public static void Reset()
        {
            s_hand.Clear();
            s_deck.Clear();
            s_stats.Reset();
            s_isInitialized = false;
            Console.WriteLine("🔄 卡牌系統已重置");
        }
    }
    
    // 簡化的卡牌使用輔助（增強版）
    public static class SimpleCardHelper
    {
        // ✅ 靜態優先級陣列，避免重複分配
        private static readonly BasicAction[] s_normalPriority = { 
            BasicAction.ATTACK, BasicAction.CHARGE, BasicAction.BLOCK 
        };

        // 使用指定索引的卡牌
        public static bool PlayCard(int handIndex, byte targetId = 0)
        {
            var hand = SimpleDeckManager.GetHand();
            if (handIndex < 0 || handIndex >= hand.Length)
            {
                Console.WriteLine($"❌ 無效的卡牌索引: {handIndex}");
                return false;
            }
            
            var card = hand[handIndex];
            
            // 檢查是否需要目標
            if (card.RequiresTarget && targetId == 0)
            {
                Console.WriteLine($"❌ 卡牌 [{card.Symbol}] {card.Name} 需要選擇目標");
                return false;
            }
            
            bool success = SimpleDeckManager.UseCard(handIndex, targetId);
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
            
            Console.WriteLine($"❌ 手牌中沒有 {action} 類型的卡牌");
            return false;
        }
        
        // ✅ 增強：檢查是否有指定類型的卡牌
        public static bool HasCardOfType(BasicAction action)
        {
            var hand = SimpleDeckManager.GetHand();
            
            foreach (var card in hand)
            {
                if (card.Action == action)
                    return true;
            }
            
            return false;
        }
        
        // ✅ 增強：獲取指定類型卡牌的索引列表
        public static void GetCardIndicesOfType(BasicAction action, Span<int> buffer, out int count)
        {
            var hand = SimpleDeckManager.GetHand();
            count = 0;
            
            for (int i = 0; i < hand.Length && count < buffer.Length; i++)
            {
                if (hand[i].Action == action)
                {
                    buffer[count++] = i;
                }
            }
        }
        
        // 自動選擇敵人目標
        public static byte GetDefaultEnemyTarget()
        {
            Span<byte> enemyBuffer = stackalloc byte[16];
            int enemyCount = 0;
            
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BASIC, enemyBuffer);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_ELITE, enemyBuffer[enemyCount..]);
            enemyCount += ActorManager.GetActorsByType(ActorType.ENEMY_BOSS, enemyBuffer[enemyCount..]);
            
            return enemyCount > 0 ? enemyBuffer[0] : CombatConstants.INVALID_ACTOR_ID;
        }
        
        // ✅ 增強：智能卡牌選擇（考慮戰鬥狀況）
        public static int SelectSmartCard()
        {
            var hand = SimpleDeckManager.GetHand();
            if (hand.Length == 0) return -1;
            
            // 獲取玩家狀態
            Span<byte> playerBuffer = stackalloc byte[16];
            int playerCount = ActorManager.GetActorsByType(ActorType.PLAYER, playerBuffer);
            if (playerCount == 0) return 0; // 沒有玩家，隨便選
            
            ref var player = ref ActorManager.GetActor(playerBuffer[0]);
            
            // 檢查是否有敵人
            byte enemyTarget = GetDefaultEnemyTarget();
            bool hasEnemies = enemyTarget != CombatConstants.INVALID_ACTOR_ID;
            
            // 智能決策邏輯
            
            // 1. 如果有蓄力且有敵人，優先攻擊
            if (player.Charge > 0 && hasEnemies)
            {
                for (int i = 0; i < hand.Length; i++)
                {
                    if (hand[i].Action == BasicAction.ATTACK)
                        return i;
                }
            }
            
            // 2. 如果血量較低，優先格擋
            if (player.HP < player.MaxHP / 3)
            {
                for (int i = 0; i < hand.Length; i++)
                {
                    if (hand[i].Action == BasicAction.BLOCK)
                        return i;
                }
            }
            
            // 3. 正常情況：使用靜態優先級陣列
            foreach (var actionPriority in s_normalPriority)
            {
                for (int i = 0; i < hand.Length; i++)
                {
                    if (hand[i].Action == actionPriority)
                    {
                        // 攻擊卡需要確認有目標
                        if (actionPriority == BasicAction.ATTACK && !hasEnemies)
                            continue;
                            
                        return i;
                    }
                }
            }
            
            // 4. 預設選擇第一張
            return 0;
        }
        
        // ✅ 增強：自動玩牌AI（更智能的版本）
        public static bool AutoPlayTurn()
        {
            var hand = SimpleDeckManager.GetHand();
            Console.WriteLine($"🤖 智能AI開始自動玩牌，手牌數: {hand.Length}");
            
            if (hand.Length == 0)
            {
                Console.WriteLine("❌ 手牌為空！");
                return false;
            }
            
            int selectedIndex = SelectSmartCard();
            if (selectedIndex < 0)
            {
                Console.WriteLine("❌ 無法選擇卡牌");
                return false;
            }
            
            var selectedCard = hand[selectedIndex];
            byte target = selectedCard.RequiresTarget ? GetDefaultEnemyTarget() : (byte)0;
            
            Console.WriteLine($"🤖 AI選擇卡牌 {selectedIndex}: [{selectedCard.Symbol}] {selectedCard.Name}");
            
            bool success = PlayCard(selectedIndex, target);
            
            if (success)
            {
                Console.WriteLine("✅ AI卡牌使用成功");
            }
            else
            {
                Console.WriteLine("❌ AI卡牌使用失敗");
            }
            
            return success;
        }
    }
}