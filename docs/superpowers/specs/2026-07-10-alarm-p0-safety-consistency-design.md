# 报警 P0 安全与一致性修复设计

## 背景

`ReeYin_V.Core` 已提供报警生命周期、定义配置、硬件规则、软硬件上报和异步持久化；`ReeYin.AlarmCenter` 已提供实时报警、历史、统计及定义/硬件规则编辑。现有实现存在三类高风险问题：禁止手动清除仅由 UI 约束；报警定义经公共配置模型编辑后会丢失高级字段；重复触发和重复确认会造成确认状态及审计噪声。

本轮只修复上述 P0 闭环，不实现治理 UI、外部通知通道、服务拆分或完整工业报警状态机。

## 目标

1. 在 Core 服务层区分人工清除、状态恢复清除和系统清除，并强制执行 `AllowManualClear`。
2. 保持现有公开接口可编译，新增结构化操作结果供新代码使用。
3. 保证报警定义在 Core 公共模型、配置服务和 AlarmCenter 编辑模型之间无损往返。
4. 让重复触发的确认重置策略可配置，默认仅在严重级别提升时重置。
5. 让重复确认具备幂等性，不重复发布事件和写审计。
6. 通过 SQLite 功能测试覆盖修复行为，并输出测试方案和实测报告。

## 非目标

- 不增加抑制、搁置和通知路由管理页面。
- 不实现邮件、企业微信、Webhook 等外部通知发送。
- 不拆分 `AlarmService` 或 `AlarmWorkbenchShellViewModel`。
- 不改变活动报警去重键 `Code + Source + Location`。
- 不改变当前异步持久化队列架构。

## 方案选择

采用兼容扩展方案：新增明确语义的类型和方法，现有 `bool`/`Task` 方法保留并委托给新实现。与直接修改现有方法返回类型相比，该方案不会迫使所有模块同步迁移；与只在 UI 增加判断相比，它能在 Core 建立真正的安全边界。

## 清除策略

新增 `AlarmClearOrigin`：

- `Manual`：操作员或 AlarmCenter 发起，必须检查 `AllowManualClear`。
- `Recovery`：硬件状态或软件状态恢复发起，可以清除禁止人工清除的报警。
- `System`：系统维护或受控复位路径，保留给显式系统调用。

新增结构化结果：

```csharp
public sealed class AlarmOperationResult
{
    public bool Success { get; init; }
    public AlarmOperationStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public string AlarmId { get; init; } = string.Empty;
}
```

状态至少包含 `Succeeded`、`NotFound`、`ManualClearNotAllowed`、`AlreadyAcknowledged` 和 `InvalidRequest`。

`IAlarmService` 增加按活动 ID 和按报警键执行清除的结构化方法。现有 `ClearAsync` 固定使用 `Manual`；现有 `ClearAlarm` 保持兼容，但内部委托统一清除实现。硬件监控、硬件 Reporter 和软件 Reporter 的恢复路径显式传入 `Recovery`，不通过人工清除入口绕过策略。

所有清除成功路径继续生成 `Cleared` 实时事件和审计；被拒绝或未找到不修改状态，不发布 `Cleared` 事件。人工清除被拒绝可返回结果给 UI，UI 显示具体原因。

## 配置无损往返

公共 `AlarmDefinition` 增加并完整携带：

- `Id`
- `DefaultSource`
- `DefaultLocation`
- `AutoClearOnRecovery`
- `DebounceMilliseconds`
- `ThrottleSeconds`
- `AcknowledgeResetMode`
- `ExtraTemplate`
- `CreatedAt`
- `UpdatedAt`

`AlarmConfigService.ToModel` 和 `ToInfo` 必须逐字段映射并复制字典，不能共享可变引用。已有定义保存时保留 `Id`、`CreatedAt` 和高级配置；只有真正的新定义才生成 ID、创建时间和默认值。

AlarmCenter 的 `AlarmDefinitionItem` 同步携带这些字段。当前 UI 暂不要求暴露所有高级字段，但加载、编辑、保存过程必须保留它们，避免用户修改名称或级别时覆盖隐藏配置。

## 确认重置和幂等

新增 `AlarmAcknowledgeResetMode`：

- `Never`：同一活动生命周期内重复触发不撤销确认。
- `OnSeverityIncrease`：仅新级别高于重复触发前的级别时撤销确认。
- `OnEveryRepeat`：保持旧行为，每次重复触发都撤销确认。

