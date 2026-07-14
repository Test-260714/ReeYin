using ImageTool.Halcon;
using Microsoft.IdentityModel.Tokens;
using Prism.Common;
using ReeYin.CSharpScript.Models;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.CSharpScript.ViewModels
{
    [Serializable]
    public class CSharpScriptViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields

        #endregion

        #region Proerties

        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        public CSharpScriptModel ModelParam
        {
            get { return base.ModelParam as CSharpScriptModel; }
            set { base.ModelParam = value; }
        }

        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }

        private string _newDll;

        public string NewDll
        {
            get { return _newDll; }
            set { _newDll = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public CSharpScriptViewModel()
        {

        }

        ~CSharpScriptViewModel()
        {

        }
        #endregion

        #region OverrideMethods
        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
            }
        }

        public override void InitParam()
        {
            InitModelParam<CSharpScriptModel>();

            //ModelParam.moduleInputParam = (Param as IModuleParam).moduleInputParam;
            if (ModelParam.ManagedDllNames == null)
                ModelParam.ManagedDllNames =
                [ "halcondotnetxl.dll", "ReeYin_V.Core.dll", "ReeYin.exe", "ReeYin.dll",
                    "Prism.dll", "Prism.Wpf.dll", "Prism.Events.dll", "Prism.DryIoc.Wpf.dll" ];

            ModelParam.GlobalParams = PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams;
            ModelParam.CustomGlobalParams = PrismProvider.ProjectManager.SltCurSolutionItem.CustomGlobalParams;
            ModelParam.OutputParamResource.Clear();
            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            ModelParam.CSharpScriptName = $"{Serial.ToString("D3")}";
            //ModelParamBase.ModuleName = ModelParam.Serial.ToString("D3") + "_" + $"{Name}";

        }
        //public override void InitParam()
        //{
        //    if (Param != null && (Param is CSharpScriptModel))
        //        ModelParam = Param as CSharpScriptModel;
        //    else
        //    {
        //        ModelParam = new CSharpScriptModel();
        //        ModelParam.moduleInputParam = (Param as IModuleParam).moduleInputParam;
        //    }


        //    if (ModelParam.ManagedDllNames == null)
        //        ModelParam.ManagedDllNames =
        //        [ "halcondotnetxl.dll", "ReeYin_V.Core.dll", "ReeYin.exe", "ReeYin.dll",
        //            "Prism.dll", "Prism.Wpf.dll", "Prism.Events.dll", "Prism.DryIoc.Wpf.dll" ];

        //    ModelParam.GlobalParams = PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams;
        //    ModelParam.CustomGlobalParams = PrismProvider.ProjectManager.SltCurSolutionItem.CustomGlobalParams;
        //    ModelParam.OutputParamResource.Clear();
        //    if (ModelParam.Serial == -999)
        //        ModelParam.Serial = Serial;

        //    ModelParam.CSharpScriptName = $"{Serial.ToString("D3")}";
        //    ModelParamBase.ModuleName = ModelParam.Serial.ToString("D3") + "_" + $"{Name}";

        //    if (!PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.ContainsKey(CSharpScriptModel.ModuleName))
        //    {
        //        PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.Add(CSharpScriptModel.ModuleName, ModelParam);
        //    }


        //}

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
                    ModelParam?.ExecuteModule();

                    break;

                case "编译":

                    ModelParam?.CompileScript();

                    break;

                case "确认":
                    {
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });
                    }
                    break;

                case "输出添加新项":
                    {
                        ModelParam.OutputParams.Add(new TransmitParam
                        {
                            LinkGuid = Guid,
                            Name = "",
                            Type = DataType._object,
                            Value = null,
                            Serial = ModelParam.Serial,
                            ParentNode = Name,
                        });

                    }
                    break;

                case "输出删除选中项":
                    {
                        MessageBoxResult result = MessageBox.Show("确定要删除此条新数据吗?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }

                        ModelParam.OutputParams.Remove(ModelParam.SltOutputParam);
                    }
                    break;

                case "添加DLL":
                    {
                        if (ModelParam.ManagedDllNames == null)
                        {
                            ModelParam.ManagedDllNames = new ObservableCollection<string>();
                        }

                        if (string.IsNullOrWhiteSpace(NewDll))
                        {
                            MessageBox.Show("请输入 DLL 文件名。");
                            return;
                        }

                        string dllName = NewDll.Trim();
                        if (ModelParam.ManagedDllNames.Any(item => string.Equals(item, dllName, StringComparison.OrdinalIgnoreCase)))
                        {
                            MessageBox.Show($"已包含{dllName}，请勿重复添加！");
                            return;
                        }

                        if (!ModelParam.IsManagedDllValid(dllName))
                        {
                            MessageBox.Show("此DLL验证无效！！！");
                            return;
                        }

                        ModelParam.ManagedDllNames.Add(dllName);
                    }
                    break;
                case "ExportConfig":

                    MessageBox.Show("导出配置成功！");

                    break;

                default:
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (ModelParam.Serial == -999)
            {
                ModelParam.Name = Name;
                ModelParam.Serial = Serial;
            }

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
                        switch (obj?.ToString())
                        {
                            case "Add":
                                if (string.IsNullOrWhiteSpace(SltOutputParamName) ||
                                    !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out var outputResource) ||
                                    outputResource is not TransmitParam curSltParam)
                                {
                                    MessageBox.Show("请选择有效的输出参数。");
                                    return;
                                }

                                if (ModelParam.OutputParams.Where(item => item.Name == SltOutputParamName).ToList().Count >= 1)
                                {
                                    MessageBox.Show("已包含重名参数，请重新输入！");
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
                                            ParentNode = Name,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name].DeepCopy(),
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        var inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name);
                                        if (inputParam == null)
                                        {
                                            MessageBox.Show("未找到对应的输入参数。");
                                            return;
                                        }

                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            ParentNode = Name,
                                            Value = inputParam.Value.DeepClone(),
                                            ResourcePath = inputParam.ResourcePath,
                                            Serial = inputParam.Serial
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
