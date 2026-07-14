using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using HalconDotNet;
using PointCloud.Algorithms.Dtos;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Registration;

internal sealed class CoarseImageRegistrationService
{
    private const int BaseCoarseStepPixel = 4;
    private const int BaseCoarseSampleStepPixel = 4;
    private const int BaseFineSampleStepPixel = 2;
    private const int BaseMinSampleCount = 200;

    public static CoarseImageSearchOptions CreateCoarseImageSearchOptions(
        double intervalX,
        double intervalY,
        CoarseRegistrationParameters coarseParam)
    {
        ArgumentNullException.ThrowIfNull(coarseParam);
        ValidateStrictPositiveFinite(intervalX, nameof(intervalX));
        ValidateStrictPositiveFinite(intervalY, nameof(intervalY));
        ValidateNonNegativeFinite(coarseParam.SearchWindowX, nameof(coarseParam.SearchWindowX));
        ValidateNonNegativeFinite(coarseParam.SearchWindowY, nameof(coarseParam.SearchWindowY));
        if (!double.IsFinite(coarseParam.SearchSpeedFactor))
            throw new ArgumentOutOfRangeException(nameof(coarseParam.SearchSpeedFactor), coarseParam.SearchSpeedFactor, "SearchSpeedFactor must be finite.");
        ValidateNonNegativeFinite(coarseParam.HeightWeight, nameof(coarseParam.HeightWeight));
        ValidateNonNegativeFinite(coarseParam.GrayWeight, nameof(coarseParam.GrayWeight));
        ValidateNonNegativeFinite(coarseParam.MaskWeight, nameof(coarseParam.MaskWeight));

        double speed = Math.Clamp(coarseParam.SearchSpeedFactor, 0.5, 4.0);

        return new CoarseImageSearchOptions
        {
            SearchWindowX = coarseParam.SearchWindowX,
            SearchWindowY = coarseParam.SearchWindowY,
            SearchRadiusCol = Math.Max(1, (int)Math.Ceiling(coarseParam.SearchWindowX / intervalX)),
            SearchRadiusRow = Math.Max(1, (int)Math.Ceiling(coarseParam.SearchWindowY / intervalY)),
            CoarseStepCol = Math.Max(1, (int)Math.Round(BaseCoarseStepPixel * speed)),
            CoarseStepRow = Math.Max(1, (int)Math.Round(BaseCoarseStepPixel * speed)),
            CoarseSampleStepCol = Math.Max(1, (int)Math.Round(BaseCoarseSampleStepPixel * speed)),
            CoarseSampleStepRow = Math.Max(1, (int)Math.Round(BaseCoarseSampleStepPixel * speed)),
            FineSampleStepCol = Math.Max(1, (int)Math.Round(BaseFineSampleStepPixel * Math.Sqrt(speed))),
            FineSampleStepRow = Math.Max(1, (int)Math.Round(BaseFineSampleStepPixel * Math.Sqrt(speed))),
            MinSampleCount = Math.Max(50, (int)Math.Round(BaseMinSampleCount / speed)),
            HeightWeight = coarseParam.HeightWeight,
            GrayWeight = coarseParam.GrayWeight,
            MaskWeight = coarseParam.MaskWeight
        };
    }

