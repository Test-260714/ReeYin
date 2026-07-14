using Custom.KBTBox.Models;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Custom.KBTBox.ViewModels
{
    public class HPManualViewModel : DialogViewModelBase
    {
        #region Fields

        #endregion

        #region Properties
        private HPManualModel _modelParam = new HPManualModel();

        public HPManualModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }


        #endregion

        #region Constructor
        public HPManualViewModel()
        {

        }
        #endregion

        #region Methods

        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "开始":
                    {
                        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStartCollect");

                    }
                    break;

                case "停止":
                    {
                        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerStopCollect");
                        ModelParam.RunNum++;
                    }
                    break;

                case "处理数据":
                    {
                        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("TrrigerDispose");
                        ModelParam.RunNum = 0;
                    }
                    break;

                case "重置":
                    {
                        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish("Reset");
                        ModelParam.RunNum = 0;
                    }
                    break;

                case "确认":
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
