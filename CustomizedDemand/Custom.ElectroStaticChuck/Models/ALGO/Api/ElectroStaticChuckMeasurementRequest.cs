using Custom.ElectroStaticChuckMeasure.ALGO.Calibration;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Api;

/// <summary>
/// 生产测量 pipeline 的请求参数。
/// </summary>
public class ElectroStaticChuckMeasurementRequest
{
    /// <summary>
    /// 待测产品的图像输入。
    /// </summary>
    public ElectroStaticChuckInput Input { get; init; } = null!;

    /// <summary>
    /// 预先完成的 CalibrationState；启用校正时必须提供对应的已标定状态。
    /// </summary>
    public CalibrationState? Calibration { get; init; }

    /// <summary>
    /// 测量、校正、配准、拼接、渲染和导出参数。
    /// </summary>
    public ElectroStaticChuckParameters Parameters { get; init; } = new();

    /// <summary>
    /// 运行选项。
    /// </summary>
    public ElectroStaticChuckRunOptions Options { get; init; } = new();

    /// <summary>
    /// 输出目录；启用 ExportResults 时必须显式提供。
    /// </summary>
    public string? OutputDirectory { get; init; }
}
