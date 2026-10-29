using UnityEngine;

/// <summary>Adaptador de escenas antiguas. El combate automático vive en MegabonkBuild.</summary>
public class PlayerShooting3D : MonoBehaviour
{
    public Camera cam;
    public WeaponType currentWeaponType = WeaponType.Revolver;
    public WeaponProfile currentWeapon;
    public int damage = 35, magazineSize = 6, reserveAmmo = 999, currentAmmo = 6;
    public float range = 100f, fireRate = .5f, reloadTime = .8f, recoilForce = .6f;
    public bool autoAim = true, thirdPersonView = true, isReloading, hitMarkerActive;
    public float hitMarkerTimer;
    public GameObject muzzleFlashPrefab, impactEffectPrefab;
    void Awake() { EquipWeapon(WeaponType.Revolver); }
    public void EquipWeapon(WeaponType type)
    {
        currentWeaponType = type;
        currentWeapon = WeaponProfile.GetProfile(type);
        isReloading = false;
        autoAim = true;
        thirdPersonView = true;
    }
}
