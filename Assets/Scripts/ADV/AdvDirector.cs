using System;
using System.Collections.Generic;
using System.Text;

namespace SummerMemories.ADV
{
    public enum AdvPhase { Playing, Choice, Finished }

    /// <summary>
    /// ADV 播放逻辑（无 MonoBehaviour，可单测）：
    /// 逐字打字机、点击推进、选项分支、历史回顾、跳过（快进到下一选项/结尾）、
    /// tip 解锁与 loopreset 回调。瞬时指令（bg/portrait/bgm/sfx）在推进时自动应用。
    /// </summary>
    public class AdvDirector
    {
        private const float CharsPerSecond = 38f;

        private AdvScript _script;
        private int _index;
        private float _charsShown;
        private readonly List<string> _history = new List<string>();

        public AdvPhase Phase { get; private set; } = AdvPhase.Playing;
        public AdvCommand Current { get; private set; }
        public IReadOnlyList<string> History => _history;

        /// <summary>当前瞬时演出状态（视图层据此渲染背景/立绘）。</summary>
        public string BgColor { get; private set; } = "#16324a";
        public string BgArt { get; private set; } = "";
        public string BgNote { get; private set; } = "";
        public string LeftPortrait { get; private set; } = "";
        public string RightPortrait { get; private set; } = "";

        public event Action<AdvCommand> OnStage;          // 瞬时指令应用
        public event Action<string> OnTip;                // 解锁 TIPS
        public event Action<string, string> OnLoopReset;  // (anchor, tip)
        public event Action OnFinished;

        public void Load(AdvScript script)
        {
            _script = script;
            _index = -1;
            _history.Clear();
            Phase = AdvPhase.Playing;
            Current = null;
            BgColor = "#16324a"; BgArt = ""; BgNote = ""; LeftPortrait = ""; RightPortrait = "";
            AdvanceToContent();
        }

        public string ScriptId => _script != null ? _script.id : "";
        public string Title => _script != null ? _script.title : "";
        public int CommandIndex => _index;

        public bool IsLineFullyShown
        {
            get
            {
                if (Current == null) return true;
                var len = Current.text == null ? 0 : Current.text.Length;
                return _charsShown >= len;
            }
        }

        public string VisibleText
        {
            get
            {
                if (Current == null || string.IsNullOrEmpty(Current.text)) return "";
                var n = (int)Math.Floor(_charsShown);
                if (n >= Current.text.Length) return Current.text;
                return Current.text.Substring(0, n);
            }
        }

        public void Tick(float dt)
        {
            if (Phase != AdvPhase.Playing || Current == null) return;
            if (Current.type == "line" || Current.type == "narration")
            {
                if (!IsLineFullyShown) _charsShown += dt * CharsPerSecond;
            }
        }

        /// <summary>玩家点击：打字中则补全；否则前进。</summary>
        public void Press()
        {
            if (Phase != AdvPhase.Playing) return;
            if (Current != null && (Current.type == "line" || Current.type == "narration"))
            {
                if (!IsLineFullyShown) { _charsShown = Current.text.Length; return; }
            }
            AdvanceToContent();
        }

        public void Choose(int optionIndex)
        {
            if (Phase != AdvPhase.Choice || Current == null || Current.options == null) return;
            if (optionIndex < 0 || optionIndex >= Current.options.Count) return;
            var opt = Current.options[optionIndex];
            if (!string.IsNullOrEmpty(opt.setFlag)) OnStage?.Invoke(new AdvCommand
            {
                type = "flag",
                note = opt.setFlag
            });
            _index = opt.jumpIndex - 1; // AdvanceToContent 会 +1
            Phase = AdvPhase.Playing;
            AdvanceToContent();
        }

        /// <summary>跳过：快进到下一个选项或脚本结尾。</summary>
        public void Skip()
        {
            if (Phase == AdvPhase.Finished) return;
            var guard = 0;
            while (guard++ < 10000)
            {
                if (Phase == AdvPhase.Choice) return;
                if (Phase == AdvPhase.Finished) return;
                AdvanceToContent(skipLines: true);
            }
        }

        private void AdvanceToContent(bool skipLines = false)
        {
            while (true)
            {
                _index++;
                if (_script == null || _index >= _script.commands.Count)
                {
                    Phase = AdvPhase.Finished;
                    Current = null;
                    OnFinished?.Invoke();
                    return;
                }
                var cmd = _script.commands[_index];
                ApplyStage(cmd);
                switch (cmd.type)
                {
                    case "line":
                    case "narration":
                        Current = cmd;
                        Phase = AdvPhase.Playing;
                        _charsShown = skipLines ? (cmd.text != null ? cmd.text.Length : 0) : 0f;
                        if (skipLines) _history.Add(FormatLine(cmd));
                        else if (cmd.type == "line") _history.Add(FormatLine(cmd));
                        return;
                    case "choice":
                        Current = cmd;
                        Phase = AdvPhase.Choice;
                        return;
                    case "tip":
                        OnTip?.Invoke(cmd.tip);
                        continue;
                    case "loopreset":
                        if (!string.IsNullOrEmpty(cmd.tip)) OnTip?.Invoke(cmd.tip);
                        OnLoopReset?.Invoke(cmd.anchor, cmd.tip);
                        Phase = AdvPhase.Finished;
                        Current = null;
                        return;
                    case "end":
                        Phase = AdvPhase.Finished;
                        Current = null;
                        OnFinished?.Invoke();
                        return;
                    default:
                        // bg / portrait / bgm / sfx / flag 等瞬时指令继续前进
                        continue;
                }
            }
        }

        private void ApplyStage(AdvCommand cmd)
        {
            switch (cmd.type)
            {
                case "bg":
                    if (!string.IsNullOrEmpty(cmd.color)) BgColor = cmd.color;
                    BgArt = cmd.art ?? "";
                    BgNote = cmd.note ?? "";
                    break;
                case "portrait":
                    if (cmd.side == "left") LeftPortrait = cmd.portrait ?? "";
                    else if (cmd.side == "right") RightPortrait = cmd.portrait ?? "";
                    else if (cmd.portrait == "") { LeftPortrait = ""; RightPortrait = ""; }
                    break;
            }
            OnStage?.Invoke(cmd);
        }

        private static string FormatLine(AdvCommand cmd)
        {
            if (cmd.type == "narration") return "　" + cmd.text;
            return string.IsNullOrEmpty(cmd.speaker) ? cmd.text : $"{cmd.speaker}：{cmd.text}";
        }
    }
}
