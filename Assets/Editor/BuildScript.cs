using System.IO;
using SummerMemories.Core;
using UnityEditor;
using UnityEditor.Build;
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
            ConfigureAndroidTools();
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

        /// <summary>
        /// 自动探测并指定 Android SDK / NDK / JDK。
        /// 优先使用 Hub 布局（PlaybackEngines/AndroidPlayer 下的 SDK、NDK、OpenJDK），
        /// 其次使用环境变量 ANDROID_SDK_ROOT / ANDROID_NDK_ROOT / JAVA_HOME。
        /// 用反射调用 AndroidExternalToolsSettings，使本文件在未安装 Android 模块时也能编译。
        /// </summary>
        private static void ConfigureAndroidTools()
        {
            var contents = EditorApplication.applicationContentsPath;
            var androidPlayer = Path.Combine(contents, "PlaybackEngines", "AndroidPlayer");

            var sdk = FirstExisting(
                Path.Combine(androidPlayer, "SDK"),
                System.Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
                System.Environment.GetEnvironmentVariable("ANDROID_HOME"));
            // Hub 布局下 NDK 内容直接展开在 NDK/ 下（含 ndk-build）；
            // 若多一层版本号目录（如 NDK/27.2.12479018），自动下钻。
            var ndk = FirstExisting(
                Path.Combine(androidPlayer, "NDK"),
                System.Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT"));
            if (ndk != null && !File.Exists(Path.Combine(ndk, "ndk-build")))
            {
                var nested = Directory.GetDirectories(ndk);
                foreach (var d in nested)
                {
                    if (File.Exists(Path.Combine(d, "ndk-build"))
                        && File.Exists(Path.Combine(d, "source.properties")))
                    { ndk = d; break; }
                }
            }
            // macOS 的 Hub OpenJDK 是 .jdk bundle 结构，Home 在 Contents/Home
            var jdk = FirstExisting(
                Path.Combine(androidPlayer, "OpenJDK", "Contents", "Home"),
                Path.Combine(androidPlayer, "OpenJDK"),
                System.Environment.GetEnvironmentVariable("JAVA_HOME"));

            SetExternalTool("sdkRootPath", sdk);
            SetExternalTool("ndkRootPath", ndk);
            SetExternalTool("jdkRootPath", jdk);
        }

        private static string FirstExisting(params string[] candidates)
        {
            foreach (var c in candidates)
            {
                if (!string.IsNullOrEmpty(c) && Directory.Exists(c)) return c;
            }
            return null;
        }

        private static void SetExternalTool(string property, string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var t = System.Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions");
            if (t == null)
            {
                Log.Warn($"找不到 AndroidExternalToolsSettings，跳过 {property} 设置（将使用 Unity 内置路径）。");
                return;
            }
            var prop = t.GetProperty(property);
            if (prop != null)
            {
                try
                {
                    prop.SetValue(null, path, null);
                    Log.Info($"Android 工具 {property} = {path}");
                }
                catch (System.Exception e)
                {
                    // 典型：NDK/SDK 版本与该 Unity 版本要求不符。不中止构建，
                    // 交给 Unity 自身的工具校验/自动下载处理（错误会进构建日志）。
                    Log.Warn($"设置 {property}='{path}' 失败（{e.GetType().Name}: {e.Message}）；将交由 Unity 默认工具链处理。");
                }
            }
        }

        private static void EnsurePlayerSettings()
        {
            PlayerSettings.companyName = "SummerMemoriesProject";
            PlayerSettings.productName = ProductName;

            // 横屏（ADV / 战棋）
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

#if UNITY_ANDROID
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
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
