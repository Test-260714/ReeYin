# 构建与测试地图

本地图提供验证入口，不替代按实际调用链选择消费者和场景。风险与最低门禁见 [统一贡献流程](../../CONTRIBUTING.md#3-风险分类与快速通道)，影响调查见 [变更影响地图](change-impact-map.md)。

## 证据状态

- **已验证**：命令在最终修改后于当前环境运行并保留了新鲜证据。
- **静态核对**：项目/解决方案路径和引用已从当前仓库确认，但命令未在本次执行。
- **未验证**：缺少依赖、许可证、环境或权限；必须说明影响，不能写成通过。

本文件中的初始命令均为“静态核对”，除非交付记录另附本次运行证据。

## 环境入口

| 项目 | 当前观察 | 状态 |
| --- | --- | --- |
| 解决方案 | `ReeYin.sln`，包含 C#、WPF 和 Native C++ 项目 | 静态核对 |
| 常用配置 | Windows、`.NET 8`/WPF；Native 和厂商 SDK 项目可能需要对应架构、许可证和本地运行库 | 静态核对 |
| 最小原则 | 先构建修改项目，再运行直接测试，随后按影响地图构建消费者；除非影响跨全仓库，否则不从全解决方案开始 | 规则要求 |

## 项目族入口

| 修改区域 | 最小构建候选 | 直接测试/场景 | 消费者扩展与特殊前提 | 状态 |
| --- | --- | --- | --- | --- |
| `Core/<project>` | `dotnet build <project.csproj> --no-restore` | 定位同名/相关测试；公共契约做兼容场景 | Application、Shell、Tools、UI；数据库/缓存另做数据场景 | 静态核对 |
| `Application/<project>` | `dotnet build <project.csproj> --no-restore` | 对应服务/UI/报警场景 | `Shell/ReeYin_V.Shell.csproj` 及实际调用者；权限/报警按 R3 | 静态核对 |
| `Shell/` | `dotnet build Shell/ReeYin_V.Shell.csproj --no-restore` | 启动、模块发现、异常和退出场景 | 多个厂商 SDK、图表、相机和 Halcon 运行库 | 静态核对 |
| `Hardware/ControlCard` | `dotnet build Hardware/ControlCard/<project>/<project>.csproj --no-restore` | `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`（适用时） | 优先模拟；真实设备另行授权且由人类执行 | 静态核对 |
| `Tools/Hardware/HardwareTool.Motion` | `dotnet build Tools/Hardware/HardwareTool.Motion/HardwareTool.Motion.csproj --no-restore` | `Tools/Hardware/HardwareTool.Motion.Tests/HardwareTool.Motion.Tests.csproj` | 控制卡抽象、图执行和方案兼容；不得连接真机 | 静态核对 |
| `CustomizedDemand/Custom.WaferFlatnessMeasure` | `dotnet build CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj --no-restore` | `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Custom.WaferFlatnessMeasure.Tests.csproj` | 控制卡、PLC、传感器和图表/Native 依赖 | 静态核对 |
| `GemeralUI/ReeYin.ChartShow` | `dotnet build GemeralUI/ReeYin.ChartShow/ReeYin.ChartShow.csproj --no-restore` | 页面打开、Binding、图表交互、DPI/主题 | `ReeYin_V.Share`、`ReeYin_V.UI`、VTK/图表运行库 | 静态核对 |
| `Semiconductor/Custom.Semiconductor.Test` | `dotnet build Semiconductor/Custom.Semiconductor.Test/Custom.Semiconductor.Test.csproj --no-restore` | 脚本编译、非法输入和领域场景 | Roslyn scripting、`ReeYin_V.Share` | 静态核对 |
| Native/Algorithm | 使用对应 `.vcxproj` 或批准的解决方案配置 | 精度、确定性、性能、ABI 和内存场景 | Visual Studio C++ 工具链、Native SDK、目标架构 | 未验证 |
| `Resource/`、`thirdparty/`、`packages/`、`OutputExe/` | 无统一独立构建 | 构建实际消费者并验证复制/加载/回滚 | 来源、许可证、完整性、架构和生成来源 | 静态核对 |

## 记录要求

实际任务必须把候选占位符替换为精确项目路径，并记录命令、工作目录、时间、环境、退出码、错误/失败/跳过计数、关键输出及证明边界。命令无法执行时标记“未验证”，说明原因、影响、替代验证和后续负责人。历史计划、修改前结果和本地图的“静态核对”都不构成完成证据。
