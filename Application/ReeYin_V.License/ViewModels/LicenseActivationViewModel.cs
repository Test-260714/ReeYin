using Microsoft.Win32;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.IOC;
using ReeYin_V.License.Models;
using ReeYin_V.License.Services;
using System;
using System.Linq;
using System.Windows.Media;

namespace ReeYin_V.License.ViewModels
{
    /// <summary>授权激活弹窗的视图模型。</summary>
    public sealed class LicenseActivationViewModel : DialogViewModelBase
    {
        /// <summary>授权业务服务。</summary>
        private readonly ILicenseService _licenseService;

        /// <summary>授权有效时的状态颜色。</summary>
        private static readonly Brush StatusOkBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0xB8, 0x67));
        /// <summary>授权即将过期或已过期时的状态颜色。</summary>
        private static readonly Brush StatusWarnBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23));
        /// <summary>授权无效时的状态颜色。</summary>
        private static readonly Brush StatusErrorBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0x4F, 0x5F));

        /// <summary>页面展示的授权状态字段。</summary>
        private string _machineCode = string.Empty;
        /// <summary>当前授权文件路径显示值。</summary>
        private string _licenseFilePath = string.Empty;
        private string _statusText = "未校验";
        /// <summary>当前状态颜色。</summary>
        private Brush _statusBrush = StatusWarnBrush;
        /// <summary>当前客户名称显示值。</summary>
        private string _customerName = "--";
        /// <summary>当前授权类型显示值。</summary>
        private string _licenseType = "--";
        /// <summary>当前到期时间显示值。</summary>
        private string _expireTime = "--";
        /// <summary>当前版本显示值。</summary>
        private string _version = "--";
        /// <summary>当前模块权限显示值。</summary>
        private string _modules = "--";
        /// <summary>当前最近校验时间显示值。</summary>
        private string _lastCheckTime = "--";
        /// <summary>当前授权是否有效。</summary>
        private bool _isActivated;

        /// <summary>创建授权激活视图模型，并初始化页面数据。</summary>
        public LicenseActivationViewModel(ILicenseService licenseService)
        {
            _licenseService = licenseService;
            ImportLicenseCommand = new DelegateCommand(ImportLicense);
            RefreshMachineCodeCommand = new DelegateCommand(RefreshMachineCode);
            ValidateCurrentLicenseCommand = new DelegateCommand(ValidateCurrentLicense);
            CloseCommand = new DelegateCommand(CloseActivationDialog);
            RefreshMachineCode();
            ValidateCurrentLicense();
        }

        /// <summary>当前设备机器码。</summary>
        public string MachineCode
        {
            get => _machineCode;
            set => SetProperty(ref _machineCode, value);
        }

        /// <summary>当前授权文件路径。</summary>
        public string LicenseFilePath
        {
            get => _licenseFilePath;
            set => SetProperty(ref _licenseFilePath, value);
        }

        /// <summary>授权状态文案。</summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>授权状态颜色。</summary>
        public Brush StatusBrush
        {
            get => _statusBrush;
            set => SetProperty(ref _statusBrush, value);
        }

        /// <summary>客户名称。</summary>
        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        /// <summary>授权类型。</summary>
        public string LicenseType
        {
            get => _licenseType;
            set => SetProperty(ref _licenseType, value);
        }

        /// <summary>到期时间。</summary>
        public string ExpireTime
        {
            get => _expireTime;
            set => SetProperty(ref _expireTime, value);
        }

        /// <summary>授权版本。</summary>
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        /// <summary>授权模块列表。</summary>
        public string Modules
        {
            get => _modules;
            set => SetProperty(ref _modules, value);
        }

        /// <summary>最近校验时间。</summary>
        public string LastCheckTime
        {
            get => _lastCheckTime;
            set => SetProperty(ref _lastCheckTime, value);
        }

        /// <summary>当前授权是否激活。</summary>
        public bool IsActivated
        {
            get => _isActivated;
            set => SetProperty(ref _isActivated, value);
        }

        /// <summary>导入授权文件命令。</summary>
        public DelegateCommand ImportLicenseCommand { get; }

        /// <summary>刷新机器码命令。</summary>
        public DelegateCommand RefreshMachineCodeCommand { get; }

        /// <summary>重新校验授权命令。</summary>
        public DelegateCommand ValidateCurrentLicenseCommand { get; }

        /// <summary>关闭弹窗命令。</summary>
        public DelegateCommand CloseCommand { get; }

        /// <summary>对话框打开时补充默认标题。</summary>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);
            if (string.IsNullOrWhiteSpace(Title))
            {
                Title = "许可证激活";
            }
        }

        /// <summary>读取并显示当前机器码。</summary>
        private void RefreshMachineCode()
        {
            MachineCode = _licenseService.GetCurrentMachineCode();
        }

        /// <summary>校验当前授权并刷新页面数据。</summary>
        private void ValidateCurrentLicense()
        {
            var result = _licenseService.ValidateCurrentLicense();
            LicenseFilePath = _licenseService.LicenseFilePath;
            ApplyValidationResult(result);
        }

        /// <summary>选择授权文件并执行导入。</summary>
        private void ImportLicense()
        {
            var dialog = new OpenFileDialog
            {
                Title = "导入 License 文件",
                Filter = "License 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var result = _licenseService.ImportLicense(dialog.FileName);
            LicenseFilePath = _licenseService.LicenseFilePath;
            ApplyValidationResult(result);
        }

        /// <summary>将校验结果映射到界面显示字段。</summary>
        private void ApplyValidationResult(LicenseValidationResult result)
        {
            IsActivated = result.IsValid;
            LastCheckTime = result.CheckedAt.ToString("yyyy-MM-dd HH:mm:ss");

            if (result.IsValid)
            {
                StatusBrush = StatusOkBrush;
                StatusText = $"授权有效: {result.Message}";
            }
            else if (result.Status == LicenseValidationStatus.Expired)
            {
                StatusBrush = StatusWarnBrush;
                StatusText = result.Message;
            }
            else
            {
                StatusBrush = StatusErrorBrush;
                StatusText = result.Message;
            }

            var license = result.License;
            if (license == null)
            {
                CustomerName = "--";
                LicenseType = "--";
                ExpireTime = "--";
                Version = "--";
                Modules = "--";
                return;
            }

            CustomerName = string.IsNullOrWhiteSpace(license.CustomerName) ? "--" : license.CustomerName;
            LicenseType = license.Type switch
            {
                ReeYin_V.License.Models.LicenseType.Permanent => "永久授权",
                ReeYin_V.License.Models.LicenseType.TimeLimited => "时效授权",
                ReeYin_V.License.Models.LicenseType.Trial => "试用授权",
                _ => "未知"
            };

            ExpireTime = license.ExpireTime.HasValue
                ? license.ExpireTime.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : "永不过期";

            Version = string.IsNullOrWhiteSpace(license.Version) ? "--" : license.Version;
            Modules = license.NormalizedModules.Count == 0
                ? "全部模块"
                : string.Join(" / ", license.NormalizedModules.OrderBy(static item => item, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>关闭授权激活弹窗。</summary>
        private void CloseActivationDialog()
        {
            CloseDialog(ButtonResult.OK);
        }
    }
}
