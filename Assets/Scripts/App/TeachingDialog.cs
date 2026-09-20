using System;
using SummerMemories.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SummerMemories.App
{
    /// <summary>模态教学/提示对话框（回潮说明、TIPS 解锁等）。</summary>
    public class TeachingDialog
    {
        public static void Show(Transform parent, string title, string body,
            string buttonText, Action onClose)
        {
            // 必须自带 Canvas：调用方通常已 ClearScreens()，parent 下不再有任何 Canvas，
            // 否则 Image/Text/Button 不会渲染也无法点击。
            var canvas = UIFactory.CreateCanvas("TeachingDialog", parent, sorting: 20);
            var overlay = UIFactory.CreatePanel("Overlay", canvas.transform,
                new Color(0.02f, 0.03f, 0.06f, 0.92f));
            var box = UIFactory.CreatePanel("Box", overlay.transform, Palette.Panel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-560, -300), new Vector2(560, 300));
            var t = UIFactory.CreateText("Title", box.transform, title, 46, Palette.Gold,
                TextAnchor.UpperCenter);
            SetOffsets((RectTransform)t.transform, new Vector2(30, -36), new Vector2(-30, -120));
            var b = UIFactory.CreateText("Body", box.transform, body, 30, Palette.Paper,
                TextAnchor.UpperLeft);
            SetOffsets((RectTransform)b.transform, new Vector2(50, -150), new Vector2(-50, -120));
            UIFactory.CreateButtonCentered("Ok", box.transform, buttonText, 32,
                Palette.Sea, Palette.White, () =>
                {
                    UnityEngine.Object.Destroy(canvas.gameObject);
                    onClose?.Invoke();
                }, 0, -230, 300, 80);
        }

        private static void SetOffsets(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.offsetMin = min;
            rt.offsetMax = max;
        }
    }
}
