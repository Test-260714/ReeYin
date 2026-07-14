using HardWareTool.PLC.Models;
using HardWareTool.PLC.Views;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.PLC.Models;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HardWareTool.PLC.ViewModels
{
    [Serializable]
    public class PLCViewModel : DialogViewModelBase, IViewModuleParam
    {

        #region Fields
        private const string REGION_NAME = "PLCOperationRegion";
        private Task _localTask;
        #endregion

        #region Properties
        [JsonIgnore]
        public IRegionManager RegionManager { get; set; }

        public new PLCModel ModelParam
        {
            get { return base.ModelParam as PLCModel; }
            set { base.ModelParam = value; }
        }
        #endregion

        #region Constructor
        public PLCViewModel()
        {

        }

        #endregion

        #region Override
        #region Override
        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
            }
        }

        public override void InitParam()
        {
            ModelParam = InitModelParam<PLCModel>();
            //ModelParam.LoadKeyParam();
        }
        #endregion
        #endregion

        #region Methods
        public void Init()
        {

        }

        private void NavigateToViewByOperationType(OperationType operationType)
        {
            if (RegionManager == null) return;

            string viewName = operationType switch
            {
                OperationType.读单个地址 => nameof(ReadSingleAddrView),
                OperationType.写单个地址 => nameof(WriteSingleAddrView),
                OperationType.轴操作 => nameof(AxisOperationView),
                OperationType.延时操作 => nameof(DelayOperationView),
                OperationType.触发事件 => nameof(TriggerEventView),
                _ => null
            };
            if (viewName == null) return;

            var navigationParams = new NavigationParameters
            {
                { "ModelParam", ModelParam }
            };

            try
            {
                RegionManager.RequestNavigate(REGION_NAME, viewName, navigationParams);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导航错误：{ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region Commands
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            //不显示说明只是加载
            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {

                case "选中指令":
                    {
                        if (ModelParam?.SltPLCOrder == null) return;
                        PrismProvider.Dispatcher.BeginInvoke(() =>
                        {
                            NavigateToViewByOperationType(ModelParam.SltPLCOrder.OperationType);
                        });
                    }
                    break;

                case "操作类型变更":
                    {
                        if (ModelParam?.SltPLCOrder == null) return;
                        NavigateToViewByOperationType(ModelParam.SltPLCOrder.OperationType);
                    }
                    break;

                case "执行":
                    {

                    }
                    break;

                case "取消":
                    {
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            //{ "Param", ModelParam },
                        });
                    }
                    break;


                case "添加新项":
                    {
                        ModelParam.PLCOrder.Add(new PLCOrder()
                        {
        
                        });
                    }
                    break;

                case "删除选中项":
                    {
                        ModelParam.PLCOrder.Remove(ModelParam.SltPLCOrder);
                        ModelParam.SltPLCOrder = null;
                    }
                    break;

                case "触发指令":
                    {
                        MessageBoxResult result = MessageBox.Show("确定要触发?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }


                        if (ModelParam.SingleExecute(ModelParam.SltPLCOrder) != NodeStatus.Success)
                        {
                            MessageBox.Show("执行失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                    }
                    break;

                case "确认":
                    {
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });
                    }
                    break;
                default:
                    break;
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {

                case "使能":
                    {

                    }
                    break;
                default:
                    break;
            }
        });
        #endregion
    }
}
