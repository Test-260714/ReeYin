# 数据源级点采集首尾去除数量设计

## 目标

将点采集“每组去除首尾数量”从数据分析页的全局输入移到数据源过滤设置弹窗中，并按每一行数据源单独保存和生效。弹窗应自适应内容高度，避免控件或按钮被遮挡。

## 方案

- 在 `DataAnalysisDataSourceOption` 上新增 `PointCollectionTrimCountPerSide`，默认值为 `2`，写入时归一化为不小于 `0`。
- 数据分析页移除顶部全局 `ModelParam.PointCollectionTrimCountPerSide` 输入。
- `DataAnalysisSourceFilterSettingViewModel` 在打开弹窗时读取当前数据源的首尾去除数量，在确定时写回该数据源。
- `DataAnalysisSourceFilterSettingView` 增加“点采集首尾去除数量”输入，并用 `MinWidth`、`MaxHeight`、`ScrollViewer` 和自动行高替代固定 `Width/Height`，保证小窗口或字体缩放时可滚动。
- 点采集聚合时，`OriginalDatas` 中与某个数据源 `OriginalDataValueName` 匹配的键使用该数据源自己的首尾去除数量；没有匹配数据源的键继续使用旧全局默认值 `2`，保持兼容。
- 标准片引用点和普通点使用同一套聚合规则，避免 CSV 与分析输入不一致。

## 测试

- 用临时反射测试验证两个数据源指向不同 `OriginalDataValueName` 且去除数量不同的时候，同一组原始数据会得到不同的平均结果。
- 构建 `Custom.WaferFlatnessMeasure.csproj` 验证 C# 与 XAML 编译通过。
