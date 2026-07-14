# ACS Buffer 脚本编译语言说明

本文档说明 ReeYin_V ACS 控制卡项目中 Buffer 脚本的语言、用途、C# 调用方式和现场使用注意事项。

## 1. 语言名称

ACS 控制器 Program Buffer 中编写、下载、编译、运行的脚本语言是 `ACSPL+`。

`ACSPL+` 是 ACS 控制器的专用运动控制脚本语言，不是 C#，也不是 G-code。C# 上位机只负责编辑脚本文本，并通过 SPiiPlus.NET API 将脚本写入控制器 Buffer、编译、运行、停止和读取诊断信息。

项目内命令速查文档：

- `ACSPL_Commands_CN.md`

## 2. Buffer 的含义

ACS 控制器中的 Program Buffer 可以理解为控制器端的程序槽。

- 每个 Buffer 有编号，当前项目按 `0..64` 做合法范围校验。
- ACSPL+ 脚本文本可以下载到指定 Buffer。
- 下载后的脚本需要编译。
- 编译通过后可以运行。
- Buffer 运行中可以被停止，也可以查询程序状态和错误码。

当前项目的 C# 封装位于：

- `App/BufferProgram.cs`

## 3. C# 与 ACSPL+ 的关系

| 层级 | 作用 | 示例 |
| --- | --- | --- |
| C# / WPF 上位机 | 编辑文本、下载 Buffer、编译、运行、停止、读取状态 | `LoadBuffer`、`CompileBuffer`、`RunBuffer` |
| ACSPL+ | 真正在 ACS 控制器内执行的运动控制脚本 | `ENABLE`、`HOME`、`WAIT`、`IF...END` |
| ACS 控制器 | 执行 Buffer 程序并控制轴、IO、PEG、数据采集 | Program Buffer 1、2、3... |

当前项目已封装的 C# 操作：

| 操作 | 项目方法 | SPiiPlus.NET API |
| --- | --- | --- |
| 读取 Buffer 脚本 | `TryUploadProgramBuffer` | `UploadBuffer` |
| 下载/覆盖 Buffer 脚本 | `TryLoadProgramBuffer` | `LoadBuffer` |
| 追加 Buffer 脚本 | `TryAppendProgramBuffer` | `AppendBuffer` |
| 编译 Buffer | `TryCompileProgramBuffer` | `CompileBuffer` |
| 运行 Buffer | `TryRunProgramBuffer` | `RunBuffer` |
| 停止 Buffer | `TryStopProgramBuffer` | `StopBuffer` |
| 读取状态/错误 | `GetProgramBufferDiagnostics` | `GetProgramState`、`GetProgramError` |

## 4. ACSPL+ 的主要用途

ACSPL+ 是运动控制专用脚本语言，常用于：

- 轴使能、失能和故障清除。
- 回零流程。
- 点到点运动。
- 直线、圆弧、路径插补。
- IO 控制。
- PEG / MARK 位置同步输出。
- 数据采集。
- 条件判断、循环、等待、子程序调用。
- 多步骤流程封装，例如停止轴、清状态、回零、等待完成、复位位置。

常见命令类别：

| 类别 | 常见命令 |
| --- | --- |
| 轴管理 | `ENABLE`、`DISABLE`、`FCLEAR`、`HOME` |
| 运动 | `PTP`、`JOG`、`MSEG...ENDS`、`BSEG...ENDS`、`XSEG...ENDS` |
| PEG / MARK | `ASSIGNPEG`、`ASSIGNPOUTS`、`PEG_I`、`PEG_R`、`STARTPEG`、`STOPPEG` |
| 数据采集 | `DC`、`SPDC`、`STOPDC` |
| 程序结构 | `CALL`、`IF...END`、`WHILE...END`、`WAIT`、`BLOCK...END` |

## 5. ACSPL+ 基本结构

ACSPL+ 脚本通常按行执行。以下内容是结构示意，实际语法需要以 ACS 官方编译器结果为准。

简单回零脚本示意：

```text
ENABLE 0
WAIT 100
HOME 0
WAIT 100
```

条件结构示意：

```text
IF 条件
    ; 条件成立时执行
ELSE
    ; 条件不成立时执行
END
```

循环结构示意：

```text
WHILE 条件
    ; 循环执行
END
```

调用子程序示意：

```text
CALL 子程序名
```

## 6. 编译的含义

