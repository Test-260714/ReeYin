using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using Custom.WaferFlatnessMeasure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.WaferFlatnessMeasure.ViewModels
{
    public class ResultDisposeViewModel : DialogViewModelBase, INavigationAware
    {
        #region Fields

        #endregion

        #region Properties
        private SensorDataCollectionModel _model = new SensorDataCollectionModel();

        public SensorDataCollectionModel ModelParam
        {
            get { return _model; }
            set { _model = value; RaisePropertyChanged(); }
        }

        #endregion
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var parameters = navigationContext.Parameters;

            int Serail = -999;
            if (parameters.TryGetValue<int>("Serial", out var id))
                Serail = id;

            ModelParam = PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches[$"{Serail.ToString("D3")}"] as SensorDataCollectionModel;

        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;

        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }
    }
}
