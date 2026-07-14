using HardWareTool.PLC.Models;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HardWareTool.PLC.ViewModels
{
    [Serializable]
    public class PLCMonitorViewModel : DialogViewModelBase
    {
        #region Fields

        #endregion

        #region Properties
        private PLCMonitorModel _modelParam = new PLCMonitorModel();

        public PLCMonitorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public PLCMonitorViewModel()
        {
            
        }
        #endregion

        #region Methods
        public override void InitParam()
        {
            if (Param != null && (Param is PLCOrder))
                ModelParam.Param = Param as PLCOrder;
            else
                ModelParam.Param = new PLCOrder();
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
                    { "Param", ModelParam.Param },
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
                case "取消":
                    {

                    }
                    break;

                case "确认":
                    {
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam.Param },
                        });
                    }
                    break;
                default:
                    break;
            }
        });
        #endregion
    }
}
