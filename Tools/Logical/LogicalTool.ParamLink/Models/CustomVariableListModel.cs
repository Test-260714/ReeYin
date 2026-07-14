using Newtonsoft.Json;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.ParamLink.Models
{
    [Serializable]
    public class CustomVariableListModel : ModelParamBase
    {
        #region Fields

        #endregion

        #region Properties
        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private ObservableCollection<TransmitParam> _LastNodeInputParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 上一节点输入
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<TransmitParam> LastNodeInputParams
        {
            get { return _LastNodeInputParams; }
            set { _LastNodeInputParams = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<TransmitParam> _globalParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 全局参数
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<TransmitParam> GlobalParams
        {
            get { return _globalParams; }
            set { _globalParams = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<TransmitParam> _customGlobalParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 自定义全局参数
        /// </summary>
        public ObservableCollection<TransmitParam> CustomGlobalParams
        {
            get { return _customGlobalParams; }
            set { _customGlobalParams = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public CustomVariableListModel()
        {
            GlobalParams = PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams;
            CustomGlobalParams = PrismProvider.ProjectManager.SltCurSolutionItem.CustomGlobalParams;
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

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：监听模块执行完成！！！");

            var RunTime = DateTime.Now.Subtract(start);
            Console.WriteLine($"模块执行时间：{RunTime.TotalMilliseconds} 毫秒");
            return Output;

        }


        #endregion


    }
}
