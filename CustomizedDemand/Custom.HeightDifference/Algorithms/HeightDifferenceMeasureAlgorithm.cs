using HalconDotNet;
using OpenCvSharp;
using System.IO;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;

namespace ReeYin.Customized.Algo.Algorithms
{
    public sealed class HeightDifferenceMeasureRequest
    {
        public HObject InputImage { get; set; } = new();

        /// <summary>
        /// 热力图预览图像的输出路径。
        /// </summary>
        public string HeatmapPreviewPath { get; set; } = string.Empty;

        /// <summary>
        /// X 方向相邻像素的物理间距，单位为毫米。
        /// </summary>
        public double IntervalX { get; set; } = 0.000117;

        /// <summary>
        /// Y 方向相邻像素的物理间距，单位为毫米。
        /// </summary>
        public double IntervalY { get; set; } = 0.000117;

        /// <summary>
        /// 高度原始值换算到物理高度的比例，单位为毫米。
        /// </summary>
        public double IntervalZ { get; set; } = 1.0;

        /// <summary>
        /// 自动测量前对高度图降采样的加速倍数。
        /// </summary>
        public double AccelerationFactor { get; set; } = 1.0;

        /// <summary>
        /// 无效高度灰度值的中心值，用于排除补洞或无数据区域。
        /// </summary>
        public double InvalidGrayCenter { get; set; } = 888888.0;

        /// <summary>
        /// 无效灰度中心值的允许偏差范围。
        /// </summary>
        public double InvalidGrayTolerance { get; set; } = 1.0;

        /// <summary>
        /// 自动测量区域在高度方向向内收缩的比例。
        /// </summary>
        public double ShrinkHeightRatio { get; set; } = 0.2;

        /// <summary>
        /// 自动测量区域在宽度方向向内收缩的比例。
        /// </summary>
        public double ShrinkWidthRatio { get; set; } = 0.6;

        /// <summary>
        /// 自动测量左侧区域向内平移的图像宽度比例。
        /// </summary>
        public double LeftRegionMoveRatio { get; set; } = 0.05;

        /// <summary>
        /// 自动测量右侧区域向内平移的图像宽度比例。
        /// </summary>
        public double RightRegionMoveRatio { get; set; } = 0.05;

        /// <summary>
        /// 均值计算时两端异常值的裁剪比例。
        /// </summary>
        public double TrimRatio { get; set; } = 0.05;
    }

    public sealed class HeightDifferenceMeasureResult
    {
        /// <summary>
        /// 两段曲线或两块区域平均高度的差值，按当前显示精度格式化输出。
        /// </summary>
        public double HeightDiff { get; set; }

        /// <summary>
        /// 热力图预览图像的输出路径。
        /// </summary>
        public string HeatmapPreviewPath { get; set; } = string.Empty;

        /// <summary>
        /// 热力图颜色映射使用的最小高度值。
        /// </summary>
        public double HeatmapRangeMin { get; set; }

        /// <summary>
        /// 热力图颜色映射使用的最大高度值。
        /// </summary>
        public double HeatmapRangeMax { get; set; }

        /// <summary>
        /// 本次测量高度数据的最小有效值。
        /// </summary>
        public double HeightRangeMin { get; set; }

        /// <summary>
        /// 本次测量高度数据的最大有效值。
        /// </summary>
        public double HeightRangeMax { get; set; }

        /// <summary>
        /// 自动测量左侧区域在原始高度图中的矩形坐标。
        /// </summary>
        public HeightDifferenceMeasureRectangle? LeftMeasureRectangle { get; set; }

        /// <summary>
        /// 自动测量右侧区域在原始高度图中的矩形坐标。
        /// </summary>
        public HeightDifferenceMeasureRectangle? RightMeasureRectangle { get; set; }
    }

    /// <summary>
    /// 自动测量矩形在图像坐标系中的上下左右边界。
    /// </summary>
    public sealed record HeightDifferenceMeasureRectangle(double Row1, double Col1, double Row2, double Col2);

    public sealed class HeightDifferenceMeasureAlgorithm
    {
        /// <summary>
        /// 执行自动高度差测量，并返回测量结果和热力图范围。
        /// </summary>
        public HeightDifferenceMeasureResult Measure(HeightDifferenceMeasureRequest request)
        {
            ValidateRequest(request);

            MeasurementAnalysisResult measurementResult = AnalyzeMeasurement(request);
            HeatmapRenderResult heatmapResult = RenderHeatmap(request);

            return new HeightDifferenceMeasureResult
            {
                HeightDiff = measurementResult.HeightDiff,
                HeatmapPreviewPath = heatmapResult.OutputPath,
                HeatmapRangeMin = heatmapResult.RangeMin,
                HeatmapRangeMax = heatmapResult.RangeMax,
                HeightRangeMin = heatmapResult.RangeMin,
                HeightRangeMax = heatmapResult.RangeMax,
                LeftMeasureRectangle = measurementResult.LeftRectangle,
                RightMeasureRectangle = measurementResult.RightRectangle
            };
        }

