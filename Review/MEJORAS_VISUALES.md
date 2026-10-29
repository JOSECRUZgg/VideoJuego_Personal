# Mejoras visuales — 17 de octubre de 2026

## Implementado

- Lenguaje visual compartido: fondo azul oscuro, acentos turquesa y ámbar, paneles de contraste, tipografía jerarquizada y estados de selección.
- Menú principal nuevo con acceso a expedición, supervivientes, ajustes y estadísticas, más resumen del equipo elegido.
- Selección de personaje y arma con tarjetas grandes, pasivas y datos de combate legibles.
- Ajustes en tres columnas: imagen, controles y audio. Se conservan las funciones de aplicación de ajustes existentes.
- Estadísticas y pantallas de victoria/derrota con el mismo estilo.
- Tab/Esc: catálogo con filtros de adquiridas, catálogo completo, sinergias y tomos; búsqueda por nombre/categoría; tarjetas desplazables y panel de descripción/requisitos.
- HUD redistribuido: vitalidad, munición, experiencia, tiempo, oleada y bajas. Muestra un máximo de cinco habilidades como resumen y remite a Tab para el resto.
- Cartas de subida de nivel y recompensa de jefe con estilos y escala compartidos.
- Escala de interfaz basada en un área de diseño mínima de 1600×900; conserva proporciones y amplía el área disponible según la relación de aspecto.
- Arena nocturna, marcas de orientación sin colisión y corrección del borde de la plataforma: cuatro tiras delgadas sustituyen la placa de neón que ocultaba todo el suelo central.
- Corregido un error preexistente de EnemyVisuals: GUI.skin se consultaba en Start, fuera de OnGUI. El estilo se inicializa una sola vez dentro de OnGUI. Las barras de enemigo se ocultan durante la pausa.

## Verificado

- Compilación de juego y editor: 0 errores y 0 advertencias (`visual-build.txt`).
- Comprobación interactiva en el editor de Unity: menú principal, acceso a selección, inicio de partida, ajustes, HUD, arena, apertura de Tab, catálogo y detalle de Penetration con requisito Take Aim.
- Búsqueda escrita de «Take Aim»: devolvió la tarjeta correspondiente.
- La iluminación nocturna y el suelo de la plataforma se comprobaron en Play Mode tras recargar los scripts.
- Las cartas de nivel y pantallas de fin de partida compilan, pero no se recorrieron todas sus variantes durante esta comprobación. No se ha hecho una campaña completa ni medido rendimiento con Profiler.

## Archivos principales

- `Assets/Scripts/UI/GamePresentation.cs`: tema y nuevas pantallas.
- `Assets/Scripts/UI/GameHUD.cs`: integración, escalado y navegación.
- `Assets/Scripts/UI/SkillMenuUI.cs`: cartas y fondo de selección.
- `Assets/Scripts/ArenaEnricher.cs`: presentación de arena.
- `Assets/Scripts/Enemy/EnemyVisuals.cs`: corrección de inicialización IMGUI.

Respaldo de UI y ArenaEnricher previo a esta revisión: `Review/Visual-before.zip`. No se guardaron cambios en escenas ni prefabs durante las pruebas.
