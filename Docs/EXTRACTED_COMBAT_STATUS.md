# Combate conectado a datos extraídos

El dump Cpp2IL identifica EnemyMovementRb, EEnemyState y SpecialAttackController, pero sus métodos están vacíos. No se ha recuperado el código nativo. La implementación de este proyecto reconstruye comportamientos y utiliza los valores serializados de EnemyData del export proporcionado.

- Catálogo versión 2: importa vida, daño, escudo, XP, velocidad, distancias y ataques habilitados con carga, radio, duración, alcance, recuperación y enfriamiento.
- Los enemigos que reciben un modelo del catálogo aplican sus estadísticas. Vida ajustada por dificultad y tier del proyecto; escudo absorbe daño antes de la vida.
- Ent: zonas de raíces y líneas simples/triples. Pharaoh: rayos fijos/giratorios y zonas de pilares. Avisos amarillos antes del daño; rojo durante el ataque; recuperación antes de perseguir nuevamente. Se cancelan al morir y se suspenden con pausa/congelación.
- El controlador selecciona ataques soportados y aplica sus valores. La geometría, cantidad de zonas, velocidad de giro, orden y frecuencia de daño del láser son reconstrucciones del proyecto, no cálculos recuperados.
- Colisión de cápsula ajustada al tamaño normalizado de la malla.

Movimiento actualizado: evasión local de obstáculos con barridos y pasos subdivididos, comprobación de altura del terreno, cancelación de cargas contra paredes. No es búsqueda global de rutas y puede atascarse en recintos cerrados. Los tiradores usan sus distancias mínima/máxima del catálogo. Proyectiles aéreos conservan dirección vertical y los proyectiles se destruyen al encontrar geometría sólida. Colores independientes por proyectil.

La población básica conserva esqueletos y mezcla variantes habilitadas por tiempo: esqueletos acorazados y gólems en bosque; momias y momias tanque en desierto. Primeros 90 segundos reservados a esqueletos. Se confirmó el catálogo serializado versión 2 con ataques de Ent y Pharaoh.

Pendiente: fidelidad de todos los ataques, IA especial de todos los mobs, navegación global, calendario completo de aparición por mapa y verificación visual/en partida. Los datos del dump 1.0.65 y del export de assets pueden corresponder a versiones distintas. No se ejecutaron pruebas de juego.
