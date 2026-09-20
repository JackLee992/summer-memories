using UnityEngine;

namespace SummerMemories.Action3D
{
    /// <summary>M1 调试/战斗 HUD（IMGUI，零资产）。M2 替换为 uGUI 美术界面；移动端虚拟按键见底部。</summary>
    public class Hud3D : MonoBehaviour
    {
        public Game3DDirector D;
        private static Texture2D _white;
        private float _scale;

        private static Texture2D White()
        {
            if (_white != null) return _white;
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
            return _white;
        }

        private void OnGUI()
        {
            if (D == null) return;
            _scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * _scale);
            var W = Screen.width / _scale;

            var p = D.Player;
            if (p != null && D.Phase != Game3DDirector.GamePhase.Intro)
            {
                // 左上：生命 / 耐力 / 回潮
                Tint(new Rect(40, 36, 360, 34), new Color(0, 0, 0, 0.35f));
                Label(new Rect(52, 38, 120, 30), "石头", 26, Color.white);
                Bar(new Rect(150, 46, 230, 16), p.Hp / p.MaxHp, new Color(0.85f, 0.22f, 0.18f));
                Bar(new Rect(150, 64, 230, 8), D.PlayerCtl.Stamina / D.PlayerCtl.MaxStamina, new Color(0.95f, 0.78f, 0.3f));

                var charges = D.Rewind.Charges;
                Label(new Rect(40, 86, 600, 30), "回潮 · 毫毛  " + new string('◇', charges) + new string('·', Rewind3D.MaxCharges - charges),
                    24, new Color(0.6f, 0.85f, 1f));

                var eye = D.PlayerCtl.EyeActive;
                Label(new Rect(40, 116, 600, 26),
                    eye ? "火眼 · 看破（暴击）" : (Time.time < D.PlayerCtl.EyeCdUntil ? "火眼 调息中…" : "火眼 就绪 [E]"),
                    22, eye ? new Color(1f, 0.55f, 0.2f) : new Color(1, 1, 1, 0.6f));

                // 右上：敌人剩余
                var alive = D.AliveEnemies();
                Tint(new Rect(W - 300, 36, 260, 40), new Color(0, 0, 0, 0.35f));
                Label(new Rect(W - 288, 40, 240, 32), "巡海夜叉  剩余 " + alive, 24, new Color(0.85f, 0.7f, 1f));

                // 锁定标记
                if (D.PlayerCtl.LockTarget != null)
                {
                    var sp = D.RigCam.WorldToScreenPoint(D.PlayerCtl.LockTarget.position + Vector3.up * 2.4f);
                    if (sp.z > 0)
                    {
                        var gx = sp.x / _scale; var gy = (Screen.height - sp.y) / _scale;
                        Label(new Rect(gx - 20, gy - 30, 40, 40), "▼", 34, new Color(1f, 0.4f, 0.3f));
                    }
                }

                // 底部键位
                Label(new Rect(0, 1000, W, 36),
                    "WASD 移动  Shift 跑  J 轻击(三连)  K 重击  空格 闪避  Q 锁定  L 桃橛  E 火眼  (右键拖动转视角)",
                    22, new Color(1, 1, 1, 0.55f));
            }

            DrawPhase(W);
            if (Application.isMobilePlatform) DrawTouchControls();
        }