`CompileBuffer` 会让 ACS 控制器检查指定 Buffer 中的 ACSPL+ 脚本。

编译通常检查：

- 命令是否存在。
- 语法是否正确。
- 块结构是否完整，例如 `IF...END`、`WHILE...END`。
- 标签、变量、数组引用是否合法。
- 当前控制器型号和固件是否支持相关命令。

编译不会保证：

- 机械动作一定安全。
- 轴不会撞机。
- IO 映射一定正确。
- PEG 输出一定接到正确硬件。
- 回零方向、速度和限位逻辑一定符合设备。

因此，编译通过只代表脚本语法可被控制器接受，不代表工艺流程已经安全。

## 7. Terminal 指令与 Buffer 程序的区别

ACS 文档中还有一类 `Communication Terminal` 指令，用于 ACS Studio 的通信终端，例如查询变量、查看 Buffer 状态等。

这类 Terminal 指令不属于 ACSPL+ 程序语言，不能直接写入 Program Buffer 当作程序语句运行。

结论：

- ACSPL+ 程序指令可以写入 Buffer、编译、运行。
- Terminal 指令用于终端交互，不应直接写入 Buffer 程序。

## 8. 当前项目中的使用流程

ACS 配置弹窗中已经提供 `Buffer脚本` 页，推荐操作流程如下：

1. 选择 `Buffer编号`。
2. 点击 `读取Buffer`，查看控制器当前脚本。
3. 编辑 ACSPL+ 脚本。
4. 点击 `下载到Buffer`。
5. 点击 `编译`。
6. 编译成功后点击 `运行`。
7. 需要停止时点击 `停止`。
8. 异常时点击 `状态` 查看 `ProgramState` 和 `ProgramError`。

相关页面与逻辑：

- `Views/AcsControlCardConfigView.xaml`
- `ViewModels/AcsControlCardConfigViewModel.cs`
- `App/BufferProgram.cs`

## 9. 回零 Buffer 的推荐用途

当前项目已有“每轴回零 Buffer 映射”配置。典型设计是每个轴绑定一个独立 Buffer：

| 轴 | Buffer | 用途 |
| --- | --- | --- |
| X | Buffer 1 | X 轴回零脚本 |
| Y | Buffer 2 | Y 轴回零脚本 |
| Z | Buffer 3 | Z 轴回零脚本 |
| R | Buffer 4 | R 轴回零脚本 |

项目执行回零时不会自动生成 ACSPL+ 脚本，而是运行已经配置、启用并下载到控制器的 Buffer。

单轴回零脚本示意：

```text
; X 轴回零脚本示意
ENABLE 0
WAIT 100
HOME 0
WAIT 100
```

实际项目中，应根据轴号、回零方式、限位输入、方向、速度和设备安全逻辑编写脚本。

## 10. 使用注意事项

- 下载脚本会覆盖控制器中对应 Buffer 的原脚本。
- 编译前确认脚本属于 ACSPL+ 程序指令，不要混入 Terminal 指令。
- 运行脚本前确认控制卡已连接。
- 运行运动类脚本前确认急停、限位、安全门、驱动状态。
- 回零脚本建议先在 ACS Studio 中单独验证，再放到项目中运行。
- PEG、MARK、SPDC、EtherCAT 等功能和控制器型号、固件、硬件映射强相关，使用前必须核对原厂手册和设备配置。
- 生产使用前建议固定 Buffer 编号规范，避免多人维护时覆盖错误 Buffer。

## 11. 推荐 Buffer 编号规范

| Buffer 范围 | 推荐用途 |
| --- | --- |
| 1-8 | 单轴回零 |
| 9-16 | 设备初始化 / 清状态 |
| 17-32 | 工艺流程脚本 |
| 33-48 | PEG / MARK 辅助脚本 |
| 49-64 | 调试、临时验证、备用 |

## 12. 最小 C# 调用示例

以下代码展示 C# 上位机下载、编译、运行 ACSPL+ 脚本的基本流程：

```csharp
var buffer = (ProgramBuffer)bufferNo;

_api.LoadBuffer(buffer, acsplText);
_api.CompileBuffer(buffer);
_api.RunBuffer(buffer, null);
_api.WaitProgramEnd(buffer, timeout);
```

项目中请优先使用 `AcsControlCard` 已封装的方法，便于统一连接检查、异常处理和诊断信息输出。
