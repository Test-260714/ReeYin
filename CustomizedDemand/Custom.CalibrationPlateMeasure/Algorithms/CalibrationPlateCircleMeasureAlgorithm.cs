using HalconDotNet;

namespace Custom.CalibrationPlateMeasure.Algorithms
{
    // 圆测量算法：复刻 V2 HDEV 的圆轮廓拟合和内外环深度计算。
    public sealed class CalibrationPlateCircleMeasureAlgorithm
    {
        private const double CenterOffsetWeight = 0.1;

        public CalibrationPlateMeasureResult Measure(CalibrationPlateMeasureRequest request)
        {
            ValidateRequest(request);
            GetHdevScaleFactors(request, out double scaleX, out double scaleY, out double xp, out double yp, out double zp);

            HObject heightImage = new();
            HObject validMask = new();
            HObject irregularRegion = new();
            HObject irregularRegion0 = new();
            HObject fullRectangle = new();
            HObject irregularMask = new();

            try
            {
                HOperatorSet.ZoomImageFactor(request.InputImage, out heightImage, scaleX, scaleY, "nearest_neighbor");
                HOperatorSet.Threshold(heightImage, out validMask, 0, 1000);
                HOperatorSet.GenEmptyObj(out irregularRegion);
                HOperatorSet.Threshold(heightImage, out irregularRegion0, 0, 0);
                HOperatorSet.ConcatObj(irregularRegion, irregularRegion0, out irregularRegion);
                HOperatorSet.GetImageSize(heightImage, out HTuple imgWidthTuple, out HTuple imgHeightTuple);
                int imgWidth = imgWidthTuple.I;
                int imgHeight = imgHeightTuple.I;

                HOperatorSet.GenRectangle1(out fullRectangle, 0, 0, imgHeight, imgWidth);
                HOperatorSet.Union1(irregularRegion, out irregularMask);
                HOperatorSet.Difference(fullRectangle, irregularMask, out fullRectangle);
                HOperatorSet.Intersection(validMask, fullRectangle, out validMask);
                HOperatorSet.ReduceDomain(heightImage, validMask, out heightImage);

                GetSelectedRoi(request, imgWidth, imgHeight, out double roiRow1, out double roiColumn1, out double roiRow2, out double roiColumn2);

                CalibrationPlateMeasureResult result = new();
                try
                {
                    result.DisplayImage.Dispose();
                    result.DisplayImage = CreateDisplayImage(heightImage, validMask);
                    result.HeightImage.Dispose();
                    HOperatorSet.CopyImage(heightImage, out HObject resultHeightImage);
                    result.HeightImage = resultHeightImage;
                    result.DisplayPixelSizeX = xp;
                    result.DisplayPixelSizeY = yp;
                    result.DisplayPixelSizeZ = zp;

                    double circlePixelSizeMm = (xp + yp) / 2.0;
                    TryMeasureCircleRoi(
                        heightImage,
                        validMask,
                        imgWidth,
                        imgHeight,
                        zp,
                        circlePixelSizeMm,
                        roiRow1,
                        roiColumn1,
                        roiRow2,
                        roiColumn2,
                        request,
                        result);

                    return result;
                }
                catch
                {
                    result.Dispose();
                    throw;
                }
            }
            finally
            {
                heightImage.Dispose();
                validMask.Dispose();
                irregularRegion.Dispose();
                irregularRegion0.Dispose();
                fullRectangle.Dispose();
                irregularMask.Dispose();
            }
        }

