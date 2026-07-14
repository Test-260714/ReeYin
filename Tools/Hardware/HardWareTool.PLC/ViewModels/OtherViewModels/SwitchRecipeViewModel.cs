using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HardWareTool.PLC.ViewModels
{
    public class SwitchRecipeViewModel : DialogViewModelBase
    {
        #region Fields
        private IConfigManager ConfigManager { get; }
        #endregion

        #region Properties

        private RecipeManager _modelParam;

        public RecipeManager ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public SwitchRecipeViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;

        }
        #endregion

        #region Commands

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            var Entities = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel ?? new PLCSetModel();
            if (Entities.Models.Count > 0)
                Entities.CurSlt = Entities.Models[0];

            ModelParam = Entities.RecipeManager;
        });

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "切换配方":
                    {
                        PrismProvider.DialogService.ShowDialog("PLCRecipeManagerView", new DialogParameters
                        {
                            { "Title", "PLC配方管理" },
                            { "Icon", "\ue6b7" },
                        }, result =>
                        {
                            if (result.Result == ButtonResult.OK)
                            {
                                ModelParam = result.Parameters.GetValue<object>("Param") as RecipeManager;



                            }
                        }, nameof(DialogWindowView));

                    }break;

                default:
                    break;
            }
        });

        #endregion

    }
}