    public CoarseImageRegistrationResult EstimateCoarseTransformByHalconImageSearch(
        ImageFrame sourceFrame,
        ImageFrame targetFrame,
        PointCloudRigidTransform2D sourceInitialTransform,
        PointCloudRigidTransform2D targetInitialTransform,
        PointCloudRigidTransform2D targetTransform,
        CoarseImageSearchOptions options,
        double invalidDepthValue)
    {
        ArgumentNullException.ThrowIfNull(sourceFrame);
        ArgumentNullException.ThrowIfNull(targetFrame);
        ArgumentNullException.ThrowIfNull(options);
        ValidateStrictPositiveFinite(sourceFrame.Descriptor.IntervalX, nameof(sourceFrame.Descriptor.IntervalX));
        ValidateStrictPositiveFinite(sourceFrame.Descriptor.IntervalY, nameof(sourceFrame.Descriptor.IntervalY));
        ValidateStrictPositiveFinite(sourceFrame.Descriptor.IntervalZ, nameof(sourceFrame.Descriptor.IntervalZ));
        ValidateStrictPositiveFinite(targetFrame.Descriptor.IntervalX, nameof(targetFrame.Descriptor.IntervalX));
        ValidateStrictPositiveFinite(targetFrame.Descriptor.IntervalY, nameof(targetFrame.Descriptor.IntervalY));
        ValidateStrictPositiveFinite(targetFrame.Descriptor.IntervalZ, nameof(targetFrame.Descriptor.IntervalZ));
        if (!double.IsFinite(invalidDepthValue))
            throw new ArgumentOutOfRangeException(nameof(invalidDepthValue), invalidDepthValue, "Invalid depth value must be finite.");

        double sourceIntervalX = sourceFrame.Descriptor.IntervalX;
        double sourceIntervalY = sourceFrame.Descriptor.IntervalY;
        double sourceIntervalZ = sourceFrame.Descriptor.IntervalZ;
        double targetIntervalX = targetFrame.Descriptor.IntervalX;
        double targetIntervalY = targetFrame.Descriptor.IntervalY;
        double targetIntervalZ = targetFrame.Descriptor.IntervalZ;
        if (Math.Abs(sourceIntervalX - targetIntervalX) > 1e-6 ||
            Math.Abs(sourceIntervalY - targetIntervalY) > 1e-6)
        {
            throw new InvalidOperationException("Coarse registration requires adjacent frames to have matching X/Y pixel intervals.");
        }

        int initialOffsetCol = (int)Math.Round((sourceInitialTransform.X - targetInitialTransform.X) / sourceIntervalX);
        int initialOffsetRow = (int)Math.Round((sourceInitialTransform.Y - targetInitialTransform.Y) / sourceIntervalY);
        int searchRadiusCol = options.SearchRadiusCol;
        int searchRadiusRow = options.SearchRadiusRow;
        int minOffsetCol = initialOffsetCol - searchRadiusCol;
        int maxOffsetCol = initialOffsetCol + searchRadiusCol;
        int minOffsetRow = initialOffsetRow - searchRadiusRow;
        int maxOffsetRow = initialOffsetRow + searchRadiusRow;
        double sourceMinDepth = sourceFrame.Descriptor.MinDepth;
        double sourceMaxDepth = sourceFrame.Descriptor.MaxDepth;
        double targetMinDepth = targetFrame.Descriptor.MinDepth;
        double targetMaxDepth = targetFrame.Descriptor.MaxDepth;
        ValidateDepthRange(sourceMinDepth, sourceMaxDepth, nameof(sourceFrame));
        ValidateDepthRange(targetMinDepth, targetMaxDepth, nameof(targetFrame));

        HObject? targetGray = null;
        HObject? sourceGray = null;
        HObject? targetHeight = null;
        HObject? sourceHeight = null;

        try
        {
            HOperatorSet.ConvertImageType(targetFrame.GrayImage, out targetGray, "byte");
            HOperatorSet.ConvertImageType(sourceFrame.GrayImage, out sourceGray, "byte");
            HOperatorSet.ConvertImageType(targetFrame.HeightImage, out targetHeight, "real");
            HOperatorSet.ConvertImageType(sourceFrame.HeightImage, out sourceHeight, "real");

            (int targetWidth, int targetHeightSize) = GetImageSize(targetHeight);
            (int sourceWidth, int sourceHeightSize) = GetImageSize(sourceHeight);
            (int targetGrayWidth, int targetGrayHeight) = GetImageSize(targetGray);
            (int sourceGrayWidth, int sourceGrayHeight) = GetImageSize(sourceGray);

            if (targetWidth <= 0 || targetHeightSize <= 0 || sourceWidth <= 0 || sourceHeightSize <= 0)
                throw new InvalidOperationException("Coarse registration requires valid gray and height image dimensions.");
            if (targetGrayWidth != targetWidth || targetGrayHeight != targetHeightSize ||
                sourceGrayWidth != sourceWidth || sourceGrayHeight != sourceHeightSize)
            {
                throw new InvalidOperationException("Coarse registration requires gray and height images in each frame to have matching dimensions.");
            }

            CandidateScore coarseBest = FindBestCandidate(
                initialOffsetCol,
                initialOffsetRow,
                searchRadiusCol,
                searchRadiusRow,
                minOffsetCol,
                maxOffsetCol,
                minOffsetRow,
                maxOffsetRow,
                options.CoarseStepCol,
                options.CoarseStepRow,
                options.CoarseSampleStepCol,
                options.CoarseSampleStepRow,
                useMedianHeight: false);
            if (!coarseBest.IsValid)
            {
                throw new InvalidOperationException(
                    $"Coarse registration failed: insufficient valid overlap samples inside search window. " +
                    $"WindowX={options.SearchWindowX:F3}, WindowY={options.SearchWindowY:F3}.");
            }

            CandidateScore refinedBest = FindBestCandidate(
                coarseBest.OffsetCol,
                coarseBest.OffsetRow,
                Math.Max(options.CoarseStepCol, 1),
                Math.Max(options.CoarseStepRow, 1),
                minOffsetCol,
                maxOffsetCol,
                minOffsetRow,
                maxOffsetRow,
                1,
                1,
                options.FineSampleStepCol,
                options.FineSampleStepRow,
                useMedianHeight: false);
            if (!refinedBest.IsValid)
                refinedBest = coarseBest;

            CandidateScore finalScore = EvaluateCandidate(
                refinedBest.OffsetCol,
                refinedBest.OffsetRow,
                options.FineSampleStepCol,
                options.FineSampleStepRow,
                useMedianHeight: true);
            if (!finalScore.IsValid)
                finalScore = refinedBest;

            PointCloudRigidTransform2D coarseTransform = new(
                targetTransform.X + finalScore.OffsetCol * sourceIntervalX,
                targetTransform.Y + finalScore.OffsetRow * sourceIntervalY,
                finalScore.HeightOffset,
                sourceInitialTransform.YawDeg);

            return new CoarseImageRegistrationResult(
                coarseTransform,
                finalScore.OffsetCol,
                finalScore.OffsetRow,
                initialOffsetCol,
                initialOffsetRow,
                finalScore.Score,
                finalScore.HeightResidual,
                finalScore.GrayMeanAbsDiff,
                finalScore.MaskMismatchRatio,
                finalScore.SampleCount);

            CandidateScore FindBestCandidate(
                int centerOffsetCol,
                int centerOffsetRow,
                int candidateSearchRadiusCol,
                int candidateSearchRadiusRow,
                int candidateMinOffsetCol,
                int candidateMaxOffsetCol,
                int candidateMinOffsetRow,
                int candidateMaxOffsetRow,
                int stepCol,
                int stepRow,
                int sampleStepCol,
                int sampleStepRow,
                bool useMedianHeight)
            {
                CandidateScore best = CandidateScore.Invalid();
                foreach (int offsetRow in EnumerateSearchOffsets(centerOffsetRow, candidateSearchRadiusRow, stepRow))
                {
                    if (offsetRow < candidateMinOffsetRow || offsetRow > candidateMaxOffsetRow)
                        continue;

                    foreach (int offsetCol in EnumerateSearchOffsets(centerOffsetCol, candidateSearchRadiusCol, stepCol))
                    {
                        if (offsetCol < candidateMinOffsetCol || offsetCol > candidateMaxOffsetCol)
                            continue;

                        CandidateScore score = EvaluateCandidate(offsetCol, offsetRow, sampleStepCol, sampleStepRow, useMedianHeight);
                        if (score.IsValid && score.Score < best.Score)
                            best = score;
                    }
                }

                return best;
            }

            CandidateScore EvaluateCandidate(
                int offsetCol,
                int offsetRow,
                int sampleStepCol,
                int sampleStepRow,
                bool useMedianHeight)
            {
                int colStart = Math.Max(0, offsetCol);
                int rowStart = Math.Max(0, offsetRow);
                int colEnd = Math.Min(targetWidth - 1, offsetCol + sourceWidth - 1);
                int rowEnd = Math.Min(targetHeightSize - 1, offsetRow + sourceHeightSize - 1);
                int overlapWidth = colEnd - colStart + 1;
                int overlapHeight = rowEnd - rowStart + 1;
                if (overlapWidth <= 0 || overlapHeight <= 0)
                    return CandidateScore.Invalid();

                int sourceColStart = colStart - offsetCol;
                int sourceRowStart = rowStart - offsetRow;
                sampleStepCol = Math.Max(1, sampleStepCol);
                sampleStepRow = Math.Max(1, sampleStepRow);
                double sampleScaleX = 1.0 / sampleStepCol;
                double sampleScaleY = 1.0 / sampleStepRow;

                HObject? targetHeightPart = null;
                HObject? sourceHeightPart = null;
                HObject? targetGrayPart = null;
                HObject? sourceGrayPart = null;
                HObject? targetHeightSample = null;
                HObject? sourceHeightSample = null;
                HObject? targetGraySample = null;
                HObject? sourceGraySample = null;
                HObject? fullSampleRegion = null;
                HObject? targetValidRegion = null;
                HObject? sourceValidRegion = null;
                HObject? targetInvalidRegion = null;
                HObject? sourceInvalidRegion = null;
                HObject? targetFilteredValidRegion = null;
                HObject? sourceFilteredValidRegion = null;
                HObject? heightValidRegion = null;
                HObject? targetHeightPhysical = null;
                HObject? sourceHeightPhysical = null;
                HObject? heightDiff = null;
                HObject? grayAbsDiff = null;
                HObject? targetHoleRegion = null;
                HObject? sourceHoleRegion = null;
                HObject? holeUnionRegion = null;
                HObject? holeIntersectionRegion = null;
                HObject? holeMismatchRegion = null;

                try
                {
                    HOperatorSet.CropPart(targetHeight, out targetHeightPart, rowStart, colStart, overlapWidth, overlapHeight);
                    HOperatorSet.CropPart(sourceHeight, out sourceHeightPart, sourceRowStart, sourceColStart, overlapWidth, overlapHeight);
                    HOperatorSet.CropPart(targetGray, out targetGrayPart, rowStart, colStart, overlapWidth, overlapHeight);
                    HOperatorSet.CropPart(sourceGray, out sourceGrayPart, sourceRowStart, sourceColStart, overlapWidth, overlapHeight);

                    HOperatorSet.ZoomImageFactor(targetHeightPart, out targetHeightSample, sampleScaleX, sampleScaleY, "nearest_neighbor");
                    HOperatorSet.ZoomImageFactor(sourceHeightPart, out sourceHeightSample, sampleScaleX, sampleScaleY, "nearest_neighbor");
                    HOperatorSet.ZoomImageFactor(targetGrayPart, out targetGraySample, sampleScaleX, sampleScaleY, "bilinear");
                    HOperatorSet.ZoomImageFactor(sourceGrayPart, out sourceGraySample, sampleScaleX, sampleScaleY, "bilinear");

                    if (!AlignSamplesToCommonSize(
                            ref targetHeightSample,
                            ref sourceHeightSample,
                            ref targetGraySample,
                            ref sourceGraySample,
                            out int sampleWidth,
                            out int sampleHeight))
                    {
                        return CandidateScore.Invalid();
                    }

                    HOperatorSet.GenRectangle1(out fullSampleRegion, 0, 0, sampleHeight - 1, sampleWidth - 1);
                    double fullSampleCount = GetRegionArea(fullSampleRegion);
                    if (fullSampleCount < options.MinSampleCount)
                        return CandidateScore.Invalid();

                    HOperatorSet.Threshold(targetHeightSample, out targetValidRegion, targetMinDepth, targetMaxDepth);
                    HOperatorSet.Threshold(sourceHeightSample, out sourceValidRegion, sourceMinDepth, sourceMaxDepth);
                    ThresholdInvalidDepthRegion(targetHeightSample, invalidDepthValue, out targetInvalidRegion);
                    ThresholdInvalidDepthRegion(sourceHeightSample, invalidDepthValue, out sourceInvalidRegion);
                    HOperatorSet.Difference(targetValidRegion, targetInvalidRegion, out targetFilteredValidRegion);
                    ReplaceHObject(ref targetValidRegion, ref targetFilteredValidRegion);
                    HOperatorSet.Difference(sourceValidRegion, sourceInvalidRegion, out sourceFilteredValidRegion);
                    ReplaceHObject(ref sourceValidRegion, ref sourceFilteredValidRegion);
                    HOperatorSet.Intersection(targetValidRegion, sourceValidRegion, out heightValidRegion);

                    double heightSampleCount = GetRegionArea(heightValidRegion);
                    if (heightSampleCount < options.MinSampleCount)
                        return CandidateScore.Invalid();

                    HOperatorSet.ScaleImage(targetHeightSample, out targetHeightPhysical, targetIntervalZ, targetTransform.Z);
                    HOperatorSet.ScaleImage(sourceHeightSample, out sourceHeightPhysical, sourceIntervalZ, 0.0);
                    HOperatorSet.SubImage(targetHeightPhysical, sourceHeightPhysical, out heightDiff, 1.0, 0.0);
                    (double heightOffset, double heightResidual) = GetIntensity(heightValidRegion, heightDiff);
                    if (useMedianHeight)
                        heightOffset = GetGrayFeature(heightValidRegion, heightDiff, "median");

                    HOperatorSet.AbsDiffImage(targetGraySample, sourceGraySample, out grayAbsDiff, 1.0);
                    (double grayMeanAbsDiff, _) = GetIntensity(fullSampleRegion, grayAbsDiff);

                    HOperatorSet.Difference(fullSampleRegion, targetValidRegion, out targetHoleRegion);
                    HOperatorSet.Difference(fullSampleRegion, sourceValidRegion, out sourceHoleRegion);
                    HOperatorSet.Union2(targetHoleRegion, sourceHoleRegion, out holeUnionRegion);
                    HOperatorSet.Intersection(targetHoleRegion, sourceHoleRegion, out holeIntersectionRegion);
                    HOperatorSet.Difference(holeUnionRegion, holeIntersectionRegion, out holeMismatchRegion);

                    double holeUnionCount = GetRegionArea(holeUnionRegion);
                    double holeMismatchCount = GetRegionArea(holeMismatchRegion);
                    double holeMismatchRatio = holeUnionCount > 0.0 ? holeMismatchCount / holeUnionCount : 0.0;

                    double score = heightResidual * options.HeightWeight +
                                   grayMeanAbsDiff * options.GrayWeight +
                                   holeMismatchRatio * options.MaskWeight;

                    return new CandidateScore(
                        true,
                        offsetCol,
                        offsetRow,
                        score,
                        heightOffset,
                        heightResidual,
                        grayMeanAbsDiff,
                        holeMismatchRatio,
                        (int)Math.Round(heightSampleCount));
                }
                finally
                {
                    DisposeObjects(
                        targetHeightPart,
                        sourceHeightPart,
                        targetGrayPart,
                        sourceGrayPart,
                        targetHeightSample,
                        sourceHeightSample,
                        targetGraySample,
                        sourceGraySample,
                        fullSampleRegion,
                        targetValidRegion,
                        sourceValidRegion,
                        targetInvalidRegion,
                        sourceInvalidRegion,
                        targetFilteredValidRegion,
                        sourceFilteredValidRegion,
                        heightValidRegion,
                        targetHeightPhysical,
                        sourceHeightPhysical,
                        heightDiff,
                        grayAbsDiff,
                        targetHoleRegion,
                        sourceHoleRegion,
                        holeUnionRegion,
                        holeIntersectionRegion,
                        holeMismatchRegion);
                }
            }
        }
        finally
        {
            DisposeObjects(targetGray, sourceGray, targetHeight, sourceHeight);
        }
    }

