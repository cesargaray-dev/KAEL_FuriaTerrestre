using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenuAndIntroController : MonoBehaviour
{
    [Header("Nombre de la Escena del Nivel 1")]
    public string gameplaySceneName = "Lab_Cesar";

    private Texture2D menuBgTex;
    private Texture2D[] introSlides = new Texture2D[4];
    private Texture2D portKaelShout;
    private Texture2D portNexaNormal;
    private Texture2D btnCyanTex;
    private Texture2D btnGoldTex;
    private Texture2D btnPurpleTex;
    private Texture2D btnCrimsonTex;
    private Texture2D panelCyanTex;

    private bool inCinematic = false;
    private bool showCredits = false;
    private bool showChapterSelect = false;
    private int currentSlide = 0;
    private float slideTimer = 0f;
    private float fadeAlpha = 0f;

    // Sistema de partículas/chispas cian y doradas flotando en OnGUI
    private struct MenuSpark
    {
        public Vector2 pos; // Coordenadas base 1920x1080
        public float speedY;
        public float swayAmp;
        public float swayFreq;
        public float phase;
        public float size;
        public bool isGold;
    }

    private MenuSpark[] sparks;
    private const int SPARK_COUNT = 42;
    private float[] btnHoverAnim = new float[20];
    private int keyboardMenuFocus = 0;
    private float lastKeyNavTime = -10f;

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
        portKaelShout  = LoadTex("StoryUI/Portrait_Kael_Shout");
        portNexaNormal = LoadTex("StoryUI/Portrait_Nexa_Normal");
        btnCyanTex     = LoadTex("StoryUI/Btn_Cyber_Cyan");
        btnGoldTex     = LoadTex("StoryUI/Btn_Cyber_Gold");
        btnPurpleTex   = LoadTex("StoryUI/Btn_Cyber_Purple");
        btnCrimsonTex  = LoadTex("StoryUI/Btn_Cyber_Crimson");
        panelCyanTex   = LoadTex("StoryUI/Panel_Cyber_Cyan");

        VorstCodexUI.EnsureExists();
        InitSparks();
    }

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }
    }

    void InitSparks()
    {
        sparks = new MenuSpark[SPARK_COUNT];
        System.Random rng = new System.Random(1337);
        for (int i = 0; i < SPARK_COUNT; i++)
        {
            sparks[i] = new MenuSpark
            {
                pos = new Vector2((float)rng.NextDouble() * 1920f, (float)rng.NextDouble() * 1080f),
                speedY = 22f + (float)rng.NextDouble() * 48f,
                swayAmp = 14f + (float)rng.NextDouble() * 28f,
                swayFreq = 0.8f + (float)rng.NextDouble() * 1.8f,
                phase = (float)rng.NextDouble() * Mathf.PI * 2f,
                size = 3.5f + (float)rng.NextDouble() * 5.0f,
                isGold = (i % 3 == 0)
            };
        }
    }

    Texture2D LoadTex(string path)
    {
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null) return tex;
        Sprite sp = Resources.Load<Sprite>(path);
        return (sp != null) ? sp.texture : null;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Actualizar partículas flotantes
        if (sparks != null)
        {
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i].pos.y -= sparks[i].speedY * dt;
                if (sparks[i].pos.y < -20f)
                {
                    sparks[i].pos.y = 1100f;
                    sparks[i].pos.x = (sparks[i].pos.x + 237f) % 1920f;
                }
            }
        }

        if (inCinematic)
        {
            slideTimer += Time.deltaTime;
            if (fadeAlpha > 0f)
                fadeAlpha = Mathf.MoveTowards(fadeAlpha, 0f, Time.deltaTime * 1.8f);
        }
        else if (VorstCodexUI.Instance == null || !VorstCodexUI.Instance.isOpen)
        {
            HandleMainMenuKeyboard();
        }
    }

    void HandleMainMenuKeyboard()
    {
        if (Time.unscaledTime - lastKeyNavTime < 0.12f) return;

        bool cPressed = (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame);
        if (!cPressed) { try { cPressed = Input.GetKeyDown(KeyCode.C); } catch { } }
        if (cPressed)
        {
            lastKeyNavTime = Time.unscaledTime;
            OpenVorstCodexFromMenu();
            return;
        }

        bool upPressed = (Keyboard.current != null && (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame));
        if (!upPressed) { try { upPressed = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W); } catch { } }
        if (upPressed)
        {
            lastKeyNavTime = Time.unscaledTime;
            keyboardMenuFocus = (keyboardMenuFocus - 1 + 8) % 8;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            return;
        }

        bool downPressed = (Keyboard.current != null && (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame));
        if (!downPressed) { try { downPressed = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S); } catch { } }
        if (downPressed)
        {
            lastKeyNavTime = Time.unscaledTime;
            keyboardMenuFocus = (keyboardMenuFocus + 1) % 8;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            return;
        }

        bool enterPressed = (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame));
        if (!enterPressed) { try { enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space); } catch { } }
        if (enterPressed)
        {
            lastKeyNavTime = Time.unscaledTime;
            ExecuteFocusedMenuButton(keyboardMenuFocus);
        }
    }

    void ExecuteFocusedMenuButton(int focusIdx)
    {
        switch (focusIdx)
        {
            case 0: StartCinematic(); break;
            case 1: LoadLevel1(); break;
            case 2:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                SceneManager.LoadScene("Lab_Zahir");
                break;
            case 3:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                SceneManager.LoadScene("03_Nivel3_Colmena");
                break;
            case 4: OpenVorstCodexFromMenu(); break;
            case 5:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                showChapterSelect = !showChapterSelect;
                if (showChapterSelect) showCredits = false;
                break;
            case 6:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                showCredits = !showCredits;
                if (showCredits) showChapterSelect = false;
                break;
            case 7:
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                Application.Quit();
                break;
        }
    }

    void OpenVorstCodexFromMenu()
    {
        showChapterSelect = false;
        showCredits = false;
        VorstCodexUI codex = VorstCodexUI.EnsureExists();
        if (codex != null) codex.OpenCodex();
    }

    void StartCinematic()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        inCinematic = true;
        currentSlide = 0;
        slideTimer = 0f;
        fadeAlpha = 1f;
    }

    void NextSlideOrStartGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
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
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

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

    // --- FUNCIONES AUXILIARES DE ESCALADO NATIVO (1920x1080 -> Píxeles Reales) ---
    Rect ScaleRect(float x, float y, float w, float h)
    {
        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;
        return new Rect(
            Mathf.Round(x * sx),
            Mathf.Round(y * sy),
            Mathf.Round(w * sx),
            Mathf.Round(h * sy)
        );
    }

    int ScaleFont(int baseSize)
    {
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        return Mathf.Max(10, Mathf.RoundToInt(baseSize * scale));
    }

    void OnGUI()
    {
        // Mantener GUI.matrix en identidad para renderizar todas las fuentes a resolución nativa con nitidez absoluta
        GUI.matrix = Matrix4x4.identity;

        if (VorstCodexUI.Instance != null && VorstCodexUI.Instance.isOpen)
        {
            return;
        }

        if (!inCinematic)
        {
            DrawMainMenuScreen();
        }
        else
        {
            DrawCinematicScreen();
        }
    }

    void DrawMainMenuScreen()
    {
        // 1. Ilustración de Fondo con ligero zoom respiratorio (Ken Burns)
        if (menuBgTex != null)
        {
            float zoom = 1.0f + Mathf.Sin(Time.time * 0.45f) * 0.025f;
            float w = 1920f * zoom;
            float h = 1080f * zoom;
            GUI.DrawTexture(ScaleRect((1920f - w) * 0.5f, (1080f - h) * 0.5f, w, h), menuBgTex, ScaleMode.StretchToFill);
        }
        else
        {
            GUI.color = new Color(0.03f, 0.05f, 0.10f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // 2. Sistema de Partículas / Chispas Cian y Doradas flotando en OnGUI
        DrawFloatingSparks();

        // 3. Panel oscuro derecho con doble línea divisoria neón para destacar el Título y los Botones
        GUI.color = new Color(0.02f, 0.04f, 0.09f, 0.84f);
        GUI.DrawTexture(ScaleRect(1060, 0, 860, 1080), Texture2D.whiteTexture);
        GUI.color = new Color(0.22f, 0.90f, 1f, 0.88f);
        GUI.DrawTexture(ScaleRect(1060, 0, 4, 1080), Texture2D.whiteTexture);
        GUI.color = new Color(1.00f, 0.82f, 0.22f, 0.45f);
        GUI.DrawTexture(ScaleRect(1068, 0, 2, 1080), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 4. Logotipo Épico del Juego (Arriba a la derecha) con fuentes nativas nítidas
        DrawStyledText(ScaleRect(1100, 62, 780, 34), "PROYECTO IDLE ACTION RPG // SECTOR 7 & BASE VORST", 17, TextAnchor.MiddleCenter, new Color(0.22f, 0.92f, 1f));
        DrawStyledText(ScaleRect(1100, 100, 780, 96), "KAEL", 84, TextAnchor.MiddleCenter, Color.white);
        DrawStyledText(ScaleRect(1100, 190, 780, 52), "FURIA TERRESTRE", 40, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.22f));
        DrawStyledText(ScaleRect(1120, 248, 740, 32), "\"Cada golpe es un mensaje: la Tierra no está en venta.\"", 16, TextAnchor.MiddleCenter, new Color(0.78f, 0.88f, 0.98f));

        // 5. Botones Interactivos del Menú con Bordes Dobles Cyberpunk, Animación Hover y Barra Lateral Luminosa
        float btnX = 1210f;
        float btnW = 560f;
        float btnH = 54f;

        if (DrawMenuButton(0, new Rect(btnX, 282, btnW, btnH), "▶  INICIAR CAMPAÑA (CON CINEMÁTICA)", new Color(0.06f, 0.26f, 0.42f), new Color(0.22f, 0.92f, 1f), true))
        {
            keyboardMenuFocus = 0;
            StartCinematic();
        }

        if (DrawMenuButton(1, new Rect(btnX, 344, btnW, btnH), "⚔  NIVEL 1: SECTOR 7 (LAB_CESAR)", new Color(0.10f, 0.15f, 0.28f), new Color(1f, 0.82f, 0.22f), false))
        {
            keyboardMenuFocus = 1;
            LoadLevel1();
        }

        if (DrawMenuButton(2, new Rect(btnX, 406, btnW, btnH), "⚡  NIVEL 2: BASE VORST (LAB_ZAHIR)", new Color(0.15f, 0.08f, 0.28f), new Color(0.85f, 0.38f, 1f), false))
        {
            keyboardMenuFocus = 2;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            SceneManager.LoadScene("Lab_Zahir");
        }

        if (DrawMenuButton(7, new Rect(btnX, 468, btnW, btnH), "☣  NIVEL 3: COLMENA NAVE NÚCLEO", new Color(0.14f, 0.07f, 0.24f), new Color(0.22f, 0.96f, 0.82f), false))
        {
            keyboardMenuFocus = 3;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            SceneManager.LoadScene("03_Nivel3_Colmena");
        }

        int unlockedCodex = VorstCodexUI.GetUnlockedSpeciesCount();
        if (DrawMenuButton(17, new Rect(btnX, 530, btnW, btnH), $"📘  CÓDICE VORST — BESTIARIO TÁCTICO ({unlockedCodex}/6) [C]", new Color(0.06f, 0.22f, 0.36f), new Color(0.24f, 0.96f, 1f), true))
        {
            keyboardMenuFocus = 4;
            OpenVorstCodexFromMenu();
        }

        if (DrawMenuButton(8, new Rect(btnX, 592, btnW, btnH), "❖  SELECTOR DE CAPÍTULOS Y 3 FINALES", new Color(0.09f, 0.18f, 0.26f), new Color(1f, 0.84f, 0.28f), false))
        {
            keyboardMenuFocus = 5;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            showChapterSelect = !showChapterSelect;
            if (showChapterSelect) showCredits = false;
        }

        if (DrawMenuButton(3, new Rect(btnX, 654, btnW, btnH), "❖  CRÉDITOS Y EQUIPO DE DESARROLLO", new Color(0.08f, 0.11f, 0.20f), new Color(0.55f, 0.65f, 1f), false))
        {
            keyboardMenuFocus = 6;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            showCredits = !showCredits;
            if (showCredits) showChapterSelect = false;
        }

        if (DrawMenuButton(4, new Rect(btnX, 716, btnW, btnH), "✖  SALIR DEL JUEGO", new Color(0.22f, 0.06f, 0.12f), new Color(1f, 0.32f, 0.42f), false))
        {
            keyboardMenuFocus = 7;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            Application.Quit();
        }

        // 6. Control Deslizable de Volumen + Botón de Silencio en la Esquina Inferior Derecha
        DrawBottomAudioControl(new Rect(btnX, 788f, btnW, 118f));

        // 6.5 Ventana Modal de Selección de Capítulo / Puente Jugable / 3 Finales
        if (showChapterSelect)
        {
            Rect chapBox = ScaleRect(65, 290, 950, 680);
            DrawCyberPanelDouble(chapBox, new Color(0.02f, 0.05f, 0.11f, 0.97f), new Color(0.22f, 0.92f, 1f), new Color(1f, 0.82f, 0.22f, 0.55f), 2.5f);
            DrawStyledText(ScaleRect(65, 308, 950, 38), "❖ SELECTOR DE CAPÍTULOS, PUENTE JUGABLE Y 3 FINALES ❖", 24, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.24f));
            DrawStyledText(ScaleRect(90, 346, 900, 28), "Acceso rápido para prueba y evaluación de niveles, transición narrativa y desenlaces morales:", 16, TextAnchor.MiddleCenter, new Color(0.75f, 0.90f, 1f));

            float cBtnX1 = 105f;
            float cBtnX2 = 545f;
            float cBtnW = 430f;
            float cBtnH = 64f;

            if (DrawMenuButton(9, new Rect(cBtnX1, 395, cBtnW, cBtnH), "⚔ CAPÍTULO 1: SECTOR 7 (CESAR)", new Color(0.08f, 0.18f, 0.30f), new Color(0.22f, 0.92f, 1f), false))
            {
                LoadLevel1();
            }
            if (DrawMenuButton(10, new Rect(cBtnX2, 395, cBtnW, cBtnH), "🌉 PUENTE JUGABLE NV.1 ➔ NV.2", new Color(0.14f, 0.12f, 0.28f), new Color(1f, 0.82f, 0.24f), false))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                BridgeLevel1To2Controller.StartBridgeSequence("Lab_Zahir");
            }

            if (DrawMenuButton(11, new Rect(cBtnX1, 478, cBtnW, cBtnH), "⚡ CAPÍTULO 2: BASE VORST (ZAHIR)", new Color(0.14f, 0.08f, 0.26f), new Color(0.85f, 0.38f, 1f), false))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                SceneManager.LoadScene("Lab_Zahir");
            }
            if (DrawMenuButton(12, new Rect(cBtnX2, 478, cBtnW, cBtnH), "☣ CAPÍTULO 3: COLMENA BIO-CABLES", new Color(0.06f, 0.22f, 0.24f), new Color(0.22f, 0.96f, 0.82f), true))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                SceneManager.LoadScene("03_Nivel3_Colmena");
            }

            DrawStyledText(ScaleRect(90, 562, 900, 32), "── DESENLACES CINEMÁTICOS DINÁMICOS (SEGÚN COMPASIÓN VS RIGOR) ──", 17, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.24f));

            if (DrawMenuButton(13, new Rect(cBtnX1, 606, cBtnW, cBtnH), "☀ FINAL 1: LUMINOSO (COMPASIÓN)", new Color(0.06f, 0.24f, 0.34f), new Color(0.25f, 0.95f, 1f), false))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                EndingCinematicController.LaunchEnding(EndingCinematicController.EndingType.Luminoso);
            }
            if (DrawMenuButton(14, new Rect(cBtnX2, 606, cBtnW, cBtnH), "🩸 FINAL 2: OSCURO (RIGOR / VORST)", new Color(0.24f, 0.06f, 0.14f), new Color(1f, 0.22f, 0.38f), false))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                EndingCinematicController.LaunchEnding(EndingCinematicController.EndingType.Oscuro);
            }

            if (DrawMenuButton(15, new Rect(220, 690, 640, cBtnH), "⚖ FINAL 3: NEUTRAL (EL ÚLTIMO LEGADO DE N.E.X.A.)", new Color(0.16f, 0.14f, 0.06f), new Color(1f, 0.82f, 0.24f), false))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                EndingCinematicController.LaunchEnding(EndingCinematicController.EndingType.Neutral);
            }

            if (DrawMenuButton(16, new Rect(395, 780, 290, 52), "✖ CERRAR SELECTOR", new Color(0.18f, 0.08f, 0.14f), new Color(1f, 0.40f, 0.48f), false))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                showChapterSelect = false;
            }
        }

        // 7. Ventana de Créditos
        if (showCredits)
        {
            Rect credBox = ScaleRect(80, 650, 920, 330);
            DrawCyberPanelDouble(credBox, new Color(0.03f, 0.05f, 0.11f, 0.96f), new Color(0.22f, 0.92f, 1f), new Color(1f, 0.82f, 0.22f, 0.5f), 2.5f);
            DrawStyledText(ScaleRect(80, 672, 920, 40), "EQUIPO DE DESARROLLO — KAEL: FURIA TERRESTRE", 24, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.22f));
            DrawStyledText(ScaleRect(120, 728, 840, 210),
                "• <color=#38e1ff>Cesar Enrique Garay García</color> — Gameplay, Combate, IA, Oleadas y Arquitectura\n" +
                "• <color=#ffd35c>Zahir Andrés Rodríguez Mora</color> — Nivel 2 Base Vorst, Progresión, Interfaz y Arte\n\n" +
                "• <color=#38e1ff>Materia:</color> Creación de Videojuegos   |   <color=#ffd35c>Profesor:</color> Gabriel Barrón Rodríguez\n" +
                "• <color=#a882ff>Banda Sonora:</color> 4 Pistas Originales (Synthwave / Cyberpunk / Industrial Darksynth)",
                19, TextAnchor.UpperLeft, Color.white, true);
        }

        DrawStyledText(ScaleRect(40, 1028, 980, 36), "Versión 2.0 — Build Móvil & PC (Unity 6)  |  Guantelete Nexo & Audio Reactivo Online", 16, TextAnchor.MiddleLeft, new Color(0.68f, 0.82f, 0.96f));
    }

    void DrawFloatingSparks()
    {
        if (sparks == null) return;
        float t = Time.unscaledTime;

        for (int i = 0; i < sparks.Length; i++)
        {
            MenuSpark sp = sparks[i];
            float x = sp.pos.x + Mathf.Sin(t * sp.swayFreq + sp.phase) * sp.swayAmp;
            float y = sp.pos.y;
            float alpha = 0.35f + Mathf.Abs(Mathf.Sin(t * 2.2f + sp.phase)) * 0.55f;

            Color c = sp.isGold
                ? new Color(1.00f, 0.84f, 0.24f, alpha)
                : new Color(0.22f, 0.92f, 1.00f, alpha);

            Rect r = ScaleRect(x, y, sp.size, sp.size);
            // Halo exterior suave
            GUI.color = new Color(c.r, c.g, c.b, alpha * 0.28f);
            GUI.DrawTexture(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), Texture2D.whiteTexture);
            // Núcleo brillante
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    void DrawBottomAudioControl(Rect baseRect)
    {
        Rect box = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool muted = (AudioManager.Instance != null) && AudioManager.Instance.isMuted;
        float vol = (AudioManager.Instance != null) ? AudioManager.Instance.masterVolume : 0.85f;
        int pct = muted ? 0 : Mathf.RoundToInt(vol * 100f);

        Color accent = muted ? new Color(1f, 0.35f, 0.42f) : new Color(0.22f, 0.92f, 1f);
        DrawCyberPanelDouble(box, new Color(0.03f, 0.06f, 0.12f, 0.95f), accent, new Color(1f, 0.82f, 0.22f, 0.35f), 2f);

        DrawStyledText(ScaleRect(baseRect.x + 18f, baseRect.y + 10f, baseRect.width - 36f, 28f),
            muted ? "🔇 SISTEMA DE AUDIO: SILENCIADO" : $"🔊 VOLUMEN MAESTRO DE AUDIO: {pct}%",
            16, TextAnchor.MiddleLeft, muted ? new Color(1f, 0.45f, 0.5f) : new Color(0.22f, 0.92f, 1f));

        // Botón de Silencio (Mute / Unmute)
        Rect muteBtn = ScaleRect(baseRect.x + 18f, baseRect.y + 46f, 145f, 54f);
        bool hoverMute = muteBtn.Contains(Event.current.mousePosition);
        DrawCyberPanel(muteBtn,
            hoverMute ? new Color(0.16f, 0.24f, 0.38f, 0.98f) : new Color(0.08f, 0.12f, 0.20f, 0.95f),
            accent, 2f);
        DrawStyledText(muteBtn, muted ? "🔇 ACTIVAR" : "🔊 SILENCIAR", 15, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(muteBtn, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }

        // Botón [-]
        Rect minusBtn = ScaleRect(baseRect.x + 176f, baseRect.y + 46f, 48f, 54f);
        DrawCyberPanel(minusBtn, new Color(0.08f, 0.13f, 0.22f), accent, 1.5f);
        DrawStyledText(minusBtn, "−", 22, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(minusBtn, GUIContent.none, GUIStyle.none) && AudioManager.Instance != null)
        {
            AudioManager.Instance.isMuted = false;
            AudioManager.Instance.masterVolume = Mathf.Clamp01(vol - 0.10f);
            AudioManager.Instance.PlayUIClick();
        }

        // Control deslizable (Slider) interactivo
        Rect sliderTrack = ScaleRect(baseRect.x + 234f, baseRect.y + 58f, 246f, 30f);
        DrawCyberPanel(sliderTrack, new Color(0.05f, 0.08f, 0.15f, 1f), new Color(0.22f, 0.55f, 0.78f, 0.8f), 1.5f);

        float fillRatio = muted ? 0f : Mathf.Clamp01(vol);
        Rect fillRect = new Rect(sliderTrack.x + 3f, sliderTrack.y + 3f, (sliderTrack.width - 6f) * fillRatio, sliderTrack.height - 6f);
        GUI.color = muted ? new Color(0.55f, 0.20f, 0.25f) : new Color(0.22f, 0.90f, 1f, 0.92f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

        // Indicador (knob) luminoso en la posición del volumen
        float knobW = Mathf.Max(8f, Mathf.Round(10f * (Screen.width / 1920f)));
        float knobX = Mathf.Clamp(sliderTrack.x + (sliderTrack.width * fillRatio) - knobW * 0.5f, sliderTrack.x, sliderTrack.xMax - knobW);
        GUI.color = new Color(1f, 0.84f, 0.24f, 1f);
        GUI.DrawTexture(new Rect(knobX, sliderTrack.y - 3f, knobW, sliderTrack.height + 6f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && sliderTrack.Contains(Event.current.mousePosition))
        {
            if (AudioManager.Instance != null)
            {
                float newVol = Mathf.Clamp01((Event.current.mousePosition.x - sliderTrack.x) / Mathf.Max(1f, sliderTrack.width));
                AudioManager.Instance.isMuted = false;
                AudioManager.Instance.masterVolume = newVol;
                Event.current.Use();
            }
        }

        // Botón [+]
        Rect plusBtn = ScaleRect(baseRect.x + 490f, baseRect.y + 46f, 48f, 54f);
        DrawCyberPanel(plusBtn, new Color(0.08f, 0.13f, 0.22f), accent, 1.5f);
        DrawStyledText(plusBtn, "+", 22, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(plusBtn, GUIContent.none, GUIStyle.none) && AudioManager.Instance != null)
        {
            AudioManager.Instance.isMuted = false;
            AudioManager.Instance.masterVolume = Mathf.Clamp01(vol + 0.10f);
            AudioManager.Instance.PlayUIClick();
        }
    }

    void DrawCinematicScreen()
    {
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawFloatingSparks();

        Texture2D slide = introSlides[currentSlide];
        if (slide != null)
        {
            float zoom = 1.0f + Mathf.Clamp01(slideTimer / 8f) * 0.08f;
            float w = 1520f * zoom;
            float h = 760f * zoom;
            Rect imgRect = ScaleRect((1920f - w) * 0.5f, 35f - (h - 760f) * 0.5f, w, h);
            GUI.DrawTexture(imgRect, slide, ScaleMode.ScaleAndCrop);

            GUI.color = new Color(0.22f, 0.88f, 1f, 0.85f);
            GUI.DrawTexture(ScaleRect(198, 32, 1524, 3), Texture2D.whiteTexture);
            GUI.DrawTexture(ScaleRect(198, 798, 1524, 3), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        Rect dialogBox = ScaleRect(160, 815, 1600, 235);
        if (panelCyanTex != null)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.98f);
            GUI.DrawTexture(dialogBox, panelCyanTex, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }
        else
        {
            DrawCyberPanelDouble(dialogBox, new Color(0.03f, 0.06f, 0.12f, 0.96f), new Color(0.22f, 0.88f, 1f), new Color(1f, 0.82f, 0.22f, 0.45f), 2.5f);
        }

        Texture2D port = (currentSlide == 1) ? portKaelShout : ((currentSlide >= 2) ? portNexaNormal : null);
        float textStartX = 195f;
        float textWidth = 1260f;

        if (port != null)
        {
            Rect pFrame = ScaleRect(178, 833, 200, 200);
            GUI.color = (currentSlide == 1) ? new Color(1f, 0.82f, 0.22f, 0.96f) : new Color(0.22f, 0.92f, 1f, 0.96f);
            GUI.DrawTexture(pFrame, Texture2D.whiteTexture);
            Rect pRect = new Rect(pFrame.x + 2f, pFrame.y + 2f, pFrame.width - 4f, pFrame.height - 4f);
            GUI.color = new Color(0.04f, 0.07f, 0.14f, 1f);
            GUI.DrawTexture(pRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(pRect, port, ScaleMode.ScaleToFit);

            textStartX = 398f;
            textWidth = 1065f;
        }

        DrawStyledText(ScaleRect(textStartX, 835, textWidth, 34), $"{slideTitles[currentSlide]}   —   <color=#ffd35c>{slideSpeakers[currentSlide]}</color>", 20, TextAnchor.MiddleLeft, new Color(0.22f, 0.92f, 1f));

        string fullTxt = slideNarrations[currentSlide];
        int chars = Mathf.Clamp(Mathf.FloorToInt(slideTimer * 45f), 0, fullTxt.Length);
        DrawStyledText(ScaleRect(textStartX, 875, textWidth, 145), fullTxt.Substring(0, chars), 21, TextAnchor.UpperLeft, Color.white, true);

        string nextLabel = (currentSlide < 3) ? $"SIGUIENTE ESCENA ({currentSlide + 1}/4) ▶" : "⚔ ¡ENTRAR AL SECTOR 7! ▶";
        if (DrawMenuButton(5, new Rect(1485, 855, 255, 78), nextLabel, new Color(0.08f, 0.35f, 0.50f), new Color(0.22f, 0.92f, 1f), true))
        {
            if (chars < fullTxt.Length)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                slideTimer = 10f;
            }
            else
            {
                NextSlideOrStartGame();
            }
        }

        if (DrawMenuButton(6, new Rect(1485, 948, 255, 60), "SALTAR INTRO ⏭", new Color(0.15f, 0.18f, 0.25f), new Color(0.65f, 0.75f, 0.88f), false))
        {
            LoadLevel1();
        }

        if (fadeAlpha > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    bool DrawMenuButton(int btnIndex, Rect baseRect, string label, Color bg, Color border, bool pulseGlow)
    {
        Rect checkRect = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool hovered = checkRect.Contains(Event.current.mousePosition);

        if (btnIndex >= 0 && btnIndex < btnHoverAnim.Length)
        {
            float target = hovered ? 1f : 0f;
            btnHoverAnim[btnIndex] = Mathf.MoveTowards(btnHoverAnim[btnIndex], target, Time.unscaledDeltaTime * 8f);
        }
        float hVal = (btnIndex >= 0 && btnIndex < btnHoverAnim.Length) ? btnHoverAnim[btnIndex] : (hovered ? 1f : 0f);

        // Desplazamiento horizontal suave al pasar el cursor
        Rect r = ScaleRect(baseRect.x - hVal * 8f, baseRect.y - hVal * 2f, baseRect.width + hVal * 12f, baseRect.height + hVal * 4f);

        float pulse = pulseGlow ? (0.85f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4.5f)) * 0.15f) : 1f;

        Texture2D btnSkin = btnCyanTex;
        if (btnIndex == 1) btnSkin = btnGoldTex;
        else if (btnIndex == 2) btnSkin = btnPurpleTex;
        else if (btnIndex == 4) btnSkin = btnCrimsonTex;

        if (btnSkin != null)
        {
            float br = hovered ? 1.24f : pulse;
            GUI.color = new Color(br, br, br, 1f);
            GUI.DrawTexture(r, btnSkin, ScaleMode.StretchToFill);
            if (hovered)
            {
                GUI.color = new Color(1f, 0.94f, 0.65f, 0.14f);
                GUI.DrawTexture(new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.height - 8f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }
        else
        {
            Color outerBorder = hovered ? Color.white : new Color(border.r, border.g, border.b, pulse);
            Color innerBorder = new Color(border.r, border.g, border.b, hovered ? 0.65f : 0.30f);
            Color finalBg = Color.Lerp(bg, new Color(border.r * 0.38f, border.g * 0.38f, border.b * 0.38f, 0.98f), hVal * 0.55f);

            DrawCyberPanelDouble(r, finalBg, outerBorder, innerBorder, pulseGlow ? 2.5f : 2f);

            float sideBarW = Mathf.Max(5f, Mathf.Round((8f + hVal * 5f) * (Screen.width / 1920f)));
            GUI.color = hovered ? new Color(1f, 0.86f, 0.28f, 1f) : border;
            GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, sideBarW, r.height - 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        DrawStyledText(r, label, 20, TextAnchor.MiddleCenter, hovered ? new Color(1f, 0.96f, 0.82f) : Color.white);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    void DrawCyberPanel(Rect r, Color bg, Color border, float thick)
    {
        float t = Mathf.Max(1f, Mathf.Round(thick * Mathf.Min(Screen.width / 1920f, Screen.height / 1080f)));
        GUI.color = border;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = bg;
        GUI.DrawTexture(new Rect(r.x + t, r.y + t, r.width - t * 2f, r.height - t * 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawCyberPanelDouble(Rect r, Color bg, Color outerBorder, Color innerBorder, float thick)
    {
        float t = Mathf.Max(1f, Mathf.Round(thick * Mathf.Min(Screen.width / 1920f, Screen.height / 1080f)));
        float gap = Mathf.Max(2f, Mathf.Round(4f * Mathf.Min(Screen.width / 1920f, Screen.height / 1080f)));

        // Borde exterior
        GUI.color = outerBorder;
        GUI.DrawTexture(r, Texture2D.whiteTexture);

        // Fondo principal
        GUI.color = bg;
        GUI.DrawTexture(new Rect(r.x + t, r.y + t, r.width - t * 2f, r.height - t * 2f), Texture2D.whiteTexture);

        // Segundo borde interior estilo Cyberpunk
        Rect inner = new Rect(r.x + t + gap, r.y + t + gap, r.width - (t + gap) * 2f, r.height - (t + gap) * 2f);
        GUI.color = innerBorder;
        GUI.DrawTexture(new Rect(inner.x, inner.y, inner.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(inner.x, inner.yMax - 1f, inner.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(inner.x, inner.y, 1f, inner.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(inner.xMax - 1f, inner.y, 1f, inner.height), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    void DrawStyledText(Rect r, string txt, int baseSize, TextAnchor align, Color col, bool wordWrap = true)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.richText = true;
        st.fontSize = ScaleFont(baseSize);
        st.fontStyle = FontStyle.Bold;
        st.alignment = align;
        st.wordWrap = wordWrap;

        string shadowTxt = System.Text.RegularExpressions.Regex.Replace(txt, "<color=[^>]+>|</color>", "");
        st.normal.textColor = new Color(0f, 0f, 0f, 0.92f);
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), shadowTxt, st);

        st.normal.textColor = col;
        GUI.Label(r, txt, st);
    }
}
