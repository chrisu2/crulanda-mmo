using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Core
{
    /// <summary>
    /// Minimal typed publish/subscribe bus for *cross-system* notifications (e.g. ActorDied).
    /// Use plain C# events for local, owner-to-listener relationships. Events should be small structs.
    /// A handler removed during a Publish may still receive that one in-flight event.
    /// </summary>
    public static class EventBus
    {
        static readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            Delegate existing;
            _handlers.TryGetValue(typeof(T), out existing);
            _handlers[typeof(T)] = Delegate.Combine(existing, handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            Delegate existing;
            if (!_handlers.TryGetValue(typeof(T), out existing)) return;
            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) _handlers.Remove(typeof(T));
            else _handlers[typeof(T)] = remaining;
        }

        public static void Publish<T>(T evt)
        {
            Delegate existing;
            if (!_handlers.TryGetValue(typeof(T), out existing)) return;

            // Snapshot so handlers may (un)subscribe while we iterate.
            var invocations = existing.GetInvocationList();
            for (int i = 0; i < invocations.Length; i++)
            {
                try
                {
                    ((Action<T>)invocations[i])(evt);
                }
                catch (Exception ex)
                {
                    CrulandaLog.Error(LogCategory.Core,
                        "EventBus handler for " + typeof(T).Name + " threw: " + ex);
                }
            }
        }

        public static int SubscriberCount<T>()
        {
            Delegate existing;
            return _handlers.TryGetValue(typeof(T), out existing) ? existing.GetInvocationList().Length : 0;
        }

        public static void Clear()
        {
            _handlers.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Clear();
        }
    }
}
