using Prism.Events;
using System;

namespace ReeYin_V.Core.Events.Hardware
{
    public class ControlCardResetOverlayEvent : PubSubEvent<ControlCardResetOverlayPayload>
    {
    }

    public class ControlCardResetOverlayPayload
    {
        public bool IsRunning { get; set; }

        public Guid OperationId { get; set; }

        public int TimeoutSeconds { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
