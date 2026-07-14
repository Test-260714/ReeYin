# Control Card Sync Capabilities Design

## 背景

当前 `ReeYin_V.Hardware.ControlCard` 的 `ControlCardBase` 同时承担基础运动流程、限位校验、默认空实现、部分固高特定能力入口等职责。随着 `ReeYin_V.Hardware.ControlCard.ACS` 增加 XSEG、LCI、Buffer、Coordinate Array Pulse、DataCollection 等能力，单纯把 ACS 的 helper 上移到基类并不够合理。

固高和 ACS 的底层同步模型不同：

- 固高以 CRD 坐标系、FIFO/缓存区、位置比较、`BufIO`、`BufDelay`、`CrdData`、`CrdMoveStart` 为核心。
- ACS 以基础插补、XSEG 队列、ACSPL Buffer、LCI 固定距离脉冲、Coordinate Array Pulse、DataCollection 为核心。

两者不能直接用同一组底层方法统一，但可以用相同的业务语义统一：多轴协调运动、运动队列/Buffer、按位置同步触发、按轨迹采集。

## 目标

1. 优化 `ControlCardBase` 中所有控制卡都需要的公共流程。
2. 保持 `IControlCard` 的基础角色，不继续污染厂商专用能力。
3. 用能力接口表达固高和 ACS 的同步能力，让业务层按能力调用，而不是按厂商类型分支。
4. 保留现有固高、ACS 调用兼容性，分阶段迁移，降低风险。

## 非目标

1. 不把 ACS 专用的 LCI、XSEG、ACSPL Buffer API 放入 `ControlCardBase`。
2. 不把固高专用的 CRD/FIFO/多核 `Barrier` 细节放入公共基类。
3. 不在本轮实现跨不同控制卡之间的硬件时钟同步。
4. 不一次性删除 `ControlPosComparison`、`BufIO`、`BufDelay` 等历史入口。

## 设计原则

- 基类统一“基础流程”，能力接口统一“业务语义”，厂商类保留“底层实现”。
- 上层业务优先判断能力接口，例如 `ISynchronizedTriggerCard`，而不是判断 `AcsControlCard` 或 `GoogolControlCard`。
- 任何新接口都必须表达“可选能力”，未支持的控制卡不需要伪实现。
- ACS 和固高已有测试必须继续通过。

## 第一层：ControlCardBase 公共优化

`ControlCardBase` 负责所有控制卡共用的安全流程和配置解析。

计划增加或优化以下受保护方法：

- `TryGetAxisConfig(En_AxisNum axisNum, out SingleAxisParam axis)`
- `GetConfiguredAxes(bool onlyUsing = true)`
- `GetOneBasedAxisNo(SingleAxisParam axis)`
- `GetZeroBasedAxisNo(SingleAxisParam axis)`
- `EnsurePositionBuffers(int requiredLength = 0)`
- `ResolveSpeedSetting(En_AxisNum axisNum, EN_SpeedType? speedType = null)`
- `GetAxisDefaultVelocity(En_AxisNum axisNum, EN_SpeedType? speedType = null)`
- `NormalizeRelativeDistance(MoveDirection direction, double distance)`
- `WaitUntilAxisStopped(En_AxisNum axisNum, int timeoutMs, int pollingIntervalMs = 20)`
- `WaitUntilAllAxesStopped(IEnumerable<En_AxisNum> axes, int timeoutMs, int pollingIntervalMs = 20)`
- `TryBuildCoordinatedTargets(...)`，用于统一校验插补轴、目标点、重复轴、非法数值。

计划增加以下 hook：

- `protected virtual int MotionTimeoutMs => 60000`
- `protected virtual bool RequiresHomeBeforeMove => true`
- `protected virtual void OnConfigChanged()`

计划修复以下基础行为：

- `Move(En_AxisNum, MoveDirection, double, out string)` 使用 `MoveDirection` 转换正负距离。
- 相对移动等待停止时使用超时，不再无限循环。
- `GoHome(out string message)` 返回 `DoGoHome(out message)` 的真实结果，异常时返回 `false`。
- `Config` set 后调用 `OnConfigChanged()`，方便 ACS 在配置变化后同步 `Options.HomeBuffers`、`Options.PegOutputs`。
- 关闭事件走统一关闭路径，避免直接 `DoClose()` 后状态未同步。

## 第二层：同步能力接口

新增能力接口，不替代 `IControlCard`，用于可选功能发现。

```csharp
public interface ICoordinatedMotionCard
{
    bool SupportsCoordinatedMotion { get; }
    bool MoveCoordinated(CoordinatedMotionRequest request, out string message);
}
```

```csharp
public interface IBufferedMotionCard
{
    bool SupportsBufferedMotion { get; }
    bool ClearMotionBuffer(short coordinateOrBuffer, out string message);
    bool CommitMotionBuffer(short coordinateOrBuffer, bool waitForEnd, out string message);
}
```

```csharp
public interface ISynchronizedTriggerCard
{
    SynchronizedTriggerCapabilities TriggerCapabilities { get; }
    bool RunSynchronizedTrigger(
        SynchronizedTriggerRequest request,
        out SynchronizedTriggerResult result,
        out string message);
}
```

核心枚举：

```csharp
public enum SynchronizedTriggerMode
{
    BufferedIo,
    PositionCompare,
    FixedDistancePulse,
    CoordinateArrayPulse,
    DataCollection
}
```

