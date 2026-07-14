using Custom.DefectOverview.Models;
using Custom.DefectOverview.Services;
using Custom.XYHD.Models;
using ReeYin_V.Core.DeepLearning;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OverviewDefectPreviewFactory = Custom.DefectOverview.Services.DefectPreviewFactory;

namespace Custom.XYHD.Services
{
    internal readonly struct XYHDFieldOrientationSettings
    {
        public bool IsConfigured { get; init; }

        public bool SwapLeftRightPaths { get; init; }

        public bool LeftPathXMirror { get; init; }

        public bool RightPathXMirror { get; init; }

        public static XYHDFieldOrientationSettings FromModel(DetectionModel model)
        {
            if (model == null)
                return default;

            return new XYHDFieldOrientationSettings
            {
                IsConfigured = true,
                SwapLeftRightPaths = model.SwapLeftRightPaths,
                LeftPathXMirror = model.LeftPathXMirror,
                RightPathXMirror = model.RightPathXMirror
            };
        }
    }

    internal readonly struct XYHDFieldOrientationMap
    {
        public string OriginalPathName { get; init; }

        public string FieldPathName { get; init; }

        public DefectOverviewPathRole SourceRole { get; init; }

        public DefectOverviewPathRole FieldRole { get; init; }

        public bool SwapApplied { get; init; }

        public bool MapXMirror { get; init; }
    }

    internal static class XYHDFieldOrientationMapper
    {
        public const string SourcePathNameKey = "XYHD_SourcePathName";
        public const string FieldPathNameKey = "XYHD_FieldPathName";
        public const string SourceRoleKey = "XYHD_SourceRole";
        public const string FieldRoleKey = "XYHD_FieldRole";
        public const string SwapAppliedKey = "XYHD_SwapApplied";
        public const string MapXMirrorAppliedKey = "XYHD_MapXMirrorApplied";
        private const string LegacyDisplayCenterXKey = "XYHD_DisplayCenterX";
        private const string LegacyDisplayCenterYKey = "XYHD_DisplayCenterY";
        private const string LegacyDisplayPixelWidthKey = "XYHD_DisplayPixelWidth";
        private const string LegacyDisplayPixelHeightKey = "XYHD_DisplayPixelHeight";

        public static XYHDFieldOrientationMap Resolve(
            string originalPathName,
            XYHDFieldOrientationSettings settings)
        {
            DefectOverviewPathRole sourceRole = ResolvePathRole(originalPathName);
            DefectOverviewPathRole fieldRole = ResolveFieldRole(sourceRole, settings.SwapLeftRightPaths);
            bool mapXMirror = ResolveMapXMirror(settings, fieldRole);

            return new XYHDFieldOrientationMap
            {
                OriginalPathName = originalPathName,
                FieldPathName = ResolvePathName(fieldRole, originalPathName),
                SourceRole = sourceRole,
                FieldRole = fieldRole,
                SwapApplied = settings.SwapLeftRightPaths && sourceRole != fieldRole,
                MapXMirror = mapXMirror
            };
        }

