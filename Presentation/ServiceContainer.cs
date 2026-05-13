using System;
using System.Collections.Generic;

namespace AlJohary.ServiceHub.Presentation
{




    public static class ServiceContainer
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T implementation)
        {
            _services[typeof(T)] = implementation;
        }

        public static T GetService<T>()
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            throw new Exception($"Service {typeof(T).Name} not registered.");
        }
    }
}
