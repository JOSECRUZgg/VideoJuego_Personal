# Auditoría de efectos de la build

Revisión estática del código — 19 de octubre de 2026. No se ejecutaron partidas ni pruebas de comportamiento. Una referencia demuestra conexión en código, no garantiza el resultado visual ni el balance en Unity.

## Catálogo de objetos

Se contrastaron los disparadores de adquisición, impacto, muerte, daño recibido, curación, movimiento, cofre y temporizadores con los consumidores de estadísticas. Las rutas siguientes permiten localizar cada efecto y su reversión cuando corresponda.

| Objeto | ID | Implementación |
| --- | --- | --- |
| Moldy Cheese | `moldy_cheese` | `Assets/Scripts/Player/MegabonkRelicManager.cs:586` |
| Borgar | `borgar` | `Assets/Scripts/Player/MegabonkRelicManager.cs:603` |
| Wrench | `wrench` | `Assets/Scripts/StageInteractable.cs:193` |
| Clover | `clover` | `Assets/Scripts/Player/MegabonkRelicManager.cs:709`; `Assets/Scripts/Player/MegabonkRelicManager.cs:746` |
| Time Bracelet | `time_bracelet` | `Assets/Scripts/Player/XPSystem.cs:213` |
| Slippery Ring | `slippery_ring` | `Assets/Scripts/Player/MegabonkRelicManager.cs:714`; `Assets/Scripts/Player/MegabonkRelicManager.cs:761` |
| Golden Glove | `golden_glove` | `Assets/Scripts/Enemy/EnemyHealth3D.cs:394` |
| Gym Sauce | `gym_sauce` | `Assets/Scripts/Player/MegabonkRelicManager.cs:710`; `Assets/Scripts/Player/MegabonkRelicManager.cs:749` |
| Oats | `oats` | `Assets/Scripts/Player/MegabonkRelicManager.cs:711`; `Assets/Scripts/Player/MegabonkRelicManager.cs:752` |
| MedKit | `medkit` | `Assets/Scripts/Player/MegabonkRelicManager.cs:839` |
| Turbo Socks | `turbo_socks` | `Assets/Scripts/Player/MegabonkRelicManager.cs:712`; `Assets/Scripts/Player/MegabonkRelicManager.cs:755` |
| Tactical Goggles | `tactical_goggles` | `Assets/Scripts/Player/MegabonkBuild.cs:151` |
| Battery | `battery` | `Assets/Scripts/Player/MegabonkRelicManager.cs:713`; `Assets/Scripts/Player/MegabonkRelicManager.cs:758` |
| Forbidden Juice | `forbidden_juice` | `Assets/Scripts/Player/MegabonkBuild.cs:13` |
| Cursed Doll | `cursed_doll` | `Assets/Scripts/Player/MegabonkRelicManager.cs:840` |
| Boss Buster | `boss_buster` | `Assets/Scripts/Player/MegabonkBuild.cs:152` |
| Key | `key` | `Assets/Scripts/Enemy/BossChest3D.cs:138`; `Assets/Scripts/Enemy/BossChest3D.cs:141` |
| Ice Crystal | `ice_crystal` | `Assets/Scripts/Player/MegabonkRelicManager.cs:587` |
| Skuleg | `skuleg` | `Assets/Scripts/Enemy/EnemyHealth3D.cs:396`; `Assets/Scripts/Player/MegabonkRelicManager.cs:292` |
| Ghost | `ghost` | `Assets/Scripts/Player/MegabonkRelicManager.cs:410`; `Assets/Scripts/Player/MegabonkRelicManager.cs:417` |
| Cactus | `cactus` | `Assets/Scripts/ArenaEnricher.cs:667`; `Assets/Scripts/Player/PlayerHealth3D.cs:81` |
| Credit Card (Red) | `credit_card_red` | `Assets/Scripts/Player/MegabonkRelicManager.cs:398`; `Assets/Scripts/Player/MegabonkRelicManager.cs:400` |
| Golden Shield | `golden_shield` | `Assets/Scripts/Player/MegabonkRelicManager.cs:626` |
| Thunder Mitts | `thunder_mitts` | `Assets/Scripts/Player/MegabonkRelicManager.cs:562` |
| Feathers | `feathers` | `Assets/Scripts/Player/MegabonkRelicManager.cs:726`; `Assets/Scripts/Player/MegabonkRelicManager.cs:773` |
| Electric Plug | `electric_plug` | `Assets/Scripts/Player/PlayerHealth3D.cs:90` |
| Coward's Cloak | `cowards_cloak` | `Assets/Scripts/Player/MegabonkRelicManager.cs:628`; `Assets/Scripts/Player/MegabonkRelicManager.cs:631`; `Assets/Scripts/Player/MegabonkRelicManager.cs:727`; `Assets/Scripts/Player/MegabonkRelicManager.cs:776` |
| Unstable Transfusion | `unstable_transfusion` | `Assets/Scripts/Player/MegabonkRelicManager.cs:578` |
| Beer | `beer` | `Assets/Scripts/Player/MegabonkRelicManager.cs:715`; `Assets/Scripts/Player/MegabonkRelicManager.cs:764` |
| Beacon | `beacon` | `Assets/Scripts/Player/MegabonkRelicManager.cs:845`; `Assets/Scripts/Player/MegabonkRelicManager.cs:847` |
| Backpack | `backpack` | `Assets/Scripts/Player/MegabonkBuild.cs:11` |
| Phantom Shroud | `phantom_shroud` | `Assets/Scripts/Player/PlayerHealth3D.cs:64` |
| Campfire | `campfire` | `Assets/Scripts/Player/MegabonkRelicManager.cs:868`; `Assets/Scripts/Player/MegabonkRelicManager.cs:875` |
| Brass Knuckles | `brass_knuckles` | `Assets/Scripts/Player/MegabonkBuild.cs:142` |
| Demonic Blade | `demonic_blade` | `Assets/Scripts/Player/MegabonkRelicManager.cs:590` |
| Demonic Blood | `demonic_blood` | `Assets/Scripts/Player/MegabonkRelicManager.cs:607` |
| Echo Shard | `echo_shard` | `Assets/Scripts/Player/MegabonkRelicManager.cs:605` |
| Moldy Gloves | `moldy_gloves` | `Assets/Scripts/Player/MegabonkRelicManager.cs:582` |
| Golden Sneakers | `golden_sneakers` | `Assets/Scripts/Player/MegabonkRelicManager.cs:885`; `Assets/Scripts/Player/MegabonkRelicManager.cs:887` |
| Idle Juice | `idle_juice` | `Assets/Scripts/Player/MegabonkBuild.cs:144` |
| Leeching Crystal | `leeching_crystal` | `Assets/Scripts/Player/MegabonkRelicManager.cs:725`; `Assets/Scripts/Player/MegabonkRelicManager.cs:770` |
| Kevin | `kevin` | `Assets/Scripts/Player/MegabonkRelicManager.cs:841` |
| Beefy Ring | `beefy_ring` | `Assets/Scripts/Player/MegabonkRelicManager.cs:514` |
| Credit Card (Green) | `credit_card_green` | `Assets/Scripts/Player/MegabonkRelicManager.cs:404`; `Assets/Scripts/Player/MegabonkRelicManager.cs:406` |
| Spiky Shield | `spiky_shield` | `Assets/Scripts/Player/PlayerHealth3D.cs:68`; `Assets/Scripts/Player/PlayerHealth3D.cs:81` |
| Scarf | `scarf` | `Assets/Scripts/Player/MegabonkBuild.cs:143` |
| Slurp Gloves | `slurp_gloves` | `Assets/Scripts/Player/MegabonkRelicManager.cs:569` |
| Mirror | `mirror` | `Assets/Scripts/Player/PlayerHealth3D.cs:78`; `Assets/Scripts/Player/PlayerHealth3D.cs:92` |
| Turbo Skates | `turbo_skates` | `Assets/Scripts/Player/MegabonkRelicManager.cs:526` |
| Slutty Cannon | `slutty_cannon` | `Assets/Scripts/Player/MegabonkRelicManager.cs:609` |
| Shattered Knowledge | `shattered_knowledge` | `Assets/Scripts/Enemy/XpOrb3D.cs:175` |
| Quin's Mask | `quins_mask` | `Assets/Scripts/Player/PlayerHealth3D.cs:88` |
| Toxic Barrel | `toxic_barrel` | `Assets/Scripts/Player/PlayerHealth3D.cs:91` |
| Gas Mask | `gas_mask` | `Assets/Scripts/Player/PlayerHealth3D.cs:70`; `Assets/Scripts/Player/PlayerHealth3D.cs:72` |
| Gamer Goggles | `gamer_goggles` | `Assets/Scripts/Player/MegabonkRelicManager.cs:516` |
| Eagle Claw | `eagle_claw` | `Assets/Scripts/Player/MegabonkBuild.cs:146` |
| Demonic Soul | `demonic_soul` | `Assets/Scripts/Player/MegabonkRelicManager.cs:509` |
| Bob (Dead) | `bob_dead` | `Assets/Scripts/Player/MegabonkRelicManager.cs:897`; `Assets/Scripts/Player/MegabonkRelicManager.cs:905` |
| Grandma's Secret Tonic | `grandmas_secret_tonic` | `Assets/Scripts/Player/MegabonkRelicManager.cs:589` |
| Cursed Grabbies | `cursed_grabbies` | `Assets/Scripts/Player/MegabonkRelicManager.cs:292`; `Assets/Scripts/Player/MegabonkRelicManager.cs:580` |
| Overpowered Lamp | `op_lamp` | `Assets/Scripts/Player/MegabonkRelicManager.cs:94`; `Assets/Scripts/Player/MegabonkRelicManager.cs:539` |
| Za Warudo | `za_warudo` | `Assets/Scripts/Player/MegabonkRelicManager.cs:91`; `Assets/Scripts/Player/MegabonkRelicManager.cs:780` |
| Big Bonk | `big_bonk` | `Assets/Scripts/Player/MegabonkBuild.cs:150`; `Assets/Scripts/Player/MegabonkRelicManager.cs:92` |
| Spicy Meatball | `spicy_meatball` | `Assets/Scripts/Player/MegabonkRelicManager.cs:93`; `Assets/Scripts/Player/MegabonkRelicManager.cs:592` |
| Ice Cube | `ice_cube` | `Assets/Scripts/Player/MegabonkRelicManager.cs:542` |
| Giant Fork | `giant_fork` | `Assets/Scripts/Player/MegabonkBuild.cs:149` |
| Power Gloves | `power_gloves` | `Assets/Scripts/Player/MegabonkRelicManager.cs:555` |
| Lightning Orb | `lightning_orb` | `Assets/Scripts/Player/MegabonkRelicManager.cs:549` |
| Chonkplate | `chonkplate` | `Assets/Scripts/Player/MegabonkBuild.cs:158`; `Assets/Scripts/Player/PlayerHealth3D.cs:32` |
| Dragonfire | `dragonfire` | `Assets/Scripts/Player/MegabonkRelicManager.cs:547` |
| Sucky Magnet | `sucky_magnet` | `Assets/Scripts/Player/MegabonkRelicManager.cs:856`; `Assets/Scripts/Player/MegabonkRelicManager.cs:858` |
| Holy Book | `holy_book` | `Assets/Scripts/Player/MegabonkRelicManager.cs:618`; `Assets/Scripts/Player/MegabonkRelicManager.cs:621`; `Assets/Scripts/Player/MegabonkRelicManager.cs:708`; `Assets/Scripts/Player/MegabonkRelicManager.cs:779`; `Assets/Scripts/Player/MegabonkRelicManager.cs:839` |
| Speed Boi | `speed_boi` | `Assets/Scripts/Player/MegabonkRelicManager.cs:24`; `Assets/Scripts/Player/MegabonkRelicManager.cs:517`; `Assets/Scripts/Player/MegabonkRelicManager.cs:518` |
| Energy Core | `energy_core` | `Assets/Scripts/Player/MegabonkRelicManager.cs:851`; `Assets/Scripts/Player/MegabonkRelicManager.cs:853` |
| Anvil | `anvil` | `Assets/Scripts/Player/MegabonkBuild.cs:55` |
| Soul Harvester | `soul_harvester` | `Assets/Scripts/Player/MegabonkRelicManager.cs:611`; `Assets/Scripts/Player/MegabonkRelicManager.cs:612` |
| Joe's Dagger | `joes_dagger` | `Assets/Scripts/Player/MegabonkBuild.cs:155` |
| Bloody Cleaver | `bloody_cleaver` | `Assets/Scripts/Player/MegabonkBuild.cs:164`; `Assets/Scripts/Player/MegabonkRelicManager.cs:576`; `Assets/Scripts/Player/MegabonkRelicManager.cs:588` |

