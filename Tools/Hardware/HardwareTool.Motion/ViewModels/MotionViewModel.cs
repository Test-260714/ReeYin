using HardwareTool.Motion.Models;
using Newtonsoft.Json;
using HardwareTool.Motion.Views;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Linq;
using System.Windows;

namespace HardwareTool.Motion.ViewModels
{
    [Serializable]
    public class MotionViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Fields
        private const string REGION_NAME = "MotionOperationRegion";
        #endregion

        #region Properties
        [JsonIgnore]
        public IRegionManager RegionManager { get; set; } = null!;

        [JsonIgnore]
        private MotionModel _modelParam = null!;

        public MotionModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public MotionViewModel()
        {

        }

        #endregion

        #region Methods
        public void Init()
        {

        }

        public override void InitParam()
        {
            if (Param is MotionModel motionModel)
                ModelParam = motionModel;
            else
                ModelParam = new MotionModel();

            ModelParam.RefreshControlCardContext();
        }

        private void SyncSelectedMovement()
        {
            if (ModelParam?.SltMovementLocus == null) return;

            ModelParam.SltMovementLocus.SltTab = (int)ModelParam.SltMovementLocus.MovingMode;
        }

        private void NavigateToSelectedMovement()
        {
            if (ModelParam?.SltMovementLocus == null) return;

            SyncSelectedMovement();
            NavigateToViewByOperationType(ModelParam.SltMovementLocus.MovingMode);
        }

        private void NavigateToViewByOperationType(CardOperaion operationType)
        {
            if (RegionManager == null) return;

            string? viewName = operationType switch
            {
                CardOperaion.点 => nameof(PointOperationView),
                CardOperaion.IO => nameof(IoOperationView),
                CardOperaion.直线线段 => nameof(LineOperationView),
                CardOperaion.圆弧线段 => nameof(ArcOperationView),
                CardOperaion.位置比较 => nameof(PosCompareOperationView),
                CardOperaion.延时 => nameof(DelayMotionOperationView),
                CardOperaion.触发事件 => nameof(TriggerEventOperationView),
                CardOperaion.自定义 => nameof(CustomMotionOperationView),
                _ => null
            };

            if (viewName == null) return;

            var navigationParams = new NavigationParameters
            {
                { "ModelParam", ModelParam }
            };

            try
            {
                RegionManager.RequestNavigate(REGION_NAME, viewName, navigationParams);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导航错误：{ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region Commands
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            ModelParam?.RefreshControlCardContext();

            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            if (!string.IsNullOrWhiteSpace(ModelParam.SltModelName))
            {
                var controlCard = ModelParam.Models?.FirstOrDefault(c => c.NickName == ModelParam.SltModelName);
                if (controlCard != null)
                {
                    ModelParam.ControlCard = controlCard;
                }
            }

            if (ModelParam?.SltMovementLocus != null)
            {
                PrismProvider.Dispatcher.BeginInvoke(() =>
                {
                    NavigateToSelectedMovement();
                });
            }

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam! },
                });
            }
        });

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "切换模块":
                    {
                        if (ModelParam?.ControlCard == null) return;
                        ModelParam.SltModelName = ModelParam.ControlCard.NickName;
                    }
                    break;

                case "选中轨迹":
                    {
                        if (ModelParam?.SltMovementLocus == null) return;

                        PrismProvider.Dispatcher.BeginInvoke(() =>
                        {
                            NavigateToSelectedMovement();
                        });
                    }
                    break;

                case "轨迹类型变更":
                    {
                        if (ModelParam?.SltMovementLocus == null) return;
                        NavigateToSelectedMovement();
                    }
                    break;

                case "执行":
                    {

                    }
                    break;

                case "取消":
                    {
                        CloseDialog(ButtonResult.OK, new DialogParameters());
                    }
                    break;

                case "添加新项":
                case "自定义添加新项":
                    {
                        if (ModelParam == null) return;

                        var movementLocus = new MovementLocus()
                        {
                            Describe = $"轨迹{ModelParam.MovementLocuss.Count + 1}"
                        };

                        ModelParam.MovementLocuss.Add(movementLocus);
                        ModelParam.SltMovementLocus = movementLocus;

                        PrismProvider.Dispatcher.BeginInvoke(() =>
                        {
                            NavigateToSelectedMovement();
                        });
                    }
                    break;

                case "删除选中项":
                case "自定义删除选中项":
                    {
                        if (ModelParam?.SltMovementLocus == null) return;

                        int removedIndex = ModelParam.MovementLocuss.IndexOf(ModelParam.SltMovementLocus);
                        ModelParam.MovementLocuss.Remove(ModelParam.SltMovementLocus);

                        if (ModelParam.MovementLocuss.Count == 0)
                        {
                            ModelParam.SltMovementLocus = null!;
                            break;
                        }

                        int nextIndex = Math.Min(removedIndex, ModelParam.MovementLocuss.Count - 1);
                        ModelParam.SltMovementLocus = ModelParam.MovementLocuss[nextIndex];

                        PrismProvider.Dispatcher.BeginInvoke(() =>
                        {
                            NavigateToSelectedMovement();
                        });
                    }
                    break;

                case "执行选中轨迹":
                case "自定义执行移动":
                case "位置比较执行移动":
                    {
                        if (ModelParam?.SltMovementLocus == null) return;
                        ModelParam.CustomMoving(ModelParam.SltMovementLocus);
                    }
                    break;

                case "位置比较添加新项":
                    {
                        if (ModelParam?.SltMovementLocus?.PosComparisonParam == null) return;
                        ModelParam.SltMovementLocus.PosComparisonParam.PosCompareDatas.Add(new PosCompareData());
                    }
                    break;

                case "位置比较删除选中项":
                    {
                        if (ModelParam?.SltMovementLocus?.PosComparisonParam?.SltPosCompareData == null) return;
                        ModelParam.SltMovementLocus.PosComparisonParam.PosCompareDatas.Remove(ModelParam.SltMovementLocus.PosComparisonParam.SltPosCompareData);
                        ModelParam.SltMovementLocus.PosComparisonParam.SltPosCompareData = null!;
                    }
                    break;

                case "确认":
                    {
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam! },
                        });
                    }
                    break;
                default:
                    break;
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {

                case "使能":
                    {

                    }
                    break;
                default:
                    break;
            }
        });
        #endregion

    }
}
