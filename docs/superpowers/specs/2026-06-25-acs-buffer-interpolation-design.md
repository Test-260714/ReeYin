# ACS Buffer 插补运动替换设计

## 背景

`ReeYin_V.Hardware.ControlCard.ACS` 当前存在两类插补实现：

- `LineInterpolation.cs` 中的普通直线插补直接调用 ACS SDK 的 `Line` / `ExtLine`。
- `CircularInterpolation.cs` 中的普通圆弧插补直接调用 ACS SDK 的 `Arc1` / `ExtArc1`。
- `XsegInterpolation.cs` 中的 XSEG 直线/圆弧辅助直接调用 ACS SDK 的 `ExtendedSegmentedMotionV2`、`SegmentLineV2`、`SegmentArc1V2`、`EndSequenceM`。

用户要求移除 ACS 项目中这些 SDK 直连插补运动方式，改为参考 `AcsLciFixedDistancePulse` 的实现，通过 ACS Program Buffer 运行插补脚本；同时替换 `ReeYin_V.Hardware.ControlCard` 项目中与插补入口相关的实现，避免通用项目继续用旧的 `CustomInterpolationMoving + LineInterpoMoving` 组合直接驱动 ACS 插补。

## 目标

1. ACS 的直线、圆弧、XSEG 直线、XSEG 圆弧插补全部改为 Buffer 脚本执行。
2. 插补路径不再直接调用 ACS SDK 插补运动 API：
   - `Line`
   - `ExtLine`
   - `Arc1`
   - `ExtArc1`
   - `ExtendedSegmentedMotionV2`
   - `SegmentLineV2`
   - `SegmentArc1V2`
   - `EndSequenceM`
3. 保留现有公共接口：
   - `LineInterpoMoving(LineInterPoParam param)`
   - `ArcInterpoMoving(ArcInterPoParam param)`
   - `MoveCoordinated(CoordinatedMotionRequest request, out string message)`
4. 基础项目 `ReeYin_V.Hardware.ControlCard` 不引入 ACS 项目依赖，不出现 `AcsControlCard` 类型判断。
5. 基础项目的通用移动入口优先使用 `ICoordinatedMotionCard`，不支持能力接口时再保留原有 fallback。

## 非目标

- 不改 ACS 单轴点动、相对运动、绝对运动、回零、PEG、LCI 触发和数据采集的 SDK 调用。
- 不要求删除 ACS SDK 引用；Buffer 操作本身仍需使用 ACS SDK 的 `LoadBuffer`、`CompileBuffer`、`RunBuffer`、`WaitProgramEnd`。
- 不扩展固高等其它厂商板卡的插补实现。
- 不改 UI 结构，除非测试发现必须暴露新的 Buffer 编号配置。

## 设计方案

### 1. ACS 内新增统一插补 Buffer 执行器

在 ACS 项目内新增或重构一个专门负责插补 Buffer 的实现单元，例如：

- `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/App/InterpolationBufferMotion.cs`

该单元负责：

- 校验连接状态、轴配置、轴使能、轴停止状态。
- 复用现有 `TryBuildInterpolationMove` / `TryBuildAxes` / `TryResolveArcCenter` 等校验和几何计算逻辑。
- 构造 ACSPL+ 脚本。
- 通过 `TryPrepareProgramBuffer` 获取 Program Buffer。
- 参考 `AcsLciFixedDistancePulse` 的流程执行：
  1. 尝试停止目标 Buffer。
  2. `_api.LoadBuffer(buffer, script)`。
  3. `_api.CompileBuffer(buffer)`。
  4. `_api.RunBuffer(buffer, null)`。
  5. 按参数等待 `_api.WaitProgramEnd(buffer, timeout)`。
  6. 更新轴状态。

默认 Buffer 编号从 ACS 选项读取。基础模型 `CoordinatedMotionRequest` 增加可空 `BufferNo` 字段；当请求指定 Buffer 时使用请求值，否则使用 `AcsControlCardOptions.InterpolationBufferNo`。ACS 默认插补 Buffer 建议为 9，避免与现有 LCI 默认 Buffer 10 和 X/Y 回零默认 Buffer 1/2 冲突。

### 2. 生成普通直线插补脚本

`LineInterpoMoving(LineInterPoParam param)` 改为：

- 构建目标轴数组和目标点。
- 做限位校验。
- 生成 ACSPL+ 脚本：
  - 定义轴号变量，例如 `int Ax0 = 0`。
  - `ENABLE (...)` 并等待使能。
  - 按 `DefaultSpeed` 设置 `VEL/ACC/DEC/KDEC/JERK`。
  - 使用 ACSPL+ 的 `PTP/e` 或 `LINE` 语句完成直线插补。
  - `STOP` 结束 Buffer。
- 通过 Buffer 执行脚本。

普通直线插补不再调用 `_api.Line` 或 `_api.ExtLine`。

### 3. 生成 XSEG 直线插补脚本

`LineInterpoMovingXseg(LineInterPoParam param)` 改为：

- 读取当前反馈点作为 XSEG 起点。
- 构建目标点。
- 生成 ACSPL+ 脚本：
  - `XSEG (...)` 从反馈起点开始。
  - `LINE (...)` 或 `LINE/p (...)` 追加目标段。
  - `ENDS (...)` 结束队列。
  - `TILL GSEG(Ax0) = -1` 等待段队列完成。
  - `STOP` 结束 Buffer。
- 通过 Buffer 执行脚本。

