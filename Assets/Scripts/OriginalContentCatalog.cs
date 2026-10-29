using System;
using UnityEngine;

/// <summary>Datos leídos de la exportación original; las referencias existentes conservan sus GUID.</summary>
public class OriginalContentCatalog : ScriptableObject
{
    [Serializable] public class Character
    {
        public string id, displayName, weaponId, originalWeapon;
        public GameObject prefab;
        public Texture2D icon;
        public GameManager.CharacterType legacy;
    }
    [Serializable] public class Decoration
    {
        public GameObject[] prefabs;
        public int amount;
        public float scaleMin, scaleMax, checkRadius, maxSlopeAngle, upOffset;
        public bool alignWithNormal;
    }
    [Serializable] public class DesertStage
    {
        public string id;
        public Decoration[] decorations;
        public float noiseScale, persistence, lacunarity, heightMultiplier, uniformScale;
        public int octaves, seed;
        public Vector2 offset;
        public AnimationCurve heightCurve;
        public Material terrainMaterial;
        public Color ambient, sunlight, sky;
        public float lightIntensity, farClip;
    }
    public Character[] characters = Array.Empty<Character>();
    public DesertStage[] desertStages = Array.Empty<DesertStage>();
    public GameObject animatedChest;
    public RuntimeAnimatorController chestController;
    public AudioClip chestOpenSound, chestTakeSound;
    public int schemaVersion;
    static OriginalContentCatalog cached;
    public static OriginalContentCatalog Instance => cached != null ? cached : cached = Resources.Load<OriginalContentCatalog>("OriginalContentCatalog");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { cached = null; }
    public static Character Selected
    {
        get
        {
            var gm = GameManager.Instance;
            var data = Instance;
            if (gm == null || data == null) return null;
            return Array.Find(data.characters, c => c.id == gm.selectedOriginalCharacter) ??
                Array.Find(data.characters, c => c.legacy == gm.selectedCharacter);
        }
    }
    public static void Select(Character character)
    {
        GameManager.Instance.selectedOriginalCharacter = character.id;
        GameManager.Instance.selectedCharacter = character.legacy;
    }
    public static DesertStage Desert => Instance == null ? null : Array.Find(Instance.desertStages,
        s => s.id == "StageDesert" + (StageDirector.Instance != null ? Mathf.Clamp(StageDirector.Instance.CurrentStage, 1, 3) : 1));
}
