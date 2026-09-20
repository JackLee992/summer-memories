using System.Collections;
using UnityEngine;

namespace SummerMemories.Action3D
{
    /// <summary>
    /// 巡海夜叉（白盒）：追击 → 起手预警(泛红光，给闪避/火眼窗口) → 挥击判定 → 收招循环。
    /// 受击/桃橛眩晕用 Combatant.StunUntil 打断；死亡由基类处理倒地。
    /// </summary>
    [RequireComponent(typeof(Combatant3D))]
    public class Enemy3D : MonoBehaviour
    {
        public Combatant3D Player;
        public float MoveSpeed = 2.5f;
        public float AttackRange = 2.0f;
        public float Damage = 1f;
        public float FirstAttackDelay = 0.6f;

        private enum E { Chase, Windup, Strike, Recover, Idle }
        private E _st = E.Idle;
        private float _t;
        private bool _judged;
        private Combatant3D _c;
        private CharacterController _cc;
        private Transform _weapon;
        private float _gravity = -24f, _vy;

        public void Init(Combatant3D player, float hp, float speed, float damage, float scale = 1f)
        {
            Player = player;
            MoveSpeed = speed;
            Damage = damage;
            _c = GetComponent<Combatant3D>();
            _cc = GetComponent<CharacterController>();
            _c.Team = Team.Enemy;
            _c.MaxHp = hp;
            _c.Hp = hp;
            _c.MoveSpeed = speed;
            _cc.center = new Vector3(0f, 1f, 0f);
            _cc.height = 2f * scale;
            _cc.radius = 0.42f * scale;
            _c.BuildVisual(
                new Color(0.18f, 0.16f, 0.24f),   // 紫黑身躯
                new Color(0.55f, 0.12f, 0.62f),   // 邪紫点缀
                new Color(0.78f, 0.78f, 0.82f),   // 骨刀
                false);
            _c.Visual.localScale = Vector3.one * scale;
            _weapon = _c.WeaponPivot;
            _st = E.Idle;
            _t = FirstAttackDelay;
        }

        private void Update()
        {
            if (_c == null) return;
            if (_c.Dead) return;
            var dt = Time.deltaTime;

            if (_cc.isGrounded && _vy < 0f) _vy = -2f;
            _vy += _gravity * dt;
            _cc.Move(Vector3.up * (_vy * dt));

            if (_c.KnockVel.sqrMagnitude > 0.01f)
            {
                _cc.Move(_c.KnockVel * dt);
                _c.KnockVel = Vector3.MoveTowards(_c.KnockVel, Vector3.zero, 10f * dt);
            }

            // 受击/桃橛硬直
            if (Time.time < _c.StunUntil)
            {
                _weapon.localRotation = Quaternion.Slerp(_weapon.localRotation, Quaternion.identity, dt * 8f);
                return;
            }
            if (Player == null || Player.Dead) { Idle(dt); return; }

            var to = Player.transform.position - transform.position; to.y = 0f;
            var dist = to.magnitude;
            if (dist > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(to), 1f - Mathf.Exp(-10f * dt));

            switch (_st)
            {
                case E.Idle:
                    _t -= dt;
                    if (_t <= 0f) _st = E.Chase;
                    break;
                case E.Chase:
                    if (dist > AttackRange)
                        _cc.Move(to.normalized * MoveSpeed * dt);
                    else
                    {
                        _st = E.Windup; _t = 0.5f; _judged = false;
                        _c.WarnGlow(true);
                        _weapon.localRotation = Quaternion.Euler(-40f, 0f, 0f);
                    }
                    break;
                case E.Windup:
                    _t -= dt;
                    // 后仰蓄力
                    _weapon.localRotation = Quaternion.Euler(-40f - (0.5f - _t) * 60f, 0f, 0f);
                    if (_t <= 0f) { _st = E.Strike; _t = 0.16f; _c.WarnGlow(false); }
                    break;
                case E.Strike:
                    _t -= dt;
                    var k = 1f - _t / 0.16f;
                    _weapon.localRotation = Quaternion.Euler(Mathf.Lerp(-70f, 30f, k), 0f, 0f);
                    if (!_judged && k > 0.45f)
                    {
                        _judged = true;
                        TryHitPlayer();
                    }
                    if (_t <= 0f) { _st = E.Recover; _t = 0.9f; _weapon.localRotation = Quaternion.identity; }
                    break;
                case E.Recover:
                    _t -= dt;
                    if (_t <= 0f) _st = E.Chase;
                    break;
            }
        }

        private void Idle(float dt)
        {
            if (_weapon != null)
                _weapon.localRotation = Quaternion.Slerp(_weapon.localRotation, Quaternion.identity, dt * 6f);
        }

        private void TryHitPlayer()
        {
            var center = transform.position + Vector3.up * 1.2f + transform.forward * AttackRange * 0.6f;
            var hits = Physics.OverlapBox(center, new Vector3(1.0f, 1.2f, AttackRange * 0.6f), transform.rotation);
            foreach (var col in hits)
            {
                var p = col.GetComponentInParent<Combatant3D>();
                if (p == null || p.Team != Team.Player || p.Dead) continue;
                var info = new DamageInfo
                {
                    Damage = Damage,
                    Direction = (Player.transform.position - transform.position).normalized,
                    Knockback = 4.2f,
                    Point = Player.transform.position + Vector3.up * 1.2f,
                    Crit = false
                };
                if (p.Invulnerable)
                {
                    GameFeel3D.I?.FloatingText(p.transform.position + Vector3.up * 1.8f, "闪避",
                        new Color(0.7f, 0.9f, 1f), 0.9f);
                    GameFeel3D.I?.Rig?.AddShake(0.12f);
                    return;
                }
                if (p.Hurt(info))
                {
                    GameFeel3D.I?.Hit(info.Point, false);
                    GameFeel3D.I?.FloatingText(info.Point, "-" + Mathf.RoundToInt(Damage),
                        new Color(1f, 0.5f, 0.4f), 1.1f);
                    GameFeel3D.I?.Rig?.AddShake(0.4f);
                }
                return;
            }
        }

        public void ResetEnemy(Vector3 pos, Quaternion rot, float hp)
        {
            _c.ResetTo(pos, rot, hp);
            _st = E.Chase;
            _t = 0f;
            _judged = false;
            if (_weapon != null) _weapon.localRotation = Quaternion.identity;
        }
    }
}
