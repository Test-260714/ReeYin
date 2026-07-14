# CustomizedDemand 领域规则

本文件继承根级 [AGENTS.md](../AGENTS.md)，只增加客户/项目模块的跨领域约束。风险定义见 [CONTRIBUTING.md](../CONTRIBUTING.md#3-风险分类与快速通道)。

## 适用范围与职责

适用于 `CustomizedDemand/` 的客户模块、定制流程、UI、算法、Recipe/Output、方案数据和设备组合。使用 [变更影响地图](../docs/development/change-impact-map.md) 逐域调查，并从 [构建与测试地图](../docs/development/build-and-test-map.md) 选择项目与消费者。

## 修改前调查

- 定位模块入口、Model/ViewModel、方案/Recipe/Output、动态页面、缓存、算法、硬件服务、厂商 SDK 和释放路径。
- 识别客户/项目特有配置和旧方案兼容；禁止把相邻客户模块的行为未经验证地推广为公共规则。
- 跨 Core、Application、Tools、Hardware、Algorithm 或 UI 时同时应用每个受影响领域的契约和门禁。

## 升级条件

- 客户配置、持久化、Recipe/Output、模块身份、共享控件或算法契约变化至少 R2。
- 硬件命令、工艺/报警/权限/安全或发布配置变化至少 R3；不可逆真实操作为 R4。
- 真实客户/生产数据不得作为调试夹具；AI 不得修改生产数据或操作真机。

## 最低验证

- 构建修改项目、直接测试和可定位消费者；覆盖旧方案、缺失配置、重复打开/初始化/释放及跨节点隔离。
- 算法覆盖精度/边界，UI 覆盖导航/Binding，硬件优先使用模拟器覆盖失败、超时、停止和恢复。
- 交付按领域分别说明兼容、未验证、回滚和所需人类评审/授权。
