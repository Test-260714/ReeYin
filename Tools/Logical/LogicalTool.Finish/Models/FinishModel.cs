using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.Finish.Models
{
    [Serializable]
    public class FinishModel : ModelParamBase
    {
        #region Fields

        #endregion

        #region Properties

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        #endregion

        #region Constructor
        public FinishModel()
        {
            TriggerModuleRun += () =>
            {
                return ExecuteModule().Result;
            };
        }
        #endregion

        #region Methods
        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var start = DateTime.Now;
            //模拟执行时间
            await Task.Delay(0);

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：结束模块执行完成！！！");

            var RunTime = DateTime.Now.Subtract(start);
            Console.WriteLine($"模块执行时间：{RunTime.TotalMilliseconds} 毫秒");
            return Output;

        }
        #endregion


    }
}
