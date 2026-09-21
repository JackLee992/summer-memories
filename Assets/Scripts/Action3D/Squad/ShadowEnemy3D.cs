using System.Collections.Generic;
using UnityEngine;

namespace SummerMemories.Action3D.Squad
{
    public class ShadowEnemy3D : MonoBehaviour
    {
        public EnemyConfig Config { get; private set; }
        public float Hp { get; private set; }
        public float Posture { get; private set; }
        public bool Alive => Hp > 0;
        public bool Winding => _state == 1;
        public bool Bound => _stun > 0;
        public bool Recovering => _state == 3;
        public bool Exposed => Bound || Recovering;
        public bool LinkReady => _binder != null && _bindTime > 0;
        public float WindupRemaining => Winding ? _timer : 10;
        public float AttackRange => Config.attackStyle == "sweep" ? 3.15f : Config.attackStyle == "rush" ? 5.2f : 2.65f;
        public SquadActor3D Target { get; private set; }
        private CharacterController _cc;
        private SquadBody _body;
        private SquadSession3D _session;
        private int _state;
        private float _timer, _stun, _bindTime, _damageFlash, _poseTime, _movement;
        private SquadActor3D _binder;
        private bool _broken;
        private readonly HashSet<SquadActor3D> _hit = new HashSet<SquadActor3D>();
        private LineRenderer _tell, _lock;
        private Material _tellMaterial;
        public void Init(SquadSession3D session, EnemyConfig config)
        {
            _session = session; Config = config; Hp = config.hp; Posture = config.posture; gameObject.layer = 8;
            _cc = gameObject.AddComponent<CharacterController>(); _cc.height = 1.8f; _cc.radius = .3f; _cc.center = Vector3.up * .9f; _cc.stepOffset = .2f;
            _body = new SquadBody(transform, false, true, false, session.Config.presentation);
            if (config.attackStyle == "guard") _body.Root.localScale = new Vector3(1.2f, 1.12f, 1.2f);
            _cc.enabled = false; transform.position = SquadConfig.Position(config.spawn); _cc.enabled = true;
            _tellMaterial = new Material(Resources.Load<Shader>("Shaders/SquadTrail"));
            _tell = Line("CommittedAttack", .055f); _lock = Line("TargetRing", .028f);
        }
        private LineRenderer Line(string name, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(transform, false);
            line.sharedMaterial = _tellMaterial; line.startWidth = line.endWidth = width; line.useWorldSpace = false;
            line.enabled = false; return line;
        }
        public bool Threatens(SquadActor3D actor)
        {
            var d = actor.transform.position - transform.position; d.y = 0;
            if (d.magnitude > AttackRange + .3f) return false;
            var forward = Vector3.Dot(transform.forward, d.normalized);
            if (Config.attackStyle == "rush") return forward > .5f && Mathf.Abs(Vector3.Dot(transform.right, d)) < 1.0f;
            return forward > (Config.attackStyle == "sweep" ? -.2f : .35f);
        }
        public void Tick(float dt)
        {
            if (!Alive) return;
            _poseTime += dt; _bindTime = Mathf.Max(0, _bindTime - dt); _damageFlash = Mathf.Max(0, _damageFlash - dt);
            if (Target != null && (!Target.Alive || Target.Hidden)) Target = null;
            _cc.Move(Vector3.down * 6 * dt); _movement = 0;
            if (_stun > 0)
            {
                _stun = Mathf.Max(0, _stun - dt);
                if (_stun == 0 && _broken) { Posture = Config.posture; _state = 3; _timer = .45f; _broken=false; }
                _body.Pose(_poseTime, 0, ActorAction.Hurt, 0); UpdateTell(); return;
            }
            _timer -= dt;
            if (_state == 1)
            {
                if (_timer <= 0) { _state = 2; _timer = Config.attackStyle == "rush" ? .34f : .18f; _hit.Clear(); }
            }
            else if (_state == 2)
            {
                if (Config.attackStyle == "rush") _cc.Move(transform.forward * 10 * dt);
                foreach (var a in _session.Party)
                {
                    if (_hit.Contains(a) || !a.Alive || a.Hidden || !Threatens(a)) continue;
                    var distance = Vector3.Distance(a.transform.position, transform.position);
                    if (Config.attackStyle == "rush" && distance > 1.75f) continue;
                    if (Config.attackStyle == "sweep" && a.transform.position.y - transform.position.y > .55f) continue;
                    if (!_session.World.Visible(transform.position, a.transform.position)) continue;
                    _hit.Add(a); a.ReceiveEnemyHit(Config.damage);
                }
                if (_timer <= 0) { _state = 3; _timer = Config.recovery; }
            }
            else if (_state == 3) { if (_timer <= 0) _state = 0; }
            else
            {
                AcquireTarget();
                if (Target != null)
                {
                    var d = Target.transform.position - transform.position; d.y = 0;
                    var distance = d.magnitude;
                    if (distance > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 1 - Mathf.Exp(-10 * dt));
                    var trigger = Config.attackStyle == "rush" ? 4.6f : Config.attackStyle == "sweep" ? 2.7f : 2.2f;
                    if (distance < trigger && _session.CanEnemyWindup(this))
                    { _state = 1; _timer = Config.windup; transform.rotation = Quaternion.LookRotation(d.normalized); }
                    else if (distance > trigger * .85f)
                    {
                        var separation = Vector3.zero;
                        foreach (var e in _session.Enemies)
                            if (e != this && e.Alive) { var away = transform.position - e.transform.position; away.y = 0; if (away.sqrMagnitude < 2.25f) separation += away.normalized * .8f; }
                        var move = (d.normalized + separation).normalized;
                        if (Physics.SphereCast(transform.position + Vector3.up * .4f, .3f, move, out _, .8f, 1)) move = Quaternion.Euler(0, 70, 0) * move;
                        _cc.Move(move * Config.speed * dt); _movement = .8f;
                    }
                }
                Posture = Mathf.Min(Config.posture, Posture + dt * .5f);
            }
            var progress = _state == 1 ? (1 - _timer / Config.windup) * .36f : _state == 2 ? .5f : _state == 3 ? Mathf.Lerp(.7f, 1, 1 - _timer / Config.recovery) : 0;
            _body.Pose(_poseTime, _movement, _state != 0 ? ActorAction.Attack : ActorAction.Free, progress, Config.attackStyle == "sweep" ? 0 : 2, Config.attackStyle == "guard");
            UpdateTell();
        }
        private void AcquireTarget()
        {
            if (Target != null && Vector3.Distance(Target.transform.position, transform.position) < Config.aggroRange && _session.World.Visible(transform.position, Target.transform.position)) return;
            Target = null; var best = Config.aggroRange;
            foreach (var a in _session.Party)
            {
                var d = Vector3.Distance(a.transform.position, transform.position);
                if (a.Alive && !a.Hidden && d < best && _session.World.Visible(transform.position, a.transform.position)) { best = d; Target = a; }
            }
        }
        // Guarding, posture and the bound follow-up are resolved against the actual attacker.
        public float ReceiveStrike(SquadActor3D owner, float damage, bool heavy, bool hair, out bool linked)
        {
            linked = false; if (!Alive) return 0;
            var flank = Vector3.Dot(transform.forward, (owner.transform.position - transform.position).normalized) < .1f;
            var guarding = Config.attackStyle == "guard" && !Exposed && !flank;
            linked = LinkReady && owner != _binder && !hair;
            if (linked) { damage *= 2; _bindTime = 0; _binder = null; }
            else if (guarding && !hair) damage *= heavy ? .65f : .3f;
            if (Recovering) damage *= 1.35f;
            var before = Hp; Hp = Mathf.Max(0, Hp - damage); _damageFlash = .12f;
            Posture = Mathf.Max(0, Posture - (hair ? Config.posture : heavy ? 5 : flank ? 2 : 1));
            if (hair) { _binder = owner; _bindTime = 2.6f; }
            var pressure = owner.IsHizuru && owner.Ryunosuke && owner.Action == ActorAction.Hair;
            if (hair || pressure || Posture <= 0) { _state = 0; _timer = 0; _stun = hair ? 2.6f : 1.6f; _broken=true; }
            else if (_state == 0 && Config.attackStyle != "guard") _stun = .12f;
            if (!Alive)
            {
                _cc.enabled = false; _body.Pose(_poseTime, 0, ActorAction.Dead, 0); Target = null;
                _tell.enabled = false; _lock.enabled = false;
            }
            UpdateTell(); return before - Hp;
        }
        private void UpdateTell()
        {
            var color = _damageFlash > 0 ? new Color(.75f, .8f, .88f) : LinkReady ? new Color(.18f, .56f, .63f) : new Color(.06f, .07f, .11f);
            _body.Cloth.color = color;
            _tell.enabled = Alive && (_state == 1 || _state == 2);
            if (_tell.enabled)
            {
                var c = Config.attackStyle == "sweep" ? new Color(1, .66f, .19f, .9f) : new Color(1, .22f, .24f, .9f);
                _tell.startColor = _tell.endColor = c; _tell.loop = true;
                if (Config.attackStyle == "rush")
                {
                    _tell.positionCount = 4;
                    _tell.SetPositions(new[] { new Vector3(-.85f,.035f,0), new Vector3(-.85f,.035f,5.2f), new Vector3(.85f,.035f,5.2f), new Vector3(.85f,.035f,0) });
                }
                else
                {
                    var angle = Config.attackStyle == "sweep" ? 100 : 68;
                    _tell.positionCount = 34; _tell.SetPosition(0, Vector3.up * .035f);
                    for (var i = 1; i < 34; i++) _tell.SetPosition(i, Vector3.up * .035f + Quaternion.Euler(0, Mathf.Lerp(-angle, angle, (i-1)/32f), 0) * Vector3.forward * AttackRange);
                }
                _tell.startWidth = _tell.endWidth = _state == 2 ? .13f : Mathf.Lerp(.025f, .09f, 1 - _timer / Config.windup);
            }
            _lock.enabled = Alive && (_session.Locked == this || Bound);
            if (_lock.enabled)
            {
                _lock.loop = true; _lock.positionCount = 40;
                _lock.startColor = _lock.endColor = Bound ? new Color(.3f,1,.93f,.9f) : new Color(1,.85f,.48f,.9f);
                for (var i=0;i<40;i++) _lock.SetPosition(i, Quaternion.Euler(0,i*9,0)*Vector3.forward*.65f+Vector3.up*.03f);
            }
        }
        private void OnDestroy() { _body?.Dispose(); if (_tellMaterial != null) Destroy(_tellMaterial); }
    }
}
