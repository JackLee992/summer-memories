using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SummerMemories.Core.UI
{
    /// <summary>
    /// 程序化 uGUI 工厂：MVP 阶段所有界面用代码构建，不依赖 .prefab/.unity 资产，
    /// 保证空工程打开即可运行、命令行构建无需手工摆场景。
    /// 参考分辨率 1920x1080（横屏 ADV / 战棋）。
    /// </summary>
    public static class UIFactory
    {
        public const float RefWidth = 1920f;
        public const float RefHeight = 1080f;

        private static Font _font;
        private static Sprite _white;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    // Unity 6 内置 Legacy 运行时字体
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _font;
            }
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (_white == null)
                {
                    var tex = Texture2D.whiteTexture;
                    _white = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
                }
                return _white;
            }
        }

        public static Sprite SolidSprite(Color c)
        {
            var tex = new Texture2D(8, 8);
            var pixels = new Color32[64];
            var col = (Color32)c;
            for (var i = 0; i < pixels.Length; i++) pixels[i] = col;
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Canvas CreateCanvas(string name, Transform parent = null, int sorting = 0)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sorting;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.matchWidthOrHeight = 0.5f;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return canvas;
        }

        public static RectTransform Rect(GameObject go) { return (RectTransform)go.transform; }

        public static GameObject CreatePanel(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int border = 0)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = color;
            img.type = Image.Type.Sliced;
            img.fillCenter = border == 0;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        /// <summary>整块拉伸的色块面板。</summary>
        public static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            return CreatePanel(name, parent, color,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        public static Text CreateText(string name, Transform parent, string content, int size,
            Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool wrap = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.15f;
            t.raycastTarget = false;
            return t;
        }

        public static Button CreateButton(string name, Transform parent, string label, int size,
            Color bg, Color fg, Action onClick,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = bg;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var txt = CreateText("Label", go.transform, label, size, fg, TextAnchor.MiddleCenter, false);
            var trt = (RectTransform)txt.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 6f);
            trt.offsetMax = new Vector2(-12f, -6f);
            return btn;
        }

        /// <summary>以居中锚点定位的按钮（x,y 为中心，w,h 为尺寸）。</summary>
        public static Button CreateButtonCentered(string name, Transform parent, string label, int size,
            Color bg, Color fg, Action onClick, float x, float y, float w, float h)
        {
            return CreateButton(name, parent, label, size, bg, fg, onClick,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x - w / 2f, y - h / 2f), new Vector2(x + w / 2f, y + h / 2f));
        }

        public static void DestroyChildren(Transform t)
        {
            for (var i = t.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
            }
        }

        public static void SetAnchored(RectTransform rt, float x, float y)
        {
            rt.anchoredPosition = new Vector2(x, y);
        }
    }
}
