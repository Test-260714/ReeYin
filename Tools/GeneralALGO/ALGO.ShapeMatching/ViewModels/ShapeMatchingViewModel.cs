using ALGO.ShapeMatching;
using ALGO.ShapeMatching.Views;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;


namespace ALGO.ShapeMatching.ViewModels
{
    /// <summary>
    /// 形状匹配参数弹窗的 ViewModel，负责参数初始化、模板命令和输出参数确认。
    /// </summary>
    public class ShapeMatchingViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region 属性

        private string _sltOutputParamName = string.Empty;
        /// <summary>
        /// 当前准备添加到输出列表的参数名称。
        /// </summary>
        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前弹窗绑定的形状匹配模块参数模型。
        /// </summary>
        public new ShapeMatchingModel ModelParam
        {
            get => base.ModelParam as ShapeMatchingModel ?? throw new InvalidOperationException("形状匹配参数模型尚未初始化。");
            set
            {
                base.ModelParam = value;
                RaisePropertyChanged();
            }
        }

        private TransmitParam? _currentOutputParam;
        /// <summary>
        /// 输出参数列表中当前选中的参数项。
        /// </summary>
        public TransmitParam? CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 界面下拉框使用的模板匹配方式枚举集合。
        /// </summary>
        public Array ShapeMatchingModes { get; set; } = Enum.GetValues(typeof(ShapeMatchingMode));

        /// <summary>
        /// 界面下拉框使用的 ROI 获取方式枚举集合。
        /// </summary>
        public Array RegionCreatModes { get; set; } = Enum.GetValues(typeof(RegionCreatMode));

        #endregion

        #region 构造函数
        /// <summary>
        /// 创建形状匹配参数弹窗 ViewModel。
        /// </summary>
        public ShapeMatchingViewModel()
        {
        }
        #endregion

        #region 方法
        /// <summary>
        /// 按 ReeYin-V 模块生命周期初始化 Model，并加载当前输入链路用于预览。
        /// </summary>
        public override void InitParam()
        {
            ModelParam = InitModelParam<ShapeMatchingModel>();
            ModelParam.EnsureParamDefinitions();

            // 只刷新链路输入和预览，避免打开页面时用旧配方值覆盖项目文件中已保存的算法参数。
            ModelParam.RefreshRuntimeInputsFromCurrentLinks(refreshPreview: true);
        }
        #endregion

        #region 命令

        /// <summary>
        /// 处理取消、执行和确认按钮命令。
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>(async (order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;

                case "执行":
                    await ModelParam.ExecuteModule();
                    break;

                case "确认":
                    {
                        ModelParam.PrepareForConfirm();
                        foreach (var outputParam in ModelParam.OutputParams.Where(item => item.IsGlobal))
                        {
                            // 逐项合并可避免 .NET 10 AddRange 扩展方法推断冲突，并保留原有去重规则。
                            if (!PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == outputParam.Guid))
                                PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(outputParam);
                        }

                        ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                            item => item.Guid.ToString(),
                            item => (object)item);

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

        /// <summary>
        /// 在隐藏加载模式下返回当前模型参数。
        /// </summary>
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (Visibility == Visibility.Hidden)
            {
                ModelParam.PrepareForConfirm();
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        /// <summary>
        /// 处理输出参数增删、模板加载和模板创建保存命令。
        /// </summary>
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
                                if (string.IsNullOrEmpty(SltOutputParamName) ||
                                    ModelParam.OutputParamResource == null ||
                                    !ModelParam.OutputParamResource.ContainsKey(SltOutputParamName))
                                    return;

                                var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;
                                if (curSltParam == null) return;

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
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name],
                                            ResourcePath = curSltParam.ResourcePath,
                                        });
                                    }
                                    else if (curSltParam.Resourece == ResoureceType.Inupt)
                                    {
                                        var inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name);
                                        if (inputParam == null)
                                            return;

                                        ModelParam.OutputParams.Add(new TransmitParam
                                        {
                                            LinkGuid = Guid,
                                            Name = SltOutputParamName,
                                            Type = DataType._object,
                                            Value = inputParam.Value,
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

                            case "加载模板":
                                OpenFileDialog dialog = new OpenFileDialog
                                {
                                    Title = "选择模板文件",
                                    Filter = "Shape Model Files (*.shm)|*.shm|NCC Model Files (*.ncm)|*.ncm|All Files (*.*)|*.*",
                                    Multiselect = false
                                };

                                var dialogResult = dialog.ShowDialog();
                                if (dialogResult == true)
                                {
                                    if (!ModelParam.LoadShapeModel(dialog.FileName))
                                    {
                                        MessageBox.Show("模板加载失败，请检查文件是否正确！");
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("未选择文件，请重新选择！");
                                }
                                break;

                            case "创建保存":
                                ModelParam.CreateAndSaveShapeModel();
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
