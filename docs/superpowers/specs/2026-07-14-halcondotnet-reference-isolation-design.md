# HALCON .NET 引用隔离设计

## 1. 问题定义

仓库根级 `Directory.Build.props` 当前无条件向子项目添加名为
`halcondotnet` 的程序集引用，但 `HintPath` 实际指向
`packages/A_ThirdParty/Halcon/halcondotnetxl.dll`。这会造成以下问题：

- 不使用 HALCON 的项目也获得 HALCON 编译依赖；
- 引用标识 `halcondotnet` 与文件程序集身份 `halcondotnetxl` 不一致；
- 部分项目同时保留项目级 HALCON 引用，形成重复项；
- 24 个项目级 `halcondotnet` 引用仍指向已经不存在的旧目录；
- PointCloud 项目必须使用 `Reference Remove` 抵消根级依赖；
- WPF 设计时临时项目会把错误的依赖闭包继续展开为 `ReferencePath`。

最小复现是评估不含任何 HALCON 源码的
`Core/ReeYin_V.Logger/ReeYin_V.Logger.csproj`。其 `Reference` 项仍包含
`halcondotnet`，且 `DefiningProjectFullPath` 指向根级
`Directory.Build.props`。相比之下，显式执行
`Reference Remove="halcondotnet"` 的 PointCloud 项目评估结果为空。

## 2. 目标

- 所有需要 HALCON 的项目统一使用 `halcondotnetxl.dll` 23.11.0.0。
- 引用标识、程序集身份和文件路径统一为 `halcondotnetxl`。
- HALCON 依赖改为项目显式启用，默认项目不获得该引用。
- 每个消费者项目评估后恰好只有一个有效 HALCON .NET 引用。
- 非消费者项目评估后不存在 `halcondotnet` 或 `halcondotnetxl` 引用。
- 清除项目文件中的失效 HALCON 路径、重复引用和补偿性 `Remove`。
- 保持 HALCON 程序集复制到输出目录的现有行为。

## 3. 非目标

- 不升级 HALCON SDK、Native Runtime 或许可证。
- 不改变业务代码中的 `HalconDotNet` 命名空间和公开类型。
- 不重构 `ImageTool.Halcon`、图像所有权或算法实现。
- 不调整现有 `ProjectReference` 依赖方向。
- 不修改或清理 `*_wpftmp.csproj`、`bin`、`obj` 等生成物。
- 不连接相机、控制卡或其他真实设备，不执行真实设备验证。
- 不顺带修复与 HALCON 引用隔离无关的现有构建警告。

## 4. 风险等级

本变更按 R2 管理。它不改变源码 API，但会改变多个项目的编译依赖和
运行时程序集选择，属于公共兼容边界与消费者构建范围。交付前需要熟悉
HALCON 23.11、模块加载和输出复制行为的人类领域评审。

## 5. 方案比较

### 5.1 根级显式启用（采用）

根级保留唯一的程序集路径和复制规则，仅当项目设置
`ReeYinUseHalconDotNet=true` 时注入引用。

优点：路径只有一个权威来源；新项目默认无 HALCON；项目依赖意图可见；
可一次性消除重复和失效路径。缺点：需要修改全部已识别消费者项目。

### 5.2 每个项目独立声明（不采用）

完全移除根级定义，在每个消费者项目中复制完整 `Reference`。

优点：项目文件可独立阅读。缺点：路径和元数据重复，未来容易再次漂移，
无法阻止普通版与 XL 版混用。

### 5.3 全局启用、逐项目排除（不采用）

保留无条件全局引用，在非消费者项目中添加 `Reference Remove`。

优点：短期保留现状。缺点：新项目继续被污染，排除列表持续增长，仍然是
默认依赖 HALCON 的错误模型。

## 6. 配置设计

根级 `Directory.Build.props` 使用条件化的唯一引用：

```xml
<ItemGroup Condition="'$(ReeYinUseHalconDotNet)' == 'true'">
  <Reference Include="halcondotnetxl">
    <HintPath>$(ReeYinRepositoryRoot)packages\A_ThirdParty\Halcon\halcondotnetxl.dll</HintPath>
    <Private>true</Private>
  </Reference>
</ItemGroup>
```

消费者项目在现有 `PropertyGroup` 中声明：

```xml
<ReeYinUseHalconDotNet>true</ReeYinUseHalconDotNet>
```

项目文件不再直接声明 `Reference Include="halcondotnet"`、
`Reference Include="halcondotnetxl"` 或
`Reference Remove="halcondotnet"`。程序集路径、标识和复制策略只由根级
配置拥有。

## 7. 消费者识别

消费者集合覆盖仓库内全部非临时 `.csproj`，采用以下证据的并集：

