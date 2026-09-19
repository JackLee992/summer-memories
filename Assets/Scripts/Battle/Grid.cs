using System.Collections.Generic;

namespace SummerMemories.Battle
{
    /// <summary>
    /// 方形网格工具：占用判定、曼哈顿距离、BFS 可达范围。
    /// </summary>
    public static class Grid
    {
        public static int Manhattan(int x1, int y1, int x2, int y2)
        {
            return System.Math.Abs(x1 - x2) + System.Math.Abs(y1 - y2);
        }

        public static bool InBounds(BattleConfig cfg, int x, int y)
        {
            return x >= 0 && y >= 0 && x < cfg.width && y < cfg.height;
        }

        public static BattleUnit UnitAt(IList<BattleUnit> units, int x, int y)
        {
            for (var i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u.Alive && u.X == x && u.Y == y) return u;
            }
            return null;
        }

        /// <summary>BFS 计算移动范围内可停留的格子（被占用格不可穿过/停留）。</summary>
        public static List<Cell> Reachable(BattleConfig cfg, IList<BattleUnit> units,
            BattleUnit mover, int range)
        {
            var result = new List<Cell>();
            var dist = new Dictionary<(int, int), int>();
            var start = (mover.X, mover.Y);
            dist[start] = 0;
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((mover.X, mover.Y));
            var dirs = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                var d = dist[(x, y)];
                if (d >= range) continue;
                foreach (var (dx, dy) in dirs)
                {
                    var nx = x + dx; var ny = y + dy;
                    if (!InBounds(cfg, nx, ny)) continue;
                    if (dist.ContainsKey((nx, ny))) continue;
                    var occupant = UnitAt(units, nx, ny);
                    if (occupant != null && occupant.Id != mover.Id) continue;
                    dist[(nx, ny)] = d + 1;
                    queue.Enqueue((nx, ny));
                    if (occupant == null) result.Add(new Cell(nx, ny));
                }
            }
            return result;
        }

        public static List<BattleUnit> TargetsInRange(IList<BattleUnit> units,
            BattleUnit attacker, Team enemyTeam)
        {
            var list = new List<BattleUnit>();
            foreach (var u in units)
            {
                if (!u.Alive || u.Team != enemyTeam) continue;
                if (Manhattan(attacker.X, attacker.Y, u.X, u.Y) <= attacker.AttackRange
                    && Manhattan(attacker.X, attacker.Y, u.X, u.Y) > 0)
                    list.Add(u);
            }
            return list;
        }
    }
}