        private static void TryMeasureCircleRoi(
            HObject heightImage,
            HObject validMask,
            int imgWidth,
            int imgHeight,
            double zp,
            double circlePixelSizeMm,
            double roiRow1,
            double roiColumn1,
            double roiRow2,
            double roiColumn2,
            CalibrationPlateMeasureRequest request,
            CalibrationPlateMeasureResult result)
        {
            double roiCenterRow = (roiRow1 + roiRow2) / 2.0;
            double roiCenterColumn = (roiColumn1 + roiColumn2) / 2.0;

            HObject cropRectangle = new();
            HObject cropImageReduced = new();
            HObject croppedImage = new();
            HObject croppedHeightImage = new();
            HObject edges = new();
            HObject circleContours = new();
            HObject originalCircleContour = new();
            HObject originalDepthMeasureRegionRaw = new();
            HObject originalImageRegion = new();
            HObject originalDepthMeasureRegion = new();
            HObject originalDepthMeasureRegionDilation1 = new();
            HObject originalDepthMeasureRegionDilation2 = new();
            HObject originalDepthMeasureCircleRegion = new();

            try
            {
                HOperatorSet.GenRectangle1(out cropRectangle, roiRow1, roiColumn1, roiRow2, roiColumn2);
                HOperatorSet.ReduceDomain(heightImage, cropRectangle, out cropImageReduced);
                HOperatorSet.CropDomain(cropImageReduced, out croppedImage);

                HOperatorSet.MinMaxGray(cropRectangle, heightImage, 0, out HTuple minValueTuple, out HTuple maxValueTuple, out _);
                double minValue = minValueTuple.D;
                double maxValue = maxValueTuple.D;
                double range = Math.Max(maxValue - minValue, 1e-9);
                double mult = 255.0 / range;
                double add = -minValue * mult;
                HOperatorSet.CopyImage(croppedImage, out croppedHeightImage);
                HOperatorSet.ScaleImage(croppedHeightImage, out croppedImage, mult, add);

                HOperatorSet.EdgesSubPix(croppedImage, out edges, "canny", 0.5, 20, 40);
                HOperatorSet.SelectContoursXld(edges, out circleContours, "contour_length", request.CircleMinContourLength, 999999, -0.5, 0.5);
                HOperatorSet.CountObj(circleContours, out HTuple circleContourCountTuple);
                if (circleContourCountTuple.I < 1)
                {
                    return;
                }

                HOperatorSet.FitCircleContourXld(
                    circleContours,
                    "geotukey",
                    -1,
                    0,
                    0,
                    3,
                    2,
                    out HTuple circleRows,
                    out HTuple circleColumns,
                    out HTuple circleRadii,
                    out _,
                    out _,
                    out _);

                HOperatorSet.GetImageSize(croppedImage, out HTuple cropWidthTuple, out HTuple cropHeightTuple);
                int cropWidth = cropWidthTuple.I;
                int cropHeight = cropHeightTuple.I;
                double maxCircleRadiusPx = Math.Sqrt(cropWidth * cropWidth + cropHeight * cropHeight) / 2.0;

                int bestCircleIndex = -1;
                double bestCircleScore = -1.0;
                int fallbackCircleIndex = -1;
                double fallbackCircleRadius = -1.0;
                for (int circleIndex = 0; circleIndex < circleRadii.Length; circleIndex++)
                {
                    // 优先选择满足最小半径且靠近 ROI 中心的大圆，保留最大有效圆作为兜底。
                    double currentCircleRow = GetD(circleRows, circleIndex);
                    double currentCircleColumn = GetD(circleColumns, circleIndex);
                    double currentCircleRadius = GetD(circleRadii, circleIndex);
                    double currentOriginalCircleRow = currentCircleRow + roiRow1;
                    double currentOriginalCircleColumn = currentCircleColumn + roiColumn1;
                    double currentCircleCenterOffsetPx = Math.Sqrt(
                        Square(currentOriginalCircleRow - roiCenterRow) +
                        Square(currentOriginalCircleColumn - roiCenterColumn));

                    bool isValidCircle =
                        currentCircleRadius > 0 &&
                        currentCircleRadius <= maxCircleRadiusPx &&
                        currentCircleRow >= 0 &&
                        currentCircleRow <= cropHeight - 1 &&
                        currentCircleColumn >= 0 &&
                        currentCircleColumn <= cropWidth - 1;
                    if (!isValidCircle)
                    {
                        continue;
                    }

                    if (currentCircleRadius > fallbackCircleRadius)
                    {
                        fallbackCircleRadius = currentCircleRadius;
                        fallbackCircleIndex = circleIndex;
                    }

                    if (currentCircleRadius < request.CircleMinRadiusPx)
                    {
                        continue;
                    }

                    double circleScore = currentCircleRadius - CenterOffsetWeight * currentCircleCenterOffsetPx;
                    if (circleScore > bestCircleScore)
                    {
                        bestCircleScore = circleScore;
                        bestCircleIndex = circleIndex;
                    }
                }

                if (bestCircleIndex < 0)
                {
                    bestCircleIndex = fallbackCircleIndex;
                }

                if (bestCircleIndex < 0)
                {
                    return;
                }

                double circleRow = GetD(circleRows, bestCircleIndex);
                double circleColumn = GetD(circleColumns, bestCircleIndex);
                double circleRadiusPx = GetD(circleRadii, bestCircleIndex);
                double originalCircleRow = circleRow + roiRow1;
                double originalCircleColumn = circleColumn + roiColumn1;
                double circleRadiusMm = circleRadiusPx * circlePixelSizeMm;
                double circleDiameterMm = circleRadiusMm * 2.0;

                HOperatorSet.GenCircleContourXld(
                    out originalCircleContour,
                    originalCircleRow,
                    originalCircleColumn,
                    circleRadiusPx,
                    0,
                    2.0 * Math.PI,
                    "positive",
                    1.0);
                HOperatorSet.GenCircle(
                    out originalDepthMeasureRegionRaw,
                    originalCircleRow,
                    originalCircleColumn,
                    circleRadiusPx + request.CircleDepthExpandPx);
                HOperatorSet.GenRectangle1(out originalImageRegion, 0, 0, imgHeight - 1, imgWidth - 1);
                HOperatorSet.Intersection(originalDepthMeasureRegionRaw, originalImageRegion, out originalDepthMeasureRegion);
                HOperatorSet.Intersection(originalDepthMeasureRegion, validMask, out originalDepthMeasureRegion);
                HOperatorSet.AreaCenter(originalDepthMeasureRegion, out HTuple originalDepthMeasureArea, out _, out _);
                if (originalDepthMeasureArea.D <= 0)
                {
                    return;
                }

                HOperatorSet.DilationCircle(originalDepthMeasureRegion, out originalDepthMeasureRegionDilation1, 5);
                HOperatorSet.DilationCircle(originalDepthMeasureRegion, out originalDepthMeasureRegionDilation2, 10);
                HOperatorSet.Difference(
                    originalDepthMeasureRegionDilation2,
                    originalDepthMeasureRegionDilation1,
                    out originalDepthMeasureCircleRegion);

                HOperatorSet.MinMaxGray(originalDepthMeasureCircleRegion, heightImage, 0, out _, out HTuple depthMaxGray, out _);
                HOperatorSet.MinMaxGray(originalDepthMeasureRegion, heightImage, 0, out HTuple depthMinGray, out _, out _);
                double depthGrayDiff = depthMaxGray.D - depthMinGray.D;

                result.TargetRegions.Add(new CalibrationPlateTargetRegion
                {
                    Index = result.TargetRegions.Count + 1,
                    Row1 = roiRow1,
                    Column1 = roiColumn1,
                    Row2 = roiRow2,
                    Column2 = roiColumn2
                });

                AppendDisplayObject(result, originalCircleContour);
                AppendDisplayObject(result, originalDepthMeasureRegion);
                result.Items.Add(new CalibrationPlateMeasureItem
                {
                    Index = result.Items.Count + 1,
                    LengthMm = circleDiameterMm,
                    WidthMm = circleRadiusMm,
                    DepthMm = depthGrayDiff * zp,
                    DepthGrayDiff = depthGrayDiff,
                    Row1 = roiRow1,
                    Column1 = roiColumn1,
                    Row2 = roiRow2,
                    Column2 = roiColumn2,
                    CircleCenterRow = originalCircleRow,
                    CircleCenterColumn = originalCircleColumn,
                    CircleRadiusPx = circleRadiusPx,
                    CircleRadiusMm = circleRadiusMm,
                    CircleDiameterMm = circleDiameterMm,
                    IsCircle = true
                });
            }
            catch (HalconException)
            {
            }
            finally
            {
                cropRectangle.Dispose();
                cropImageReduced.Dispose();
                croppedImage.Dispose();
                croppedHeightImage.Dispose();
                edges.Dispose();
                circleContours.Dispose();
                originalCircleContour.Dispose();
                originalDepthMeasureRegionRaw.Dispose();
                originalImageRegion.Dispose();
                originalDepthMeasureRegion.Dispose();
                originalDepthMeasureRegionDilation1.Dispose();
                originalDepthMeasureRegionDilation2.Dispose();
                originalDepthMeasureCircleRegion.Dispose();
            }
        }

