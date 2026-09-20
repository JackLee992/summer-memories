using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SummerMemories.Action3D
{
    /// <summary>
    /// 3D 动作垂直切片总控：开场电影 → 战斗 → 死亡回潮演出 → 胜利/败北/重开。
    /// 零场景、纯代码生成世界/角色/相机/HUD，命令行即可构建。
    /// </summary>
    public class Game3DDirector : MonoBehaviour
    {
        public enum GamePhase { Intro, Fight, Rewind, Victory, Defeat }

        public GamePhase Phase = GamePhase.Intro;
        public float IntroT;
        public float IntroDur = 3.4f;
        public float RewindT;

        public Combatant3D Player;
        public PlayerController3D PlayerCtl;
        public readonly Rewind3D Rewind = new Rewind3D();
        public readonly List<Combatant3D> Enemies = new List<Combatant3D>();
        public readonly List<Enemy3D> EnemyAi = new List<Enemy3D>();
        public CameraRig3D Rig;
        public GameFeel3D Feel;
        public Camera RigCam => Rig != null ? Rig.Camera : null;

        private const int CharLayer = 8;
        private WorldBuilder3D.World _world;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            _world = WorldBuilder3D.Build();

            // 玩家
            var pgo = new GameObject("Player_Shitou");
            pgo.transform.SetParent(transform, true);
            pgo.transform.position = _world.PlayerSpawn;
            pgo.transform.rotation = Quaternion.LookRotation(Vector3.back);
            var pctl = pgo.AddComponent<PlayerController3D>();
            Player = pgo.GetComponent<Combatant3D>();
            PlayerCtl = pctl;
            SetLayerRecursive(pgo, CharLayer);

            // 敌人
            var hp = new[] { 2f, 2f, 3f };
            var spd = new[] { 2.5f, 2.9f, 2.2f };
            var dmg = new[] { 1f, 1f, 1f };
            var sc = new[] { 1f, 0.95f, 1.14f };
            for (var i = 0; i < _world.EnemySpawns.Length; i++)
            {
                var ego = new GameObject("Yecha_" + i);
                ego.transform.SetParent(transform, true);
                ego.transform.position = _world.EnemySpawns[i];
                ego.transform.rotation =
                    Quaternion.LookRotation((_world.PlayerSpawn - _world.EnemySpawns[i]).normalized);
                var eai = ego.AddComponent<Enemy3D>();
                var ec = ego.GetComponent<Combatant3D>();
                eai.Init(Player, hp[i], spd[i], dmg[i], sc[i]);
                Enemies.Add(ec);
                EnemyAi.Add(eai);
                SetLayerRecursive(ego, CharLayer);
            }

            // 相机
            var cgo = new GameObject("CameraRig");
            cgo.transform.SetParent(transform, true);
            Rig = cgo.AddComponent<CameraRig3D>();
            Rig.Init(Player.transform);
            PlayerCtl.Rig = Rig;
            PlayerCtl.Enemies = Enemies;

            // 打击感
            Feel = gameObject.AddComponent<GameFeel3D>();
            Feel.Camera = Rig.Camera;
            Feel.Rig = Rig;

            // HUD
            var hgo = new GameObject("HUD");
            hgo.transform.SetParent(transform, true);
            hgo.AddComponent<Hud3D>().D = this;

            // 回潮开局快照
            Rewind.Capture(Player, Enemies, EnemyAi);

            // 开场：冻结战斗，导演控相机
            Phase = GamePhase.Intro;
            IntroT = 0f;
            PlayerCtl.enabled = false;
            foreach (var e in EnemyAi) e.enabled = false;
            Rig.enabled = false;
        }

        private void Update()
        {
            // Build 失败兜底：世界未建成时不再每帧空转报错
            if (Rig == null || Player == null) { Input3D.EndFrame(); return; }
            switch (Phase)
            {
                case GamePhase.Intro: TickIntro(); break;
                case GamePhase.Fight: TickFight(); break;
                case GamePhase.Rewind: break; // 由协程推进
                case GamePhase.Victory:
                case GamePhase.Defeat:
                    if (Input3D.Pressed(Input3D.Btn.Confirm)) Restart();
                    break;
            }
            Input3D.EndFrame();
        }

        private void TickIntro()
        {
            IntroT += Time.deltaTime;
            if (Input3D.Pressed(Input3D.Btn.Confirm)) IntroT = IntroDur;

            var cam = Rig.Camera;
            var pp = Player.transform.position + Vector3.up * 1.4f;
            var fwd = Player.transform.forward;
            var front = Player.transform.position + fwd * 5f + Vector3.up * 2.3f;
            var behind = Player.transform.position - fwd * 5.2f + Vector3.up * 2.3f;
            var k = Mathf.Clamp01(IntroT / IntroDur);
            var ease = Mathf.SmoothStep(0f, 1f, k);
            cam.transform.position = Vector3.Lerp(front, behind, ease);
            cam.transform.LookAt(pp);

            if (IntroT >= IntroDur) StartFight();
        }

        private void StartFight()
        {
            Phase = GamePhase.Fight;
            PlayerCtl.enabled = true;
            foreach (var e in EnemyAi) e.enabled = true;
            Rig.enabled = true;
        }

        private void TickFight()
        {
            if (Player.Dead)
            {
                if (Rewind.Charges > 0) StartCoroutine(DoRewind());
                else EnterDefeat();
                return;
            }
            if (AliveEnemies() == 0) EnterVictory();
        }

        public int AliveEnemies()
        {
            var n = 0;
            foreach (var e in Enemies) if (!e.Dead) n++;
            return n;
        }

        private IEnumerator DoRewind()
        {
            Phase = GamePhase.Rewind;
            RewindT = 0f;
            Rewind.Spend();

            PlayerCtl.enabled = false;
            foreach (var e in EnemyAi) e.enabled = false;
            var ccs = new List<CharacterController> { Player.GetComponent<CharacterController>() };
            foreach (var e in Enemies) ccs.Add(e.GetComponent<CharacterController>());
            foreach (var c in ccs) c.enabled = false;

            Feel.Suspended = true;
            Time.timeScale = 0.12f;
            var t = 0f;
            while (t < 1.7f)
            {
                t += Time.unscaledDeltaTime;
                RewindT = t / 1.7f;
                Rig.AddShake(0.015f);
                yield return null;
            }

            Rewind.Restore(Player, PlayerCtl, Enemies, EnemyAi);
            PlayerCtl.ResetState();

            Time.timeScale = 1f;
            Feel.Suspended = false;
            RewindT = 0f;
            foreach (var c in ccs) c.enabled = true;
            PlayerCtl.enabled = true;
            foreach (var e in EnemyAi) e.enabled = true;
            Phase = GamePhase.Fight;
        }

        private void EnterVictory()
        {
            Phase = GamePhase.Victory;
            foreach (var e in EnemyAi) e.enabled = false;
            StartCoroutine(SlowMo());
        }

        private IEnumerator SlowMo()
        {
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(1.3f);
            Time.timeScale = 1f;
        }

        private void EnterDefeat()
        {
            Phase = GamePhase.Defeat;
            PlayerCtl.enabled = false;
            foreach (var e in EnemyAi) e.enabled = false;
        }

        private void Restart()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            Feel.Suspended = false;
            Rewind.Charges = Rewind3D.MaxCharges;
            Rewind.Restore(Player, PlayerCtl, Enemies, EnemyAi);
            PlayerCtl.ResetState();
            Player.GetComponent<CharacterController>().enabled = true;
            foreach (var e in Enemies) e.GetComponent<CharacterController>().enabled = true;
            PlayerCtl.enabled = true;
            foreach (var e in EnemyAi) e.enabled = true;
            Rig.enabled = true;
            RewindT = 0f;
            Phase = GamePhase.Fight;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }
    }
}
