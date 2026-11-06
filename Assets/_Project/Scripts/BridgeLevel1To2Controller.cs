using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// BLOQUE 3 (Issue #18): Cinemática Puente Interactiva y Jugable entre el Nivel 1 (Lab_Cesar)
/// y el Nivel 2 (Lab_Zahir).
/// - Fase 1: Secuencia Narrativa Animada de 4 Actos con arte procedural HD, paneo, partículas y N.E.X.A.
/// - Fase 2: Tramo Jugable de Infiltración en el Perímetro donde el jugador mueve a Kael (A/D),
///   destruye la Barrera de Energía Vorst con (J) y cruza el umbral hacia Lab_Zahir.
/// </summary>
public class BridgeLevel1To2Controller : MonoBehaviour
{
    public static BridgeLevel1To2Controller Instance { get; private set; }
    public static bool launchBridgeOnSceneLoad = false;

    public bool isBridgeActive = false;
    public bool isInPlayablePhase = false;

    private int currentAct = 0;
    private float actTimer = 0f;
    private const float ACT_DURATION = 5.4f;

    // Texturas generadas en memoria para los 4 actos cinemáticos
    private Texture2D[] actTextures = new Texture2D[4];
    private Texture2D texNexaPortrait;

    // Fase 2: Barrera de Energía Vorst destructible en el mundo 2D
    private GameObject barrierObject;
    private SpriteRenderer barrierRenderer;
    private float barrierMaxHp = 3f;
    private float barrierCurrentHp = 3f;
    private float barrierHitFlashTimer = 0f;
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
    }

    public void BeginPhase1Narrative()
    {
        isBridgeActive = true;
        isInPlayablePhase = false;
        currentAct = 0;
        actTimer = 0f;
        barrierDestroyed = false;
        fadeOutAlpha = 0f;

        // Ocultar modales previos y detener spawner del Nivel 1
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.isLevelCompleted = false;
            PlayerStats.Instance.isChoosingDilemma = false;
            PlayerStats.Instance.isPaused = false;
            PlayerStats.Instance.isUpgradeTreeOpen = false;
        }

        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
        }

        // Eliminar restos de enemigos si hubiera alguno
        EnemyDummy[] enemies = FindObjectsByType<EnemyDummy>();
        foreach (EnemyDummy e in enemies)
        {
            if (e != null) Destroy(e.gameObject);
        }

        GenerateBridgeActArtwork();
        texNexaPortrait = Resources.Load<Texture2D>("StoryUI/Portrait_NEXA");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayVictoryAndDilemmaMusic();

        Time.timeScale = 1f;
    }

    private void BeginPhase2PlayableInfiltration()
    {
        isInPlayablePhase = true;
        barrierCurrentHp = barrierMaxHp;
        barrierDestroyed = false;
        portalTransitionTimer = 0f;

        kael = FindAnyObjectByType<KaelAutoController>();
        if (kael == null && PlayerStats.Instance != null)
            kael = PlayerStats.Instance.GetComponent<KaelAutoController>();

        float spawnX = 6f;
        float groundY = -2.75f;
        if (kael != null)
        {
            spawnX = kael.transform.position.x + 5.2f;
            groundY = kael.transform.position.y;
        }

        // Crear la Barrera de Energía Vorst del Perímetro frente a Kael
        if (barrierObject != null) Destroy(barrierObject);
        barrierObject = new GameObject("Vorst_Perimeter_EnergyBarrier");
        barrierObject.transform.position = new Vector3(spawnX, groundY - 0.1f, 0f);
        barrierObject.transform.localScale = new Vector3(1.45f, 1.55f, 1f);

        barrierRenderer = barrierObject.AddComponent<SpriteRenderer>();
        barrierRenderer.sprite = Level3MonsterAssetGenerator.GetVorstEnergyBarrierSprite();
        barrierRenderer.sortingOrder = 15;

        if (CompanionDialogueManager.Instance != null)
        {
            CompanionDialogueManager.Instance.ShowCustomMessage(
                "N.E.X.A. // INFILTRACIÓN DE PERÍMETRO",
                "Kael, detecto la bio-señal de Lía dentro del reactor del Nivel 2. ¡Avanza con [A/D] y golpea con [J] la Barrera de Energía Vorst para infiltrarnos!",
                new Color(0.20f, 0.95f, 1f),
                6.5f
            );
        }
    }

    void Update()
    {
        if (!isBridgeActive) return;

        if (!isInPlayablePhase)
        {
            actTimer += Time.unscaledDeltaTime;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                AdvanceAct();
            }
            else if (actTimer >= ACT_DURATION)
            {
                AdvanceAct();
            }
            return;
        }

        // --- FASE 2: TRAMO JUGABLE DE INFILTRACIÓN ---
        if (kael == null) kael = FindAnyObjectByType<KaelAutoController>();

        if (barrierHitFlashTimer > 0f)
        {
            barrierHitFlashTimer -= Time.deltaTime;
            if (barrierRenderer != null)
                barrierRenderer.color = (barrierHitFlashTimer > 0f) ? Color.white : new Color(1f, 0.35f, 0.75f, 0.95f);
        }
        else if (barrierRenderer != null && !barrierDestroyed)
        {
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 8f);
            barrierRenderer.color = new Color(1f, 0.25f * pulse, 0.65f, 0.92f);
        }

        if (!barrierDestroyed && barrierObject != null && kael != null)
        {
            float distToBarrier = barrierObject.transform.position.x - kael.transform.position.x;

            // Impedir que Kael atraviese la barrera antes de romperla
            if (distToBarrier < 1.15f)
            {
                Vector3 pos = kael.transform.position;
                pos.x = barrierObject.transform.position.x - 1.15f;
                kael.transform.position = pos;
            }

            // Detectar golpe manual de Kael con J o K cerca de la barrera
            if (distToBarrier <= 2.95f && distToBarrier >= -0.8f)
            {
                if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Q))
                {
                    DamagePerimeterBarrier(Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Q) ? 3f : 1f);
                }
            }
        }
        else if (barrierDestroyed)
        {
            portalTransitionTimer += Time.deltaTime;
            // Generar partículas de portal en el umbral destruido
            if (CombatVFXManager.Instance != null && barrierObject != null && Random.value < 0.35f)
            {
                CombatVFXManager.Instance.SpawnHitSparks(
                    barrierObject.transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.4f, 2.2f), 0f),
                    true
                );
            }

            bool crossedPortal = (kael != null && barrierObject != null && kael.transform.position.x >= barrierObject.transform.position.x - 0.3f);
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
            CombatVFXManager.Instance.SpawnHitSparks(barrierObject.transform.position + Vector3.up * 1.2f, true);
            CombatVFXManager.Instance.SpawnShockwaveRing(barrierObject.transform.position + Vector3.up * 1.0f, new Color(1f, 0.25f, 0.65f), 2.2f);
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
                CombatVFXManager.Instance.SpawnEnemyDeathBurst(barrierObject.transform.position + Vector3.up * 1.2f, true);
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayHeavyHitSfx();

            if (CompanionDialogueManager.Instance != null)
            {
                CompanionDialogueManager.Instance.ShowCustomMessage(
                    "N.E.X.A. // BARRERA DESTRUIDA",
                    "¡Barrera perimetral colapsada! Cruza el umbral hacia la derecha [D] para entrar en la Fortaleza Vorst (Nivel 2).",
                    new Color(0.25f, 1f, 0.55f),
                    4.0f
                );
            }
        }
    }

    private void AdvanceAct()
    {
        actTimer = 0f;
        currentAct++;
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

    private void GenerateBridgeActArtwork()
    {
        bool isCorrupt = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);

        for (int act = 0; act < 4; act++)
        {
            int w = 512, h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < h; y++)
            {
                float ty = y / (float)h;
                for (int x = 0; x < w; x++)
                {
                    float tx = x / (float)w;
                    Color c = new Color(0.04f, 0.02f, 0.09f, 1f);

                    if (act == 0)
                    {
                        // Acto 1: Humo y chispas en las ruinas del Sector 7 tras caer el Comandante Vorst
                        c = Color.Lerp(new Color(0.05f, 0.08f, 0.16f, 1f), new Color(0.25f, 0.08f, 0.14f, 1f), 1f - ty);
                        if (ty < 0.28f) c = new Color(0.08f, 0.09f, 0.14f, 1f);
                        float dKael = Vector2.Distance(new Vector2(tx, ty), new Vector2(0.38f, 0.36f));
                        if (dKael < 0.09f) c = new Color(0.15f, 0.92f, 1f, 1f);
                    }
                    else if (act == 1)
                    {
                        // Acto 2: Torre de Rayo Tractor elevando la cápsula criogénica de Lía hacia la estratósfera
                        c = Color.Lerp(new Color(0.04f, 0.01f, 0.10f, 1f), new Color(0.18f, 0.04f, 0.28f, 1f), ty);
                        float beamDist = Mathf.Abs(tx - 0.52f);
                        if (beamDist < 0.045f)
                        {
                            c = Color.Lerp(new Color(1f, 0.20f, 0.68f, 1f), Color.white, 1f - beamDist / 0.045f);
                        }
                        // Cápsula de Lía ascendiendo en el haz
                        if (Vector2.Distance(new Vector2(tx, ty), new Vector2(0.52f, 0.68f)) < 0.045f)
                        {
                            c = new Color(0.30f, 0.98f, 1f, 1f);
                        }
                    }
                    else if (act == 2)
                    {
                        // Acto 3: Primer plano del Guantelete Nexo mutando según Compasión o Rigor
                        Color moralCore = isCorrupt ? new Color(1f, 0.18f, 0.42f, 1f) : new Color(0.18f, 0.96f, 1f, 1f);
                        Color moralOuter = isCorrupt ? new Color(0.58f, 0.08f, 0.92f, 1f) : new Color(1f, 0.86f, 0.32f, 1f);
                        float dCenter = Vector2.Distance(new Vector2(tx, ty), new Vector2(0.5f, 0.5f));
                        float ring = Mathf.Clamp01(1f - Mathf.Abs(dCenter - 0.22f) * 18f);
                        c = Color.Lerp(new Color(0.03f, 0.04f, 0.10f, 1f), moralOuter, Mathf.Clamp01(1f - dCenter * 2.1f));
                        if (dCenter < 0.11f || ring > 0.5f) c = Color.Lerp(c, moralCore, 0.85f);
                    }
                    else
                    {
                        // Acto 4: Compuertas blindadas y Barrera de Energía Vorst del Nivel 2
                        c = Color.Lerp(new Color(0.05f, 0.02f, 0.12f, 1f), new Color(0.14f, 0.05f, 0.24f, 1f), 1f - ty);
                        if (Mathf.Abs(tx - 0.68f) < 0.035f && ty > 0.15f && ty < 0.88f)
                        {
                            c = new Color(1f, 0.22f, 0.58f, 1f);
                        }
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            actTextures[act] = tex;
        }
    }

    void OnGUI()
    {
        if (!isBridgeActive) return;

        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;

        if (!isInPlayablePhase)
        {
            DrawPhase1NarrativeGUI(sx, sy);
        }
        else
        {
            DrawPhase2PlayableHUD(sx, sy);
        }
    }

    private void DrawPhase1NarrativeGUI(float sx, float sy)
    {
        GUI.color = new Color(0.02f, 0.02f, 0.05f, 0.96f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        bool isCorrupt = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);

        string[] actTitles = {
            "ACTO I — LA CAÍDA DEL SECTOR 7",
            "ACTO II — LA TORRE DE RAYO TRACTOR Y LA CÁPSULA DE LÍA",
            isCorrupt
                ? "ACTO III — RESONANCIA DEL GUANTELETE: DISONANCIA VORST"
                : "ACTO III — RESONANCIA DEL GUANTELETE: PUREZA NEXO",
            "ACTO IV — PERÍMETRO DE LA FORTALEZA BIOMECÁNICA (NIVEL 2)"
        };

        string[] actSubtitles = {
            "Entre el humo y las chispas del Comandante Vorst derrotado, Kael emerge victorioso mientras las defensas urbanas del Sector 7 terminan de colapsar.",
            "En el horizonte se alza la Fortaleza Biomecánica Vorst, disparando un colosal haz de tracción púrpura hacia la estratósfera donde asciende la cápsula criogénica de su hermana Lía.",
            isCorrupt
                ? "El Guantelete Nexo absorbe sin filtro el núcleo del Comandante. Venas carmesí y púrpura abisal sobrecargan la armadura de Kael con un poder agresivo y letal."
                : "El Guantelete Nexo purifica el núcleo del Comandante en una onda cian cristalina y dorada solar, fortaleciendo el vínculo simbiótico entre Kael y N.E.X.A.",
            "N.E.X.A.: «Kael, detecto la bio-señal de Lía dentro del reactor del Nivel 2. ¡Rompe la barrera de contención perimetral para infiltrarnos en la Base Vorst!»"
        };

        int idx = Mathf.Clamp(currentAct, 0, 3);

        // Marco principal de la cinemática puente
        Rect frameRect = new Rect(210f * sx, 90f * sy, 1500f * sx, 640f * sy);
        GUI.color = new Color(0.18f, 0.85f, 1f, 0.9f);
        GUI.DrawTexture(new Rect(frameRect.x - 3f, frameRect.y - 3f, frameRect.width + 6f, frameRect.height + 6f), Texture2D.whiteTexture);
        GUI.color = new Color(0.03f, 0.04f, 0.09f, 1f);
        GUI.DrawTexture(frameRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (actTextures[idx] != null)
        {
            // Efecto Ken Burns (paneo/zoom suave)
            float zoom = 1f + (actTimer / ACT_DURATION) * 0.06f;
            Rect uv = new Rect((1f - 1f / zoom) * 0.5f, (1f - 1f / zoom) * 0.5f, 1f / zoom, 1f / zoom);
            GUI.DrawTextureWithTexCoords(frameRect, actTextures[idx], uv);
        }

        // Caja inferior de diálogo narrativo
        Rect dialogBox = new Rect(210f * sx, 755f * sy, 1500f * sx, 195f * sy);
        GUI.color = new Color(0.03f, 0.05f, 0.12f, 0.95f);
        GUI.DrawTexture(dialogBox, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(14, Mathf.RoundToInt(24f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        titleSt.normal.textColor = new Color(0.22f, 0.92f, 1f);
        GUI.Label(new Rect(245f * sx, 770f * sy, 1420f * sx, 38f * sy), actTitles[idx], titleSt);

        GUIStyle bodySt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(12, Mathf.RoundToInt(19f * sy)),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        bodySt.normal.textColor = Color.white;
        GUI.Label(new Rect(245f * sx, 816f * sy, 1420f * sx, 95f * sy), actSubtitles[idx], bodySt);

        // Botones de control
        if (GUI.Button(new Rect(1190f * sx, 972f * sy, 250f * sx, 52f * sy), "SIGUIENTE ACTO [ESPACIO]"))
        {
            AdvanceAct();
        }
        if (GUI.Button(new Rect(1460f * sx, 972f * sy, 250f * sx, 52f * sy), "IR AL TRAMO JUGABLE ⏭"))
        {
            BeginPhase2PlayableInfiltration();
        }
    }

    private void DrawPhase2PlayableHUD(float sx, float sy)
    {
        // Banner superior táctico de infiltración
        Rect topBanner = new Rect(460f * sx, 108f * sy, 1000f * sx, 88f * sy);
        GUI.color = new Color(0.02f, 0.04f, 0.11f, 0.92f);
        GUI.DrawTexture(topBanner, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.25f, 0.62f, 0.9f);
        GUI.DrawTexture(new Rect(topBanner.x, topBanner.y, topBanner.width, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(12, Mathf.RoundToInt(18f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        if (!barrierDestroyed)
        {
            st.normal.textColor = new Color(1f, 0.45f, 0.72f);
            GUI.Label(new Rect(480f * sx, 114f * sy, 960f * sx, 34f * sy),
                "⚡ TRAMO JUGABLE DE TRANSICIÓN: ROMPE LA BARRERA PERIMETRAL VORST ⚡", st);

            st.normal.textColor = Color.white;
            GUI.Label(new Rect(480f * sx, 148f * sy, 960f * sx, 34f * sy),
                $"Camina hacia la derecha con [D] y ataca la Barrera con [J] ({Mathf.CeilToInt(barrierCurrentHp)}/{Mathf.CeilToInt(barrierMaxHp)} Integridad)", st);
        }
        else
        {
            st.normal.textColor = new Color(0.25f, 1f, 0.55f);
            GUI.Label(new Rect(480f * sx, 126f * sy, 960f * sx, 44f * sy),
                "✓ ¡BARRERA DESTRUIDA! AVANZA CON [D] PARA INGRESAR AL NIVEL 2 (BASE VORST)", st);
        }

        if (fadeOutAlpha > 0.01f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeOutAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
