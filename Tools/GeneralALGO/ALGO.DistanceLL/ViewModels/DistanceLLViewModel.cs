using Newtonsoft.Json;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Linq;
using System.Windows;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.DistanceLL.ViewModels
{
    [Serializable]
    public class DistanceLLViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region 属性
        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { SetProperty(ref _sltOutputParamName, value); }
        }

        public new DistanceLLModel ModelParam
        {
            get { return base.ModelParam as DistanceLLModel; }
            set
            {
                base.ModelParam = value;
                RaisePropertyChanged();
            }
        }

        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { SetProperty(ref _currentOutputParam, value); }
        }
        #endregion

        #region 生命周期
        public override void OnDialogClosed()
        {
            if (ModelParam != null)
            {
                ModelParam.IsDebug = false;
            }
        }
        #endregion

        #region 方法
        public override void InitParam()
        {
            ModelParam = InitModelParam<DistanceLLModel>();
            // 基类 InitOutputParamResource 统一写死 _object，这里按成员真实类型重建，
            // 让 Distance 等数值输出落到 DataType.Double。
            RebuildOutputParamResource();
            ModelParam.LoadKeyParam();
        }

        /// <summary>
        /// 按输出成员的真实类型重建输出参数资源，覆盖基类默认的 _object 类型。
        /// </summary>
        private void RebuildOutputParamResource()
        {
            var dataPoints = OutputParamCollector.GetDataPoints(typeof(DistanceLLModel));
            var values = OutputParamCollector.GetDataPointValues(ModelParam);
            ModelParam.OutputParamResource.Clear();
            foreach (var point in dataPoints)
            {
                ModelParam.OutputParamResource.Add(point.Name + $"[{point.Description}]", new TransmitParam
                {
                    LinkGuid = Guid,
                    Name = point.Name,
                    Type = ResolveOutputDataType(point.MemberType),
                    Resourece = ResoureceType.None,
                    Value = values.TryGetValue(point.Name, out object value) ? value : null,
                    Describe = point.Description,
                    ResourcePath = point.MemberInfo.DeclaringType.FullName + "." + point.Name
                });
            }
        }

        /// <summary>
        /// 根据输出成员的真实类型推断对应的 DataType。
        /// 数值类型输出为 Double/Int，其余复杂对象保持 _object。
        /// </summary>
        private static DataType ResolveOutputDataType(Type memberType)
        {
            // 可空类型取其基础类型，保证 double? 仍被识别为 Double
            memberType = Nullable.GetUnderlyingType(memberType) ?? memberType;

            if (memberType == typeof(double) || memberType == typeof(float) || memberType == typeof(decimal))
                return DataType.Double;

            if (memberType == typeof(int) || memberType == typeof(long) || memberType == typeof(short))
                return DataType.Int;

            if (memberType == typeof(bool))
                return DataType.Bool;

            if (memberType == typeof(string))
                return DataType.String;

            return DataType._object;
        }

        private void AddSelectedOutputParam()
        {
            if (string.IsNullOrWhiteSpace(SltOutputParamName)
                || !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object value)
                || value is not TransmitParam curSltParam)
            {
                return;
            }

            if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
            {
                System.Windows.MessageBox.Show("已包含重名参数，请重新输入！");
                return;
            }

            if (curSltParam.Resourece == ResoureceType.None)
            {
                ModelParam.OutputParams.Add(new TransmitParam
                {
                    LinkGuid = Guid,
                    ParamName = curSltParam.Name,
                    Serial = ModelParam.Serial,
                    Name = SltOutputParamName,
                    // 沿用资源项已派生的类型（Distance 为 Double，对象为 _object）
                    Type = curSltParam.Type,
                    Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name],
                    ResourcePath = curSltParam.ResourcePath,
                });
                return;
            }

            if (curSltParam.Resourece == ResoureceType.Inupt)
            {
                TransmitParam inputParam = ModelParam.InputParams.FirstOrDefault(item => item.Name == curSltParam.Name);
                if (inputParam == null)
                    return;

                ModelParam.OutputParams.Add(new TransmitParam
                {
                    LinkGuid = Guid,
                    Name = SltOutputParamName,
                    Type = DataType._object,
                    ParentNode = Name,
                    Value = inputParam.Value,
                    ResourcePath = inputParam.ResourcePath,
                    Serial = inputParam.Serial
                });
            }
        }

        private void DeleteSelectedOutputParam()
        {
            if (CurrentOutputParam == null)
                return;

            ModelParam.OutputParams.Remove(CurrentOutputParam);
            PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams.Remove(CurrentOutputParam);
            CurrentOutputParam = null;
        }
        #endregion

        #region 命令
        /// <summary>通用指令。</summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "执行":
                    _ = ModelParam.ExecuteModule();
                    break;
                case "确认":
                    ModelParam.LoadKeyParam();
                    foreach (TransmitParam item in ModelParam.OutputParams.Where(item => item.IsGlobal &&
                        !PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == item.Guid)))
                    {
                        PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(item);
                    }

                    ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                        item => item.Guid.ToString(),
                        item => (object)item);

                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam },
                    });
                    break;
                default:
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            ModelParam.LoadKeyParam();
            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        [JsonIgnore]
        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    AddSelectedOutputParam();
                    break;
                case "Delete":
                    DeleteSelectedOutputParam();
                    break;
                default:
                    break;
            }
        });
        #endregion
    }
}
