# Recall 戰鬥系統 - 專案概述

## 核心概念
- **回合制戰鬥** + 記憶時間線系統
- **基本動作**：Attack, Block, Charge
- **Echo系統**：重現先前動作組合 (未來功能)
- **HLA系統**：高階戰術抽象，翻譯為原子命令序列

## 技術要求
- **最低限度C風格**設計 (性價比最佳)
- **命令驅動架構** - 所有狀態修改透過AtomicCmd
- **零GC核心路徑** - 戰鬥執行過程無記憶體分配
- **相容Unity/Godot** - 核心無引擎依賴

## 核心架構決策

### 三層架構
```
HLA Layer      (戰術意圖) - 玩家/AI選擇戰術
Translation    (解析層)   - HLA → AtomicCmd[]序列  
Execution      (執行層)   - AtomicCmd修改Actor狀態
```

### 資料流向
```
HLA選擇 → Translation解析 → AtomicCmd生成 → Command執行 → Actor狀態更新
```

### 檔案結構 (Phase 1)
1. **Actor.cs** - 角色數據管理 (純數據容器)
2. **Command.cs** - 原子命令系統 (所有業務邏輯)
3. **HLA.cs** - 高階動作定義與翻譯
4. **Phase.cs** - 流程控制 (回合制狀態機)

## 設計約束
- **30個以內操作碼** - 避免過度複雜化
- **固定記憶體池** - 64個Actor上限
- **單執行緒設計** - 避免並發複雜度
- **函數式核心** - 核心邏輯無副作用，狀態修改集中

## 與遊戲設計文檔的對應
- **基本操作** → CmdOp.ATTACK/BLOCK/CHARGE
- **記憶時間軸** → 未來Echo系統基礎
- **回響卡牌** → HLA系統實現
- **Phase機制** → Phase.cs實現四階段循環