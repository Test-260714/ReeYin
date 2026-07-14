using LogicalTool.GeneralOutput.Models;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LogicalTool.GeneralOutput.ViewModels
{
    [Serializable]
    public class GeneralOutputViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        #endregion

        #region Properties  
        private GeneralOutputModel _modelParam;

        public GeneralOutputModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public GeneralOutputViewModel()
        {
            
        }
        #endregion

        #region Commands

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            //等待加载完成赋值


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
                case "执行":


                    break;
                case "使能":
                    //var CurCom = ModelParam.ComModel.CommunicationModels.FirstOrDefault(p => p.Key == ModelParam.SltCom);

                    ////if(SelectedMonitor.IsUsing)
                    //CurCom.ReceiveString += MonitorCom;
                    ////else
                    ////    CurCom.ReceiveString -= MonitorCom;

                    break;
                case "取消":

                    break;
                case "确认":

                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam },
                    });
                    break;
                default:
                    break;
            }

        });
        #endregion

        #region Methods
        public void Init()
        {
            
        }
        #endregion

    }
}