        private void DrawPhase(float W)
        {
            switch (D.Phase)
            {
                case Game3DDirector.GamePhase.Intro:
                {
                    Tint(new Rect(0, 0, W, 1080), new Color(0, 0, 0, 0.35f));
                    var t = D.IntroT;
                    Label(new Rect(0, 250, W, 90), "夏日回忆 · 归墟来潮", 64, new Color(0.95f, 0.92f, 0.8f));
                    Label(new Rect(0, 350, W, 50), "海 堤 夜 战", 34, new Color(0.7f, 0.82f, 1f));
                    var line1 = t > 0.6f ? "七月十五，夜。归墟之潮倒灌，海堤上的引魂灯一盏盏灭了。" : "";
                    var line2 = t > 1.8f ? "石头握紧桃橛——今夜，他要把被潮水带走的人，带回来。" : "";
                    if (line1 != "") Label(new Rect(0, 560, W, 40), line1, 30, new Color(0.9f, 0.92f, 0.95f));
                    if (line2 != "") Label(new Rect(0, 610, W, 40), line2, 30, new Color(0.9f, 0.92f, 0.95f));
                    if (t > 2.8f) Label(new Rect(0, 820, W, 40), "按 回车 / 点击 开始", 28, new Color(1f, 0.85f, 0.5f));
                    break;
                }
                case Game3DDirector.GamePhase.Rewind:
                {
                    var k = D.RewindT;
                    var pulse = 0.18f + 0.18f * Mathf.Sin(k * Mathf.PI * 6f);
                    Tint(new Rect(0, 0, W, 1080), new Color(0.35f, 0.62f, 0.75f, pulse));
                    Label(new Rect(0, 420, W, 120), "回  潮", 96, new Color(0.8f, 0.95f, 1f));
                    Label(new Rect(0, 560, W, 40), "潮水倒流，时辰倒转……", 28, new Color(0.85f, 0.95f, 1f));
                    break;
                }
                case Game3DDirector.GamePhase.Victory:
                {
                    Tint(new Rect(0, 0, W, 1080), new Color(0, 0, 0, 0.45f));
                    Label(new Rect(0, 380, W, 90), "海 堤 已 清", 64, new Color(1f, 0.9f, 0.6f));
                    Label(new Rect(0, 480, W, 44), "第一章 · 完", 30, new Color(0.9f, 0.9f, 0.9f));
                    Label(new Rect(0, 700, W, 36), "按 回车 再战", 26, new Color(1f, 0.85f, 0.5f));
                    break;
                }
                case Game3DDirector.GamePhase.Defeat:
                {
                    Tint(new Rect(0, 0, W, 1080), new Color(0, 0, 0, 0.6f));
                    Label(new Rect(0, 400, W, 90), "无 力 回 天 ……", 60, new Color(0.8f, 0.5f, 0.5f));
                    Label(new Rect(0, 700, W, 36), "毫毛已尽。按 回车 重新入潮", 26, new Color(0.85f, 0.8f, 0.8f));
                    break;
                }
            }
        }

        private void DrawTouchControls()
        {
            // M4：右侧动作按钮（按住/点按映射虚拟输入）
            Btn("轻", 1500, 880, Input3D.Btn.Light);
            Btn("重", 1620, 800, Input3D.Btn.Heavy);
            Btn("闪", 1380, 900, Input3D.Btn.Dodge);
            Btn("锁", 1740, 880, Input3D.Btn.Lock);
            Btn("橛", 1620, 940, Input3D.Btn.Nail);
            Btn("眼", 1740, 760, Input3D.Btn.Skill);
        }

        private void Btn(string label, float x, float y, Input3D.Btn b)
        {
            var r = new Rect(x, y, 96, 96);
            var down = GUI.RepeatButton(r, label);
            Input3D.SetVirtualButton(b, down);
        }

        private static void Bar(Rect r, float ratio, Color c)
        {
            Tint(r, new Color(0, 0, 0, 0.55f));
            Tint(new Rect(r.x, r.y, r.width * Mathf.Clamp01(ratio), r.height), c);
        }

        private static void Tint(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, WhiteTexture());
            GUI.color = old;
        }

        private static Texture2D WhiteTexture()
        {
            if (_white != null) return _white;
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
            return _white;
        }

        private static GUIStyle _label;
        private static void Label(Rect r, string s, int size, Color c)
        {
            if (_label == null) { _label = new GUIStyle(GUI.skin.label); _label.alignment = TextAnchor.MiddleCenter; }
            _label.fontSize = size;
            _label.normal.textColor = c;
            GUI.Label(r, s, _label);
        }
    }
}
