using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static OpenCvSharp.ML.SVM;

namespace HardWareTool.PLC.ViewModels
{
    public class PLCRecipeManagerViewModel : DialogViewModelBase
    {
        #region Fields

        public PLCBase CurPLC;

        private IConfigManager ConfigManager { get; }
        #endregion

        #region Properties
        private RecipeManager _modelParam;
        public RecipeManager ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private bool _editCustomParam = false;
        /// <summary>
        /// 编辑自定义参数
        /// </summary>
        public bool EditCustomParam
        {
            get { return _editCustomParam; }
            set { _editCustomParam = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public PLCRecipeManagerViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;

            var Entities = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel ?? new PLCSetModel();
            if (Entities.Models.Count > 0)
            {
                CurPLC = Entities.Models[0];
                Entities.CurSlt = Entities.Models[0];
            }

            ModelParam = Entities.RecipeManager;
        }
        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "切换配方":
                    {
                        foreach (var recipe in ModelParam.Recipes)
                        {
                            if (ModelParam.SltRecipe == recipe)
                            {
                                CurPLC.WritePLCPara(new PLCParaInfoModel
                                {
                                    PLCAddress = ModelParam.SltRecipe.Addr,
                                    ParaType = ModelParam.SltRecipe.ParamType,
                                    ParaValue = ModelParam.SltRecipe.Value
                                });
                                recipe.IsUsing = true;
                            }
                            else
                                recipe.IsUsing = false;


                        }

                    }break;

                case "编辑配方参数":
                    {
                        EditCustomParam = true;
                    }
                    break;

                case "配方参数关闭":
                    {
                        if (ModelParam.SltRecipe == null) return;

                    }
                    break;
                case "配方参数打开":
                    {
                        if (ModelParam.SltRecipe == null) return;

                        //PrismProvider.Dispatcher.BeginInvoke(() =>
                        //{
                        //    ModelParam.SltPLCOrder.SltTab = (int)ModelParam.SltPLCOrder.OperationType;
                        //});

                    }
                    break;

                case "触发事件":
                    {
                        //if (ModelParam.SingleExecute(ModelParam.SltPLCOrder) != NodeStatus.Success)
                        //{
                        //    MessageBox.Show("执行失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        //}
                    }
                    break;

                case "执行":
                    {

                    }
                    break;

                case "取消":
                    {
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            //{ "Param", ModelParam },
                        });
                    }
                    break;


                case "添加新项":
                    {
                        ModelParam.Recipes.Add(new PLCOrder()
                        {
                            OperationType = OperationType.写单个地址,
                            ParamType = EnumParaInfoModelParaType.Bool,
                        });
                    }
                    break;

                case "删除选中项":
                    {
                        ModelParam.Recipes.Remove(ModelParam.SltRecipe);
                        ModelParam.SltRecipe = null;
                    }
                    break;


                case "确认":
                    {
                        MessageBoxResult result = MessageBox.Show("确定要保存当前参数吗?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }
                        var Entities = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel ?? new PLCSetModel();
                        if (Entities.Models.Count > 0)
                            Entities.CurSlt = Entities.Models[0];

                        Entities.RecipeManager = ModelParam;

                        ConfigManager.Write(ConfigKey.PLCConfig, Entities);

                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });
                    }
                    break;
                default:
                    break;
            }
        });
        #endregion

        #region Methods

        #endregion

    }
}
