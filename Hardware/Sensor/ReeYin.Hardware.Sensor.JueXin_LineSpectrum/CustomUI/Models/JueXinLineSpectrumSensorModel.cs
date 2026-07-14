using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Services.Module;
using System;

namespace ReeYin.Hardware.Sensor.JueXin_LineSpectrum.CustomUI.Models
{
    /// <summary>
    /// 觉芯线光谱传感器设置页面模型。
    /// </summary>
    [Serializable]
    public class JueXinLineSpectrumSensorModel : BindableBase
    {
        [JsonIgnore]
        private JueXinLineSpectrumSensor _sensor = new JueXinLineSpectrumSensor();

        public JueXinLineSpectrumSensor Sensor
        {
            get { return _sensor; }
            set { _sensor = value; RaisePropertyChanged(); }
        }
    }
}
