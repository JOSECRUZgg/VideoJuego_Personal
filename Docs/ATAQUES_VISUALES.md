# Corrección de ataques visibles

- Fox/Zorro usa mb_fire; Bandit_0 usa mb_dexecutioner, confirmado en OriginalContentCatalog.asset.
- BuildModelPresentation generaba adornos orbitales mediante IconVolume cuando no encontraba malla. Se retiró esa presentación permanente de armas; los iconos del HUD y modelos de recompensa se conservan.
- WeaponCastMotion dibuja un movimiento corto de lanzamiento/barrido al ejecutar fuego, espada, katana o guadaña. Es una reconstrucción procedural, no un clip original de animación del esqueleto del personaje. Se pausa con el juego y desaparece al terminar la partida.
- Bandido ya no usa ese barrido: WeaponDexecutioner.asset enlaza un ataque sin renderizadores con DexecutionerProjectilePrefab, que sí tiene partículas. Ahora lanza una onda frontal con ese prefab y una malla simple de reserva si falla su material. El umbral de ejecución se aplica al impacto.
- Auditoría estática: los 21 personajes del catálogo utilizan 21 identificadores de arma inicial presentes en el sistema de ataques. Esta auditoría verifica rutas de código, no garantiza que todos los efectos se vean correctamente en Play.
- Las armas de corte seleccionan enemigos dentro de su radio efectivo antes de consumir el enfriamiento. Antes podían atacar hacia enemigos a 45 unidades sin llegar a dañarlos.
- MegabonkProjectile inicializa su lógica antes de cargar efectos; un fallo visual no cancela su creación. El error se registra para poder reparar el asset correspondiente.
- El barrido básico recorre una copia del registro de enemigos para no invalidar el recorrido al morir un objetivo.

Pendiente de validación en Play: elegir Fox y Bandit, acercarse a enemigos, comprobar animación, pérdida de vida enemiga y ausencia de adornos orbitales; repetir con otras armas. No se afirma que todos los personajes ni los VFX originales hayan sido validados.
