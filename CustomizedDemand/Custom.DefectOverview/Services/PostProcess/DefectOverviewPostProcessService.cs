using ALGO.DefectPostProcess.Models;
using Custom.DefectOverview.Models;
using HalconDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Custom.DefectOverview.Services
{
    public interface IDefectOverviewPostProcessService
    {
        IReadOnlyList<Result> FilterResults(DefectOverviewPathPacket packet);
    }

    public sealed class DefectOverviewPostProcessService : IDefectOverviewPostProcessService
    {
        private const string DefectPostProcessModelTypeName = "ALGO.DefectPostProcess.Models.DefectPostProcessModel";
        private const string DefectPostProcessSchemeTypeName = "ALGO.DefectPostProcess.Models.DefectPostProcessScheme";

        private readonly object _sync = new();
        private RuntimeSchemeSnapshot _cachedSnapshot;
        private string _failedSchemeFilePath = string.Empty;
        private DateTime _failedLastWriteUtc;

        public IReadOnlyList<Result> FilterResults(DefectOverviewPathPacket packet)
        {
            if (packet == null)
                return Array.Empty<Result>();

            List<Result> fallbackResults = packet.Results?.Where(item => item != null).ToList() ?? new List<Result>();
            RuntimeSchemeSnapshot snapshot = GetRuntimeSchemeSnapshot(packet.SchemeFilePath);
            if (snapshot?.Scheme == null || fallbackResults.Count == 0)
                return fallbackResults;

            if (!TryCreateRuntimeModel(snapshot, packet, out DefectPostProcessModel model))
                return fallbackResults;

            try
            {
                ExecuteModuleOutput output = model.ExecuteModule().Result;
                if (output?.RunStatus != NodeStatus.Success && output?.RunStatus != NodeStatus.None)
                {
                    Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverview] DefectPostProcess execute skipped, status={output?.RunStatus}");
                    return fallbackResults;
                }

                return model.Results?.Where(item => item != null).ToList() ?? new List<Result>();
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverview] DefectPostProcess execute failed: {ex.Message}");
                return fallbackResults;
            }
            finally
            {
                try
                {
                    model.Dispose();
                }
                catch
                {
                }
            }
        }

        private RuntimeSchemeSnapshot GetRuntimeSchemeSnapshot(string preferredSchemePath)
        {
            string schemePath = ResolveSchemeFilePath(preferredSchemePath);
            if (string.IsNullOrWhiteSpace(schemePath) || !File.Exists(schemePath))
                return null;

            DateTime lastWriteUtc;
            try
            {
                lastWriteUtc = File.GetLastWriteTimeUtc(schemePath);
            }
            catch
            {
                return null;
            }

            lock (_sync)
            {
                if (_cachedSnapshot != null
                    && string.Equals(_cachedSnapshot.SourceFilePath, schemePath, StringComparison.OrdinalIgnoreCase)
                    && _cachedSnapshot.LastWriteUtc == lastWriteUtc)
                {
                    return _cachedSnapshot;
                }

                if (string.Equals(_failedSchemeFilePath, schemePath, StringComparison.OrdinalIgnoreCase)
                    && _failedLastWriteUtc == lastWriteUtc)
                {
                    return null;
                }

                if (!TryLoadRuntimeSchemeSnapshot(schemePath, lastWriteUtc, out RuntimeSchemeSnapshot snapshot))
                {
                    _failedSchemeFilePath = schemePath;
                    _failedLastWriteUtc = lastWriteUtc;
                    return null;
                }

                _cachedSnapshot = snapshot;
                _failedSchemeFilePath = string.Empty;
                _failedLastWriteUtc = default;
                return snapshot;
            }
        }

        private static string ResolveSchemeFilePath(string preferredSchemePath)
        {
            if (!string.IsNullOrWhiteSpace(preferredSchemePath))
                return preferredSchemePath;

            return PrismProvider.ProjectManager?.SolutionManager?.DefaultBaseInfo?.FilePath
                ?? PrismProvider.ProjectManager?.SltCurSolutionItem?.FilePath;
        }

        private static bool TryLoadRuntimeSchemeSnapshot(string schemePath, DateTime lastWriteUtc, out RuntimeSchemeSnapshot snapshot)
        {
            snapshot = null;

            try
            {
                JsonSerializerSettings settings = CreateJsonSettings();
                string json = File.ReadAllText(schemePath);
                JToken rootToken = JToken.Parse(json);

                if (TryLoadSchemeOnly(rootToken, settings, schemePath, lastWriteUtc, out snapshot))
                    return true;

                if (TryLoadSchemeFromModel(rootToken, settings, schemePath, lastWriteUtc, out snapshot))
                    return true;

                Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverview] No DefectPostProcess config found in scheme: {schemePath}");
                return false;
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverview] Load DefectPostProcess scheme failed: {ex.Message}");
                return false;
            }
        }

        private static bool TryLoadSchemeOnly(
            JToken rootToken,
            JsonSerializerSettings settings,
            string schemePath,
            DateTime lastWriteUtc,
            out RuntimeSchemeSnapshot snapshot)
        {
            snapshot = null;
            if (rootToken is not JObject rootObject)
                return false;

            string typeName = rootObject["$type"]?.Value<string>() ?? string.Empty;
            if (!typeName.StartsWith(DefectPostProcessSchemeTypeName, StringComparison.Ordinal))
                return false;

            DefectPostProcessScheme scheme = JsonConvert.DeserializeObject<DefectPostProcessScheme>(rootObject.ToString(), settings);
            if (scheme == null)
                return false;

            snapshot = new RuntimeSchemeSnapshot
            {
                SourceFilePath = schemePath,
                LastWriteUtc = lastWriteUtc,
                Scheme = CloneScheme(scheme),
                CurrentSchemeName = scheme.Name ?? string.Empty
            };
            return true;
        }

        private static bool TryLoadSchemeFromModel(
            JToken rootToken,
            JsonSerializerSettings settings,
            string schemePath,
            DateTime lastWriteUtc,
            out RuntimeSchemeSnapshot snapshot)
        {
            snapshot = null;

            IEnumerable<JObject> modelCandidates = EnumerateJsonObjects(rootToken);

            JObject modelObject = modelCandidates
                .FirstOrDefault(item =>
                {
                    string typeName = item["$type"]?.Value<string>() ?? string.Empty;
                    return typeName.StartsWith(DefectPostProcessModelTypeName, StringComparison.Ordinal);
                });

            if (modelObject == null)
                return false;

            DefectPostProcessModel model = null;
            try
            {
                model = JsonConvert.DeserializeObject<DefectPostProcessModel>(modelObject.ToString(), settings);
                if (model == null)
                    return false;

                DefectPostProcessScheme activeScheme = SelectActiveScheme(model);
                if (activeScheme == null)
                    return false;

                snapshot = new RuntimeSchemeSnapshot
                {
                    SourceFilePath = schemePath,
                    LastWriteUtc = lastWriteUtc,
                    Scheme = CloneScheme(activeScheme),
                    CurrentSchemeName = model.CurrentSchemeName ?? activeScheme.Name ?? string.Empty
                };
                return true;
            }
            finally
            {
                try
                {
                    model?.Dispose();
                }
                catch
                {
                }
            }
        }

        private static IEnumerable<JObject> EnumerateJsonObjects(JToken rootToken)
        {
            if (rootToken == null)
                yield break;

            if (rootToken is JObject rootObject)
                yield return rootObject;

            if (rootToken is JContainer container)
            {
                foreach (JObject childObject in container.Descendants().OfType<JObject>())
                    yield return childObject;
            }
        }

        private static DefectPostProcessScheme SelectActiveScheme(DefectPostProcessModel model)
        {
            if (model == null)
                return null;

            DefectPostProcessScheme matchedScheme = model.SchemeConfigs?
                .FirstOrDefault(item =>
                    item != null &&
                    !string.IsNullOrWhiteSpace(model.CurrentSchemeName) &&
                    string.Equals(item.Name, model.CurrentSchemeName, StringComparison.OrdinalIgnoreCase));

            if (matchedScheme != null)
                return BuildRuntimeScheme(matchedScheme, model);

            if (model.SchemeConfigs != null && model.SchemeConfigs.Count > 0)
                return BuildRuntimeScheme(model.SchemeConfigs[0], model);

            if (model.DefectRuleConfigs == null || model.DefectRuleConfigs.Count == 0)
                return null;

            return new DefectPostProcessScheme
            {
                Name = string.IsNullOrWhiteSpace(model.CurrentSchemeName) ? "Default" : model.CurrentSchemeName,
                SelectedRuleKey = string.Empty,
                DefectRuleConfigs = CloneRuleConfigs(model.DefectRuleConfigs),
                CalibrationFilePath = model.CalibrationFilePath ?? string.Empty
            };
        }

        private static DefectPostProcessScheme BuildRuntimeScheme(DefectPostProcessScheme scheme, DefectPostProcessModel model)
        {
            return new DefectPostProcessScheme
            {
                Name = scheme?.Name ?? model?.CurrentSchemeName ?? "Default",
                SelectedRuleKey = scheme?.SelectedRuleKey ?? string.Empty,
                DefectRuleConfigs = CloneRuleConfigs(scheme?.DefectRuleConfigs ?? model?.DefectRuleConfigs),
                CalibrationFilePath = !string.IsNullOrWhiteSpace(scheme?.CalibrationFilePath)
                    ? scheme.CalibrationFilePath
                    : model?.CalibrationFilePath ?? string.Empty,
                InputImageBinding = CloneBinding(scheme?.InputImageBinding),
                InputResultsBinding = CloneBinding(scheme?.InputResultsBinding),
                InputPixelEquivalentXBinding = CloneBinding(scheme?.InputPixelEquivalentXBinding),
                InputPixelEquivalentYBinding = CloneBinding(scheme?.InputPixelEquivalentYBinding),
                InputEdgeCalibrationXBinding = CloneBinding(scheme?.InputEdgeCalibrationXBinding)
            };
        }

        private static bool TryCreateRuntimeModel(
            RuntimeSchemeSnapshot snapshot,
            DefectOverviewPathPacket packet,
            out DefectPostProcessModel model)
        {
            model = null;

            try
            {
                string calibrationFilePath = ResolveCalibrationFilePath(snapshot.SourceFilePath, snapshot.Scheme?.CalibrationFilePath);
                bool usePixelFallback = ShouldInjectPixelFallback(snapshot.Scheme, calibrationFilePath)
                    && !packet.PixelEquivalentX.HasValue
                    && !packet.PixelEquivalentY.HasValue;

                model = new DefectPostProcessModel
                {
                    Serial = -1,
                    Name = $"{packet.SourceName}_DefectOverviewRuntime",
                    moduleInputParam = new ModuleParam(),
                    moduleOutputParam = new ModuleParam(),
                    OutputParams = new ObservableCollection<TransmitParam>(),
                    DefectRuleConfigs = CloneRuleConfigs(snapshot.Scheme?.DefectRuleConfigs),
                    CalibrationFilePath = calibrationFilePath,
                    CurrentSchemeName = snapshot.CurrentSchemeName,
                    SchemeName = snapshot.CurrentSchemeName
                };

                Dictionary<string, object> inputs = model.moduleInputParam.TransmitParams;

                TransmitParam imageParam = CreateRuntimeInputParam(
                    snapshot.Scheme?.InputImageBinding,
                    DataType.HObject,
                    "Image",
                    packet.PathImage);
                inputs[imageParam.Guid.ToString()] = imageParam;
                ApplyRuntimeBinding(model, imageParam, RuntimeBindingType.Image);

                TransmitParam resultsParam = CreateRuntimeInputParam(
                    snapshot.Scheme?.InputResultsBinding,
                    DataType.List,
                    "Results",
                    packet.Results?.ToList() ?? new List<Result>());
                inputs[resultsParam.Guid.ToString()] = resultsParam;
                ApplyRuntimeBinding(model, resultsParam, RuntimeBindingType.Results);

                if (packet.PixelEquivalentX.HasValue || usePixelFallback)
                {
                    double pixelEquivalentX = packet.PixelEquivalentX ?? 1d;
                    TransmitParam pixelXParam = CreateRuntimeInputParam(
                        snapshot.Scheme?.InputPixelEquivalentXBinding,
                        DataType.Double,
                        "PixelEquivalentX",
                        pixelEquivalentX);
                    inputs[pixelXParam.Guid.ToString()] = pixelXParam;
                    ApplyRuntimeBinding(model, pixelXParam, RuntimeBindingType.PixelEquivalentX);
                }

                if (packet.PixelEquivalentY.HasValue || usePixelFallback)
                {
                    double pixelEquivalentY = packet.PixelEquivalentY ?? 1d;
                    TransmitParam pixelYParam = CreateRuntimeInputParam(
                        snapshot.Scheme?.InputPixelEquivalentYBinding,
                        DataType.Double,
                        "PixelEquivalentY",
                        pixelEquivalentY);
                    inputs[pixelYParam.Guid.ToString()] = pixelYParam;
                    ApplyRuntimeBinding(model, pixelYParam, RuntimeBindingType.PixelEquivalentY);
                }

                if (packet.EdgeCalibrationX.HasValue)
                {
                    TransmitParam edgeCalibrationParam = CreateRuntimeInputParam(
                        snapshot.Scheme?.InputEdgeCalibrationXBinding,
                        DataType.Double,
                        "EdgeCalibrationX",
                        packet.EdgeCalibrationX.Value);
                    inputs[edgeCalibrationParam.Guid.ToString()] = edgeCalibrationParam;
                    ApplyRuntimeBinding(model, edgeCalibrationParam, RuntimeBindingType.EdgeCalibrationX);
                }

                return true;
            }
            catch (Exception ex)
            {
                Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverview] Create runtime DefectPostProcess model failed: {ex.Message}");
                try
                {
                    model?.Dispose();
                }
                catch
                {
                }

                model = null;
                return false;
            }
        }

        private static string ResolveCalibrationFilePath(string schemePath, string calibrationFilePath)
        {
            if (string.IsNullOrWhiteSpace(calibrationFilePath))
                return string.Empty;

            if (Path.IsPathRooted(calibrationFilePath))
                return calibrationFilePath;

            if (string.IsNullOrWhiteSpace(schemePath))
                return calibrationFilePath;

            string baseDirectory = Path.GetDirectoryName(schemePath) ?? string.Empty;
            return string.IsNullOrWhiteSpace(baseDirectory)
                ? calibrationFilePath
                : Path.GetFullPath(Path.Combine(baseDirectory, calibrationFilePath));
        }

        private static bool ShouldInjectPixelFallback(DefectPostProcessScheme scheme, string calibrationFilePath)
        {
            if (scheme == null)
                return false;

            if (!string.IsNullOrWhiteSpace(calibrationFilePath) && File.Exists(calibrationFilePath))
                return false;

            return !HasEnabledActualUnitRule(scheme);
        }

        private static bool HasEnabledActualUnitRule(DefectPostProcessScheme scheme)
        {
            if (scheme?.DefectRuleConfigs == null)
                return false;

            foreach (DefectRuleConfig rule in scheme.DefectRuleConfigs.Where(item => item != null))
            {
                if (rule.FeatureThresholds == null)
                    continue;

                foreach (FeatureThresholdItem threshold in rule.FeatureThresholds.Where(item => item?.IsEnabled == true))
                {
                    string unit = threshold.Unit?.Trim() ?? string.Empty;
                    if (string.Equals(unit, "mm", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(unit, "mm^2", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(unit, "mm2", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void ApplyRuntimeBinding(DefectPostProcessModel model, TransmitParam param, RuntimeBindingType bindingType)
        {
            DefectPostProcessInputBinding binding = new DefectPostProcessInputBinding
            {
                IsLink = true,
                LinkGuid = param.LinkGuid,
                Serial = param.Serial,
                ParentNode = param.ParentNode,
                Guid = param.Guid,
                Resourece = ResoureceType.Inupt,
                Name = param.Name ?? string.Empty,
                ParamName = param.ParamName ?? string.Empty,
                Type = param.Type,
                Describe = param.Describe ?? string.Empty,
                IsGlobal = false,
                ResourcePath = param.ResourcePath ?? string.Empty
            };

            switch (bindingType)
            {
                case RuntimeBindingType.Image:
                    model.InputImageBinding = binding;
                    model.InputImageGuid = param.Guid;
                    model.InputImageName = param.Name ?? string.Empty;
                    break;
                case RuntimeBindingType.Results:
                    model.InputResultsBinding = binding;
                    model.InputResultsGuid = param.Guid;
                    model.InputResultsName = param.Name ?? string.Empty;
                    break;
                case RuntimeBindingType.PixelEquivalentX:
                    model.InputPixelEquivalentXBinding = binding;
                    model.InputPixelEquivalentXGuid = param.Guid;
                    model.InputPixelEquivalentXName = param.Name ?? string.Empty;
                    break;
                case RuntimeBindingType.PixelEquivalentY:
                    model.InputPixelEquivalentYBinding = binding;
                    model.InputPixelEquivalentYGuid = param.Guid;
                    model.InputPixelEquivalentYName = param.Name ?? string.Empty;
                    break;
                case RuntimeBindingType.EdgeCalibrationX:
                    model.InputEdgeCalibrationXBinding = binding;
                    model.InputEdgeCalibrationXGuid = param.Guid;
                    model.InputEdgeCalibrationXName = param.Name ?? string.Empty;
                    break;
            }
        }

        private static TransmitParam CreateRuntimeInputParam(
            DefectPostProcessInputBinding binding,
            DataType dataType,
            string fallbackName,
            object value)
        {
            Guid guid = binding?.Guid != Guid.Empty ? binding.Guid : Guid.NewGuid();
            string name = string.IsNullOrWhiteSpace(binding?.Name) ? fallbackName : binding.Name;
            string paramName = string.IsNullOrWhiteSpace(binding?.ParamName) ? fallbackName : binding.ParamName;

            return new TransmitParam
            {
                IsLink = true,
                LinkGuid = binding?.LinkGuid ?? guid,
                Serial = -1,
                ParentNode = "DefectOverviewRuntime",
                Guid = guid,
                Resourece = ResoureceType.Inupt,
                Name = name,
                ParamName = paramName,
                Type = dataType,
                Value = value,
                Describe = binding?.Describe ?? string.Empty,
                IsGlobal = false,
                ResourcePath = binding?.ResourcePath ?? string.Empty
            };
        }

        private static JsonSerializerSettings CreateJsonSettings()
        {
            return new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                TypeNameHandling = TypeNameHandling.Auto,
                Error = (_, args) =>
                {
                    args.ErrorContext.Handled = true;
                }
            };
        }

        private static List<DefectRuleConfig> CloneRuleConfigs(IEnumerable<DefectRuleConfig> rules)
        {
            return rules?
                .Where(item => item != null)
                .Select(item => new DefectRuleConfig
                {
                    RuleKey = item.RuleKey ?? string.Empty,
                    ClassId = item.ClassId,
                    ClassName = item.ClassName ?? string.Empty,
                    MinimumConfidence = item.MinimumConfidence,
                    IsNmsEnabled = item.IsNmsEnabled,
                    NmsIoUThreshold = item.NmsIoUThreshold,
                    FeatureThresholds = new ObservableCollection<FeatureThresholdItem>(
                        item.FeatureThresholds?
                            .Where(threshold => threshold != null)
                            .Select(threshold => new FeatureThresholdItem
                            {
                                IsEnabled = threshold.IsEnabled,
                                FeatureKey = threshold.FeatureKey ?? string.Empty,
                                FeatureName = threshold.FeatureName ?? string.Empty,
                                MinimumValue = threshold.MinimumValue ?? string.Empty,
                                MaximumValue = threshold.MaximumValue ?? string.Empty,
                                Unit = threshold.Unit ?? string.Empty,
                                RelationOperator = threshold.RelationOperator ?? string.Empty,
                                CanEditRelation = threshold.CanEditRelation
                            })
                        ?? Enumerable.Empty<FeatureThresholdItem>())
                })
                .ToList() ?? new List<DefectRuleConfig>();
        }

        private static DefectPostProcessInputBinding CloneBinding(DefectPostProcessInputBinding binding)
        {
            return binding == null
                ? new DefectPostProcessInputBinding()
                : new DefectPostProcessInputBinding
                {
                    IsLink = binding.IsLink,
                    LinkGuid = binding.LinkGuid,
                    Serial = binding.Serial,
                    ParentNode = binding.ParentNode ?? string.Empty,
                    Guid = binding.Guid,
                    Resourece = binding.Resourece,
                    Name = binding.Name ?? string.Empty,
                    ParamName = binding.ParamName ?? string.Empty,
                    Type = binding.Type,
                    Describe = binding.Describe ?? string.Empty,
                    IsGlobal = binding.IsGlobal,
                    ResourcePath = binding.ResourcePath ?? string.Empty
                };
        }

        private static DefectPostProcessScheme CloneScheme(DefectPostProcessScheme scheme)
        {
            if (scheme == null)
                return null;

            return new DefectPostProcessScheme
            {
                Name = scheme.Name ?? string.Empty,
                UpdatedTime = scheme.UpdatedTime,
                SelectedRuleKey = scheme.SelectedRuleKey ?? string.Empty,
                DefectRuleConfigs = CloneRuleConfigs(scheme.DefectRuleConfigs),
                CalibrationFilePath = scheme.CalibrationFilePath ?? string.Empty,
                InputImageBinding = CloneBinding(scheme.InputImageBinding),
                InputResultsBinding = CloneBinding(scheme.InputResultsBinding),
                InputPixelEquivalentXBinding = CloneBinding(scheme.InputPixelEquivalentXBinding),
                InputPixelEquivalentYBinding = CloneBinding(scheme.InputPixelEquivalentYBinding),
                InputEdgeCalibrationXBinding = CloneBinding(scheme.InputEdgeCalibrationXBinding)
            };
        }

        private sealed class RuntimeSchemeSnapshot
        {
            public string SourceFilePath { get; init; } = string.Empty;

            public DateTime LastWriteUtc { get; init; }

            public string CurrentSchemeName { get; init; } = string.Empty;

            public DefectPostProcessScheme Scheme { get; init; }
        }

        private enum RuntimeBindingType
        {
            Image,
            Results,
            PixelEquivalentX,
            PixelEquivalentY,
            EdgeCalibrationX
        }
    }
}