    private static bool AlignSamplesToCommonSize(
        ref HObject? targetHeightSample,
        ref HObject? sourceHeightSample,
        ref HObject? targetGraySample,
        ref HObject? sourceGraySample,
        out int sampleWidth,
        out int sampleHeight)
    {
        (int targetHeightWidth, int targetHeightHeight) = GetImageSize(targetHeightSample);
        (int sourceHeightWidth, int sourceHeightHeight) = GetImageSize(sourceHeightSample);
        (int targetGrayWidth, int targetGrayHeight) = GetImageSize(targetGraySample);
        (int sourceGrayWidth, int sourceGrayHeight) = GetImageSize(sourceGraySample);

        sampleWidth = Math.Min(
            Math.Min(targetHeightWidth, sourceHeightWidth),
            Math.Min(targetGrayWidth, sourceGrayWidth));
        sampleHeight = Math.Min(
            Math.Min(targetHeightHeight, sourceHeightHeight),
            Math.Min(targetGrayHeight, sourceGrayHeight));
        if (sampleWidth <= 0 || sampleHeight <= 0)
            return false;

        CropSampleToSize(ref targetHeightSample, sampleWidth, sampleHeight);
        CropSampleToSize(ref sourceHeightSample, sampleWidth, sampleHeight);
        CropSampleToSize(ref targetGraySample, sampleWidth, sampleHeight);
        CropSampleToSize(ref sourceGraySample, sampleWidth, sampleHeight);
        return true;
    }

