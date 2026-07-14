using LogicalTool.Loop.Models;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Models;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI;
using System;
using System.Windows;

namespace LogicalTool.Loop.ViewModels
{
    [Serializable]
    public class LoopViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        private object _oldParam;

        #endregion

        #region Properties

        /// <summary>
        /// 循环模型参数
        /// </summary>
        public LoopModel ModelParam
        {
            get { return base.ModelParam as LoopModel; }
            set { base.ModelParam = value; }
        }

        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor

        public LoopViewModel()
        {
        }

        #endregion

        #region Override

        public override bool CanCloseDialog()
        {
            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// 初始化参数
        /// </summary>
        public override void InitParam()
        {
            InitModelParam<LoopModel>();
        }

        /// <summary>
        /// 验证参数有效性
        /// </summary>
        private bool ValidateParameters()
        {
            // 如果是指定次数模式
            if (ModelParam.IsAssignVisibility == Visibility.Visible)
            {
                if (ModelParam.LoopNum < -1 || ModelParam.LoopNum == 0)
                {
                    MessageView.Ins.MessageBoxShow("循环次数必须大于0或等于-1（无限循环）");
                    return false;
                }
            }
            // 如果是链接次数模式
            else if (ModelParam.IsLinkVisibility == Visibility.Visible)
            {
                if (ModelParam.LinkLoopNum == null)
                {
                    MessageView.Ins.MessageBoxShow("请选择链接的循环次数参数");
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Commands

        /// <summary>
        /// 加载命令
        /// </summary>
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            // 初始化可见性（默认显示指定次数）
            if (ModelParam.IsAssignVisibility == Visibility.Collapsed &&
                ModelParam.IsLinkVisibility == Visibility.Collapsed)
            {
                ModelParam.IsAssignVisibility = Visibility.Visible;
                ModelParam.IsLinkVisibility = Visibility.Hidden;
            }
            PrismProvider.SoftwareAlarmReporter?.Report(
code: "SW.MYMODULE.NO_RESULT",
source: "MyVisionModule",
location: $"{Serial:D3}",
message: "检测模块未输出有效结果。",
severity: AlarmSeverity.Error,
extraData: new Dictionary<string, object?>
{
["Serial"] = Serial,
["ModuleName"] = "MyVisionModule",
["Reason"] = "NoResult"
});
            PrismProvider.AlarmService.AddAlarm(new AlarmRaiseRequest
            {
                Code = "DEMO.ALARM.REPEAT",
                Name = "重复触发报警",
                Category = "Demo",
                Message = "第一次触发。",
                Level = AlarmSeverity.Fatal,
                Source = "AlarmDemo",
                Location = "DebugPanel",
                PopupMode = AlarmPopupMode.Growl,
                PopupThrottleSeconds = 0,
                NeedAcknowledge = false,
                AllowManualClear = true
            });

            // 如果不显示界面，直接关闭对话框
            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam }
                });
            }
        });

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>(order =>
        {
            switch (order)
            {
                case "执行":
                    // 预留执行逻辑
                    break;

                case "取消":
                    // 取消时返回旧参数
                    CloseDialog(ButtonResult.Cancel, new DialogParameters
                    {
                        { "Param", _oldParam }
                    });
                    break;

                case "指定次数":
                    ModelParam.IsLinkVisibility = Visibility.Hidden;
                    ModelParam.IsAssignVisibility = Visibility.Visible;
                    break;

                case "链接次数":
                    ModelParam.IsLinkVisibility = Visibility.Visible;
                    ModelParam.IsAssignVisibility = Visibility.Hidden;
                    break;

                case "终止循环":
                    ModelParam.IsAbortLoop = true;
                    ModelParam.IsLoopFlag = false;
                    //ModelParam.TransmitLoopNum = ModelParam.LoopNum;
                    ModelParam.TransmitLoopNum = 0;
                    MessageView.Ins.MessageBoxShow("循环已终止", eMsgType.Info);
                    break;

                case "确认":
                    if (ValidateParameters())
                    {
                        CloseDialog(ButtonResult.OK, new DialogParameters
                        {
                            { "Param", ModelParam }
                        });
                    }
                    break;
            }
        });

        /// <summary>
        /// 参数值变化命令
        /// </summary>
        public DelegateCommand<string> ValueChanged => new DelegateCommand<string>(order =>
        {
            switch (order)
            {
                case "LoopNum":
                    //ModelParam.TransmitLoopNum = ModelParam.LoopNum;
                    ModelParam.TransmitLoopNum = 0;
                    break;
            }
        });


        public DelegateCommand<object> DataOperateCommand
        {
            get
            {
                return new DelegateCommand<object>
                (
                    (obj) =>
                    {
                        switch (obj?.ToString())
                        {
                            case "Add":
                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;

                                if (ModelParam.OutputParams.Where(item => item.Name == SltOutputParamName).ToList().Count >= 1)
                                {
                                    MessageBox.Show("已包含重名参数，请重新输入！");
                                }
                                else
                                {
                                    if (curSltParam.Resourece == ResoureceType.None)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            ParamName = curSltParam.Name,
                                            Serial = ModelParam.Serial,
                                            ParentNode = Name,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name].DeepCopy(),
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            ParentNode = Name,
                                            Value = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.Value.DeepClone(),
                                            ResourcePath = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.ResourcePath,
                                            Serial = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name).Serial
                                        });
                                    }
                                }

                                break;
                            case "Delete":
                                if (CurrentOutputParam != null)
                                {
                                    ModelParam.OutputParams.Remove(CurrentOutputParam);
                                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
                                    CurrentOutputParam = null;
                                }

                                break;
                            default:
                                break;
                        }
                    }
                );
            }
        }
        #endregion
    }
}
