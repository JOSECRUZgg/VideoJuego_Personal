# Integración de recursos extraídos

El importador `Tools/ImportExtractedVisuals.ps1` copia recursos visuales y audio a `Assets/Resources/ExtractedVisuals`. Remapea GUID para evitar colisiones, conserva mallas, renderers, partículas y animadores, y elimina componentes de gameplay externos. Los materiales se adaptan a URP con su textura de color.

## Conexiones activas

- Personajes actuales: RobotCowboy, Knight, Calcium, B_Megachad; parámetros moving, grounded y jumping controlados por el movimiento actual.
- Enemigos después de 90 segundos: Bandit, CorruptedKnight, BloodMage, Birdo y Ogre según tipo. Conservan la IA y daño actuales.
- Interactuables: PotSmall, ChargeShrine, GreedShrine, CursedShrine, Microwave, BossSpawner, PlayerSpawnPortal, ShrineMaoi, ShrineBalance y Chest/ChestFree.
- Vegetación: Tree0, Tree2, Tree0Forest2, Tree1Forest2, Bush0, Log0 y DeadTree0.
- Proyectiles: RocketProjectile, BowProjectile, ProjectileBone, FireballProjectile, RevolverProjectile, HomingOrb y GhostProjectile.
- Eventos: ruptura de vasija, finalización de santuario, apertura de cofre e impactos/explosiones de armas. Sonidos PotBreak y ChestTake.
- Efectos temporales limitados a 40 objetos simultáneos y 160 partículas por sistema.

## Alcance y verificación

414 prefabs importados como catálogo; importar un prefab no implementa una habilidad nueva. Los prefabs de interfaces pierden sus componentes de UI en esta importación visual y no reemplazan los menús existentes. Las escenas, scripts originales y sus mecánicas no se importan.

Auditoría de GUID de prefabs: una referencia externa ausente en ArrowTestPrefab, que además carece de malla y no se utiliza. Los reemplazos conectados no presentan referencias externas ausentes en esa auditoría. El catálogo restante está disponible para integraciones específicas.

Compilación C# revisada; no se ejecutaron partidas por indicación del usuario. Pendiente comprobar apariencia, orientación, materiales de partículas y transiciones de animación en Unity.
