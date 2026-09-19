using System;
using System.Collections.Generic;

namespace SummerMemories.Core
{
    /// <summary>
    /// 轻量类型化事件总线。模块间解耦通信用，不跨场景持久化。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<string, List<Action<object>>> _handlers
            = new Dictionary<string, List<Action<object>>>();

        public static void Subscribe(string evt, Action<object> handler)
        {
            if (!_handlers.TryGetValue(evt, out var list))
            {
                list = new List<Action<object>>();
                _handlers[evt] = list;
            }
            if (!list.Contains(handler)) list.Add(handler);
        }

        public static void Unsubscribe(string evt, Action<object> handler)
        {
            if (_handlers.TryGetValue(evt, out var list)) list.Remove(handler);
        }

        public static void Publish(string evt, object payload = null)
        {
            if (!_handlers.TryGetValue(evt, out var list)) return;
            // 复制一份，避免回调中增删导致迭代异常
            var copy = list.ToArray();
            foreach (var h in copy)
            {
                try { h(payload); }
                catch (Exception e) { Log.Error($"EventBus handler error on '{evt}': {e}"); }
            }
        }

        public static void Clear() { _handlers.Clear(); }
    }
}
