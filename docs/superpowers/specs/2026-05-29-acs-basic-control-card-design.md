# ACS 基础控制卡功能补全设计

日期：2026-05-29

## 背景

`ReeYin_V.Hardware.ControlCard.ACS` 已经接入 `ACS.SPiiPlusNET8.dll`，并按 `partial` 文件拆分了连接、轴控制、运动、停止、回零、IO、插补等基础代码。当前项目可构建通过，但 ACS 基础功能还缺少统一的 ACS 专用配置、真实回零入口、Program Buffer 诊断封装，以及配置界面的 ACS 专用编辑区。

本设计参考本地整理的 `SPiiPlus.NET-Library-Programmers-Guide_中文快速查阅手册.md`，第一阶段先补全基础控制卡能力；高级运动和其他扩展功能后续再实现。

## 目标

- 先补全 ACS 基础功能，再扩展高级运动和其他功能。
- 新增 ACS 专用配置模型，不污染通用 `SingleAxisParam`。
- 真实回零采用控制器内已有 ACSPL Buffer/Program：每个轴独立配置 Buffer，C# 负责按轴顺序运行并等待结束。
- 控制卡配置页面新增 ACS 专用配置区，可编辑连接参数、IO 数量、每轴 Home Buffer 映射、启用和超时。
- 封装 Program Buffer 运行、等待、停止、状态和错误读取，为后续 PVT、PEG、采集、Buffer 管理复用。
- 保持 `IControlCard` 和 `ControlCardBase` 的公共基础签名稳定，避免影响 Googol/ZMotion。

## 非目标

- 不在第一阶段实现 PVT/PV、分段运动、Spline/NURBS、Blend 等高级运动功能。
- 不在第一阶段实现 PEG、Data Collection、EtherCAT/CoE、Flash 保存、Buffer 下载/编译/上传。
- 不在第一阶段改造通用 `IControlCard` 为平台化高级扩展接口。
- 不在 C# 侧完整实现限位、原点、Index、方向、速度等机械回零算法；真实回零逻辑由控制器内 ACSPL Buffer 承担。
- 不把反馈位置清零等同于机械回零；`ResetFeedbackPosition()` 仅保留为显式辅助能力。

## 总体架构

### ACS 项目结构

继续保持现有 `AcsControlCard` 的 `partial` 分类：

- `AcsControlCard.cs`：类主体、构造函数和兼容属性。
- `InitCard.cs`：连接生命周期和初始化配置。
- `AxisSetting.cs`：轴使能、状态映射、速度配置、轴号转换。
- `JogMove.cs`：点位、Jog、绝对运动。
- `Stop.cs`：停止和等待停止。
- `GoHome.cs`：回零入口与反馈位置清零辅助。
- `IOControl.cs`：输入输出读取和写入。
- `LineInterpolation.cs` / `CircularInterpolation.cs` / `CustomInterpolation.cs`：现有插补能力。
- 新增 `AcsControlCardOptions.cs`：ACS 专用配置模型。
- 新增 `BufferProgram.cs`：Program Buffer 运行、等待、停止、诊断封装。

### 依赖边界

- 基础通用能力仍通过 `ControlCardBase` / `IControlCard` 暴露。
- ACS 特有配置通过 `AcsControlCard.Options` 暴露并序列化。
- ACS 特有运行逻辑只放在 ACS 项目内，不反向修改 Googol/ZMotion。
- 配置 UI 可以识别当前卡是否为 ACS，并显示 ACS 配置区；非 ACS 卡不显示该区域。

## ACS 专用配置模型

新增 `AcsControlCardOptions`，挂在 `AcsControlCard` 上：

- `ConnectionMode`
- `RemoteAddress`
- `UseTcp`
- `EthernetPort`
- `SerialPort`
- `SerialBaudRate`
- `PciSlotNumber`
- `InternalTimeout`
- `DigitalInputCount`
- `DigitalOutputCount`
- `HomeBeforeRunStopMode`
- `ObservableCollection<AcsAxisHomeBufferConfig> HomeBuffers`

新增 `AcsAxisHomeBufferConfig`：

- `En_AxisNum Axis`
- `int BufferNo`
- `bool IsEnabled`
- `int Timeout`
- `bool StopAxisBeforeRun`
- `bool ResetFeedbackAfterSuccess`
- `double ResetPosition`

### 默认值与兼容

- `AcsControlCard` 构造时创建默认 `Options`。
- 已有配置反序列化后如果 `Options` 为空，通过 `EnsureOptions()` 补默认值。
- 新建 ACS 卡时自动补齐 X/Y/Z/R 等常用轴的 Home Buffer 映射，例如 X/Y/Z/R 对应 Buffer 1/2/3/4。
- 默认映射不代表真实回零已可用；用户仍需确认控制器内对应 Buffer 已下载、编译，并在 UI 中启用。
- 旧的 `ConnectionMode`、`RemoteAddress`、`UseTcp`、`EthernetPort`、`SerialPort`、`SerialBaudRate`、`PciSlotNumber`、`InternalTimeout`、`DigitalInputCount`、`DigitalOutputCount` 属性可以保留为兼容转发属性，内部读写 `Options`。

