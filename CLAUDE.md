# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 全局规则

- **语言**: 使用中文回复，保持沟通清晰友好。
- **编辑策略**: 优先编辑现有文件，避免不必要地重写整个文件。
- **阅读策略**: 除非文件被编辑过，否则不要重复阅读已读过的文件。
- **输出风格**: 输出简洁明了，但推理过程必须详尽。
- **代码规范**:
  - 单个代码文件不超过400行，超长则拆分。
  - 嵌套层级不超过4层。

## GitHub 账户

- 默认 GitHub 账户: **Cathy729** (用户唯一账户)
- 所有与 GitHub 相关的操作（push、PR、issue等）默认使用此账户。

## Build & Run

```bash
# Build the project (Debug configuration)
/path/to/MSBuild.exe DualMystery.csproj /p:Configuration=Debug

# Build and run
/path/to/MSBuild.exe DualMystery.csproj /p:Configuration=Debug && bin/Debug/DualMystery.exe

# Clean build artifacts
/path/to/MSBuild.exe DualMystery.csproj /t:Clean
```

- .NET Framework 4.7.2 WinForms application — requires Visual Studio 2017+ or MSBuild with the .NET Framework 4.7.2 SDK.
- 构建时使用 csc_wrapper.cmd 调用 dotnet Roslyn 编译器以支持 C# 7.3 语法。
- Project file: `DualMystery.csproj` (ToolsVersion 15.0).

## Architecture Overview

A two-player cooperative detective game ("双线谜案" / DualMystery) set in 1930s. Two players each run a client form connected via TCP to a game server — Player A investigates the study (书房), Player B investigates the corridor (走廊). They communicate via an in-game wall phone (backed by TCP) and must cooperate to solve a locked-room murder.

### 网络架构

所有游戏通信改为 **TCP 客户端-服务器模式**（localhost:8888）：

```
Program.cs  →  GameServer (后台线程 TCP 监听)
                    ├── GameClient (玩家A ← 书房)
                    └── GameClient (玩家B ← 走廊)
```

- **GameServer** (`GameServer.cs`): TCP 服务器。管理游戏状态（线索、电话、指认），通过 JSON 消息路由客户端请求并广播事件。
- **GameClient** (`GameClient.cs`): TCP 客户端。连接服务器，提供方法调用（DiscoverClue, SendChatMessage 等）和事件回调。
- **消息协议**: 基于 JSON 的行终止协议，每条消息包含 Type（消息类型）和 Data（键值对数据）。
- 消息类型: DiscoverClue, ClueDiscovered, RequestCall, CallEstablished, SendChatMessage, ChatMessage, SubmitAccusation, AccusationResult, UnlockSafe, SafeUnlocked 等。
- 所有游戏状态（GameManager, PhoneManager, ChatService）在服务器端维护，客户端通过 TCP 同步。

### Entry Point & Forms

```
Program.cs  →  GameServer.Start()  →  FormMain (菜单)
                                        →  FormPlayerA (书房, 左侧)
                                        →  FormPlayerB (走廊, 右侧)
                                        →  FormChat (电话聊天 + 数据包可视化)
                                        →  FormEnding (结局动画)
```

- **FormMain** (`FormMain.cs`): 标题画面，两个按钮打开各自玩家界面。
- **FormPlayerA** (`FormPlayerA.cs`): 书房场景（~450行）。包含场景物品（壁炉、吊灯、书架、窗户、地毯、尸体等）、NPC（贝蒂、格雷医生）、线索发现、电话系统、指认按钮。
- **FormPlayerB** (`FormPlayerB.cs`): 走廊场景（~450行）。场景物品（壁灯、挂画、花瓶等）、NPC（埃德加、莫里斯）。
- **FormChat** (`FormChat.cs`): 电话聊天窗口 + 数据包传输可视化（SEQ/ACK/校验和动画，模拟停等协议）。
- **FormEnding** (`FormEnding.cs`): 全屏结局动画，手动点击推进字幕，右上角有关闭按钮，Esc 退出。

### Core Services (Server-side)

- **GameManager** (`GameManager.cs`): 服务器端游戏状态。管理线索列表、保险箱解锁、双人指认系统。
- **PhoneManager** (`PhoneManager.cs`): 呼叫状态机（空闲→响铃→通话中）。
- **ChatService** (`ChatService.cs`): 聊天消息历史记录。
- **GameServer** (`GameServer.cs`): TCP 服务器（端口 8888），接受 2 个客户端，消息路由与广播。
- **GameClient** (`GameClient.cs`): TCP 客户端包装器，提供方法调用和事件订阅。

### Data Models

- **Clue** (`Clue.cs`): Id, Name, Description, IsDiscovered, DiscoveredBy ("A" or "B").
- **ChatMessage** (`ChatMessage.cs`): Sender ("A", "B", or "System"), Text, Time.

### Pixel Art System

- **PixelIcons** (`PixelIcons.cs`): GDI+ 代码生成所有游戏图标（16×16 放大到 32×32）。包括线索图标、场景装饰、NPC 道具等。

### Gameplay Flow

1. 两位玩家独立探索各自场景
2. 书房线索：凶器刀、烧毁的信、圣经暗格纸条(密码提示)、带血手帕、保险箱、书桌
3. 走廊线索：家族照片(提示"19")、当票、小钥匙、旧日历(提示"12/25")
4. 玩家使用壁挂电话交流线索（通过 TCP 传输）
5. 合并线索 → 解开保险箱密码 1225（圣诞节）
6. 保险箱内找到遗嘱 + 举报信 → 动机证据
7. 两人必须一致指认真凶"莫里斯"才能触发结局
8. 指认不一致则重置后可重新指认
9. 双人正确指认 → FormEnding 结局动画

### 数据通信技术特性

- **TCP 网络通信**: 所有游戏通信基于 TCP Socket（localhost），演示了客户端-服务器架构。
- **数据包可视化**: 电话聊天中的消息被拆分为数据包显示（SEQ、数据块、校验和），模拟停等协议的发送-ACK-重传过程。
