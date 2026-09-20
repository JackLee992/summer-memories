using System.Collections.Generic;
using UnityEngine;

namespace SummerMemories.Core.UI
{
    /// <summary>
    /// 原创美术资源加载（Resources/Art 下），缺失时返回 null 由视图回退到纯色占位。
    /// Sprite 按 key 缓存，避免刷新时反复创建。
    /// </summary>
    public static class ArtRegistry
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite LoadBg(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return Load($"Art/Bg/{key}");
        }

        public static Sprite LoadPortrait(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return Load($"Art/Portraits/{key}");
        }

        public static Sprite LoadIcon(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return Load($"Art/Icons/{key}");
        }

        private static Sprite Load(string resPath)
        {
            if (Cache.TryGetValue(resPath, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>(resPath);
            if (tex == null) return null;
            tex.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
            Cache[resPath] = sprite;
            return sprite;
        }
    }
}
