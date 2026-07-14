# Line Segment Raw And Filtered CSV Design

## Goal
线扫模式下，每条线段和最终汇总都同时导出未滤波数据与中值/平滑滤波后的数据。按用户确认，滤波后文件使用 `_Filtered` 后缀。

## Scope
- 点测模式保持现有导出逻辑。
- 线扫每条线段输出：`Line_001_PreDatas.csv`（未滤波）和 `Line_001_PreDatas_Filtered.csv`（滤波后）。
- 线扫最终汇总输出：`LineSegments_All_PreDatas.csv`（未滤波）和 `LineSegments_All_PreDatas_Filtered.csv`（滤波后）。
- 已有分析结果继续基于滤波后的 `PreDatas` 执行。
- 未启用中值/平滑滤波时仍输出两份，内容可相同，保证文件结构稳定。

## Approach
在停止采集时先从对齐后的 `MeasureData` 生成未滤波 `PreprocessDatasetModel`，再通过 `PreprocessDatasetFilter.Apply` 生成滤波副本。模型继续把滤波副本写入 `PreDatas` 供 UI、输出参数和算法使用，同时在线扫 CSV 会话中维护两套汇总缓存：原始未滤波和滤波后。存储服务新增可传入文件名后缀的保存方法，避免重复代码。

## Data Flow
1. 线扫停止采集后得到 `DataCollect`。
2. 生成 `rawPreDatas`，再生成 `filteredPreDatas`。
3. `PreDatas = filteredPreDatas`，保持现有 UI/算法行为。
4. 当前线段保存 raw 与 filtered 两份 CSV。
5. 会话完成时保存 raw 与 filtered 两份汇总 CSV；filtered 汇总继续用于后续线扫汇总分析。

## Error Handling
- 任一数据集为空时跳过对应 CSV 并记录日志。
- 汇总会话被清理时同时清理两套缓存。
- `IsSavePreDatasToCsv == false` 时两套 CSV 都不导出。
