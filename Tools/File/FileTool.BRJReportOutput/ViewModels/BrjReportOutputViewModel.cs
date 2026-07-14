using FileTool.BRJReportOutput.Models;
using FileTool.BRJReportOutput.Services;
using FileTool.BRJReportOutput.Views;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FileTool.BRJReportOutput.ViewModels
{
    [Serializable]
    public class BrjReportOutputViewModel : DialogViewModelBase, IViewModuleParam
    {
        private const int PageSize = 27;

        private QueryMode _queryMode = QueryMode.All;
        private DateTime? _startDate = DateTime.Today.AddMonths(-1);
        private DateTime? _endDate = DateTime.Today;
        private string _querySN = string.Empty;
        private int _currentPage = 1;
        private int _totalPage = 1;
        private string _statusText = string.Empty;
        private bool _hasLoadedPage;
        private int _selectedBatchVersion;
        private int _completedBatchRefreshVersion;
        private readonly List<BrjDefectMapPoint> _allDefectMapPoints = new();

        private ObservableCollection<BrjReportRecord> _reportRecords = new();
        public ObservableCollection<BrjReportRecord> ReportRecords
        {
            get { return _reportRecords; }
            set
            {
                _reportRecords = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<BrjDefectRecord> SelectedBatchDefects { get; } = new();

        public ObservableCollection<BrjDefectMapPoint> DefectMapPoints { get; } = new();

        public ObservableCollection<double> DefectMapGuideLinePercents { get; } = new();

        public ObservableCollection<BrjDefectVisualStyle> DefectLegendItems { get; } = new();

        public ObservableCollection<BrjChartStatItem> DefectTypeStats { get; } = new();

        public ObservableCollection<BrjChartStatItem> DefectAreaStats { get; } = new();

        public ObservableCollection<BrjChartStatItem> DefectDiameterStats { get; } = new();

        public ObservableCollection<BrjChartStatItem> DefectSlitStats { get; } = new();

        public BrjReportOutputViewModel()
        {
            PrismProvider.EventAggregator?
                .GetEvent<BrjReportDataChangedEvent>()
                .Subscribe(OnBrjReportDataChanged, ThreadOption.UIThread);
        }

        private BrjDefectMapPoint? _selectedDefectMapPoint;
        public BrjDefectMapPoint? SelectedDefectMapPoint
        {
            get { return _selectedDefectMapPoint; }
            set
            {
                _selectedDefectMapPoint = value;
                RaisePropertyChanged();
            }
        }

        private double _defectMapLengthM = 1d;
        public double DefectMapLengthM
        {
            get { return _defectMapLengthM; }
            set
            {
                _defectMapLengthM = value;
                RaisePropertyChanged();
            }
        }

        private BrjReportRecord? _selectedReportRecord;
        public BrjReportRecord? SelectedReportRecord
        {
            get { return _selectedReportRecord; }
            set
            {
                _selectedReportRecord = value;
                RaisePropertyChanged();
                _ = RefreshSelectedBatchMapAsync(value);
            }
        }

        public DateTime? StartDate
        {
            get { return _startDate; }
            set
            {
                _startDate = value;
                RaisePropertyChanged();
            }
        }

        public DateTime? EndDate
        {
            get { return _endDate; }
            set
            {
                _endDate = value;
                RaisePropertyChanged();
            }
        }

        public string QuerySN
        {
            get { return _querySN; }
            set
            {
                _querySN = value;
                RaisePropertyChanged();
            }
        }

        public int CurrentPage
        {
            get { return _currentPage; }
            set
            {
                _currentPage = value;
                RaisePropertyChanged();
            }
        }

        public int TotalPage
        {
            get { return _totalPage; }
            set
            {
                _totalPage = value;
                RaisePropertyChanged();
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;
                RaisePropertyChanged();
            }
        }

        public new BrjReportOutputModel ModelParam
        {
            get { return (base.ModelParam as BrjReportOutputModel)!; }
            set
            {
                base.ModelParam = value;
                RaisePropertyChanged();
            }
        }

        public DelegateCommand QueryByDateCommand => new(async () => await QueryByDateAsync());
        public DelegateCommand QueryBySNCommand => new(async () => await QueryBySNAsync());
        public DelegateCommand<string> QuickQueryCommand => new(async mode => await QuickQueryAsync(mode));
        public DelegateCommand FirstPageCommand => new(async () => await GoToPageAsync(1));
        public DelegateCommand PreviousPageCommand => new(async () => await GoToPageAsync(CurrentPage - 1));
        public DelegateCommand NextPageCommand => new(async () => await GoToPageAsync(CurrentPage + 1));
        public DelegateCommand LastPageCommand => new(async () => await GoToPageAsync(TotalPage));
        public DelegateCommand ExportBatchReportCommand => new(async () => await ExportBatchReportAsync());
        public DelegateCommand OpenReportFolderCommand => new(async () => await OpenReportFolderAsync());
        public DelegateCommand ShowBatchDefectsCommand => new(async () => await ShowBatchDefectsAsync());
        public DelegateCommand OpenDiameterGroupSettingsCommand => new(async () => await OpenDiameterGroupSettingsAsync());
        public DelegateCommand SelectReportDirectoryCommand => new(async () => await SelectReportDirectoryAsync());
        public DelegateCommand ResetReportDirectoryCommand => new(async () => await ResetReportDirectoryAsync());
        public DelegateCommand<BrjDefectVisualStyle> ToggleDefectLegendCommand => new(_ => ApplyDefectLegendFilter());

        public DelegateCommand LoadCommand => new DelegateCommand(async () =>
        {
            if (Visibility == System.Windows.Visibility.Hidden)
            {
                ModelParam.LoadKeyParam();
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
                return;
            }

            if (!_hasLoadedPage)
            {
                BrjReportStorage.EnsureCreated();
                await LoadPageAsync(1);
                _hasLoadedPage = true;
            }
        });

        public override void InitParam()
        {
            ModelParam = InitModelParam<BrjReportOutputModel>();
            LoadPageAsync(1).GetAwaiter().GetResult();
            _hasLoadedPage = true;
        }

        private async Task QueryByDateAsync()
        {
            if (StartDate == null || EndDate == null)
            {
                StatusText = "请选择开始日期和结束日期";
                return;
            }

            _queryMode = QueryMode.Date;
            await LoadPageAsync(1);
        }

        private async Task QueryBySNAsync()
        {
            if (string.IsNullOrWhiteSpace(QuerySN))
            {
                StatusText = "请输入要查询的批次号";
                return;
            }

            _queryMode = QueryMode.SN;
            await LoadPageAsync(1);
        }

        private async Task QuickQueryAsync(string mode)
        {
            DateTime today = DateTime.Today;

            switch (mode)
            {
                case "Today":
                    StartDate = today;
                    EndDate = today;
                    break;
                case "Yesterday":
                    StartDate = today.AddDays(-1);
                    EndDate = today.AddDays(-1);
                    break;
                case "ThreeDays":
                    StartDate = today.AddDays(-2);
                    EndDate = today;
                    break;
                case "SevenDays":
                    StartDate = today.AddDays(-6);
                    EndDate = today;
                    break;
                case "ThirtyDays":
                    StartDate = today.AddDays(-29);
                    EndDate = today;
                    break;
                case "OneYear":
                    StartDate = today.AddYears(-1);
                    EndDate = today;
                    break;
                default:
                    return;
            }

            _queryMode = QueryMode.Date;
            await LoadPageAsync(1);
        }

        private async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPage)
            {
                return;
            }

            await LoadPageAsync(page);
        }

        private async Task LoadPageAsync(int page)
        {
            try
            {
                int total;
                if (_queryMode == QueryMode.Date)
                {
                    DateTime start = StartDate!.Value.Date;
                    DateTime end = EndDate!.Value.Date.AddDays(1).AddTicks(-1);
                    total = await BrjReportStorage.CountRecordsAsync(start, end);
                    TotalPage = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
                    CurrentPage = Math.Min(Math.Max(1, page), TotalPage);
                    ReportRecords = new ObservableCollection<BrjReportRecord>(
                        await BrjReportStorage.QueryRecordsAsync(start, end, CurrentPage, PageSize));
                }
                else if (_queryMode == QueryMode.SN)
                {
                    string sn = QuerySN.Trim();
                    total = await BrjReportStorage.CountRecordsBySNAsync(sn);
                    TotalPage = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
                    CurrentPage = Math.Min(Math.Max(1, page), TotalPage);
                    ReportRecords = new ObservableCollection<BrjReportRecord>(
                        await BrjReportStorage.QueryRecordsBySNAsync(sn, CurrentPage, PageSize));
                }
                else
                {
                    total = await BrjReportStorage.CountRecordsAsync();
                    TotalPage = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
                    CurrentPage = Math.Min(Math.Max(1, page), TotalPage);
                    ReportRecords = new ObservableCollection<BrjReportRecord>(
                        await BrjReportStorage.QueryRecordsAsync(CurrentPage, PageSize));
                }

                SelectedReportRecord = ReportRecords.FirstOrDefault();
                StatusText = $"共 {total} 条记录";
            }
            catch (Exception ex)
            {
                ReportRecords = new ObservableCollection<BrjReportRecord>();
                SelectedReportRecord = null;
                TotalPage = 1;
                CurrentPage = 1;
                StatusText = $"查询报表记录失败：{ex.Message}";
            }
        }

        private async Task ExportBatchReportAsync()
        {
            try
            {
                if (SelectedReportRecord == null)
                {
                    StatusText = "请选择要生成报表的批次";
                    return;
                }

                var defects = await BrjReportStorage.QueryDefectsAsync(SelectedReportRecord.SN);
                string reportDirectory = await BrjReportStorage.QueryReportOutputDirectoryAsync();
                string filePath = await BrjReportExportService.ExportBatchReportAsync(SelectedReportRecord, defects, reportDirectory);
                StatusText = $"已生成报表：{filePath}";
            }
            catch (Exception ex)
            {
                StatusText = $"生成批次报表失败：{ex.Message}";
            }
        }

        private async Task OpenReportFolderAsync()
        {
            string reportDirectory = BrjReportExportService.ResolveReportDirectory(await BrjReportStorage.QueryReportOutputDirectoryAsync());
            Directory.CreateDirectory(reportDirectory);
            Process.Start(new ProcessStartInfo(reportDirectory)
            {
                UseShellExecute = true,
            });
        }

        private async Task SelectReportDirectoryAsync()
        {
            string currentDirectory = BrjReportExportService.ResolveReportDirectory(await BrjReportStorage.QueryReportOutputDirectoryAsync());
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "请选择BRJ报表保存目录",
            };

            if (Directory.Exists(currentDirectory))
            {
                dialog.InitialDirectory = currentDirectory;
            }

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            await BrjReportStorage.SaveReportOutputDirectoryAsync(dialog.FolderName);
            StatusText = $"报表保存目录：{BrjReportExportService.ResolveReportDirectory(dialog.FolderName)}";
        }

        private async Task ResetReportDirectoryAsync()
        {
            await BrjReportStorage.SaveReportOutputDirectoryAsync(string.Empty);
            StatusText = $"已恢复默认报表目录：{BrjReportExportService.ReportDirectory}";
        }

        private async void OnBrjReportDataChanged(BrjReportDataChangedPayload payload)
        {
            try
            {
                if (payload == null || !payload.IsRollCompleted || string.IsNullOrWhiteSpace(payload.SN))
                {
                    return;
                }

                int version = ++_completedBatchRefreshVersion;
                await Task.Delay(300);
                if (version != _completedBatchRefreshVersion)
                {
                    return;
                }

                await RefreshCompletedBatchAsync(payload.SN.Trim(), payload.DefectCount);
            }
            catch (Exception ex)
            {
                StatusText = $"批次刷新失败：{ex.Message}";
            }
        }

        private async Task RefreshCompletedBatchAsync(string sn, int defectCount)
        {
            BrjReportRecord? latestRecord = await BrjReportStorage.QueryRecordAsync(sn);
            if (latestRecord == null)
            {
                return;
            }

            string? selectedSN = SelectedReportRecord?.SN;
            int rowIndex = ReportRecords
                .Select((item, index) => new { item, index })
                .FirstOrDefault(row => string.Equals(row.item.SN, sn, StringComparison.OrdinalIgnoreCase))
                ?.index ?? -1;

            if (rowIndex >= 0)
            {
                ReportRecords[rowIndex] = latestRecord;
            }

            if (string.Equals(selectedSN, sn, StringComparison.OrdinalIgnoreCase))
            {
                SelectedReportRecord = latestRecord;
                StatusText = $"换卷完成，已刷新批次 {sn}，缺陷 {defectCount} 条";
            }
        }

        private async Task ShowBatchDefectsAsync()
        {
            try
            {
                if (SelectedReportRecord == null)
                {
                    StatusText = "请选择要显示缺陷明细的批次";
                    return;
                }

                List<BrjDefectRecord> defects = await BrjReportStorage.QueryDefectsAsync(SelectedReportRecord.SN);
                var dialog = new BrjDefectRecordDialog(SelectedReportRecord, defects);
                Window? owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive)
                    ?? Application.Current?.MainWindow;
                if (owner != null)
                {
                    dialog.Owner = owner;
                }

                dialog.ShowDialog();
                StatusText = $"已显示批次 {SelectedReportRecord.SN} 缺陷 {defects.Count} 条";
            }
            catch (Exception ex)
            {
                StatusText = $"显示批次缺陷明细失败：{ex.Message}";
            }
        }

        private async Task OpenDiameterGroupSettingsAsync()
        {
            List<BrjReportSetting> settings = await BrjReportStorage.QueryDiameterGroupSettingsAsync();
            var dialog = new BrjDiameterGroupSettingsDialog(settings);
            Window? owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive)
                ?? Application.Current?.MainWindow;
            if (owner != null)
            {
                dialog.Owner = owner;
            }

            if (dialog.ShowDialog() == true)
            {
                await BrjReportStorage.SaveDiameterGroupSettingsAsync(dialog.Settings);
                StatusText = "直径分组参数已保存";
            }
        }

        private async Task RefreshSelectedBatchMapAsync(BrjReportRecord? record)
        {
            try
            {
                int version = ++_selectedBatchVersion;
                SelectedBatchDefects.Clear();
                DefectMapPoints.Clear();
                DefectMapGuideLinePercents.Clear();
                DefectLegendItems.Clear();
                ClearStatistics();
                _allDefectMapPoints.Clear();
                SelectedDefectMapPoint = null;

                if (record == null)
                {
                    DefectMapLengthM = 1d;
                    return;
                }

                List<BrjDefectRecord> defects = await BrjReportStorage.QueryDefectsAsync(record.SN);
                if (version != _selectedBatchVersion)
                {
                    return;
                }

                if (record.DefectCount > 0 && defects.Count != record.DefectCount)
                {
                    StatusText = $"缺陷明细数量不一致：汇总 {record.DefectCount} 条，明细 {defects.Count} 条";
                }

                foreach (BrjDefectRecord defect in defects)
                {
                    SelectedBatchDefects.Add(defect);
                }

                DefectMapLengthM = ResolveMapLength(record, defects);
                double widthMm = ResolveMapWidth(record, defects);
                Dictionary<string, BrjDefectVisualStyle> styles = BrjDefectStyleResolver.ResolveStyles(defects);
                List<BrjReportSetting> diameterGroups = await BrjReportStorage.QueryDiameterGroupSettingsAsync();
                if (version != _selectedBatchVersion)
                {
                    return;
                }

                foreach (BrjDefectVisualStyle style in styles.Values)
                {
                    DefectLegendItems.Add(style);
                }

                foreach (double guideLinePercent in ResolveGuideLinePercents(record, widthMm))
                {
                    DefectMapGuideLinePercents.Add(guideLinePercent);
                }

                for (int i = 0; i < defects.Count; i++)
                {
                    BrjDefectRecord defect = defects[i];
                    double xPercent = widthMm <= 0d ? 50d : defect.PositionXMm / widthMm * 100d;
                    BrjDefectVisualStyle style = BrjDefectStyleResolver.ResolveStyle(styles, defect.DefectType);
                    _allDefectMapPoints.Add(new BrjDefectMapPoint
                    {
                        Source = defect,
                        DefectKey = style.DefectType,
                        DefectType = string.IsNullOrWhiteSpace(defect.DefectType) ? "未分类" : defect.DefectType,
                        MarkerKind = style.MarkerKind,
                        XPercent = Math.Clamp(xPercent, 0d, 100d),
                        MeterValue = Math.Max(0d, defect.PositionYM),
                        Fill = style.Fill,
                        Stroke = style.Stroke,
                    });
                }

                RefreshBatchStatistics(record, defects, styles, diameterGroups, widthMm);
                ApplyDefectLegendFilter();
            }
            catch (Exception ex)
            {
                DefectMapLengthM = Math.Max(1d, Math.Max(record?.RollLengthM ?? 0d, record?.DetectMeters ?? 0d));
                ClearStatistics();
                StatusText = $"缺陷地图读取失败：{ex.Message}";
            }
        }

        private void ClearStatistics()
        {
            DefectTypeStats.Clear();
            DefectAreaStats.Clear();
            DefectDiameterStats.Clear();
            DefectSlitStats.Clear();
        }

        private void RefreshBatchStatistics(
            BrjReportRecord record,
            IReadOnlyList<BrjDefectRecord> defects,
            IReadOnlyDictionary<string, BrjDefectVisualStyle> styles,
            IReadOnlyList<BrjReportSetting> diameterGroups,
            double widthMm)
        {
            ClearStatistics();
            AddStatItems(DefectTypeStats, BuildTypeStats(defects, styles));
            AddStatItems(DefectAreaStats, BuildAreaStats(defects));
            AddStatItems(DefectDiameterStats, BuildDiameterStats(defects, diameterGroups));
            AddStatItems(DefectSlitStats, BuildSlitStats(record, defects, widthMm));
        }

        private static void AddStatItems(ObservableCollection<BrjChartStatItem> target, IEnumerable<BrjChartStatItem> items)
        {
            foreach (BrjChartStatItem item in items)
            {
                target.Add(item);
            }
        }

        private static IEnumerable<BrjChartStatItem> BuildTypeStats(
            IReadOnlyList<BrjDefectRecord> defects,
            IReadOnlyDictionary<string, BrjDefectVisualStyle> styles)
        {
            int total = defects.Count;
            int index = 0;
            return defects
                .GroupBy(item => NormalizeText(item.DefectType))
                .OrderByDescending(group => group.Count())
                .Select(group =>
                {
                    BrjDefectVisualStyle style = BrjDefectStyleResolver.ResolveStyle(styles, group.Key);
                    return CreateStatItem(group.Key, group.Count(), total, style.Fill, index++);
                })
                .ToList();
        }

        private static IEnumerable<BrjChartStatItem> BuildAreaStats(IReadOnlyList<BrjDefectRecord> defects)
        {
            int total = defects.Count;
            var groups = new[]
            {
                new StatRange("面积<0.5", null, 0.5d),
                new StatRange("0.5<=面积<1", 0.5d, 1d),
                new StatRange("1<=面积<2", 1d, 2d),
                new StatRange("2<=面积<5", 2d, 5d),
                new StatRange("面积>=5", 5d, null),
            };

            return groups
                .Select((group, index) => CreateStatItem(
                    group.Name,
                    defects.Count(defect => IsInRange(defect.AreaMm2, group.Min, group.Max)),
                    total,
                    ResolvePaletteBrush(index),
                    index))
                .ToList();
        }

        private static IEnumerable<BrjChartStatItem> BuildDiameterStats(IReadOnlyList<BrjDefectRecord> defects, IReadOnlyList<BrjReportSetting> diameterGroups)
        {
            int total = defects.Count;
            return diameterGroups
                .OrderBy(item => item.SortIndex)
                .Select((group, index) => CreateStatItem(
                    BuildDiameterGroupName(group),
                    defects.Count(defect => IsInDiameterGroup(defect.DiameterMm, group)),
                    total,
                    ResolveBrush(group.ColorHex, index),
                    index))
                .ToList();
        }

        private static IEnumerable<BrjChartStatItem> BuildSlitStats(BrjReportRecord record, IReadOnlyList<BrjDefectRecord> defects, double widthMm)
        {
            int total = defects.Count;
            if (defects.Any(item => item.SlitIndex != 0) || defects.Select(item => item.SlitIndex).Distinct().Count() > 1)
            {
                return defects
                    .GroupBy(item => item.SlitIndex)
                    .OrderBy(group => group.Key)
                    .Select((group, index) => CreateStatItem($"分条{group.Key}", group.Count(), total, ResolvePaletteBrush(index), index))
                    .ToList();
            }

            List<double> boundaries = ResolveCoordinateValues(record.SlitLeftCoordinates)
                .Concat(ResolveCoordinateValues(record.SlitRightCoordinates))
                .Where(item => item >= 0d && item <= widthMm)
                .Concat(new[] { 0d, widthMm })
                .Distinct()
                .OrderBy(item => item)
                .ToList();

            if (boundaries.Count < 3)
            {
                return new[] { CreateStatItem("整幅", defects.Count, total, ResolvePaletteBrush(0), 0) };
            }

            var result = new List<BrjChartStatItem>();
            for (int i = 0; i < boundaries.Count - 1; i++)
            {
                double left = boundaries[i];
                double right = boundaries[i + 1];
                int count = defects.Count(defect => i == boundaries.Count - 2
                    ? defect.PositionXMm >= left && defect.PositionXMm <= right
                    : defect.PositionXMm >= left && defect.PositionXMm < right);
                result.Add(CreateStatItem($"分条{i + 1}({FormatNumber(left)}-{FormatNumber(right)}mm)", count, total, ResolvePaletteBrush(i), i));
            }

            return result;
        }

        private static BrjChartStatItem CreateStatItem(string name, int count, int total, Brush fill, int sortIndex)
        {
            return new BrjChartStatItem
            {
                Name = name,
                Count = count,
                Percent = total <= 0 ? 0d : count * 100d / total,
                Fill = fill,
                SortIndex = sortIndex,
            };
        }

        private static bool IsInRange(double value, double? min, double? max)
        {
            bool minMatched = min == null || value >= min.Value;
            bool maxMatched = max == null || value < max.Value;
            return minMatched && maxMatched;
        }

        private static bool IsInDiameterGroup(double diameter, BrjReportSetting group)
        {
            return IsInRange(diameter, group.MinDiameterMm, group.MaxDiameterMm);
        }

        private static string BuildDiameterGroupName(BrjReportSetting group)
        {
            if (!string.IsNullOrWhiteSpace(group.GroupName))
            {
                return group.GroupName;
            }

            if (group.MinDiameterMm != null && group.MaxDiameterMm != null)
            {
                return $"{FormatNumber(group.MinDiameterMm.Value)}<=直径<{FormatNumber(group.MaxDiameterMm.Value)}";
            }

            if (group.MinDiameterMm != null)
            {
                return $"{FormatNumber(group.MinDiameterMm.Value)}<=直径";
            }

            return $"直径<{FormatNumber(group.MaxDiameterMm ?? 0d)}";
        }

        private static string NormalizeText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "未分类" : text.Trim();
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.###");
        }

        private static Brush ResolveBrush(string colorHex, int index)
        {
            if (!string.IsNullOrWhiteSpace(colorHex))
            {
                try
                {
                    return (Brush)new BrushConverter().ConvertFromString(colorHex.Trim())!;
                }
                catch
                {
                }
            }

            return ResolvePaletteBrush(index);
        }

        private static Brush ResolvePaletteBrush(int index)
        {
            Color[] colors =
            {
                Color.FromRgb(59, 130, 246),
                Color.FromRgb(239, 68, 68),
                Color.FromRgb(245, 158, 11),
                Color.FromRgb(34, 197, 94),
                Color.FromRgb(139, 92, 246),
                Color.FromRgb(14, 165, 233),
                Color.FromRgb(236, 72, 153),
            };

            return new SolidColorBrush(colors[Math.Abs(index) % colors.Length]);
        }

        private void ApplyDefectLegendFilter()
        {
            int? selectedId = SelectedDefectMapPoint?.Source.Id;
            HashSet<string> visibleTypes = DefectLegendItems
                .Where(item => item.IsVisible)
                .Select(item => item.DefectType)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            DefectMapPoints.Clear();
            foreach (BrjDefectMapPoint point in _allDefectMapPoints.Where(item => visibleTypes.Contains(item.DefectKey)))
            {
                DefectMapPoints.Add(point);
            }

            SelectedDefectMapPoint = selectedId.HasValue
                ? DefectMapPoints.FirstOrDefault(item => item.Source.Id == selectedId.Value) ?? DefectMapPoints.FirstOrDefault()
                : DefectMapPoints.FirstOrDefault();
        }

        private static double ResolveMapWidth(BrjReportRecord record, IReadOnlyCollection<BrjDefectRecord> defects)
        {
            double maxPosition = defects.Count == 0 ? 0d : defects.Max(item => Math.Max(0d, item.PositionXMm));
            return Math.Max(1d, Math.Max(record.ProductWidthMm, maxPosition));
        }

        private static double ResolveMapLength(BrjReportRecord record, IReadOnlyCollection<BrjDefectRecord> defects)
        {
            double maxPosition = defects.Count == 0 ? 0d : defects.Max(item => Math.Max(0d, item.PositionYM));
            return Math.Max(1d, Math.Max(Math.Max(record.RollLengthM, record.DetectMeters), maxPosition));
        }

        private static IEnumerable<double> ResolveGuideLinePercents(BrjReportRecord record, double widthMm)
        {
            if (widthMm <= 0d)
            {
                yield break;
            }

            foreach (double xMm in ResolveCoordinateValues(record.SlitLeftCoordinates).Concat(ResolveCoordinateValues(record.SlitRightCoordinates)).Distinct())
            {
                double percent = xMm / widthMm * 100d;
                if (double.IsFinite(percent))
                {
                    yield return Math.Clamp(percent, 0d, 100d);
                }
            }
        }

        private static IEnumerable<double> ResolveCoordinateValues(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            foreach (Match match in Regex.Matches(text, @"-?\d+(\.\d+)?"))
            {
                if (double.TryParse(match.Value, out double value))
                {
                    yield return value;
                }
            }
        }

        private enum QueryMode
        {
            All,
            Date,
            SN,
        }

        private sealed record StatRange(string Name, double? Min, double? Max);
    }

    public class BrjChartStatItem
    {
        public string Name { get; init; } = string.Empty;

        public int Count { get; init; }

        public double Percent { get; init; }

        public Brush Fill { get; init; } = Brushes.SteelBlue;

        public int SortIndex { get; init; }

        public string CountText => $"{Count} / {Percent:0.00}%";
    }
}
