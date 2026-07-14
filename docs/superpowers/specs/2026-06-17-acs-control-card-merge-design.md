# ACS 控制卡测试/监控页面合并设计

## 目标
将独立状态监控窗口的能力合并进 `AcsControlCardTestView`，让 ACS 控制卡只需要打开一个综合调试窗口。

## 采用方案
采用方案 A：在 `AcsControlCardTestView` 的 `TabControl` 中新增“状态监控”页签，并通过 `AcsControlCardTestViewModel` 暴露一个 `Monitor` 子 ViewModel。配置页上的“ACS测试面板”和“状态监控”按钮都打开 `AcsControlCardTestView`；状态监控入口打开后默认选中监控页签。

## 影响范围
- `AcsControlCardTestView.xaml`：新增监控页签，复用监控页面的摘要卡片、刷新按钮、轴状态表格、状态文本。
- `AcsControlCardTestView.xaml.cs`：增加可选初始页签参数，关闭窗口时释放可释放的 DataContext。
- `AcsControlCardTestViewModel.cs`：持有并初始化 `AcsControlCardMonitorPanelViewModel`，实现 `IDisposable`。
- `AcsControlCardConfigViewModel.cs`：两个入口统一打开测试窗口，监控入口传入监控页签。

## 边界
删除独立状态监控页面文件；监控刷新和自动刷新逻辑由 `AcsControlCardMonitorPanelViewModel` 负责，并通过测试面板中的“状态监控”页签使用。

## 验证
先运行结构验证确认当前代码缺少合并点，再实现并运行同一验证通过；最后构建 ACS 项目。
