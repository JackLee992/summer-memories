using System;
using System.Collections.Generic;
using UnityEngine;

namespace SummerMemories.Action3D.Squad
{
    [Serializable] public class SquadConfig
    {
        public string id, titleKey, saveSlot, initialControlledId;
        public PartyConfig[] party;
        public AbilityConfig[] abilities;
        public FormConfig[] scanTemplates;
        public InteractionConfig[] interactions;
        public EnemyConfig[] enemies, introEnemies;
        public WorldConfig world;
        public SquadAudioConfig audio;
        public string[] storyIds;
        public SquadPresentation presentation = new SquadPresentation();
        public PartyConfig Member(string id) => Array.Find(party, a => a.id == id);
        public AbilityConfig Ability(string id) => Array.Find(abilities, a => a.id == id);
        public FormConfig Form(string id) => Array.Find(scanTemplates, a => a.id == id);
        public static Vector3 Position(float[] p) => new Vector3(p[0], p[1], p[2]);
        public void Validate()
        {
            if (id != "st_squad_demo" || party == null || party.Length < 2 || world == null)
                throw new InvalidOperationException("Invalid squad configuration");
            var ids = new HashSet<string>();
            foreach (var p in party)
            {
                if (!ids.Add(p.id) || p.hp <= 0 || p.spawn == null || p.spawn.Length != 3)
                    throw new InvalidOperationException("Invalid party member: " + p.id);
                foreach (var a in p.abilities)
                    if (Ability(a) == null || Ability(a).ownerId != p.id)
                        throw new InvalidOperationException("Invalid ability ownership: " + a);
            }
            if (Member(initialControlledId) == null) throw new InvalidOperationException("Missing initial actor");
            foreach (var f in scanTemplates)
                if (f.sizeMeters == null || f.sizeMeters.Length != 3 || Array.Exists(f.sizeMeters, n => n <= 0))
                    throw new InvalidOperationException("Invalid form: " + f.id);
            foreach (var e in enemies)
                if (e.hp <= 0 || e.spawn.Length != 3) throw new InvalidOperationException("Invalid enemy: " + e.id);
        }
    }
    [Serializable] public class SquadPresentation
    {
        public bool isometric;
        public string saveSlot, spriteRoot="Art/Portraits/25D/";
        public float cameraYaw=-25, cameraPitch=48, orthographicSize=8.4f, spriteHeight=2.35f;
    }
    [Serializable] public class PartyConfig
    {
        public string id, nameKey, role;
        public string[] abilities;
        public float hp = 12, attack = 1, speed = 3.8f;
        public float[] spawn;
    }
    [Serializable] public class AbilityConfig
    {
        public string id, ownerId, nameKey;
        public float rangeMeters = 5, durationSeconds = 1, cooldownSeconds = 5;
    }
    [Serializable] public class FormConfig
    {
        public string id, nameKey, gameplay, attachToId;
        public float[] sizeMeters, spawn;
        public float moveSpeed = 1.3f;
    }
    [Serializable] public class InteractionConfig
    {
        public string id, tipId, nameKey, descriptionKey, reward;
        public float[] spawn;
        public bool requiredForEncounter;
    }
    [Serializable] public class EnemyConfig
    {
        public string id, nameKey, attackStyle = "sweep";
        public float[] spawn;
        public float hp = 6, damage = 2, speed = 2.2f, windup = .8f;
        public float posture = 6, aggroRange = 14, recovery = 1.25f;
    }
    [Serializable] public class WorldConfig
    {
        public float width = 24, length = 60;
        public float[] encounterGate, exit, npc;
        public SolidConfig[] solids;
    }
    [Serializable] public class SolidConfig
    {
        public string id;
        public float[] position, size;
    }
    [Serializable] public class SquadAudioConfig
    {
        public string ambience, shadowReveal, rewind, hair, shell;
    }
    public enum SquadPhase { Title, Story, Explore, Fight, Defeat, Rewinding, Victory }
    public enum AllyOrder { Follow, Hold, Focus }
    public enum ActorAction { Free, Attack, Hair, Dodge, Hurt, Dead }
    [Serializable] public class ActorSnapshot
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public float hp, stamina, hairCooldown, morphCooldown;
        public bool ryunosuke, hasPipe, pipeEquipped;
        public List<string> scans = new List<string>();
    }
    [Serializable] public class SquadCheckpoint
    {
        public SquadPhase phase;
        public int activeIndex;
        public List<ActorSnapshot> actors = new List<ActorSnapshot>();
        public List<string> collected = new List<string>();
    }
}
