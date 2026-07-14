# Tools 领域规则

本文件继承仓库根级 [AGENTS.md](../AGENTS.md)，只能收紧或细化根级门禁，不得放宽安全、验证、评审、授权或例外要求。规则冲突时执行更严格者并停止冲突范围内的修改，交由负责人裁决。

## 适用范围

适用于 `Tools/` 及其全部子目录中的节点工具、执行模块、Model/ViewModel、参数页面、Recipe/Output、DynamicView、节点缓存、硬件/算法组合、测试和文档。更深层 `AGENTS.md` 可以继续收紧本文件。

## 修改前必须阅读

- [开发规范索引](../docs/development/README.md)
- [模块开发与生命周期](../docs/development/module-development.md)
- [架构与依赖边界](../docs/development/architecture.md)
- [编码标准](../docs/development/coding-standards.md)
- [测试与验证](../docs/development/testing-and-verification.md)
- [评审与交付](../docs/development/review-and-delivery.md)
- AI 参与时还必须阅读 [AI 开发规范](../docs/development/ai-development.md)
- 调用硬件、处理生产数据、网络/外部进程或第三方依赖时还必须阅读 [安全与信息安全](../docs/development/safety-and-security.md)

## Model、ViewModel 与持久化契约

- 修改前必须定位主 Model、主 ViewModel、模块注册类、所有 `INavigationAware` 二级页面、真实执行入口、Recipe/Output、自定义窗口、缓存、事件和释放路径，并判断是现代模式还是 legacy/手动模式。legacy 只作为兼容基线，不授权机械迁移或顺手重构。
- 新增模块的主 Model 使用 `ModelParamBase`；主参数 ViewModel 使用 `DialogViewModelBase`，参与节点参数对话框时实现 `IViewModuleParam`。强类型 `ModelParam` 以 `base.ModelParam` 为唯一后备存储，`InitParam` 通过 `InitModelParam<TModel>()` 接入现有框架初始化调用链。
- `LoadKeyParam` 默认先调用并检查 `base.LoadKeyParam()`，再加载模块专用状态；失败必须阻止确认和执行。`OnceInit` 先执行基类并保留 `false` 检查；本轮无危险基础注册自身均成功后可以设置 `IsOnceInit` 幂等标志，即使缓存、Recipe 或参数传递尚未证明，但该标志不得作为完整 ready，真实能力必须继续失败关闭；基础注册自身失败时不得设置。
- 当前基类 `OnceInit()` 会尝试节点缓存和 Recipe 同步，但其返回值不传播这些步骤的组合结果。模块不得把基类返回 `true` 当作缓存或 Recipe 已就绪证据；必须用实际可用查询/状态独立核验。无法可靠核验时停止依赖该保证的初始化，并把框架契约改进作为单独 R2 或更高变更评审。
- 当前 `DialogViewModelBase.InitModelParam<TModel>()` 进入 `InitializeModelParam` 后会调用派生 `OnceInit()`，但忽略其返回值；即使返回 `false`，仍继续执行 `LoadSpecificConfig`、`InitOutputParamResource`、`TransferParam` 和调试状态设置。有 Dispatcher 时，`TransferParam()` 仅以 `BeginInvoke` 排队 `TransferParamCore()` 后立即返回 `true`，既不等待实际参数传递，也不传播排队后发生的失败。
- 当前框架没有可等待且可传播失败的完整初始化完成信号。不得把 `OnceInit() == true`、`InitModelParam<TModel>()` 已返回、单一 `InitializationReady`/等效布尔值，或参数传递任务已排队，当作完整就绪证明；也不得伪造或提前发布 ready 状态。修正初始化调用契约或暴露可等待结果必须作为单独 R2 或更高变更设计、迁移和验证，涉及硬件/安全时按 R3/R4 提升。
- 需要硬件或执行安全的模块在初始化阶段不得产生不可逆副作用；`TriggerModuleRun`、硬件命令和其他危险入口默认失败关闭。只有独立、显式的启动/启用步骤实际等待并核验该操作依赖的可观察后置条件，例如缓存存在且类型正确、Recipe 同步状态、必要参数传递完成及目标硬件状态，才可开放对应能力。某项前置条件无法观察或等待时必须保持该能力禁用，并先实施经评审的框架级 R2/R3 设计以暴露可等待结果；`LoadKeyParam`/参数确认仍须检查其自身同步返回值，但该结果不能代替完整初始化证明。
- `OnceInit` 或后续初始化步骤部分失败时，必须立即阻止相关执行入口，并按创建的逆序清理本轮注册、订阅和自有资源，使重试前状态可诊断；清理结果也不能被包装为完整就绪证明。
- `[RecipeParam]` 成员必须可读写，不得与导致收集器跳过的 `[JsonIgnore]` 组合；名称、路径、类型、默认值和应用时机保持兼容。`[OutputParam]` 名称、类型、可写性、资源所有权和下游刷新语义同样是 R2 兼容边界。

