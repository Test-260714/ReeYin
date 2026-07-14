using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Custom.DefectOverview.Services;
using HalconDotNet;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;

namespace Custom.DefectOverview.Models
{
    public sealed partial class DefectOverviewPublishModel : ModelParamBase
    {
	private string ResolvePathName()
	{
		if (!string.IsNullOrWhiteSpace(PathName))
		{
			return PathName;
		}
		if (FrameLayout != DefectOverviewFrameLayout.DualPath)
		{
			return string.Empty;
		}
		DefectOverviewPathRole pathRole = PathRole;
		if (1 == 0)
		{
		}
		string result = pathRole switch
		{
			DefectOverviewPathRole.Left => "左路", 
			DefectOverviewPathRole.Right => "右路", 
			_ => "通道", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private string ResolveDualPathName(DefectOverviewPathRole pathRole)
	{
		if (1 == 0)
		{
		}
		string result = pathRole switch
		{
			DefectOverviewPathRole.Left => string.IsNullOrWhiteSpace(LeftPathName) ? "左路" : LeftPathName, 
			DefectOverviewPathRole.Right => string.IsNullOrWhiteSpace(RightPathName) ? "右路" : RightPathName, 
			_ => "通道", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private object ResolveSelectedInputValue(TransmitParam selectedParam, bool isDeepClone = true)
	{
		string source;
		return ResolveSelectedInputValue(selectedParam, isDeepClone, out source);
	}

	private object ResolveSelectedInputValue(TransmitParam selectedParam, bool isDeepClone, out string source)
	{
		source = null;
		if (selectedParam == null)
		{
			return null;
		}
		object obj = ResolveSelectedRuntimeValue(selectedParam, isDeepClone, out source);
		if (obj != null)
		{
			return obj;
		}
		if (selectedParam.Value is HImage hImage && hImage.IsInitialized())
		{
			source = "selected-local";
			return isDeepClone ? hImage.CopyImage() : hImage;
		}
		if (selectedParam.Value is HObject hObject && hObject.IsInitialized())
		{
			source = "selected-local";
			return isDeepClone ? hObject.Clone() : hObject;
		}
		source = ((selectedParam.Value == null) ? null : "selected-local");
		return selectedParam.Value;
	}

	private object ResolveSelectedRuntimeValue(TransmitParam selectedParam, bool isDeepClone, out string source)
	{
		source = null;
		if (selectedParam == null)
		{
			return null;
		}
		object obj = FindValueInInputParams(selectedParam, isDeepClone);
		if (obj != null)
		{
			source = "current-input";
			return obj;
		}
		object obj2 = FindValueFromAncestorGraphOutputs(selectedParam, isDeepClone, out source);
		if (obj2 != null)
		{
			return obj2;
		}
		object obj3 = FindValueInSourceCaches(selectedParam, isDeepClone, out source);
		if (obj3 != null)
		{
			return obj3;
		}
		return null;
	}

	private (HImage Image, string Source) ResolveSelectedImageInput(TransmitParam selectedParam)
	{
		return ResolveSelectedImageInput(selectedParam, InputResults);
	}

	private (HImage Image, string Source) ResolveSelectedImageInput(TransmitParam selectedParam, TransmitParam resultsParam)
	{
		bool flag = HasConfiguredInputSelection(selectedParam);
		string text = DescribeTransmitParam(selectedParam);
		if (HasLinkedSourceBinding(resultsParam) && resultsParam.Serial >= 0 && selectedParam != null && selectedParam.Serial >= 0 && selectedParam.Serial != resultsParam.Serial)
		{
			HImage hImage = FindImageFromSourceNode(resultsParam);
			if (hImage != null)
			{
				return (Image: hImage, Source: "results-source-priority:" + DescribeTransmitParam(resultsParam));
			}
			Custom.DefectOverview.DefectOverviewConsole.WriteLine("[DefectOverviewPublish] ResultsSourceImage miss source=" + DescribeTransmitParam(resultsParam) + ", selected=" + text);
		}
		if (flag)
		{
			object value = GetTransmitParam(base.InputParams, selectedParam, IsDC: false) ?? FindValueInInputParams(selectedParam, isDeepClone: false);
			HImage hImage2 = ConvertToImage(value);
			if (hImage2 != null)
			{
				return (Image: hImage2, Source: "selected-current:" + text);
			}
			if (!selectedParam.IsLink)
			{
				HImage hImage3 = ConvertToImage(selectedParam.Value);
				if (hImage3 != null)
				{
					return (Image: hImage3, Source: "selected-local:" + text);
				}
			}
			string source;
			object value2 = ResolveSelectedRuntimeValue(selectedParam, isDeepClone: false, out source);
			HImage hImage4 = ConvertToImage(value2);
			if (hImage4 != null)
			{
				return (Image: hImage4, Source: "selected-runtime:" + (source ?? text));
			}
			(HImage, string) result = FindImageFromNamedSelection(selectedParam);
			if (result.Item1 != null)
			{
				return result;
			}
			if (HasLinkedSourceBinding(selectedParam))
			{
				HImage hImage5 = FindImageFromSourceNode(selectedParam);
				if (hImage5 != null)
				{
					return (Image: hImage5, Source: "selected-source-node:" + text);
				}
			}
		}
		if (HasLinkedSourceBinding(resultsParam))
		{
			HImage hImage6 = FindImageFromSourceNode(resultsParam);
			if (hImage6 != null)
			{
				return (Image: hImage6, Source: "results-source-node:" + DescribeTransmitParam(resultsParam));
			}
		}
		string source2;
		HImage hImage7 = FindCurrentContextImage(out source2);
		if (hImage7 != null)
		{
			return (Image: hImage7, Source: source2);
		}
		return (Image: null, Source: flag ? ("missing-current:" + text) : "none");
	}

	private (HImage Image, string Source) FindImageFromNamedSelection(TransmitParam selectedParam)
	{
		List<string> exactParamNames = GetExactParamNames(selectedParam);
		if (exactParamNames.Count == 0)
		{
			return (Image: null, Source: null);
		}
		if (selectedParam != null && selectedParam.Serial >= 0)
		{
			HImage hImage = FindImageFromSourceNode(selectedParam.Serial, exactParamNames);
			if (hImage != null)
			{
				return (Image: hImage, Source: "selected-source-name:" + DescribeTransmitParam(selectedParam));
			}
		}
		HImage hImage2 = FindImageFromAncestorGraphOutputs(selectedParam?.Serial ?? (-1), exactParamNames);
		if (hImage2 != null)
		{
			return (Image: hImage2, Source: "selected-ancestor-name:" + DescribeTransmitParam(selectedParam));
		}
		return (Image: null, Source: null);
	}

	private object FindValueInInputParams(TransmitParam selectedParam, bool isDeepClone)
	{
		if (selectedParam == null || base.InputParams == null || base.InputParams.Count == 0)
		{
			return null;
		}
		TransmitParam matched;
		return TryFindSelectedValue(base.InputParams, selectedParam, isDeepClone, selectedParam.Serial >= 0, out matched);
	}

	private object FindValueInSourceCaches(TransmitParam selectedParam, bool isDeepClone)
	{
		string source;
		return FindValueInSourceCaches(selectedParam, isDeepClone, out source);
	}

	private object FindValueInSourceCaches(TransmitParam selectedParam, bool isDeepClone, out string source)
	{
		source = null;
		try
		{
			if (selectedParam == null || selectedParam.Serial < 0)
			{
				return null;
			}
			NodifySolutionItem nodifySolutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
			if (nodifySolutionItem == null)
			{
				return null;
			}
			foreach (string item in EnumerateSourceCacheKeys(selectedParam.Serial))
			{
				TransmitParam matched;
				if (nodifySolutionItem.NodesOutputCache != null && nodifySolutionItem.NodesOutputCache.TryGetValue(item, out var value))
				{
					object obj = TryFindSelectedValue(value, selectedParam, isDeepClone, requireSerialMatch: false, out matched);
					if (obj != null)
					{
						source = "NodesOutputCache[" + item + "]";
						return obj;
					}
				}
				if (nodifySolutionItem.NodeParamCaches != null && nodifySolutionItem.NodeParamCaches.TryGetValue(item, out var value2) && value2 is ModelParamBase modelParamBase)
				{
					object obj2 = TryFindSelectedValue(modelParamBase.OutputParams, selectedParam, isDeepClone, requireSerialMatch: false, out matched);
					if (obj2 != null)
					{
						source = "NodeParamCaches[" + item + "].Output";
						return obj2;
					}
					obj2 = TryFindSelectedValue(modelParamBase.moduleOutputParam, selectedParam, isDeepClone, requireSerialMatch: false, out matched);
					if (obj2 != null)
					{
						source = "NodeParamCaches[" + item + "].ModuleOutput";
						return obj2;
					}
					obj2 = TryFindSelectedValue(modelParamBase.InputParams, selectedParam, isDeepClone, requireSerialMatch: false, out matched);
					if (obj2 != null)
					{
						source = "NodeParamCaches[" + item + "].Input";
						return obj2;
					}
					obj2 = TryFindSelectedValue(modelParamBase.moduleInputParam, selectedParam, isDeepClone, requireSerialMatch: false, out matched);
					if (obj2 != null)
					{
						source = "NodeParamCaches[" + item + "].ModuleInput";
						return obj2;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private object FindValueFromAncestorGraphOutputs(TransmitParam selectedParam, bool isDeepClone, out string source)
	{
		source = null;
		if (selectedParam == null)
		{
			return null;
		}
		NodifySolutionItem nodifySolutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
		if (!(nodifySolutionItem?.NodeCaches is IEnumerable source2) || nodifySolutionItem.NodeCaches is string)
		{
			return null;
		}
		List<object> list = (from object item in source2
			where item != null
			select item).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		object obj = list.FirstOrDefault((object node) => GetNodeSerial(node) == base.Serial);
		if (obj == null)
		{
			return null;
		}
		foreach (object item in EnumerateAncestorNodes(obj))
		{
			int nodeSerial = GetNodeSerial(item);
			if (selectedParam.Serial < 0 || nodeSerial < 0 || nodeSerial == selectedParam.Serial)
			{
				TransmitParam matched;
				object obj2 = TryFindSelectedValue(GetNodeOutputParams(item), selectedParam, isDeepClone, selectedParam.Serial >= 0, out matched);
				if (obj2 != null)
				{
					source = $"ancestor-live[{nodeSerial:D3}]";
					return obj2;
				}
			}
		}
		return null;
	}

	private static object TryFindSelectedValue(IEnumerable<TransmitParam> transmitParams, TransmitParam selectedParam, bool isDeepClone)
	{
		TransmitParam matched;
		return TryFindSelectedValue(transmitParams, selectedParam, isDeepClone, requireSerialMatch: false, out matched);
	}

	private static object TryFindSelectedValue(IEnumerable<TransmitParam> transmitParams, TransmitParam selectedParam, bool isDeepClone, bool requireSerialMatch, out TransmitParam matched)
	{
		matched = null;
		if (transmitParams == null || selectedParam == null)
		{
			return null;
		}
		List<TransmitParam> list = transmitParams.Where((TransmitParam item) => item != null).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		matched = list.FirstOrDefault((TransmitParam item) => item != null && selectedParam.Guid != Guid.Empty && item.Guid == selectedParam.Guid);
		List<string> exactNames = GetExactParamNames(selectedParam);
		if (matched == null && exactNames.Count > 0 && selectedParam.Serial >= 0)
		{
			matched = list.FirstOrDefault((TransmitParam item) => item.Serial == selectedParam.Serial && IsExactParamName(item, exactNames));
		}
		if (matched == null && exactNames.Count > 0 && !requireSerialMatch)
		{
			matched = list.FirstOrDefault((TransmitParam item) => IsExactParamName(item, exactNames));
		}
		return (matched == null) ? null : CloneSelectedValue(matched.Value, isDeepClone);
	}

	private static object TryFindSelectedValue(ModuleParam moduleParam, TransmitParam selectedParam, bool isDeepClone)
	{
		TransmitParam matched;
		return TryFindSelectedValue(moduleParam, selectedParam, isDeepClone, requireSerialMatch: false, out matched);
	}

	private static object TryFindSelectedValue(ModuleParam moduleParam, TransmitParam selectedParam, bool isDeepClone, bool requireSerialMatch, out TransmitParam matched)
	{
		matched = null;
		if (moduleParam?.TransmitParams == null || moduleParam.TransmitParams.Count == 0)
		{
			return null;
		}
		return TryFindSelectedValue(moduleParam.TransmitParams.Values.OfType<TransmitParam>(), selectedParam, isDeepClone, requireSerialMatch, out matched);
	}

	private static object CloneSelectedValue(object value, bool isDeepClone)
	{
		if (value is TransmitParam transmitParam)
		{
			value = transmitParam.Value;
		}
		if (value == null)
		{
			return null;
		}
		if (value is HImage hImage && hImage.IsInitialized())
		{
			return isDeepClone ? hImage.CopyImage() : hImage;
		}
		if (value is HObject hObject && hObject.IsInitialized())
		{
			return isDeepClone ? hObject.Clone() : hObject;
		}
		if (!isDeepClone)
		{
			return value;
		}
		try
		{
			return value.DeepClone();
		}
		catch
		{
			return value;
		}
	}

	private static HImage ConvertToImage(object value)
	{
		if (value != null)
		{
			if (!(value is TransmitParam transmitParam))
			{
				HObject hObject;
				if (!(value is HImage hImage))
				{
					hObject = value as HObject;
					if (hObject == null)
					{
						goto IL_00bc;
					}
				}
				else
				{
					if (hImage.IsInitialized())
					{
						try
						{
							return hImage.CopyImage();
						}
						catch
						{
							return null;
						}
					}
					hObject = (HObject)value;
				}
				if (!hObject.IsInitialized())
				{
					goto IL_00bc;
				}
				try
				{
					HImage hImage2 = new HImage(hObject);
					if (!hImage2.IsInitialized())
					{
						hImage2.Dispose();
						return null;
					}
					HImage result = hImage2.CopyImage();
					hImage2.Dispose();
					return result;
				}
				catch
				{
					return null;
				}
			}
			return ConvertToImage(transmitParam.Value);
		}
		return null;
		IL_00bc:
		return TryConvertImageFromEnumerable(value as IEnumerable);
	}

	private static HImage TryConvertImageFromEnumerable(IEnumerable values)
	{
		if (values == null || values is string)
		{
			return null;
		}
		foreach (object value in values)
		{
			HImage hImage = ConvertToImageSafe(value);
			if (hImage != null)
			{
				return hImage;
			}
		}
		return null;
	}

	private HImage FindCurrentContextImage(out string source)
	{
		IEnumerable<TransmitParam> inputParams = base.InputParams;
		foreach (TransmitParam item in inputParams ?? Enumerable.Empty<TransmitParam>())
		{
			HImage hImage = ConvertTransmitParamToImage(item);
			if (hImage != null)
			{
				source = "current-input-params";
				return hImage;
			}
		}
		IEnumerable<object> enumerable = moduleInputParam?.TransmitParams?.Values;
		foreach (object item2 in enumerable ?? Enumerable.Empty<object>())
		{
			if (item2 is TransmitParam param)
			{
				HImage hImage2 = ConvertTransmitParamToImage(param);
				if (hImage2 != null)
				{
					source = "current-module-input";
					return hImage2;
				}
			}
		}
		source = "none";
		return null;
	}

	private static HImage ConvertTransmitParamToImage(TransmitParam param)
	{
		if (param == null)
		{
			return null;
		}
		return ConvertToImage(param.Value);
	}

	private static bool HasConfiguredInputSelection(TransmitParam param)
	{
		if (param == null)
		{
			return false;
		}
		return param.IsLink || !string.IsNullOrWhiteSpace(param.Name) || !string.IsNullOrWhiteSpace(param.ParamName) || param.Value != null;
	}

	private static bool HasLinkedSourceBinding(TransmitParam param)
	{
		if (param == null)
		{
			return false;
		}
		if ((string.IsNullOrWhiteSpace(param.Name) && string.IsNullOrWhiteSpace(param.ParamName)) || param.Serial < 0)
		{
			return false;
		}
		return param.IsLink || param.Resourece == ResoureceType.Inupt || param.Resourece == ResoureceType.LastInput;
	}

	private static string DescribeTransmitParam(TransmitParam param)
	{
		if (param == null)
		{
			return "null";
		}
		string transmitParamDisplayName = GetTransmitParamDisplayName(param);
		return $"{transmitParamDisplayName}@{param.Serial}[{param.Resourece};link={(param.IsLink ? 1 : 0)}]";
	}

	private static string GetTransmitParamDisplayName(TransmitParam param)
	{
		if (param == null)
		{
			return "null";
		}
		string text = (string.IsNullOrWhiteSpace(param.Name) ? string.Empty : param.Name);
		string text2 = (string.IsNullOrWhiteSpace(param.ParamName) ? string.Empty : param.ParamName);
		if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2))
		{
			return "unnamed";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		if (string.IsNullOrWhiteSpace(text2) || string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		return text + "/" + text2;
	}

	private HImage FindImageFromSourceNode(TransmitParam sourceParam)
	{
		return FindImageFromSourceNode(sourceParam?.Serial ?? (-1), null);
	}

	private HImage FindImageFromSourceNode(int sourceSerial, IReadOnlyCollection<string> exactNames)
	{
		try
		{
			if (sourceSerial < 0)
			{
				return null;
			}
			NodifySolutionItem nodifySolutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
			if (nodifySolutionItem == null)
			{
				return null;
			}
			foreach (string item in EnumerateSourceCacheKeys(sourceSerial))
			{
				if (nodifySolutionItem.NodesOutputCache != null && nodifySolutionItem.NodesOutputCache.TryGetValue(item, out var value))
				{
					HImage hImage = FindImageInTransmitParams(value, exactNames);
					if (hImage != null)
					{
						return hImage;
					}
				}
				if (nodifySolutionItem.NodeParamCaches == null || !nodifySolutionItem.NodeParamCaches.TryGetValue(item, out var value2) || value2 == null)
				{
					continue;
				}
				if (value2 is ModelParamBase modelParamBase)
				{
					HImage hImage2 = FindImageInTransmitParams(modelParamBase.OutputParams, exactNames);
					if (hImage2 != null)
					{
						return hImage2;
					}
					HImage hImage3 = FindImageInModuleParams(modelParamBase.moduleOutputParam, exactNames);
					if (hImage3 != null)
					{
						return hImage3;
					}
					HImage hImage4 = FindImageInTransmitParams(modelParamBase.InputParams, exactNames);
					if (hImage4 != null)
					{
						return hImage4;
					}
					HImage hImage5 = FindImageInModuleParams(modelParamBase.moduleInputParam, exactNames);
					if (hImage5 != null)
					{
						return hImage5;
					}
				}
				HImage hImage6 = FindImageInModelObject(value2, exactNames);
				if (hImage6 != null)
				{
					return hImage6;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private HImage FindImageFromAncestorGraphOutputs(IReadOnlyCollection<string> exactNames)
	{
		return FindImageFromAncestorGraphOutputs(-1, exactNames);
	}

	private HImage FindImageFromAncestorGraphOutputs(int sourceSerial, IReadOnlyCollection<string> exactNames)
	{
		if (exactNames == null || exactNames.Count == 0)
		{
			return null;
		}
		NodifySolutionItem nodifySolutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
		if (!(nodifySolutionItem?.NodeCaches is IEnumerable source) || nodifySolutionItem.NodeCaches is string)
		{
			return null;
		}
		List<object> list = (from object item in source
			where item != null
			select item).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		object obj = list.FirstOrDefault((object node) => GetNodeSerial(node) == base.Serial);
		if (obj == null)
		{
			return null;
		}
		foreach (object item in EnumerateAncestorNodes(obj))
		{
			int nodeSerial = GetNodeSerial(item);
			if (sourceSerial >= 0 && nodeSerial >= 0 && nodeSerial != sourceSerial)
			{
				continue;
			}
			HImage hImage = FindImageInTransmitParams(GetNodeOutputParams(item), exactNames);
			if (hImage != null)
			{
				return hImage;
			}
			if (nodeSerial >= 0)
			{
				HImage hImage2 = FindImageFromSourceNode(nodeSerial, exactNames);
				if (hImage2 != null)
				{
					return hImage2;
				}
			}
		}
		return null;
	}

	private static IEnumerable<object> EnumerateAncestorNodes(object currentNode)
	{
		Stack<object> stack = new Stack<object>();
		HashSet<int> visitedSerials = new HashSet<int>();
		foreach (object parentNode in GetNodeCollectionProperty(currentNode, "LastNodes"))
		{
			stack.Push(parentNode);
		}
		while (stack.Count > 0)
		{
			object node = stack.Pop();
			if (node == null)
			{
				continue;
			}
			int nodeSerial = GetNodeSerial(node);
			if (nodeSerial >= 0 && !visitedSerials.Add(nodeSerial))
			{
				continue;
			}
			yield return node;
			foreach (object parentNode2 in GetNodeCollectionProperty(node, "LastNodes"))
			{
				stack.Push(parentNode2);
			}
		}
	}

	private static IEnumerable<object> GetNodeCollectionProperty(object source, string propertyName)
	{
		object value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
		IEnumerable enumerable = value as IEnumerable;
		if (enumerable == null || value is string)
		{
			yield break;
		}
		foreach (object item in enumerable)
		{
			if (item != null)
			{
				yield return item;
			}
		}
	}

	private static IEnumerable<TransmitParam> GetNodeOutputParams(object node)
	{
		object moduleParamObject = node?.GetType().GetProperty("ModuleParam")?.GetValue(node);
		if (moduleParamObject is ModelParamBase model)
		{
			IEnumerable<TransmitParam> outputParams = model.OutputParams;
			foreach (TransmitParam param in outputParams ?? Enumerable.Empty<TransmitParam>())
			{
				if (param != null)
				{
					yield return param;
				}
			}
			foreach (TransmitParam param2 in EnumerateModuleTransmitParams(model.moduleOutputParam))
			{
				if (param2 != null)
				{
					yield return param2;
				}
			}
			yield break;
		}
		object outputParamsObject = moduleParamObject?.GetType().GetProperty("OutputParams")?.GetValue(moduleParamObject);
		IEnumerable enumerable = outputParamsObject as IEnumerable;
		if (enumerable == null || outputParamsObject is string)
		{
			yield break;
		}
		foreach (object item in enumerable)
		{
			if (item is TransmitParam param3)
			{
				yield return param3;
			}
		}
	}

	private static IEnumerable<TransmitParam> EnumerateModuleTransmitParams(ModuleParam moduleParam)
	{
		if (moduleParam?.TransmitParams == null)
		{
			yield break;
		}
		foreach (object value in moduleParam.TransmitParams.Values)
		{
			if (value is TransmitParam param)
			{
				yield return param;
			}
		}
	}

	private static int GetNodeSerial(object node)
	{
		object obj = node?.GetType().GetProperty("ModuleParam")?.GetValue(node);
		if (obj?.GetType().GetProperty("Serial")?.GetValue(obj) is int result)
		{
			return result;
		}
		object obj2 = node?.GetType().GetProperty("MenuInfo")?.GetValue(node);
		return (obj2?.GetType().GetProperty("Serial")?.GetValue(obj2) is int num) ? num : (-1);
	}

	private List<Result> FindResultsFromSourceNode(TransmitParam sourceParam)
	{
		try
		{
			if (sourceParam == null || sourceParam.Serial < 0)
			{
				return new List<Result>();
			}
			NodifySolutionItem nodifySolutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
			if (nodifySolutionItem == null)
			{
				return new List<Result>();
			}
			foreach (string item in EnumerateSourceCacheKeys(sourceParam.Serial))
			{
				if (nodifySolutionItem.NodeParamCaches != null && nodifySolutionItem.NodeParamCaches.TryGetValue(item, out var value) && value != null)
				{
					if (value is ModelParamBase modelParamBase)
					{
						List<Result> list = FindResultsInTransmitParams(modelParamBase.OutputParams);
						if (list.Count > 0)
						{
							return list;
						}
						List<Result> list2 = FindResultsInModuleParams(modelParamBase.moduleOutputParam);
						if (list2.Count > 0)
						{
							return list2;
						}
						List<Result> list3 = FindResultsInModelObject(modelParamBase);
						if (list3.Count > 0)
						{
							return list3;
						}
						List<Result> list4 = FindResultsInTransmitParams(modelParamBase.InputParams);
						if (list4.Count > 0)
						{
							return list4;
						}
						List<Result> list5 = FindResultsInModuleParams(modelParamBase.moduleInputParam);
						if (list5.Count > 0)
						{
							return list5;
						}
					}
					List<Result> list6 = FindResultsInModelObject(value);
					if (list6.Count > 0)
					{
						return list6;
					}
				}
				if (nodifySolutionItem.NodesOutputCache != null && nodifySolutionItem.NodesOutputCache.TryGetValue(item, out var value2))
				{
					List<Result> list7 = FindResultsInTransmitParams(value2);
					if (list7.Count > 0)
					{
						return list7;
					}
				}
			}
		}
		catch
		{
		}
		return new List<Result>();
	}

	private static HImage FindImageInModuleParams(ModuleParam moduleParam, IReadOnlyCollection<string> exactNames = null)
	{
		if (moduleParam?.TransmitParams == null || moduleParam.TransmitParams.Count == 0)
		{
			return null;
		}
		foreach (object value in moduleParam.TransmitParams.Values)
		{
			if (value is TransmitParam param && (exactNames == null || exactNames.Count <= 0 || IsExactParamName(param, exactNames)))
			{
				HImage hImage = ConvertTransmitParamToImage(param);
				if (hImage != null)
				{
					return hImage;
				}
			}
		}
		return null;
	}

	private static List<Result> FindResultsInModuleParams(ModuleParam moduleParam)
	{
		if (moduleParam?.TransmitParams == null || moduleParam.TransmitParams.Count == 0)
		{
			return new List<Result>();
		}
		foreach (object value in moduleParam.TransmitParams.Values)
		{
			if (value is TransmitParam transmitParam)
			{
				List<Result> list = ExtractResults(transmitParam.Value);
				if (list.Count > 0)
				{
					return list;
				}
			}
		}
		return new List<Result>();
	}

	private HImage FindImageFromNodeOutputCache()
	{
		try
		{
			Dictionary<string, ObservableCollection<TransmitParam>> dictionary = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodesOutputCache;
			if (dictionary == null || dictionary.Count == 0)
			{
				return null;
			}
			string currentSerial = base.Serial.ToString(CultureInfo.InvariantCulture);
			IEnumerable<KeyValuePair<string, ObservableCollection<TransmitParam>>> enumerable = from item in dictionary
				where item.Value != null && item.Value.Count > 0 && !string.Equals(item.Key, currentSerial, StringComparison.OrdinalIgnoreCase)
				orderby ResolveCachePriority(item.Key) descending
				select item;
			foreach (KeyValuePair<string, ObservableCollection<TransmitParam>> item in enumerable)
			{
				HImage hImage = FindImageInTransmitParams(item.Value);
				if (hImage != null)
				{
					return hImage;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private HImage FindImageFromNodeParamCache()
	{
		try
		{
			Dictionary<string, object> dictionary = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches;
			if (dictionary == null || dictionary.Count == 0)
			{
				return null;
			}
			string currentSerial = base.Serial.ToString(CultureInfo.InvariantCulture);
			IEnumerable<KeyValuePair<string, object>> enumerable = from item in dictionary
				where item.Value != null && !string.Equals(item.Key, currentSerial, StringComparison.OrdinalIgnoreCase)
				orderby ResolveCachePriority(item.Key) descending
				select item;
			foreach (KeyValuePair<string, object> item in enumerable)
			{
				if (item.Value is ModelParamBase modelParamBase)
				{
					HImage hImage = FindImageInTransmitParams(modelParamBase.OutputParams);
					if (hImage != null)
					{
						return hImage;
					}
				}
				HImage hImage2 = FindImageInModelObject(item.Value);
				if (hImage2 != null)
				{
					return hImage2;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static HImage FindImageInTransmitParams(IEnumerable<TransmitParam> transmitParams, IReadOnlyCollection<string> exactNames = null)
	{
		if (transmitParams == null)
		{
			return null;
		}
		List<TransmitParam> list = transmitParams.Where((TransmitParam item) => item != null).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		if (exactNames != null && exactNames.Count > 0)
		{
			foreach (TransmitParam item in list)
			{
				if (IsExactParamName(item, exactNames))
				{
					HImage hImage = ConvertTransmitParamToImage(item);
					if (hImage != null)
					{
						return hImage;
					}
				}
			}
			return null;
		}
		string[] array = new string[7] { "Image", "InputImage", "DisposeImage", "OriginalImage", "PathImage", "采集图像", "被处理的图像" };
		string[] array2 = array;
		foreach (string preferredName in array2)
		{
			TransmitParam param = list.FirstOrDefault((TransmitParam item) => string.Equals(item.Name, preferredName, StringComparison.OrdinalIgnoreCase) || string.Equals(item.ParamName, preferredName, StringComparison.OrdinalIgnoreCase));
			HImage hImage2 = ConvertTransmitParamToImage(param);
			if (hImage2 != null)
			{
				return hImage2;
			}
		}
		foreach (TransmitParam item2 in list)
		{
			HImage hImage3 = ConvertTransmitParamToImage(item2);
			if (hImage3 != null)
			{
				return hImage3;
			}
		}
		return null;
	}

	private static List<Result> FindResultsInTransmitParams(IEnumerable<TransmitParam> transmitParams)
	{
		if (transmitParams == null)
		{
			return new List<Result>();
		}
		List<TransmitParam> list = transmitParams.Where((TransmitParam item) => item != null).ToList();
		if (list.Count == 0)
		{
			return new List<Result>();
		}
		string[] array = new string[5] { "Results", "InputResults", "PublishedResults", "杈撳叆缂洪櫡缁撴灉", "绛涢€夊悗鐨勭己闄风粨鏋?" };
		string[] array2 = array;
		foreach (string preferredName in array2)
		{
			List<Result> list2 = ExtractResults(list.FirstOrDefault((TransmitParam item) => string.Equals(item.Name, preferredName, StringComparison.OrdinalIgnoreCase) || string.Equals(item.ParamName, preferredName, StringComparison.OrdinalIgnoreCase))?.Value);
			if (list2.Count > 0)
			{
				return list2;
			}
		}
		foreach (TransmitParam item in list)
		{
			List<Result> list3 = ExtractResults(item.Value);
			if (list3.Count > 0)
			{
				return list3;
			}
		}
		return new List<Result>();
	}

	private static HImage FindImageInModelObject(object model, IReadOnlyCollection<string> exactNames = null)
	{
		if (model == null)
		{
			return null;
		}
		Type type = model.GetType();
		if (exactNames != null && exactNames.Count > 0)
		{
			foreach (string exactName in exactNames)
			{
				PropertyInfo property = type.GetProperty(exactName, BindingFlags.Instance | BindingFlags.Public);
				if (!(property == null) && property.GetIndexParameters().Length == 0)
				{
					HImage hImage = ConvertToImageSafe(property.GetValue(model));
					if (hImage != null)
					{
						return hImage;
					}
				}
			}
			return null;
		}
		string[] array = new string[5] { "Image", "InputImage", "DisposeImage", "OriginalImage", "PathImage" };
		string[] array2 = array;
		foreach (string name in array2)
		{
			PropertyInfo property2 = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
			if (!(property2 == null) && property2.GetIndexParameters().Length == 0)
			{
				HImage hImage2 = ConvertToImageSafe(property2.GetValue(model));
				if (hImage2 != null)
				{
					return hImage2;
				}
			}
		}
		return null;
	}

	private static List<Result> FindResultsInModelObject(object model)
	{
		if (model == null)
		{
			return new List<Result>();
		}
		string[] array = new string[2] { "Results", "PublishedResults" };
		Type type = model.GetType();
		string[] array2 = array;
		foreach (string name in array2)
		{
			PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
			if (!(property == null) && property.GetIndexParameters().Length == 0)
			{
				List<Result> list = ExtractResults(property.GetValue(model));
				if (list.Count > 0)
				{
					return list;
				}
			}
		}
		return new List<Result>();
	}

	private static List<string> GetExactParamNames(TransmitParam param)
	{
		List<string> list = new List<string>();
		AddExactParamName(list, param?.Name);
		AddExactParamName(list, param?.ParamName);
		return list;
	}

	private static void AddExactParamName(ICollection<string> exactNames, string value)
	{
		if (!string.IsNullOrWhiteSpace(value) && !exactNames.Any((string item) => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
		{
			exactNames.Add(value);
		}
	}

	private static bool IsExactParamName(TransmitParam param, IReadOnlyCollection<string> exactNames)
	{
		if (param == null || exactNames == null || exactNames.Count == 0)
		{
			return false;
		}
		return exactNames.Any((string exactName) => string.Equals(param.Name, exactName, StringComparison.OrdinalIgnoreCase) || string.Equals(param.ParamName, exactName, StringComparison.OrdinalIgnoreCase));
	}

	private static IEnumerable<string> EnumerateSourceCacheKeys(int sourceSerial)
	{
		if (sourceSerial >= 0)
		{
			HashSet<string> yieldedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			string rawKey = sourceSerial.ToString(CultureInfo.InvariantCulture);
			if (yieldedKeys.Add(rawKey))
			{
				yield return rawKey;
			}
			string paddedKey = sourceSerial.ToString("D3", CultureInfo.InvariantCulture);
			if (yieldedKeys.Add(paddedKey))
			{
				yield return paddedKey;
			}
		}
	}

	private static HImage ConvertToImageSafe(object value)
	{
		try
		{
			return ConvertToImage(value);
		}
		catch
		{
			return null;
		}
	}

	private static int ResolveCachePriority(string cacheKey)
	{
		if (string.IsNullOrWhiteSpace(cacheKey))
		{
			return int.MinValue;
		}
		int result;
		return int.TryParse(cacheKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : (-2147483647);
	}

	private static string DescribeImageState(HImage image)
	{
		try
		{
			if (image == null)
			{
				return "null";
			}
			if (!image.IsInitialized())
			{
				return "not-initialized";
			}
			image.GetImageSize(out int width, out int height);
			int value = image.CountChannels();
			int value2 = image.CountObj();
			return $"{width}x{height}, ch={value}, obj={value2}";
		}
		catch (Exception ex)
		{
			return "invalid:" + ex.GetType().Name;
		}
	}

	private static string DescribeBitmapState(BitmapSource bitmap)
	{
		if (bitmap == null)
		{
			return "null";
		}
		return $"{bitmap.PixelWidth}x{bitmap.PixelHeight}, format={bitmap.Format}";
	}

	private static string DescribeResultIdentity(Result result)
	{
		if (result == null)
		{
			return "null";
		}
		string value = (string.IsNullOrWhiteSpace(result.ClassName) ? "-" : result.ClassName);
		return $"{value}#{result.ClassId}";
	}

	private static string DescribeResultImageState(IReadOnlyCollection<Result> results)
	{
		if (results == null || results.Count == 0)
		{
			return "results=0";
		}
		int num = 0;
		int num2 = 0;
		foreach (Result result in results)
		{
			if (result?.Others != null)
			{
				object value2;
				if (result.Others.TryGetValue("DefectOverview_DisplayTargetBitmap", out var value) && value != null)
				{
					num++;
				}
				else if (result.Others.TryGetValue("XYHD_DisplayTargetBitmap", out value2) && value2 != null)
				{
					num++;
				}
			}
			if (result?.Seg == null)
			{
				continue;
			}
			try
			{
				if (result.Seg.IsInitialized())
				{
					num2++;
				}
			}
			catch
			{
			}
		}
		return $"results={results.Count}, metadataBitmap={num}, seg={num2}";
	}

    }
}