        /// <summary>
        /// 分割左右测量区域并计算两侧平均高度差。
        /// </summary>
        private static MeasurementAnalysisResult AnalyzeMeasurement(HeightDifferenceMeasureRequest request)
        {
            GetScaleFactors(request, out double scaleX, out double scaleY);

            HObject scaledImage;
            HObject reducedImage;
            HObject heightImageRaw;
            HObject fullRectangle;
            HObject irregularRegion;
            HObject validRegion;
            HObject heightImageScale;
            HObject darkRegion;
            HObject brightRegion;
            HObject darkConnectedRegion;
            HObject darkLargestRegion;
            HObject darkFilledRegion;
            HObject brightConnectedRegion;
            HObject brightLargestRegion;
            HObject brightFilledRegion;
            HObject mainRegions;
            HObject sortedMainRegions;
            HObject leftRegion;
            HObject rightRegion;

            HTuple scaledWidthTuple = new();
            HTuple scaledHeightTuple = new();
            HTuple sourceWidthTuple = new();
            HTuple sourceHeightTuple = new();
            HTuple numMainRegionsTuple = new();
            HTuple darkThresholdTuple = new();
            HTuple brightThresholdTuple = new();

            HOperatorSet.GenEmptyObj(out scaledImage);
            HOperatorSet.GenEmptyObj(out reducedImage);
            HOperatorSet.GenEmptyObj(out heightImageRaw);
            HOperatorSet.GenEmptyObj(out fullRectangle);
            HOperatorSet.GenEmptyObj(out irregularRegion);
            HOperatorSet.GenEmptyObj(out validRegion);
            HOperatorSet.GenEmptyObj(out heightImageScale);
            HOperatorSet.GenEmptyObj(out darkRegion);
            HOperatorSet.GenEmptyObj(out brightRegion);
            HOperatorSet.GenEmptyObj(out darkConnectedRegion);
            HOperatorSet.GenEmptyObj(out darkLargestRegion);
            HOperatorSet.GenEmptyObj(out darkFilledRegion);
            HOperatorSet.GenEmptyObj(out brightConnectedRegion);
            HOperatorSet.GenEmptyObj(out brightLargestRegion);
            HOperatorSet.GenEmptyObj(out brightFilledRegion);
            HOperatorSet.GenEmptyObj(out mainRegions);
            HOperatorSet.GenEmptyObj(out sortedMainRegions);
            HOperatorSet.GenEmptyObj(out leftRegion);
            HOperatorSet.GenEmptyObj(out rightRegion);

            HOperatorSet.GetImageSize(request.InputImage, out sourceWidthTuple, out sourceHeightTuple);

            var sourceWidth = sourceWidthTuple.I;
            var sourceHeight = sourceHeightTuple.I;

            HOperatorSet.GenRectangle1(out fullRectangle, 0, 0, sourceHeight - 1, sourceWidth - 1);
            HOperatorSet.Threshold(request.InputImage, out irregularRegion, request.InvalidGrayCenter - request.InvalidGrayTolerance, request.InvalidGrayCenter + request.InvalidGrayTolerance);
            HOperatorSet.Difference(fullRectangle, irregularRegion, out validRegion);
            HOperatorSet.ReduceDomain(request.InputImage, validRegion, out reducedImage);
            HOperatorSet.ZoomImageFactor(reducedImage, out scaledImage, scaleX, scaleY, "nearest_neighbor");

            try
            {
                HOperatorSet.GetImageSize(scaledImage, out scaledWidthTuple, out scaledHeightTuple);

                HOperatorSet.CopyImage(scaledImage, out heightImageRaw);
                heightImageScale = CreateByteScaledImage(heightImageRaw);

                HOperatorSet.BinaryThreshold(heightImageScale, out darkRegion, "max_separability", "dark", out darkThresholdTuple);
                HOperatorSet.BinaryThreshold(heightImageScale, out brightRegion, "max_separability", "light", out brightThresholdTuple);
                HOperatorSet.Connection(darkRegion, out darkConnectedRegion);
                HOperatorSet.SelectShapeStd(darkConnectedRegion, out darkLargestRegion, "max_area", 70);
                HOperatorSet.FillUp(darkLargestRegion, out darkFilledRegion);
                HOperatorSet.Connection(brightRegion, out brightConnectedRegion);
                HOperatorSet.SelectShapeStd(brightConnectedRegion, out brightLargestRegion, "max_area", 70);
                HOperatorSet.FillUp(brightLargestRegion, out brightFilledRegion);
                HOperatorSet.ConcatObj(darkFilledRegion, brightFilledRegion, out mainRegions);
                HOperatorSet.SortRegion(mainRegions, out sortedMainRegions, "first_point", "true", "column");


                HOperatorSet.CountObj(sortedMainRegions, out numMainRegionsTuple);
                var numMainRegions = numMainRegionsTuple.I;
                if (numMainRegions < 2)
                {
                    throw new InvalidOperationException($"主区域数量不足，当前仅检测到 {numMainRegions} 个区域。");
                }

                var leftRegionMovePx = (int)Math.Round(scaledWidthTuple.I * request.LeftRegionMoveRatio);
                var rightRegionMovePx = (int)Math.Round(scaledWidthTuple.I * request.RightRegionMoveRatio);

                HOperatorSet.SelectObj(sortedMainRegions, out leftRegion, 1);
                HOperatorSet.SelectObj(sortedMainRegions, out rightRegion, 2);


                var leftMeasure = MeasureSingleRegion(leftRegion, heightImageRaw, 1, scaledWidthTuple.I, request, leftRegionMovePx, rightRegionMovePx);

                var rightMeasure = MeasureSingleRegion(rightRegion, heightImageRaw, 2, scaledWidthTuple.I, request, leftRegionMovePx, rightRegionMovePx);

                var leftRectangle = MapRectangleToSourceImage(leftMeasure.MeasureRectangle, scaleX, scaleY, sourceWidth, sourceHeight);

                var rightRectangle = MapRectangleToSourceImage(rightMeasure.MeasureRectangle, scaleX, scaleY, sourceWidth, sourceHeight);

                return new MeasurementAnalysisResult(Math.Abs(rightMeasure.Mean - leftMeasure.Mean) * request.IntervalZ, leftRectangle, rightRectangle);
            }
            finally
            {
                leftRegion.Dispose();
                rightRegion.Dispose();

                sortedMainRegions.Dispose();
                mainRegions.Dispose();
                brightFilledRegion.Dispose();
                brightLargestRegion.Dispose();
                brightConnectedRegion.Dispose();
                brightRegion.Dispose();
                darkFilledRegion.Dispose();
                darkLargestRegion.Dispose();
                darkConnectedRegion.Dispose();
                darkRegion.Dispose();

                heightImageScale.Dispose();
                heightImageRaw.Dispose();
                scaledWidthTuple.Dispose();
                scaledHeightTuple.Dispose();

                scaledImage.Dispose();
                reducedImage.Dispose();
                validRegion.Dispose();
                irregularRegion.Dispose();
                fullRectangle.Dispose();
                darkThresholdTuple.Dispose();
                brightThresholdTuple.Dispose();
                numMainRegionsTuple.Dispose();
                sourceWidthTuple.Dispose();
                sourceHeightTuple.Dispose();
            }
        }

