using UnityEngine;

namespace SummerMemories.Action3D
{
    /// <summary>
    /// 统一输入：M1 键鼠；触屏虚拟键由 HUD 调用 SetVirtual* 注入（M4 安卓）。
    /// 键位：WASD 移动，Shift 奔跑，Space 闪避，J 轻击，K 重击，L 桃橛，Q 锁定/火眼，按住鼠标右键拖动转视角。
    /// </summary>
    public static class Input3D
    {
        // 触屏 / UI 注入的虚拟移动（-1..1）与按键边沿
        public static Vector2 VirtualMove;
        public static bool VirtualRun;
        private static readonly bool[] _virtualDown = new bool[8];
        private static readonly bool[] _virtualPressed = new bool[8];

        public enum Btn
        {
            Light = 0, Heavy = 1, Dodge = 2, Lock = 3, Nail = 4, Skill = 5, Confirm = 6, Cancel = 7
        }

        public static void SetVirtualButton(Btn b, bool held)
        {
            var i = (int)b;
            if (held && !_virtualDown[i]) _virtualPressed[i] = true;
            _virtualDown[i] = held;
        }

        /// <summary>每帧由 Director 在逻辑末尾调用，清掉单帧边沿。</summary>
        public static void EndFrame()
        {
            for (var i = 0; i < _virtualPressed.Length; i++) _virtualPressed[i] = false;
        }

        public static Vector2 Move()
        {
            var x = 0f;
            var y = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
            var k = new Vector2(x, y);
            var v = VirtualMove;
            var merged = k.sqrMagnitude >= v.sqrMagnitude ? k : v;
            return merged.sqrMagnitude > 1f ? merged.normalized : merged;
        }

        public static bool RunHeld => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || VirtualRun;

        public static bool Pressed(Btn b)
        {
            var i = (int)b;
            if (_virtualPressed[i]) return true;
            switch (b)
            {
                case Btn.Light: return Input.GetKeyDown(KeyCode.J);
                case Btn.Heavy: return Input.GetKeyDown(KeyCode.K);
                case Btn.Dodge: return Input.GetKeyDown(KeyCode.Space);
                case Btn.Lock: return Input.GetKeyDown(KeyCode.Q);
                case Btn.Nail: return Input.GetKeyDown(KeyCode.L);
                case Btn.Skill: return Input.GetKeyDown(KeyCode.E);
                case Btn.Confirm: return Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
                default: return false;
            }
        }

        /// <summary>相机旋转输入：右键拖拽（PC）。触屏右半区拖拽由 TouchLook 提供。</summary>
        public static Vector2 Look()
        {
            var v = Vector2.zero;
            if (Input.GetMouseButton(1))
            {
                v.x += Input.GetAxis("Mouse X");
                v.y += Input.GetAxis("Mouse Y");
            }
            if (TouchLookEnabled && Input.touchCount > 0)
            {
                for (var i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Moved && t.position.x > Screen.width * 0.5f)
                        v += t.deltaPosition * 0.01f;
                }
            }
            return v;
        }

        public static bool TouchLookEnabled;
    }
}
