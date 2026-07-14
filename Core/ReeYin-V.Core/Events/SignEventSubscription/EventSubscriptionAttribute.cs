using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Events
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class EventSubscriptionAttribute : Attribute
    {
        public Type EventType { get; }
        public string Description { get; set; }
        public ThreadOption ThreadOption { get; }

        public EventSubscriptionAttribute(Type eventType, string description = "", ThreadOption threadOption = ThreadOption.PublisherThread)
        {
            EventType = eventType;
            Description = description;
            ThreadOption = threadOption;
        }
    }
}
