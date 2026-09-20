using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SummerMemories.Action3D
{
    public enum Team { Player, Enemy }

    public struct DamageInfo
    {
        public float Damage;
        public Vector3 Direction;   // 击退方向（水平）
        public float Knockback;
        public Vector3 Point;
        public bool Crit;
    }

    /// <summary>战斗实体：血量、阵营、占位人形视觉、受击闪红/击退/死亡倒地。玩家与敌人共用。</summary>
    [RequireComponent(typeof(CharacterController))]
    public class Combatant3D : MonoBehaviour
    {
        public Team Team = Team.Enemy;
        public float MaxHp = 3;
        public float Hp;
        public bool Dead;
        public bool Invulnerable;
        public float MoveSpeed = 3.2f;

        public CharacterController Cc;
        public Transform Visual;
        public Transform WeaponPivot;
        public Renderer[] Rends;

        private Material[] _mats;
        private Color[] _baseColors;
        private Color[] _baseEmission;
        private Coroutine _flashRoutine;

        public Vector3 KnockVel;
        public float StunUntil;
        public float SpawnTime;

        protected virtual void Awake()
        {
            Cc = GetComponent<CharacterController>();
            Hp = MaxHp;
            SpawnTime = Time.time;
        }

        public void BuildVisual(Color body, Color accent, Color weapon, bool playerWeapon)
        {
            var vis = new GameObject("Visual").transform;
            vis.SetParent(transform, false);

            var bodyGo = Vis.Prim("Body", PrimitiveType.Capsule, vis, body,
                new Vector3(0f, 1.08f, 0f), new Vector3(0.82f, 0.62f, 0.82f));
            Vis.Prim("Head", PrimitiveType.Sphere, vis, accent,
                new Vector3(0f, 1.92f, 0f), Vector3.one * 0.46f);
            // 肩线（点缀色，区分阵营）
            Vis.Prim("Sash", PrimitiveType.Cube, vis, accent,
                new Vector3(0f, 1.32f, 0f), new Vector3(0.86f, 0.16f, 0.86f));

            // 武器挂点在身体右前侧
            var pivotGo = Vis.Empty("WeaponPivot", vis, new Vector3(0.42f, 1.35f, 0.18f));
            WeaponPivot = pivotGo.transform;
            if (playerWeapon)
            {
                // 桃木短棍：竖长
                Vis.Prim("Weapon", PrimitiveType.Cube, WeaponPivot, weapon,
                    new Vector3(0.05f, 0.62f, 0.05f), new Vector3(0.10f, 1.25f, 0.10f));
            }
            else
            {
                // 骨刀：略弯的长刃（用斜置细 cube 表示）
                var blade = Vis.Prim("Weapon", PrimitiveType.Cube, WeaponPivot, weapon,
                    new Vector3(0f, 0.45f, 0.12f), new Vector3(0.08f, 1.05f, 0.16f));
                blade.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            }

            Visual = vis;
            Rends = vis.GetComponentsInChildren<Renderer>();
            _mats = new Material[Rends.Length];
            _baseColors = new Color[Rends.Length];
            _baseEmission = new Color[Rends.Length];
            for (var i = 0; i < Rends.Length; i++)
            {
                _mats[i] = Rends[i].material; // 访问 material 自动实例化
                _baseColors[i] = _mats[i].HasProperty("_BaseColor")
                    ? _mats[i].GetColor("_BaseColor")
                    : _mats[i].color;
                _baseEmission[i] = _mats[i].HasProperty("_EmissionColor")
                    ? _mats[i].GetColor("_EmissionColor")
                    : Color.black;
            }
        }

        public virtual bool Hurt(DamageInfo dmg)
        {
            if (Dead || Invulnerable) return false;
            Hp -= dmg.Damage;

            KnockVel = dmg.Direction.normalized * dmg.Knockback;
            StunUntil = Time.time + 0.28f;

            Flash(Color.white, 0.12f);
            if (Hp <= 0f)
            {
                Hp = 0f;
                Die();
            }
            return true;
        }

        public virtual void Die()
        {
            if (Dead) return;
            Dead = true;
            Invulnerable = true;
            // 倒地表现：视觉整体倒下并下沉
            StopAllCoroutines();
            StartCoroutine(FallDown());
        }

        private IEnumerator FallDown()
        {
            var t = 0f;
            var start = Visual.localRotation;
            var fall = Quaternion.Euler(82f, 0f, Random.Range(-12f, 12f));
            var startPos = Visual.localPosition;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / 0.5f);
                Visual.localRotation = Quaternion.Slerp(start, fall, k);
                Visual.localPosition = Vector3.Lerp(startPos, startPos + Vector3.down * 0.55f, k);
                yield return null;
            }
            if (Cc != null) Cc.enabled = false;
        }

        public void Flash(Color c, float dur)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(c, dur));
        }

        private IEnumerator FlashRoutine(Color c, float dur)
        {
            SetColor(c);
            yield return new WaitForSecondsRealtime(dur);
            RestoreColor();
            _flashRoutine = null;
        }

        private void SetColor(Color c)
        {
            for (var i = 0; i < _mats.Length; i++)
            {
                if (_mats[i].HasProperty("_BaseColor")) _mats[i].SetColor("_BaseColor", c);
                if (_mats[i].HasProperty("_Color")) _mats[i].SetColor("_Color", c);
            }
        }

        private void RestoreColor()
        {
            for (var i = 0; i < _mats.Length; i++)
            {
                if (_mats[i].HasProperty("_BaseColor")) _mats[i].SetColor("_BaseColor", _baseColors[i]);
                if (_mats[i].HasProperty("_Color")) _mats[i].SetColor("_Color", _baseColors[i]);
            }
        }

        /// <summary>预警：身体泛红光（敌人起手）。</summary>
        public void WarnGlow(bool on)
        {
            for (var i = 0; i < _mats.Length; i++)
            {
                if (!_mats[i].HasProperty("_EmissionColor")) continue;
                _mats[i].EnableKeyword("_EMISSION");
                _mats[i].SetColor("_EmissionColor", on ? new Color(1.2f, 0.12f, 0.08f) : _baseEmission[i]);
            }
        }

        public void ResetTo(Vector3 pos, Quaternion rot, float hp)
        {
            Dead = false;
            Invulnerable = false;
            Hp = MaxHp = hp;
            KnockVel = Vector3.zero;
            StunUntil = 0f;
            if (Cc != null) Cc.enabled = true;
            var t = transform;
            t.position = pos;
            t.rotation = rot;
            if (Visual != null)
            {
                Visual.localRotation = Quaternion.identity;
                Visual.localPosition = Vector3.zero;
            }
            RestoreColor();
            WarnGlow(false);
        }
    }
}