        private static HObject CreateDisplayImage(HObject sourceImage, HObject measureRegion)
        {
            HObject normalizedImage = new();
            try
            {
                HOperatorSet.MinMaxGray(measureRegion, sourceImage, 0, out HTuple minValueTuple, out HTuple maxValueTuple, out _);
                double minValue = minValueTuple.D;
                double maxValue = maxValueTuple.D;
                double range = Math.Max(maxValue - minValue, 1e-9);
                HOperatorSet.ScaleImage(sourceImage, out normalizedImage, 255.0 / range, -minValue * 255.0 / range);
                HOperatorSet.ConvertImageType(normalizedImage, out HObject displayImage, "byte");
                return displayImage;
            }
            finally
            {
                normalizedImage.Dispose();
            }
        }

        private static void AppendDisplayObject(CalibrationPlateMeasureResult result, HObject displayObject)
        {
            if (displayObject == null || !displayObject.IsInitialized())
            {
                return;
            }

            HOperatorSet.ConcatObj(result.DisplayContours, displayObject, out HObject combinedObjects);
            result.DisplayContours.Dispose();
            result.DisplayContours = combinedObjects;
        }

        private static void GetHdevScaleFactors(
            CalibrationPlateMeasureRequest request,
            out double scaleX,
            out double scaleY,
            out double xp,
            out double yp,
            out double zp)
        {
            if (request.IntervalX < request.IntervalY)
            {
                scaleX = 1.0;
                scaleY = request.IntervalY / request.IntervalX;
            }
            else if (request.IntervalX > request.IntervalY)
            {
                scaleX = request.IntervalX / request.IntervalY;
                scaleY = 1.0;
            }
            else
            {
                scaleX = 1.0;
                scaleY = 1.0;
            }

            double intervalX = request.IntervalX / scaleX;
            double intervalY = request.IntervalY / scaleY;
            xp = (intervalX * request.AccelerationFactor) / scaleX;
            yp = (intervalY * request.AccelerationFactor) / scaleY;
            zp = request.IntervalZ;
        }

