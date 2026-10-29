# Pasivas, modelos y fidelidad — revisión del 21 de octubre

## Fuentes inspeccionadas

- MEGABONK/ExportedProject/Assets/MonoBehaviour: los 21 CharacterData y sus referencias passive; Weapon*.asset.
- Localization/String Tables/PassiveAbilities_en.asset: descripciones y comportamiento.
- Scripts/Assembly-CSharp/.../AbilitiesPassive/Implementations: campos y eventos. Los métodos están vacíos y los coeficientes no tienen inicializadores: no se recuperó el código original ejecutable.
- CHARACTER XTRACTOR/MODELOS EXTRAIDOS/ExportedProject/Assets: prefabs y mallas de objetos; las coincidencias principales ya estaban importadas. No se duplicaron assets ni GUID.

## Implementado

CharacterPassives conecta las 21 asignaciones: Clank/crítico, Knight/armadura, Calcium/acumulación de velocidad y daño, Megachad/Flex, Fox/suerte, Bandit/cadencia, Spaceman/XP, Ogre/daño, Vlad/robo de vida, Ninja/evasión-ejecución, Amog/propagación de veneno, Noelle/tamaño y congelados, Birdo/flotar y daño aéreo, Monke/trepar y vida, Athena/daño con poca vida y espinas, Robinette/oro y daño, Chadwell/dificultad, Dicehead/estadística aleatoria, Roberto/cofres gratuitos y flotación, Tony/carga por movimiento, Bush/marcas y explosión. El selector describe los efectos activos.

Las pasivas se reinician por partida, escalan desde el nivel 2 y se detienen en pausa. Se retiraron los bonus arbitrarios anteriores de evasión de Clank y daño de Knight. La trepada usa colisiones reales, no atraviesa paredes. Ejecuciones de Ninja no afectan jefes/élites. El drenaje de Vlad conserva fracciones antes de curar.

IMPORTANTE: las descripciones originales respaldan el tipo de comportamiento, no los valores elegidos. Coeficientes, límites, tiempos de recarga, frecuencia de cofres y selección de estadísticas de Gamba son reconstruidos y necesitan balance. No se afirma equivalencia exacta.

## Armas

RecoveredWeaponValues usa los daños base confirmados: revólver 5; hueso, espada, katana y misil 11; banana/arco 9; fuego 10; aura 6. El revólver dispara su secuencia de seis tiros en lugar de seis balas simultáneas y empieza sin rebotes como indica su asset. Se suman attackDuration y endCooldown para las secuencias básicas recuperadas; es una interpretación del ciclo, no una reproducción confirmada del scheduler original. Mejoras por nivel (+25%), velocidad de proyectiles y varias reglas especiales siguen reconstruidas. Los valores de armas sin un archivo inequívoco no se sustituyeron por supuestos.

## Objetos

Llave: malla original model_key. Libro sagrado: malla original Book. Se buscan los materiales del renderer que usa exactamente esa malla en KeyPickupMonke, FoxWizard y CoffinTile; solo se reutilizan si todos son válidos y compatibles. Si no, se conserva el material de color adaptado. Esto recupera materiales de esas mallas, no demuestra que el libro de escenario sea el modelo original del objeto Libro sagrado. Se conservan los prefabs ya enlazados de Borgar/fogata/máscara. CheeseHat es una malla de sombrero, no se sustituye automáticamente el queso por ella. Los demás objetos sin malla inequívoca conservan sus volúmenes de iconos; no son modelos originales completos.

## Verificación y pendientes

Revisión del 22 de octubre: los bonus de nivel se aplican antes de abrir el selector de mejoras, sin duplicarlos al reanudar. Vlad drena solo vida realmente perdida tras absorber escudos. Ninja busca un objetivo normal válido y atraviesa su escudo al ejecutarlo. Los cofres de Roberto buscan suelo transitable y reintentan si no lo encuentran. Se reinicia la referencia estática de pasivas al iniciar el runtime.

Compilación C# y revisión de fuentes; sin Play Mode ni pruebas jugables. Pendientes: verificar trepado/flotación en distintos terrenos, balance de todas las pasivas, materiales/texturas originales de llave/libro y comportamiento exacto de armas cuyo código está vacío. No se han reconstruido modelos artísticos completos para todos los objetos.
