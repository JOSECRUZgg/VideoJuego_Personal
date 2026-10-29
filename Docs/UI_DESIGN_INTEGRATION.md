# Integración visual de las interfaces de MEGABONK

Se revisó `C:\Users\JOSEC\Desktop\MATERIAS UT\10MO CUATRIMESTRE\JUAN\MEGABONK\ExportedProject\Assets`.

## Diseños localizados

- `Scenes/MainMenu.unity`: composición del menú principal.
- `Scenes/LoadingScreen.unity`: pantalla de carga, barra y texto de estado.
- `Scenes/BootScene.unity`: entrada del juego y ventanas de personajes/desbloqueos.
- `Scenes/GeneratedMap.unity`: HUD, minimapa e interfaces durante la partida.
- Prefabs `Panel1`–`Panel4`, `DynamicWindow`, `DynamicWindowPrompt` y `Chest`: marcos y ventanas del estilo original.
- Texturas importadas en `Assets/Resources/ExtractedVisuals/Texture2D`: `BorderWindow`, `background_Gray`, `MyButton`, `MyButtonOverlay`, `WindowOverlaySides` y `MegabonkLogo`.
- Tipografía importada: `Assets/Resources/ExtractedVisuals/Font/alagard.ttf`.

## Aplicado al proyecto actual

- Las ventanas de personaje, mapa, desbloqueos, tienda, toggler, ajustes y HUD usan el marco original `BorderWindow` con sus bordes de nueve partes. Las esquinas ya no se estiran en paneles anchos.
- Los botones principales usan `MyButton` también con bordes de nueve partes y la fuente `alagard`.
- Los fondos de las pantallas usan `background_Gray` como capa visual, manteniendo el fondo procedural para no ocultar la escena.
- El menú principal usa `MegabonkLogo` original.
- La selección de mapa usa `ForestIconBig` y `DesertIconBig`. El mapa nevado conserva su marcador porque no hay una miniatura equivalente en la extracción.
- El menú deja visible el escenario 3D del proyecto detrás de los paneles; antes lo tapaban dibujos geométricos.
- Desbloqueos usa una cuadrícula de nueve columnas, pestañas y una banda de detalles inferior. Los huecos de personajes todavía no implementados se muestran como bloqueados.
- Las casillas de personajes y desbloqueos usan `Border2` original, respetando sus esquinas.
- La transición entre etapas usa el marco y logo originales para reproducir la pantalla de carga de `LoadingScreen.unity`.

Las escenas extraídas tienen referencias a sus controladores originales; se integraron sus recursos visuales en las interfaces funcionales actuales. El escenario 3D del menú y la vista previa del personaje siguen siendo los del proyecto actual, por lo que aún no son idénticos a las capturas.

