using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Gestor central de narrativa visual cinemática y desenlaces morales para los capítulos finales:
/// - Viñeta 1 (Nv.2 -> Nv.3): Kael detonando la barrera exterior de la Nave Núcleo e ingresando por una brecha en llamas.
/// - Viñeta 2 (Nv.3 -> Nv.4): Kael contemplando a Lía dormida en la cápsula de energía mientras la sombra del Soberano se alza tras él.
/// - 3 Pinturas Finales (Desenlaces Morales):
///   * Final 1 (Armonía Humana): Amanecer en la Tierra (StoryUI/Cinematics/Ending_1_EarthDawn).
///   * Final 2 (Sacrificio Nexo): Silueta solitaria en el espacio (StoryUI/Cinematics/Ending_2_SpaceSilhouette).
///   * Final 3 (Tirano Vorst): Kael entronizado con armadura biomecánica total (StoryUI/Cinematics/Ending_3_BiomechThrone).
/// Sincronizado con CompanionDialogueManager para voces sintéticas, subtítulos y efectos cinemáticos,
/// e incluye botón de Salto Táctico en cada transición.
/// </summary>
public class EndingsManager : MonoBehaviour
{
    public enum CinematicVignetteType
    {
        None = 0,
        CoreBreach_Lv2To3 = 1,
        LiaDiscovery_Lv3To4 = 2
    }

    public static EndingsManager Instance { get; private set; }

    public bool hasTriggeredEnding { get; private set; } = false;
    public bool isCinematicActive => activeVignette != CinematicVignetteType.None;

    private CinematicVignetteType activeVignette = CinematicVignetteType.None;
    private string pendingTargetScene = "";
    private int currentSlideIndex = 0;
    private float slideTimer = 0f;
    private const float CHARS_PER_SEC = 50f;

    private Texture2D texCoreBreach;
    private Texture2D texLiaCapsule;
    private Texture2D texEndingDawn;
    private Texture2D texEndingSpace;
    private Texture2D texEndingThrone;

    private bool transitionBannerActive = false;
    private string transitionTitle = "";
    private string transitionSubtitle = "";
    private Color transitionColor = new Color(0.25f, 0.95f, 1f);

    public static bool CanLoadSceneSafe(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        if (Application.CanStreamedLevelBeLoaded(sceneName)) return true;
#if UNITY_EDITOR
        string editorPath = $"Assets/_Project/Scenes/{sceneName}.unity";
        if (System.IO.File.Exists(editorPath)) return true;
#endif
        return false;
    }

    public static void LoadSceneSafe(string sceneName)
    {
        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(sceneName)) return;

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }
#if UNITY_EDITOR
        string editorPath = $"Assets/_Project/Scenes/{sceneName}.unity";
        if (System.IO.File.Exists(editorPath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                editorPath,
                new LoadSceneParameters(LoadSceneMode.Single)
            );
            return;
        }
