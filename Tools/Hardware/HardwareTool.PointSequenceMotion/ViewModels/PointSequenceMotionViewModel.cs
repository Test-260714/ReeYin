using HardwareTool.PointSequenceMotion.Models;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.ControlCard;
using System;
using System.Collections.Generic;
using System.Windows;

namespace HardwareTool.PointSequenceMotion.ViewModels
{
    public class PointSequenceMotionViewModel : DialogViewModelBase, IViewModuleParam
    {
        private int _pointCount = 5;
        public int PointCount
        {
            get => _pointCount;
            set { _pointCount = Math.Max(1, value); RaisePropertyChanged(); }
        }

        public IEnumerable<PointSequenceSelectMode> SelectModes { get; } =
            Enum.GetValues<PointSequenceSelectMode>();

        public IEnumerable<EN_SpeedType> SpeedTypes { get; } =
            Enum.GetValues<EN_SpeedType>();

        public new PointSequenceMotionModel ModelParam
        {
            get => (PointSequenceMotionModel)base.ModelParam;
            set => base.ModelParam = value;
        }

        public override void InitParam()
        {
            ModelParam = InitModelParam<PointSequenceMotionModel>();
            ModelParam.RefreshControlCardContext();
            PointCount = Math.Max(1, ModelParam.Points.Count);
        }

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            ModelParam.RefreshControlCardContext();
            if (!string.IsNullOrWhiteSpace(ModelParam.SltModelName) && ModelParam.ControlCard == null)
            {
                foreach (var card in ModelParam.Models)
                {
                    if (card.NickName == ModelParam.SltModelName)
                    {
                        ModelParam.ControlCard = card;
                        break;
                    }
                }
            }

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam },
                });
            }
        });

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>(order =>
        {
            switch (order)
            {
                case "切换控制卡":
                    if (ModelParam.ControlCard != null)
                    {
                        ModelParam.SltModelName = ModelParam.ControlCard.NickName;
                    }
                    break;

                case "生成点位":
                    ModelParam.SetPointCount(PointCount);
                    PointCount = ModelParam.Points.Count;
                    break;

                case "五点":
                    PointCount = 5;
                    ModelParam.SetPointCount(5);
                    break;

                case "九点":
                    PointCount = 9;
                    ModelParam.SetPointCount(9);
                    break;

                case "添加点位":
                    ModelParam.AddPoint();
                    PointCount = ModelParam.Points.Count;
                    break;

                case "删除点位":
                    ModelParam.RemoveSelectedPoint();
                    PointCount = Math.Max(1, ModelParam.Points.Count);
                    break;

                case "重置循环":
                    ModelParam.ResetCycle();
                    break;

                case "执行选中点":
                    ModelParam.MoveSelectedPoint(out _);
                    break;

                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;

                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters
                    {
                        { "Param", ModelParam },
                    });
                    break;
            }
        });
    }
}
