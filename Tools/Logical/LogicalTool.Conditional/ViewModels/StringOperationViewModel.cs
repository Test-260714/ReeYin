using LogicalTool.Conditional.Models;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Linq;
using System.Windows.Media;

namespace LogicalTool.Conditional.ViewModels
{
    [Serializable]
    public class StringOperationViewModel : BindableBase, IViewModuleParam, INavigationAware
    {
        #region Properties
        private JudgeCodition _curCodition;

        public JudgeCodition CurCodition
        {
            get { return _curCodition; }
            set
            {
                _curCodition = value;
                RaisePropertyChanged();
                UpdateStringLength();
                UpdateResultPreviews();
            }
        }

        private ConditionModel _modelParam;

        public ConditionModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private int _stringLength;

        public int StringLength
        {
            get { return _stringLength; }
            set { _stringLength = value; RaisePropertyChanged(); }
        }

        // 结果预览
        private string _splitResultText = "未计算";
        public string SplitResultText
        {
            get { return _splitResultText; }
            set { _splitResultText = value; RaisePropertyChanged(); }
        }

        private Brush _splitResultColor = Brushes.Gray;
        public Brush SplitResultColor
        {
            get { return _splitResultColor; }
            set { _splitResultColor = value; RaisePropertyChanged(); }
        }

        private string _checkResultText = "未计算";
        public string CheckResultText
        {
            get { return _checkResultText; }
            set { _checkResultText = value; RaisePropertyChanged(); }
        }

        private Brush _checkResultColor = Brushes.Gray;
        public Brush CheckResultColor
        {
            get { return _checkResultColor; }
            set { _checkResultColor = value; RaisePropertyChanged(); }
        }

        private string _substringResultText = "未计算";
        public string SubstringResultText
        {
            get { return _substringResultText; }
            set { _substringResultText = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public StringOperationViewModel()
        {
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            ModelParam = navigationContext.Parameters.GetValue<ConditionModel>("ModelParam");
            CurCodition = ModelParam.SltJudgeCodition;
            UpdateResultPreviews();
        }
        #endregion

        #region Methods

        private void UpdateStringLength()
        {
            StringLength = CurCodition?.Variable?.Value?.ToString()?.Length ?? 0;
        }

        /// <summary>
        /// 更新所有结果预览
        /// </summary>
        private void UpdateResultPreviews()
        {
            if (CurCodition?.Variable?.Value == null)
            {
                SplitResultText = "无数据";
                CheckResultText = "无数据";
                SubstringResultText = "无数据";
                return;
            }

            string source = CurCodition.Variable.Value.ToString();

            // 分割预览
            if (CurCodition.StringSplitEnabled)
            {
                var result = PerformSplit(source);
                SplitResultText = $"共 {result.Length} 项";
                SplitResultColor = Brushes.Green;
            }
            else
            {
                SplitResultText = "未启用";
                SplitResultColor = Brushes.Gray;
            }

            // 判断预览
            if (CurCodition.StringCheckEnabled)
            {
                bool result = PerformCharCheck(source);
                CheckResultText = result ? "True (满足条件)" : "False (不满足条件)";
                CheckResultColor = result ? Brushes.Green : Brushes.Red;
            }
            else
            {
                CheckResultText = "未启用";
                CheckResultColor = Brushes.Gray;
            }

            // 提取预览
            if (CurCodition.SubstringEnabled)
            {
                SubstringResultText = PerformSubstring(source);
                if (string.IsNullOrEmpty(SubstringResultText))
                    SubstringResultText = "(空)";
            }
            else
            {
                SubstringResultText = "未启用";
            }
        }

        private bool PerformCharCheck(string source)
        {
            if (string.IsNullOrEmpty(CurCodition.TargetString)) return false;

            var comparison = CurCodition.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return CurCodition.StringCheckType switch
            {
                StringCheckType.Contains => source.Contains(CurCodition.TargetString, comparison),
                StringCheckType.StartsWith => source.StartsWith(CurCodition.TargetString, comparison),
                StringCheckType.EndsWith => source.EndsWith(CurCodition.TargetString, comparison),
                StringCheckType.Equals => source.Equals(CurCodition.TargetString, comparison),
                StringCheckType.NotContains => !source.Contains(CurCodition.TargetString, comparison),
                _ => false
            };
        }

        private string[] PerformSplit(string source)
        {
            if (string.IsNullOrEmpty(CurCodition.SplitDelimiter)) return new[] { source };

            var options = CurCodition.RemoveEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;

            string[] result = CurCodition.MaxSplitCount > 0
                ? source.Split(new[] { CurCodition.SplitDelimiter }, CurCodition.MaxSplitCount, options)
                : source.Split(new[] { CurCodition.SplitDelimiter }, options);

            if (CurCodition.TrimEntries)
                result = result.Select(s => s.Trim()).ToArray();

            return result;
        }

        private string PerformSubstring(string source)
        {
            if (CurCodition.SubstringStartIndex >= source.Length) return string.Empty;

            if (CurCodition.SubstringLength == 0 || CurCodition.SubstringStartIndex + CurCodition.SubstringLength > source.Length)
                return source.Substring(CurCodition.SubstringStartIndex);

            return source.Substring(CurCodition.SubstringStartIndex, CurCodition.SubstringLength);
        }

        #endregion

        #region Commands
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "测试操作":
                    UpdateResultPreviews();
                    break;

                case "追加至输出":
                    AddToOutput();
                    break;
            }
        });

        private void AddToOutput()
        {
            if (CurCodition?.Variable?.Value == null) return;
            string source = CurCodition.Variable.Value.ToString();

            if (CurCodition.StringSplitEnabled)
            {
                var splitResult = PerformSplit(source);
                ModelParam.OutputParams.Add(new TransmitParam
                {
                    Name = $"{CurCodition.Variable.Name}_分割结果",
                    Value = splitResult,
                    Type = DataType.Array,
                    Describe = $"分割: \"{CurCodition.SplitDelimiter}\" -> {splitResult.Length} 项"
                });
            }

            if (CurCodition.StringCheckEnabled)
            {
                bool checkResult = PerformCharCheck(source);
                CurCodition.IsSatisfy = checkResult;
                ModelParam.OutputParams.Add(new TransmitParam
                {
                    Name = $"{CurCodition.Variable.Name}_判断结果",
                    Value = checkResult,
                    Type = DataType.Bool,
                    Describe = $"字符判断: {CurCodition.StringCheckType} \"{CurCodition.TargetString}\""
                });
            }

            if (CurCodition.SubstringEnabled)
            {
                string substringResult = PerformSubstring(source);
                ModelParam.OutputParams.Add(new TransmitParam
                {
                    Name = $"{CurCodition.Variable.Name}_提取结果",
                    Value = substringResult,
                    Type = DataType.String,
                    Describe = $"提取: [{CurCodition.SubstringStartIndex}, {CurCodition.SubstringLength}]"
                });
            }
        }
        #endregion
    }
}