    private static void CropSampleToSize(ref HObject? image, int width, int height)
    {
        (int currentWidth, int currentHeight) = GetImageSize(image);
        if (currentWidth == width && currentHeight == height)
            return;

        HOperatorSet.CropPart(image, out HObject cropped, 0, 0, width, height);
        image?.Dispose();
        image = cropped;
    }

    private static IEnumerable<int> EnumerateSearchOffsets(int center, int radius, int step)
    {
        step = Math.Max(1, step);
        int start = center - Math.Max(0, radius);
        int end = center + Math.Max(0, radius);
        int last = start - step;

        for (int value = start; value <= end; value += step)
        {
            last = value;
            yield return value;
        }

        if (last != center && center >= start && center <= end)
            yield return center;
        if (last != end)
            yield return end;
    }

    private static (int Width, int Height) GetImageSize(HObject? image)
    {
        ArgumentNullException.ThrowIfNull(image);
        HOperatorSet.GetImageSize(image, out HTuple width, out HTuple height);
        try
        {
            return (width.I, height.I);
        }
        finally
        {
            height.Dispose();
            width.Dispose();
        }
    }

    private static double GetRegionArea(HObject? region)
    {
        ArgumentNullException.ThrowIfNull(region);
        HOperatorSet.AreaCenter(region, out HTuple area, out HTuple row, out HTuple col);
        try
        {
            return area.D;
        }
        finally
        {
            col.Dispose();
            row.Dispose();
            area.Dispose();
        }
    }

