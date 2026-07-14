using ReeYin_V.Hardware.ControlCard.Models;

namespace ReeYin_V.Hardware.ControlCard
{
    public interface ICoordinatedMotionCard
    {
        bool SupportsCoordinatedMotion { get; }
        bool MoveCoordinated(CoordinatedMotionRequest request, out string message);
    }

    public interface IBufferedMotionCard
    {
        bool SupportsBufferedMotion { get; }
        bool ClearMotionBuffer(short coordinateOrBuffer, out string message);
        bool CommitMotionBuffer(short coordinateOrBuffer, bool waitForEnd, out string message);
    }

    public interface ISynchronizedTriggerCard
    {
        SynchronizedTriggerCapabilities TriggerCapabilities { get; }
        bool RunSynchronizedTrigger(
            SynchronizedTriggerRequest request,
            out SynchronizedTriggerResult result,
            out string message);
    }
}
