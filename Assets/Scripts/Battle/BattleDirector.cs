using System;
using System.Collections.Generic;
using System.Linq;
using SummerMemories.Core;

namespace SummerMemories.Battle
{
    /// <summary>
    /// 战棋回合导演（纯逻辑，无 MonoBehaviour）：
    /// 玩家阶段（移动/攻击/钉击/待机，行动前压快照）→ 敌人意图预告 → 敌人执行
    /// → 胜负判定。石头阵亡即败（外层回潮由 App 层接管）。
    /// </summary>
    public class BattleDirector
    {
        public const string HeroId = "shitou";

        public BattleConfig Config { get; private set; }
        public List<BattleUnit> Units { get; private set; } = new List<BattleUnit>();
        public List<EnemyIntent> Intents { get; private set; } = new List<EnemyIntent>();
        public List<string> Log { get; } = new List<string>();
        public BattlePhase Phase { get; private set; } = BattlePhase.Player;
        public int TurnNumber { get; private set; } = 1;
        public int NailsLeft { get; private set; }
        public RewindSystem Rewind { get; private set; }

        public BattleUnit Selected { get; private set; }
        public List<Cell> MoveCells { get; private set; } = new List<Cell>();
        public List<BattleUnit> AttackTargets { get; private set; } = new List<BattleUnit>();

        /// <summary>火眼金睛：本回合是否已窥见敌人意图（每回合限一次）。</summary>
        public bool ForesightActive { get; private set; }
        private int _foresightUsedTurn = -1;
        public bool CanForesight => Phase == BattlePhase.Player && _foresightUsedTurn != TurnNumber;

        public event Action OnChanged;
        public event Action OnVictory;
        public event Action OnDefeat;

        public void Init(BattleConfig config)
        {
            Config = config;
            Units = config.units.Select(u => new BattleUnit(u)).ToList();
            NailsLeft = config.nails;
            Rewind = new RewindSystem(config.rewindCharges);
            Phase = BattlePhase.Player;
            TurnNumber = 1;
            Log.Clear();
            AddLog(config.introLine);
            AddLog($"第 {TurnNumber} 回合 · 我方行动");
            PushSnapshot(); // 初始局面，保证第一次回溯有效
            Select(null);
        }

        public IEnumerable<BattleUnit> AliveAllies()
            => Units.Where(u => u.Alive && u.Team == Team.Ally);
        public IEnumerable<BattleUnit> AliveShadows()
            => Units.Where(u => u.Alive && u.Team == Team.Shadow);

        private void AddLog(string msg)
        {
            if (!string.IsNullOrEmpty(msg)) Log.Add(msg);
        }

        public void Select(BattleUnit unit)
        {
            Selected = unit;
            MoveCells.Clear();
            AttackTargets.Clear();
            if (unit != null && unit.Team == Team.Ally && !unit.Acted && !unit.Stunned
                && Phase == BattlePhase.Player)
            {
                MoveCells = Grid.Reachable(Config, Units, unit, unit.MoveRange);
                AttackTargets = Grid.TargetsInRange(Units, unit, Team.Shadow);
            }
            OnChanged?.Invoke();
        }

        public bool CanMoveTo(int x, int y)
            => MoveCells.Any(c => c.X == x && c.Y == y);

        public bool CanAttack(BattleUnit target)
            => Selected != null && AttackTargets.Contains(target);

        private void PushSnapshot()
        {
            // 任何落子都会使旧的预判失效
            ForesightActive = false;
            var snap = new BattleSnapshot
            {
                TurnNumber = TurnNumber,
                NailsLeft = NailsLeft,
                SelectedUnitId = Selected != null ? Selected.Id : null,
                Units = Units.Select(u => u.Clone()).ToList()
            };
            Rewind.Push(snap);
        }

        private void RestoreSnapshot(BattleSnapshot snap)
        {
            Units = snap.Units.Select(u => u.Clone()).ToList();
            TurnNumber = snap.TurnNumber;
            NailsLeft = snap.NailsLeft;
            ForesightActive = false;
            Phase = BattlePhase.Player;
            Intents.Clear();
            var sel = string.IsNullOrEmpty(snap.SelectedUnitId) ? null
                : Units.FirstOrDefault(u => u.Id == snap.SelectedUnitId);
            Select(sel);
            AddLog("◈ 拔下一根救命毫毛，战局倒退回上一个决策点");
        }

