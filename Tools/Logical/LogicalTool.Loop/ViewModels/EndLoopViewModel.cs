using LogicalTool.Loop.Models;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.UI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace LogicalTool.Loop.ViewModels
{
    [Serializable]
    public class EndLoopViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Properties

        private ObservableCollection<TransmitParam> _globalParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 全局参数
        /// </summary>
        public ObservableCollection<TransmitParam> GlobalParams
        {
            get => _globalParams;
            set { _globalParams = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _globalParamsNames = new ObservableCollection<string>();
        /// <summary>
        /// 全局参数名称列表
        /// </summary>
        public ObservableCollection<string> GlobalParamsNames
        {
            get => _globalParamsNames;
            set { _globalParamsNames = value; RaisePropertyChanged(); }
        }

        private TransmitParam _sltGlobalParam;
        /// <summary>
        /// 选中的全局参数
        /// </summary>
        public TransmitParam SltGlobalParam
        {
            get => _sltGlobalParam;
            set { _sltGlobalParam = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 结束循环模型参数
        /// </summary>
        public EndLoopModel ModelParam
        {
            get => base.ModelParam as EndLoopModel;
            set { base.ModelParam = value; RaisePropertyChanged(); }
        }

        private TransmitParam _currentOutputParam;
        /// <summary>
        /// 当前输出参数。
        /// </summary>
        public TransmitParam CurrentOutputParam
        {
            get => _currentOutputParam;
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor

        public EndLoopViewModel()
        {
            InitializeGlobalParams();
        }

        #endregion

        #region Methods

        /// <summary>
        /// 初始化全局参数列表
        /// </summary>
        private void InitializeGlobalParams()
        {
            GlobalParams = PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams;
            GlobalParamsNames.Clear();

            foreach (var item in GlobalParams)
            {
                GlobalParamsNames.Add($"{item.Serial:D3}_{item.Name}");
            }
        }

        /// <summary>
        /// 初始化参数
        /// </summary>
        public override void InitParam()
        {
            InitModelParam<EndLoopModel>();
            ModelParam.TransferParam();
        }

        /// <summary>
        /// 验证参数有效性
        /// </summary>
        private bool ValidateParameters()
        {
            if (ModelParam.EndNodifyNum < 0)
            {
                MessageView.Ins.MessageBoxShow("结束节点号必须大于0");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 按上一节点输入刷新全部输出项。
        /// </summary>
        private void RefreshOutputParamsPreview()
        {
            if (ModelParam == null)
            {
                return;
            }

            var configuredOutputs = ModelParam.OutputParams.ToList();
            ModelParam.OutputParams.Clear();
            foreach (var input in ModelParam.InputParams.Where(item => item != null))
            {
                var configured = configuredOutputs.FirstOrDefault(item => item.Guid == input.Guid);

                ModelParam.OutputParams.Add(new TransmitParam
                {
                    Guid = input.Guid,
                    LinkGuid = input.LinkGuid,
                    Serial = ModelParam.Serial,
                    ParentNode = Name,
                    Name = string.IsNullOrWhiteSpace(configured?.Name) ? input.Name : configured.Name,
                    ParamName = string.IsNullOrWhiteSpace(input.ParamName) ? input.Name : input.ParamName,
                    Type = DataType.List,
                    Value = input.Value,
                    Describe = string.IsNullOrWhiteSpace(configured?.Describe) ? input.Describe : configured.Describe,
                    IsGlobal = configured?.IsGlobal ?? false,
                    ResourcePath = input.ResourcePath,
                    Resourece = ResoureceType.Output
                });
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// 加载命令
        /// </summary>
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            // 等待加载完成后赋值序列号
            if (ModelParam.Serial == -999)
            {
                ModelParam.Serial = Serial;
            }

            ModelParam.LoadKeyParam();
            RefreshOutputParamsPreview();

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
                    CloseDialog(ButtonResult.Cancel);
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

        public DelegateCommand<string> DataOperateCommand => new DelegateCommand<string>(order =>
        {
            switch (order)
            {
                case "RefreshOutput":
                    RefreshOutputParamsPreview();
                    break;
            }
        });

        #endregion
    }
}
