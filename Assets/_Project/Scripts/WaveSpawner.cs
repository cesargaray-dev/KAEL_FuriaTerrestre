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

    [Header("Configuración Nivel 2 (Base Vorst - Lab_Zahir)")]
    public bool isLevel2BaseVorst = false;
    public float difficultyMultiplier = 1.0f;

    [Header("Estado Actual")]
    public int currentWave = 0;
    public int totalKillsInLevel = 0;
    private int enemiesAlive = 0;
    private bool spawningWave = false;

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "Lab_Zahir")
        {
            isLevel2BaseVorst = true;
            // Pre-cargar todos los cuadros de animación del Nivel 2 en memoria para eliminar cualquier tirón
            EnemyDummy.PreloadLevel2Frames();
        }

        // Asegurar que exista el gestor de Audio en la escena
        if (FindFirstObjectByType<LevelAudioManager>() == null)
        {
            gameObject.AddComponent<LevelAudioManager>();
        }

        if (kaelTransform == null)
        {
            KaelAutoController kael = FindFirstObjectByType<KaelAutoController>();
            if (kael != null) kaelTransform = kael.transform;
        }

        // Asegurar que la altura de los enemigos esté por encima del HUD inferior
        if (groundY < -3.0f) groundY = -2.85f;

        StartCoroutine(SpawnNextWaveRoutine());
    }

    void Update()
    {
        if (PlayerStats.Instance != null && (PlayerStats.Instance.isGameOver || PlayerStats.Instance.isLevelCompleted || PlayerStats.Instance.isChoosingDilemma))
            return;

        if (!spawningWave && enemiesAlive <= 0 && currentWave < totalWaves)
        {
            StartCoroutine(SpawnNextWaveRoutine());
        }
    }

    // Reinicia desde la Oleada 1 tras un Game Over conservando el Oro y las Mejoras de Kael
    public void RestartWavesKeepingProgress()
    {
        StopAllCoroutines();

        EnemyDummy[] remaining = FindObjectsByType<EnemyDummy>(FindObjectsSortMode.None);
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

        StartCoroutine(SpawnNextWaveRoutine());
    }

    IEnumerator SpawnNextWaveRoutine()
    {
        spawningWave = true;
        currentWave++;

        bool isMiniBossWave = (currentWave == 5);
        bool isBossWave = (currentWave == totalWaves);

        if (isMiniBossWave && CombatVFXManager.Instance != null)
        {
            if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();
            string warnTitle = isLevel2BaseVorst ? "⚠ ¡COLOSO DE NÚCLEO ROJO! ⚠" : "⚠ ¡ALERTA DE TANQUE PESADO! ⚠";
            string warnSub = isLevel2BaseVorst ? "PRIMER JEFE: BRUTO VORST EN LA OLEADA 5" : "BRUTO VORST DETECTADO EN LA OLEADA 5";
            CombatVFXManager.Instance.TriggerBossWarning(warnTitle, warnSub, 2.4f);
            yield return new WaitForSeconds(2.0f);
        }
        else if (isBossWave && CombatVFXManager.Instance != null)
        {
            if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();
            string warnTitle = isLevel2BaseVorst ? "⚠ ¡SOBERANO DE LA BASE VORST! ⚠" : "⚠ ¡ADVERTENCIA: LÍDER DE ZONA! ⚠";
            string warnSub = isLevel2BaseVorst ? "JEFE FINAL: ARCONTE PSIÓNICO VORST" : "COMANDANTE VORST BLOQUEA LA SALIDA DEL SECTOR 7";
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
                    // JEFE FINAL (OLEADA 10): 3er Personaje (Arconte Vorst en Nivel 2)
                    enemy.isBoss = true;
                    enemy.enemyTitle = isLevel2BaseVorst ? "Arconte Vorst — Soberano Psiónico" : "Comandante Vorst — Líder Sector 7";

                    if (isLevel2BaseVorst)
                    {
                        enemy.SetupCustomVisuals("Archon");
                        enemyObj.transform.localScale = Vector3.one * 1.35f;
                        if (sr != null) sr.color = Color.white;
                        enemy.attackRange = 2.4f;
                        enemy.attackRate = 1.0f;
                        // Vida elevada a 2200 HP para que el combate dure de 20 a 35 segundos y se vean todas sus fases, ataques psiónicos y muerte
                        enemy.maxHP = 2200f;
                        enemy.damage = 22f;
                        enemy.goldReward = 250;
                        enemy.expReward = 220;
                    }
                    else
                    {
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
                    // PRIMER JEFE / MINI-JEFE (OLEADA 5): 2do Personaje (Bruto Vorst en Nivel 2)
                    enemy.isMiniBoss = true;
                    enemy.enemyTitle = isLevel2BaseVorst ? "Bruto Vorst — Coloso de Núcleo Rojo" : "Bruto Vorst — Coloso Blindado";

                    if (isLevel2BaseVorst)
                    {
                        enemy.SetupCustomVisuals("Brute");
                        enemyObj.transform.localScale = Vector3.one * 1.30f;
                        if (sr != null) sr.color = Color.white;
                        // Bruto con 950 HP para resistir varios combos y mostrar su embestida pesada telegrafiada
                        enemy.maxHP = 950f;
                        enemy.damage = 15f;
                        enemy.moveSpeed = 1.55f;
                        enemy.attackRange = 1.65f;
                        enemy.goldReward = 120;
                        enemy.expReward = 110;
                    }
                    else
                    {
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
                    // HORDA SENCILLA (OLEADAS 1 A 10): 1er Personaje (Acechador Vorst bípedo con antenas y garras en Nivel 2)
                    if (isLevel2BaseVorst)
                    {
                        enemy.enemyTitle = "Acechador Vorst";
                        enemy.SetupCustomVisuals("Stalker");
                        enemyObj.transform.localScale = Vector3.one * 1.0f;
                        if (sr != null) sr.color = Color.white;
                        enemy.attackRange = 1.45f;
                        enemy.moveSpeed = 2.0f;

                        // Vida nivelada (empieza en 58 HP) para que aguante 4-5 golpes de Kael y se aprecien bien sus animaciones de caminar, atacar y morir
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
                if (PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.TriggerEndOfZoneDilemma();
                }
            }
            else if (PlayerStats.Instance != null && kaelTransform != null)
            {
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
