# ACS PEG 与 Data Collection 工艺功能设计
日期：2026-05-29

## 背景

第一阶段已经补齐 `ReeYin_V.Hardware.ControlCard.ACS` 的基础控制卡能力：ACS 专用配置、连接参数、IO 数量、轴状态映射、Program Buffer 诊断，以及基于控制器内 ACSPL Buffer 的真实回零入口。

第二阶段第一批工艺功能聚焦 PEG 位置事件输出与 Data Collection 数据采集。用户已确认一维和二维都要支持，并明确需要实现等间隔脉冲输出。

本设计参考本地 `SPiiPlus.NET-Library-Programmers-Guide_中文快速查阅手册.md` 与本地 `ACS.SPiiPlusNET8.dll` 反射签名。

## 已确认 SDK 能力

PEG 相关签名：

```csharp
void AssignPegNT(Axis axis, int engToEncBitCode, int gpOutsBitCode)
void AssignPegOutputsNT(Axis axis, int outputIndex, int bitCode)
void AssignFastInputsNT(Axis axis, int inputIndex, int bitCode)
void PegIncNTV2(MotionFlags flags, Axis axis, double width, double firstPoint, double interval, double lastPoint,
                int errMapAxis1, double axisCoord1, int errMapAxis2, double axisCoord2,
                int errMapMaxSize, double minDirDistance, int tbNumber, double tbPeriod)
void PegRandomNTV2(MotionFlags flags, Axis axis, double width, int mode, int firstIndex, int lastIndex,
                   string pointArray, string stateArray,
                   int errMapAxis1, double axisCoord1, int errMapAxis2, double axisCoord2,
                   double minDirDistance, int tbNumber, double tbPeriod)
void WaitPegReadyNT(Axis axis, int timeout)
void StartPegNT(Axis axis)
void StopPegNT(Axis axis)
```

Data Collection 相关签名：

```csharp
void DataCollectionExt(DataCollectionFlags flags, Axis axis, string array, int nSample, double period, string vars)
void StopCollect()
void WaitCollectEndExt(int timeout, Axis axis)
object ReadVariable(string variable, ProgramBuffer nBuf, int from1, int to1, int from2, int to2)
double[] ReadRealVector(string var, int from1, int to1, ProgramBuffer nBuf)
double[] ReadRealMatrix(string var, int from1, int to1, int from2, int to2, ProgramBuffer nBuf)
void WriteVariable(object value, string variable, ProgramBuffer nBuf, int from1, int to1, int from2, int to2)
```

## 目标

- 支持一维等间隔 PEG 脉冲输出。
- 支持一维随机点 PEG 脉冲输出。
- 支持二维等间隔 PEG 脉冲输出，至少覆盖直线、圆弧、折线路径。
- 支持二维随机点 PEG 脉冲输出，兼容现有 `PosCompareData` 点表。
- 提供 Data Collection 启动、停止、等待和数据读回，用于验证 PEG 触发过程。
- 保持 `IControlCard` 和 `ControlCardBase` 公共接口稳定，不强制影响 Googol/ZMotion。
- 在 ACS 插件内部新增强类型工艺 API，并让现有 `ControlPosComparison(...)` 作为兼容入口。

## 非目标

- 不用 C# 定时器模拟高速脉冲；PEG 必须走 ACS 控制器硬件/固件路径。
- 不在本批实现复杂曲线 UI 或实时图表，只提供可被调试页或业务模块调用的 API 与结果模型。
- 不把固高二维位置比较语义原样套到 ACS；二维等间隔通过路径重采样和 PEG 点表实现。
- 不在本批处理 Flash 保存、EtherCAT/CoE、FRF、激光 `LCENABLE/LCDISABLE` 专用流程。
- 不保证非单调、频繁回头、自相交轨迹在单个 PEG 引擎里天然正确；这类路径需要拆段或由控制器侧 ACSPL Buffer 承载更复杂逻辑。

## 总体架构

继续保持 `AcsControlCard` 的 partial 文件结构，新增三个 ACS 专用文件：

- `AcsProcessModels.cs`：PEG、二维路径、Data Collection 请求和结果模型。
- `AcsPeg.cs`：PEG 分配、增量 PEG、随机 PEG、一维/二维兼容入口。
- `AcsDataCollection.cs`：Data Collection 启停、等待和数组读回。

二维等间隔 PEG 不直接依赖一个“二维 PEG API”。实现方式是：

1. C# 根据二维路径按路径长度生成等间隔采样点。
2. 将采样点投影到 ACS PEG 参考轴位置序列。
3. 将位置序列和输出状态序列写入控制器变量数组。
4. 调用 `PegRandomNTV2(...)` 启动随机 PEG 点表输出。

