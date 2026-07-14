using Custom.KCJC.Models;
using Prism.Common;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Custom.KCJC.Models.KCJC0_Algorithm;

namespace Custom.KCJC.Views
{
    /// <summary>
    /// StatisticsTableChart.xaml 的交互逻辑
    /// </summary>
    public partial class StatisticsTableChart : UserControl
    {
        #region Fields
        public string ChartName { get; set; }

        private KCJC0_MeasureParam _mPara;
        private OtherConfigModel _judgeOtherConfig;
        private string _strFormat = "F3";

        private List<ScanRecord> _scanHistory = new();
        private int _grooveScanCounter = 0;
        private string _currentBatchNo = string.Empty;
        private static readonly string _uploadSnapshotRootPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KCJC", "UploadCache");
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _uploadFileLocks = new();
        private const int ConvexCsvSegmentCount = 2;
        private const int NormalCsvOldBaseFieldCount = 6;
        private const int NormalCsvBaseFieldCount = 11;
        private int _currentConvexCount = 0;
        private readonly List<ConvexSegmentSnapshot> _currentConvexSegments = new();
        public ObservableCollection<List<KCJC0_StatisticsTableDataModel>> DynamicDataSource { get; set; } = new ObservableCollection<List<KCJC0_StatisticsTableDataModel>>();
        public ObservableCollection<List<KCJC0_StatisticsTableDataModel>> ConvexDataSource { get; set; } = new ObservableCollection<List<KCJC0_StatisticsTableDataModel>>();

        #endregion

