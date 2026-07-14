using Prism.Events;
using ReeYin_V.Core.Services.Alarm.Models;

namespace ReeYin_V.Core.Events.Alarm
{
    public sealed class AlarmRealtimeEvent : PubSubEvent<AlarmRealtimeEntry>
    {
    }
}
