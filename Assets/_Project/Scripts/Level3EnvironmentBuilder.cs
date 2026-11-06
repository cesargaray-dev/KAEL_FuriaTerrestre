using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Construye y anima las capas de paralaje biomecánico exclusivas del Nivel 3
/// ("Interior de la Nave Núcleo - El Laberinto de Bio-Cables"):
/// - Bio-cables orgánico-metálicos pulsantes con venas cian y magenta.
/// - Hilera de cápsulas de estasis con siluetas humanas suspendidas.
/// - Neblina y esporas de bio-plasma flotando en la cámara.
/// </summary>
public class Level3EnvironmentBuilder : MonoBehaviour
{
    public static Level3EnvironmentBuilder Instance { get; private set; }

    private Transform camTransform;
    private readonly List<SpriteRenderer> pulsingCables = new List<SpriteRenderer>();
    private readonly List<Transform> stasisPods = new List<Transform>();
    private readonly List<Transform> foregroundRibs = new List<Transform>();
    private float baseCamX;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        CheckAndSpawnForCurrentScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckAndSpawnForCurrentScene();
    }

    private static void CheckAndSpawnForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "03_Nivel3_Colmena" && Instance == null)
        {
            GameObject go = new GameObject("Level3_BiomechanicParallax");
            go.AddComponent<Level3EnvironmentBuilder>();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
            baseCamX = camTransform.position.x;
            Camera.main.backgroundColor = new Color(0.04f, 0.015f, 0.09f, 1f);
        }

        BuildBiomechanicLayers();
    }

    private void BuildBiomechanicLayers()
    {
        Sprite cableSprite = Level3MonsterAssetGenerator.GetBioCableSprite();
        Sprite podSprite = Level3MonsterAssetGenerator.GetStasisPodSprite();

        // 1. Hilera de Cápsulas de Estasis adicionales en capa media con paralaje suave
        for (int i = -2; i <= 14; i++)
        {
            GameObject podObj = new GameObject($"StasisPod_Parallax_{i}");
            podObj.transform.SetParent(transform);
            float worldX = i * 9.5f + 3.2f;
            float worldY = 0.85f + Mathf.Sin(i * 1.3f) * 0.35f;
            podObj.transform.position = new Vector3(worldX, worldY, 2f);
            podObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            SpriteRenderer sr = podObj.AddComponent<SpriteRenderer>();
            sr.sprite = podSprite;
            sr.sortingOrder = -6;
            sr.color = (i % 3 == 0)
                ? new Color(1f, 0.55f, 0.88f, 0.62f)
                : new Color(0.55f, 0.95f, 1f, 0.62f);

            stasisPods.Add(podObj.transform);
        }

        // 2. Bio-Cables Superiores e Inferiores Pulsantes (Paralaje cercano)
        for (int i = -3; i <= 18; i++)
        {
            // Cable superior colgante
            GameObject topCable = new GameObject($"BioCable_Top_{i}");
            topCable.transform.SetParent(transform);
            topCable.transform.position = new Vector3(i * 6.2f, 3.85f + Mathf.Sin(i * 0.9f) * 0.25f, 1f);
            topCable.transform.localScale = new Vector3(1.65f, 1.25f, 1f);

            SpriteRenderer srTop = topCable.AddComponent<SpriteRenderer>();
            srTop.sprite = cableSprite;
            srTop.sortingOrder = -4;
            srTop.color = new Color(0.45f, 0.95f, 1f, 0.78f);
            pulsingCables.Add(srTop);
            foregroundRibs.Add(topCable.transform);

            // Conducto de bio-plasma a media altura
            if (i % 2 == 0)
            {
                GameObject midCable = new GameObject($"BioCable_Mid_{i}");
                midCable.transform.SetParent(transform);
                midCable.transform.position = new Vector3(i * 6.2f + 2.4f, 2.35f, 1.5f);
                midCable.transform.localScale = new Vector3(1.45f, 0.95f, 1f);

                SpriteRenderer srMid = midCable.AddComponent<SpriteRenderer>();
                srMid.sprite = cableSprite;
                srMid.sortingOrder = -5;
                srMid.color = new Color(1f, 0.35f, 0.78f, 0.65f);
                pulsingCables.Add(srMid);
            }
        }
    }

    void Update()
    {
        if (camTransform == null && Camera.main != null)
            camTransform = Camera.main.transform;

        float t = Time.time;

        // Pulsación orgánica de las venas de bio-cables (cian <-> magenta bioluminiscente)
        for (int i = 0; i < pulsingCables.Count; i++)
        {
            SpriteRenderer sr = pulsingCables[i];
            if (sr == null) continue;
            float pulse = 0.55f + 0.45f * Mathf.Sin(t * 2.8f + i * 0.65f);
            Color baseCol = (i % 2 == 0)
                ? Color.Lerp(new Color(0.20f, 0.75f, 0.95f, 0.68f), new Color(0.55f, 1f, 1f, 0.92f), pulse)
                : Color.Lerp(new Color(0.75f, 0.18f, 0.62f, 0.55f), new Color(1f, 0.42f, 0.85f, 0.85f), pulse);
            sr.color = baseCol;
        }

        // Flotación suave de las cápsulas de estasis con humanos prisioneros
        for (int i = 0; i < stasisPods.Count; i++)
        {
            Transform pod = stasisPods[i];
            if (pod == null) continue;
            Vector3 p = pod.position;
            p.y = 0.85f + Mathf.Sin(t * 1.4f + i * 0.9f) * 0.14f;
            pod.position = p;
        }

        // Esporas bioluminiscentes ambientales en la Colmena Vorst
        if (CombatVFXManager.Instance != null && camTransform != null && Random.value < 0.14f)
        {
            Vector3 sporePos = new Vector3(
                camTransform.position.x + Random.Range(-9.5f, 9.5f),
                Random.Range(-3.2f, 3.6f),
                0f
            );
            Color sporeCol = (Random.value > 0.45f)
                ? new Color(0.15f, 0.95f, 1f, 0.75f)
                : new Color(0.95f, 0.25f, 0.75f, 0.75f);
            CombatVFXManager.Instance.SpawnFloatingRuneOrSpore(sporePos, sporeCol);
        }
    }
}
