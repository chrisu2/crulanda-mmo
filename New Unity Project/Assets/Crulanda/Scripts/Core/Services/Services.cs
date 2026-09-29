using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Core
{
    /// <summary>
    /// Deliberately tiny service locator for a handful of long-lived services (content registry,
    /// save store, later: sim world). It is NOT a place to hang gameplay state. Prefer explicit
    /// references (serialized fields / constructor arguments) everywhere else.
    /// </summary>
    public static class Services
    {
        static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException("instance");
            if (_services.ContainsKey(typeof(T)))
                throw new InvalidOperationException("Service already registered: " + typeof(T).Name);
            _services[typeof(T)] = instance;
        }

        public static void Replace<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException("instance");
            _services[typeof(T)] = instance;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            object obj;
            if (_services.TryGetValue(typeof(T), out obj))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }

        public static T Get<T>() where T : class
        {
            T service;
            if (!TryGet(out service))
                throw new InvalidOperationException(
                    "Service not registered: " + typeof(T).Name + ". Is GameBootstrap present in the scene?");
            return service;
        }

        /// <summary>Removes the registration only if it still points at <paramref name="instance"/>.</summary>
        public static void Unregister<T>(T instance) where T : class
        {
            object current;
            if (_services.TryGetValue(typeof(T), out current) && ReferenceEquals(current, instance))
                _services.Remove(typeof(T));
        }

        public static void Clear()
        {
            _services.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Clear();
        }
    }
}
