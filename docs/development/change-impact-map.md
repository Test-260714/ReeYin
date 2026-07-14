# 变更影响地图

本地图帮助贡献者从修改位置找到必须调查的契约、消费者、副作用和验证入口。它是调查起点，不是完整调用图，也不证明未列出的消费者不存在。风险等级以 [统一贡献流程](../../CONTRIBUTING.md#3-风险分类与快速通道) 为唯一权威定义，构建入口见 [构建与测试地图](build-and-test-map.md)。

## 使用方法

1. 从修改文件向上查找最近的 `AGENTS.md`。
2. 在下表定位目录职责、必查契约和典型消费者。
3. 通过 `.csproj`、代码 usage、模块注册、反射/动态加载、XAML 资源、Recipe/Output、缓存和数据库路径核对实际影响。
4. 判定风险和必读规范，再从构建与测试地图选择最小验证并沿消费者扩展。
5. 发现未知消费者、跨域副作用或风险扩大时停止快速通道并重新分级。

## 一级目录影响

| 区域 | 职责与必查契约 | 典型消费者/副作用 | 升级提示 | 就近规则 |
| --- | --- | --- | --- | --- |
| `Application/` | 应用服务、配置、登录/权限、报警、状态、Recipe、项目和节点编排；检查公共服务、导航、后台任务和状态传播 | `Shell`、UI、Tools、客户模块；报警和权限可能跨应用传播 | 公共服务/配置至少 R2；报警或权限语义至少 R3 | `Application/AGENTS.md` |
| `Core/` | 公共基础设施、数据库、缓存、事件、节点模型、共享 UI；检查 API、序列化、资源键和全局生命周期 | 几乎所有托管项目 | 公共/数据/全局行为至少 R2 | `Core/AGENTS.md` |
| `Shell/` | 组合根、模块发现、启动/退出、运行时复制和全局异常 | 整个运行时、发布产物、插件加载 | 加载/启动/停止/发布通常至少 R2，安全发布为 R3 | `Shell/AGENTS.md` |
| `Hardware/` | 相机、控制卡、PLC、传感器和设备抽象；检查单位、超时、停止、重连和资源所有权 | Tools、Application、CustomizedDemand | 设备命令/安全语义至少 R3；不可逆真实操作为 R4 | `Hardware/AGENTS.md` |
| `Tools/` | 节点模块、执行委托、Recipe/Output、DynamicView 和节点缓存 | 图执行、方案、全局/下游缓存、硬件服务 | 生命周期/持久化至少 R2；硬件调用按 R3/R4 | `Tools/AGENTS.md` |
| `Algorithm/` | 托管/Native 算法、精度、ABI 和非托管内存 | Tools、CustomizedDemand、发布复制 | ABI/精度/所有权至少 R2；安全决策输入按更高等级 | `Algorithm/AGENTS.md` |
| `CustomUI/` | 项目/客户组合 UI、Prism 导航和主题扩展 | Application、Tools 页面、共享资源 | 共享资源/导航契约至少 R2 | `CustomUI/AGENTS.md` |
| `GemeralUI/` | 当前观察为 `ReeYin.ChartShow` 图表 UI，引用 `Application/ReeYin_V.Share` 和 `Core/ReeYin_V.UI`；不是共享 UI 权威 | 图表页面及其调用者；第三方图表和 VTK 运行时 | 公共控件/资源契约至少 R2 | `GemeralUI/AGENTS.md` |
| `CustomizedDemand/` | 客户模块，组合 UI、Core、Tools、Hardware、算法和厂商 SDK | 对应客户交付、方案和设备流程 | 跨域、客户数据、硬件或持久化按最高等级 | `CustomizedDemand/AGENTS.md` |
| `Semiconductor/` | 半导体脚本/测试和领域流程；当前项目引用 `Application/ReeYin_V.Share` | 领域脚本、模型和相关客户模块 | 工艺/数据契约至少 R2；设备/安全语义至少 R3 | `Semiconductor/AGENTS.md` |
| `Resource/` | 配置、图标及共享静态资源 | 打包、UI、运行配置 | 资源标识/配置/复制路径变化按消费者升级 | `Resource/AGENTS.md` |
| `thirdparty/` | 厂商 SDK、Native 库和第三方二进制 | 构建、运行时复制、设备/算法项目 | 新增/替换依赖至少 R2；设备/发布影响可为 R3 | `thirdparty/AGENTS.md` |
| `packages/` | 本地 NuGet/工具包缓存或受控依赖副本 | 多项目恢复和编译 | 版本/来源/许可证/运行时变化至少 R2 | `packages/AGENTS.md` |
| `OutputExe/` | 构建输出、模块复制和发布产物 | Shell 启动、模块发现和交付 | 发布配置为 R3；生成物不得作为源文件修补 | `OutputExe/AGENTS.md` |
| `docs/` | 规范、设计、计划和验证记录 | 开发流程和审计 | 改变门禁至少 R2；纯说明可 R0-Lite | 根规则 |
| `scripts/` | 构建、验证和治理自动化 | 开发/CI 流程、文件和外部进程 | 改变构建/验证门禁按实际影响，治理门禁至少 R2 | 根规则 |
| `.agents/`、`.codex/` | 本地代理/工具配置 | AI 和自动化行为 | 按权限、外部进程和规则影响升级 | 根规则 |

## 横切契约搜索

| 变更 | 必查路径 |
| --- | --- |
| 公共 API | `ProjectReference`、类型/成员 usage、反射、脚本和外部消费者风险 |
| Recipe/Output | 收集、保存、加载、下发、执行时机、`ParamName`、全局/下游缓存和旧方案 |
| 序列化/数据库 | 类型/成员标识、默认值、旧数据、迁移、事务失败、备份和恢复 |
| 模块加载 | Prism 注册、程序集扫描、输出复制、模块身份、启动/退出和卸载 |
| XAML/资源 | `StaticResource`/`DynamicResource`、资源 URI、覆盖顺序、主题和消费者页面 |
| 硬件 | 抽象与实现、调用者、单位、超时、取消、停止、重连、释放和模拟/真机隔离 |
| Native/第三方 | API/ABI、架构、许可证、来源、完整性、运行时复制和回滚版本 |

无法定位全部消费者时，必须记录搜索范围、未知消费者风险和后续动作，不能把本表或仓库内构建扩大解释为完整兼容证明。
