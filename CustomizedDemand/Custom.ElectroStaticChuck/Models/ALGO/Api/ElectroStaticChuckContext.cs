using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Api;

/// <summary>
/// 提供 ElectroStaticChuck ALGO 单次运行范围内的上下文信息。
/// </summary>
/// <remarks>
/// Context 容器本身不可变，但会保留调用方传入的参数和选项对象引用。
/// 如果调用方需要快照隔离，应在调用前自行复制参数和选项对象。
/// </remarks>
public sealed class ElectroStaticChuckContext
{
    private ElectroStaticChuckContext(
        ElectroStaticChuckParameters parameters,
        ElectroStaticChuckRunOptions options,
        string? outputDirectory,
        ElectroStaticChuckInput? input)
    {
        Parameters = parameters;
        Options = options;
        OutputDirectory = outputDirectory;
        Input = input;
    }

    /// <summary>
    /// 获取调用方传入的参数对象引用。
    /// </summary>
    public ElectroStaticChuckParameters Parameters { get; }

    /// <summary>
    /// 获取调用方传入的运行选项对象引用；未传入时使用新的默认选项实例。
    /// </summary>
    public ElectroStaticChuckRunOptions Options { get; }

    /// <summary>
    /// 获取当前运行上下文关联的可选输出目录。
    /// </summary>
    public string? OutputDirectory { get; }

    /// <summary>
    /// 获取当前运行上下文关联的可选输入。
    /// </summary>
    public ElectroStaticChuckInput? Input { get; }

    public IAlgoProgressReporter ProgressReporter => Options.ProgressReporter ?? NullAlgoProgressReporter.Instance;

    public void ReportProgress(AlgoProgressEvent progress)
    {
        ProgressReporter.Report(progress);
    }

    /// <summary>
    /// 创建运行上下文，并保留传入的参数和选项对象引用。
    /// </summary>
    /// <remarks>
    /// 此方法不会克隆 <paramref name="parameters"/> 或 <paramref name="options"/>。
    /// 返回的 context 只保证不能通过 context API 替换这些引用。
    /// </remarks>
    public static ElectroStaticChuckContext Create(
        ElectroStaticChuckParameters parameters,
        ElectroStaticChuckRunOptions? options = null,
        string? outputDirectory = null,
        ElectroStaticChuckInput? input = null)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return new ElectroStaticChuckContext(parameters, options ?? new ElectroStaticChuckRunOptions(), outputDirectory, input);
    }
}
