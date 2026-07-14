# 简洁版报警数据结构设计

## 目标

重新收敛 `ReeYin_V.Core.Services.Alarm` 的核心数据结构，降低 `Hardware*`、`Governance*` 和配置页面之间的耦合。新的报警设计只暴露四类核心模型：

- `AlarmDefinition`：报警定义和提示策略。
- `AlarmSignal`：软件/硬件统一报警输入。
- `AlarmTriggerRule`：设备状态/硬件信号的自定义触发规则。
- `AlarmRecord`：报警运行态和历史态记录。

## 保留兼容

为避免一次性破坏现有数据库和调用链，旧模型和旧表第一阶段保留：

- `AlarmDefinitionInfo`
- `AlarmReportRequest`
- `AlarmRaiseRequest`
- `HardwareAlarmRuleInfo`
- `HardwareAlarmRuleEntity`

新增模型通过适配器映射到旧模型，现有数据库表不改名。

## Core 调用链

软件报警：

```text
SoftwareAlarmReporter -> IAlarmService.Report(AlarmSignal) -> AlarmService -> AlarmDefinitionResolver -> AlarmRecord
```

硬件报警：

```text
HardwareStatusChangedEvent -> HardwareAlarmMonitorService -> AlarmTriggerRuleEngine -> IAlarmService.Report(AlarmSignal)
```

配置弹窗：

```text
AlarmCenter -> IAlarmConfigService -> IAlarmDefinitionService + IHardwareAlarmRuleService
```

## AlarmCenter 简化

报警定义弹窗只保留两个配置区域：

- 报警定义
- 自定义触发规则

不再在设计人员配置弹窗中显示报警抑制、报警搁置、通知路由和事件审计。治理类功能保留在 Core 兼容层，不作为当前配置页面的主能力。

## 实施边界

本阶段做兼容式重构：

1. 新增简洁模型和统一配置服务。
2. 新增 `IAlarmService.Report(AlarmSignal)`。
3. AlarmCenter 配置弹窗改用 `IAlarmConfigService`。
4. 删除配置页面对 `IAlarmGovernanceService`、`IAlarmDefinitionService`、`IHardwareAlarmRuleService` 的直接依赖。
5. 保持现有报警运行、历史、统计页面可编译。

