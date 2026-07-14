using Prism.Mvvm;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.CustomProject
{
    /// <summary>
    /// 用来管理不同现场的配置，管理Modules下的DLL是否可以被加载
    /// </summary>
    [ExposedService(Lifetime.Singleton, 3, typeof(ICustomProjectManager))]
    [Serializable]
    public class CustomProjectManager : BindableBase, ICustomProjectManager
    {
        #region Fields

        #endregion

        #region Properties

        #endregion

        #region Constructor
        public CustomProjectManager()
        {
            
        }
        #endregion

        #region Methods
        public void Dispose()
        {

        }

        public bool Init()
        {

            return false;
        }
        #endregion

        #region Commands

        #endregion
    }
}
