using ALGO.DefectPostProcess.Models;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace ALGO.DefectPostProcess.ViewModels
{
    /// <summary>
    /// 实现实例特征值弹窗的视图模型。
    /// </summary>
    public class DefectFeatureValuesViewModel : DialogViewModelBase
    {
        private DefectPostProcessModel _sourceModel;

        public ObservableCollection<DefectFeatureFilterOption> FilterOptions { get; } = new ObservableCollection<DefectFeatureFilterOption>();
        public ObservableCollection<DefectFeatureValueItem> AllItems { get; } = new ObservableCollection<DefectFeatureValueItem>();
        public ObservableCollection<DefectFeatureValueItem> FilteredItems { get; } = new ObservableCollection<DefectFeatureValueItem>();

        private string _lengthColumnHeader = "长度";
        public string LengthColumnHeader
        {
            get { return _lengthColumnHeader; }
            set { SetProperty(ref _lengthColumnHeader, value); }
        }

        private string _widthColumnHeader = "宽度";
        public string WidthColumnHeader
        {
            get { return _widthColumnHeader; }
            set { SetProperty(ref _widthColumnHeader, value); }
        }

        private string _areaColumnHeader = "面积";
        public string AreaColumnHeader
        {
            get { return _areaColumnHeader; }
            set { SetProperty(ref _areaColumnHeader, value); }
        }

        private DefectFeatureFilterOption _selectedFilter;
        public DefectFeatureFilterOption SelectedFilter
        {
            get { return _selectedFilter; }
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    RefreshFilteredItems();
                }
            }
        }

        public string SummaryText
        {
            get
            {
                int passCount = FilteredItems.Count(item => item.IsMatched);
                return $"实例总数: {FilteredItems.Count}    通过规则: {passCount}";
            }
        }

        public DelegateCommand CloseCommand { get; }

        /// <summary>
        /// 初始化实例特征值弹窗视图模型。
        /// </summary>
        public DefectFeatureValuesViewModel()
        {
            CloseCommand = new DelegateCommand(() => CloseDialog(ButtonResult.OK));
        }

        /// <summary>
        /// 处理弹窗打开后的数据加载。
        /// </summary>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);
            UnsubscribeSourceModel();

            if (Param is not DefectFeatureValueDialogData dialogData)
            {
                return;
            }

            _sourceModel = dialogData.SourceModel;
            if (_sourceModel != null)
            {
                _sourceModel.PropertyChanged += SourceModel_PropertyChanged;
                LoadDialogData(_sourceModel.CreateFeatureValueDialogData());
                return;
            }

            LoadDialogData(dialogData);
        }

        /// <summary>
        /// 关闭弹窗时释放模型事件订阅。
        /// </summary>
        public override void OnDialogClosed()
        {
            UnsubscribeSourceModel();
            base.OnDialogClosed();
        }

        /// <summary>
        /// 刷新筛选后的实例列表。
        /// </summary>
        private void RefreshFilteredItems()
        {
            FilteredItems.Clear();

            var query = AllItems.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SelectedFilter?.RuleKey))
            {
                query = query.Where(item => item.RuleKey == SelectedFilter.RuleKey);
            }

            foreach (DefectFeatureValueItem item in query.OrderBy(item => item.ClassId).ThenBy(item => item.InstanceIndex))
            {
                FilteredItems.Add(item);
            }

            RaisePropertyChanged(nameof(SummaryText));
        }

        /// <summary>
        /// 构建表头显示文本。
        /// </summary>
        private static string BuildColumnHeader(string title, string unit)
        {
            return string.IsNullOrWhiteSpace(unit)
                ? title
                : $"{title}({unit})";
        }

        /// <summary>
        /// 装载弹窗展示数据，并尽量保持当前筛选项不变。
        /// </summary>
        private void LoadDialogData(DefectFeatureValueDialogData dialogData, string preferredRuleKey = null)
        {
            if (dialogData == null)
            {
                return;
            }

            LengthColumnHeader = BuildColumnHeader("长度", dialogData.LengthUnit);
            WidthColumnHeader = BuildColumnHeader("宽度", dialogData.WidthUnit);
            AreaColumnHeader = BuildColumnHeader("面积", dialogData.AreaUnit);

            string targetRuleKey = string.IsNullOrWhiteSpace(preferredRuleKey)
                ? dialogData.CurrentRuleKey
                : preferredRuleKey;

            FilterOptions.Clear();
            AllItems.Clear();

            FilterOptions.Add(new DefectFeatureFilterOption
            {
                RuleKey = string.Empty,
                DisplayName = "全部缺陷"
            });

            foreach (IGrouping<string, DefectFeatureValueItem> group in dialogData.Items
                         .GroupBy(item => item.RuleKey)
                         .OrderBy(item => item.First().ClassId))
            {
                DefectFeatureValueItem firstItem = group.First();
                FilterOptions.Add(new DefectFeatureFilterOption
                {
                    RuleKey = group.Key,
                    DisplayName = $"{firstItem.DefectName} (ClassId:{firstItem.ClassId})"
                });
            }

            foreach (DefectFeatureValueItem item in dialogData.Items)
            {
                AllItems.Add(item);
            }

            SelectedFilter = FilterOptions.FirstOrDefault(item => item.RuleKey == targetRuleKey) ?? FilterOptions.FirstOrDefault();
            if (SelectedFilter == null)
            {
                RefreshFilteredItems();
            }
        }

        /// <summary>
        /// 当源模型重新计算后，同步刷新弹窗中的实例特征数据。
        /// </summary>
        private void SourceModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_sourceModel == null || !ShouldReloadDialogData(e?.PropertyName))
            {
                return;
            }

            string selectedRuleKey = SelectedFilter?.RuleKey;
            Action reloadAction = () => LoadDialogData(_sourceModel.CreateFeatureValueDialogData(), selectedRuleKey);

            if (Application.Current?.Dispatcher?.CheckAccess() == true)
            {
                reloadAction();
                return;
            }

            Application.Current?.Dispatcher?.Invoke(reloadAction);
        }

        /// <summary>
        /// 判断当前属性变化是否需要刷新弹窗数据。
        /// </summary>
        private static bool ShouldReloadDialogData(string propertyName)
        {
            return propertyName == nameof(DefectPostProcessModel.FeatureValueDialogRefreshToken)
                || propertyName == nameof(DefectPostProcessModel.EdgeCalibrationX)
                || propertyName == nameof(DefectPostProcessModel.CurrentDefect);
        }

        /// <summary>
        /// 解除对源模型的事件订阅。
        /// </summary>
        private void UnsubscribeSourceModel()
        {
            if (_sourceModel == null)
            {
                return;
            }

            _sourceModel.PropertyChanged -= SourceModel_PropertyChanged;
            _sourceModel = null;
        }
    }
}
