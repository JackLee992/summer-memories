using System;
using UnityEngine;
namespace SummerMemories.Action3D.Side2D
{
    [Serializable] public class SideConfig
    {
        public string saveSlot="st_side2d",background;
        public float length=88;
        public SideCharacter[] characters;
        public SideSolid[] solids;
        public SideEnemyData[] enemies;
        public SideObject[] objects;
    }
    [Serializable] public class SideCharacter
    {public string id,nameKey,sheet;public float hp=20,speed=5,damage=3,reach=2;}
    [Serializable] public class SideSolid
    {public string id,opensWith;public float x,y,width,height;}
    [Serializable] public class SideEnemyData
    {public string id,style,nameKey;public float x,hp=14,damage=3,reach=2,windup=.7f;}
    [Serializable] public class SideObject
    {public string id,nameKey,promptKey,detailKey,requires,sets;public float x,y;public string type;}
    public enum SidePhase{Play,Defeat,Victory}
    public enum SideAction{Idle,Run,Jump,Fall,Dash,Attack,Heavy,Skill,Hurt,Dead}
    public enum SideForm{Human,Stone,Watch}
    [Serializable] public class SideActor
    {
        public string id;public float hp,cooldown,stamina=100;public bool ryunosuke,pipe;
        public Vector2 position;public float velocityY;public int facing=1;
        [NonSerialized] public SideAction action;
        [NonSerialized] public float actionTime,actionLength,comboTime,invulnerable;
        [NonSerialized] public int combo;
        [NonSerialized] public bool hit,grounded;
        public SideForm form;
    }
    public sealed class SideEnemy
    {
        public SideEnemyData data;public float x,hp,windup,recovery,bound;public int facing=-1,linkActor=-1;public bool committed;
        public bool Alive=>hp>0;
    }
    public struct SideInput
    {public float move;public bool jump,dash,attack,heavy,skill,interact,copy,returnHuman,consciousness;public int switchTo;}
}
