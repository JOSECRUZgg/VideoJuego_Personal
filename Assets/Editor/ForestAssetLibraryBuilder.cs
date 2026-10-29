using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public class ForestAssetLibraryBuilder : IPreprocessBuildWithReport
{
    const string PathName = "Assets/Resources/Forest/ForestAssetLibrary.asset";
    static readonly string[] Names = { "UNS_Spruce_01", "UNS_Spruce_02", "UNS_Bush", "UNS_Grass", "UNS_Flower", "UNS_Mushroom_Patch", "UNS_Standard_Rock_01", "UNS_Standard_Rock_02", "PT_Pine_Tree_03_green", "PT_Fruit_Tree_01_green", "PT_Fruit_Tree_01_apples", "PT_Pine_Tree_03_logs", "PT_Pine_Tree_03_stump", "PT_Grass_02", "PT_Caesars_Mushroom_01", "PT_Menhir_Rock_02" };
    static ForestAssetLibraryBuilder() { EditorApplication.delayCall += EnsureLibrary; }
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) { EnsureLibrary(); }
    [MenuItem("Tools/Forest/Actualizar assets del bosque")]
    public static void EnsureLibrary()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scenery = new[] { "Platform_circle", "Altar_1", "Altar_2", "VFX_TorchLight_Green", "VFX_TorchLight",
            "Broken_column", "Column_broken_small", "Column", "Stone_post_2", "Wall_broken_1", "Wall_broken_2",
            "Wall_round_1", "Wall_round_2", "Wall_round_3", "Stone_structure", "Gate", "Gate_broken", "Arch_1",
            "Tower_A", "Tower_B", "Rubble_1", "Rubble_2", "Stone_slab_1", "Stone_slab_2", "PT_Pine_Tree_03_dead",
            "UNS_Standard_Rock_03", "PT_Generic_Rock_01", "UNS_Rock_Cliff_01", "UNS_Rock_Cliff_02",
            "UNS_Rock_Cliff_03", "UNS_Rock_Cliff_04", "UNS_Mountain", "Stars" };
        var prefabs = Names.Concat(scenery).Concat(new[] { "Enemy", "SkeletonEnemy", "FlyingShooterEnemy", "DragonBoss" }).Distinct().Select(name => AssetDatabase.FindAssets(name + " t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileNameWithoutExtension(p) == name)
            .OrderBy(p => p).Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault())
            .Where(p => p != null).ToArray();
        var library = AssetDatabase.LoadAssetAtPath<ForestAssetLibrary>(PathName);
        if (library == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Forest")) AssetDatabase.CreateFolder("Assets/Resources", "Forest");
            library = ScriptableObject.CreateInstance<ForestAssetLibrary>();
            AssetDatabase.CreateAsset(library, PathName);
        }
        if (library.prefabs != null && library.prefabs.SequenceEqual(prefabs)) return;
        library.prefabs = prefabs;
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
    }
}
