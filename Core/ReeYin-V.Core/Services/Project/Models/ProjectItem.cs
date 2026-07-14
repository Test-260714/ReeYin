using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Project.Models
{
    /// <summary>
    /// 和解决方案相关的都放到这个类中，统一管理
    /// </summary>
    [Serializable]
    public class ProjectItem : BindableBase
    {
        #region Fields
        /// <summary>
        /// 程序运行过程中的相关参数
        /// </summary>
        public NodifySolutionItem CurSolutionItem { get; set; } = new NodifySolutionItem();

        /// <summary>
        /// 基础信息
        /// </summary>
        public ProjectItemBaseInfo BaseInfo { get; set; } = new ProjectItemBaseInfo();

        /// <summary>
        /// 其他配置
        /// </summary>
        public Dictionary<string, object> OtherConfig { get; set; } = new Dictionary<string, object>();
        #endregion

        #region Constructor
        public ProjectItem()
        {
            CurSolutionItem ??= new NodifySolutionItem();
            BaseInfo ??= new ProjectItemBaseInfo();
            OtherConfig ??= new Dictionary<string, object>();
        }
        #endregion

        #region Methods

        #endregion
    }
}