## 控制卡配置 UI

在 `ControlCardConfigView.xaml` 的控制卡配置区域新增 ACS 专用配置区：

- 当前选中卡是 `ReeYin_V.Hardware.ControlCard.ACS.App.AcsControlCard` 时显示。
- 非 ACS 卡隐藏，避免影响其他厂家卡。
- 使用现有 `DefaultDataGridStyle` 和共享控件风格。

### UI 内容

连接和基础参数：

- 连接方式：Simulator、Ethernet、Serial、PCI。
- IP 地址、TCP/UDP、以太网端口。
- 串口号、波特率。
- PCI Slot。
- 内部超时。
- 数字输入数量、数字输出数量。
- 回零前默认停止方式。

Home Buffer 映射表：

- 业务轴。
- Buffer 编号。
- 是否启用。
- 超时。
- 运行前是否停止该轴。
- 成功后是否反馈位置清零。
- 清零位置。

保存仍走现有 `ConfigManager.Write(ConfigKey.ControlCard, ModelParam)`，不新增独立 ACS 配置文件。

## 基础功能范围

### 连接配置

`DoInit()` 和 `OpenCommunication()` 优先读取 `Options`：

- Simulator：`OpenCommSimulator()`
- Ethernet TCP：`OpenCommEthernetTCP(address, port)`
- Ethernet UDP：`OpenCommEthernetUDP(address, port)`
- Serial：`OpenCommSerial(port, baudRate)`
- PCI：`OpenCommPCI(slotNumber)`

连接成功后：

- 设置 `IsConnected = true`。
- 初始化 `CurPos` / `CurPulse`。
- 调用 `DoConfigure()` 配置启用轴速度、加减速度。

连接失败时：

- 设置 `IsConnected = false`。
- 设置 `State = HardwareState.Error`。
- 返回 `false` 并输出异常信息。

### 轴基础控制

保留现有功能并补强保护：

- `DoGetAxisEnable`
- `DoSetAxisEnable`
- `DoGetAxisStopped`
- `Move`
- `DoMoveAxis`
- `DoMoveContinue`
- `JogAxis`
- `MoveAbsoluteAxis`
- `DoStop`

基础补强点：

- 运动前校验连接状态、轴配置、使能状态、停止状态。
- 按 `SpeedSetting` 配置 `SetVelocity`、`SetAcceleration`、`SetDeceleration`。
- 运动和停止后刷新轴状态。
- 异常捕获并返回 `false`。

### 轴状态映射

`AxisSetting.cs` 继续通过 `GetMotorState()` 同步状态，并尽量映射：

- 使能状态。
- 运动状态。
- 到位状态。
- 故障/报警状态。
- 正负限位状态。
- 当前位置。

状态同步目标：

- `SingleAxisParam.IsEnable`
- `SingleAxisParam.IsMoving`
- `SingleAxisParam.IsHomed`
- `SingleAxisParam.IsPositiveLimit`
- `SingleAxisParam.IsNegativeLimit`
- `SingleAxisParam.AxisStatus`
- `SingleAxisParam.CurPos`

### 位置读取

`GetAllPosInfos()` 使用 `GetFPosition()` 更新：

- `Config.AllAxis[i].CurPos`
- `CurPos[]`

需要保护：

- 控制卡未连接返回 `false`。
- `Config.AllAxis` 为空时不抛异常。
- 轴号转换越界时跳过或返回失败。
- `CurPos` 数组长度不足时自动扩容。

### IO

IO 数量从 `Options` 读取：

- `DigitalInputCount`
- `DigitalOutputCount`

保留当前 port/bit 映射：

- `port = index / 32`
- `bit = index % 32`

基础补强点：

- `GetAllInput()` / `GetAllOutput()` 按配置数量生成数组。
- `SetSpecifiedIO()` / `GetSpecifiedIO()` 对负数和越界 index 返回 `false`。
- 异常捕获并返回 `false`。

## 真实回零设计

### 回零策略

采用每轴独立 ACSPL Buffer：

- 每个业务轴配置一个 `AcsAxisHomeBufferConfig`。
- `DoGoHome(out message)` 按 `Config.AllAxis.Where(axis => axis.IsUsing)` 的顺序逐轴执行。
- 只执行 `HomeBuffers` 中 `IsEnabled = true` 且匹配当前业务轴的配置。
- 任一轴失败后停止后续轴回零，并返回详细失败信息。

### 回零流程

单轴回零流程：

1. 查找业务轴对应的 ACS 轴号和 Home Buffer 配置。
2. 校验 Buffer 编号合法。
3. 如果 `StopAxisBeforeRun = true`，先按 `HomeBeforeRunStopMode` 或配置停止该轴。
4. 调用 Program Buffer 封装运行指定 Buffer。
5. 调用 Program Buffer 封装等待 Buffer 结束，超时时返回失败。
6. 读取 Program 状态和 Program 错误。
7. 刷新轴状态和位置。
8. 如果 `ResetFeedbackAfterSuccess = true`，调用 `ResetFeedbackPosition(axis, ResetPosition)`。
9. 标记该轴回零完成。

