using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.Hyperson.CustomUI.Defines;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KCJC.Models
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
        private string _saveDatasPath;
        [Browsable(false)]
        public string SaveDatasPath
        {
            get { return _saveDatasPath; }
            set { _saveDatasPath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isSaveDatas;
        [Category("3.数据参数"), DisplayName("是否启用数据存储")]
        public bool IsSaveDatas
        {
            get { return _isSaveDatas; }
            set { _isSaveDatas = value; RaisePropertyChanged(); }
        }

        private bool _isUploadPublicDisk;
        /// <summary>
        /// 是否启用公盘上传
        /// </summary>
        public bool IsUploadPublicDisk
        {
            get { return _isUploadPublicDisk; }
            set { _isUploadPublicDisk = value; RaisePropertyChanged(); }
        }

        private string _publicDiskPath = string.Empty;
        /// <summary>
        /// 公盘根路径
        /// </summary>
        public string PublicDiskPath
        {
            get { return _publicDiskPath; }
            set { _publicDiskPath = value; RaisePropertyChanged(); }
        }

        private bool _isUploadSummaryPublicDisk;
        /// <summary>
        /// 是否启用汇总CSV公盘上传。
        /// </summary>
        public bool IsUploadSummaryPublicDisk
        {
            get { return _isUploadSummaryPublicDisk; }
            set { _isUploadSummaryPublicDisk = value; RaisePropertyChanged(); }
        }

        private string _summaryPublicDiskPath = string.Empty;
        /// <summary>
        /// 汇总CSV公盘根路径。
        /// </summary>
        public string SummaryPublicDiskPath
        {
            get { return _summaryPublicDiskPath; }
            set { _summaryPublicDiskPath = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isSaveImage;
        [Category("2.存图参数"), DisplayName("是否启用存图")]
        public bool IsSaveImage
        {
            get { return _isSaveImage; }
            set { _isSaveImage = value; RaisePropertyChanged(); }
        }

        private short _plcTotalCount;
        /// <summary>
        /// PLC写入总次数
        /// </summary>
        public short PlcTotalCount
        {
            get { return _plcTotalCount; }
            set { _plcTotalCount = value; RaisePropertyChanged(); }
        }

        private short _plcResetCount;
        /// <summary>
        /// PLC写入复位次数
        /// </summary>
        public short PlcResetCount
        {
            get { return _plcResetCount; }
            set { _plcResetCount = value; RaisePropertyChanged(); }
        }

        private float _plcMaterialLength;
        /// <summary>
        /// PLC写入材料长度
        /// </summary>
        public float PlcMaterialLength
        {
            get { return _plcMaterialLength; }
            set { _plcMaterialLength = value; RaisePropertyChanged(); }
        }
        [JsonIgnore]
        private float _detectPosition1;
        [RecipeParam("DetectPosition1", "检测位1")]
        /// <summary>
        /// PLC写入检测位1
        /// </summary>
        public float DetectPosition1
        {
            get { return _detectPosition1; }
            set { _detectPosition1 = value; RaisePropertyChanged(); }
        }
        [JsonIgnore]
        private float _detectPosition2;
        [RecipeParam("DetectPosition2", "检测位2")]
        /// <summary>
        /// PLC写入检测位2
        /// </summary>
        public float DetectPosition2
        {
            get { return _detectPosition2; }
            set { _detectPosition2 = value; RaisePropertyChanged(); }
        }
        [JsonIgnore]
        private float _runSpeed;
        [RecipeParam("RunSpeed", "运行速度")]
        /// <summary>
        /// PLC写入运行速度
        /// </summary>
        public float RunSpeed
        {
            get { return _runSpeed; }
            set { _runSpeed = value; RaisePropertyChanged(); }
        }
        [JsonIgnore]
        private int _exposureTime;
        [RecipeParam("ExposureTime", "曝光时间")]
        /// <summary>
        /// 曝光时间
        /// </summary>
        public int ExposureTime
        {
            get { return _exposureTime; }
            set { _exposureTime = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _intensity;
        [RecipeParam("Intensity", "光强")]
        /// <summary>
        /// 海伯森光强
        /// </summary>
        public int Intensity
        {
            get { return _intensity; }
            set { _intensity = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Gain _gainIndex = Gain.GAIN_1;
        [RecipeParam("GainIndex", "增益")]
        /// <summary>
        /// 海伯森增益
        /// </summary>
        public Gain GainIndex
        {
            get { return _gainIndex; }
            set { _gainIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _encoderDivision;
        [RecipeParam("EncoderDivision", "固定预分频值")]
        /// <summary>
        /// 海伯森固定预分频值
        /// </summary>
        public int EncoderDivision
        {
            get { return _encoderDivision; }
            set { _encoderDivision = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _productModelName = string.Empty;
        /// <summary>
        /// 当前产品型号
        /// </summary>
        public string ProductModelName
        {
            get { return _productModelName; }
            set { _productModelName = value; RaisePropertyChanged(); }
        }

        private string _workpieceName = string.Empty;
        /// <summary>
        /// 汇总报表工件名称。
        /// </summary>
        public string WorkpieceName
        {
            get { return _workpieceName; }
            set { _workpieceName = value; RaisePropertyChanged(); }
        }

        private string _batchNo = string.Empty;
        /// <summary>
        /// 当前批次号
        /// </summary>
        public string BatchNo
        {
            get { return _batchNo; }
            set { _batchNo = value; RaisePropertyChanged(); }
        }

        private string _workshop = string.Empty;
        /// <summary>
        /// 当前车间
        /// </summary>
        public string Workshop
        {
            get { return _workshop; }
            set { _workshop = value; RaisePropertyChanged(); }
        }

        private string _processName = string.Empty;
        /// <summary>
        /// 当前工序
        /// </summary>
        public string ProcessName
        {
            get { return _processName; }
            set { _processName = value; RaisePropertyChanged(); }
        }

        private string _reportType = "首件";
        /// <summary>
        /// 汇总报表类型。
        /// </summary>
        public string ReportType
        {
            get { return _reportType; }
            set { _reportType = value; RaisePropertyChanged(); }
        }

        private string _shiftName = string.Empty;
        /// <summary>
        /// 汇总报表班别。
        /// </summary>
        public string ShiftName
        {
            get { return _shiftName; }
            set { _shiftName = value; RaisePropertyChanged(); }
        }

        private string _machineNo = string.Empty;
        /// <summary>
        /// 汇总报表机台号。
        /// </summary>
        public string MachineNo
        {
            get { return _machineNo; }
            set { _machineNo = value; RaisePropertyChanged(); }
        }

        private string _tester = string.Empty;
        /// <summary>
        /// 汇总报表测试员。
        /// </summary>
        public string Tester
        {
            get { return _tester; }
            set { _tester = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<SummaryTestItemConfig> _summaryTestItems = new ObservableCollection<SummaryTestItemConfig>();
        /// <summary>
        /// 汇总CSV输出的测试项目；当前类别未勾选时默认输出当前类别全部项目。
        /// </summary>
        [Browsable(false)]
        public ObservableCollection<SummaryTestItemConfig> SummaryTestItems
        {
            get
            {
                EnsureSummaryTestItems();
                return _summaryTestItems;
            }
            set
            {
                _summaryTestItems = value ?? new ObservableCollection<SummaryTestItemConfig>();
                EnsureSummaryTestItems();
                RaisePropertyChanged();
            }
        }

        private int _calibrationNodeSerial = 1;
        /// <summary>
        /// 手动执行标准片流程时使用的节点序号
        /// </summary>
        public int CalibrationNodeSerial
        {
            get { return _calibrationNodeSerial; }
            set { _calibrationNodeSerial = value; RaisePropertyChanged(); }
        }

        private double _etchingLineDepthLowerLimit = 15;
        /// <summary>
        /// 槽深下限(μm)
        /// </summary>
        [RecipeParam("EtchingLineDepthLowerLimit", "槽深下限")]
        public double EtchingLineDepthLowerLimit
        {
            get { return _etchingLineDepthLowerLimit; }
            set { _etchingLineDepthLowerLimit = value; RaisePropertyChanged(); }
        }

        private double _etchingLineDepthUpperLimit = 15;
        /// <summary>
        /// 槽深上限(μm)
        /// </summary>
        [RecipeParam("EtchingLineDepthUpperLimit", "槽深上限")]
        public double EtchingLineDepthUpperLimit
        {
            get { return _etchingLineDepthUpperLimit; }
            set { _etchingLineDepthUpperLimit = value; RaisePropertyChanged(); }
        }

        private double _etchingLineWidthLowerLimit = 500;
        /// <summary>
        /// 槽宽下限(μm)
        /// </summary>
        [RecipeParam("EtchingLineWidthLowerLimit", "槽宽下限")]
        public double EtchingLineWidthLowerLimit
        {
            get { return _etchingLineWidthLowerLimit; }
            set { _etchingLineWidthLowerLimit = value; RaisePropertyChanged(); }
        }

        private double _etchingLineWidthUpperLimit = 500;
        /// <summary>
        /// 槽宽上限(μm)
        /// </summary>
        [RecipeParam("EtchingLineWidthUpperLimit", "槽宽上限")]
        public double EtchingLineWidthUpperLimit
        {
            get { return _etchingLineWidthUpperLimit; }
            set { _etchingLineWidthUpperLimit = value; RaisePropertyChanged(); }
        }

        private double _etchingPointDistLowerLimit = 1050;
        /// <summary>
        /// 槽间距下限(μm)
        /// </summary>
        [RecipeParam("EtchingPointDistLowerLimit", "槽间距下限")]
        public double EtchingPointDistLowerLimit
        {
            get { return _etchingPointDistLowerLimit; }
            set { _etchingPointDistLowerLimit = value; RaisePropertyChanged(); }
        }

        private double _etchingPointDistUpperLimit = 1050;
        /// <summary>
        /// 槽间距上限(μm)
        /// </summary>
        [RecipeParam("EtchingPointDistUpperLimit", "槽间距上限")]
        public double EtchingPointDistUpperLimit
        {
            get { return _etchingPointDistUpperLimit; }
            set { _etchingPointDistUpperLimit = value; RaisePropertyChanged(); }
        }

        private double _pointHeightLowerLimit = 80;
        /// <summary>
        /// 凸点高度下限(μm)
        /// </summary>
        [RecipeParam("PointHeightLowerLimit", "凸点高度下限")]
        public double PointHeightLowerLimit
        {
            get { return _pointHeightLowerLimit; }
            set { _pointHeightLowerLimit = value; RaisePropertyChanged(); }
        }

        private double _pointHeightUpperLimit = 80;
        /// <summary>
        /// 凸点高度上限(μm)
        /// </summary>
        [RecipeParam("PointHeightUpperLimit", "凸点高度上限")]
        public double PointHeightUpperLimit
        {
            get { return _pointHeightUpperLimit; }
            set { _pointHeightUpperLimit = value; RaisePropertyChanged(); }
        }

        private double _pointDiameterLowerLimit = 2100;
        /// <summary>
        /// 凸点直径下限(μm)
        /// </summary>
        [RecipeParam("PointDiameterLowerLimit", "凸点直径下限")]
        public double PointDiameterLowerLimit
        {
            get { return _pointDiameterLowerLimit; }
            set { _pointDiameterLowerLimit = value; RaisePropertyChanged(); }
        }

        private double _pointDiameterUpperLimit = 2100;
        /// <summary>
        /// 凸点直径上限(μm)
        /// </summary>
        [RecipeParam("PointDiameterUpperLimit", "凸点直径上限")]
        public double PointDiameterUpperLimit
        {
            get { return _pointDiameterUpperLimit; }
            set { _pointDiameterUpperLimit = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constrctor
        public OtherConfigModel()
        {
            EnsureSummaryTestItems();
        }
        #endregion

        private void EnsureSummaryTestItems()
        {
            string[] grooveItems = { "槽孔深度", "槽孔宽度", "槽孔间距" };
            string[] convexItems = { "压花高度均值1", "压花高度均值2", "压花高度均值极差", "压花高度极大值（单点）", "压花高度极小值（单点）" };

            // 刻槽按工艺大类勾选，压花按客户要求的5个高度明细项勾选。
            HashSet<string> existingKeys = new HashSet<string>();
            for (int index = _summaryTestItems.Count - 1; index >= 0; index--)
            {
                SummaryTestItemConfig item = _summaryTestItems[index];
                bool isGrooveItem = item.Category == "刻槽" && grooveItems.Contains(item.Name);
                bool isConvexItem = item.Category == "压花" && convexItems.Contains(item.Name);
                string key = $"{item.Category}|{item.Name}";
                if ((!isGrooveItem && !isConvexItem) || !existingKeys.Add(key))
                    _summaryTestItems.RemoveAt(index);
            }

            AddSummaryTestItem("刻槽", "槽孔深度");
            AddSummaryTestItem("刻槽", "槽孔宽度");
            AddSummaryTestItem("刻槽", "槽孔间距");
            AddSummaryTestItem("压花", "压花高度均值1");
            AddSummaryTestItem("压花", "压花高度均值2");
            AddSummaryTestItem("压花", "压花高度均值极差");
            AddSummaryTestItem("压花", "压花高度极大值（单点）");
            AddSummaryTestItem("压花", "压花高度极小值（单点）");
        }

        private void AddSummaryTestItem(string category, string name)
        {
            if (_summaryTestItems.Any(item => item.Category == category && item.Name == name))
                return;

            _summaryTestItems.Add(new SummaryTestItemConfig
            {
                Category = category,
                Name = name
            });
        }


    }

    [Serializable]
    public class SummaryTestItemConfig : BindableBase
    {
        private string _category = string.Empty;
        /// <summary>
        /// 汇总测试项目分类。
        /// </summary>
        public string Category
        {
            get { return _category; }
            set { _category = value; RaisePropertyChanged(); }
        }

        private string _name = string.Empty;
        /// <summary>
        /// 汇总测试项目名称。
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        private bool _isChecked;
        /// <summary>
        /// 是否输出该测试项目。
        /// </summary>
        public bool IsChecked
        {
            get { return _isChecked; }
            set { _isChecked = value; RaisePropertyChanged(); }
        }
    }
}
