using LogicalTool.Conditional.Models;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Windows.Media;

namespace LogicalTool.Conditional.ViewModels
{
    [Serializable]
    public class IntConditionViewModel : BindableBase, IViewModuleParam, INavigationAware
    {
        #region Fields

        #endregion

        #region Properties
        private JudgeCodition _curCodition;

        public JudgeCodition CurCodition
        {
            get { return _curCodition; }
            set
            {
                _curCodition = value;
                RaisePropertyChanged();
                UpdateResultPreviews();
            }
        }

        private ConditionModel _modelParam;

        public ConditionModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private string _compareResultText = "未计算";
        public string CompareResultText
        {
            get { return _compareResultText; }
            set { _compareResultText = value; RaisePropertyChanged(); }
        }

        private Brush _compareResultColor = Brushes.Gray;
        public Brush CompareResultColor
        {
            get { return _compareResultColor; }
            set { _compareResultColor = value; RaisePropertyChanged(); }
        }

        private string _rangeResultText = "未计算";
        public string RangeResultText
        {
            get { return _rangeResultText; }
            set { _rangeResultText = value; RaisePropertyChanged(); }
        }

        private Brush _rangeResultColor = Brushes.Gray;
        public Brush RangeResultColor
        {
            get { return _rangeResultColor; }
            set { _rangeResultColor = value; RaisePropertyChanged(); }
        }

        private string _operationResultText = "未计算";
        public string OperationResultText
        {
            get { return _operationResultText; }
            set { _operationResultText = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public IntConditionViewModel()
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

        /// <summary>
        /// 更新结果预览
        /// </summary>
        private void UpdateResultPreviews()
        {
            if (CurCodition?.Variable?.Value == null)
            {
                CompareResultText = "无数据";
                RangeResultText = "无数据";
                OperationResultText = "无数据";
                return;
            }

            if (!int.TryParse(CurCodition.Variable.Value?.ToString(), out int currentValue))
            {
                CompareResultText = "非整数";
                RangeResultText = "非整数";
                OperationResultText = "非整数";
                return;
            }

            // 比较判断结果
            if (CurCodition.IntCompareEnabled)
            {
                bool compareResult = EvaluateCompare(currentValue, CurCodition.IntCompareType, CurCodition.IntCompareValue);
                CompareResultText = compareResult ? "True (满足条件)" : "False (不满足条件)";
                CompareResultColor = compareResult ? Brushes.Green : Brushes.Red;
            }
            else
            {
                CompareResultText = "未启用";
                CompareResultColor = Brushes.Gray;
            }

            // 范围判断结果
            if (CurCodition.IntRangeEnabled)
            {
                bool rangeResult = EvaluateRange(currentValue, CurCodition.IntRangeMin, CurCodition.IntRangeMax,
                    CurCodition.IntRangeIncludeMin, CurCodition.IntRangeIncludeMax);
                RangeResultText = rangeResult ? "True (在范围内)" : "False (不在范围内)";
                RangeResultColor = rangeResult ? Brushes.Green : Brushes.Red;
            }
            else
            {
                RangeResultText = "未启用";
                RangeResultColor = Brushes.Gray;
            }

            // 运算结果
            if (CurCodition.IntOperationEnabled)
            {
                int operationResult = EvaluateOperation(currentValue, CurCodition.IntOperationType, CurCodition.IntOperationValue);
                OperationResultText = operationResult.ToString();
            }
            else
            {
                OperationResultText = "未启用";
            }
        }

        /// <summary>
        /// 执行比较判断
        /// </summary>
        private bool EvaluateCompare(int currentValue, IntCompareType compareType, int compareValue)
        {
            return compareType switch
            {
                IntCompareType.Equal => currentValue == compareValue,
                IntCompareType.NotEqual => currentValue != compareValue,
                IntCompareType.GreaterThan => currentValue > compareValue,
                IntCompareType.GreaterThanOrEqual => currentValue >= compareValue,
                IntCompareType.LessThan => currentValue < compareValue,
                IntCompareType.LessThanOrEqual => currentValue <= compareValue,
                _ => false
            };
        }

        /// <summary>
        /// 执行范围判断
        /// </summary>
        private bool EvaluateRange(int currentValue, int min, int max, bool includeMin, bool includeMax)
        {
            bool minCheck = includeMin ? currentValue >= min : currentValue > min;
            bool maxCheck = includeMax ? currentValue <= max : currentValue < max;
            return minCheck && maxCheck;
        }

        /// <summary>
        /// 执行数值运算
        /// </summary>
        private int EvaluateOperation(int currentValue, IntOperationType operationType, int operationValue)
        {
            return operationType switch
            {
                IntOperationType.Add => currentValue + operationValue,
                IntOperationType.Subtract => currentValue - operationValue,
                IntOperationType.Multiply => currentValue * operationValue,
                IntOperationType.Divide => operationValue != 0 ? currentValue / operationValue : 0,
                IntOperationType.Modulo => operationValue != 0 ? currentValue % operationValue : 0,
                _ => currentValue
            };
        }

        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "测试判断":
                    UpdateResultPreviews();
                    break;

                case "追加至输出":
                    AddToOutput();
                    break;

                case "取消":
                    break;

                default:
                    break;
            }
        });

