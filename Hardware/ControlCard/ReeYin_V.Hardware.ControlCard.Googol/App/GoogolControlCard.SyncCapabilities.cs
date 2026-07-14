using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    public partial class GoogolControlCard : ICoordinatedMotionCard, IBufferedMotionCard, ISynchronizedTriggerCard
    {
        public bool SupportsCoordinatedMotion => true;

        public bool SupportsBufferedMotion => true;

        public SynchronizedTriggerCapabilities TriggerCapabilities =>
            SynchronizedTriggerCapabilities.PositionCompare |
            SynchronizedTriggerCapabilities.BufferedIo;

        public bool MoveCoordinated(CoordinatedMotionRequest request, out string message)
        {
            message = string.Empty;
            if (request == null)
            {
                message = "Synchronized motion request cannot be null.";
                return false;
            }

            try
            {
                return request.Kind switch
                {
                    CoordinatedMotionKind.Line => LineInterpoMoving(BuildLineParam(request)),
                    CoordinatedMotionKind.Arc when request.ArcParam != null => ArcInterpoMoving(request.ArcParam),
                    CoordinatedMotionKind.Custom when request.CustomCommand != null =>
                        CustomInterpolationMoving(BuildCustomParam(request), request.CustomCommand, request.WaitForEnd),
                    _ => FailUnsupportedCoordinatedMotion(request.Kind, out message)
                };
            }
            catch (Exception ex)
            {
                message = $"Googol synchronized motion failed: {ex.Message}";
                return false;
            }
        }

        public bool ClearMotionBuffer(short coordinateOrBuffer, out string message)
        {
            var ok = CrdBufClear(coordinateOrBuffer);
            message = ok ? "Googol CRD FIFO cleared." : "Googol CRD FIFO clear failed.";
            return ok;
        }

        public bool CommitMotionBuffer(short coordinateOrBuffer, bool waitForEnd, out string message)
        {
            if (!CrdData(coordinateOrBuffer))
            {
                message = "Googol CRD data push failed.";
                return false;
            }

            if (!CrdMoveStart(coordinateOrBuffer, waitForEnd))
            {
                message = "Googol CRD motion start failed.";
                return false;
            }

            message = "Googol CRD motion started.";
            return true;
        }

        public bool RunSynchronizedTrigger(
            SynchronizedTriggerRequest request,
            out SynchronizedTriggerResult result,
            out string message)
        {
            result = new SynchronizedTriggerResult();
            message = string.Empty;
            if (request == null)
            {
                message = "Synchronized trigger request cannot be null.";
                result.Message = message;
                return false;
            }

            var success = request.Mode switch
            {
                SynchronizedTriggerMode.BufferedIo => RunBufferedIo(request, out message),
                SynchronizedTriggerMode.PositionCompare => RunPositionCompare(request, out message),
                _ => FailUnsupportedTrigger(request.Mode, out message)
            };

            result.Success = success;
            result.PointCount = request.Points.Count;
            result.Message = message;
            return success;
        }

        private LineInterPoParam BuildLineParam(CoordinatedMotionRequest request)
        {
            if (request.LineParam != null)
            {
                return request.LineParam;
            }

            return new LineInterPoParam
            {
                InterPoAxiss = request.Axes.ToList(),
                TargetPosDic = new Dictionary<En_AxisNum, double>(request.TargetPositions),
                TargetPos = request.Axes.Select(axis => request.TargetPositions[axis]).ToArray(),
                DefaultSpeed = request.SpeedType,
                waitforend = request.WaitForEnd
            };
        }

        private CustomInterPoParam BuildCustomParam(CoordinatedMotionRequest request)
        {
            if (request.CustomParam != null)
            {
                return request.CustomParam;
            }

            return new CustomInterPoParam
            {
                InterPoAxiss = request.Axes.ToList(),
                TargetPosDic = new Dictionary<En_AxisNum, double>(request.TargetPositions),
                TargetPos = request.Axes.Select(axis => request.TargetPositions[axis]).ToArray(),
                DefaultSpeed = request.SpeedType
            };
        }

        private bool RunBufferedIo(SynchronizedTriggerRequest request, out string message)
        {
            if (request.DelayMilliseconds > 0 && !BufDelay(request.DelayMilliseconds))
            {
                message = "Googol buffered delay write failed.";
                return false;
            }

            if (!BufIO(request.DoMask, request.DoValue))
            {
                message = "Googol buffered IO write failed.";
                return false;
            }

            message = "Googol buffered IO write completed.";
            return true;
        }

        private bool RunPositionCompare(SynchronizedTriggerRequest request, out string message)
        {
            var param = request.PositionCompareParam ?? new PosComparisonOutputParam();
            var template = request.PositionCompareDataTemplate ?? new PosCompareData();

            foreach (var point in request.Points)
            {
                InsertPosCompareData(
                    new[] { point.X, point.Y },
                    new PosCompareData
                    {
                        Hso = template.Hso,
                        Gpo = template.Gpo,
                        SegmentNumber = template.SegmentNumber
                    });
            }

            if (!ControlPosComparison(true, param))
            {
                message = "Googol position compare start failed.";
                return false;
            }

            message = "Googol position compare started.";
            return true;
        }

        private static bool FailUnsupportedCoordinatedMotion(CoordinatedMotionKind kind, out string message)
        {
            message = $"Googol does not support synchronized motion kind: {kind}.";
            return false;
        }

        private static bool FailUnsupportedTrigger(SynchronizedTriggerMode mode, out string message)
        {
            message = $"Googol does not support synchronized trigger mode: {mode}.";
            return false;
        }
    }
}
