using System;
using SummerMemories.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SummerMemories.App
{
    /// <summary>标题画面：原创标题美术 + 新游戏 / 继续。</summary>
    public class TitleScreen
    {
        public static GameObject Build(Transform parent, bool hasSave,
            Action onNewGame, Action onContinue)
        {
            var canvas = UIFactory.CreateCanvas("Title", parent, sorting: 0);
            var root = canvas.transform;

            var bg = UIFactory.CreatePanel("Bg", root, Palette.DeepSea);
            var bgImg = bg.GetComponent<Image>();
            var art = ArtRegistry.LoadBg("title_bg");
            if (art != null)
            {
                bgImg.sprite = art;
                bgImg.color = Color.white;
                bgImg.preserveAspect = true;
            }
            // 底部渐变压暗，突出标题与按钮
            UIFactory.CreatePanel("Shade", root, new Color(0.03f, 0.05f, 0.10f, 0.55f),
                new Vector2(0, 0), new Vector2(1, 0.5f),
                Vector2.zero, Vector2.zero);

            var title = UIFactory.CreateText("Title", root, "夏日回忆", 110, Palette.Paper,
                TextAnchor.MiddleCenter, false);
            SetOffsets((RectTransform)title.transform, new Vector2(0, 120), new Vector2(0, 360));
            var sub = UIFactory.CreateText("Sub", root, "SUMMER MEMORIES  ·  归墟来潮", 34,
                Palette.Gold, TextAnchor.MiddleCenter, false);
            SetOffsets((RectTransform)sub.transform, new Vector2(0, 60), new Vector2(0, 120));

            UIFactory.CreateButtonCentered("NewGame", root, "新游戏", 38,
                Palette.Sea, Palette.White, () => onNewGame?.Invoke(),
                0, -120, 360, 90);
            var cont = UIFactory.CreateButtonCentered("Continue", root, "继续", 38,
                Palette.PanelSoft, Palette.Paper, () => onContinue?.Invoke(),
                0, -240, 360, 90);
            cont.interactable = hasSave;

            var note = UIFactory.CreateText("Note", root,
                "原创角色与素材 · 取材《山海经》与中国神话", 24,
                new Color(1, 1, 1, 0.7f), TextAnchor.LowerCenter, false);
            SetOffsets((RectTransform)note.transform, new Vector2(0, 30), new Vector2(0, 80));
            return canvas.gameObject;
        }

        private static void SetOffsets(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.offsetMin = min;
            rt.offsetMax = max;
        }
    }
}
