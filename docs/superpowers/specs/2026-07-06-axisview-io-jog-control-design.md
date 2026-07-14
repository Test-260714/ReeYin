# AxisView IO 方向点动控制设计

## 背景

`ReeYin_V.Hardware.ControlCard` 已有两套相关能力：

- `AxisView` 轴操作面板通过按钮触发 `JogAxis(...)`，支持 X/Y/Z/Z1/Z2 的连续点动与停止。
- 控制卡公共接口 `IControlCard.GetAllInput(out bool[] Status)` 已由 ACS、Googol、ZMotion 等具体控制卡实现，可轮询输入 IO 状态。

本次需求是在 `AxisView` 打开时，通过外部输入 IO 控制 X/Y 平面方向移动。IO3 表示向上，对应 Y 轴正向；IO4 表示向下，对应 Y 轴反向；IO5 表示向左，对应 X 轴反向；IO6 表示向右，对应 X 轴正向。IO 按住时连续移动，松开后停止。

## 范围

包含：

- 在控制卡配置页 `ControlCardConfigView` 的“其他”页增加启用开关和 IO 端口配置。
- 配置保存到 `ControlCardConfigModel`，默认不启用，默认端口为 3/4/5/6。
- 在 `AxisView` 打开期间轮询输入 IO，并把有效 IO 状态转换为 X/Y 连续 Jog。
- `AxisView` 关闭、禁用、IO 读取失败、控制卡复位中或多方向同时触发时，停止当前 IO Jog。
- 添加结构性测试，覆盖配置、映射、生命周期和安全停止逻辑。

不包含：

- 不做后台全局 IO Jog 服务；离开 `AxisView` 后不继续响应 IO。
- 不改变 Z/Z1/Z2 的 IO 控制逻辑。
- 不修改各控制卡底层 `GetAllInput()` 的硬件读取语义。
- 不改变鼠标按钮点动现有行为。

## 推荐架构

采用独立的小状态机类，由 `AxisViewModel` 持有并随窗口生命周期启动/停止。

建议新增：

- `AxisViewIoJogSettings` 或直接在 `ControlCardConfigModel` 增加配置属性。
- `AxisViewIoJogDirection`：描述上、下、左、右四个逻辑方向。
- `AxisViewIoJogController`：负责解析输入 IO、处理边沿变化、调用 `JogAxis` 开始/停止连续运动。

`AxisViewModel` 仍负责页面生命周期和 UI 定时器；IO Jog 控制器只处理“当前输入快照 -> 是否需要启动/停止某个轴 Jog”的纯逻辑，避免把 `AxisViewModel` 再做大。

## 配置模型

在 `ControlCardConfigModel` 增加以下可序列化属性：

- `IsAxisViewIoJogEnabled`：默认 `false`。
- `AxisViewIoJogUpInputPort`：默认 `3`。
- `AxisViewIoJogDownInputPort`：默认 `4`。
- `AxisViewIoJogLeftInputPort`：默认 `5`。
- `AxisViewIoJogRightInputPort`：默认 `6`。

端口配置使用输入 IO 的数组索引，保持与现有 `GetAllInput()` / `IOManagerViewModel` 的端口定义一致。配置页保存时写入 `ConfigKey.ControlCard`，不额外引入新的配置文件。

## UI 设计

在 `ControlCardConfigView` 的“其他”页中，保留原有“启动设置”，新增一个“AxisView IO方向控制”区域：

- 复选框：启用 AxisView IO 方向控制。
- 四个数字输入：
  - 向上输入 IO：默认 3。
  - 向下输入 IO：默认 4。
  - 向左输入 IO：默认 5。
  - 向右输入 IO：默认 6。
- 说明文字：仅在运动轴操作面板打开时生效；按住 IO 连续移动，松开停止；多个方向同时触发会停止运动。

保存按钮沿用现有控制卡配置保存流程。

## 运行流程

`AxisViewModel` 初始化时创建 `AxisViewIoJogController`，并在现有 100ms 定时器中调用：

