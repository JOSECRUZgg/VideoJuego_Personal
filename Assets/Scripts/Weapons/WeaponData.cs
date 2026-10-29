using UnityEngine;

/// <summary>
/// Tipos y perfiles numéricos de las armas base del juego.
/// Datos conservados para compatibilidad de escenas y efectos antiguos.
/// </summary>
public enum WeaponType
{
    Revolver,   // Pistola
    Shotgun,    // Escopeta
    SMG,        // Subfusil
    Crossbow    // Ballesta
}

[System.Serializable]
public class WeaponProfile
{
    public WeaponType type;
    public string name;
    public string icon;
    public int damage;
    public float fireRate;      // Disparos por segundo (lapso = 1.0 / fireRate)
    public int maxAmmo;
    public float reloadTime;    // Segundos que tarda la recarga
    public float spread;        // Ángulo de dispersión en grados
    public int projectileCount; // Proyectiles simultáneos por disparo
    public bool isExplosive;    // Proyectil explosivo (Ballesta)
    public string description;
    public Color themeColor;

    public static WeaponProfile GetProfile(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Revolver:
                return new WeaponProfile
                {
                    type = WeaponType.Revolver,
                    name = "Pistola (Revolver)",
                    icon = "🔫",
                    damage = 35,
                    fireRate = 2.0f, // 0.5s entre disparos
                    maxAmmo = 6,
                    reloadTime = 0.8f,
                    spread = 2.0f,
                    projectileCount = 1,
                    isExplosive = false,
                    description = "Equilibrada, precisa y con buen daño por impacto.",
                    themeColor = new Color(0.2f, 0.85f, 1f)
                };

            case WeaponType.Shotgun:
                return new WeaponProfile
                {
                    type = WeaponType.Shotgun,
                    name = "Escopeta (Shotgun)",
                    icon = "💥",
                    damage = 18,
                    fireRate = 1.2f, // 0.83s entre disparos
                    maxAmmo = 3,
                    reloadTime = 1.2f,
                    spread = 14.0f,
                    projectileCount = 5, // 5 perdigones en abanico
                    isExplosive = false,
                    description = "Dispara 5 proyectiles en abanico devastadores a corta distancia.",
                    themeColor = new Color(1f, 0.5f, 0.1f)
                };

            case WeaponType.SMG:
                return new WeaponProfile
                {
                    type = WeaponType.SMG,
                    name = "Subfusil (SMG)",
                    icon = "⚡",
                    damage = 11,
                    fireRate = 8.0f, // 0.125s entre disparos
                    maxAmmo = 25,
                    reloadTime = 0.6f,
                    spread = 6.0f,
                    projectileCount = 1,
                    isExplosive = false,
                    description = "Cadencia extrema de 8 disparos por segundo con cargador ampliado.",
                    themeColor = new Color(1f, 0.9f, 0.2f)
                };

            case WeaponType.Crossbow:
                return new WeaponProfile
                {
                    type = WeaponType.Crossbow,
                    name = "Ballesta (Crossbow)",
                    icon = "🏹",
                    damage = 80,
                    fireRate = 0.9f, // 1.11s entre disparos
                    maxAmmo = 1,
                    reloadTime = 0.9f,
                    spread = 0.5f,
                    projectileCount = 1,
                    isExplosive = true,
                    description = "1 disparo masivo de 80 de daño que explota en área y maldice a los enemigos.",
                    themeColor = new Color(0.8f, 0.2f, 1f)
                };

            default:
                return GetProfile(WeaponType.Revolver);
        }
    }
}
