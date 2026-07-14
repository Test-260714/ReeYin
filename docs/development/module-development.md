# 模块开发与生命周期

## 修改前快速清单

- [ ] 定位节点的主 Model、主 ViewModel、模块注册类、所有 `INavigationAware` 二级页面，以及自定义 Recipe/Output、窗口、缓存和执行入口。
- [ ] 判断当前模块是现代模式（`base.ModelParam` + `InitModelParam<TModel>()`）还是 legacy/手动模式；现有代码只能作为兼容基线，不能自动视为正确范例。
- [ ] 画清从模块注册、参数页打开、`InitParam`、`OnceInit`、`LoadKeyParam`、执行、输出刷新到 `Dispose` 的调用链和资源所有权。
- [ ] 列出 `Serial`、`Guid`、`mWindowH`、缓存键、序列化成员、Recipe 路径、Output 名称/类型、动态页面身份和执行顺序等兼容边界。
- [ ] 按 [架构与依赖边界](architecture.md)、[编码标准](coding-standards.md) 和 [测试与验证](testing-and-verification.md) 判定 R0-R4；生命周期、Recipe、Output、序列化或缓存变更通常至少是 R2。
- [ ] 只修改已批准范围；legacy 模块只有在初始化、输出资源、序列化、缓存、窗口和执行流都能保持兼容时才迁移。
- [ ] 为失败、取消、超时、停止、恢复、重复初始化、并发和释放路径准备验证；真实设备操作必须另行取得对应授权，R4 操作不得由 AI 执行。
- [ ] 需要实现细节、审计方法或更多示例时，继续阅读本文后续章节；本仓库规范是模块行为和交付门禁的权威来源。

## 目的、范围与事实边界

本规范约束 ReeYin-V 节点模块的 Model/ViewModel 生命周期、Recipe/Output、执行入口、动态页面、节点缓存和 XAML 共享资源接入。目标是复用框架生命周期，避免重复注册、参数未同步、缓存或页面残留、输出不刷新和资源泄漏。

本文描述新增或明确重构时的目标模式，不表示仓库中所有既有模块已经符合这些规则，也不授权顺手迁移 legacy 模块。本文本身是 R0 文档；实际模块变更必须按其最高影响重新定级。涉及公共契约、MVVM 生命周期、Recipe、Output、序列化或缓存时按 R2 或更高等级执行设计、消费者验证和领域评审；涉及硬件、运动、安全或生产数据时还必须满足对应 R3/R4 门禁。

非目标包括：改变业务算法、统一重写所有旧模块、替换公共框架契约、连接或操作真实设备，以及用编译结果证明运行时或设备行为。

## 修改前识别模块形态

先从模块装配入口查到运行副作用，至少记录下列对象和关系：

| 对象 | 必须确认的内容 |
| --- | --- |
| 主 Model | 是否继承 `ModelParamBase`；持久化成员、运行时资源、输入/输出、Recipe、执行委托和释放责任 |
| 主 ViewModel | 是否继承 `DialogViewModelBase`；是否需要 `IViewModuleParam`；`ModelParam` 来源和 `InitParam` 流程 |
| 模块注册类 | 节点/对话框注册、`RegisterForNavigation<TView>()`、服务作用域和动态页面类型注册 |
| 二级页面 | 哪些 ViewModel 实现 `INavigationAware`；导航参数、缓存读取、订阅和离开/释放行为 |
| 自定义 Recipe/Output | 自定义收集、动态输出名/类型、全局参数刷新、旧数据路径和资源所有权 |
| 运行时资源 | `mWindowH`、图像/Native 对象、硬件借用、事件、任务、计时器、取消令牌和缓存 |

### 现代与 legacy 判定

现代主参数页通常同时具备：

