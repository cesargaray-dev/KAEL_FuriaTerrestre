using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public Transform kaelTransform;
    public GameObject vorstWalkerPrefab;

    [Header("Configuración de Oleadas (Sector 7)")]
    public int totalWaves = 10;
    public int baseEnemiesPerWave = 3;
    public float spawnDistanceAhead = 11f; // Distancia a la derecha de Kael (fuera de pantalla)
    public float enemySpacing = 1.6f;      // Separación entre cada monstruo de la misma oleada
    public float groundY = -3.4f;          // Altura Y del asfalto donde pisan los monstruos

    [Header("Estado Actual")]
    public int currentWave = 0;
    private int enemiesAlive = 0;
    private bool spawningWave = false;

    void Start()
    {
        if (kaelTransform == null)
        {
            KaelAutoController kael = FindFirstObjectByType<KaelAutoController>();
            if (kael != null) kaelTransform = kael.transform;
        }

        StartCoroutine(SpawnNextWaveRoutine());
    }

    void Update()
    {
        // Si ya murieron todos los enemigos de la oleada actual, lanzar la siguiente
        if (!spawningWave && enemiesAlive <= 0 && currentWave < totalWaves)
        {
            StartCoroutine(SpawnNextWaveRoutine());
        }
    }

    IEnumerator SpawnNextWaveRoutine()
    {
        spawningWave = true;
        currentWave++;
        Debug.Log($"--- ¡Iniciando Oleada {currentWave} de {totalWaves}! ---");

        yield return new WaitForSeconds(1.2f); // Pequeño respiro para que Kael corra entre oleadas

        int count = baseEnemiesPerWave + (currentWave / 2); // Cada 2 oleadas sale 1 monstruo extra
        enemiesAlive = count;

        for (int i = 0; i < count; i++)
        {
            if (kaelTransform == null || vorstWalkerPrefab == null) yield break;

            float spawnX = kaelTransform.position.x + spawnDistanceAhead + (i * enemySpacing);
            Vector3 spawnPos = new Vector3(spawnX, groundY, 0f);

            GameObject enemyObj = Instantiate(vorstWalkerPrefab, spawnPos, Quaternion.identity);

            // Escalar ligeramente la vida del monstruo según la oleada
            EnemyDummy enemy = enemyObj.GetComponent<EnemyDummy>();
            if (enemy != null)
            {
                enemy.maxHP = 30f * (1f + (currentWave - 1) * 0.15f);
                enemy.OnEnemyDied += HandleEnemyDied;
            }
        }

        spawningWave = false;
    }

    void HandleEnemyDied()
    {
        enemiesAlive--;
        if (enemiesAlive <= 0 && currentWave >= totalWaves)
        {
            Debug.Log("¡SECTOR 7 COMPLETADO! Todos los enemigos fueron derrotados.");
        }
    }
}