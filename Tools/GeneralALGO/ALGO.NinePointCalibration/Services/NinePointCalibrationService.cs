using ALGO.NinePointCalibration.Models;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ALGO.NinePointCalibration.Services
{
    public class NinePointCalibrationService
    {
        private const int CalibrationPointCount = 9;
        private const double Epsilon = 1E-9d;

        public NinePointCalibrationResult Calculate(IList<NinePointCalibrationPoint> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            List<NinePointCalibrationPoint> validPoints = points
                .Where(item => item != null && item.IsUsed)
                .OrderBy(item => item.Index)
                .ToList();

            ValidatePoints(validPoints);

            HTuple pixelX = new HTuple(validPoints.Select(item => item.PixelX).ToArray());
            HTuple pixelY = new HTuple(validPoints.Select(item => item.PixelY).ToArray());
            HTuple machineX = new HTuple(validPoints.Select(item => item.MachineX).ToArray());
            HTuple machineY = new HTuple(validPoints.Select(item => item.MachineY).ToArray());

            HOperatorSet.VectorToHomMat2d(pixelX, pixelY, machineX, machineY, out HTuple homMat2D);
            HOperatorSet.HomMat2dInvert(homMat2D, out HTuple inverseHomMat2D);

            double maxError = 0;
            double sumError = 0;
            foreach (NinePointCalibrationPoint point in validPoints)
            {
                Transform(homMat2D, point.PixelX, point.PixelY, out double fitX, out double fitY);
                double error = GetDistance(fitX, fitY, point.MachineX, point.MachineY);

                point.FitMachineX = Round6(fitX);
                point.FitMachineY = Round6(fitY);
                point.Error = Round6(error);

                sumError += error;
                maxError = Math.Max(maxError, error);
            }

            return new NinePointCalibrationResult
            {
                HomMat2D = ToArray(homMat2D),
                InverseHomMat2D = ToArray(inverseHomMat2D),
                AverageError = Round6(sumError / validPoints.Count),
                MaxError = Round6(maxError),
                CalibratedTime = DateTime.Now,
            };
        }

        public bool TryPixelToMachine(double[] homMat2D, double pixelX, double pixelY, out double machineX, out double machineY, out string message)
        {
            machineX = 0;
            machineY = 0;
            message = string.Empty;

            if (!ValidateMatrix(homMat2D, out message))
            {
                return false;
            }

            if (!IsFinite(pixelX) || !IsFinite(pixelY))
            {
                message = "像素坐标存在非法数值。";
                return false;
            }

            try
            {
                Transform(new HTuple(homMat2D), pixelX, pixelY, out machineX, out machineY);
                machineX = Round6(machineX);
                machineY = Round6(machineY);
                return true;
            }
            catch (Exception ex)
            {
                message = $"坐标转换失败：{ex.Message}";
                return false;
            }
        }

        public bool TryMachineToPixel(double[] inverseHomMat2D, double machineX, double machineY, out double pixelX, out double pixelY, out string message)
        {
            pixelX = 0;
            pixelY = 0;
            message = string.Empty;

            if (!ValidateMatrix(inverseHomMat2D, out message))
            {
                return false;
            }

            if (!IsFinite(machineX) || !IsFinite(machineY))
            {
                message = "机械坐标存在非法数值。";
                return false;
            }

            try
            {
                Transform(new HTuple(inverseHomMat2D), machineX, machineY, out pixelX, out pixelY);
                pixelX = Round6(pixelX);
                pixelY = Round6(pixelY);
                return true;
            }
            catch (Exception ex)
            {
                message = $"反向坐标转换失败：{ex.Message}";
                return false;
            }
        }

        private static void ValidatePoints(IReadOnlyList<NinePointCalibrationPoint> points)
        {
            if (points.Count != CalibrationPointCount)
            {
                throw new InvalidOperationException($"九点标定需要启用 {CalibrationPointCount} 个点，当前为 {points.Count} 个。");
            }

            if (points.Any(item => !IsFinite(item.PixelX) || !IsFinite(item.PixelY) || !IsFinite(item.MachineX) || !IsFinite(item.MachineY)))
            {
                throw new InvalidOperationException("标定点存在非法坐标。");
            }

            int uniquePixelCount = points
                .Select(item => $"{Round6(item.PixelX)}_{Round6(item.PixelY)}")
                .Distinct(StringComparer.Ordinal)
                .Count();

            int uniqueMachineCount = points
                .Select(item => $"{Round6(item.MachineX)}_{Round6(item.MachineY)}")
                .Distinct(StringComparer.Ordinal)
                .Count();

            if (uniquePixelCount < 3 || uniqueMachineCount < 3)
            {
                throw new InvalidOperationException("标定点存在重复坐标，无法计算稳定的转换矩阵。");
            }
        }

        private static bool ValidateMatrix(double[] matrix, out string message)
        {
            message = string.Empty;
            if (matrix == null || matrix.Length != 6)
            {
                message = "当前没有可用的 Halcon 仿射矩阵。";
                return false;
            }

            return true;
        }

        private static void Transform(HTuple homMat2D, double sourceX, double sourceY, out double targetX, out double targetY)
        {
            HOperatorSet.AffineTransPoint2d(homMat2D, sourceX, sourceY, out HTuple x, out HTuple y);
            targetX = x.D;
            targetY = y.D;

            if (!IsFinite(targetX) || !IsFinite(targetY))
            {
                throw new InvalidOperationException("转换结果不是有效数值。");
            }
        }

        private static double[] ToArray(HTuple tuple)
        {
            double[] result = new double[tuple.Length];
            for (int index = 0; index < tuple.Length; index++)
            {
                result[index] = tuple[index].D;
            }

            return result;
        }

        private static double GetDistance(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static double Round6(double value)
        {
            return Math.Round(value, 6);
        }
    }
}
