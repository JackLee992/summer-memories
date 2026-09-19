using System.IO;
using SummerMemories.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SummerMemories.Editor
{
    /// <summary>
    /// 一键构建。运行时通过 [RuntimeInitializeOnLoadMethod] 自举，
    /// 因此构建时动态生成一个空 Boot 场景即可，仓库无需手工维护 .unity 文件。
    /// 命令行：
    ///   Unity -batchmode -quit -projectPath . \
    ///     -executeMethod SummerMemories.Editor.BuildScript.BuildAndroid \
    ///     -logFile -
    /// </summary>
    public static class BuildScript
    {
        private const string BundleId = "com.summermemories.study";
        private const string ProductName = "SummerMemories";

        [MenuItem("夏日回忆/Build/Android APK (ARM64)")]
        public static void BuildAndroid()
        {
            Log.Info("开始构建 Android APK …");
            EnsurePlayerSettings();

            var scenePath = CreateBootScene();
            try
            {
                var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
                Directory.CreateDirectory(outDir);
                var apkPath = Path.Combine(outDir, "summer-memories-v0.1-android.apk");

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { scenePath },
                    locationPathName = apkPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.Development | BuildOptions.AllowDebugging
                };

                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                Log.Info($"构建结果：{summary.result}，大小 {summary.totalSize / 1024 / 1024} MB，输出 {apkPath}");
                if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                {
                    throw new System.Exception($"Android 构建失败：{summary.result}");
                }
            }
            finally
            {
                RemoveBootScene(scenePath);
            }
        }

        [MenuItem("夏日回忆/Build/在编辑器中生成 Boot 场景（用于按 Play 调试）")]
        public static void GenerateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var path = "Assets/Scenes/Boot.unity";
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            Log.Info($"已生成 {path}，直接按 Play 即可（GameDirector 会自动启动）。");
        }

        private static void EnsurePlayerSettings()
        {
            PlayerSettings.companyName = "SummerMemoriesProject";
            PlayerSettings.productName = ProductName;
            PlayerSettings.applicationIdentifier = BundleId;

            // 横屏（ADV / 战棋）
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

#if UNITY_ANDROID
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // Android 8.0
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.bundleVersion = "0.1.0";
            // 使用 Unity 默认 debug keystore 签名（自用包）
            PlayerSettings.Android.useCustomKeystore = false;
#endif
        }

        private static string CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var path = "Temp/Boot_build.unity";
            Directory.CreateDirectory("Temp");
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void RemoveBootScene(string scenePath)
        {
            try
            {
                if (File.Exists(scenePath)) File.Delete(scenePath);
                var meta = scenePath + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
            }
            catch { /* 临时文件清理失败不影响结果 */ }
        }
    }
}
