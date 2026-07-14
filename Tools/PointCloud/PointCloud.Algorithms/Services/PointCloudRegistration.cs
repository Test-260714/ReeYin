using PointCloud.Algorithms.Dtos;
using PointCloud.Algorithms.Native;
using PointCloud.Interop;

namespace PointCloud.Algorithms.Services;

public static class PointCloudRegistration
{
    private const int OptimizeXMask = 1;
    private const int OptimizeYMask = 2;
    private const int OptimizeZMask = 4;
    private const int OptimizeYawDegMask = 8;

    public static PointCloudRegistrationResult RegisterConstrainedIcp(
        PointCloudHandle source,
        PointCloudHandle target,
        PointCloudRigidTransform2D initialTransform,
        PointCloudRegistrationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ValidateTransform(initialTransform);
        ValidatePointClouds(source, target);

        PointCloudRegistrationOptions effectiveOptions = options ?? new PointCloudRegistrationOptions();
        ValidateOptions(effectiveOptions);

        double[] outTransform = new double[4];
        double[] outMetrics = new double[5];
        int result = PclAlgorithmsNative.ConstrainedIcpRegistration(
            source,
            target,
            initialTransform.X,
            initialTransform.Y,
            initialTransform.Z,
            initialTransform.YawDeg,
            effectiveOptions.MaxIterations,
            effectiveOptions.MaxCorrespondenceDistance,
            effectiveOptions.TransformationEpsilon,
            effectiveOptions.FitnessEpsilon,
            CreateOptimizationMask(effectiveOptions),
            outTransform,
            outMetrics);

        if (result == 0)
        {
            throw new PointCloudInteropException("Constrained ICP registration failed.");
        }

        double inlierRatio = outMetrics[1];
        if (inlierRatio < effectiveOptions.MinInlierRatio)
        {
            throw new PointCloudInteropException(
                $"Constrained ICP inlier ratio {inlierRatio} is below the minimum {effectiveOptions.MinInlierRatio}.");
        }

        bool converged = outMetrics[4] != 0.0;

        return new PointCloudRegistrationResult
        {
            Converged = converged,
            Transform = new PointCloudRigidTransform2D(outTransform[0], outTransform[1], outTransform[2], outTransform[3]),
            Rmse = outMetrics[0],
            InlierRatio = inlierRatio,
            InlierCount = CheckedMetricToInt(outMetrics[2], nameof(PointCloudRegistrationResult.InlierCount)),
            Iterations = CheckedMetricToInt(outMetrics[3], nameof(PointCloudRegistrationResult.Iterations)),
            Message = "Constrained ICP completed."
        };
    }

    private static void ValidatePointClouds(PointCloudHandle source, PointCloudHandle target)
    {
        int sourceCount = PointClouds.Count(source);
        int targetCount = PointClouds.Count(target);
        if (sourceCount < 3 || targetCount < 3)
        {
            throw new PointCloudInteropException("Constrained ICP registration requires at least 3 source and target points.");
        }
    }

    private static void ValidateTransform(PointCloudRigidTransform2D transform)
    {
        ThrowIfNonFinite(transform.X, nameof(transform.X));
        ThrowIfNonFinite(transform.Y, nameof(transform.Y));
        ThrowIfNonFinite(transform.Z, nameof(transform.Z));
        ThrowIfNonFinite(transform.YawDeg, nameof(transform.YawDeg));
    }

    private static void ValidateOptions(PointCloudRegistrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxIterations);
        ThrowIfNotPositiveFinite(options.MaxCorrespondenceDistance, nameof(options.MaxCorrespondenceDistance));
        ThrowIfNotPositiveFinite(options.TransformationEpsilon, nameof(options.TransformationEpsilon));
        ThrowIfNotPositiveFinite(options.FitnessEpsilon, nameof(options.FitnessEpsilon));

        if (!double.IsFinite(options.MinInlierRatio) || options.MinInlierRatio <= 0.0 || options.MinInlierRatio > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MinInlierRatio),
                options.MinInlierRatio,
                "MinInlierRatio must be finite and inside (0, 1].");
        }
    }

    private static int CreateOptimizationMask(PointCloudRegistrationOptions options)
    {
        int mask = 0;
        if (options.OptimizeX)
            mask |= OptimizeXMask;
        if (options.OptimizeY)
            mask |= OptimizeYMask;
        if (options.OptimizeZ)
            mask |= OptimizeZMask;
        if (options.OptimizeYawDeg)
            mask |= OptimizeYawDegMask;

        return mask;
    }

    private static void ThrowIfNotPositiveFinite(double value, string paramName)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Registration options must be positive finite values.");
        }
    }

    private static void ThrowIfNonFinite(double value, string paramName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Registration transform values must be finite.");
        }
    }

    private static int CheckedMetricToInt(double value, string metricName)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > int.MaxValue)
        {
            throw new PointCloudInteropException($"Native constrained ICP returned invalid {metricName}: {value}.");
        }

        return (int)Math.Round(value);
    }
}