- 主 Model 继承 `ModelParamBase`，生命周期扩展放在 `LoadKeyParam`、`OnceInit` 和 `Dispose` 的适当位置。
- 主 ViewModel 的强类型 `ModelParam` 读写 `base.ModelParam`。
- `InitParam` 使用 `ModelParam = InitModelParam<TModel>()`，而不是自行拼装初始化步骤。
- `OnceInit` 只创建无危险的基础注册，并且最多为执行入口安装失败关闭守卫；真实执行委托只能在 `InitModelParam<TModel>()` 返回后的独立启动/启用步骤核验所依赖的可观察后置条件后开放。
- 节点级动态页面按自身依赖处理：仅依赖此时已有效状态的页面可以在 `OnceInit` 中幂等添加；依赖缓存、Recipe、参数传递等后置条件的页面必须等到相应状态可观察且核验成功后再添加，依赖不可观察时保持不注册。

出现以下任一情况时，先按 legacy 处理并调查完整行为，不要直接机械替换：

- ViewModel 自己维护 `_modelParam`，或在 `InitParam` 中 `new Model()`。
- 手动设置 `Serial`、调用 `OnceInit`、`TransferParam`、`InitOutputParamResource`，或自行绑定 `mWindowH`。
- 构造函数、导航回调或每次打开页面时注册动态页面、事件或 `TriggerModuleRun`。
- 自定义输出资源无法直接由 `[OutputParam]` 表达，或旧缓存键、Recipe 路径、序列化数据依赖特殊格式。

legacy 不是豁免，也不是需要在当前任务中自动清理的缺陷清单。先把旧行为作为兼容基线，只有在所有消费者、旧数据和失败/恢复路径都能验证等价时才实施迁移。

## 主 Model 与主 ViewModel 契约

### 主 Model

需要随节点持久化并参与框架运行的主 Model 必须继承 `ModelParamBase`。当节点使用的序列化机制要求类型可序列化，或相邻同类 Model/旧项目数据已依赖该标记时，保留或添加 `[Serializable]`；运行时句柄、窗口、事件、任务、委托和不可持久化资源仍须按实际序列化器使用相应 `[JsonIgnore]`/非序列化约定。不要仅凭基类带有 `[Serializable]` 就假设派生类型和全部成员一定可安全往返。

```csharp
[Serializable]
public class MyModuleModel : ModelParamBase
{
    // 持久化参数、运行时资源和生命周期扩展。
}
```

新增或改变序列化成员前必须确认类型标识、成员名称/类型、默认值、枚举值、忽略属性和旧数据读取行为。不可序列化的运行时对象必须明确重建与释放路径，不能把反序列化成功等同于语义有效。

### 主 ViewModel

主参数页 ViewModel 必须继承 `DialogViewModelBase`；参与节点参数对话框约定时实现 `IViewModuleParam`。强类型属性必须以 `base.ModelParam` 为唯一后备存储，不得再维护一份可能分叉的私有 Model。

```csharp
public class MyModuleViewModel : DialogViewModelBase, IViewModuleParam
{
    public new MyModuleModel ModelParam
    {
        get => base.ModelParam as MyModuleModel
            ?? throw new InvalidOperationException("模块参数尚未初始化。");
        set
        {
            base.ModelParam = value;
            RaisePropertyChanged();
        }
    }

    public override void InitParam()
    {
        ModelParam = InitModelParam<MyModuleModel>();
    }
}
```

`InitModelParam<TModel>()`/基类初始化负责复用传入或缓存的正确 Model、设置 `Serial`，并依次接入 `OnceInit`、`LoadSpecificConfig`、`InitOutputParamResource(Guid)`、`TransferParam` 和调试状态。需要特殊图像窗口或控件时重写 `LoadSpecificConfig(ModelParamBase)`，并在没有明确接管全部窗口初始化责任时调用基类；不要在 ViewModel 构造函数中手动复制这条链。

当前 `InitializeModelParam` 忽略 `OnceInit()` 返回值并继续后续步骤；有 Dispatcher 时，`TransferParam()` 通过 `BeginInvoke` 排队后立即返回 `true`，不等待 `TransferParamCore()` 完成或传播其后续失败。因此当前框架没有可等待、可传播失败的完整初始化完成信号，不得把 `OnceInit()` 返回、`InitModelParam<TModel>()` 返回、单一布尔状态或任务已排队当作完整就绪证明。`TransferParam()` 发生在 `OnceInit()` 之后，所以 `OnceInit()` 也不可能核验本轮参数传递已经完成。

