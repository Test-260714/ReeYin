using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.IO;

namespace ReeYin.ProjectManager.Models
{
    /// <summary>
    /// 解决方案信息模型
    /// </summary>
    [Serializable]
    public class SolutionInfo : BindableBase
    {
        private string _name;
        /// <summary>
        /// 解决方案名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private Guid _guid = Guid.NewGuid();
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public Guid Guid
        {
            get => _guid;
            set => SetProperty(ref _guid, value);
        }

        private string _description;
        /// <summary>
        /// 描述信息
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _filePath;
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set
            {
                SetProperty(ref _filePath, value);
                RaisePropertyChanged(nameof(FileExists));
            }
        }

        private DateTime _lastModified = DateTime.Now;
        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModified
        {
            get => _lastModified;
            set => SetProperty(ref _lastModified, value);
        }

        private bool _isCurrent;
        /// <summary>
        /// 是否为当前解决方案
        /// </summary>
        [JsonIgnore]
        public bool IsCurrent
        {
            get => _isCurrent;
            set => SetProperty(ref _isCurrent, value);
        }

        /// <summary>
        /// 文件是否存在
        /// </summary>
        [JsonIgnore]
        public bool FileExists => !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

        /// <summary>
        /// 显示用的最后修改时间
        /// </summary>
        [JsonIgnore]
        public string DisplayLastModified
        {
            get
            {
                var span = DateTime.Now - LastModified;
                if (span.TotalMinutes < 1) return "刚刚";
                if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} 分钟前";
                if (span.TotalDays < 1) return $"{(int)span.TotalHours} 小时前";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays} 天前";
                return LastModified.ToString("yyyy-MM-dd");
            }
        }
    }
}
