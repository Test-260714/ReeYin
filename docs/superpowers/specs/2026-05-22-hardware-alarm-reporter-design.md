# 硬件报警统一上报设计

## 背景

当前 `ReeYin.AlarmCenter` 已具备实时报警、历史查询、统计分析、确认和清除能力，底层由 `ReeYin_V.Core` 中的 `IAlarmService` / `AlarmService` 提供报警生命周期管理。硬件模块中仍存在报警处理分散的问题：部分模块只弹窗、写日志或设置 `HardwareState`，没有统一进入报警中心；不同模块对编码、来源、位置、严重等级和恢复清除的处理也不一致。

本次优化选择“统一硬件报警框架 + 典型模块试点接入”，并明确所有核心能力必须放在 `ReeYin_V.Core` 项目下。硬件模块只调用 Core 提供的接口，不直接依赖 `ReeYin.AlarmCenter` UI 项目。

## 目标

- 在 `Core\ReeYin-V.Core` 下建立硬件报警统一上报能力。
- 统一硬件报警编码、来源、分类、严重等级、确认策略和清除策略。
- 支持连接失败、断线、初始化失败、操作失败、安全异常、恢复清除等常见场景。
- 先接入 PLC、ZMotion 运动控制卡、Truelight3D 传感器作为试点，形成后续模块迁移模板。
- 保持 `ReeYin.AlarmCenter` 现有 UI 和数据流基本不变，让硬件报警通过 `IAlarmService` 自然展示。

## 非目标

- 本阶段不全量改造所有硬件模块。
- 本阶段不重构 `ReeYin.AlarmCenter` UI。
- 本阶段不实现完整报警规则配置界面。
- 本阶段不改变 `AlarmService` 的持久化模型和历史查询接口。

## 放置位置

核心文件放在：

```text
Core\ReeYin-V.Core\Services\Alarm\Hardware
```

建议新增文件：

```text
IHardwareAlarmReporter.cs
HardwareAlarmReporter.cs
HardwareAlarmRequest.cs
HardwareAlarmCodes.cs
HardwareAlarmSources.cs
HardwareAlarmCategories.cs
```

如后续硬件报警规则变复杂，可继续增加：

```text
HardwareAlarmSeverityPolicy.cs
HardwareAlarmThrottlePolicy.cs
```

## 核心组件

### IHardwareAlarmReporter

硬件模块调用的统一入口。接口只暴露硬件语义，不要求硬件模块直接拼 `AlarmRaiseRequest`。

建议接口：

```csharp
public interface IHardwareAlarmReporter
{
    void ReportConnectionFailed(string source, string location, string message, Exception? exception = null);

    void ReportDisconnected(string source, string location, string message, Exception? exception = null);

    void ReportInitializationFailed(string source, string location, string message, Exception? exception = null);

    void ReportOperationFailed(string source, string location, string operation, string message, Exception? exception = null);

    void ReportSafetyError(string source, string location, string message, Exception? exception = null);

    void Report(HardwareAlarmRequest request);

    void Clear(string code, string source, string location, string note = "硬件状态恢复");
}
```

### HardwareAlarmReporter

默认实现放在 Core，依赖现有 `IAlarmService`。主要职责：

- 将硬件场景转换为 `AlarmRaiseRequest`。
- 统一设置 `Code`、`Name`、`Category`、`Level`、`NeedAcknowledge`、`AllowManualClear`。
- 将异常类型、异常消息、操作名、错误码、设备名、IP、端口、轴号等放入 `ExtraData`。
- 调用 `IAlarmService.AddAlarm(...)` 上报。
- 调用 `IAlarmService.ClearAlarm(...)` 清除。
- 做简单参数防御，避免空 `source`、空 `location`、空 `message` 造成无效报警。
- 可选做短时间节流，避免同一硬件异常在高频循环里反复上报。

### HardwareAlarmRequest

面向复杂场景的请求模型，供试点模块或后续模块在需要更多上下文时使用。

建议字段：

- `Code`
- `Name`
- `Category`
- `Source`
- `Location`
- `Message`
- `Severity`
- `Operation`
- `ErrorCode`
- `NeedAcknowledge`
- `AllowManualClear`
- `Exception`
- `ExtraData`

### HardwareAlarmCodes

统一编码常量。第一阶段建议包含：

