using NetTaste;
using Newtonsoft.Json;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.Conditional.Models
{

    [Serializable]
    public class ConditionModel : ModelParamBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private ObservableCollection<JudgeCodition> _allJudgeCodition = new ObservableCollection<JudgeCodition>();

        public ObservableCollection<JudgeCodition> AllJudgeCodition
        {
            get { return _allJudgeCodition; }
            set { _allJudgeCodition = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private JudgeCodition _sltJudgeCodition = new JudgeCodition();

        public JudgeCodition SltJudgeCodition
        {
            get { return _sltJudgeCodition; }
            set { _sltJudgeCodition = value; RaisePropertyChanged(); }
        }


        #endregion

        #region Constructor
        public ConditionModel()
        {
            TriggerModuleRun += () =>
            {
                return ExecuteModule().Result;
            };
        }
        #endregion

        #region Methods
        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            ObservableCollection<TransmitParam> outputParams = new ObservableCollection<TransmitParam>();
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                var result = NodeStatus.Success;
                try
                {
                    #region 检测参数（对链接参数重新赋值）
                    Console.WriteLine($"开始加载参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                    LoadKeyParam();
                    Console.WriteLine($"结束加载参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                    #endregion

                    foreach (var codition in AllJudgeCodition)
                    {
                        if (codition.IsUsing)
                        {
                            switch (codition.Variable.Type)
                            {
                                case DataType.List:
                                    {
                                        if (codition.IsOutputSingle)
                                        {
                                            if (codition.Variable.Value is IList list)
                                            {
                                                // 注意做边界检查
                                                var value = list[codition.SingleIndex];   // value 类型是 object（装箱/引用）
                                                OutputParams.Add(new TransmitParam
                                                {
                                                    Value = value,
                                                });
                                            }
                                        }
                                    }
                                    break;

                                case DataType.Int:
                                    {
                                        outputParams.AddRange(ExecuteIntCondition(codition));

                                        if (!codition.IsSatisfy)
                                        {
                                            result = NodeStatus.Failed;
                                        }
                                    }
                                    break;

                                case DataType.String:
                                    {
                                        var stringOutputs = ExecuteStringCondition(codition);
                                        if (stringOutputs != null)
                                        {
                                            foreach (var p in stringOutputs)
                                                outputParams.Add(p);
                                        }
                                        if (!codition.IsSatisfy)
                                        {
                                            result = NodeStatus.Failed;
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace.ToString());
                    return NodeStatus.Error;
                }
                #region 输出

                Console.WriteLine($"开始输出参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");

                //执行后对输出参数重新赋值
                OutputParams = outputParams;
                //foreach (var item in OutputParams)
                //{
                //    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                //}
                Console.WriteLine($"完成赋值时间：{DateTime.Now.ToString($"HH:mm:ss.fff")}");


                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }
                Console.WriteLine($"结束输出参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                #endregion

                return result;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：找直线模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        /// <summary>
        /// 执行Int类型条件判断
        /// </summary>
        private ObservableCollection<TransmitParam> ExecuteIntCondition(JudgeCodition codition)
        {
            ObservableCollection<TransmitParam> outputParams = new ObservableCollection<TransmitParam>();
            if (codition.Variable?.Value == null) return null;

            if (!int.TryParse(codition.Variable.Value?.ToString(), out int currentValue))
                return null;

            // 比较判断
            if (codition.IntCompareEnabled)
            {
                bool compareResult = EvaluateIntCompare(currentValue, codition.IntCompareType, codition.IntCompareValue);
                codition.IsSatisfy = compareResult;
                outputParams.Add(new TransmitParam
                {
                    Name = $"{codition.Variable.Name}_比较结果",
                    Value = compareResult,
                    Type = DataType.Bool,
                    Describe = $"比较判断: {currentValue} {GetCompareSymbol(codition.IntCompareType)} {codition.IntCompareValue}"
                });
            }

            // 范围判断
            if (codition.IntRangeEnabled)
            {
                bool rangeResult = EvaluateIntRange(currentValue, codition.IntRangeMin, codition.IntRangeMax,
                    codition.IntRangeIncludeMin, codition.IntRangeIncludeMax);
                codition.IsSatisfy = rangeResult;
                outputParams.Add(new TransmitParam
                {
                    Name = $"{codition.Variable.Name}_范围结果",
                    Value = rangeResult,
                    Type = DataType.Bool,
                    Describe = $"范围判断: {codition.IntRangeMin} ~ {codition.IntRangeMax}"
                });
            }

            // 数值运算
            if (codition.IntOperationEnabled)
            {
                int operationResult = EvaluateIntOperation(currentValue, codition.IntOperationType, codition.IntOperationValue);
                codition.IsSatisfy = true;
                outputParams.Add(new TransmitParam
                {
                    Name = $"{codition.Variable.Name}_运算结果",
                    Value = operationResult,
                    Type = DataType.Int,
                    Describe = $"运算: {currentValue} {GetOperationSymbol(codition.IntOperationType)} {codition.IntOperationValue}"
                });
            }

            return outputParams;
        }

        /// <summary>
        /// 执行Int比较判断
        /// </summary>
        private bool EvaluateIntCompare(int currentValue, IntCompareType compareType, int compareValue)
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
        /// 执行Int范围判断
        /// </summary>
        private bool EvaluateIntRange(int currentValue, int min, int max, bool includeMin, bool includeMax)
        {
            bool minCheck = includeMin ? currentValue >= min : currentValue > min;
            bool maxCheck = includeMax ? currentValue <= max : currentValue < max;
            return minCheck && maxCheck;
        }

        /// <summary>
        /// 执行Int数值运算
        /// </summary>
        private int EvaluateIntOperation(int currentValue, IntOperationType operationType, int operationValue)
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

        /// <summary>
        /// 执行String类型条件判断（字符判断、分割、子串提取）
        /// </summary>
        private ObservableCollection<TransmitParam> ExecuteStringCondition(JudgeCodition codition)
        {
            ObservableCollection<TransmitParam> outputParams = new ObservableCollection<TransmitParam>();
            if (codition.Variable?.Value == null) return null;

            string source = codition.Variable.Value.ToString();

            // 字符判断
            if (codition.StringCheckEnabled)
            {
                bool checkResult = EvaluateStringCheck(source, codition);
                codition.IsSatisfy = checkResult;
                outputParams.Add(new TransmitParam
                {
                    Name = $"{codition.Variable.Name}_判断结果",
                    Value = checkResult,
                    Type = DataType.Bool,
                    Describe = $"字符判断: {codition.StringCheckType} \"{codition.TargetString}\""
                });
            }

            // 字符串分割
            if (codition.StringSplitEnabled)
            {
                string[] splitResult = PerformStringSplit(source, codition);
                string splitresultvalue = "[" + string.Join(", ", splitResult) + "]"; 
                outputParams.Add(new TransmitParam
                {
                    Name = $"{codition.Variable.Name}_分割结果",
                    Value = splitresultvalue,
                    Type = DataType.Array,
                    Describe = $"分割: \"{codition.SplitDelimiter}\" → {splitResult.Length} 项"
                });
            }

            // 子字符串提取
            if (codition.SubstringEnabled)
            {
                string substringResult = PerformSubstring(source, codition);
                outputParams.Add(new TransmitParam
                {
                    Name = $"{codition.Variable.Name}_提取结果",
                    Value = substringResult,
                    Type = DataType.String,
                    Describe = $"提取: [{codition.SubstringStartIndex}, {codition.SubstringLength}]"
                });
            }

            return outputParams;
        }

        /// <summary>
        /// String字符判断
        /// </summary>
        private bool EvaluateStringCheck(string source, JudgeCodition codition)
        {
            if (string.IsNullOrEmpty(codition.TargetString)) return false;

            var comparison = codition.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return codition.StringCheckType switch
            {
                StringCheckType.Contains => source.Contains(codition.TargetString, comparison),
                StringCheckType.StartsWith => source.StartsWith(codition.TargetString, comparison),
                StringCheckType.EndsWith => source.EndsWith(codition.TargetString, comparison),
                StringCheckType.Equals => source.Equals(codition.TargetString, comparison),
                StringCheckType.NotContains => !source.Contains(codition.TargetString, comparison),
                _ => false
            };
        }

        /// <summary>
        /// String字符串分割
        /// </summary>
        private string[] PerformStringSplit(string source, JudgeCodition codition)
        {
            if (string.IsNullOrEmpty(codition.SplitDelimiter)) return new[] { source };

            var options = codition.RemoveEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;

            string[] result = codition.MaxSplitCount > 0
                ? source.Split(new[] { codition.SplitDelimiter }, codition.MaxSplitCount, options)
                : source.Split(new[] { codition.SplitDelimiter }, options);

            if (codition.TrimEntries)
                result = result.Select(s => s.Trim()).ToArray();

            return result;
        }

        /// <summary>
        /// String字符串提取
        /// </summary>
        private string PerformSubstring(string source, JudgeCodition codition)
        {
            if (codition.SubstringStartIndex >= source.Length) return string.Empty;

            if (codition.SubstringLength == 0 || codition.SubstringStartIndex + codition.SubstringLength > source.Length)
                return source.Substring(codition.SubstringStartIndex);

            return source.Substring(codition.SubstringStartIndex, codition.SubstringLength);
        }

        #endregion

        #region Override
        /// <summary>
        /// 加载关键参数
        /// </summary>
        /// <returns></returns>
        public override bool LoadKeyParam()
        {
            try
            {
                foreach (var condition in AllJudgeCodition)
                {
                    if (condition?.Variable == null)
                        continue;

                    GetTransmitParam(InputParams, condition.Variable, false);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"条件模块加载参数失败{ex.StackTrace}");
                return false;
            }
        }
        #endregion
    }

    public enum JudgeCoditionType
    {
        None = 0,
        循环,


        自定义条件 = 99,
    }

    /// <summary>
    /// Int比较类型
    /// </summary>
    public enum IntCompareType
    {
        Equal,              // 等于
        NotEqual,           // 不等于
        GreaterThan,        // 大于
        GreaterThanOrEqual, // 大于等于
        LessThan,           // 小于
        LessThanOrEqual     // 小于等于
    }

    /// <summary>
    /// Int运算类型
    /// </summary>
    public enum IntOperationType
    {
        Add,      // 加
        Subtract, // 减
        Multiply, // 乘
        Divide,   // 除
        Modulo    // 取模
    }

    /// <summary>
    /// 判断条件
    /// </summary>
    [Serializable]
    public partial class JudgeCodition : BindableBase
    {
        [JsonIgnore]
        private Guid _guid;
        public Guid Guid
        {
            get { return _guid; }
            set { _guid = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _variable;

        public TransmitParam Variable
        {
            get { return _variable; }
            set { _variable = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isUsing;

        public bool IsUsing
        {
            get { return _isUsing; }
            set { _isUsing = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isSatisfy;
        /// <summary>
        /// 是否满足条件
        /// </summary>
        public bool IsSatisfy
        {
            get { return _isSatisfy; }
            set { _isSatisfy = value; RaisePropertyChanged(); }
        }



        #region List判断条件
        [JsonIgnore]
        private bool _isOutputSingle;
        /// <summary>
        /// 输出集合中指定单个参数
        /// </summary>
        public bool IsOutputSingle
        {
            get { return _isOutputSingle; }
            set { _isOutputSingle = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _singleIndex;
        /// <summary>
        /// 指定集合的单个下标
        /// </summary>
        public int SingleIndex
        {
            get { return _singleIndex; }
            set { _singleIndex = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Int判断条件

        [JsonIgnore]
        private bool _intCompareEnabled;
        /// <summary>
        /// 启用Int比较判断
        /// </summary>
        public bool IntCompareEnabled
        {
            get { return _intCompareEnabled; }
            set { _intCompareEnabled = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private IntCompareType _intCompareType = IntCompareType.Equal;
        /// <summary>
        /// Int比较类型
        /// </summary>
        public IntCompareType IntCompareType
        {
            get { return _intCompareType; }
            set { _intCompareType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _intCompareValue;
        /// <summary>
        /// Int比较值
        /// </summary>
        public int IntCompareValue
        {
            get { return _intCompareValue; }
            set { _intCompareValue = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _intRangeEnabled;
        /// <summary>
        /// 启用Int范围判断
        /// </summary>
        public bool IntRangeEnabled
        {
            get { return _intRangeEnabled; }
            set { _intRangeEnabled = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _intRangeMin;
        /// <summary>
        /// Int范围最小值
        /// </summary>
        public int IntRangeMin
        {
            get { return _intRangeMin; }
            set { _intRangeMin = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _intRangeMax;
        /// <summary>
        /// Int范围最大值
        /// </summary>
        public int IntRangeMax
        {
            get { return _intRangeMax; }
            set { _intRangeMax = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _intRangeIncludeMin = true;
        /// <summary>
        /// 范围判断是否包含最小值
        /// </summary>
        public bool IntRangeIncludeMin
        {
            get { return _intRangeIncludeMin; }
            set { _intRangeIncludeMin = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _intRangeIncludeMax = true;
        /// <summary>
        /// 范围判断是否包含最大值
        /// </summary>
        public bool IntRangeIncludeMax
        {
            get { return _intRangeIncludeMax; }
            set { _intRangeIncludeMax = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _intOperationEnabled;
        /// <summary>
        /// 启用Int数值运算
        /// </summary>
        public bool IntOperationEnabled
        {
            get { return _intOperationEnabled; }
            set { _intOperationEnabled = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private IntOperationType _intOperationType = IntOperationType.Add;
        /// <summary>
        /// Int运算类型
        /// </summary>
        public IntOperationType IntOperationType
        {
            get { return _intOperationType; }
            set { _intOperationType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _intOperationValue;
        /// <summary>
        /// Int运算值
        /// </summary>
        public int IntOperationValue
        {
            get { return _intOperationValue; }
            set { _intOperationValue = value; RaisePropertyChanged(); }
        }

        #endregion

        #region 参数链接

        [JsonIgnore]
        private bool _paramLinkEnabled;
        /// <summary>
        /// 启用参数链接
        /// </summary>
        public bool ParamLinkEnabled
        {
            get { return _paramLinkEnabled; }
            set { _paramLinkEnabled = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ResoureceType _paramLinkSourceType = ResoureceType.None;
        /// <summary>
        /// 参数链接来源类型
        /// </summary>
        public ResoureceType ParamLinkSourceType
        {
            get { return _paramLinkSourceType; }
            set { _paramLinkSourceType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _linkedParam;
        /// <summary>
        /// 链接的目标参数
        /// </summary>
        public TransmitParam LinkedParam
        {
            get { return _linkedParam; }
            set { _linkedParam = value; RaisePropertyChanged(); }
        }

        #endregion
    }
}
