using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CarPartsShopWPF.Shared.Helpers
{
    public static class TypedMessenger
    {
        private static readonly ConcurrentDictionary<Type, List<Action<object>>> _subscribers = new ConcurrentDictionary<Type, List<Action<object>>>();

        public static void Subscribe<T>(Action<T> action)
        {
            var type = typeof(T);
            var list = _subscribers.GetOrAdd(type, _ => new List<Action<object>>());
            lock (list)
            {
                list.Add(o => action((T)o));
            }
        }

        public static void Send<T>(T message)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list))
            {
                Action<object>[] handlers;
                lock (list)
                {
                    handlers = list.ToArray();
                }
                foreach (var handler in handlers)
                {
                    handler(message);
                }
            }
        }
    }
}
