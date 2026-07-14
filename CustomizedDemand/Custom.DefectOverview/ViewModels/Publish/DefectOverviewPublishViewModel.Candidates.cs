using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Custom.DefectOverview.Models;
using Custom.DefectOverview.Models.GroupedDualCamera;
using Custom.DefectOverview.Services;
using HalconDotNet;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;

namespace Custom.DefectOverview.ViewModels
{
    public sealed partial class DefectOverviewPublishViewModel : DialogViewModelBase, IViewModuleParam
    {
	private void RefreshCandidates()
	{
		ModelParam.EnsureDefaultGroupedDualCameraBindings();
		List<TransmitParam> list = CollectSelectableParams();
		MergeCurrentSelections(list);
		ResetCollection(ImageCandidates, list.Where(PathInputSelectionHelper.IsImageParam));
		ResetCollection(ResultCandidates, list.Where(PathInputSelectionHelper.IsResultParam));
		ModelParam.InputImage = PathInputSelectionHelper.MatchInputParam(ModelParam.InputImage, ImageCandidates);
		ModelParam.InputResults = PathInputSelectionHelper.MatchInputParam(ModelParam.InputResults, ResultCandidates);
		ModelParam.LeftInputImage = PathInputSelectionHelper.MatchPathInputParam(ModelParam.LeftInputImage, ImageCandidates, DefectOverviewPathRole.Left);
		ModelParam.LeftInputResults = PathInputSelectionHelper.MatchPathInputParam(ModelParam.LeftInputResults, ResultCandidates, DefectOverviewPathRole.Left);
		ModelParam.RightInputImage = PathInputSelectionHelper.MatchPathInputParam(ModelParam.RightInputImage, ImageCandidates, DefectOverviewPathRole.Right);
		ModelParam.RightInputResults = PathInputSelectionHelper.MatchPathInputParam(ModelParam.RightInputResults, ResultCandidates, DefectOverviewPathRole.Right);
		RefreshGroupedDualCameraSourceBindings();
		RefreshGroupedDualCameraBindingCandidates();
	}

	private void RefreshGroupedDualCameraBindingCandidates()
	{
		foreach (GroupedDualCameraBinding binding in ModelParam.GroupedDualCameraBindings ?? Enumerable.Empty<GroupedDualCameraBinding>())
		{
			TransmitParam resultInput = PathInputSelectionHelper.HasConfiguredInputSelection(binding.ResultInput)
				? binding.ResultInput
				: ModelParam.GetDefaultGroupedDualCameraResultInput(binding);
			binding.ResultInput = PathInputSelectionHelper.MatchInputParam(resultInput, ResultCandidates);
			binding.ImageInput = PathInputSelectionHelper.MatchInputParam(binding.ImageInput, ImageCandidates);
		}
	}

	private void MergeCurrentSelections(ICollection<TransmitParam> target)
	{
		AddIfMissing(target, ModelParam.InputImage);
		AddIfMissing(target, ModelParam.InputResults);
		AddIfMissing(target, ModelParam.LeftInputImage);
		AddIfMissing(target, ModelParam.LeftInputResults);
		AddIfMissing(target, ModelParam.RightInputImage);
		AddIfMissing(target, ModelParam.RightInputResults);
		foreach (GroupedDualCameraBinding binding in ModelParam.GroupedDualCameraBindings ?? Enumerable.Empty<GroupedDualCameraBinding>())
		{
			AddIfMissing(target, binding.ImageInput);
			AddIfMissing(target, binding.ResultInput);
		}
	}

	private static TransmitParam MatchInputParam(TransmitParam current, IEnumerable<TransmitParam> candidates)
	{
		List<TransmitParam> list = candidates?.Where((TransmitParam item) => item != null).ToList() ?? new List<TransmitParam>();
		if (list.Count == 0)
		{
			return current ?? new TransmitParam();
		}
		if (current == null)
		{
			return new TransmitParam();
		}
		TransmitParam transmitParam = list.FirstOrDefault((TransmitParam item) => HasSameGuid(item, current));
		if (transmitParam == null)
		{
			transmitParam = list.FirstOrDefault((TransmitParam item) => HasSharedIdentityWithSameSerial(item, current));
		}
		if (transmitParam == null)
		{
			transmitParam = list.FirstOrDefault((TransmitParam item) => item != current && IsUsableCandidate(item) && HasSharedParamIdentity(item, current));
		}
		if (transmitParam == null)
		{
			transmitParam = list.FirstOrDefault((TransmitParam item) => IsUsableCandidate(item) && HasSharedParamIdentity(item, current));
		}
		if (transmitParam != null)
		{
			return transmitParam;
		}
		return (string.IsNullOrWhiteSpace(current.Name) && string.IsNullOrWhiteSpace(current.ParamName)) ? new TransmitParam() : current;
	}