需要硬件或执行安全的模块必须让危险入口默认失败关闭。`OnceInit()` 最多安装拒绝执行的守卫或禁用入口，不得直接把 `TriggerModuleRun` 指向真实硬件/核心逻辑；只有 `InitModelParam<TModel>()` 返回后的独立显式启动/启用步骤，才能等待并核验该操作实际依赖的可观察后置条件，成功后原子地替换为真实委托或打开守卫。失败或无法观察/等待的条件必须保持拒绝执行，并先通过 R2/R3 框架设计暴露可等待结果。初始化部分失败必须逆序清理本轮注册、订阅和自有资源。

加载/确认命令在把 `"Param"` 返回调用方前必须调用 `ModelParam.LoadKeyParam()`，检查返回值并在失败时阻止确认或执行。不得忽略 `false` 后继续把未同步参数当作有效参数返回。

## Model 生命周期顺序

### `LoadKeyParam`

扩展输入、Recipe 或运行时选择加载时，默认先调用 `base.LoadKeyParam()`。基类负责输入参数传递、Recipe 应用和标记输入同步；任何阶段失败都必须立即返回失败，不得继续使用部分更新状态。

```csharp
public override bool LoadKeyParam()
{
    if (!base.LoadKeyParam())
    {
        return false;
    }

    return LoadModuleSpecificRuntimeState();
}
```

只有设计明确批准绕过框架输入/Recipe 同步，并覆盖公共消费者、兼容和回滚验证时，才能不调用基类。模块专用加载抛出异常时，应在拥有恢复或契约转换责任的边界记录脱敏上下文并返回明确失败；不能空 `catch`、只打印后继续执行，或把异常转换为 `true`。

### `OnceInit`

当前 [ModelParamBase.cs](../../Core/ReeYin-V.Core/Interfaces/ModelParamBase.cs) 的 `OnceInit()` 会建立节点删除订阅；当 `Serial >= 0` 时调用 `AddNodeParamCache(this)`；随后调用 `SyncRecipeParams()`。但当前实现没有检查缓存添加或 Recipe 同步的返回值，最后无条件返回 `true`。因此，当前的 `base.OnceInit() == true` **不能证明**节点缓存已写入或 Recipe 元数据已同步成功。

一次性基础初始化必须按以下顺序处理：

1. 检查 `IsOnceInit`；该标志只能表示无危险的基础注册阶段已经完成，不能表示参数传递、缓存、Recipe、硬件或执行能力已经就绪。
2. 调用 `base.OnceInit()` 并保留 `false` 检查，以兼容基类将来开始传播失败的实现；不能把当前返回的 `true` 当作缓存或 Recipe 成功证据。
3. 只创建不触达硬件或核心执行逻辑的基础注册；`TriggerModuleRun` 在此阶段最多安装明确返回失败/未就绪的守卫。基础注册部分失败时按创建逆序清理并返回失败。
4. 当本轮无危险基础注册均成功后可以设置 `IsOnceInit = true`，但该值仅记录基础注册完成，不承诺缓存/Recipe/参数传递或完整初始化就绪。
5. 在 `InitModelParam<TModel>()` 返回后的独立启动/启用步骤中，分别等待并核验执行所依赖的可观察后置条件；全部成功后才替换真实执行委托或打开守卫，任一失败或不可观察都保持拒绝执行。

当前框架没有通过 `OnceInit()` 返回值暴露缓存添加与 Recipe 同步的组合结果，这是需要明确记录的框架限制。关键模块应在后续独立启动/启用步骤中使用实际可用的 ProjectManager/Recipe 查询能力和日志核验执行所需状态；若现有 API 不能可靠证明依赖已经就绪，不得虚构一个“检查通过”门禁或开放真实执行。此时可以保留仅表示基础注册完成的 `IsOnceInit = true`，但危险入口必须继续拒绝，并将“让基类传播失败或提供可查询状态”作为单独的框架变更，按 R2 或更高等级设计、验证和评审。

