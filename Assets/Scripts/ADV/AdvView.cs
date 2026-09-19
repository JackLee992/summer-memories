using System;
using System.Collections.Generic;
using SummerMemories.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SummerMemories.ADV
{
    /// <summary>
    /// ADV 视图：全部用代码构建 uGUI。背景/立绘为纯色占位（显示素材 key），
    /// 未来从 ExternalContentLocator 加载外置版权素材替换。
    /// </summary>
    public class AdvView : MonoBehaviour
    {
        private AdvDirector _director;

        private Image _bg;
        private Text _note;
        private Image _leftPortrait;
        private Text _leftPortraitKey;
        private Image _rightPortrait;
        private Text _rightPortraitKey;
        private Text _name;
        private Text _body;
        private Text _continueMark;
        private GameObject _choicePanel;
        private readonly List<Button> _choiceButtons = new List<Button>();
        private GameObject _logPanel;
        private Text _logText;
        private Text _titleText;

        private Action _onFinished;
        private Action<string, string> _onLoopReset;
        private Action<string> _onTip;
        private bool _closed;

        public void Play(AdvScript script,
            Action onFinished,
            Action<string, string> onLoopReset,
            Action<string> onTip)
        {
            _onFinished = onFinished;
            _onLoopReset = onLoopReset;
            _onTip = onTip;
            BuildUi();
            _director = new AdvDirector();
            _director.OnTip += tip => _onTip?.Invoke(tip);
            _director.OnLoopReset += (anchor, tip) => _onLoopReset?.Invoke(anchor, tip);
            _director.OnFinished += HandleFinished;
            _director.Load(script);
            _titleText.text = script.title;
            Refresh();
        }

        private void Update()
        {
            if (_closed || _director == null) return;
            _director.Tick(Time.deltaTime);
            if (_body != null && _director.Phase == AdvPhase.Playing)
            {
                _body.text = _director.VisibleText;
                if (_continueMark != null)
                    _continueMark.enabled = _director.IsLineFullyShown && _director.Current != null
                        && (_director.Current.type == "line" || _director.Current.type == "narration");
            }
        }

        private void BuildUi()
        {
            var canvas = UIFactory.CreateCanvas("ADV", transform, sorting: 10);
            var root = canvas.transform;

            // 背景
            var bgGo = UIFactory.CreatePanel("Background", root, Palette.DeepSea);
            _bg = bgGo.GetComponent<Image>();
            _note = UIFactory.CreateText("SceneNote", root, "", 30, Palette.Paper, TextAnchor.UpperLeft);
            SetOffsets((RectTransform)_note.transform, new Vector2(60, -70), new Vector2(-60, -120));

            _titleText = UIFactory.CreateText("ChapterTitle", root, "", 40, Palette.Gold, TextAnchor.UpperCenter);
            SetOffsets((RectTransform)_titleText.transform, new Vector2(0, -40), new Vector2(0, -110));

            // 立绘位（左右）
            CreatePortraitSlot("LeftPortrait", root, true, out _leftPortrait, out _leftPortraitKey);
            CreatePortraitSlot("RightPortrait", root, false, out _rightPortrait, out _rightPortraitKey);

            // 顶部按钮：日志 / 跳过
            UIFactory.CreateButton("LogBtn", root, "日志", 28, Palette.PanelSoft, Palette.Paper,
                ToggleLog, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-360, -30), new Vector2(-220, -90));
            UIFactory.CreateButton("SkipBtn", root, "跳过", 28, Palette.PanelSoft, Palette.Paper,
                () => _director.Skip(), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-200, -30), new Vector2(-60, -90));

            // 底部对白框
            var box = UIFactory.CreatePanel("DialogueBox", root, new Color(0.08f, 0.10f, 0.16f, 0.92f),
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(60, 40), new Vector2(-60, 380));
            var nameBox = UIFactory.CreatePanel("NameBox", box.transform, Palette.Sea,
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, -30), new Vector2(360, 40));
            _name = UIFactory.CreateText("Name", nameBox.transform, "", 34, Palette.White, TextAnchor.MiddleCenter);
            Stretch((RectTransform)_name.transform);

            _body = UIFactory.CreateText("Body", box.transform, "", 36, Palette.Paper, TextAnchor.UpperLeft);
            SetOffsets((RectTransform)_body.transform, new Vector2(40, -70), new Vector2(-40, -30));

            _continueMark = UIFactory.CreateText("Mark", box.transform, "▼", 30, Palette.Gold,
                TextAnchor.LowerRight);
            SetOffsets((RectTransform)_continueMark.transform, new Vector2(-90, 6), new Vector2(-20, 46));
            _continueMark.enabled = false;

            // 全屏点击推进区域（在对白框与背景之上、按钮之下）
            var clickGo = new GameObject("ClickCatcher", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            clickGo.transform.SetParent(root, false);
            var clickImg = clickGo.GetComponent<Image>();
            clickImg.color = new Color(0, 0, 0, 0);
            clickImg.raycastTarget = true;
            Stretch((RectTransform)clickGo.transform);
            clickGo.GetComponent<Button>().onClick.AddListener(() => _director.Press());
            clickGo.transform.SetAsLastSibling();
            // 按钮重新置顶
            _choicePanel = new GameObject("ChoicePanel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(VerticalLayoutGroup));
            _choicePanel.transform.SetParent(root, false);
            Stretch((RectTransform)_choicePanel.transform);
            var vlg = _choicePanel.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 24;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // 日志面板（默认隐藏）
            BuildLogPanel(root);
        }

        private void CreatePortraitSlot(string n, Transform root, bool left,
            out Image img, out Text keyText)
        {
            var go = UIFactory.CreatePanel(n, root, new Color(0.20f, 0.24f, 0.34f, 0.55f),
                left ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f),
                left ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f),
                left ? new Vector2(120, -260) : new Vector2(-620, -260),
                left ? new Vector2(620, 260) : new Vector2(-120, 260));
            img = go.GetComponent<Image>();
            keyText = UIFactory.CreateText("Key", go.transform, "", 26,
                new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter);
            Stretch((RectTransform)keyText.transform);
        }

        private void BuildLogPanel(Transform root)
        {
            _logPanel = UIFactory.CreatePanel("LogPanel", root, new Color(0.05f, 0.06f, 0.10f, 0.97f));
            _logText = UIFactory.CreateText("Log", _logPanel.transform, "", 30, Palette.Paper,
                TextAnchor.UpperLeft);
            SetOffsets((RectTransform)_logText.transform, new Vector2(80, -120), new Vector2(-80, -160));
            UIFactory.CreateButtonCentered("CloseLog", _logPanel.transform, "关闭", 30,
                Palette.Sea, Palette.White, () => _logPanel.SetActive(false),
                0, -460, 260, 80);
            _logPanel.SetActive(false);
        }

        private void ToggleLog()
        {
            var show = !_logPanel.activeSelf;
            _logPanel.SetActive(show);
            if (show)
            {
                _logText.text = string.Join("\n\n", _director.History);
            }
        }

        private void Refresh()
        {
            // 背景：优先原创美术图，缺失时回退纯色
            var bgSprite = ArtRegistry.LoadBg(_director.BgArt);
            if (bgSprite != null)
            {
                _bg.type = Image.Type.Simple;
                _bg.sprite = bgSprite;
                _bg.color = Color.white;
                _bg.preserveAspect = true;
            }
            else
            {
                _bg.type = Image.Type.Simple;
                _bg.sprite = UIFactory.WhiteSprite;
                _bg.color = Palette.Hex(_director.BgColor);
            }
            _note.text = _director.BgNote;
            UpdatePortrait(_leftPortrait, _leftPortraitKey, _director.LeftPortrait);
            UpdatePortrait(_rightPortrait, _rightPortraitKey, _director.RightPortrait);

            // 选项
            foreach (var b in _choiceButtons) Destroy(b.gameObject);
            _choiceButtons.Clear();

            if (_director.Phase == AdvPhase.Choice && _director.Current != null
                && _director.Current.options != null)
            {
                _choicePanel.SetActive(true);
                var le = _choicePanel.AddComponent<LayoutElement>();
                le.minHeight = 80;
                for (var i = 0; i < _director.Current.options.Count; i++)
                {
                    var idx = i;
                    var opt = _director.Current.options[i];
                    var btn = UIFactory.CreateButton($"Choice{i}", _choicePanel.transform, opt.text,
                        34, Palette.PanelSoft, Palette.Paper, () => _director.Choose(idx),
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(-360, 0), new Vector2(360, 0));
                    var layout = btn.gameObject.AddComponent<LayoutElement>();
                    layout.minHeight = 90;
                    layout.preferredWidth = 720;
                    _choiceButtons.Add(btn);
                }
            }
            else
            {
                _choicePanel.SetActive(false);
            }

            // 文本
            if (_director.Phase == AdvPhase.Playing && _director.Current != null)
            {
                var c = _director.Current;
                _name.text = c.type == "narration" ? "" : (c.speaker ?? "");
                _name.transform.parent.gameObject.SetActive(c.type == "line");
                _body.text = _director.VisibleText;
            }
            else if (_director.Phase == AdvPhase.Choice)
            {
                _body.text = "";
                _name.transform.parent.gameObject.SetActive(false);
            }
        }

        private void UpdatePortrait(Image img, Text keyText, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                img.gameObject.SetActive(false);
                return;
            }
            img.gameObject.SetActive(true);
            var sprite = ArtRegistry.LoadPortrait(key);
            if (sprite != null)
            {
                img.type = Image.Type.Simple;
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = true;
                keyText.gameObject.SetActive(false);
            }
            else
            {
                img.type = Image.Type.Simple;
                img.sprite = UIFactory.WhiteSprite;
                img.color = new Color(0.20f, 0.24f, 0.34f, 0.55f);
                keyText.gameObject.SetActive(true);
                keyText.text = $"［立绘占位］\n{key}";
            }
        }

        private void HandleFinished()
        {
            // loopreset 已通过事件回调处理；普通 end 走 onFinished
            Refresh();
            if (_director != null && _director.Phase == AdvPhase.Finished)
            {
                // 由回调类型决定：若触发过 loopreset，GameDirector 会切换流程
                _onFinished?.Invoke();
            }
        }

        private void LateUpdate()
        {
            // 选项出现/消失后刷新一次
            if (_director != null && ((_director.Phase == AdvPhase.Choice) != _choicePanel.activeSelf))
                Refresh();
        }

        public void Close()
        {
            _closed = true;
            if (gameObject != null) Destroy(gameObject);
        }

        // —— RectTransform 辅助 ——
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetOffsets(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.offsetMin = min;
            rt.offsetMax = max;
        }
    }
}