        #region Constructor
        public StatisticsTableChart()
        {
            InitializeComponent();

            //订阅
            PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Subscribe((pd) =>
            {
                try
                {
                    PrismProvider.Dispatcher.BeginInvoke(() =>
                    {
                        _mPara = new KCJC0_MeasureParam();

                        var result = pd.GetMemoryPara<KCJC0_MeasureResult>("KCJC0_MeasureResult", null);
                        if (result == null) return;

                        _mPara = pd.GetMemoryPara("KCJC0_MeasureParam", new KCJC0_MeasureParam());
                        _judgeOtherConfig = pd.GetMemoryPara("KCJC0_JudgeOtherConfig", new OtherConfigModel());
                        string SavePath = pd.GetMemoryPara("SaveDatasPath", "");
                        bool IsSaveDatas = pd.GetMemoryPara("IsSaveDatas", false);
                        bool isUploadPublicDisk = pd.GetMemoryPara("IsUploadPublicDisk", false);
                        string publicDiskPath = pd.GetMemoryPara("PublicDiskPath", "");
                        bool isUploadSummaryPublicDisk = pd.GetMemoryPara("IsUploadSummaryPublicDisk", false);
                        string summaryPublicDiskPath = pd.GetMemoryPara("SummaryPublicDiskPath", "");
                        string productModelName = pd.GetMemoryPara("ProductModelName", "");
                        string workpieceName = pd.GetMemoryPara("WorkpieceName", "");
                        string batchNo = pd.GetMemoryPara("BatchNo", "");
                        string workshop = pd.GetMemoryPara("Workshop", "");
                        string processName = pd.GetMemoryPara("ProcessName", "");
                        string reportType = pd.GetMemoryPara("ReportType", "");
                        string shiftName = pd.GetMemoryPara("ShiftName", "");
                        string machineNo = pd.GetMemoryPara("MachineNo", "");
                        string tester = pd.GetMemoryPara("Tester", "");
                        ObservableCollection<SummaryTestItemConfig> summaryTestItems = pd.GetMemoryPara("SummaryTestItems", new ObservableCollection<SummaryTestItemConfig>());
                        int convexActualSegmentIndex = pd.GetMemoryPara("KCJC0_ConvexActualSegmentIndex", 1);
                        int convexDisplaySegmentCount = pd.GetMemoryPara("KCJC0_ConvexDisplaySegmentCount", 1);
                        int convexActualLoopCount = pd.GetMemoryPara("KCJC0_ConvexActualLoopCount", 1);
                        bool convexRunCompleted = pd.GetMemoryPara("KCJC0_ConvexRunCompleted", true);
                        string currentBatchNo = string.IsNullOrWhiteSpace(batchNo) ? string.Empty : batchNo.Trim();
                        if (!string.Equals(_currentBatchNo, currentBatchNo, StringComparison.Ordinal))
                        {
                            _grooveScanCounter = 0;
                            _currentBatchNo = currentBatchNo;
                        }
                        int currentGrooveScanCount = convexRunCompleted ? _grooveScanCounter + 1 : _grooveScanCounter;
                        if (result == null) return ;
                        _currentConvexCount = result.ConvexResultsList.Count;
                        UpdateDynamicColumns();
                        List<KCJC0_PartitionResult> partitionResults = result.PartitionResults;

                        //各分区值
                        for (int f = 0; f < _mPara.SamplePointNum; f++)
                        {
                            List<string> data = new();
                            if (partitionResults.Count > f)
                            {
                                //间距
                                for (int i = 0; i < _mPara.EtchingLineNumMax - 1; i++)
                                {
                                    if (partitionResults[f].EtchingPointDistRealList.Length > i)
                                        data.Add(partitionResults[f].EtchingPointDistRealList[i].ToString(_strFormat));
                                    else
                                        data.Add("");
                                }
                                //槽深
                                for (int i = 0; i < _mPara.EtchingLineNumMax; i++)
                                {
                                    if (partitionResults[f].EtchingLineDepthList.Length > i)
                                        data.Add(partitionResults[f].EtchingLineDepthList[i].ToString(_strFormat));
                                    else
                                        data.Add("");
                                }
                                //槽宽
                                for (int i = 0; i < _mPara.EtchingLineNumMax; i++)
                                {
                                    if (partitionResults[f].EtchingLineWidthRealList.Length > i)
                                        data.Add(partitionResults[f].EtchingLineWidthRealList[i].ToString(_strFormat));
                                    else
                                        data.Add("");
                                }
                            }
                            else
                            {
                                for (int i = 0; i < _mPara.EtchingLineNumMax - 1; i++)
                                    data.Add("");
                                for (int i = 0; i < _mPara.EtchingLineNumMax; i++)
                                    data.Add("");
                                for (int i = 0; i < _mPara.EtchingLineNumMax; i++)
                                    data.Add("");
                            }

                            if(IsSaveDatas)
                                UpdateDynamicRow($"F{f + 1}", data);
                        }

                        //纵向计算
                        //平均值
                        List<double> pdAvgData = result.EtchingPointDistRealMeanList.ToList(); //间距
                        List<double> ldAvgData = result.EtchingLineDepthMeanList.ToList();//槽深
                        List<double> lwAvgData = result.EtchingLineWidthRealMeanList.ToList(); //槽宽

                        //最大值
                        List<double> pdMaxData = result.EtchingPointDistRealMaxList.ToList(); //间距
                        List<double> ldMaxData = result.EtchingLineDepthMaxList.ToList(); //槽深
                        List<double> lwMaxData = result.EtchingLineWidthRealMaxList.ToList(); //槽宽

                        //最小值
                        List<double> pdMinData = result.EtchingPointDistRealMinList.ToList(); //间距
                        List<double> ldMinData = result.EtchingLineDepthMinList.ToList(); //槽深
                        List<double> lwMinData = result.EtchingLineWidthRealMinList.ToList(); //槽宽

                        //标准差
                        List<double> pdStdData = result.EtchingPointDistRealMaxList.Select(x => x - result.EtchingPointDistRealMinList[result.EtchingPointDistRealMaxList.ToList().IndexOf(x)]).ToList(); //间距
                        List<double> ldStdData = result.EtchingLineDepthMaxList.Select(x => x - result.EtchingLineDepthMinList[result.EtchingLineDepthMaxList.ToList().IndexOf(x)]).ToList(); //槽深
                        List<double> lwStdData = result.EtchingLineWidthRealMaxList.Select(x => x - result.EtchingLineWidthRealMinList[result.EtchingLineWidthRealMaxList.ToList().IndexOf(x)]).ToList(); //槽宽

                        //纵向计算
                        //平均值
                        UpdateDynamicRow($"平均值", pdAvgData.Concat(ldAvgData).Concat(lwAvgData).Select(x => x.ToString(_strFormat)).ToList());
                        //最大值
                        UpdateDynamicRow("最大值", pdMaxData.Concat(ldMaxData).Concat(lwMaxData).Select(x => x.ToString(_strFormat)).ToList());
                        //最小值
                        UpdateDynamicRow("最小值", pdMinData.Concat(ldMinData).Concat(lwMinData).Select(x => x.ToString(_strFormat)).ToList());
                        //标准差
                        UpdateDynamicRow("极差", pdStdData.Concat(ldStdData).Concat(lwStdData).Select(x => x.ToString(_strFormat)).ToList());

                        if (_mPara.AlgorithmType == 1 || _mPara.AlgorithmType == 2 || _currentConvexSegments.Count > 0)
                        {
                            HandleConvexResult(result.ConvexResultsList, convexActualSegmentIndex, convexDisplaySegmentCount, convexRunCompleted, convexActualLoopCount, IsSaveDatas, SavePath, isUploadPublicDisk, publicDiskPath, isUploadSummaryPublicDisk, summaryPublicDiskPath, productModelName, batchNo, workshop, processName, reportType, workpieceName, shiftName, machineNo, tester, summaryTestItems);
                        }

                        PrismProvider.Dispatcher.BeginInvoke(() =>
                        {
                            AdjustColumnWidths();
                        });

                        #region 写入CSV
                        // ✅ 新增：保存本次扫描原始数据到历史记录
                        var record = new ScanRecord
                        {
                            ScanTime = DateTime.Now,
                            ScanCount = 0
                        };

                        for (int f = 0; f < _mPara.SamplePointNum && f < partitionResults.Count; f++)
                        {
                            string fLabel = $"F{f + 1}";

                            // 间距
                            for (int i = 0; i < _mPara.EtchingLineNumMax - 1; i++)
                            {
                                if (i < partitionResults[f].EtchingPointDistRealList.Length)
                                {
                                    record.Spacings[$"{fLabel}-L{i + 1}-L{i + 2}"] = partitionResults[f].EtchingPointDistRealList[i];
                                }
                            }

                            // 槽深
                            for (int i = 0; i < _mPara.EtchingLineNumMax; i++)
                            {
                                if (i < partitionResults[f].EtchingLineDepthList.Length)
                                {
                                    record.Depths[$"{fLabel}-L{i + 1}"] = partitionResults[f].EtchingLineDepthList[i];
                                }
                            }

                            // 槽宽
                            for (int i = 0; i < _mPara.EtchingLineNumMax; i++)
                            {
                                if (i < partitionResults[f].EtchingLineWidthRealList.Length)
                                {
                                    record.Widths[$"{fLabel}-L{i + 1}"] = partitionResults[f].EtchingLineWidthRealList[i];
                                }
                            }
                        }

                        if (_mPara.AlgorithmType == 0 || _mPara.AlgorithmType == 2)
                        {
                            _scanHistory.Add(record);
                            if (convexRunCompleted)
                            {
                                _grooveScanCounter = currentGrooveScanCount;
                                foreach (var item in _scanHistory)
                                {
                                    item.ScanCount = _grooveScanCounter;
                                }
                            }

                            if(IsSaveDatas && !string.IsNullOrWhiteSpace(SavePath) && convexRunCompleted)
                            {
                                string dataFolder = System.IO.Path.Combine(SavePath, $"datas-{record.ScanTime:yyyyMMdd}");
                                if (!Directory.Exists(dataFolder))
                                {
                                    Directory.CreateDirectory(dataFolder);
                                }

                                string grooveCsvPath = System.IO.Path.Combine(dataFolder, "刻槽.csv");
                                ExportScanHistoryToCsv(grooveCsvPath, productModelName, batchNo, workshop, processName, reportType, workpieceName, shiftName, machineNo, tester);
                                QueueCsvUpload(grooveCsvPath, SavePath, isUploadPublicDisk, publicDiskPath);

                                string grooveSummaryCsvPath = System.IO.Path.Combine(dataFolder, "刻槽汇总.csv");
                                ExportGrooveSummaryCsv(grooveSummaryCsvPath, reportType, workpieceName, shiftName, workshop, processName, machineNo, tester, summaryTestItems);
                                QueueCsvUpload(grooveSummaryCsvPath, SavePath, isUploadSummaryPublicDisk, summaryPublicDiskPath);
                            }

                            if (convexRunCompleted)
                            {
                                _scanHistory.Clear();
                            }
                        }
                        #endregion

                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.StackTrace}");
                }

            }, ThreadOption.UIThread);

            PrismProvider.EventAggregator.GetEvent<WorkStatusChangeEvent>().Subscribe(status =>
            {
                if (status != ReeYin_V.Core.Services.WorkStatus.WorkStatus.Error)
                    return;

                PrismProvider.Dispatcher.BeginInvoke(() =>
                {
                    ClearConvexRuntimeCache();
                });
            }, ThreadOption.UIThread);
        }
        #endregion

        #region Methods
        private void ClearConvexRuntimeCache()
        {
            _currentConvexCount = 0;
            _currentConvexSegments.Clear();
            ConvexDataSource.Clear();
            ConvexDataGrid.Columns.Clear();
        }

        public void UpdateDynamicColumns()
        {
            PrismProvider.Dispatcher.Invoke(() =>
            {
                DynamicDataGrid.AutoGenerateColumns = true;
                DynamicDataGrid.Columns.Clear();
                DynamicDataSource.Clear();

                List<string> headerText = new List<string>();
                headerText.Add("分区");
                //间距
                for (int i = 0; i < _mPara.EtchingLineNumMax - 1; i++)
                    headerText.Add($"L{i + 1}-L{i + 2}间距");
                //槽深
                for (int i = 0; i < _mPara.EtchingLineNumMax; i++)
                    headerText.Add($"L{i + 1}槽深");
                //槽宽
                for (int i = 0; i < _mPara.EtchingLineNumMax; i++)
                    headerText.Add($"L{i + 1}槽宽");

                for (int columnIndex = 0; columnIndex < headerText.Count; columnIndex++)
                {
                    DynamicDataGrid.Columns.Add(CreateDisplayColumn(headerText[columnIndex], columnIndex));
                }
            });
        }

        public void UpdateDynamicRow(string name, List<string> datas)
        {
            PrismProvider.Dispatcher.Invoke(() =>
            {
                bool canJudge = name.StartsWith("F", StringComparison.OrdinalIgnoreCase);
                List<KCJC0_StatisticsTableDataModel> dataModels = new List<KCJC0_StatisticsTableDataModel>();
                dataModels.Add(new KCJC0_StatisticsTableDataModel
                {
                    IsHeader = true,
                    DisplayStr = name,
                    ForegroundBrush = Brushes.Black
                });
                for (int i = 0; i < datas.Count; i++)
                {
                    dataModels.Add(CreateDynamicCell(datas[i], i, canJudge));
                }

                DynamicDataSource.Add(dataModels);
            });
        }

        public void UpdateConvexColumns(int convexCount)
        {
            PrismProvider.Dispatcher.Invoke(() =>
            {
                ConvexDataGrid.Columns.Clear();
                ConvexDataSource.Clear();

                List<string> headerText = new List<string> { "测量值", "最大值", "最小值", "极差", "均值" };
                for (int i = 0; i < convexCount; i++)
                    headerText.Add($"点{i + 1}");

                for (int columnIndex = 0; columnIndex < headerText.Count; columnIndex++)
                {
                    ConvexDataGrid.Columns.Add(CreateDisplayColumn(headerText[columnIndex], columnIndex));
                }

                ConvexDataGrid.ItemsSource = ConvexDataSource;
            });
        }

        public void UpdateConvexRow(string name, List<string> datas, bool isDepthRow)
        {
            PrismProvider.Dispatcher.Invoke(() =>
            {
                List<KCJC0_StatisticsTableDataModel> dataModels = new List<KCJC0_StatisticsTableDataModel>();
                dataModels.Add(new KCJC0_StatisticsTableDataModel
                {
                    IsHeader = true,
                    DisplayStr = name,
                    ForegroundBrush = Brushes.Black
                });
                List<double> validValues = datas
                    .Select(ParseConvexValue)
                    .Where(item => item.HasValue)
                    .Select(item => item!.Value)
                    .ToList();

                double? maxValue = validValues.Count > 0 ? validValues.Max() : (double?)null;
                double? minValue = validValues.Count > 0 ? validValues.Min() : (double?)null;
                dataModels.Add(CreateSummaryCell(FormatSummaryValue(maxValue)));
                dataModels.Add(CreateSummaryCell(FormatSummaryValue(minValue)));
                dataModels.Add(CreateSummaryCell(FormatSummaryValue(maxValue.HasValue && minValue.HasValue ? maxValue.Value - minValue.Value : (double?)null)));
                dataModels.Add(CreateSummaryCell(FormatSummaryValue(validValues.Count > 0 ? validValues.Average() : (double?)null)));

                foreach (var item in datas)
                {
                    dataModels.Add(CreateConvexCell(item, isDepthRow));
                }
                ConvexDataSource.Add(dataModels);
            });
        }

        // 统计表列是动态生成的，模板列最稳，文本和颜色都能一起绑。
        private static DataGridTemplateColumn CreateDisplayColumn(string header, int columnIndex)
        {
            FrameworkElementFactory textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetBinding(TextBlock.TextProperty, new Binding($"[{columnIndex}].DisplayStr"));
            textFactory.SetBinding(TextBlock.ForegroundProperty, new Binding($"[{columnIndex}].ForegroundBrush"));
            textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

            DataTemplate cellTemplate = new DataTemplate
            {
                VisualTree = textFactory
            };

            return new DataGridTemplateColumn
            {
                Header = header,
                CellTemplate = cellTemplate,
                Width = DataGridLength.Auto,
            };
        }

        private KCJC0_StatisticsTableDataModel CreateDynamicCell(string item, int dataIndex, bool canJudge)
        {
            if (!canJudge)
            {
                return CreateJudgeCell(item, 0, 0, true);
            }

            int spacingCount = Math.Max(0, _mPara.EtchingLineNumMax - 1);
            if (dataIndex < spacingCount)
            {
                return CreateJudgeCell(item, _judgeOtherConfig.EtchingPointDistLowerLimit, _judgeOtherConfig.EtchingPointDistUpperLimit, true);
            }

            if (dataIndex < spacingCount + _mPara.EtchingLineNumMax)
            {
                return CreateJudgeCell(item, _judgeOtherConfig.EtchingLineDepthLowerLimit, _judgeOtherConfig.EtchingLineDepthUpperLimit, true);
            }

            return CreateJudgeCell(item, _judgeOtherConfig.EtchingLineWidthLowerLimit, _judgeOtherConfig.EtchingLineWidthUpperLimit, true);
        }

        private KCJC0_StatisticsTableDataModel CreateConvexCell(string item, bool isDepthRow)
        {
            if (isDepthRow)
            {
                return CreateJudgeCell(item, _judgeOtherConfig.PointHeightLowerLimit, _judgeOtherConfig.PointHeightUpperLimit, false);
            }

            return CreateJudgeCell(item, _judgeOtherConfig.PointDiameterLowerLimit, _judgeOtherConfig.PointDiameterUpperLimit, false);
        }

        private KCJC0_StatisticsTableDataModel CreateSummaryCell(string item)
        {
            KCJC0_StatisticsTableDataModel dataModel = new KCJC0_StatisticsTableDataModel
            {
                IsHeader = false,
                DisplayStr = "null",
                Value = -999,
                ForegroundBrush = Brushes.Black,
                OkNgValue = string.Empty,
                IsNg = false
            };

            if (string.IsNullOrWhiteSpace(item))
                return dataModel;

            dataModel.DisplayStr = item;
            if (double.TryParse(item, out double value))
            {
                dataModel.Value = value;
            }

            return dataModel;
        }

        private void HandleConvexResult(List<KCJC0_ConvexConcaveResult> convexResults, int actualSegmentIndex, int displaySegmentCount, bool runCompleted, int scanCount, bool isSaveDatas, string savePath, bool isUploadPublicDisk, string publicDiskPath, bool isUploadSummaryPublicDisk, string summaryPublicDiskPath, string productModelName, string batchNo, string workshop, string processName, string reportType, string workpieceName, string shiftName, string machineNo, string tester, ObservableCollection<SummaryTestItemConfig> summaryTestItems)
        {
            int finalDisplaySegmentCount = Math.Min(Math.Max(displaySegmentCount, 1), 3);
            int finalActualSegmentIndex = Math.Max(actualSegmentIndex, 1);

            if (finalActualSegmentIndex == 1)
            {
                _currentConvexSegments.Clear();
            }

            if (finalActualSegmentIndex <= finalDisplaySegmentCount)
            {
                ConvexSegmentSnapshot snapshot = CreateConvexSegmentSnapshot(finalActualSegmentIndex, convexResults);
                int existingIndex = _currentConvexSegments.FindIndex(item => item.SegmentIndex == finalActualSegmentIndex);
                if (existingIndex >= 0)
                {
                    _currentConvexSegments[existingIndex] = snapshot;
                }
                else
                {
                    _currentConvexSegments.Add(snapshot);
                }
            }

            if (!runCompleted)
                return;

            int pointCount = Math.Max(_currentConvexSegments.Select(item => Math.Max(item.DepthValues.Count, item.DiameterValues.Count)).DefaultIfEmpty(0).Max(), 1);
            UpdateConvexColumns(pointCount);

            for (int segmentIndex = 1; segmentIndex <= finalDisplaySegmentCount; segmentIndex++)
            {
                ConvexSegmentSnapshot snapshot = _currentConvexSegments.FirstOrDefault(item => item.SegmentIndex == segmentIndex) ?? new ConvexSegmentSnapshot { SegmentIndex = segmentIndex };
                UpdateConvexRow($"区{segmentIndex}深度", BuildPointDisplayValues(snapshot.DepthValues, pointCount), true);
                UpdateConvexRow($"区{segmentIndex}直径", BuildPointDisplayValues(snapshot.DiameterValues, pointCount), false);
            }

            UpdateConvexTotalRow("总深度", _currentConvexSegments.SelectMany(item => item.DepthValues), pointCount, true);
            UpdateConvexTotalRow("总直径", _currentConvexSegments.SelectMany(item => item.DiameterValues), pointCount, false);

            if (isSaveDatas && !string.IsNullOrWhiteSpace(savePath) && (_mPara.AlgorithmType == 1 || _mPara.AlgorithmType == 2))
            {
                DateTime exportTime = DateTime.Now;
                string dataFolder = System.IO.Path.Combine(savePath, $"datas-{exportTime:yyyyMMdd}");
                if (!Directory.Exists(dataFolder))
                {
                    Directory.CreateDirectory(dataFolder);
                }

                string convexCsvPath = System.IO.Path.Combine(dataFolder, "刻点.csv");
                ExportConvexCsv(convexCsvPath, exportTime, scanCount, productModelName, batchNo, workshop, processName, reportType, workpieceName, shiftName, machineNo, tester, _currentConvexSegments);
                QueueCsvUpload(convexCsvPath, savePath, isUploadPublicDisk, publicDiskPath);

                string convexSummaryCsvPath = System.IO.Path.Combine(dataFolder, "压花汇总.csv");
                ExportConvexSummaryCsv(convexSummaryCsvPath, exportTime, reportType, workpieceName, shiftName, workshop, processName, machineNo, tester, summaryTestItems, _currentConvexSegments);
                QueueCsvUpload(convexSummaryCsvPath, savePath, isUploadSummaryPublicDisk, summaryPublicDiskPath);
            }

            _currentConvexSegments.Clear();
        }

        private ConvexSegmentSnapshot CreateConvexSegmentSnapshot(int segmentIndex, List<KCJC0_ConvexConcaveResult> convexResults)
        {
            ConvexSegmentSnapshot snapshot = new ConvexSegmentSnapshot
            {
                SegmentIndex = segmentIndex
            };

            if (convexResults == null)
                return snapshot;

            foreach (var item in convexResults)
            {
                snapshot.DepthValues.Add(IsInvalidCsvValue(item.HeightDiff) || item.HeightDiff == 0 ? null : item.HeightDiff);
                double diameter = item.Radius > 0 && !IsInvalidCsvValue(item.Radius) ? item.Radius * 2 : double.NaN;
                snapshot.DiameterValues.Add(double.IsNaN(diameter) || IsInvalidCsvValue(diameter) ? null : diameter);
            }

            return snapshot;
        }

        private List<string> BuildPointDisplayValues(List<double?> values, int pointCount)
        {
            List<string> result = new List<string>();
            for (int index = 0; index < pointCount; index++)
            {
                double? value = index < values.Count ? values[index] : null;
                result.Add(FormatSummaryValue(value));
            }

            return result;
        }

        private void UpdateConvexTotalRow(string name, IEnumerable<double?> values, int pointCount, bool isDepthRow)
        {
            PrismProvider.Dispatcher.Invoke(() =>
            {
                List<KCJC0_StatisticsTableDataModel> dataModels = new List<KCJC0_StatisticsTableDataModel>();
                dataModels.Add(new KCJC0_StatisticsTableDataModel
                {
                    IsHeader = true,
                    DisplayStr = name,
                    ForegroundBrush = Brushes.Black
                });

                List<double> validValues = values
                    .Where(item => item.HasValue && !IsInvalidCsvValue(item.Value))
                    .Select(item => item!.Value)
                    .ToList();

                double? maxValue = validValues.Count > 0 ? validValues.Max() : (double?)null;
                double? minValue = validValues.Count > 0 ? validValues.Min() : (double?)null;
                dataModels.Add(CreateConvexCell(FormatSummaryValue(maxValue), isDepthRow));
                dataModels.Add(CreateConvexCell(FormatSummaryValue(minValue), isDepthRow));
                dataModels.Add(CreateSummaryCell(FormatSummaryValue(maxValue.HasValue && minValue.HasValue ? maxValue.Value - minValue.Value : (double?)null)));
                dataModels.Add(CreateConvexCell(FormatSummaryValue(validValues.Count > 0 ? validValues.Average() : (double?)null), isDepthRow));

                for (int index = 0; index < pointCount; index++)
                {
                    dataModels.Add(CreateSummaryCell(string.Empty));
                }

                ConvexDataSource.Add(dataModels);
            });
        }

        private double? ParseConvexValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (!double.TryParse(value, out double parsedValue))
                return null;

            return IsInvalidCsvValue(parsedValue) ? null : parsedValue;
        }