如果基础注册步骤失败，必须报告失败并清理本次已创建的局部订阅/资源，使重试具有明确语义；不得在基础注册完成前把 `IsOnceInit` 设为 `true`，也不得吞掉异常后伪装成功。后续启用失败不得回退为调用真实逻辑，且要清理该启用步骤本轮副作用并保持守卫关闭。对重复打开、反序列化回调和恢复路径都要验证幂等性。

### `Dispose`

释放顺序必须是：先停止或取消本模块拥有的任务，解绑本地事件，释放本模块拥有的图像/Native/窗口辅助资源和自定义动态页面；最后调用 `base.Dispose()`。基类负责移除 Recipe 元数据、节点参数缓存、Output/Global 缓存、节点删除订阅和当前 `Serial` 的 `NodeMap` 页面。

释放必须幂等，并覆盖部分初始化失败、重复关闭、节点删除、方案切换和应用退出。局部清理失败不能导致基类清理被跳过；应按全局异常规范保留主要故障和清理故障的诊断信息。借用自 DI、属性 getter、方法参数或其他所有者的资源不得擅自释放。

```csharp
public override void Dispose()
{
    try
    {
        _runCancellation?.Cancel();
        UnsubscribeLocalEvents();
        DisposeOwnedResources();
        RemoveOwnedCustomViews();
    }
    finally
    {
        base.Dispose();
    }
}
```

不要仅为“统一写法”迁移 legacy 生命周期。迁移前必须证明旧 `Serial`、`mWindowH`、缓存键、输出资源、序列化和执行流能完整保留。

## 执行入口与委托

[NodeViewModel.cs](../../Application/NodifyFlow/Nodify.FlowApp/ViewModels/Node/NodeViewModel.cs) 的当前图运行路径在 `Execute`、`ExecuteMulti` 等分支中直接调用 `ModuleParam.TriggerModuleRun.Invoke()`。因此，图执行**不会必然经过** `ModelParamBase.ExecuteModule()`；同步注册到 `TriggerModuleRun` 的模块逻辑会绕过基类包装。

[ModelParamBase.cs](../../Core/ReeYin-V.Core/Interfaces/ModelParamBase.cs) 中的 `ExecuteModule()` 只有在调用方显式调用并实际执行其返回的 Task 时，才会在 Task 主体内调用 `ApplyRecipeParamValues()`，然后调用 `TriggerModuleRun`。但当前实现忽略 `ApplyRecipeParamValues()` 的 `false` 结果并继续执行委托，所以显式走现有包装既不能证明 Recipe 应用成功，也不是安全的逐次应用方案。当前图运行通常依赖参数页加载/确认阶段此前成功调用 `LoadKeyParam()` 所应用的 Recipe，不能据此推断“每次图执行都会重新应用 Recipe”。

若业务要求每次执行都应用当前 Recipe，必须先实施经评审的 R2/R3 框架修改或受控包装，使 Recipe 应用失败能阻止真实委托、产生调用方可观察的失败结果，再验证 Task 启动/等待、异常、状态、取消、超时、并发和兼容行为。禁止直接推荐当前 `ModelParamBase.ExecuteModule()` 作为解决方案，也不能仅靠声明同名方法或注册同步委托来假设这一保证。

无论调用入口如何选择，模块真实执行逻辑必须只注册一次：

- `OnceInit()` 中最多用 `TriggerModuleRun = ...` 安装明确失败关闭的守卫；不得在该阶段赋值为真实硬件/核心执行方法。
- `InitModelParam<TModel>()` 返回后的独立启动/启用步骤必须逐项核验可观察后置条件；全部成功后才能原子替换为真实委托或打开守卫，失败和不可观察路径都保持拒绝执行。
- 禁止在可能重复调用的初始化、反序列化或打开路径使用 `+=`，除非多播就是经过设计和验证的公共行为。
- 需要取消注册时保存命名方法、委托字段或 Prism 订阅令牌。`TriggerModuleRun -= () => ExecuteModule().Result` 创建的是新委托，不能移除原匿名 lambda。
- 如果派生 Model 定义与基类同签名的 `ExecuteModule()`，必须显式使用 `new`，并用注释说明框架的 `TriggerModuleRun` 调用派生执行逻辑，避免无意隐藏。

