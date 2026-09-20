using UnityEngine;
using UnityEngine.EventSystems;

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
            EnsureEventSystem();

            var args = System.Environment.GetCommandLineArgs();
            var mode = "legacy";
#if SM_ACTION3D
            mode = "action3d";
#endif
#if SM_SQUAD || UNITY_EDITOR
            mode = "squad";
#endif
            foreach (var arg in args)
            {
                if (arg == "--squad" || arg == "--squad-smoke") mode = "squad";
                if (arg == "--action3d") mode = "action3d";
                if (arg == "--legacy") mode = "legacy";
            }
            if (Object.FindFirstObjectByType<GameDirector>() != null ||
                Object.FindFirstObjectByType<SquadDemoDirector>() != null ||
                Object.FindFirstObjectByType<Action3D.Game3DDirector>() != null) return;
            var go = new GameObject(mode == "squad" ? "SquadDemo" : mode == "action3d" ? "Action3D" : "GameDirector");
            Object.DontDestroyOnLoad(go);
            if (mode == "squad") go.AddComponent<SquadDemoDirector>();
            else if (mode == "action3d") go.AddComponent<Action3D.Game3DDirector>();
            else go.AddComponent<GameDirector>();
            Core.Log.Info("GameBootstrap mode: " + mode);
        }

        /// <summary>
        /// UGUI 的按钮 / 触摸（标题、ADV 推进、战棋格子、对话框）依赖场景中的
        /// EventSystem + 输入模块。零场景启动下没有预置 EventSystem，
        /// 缺失会导致真机上所有点击无响应，因此启动时确保存在一个。
        /// 项目 activeInputHandler=0（旧 Input Manager），使用 StandaloneInputModule。
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(es);
            Core.Log.Info("EventSystem 已创建");
        }
    }
}
