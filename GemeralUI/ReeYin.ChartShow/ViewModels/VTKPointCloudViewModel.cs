using ReeYin.ChartShow.Models;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.ChartShow.ViewModels
{
    public class VTKPointCloudViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        #endregion

        #region Properties
        private VTKPointCloudModel _model;

        public VTKPointCloudModel Model
        {
            get { return _model; }
            set { _model = value; RaisePropertyChanged(); }
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
                case "文件加载":
                    {
                        Model.mVTKPCDisplay.LoadPointCloudFile("Ply");
                    }
                    break;

                default:
                    break;
            }
        });
        #endregion
    }
}
