using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.DataCollectRelated
{
    /// <summary>
    /// IDataProcessor
    /// 数据处理服务
    /// </summary>
    public interface IDataProcessorService
    {
        /// <summary>
        /// 初始化
        /// </summary>
        void Init();

        /// <summary>
        /// 处理前准备工作
        /// </summary>
        /// <param name="param"></param>
        void PrepareForProcess(object param);

        /// <summary>
        /// 数据处理
        /// </summary>
        Task<ProcessedData> Process(List<MeasureData> OriginMeasureData);

        /// <summary>
        /// 检测后处理
        /// </summary>
        void PostProcess(ProcessedData processedData);

        /// <summary>
        /// 自定义处理
        /// </summary>
        /// <param name="cmdName"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        object SendCommand(string cmdName, object para);
    }
}
