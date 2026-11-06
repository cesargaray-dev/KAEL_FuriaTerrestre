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
            EnemyDummy.PreloadLevel3Frames();
        }
        else if (sceneName == "Lab_Zahir" || (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 2))
        {
            isLevel2BaseVorst = true;
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

        if (groundY < -3.0f) groundY = -2.85f;

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
                ? "MINI-JEFE DE OLEADA 5: INQUISIDOR PSIÓNICO VORST"
                : (isLevel2BaseVorst ? "PRIMER JEFE: BRUTO VORST EN LA OLEADA 5" : "BRUTO VORST DETECTADO EN LA OLEADA 5");
            CombatVFXManager.Instance.TriggerBossWarning(warnTitle, warnSub, 2.4f);
            yield return new WaitForSeconds(2.0f);
        }
        else if (isBossWave && CombatVFXManager.Instance != null)
        {
            if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();
            string warnTitle = isLevel3Hive
                ? "⚠ AMENAZA EXTREMA: CARCELERO BIOTECNOLÓGICO VORTEX-OMEGA ⚠"
                : (isLevel2BaseVorst ? "⚠ ¡SOBERANO DE LA BASE VORST! ⚠" : "⚠ ¡ADVERTENCIA: LÍDER DE ZONA! ⚠");
            string warnSub = isLevel3Hive
                ? "JEFE DE LA NAVE NÚCLEO — GUARDIÁN DE LAS CÁPSULAS DE ESTASIS"
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
                        // BLOQUE 5: JEFE FINAL DEL NIVEL 3 (VORTEX-OMEGA, escala 2.4x)
                        enemy.enemyTitle = "Carcelero Biotecnológico VORTEX-OMEGA";
                        enemy.SetupLevel3Visuals("VortexOmega");
                        enemyObj.transform.localScale = Vector3.one * 2.40f;
                        if (sr != null) sr.color = Color.white;
                        enemy.attackRange = 2.55f;
                        enemy.attackRate = 1.05f;
                        enemy.moveSpeed = 1.60f;
                        enemy.maxHP = 3200f;
                        enemy.damage = 26f;
                        enemy.goldReward = 380;
                        enemy.expReward = 340;
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
                        // BLOQUE 5: MINI-JEFE OLEADA 5 DEL NIVEL 3 (Inquisidor Psíquico Vorst, escala 1.9x)
                        enemy.enemyTitle = "Inquisidor Psíquico Vorst";
                        enemy.SetupLevel3Visuals("HiveInquisitor");
                        enemyObj.transform.localScale = Vector3.one * 1.90f;
                        if (sr != null) sr.color = Color.white;
                        enemy.maxHP = 1350f;
                        enemy.damage = 18f;
                        enemy.moveSpeed = 1.65f;
                        enemy.attackRange = 2.15f;
                        enemy.goldReward = 175;
                        enemy.expReward = 160;
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
                        // BLOQUE 5: 3 MONSTRUOS BIOMECÁNICOS EXCLUSIVOS DEL NIVEL 3 (Cero reutilización básica)
                        int archetype = (currentWave + i) % 3;
                        if (sr != null) sr.color = Color.white;

                        if (archetype == 0)
                        {
                            // 1. Acechador Quimera Vorst (BioStalker): +35% velocidad, salto y tajo doble
                            enemy.enemyTitle = "Acechador Quimera Vorst";
                            enemy.SetupLevel3Visuals("BioStalker");
                            enemyObj.transform.localScale = Vector3.one * 1.05f;
                            enemy.moveSpeed = 2.70f;
                            enemy.attackRange = 1.48f;
                            enemy.attackRate = 1.20f;
                            enemy.maxHP = Mathf.RoundToInt(72f * Mathf.Pow(1.16f, currentWave - 1));
                            enemy.damage = Mathf.RoundToInt(7f * Mathf.Pow(1.13f, currentWave - 1));
                        }
                        else if (archetype == 1)
                        {
                            // 2. Flagelador de Médula (BioLasher): Levitante sinusoidal, látigo a media distancia
                            enemy.enemyTitle = "Flagelador de Médula";
                            enemy.SetupLevel3Visuals("BioLasher");
                            enemyObj.transform.localScale = Vector3.one * 1.12f;
                            enemy.moveSpeed = 1.95f;
                            enemy.attackRange = 2.25f;
                            enemy.attackRate = 1.0f;
                            enemy.maxHP = Mathf.RoundToInt(86f * Mathf.Pow(1.16f, currentWave - 1));
                            enemy.damage = Mathf.RoundToInt(8f * Mathf.Pow(1.14f, currentWave - 1));
                        }
                        else
                        {
                            // 3. Coloso de Crisálida (BioHulk): Pesado, placas óseas moradas, aplastamiento telegrafiado
                            enemy.enemyTitle = "Coloso de Crisálida";
                            enemy.SetupLevel3Visuals("BioHulk");
                            enemyObj.transform.localScale = Vector3.one * 1.28f;
                            enemy.moveSpeed = 1.42f;
                            enemy.attackRange = 1.72f;
                            enemy.attackRate = 0.82f;
                            enemy.maxHP = Mathf.RoundToInt(135f * Mathf.Pow(1.17f, currentWave - 1));
                            enemy.damage = Mathf.RoundToInt(11f * Mathf.Pow(1.14f, currentWave - 1));
                        }

                        enemy.goldReward = Mathf.RoundToInt(12f + (currentWave * 3.5f));
                        enemy.expReward = 16 + (currentWave * 4);
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
