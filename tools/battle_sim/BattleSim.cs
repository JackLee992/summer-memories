// BattleSim.cs —— 首战（battle_01）可通关性的 headless 验证（不依赖 Unity 运行时）。
//
// 用束搜索（beam search）在完全确定性的规则上寻找一条胜利路径：
// 敌人 AI 由 BattleDirector.ComputeIntents/ExecuteEnemyTurns 确定性驱动，
// 因此只要存在一条玩家行动序列能取胜，就能证明关卡可通关（并复盘战报）。
//
// 编译/运行：tools/battle_sim/run_sim.sh
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SummerMemories.Battle;

namespace SummerMemories.Tools
{
    public struct Act
    {
        public string UnitId;
        public string Kind;   // move / atk / nail / wait
        public int X, Y;
        public string TargetId;
        public override string ToString()
        {
            switch (Kind)
            {
                case "move": return $"{UnitId}→({X},{Y})";
                case "atk":  return $"{UnitId} 攻击 {TargetId}";
                case "nail": return $"{UnitId} 桃橛定 {TargetId}";
                default:     return $"{UnitId} 待机";
            }
        }
    }

    public sealed class TurnPlan
    {
        public List<Act> Acts = new List<Act>();
    }

    public sealed class Node
    {
        public List<TurnPlan> Plans = new List<TurnPlan>();
        public double Score;
        public string Sig = "";
    }

    public static class BattleSim
    {
        private static BattleConfig _cfg;

        public static int Main(string[] args)
        {
            var repo = FindRepoRoot();
            var json = File.ReadAllText(Path.Combine(repo, "Assets/Resources/Battles/battle_01.json"));
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, IncludeFields = true };
            _cfg = JsonSerializer.Deserialize<BattleConfig>(json, opts);

            Console.WriteLine($"关卡：{_cfg.title}（{_cfg.width}×{_cfg.height}），毫毛 {_cfg.rewindCharges}，桃橛 {_cfg.nails}");
            foreach (var u in _cfg.units)
                Console.WriteLine($"  {(u.team == 0 ? "我方" : "敌方")} {u.id} hp{u.hp} atk{u.atk} move{u.moveRange} range{u.attackRange} skill{u.skill} @({u.gx},{u.gy})");

            const int beamWidth = 120, maxTurns = 14;
            var beam = new List<Node> { new Node() };
            List<TurnPlan> winning = null;

            for (var turn = 1; turn <= maxTurns && winning == null; turn++)
            {
                var expanded = new List<Node>();
                foreach (var node in beam)
                {
                    foreach (var plan in EnumTurnPlans(node))
                    {
                        var dir = Replay(node.Plans.Concat(new[] { plan }).ToList());
                        if (dir.Phase == BattlePhase.Victory) { winning = node.Plans.Concat(new[] { plan }).ToList(); break; }
                        if (dir.Phase == BattlePhase.Defeat) continue;
                        var child = new Node
                        {
                            Plans = node.Plans.Concat(new[] { plan }).ToList(),
                            Sig = Signature(dir),
                            Score = Score(dir, turn)
                        };
                        expanded.Add(child);
                    }
                    if (winning != null) break;
                }
                if (winning != null) break;
                beam = expanded
                    .GroupBy(n => n.Sig)
                    .Select(g => g.OrderByDescending(n => n.Score).First())
                    .OrderByDescending(n => n.Score)
                    .Take(beamWidth)
                    .ToList();
                if (beam.Count == 0)
                {
                    Console.WriteLine($"第 {turn} 回合后所有分支均败北。");
                    break;
                }
                Console.WriteLine($"第 {turn} 回合后：扩展 {expanded.Count}，束内 {beam.Count}，最佳 {beam[0].Score:F0} | {beam[0].Sig}");
            }

            if (winning == null)
            {
                Console.WriteLine($"在 {maxTurns} 回合内未找到胜利路径。");
                return 1;
            }

            Console.WriteLine("\n===== 找到胜利路径，完整战报复盘 =====");
            var final = Replay(winning, true);
            Console.WriteLine($"\n结果：{final.Phase}，用时 {winning.Count} 回合，剩余桃橛 {final.NailsLeft}");
            return final.Phase == BattlePhase.Victory ? 0 : 1;
        }

        private static void ApplyAct(BattleDirector dir, Act act)
        {
            var unit = dir.Units.FirstOrDefault(u => u.Id == act.UnitId);
            if (unit == null || !unit.Alive || unit.Acted) return;
            dir.Select(unit);
            switch (act.Kind)
            {
                case "move": dir.Move(unit, act.X, act.Y); break;
                case "atk":
                    var t = dir.Units.FirstOrDefault(u => u.Id == act.TargetId);
                    if (t != null) dir.Attack(unit, t);
                    break;
                case "nail":
                    var n = dir.Units.FirstOrDefault(u => u.Id == act.TargetId);
                    if (n != null) dir.TryNail(n);
                    break;
                case "wait": dir.Wait(unit); break;
            }
        }