#endif
        SceneManager.LoadScene(sceneName);
    }

    private bool IsSharedSceneControllerObject()
    {
        return GetComponent<PlayerStats>() != null ||
               GetComponent<WaveSpawner>() != null ||
               GetComponent<MainMenuAndIntroController>() != null ||
               GetComponent<Camera>() != null;
    }

    public static EndingsManager EnsureExists()
    {
        if (Instance != null) return Instance;
        EndingsManager existing = FindAnyObjectByType<EndingsManager>();
        if (existing != null && !existing.IsSharedSceneControllerObject())
        {
            Instance = existing;
            if (existing.transform.parent == null)
            {
                DontDestroyOnLoad(existing.gameObject);
            }
            return existing;
        }
        if (existing != null && existing.IsSharedSceneControllerObject())
        {
            Destroy(existing);
        }
        GameObject go = new GameObject("EndingsManager");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<EndingsManager>();
        return Instance;
    }

    public static void TriggerEndingFromFinalBoss()
    {
        EnsureExists().ProjectEndingFromPlayerState();
    }

    public static void LaunchTransitionLv2To3(string targetScene = "03_Nivel3_Colmena")
    {
        if (string.IsNullOrEmpty(targetScene)) targetScene = "03_Nivel3_Colmena";
        EnsureExists().StartInterLevelVignette(CinematicVignetteType.CoreBreach_Lv2To3, targetScene);
    }

    public static void LaunchTransitionLv3To4(string targetScene = "04_Final_NexusThroneroom")
    {
        if (string.IsNullOrEmpty(targetScene)) targetScene = "04_Final_NexusThroneroom";
        EnsureExists().StartInterLevelVignette(CinematicVignetteType.LiaDiscovery_Lv3To4, targetScene);
    }

    void Awake()
    {
        if (IsSharedSceneControllerObject())
        {
            // Nunca marcar GameManager con DontDestroyOnLoad ni destruir el GameManager de la escena
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            LoadCinematicTextures();
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        LoadCinematicTextures();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private static Texture2D LoadCinematicTex(string primaryPath, string fallbackPath)
    {
        Texture2D tex = Resources.Load<Texture2D>(primaryPath);
        if (tex == null)
        {
            Sprite sp = Resources.Load<Sprite>(primaryPath);
            if (sp != null) tex = sp.texture;
        }
        if (tex == null && !string.IsNullOrEmpty(fallbackPath))
        {
            tex = Resources.Load<Texture2D>(fallbackPath);
            if (tex == null)
            {
                Sprite sp2 = Resources.Load<Sprite>(fallbackPath);
                if (sp2 != null) tex = sp2.texture;
            }
        }
        if (tex != null) tex.filterMode = FilterMode.Point;
        return tex;
    }

    public void LoadCinematicTextures()
    {
        if (texCoreBreach == null)
            texCoreBreach = LoadCinematicTex("StoryUI/Cinematics/Cinematic_1_CoreBreach", "StoryUI/Bridge_4");
        if (texLiaCapsule == null)
            texLiaCapsule = LoadCinematicTex("StoryUI/Cinematics/Cinematic_2_LiaCapsule", "Level4/BG_Throneroom_Level4");
        if (texEndingDawn == null)
            texEndingDawn = LoadCinematicTex("StoryUI/Cinematics/Ending_1_EarthDawn", "StoryUI/Ending_Light");
        if (texEndingSpace == null)
            texEndingSpace = LoadCinematicTex("StoryUI/Cinematics/Ending_2_SpaceSilhouette", "StoryUI/Ending_Neutral");
        if (texEndingThrone == null)
            texEndingThrone = LoadCinematicTex("StoryUI/Cinematics/Ending_3_BiomechThrone", "StoryUI/Ending_Dark");
    }

    public Texture2D GetEndingIllustration(EndingCinematicController.EndingType type)
    {
        LoadCinematicTextures();
        switch (type)
        {
            case EndingCinematicController.EndingType.Luminoso: return texEndingDawn;
            case EndingCinematicController.EndingType.Oscuro:   return texEndingThrone;
            default:                                            return texEndingSpace;
        }
    }

    public void StartInterLevelVignette(CinematicVignetteType type, string nextScene)
    {
        LoadCinematicTextures();
        activeVignette = type;
        if (string.IsNullOrEmpty(nextScene))
        {
            nextScene = (type == CinematicVignetteType.CoreBreach_Lv2To3)
                ? "03_Nivel3_Colmena"
                : "04_Final_NexusThroneroom";
        }
        pendingTargetScene = nextScene;
        hasTriggeredEnding = false;
        transitionBannerActive = false;
        currentSlideIndex = 0;
        slideTimer = 0f;

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.isLevelCompleted = false;
            PlayerStats.Instance.isChoosingDilemma = false;
            PlayerStats.Instance.isPaused = false;
            PlayerStats.Instance.isUpgradeTreeOpen = false;
            PlayerStats.Instance.isArsenalShopOpen = false;
        }

        CompanionDialogueManager dlg = CompanionDialogueManager.EnsureExists();
        if (dlg != null)
        {
            dlg.PlayCinematicTransitionEffect();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVictoryAndDilemmaMusic();
        }

        Time.timeScale = 0f;
    }

    public void SkipActiveCinematic()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        if (activeVignette != CinematicVignetteType.None)
        {
            CinematicVignetteType finishedVignette = activeVignette;
            string target = pendingTargetScene;
            if (string.IsNullOrEmpty(target))
            {
                target = (finishedVignette == CinematicVignetteType.CoreBreach_Lv2To3)
                    ? "03_Nivel3_Colmena"
                    : "04_Final_NexusThroneroom";
            }
            activeVignette = CinematicVignetteType.None;
            pendingTargetScene = "";
            hasTriggeredEnding = false;
            transitionBannerActive = false;
            Time.timeScale = 1f;
            LoadSceneSafe(target);
            return;
        }

        if (transitionBannerActive)
        {
            StopAllCoroutines();
            transitionBannerActive = false;
            EndingCinematicController.LaunchEnding(EvaluateMoralEnding());
        }
    }

    void Update()
    {
        if (activeVignette == CinematicVignetteType.None && !transitionBannerActive) return;

        if (Keyboard.current != null &&
            (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame))
        {
            SkipActiveCinematic();
            return;
        }

        if (activeVignette != CinematicVignetteType.None)
        {
            slideTimer += Time.unscaledDeltaTime;

            string[] slides = GetActiveVignetteSlides(out string[] speakers, out string[] portraits);
            string fullText = slides[Mathf.Clamp(currentSlideIndex, 0, slides.Length - 1)];
            int visibleChars = Mathf.Clamp(Mathf.FloorToInt(slideTimer * CHARS_PER_SEC), 0, fullText.Length);

            if (visibleChars < fullText.Length && CompanionDialogueManager.Instance != null)
            {
                string portKey = portraits[Mathf.Clamp(currentSlideIndex, 0, portraits.Length - 1)];
                CompanionDialogueManager.Instance.PlaySyntheticVoiceTick(portKey, visibleChars);
            }

            if (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            {
                AdvanceVignetteSlide(slides.Length, visibleChars < fullText.Length);
            }
        }
    }

    private void AdvanceVignetteSlide(int totalSlides, bool textStillTyping)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        if (textStillTyping)
        {
            slideTimer = 999f;
            return;
        }

        if (currentSlideIndex < totalSlides - 1)
        {
            currentSlideIndex++;
            slideTimer = 0f;
            if (CompanionDialogueManager.Instance != null)
            {
                CompanionDialogueManager.Instance.PlayCinematicTransitionEffect();
            }
        }
        else
        {
            SkipActiveCinematic();
        }
    }

    private string[] GetActiveVignetteSlides(out string[] speakers, out string[] portraits)
    {
        if (activeVignette == CinematicVignetteType.CoreBreach_Lv2To3)
        {
            speakers = new string[]
            {
                "N.E.X.A. [INFILTRACIÓN EN NAVE NÚCLEO]",
                "KAEL VANE [DETONACIÓN DE BARRERA]",
                "N.E.X.A. [BRECHA EN LLAMAS ABIERTA]"
            };
            portraits = new string[] { "nexa_alert", "kael_shout", "nexa_normal" };
            return new string[]
            {
                "CAPÍTULO III — ASALTO A LA NAVE NÚCLEO: Tras derribar al Arconte de la Base Vorst, Kael asciende hasta el blindaje exterior de la colosal Nave Núcleo suspendida sobre el Sector 7.",
                "«¡N.E.X.A., concentra toda la sobrecarga del reactor en mi puño derecho! ¡Ninguna barrera alienígena me separará de Lía!» El impacto del Guantelete detona el escudo orbital abriendo una brecha en llamas.",
                "Entre acero incandescente y chispas de plasma cian, Kael se lanza al interior del Laberinto de Bio-Cables (Nivel 3) donde miles de cápsulas criogénicas laten en la oscuridad."
            };
        }

        speakers = new string[]
        {
            "KAEL VANE [DESCUBRIMIENTO DEL SANTUARIO]",
            "N.E.X.A. [ALERTA DE SOMBRA SOBERANA]",
            "KAEL VANE [JURAMENTO FRATERNAL]"
        };
        portraits = new string[] { "kael_normal", "nexa_alert", "kael_shout" };
        return new string[]
        {
            "CAPÍTULO FINAL — EL TRONO DE LA DISONANCIA: Con VORTEX-OMEGA reducido a cenizas, las compuertas superiores se abren hacia el Domo Imperial frente a la Tierra en órbita. En el centro brilla la cápsula de Lía.",
            "«¡Lía, por fin te encontré!» Kael contempla a su hermana dormida en estasis bio-energética... pero una colosal sombra de armadura púrpura y estoque de plasma se alza silenciosamente tras el trono.",
            "«¡ADVERTENCIA CRÍTICA, KAEL! ¡El Soberano Supremo Vorst nos estaba esperando! Su enlace absorbe la energía de la cápsula de Lía. ¡Prepárate para la batalla final por la Tierra!»"
        };
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

        CompanionDialogueManager dlg = CompanionDialogueManager.EnsureExists();
        if (dlg != null)
        {
            dlg.PlayCinematicTransitionEffect();
        }

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
        if (activeVignette != CinematicVignetteType.None)
        {
            DrawWidescreenChapterVignette();
            return;
        }

        if (!transitionBannerActive) return;
        if (EndingCinematicController.Instance != null && EndingCinematicController.Instance.isEndingActive) return;

        GUI.depth = -800;
        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;

        Rect banner = new Rect(320f * sx, 160f * sy, 1280f * sx, 134f * sy);
        GUI.color = new Color(0.02f, 0.03f, 0.08f, 0.94f);
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
        GUI.Label(new Rect(banner.x + 20f * sx, banner.y + 12f * sy, banner.width - 40f * sx, 44f * sy), transitionTitle, titleStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(13, Mathf.RoundToInt(18f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        subStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(banner.x + 30f * sx, banner.y + 58f * sy, banner.width - 60f * sx, 42f * sy), transitionSubtitle, subStyle);

        // Botón de Salto Táctico inmediato durante el banner de transición
        Rect skipBannerBtn = new Rect(1320f * sx, 304f * sy, 280f * sx, 42f * sy);
        if (DrawTacticalButton(skipBannerBtn, "⏭ SALTO TÁCTICO [ESC]", "StoryUI/Btn_Cyber_Gold", transitionColor, Mathf.Max(11, Mathf.RoundToInt(14f * sy))))
        {
            SkipActiveCinematic();
        }
    }

    private void DrawWidescreenChapterVignette()
    {
        GUI.depth = -920;
        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;

        GUI.color = new Color(0.01f, 0.01f, 0.03f, 0.99f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        bool isVignette1 = (activeVignette == CinematicVignetteType.CoreBreach_Lv2To3);
        Color themeCol = isVignette1 ? new Color(1f, 0.52f, 0.18f) : new Color(0.76f, 0.32f, 1f);
        Texture2D activeTex = isVignette1 ? texCoreBreach : texLiaCapsule;
        string headerBadge = isVignette1
            ? "🔥 TRANSICIÓN CINEMÁTICA (NV.2 → NV.3) — INFILTRACIÓN EN LA NAVE NÚCLEO"
            : "👁 TRANSICIÓN CINEMÁTICA (NV.3 → NV.4) — LA CÁPSULA DE LÍA Y LA SOMBRA DEL TRONO";
        string mainTitle = isVignette1
            ? "VIÑETA I: «BRECHA EN LLAMAS SOBRE LA COLMENA VORST»"
            : "VIÑETA II: «EL SANTUARIO DE LÍA Y EL SOBERANO SUPREMO»";

        GUIStyle badgeSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(12, Mathf.RoundToInt(16f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        badgeSt.normal.textColor = themeCol;
        GUI.Label(new Rect(160f * sx, 16f * sy, 1600f * sx, 26f * sy), headerBadge, badgeSt);

        GUIStyle titleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(18, Mathf.RoundToInt(31f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleSt.normal.textColor = Color.white;
        GUI.Label(new Rect(160f * sx, 42f * sy, 1600f * sx, 42f * sy), mainTitle, titleSt);

        // Marco Widescreen HD (1380x560 escalado)
        Rect artFrame = new Rect(270f * sx, 92f * sy, 1380f * sx, 560f * sy);
        GUI.color = themeCol;
        GUI.DrawTexture(new Rect(artFrame.x - 3f, artFrame.y - 3f, artFrame.width + 6f, artFrame.height + 6f), Texture2D.whiteTexture);
        GUI.color = Color.black;
        GUI.DrawTexture(artFrame, Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (activeTex != null)
        {
            float zoom = 1f + Mathf.PingPong(slideTimer * 0.016f, 0.045f);
            Rect uv = new Rect((1f - 1f / zoom) * 0.5f, (1f - 1f / zoom) * 0.5f, 1f / zoom, 1f / zoom);
            GUI.DrawTextureWithTexCoords(artFrame, activeTex, uv);
        }

        // Línea de escaneo holográfico sobre la ilustración
        float scanY = artFrame.y + Mathf.PingPong(Time.unscaledTime * 95f * sy, artFrame.height - 4f);
        GUI.color = new Color(themeCol.r, themeCol.g, themeCol.b, 0.24f);
        GUI.DrawTexture(new Rect(artFrame.x, scanY, artFrame.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Cuadro de Subtítulos Sincronizado con CompanionDialogueManager
        string[] slides = GetActiveVignetteSlides(out string[] speakers, out string[] portraits);
        int idx = Mathf.Clamp(currentSlideIndex, 0, slides.Length - 1);
        string fullText = slides[idx];
        int visibleChars = Mathf.Clamp(Mathf.FloorToInt(slideTimer * CHARS_PER_SEC), 0, fullText.Length);
        string shownText = fullText.Substring(0, visibleChars);

        Rect narrativeBox = new Rect(270f * sx, 668f * sy, 1380f * sx, 214f * sy);
        string panelPath = isVignette1 ? "StoryUI/Panel_Cyber_Gold" : "StoryUI/Panel_Cyber_Crimson";
        Texture2D cyberPanel = PlayerStats.LoadResTex(panelPath);
        if (cyberPanel != null)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.98f);
            GUI.DrawTexture(narrativeBox, cyberPanel, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = new Color(0.03f, 0.04f, 0.10f, 0.96f);
            GUI.DrawTexture(narrativeBox, Texture2D.whiteTexture);
            GUI.color = themeCol;
            GUI.DrawTexture(new Rect(narrativeBox.x, narrativeBox.y, narrativeBox.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Retrato del hablante
        Rect portRect = new Rect(narrativeBox.x + 22f * sx, narrativeBox.y + 22f * sy, 148f * sx, 148f * sy);
        GUI.color = themeCol;
        GUI.DrawTexture(new Rect(portRect.x - 2f, portRect.y - 2f, portRect.width + 4f, portRect.height + 4f), Texture2D.whiteTexture);
        GUI.color = new Color(0.03f, 0.05f, 0.11f, 1f);
        GUI.DrawTexture(portRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        string portRes = portraits[idx] == "kael_shout" ? "StoryUI/Portrait_Kael_Shout"
                       : portraits[idx] == "kael_normal" ? "StoryUI/Portrait_Kael_Normal"
                       : portraits[idx] == "nexa_alert" ? "StoryUI/Portrait_Nexa_Alert"
                       : "StoryUI/Portrait_Nexa_Normal";
        Texture2D portTex = PlayerStats.LoadResTex(portRes);
        if (portTex != null)
        {
            portTex.filterMode = FilterMode.Point;
            GUI.DrawTexture(portRect, portTex, ScaleMode.ScaleToFit);
        }

        GUIStyle spkStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(13, Mathf.RoundToInt(19f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        spkStyle.normal.textColor = themeCol;
        GUI.Label(new Rect(narrativeBox.x + 190f * sx, narrativeBox.y + 18f * sy, 1150f * sx, 28f * sy), "📡 " + speakers[idx], spkStyle);

        GUIStyle bodySt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(13, Mathf.RoundToInt(20f * sy)),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        bodySt.normal.textColor = Color.white;
        GUI.Label(new Rect(narrativeBox.x + 190f * sx, narrativeBox.y + 52f * sy, 1150f * sx, 118f * sy), shownText, bodySt);

        int btnFont = Mathf.Max(12, Mathf.RoundToInt(16f * sy));

        // Botón de Avance de Acto
        string nextLabel = (idx < slides.Length - 1)
            ? $"SIGUIENTE ACTO ({idx + 1}/{slides.Length}) [ESPACIO] ▶"
            : "⚔ INICIAR DESPLIEGUE DE COMBATE ▶";
        if (DrawTacticalButton(
            new Rect(270f * sx, 902f * sy, 480f * sx, 56f * sy),
            nextLabel,
            "StoryUI/Btn_Cyber_Cyan",
            new Color(0.22f, 0.95f, 1f),
            btnFont))
        {
            AdvanceVignetteSlide(slides.Length, visibleChars < fullText.Length);
        }

        // Botón de Salto Táctico (Requisito: permitir omitir cada transición cinemática)
        if (DrawTacticalButton(
            new Rect(1190f * sx, 902f * sy, 460f * sx, 56f * sy),
            "⏭ SALTO TÁCTICO — OMITIR CINEMÁTICA [ESC]",
            "StoryUI/Btn_Cyber_Crimson",
            new Color(1f, 0.35f, 0.45f),
            btnFont))
        {
            SkipActiveCinematic();
        }
    }

    private static bool DrawTacticalButton(Rect rect, string label, string btnResPath, Color borderCol, int fontSize)
    {
        Vector2 mousePos = Vector2.zero;
        if (Mouse.current != null)
        {
            Vector2 mp = Mouse.current.position.ReadValue();
            mousePos = new Vector2(mp.x, Screen.height - mp.y);
        }
        bool hovered = rect.Contains(mousePos);

        Rect drawRect = hovered
            ? new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f)
            : rect;

        Texture2D btnTex = PlayerStats.LoadResTex(btnResPath);
        if (btnTex != null)
        {
            GUI.color = hovered ? Color.white : new Color(0.92f, 0.94f, 1f, 0.98f);
            GUI.DrawTexture(drawRect, btnTex, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = borderCol;
            GUI.DrawTexture(new Rect(drawRect.x - 2f, drawRect.y - 2f, drawRect.width + 4f, drawRect.height + 4f), Texture2D.whiteTexture);
            GUI.color = new Color(0.05f, 0.08f, 0.16f, 0.96f);
            GUI.DrawTexture(drawRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        st.normal.textColor = new Color(0f, 0f, 0f, 0.92f);
        GUI.Label(new Rect(drawRect.x + 2f, drawRect.y + 2f, drawRect.width, drawRect.height), label, st);
        st.normal.textColor = hovered ? new Color(1f, 0.98f, 0.78f) : Color.white;
        GUI.Label(drawRect, label, st);

        GUI.color = Color.clear;
        bool clicked = GUI.Button(drawRect, "");
        GUI.color = Color.white;
        return clicked;
    }
}