XSEG 直线插补不再调用 `ExtendedSegmentedMotionV2`、`SegmentLineV2` 或 `EndSequenceM`。

### 4. 生成普通圆弧插补脚本

`ArcInterpoMoving(ArcInterPoParam param)` 改为：

- 保持现有轴选择、目标字典、限位校验和圆心解析。
- 生成 ACSPL+ 脚本：
  - 设置轴运动参数。
  - 根据起点/终点/圆心/方向执行圆弧插补语句。
  - `STOP` 结束 Buffer。
- 通过 Buffer 执行脚本。

普通圆弧插补不再调用 `_api.Arc1` 或 `_api.ExtArc1`。

### 5. 生成 XSEG 圆弧插补脚本

`ArcInterpoMovingXseg(ArcInterPoParam param)` 改为：

- 继续限制最多两个插补轴。
- 继续用当前反馈位置作为起点解析圆心，避免使用过期的 `param.Origin`。
- 生成 ACSPL+ 脚本：
  - `XSEG (...)` 从反馈起点开始。
  - `ARC1` / `ARC2` 追加圆弧段。
  - `ENDS (...)` 结束队列。
  - `TILL GSEG(Ax0) = -1` 等待队列完成。
  - `STOP` 结束 Buffer。
- 通过 Buffer 执行脚本。

XSEG 圆弧插补不再调用 `ExtendedSegmentedMotionV2`、`SegmentArc1V2` 或 `EndSequenceM`。

### 6. 基础项目入口适配

`ReeYin_V.Hardware.ControlCard` 中的通用移动入口应按能力接口优先：

- `CoordinateCacheViewModel` 已经优先走 `ICoordinatedMotionCard.MoveCoordinated`，保留该结构。
- `AxisViewModel` 的“移动至指定位置”当前直接调用 `CustomInterpolationMoving + LineInterpoMoving`，需要改为：
  - 若 `ControlCard is ICoordinatedMotionCard card && card.SupportsCoordinatedMotion`，构造 `CoordinatedMotionRequest` 并调用 `MoveCoordinated`。
  - 调用成功后再移动 Z / Z1 等额外轴。
  - 调用失败时停止其它轴移动。
  - 不支持能力接口时保留原 fallback。

基础项目不引用 ACS 命名空间，不判断 ACS 具体类型。

## 数据流

```text
通用移动入口
  -> ICoordinatedMotionCard.MoveCoordinated
    -> ACS BuildLineParam / ArcParam
      -> ACS 插补参数校验
        -> ACSPL+ 插补脚本
          -> LoadBuffer
          -> CompileBuffer
          -> RunBuffer
          -> WaitProgramEnd
          -> UpdateAxisStates
```

旧板卡或不支持能力接口的板卡：

```text
通用移动入口
  -> CustomInterpolationMoving
    -> LineInterpoMoving fallback
```

## 错误处理

- 参数为空、未连接、轴未配置、轴重复、目标缺失、目标非有限值时返回 `false` 并输出诊断信息。
- 限位校验失败时返回 `false`，沿用现有限位告警逻辑。
- Buffer 加载、编译、运行或等待失败时：
  - 尝试停止目标 Buffer。
  - 拼接 `FormatProgramDiagnostics(bufferNo)` 到错误信息。
  - 返回 `false`，阻止额外轴运动。
- `waitforend = false` 时允许只启动 Buffer，不等待结束；默认仍等待。

## 测试策略

更新 `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`：

1. 新增或替换 ACS 插补测试：
   - 普通直线插补脚本必须包含 `LoadBuffer` / `CompileBuffer` / `RunBuffer` / `WaitProgramEnd` 执行流程。
   - 普通直线插补源码不得包含 `_api.Line(`、`_api.ExtLine(`。
   - XSEG 直线插补源码不得包含 `ExtendedSegmentedMotionV2`、`SegmentLineV2`、`EndSequenceM`。
   - 普通圆弧插补源码不得包含 `_api.Arc1(`、`_api.ExtArc1(`。
   - XSEG 圆弧插补源码不得包含 `SegmentArc1V2`、`EndSequenceM`。
2. 保留已有“ACS-specific XSEG 方法不进入基础接口”的测试。
3. 更新 `AxisViewModel` 测试：
   - 不出现 ACS 命名空间或 `AcsControlCard`。
   - 优先使用 `ICoordinatedMotionCard`。
   - 协同移动失败时不继续移动 Z / Z1。
   - 不支持协同时保留 `CustomInterpolationMoving + LineInterpoMoving` fallback。
4. 构建验证：
   - `dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj --no-restore /p:SolutionDir=E:\Company\工作目录\ReeYin-V\ReeYin-V\ /m:1 /nodeReuse:false /p:UseSharedCompilation=false`
   - `dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore /p:SolutionDir=E:\Company\工作目录\ReeYin-V\ReeYin-V\ /m:1 /nodeReuse:false /p:UseSharedCompilation=false`
   - `dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-build`

## 风险

- ACSPL+ 圆弧语法需要与控制器环境一致；测试能保证脚本文本和流程，真实设备仍需现场联调。
- Buffer 编号若与用户已有脚本冲突，可能覆盖已有 Buffer 内容；默认插补 Buffer 应在 ACS 选项中明确并可配置。
- 现有测试中“basic line remains basic path”和“XSEG uses SDK queue”与新需求相反，需要按新语义替换。
- 旧的 `CustomInterpolationMoving` 对 ACS 来说会间接触发 Buffer 化后的 `LineInterpoMoving`，但基础项目应优先使用能力接口，减少旧路径依赖。
