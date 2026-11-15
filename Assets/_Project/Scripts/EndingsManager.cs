using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Gestor central de desenlaces para la Batalla Final en 04_Final_NexusThroneroom.
/// Detiene el combate activo tras derrotar al Soberano Supremo, libera la cápsula
/// de Lía, persiste el progreso moral y proyecta el final ético correspondiente
/// (Armonía Humana / Sacrificio Nexo / Tirano Vorst) en EndingCinematicController.
/// </summary>
public class EndingsManager : MonoBehaviour
{
    public static EndingsManager Instance { get; private set; }

    public bool hasTriggeredEnding { get; private set; } = false;
    private bool transitionBannerActive = false;
    private string transitionTitle = "";
    private string transitionSubtitle = "";
    private Color transitionColor = new Color(0.25f, 0.95f, 1f);

    public static EndingsManager EnsureExists()
    {
        if (Instance != null) return Instance;
        EndingsManager existing = FindAnyObjectByType<EndingsManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("EndingsManager");
        Instance = go.AddComponent<EndingsManager>();
        return Instance;
    }

    public static void TriggerEndingFromFinalBoss()
    {
        EnsureExists().ProjectEndingFromPlayerState();
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

    public EndingCinematicController.EndingType EvaluateMoralEnding()
    {
        int comp = 0;
        int rigor = 0;
        if (PlayerStats.Instance != null)
        {
            comp = PlayerStats.Instance.compassionPoints;
            rigor = PlayerStats.Instance.rigorPoints;
        }

        if (comp > rigor) return EndingCinematicController.EndingType.Luminoso;   // Armonía Humana
        if (rigor > comp) return EndingCinematicController.EndingType.Oscuro;     // Tirano Vorst
        return EndingCinematicController.EndingType.Neutral;                      // Sacrificio Nexo
    }

    public void ProjectEndingFromPlayerState()
    {
        if (hasTriggeredEnding) return;
        hasTriggeredEnding = true;

        Time.timeScale = 1f;

        // 1. Detener el generador de oleadas y limpiar amenazas secundarias activas
        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.StopAllCoroutines();
            spawner.enabled = false;
        }

        EnemyDummy[] enemies = FindObjectsByType<EnemyDummy>(FindObjectsInactive.Exclude);
        foreach (EnemyDummy e in enemies)
        {
            if (e != null && !e.IsDead)
            {
                // Desactivar enemigos de escolta restantes para cerrar el combate limpiamente
                e.StopAllCoroutines();
                Collider2D c = e.GetComponent<Collider2D>();
                if (c != null) c.enabled = false;
                if (!e.isBoss)
                {
                    Destroy(e.gameObject, 0.15f);
                }
            }
        }

        // 2. Evaluar y guardar desenlace moral
        EndingCinematicController.EndingType selectedEnding = EvaluateMoralEnding();

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.isPaused = false;
            PlayerStats.Instance.isUpgradeTreeOpen = false;
            PlayerStats.Instance.isArsenalShopOpen = false;
            PlayerStats.Instance.isChoosingSkill = false;
            PlayerStats.Instance.isChoosingDilemma = false;
            PlayerStats.Instance.SaveProgressForNextLevel();
        }

        PlayerPrefs.SetInt("KAEL_GAME_COMPLETED", 1);
        PlayerPrefs.SetString("KAEL_LAST_ENDING", selectedEnding.ToString());
        PlayerPrefs.Save();

        // 3. Liberar visualmente la cápsula de Lía en el domo si está presente
        NexusThroneroomEnvironment env = NexusThroneroomEnvironment.Instance;
        if (env != null)
        {
            env.TriggerLiaLiberationVisual(selectedEnding);
        }

        StartCoroutine(ReleaseLiaAndLaunchEndingRoutine(selectedEnding));
    }

    private IEnumerator ReleaseLiaAndLaunchEndingRoutine(EndingCinematicController.EndingType selectedEnding)
    {
        switch (selectedEnding)
        {
            case EndingCinematicController.EndingType.Luminoso:
                transitionTitle = "☀ CÁPSULA DE LÍA LIBERADA — SENDA: ARMONÍA HUMANA";
                transitionSubtitle = "Pureza Nexo dominante: Kael purifica el corazón del Trono sin perder su humanidad.";
                transitionColor = new Color(0.25f, 0.95f, 1f);
                break;
            case EndingCinematicController.EndingType.Oscuro:
                transitionTitle = "☣ TRONO ASIMILADO — SENDA: TIRANO VORST";
                transitionSubtitle = "Disonancia dominante: El Guantelete absorbe al Soberano y corona un nuevo señor.";
                transitionColor = new Color(1f, 0.22f, 0.45f);
                break;
            default:
                transitionTitle = "❖ NÚCLEO ESTABILIZADO — SENDA: SACRIFICIO NEXO";
                transitionSubtitle = "Equilibrio simbiótico: N.E.X.A. asume la sobrecarga para salvar a Kael y Lía.";
                transitionColor = new Color(1f, 0.85f, 0.28f);
                break;
        }

        transitionBannerActive = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVictoryAndDilemmaMusic();
        }

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.TriggerScreenFlash(transitionColor, 0.72f);
            CombatVFXManager.Instance.ShakeCamera(0.45f, 0.16f);
        }

        yield return new WaitForSecondsRealtime(1.55f);

        transitionBannerActive = false;
        EndingCinematicController.LaunchEnding(selectedEnding);
    }

    void OnGUI()
    {
        if (!transitionBannerActive) return;
        if (EndingCinematicController.Instance != null && EndingCinematicController.Instance.isEndingActive) return;

        GUI.depth = -800;
        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;

        Rect banner = new Rect(320f * sx, 160f * sy, 1280f * sx, 124f * sy);
        GUI.color = new Color(0.02f, 0.03f, 0.08f, 0.92f);
        GUI.DrawTexture(banner, Texture2D.whiteTexture);
        GUI.color = transitionColor;
        GUI.DrawTexture(new Rect(banner.x, banner.y, banner.width, 4f * sy), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(banner.x, banner.yMax - 4f * sy, banner.width, 4f * sy), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(16, Mathf.RoundToInt(28f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = transitionColor;
        GUI.Label(new Rect(banner.x + 20f * sx, banner.y + 14f * sy, banner.width - 40f * sx, 46f * sy), transitionTitle, titleStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(13, Mathf.RoundToInt(18f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        subStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(banner.x + 30f * sx, banner.y + 64f * sy, banner.width - 60f * sx, 44f * sy), transitionSubtitle, subStyle);
    }
}
