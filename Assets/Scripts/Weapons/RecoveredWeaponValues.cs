/// <summary>Confirmed Weapon*.asset values; upgrade coefficients remain reconstructed.</summary>
public static class RecoveredWeaponValues
{
    public static int Damage(string id,int level,int fallback)
    {
        float value;
        switch(id)
        {
            case "mb_revolver":value=5;break;
            case "mb_bone":case "mb_sword":case "mb_katana":case "mb_rocket":value=11;break;
            case "mb_banana":case "mb_bow":value=9;break;
            case "mb_fire":value=10;break;
            case "mb_aura":value=6;break;
            default:return fallback;
        }
        return UnityEngine.Mathf.RoundToInt(value*(1+.25f*(level-1)));
    }
    public static float Interval(string id,float fallback)
    {
        // Attack duration + endCooldown where the source describes an attack sequence.
        switch(id)
        {
            case "mb_revolver":return 2.4f;
            case "mb_sword":return 1.25f;
            case "mb_katana":return .9f;
            case "mb_bow":return 1.42f;
            case "mb_bone":return 3.4f;
            case "mb_banana":return 3.8f;
            case "mb_fire":return 2.5f;
            case "mb_aura":return .3f;
            default:return fallback;
        }
    }
}
