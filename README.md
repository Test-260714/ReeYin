# ReeYin-V

ReeYin-V 是一个基于 **.NET 8 + WPF + Prism** 的模块化工业视觉/设备控制平台。项目将业务界面、硬件接入、算法工具、定制需求拆分为独立模块（DLL），由主程序在运行时动态加载，适合做“标准能力 + 现场定制”并行演进的系统。

## 1. 核心定位

- 面向工业视觉与自动化场景（图像采集、处理、测量、深度学习推理、设备联动）。
- 采用 **模块化插件架构**，支持按目录加载、按条件启用、按现场配置裁剪功能。
- 提供 **节点化工具生态**（图像、逻辑、硬件、算法、脚本、定制模块），便于快速组合流程。

## 2. 技术栈

- 框架：`WPF`、`Prism.DryIoc`、`Nodify`
- 运行时：`net8.0-windows`
- 数据层：`SqlSugar + SQLite(SQLCipher)`
- 视觉与算法：`HALCON`、`OpenCvSharp`
- 日志：`NLog`
- 部分原生能力：`C++/CLI` 与 Native 工程（标定、深度学习、加密等）

## 3. 总体架构

```text
Shell (ReeYin 主程序)
  ├─ Core
  │   ├─ IOC/服务注册（ExposedService + 自动初始化）
  │   ├─ 配置与缓存（Config/Cache）
  │   ├─ 用户/权限/菜单/语言/主题/工作状态
  │   ├─ 项目与方案管理（NodifySolution）
  │   └─ 模块分类与条件加载（CategoryModuleManager + DB Rule）
  │
  ├─ Application（业务模块）
  │   ├─ Login / Main / Initialize / Config
  │   ├─ UserManager / Permission
  │   ├─ Status / RootManager / NodifySolution
  │   └─ Share（跨模块事件、常量、服务）
  │
  ├─ Hardware（硬件适配层）
  │   ├─ Camera（HIK/Basler/DaHeng/IKap/...）
  │   ├─ ControlCard（Googol/ZMotion/Leisai/None）
  │   ├─ PLC / Sensor / LightController
  │   └─ Base + Vendor 插件分层
  │
  ├─ Tools（节点工具层）
  │   ├─ Image（取图/连续采集/存图）
  │   ├─ Logical（条件/循环/监控/结束/参数）
  │   ├─ GeneralALGO（测量、匹配、区域、DL 等）
  │   ├─ HardwareTool（运动/PLC）
  │   └─ CSharpScript（脚本节点）
  │
  ├─ CustomizedDemand（现场定制模块）
  └─ Algorithm（Native 算法工程）
```

## 4. 启动流程（高层）

1. `Shell` 启动，做单实例检查、全局异常捕获、日志/控制台初始化。
2. 优先加载 `CoreModule`、`ShareModule`。
3. 通过 `DirectoryModuleCatalog` 从 `./Modules` 目录发现业务/工具/硬件模块。
4. 进入登录模块，登录成功后进入硬件初始化与主界面。
5. 运行过程中通过事件总线（`EventAggregator`）联动权限、语言、工作状态、硬件状态等。

## 5. 基本功能

- **账户与权限**
  - 登录、记住密码、刷卡登录。
  - 用户/角色/权限/菜单可见性管理（RBAC 思路）。
- **模块化主界面**
  - 主区域按模块导航，菜单由权限与语言动态驱动。
- **硬件接入与初始化**
  - 支持多类型设备（相机、控制卡、PLC、传感器、光源控制器）。
  - 启动阶段统一初始化，退出阶段统一关闭。
- **视觉与算法节点工具**
  - 图像采集、图像处理、几何测量、形状匹配、深度学习推理等。
  - 逻辑控制节点（条件、循环、监视、结束、参数关联）。
- **方案/项目管理**
  - 支持方案列表、默认启动项、路径管理、运行缓存。
- **状态与维护能力**
  - 硬件状态面板、日志面板、模块启用配置、在线/离线更新。

## 6. 模块加载与部署机制

- 运行时模块目录：`OutputExe/<Config>/net8.0-windows/Modules`
- 各业务/工具/硬件项目在 `PostBuild` 中会将自身 DLL 复制到 `Modules`。
- `CategoryModuleManager` 支持按分类初始化模块。
- `ConditionalModuleInitializer` + `module_load_config` 支持按规则（启用、站点、互斥、依赖、许可证）控制模块加载。

## 7. 目录说明（顶层）

- `Shell/`：应用入口与主窗口
- `Core/`：基础能力（IOC、配置、数据库、服务、事件）
- `Application/`：主业务模块
- `Hardware/`：硬件基座与厂商实现
- `Tools/`：节点工具模块
- `CustomizedDemand/`：定制项目能力
- `Algorithm/`：原生算法工程
- `OutputExe/`：编译输出、模块目录、发布产物
- `Resource/`：资源文件
- `thirdparty/`、`packages/`：第三方依赖与二进制

## 8. 快速开始

### 8.1 环境要求

- Windows 10/11 x64
- .NET 8 SDK
- Visual Studio 2022（建议，需包含桌面开发组件）
- 现场对应硬件 SDK / 驱动（如相机、控制卡、HALCON 运行库等）

### 8.2 编译

```bash
dotnet restore ReeYin.sln
dotnet build ReeYin.sln -c Debug
```

### 8.3 运行

推荐先整解决方案构建（确保模块 DLL 已复制到 `Modules`），再运行：

```bash
dotnet run --project Shell/ReeYin_V.Shell.csproj
```

或直接启动输出目录中的可执行文件：

- `OutputExe/Debug/net8.0-windows/ReeYin.exe`

### 8.4 发布（示例）

```bash
dotnet publish Shell/ReeYin_V.Shell.csproj -c Release -r win-x64
```

## 9. 配置与数据

- SQLite 数据库：`Config/DB_ReeYin.db`
- JSON 配置：`Config/*.json`（通过 `ConfigKey` 分域）
- 本地缓存/受控数据：`C:\ProgramData\ReeYin\Project`（运行时创建隐藏目录）
- 日志：输出目录下 `logs`/`Logs`

## 10. 二次开发建议

- 新增模块时，建议遵循：
  1. 新建 Class Library（`net8.0-windows`）。
  2. 实现 `IModule`，在 `RegisterTypes` 中注册视图/服务。
  3. 若是节点工具，使用 `RegisterDialogAndMenu` 注入菜单。
  4. 配置 `PostBuild` 将 DLL 复制到 `Modules`。
  5. 如需可控加载，在数据库 `module_load_config` 增加规则。

---