        public bool TryRewind()
        {
            if (!Rewind.CanRewind) { AddLog("时之沙已经用尽"); OnChanged?.Invoke(); return false; }
            var snap = Rewind.Rewind();
            RestoreSnapshot(snap);
            return true;
        }

        public void Move(BattleUnit unit, int x, int y)
        {
            if (unit != Selected || !CanMoveTo(x, y)) return;
            PushSnapshot();
            AddLog($"{unit.DisplayName} 移动到 ({x},{y})");
            unit.X = x; unit.Y = y;
            unit.Acted = true;
            Select(null);
            CheckResult();
        }

        public void Attack(BattleUnit attacker, BattleUnit target)
        {
            if (attacker != Selected || !CanAttack(target)) return;
            PushSnapshot();
            var skillName = attacker.Skill == UnitSkill.HairShot ? "衔石投石" : "攻击";
            AddLog($"{attacker.DisplayName} 使用{skillName}，命中 {target.DisplayName}（-{attacker.Atk}HP）");
            target.Hp -= attacker.Atk;
            if (target.Hp <= 0)
            {
                target.Alive = false;
                AddLog($"{target.DisplayName} 化为黑泥消散");
            }
            attacker.Acted = true;
            Select(null);
            CheckResult();
        }

        /// <summary>桃橛：石头对相邻夜叉/罔两使用桃橛，使其下回合眩晕（无法行动）。</summary>
        public bool TryNail(BattleUnit target)
        {
            if (Selected == null || Selected.Skill != UnitSkill.Nail) return false;
            if (NailsLeft <= 0) { AddLog("桃橛用完了"); OnChanged?.Invoke(); return false; }
            if (target == null || !target.Alive || target.Team != Team.Shadow) return false;
            if (Grid.Manhattan(Selected.X, Selected.Y, target.X, target.Y) > 1)
            { AddLog("桃橛只能钉相邻的夜叉"); OnChanged?.Invoke(); return false; }
            PushSnapshot();
            NailsLeft--;
            target.Stunned = true;
            AddLog($"石头以桃橛钉入 {target.DisplayName} 的影中，它被定在原地！（剩余桃橛 {NailsLeft}）");
            Selected.Acted = true;
            Select(null);
            return true;
        }

        public void Wait(BattleUnit unit)
        {
            if (unit != Selected) return;
            PushSnapshot();
            AddLog($"{unit.DisplayName} 原地待机");
            unit.Acted = true;
            Select(null);
        }

        public bool AllAlliesActed()
            => AliveAllies().All(u => u.Acted || u.Stunned);

