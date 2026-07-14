using Prism.Mvvm;
using ReeYin_V.Core.Services.Update;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ReeYin.RootManager.Models
{
    /// <summary>
    /// 更新模块数据模型
    /// </summary>
    public class UpdateModel : BindableBase
    {
        #region 在线更新配置

        private string _serverUrl = "http://localhost:5000/api/update";
        /// <summary>
        /// 更新服务器地址
        /// </summary>
        public string ServerUrl
        {
            get => _serverUrl;
            set => SetProperty(ref _serverUrl, value);
        }

        private string _licenseKey;
        /// <summary>
        /// 授权码
        /// </summary>
        public string LicenseKey
        {
            get => _licenseKey;
            set => SetProperty(ref _licenseKey, value);
        }

        private string _componentFilter;
        /// <summary>
        /// 组件名称筛选
        /// </summary>
        public string ComponentFilter
        {
            get => _componentFilter;
            set => SetProperty(ref _componentFilter, value);
        }

        private bool _includePreRelease;
        /// <summary>
        /// 是否包含预发布版本
        /// </summary>
        public bool IncludePreRelease
        {
            get => _includePreRelease;
            set => SetProperty(ref _includePreRelease, value);
        }

        #endregion

        #region 离线更新配置

        private string _offlinePackagePath;
        /// <summary>
        /// 离线更新包路径
        /// </summary>
        public string OfflinePackagePath
        {
            get => _offlinePackagePath;
            set => SetProperty(ref _offlinePackagePath, value);
        }

        private bool _isOfflineMode;
        /// <summary>
        /// 是否为离线模式
        /// </summary>
        public bool IsOfflineMode
        {
            get => _isOfflineMode;
            set => SetProperty(ref _isOfflineMode, value);
        }

        #endregion

        #region 更新状态

        private ObservableCollection<UpdatePackageInfo> _availableUpdates = new ObservableCollection<UpdatePackageInfo>();
        /// <summary>
        /// 可用更新列表
        /// </summary>
        public ObservableCollection<UpdatePackageInfo> AvailableUpdates
        {
            get => _availableUpdates;
            set => SetProperty(ref _availableUpdates, value);
        }

        private UpdatePackageInfo _selectedUpdate;
        /// <summary>
        /// 选中的更新包
        /// </summary>
        public UpdatePackageInfo SelectedUpdate
        {
            get => _selectedUpdate;
            set => SetProperty(ref _selectedUpdate, value);
        }

        private string _currentVersion;
        /// <summary>
        /// 当前版本
        /// </summary>
        public string CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        private bool _isChecking;
        /// <summary>
        /// 是否正在检查更新
        /// </summary>
        public bool IsChecking
        {
            get => _isChecking;
            set => SetProperty(ref _isChecking, value);
        }

        private bool _isUpdating;
        /// <summary>
        /// 是否正在更新
        /// </summary>
        public bool IsUpdating
        {
            get => _isUpdating;
            set => SetProperty(ref _isUpdating, value);
        }

        private int _updateProgress;
        /// <summary>
        /// 更新进度 (0-100)
        /// </summary>
        public int UpdateProgress
        {
            get => _updateProgress;
            set => SetProperty(ref _updateProgress, value);
        }

        private string _updateStatus;
        /// <summary>
        /// 更新状态消息
        /// </summary>
        public string UpdateStatus
        {
            get => _updateStatus;
            set => SetProperty(ref _updateStatus, value);
        }

        private UpdateStage _currentStage = UpdateStage.Checking;
        /// <summary>
        /// 当前更新阶段
        /// </summary>
        public UpdateStage CurrentStage
        {
            get => _currentStage;
            set => SetProperty(ref _currentStage, value);
        }

        #endregion

        #region 离线包信息

        private OfflinePackageInfo _offlinePackageInfo;
        /// <summary>
        /// 离线包信息
        /// </summary>
        public OfflinePackageInfo OfflinePackageInfo
        {
            get => _offlinePackageInfo;
            set => SetProperty(ref _offlinePackageInfo, value);
        }

        #endregion
    }

    /// <summary>
    /// 更新包清单
    /// </summary>
    public class PackageManifest
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string MinRequiredVersion { get; set; }
        public List<string> Files { get; set; }
        public string ChangeLog { get; set; }
    }

    /// <summary>
    /// 离线包信息
    /// </summary>
    public class OfflinePackageInfo : BindableBase
    {
        private string _filePath;
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        private long _fileSize;
        public long FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        private string _version;
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        private DateTime _createTime;
        public DateTime CreateTime
        {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        private string _validationMessage;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// 格式化的文件大小
        /// </summary>
        public string FormattedSize
        {
            get
            {
                if (FileSize < 1024) return $"{FileSize} B";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F2} KB";
                if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024):F2} MB";
                return $"{FileSize / (1024.0 * 1024 * 1024):F2} GB";
            }
        }
    }
}