        /// <summary>
        /// 对单个自动测量区域收缩、平移后统计平均高度。
        /// </summary>
        private static SingleRegionMeasureResult MeasureSingleRegion(HObject region, HObject heightImageRaw, int regionIndex, int imageWidth,
            HeightDifferenceMeasureRequest request, int leftRegionMovePx, int rightRegionMovePx)
        {
            HTuple row1Tuple;
            HTuple col1Tuple;
            HTuple row2Tuple;
            HTuple col2Tuple;

            HObject innerRect;
            HObject measureRect;
            HObject shiftedRect;

            HOperatorSet.GenEmptyObj(out innerRect);
            HOperatorSet.GenEmptyObj(out measureRect);
            HOperatorSet.GenEmptyObj(out shiftedRect);


            HTuple measureRow1Tuple = new();
            HTuple measureCol1Tuple = new();
            HTuple measureRow2Tuple = new();
            HTuple measureCol2Tuple = new();
            HTuple rowsTuple = new();
            HTuple colsTuple = new();
            HTuple grayValuesTuple = new();

            HOperatorSet.InnerRectangle1(region, out row1Tuple, out col1Tuple, out row2Tuple, out col2Tuple);

            try
            {
                double row1 = row1Tuple.D;
                double col1 = col1Tuple.D;
                double row2 = row2Tuple.D;
                double col2 = col2Tuple.D;

                double heightPx = row2 - row1 + 1;
                double widthPx = col2 - col1 + 1;
                int shrinkHeightPx = Math.Max(1, (int)Math.Round(heightPx * request.ShrinkHeightRatio));
                int shrinkWidthPx = Math.Max(1, (int)Math.Round(widthPx * request.ShrinkWidthRatio));
                double erodeHeight = heightPx - shrinkHeightPx + 1;
                double erodeWidth = widthPx - shrinkWidthPx + 1;

                HOperatorSet.GenRectangle1(out innerRect, row1, col1, row2, col2);
                HOperatorSet.ErosionRectangle1(innerRect, out measureRect, erodeWidth, erodeHeight);
                HOperatorSet.SmallestRectangle1(measureRect, out measureRow1Tuple, out measureCol1Tuple, out measureRow2Tuple, out measureCol2Tuple);


                double measureRow1 = measureRow1Tuple.D;
                double measureCol1 = measureCol1Tuple.D;
                double measureRow2 = measureRow2Tuple.D;
                double measureCol2 = measureCol2Tuple.D;

                int colOffset = regionIndex == 1 ? -leftRegionMovePx : rightRegionMovePx;
                double shiftedCol1 = measureCol1 + colOffset;
                double shiftedCol2 = measureCol2 + colOffset;

                if (shiftedCol1 < 0)
                {
                    shiftedCol2 -= shiftedCol1;
                    shiftedCol1 = 0;
                }

                if (shiftedCol2 > imageWidth - 1)
                {
                    shiftedCol1 -= shiftedCol2 - (imageWidth - 1);
                    shiftedCol2 = imageWidth - 1;
                }

                shiftedCol1 = Math.Max(0, shiftedCol1);
                shiftedCol2 = Math.Min(imageWidth - 1, shiftedCol2);
                shiftedCol2 = Math.Max(shiftedCol1, shiftedCol2);

                HOperatorSet.GenRectangle1(out shiftedRect, measureRow1, shiftedCol1, measureRow2, shiftedCol2);
                HOperatorSet.GetRegionPoints(shiftedRect, out rowsTuple, out colsTuple);
                HOperatorSet.GetGrayval(heightImageRaw, rowsTuple, colsTuple, out grayValuesTuple);

                double mean = CalculateTrimmedMean(grayValuesTuple, request.TrimRatio);
                HeightDifferenceMeasureRectangle rectangle = new(measureRow1, shiftedCol1, measureRow2, shiftedCol2);
                return new SingleRegionMeasureResult(mean, rectangle);
            }
            finally
            {
                grayValuesTuple.Dispose();
                rowsTuple.Dispose();
                colsTuple.Dispose();
                shiftedRect.Dispose();
                measureRow1Tuple.Dispose();
                measureCol1Tuple.Dispose();
                measureRow2Tuple.Dispose();
                measureCol2Tuple.Dispose();
                measureRect.Dispose();
                innerRect.Dispose();
                row1Tuple.Dispose();
                col1Tuple.Dispose();
                row2Tuple.Dispose();
                col2Tuple.Dispose();
            }
        }