    private static (double Mean, double Deviation) GetIntensity(HObject? region, HObject? image)
    {
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(image);
        HOperatorSet.Intensity(region, image, out HTuple mean, out HTuple deviation);
        try
        {
            return (mean.D, deviation.D);
        }
        finally
        {
            deviation.Dispose();
            mean.Dispose();
        }
    }

    private static double GetGrayFeature(HObject? region, HObject? image, string feature)
    {
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(image);
        HOperatorSet.GrayFeatures(region, image, feature, out HTuple value);
        try
        {
            return value.D;
        }
        finally
        {
            value.Dispose();
        }
    }

    private static void ThresholdInvalidDepthRegion(HObject? heightImage, double invalidValue, out HObject? invalidRegion)
    {
        ArgumentNullException.ThrowIfNull(heightImage);
        HOperatorSet.Threshold(heightImage, out invalidRegion, invalidValue, invalidValue);
    }

    private static void ReplaceHObject(ref HObject? target, ref HObject? source)
    {
        HObject? previous = target;
        if (!ReferenceEquals(previous, source))
            previous?.Dispose();

        target = source;
        source = null;
    }

    private static void DisposeObjects(params HObject?[] objects)
    {
        foreach (HObject? item in objects)
            item?.Dispose();
    }

    private static void ValidateStrictPositiveFinite(double value, string paramName)
    {
        if (!double.IsFinite(value) || value <= 0.0)
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be finite and positive.");
    }

