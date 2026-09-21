#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SummerMemories.Action3D.Side2D;
using SummerMemories.Core.Save;
using UnityEngine;
namespace SummerMemories.App
{
    public sealed class SideSmokeTest:MonoBehaviour
    {
        private SideDirector _app;private string _slot;private SideSession S=>_app.Session;private readonly List<string> _passed=new List<string>();
        public void Init(SideDirector app,string slot){_app=app;_slot=slot;}
        private void Check(bool ok,string name){if(!ok)throw new Exception(name);_passed.Add(name);Debug.Log("[SIDE_SMOKE] PASS "+name);}
        private void Tick(float seconds,float move=0)
        {for(var n=0;n<Mathf.CeilToInt(seconds/.02f);n++)S.Step(.02f,new SideInput{move=move,switchTo=-1});}
        private void Place(float x,float y=0){S.Active.position=new Vector2(x,y);S.Active.action=SideAction.Idle;S.Active.velocityY=0;}
        private void Strike(bool heavy=false,bool skill=false){Check(S.Attack(heavy,skill),"attack_"+_passed.Count);Tick(.9f);}
        private IEnumerator Start()
        {
            yield return null;Exception error=null;try{Run();}catch(Exception e){error=e;}
            var args=Environment.GetCommandLineArgs();var at=Array.IndexOf(args,"--side-report");
            if(at>=0&&at+1<args.Length)File.WriteAllText(args[at+1],JsonUtility.ToJson(new Report{success=error==null,passed=_passed.ToArray(),error=error?.ToString()??""},true));
            SaveSystem.Delete(_slot);if(error!=null)Debug.LogError(error);Application.Quit(error==null?0:1);
        }
        private void Run()
        {
            Check(_app.Screen=="title","boot_to_title");_app.StartGame();Check(S.Dialogue=="s2.story.arrival","new_game_has_intro");S.DismissDialogue();Tick(.1f);
            var p=S.Active;var x=p.position.x;Tick(.5f,1);Check(p.position.x>x+2,"horizontal_movement");
            S.Step(.02f,new SideInput{jump=true,switchTo=-1});Tick(.15f);Check(p.position.y>.8f,"physical_jump");Tick(1);Check(p.position.y<.08f,"lands_on_ground");
            Place(6.5f);S.Interact();Check(p.pipe&&S.Flags.Contains("pipe"),"equip_steel_pipe");
            Place(9);S.Interact();Check(!S.Scans.Contains("stone"),"only_ushio_can_scan");Check(S.Switch(1),"switch_character");S.Interact();Check(S.Scans.Contains("stone"),"scan_stone");
            Place(10.7f);Tick(1,1);Check(S.Active.position.x<12,"human_blocked_by_low_tunnel");
            Check(S.Copy(),"copy_stone");Tick(1.5f,1);Check(S.Active.position.x>14,"small_form_enters_tunnel");Check(!S.Uncopy()&&!S.Switch(0),"no_human_restore_inside_tunnel");
            Place(17);S.Interact();Check(S.Flags.Contains("shell")&&S.Memory.Contains("shell"),"shell_investigation");S.DismissDialogue();Tick(1.3f,1);Check(S.Uncopy(),"restore_after_leaving_tunnel");
            Place(21);S.Interact();Check(S.Flags.Contains("tunnel_open"),"open_warehouse_gate");
            Place(4);S.Interact();Check(S.Scans.Contains("watch"),"scan_watch");Check(S.CarryWatch()&&S.ActiveIndex==0&&S.WatchCarried,"watch_carried_by_shinpei");
            S.Dialogue="";Place(32);Tick(.3f,1);S.DismissDialogue();Tick(.5f,1);Check(!S.Flags.Contains("alarm"),"watch_bypasses_patrol_alarm");
            S.Switch(1);Check(!S.WatchCarried,"switch_ushio_releases_watch");S.DismissDialogue();Place(32);var enemy=S.Enemies[0];var hp=enemy.hp;Strike(false,true);Check(enemy.hp<hp&&enemy.bound>0,"hair_binds_enemy");
            Check(S.Switch(2),"switch_to_hizuru_after_bind");Place(enemy.x-1.5f);hp=enemy.hp;Strike(true);Check(enemy.hp<hp-6,"partner_followup_heavy_damage");
            var guard=S.Enemies.Find(e=>e.data.style=="guard");Place(guard.x-2);hp=guard.hp;Strike(true);Check(guard.hp<hp-5,"hammer_breaks_guard");
            S.Active.hp=100;foreach(var e in S.Enemies)
            {
                if(e.data.x>65)continue;
                for(var n=0;n<12&&e.Alive;n++){Place(e.x-1.4f);S.Active.facing=1;Tick(.9f);S.DismissDialogue();S.Attack(true,false);Tick(.85f);S.DismissDialogue();}
            }
            Place(51);Tick(.05f);Check(S.Flags.Contains("battle_clear")&&S.Anchor==53,"battle_opens_gate_and_sets_checkpoint");S.DismissDialogue();
            Place(66);Tick(.1f);Check(S.RescueTime>0&&S.Dialogue!="","rescue_warning_stops_time");var time=S.RescueTime;Tick(2);Check(S.RescueTime==time,"dialogue_pauses_countdown");S.DismissDialogue();Place(60);Tick(36);Check(S.Phase==SidePhase.Defeat&&S.Memory.Contains("collapse"),"timed_failure_records_knowledge");
            var branches=S.Archive.branches.Count;_app.Rewind();S.DismissDialogue();Check(S.Active.position.x==53&&S.Archive.branches.Count==branches+1&&S.Memory.Contains("collapse")&&S.Scans.Contains("stone"),"rewind_restores_anchor_keeps_knowledge_and_all_branches");
            Place(66);Tick(.1f);Check(S.Dialogue=="s2.story.rescue_known","second_loop_uses_known_information");S.DismissDialogue();S.Switch(2);Place(71);Strike(true);Check(S.Flags.Contains("core_broken"),"heavy_destroys_core");
            time=S.RescueTime;Tick(2);Check(S.RescueTime==time,"core_stops_collapse");Place(85);S.Interact();Check(S.Phase==SidePhase.Victory,"rescue_reaches_victory");
            _app.Persist();_app.Title();_app.Continue();Check(S.Phase==SidePhase.Victory&&S.Archive.branches.Count==branches+1,"victory_and_timeline_survive_save_load");
            Check(S.Archive.branches[0].route.Count>3&&S.Archive.branches[0].events.Exists(e=>e.kind=="defeat"),"overlook_has_map_time_and_events");
        }
        [Serializable] private class Report{public bool success;public string[] passed;public string error;}
    }
}
#endif