```csharp
// OnceInit 中只允许安装失败关闭入口；不得在这里触达真实核心逻辑。
TriggerModuleRun = RejectExecutionUntilEnabled;

private ExecuteModuleOutput RejectExecutionUntilEnabled()
{
    return new ExecuteModuleOutput { RunStatus = NodeStatus.Failed };
}
```

上例只展示 `OnceInit()` 可安装的守卫，不展示也不授权真实执行委托赋值。真实委托只能由后续独立启动/启用步骤在核验成功后替换，或由该步骤打开同等失败关闭的守卫；替换/打开操作还必须满足实际并发与可见性要求。实际耗时操作必须遵守异步、取消、超时、线程和安全停止规范。若既有同步 `Func<ExecuteModuleOutput>` 边界只能通过 `.Result` 桥接异步方法，必须证明不会在 UI/受限同步上下文死锁并把阻塞限制在该边界；能使用同步核心或完整异步链时不得无理由阻塞。

执行异常必须由拥有执行契约的边界转换为明确失败状态并记录可诊断、脱敏的上下文，或继续传播给明确所有者。禁止空 `catch`、仅记录后返回 `NodeStatus.OK`、丢弃未观察 Task，或在失败后继续设备/数据副作用。

## Recipe 与 Output 参数

### `[RecipeParam]`

- 用于由 Recipe Manager 收集、保存、下发和应用的值；成员必须可读写。
- 禁止与 Newtonsoft 或 System.Text.Json 的 `[JsonIgnore]` 同时标记；收集器遇到忽略标记会跳过该成员及其嵌套路径。
- 自定义嵌套配置类可以包含 Recipe 值，但父成员不能被 `[JsonIgnore]` 忽略。收集器不递归集合、委托以及 `System.*`/`Microsoft.*` 等框架类型。
- 嵌套成员显示名称可能重复时，使用唯一名称，或让消费者使用 `Path`/`MemberPath`；不得依赖有歧义的简单名称。
- 更改名称、类型、成员路径、默认值或应用时机属于兼容边界，必须验证旧 Recipe 和缺失/无效值。

```csharp
[RecipeParam("采样次数", "由配方保存和下发的采样次数")]
public int SampleCount
{
    get => _sampleCount;
    set => SetProperty(ref _sampleCount, value);
}
```

### `[OutputParam]`

[OutputParamAttribute.cs](../../Application/ReeYin_V.Share/Extension/OutputParamAttribute.cs) 定义反射收集键；[TransmitParam.cs](../../Core/ReeYin-V.Core/Services/Project/Models/TransmitParam.cs) 则分别保存 `ParamName` 和显示用 `Name`。

- 用于下游节点可选择、显示或同步到全局参数的输出。
- 可以标记字段或可写属性；只读属性不会被 `OutputParamCollector` 收集。
- 运行时资源型输出可以按序列化需要使用 `[JsonIgnore]`，但必须定义克隆、借用、接管和释放责任，避免悬空引用或重复释放。
- 输出名称、类型、可写性和刷新时机都是公共契约；不能只更新 Model 属性而让 `OutputParams`、全局参数或下游缓存保持旧值。`TransmitParam.ParamName` 保存反射输出成员键，`TransmitParam.Name` 可能是包含节点或 UI 限定信息的显示名，两者不能混用。

```csharp
[OutputParam("ResultCount", "本次执行的结果数量")]
[JsonIgnore]
public int ResultCount { get; set; }
```

创建由 `[OutputParam]` 产生的 `TransmitParam` 时，必须把收集器定义名写入 `ParamName`；显示用 `Name` 可以按既有兼容格式包含节点信息。当前 [EveSensorDataCollectionModel.cs](../../CustomizedDemand/Custom.EVEMFDJC/Models/EveSensorDataCollectionModel.cs) 的刷新路径也使用 `item.ParamName` 查找 `OutputParamCollector.GetDataPointValues(this)` 的反射值。

执行更新标记输出后，在下游或全局缓存需要新值时，按 `ParamName` 更新反射型输出，再调用 `UpdateParam()`。缺少 `ParamName` 或找不到对应反射键必须形成可观察失败，禁止跳过后保留旧值：