        /// <summary>
        /// 根据加速因子计算预览图到原图的 X/Y 缩放比例。
        /// </summary>
        private static void GetScaleFactors(HeightDifferenceMeasureRequest request, out double scaleX, out double scaleY)
        {
            scaleX = 1.0;
            scaleY = 1.0;

            if (request.IntervalX < request.IntervalY)
            {
                scaleY = request.IntervalY / request.IntervalX;
            }
            else if (request.IntervalX > request.IntervalY)
            {
                scaleX = request.IntervalX / request.IntervalY;
            }
        }

        /// <summary>
        /// 把降采样图上的测量矩形映射回原始图像坐标。
        /// </summary>
        private static HeightDifferenceMeasureRectangle MapRectangleToSourceImage(HeightDifferenceMeasureRectangle rectangle, double scaleX, double scaleY, int sourceWidth, int sourceHeight)
        {
            int row1 = Math.Clamp((int)Math.Round(rectangle.Row1 / scaleY), 0, sourceHeight - 1);
            int col1 = Math.Clamp((int)Math.Round(rectangle.Col1 / scaleX), 0, sourceWidth - 1);
            int row2 = Math.Clamp((int)Math.Round(rectangle.Row2 / scaleY), 0, sourceHeight - 1);
            int col2 = Math.Clamp((int)Math.Round(rectangle.Col2 / scaleX), 0, sourceWidth - 1);

            if (row2 < row1)
            {
                row2 = row1;
            }

            if (col2 < col1)
            {
                col2 = col1;
            }

            return new HeightDifferenceMeasureRectangle(row1, col1, row2, col2);
        }

        /// <summary>
        /// 将原始高度图按灰度范围缩放为 8 位图用于阈值分割。
        /// </summary>
        private static HObject CreateByteScaledImage(HObject heightImageRaw)
        {
            HTuple minTuple;
            HTuple maxTuple;
            HTuple rangeTuple;
            HObject scaledImage;

            HOperatorSet.MinMaxGray(heightImageRaw, heightImageRaw, 0, out minTuple, out maxTuple, out rangeTuple);

            try
            {
                double min = minTuple.D;
                double max = maxTuple.D;

                if (max > min)
                {
                    HOperatorSet.ScaleImage(heightImageRaw, out scaledImage, 255.0 / (max - min), -min * 255.0 / (max - min));
                }
                else
                {
                    HOperatorSet.CopyImage(heightImageRaw, out scaledImage);
                }

                try
                {
                    HObject byteImage;
                    HOperatorSet.ConvertImageType(scaledImage, out byteImage, "byte");
                    return byteImage;
                }
                finally
                {
                    scaledImage.Dispose();
                }
            }
            finally
            {
                minTuple.Dispose();
                maxTuple.Dispose();
                rangeTuple.Dispose();
            }
        }

