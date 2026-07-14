using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Events
{

    public class EventSubscriptionInfo
    {
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string EventName { get; set; }
        public string Description { get; set; }
        public string ParameterType { get; set; }

        public bool IsUsing { get; set; }
}

    public static class EventSubscriptionRegistry
    {
        public static readonly List<EventSubscriptionInfo> _subscriptions = new();

        public static void Add(EventSubscriptionInfo info)
        {
            lock (_subscriptions)
            {
                _subscriptions.Add(info);
             }
        }

        public static IReadOnlyList<EventSubscriptionInfo> GetAll()
        {
            lock (_subscriptions)
            {
                return _subscriptions.ToList();
            }
        }

        public static void PrintAll()
        {
            foreach (var s in GetAll())
            {
                Console.WriteLine(
                    $"事件: {s.EventName,-25} | 方法: {s.MethodName,-20} | 类: {s.ClassName,-30} | 描述: {s.Description}");
            }
        }
    }

    public static class EventSubscriptionHelper
    {
        public static void AutoSubscribe(object target, IEventAggregator eventAggregator)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));

            var methods = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttributes(typeof(EventSubscriptionAttribute), false).Any());

            foreach (var method in methods)
            {
                foreach (var attr in method.GetCustomAttributes<EventSubscriptionAttribute>())
                {
                    var eventType = attr.EventType;

                    // 事件必须继承 EventBase
                    if (!typeof(EventBase).IsAssignableFrom(eventType))
                        throw new InvalidOperationException($"事件类型 {eventType.Name} 必须继承自 EventBase");

                    // 获取事件实例
                    var @event = eventAggregator.GetEvent(eventType);

                    // 方法参数检查
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1)
                        throw new InvalidOperationException($"方法 {method.Name} 必须只有一个参数");

                    var paramType = parameters[0].ParameterType;

                    // 生成 Action<T>
                    var actionType = typeof(Action<>).MakeGenericType(paramType);
                    var action = Delegate.CreateDelegate(actionType, target, method);

                    // 查找 Subscribe(Action<T>, ThreadOption) 重载
                    var subscribeMethod = eventType.GetMethods()
                        .Where(m => m.Name == "Subscribe")
                        .Where(m =>
                        {
                            var p = m.GetParameters();
                            return p.Length == 2 &&
                                   p[0].ParameterType.IsGenericType &&
                                   p[1].ParameterType == typeof(ThreadOption);
                        })
                        .FirstOrDefault();

                    if (subscribeMethod == null)
                        throw new MissingMethodException($"{eventType.Name} 未找到 Subscribe(Action<T>, ThreadOption)");

                    // 调用 Subscribe(action, threadOption)
                    subscribeMethod.Invoke(@event, new object[] { action, attr.ThreadOption });

                    // 记录注册信息
                    EventSubscriptionInfo eventSubscriptionInfo = new EventSubscriptionInfo
                    {
                        ClassName = target.GetType().FullName,
                        MethodName = method.Name,
                        EventName = eventType.Name,
                        Description = attr.Description,
                        ParameterType = paramType.Name
                    };

                    if (!InformationRepeated(eventSubscriptionInfo))
                    {
                        EventSubscriptionRegistry.Add(eventSubscriptionInfo);
                    }
                }
            }
        }

        private static EventBase GetEvent(this IEventAggregator eventAggregator, Type eventType)
        {
            var method = typeof(IEventAggregator)
                .GetMethod("GetEvent", Type.EmptyTypes)
                .MakeGenericMethod(eventType);

            return (EventBase)method.Invoke(eventAggregator, null);
        }

        /// <summary>
        /// 判断是否添加重复的注册信息
        /// </summary>
        /// <param name="eventSubscriptionInfo"></param>
        /// <returns></returns>
        private static bool InformationRepeated(EventSubscriptionInfo eventSubscriptionInfo)
        {
            return EventSubscriptionRegistry._subscriptions.Count(x => x.ClassName == eventSubscriptionInfo.ClassName && x.MethodName == eventSubscriptionInfo.MethodName &&
            x.EventName == eventSubscriptionInfo.EventName && x.Description == eventSubscriptionInfo.Description && x.ParameterType == eventSubscriptionInfo.ParameterType) >= 1 ? true : false;
        }
    }



    //public static class EventSubscriptionHelper
    //{
    //    public static void AutoSubscribe(object target, IEventAggregator eventAggregator)
    //    {
    //        if (target == null) throw new ArgumentNullException(nameof(target));
    //        if (eventAggregator == null) throw new ArgumentNullException(nameof(eventAggregator));

    //        var methods = target.GetType()
    //            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    //            .Where(m => m.GetCustomAttributes(typeof(EventSubscriptionAttribute), false).Any());

    //        foreach (var method in methods)
    //        {
    //            foreach (var attr in method.GetCustomAttributes<EventSubscriptionAttribute>())
    //            {
    //                var eventType = attr.EventType;

    //                // 检查是否是 PubSubEvent<T>
    //                if (!typeof(EventBase).IsAssignableFrom(eventType))
    //                    throw new InvalidOperationException($"事件类型 {eventType.Name} 必须继承自 Prism.Events.EventBase");

    //                // 订阅逻辑
    //                var @event = eventAggregator.GetEvent(eventType);

    //                // 获取方法参数
    //                var parameters = method.GetParameters();
    //                if (parameters.Length != 1)
    //                    throw new InvalidOperationException($"方法 {method.Name} 必须只有一个参数，对应事件类型的泛型参数");

    //                var paramType = parameters[0].ParameterType;

    //                // 构造 Subscribe(Action<T>)
    //                var subscribeMethod = eventType.GetMethod("Subscribe", new[] { typeof(Action<>).MakeGenericType(paramType) });
    //                var actionType = typeof(Action<>).MakeGenericType(paramType);

    //                var action = Delegate.CreateDelegate(actionType, target, method);


    //                subscribeMethod.Invoke(@event, new object[] { action });

    //                EventSubscriptionRegistry.Add(new EventSubscriptionInfo
    //                {
    //                    ClassName = target.GetType().FullName,
    //                    MethodName = method.Name,
    //                    EventName = eventType.Name,
    //                    Description = attr.Description,
    //                    ParameterType = paramType.Name
    //                });
    //            }
    //        }
    //    }

    //    private static EventBase GetEvent(this IEventAggregator eventAggregator, Type eventType)
    //    {
    //        var method = typeof(IEventAggregator).GetMethod("GetEvent", Type.EmptyTypes)
    //                                             .MakeGenericMethod(eventType);
    //        return (EventBase)method.Invoke(eventAggregator, null);
    //    }
    //}


}
