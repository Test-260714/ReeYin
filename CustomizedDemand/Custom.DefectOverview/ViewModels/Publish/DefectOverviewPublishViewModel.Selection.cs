using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Custom.DefectOverview.Models;
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
	private static void AddIfMissing(ICollection<TransmitParam> target, TransmitParam param)
	{
		if (target != null && param != null && (!string.IsNullOrWhiteSpace(param.Name) || !string.IsNullOrWhiteSpace(param.ParamName)) && !target.Any((TransmitParam item) => IsSameParam(item, param)))
		{
			target.Add(param);
		}
	}

	private static string BuildParamKey(TransmitParam param)
	{
		if (param.Guid != Guid.Empty)
		{
			return param.Guid.ToString();
		}
		string text = BuildSourceKey(param);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		List<string> paramIdentities = GetParamIdentities(param);
		if (param.Serial >= 0 && paramIdentities.Count > 0)
		{
			return $"{param.Serial:D3}:{string.Join("|", paramIdentities)}";
		}
		return $"{param.Serial:D3}:{param.Name}:{param.ParamName}";
	}

	private static bool IsSameParam(TransmitParam left, TransmitParam right)
	{
		if (left == right)
		{
			return true;
		}
		if (left == null || right == null)
		{
			return false;
		}
		if (HasSameGuid(left, right))
		{
			return true;
		}
		if (HasSharedIdentityWithSameSerial(left, right))
		{
			return true;
		}
		return HasSharedParamIdentity(left, right) && (!IsUsableCandidate(left) || !IsUsableCandidate(right));
	}

	private static bool HasSameGuid(TransmitParam left, TransmitParam right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		return left.Guid != Guid.Empty && right.Guid != Guid.Empty && left.Guid == right.Guid;
	}

	private static bool HasSharedIdentityWithSameSerial(TransmitParam left, TransmitParam right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		return left.Serial >= 0 && right.Serial >= 0 && left.Serial == right.Serial && !HasConflictingGuid(left, right) && HasSameSourceKey(left, right) && HasSharedParamIdentity(left, right);
	}

	private static bool HasConflictingGuid(TransmitParam left, TransmitParam right)
	{
		return left?.Guid != Guid.Empty && right?.Guid != Guid.Empty && left.Guid != right.Guid;
	}

	private static bool HasSameSourceKey(TransmitParam left, TransmitParam right)
	{
		string text = BuildSourceKey(left);
		string text2 = BuildSourceKey(right);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return true;
		}
		return string.Equals(text, text2, StringComparison.OrdinalIgnoreCase);
	}

	private static string BuildSourceKey(TransmitParam param)
	{
		if (param == null)
		{
			return string.Empty;
		}
		List<string> list = new List<string>();
		if (param.Serial >= 0)
		{
			list.Add($"S:{param.Serial:D3}");
		}
		AddSourceKeyPart(list, "P", param.ParentNode);
		AddSourceKeyPart(list, "L", (param.LinkGuid == Guid.Empty) ? null : param.LinkGuid.ToString());
		AddSourceKeyPart(list, "R", param.ResourcePath);
		AddSourceKeyPart(list, "N", param.Name);
		AddSourceKeyPart(list, "PN", param.ParamName);
		return (list.Count == 0) ? string.Empty : string.Join("|", list);
	}

	private static void AddSourceKeyPart(ICollection<string> parts, string prefix, string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			parts.Add(prefix + ":" + value);
		}
	}

	private static bool HasSharedParamIdentity(TransmitParam left, TransmitParam right)
	{
		List<string> paramIdentities = GetParamIdentities(left);
		List<string> rightIdentities = GetParamIdentities(right);
		if (paramIdentities.Count == 0 || rightIdentities.Count == 0)
		{
			return false;
		}
		return paramIdentities.Any((string leftIdentity) => rightIdentities.Any((string rightIdentity) => string.Equals(leftIdentity, rightIdentity, StringComparison.OrdinalIgnoreCase)));
	}

	private static List<string> GetParamIdentities(TransmitParam param)
	{
		List<string> list = new List<string>();
		AddParamIdentity(list, param?.Name);
		AddParamIdentity(list, param?.ParamName);
		return list;
	}

	private static void AddParamIdentity(ICollection<string> identities, string value)
	{
		if (!string.IsNullOrWhiteSpace(value) && !identities.Any((string item) => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
		{
			identities.Add(value);
		}
	}

	private static bool IsUsableCandidate(TransmitParam param)
	{
		if (param == null)
		{
			return false;
		}
		return param.Serial >= 0 || param.IsLink || param.Resourece == ResoureceType.Inupt || param.Resourece == ResoureceType.LastInput || param.Value != null;
	}

	private static void ResetCollection(ObservableCollection<TransmitParam> target, IEnumerable<TransmitParam> source)
	{
		target.Clear();
		foreach (TransmitParam item in source ?? Enumerable.Empty<TransmitParam>())
		{
			target.Add(item);
		}
	}

	private static bool IsImageParam(TransmitParam param)
	{
		if (param == null)
		{
			return false;
		}
		if (param.Value is HImage || param.Value is HObject)
		{
			return true;
		}
		if (param.Type == DataType.HObject || param.Type == DataType.Mat)
		{
			return true;
		}
		string text = param.Name + " " + param.ParamName;
		return text.Contains("PathImage", StringComparison.OrdinalIgnoreCase) || text.Contains("Image", StringComparison.OrdinalIgnoreCase) || text.Contains("图像", StringComparison.OrdinalIgnoreCase) || text.Contains("原图", StringComparison.OrdinalIgnoreCase) || text.Contains("路径图", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsResultParam(TransmitParam param)
	{
		if (param == null)
		{
			return false;
		}
		if (param.Value is Result)
		{
			return true;
		}
		if (param.Value is IEnumerable<Result>)
		{
			return true;
		}
		if (param.Value is IEnumerable enumerable && !(param.Value is string))
		{
			foreach (object item in enumerable)
			{
				if (item is Result)
				{
					return true;
				}
			}
		}
		string text = param.Name + " " + param.ParamName;
		return text.Contains("Results", StringComparison.OrdinalIgnoreCase) || text.Contains("DefectResults", StringComparison.OrdinalIgnoreCase) || text.Contains("缺陷结果", StringComparison.OrdinalIgnoreCase) || text.EndsWith("结果", StringComparison.OrdinalIgnoreCase);
	}
    }
}