整体回零状态：

- 开始时：`IsAxisHoming = true`。
- 全部成功：`IsAxisHomed = true`。
- 失败/异常：`IsAxisHomed = false`。
- finally：`IsAxisHoming = false`。

### 反馈位置清零

`ResetFeedbackPosition()` 继续保留，但用途明确：

- 它只调用 `SetFPosition()` 改变反馈位置。
- 它不代表机械回零。
- 真实回零成功后是否调用它由 `ResetFeedbackAfterSuccess` 控制。

## Program Buffer 封装

新增 `BufferProgram.cs`，集中封装 SPiiPlus.NET Program Buffer 相关基础能力。

基础方法：

- `RunProgramBuffer(int bufferNo, out string message)`
- `WaitProgramBufferEnd(int bufferNo, int timeout, out string message)`
- `StopProgramBuffer(int bufferNo, out string message)`
- `TryGetProgramState(int bufferNo, out object? state, out string message)`
- `TryGetProgramError(int bufferNo, out object? error, out string message)`
- `TryToProgramBuffer(int bufferNo, out ProgramBuffer buffer, out string message)`

封装原则：

- 对外只接收 `int bufferNo`，内部转换为 `ProgramBuffer`。
- Buffer 编号不合法时返回 `false` 和明确 message。
- 所有 SDK 异常都捕获并带上操作名称。
- `WaitProgramBufferEnd()` 超时后读取状态和错误，便于定位控制器程序问题。

## 错误处理

所有基础 ACS SDK 调用继续捕获异常并返回 `false`，避免控制卡线程直接崩溃。

回零失败信息至少包含：

- 业务轴。
- ACS 轴号。
- Buffer 编号。
- ProgramState。
- ProgramError。
- 异常消息或超时信息。

需要明确处理的失败类型：

- 控制卡未连接。
- Home Buffer 未配置。
- Home Buffer 未启用。
- Buffer 编号非法。
- RunBuffer 调用失败。
- WaitProgramEnd 超时。
- ProgramError 非空或显示程序错误。
- 轴状态/位置刷新失败。

## 分阶段路线

### 第一阶段：基础功能补全

- ACS 专用配置模型。
- 控制卡配置 UI 的 ACS 配置区。
- Program Buffer 基础封装。
- `DoGoHome()` 使用每轴 Home Buffer 运行真实回零入口。
- 连接、轴状态、位置、IO 的基础保护和诊断补强。
- ACS 项目构建验证。

### 第二阶段：高级运动

- PVT/PV。
- 多点运动。
- 分段运动。
- Spline/NURBS。
- Blend。
- 轨迹队列。

### 第三阶段：其他扩展

- PEG 位置比较。
- Data Collection。
- EtherCAT/CoE。
- Buffer 下载、编译、上传、管理。
- Flash 保存。
- 控制器服务信息和更完整诊断。

## 验证方式

必须执行 ACS 项目构建：

```powershell
dotnet build 'E:\Company\工作目录\ReeYin-V\ReeYin-V\Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

通过标准：

- 构建结果 `0 errors`。
- 允许现有 `halcondotnet` 引用警告和已有 nullable 警告。
- 不新增 ACS 项目编译错误。

可选验证：

- Simulator 模式验证连接和基础 API 路径。
- 真实控制器上由用户确认各轴 Home Buffer 已下载、编译且可独立运行。
- 真实设备验证 `GoHome()` 时按轴顺序运行对应 Buffer，并能在失败时返回明确诊断。

## 风险与应对

- 风险：不同 SPiiPlus.NET 版本的 Program Buffer 枚举或方法签名存在差异。应对：实现前用本地 `ACS.SPiiPlusNET8.dll` 编译验证方法签名。
- 风险：真实回零依赖控制器内 Buffer 内容，模拟器无法证明机械动作正确。应对：C# 只验证运行入口和等待/诊断路径，真实设备由用户验证 Buffer 程序。
- 风险：UI 增加 ACS 专用区域影响非 ACS 卡。应对：通过类型判断只在 ACS 卡选中时显示。
- 风险：旧 JSON 没有 `Options` 导致空引用。应对：所有入口调用 `EnsureOptions()`。
- 风险：默认 Buffer 映射被误认为可直接使用。应对：默认映射不强制启用，UI 和消息提示用户确认控制器 Buffer。
- 风险：错误诊断对象类型不稳定。应对：诊断封装以字符串形式输出，避免 UI 或日志依赖具体枚举类型。

## 自审结果

- Placeholder scan：无 TBD/TODO/占位需求。
- Consistency：设计与用户确认一致，先基础、后高级和其他扩展；真实回零使用每轴已有 ACSPL Buffer。
- Scope：第一阶段不修改通用接口，不实现高级运动和 PEG/采集等扩展。
- Ambiguity：明确了配置落点、UI 范围、回零承载方式、失败处理和验证标准。
