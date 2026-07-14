using ALGO.RegionTrans;
using ALGO.RegionTrans.ViewModels;
using ALGO.RegionTrans.Views;
using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using ImageTool.Halcon.Model;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static ALGO.RegionTrans.ViewModels.RegionTransViewModel;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.RegionTrans.ViewModels
{
    public class RegionTransViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Proerties

        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        public new RegionTransModel ModelParam
        {
            get { return base.ModelParam as RegionTransModel; }
            set { base.ModelParam = value; }
        }

        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }


        public enum RegionTransMode
        {
            [Description("形状转换")]
            形状转换,

            [Description("最小形状")]
            最小形状,
        }

        public enum RegionTransType
        {
            [Description("convex")]
            凸包性,

            [Description("ellipse")]
            椭圆,

            [Description("outer_circle")]
            最小外接圆,

            [Description("inner_circle")]
            最大内接圆,

            [Description("rectangle1")]
            最小外接矩形1,

            [Description("rectangle2")]
            最小外接矩形2,

            [Description("inner_rectangle1")]
            最大内接矩形1,

            [Description("inner_rectangle2")]
            最大内接矩形2,
        }

        public enum SmallRegionType
        {
            [Description("圆形")]
            圆形,

            [Description("矩形1")]
            矩形1,

            [Description("矩形2")]
            矩形2,
        }

        public Array RegionTransModes { get; set; } = Enum.GetValues(typeof(RegionTransMode));

        public Array RegionTransTypes { get; set; } = Enum.GetValues(typeof(RegionTransType));

        public Array SmallRegionTypes { get; set; } = Enum.GetValues(typeof(SmallRegionType));

        #endregion

        #region Constructor
        public RegionTransViewModel()
        {
            
        }

        #endregion

        #region Methods
        public override void InitParam()
        {
            ModelParam = InitModelParam<RegionTransModel>();
            ModelParam.UpdateTransTypeList();
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
                case "取消":
                    CloseDialog(ButtonResult.No);

                    break;
                case "执行":
                    ModelParam.ExecuteModule();

                    break;
                case "确认":
                    {
                        PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.AddRange(
                        ModelParam.OutputParams.Where(item => item.IsGlobal &&
                        !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)));

                        ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                            item => item.Guid.ToString(),
                            item => (object)item);


                        if (!PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.ContainsKey(RegionTransModel.ModuleName))
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.Add(RegionTransModel.ModuleName, ModelParam.mWindowH);
                        }
                        else
                        {
                            PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair[RegionTransModel.ModuleName] = ModelParam.mWindowH;
                        }

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

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            ModelParam.LoadKeyParam();

            //不显示说明只是加载
            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        public DelegateCommand<object> DataOperateCommand
        {
            get
            {
                return new DelegateCommand<object>
                (
                    (obj) =>
                    {
                        //var tempModelParam = ModelParam.DeepClone();

                        switch (obj?.ToString())
                        {
                            case "Add":
                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;

                                if (ModelParam.OutputParams.Where(item => item.Name == SltOutputParamName).ToList().Count >= 1)
                                {
                                    System.Windows.MessageBox.Show("已包含重名参数，请重新输入！");
                                }
                                else
                                {
                                    if (curSltParam.Resourece == ResoureceType.None)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            ParamName = curSltParam.Name,
                                            Serial = ModelParam.Serial,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name],
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.Value,
                                            ResourcePath = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name)?.ResourcePath,
                                            Serial = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name).Serial
                                        });
                                    }
                                }

                                break;

                            case "Delete":
                                if (CurrentOutputParam != null)
                                {
                                    ModelParam.OutputParams.Remove(CurrentOutputParam);
                                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
                                    CurrentOutputParam = null;
                                }

                                break;

                            default:
                                break;
                        }
                    }
                );
            }
        }

        #endregion
    }
}
