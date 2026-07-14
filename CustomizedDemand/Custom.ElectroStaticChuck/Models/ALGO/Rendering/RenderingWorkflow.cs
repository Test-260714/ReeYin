using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Measurement;
using HalconDotNet;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Rendering;

public sealed class RenderingWorkflow
{
    private readonly ResultOverlayRenderer _renderer = new();

    public RenderResult Render(MeasurementResult measurement, ElectroStaticChuckContext context)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Options.RenderOverlay || !HasDisplayGrayImage(measurement))
            return new RenderResult(null);

        return new RenderResult(_renderer.Render(measurement));
    }

    private static bool HasDisplayGrayImage(MeasurementResult measurement)
    {
        try
        {
            HOperatorSet.CountChannels(measurement.DisplayGrayImage, out HTuple channels);
            try
            {
                if (channels.Length == 0 || channels[0].I != 1)
                    return false;
            }
            finally
            {
                channels.Dispose();
            }

            HOperatorSet.GetImageSize(measurement.DisplayGrayImage, out HTuple width, out HTuple height);
            try
            {
                return width.I > 0 && height.I > 0;
            }
            finally
            {
                width.Dispose();
                height.Dispose();
            }
        }
        catch (HOperatorException)
        {
            return false;
        }
    }
}
