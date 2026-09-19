using System.IO;
using UnityEngine;

namespace SummerMemories.Core.Content
{
    /// <summary>
    /// 定位玩家外置导入的版权素材。任何素材缺失时返回空字符串，
    /// 上层（ADV/Battle 视图）回退到程序化占位表现，保证无素材也能完整游玩。
    /// </summary>
    public class ExternalContentLocator
    {
        public const string Folder = "ImportedContent";

        public string Root { get; }

        public ExternalContentLocator()
        {
            Root = Path.Combine(Application.persistentDataPath, Folder);
        }

        public bool EnsureRoot()
        {
            if (Directory.Exists(Root)) return true;
            try { Directory.CreateDirectory(Root); return true; }
            catch (System.Exception e) { Log.Error($"无法创建素材目录：{e.Message}"); return false; }
        }

        /// <summary>返回素材绝对路径；不存在返回空串。</summary>
        public string Resolve(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return "";
            var full = Path.Combine(Root, relativePath);
            return File.Exists(full) ? full : "";
        }

        public string ManifestPath => Path.Combine(Root, "manifest.json");

        public bool HasManifest => File.Exists(ManifestPath);
    }
}