    private static void ValidateNonNegativeFinite(double value, string paramName)
    {
        if (!double.IsFinite(value) || value < 0.0)
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be finite and non-negative.");
    }

    private static void ValidateDepthRange(double minDepth, double maxDepth, string paramName)
    {
        if (!double.IsFinite(minDepth) || !double.IsFinite(maxDepth) || minDepth > maxDepth)
            throw new ArgumentOutOfRangeException(paramName, $"Depth range must be finite and ordered. Min={minDepth}, Max={maxDepth}");
    }

    private readonly record struct CandidateScore(
        bool IsValid,
        int OffsetCol,
        int OffsetRow,
        double Score,
        double HeightOffset,
        double HeightResidual,
        double GrayMeanAbsDiff,
        double MaskMismatchRatio,
        int SampleCount)
    {
        public static CandidateScore Invalid()
        {
            return new CandidateScore(
                false,
                0,
                0,
                double.PositiveInfinity,
                0.0,
                double.PositiveInfinity,
                double.PositiveInfinity,
                double.PositiveInfinity,
                0);
        }
    }
}

internal sealed class CoarseImageSearchOptions
{
    public double SearchWindowX { get; init; }
    public double SearchWindowY { get; init; }
    public int SearchRadiusCol { get; init; }
    public int SearchRadiusRow { get; init; }
    public int CoarseStepCol { get; init; }
    public int CoarseStepRow { get; init; }
    public int CoarseSampleStepCol { get; init; }
    public int CoarseSampleStepRow { get; init; }
    public int FineSampleStepCol { get; init; }
    public int FineSampleStepRow { get; init; }
    public int MinSampleCount { get; init; }
    public double HeightWeight { get; init; }
    public double GrayWeight { get; init; }
    public double MaskWeight { get; init; }
}

internal readonly record struct CoarseImageRegistrationResult(
    PointCloudRigidTransform2D Transform,
    int OffsetCol,
    int OffsetRow,
    int InitialOffsetCol,
    int InitialOffsetRow,
    double Score,
    double HeightResidual,
    double GrayMeanAbsDiff,
    double MaskMismatchRatio,
    int SampleCount);
