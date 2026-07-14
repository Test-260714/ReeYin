using ReeYin_V.Core.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReeYin_V.Core.Calibration
{
    public static class CalibrationFilePathResolver
    {
        public static string ResolveSingleCameraOutputFile(
            string outputPath,
            string cameraId,
            eCalibrationBoardType boardType)
        {
            return ResolveOutputFile(outputPath, GetSingleCameraDefaultFileName(cameraId, boardType));
        }

        public static string ResolveMultiCameraOutputFile(
            string outputPath,
            IEnumerable<string> cameraIds,
            eCalibrationBoardType boardType)
        {
            return ResolveOutputFile(outputPath, GetMultiCameraDefaultFileName(cameraIds, boardType));
        }

        public static string GetSingleCameraDefaultFileName(string cameraId, eCalibrationBoardType boardType)
        {
            return $"camera_{SanitizeFileNameSegment(cameraId, "camera")}_{GetBoardTypeSegment(boardType)}.yaml";
        }

        public static string GetMultiCameraDefaultFileName(IEnumerable<string> cameraIds, eCalibrationBoardType boardType)
        {
            if (cameraIds == null)
            {
                throw new ArgumentNullException(nameof(cameraIds));
            }

            string joinedCameraIds = string.Join(
                "_",
                cameraIds
                    .Where(cameraId => !string.IsNullOrWhiteSpace(cameraId))
                    .Select(cameraId => SanitizeFileNameSegment(cameraId, "camera"))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(cameraId => cameraId, StringComparer.Ordinal));

            if (string.IsNullOrWhiteSpace(joinedCameraIds))
            {
                joinedCameraIds = "cameras";
            }

            return $"multi_camera_{joinedCameraIds}_{GetBoardTypeSegment(boardType)}.yaml";
        }

        public static bool IsYamlFilePath(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveOutputFile(string outputPath, string defaultFileName)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException("标定文件导出目录为空。");
            }

            if (IsYamlFilePath(outputPath))
            {
                return outputPath;
            }

            return Path.Combine(outputPath, defaultFileName);
        }

        private static string GetBoardTypeSegment(eCalibrationBoardType boardType)
        {
            return boardType switch
            {
                eCalibrationBoardType.像素比 => "PixelRatio",
                eCalibrationBoardType.棋盘格 => "Chessboard",
                eCalibrationBoardType.Charuco => "Charuco",
                _ => boardType.ToString()
            };
        }

        private static string SanitizeFileNameSegment(string value, string fallback)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = new string((value ?? string.Empty)
                .Trim()
                .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
                .ToArray());

            return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }
    }
}
