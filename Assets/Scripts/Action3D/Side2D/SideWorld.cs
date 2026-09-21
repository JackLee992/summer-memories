using System.Collections.Generic;
using UnityEngine;
namespace SummerMemories.Action3D.Side2D
{
    public sealed class SideWorld : MonoBehaviour
    {
        public Camera Camera {get;private set;}
        private SideSession _session;private SpriteRenderer _player,_stone,_watch,_mio,_slash;
        private readonly List<SpriteRenderer> _bars=new List<SpriteRenderer>();
        private LineRenderer[] _hair;private Material _hairMaterial;
        private readonly List<SpriteRenderer> _enemies=new List<SpriteRenderer>(),_cues=new List<SpriteRenderer>();
        private readonly Dictionary<string,GameObject> _gates=new Dictionary<string,GameObject>(),_objects=new Dictionary<string,GameObject>();
        private readonly Dictionary<string,Sprite[]> _frames=new Dictionary<string,Sprite[]>();
        private readonly List<Sprite> _generated=new List<Sprite>();
        private Sprite _white;private float _time;
        public void Init(SideSession session)
        {
            _session=session;
            var pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();_white=Sprite.Create(pixel,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);_generated.Add(_white);
            Camera=new GameObject("SideCamera").AddComponent<Camera>();Camera.transform.SetParent(transform,false);Camera.orthographic=true;Camera.orthographicSize=5.5f;Camera.backgroundColor=new Color(.12f,.21f,.25f);Camera.clearFlags=CameraClearFlags.SolidColor;Camera.transform.position=new Vector3(10,3,-20);Camera.farClipPlane=100;Camera.gameObject.AddComponent<AudioListener>();
            foreach(var c in session.Config.characters)_frames[c.id]=LoadSheet(c.sheet);
            var bg=Resources.Load<Texture2D>(session.Config.background);
            if(bg!=null)
            {
                var image=Sprite.Create(bg,new Rect(0,0,bg.width,bg.height),new Vector2(.5f,.5f),bg.height/12f);_generated.Add(image);
                for(var i=0;i<4;i++){var s=Draw("CoastalBackdrop",new Vector2(10+i*27,4.8f),Vector2.one,Color.white,-30);s.sprite=image;}
            }
            else Rect("Sky",new Vector2(44,4),new Vector2(100,20),new Color(.34f,.54f,.59f),-30);
            // Ground and collision architecture use crisp flat shapes; gameplay sits on one readable plane.
            Rect("Foundation",new Vector2(44,-1.6f),new Vector2(90,3.2f),new Color(.13f,.20f,.22f),2);
            Rect("StoneLip",new Vector2(44,-.12f),new Vector2(90,.24f),new Color(.58f,.61f,.54f),3);
            for(var x=0;x<90;x++)
            {Rect("PavingSeam",new Vector2(x,-.1f),new Vector2(.025f,.20f),new Color(.27f,.37f,.37f),4);Rect("Brick",new Vector2(x+.5f,-.5f),new Vector2(.96f,.52f),new Color(.19f+x%3*.015f,.28f,.29f),3);}
            foreach(var b in session.Config.solids)
            {
                if(b.id=="floor")continue;
                var frame=Draw("Solid_"+b.id,new Vector2(b.x,b.y),new Vector2(b.width,b.height),new Color(.12f,.22f,.25f),4);
                if(!string.IsNullOrEmpty(b.opensWith))_gates[b.opensWith]=frame.gameObject;
                if(b.id=="tunnel")
                {
                    for(var i=0;i<11;i++)Rect("WarehouseSlat",new Vector2(b.x-b.width*.5f+.33f+i*.63f,b.y),new Vector2(.56f,b.height-.15f),new Color(.29f+i%2*.03f,.35f,.33f),5);
                    Rect("WarehouseRoof",new Vector2(b.x,b.y+b.height*.5f),new Vector2(b.width+.4f,.18f),new Color(.50f,.55f,.50f),6);
                    Rect("LowGapTrim",new Vector2(b.x,b.y-b.height*.5f),new Vector2(b.width,.12f),new Color(.57f,.43f,.28f),6);
                }
            }
            foreach(var o in session.Config.objects)
            {
                if(o.type=="exit")continue;
                var s=Draw(o.id,new Vector2(o.x,o.y+.27f),new Vector2(.42f,.42f),o.type=="core"?new Color(.19f,.12f,.25f):new Color(.88f,.70f,.37f),8);
                if(o.type=="pipe"){s.transform.localScale=new Vector3(.10f,1.25f,1);s.transform.rotation=Quaternion.Euler(0,0,-65);}
                if(o.type=="core")s.transform.localScale=new Vector3(1.6f,2.8f,1);
                _objects[o.id]=s.gameObject;
            }
            _player=Draw("Player",Vector2.zero,Vector2.one,Color.white,10);
            _stone=Draw("CopiedStone",Vector2.zero,new Vector2(.42f,.35f),new Color(.49f,.57f,.55f),10);
            _watch=Draw("UshioWatch",Vector2.zero,new Vector2(.10f,.15f),new Color(1,.8f,.35f),12);
            var mio=Resources.Load<Texture2D>("Art/Portraits/25D/st_mio");
            _mio=Draw("Mio",new Vector2(85,0),Vector2.one,Color.white,10);
            if(mio!=null){var sp=Sprite.Create(mio,new Rect(0,0,mio.width,mio.height),new Vector2(.5f,0),mio.height/2.2f);_generated.Add(sp);_mio.sprite=sp;}
            _slash=Draw("Strike",Vector2.zero,Vector2.one,new Color(1,.84f,.53f),13);
            _hairMaterial=new Material(Resources.Load<Shader>("Shaders/SquadTrail"));_hair=new LineRenderer[3];
            for(var i=0;i<3;i++){var line=new GameObject("HairStrand"+i).AddComponent<LineRenderer>();line.transform.SetParent(transform,false);line.sharedMaterial=_hairMaterial;line.positionCount=18;line.sortingOrder=14;line.startWidth=.06f;line.endWidth=.025f;line.startColor=line.endColor=new Color(1,.82f,.35f);_hair[i]=line;}
            RebuildEnemies();
        }
        private Sprite[] LoadSheet(string resource)
        {
            var texture=Resources.Load<Texture2D>(resource);var result=new Sprite[12];
            if(texture==null)throw new System.InvalidOperationException("Missing side animation "+resource);
            var w=texture.width/4;var h=texture.height/3;
            for(var i=0;i<12;i++){result[i]=Sprite.Create(texture,new Rect(i%4*w,(2-i/4)*h,w,h),new Vector2(.5f,.06f),h/2.65f);_generated.Add(result[i]);}
            return result;
        }
        private SpriteRenderer Draw(string name,Vector2 p,Vector2 scale,Color color,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.position=new Vector3(p.x,p.y,0);go.transform.localScale=new Vector3(scale.x,scale.y,1);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=_white;renderer.color=color;renderer.sortingOrder=order;return renderer;
        }
        private void Rect(string name,Vector2 p,Vector2 scale,Color color,int order)=>Draw(name,p,scale,color,order);
        private void RebuildEnemies()
        {
            foreach(var b in _bars)Destroy(b.gameObject);_bars.Clear();
            foreach(var e in _enemies)Destroy(e.gameObject);foreach(var c in _cues)Destroy(c.gameObject);_enemies.Clear();_cues.Clear();
            foreach(var e in _session.Enemies)
            {_bars.Add(Draw("EnemyHp",new Vector2(e.x,2.7f),new Vector2(1.4f,.065f),new Color(.93f,.53f,.40f),16));_enemies.Add(Draw(e.data.id,new Vector2(e.x,0),Vector2.one,new Color(.12f,.16f,.22f),9));_cues.Add(Draw("AttackWarning",new Vector2(e.x,.07f),Vector2.one,new Color(.96f,.35f,.27f,.65f),7));}
        }
        public void Render(float dt)
        {
            var s=_session;var p=s.Active;if(!s.Paused&&s.Dialogue==""&&s.Phase==SidePhase.Play)_time+=dt;
            if(_enemies.Count!=s.Enemies.Count)RebuildEnemies();
            var frame=0;
            switch(p.action)
            {
                case SideAction.Run:frame=1+(int)(_time*9)%4;break;
                case SideAction.Jump:frame=5;break;case SideAction.Fall:frame=6;break;case SideAction.Dash:frame=7;break;
                case SideAction.Attack:case SideAction.Heavy:case SideAction.Skill:frame=p.actionTime/p.actionLength<.35f?8:p.actionTime/p.actionLength<.7f?9:10;break;
                case SideAction.Hurt:case SideAction.Dead:frame=11;break;
            }
            _player.sprite=_frames[p.id][frame];_player.flipX=p.facing<0;_player.transform.position=p.position;
            _player.color=p.invulnerable>0&&((int)(_time*18)%2==0)?new Color(1,1,1,.5f):Color.white;
            _player.enabled=p.form!=SideForm.Stone;_stone.enabled=!_player.enabled;_stone.transform.position=p.position+Vector2.up*.17f;
            _watch.enabled=s.WatchCarried&&s.ActiveIndex==0;_watch.transform.position=p.position+new Vector2(.15f*p.facing,1.18f);
            _slash.enabled=(p.action==SideAction.Attack||p.action==SideAction.Heavy||p.action==SideAction.Skill)&&p.actionTime/p.actionLength>.38f&&p.actionTime/p.actionLength<.64f;
            if(_slash.enabled)
            {
                var range=p.action==SideAction.Skill&&(s.ActiveIndex==1||s.WatchCarried)?6:s.Config.characters[s.ActiveIndex].reach;
                _slash.transform.position=p.position+new Vector2(p.facing*range*.5f,1.1f);_slash.transform.localScale=new Vector3(range,.055f,1);
            }
            var hairActive=p.action==SideAction.Skill&&(s.ActiveIndex==1||s.WatchCarried&&s.ActiveIndex==0)&&p.actionTime>.16f&&p.actionTime<.6f;
            for(var i=0;i<3;i++)
            {
                var line=_hair[i];line.enabled=hairActive;if(!hairActive)continue;
                for(var n=0;n<18;n++){var t=n/17f;line.SetPosition(n,new Vector3(p.position.x+p.facing*t*6,p.position.y+1.65f-t*.6f+Mathf.Sin(t*9+i+_time*18)*.12f*t,0));}
            }
            for(var i=0;i<s.Enemies.Count;i++)
            {
                var e=s.Enemies[i];_bars[i].enabled=e.Alive;_bars[i].transform.position=new Vector3(e.x,2.65f,0);_bars[i].transform.localScale=new Vector3(1.4f*e.hp/e.data.hp,.065f,1);var r=_enemies[i];r.enabled=e.Alive;r.sprite=_frames["st_shinpei"][e.committed?8:e.recovery>0?10:1+(int)(_time*7)%4];r.flipX=e.facing<0;r.transform.position=new Vector3(e.x,0,0);
                r.color=e.bound>0?new Color(.38f,.63f,.68f):new Color(.09f,.12f,.18f);
                var cue=_cues[i];cue.enabled=e.Alive&&e.committed;
                cue.transform.position=new Vector3(e.x+e.facing*e.data.reach*.5f,.045f,0);cue.transform.localScale=new Vector3(e.data.reach,.09f,1);
            }
            foreach(var pair in _gates)pair.Value.SetActive(!s.Flags.Contains(pair.Key));
            foreach(var o in s.Config.objects)if(_objects.TryGetValue(o.id,out var go))go.SetActive(string.IsNullOrEmpty(o.sets)||!s.Flags.Contains(o.sets));
            var target=new Vector3(Mathf.Clamp(p.position.x+p.facing*2.3f,9.5f,s.Config.length-9.5f),3,-20);
            Camera.transform.position=Vector3.Lerp(Camera.transform.position,target,1-Mathf.Exp(-dt*6));
        }
        private void OnDestroy(){if(_hairMaterial!=null)Destroy(_hairMaterial);foreach(var s in _generated)Destroy(s);if(_white!=null)Destroy(_white.texture);}
    }
}