## Armas y tomos

| Elemento | ID | Aplicación |
| --- | --- | --- |
| Revólver | `mb_revolver` | MegabonkBuild.Attack → Hit / MegabonkProjectile; daño, crítico, tamaño y cadencia compartidos. |
| Hueso | `mb_bone` | MegabonkBuild.Attack → Hit / MegabonkProjectile; daño, crítico, tamaño y cadencia compartidos. |
| Espada | `mb_sword` | MegabonkBuild.Attack → Hit / MegabonkProjectile; daño, crítico, tamaño y cadencia compartidos. |
| Katana | `mb_katana` | MegabonkBuild.Attack → Hit / MegabonkProjectile; daño, crítico, tamaño y cadencia compartidos. |
| Arco | `mb_bow` | MegabonkBuild.Attack → Hit / MegabonkProjectile; daño, crítico, tamaño y cadencia compartidos. |
| Bastón de rayos | `mb_lightning` | MegabonkBuild.Attack → Hit / MegabonkProjectile; daño, crítico, tamaño y cadencia compartidos. |
| Bastón de fuego | `mb_fire` | MegabonkBuild.Attack → Hit / MegabonkProjectile; daño, crítico, tamaño y cadencia compartidos. |
| Aura | `mb_aura` | MegabonkBuild.Attack → Hit / MegabonkProjectile; daño, crítico, tamaño y cadencia compartidos. |
| Misiles | `mb_rocket` | MegabonkBuild.Attack → Hit / MegabonkProjectile; daño, crítico, tamaño y cadencia compartidos. |
| Tomo de Daño | `mb_damage` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Enfriamiento | `mb_cooldown` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Cantidad | `mb_quantity` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Agilidad | `mb_speed` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Tamaño | `mb_size` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Atracción | `mb_attraction` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Experiencia | `mb_xp` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Suerte | `mb_luck` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Maldito | `mb_cursed` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Vida | `mb_health` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Armadura | `mb_armor` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Regeneración | `mb_regen` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |
| Tomo de Precisión | `mb_precision` | MegabonkBuild.Add / Level → XPSystem, PlayerMovement3D, PlayerHealth3D o MegabonkRelicManager, según estadística. |

## Correcciones principales

- Bob inicializa el componente de aliados; buffs de movimiento usan entrada de movimiento, no velocidad nominal en reposo.
- On-hit se evalúa antes del golpe mortal; Lamp repite efectos sin saltarse el enfriamiento entre ataques.
- Explosiones de armas y misiles de reliquias escalan; misiles de reliquias recuperan un objetivo vivo.
- Veneno de guantes es veneno en área; escudo, robo de vida, Bloodmark, crítico y empuje tienen consumidores reales.
- Speed Boi ralentiza enemigos/proyectiles. Za Warudo los congela y evita muerte una vez por partida.
- Oro y XP conservan fracciones; las gemas nacen sobre terreno y Shattered Knowledge usa su trayecto.
- Sacrificar Beer restaura únicamente la vida máxima efectivamente descontada; duplicar Za Warudo no rearma el uso.
- Descripciones ajustadas a esta implementación; no se certifica equivalencia exacta con el juego comercial.

## Validación

Compilación estática de Assets/Scripts contra las referencias Unity del proyecto: sin errores. Advertencias de campos visuales antiguos sin uso. Pendiente de validación en partida por el usuario: combinaciones, balance, animaciones y costes en escenas reales.
