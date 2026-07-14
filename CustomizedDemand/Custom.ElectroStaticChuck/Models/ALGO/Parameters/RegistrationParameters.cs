namespace Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

[Serializable]
public sealed class RegistrationParameters
{
    public CoarseRegistrationParameters Coarse { get; init; } = new();

    public FineRegistrationParameters Fine { get; init; } = new();
}

[Serializable]
public sealed class CoarseRegistrationParameters
{
    /// <summary>
    /// X 方向粗搜索半径，单位与图像 X 方向点间距一致。
    /// </summary>
    public double SearchWindowX { get; init; } = 50.0;

    /// <summary>
    /// Y 方向粗搜索半径，单位与图像 Y 方向点间距一致。
    /// </summary>
    public double SearchWindowY { get; init; } = 500.0;

    /// <summary>
    /// 粗配准搜索加速系数；值越大搜索和评分越稀疏。
    /// </summary>
    public double SearchSpeedFactor { get; init; } = 1.0;

    /// <summary>
    /// 高度残差在粗配准评分中的权重。
    /// </summary>
    public double HeightWeight { get; init; } = 0.05;

    /// <summary>
    /// 灰度平均绝对差在粗配准评分中的权重。
    /// </summary>
    public double GrayWeight { get; init; } = 0.05;

    /// <summary>
    /// 无效区域 mask 不匹配在粗配准评分中的权重。
    /// </summary>
    public double MaskWeight { get; init; }
}

public enum FineRegistrationMode
{
    /// <summary>
    /// 仅执行粗配准，不执行 ICP 精配准。
    /// </summary>
    CoarseOnly = 0,

    /// <summary>
    /// 先执行粗配准，再执行 ICP 精配准。
    /// </summary>
    CoarseAndFine = 1,

    /// <summary>
    /// 不执行图像/点云配准，仅使用文件名中的机械偏移。
    /// </summary>
    NoRegistration = 2
}

[Serializable]
public sealed class FineRegistrationParameters
{
    /// <summary>
    /// 配准流程模式。
    /// </summary>
    public FineRegistrationMode Mode { get; init; } = FineRegistrationMode.CoarseAndFine;

    /// <summary>
    /// NoRegistration 模式下是否根据相邻帧重叠区域估计 Z 高度基准修正。
    /// </summary>
    public bool NoRegistrationUseZCorrection { get; init; }

    /// <summary>
    /// NoRegistration Z-only 修正的重叠区域采样步长，单位为像素。
    /// </summary>
    public int NoRegistrationZSampleStepPixel { get; init; } = 4;

    /// <summary>
    /// NoRegistration Z-only 修正所需的最少有效重叠样本数。
    /// </summary>
    public int NoRegistrationMinZSampleCount { get; init; } = 200;

    /// <summary>
    /// ICP 前体素降采样叶尺寸相对 XY 最大点间距的倍率。
    /// </summary>
    public double IcpLeafSizeMultiplier { get; init; } = 1.0;

    /// <summary>
    /// ICP 最大迭代次数。
    /// </summary>
    public int MaxIterations { get; init; } = 1;

    /// <summary>
    /// ICP 最大对应点距离相对 XY 最大点间距的倍率。
    /// </summary>
    public double MaxCorrespondenceDistanceMultiplier { get; init; } = 3.0;

    /// <summary>
    /// ICP 最大对应点距离下限。
    /// </summary>
    public double MinMaxCorrespondenceDistance { get; init; } = 1.0;

    /// <summary>
    /// ICP transform 收敛阈值。
    /// </summary>
    public double TransformationEpsilon { get; init; } = 0.000001;

    /// <summary>
    /// ICP fitness 收敛阈值。
    /// </summary>
    public double FitnessEpsilon { get; init; } = 0.000001;

    /// <summary>
    /// ICP 接受结果所需的最小 inlier ratio，取值范围为 (0, 1]。
    /// </summary>
    public double MinInlierRatio { get; init; } = 0.05;

    public bool OptimizeX { get; init; } = true;

    public bool OptimizeY { get; init; } = true;

    public bool OptimizeZ { get; init; } = true;

    public bool OptimizeYawDeg { get; init; } = true;
}
