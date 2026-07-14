using Newtonsoft.Json;
using ReeYin_V.Core;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.Models
{
    [Serializable]
    public class SensorSetModel : BindableBase, IHardwareModule
    {
        #region Fields

        #endregion

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
        private SensorBase _curSlt;
        /// <summary>
        /// 当前选中
        /// </summary>
        public SensorBase CurSlt
        {
            get
            {
                return _curSlt;
            }
            set { _curSlt = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<SensorBase> _models = new ObservableCollection<SensorBase>();
        /// <summary>
        /// 所有传感器集合
        /// </summary>
        public ObservableCollection<SensorBase> Models
        {
            get
            {
                return _models;
            }
            set { _models = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public SensorSetModel()
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
                model.State = model.State;
            }
        }

        #endregion

    }
}
