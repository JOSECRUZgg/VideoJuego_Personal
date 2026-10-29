# Revisión del reporte y correcciones

Fecha: 17 de octubre de 2026. Proyecto: UnityShooter, Unity 6000.6.0f1.

El reporte recibido se contrastó con los scripts. Las apariencias de los seis tipos de enemigos, trazadores por arma, retículo dinámico, números de daño y partículas ambientales ya estaban implementadas. Se conservaron. No era correcto considerar todo el rendimiento optimizado: persistían búsquedas globales por frame y creación de materiales sin gestión de vida útil.

## Cambios realizados

- Registro de enemigos activos y contador de vivos actualizado al habilitar, deshabilitar y morir. El spawner consulta el contador sin buscar todos los objetos; el minimapa utiliza el registro.
- Recuento actualizado después de generar enemigos, evitando terminar una oleada en el mismo frame en que aparece su último enemigo. Se reinicia el temporizador entre oleadas.
- Materiales compartidos para proyectiles, chispas, enemigos, ojos, gemas, cofres, invocaciones, arma y efectos de habilidades. Caché por shader, color y emisión, con liberación al finalizar la aplicación/sesión. La arena conserva la liberación de sus materiales propios.
- Colores de daño y estados de enemigos mediante MaterialPropertyBlock; el parpadeo de élites configura también la propiedad de color de URP.
- Proyectiles enemigos sin una luz puntual por bala y detección del trayecto recorrido entre frames para reducir impactos perdidos a baja tasa de cuadros.
- Pausa protegida durante selección de habilidades y tomos. Cerrar una selección respeta la pausa manual. Movimiento, cámara, disparo, enemigos y recogida de XP no avanzan durante la pausa.
- Reinicio de salud máxima base, regeneración, mensajes de daño, recarga, marcadores de impacto, movimiento, agachado, empuje y menús. Se limpian proyectiles enemigos, cofres, trazadores, charcos, señuelos y números de daño. Los enemigos se desactivan antes de su destrucción diferida.
- XP recibida durante una selección conservada; el excedente abre sucesivas selecciones de nivel. Se rechazan IDs de mejora desconocidos y aplicaciones duplicadas.
- Explosiones aplicadas una sola vez por enemigo aunque tenga varios colliders; se excluye al propio emisor. La ballesta explota también cuando el impacto principal mata al objetivo. El Sploder daña al jugador dentro de su radio.
- Justice cuenta bajas de Holy Strike, no todos sus impactos. El registro de bajas muestra los puntos reales del enemigo.
- API FindObjectsByType ajustada a la versión instalada, eliminando advertencias por el parámetro de ordenamiento obsoleto.
- Eliminada la captura de variable por frame al retirar números de daño; estilo del mensaje de esquiva/escudo reutilizado.

## Validación y límites

- Compilación de Assembly-CSharp y Assembly-CSharp-Editor con .NET: correcta, 0 errores y 0 advertencias. Véase `build-results.txt`.
- La compilación incluye explícitamente `Assets/Editor/ShooterRegressionChecks.cs` mediante `include-tests.targets` porque el proyecto generado aún no incluía el archivo nuevo.
- Pruebas de regresión en Play Mode preparadas para salud entre partidas, mejoras duplicadas/desconocidas, pausa de tomos, XP retenida, reinicio de menús, reutilización de materiales, altas/bajas de enemigos, cierre de oleadas y limpieza de cofres/proyectiles.
- La ejecución de Unity en batch terminó con código 1 antes de ejecutar las pruebas. Hay otras instancias de Unity abiertas y existe el archivo de bloqueo del proyecto; el log no proporciona un motivo más específico. No se cerraron esas instancias ni se eliminó el bloqueo. **Las pruebas de Play Mode no se dan por aprobadas.** Véase `unity-validation.log`.
- No se midieron FPS, memoria ni GC con el Profiler. El juego aún crea objetos de efectos, y algunos ataques usan búsquedas o consultas físicas que asignan memoria. No se afirma «cero allocations», ni se valida visualmente la escena solo por compilar.

## Repetir la verificación

Compilación desde la raíz del proyecto:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet -p:CustomBeforeMicrosoftCommonTargets=C:\Users\JOSEC\UnityShooter\Review\include-tests.targets
```

Con el proyecto cerrado en otras instancias de Unity, ejecutar las pruebas en segundo plano:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Users\JOSEC\UnityShooter' -executeMethod ShooterRegressionChecks.RunBatch -logFile 'C:\Users\JOSEC\UnityShooter\Review\unity-validation.log'
```

El runner abre Main.unity, entra a Play Mode, escribe `Review/playmode-results.txt` y cierra esa instancia con código 0/1 según el resultado. No guarda cambios de escena. Queda pendiente una comprobación visual interactiva y una sesión de Profiler con muchas oleadas.

## Respaldo

`Scripts-before-review.zip` contiene los scripts originales previos a esta revisión. No se modificaron las escenas ni los prefabs.

## Corrección posterior: mobs que mueren sin intervención

La autodetonación del Sploder reutilizaba la explosión ofensiva del jugador y después se infligía 99999 de daño. Esto mataba a enemigos vecinos y otorgaba puntos, bajas, XP y efectos de muerte al jugador sin disparar. Se separó esa ruta: el Sploder conserva su ataque al jugador y su animación de destrucción, pero no daña a otros enemigos ni concede recompensas o activa habilidades por baja. Las explosiones de armas y habilidades mantienen su daño de área. Se agregaron dos comprobaciones al runner para vecinos intactos y ausencia de recompensas; su ejecución en Play Mode sigue pendiente.