一维等间隔 PEG 直接调用 `PegIncNTV2(...)`，因为该 API 原生支持 `firstPoint / interval / lastPoint`。

## 数据模型

### PEG 基础配置

新增 `AcsPegOutputConfig`：

- `En_AxisNum Axis`：业务轴。
- `int EngineToEncoderBitCode`：PEG 引擎到编码器映射位码。
- `int GeneralOutputBitCode`：可作为 PEG 输出的通用输出位码。
- `int OutputIndex`：PEG 输出映射索引。
- `int OutputBitCode`：物理输出映射位码。
- `double PulseWidth`：脉宽。
- `int Timeout`：等待准备超时。
- `MotionFlags Flags`：默认运动标志，支持同步启动等。

### 一维等间隔请求

新增 `AcsPegIncrementalRequest`：

- `AcsPegOutputConfig Output`。
- `double FirstPoint`。
- `double Interval`。
- `double LastPoint`。
- `bool StartImmediately`。
- `bool WaitReadyBeforeStart`。

校验规则：

- `Interval` 必须大于 0。
- `FirstPoint`、`LastPoint`、`PulseWidth` 必须是有限数。
- `FirstPoint <= LastPoint` 表示正方向点列；如果需要反方向，必须由调用方传入符合 ACS 控制器约束的点列或走随机 PEG。

### 二维路径请求

新增 `AcsPeg2DPathRequest`：

- `AcsPegOutputConfig Output`：PEG 参考轴和输出映射。
- `En_AxisNum XAxis`、`En_AxisNum YAxis`。
- `AcsPeg2DPathKind PathKind`：`Line`、`ArcCenter`、`Polyline`。
- `AcsPoint2D Start`、`AcsPoint2D End`。
- `AcsPoint2D Center`：圆弧中心方式使用。
- `DirOfRotation ArcDirection`。
- `IList<AcsPoint2D> Points`：折线路径使用。
- `double Interval`：二维路径长度等间隔。
- `double PulseWidth`。
- `string PointArrayName`、`string StateArrayName`：控制器变量数组名。
- `int StateValue`：输出状态值，默认 1。
- `bool StartImmediately`。

二维采样规则：

- 直线：按起点到终点的直线长度，每隔 `Interval` 生成一个采样点。
- 圆弧：按圆心、方向和起终点计算弧长，每隔 `Interval` 生成一个采样点。
- 折线：按累计折线长度重采样，跨段时保持等路径长度间隔。
- 最后一个点是否包含终点由采样函数控制：当终点距离最近触发点小于一个极小容差时不重复追加。

二维 PEG 点表映射规则：

- 第一版将二维路径采样点投影为参考轴位置序列：默认使用 `XAxis` 的位置作为 PEG 触发坐标。
- 如果路径不是 X 单调，则允许切换参考轴为 `YAxis`；如果两个轴都非单调，则返回失败并提示需要拆段或控制器侧 Buffer。
- 点表写入 `PointArrayName`，状态写入 `StateArrayName`，再调用 `PegRandomNTV2(...)`。

### Data Collection 请求

新增 `AcsDataCollectionRequest`：

- `En_AxisNum Axis`：采集关联轴。
- `string ArrayName`：控制器采集数组名。
- `int SampleCount`。
- `double Period`。
- `string Variables`：ACS 变量表达式，例如位置、速度、输出状态。
- `DataCollectionFlags Flags`。
- `int Timeout`。

新增 `AcsDataCollectionResult`：

- `bool Success`。
- `string Message`。
- `string ArrayName`。
- `int SampleCount`。
- `double[] Values`：一维读回。
- `double[] MatrixValues`：多变量矩阵读回时的扁平数据。

## API 设计

### ACS 专用 PEG API

在 `AcsControlCard` 中新增 public 方法：

```csharp
public bool ConfigurePegOutput(AcsPegOutputConfig config, out string message)
public bool StartIncrementalPeg(AcsPegIncrementalRequest request, out string message)
public bool StartRandomPeg(AcsPegRandomRequest request, out string message)
public bool StartEquidistantPeg2D(AcsPeg2DPathRequest request, out string message)
public bool StopPeg(En_AxisNum axisId, out string message)
public bool WaitPegReady(En_AxisNum axisId, int timeout, out string message)
```

### 兼容现有位置比较入口

重写 ACS 的 `ControlPosComparison(bool On_Off, PosComparisonOutputParam param)`：

