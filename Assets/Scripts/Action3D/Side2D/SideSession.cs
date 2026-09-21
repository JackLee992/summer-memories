using System;
using System.Collections.Generic;
using SummerMemories.Action3D.Squad;
using UnityEngine;
namespace SummerMemories.Action3D.Side2D
{
    [Serializable] public class SideSave
    {
        public int version=1,active,loops;public float anchor=3;public bool completed;
        public List<string> memory=new List<string>(),scans=new List<string>(),flags=new List<string>();
        public OverlookArchive archive=new OverlookArchive();
    }
    public sealed class SideSession
    {
        public SideConfig Config {get;}
        public readonly List<SideActor> Party=new List<SideActor>();
        public readonly List<SideEnemy> Enemies=new List<SideEnemy>();
        public readonly HashSet<string> Flags=new HashSet<string>(),Memory=new HashSet<string>(),Scans=new HashSet<string>();
        public int ActiveIndex,Loops;public float Anchor=3,RescueTime=-1;
        public SideActor Active=>Party[ActiveIndex];
        public OverlookArchive Archive=new OverlookArchive();
        public SidePhase Phase=SidePhase.Play;
        public bool Paused,WatchCarried;
        public string Notice="",Dialogue="";
        public SideObject Context {get;private set;}
        public event Action Changed;
        public event Action<string> Sound;
        public event Action<Vector2,float> Hit;
        public SideSession(SideConfig config){Config=config;NewGame();}
        public void NewGame()
        {
            Flags.Clear();Memory.Clear();Scans.Clear();Archive=new OverlookArchive();Anchor=3;Loops=0;
            Archive.Begin("s2.anchor.arrival");Reset();Dialogue="s2.story.arrival";Record("begin","s2.anchor.arrival");
        }
        private void Reset()
        {
            Party.Clear();for(var i=0;i<Config.characters.Length;i++){var c=Config.characters[i];Party.Add(new SideActor{id=c.id,hp=c.hp,position=new Vector2(Anchor,0),pipe=Flags.Contains("pipe")});}
            ActiveIndex=0;WatchCarried=false;Enemies.Clear();
            foreach(var e in Config.enemies)
            {if(e.x<Anchor-3)continue;Enemies.Add(new SideEnemy{data=e,x=e.x,hp=e.hp});}
            RescueTime=-1;Phase=SidePhase.Play;Paused=false;Dialogue="";
            if(Anchor<25)Flags.Remove("tunnel_open");if(Anchor<53){Flags.Remove("battle_clear");Flags.Remove("alarm");}
            Flags.Remove("rescue_started");Flags.Remove("core_broken");Flags.Remove("mio_safe");
        }
        public void DismissDialogue(){Dialogue="";Changed?.Invoke();}
        public void Step(float dt,SideInput input)
        {
            if(Paused||Phase!=SidePhase.Play||Dialogue!="")return;
            dt=Mathf.Clamp(dt,0,.04f);Archive.Observe(dt,new Vector3(Active.position.x,0,0));
            foreach(var a in Party){a.cooldown=Mathf.Max(0,a.cooldown-dt);a.invulnerable=Mathf.Max(0,a.invulnerable-dt);a.comboTime-=dt;a.stamina=Mathf.Min(100,a.stamina+dt*18);}
            if(input.switchTo>=0)Switch(input.switchTo);
            var p=Active;var c=Config.characters[ActiveIndex];
            if(input.returnHuman)Uncopy();if(input.copy)Copy();
            if(input.consciousness&&ActiveIndex==2&&p.action!=SideAction.Hurt){p.ryunosuke=!p.ryunosuke;Notice=p.ryunosuke?"s2.ryunosuke":"s2.hizuru";Record("consciousness",Notice);}
            if(input.interact)Interact();
            if(Dialogue!="")return;
            if(input.attack)Attack(false,false);else if(input.heavy)Attack(true,false);else if(input.skill)Attack(false,true);
            if(input.dash&&p.stamina>=24&&p.form==SideForm.Human&&IsFree(p))
            {p.stamina-=24;Begin(p,SideAction.Dash,.32f);p.invulnerable=.20f;Sound?.Invoke("dodge");}
            if(input.jump&&p.grounded&&p.form==SideForm.Human&&IsFree(p))
            {p.velocityY=9.5f;p.grounded=false;Sound?.Invoke("jump");}
            float motion=0;
            if(p.action==SideAction.Attack||p.action==SideAction.Heavy||p.action==SideAction.Skill||p.action==SideAction.Dash||p.action==SideAction.Hurt)
            {
                p.actionTime+=dt;
                if(p.action==SideAction.Dash)motion=p.facing*11;
                if(!p.hit&&p.actionTime>=p.actionLength*.45f)
                {p.hit=true;if(p.action==SideAction.Attack||p.action==SideAction.Heavy||p.action==SideAction.Skill)Strike();}
                if(p.actionTime>=p.actionLength)p.action=SideAction.Idle;
            }
            if(IsFree(p))
            {
                motion=input.move*c.speed*(p.form==SideForm.Stone?.65f:1);if(Mathf.Abs(input.move)>.1f)p.facing=input.move>0?1:-1;
                p.action=Mathf.Abs(motion)>.1f?SideAction.Run:SideAction.Idle;
            }
            Move(p,motion*dt,0);p.velocityY-=25*dt;Move(p,0,p.velocityY*dt);
            if(IsFree(p)&&!p.grounded)p.action=p.velocityY>0?SideAction.Jump:SideAction.Fall;
            if(p.position.y<-5){Damage(p,100);}
            foreach(var e in Enemies)TickEnemy(e,dt);
            if(Phase!=SidePhase.Play)return;
            if(Active.position.x>26&&!Flags.Contains("arrival_warehouse"))
            {Flags.Add("arrival_warehouse");Dialogue="s2.story.warehouse";Record("event",Dialogue);}
            if(Active.position.x>33&&!(WatchCarried&&ActiveIndex==0)&&!Flags.Contains("alarm")&&!Flags.Contains("battle_clear"))
            {Flags.Add("alarm");Notice="s2.alarm";Memory.Add("alarm");Record("event","s2.alarm");}
            if(Enemies.TrueForAll(e=>!e.Alive||e.x>65)&&Active.position.x>38&&!Flags.Contains("battle_clear"))
            {Flags.Add("battle_clear");Anchor=53;Dialogue="s2.story.battle_clear";Record("checkpoint",Dialogue);Changed?.Invoke();}
            if(Active.position.x>65&&!Flags.Contains("rescue_started"))
            {Flags.Add("rescue_started");RescueTime=35;Dialogue=Memory.Contains("collapse")?"s2.story.rescue_known":"s2.story.rescue";Record("event",Dialogue);}
            if(RescueTime>=0&&!Flags.Contains("core_broken"))
            {RescueTime-=dt;if(RescueTime<=0){Memory.Add("collapse");Fail("s2.fail.collapse");}}
            RefreshContext();
        }
        private static bool IsFree(SideActor p)=>p.action==SideAction.Idle||p.action==SideAction.Run||p.action==SideAction.Jump||p.action==SideAction.Fall;
        private static void Begin(SideActor p,SideAction action,float duration){p.action=action;p.actionTime=0;p.actionLength=duration;p.hit=false;}
        public bool Switch(int index)
        {
            if(Phase!=SidePhase.Play||Paused||Dialogue!=""||index<0||index>=Party.Count||index==ActiveIndex||Party[index].hp<=0||!IsFree(Active))return false;
            if(!Uncopy())return false;
            var position=Active.position;var facing=Active.facing;ActiveIndex=index;Active.position=position;Active.facing=facing;Active.velocityY=0;Active.action=SideAction.Idle;Active.invulnerable=.15f;
            if(index==1)WatchCarried=false;Record("switch",Config.characters[index].nameKey);return true;
        }
        public bool Attack(bool heavy,bool skill)
        {
            var p=Active;if(Phase!=SidePhase.Play||Paused||Dialogue!=""||!IsFree(p)||p.form!=SideForm.Human)return false;
            if(skill)
            {
                if(p.cooldown>0){Notice="s2.cooldown";return false;}
                if(ActiveIndex==0)
                {
                    if(WatchCarried){p.cooldown=5;Begin(p,SideAction.Skill,.65f);return true;}
                    Notice="s2.shinpei.skill";return false;
                }
                p.cooldown=ActiveIndex==1?5:6;
            }
            if(heavy&&p.stamina<28){Notice="s2.tired";return false;}
            if(heavy)p.stamina-=28;
            p.combo=p.comboTime>0?(p.combo+1)%3:0;p.comboTime=1.3f;
            Begin(p,skill?SideAction.Skill:heavy?SideAction.Heavy:SideAction.Attack,skill?.65f:heavy?.8f:.40f);return true;
        }
        private void Strike()
        {
            var p=Active;var c=Config.characters[ActiveIndex];var skill=p.action==SideAction.Skill;var heavy=p.action==SideAction.Heavy;
            var hair=skill&&(ActiveIndex==1||WatchCarried&&ActiveIndex==0);var reach=hair?6:c.reach+(heavy?.4f:0);
            foreach(var e in Enemies)
            {
                if(!e.Alive||Mathf.Abs(e.x-p.position.x)>reach||(e.x-p.position.x)*p.facing<-.2f||p.position.y>1.6f)continue;
                var damage=c.damage*(heavy?1.7f:1)*(p.combo==2?1.3f:1);if(ActiveIndex==0&&!p.pipe)damage*=.6f;
                if(e.data.style=="guard"&&e.bound<=0&&!heavy&&(p.position.x-e.x)*e.facing>0)damage*=.25f;
                if(e.linkActor>=0&&e.linkActor!=ActiveIndex){damage*=2;e.linkActor=-1;Notice="s2.team.followup";}
                if(hair){e.bound=2.5f;e.linkActor=ActiveIndex;damage=2;}
                else if(heavy||skill){e.bound=skill&&p.ryunosuke?1.8f:.55f;e.committed=false;}
                e.hp=Mathf.Max(0,e.hp-damage);e.recovery=Mathf.Max(e.recovery,.25f);Hit?.Invoke(new Vector2(e.x,1),damage);Sound?.Invoke(hair?"hair":"hit");
                if(!e.Alive)Record("enemy",e.data.nameKey);
            }
            if(Flags.Contains("rescue_started")&&!Flags.Contains("core_broken")&&Mathf.Abs(p.position.x-73)<3.3f)
            {
                if(ActiveIndex==2&&heavy){Flags.Add("core_broken");Memory.Add("core");Notice="s2.core.broken";Record("event",Notice);Sound?.Invoke("hit");Changed?.Invoke();}
                else Notice="s2.core.hint";
            }
        }
        private void TickEnemy(SideEnemy e,float dt)
        {
            if(!e.Alive)return;e.bound=Mathf.Max(0,e.bound-dt);if(e.bound>0)return;e.linkActor=-1;
            e.recovery=Mathf.Max(0,e.recovery-dt);if(e.recovery>0)return;
            var p=Active;var dist=p.position.x-e.x;
            if(Mathf.Abs(dist)>13||WatchCarried&&ActiveIndex==0&&!Flags.Contains("alarm")&&e.data.style=="sentry")return;
            if(e.committed)
            {
                e.windup-=dt;
                if(e.windup<=0)
                {
                    var inRange=Mathf.Abs(dist)<e.data.reach&&(dist*e.facing>-.3f);
                    var jumpSafe=e.data.style=="sweep"&&p.position.y>.7f;
                    if(inRange&&!jumpSafe)Damage(p,e.data.damage*(Flags.Contains("alarm")?1.2f:1));
                    e.committed=false;e.recovery=1.15f;
                }
            }
            else if(Mathf.Abs(dist)<e.data.reach+.1f)
            {e.facing=dist>0?1:-1;e.committed=true;e.windup=e.data.windup;}
            else
            {e.facing=dist>0?1:-1;e.x+=e.facing*dt*(e.data.style=="guard"?1.2f:2.1f);}
        }
        public void Damage(SideActor p,float amount)
        {
            if(p.invulnerable>0)return;p.hp=Mathf.Max(0,p.hp-amount);p.invulnerable=.6f;Begin(p,SideAction.Hurt,.30f);Sound?.Invoke("hit");
            if(p.hp<=0)
            {
                p.action=SideAction.Dead;
                if(p==Party[0])Fail("s2.fail.shinpei");
                else {ActiveIndex=0;Party[0].position=p.position;Party[0].invulnerable=.8f;Notice="s2.ally.down";}
            }
        }
        public void Fail(string reason){Phase=SidePhase.Defeat;Notice=reason;Archive.Current.outcome="st.timeline.defeat";Record("defeat",reason);Changed?.Invoke();}
        public void Rewind()
        {
            var previous=Archive.Current;if(previous.outcome=="st.timeline.active")previous.outcome="st.timeline.rewound";
            Archive.Begin(Anchor>25?"s2.anchor.warehouse":"s2.anchor.arrival",previous.id,previous.elapsed);Loops++;Reset();Dialogue="s2.story.rewind";Record("rewind",Dialogue);Sound?.Invoke("rewind");Changed?.Invoke();
        }
        public bool Copy()
        {
            if(Phase!=SidePhase.Play||Paused||Dialogue!=""||ActiveIndex!=1||!IsFree(Active)){Notice="s2.copy.ushio";return false;}
            if(Active.form==SideForm.Stone)return Uncopy();
            if(Scans.Contains("stone")){Active.form=SideForm.Stone;Notice="s2.copy.stone";Record("copy",Notice);return true;}
            Notice="s2.copy.scan_first";return false;
        }
        public bool CarryWatch()
        {
            if(Phase!=SidePhase.Play||Paused||Dialogue!=""||ActiveIndex!=1||!Scans.Contains("watch")||!Uncopy())return false;
            var p=Active.position;WatchCarried=true;ActiveIndex=0;Active.position=p;Active.action=SideAction.Idle;Notice="s2.copy.watch";Record("copy",Notice);return true;
        }
        public bool Uncopy()
        {
            if(Active.form!=SideForm.Stone)return true;
            if(Blocked(Active.position,.48f,1.8f)){Notice="s2.copy.ceiling";return false;}
            Active.form=SideForm.Human;return true;
        }
        public void RefreshContext()
        {
            Context=null;var distance=1.8f;
            foreach(var o in Config.objects)
            {
                if(o.sets!=null&&Flags.Contains(o.sets))continue;
                if(!string.IsNullOrEmpty(o.requires)&&!Flags.Contains(o.requires))continue;
                var d=Mathf.Abs(o.x-Active.position.x)+Mathf.Abs(o.y-Active.position.y);
                if(d<distance){distance=d;Context=o;}
            }
        }
        public void Interact()
        {
            if(Phase!=SidePhase.Play||Paused||Dialogue!="")return;
            RefreshContext();var o=Context;if(o==null)return;
            if(o.type=="scan")
            {if(ActiveIndex!=1){Notice="s2.copy.ushio";return;}Scans.Add(o.id);}
            if(o.type=="core"){Notice="s2.core.hint";return;}
            if(o.type=="exit")
            {
                if(!Flags.Contains("core_broken")){Notice="s2.core.hint";return;}
                Flags.Add("mio_safe");Phase=SidePhase.Victory;Archive.Current.outcome="st.timeline.victory";
            }
            if(o.type=="pipe")foreach(var a in Party)a.pipe=true;
            if(!string.IsNullOrEmpty(o.sets))Flags.Add(o.sets);
            Memory.Add(o.id);Notice=o.detailKey;Record(o.type,o.detailKey);
            if(o.type=="clue")Dialogue=o.detailKey;Sound?.Invoke(o.id=="shell"?"shell":"pickup");Changed?.Invoke();
        }
        private bool SolidEnabled(SideSolid b)=>string.IsNullOrEmpty(b.opensWith)||!Flags.Contains(b.opensWith);
        private bool Blocked(Vector2 p,float width,float height)
        {
            foreach(var b in Config.solids)if(SolidEnabled(b)&&p.x+width/2>b.x-b.width/2+.01f&&p.x-width/2<b.x+b.width/2-.01f&&p.y+height>b.y-b.height/2+.01f&&p.y<b.y+b.height/2-.01f)return true;
            return false;
        }
        private void Move(SideActor p,float dx,float dy)
        {
            var width=p.form==SideForm.Stone?.38f:.48f;var height=p.form==SideForm.Stone?.35f:1.8f;
            var steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(Mathf.Abs(dx),Mathf.Abs(dy))/.08f));
            for(var i=0;i<steps;i++)
            {
                var next=p.position+new Vector2(dx/steps,dy/steps);
                if(Blocked(next,width,height))
                {if(dy<0){p.grounded=true;p.velocityY=0;}else if(dy>0)p.velocityY=0;return;}
                p.position=next;if(dy!=0)p.grounded=false;
            }
            p.position=new Vector2(Mathf.Clamp(p.position.x,.5f,Config.length-.5f),p.position.y);
        }
        public void Record(string kind,string key)
        {
            var p=Active;var e=new TimelineEvent{time=Archive.Current.elapsed,kind=kind,detailKey=key,actorId=p.id,position=new Vector3(p.position.x,0,0)};
            foreach(var a in Party)e.party.Add(new ActorSnapshot{id=a.id,hp=a.hp,position=new Vector3(a.position.x,a.position.y,0),ryunosuke=a.ryunosuke});
            Archive.Current.events.Add(e);
        }
        public SideSave Save()=>new SideSave{active=ActiveIndex,anchor=Anchor,loops=Loops,completed=Phase==SidePhase.Victory,flags=new List<string>(Flags),memory=new List<string>(Memory),scans=new List<string>(Scans),archive=Archive};
        public void Load(SideSave s)
        {
            if(s==null||s.version!=1||s.archive==null||s.archive.branches==null||s.archive.Current==null||s.flags==null||s.memory==null||s.scans==null||float.IsNaN(s.anchor)||s.anchor<0||s.anchor>Config.length)throw new ArgumentException("Invalid side save");
            Flags.Clear();Flags.UnionWith(s.flags);Memory.Clear();Memory.UnionWith(s.memory);Scans.Clear();Scans.UnionWith(s.scans);Anchor=s.anchor;Loops=s.loops;Archive=s.archive;
            if(!s.completed){var prev=Archive.Current;Archive.Begin(Anchor>25?"s2.anchor.warehouse":"s2.anchor.arrival",prev.id,prev.elapsed);}
            Reset();if(s.completed)Phase=SidePhase.Victory;
        }
    }
}
