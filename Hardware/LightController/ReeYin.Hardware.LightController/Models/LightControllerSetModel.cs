using Newtonsoft.Json;
using ReeYin_V.Core.Base;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.LightController.Models
{
    [Serializable]
    public class LightControllerSetModel : BindableBase, IHardwareModule
    {
        #region Properties
        [JsonIgnore]
        private string _SltVendorType;
        /// <summary>
        /// 选择的厂家类型
        /// </summary>
        public string SltVendorType
        {
            get { return _SltVendorType; }
            set { _SltVendorType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private LightControllerBase _curSlt;
        /// <summary>
        /// 当前选中
        /// </summary>
        public LightControllerBase CurSlt
        {
            get { return _curSlt; }
            set { _curSlt = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<LightControllerBase> _models = new ObservableCollection<LightControllerBase>();
        /// <summary>
        /// 所有光源控制器集合
        /// </summary>
        public ObservableCollection<LightControllerBase> Models
        {
            get { return _models; }
            set { _models = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public LightControllerSetModel()
        {
        }

        public InitResult Init()
        {
            Dictionary<string, bool> Status = new Dictionary<string, bool>();

            foreach (var model in Models)
            {
                model?.Init();
            }

            InitResult result = new InitResult();

            if (Status.Values.Any(value => value == false))
            {
                result = new InitResult
                {
                    Message = "连接失败！",
                    Success = false,
                };
            }
            else
            {
                result = new InitResult
                {
                    Message = "连接成功！",
                    Success = true,
                };
            }
            return result;
        }
        #endregion

        #region Methods
        public void Shutdown()
        {
            foreach (var model in Models)
            {
                model.Close();
            }
        }

        public void RefreshStatus()
        {
            foreach (var model in Models)
            {
                // 刷新每个光源控制器的状态
                // 可以在这里更新连接状态、参数等
            }
        }
        #endregion
    }
}
