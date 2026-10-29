using System;
using UnityEngine;

public class ExtractedEnemyCatalog : ScriptableObject
{
    public int schemaVersion;
    [Serializable] public class Attack
    {
        public string name;
        public float charge, radius, cooldown, initialCooldown, range, recovery, damageMultiplier, duration, endLag;
    }
    [Serializable] public class Entry
    {
        public string id;
        public Material material;
        public Mesh[] frames;
        public float fps, speed, spawnAfter;
        public Vector3 rotation;
        public bool flying;
        public int hp, damage, shield, xp;
        public float minDistance, maxDistance;
        public Attack[] attacks;
    }
    public Entry[] entries;
}
