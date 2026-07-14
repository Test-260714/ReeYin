using HalconDotNet;

namespace Custom.CalibrationPlateMeasure.Algorithms
{
    // 刻槽测量算法：复刻 HDEV 直线边界检测和深度区域计算。
    public sealed class CalibrationPlateMeasureAlgorithm
    {
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
            HObject targetStripCandidates = new();

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
                HOperatorSet.GenRectangle1(out targetStripCandidates, roiRow1, roiColumn1, roiRow2, roiColumn2);

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

                    try
                    {
                        MeasureTarget(
                            heightImage,
                            targetStripCandidates,
                            1,
                            imgWidth,
                            imgHeight,
                            xp,
                            yp,
                            zp,
                            request,
                            result);
                    }
                    catch (HalconException)
                    {
                    }
                    catch (InvalidOperationException)
                    {
                    }

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
                targetStripCandidates.Dispose();
            }
        }

        private static void MeasureTarget(
            HObject heightImage,
            HObject targetStripCandidates,
            int targetIndex,
            int imgWidth,
            int imgHeight,
            double xp,
            double yp,
            double zp,
            CalibrationPlateMeasureRequest request,
            CalibrationPlateMeasureResult result)
        {
            // 后续计算只在用户框选 ROI 内进行，不再做动态阈值自动找目标。
            HObject targetRegion = new();
            HObject cropRectangle = new();
            HObject cropImageReduced = new();
            HObject croppedImage = new();
            HObject croppedHeightImage = new();
            HObject edges = new();
            HObject selectedEdges = new();
            HObject contoursSplit = new();
            HObject lineContours = new();
            HObject upperBoundaryFitPoints = new();
            HObject lowerBoundaryFitPoints = new();
            HObject originalDepthMeasureRegionRaw = new();
            HObject originalDepthMeasureContourRaw = new();
            HObject originalImageRegion = new();
            HObject originalDepthMeasureRegion = new();
            HObject upperBoundaryLine = new();
            HObject lowerBoundaryLine = new();
            HObject leftBoundaryLine = new();
            HObject rightBoundaryLine = new();

            try
            {
                HOperatorSet.SelectObj(targetStripCandidates, out targetRegion, targetIndex);
                HOperatorSet.SmallestRectangle1(targetRegion, out HTuple targetRow1Tuple, out HTuple targetColumn1Tuple, out HTuple targetRow2Tuple, out HTuple targetColumn2Tuple);

                result.TargetRegions.Add(new CalibrationPlateTargetRegion
                {
                    Index = targetIndex,
                    Row1 = targetRow1Tuple.D,
                    Column1 = targetColumn1Tuple.D,
                    Row2 = targetRow2Tuple.D,
                    Column2 = targetColumn2Tuple.D
                });

                double roiRow1 = Math.Max(0, targetRow1Tuple.D);
                double roiColumn1 = Math.Max(0, targetColumn1Tuple.D);
                double roiRow2 = Math.Min(imgHeight - 1, targetRow2Tuple.D);
                double roiColumn2 = Math.Min(imgWidth - 1, targetColumn2Tuple.D);

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
                HOperatorSet.SelectContoursXld(edges, out selectedEdges, "contour_length", 50, 99999, -0.5, 0.5);
                HOperatorSet.SegmentContoursXld(selectedEdges, out contoursSplit, "lines", 5, 4, 2);
                HOperatorSet.SelectContoursXld(contoursSplit, out lineContours, "contour_length", 80, 99999, -0.5, 0.5);
                HOperatorSet.FitLineContourXld(
                    lineContours,
                    "tukey",
                    -1,
                    0,
                    5,
                    2,
                    out HTuple rowBegin,
                    out HTuple colBegin,
                    out HTuple rowEnd,
                    out HTuple colEnd,
                    out _,
                    out _,
                    out _);

                if (rowBegin.Length < 2)
                {
                    return;
                }

                SelectHdevBoundaryLines(rowBegin, colBegin, rowEnd, colEnd, out int[] upperLineIndices, out int[] lowerLineIndices, out double meanLineAngle);

                double lengthPixelSize = Math.Sqrt(Square(Math.Cos(meanLineAngle) * xp) + Square(Math.Sin(meanLineAngle) * yp));
                double widthPixelSize = Math.Sqrt(Square(Math.Sin(meanLineAngle) * xp) + Square(Math.Cos(meanLineAngle) * yp));
                HOperatorSet.GetImageSize(croppedImage, out HTuple cropWidthTuple, out HTuple cropHeightTuple);
                int cropWidth = cropWidthTuple.I;
                int cropHeight = cropHeightTuple.I;
                double cropCenterCol = (cropWidth - 1) / 2.0;

                double tangentRow = Math.Sin(meanLineAngle);
                double tangentCol = Math.Cos(meanLineAngle);
                double normalRow = Math.Cos(meanLineAngle);
                double normalCol = -Math.Sin(meanLineAngle);

                HOperatorSet.GenContourPolygonXld(
                    out upperBoundaryFitPoints,
                    BuildFitTuple(rowBegin, rowEnd, upperLineIndices),
                    BuildFitTuple(colBegin, colEnd, upperLineIndices));
                HOperatorSet.GenContourPolygonXld(
                    out lowerBoundaryFitPoints,
                    BuildFitTuple(rowBegin, rowEnd, lowerLineIndices),
                    BuildFitTuple(colBegin, colEnd, lowerLineIndices));
                HOperatorSet.FitLineContourXld(upperBoundaryFitPoints, "tukey", -1, 0, 5, 2, out HTuple upperRowBegin, out HTuple upperColBegin, out HTuple upperRowEnd, out HTuple upperColEnd, out _, out _, out _);
                HOperatorSet.FitLineContourXld(lowerBoundaryFitPoints, "tukey", -1, 0, 5, 2, out HTuple lowerRowBegin, out HTuple lowerColBegin, out HTuple lowerRowEnd, out HTuple lowerColEnd, out _, out _, out _);

                double upperSlope = SafeSlope(upperRowBegin.D, upperRowEnd.D, upperColBegin.D, upperColEnd.D);
                double lowerSlope = SafeSlope(lowerRowBegin.D, lowerRowEnd.D, lowerColBegin.D, lowerColEnd.D);
                double upperRowCenter = upperRowBegin.D + upperSlope * (cropCenterCol - upperColBegin.D);
                double lowerRowCenter = lowerRowBegin.D + lowerSlope * (cropCenterCol - lowerColBegin.D);
                double signedBoundaryWidthPx = (lowerRowCenter - upperRowCenter) * normalRow;
                double boundaryWidthPx = Math.Abs(signedBoundaryWidthPx);
                double boundaryWidthMm = boundaryWidthPx * widthPixelSize;

                if (!TryMeasureSideEdges(
                    croppedImage,
                    cropWidth,
                    cropHeight,
                    (upperRowCenter + lowerRowCenter) / 2.0,
                    cropCenterCol,
                    meanLineAngle,
                    tangentRow,
                    tangentCol,
                    boundaryWidthPx,
                    out double leftCenterRow,
                    out double leftCenterCol,
                    out double rightCenterRow,
                    out double rightCenterCol))
                {
                    return;
                }

                double boundaryLengthPx = Math.Abs((rightCenterRow - leftCenterRow) * tangentRow + (rightCenterCol - leftCenterCol) * tangentCol);
                double boundaryLengthMm = boundaryLengthPx * lengthPixelSize;
                double halfSignedWidthPx = signedBoundaryWidthPx / 2.0;

                double upperLeftRow = leftCenterRow - normalRow * halfSignedWidthPx;
                double upperLeftCol = leftCenterCol - normalCol * halfSignedWidthPx;
                double lowerLeftRow = leftCenterRow + normalRow * halfSignedWidthPx;
                double lowerLeftCol = leftCenterCol + normalCol * halfSignedWidthPx;
                double upperRightRow = rightCenterRow - normalRow * halfSignedWidthPx;
                double upperRightCol = rightCenterCol - normalCol * halfSignedWidthPx;
                double lowerRightRow = rightCenterRow + normalRow * halfSignedWidthPx;
                double lowerRightCol = rightCenterCol + normalCol * halfSignedWidthPx;

                double signedDirection = signedBoundaryWidthPx < 0 ? -1.0 : 1.0;
                double halfSignedWidthExpandedPx = halfSignedWidthPx + signedDirection * request.DepthExpand;
                CreateDepthPolygon(
                    leftCenterRow,
                    leftCenterCol,
                    rightCenterRow,
                    rightCenterCol,
                    tangentRow,
                    tangentCol,
                    normalRow,
                    normalCol,
                    halfSignedWidthExpandedPx,
                    request.DepthExpand,
                    out double depthUpperLeftRow,
                    out double depthUpperLeftCol,
                    out double depthUpperRightRow,
                    out double depthUpperRightCol,
                    out double depthLowerRightRow,
                    out double depthLowerRightCol,
                    out double depthLowerLeftRow,
                    out double depthLowerLeftCol);

                double originalDepthUpperLeftRow = depthUpperLeftRow + roiRow1;
                double originalDepthUpperLeftCol = depthUpperLeftCol + roiColumn1;
                double originalDepthUpperRightRow = depthUpperRightRow + roiRow1;
                double originalDepthUpperRightCol = depthUpperRightCol + roiColumn1;
                double originalDepthLowerRightRow = depthLowerRightRow + roiRow1;
                double originalDepthLowerRightCol = depthLowerRightCol + roiColumn1;
                double originalDepthLowerLeftRow = depthLowerLeftRow + roiRow1;
                double originalDepthLowerLeftCol = depthLowerLeftCol + roiColumn1;

                HOperatorSet.GenContourPolygonXld(
                    out originalDepthMeasureContourRaw,
                    new HTuple(originalDepthLowerLeftRow, originalDepthLowerRightRow, originalDepthUpperRightRow, originalDepthUpperLeftRow, originalDepthLowerLeftRow),
                    new HTuple(originalDepthLowerLeftCol, originalDepthLowerRightCol, originalDepthUpperRightCol, originalDepthUpperLeftCol, originalDepthLowerLeftCol));
                HOperatorSet.GenRegionContourXld(originalDepthMeasureContourRaw, out originalDepthMeasureRegionRaw, "filled");

                HOperatorSet.GenRectangle1(out originalImageRegion, 0, 0, imgHeight - 1, imgWidth - 1);
                HOperatorSet.Intersection(originalDepthMeasureRegionRaw, originalImageRegion, out originalDepthMeasureRegion);
                HOperatorSet.MinMaxGray(originalDepthMeasureRegion, heightImage, 0, out HTuple depthMinGray, out HTuple depthMaxGray, out _);
                double depthGrayDiff = depthMaxGray.D - depthMinGray.D;

                AppendDisplayObject(result, originalDepthMeasureContourRaw);
                AppendDisplayBoundaryLines(
                    result,
                    roiRow1 + upperLeftRow,
                    roiColumn1 + upperLeftCol,
                    roiRow1 + upperRightRow,
                    roiColumn1 + upperRightCol,
                    roiRow1 + lowerRightRow,
                    roiColumn1 + lowerRightCol,
                    roiRow1 + lowerLeftRow,
                    roiColumn1 + lowerLeftCol,
                    ref upperBoundaryLine,
                    ref lowerBoundaryLine,
                    ref leftBoundaryLine,
                    ref rightBoundaryLine);

                result.Items.Add(new CalibrationPlateMeasureItem
                {
                    Index = targetIndex,
                    LengthMm = boundaryLengthMm,
                    WidthMm = boundaryWidthMm,
                    DepthMm = depthGrayDiff * zp,
                    DepthGrayDiff = depthGrayDiff,
                    Row1 = roiRow1,
                    Column1 = roiColumn1,
                    Row2 = roiRow2,
                    Column2 = roiColumn2,
                    UpperRow = roiRow1 + upperLeftRow,
                    LowerRow = roiRow1 + lowerLeftRow,
                    LeftColumn = roiColumn1 + leftCenterCol,
                    RightColumn = roiColumn1 + rightCenterCol,
                    UpperLeftRow = roiRow1 + upperLeftRow,
                    UpperLeftColumn = roiColumn1 + upperLeftCol,
                    LowerLeftRow = roiRow1 + lowerLeftRow,
                    LowerLeftColumn = roiColumn1 + lowerLeftCol,
                    UpperRightRow = roiRow1 + upperRightRow,
                    UpperRightColumn = roiColumn1 + upperRightCol,
                    LowerRightRow = roiRow1 + lowerRightRow,
                    LowerRightColumn = roiColumn1 + lowerRightCol,
                    DepthUpperLeftRow = originalDepthUpperLeftRow,
                    DepthUpperLeftColumn = originalDepthUpperLeftCol,
                    DepthUpperRightRow = originalDepthUpperRightRow,
                    DepthUpperRightColumn = originalDepthUpperRightCol,
                    DepthLowerRightRow = originalDepthLowerRightRow,
                    DepthLowerRightColumn = originalDepthLowerRightCol,
                    DepthLowerLeftRow = originalDepthLowerLeftRow,
                    DepthLowerLeftColumn = originalDepthLowerLeftCol
                });
            }
            finally
            {
                targetRegion.Dispose();
                cropRectangle.Dispose();
                cropImageReduced.Dispose();
                croppedImage.Dispose();
                croppedHeightImage.Dispose();
                edges.Dispose();
                selectedEdges.Dispose();
                contoursSplit.Dispose();
                lineContours.Dispose();
                upperBoundaryFitPoints.Dispose();
                lowerBoundaryFitPoints.Dispose();
                originalDepthMeasureContourRaw.Dispose();
                originalDepthMeasureRegionRaw.Dispose();
                originalImageRegion.Dispose();
                originalDepthMeasureRegion.Dispose();
                upperBoundaryLine.Dispose();
                lowerBoundaryLine.Dispose();
                leftBoundaryLine.Dispose();
                rightBoundaryLine.Dispose();
            }
        }

        private static bool TryMeasureSideEdges(
            HObject croppedImage,
            int cropWidth,
            int cropHeight,
            double centerBoundaryRow,
            double centerBoundaryCol,
            double meanLineAngle,
            double tangentRow,
            double tangentCol,
            double boundaryWidthPx,
            out double leftCenterRow,
            out double leftCenterCol,
            out double rightCenterRow,
            out double rightCenterCol)
        {
            leftCenterRow = 0;
            leftCenterCol = 0;
            rightCenterRow = 0;
            rightCenterCol = 0;

            double sideMeasureLength1 = Math.Sqrt(cropWidth * cropWidth + cropHeight * cropHeight) / 2.0;
            double sideMeasureLength2 = boundaryWidthPx * 0.2;
            HOperatorSet.GenMeasureRectangle2(
                centerBoundaryRow,
                centerBoundaryCol,
                -meanLineAngle,
                sideMeasureLength1,
                sideMeasureLength2,
                cropWidth,
                cropHeight,
                "bilinear",
                out HTuple measureHandle);

            try
            {
                HOperatorSet.MeasurePos(
                    croppedImage,
                    measureHandle,
                    1.0,
                    10,
                    "all",
                    "all",
                    out HTuple sideEdgeRows,
                    out HTuple sideEdgeCols,
                    out _,
                    out _);

                if (sideEdgeCols.Length < 2)
                {
                    return false;
                }

                int leftEdgeIndex = 0;
                int rightEdgeIndex = 0;
                double leftProjection = GetD(sideEdgeRows, 0) * tangentRow + GetD(sideEdgeCols, 0) * tangentCol;
                double rightProjection = leftProjection;
                for (int index = 1; index < sideEdgeCols.Length; index++)
                {
                    double edgeProjection = GetD(sideEdgeRows, index) * tangentRow + GetD(sideEdgeCols, index) * tangentCol;
                    if (edgeProjection < leftProjection)
                    {
                        leftEdgeIndex = index;
                        leftProjection = edgeProjection;
                    }

                    if (edgeProjection > rightProjection)
                    {
                        rightEdgeIndex = index;
                        rightProjection = edgeProjection;
                    }
                }

                leftCenterRow = GetD(sideEdgeRows, leftEdgeIndex);
                leftCenterCol = GetD(sideEdgeCols, leftEdgeIndex);
                rightCenterRow = GetD(sideEdgeRows, rightEdgeIndex);
                rightCenterCol = GetD(sideEdgeCols, rightEdgeIndex);
                return true;
            }
            finally
            {
                HOperatorSet.CloseMeasure(measureHandle);
            }
        }

        private static void CreateDepthPolygon(
            double leftCenterRow,
            double leftCenterCol,
            double rightCenterRow,
            double rightCenterCol,
            double tangentRow,
            double tangentCol,
            double normalRow,
            double normalCol,
            double halfSignedWidthExpandedPx,
            double depthExpandPx,
            out double depthUpperLeftRow,
            out double depthUpperLeftCol,
            out double depthUpperRightRow,
            out double depthUpperRightCol,
            out double depthLowerRightRow,
            out double depthLowerRightCol,
            out double depthLowerLeftRow,
            out double depthLowerLeftCol)
        {
            double depthUpperLinePoint1Row = leftCenterRow - normalRow * halfSignedWidthExpandedPx;
            double depthUpperLinePoint1Col = leftCenterCol - normalCol * halfSignedWidthExpandedPx;
            double depthUpperLinePoint2Row = rightCenterRow - normalRow * halfSignedWidthExpandedPx;
            double depthUpperLinePoint2Col = rightCenterCol - normalCol * halfSignedWidthExpandedPx;
            double depthLowerLinePoint1Row = leftCenterRow + normalRow * halfSignedWidthExpandedPx;
            double depthLowerLinePoint1Col = leftCenterCol + normalCol * halfSignedWidthExpandedPx;
            double depthLowerLinePoint2Row = rightCenterRow + normalRow * halfSignedWidthExpandedPx;
            double depthLowerLinePoint2Col = rightCenterCol + normalCol * halfSignedWidthExpandedPx;
            double depthLeftLinePoint1Row = leftCenterRow - tangentRow * depthExpandPx;
            double depthLeftLinePoint1Col = leftCenterCol - tangentCol * depthExpandPx;
            double depthLeftLinePoint2Row = depthLeftLinePoint1Row + normalRow;
            double depthLeftLinePoint2Col = depthLeftLinePoint1Col + normalCol;
            double depthRightLinePoint1Row = rightCenterRow + tangentRow * depthExpandPx;
            double depthRightLinePoint1Col = rightCenterCol + tangentCol * depthExpandPx;
            double depthRightLinePoint2Row = depthRightLinePoint1Row + normalRow;
            double depthRightLinePoint2Col = depthRightLinePoint1Col + normalCol;

            IntersectLines(depthUpperLinePoint1Row, depthUpperLinePoint1Col, depthUpperLinePoint2Row, depthUpperLinePoint2Col, depthLeftLinePoint1Row, depthLeftLinePoint1Col, depthLeftLinePoint2Row, depthLeftLinePoint2Col, out double rawUpperLeftRow, out double rawUpperLeftCol);
            IntersectLines(depthUpperLinePoint1Row, depthUpperLinePoint1Col, depthUpperLinePoint2Row, depthUpperLinePoint2Col, depthRightLinePoint1Row, depthRightLinePoint1Col, depthRightLinePoint2Row, depthRightLinePoint2Col, out double rawUpperRightRow, out double rawUpperRightCol);
            IntersectLines(depthLowerLinePoint1Row, depthLowerLinePoint1Col, depthLowerLinePoint2Row, depthLowerLinePoint2Col, depthRightLinePoint1Row, depthRightLinePoint1Col, depthRightLinePoint2Row, depthRightLinePoint2Col, out double rawLowerRightRow, out double rawLowerRightCol);
            IntersectLines(depthLowerLinePoint1Row, depthLowerLinePoint1Col, depthLowerLinePoint2Row, depthLowerLinePoint2Col, depthLeftLinePoint1Row, depthLeftLinePoint1Col, depthLeftLinePoint2Row, depthLeftLinePoint2Col, out double rawLowerLeftRow, out double rawLowerLeftCol);

            double[] cornerRows = [rawUpperLeftRow, rawUpperRightRow, rawLowerRightRow, rawLowerLeftRow];
            double[] cornerCols = [rawUpperLeftCol, rawUpperRightCol, rawLowerRightCol, rawLowerLeftCol];
            double depthTMin = double.PositiveInfinity;
            double depthTMax = double.NegativeInfinity;
            double depthNMin = double.PositiveInfinity;
            double depthNMax = double.NegativeInfinity;

            for (int index = 0; index < cornerRows.Length; index++)
            {
                double tProjection = cornerRows[index] * tangentRow + cornerCols[index] * tangentCol;
                double nProjection = cornerRows[index] * normalRow + cornerCols[index] * normalCol;
                depthTMin = Math.Min(depthTMin, tProjection);
                depthTMax = Math.Max(depthTMax, tProjection);
                depthNMin = Math.Min(depthNMin, nProjection);
                depthNMax = Math.Max(depthNMax, nProjection);
            }

            depthUpperLeftRow = depthTMin * tangentRow + depthNMin * normalRow;
            depthUpperLeftCol = depthTMin * tangentCol + depthNMin * normalCol;
            depthUpperRightRow = depthTMax * tangentRow + depthNMin * normalRow;
            depthUpperRightCol = depthTMax * tangentCol + depthNMin * normalCol;
            depthLowerRightRow = depthTMax * tangentRow + depthNMax * normalRow;
            depthLowerRightCol = depthTMax * tangentCol + depthNMax * normalCol;
            depthLowerLeftRow = depthTMin * tangentRow + depthNMax * normalRow;
            depthLowerLeftCol = depthTMin * tangentCol + depthNMax * normalCol;
        }

        private static void IntersectLines(
            double row1,
            double col1,
            double row2,
            double col2,
            double row3,
            double col3,
            double row4,
            double col4,
            out double row,
            out double col)
        {
            HOperatorSet.IntersectionLines(row1, col1, row2, col2, row3, col3, row4, col4, out HTuple rowTuple, out HTuple colTuple, out _);
            row = rowTuple.D;
            col = colTuple.D;
        }

        private static void SelectHdevBoundaryLines(
            HTuple rowBegin,
            HTuple colBegin,
            HTuple rowEnd,
            HTuple colEnd,
            out int[] upperLineIndices,
            out int[] lowerLineIndices,
            out double meanLineAngle)
        {
            const double pi = Math.PI;
            const double angleTolerance = 15.0 * pi / 180.0;
            int referenceLineIndex = 0;
            double referenceLineLength = -1.0;
            var angles = new List<double>();
            var centerRows = new List<double>();
            var centerCols = new List<double>();
            var lengths = new List<double>();

            if (rowBegin.Length < 2)
            {
                throw new InvalidOperationException("Line boundary count is insufficient.");
            }

            for (int index = 0; index < rowBegin.Length; index++)
            {
                double deltaRow = GetD(rowEnd, index) - GetD(rowBegin, index);
                double deltaCol = GetD(colEnd, index) - GetD(colBegin, index);
                double lineAngle = Math.Atan2(deltaRow, deltaCol);
                if (lineAngle > pi / 2.0)
                {
                    lineAngle -= pi;
                }

                if (lineAngle < -pi / 2.0)
                {
                    lineAngle += pi;
                }

                double lineLength = Math.Sqrt(deltaRow * deltaRow + deltaCol * deltaCol);
                angles.Add(lineAngle);
                centerRows.Add((GetD(rowBegin, index) + GetD(rowEnd, index)) / 2.0);
                centerCols.Add((GetD(colBegin, index) + GetD(colEnd, index)) / 2.0);
                lengths.Add(lineLength);

                if (lineLength > referenceLineLength)
                {
                    referenceLineLength = lineLength;
                    referenceLineIndex = index;
                }
            }

            double referenceLineAngle = angles[referenceLineIndex];
            double referenceNormalRow = Math.Cos(referenceLineAngle);
            double referenceNormalCol = -Math.Sin(referenceLineAngle);
            var validLineIndices = new List<int>();
            var validLineNormalProjections = new List<double>();

            for (int index = 0; index < rowBegin.Length; index++)
            {
                if (Math.Abs(angles[index] - referenceLineAngle) <= angleTolerance)
                {
                    double lineNormalProjection = centerRows[index] * referenceNormalRow + centerCols[index] * referenceNormalCol;
                    validLineIndices.Add(index);
                    validLineNormalProjections.Add(lineNormalProjection);
                }
            }

            if (validLineIndices.Count < 2)
            {
                throw new InvalidOperationException("Parallel line boundary count is insufficient.");
            }

            List<double> sortedLineNormalProjections = validLineNormalProjections.OrderBy(value => value).ToList();
            double splitNormalProjection = (sortedLineNormalProjections[0] + sortedLineNormalProjections[^1]) / 2.0;
            double maxNormalProjectionGap = -1.0;
            for (int index = 0; index < sortedLineNormalProjections.Count - 1; index++)
            {
                double normalProjectionGap = sortedLineNormalProjections[index + 1] - sortedLineNormalProjections[index];
                if (normalProjectionGap > maxNormalProjectionGap)
                {
                    maxNormalProjectionGap = normalProjectionGap;
                    splitNormalProjection = (sortedLineNormalProjections[index] + sortedLineNormalProjections[index + 1]) / 2.0;
                }
            }

            var group1LineIndices = new List<int>();
            var group2LineIndices = new List<int>();
            double group1RowSum = 0.0;
            double group2RowSum = 0.0;

            for (int index = 0; index < validLineIndices.Count; index++)
            {
                int lineIndex = validLineIndices[index];
                if (validLineNormalProjections[index] <= splitNormalProjection)
                {
                    group1LineIndices.Add(lineIndex);
                    group1RowSum += centerRows[lineIndex];
                }
                else
                {
                    group2LineIndices.Add(lineIndex);
                    group2RowSum += centerRows[lineIndex];
                }
            }

            if (group1LineIndices.Count < 1 || group2LineIndices.Count < 1)
            {
                throw new InvalidOperationException("Upper/lower boundary grouping failed.");
            }

            double group1MeanRow = group1RowSum / group1LineIndices.Count;
            double group2MeanRow = group2RowSum / group2LineIndices.Count;
            if (group1MeanRow <= group2MeanRow)
            {
                upperLineIndices = group1LineIndices.ToArray();
                lowerLineIndices = group2LineIndices.ToArray();
            }
            else
            {
                upperLineIndices = group2LineIndices.ToArray();
                lowerLineIndices = group1LineIndices.ToArray();
            }

            int[] selectedLineIndices = upperLineIndices.Concat(lowerLineIndices).ToArray();
            double totalLength = selectedLineIndices.Sum(index => lengths[index]);
            meanLineAngle = totalLength <= 0
                ? 0
                : selectedLineIndices.Sum(index => angles[index] * lengths[index]) / totalLength;
        }

        private static HTuple BuildFitTuple(HTuple begin, HTuple end, IReadOnlyList<int> lineIndices)
        {
            var values = new List<double>(lineIndices.Count * 2);
            foreach (int lineIndex in lineIndices)
            {
                values.Add(GetD(begin, lineIndex));
            }

            foreach (int lineIndex in lineIndices)
            {
                values.Add(GetD(end, lineIndex));
            }

            return new HTuple(values.ToArray());
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

        private static void AppendDisplayBoundaryLines(
            CalibrationPlateMeasureResult result,
            double upperLeftRow,
            double upperLeftCol,
            double upperRightRow,
            double upperRightCol,
            double lowerRightRow,
            double lowerRightCol,
            double lowerLeftRow,
            double lowerLeftCol,
            ref HObject upperBoundaryLine,
            ref HObject lowerBoundaryLine,
            ref HObject leftBoundaryLine,
            ref HObject rightBoundaryLine)
        {
            HOperatorSet.GenContourPolygonXld(
                out upperBoundaryLine,
                new HTuple(upperLeftRow, upperRightRow),
                new HTuple(upperLeftCol, upperRightCol));
            HOperatorSet.GenContourPolygonXld(
                out lowerBoundaryLine,
                new HTuple(lowerLeftRow, lowerRightRow),
                new HTuple(lowerLeftCol, lowerRightCol));
            HOperatorSet.GenContourPolygonXld(
                out leftBoundaryLine,
                new HTuple(upperLeftRow, lowerLeftRow),
                new HTuple(upperLeftCol, lowerLeftCol));
            HOperatorSet.GenContourPolygonXld(
                out rightBoundaryLine,
                new HTuple(upperRightRow, lowerRightRow),
                new HTuple(upperRightCol, lowerRightCol));

            AppendDisplayObject(result, upperBoundaryLine);
            AppendDisplayObject(result, lowerBoundaryLine);
            AppendDisplayObject(result, leftBoundaryLine);
            AppendDisplayObject(result, rightBoundaryLine);
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

        private static double SafeSlope(double rowBegin, double rowEnd, double colBegin, double colEnd)
        {
            double deltaCol = colEnd - colBegin;
            if (Math.Abs(deltaCol) < 1e-9)
            {
                return 0;
            }

            return (rowEnd - rowBegin) / deltaCol;
        }

        private static double Square(double value)
        {
            return value * value;
        }

        private static double GetD(HTuple tuple, int index)
        {
            return tuple[index].D;
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
    }
}