1. 项目目录的编译输入包含 `HalconDotNet` 命名空间或 HALCON 类型；
2. 项目文件当前存在 `halcondotnet` 或 `halcondotnetxl` 直接引用；
3. 项目直接引用 `Tools/Image/ImageTool.Halcon/ImageTool.Halcon.csproj`。

该并集优先保持兼容，避免仅按文本 usage 删除由公开签名、XAML、反射、
设计器或项目依赖要求的引用。实施时生成明确项目清单并人工检查嵌套项目
边界；`*_wpftmp.csproj`、`bin`、`obj` 和 `obj-*` 不参与清单。

没有以上任一证据的项目不得设置 `ReeYinUseHalconDotNet`。PointCloud 四个
项目属于非消费者，删除其补偿性 `Reference Remove` 后应自然得到空引用。

## 8. 兼容性

### 8.1 编译兼容

`halcondotnet.dll` 与 `halcondotnetxl.dll` 都暴露 `HalconDotNet` 命名空间，
但程序集身份不同。本变更按用户确认统一到 XL 变体，所有消费者必须重新
编译，禁止依靠旧的 `bin` 或 `OutputExe` 产物证明兼容。

### 8.2 运行时兼容

`Private=true` 保持托管程序集 Copy Local 行为。Shell 现有 HALCON XL Native
Runtime 复制/发布配置不在本次修改范围内，但必须通过干净输出验证托管与
Native DLL 可被加载。构建成功不能代替模块加载或启动冒烟。

### 8.3 公共契约和数据

不改变公开 C# 签名、序列化成员、Recipe、Output、缓存、数据库或配置格式。
如果消费者构建暴露出公开签名对普通版程序集身份的二进制依赖，必须停止
实施并升级设计，不得通过绑定重定向或复制旧 DLL 静默绕过。

## 9. 验证设计

新增仓库级 PowerShell 验证脚本，先在修改前运行并确认失败，再实施配置
修改。脚本检查：

- 根级引用必须受 `ReeYinUseHalconDotNet` 条件控制；
- 根级引用的 Include、HintPath、程序集真实身份和版本必须一致；
- 所有非临时项目不得包含项目级 HALCON `Include` 或 `Remove`；
- 每个显式消费者项目评估后恰好得到一个 `halcondotnetxl`；
- 每个非消费者代表项目评估后不含 HALCON 引用；
- 不存在指向缺失 `halcondotnet.dll` 的 HintPath；
- HALCON DLL 存在，程序集名为 `halcondotnetxl`，版本为 23.11.0.0。

分层验证顺序：

1. 运行验证脚本，保留 RED 和 GREEN 结果；
2. 对 Logger、PointCloud、Core、`ImageTool.Halcon` 和一个定制消费者执行
   MSBuild 项目评估；
3. 构建最小 HALCON 消费者与非消费者；
4. 构建直接引用 `ImageTool.Halcon` 的消费者；
5. 在依赖和 SDK 可用时构建 `ReeYin.sln`；
6. 由具备 HALCON 环境的人工执行干净输出下的应用启动和 HALCON 模块加载
   冒烟，记录托管/Native DLL 加载结果。

缺少 NuGet 资产、HALCON 许可证或 Native Runtime 时，对应构建或运行验证
必须标记为“未验证”，不得推断为通过。

## 10. 失败、恢复与回滚

- 验证脚本发现消费者缺失时，停止构建扩展，补充证据并更新消费者清单。
- 任何消费者出现程序集身份、公开签名或运行时加载不兼容时，停止交付，
  不同时保留普通版和 XL 版掩盖问题。
- 回滚方式是恢复原 `Directory.Build.props` 和项目文件，不涉及数据恢复。
- 回滚后必须重新运行项目评估和最小消费者构建；旧的重复/失效引用警告会
  重新出现，应在交付记录中明确。

## 11. 成功标准

1. 非 HALCON 项目不再显示或解析 HALCON .NET 引用。
2. HALCON 消费者只解析一个 `halcondotnetxl` 23.11.0.0 引用。
3. 仓库不存在项目级 HALCON Include、Remove 或失效 HintPath。
4. PointCloud 项目无需补偿配置即可保持无 HALCON 依赖。
5. 最小消费者、可定位直接消费者和解决方案验证结果按 R2 门禁记录。
6. 最终差异不包含生成物、依赖升级或业务代码改动。

## 12. 评审与交付限制

- 需要 HALCON/模块加载领域人员评审消费者清单、XL 兼容和运行时复制结论。
- AI 不执行真实设备验证，不批准兼容风险或验证例外。
- 提交、推送、合并、发布和部署仍需分别获得明确授权。

