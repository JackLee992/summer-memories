using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SummerMemories.Core;
using SummerMemories.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SummerMemories.Battle
{
    /// <summary>
    /// 战棋视图（代码构建 uGUI）：网格、单位、移动/攻击高亮、敌人意图预告、
    /// 救命毫毛回溯、桃橛瞄准、战斗日志与胜负结算。
    /// </summary>
    public class BattleView : MonoBehaviour
    {
        private const float CellSize = 128f;
        private const float GridOriginX = -320f; // 相对中心偏移，右侧留给信息面板
        private const float GridOriginY = 0f;

        private BattleDirector _battle;
        private Transform _gridRoot;
        private Transform _unitRoot;
        private Text _topInfo;
        private Text _logText;
        private GameObject _endPanel;
        private Text _endTitle;
        private Button _nailBtn;
        private Button _rewindBtn;
        private Button _endTurnBtn;
        private Button _foresightBtn;
        private bool _nailAiming;

        private System.Action<bool> _onEnd; // true=胜利 false=败北

        public void Play(BattleConfig config, System.Action<bool> onEnd)
        {
            _onEnd = onEnd;
            _battle = new BattleDirector();
            _battle.OnChanged += Refresh;
            _battle.OnVictory += () => ShowEnd(true);
            _battle.OnDefeat += () => ShowEnd(false);
            BuildUi();
            _battle.Init(config);
        }

        private void BuildUi()
        {
            var canvas = UIFactory.CreateCanvas("Battle", transform, sorting: 10);
            var root = canvas.transform;
            UIFactory.CreatePanel("Bg", root, Palette.Night);

            // 顶部信息
            _topInfo = UIFactory.CreateText("TopInfo", root, "", 32, Palette.Paper, TextAnchor.UpperLeft);
            SetOffsets((RectTransform)_topInfo.transform, new Vector2(50, -30), new Vector2(-560, -110));

            // 网格与单位根
            _gridRoot = NewChild(root, "GridRoot");
            _unitRoot = NewChild(root, "UnitRoot");

            // 右侧面板
            var panel = UIFactory.CreatePanel("SidePanel", root, Palette.Panel,
                new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-600, 30), new Vector2(-30, -30));
            var title = UIFactory.CreateText("BattleTitle", panel.transform, "战斗", 34, Palette.Gold,
                TextAnchor.UpperCenter);
            SetOffsets((RectTransform)title.transform, new Vector2(20, -20), new Vector2(-20, -80));

            _logText = UIFactory.CreateText("Log", panel.transform, "", 24, Palette.Paper, TextAnchor.UpperLeft);
            SetOffsets((RectTransform)_logText.transform, new Vector2(24, -440), new Vector2(-24, -24));

            // 按钮（右下向上排列）
            _rewindBtn = UIFactory.CreateButtonCentered("Rewind", panel.transform, "救命毫毛 ×3", 30,
                Palette.DeepSea, Palette.White, OnRewind, 0, -330, 480, 76);
            _foresightBtn = UIFactory.CreateButtonCentered("Foresight", panel.transform, "火眼金睛（窥敌一回合）", 26,
                Palette.Gold, Palette.Ink, OnForesight, 0, -240, 480, 70);
            _nailBtn = UIFactory.CreateButtonCentered("Nail", panel.transform, "桃橛定身（点相邻夜叉）", 26,
                Palette.ShadowAccent, Palette.White, OnNailAim, 0, -160, 480, 70);
            UIFactory.CreateButtonCentered("Wait", panel.transform, "待机", 28,
                Palette.PanelSoft, Palette.Paper, OnWait, 0, -80, 230, 64);
            _endTurnBtn = UIFactory.CreateButtonCentered("EndTurn", panel.transform, "结束回合", 30,
                Palette.Danger, Palette.White, OnEndTurn, 0, 10, 480, 76);

            // 结算覆盖层
            _endPanel = UIFactory.CreatePanel("EndPanel", root, new Color(0.03f, 0.04f, 0.08f, 0.92f));
            _endTitle = UIFactory.CreateText("EndTitle", _endPanel.transform, "", 64, Palette.Gold,
                TextAnchor.MiddleCenter);
            Stretch((RectTransform)_endTitle.transform);
            var hint = UIFactory.CreateText("Hint", _endPanel.transform, "（点击屏幕继续）", 28,
                Palette.Dim, TextAnchor.LowerCenter);
            SetOffsets((RectTransform)hint.transform, new Vector2(0, 120), new Vector2(0, 200));
            var catcher = new GameObject("EndCatcher", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            catcher.transform.SetParent(_endPanel.transform, false);
            catcher.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            Stretch((RectTransform)catcher.transform);
            catcher.GetComponent<Button>().onClick.AddListener(() =>
            {
                var win = _battle.Phase == BattlePhase.Victory;
                _onEnd?.Invoke(win);
            });
            _endPanel.SetActive(false);
        }

        private Transform NewChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        // —— 输入 ——
        private void OnCellClick(int x, int y)
        {
            if (_battle.Phase != BattlePhase.Player) return;
            var occupant = Grid.UnitAt(_battle.Units, x, y);

            if (_nailAiming && _battle.Selected != null)
            {
                if (occupant != null && occupant.Team == Team.Shadow)
                {
                    _battle.TryNail(occupant);
                }
                _nailAiming = false;
                return;
            }

            if (_battle.Selected != null)
            {
                // 攻击范围内的敌人
                if (occupant != null && occupant.Team == Team.Shadow
                    && _battle.CanAttack(occupant))
                {
                    _battle.Attack(_battle.Selected, occupant);
                    return;
                }
                // 移动
                if (occupant == null && _battle.CanMoveTo(x, y))
                {
                    _battle.Move(_battle.Selected, x, y);
                    return;
                }
            }
            // 选中我方单位
            if (occupant != null && occupant.Team == Team.Ally) _battle.Select(occupant);
            else _battle.Select(null);
        }

        private void OnNailAim()
        {
            if (_battle.Selected == null || _battle.Selected.Skill != UnitSkill.Nail) return;
            _nailAiming = true;
            Log.Warn("桃橛瞄准中：点击相邻的夜叉");
        }

        private void OnWait()
        {
            if (_battle.Selected != null) _battle.Wait(_battle.Selected);
        }

        private void OnRewind()
        {
            _nailAiming = false;
            _battle.TryRewind();
        }

        private void OnForesight()
        {
            _battle.RevealIntents();
        }

        private void OnEndTurn()
        {
            if (_battle.Phase != BattlePhase.Player) return;
            _nailAiming = false;
            _battle.Select(null);
            _battle.BeginIntentPreview();
            StartCoroutine(PreviewThenExecute());
        }

        private IEnumerator PreviewThenExecute()
        {
            yield return new WaitForSeconds(2.0f);
            if (_battle != null && _battle.Phase == BattlePhase.IntentPreview)
                _battle.ExecuteEnemyTurns();
        }

        // —— 渲染 ——
        private void Refresh()
        {
            if (_battle == null) return;
            RenderGrid();
            RenderUnits();
            RenderHud();
        }

        private void RenderGrid()
        {
            ClearChildren(_gridRoot);
            var moveSet = new HashSet<(int, int)>(_battle.MoveCells.Select(c => (c.X, c.Y)));
            var attackSet = new HashSet<(int, int)>(_battle.AttackTargets.Select(u => (u.X, u.Y)));
            var intentAttack = new HashSet<(int, int)>();
            var intentMove = new HashSet<(int, int)>();
            if (_battle.Phase == BattlePhase.IntentPreview || _battle.ForesightActive)
            {
                foreach (var i in _battle.Intents)
                {
                    intentMove.Add((i.ToX, i.ToY));
                    if (i.Attacks) intentAttack.Add((i.TargetX, i.TargetY));
                }
            }

            for (var y = 0; y < _battle.Config.height; y++)
            {
                for (var x = 0; x < _battle.Config.width; x++)
                {
                    Color color = (x + y) % 2 == 0 ? Palette.PanelSoft : Palette.DeepSea;
                    var go = MakeCell(x, y, color);
                    if (moveSet.Contains((x, y))) go.GetComponent<Image>().color =
                        new Color(0.35f, 0.65f, 0.9f, 0.85f);
                    if (intentMove.Contains((x, y))) go.GetComponent<Image>().color =
                        new Color(0.85f, 0.75f, 0.25f, 0.7f);
                    if (attackSet.Contains((x, y)) || intentAttack.Contains((x, y)))
                    {
                        var mark = UIFactory.CreatePanel("AttackMark", go.transform,
                            new Color(0.85f, 0.25f, 0.25f, 0.55f));
                        var t = UIFactory.CreateText("T", mark.transform,
                            intentAttack.Contains((x, y)) ? "袭" : "", 30, Palette.White,
                            TextAnchor.MiddleCenter);
                        Stretch((RectTransform)t.transform);
                    }
                }
            }
        }

        private GameObject MakeCell(int x, int y, Color color)
        {
            var go = new GameObject($"Cell_{x}_{y}", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            go.transform.SetParent(_gridRoot, false);
            var img = go.GetComponent<Image>();
            img.sprite = UIFactory.WhiteSprite;
            img.color = color;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(CellSize - 4, CellSize - 4);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            var px = GridOriginX + (x - _battle.Config.width / 2f + 0.5f) * CellSize;
            var py = GridOriginY + (_battle.Config.height / 2f - 0.5f - y) * CellSize;
            rt.anchoredPosition = new Vector2(px, py);
            var cx = x; var cy = y;
            go.GetComponent<Button>().onClick.AddListener(() => OnCellClick(cx, cy));
            return go;
        }

        private void RenderUnits()
        {
            ClearChildren(_unitRoot);
            foreach (var u in _battle.Units)
            {
                if (!u.Alive) continue;
                var color = u.Team == Team.Ally ? Palette.Ally : Palette.Shadow;
                if (u.Team == Team.Shadow && u.Stunned) color = Palette.Dim;
                var go = new GameObject($"Unit_{u.Id}", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image));
                go.transform.SetParent(_unitRoot, false);
                var img = go.GetComponent<Image>();
                img.sprite = UIFactory.WhiteSprite;
                img.color = color;
                img.raycastTarget = false; // 点击须穿透到格子按钮
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(CellSize - 18, CellSize - 18);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                var px = GridOriginX + (u.X - _battle.Config.width / 2f + 0.5f) * CellSize;
                var py = GridOriginY + (_battle.Config.height / 2f - 0.5f - u.Y) * CellSize;
                rt.anchoredPosition = new Vector2(px, py);

                var label = UIFactory.CreateText("Label", go.transform,
                    $"{u.DisplayName}\n{u.Hp}/{u.MaxHp}" + (u.Acted ? " 已" : "") + (u.Stunned ? " 定" : ""),
                    24, Palette.White, TextAnchor.MiddleCenter, false);
                Stretch((RectTransform)label.transform);

                if (_battle.Selected == u)
                {
                    var frame = UIFactory.CreatePanel("SelFrame", go.transform, new Color(1f, 0.84f, 0.25f, 0.28f));
                    frame.GetComponent<Image>().raycastTarget = false;
                    var outline = frame.AddComponent<Outline>();
                    outline.effectColor = Palette.Gold;
                    outline.effectDistance = new Vector2(4, -4);
                }
            }
        }

        private void RenderHud()
        {
            _topInfo.text = $"{_battle.Config.title}　第 {_battle.TurnNumber} 回合\n" +
                           $"救命毫毛 {_battle.Rewind.ChargesLeft}/{_battle.Rewind.MaxCharges}　" +
                           $"桃橛 {_battle.NailsLeft}";
            _logText.text = string.Join("\n", _battle.Log.Skip(System.Math.Max(0, _battle.Log.Count - 12)));
            var playerPhase = _battle.Phase == BattlePhase.Player;
            _endTurnBtn.interactable = playerPhase;
            _rewindBtn.interactable = playerPhase && _battle.Rewind.CanRewind;
            _rewindBtn.GetComponentInChildren<Text>().text =
                $"救命毫毛 ×{_battle.Rewind.ChargesLeft}";
            _foresightBtn.interactable = playerPhase && _battle.CanForesight;
            _nailBtn.interactable = playerPhase && _battle.NailsLeft > 0
                && _battle.Selected != null && _battle.Selected.Skill == UnitSkill.Nail;
        }

        private void ShowEnd(bool victory)
        {
            _endPanel.SetActive(true);
            _endTitle.text = victory ? "战斗胜利" : "回潮";
            _endTitle.color = victory ? Palette.Gold : Palette.Danger;
        }

        public void Close() { if (gameObject != null) Destroy(gameObject); }

        private static void ClearChildren(Transform t)
        {
            for (var i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        private static void SetOffsets(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.offsetMin = min; rt.offsetMax = max;
        }
    }
}