        /// <summary>
        /// 对有效高度值排序并裁剪两端异常值后计算均值。
        /// </summary>
        private static double CalculateTrimmedMean(HTuple grayValues, double trimRatio)
        {
            HTuple middleGrayValues = new();
            HTuple graySumTuple = new();

            HTuple sortedGrayValues;
            HOperatorSet.TupleSort(grayValues, out sortedGrayValues);

            try
            {
                int count = sortedGrayValues.Length;
                int trimCount = (int)Math.Floor(count * trimRatio);
                int start = trimCount;
                int end = count - trimCount - 1;

                if (end >= start)
                {
                    HOperatorSet.TupleSelectRange(sortedGrayValues, start, end, out middleGrayValues);
                }
                else
                {
                    middleGrayValues = sortedGrayValues.Clone();
                }
                HOperatorSet.TupleSum(middleGrayValues, out graySumTuple);

                return middleGrayValues.Length > 0 ? graySumTuple.D / middleGrayValues.Length : 0.0;
            }
            finally
            {
                graySumTuple.Dispose();
                middleGrayValues.Dispose();
                sortedGrayValues.Dispose();
            }
        }

        /// <summary>
        /// 输出不带测量框的热力图预览，测量框由结果选中回显逻辑单独绘制。
        /// </summary>
        private static HeatmapRenderResult RenderHeatmap(HeightDifferenceMeasureRequest request)
        {
            using Mat sourceMat = ConvertHalconImageToMat(request.InputImage);
            using Mat floatMat = new();
            using Mat invalidMask = new();
            using Mat validMask = new();
            using Mat shiftedMat = new();
            using Mat normalizedMat = new();
            using Mat heatmapMat = new();
            using Mat canvas = new();
            using Mat colorBarGray = CreateColorBarGray(sourceMat.Rows, 28);
            using Mat colorBarMat = new();

            sourceMat.ConvertTo(floatMat, MatType.CV_32FC1);

            Cv2.InRange(
                floatMat,
                new Scalar(request.InvalidGrayCenter - request.InvalidGrayTolerance),
                new Scalar(request.InvalidGrayCenter + request.InvalidGrayTolerance),
                invalidMask);
            Cv2.Compare(invalidMask, Scalar.All(0), validMask, CmpType.EQ);

            if (Cv2.CountNonZero(validMask) == 0)
            {
                throw new InvalidOperationException("热力图渲染失败：输入图像中没有有效像素。");
            }

            Cv2.MinMaxLoc(floatMat, out double minRawValue, out double maxRawValue, out Point _, out Point _, validMask);
            double displayMinRawValue = minRawValue;
            double displayMaxRawValue = maxRawValue;

            if (Math.Abs(maxRawValue - minRawValue) < 1e-12)
            {
                maxRawValue = minRawValue + 1.0;
            }

            shiftedMat.Create(floatMat.Size(), MatType.CV_32FC1);
            shiftedMat.SetTo(Scalar.All(0));

            Cv2.Subtract(floatMat, Scalar.All(minRawValue), shiftedMat, validMask, -1);
            Cv2.Multiply(shiftedMat, Scalar.All(255.0 / (maxRawValue - minRawValue)), shiftedMat, 1, -1);
            shiftedMat.ConvertTo(normalizedMat, MatType.CV_8UC1);
            normalizedMat.SetTo(0, invalidMask);

            Cv2.ApplyColorMap(normalizedMat, heatmapMat, ColormapTypes.Jet);
            heatmapMat.SetTo(Scalar.Black, invalidMask);

            Cv2.ApplyColorMap(colorBarGray, colorBarMat, ColormapTypes.Jet);

            int imageX = 20;
            int imageY = 20;
            int colorBarX = imageX + heatmapMat.Cols + 30;
            int colorBarY = imageY;

            canvas.Create(floatMat.Rows + 40, floatMat.Cols + 170, MatType.CV_8UC3);
            canvas.SetTo(Scalar.All(255));

            heatmapMat.CopyTo(new Mat(canvas, new Rect(imageX, imageY, heatmapMat.Cols, heatmapMat.Rows)));
            colorBarMat.CopyTo(new Mat(canvas, new Rect(colorBarX, colorBarY, colorBarMat.Cols, colorBarMat.Rows)));

            Cv2.Rectangle(canvas, new Rect(imageX, imageY, heatmapMat.Cols, heatmapMat.Rows), Scalar.Black, 1);
            Cv2.Rectangle(canvas, new Rect(colorBarX, colorBarY, colorBarMat.Cols, colorBarMat.Rows), Scalar.Black, 1);

            double rangeMin = displayMinRawValue * request.IntervalZ;
            double rangeMax = displayMaxRawValue * request.IntervalZ;
            double rangeMid = (rangeMin + rangeMax) * 0.5;

            DrawColorBarLabels(canvas, colorBarX, colorBarY, colorBarMat.Rows, colorBarMat.Cols, rangeMin, rangeMid, rangeMax);

            string outputDirectory = Path.GetDirectoryName(request.HeatmapPreviewPath) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            if (!Cv2.ImWrite(request.HeatmapPreviewPath, canvas))
            {
                throw new InvalidOperationException($"热力图临时预览图保存失败：{request.HeatmapPreviewPath}");
            }

            return new HeatmapRenderResult(request.HeatmapPreviewPath, rangeMin, rangeMax);
        }

