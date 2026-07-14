#nullable enable

using Prism.Events;
using ReeYin_V.Core.Services.DynamicView;
using System;
using System.Threading;

namespace ReeYin_V.Core.Events
{
    public class DynamicRegionViewFocusEvent : PubSubEvent<DynamicRegionViewFocusRequest>
    {
    }

    public sealed class DynamicRegionViewFocusRequest
    {
        private int _completionInvoked;

        public DynamicViewType Type { get; init; } = DynamicViewType.General;

        public int Serial { get; init; } = -1;

        public string ViewName { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public string Subjection { get; init; } = string.Empty;

        public Action<bool, string>? Completion { get; init; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ViewName);

        public bool IsCompleted => Volatile.Read(ref _completionInvoked) != 0;

        public void Complete(bool success, string message = "")
        {
            if (Interlocked.Exchange(ref _completionInvoked, 1) == 0)
            {
                Completion?.Invoke(success, message);
            }
        }
    }
}
