using Custom.WaferFlatnessMeasure.Models;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.IOC;
using System;

namespace Custom.WaferFlatnessMeasure.ViewModels
{
    public class DataAnalysisSourceFilterSettingViewModel : DialogViewModelBase
    {
        private DataAnalysisDataSourceOption? _source;

        private string _dataSourceName = string.Empty;
        public string DataSourceName
        {
            get { return _dataSourceName; }
            set { SetProperty(ref _dataSourceName, value); }
        }

        private string _originalDataValueName = string.Empty;
        public string OriginalDataValueName
        {
            get { return _originalDataValueName; }
            set { SetProperty(ref _originalDataValueName, value); }
        }

        private bool _isFilterEnabled;
        public bool IsFilterEnabled
        {
            get { return _isFilterEnabled; }
            set { SetProperty(ref _isFilterEnabled, value); }
        }

        private bool _isRawDataFilter;
        public bool IsRawDataFilter
        {
            get { return _isRawDataFilter; }
            set { SetProperty(ref _isRawDataFilter, value); }
        }

        private double _filterMin;
        public double FilterMin
        {
            get { return _filterMin; }
            set { SetProperty(ref _filterMin, value); }
        }

        private double _filterMax;
        public double FilterMax
        {
            get { return _filterMax; }
            set { SetProperty(ref _filterMax, value); }
        }

        private int _pointCollectionTrimCountPerSide = 2;
        public int PointCollectionTrimCountPerSide
        {
            get { return _pointCollectionTrimCountPerSide; }
            set { SetProperty(ref _pointCollectionTrimCountPerSide, Math.Max(0, value)); }
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            string title = parameters.GetValue<string>("Title");
            string icon = parameters.GetValue<string>("Icon");
            Icon = string.IsNullOrWhiteSpace(icon) ? "\ue640" : icon;
            Title = $"{Icon} {(string.IsNullOrWhiteSpace(title) ? "数据过滤设置" : title)}";

            _source = parameters.GetValue<DataAnalysisDataSourceOption>("DataSource");
            LoadFromSource();
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>(order =>
        {
            switch (order)
            {
                case "OK":
                    ApplyToSource();
                    CloseDialog(ButtonResult.OK);
                    break;
                case "Cancel":
                    CloseDialog(ButtonResult.Cancel);
                    break;
            }
        });

        private void LoadFromSource()
        {
            if (_source == null)
            {
                DataSourceName = string.Empty;
                OriginalDataValueName = string.Empty;
                IsFilterEnabled = false;
                IsRawDataFilter = false;
                FilterMin = -99999999d;
                FilterMax = 99999999d;
                PointCollectionTrimCountPerSide = 2;
                return;
            }

            DataSourceName = _source.Name;
            OriginalDataValueName = _source.OriginalDataValueName;
            IsFilterEnabled = _source.IsFilterEnabled;
            IsRawDataFilter = _source.IsRawDataFilter;
            FilterMin = _source.FilterMin;
            FilterMax = _source.FilterMax;
            PointCollectionTrimCountPerSide = _source.PointCollectionTrimCountPerSide;
        }

        private void ApplyToSource()
        {
            if (_source == null)
            {
                return;
            }

            _source.IsFilterEnabled = IsFilterEnabled;
            _source.IsRawDataFilter = IsRawDataFilter;
            _source.FilterMin = FilterMin;
            _source.FilterMax = FilterMax;
            _source.PointCollectionTrimCountPerSide = PointCollectionTrimCountPerSide;
        }
    }
}
