using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SummerMemories.Action3D.Squad
{
    public class SquadSession3D : MonoBehaviour
    {
        public SquadConfig Config {get;private set;}
        public SquadWorld3D World {get;private set;}
        public readonly List<SquadActor3D> Party=new List<SquadActor3D>();
        public readonly List<ShadowEnemy3D> Enemies=new List<ShadowEnemy3D>();
        public readonly HashSet<string> Collected=new HashSet<string>(), Memories=new HashSet<string>();
        public SquadPhase Phase {get;private set;}=SquadPhase.Title;
        public AllyOrder Order {get;private set;}
        public SquadCheckpoint Checkpoint {get;private set;}
        public OverlookArchive Archive {get;set;}=new OverlookArchive();
        public event Action ArchiveChanged;
        public int ActiveIndex {get;private set;}
        public int LoopCount {get;set;}
        public SquadActor3D Active => Party[ActiveIndex];
        public ShadowEnemy3D Locked {get;private set;}
        public CameraRig3D CameraRig {get;private set;}
        public SquadTactics Tactical {get;private set;}
        public bool Paused {get;set;}
        public bool CanPlay => !Paused && (Phase==SquadPhase.Explore || Phase==SquadPhase.Fight);
        public float ScanProgress => _scanning==null ? 0 : _scanTime/Config.Ability("st_scan").durationSeconds;
        public string ScanningId => _scanning?.id;
        public event Action<string> Feedback, Sound, ClueFound;
        public event Action<Vector3,float,bool> Hit;
        public void PlaySound(string id)=>Sound?.Invoke(id);
        public event Action CheckpointChanged, Rewound, Finished;
        private FormConfig _scanning;
        private Vector3 _scanStart;
        private int _scanDamage;
        private float _scanTime,_rewindTime;
        private LineRenderer _hair;
        private Material _hairMaterial;
        private float _hairTime;
        private float _hitStop;
        public void PerfectDodge(){Notify("st.feedback.perfectDodge");CameraRig.AddShake(.08f);_hitStop=.055f;}
        public bool CanEnemyWindup(ShadowEnemy3D enemy)
        { foreach(var other in Enemies)if(other!=enemy&&other.Alive&&other.Winding)return false;return true; }
        public void Init(SquadConfig config)
        {
            Config=config;config.Validate();
            World=new GameObject("SquadCoast").AddComponent<SquadWorld3D>();World.transform.SetParent(transform,false);World.Build(config);
            foreach(var p in config.party)
            {
                var a=new GameObject(p.id).AddComponent<SquadActor3D>();a.transform.SetParent(transform,false);a.Init(this,p);Party.Add(a);
            }
            ActiveIndex=Party.FindIndex(a=>a.Config.id==config.initialControlledId);
            CameraRig=new GameObject("SquadCamera").AddComponent<CameraRig3D>();CameraRig.transform.SetParent(transform,false);CameraRig.Init(Active.transform);
            CameraRig.Distance=5.6f;CameraRig.Height=1.9f;CameraRig.Pitch=10;CameraRig.Camera.backgroundColor=new Color(.53f,.72f,.75f);CameraRig.Camera.clearFlags=CameraClearFlags.Skybox;CameraRig.InputEnabled=false;CameraRig.Snap();
            if(config.presentation.isometric)
            {
                CameraRig.Isometric=true;CameraRig.Yaw=config.presentation.cameraYaw;CameraRig.Pitch=config.presentation.cameraPitch;CameraRig.OrthographicSize=config.presentation.orthographicSize;CameraRig.Snap();
                Tactical=gameObject.AddComponent<SquadTactics>();Tactical.Init(this);
            }
            _hair=new GameObject("HairArc").AddComponent<LineRenderer>();_hair.transform.SetParent(transform,false);
            _hairMaterial=SquadVisual.Material(new Color(1,.79f,.25f));_hair.sharedMaterial=_hairMaterial;_hair.startWidth=.10f;_hair.endWidth=.045f;_hair.positionCount=12;_hair.enabled=false;
        }
        public void Begin()
        {
            Tactical?.Clear();
            Phase=SquadPhase.Explore;LoopCount=0;Memories.Clear();Collected.Clear();Order=AllyOrder.Follow;
            for(var i=0;i<Party.Count;i++) Party[i].Restore(new ActorSnapshot {id=Party[i].Config.id,position=SquadConfig.Position(Party[i].Config.spawn),rotation=Quaternion.identity,hp=Party[i].Config.hp,stamina=100});
            ActiveIndex=0;ClearEnemies();SpawnIntro();World.RestoreMarkers(Collected);CancelScan();SetTarget();
            Archive=new OverlookArchive();Archive.Begin("st.timeline.exploreAnchor");Record("begin","st.timeline.exploreAnchor");CaptureCheckpoint();
        }
        public void Step(float dt,bool input=true)
        {
            CameraRig.InputEnabled=CanPlay && input;
            if(Tactical!=null && CanPlay)
            {
                if(input)Tactical.ReadInput();
                if(Tactical.Planning)return;
            }
            if(Phase==SquadPhase.Rewinding)
            {
                _rewindTime-=dt;
                if(_rewindTime<=0){Restore(Checkpoint);Record("begin",Archive.Current.anchor);Rewound?.Invoke();}
                return;
            }
            if(!CanPlay)return;
            if(_hitStop>0){if(input)ReadInput(0);_hitStop=Mathf.Max(0,_hitStop-dt);return;}
            Archive.Observe(dt,Active.transform.position);
            _hairTime-=dt;_hair.enabled=_hairTime>0;
            if(Locked!=null && !Locked.Alive) Locked=null;
            CameraRig.LockTarget=Locked!=null?Locked.transform:null;
            if(input) ReadInput(dt);
            if(!CanPlay)return;
            foreach(var actor in Party) actor.Tick(dt);
            if(Tactical!=null)for(var i=0;i<Party.Count;i++)Tactical.TickActor(i,dt);
            TickScan(dt);
            foreach(var actor in Party)
            {
                if(actor==Active || !actor.Alive || actor.Carried || (Tactical!=null && Tactical.HasOrder(Party.IndexOf(actor))))continue;
                if(Order==AllyOrder.Hold)continue;
                if(!actor.Free)continue;
                var danger=Enemies.Find(e=>e.Alive&&e.Winding&&e.WindupRemaining<.25f&&e.Threatens(actor));
                if(danger!=null)
                {var evade=actor.transform.position-danger.transform.position;evade.y=0;actor.Dodge(danger.Config.attackStyle=="rush"?danger.transform.right:evade.normalized);continue;}
                var target=Order==AllyOrder.Focus&&Locked!=null?Locked:NearestEnemy(actor.transform.position,Order==AllyOrder.Focus?18:5);
                if(target!=null && (Order==AllyOrder.Focus || Vector3.Distance(actor.transform.position,target.transform.position)<2.1f))
                {
                    var d=target.transform.position-actor.transform.position;d.y=0;
                    if(Order==AllyOrder.Focus && actor.IsUshio && actor.HairCooldown<=0 && d.magnitude<6 && !target.Bound)
                    {actor.transform.rotation=Quaternion.LookRotation(d.normalized);actor.Attack(true);}
                    else if(d.magnitude<2.05f && (!target.Winding || target.WindupRemaining>.5f))
                    {actor.transform.rotation=Quaternion.LookRotation(d.normalized);actor.Attack(false,actor.IsHizuru&&target.Config.attackStyle=="guard"&&actor.Stamina>45);}
                    else if(!target.Winding || d.magnitude>target.AttackRange+.5f)
                    {var flank=target.transform.position+target.transform.right*(Party.IndexOf(actor)%2==0?-1.55f:1.55f)-actor.transform.position;flank.y=0;MoveAlly(actor,flank.normalized,dt);}
                }
                else
                {
                    var formation=Active.transform.position + Quaternion.Euler(0,CameraRig.Yaw,0)*new Vector3(Party.IndexOf(actor)%2==0 ? -1.8f : 1.8f,0,-1.8f);
                    var d=formation-actor.transform.position;d.y=0;
                    if(d.magnitude>1.3f)MoveAlly(actor,d.normalized,dt);
                }
            }
            foreach(var e in Enemies)e.Tick(dt);
            if(!Party[0].Alive){Phase=SquadPhase.Defeat;CancelScan();Locked=null;Archive.Current.outcome="st.timeline.defeat";Record("defeat",Party[0].Config.nameKey);return;}
            if(!Active.Alive){ActiveIndex=0;SetTarget();}
            if(Phase==SquadPhase.Fight && Enemies.TrueForAll(e=>!e.Alive)) NotifyOnceClear();
        }
        private bool _clearAnnounced;
        private void NotifyOnceClear(){if(_clearAnnounced)return;_clearAnnounced=true;Notify("st.feedback.clear");}
        private void MoveAlly(SquadActor3D actor,Vector3 d,float dt)
        {
            // Local avoidance suffices for the open promenade; Hold is explicit and persistent.
            if(Physics.SphereCast(actor.transform.position+Vector3.up*.36f,.29f,d,out _,1.2f,1))
            {
                var side=Quaternion.Euler(0,75,0)*d;
                if(Physics.SphereCast(actor.transform.position+Vector3.up*.36f,.29f,side,out _,1,1))side=Quaternion.Euler(0,-75,0)*d;
                d=side;
            }
            actor.Move(d,false,dt);
        }
        private void ReadInput(float dt)
        {
            var dir=Quaternion.Euler(0,CameraRig.Yaw,0)*new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"));
            if(dir.sqrMagnitude>.01f)Tactical?.Cancel(ActiveIndex);
            Active.Move(dir,Input.GetKey(KeyCode.LeftShift),dt);
            if(Input.GetKeyDown(KeyCode.Tab))Switch((ActiveIndex+1)%Party.Count);
            for(var i=0;i<Party.Count;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))Switch(i);
            if(Input.GetKeyDown(KeyCode.Alpha4))SetOrder(AllyOrder.Follow);
            if(Input.GetKeyDown(KeyCode.Alpha5))SetOrder(AllyOrder.Hold);
            if(Input.GetKeyDown(KeyCode.Alpha6))SetOrder(AllyOrder.Focus);
            if(Input.GetKeyDown(KeyCode.Q)){Locked=Locked!=null?null:NearestEnemy(Active.transform.position,18);}
            var pointerUI=EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject();
            if(Input.GetKeyDown(KeyCode.J) || (Tactical==null&&Input.GetMouseButtonDown(0)&&!pointerUI))Attack(false);
            if(Input.GetKeyDown(KeyCode.E))Attack(true);
            if(Input.GetKeyDown(KeyCode.K))Attack(false,true);
            if(Input.GetKeyDown(KeyCode.T))Active.ToggleWeapon();
            if(Input.GetKeyDown(Tactical!=null?KeyCode.LeftAlt:KeyCode.Space)){CancelScan();Tactical?.Cancel(ActiveIndex);Active.Jump();}
            if(Input.GetKeyDown(KeyCode.LeftControl)){CancelScan();Tactical?.Cancel(ActiveIndex);Active.Dodge(dir);}
            if(Input.GetKeyDown(KeyCode.F))Interact();
            if(Input.GetKeyDown(KeyCode.Z))Morph("st_object_crate");
            if(Input.GetKeyDown(KeyCode.X))Morph("st_object_stone");
            if(Input.GetKeyDown(KeyCode.C))Morph("st_object_watch");
            if(Input.GetKeyDown(KeyCode.G)&&!Active.Unmorph())Notify("st.feedback.clearance");
            if(Input.GetKeyDown(KeyCode.V))ToggleConsciousness();
            if(Input.GetKeyDown(KeyCode.R))Rewind();
        }
        public bool Switch(int index)
        {
            if(!CanPlay || index<0 || index>=Party.Count || index==ActiveIndex)return false;
            if(!Party[index].Alive || (!Active.CanYield && Active.Alive)){Notify("st.feedback.switchBusy");return false;}
            if(Party[index].Carried && !Party[index].Unmorph()){Notify("st.feedback.clearance");return false;}
            if(!Active.Carried && !Active.Unmorph()){Notify("st.feedback.clearance");return false;}
            CancelScan();ActiveIndex=index;CameraRig.Target=Active.transform;Record("switch",Active.Config.nameKey);return true;
        }
        private void SetTarget(){Locked=null;CameraRig.LockTarget=null;CameraRig.Target=Active.transform;CameraRig.Snap();}
        public void SetOrder(AllyOrder order){if(!CanPlay)return;Tactical?.Clear();Order=order;Notify("st.order."+order.ToString().ToLowerInvariant());Record("order","st.order."+order.ToString().ToLowerInvariant());}
        public void ToggleConsciousness(){if(CanPlay && Active.ToggleConsciousness()){Notify(Active.Ryunosuke?"st.feedback.ryunosuke":"st.feedback.hizuru");Record("consciousness",Active.Ryunosuke?"st.character.ryunosuke":"st.character.hizuru");}}
        public bool Attack(bool special,bool heavy=false)
        {
            if(!CanPlay)return false;
            Tactical?.Cancel(ActiveIndex);CancelScan();var target=Locked!=null?Locked:NearestEnemy(Active.transform.position,special?7:3);
            if(target!=null){var d=target.transform.position-Active.transform.position;d.y=0;if(d.sqrMagnitude>.01f)Active.transform.rotation=Quaternion.LookRotation(d);}
            var result=Active.Attack(special,heavy);
            if(!result && special)Notify("st.feedback.abilityUnavailable");
            return result;
        }
        public void Strike(SquadActor3D owner,float range,float damage,bool bind,bool hairVisual=false)
        {
            if(!hairVisual)SquadAttackTrail.Spawn(owner,range,bind);
            ShadowEnemy3D first=null;
            foreach(var e in Enemies)
            {
                var d=e.transform.position-owner.transform.position;d.y=0;
                if(e.Alive && d.magnitude<=range && Vector3.Dot(owner.transform.forward,d.normalized)>.3f && World.Visible(owner.transform.position,e.transform.position))
                {var dealt=e.ReceiveStrike(owner,damage,bind,hairVisual,out var linked);Hit?.Invoke(e.transform.position+Vector3.up*1.5f,dealt,bind||linked);if(linked)Notify("st.feedback.linked");if(first==null)first=e;}
            }
            if(hairVisual)
            {
                Sound?.Invoke("hair");
                var from=owner.transform.position+Vector3.up*1.6f;
                var to=first!=null?first.transform.position+Vector3.up:from+owner.transform.forward*range;
                for(var i=0;i<12;i++){var t=i/11f;_hair.SetPosition(i,Vector3.Lerp(from,to,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.6f);}
                _hairTime=.28f;
            }
            if(first!=null){CameraRig.AddShake(bind?.2f:.10f);Sound?.Invoke(hairVisual?"hair":"hit");_hitStop=Mathf.Max(_hitStop,bind?.065f:.035f);}
        }
        public ShadowEnemy3D NearestEnemy(Vector3 p,float max)
        {ShadowEnemy3D found=null;foreach(var e in Enemies){var d=Vector3.Distance(p,e.transform.position);if(e.Alive&&d<max){found=e;max=d;}}return found;}
        public string PromptKey {get;private set;}
        public string ContextNameKey {get;private set;}
        public void RefreshContext()
        {
            ContextNameKey="";PromptKey="";
            if(!CanPlay)return;
            foreach(var a in Party)if(!a.Alive&&Vector3.Distance(Active.transform.position,a.transform.position)<2.5f){PromptKey="st.prompt.revive";return;}
            foreach(var c in Config.interactions)if(!Collected.Contains(c.id)&&Vector3.Distance(Active.transform.position,SquadConfig.Position(c.spawn))<2.2f){PromptKey="st.prompt.inspect";ContextNameKey=c.nameKey;return;}
            if(Vector3.Distance(Active.transform.position,SquadConfig.Position(Config.world.encounterGate))<3 && Phase==SquadPhase.Explore){PromptKey="st.prompt.encounter";return;}
            if(Vector3.Distance(Active.transform.position,SquadConfig.Position(Config.world.exit))<3){PromptKey="st.prompt.exit";return;}
            foreach(var f in Config.scanTemplates)if(f.attachToId!=Active.Config.id && Vector3.Distance(Active.transform.position,ScanPosition(f))<Config.Ability("st_scan").rangeMeters){PromptKey=Active.IsUshio?"st.prompt.scan":"st.prompt.needUshio";ContextNameKey=f.nameKey;return;}
        }
        public void Interact()
        {
            if(!CanPlay || !Active.Free)return;
            Tactical?.Cancel(ActiveIndex);
            foreach(var a in Party)if(!a.Alive&&Vector3.Distance(Active.transform.position,a.transform.position)<2.5f)
            {var s=a.Snapshot();s.hp=a.Config.hp*.45f;a.Restore(s);Notify("st.feedback.revived");Record("revive",a.Config.nameKey);return;}
            foreach(var c in Config.interactions)
                if(!Collected.Contains(c.id)&&Vector3.Distance(Active.transform.position,SquadConfig.Position(c.spawn))<2.2f && World.Visible(Active.transform.position,SquadConfig.Position(c.spawn)))
                {Collected.Add(c.id);Memories.Add(c.id);if(c.reward=="pipe")Party[0].GrantPipe();World.RestoreMarkers(Collected);Record("clue",c.nameKey);ClueFound?.Invoke(c.tipId);Notify(c.descriptionKey);Sound?.Invoke(c.id.Contains("shell")?"shell":"shadowReveal");return;}
            if(Vector3.Distance(Active.transform.position,SquadConfig.Position(Config.world.encounterGate))<3 && Phase==SquadPhase.Explore){StartEncounter();return;}
            if(Vector3.Distance(Active.transform.position,SquadConfig.Position(Config.world.exit))<3)
            {
                if(Phase==SquadPhase.Fight&&Enemies.TrueForAll(e=>!e.Alive)){Phase=SquadPhase.Victory;Archive.Current.outcome="st.timeline.victory";Record("victory","st.character.mio");Finished?.Invoke();}
                else Notify("st.feedback.exitLocked");
                return;
            }
            foreach(var f in Config.scanTemplates)
                if(f.attachToId!=Active.Config.id && Vector3.Distance(Active.transform.position,ScanPosition(f))<Config.Ability("st_scan").rangeMeters){BeginScan(f.id);return;}
        }
        public bool BeginScan(string id)
        {
            var f=Config.Form(id);
            if(!CanPlay || !Active.IsUshio || !Active.Free || Active.Form!=null || f==null){Notify("st.feedback.scanOwner");return false;}
            if(Active.Scans.Contains(id)){Notify("st.feedback.scanned");return false;}
            if(!ScanVisible(f))return false;
            _scanning=f;_scanTime=0;_scanStart=Active.transform.position;_scanDamage=Active.DamageRevision;return true;
        }
        private Vector3 ScanPosition(FormConfig f) => string.IsNullOrEmpty(f.attachToId)?SquadConfig.Position(f.spawn):Party.Find(a=>a.Config.id==f.attachToId).transform.position;
        private bool ScanVisible(FormConfig f)
        {
            var p=ScanPosition(f);
            if(Vector3.Distance(p,Active.transform.position)>Config.Ability("st_scan").rangeMeters)return false;
            if(Physics.Linecast(Active.transform.position+Vector3.up*.8f,p+Vector3.up*.2f,out var hit,1,QueryTriggerInteraction.Ignore))return hit.collider.name==f.id;
            return true;
        }
        private void TickScan(float dt)
        {
            if(_scanning==null)return;
            if(!Active.Free || Active.DamageRevision!=_scanDamage || Vector3.Distance(_scanStart,Active.transform.position)>.2f || !ScanVisible(_scanning)){CancelScan();Notify("st.feedback.scanInterrupted");return;}
            _scanTime+=dt;
            if(_scanTime>=Config.Ability("st_scan").durationSeconds){Active.Scans.Add(_scanning.id);Record("scan",_scanning.nameKey);CancelScan();Notify("st.feedback.scanDone");Sound?.Invoke("shell");}
        }
        private void CancelScan(){_scanning=null;_scanTime=0;}
        public bool Morph(string id)
        {
            if(!CanPlay)return false;Tactical?.Cancel(ActiveIndex);CancelScan();var form=Config.Form(id);
            if(form!=null && Active.Morph(form))
            {
                Notify("st.feedback.morph");Record("morph",form.nameKey);
                if(Active.Carried){ActiveIndex=Party.FindIndex(a=>a.Config.id==form.attachToId);SetTarget();}
                return true;
            }
            Notify("st.feedback.morphUnavailable");return false;
        }
        public bool StartEncounter()
        {
            if(!CanPlay || Phase!=SquadPhase.Explore)return false;
            foreach(var clue in Config.interactions)if(clue.requiredForEncounter && !Collected.Contains(clue.id)){Notify("st.feedback.cluesRequired");return false;}
            // A safe regroup point avoids a checkpoint inside a tunnel or with an unrecoverable ally.
            foreach(var a in Party)
            {
                var s=a.Snapshot();s.position=SquadConfig.Position(Config.world.encounterGate)+new Vector3((Party.IndexOf(a)-1)*1.6f,0,-1);
                s.hp=a.Config.hp;s.stamina=100;s.hairCooldown=0;s.morphCooldown=0;a.Restore(s);
            }
            Tactical?.Clear();
            Phase=SquadPhase.Fight;Order=AllyOrder.Follow;CancelScan();SpawnEnemies();Record("encounter","st.timeline.battleAnchor");CaptureCheckpoint();Sound?.Invoke("shadowReveal");Notify("st.feedback.encounter");SetTarget();return true;
        }
        public void CaptureCheckpoint()
        {
            Checkpoint=new SquadCheckpoint{phase=Phase,activeIndex=ActiveIndex,collected=new List<string>(Collected)};
            foreach(var a in Party)Checkpoint.actors.Add(a.Snapshot());CheckpointChanged?.Invoke();
        }
        public void Restore(SquadCheckpoint snapshot)
        {
            Tactical?.Clear();ValidateCheckpoint(snapshot);CancelScan();_hairTime=0;_hitStop=0;_hair.enabled=false;Order=AllyOrder.Follow;
            for(var i=0;i<Party.Count;i++)Party[i].Restore(snapshot.actors[i]);
            ActiveIndex=snapshot.activeIndex;Collected.Clear();foreach(var c in snapshot.collected)Collected.Add(c);
            World.RestoreMarkers(Collected);Phase=snapshot.phase;Checkpoint=snapshot;
            if(Phase==SquadPhase.Fight)SpawnEnemies();else{ClearEnemies();SpawnIntro();}SetTarget();
        }
        public void ValidateCheckpoint(SquadCheckpoint s)
        {
            if(s==null||s.actors==null||s.actors.Count!=Party.Count||s.activeIndex<0||s.activeIndex>=Party.Count||s.collected==null||(s.phase!=SquadPhase.Explore&&s.phase!=SquadPhase.Fight))throw new InvalidOperationException("Invalid squad checkpoint");
            for(var i=0;i<Party.Count;i++)
            {
                var a=s.actors[i];if(a.id!=Party[i].Config.id||a.hp<=0||a.hp>Party[i].Config.hp||a.scans==null||float.IsNaN(a.position.x)||float.IsNaN(a.position.z)||Mathf.Abs(a.position.x)>12||a.position.z<0||a.position.z>Config.world.length)throw new InvalidOperationException("Invalid actor snapshot");
                foreach(var id in a.scans)if(Config.Form(id)==null)throw new InvalidOperationException("Unknown scanned form");
            }
        }
        public void Rewind()
        {
            if(Checkpoint==null||Phase==SquadPhase.Rewinding||Phase==SquadPhase.Title)return;
            Tactical?.Clear();Paused=false;CancelScan();
            var previous=Archive.Current;var forkTime=previous!=null?previous.elapsed:0;
            if(previous!=null){if(previous.outcome=="st.timeline.active")previous.outcome="st.timeline.rewound";Record("rewind",Checkpoint.phase==SquadPhase.Fight?"st.timeline.battleAnchor":"st.timeline.exploreAnchor");}
            Archive.Begin(Checkpoint.phase==SquadPhase.Fight?"st.timeline.battleAnchor":"st.timeline.exploreAnchor",previous!=null?previous.id:-1,forkTime);
            Phase=SquadPhase.Rewinding;LoopCount++;_rewindTime=.75f;Locked=null;CameraRig.LockTarget=null;Sound?.Invoke("rewind");
        }
        private void ClearEnemies(){foreach(var e in Enemies){e.gameObject.SetActive(false);Destroy(e.gameObject);}Enemies.Clear();_clearAnnounced=false;Locked=null;}
        private void SpawnIntro(){if(Config.introEnemies==null)return;foreach(var c in Config.introEnemies){var e=new GameObject(c.id).AddComponent<ShadowEnemy3D>();e.transform.SetParent(transform,false);e.Init(this,c);Enemies.Add(e);}}
        private void SpawnEnemies(){ClearEnemies();foreach(var c in Config.enemies){var e=new GameObject(c.id).AddComponent<ShadowEnemy3D>();e.transform.SetParent(transform,false);e.Init(this,c);Enemies.Add(e);}}
        public void RecordDeath(SquadActor3D actor){Record("down",actor.Config.nameKey);}
        public void Record(string kind,string detail){Archive.Record(kind,detail,Active,Party);ArchiveChanged?.Invoke();}
        public void Notify(string key){Feedback?.Invoke(key);}
        private void OnDestroy(){if(_hairMaterial!=null)Destroy(_hairMaterial);}
    }
}
