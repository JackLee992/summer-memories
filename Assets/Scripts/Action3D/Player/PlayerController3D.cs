using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SummerMemories.Action3D
{
    /// <summary>
    /// 石头：第三人称移动/奔跑/闪避(无敌帧)，轻击三连、重击、桃橛(范围眩晕)、火眼(暴击窗口)、锁定。
    /// M1 纯代码驱动占位人形动作；M3 替换为 Mixamo 动画状态机，战斗判定与手感层保持不变。
    /// </summary>
    [RequireComponent(typeof(Combatant3D))]
    public class PlayerController3D : MonoBehaviour
    {
        public CameraRig3D Rig;
        public List<Combatant3D> Enemies = new List<Combatant3D>();
        public Transform LockTarget;

        public float MaxStamina = 100f;
        public float Stamina = 100f;
        public float EyeUntil;        // 火眼暴击窗口结束时间
        public float EyeCdUntil;      // 火眼冷却
        public bool EyeActive => Time.time < EyeUntil;

        private enum S { Free, Attack, Dodge, Hurt, Dead }
        private S _st = S.Free;

        private Combatant3D _c;
        private CharacterController _cc;
        private Transform _weapon;
        private Transform _visual;

        private float _walk = 3.3f, _run = 6.0f, _gravity = -24f, _vy;
        private float _dodgeCdUntil;
        private float _nailCdUntil;
        private float _prevHp;
        private Coroutine _action;
        private float _bob;

        // 三段轻击数值
        private static readonly float[] LightDmg = { 1f, 1f, 2f };
        private static readonly float[] LightKnock = { 2.2f, 2.6f, 5f };

        private void Awake()
        {
            _c = GetComponent<Combatant3D>();
            _cc = GetComponent<CharacterController>();
            _cc.center = new Vector3(0f, 1f, 0f);
            _cc.height = 2f;
            _cc.radius = 0.42f;
            _cc.slopeLimit = 55f;
            _c.Team = Team.Player;
            _c.MaxHp = 5f;
            _c.Hp = 5f;
            _c.MoveSpeed = _walk;
            _c.BuildVisual(
                new Color(0.85f, 0.28f, 0.20f),   // 身体：石红短衫
                new Color(0.16f, 0.22f, 0.30f),   // 点缀：藏青
                new Color(0.62f, 0.40f, 0.18f),   // 桃木棍
                true);
            _visual = _c.Visual;
            _weapon = _c.WeaponPivot;
            _prevHp = _c.Hp;
        }

        private void Update()
        {
            if (_c == null) return;
            var dt = Time.deltaTime;

            if (_c.Dead) { _st = S.Dead; return; }

            // 受击检测（血量下降即硬直，打断攻击/闪避）
            if (_c.Hp < _prevHp - 0.001f)
            {
                if (_st != S.Dodge)
                {
                    if (_action != null) { StopCoroutine(_action); _action = null; }
                    _st = S.Hurt;
                }
            }
            _prevHp = _c.Hp;
            if (_st == S.Hurt && Time.time >= _c.StunUntil) _st = S.Free;

            // 重力（所有状态）
            if (_cc.isGrounded && _vy < 0f) _vy = -2f;
            _vy += _gravity * dt;
            _cc.Move(Vector3.up * (_vy * dt));

            // 击退
            if (_c.KnockVel.sqrMagnitude > 0.01f)
            {
                _cc.Move(_c.KnockVel * dt);
                _c.KnockVel = Vector3.MoveTowards(_c.KnockVel, Vector3.zero, 9f * dt);
            }

            // 体力回复
            if (_st == S.Free) Stamina = Mathf.Min(MaxStamina, Stamina + 30f * dt);

            switch (_st)
            {
                case S.Free: TickFree(dt); break;
                case S.Attack: FaceLockTarget(dt); break;
                case S.Hurt: FaceLockTarget(dt); break;
            }

            AnimateLocomotion(dt);
        }

        private void TickFree(float dt)
        {
            if (Input3D.Pressed(Input3D.Btn.Lock)) ToggleLock();
            if (Input3D.Pressed(Input3D.Btn.Skill)) TryEye();
            if (Input3D.Pressed(Input3D.Btn.Light)) { _action = StartCoroutine(LightChain()); return; }
            if (Input3D.Pressed(Input3D.Btn.Heavy)) { _action = StartCoroutine(Heavy()); return; }
            if (Input3D.Pressed(Input3D.Btn.Nail)) { _action = StartCoroutine(Nail()); return; }
            if (Input3D.Pressed(Input3D.Btn.Dodge)) { _action = StartCoroutine(Dodge()); return; }

            var mv = Input3D.Move();
            var yaw = Rig != null ? Rig.Yaw : 0f;
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var dir = (rot * Vector3.forward * mv.y + rot * Vector3.right * mv.x);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) dir.Normalize();

            var sprint = Input3D.RunHeld && mv.y > 0.1f && Stamina > 1f;
            if (sprint) Stamina = Mathf.Max(0f, Stamina - 14f * dt);
            var speed = sprint ? _run : _walk;
            if (dir.sqrMagnitude > 0.01f)
            {
                _cc.Move(dir * speed * dt);
                if (LockTarget == null)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(dir), 1f - Mathf.Exp(-14f * dt));
            }
            if (LockTarget != null) FaceLockTarget(dt);
        }

        private void FaceLockTarget(float dt)
        {
            if (LockTarget == null) return;
            var to = LockTarget.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(to), 1f - Mathf.Exp(-16f * dt));
        }

        // ---------- 攻击 ----------

        private IEnumerator LightChain()
        {
            _st = S.Attack;
            var idx = 0;
            while (idx < 3)
            {
                yield return Swing(idx);
                if (idx == 2) break;
                // 连段衔接窗口
                float t = 0f, window = 0.34f; bool goNext = false;
                while (t < window)
                {
                    if (Input3D.Pressed(Input3D.Btn.Light)) { goNext = true; break; }
                    t += Time.deltaTime; yield return null;
                }
                if (!goNext) break;
                idx++;
            }
            _weapon.localRotation = Quaternion.identity;
            _st = S.Free;
            _action = null;
        }

        private IEnumerator Swing(int idx)
        {
            FaceLockTarget(0.2f);
            var dmg = LightDmg[idx];
            var knock = LightKnock[idx];
            var hit = new HashSet<Combatant3D>();
            bool judged = false;

            // 起手
            yield return Stroke(0.10f, 0f, -100f, -30f);
            // 判定 + 挥出
            var t = 0f; const float active = 0.12f;
            while (t < active)
            {
                t += Time.deltaTime;
                var k = t / active;
                _weapon.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-30f, 70f, k), 0f);
                // 小幅前冲
                _cc.Move(transform.forward * (1.6f * Time.deltaTime));
                if (!judged && k > 0.45f)
                {
                    judged = true;
                    DoMelee(1.7f, 1.05f, dmg, knock, out _);
                }
                yield return null;
            }
            GameFeel3D.I?.Slash(transform.position + transform.forward * 1.4f + Vector3.up * 1.3f,
                Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0f, 0f, 90f),
                new Color(1f, 0.95f, 0.7f));
            // 收招
            yield return Stroke(0.16f, 0f, 70f, 0f);
        }

        private IEnumerator Heavy()
        {
            if (Stamina < 15f) { _st = S.Free; _action = null; yield break; }
            Stamina -= 15f;
            _st = S.Attack;
            FaceLockTarget(0.2f);
            yield return Stroke(0.28f, 0f, 0f, -130f); // 举棍过顶
            bool judged = false;
            var t = 0f; const float active = 0.18f;
            while (t < active)
            {
                t += Time.deltaTime;
                var k = t / active;
                _weapon.localRotation = Quaternion.Euler(Mathf.Lerp(-130f, 40f, k), 0f, 0f);
                _cc.Move(transform.forward * (1.2f * Time.deltaTime));
                if (!judged && k > 0.4f)
                {
                    judged = true;
                    DoMelee(1.9f, 1.25f, 2f, 6.5f, out var any);
                    if (any) GameFeel3D.I?.Freeze(0.08f, 0.03f);
                }
                yield return null;
            }
            GameFeel3D.I?.Slash(transform.position + transform.forward * 1.5f + Vector3.up * 1.2f,
                Quaternion.LookRotation(transform.forward), new Color(1f, 0.7f, 0.35f));
            yield return Stroke(0.24f, 0f, 40f, 0f);
            _weapon.localRotation = Quaternion.identity;
            _st = S.Free;
            _action = null;
        }

        private IEnumerator Nail()
        {
            if (Time.time < _nailCdUntil || Stamina < 25f) { _st = S.Free; _action = null; yield break; }
            Stamina -= 25f; _nailCdUntil = Time.time + 5f;
            _st = S.Attack;
            FaceLockTarget(0.2f);
            yield return Stroke(0.26f, 0f, -20f, -60f);
            // 前方扇形 AOE：伤害 + 眩晕
            var hits = Physics.OverlapSphere(transform.position + Vector3.up * 1.2f, 3.6f);
            var any = false;
            foreach (var col in hits)
            {
                var e = col.GetComponentInParent<Combatant3D>();
                if (e == null || e.Team != Team.Enemy || e.Dead) continue;
                var to = e.transform.position - transform.position; to.y = 0f;
                if (Vector3.Angle(transform.forward, to) > 75f) continue;
                var info = new DamageInfo
                {
                    Damage = 1f,
                    Direction = (e.transform.position - transform.position).normalized,
                    Knockback = 3.5f,
                    Point = e.transform.position + Vector3.up * 1.2f,
                    Crit = false
                };
                if (e.Hurt(info))
                {
                    e.StunUntil = Mathf.Max(e.StunUntil, Time.time + 1.6f); // 桃橛钉桩眩晕
                    e.WarnGlow(false);
                    GameFeel3D.I?.Hit(info.Point, false);
                    GameFeel3D.I?.FloatingText(info.Point, "钉", new Color(0.8f, 0.95f, 1f), 1.1f);
                    any = true;
                }
            }
            if (any) GameFeel3D.I?.Freeze(0.1f, 0.04f);
            yield return new WaitForSeconds(0.24f);
            _weapon.localRotation = Quaternion.identity;
            _st = S.Free;
            _action = null;
        }

        private IEnumerator Stroke(float dur, float yFrom, float yTo, float xTarget)
        {
            var t = 0f;
            var a = _weapon.localRotation;
            var b = Quaternion.Euler(xTarget, yTo, 0f);
            while (t < dur)
            {
                t += Time.deltaTime;
                _weapon.localRotation = Quaternion.Slerp(a, b, t / dur);
                yield return null;
            }
        }

        private void DoMelee(float range, float halfW, float dmg, float knock, out bool any)
        {
            any = false;
            var crit = EyeActive;
            var center = transform.position + Vector3.up * 1.2f + transform.forward * range * 0.5f;
            var hits = Physics.OverlapBox(center, new Vector3(halfW, 1.25f, range * 0.5f),
                transform.rotation);
            var seen = new HashSet<Combatant3D>();
            foreach (var col in hits)
            {
                var e = col.GetComponentInParent<Combatant3D>();
                if (e == null || e.Team != Team.Enemy || e.Dead || !seen.Add(e)) continue;
                var realDmg = crit ? dmg * 2f : dmg;
                var info = new DamageInfo
                {
                    Damage = realDmg,
                    Direction = (e.transform.position - transform.position).normalized + transform.forward * 0.4f,
                    Knockback = knock,
                    Point = e.transform.position + Vector3.up * 1.2f,
                    Crit = crit
                };
                if (e.Hurt(info))
                {
                    any = true;
                    GameFeel3D.I?.Hit(info.Point, crit);
                    GameFeel3D.I?.FloatingText(info.Point, crit ? Mathf.RoundToInt(realDmg) + "!" : Mathf.RoundToInt(realDmg).ToString(),
                        crit ? new Color(1f, 0.84f, 0.3f) : Color.white, crit ? 1.4f : 1f);
                }
            }
        }

        // ---------- 闪避 / 火眼 / 锁定 ----------

        private IEnumerator Dodge()
        {
            if (Time.time < _dodgeCdUntil || Stamina < 20f) { _st = S.Free; _action = null; yield break; }
            Stamina -= 20f; _dodgeCdUntil = Time.time + 0.65f;
            _st = S.Dodge;
            var mv = Input3D.Move();
            var yaw = Rig != null ? Rig.Yaw : 0f;
            var rotQ = Quaternion.Euler(0f, yaw, 0f);
            var dir = (rotQ * Vector3.forward * mv.y + rotQ * Vector3.right * mv.x);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.1f) dir = -transform.forward;
            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir);

            _c.Invulnerable = true;
            var t = 0f; const float dur = 0.32f;
            while (t < dur)
            {
                t += Time.deltaTime;
                var k = t / dur;
                var speed = Mathf.Sin(k * Mathf.PI) * 11f + 2f; // 首尾缓动的冲刺
                _cc.Move(dir * speed * Time.deltaTime);
                if (_visual != null) _visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI) * -18f);
                if (t > 0.24f) _c.Invulnerable = false;
                yield return null;
            }
            _c.Invulnerable = false;
            if (_visual != null) _visual.localRotation = Quaternion.identity;
            _st = S.Free;
            _action = null;
        }

        private void TryEye()
        {
            if (Time.time < EyeCdUntil) return;
            EyeCdUntil = Time.time + 9f;
            EyeUntil = Time.time + 3.5f;
            GameFeel3D.I?.FloatingText(transform.position + Vector3.up * 2.2f, "火眼",
                new Color(1f, 0.5f, 0.2f), 1.2f);
            if (LockTarget == null) ToggleLock();
        }

        public void ToggleLock()
        {
            Combatant3D best = null;
            var bestScore = float.MaxValue;
            foreach (var e in Enemies)
            {
                if (e == null || e.Dead) continue;
                var to = e.transform.position - transform.position;
                var dist = to.magnitude;
                if (dist > 16f) continue;
                // 距离 + 与当前朝向夹角综合
                var ang = Vector3.Angle(transform.forward, to);
                var score = dist + ang * 0.08f;
                if (score < bestScore) { bestScore = score; best = e; }
            }
            if (best == null) { LockTarget = null; if (Rig != null) Rig.LockTarget = null; return; }
            // 已锁同一目标则取消
            if (LockTarget == best.transform)
            { LockTarget = null; if (Rig != null) Rig.LockTarget = null; }
            else
            { LockTarget = best.transform; if (Rig != null) Rig.LockTarget = best.transform; }
        }

        public void ClearLockIf(Combatant3D dead)
        {
            if (LockTarget == dead.transform)
            { LockTarget = null; if (Rig != null) Rig.LockTarget = null; }
        }

        /// <summary>回潮/重开后重置战斗状态（复活由 Combatant.ResetTo 处理）。</summary>
        public void ResetState()
        {
            _st = S.Free;
            _action = null;
            Stamina = MaxStamina;
            EyeUntil = 0f;
            _c.Invulnerable = false;
            LockTarget = null;
            if (Rig != null) Rig.LockTarget = null;
            if (_visual != null) { _visual.localRotation = Quaternion.identity; _visual.localPosition = Vector3.zero; }
            if (_weapon != null) _weapon.localRotation = Quaternion.identity;
        }

        private void AnimateLocomotion(float dt)
        {
            if (_visual == null || _st == S.Dodge) return;
            if (_st != S.Free) { _visual.localRotation = Quaternion.Slerp(_visual.localRotation, Quaternion.identity, dt*10f); return; }
            var speed = _cc.velocity; speed.y = 0f;
            var moving = speed.magnitude > 0.4f;
            if (moving)
            {
                _bob += dt * 11f;
                _visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_bob)) * 0.06f, 0f);
                _visual.localRotation = Quaternion.Euler(8f, 0f, 0f); // 奔跑前倾
            }
            else
            {
                _bob += dt * 2f;
                _visual.localPosition = new Vector3(0f, Mathf.Sin(_bob) * 0.015f, 0f);
                _visual.localRotation = Quaternion.Slerp(_visual.localRotation, Quaternion.identity, dt*8f);
            }
        }
    }
}