```csharp
[Flags]
public enum SynchronizedTriggerCapabilities
{
    None = 0,
    BufferedIo = 1,
    PositionCompare = 2,
    FixedDistancePulse = 4,
    CoordinateArrayPulse = 8,
    DataCollection = 16
}
```

请求模型表达业务语义：

- 参与轴集合。
- 目标点集合，要求保留输入顺序。
- 触发模式。
- 脉冲宽度、间距、窗口、Buffer 编号等可选参数。
- 是否等待完成。
- 超时。

具体字段在实现计划中根据现有 ACS/固高参数模型落地，避免提前过度设计。

## 第三层：固高适配

固高实现 `ICoordinatedMotionCard`：

- `Line` 映射到 `CrdMoveAbsoluteToPointXY/XYZ/XYZR`。
- `Arc` 映射到 `CrdMoveAbsoluteToArcXYR/XYC`。
- `Custom/Polyline` 映射到 `CustomInterpolationMoving` 和 `PushOrder`。
- 运动提交保留 `CrdData` 和 `CrdMoveStart`。
- 多核同步启动保留 `Barrier`，作为固高内部实现细节。

固高实现 `IBufferedMotionCard`：

- `ClearMotionBuffer` 映射到 `CrdBufClear`。
- `CommitMotionBuffer` 映射到 `CrdData` 和 `CrdMoveStart`。

固高实现 `ISynchronizedTriggerCard`：

- `PositionCompare` 映射到 `ControlPosComparison`、`InsertPosCompareData`。
- `BufferedIo` 映射到 `BufIO`。
- 延时类指令复用 `BufDelay`，可由 `BufferedIo` 请求中的可选延时表达。

历史方法继续保留，旧业务不受影响。

## 第四层：ACS 适配

ACS 实现 `ICoordinatedMotionCard`：

- `Line` 映射到 `LineInterpoMoving`。
- `XsegLine/Polyline` 映射到 `LineInterpoMovingXseg` 或现有 XSEG helper。
- `Arc` 映射到 `ArcInterpoMoving` 或 `ArcInterpoMovingXseg`。
- 目标解析、重复轴校验、有限数校验、限位校验复用 `ControlCardBase` helper。

ACS 实现 `IBufferedMotionCard`：

- `ClearMotionBuffer` 映射到 `TryClearProgramBuffer`。
- `CommitMotionBuffer` 映射到 `TryRunProgramBuffer` 或特定 Buffer 运行流程。

ACS 实现 `ISynchronizedTriggerCard`：

- `FixedDistancePulse` 映射到 `TryRunLciFixedDistancePulseXseg`。
- `CoordinateArrayPulse` 映射到 `TryRunLciCoordinateArrayPulse`。
- `DataCollection` 映射到 `RunDataCollection`。

LCI、XSEG、ACSPL Buffer 脚本生成仍留在 ACS 项目内，不进入公共基类。

## 上层业务迁移

后续业务优先按能力调用：

```csharp
if (controlCard is ISynchronizedTriggerCard triggerCard &&
    triggerCard.TriggerCapabilities.HasFlag(SynchronizedTriggerCapabilities.CoordinateArrayPulse))
{
    triggerCard.RunSynchronizedTrigger(request, out result, out message);
}
```

`Custom.WaferFlatnessMeasure` 的点模式可按此顺序选择：

1. ACS `CoordinateArrayPulse`，保留输入点顺序。
2. 固高 `PositionCompare`，保留输入点顺序。
3. 普通逐点移动 fallback。

## 测试策略

基类测试：

- 反向相对移动会传入负距离。
- 回零失败时 `GoHome` 返回 `false`。
- 等待停止超时返回失败，不无限阻塞。
- 插补目标缺失、重复轴、非法数值会被拒绝。
- 完整目标字典参与联动限位校验。

固高测试：

- 固高声明 `PositionCompare` 和 `BufferedIo` 能力。
- 点集合按输入顺序进入位置比较队列。
- `BufIO`、`BufDelay`、`CrdData`、`CrdMoveStart` 历史入口仍存在。
- 固高多核 `Barrier` 启动逻辑不被上移或删除。

ACS 测试：

- ACS 声明 `FixedDistancePulse`、`CoordinateArrayPulse`、`DataCollection` 能力。
- Coordinate Array Pulse 仍按输入点顺序写入 ACS 数组。
- LCI/XSEG 不出现在 `IControlCard` 和 `ControlCardBase`。
- 现有 ACS 测试全部通过。

## 实施阶段

### 阶段一：基类安全优化

只修改 `ControlCardBase` 和必要测试，修复方向、回零返回值、等待超时、公共 helper、配置 hook。

### 阶段二：新增能力模型和接口

在 `ReeYin_V.Hardware.ControlCard` 中新增能力接口和请求/结果模型。固高和 ACS 分别实现接口，但保留原有方法。

### 阶段三：业务层迁移

将 `Custom.WaferFlatnessMeasure` 等依赖厂商判断的路径迁移到能力判断，优先使用同步触发能力，最后 fallback 到普通运动。

## 风险与约束

- `GoHome` 返回值修复可能暴露以前被吞掉的失败，需要检查调用方是否依赖旧行为。
- 固高部分源码存在乱码文本，测试断言应尽量基于方法名和结构，不依赖乱码字符串。
- ACS 构建可能受正在运行的主程序或 Visual Studio 锁定 DLL 影响，验证时优先使用项目级 build 和现有 ACS.Tests。
- 不在本轮承诺跨控制卡硬件时钟同步。
