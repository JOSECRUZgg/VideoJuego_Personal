# Integración de interfaces — 22 octubre 2026

## Estado real: integración parcial

Se analizaron MainMenu, LoadingScreen, GeneratedMap y FinalBossMap de la exportación facilitada. Los controladores originales contienen métodos vacíos: copiar escenas no recupera sus funciones.

Se generaron 21 prefabs visuales en `Assets/Resources/OriginalUI`. El detalle está en `OriginalInterfaceAudit.md`. Conservan jerarquía RectTransform, anclajes, actividad y sprites resolubles. No son copias completas: faltan grupos de distribución, localización, animaciones, RawImage y conexiones de datos. TMP se adapta temporalmente a Text/alagard. El contador de imágenes incluye también imágenes originalmente sin sprite.

## Funciones implementadas en las interfaces compatibles existentes

- Misiones abre los requisitos reales de desbloqueo del sistema actual, no una clasificación.
- Créditos tiene su propia pantalla.
- Clasificación muestra únicamente el registro local; no simula jugadores ni servicio online.
- Ajustes guardan preferencias; incluye pantalla completa y resolución en ejecutable, sin redimensionar Game View.
- Pausa permite abrir ajustes, reanudar y reiniciar. Cerrar ajustes con Escape vuelve a pausa.
- Reiniciar inicia otra partida y no simplemente devuelve al menú.
- Transición de carga bloquea entradas y dibuja pantalla antes de generar el mapa existente. La generación sigue siendo síncrona; no se inventa porcentaje. Se añadió un método para cargar escenas asíncronas, pero el juego sigue utilizando Main y no se cambiaron Build Settings.
- Victoria y derrota solo se aceptan desde Playing. Pausa bloqueada durante carga y ventanas de recompensas.

## Pruebas realizadas

- Compilación C# con dotnet y referencias del proyecto: **0 errores, 175 advertencias**. No equivale a prueba de juego.
- Importación en el Unity abierto: generados los 21 prefabs y auditoría. Se corrigió conflicto Image/Text durante la conversión.
- Play del menú importado: **falló validación visual** (elementos descolocados y texto de prueba original). Se detuvo Play y el menú importado queda deshabilitado por defecto mediante `OriginalMenuBridge.EnableImportedMenu`. El menú compatible permanece como ruta activa.
- Pendiente: comprobar navegación completa del menú compatible, partida/carga, pausa/tiempo, persistencia tras reinicio, datos del HUD y disparadores de resultados/subida de nivel. No se declaran aprobados.

## Archivos principales

- `Assets/Editor/OriginalInterfaceImporter.cs`: importación visual incremental y auditoría. Menú Tools/Megabonk/Importar jerarquías UI originales.
- `Assets/Scripts/UI/OriginalMenuBridge.cs` y `OriginalMenuAction.cs`: puente de eventos, deshabilitado por defecto hasta corregir distribución.
- `Assets/Scripts/UI/InterfaceIntegration.cs`: navegación, preferencias, Misiones y Créditos.
- `Assets/Scripts/UI/UiTransition.cs`: transición y bloqueo de carga.
- `Assets/Scripts/GameManager.cs`: estados, pausa, reinicio y carga.
- `Assets/Scripts/UI/GameHUD.cs` y `GamePresentation.cs`: conexiones de pantallas compatibles.

## Trabajo pendiente para cumplir el prompt completo

Reconstruir los componentes de layout y localización originales, resolver sprites reales faltantes, preservar TMP y animaciones viables, conectar controles originales de tienda/personajes/mapas/ajustes/misiones, y alimentar HUD/resultados/level-up originales con datos reales. Solo después habilitar el puente y repetir pruebas en Play. Las escenas originales no fueron sustituidas ni añadidas al ejecutable; combate, progresión y mapas no se reemplazaron.

Actualización: la tienda compatible vende ahora un espacio adicional de arma y otro de tomo por 400 de plata cada uno. Los rangos se guardan y `MegabonkBuild` los aplica al comenzar la siguiente expedición. Los cuatro indicadores del menú principal usan métricas persistentes que corresponden a sus textos; antes mezclaban bajas, jefes, intentos y oleadas con objetivos distintos. Esto no convierte las plantillas de interfaz original en pantallas activas.

## Continuación: importador v2

Se identificó que Menu/Content utiliza VerticalLayoutGroup para dar dimensiones a Main, cuyo RectTransform exportado tiene tamaño cero. Sin ese componente todos los descendientes se ubicaban alrededor de un contenedor sin tamaño. El logo además es RawImage y usa escala 0.4, ambas omitidas por v1.

El importador ahora reconstruye VerticalLayoutGroup, HorizontalLayoutGroup, GridLayoutGroup, LayoutElement y ContentSizeFitter; conserva escalas, Image.Type, preserveAspect, fillAmount y componentes deshabilitados. Añade RawImage y alineación del texto. Ya no confunde referencias m_Text de otros scripts con componentes de texto. Se desactiva el Funfact de prueba y se traducen las etiquetas de navegación conocidas.

La revisión se regenera automáticamente tras recompilar en Unity fuera de Play; también puede ejecutarse desde Tools/Megabonk/Importar jerarquías UI originales. No se habilita el puente sin validación visual. Pendientes: comprobar v2 en Play y terminar conexiones de datos y controles de los paneles. Este cambio no equivale a completar esas pruebas ni la integración total.