        /// <summary>火眼金睛：玩家阶段提前窥见敌人本回合意图，每回合一次，不推进时间。</summary>
        public bool RevealIntents()
        {
            if (!CanForesight) return false;
            _foresightUsedTurn = TurnNumber;
            Intents = ComputeIntents();
            ForesightActive = true;
            AddLog("◈ 火眼金睛开——夜叉的杀意与去向无所遁形");
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>玩家结束回合：计算并暴露敌人意图（视图预告后调用 ExecuteEnemyTurns）。</summary>
        public void BeginIntentPreview()
        {
            if (Phase != BattlePhase.Player) return;
            Intents = ComputeIntents();
            Phase = BattlePhase.IntentPreview;
            AddLog("—— 影子的意图浮现 ——");
            OnChanged?.Invoke();
        }

        public List<EnemyIntent> ComputeIntents()
        {
            var intents = new List<EnemyIntent>();
            foreach (var enemy in Units.Where(u => u.Alive && u.Team == Team.Shadow))
            {
                var intent = new EnemyIntent
                {
                    UnitId = enemy.Id, FromX = enemy.X, FromY = enemy.Y,
                    ToX = enemy.X, ToY = enemy.Y
                };
                if (enemy.Stunned)
                {
                    intents.Add(intent);
                    continue;
                }
                var target = NearestAlly(enemy);
                if (target == null) { intents.Add(intent); continue; }
                var (tx, ty) = StepToward(enemy, target, enemy.MoveRange);
                intent.ToX = tx; intent.ToY = ty;
                if (Grid.Manhattan(tx, ty, target.X, target.Y) <= enemy.AttackRange)
                {
                    intent.Attacks = true;
                    intent.TargetX = target.X; intent.TargetY = target.Y;
                    intent.TargetId = target.Id;
                }
                intents.Add(intent);
            }
            return intents;
        }

        public void ExecuteEnemyTurns()
        {
            if (Phase != BattlePhase.IntentPreview) return;
            Phase = BattlePhase.EnemyActing;
            AddLog("—— 影子行动 ——");
            foreach (var intent in Intents)
            {
                var enemy = Units.FirstOrDefault(u => u.Id == intent.UnitId);
                if (enemy == null || !enemy.Alive) continue;
                if (enemy.Stunned) { AddLog($"{enemy.DisplayName} 被桃橛定住，无法行动"); continue; }
                enemy.X = intent.ToX; enemy.Y = intent.ToY;
                if (intent.Attacks)
                {
                    var target = Units.FirstOrDefault(u => u.Id == intent.TargetId);
                    if (target != null && target.Alive
                        && Grid.Manhattan(enemy.X, enemy.Y, target.X, target.Y) <= enemy.AttackRange)
                    {
                        AddLog($"{enemy.DisplayName} 袭击 {target.DisplayName}（-{enemy.Atk}HP）");
                        target.Hp -= enemy.Atk;
                        if (target.Hp <= 0)
                        {
                            target.Alive = false;
                            AddLog($"{target.DisplayName} 倒下了……");
                        }
                    }
                }
                if (CheckResult()) return;
            }
            // 新回合
            TurnNumber++;
            foreach (var u in Units)
            {
                u.Acted = false;
                u.Stunned = false; // 桃橛只持续敌人行动阶段
            }
            Intents.Clear();
            ForesightActive = false;
            Phase = BattlePhase.Player;
            AddLog($"第 {TurnNumber} 回合 · 我方行动");
            Select(null);
            CheckResult();
        }

        private BattleUnit NearestAlly(BattleUnit enemy)
        {
            BattleUnit best = null;
            var bestD = int.MaxValue;
            foreach (var a in AliveAllies())
            {
                var d = Grid.Manhattan(enemy.X, enemy.Y, a.X, a.Y);
                if (d < bestD) { bestD = d; best = a; }
            }
            return best;
        }

        /// <summary>沿最短路径向目标移动最多 step 步，返回最终坐标。</summary>
        private (int, int) StepToward(BattleUnit enemy, BattleUnit target, int steps)
        {
            var x = enemy.X; var y = enemy.Y;
            var dirs = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            for (var s = 0; s < steps; s++)
            {
                if (Grid.Manhattan(x, y, target.X, target.Y) <= enemy.AttackRange) break;
                var best = (x, y);
                var bestD = Grid.Manhattan(x, y, target.X, target.Y);
                foreach (var (dx, dy) in dirs)
                {
                    var nx = x + dx; var ny = y + dy;
                    if (!Grid.InBounds(Config, nx, ny)) continue;
                    var occ = Grid.UnitAt(Units, nx, ny);
                    if (occ != null && occ.Id != enemy.Id) continue;
                    var d = Grid.Manhattan(nx, ny, target.X, target.Y);
                    if (d < bestD) { bestD = d; best = (nx, ny); }
                }
                if (best == (x, y)) break;
                x = best.Item1; y = best.Item2;
            }
            return (x, y);
        }

        /// <summary>返回 true 表示战斗已结束。</summary>
        private bool CheckResult()
        {
            if (!AliveShadows().Any())
            {
                Phase = BattlePhase.Victory;
                AddLog("战斗胜利。" + Config.victoryLine);
                OnChanged?.Invoke();
                OnVictory?.Invoke();
                return true;
            }
            var hero = Units.FirstOrDefault(u => u.Id == HeroId);
            if (hero == null || !hero.Alive)
            {
                Phase = BattlePhase.Defeat;
                AddLog("石头倒下了——黑水漫过头顶……");
                OnChanged?.Invoke();
                OnDefeat?.Invoke();
                return true;
            }
            OnChanged?.Invoke();
            return false;
        }
    }
}
