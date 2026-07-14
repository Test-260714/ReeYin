using HalconDotNet;
using ImageTool.Halcon.Model;
using Newtonsoft.Json;
using ReeYin_V.Core.Calibration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ALGO.MeasureCircle
{
    public partial class MeasureCircleModel
    {
        private bool _enableCalibration;

        /// <summary>
        /// 是否启用标定输出。
        /// </summary>
        public bool EnableCalibration
        {
            get { return _enableCalibration; }
            set
            {
                if (SetProperty(ref _enableCalibration, value))
                {
                    if (_enableCalibration)
                    {
                        InvalidateCalibrationRuntime();
                    }
                    else
                    {
                        DisableCalibration();
                    }
                }
            }
        }

        private string _calibrationFilePath = string.Empty;

        /// <summary>
        /// 标定文件路径。
        /// </summary>
        public string CalibrationFilePath
        {
            get { return _calibrationFilePath; }
            set
            {
                if (SetProperty(ref _calibrationFilePath, value ?? string.Empty))
                {
                    InvalidateCalibrationRuntime();
                }
            }
        }

        [JsonIgnore]
        private bool _isCalibrationEffective;

        /// <summary>
        /// 当前标定运行时是否已生效。
        /// </summary>
        [JsonIgnore]
        public bool IsCalibrationEffective
        {
            get { return _isCalibrationEffective; }
        }

        [JsonIgnore]
        private CameraCalibrationSdk _cameraCalib;

        [JsonIgnore]
        private string _calibrationCameraId = string.Empty;

        /// <summary>
        /// 标定状态文本。
        /// </summary>
        [JsonIgnore]
        public string CalibrationStatusText
        {
            get
            {
                if (!EnableCalibration)
                {
                    return "未启用标定";
                }

                if (IsCalibrationEffective)
                {
                    return string.IsNullOrWhiteSpace(_calibrationCameraId)
                        ? "标定已生效"
                        : $"标定已生效：{_calibrationCameraId}";
                }

                if (string.IsNullOrWhiteSpace(CalibrationFilePath))
                {
                    return "未选择标定文件，输出像素坐标";
                }

                return "标定未生效，输出像素坐标";
            }
        }

        public bool ValidateCalibrationConfig(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!EnableCalibration)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(CalibrationFilePath))
            {
                errorMessage = "启用标定后必须选择标定文件。";
                return false;
            }

            if (!File.Exists(CalibrationFilePath))
            {
                errorMessage = "标定文件不存在，请重新选择有效文件。";
                return false;
            }

            return true;
        }

        private void UpdateOutputCircleCoordinates(ROICircle circle, HTuple rowList, HTuple colList)
        {
            double centerX = circle.CenterX;
            double centerY = circle.CenterY;
            double radius = circle.Radius;

            if (EnableCalibration &&
                EnsureCalibrationRuntimeReady() &&
                TryConvertPointToBoard(circle.CenterX, circle.CenterY, out double boardCenterX, out double boardCenterY) &&
                TryCalculateCalibratedRadius(circle, rowList, colList, boardCenterX, boardCenterY, out double boardRadius))
            {
                centerX = boardCenterX;
                centerY = boardCenterY;
                radius = boardRadius;
                SetCalibrationState(true, _calibrationCameraId);
            }
            else if (!EnableCalibration)
            {
                SetCalibrationState(false);
            }

            OutCircleCenterX = Math.Round(centerX, 4);
            OutCircleCenterY = Math.Round(centerY, 4);
            OutCircleRadius = Math.Round(radius, 4);
        }

        private bool TryCalculateCalibratedRadius(
            ROICircle circle,
            HTuple rowList,
            HTuple colList,
            double boardCenterX,
            double boardCenterY,
            out double boardRadius)
        {
            boardRadius = 0d;

            if (TryBuildWorldPointsFromMeasurementPoints(rowList, colList, out List<(double X, double Y)> worldPoints) &&
                CircleCalibrationMath.TryCalculateWorldRadius(boardCenterX, boardCenterY, worldPoints, out boardRadius))
            {
                return true;
            }

            if (TryBuildWorldPointsFromCircleSamples(circle, out worldPoints) &&
                CircleCalibrationMath.TryCalculateWorldRadius(boardCenterX, boardCenterY, worldPoints, out boardRadius))
            {
                return true;
            }

            return false;
        }

        private bool TryBuildWorldPointsFromMeasurementPoints(HTuple rowList, HTuple colList, out List<(double X, double Y)> worldPoints)
        {
            worldPoints = new List<(double X, double Y)>();
            int count = Math.Min(rowList?.Length ?? 0, colList?.Length ?? 0);

            if (count < 3)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (!TryConvertPointToBoard(colList[i].D, rowList[i].D, out double boardX, out double boardY))
                {
                    return false;
                }

                worldPoints.Add((boardX, boardY));
            }

            return worldPoints.Count >= 3;
        }

        private bool TryBuildWorldPointsFromCircleSamples(ROICircle circle, out List<(double X, double Y)> worldPoints)
        {
            const int sampleCount = 16;
            worldPoints = new List<(double X, double Y)>(sampleCount);

            if (circle.Radius <= 0)
            {
                return false;
            }

            for (int i = 0; i < sampleCount; i++)
            {
                double angle = 2d * Math.PI * i / sampleCount;
                double pixelX = circle.CenterX + circle.Radius * Math.Cos(angle);
                double pixelY = circle.CenterY + circle.Radius * Math.Sin(angle);

                if (!TryConvertPointToBoard(pixelX, pixelY, out double boardX, out double boardY))
                {
                    return false;
                }

                worldPoints.Add((boardX, boardY));
            }

            return worldPoints.Count >= 3;
        }

        private bool EnsureCalibrationRuntimeReady()
        {
            if (!EnableCalibration)
            {
                SetCalibrationState(false);
                return false;
            }

            if (string.IsNullOrWhiteSpace(CalibrationFilePath) || !File.Exists(CalibrationFilePath))
            {
                SetCalibrationState(false);
                return false;
            }

            if (_cameraCalib != null && !string.IsNullOrWhiteSpace(_calibrationCameraId))
            {
                SetCalibrationState(true, _calibrationCameraId);
                return true;
            }

            try
            {
                ReleaseCalibrationRuntime();
                _cameraCalib = new CameraCalibrationSdk();
                _cameraCalib.loadCalibrationFile(CalibrationFilePath);

                CameraCalibrationSdk.CameraParams cameraParams = default;
                _cameraCalib.getCameraParams(ref cameraParams);

                if (string.IsNullOrWhiteSpace(cameraParams.cameraId))
                {
                    ReleaseCalibrationRuntime();
                    SetCalibrationState(false);
                    return false;
                }

                SetCalibrationState(true, cameraParams.cameraId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载标定文件失败：{ex.Message}");
                ReleaseCalibrationRuntime();
                SetCalibrationState(false);
                return false;
            }
        }

        private bool TryConvertPointToBoard(double pixelX, double pixelY, out double boardX, out double boardY)
        {
            boardX = pixelX;
            boardY = pixelY;

            if (_cameraCalib == null || string.IsNullOrWhiteSpace(_calibrationCameraId))
            {
                SetCalibrationState(false);
                return false;
            }

            try
            {
                _cameraCalib.pixelToWorld(_calibrationCameraId, pixelX, pixelY, out boardX, out boardY, out _);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"标定坐标转换失败：{ex.Message}");
                SetCalibrationState(false);
                return false;
            }
        }

        private void DisableCalibration()
        {
            ReleaseCalibrationRuntime();
            SetCalibrationState(false);

            if (!string.IsNullOrWhiteSpace(_calibrationFilePath))
            {
                _calibrationFilePath = string.Empty;
                RaisePropertyChanged(nameof(CalibrationFilePath));
            }
        }

        private void InvalidateCalibrationRuntime()
        {
            ReleaseCalibrationRuntime();
            SetCalibrationState(false);
        }

        private void ReleaseCalibrationRuntime()
        {
            try
            {
                _cameraCalib?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放标定资源失败：{ex.Message}");
            }
            finally
            {
                _cameraCalib = null;
                _calibrationCameraId = string.Empty;
            }
        }

        private void SetCalibrationState(bool isEffective, string cameraId = "")
        {
            cameraId ??= string.Empty;

            bool effectiveChanged = _isCalibrationEffective != isEffective;
            bool cameraChanged = !string.Equals(_calibrationCameraId, cameraId, StringComparison.Ordinal);

            _isCalibrationEffective = isEffective;
            _calibrationCameraId = cameraId;

            if (effectiveChanged)
            {
                RaisePropertyChanged(nameof(IsCalibrationEffective));
            }

            if (effectiveChanged || cameraChanged)
            {
                RaisePropertyChanged(nameof(CalibrationStatusText));
            }
        }
    }
}
