using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Main.UC.ViewModels
{
    public class DynamicRegionControlViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;

        public string RegionName { get; private set; }

        public DelegateCommand AddViewCommand { get; }

        public DynamicRegionControlViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            AddViewCommand = new DelegateCommand(OnAddView);
        }

        public void Initialize(string regionName)
        {
            RegionName = regionName;
        }

        /// <summary>
        /// 添加指定View
        /// </summary>
        private void OnAddView()
        {

            PrismProvider.Dispatcher.Invoke(() =>
            {
                //加载主界面
                PrismProvider.ModuleManager.LoadModule("ControlCardBaseModule");
                var region = _regionManager.Regions[RegionName];

                //导航到主区域
                region.RequestNavigate("ControlCardConfigView");

            });
        }
    }
}
