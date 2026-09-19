using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace SummerMemories.Core.Content
{
    /// <summary>
    /// 版权素材外置清单（ROM-free 合规模型，参考 pal-mobile / harkinianpad）：
    /// 仓库与安装包只包含原创代码与占位素材；动画美术、音乐、配音、原文等
    /// 版权内容由玩家自行导入到 persistentDataPath/ImportedContent/，
    /// 并通过 SHA-256 清单校验，校验通过才加载。
    /// </summary>
    [Serializable]
    public class ContentManifest
    {
        public List<ContentEntry> entries = new List<ContentEntry>();
    }

    [Serializable]
    public class ContentEntry
    {
        public string path;   // 相对 ImportedContent 根目录，如 bg/jul22_port.png
        public string sha256;
        public string kind;   // bg / portrait / bgm / voice / font
    }

    public static class ContentManifestBuilder
    {
        /// <summary>扫描目录生成清单（tools/content_manifest.py 的运行时等价物）。</summary>
        public static ContentManifest Build(string root)
        {
            var manifest = new ContentManifest();
            if (!Directory.Exists(root)) return manifest;
            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                if (Path.GetFileName(f) == "manifest.json") continue;
                var rel = f.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar)
                    .Replace('\\', '/');
                manifest.entries.Add(new ContentEntry
                {
                    path = rel,
                    sha256 = Sha256Of(f),
                    kind = GuessKind(rel)
                });
            }
            return manifest;
        }

        public static string Sha256Of(string file)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(file);
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string GuessKind(string rel)
        {
            var ext = Path.GetExtension(rel).ToLowerInvariant();
            switch (ext)
            {
                case ".png": case ".jpg": case ".jpeg":
                    return rel.Contains("portrait") ? "portrait" : "bg";
                case ".wav": case ".ogg": case ".mp3":
                    return rel.Contains("voice") ? "voice" : "bgm";
                case ".ttf": case ".otf": return "font";
                default: return "data";
            }
        }
    }
}
