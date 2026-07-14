using HalconDotNet;
using NetTaste;
using Newtonsoft.Json;
using Prism.Ioc;
using Prism.Mvvm;
using ReeYin_V.Core;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.Camera.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReeYin_V.Hardware.Camera.Models
{
    [Serializable]
    public class CameraSetModel : BindableBase, IHardwareModule
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private ObservableCollection<CameraInfoModel> _CameraNos = new ObservableCollection<CameraInfoModel>();

        public ObservableCollection<CameraInfoModel> CameraNos
        {
            get { return _CameraNos; }
            set { _CameraNos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<CameraBase> _CameraModels = new ObservableCollection<CameraBase>();

        public ObservableCollection<CameraBase> CameraModels
        {
            get { return _CameraModels; }
            set { _CameraModels = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _SelectedIndex;

        public int SelectedIndex
        {
            get { return _SelectedIndex; }
            set { _SelectedIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _SelectedCameraType;

        public string SelectedCameraType
        {
            get { return _SelectedCameraType; }
            set { _SelectedCameraType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private CameraInfoModel _CameraNo = new CameraInfoModel();

        public CameraInfoModel CameraNo
        {
            get { return _CameraNo; }
            set { _CameraNo = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public CameraSetModel()
        {
            
        }
        #endregion

        #region Methods
        public InitResult Init()
        {
            Dictionary<string, bool> CamStatus = new Dictionary<string, bool>(); 
            foreach (var Cam in CameraModels)
            {
                CamStatus.Add(Cam.CameraNo, Cam.Connected);
            }
            InitResult result = new InitResult();
            if (CamStatus.Values.Any(value => value == false))
            {
                result = new InitResult 
                {
                    Message="连接失败！",
                    Success=false,
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

        public void Shutdown()
        {
            foreach (var Cam in CameraModels)
            {
                Cam.DisConnectDev();
            }
        }

        public void RefreshStatus()
        {
            foreach (var Cam in CameraModels)
            {
            }
        }


        #endregion

    }
}
