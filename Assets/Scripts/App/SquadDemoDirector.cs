using System;
using System.Collections.Generic;
using SummerMemories.Action3D.Squad;
using SummerMemories.ADV;
using SummerMemories.Core.Localization;
using SummerMemories.Core.Save;
using UnityEngine;

namespace SummerMemories.App
{
    public class SquadDemoDirector : MonoBehaviour
    {
        public SquadSession3D Session {get;private set;}
        public LocalizationService Text {get;private set;}
        public AdvDirector Story {get;private set;}
        public string Screen {get;private set;}="title";
        public string Notice {get;private set;}="";
        public bool SmokeMode {get;private set;}
        public bool HasContinue {get;private set;}
        private SquadHud _hud;
        private SaveSlot _save;
        private AudioSource _ambience,_sfx;
        private readonly Dictionary<string,AudioClip> _clips=new Dictionary<string,AudioClip>();
        private readonly HashSet<string> _tips=new HashSet<string>();
        private float _noticeUntil;
        private bool _saveError,_completed;
        private string _slot;
        private void Awake()
        {
            SmokeMode=Array.IndexOf(Environment.GetCommandLineArgs(),"--squad-smoke")>=0;
            Text=new LocalizationService();Text.Load();
            if(UnityEngine.EventSystems.EventSystem.current!=null)UnityEngine.EventSystems.EventSystem.current.sendNavigationEvents=false;
            var asset=Resources.Load<TextAsset>("Battles/st_squad_demo");
            if(asset==null)throw new InvalidOperationException("Squad configuration missing");
            var config=JsonUtility.FromJson<SquadConfig>(asset.text);
            var iso=false;
#if SM_SQUAD25D || UNITY_EDITOR
            iso=true;
#endif
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"--squad25d")>=0)iso=true;
            if(Array.IndexOf(args,"--squad3d")>=0)iso=false;
            if(iso)
            {
                config.presentation=JsonUtility.FromJson<SquadPresentation>(Resources.Load<TextAsset>("Battles/st_squad_25d").text);
                config.saveSlot=config.presentation.saveSlot;
            }
            _slot=SmokeMode?"st_smoke_"+Guid.NewGuid().ToString("N"):config.saveSlot;
            Session=gameObject.AddComponent<SquadSession3D>();Session.Init(config);Session.Paused=true;
            _ambience=gameObject.AddComponent<AudioSource>();_ambience.clip=Resources.Load<AudioClip>(config.audio.ambience);_ambience.loop=true;_ambience.volume=.38f;
            _sfx=gameObject.AddComponent<AudioSource>();_sfx.volume=.45f;
            _clips["hair"]=Resources.Load<AudioClip>(config.audio.hair);_clips["shell"]=Resources.Load<AudioClip>(config.audio.shell);
            foreach(var fx in new[]{"hit","dodge","jump"})_clips[fx]=Resources.Load<AudioClip>("Audio/Prototype/st_"+fx+"_v02");
            _clips["rewind"]=Resources.Load<AudioClip>(config.audio.rewind);_clips["shadowReveal"]=Resources.Load<AudioClip>(config.audio.shadowReveal);
            Session.Feedback+=ShowNotice;Session.Sound+=PlaySound;
            Session.ArchiveChanged+=Persist;
            Session.ClueFound+=tip=>{_tips.Add(tip);Persist();};Session.CheckpointChanged+=Persist;
            Session.Rewound+=()=>PlayStory("st_demo_return");
            Session.Finished+=()=>{_completed=true;Screen="victory";Session.Paused=true;Persist();};
            try {HasContinue=SaveSystem.HasSave(_slot);}catch(Exception e){SaveError(e);}
            _hud=gameObject.AddComponent<SquadHud>();_hud.Init(this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(SmokeMode)gameObject.AddComponent<SquadSmokeTest>().Init(this,_slot);
#endif
        }
        public void NewGame(bool showStory=true)
        {
            _completed=false;_tips.Clear();_save=new SaveSlot{slotName=_slot,currentChapterId="st_demo",anchorId="st_coast"};
            Session.Begin();_ambience.Play();if(showStory)PlayStory("st_demo_intro");else{foreach(var id in new[]{"st_tip_shinpei","st_tip_ushio","st_tip_hizuru","st_tip_ryunosuke","st_tip_party","st_tip_scan","st_tip_watch","st_tip_overlook"})_tips.Add(id);Resume();}
        }
        public void Continue()
        {
            try
            {
                _save=SaveSystem.Load(_slot);
                if(_save==null){HasContinue=false;ShowNotice("st.feedback.noSave");return;}
                var checkpoint=JsonUtility.FromJson<SquadCheckpoint>(_save.GetFlag("st.checkpoint"));
                var archiveJson=_save.GetFlag("st.archive");
                Session.Archive=string.IsNullOrEmpty(archiveJson)?new OverlookArchive():JsonUtility.FromJson<OverlookArchive>(archiveJson);
                if(Session.Archive==null||Session.Archive.version!=1||Session.Archive.branches==null)throw new InvalidOperationException("Invalid timeline archive");
                if(Session.Archive.Current==null)Session.Archive.Begin("st.timeline.exploreAnchor");
                Session.ValidateCheckpoint(checkpoint);Session.Restore(checkpoint);Session.LoopCount=_save.loopCount;
                Session.Memories.Clear();foreach(var entry in _save.flags)if(entry.key.StartsWith("st.memory.")&&entry.value=="1")Session.Memories.Add(entry.key.Substring(10));
                _tips.Clear();foreach(var tip in _save.tipsUnlocked)_tips.Add(tip);
                _ambience.Play();
                _completed=_save.GetFlag("st.completed")=="1";
                if(_completed){Screen="victory";Session.Paused=true;}
                else if(!string.IsNullOrEmpty(_save.advScriptId))PlayStory(_save.advScriptId,_save.advCommandIndex);
                else
                {
                    var previous=Session.Archive.Current;
                    if(previous.outcome=="st.timeline.active")previous.outcome="st.timeline.loaded";
                    Session.Archive.Begin(checkpoint.phase==SquadPhase.Fight?"st.timeline.battleAnchor":"st.timeline.exploreAnchor",previous.id,previous.elapsed);
                    Session.Record("continue",Session.Archive.Current.anchor);Resume();
                }
            }
            catch(Exception e){SaveError(e);}
        }
        public void PlayStory(string id,int commandIndex=0)
        {
            Screen="story";Session.Paused=true;
            var text=Resources.Load<TextAsset>("Story/"+id);
            if(text==null)throw new InvalidOperationException("Missing story: "+id);
            Story=new AdvDirector();Story.OnTip+=tip=>_tips.Add(tip);
            Story.OnFinished+=()=>{if(id=="st_demo_intro")PlayStory("st_demo_squad");else Resume();};
            Story.Load(JsonUtility.FromJson<AdvScript>(text.text));
            for(var n=0;n<100 && Story.ScriptId==id && Story.CommandIndex<commandIndex && Story.Phase!=AdvPhase.Finished;n++)Story.Press();
            Persist();
        }
        public void Advance(){if(Screen!="story")return;Story.Press();Persist();}
        public void Resume(){Session.Tactical?.Close();Screen="play";Session.Paused=false;Story=null;Persist();}
        public void Overlook(){if(Screen=="play"||Screen=="pause"){Screen="overlook";Session.Paused=true;Persist();}}
        public void Pause(){if(Screen=="play"){Screen="pause";Session.Paused=true;}}
        public void Rewind(){_completed=false;Screen="play";Story=null;Session.Rewind();}
        public void Title(){Persist();Screen="title";Story=null;Session.Paused=true;HasContinue=SaveSystem.HasSave(_slot);}
        public void Quit(){Application.Quit();}
        private void OnApplicationQuit(){if(!SmokeMode)Persist();}
        public void OpenForms(){if(Screen=="play"){Screen="forms";Session.Paused=true;}}
        public void ChooseForm(string id){Resume();Session.Morph(id);}
        public void OpenJournal(){if(Screen=="pause"){Screen="journal";Session.Paused=true;}}
        private void Update()
        {
            if(SmokeMode)return;
            Frame(Mathf.Min(Time.deltaTime,.05f),true);
        }
        public void Frame(float dt,bool input)
        {
            if(input)
            {
                if(Input.GetKeyDown(KeyCode.Space)&&Screen=="play")Session.Tactical?.Toggle();
                if(Input.GetKeyDown(KeyCode.Return)){if(Screen=="story")Advance();else if(Screen=="title")NewGame(false);else if(Screen=="defeat")Rewind();}
                if(Input.GetKeyDown(KeyCode.Escape)){if(Screen=="play"&&Session.Tactical!=null&&Session.Tactical.Planning)Session.Tactical.Close();else if(Screen=="play")Pause();else if(Screen=="pause"||Screen=="overlook"||Screen=="journal"||Screen=="forms")Resume();}
                if(Input.GetKeyDown(KeyCode.B)){if(Screen=="forms")Resume();else OpenForms();}
                if(Input.GetKeyDown(KeyCode.M)){if(Screen=="overlook")Resume();else Overlook();}
                if(Screen=="defeat"&&Input.GetKeyDown(KeyCode.R))Rewind();
            }
            if(Screen=="story")Story?.Tick(dt);
            Session.Step(dt,input);
            if(Session.Phase==SquadPhase.Defeat && Screen=="play"){Screen="defeat";Session.Paused=true;}
            if(Time.unscaledTime>_noticeUntil && !_saveError)Notice="";
        }
        private void ShowNotice(string key){Notice=Text.Get(key);_noticeUntil=Time.unscaledTime+6;}
        private void SaveError(Exception e){_saveError=true;Notice=Text.Get("st.feedback.saveError");Debug.LogWarning("Squad save: "+e.Message);}
        private void PlaySound(string key){if(_clips.TryGetValue(key,out var clip)&&clip!=null)_sfx.PlayOneShot(clip);}
        public void Persist()
        {
            if(_save==null||Session.Checkpoint==null)return;
            try
            {
                _save.loopCount=Session.LoopCount;_save.tipsUnlocked=new List<string>(_tips);
                _save.SetFlag("st.checkpoint",JsonUtility.ToJson(Session.Checkpoint));
                _save.SetFlag("st.archive",JsonUtility.ToJson(Session.Archive));
                _save.SetFlag("st.completed",_completed?"1":"0");
                foreach(var id in Session.Memories)_save.SetFlag("st.memory."+id,"1");
                _save.advScriptId=Screen=="story"&&Story!=null?Story.ScriptId:"";
                _save.advCommandIndex=Screen=="story"&&Story!=null?Story.CommandIndex:0;
                SaveSystem.Save(_save);HasContinue=true;
            }
            catch(Exception e){SaveError(e);}
        }
        public string Journal()
        {
            var data=Resources.Load<TextAsset>("Tips/tips_zh");
            var catalog=JsonUtility.FromJson<JournalCatalog>(data.text);var lines=new List<string>();
            if(catalog.entries!=null)foreach(var t in catalog.entries)if(_tips.Contains(t.id))lines.Add(t.title+"\n"+t.text);
            return lines.Count>0?string.Join("\n\n",lines):Text.Get("st.journal.empty");
        }
        [Serializable] private class JournalCatalog {public JournalEntry[] entries=Array.Empty<JournalEntry>();}
        [Serializable] private class JournalEntry {public string id="",title="",text="";}
    }
}