        /// <summary>
        /// 追加到输出参数
        /// </summary>
        private void AddToOutput()
        {
            if (CurCodition?.Variable?.Value == null) return;

            if (!int.TryParse(CurCodition.Variable.Value?.ToString(), out int currentValue))
                return;

            // 根据启用的功能添加输出
            if (CurCodition.IntCompareEnabled)
            {
                bool compareResult = EvaluateCompare(currentValue, CurCodition.IntCompareType, CurCodition.IntCompareValue);
                ModelParam.OutputParams.Add(new TransmitParam
                {
                    Name = $"{CurCodition.Variable.Name}_比较结果",
                    Value = compareResult,
                    Type = DataType.Bool,
                    Describe = $"比较判断: {currentValue} {GetCompareSymbol(CurCodition.IntCompareType)} {CurCodition.IntCompareValue}"
                });
            }

            if (CurCodition.IntRangeEnabled)
            {
                bool rangeResult = EvaluateRange(currentValue, CurCodition.IntRangeMin, CurCodition.IntRangeMax,
                    CurCodition.IntRangeIncludeMin, CurCodition.IntRangeIncludeMax);
                ModelParam.OutputParams.Add(new TransmitParam
                {
                    Name = $"{CurCodition.Variable.Name}_范围结果",
                    Value = rangeResult,
                    Type = DataType.Bool,
                    Describe = $"范围判断: {CurCodition.IntRangeMin} ~ {CurCodition.IntRangeMax}"
                });
            }

            if (CurCodition.IntOperationEnabled)
            {
                int operationResult = EvaluateOperation(currentValue, CurCodition.IntOperationType, CurCodition.IntOperationValue);
                ModelParam.OutputParams.Add(new TransmitParam
                {
                    Name = $"{CurCodition.Variable.Name}_运算结果",
                    Value = operationResult,
                    Type = DataType.Int,
                    Describe = $"运算: {currentValue} {GetOperationSymbol(CurCodition.IntOperationType)} {CurCodition.IntOperationValue}"
                });
            }
        }

        private string GetCompareSymbol(IntCompareType type)
        {
            return type switch
            {
                IntCompareType.Equal => "==",
                IntCompareType.NotEqual => "!=",
                IntCompareType.GreaterThan => ">",
                IntCompareType.GreaterThanOrEqual => ">=",
                IntCompareType.LessThan => "<",
                IntCompareType.LessThanOrEqual => "<=",
                _ => "?"
            };
        }

        private string GetOperationSymbol(IntOperationType type)
        {
            return type switch
            {
                IntOperationType.Add => "+",
                IntOperationType.Subtract => "-",
                IntOperationType.Multiply => "*",
                IntOperationType.Divide => "/",
                IntOperationType.Modulo => "%",
                _ => "?"
            };
        }
        #endregion
    }
}
