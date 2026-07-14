# 报警 P0 修复测试方案

## 1. 测试目标

验证人工清除安全边界、恢复清除、报警定义无损编辑、确认重置策略和重复确认幂等性，同时回归报警历史、统计、导出、规则和异步持久化能力。

## 2. 测试环境

- 操作系统：当前 Windows 开发环境。
- 运行时：项目锁定的 .NET 8 SDK。
- 数据库：测试进程创建的临时 SQLite 数据库。
- 测试入口：`Scratch/AlarmCenterFunctionalTests/AlarmCenterFunctionalTests.csproj`。
- 网络：不需要外部网络。

## 3. 前置条件

- NuGet 资产已存在，可使用 `--no-restore` 构建和运行。
- 测试数据库必须包含报警记录、定义、抑制、搁置、通知路由和审计表。
- 每个生命周期测试使用唯一 `Code + Source + Location`，避免用例间污染。

## 4. 自动化测试用例

| 编号 | 场景 | 操作 | 预期结果 |
| --- | --- | --- | --- |
| AC-P0-001 | 禁止人工清除 | 创建 `AllowManualClear=false` 活动报警后以 `Manual` 清除 | 返回 `ManualClearNotAllowed`，报警仍活动，无 Cleared 审计 |
| AC-P0-002 | 恢复清除 | 对 AC-P0-001 报警以 `Recovery` 清除 | 返回 `Succeeded`，报警关闭，产生一次 Cleared 审计 |
| AC-P0-003 | 允许人工清除 | 创建 `AllowManualClear=true` 报警并人工清除 | 返回 `Succeeded`，报警关闭 |
| AC-P0-004 | 目标不存在 | 使用不存在的活动 ID 清除 | 返回 `NotFound`，不发布状态变化 |
| AC-P0-005 | Core 定义往返 | 完整定义执行 `ToModel -> ToInfo` | 所有字段值一致，扩展字典为深复制 |
| AC-P0-006 | AlarmCenter 隐藏字段 | 仅修改名称后执行 Item 往返 | ID、默认来源/位置、策略、模板和时间不变 |
| AC-P0-007 | Never 策略 | 确认后同键重复触发 | 仍为已确认 |
| AC-P0-008 | OnSeverityIncrease 策略 | 确认后同级重复，再升级重复 | 同级保持确认；升级后变为待确认 |
| AC-P0-009 | OnEveryRepeat 策略 | 确认后同键重复触发 | 变为待确认 |
| AC-P0-010 | 重复确认 | 同一报警连续确认两次 | 第二次返回 `AlreadyAcknowledged`，事件和审计不增加 |
| AC-P0-011 | 全量回归 | 执行现有功能测试集合 | 原有功能测试全部通过 |

## 5. 构建与执行命令

```powershell
dotnet run --project Scratch/AlarmCenterFunctionalTests/AlarmCenterFunctionalTests.csproj --no-restore
dotnet build Core/ReeYin-V.Core/ReeYin_V.Core.csproj --no-restore
dotnet build Application/ReeYin.AlarmCenter/ReeYin.AlarmCenter.csproj --no-restore
```

## 6. 手工冒烟测试

1. 打开报警工作台并触发禁止人工清除的安全报警，确认清除按钮不可用或操作返回明确拒绝提示。
2. 模拟对应硬件状态恢复，确认报警自动关闭并进入历史记录。
3. 创建带默认来源、位置、防抖、节流和扩展模板的定义；只修改名称并保存，再次加载后核对高级字段未改变。
4. 触发需要确认的报警并确认；同级重复触发后仍保持确认，提升到更严重等级后重新进入待确认。
5. 连续点击确认时，只产生一条确认实时事件和一条确认审计。

手工冒烟依赖可运行主程序和硬件/模拟事件环境；无法执行的项目在测试报告中标记“未执行”，不能视为通过。

## 7. 通过标准

- AC-P0-001 至 AC-P0-011 全部通过。
- Core、AlarmCenter 和功能测试工程构建无错误。
- 新增行为没有破坏现有 Reporter 恢复清除。
- 测试报告中的结论均有本轮实际命令输出支撑。