        /// <summary>
        /// 将 HALCON 灰度图复制为 OpenCV Mat 以便伪彩渲染。
        /// </summary>
        private static Mat ConvertHalconImageToMat(HObject image)
        {
            HTuple pointerTuple;
            HTuple typeTuple;
            HTuple widthTuple;
            HTuple heightTuple;

            HOperatorSet.GetImagePointer1(image, out pointerTuple, out typeTuple, out widthTuple, out heightTuple);

            try
            {
                int width = widthTuple.I;
                int height = heightTuple.I;
                IntPtr pointer = pointerTuple.IP;
                string imageType = typeTuple.S;

                MatType matType = imageType switch
                {
                    "byte" => MatType.CV_8UC1,
                    "int2" => MatType.CV_16SC1,
                    "uint2" => MatType.CV_16UC1,
                    "int4" => MatType.CV_32SC1,
                    "real" => MatType.CV_32FC1,
                    _ => throw new NotSupportedException($"不支持的 HALCON 图像类型：{imageType}")
                };

                return Mat.FromPixelData(height, width, matType, pointer, 0).Clone();
            }
            finally
            {
                pointerTuple.Dispose();
                typeTuple.Dispose();
                widthTuple.Dispose();
                heightTuple.Dispose();
            }
        }
        /// <summary>
        /// 生成从高到低渐变的灰度色带图像。
        /// </summary>
        private static Mat CreateColorBarGray(int height, int width)
        {
            Mat gray = new(height, width, MatType.CV_8UC1);

            for (int y = 0; y < height; y++)
            {
                byte value = (byte)Math.Round(255.0 - (255.0 * y / Math.Max(height - 1, 1)));
                gray.Row(y).SetTo(value);
            }

            return gray;
        }

        /// <summary>
        /// 在热力图色带旁绘制最大、中间和最小高度标签。
        /// </summary>
        private static void DrawColorBarLabels(
            Mat canvas,
            int colorBarX,
            int colorBarY,
            int colorBarHeight,
            int colorBarWidth,
            double rangeMin,
            double rangeMid,
            double rangeMax)
        {
            int textX = colorBarX + colorBarWidth + 12;
            int topY = colorBarY + 8;
            int midY = colorBarY + colorBarHeight / 2 + 5;
            int bottomY = colorBarY + colorBarHeight - 4;

            Cv2.PutText(canvas, $"{rangeMax:F3} mm", new Point(textX, topY), HersheyFonts.HersheySimplex, 0.5, Scalar.Black, 2);
            Cv2.PutText(canvas, $"{rangeMid:F3} mm", new Point(textX, midY), HersheyFonts.HersheySimplex, 0.5, Scalar.Black, 2);
            Cv2.PutText(canvas, $"{rangeMin:F3} mm", new Point(textX, bottomY), HersheyFonts.HersheySimplex, 0.5, Scalar.Black, 2);
            Cv2.PutText(canvas, "Range (mm)", new Point(colorBarX - 2, colorBarY - 8), HersheyFonts.HersheySimplex, 0.5, Scalar.Black, 2);
        }
        /// <summary>
        /// 校验高度差测量或热力图生成所需的输入图像和参数。
        /// </summary>
        private static void ValidateRequest(HeightDifferenceMeasureRequest request)
        {
            if (request.InputImage is null || !request.InputImage.IsInitialized())
            {
                throw new ArgumentException("输入图像不能为空，且必须是已初始化的 HObject。", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.HeatmapPreviewPath))
            {
                throw new ArgumentException("热力图临时预览路径不能为空。", nameof(request));
            }

            if (request.IntervalX <= 0 || request.IntervalY <= 0 || request.IntervalZ <= 0)
            {
                throw new ArgumentException("X/Y 像素间距和 Z 向单位换算系数必须大于 0。", nameof(request));
            }

            if (request.AccelerationFactor <= 0)
            {
                throw new ArgumentException("AccelerationFactor 必须大于 0。", nameof(request));
            }

            if (request.ShrinkHeightRatio <= 0 || request.ShrinkHeightRatio > 1)
            {
                throw new ArgumentException("ShrinkHeightRatio 必须在 (0, 1] 范围内。", nameof(request));
            }

            if (request.ShrinkWidthRatio <= 0 || request.ShrinkWidthRatio > 1)
            {
                throw new ArgumentException("ShrinkWidthRatio 必须在 (0, 1] 范围内。", nameof(request));
            }

            if (request.LeftRegionMoveRatio < 0 || request.RightRegionMoveRatio < 0)
            {
                throw new ArgumentException("左右区域移动比例不能小于 0。", nameof(request));
            }

            if (request.TrimRatio < 0 || request.TrimRatio >= 0.5)
            {
                throw new ArgumentException("TrimRatio 必须在 [0, 0.5) 范围内。", nameof(request));
            }
        }

        /// <summary>
        /// 保存热力图预览路径和颜色映射范围。
        /// </summary>
        private sealed record HeatmapRenderResult(string OutputPath, double RangeMin, double RangeMax);