        // 重放完整计划；tail 为只应用而不结算回合的追加行动（用于枚举半回合）
        private static BattleDirector Replay(List<TurnPlan> plans, bool verbose = false, List<Act> tail = null)
        {
            var dir = new BattleDirector();
            dir.Init(_cfg);
            foreach (var plan in plans)
            {
                foreach (var act in plan.Acts)
                {
                    ApplyAct(dir, act);
                    if (dir.Phase is BattlePhase.Victory or BattlePhase.Defeat)
                    {
                        if (verbose) foreach (var l in dir.Log) Console.WriteLine(l);
                        return dir;
                    }
                }
                if (dir.Phase == BattlePhase.Player && dir.AllAlliesActed())
                {
                    dir.BeginIntentPreview();
                    dir.ExecuteEnemyTurns();
                    if (dir.Phase is BattlePhase.Victory or BattlePhase.Defeat)
                    {
                        if (verbose) foreach (var l in dir.Log) Console.WriteLine(l);
                        return dir;
                    }
                }
            }
            if (tail != null)
                foreach (var act in tail)
                {
                    ApplyAct(dir, act);
                    if (dir.Phase is BattlePhase.Victory or BattlePhase.Defeat)
                    {
                        if (verbose) foreach (var l in dir.Log) Console.WriteLine(l);
                        return dir;
                    }
                }
            if (verbose) foreach (var l in dir.Log) Console.WriteLine(l);
            return dir;
        }

        private static IEnumerable<TurnPlan> EnumTurnPlans(Node node)
        {
            var dir0 = Replay(node.Plans);
            if (dir0.Phase != BattlePhase.Player) yield break;
            var allies = dir0.Units.Where(u => u.Alive && u.Team == Team.Ally).ToList();
            foreach (var heroFirst in new[] { true, false })
            {
                var firstId = heroFirst ? "shitou" : "jingwei";
                var secondId = heroFirst ? "jingwei" : "shitou";
                var first0 = allies.First(u => u.Id == firstId);
                foreach (var a in EnumActsOn(dir0, first0))
                {
                    // 应用第一行动后的半回合局面
                    var d1 = Replay(node.Plans, false, new List<Act> { a });
                    if (d1.Phase == BattlePhase.Victory) { yield return new TurnPlan { Acts = new List<Act> { a } }; continue; }
                    if (d1.Phase == BattlePhase.Defeat) continue;

                    var second = d1.Units.FirstOrDefault(u => u.Id == secondId && u.Alive && !u.Acted);
                    if (second == null)
                    {
                        // 第二单位已倒下：单行动成回合，需结算验证
                        var plan = new TurnPlan { Acts = new List<Act> { a } };
                        var probe = Replay(node.Plans.Concat(new[] { plan }).ToList());
                        if (probe.Phase != BattlePhase.Defeat) yield return plan;
                        continue;
                    }
                    foreach (var b in EnumActs(d1State: d1, unit: second))
                    {
                        var plan = new TurnPlan { Acts = new List<Act> { a, b } };
                        var probe = Replay(node.Plans.Concat(new[] { plan }).ToList());
                        if (probe.Phase == BattlePhase.Victory) { yield return plan; yield break; }
                        if (probe.Phase != BattlePhase.Defeat) yield return plan;
                    }
                }
            }
        }

        // 两个重载：从计划重放后枚举（第一行动），或在给定半回合局面上枚举（第二行动）
        private static IEnumerable<Act> EnumActs(BattleDirector d1State, BattleUnit unit)
        {
            foreach (var a in EnumActsOn(d1State, unit)) yield return a;
        }

        private static IEnumerable<Act> EnumActsOn(BattleDirector dir, BattleUnit unit)
        {
            dir.Select(unit);
            foreach (var t in dir.AttackTargets.ToList())
                yield return new Act { UnitId = unit.Id, Kind = "atk", TargetId = t.Id };
            if (unit.Skill == UnitSkill.Nail && dir.NailsLeft > 0)
            {
                foreach (var e in dir.Units.Where(e => e.Alive && e.Team == Team.Shadow
                    && Grid.Manhattan(unit.X, unit.Y, e.X, e.Y) <= 1))
                    yield return new Act { UnitId = unit.Id, Kind = "nail", TargetId = e.Id };
            }
            foreach (var c in dir.MoveCells.ToList())
                yield return new Act { UnitId = unit.Id, Kind = "move", X = c.X, Y = c.Y };
            yield return new Act { UnitId = unit.Id, Kind = "wait" };
        }

        private static string Signature(BattleDirector d)
        {
            var s = $"T{d.TurnNumber} N{d.NailsLeft}|";
            foreach (var u in d.Units.OrderBy(u => u.Id))
                s += $"{u.Id}:{(u.Alive ? 1 : 0)}{u.Hp}@{u.X},{u.Y}{(u.Stunned ? "S" : "")};";
            return s;
        }

        private static double Score(BattleDirector d, int turn)
        {
            var enemyHp = d.Units.Where(u => u.Team == Team.Shadow).Sum(u => u.Alive ? u.Hp : 0);
            var enemyMax = _cfg.units.Where(u => u.team == 1).Sum(u => u.hp);
            var hero = d.Units.First(u => u.Id == "shitou");
            var jing = d.Units.First(u => u.Id == "jingwei");
            return (enemyMax - enemyHp) * 1000
                   + (hero.Alive ? hero.Hp * 120 : -1_000_000)
                   + (jing.Alive ? 400 + jing.Hp * 40 : 0)
                   + d.NailsLeft * 30
                   - turn * 10;
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Assets/Scripts"))) return dir.FullName;
                dir = dir.Parent;
            }
            return AppContext.BaseDirectory;
        }
    }
}