```text
HW.CONNECTION_FAILED
HW.DISCONNECTED
HW.INITIALIZATION_FAILED
HW.OPERATION_FAILED
HW.SAFETY_ERROR
HW.PLC.HEARTBEAT_TIMEOUT
HW.PLC.READ_WRITE_FAILED
HW.MOTION.CONTROLLER_ERROR
HW.MOTION.SERVO_ALARM
HW.MOTION.LIMIT_TRIGGERED
HW.SENSOR.ACQUIRE_FAILED
HW.SENSOR.NO_DATA
```

### HardwareAlarmSources

统一来源常量。第一阶段建议包含：

```text
PLC
MotionCard
Sensor
Camera
LightController
ControlCard
```

### HardwareAlarmCategories

统一分类常量。第一阶段建议包含：

```text
硬件通讯
硬件初始化
硬件操作
运动安全
采集设备
配置错误
```

## PrismProvider 暴露方式

`HardwareAlarmReporter` 通过 Core 现有 `[ExposedService]` 注册为单例。

为降低试点模块改造成本，建议在 `PrismProvider` 增加静态入口：

```csharp
public static IHardwareAlarmReporter HardwareAlarmReporter { get; private set; }
```

构造函数中通过 DI 注入并赋值。这样硬件模块可以沿用当前项目中已有的 `PrismProvider.AlarmService` 风格：

```csharp
PrismProvider.HardwareAlarmReporter.ReportConnectionFailed(...);
```

如果某些模块已具备 DI 条件，可直接注入 `IHardwareAlarmReporter`。试点阶段优先使用 `PrismProvider.HardwareAlarmReporter`，降低改动面。

## 报警规则

### 连接失败

- 默认编码：`HW.CONNECTION_FAILED`
- 默认等级：`AlarmSeverity.Error`
- 默认分类：`硬件通讯`
- `NeedAcknowledge = true`
- `AllowManualClear = false`
- 清除条件：连接成功或心跳恢复。

### 断线

- 默认编码：`HW.DISCONNECTED`
- 默认等级：`AlarmSeverity.Error`
- 默认分类：`硬件通讯`
- `NeedAcknowledge = true`
- `AllowManualClear = false`
- 清除条件：重新连接成功。

### 初始化失败

- 默认编码：`HW.INITIALIZATION_FAILED`
- 默认等级：`AlarmSeverity.Error`
- 默认分类：`硬件初始化`
- `NeedAcknowledge = true`
- `AllowManualClear = true`
- 清除条件：初始化成功或配置修复后成功连接。

### 操作失败

- 默认编码：`HW.OPERATION_FAILED`
- 默认等级：`AlarmSeverity.Error`
- 默认分类：`硬件操作`
- `NeedAcknowledge = true`
- `AllowManualClear = true`
- 清除条件：下一次相同操作成功，或流程复位后清除。

### 安全异常

- 默认编码：`HW.SAFETY_ERROR`
- 默认等级：`AlarmSeverity.Fatal`
- 默认分类：`运动安全`
- `NeedAcknowledge = true`
- `AllowManualClear = false`
- 清除条件：硬件复位、状态恢复安全条件后由代码清除。

## 试点接入

### PLC

代表通讯类硬件报警。

接入点：

- 连接失败。
- 断开异常。
- 心跳地址未配置或心跳超时。
- 读写 PLC 异常。
- 心跳恢复或连接成功时清除相关报警。

建议编码：

```text
HW.PLC.HEARTBEAT_TIMEOUT
HW.PLC.READ_WRITE_FAILED
HW.CONNECTION_FAILED
HW.DISCONNECTED
```

### ZMotion 运动控制卡

代表高严重度运动控制报警。

接入点：

- 控制卡连接失败。
- 控制卡异常状态。
- 伺服报警。
- 限位触发。
- 运动安全类异常。
- 控制卡恢复连接或复位成功后清除可恢复报警。

建议编码：

```text
HW.MOTION.CONTROLLER_ERROR
HW.MOTION.SERVO_ALARM
HW.MOTION.LIMIT_TRIGGERED
HW.SAFETY_ERROR
```

安全类报警默认 `Fatal`，不允许手动清除。

### Truelight3D 传感器

代表采集类硬件报警。

接入点：

- 连接失败。
- 开始采集失败。
- 读取结果失败。
- 点云输出或测量数据为空。
- 下一次成功采集或成功读取结果后清除对应报警。

建议编码：

```text
HW.SENSOR.ACQUIRE_FAILED
HW.SENSOR.NO_DATA
HW.CONNECTION_FAILED
```

