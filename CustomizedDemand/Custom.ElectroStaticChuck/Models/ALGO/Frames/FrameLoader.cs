using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using HalconDotNet;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Frames;

public static class FrameLoader
{
    public static ImageFrame Load(FrameDescriptor descriptor, SensorParameters sensor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(sensor);
        Validation.Finite(sensor.InvalidValue, nameof(sensor.InvalidValue), descriptor.HeightImagePath);

        HObject? rawGray = null;
        HObject? rawHeight = null;
        HObject? gray = null;
        HObject? height = null;
        HObject? validMask = null;
        HObject? invalidRegion = null;
        HObject? filteredMask = null;

        try
        {
            HOperatorSet.ReadImage(out rawGray, descriptor.GrayImagePath);
            HOperatorSet.ReadImage(out rawHeight, descriptor.HeightImagePath);
            HOperatorSet.ConvertImageType(rawGray, out gray, "byte");
            HOperatorSet.ConvertImageType(rawHeight, out height, "real");
            ValidateSingleChannelImage(gray!, descriptor.GrayImagePath);

            if (descriptor.IsFlip)
            {
                HOperatorSet.MirrorImage(gray, out HObject? flippedGray, "row");
                gray.Dispose();
                gray = flippedGray;
                HOperatorSet.MirrorImage(height, out HObject? flippedHeight, "row");
                height.Dispose();
                height = flippedHeight;
            }

            ValidateMatchingImageSize(gray!, height!, descriptor);
            HOperatorSet.Threshold(height, out validMask, descriptor.MinDepth, descriptor.MaxDepth);
            HOperatorSet.Threshold(height, out invalidRegion, sensor.InvalidValue-1, sensor.InvalidValue+1);
            HOperatorSet.Difference(validMask, invalidRegion, out filteredMask);
            validMask.Dispose();
            validMask = filteredMask;
            filteredMask = null;

            (int width, int heightPixels) = GetImageSize(height!);
            long validPointCount = CountRegionArea(validMask!);
            HObject? frameGray = null;
            HObject? frameHeight = null;
            HObject? frameValidMask = null;
            try
            {
                frameGray = gray!.Clone();
                frameHeight = height!.Clone();
                frameValidMask = validMask!.Clone();

                var frame = new ImageFrame(
                    descriptor,
                    frameGray,
                    frameHeight,
                    frameValidMask,
                    width,
                    heightPixels,
                    validPointCount);

                frameGray = null;
                frameHeight = null;
                frameValidMask = null;
                return frame;
            }
            finally
            {
                HObjectUtils.DisposeAll(frameGray, frameHeight, frameValidMask);
            }
        }
        finally
        {
            HObjectUtils.DisposeAll(rawGray, rawHeight, gray, height, validMask, invalidRegion, filteredMask);
        }
    }

    private static void ValidateSingleChannelImage(HObject image, string imagePath)
    {
        HOperatorSet.CountChannels(image, out HTuple channelCount);
        try
        {
            if (channelCount.I != 1)
                throw new InvalidOperationException($"Streaming gray image must be single-channel; Channels={channelCount.I}, Path={imagePath}");
        }
        finally
        {
            channelCount.Dispose();
        }
    }

    private static void ValidateMatchingImageSize(HObject gray, HObject height, FrameDescriptor descriptor)
    {
        HOperatorSet.GetImageSize(gray, out HTuple grayWidth, out HTuple grayHeight);
        HOperatorSet.GetImageSize(height, out HTuple heightWidth, out HTuple heightHeight);
        try
        {
            if (grayWidth.I != heightWidth.I || grayHeight.I != heightHeight.I)
            {
                throw new InvalidOperationException(
                    $"Streaming gray/height image size mismatch for frame {descriptor.Index}: " +
                    $"Gray={grayWidth.I}x{grayHeight.I}, Height={heightWidth.I}x{heightHeight.I}, " +
                    $"GrayPath={descriptor.GrayImagePath}, HeightPath={descriptor.HeightImagePath}");
            }
        }
        finally
        {
            grayWidth.Dispose();
            grayHeight.Dispose();
            heightWidth.Dispose();
            heightHeight.Dispose();
        }
    }

    private static (int Width, int Height) GetImageSize(HObject image)
    {
        HOperatorSet.GetImageSize(image, out HTuple width, out HTuple height);
        try
        {
            return (width.I, height.I);
        }
        finally
        {
            width.Dispose();
            height.Dispose();
        }
    }

    private static long CountRegionArea(HObject region)
    {
        HOperatorSet.AreaCenter(region, out HTuple area, out HTuple centerRow, out HTuple centerCol);
        try
        {
            return Math.Max(0L, (long)Math.Round(area.D));
        }
        finally
        {
            area.Dispose();
            centerRow.Dispose();
            centerCol.Dispose();
        }
    }
}
