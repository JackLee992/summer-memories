using System.Collections.Generic;
using UnityEngine;

namespace SummerMemories.Action3D
{
    /// <summary>
    /// 回潮（时间回溯）：战斗开局快照 + 死亡后回溯。本作核心机制。
    /// 只存状态数据；倒流演出（插值/后处理/字幕）由 Game3DDirector 协程呈现。
    /// </summary>
    public class Rewind3D
    {
        public int Charges = 3;
        public const int MaxCharges = 3;

        private struct EnemySnap
        {
            public Vector3 Pos;
            public Quaternion Rot;
            public float Hp;
        }

        private Vector3 _pPos;
        private Quaternion _pRot;
        private float _pHp;
        private readonly List<EnemySnap> _enemies = new List<EnemySnap>();

        public void Capture(Combatant3D player, List<Combatant3D> enemies, List<Enemy3D> enemyAi)
        {
            _enemies.Clear();
            _pPos = player.transform.position;
            _pRot = player.transform.rotation;
            _pHp = player.MaxHp;
            for (var i = 0; i < enemies.Count; i++)
            {
                _enemies.Add(new EnemySnap
                {
                    Pos = enemies[i].transform.position,
                    Rot = enemies[i].transform.rotation,
                    Hp = enemies[i].MaxHp
                });
            }
            Charges = MaxCharges;
        }

        /// <summary>消耗一次回潮，返回剩余次数；-1 表示已无回潮（败北）。</summary>
        public int Spend()
        {
            if (Charges <= 0) return -1;
            Charges--;
            return Charges;
        }

        public void Restore(Combatant3D player, PlayerController3D playerCtl,
            List<Combatant3D> enemies, List<Enemy3D> enemyAi)
        {
            player.ResetTo(_pPos, _pRot, _pHp);
            for (var i = 0; i < enemies.Count && i < _enemies.Count; i++)
            {
                var s = _enemies[i];
                if (enemyAi != null && i < enemyAi.Count && enemyAi[i] != null)
                    enemyAi[i].ResetEnemy(s.Pos, s.Rot, s.Hp);
                else
                    enemies[i].ResetTo(s.Pos, s.Rot, s.Hp);
                playerCtl?.ClearLockIf(enemies[i]);
            }
            playerCtl.LockTarget = null;
        }

        public Vector3 PlayerStartPos => _pPos;
    }
}
