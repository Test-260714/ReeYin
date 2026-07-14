using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using Newtonsoft.Json.Linq;
using ReeYin_V.Core.Calibration;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeepLearningPoint = ReeYin_V.Core.DeepLearning.Point;
using HalconDrawingObject = ReeYin_V.UI.Controls.DrawingObjectInfo;
using HalconShapeType = ReeYin_V.UI.Controls.ShapeType;

namespace ALGO.DefectPostProcess.Models
{
    public partial class DefectPostProcessModel
    {
        #region 运行时预览、标定与诊断


        /// <summary>
        /// 清空运行期预览缓存、ROI 和显示状态，供执行开始或释放时复位使用。
        /// </summary>
        private void ClearRuntimePreviewState()
        {
            RunOnDispatcher(() =>
            {
                PreviewResultCount = 0;
                ClearSheetSizeJudgePreviewRegions();
                ClearPreviewRois();
                ClearPreviewDisplayCore();
            });
        }

        /// <summary>
        /// 按偏移量切换当前预览图，实际索引由选择逻辑做边界归一化。
        /// </summary>
        public void MovePreviewImage(int offset)
        {
            SelectPreviewImage(CurrentPreviewImageIndex + offset);
        }

        /// <summary>
        /// 选择指定预览图并刷新当前图像和覆盖层。
        /// </summary>
        public void SelectPreviewImage(int imageIndex)
        {
            CurrentPreviewImageIndex = imageIndex;
            RefreshCurrentPreviewImageFromRuntimeImages();
            RequestPreviewRefresh();
        }

        /// <summary>
        /// 将预览图索引限制在当前图像数量范围内。
        /// </summary>
        private int NormalizePreviewImageIndex(int imageIndex)
        {
            int imageCount = PreviewImageCount;
            if (imageCount <= 0)
            {
                return 0;
            }

            return Math.Clamp(imageIndex, 0, imageCount - 1);
        }

        /// <summary>
        /// 通知预览图导航相关绑定属性刷新。
        /// </summary>
        private void RaisePreviewImageNavigationProperties()
        {
            RaisePropertyChanged(nameof(CurrentPreviewImageIndex));
            RaisePropertyChanged(nameof(PreviewImageCount));
            RaisePropertyChanged(nameof(CurrentPreviewImageDisplayText));
            RaisePropertyChanged(nameof(CanMovePreviousPreviewImage));
            RaisePropertyChanged(nameof(CanMoveNextPreviewImage));
        }

        /// <summary>
        /// 按当前运行缓存重建预览图像和缺陷 ROI。
        /// </summary>
        public void RefreshPreview()
        {
            RefreshPreview(preferExecutionResults: false);
        }

        /// <summary>
        /// 按指定结果来源刷新预览，执行结果优先时使用最新运行输出。
        /// </summary>
        private void RefreshPreview(bool preferExecutionResults)
        {
            if (IsFastModeEnabled)
            {
                return;
            }

            List<Result> previewResults = GetPreviewResultsCore(preferExecutionResults);
            PreviewResultCount = previewResults.Count;

            using HImage previewImage = CopyCurrentPreviewImageOrNull();
            if (!HalconImageOwnership.IsInitializedSafe(previewImage))
            {
                ClearPreviewRois();
                ClearPreviewDisplay();
                return;
            }

            try
            {
                UpdatePreviewRois(previewResults);
                DisplayPreviewImage(previewImage, refreshCachedRois: true);
            }
            catch (Exception ex)
            {
                LogWarning($"Refresh preview failed: {ex.Message}");
                ClearPreviewRois();
                ClearPreviewDisplay();
            }
        }

        /// <summary>
        /// 根据当前图像的结果重建预览 ROI 缓存。
        /// </summary>
        private void UpdatePreviewRois(IReadOnlyList<Result> previewResults)
        {
            ClearPreviewRois();

            if (previewResults != null)
            {
                foreach (Result previewResult in previewResults)
                {
                    HObject defectRegion = SafeCloneHObject(previewResult.Seg, "DefectPostProcess clone preview defect region failed");
                    if (!IsInitializedSafely(defectRegion))
                    {
                        continue;
                    }

                    ShowHRoi(new HRoi(Serial, ModuleName, string.Empty, HRoiType.检测结果, GetPreviewColor(previewResult.ClassId), defectRegion, _for: true));
                }
            }

            AddSheetSizeJudgePreviewRois();
        }

        /// <summary>
        /// 将缓存 ROI 同步到预览绘制对象集合。
        /// </summary>
        public void ShowHRoi()
        {
            if (IsFastModeEnabled)
            {
                return;
            }

            RunOnDispatcher(RefreshPreviewDrawObjectsFromCachedRoisCore);
        }

