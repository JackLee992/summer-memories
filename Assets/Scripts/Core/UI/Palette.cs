using UnityEngine;

namespace SummerMemories.Core.UI
{
    /// <summary>
    /// 占位配色与程序化贴图。MVP 阶段不依赖任何美术资产，全部用纯色块；
    /// 正式版权素材通过 ContentManifest 外置导入后替换。
    /// </summary>
    public static class Palette
    {
        // 夜晚 / 海面 / 回忆蓝调
        public static readonly Color Night = Hex("#101626");
        public static readonly Color DeepSea = Hex("#16324a");
        public static readonly Color Sea = Hex("#2a6f8f");
        public static readonly Color Dusk = Hex("#c96f4a");
        public static readonly Color Paper = Hex("#f2e9d8");
        public static readonly Color Ink = Hex("#23201d");
        public static readonly Color White = Color.white;
        public static readonly Color Shadow = Hex("#3a2f3f");     // 影子（敌人）紫黑
        public static readonly Color ShadowAccent = Hex("#8a6db0");
        public static readonly Color Ally = Hex("#5aa7a7");
        public static readonly Color Danger = Hex("#c44b4b");
        public static readonly Color Gold = Hex("#d9a441");
        public static readonly Color Dim = Hex("#9a938a");
        public static readonly Color Panel = Hex("#1c2434");
        public static readonly Color PanelSoft = Hex("#28344a");

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta;
        }
    }
}
