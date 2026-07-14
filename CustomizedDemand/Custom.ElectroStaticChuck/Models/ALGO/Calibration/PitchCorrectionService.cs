using System.Runtime.InteropServices;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using HalconDotNet;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Calibration;

public sealed class PitchCorrectionService
{
    public HObject Correct(ImageFrame frame, PitchCalibrationState state)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(state);

        if (!state.IsCalibrated || Math.Abs(state.Slope) <= 1e-10)
            return CopyReducedHeightImage(frame);

        float[] imageData;
        HTuple? pointer = null;
        HTuple? type = null;
        HTuple? width = null;
        HTuple? height = null;
        try
        {
            HOperatorSet.GetImagePointer1(
                frame.HeightImage,
                out HTuple pointerValue,
                out HTuple typeValue,
                out HTuple widthValue,
                out HTuple heightValue);

            pointer = pointerValue;
            type = typeValue;
            width = widthValue;
            height = heightValue;

            imageData = new float[frame.Width * frame.Height];
            Marshal.Copy(pointer.IP, imageData, 0, imageData.Length);
        }
        finally
        {
            pointer?.Dispose();
            type?.Dispose();
            width?.Dispose();
            height?.Dispose();
        }

        double intervalX = frame.Descriptor.IntervalX;
        for (int row = 0; row < frame.Height; row++)
        {
            int rowOffset = row * frame.Width;
            for (int col = 0; col < frame.Width; col++)
            {
                // 保持旧算法行为：DepthBase 仅作为标定输出保留，当前校正只扣除 slope 项。
                double compensation = col * intervalX * state.Slope;
                imageData[rowOffset + col] = (float)(imageData[rowOffset + col] - compensation);
            }
        }

        GCHandle handle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
        try
        {
            HOperatorSet.GenImage1(
                out HObject corrected,
                "real",
                frame.Width,
                frame.Height,
                handle.AddrOfPinnedObject());
            return ReduceToValidMask(corrected, frame.ValidMask);
        }
        finally
        {
            handle.Free();
        }
    }

    private static HObject CopyReducedHeightImage(ImageFrame frame)
    {
        HObject copy = frame.HeightImage.Clone();
        return ReduceToValidMask(copy, frame.ValidMask);
    }

    private static HObject ReduceToValidMask(HObject image, HObject validMask)
    {
        try
        {
            HOperatorSet.ReduceDomain(image, validMask, out HObject reduced);
            image.Dispose();
            return reduced;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }
}
