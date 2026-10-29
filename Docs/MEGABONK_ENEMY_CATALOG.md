# Catálogo de enemigos extraídos

Fuente actual: MEGABONK/ExportedProject/Assets, carpeta facilitada por el usuario.

El importador visual copia las mallas, materiales y prefabs con referencias remapeadas. El editor genera Resources/ExtractedEnemyCatalog.asset a partir de EnemyData y las secuencias AnimatedMesh originales. Se puede regenerar desde Tools/Megabonk/Rebuild extracted enemy catalog. Solo se incluyen entradas con material y mallas resolubles.

Conexiones: esqueletos al inicio; gólems/momias después de 90 segundos; BoomerSpider, GoblinStrong/Scorpion, Wisp/CactusShooter, Bee y miniboss gólem/escorpión según el arquetipo de aparición existente. Jefes EntStage1–3 en bosque y nieve; Pharaoh1–3 en desierto. La nieve usa provisionalmente los jefes de bosque.

La reproducción visual usa fotogramas de malla con orientación original, escala normalizada y apoyo en suelo; se detiene al congelarse, morir o pausar. Si falta una entrada se conserva la representación anterior.

IMPORTANTE: esto conecta representación visual, no reproduce la IA original. Los ataques, estadísticas y estados de combate siguen siendo los del proyecto. EnemySpawner.cs extraído solo contiene un campo: no es un sistema funcional listo para copiar. La fidelidad exacta de patrones de jefes, escenarios completos, misiones y habilidades sigue pendiente. Las escenas y TerrainData originales no sustituyen automáticamente el terreno actual. No se han ejecutado partidas.
