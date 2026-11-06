using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BLOQUE 6 (Issue #24): Sistema de 3 Finales Cinemáticos según el Nivel de Disonancia Nexo
/// (Compasión vs Rigor). Incluye ilustraciones Widescreen HD (Ending_Light, Ending_Dark, Ending_Neutral),
/// narración épica en múltiples actos, partículas atmosféricas y selector interactivo de desenlace.
/// </summary>
public class EndingCinematicController : MonoBehaviour
{
    public enum EndingType
    {
        Luminoso, // Compasión > Rigor: "El Héroe de la Tierra"
        Oscuro,   // Rigor > Compasión: "El Señor de la Disonancia"
        Neutral   // Compasión == Rigor: "El Último Legado de N.E.X.A."
    }

    public static EndingCinematicController Instance { get; private set; }

    public bool isActive = false;
    public EndingType activeEnding = EndingType.Luminoso;
    private int currentSlide = 0;
    private float slideTimer = 0f;

    private Texture2D texLight;
    private Texture2D texDark;
    private Texture2D texNeutral;

    public static void LaunchEndingFromMoralState()
    {
        int comp = 0;
        int rig = 0;
        if (PlayerStats.Instance != null)
        {
            comp = PlayerStats.Instance.compassionPoints;
            rig = PlayerStats.Instance.rigorPoints;
        }

        EndingType chosen = EndingType.Neutral;
        if (comp > rig) chosen = EndingType.Luminoso;
        else if (rig > comp) chosen = EndingType.Oscuro;

        LaunchEnding(chosen);
    }

    public static void LaunchEnding(EndingType type)
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("EndingCinematicController");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<EndingCinematicController>();
        }
        Instance.ShowEnding(type);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadEndingIllustrations();
    }

    private void LoadEndingIllustrations()
    {
        if (texLight == null) texLight = Resources.Load<Texture2D>("StoryUI/Ending_Light");
        if (texDark == null) texDark = Resources.Load<Texture2D>("StoryUI/Ending_Dark");
        if (texNeutral == null) texNeutral = Resources.Load<Texture2D>("StoryUI/Ending_Neutral");
    }

    public void ShowEnding(EndingType type)
    {
        LoadEndingIllustrations();
        activeEnding = type;
        currentSlide = 0;
        slideTimer = 0f;
        isActive = true;

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.isLevelCompleted = false;
            PlayerStats.Instance.isChoosingDilemma = false;
            PlayerStats.Instance.isPaused = false;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVictoryAndDilemmaMusic();
        }

        Time.timeScale = 0f;
    }

    void Update()
    {
        if (!isActive) return;

        slideTimer += Time.unscaledDeltaTime;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            currentSlide = (currentSlide + 1) % 3;
            slideTimer = 0f;
        }
    }

    void OnGUI()
    {
        if (!isActive) return;

        GUI.depth = -500;
        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;

        // Fondo oscuro profundo
        GUI.color = new Color(0.01f, 0.01f, 0.03f, 0.98f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Color themeCol;
        Texture2D activeTex;
        string headerBadge;
        string mainTitle;
        string[] slides;
        string epilogueQuote;

        if (activeEnding == EndingType.Luminoso)
        {
            themeCol = new Color(0.25f, 0.95f, 1f);
            activeTex = texLight;
            headerBadge = "☀ DESENLACE CANÓNICO I — SENDA DE LA PUREZA NEXO (COMPASIÓN > RIGOR)";
            mainTitle = "FINAL LUMINOSO: «EL HÉROE DE LA TIERRA»";
            slides = new string[]
            {
                "ACTO FINAL I: Tras abatir al Carcelero Biotecnológico VORTEX-OMEGA, Kael canaliza la resonancia cian cristalina de su Guantelete Nexo para purificar el núcleo de la nave nodriza sin sacrificar su humanidad.",
                "ACTO FINAL II: Miles de cápsulas de estasis se abren suavemente en toda la Colmena. Entre la luz dorada emerge su hermana Lía, sana y salva, abrazando a Kael mientras las defensas Vorst se desactivan en todo el planeta.",
                "EPÍLOGO: Juntos contemplan el amanecer cian y dorado sobre una Tierra libre. N.E.X.A. permanece sincronizada como guardiana pacífica de la nueva era humana."
            };
            epilogueQuote = "«El verdadero poder del Nexo nunca estuvo en la furia de las armas Vorst, sino en la humanidad que elegimos proteger.» — N.E.X.A.";
        }
        else if (activeEnding == EndingType.Oscuro)
        {
            themeCol = new Color(1f, 0.22f, 0.45f);
            activeTex = texDark;
            headerBadge = "☣ DESENLACE CANÓNICO II — SENDA DE LA DISONANCIA VORST (RIGOR > COMPASIÓN)";
            mainTitle = "FINAL OSCURO: «EL SEÑOR DE LA DISONANCIA»";
            slides = new string[]
            {
                "ACTO FINAL I: La absorción incontrolada de núcleos Vorst rompe el último sello biológico del Guantelete. Venas de plasma púrpura abisal y carmesí envuelven la armadura de Kael con una potencia devastadora.",
                "ACTO FINAL II: Kael libera un pulso de aniquilación total que desintegra instantáneamente a toda la flota invasora Vorst en órbita y rescata la cápsula de su hermana Lía entre las cenizas de la Colmena.",
                "EPÍLOGO: La Tierra está a salvo, pero cuando Lía despierta y mira los ojos carmesí de su hermano, comprende que Kael se ha transformado en el nuevo Arconte de la Disonancia, temido incluso por la humanidad."
            };
            epilogueQuote = "«ADVERTENCIA CRÍTICA: Límite biológico superado. El portador Kael Vane ha ascendido como Soberano Absoluto de la Red Vorst.» — N.E.X.A.";
        }
        else
        {
            themeCol = new Color(0.65f, 0.88f, 1f);
            activeTex = texNeutral;
            headerBadge = "❖ DESENLACE CANÓNICO III — EQUILIBRIO SIMBIÓTICO (COMPASIÓN == RIGOR)";
            mainTitle = "FINAL NEUTRAL: «EL ÚLTIMO LEGADO DE N.E.X.A.»";
            slides = new string[]
            {
                "ACTO FINAL I: Con el reactor central de la Nave Núcleo al borde del colapso gravitacional, ni la pureza ni la fuerza bruta bastan para contener la explosión sin destruir la cápsula de Lía.",
                "ACTO FINAL II: En una decisión autónoma, la IA N.E.X.A. desacopla su matriz neural del Guantelete de Kael y transfiere su propia conciencia al corazón del reactor Vorst para estabilizar la evacuación.",
                "EPÍLOGO: «Fue un honor pelear a tu lado, Kael. Cuida de Lía.» El Guantelete queda en silencio mientras Kael y su hermana escapan hacia la Tierra bajo una lluvia de fragmentos de luz cian."
            };
            epilogueQuote = "«TRANSMISIÓN FINAL DE N.E.X.A.: Protocolo de protección completado al 100%. Hasta siempre, compañero.»";
        }

        // Cabecera superior
        GUIStyle badgeSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(11, Mathf.RoundToInt(15f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        badgeSt.normal.textColor = themeCol;
        GUI.Label(new Rect(160f * sx, 22f * sy, 1600f * sx, 28f * sy), headerBadge, badgeSt);

        GUIStyle titleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(18, Mathf.RoundToInt(34f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleSt.normal.textColor = Color.white;
        GUI.Label(new Rect(160f * sx, 50f * sy, 1600f * sx, 48f * sy), mainTitle, titleSt);

        // Marco Widescreen de la Ilustración Final (1280x540 escalado)
        Rect artFrame = new Rect(260f * sx, 108f * sy, 1400f * sx, 550f * sy);
        GUI.color = themeCol;
        GUI.DrawTexture(new Rect(artFrame.x - 3f, artFrame.y - 3f, artFrame.width + 6f, artFrame.height + 6f), Texture2D.whiteTexture);
        GUI.color = Color.black;
        GUI.DrawTexture(artFrame, Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (activeTex != null)
        {
            float zoom = 1f + Mathf.PingPong(slideTimer * 0.015f, 0.06f);
            Rect uv = new Rect((1f - 1f / zoom) * 0.5f, (1f - 1f / zoom) * 0.5f, 1f / zoom, 1f / zoom);
            GUI.DrawTextureWithTexCoords(artFrame, activeTex, uv);
        }

        // Línea de escaneo sutil sobre la pintura
        float scanY = artFrame.y + Mathf.PingPong(Time.unscaledTime * 110f * sy, artFrame.height - 4f);
        GUI.color = new Color(themeCol.r, themeCol.g, themeCol.b, 0.25f);
        GUI.DrawTexture(new Rect(artFrame.x, scanY, artFrame.width, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Panel narrativo inferior
        Rect narrativeBox = new Rect(260f * sx, 676f * sy, 1400f * sx, 210f * sy);
        GUI.color = new Color(0.03f, 0.04f, 0.10f, 0.95f);
        GUI.DrawTexture(narrativeBox, Texture2D.whiteTexture);
        GUI.color = themeCol;
        GUI.DrawTexture(new Rect(narrativeBox.x, narrativeBox.y, narrativeBox.width, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle bodySt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(13, Mathf.RoundToInt(20f * sy)),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        bodySt.normal.textColor = Color.white;
        GUI.Label(new Rect(295f * sx, 694f * sy, 1330f * sx, 110f * sy), slides[currentSlide], bodySt);

        GUIStyle quoteSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(12, Mathf.RoundToInt(16f * sy)),
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter
        };
        quoteSt.normal.textColor = themeCol;
        GUI.Label(new Rect(295f * sx, 815f * sy, 1330f * sx, 36f * sy), epilogueQuote, quoteSt);

        // Indicador de páginas y controles
        if (GUI.Button(new Rect(260f * sx, 905f * sy, 290f * sx, 52f * sy), $"▶ SIGUIENTE PÁGINA ({currentSlide + 1}/3)"))
        {
            currentSlide = (currentSlide + 1) % 3;
            slideTimer = 0f;
        }

        // Botones para previsualizar cualquiera de los 3 finales en vivo
        if (GUI.Button(new Rect(575f * sx, 905f * sy, 245f * sx, 52f * sy), "☀ FINAL 1: LUMINOSO"))
        {
            ShowEnding(EndingType.Luminoso);
        }
        if (GUI.Button(new Rect(835f * sx, 905f * sy, 245f * sx, 52f * sy), "☣ FINAL 2: OSCURO"))
        {
            ShowEnding(EndingType.Oscuro);
        }
        if (GUI.Button(new Rect(1095f * sx, 905f * sy, 245f * sx, 52f * sy), "❖ FINAL 3: NEUTRAL"))
        {
            ShowEnding(EndingType.Neutral);
        }

        if (GUI.Button(new Rect(1365f * sx, 905f * sy, 295f * sx, 52f * sy), "🏠 MENÚ PRINCIPAL"))
        {
            isActive = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene("00_MainMenu");
        }
    }
}
