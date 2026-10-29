# Auditoría de interfaces originales

Importador v2. Se preservan jerarquías RectTransform, anclajes, escalas, orden, actividad, disposición horizontal/vertical/cuadrícula, dimensiones de layout, imágenes y RawImage disponibles. TMP se adapta a Text/alagard con alineación. No se importan controladores vacíos, Steam, eventos destructivos ni animaciones con bindings no verificados.

MainMenu / UI/Tabs/Menu: 145 nodos; 10 acciones; 2 imágenes sin sprite resuelto.
MainMenu / UI/Tabs/W_Settings: 113 nodos; 1 acciones; 6 imágenes sin sprite resuelto.
MainMenu / UI/Tabs/W_Shop: 148 nodos; 1 acciones; 1 imágenes sin sprite resuelto.
MainMenu / UI/Tabs/Character/W_Character: 194 nodos; 1 acciones; 1 imágenes sin sprite resuelto.
MainMenu / UI/Tabs/MapNew2/Maps And Stats/W_Maps: 40 nodos; 1 acciones; 1 imágenes sin sprite resuelto.
MainMenu / UI/Tabs/W_LeaderboardsFull: 293 nodos; 2 acciones; 1 imágenes sin sprite resuelto.
MainMenu / UI/Tabs/W_Quests2NEW: 127 nodos; 2 acciones; 1 imágenes sin sprite resuelto.
MainMenu / UI/Tabs/W_Credits: 29 nodos; 1 acciones; 1 imágenes sin sprite resuelto.
LoadingScreen / Canvas: 3 nodos; 0 acciones; 0 imágenes sin sprite resuelto.
GeneratedMap / GameUI/PauseUI/Main: 90 nodos; 0 acciones; 1 imágenes sin sprite resuelto.
GeneratedMap / GameUI/PauseUI/W_Settings: 113 nodos; 0 acciones; 6 imágenes sin sprite resuelto.
GeneratedMap / GameUI/GameUI/HUD: 119 nodos; 0 acciones; 1 imágenes sin sprite resuelto.
GeneratedMap / GameUI/GameUI/DeathScreen: 189 nodos; 4 acciones; 2 imágenes sin sprite resuelto.
GeneratedMap / GameUI/GameUI/DeathScreen/TierVictoryScreen: 19 nodos; 0 acciones; 0 imágenes sin sprite resuelto.
GeneratedMap / GameUI/GameUI/EncounterWindows/LevelupScreen: 121 nodos; 1 acciones; 0 imágenes sin sprite resuelto.
FinalBossMap / GameUI/PauseUI/Main: 90 nodos; 0 acciones; 1 imágenes sin sprite resuelto.
FinalBossMap / GameUI/PauseUI/W_Settings: 113 nodos; 0 acciones; 6 imágenes sin sprite resuelto.
FinalBossMap / GameUI/GameUI/HUD: 119 nodos; 0 acciones; 1 imágenes sin sprite resuelto.
FinalBossMap / GameUI/GameUI/DeathScreen: 189 nodos; 4 acciones; 2 imágenes sin sprite resuelto.
FinalBossMap / GameUI/GameUI/DeathScreen/TierVictoryScreen: 19 nodos; 0 acciones; 0 imágenes sin sprite resuelto.
FinalBossMap / GameUI/GameUI/EncounterWindows/LevelupScreen: 121 nodos; 1 acciones; 0 imágenes sin sprite resuelto.

Importador v2: conserva escalas, RawImage, disposición horizontal/vertical, LayoutElement, ContentSizeFitter y tipo de Image. El puente de menú sigue deshabilitado hasta validar en Play. Los otros prefabs son plantillas: faltan conexiones de datos y eventos.