```csharp
private bool TryRefreshMarkedOutputParams()
{
    var values = OutputParamCollector.GetDataPointValues(this);
    foreach (var outputParam in OutputParams
        .Where(item => item.Resourece == ResoureceType.None))
    {
        if (string.IsNullOrWhiteSpace(outputParam.ParamName)
            || !values.TryGetValue(outputParam.ParamName, out var value))
        {
            return false;
        }

        outputParam.Value = value;
    }

    return true;
}

if (!TryRefreshMarkedOutputParams() || !UpdateParam())
{
    return new ExecuteModuleOutput { RunStatus = NodeStatus.Failed };
}
```

上例只处理 `ResoureceType.None` 的反射型输出；输入透传或其他自定义资源必须走各自已定义的映射和所有权路径。调用方必须在刷新返回 `false` 时使用模块现有日志入口记录脱敏上下文并返回失败状态。legacy 输出若没有 `ParamName`，应先建立显示名到反射键的显式兼容迁移并验证旧方案，不能静默回退到 `Name` 猜测键。

自定义动态输出名称/类型无法由 `[OutputParam]` 表达时，可以保留或重写 `InitOutputParamResource`，但必须记录为何基类收集器不足，并验证 `Guid`、资源路径、名称、类型、旧输出选择和全局/下游刷新兼容。不得在迁移时无证据删除旧输出资源初始化。

## DynamicView、导航与节点缓存

### 注册与添加

页面类型在模块注册类中通过 Prism 注册一次：

```csharp
containerRegistry.RegisterForNavigation<OtherConfigView>();
```

节点专属页面在 Model 的 `OnceInit` 中，在 `base.OnceInit()` 返回且本模块必需的缓存/Recipe 状态已经按前述限制核验后添加。守卫保证同一 Model 的重复初始化不会重复产生副作用；`NodeSerial` 必须使用此时已经有效的当前 `Serial`。

```csharp
PrismProvider.DynamicViewManager.AddDynamic(new DynamicView
{
    Type = DynamicViewType.NodeMap,
    NodeSerial = Serial,
    DisplayName = $"{Serial}-其他配置",
    ViewName = "OtherConfigView"
});
```

`DynamicViewType.NodeMap` 表示页面属于一个节点，框架按 `Serial` 清理。`DynamicViewType.Custom` 表示非节点级自定义页面，应使用稳定的 `Subjection` 区分归属，并由创建它的所有者显式移除；不能把节点页面改成 `Custom` 来绕过节点生命周期。不要在 ViewModel 构造函数中添加 NodeMap 页面，此时真实 `Serial` 可能尚未赋值。

### `INavigationAware` 与 `NodeParamCaches`

二级页面必须校验导航参数存在性和类型，再使用约定的 `{serial:D3}` 键、`TryGetValue` 和 Model 类型检查解析父节点。节点可能已删除、页面可能延迟打开，禁止直接使用字典索引或未经检查的强制转换。

```csharp
public void OnNavigatedTo(NavigationContext navigationContext)
{
    if (!navigationContext.Parameters.TryGetValue<int>("Serial", out var serial))
    {
        return;
    }

    var caches = PrismProvider.ProjectManager?
        .SltCurSolutionItem?
        .NodeParamCaches;

    if (caches?.TryGetValue($"{serial:D3}", out var value) == true
        && value is MyModuleModel parent)
    {
        Model = parent.OtherConfig;
    }
}
```

新增模块使用 `{serial:D3}`。legacy 模块若使用原始序号或其他缓存键，先定位所有读写方和旧方案数据；只有提供迁移/兼容窗口并验证恢复路径后才能改键。`ModelParamBase.Dispose()` 会移除 Model 节点缓存和当前节点的 NodeMap 页面；自定义缓存键、`Custom` 页面和外部订阅仍由模块显式清理。

验证至少覆盖两个同类节点，确认页面身份、缓存读取和输出不会串节点；还要覆盖重复打开、导航往返、节点删除、反序列化恢复和最终释放。

## XAML 与共享 UI 资源

