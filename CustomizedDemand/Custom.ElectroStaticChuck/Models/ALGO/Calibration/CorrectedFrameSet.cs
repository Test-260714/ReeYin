using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using HalconDotNet;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Calibration;

public sealed class CorrectedFrameSet
{
    private readonly PitchCorrectionService _pitchCorrection = new();

    public CorrectedFrameSet(FrameSet frames)
        : this(frames, new CalibrationState(), usePitchCorrection: false)
    {
    }

    public CorrectedFrameSet(FrameSet frames, CalibrationState calibration, bool usePitchCorrection)
    {
        Frames = frames ?? throw new ArgumentNullException(nameof(frames));
        Calibration = calibration ?? throw new ArgumentNullException(nameof(calibration));
        UsePitchCorrection = usePitchCorrection;
    }

    public FrameSet Frames { get; }

    public CalibrationState Calibration { get; }

    public bool UsePitchCorrection { get; }

    public ImageFrame LoadFrame(int index, SensorParameters sensor)
    {
        ArgumentNullException.ThrowIfNull(sensor);
        if (index < 0 || index >= Frames.Frames.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Corrected frame index is outside the frame set.");

        FrameDescriptor descriptor = Frames.Frames[index];
        if (!ShouldApplyPitchCorrection())
            return FrameLoader.Load(descriptor, sensor);

        ImageFrame? rawFrame = null;
        HObject? grayImage = null;
        HObject? heightImage = null;
        HObject? validMask = null;
        try
        {
            rawFrame = FrameLoader.Load(descriptor, sensor);
            grayImage = rawFrame.GrayImage.Clone();
            heightImage = _pitchCorrection.Correct(rawFrame, Calibration.Pitch);
            validMask = rawFrame.ValidMask.Clone();

            var correctedFrame = new ImageFrame(
                descriptor,
                grayImage,
                heightImage,
                validMask,
                rawFrame.Width,
                rawFrame.Height,
                rawFrame.ValidPointCount);

            grayImage = null;
            heightImage = null;
            validMask = null;
            return correctedFrame;
        }
        finally
        {
            rawFrame?.Dispose();
            HObjectUtils.DisposeAll(grayImage, heightImage, validMask);
        }
    }

    private bool ShouldApplyPitchCorrection()
    {
        return UsePitchCorrection && Calibration.Pitch.IsCalibrated;
    }
}
