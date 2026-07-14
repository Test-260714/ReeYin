# ACS 测试页运动参数设置设计

## 目标
在 `AcsControlCardTestView` 中增加每轴独立的 ACS 运动参数设置能力，支持 Velocity、Acceleration、Deceleration、Kill Deceleration、Jerk 的读取和写入。

## 当前参数位置
ACS 项目目前在以下位置设置部分速度参数：
- `App/InitCard.cs`：初始化时按轴配置写入 `SetVelocity`、`SetAcceleration`、`SetDeceleration`。
- `App/JogMove.cs`：相对移动前按 `SpeedSetting.MaxSpeed` / `SpeedSetting.AccSpeed` 写入 Velocity、Acceleration、Deceleration。
- `App/AxisSetting.cs`：插补前按速度档位或插补配置写入 Velocity、Acceleration、Deceleration。
- `SpeedSetting` 仅包含 `MaxSpeed` 和 `AccSpeed`，当前没有 Kill Deceleration 和 Jerk 的配置入口。

## 方案
新增运行时测试诊断能力，不修改现有配方/轴配置结构：
- 新增 `AcsAxisMotionProfile` 模型，保存单轴的五项 ACS 运动参数和状态消息。
- 在 `AcsControlCard` 的测试诊断 partial 中增加 `TryGetAxisMotionProfile`、`TrySetAxisMotionProfile`，直接调用 ACS API 的 `Get/SetVelocity`、`Get/SetAcceleration`、`Get/SetDeceleration`、`Get/SetKillDeceleration`、`Get/SetJerk`。
- 在 `AcsControlCardTestViewModel` 中增加 `AxisMotionProfiles`、`SelectedMotionProfile` 和刷新/应用命令。
- 在“单轴运动”页签增加运动参数 DataGrid，每轴一行，可编辑五项参数，并提供“刷新”“应用选中轴”“应用全部轴”。

## 验证
用结构验证脚本确认模型、接口、命令、XAML 绑定存在，再构建 ACS 项目确认编译通过。
