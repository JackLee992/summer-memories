using System;
using SummerMemories.Action3D.Squad;
using SummerMemories.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SummerMemories.App
{
    public class SquadHud : MonoBehaviour
    {
        private SquadDemoDirector _app;
        private Transform _root;
        private GameObject _play,_overlay;
        private Text _objective,_prompt,_notice,_combat,_order,_story,_speaker;
        private Text _waypoint,_weapon,_plan;
        private GameObject _planPanel;
        private Text _planToggle;
        private bool Iso=>_app.Session.Tactical!=null;
        private Image _enemyHealth,_enemyPosture;
        private Text[] _actors;
        private Image[] _health;
        private string _screen="";
        private readonly Color _ink=new Color(.025f,.072f,.095f,.92f), _pale=new Color(.86f,.94f,.91f), _gold=new Color(.98f,.76f,.38f), _cyan=new Color(.25f,.8f,.81f);
        private string L(string k)=>_app.Text.Get(k);
        private GameObject Panel(string name,Transform parent,float x,float y,float w,float h,Color c)
        {
            var go=UIFactory.CreatePanel(name,parent,c,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y-h),new Vector2(x+w,-y));
            go.GetComponent<Image>().raycastTarget=false;return go;
        }
        private Text Label(Transform p,string content,int size,float x,float y,float w,float h,Color? c=null)
        {
            var t=UIFactory.CreateText("Text",p,content,size,c??_pale,TextAnchor.UpperLeft);
            var outline=t.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.015f,.045f,.06f,.9f);outline.effectDistance=new Vector2(1,-1);
            var rt=(RectTransform)t.transform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return t;
        }
        private RawImage Picture(Transform p,string key,float x,float y,float w,float h)
        {
            var go=new GameObject("Art",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));go.transform.SetParent(p,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
            var image=go.GetComponent<RawImage>();image.texture=Resources.Load<Texture2D>(key);image.raycastTarget=false;return image;
        }
        private void Button(Transform p,string key,float x,float y,float w,Action action)
        {UIFactory.CreateButton(key,p,L(key),25,new Color(.12f,.31f,.36f,.97f),_pale,action,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y-58),new Vector2(x+w,-y));}
        public void Init(SquadDemoDirector app)
        {
            _app=app;_root=UIFactory.CreateCanvas("SquadUI",transform,20).transform;
            _play=Panel("HUD",_root,0,0,1920,1080,Color.clear);
            Label(_play.transform,L(Iso?"st.25d.brand":"st.hud.brand"),23,44,30,750,40,_gold);
            var objective=Panel("Objective",_play.transform,1430,40,450,200,_ink);
            Label(objective.transform,L("st.hud.objective"),22,22,18,400,40,_cyan);
            _objective=Label(objective.transform,"",24,22,58,404,128);
            _actors=new Text[app.Session.Party.Count];_health=new Image[_actors.Length];
            for(var i=0;i<_actors.Length;i++)
            {
                var card=Panel("Actor"+i,_play.transform,40,102+i*96,350,82,_ink);
                Picture(card.transform,"Art/Portraits/"+app.Session.Party[i].Config.id+"_v02",0,0,82,82);
                if(Iso){var index=i;var select=card.AddComponent<Button>();card.GetComponent<Image>().raycastTarget=true;select.targetGraphic=card.GetComponent<Image>();select.onClick.AddListener(()=>_app.Session.Tactical.Select(index));}
                _actors[i]=Label(card.transform,"",21,94,8,255,62);
                var bar=UIFactory.CreatePanel("Health",card.transform,_cyan);bar.GetComponent<Image>().raycastTarget=false;
                var rt=(RectTransform)bar.transform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(94,-72);rt.sizeDelta=new Vector2(240,4);
                _health[i]=bar.GetComponent<Image>();
            }
            _waypoint=Label(_play.transform,"",26,800,480,320,55,_gold);_waypoint.alignment=TextAnchor.MiddleCenter;
            _weapon=Label(_play.transform,"",23,1460,835,420,100,_gold);
            gameObject.AddComponent<SquadCombatFeedback>().Init(app,_play.transform);
            _order=Label(_play.transform,"",20,44,400,440,70,_gold);
            _combat=Label(_play.transform,"",24,650,75,650,130,_gold);
            _enemyHealth=Panel("EnemyHealth",_play.transform,650,119,600,6,_gold).GetComponent<Image>();
            _enemyPosture=Panel("EnemyPosture",_play.transform,650,132,600,3,_cyan).GetComponent<Image>();
            _prompt=Label(_play.transform,"",28,540,765,940,75,_gold);_prompt.alignment=TextAnchor.MiddleCenter;
            var footer=Panel("Controls",_play.transform,40,965,1840,84,_ink);
            Label(footer.transform,L(Iso?"st.25d.controls":"st.controls"),19,22,13,1790,68);
            if(Iso)
            {
                Button(_play.transform,"st.plan.toggle",44,490,346,()=>app.Session.Tactical.Toggle());
                _planToggle=_play.transform.GetChild(_play.transform.childCount-1).GetComponentInChildren<Text>();
                _planPanel=Panel("Planning",_play.transform,460,165,930,170,_ink);
                _plan=Label(_planPanel.transform,"",24,24,15,885,90,_gold);
                Button(_planPanel.transform,"st.plan.attack",24,103,274,()=>app.Session.Tactical.SetIntent(TacticalAction.Attack));
                Button(_planPanel.transform,"st.plan.heavy",328,103,274,()=>app.Session.Tactical.SetIntent(TacticalAction.Heavy));
                Button(_planPanel.transform,"st.plan.skill",632,103,274,()=>app.Session.Tactical.SetIntent(TacticalAction.Skill));
            }
            _notice=Label(_root,"",25,450,835,1050,65,_pale);_notice.alignment=TextAnchor.MiddleCenter;
        }
        private void LateUpdate()
        {
            if(_app==null)return;
            var s=_app.Session;
            if(_screen!=_app.Screen){_screen=_app.Screen;BuildOverlay();}
            _notice.text=_app.Notice;
            if(_screen=="story"&&_app.Story!=null){_story.text=_app.Story.VisibleText;_speaker.text=_app.Story.Current?.speaker??_app.Story.Title;}
            if(_screen!="play")return;
            if(Iso)
            {
                var t=s.Tactical;_planPanel.SetActive(t.Planning);
                _planToggle.text=L(t.Planning?"st.plan.execute":"st.plan.toggle");
                _plan.text=L("st.plan.paused")+"   ·   "+L(s.Party[t.Selected].Config.nameKey)+" / "+L("st.plan."+t.Intent.ToString().ToLowerInvariant())+"\n"+L("st.plan.instructions");
            }
            _weapon.text=L("st.hud.weapon")+"  "+L(s.Active.WeaponKey)+"\n"+L("st.hud.combo")+"  "+(s.Active.Combo+1)+" / 3";
            UpdateWaypoint();
            for(var i=0;i<s.Party.Count;i++)
            {
                var a=s.Party[i];var name=L(a.Ryunosuke?"st.character.ryunosuke":a.Config.nameKey);
                var state=!a.Alive?L("st.hud.down"):a.Form!=null?L(a.Form.nameKey):$"{L("st.hud.stamina")} {a.Stamina:0}";
                var selected=Iso&&s.Tactical.Planning?s.Tactical.Selected:s.ActiveIndex;
                if(Iso && s.Tactical.HasOrder(i))state=L(s.Tactical.OrderKey(i));
                _actors[i].text=$"{(i==selected?"▸":"  ")} {i+1}  {name}   {a.Hp:0}/{a.Config.hp:0}\n     {state}";
                _actors[i].color=i==selected?_gold:_pale;
                ((RectTransform)_health[i].transform).sizeDelta=new Vector2(240*a.Hp/a.Config.hp,4);
            }
            _order.text=L("st.order."+s.Order.ToString().ToLowerInvariant())+"\n"+string.Format(L("st.hud.loop"),s.LoopCount);
            _objective.text=Objective();
            s.RefreshContext();
            _prompt.text=s.Phase==SquadPhase.Rewinding?L("st.hud.rewinding") : s.ScanningId!=null?string.Format(L("st.hud.scanning"),s.ScanProgress*100):L(s.PromptKey)+"  "+L(s.ContextNameKey);
            var enemy=s.Locked!=null?s.Locked:s.NearestEnemy(s.Active.transform.position,12);
            _enemyHealth.enabled=_enemyPosture.enabled=enemy!=null;
            if(enemy!=null)
            {
                ((RectTransform)_enemyHealth.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,600*enemy.Hp/enemy.Config.hp);
                ((RectTransform)_enemyPosture.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,600*enemy.Posture/enemy.Config.posture);
                var cue=enemy.LinkReady?"st.combat.link":enemy.Recovering?"st.combat.recovery":"st.combat."+enemy.Config.attackStyle;
                _combat.text=L(enemy.Config.nameKey)+$"  {enemy.Hp:0}/{enemy.Config.hp:0}   "+L("st.combat.posture")+$" {enemy.Posture:0}\n\n"+L(Iso&&cue=="st.combat.sweep"?"st.25d.sweep":cue);
            }
            else _combat.text="";
            if(s.Active.CounterReady)_weapon.text+="\n"+L("st.combat.counter");
            if(s.Active.Ryunosuke && enemy!=null && enemy.Winding)_combat.text+="\n"+L("st.hud.foresee");
            if(s.Active.IsUshio || s.Active.Ryunosuke)_combat.text+=$"\nE  {L(s.Active.IsUshio?"st.ability.hair":"st.ability.foresee")}   {s.Active.HairCooldown:0.0}s";
        }
        private void UpdateWaypoint()
        {
            var s=_app.Session;var enemy=s.NearestEnemy(s.Active.transform.position,100);Vector3 target;string label;
            if(enemy!=null){target=enemy.transform.position+Vector3.up*2.2f;label=L(enemy.Config.nameKey);}
            else
            {
                var clue=Array.Find(s.Config.interactions,c=>!s.Collected.Contains(c.id)&&(c.requiredForEncounter||c.reward=="pipe"));
                target=clue!=null?SquadConfig.Position(clue.spawn)+Vector3.up*2.4f:SquadConfig.Position(s.Phase==SquadPhase.Explore?s.Config.world.encounterGate:s.Config.world.exit)+Vector3.up*2;
                label=clue!=null?L(clue.nameKey):L(s.Phase==SquadPhase.Explore?"st.prompt.encounter":"st.prompt.exit");
            }
            var screen=s.CameraRig.Camera.WorldToViewportPoint(target);
            var x=screen.z<0?.5f:Mathf.Clamp(screen.x,.27f,.75f);var y=screen.z<0?.28f:Mathf.Clamp(screen.y,.23f,.73f);
            ((RectTransform)_waypoint.transform).anchoredPosition=new Vector2(x*1920-160,-(1-y)*1080);
            _waypoint.text=(screen.z<0?"↓ ":"◇ ")+label+"  "+Vector3.Distance(s.Active.transform.position,target).ToString("0")+"m";
        }
        private string Objective()
        {
            var s=_app.Session;string key;Vector3 target;
            if(s.Phase==SquadPhase.Explore)
            {
                if(s.Enemies.Exists(e=>e.Alive))return L("st.goal.patrol");
                var clue=Array.Find(s.Config.interactions,c=>c.requiredForEncounter&&!s.Collected.Contains(c.id));
                key=clue!=null?(clue.id.Contains("shell")?"st.goal.shell":"st.goal.shadow"):"st.goal.encounter";
                target=SquadConfig.Position(clue!=null?clue.spawn:s.Config.world.encounterGate);
            }
            else {key=s.Enemies.TrueForAll(e=>!e.Alive)?"st.goal.exit":"st.goal.fight";target=SquadConfig.Position(s.Config.world.exit);}
            return L(key)+"\n"+string.Format(L("st.hud.distance"),Vector3.Distance(s.Active.transform.position,target));
        }
        private void BuildOverlay()
        {
            if(_overlay!=null){_overlay.SetActive(false);Destroy(_overlay);}
            _play.SetActive(_screen=="play");
            if(_screen=="play")return;
            _overlay=Panel("Overlay",_root,0,0,1920,1080,new Color(.025f,.055f,.075f,.48f));
            if(_screen=="forms")
            {
                var forms=Panel("Forms",_overlay.transform,330,200,1260,680,_ink);
                Label(forms.transform,L("st.forms.title"),44,35,25,1160,65,_gold);
                Label(forms.transform,L("st.forms.body"),27,35,110,1170,100);
                for(var i=0;i<_app.Session.Config.scanTemplates.Length;i++)
                {
                    var f=_app.Session.Config.scanTemplates[i];var scanned=_app.Session.Party[1].Scans.Contains(f.id);
                    var key=f.id;Label(forms.transform,L(f.nameKey)+"   "+L(scanned?"st.forms.ready":"st.forms.locked"),26,40,220+i*88,750,70,scanned?_gold:_pale);
                    if(scanned&&_app.Session.Active.IsUshio)Button(forms.transform,f.nameKey,885,210+i*88,325,()=>_app.ChooseForm(key));
                }
                if(!_app.Session.Active.IsUshio)Label(forms.transform,L("st.forms.need"),24,40,500,1000,55,_gold);
                Button(forms.transform,"st.button.resume",40,570,330,_app.Resume);return;
            }
            if(_screen=="journal")
            {
                var journalPanel=Panel("Journal",_overlay.transform,180,100,1560,880,_ink);
                Label(journalPanel.transform,L("st.button.journal"),42,35,30,1000,65,_gold);
                Button(journalPanel.transform,"st.button.resume",1190,30,330,_app.Resume);
                var viewport=UIFactory.CreatePanel("Scroll",journalPanel.transform,new Color(.02f,.05f,.07f,.9f),new Vector2(0,0),new Vector2(1,1),new Vector2(35,30),new Vector2(-35,-130));
                viewport.AddComponent<RectMask2D>();var scroll=viewport.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=40;
                var content=Label(viewport.transform,_app.Journal(),25,20,10,1440,1500);
                ((RectTransform)content.transform).sizeDelta=new Vector2(1440,Mathf.Max(720,content.preferredHeight+30));
                scroll.viewport=(RectTransform)viewport.transform;scroll.content=(RectTransform)content.transform;
                return;
            }
            if(_screen=="overlook"){_overlay.AddComponent<OverlookView>().Init(_app);return;}
            if(_screen=="story")
            {
                var box=Panel("Dialogue",_overlay.transform,180,730,1560,270,_ink);
                _speaker=Label(box.transform,"",28,36,24,1300,42,_gold);_story=Label(box.transform,"",30,36,80,1450,110);
                Button(box.transform,"st.button.next",1230,195,275,_app.Advance);
                Label(_overlay.transform,L("st.story.label"),24,190,680,1500,42,_pale);return;
            }
            if(_screen=="title")
            {
                Picture(_overlay.transform,"Art/Bg/st_coast_v02",0,0,1920,1080);
                var box=Panel("TitleCard",_overlay.transform,100,170,790,705,_ink);
                Label(box.transform,L(Iso?"st.25d.brand":"st.title.eyebrow"),23,44,40,700,44,_cyan);
                Label(box.transform,L("st.title.main"),72,40,118,700,105,_pale);
                Label(box.transform,L(Iso?"st.25d.subtitle":"st.title.subtitle"),28,44,240,700,105,_gold);
                Button(box.transform,"st.button.new",44,410,322,()=>_app.NewGame(false));
                if(_app.HasContinue)Button(box.transform,"st.button.continue",388,410,352,_app.Continue);
                Button(box.transform,"st.button.story",44,493,322,()=>_app.NewGame(true));
                Button(box.transform,"st.button.quit",388,493,352,_app.Quit);
                Label(box.transform,L(Iso?"st.25d.note":"st.title.note"),21,44,595,700,85,_pale);return;
            }
            var panel=Panel("Menu",_overlay.transform,340,220,1240,640,_ink);
            Label(panel.transform,L("st.screen."+_screen),49,45,30,1140,75,_gold);
            Label(panel.transform,L("st.body."+_screen),27,45,132,1120,170);
            if(_screen=="pause")
            {
                Button(panel.transform,"st.button.resume",45,330,330,_app.Resume);
                Button(panel.transform,"st.button.overlook",425,250,330,_app.Overlook);
                Button(panel.transform,"st.button.journal",425,330,330,_app.OpenJournal);
            }
            else Button(panel.transform,"st.button.retry",45,330,330,_app.Rewind);
            Button(panel.transform,"st.button.title",805,330,385,_app.Title);
        }
    }
}