1. 刷新位置和速度保持现有逻辑。
2. 如果 `IsAxisViewIoJogEnabled == false`，调用控制器 `StopActiveJog()` 并跳过 IO 处理。
3. 如果控制卡正在复位、未就绪或 X/Y 未配置，停止当前 IO Jog。
4. 调用 `ControlCard.GetAllInput(out inputs)`。
5. 控制器读取配置端口对应的输入状态。
6. 如果没有方向触发，停止当前 IO Jog。
7. 如果只有一个方向触发：
   - 当前没有 IO Jog：先校验对应轴配置、复位状态、Jog 限位，然后调用 `JogAxis(axis, direction, CurSpeedType, true)`。
   - 当前方向相同：保持运行，不重复下发 Jog。
   - 当前方向不同：先停止上一次方向，再启动新方向。
8. 如果多个方向同时触发：停止当前 IO Jog，不启动新方向，等待恢复为单方向输入。

`AxisView` 关闭时通过现有 `"关闭"` 命令调用控制器 `Dispose()` 或 `StopActiveJog()`，确保 IO 按住但窗口关闭时不会继续运动。

## 方向映射

默认映射如下：

| 逻辑方向 | 默认输入 IO | 轴 | 方向 |
| --- | ---: | --- | --- |
| 上 | 3 | Y | 正向 |
| 下 | 4 | Y | 反向 |
| 左 | 5 | X | 反向 |
| 右 | 6 | X | 正向 |

连续 Jog 使用 `ModelParam.CurSpeedType`。即使 `ModelParam.IsInching == true`，IO 方向控制也按连续 Jog 处理，因为需求明确为“按住连续移动，松开停止”。

## 错误处理与安全策略

- IO 读取失败：停止当前 IO Jog，并记录日志。
- 配置端口越界：忽略该方向；如果因此没有唯一有效方向，则停止。
- 多方向同时触发：停止，不选择优先级，避免不确定运动。
- X/Y 未配置：不启动对应方向。
- 控制卡复位中：不启动，并停止当前 IO Jog。
- 限位校验失败：不启动，并可复用 `ValidateJogLimitCondition(...)` 的提示日志或页面提示策略。
- 停止调用使用 `ControlCard.JogAxis(activeAxis, activeDirection, ModelParam.CurSpeedType, false)`，与现有鼠标连续 Jog 停止路径保持一致。

## 测试策略

沿用现有 `ReeYin_V.Hardware.ControlCard.ACS.Tests` 的结构性测试风格：

- 验证 `ControlCardConfigModel` 暴露 IO Jog 启用开关和四个默认端口。
- 验证 `ControlCardConfigView.xaml` 的“其他”页绑定启用开关和 IO 端口。
- 验证 `AxisViewModel` 在关闭时停止 IO Jog。
- 验证 IO3/4/5/6 映射到 Y+/Y-/X-/X+。
- 验证多方向同时触发时走停止策略。
- 验证 IO Jog 使用 `CurSpeedType` 的连续 Jog，而不是步长点动。

构建验证：

- `dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj`
- `dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

## 风险

- 不同控制卡的输入 IO 有可能存在电平取反差异；本设计沿用现有 `GetAllInput()` 的布尔语义，不在本层做反相配置。
- `AxisViewModel` 现有定时器同时刷新位置、速度和 IO；若硬件 IO 读取阻塞，页面刷新也会受影响。当前设计延续现有模式，必要时后续可拆为独立轻量定时器。
- IO 按住时关闭窗口依赖关闭命令触发停止；实现时需要确保 `Closed` 事件和异常路径都调用停止。

## 验收标准

- 控制卡配置页“其他”页能启用/禁用 AxisView IO 方向控制，并配置上/下/左/右输入 IO。
- 默认配置为未启用；启用后默认 IO3/4/5/6 对应 Y+/Y-/X-/X+。
- 仅打开 `AxisView` 时响应 IO；关闭 `AxisView` 后 IO 不再触发移动。
- 按住单个方向 IO 时对应轴连续运动，松开后停止。
- 多方向 IO 同时触发时停止运动，不发生斜向或随机方向移动。
- 配置保存后重启软件仍保留启用状态和 IO 端口。
- 相关项目构建通过。
