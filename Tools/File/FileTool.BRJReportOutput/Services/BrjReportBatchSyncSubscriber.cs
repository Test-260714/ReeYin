#nullable enable

using FileTool.BRJReportOutput.Models;
using Prism.Events;
using Prism.Ioc;
using ReeYin_V.Core.Events;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileTool.BRJReportOutput.Services
{
    internal static class BrjReportBatchSyncSubscriber
    {
        private static readonly object SubscribeSync = new();
        private static bool _isSubscribed;
        private static IEventAggregator? _eventAggregator;

        public static void Subscribe(IContainerProvider containerProvider)
        {
            if (containerProvider == null)
            {
                return;
            }

            lock (SubscribeSync)
            {
                if (_isSubscribed)
                {
                    return;
                }

                _eventAggregator = containerProvider.Resolve<IEventAggregator>();
                _eventAggregator
                    .GetEvent<DefectBatchReportSyncEvent>()
                    .Subscribe(Handle, ThreadOption.BackgroundThread, keepSubscriberReferenceAlive: true);
                _isSubscribed = true;
            }

            Logs.LogInfo("[BRJReport] DefectBatchReportSyncEvent subscribed.");
        }

        private static async void Handle(DefectBatchReportSyncRequest request)
        {
            if (request == null)
            {
                return;
            }

            try
            {
                List<DefectBatchReportItem> defects = request.Defects ?? new List<DefectBatchReportItem>();
                BrjReportRecord record = ToRecord(request, defects);

                Logs.LogInfo($"[BRJReport] Sync received: SN={record.SN}, Version={request.SnapshotVersion}, Defects={defects.Count}");
                await BrjReportStorage.SaveRollAsync(record, ToDefectRecords(request, defects)).ConfigureAwait(false);
                if (request.IsRollCompleted)
                {
                    _eventAggregator?.GetEvent<BrjReportDataChangedEvent>().Publish(new BrjReportDataChangedPayload
                    {
                        SN = record.SN,
                        DefectCount = defects.Count,
                        DetectMeters = record.DetectMeters,
                        IsRollCompleted = true,
                        SnapshotVersion = request.SnapshotVersion,
                    });
                }

                request.Completion?.TrySetResult(new DefectBatchReportSyncResult
                {
                    Success = true,
                    Message = request.IsRollCompleted
                        ? $"BRJ report synced and completed: SN={record.SN}, defects={defects.Count}"
                        : $"BRJ report synced: SN={record.SN}, defects={defects.Count}",
                    SN = record.SN,
                    DefectCount = defects.Count,
                    SnapshotVersion = request.SnapshotVersion,
                });

                Logs.LogInfo($"[BRJReport] Sync saved: SN={record.SN}, Version={request.SnapshotVersion}, Defects={defects.Count}");
            }
            catch (Exception ex)
            {
                request.Completion?.TrySetResult(new DefectBatchReportSyncResult
                {
                    Success = false,
                    Message = $"BRJ report sync failed: {ex.Message}",
                    SN = request.SN ?? string.Empty,
                    DefectCount = request.Defects?.Count ?? 0,
                    SnapshotVersion = request.SnapshotVersion,
                });

                Logs.LogError($"[BRJReport] Sync failed: SN={request.SN}, Version={request.SnapshotVersion}, Error={ex}");
            }
        }

        private static BrjReportRecord ToRecord(DefectBatchReportSyncRequest request, IReadOnlyCollection<DefectBatchReportItem> defects)
        {
            DateTime syncTime = request.SyncTime == default ? DateTime.Now : request.SyncTime;
            DateTime createTime = request.BatchStartedTime == default ? syncTime : request.BatchStartedTime;

            return new BrjReportRecord
            {
                SN = NormalizeText(request.SN),
                CreateTime = createTime,
                DefectCount = defects.Count,
                DetectMeters = NormalizeNumber(request.DetectMeters),
                EndTime = request.IsRollCompleted ? request.BatchEndedTime ?? syncTime : null,
                ProductWidthMm = NormalizeNumber(request.ProductWidthMm),
                RollLengthM = NormalizeNumber(request.DetectMeters),
                OperatorName = NormalizeText(request.OperatorName),
                ShiftName = NormalizeText(request.ShiftName),
                ProductModel = NormalizeText(request.ProductModel),
                ImageCount = Math.Max(0, request.TotalFrames),
                OkCount = Math.Max(0, request.OkFrames),
                NgCount = Math.Max(0, request.NgFrames),
                CameraCount = request.CameraCount > 0 ? request.CameraCount : ResolveCameraCount(defects),
                ResolutionX = NormalizeText(request.ResolutionX),
                ResolutionY = NormalizeText(request.ResolutionY),
                ImageWidth = NormalizeText(request.ImageWidth),
                ImageHeight = NormalizeText(request.ImageHeight),
                SlitLeftCoordinates = NormalizeText(request.SlitLeftCoordinates),
                SlitRightCoordinates = NormalizeText(request.SlitRightCoordinates),
            };
        }

        private static List<BrjDefectRecord> ToDefectRecords(DefectBatchReportSyncRequest request, IEnumerable<DefectBatchReportItem> defects)
        {
            DateTime syncTime = request.SyncTime == default ? DateTime.Now : request.SyncTime;
            return (defects ?? Enumerable.Empty<DefectBatchReportItem>())
                .Where(item => item != null)
                .Select((item, index) => new BrjDefectRecord
                {
                    SN = NormalizeText(request.SN),
                    DefectIndex = item.DefectIndex > 0 ? item.DefectIndex : index + 1,
                    CameraIndex = Math.Max(0, item.CameraIndex),
                    CameraName = NormalizeText(item.CameraName),
                    SegmentIndex = Math.Max(0, item.SegmentIndex),
                    SlitIndex = Math.Max(0, item.SlitIndex),
                    DefectType = NormalizeText(item.DefectType),
                    AreaMm2 = NormalizeNumber(item.AreaMm2),
                    DiameterMm = NormalizeNumber(item.DiameterMm),
                    PositionXMm = NormalizeNumber(item.PositionXMm),
                    PositionYM = NormalizeNumber(item.PositionYM),
                    DefectImagePath = NormalizeText(item.DefectImagePath),
                    CreateTime = item.CreateTime == default ? syncTime : item.CreateTime,
                })
                .ToList();
        }

        private static int ResolveCameraCount(IReadOnlyCollection<DefectBatchReportItem> defects)
        {
            if (defects.Count == 0)
            {
                return 0;
            }

            return defects.Max(item => Math.Max(0, item.CameraIndex)) + 1;
        }

        private static string NormalizeText(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static double NormalizeNumber(double value)
        {
            return double.IsFinite(value) ? value : 0d;
        }
    }

    internal sealed class BrjReportDataChangedEvent : PubSubEvent<BrjReportDataChangedPayload>
    {
    }

    internal sealed class BrjReportDataChangedPayload
    {
        public string SN { get; init; } = string.Empty;

        public int DefectCount { get; init; }

        public double DetectMeters { get; init; }

        public bool IsRollCompleted { get; init; }

        public long SnapshotVersion { get; init; }
    }
}
