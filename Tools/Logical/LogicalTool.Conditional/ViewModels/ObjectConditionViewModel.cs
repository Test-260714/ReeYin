using LogicalTool.Conditional.Models;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.Conditional.ViewModels
{
    [Serializable]
    public class ObjectConditionViewModel : IViewModuleParam, INavigationAware
    {

        #region Fields

        #endregion

        #region Properties

        #endregion

        #region Constructor
        public ObjectConditionViewModel()
        {

        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var Codition = navigationContext.Parameters.GetValue<JudgeCodition>("Codition");

        }
        #endregion

        #region Commands

        #endregion
    }
}