        /// <summary>
        /// 登记一条预览 ROI；普通 ROI 按类型替换，缺陷结果 ROI 可追加多条。
        /// </summary>
        public void ShowHRoi(HRoi roi)
        {
            try
            {
                if (roi == null)
                {
                    return;
                }

                mHRoi ??= new List<HRoi>();
                int index = mHRoi.FindIndex(item => item.roiType == roi.roiType && item.ModuleName == roi.ModuleName);
                if (roi.fors)
                {
                    mHRoi.Add(roi);
                    return;
                }

                if (index > -1)
                {
                    DisposeRoiObject(mHRoi[index]);
                    mHRoi[index] = roi;
                }
                else
                {
                    mHRoi.Add(roi);
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// 释放并清空当前缓存的 ROI HALCON 对象。
        /// </summary>
        private void ClearPreviewRois()
        {
            foreach (HRoi roi in mHRoi ?? Enumerable.Empty<HRoi>())
            {
                DisposeRoiObject(roi);
            }

            mHRoi?.Clear();
        }

        /// <summary>
        /// 释放单个 ROI 持有的 HALCON 对象。
        /// </summary>
        private static void DisposeRoiObject(HRoi roi)
        {
            SafeDisposeHObject(roi?.hobject);
            if (roi != null)
            {
                roi.hobject = null;
            }
        }

        /// <summary>
        /// 按类别编号取稳定的预览颜色。
        /// </summary>
        private static string GetPreviewColor(int classId)
        {
            int colorIndex = (int)(Math.Abs((long)classId) % PreviewColors.Length);
            return PreviewColors[colorIndex];
        }

        /// <summary>
        /// 通知缺陷特征值弹窗刷新当前结果显示。
        /// </summary>
        private void NotifyFeatureValueDialogChanged()
        {
            FeatureValueDialogRefreshToken++;
        }


        /// <summary>
        /// 使用标定 SDK 读取文件并返回相机参数，失败时释放已创建的 SDK 实例。
        /// </summary>
        private static bool TryLoadCalibrationFileWithSdk(string filePath, out CameraCalibrationSdk cameraCalibrationSdk,
            out CameraCalibrationSdk.CameraParams cameraParams, out string error)
        {
            cameraCalibrationSdk = null;
            cameraParams = new CameraCalibrationSdk.CameraParams();
            error = string.Empty;

            try
            {
                cameraCalibrationSdk = new CameraCalibrationSdk();
                cameraCalibrationSdk.loadCalibrationFile(filePath);
                cameraCalibrationSdk.getCameraParams(ref cameraParams);
                return true;
            }
            catch (Exception ex)
            {
                cameraCalibrationSdk?.Dispose();
                cameraCalibrationSdk = null;
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 将 SDK 相机参数转换为本模块使用的标定坐标上下文。
        /// </summary>
        private static CalibrationCoordinateContext CreateCalibrationCoordinateContext(CameraCalibrationSdk.CameraParams cameraParams)
        {
            double[] extrinsic = cameraParams.hasExtrinsic != 0
                ? CloneCalibrationValues(cameraParams.extrinsic)
                : BuildExtrinsicFromRotationTranslation(
                    cameraParams.hasRvec != 0 ? CloneCalibrationValues(cameraParams.rvec) : Array.Empty<double>(),
                    cameraParams.hasTvec != 0 ? CloneCalibrationValues(cameraParams.tvec) : Array.Empty<double>());

            return new CalibrationCoordinateContext
            {
                CameraId = cameraParams.cameraId ?? string.Empty,
                IntervalX = cameraParams.hasIntervalX != 0 ? cameraParams.intervalX : 0d,
                IntervalY = cameraParams.hasIntervalY != 0 ? cameraParams.intervalY : 0d,
                Intrinsic = cameraParams.hasIntrinsic != 0 ? CloneCalibrationValues(cameraParams.intrinsic) : Array.Empty<double>(),
                Distortion = cameraParams.hasDistortion != 0 ? CloneCalibrationValues(cameraParams.distortion) : Array.Empty<double>(),
                Extrinsic = extrinsic,
                HomographyMatrix = cameraParams.hasHomographyMatrix != 0 ? CloneCalibrationValues(cameraParams.homographyMatrix) : Array.Empty<double>(),
                HomographyUsesPixelCoordinates = cameraParams.hasHomographyMatrix != 0 && cameraParams.hasIntrinsic == 0
            };
        }

        /// <summary>
        /// 复制标定数组，避免直接持有 SDK 返回的缓冲引用。
        /// </summary>
        private static double[] CloneCalibrationValues(double[] values)
        {
            return values?.ToArray() ?? Array.Empty<double>();
        }

        /// <summary>
        /// 根据扩展名和首个有效字符判断文件是否应按 JSON 解析。
        /// </summary>
        private static bool ShouldTryLoadCalibrationJson(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                using StreamReader reader = File.OpenText(filePath);
                while (!reader.EndOfStream)
                {
                    int current = reader.Read();
                    if (current < 0)
                    {
                        break;
                    }

                    if (!char.IsWhiteSpace((char)current))
                    {
                        return current == '{' || current == '[';
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// 从 JSON 标定文件提取坐标转换上下文，兼容新旧字段名。
        /// </summary>
        private static bool TryLoadCalibrationJsonContext(string filePath, out CalibrationCoordinateContext calibrationContext, out string error)
        {
            calibrationContext = null;
            error = string.Empty;

            try
            {
                JToken rootToken = JToken.Parse(File.ReadAllText(filePath));
                JObject calibrationObject = FindCalibrationContextObject(rootToken);
                if (calibrationObject == null)
                {
                    error = "No calibration data was found in JSON.";
                    return false;
                }

                double[] jsonExtrinsic = GetCalibrationArrayValue(rootToken, calibrationObject, "extrinsic");
                if (jsonExtrinsic.Length == 0)
                {
                    jsonExtrinsic = BuildExtrinsicFromRotationTranslation(
                        GetCalibrationArrayValue(rootToken, calibrationObject, "rvec"),
                        GetCalibrationArrayValue(rootToken, calibrationObject, "tvec"));
                }

                double[] jsonHomography = GetCalibrationArrayValue(rootToken, calibrationObject, "homographyMatrix");
                bool homographyUsesPixelCoordinates = false;
                if (jsonHomography.Length == 0)
                {
                    jsonHomography = GetCalibrationArrayValue(rootToken, calibrationObject, "H_canvas_to_board");
                    homographyUsesPixelCoordinates = jsonHomography.Length > 0;
                }

                double intervalX = GetCalibrationDoubleValue(rootToken, calibrationObject, "intervalX");
                double intervalY = GetCalibrationDoubleValue(rootToken, calibrationObject, "intervalY");
                double worldUnitsPerPixel = GetCalibrationDoubleValue(rootToken, calibrationObject, "worldUnitsPerPixel");
                if ((intervalX <= 0 || intervalY <= 0) && worldUnitsPerPixel > 0)
                {
                    intervalX = intervalX > 0 ? intervalX : worldUnitsPerPixel;
                    intervalY = intervalY > 0 ? intervalY : worldUnitsPerPixel;
                }

                calibrationContext = new CalibrationCoordinateContext
                {
                    CameraId = GetCalibrationStringValue(rootToken, calibrationObject, "cameraId"),
                    IntervalX = intervalX,
                    IntervalY = intervalY,
                    Intrinsic = GetCalibrationArrayValue(rootToken, calibrationObject, "intrinsic"),
                    Distortion = GetCalibrationArrayValue(rootToken, calibrationObject, "distortion"),
                    Extrinsic = jsonExtrinsic,
                    HomographyMatrix = jsonHomography,
                    HomographyUsesPixelCoordinates = homographyUsesPixelCoordinates
                };

                if (string.IsNullOrWhiteSpace(calibrationContext.CameraId))
                {
                    calibrationContext.CameraId = GetCalibrationStringValue(rootToken, calibrationObject, "virtualCameraId");
                }

                if (string.IsNullOrWhiteSpace(calibrationContext.CameraId))
                {
                    calibrationContext.CameraId = Path.GetFileNameWithoutExtension(filePath);
                }

                if (!CanConvertWithCalibrationContext(calibrationContext) && !HasValidCalibrationInterval(calibrationContext))
                {
                    error = "JSON calibration file does not contain usable transform data.";
                    calibrationContext = null;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                calibrationContext = null;
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 从文本或 YAML 风格标定文件提取坐标转换上下文，作为 SDK 和 JSON 外的兼容路径。
        /// </summary>
        private static bool TryLoadCalibrationTextContext(string filePath, out CalibrationCoordinateContext calibrationContext, out string error)
        {
            calibrationContext = null;
            error = string.Empty;

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                double intervalX = TryReadCalibrationTextDouble(lines, "intervalX", out double parsedIntervalX) ? parsedIntervalX : 0d;
                double intervalY = TryReadCalibrationTextDouble(lines, "intervalY", out double parsedIntervalY) ? parsedIntervalY : 0d;
                if ((intervalX <= 0 || intervalY <= 0)
                    && TryReadCalibrationTextDouble(lines, "worldUnitsPerPixel", out double worldUnitsPerPixel)
                    && worldUnitsPerPixel > 0)
                {
                    intervalX = intervalX > 0 ? intervalX : worldUnitsPerPixel;
                    intervalY = intervalY > 0 ? intervalY : worldUnitsPerPixel;
                }

                double[] extrinsic = TryReadCalibrationTextArray(lines, "extrinsic", out double[] parsedExtrinsic)
                    ? parsedExtrinsic
                    : BuildExtrinsicFromRotationTranslation(
                        TryReadCalibrationTextArray(lines, "rvec", out double[] rvec) ? rvec : Array.Empty<double>(),
                        TryReadCalibrationTextArray(lines, "tvec", out double[] tvec) ? tvec : Array.Empty<double>());
                double[] homographyMatrix = TryReadCalibrationTextArray(lines, "homographyMatrix", out double[] parsedHomographyMatrix)
                    ? parsedHomographyMatrix
                    : Array.Empty<double>();
                bool homographyUsesPixelCoordinates = false;
                if (homographyMatrix.Length == 0
                    && TryReadCalibrationTextArray(lines, "H_canvas_to_board", out double[] canvasToBoardHomography))
                {
                    homographyMatrix = canvasToBoardHomography;
                    homographyUsesPixelCoordinates = true;
                }

                calibrationContext = new CalibrationCoordinateContext
                {
                    CameraId = TryReadCalibrationTextString(lines, "cameraId", out string cameraId)
                        ? cameraId
                        : TryReadCalibrationTextString(lines, "virtualCameraId", out string virtualCameraId)
                            ? virtualCameraId
                            : Path.GetFileNameWithoutExtension(filePath),
                    IntervalX = intervalX,
                    IntervalY = intervalY,
                    Intrinsic = TryReadCalibrationTextArray(lines, "intrinsic", out double[] intrinsic) ? intrinsic : Array.Empty<double>(),
                    Distortion = TryReadCalibrationTextArray(lines, "distortion", out double[] distortion) ? distortion : Array.Empty<double>(),
                    Extrinsic = extrinsic,
                    HomographyMatrix = homographyMatrix,
                    HomographyUsesPixelCoordinates = homographyUsesPixelCoordinates
                };

                if (!CanConvertWithCalibrationContext(calibrationContext) && !HasValidCalibrationInterval(calibrationContext))
                {
                    error = "Calibration file does not contain usable transform data or valid intervalX/intervalY.";
                    calibrationContext = null;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 从文本标定行读取双精度配置值。
        /// </summary>
        private static bool TryReadCalibrationTextDouble(IEnumerable<string> lines, string propertyName, out double value)
        {
            value = 0d;
            if (!TryReadCalibrationTextString(lines, propertyName, out string textValue))
            {
                return false;
            }

            return TryConvertToDouble(textValue, out value);
        }

        /// <summary>
        /// 从文本标定行读取标量字符串值。
        /// </summary>
        private static bool TryReadCalibrationTextString(IEnumerable<string> lines, string propertyName, out string value)
        {
            value = string.Empty;
            if (lines == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            foreach (string line in lines)
            {
                string scalarValue = ExtractCalibrationTextScalar(line, propertyName);
                if (!string.IsNullOrWhiteSpace(scalarValue))
                {
                    value = scalarValue;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 从文本标定行或 OpenCV 矩阵块读取数值数组。
        /// </summary>
        private static bool TryReadCalibrationTextArray(IList<string> lines, string propertyName, out double[] values)
        {
            values = Array.Empty<double>();
            if (lines == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (!IsCalibrationTextPropertyLine(lines[i], propertyName))
                {
                    continue;
                }

                string inlineValue = ExtractCalibrationTextRawValue(lines[i], propertyName);
                if (!string.IsNullOrWhiteSpace(inlineValue) && TryParseCalibrationNumberList(inlineValue, out values))
                {
                    return values.Length > 0;
                }

                string matrixDataText = CollectOpenCvMatrixData(lines, i + 1);
                return TryParseCalibrationNumberList(matrixDataText, out values) && values.Length > 0;
            }

            return false;
        }

        /// <summary>
        /// 判断当前文本行是否是指定标定字段。
        /// </summary>
        private static bool IsCalibrationTextPropertyLine(string line, string propertyName)
        {
            string trimmedLine = RemoveCalibrationTextComment(line).Trim();
            int separatorIndex = trimmedLine.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return false;
            }

            string key = trimmedLine.Substring(0, separatorIndex).Trim();
            return string.Equals(key, propertyName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 收集 OpenCV YAML 矩阵的 data 段文本。
        /// </summary>
        private static string CollectOpenCvMatrixData(IList<string> lines, int startIndex)
        {
            bool isCollectingData = false;
            List<string> dataLines = new List<string>();

            for (int i = startIndex; i < lines.Count; i++)
            {
                string line = RemoveCalibrationTextComment(lines[i]);
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                if (!isCollectingData && IsTopLevelCalibrationTextProperty(line))
                {
                    break;
                }

                if (!isCollectingData)
                {
                    int dataIndex = trimmedLine.IndexOf("data:", StringComparison.OrdinalIgnoreCase);
                    if (dataIndex < 0)
                    {
                        continue;
                    }

                    isCollectingData = true;
                    dataLines.Add(trimmedLine.Substring(dataIndex + "data:".Length));
                    if (trimmedLine.Contains("]"))
                    {
                        break;
                    }

                    continue;
                }

                dataLines.Add(trimmedLine);
                if (trimmedLine.Contains("]"))
                {
                    break;
                }
            }

            return string.Join(" ", dataLines);
        }

        /// <summary>
        /// 判断文本行是否是顶层标定字段，避免矩阵解析越界。
        /// </summary>
        private static bool IsTopLevelCalibrationTextProperty(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || char.IsWhiteSpace(line[0]))
            {
                return false;
            }

            string trimmedLine = line.Trim();
            int separatorIndex = trimmedLine.IndexOf(':');
            return separatorIndex > 0 && !trimmedLine.StartsWith("%", StringComparison.Ordinal);
        }

        /// <summary>
        /// 将标定文本中的数字序列解析为数组。
        /// </summary>
        private static bool TryParseCalibrationNumberList(string text, out double[] values)
        {
            values = Array.Empty<double>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string normalizedText = text
                .Replace("[", " ")
                .Replace("]", " ")
                .Replace("{", " ")
                .Replace("}", " ")
                .Replace("(", " ")
                .Replace(")", " ");
            string[] parts = normalizedText.Split(new[] { ',', ';', '|', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<double> numbers = new List<double>();
            foreach (string part in parts)
            {
                if (TryConvertToDouble(part, out double number))
                {
                    numbers.Add(number);
                }
            }

            values = numbers.ToArray();
            return values.Length > 0;
        }

        /// <summary>
        /// 由旋转向量和平移向量构造 4x4 外参矩阵。
        /// </summary>
        private static double[] BuildExtrinsicFromRotationTranslation(double[] rvec, double[] tvec)
        {
            if (rvec == null || tvec == null || rvec.Length < 3 || tvec.Length < 3)
            {
                return Array.Empty<double>();
            }

            double rx = rvec[0];
            double ry = rvec[1];
            double rz = rvec[2];
            double theta = Math.Sqrt((rx * rx) + (ry * ry) + (rz * rz));
            double[] rotation = theta <= 1e-12
                ? new[] { 1d, 0d, 0d, 0d, 1d, 0d, 0d, 0d, 1d }
                : BuildRotationMatrixByRodrigues(rx / theta, ry / theta, rz / theta, theta);

            return new[]
            {
                rotation[0], rotation[1], rotation[2], tvec[0],
                rotation[3], rotation[4], rotation[5], tvec[1],
                rotation[6], rotation[7], rotation[8], tvec[2],
                0d, 0d, 0d, 1d
            };
        }

        /// <summary>
        /// 按 Rodrigues 公式生成 3x3 旋转矩阵。
        /// </summary>
        private static double[] BuildRotationMatrixByRodrigues(double kx, double ky, double kz, double theta)
        {
            double cos = Math.Cos(theta);
            double sin = Math.Sin(theta);
            double oneMinusCos = 1d - cos;

            return new[]
            {
                cos + (kx * kx * oneMinusCos),
                (kx * ky * oneMinusCos) - (kz * sin),
                (kx * kz * oneMinusCos) + (ky * sin),
                (ky * kx * oneMinusCos) + (kz * sin),
                cos + (ky * ky * oneMinusCos),
                (ky * kz * oneMinusCos) - (kx * sin),
                (kz * kx * oneMinusCos) - (ky * sin),
                (kz * ky * oneMinusCos) + (kx * sin),
                cos + (kz * kz * oneMinusCos)
            };
        }

        /// <summary>
        /// 提取文本标定行中指定字段冒号后的原始值。
        /// </summary>
        private static string ExtractCalibrationTextRawValue(string line, string propertyName)
        {
            string trimmedLine = RemoveCalibrationTextComment(line).Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                return string.Empty;
            }

            int separatorIndex = trimmedLine.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return string.Empty;
            }

            string key = trimmedLine.Substring(0, separatorIndex).Trim();
            return string.Equals(key, propertyName, StringComparison.OrdinalIgnoreCase)
                ? trimmedLine.Substring(separatorIndex + 1).Trim()
                : string.Empty;
        }

        /// <summary>
        /// 提取文本标定行的第一个标量值。
        /// </summary>
        private static string ExtractCalibrationTextScalar(string line, string propertyName)
        {
            string rawValue = ExtractCalibrationTextRawValue(line, propertyName);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            string normalizedValue = rawValue
                .Trim('\'', '"')
                .Trim('[', ']', '(', ')')
                .Trim();
            string[] parts = normalizedValue.Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0].Trim('\'', '"') : normalizedValue;
        }

        /// <summary>
        /// 去除文本标定行中的井号注释。
        /// </summary>
        private static string RemoveCalibrationTextComment(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return string.Empty;
            }

            int commentIndex = line.IndexOf('#');
            return commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
        }

        /// <summary>
        /// 在 JSON 根节点及子节点中查找标定对象。
        /// </summary>
        private static JObject FindCalibrationContextObject(JToken rootToken)
        {
            if (rootToken is JObject rootObject && IsCalibrationContextObject(rootObject))
            {
                return rootObject;
            }

            if (rootToken is not JContainer container)
            {
                return null;
            }

            return container.Descendants()
                .OfType<JObject>()
                .FirstOrDefault(IsCalibrationContextObject);
        }

        /// <summary>
        /// 判断 JSON 对象是否包含可用的标定字段。
        /// </summary>
        private static bool IsCalibrationContextObject(JObject candidate)
        {
            return candidate != null
                && (TryGetPropertyToken(candidate, "intrinsic", out _)
                    || TryGetPropertyToken(candidate, "extrinsic", out _)
                    || TryGetPropertyToken(candidate, "homographyMatrix", out _)
                    || TryGetPropertyToken(candidate, "H_canvas_to_board", out _)
                    || TryGetPropertyToken(candidate, "intervalX", out _)
                    || TryGetPropertyToken(candidate, "worldUnitsPerPixel", out _)
                    || TryGetPropertyToken(candidate, "cameraId", out _)
                    || TryGetPropertyToken(candidate, "virtualCameraId", out _));
        }

        /// <summary>
        /// 从 JSON 标定对象或根节点读取字符串字段。
        /// </summary>
        private static string GetCalibrationStringValue(JToken rootToken, JObject calibrationObject, string propertyName)
        {
            JToken propertyToken = GetCalibrationPropertyToken(rootToken, calibrationObject, propertyName);
            return propertyToken?.Type == JTokenType.String
                ? propertyToken.Value<string>()?.Trim() ?? string.Empty
                : propertyToken?.ToString().Trim() ?? string.Empty;
        }

        /// <summary>
        /// 从 JSON 标定对象或根节点读取数值字段。
        /// </summary>
        private static double GetCalibrationDoubleValue(JToken rootToken, JObject calibrationObject, string propertyName)
        {
            JToken propertyToken = GetCalibrationPropertyToken(rootToken, calibrationObject, propertyName);
            return TryConvertTokenToDouble(propertyToken, out double value) ? value : 0d;
        }

        /// <summary>
        /// 从 JSON 标定对象或根节点读取数组字段。
        /// </summary>
        private static double[] GetCalibrationArrayValue(JToken rootToken, JObject calibrationObject, string propertyName)
        {
            JToken propertyToken = GetCalibrationPropertyToken(rootToken, calibrationObject, propertyName);
            return TryReadCalibrationArray(propertyToken, out double[] values) ? values : Array.Empty<double>();
        }

        /// <summary>
        /// 按字段名优先从标定对象读取，缺失时回退到根对象。
        /// </summary>
        private static JToken GetCalibrationPropertyToken(JToken rootToken, JObject calibrationObject, string propertyName)
        {
            if (TryGetPropertyToken(calibrationObject, propertyName, out JToken propertyToken))
            {
                return propertyToken;
            }

            if (!ReferenceEquals(rootToken, calibrationObject)
                && rootToken is JObject rootObject
                && TryGetPropertyToken(rootObject, propertyName, out propertyToken))
            {
                return propertyToken;
            }

            return null;
        }

        /// <summary>
        /// 忽略大小写获取 JSON 属性节点。
        /// </summary>
        private static bool TryGetPropertyToken(JObject source, string propertyName, out JToken propertyToken)
        {
            propertyToken = null;
            if (source == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            JProperty property = source.Properties()
                .FirstOrDefault(item => string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            if (property == null)
            {
                return false;
            }

            propertyToken = property.Value;
            return true;
        }

        /// <summary>
        /// 从 JSON 数组、对象或单值中读取标定数值数组。
        /// </summary>
        private static bool TryReadCalibrationArray(JToken sourceToken, out double[] values)
        {
            values = Array.Empty<double>();
            if (sourceToken == null)
            {
                return false;
            }

            if (sourceToken.Type == JTokenType.Array)
            {
                List<double> numbers = new List<double>();
                CollectCalibrationNumbers(sourceToken, numbers);
                if (numbers.Count > 0)
                {
                    values = numbers.ToArray();
                    return true;
                }

                return false;
            }

            if (sourceToken is JObject sourceObject)
            {
                if (TryGetPropertyToken(sourceObject, "data", out JToken dataToken))
                {
                    return TryReadCalibrationArray(dataToken, out values);
                }

                if (TryGetPropertyToken(sourceObject, "value", out JToken valueToken))
                {
                    return TryReadCalibrationArray(valueToken, out values);
                }

                return false;
            }

            if (TryConvertTokenToDouble(sourceToken, out double singleValue))
            {
                values = new[] { singleValue };
                return true;
            }

            return false;
        }

        /// <summary>
        /// 递归收集 JSON 数组中的标定数值。
        /// </summary>
        private static void CollectCalibrationNumbers(JToken sourceToken, IList<double> values)
        {
            if (sourceToken == null || values == null)
            {
                return;
            }

            if (sourceToken.Type == JTokenType.Array)
            {
                foreach (JToken childToken in sourceToken.Children())
                {
                    CollectCalibrationNumbers(childToken, values);
                }

                return;
            }

            if (TryConvertTokenToDouble(sourceToken, out double numericValue))
            {
                values.Add(numericValue);
            }
        }

        /// <summary>
        /// 将 JSON 数值或字符串节点转换为 double。
        /// </summary>
        private static bool TryConvertTokenToDouble(JToken sourceToken, out double value)
        {
            value = 0d;
            if (sourceToken == null)
            {
                return false;
            }

            return sourceToken.Type switch
            {
                JTokenType.Integer or JTokenType.Float or JTokenType.String => TryConvertToDouble(((JValue)sourceToken).Value, out value),
                _ => false
            };
        }

        /// <summary>
        /// 判断标定上下文是否具备矩阵转换条件。
        /// </summary>
        private static bool CanConvertWithCalibrationContext(CalibrationCoordinateContext calibrationContext)
        {
            return HasValidCalibrationMatrix(calibrationContext?.HomographyMatrix, 9)
                || (HasValidCalibrationMatrix(calibrationContext?.Intrinsic, 9)
                    && HasValidCalibrationMatrix(calibrationContext?.Extrinsic, 16));
        }

        /// <summary>
        /// 判断标定上下文是否提供有效像素间距。
        /// </summary>
        private static bool HasValidCalibrationInterval(CalibrationCoordinateContext calibrationContext)
        {
            return calibrationContext != null
                && calibrationContext.IntervalX > 0
                && calibrationContext.IntervalY > 0;
        }

        /// <summary>
        /// 判断标定矩阵长度和数值是否满足使用要求。
        /// </summary>
        private static bool HasValidCalibrationMatrix(double[] values, int requiredLength)
        {
            return values != null
                && values.Length >= requiredLength
                && values.Take(requiredLength).All(item => !double.IsNaN(item) && !double.IsInfinity(item));
        }

        /// <summary>
        /// 按当前标定上下文刷新结果中心点的物理坐标缓存。
        /// </summary>
        private void UpdateResultPhysicalCoordinates(IEnumerable<Result> results)
        {
            if (results == null)
            {
                return;
            }

            foreach (Result result in results)
            {
                UpdateResultPhysicalCoordinate(result);
            }
        }

        /// <summary>
        /// 刷新测量输出，异常时清空界面测量列表并记录日志。
        /// </summary>
        private void RefreshMeasurementOutputsSafely()
        {
            try
            {
                UpdateResultPhysicalCoordinates(Results);
                UpdateDefectMeasurementOutputs();
            }
            catch (Exception ex)
            {
                LogWarning($"Refresh measurement outputs failed: {ex.Message}");
                RunOnDispatcher(() =>
                {
                    DefectMeasurements.Clear();
                });
            }
        }

        /// <summary>
        /// 更新单个结果的面积缓存和物理坐标扩展字段。
        /// </summary>
        private void UpdateResultPhysicalCoordinate(Result result)
        {
            UpdateResultActualAreaCache(result);

            if (!TryConvertResultCenterToWorld(result, out double worldX, out double worldY, out double worldZ, out string coordinateSource))
            {
                RemoveResultPhysicalCoordinate(result);
                return;
            }

            result.Others ??= new Dictionary<string, object>();
            result.Others[DefectPostProcessResultKeys.WorldX] = worldX;
            result.Others[DefectPostProcessResultKeys.WorldY] = worldY;
            result.Others[DefectPostProcessResultKeys.WorldZ] = worldZ;
            result.Others[DefectPostProcessResultKeys.CoordinateSource] = coordinateSource;
        }

        /// <summary>
        /// 将单个缺陷结果中心从像素坐标转换为物理坐标。
        /// </summary>
        private bool TryConvertResultCenterToWorld(Result result, out double worldX, out double worldY, out double worldZ, out string coordinateSource)
        {
            worldX = 0d;
            worldY = 0d;
            worldZ = 0d;
            coordinateSource = string.Empty;

            if (result == null
                || float.IsNaN(result.Cx)
                || float.IsNaN(result.Cy)
                || float.IsInfinity(result.Cx)
                || float.IsInfinity(result.Cy))
            {
                return false;
            }

            if (TryConvertPixelToCalibrationWorld(result.Cx, result.Cy, out worldX, out worldY, out worldZ))
            {
                coordinateSource = CalibrationCoordinateSource;
                ApplyCoordinateReferenceOffset(coordinateSource, ref worldX, ref worldY, ref worldZ);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 按板边标定和图像上边中心参考点修正物理坐标。
        /// </summary>
        private void ApplyCoordinateReferenceOffset(string coordinateSource, ref double worldX, ref double worldY, ref double worldZ)
        {
            worldX -= EdgeCalibrationX;

            if (TryConvertImageTopCenterToWorld(coordinateSource, out _, out double referenceY, out _))
            {
                worldY = (worldY - referenceY) * GetVerticalCoordinateDirectionSign(coordinateSource);
                return;
            }

            if (TryConvertImageOriginToWorld(coordinateSource, out _, out double originY, out _))
            {
                worldY = (worldY - originY) * GetVerticalCoordinateDirectionSign(coordinateSource);
            }
        }

        /// <summary>
        /// 根据图像上下边中心的世界坐标判断 Y 方向符号。
        /// </summary>
        private double GetVerticalCoordinateDirectionSign(string coordinateSource)
        {
            if (TryConvertImageTopCenterToWorld(coordinateSource, out _, out double topY, out _)
                && TryConvertImageBottomCenterToWorld(coordinateSource, out _, out double bottomY, out _)
                && !double.IsNaN(topY)
                && !double.IsNaN(bottomY)
                && !double.IsInfinity(topY)
                && !double.IsInfinity(bottomY)
                && !topY.Equals(bottomY))
            {
                return bottomY > topY ? 1d : -1d;
            }

            return 1d;
        }

        /// <summary>
        /// 将图像左上角像素点转换为世界坐标。
        /// </summary>
        private bool TryConvertImageOriginToWorld(string coordinateSource, out double originX, out double originY, out double originZ)
        {
            originX = 0d;
            originY = 0d;
            originZ = 0d;

            if (string.Equals(coordinateSource, CalibrationCoordinateSource, StringComparison.Ordinal))
            {
                return TryConvertPixelToCalibrationWorld(0d, 0d, out originX, out originY, out originZ);
            }

            return false;
        }

        /// <summary>
        /// 将图像上边中心像素点转换为世界坐标。
        /// </summary>
        private bool TryConvertImageTopCenterToWorld(string coordinateSource, out double worldX, out double worldY, out double worldZ)
        {
            worldX = 0d;
            worldY = 0d;
            worldZ = 0d;

            if (!TryGetImageTopCenterPixel(out double pixelX, out double pixelY))
            {
                return false;
            }

            if (string.Equals(coordinateSource, CalibrationCoordinateSource, StringComparison.Ordinal))
            {
                return TryConvertPixelToCalibrationWorld(pixelX, pixelY, out worldX, out worldY, out worldZ);
            }

            return false;
        }

        /// <summary>
        /// 将图像下边中心像素点转换为世界坐标。
        /// </summary>
        private bool TryConvertImageBottomCenterToWorld(string coordinateSource, out double worldX, out double worldY, out double worldZ)
        {
            worldX = 0d;
            worldY = 0d;
            worldZ = 0d;

            if (!TryGetImageBottomCenterPixel(out double pixelX, out double pixelY))
            {
                return false;
            }

            if (string.Equals(coordinateSource, CalibrationCoordinateSource, StringComparison.Ordinal))
            {
                return TryConvertPixelToCalibrationWorld(pixelX, pixelY, out worldX, out worldY, out worldZ);
            }

            return false;
        }

        /// <summary>
        /// 获取当前预览图上边中心点的图像坐标。
        /// </summary>
        private bool TryGetImageTopCenterPixel(out double pixelX, out double pixelY)
        {
            pixelX = 0d;
            pixelY = 0d;

            using HImage image = CopyCurrentPreviewImageOrNull();
            if (!HalconImageOwnership.IsInitializedSafe(image))
            {
                return false;
            }

            try
            {
                HOperatorSet.GetImageSize(image, out HTuple width, out _);
                if (width.Length == 0 || width.D <= 0)
                {
                    return false;
                }

                pixelX = (width.D - 1d) / 2d;
                return true;
            }
            catch (Exception ex)
            {
                LogWarning($"Get top-center image pixel failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取当前预览图下边中心点的图像坐标。
        /// </summary>
        private bool TryGetImageBottomCenterPixel(out double pixelX, out double pixelY)
        {
            pixelX = 0d;
            pixelY = 0d;

            using HImage image = CopyCurrentPreviewImageOrNull();
            if (!HalconImageOwnership.IsInitializedSafe(image))
            {
                return false;
            }

            try
            {
                HOperatorSet.GetImageSize(image, out HTuple width, out HTuple height);
                if (width.Length == 0 || height.Length == 0 || width.D <= 0 || height.D <= 0)
                {
                    return false;
                }

                pixelX = (width.D - 1d) / 2d;
                pixelY = height.D - 1d;
                return true;
            }
            catch (Exception ex)
            {
                LogWarning($"Get bottom-center image pixel failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用已加载的标定上下文将像素坐标转换为世界坐标。
        /// </summary>
        private bool TryConvertPixelToWorldWithCalibrationContext(double pixelX, double pixelY, out double worldX, out double worldY, out double worldZ)
        {
            worldX = 0d;
            worldY = 0d;
            worldZ = 0d;

            CalibrationCoordinateContext calibrationContext = _calibrationCoordinateContext;
            if (calibrationContext == null)
            {
                return false;
            }

            if (calibrationContext.HomographyUsesPixelCoordinates
                && TryConvertPointByHomography(calibrationContext.HomographyMatrix, pixelX, pixelY, out worldX, out worldY))
            {
                return true;
            }

            if (TryUndistortPixelCoordinate(pixelX, pixelY, calibrationContext, out double normalizedX, out double normalizedY))
            {
                if (TryConvertPointByHomography(calibrationContext.HomographyMatrix, normalizedX, normalizedY, out worldX, out worldY))
                {
                    return true;
                }

                if (TryConvertNormalizedPointByExtrinsic(calibrationContext.Extrinsic, normalizedX, normalizedY, out worldX, out worldY))
                {
                    return true;
                }
            }

            return TryConvertPixelToWorldByInterval(calibrationContext.IntervalX, calibrationContext.IntervalY, pixelX, pixelY, out worldX, out worldY);
        }

        /// <summary>
        /// 标定矩阵不可用时，使用像素间距将像素坐标换算为物理坐标。
        /// </summary>
        private static bool TryConvertPixelToWorldByInterval(double intervalX, double intervalY, double pixelX, double pixelY,
            out double worldX, out double worldY)
        {
            worldX = 0d;
            worldY = 0d;

            if (intervalX <= 0 || intervalY <= 0)
            {
                return false;
            }

            worldX = pixelX * intervalX;
            worldY = pixelY * intervalY;
            return !double.IsNaN(worldX) && !double.IsNaN(worldY) && !double.IsInfinity(worldX) && !double.IsInfinity(worldY);
        }

        /// <summary>
        /// 根据内参和畸变参数将像素坐标转换为归一化去畸变坐标。
        /// </summary>
        private static bool TryUndistortPixelCoordinate(double pixelX, double pixelY, CalibrationCoordinateContext calibrationContext,
            out double normalizedX, out double normalizedY)
        {
            normalizedX = 0d;
            normalizedY = 0d;

            if (!HasValidCalibrationMatrix(calibrationContext?.Intrinsic, 9))
            {
                return false;
            }

            double fx = calibrationContext.Intrinsic[0];
            double fy = calibrationContext.Intrinsic[4];
            double cx = calibrationContext.Intrinsic[2];
            double cy = calibrationContext.Intrinsic[5];
            if (fx == 0 || fy == 0 || double.IsNaN(fx) || double.IsNaN(fy) || double.IsInfinity(fx) || double.IsInfinity(fy))
            {
                return false;
            }

            double distortedX = (pixelX - cx) / fx;
            double distortedY = (pixelY - cy) / fy;
            if (!HasValidCalibrationMatrix(calibrationContext.Distortion, 1))
            {
                normalizedX = distortedX;
                normalizedY = distortedY;
                return !double.IsNaN(normalizedX) && !double.IsNaN(normalizedY) && !double.IsInfinity(normalizedX) && !double.IsInfinity(normalizedY);
            }

            double k1 = GetCalibrationValue(calibrationContext.Distortion, 0);
            double k2 = GetCalibrationValue(calibrationContext.Distortion, 1);
            double p1 = GetCalibrationValue(calibrationContext.Distortion, 2);
            double p2 = GetCalibrationValue(calibrationContext.Distortion, 3);
            double k3 = GetCalibrationValue(calibrationContext.Distortion, 4);
            double k4 = GetCalibrationValue(calibrationContext.Distortion, 5);
            double k5 = GetCalibrationValue(calibrationContext.Distortion, 6);
            double k6 = GetCalibrationValue(calibrationContext.Distortion, 7);

            normalizedX = distortedX;
            normalizedY = distortedY;

            for (int i = 0; i < 8; i++)
            {
                double r2 = (normalizedX * normalizedX) + (normalizedY * normalizedY);
                double r4 = r2 * r2;
                double r6 = r4 * r2;
                double radial = 1d + (k1 * r2) + (k2 * r4) + (k3 * r6);
                double radialDenominator = 1d + (k4 * r2) + (k5 * r4) + (k6 * r6);
                if (radial == 0d || double.IsNaN(radial) || double.IsInfinity(radial)
                    || radialDenominator == 0d || double.IsNaN(radialDenominator) || double.IsInfinity(radialDenominator))
                {
                    return false;
                }

                double inverseRadial = radialDenominator / radial;
                double deltaX = (2d * p1 * normalizedX * normalizedY) + (p2 * (r2 + (2d * normalizedX * normalizedX)));
                double deltaY = (p1 * (r2 + (2d * normalizedY * normalizedY))) + (2d * p2 * normalizedX * normalizedY);

                normalizedX = (distortedX - deltaX) * inverseRadial;
                normalizedY = (distortedY - deltaY) * inverseRadial;
            }

            return !double.IsNaN(normalizedX) && !double.IsNaN(normalizedY)
                && !double.IsInfinity(normalizedX) && !double.IsInfinity(normalizedY);
        }

        /// <summary>
        /// 安全读取标定数组元素，缺失或非法时返回 0。
        /// </summary>
        private static double GetCalibrationValue(double[] values, int index)
        {
            if (values == null || index < 0 || index >= values.Length)
            {
                return 0d;
            }

            double value = values[index];
            return double.IsNaN(value) || double.IsInfinity(value) ? 0d : value;
        }

        /// <summary>
        /// 通过单应矩阵将输入点转换到物理平面坐标。
        /// </summary>
        private static bool TryConvertPointByHomography(double[] homographyMatrix, double inputX, double inputY,
            out double worldX, out double worldY)
        {
            worldX = 0d;
            worldY = 0d;

            if (!HasValidCalibrationMatrix(homographyMatrix, 9))
            {
                return false;
            }

            double w = (homographyMatrix[6] * inputX) + (homographyMatrix[7] * inputY) + homographyMatrix[8];
            if (System.Math.Abs(w) < 1e-12 || double.IsNaN(w) || double.IsInfinity(w))
            {
                return false;
            }

            worldX = ((homographyMatrix[0] * inputX) + (homographyMatrix[1] * inputY) + homographyMatrix[2]) / w;
            worldY = ((homographyMatrix[3] * inputX) + (homographyMatrix[4] * inputY) + homographyMatrix[5]) / w;
            return !double.IsNaN(worldX) && !double.IsNaN(worldY) && !double.IsInfinity(worldX) && !double.IsInfinity(worldY);
        }

        /// <summary>
        /// 通过外参矩阵将归一化相机坐标投影到世界平面。
        /// </summary>
        private static bool TryConvertNormalizedPointByExtrinsic(double[] extrinsicMatrix, double normalizedX, double normalizedY,
            out double worldX, out double worldY)
        {
            worldX = 0d;
            worldY = 0d;

            if (!HasValidCalibrationMatrix(extrinsicMatrix, 16))
            {
                return false;
            }

            double a00 = extrinsicMatrix[0] - (extrinsicMatrix[8] * normalizedX);
            double a01 = extrinsicMatrix[1] - (extrinsicMatrix[9] * normalizedX);
            double a10 = extrinsicMatrix[4] - (extrinsicMatrix[8] * normalizedY);
            double a11 = extrinsicMatrix[5] - (extrinsicMatrix[9] * normalizedY);
            double b0 = (extrinsicMatrix[11] * normalizedX) - extrinsicMatrix[3];
            double b1 = (extrinsicMatrix[11] * normalizedY) - extrinsicMatrix[7];

            double determinant = (a00 * a11) - (a01 * a10);
            if (System.Math.Abs(determinant) < 1e-12 || double.IsNaN(determinant) || double.IsInfinity(determinant))
            {
                return false;
            }

            worldX = ((b0 * a11) - (a01 * b1)) / determinant;
            worldY = ((a00 * b1) - (b0 * a10)) / determinant;
            return !double.IsNaN(worldX) && !double.IsNaN(worldY) && !double.IsInfinity(worldX) && !double.IsInfinity(worldY);
        }

        /// <summary>
        /// 判断世界坐标三个分量是否都是有限数值。
        /// </summary>
        private static bool IsValidWorldCoordinate(double worldX, double worldY, double worldZ)
        {
            return !(double.IsNaN(worldX) || double.IsNaN(worldY) || double.IsNaN(worldZ)
                || double.IsInfinity(worldX) || double.IsInfinity(worldY) || double.IsInfinity(worldZ));
        }

        /// <summary>
        /// 清空结果扩展字段中的物理坐标信息。
        /// </summary>
        private static void RemoveResultPhysicalCoordinate(Result result)
        {
            if (result == null)
            {
                return;
            }

            result.Others ??= new Dictionary<string, object>();
            result.Others[DefectPostProcessResultKeys.WorldX] = null;
            result.Others[DefectPostProcessResultKeys.WorldY] = null;
            result.Others[DefectPostProcessResultKeys.WorldZ] = null;
            result.Others[DefectPostProcessResultKeys.CoordinateSource] = null;
        }

        /// <summary>
        /// 规范化标定文件路径，非法路径保留原始文本。
        /// </summary>
        private static string NormalizeCalibrationFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return string.Empty;
            }

            string trimmedPath = filePath.Trim();
            try
            {
                return Path.GetFullPath(trimmedPath);
            }
            catch
            {
                return trimmedPath;
            }
        }


        /// <summary>
        /// 从结果扩展字段读取物理坐标数值，供测量表和外部显示复用。
        /// </summary>
        internal static double? GetResultPhysicalCoordinate(Result result, string key)
        {
            if (result?.Others == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            if (!result.Others.TryGetValue(key, out object value))
            {
                return null;
            }

            return TryConvertToDouble(value, out double coordinate) ? coordinate : null;
        }

        /// <summary>
        /// 读取结果物理坐标的来源标记。
        /// </summary>
        internal static string GetResultCoordinateSource(Result result)
        {
            if (result?.Others == null)
            {
                return string.Empty;
            }

            return result.Others.TryGetValue(DefectPostProcessResultKeys.CoordinateSource, out object value)
                ? value?.ToString() ?? string.Empty
                : string.Empty;
        }


        /// <summary>
        /// 克隆跨模块输入值，避免长期持有上游借用的 HALCON 对象。
        /// </summary>
        private static object CloneInputValue(object value)
        {
            if (value is HObject hObject)
            {
                return SafeCloneHObject(hObject, "DefectPostProcess clone input HObject failed");
            }

            if (value is Result result)
            {
                return CloneResult(result);
            }

            if (value is List<Result> results)
            {
                return CloneResults(results);
            }

            if (value is IEnumerable<Result> resultEnumerable)
            {
                return CloneResults(resultEnumerable);
            }

            if (value is IEnumerable<IEnumerable<Result>> resultGroups)
            {
                return CloneResultGroups(resultGroups);
            }

            if (value is IEnumerable enumerable and not string)
            {
                List<object> items = enumerable.Cast<object>().Where(item => item != null).ToList();
                if (items.Count == 0)
                {
                    return value;
                }

                if (items.All(item => item is Result))
                {
                    return CloneResults(items.Cast<Result>());
                }

                if (items.All(item => item is IEnumerable<Result>))
                {
                    return CloneResultGroups(items.Cast<IEnumerable<Result>>());
                }
            }

            return value;
        }

        /// <summary>
        /// 克隆结果列表并隔离其中的 HALCON 对象。
        /// </summary>
        private static List<Result> CloneResults(IEnumerable<Result> source)
        {
            return source?
                .Where(item => item != null)
                .Select(CloneResult)
                .ToList() ?? new List<Result>();
        }

        /// <summary>
        /// 克隆按图像分组的结果集合并保持分组语义。
        /// </summary>
        private static List<List<Result>> CloneResultGroups(IEnumerable<IEnumerable<Result>> source)
        {
            return CloneAndTagResultGroups(source);
        }

        /// <summary>
        /// 从输入值中提取并克隆按图像分组的检测结果。
        /// </summary>
        private static List<List<Result>> ExtractLinkedResultGroups(object value)
        {
            switch (value)
            {
                case null:
                    return new List<List<Result>>();
                case Result result:
                    return CloneAndTagResultGroups(new[] { new[] { result } });
                case IEnumerable<IEnumerable<Result>> resultGroups:
                    return CloneAndTagResultGroups(resultGroups);
                case IEnumerable enumerable when value is not string:
                    {
                        List<object> items = enumerable.Cast<object>().Where(item => item != null).ToList();
                        if (items.Count == 0)
                        {
                            return new List<List<Result>>();
                        }

                        if (items.All(item => item is IEnumerable<Result>))
                        {
                            return CloneAndTagResultGroups(items.Cast<IEnumerable<Result>>());
                        }

                        if (items.All(item => item is Result))
                        {
                            return CloneFlatResultsByPreservedImageIndex(items.Cast<Result>());
                        }

                        return new List<List<Result>>();
                    }
                default:
                    return new List<List<Result>>();
            }
        }

        /// <summary>
        /// 预览图所有权锁，保护 DisposeImage 的替换、复制和释放。
        /// </summary>
        private object PreviewImageOwnershipLock
        {
            get
            {
                if (_previewImageOwnershipLock != null)
                {
                    return _previewImageOwnershipLock;
                }

                System.Threading.Interlocked.CompareExchange(ref _previewImageOwnershipLock, new object(), null);
                return _previewImageOwnershipLock;
            }
        }

        /// <summary>
        /// 检查模块当前是否持有可用的预览图像。
        /// </summary>
        private bool HasCurrentPreviewImage()
        {
            lock (PreviewImageOwnershipLock)
            {
                return HalconImageOwnership.IsInitializedSafe(DisposeImage);
            }
        }

        /// <summary>
        /// 复制当前自有预览图，调用方负责释放返回的 HImage。
        /// </summary>
        private HImage CopyCurrentPreviewImageOrNull()
        {
            lock (PreviewImageOwnershipLock)
            {
                return CopyPreviewImageOrNull(DisposeImage);
            }
        }

        /// <summary>
        /// 替换模块持有的预览图像，接管新图并释放旧图。
        /// </summary>
        private void ReplaceCurrentPreviewImage(HImage nextOwnedImage)
        {
            lock (PreviewImageOwnershipLock)
            {
                HImage oldImage = DisposeImage;
                DisposeImage = nextOwnedImage;
                if (!ReferenceEquals(oldImage, nextOwnedImage))
                {
                    HalconImageOwnership.DisposeOwned(oldImage);
                }
            }
        }

        /// <summary>
        /// 释放并清空模块当前持有的预览图像。
        /// </summary>
        private void ClearCurrentPreviewImage()
        {
            lock (PreviewImageOwnershipLock)
            {
                HImage oldImage = DisposeImage;
                DisposeImage = null;
                HalconImageOwnership.DisposeOwned(oldImage);
            }
        }

        /// <summary>
        /// 复制预览图并切到 UI 线程发布，避免后台线程直接操作绑定对象。
        /// </summary>
        private void DisplayPreviewImage(HObject previewImage, bool refreshCachedRois)
        {
            if (IsFastModeEnabled || previewImage == null || !IsInitializedSafely(previewImage))
            {
                return;
            }

            HImage imageForUi = null;
            try
            {
                long updateVersion = NextPreviewUpdateVersion();
                imageForUi = CopyPreviewImageOrNull(previewImage);
                if (!HalconImageOwnership.IsInitializedSafe(imageForUi))
                {
                    return;
                }

                RunOnDispatcher(() =>
                {
                    if (!IsPreviewUpdateCurrent(updateVersion))
                    {
                        return;
                    }

                    SetPreviewImageObject(imageForUi);
                    imageForUi = null;
                    if (refreshCachedRois)
                    {
                        RefreshPreviewDrawObjectsFromCachedRoisCore();
                    }
                    else
                    {
                        ClearPreviewDrawObjects();
                    }
                });
            }
            catch (Exception ex)
            {
                LogTrace($"Refresh preview display failed: {ex.Message}");
            }
            finally
            {
                HalconImageOwnership.DisposeOwned(imageForUi);
            }
        }

        /// <summary>
        /// 更新 UI 预览图像引用并释放旧图像。
        /// </summary>
        private void SetPreviewImageObject(HImage image)
        {
            if (HalconImageOwnership.IsInitializedSafe(image))
            {
                try
                {
                    image.GetImageSize(out int width, out int height);
                    PreviewImageWidth = width;
                    PreviewImageHeight = height;
                }
                catch
                {
                    PreviewImageWidth = 1.0d;
                    PreviewImageHeight = 1.0d;
                }
            }
            else
            {
                PreviewImageWidth = 1.0d;
                PreviewImageHeight = 1.0d;
            }

            HObject oldImage = _previewImageObject;
            PreviewImageObject = image;
            SafeDisposeHObject(oldImage);
        }

        /// <summary>
        /// 复制传入图像，返回值由调用方负责释放，避免直接持有上游借用对象。
        /// </summary>
        private static HImage CopyPreviewImageOrNull(HObject previewImage)
        {
            if (previewImage is HImage hImage)
            {
                return HalconImageOwnership.CopyOwnedOrNull(hImage);
            }

            return HalconImageOwnership.TryCopyBorrowed(previewImage, 1, out HImage imageCopy)
                ? imageCopy
                : null;
        }

        /// <summary>
        /// 在 UI 线程清空预览图和绘制对象。
        /// </summary>
        private void ClearPreviewDisplay()
        {
            RunOnDispatcher(ClearPreviewDisplayCore);
        }

        /// <summary>
        /// 清空预览显示核心状态，调用方需确保在 UI 线程。
        /// </summary>
        private void ClearPreviewDisplayCore()
        {
            NextPreviewUpdateVersion();
            ClearPreviewDrawObjects();
            SetPreviewImageObject(null);
        }

        /// <summary>
        /// 生成预览更新版本号，用于丢弃过期 UI 刷新。
        /// </summary>
        private long NextPreviewUpdateVersion()
        {
            return System.Threading.Interlocked.Increment(ref _previewUpdateVersion);
        }

        /// <summary>
        /// 读取当前预览更新版本号。
        /// </summary>
        private long CurrentPreviewUpdateVersion()
        {
            return System.Threading.Volatile.Read(ref _previewUpdateVersion);
        }

        /// <summary>
        /// 判断待发布的预览版本是否仍是最新版本。
        /// </summary>
        private bool IsPreviewUpdateCurrent(long updateVersion)
        {
            return updateVersion == CurrentPreviewUpdateVersion();
        }

        /// <summary>
        /// 根据缓存 ROI 重建绘制对象集合，调用方需在 UI 线程。
        /// </summary>
        private void RefreshPreviewDrawObjectsFromCachedRoisCore()
        {
            ClearPreviewDrawObjects();
            foreach (HRoi roi in mHRoi?.Where(item => item?.ModuleName == ModuleName).ToList() ?? new List<HRoi>())
            {
                AddRegionPreviewDrawObject(roi.hobject, roi.drawColor);
            }
        }

        /// <summary>
        /// 清空预览覆盖层绘制对象集合。
        /// </summary>
        private void ClearPreviewDrawObjects()
        {
            foreach (HalconDrawingObject drawObject in PreviewDrawObjects.ToList())
            {
                SafeDisposeHObject(drawObject?.Hobject);
            }

            PreviewDrawObjects.Clear();
        }

        /// <summary>
        /// 将 HALCON 区域转换为 UI 覆盖层绘制对象。
        /// </summary>
        private void AddRegionPreviewDrawObject(HObject region, string color)
        {
            if (region == null || !IsInitializedSafely(region))
            {
                return;
            }

            try
            {
                HObject previewRegion = SafeCloneHObject(region, "DefectPostProcess clone preview region failed");
                if (!IsInitializedSafely(previewRegion))
                {
                    return;
                }

                PreviewDrawObjects.Add(new HalconDrawingObject
                {
                    ShapeType = HalconShapeType.Region,
                    Hobject = previewRegion,
                    Color = NormalizePreviewColor(color),
                    IsFillDisplay = false
                });
            }
            catch (Exception ex)
            {
                LogTrace($"Add preview region draw object failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 规范化预览颜色，空值时使用默认颜色。
        /// </summary>
        private static string NormalizePreviewColor(string halconColor)
        {
            return (halconColor ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "cyan" => "cyan",
                "magenta" => "magenta",
                "green" => "green",
                "yellow" => "yellow",
                "orange" => "orange",
                "red" => "red",
                "blue" => "blue",
                _ => "green"
            };
        }

        /// <summary>
        /// 克隆扁平结果并按结果中的图像索引重新分组。
        /// </summary>
        private static List<List<Result>> CloneFlatResultsByPreservedImageIndex(IEnumerable<Result> source)
        {
            List<Result> clonedResults = CloneResults(source);
            if (clonedResults.Count == 0)
            {
                return new List<List<Result>>();
            }

            int groupCount = clonedResults.Max(GetResultImageIndex) + 1;
            List<List<Result>> groups = Enumerable.Range(0, groupCount)
                .Select(_ => new List<Result>())
                .ToList();

            foreach (Result result in clonedResults)
            {
                int imageIndex = GetResultImageIndex(result);
                groups[imageIndex].Add(result);
            }

            return groups;
        }

        /// <summary>
        /// 克隆结果分组并写入对应图像索引。
        /// </summary>
        private static List<List<Result>> CloneAndTagResultGroups(IEnumerable<IEnumerable<Result>> resultGroups)
        {
            List<List<Result>> clonedGroups = new List<List<Result>>();
            if (resultGroups == null)
            {
                return clonedGroups;
            }

            int imageIndex = 0;
            foreach (IEnumerable<Result> resultGroup in resultGroups)
            {
                List<Result> clonedGroup = CloneResults(resultGroup);
                TagResultsWithImageIndex(clonedGroup, imageIndex);
                clonedGroups.Add(clonedGroup);
                imageIndex++;
            }

            return clonedGroups;
        }

        /// <summary>
        /// 给同一组结果写入图像索引扩展字段。
        /// </summary>
        private static void TagResultsWithImageIndex(IEnumerable<Result> results, int imageIndex)
        {
            if (results == null)
            {
                return;
            }

            foreach (Result result in results)
            {
                result.Others ??= new Dictionary<string, object>();
                result.Others[DefectPostProcessResultKeys.ImageIndex] = imageIndex;
            }
        }

        /// <summary>
        /// 读取结果扩展字段中的图像索引，缺失时返回 0。
        /// </summary>
        private static int GetResultImageIndex(Result result)
        {
            if (result?.Others != null
                && result.Others.TryGetValue(DefectPostProcessResultKeys.ImageIndex, out object value)
                && value != null)
            {
                try
                {
                    return Math.Max(0, Convert.ToInt32(value));
                }
                catch
                {
                    return 0;
                }
            }

            return 0;
        }

        /// <summary>
        /// 判断结果是否属于当前预览图。
        /// </summary>
        private bool IsResultOnCurrentPreviewImage(Result result)
        {
            return GetResultImageIndex(result) == CurrentPreviewImageIndex;
        }

        /// <summary>
        /// 按图像索引把结果重新组织为分组列表。
        /// </summary>
        private List<List<Result>> BuildResultGroupsByImageIndex(IEnumerable<Result> results)
        {
            List<Result> resultList = results?.Where(item => item != null).ToList() ?? new List<Result>();
            int groupCount = Math.Max(PreviewImageCount, SourceResultsByImage?.Count ?? 0);
            if (resultList.Count > 0)
            {
                groupCount = Math.Max(groupCount, resultList.Max(GetResultImageIndex) + 1);
            }

            List<List<Result>> groups = Enumerable.Range(0, groupCount)
                .Select(_ => new List<Result>())
                .ToList();

            foreach (Result result in resultList)
            {
                int imageIndex = GetResultImageIndex(result);
                while (groups.Count <= imageIndex)
                {
                    groups.Add(new List<Result>());
                }

                groups[imageIndex].Add(result);
            }

            return groups;
        }

        /// <summary>
        /// 输出输入结果摘要，辅助定位后处理输入链路问题。
        /// </summary>
        private void LogInputResultsSummary(object rawValue, List<Result> extractedResults)
        {
            List<Result> results = extractedResults ?? new List<Result>();
            int segReadyCount = results.Count(item => IsInitializedSafely(item?.Seg));
            int instanceSegCount = results.Count(item => item?.ModelType == eDeepLearningModelType.实例分割);
            int processableCount = GetProcessableResults(results).Count;
            string rawType = rawValue?.GetType().FullName ?? "null";

            LogTrace(
                $"Input results summary: RawType={rawType}, Extracted={results.Count}, " +
                $"InstanceSeg={instanceSegCount}, SegReady={segReadyCount}, Processable={processableCount}");

            int sampleCount = Math.Min(results.Count, 10);
            for (int i = 0; i < sampleCount; i++)
            {
                Result item = results[i];
                LogTrace(
                    $"Input result[{i}]: ClassId={item?.ClassId ?? 0}, " +
                    $"ClassName={item?.ClassName ?? "<null>"}, ModelType={item?.ModelType}, " +
                    $"SegReady={IsInitializedSafely(item?.Seg)}");
            }
        }

        /// <summary>
        /// 筛选后处理实际支持的缺陷结果，目前仅处理实例分割结果。
        /// </summary>
        internal List<Result> GetProcessableResults(IEnumerable<Result> source)
        {
            return source?
                .Where(item => item != null)
                .Where(IsProcessableResult)
                .ToList() ?? new List<Result>();
        }

        /// <summary>
        /// 判断单个结果是否是后处理支持的实例分割结果。
        /// </summary>
        private static bool IsProcessableResult(Result result)
        {
            if (result == null)
            {
                return false;
            }

            return result.ModelType == eDeepLearningModelType.实例分割;
        }

        /// <summary>
        /// 判断单个结果是否能绘制为预览缺陷区域。
        /// </summary>
        private static bool IsPreviewDrawableResult(Result result)
        {
            return IsProcessableResult(result) && IsInitializedSafely(result.Seg);
        }

        /// <summary>
        /// 安全判断 HALCON 对象是否已初始化。
        /// </summary>
        private static bool IsInitializedSafely(HObject hObject)
        {
            return HalconImageOwnership.IsInitializedSafe(hObject);
        }

        /// <summary>
        /// 通过访问首个对象验证 HALCON 图像是否仍可读。
        /// </summary>
        private static bool IsReadableImageSafely(HObject hObject)
        {
            if (!IsInitializedSafely(hObject))
            {
                return false;
            }

            HObject selected = null;
            try
            {
                int count = hObject.CountObj();
                if (count <= 0)
                {
                    return false;
                }

                HOperatorSet.SelectObj(hObject, out selected, 1);
                HOperatorSet.GetImageSize(selected, out HTuple width, out HTuple height);
                return width != null && height != null;
            }
            catch
            {
                return false;
            }
            finally
            {
                selected?.Dispose();
            }
        }


        /// <summary>
        /// 深拷贝 Result 及其 HALCON 对象，用于跨边界输出隔离。
        /// </summary>
        private static Result CloneResult(Result source)
        {
            Result target = new Result
            {
                Cx = source.Cx,
                Cy = source.Cy,
                Width = source.Width,
                Height = source.Height,
                Angle = source.Angle,
                Confidence = source.Confidence,
                ClassId = source.ClassId,
                ClassName = source.ClassName,
                ModelType = source.ModelType,
                Seg = SafeCloneHObject(source.Seg, "DefectPostProcess clone result Seg failed") ?? new HObject()
            };

            target.Kpt = new Keypoints
            {
                Thresh = source.Kpt?.Thresh ?? 0
            };

            if (source.Kpt?.Points != null)
            {
                target.Kpt.Points = source.Kpt.Points
                    .Select(item => new DeepLearningPoint
                    {
                        X = item.X,
                        Y = item.Y,
                        Confidence = item.Confidence
                    })
                    .ToList();
            }

            if (source.Kpt?.Skeletons != null)
            {
                target.Kpt.Skeletons = source.Kpt.Skeletons
                    .Select(item => new Skeleton
                    {
                        StartKptId = item.StartKptId,
                        EndKptId = item.EndKptId
                    })
                    .ToList();
            }

            if (source.Others != null)
            {
                target.Others = source.Others.ToDictionary(
                    item => item.Key,
                    item => item.Value is HObject hObject ? SafeCloneHObject(hObject, "DefectPostProcess clone result Others HObject failed") : item.Value);
            }

            return target;
        }

        /// <summary>
        /// 复制 HALCON 图像或区域对象；返回值由调用方持有并负责释放。
        /// </summary>
        private static HObject SafeCloneHObject(HObject hObject, string logPrefix)
        {
            if (hObject == null)
            {
                return null;
            }

            try
            {
                if (!hObject.IsInitialized())
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogStaticWarning($"{logPrefix}: {ex.Message}");
                return null;
            }

            Exception copyImageException = null;
            try
            {
                HOperatorSet.CopyImage(hObject, out HObject imageCopy);
                return imageCopy;
            }
            catch (Exception ex)
            {
                copyImageException = ex;
            }

            try
            {
                HOperatorSet.CopyObj(hObject, out HObject objectCopy, 1, -1);
                return objectCopy;
            }
            catch (Exception ex)
            {
                LogStaticWarning($"{logPrefix}: CopyImage={copyImageException?.Message}; CopyObj={ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 克隆对外发布值，避免输出边界暴露模块内部的 HALCON 对象。
        /// </summary>
        private static object CloneOutputValueForBoundary(object value)
        {
            if (value is HObject hObject)
            {
                return SafeCloneHObject(hObject, "DefectPostProcess clone input HObject failed");
            }

            if (value is Result result)
            {
                return CloneResult(result);
            }

            if (value is IEnumerable<IEnumerable<Result>> resultGroups)
            {
                return CloneResultGroups(resultGroups);
            }

            if (value is IEnumerable<Result> results)
            {
                return CloneResults(results);
            }

            return value;
        }

        /// <summary>
        /// 释放被替换的旧输出值，跳过仍被输入或新输出共享的对象。
        /// </summary>
        private static void DisposePreviousOutputValue(object previousValue, object sourceValue, object nextValue)
        {
            if (previousValue == null ||
                ReferenceEquals(previousValue, sourceValue) ||
                ReferenceEquals(previousValue, nextValue))
            {
                return;
            }

            if (previousValue is HObject hObject)
            {
                SafeDisposeHObject(hObject);
                return;
            }

            if (previousValue is Result result)
            {
                DisposeResult(result);
                return;
            }

            if (previousValue is IEnumerable<IEnumerable<Result>> resultGroups)
            {
                DisposeResultOutputs(null, resultGroups);
                return;
            }

            if (previousValue is IEnumerable<Result> results)
            {
                DisposeResultOutputs(results);
            }
        }

        /// <summary>
        /// 释放 Result 集合中由本模块克隆或持有的 HALCON 对象。
        /// </summary>
        private static void DisposeResultOutputs(
            IEnumerable<Result> results,
            IEnumerable<IEnumerable<Result>> resultGroups = null)
        {
            HashSet<Result> disposedResults = new HashSet<Result>();

            foreach (Result result in results ?? Enumerable.Empty<Result>())
            {
                DisposeResult(result, disposedResults);
            }

            foreach (IEnumerable<Result> group in resultGroups ?? Enumerable.Empty<IEnumerable<Result>>())
            {
                foreach (Result result in group ?? Enumerable.Empty<Result>())
                {
                    DisposeResult(result, disposedResults);
                }
            }
        }

        /// <summary>
        /// 释放单个 Result 中由本模块持有的 HALCON 对象。
        /// </summary>
        private static void DisposeResult(Result result)
        {
            DisposeResult(result, null);
        }

        /// <summary>
        /// 释放单个 Result 中由本模块持有的 HALCON 对象。
        /// </summary>
        private static void DisposeResult(Result result, ISet<Result> disposedResults)
        {
            if (result == null)
            {
                return;
            }

            if (disposedResults != null && !disposedResults.Add(result))
            {
                return;
            }

            SafeDisposeHObject(result.Seg);
            result.Seg = null;

            if (result.Others == null)
            {
                return;
            }

            foreach (HObject hObject in result.Others.Values.OfType<HObject>())
            {
                SafeDisposeHObject(hObject);
            }
        }

        /// <summary>
        /// 安全释放 HALCON 对象，忽略重复释放异常。
        /// </summary>
        private static void SafeDisposeHObject(HObject hObject)
        {
            try
            {
                hObject?.Dispose();
            }
            catch
            {
            }
        }

        #region 诊断日志

        /// <summary>
        /// 启动执行超时看门狗，只记录诊断日志，不中断主流程。
        /// </summary>
        private CancellationTokenSource StartPostProcessWatchdog(long runId)
        {
            var cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                    if (!token.IsCancellationRequested)
                    {
                        LogWarning(
                            $"Post-process execution exceeded 5 seconds: RunId={runId}, ImageReady={HasCurrentPreviewImage()}, " +
                            $"SourceResults={SourceResults?.Count ?? 0}, ResultsValue={_inputResults?.Value?.GetType().Name ?? "null"}, " +
                            $"CustomAlgorithm={HasEnabledCustomAlgorithm()}, FastMode={IsFastModeEnabled}");
                    }
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    LogTrace($"Post-process watchdog failed: RunId={runId}, Error={ex.Message}");
                }
            });

            return cts;
        }

        /// <summary>
        /// 生成单个输入链接参数的诊断文本。
        /// </summary>
        private static string DescribeDefectTransmitParamForLog(TransmitParam param)
        {
            return DescribeTransmitParamForDiagnostics(param);
        }

        /// <summary>
        /// 生成输入链接参数集合的诊断文本。
        /// </summary>
        private static string DescribeDefectTransmitParamsForLog(IEnumerable<TransmitParam> parameters)
        {
            if (parameters == null)
            {
                return "<null>";
            }

            return string.Join("; ", parameters.Select(DescribeDefectTransmitParamForLog));
        }

        /// <summary>
        /// 写入缺陷后处理 TRACE 日志。
        /// </summary>
        private void LogTrace(string message)
        {
            WriteLog("TRACE", message);
        }

        /// <summary>
        /// 写入缺陷后处理 INFO 日志。
        /// </summary>
        private void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// 写入缺陷后处理 WARN 日志。
        /// </summary>
        private void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        /// <summary>
        /// 写入缺陷后处理 ERROR 日志。
        /// </summary>
        private void LogError(string message)
        {
            WriteLog("ERROR", message);
        }

        /// <summary>
        /// 追加模块序号并写入实例诊断日志。
        /// </summary>
        private void WriteLog(string level, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            WriteStaticLog(level, $"[DefectPostProcess] {Serial:D3} {message}");
        }

        /// <summary>
        /// 写入不依赖模块实例的静态警告日志。
        /// </summary>
        private static void LogStaticWarning(string message)
        {
            WriteStaticLog("WARN", message);
        }

        /// <summary>
        /// 统一写入缺陷后处理日志，按环境变量过滤详细日志。
        /// </summary>
        private static void WriteStaticLog(string level, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string normalizedLevel = (level ?? string.Empty).Trim().ToUpperInvariant();
            if ((normalizedLevel == "TRACE" || normalizedLevel == "DEBUG" || normalizedLevel == "INFO")
                && !IsVerboseFlowLogEnabled())
            {
                return;
            }

            string formatted = message.StartsWith(LogPrefix, StringComparison.Ordinal)
                ? message
                : $"{LogPrefix} {message}";
            try
            {
                switch (normalizedLevel)
                {
                    case "WARN":
                    case "WARNING":
                        Logs.LogWarning(formatted);
                        break;
                    case "ERROR":
                        Logs.LogError(formatted);
                        break;
                    case "FATAL":
                        Logs.LogFatal(formatted);
                        break;
                    case "TRACE":
                    case "DEBUG":
                        Logs.LogTrace(formatted);
                        break;
                    default:
                        Logs.LogInfo(formatted);
                        break;
                }
            }
            catch
            {
                // 日志失败不能影响模块主流程。
            }
        }

        /// <summary>
        /// 判断是否开启详细流程日志环境变量。
        /// </summary>
        private static bool IsVerboseFlowLogEnabled()
        {
            string value = Environment.GetEnvironmentVariable("REEYIN_VERBOSE_FLOW_LOG");
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        /// <summary>
        /// 在 UI Dispatcher 上执行绑定集合和预览对象更新。
        /// </summary>
        private static void RunOnDispatcher(Action action)
        {
            if (action == null)
            {
                return;
            }

            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }

        #endregion
    }
}