        private static void ValidateRequest(CalibrationPlateMeasureRequest request)
        {
            if (request.InputImage == null || !request.InputImage.IsInitialized())
            {
                throw new ArgumentException("Input image cannot be empty.", nameof(request));
            }

            if (request.IntervalX <= 0 || request.IntervalY <= 0 || request.IntervalZ <= 0)
            {
                throw new ArgumentException("Pixel spacing and height scale must be greater than 0.", nameof(request));
            }

            if (request.AccelerationFactor <= 0)
            {
                throw new ArgumentException("Acceleration factor must be greater than 0.", nameof(request));
            }

            if (!request.HasSelectedRoi)
            {
                throw new ArgumentException("Selected ROI is required.", nameof(request));
            }

            if (request.CircleMinContourLength <= 0 ||
                request.CircleMinRadiusPx <= 0)
            {
                throw new ArgumentException("Circle measurement parameters are invalid.", nameof(request));
            }
        }

        private static void GetSelectedRoi(
            CalibrationPlateMeasureRequest request,
            int imgWidth,
            int imgHeight,
            out double row1,
            out double column1,
            out double row2,
            out double column2)
        {
            row1 = Math.Clamp(Math.Min(request.SelectedRoiRow1, request.SelectedRoiRow2), 0, imgHeight - 1);
            column1 = Math.Clamp(Math.Min(request.SelectedRoiColumn1, request.SelectedRoiColumn2), 0, imgWidth - 1);
            row2 = Math.Clamp(Math.Max(request.SelectedRoiRow1, request.SelectedRoiRow2), 0, imgHeight - 1);
            column2 = Math.Clamp(Math.Max(request.SelectedRoiColumn1, request.SelectedRoiColumn2), 0, imgWidth - 1);
            if (row2 - row1 < 2 || column2 - column1 < 2)
            {
                throw new ArgumentException("Selected ROI is too small.", nameof(request));
            }
        }

        private static double Square(double value)
        {
            return value * value;
        }

        private static double GetD(HTuple tuple, int index)
        {
            return tuple[index].D;
        }
    }
}
