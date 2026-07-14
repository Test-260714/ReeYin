# Googol 控制卡启动复位与速度监控设计

## 背景

`ReeYin_V.Hardware.ControlCard.ACS` 已经通过公共控制卡链路支持软件启动后自动复位；`ReeYin_V.Hardware.ControlCard` 的 `AxisView` 页面也已经通过统一的速度刷新接口显示各轴当前速度。本次变更要让 `ReeYin_V.Hardware.ControlCard.Googol` 在同一公共链路下具备等价能力，避免在初始化页、轴操作面板和控制卡配置页中出现供应商差异。

## 范围

- 保持公共启动复位入口不变：`ControlCardConfigModel.ExecuteStartupResetAsync()` 继续选择已初始化且已连接的控制卡，并调用 `ControlCardBase.GoHome()`。
- 在 Googol 控制卡中完善 `DoGoHome(out string message)`，使软件启动自动复位能够稳定调用固高回零流程。
- 保持并补强 Googol 当前速度监控：通过 `GetAllSpeedInfos()` 读取编码器速度，更新 `SingleAxisParam.CurSpeed` 和 `ControlCardBase.CurSpeed`。
- 增加结构性测试，确认 Googol 与公共 AxisView/启动复位接口对齐。

不在本次范围内：

- 不改 ACS 已有功能。
- 不重构 `AxisView` 页面布局。
- 不新增固高专属配置页面功能。
- 不改变用户配置中的“软件启动自动复位”默认值。

## 推荐架构

沿用现有抽象层：

- `ControlCardConfigModel` 负责启动阶段的自动复位开关、超时提示与可用控制卡选择。
- `ControlCardBase.GoHome()` 负责统一发布复位遮罩、停止轴、设置 `IsAxisHoming` / `IsAxisHomed` 状态，并委托供应商实现。
- `GoogolControlCard.DoGoHome()` 负责固高卡的具体回零步骤。
- `AxisViewModel` 继续调用 `ControlCard.GetAllSpeedInfos()`，不关心底层供应商。
- `GoogolGTMotion.GetEncVel()` 继续封装 `GTN_GetEncVel`，作为固高速度读取的底层 API。

这样可以让启动复位和速度监控都走公共接口，减少基础项目对 Googol 项目的反向依赖。

## Googol 回零流程

`GoogolControlCard.DoGoHome(out message)` 应按以下顺序执行：

1. 检查 `IsConnected`；未连接时返回失败消息。
2. 读取 `Config.AllAxis` 中 `IsUsing == true` 的轴；没有启用轴时返回失败消息。
3. 对参与复位的轴先写 `IsResetCompleted = false`。
4. 清除报警、检查/补使能、停止轴运动。
5. 先下发 `Top` / `High` 优先级轴的回零命令，等待这些轴回零完成。
6. 再下发普通优先级轴的回零命令，等待这些轴回零完成。
7. 回零完成后按现有固高流程清零/绑定规划器位置、恢复默认速度。
8. 成功时写 `IsNeedReset = false`，并把参与复位的轴写 `IsResetCompleted = true`；`IsAxisHomed` 由 `ControlCardBase.GoHome()` 根据返回值统一设置。
9. 失败时返回带轴号或步骤的明确错误消息。
10. `finally` 中保证 `IsReseting = false`，避免异常后页面长期禁止操作。

现有 `ResetRobot()` 可作为历史参考，但本次优先完善 `DoGoHome()`，因为启动自动复位和 AxisView 的“复位”按钮都通过 `ControlCardBase.GoHome()` 进入供应商实现。

## Googol 速度监控流程

保留现有统一接口：

- `GoogolGTMotion.GetEncVel(short axisId, ref double[] encVel, short core = 2)` 负责参数校验并调用 `GTN_GetEncVel`。
- `GoogolControlCard.GetAllSpeedInfos(short core = 2)`：
  - 未连接返回 `false`。
  - 调用 `EnsureSpeedBuffers()`，确保稀疏物理轴号也不会越界。
  - 按最大物理轴号创建临时速度数组。
  - 调用 `Motion.GetEncVel(1, ref tmpAllSpeedInfos, core)` 批量读取编码器速度。
  - 使用各轴 `PulseEquivalent` 转换到用户单位。
  - 更新 `axisConfig.CurSpeed` 和 `CurSpeed[AxisNo - 1]`。
- `GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)` 继续复制当前配置轴顺序的速度值。

`AxisViewModel` 无需供应商分支，继续从 `AxisViewAxisMatcher.BuildSpeedSnapshot()` 映射到 X/Y/Z/Z1/Z2 显示顺序。

## 错误处理

- 未连接、无启用轴、清报警失败、使能失败、停止失败、回零命令下发失败、等待回零超时都返回 `false` 和可读消息。
- 单轴失败时消息包含轴枚举值，便于用户定位。
- 异常路径记录控制台日志，同时通过 `message` 返回给 `ControlCardBase.GoHome()` 和初始化页。
- 任何失败路径都恢复 `IsReseting`，避免软件重启复位失败后 AxisView 不能继续操作。

## 测试策略

优先使用现有 `ReeYin_V.Hardware.ControlCard.ACS.Tests` 的结构性测试风格，新增/补强以下检查：

- 公共启动复位仍通过 `ControlCardConfigModel.ExecuteStartupResetAsync()` 调 `GoHome(out var message)`。
- Googol `DoGoHome()` 包含连接检查、启用轴过滤、优先级分组、`IsResetCompleted` 更新、`IsNeedReset = false` 和 `finally` 状态清理。
- Googol 速度监控继续通过 `Motion.GetEncVel`、`PulseEquivalent`、`axisConfig.CurSpeed` 和 `CurSpeed[axisIndex]` 更新。
- `AxisView` 仍绑定 `ModelParam.CurSpeedInfos[0..4]`，供应商实现变化不影响页面。

验证命令以能编译触达项目为准：

- `dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\ReeYin_V.Hardware.ControlCard.Googol.csproj`
- `dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

## 风险

- 固高回零真实硬件行为无法仅靠结构性测试完全验证，最终仍需在设备上执行一次启动自动复位和 AxisView 速度显示验证。
- `GTN_GetEncVel` 返回单位依赖固高驱动配置，本设计沿用现有 `PulseEquivalent` 换算逻辑，不额外改变单位语义。
- 如果现场只希望复位高优先级轴，普通优先级轴纳入自动复位可能改变行为；当前需求是对齐 ACS 的软件启动复位，因此默认复位所有启用轴。

## 验收标准

- 勾选控制卡配置页“软件启动自动复位”后，软件启动并完成 Googol 控制卡初始化时会自动调用固高回零。
- 回零成功后，参与复位的启用轴 `IsResetCompleted` 为 `true`，控制卡 `IsNeedReset` 为 `false`，`ControlCardBase.GoHome()` 将 `IsAxisHomed` 置为 `true`。
- 回零失败时初始化页能显示明确错误，且 `IsReseting` 不会卡死。
- 打开 AxisView 后，Googol 控制卡的 X/Y/Z/Z1/Z2 当前速度按实际配置轴显示，未配置轴显示 0。
- 相关项目编译通过。
