using System;
using SummerMemories.Action3D.Side2D;
using SummerMemories.Core.UI;
using UnityEngine;
using UnityEngine.UI;
namespace SummerMemories.App
{
    public sealed class SideHud:MonoBehaviour
    {
        private SideDirector _app;private Transform _canvas;private GameObject _play,_overlay;
        private Text _goal,_prompt,_notice,_timer;private Text[] _names;private Image[] _hp;private Image[] _cards;
        private string _built="";private int _branch=-1,_event=-1;
        private readonly Color _ink=new Color(.035f,.085f,.11f,.94f),_white=new Color(.91f,.94f,.88f),_gold=new Color(.99f,.79f,.43f),_blue=new Color(.3f,.79f,.78f);
        private string L(string key)=>string.IsNullOrEmpty(key)?"":_app.Text.Get(key);
        private GameObject Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
        {var go=UIFactory.CreatePanel(name,parent,color,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y-h),new Vector2(x+w,-y));go.GetComponent<Image>().raycastTarget=false;return go;}
        private Text Label(Transform parent,string text,int size,float x,float y,float w,float h,Color? color=null)
        {
            var t=UIFactory.CreateText("Text",parent,text,size,color??_white,TextAnchor.UpperLeft);var r=(RectTransform)t.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return t;
        }
        private void Button(Transform parent,string text,float x,float y,float w,Action action)
        {UIFactory.CreateButton("Button",parent,text,24,new Color(.10f,.28f,.33f,.97f),_white,action,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y-58),new Vector2(x+w,-y));}
        public void Init(SideDirector app)
        {
            _app=app;_canvas=UIFactory.CreateCanvas("SideUI",transform,30).transform;_play=Panel("HUD",_canvas,0,0,1920,1080,Color.clear);
            Label(_play.transform,L("s2.subtitle"),24,40,28,800,40,_gold);
            var mission=Panel("Mission",_play.transform,1240,26,635,136,_ink);_goal=Label(mission.transform,"",26,22,16,592,105);
            _names=new Text[3];_hp=new Image[3];_cards=new Image[3];
            for(var i=0;i<3;i++)
            {
                var n=i;var card=Panel("Party"+i,_play.transform,40+i*318,922,302,115,_ink);_cards[i]=card.GetComponent<Image>();_cards[i].raycastTarget=true;
                var button=card.AddComponent<Button>();button.targetGraphic=_cards[i];button.onClick.AddListener(()=>app.Session.Switch(n));
                _names[i]=Label(card.transform,"",23,15,12,280,75);
                _hp[i]=Panel("Hp",card.transform,15,98,272,4,_blue).GetComponent<Image>();
            }
            _prompt=Label(_play.transform,"",27,420,809,1080,70,_gold);_prompt.alignment=TextAnchor.MiddleCenter;
            var strip=Panel("Notice",_play.transform,40,92,690,72,_ink);_notice=Label(strip.transform,"",23,17,10,655,62);
            _timer=Label(_play.transform,"",30,1240,180,635,55,_gold);
            Label(_play.transform,L("s2.controls"),18,1040,942,830,83,new Color(.78f,.84f,.82f));
        }
        private void LateUpdate()
        {
            if(_app==null)return;var s=_app.Session;var screen=_app.Screen;
            _play.SetActive(screen=="play"&&s.Dialogue==""&&s.Phase==SidePhase.Play);
            var mode=screen+"|"+s.Dialogue+"|"+s.Phase+"|"+_branch+"|"+_event;
            if(mode!=_built){_built=mode;Overlay();}
            _goal.text=L(!s.Flags.Contains("tunnel_open")?(s.Scans.Contains("stone")?"s2.goal.tunnel":"s2.goal.start"):!s.Flags.Contains("battle_clear")?"s2.goal.fight":!s.Flags.Contains("rescue_started")?"s2.goal.rescue":s.Flags.Contains("core_broken")?"s2.goal.exit":"s2.goal.core");
            s.RefreshContext();_prompt.text=s.Context==null?"":L(s.Context.promptKey)+"  ·  "+L(s.Context.nameKey);
            _notice.text=L(s.Notice);_timer.text=s.RescueTime>=0&&!s.Flags.Contains("core_broken")?string.Format(L("s2.hud.timer"),s.RescueTime):string.Format(L("s2.hud.loops"),s.Loops);
            for(var i=0;i<3;i++)
            {
                var a=s.Party[i];_names[i].text=$"{i+1}  {L(a.ryunosuke?"st.character.ryunosuke":s.Config.characters[i].nameKey)}   {a.hp:0}/{s.Config.characters[i].hp:0}\n"+(a.hp<=0?L("s2.hud.down"):a.form==SideForm.Stone?L("s2.hud.stone"):s.WatchCarried&&i==1?L("s2.hud.watch"):a.cooldown>0?$"E  {a.cooldown:0.0}s":L("s2.hud.ready"));
                _cards[i].color=i==s.ActiveIndex?new Color(.13f,.31f,.34f,.96f):_ink;
                ((RectTransform)_hp[i].transform).sizeDelta=new Vector2(272*a.hp/s.Config.characters[i].hp,4);
            }
        }
        private void Overlay()
        {
            if(_overlay!=null){_overlay.SetActive(false);Destroy(_overlay);}
            var s=_app.Session;var screen=_app.Screen;
            if(screen=="play"&&s.Dialogue==""&&s.Phase==SidePhase.Play)return;
            _overlay=Panel("Overlay",_canvas,0,0,1920,1080,new Color(.02f,.05f,.075f,.62f));
            if(screen=="title")
            {
                var panel=Panel("Title",_overlay.transform,120,210,800,660,_ink);
                Label(panel.transform,L("s2.title"),74,45,42,720,100);Label(panel.transform,L("s2.subtitle"),30,49,155,700,70,_gold);
                Label(panel.transform,L("s2.intro"),28,49,260,700,100);
                Button(panel.transform,L("s2.start"),49,420,325,_app.StartGame);
                if(_app.HasContinue)Button(panel.transform,L("s2.continue"),405,420,345,_app.Continue);
                Button(panel.transform,L("s2.exit"),49,500,325,()=>Application.Quit());
                Label(panel.transform,L("s2.version"),20,49,600,710,45,_blue);return;
            }
            if(screen=="overlook"){Timeline();return;}
            if(s.Dialogue!=""&&screen=="play")
            {
                var panel=Panel("Dialogue",_overlay.transform,190,665,1540,315,_ink);
                Label(panel.transform,L(s.Dialogue),29,35,30,1450,195);
                Button(panel.transform,L("s2.next"),1140,230,350,s.DismissDialogue);return;
            }
            var box=Panel("Menu",_overlay.transform,360,280,1200,500,_ink);
            var victory=s.Phase==SidePhase.Victory;var text=screen=="pause"?"s2.pause":victory?"s2.story.victory":s.Notice;
            Label(box.transform,L(text),36,45,45,1100,230,_gold);
            Button(box.transform,L(screen=="pause"?"s2.resume":"s2.rewind"),45,315,345,()=>{if(screen=="pause")_app.Resume();else _app.Rewind();});
            Button(box.transform,L("s2.overlook"),420,315,345,_app.Overlook);Button(box.transform,L("s2.back"),795,315,345,_app.Title);
        }
        private void Timeline()
        {
            var s=_app.Session;var root=Panel("Archive",_overlay.transform,80,55,1760,970,_ink);
            Label(root.transform,L("s2.timeline.title"),40,35,25,1320,70,_gold);Button(root.transform,L("s2.resume"),1400,27,320,_app.Resume);
            var branches=s.Archive.branches;var index=Mathf.Clamp(_branch<0?branches.Count-1:_branch,0,branches.Count-1);var b=branches[index];
            Button(root.transform,"◀",35,115,80,()=>{_branch=Mathf.Max(0,index-1);_event=-1;});
            Label(root.transform,$"{index+1} / {branches.Count}  ·  {L(b.anchor)}  ·  {b.elapsed:0.0}s  ·  {L(b.outcome)}",26,140,125,1420,55);
            Button(root.transform,"▶",1635,115,80,()=>{_branch=Mathf.Min(branches.Count-1,index+1);_event=-1;});
            Label(root.transform,L("s2.timeline.route"),24,35,195,1000,50,_blue);
            foreach(var p in b.route)Panel("Explored",root.transform,35+p.x/s.Config.length*1675,259,Mathf.Max(12,1675/s.Config.length*2),16,new Color(.25f,.61f,.62f));
            foreach(var e in b.events)Panel("MapEvent",root.transform,35+e.position.x/s.Config.length*1675,251,5,32,_gold);
            var selected=Mathf.Clamp(_event<0?b.events.Count-1:_event,0,Mathf.Max(0,b.events.Count-1));
            var first=Mathf.Max(0,selected-5);
            for(var n=first;n<Mathf.Min(b.events.Count,first+7);n++)
            {var eventIndex=n;var e=b.events[n];Button(root.transform,$"{e.time:0.0}s · {L(e.detailKey).Split('\n')[0]}",35,330+(n-first)*72,1040,()=>{_branch=index;_event=eventIndex;});}
            if(b.events.Count>0)
            {
                var e=b.events[selected];var text=$"{e.time:0.0}s · {e.position.x:0}m\n\n{L(e.detailKey)}\n\n";
                foreach(var a in e.party)text+=L("st.character."+a.id.Replace("st_",""))+$"  HP {a.hp:0}\n";
                Label(root.transform,text,25,1120,330,595,520);
            }
            else Label(root.transform,L("s2.timeline.none"),26,40,350,1500,80);
            Button(root.transform,"↑",35,865,100,()=>{_branch=index;_event=Mathf.Max(0,selected-7);});
            Button(root.transform,"↓",160,865,100,()=>{_branch=index;_event=Mathf.Min(b.events.Count-1,selected+7);});
        }
    }
}
