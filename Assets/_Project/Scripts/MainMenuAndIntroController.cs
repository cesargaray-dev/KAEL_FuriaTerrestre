using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuAndIntroController : MonoBehaviour
{
    [Header("Nombre de la Escena del Nivel 1")]
    public string gameplaySceneName = "Lab_Cesar";

    private Texture2D menuBgTex;
    private Texture2D[] introSlides = new Texture2D[4];
    private Texture2D portKaelShout;
    private Texture2D portNexaNormal;

    private bool inCinematic = false;
    private bool showCredits = false;
    private int currentSlide = 0;
    private float slideTimer = 0f;
    private float fadeAlpha = 0f;

    private readonly string[] slideTitles = {
        "ACTO I — LA CAÍDA EN 90 MINUTOS",
        "ACTO II — LA PROMESA A LÍA",
        "ACTO III — EL LEGADO BAJO LAS RUINAS",
        "ACTO IV — SINCRONIZACIÓN NEXO"
    };

    private readonly string[] slideSpeakers = {
        "ARCHIVO DE DEFENSA TERRESTRE",
        "KAEL",
        "N.E.X.A. [SISTEMA ANCESTRAL]",
        "KAEL & N.E.X.A."
    };

    private readonly string[] slideNarrations = {
        "Cayeron desde la órbita sin previo aviso. La flota biomecánica Vorst fracturó las defensas del planeta en noventa minutos, convirtiendo nuestras ciudades en hornos de recolección.",
        "\"¡LÍA! ¡Suéltenla!...\" Un haz de tracción púrpura arrancó a mi hermana menor de mis brazos hacia la Nave Núcleo. Entre el fuego del Sector 7, juré que ni un ejército entero me impediría traerla de vuelta.",
        "Bajo los escombros del antiguo museo, un reactor olvidado respondió a mi pulso. El Guantelete Nexo se fundió con mi brazo derecho: un arma evolutiva capaz de devorar la energía de los propios Vorst.",
        "\"Enlace neuronal completado, Kael. Soy N.E.X.A. He localizado la señal vital de Lía en la cúspide de la Nave Núcleo... Pero ten cuidado: cada vez que uses nuestro poder, la Disonancia consumirá parte de tu humanidad.\""
    };

    void Awake()
    {
        Time.timeScale = 1f;
        menuBgTex = LoadTex("StoryUI/MenuBG");
        for (int i = 0; i < 4; i++)
        {
            introSlides[i] = LoadTex($"StoryUI/Intro_{i + 1}");
        }
        portKaelShout = LoadTex("StoryUI/Portrait_Kael_Shout");
        portNexaNormal = LoadTex("StoryUI/Portrait_Nexa_Normal");
    }

    Texture2D LoadTex(string path)
    {
        Sprite sp = Resources.Load<Sprite>(path);
        return (sp != null) ? sp.texture : Resources.Load<Texture2D>(path);
    }

    void Update()
    {
        if (inCinematic)
        {
            slideTimer += Time.deltaTime;
            if (fadeAlpha > 0f)
                fadeAlpha = Mathf.MoveTowards(fadeAlpha, 0f, Time.deltaTime * 1.8f);
        }
    }

    void StartCinematic()
    {
        inCinematic = true;
        currentSlide = 0;
        slideTimer = 0f;
        fadeAlpha = 1f;
    }

    void NextSlideOrStartGame()
    {
        if (currentSlide < 3)
        {
            currentSlide++;
            slideTimer = 0f;
            fadeAlpha = 0.85f;
        }
        else
        {
            LoadLevel1();
        }
    }

    void LoadLevel1()
    {
        if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
        else if (SceneManager.sceneCountInBuildSettings > 1)
        {
            SceneManager.LoadScene(1);
        }
        else
        {
            Debug.LogError($"Agrega la escena '{gameplaySceneName}' en File -> Build Profiles / Build Settings -> Scene List.");
        }
    }

    void OnGUI()
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1f));

        if (!inCinematic)
        {
            DrawMainMenuScreen();
        }
        else
        {
            DrawCinematicScreen();
        }

        GUI.matrix = oldMatrix;
    }

    void DrawMainMenuScreen()
    {
        // 1. Ilustración de Fondo con ligero zoom respiratorio (Ken Burns)
        if (menuBgTex != null)
        {
            float zoom = 1.0f + Mathf.Sin(Time.time * 0.45f) * 0.025f;
            float w = 1920f * zoom;
            float h = 1080f * zoom;
            GUI.DrawTexture(new Rect((1920f - w) * 0.5f, (1080f - h) * 0.5f, w, h), menuBgTex, ScaleMode.StretchToFill);
        }

        // Gradiente oscuro a la derecha para que el Título y los Botones resalten con nitidez total
        GUI.color = new Color(0.02f, 0.04f, 0.09f, 0.78f);
        GUI.DrawTexture(new Rect(1080, 0, 840, 1080), Texture2D.whiteTexture);
        GUI.color = new Color(0.22f, 0.88f, 1f, 0.8f);
        GUI.DrawTexture(new Rect(1080, 0, 4, 1080), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 2. Logotipo Épico del Juego (Arriba a la derecha)
        DrawStyledText(new Rect(1120, 95, 760, 40), "PROYECTO IDLE ACTION RPG // SECTOR 7", 18, TextAnchor.MiddleCenter, new Color(0.22f, 0.92f, 1f));
        DrawStyledText(new Rect(1120, 140, 760, 95), "KAEL", 86, TextAnchor.MiddleCenter, Color.white);
        DrawStyledText(new Rect(1120, 225, 760, 55), "FURIA TERRESTRE", 42, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.22f));
        DrawStyledText(new Rect(1140, 290, 720, 35), "\"Cada golpe es un mensaje: la Tierra no está en venta.\"", 17, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.95f));

        // 3. Botones Interactivos del Menú
        float btnX = 1240f;
        float btnW = 520f;
        float btnH = 72f;

        if (DrawMenuButton(new Rect(btnX, 370, btnW, btnH), "▶  INICIAR CAMPAÑA (CON CINEMÁTICA)", new Color(0.08f, 0.32f, 0.48f), new Color(0.22f, 0.92f, 1f), true))
        {
            StartCinematic();
        }

        if (DrawMenuButton(new Rect(btnX, 462, btnW, btnH), "⚔  NIVEL 1: SECTOR 7 (LAB_CESAR)", new Color(0.12f, 0.16f, 0.28f), new Color(1f, 0.82f, 0.22f), false))
        {
            LoadLevel1();
        }

        if (DrawMenuButton(new Rect(btnX, 554, btnW, btnH), "⚡  NIVEL 2: BASE VORST (LAB_ZAHIR)", new Color(0.16f, 0.08f, 0.28f), new Color(0.85f, 0.35f, 1f), false))
        {
            SceneManager.LoadScene("Lab_Zahir");
        }

        if (DrawMenuButton(new Rect(btnX, 646, btnW, btnH), "❖  CRÉDITOS Y EQUIPO DE DESARROLLO", new Color(0.08f, 0.10f, 0.18f), new Color(0.65f, 0.45f, 1f), false))
        {
            showCredits = !showCredits;
        }

        if (DrawMenuButton(new Rect(btnX, 738, btnW, btnH), "✖  SALIR DEL JUEGO", new Color(0.22f, 0.06f, 0.12f), new Color(1f, 0.3f, 0.4f), false))
        {
            Application.Quit();
        }

        // Ventana de Créditos
        if (showCredits)
        {
            Rect credBox = new Rect(120, 680, 860, 320);
            DrawCyberPanel(credBox, new Color(0.03f, 0.05f, 0.11f, 0.94f), new Color(0.22f, 0.92f, 1f), 3f);
            DrawStyledText(new Rect(credBox.x, credBox.y + 20, credBox.width, 40), "EQUIPO DE DESARROLLO — KAEL: FURIA TERRESTRE", 24, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.22f));
            DrawStyledText(new Rect(credBox.x + 40, credBox.y + 75, credBox.width - 80, 180),
                "• <b>Cesar Enrique Garay García</b> — Gameplay, Combate, IA, Oleadas y Arquitectura\n" +
                "• <b>Zahir Andrés Rodríguez Mora</b> — Progresión, Interfaz, Arte y Audio\n\n" +
                "• <b>Materia:</b> Creación de Videojuegos   |   <b>Profesor:</b> Gabriel Barrón Rodríguez",
                20, TextAnchor.MiddleLeft, Color.white);
        }

        DrawStyledText(new Rect(40, 1030, 900, 35), "Versión 1.0 — Build Móvil & PC (Unity 6)  |  Guantelete Nexo Online", 16, TextAnchor.MiddleLeft, new Color(0.6f, 0.75f, 0.9f));
    }

    void DrawCinematicScreen()
    {
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Texture2D slide = introSlides[currentSlide];
        if (slide != null)
        {
            float zoom = 1.0f + Mathf.Clamp01(slideTimer / 8f) * 0.08f;
            float w = 1520f * zoom;
            float h = 760f * zoom;
            Rect imgRect = new Rect((1920f - w) * 0.5f, 35f - (h - 760f) * 0.5f, w, h);
            GUI.DrawTexture(imgRect, slide, ScaleMode.ScaleAndCrop);

            GUI.color = new Color(0.22f, 0.88f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(198, 32, 1524, 3), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(198, 798, 1524, 3), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        Rect dialogBox = new Rect(160, 815, 1600, 235);
        DrawCyberPanel(dialogBox, new Color(0.03f, 0.06f, 0.12f, 0.96f), new Color(0.22f, 0.88f, 1f), 3f);

        Texture2D port = (currentSlide == 1) ? portKaelShout : ((currentSlide >= 2) ? portNexaNormal : null);
        float textStartX = dialogBox.x + 35f;
        float textWidth = dialogBox.width - 340f;

        if (port != null)
        {
            // Retrato grande (200x200 píxeles llenando todo el recuadro izquierdo)
            Rect pRect = new Rect(dialogBox.x + 18, dialogBox.y + 18, 200, 200);
            GUI.color = new Color(0.06f, 0.10f, 0.18f, 1f);
            GUI.DrawTexture(pRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(pRect, port, ScaleMode.ScaleAndCrop);

            textStartX = dialogBox.x + 238f;
            textWidth = dialogBox.width - 530f;
        }

        DrawStyledText(new Rect(textStartX, dialogBox.y + 20, textWidth, 32), $"{slideTitles[currentSlide]}   —   <color=#ffd35c>{slideSpeakers[currentSlide]}</color>", 20, TextAnchor.MiddleLeft, new Color(0.22f, 0.92f, 1f));

        string fullTxt = slideNarrations[currentSlide];
        int chars = Mathf.Clamp(Mathf.FloorToInt(slideTimer * 45f), 0, fullTxt.Length);
        DrawStyledText(new Rect(textStartX, dialogBox.y + 60, textWidth, 145), fullTxt.Substring(0, chars), 21, TextAnchor.UpperLeft, Color.white);

        string nextLabel = (currentSlide < 3) ? $"SIGUIENTE ESCENA ({currentSlide + 1}/4) ▶" : "⚔ ¡ENTRAR AL SECTOR 7! ▶";
        if (DrawMenuButton(new Rect(dialogBox.x + dialogBox.width - 275, dialogBox.y + 45, 250, 75), nextLabel, new Color(0.08f, 0.35f, 0.50f), new Color(0.22f, 0.92f, 1f), true))
        {
            if (chars < fullTxt.Length)
                slideTimer = 10f;
            else
                NextSlideOrStartGame();
        }

        if (DrawMenuButton(new Rect(dialogBox.x + dialogBox.width - 275, dialogBox.y + 135, 250, 58), "SALTAR INTRO ⏭", new Color(0.15f, 0.18f, 0.25f), Color.gray, false))
        {
            LoadLevel1();
        }

        if (fadeAlpha > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
    bool DrawMenuButton(Rect r, string label, Color bg, Color border, bool pulseGlow)
    {
        float pulse = pulseGlow ? (0.75f + Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.25f) : 1f;
        DrawCyberPanel(r, bg, new Color(border.r, border.g, border.b, pulse), pulseGlow ? 3.5f : 2f);
        DrawStyledText(r, label, 20, TextAnchor.MiddleCenter, Color.white);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    void DrawCyberPanel(Rect r, Color bg, Color border, float thick)
    {
        GUI.color = border;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = bg;
        GUI.DrawTexture(new Rect(r.x + thick, r.y + thick, r.width - thick * 2f, r.height - thick * 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawStyledText(Rect r, string txt, int size, TextAnchor align, Color col)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.richText = true;
        st.fontSize = size;
        st.fontStyle = FontStyle.Bold;
        st.alignment = align;
        st.wordWrap = true;

        st.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), txt, st);

        st.normal.textColor = col;
        GUI.Label(r, txt, st);
    }
}