## 数据流

```text
硬件模块状态变化或异常
  -> PrismProvider.HardwareAlarmReporter.ReportXxx(...)
  -> HardwareAlarmReporter 构造 AlarmRaiseRequest
  -> IAlarmService.AddAlarm(...)
  -> AlarmService 去重、更新活动报警、写入实时流、异步持久化
  -> ReeYin.AlarmCenter 实时页/历史页/统计页展示

硬件恢复
  -> PrismProvider.HardwareAlarmReporter.Clear(...)
  -> IAlarmService.ClearAlarm(...)
  -> 活动报警关闭，进入历史记录
```

## 错误处理

- `HardwareAlarmReporter` 不应向硬件业务流程抛出报警上报异常。
- 如果报警上报失败，应写入现有日志系统，避免影响设备控制流程。
- 对空参数做降级处理：
  - `source` 为空时使用 `Hardware`。
  - `location` 为空时使用 `Unknown`。
  - `message` 为空时使用默认中文描述。
- 异常对象不直接序列化完整堆栈到 `Message`，避免 UI 过长；堆栈可放入 `ExtraData` 或只记录到日志。

## 去重与节流

现有 `AlarmService` 已按 `Code + Source + Location` 对活动报警去重。本设计继续沿用该机制。

Reporter 层建议增加轻量节流：

- 同一 `Code + Source + Location` 在 1 秒内重复上报时可跳过。
- 安全类报警不跳过，但仍依赖 `AlarmService` 合并为同一活动报警。
- 清除操作不节流。

## 测试策略

### 构建验证

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore
```

### 单元验证

建议为 `HardwareAlarmReporter` 增加轻量测试或调试用假服务：

- `ReportConnectionFailed` 生成 `HW.CONNECTION_FAILED`。
- `ReportSafetyError` 使用 `Fatal` 且 `AllowManualClear = false`。
- `Clear` 调用 `IAlarmService.ClearAlarm`。
- 异常信息进入 `ExtraData`。
- 空参数有默认值。

### 手工验证

- 模拟 PLC 连接失败，报警中心实时页出现活动报警。
- 模拟 PLC 心跳恢复，对应报警被清除并进入历史。
- 模拟 ZMotion 运动安全异常，报警等级为致命，不能手动清除。
- 模拟 Truelight3D 采集失败，报警出现；下一次采集成功后清除。

## 分阶段实施

### 第一阶段：Core 框架

- 新增 `IHardwareAlarmReporter`。
- 新增 `HardwareAlarmReporter`。
- 新增硬件报警请求和常量类。
- 在 `PrismProvider` 暴露 `HardwareAlarmReporter`。
- 保持 `ReeYin.AlarmCenter` 不改。

### 第二阶段：试点接入

- PLC 接入连接、心跳、读写异常。
- ZMotion 接入连接和安全异常。
- Truelight3D 接入连接、采集、无数据异常。

### 第三阶段：推广模板

- 总结试点接入方式。
- 逐步迁移相机、光源、其他传感器、其他运动控制卡。
- 评估是否增加硬件报警配置界面。

## 风险与缓解

- 风险：硬件模块状态语义不一致，可能误报。
  - 缓解：第一阶段不做全局自动拦截，只在明确失败点手动调用 reporter。
- 风险：高频操作失败导致报警中心刷新压力过大。
  - 缓解：Reporter 层节流，`AlarmService` 层继续生命周期去重。
- 风险：安全报警被误清除。
  - 缓解：安全类默认 `AllowManualClear = false`，只在硬件恢复/复位代码路径清除。
- 风险：硬件模块依赖 Core 后出现循环依赖。
  - 缓解：Reporter 放在 `ReeYin_V.Core`，硬件项目本来已依赖 Core，不依赖 UI 项目。
- 风险：报警上报失败影响硬件动作。
  - 缓解：Reporter 捕获异常并写日志，不阻塞硬件业务流程。

## 验收标准

- Core 中存在统一硬件报警上报接口和实现。
- PLC、ZMotion、Truelight3D 至少各完成一个代表性报警接入。
- 触发试点异常后，`ReeYin.AlarmCenter` 实时报警可看到对应记录。
- 恢复试点硬件状态后，对应活动报警可自动清除并进入历史。
- 构建通过，无新增编译错误。
- 硬件模块不直接依赖 `ReeYin.AlarmCenter`。