模块页面默认复用 `ReeYin_V.UI` 的共享资源。正常由 Shell 承载的页面依赖应用级合并的 `Core/ReeYin_V.UI/Style/Generic.xaml`；独立宿主确实没有资源时，才在 App/Window 级合并 `Generic.xaml`，不要复制单个模板。

| 用途 | 默认资源/控件 |
| --- | --- |
| 常规操作按钮 | `Style="{StaticResource GeneralButtonStyle}"`；仅在同页面家族已有明确需求时使用 `GeneralButtonStyle2` |
| 参数或输出表格 | `Style="{StaticResource DefaultDataGridStyle}"` |
| 参数分组 | `Style="{DynamicResource ExpanderStyle}"` |
| 主题颜色 | `{DynamicResource SelectedBrush}`、`BtnOverColor`、`RegionBackgroundBrush`、`SplitBrush` |
| 图标文字 | `FontFamily="{StaticResource Iconfont}"`，并复用既有图标码位语义 |
| 上游/全局参数链接 | `WxLink` |
| 数值输入与范围约束 | `WxNumericUpDown` |

主题刷必须使用 `DynamicResource`，避免硬编码颜色破坏亮色、暗色或定制主题。新增局部样式仅允许以下两类情况：基于共享样式用 `BasedOn="{StaticResource ...}"` 做小范围扩展；或同一模块页面家族已有必要且兼容的局部变体。局部样式不得复制整个 `Button`、`DataGrid`、`Expander` 或滚动模板。

View 只负责呈现和纯交互；设备、数据库和长生命周期资源不能由 code-behind 或 UI 直接拥有。新增/修改页面必须检查 Binding/命令错误、禁用与运行状态、失败提示、取消/关闭、键盘/焦点、长文本、缩放/DPI 和主题。

## legacy 迁移与兼容门禁

迁移前建立行为清单和基线证据，至少逐项对比：

| 兼容项 | 必须保持或提供迁移的内容 |
| --- | --- |
| `Serial` | 赋值时机、默认/无效值处理、节点隔离、显示和缓存关联 |
| `mWindowH` | 图像窗口的创建/复用、`ModuleName`、窗口缓存和释放所有权 |
| 缓存键 | `NodeParamCaches`、节点输出缓存及自定义缓存的格式、大小写和生命周期 |
| 输出资源 | `Guid`、名称、类型、描述、资源路径、输出选择、全局参数和刷新时机 |
| 序列化 | 类型/成员标识、默认值、忽略属性、旧数据读取、缺失/额外字段和运行时重建 |
| Recipe | 名称、`Path`/`MemberPath`、保存/加载/下发时机和无效值失败语义 |
| 执行流 | 实际调用方是直接委托还是包装入口、Recipe 的实际应用时机、`TriggerModuleRun` 次数、状态/异常、取消/超时和停止 |
| 动态页面/事件 | 注册身份、重复打开、导航恢复、节点删除、取消订阅和释放 |

推荐迁移顺序是：先记录现有调用链和行为测试；再把强类型属性绑定到 `base.ModelParam`；用 `InitModelParam<TModel>()` 替换可证明冗余的手动步骤；把一次性 Model 工作移入 `OnceInit`；把特殊窗口绑定移入 `LoadSpecificConfig`；最后在输出资源确实等价时才删除手动初始化。

任何一项无法证明等价时，保留 legacy 实现并在交付中记录原因、风险和后续负责人，不得以“现代化”为理由扩大当前任务。迁移改变生命周期、Recipe、Output、序列化或缓存时至少按 R2 处理；涉及硬件或安全行为时取更高等级。

## 失败、异常、日志与状态

