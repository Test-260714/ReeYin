using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KBTBox.Models
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

        [JsonIgnore]
        private bool _isSaveCSV;
        /// <summary>
        /// 是否存CSV文件
        /// </summary>
        public bool IsSaveCSV
        {
            get { return _isSaveCSV; }
            set { _isSaveCSV = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _saveCSVPath = @"D:\KBT_data";
        /// <summary>
        /// CSV存储路径
        /// </summary>
        public string SaveCSVPath
        {
            get { return _saveCSVPath; }
            set { _saveCSVPath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isEnableJudgment;
        /// <summary>
        /// 是否启用数据判定
        /// </summary>
        public bool IsEnableJudgment
        {
            get { return _isEnableJudgment; }
            set { _isEnableJudgment = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isAutoStart;
        /// <summary>
        /// 软件启动后是否自动运行流程
        /// </summary>
        public bool IsAutoStart
        {
            get { return _isAutoStart; }
            set { _isAutoStart = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constrctor
        public OtherConfigModel()
        {

        }
        #endregion


    }
}
