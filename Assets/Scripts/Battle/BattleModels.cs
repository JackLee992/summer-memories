using System;
using System.Collections.Generic;

namespace SummerMemories.Battle
{
    public enum Team { Ally = 0, Shadow = 1 }
    public enum BattlePhase { Player, IntentPreview, EnemyActing, Victory, Defeat }
    public enum UnitSkill { None, Nail, HairShot }

    [Serializable]
    public class UnitConfig
    {
        public string id;
        public string displayName;
        public int team;          // Team 枚举值
        public int hp = 3;
        public int atk = 1;
        public int moveRange = 3;
        public int attackRange = 1;
        public int skill = 0;     // UnitSkill 枚举值
        public int gx;
        public int gy;
    }

    [Serializable]
    public class BattleConfig
    {
        public string id;
        public string title;
        public int width = 6;
        public int height = 6;
        public int rewindCharges = 3;
        public int nails = 2;
        public string introLine = "";
        public string victoryLine = "";
        public string defeatLoopTip = "tip_death_returns";
        public List<UnitConfig> units = new List<UnitConfig>();
    }

    /// <summary>运行时单位。</summary>
    public class BattleUnit
    {
        public string Id;
        public string DisplayName;
        public Team Team;
        public int Hp, MaxHp;
        public int Atk;
        public int MoveRange;
        public int AttackRange;
        public UnitSkill Skill;
        public int X, Y;
        public bool Acted;       // 本回合已行动
        public bool Stunned;     // 被桃橛定身，下回合无法行动
        public bool Alive = true;

        public BattleUnit() { }

        public BattleUnit(UnitConfig c)
        {
            Id = c.id;
            DisplayName = c.displayName;
            Team = (Team)c.team;
            Hp = MaxHp = c.hp;
            Atk = c.atk;
            MoveRange = c.moveRange;
            AttackRange = c.attackRange;
            Skill = (UnitSkill)c.skill;
            X = c.gx; Y = c.gy;
        }

        public BattleUnit Clone()
        {
            return (BattleUnit)MemberwiseClone();
        }
    }

    public struct Cell
    {
        public int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
    }

    /// <summary>敌人行动意图（玩家阶段结束时预告）。</summary>
    public class EnemyIntent
    {
        public string UnitId;
        public int FromX, FromY;
        public int ToX, ToY;
        public bool Attacks;
        public int TargetX, TargetY;
        public string TargetId;
    }
}
