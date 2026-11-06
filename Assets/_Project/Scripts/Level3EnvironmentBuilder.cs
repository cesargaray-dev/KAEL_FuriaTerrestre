using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona la atmósfera bioluminiscente y las esporas flotantes del Nivel 3
/// ("Interior de la Nave Núcleo - El Laberinto de Bio-Cables"), dejando lucir
/// con total limpieza el arte Pixel-Art HD de BG_Hive_Level3.png.
/// </summary>
public class Level3EnvironmentBuilder : MonoBehaviour
{
    public static Level3EnvironmentBuilder Instance { get; private set; }

    private Transform camTransform;

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
            GameObject go = new GameObject("Level3_BiomechanicAtmosphere");
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
            Camera.main.backgroundColor = new Color(0.04f, 0.015f, 0.09f, 1f);
        }
    }

    void Update()
    {
        if (camTransform == null && Camera.main != null)
            camTransform = Camera.main.transform;

        // Esporas bioluminiscentes sutiles flotando en la cámara de la Colmena Vorst
        if (CombatVFXManager.Instance != null && camTransform != null && Random.value < 0.09f)
        {
            Vector3 sporePos = new Vector3(
                camTransform.position.x + Random.Range(-9.5f, 9.5f),
                Random.Range(-2.9f, 3.4f),
                0f
            );
            Color sporeCol = (Random.value > 0.45f)
                ? new Color(0.15f, 0.95f, 0.85f, 0.65f)
                : new Color(0.95f, 0.25f, 0.75f, 0.65f);
            CombatVFXManager.Instance.SpawnFloatingRuneOrSpore(sporePos, sporeCol);
        }
    }
}
