# 报警 P0 修复测试报告

## 1. 结论

报警 P0 自动化验证通过。功能测试共 22 项，22 项通过、0 项失败；`ReeYin_V.Core` 与 `ReeYin.AlarmCenter` 均构建成功。GUI 手工冒烟未执行，因此不纳入通过结论。

## 2. 变更验证范围

- Core 区分 `Manual`、`Recovery`、`System` 清除来源。
- `AllowManualClear=false` 时 Core 拒绝人工清除，恢复清除仍可关闭报警。
- 清除和确认提供结构化操作结果，旧接口保留兼容。
- 报警定义 ID、默认来源/位置、恢复/防抖/节流策略、确认重置策略、扩展模板和时间元数据无损传递。
- 重复触发支持 `Never`、`OnSeverityIncrease`、`OnEveryRepeat`。
- 重复确认不再产生第二条实时确认事件；结构化接口返回 `AlreadyAcknowledged`。

## 3. TDD 记录

### 3.1 人工清除策略 RED

新增用例首次构建失败，缺少：

- `AlarmClearOrigin`
- `AlarmOperationStatus`
- `AlarmOperationResult`
- `AlarmService.ClearByIdAsync`

实现后该用例通过。

### 3.2 定义往返 RED

新增字段断言首次构建失败，`AlarmDefinition` 缺少 ID、默认来源/位置、防抖、节流、确认重置策略、扩展模板和时间字段。补齐模型及 Core/AlarmCenter 映射后通过。

### 3.3 重复确认 RED

旧实现连续确认后实时事件数量从 2 增加到 3，断言失败。幂等处理完成后第二次确认不再增加实时事件，结构化结果为 `AlreadyAcknowledged`。

## 4. 实际执行结果

### 4.1 功能测试工程构建

```powershell
dotnet build Scratch/AlarmCenterFunctionalTests/AlarmCenterFunctionalTests.csproj --no-restore --verbosity minimal -p:UseSharedCompilation=false
```

结果：成功，0 个错误；存在 1 个既有 `halcondotnet` 引用解析警告。

### 4.2 功能测试执行

```powershell
dotnet run --project Scratch/AlarmCenterFunctionalTests/AlarmCenterFunctionalTests.csproj --no-build
```

结果：

```text
RESULT Passed=22; Failed=0
```

新增/增强覆盖包含：

- 禁止人工清除。
- 恢复清除。
- 不存在 ID。
- 定义隐藏字段往返。
- `Never` 确认策略。
- `OnSeverityIncrease` 同级保持、升级重置。
- `OnEveryRepeat` 每次重置。
- 重复确认幂等及结构化状态。

### 4.3 Core 构建

```powershell
dotnet build Core/ReeYin-V.Core/ReeYin_V.Core.csproj --no-restore --verbosity minimal -p:UseSharedCompilation=false
```

结果：成功，0 个警告，0 个错误。

### 4.4 AlarmCenter 构建

```powershell
dotnet build Application/ReeYin.AlarmCenter/ReeYin.AlarmCenter.csproj --no-restore --verbosity minimal -p:UseSharedCompilation=false
```

结果：成功，1 个警告，0 个错误。警告来自既有 `ReeYin_V.Share` 对 `halcondotnet` 的引用解析，不是本次报警修改引入。

## 5. 调用路径审计

- AlarmCenter 工作台选中项清除：`ClearByIdAsync(..., Manual)`。
- `ReeYin.Status` 旧 UI：`ClearAsync`，其 Core 包装固定为 `Manual`。
- `ClearAlarm` 兼容入口：Core 固定委托 `Manual`。
- `HardwareAlarmReporter`：`ClearByKey(..., Recovery)`。
- `SoftwareAlarmReporter`：`ClearByKey(..., Recovery)`。

未发现 Reporter 恢复路径误用人工清除来源。

## 6. 基线环境问题

首次基线构建发现 `obj-codex`、`obj-codex-task12` 等历史中间目录被 SDK 当作源码，造成 18 个程序集特性重复错误。已在 `Directory.Build.props` 增加 `**/obj-*/**` 默认排除，不删除任何历史目录。修复后基线功能测试为 21/21 通过。

当前 `.git` 目录存在，但 Git 命令返回“不是 Git 仓库”，无法创建隔离 worktree、检查差异或提交变更。

## 7. 未执行项目与遗留风险

- 未启动 WPF 主程序执行 GUI 手工冒烟；测试方案第 6 节的五项手工场景状态为“未执行”。
- 未接入真实硬件状态源验证自动恢复；已通过 Core SQLite 功能测试验证同一路径语义。
- 异步持久化队列仍没有正式停止/flush 生命周期。测试中多个报警快速写入时曾触发测试数据库关闭竞态，测试清理已容错，但生产侧队列治理不属于本轮范围。
- `AcknowledgeOperationAsync` 在检查与兼容确认调用之间存在很小的并发窗口；`ConfirmAlarm` 自身已幂等，因此不会重复发布事件，但极端并发下两个调用的结构化返回状态可能都不是 `AlreadyAcknowledged`。后续可将结构化确认完全收敛到单次锁内状态变更。

## 8. 验收判定

| 项目 | 状态 |
| --- | --- |
| Core 强制人工清除策略 | 通过 |
| 恢复清除不受人工限制 | 通过 |
| 定义高级字段无损传递 | 通过 |
| 默认严重度提升重置确认 | 通过 |
| Never / EveryRepeat 策略 | 通过 |
| 重复确认无重复实时事件 | 通过 |
| 功能测试回归 | 通过，22/22 |
| Core 构建 | 通过 |
| AlarmCenter 构建 | 通过，存在 1 个既有警告 |
| GUI 手工冒烟 | 未执行 |
