using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// BLOQUE 3 (Issue #18): Cinemática Puente Interactiva y Jugable entre el Nivel 1 (Lab_Cesar)
/// y el Nivel 2 (Lab_Zahir).
/// - Fase 1: Secuencia Narrativa Ilustrada en 4 Actos con estética Cyberpunk HD (idéntica a la Intro),
///   retratos de Kael y N.E.X.A., efecto teletipo y avance manual mediante botones biselados.
/// - Fase 2: Tramo Jugable de Infiltración en el Perímetro donde el jugador mueve a Kael (A/D),
///   destruye la Compuerta de Campo de Fuerza Vorst con (J) y cruza el umbral hacia Lab_Zahir.
/// </summary>
public class BridgeLevel1To2Controller : MonoBehaviour
{
    public static BridgeLevel1To2Controller Instance { get; private set; }
    public static bool launchBridgeOnSceneLoad = false;

    public bool isBridgeActive = false;
    public bool isInPlayablePhase = false;

    private int currentAct = 0;
    private float actTimer = 0f;
    private float fadeAlpha = 0f;

    // Texturas HD para los 4 actos cinemáticos y skins de interfaz
    private Texture2D[] actTextures = new Texture2D[4];
    private Texture2D portKaelShout;
    private Texture2D portNexaNormal;
    private Texture2D panelCyanTex;
    private Texture2D btnCyanTex;
    private Texture2D btnGoldTex;
    private Texture2D btnPurpleTex;
    private float[] btnHoverAnim = new float[4];

    // Partículas flotantes estilo Cyberpunk para la cinemática
    private struct BridgeSpark
    {
        public Vector2 pos;
        public float speedY;
        public float swayAmp;
        public float swayFreq;
        public float phase;
        public float size;
        public bool isGold;
    }
    private BridgeSpark[] sparks;
    private const int SPARK_COUNT = 38;

    // Fase 2: Compuerta de Energía Vorst destructible en el mundo 2D
    private GameObject barrierObject;
    private SpriteRenderer barrierRenderer;
    private float barrierMaxHp = 3f;
    private float barrierCurrentHp = 3f;
    private float barrierHitFlashTimer = 0f;
    private float barrierArcTimer = 0f;
    private bool barrierDestroyed = false;
    private float portalTransitionTimer = 0f;
    private float fadeOutAlpha = 0f;