- `LoadKeyParam`、`OnceInit`、输出刷新、导航解析和执行入口的失败必须被调用方观察；返回 `bool` 的方法失败时返回 `false`，执行失败使用准确的 `NodeStatus`/契约结果，不能继续成功路径。
- 只在能补充上下文、转换契约结果、恢复或清理的边界捕获异常。禁止空 `catch`、只写控制台后继续、丢失原异常，或把异常改写成成功。
- 使用仓库现有 NLog/局部日志入口，记录操作、脱敏稳定标识、阶段、状态和可执行上下文；不要重复记录同一异常，也不要记录凭据、客户/生产数据或设备敏感标识。
- 取消不是成功，也不应默认记为错误；超时、取消、资源不可用和不可恢复故障必须区分。硬件失败不得降级为普通业务成功。
- 后台任务、事件和计时器必须有可定位所有者；异常要被观察，释放时要取消、等待或解绑。匿名 lambda 只有保存原实例后才能可靠取消订阅。
- 清理失败不能隐藏主要故障。需要报告多个故障时保留主异常和清理异常的诊断关系，并确保安全停止和基类释放仍被尝试。

## 验证矩阵

验证必须在最终修改后产生新鲜证据，按 L0 到更高层逐步推进。下表是模块变更的最低关注点，不替代 [测试与验证](testing-and-verification.md) 的风险门禁。

| 变更面 | 最低验证 |
| --- | --- |
| 所有模块代码 | 先检查最终差异、静态规则和敏感信息；构建实际 touched `.csproj`，记录完整错误和警告；再构建可定位消费者 |
| 生命周期/委托 | 新建、反序列化、重复 `InitParam`/`OnceInit`、重复打开/确认/关闭、节点删除和重复 `Dispose`；独立核验节点缓存和 Recipe 状态，确认无重复执行、事件或页面残留 |
| Recipe/Output | Recipe 收集、保存、加载、下发、直接委托与包装入口的实际应用时机、嵌套/重名/无效成员；按 `ParamName` 刷新输出、全局/下游缓存、资源所有权和旧方案读取 |
| DynamicView/缓存 | 至少两个同类节点，验证 `{serial:D3}` 隔离、导航往返、延迟打开、节点删除、反序列化恢复、`NodeMap`/`Custom` 清理 |
| XAML/UI | 构建页面项目；人工检查页面打开、Binding/命令、关键交互、取消/关闭、键盘/焦点、长文本、缩放/DPI、亮/暗主题 |
| 序列化/legacy 迁移 | 新数据往返、旧数据读取、缺失/额外/错误字段、默认值、Recipe/Output/缓存兼容、迁移失败恢复和回滚 |
| 失败与恢复 | 基类返回失败、模块加载失败、异常、取消、超时、停止、重试边界、部分初始化、资源不可用和清理失败；确认不会误报成功 |
| 硬件相关模块 | 先用桩/模拟器覆盖离线、超时、异常、停止、重连和释放；模拟/编译结果不得表述为真实设备验证 |
| 真实设备场景 | 仅在 [安全与信息安全](safety-and-security.md) 的设备身份、人员/工件安全、限位、急停/互锁、速度/加速度/单位、超时和安全停止前提满足，并取得限定对象/操作/执行人/窗口的授权后由有权限的人类执行；R4 操作禁止交给 AI |

每条命令证据记录精确命令、工作目录、执行时间和时区、环境/SDK/配置/架构、退出码、错误/失败/跳过计数、关键输出、证明范围和未证明范围；工具没有某字段时写“`不适用`”。人工场景记录步骤、环境、预期与实际结果、执行人、时间、适用的脱敏截图/日志及未覆盖分支。失败、跳过和未验证项必须如实列出，不能用构建或模拟结果替代场景、UI 或真实设备证据。

## 自审、交付与回滚

交付前确认差异只含批准范围，调用链和兼容清单已覆盖，异常/取消/释放路径可诊断，文档与实现一致，且没有把 legacy 现状描述为全仓合规。R2 公共契约或兼容边界必须由熟悉领域的人类评审；R3/R4 继续执行根级领域评审、变更批准、停止/回滚和风险披露门禁，作者或 AI 不得批准自己的例外。

交付说明必须列出修改内容与非目标、风险等级、最终修改后的验证证据、失败/未验证项、API/配置/数据/Recipe/Output/缓存/UI/设备兼容结论、残余风险和所需评审/批准。回滚应恢复变更前源码和配置，并在适用时恢复经备份的 Recipe、方案或数据；回滚前必须停止相关执行、释放模块资源并确认旧版本仍能读取现有数据。提交、推送、合并、发布、部署和真实设备操作均是独立授权动作。