## 执行、输出、缓存与动态页面

- 当前图执行路径可直接调用 `TriggerModuleRun`，不保证经过 `ModelParamBase.ExecuteModule()` 的 Recipe 包装。修改时必须追踪真实调用方和 Recipe 实际应用时机；若要求每次执行重应用 Recipe，必须设计并验证调用路径变化，不能通过声明同名方法或注册委托推定该行为。
- 真实执行逻辑只注册一次，使用明确替换或空值保护；在重复初始化、反序列化或页面打开路径中不得累积 `+=` 委托。同步桥接异步时必须证明不会在 UI/受限同步上下文死锁，并让异常、取消和超时可观察。
- 执行结果必须准确区分成功、失败、取消、超时、停止和资源不可用。禁止空 `catch`、只记录后返回 `NodeStatus.OK`、丢弃未观察 Task，或在失败后继续硬件/数据副作用。
- 标记输出更新后，必须用 `OutputParamCollector.GetDataPointValues` 的反射键按 `TransmitParam.ParamName` 刷新，再检查 `UpdateParam()`，使模块、全局和下游缓存一致。`Name` 只用于显示；legacy 缺失 `ParamName` 时先提供显式兼容迁移并验证旧方案，禁止静默猜键或保留陈旧值。
- 节点缓存使用 `{serial:D3}`、`TryGetValue` 和类型检查；改键先定位全部读写方并提供迁移、兼容窗口与恢复。缓存读取失败、节点删除或页面延迟打开必须安全失败，禁止未经检查的索引和强制转换。
- 导航类型在模块入口注册一次。节点专属页面使用 `DynamicViewType.NodeMap` 和有效 `NodeSerial`，在幂等 `OnceInit` 中添加；非节点页面使用稳定 `Subjection` 的 `Custom` 并由创建者移除。同一所有者/节点/页面身份不得重复，ViewModel 构造函数不得依赖尚未赋值的导航参数或 `Serial` 创建页面。

## 初始化、释放与安全边界

- 重复 `InitParam`/`OnceInit`、重复打开/确认/关闭、反序列化和恢复不得累积动态页面、事件、执行委托、窗口或缓存。模块级初始化部分失败时必须清理本轮副作用并保持可诊断、可重试状态。
- `Dispose` 先停止/取消自有任务，解绑事件，释放自有图像/Native/窗口辅助资源和 `Custom` 页面，最后在 `finally` 中执行 `base.Dispose()`；释放必须幂等并覆盖节点删除、方案切换和退出。DI、输入参数或其他所有者提供的借用资源不得擅自释放。
- Tools 只通过公开契约组合 Core、Hardware 和 Algorithm，不复制驱动或依赖 `Shell` 内部类型。UI 不拥有硬件资源；任何真实设备动作通过有安全边界的命令/服务，并另行取得真实设备操作授权；生产数据变更另行取得生产数据变更授权。两类授权都必须限定对象、操作、执行人、窗口、检查及停止/回滚/恢复；R4 操作需要具体执行批准且不得由 AI 执行。

## 修改该目录必须完成

以下清单按实际影响和 R0-R4 执行；不适用项必须记录原因，不能虚构为通过。纯 R0 文档变更执行 L0 差异、内容、路径和链接检查；R3/R4 的领域/安全评审、变更负责人批准、停止/回滚、风险披露及 R4 具体执行批准仍按根级强门禁执行。

- [ ] 画清注册、`InitParam`、`OnceInit`、`LoadKeyParam`、实际图执行、输出刷新、导航/缓存和 `Dispose` 调用链，列出资源所有者与 legacy 兼容点。
- [ ] 保持 `Serial`、`Guid`、序列化成员、Recipe 路径、Output 名称/类型/`ParamName`、缓存键、动态页面身份和执行状态兼容；变化按至少 R2 处理。
- [ ] 验证 `InitModelParam<TModel>()` 忽略 `OnceInit()` 返回值、`TransferParam()` 可能仅排队即返回、当前无完整可等待初始化信号、危险入口默认失败关闭、显式启动/启用步骤的实际后置条件核验与部分失败逆序清理，以及图执行直调语义；不把单一布尔、任务排队、返回值或同名 `ExecuteModule()` 当作未证明的框架保证。
- [ ] 覆盖失败、取消、超时、停止、重复初始化/释放、反序列化、节点删除、至少两个同类节点隔离，以及事件/委托/页面不累积。
- [ ] 构建每个修改的 `.csproj`、直接相关测试和可定位消费者；记录完整警告/错误及最终修改后的新鲜证据。
- [ ] 场景验证覆盖 Recipe 收集/保存/加载/下发与实际应用时机、按 `ParamName` 刷新 Output/全局/下游缓存、DynamicView 导航往返和旧方案读取。
- [ ] 硬件相关场景先用桩/模拟器；真机由获授权的人类执行，R4 另需具体批准。交付时披露未验证项、兼容结论、回滚和所需评审/批准。
