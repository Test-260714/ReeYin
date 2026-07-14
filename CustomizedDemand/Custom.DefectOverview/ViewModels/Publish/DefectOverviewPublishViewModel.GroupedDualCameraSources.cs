using Custom.DefectOverview.Models.Common;
using Custom.DefectOverview.Models.GroupedDualCamera;
using Custom.DefectOverview.Services;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Custom.DefectOverview.ViewModels
{
    public sealed partial class DefectOverviewPublishViewModel : DialogViewModelBase, IViewModuleParam
    {
        private sealed class GroupedDualCameraSourceInfo
        {
            public int SourceSerial { get; set; } = -1;

            public int SourceIndex { get; set; }

            public string CameraName { get; set; } = string.Empty;

            public string OutputName { get; set; } = string.Empty;

            public TransmitParam ImageInput { get; set; } = new();
        }

        private sealed class GroupedDualCameraSourceCameraRow
        {
            public int Index { get; set; }

            public string CameraName { get; set; } = string.Empty;

            public string OutputName { get; set; } = string.Empty;
        }

        private void RefreshGroupedDualCameraSourceBindings()
        {
            List<GroupedDualCameraSourceInfo> sources = CollectGroupedDualCameraSources();
            RefreshGroupedDualCameraGroupOptions(sources.Count);

            if (sources.Count == 0)
            {
                ModelParam.EnsureDefaultGroupedDualCameraBindings();
                return;
            }

            List<GroupedDualCameraBinding> previous = ModelParam.GroupedDualCameraBindings?
                .Where(binding => binding != null)
                .OrderBy(binding => binding.SortIndex)
                .ToList() ?? new List<GroupedDualCameraBinding>();
            ObservableCollection<GroupedDualCameraBinding> next = new();
            List<TransmitParam> orderedResults = ResultCandidates
                .Where(param => param != null)
                .OrderBy(param => param.Serial)
                .ThenBy(param => param.Name)
                .ThenBy(param => param.ParamName)
                .ToList();

            for (int i = 0; i < sources.Count; i++)
            {
                GroupedDualCameraSourceInfo source = sources[i];
                GroupedDualCameraBinding existing = FindExistingGroupedDualCameraBinding(previous, source, i);
                string groupKey = NormalizeGroupedDualCameraGroupOption(existing?.GroupKey);
                if (string.IsNullOrWhiteSpace(groupKey))
                    groupKey = ResolveDefaultGroupedDualCameraGroupKey(i);

                WidthSide side = existing?.Side is WidthSide.Left or WidthSide.Right
                    ? existing.Side
                    : ResolveDefaultGroupedDualCameraSide(i);
                TransmitParam resultInput = PathInputSelectionHelper.HasConfiguredInputSelection(existing?.ResultInput)
                    ? existing.ResultInput
                    : ResolveGroupedDualCameraResultForSource(source.ImageInput, orderedResults, i);

                next.Add(new GroupedDualCameraBinding
                {
                    SortIndex = i + 1,
                    SourceSerial = source.SourceSerial,
                    SourceCameraName = source.CameraName,
                    SourceOutputName = source.OutputName,
                    GroupKey = groupKey,
                    GroupName = source.CameraName,
                    Side = side,
                    DisplayName = BuildGroupedDualCameraDisplayIndex(groupKey, side),
                    ResultInput = resultInput ?? new TransmitParam(),
                    ImageInput = source.ImageInput ?? new TransmitParam(),
                    IsRequired = existing?.IsRequired ?? true
                });
            }

            ModelParam.GroupedDualCameraBindings = next;
        }

        private void RefreshGroupedDualCameraGroupOptions(int sourceCount)
        {
            int groupCount = Math.Max(3, (sourceCount + 1) / 2);
            List<string> next = Enumerable.Range(1, groupCount)
                .Select(index => index.ToString("D2", CultureInfo.InvariantCulture))
                .ToList();

            if (GroupedDualCameraGroupOptions.SequenceEqual(next, StringComparer.OrdinalIgnoreCase))
                return;

            GroupedDualCameraGroupOptions.Clear();
            foreach (string item in next)
                GroupedDualCameraGroupOptions.Add(item);
        }

        private List<GroupedDualCameraSourceInfo> CollectGroupedDualCameraSources()
        {
            Dictionary<string, GroupedDualCameraSourceInfo> sources = new(StringComparer.OrdinalIgnoreCase);
            foreach (object node in EnumerateGroupedDualCameraAncestorNodes())
            {
                object model = GetGroupedDualCameraNodeModel(node);
                if (model == null)
                    continue;

                int serial = GetGroupedDualCameraNodeSerial(node, model);
                foreach (GroupedDualCameraSourceCameraRow row in CollectGroupedDualCameraRows(model))
                {
                    TransmitParam imageInput = FindGroupedDualCameraImageCandidate(serial, row.OutputName);
                    if (imageInput == null)
                        continue;

                    string key = $"{serial:D3}:{row.OutputName}";
                    if (sources.ContainsKey(key))
                        continue;

                    sources[key] = new GroupedDualCameraSourceInfo
                    {
                        SourceSerial = serial,
                        SourceIndex = row.Index,
                        CameraName = string.IsNullOrWhiteSpace(row.CameraName) ? row.OutputName : row.CameraName,
                        OutputName = row.OutputName,
                        ImageInput = imageInput
                    };
                }
            }

            return sources.Values
                .OrderBy(source => source.SourceSerial)
                .ThenBy(source => source.SourceIndex)
                .ThenBy(source => source.OutputName)
                .ToList();
        }

        private IEnumerable<object> EnumerateGroupedDualCameraAncestorNodes()
        {
            NodifySolutionItem solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solution?.NodeCaches is not IEnumerable nodeCaches || solution.NodeCaches is string)
                yield break;

            List<object> nodes = nodeCaches
                .Cast<object>()
                .Where(node => node != null)
                .ToList();
            if (nodes.Count == 0)
                yield break;

            object currentNode = nodes.FirstOrDefault(node => TryGetNodeId(node, out Guid nodeId) && nodeId == Guid);
            IEnumerable<object> relevantNodes = currentNode != null ? EnumerateAncestorNodes(currentNode) : nodes;
            foreach (object node in relevantNodes)
                yield return node;
        }

        private IEnumerable<GroupedDualCameraSourceCameraRow> CollectGroupedDualCameraRows(object model)
        {
            bool usePseudo = ReadBoolProperty(model, "UsePseudoGrab");
            List<GroupedDualCameraSourceCameraRow> primary = ReadCameraRows(
                ReadEnumerableProperty(model, usePseudo ? "PseudoCameraItems" : "MultiCameraItems"));
            if (primary.Count > 0)
                return primary;

            List<GroupedDualCameraSourceCameraRow> pseudo = ReadCameraRows(ReadEnumerableProperty(model, "PseudoCameraItems"));
            if (pseudo.Count > 0)
                return pseudo;

            return ReadCameraRows(ReadEnumerableProperty(model, "MultiCameraItems"));
        }

        private static List<GroupedDualCameraSourceCameraRow> ReadCameraRows(IEnumerable items)
        {
            if (items == null)
                return new List<GroupedDualCameraSourceCameraRow>();

            List<GroupedDualCameraSourceCameraRow> rows = new();
            foreach (object item in items)
            {
                if (item == null)
                    continue;

                int index = ReadIntProperty(item, "Index", rows.Count + 1);
                string outputName = ReadStringProperty(item, "OutputName");
                if (string.IsNullOrWhiteSpace(outputName))
                    outputName = $"Image{Math.Max(1, index)}";

                rows.Add(new GroupedDualCameraSourceCameraRow
                {
                    Index = index,
                    CameraName = ReadStringProperty(item, "CameraNo"),
                    OutputName = outputName
                });
            }

            return rows
                .OrderBy(row => row.Index)
                .ThenBy(row => row.OutputName)
                .ToList();
        }

        private TransmitParam FindGroupedDualCameraImageCandidate(int serial, string outputName)
        {
            if (string.IsNullOrWhiteSpace(outputName))
                return null;

            List<TransmitParam> matches = ImageCandidates
                .Where(param => param != null && IsNamedTransmitParam(param, outputName))
                .ToList();
            TransmitParam serialMatch = matches.FirstOrDefault(param => param.Serial == serial);
            return serialMatch ?? (matches.Count == 1 ? matches[0] : null);
        }

        private TransmitParam ResolveGroupedDualCameraResultForSource(
            TransmitParam imageInput,
            IReadOnlyList<TransmitParam> orderedResults,
            int sourceIndex)
        {
            TransmitParam matched = orderedResults?
                .FirstOrDefault(result => IsResultSourceImage(result, imageInput));
            if (matched != null)
                return matched;

            if (orderedResults != null && sourceIndex >= 0 && sourceIndex < orderedResults.Count)
                return orderedResults[sourceIndex];

            return new TransmitParam();
        }

        private bool IsResultSourceImage(TransmitParam resultInput, TransmitParam imageInput)
        {
            if (resultInput == null || imageInput == null)
                return false;

            object model = FindGroupedDualCameraNodeModelBySerial(resultInput.Serial);
            TransmitParam directInput = ReadTransmitParamProperty(model, "InputImage");
            if (IsSameGroupedDualCameraParam(directInput, imageInput))
                return true;

            object binding = model?.GetType().GetProperty("InputImageBinding")?.GetValue(model);
            return IsSameGroupedDualCameraBinding(binding, imageInput);
        }

        private object FindGroupedDualCameraNodeModelBySerial(int serial)
        {
            if (serial < 0)
                return null;

            NodifySolutionItem solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solution?.NodeCaches is not IEnumerable nodeCaches || solution.NodeCaches is string)
                return null;

            foreach (object node in nodeCaches)
            {
                object model = GetGroupedDualCameraNodeModel(node);
                if (GetGroupedDualCameraNodeSerial(node, model) == serial)
                    return model;
            }

            return null;
        }

        private GroupedDualCameraBinding FindExistingGroupedDualCameraBinding(
            IReadOnlyList<GroupedDualCameraBinding> previous,
            GroupedDualCameraSourceInfo source,
            int sourceIndex)
        {
            if (previous == null || previous.Count == 0 || source == null)
                return null;

            GroupedDualCameraBinding matched = previous.FirstOrDefault(binding =>
                binding.SourceSerial == source.SourceSerial
                && string.Equals(binding.SourceOutputName, source.OutputName, StringComparison.OrdinalIgnoreCase));
            matched ??= previous.FirstOrDefault(binding => IsSameGroupedDualCameraParam(binding.ImageInput, source.ImageInput));
            matched ??= previous.FirstOrDefault(binding => binding.SortIndex == sourceIndex + 1);
            return matched;
        }

        private static string ResolveDefaultGroupedDualCameraGroupKey(int sourceIndex)
        {
            int groupIndex = sourceIndex / 2 + 1;
            return groupIndex.ToString("D2", CultureInfo.InvariantCulture);
        }

        private static WidthSide ResolveDefaultGroupedDualCameraSide(int sourceIndex)
        {
            return sourceIndex % 2 == 0 ? WidthSide.Left : WidthSide.Right;
        }

        private static string BuildGroupedDualCameraDisplayIndex(string groupKey, WidthSide side)
        {
            string normalizedGroup = NormalizeGroupedDualCameraGroupOption(groupKey);
            if (string.IsNullOrWhiteSpace(normalizedGroup))
                normalizedGroup = "??";

            string sideText = side switch
            {
                WidthSide.Left => "L",
                WidthSide.Right => "R",
                _ => "?"
            };
            return $"{normalizedGroup}-{sideText}";
        }

        private static string NormalizeGroupedDualCameraGroupOption(string groupKey)
        {
            if (string.IsNullOrWhiteSpace(groupKey))
                return string.Empty;

            string text = groupKey.Trim();
            if (text.Length >= 2
                && (text[0] == 'G' || text[0] == 'g')
                && int.TryParse(text[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int gIndex))
            {
                return gIndex.ToString("D2", CultureInfo.InvariantCulture);
            }

            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                ? index.ToString("D2", CultureInfo.InvariantCulture)
                : text;
        }

        private static bool IsSameGroupedDualCameraParam(TransmitParam left, TransmitParam right)
        {
            if (left == null || right == null)
                return false;

            if (left.Guid != Guid.Empty && right.Guid != Guid.Empty && left.Guid == right.Guid)
                return true;

            if (left.Serial >= 0
                && right.Serial >= 0
                && left.Serial == right.Serial
                && HasSharedGroupedDualCameraParamName(left, right))
            {
                return true;
            }

            return !HasUsableGroupedDualCameraIdentity(left)
                && !HasUsableGroupedDualCameraIdentity(right)
                && HasSharedGroupedDualCameraParamName(left, right);
        }

        private static bool IsSameGroupedDualCameraBinding(object binding, TransmitParam param)
        {
            if (binding == null || param == null)
                return false;

            Guid bindingGuid = ReadGuidProperty(binding, "Guid");
            if (bindingGuid != Guid.Empty && param.Guid != Guid.Empty && bindingGuid == param.Guid)
                return true;

            int bindingSerial = ReadIntProperty(binding, "Serial", -1);
            if (bindingSerial >= 0 && param.Serial >= 0 && bindingSerial != param.Serial)
                return false;

            string bindingName = ReadStringProperty(binding, "Name");
            string bindingParamName = ReadStringProperty(binding, "ParamName");
            return IsNamedTransmitParam(param, bindingName) || IsNamedTransmitParam(param, bindingParamName);
        }

        private static bool HasSharedGroupedDualCameraParamName(TransmitParam left, TransmitParam right)
        {
            return IsNamedTransmitParam(left, right.Name)
                || IsNamedTransmitParam(left, right.ParamName)
                || IsNamedTransmitParam(right, left.Name)
                || IsNamedTransmitParam(right, left.ParamName);
        }

        private static bool HasUsableGroupedDualCameraIdentity(TransmitParam param)
        {
            return param != null
                && (param.Guid != Guid.Empty
                    || param.Serial >= 0
                    || !string.IsNullOrWhiteSpace(param.Name)
                    || !string.IsNullOrWhiteSpace(param.ParamName));
        }

        private static bool IsNamedTransmitParam(TransmitParam param, string name)
        {
            return param != null
                && !string.IsNullOrWhiteSpace(name)
                && (string.Equals(param.Name, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(param.ParamName, name, StringComparison.OrdinalIgnoreCase));
        }

        private static object GetGroupedDualCameraNodeModel(object node)
        {
            return node?.GetType().GetProperty("ModuleParam")?.GetValue(node);
        }

        private static int GetGroupedDualCameraNodeSerial(object node, object model)
        {
            if (ReadNullableIntProperty(model, "Serial") is int modelSerial)
                return modelSerial;

            object menuInfo = node?.GetType().GetProperty("MenuInfo")?.GetValue(node);
            return ReadIntProperty(menuInfo, "Serial", -1);
        }

        private static TransmitParam ReadTransmitParamProperty(object source, string propertyName)
        {
            return source?.GetType().GetProperty(propertyName)?.GetValue(source) as TransmitParam;
        }

        private static IEnumerable ReadEnumerableProperty(object source, string propertyName)
        {
            object value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
            return value is string ? null : value as IEnumerable;
        }

        private static bool ReadBoolProperty(object source, string propertyName)
        {
            object value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
            return value is bool result && result;
        }

        private static int? ReadNullableIntProperty(object source, string propertyName)
        {
            object value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
            return value is int result ? result : null;
        }

        private static int ReadIntProperty(object source, string propertyName, int fallback)
        {
            object value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
            if (value is int intValue)
                return intValue;

            return value != null
                && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                    ? parsed
                    : fallback;
        }

        private static Guid ReadGuidProperty(object source, string propertyName)
        {
            object value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
            if (value is Guid guid)
                return guid;

            return value != null
                && Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out Guid parsed)
                    ? parsed
                    : Guid.Empty;
        }

        private static string ReadStringProperty(object source, string propertyName)
        {
            object value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
            return value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }
}
