using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public Transform kaelTransform;
    public GameObject vorstWalkerPrefab;

    [Header("Configuración de Hordas")]
    public int totalWaves = 10;
    public int baseEnemiesPerWave = 4; // Sube hasta 9 enemigos en las últimas oleadas
    public float spawnDistanceAhead = 10.5f;
    public float enemySpacing = 1.35f;
    public float groundY = -2.85f;     // Alineado con Kael por encima de la barra de botones

    [Header("Configuración Nivel 2 y Nivel 3")]
    public bool isLevel2BaseVorst = false;
    public bool isLevel3Hive = false;
    public float difficultyMultiplier = 1.0f;

    [Header("Estado Actual")]
    public int currentWave = 0;
    public int totalKillsInLevel = 0;
    private int enemiesAlive = 0;
    private bool spawningWave = false;

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "03_Nivel3_Colmena" || (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 3))
        {
            isLevel3Hive = true;
            isLevel2BaseVorst = false;
            groundY = -2.95f;
            EnemyDummy.PreloadLevel3Frames();
        }
        else if (sceneName == "Lab_Zahir" || (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 2))
        {
            isLevel2BaseVorst = true;
            groundY = -2.95f;
            EnemyDummy.PreloadLevel2Frames();
        }

        // Asegurar que exista el gestor de Audio en la escena
        if (FindAnyObjectByType<LevelAudioManager>() == null)
        {
            gameObject.AddComponent<LevelAudioManager>();
        }

        if (kaelTransform == null)
        {
            KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
            if (kael != null) kaelTransform = kael.transform;
        }

        if (groundY < -3.05f) groundY = -2.95f;

        StartCoroutine(SpawnNextWaveRoutine());
    }

    void Update()
    {
        if (PlayerStats.Instance != null &&
            (PlayerStats.Instance.isPaused ||
             PlayerStats.Instance.isUpgradeTreeOpen ||
             PlayerStats.Instance.isChoosingSkill ||
             PlayerStats.Instance.isGameOver ||
             PlayerStats.Instance.isLevelCompleted ||
             PlayerStats.Instance.isChoosingDilemma))
            return;

        if (BridgeLevel1To2Controller.Instance != null && BridgeLevel1To2Controller.Instance.isBridgeActive)
            return;

        if (!spawningWave && enemiesAlive <= 0 && currentWave < totalWaves)
        {
            StartCoroutine(SpawnNextWaveRoutine());
        }
    }

    public void RestartWavesKeepingProgress()
    {
        StopAllCoroutines();

        EnemyDummy[] remaining = FindObjectsByType<EnemyDummy>(FindObjectsInactive.Exclude);
        foreach (EnemyDummy e in remaining)
        {
            Destroy(e.gameObject);
        }

        currentWave = 0;
        enemiesAlive = 0;
        spawningWave = false;

        if (kaelTransform != null)
        {
            KaelAutoController kael = kaelTransform.GetComponent<KaelAutoController>();
            if (kael != null) kael.ResetKaelPosition();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCombatMusic();
        }

        StartCoroutine(SpawnNextWaveRoutine());
    }

    /// <summary>
    /// Devuelve el arquetipo (0: BioStalker, 1: BioLasher, 2: BioHulk, 3: HiveInquisitor)
    /// con composiciones temáticas y no repetitivas para cada oleada del Nivel 3.
    /// </summary>
    public static int GetLevel3EnemyArchetype(int wave, int enemyIndex, int totalInWave)
    {
        // 0: BioStalker (Pantera ágil melee)
        // 1: BioLasher (Alienígena levitador a media distancia)
        // 2: BioHulk (Coloso blindado tanque)
        // 3: HiveInquisitor (Hechicero psiónico a distancia)

        int[][] waveCompositions = new int[][]
        {
            // Oleada 1: Incursión rápida de reconocimiento (Panteras con un tentacular de apoyo)
            new int[] { 0, 0, 0, 1 },
            // Oleada 2: Tentáculos en la penumbra (Predominio de tentaculares con panteras flanqueando)
            new int[] { 0, 1, 1, 0, 1 },
            // Oleada 3: Primera Muralla Biomecánica (Aparición del primer Coloso tanque)
            new int[] { 2, 0, 0, 1, 0 },
            // Oleada 4: Ecos Psiónicos (Lanzadores a distancia Inquisidor + vanguardia de panteras)
            new int[] { 0, 3, 1, 0, 3 },
            // Oleada 5: Escolta del Inquisidor Supremo (Coloso protector + acechadores)
            new int[] { 2, 0, 1, 0, 2 },
            // Oleada 6: Asedio Blindado (Doble Coloso al frente con soporte de látigos)
            new int[] { 2, 2, 0, 1, 0, 1 },
            // Oleada 7: Bombardeo Psiónico Cruzado (Múltiples hechiceros y tentáculos)
            new int[] { 3, 1, 3, 0, 3, 1 },
            // Oleada 8: Enjambre Quimérico Equilibrado (Composición híbrida variada)
            new int[] { 0, 2, 3, 1, 0, 2, 3 },
            // Oleada 9: Guardia Pretoriana de la Colmena (Tanques colosales + hechiceros psiónicos pesados)
            new int[] { 2, 3, 2, 1, 3, 0, 2 },
            // Oleada 10: Escolta del Mecha Vortex-Omega
            new int[] { 0, 3, 2, 1, 0 }
        };

        int waveIdx = Mathf.Clamp(wave - 1, 0, waveCompositions.Length - 1);
        int[] comp = waveCompositions[waveIdx];
        if (comp != null && comp.Length > 0)
        {
            return comp[enemyIndex % comp.Length];
        }
        return enemyIndex % 4;
    }

    /// <summary>
    /// BLOQUE 5 (Issue #23): Cuando el Jefe Final VORTEX-OMEGA entra en Fase de Furia (<45% HP),
    /// invoca 2 Acechadores Quimera (BioStalkers) de escolta.
    /// </summary>
    public void SpawnVortexOmegaEscorts(Vector3 bossPos)
    {
        if (vorstWalkerPrefab == null) return;

        for (int i = 0; i < 2; i++)
        {
            Vector3 escortPos = new Vector3(bossPos.x + 1.8f + i * 1.4f, groundY, 0f);
            GameObject obj = Instantiate(vorstWalkerPrefab, escortPos, Quaternion.identity);
            EnemyDummy enemy = obj.GetComponent<EnemyDummy>();
            if (enemy != null)
            {
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Color.white;
                enemy.enemyTitle = "Acechador Quimera [Escolta Omega]";
                enemy.SetupLevel3Visuals("BioStalker");
                obj.transform.localScale = Vector3.one * 1.05f;
                enemy.moveSpeed = 2.85f;
                enemy.attackRange = 1.5f;
                enemy.maxHP = 125f;
                enemy.currentHP = 125f;
                enemy.damage = 11f;
                enemy.goldReward = 22;
                enemy.expReward = 28;

                enemiesAlive++;
                enemy.OnEnemyDied += HandleEnemyDied;
            }
        }
    }

    IEnumerator SpawnNextWaveRoutine()
    {
        spawningWave = true;
        currentWave++;

        bool isMiniBossWave = (currentWave == 5);
        bool isBossWave = (currentWave == totalWaves);

        if (isMiniBossWave || isBossWave)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBossMusic();
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCombatMusic();
        }

        if (isMiniBossWave && CombatVFXManager.Instance != null)
        {
            if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();
            string warnTitle = isLevel3Hive
                ? "⚠ ¡ANOMALÍA PSIÓNICA EN LA COLMENA! ⚠"
                : (isLevel2BaseVorst ? "⚠ ¡COLOSO DE NÚCLEO ROJO! ⚠" : "⚠ ¡ALERTA DE TANQUE PESADO! ⚠");
            string warnSub = isLevel3Hive
                ? "MINI-JEFE DE OLEADA 5: INQUISIDOR PSIÓNICO SUPREMO"
                : (isLevel2BaseVorst ? "PRIMER JEFE: BRUTO VORST EN LA OLEADA 5" : "BRUTO VORST DETECTADO EN LA OLEADA 5");
            CombatVFXManager.Instance.TriggerBossWarning(warnTitle, warnSub, 2.4f);
            yield return new WaitForSeconds(2.0f);
        }
        else if (isBossWave && CombatVFXManager.Instance != null)
        {
            if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();
            string warnTitle = isLevel3Hive
                ? "⚠ AMENAZA EXTREMA: MECHA BERSERKER VORTEX-OMEGA ⚠"
                : (isLevel2BaseVorst ? "⚠ ¡SOBERANO DE LA BASE VORST! ⚠" : "⚠ ¡ADVERTENCIA: LÍDER DE ZONA! ⚠");
            string warnSub = isLevel3Hive
                ? "JEFE FINAL DE LA COLMENA — GUARDIÁN DE LAS CÁPSULAS DE ESTASIS"
                : (isLevel2BaseVorst ? "JEFE FINAL: ARCONTE PSIÓNICO VORST" : "COMANDANTE VORST BLOQUEA LA SALIDA DEL SECTOR 7");
            CombatVFXManager.Instance.TriggerBossWarning(warnTitle, warnSub, 2.8f);
            yield return new WaitForSeconds(2.2f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        int hordeCount = baseEnemiesPerWave + Mathf.FloorToInt(currentWave * 0.50f);
        if (isMiniBossWave || isBossWave) hordeCount += 1;

        enemiesAlive = hordeCount;

        for (int i = 0; i < hordeCount; i++)
        {
            if (kaelTransform == null || vorstWalkerPrefab == null) yield break;

            float jitterX = Random.Range(-0.12f, 0.12f);
            float spawnX = kaelTransform.position.x + spawnDistanceAhead + (i * enemySpacing) + jitterX;
            Vector3 spawnPos = new Vector3(spawnX, groundY, 0f);

            GameObject enemyObj = Instantiate(vorstWalkerPrefab, spawnPos, Quaternion.identity);
            EnemyDummy enemy = enemyObj.GetComponent<EnemyDummy>();

            if (enemy != null)
            {
                SpriteRenderer sr = enemyObj.GetComponent<SpriteRenderer>();

                if (isBossWave && i == hordeCount - 1)
                {
                    enemy.isBoss = true;
                    if (isLevel3Hive)
                    {
                        // BLOQUE 5: JEFE FINAL DEL NIVEL 3 (VORTEX-OMEGA)
                        if (sr != null) sr.color = Color.white;
                        enemy.SetupLevel3Monster(0, currentWave, boss: true, miniBoss: false);
                    }
                    else if (isLevel2BaseVorst)
                    {
                        enemy.enemyTitle = "Arconte Vorst — Soberano Psiónico";
                        enemy.SetupCustomVisuals("Archon");
                        enemyObj.transform.localScale = Vector3.one * 1.35f;
                        if (sr != null) sr.color = Color.white;
                        enemy.attackRange = 2.4f;
                        enemy.attackRate = 1.0f;
                        enemy.maxHP = 2200f;
                        enemy.damage = 22f;
                        enemy.goldReward = 250;
                        enemy.expReward = 220;
                    }
                    else
                    {
                        enemy.enemyTitle = "Comandante Vorst — Líder Sector 7";
                        enemyObj.transform.localScale *= 1.75f;
                        if (sr != null) sr.color = new Color(1f, 0.35f, 0.65f);
                        enemy.attackRange = 1.9f;
                        enemy.maxHP = 450f;
                        enemy.damage = 18f;
                        enemy.goldReward = 150;
                        enemy.expReward = 140;
                    }
                }
                else if (isMiniBossWave && i == hordeCount - 1)
                {
                    enemy.isMiniBoss = true;
                    if (isLevel3Hive)
                    {
                        // BLOQUE 5: MINI-JEFE OLEADA 5 DEL NIVEL 3 (Inquisidor Psiónico Supremo)
                        if (sr != null) sr.color = Color.white;
                        enemy.SetupLevel3Monster(3, currentWave, boss: false, miniBoss: true);
                    }
                    else if (isLevel2BaseVorst)
                    {
                        enemy.enemyTitle = "Bruto Vorst — Coloso de Núcleo Rojo";
                        enemy.SetupCustomVisuals("Brute");
                        enemyObj.transform.localScale = Vector3.one * 1.30f;
                        if (sr != null) sr.color = Color.white;
                        enemy.maxHP = 950f;
                        enemy.damage = 15f;
                        enemy.moveSpeed = 1.55f;
                        enemy.attackRange = 1.65f;
                        enemy.goldReward = 120;
                        enemy.expReward = 110;
                    }
                    else
                    {
                        enemy.enemyTitle = "Bruto Vorst — Coloso Blindado";
                        enemyObj.transform.localScale *= 1.45f;
                        if (sr != null) sr.color = new Color(1f, 0.65f, 0.25f);
                        enemy.maxHP = 240f;
                        enemy.damage = 14f;
                        enemy.moveSpeed = 1.6f;
                        enemy.attackRange = 1.7f;
                        enemy.goldReward = 65;
                        enemy.expReward = 75;
                    }
                }
                else
                {
                    if (isLevel3Hive)
                    {
                        // BLOQUE 5: COMPOSICIONES TEMÁTICAS Y VARIADAS EN CADA OLEADA DEL NIVEL 3
                        int archetype = GetLevel3EnemyArchetype(currentWave, i, hordeCount);
                        if (sr != null) sr.color = Color.white;
                        enemy.SetupLevel3Monster(archetype, currentWave, boss: false, miniBoss: false);
                    }
                    else if (isLevel2BaseVorst)
                    {
                        enemy.enemyTitle = "Acechador Vorst";
                        enemy.SetupCustomVisuals("Stalker");
                        enemyObj.transform.localScale = Vector3.one * 1.0f;
                        if (sr != null) sr.color = Color.white;
                        enemy.attackRange = 1.45f;
                        enemy.moveSpeed = 2.0f;
                        enemy.maxHP = Mathf.RoundToInt(58f * Mathf.Pow(1.18f, currentWave - 1));
                        enemy.damage = Mathf.RoundToInt(5f * Mathf.Pow(1.14f, currentWave - 1));
                        enemy.goldReward = Mathf.RoundToInt(8f + (currentWave * 2.5f));
                        enemy.expReward = 12 + (currentWave * 3);
                    }
                    else
                    {
                        enemy.maxHP = Mathf.RoundToInt(30f * Mathf.Pow(1.24f, currentWave - 1));
                        enemy.damage = Mathf.RoundToInt(4f * Mathf.Pow(1.18f, currentWave - 1));
                        enemy.goldReward = Mathf.RoundToInt(6f + (currentWave * 2f));
                        enemy.expReward = 10 + (currentWave * 2);
                    }
                }

                enemy.OnEnemyDied += HandleEnemyDied;
            }
        }

        spawningWave = false;
    }

    void HandleEnemyDied()
    {
        enemiesAlive--;
        totalKillsInLevel++;

        if (enemiesAlive <= 0)
        {
            if (currentWave >= totalWaves)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayVictoryDilemmaMusic();
                }

                if (PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.TriggerEndOfZoneDilemma();
                }
            }
            else if (PlayerStats.Instance != null && kaelTransform != null)
            {
                if (currentWave == 5 && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayCombatMusic();
                }

                float heal = Mathf.RoundToInt(PlayerStats.Instance.maxHP * 0.15f);
                PlayerStats.Instance.currentHP = Mathf.Min(PlayerStats.Instance.maxHP, PlayerStats.Instance.currentHP + heal);

                if (CombatVFXManager.Instance != null)
                {
                    CombatVFXManager.Instance.SpawnFloatingText(
                        kaelTransform.position + Vector3.up * 1.5f,
                        $"¡OLEADA {currentWave} SUPERADA! (+{heal} HP)",
                        new Color(0.2f, 0.95f, 0.5f),
                        20,
                        1.2f
                    );
                }
            }
        }
    }
}
