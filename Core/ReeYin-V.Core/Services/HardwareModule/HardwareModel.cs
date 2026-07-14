using Prism.Mvvm;
using ReeYin_V.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Module
{
    public class HardwareConfigItem : BindableBase
    {
        private string _title;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _icon;
        public string Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private HardwareType _hardType;
        public HardwareType HardType
        {
            get => _hardType;
            set => SetProperty(ref _hardType, value);
        }

        private ConfigKey _config;
        public ConfigKey Config
        {
            get => _config;
            set => SetProperty(ref _config, value);
        }

        /// <summary>
        /// 导航事件
        /// </summary>
        public Action Navigation { get; set; }
    }

    /// <summary>
    /// 硬件状态
    /// </summary>
    public class HardwareStatus : BindableBase
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        private bool _isConnect;

        public bool IsConnect
        {
            get { return _isConnect; }
            set { _isConnect = value; RaisePropertyChanged(); }
        }

        private HardwareState _status;

        public HardwareState Status
        {
            get { return _status; }
            set { _status = value; RaisePropertyChanged(); }
        }

        private string _describe;

        public string Describe
        {
            get { return _describe; }
            set { _describe = value; RaisePropertyChanged(); }
        }

        public string SourceType { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string ErrorCode { get; set; } = string.Empty;

        public string Operation { get; set; } = string.Empty;

#nullable enable annotations
        public IDictionary<string, object?> ExtraData { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
#nullable restore annotations

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
