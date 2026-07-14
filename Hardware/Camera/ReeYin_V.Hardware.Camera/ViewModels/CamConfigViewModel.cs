using Prism.Commands;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.Camera.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.Camera.ViewModels
{
    public class CamConfigViewModel : DialogViewModelBase
    {
        #region Fields
        private CameraBase curIns;
        #endregion

        #region Properties
        private ConfigModel _model = new ConfigModel();

        public ConfigModel Model
        {
            get { return _model; }
            set { _model = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public CamConfigViewModel()
        {

        }
        #endregion

        #region Methods
        public override void InitParam()
        {
            curIns = Param as CameraBase;
            if (curIns != null)
            {
                // 如果相机还没有 Config，用 ViewModel 的 Model 赋给它
                if (curIns.Config == null)
                    curIns.Config = Model;
                else
                    Model = curIns.Config; // 用相机已有的 Config，保持双向同步
            }
        }
        #endregion

        #region Commands
        /// <summary>
        /// 设置参数变更指令
        /// </summary>
        public DelegateCommand<string> ValueChangedCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "曝光":
                    {
                        curIns.SetSpecifiedParam("Double", "ExposureTime", Model.ExposeTime);
                    }
                    break;
                case "增益":
                    {
                        curIns.SetSpecifiedParam("Double", "Gain", Model.Gain);
                    }
                    break;
                case "行频":
                    {
                        curIns.SetLineRate(Model.LineRate);
                    }
                    break;
                default:
                    break;
            }
        });
        #endregion
    }
}