        /// <summary>
        /// 保存自动测量高度差及左右区域矩形。
        /// </summary>
        private sealed record MeasurementAnalysisResult(double HeightDiff, HeightDifferenceMeasureRectangle LeftRectangle, HeightDifferenceMeasureRectangle RightRectangle);

        /// <summary>
        /// 保存单个区域的平均高度和实际测量矩形。
        /// </summary>
        private sealed record SingleRegionMeasureResult(double Mean, HeightDifferenceMeasureRectangle MeasureRectangle);
    }

    /// <summary>
    /// PCD 或高度图 Z 向数值本身使用的物理单位。
    /// </summary>
    public enum HeightDifferenceZValueUnit
    {
        mm,
        m,
        um
    }

    public static class HeightDifferenceZValueUnitExtensions
    {
        /// <summary>
        /// 返回一个原始 Z 数值按当前单位换算为毫米时需要乘的系数。
        /// </summary>
        public static double ToMillimeterFactor(this HeightDifferenceZValueUnit unit)
        {
            return unit switch
            {
                HeightDifferenceZValueUnit.mm => 1.0,
                HeightDifferenceZValueUnit.m => 1000.0,
                HeightDifferenceZValueUnit.um => 0.001,
                _ => 1.0
            };
        }

        /// <summary>
        /// 返回一个原始 Z 数值结合数值系数和单位后换算为毫米时需要乘的系数。
        /// </summary>
        public static double ToMillimeterFactor(this HeightDifferenceZValueUnit unit, double valueScale)
        {
            double normalizedScale = double.IsFinite(valueScale) && valueScale > 0
                ? valueScale
                : 1.0;
            return normalizedScale * unit.ToMillimeterFactor();
        }
    }

    internal static class HeightDifferenceHeatmapRenderer
    {
        /// <summary>
        /// 按高度范围生成伪彩热力图和色带预览结果。
        /// </summary>
        public static HeightDifferenceMeasureResult GenerateHeatmap(HeightDifferenceMeasureRequest request)
        {
            ValidateRequest(request);

            using Mat sourceMat = ConvertHalconImageToMat(request.InputImage);
            using Mat floatMat = new();
            using Mat invalidMask = new();
            using Mat validMask = new();
            using Mat shiftedMat = new();
            using Mat normalizedMat = new();
            using Mat heatmapMat = new();
            using Mat canvas = new();
            using Mat colorBarGray = CreateColorBarGray(sourceMat.Rows, 28);
            using Mat colorBarMat = new();

            sourceMat.ConvertTo(floatMat, MatType.CV_32FC1);

            Cv2.InRange(
                floatMat,
                new Scalar(request.InvalidGrayCenter - request.InvalidGrayTolerance),
                new Scalar(request.InvalidGrayCenter + request.InvalidGrayTolerance),
                invalidMask);
            Cv2.Compare(invalidMask, Scalar.All(0), validMask, CmpType.EQ);

            if (Cv2.CountNonZero(validMask) == 0)
            {
                throw new InvalidOperationException("热力图渲染失败：输入图像中没有有效像素。");
            }

            Cv2.MinMaxLoc(floatMat, out double minRawValue, out double maxRawValue, out Point _, out Point _, validMask);
            double displayMinRawValue = minRawValue;
            double displayMaxRawValue = maxRawValue;
            if (Math.Abs(maxRawValue - minRawValue) < 1e-12)
            {
                maxRawValue = minRawValue + 1.0;
            }

            shiftedMat.Create(floatMat.Size(), MatType.CV_32FC1);
            shiftedMat.SetTo(Scalar.All(0));

            Cv2.Subtract(floatMat, Scalar.All(minRawValue), shiftedMat, validMask, -1);
            Cv2.Multiply(shiftedMat, Scalar.All(255.0 / (maxRawValue - minRawValue)), shiftedMat, 1, -1);
            shiftedMat.ConvertTo(normalizedMat, MatType.CV_8UC1);
            normalizedMat.SetTo(0, invalidMask);

            Cv2.ApplyColorMap(normalizedMat, heatmapMat, ColormapTypes.Jet);
            heatmapMat.SetTo(Scalar.Black, invalidMask);

            Cv2.ApplyColorMap(colorBarGray, colorBarMat, ColormapTypes.Jet);

            int imageX = 20;
            int imageY = 20;
            int colorBarX = imageX + heatmapMat.Cols + 30;
            int colorBarY = imageY;

            canvas.Create(floatMat.Rows + 40, floatMat.Cols + 170, MatType.CV_8UC3);
            canvas.SetTo(Scalar.All(255));

            heatmapMat.CopyTo(new Mat(canvas, new Rect(imageX, imageY, heatmapMat.Cols, heatmapMat.Rows)));
            colorBarMat.CopyTo(new Mat(canvas, new Rect(colorBarX, colorBarY, colorBarMat.Cols, colorBarMat.Rows)));

            Cv2.Rectangle(canvas, new Rect(imageX, imageY, heatmapMat.Cols, heatmapMat.Rows), Scalar.Black, 1);
            Cv2.Rectangle(canvas, new Rect(colorBarX, colorBarY, colorBarMat.Cols, colorBarMat.Rows), Scalar.Black, 1);

            double rangeMin = displayMinRawValue * request.IntervalZ;
            double rangeMax = displayMaxRawValue * request.IntervalZ;
            double rangeMid = (rangeMin + rangeMax) * 0.5;

            DrawColorBarLabels(canvas, colorBarX, colorBarY, colorBarMat.Rows, colorBarMat.Cols, rangeMin, rangeMid, rangeMax);

            string outputDirectory = Path.GetDirectoryName(request.HeatmapPreviewPath) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            if (!Cv2.ImWrite(request.HeatmapPreviewPath, canvas))
            {
                throw new InvalidOperationException($"热力图临时预览图保存失败：{request.HeatmapPreviewPath}");
            }

            return new HeightDifferenceMeasureResult
            {
                HeightDiff = double.NaN,
                HeatmapPreviewPath = request.HeatmapPreviewPath,
                HeatmapRangeMin = rangeMin,
                HeatmapRangeMax = rangeMax,
                HeightRangeMin = rangeMin,
                HeightRangeMax = rangeMax
            };
        }

