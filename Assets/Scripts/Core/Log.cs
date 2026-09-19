using UnityEngine;

namespace SummerMemories.Core
{
    public static class Log
    {
        private const string Tag = "[SummerMemories]";

        public static void Info(string msg) { Debug.Log($"{Tag} {msg}"); }
        public static void Warn(string msg) { Debug.LogWarning($"{Tag} {msg}"); }
        public static void Error(string msg) { Debug.LogError($"{Tag} {msg}"); }
    }
}
