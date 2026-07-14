# ACS 控制卡 partial 分类重构设计

日期：2026-05-20

## 背景

`ReeYin_V.Hardware.ControlCard.ACS` 当前将连接、轴控制、运动、插补、IO、辅助方法集中在 `App/AcsControlCard.cs`。`ReeYin_V.Hardware.ControlCard.Googol` 则使用 `partial class` 按功能拆分，文件职责更清晰。此次只按现有 `IControlCard` / `ControlCardBase` 抽象整理 ACS 实现，不新增 ACSPL 手册中的扩展功能。

## 目标

- 参考 Googol 项目的组织方式，将 `AcsControlCard` 拆分为多个 `partial` 文件。
- 保持现有公共接口、继承关系和运行行为不变。
- 让连接、轴设置、运动、停止、回零、IO、插补、辅助方法分别落在明确文件中。
- 完成后确保 ACS 项目可构建。

## 非目标

- 不实现新的 ACS 专用功能，例如 PEG、MARK、DC 数据采集、CONNECT、EtherCAT 映射或 CoE 读写。
- 不修改 `IControlCard`、`ControlCardBase` 的方法签名。
- 不引入新依赖或更换 ACS SDK。
- 不调整 UI、配置模型或模块加载逻辑。
- 不做机械回零流程增强；保留当前 ACS 实现中“反馈位置清零不等同机械回零”的行为。

## 文件拆分设计

### `App/AcsControlCard.cs`

保留类主体、字段、构造函数和基础配置属性：

- `_syncRoot`
- `_api`
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
- 常量：默认 IO 数量、默认超时

该文件不再承载具体运动和 IO 逻辑。

### `App/InitCard.cs`

负责连接生命周期：

- `DoInit`
- `DoConfigure`
- `DoClose`
- `OpenCommunication`
- `GetSerialPort`
- `GetSerialBaudRate`

连接模式仍支持 Ethernet、Serial、PCI、Simulator。异常处理保持返回 `false` 或设置状态的原逻辑。

### `App/AxisSetting.cs`

负责轴状态、轴号转换和速度配置：

- `DoGetAxisEnable`
- `DoSetAxisEnable`
- `SetAxisEnabled`
- `DoGetAxisStopped`
- `PrepareMotorToMove`
- `GetFlag`
- `GetSpeedSetting`
- `GetAxisVelocity`
- `GetInterpolationVelocity`
- `ConfigureInterpolationAxes`
- `TryGetAxisConfig`
- `ToAcsAxis`
- `ToAcsRotation`
- `UpdateAxisState`
- `UpdateAxisStates`
- `InitializeAxisBuffers`
- `EnsurePositionBuffers`

### `App/JogMove.cs`

负责单轴相对/绝对/连续运动：

- `Move`
- `DoMoveAxis`
- `DoMoveContinue`
- `JogAxis` 两个重载
- `MoveAbsoluteAxis`

保留方向转换逻辑：正向使用正距离/正速度，反向使用负距离/负速度。

### `App/Stop.cs`

负责停止和等待：

- `DoStop`
- `StopConfiguredAxes`
- `StopAxis`
- `WaitUntilStopped` 单轴版本
- `WaitUntilStopped` 多轴版本

减速停止继续使用 `Halt`，立即停止继续使用 `Kill`。

### `App/GoHome.cs`

负责回零相关现有能力：

- `DoGoHome`
- `ResetFeedbackPosition`

当前 ACS 实现只将反馈位置设为 0，不表示真实机械回零。保留该提示，避免行为语义变化。

### `App/LineInterpolation.cs`

负责直线插补：

- `LineInterpoMoving`
- `TryBuildInterpolationMove`
- `TryBuildAxes`
- `PrepareInterpolationAxes`

目标位置校验继续调用 `ValidateLimitPosition`。

### `App/CircularInterpolation.cs`

负责圆弧插补：

- `ArcInterpoMoving`
- `TryResolveArcCenter`
- `TryResolveCenterFromRadius`
- `GetDistance`

支持现有 Center、Radius、Angle 三种圆弧描述方式。

### `App/CustomInterpolation.cs`

负责自定义插补：

- `CustomInterpolationMoving`

保留现有自定义指令回调约定：`ConstomOrder()` 返回 `"OK"` 表示成功。

### `App/IOControl.cs`

负责 IO：

- `GetAllInput`
- `GetAllOutput`
- `SetSpecifiedIO`
- `GetSpecifiedIO`
- `GetIoPort`
- `GetIoBit`

默认按每 port 32 bit 计算端口号和 bit 号。

### `App/Assist.cs`

负责通用辅助：

- `TryExecute`
- `IsFinite`

仅放无法归入某一业务分类的纯辅助方法。

## 行为保持要求

- 原有 public/override 方法签名不变。
- 原有连接参数默认值不变。
- 原有异常处理策略不变：捕获异常、输出 Console、返回 false 或保持状态。
- 原有 ACS API 调用不做语义替换。
- 原有限位校验调用点不减少。
- 原有 `waitforend` 行为保持。

## 验证方式

执行：

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

通过标准：

- ACS 项目编译成功。
- 不新增 ACS 项目编译错误。
- 允许存在项目现有的外部引用警告，例如 `halcondotnet` 引用警告。

## 风险与应对

- 风险：拆分时遗漏 using 或方法所在 partial 文件。应对：拆分后立即构建验证。
- 风险：中文枚举名在终端显示乱码导致误改。应对：不依赖显示乱码，使用原源文件真实文本移动代码。
- 风险：方法移动后访问级别错误。应对：保持原方法访问修饰符。
- 风险：构建命令不带 `SolutionDir` 会失败。应对：验证命令显式传入 `SolutionDir`。
