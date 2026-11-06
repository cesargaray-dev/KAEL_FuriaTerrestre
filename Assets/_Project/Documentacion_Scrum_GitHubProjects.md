# 🚀 KAEL: FURIA TERRESTRE — PLAN MAESTRO SCRUM & GITHUB PROJECTS

**Proyecto:** KAEL: Furia Terrestre (Action RPG 2D en Unity 6)  
**Metodología:** Marco Ágil Scrum (4 Sprints de Desarrollo con entregas incrementales)  
**Repositorio GitHub:** [`cesargaray-dev/KAEL_FuriaTerrestre`](https://github.com/cesargaray-dev/KAEL_FuriaTerrestre)  
**Tablero de Proyecto:** `KAEL: Furia Terrestre - Roadmap` (GitHub Projects)  
**Fecha de Inicio del Proyecto:** 14 de Octubre de 2026  
**Fecha Estimada de Finalización:** 21 de Noviembre de 2026  

---

## 👥 1. Equipo de Desarrollo y Reparto de Responsabilidades (50% / 50%)

El equipo de desarrollo se organiza bajo una estructura paritaria de desarrollo ágil, asegurando una distribución equitativa de carga de trabajo, módulos arquitectónicos y diseño de arte y experiencia:

| Integrante | Usuario GitHub | Rol Principal | Especialidad Técnica y Módulos | Carga de Issues |
| :--- | :--- | :--- | :--- | :---: |
| **Cesar Enrique Garay García** | `@cesargaray-dev` / `@Cesax69` | **Tech Lead & Senior Unity Developer** | Arquitectura de combate, máquinas de estado de Kael, físicas, optimización de fuentes nativas en OnGUI, sistema de audio con crossfade, oleadas, Nivel 3 (Nave Núcleo) y lógica de jefes. | **10 Issues (50%)** |
| **Zahir Andrés Rodríguez Mora** | `@ZahirAndres` | **Senior Technical Artist & Systems Integrator** | Dirección de arte 2D, escenarios de niveles (Nivel 2 Base Vorst y Nivel 4 Trono de la Disonancia), spritesheets de enemigos, animación frame-by-frame, diseño de interfaz de arsenal y códice. | **10 Issues (50%)** |

---

## 📅 2. Cronograma de Sprints y Milestones

Basado en la auditoría del historial de commits de Git, el proyecto se distribuye en 4 Sprints cronológicos:

```
[SPRINT 1: 14 Oct - 21 Oct] ➔ [SPRINT 2: 22 Oct - 05 Nov] ➔ [SPRINT 3: 06 Nov - 13 Nov] ➔ [SPRINT 4: 14 Nov - 21 Nov]
       (COMPLETADO)                   (COMPLETADO)                  (EN PROGRESO)                   (PLANIFICADO)
```

### 🏁 Milestone 1: Sprint 1 — Prototipo, Combate y Nivel 1: Sector 7
- **Periodo:** 14 de Octubre de 2026 – 21 de Octubre de 2026
- **Estado en GitHub:** `Closed / Completado`
- **Objetivo Central:** Construcción del entorno base de Unity 6, lógica inicial de Kael, IA de Caminantes Vorst, generador de 10 oleadas y la escena jugable `Lab_Cesar`.

### 🏁 Milestone 2: Sprint 2 — Menú, Narrativa N.E.X.A., Audio, Action RPG y Nivel 2
- **Periodo:** 22 de Octubre de 2026 – 05 de Noviembre de 2026
- **Estado en GitHub:** `Closed / Completado`
- **Objetivo Central:** Integración del Menú Principal con cinemática de 4 actos, diálogos holográficos de N.E.X.A., transición del control automático a **Action RPG manual (`A, S, D, J, K`)**, AudioManager persistente con crossfade de 4 pistas MP3, Árbol de Mejoras modal y creación completa del Nivel 2 (`Lab_Zahir`).

### 🏁 Milestone 3: Sprint 3 — Combate Táctico Avanzado, Tienda de Arsenal y Cinemática Puente
- **Periodo:** 06 de Noviembre de 2026 – 13 de Noviembre de 2026
- **Estado en GitHub:** `Open / En Progreso`
- **Objetivo Central:** Incorporar mecánicas de Bloqueo Perfecto (*Parry*), Modo Sobremarcha (*Overdrive*), Dron Centinela de soporte, catálogo de iconos pixel-art HD en el Árbol de Habilidades, Tienda de Arsenal con personalización de guanteletes y cinemática interactiva de transición entre Nivel 1 y Nivel 2.

### 🏁 Milestone 4: Sprint 4 — Moralidad Visual, Códice Vorst y Desenlace en Niveles 3 y 4
- **Periodo:** 14 de Noviembre de 2026 – 21 de Noviembre de 2026
- **Estado en GitHub:** `Open / Planificado`
- **Objetivo Central:** Sistema de mutación física (Compasión vs Disonancia), Códice/Bestiario táctico en el menú, Nivel 3 (Nave Núcleo), Nivel 4 (Trono de la Disonancia con Jefe de 3 fases) y 3 cinemáticas de desenlace según la ética del jugador.

---

## 🏷️ 3. Sistema de Etiquetas (GitHub Labels)

Para clasificar los ítems de backlog y permitir filtros en los tableros Kanban de GitHub Projects:

- `sprint-1`: Tareas del Sprint 1 (Prototipo y Sector 7).
- `sprint-2`: Tareas del Sprint 2 (Nivel 2, Audio y Action RPG).
- `sprint-3`: Tareas del Sprint 3 (Combate Táctico y Arsenal).
- `sprint-4`: Tareas del Sprint 4 (Lore, Moral y Desenlace).
- `programacion`: Lógica C#, físicas, algoritmos de combate e IA.
- `arte-animacion`: Spritesheets, diseño visual de escenarios, VFX y shaders.
- `ui-ux`: Menús, ventanas modales, renderizado de fuentes nativas y HUD.
- `narrativa-niveles`: Diseño de oleadas, cinemáticas, diálogos y dilemas éticos.
- `audio-vfx`: Música dinámica, efectos sonoros sincronizados y partículas.
- `completado`: Tareas validadas y fusionadas en `main`.
- `por-hacer`: Tareas activas en backlog para desarrollo.

---

## 📋 4. Desglose Detallado de los 20 Issues de GitHub

### 🟢 SPRINT 1: ARQUITECTURA BASE Y NIVEL 1 (COMPLETADO)

#### Issue #8: `[S1 - DONE] Arquitectura de Combate, Combo de 3 Golpes y Habilidad Ultimate "Sobrecarga Nexo"`
- **Asignado a:** Cesar Enrique Garay García (`@cesargaray-dev`)
- **Etiquetas:** `sprint-1`, `programacion`, `audio-vfx`, `completado`
- **Archivos:** `KaelAutoController.cs`, `CombatVFXManager.cs`, `PlayerStats.cs`
- **Descripción:** Implementación del sistema de combate cuerpo a cuerpo de Kael con detección por `RaycastAll` / `OverlapBox`, combo secuencial de 3 golpes (con 3er impacto radial y empuje) y corrutina cinemática de Ultimate de 3 fases con láser triple y vibración de cámara.

#### Issue #9: `[S1 - DONE] Sistema de 10 Oleadas, Progresión de Dificultad y Mini-Jefe Vorst Sector 7`
- **Asignado a:** Cesar Enrique Garay García (`@cesargaray-dev`)
- **Etiquetas:** `sprint-1`, `programacion`, `narrativa-niveles`, `completado`
- **Archivos:** `WaveSpawner.cs`, `EnemyDummy.cs`
- **Descripción:** Bucle principal de juego en `Lab_Cesar.unity` con escalado matemático de atributos (+15% HP/daño por oleada), pausas de respiro táctico, Mini-Jefe en Oleada 5 y Comandante Vorst en Oleada 10 con barra de vida superior.

#### Issue #10: `[S1 - DONE] Spritesheet de Kael, Animaciones 2D y Escenario Urbano Sector 7`
- **Asignado a:** Zahir Andrés Rodríguez Mora (`@ZahirAndres`)
- **Etiquetas:** `sprint-1`, `arte-animacion`, `narrativa-niveles`, `completado`
- **Archivos:** `Assets/_Project/Sprites/Kael/`, `Assets/_Project/Animations/Kael/`, `Lab_Cesar.unity`
- **Descripción:** Creación del atlas de sprites de Kael con 7 animaciones base (Idle, Run, Attacks 1-3, Hurt, Die), Animator Controller libre de saltos de pivote y composición del fondo urbano destruido del Sector 7.

#### Issue #11: `[S1 - DONE] IA de Caminantes Vorst, Máquina de Estados y Detección de Daño`
- **Asignado a:** Zahir Andrés Rodríguez Mora (`@ZahirAndres`)
- **Etiquetas:** `sprint-1`, `programacion`, `arte-animacion`, `completado`
- **Archivos:** `Vorst_Walker.prefab`, `EnemyDummy.cs`
- **Descripción:** Inteligencia artificial básica para los invasores Vorst: marcha hacia Kael, rango de ataque, ciclo de muerte con disolución, recompensas en monedas/experiencia y retroalimentación visual con parpadeo blanco (`HitFlash`).

---

### 🟢 SPRINT 2: AUDIO, ACTION RPG Y NIVEL 2 (COMPLETADO)

#### Issue #12: `[S2 - DONE] Nivel 2 Base Vorst: Escenario de Fortaleza y Nuevos Enemigos Vorst`
- **Asignado a:** Zahir Andrés Rodríguez Mora (`@ZahirAndres`)
- **Etiquetas:** `sprint-2`, `arte-animacion`, `narrativa-niveles`, `completado`
- **Archivos:** `Lab_Zahir.unity`, `Resources/Level2/`, `EnemyDummy.cs`
- **Descripción:** Creación de la escena `Lab_Zahir` con fondo de fortaleza alienígena y esfera de convergencia, y 3 nuevos enemigos en lienzos 256x256 con precarga en memoria: Stalker (rápido), Bruto (tanque con puñetazo telegrafiado) y Arconte (volador con rayos).

#### Issue #13: `[S2 - DONE] Balanceo de Parámetros del Nivel 2, Efectos de Sonido y Sistema de Spawner`
- **Asignado a:** Zahir Andrés Rodríguez Mora (`@ZahirAndres`)
- **Etiquetas:** `sprint-2`, `programacion`, `audio-vfx`, `completado`
- **Archivos:** `WaveSpawner.cs`, `LevelAudioManager.cs`, `CombatVFXManager.cs`
- **Descripción:** Calibración de oleadas del Nivel 2 para presentar un salto de dificultad justo, sistema de audio sintético procedural y sincronización con efectos visuales de explosión y partículas.

#### Issue #14: `[S2 - DONE] Menú Principal Cyberpunk, Cinemática Narrativa de 4 Actos y Diálogos N.E.X.A.`
- **Asignado a:** Cesar Enrique Garay García (`@cesargaray-dev`)
- **Etiquetas:** `sprint-2`, `ui-ux`, `narrativa-niveles`, `audio-vfx`, `completado`
- **Archivos:** `MainMenuAndIntroController.cs`, `CompanionDialogueManager.cs`, `00_MainMenu.unity`
- **Descripción:** Menú de inicio interactivo con zoom Ken Burns, partículas OnGUI y control de audio deslizable; cinemática de 4 actos con texto teletipo y sistema de diálogos holográficos de N.E.X.A. durante las oleadas.

#### Issue #15: `[S2 - DONE] Evolución a Action RPG Manual, Audio con Crossfade y Árbol de Mejoras Modal`
- **Asignado a:** Cesar Enrique Garay García (`@cesargaray-dev`)
- **Etiquetas:** `sprint-2`, `programacion`, `ui-ux`, `audio-vfx`, `completado`
- **Archivos:** `KaelAutoController.cs`, `PlayerStats.cs`, `AudioManager.cs`
- **Descripción:** Control manual (`A/D` movimiento, `S` guardia `-40%`, `J/Espacio` combo, `K/Q` Ultimate); AudioManager persistente con 4 pistas MP3 y crossfade suave; y modal de Árbol de Mejoras (`Tab/M`) con pantallas de Game Over y Victoria HD.

---

### 🟡 SPRINT 3: COMBATE TÁCTICO AVANZADO Y ARSENAL (EN PROGRESO)

#### Issue #16: `[S3 - TODO] Mecánica de Bloqueo Perfecto (Parry) con Ralentización Temporal y Contragolpe`
- **Asignado a:** Cesar Enrique Garay García (`@cesargaray-dev`)
- **Etiquetas:** `sprint-3`, `programacion`, `audio-vfx`, `por-hacer`
- **Archivos:** `KaelAutoController.cs`, `PlayerStats.cs`, `CombatVFXManager.cs`
- **Descripción:** Ventana de 0.20s al presionar `S`: anulación del 100% de daño, hit-stop cinematográfico (`Time.timeScale = 0.20f`), onda expansiva dorada y habilitación de Contragolpe Nexo Crítico (+150% de daño y aturdimiento de 1.5s).

#### Issue #17: `[S3 - TODO] Modo Sobremarcha Nexo por Racha de Combos (Overdrive)`
- **Asignado a:** Cesar Enrique Garay García (`@cesargaray-dev`)
- **Etiquetas:** `sprint-3`, `programacion`, `audio-vfx`, `ui-ux`, `por-hacer`
- **Archivos:** `KaelAutoController.cs`, `PlayerStats.cs`, `CombatVFXManager.cs`
- **Descripción:** Medidor de combo en el HUD con decaimiento tras 3.5s. Al llegar a x30 impactos sin recibir daño, Kael entra en Sobremarcha (+25% velocidad, +35% cadencia, estelas cromáticas *Ghost Trails* y ondas de corte de plasma por golpe).

#### Issue #18: `[S3 - TODO] Cinemática Puente Jugable de Transición entre Nivel 1 y Nivel 2`
- **Asignado a:** Cesar Enrique Garay García (`@cesargaray-dev`)
- **Etiquetas:** `sprint-3`, `narrativa-niveles`, `audio-vfx`, `programacion`, `por-hacer`
- **Archivos:** `LevelTransitionController.cs`, `CompanionDialogueManager.cs`, `PlayerStats.cs`
- **Descripción:** Secuencia interactiva tras vencer al Comandante Vorst del Nivel 1: colapso de compuertas, avistamiento de la Nave Núcleo, diálogo de emergencia de N.E.X.A. y carga asíncrona hacia `Lab_Zahir` conservando oro y mejoras.

#### Issue #19: `[S3 - TODO] Dron Centinela N.E.X.A. de Soporte Táctico y Asistencia de Fuego`
- **Asignado a:** Zahir Andrés Rodríguez Mora (`@ZahirAndres`)
- **Etiquetas:** `sprint-3`, `programacion`, `arte-animacion`, `por-hacer`
- **Archivos:** `NexaDroneCompanion.cs`, `NexaDrone.prefab`
- **Descripción:** Compañero flotante que sigue a Kael con física de amortiguación, dispara proyectiles de plasma guiados cada 2.5s y despliega un escudo baluarte que absorbe 3 impactos cuando la salud de Kael cae por debajo del 25%.

#### Issue #20: `[S3 - TODO] Iconos Pixel-Art HD para el Árbol de Habilidades y Matriz Nexo`
- **Asignado a:** Zahir Andrés Rodríguez Mora (`@ZahirAndres`)
- **Etiquetas:** `sprint-3`, `arte-animacion`, `ui-ux`, `por-hacer`
- **Archivos:** `Assets/_Project/Resources/StoryUI/Icons/`, `PlayerStats.cs`
- **Descripción:** Reemplazo de emojis por catálogo completo de iconos 64x64/128x128 para cada nodo de las 3 ramas del Árbol de Mejoras (Atributos Base, Técnicas de Combate y Matriz Evolutiva Nexo) con estética de circuitos cyberpunk.

#### Issue #21: `[S3 - TODO] Tienda Táctica de Arsenal con Modificadores Visuales del Guantelete`
- **Asignado a:** Zahir Andrés Rodríguez Mora (`@ZahirAndres`)
- **Etiquetas:** `sprint-3`, `ui-ux`, `programacion`, `arte-animacion`, `por-hacer`
- **Archivos:** `ArsenalShopUI.cs`, `KaelAutoController.cs`, `PlayerStats.cs`
- **Descripción:** Tienda "Arsenal de Vanguardia" con 4 variantes equipables del Guantelete Nexo (Prototipo Cian, Piro-Reactor Carmesí con quemadura, Plasma Ígneo Dorado con críticos y Vacío Vorst Púrpura con robo de vida), alterando los colores de los efectos de impacto.

---

### 🔵 SPRINT 4: MORALIDAD, CÓDICE Y DESENLACE (PLANIFICADO)

#### Issue #22: `[S4 - TODO] Sistema de Transformación Visual Dinámica de Kael (Compasión vs Disonancia)`
- **Asignado a:** Cesar Enrique Garay García (`@cesargaray-dev`)
- **Etiquetas:** `sprint-4`, `programacion`, `arte-animacion`, `narrativa-niveles`, `por-hacer`
- **Archivos:** `PlayerStats.cs`, `KaelAutoController.cs`, `CombatVFXManager.cs`
- **Descripción:** Mutación visual según dilemas morales: Ruta de la Humanidad (armadura blanca y dorada, aura cian pura, regeneración aumentada) vs Ruta de la Disonancia (brazo biomecánico púrpura con cuchillas óseas, ojos rojos, daño corrosivo y drenaje de vida por sobrecarga).

#### Issue #23: `[S4 - TODO] Nivel 3: 'Interior de la Nave Núcleo - El Laberinto de Bio-Cables'`
- **Asignado a:** Cesar Enrique Garay García (`@cesargaray-dev`)
- **Etiquetas:** `sprint-4`, `programacion`, `narrativa-niveles`, `audio-vfx`, `por-hacer`
- **Archivos:** `03_Mothership_Interior.unity`, `WaveSpawner.cs`, `CompanionDialogueManager.cs`, `PlayerStats.cs`
- **Descripción:** Escenario del vientre de la nave nodriza: plataformas biomecánicas suspendidas, peligros de conductos de plasma, oleadas con Mini-Jefe Inquisidor Psíquico (oleada 5), Jefe Carcelero Biotecnológico (oleada 10), diálogos de N.E.X.A. y el dilema moral de las cápsulas de estasis.

#### Issue #24: `[S4 - TODO] Sistema de 3 Finales Cinemáticos según el Nivel de Disonancia Nexo`
- **Asignado a:** Cesar Enrique Garay García (`@cesargaray-dev`)
- **Etiquetas:** `sprint-4`, `narrativa-niveles`, `ui-ux`, `audio-vfx`, `por-hacer`
- **Archivos:** `EndingsManager.cs`, `PlayerStats.cs`
- **Descripción:** 3 epílogos cinematográficos según las decisiones tomadas: Final 1 (Rescate Puro y reconstrucción terrestre), Final 2 (El Guardián Silencioso y exilio estelar) y Final 3 (El Nuevo Soberano y dominación de la armada Vorst).

#### Issue #25: `[S4 - TODO] Códice / Bestiario Holográfico Vorst Interactivo`
- **Asignado a:** Zahir Andrés Rodríguez Mora (`@ZahirAndres`)
- **Etiquetas:** `sprint-4`, `ui-ux`, `arte-animacion`, `narrativa-niveles`, `por-hacer`
- **Archivos:** `VorstCodexUI.cs`, `Assets/_Project/Resources/Codex/`
- **Descripción:** Enciclopedia táctica interactiva en el menú con fichas de datos de cada espécimen alienígena: modelo animado, puntos débiles, debilidades elementales, lore de la invasión y registro histórico de bajas conseguidas.

#### Issue #26: `[S4 - TODO] Nivel 4 Final: 'El Trono de la Disonancia' y Batalla de 3 Fases contra el Soberano Supremo`
- **Asignado a:** Zahir Andrés Rodríguez Mora (`@ZahirAndres`)
- **Etiquetas:** `sprint-4`, `narrativa-niveles`, `arte-animacion`, `por-hacer`
- **Archivos:** `04_Final_NexusThroneroom.unity`, `SovereignBossAI.cs`, `WaveSpawner.cs`, `EndingsManager.cs`
- **Descripción:** Escenario de máxima tensión en la cúpula superior con la cápsula de Lía interactiva, guardia pretoriana de élite, Custodio Pretoriano (oleada 5), confesión de N.E.X.A. sobre el origen del Guantelete (oleada 8) y batalla climática en 3 fases contra el Arconte Supremo / Soberano Vorst.

#### Issue #27: `[S4 - TODO] Cinemáticas Intermedias entre Niveles 2, 3 y 4 e Ilustraciones Finales de Desenlace`
- **Asignado a:** Zahir Andrés Rodríguez Mora (`@ZahirAndres`)
- **Etiquetas:** `sprint-4`, `arte-animacion`, `narrativa-niveles`, `ui-ux`, `por-hacer`
- **Archivos:** `Assets/_Project/Resources/StoryUI/Cinematics/`, `EndingsManager.cs`
- **Descripción:** Ilustraciones en estilo cómic/pixel-art de alta definición para las transiciones intermedias clave (brecha en la nave y descubrimiento de Lía) y las tres pantallas ilustradas de los desenlaces finales con soporte para subtítulos.

---

## 🛠️ 5. Vinculación con GitHub Projects

Para reflejar estos 20 issues en el tablero interactivo de **GitHub Projects** (`KAEL: Furia Terrestre - Roadmap`):

1. **Autorizar permisos de proyectos en GitHub CLI:**
   Ejecuta en tu terminal el siguiente comando (se abrirá una pestaña en tu navegador para confirmar el permiso `project`):
   ```bash
   gh auth refresh -s project
   ```

2. **Ejecutar el script automatizado de vinculación:**
   Una vez autorizado, ejecuta el script provisto en la raíz del repositorio:
   ```powershell
   ./sync_github_project.ps1
   ```
   El script creará automáticamente el tablero Kanban `KAEL: Furia Terrestre - Roadmap` y colocará:
   - Los Issues #1 al #8 en la columna **`Done`** (Sprints 1 y 2).
   - Los Issues #9 al #14 en la columna **`In Progress`** (Sprint 3).
   - Los Issues #15 al #20 en la columna **`Todo`** (Sprint 4).
