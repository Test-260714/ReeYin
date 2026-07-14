# Line Segment Surface Analysis Design

## Goal
线扫全部线段汇总生成 `LineSegments_All_PreDatas.csv` 后，只按 SensorDataCollectionView 页面“数据分析”中勾选的算法，基于 CSV 的 `UpSurface` / `DownSurface` 两列计算上表面、下表面及两面成对指标。

## Scope
- 点测模式保持现有 `RunALGO()` 行为。
- 线扫模式只使用 `UpSurface` 和 `DownSurface` 两个核心表面列。
- 数据分析中未勾选的算法不计算、不追加结果。
- 计算结果追加到 `LineSegments_All_PreDatas.csv`，并更新模型中的分析结果和输出参数。

## Approach
在线扫汇总 CSV 保存完成后，调用新的汇总分析入口。该入口克隆汇总 `PreDatas`，复用现有过滤、平面度、平行度、TTV、THK、TIR、Warp 计算逻辑，但构造固定的上/下表面数据源和算法勾选快照，不依赖用户新增的数据源。这样能保持点测数据分析逻辑不变，同时补齐线扫 `IsPoint == false` 下原来跳过计算的问题。

## Data Flow
1. 每条线段继续按现有逻辑保存 `Line_###_PreDatas.csv`。
2. 最后一条线段完成后保存 `LineSegments_All_PreDatas.csv` 和 PLY。
3. 汇总 `PreDatas` 进入线扫分析入口。
4. 根据 `DataAnalysisAlgorithms.IsEnabled` 选择算法。
5. 使用 `UpSurface` / `DownSurface` 生成单面或双面点云。
6. 结果写入 `DataAnalysisResults`，追加到汇总 CSV，刷新输出参数。

## Error Handling
- 汇总点数为空时不计算。
- 单面算法有效点少于 3 点时记录“点数不足”。
- 双面算法任一表面少于 3 点或点数不匹配时记录“点数不足”。
- CSV 路径不存在时跳过追加并记录日志。

## Tests
- 新增测试证明线扫汇总 CSV 保存后会触发分析入口。
- 新增测试证明线扫汇总只使用 `UpSurface` / `DownSurface`，不使用其它数据源。
- 新增测试证明未勾选算法不执行。
