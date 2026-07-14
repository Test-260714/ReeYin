using Custom.DefectOverview.Models;
using Custom.DefectOverview.Models.Common;
using Custom.DefectOverview.Services;
using HalconDotNet;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Custom.DefectOverview.ViewModels
{
    public sealed partial class DefectOverviewPublishViewModel : DialogViewModelBase, IViewModuleParam
    {
        public new DefectOverviewPublishModel ModelParam
        {
            get
            {
                if (base.ModelParam is DefectOverviewPublishModel model)
                    return model;

                ModelParam = new DefectOverviewPublishModel();
                return base.ModelParam as DefectOverviewPublishModel;
            }
            set
            {
                base.ModelParam = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SelectedPublishSchemeIndex));
            }
        }

        public Array FrameLayouts { get; } = Enum.GetValues(typeof(DefectOverviewFrameLayout));

        public Array PathRoles { get; } = Enum.GetValues(typeof(DefectOverviewPathRole));

        public Array ResultModes { get; } = Enum.GetValues(typeof(DefectOverviewResultMode));

        public Array WidthSides { get; } = Enum.GetValues(typeof(WidthSide));

        public IReadOnlyList<WidthSideOption> WidthSideOptions { get; } =
        [
            new WidthSideOption(WidthSide.Unknown, "未设置"),
            new WidthSideOption(WidthSide.Left, "L - 左侧"),
            new WidthSideOption(WidthSide.Right, "R - 右侧")
        ];

        public ObservableCollection<string> GroupedDualCameraGroupOptions { get; } = new();

        public ObservableCollection<TransmitParam> ImageCandidates { get; } = new();

        public ObservableCollection<TransmitParam> ResultCandidates { get; } = new();

        public int SelectedPublishSchemeIndex
        {
            get
            {
                if (ModelParam?.UseGroupedDualCameraInputs == true)
                    return 2;

                if (ModelParam?.UseDualPathInputs == true)
                    return 1;

                return 0;
            }
            set
            {
                if (ModelParam == null)
                    return;

                int oldIndex = SelectedPublishSchemeIndex;
                int newIndex = value switch
                {
                    1 => 1,
                    2 => 2,
                    _ => 0
                };

                if (oldIndex == newIndex)
                    return;

                switch (newIndex)
                {
                    case 1:
                        ModelParam.UseDualPathInputs = true;
                        break;
                    case 2:
                        ModelParam.UseGroupedDualCameraInputs = true;
                        ModelParam.EnsureDefaultGroupedDualCameraBindings();
                        break;
                    default:
                        ModelParam.UseGroupedDualCameraInputs = false;
                        ModelParam.UseDualPathInputs = false;
                        break;
                }

                RefreshCandidates();
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ModelParam));
            }
        }

        public override void InitParam()
        {
            InitModelParam<DefectOverviewPublishModel>();
            ModelParam.Guid = Guid;
            ModelParam.Name = Name;
            ModelParam.EnsureDefaultOutputParams(Guid, Name);
            ModelParam.LoadKeyParam();
            RefreshCandidates();
            RaisePropertyChanged(nameof(SelectedPublishSchemeIndex));
        }

        public override void OnDialogClosed()
        {
            if (ModelParam != null)
                ModelParam.IsDebug = false;
        }

        public DelegateCommand<string> ClearSelectedInputCommand => new(name =>
        {
            switch (name)
            {
                case "Image":
                    ModelParam.InputImage = new TransmitParam();
                    break;
                case "Results":
                    ModelParam.InputResults = new TransmitParam();
                    break;
                case "LeftImage":
                    ModelParam.LeftInputImage = new TransmitParam();
                    break;
                case "LeftResults":
                    ModelParam.LeftInputResults = new TransmitParam();
                    break;
                case "RightImage":
                    ModelParam.RightInputImage = new TransmitParam();
                    break;
                case "RightResults":
                    ModelParam.RightInputResults = new TransmitParam();
                    break;
            }

            RefreshCandidates();
        });

        public DelegateCommand RefreshInputParamsCommand => new(() =>
        {
            ModelParam.LoadKeyParam();
            ModelParam.EnsureDefaultOutputParams(Guid, Name);
            ModelParam.EnsureDefaultGroupedDualCameraBindings();
            RefreshCandidates();
        });

        public DelegateCommand ResetGroupedDualCameraBindingsCommand => new(() =>
        {
            ModelParam.GroupedDualCameraBindings.Clear();
            RefreshCandidates();
        });

        public DelegateCommand ExecuteCommand => new(() =>
        {
            _ = ModelParam.ExecuteModule();
        });

        public DelegateCommand ConfirmCommand => new(() =>
        {
            CloseDialog(ButtonResult.OK, new DialogParameters
            {
                { "Param", ModelParam }
            });
        });

        public DelegateCommand CancelCommand => new(() =>
        {
            CloseDialog(ButtonResult.No);
        });

    }

    public sealed class WidthSideOption
    {
        public WidthSideOption(WidthSide value, string displayName)
        {
            Value = value;
            DisplayName = displayName ?? string.Empty;
        }

        public WidthSide Value { get; }

        public string DisplayName { get; }
    }
}

