using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public Transform kaelTransform;
    public GameObject vorstWalkerPrefab;

    [Header("Configuración de Hordas (Sector 7)")]
    public int totalWaves = 10;
    public int baseEnemiesPerWave = 4; // Sube hasta 9 enemigos en las últimas oleadas
    public float spawnDistanceAhead = 10.5f;
    public float enemySpacing = 1.25f; // Hordas compactas para lucir el golpe en área y la Ultimate
    public float groundY = -2.85f;     // Alineado con Kael por encima de la barra de botones

    [Header("Estado Actual")]
    public int currentWave = 0;
    public int totalKillsInLevel = 0;
    private int enemiesAlive = 0;
    private bool spawningWave = false;

    void Start()
    {
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
            CombatVFXManager.Instance.TriggerBossWarning("⚠ ¡ALERTA DE TANQUE PESADO! ⚠", "BRUTO VORST DETECTADO EN LA OLEADA 5", 2.4f);
            yield return new WaitForSeconds(2.0f);
        }
        else if (isBossWave && CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.TriggerBossWarning("⚠ ¡ADVERTENCIA: LÍDER DE ZONA! ⚠", "COMANDANTE VORST BLOQUEA LA SALIDA DEL SECTOR 7", 2.8f);
            yield return new WaitForSeconds(2.2f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        // Cantidad de enemigos en la horda: empieza en 4 y llega hasta 9 en las oleadas finales
        int hordeCount = baseEnemiesPerWave + Mathf.FloorToInt(currentWave * 0.55f);
        if (isMiniBossWave || isBossWave) hordeCount += 1;

        enemiesAlive = hordeCount;

        for (int i = 0; i < hordeCount; i++)
        {
            if (kaelTransform == null || vorstWalkerPrefab == null) yield break;

            // Ligera variación en X para que se sienta como una horda orgánica
            float jitterX = Random.Range(-0.15f, 0.15f);
            float spawnX = kaelTransform.position.x + spawnDistanceAhead + (i * enemySpacing) + jitterX;
            Vector3 spawnPos = new Vector3(spawnX, groundY, 0f);

            GameObject enemyObj = Instantiate(vorstWalkerPrefab, spawnPos, Quaternion.identity);
            EnemyDummy enemy = enemyObj.GetComponent<EnemyDummy>();

            if (enemy != null)
            {
                if (isBossWave && i == hordeCount - 1)
                {
                    // JEFE DE ZONA (OLEADA 10): Comandante Vorst
                    enemy.isBoss = true;
                    enemy.enemyTitle = "Comandante Vorst — Líder Sector 7";
                    enemyObj.transform.localScale *= 1.75f;

                    SpriteRenderer sr = enemyObj.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(1f, 0.35f, 0.65f);

                    enemy.maxHP = 450f;
                    enemy.damage = 18f;
                    enemy.attackRange = 1.9f;
                    enemy.goldReward = 150;
                    enemy.expReward = 140;
                }
                else if (isMiniBossWave && i == hordeCount - 1)
                {
                    // MINI-JEFE (OLEADA 5): Bruto Vorst
                    enemy.isMiniBoss = true;
                    enemy.enemyTitle = "Bruto Vorst — Coloso Blindado";
                    enemyObj.transform.localScale *= 1.45f;

                    SpriteRenderer sr = enemyObj.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(1f, 0.65f, 0.25f);

                    enemy.maxHP = 240f;
                    enemy.damage = 14f;
                    enemy.moveSpeed = 1.6f;
                    enemy.attackRange = 1.7f;
                    enemy.goldReward = 65;
                    enemy.expReward = 75;
                }
                else
                {
                    // CAMINANTE VORST EN HORDA: Escala su vida (+24% por oleada) y su daño (+18% por oleada)
                    enemy.maxHP = Mathf.RoundToInt(30f * Mathf.Pow(1.24f, currentWave - 1));
                    enemy.damage = Mathf.RoundToInt(4f * Mathf.Pow(1.18f, currentWave - 1));
                    enemy.goldReward = Mathf.RoundToInt(6f + (currentWave * 2f));
                    enemy.expReward = 10 + (currentWave * 2);
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
                // Curar un 15% de la vida máxima al limpiar una oleada y mostrar cartel
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