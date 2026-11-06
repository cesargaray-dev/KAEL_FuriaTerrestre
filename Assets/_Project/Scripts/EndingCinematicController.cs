using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// BLOQUE 6 (Issue #24): Sistema de 3 Finales Cinemáticos según el Nivel de Disonancia Nexo
/// (Compasión vs Rigor). Incluye ilustraciones Widescreen HD (Ending_Light, Ending_Dark, Ending_Neutral),
/// retratos holográficos, botones biselados de alta nitidez y narración interactiva manual.
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
    public bool isEndingActive => isActive;
    public EndingType activeEnding = EndingType.Luminoso;
    private int currentSlide = 0;
    private float slideTimer = 0f;
    private const float CHARS_PER_SEC = 52f;

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
        if (texLight == null)
        {
            texLight = Resources.Load<Texture2D>("StoryUI/Ending_Light");
            if (texLight != null) texLight.filterMode = FilterMode.Point;
        }
        if (texDark == null)
        {
            texDark = Resources.Load<Texture2D>("StoryUI/Ending_Dark");
            if (texDark != null) texDark.filterMode = FilterMode.Point;
        }
        if (texNeutral == null)
        {
            texNeutral = Resources.Load<Texture2D>("StoryUI/Ending_Neutral");
            if (texNeutral != null) texNeutral.filterMode = FilterMode.Point;
        }
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
        if (Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        {
            currentSlide = (currentSlide + 1) % 3;
            slideTimer = 0f;
        }
    }

    private static bool DrawEndingCyberButton(Rect rect, string label, string btnResPath, Color borderCol, int fontSize)
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
            if (hovered)
            {
                GUI.color = new Color(borderCol.r, borderCol.g, borderCol.b, 0.25f);
                GUI.DrawTexture(drawRect, Texture2D.whiteTexture);
            }
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

    void OnGUI()
    {
        if (!isActive) return;

        GUI.depth = -900;
        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;

        // Fondo oscuro profundo
        GUI.color = new Color(0.01f, 0.01f, 0.03f, 0.99f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Color themeCol;
        Texture2D activeTex;
        string panelPath;
        string headerBadge;
        string mainTitle;
        string[] slides;
        string epilogueQuote;
        string portraitPath;

        if (activeEnding == EndingType.Luminoso)
        {
            themeCol = new Color(0.25f, 0.95f, 1f);
            activeTex = texLight;
            panelPath = "StoryUI/Panel_Cyber_Cyan";
            portraitPath = "StoryUI/Portrait_Kael_Normal";
            headerBadge = "☀ DESENLACE CANÓNICO I — SENDA DE LA PUREZA NEXO (COMPASIÓN > RIGOR)";
            mainTitle = "FINAL LUMINOSO: «EL HÉROE DE LA TIERRA»";
            slides = new string[]
            {
                "ACTO FINAL I: Tras derrotar a la jerarquía suprema Vorst, Kael canaliza la resonancia cian cristalina de su Guantelete Nexo para purificar el núcleo de la nave nodriza sin sacrificar su humanidad.",
                "ACTO FINAL II: Las cápsulas criogénicas se abren suavemente. Entre la luz dorada del amanecer emerge su hermana Lía, sana y salva, abrazando a Kael mientras las naves Vorst caen desactivadas.",
                "EPÍLOGO: Juntos contemplan el nuevo amanecer sobre una Tierra libre y reconstruida. N.E.X.A. permanece sincronizada como guardiana pacífica de la humanidad."
            };
            epilogueQuote = "«El verdadero poder del Nexo nunca estuvo en la furia de las armas Vorst, sino en la humanidad que elegimos proteger.» — N.E.X.A.";
        }
        else if (activeEnding == EndingType.Oscuro)
        {
            themeCol = new Color(1f, 0.22f, 0.45f);
            activeTex = texDark;
            panelPath = "StoryUI/Panel_Cyber_Crimson";
            portraitPath = "StoryUI/Portrait_Kael_Shout";
            headerBadge = "☣ DESENLACE CANÓNICO II — SENDA DE LA DISONANCIA VORST (RIGOR > COMPASIÓN)";
            mainTitle = "FINAL OSCURO: «EL SEÑOR DE LA DISONANCIA»";
            slides = new string[]
            {
                "ACTO FINAL I: La absorción incontrolada de núcleos Vorst rompe el último sello biológico del Guantelete. Venas de plasma púrpura abisal y carmesí envuelven la armadura de Kael con una potencia devastadora.",
                "ACTO FINAL II: Kael libera un pulso de aniquilación total que somete instantáneamente a toda la flota invasora Vorst en órbita y rescata la cápsula de su hermana Lía entre las cenizas del trono.",
                "EPÍLOGO: La Tierra está a salvo, pero cuando Lía despierta y mira los ojos carmesí de su hermano en el trono biomecánico, comprende que Kael se ha transformado en el nuevo Soberano de la Disonancia."
            };
            epilogueQuote = "«ADVERTENCIA CRÍTICA: Límite biológico superado. El portador Kael Vane ha ascendido como Soberano Absoluto de la Red Vorst.» — N.E.X.A.";
        }
        else
        {
            themeCol = new Color(0.65f, 0.88f, 1f);
            activeTex = texNeutral;
            panelPath = "StoryUI/Panel_Cyber_Gold";
            portraitPath = "StoryUI/Portrait_Nexa_Normal";
            headerBadge = "❖ DESENLACE CANÓNICO III — EQUILIBRIO SIMBIÓTICO (COMPASIÓN == RIGOR)";
            mainTitle = "FINAL NEUTRAL: «EL ÚLTIMO LEGADO DE N.E.X.A.»";
            slides = new string[]
            {
                "ACTO FINAL I: Con el reactor central de la Nave Núcleo al borde del colapso gravitacional, ni la pureza ni la fuerza bruta bastan para contener la explosión sin poner en riesgo la cápsula de Lía.",
                "ACTO FINAL II: En una decisión autónoma, la IA N.E.X.A. desacopla su matriz neural del Guantelete de Kael y transfiere su propia conciencia al corazón del reactor Vorst para estabilizar la evacuación.",
                "EPÍLOGO: «Fue un honor pelear a tu lado, Kael. Cuida de Lía.» El Guantelete queda en silencio mientras Kael y su hermana escapan hacia la Tierra bajo una lluvia de fragmentos de luz cian."
            };
            epilogueQuote = "«TRANSMISIÓN FINAL DE N.E.X.A.: Protocolo de protección completado al 100%. Hasta siempre, compañero.»";
        }

        // Cabecera superior
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
            fontSize = Mathf.Max(18, Mathf.RoundToInt(32f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleSt.normal.textColor = Color.white;
        GUI.Label(new Rect(160f * sx, 42f * sy, 1600f * sx, 42f * sy), mainTitle, titleSt);

        // Marco Widescreen de la Ilustración Final (1380x560 escalado)
        Rect artFrame = new Rect(270f * sx, 92f * sy, 1380f * sx, 560f * sy);
        GUI.color = themeCol;
        GUI.DrawTexture(new Rect(artFrame.x - 3f, artFrame.y - 3f, artFrame.width + 6f, artFrame.height + 6f), Texture2D.whiteTexture);
        GUI.color = Color.black;
        GUI.DrawTexture(artFrame, Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (activeTex != null)
        {
            float zoom = 1f + Mathf.PingPong(slideTimer * 0.015f, 0.05f);
            Rect uv = new Rect((1f - 1f / zoom) * 0.5f, (1f - 1f / zoom) * 0.5f, 1f / zoom, 1f / zoom);
            GUI.DrawTextureWithTexCoords(artFrame, activeTex, uv);
        }

        // Línea de escaneo sutil sobre la ilustración
        float scanY = artFrame.y + Mathf.PingPong(Time.unscaledTime * 95f * sy, artFrame.height - 4f);
        GUI.color = new Color(themeCol.r, themeCol.g, themeCol.b, 0.22f);
        GUI.DrawTexture(new Rect(artFrame.x, scanY, artFrame.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Panel narrativo inferior biselado
        Rect narrativeBox = new Rect(270f * sx, 668f * sy, 1380f * sx, 214f * sy);
        Texture2D cyberPanel = PlayerStats.LoadResTex(panelPath);
        if (cyberPanel != null)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.98f);
            GUI.DrawTexture(narrativeBox, cyberPanel, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = new Color(0.03f, 0.04f, 0.10f, 0.95f);
            GUI.DrawTexture(narrativeBox, Texture2D.whiteTexture);
            GUI.color = themeCol;
            GUI.DrawTexture(new Rect(narrativeBox.x, narrativeBox.y, narrativeBox.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Retrato a la izquierda del cuadro narrativo
        Rect portRect = new Rect(narrativeBox.x + 22f * sx, narrativeBox.y + 22f * sy, 148f * sx, 148f * sy);
        GUI.color = themeCol;
        GUI.DrawTexture(new Rect(portRect.x - 2f, portRect.y - 2f, portRect.width + 4f, portRect.height + 4f), Texture2D.whiteTexture);
        GUI.color = new Color(0.03f, 0.05f, 0.11f, 1f);
        GUI.DrawTexture(portRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        Texture2D portTex = PlayerStats.LoadResTex(portraitPath);
        if (portTex != null)
        {
            portTex.filterMode = FilterMode.Point;
            GUI.DrawTexture(portRect, portTex, ScaleMode.ScaleToFit);
        }

        string fullText = slides[currentSlide];
        int visibleChars = Mathf.Clamp(Mathf.FloorToInt(slideTimer * CHARS_PER_SEC), 0, fullText.Length);
        string shownText = fullText.Substring(0, visibleChars);

        GUIStyle bodySt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(13, Mathf.RoundToInt(20f * sy)),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        bodySt.normal.textColor = Color.white;
        GUI.Label(new Rect(narrativeBox.x + 190f * sx, narrativeBox.y + 24f * sy, 1160f * sx, 112f * sy), shownText, bodySt);

        GUIStyle quoteSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(12, Mathf.RoundToInt(15f * sy)),
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleLeft
        };
        quoteSt.normal.textColor = themeCol;
        GUI.Label(new Rect(narrativeBox.x + 190f * sx, narrativeBox.y + 148f * sy, 1160f * sx, 40f * sy), epilogueQuote, quoteSt);

        int btnFont = Mathf.Max(12, Mathf.RoundToInt(16f * sy));

        // Botón principal de avance de página
        if (DrawEndingCyberButton(
            new Rect(270f * sx, 902f * sy, 300f * sx, 56f * sy),
            $"SIGUIENTE PÁGINA ({currentSlide + 1}/3) ▶",
            "StoryUI/Btn_Cyber_Cyan",
            new Color(0.2f, 0.95f, 1f),
            btnFont))
        {
            if (visibleChars < fullText.Length)
            {
                slideTimer = 999f;
            }
            else
            {
                currentSlide = (currentSlide + 1) % 3;
                slideTimer = 0f;
            }
        }

        // Botones para previsualizar cualquiera de los 3 finales en vivo
        if (DrawEndingCyberButton(
            new Rect(590f * sx, 902f * sy, 235f * sx, 56f * sy),
            "☀ FINAL LUMINOSO",
            "StoryUI/Btn_Cyber_Gold",
            new Color(1f, 0.82f, 0.25f),
            btnFont))
        {
            ShowEnding(EndingType.Luminoso);
        }

        if (DrawEndingCyberButton(
            new Rect(840f * sx, 902f * sy, 235f * sx, 56f * sy),
            "☣ FINAL OSCURO",
            "StoryUI/Btn_Cyber_Crimson",
            new Color(1f, 0.25f, 0.45f),
            btnFont))
        {
            ShowEnding(EndingType.Oscuro);
        }

        if (DrawEndingCyberButton(
            new Rect(1090f * sx, 902f * sy, 235f * sx, 56f * sy),
            "❖ FINAL NEUTRAL",
            "StoryUI/Btn_Cyber_Purple",
            new Color(0.75f, 0.35f, 1f),
            btnFont))
        {
            ShowEnding(EndingType.Neutral);
        }

        if (DrawEndingCyberButton(
            new Rect(1345f * sx, 902f * sy, 305f * sx, 56f * sy),
            "🏠 MENÚ PRINCIPAL",
            "StoryUI/Btn_Cyber_Emerald",
            new Color(0.25f, 0.95f, 0.55f),
            btnFont))
        {
            isActive = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene("00_MainMenu");
        }
    }
}
