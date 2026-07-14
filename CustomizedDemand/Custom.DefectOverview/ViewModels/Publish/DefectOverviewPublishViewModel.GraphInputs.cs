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
	private List<TransmitParam> CollectSelectableParams()
	{
		Dictionary<string, TransmitParam> unique = new Dictionary<string, TransmitParam>(StringComparer.OrdinalIgnoreCase);
		IEnumerable<TransmitParam> enumerable = ModelParam?.InputParams;
		foreach (TransmitParam item in enumerable ?? Enumerable.Empty<TransmitParam>())
		{
			AddParam(item);
		}
		foreach (TransmitParam item2 in CollectGraphOutputParams())
		{
			AddParam(item2);
		}
		return (from item in unique.Values
			where item != null && (!string.IsNullOrWhiteSpace(item.Name) || !string.IsNullOrWhiteSpace(item.ParamName))
			orderby item.Serial, item.Name, item.ParamName
			select item).ToList();
		void AddParam(TransmitParam param)
		{
			if (param != null)
			{
				string key = BuildParamKey(param);
				if (!unique.TryGetValue(key, out var value) || PathInputSelectionHelper.ShouldReplaceCandidate(value, param))
				{
					unique[key] = param;
				}
			}
		}
	}

	private IEnumerable<TransmitParam> CollectGraphOutputParams()
	{
		NodifySolutionItem solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
		object obj = solution?.NodeCaches;
		IEnumerable nodeCaches = obj as IEnumerable;
		if (nodeCaches == null || solution.NodeCaches is string)
		{
			yield break;
		}
		List<object> nodes = (from object item in nodeCaches
			where item != null
			select item).ToList();
		if (nodes.Count == 0)
		{
			yield break;
		}
		Guid nodeId;
		object currentNode = nodes.FirstOrDefault((object node2) => TryGetNodeId(node2, out nodeId) && nodeId == base.Guid);
		IEnumerable<object> relevantNodes = ((currentNode != null) ? EnumerateAncestorNodes(currentNode) : nodes);
		foreach (object node in relevantNodes)
		{
			foreach (TransmitParam nodeOutputParam in GetNodeOutputParams(node))
			{
				yield return nodeOutputParam;
			}
		}
	}

	private IEnumerable<object> EnumerateAncestorNodes(object currentNode)
	{
		Stack<object> stack = new Stack<object>();
		HashSet<Guid> visited = new HashSet<Guid>();
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
			if (!TryGetNodeId(node, out var nodeId))
			{
				yield return node;
			}
			else
			{
				if (!visited.Add(nodeId))
				{
					continue;
				}
				yield return node;
			}
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
		ModelParamBase model = moduleParamObject as ModelParamBase;
		if (model == null || model.OutputParams == null)
		{
			yield break;
		}
		foreach (TransmitParam param in model.OutputParams)
		{
			if (param != null)
			{
				yield return param;
			}
		}
	}

	private static bool TryGetNodeId(object node, out Guid nodeId)
	{
		nodeId = Guid.Empty;
		if (node?.GetType().GetProperty("Id")?.GetValue(node) is Guid guid)
		{
			nodeId = guid;
			return true;
		}
		return false;
	}

    }
}
