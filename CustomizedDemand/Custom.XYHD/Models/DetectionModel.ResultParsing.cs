using HalconDotNet;
using Custom.XYHD.Services;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Custom.XYHD.Models
{
    public partial class DetectionModel
    {

        /// <summary>
        /// 解析结构体 —— 每条路径的解析结果
        /// </summary>
        private struct PathResult
        {
            public string pathName;           
            public int sourceSerial;       
            public List<bool> isOks;
            public bool isOksExplicit;
            public List<ReeYin_V.Core.DeepLearning.Result> results;
            public HImage pathImage;
            public string pathImageSource;
        }

        private static bool IsPathNg(PathResult path)
        {
            if ((path.results?.Count ?? 0) > 0)
                return true;

            if (path.isOks?.Any(ok => !ok) == true)
                return true;

            return false;
        }

        private void AddEmptyPathResult(string pathName, int serial, List<PathResult> paths, string reason)
        {
            if (paths == null)
                return;

            string effectivePathName = string.IsNullOrWhiteSpace(pathName)
                ? ResolvePathName(null, serial)
                : pathName;

            paths.Add(new PathResult
            {
                pathName = effectivePathName,
                sourceSerial = serial,
                isOks = new List<bool>(),
                isOksExplicit = false,
                results = new List<Result>()
            });

            AddLog($"[Parse] {effectivePathName}路{reason}，按空结果发布占位: Serial={serial}", "WARN");
        }

        private static int ResolveExplicitPathSerial(int fixedSerial, TransmitParam resultInput)
        {
            if (resultInput?.Serial >= 0)
                return resultInput.Serial;

            if (fixedSerial > 0)
                return fixedSerial;

            return -1;
        }

        /// <summary>
        /// 只解析用户显式绑定的左右路结果，不再扫描运行时缓存，也不按节点序号猜左右路。
        /// 支持两种数据格式：
        ///   A) 后处理节点：ResultsByImage / List&lt;List&lt;Result&gt;&gt;
        ///   B) 后处理节点：Results / List&lt;Result&gt;
        /// </summary>
        private List<PathResult> ParseAllDetectResults()
        {
            var paths = new List<PathResult>();

            TryAddExplicitPathResult(
                "Left",
                PathNameFromRole("Left"),
                LeftInputResults,
                DefaultResultsByImageInputName,
                DefaultLeftResultsSerial,
                LeftInputImage,
                DefaultLeftImageInputName,
                !HasUserConfiguredName(LeftInputResultsName, DefaultLeftResultsInputDisplayName),
                paths);

            TryAddExplicitPathResult(
                "Right",
                PathNameFromRole("Right"),
                RightInputResults,
                DefaultResultsByImageInputName,
                DefaultRightResultsSerial,
                RightInputImage,
                DefaultRightImageInputName,
                !HasUserConfiguredName(RightInputResultsName, DefaultRightResultsInputDisplayName),
                paths);

            NormalizeParsedPathResults(paths);
            return paths;
        }

        private void TryAddExplicitPathResult(
            string role,
            string pathName,
            TransmitParam resultInput,
            string resultInputName,
            int fixedSerial,
            TransmitParam targetImageInput,
            string targetImageInputName,
            bool allowFixedFallback,
            List<PathResult> paths)
        {
            if (fixedSerial <= 0 && string.IsNullOrWhiteSpace(resultInput?.Name) && string.IsNullOrWhiteSpace(resultInputName))
            {
                AddLog($"[Parse] {pathName}路结果源未选择，跳过", "WARN");
                return;
            }

            int effectiveFixedSerial = allowFixedFallback ? fixedSerial : -1;
            var fixedInput = allowFixedFallback ? FindInputBySerial(fixedSerial, resultInputName) : null;
            string fallbackResultName = allowFixedFallback ? resultInputName : null;
            object value = GetSelectedInputValue(
                resultInput,
                fallbackResultName,
                effectiveFixedSerial,
                out string valueSource,
                out TransmitParam matchedInput);
            if (value == null)
            {
                AddEmptyPathResult(
                    pathName,
                    ResolveExplicitPathSerial(effectiveFixedSerial, resultInput),
                    paths,
                    $"结果源无有效值: {DescribeFixedSerialInput(fixedInput, effectiveFixedSerial, resultInputName, resultInput)}");
                return;
            }

            int beforeCount = paths.Count;
            string name = EffectiveName(matchedInput?.Name ?? matchedInput?.ParamName, resultInput?.Name ?? resultInput?.ParamName);
            name = EffectiveName(name, resultInputName ?? role);
            int serial = matchedInput?.Serial >= 0
                ? matchedInput.Serial
                : resultInput?.Serial >= 0
                    ? resultInput.Serial
                    : effectiveFixedSerial;
            object resultSourceImages = TryGetResultSourceInputImages(serial, out string resultSourceImageSource);
            AddLog($"[Parse] ExplicitInput Path={pathName}, Select={DescribeInput(resultInput, resultInputName)}, Hit={valueSource ?? "-"}, Serial={serial}, Name={name}, SourceImage={resultSourceImageSource ?? "-"}", "DEBUG");
            TryAddPathResult(
                name,
                serial,
                value,
                paths,
                pathName,
                targetImageInput,
                targetImageInputName,
                resultSourceImages,
                resultSourceImageSource);

            if (paths.Count == beforeCount)
            {
                AddEmptyPathResult(
                    pathName,
                    serial,
                    paths,
                    $"结果源无法解析为缺陷结果: {DescribeFixedSerialInput(fixedInput, effectiveFixedSerial, resultInputName, resultInput)}");
            }
        }

        private void NormalizeParsedPathResults(List<PathResult> paths)
        {
            if (paths == null || paths.Count == 0)
                return;

            var bestByRole = new Dictionary<string, PathResult>(StringComparer.OrdinalIgnoreCase);
            var unknownPaths = new List<PathResult>();

            foreach (var path in paths)
            {
                string role = ResolvePathRoleKey(path.pathName);

                if (string.IsNullOrWhiteSpace(role))
                {
                    unknownPaths.Add(path);
                    continue;
                }

                if (!bestByRole.TryGetValue(role, out var existing))
                {
                    bestByRole[role] = path;
                    continue;
                }

                if (TryMergePathResult(ref existing, path))
                {
                    AddLog($"[Parse] 合并同路结果: Path={existing.pathName}, Serial={existing.sourceSerial}, IsOks={existing.isOks?.Count ?? 0}, Defects={existing.results?.Count ?? 0}", "DEBUG");
                    bestByRole[role] = existing;
                }
                else if (IsBetterPathResult(path, existing))
                {
                    AddLog($"[Parse] 跳过重复路径结果: Path={existing.pathName}, Serial={existing.sourceSerial}, 缺陷={existing.results?.Count ?? 0}", "DEBUG");
                    bestByRole[role] = path;
                }
                else
                {
                    AddLog($"[Parse] 跳过重复路径结果: Path={path.pathName}, Serial={path.sourceSerial}, 缺陷={path.results?.Count ?? 0}", "DEBUG");
                }
            }

            paths.Clear();
            if (bestByRole.TryGetValue("Left", out var left))
                paths.Add(left);
            if (bestByRole.TryGetValue("Right", out var right))
                paths.Add(right);
            paths.AddRange(unknownPaths);
        }

        private static bool TryMergePathResult(ref PathResult existing, PathResult incoming)
        {
            bool sameSerial = existing.sourceSerial > 0
                && incoming.sourceSerial > 0
                && existing.sourceSerial == incoming.sourceSerial;
            bool samePathName = NameMatch(existing.pathName, incoming.pathName);
            if (!sameSerial && !samePathName)
                return false;

            var mergedIsOks = existing.isOks ?? new List<bool>();
            var mergedResults = existing.results ?? new List<Result>();

            bool useIncomingIsOks = ShouldUseIncomingIsOks(existing, incoming);
            if (useIncomingIsOks)
                mergedIsOks = new List<bool>(incoming.isOks);

            if (incoming.results != null && incoming.results.Count > 0)
            {
                if (mergedResults.Count == 0)
                {
                    mergedResults = new List<Result>(incoming.results.Where(item => item != null));
                }
                else
                {
                    mergedResults.AddRange(incoming.results.Where(item => item != null));
                }
            }

            existing.isOks = mergedIsOks;
            if (useIncomingIsOks)
                existing.isOksExplicit = incoming.isOksExplicit;
            else
                existing.isOksExplicit = existing.isOksExplicit || (existing.isOks?.Count == 0 && incoming.isOksExplicit);
            existing.results = mergedResults;
            if (existing.sourceSerial <= 0 && incoming.sourceSerial > 0)
                existing.sourceSerial = incoming.sourceSerial;
            if (string.IsNullOrWhiteSpace(existing.pathName) && !string.IsNullOrWhiteSpace(incoming.pathName))
                existing.pathName = incoming.pathName;
            if ((existing.pathImage == null || !existing.pathImage.IsInitialized())
                && incoming.pathImage != null
                && incoming.pathImage.IsInitialized())
            {
                existing.pathImage = incoming.pathImage;
                existing.pathImageSource = incoming.pathImageSource;
            }

            return true;
        }

        private static bool ShouldUseIncomingIsOks(PathResult existing, PathResult incoming)
        {
            int incomingCount = incoming.isOks?.Count ?? 0;
            if (incomingCount <= 0)
                return false;

            int existingCount = existing.isOks?.Count ?? 0;
            if (existingCount == 0)
                return true;

            if (!existing.isOksExplicit && incoming.isOksExplicit)
                return true;

            if (existing.isOksExplicit == incoming.isOksExplicit && incomingCount > existingCount)
                return true;

            return false;
        }

        private static bool IsBetterPathResult(PathResult candidate, PathResult existing)
        {
            int candidatePieceCount = candidate.isOks?.Count ?? 0;
            int existingPieceCount = existing.isOks?.Count ?? 0;
            if (candidate.isOksExplicit != existing.isOksExplicit && candidatePieceCount > 0 && existingPieceCount > 0)
                return candidate.isOksExplicit;

            if (candidatePieceCount != existingPieceCount)
                return candidatePieceCount > existingPieceCount;

            int candidateCount = candidate.results?.Count ?? 0;
            int existingCount = existing.results?.Count ?? 0;
            if (candidateCount != existingCount)
                return candidateCount > existingCount;

            return false;
        }

        private static string ResolvePathRoleKey(string pathName)
        {
            if (IsLeftPathText(pathName))
                return "Left";

            if (IsRightPathText(pathName))
                return "Right";

            return null;
        }

        private static string PathNameFromRole(string role)
        {
            if (string.Equals(role, "Left", StringComparison.OrdinalIgnoreCase))
                return "左";

            if (string.Equals(role, "Right", StringComparison.OrdinalIgnoreCase))
                return "右";

            return role ?? string.Empty;
        }

        /// <summary>
        /// 尝试将一个参数值解析为检测路径结果并加入列表
        /// </summary>
        private void TryAddPathResult(
            string name,
            int serial,
            object value,
            List<PathResult> paths,
            string explicitPathName = null,
            TransmitParam targetImageInput = null,
            string targetImageInputName = null,
            object preferredTargetImages = null,
            string preferredTargetSource = null)
        {
            // ============ 方式A：ResultsByImage / List<List<Result>> ============
            if (TryExtractGroupedResults(value, out var listListResult))
            {
                var isOks = new List<bool>();
                var results = new List<Result>();
                string pathName = string.IsNullOrWhiteSpace(explicitPathName)
                    ? ResolvePathName(name, serial)
                    : explicitPathName;

                for (int imageIndex = 0; imageIndex < listListResult.Count; imageIndex++)
                {
                    var innerList = listListResult[imageIndex];
                    bool imageIsOk = true;
                    foreach (var r in innerList ?? new List<Result>())
                    {
                        if (r == null)
                            continue;

                        TagResultImageIndex(r, imageIndex);
                        results.Add(r);
                        bool explicitIsOk = false;
                        bool hasExplicitOk = r.Others != null
                            && r.Others.TryGetValue("IsOK", out var isOkVal)
                            && isOkVal is bool
                            && (explicitIsOk = (bool)isOkVal);
                        if (!hasExplicitOk || !explicitIsOk)
                        {
                            imageIsOk = false;
                        }
                    }
                    isOks.Add(imageIsOk);
                }

                AttachDisplayTargetMetadata(
                    pathName,
                    serial,
                    results,
                    targetImageInput,
                    targetImageInputName,
                    preferredTargetImages,
                    preferredTargetSource);

                int pathImageIndex = ResolveFirstResultImageIndex(results, 0);
                HImage pathImage = TryExtractHImageAt(preferredTargetImages, pathImageIndex);
                string pathImageSource = pathImage != null && pathImage.IsInitialized()
                    ? $"{preferredTargetSource ?? "ResultSource.InputImage"}[{pathImageIndex}]"
                    : null;
                paths.Add(new PathResult
                {
                    pathName = pathName,
                    sourceSerial = serial,
                    isOks = isOks,
                    isOksExplicit = true,
                    results = results,
                    pathImage = pathImage,
                    pathImageSource = pathImageSource
                });

                AddLog($"[Parse] ✓ 路径 [{name}] (Serial={serial}): " +
                       $"图数={listListResult.Count}, 缺陷={results.Count}项, " +
                       $"NG图数={isOks.Count(o => !o)}", "DEBUG");
                return;
            }

            // ============ 方式B：后处理节点 Results / List<Result> ============
            if (TryExtractFlatResults(value, out var flatResults))
            {
                string pathName = string.IsNullOrWhiteSpace(explicitPathName)
                    ? ResolvePathName(name, serial)
                    : explicitPathName;
                AttachDisplayTargetMetadata(
                    pathName,
                    serial,
                    flatResults,
                    targetImageInput,
                    targetImageInputName,
                    preferredTargetImages,
                    preferredTargetSource);

                int pathImageIndex = ResolveFirstResultImageIndex(flatResults, 0);
                HImage pathImage = TryExtractHImageAt(preferredTargetImages, pathImageIndex);
                string pathImageSource = pathImage != null && pathImage.IsInitialized()
                    ? $"{preferredTargetSource ?? "ResultSource.InputImage"}[{pathImageIndex}]"
                    : null;
                paths.Add(new PathResult
                {
                    pathName = pathName,
                    sourceSerial = serial,
                    isOks = new List<bool>(),
                    isOksExplicit = false,
                    results = flatResults,
                    pathImage = pathImage,
                    pathImageSource = pathImageSource
                });

                AddLog($"[Parse] ✓ 路径 [{pathName}] (Serial={serial}, Name={name}): " +
                       $"后处理结果={flatResults.Count}项", "DEBUG");
                return;
            }

            AddLog($"[Parse] 跳过 [{name}] (Serial={serial}): 类型={value.GetType().Name}，无法识别", "DEBUG");
        }

        private static bool TryExtractGroupedResults(object value, out List<List<Result>> groups)
        {
            groups = new List<List<Result>>();
            if (value == null || value is string || value is IEnumerable<Result>)
                return false;

            if (value is IEnumerable<IEnumerable<Result>> typedGroups)
            {
                groups = typedGroups
                    .Select(group => group?.Where(item => item != null).ToList() ?? new List<Result>())
                    .ToList();
                return true;
            }

            if (value is not IEnumerable enumerable)
                return false;

            bool sawGroup = false;
            foreach (object item in enumerable)
            {
                if (item == null)
                {
                    groups.Add(new List<Result>());
                    sawGroup = true;
                    continue;
                }

                if (item is Result || item is string || item is not IEnumerable inner)
                {
                    groups.Clear();
                    return false;
                }

                var group = new List<Result>();
                foreach (object innerItem in inner)
                {
                    if (innerItem == null)
                        continue;

                    if (innerItem is Result result)
                    {
                        group.Add(result);
                        continue;
                    }

                    groups.Clear();
                    return false;
                }

                groups.Add(group);
                sawGroup = true;
            }

            return sawGroup;
        }

        private static bool TryExtractFlatResults(object value, out List<Result> results)
        {
            results = new List<Result>();
            if (value == null || value is string)
                return false;

            if (value is IEnumerable<Result> resultEnumerable)
            {
                results = resultEnumerable
                    .Where(item => item != null)
                    .ToList();
                return true;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is Result result)
                    {
                        results.Add(result);
                    }
                }

                return results.Count > 0;
            }

            return false;
        }
    }
}
