using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.MFDJC.Models
{
    /// <summary>
    /// 用来存放显示给页面能够修改的参数
    /// </summary>
    [Serializable]
    public class OtherConfigModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        private int _manualOffsetX;

        [Category("1.手动参数"), DisplayName("X方向拼接偏移")]
        public int ManualOffsetX
        {
            get { return _manualOffsetX; }
            set { _manualOffsetX = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _manualOffsetY;
        [Category("1.手动参数"), DisplayName("Y方向拼接偏移")]
        public int ManualOffsetY
        {
            get { return _manualOffsetY; }
            set { _manualOffsetY = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _savePath;
        [Browsable(false)]
        public string SavePath
        {
            get { return _savePath; }
            set { _savePath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isSaveImage;
        [Category("2.存图参数"), DisplayName("是否启用存图")]
        public bool IsSaveImage
        {
            get { return _isSaveImage; }
            set { _isSaveImage = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constrctor
        public OtherConfigModel()
        {
            
        }
        #endregion


    }
}