        /// <summary>
        /// 校验高度差测量或热力图生成所需的输入图像和参数。
        /// </summary>
        private static void ValidateRequest(HeightDifferenceMeasureRequest request)
        {
            if (request.InputImage is null || !request.InputImage.IsInitialized())
            {
                throw new ArgumentException("输入图像不能为空，且必须是已初始化的 HObject。", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.HeatmapPreviewPath))
            {
                throw new ArgumentException("热力图临时预览路径不能为空。", nameof(request));
            }

            if (request.IntervalX <= 0 || request.IntervalY <= 0 || request.IntervalZ <= 0)
            {
                throw new ArgumentException("X/Y 像素间距和 Z 向单位换算系数必须大于 0。", nameof(request));
            }
        }

        /// <summary>
        /// 将 HALCON 灰度图复制为 OpenCV Mat 以便伪彩渲染。
        /// </summary>
        private static Mat ConvertHalconImageToMat(HObject image)
        {
            HOperatorSet.GetImagePointer1(image, out HTuple pointerTuple, out HTuple typeTuple, out HTuple widthTuple, out HTuple heightTuple);

            try
            {
                int width = widthTuple.I;
                int height = heightTuple.I;
                IntPtr pointer = pointerTuple.IP;
                string imageType = typeTuple.S;

                MatType matType = imageType switch
                {
                    "byte" => MatType.CV_8UC1,
                    "int2" => MatType.CV_16SC1,
                    "uint2" => MatType.CV_16UC1,
                    "int4" => MatType.CV_32SC1,
                    "real" => MatType.CV_32FC1,
                    _ => throw new NotSupportedException($"不支持的 HALCON 图像类型：{imageType}")
                };

                return Mat.FromPixelData(height, width, matType, pointer, 0).Clone();
            }
            finally
            {
                pointerTuple.Dispose();
                typeTuple.Dispose();
                widthTuple.Dispose();
                heightTuple.Dispose();
            }
        }

        /// <summary>
        /// 生成从高到低渐变的灰度色带图像。
        /// </summary>
        private static Mat CreateColorBarGray(int height, int width)
        {
            Mat gray = new(height, width, MatType.CV_8UC1);

            for (int y = 0; y < height; y++)
            {
                byte value = (byte)Math.Round(255.0 - (255.0 * y / Math.Max(height - 1, 1)));
                gray.Row(y).SetTo(value);
            }

            return gray;
        }

        /// <summary>
        /// 在热力图色带旁绘制最大、中间和最小高度标签。
        /// </summary>
        private static void DrawColorBarLabels(
            Mat canvas,
            int colorBarX,
            int colorBarY,
            int colorBarHeight,
            int colorBarCols,
            double rangeMin,
            double rangeMid,
            double rangeMax)
        {
            int textX = colorBarX + colorBarCols + 12;
            int topY = colorBarY + 8;
            int midY = colorBarY + colorBarHeight / 2 + 5;
            int bottomY = colorBarY + colorBarHeight - 4;

            Cv2.PutText(canvas, $"{rangeMax:F3} mm", new Point(textX, topY), HersheyFonts.HersheySimplex, 0.5, Scalar.Black, 2);
            Cv2.PutText(canvas, $"{rangeMid:F3} mm", new Point(textX, midY), HersheyFonts.HersheySimplex, 0.5, Scalar.Black, 2);
            Cv2.PutText(canvas, $"{rangeMin:F3} mm", new Point(textX, bottomY), HersheyFonts.HersheySimplex, 0.5, Scalar.Black, 2);
            Cv2.PutText(canvas, "Range (mm)", new Point(colorBarX - 2, colorBarY - 8), HersheyFonts.HersheySimplex, 0.5, Scalar.Black, 2);
        }
    }
}
