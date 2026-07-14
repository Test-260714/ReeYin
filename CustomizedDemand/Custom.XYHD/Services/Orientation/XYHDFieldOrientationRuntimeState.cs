using Custom.XYHD.Models;

namespace Custom.XYHD.Services
{
    internal readonly struct XYHDFieldOrientationSnapshot
    {
        public bool IsConfigured { get; init; }
        public int OwnerSerial { get; init; }
        public bool SwapLeftRightPaths { get; init; }
        public bool LeftPathXMirror { get; init; }
        public bool RightPathXMirror { get; init; }
    }

    internal static class XYHDFieldOrientationRuntimeState
    {
        private static readonly object Sync = new();
        private static XYHDFieldOrientationSnapshot _snapshot;

        public static void Update(DetectionModel model)
        {
            if (model == null)
                return;

            lock (Sync)
            {
                _snapshot = new XYHDFieldOrientationSnapshot
                {
                    IsConfigured = true,
                    OwnerSerial = model.Serial,
                    SwapLeftRightPaths = model.SwapLeftRightPaths,
                    LeftPathXMirror = model.LeftPathXMirror,
                    RightPathXMirror = model.RightPathXMirror
                };
            }
        }

        public static XYHDFieldOrientationSnapshot GetSnapshot()
        {
            lock (Sync)
            {
                return _snapshot;
            }
        }
    }
}
