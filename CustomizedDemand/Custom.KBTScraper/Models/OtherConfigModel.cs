using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KBTScraper.Models
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
        private string _savePath = @"D:\test_results";
        [Browsable(false)]
        public string SavePath
        {
            get { return _savePath; }
            set { _savePath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isSaveImage = true;  // 默认开启存图
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
        private string _saveCSVPath = @"D:\KBTScraper_data";
        /// <summary>
        /// CSV存储路径
        /// </summary>
        public string SaveCSVPath
        {
            get { return _saveCSVPath; }
            set { _saveCSVPath = value; RaisePropertyChanged(); }
        }

        private string _productNum = string.Empty;
        /// <summary>
        /// 产品号
        /// </summary>
        public string ProductNum
        {
            get { return _productNum; }
            set { _productNum = value; RaisePropertyChanged(); }
        }

        private string _batchNum = string.Empty;
        /// <summary>
        /// 批次号
        /// </summary>
        public string BatchNum
        {
            get { return _batchNum; }
            set { _batchNum = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constrctor
        public OtherConfigModel()
        {

        }
        #endregion


    }
}