- `On_Off = false`：按 `param.psoIndex` 映射到对应 PEG 轴并停止。
- `compareDimension = 1`：使用 `PegIncNTV2` 或随机点表，取决于 `param.PosCompareDatas` 是否存在。
- `compareDimension = 2`：优先使用 `param.PosCompareDatas` 作为二维随机点表；如果只有 `syncPos` 和起终点上下文不足，则返回明确失败信息，不猜测路径。

由于现有 `PosComparisonOutputParam` 缺少二维路径起终点、圆弧和折线信息，完整二维等间隔路径应走新的 `StartEquidistantPeg2D(...)` ACS 专用方法。

### Data Collection API

在 `AcsControlCard` 中新增 public 方法：

```csharp
public bool StartDataCollection(AcsDataCollectionRequest request, out string message)
public bool StopDataCollection(out string message)
public bool WaitDataCollectionEnd(En_AxisNum axisId, int timeout, out string message)
public bool ReadDataCollection(AcsDataCollectionRequest request, out AcsDataCollectionResult result)
public bool RunDataCollection(AcsDataCollectionRequest request, out AcsDataCollectionResult result)
```

`RunDataCollection(...)` 是组合方法：启动、等待、读取，适合调试和验证。

## 错误处理与安全策略

- 所有 API 首先检查 `IsConnected`。
- 所有点位、间隔、脉宽、采样周期必须是有限数，采样数必须大于 0。
- PEG 输出映射位码不做硬编码假设，由配置传入；默认值只作为模板，不代表真实设备可用。
- 二维路径如果无法映射到单调参考轴，返回失败并提示拆段。
- 启动 PEG 前先配置输出映射，再配置点列，再等待 ready，最后按请求决定是否启动。
- 启动失败时尽量调用 `StopPegNT(...)` 清理该 PEG 轴。
- Data Collection 失败时尽量调用 `StopCollect()`。
- 所有异常写入 Console 并返回包含轴、数组名、PEG 参数的 message，便于现场定位。

## 配置扩展

扩展 `AcsControlCardOptions`：

- `ObservableCollection<AcsPegOutputConfig> PegOutputs`。
- `string DefaultPegPointArrayName`，默认 `RY_PEG_POINTS`。
- `string DefaultPegStateArrayName`，默认 `RY_PEG_STATES`。
- `string DefaultDataCollectionArrayName`，默认 `RY_DC_DATA`。
- `int DefaultDataCollectionSampleCount`，默认 1000。
- `double DefaultDataCollectionPeriod`，默认 1.0。

`EnsureOptions()` 同步补齐常用轴的 PEG 输出配置，但默认不启用或不假设实际 IO 映射正确。

## 验证方式

本批至少执行：

```powershell
dotnet build 'Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj' --no-restore -p:SolutionDir='E:\Company\工作目录\ReeYin-V\ReeYin-V\'
```

通过标准：

- `0 errors`。
- 允许现有 `halcondotnet` 引用 warning。
- Program Buffer、回零、基础 IO 构建不回退。

可选硬件/模拟器验证：

- 一维 `StartIncrementalPeg`：在低速单轴运动中观察等间隔输出。
- 二维 `StartEquidistantPeg2D`：对直线轨迹生成点表，读回点表确认间距一致，再在低风险输出通道验证脉冲。
- Data Collection：采集位置变量，确认返回数据长度和采样周期符合设置。

## 风险与应对

- 风险：PEG 输出映射与具体控制器型号、固件和 IO 端子强相关。应对：所有映射由配置传入，默认值不自动启用。
- 风险：二维等间隔依赖路径和参考轴单调性。应对：支持直线、圆弧、折线重采样；非单调路径明确失败并提示拆段。
- 风险：现有 `PosComparisonOutputParam` 信息不足以表达完整二维路径。应对：兼容入口只处理点表；完整二维等间隔使用 ACS 专用 API。
- 风险：Data Collection 消耗控制器实时资源。应对：限制默认采样数，失败时停止采集，并提供明确错误信息。
- 风险：真实脉冲输出无法仅靠编译证明。应对：C# 验证 SDK 调用、点表生成和读回；真实 IO 由用户在低风险设备上验证。

## 自审结果

- 完成度扫描：未发现未完成标记或临时标记。
- 一致性检查：目标、API、配置、验证均围绕一维/二维 PEG 与 Data Collection，不包含 Flash/EtherCAT 等非本批范围。
- 范围检查：本设计足够形成单个实施计划；二维复杂路径只支持直线、圆弧、折线，非单调路径明确失败。
- 歧义处理：明确一维等间隔用 `PegIncNTV2`，二维等间隔用路径重采样和 `PegRandomNTV2` 点表，不使用 C# 定时器模拟脉冲。