        private string FormatSummaryValue(double? value)
        {
            if (!value.HasValue || IsInvalidCsvValue(value.Value))
                return string.Empty;

            return value.Value.ToString(_strFormat);
        }

        private KCJC0_StatisticsTableDataModel CreateJudgeCell(string item, double lowerLimit, double upperLimit, bool isGrooveValue)
        {
            KCJC0_StatisticsTableDataModel dataModel = new KCJC0_StatisticsTableDataModel
            {
                IsHeader = false,
                DisplayStr = "null",
                Value = -999,
                ForegroundBrush = Brushes.Black,
                OkNgValue = string.Empty
            };

            if (isGrooveValue)
            {
                dataModel.SDownLimit = lowerLimit;
                dataModel.SUpLimit = upperLimit;
            }
            else
            {
                dataModel.CDownLimit = lowerLimit;
                dataModel.CUpLimit = upperLimit;
            }

            if (string.IsNullOrWhiteSpace(item))
                return dataModel;

            dataModel.DisplayStr = item;
            if (!double.TryParse(item, out double value))
                return dataModel;

            dataModel.Value = value;
            // -1 是当前项目里的无效占位值，只展示，不参与 OK/NG 判定。
            if (Math.Abs(value + 1d) < 0.000001d)
                return dataModel;

            bool canJudge = upperLimit > lowerLimit;
            bool isNg = canJudge && (value < lowerLimit || value > upperLimit);
            dataModel.IsNg = isNg;
            dataModel.OkNgValue = canJudge ? (isNg ? "NG" : "OK") : string.Empty;
            dataModel.ForegroundBrush = isNg ? Brushes.Red : Brushes.Black;
            return dataModel;
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is childItem item)
                    return item;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }

            return null;
        }

        private async void AdjustColumnWidths()
        {
            DynamicDataGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            foreach (var column in DynamicDataGrid.Columns)
            {
                column.Width = DataGridLength.Auto; // Use Auto for columns
            }
            await Task.Delay(10);

            double totalColumnWidth = 0;
            foreach (var column in DynamicDataGrid.Columns)
            {
                totalColumnWidth += column.ActualWidth;
            }

            double dataGridWidth = DynamicDataGrid.ActualWidth;

            if (totalColumnWidth <= dataGridWidth)
            {
                DynamicDataGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled; // Disable scroll bar if columns fit

                foreach (var column in DynamicDataGrid.Columns)
                {
                    column.Width = new DataGridLength(1, DataGridLengthUnitType.Star); // Stretch the columns
                }
            }
        }


        #endregion

        #region 导出CSV文件
        private static string EscapeCsvField(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            // 如果字段包含特殊字符：逗号、双引号、换行、回车
            if (input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r'))
            {
                // 将所有 " 替换为 ""（CSV 标准转义方式）
                // 然后用 " 包裹整个字段
                return "\"" + input.Replace("\"", "\"\"") + "\"";
            }

            return input;
        }


        public void ExportScanHistoryToCsv(string filePath, string productModelName, string batchNo, string workshop, string processName, string reportType, string workpieceName, string shiftName, string machineNo, string tester)
        {
            if (!_scanHistory.Any() || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            int currentPartitionCount = _mPara.SamplePointNum;
            int currentLineCount = _mPara.EtchingLineNumMax;
            CloseOpenedCsvIfNeeded(filePath);
            bool fileExists = File.Exists(filePath);
            (int finalPartitionCount, int finalLineCount) = EnsureGrooveCsvColumns(filePath, currentPartitionCount, currentLineCount);
            List<string> headers = BuildGrooveCsvHeaders(finalPartitionCount, finalLineCount);

            List<string> csvLines = new List<string>();

            foreach (var record in _scanHistory)
            {
                var row = new List<string>
                {
                    record.ScanTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    record.ScanCount.ToString(),
                    NormalizeCsvText(productModelName),
                    NormalizeCsvText(batchNo),
                    NormalizeCsvText(workshop),
                    NormalizeCsvText(processName),
                    NormalizeCsvText(reportType),
                    NormalizeCsvText(workpieceName),
                    NormalizeCsvText(shiftName),
                    NormalizeCsvText(machineNo),
                    NormalizeCsvText(tester)
                };

                for (int lineIndex = 1; lineIndex <= finalLineCount; lineIndex++)
                {
                    List<double?> values = GetGrooveValues(record.Depths, $"L{lineIndex}", finalPartitionCount);
                    row.Add(GetNgPartitions(values, _judgeOtherConfig.EtchingLineDepthLowerLimit, _judgeOtherConfig.EtchingLineDepthUpperLimit));
                    row.AddRange(values.Select(FormatCsvNumber));
                    row.Add(GetAverageCsvNumber(values));
                }

                for (int lineIndex = 1; lineIndex <= finalLineCount; lineIndex++)
                {
                    List<double?> values = GetGrooveValues(record.Widths, $"L{lineIndex}", finalPartitionCount);
                    row.Add(GetNgPartitions(values, _judgeOtherConfig.EtchingLineWidthLowerLimit, _judgeOtherConfig.EtchingLineWidthUpperLimit));
                    row.AddRange(values.Select(FormatCsvNumber));
                    row.Add(GetAverageCsvNumber(values));
                }

                for (int lineIndex = 1; lineIndex < finalLineCount; lineIndex++)
                {
                    List<double?> values = GetGrooveValues(record.Spacings, $"L{lineIndex}-L{lineIndex + 1}", finalPartitionCount);
                    row.Add(GetNgPartitions(values, _judgeOtherConfig.EtchingPointDistLowerLimit, _judgeOtherConfig.EtchingPointDistUpperLimit));
                    row.AddRange(values.Select(FormatCsvNumber));
                    row.Add(GetAverageCsvNumber(values));
                }

                PadCsvFields(row, headers.Count);
                csvLines.Add(BuildCsvLine(row));
            }

            AppendCsvLines(filePath, !fileExists ? BuildCsvLine(headers) : null, csvLines);
        }

        private void ExportConvexCsv(string filePath, DateTime scanTime, int scanCount, string productModelName, string batchNo, string workshop, string processName, string reportType, string workpieceName, string shiftName, string machineNo, string tester, List<ConvexSegmentSnapshot> convexSegments)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            int pointCount = Math.Max(convexSegments.Select(item => Math.Max(item.DepthValues.Count, item.DiameterValues.Count)).DefaultIfEmpty(0).Max(), 1);
            CloseOpenedCsvIfNeeded(filePath);
            bool fileExists = File.Exists(filePath);
            int finalPointCount = EnsureConvexCsvColumns(filePath, pointCount);
            List<string> headers = BuildConvexCsvHeaders(finalPointCount);

            List<string> row = new List<string>
            {
                scanTime.ToString("yyyy-MM-dd HH:mm:ss"),
                NormalizeCsvText(productModelName),
                NormalizeCsvText(batchNo),
                scanCount.ToString(),
                NormalizeCsvText(workshop),
                NormalizeCsvText(processName),
                NormalizeCsvText(reportType),
                NormalizeCsvText(workpieceName),
                NormalizeCsvText(shiftName),
                NormalizeCsvText(machineNo),
                NormalizeCsvText(tester)
            };

            AppendConvexOverallSummary(row, convexSegments, true);

            for (int segmentIndex = 1; segmentIndex <= ConvexCsvSegmentCount; segmentIndex++)
            {
                ConvexSegmentSnapshot snapshot = convexSegments.FirstOrDefault(item => item.SegmentIndex == segmentIndex);
                AppendConvexMetricSummary(row, snapshot?.DepthValues, finalPointCount);
            }

            for (int segmentIndex = 1; segmentIndex <= ConvexCsvSegmentCount; segmentIndex++)
            {
                ConvexSegmentSnapshot snapshot = convexSegments.FirstOrDefault(item => item.SegmentIndex == segmentIndex);
                AppendConvexMetricPoints(row, snapshot?.DepthValues, finalPointCount);
            }

            AppendConvexOverallSummary(row, convexSegments, false);

            for (int segmentIndex = 1; segmentIndex <= ConvexCsvSegmentCount; segmentIndex++)
            {
                ConvexSegmentSnapshot snapshot = convexSegments.FirstOrDefault(item => item.SegmentIndex == segmentIndex);
                AppendConvexMetricSummary(row, snapshot?.DiameterValues, finalPointCount);
            }

            for (int segmentIndex = 1; segmentIndex <= ConvexCsvSegmentCount; segmentIndex++)
            {
                ConvexSegmentSnapshot snapshot = convexSegments.FirstOrDefault(item => item.SegmentIndex == segmentIndex);
                AppendConvexMetricPoints(row, snapshot?.DiameterValues, finalPointCount);
            }

            PadCsvFields(row, headers.Count);
            AppendCsvLines(filePath, !fileExists ? BuildCsvLine(headers) : null, new[] { BuildCsvLine(row) });
        }

        private void ExportGrooveSummaryCsv(string filePath, string reportType, string productModelName, string shiftName, string workshop, string processName, string machineNo, string tester, ObservableCollection<SummaryTestItemConfig> summaryTestItems)
        {
            if (!_scanHistory.Any() || string.IsNullOrWhiteSpace(filePath))
                return;

            CloseOpenedCsvIfNeeded(filePath);
            bool fileExists = File.Exists(filePath);
            int sequence = GetNextSummarySequence(filePath);
            List<string> csvLines = new List<string>();

            foreach (var record in _scanHistory)
            {
                List<(string ItemName, double? Value)> metrics = new List<(string ItemName, double? Value)>();
                for (int lineIndex = 1; lineIndex <= _mPara.EtchingLineNumMax; lineIndex++)
                {
                    List<double> values = GetValidSummaryValues(GetGrooveValues(record.Depths, $"L{lineIndex}", _mPara.SamplePointNum));
                    AppendMetricGroup(metrics, $"L{lineIndex}槽孔深度", values);
                }

                for (int lineIndex = 1; lineIndex <= _mPara.EtchingLineNumMax; lineIndex++)
                {
                    List<double> values = GetValidSummaryValues(GetGrooveValues(record.Widths, $"L{lineIndex}", _mPara.SamplePointNum));
                    AppendMetricGroup(metrics, $"L{lineIndex}槽孔宽度", values);
                }

                for (int lineIndex = 1; lineIndex < _mPara.EtchingLineNumMax; lineIndex++)
                {
                    List<double> values = GetValidSummaryValues(GetGrooveValues(record.Spacings, $"L{lineIndex}-L{lineIndex + 1}", _mPara.SamplePointNum));
                    AppendMetricGroup(metrics, $"L{lineIndex}-L{lineIndex + 1}槽孔间距", values);
                }

                if (AppendSummaryMetricGroup(csvLines, sequence, "刻槽", metrics, reportType, productModelName, shiftName, workshop, processName, machineNo, tester, record.ScanTime, summaryTestItems))
                    sequence++;
            }

            AppendCsvLines(filePath, !fileExists ? BuildCsvLine(BuildSummaryCsvHeaders()) : null, csvLines);
        }

        private void ExportConvexSummaryCsv(string filePath, DateTime scanTime, string reportType, string productModelName, string shiftName, string workshop, string processName, string machineNo, string tester, ObservableCollection<SummaryTestItemConfig> summaryTestItems, List<ConvexSegmentSnapshot> convexSegments)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            CloseOpenedCsvIfNeeded(filePath);
            bool fileExists = File.Exists(filePath);
            int sequence = GetNextSummarySequence(filePath);
            List<string> csvLines = new List<string>();
            List<double> depthValues1 = GetValidSummaryValues(convexSegments.FirstOrDefault(item => item.SegmentIndex == 1)?.DepthValues ?? Enumerable.Empty<double?>());
            List<double> depthValues2 = GetValidSummaryValues(convexSegments.FirstOrDefault(item => item.SegmentIndex == 2)?.DepthValues ?? Enumerable.Empty<double?>());
            List<double> depthValues = depthValues1.Concat(depthValues2).ToList();
            double? depthAverage1 = GetAverageSummaryValue(depthValues1);
            double? depthAverage2 = GetAverageSummaryValue(depthValues2);

            List<(string ItemName, double? Value)> metrics = new List<(string ItemName, double? Value)>
            {
                ("压花高度均值1", depthAverage1),
                ("压花高度均值2", depthAverage2),
                ("压花高度均值极差", GetAbsRangeSummaryValue(depthAverage1, depthAverage2)),
                ("压花高度极大值（单点）", GetMaxSummaryValue(depthValues)),
                ("压花高度极小值（单点）", GetMinSummaryValue(depthValues))
            };

            AppendSummaryMetricGroup(csvLines, sequence, "压花", metrics, reportType, productModelName, shiftName, workshop, processName, machineNo, tester, scanTime, summaryTestItems);

            AppendCsvLines(filePath, !fileExists ? BuildCsvLine(BuildSummaryCsvHeaders()) : null, csvLines);
        }

        private static void AppendMetricGroup(List<(string ItemName, double? Value)> metrics, string itemPrefix, List<double> values)
        {
            metrics.Add(($"{itemPrefix}平均值", GetAverageSummaryValue(values)));
            metrics.Add(($"{itemPrefix}最大值", GetMaxSummaryValue(values)));
            metrics.Add(($"{itemPrefix}最小值", GetMinSummaryValue(values)));
            metrics.Add(($"{itemPrefix}极差", GetRangeSummaryValue(values)));
        }

        private bool AppendSummaryMetricGroup(List<string> csvLines, int sequence, string category, List<(string ItemName, double? Value)> metrics, string reportType, string productModelName, string shiftName, string workshop, string processName, string machineNo, string tester, DateTime scanTime, ObservableCollection<SummaryTestItemConfig> summaryTestItems)
        {
            bool isFirstLine = true;
            bool hasExported = false;
            foreach (var metric in metrics)
            {
                if (!ShouldExportSummaryItem(summaryTestItems, category, metric.ItemName))
                    continue;

                csvLines.Add(BuildCsvLine(new[]
                {
                    isFirstLine ? sequence.ToString() : string.Empty,
                    isFirstLine ? NormalizeCsvText(reportType) : string.Empty,
                    isFirstLine ? NormalizeCsvText(productModelName) : string.Empty,
                    isFirstLine ? NormalizeCsvText(shiftName) : string.Empty,
                    isFirstLine ? NormalizeCsvText(workshop) : string.Empty,
                    isFirstLine ? NormalizeCsvText(processName) : string.Empty,
                    isFirstLine ? NormalizeCsvText(machineNo) : string.Empty,
                    isFirstLine ? NormalizeCsvText(tester) : string.Empty,
                    metric.ItemName,
                    FormatCsvNumber(metric.Value),
                    scanTime.ToString("yyyy/M/d H:mm")
                }));

                isFirstLine = false;
                hasExported = true;
            }

            return hasExported;
        }

        private static List<string> BuildSummaryCsvHeaders()
        {
            return new List<string> { "序号", "报表类型", "工件名称", "班别", "车间", "工序", "机台号", "测试员", "测试项目", "测试值", "时间" };
        }

        private static int GetNextSummarySequence(string filePath)
        {
            if (!File.Exists(filePath))
                return 1;

            int maxSequence = 0;
            // CSV可能正被Excel/WPS打开，读取序号时允许共享读写，避免汇总导出直接崩。
            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            bool isHeader = true;
            while (reader.ReadLine() is string line)
            {
                if (isHeader)
                {
                    isHeader = false;
                    continue;
                }

                List<string> fields = ParseCsvLine(line);
                if (fields.Count > 0 && int.TryParse(fields[0], out int sequence))
                    maxSequence = Math.Max(maxSequence, sequence);
            }

            return maxSequence + 1;
        }

        private static bool ShouldExportSummaryItem(ObservableCollection<SummaryTestItemConfig> summaryTestItems, string category, string itemName)
        {
            List<SummaryTestItemConfig> categoryItems = summaryTestItems?
                .Where(item => item.Category == category)
                .ToList() ?? new List<SummaryTestItemConfig>();
            List<SummaryTestItemConfig> checkedItems = categoryItems
                .Where(item => item.IsChecked)
                .ToList();

            return checkedItems.Count == 0 || checkedItems.Any(item => itemName.Contains(item.Name, StringComparison.Ordinal));
        }

        private static List<double> GetValidSummaryValues(IEnumerable<double> values)
        {
            return values?
                .Where(value => !IsInvalidCsvValue(value))
                .ToList() ?? new List<double>();
        }

        private static List<double> GetValidSummaryValues(IEnumerable<double?> values)
        {
            return values?
                .Where(value => value.HasValue && !IsInvalidCsvValue(value.Value))
                .Select(value => value!.Value)
                .ToList() ?? new List<double>();
        }

        private static double? GetAverageSummaryValue(List<double> values)
        {
            return values.Count > 0 ? values.Average() : (double?)null;
        }

        private static double? GetMaxSummaryValue(List<double> values)
        {
            return values.Count > 0 ? values.Max() : (double?)null;
        }

        private static double? GetMinSummaryValue(List<double> values)
        {
            return values.Count > 0 ? values.Min() : (double?)null;
        }

        private static double? GetRangeSummaryValue(List<double> values)
        {
            return values.Count > 0 ? values.Max() - values.Min() : (double?)null;
        }

        private static double? GetAbsRangeSummaryValue(double? value1, double? value2)
        {
            return value1.HasValue && value2.HasValue ? Math.Abs(value1.Value - value2.Value) : (double?)null;
        }


        private int EnsureConvexCsvColumns(string filePath, int requiredPointCount)
        {
            if (!File.Exists(filePath))
                return requiredPointCount;

            List<string> lines = File.ReadAllLines(filePath, Encoding.UTF8).ToList();
            if (lines.Count == 0)
                return requiredPointCount;

            List<string> headerFields = ParseCsvLine(lines[0]);
            bool hasNormalCsvInfoColumns = HasNormalCsvInfoColumns(headerFields);
            int sourceDataStartIndex = hasNormalCsvInfoColumns ? NormalCsvBaseFieldCount : NormalCsvOldBaseFieldCount;
            int existingPointCount = GetConvexPointCountFromHeader(lines[0]);
            int sourceSegmentCount = GetConvexSegmentCountFromHeader(headerFields);
            int finalPointCount = Math.Max(existingPointCount, requiredPointCount);
            bool hasSeparatedLayout = IsSeparatedConvexCsvHeader(headerFields, existingPointCount, sourceDataStartIndex);
            bool hasMixedSummaryLayout = IsMixedSummaryConvexCsvHeader(headerFields, sourceDataStartIndex);
            int overallSummaryHeaderCount = GetConvexOverallSummaryHeaderCount(headerFields, sourceDataStartIndex);
            if (hasNormalCsvInfoColumns && finalPointCount <= existingPointCount && hasSeparatedLayout && sourceSegmentCount == ConvexCsvSegmentCount)
                return finalPointCount;

            lines[0] = BuildCsvLine(BuildConvexCsvHeaders(finalPointCount));
            int targetFieldCount = GetConvexCsvFieldCount(finalPointCount);
            for (int index = 1; index < lines.Count; index++)
            {
                List<string> fields = ParseCsvLine(lines[index]);
                fields = RebuildConvexCsvRow(fields, existingPointCount, finalPointCount, sourceSegmentCount, hasSeparatedLayout, hasMixedSummaryLayout, overallSummaryHeaderCount, hasNormalCsvInfoColumns, sourceDataStartIndex);
                PadCsvFields(fields, targetFieldCount);
                lines[index] = BuildCsvLine(fields);
            }

            WriteAllCsvLines(filePath, lines);
            return finalPointCount;
        }

        private static List<string> BuildConvexCsvHeaders(int pointCount)
        {
            List<string> headers = new List<string> { "时间", "型号", "批次号", "扫描次数", "车间", "工序", "报表类型", "工件名称", "班别", "机台号", "测试员", "深度最大值", "深度最小值", "深度均值" };
            for (int segmentIndex = 1; segmentIndex <= ConvexCsvSegmentCount; segmentIndex++)
            {
                headers.Add($"区{segmentIndex}深度最大值");
                headers.Add($"区{segmentIndex}深度最小值");
                headers.Add($"区{segmentIndex}深度均值");
            }

            for (int segmentIndex = 1; segmentIndex <= ConvexCsvSegmentCount; segmentIndex++)
            {
                for (int pointIndex = 1; pointIndex <= pointCount; pointIndex++)
                    headers.Add($"区{segmentIndex}点{pointIndex}深度");
            }

            headers.Add("直径最大值");
            headers.Add("直径最小值");
            headers.Add("直径均值");

            for (int segmentIndex = 1; segmentIndex <= ConvexCsvSegmentCount; segmentIndex++)
            {
                headers.Add($"区{segmentIndex}直径最大值");
                headers.Add($"区{segmentIndex}直径最小值");
                headers.Add($"区{segmentIndex}直径均值");
            }

            for (int segmentIndex = 1; segmentIndex <= ConvexCsvSegmentCount; segmentIndex++)
            {
                for (int pointIndex = 1; pointIndex <= pointCount; pointIndex++)
                    headers.Add($"区{segmentIndex}点{pointIndex}直径");
            }

            return headers;
        }

        private static int GetConvexPointCountFromHeader(string headerLine)
        {
            List<string> headerFields = ParseCsvLine(headerLine);
            int pointCount = headerFields.Count(field =>
                (field.StartsWith("区1点", StringComparison.Ordinal) || field.StartsWith("1段点", StringComparison.Ordinal) || field.StartsWith("第1段点", StringComparison.Ordinal)) &&
                field.EndsWith("深度", StringComparison.Ordinal));
            if (pointCount <= 0)
                return 1;

            return pointCount;
        }

        private static int GetConvexSegmentCountFromHeader(List<string> headerFields)
        {
            int segmentCount = headerFields
                .Select(GetConvexSegmentIndexFromDepthMaxHeader)
                .Select(text => int.TryParse(text, out int value) ? value : 0)
                .DefaultIfEmpty(0)
                .Max();

            return Math.Max(segmentCount, ConvexCsvSegmentCount);
        }

        private static string GetConvexSegmentIndexFromDepthMaxHeader(string field)
        {
            if (field.StartsWith("区", StringComparison.Ordinal) && field.EndsWith("深度最大值", StringComparison.Ordinal))
                return field.Substring(1, field.Length - "区".Length - "深度最大值".Length);

            return field.EndsWith("段深度最大值", StringComparison.Ordinal) ? field.Split('段')[0] : string.Empty;
        }

        private static int GetConvexCsvFieldCount(int pointCount)
        {
            int metricGroupFieldCount = pointCount + 3;
            return NormalCsvBaseFieldCount + 6 + ConvexCsvSegmentCount * metricGroupFieldCount * 2;
        }

        private static bool IsSeparatedConvexCsvHeader(List<string> headerFields, int pointCount, int dataStartIndex)
        {
            if (headerFields == null || headerFields.Count <= dataStartIndex)
                return false;

            int diameterSummaryIndex = dataStartIndex + 3 + ConvexCsvSegmentCount * 3 + ConvexCsvSegmentCount * pointCount;
            if (headerFields.Count <= diameterSummaryIndex)
                return false;

            return string.Equals(headerFields[dataStartIndex], "深度最大值", StringComparison.Ordinal) &&
                   string.Equals(headerFields[diameterSummaryIndex], "直径最大值", StringComparison.Ordinal);
        }

        private static bool IsMixedSummaryConvexCsvHeader(List<string> headerFields, int dataStartIndex)
        {
            if (headerFields == null || headerFields.Count < dataStartIndex + 9)
                return false;

            return string.Equals(headerFields[dataStartIndex], "深度最大值", StringComparison.Ordinal) &&
                   string.Equals(headerFields[dataStartIndex + 3], "直径最大值", StringComparison.Ordinal) &&
                   (string.Equals(headerFields[dataStartIndex + 6], "区1深度最大值", StringComparison.Ordinal) ||
                    string.Equals(headerFields[dataStartIndex + 6], "1段深度最大值", StringComparison.Ordinal));
        }

        private static int GetConvexOverallSummaryHeaderCount(List<string> headerFields, int dataStartIndex)
        {
            if (headerFields == null || headerFields.Count < dataStartIndex + 3)
                return 0;

            if (headerFields.Count >= dataStartIndex + 6 &&
                string.Equals(headerFields[dataStartIndex], "深度最大值", StringComparison.Ordinal) &&
                string.Equals(headerFields[dataStartIndex + 1], "深度最小值", StringComparison.Ordinal) &&
                string.Equals(headerFields[dataStartIndex + 2], "深度均值", StringComparison.Ordinal) &&
                string.Equals(headerFields[dataStartIndex + 3], "直径最大值", StringComparison.Ordinal) &&
                string.Equals(headerFields[dataStartIndex + 4], "直径最小值", StringComparison.Ordinal) &&
                string.Equals(headerFields[dataStartIndex + 5], "直径均值", StringComparison.Ordinal))
            {
                return 6;
            }

            if (string.Equals(headerFields[dataStartIndex], "深度最大值", StringComparison.Ordinal) &&
                string.Equals(headerFields[dataStartIndex + 1], "深度最小值", StringComparison.Ordinal) &&
                string.Equals(headerFields[dataStartIndex + 2], "深度均值", StringComparison.Ordinal))
            {
                return 3;
            }

            if (string.Equals(headerFields[dataStartIndex], "最大值", StringComparison.Ordinal) &&
                string.Equals(headerFields[dataStartIndex + 1], "最小值", StringComparison.Ordinal) &&
                string.Equals(headerFields[dataStartIndex + 2], "均值", StringComparison.Ordinal))
            {
                return 3;
            }

            return 0;
        }

        private static string GetFieldOrEmpty(List<string> fields, int index)
        {
            return index >= 0 && index < fields.Count ? fields[index] : string.Empty;
        }

        /// <summary>
        /// 判断正常结果CSV是否已经包含信息配置字段。
        /// </summary>
        private static bool HasNormalCsvInfoColumns(List<string> headerFields)
        {
            return headerFields.Contains("报表类型") &&
                   headerFields.Contains("工件名称") &&
                   headerFields.Contains("班别") &&
                   headerFields.Contains("机台号") &&
                   headerFields.Contains("测试员");
        }

        private static List<string> GetFieldRange(List<string> fields, int startIndex, int count)
        {
            List<string> result = new List<string>();
            for (int index = 0; index < count; index++)
            {
                result.Add(GetFieldOrEmpty(fields, startIndex + index));
            }

            return result;
        }

        private sealed class ConvexCsvMetricFields
        {
            public string Max { get; set; } = string.Empty;
            public string Min { get; set; } = string.Empty;
            public string Avg { get; set; } = string.Empty;
            public List<string> Points { get; } = new List<string>();
        }

        private sealed class ConvexCsvSegmentFields
        {
            public ConvexCsvMetricFields Depth { get; } = new ConvexCsvMetricFields();
            public ConvexCsvMetricFields Diameter { get; } = new ConvexCsvMetricFields();
        }

        private static List<string> RebuildConvexCsvRow(List<string> fields, int sourcePointCount, int targetPointCount, int sourceSegmentCount, bool hasSeparatedLayout, bool hasMixedSummaryLayout, int overallSummaryHeaderCount, bool hasNormalCsvInfoColumns, int sourceDataStartIndex)
        {
            List<string> fixedFields = new List<string>();
            for (int index = 0; index < NormalCsvOldBaseFieldCount; index++)
            {
                fixedFields.Add(GetFieldOrEmpty(fields, index));
            }

            if (hasNormalCsvInfoColumns)
            {
                for (int index = NormalCsvOldBaseFieldCount; index < NormalCsvBaseFieldCount; index++)
                {
                    fixedFields.Add(GetFieldOrEmpty(fields, index));
                }
            }
            else
            {
                PadCsvFields(fixedFields, NormalCsvBaseFieldCount);
            }

            List<ConvexCsvSegmentFields> sourceSegments = Enumerable.Range(0, sourceSegmentCount)
                .Select(_ => new ConvexCsvSegmentFields())
                .ToList();

            int offset = sourceDataStartIndex;
            List<string> overallSummary = new List<string> { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };

            if (overallSummaryHeaderCount == 6)
            {
                overallSummary[0] = GetFieldOrEmpty(fields, offset++);
                overallSummary[1] = GetFieldOrEmpty(fields, offset++);
                overallSummary[2] = GetFieldOrEmpty(fields, offset++);
                overallSummary[3] = GetFieldOrEmpty(fields, offset++);
                overallSummary[4] = GetFieldOrEmpty(fields, offset++);
                overallSummary[5] = GetFieldOrEmpty(fields, offset++);
            }
            else if (overallSummaryHeaderCount == 3)
            {
                overallSummary[0] = GetFieldOrEmpty(fields, offset++);
                overallSummary[1] = GetFieldOrEmpty(fields, offset++);
                overallSummary[2] = GetFieldOrEmpty(fields, offset++);
            }

            if (hasSeparatedLayout)
            {
                foreach (ConvexCsvSegmentFields segment in sourceSegments)
                {
                    segment.Depth.Max = GetFieldOrEmpty(fields, offset++);
                    segment.Depth.Min = GetFieldOrEmpty(fields, offset++);
                    segment.Depth.Avg = GetFieldOrEmpty(fields, offset++);
                }

                foreach (ConvexCsvSegmentFields segment in sourceSegments)
                {
                    segment.Depth.Points.AddRange(GetFieldRange(fields, offset, sourcePointCount));
                    offset += sourcePointCount;
                }

                if (overallSummaryHeaderCount == 3)
                {
                    overallSummary[3] = GetFieldOrEmpty(fields, offset++);
                    overallSummary[4] = GetFieldOrEmpty(fields, offset++);
                    overallSummary[5] = GetFieldOrEmpty(fields, offset++);
                }

                foreach (ConvexCsvSegmentFields segment in sourceSegments)
                {
                    segment.Diameter.Max = GetFieldOrEmpty(fields, offset++);
                    segment.Diameter.Min = GetFieldOrEmpty(fields, offset++);
                    segment.Diameter.Avg = GetFieldOrEmpty(fields, offset++);
                }

                foreach (ConvexCsvSegmentFields segment in sourceSegments)
                {
                    segment.Diameter.Points.AddRange(GetFieldRange(fields, offset, sourcePointCount));
                    offset += sourcePointCount;
                }
            }
            else if (hasMixedSummaryLayout)
            {
                foreach (ConvexCsvSegmentFields segment in sourceSegments)
                {
                    segment.Depth.Max = GetFieldOrEmpty(fields, offset++);
                    segment.Depth.Min = GetFieldOrEmpty(fields, offset++);
                    segment.Depth.Avg = GetFieldOrEmpty(fields, offset++);
                    segment.Diameter.Max = GetFieldOrEmpty(fields, offset++);
                    segment.Diameter.Min = GetFieldOrEmpty(fields, offset++);
                    segment.Diameter.Avg = GetFieldOrEmpty(fields, offset++);
                }

                foreach (ConvexCsvSegmentFields segment in sourceSegments)
                {
                    segment.Depth.Points.AddRange(GetFieldRange(fields, offset, sourcePointCount));
                    offset += sourcePointCount;
                    segment.Diameter.Points.AddRange(GetFieldRange(fields, offset, sourcePointCount));
                    offset += sourcePointCount;
                }
            }
            else
            {
                foreach (ConvexCsvSegmentFields segment in sourceSegments)
                {
                    segment.Depth.Points.AddRange(GetFieldRange(fields, offset, sourcePointCount));
                    offset += sourcePointCount;
                    segment.Depth.Max = GetFieldOrEmpty(fields, offset++);
                    segment.Depth.Min = GetFieldOrEmpty(fields, offset++);
                    segment.Depth.Avg = GetFieldOrEmpty(fields, offset++);

                    segment.Diameter.Points.AddRange(GetFieldRange(fields, offset, sourcePointCount));
                    offset += sourcePointCount;
                    segment.Diameter.Max = GetFieldOrEmpty(fields, offset++);
                    segment.Diameter.Min = GetFieldOrEmpty(fields, offset++);
                    segment.Diameter.Avg = GetFieldOrEmpty(fields, offset++);
                }
            }

            List<string> rebuiltRow = new List<string>(fixedFields);
            rebuiltRow.AddRange(overallSummary.Take(3));
            foreach (ConvexCsvSegmentFields segment in sourceSegments.Take(ConvexCsvSegmentCount))
            {
                rebuiltRow.Add(segment.Depth.Max);
                rebuiltRow.Add(segment.Depth.Min);
                rebuiltRow.Add(segment.Depth.Avg);
            }

            foreach (ConvexCsvSegmentFields segment in sourceSegments.Take(ConvexCsvSegmentCount))
            {
                rebuiltRow.AddRange(segment.Depth.Points.Take(targetPointCount));
                PadCsvFields(rebuiltRow, rebuiltRow.Count + Math.Max(targetPointCount - segment.Depth.Points.Count, 0));
            }

            rebuiltRow.AddRange(overallSummary.Skip(3).Take(3));
            foreach (ConvexCsvSegmentFields segment in sourceSegments.Take(ConvexCsvSegmentCount))
            {
                rebuiltRow.Add(segment.Diameter.Max);
                rebuiltRow.Add(segment.Diameter.Min);
                rebuiltRow.Add(segment.Diameter.Avg);
            }

            foreach (ConvexCsvSegmentFields segment in sourceSegments.Take(ConvexCsvSegmentCount))
            {
                rebuiltRow.AddRange(segment.Diameter.Points.Take(targetPointCount));
                PadCsvFields(rebuiltRow, rebuiltRow.Count + Math.Max(targetPointCount - segment.Diameter.Points.Count, 0));
            }

            return rebuiltRow;
        }

        private void AppendConvexOverallSummary(List<string> row, List<ConvexSegmentSnapshot> convexSegments, bool isDepth)
        {
            List<double> validValues = convexSegments?
                .Where(item => item != null)
                .SelectMany(item => isDepth ? item.DepthValues : item.DiameterValues)
                .Where(item => item.HasValue && !IsInvalidCsvValue(item.Value))
                .Select(item => item!.Value)
                .ToList() ?? new List<double>();

            row.Add(FormatSummaryValue(validValues.Count > 0 ? validValues.Max() : (double?)null));
            row.Add(FormatSummaryValue(validValues.Count > 0 ? validValues.Min() : (double?)null));
            row.Add(FormatSummaryValue(validValues.Count > 0 ? validValues.Average() : (double?)null));
        }

        private void AppendConvexMetricSummary(List<string> row, List<double?> values, int pointCount)
        {
            List<double?> normalizedValues = new List<double?>();
            for (int index = 0; index < pointCount; index++)
            {
                double? value = values != null && index < values.Count ? values[index] : null;
                normalizedValues.Add(value.HasValue && !IsInvalidCsvValue(value.Value) ? value : null);
            }

            List<double> validValues = normalizedValues
                .Where(item => item.HasValue)
                .Select(item => item!.Value)
                .ToList();

            row.Add(FormatSummaryValue(validValues.Count > 0 ? validValues.Max() : (double?)null));
            row.Add(FormatSummaryValue(validValues.Count > 0 ? validValues.Min() : (double?)null));
            row.Add(FormatSummaryValue(validValues.Count > 0 ? validValues.Average() : (double?)null));
        }

        private void AppendConvexMetricPoints(List<string> row, List<double?> values, int pointCount)
        {
            List<double?> normalizedValues = new List<double?>();
            for (int index = 0; index < pointCount; index++)
            {
                double? value = values != null && index < values.Count ? values[index] : null;
                normalizedValues.Add(value.HasValue && !IsInvalidCsvValue(value.Value) ? value : null);
            }

            row.AddRange(normalizedValues.Select(FormatCsvNumber));
        }

        private static void PadCsvFields(List<string> fields, int targetCount)
        {
            while (fields.Count < targetCount)
            {
                fields.Add(string.Empty);
            }
        }

        private static List<string> ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();
            if (line == null)
            {
                fields.Add(string.Empty);
                return fields;
            }

            StringBuilder current = new StringBuilder();
            bool inQuotes = false;
            for (int index = 0; index < line.Length; index++)
            {
                char ch = line[index];
                if (ch == '"')
                {
                    if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        current.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            fields.Add(current.ToString());
            return fields;
        }

        private static string BuildCsvLine(IEnumerable<string> fields)
        {
            return string.Join(",", fields.Select(EscapeCsvField));
        }

        private static void AppendCsvLines(string filePath, string? headerLine, IEnumerable<string> csvLines)
        {
            try
            {
                AppendCsvLinesCore(filePath, headerLine, csvLines);
            }
            catch (IOException)
            {
                CloseOpenedCsvIfNeeded(filePath);
                AppendCsvLinesCore(filePath, headerLine, csvLines);
            }
        }

        private static void AppendCsvLinesCore(string filePath, string? headerLine, IEnumerable<string> csvLines)
        {
            using var writer = new StreamWriter(filePath, append: true, Encoding.UTF8);
            if (headerLine != null)
            {
                writer.WriteLine(headerLine);
            }

            foreach (string csvLine in csvLines)
            {
                writer.WriteLine(csvLine);
            }
        }

        private static void WriteAllCsvLines(string filePath, IEnumerable<string> lines)
        {
            try
            {
                File.WriteAllLines(filePath, lines, new UTF8Encoding(true));
            }
            catch (IOException)
            {
                CloseOpenedCsvIfNeeded(filePath);
                File.WriteAllLines(filePath, lines, new UTF8Encoding(true));
            }
        }

        private static void CloseOpenedCsvIfNeeded(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            foreach (string progId in new[] { "Excel.Application", "Ket.Application", "ET.Application" })
            {
                CloseOpenedWorkbookIfNeeded(filePath, progId);
            }
        }

        private static void CloseOpenedWorkbookIfNeeded(string filePath, string progId)
        {
            object? excelObject = null;
            object? workbooksObject = null;
            try
            {
                CLSIDFromProgID(progId, out Guid excelGuid);
                GetActiveObject(ref excelGuid, IntPtr.Zero, out excelObject);
                dynamic excel = excelObject;
                workbooksObject = excel.Workbooks;
                dynamic workbooks = workbooksObject;
                int workbookCount = workbooks.Count;
                string targetPath = System.IO.Path.GetFullPath(filePath);

                for (int index = workbookCount; index >= 1; index--)
                {
                    object? workbookObject = null;
                    try
                    {
                        workbookObject = workbooks[index];
                        dynamic workbook = workbookObject;
                        string workbookPath = Convert.ToString(workbook.FullName) ?? string.Empty;
                        if (string.Equals(System.IO.Path.GetFullPath(workbookPath), targetPath, StringComparison.OrdinalIgnoreCase))
                        {
                            workbook.Close(false);
                            Thread.Sleep(200);
                            break;
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        ReleaseComObject(workbookObject);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                ReleaseComObject(workbooksObject);
                ReleaseComObject(excelObject);
            }
        }

        private static void ReleaseComObject(object? comObject)
        {
            if (comObject != null && Marshal.IsComObject(comObject))
            {
                Marshal.FinalReleaseComObject(comObject);
            }
        }

        [DllImport("ole32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void CLSIDFromProgID(string lpszProgID, out Guid pclsid);

        [DllImport("oleaut32.dll", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        private void QueueCsvUpload(string localFilePath, string localRootPath, bool isUploadPublicDisk, string publicDiskPath)
        {
            if (!isUploadPublicDisk || string.IsNullOrWhiteSpace(localFilePath) || string.IsNullOrWhiteSpace(localRootPath) || string.IsNullOrWhiteSpace(publicDiskPath))
                return;

            _ = Task.Run(async () =>
            {
                string snapshotPath = string.Empty;
                string targetPath = string.Empty;
                try
                {
                    snapshotPath = CreateCsvUploadSnapshot(localFilePath);
                    targetPath = BuildPublicDiskTargetPath(localFilePath, localRootPath, publicDiskPath);
                    await UploadCsvSnapshotAsync(snapshotPath, targetPath);
                    Logs.LogInfo($"CSV上传公盘成功，本地文件：{localFilePath}，公盘文件：{targetPath}");
                }
                catch (Exception ex)
                {
                    Logs.LogError(ex);
                    Logs.LogWarning($"CSV上传公盘失败，本地文件：{localFilePath}，公盘文件：{targetPath}");
                }
                finally
                {
                    if (!string.IsNullOrWhiteSpace(snapshotPath) && File.Exists(snapshotPath))
                    {
                        File.Delete(snapshotPath);
                    }
                }
            });
        }

        private static string CreateCsvUploadSnapshot(string localFilePath)
        {
            Directory.CreateDirectory(_uploadSnapshotRootPath);
            string snapshotPath = System.IO.Path.Combine(_uploadSnapshotRootPath, $"{Guid.NewGuid():N}{System.IO.Path.GetExtension(localFilePath)}");
            using FileStream sourceStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using FileStream snapshotStream = new FileStream(snapshotPath, FileMode.Create, FileAccess.Write, FileShare.None);
            sourceStream.CopyTo(snapshotStream);
            return snapshotPath;
        }

        private static string BuildPublicDiskTargetPath(string localFilePath, string localRootPath, string publicDiskPath)
        {
            string relativePath = System.IO.Path.GetRelativePath(localRootPath, localFilePath);
            return System.IO.Path.Combine(publicDiskPath, relativePath);
        }

        private static async Task UploadCsvSnapshotAsync(string snapshotPath, string targetPath)
        {
            SemaphoreSlim semaphore = _uploadFileLocks.GetOrAdd(targetPath, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                string? targetDirectoryPath = System.IO.Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDirectoryPath))
                {
                    Directory.CreateDirectory(targetDirectoryPath);
                }

                File.Copy(snapshotPath, targetPath, true);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private (int partitionCount, int lineCount) EnsureGrooveCsvColumns(string filePath, int requiredPartitionCount, int requiredLineCount)
        {
            if (!File.Exists(filePath))
                return (requiredPartitionCount, requiredLineCount);

            List<string> lines = File.ReadAllLines(filePath, Encoding.UTF8).ToList();
            if (lines.Count == 0)
                return (requiredPartitionCount, requiredLineCount);

            List<string> headerFields = ParseCsvLine(lines[0]);
            bool hasWorkshopAndProcessColumns = headerFields.Contains("车间") && headerFields.Contains("工序");
            bool hasNormalCsvInfoColumns = HasNormalCsvInfoColumns(headerFields);
            (int existingPartitionCount, int existingLineCount) = GetGrooveCsvSizeFromHeader(lines[0]);
            int finalPartitionCount = Math.Max(existingPartitionCount, requiredPartitionCount);
            int finalLineCount = Math.Max(existingLineCount, requiredLineCount);
            if (hasWorkshopAndProcessColumns && hasNormalCsvInfoColumns && finalPartitionCount <= existingPartitionCount && finalLineCount <= existingLineCount)
                return (existingPartitionCount, existingLineCount);

            lines[0] = BuildCsvLine(BuildGrooveCsvHeaders(finalPartitionCount, finalLineCount));
            int targetFieldCount = GetGrooveCsvFieldCount(finalPartitionCount, finalLineCount);
            for (int index = 1; index < lines.Count; index++)
            {
                List<string> fields = ParseCsvLine(lines[index]);
                if (!hasWorkshopAndProcessColumns)
                {
                    while (fields.Count < 4)
                    {
                        fields.Add(string.Empty);
                    }

                    fields.Insert(4, string.Empty);
                    fields.Insert(5, string.Empty);
                }

                if (!hasNormalCsvInfoColumns)
                {
                    while (fields.Count < NormalCsvOldBaseFieldCount)
                    {
                        fields.Add(string.Empty);
                    }

                    fields.Insert(6, string.Empty);
                    fields.Insert(7, string.Empty);
                    fields.Insert(8, string.Empty);
                    fields.Insert(9, string.Empty);
                    fields.Insert(10, string.Empty);
                }

                PadCsvFields(fields, targetFieldCount);
                lines[index] = BuildCsvLine(fields);
            }

            WriteAllCsvLines(filePath, lines);
            return (finalPartitionCount, finalLineCount);
        }

        private static List<string> BuildGrooveCsvHeaders(int partitionCount, int lineCount)
        {
            List<string> headers = new List<string> { "扫描时间", "扫描次数", "型号", "批次号", "车间", "工序", "报表类型", "工件名称", "班别", "机台号", "测试员" };

            for (int lineIndex = 1; lineIndex <= lineCount; lineIndex++)
            {
                headers.Add($"L{lineIndex}槽深NG");
                for (int partitionIndex = 1; partitionIndex <= partitionCount; partitionIndex++)
                    headers.Add($"L{lineIndex}-F{partitionIndex}槽深");
                headers.Add($"L{lineIndex}槽深均值");
            }

            for (int lineIndex = 1; lineIndex <= lineCount; lineIndex++)
            {
                headers.Add($"L{lineIndex}槽宽NG");
                for (int partitionIndex = 1; partitionIndex <= partitionCount; partitionIndex++)
                    headers.Add($"L{lineIndex}-F{partitionIndex}槽宽");
                headers.Add($"L{lineIndex}槽宽均值");
            }

            for (int lineIndex = 1; lineIndex < lineCount; lineIndex++)
            {
                headers.Add($"L{lineIndex}-L{lineIndex + 1}槽间距NG");
                for (int partitionIndex = 1; partitionIndex <= partitionCount; partitionIndex++)
                    headers.Add($"L{lineIndex}-L{lineIndex + 1}-F{partitionIndex}槽间距");
                headers.Add($"L{lineIndex}-L{lineIndex + 1}槽间距均值");
            }

            return headers;
        }

        private static (int partitionCount, int lineCount) GetGrooveCsvSizeFromHeader(string headerLine)
        {
            List<string> headerFields = ParseCsvLine(headerLine);
            int lineCount = headerFields.Count(field => field.EndsWith("槽深NG", StringComparison.Ordinal));
            if (lineCount <= 0)
                lineCount = 1;

            int firstDepthNgIndex = headerFields.IndexOf("L1槽深NG");
            int firstDepthAvgIndex = headerFields.IndexOf("L1槽深均值");
            int partitionCount = 1;
            if (firstDepthNgIndex >= 0 && firstDepthAvgIndex > firstDepthNgIndex)
            {
                partitionCount = Math.Max(firstDepthAvgIndex - firstDepthNgIndex - 1, 1);
            }

            return (partitionCount, lineCount);
        }

        private static int GetGrooveCsvFieldCount(int partitionCount, int lineCount)
        {
            int grooveGroupFieldCount = partitionCount + 2;
            return NormalCsvBaseFieldCount + lineCount * grooveGroupFieldCount * 2 + Math.Max(lineCount - 1, 0) * grooveGroupFieldCount;
        }

        private List<double?> GetGrooveValues(Dictionary<string, double> source, string metricKey, int partitionCount)
        {
            List<double?> values = new List<double?>();
            for (int partitionIndex = 1; partitionIndex <= partitionCount; partitionIndex++)
            {
                if (source.TryGetValue($"F{partitionIndex}-{metricKey}", out double value))
                {
                    values.Add(value);
                }
                else
                {
                    values.Add(null);
                }
            }

            return values;
        }

        private string GetNgPartitions(List<double?> values, double lowerLimit, double upperLimit)
        {
            if (upperLimit <= lowerLimit)
                return string.Empty;

            List<string> ngPartitions = new List<string>();
            for (int index = 0; index < values.Count; index++)
            {
                if (!values[index].HasValue || IsInvalidCsvValue(values[index]!.Value))
                    continue;

                double value = values[index]!.Value;
                if (value < lowerLimit || value > upperLimit)
                {
                    ngPartitions.Add($"F{index + 1}");
                }
            }

            return string.Join(",", ngPartitions);
        }

        private string GetAverageCsvNumber(List<double?> values)
        {
            List<double> validValues = values
                .Where(item => item.HasValue && !IsInvalidCsvValue(item.Value))
                .Select(item => item!.Value)
                .ToList();

            if (validValues.Count == 0)
                return string.Empty;

            return validValues.Average().ToString(_strFormat);
        }

        private string FormatCsvNumber(double? value)
        {
            if (!value.HasValue || IsInvalidCsvValue(value.Value))
                return string.Empty;

            return value.Value.ToString(_strFormat);
        }

        // -1、NaN、Infinity 都按无效值处理，只保留空白，别往 CSV 里硬塞垃圾。
        private static bool IsInvalidCsvValue(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value + 1d) < 0.000001d;
        }

        private static string NormalizeCsvText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
        #endregion


        #region Commands
        private void DynamicDataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
                AdjustColumnWidths();
            DynamicDataGrid.ItemsSource = DynamicDataSource;
            if (DynamicDataSource == null) return;
        }
        #endregion
    }

    public partial class KCJC0_StatisticsTableDataModel : BindableBase
    {
        private bool _isHeader;
        public bool IsHeader
        {
            get { return _isHeader; }
            set { _isHeader = value; RaisePropertyChanged(); }
        }

        private string _displayStr;
        public string DisplayStr
        {
            get { return _displayStr; }
            set { _displayStr = value; RaisePropertyChanged(); }
        }

        private double _value;
        public double Value
        {
            get { return _value; }
            set { _value = value; RaisePropertyChanged(); }
        }

        private double _sUpLimit;
        public double SUpLimit
        {
            get { return _sUpLimit; }
            set { _sUpLimit = value; RaisePropertyChanged(); }
        }

        private double _sDownLimit;
        public double SDownLimit
        {
            get { return _sDownLimit; }
            set { _sDownLimit = value; RaisePropertyChanged(); }
        }

        private double _cUpLimit;
        public double CUpLimit
        {
            get { return _cUpLimit; }
            set { _cUpLimit = value; RaisePropertyChanged(); }
        }

        private double _cDownLimit;
        public double CDownLimit
        {
            get { return _cDownLimit; }
            set { _cDownLimit = value; RaisePropertyChanged(); }
        }

        private Brush _foregroundBrush = Brushes.Black;
        public Brush ForegroundBrush
        {
            get { return _foregroundBrush; }
            set { _foregroundBrush = value; RaisePropertyChanged(); }
        }

        private bool _isNg;
        public bool IsNg
        {
            get { return _isNg; }
            set { _isNg = value; RaisePropertyChanged(); }
        }

        private string _okNgValue = string.Empty;
        public string OkNgValue
        {
            get { return _okNgValue; }
            set { _okNgValue = value; RaisePropertyChanged(); }
        }
    }

    public class ScanRecord
    {
        public DateTime ScanTime { get; set; }
        public int ScanCount { get; set; }

        // 间距：Dictionary<string, double> 或直接展开字段
        public Dictionary<string, double> Spacings { get; } = new();
        public Dictionary<string, double> Depths { get; } = new();
        public Dictionary<string, double> Widths { get; } = new();

        // 辅助方法：获取列值（用于导出）
        public double GetValue(string columnName)
        {
            // 解析 "F1-L1-L2分区间距" → 类型=spacing, 分区=F1, key=L1-L2
            if (columnName.Contains("分区间距"))
            {
                var key = columnName.Replace("分区间距", "");
                return Spacings.TryGetValue(key, out var v) ? v : double.NaN;
            }
            else if (columnName.Contains("分区槽深"))
            {
                var key = columnName.Replace("分区槽深", "");
                return Depths.TryGetValue(key, out var v) ? v : double.NaN;
            }
            else if (columnName.Contains("分区槽宽"))
            {
                var key = columnName.Replace("分区槽宽", "");
                return Widths.TryGetValue(key, out var v) ? v : double.NaN;
            }
            return double.NaN;
        }
    }

    public class ConvexSegmentSnapshot
    {
        public int SegmentIndex { get; set; }

        public List<double?> DepthValues { get; } = new List<double?>();

        public List<double?> DiameterValues { get; } = new List<double?>();
    }
}
