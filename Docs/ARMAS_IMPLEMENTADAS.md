# Ampliación de armas y puntos 3–4

Se añaden 13 armas a las 10 existentes, disponibles en subidas de nivel y como arma inicial de los personajes correspondientes. Comparten daño, tamaño, crítico, velocidad de ataque y cantidad donde corresponde. Las armas se mejoran hasta nivel 10; no reemplazan las demás armas equipadas.

| Arma | Mecánica implementada |
|---|---|
| Frasco venenoso | Proyectil, nube de 3 segundos, veneno periódico |
| Dexecutioner | Corte frontal, ejecución de enemigos normales bajo 15% de vida |
| Tornado | Zona móvil con daño cada medio segundo |
| Francotirador | Proyectil rápido, perforación y crítico adicional; automático |
| Espada corrupta | Onda perforante y maldición retardada |
| Dados | Tirada 1–6, multiplicador de daño y explosión al sacar 6 |
| Égida | Cargas recargables que bloquean daño y liberan una onda |
| Paso helado | Zonas persistentes con daño y congelación |
| Hachas | Dos hachas giratorias perforantes más cantidad de la build |
| Guadaña | Barrido circular de daño |
| Agujero negro | Área de daño y atracción; jefes/élites inmunes al desplazamiento |
| Daga inalámbrica | Proyectil dirigido con saltos a objetivos nuevos |
| Magia de sangre | Proyectiles dirigidos que curan 1 punto al impactar |

Se reutilizan prefabs visuales importados cuando son válidos, iconos del Press Kit y sonidos originales disponibles. La daga ahora utiliza BluetoothProjectile, localizado en la ampliación visual. Los prefabs sin malla válida tienen presentación de respaldo diferenciada. Las zonas se eliminan al cambiar de etapa o salir de partida y se congelan durante la pausa. Hay límites de 32 emisiones por ataque nuevo y 96 zonas simultáneas para evitar saturación extrema.

Los datos fuente Weapon*.asset aportan daño/cadencia; sus métodos C# vacíos no aportan el algoritmo. Las reglas nuevas son reconstrucciones funcionales, no equivalencia exacta de balance, animaciones o efectos del juego comercial. Dice tiene daño base reconstruido porque el dato original es cero.

Punto 3: se conserva la animación original Open → Opened, con recompensa única; se corrige el tamaño y apoyo del cofre usando la pose cerrada.

Punto 4: se conserva el selector completo de 21 modelos con arrastre 360°; captura del mouse durante el giro y restauración de máscaras de cámara al cerrar la vista previa. El arma mostrada coincide con la resuelta al iniciar la partida.

Comprobación: compilación y revisión estática de rutas, no pruebas jugables. Para revisar: abrir Main, Jugar, elegir un personaje nuevo, girarlo y comenzar; subir de nivel permite añadir/mejorar armas. Los cofres se abren con E y oro suficiente. El editor regenera el catálogo versión 3 automáticamente, o mediante Tools > Megabonk > Integrar desierto, cofres y personajes originales.
