using System.Collections.Generic;
using UnityEngine;

namespace SummerMemories.Action3D.Squad
{
    public class SquadActor3D : MonoBehaviour
    {
        public PartyConfig Config { get; private set; }
        public CharacterController Controller { get; private set; }
        public float Hp { get; private set; }
        public float Stamina { get; private set; } = 100;
        public float HairCooldown { get; private set; }
        public float MorphCooldown { get; private set; }
        public bool HasPipe { get; private set; }
        public bool PipeEquipped { get; private set; }
        public int Combo { get; private set; }
        public bool CanYield => Free || ((Action==ActorAction.Attack || Action==ActorAction.Hair) && _hit);
        public bool CounterReady => _counterTime>0;
        public bool Evading => Action==ActorAction.Dodge && _actionTime<.24f;
        public bool Airborne => !Grounded;
        public string WeaponKey => PipeEquipped ? "st.weapon.pipe" : IsUshio ? "st.weapon.hair" : IsHizuru ? "st.weapon.hammer" : "st.weapon.fists";
        private float _verticalSpeed,_comboTime,_bufferTime,_poseTime,_counterTime;
        private bool _counterStrike,_perfectAwarded;
        private bool _heavy,_bufferHeavy;
        private bool Grounded => Controller.enabled && (Controller.isGrounded || Physics.CheckSphere(transform.position+Vector3.up*.055f,.10f,1,QueryTriggerInteraction.Ignore));
        public ActorAction Action { get; private set; }
        public FormConfig Form { get; private set; }
        public readonly HashSet<string> Scans = new HashSet<string>();
        public bool IsHizuru => Config.id == "st_hizuru";
        public bool Ryunosuke { get; private set; }
        public bool IsUshio => Config.id == "st_ushio";
        public bool Alive => Hp > 0;
        public bool Free => Alive && Action == ActorAction.Free;
        public bool Carried => Form != null && !string.IsNullOrEmpty(Form.attachToId);
        public bool Hidden => Alive && Form != null && (Carried || _motion < .05f);
        public int DamageRevision { get; private set; }
        private SquadSession3D _session;
        private SquadBody _body;
        private GameObject _formVisual;
        private float _actionTime, _actionLength, _motion, _morphTime;
        private bool _hit;
        private Vector3 _dodgeDirection;
        public void Init(SquadSession3D session, PartyConfig config)
        {
            _session=session; Config=config; Hp=config.hp; gameObject.layer=8;
            Controller=gameObject.AddComponent<CharacterController>();
            Controller.radius=.28f; Controller.height=1.8f; Controller.center=Vector3.up*.9f;
            Controller.skinWidth=.025f; Controller.stepOffset=.22f; Controller.minMoveDistance=0;
            _body=new SquadBody(transform,IsUshio,false,IsHizuru);
            Teleport(SquadConfig.Position(config.spawn),Quaternion.identity);
        }
        public void Tick(float dt)
        {
            _poseTime+=dt;_counterTime=Mathf.Max(0,_counterTime-dt);
            if(Carried) { var carrier=_session.Party.Find(a=>a.Config.id==Form.attachToId);transform.position=carrier.transform.position+carrier.transform.right*.34f+Vector3.up*1.04f;transform.rotation=carrier.transform.rotation; }
            HairCooldown=Mathf.Max(0,HairCooldown-dt); MorphCooldown=Mathf.Max(0,MorphCooldown-dt);
            _motion=Mathf.MoveTowards(_motion,0,dt*6);
            _comboTime-=dt;_bufferTime-=dt;
            if(Action!=ActorAction.Free && Action!=ActorAction.Dead)
            {
                _actionTime+=dt;
                if(Action==ActorAction.Dodge) MoveRaw(_dodgeDirection*7,dt);
                if(Action==ActorAction.Attack && _actionTime>_actionLength*.2f && _actionTime<_actionLength*.46f)
                    MoveRaw(transform.forward*(_heavy?2.1f:1.5f),dt);
                if(!_hit && _actionTime>=_actionLength*.42f)
                {
                    _hit=true;
                    if(Action==ActorAction.Attack) _session.Strike(this,PipeEquipped||IsHizuru ? 2.7f:2.2f,(PipeEquipped?2.6f:Config.attack)*(Ryunosuke?1.5f:1)*(_heavy?2.2f:Combo==2?1.4f:1)*(_counterStrike?1.6f:1),_heavy);
                    if(Action==ActorAction.Hair) _session.Strike(this,IsUshio ? _session.Config.Ability("st_hair").rangeMeters : 3.5f,IsUshio ? 3 : 5,true,IsUshio);
                }
                if(_actionTime>=_actionLength) { Action=ActorAction.Free;if(_bufferTime>0){_bufferTime=0;Attack(false,_bufferHeavy);} }
            }
            if(Form!=null && !Carried)
            {
                _morphTime-=dt;
                if(_morphTime<=0 && CanStand()) Unmorph();
            }
            if(Alive && !Carried)
            {
                if(Grounded && _verticalSpeed<0)_verticalSpeed=-2;
                _verticalSpeed-=18*dt;
                var flags=Controller.Move(Vector3.up*_verticalSpeed*dt);
                if((flags&CollisionFlags.Above)!=0 && _verticalSpeed>0)_verticalSpeed=0;
            }
            if(_motion<.05f && Action==ActorAction.Free) Stamina=Mathf.Min(100,Stamina+22*dt);
            _body.Pose(_poseTime,_motion,Action,_actionLength>0 ? _actionTime/_actionLength : 0,Combo,_heavy,Airborne);
        }
        public void Move(Vector3 direction,bool sprint,float dt)
        {
            if(!Free || Carried) return;
            direction.y=0; direction=Vector3.ClampMagnitude(direction,1);
            var speed=Form==null ? Config.speed : Form.moveSpeed;
            if(sprint && Form==null && direction.sqrMagnitude>.1f && Stamina>0) {speed*=1.5f;Stamina=Mathf.Max(0,Stamina-24*dt);}
            else Stamina=Mathf.Min(100,Stamina+14*dt);
            MoveRaw(direction*speed,dt);
            _motion=direction.magnitude;
            if(direction.sqrMagnitude>.01f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-15*dt));
        }
        private void MoveRaw(Vector3 velocity,float dt) { if(Controller.enabled) Controller.Move(velocity*dt); }
        private void StartAction(ActorAction action,float length)
        { Action=action; _actionLength=length; _actionTime=0; _hit=false; }
        public bool Attack(bool hair=false,bool heavy=false)
        {
            if(!hair && Action==ActorAction.Attack)
            { _bufferTime=_actionLength-_actionTime+.08f;_bufferHeavy=heavy;return true; }
            if(!Free || Form!=null || (hair && (!(IsUshio || (IsHizuru && Ryunosuke)) || HairCooldown>0))) return false;
            if(heavy && Stamina<22)return false;
            _heavy=heavy;if(heavy)Stamina-=22;
            _counterStrike=!hair && CounterReady;if(_counterStrike)_counterTime=0;
            Combo=_comboTime>0?(Combo+1)%3:0;_comboTime=1.2f;
            if(hair) HairCooldown=_session.Config.Ability(IsUshio ? "st_hair" : "st_foresee").cooldownSeconds;
            StartAction(hair ? ActorAction.Hair : ActorAction.Attack,hair ? .62f : heavy ? .82f : Combo==2?.54f:.42f);
            return true;
        }
        public bool Jump()
        {
            if(!Free||Form!=null||!Grounded)return false;
            _verticalSpeed=6.8f;_session.PlaySound("jump");return true;
        }
        public void GrantPipe(){HasPipe=true;PipeEquipped=true;_body.Equip(true);}
        public void ToggleWeapon(){if(Config.id!="st_shinpei"||!Free||!HasPipe)return;PipeEquipped=!PipeEquipped;_body.Equip(PipeEquipped);}
        public bool ToggleConsciousness()
        { if(!IsHizuru || !Free) return false; Ryunosuke=!Ryunosuke;return true; }
        public bool Dodge(Vector3 direction)
        {
            if((!Free && !(Action==ActorAction.Attack && _actionTime>=_actionLength*.42f)) || Form!=null || Stamina<28) return false;
            _bufferTime=0;
            _perfectAwarded=false;
            Stamina-=28; _session.PlaySound("dodge"); _dodgeDirection=direction.sqrMagnitude>.01f ? direction.normalized : -transform.forward;
            StartAction(ActorAction.Dodge,.4f); return true;
        }
        public void Damage(float value)
        {
            if(!Alive || Evading) return;
            Hp=Mathf.Max(0,Hp-value); DamageRevision++;
            if(Form!=null && CanStand()) Unmorph();
            StartAction(Alive ? ActorAction.Hurt : ActorAction.Dead,.32f);
            if(!Alive)_session.RecordDeath(this);
            _session.Notify("st.feedback.hit");
        }
        public void ReceiveEnemyHit(float value)
        {
            if(Evading)
            {
                if(!_perfectAwarded && _actionTime<.16f)
                { _perfectAwarded=true;_counterTime=2.5f;Stamina=Mathf.Min(100,Stamina+18);if(this==_session.Active)_session.PerfectDodge(); }
                return;
            }
            Damage(value);
        }
        public bool Morph(FormConfig form)
        {
            if(!IsUshio || !Free || Form!=null || MorphCooldown>0 || !Scans.Contains(form.id)) return false;
            var height=form.sizeMeters[1]; var radius=Mathf.Min(form.sizeMeters[0],form.sizeMeters[2])*.5f;
            if(string.IsNullOrEmpty(form.attachToId) && Physics.CheckCapsule(transform.position+Vector3.up*(radius+.04f),transform.position+Vector3.up*Mathf.Max(radius+.04f,height-radius),radius,1,QueryTriggerInteraction.Ignore)) return false;
            Form=form; _morphTime=_session.Config.Ability("st_morph").durationSeconds;
            Controller.stepOffset=.08f; Controller.height=height; Controller.radius=Mathf.Min(radius,height*.5f); Controller.center=Vector3.up*(height*.5f);
            _body.Root.gameObject.SetActive(false);
            _formVisual=SquadVisual.Shape(form.id,transform,form.id.EndsWith("stone")?PrimitiveType.Sphere:PrimitiveType.Cube,
                Vector3.up*(height*.5f),SquadConfig.Position(form.sizeMeters),_body.Hair);
            if(Carried) Controller.enabled=false;
            return true;
        }
        public bool CanStand() => !Physics.CheckCapsule(transform.position+Vector3.up*.32f,transform.position+Vector3.up*1.5f,.28f,1,QueryTriggerInteraction.Ignore);
        public bool Unmorph()
        {
            if(Form==null) return true;
            if(Carried)
            {
                var carrier=_session.Party.Find(a=>a.Config.id==Form.attachToId);
                var found=false;var destination=Vector3.zero;
                foreach(var offset in new[]{Vector3.right,Vector3.left,Vector3.back,Vector3.forward})
                {var p=carrier.transform.position+offset*1.2f;if(!Physics.CheckCapsule(p+Vector3.up*.32f,p+Vector3.up*1.5f,.28f,1,QueryTriggerInteraction.Ignore)){destination=p;found=true;break;}}
                if(!found)return false;transform.position=destination;
            }
            else if(!CanStand()) return false;
            ClearForm();Controller.enabled=true; MorphCooldown=_session.Config.Ability("st_morph").cooldownSeconds; return true;
        }
        private void ClearForm()
        {
            Form=null; if(_formVisual!=null) { _formVisual.SetActive(false);Destroy(_formVisual); }
            _body.Root.gameObject.SetActive(true); Controller.height=1.8f; Controller.radius=.28f; Controller.center=Vector3.up*.9f;Controller.stepOffset=.22f;
        }
        public ActorSnapshot Snapshot() => new ActorSnapshot {id=Config.id,position=transform.position,rotation=transform.rotation,hp=Hp,stamina=Stamina,hairCooldown=HairCooldown,morphCooldown=MorphCooldown,scans=new List<string>(Scans),ryunosuke=Ryunosuke,hasPipe=HasPipe,pipeEquipped=PipeEquipped};
        public void Restore(ActorSnapshot s)
        {
            ClearForm(); Hp=s.hp; Stamina=s.stamina; HairCooldown=s.hairCooldown; MorphCooldown=s.morphCooldown;
            Ryunosuke=s.ryunosuke;HasPipe=s.hasPipe;PipeEquipped=s.pipeEquipped;_body.Equip(PipeEquipped);_verticalSpeed=0;_bufferTime=0;_comboTime=0;_counterTime=0;_counterStrike=false; Scans.Clear(); foreach(var id in s.scans) Scans.Add(id);
            Action=Hp>0?ActorAction.Free:ActorAction.Dead; _actionTime=0;_actionLength=0;_hit=false;_motion=0;
            Teleport(s.position,s.rotation);
        }
        public void Teleport(Vector3 position,Quaternion rotation)
        { Controller.enabled=false; transform.SetPositionAndRotation(position,rotation);Controller.enabled=true; }
        private void OnDestroy() { _body?.Dispose(); }
    }
}