    private KaelAutoController kael;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterBridgeHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (launchBridgeOnSceneLoad && scene.name == "Lab_Cesar")
        {
            launchBridgeOnSceneLoad = false;
            StartBridgeSequence();
        }
    }

    public static void StartBridgeSequence(string nextScene = "Lab_Zahir")
    {
        if (SceneManager.GetActiveScene().name != "Lab_Cesar")
        {
            launchBridgeOnSceneLoad = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene("Lab_Cesar");
            return;
        }

        if (Instance == null)
        {
            GameObject go = new GameObject("BridgeLevel1To2Controller");
            Instance = go.AddComponent<BridgeLevel1To2Controller>();
        }
        Instance.BeginPhase1Narrative();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitSparks();
    }

    private void InitSparks()
    {
        sparks = new BridgeSpark[SPARK_COUNT];
        System.Random rng = new System.Random(2026);
        for (int i = 0; i < SPARK_COUNT; i++)
        {
            sparks[i] = new BridgeSpark
            {
                pos = new Vector2((float)rng.NextDouble() * 1920f, (float)rng.NextDouble() * 1080f),
                speedY = 20f + (float)rng.NextDouble() * 44f,
                swayAmp = 12f + (float)rng.NextDouble() * 24f,
                swayFreq = 0.8f + (float)rng.NextDouble() * 1.6f,
                phase = (float)rng.NextDouble() * Mathf.PI * 2f,
                size = 3.5f + (float)rng.NextDouble() * 4.5f,
                isGold = (i % 3 == 0)
            };
        }
    }

    private Texture2D LoadUITex(string path)
    {
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null) return tex;
        Sprite sp = Resources.Load<Sprite>(path);
        return (sp != null) ? sp.texture : null;
    }

    public void BeginPhase1Narrative()
    {
        isBridgeActive = true;
        isInPlayablePhase = false;
        currentAct = 0;
        actTimer = 0f;
        fadeAlpha = 1f;
        barrierDestroyed = false;
        fadeOutAlpha = 0f;

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.isLevelCompleted = false;
            PlayerStats.Instance.isChoosingDilemma = false;
            PlayerStats.Instance.isPaused = false;
            PlayerStats.Instance.isUpgradeTreeOpen = false;
            PlayerStats.Instance.isArsenalShopOpen = false;
        }

        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
        }

        EnemyDummy[] enemies = FindObjectsByType<EnemyDummy>();
        foreach (EnemyDummy e in enemies)
        {
            if (e != null) Destroy(e.gameObject);
        }

        portKaelShout  = LoadUITex("StoryUI/Portrait_Kael_Shout");
        portNexaNormal = LoadUITex("StoryUI/Portrait_Nexa_Normal");
        panelCyanTex   = LoadUITex("StoryUI/Panel_Cyber_Cyan");
        btnCyanTex     = LoadUITex("StoryUI/Btn_Cyber_Cyan");
        btnGoldTex     = LoadUITex("StoryUI/Btn_Cyber_Gold");
        btnPurpleTex   = LoadUITex("StoryUI/Btn_Cyber_Purple");

        LoadBridgeActArtwork();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayVictoryAndDilemmaMusic();

        Time.timeScale = 1f;
    }

    private void LoadBridgeActArtwork()
    {
        for (int act = 0; act < 4; act++)
        {
            Texture2D resTex = LoadUITex($"StoryUI/Bridge_{act + 1}");
            if (resTex != null)
            {
                resTex.filterMode = FilterMode.Bilinear;
                resTex.wrapMode = TextureWrapMode.Clamp;
                actTextures[act] = resTex;
            }
        }
    }

    private void BeginPhase2PlayableInfiltration()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        isInPlayablePhase = true;
        barrierCurrentHp = barrierMaxHp;
        barrierDestroyed = false;
        portalTransitionTimer = 0f;

        kael = FindAnyObjectByType<KaelAutoController>();
        if (kael == null && PlayerStats.Instance != null)
            kael = PlayerStats.Instance.GetComponent<KaelAutoController>();

        float spawnX = 6f;
        float groundY = -2.95f;
        if (kael != null)
        {
            spawnX = kael.transform.position.x + 5.6f;
            groundY = kael.transform.position.y;
        }

        // Crear la Compuerta de Campo de Fuerza Biomecánico Vorst frente a Kael
        if (barrierObject != null) Destroy(barrierObject);
        barrierObject = new GameObject("Vorst_Perimeter_EnergyGate");
        barrierObject.transform.position = new Vector3(spawnX, groundY - 0.12f, 0f);
        barrierObject.transform.localScale = new Vector3(1.55f, 1.55f, 1f);

        barrierRenderer = barrierObject.AddComponent<SpriteRenderer>();
        barrierRenderer.sprite = Level3MonsterAssetGenerator.GetVorstEnergyBarrierSprite();
        barrierRenderer.sortingOrder = 15;

        if (CompanionDialogueManager.Instance != null)
        {
            CompanionDialogueManager.Instance.ShowCustomMessage(
                "N.E.X.A. // INFILTRACIÓN DE PERÍMETRO",
                "Kael, el Generador de Campo de Fuerza Vorst bloquea el acceso al Nivel 2. ¡Avanza con [D] y destrúyelo con [J] para abrir brecha!",
                new Color(0.20f, 0.95f, 1f),
                6.5f
            );
        }
    }

    void Update()
    {
        if (!isBridgeActive) return;

        float dt = Time.unscaledDeltaTime;
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

        if (!isInPlayablePhase)
        {
            actTimer += dt;
            if (fadeAlpha > 0f)
                fadeAlpha = Mathf.MoveTowards(fadeAlpha, 0f, dt * 1.8f);

            // Avance SOLO manual cuando el jugador presiona Espacio o Enter (nunca se salta solo por tiempo)
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                string fullTxt = GetActNarration(currentAct);
                int chars = Mathf.Clamp(Mathf.FloorToInt(actTimer * 45f), 0, fullTxt.Length);
                if (chars < fullTxt.Length)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                    actTimer = 20f;
                }
                else
                {
                    AdvanceAct();
                }
            }
            return;
        }

        // --- FASE 2: TRAMO JUGABLE DE INFILTRACIÓN ---
        if (kael == null) kael = FindAnyObjectByType<KaelAutoController>();

        if (barrierHitFlashTimer > 0f)
        {
            barrierHitFlashTimer -= Time.deltaTime;
            if (barrierRenderer != null)
                barrierRenderer.color = (barrierHitFlashTimer > 0f) ? new Color(1.4f, 1.4f, 1.4f, 1f) : Color.white;
        }
        else if (barrierRenderer != null && !barrierDestroyed)
        {
            float pulse = 0.92f + 0.08f * Mathf.Sin(Time.time * 7f);
            barrierRenderer.color = new Color(1f, pulse, 1f, 1f);
        }

        // Arcos eléctricos alrededor de la Compuerta Vorst
        if (!barrierDestroyed && barrierObject != null && CombatVFXManager.Instance != null)
        {
            barrierArcTimer -= Time.deltaTime;
            if (barrierArcTimer <= 0f)
            {
                barrierArcTimer = 0.16f;
                CombatVFXManager.Instance.SpawnElectricArc(
                    barrierObject.transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.4f, 2.5f), 0f),
                    new Color(1f, 0.22f, 0.78f, 0.9f)
                );
            }
        }

        if (!barrierDestroyed && barrierObject != null && kael != null)
        {
            float distToBarrier = barrierObject.transform.position.x - kael.transform.position.x;

            // Impedir que Kael atraviese la compuerta antes de romperla
            if (distToBarrier < 1.25f)
            {
                Vector3 pos = kael.transform.position;
                pos.x = barrierObject.transform.position.x - 1.25f;
                kael.transform.position = pos;
            }

            // Detectar golpe manual de Kael con J o K cerca de la compuerta
            if (distToBarrier <= 3.15f && distToBarrier >= -0.8f)
            {
                if (KaelAutoController.WasAttackPressed() || KaelAutoController.WasUltimatePressed())
                {
                    DamagePerimeterBarrier(KaelAutoController.WasUltimatePressed() ? 3f : 1f);
                }
            }
        }
        else if (barrierDestroyed)
        {
            portalTransitionTimer += Time.deltaTime;
            if (CombatVFXManager.Instance != null && barrierObject != null && Random.value < 0.35f)
            {
                CombatVFXManager.Instance.SpawnHitSparks(
                    barrierObject.transform.position + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.4f, 2.3f), 0f),
                    true
                );
            }

            bool crossedPortal = (kael != null && barrierObject != null && kael.transform.position.x >= barrierObject.transform.position.x - 0.25f);
            if (crossedPortal || portalTransitionTimer >= 2.2f)
            {
                fadeOutAlpha = Mathf.MoveTowards(fadeOutAlpha, 1f, Time.deltaTime * 1.4f);
                if (fadeOutAlpha >= 0.99f)
                {
                    CompleteBridgeAndLoadLevel2();
                }
            }
        }
    }

    public void DamagePerimeterBarrier(float dmg)
    {
        if (barrierDestroyed || barrierObject == null) return;

        barrierCurrentHp = Mathf.Max(0f, barrierCurrentHp - dmg);
        barrierHitFlashTimer = 0.14f;

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnHitSparks(barrierObject.transform.position + Vector3.up * 1.35f, true, 10);
            CombatVFXManager.Instance.SpawnShockwaveRing(barrierObject.transform.position + Vector3.up * 1.2f, new Color(1f, 0.25f, 0.72f), 2.4f);
            CombatVFXManager.Instance.SpawnFloatingText(
                barrierObject.transform.position + Vector3.up * 2.5f,
                $"💥 ¡IMPACTO AL GENERADOR! ({Mathf.CeilToInt(barrierCurrentHp)}/{Mathf.CeilToInt(barrierMaxHp)})",
                new Color(1f, 0.85f, 0.25f),
                20,
                0.85f
            );
        }

        CameraFollow2D cam = FindAnyObjectByType<CameraFollow2D>();
        if (cam != null) cam.TriggerShake(0.18f, 0.22f);

        if (barrierCurrentHp <= 0f)
        {
            barrierDestroyed = true;
            if (barrierRenderer != null) barrierRenderer.enabled = false;

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.TriggerScreenFlash(new Color(0.25f, 0.95f, 1f), 0.32f);
                CombatVFXManager.Instance.SpawnEnemyDeathBurst(barrierObject.transform.position + Vector3.up * 1.3f, true);
                CombatVFXManager.Instance.SpawnFloatingText(
                    barrierObject.transform.position + Vector3.up * 2.2f,
                    "⚡ ¡COMPUERTA VORST DESTRUIDA! AVANZA CON [D] ⚡",
                    new Color(0.25f, 1f, 0.60f),
                    24,
                    1.6f
                );
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayHeavyHitSfx();
        }
    }

    public void AdvanceAct()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        currentAct++;
        actTimer = 0f;
        fadeAlpha = 0.85f;
        if (currentAct >= 4)
        {
            BeginPhase2PlayableInfiltration();
        }
    }

    private void CompleteBridgeAndLoadLevel2()
    {
        isBridgeActive = false;
        isInPlayablePhase = false;
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.SaveProgressForNextLevel();
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene("Lab_Zahir");
    }

    private string GetActTitle(int act)
    {
        bool isCorrupt = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);
        switch (act)
        {
            case 0: return "ACTO I — LA CAÍDA DEL SECTOR 7";
            case 1: return "ACTO II — LA TORRE DE RAYO TRACTOR Y LA CÁPSULA DE LÍA";
            case 2: return isCorrupt ? "ACTO III — RESONANCIA VORST (DISONANCIA)" : "ACTO III — RESONANCIA SOLAR (PUREZA NEXO)";
            default: return "ACTO IV — EL PERÍMETRO DE LA BASE VORST (NIVEL 2)";
        }
    }

    private string GetActSpeaker(int act)
    {
        switch (act)
        {
            case 0: return "KAEL";
            case 1: return "N.E.X.A. [ALERTA ESTRATOSFÉRICA]";
            case 2: return "KAEL & N.E.X.A.";
            default: return "N.E.X.A. [INFILTRACIÓN]";
        }
    }

    private string GetActNarration(int act)
    {
        bool isCorrupt = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);
        switch (act)
        {
            case 0:
                return "Entre el fuego y los restos humeantes del Comandante Vorst derrotado, aseguramos las ruinas del Sector 7. Pero la tierra vuelve a temblar: la verdadera fortaleza alienígena acaba de activar sus reactores en el horizonte.";
            case 1:
                return "«¡Kael, mira hacia la estratósfera! La Torre de Convergencia de la Base Vorst está disparando un rayo tractor masivo hacia la Nave Núcleo... ¡La cápsula criogénica de tu hermana Lía va dentro de ese haz!»";
            case 2:
                return isCorrupt
                    ? "El Guantelete Nexo devora sin filtro la energía residual del Comandante. Venas de plasma púrpura y carmesí recorren mi brazo derecho, otorgándome una potencia crítica devastadora mientras N.E.X.A. advierte del peligro."
                    : "El Guantelete Nexo purifica el núcleo del Comandante en una onda cian cristalina y dorada. Mi sincronización con N.E.X.A. alcanza un nuevo nivel, restaurando mis signos vitales para el asalto a la fortaleza.";
            default:
                return "«Hemos llegado a las compuertas blindadas de la Base Vorst (Nivel 2), pero un Generador de Escudo Perimetral bloquea la entrada. ¡Avanza y destrúyelo con el Guantelete Nexo para infiltrarnos!»";
        }
    }

    private Rect ScaleRect(float x, float y, float w, float h)
    {
        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;
        return new Rect(Mathf.Round(x * sx), Mathf.Round(y * sy), Mathf.Round(w * sx), Mathf.Round(h * sy));
    }

    private int ScaleFont(int baseSize)
    {
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        return Mathf.Max(10, Mathf.RoundToInt(baseSize * scale));
    }

    void OnGUI()
    {
        if (!isBridgeActive) return;
        GUI.matrix = Matrix4x4.identity;

        if (!isInPlayablePhase)
        {
            DrawPhase1CinematicScreen();
        }
        else
        {
            DrawPhase2PlayableHUD();
        }
    }

    private void DrawPhase1CinematicScreen()
    {
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawFloatingSparks();

        int idx = Mathf.Clamp(currentAct, 0, 3);
        Texture2D slide = actTextures[idx];
        if (slide != null)
        {
            float zoom = 1.0f + Mathf.Clamp01(actTimer / 10f) * 0.06f;
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
            GUI.color = new Color(0.22f, 0.88f, 1f, 0.95f);
            GUI.DrawTexture(dialogBox, Texture2D.whiteTexture);
            GUI.color = new Color(0.03f, 0.06f, 0.12f, 0.96f);
            GUI.DrawTexture(new Rect(dialogBox.x + 2f, dialogBox.y + 2f, dialogBox.width - 4f, dialogBox.height - 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        Texture2D port = (idx == 0) ? portKaelShout : portNexaNormal;
        float textStartX = 195f;
        float textWidth = 1260f;

        if (port != null)
        {
            Rect pFrame = ScaleRect(178, 833, 200, 200);
            GUI.color = (idx == 0) ? new Color(1f, 0.82f, 0.22f, 0.96f) : new Color(0.22f, 0.92f, 1f, 0.96f);
            GUI.DrawTexture(pFrame, Texture2D.whiteTexture);
            Rect pRect = new Rect(pFrame.x + 2f, pFrame.y + 2f, pFrame.width - 4f, pFrame.height - 4f);
            GUI.color = new Color(0.04f, 0.07f, 0.14f, 1f);
            GUI.DrawTexture(pRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(pRect, port, ScaleMode.ScaleToFit);

            textStartX = 398f;
            textWidth = 1065f;
        }

        DrawStyledText(
            ScaleRect(textStartX, 835, textWidth, 34),
            $"{GetActTitle(idx)}   —   <color=#ffd35c>{GetActSpeaker(idx)}</color>",
            20,
            TextAnchor.MiddleLeft,
            new Color(0.22f, 0.92f, 1f)
        );

        string fullTxt = GetActNarration(idx);
        int chars = Mathf.Clamp(Mathf.FloorToInt(actTimer * 45f), 0, fullTxt.Length);
        DrawStyledText(ScaleRect(textStartX, 875, textWidth, 145), fullTxt.Substring(0, chars), 21, TextAnchor.UpperLeft, Color.white, true);

        string nextLabel = (idx < 3) ? $"SIGUIENTE ESCENA ({idx + 1}/4) ▶" : "⚔ ¡ASALTAR PERÍMETRO! ▶";
        if (DrawCyberButton(0, new Rect(1485, 855, 255, 78), nextLabel, btnCyanTex, new Color(0.22f, 0.92f, 1f), true))
        {
            if (chars < fullTxt.Length)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                actTimer = 20f;
            }
            else
            {
                AdvanceAct();
            }
        }

        if (DrawCyberButton(1, new Rect(1485, 948, 255, 60), "SALTAR CINEMÁTICA ⏭", btnPurpleTex, new Color(0.65f, 0.75f, 0.88f), false))
        {
            BeginPhase2PlayableInfiltration();
        }

        if (fadeAlpha > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    private void DrawPhase2PlayableHUD()
    {
        // 1. Barra de integridad en el mundo sobre la Compuerta Vorst
        if (!barrierDestroyed && barrierObject != null && Camera.main != null)
        {
            Vector3 worldTop = barrierObject.transform.position + new Vector3(0f, 3.1f, 0f);
            Vector3 sp = Camera.main.WorldToScreenPoint(worldTop);
            if (sp.z > 0f)
            {
                float sx = Screen.width / 1920f;
                float sy = Screen.height / 1080f;
                float bw = 220f * sx;
                float bh = 18f * sy;
                float bx = sp.x - bw * 0.5f;
                float by = (Screen.height - sp.y) - bh * 0.5f;

                GUI.color = new Color(0.02f, 0.04f, 0.10f, 0.95f);
                GUI.DrawTexture(new Rect(bx - 3f, by - 26f * sy, bw + 6f, bh + 30f * sy), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.25f, 0.72f, 0.95f);
                GUI.DrawTexture(new Rect(bx - 2f, by - 2f, bw + 4f, bh + 4f), Texture2D.whiteTexture);
                GUI.color = new Color(0.12f, 0.04f, 0.16f, 1f);
                GUI.DrawTexture(new Rect(bx, by, bw, bh), Texture2D.whiteTexture);

                float ratio = Mathf.Clamp01(barrierCurrentHp / Mathf.Max(1f, barrierMaxHp));
                GUI.color = new Color(1f, 0.20f, 0.62f, 1f);
                GUI.DrawTexture(new Rect(bx, by, bw * ratio, bh), Texture2D.whiteTexture);
                GUI.color = Color.white;

                DrawStyledText(
                    new Rect(bx - 20f * sx, by - 24f * sy, bw + 40f * sx, 22f * sy),
                    $"⚡ COMPUERTA LÁSER VORST [{Mathf.CeilToInt(barrierCurrentHp)}/{Mathf.CeilToInt(barrierMaxHp)}]",
                    13,
                    TextAnchor.MiddleCenter,
                    new Color(1f, 0.85f, 0.28f)
                );
            }
        }

        // 2. Banner superior táctico de infiltración
        Rect topBanner = ScaleRect(440f, 102f, 1040f, 84f);
        if (panelCyanTex != null)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.96f);
            GUI.DrawTexture(topBanner, panelCyanTex, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = new Color(0.02f, 0.04f, 0.11f, 0.94f);
            GUI.DrawTexture(topBanner, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (!barrierDestroyed)
        {
            DrawStyledText(
                ScaleRect(460f, 110f, 1000f, 32f),
                "⚡ TRAMO JUGABLE DE INFILTRACIÓN: DESTRUYE LA COMPUERTA DE ESCUDO VORST ⚡",
                18,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.24f)
            );
            DrawStyledText(
                ScaleRect(460f, 144f, 1000f, 30f),
                $"Camina hacia la derecha con [D] y golpea el Generador Vorst con [J] ({Mathf.CeilToInt(barrierCurrentHp)}/{Mathf.CeilToInt(barrierMaxHp)} Integridad)",
                16,
                TextAnchor.MiddleCenter,
                Color.white
            );
        }
        else
        {
            DrawStyledText(
                ScaleRect(460f, 122f, 1000f, 42f),
                "✓ ¡COMPUERTA DESTRUIDA! AVANZA CON [D] PARA ENTRAR AL NIVEL 2 (BASE VORST)",
                19,
                TextAnchor.MiddleCenter,
                new Color(0.25f, 1f, 0.58f)
            );
        }

        if (fadeOutAlpha > 0.01f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeOutAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    private void DrawFloatingSparks()
    {
        if (sparks == null) return;
        float t = Time.unscaledTime;
        for (int i = 0; i < sparks.Length; i++)
        {
            BridgeSpark sp = sparks[i];
            float x = sp.pos.x + Mathf.Sin(t * sp.swayFreq + sp.phase) * sp.swayAmp;
            float y = sp.pos.y;
            float alpha = 0.35f + Mathf.Abs(Mathf.Sin(t * 2.2f + sp.phase)) * 0.55f;
            Color c = sp.isGold ? new Color(1f, 0.84f, 0.24f, alpha) : new Color(0.22f, 0.92f, 1f, alpha);
            Rect r = ScaleRect(x, y, sp.size, sp.size);
            GUI.color = new Color(c.r, c.g, c.b, alpha * 0.28f);
            GUI.DrawTexture(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), Texture2D.whiteTexture);
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    private bool DrawCyberButton(int btnIndex, Rect baseRect, string label, Texture2D skin, Color border, bool pulseGlow)
    {
        Rect checkRect = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool hovered = checkRect.Contains(Event.current.mousePosition);
        if (btnIndex >= 0 && btnIndex < btnHoverAnim.Length)
        {
            btnHoverAnim[btnIndex] = Mathf.MoveTowards(btnHoverAnim[btnIndex], hovered ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        }
        float hVal = (btnIndex >= 0 && btnIndex < btnHoverAnim.Length) ? btnHoverAnim[btnIndex] : (hovered ? 1f : 0f);
        Rect r = ScaleRect(baseRect.x - hVal * 6f, baseRect.y - hVal * 2f, baseRect.width + hVal * 10f, baseRect.height + hVal * 4f);
        float pulse = pulseGlow ? (0.85f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4.5f)) * 0.15f) : 1f;

        if (skin != null)
        {
            float br = hovered ? 1.22f : pulse;
            GUI.color = new Color(br, br, br, 1f);
            GUI.DrawTexture(r, skin, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = hovered ? Color.white : border;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(0.06f, 0.14f, 0.24f, 0.96f);
            GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        DrawStyledText(r, label, 18, TextAnchor.MiddleCenter, hovered ? new Color(1f, 0.96f, 0.82f) : Color.white);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    private void DrawStyledText(Rect r, string txt, int baseSize, TextAnchor align, Color col, bool wordWrap = true)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            fontSize = ScaleFont(baseSize),
            fontStyle = FontStyle.Bold,
            alignment = align,
            wordWrap = wordWrap
        };

        string shadowTxt = System.Text.RegularExpressions.Regex.Replace(txt, "<color=[^>]+>|</color>", "");
        st.normal.textColor = new Color(0f, 0f, 0f, 0.92f);
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), shadowTxt, st);

        st.normal.textColor = col;
        GUI.Label(r, txt, st);
    }
}
