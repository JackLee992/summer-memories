#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SummerMemories.Action3D.Squad;
using SummerMemories.Core.Save;
using UnityEngine;

namespace SummerMemories.App
{
    // Drives the same runtime APIs as keyboard play; only fixture positioning/damage is direct.
    public class SquadSmokeTest : MonoBehaviour
    {
        private SquadDemoDirector _app;
        private SquadSession3D S=>_app.Session;
        private string _slot;
        private readonly List<string> _passed=new List<string>();
        public void Init(SquadDemoDirector app,string slot){_app=app;_slot=slot;}
        private void Check(bool condition,string name)
        {if(!condition)throw new Exception(name);_passed.Add(name);Debug.Log("[SQUAD_SMOKE] PASS "+name);}
        private void Tick(float seconds)
        {for(var i=0;i<Mathf.CeilToInt(seconds/.02f);i++){_app.Frame(.02f,false);Physics.SyncTransforms();}}
        private void Dialogue()
        {for(var i=0;i<100&&_app.Screen=="story";i++)_app.Advance();}
        private void Place(SquadActor3D a,Vector3 p){a.Teleport(p,Quaternion.identity);Physics.SyncTransforms();}
        private void Scan(string id)
        {Check(S.BeginScan(id),"begin_scan_"+id);Tick(1.1f);Check(S.Active.Scans.Contains(id),"complete_scan_"+id);}
        private IEnumerator Start()
        {
            yield return null;
            var tests=Run();Exception error=null;
            while(true)
            {
                bool next=false;
                try{next=tests.MoveNext();}catch(Exception e){error=e;}
                if(error!=null||!next)break;
                yield return tests.Current;
            }
            var report=new Report{passed=_passed.ToArray(),success=error==null,error=error?.ToString()??""};
            var args=Environment.GetCommandLineArgs();var index=Array.IndexOf(args,"--squad-report");
            if(index>=0&&index+1<args.Length)File.WriteAllText(args[index+1],JsonUtility.ToJson(report,true));
            try{SaveSystem.Delete(_slot);}catch(Exception e){Debug.LogWarning(e.Message);}
            if(error!=null)Debug.LogError("[SQUAD_SMOKE] FAIL "+error);
            else Debug.Log("[SQUAD_SMOKE] ALL PASS "+_passed.Count);
            Application.Quit(error==null?0:1);
        }
        [Serializable] private class Report{public string[] passed;public bool success;public string error;}
        private IEnumerator Run()
        {
            Check(_app.Screen=="title","boot_title_single_session");
            Check(FindObjectsByType<SquadSession3D>(FindObjectsSortMode.None).Length==1,"single_bootstrap");
            _app.NewGame();Check(_app.Screen=="story","title_to_intro");Dialogue();Check(_app.Screen=="play","intro_to_party_exploration");
            Check(S.Party.Count==3,"three_bodies_four_characters");
            var shinpei=S.Party[0];var ushio=S.Party[1];var hizuru=S.Party[2];
            // The playable opening tests equipment, physical jumping and the first real fight.
            Place(shinpei,new Vector3(1.5f,0,6));S.Interact();
            Check(shinpei.HasPipe&&shinpei.PipeEquipped,"pickup_equips_weapon_and_model");
            shinpei.ToggleWeapon();Check(!shinpei.PipeEquipped,"weapon_toggle_to_fists");shinpei.ToggleWeapon();
            S.SetOrder(AllyOrder.Hold);Place(shinpei,new Vector3(0,0,6.7f));Tick(.1f);
            Check(shinpei.Jump(),"grounded_jump_starts");var apex=0f;
            for(var j=0;j<45;j++){shinpei.Move(Vector3.forward,false,.02f);Tick(.02f);apex=Mathf.Max(apex,shinpei.transform.position.y);}
            Check(apex>.85f&&shinpei.transform.position.z>9,"jump_clears_real_low_obstacle");Tick(.5f);
            Check(shinpei.transform.position.y<.15f,"jump_lands_on_ground");
            S.SetOrder(AllyOrder.Focus);
            for(var j=0;j<600&&S.Enemies.Exists(e=>e.Alive);j++)
            {
                var enemy=S.NearestEnemy(shinpei.transform.position,100);var direction=enemy.transform.position-shinpei.transform.position;direction.y=0;
                shinpei.Move(direction.normalized,false,.02f);if(direction.magnitude<3)S.Attack(false,j%100==0);Tick(.02f);
            }
            Check(S.Enemies.TrueForAll(e=>!e.Alive),"opening_patrol_defeated_with_equipped_weapon");Tick(1);
            Check(shinpei.PipeEquipped,"combat_does_not_drop_weapon");
            shinpei.Damage(3);Tick(.5f);var hp=shinpei.Hp;
            Check(S.Switch(1),"switch_to_ushio");Check(S.Switch(0)&&shinpei.Hp==hp,"switch_preserves_individual_hp");
            S.SetOrder(AllyOrder.Hold);var hold=ushio.transform.position;
            for(var i=0;i<80;i++){shinpei.Move(Vector3.forward,false,.02f);Tick(.02f);}
            Check(Vector3.Distance(hold,ushio.transform.position)<.15f,"hold_position");
            S.SetOrder(AllyOrder.Follow);Tick(2);Check(Vector3.Distance(hold,ushio.transform.position)>.5f,"follow_moves_companion");
            S.SetOrder(AllyOrder.Hold);Check(S.Switch(1),"switch_for_scanning");
            Check(!S.Morph("st_object_stone"),"unscanned_form_rejected");
            Place(ushio,new Vector3(-3,0,10));Check(S.BeginScan("st_object_stone"),"scan_starts");
            ushio.Damage(1);Tick(.1f);Check(S.ScanningId==null&&!ushio.Scans.Contains("st_object_stone"),"damage_interrupts_scan");Tick(.5f);
            Scan("st_object_stone");
            Place(ushio,new Vector3(3,0,8));Scan("st_object_crate");
            Place(shinpei,new Vector3(0,0,6));Place(ushio,new Vector3(1,0,6));Scan("st_object_watch");
            Check(S.Morph("st_object_watch")&&S.Active==shinpei&&ushio.Carried,"watch_attaches_and_controls_carrier");
            for(var i=0;i<60;i++){shinpei.Move(Vector3.forward,false,.02f);Tick(.02f);}
            Check(Vector3.Distance(ushio.transform.position,shinpei.transform.position)<1.3f&&!ushio.Controller.enabled,"watch_tracks_carrier_without_independent_collision");
            Check(S.Switch(1)&&!ushio.Carried&&ushio.Controller.enabled,"watch_safe_release");Tick(6.1f);
            Place(ushio,new Vector3(-6,0,16.5f));Check(S.Morph("st_object_stone"),"stone_transform");
            Check(ushio.Controller.height<.4f,"stone_has_small_collision");
            for(var i=0;i<145;i++){ushio.Move(Vector3.forward,false,.02f);Tick(.02f);}
            Check(ushio.transform.position.z>21.3f,"small_form_physically_passes_tunnel");
            Check(!ushio.Unmorph()&&!S.Switch(0),"tunnel_prevents_unsafe_unmorph_and_switch");
            S.Interact();Check(S.Collected.Contains("st_shell_clue"),"shell_inside_tunnel_collected");
            for(var i=0;i<165;i++){ushio.Move(Vector3.back,false,.02f);Tick(.02f);}
            Check(ushio.Unmorph(),"can_exit_tunnel_and_unmorph");
            yield return null;
            Check(S.Switch(2),"switch_to_hizuru");var shared=hizuru.Hp;S.ToggleConsciousness();
            Check(hizuru.Ryunosuke&&hizuru.Hp==shared,"ryunosuke_shares_body_hp");S.ToggleConsciousness();Check(!hizuru.Ryunosuke,"return_to_hizuru");
            Place(hizuru,new Vector3(0,0,16));S.Interact();Check(S.Collected.Contains("st_shadow_clue"),"shadow_investigation");
            Place(hizuru,new Vector3(5,0,25));S.Interact();Check(S.Collected.Contains("st_hair_clue"),"hair_investigation");
            S.Switch(0);Place(shinpei,new Vector3(0,0,33));S.RefreshContext();Check(S.PromptKey=="st.prompt.encounter","wearable_scan_does_not_steal_encounter_prompt");S.Interact();Check(S.Phase==SquadPhase.Fight&&S.Enemies.Count==3,"investigation_to_encounter");
            Check(S.Checkpoint.actors[1].scans.Count==3,"checkpoint_captures_scan_library");
            S.SetOrder(AllyOrder.Hold);S.Switch(1);Place(ushio,new Vector3(-3,0,35));
            Check(S.Attack(true),"hair_attack_started");Tick(.85f);
            Check(S.Enemies[0].Hp<S.Enemies[0].Config.hp&&S.Enemies[0].Bound,"hair_damages_and_interrupts_actual_enemy");
            var cooldown=ushio.HairCooldown;Check(S.Switch(2)&&S.Switch(1)&&ushio.HairCooldown==cooldown,"switch_retains_skill_cooldown");
            S.Switch(2);S.ToggleConsciousness();Place(hizuru,S.Enemies[1].transform.position+Vector3.back*2.5f);
            Check(S.Attack(true),"ryunosuke_pressure_started");Tick(.85f);Check(S.Enemies[1].Bound,"ryunosuke_interrupts_enemy");
            // Camouflage target selection, with other party members out of detection range.
            S.Switch(1);Place(shinpei,new Vector3(9,0,4));Place(hizuru,new Vector3(-9,0,4));Place(ushio,new Vector3(0,0,42));
            Check(S.Morph("st_object_crate"),"crate_transform");Tick(.3f);
            Check(S.Enemies.TrueForAll(e=>e.Target!=ushio),"stationary_disguise_not_acquired");
            ushio.Move(Vector3.right,false,.02f);Tick(.02f);
            Check(!ushio.Hidden,"moving_disguise_reveals");
            shinpei.Damage(100);Tick(.1f);Check(_app.Screen=="defeat","shinpei_death_reaches_failure");
            for(var repeat=0;repeat<20;repeat++)
            {
                _app.Rewind();Tick(.8f);Dialogue();
                if(S.Phase!=SquadPhase.Fight||S.Party.Exists(a=>!a.Alive||a.Form!=null)||S.Enemies.Exists(e=>e.Hp!=e.Config.hp)||S.Order!=AllyOrder.Follow)throw new Exception("Rewind failed at "+repeat);
                foreach(var enemy in S.Enemies)if(Vector3.Distance(enemy.transform.position,SquadConfig.Position(enemy.Config.spawn))>.1f)throw new Exception("Enemy rewind position drift: "+enemy.transform.position);
                if(repeat%5==0)yield return null;
            }
            Check(S.LoopCount==20&&S.Memories.Count==4,"twenty_rewinds_restore_party_world_and_keep_memory");
            Check(S.Archive.branches.Count==21&&S.Archive.branches[0].events.Exists(e=>e.kind=="clue")&&S.Archive.branches[0].route.Count>5,"overlook_keeps_all_experienced_branches_events_and_map");
            var oldTime=S.Archive.branches[0].elapsed;
            _app.Overlook();var currentTime=S.Archive.Current.elapsed;Tick(1);
            Check(_app.Screen=="overlook"&&S.Archive.Current.elapsed==currentTime,"overlook_pauses_world_time");_app.Resume();
            _app.Persist();_app.Title();_app.Continue();Dialogue();
            Check(S.Phase==SquadPhase.Fight&&S.LoopCount==20&&S.Party[1].Scans.Count==3,"save_continue_restores_battle_anchor");
            Check(S.Archive.branches.Count==22&&S.Archive.branches[0].elapsed==oldTime,"overlook_survives_save_load_and_branches_at_anchor");
            CombatTests();
            if(S.Config.presentation.isometric)TacticalTests();
            // Actual combat commands, no direct enemy HP mutation. AI Focus contributes damage.
            S.Switch(1);S.SetOrder(AllyOrder.Focus);
            var steps=0;
            while(S.Enemies.Exists(e=>e.Alive)&&S.Phase==SquadPhase.Fight&&steps++<1800)
            {
                var enemy=S.NearestEnemy(S.Active.transform.position,100);
                var d=enemy.transform.position-S.Active.transform.position;d.y=0;
                S.Active.Move(d.normalized,false,.02f);
                if(S.Active.HairCooldown<=0&&d.magnitude<6)S.Attack(true);
                else if(d.magnitude<2.3f)S.Attack(false);
                _app.Frame(.02f,false);Physics.SyncTransforms();
                if(steps%100==0)yield return null;
            }
            if(S.Phase!=SquadPhase.Fight||S.Enemies.Exists(e=>e.Alive))
            {
                foreach(var a in S.Party)Debug.Log("[SQUAD_DIAG] actor "+a.Config.id+" hp="+a.Hp+" pos="+a.transform.position+" action="+a.Action);
                foreach(var e in S.Enemies)Debug.Log("[SQUAD_DIAG] enemy "+e.Config.id+" hp="+e.Hp+" pos="+e.transform.position);
                Debug.Log("[SQUAD_DIAG] phase="+S.Phase+" steps="+steps);
            }
            Check(S.Phase==SquadPhase.Fight&&S.Enemies.TrueForAll(e=>!e.Alive),"win_encounter_using_combat_and_focus_ai");
            Tick(1); // Let the final attack recover before the player can interact.
            S.Switch(0);Place(shinpei,SquadConfig.Position(S.Config.world.exit));S.RefreshContext();Check(S.PromptKey=="st.prompt.exit","wearable_scan_does_not_steal_exit_prompt");S.Interact();Check(_app.Screen=="victory","fight_to_mio_victory");
            _app.Title();_app.Continue();Check(_app.Screen=="victory","victory_persists_through_title_and_continue");
            SaveTests();
            Check(!_app.Journal().Contains("st_tip_"),"journal_content_resolves");
        }
        private void CombatTests()
        {
            var checkpoint=S.Checkpoint;S.Restore(checkpoint);S.SetOrder(AllyOrder.Hold);S.Switch(0);
            var player=S.Party[0];var ushio=S.Party[1];var guard=S.Enemies[2];
            Place(player,guard.transform.position+Vector3.forward*2);
            var front=guard.ReceiveStrike(player,2,false,false,out _);
            Place(player,guard.transform.position+Vector3.back*2);
            var rear=guard.ReceiveStrike(player,2,false,false,out _);
            Check(rear>front*2,"guard_front_resists_and_flank_bypasses");
            guard.ReceiveStrike(ushio,1,false,true,out _);
            Check(guard.LinkReady&&guard.Bound,"hair_creates_real_followup_window");
            var own=guard.ReceiveStrike(ushio,2,false,false,out var same);
            Check(!same&&guard.LinkReady,"same_actor_cannot_consume_team_followup");
            var partner=guard.ReceiveStrike(player,2,false,false,out var linked);
            Check(linked&&partner>own*1.9f&&!guard.LinkReady,"switch_partner_consumes_double_damage_followup");
            S.Restore(checkpoint);S.SetOrder(AllyOrder.Hold);S.Switch(0);player=S.Active;
            Place(player,new Vector3(0,0,28));Tick(.1f);var hp=player.Hp;
            Check(player.Dodge(Vector3.right),"dodge_begins_with_stamina_cost");Tick(.04f);player.ReceiveEnemyHit(3);
            Check(player.Hp==hp&&player.CounterReady,"precise_dodge_avoids_hit_and_arms_counter");
            Tick(.32f);player.ReceiveEnemyHit(2);
            Check(player.Hp<hp,"dodge_recovery_is_vulnerable");
            Tick(.5f);S.Attack(false);Tick(.22f);
            Check(!player.CounterReady&&player.CanYield,"counter_consumed_and_switch_allowed_after_impact");
            Check(S.Switch(1),"can_switch_during_attack_recovery");
            S.Restore(checkpoint);S.SetOrder(AllyOrder.Hold);S.Switch(0);player=S.Active;
            var sweep=S.Enemies[1];
            Place(S.Party[1],new Vector3(-9,0,4));Place(S.Party[2],new Vector3(9,0,4));
            Place(player,sweep.transform.position+Vector3.forward*2.5f);Tick(.04f);
            for(var n=0;n<100&&!sweep.Winding;n++)Tick(.02f);
            Check(sweep.Winding&&sweep.Threatens(player),"sweep_commits_to_visible_threat_area");
            for(var n=0;n<100&&sweep.WindupRemaining>.31f;n++)Tick(.02f);
            hp=player.Hp;Check(player.Jump(),"jump_can_answer_sweep_telegraph");Tick(.55f);
            Check(player.Hp==hp&&sweep.Recovering,"jump_avoids_sweep_and_enemy_enters_punishable_recovery");
            S.Restore(checkpoint);S.SetOrder(AllyOrder.Hold);S.Switch(0);
            guard=S.Enemies[2];player=S.Active;Place(player,guard.transform.position+Vector3.forward*2);
            guard.ReceiveStrike(player,1,true,false,out _);guard.ReceiveStrike(player,1,true,false,out _);guard.ReceiveStrike(player,1,true,false,out _);
            Check(guard.Bound&&guard.Posture==0,"heavy_attacks_break_guard_posture");
            S.Restore(checkpoint);
        }
        private void TacticalTests()
        {
            var checkpoint=S.Checkpoint;S.Restore(checkpoint);S.SetOrder(AllyOrder.Hold);S.Switch(0);
            var t=S.Tactical;var p=S.Party[0];var u=S.Party[1];
            Check(t!=null&&S.CameraRig.Isometric,"25d_orthographic_camera_configured");
            Check(p.GetComponentsInChildren<SquadSpriteBody>(true).Length==1,"25d_illustrated_character_loaded");
            Check(S.Config.saveSlot=="st_demo25d","25d_isolated_play_save");
            Place(p,new Vector3(0,0,5));Place(u,new Vector3(-3,0,10));S.Switch(1);S.BeginScan("st_object_stone");
            var scan=S.ScanProgress;var time=S.Archive.Current.elapsed;var hp=p.Hp;var enemy=S.Enemies[0].transform.position;var cooldown=u.HairCooldown;
            t.Toggle();Tick(2);
            Check(t.Planning&&S.Archive.Current.elapsed==time&&S.ScanProgress==scan&&p.Hp==hp&&u.HairCooldown==cooldown&&S.Enemies[0].transform.position==enemy,"25d_planning_freezes_battle_scan_cooldown_and_time");
            Check(t.Issue(0,new Vector3(0,0,10.5f)),"25d_queue_route_around_jump_obstacle");
            Check(t.Issue(1,new Vector3(-5,0,13)),"25d_independent_second_actor_order");
            var before=p.transform.position;Tick(.5f);Check(p.transform.position==before,"25d_queued_orders_do_not_move_during_planning");
            Check(!t.Issue(0,new Vector3(0,0,8)),"25d_reject_destination_inside_solid");
            t.Close();Tick(5);
            Check(Vector3.Distance(p.transform.position,new Vector3(0,0,10.5f))<.7f&&Vector3.Distance(u.transform.position,new Vector3(-5,0,13))<.7f,"25d_two_orders_execute_and_route_around_obstacle");
            S.Restore(checkpoint);Check(!t.HasOrder(0)&&!t.HasOrder(1)&&!t.Planning,"25d_restore_clears_deployment");
            S.SetOrder(AllyOrder.Hold);S.Switch(1);Place(u,new Vector3(-3,0,35));var target=S.Enemies[0];hp=target.Hp;
            t.Toggle();t.Select(1);Check(t.Issue(1,target.transform.position,target,TacticalAction.Skill),"25d_queue_hair_against_enemy");
            Tick(1);Check(target.Hp==hp,"25d_attack_waits_for_time_resume");
            t.Close();Tick(.9f);Check(target.Hp<hp&&target.Bound,"25d_queued_hair_hits_and_binds_actual_enemy");
            t.Toggle();S.Rewind();Check(!t.HasOrder(1)&&!t.Planning,"25d_rewind_clears_plans");Tick(.8f);Dialogue();S.Restore(checkpoint);
        }
        private void SaveTests()
        {
            var name="st_test_"+Guid.NewGuid().ToString("N");
            var dir=Path.Combine(Application.persistentDataPath,"saves");var path=Path.Combine(dir,name+".json");
            try
            {
                var data=new SaveSlot{slotName=name};data.SetFlag("test","one");SaveSystem.Save(data);data.SetFlag("test","two");SaveSystem.Save(data);
                Check(SaveSystem.Load(name).GetFlag("test")=="two","atomic_replace_on_current_runtime");
                File.WriteAllText(path,"{ broken json");Check(SaveSystem.Load(name)==null,"corrupt_save_returns_empty");
                Check(!File.Exists(path)&&Directory.GetFiles(dir,name+".json.broken-*").Length==1,"corrupt_save_is_quarantined");
                SaveSystem.Save(data);Check(SaveSystem.Load(name)!=null,"new_save_after_quarantine");
                Check(SaveSystem.HasSave(_slot),"test_does_not_touch_play_slot");
            }
            finally
            {
                SaveSystem.Delete(name);foreach(var f in Directory.GetFiles(dir,name+".json.broken-*"))File.Delete(f);
            }
        }
    }
}
#endif
