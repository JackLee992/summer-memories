using System.Collections;
using UnityEngine;

namespace SummerMemories.Action3D
{
    /// <summary>
    /// 打击感层：命中顿帧(hit-stop)、命中爆光、刀光、3D 伤害飘字。
    /// 顿帧用 unscaledTime 计时恢复；回潮演出期间由 Director 置 Suspended 暂停接管 timeScale。
    /// </summary>
    public class GameFeel3D : MonoBehaviour
    {
        public static GameFeel3D I;
        public Camera Camera;
        public CameraRig3D Rig;

        private float _stopUntil;
        public bool Suspended;

        private void Awake() { I = this; }

        public void Freeze(float seconds = 0.06f, float scale = 0.05f)
        {
            Time.timeScale = scale;
            _stopUntil = Time.unscaledTime + seconds;
        }

        private void Update()
        {
            if (Suspended) return;
            if (_stopUntil > 0f && Time.unscaledTime >= _stopUntil)
            {
                Time.timeScale = 1f;
                _stopUntil = 0f;
            }
        }

        public void Hit(Vector3 point, bool crit)
        {
            Freeze(crit ? 0.09f : 0.05f, crit ? 0.02f : 0.06f);
            if (Rig != null) Rig.AddShake(crit ? 0.6f : 0.32f);
            StartCoroutine(Spark(point, crit ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.95f, 0.75f),
                crit ? 1.5f : 1.0f));
        }

        private IEnumerator Spark(Vector3 point, Color c, float size)
        {
            var go = Vis.Prim("HitSpark", PrimitiveType.Quad, null, c,
                point, Vector3.one * 0.2f, collider: false, mat: Mat.MakeEmissive(c, 3f));
            if (Camera != null) go.transform.rotation = Camera.transform.rotation;
            var t = 0f;
            const float dur = 0.14f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                var k = t / dur;
                go.transform.localScale = Vector3.one * (0.2f + k * 1.6f * size);
                if (Camera != null) go.transform.rotation = Camera.transform.rotation;
                yield return null;
            }
            Destroy(go);
        }

        public void Slash(Vector3 pivot, Quaternion rot, Color c)
        {
            StartCoroutine(SlashRoutine(pivot, rot, c));
        }

        private IEnumerator SlashRoutine(Vector3 pivot, Quaternion rot, Color c)
        {
            var go = Vis.Prim("Slash", PrimitiveType.Cube, null, c,
                pivot, new Vector3(1.6f, 0.06f, 0.06f), collider: false,
                mat: Mat.MakeEmissive(c, 2.4f));
            go.transform.rotation = rot;
            var t = 0f;
            const float dur = 0.1f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                var k = t / dur;
                go.transform.localScale = new Vector3(1.6f * (1f - k * 0.4f), 0.06f, 0.06f);
                yield return null;
            }
            Destroy(go);
        }

        public void FloatingText(Vector3 worldPos, string text, Color c, float size = 1f)
        {
            var go = new GameObject("DmgText");
            go.transform.position = worldPos + Vector3.up * 2f + Random.insideUnitSphere * 0.15f;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 56;
            tm.characterSize = 0.16f * size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = c;
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = tm.font.material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            StartCoroutine(FloatAndFade(go, size));
        }

        private IEnumerator FloatAndFade(GameObject go, float size)
        {
            var tm = go.GetComponent<TextMesh>();
            var start = go.transform.position;
            var t = 0f;
            const float dur = 0.75f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                var k = t / dur;
                go.transform.position = start + Vector3.up * (1.1f * k);
                if (Camera != null)
                    go.transform.rotation = Quaternion.LookRotation(go.transform.position - Camera.transform.position);
                var col = tm.color;
                col.a = 1f - k;
                tm.color = col;
                go.transform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.18f) * size;
                yield return null;
            }
            Destroy(go);
        }
    }
}
