using UnityEngine;

namespace SummerMemories.Action3D
{
    /// <summary>
    /// 纯代码生成材质的统一入口（3D 白盒）。
    /// 全部使用工程自带、位于 Resources/Shaders 的极简 SM/Unlit（纯色 + 距离雾），
    /// 不依赖内置 Standard：后者在零场景构建里会被剥离，而把它卷入 unity_builtin_extra
    /// 又会触发本工程的序列化锁断言。SM/Unlit 是普通工程资源，随 Resources 打包，
    /// Shader.Find 稳定命中。将来切 URP 时替换为 URP 版本并恢复光照参数即可。
    /// </summary>
    public static class Mat
    {
        private static Shader _shader;
        private static bool _ready;

        public static Shader Shader
        {
            get
            {
                if (!_ready)
                {
                    _ready = true;
                    _shader = Find("SM/Unlit", "Unlit/Color", "Hidden/Internal-Colored", "Diffuse");
                }
                return _shader;
            }
        }

        private static Shader Find(params string[] names)
        {
            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s != null) return s;
            }
            return null;
        }

        // 白盒阶段无光照：smoothness / metallic 仅为签名兼容，暂不生效。
        public static Material Make(Color c, float smoothness = 0f, float metallic = 0f)
        {
            var m = new Material(Shader);
            SetColor(m, c);
            return m;
        }

        public static Material MakeUnlit(Color c)
        {
            var m = new Material(Shader);
            SetColor(m, c);
            return m;
        }

        // 白盒无自发光通道：用更亮的纯色模拟灯球/高光（URP 阶段恢复真 emission）。
        public static Material MakeEmissive(Color c, float intensity = 1.6f)
        {
            var bright = new Color(
                Mathf.Clamp01(c.r * intensity),
                Mathf.Clamp01(c.g * intensity),
                Mathf.Clamp01(c.b * intensity), 1f);
            return MakeUnlit(bright);
        }

        public static void SetMainColor(Material m, Color c) => SetColor(m, c);

        private static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }

    /// <summary>
    /// 基本体生成助手。第 4 参给颜色（未显式传 mat 时用它生成 SM/Unlit 材质），
    /// 默认移除碰撞体（碰撞由角色控制器/显式 Collider 负责）。
    /// </summary>
    public static class Vis
    {
        public static GameObject Prim(string name, PrimitiveType type, Transform parent,
            Color color, Vector3 localPos, Vector3? scale = null, bool collider = false,
            Material mat = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!collider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            if (scale.HasValue) go.transform.localScale = scale.Value;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat ?? Mat.Make(color);
            return go;
        }

        public static GameObject Empty(string name, Transform parent, Vector3 localPos = default)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }
    }
}
