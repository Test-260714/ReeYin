using Newtonsoft.Json;
using ReeYin_V.Core.Interfaces;
using System;

namespace LogicalTool.Conditional.Models
{
    /// <summary>
    /// 字符串判断类型
    /// </summary>
    public enum StringCheckType
    {
        Contains,           // 包含指定字符
        StartsWith,         // 以指定字符开头
        EndsWith,           // 以指定字符结尾
        Equals,             // 等于指定字符
        NotContains         // 不包含指定字符
    }

    /// <summary>
    /// JudgeCodition - 字符串操作相关属性
    /// </summary>
    public partial class JudgeCodition
    {
        #region 字符串分割

        [JsonIgnore]
        private bool _stringSplitEnabled;
        /// <summary>
        /// 启用字符串分割
        /// </summary>
        public bool StringSplitEnabled
        {
            get { return _stringSplitEnabled; }
            set { _stringSplitEnabled = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _splitDelimiter = ",";
        /// <summary>
        /// 分隔符
        /// </summary>
        public string SplitDelimiter
        {
            get { return _splitDelimiter; }
            set { _splitDelimiter = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _removeEmptyEntries = true;
        /// <summary>
        /// 移除空白项
        /// </summary>
        public bool RemoveEmptyEntries
        {
            get { return _removeEmptyEntries; }
            set { _removeEmptyEntries = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _trimEntries = true;
        /// <summary>
        /// 去除首尾空格
        /// </summary>
        public bool TrimEntries
        {
            get { return _trimEntries; }
            set { _trimEntries = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _maxSplitCount;
        /// <summary>
        /// 最大分割数量（0表示不限制）
        /// </summary>
        public int MaxSplitCount
        {
            get { return _maxSplitCount; }
            set { _maxSplitCount = value; RaisePropertyChanged(); }
        }

        #endregion

        #region 字符判断

        [JsonIgnore]
        private bool _stringCheckEnabled;
        /// <summary>
        /// 启用字符判断
        /// </summary>
        public bool StringCheckEnabled
        {
            get { return _stringCheckEnabled; }
            set { _stringCheckEnabled = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private StringCheckType _stringCheckType = StringCheckType.Contains;
        /// <summary>
        /// 字符判断类型
        /// </summary>
        public StringCheckType StringCheckType
        {
            get { return _stringCheckType; }
            set { _stringCheckType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _targetString = "";
        /// <summary>
        /// 目标字符
        /// </summary>
        public string TargetString
        {
            get { return _targetString; }
            set { _targetString = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _ignoreCase;
        /// <summary>
        /// 忽略大小写
        /// </summary>
        public bool IgnoreCase
        {
            get { return _ignoreCase; }
            set { _ignoreCase = value; RaisePropertyChanged(); }
        }

        #endregion

        #region 子字符串提取

        [JsonIgnore]
        private bool _substringEnabled;
        /// <summary>
        /// 启用子字符串提取
        /// </summary>
        public bool SubstringEnabled
        {
            get { return _substringEnabled; }
            set { _substringEnabled = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _substringStartIndex;
        /// <summary>
        /// 起始位置
        /// </summary>
        public int SubstringStartIndex
        {
            get { return _substringStartIndex; }
            set { _substringStartIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _substringLength;
        /// <summary>
        /// 提取长度（0表示提取到末尾）
        /// </summary>
        public int SubstringLength
        {
            get { return _substringLength; }
            set { _substringLength = value; RaisePropertyChanged(); }
        }

        #endregion
    }
}
