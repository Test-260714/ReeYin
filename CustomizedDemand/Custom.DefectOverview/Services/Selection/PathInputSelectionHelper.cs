using Custom.DefectOverview.Models;
using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Custom.DefectOverview.Services
{
    public static class PathInputSelectionHelper
    {
        public static bool IsImageParam(TransmitParam param)
        {
            if (param == null)
                return false;

            if (param.Value is HImage || param.Value is HObject)
                return true;

            if (param.Type == DataType.HObject || param.Type == DataType.Mat)
                return true;

            string text = BuildPathText(param);
            return ContainsAny(text, "PathImage", "Image", "Crop", "Cropped", "LeftImage", "RightImage")
                || ContainsAny(text, "图像", "原图", "路径图", "裁剪", "左图", "右图");
        }

        public static bool IsResultParam(TransmitParam param)
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

            string text = BuildPathText(param);
            return ContainsAny(text, "Results", "DefectResults")
                || ContainsAny(text, "缺陷结果", "结果");
        }

        public static TransmitParam MatchInputParam(TransmitParam current, IEnumerable<TransmitParam> candidates)
        {
            List<TransmitParam> list = candidates?.Where(item => item != null).ToList() ?? new List<TransmitParam>();
            if (list.Count == 0)
                return current ?? new TransmitParam();

            if (current == null)
                return new TransmitParam();

            TransmitParam matched = list.FirstOrDefault(item => HasSameGuid(item, current));
            matched ??= list.FirstOrDefault(item => HasSharedIdentityWithSameSerial(item, current));
            matched ??= list.FirstOrDefault(item =>
                !ReferenceEquals(item, current)
                && IsUsableCandidate(item)
                && HasSharedParamIdentity(item, current));
            matched ??= list.FirstOrDefault(item =>
                IsUsableCandidate(item)
                && HasSharedParamIdentity(item, current));

            if (matched != null)
                return matched;

            return string.IsNullOrWhiteSpace(current.Name) && string.IsNullOrWhiteSpace(current.ParamName)
                ? new TransmitParam()
                : current;
        }

        public static TransmitParam MatchPathInputParam(
            TransmitParam current,
            IEnumerable<TransmitParam> candidates,
            DefectOverviewPathRole pathRole)
        {
            List<TransmitParam> list = candidates?.Where(item => item != null).ToList() ?? new List<TransmitParam>();
            TransmitParam matched = MatchInputParam(current, list);
            if (pathRole == DefectOverviewPathRole.Unknown || list.Count == 0)
                return matched;

            TransmitParam preferred = list
                .Where(item => IsPathCandidate(item, pathRole))
                .OrderByDescending(item => GetPathCandidateScore(item, pathRole))
                .ThenBy(item => item.Serial)
                .ThenBy(item => item.Name)
                .ThenBy(item => item.ParamName)
                .FirstOrDefault();
            if (preferred == null)
                return matched;

            if (!HasConfiguredInputSelection(current))
                return preferred;

            if (IsOppositePathCandidate(matched, pathRole))
                return preferred;

            return matched;
        }

        public static bool HasConfiguredInputSelection(TransmitParam param)
        {
            return param != null
                && (param.IsLink
                    || !string.IsNullOrWhiteSpace(param.Name)
                    || !string.IsNullOrWhiteSpace(param.ParamName)
                    || param.Value != null);
        }

        public static bool IsPathCandidate(TransmitParam param, DefectOverviewPathRole pathRole)
        {
            string text = BuildPathText(param);
            return pathRole switch
            {
                DefectOverviewPathRole.Left => IsLeftPathText(text),
                DefectOverviewPathRole.Right => IsRightPathText(text),
                _ => false
            };
        }

        public static bool IsOppositePathCandidate(TransmitParam param, DefectOverviewPathRole pathRole)
        {
            string text = BuildPathText(param);
            return pathRole switch
            {
                DefectOverviewPathRole.Left => IsRightPathText(text),
                DefectOverviewPathRole.Right => IsLeftPathText(text),
                _ => false
            };
        }

        public static bool IsLeftPathText(string text)
        {
            return ContainsAny(text, "left", "path1", "lane1")
                || ContainsAny(text, "左", "左路", "裁剪左");
        }

        public static bool IsRightPathText(string text)
        {
            return ContainsAny(text, "right", "path2", "lane2")
                || ContainsAny(text, "右", "右路", "裁剪右");
        }

        public static bool ShouldReplaceCandidate(TransmitParam existing, TransmitParam incoming)
        {
            if (existing == null || incoming == null)
                return incoming != null;

            if (string.IsNullOrWhiteSpace(existing.ParentNode) && !string.IsNullOrWhiteSpace(incoming.ParentNode))
                return true;

            if (string.IsNullOrWhiteSpace(existing.ResourcePath) && !string.IsNullOrWhiteSpace(incoming.ResourcePath))
                return true;

            return existing.Value == null && incoming.Value != null;
        }

        private static int GetPathCandidateScore(TransmitParam param, DefectOverviewPathRole pathRole)
        {
            string text = BuildPathText(param);
            int score = 0;
            if (pathRole == DefectOverviewPathRole.Left && IsLeftPathText(text))
                score += 100;
            if (pathRole == DefectOverviewPathRole.Right && IsRightPathText(text))
                score += 100;
            if (ContainsAny(text, "裁剪", "crop", "cropped"))
                score += 30;
            if (ContainsAny(text, "PathImage", "路径图", "图像", "image"))
                score += 20;
            if (param.IsLink)
                score += 6;
            if (param.Serial >= 0)
                score += 4;
            if (param.Value != null)
                score += 2;
            return score;
        }

        private static string BuildPathText(TransmitParam param)
        {
            if (param == null)
                return string.Empty;

            return $"{param.Name} {param.ParamName} {param.ParentNode} {param.ResourcePath}";
        }

        private static bool ContainsAny(string text, params string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(text) || tokens == null)
                return false;

            return tokens.Any(token =>
                !string.IsNullOrWhiteSpace(token)
                && text.Contains(token, StringComparison.OrdinalIgnoreCase));
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
    }
}
