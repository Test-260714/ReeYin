using Custom.XYHD.Models;
using Custom.DefectOverview.Models;
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
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Custom.XYHD.ViewModels
{
    public sealed class DetectionPublishConfigViewModel : DialogViewModelBase, IViewModuleParam
    {
        private bool _isDialogContext;
        private DetectionModel _model = new();

        public DetectionModel Model
        {
            get => _model;
            set => AttachModel(value ?? new DetectionModel());
        }

        public ObservableCollection<TransmitParam> ImageCandidates { get; } = new();

        public ObservableCollection<TransmitParam> ResultCandidates { get; } = new();

        public DelegateCommand RefreshInputParamsCommand { get; }

        public DelegateCommand<string> ClearSelectedInputCommand { get; }

        public DelegateCommand ExecuteCommand { get; }

        public DelegateCommand ConfirmCommand { get; }

        public DelegateCommand CancelCommand { get; }

        public DelegateCommand SelectSavePathCommand { get; }

        public DetectionPublishConfigViewModel()
        {
            RefreshInputParamsCommand = new DelegateCommand(RefreshInputParams);
            ClearSelectedInputCommand = new DelegateCommand<string>(ClearSelectedInput);
            ExecuteCommand = new DelegateCommand(() => _ = Model.ExecuteModule());
            ConfirmCommand = new DelegateCommand(Confirm);
            CancelCommand = new DelegateCommand(() => CloseDialogWhenReady(ButtonResult.No));
            SelectSavePathCommand = new DelegateCommand(SelectSavePath);
        }

        public override void InitParam()
        {
            _isDialogContext = true;
            base.InitParam();
            Model = InitModelParam<DetectionModel>();
            RefreshInputParams();

            if (Visibility == Visibility.Hidden)
            {
                CloseDialogWhenReady(ButtonResult.OK, new DialogParameters
                {
                    { "Param", Model }
                });
            }
        }

        public override void OnDialogClosed()
        {
            base.OnDialogClosed();
            if (_model != null)
            {
                _model.IsDebug = false;
                _model.PropertyChanged -= OnModelPropertyChanged;
            }
        }

        public string InputPortSummary
        {
            get
            {
                var model = Model;
                return string.Join(" | ", new[]
                {
                    $"原图:{DescribeSelectedInput(model.InputOriginalImage, model.InputOriginalImageName)}",
                    $"左图:{DescribeSelectedInput(model.LeftInputImage, model.LeftInputImageName)}",
                    $"左结果:{DescribeSelectedInput(model.LeftInputResults, model.LeftInputResultsName)}",
                    $"右图:{DescribeSelectedInput(model.RightInputImage, model.RightInputImageName)}",
                    $"右结果:{DescribeSelectedInput(model.RightInputResults, model.RightInputResultsName)}"
                });
            }
        }

        public string OutputPortSummary
        {
            get
            {
                var names = Model.OutputParams?
                    .Select(param => param?.Name ?? param?.ParamName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()
                    .ToList();

                return names == null || names.Count == 0
                    ? DetectionModel.DetectionOutputParamName
                    : string.Join(" | ", names);
            }
        }

        public string FieldOrientationSummary
        {
            get
            {
                var model = Model;
                return $"现场左路 <- 图像:{DescribeFieldImageSource(model, true)}, 结果:{DescribeFieldResultSource(model, true)} ({FormatMirrorText(model.LeftPathXMirror)})；现场右路 <- 图像:{DescribeFieldImageSource(model, false)}, 结果:{DescribeFieldResultSource(model, false)} ({FormatMirrorText(model.RightPathXMirror)})";
            }
        }

        private void AttachModel(DetectionModel model)
        {
            if (ReferenceEquals(_model, model))
                return;

            if (_model != null)
                _model.PropertyChanged -= OnModelPropertyChanged;

            _model = model;

            if (_model != null)
                _model.PropertyChanged += OnModelPropertyChanged;

            RaisePropertyChanged(nameof(Model));
            RaiseSummaries();
        }

        private void OnModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaiseSummaries();
        }

        private void RefreshInputParams()
        {
            if (Model.Serial == -999)
                Model.Serial = Serial;

            Model.LoadKeyParam();
            Model.EnsureDefaultOutputParam(Guid, Name);
            Model.TryRebindInputLinks();
            Model.SyncInputNamesFromLinks();
            RefreshCandidates();
            RaiseSummaries();
        }

        private void Confirm()
        {
            Model.TryRebindInputLinks();
            Model.SyncInputNamesFromLinks();
            Model.EnsureDefaultOutputParam(Guid, Name);
            Model.moduleOutputParam.TransmitParams = Model.OutputParams.ToDictionary(
                item => item.Guid.ToString(),
                item => (object)item);

            CloseDialogWhenReady(ButtonResult.OK, new DialogParameters
            {
                { "Param", Model }
            });
        }

        private void CloseDialogWhenReady(ButtonResult buttonResult, IDialogParameters dialogParameters = null)
        {
            if (!_isDialogContext)
                return;

            var dispatcher = PrismProvider.Dispatcher ?? Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                RequestClose.Invoke(dialogParameters, buttonResult);
                return;
            }

            dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(() => RequestClose.Invoke(dialogParameters, buttonResult)));
        }

        private void ClearSelectedInput(string name)
        {
            switch (name)
            {
                case "OriginalImage":
                    Model.InputOriginalImage = new TransmitParam();
                    break;
                case "LeftImage":
                    Model.LeftInputImage = new TransmitParam();
                    break;
                case "LeftResults":
                    Model.LeftInputResults = new TransmitParam();
                    break;
                case "RightImage":
                    Model.RightInputImage = new TransmitParam();
                    break;
                case "RightResults":
                    Model.RightInputResults = new TransmitParam();
                    break;
            }

            RefreshCandidates();
            RaiseSummaries();
        }

        private void SelectSavePath()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择图像保存路径",
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrWhiteSpace(Model.SavePath) && Directory.Exists(Model.SavePath))
                dialog.SelectedPath = Model.SavePath;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                Model.SavePath = dialog.SelectedPath;
        }

        private void RefreshCandidates()
        {
            List<TransmitParam> all = CollectSelectableParams();
            MergeCurrentSelections(all);

            ResetCollection(ImageCandidates, all.Where(PathInputSelectionHelper.IsImageParam));
            ResetCollection(ResultCandidates, all.Where(PathInputSelectionHelper.IsResultParam));

            Model.InputOriginalImage = PathInputSelectionHelper.MatchInputParam(Model.InputOriginalImage, ImageCandidates);
            Model.LeftInputImage = PathInputSelectionHelper.MatchPathInputParam(Model.LeftInputImage, ImageCandidates, DefectOverviewPathRole.Left);
            Model.LeftInputResults = PathInputSelectionHelper.MatchPathInputParam(Model.LeftInputResults, ResultCandidates, DefectOverviewPathRole.Left);
            Model.RightInputImage = PathInputSelectionHelper.MatchPathInputParam(Model.RightInputImage, ImageCandidates, DefectOverviewPathRole.Right);
            Model.RightInputResults = PathInputSelectionHelper.MatchPathInputParam(Model.RightInputResults, ResultCandidates, DefectOverviewPathRole.Right);
        }

        private void MergeCurrentSelections(ICollection<TransmitParam> target)
        {
            AddIfMissing(target, Model.InputOriginalImage);
            AddIfMissing(target, Model.LeftInputImage);
            AddIfMissing(target, Model.LeftInputResults);
            AddIfMissing(target, Model.RightInputImage);
            AddIfMissing(target, Model.RightInputResults);
        }

        private List<TransmitParam> CollectSelectableParams()
        {
            Dictionary<string, TransmitParam> unique = new(StringComparer.OrdinalIgnoreCase);

            void AddParam(TransmitParam param)
            {
                if (param == null)
                    return;

                string key = BuildParamKey(param);
                if (!unique.TryGetValue(key, out TransmitParam existing) || PathInputSelectionHelper.ShouldReplaceCandidate(existing, param))
                    unique[key] = param;
            }

            foreach (TransmitParam param in Model.InputParams ?? Enumerable.Empty<TransmitParam>())
                AddParam(param);

            foreach (TransmitParam param in CollectGraphOutputParams())
                AddParam(param);

            return unique.Values
                .Where(item => item != null && (!string.IsNullOrWhiteSpace(item.Name) || !string.IsNullOrWhiteSpace(item.ParamName)))
                .OrderBy(item => item.Serial)
                .ThenBy(item => item.Name)
                .ThenBy(item => item.ParamName)
                .ToList();
        }

        private IEnumerable<TransmitParam> CollectGraphOutputParams()
        {
            var solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solution?.NodeCaches is not IEnumerable nodeCaches || solution.NodeCaches is string)
                yield break;

            List<object> nodes = nodeCaches.Cast<object>().Where(item => item != null).ToList();
            object currentNode = nodes.FirstOrDefault(node => TryGetNodeId(node, out Guid nodeId) && nodeId == Guid);
            IEnumerable<object> relevantNodes = currentNode != null
                ? EnumerateAncestorNodes(currentNode)
                : nodes;

            foreach (object node in relevantNodes)
            {
                foreach (TransmitParam param in GetNodeOutputParams(node))
                    yield return param;
            }
        }

        private static IEnumerable<object> EnumerateAncestorNodes(object currentNode)
        {
            Stack<object> stack = new();
            HashSet<Guid> visited = new();

            foreach (object parentNode in GetNodeCollectionProperty(currentNode, "LastNodes"))
                stack.Push(parentNode);

            while (stack.Count > 0)
            {
                object node = stack.Pop();
                if (node == null)
                    continue;

                if (TryGetNodeId(node, out Guid nodeId) && !visited.Add(nodeId))
                    continue;

                yield return node;

                foreach (object parentNode in GetNodeCollectionProperty(node, "LastNodes"))
                    stack.Push(parentNode);
            }
        }

        private static IEnumerable<object> GetNodeCollectionProperty(object source, string propertyName)
        {
            object value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
            if (value is not IEnumerable enumerable || value is string)
                yield break;

            foreach (object item in enumerable)
            {
                if (item != null)
                    yield return item;
            }
        }

        private static IEnumerable<TransmitParam> GetNodeOutputParams(object node)
        {
            object moduleParamObject = node?.GetType().GetProperty("ModuleParam")?.GetValue(node);
            if (moduleParamObject is not ModelParamBase model || model.OutputParams == null)
                yield break;

            foreach (TransmitParam param in model.OutputParams)
            {
                if (param != null)
                    yield return param;
            }
        }

        private static bool TryGetNodeId(object node, out Guid nodeId)
        {
            nodeId = Guid.Empty;
            object value = node?.GetType().GetProperty("Id")?.GetValue(node);
            if (value is Guid id)
            {
                nodeId = id;
                return true;
            }

            return false;
        }

        private static TransmitParam MatchInputParam(TransmitParam current, IEnumerable<TransmitParam> candidates)
        {
            List<TransmitParam> list = candidates?.Where(item => item != null).ToList() ?? new List<TransmitParam>();
            if (current == null)
                return new TransmitParam();

            TransmitParam matched = list.FirstOrDefault(item => HasSameGuid(item, current));
            matched ??= list.FirstOrDefault(item => HasSharedIdentityWithSameSerial(item, current));
            matched ??= list.FirstOrDefault(item => IsUsableCandidate(item) && HasSharedParamIdentity(item, current));
            if (matched != null)
                return matched;

            return string.IsNullOrWhiteSpace(current.Name) && string.IsNullOrWhiteSpace(current.ParamName)
                ? new TransmitParam()
                : current;
        }

        private static TransmitParam MatchPathInputParam(TransmitParam current, IEnumerable<TransmitParam> candidates, bool isLeft)
        {
            List<TransmitParam> list = candidates?.Where(item => item != null).ToList() ?? new List<TransmitParam>();
            TransmitParam matched = MatchInputParam(current, list);
            TransmitParam preferred = list.FirstOrDefault(item => IsPathCandidate(item, isLeft));
            if (HasConfiguredInputSelection(current) || preferred == null)
                return matched;

            return preferred;
        }

        private static bool HasConfiguredInputSelection(TransmitParam param)
        {
            return param != null
                && (param.IsLink
                    || !string.IsNullOrWhiteSpace(param.Name)
                    || !string.IsNullOrWhiteSpace(param.ParamName)
                    || param.Value != null);
        }

        private static bool IsPathCandidate(TransmitParam param, bool isLeft)
        {
            string text = $"{param?.Name} {param?.ParamName} {param?.ParentNode} {param?.ResourcePath}";
            return isLeft ? IsLeftPathText(text) : IsRightPathText(text);
        }

        private static bool IsLeftPathText(string text)
        {
            return !string.IsNullOrWhiteSpace(text)
                && (text.Contains("左", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("left", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("path1", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("lane1", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsRightPathText(string text)
        {
            return !string.IsNullOrWhiteSpace(text)
                && (text.Contains("右", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("right", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("path2", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("lane2", StringComparison.OrdinalIgnoreCase));
        }

        private static void AddIfMissing(ICollection<TransmitParam> target, TransmitParam param)
        {
            if (target == null || param == null)
                return;

            if (string.IsNullOrWhiteSpace(param.Name) && string.IsNullOrWhiteSpace(param.ParamName))
                return;

            if (target.Any(item => IsSameParam(item, param)))
                return;

            target.Add(param);
        }

        private static string BuildParamKey(TransmitParam param)
        {
            if (param.Guid != Guid.Empty)
                return param.Guid.ToString();

            string sourceKey = BuildSourceKey(param);
            if (!string.IsNullOrWhiteSpace(sourceKey))
                return sourceKey;

            List<string> identities = GetParamIdentities(param);
            if (param.Serial >= 0 && identities.Count > 0)
                return $"{param.Serial:D3}:{string.Join("|", identities)}";

            return $"{param.Serial:D3}:{param.Name}:{param.ParamName}";
        }

        private static bool IsSameParam(TransmitParam left, TransmitParam right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null)
                return false;

            if (HasSameGuid(left, right))
                return true;

            if (HasSharedIdentityWithSameSerial(left, right))
                return true;

            return HasSharedParamIdentity(left, right)
                && (!IsUsableCandidate(left) || !IsUsableCandidate(right));
        }

        private static bool HasSameGuid(TransmitParam left, TransmitParam right)
        {
            return left?.Guid != Guid.Empty
                && right?.Guid != Guid.Empty
                && left.Guid == right.Guid;
        }

        private static bool HasSharedIdentityWithSameSerial(TransmitParam left, TransmitParam right)
        {
            return left != null
                && right != null
                && left.Serial >= 0
                && right.Serial >= 0
                && left.Serial == right.Serial
                && !HasConflictingGuid(left, right)
                && HasSameSourceKey(left, right)
                && HasSharedParamIdentity(left, right);
        }

        private static bool HasConflictingGuid(TransmitParam left, TransmitParam right)
        {
            return left?.Guid != Guid.Empty
                && right?.Guid != Guid.Empty
                && left.Guid != right.Guid;
        }

        private static bool HasSameSourceKey(TransmitParam left, TransmitParam right)
        {
            string leftKey = BuildSourceKey(left);
            string rightKey = BuildSourceKey(right);
            if (string.IsNullOrWhiteSpace(leftKey) || string.IsNullOrWhiteSpace(rightKey))
                return true;

            return string.Equals(leftKey, rightKey, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildSourceKey(TransmitParam param)
        {
            if (param == null)
                return string.Empty;

            List<string> parts = new();
            if (param.Serial >= 0)
                parts.Add($"S:{param.Serial:D3}");
            AddSourceKeyPart(parts, "P", param.ParentNode);
            AddSourceKeyPart(parts, "L", param.LinkGuid == Guid.Empty ? null : param.LinkGuid.ToString());
            AddSourceKeyPart(parts, "R", param.ResourcePath);
            AddSourceKeyPart(parts, "N", param.Name);
            AddSourceKeyPart(parts, "PN", param.ParamName);
            return parts.Count == 0 ? string.Empty : string.Join("|", parts);
        }

        private static void AddSourceKeyPart(ICollection<string> parts, string prefix, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add($"{prefix}:{value}");
        }

        private static bool HasSharedParamIdentity(TransmitParam left, TransmitParam right)
        {
            List<string> leftIdentities = GetParamIdentities(left);
            List<string> rightIdentities = GetParamIdentities(right);
            return leftIdentities.Count > 0
                && rightIdentities.Count > 0
                && leftIdentities.Any(leftIdentity =>
                    rightIdentities.Any(rightIdentity =>
                        string.Equals(leftIdentity, rightIdentity, StringComparison.OrdinalIgnoreCase)));
        }

        private static List<string> GetParamIdentities(TransmitParam param)
        {
            List<string> identities = new();
            AddParamIdentity(identities, param?.Name);
            AddParamIdentity(identities, param?.ParamName);
            return identities;
        }

        private static void AddParamIdentity(ICollection<string> identities, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (!identities.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
                identities.Add(value);
        }

        private static bool IsUsableCandidate(TransmitParam param)
        {
            return param != null
                && (param.Serial >= 0
                    || param.IsLink
                    || param.Resourece == ResoureceType.Inupt
                    || param.Resourece == ResoureceType.LastInput
                    || param.Value != null);
        }

        private static bool ShouldReplaceCandidate(TransmitParam existing, TransmitParam incoming)
        {
            if (existing == null || incoming == null)
                return incoming != null;

            if (string.IsNullOrWhiteSpace(existing.ParentNode) && !string.IsNullOrWhiteSpace(incoming.ParentNode))
                return true;

            if (string.IsNullOrWhiteSpace(existing.ResourcePath) && !string.IsNullOrWhiteSpace(incoming.ResourcePath))
                return true;

            return existing.Value == null && incoming.Value != null;
        }

        private static void ResetCollection(ObservableCollection<TransmitParam> target, IEnumerable<TransmitParam> source)
        {
            target.Clear();
            foreach (TransmitParam item in source ?? Enumerable.Empty<TransmitParam>())
                target.Add(item);
        }

        private static bool IsImageParam(TransmitParam param)
        {
            if (param == null)
                return false;

            if (param.Value is HImage || param.Value is HObject)
                return true;

            if (param.Type == DataType.HObject || param.Type == DataType.Mat)
                return true;

            string name = $"{param.Name} {param.ParamName}";
            return name.Contains("PathImage", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Image", StringComparison.OrdinalIgnoreCase)
                || name.Contains("图像", StringComparison.OrdinalIgnoreCase)
                || name.Contains("原图", StringComparison.OrdinalIgnoreCase)
                || name.Contains("路径图", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsResultParam(TransmitParam param)
        {
            if (param == null)
                return false;

            if (param.Value is Result || param.Value is IEnumerable<Result>)
                return true;

            if (param.Value is IEnumerable enumerable && param.Value is not string)
            {
                foreach (object item in enumerable)
                {
                    if (item is Result || item is IEnumerable<Result>)
                        return true;
                }
            }

            string name = $"{param.Name} {param.ParamName}";
            return name.Contains("Results", StringComparison.OrdinalIgnoreCase)
                || name.Contains("DefectResults", StringComparison.OrdinalIgnoreCase)
                || name.Contains("缺陷结果", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("结果", StringComparison.OrdinalIgnoreCase);
        }

        private static string DescribeSelectedInput(TransmitParam param, string fallbackName)
        {
            if (!string.IsNullOrWhiteSpace(param?.Name))
                return param.Name;

            if (!string.IsNullOrWhiteSpace(param?.ParamName))
                return param.ParamName;

            return string.IsNullOrWhiteSpace(fallbackName) ? "未选择" : fallbackName;
        }

        private static string DescribeFieldImageSource(DetectionModel model, bool fieldLeft)
        {
            bool useRightInput = fieldLeft ? model.SwapLeftRightPaths : !model.SwapLeftRightPaths;
            return useRightInput
                ? DescribeSelectedInput(model.RightInputImage, model.RightInputImageName)
                : DescribeSelectedInput(model.LeftInputImage, model.LeftInputImageName);
        }

        private static string DescribeFieldResultSource(DetectionModel model, bool fieldLeft)
        {
            bool useRightInput = fieldLeft ? model.SwapLeftRightPaths : !model.SwapLeftRightPaths;
            return useRightInput
                ? DescribeSelectedInput(model.RightInputResults, model.RightInputResultsName)
                : DescribeSelectedInput(model.LeftInputResults, model.LeftInputResultsName);
        }

        private static string FormatMirrorText(bool enabled)
        {
            return enabled ? "X 坐标镜像" : "X 坐标不镜像";
        }

        private void RaiseSummaries()
        {
            RaisePropertyChanged(nameof(InputPortSummary));
            RaisePropertyChanged(nameof(OutputPortSummary));
            RaisePropertyChanged(nameof(FieldOrientationSummary));
        }
    }
}
