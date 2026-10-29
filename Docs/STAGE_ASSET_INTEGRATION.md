# Recursos de etapas, interfaz y armas

Integración implementada:

- Banana disponible en el catálogo de mejoras con icono Bananarang y modelo BananaProjectile; atraviesa enemigos, gira y vuelve al jugador. Puede golpear una vez de ida y otra de vuelta. Se beneficia de cantidad, tamaño, daño y velocidad de proyectil. Huesos conservan rebotes y reciben giro visual.
- Interfaz compartida usa alagard.ttf y botones StoneButton usan MyButton.png del export.
- El editor genera Resources/ExtractedStageCatalog.asset desde StageForest1–3 y StageDesert1–3 al importar los scripts. También lo genera antes de compilar una build. Material de piso, luz ambiental, color/intensidad solar y fondo de cámara se conectan al escenario. Se conserva la preferencia del usuario de no tener niebla. Los shaders extraídos previamente se adaptaron a URP.
- Portal: sonido PortalPass, pantalla de carga, pausa temporal, limpieza de enemigos/proyectiles/cofres/XP/curación, reconstrucción de escenario, reposicionamiento sobre terreno y reinicio del reloj de etapa. El bosque cambia su relieve entre etapas.

Límites: la generación continúa siendo síncrona durante la pantalla de carga. No se copió la escena LoadingScreen original ni la jerarquía completa de MainMenu. Mazmorra, cementerio, infierno y arena final aún no están conectados como mapas jugables. La nieve conserva su tratamiento actual. El generador y las trayectorias son implementaciones propias; no se recuperaron los cuerpos de los métodos originales. Requiere comprobación visual y de jugabilidad por el usuario.
