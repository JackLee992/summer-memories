using System;
using System.IO;
using SummerMemories.Action3D.Side2D;
using SummerMemories.Core.Localization;
using SummerMemories.Core.Save;
using UnityEngine;
namespace SummerMemories.App
{
    public sealed class SideDirector : MonoBehaviour
    {
        public SideSession Session {get;private set;}
        public SideWorld World {get;private set;}
        public LocalizationService Text {get;private set;}
        public string Screen="title";
        public bool Smoke {get;private set;}
        private SideHud _hud;private AudioSource _music,_audio;private float _hitstop;private string _slot;
        public bool HasContinue=>SaveSystem.HasSave(_slot);
        private void Awake()
        {
            Smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"--side-smoke")>=0;
            Text=new LocalizationService();Text.Load();
            var config=JsonUtility.FromJson<SideConfig>(Resources.Load<TextAsset>("Battles/st_side2d").text);
            Session=new SideSession(config);_slot=Smoke?"st_side_test_"+Guid.NewGuid().ToString("N"):config.saveSlot;
            World=gameObject.AddComponent<SideWorld>();World.Init(Session);
            _audio=gameObject.AddComponent<AudioSource>();_audio.volume=.4f;
            _music=gameObject.AddComponent<AudioSource>();_music.clip=Resources.Load<AudioClip>("Audio/Prototype/st_ambient_v01");_music.loop=true;_music.volume=.22f;_music.Play();
            Session.Sound+=key=>{var clip=Resources.Load<AudioClip>("Audio/Prototype/st_"+key+"_"+(key=="hit"||key=="jump"||key=="dodge"?"v02":"v01"));if(clip!=null)_audio.PlayOneShot(clip);};
            Session.Hit+=(p,d)=>_hitstop=.055f;Session.Changed+=Persist;
            _hud=gameObject.AddComponent<SideHud>();_hud.Init(this);
            if(UnityEngine.EventSystems.EventSystem.current!=null)UnityEngine.EventSystems.EventSystem.current.sendNavigationEvents=false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(Smoke)gameObject.AddComponent<SideSmokeTest>().Init(this,_slot);
#endif
        }
        public void StartGame(){Session.NewGame();Screen="play";Persist();}
        public void Continue()
        {
            try{var save=SaveSystem.Load(_slot);if(save==null)return;Session.Load(JsonUtility.FromJson<SideSave>(save.GetFlag("side")));Screen="play";}
            catch(ArgumentException)
            {
                var p=Path.Combine(Application.persistentDataPath,"saves",_slot+".json");
                if(File.Exists(p))File.Move(p,p+".broken-"+DateTime.UtcNow.Ticks);Session.Notice="s2.save.error";
            }
            catch(Exception e){Session.Notice="s2.save.error";Debug.LogWarning(e);}
        }
        public void Persist()
        {
            if(Screen=="title")return;
            try{var save=new SaveSlot{slotName=_slot,loopCount=Session.Loops};save.SetFlag("side",JsonUtility.ToJson(Session.Save()));SaveSystem.Save(save);}
            catch(Exception e){Session.Notice="s2.save.error";Debug.LogWarning(e);}
        }
        public void Resume(){Screen="play";Session.Paused=false;}
        public void Title(){Persist();Screen="title";Session.Paused=true;}
        public void Rewind(){Screen="play";Session.Rewind();}
        public void Overlook(){Screen=Screen=="overlook"?"play":"overlook";Session.Paused=Screen!="play";}
        private void Update()
        {
            if(Smoke)return;
            if(Input.GetKeyDown(KeyCode.Escape)){if(Screen=="play"){Screen="pause";Session.Paused=true;}else if(Screen!="title")Resume();}
            if(Input.GetKeyDown(KeyCode.Return))
            {if(Screen=="title")StartGame();else if(Session.Dialogue!="")Session.DismissDialogue();else if(Session.Phase==SidePhase.Defeat)Rewind();}
            if(Screen=="play"&&Input.GetKeyDown(KeyCode.R))Rewind();
            if(Screen!="title"&&Input.GetKeyDown(KeyCode.M))Overlook();
            var input=new SideInput{switchTo=-1};
            if(Screen=="play"&&Session.Dialogue=="")
            {
                input.move=Input.GetAxisRaw("Horizontal");input.jump=Input.GetKeyDown(KeyCode.Space);input.dash=Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.LeftControl);
                input.attack=Input.GetKeyDown(KeyCode.J);input.heavy=Input.GetKeyDown(KeyCode.K);input.skill=Input.GetKeyDown(KeyCode.E);input.interact=Input.GetKeyDown(KeyCode.F);input.copy=Input.GetKeyDown(KeyCode.C);input.returnHuman=Input.GetKeyDown(KeyCode.G);input.consciousness=Input.GetKeyDown(KeyCode.V);
                for(var i=0;i<3;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))input.switchTo=i;
                if(Input.GetKeyDown(KeyCode.B))Session.CarryWatch();
            }
            Frame(Mathf.Min(Time.deltaTime,.04f),input);
        }
        public void Frame(float dt,SideInput input)
        {
            Session.Paused=Screen!="play";
            if(_hitstop>0){_hitstop-=dt;World.Render(0);return;}
            Session.Step(dt,input);World.Render(dt);
        }
        private void OnApplicationQuit(){if(!Smoke)Persist();}
    }
}
