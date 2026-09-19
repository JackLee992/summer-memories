using UnityEngine;

namespace SummerMemories.Core.UI
{
    /// <summary>
    /// 原创美术资源加载（Resources/Art 下），缺失时返回 null 由视图回退到纯色占位。
    /// 版权素材外置模式下，玩家导入的同名资源优先于内置占位（见 ExternalContentLocator）。
    /// </summary>
    public static class ArtRegistry
    {
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
            var tex = Resources.Load<Texture2D>(resPath);
            if (tex == null) return null;
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