        public static DefectOverviewPathRole ResolvePathRole(string pathName)
        {
            if (string.IsNullOrWhiteSpace(pathName))
                return DefectOverviewPathRole.Unknown;

            if (pathName.Contains("左", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("left", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("path1", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("lane1", StringComparison.OrdinalIgnoreCase))
            {
                return DefectOverviewPathRole.Left;
            }

            if (pathName.Contains("右", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("right", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("path2", StringComparison.OrdinalIgnoreCase)
                || pathName.Contains("lane2", StringComparison.OrdinalIgnoreCase))
            {
                return DefectOverviewPathRole.Right;
            }

            return DefectOverviewPathRole.Unknown;
        }

        public static string ResolvePathName(DefectOverviewPathRole role, string fallbackPathName = null)
        {
            return role switch
            {
                DefectOverviewPathRole.Left => "左路",
                DefectOverviewPathRole.Right => "右路",
                _ => fallbackPathName ?? string.Empty
            };
        }

        public static List<Result> ApplyToResults(
            List<Result> results,
            double laneWidth,
            XYHDFieldOrientationMap orientation)
        {
            if (results == null || results.Count == 0)
                return results ?? [];

            if (orientation.FieldRole == DefectOverviewPathRole.Unknown)
                return results.Where(result => result != null).ToList();

            return results
                .Where(result => result != null)
                .Select(result => CloneResultWithFieldMapMetadata(result, laneWidth, orientation))
                .ToList();
        }

        public static double ResolveMapCenterX(Result result, double laneWidth)
        {
            if (result == null)
                return 0d;

            if (TryGetResultMetadataDouble(result, OverviewDefectPreviewFactory.MapCenterXKey, out double centerX)
                || TryGetResultMetadataDouble(result, OverviewDefectPreviewFactory.DisplayCenterXKey, out centerX))
            {
                return double.IsFinite(centerX) ? centerX : 0d;
            }

            centerX = result.Cx;

            if (laneWidth > 1d
                && TryGetResultMetadataBool(result, OverviewDefectPreviewFactory.MapXMirrorKey, out bool mapXMirror)
                && mapXMirror)
            {
                centerX = laneWidth - centerX;
            }

            return double.IsFinite(centerX) ? centerX : 0d;
        }

        private static DefectOverviewPathRole ResolveFieldRole(
            DefectOverviewPathRole sourceRole,
            bool swapLeftRightPaths)
        {
            if (!swapLeftRightPaths)
                return sourceRole;

            return sourceRole switch
            {
                DefectOverviewPathRole.Left => DefectOverviewPathRole.Right,
                DefectOverviewPathRole.Right => DefectOverviewPathRole.Left,
                _ => sourceRole
            };
        }

        private static bool ResolveMapXMirror(
            XYHDFieldOrientationSettings settings,
            DefectOverviewPathRole fieldRole)
        {
            return fieldRole switch
            {
                DefectOverviewPathRole.Left => settings.LeftPathXMirror,
                DefectOverviewPathRole.Right => settings.RightPathXMirror,
                _ => false
            };
        }

        private static Result CloneResultWithFieldMapMetadata(
            Result source,
            double laneWidth,
            XYHDFieldOrientationMap orientation)
        {
            Result target = new()
            {
                Cx = source.Cx,
                Cy = source.Cy,
                Width = source.Width,
                Height = source.Height,
                Angle = source.Angle,
                Confidence = source.Confidence,
                ClassId = source.ClassId,
                ClassName = source.ClassName,
                Kpt = source.Kpt,
                Seg = source.Seg,
                ModelType = source.ModelType,
                Others = source.Others != null
                    ? new Dictionary<string, object>(source.Others)
                    : new Dictionary<string, object>()
            };

            double centerX = ResolveSourceMapCenterX(source, laneWidth, orientation.SourceRole);
            if (orientation.MapXMirror && laneWidth > 1d)
                centerX = laneWidth - centerX;

            target.Others[OverviewDefectPreviewFactory.MapCenterXKey] = double.IsFinite(centerX) ? centerX : 0d;
            UpdateDisplayGeometryMetadata(target, laneWidth, orientation);
            target.Others[SourcePathNameKey] = orientation.OriginalPathName ?? string.Empty;
            target.Others[FieldPathNameKey] = orientation.FieldPathName ?? string.Empty;
            target.Others[SourceRoleKey] = orientation.SourceRole.ToString();
            target.Others[FieldRoleKey] = orientation.FieldRole.ToString();
            target.Others[SwapAppliedKey] = orientation.SwapApplied;
            target.Others[MapXMirrorAppliedKey] = orientation.MapXMirror;
            return target;
        }

        private static void UpdateDisplayGeometryMetadata(
            Result target,
            double laneWidth,
            XYHDFieldOrientationMap orientation)
        {
            if (target?.Others == null)
                return;

            double displayCenterX = ResolveSourceDisplayCenterX(target, laneWidth, orientation.SourceRole);
            if (double.IsFinite(displayCenterX))
            {
                target.Cx = (float)displayCenterX;
                target.Others[OverviewDefectPreviewFactory.DisplayCenterXKey] = displayCenterX;
                target.Others[LegacyDisplayCenterXKey] = displayCenterX;
            }

            if (TryGetResultMetadataDouble(target, OverviewDefectPreviewFactory.DisplayCenterYKey, out double displayCenterY)
                || TryGetResultMetadataDouble(target, LegacyDisplayCenterYKey, out displayCenterY)
                || double.IsFinite(target.Cy))
            {
                displayCenterY = double.IsFinite(displayCenterY) ? displayCenterY : target.Cy;
                target.Others[OverviewDefectPreviewFactory.DisplayCenterYKey] = displayCenterY;
                target.Others[LegacyDisplayCenterYKey] = displayCenterY;
            }

            if (TryGetResultMetadataDouble(target, OverviewDefectPreviewFactory.DisplayPixelWidthKey, out double pixelWidth)
                || TryGetResultMetadataDouble(target, LegacyDisplayPixelWidthKey, out pixelWidth)
                || target.Width > 0)
            {
                pixelWidth = pixelWidth > 0 ? pixelWidth : target.Width;
                target.Others[OverviewDefectPreviewFactory.DisplayPixelWidthKey] = pixelWidth;
                target.Others[LegacyDisplayPixelWidthKey] = pixelWidth;
            }

            if (TryGetResultMetadataDouble(target, OverviewDefectPreviewFactory.DisplayPixelHeightKey, out double pixelHeight)
                || TryGetResultMetadataDouble(target, LegacyDisplayPixelHeightKey, out pixelHeight)
                || target.Height > 0)
            {
                pixelHeight = pixelHeight > 0 ? pixelHeight : target.Height;
                target.Others[OverviewDefectPreviewFactory.DisplayPixelHeightKey] = pixelHeight;
                target.Others[LegacyDisplayPixelHeightKey] = pixelHeight;
            }
        }

        private static double ResolveSourceDisplayCenterX(Result result, double laneWidth, DefectOverviewPathRole sourceRole)
        {
            if (result == null)
                return 0d;

            if (TryGetResultMetadataDouble(result, LegacyDisplayCenterXKey, out double centerX)
                || TryGetResultMetadataDouble(result, OverviewDefectPreviewFactory.DisplayCenterXKey, out centerX))
            {
                return NormalizeSourceCenterX(centerX, laneWidth, sourceRole);
            }

            return NormalizeSourceCenterX(result.Cx, laneWidth, sourceRole);
        }

        private static double ResolveSourceMapCenterX(Result result, double laneWidth = 0d, DefectOverviewPathRole sourceRole = DefectOverviewPathRole.Unknown)
        {
            if (result == null)
                return 0d;

            if (TryGetResultMetadataDouble(result, OverviewDefectPreviewFactory.MapCenterXKey, out double centerX)
                || TryGetResultMetadataDouble(result, OverviewDefectPreviewFactory.DisplayCenterXKey, out centerX)
                || TryGetResultMetadataDouble(result, LegacyDisplayCenterXKey, out centerX))
            {
                return NormalizeSourceCenterX(centerX, laneWidth, sourceRole);
            }

            return NormalizeSourceCenterX(result.Cx, laneWidth, sourceRole);
        }

        private static double NormalizeSourceCenterX(double centerX, double laneWidth, DefectOverviewPathRole sourceRole)
        {
            if (!double.IsFinite(centerX))
                return 0d;

            if (sourceRole == DefectOverviewPathRole.Right
                && laneWidth > 1d
                && centerX > laneWidth
                && centerX <= laneWidth * 2d + 1d)
            {
                centerX -= laneWidth;
            }

            if (laneWidth > 1d)
                centerX = Math.Clamp(centerX, 0d, laneWidth);

            return centerX;
        }

        private static bool TryGetResultMetadataDouble(Result result, string key, out double value)
        {
            value = 0d;
            if (result?.Others == null || string.IsNullOrWhiteSpace(key))
                return false;

            if (!result.Others.TryGetValue(key, out object rawValue) || rawValue == null)
                return false;

            try
            {
                value = Convert.ToDouble(rawValue, CultureInfo.InvariantCulture);
                return double.IsFinite(value);
            }
            catch
            {
                value = 0d;
                return false;
            }
        }

        private static bool TryGetResultMetadataBool(Result result, string key, out bool value)
        {
            value = false;
            if (result?.Others == null || string.IsNullOrWhiteSpace(key))
                return false;

            if (!result.Others.TryGetValue(key, out object rawValue) || rawValue == null)
                return false;

            if (rawValue is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            if (rawValue is string text && bool.TryParse(text, out bool parsed))
            {
                value = parsed;
                return true;
            }

            try
            {
                value = Convert.ToDouble(rawValue, CultureInfo.InvariantCulture) != 0d;
                return true;
            }
            catch
            {
                value = false;
                return false;
            }
        }
    }
}
