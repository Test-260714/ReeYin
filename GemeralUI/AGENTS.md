# GemeralUI 领域规则

本文件继承根级 [AGENTS.md](../AGENTS.md)，适用于 `GemeralUI/` 的通用业务控件。共享主题和基础控件的当前权威仍是 `Core/ReeYin_V.UI`，目录名不转移所有权。

## 契约与调查

- 修改公共控件、依赖属性、事件、Binding、模板部件和资源键前，定位所有 XAML/代码消费者及运行时依赖。
- `ReeYin.ChartShow` 当前引用 `Application/ReeYin_V.Share`、`Core/ReeYin_V.UI` 和 VTK/图表组件；变更必须检查数据契约、UI 线程、资源释放和运行时复制。
- 风险见 [CONTRIBUTING.md](../CONTRIBUTING.md#3-风险分类与快速通道)，消费者见 [变更影响地图](../docs/development/change-impact-map.md)，命令见 [构建与测试地图](../docs/development/build-and-test-map.md)。

## 升级与验证

- 公共控件、共享资源、数据/事件契约或运行时依赖变化至少 R2；纯单页内部视觉变化按实际影响判定。
- 构建修改项目和消费者，验证 Binding/命令错误、数据更新、线程、重复打开/关闭、DPI、长文本、主题和 Native/图表资源释放。
- 不得在 UI 中拥有硬件或数据库长生命周期资源；交付说明公共 UI 和运行时依赖兼容性。
