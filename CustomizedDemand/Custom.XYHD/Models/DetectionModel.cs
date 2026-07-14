using HalconDotNet;
using Custom.XYHD.Services;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Custom.XYHD.Models
{
    [Serializable]
    public partial class DetectionModel : ModelParamBase
    {
        public const string DetectionOutputParamName = "XYHD_Detection";
        public const string LeftNgOutputParamName = "XYHD_LeftNg";
        public const string RightNgOutputParamName = "XYHD_RightNg";
        public const string IsNgOutputParamName = "XYHD_IsNg";
        private const string DefectPostProcessImageIndexKey = "DefectPostProcess.ImageIndex";
        private const string XYHDSourceImageIndexKey = "XYHD_SourceImageIndex";
        private const string DefaultOriginalImageInputName = "Image";
        private const string DefaultLeftImageInputName = "输入原图左";
        private const string DefaultRightImageInputName = "输入原图右";
        private const string DefaultResultsByImageInputName = "ResultsByImage";
        private const int DefaultLeftResultsSerial = 5;
        private const int DefaultRightResultsSerial = 9;
        private static string DefaultLeftResultsInputDisplayName => $"{DefaultResultsByImageInputName}@{DefaultLeftResultsSerial}";
        private static string DefaultRightResultsInputDisplayName => $"{DefaultResultsByImageInputName}@{DefaultRightResultsSerial}";
        private const string DetectionOutputParamDescription = "XYHD检测发布包";
        private const string LeftNgOutputParamDescription = "XYHD左路NG";
        private const string RightNgOutputParamDescription = "XYHD右路NG";
        private const string IsNgOutputParamDescription = "XYHD整帧NG";

        public DetectionModel()
        {
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        public override bool OnceInit()
        {
            if (IsOnceInit)
                return true;

            if (!base.OnceInit())
                return false;

            TriggerModuleRun ??= () => ExecuteModule().Result;
            IsOnceInit = true;
            return true;
        }

        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            var sw = Stopwatch.StartNew();
            long runId = DateTime.UtcNow.Ticks;
            List<XYHDInputPacket> imagePackets = null;
            List<XYHDInputPacket> outputPackets = null;
            List<PathResult> pathResults = null;
            try
            {
                AddLog(
                    $"执行开始: RunId={runId}, Serial={Serial}, Thread={Environment.CurrentManagedThreadId}, " +
                    $"InputOriginal={DescribeInputBindingForLog(InputOriginalImage, InputOriginalImageName)}, " +
                    $"LeftImage={DescribeInputBindingForLog(LeftInputImage, LeftInputImageName)}, " +
                    $"LeftResults={DescribeInputBindingForLog(LeftInputResults, LeftInputResultsName)}, " +
                    $"RightImage={DescribeInputBindingForLog(RightInputImage, RightInputImageName)}, " +
                    $"RightResults={DescribeInputBindingForLog(RightInputResults, RightInputResultsName)}, " +
                //$"ModuleInput={DescribeTransmitParamsForLog(moduleInputParam?.TransmitParams?.Values?.OfType<TransmitParam>())}",
                    "INFO");

                TryRebindInputLinks();
                SyncInputNamesFromLinks();
                AddLog(
            //$"输入重绑定完成: RunId={runId}, InputParams={DescribeTransmitParamsForLog(InputParams)}, " +
                    $"CacheKeys={DescribeNodeOutputCacheKeysForLog()}",
                    "DEBUG");

                pathResults = ParseAllDetectResults();
                AddLog(
                    $"解析缺陷结果完成: RunId={runId}, PathCount={pathResults.Count}, " +
                    $"Paths=[{string.Join("; ", pathResults.Select(DescribePathResultForLog))}]",
                    pathResults.Count == 0 ? "WARN" : "INFO");
                if (pathResults.Count == 0)
                {
                    PublishedPackets = new List<XYHDInputPacket>();
                    SetNgOutputs(false, false, updateParam: false);
                    EnsureDefaultOutputParam(Guid, Name);
                    RefreshPublishedPacketsOutputValue();
                    UpdateParam();
                    AddLog(
                        $"XYHD 同步模式：未解析到左右路缺陷结果，本次不发布 XYHD_Detection，RunId={runId}, " +
                        $"Elapsed={sw.Elapsed.TotalMilliseconds:F3}ms",
                        "WARN");
                    return Task.FromResult(Output = new ExecuteModuleOutput
                    {
                        RunStatus = NodeStatus.Success,
                        RunTime = sw.Elapsed.TotalMilliseconds
                    });
                }

                string originalSource = "SkippedOriginalImage";
                var frameId = DateTime.UtcNow.Ticks;
                var publishCount = 0;
                imagePackets = new List<XYHDInputPacket>();
                outputPackets = new List<XYHDInputPacket>();
                var leftNg = pathResults.Any(path => IsLeftPathText(path.pathName) && IsPathNg(path));
                var rightNg = pathResults.Any(path => IsRightPathText(path.pathName) && IsPathNg(path));
                if (SwapLeftRightPaths)
                    (leftNg, rightNg) = (rightNg, leftNg);

                foreach (var path in pathResults)
                {
                    var pathImage = CopyImageSafe(path.pathImage);
                    var pathImageSource = path.pathImageSource;
                    if (pathImage == null || !pathImage.IsInitialized())
                        pathImage = TryGetSelectedPathImage(path.pathName, out pathImageSource);
                    if (pathImage == null || !pathImage.IsInitialized())
                    {
                        pathImage?.Dispose();
                        pathImage = null;
                        pathImageSource ??= "固定输入未命中";
                        AddLog($"[TryGetPathImage] {path.pathName}路固定图像未命中或无有效图像，本次不做裁图兜底", "WARN");
                    }

                    var packet = new XYHDInputPacket
                    {
                        OriginalImage = null,
                        PathImage = pathImage,
                        PathName = path.pathName,
                        IsOks = path.isOks,
                        DefectResults = path.results,
                        ReceiveTime = DateTime.Now,
                        SourceSerial = path.sourceSerial,
                        OwnerSerial = Serial,
                        HasFieldOrientationSettings = true,
                        SwapLeftRightPaths = SwapLeftRightPaths,
                        LeftPathXMirror = LeftPathXMirror,
                        RightPathXMirror = RightPathXMirror,
                        FrameId = frameId
                    };

                    imagePackets.Add(packet);
                    outputPackets.Add(CreateOutputPacketWithoutImages(packet));
                    AddLog(
                        $"准备发布事件: RunId={runId}, Event=XYHD_Detection, Path={path.pathName}, " +
                  // $"Frame={frameId}, SourceSerial={path.sourceSerial}, PacketImage={DescribeValueForLog(packet.PathImage)}",
                        "DEBUG");
                    PrismProvider.EventAggregator.GetEvent<OutputResultEvent>()
                        .Publish(("XYHD_Detection", packet));

                    publishCount++;
                    AddLog(
                        $"XYHD 同步模式发布: RunId={runId}, Frame={frameId}, Path={path.pathName}, Serial={path.sourceSerial}, " +
                        $"OriginalSource={originalSource}, PathSource={pathImageSource ?? "未命中"}, " +
                        $"Original={(packet.OriginalImage != null && packet.OriginalImage.IsInitialized() ? "OK" : "Null")}, " +
                        $"PathImage={(packet.PathImage != null && packet.PathImage.IsInitialized() ? "OK" : "Null")}, " +
                        $"缺陷数={path.results?.Count ?? 0}",
                        "INFO");
                }

                PublishedPackets = outputPackets;
                outputPackets = null;
                RetirePacketBatch(imagePackets);
                imagePackets = null;
                SetNgOutputs(leftNg, rightNg, updateParam: false);
                EnsureDefaultOutputParam(Guid, Name);
                RefreshPublishedPacketsOutputValue();
                UpdateParam();
                AddLog(
                    $"XYHD 同步模式完成: RunId={runId}, Frame={frameId}, 发布包数={publishCount}, " +
                    $"LeftNg={leftNg}, RightNg={rightNg}, IsNg={IsNgOutput}, " +
               // $"OutputParams={DescribeTransmitParamsForLog(OutputParams)}, Elapsed={sw.Elapsed.TotalMilliseconds:F3}ms",
                    "INFO");

                return Task.FromResult(Output = new ExecuteModuleOutput
                {
                    RunStatus = NodeStatus.Success,
                    RunTime = sw.Elapsed.TotalMilliseconds
                });
            }
            catch (Exception ex)
            {
                DisposePacketBatch(imagePackets);
                DisposePacketBatch(outputPackets);
                AddLog($"输入处理异常: RunId={runId}, Elapsed={sw.Elapsed.TotalMilliseconds:F3}ms, Error={ex}", "ERROR");
                return Task.FromResult(Output = new ExecuteModuleOutput
                {
                    RunStatus = NodeStatus.Error,
                    RunTime = sw.Elapsed.TotalMilliseconds
                });
            }
            finally
            {
                DisposePathResultImages(pathResults);
                AddLog($"执行结束: RunId={runId}, TotalElapsed={sw.Elapsed.TotalMilliseconds:F3}ms", "DEBUG");
            }
        }

        public override void Dispose()
        {
            foreach (var output in OutputParams?.Where(IsDetectionOutputParam) ?? Enumerable.Empty<TransmitParam>())
                output.Value = null;

            DisposePacketBatch(_publishedPackets);
            _publishedPackets?.Clear();

            while (_retiredPacketBatches.Count > 0)
                DisposePacketBatch(_retiredPacketBatches.Dequeue());

            base.Dispose();
        }

        public override bool LoadKeyParam()
        {
            base.LoadKeyParam();
            TryRebindInputLinks();
            SyncInputNamesFromLinks();
            EnsureDefaultOutputParam(Guid, Name);
            return true;
        }
    }
}