	private static TransmitParam MatchPathInputParam(TransmitParam current, IEnumerable<TransmitParam> candidates, DefectOverviewPathRole pathRole)
	{
		List<TransmitParam> list = candidates?.Where((TransmitParam item) => item != null).ToList() ?? new List<TransmitParam>();
		TransmitParam transmitParam = MatchInputParam(current, list);
		if (pathRole == DefectOverviewPathRole.Unknown || list.Count == 0)
		{
			return transmitParam;
		}
		TransmitParam transmitParam2 = list.FirstOrDefault((TransmitParam item) => IsPathCandidate(item, pathRole));
		if (transmitParam2 == null)
		{
			return transmitParam;
		}
		if (!HasConfiguredInputSelection(current))
		{
			return transmitParam2;
		}
		if (IsOppositePathCandidate(transmitParam, pathRole))
		{
			return transmitParam2;
		}
		return transmitParam;
	}

	private static bool IsPathCandidate(TransmitParam param, DefectOverviewPathRole pathRole)
	{
		string text = BuildPathText(param);
		if (1 == 0)
		{
		}
		bool flag = pathRole switch
		{
			DefectOverviewPathRole.Left => IsLeftPathText(text), 
			DefectOverviewPathRole.Right => IsRightPathText(text), 
			_ => false, 
		};
		if (1 == 0)
		{
		}
		if (flag)
		{
			return true;
		}
		return false;
	}

	private static bool IsOppositePathCandidate(TransmitParam param, DefectOverviewPathRole pathRole)
	{
		string text = BuildPathText(param);
		if (1 == 0)
		{
		}
		bool flag = pathRole switch
		{
			DefectOverviewPathRole.Left => IsRightPathText(text), 
			DefectOverviewPathRole.Right => IsLeftPathText(text), 
			_ => false, 
		};
		if (1 == 0)
		{
		}
		if (flag)
		{
			return true;
		}
		return false;
	}

	private static bool HasConfiguredInputSelection(TransmitParam param)
	{
		if (param == null)
		{
			return false;
		}
		return param.IsLink || !string.IsNullOrWhiteSpace(param.Name) || !string.IsNullOrWhiteSpace(param.ParamName) || param.Value != null;
	}

	private static string BuildPathText(TransmitParam param)
	{
		if (param == null)
		{
			return string.Empty;
		}
		return $"{param.Name} {param.ParamName} {param.ParentNode} {param.ResourcePath}";
	}

	private static bool ShouldReplaceCandidate(TransmitParam existing, TransmitParam incoming)
	{
		if (existing == null || incoming == null)
		{
			return incoming != null;
		}
		if (string.IsNullOrWhiteSpace(existing.ParentNode) && !string.IsNullOrWhiteSpace(incoming.ParentNode))
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(existing.ResourcePath) && !string.IsNullOrWhiteSpace(incoming.ResourcePath))
		{
			return true;
		}
		return existing.Value == null && incoming.Value != null;
	}

	private static bool IsLeftPathText(string text)
	{
		return !string.IsNullOrWhiteSpace(text) && (text.Contains("左", StringComparison.OrdinalIgnoreCase) || text.Contains("left", StringComparison.OrdinalIgnoreCase) || text.Contains("path1", StringComparison.OrdinalIgnoreCase) || text.Contains("lane1", StringComparison.OrdinalIgnoreCase));
	}

	private static bool IsRightPathText(string text)
	{
		return !string.IsNullOrWhiteSpace(text) && (text.Contains("右", StringComparison.OrdinalIgnoreCase) || text.Contains("right", StringComparison.OrdinalIgnoreCase) || text.Contains("path2", StringComparison.OrdinalIgnoreCase) || text.Contains("lane2", StringComparison.OrdinalIgnoreCase));
	}

    }
}
