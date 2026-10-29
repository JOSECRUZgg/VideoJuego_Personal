# Desierto, cofres y selector de personajes

## Abrir y revisar

1. Abre `Assets/Scenes/Main.unity` en Unity. Deja terminar la compilación y la importación.
2. El catálogo se genera automáticamente cuando falta o cambia su versión. También está disponible en **Tools > Megabonk > Integrar desierto, cofres y personajes originales**.
3. En el menú del juego: **Jugar**, selecciona uno de los 21 personajes y arrastra horizontalmente sobre su vista 3D para girarlo. Confirma y elige **Desierto**. El avatar de partida utiliza la misma referencia de prefab que el selector.
4. Acércate a un cofre y pulsa **E** con el oro necesario. Se cobra una vez, se reproduce `Open`, se transita a `Opened` y entonces se presenta la recompensa. El cofre permanece abierto al reclamarla.

Las pruebas jugables las realiza el usuario. La verificación realizada aquí comprende compilación, catálogo y referencias de recursos; no confirma una partida jugada ni equivalencia visual píxel a píxel.

## Recursos y referencias

Fuente: `C:\Users\JOSEC\Desktop\MATERIAS UT\10MO CUATRIMESTRE\JUAN\MEGABONK\ExportedProject\Assets`.

Se reutilizan los prefabs, mallas, materiales, texturas, clips y controladores ya integrados en `Assets/Resources/ExtractedVisuals`. Sus `.meta` están ocultos en Windows: no faltaban. Esta integración conserva sus GUID actuales, remapeados por la importación anterior, y no modifica los archivos originales.

- Desierto: `StageDesert1/2/3`, sus `proceduralNoiseData`, `proceduralTerrainData`, curva de altura, ambiente, luz y material triplanar. Grupos originales de cactus, rocas, árboles secos, arbustos, troncos, casas y landmarks; sus variantes, cantidades objetivo, escalas, orientación de autor y reglas de inclinación proceden de los datos serializados.
- Cofre: rig de `OpenChest`, controlador `Animator_Chest`, clips `Idle`, `Open`, `Opened`, `OpenedBounce`, `Intro` y audio referenciado por el prefab. El controlador de reposo abierto que traía `OpenChest` se sustituye en la instancia por el controlador original de apertura. Se comprueban las rutas de los clips contra el rig.
- Personajes: todas las entradas `eCharacter` con mallas/materiales disponibles, nombres de la tabla española, iconos y controladores originales. La cámara de vista previa usa una capa exclusiva, distancia de encuadre calculada con los límites del modelo y tiempo no escalado para el reposo.

## Fidelidad y límites conocidos

Los scripts C# exportados contienen cuerpos vacíos. No permiten recuperar exactamente el algoritmo procedural ni la secuencia programada de efectos. Se reconstruye el terreno a partir de sus parámetros originales sobre el área transitable de 460 × 460 del proyecto. Se reservan recorridos y sectores de interactuables; si no cabe una decoración sin superponerse, se omite esa instancia. No se presenta esta distribución como una reproducción exacta de una semilla del juego comercial.

El shader original está exportado como shader compilado. `OriginalDesertSurface.shader` adapta sus texturas superior/lateral, escalas y color a URP. Los cambios de iluminación entre etapas utilizan los colores e intensidades originales. Se mantiene la preferencia previa del usuario de no tener niebla.

Los 21 personajes tienen modelo seleccionable y utilizable en partida. Las 13 armas antes sustituidas por revólver ahora tienen mecánicas propias en `ExtendedWeapons.cs`, resolución de arma inicial y opciones de mejora. Se conservan daño/cadencia base de los archivos cuando son aprovechables; dados, ejecución, curación y áreas usan reglas reconstruidas, no código original recuperado. La ampliación posterior conecta las 21 pasivas mediante `CharacterPassives.cs`; sus coeficientes no recuperados son reconstruidos (ver `PASIVAS_Y_FIDELIDAD.md`). El catálogo anterior se resuelve también en runtime y el editor lo actualizará a versión 3 al importar.

## Archivos principales

- `Assets/Editor/OriginalContentBuilder.cs`: extracción de parámetros, catálogo y auditoría.
- `Assets/Scripts/OriginalContentCatalog.cs` y `Assets/Resources/OriginalContentCatalog.asset`: referencias persistentes incluidas en el ejecutable.
- `Assets/Scripts/OriginalDesertDesign.cs`, `ArenaEnricher.cs` y `Assets/Resources/StageDesert*Surface.mat`: generación, colocación, colisiones y materiales.
- `Assets/Scripts/OriginalModel.cs`: instancias visuales con rig y materiales, sin combate ni físicas heredadas.
- `Assets/Scripts/UI/CharacterPreview3D.cs`, `OriginalCharacterSelection.cs`, `GamePresentation.cs` y `GameHUD.cs`: selector y vista giratoria.
- `Assets/Scripts/Player/PlayerCharacterVisual.cs`, `MegabonkBuild.cs`, `GameManager.cs` y `StageDirector.cs`: avatar elegido, arma compatible y reinicio correcto de etapa.
- `Assets/Scripts/Enemy/BossChest3D.cs`: interacción, animación, recompensa única y persistencia del estado abierto.

No fue necesario sobrescribir escenas: la escena `Main` existente crea estos elementos mediante sus componentes actuales.
