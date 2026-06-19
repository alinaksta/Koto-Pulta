using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Services
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _services.Clear();
        }

        public static void Register<T>(T service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            Type type = typeof(T);

            if (_services.TryGetValue(type, out object existing) && !ReferenceEquals(existing, service))
            {
                throw new InvalidOperationException($"Service already registered for type {type.Name}");
            }

            _services[typeof(T)] = service;
        }

        public static T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            throw new Exception($"Service of type {typeof(T)} is not registered.");
        }

        public static bool TryGet<T>(out T service)
        {
            if (_services.TryGetValue(typeof(T), out object obj))
            {
                service = (T)obj;
                return true;
            }

            service = default;
            return false;
        }

        public static void Unregister<T>()
        {
            _services.Remove(typeof(T));
        }
    }
}