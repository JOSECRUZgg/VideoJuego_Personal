# Integración gradual de sistemas originales

## Datos comprobados

- Proyecto destino: Unity 6000.6.0f1, URP 17.5.0, UGUI 2.5.0; la escena activa del juego es `Assets/Scenes/Main.unity`.
- La exportación contiene `MainMenu`, `LoadingScreen`, `GeneratedMap` y `FinalBossMap`, además de los prefabs y datos de enemigos. Su `EnemyBehaviour.FixedUpdate` está vacío, `MapGenerationController.GenerateMap` devuelve `null` y las funciones de `SummonerController` están vacías. Esos controladores no se pueden reutilizar como sistemas funcionales.
- El proyecto tiene un catálogo extraído con mallas por fotogramas, materiales, velocidad, daño, vida, escudo, XP, distancia y ataques para enemigos y jefes. Las 20 identidades que usa `ExtractedEnemyMeshVisual` están presentes en el catálogo.

## Cambio integrado en esta etapa

`EnemySpawner3D.SpawnEnemy` intenta aplicar primero `ExtractedEnemyMeshVisual` a cada enemigo normal, pasando el tiempo de partida y la etapa. Si el recurso no se puede cargar, conserva `MonsterPackVisual` como respaldo. La implementación existente de `EnemyHealth3D` toma vida, escudo y XP del dato extraído cuando la asignación tuvo éxito; `EnemyAI3D` toma velocidad, daño y distancia. No se copiaron métodos decompilados vacíos.

El jefe de etapa ya leía EntStage1-3 en bosque y Pharaoh1-3 en desierto mediante `ExtractedEnemyMeshVisual`; sus ataques de raíces, pilares y láseres tienen datos serializados distintos. Se corrigió el HUD para mostrar la identidad y etapa del jefe actual, y el spawner conserva la barra de otro jefe vivo si muere primero una copia de Boss Curse.

El portal de la tercera etapa ahora inicia un encuentro final con la malla animada y los valores serializados de `GhostKingData` (500 000 de vida, 40 de daño), en vez de conceder la victoria inmediatamente. Se detienen las apariciones de hordas normales y la victoria se concede al derrotarlo. Raíces, múltiples pinchos y oscuridad usan tiempos y daño recuperados con una implementación reconstruida. La oscuridad muestra una advertencia y permite refugiarse junto a los cuatro santuarios del encuentro; usa el prefab visual importado, pero la lógica exacta de las lámparas originales no se recuperó. El mapa `FinalBossMap` original tampoco se reconstruyó porque su controlador exportado no contiene métodos funcionales. El encuentro usa el terreno transitable actual y, si falla la carga de la malla, conserva un jefe de respaldo para no bloquear la partida.

Los seis archivos StageForest1-3 y StageDesert1-3 contienen `stageTimeline` serializado. `SourceStageTimeline` recupera sus duraciones de etapa (600/540/480 segundos) y los cuatro eventos por etapa. `EnemySpawner3D` activa dos apariciones de minijefe y dos intervalos de enjambre según esos tiempos. Al cambiar de etapa se reinicia el cursor de eventos. La frecuencia exacta y las cartas de enemigos originales no se recuperaron: `SummonerController` contiene métodos vacíos, por lo que el enjambre usa la densidad existente del spawner y un multiplicador de 1.5 durante la duración del evento.

Se corrigieron tres condiciones de progreso: vencer sin daño al primer jefe solo se registra cuando han caído también sus copias de maldición; el altar Boss Curse no se consume si no pudo crear al jefe adicional; y cambiar a un personaje del selector clásico borra la identidad original anterior para no mezclar modelo, pasiva y contadores. Los santuarios de carga completados permanecen en el escenario y sus refugios se señalan en el minimapa durante el encuentro final. La quemadura conserva la atribución del arma cuando otra fuente aplica una quemadura igual o más débil.

## Aún no integrado

- Controladores originales completos de IA, oleadas, generación de mapa y jefes: la exportación carece de cuerpos funcionales en métodos esenciales. El proyecto usa adaptaciones propias.
- Las escenas originales como flujo de ejecución: Build Settings solo incluye Main y SampleScene. La pantalla de carga original está importada como plantilla, pero no conecta escenas.
- El menú original y los otros paneles importados: las jerarquías visuales existen, pero el puente del menú sigue desactivado hasta validación visual y faltan conexiones de datos.
- Variedad completa de jefes, la arena original de `FinalBossMap` y la fidelidad exacta del ataque de oscuridad del Rey Fantasma.
- Los desbloqueos de objetos ya no usan la meta falsa de bajas según rareza. Los requisitos definidos en el catálogo se conectaron a contadores persistentes: tomos, cofres, vasijas, vida máxima, dificultad, niveles y personajes, tipos de ataque, curación por robo de vida, críticos, muertes, santuarios, maldiciones de jefe, supervivencia al enjambre y victorias finales. La condición `a_demonicBlood.asset` indica `bloodMagicProcs` con objetivo 400; se cuenta al impactar Magia de Sangre. Algunas traducciones de condición y las reglas reconstruidas de desafíos no garantizan equivalencia con el original. El fallback de una condición no reconocida permanece bloqueado y se etiqueta como pendiente, no se desbloquea por bajas ajenas.

## Verificación

Se verificaron rutas y datos estáticamente y se compiló C#. Falta validar en Play los modelos, fotogramas animados, colisiones, spawn escalonado y recompensas; la compilación no confirma su comportamiento visual.
