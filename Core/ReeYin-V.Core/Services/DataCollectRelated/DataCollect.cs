
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.DataCollectRelated
{
    /// <summary>
    /// 通用MeasureData继承基类
    /// </summary>
    public class MeasureData
    {
        //无效数据，默认使用：888888.0

        /// <summary>
        /// 原始数据集
        /// <通道,<类型，数据>>
        /// </summary>
        public Dictionary<string, Dictionary<string,object>> OriginalDatas { get; set; }

        /// <summary>
        /// 面阵探测器的数据源，用到再初始化
        /// </summary>
        public List<float[]> AreaData { get; set; }

        /// <summary>
        /// 数据接收时间
        /// </summary>
        public DateTime RTime { get; set; }

        /// <summary>
        /// X位置
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y位置
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Z位置
        /// </summary>
        public double Z { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get => _isValid; set => _isValid = value; }
        private bool _isValid = true;



    }


    /// <summary>
    /// 通用ProcessedData继承基类
    /// </summary>
    public class ProcessedData
    {
        public Mat Gray { get; set; }

        /// <summary>
        /// 原始数据
        /// </summary>
        public List<MeasureData> OriginDataList { get; set; }

        /// <summary>
        /// 处理后的数据
        /// </summary>
        public List<MeasureData> MeasureDataList { get; set; }

        /// <summary>
        /// pd创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// pd处理结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>       
        public bool IsValid { get => _isValid; set => _isValid = value; }
        private bool _isValid = true;

        /// <summary>
        /// 方向
        /// </summary>
        public bool IsForward { get; set; }

        /// <summary>
        /// 速度
        /// </summary>
        public double Speed { get; set; }

        /// <summary>
        /// 扫描次数
        /// </summary>
        public int ScanCount { get; set; }

        #region Constructor
        public ProcessedData()
        {
            
        }

        ~ProcessedData()
        {

        }
        #endregion


        #region MemoryMap
        /// <summary>
        /// 缓存
        /// </summary>
        [JsonIgnore]
        private Dictionary<string, object> _memoryParaMap = new Dictionary<string, object>();

        public void SetMemoryPara(string paraName, object paraValue)
        {
            lock (this)
            {
                _memoryParaMap[paraName] = paraValue;
            }
        }

        public T GetMemoryPara<T>(string paraName, T defaultValue)
        {
            lock (this)
            {
                if (_memoryParaMap.ContainsKey(paraName))
                {
                    return (T)_memoryParaMap[paraName];
                }
                else
                {
                    return defaultValue;
                }
            }
        }

        #endregion

    }
}