新建定义默认使用 `OnSeverityIncrease`。持久化实体增加枚举值字段，由 SqlSugar CodeFirst 完成列迁移。默认系统定义显式设置该策略，避免依赖 CLR 默认值。

重复触发时先保存原严重级别，再更新报警内容，最后按定义随报警请求传入的策略决定是否重置确认。该策略需要进入 `AlarmRaiseRequest`、`AlarmInfo`、`AlarmRecordEntity` 和实时/历史映射，确保活动状态恢复及持久化一致。

确认操作改成幂等：报警已经确认时返回 `AlreadyAcknowledged`，不更新时间、不追加 `Confirmed` 实时事件、不写第二条确认审计。旧 `ConfirmAlarm` 返回 `true` 仅表示目标报警存在且最终处于已确认状态，以维持调用方兼容；新结构化方法用于区分首次确认和重复确认。

## 数据流

```text
AlarmCenter 手工清除 -> ClearOperation(Manual) -> 策略校验 -> 清除/拒绝
硬件或软件恢复     -> ClearOperation(Recovery) -> 清除

定义加载 -> AlarmDefinitionInfo -> AlarmDefinition -> AlarmDefinitionItem
定义保存 <- AlarmDefinitionInfo <- AlarmDefinition <- AlarmDefinitionItem

重复上报 -> 保存旧级别 -> 更新报警 -> 应用 AcknowledgeResetMode
重复确认 -> 检查 IsConfirmed -> 返回 AlreadyAcknowledged，不产生副作用
```

## 错误处理与兼容性

- 无效 ID、空用户和目标不存在通过 `AlarmOperationResult` 返回，不抛出业务异常。
- 数据库异步持久化失败仍沿用现有重试和日志机制，本轮不改变一致性模型。
- 新增数据库列必须提供可安全迁移的默认值。
- Reporter 和监控服务内部调用全部改为显式清除来源，避免依赖兼容入口的默认含义。
- UI 继续支持旧命令绑定；仅增强失败提示，不改变导航和 Prism 生命周期。

## 测试设计

自动化测试继续使用 `Scratch/AlarmCenterFunctionalTests` 的控制台测试脚手架和临时 SQLite 数据库，遵循测试先行：每个修复先加入会因旧行为失败的断言，再实现最小改动。

核心用例：

1. `AllowManualClear=false` 时人工清除返回 `ManualClearNotAllowed`，报警仍活动。
2. 同一报警通过 `Recovery` 清除成功并写入 `Cleared` 审计。
3. `AllowManualClear=true` 时人工清除成功。
4. 不存在的活动 ID 返回 `NotFound` 且无事件副作用。
5. 报警定义完整字段经过 `ToModel -> ToInfo` 后值保持一致且字典为深复制。
6. AlarmCenter 编辑一个展示字段后，隐藏高级字段保持不变。
7. `Never` 模式下重复触发不撤销确认。
8. `OnSeverityIncrease` 模式下同级重复不撤销确认，升级时撤销确认。
9. `OnEveryRepeat` 保持旧行为。
10. 已确认报警再次确认返回 `AlreadyAcknowledged`，实时事件和审计数量不增加。
11. 原有报警定义、硬件规则、历史、统计、导出和持久化功能测试继续通过。

## 测试文档与报告

实施时创建：

- `docs/testing/alarm-p0-test-plan-2026-07-10.md`：测试范围、环境、前置条件、用例步骤、预期结果和执行命令。
- `docs/testing/alarm-p0-test-report-2026-07-10.md`：实际构建与测试命令、通过/失败统计、失败证据、修复复测结果和遗留风险。

报告只记录实际执行结果；若环境或依赖导致测试无法执行，报告明确标记阻塞项，不把未运行用例写成通过。

## 验收标准

- 禁止人工清除的报警不能通过 AlarmCenter 或新的人工清除 Core API 被清除。
- 状态恢复仍能正常关闭该类报警。
- 已有定义通过 AlarmCenter 编辑保存后不丢失高级字段。
- 默认重复触发仅在严重级别提升时重置确认。
- 重复确认不产生重复事件和审计。
- 新增回归用例通过，原功能测试通过。
- `ReeYin-V.Core`、`ReeYin.AlarmCenter` 及功能测试工程构建成功。
- 测试方案和测试报告内容与实际执行证据一致。
