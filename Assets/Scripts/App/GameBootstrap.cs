using UnityEngine;

namespace SummerMemories.App
{
    /// <summary>
    /// 零场景配置启动：任何场景（含构建时临时新建的空场景）加载前自动创建
    /// GameDirector，因此工程无需提交 .unity 场景文件即可运行与打包。
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            var existing = Object.FindFirstObjectByType<GameDirector>();
            if (existing != null) return;
            var go = new GameObject("GameDirector");
            go.AddComponent<GameDirector>();
            Object.DontDestroyOnLoad(go);
            Core.Log.Info("GameDirector 已启动");
        }
    }
}
