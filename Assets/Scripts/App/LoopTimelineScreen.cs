using System.Collections.Generic;
using SummerMemories.Core.UI;
using SummerMemories.Loop;
using UnityEngine;
using UnityEngine.UI;

namespace SummerMemories.App
{
    /// <summary>周目/时间线界面：显示回潮次数、已继承的 TIPS 情报与战斗结果。</summary>
    public class LoopTimelineScreen
    {
        public static GameObject Build(Transform parent, int loopCount,
            TipsSystem tips, bool battleCleared,
            System.Action onBackToTitle, System.Action onRetryBattle)
        {
            var canvas = UIFactory.CreateCanvas("Timeline", parent, sorting: 12);
            var root = canvas.transform;
            UIFactory.CreatePanel("Bg", root, Palette.Night);

            var title = UIFactory.CreateText("Title", root,
                battleCleared ? "序章 · 归墟来潮（完）" : "周目时间线", 56,
                Palette.Gold, TextAnchor.MiddleCenter, false);
            SetOffsets((RectTransform)title.transform, new Vector2(0, 360), new Vector2(0, 470));

            var loop = UIFactory.CreateText("Loop", root, $"回潮次数：{loopCount}", 38,
                Palette.Paper, TextAnchor.MiddleCenter, false);
            SetOffsets((RectTransform)loop.transform, new Vector2(0, 250), new Vector2(0, 320));

            // TIPS 列表
            var panel = UIFactory.CreatePanel("TipsPanel", root, Palette.Panel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-520, -120), new Vector2(520, 180));
            var head = UIFactory.CreateText("Head", panel.transform, "◆ 跨回潮继承的《山海异闻》", 32,
                Palette.Sea, TextAnchor.UpperLeft);
            SetOffsets((RectTransform)head.transform, new Vector2(30, -24), new Vector2(-30, -80));

            var lines = new List<string>();
            foreach (var id in tips.UnlockedIds)
            {
                var e = tips.GetEntry(id);
                lines.Add(e != null ? $"· {e.title} —— {e.text}" : $"· {id}");
            }
            if (lines.Count == 0) lines.Add("（尚无情报）");
            var body = UIFactory.CreateText("Body", panel.transform,
                string.Join("\n", lines), 28, Palette.Paper, TextAnchor.UpperLeft);
            SetOffsets((RectTransform)body.transform, new Vector2(30, -100), new Vector2(-30, -30));

            if (battleCleared)
            {
                UIFactory.CreateButtonCentered("Back", root, "回到标题", 34,
                    Palette.Sea, Palette.White, () => onBackToTitle?.Invoke(),
                    -180, -340, 320, 84);
                var mvp = UIFactory.CreateText("MVP", root,
                    "MVP 内容到此为止：序章 ADV · 回潮教学 · 第一场策略战斗已完成。", 26,
                    Palette.Dim, TextAnchor.MiddleCenter, false);
                SetOffsets((RectTransform)mvp.transform, new Vector2(0, -430), new Vector2(0, -380));
            }
            else
            {
                UIFactory.CreateButtonCentered("Retry", root, "带着情报再次挑战", 34,
                    Palette.Danger, Palette.White, () => onRetryBattle?.Invoke(),
                    -180, -340, 360, 84);
                UIFactory.CreateButtonCentered("Back", root, "回到标题", 34,
                    Palette.PanelSoft, Palette.Paper, () => onBackToTitle?.Invoke(),
                    260, -340, 280, 84);
            }
            return canvas.gameObject;
        }

        private static void SetOffsets(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.offsetMin = min;
            rt.offsetMax = max;
        }
    }
}
