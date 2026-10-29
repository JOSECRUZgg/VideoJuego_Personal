# Modelos de la build y hechizos

Integración visual sin modificar el daño ni añadir colisiones a las presentaciones:

- Las armas equipadas se muestran alrededor del personaje. Se reutilizan prefabs originales con malla/material válidos; los restantes se reconstruyen extruyendo el icono del arma.
- Los objetos ganados en cofres o mediante duplicación muestran un modelo giratorio sobre el jugador durante 2,5 segundos. Borgar, llave, fogata y máscara intentan usar sus prefabs; el resto utiliza una silueta de píxeles con volumen, no una malla original. No se colocan simultáneamente 78 objetos alrededor del personaje.
- Hay recursos de icono para los 78 objetos registrados en el proveedor. Los volúmenes se crean una vez por identificador, se almacenan en caché y omiten caras internas. Esta presentación conserva el dibujo del icono; no equivale a modelar manualmente cada objeto desde todos los ángulos.
- Daga inalámbrica: se encontró y conectó BluetoothProjectile, además de la lógica de persecución/rebote ya existente.
- Fuego, sangre y energía incorporan efectos originales siguiendo al proyectil. Rayos, espada, katana, aura e impactos de fuego disparan sus efectos asociados. Las áreas usan efectos originales ligados a su duración y movimiento. Se mantienen respaldos visuales cuando los recursos originales no son utilizables.
- La XP usa un cristal facetado reconstruido conservando colores, atracción y recogida.
- Vasijas, santuarios, portales y cofres conservan las integraciones 3D existentes, incluidas las vasijas/linternas solicitadas previamente.

Archivos: BuildModelPresentation.cs, SpellModelEffects.cs, CombatVisuals.cs, ExtendedWeapons.cs, MegabonkBuild.cs, MegabonkRelicManager.cs, MegabonkIconProvider.cs y XpOrb3D.cs. No se sustituyen escenas ni se modifican los assets originales.

Comprobaciones: compilación C# y existencia de recursos de iconos. Sin pruebas jugables ni verificación visual en Play Mode. No se afirma que todos los objetos tengan un modelo original: muchos solo tienen iconos en los archivos disponibles. Los shaders y partículas todavía requieren revisión visual en el editor por el usuario.
