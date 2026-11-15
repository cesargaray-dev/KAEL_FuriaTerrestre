using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Constructor automático del escenario definitivo 04_Final_NexusThroneroom ("El Trono de la Disonancia"):
/// - Ventanal panorámico espacial con la Tierra en órbita y costillas arquitectónicas del domo.
/// - Suelo de obsidiana biomecánica reflectante con runas Vorst pulsantes.
/// - Cápsula cilíndrica interactiva de bio-conversión de Lía conectada con rayos violetas al Trono alienígena.
/// - Cenizas energéticas flotantes y anillos de distorsión gravitacional.
/// </summary>
public class NexusThroneroomEnvironment : MonoBehaviour
{
    public static NexusThroneroomEnvironment Instance { get; private set; }

    private Transform camTransform;
    private Transform kaelTransform;

    private Transform sanctumGroup;
    private Transform liaCapsuleTransform;
    private Transform throneTransform;
    private SpriteRenderer shieldPulseRenderer;
    private readonly List<LineRenderer> throneConduits = new List<LineRenderer>();
    private readonly List<Transform> gravRings = new List<Transform>();

    private float nextCapsuleSyncTime = 0f;
    private float ashSpawnTimer = 0f;
    private float gravSpawnTimer = 0f;
    private float sanctumAnchorX = 0f;
    private bool isLiaLiberated = false;
    private bool isPhase3Siphoning = false;
    public bool IsBossArenaLocked { get; private set; } = false;
    public float ArenaMinX => sanctumAnchorX - 7.8f;
    public float ArenaMaxX => sanctumAnchorX + 7.8f;

    private LineRenderer leftArenaBarrier;
    private LineRenderer rightArenaBarrier;
    private SpriteRenderer leftBarrierCurtain;
    private SpriteRenderer rightBarrierCurtain;
    private static Texture2D cachedBarrierCurtainTex;

    private const float CorridorModuleWidth = 10.24f;

    static float SnapToBackgroundTileX(float worldX)
    {
        return Mathf.Round(worldX / CorridorModuleWidth) * CorridorModuleWidth;
    }

    public float ClampXToBossArena(float x)
    {
        return IsBossArenaLocked ? Mathf.Clamp(x, ArenaMinX, ArenaMaxX) : x;
    }

    public void LockBossArenaAround(float referenceX)
    {
        sanctumAnchorX = SnapToBackgroundTileX(referenceX);
        IsBossArenaLocked = true;

        if (sanctumGroup != null)
        {
            sanctumGroup.gameObject.SetActive(true);
            sanctumGroup.position = new Vector3(sanctumAnchorX, -2.95f, 0f);
        }

        UpdateArenaContainmentBarriers(true);

        if (InfiniteBackground.Instance != null)
        {
            InfiniteBackground.Instance.ActivateLevel4SanctumAt(sanctumAnchorX);
        }
    }

    public void ResetSanctumArena()
    {
        IsBossArenaLocked = false;
        isPhase3Siphoning = false;
        isLiaLiberated = false;
        UpdateArenaContainmentBarriers(false);
        if (sanctumGroup != null)
        {
            sanctumGroup.gameObject.SetActive(false);
        }
        if (InfiniteBackground.Instance != null)
        {
            InfiniteBackground.Instance.ResetLevel4Sanctum();
        }
    }

    void UpdateArenaContainmentBarriers(bool active)
    {
        if (active && sanctumGroup != null && leftArenaBarrier == null)
        {
            leftArenaBarrier = CreateContainmentBarrierLine("LeftArenaBarrier", -7.8f, out leftBarrierCurtain);
            rightArenaBarrier = CreateContainmentBarrierLine("RightArenaBarrier", 7.8f, out rightBarrierCurtain);
        }
        if (leftArenaBarrier != null) leftArenaBarrier.enabled = active;
        if (rightArenaBarrier != null) rightArenaBarrier.enabled = active;
        if (leftBarrierCurtain != null) leftBarrierCurtain.enabled = active;
        if (rightBarrierCurtain != null) rightBarrierCurtain.enabled = active;
    }

    static Texture2D GetBarrierCurtainTexture()
    {
        if (cachedBarrierCurtainTex != null) return cachedBarrierCurtainTex;
        const int w = 32;
        const int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            float vertFade = Mathf.Sin(v * Mathf.PI);
            float scanline = (y % 6 < 2) ? 1.0f : 0.48f;
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                float edge = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 1.6f);
                float a = edge * vertFade * scanline * 0.55f;
                tex.SetPixel(x, y, new Color(0.68f, 0.25f, 1f, a));
            }
        }
        tex.Apply();
        cachedBarrierCurtainTex = tex;
        return cachedBarrierCurtainTex;
    }

    LineRenderer CreateContainmentBarrierLine(string barrierName, float localX, out SpriteRenderer curtainSr)
    {
        GameObject obj = new GameObject(barrierName);
        obj.transform.SetParent(sanctumGroup, false);
        obj.transform.localPosition = new Vector3(localX, 0f, 0f);

        // Cortina energética translúcida con líneas de escaneo
        GameObject curtainObj = new GameObject("EnergyCurtain");
        curtainObj.transform.SetParent(obj.transform, false);
        curtainObj.transform.localPosition = new Vector3(0f, 2.55f, 0f);
        curtainObj.transform.localScale = new Vector3(2.2f, 4.4f, 1f);
        curtainSr = curtainObj.AddComponent<SpriteRenderer>();
        curtainSr.sprite = Sprite.Create(
            GetBarrierCurtainTexture(),
            new Rect(0f, 0f, 32f, 128f),
            new Vector2(0.5f, 0.5f),
            100f
        );
        curtainSr.sortingOrder = 11;
        curtainSr.enabled = false;

        // Emisor de suelo y techo
        Sprite ringSpr = Level3MonsterAssetGenerator.GetPsionicRingSprite();
        for (int p = 0; p < 2; p++)
        {
            GameObject emitter = new GameObject(p == 0 ? "FloorEmitter" : "CeilingEmitter");
            emitter.transform.SetParent(obj.transform, false);
            emitter.transform.localPosition = new Vector3(0f, p == 0 ? 0.06f : 5.05f, 0f);
            emitter.transform.localScale = new Vector3(0.95f, 0.28f, 1f);
            SpriteRenderer esr = emitter.AddComponent<SpriteRenderer>();
            esr.sprite = ringSpr;
            esr.color = new Color(0.28f, 0.94f, 1f, 0.72f);
            esr.sortingOrder = 12;
        }

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = 2;
        lr.startWidth = 0.06f;
        lr.endWidth = 0.06f;
        lr.startColor = new Color(0.28f, 0.94f, 1f, 0.65f);
        lr.endColor = new Color(0.75f, 0.22f, 1f, 0.25f);
        lr.sortingOrder = 12;
        lr.SetPosition(0, new Vector3(0f, -0.1f, 0f));
        lr.SetPosition(1, new Vector3(0f, 5.1f, 0f));
        return lr;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureExistsIfLevel4();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureExistsIfLevel4();
    }

    public static void EnsureExistsIfLevel4()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool isLevel4 = (sceneName == "04_Final_NexusThroneroom") ||
                        (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 4);
        if (isLevel4)
        {
            EnsureExists();
        }
    }

    public static NexusThroneroomEnvironment EnsureExists()
    {
        if (Instance != null) return Instance;
        NexusThroneroomEnvironment existing = FindAnyObjectByType<NexusThroneroomEnvironment>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("NexusThroneroomEnvironment");
        Instance = go.AddComponent<NexusThroneroomEnvironment>();
        return Instance;
    }

    public Vector3 GetCapsuleWorldPosition()
    {
        if (liaCapsuleTransform != null) return liaCapsuleTransform.position;
        return new Vector3(sanctumAnchorX, -1.1f, 0f);
    }

    public void SetPhase3SiphonState(bool active, Vector3 arenaCenterHint)
    {
        isPhase3Siphoning = active;
        if (active && !IsBossArenaLocked)
        {
            LockBossArenaAround(arenaCenterHint.x);
        }
    }

    public void TriggerLiaLiberationVisual(EndingCinematicController.EndingType ending)
    {
        isLiaLiberated = true;
        isPhase3Siphoning = false;

        Color libColor = (ending == EndingCinematicController.EndingType.Oscuro)
            ? new Color(1f, 0.25f, 0.45f, 0.9f)
            : ((ending == EndingCinematicController.EndingType.Neutral)
                ? new Color(1f, 0.85f, 0.28f, 0.9f)
                : new Color(0.28f, 0.96f, 1f, 0.9f));

        foreach (LineRenderer lr in throneConduits)
        {
            if (lr != null)
            {
                lr.startColor = libColor;
                lr.endColor = libColor;
            }
        }
        if (shieldPulseRenderer != null) shieldPulseRenderer.color = libColor;
        if (CombatVFXManager.Instance != null && liaCapsuleTransform != null)
        {
            CombatVFXManager.Instance.SpawnShockwaveRing(liaCapsuleTransform.position, libColor, 4.2f);
            CombatVFXManager.Instance.SpawnHealingParticles(liaCapsuleTransform.position);
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
            Camera.main.backgroundColor = new Color(0.03f, 0.015f, 0.08f, 1f);
        }

        // Mantener activo el nuevo fondo panorámico Pixel Art HD (BG_Throneroom_Level4)
        InfiniteBackground levelBg = FindAnyObjectByType<InfiniteBackground>(FindObjectsInactive.Include);
        if (levelBg != null && !levelBg.gameObject.activeSelf)
        {
            levelBg.gameObject.SetActive(true);
        }
        Level3EnvironmentBuilder oldL3Env = FindAnyObjectByType<Level3EnvironmentBuilder>();
        if (oldL3Env != null)
        {
            oldL3Env.enabled = false;
        }

        if (KaelAutoController.Instance != null)
            kaelTransform = KaelAutoController.Instance.transform;

        EndingsManager.EnsureExists();
        BuildThroneroomVisuals();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void BuildThroneroomVisuals()
    {
        // El arte de fondo (espacio, Tierra en órbita, plataforma obsidiana, trono y cápsula iluminada de Lía)
        // lo dibuja BG_Throneroom_Level4.png. Aquí anclamos el halo interactivo y los conductos exactamente sobre cada baldosa.
        float refX = (kaelTransform != null) ? kaelTransform.position.x : ((camTransform != null) ? camTransform.position.x : 0f);
        sanctumAnchorX = SnapToBackgroundTileX(refX);

        sanctumGroup = new GameObject("SanctumAltarAndLiaCapsule").transform;
        sanctumGroup.SetParent(transform, false);
        sanctumGroup.position = new Vector3(sanctumAnchorX, -2.95f, 0f);

        // Ancla sobre el Trono alienígena (centro de la baldosa, debajo de la cápsula)
        GameObject throneObj = new GameObject("AlienDissonanceThroneAnchor");
        throneObj.transform.SetParent(sanctumGroup, false);
        throneObj.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        throneTransform = throneObj.transform;

        Sprite ringSpr = Level3MonsterAssetGenerator.GetPsionicRingSprite();
        for (int r = 0; r < 2; r++)
        {
            GameObject gr = new GameObject($"ThroneGravRing_{r}");
            gr.transform.SetParent(throneTransform, false);
            gr.transform.localPosition = new Vector3(0f, 0.25f + r * 0.30f, 0f);
            gr.transform.localScale = new Vector3(1.85f + r * 0.55f, 0.42f, 1f);
            SpriteRenderer gsr = gr.AddComponent<SpriteRenderer>();
            gsr.sprite = ringSpr;
            gsr.sortingOrder = -6;
            gsr.color = (r == 0) ? new Color(0.68f, 0.22f, 1f, 0.34f) : new Color(0.20f, 0.92f, 1f, 0.26f);
            gravRings.Add(gr.transform);
        }

        // Ancla interactiva sobre la Cápsula cilíndrica de Lía en BG_Throneroom_Level4
        GameObject capObj = new GameObject("LiaBioConversionCapsuleAnchor");
        capObj.transform.SetParent(sanctumGroup, false);
        capObj.transform.localPosition = new Vector3(0f, 3.45f, 0f);
        liaCapsuleTransform = capObj.transform;

        GameObject pulseGlow = new GameObject("CapsuleSyncHalo");
        pulseGlow.transform.SetParent(capObj.transform, false);
        shieldPulseRenderer = pulseGlow.AddComponent<SpriteRenderer>();
        shieldPulseRenderer.sprite = ringSpr;
        shieldPulseRenderer.sortingOrder = -5;
        shieldPulseRenderer.color = new Color(0.45f, 0.82f, 1f, 0.38f);

        // Rayos violetas sutiles recorriendo los cables superiores sobre la Cápsula de Lía
        Material arcMat = new Material(Shader.Find("Sprites/Default"));
        for (int c = 0; c < 2; c++)
        {
            GameObject conduitObj = new GameObject($"VioletConduit_{c}");
            conduitObj.transform.SetParent(sanctumGroup, false);
            LineRenderer lr = conduitObj.AddComponent<LineRenderer>();
            lr.material = arcMat;
            lr.sortingOrder = -5;
            lr.startWidth = 0.045f;
            lr.endWidth = 0.025f;
            lr.positionCount = 7;
            Color cCol = (c == 0) ? new Color(0.84f, 0.22f, 1f, 0.62f) : new Color(0.24f, 0.94f, 1f, 0.56f);
            lr.startColor = cCol;
            lr.endColor = cCol;
            throneConduits.Add(lr);
        }

        // Oculto durante el corredor de oleadas 1-9; se activa al llegar al Santuario del Trono en Oleada 10
        sanctumGroup.gameObject.SetActive(IsBossArenaLocked);
        UpdateArenaContainmentBarriers(IsBossArenaLocked);
    }

    void RelocateSanctumCenter(float newX, bool withFlash)
    {
        sanctumAnchorX = SnapToBackgroundTileX(newX);
        if (sanctumGroup != null)
        {
            sanctumGroup.position = new Vector3(sanctumAnchorX, -2.95f, 0f);
        }
        if (withFlash && CombatVFXManager.Instance != null && liaCapsuleTransform != null)
        {
            CombatVFXManager.Instance.SpawnShockwaveRing(liaCapsuleTransform.position, new Color(0.75f, 0.22f, 1f), 2.8f);
        }
    }

    void Update()
    {
        if (camTransform == null && Camera.main != null) camTransform = Camera.main.transform;
        if (kaelTransform == null && KaelAutoController.Instance != null) kaelTransform = KaelAutoController.Instance.transform;

        if (PlayerStats.Instance != null && PlayerStats.Instance.IsAnyBlockingModalOpen) return;

        float t = Time.time;

        // Solo reubicar si la arena aún no está fijada al Santuario único del Soberano
        if (kaelTransform != null && !IsBossArenaLocked)
        {
            float snapped = SnapToBackgroundTileX(kaelTransform.position.x);
            if (Mathf.Abs(snapped - sanctumAnchorX) > 1.0f)
            {
                RelocateSanctumCenter(snapped, withFlash: false);
            }
        }

        if (IsBossArenaLocked && kaelTransform != null)
        {
            float basePulse = 0.40f + Mathf.Sin(t * 4.5f) * 0.12f;
            if (leftBarrierCurtain != null)
            {
                float proxL = Mathf.Clamp01(1f - Mathf.Abs(kaelTransform.position.x - ArenaMinX) / 2.2f);
                leftBarrierCurtain.color = new Color(0.55f + proxL * 0.35f, 0.28f + proxL * 0.55f, 1f, basePulse + proxL * 0.42f);
            }
            if (rightBarrierCurtain != null)
            {
                float proxR = Mathf.Clamp01(1f - Mathf.Abs(kaelTransform.position.x - ArenaMaxX) / 2.2f);
                rightBarrierCurtain.color = new Color(0.55f + proxR * 0.35f, 0.28f + proxR * 0.55f, 1f, basePulse + proxR * 0.42f);
            }
        }

        if (shieldPulseRenderer != null)
        {
            float scalePulse = 1.35f + Mathf.Sin(t * 3.5f) * 0.16f;
            shieldPulseRenderer.transform.localScale = new Vector3(scalePulse, scalePulse * 1.45f, 1f);
            shieldPulseRenderer.transform.Rotate(0f, 0f, 34f * Time.deltaTime);
        }
        for (int i = 0; i < gravRings.Count; i++)
        {
            if (gravRings[i] != null)
            {
                float rotDir = (i % 2 == 0) ? 45f : -62f;
                gravRings[i].Rotate(0f, 0f, rotDir * Time.deltaTime);
                float bob = Mathf.Sin(t * 2.5f + i) * 0.08f;
                Vector3 lp = gravRings[i].localPosition;
                gravRings[i].localPosition = new Vector3(lp.x, (0.25f + i * 0.30f) + bob, lp.z);
            }
        }

        // Animar destellos eléctricos sobre los conductos acoplados a la cápsula de Lía
        if (liaCapsuleTransform != null && throneTransform != null)
        {
            Vector3 capTop = liaCapsuleTransform.position + new Vector3(0f, 0.95f, 0f);
            for (int c = 0; c < throneConduits.Count; c++)
            {
                LineRenderer lr = throneConduits[c];
                if (lr == null) continue;
                Vector3 capUpperEnd = capTop + new Vector3((c == 0) ? -1.45f : 1.45f, 1.55f, 0f);
                int count = lr.positionCount;
                for (int k = 0; k < count; k++)
                {
                    float u = k / (float)(count - 1);
                    Vector3 basePt = Vector3.Lerp(capTop, capUpperEnd, u);
                    float curveX = Mathf.Sin(u * Mathf.PI) * ((c == 0) ? -0.22f : 0.22f);
                    float jitterY = Mathf.Sin(t * (7.5f + c * 2.2f) + k * 1.9f) * 0.10f;
                    lr.SetPosition(k, basePt + new Vector3(curveX, jitterY, 0f));
                }
            }
        }

        // Partículas de cenizas energéticas y distorsiones gravitacionales en el escenario
        ashSpawnTimer -= Time.deltaTime;
        if (ashSpawnTimer <= 0f && CombatVFXManager.Instance != null && camTransform != null)
        {
            ashSpawnTimer = 0.085f;
            Vector3 ashPos = new Vector3(
                camTransform.position.x + Random.Range(-10f, 10f),
                Random.Range(-2.85f, 3.2f),
                0f
            );
            Color ashCol = (Random.value > 0.42f)
                ? new Color(0.84f, 0.22f, 1f, 0.72f)
                : ((Random.value > 0.5f) ? new Color(1f, 0.35f, 0.52f, 0.65f) : new Color(0.25f, 0.92f, 1f, 0.68f));
            CombatVFXManager.Instance.SpawnFloatingRuneOrSpore(ashPos, ashCol);
        }

        gravSpawnTimer -= Time.deltaTime;
        if (gravSpawnTimer <= 0f && CombatVFXManager.Instance != null && liaCapsuleTransform != null)
        {
            gravSpawnTimer = 2.6f;
            CombatVFXManager.Instance.SpawnShockwaveRing(
                liaCapsuleTransform.position + Vector3.up * Random.Range(-0.4f, 0.8f),
                new Color(0.68f, 0.20f, 1f, 0.35f),
                1.9f
            );
        }

        // Interacción manual con la cápsula de Lía mediante tecla [E] cuando Kael está cerca
        if (!isLiaLiberated && IsKaelNearCapsule() && Time.time >= nextCapsuleSyncTime && WasInteractPressed())
        {
            TriggerCapsuleInteraction();
        }
    }

    public bool IsKaelNearCapsule()
    {
        if (kaelTransform == null || liaCapsuleTransform == null) return false;
        return Mathf.Abs(kaelTransform.position.x - liaCapsuleTransform.position.x) <= 3.6f;
    }

    public void TriggerCapsuleInteraction()
    {
        if (isLiaLiberated || Time.time < nextCapsuleSyncTime) return;
        nextCapsuleSyncTime = Time.time + 16f;

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.HealPercent(0.15f);
            PlayerStats.Instance.AddUltCharge(25f);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        if (CombatVFXManager.Instance != null && liaCapsuleTransform != null)
        {
            Color cCyan = new Color(0.26f, 0.96f, 1f);
            CombatVFXManager.Instance.SpawnShockwaveRing(liaCapsuleTransform.position, cCyan, 3.2f);
            CombatVFXManager.Instance.SpawnHealingParticles(kaelTransform != null ? kaelTransform.position : liaCapsuleTransform.position);
            CombatVFXManager.Instance.SpawnFloatingText(
                liaCapsuleTransform.position + Vector3.up * 1.9f,
                "💙 LÍA: «¡Resiste, Kael! Siento tu luz en el Guantelete...» (+15% HP / +25% ULT)",
                cCyan,
                18,
                2.0f
            );
        }
    }

    static bool WasInteractPressed()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
        try { return Input.GetKeyDown(KeyCode.E); }
        catch { return false; }
    }

    void OnGUI()
    {
        if (!IsBossArenaLocked || isLiaLiberated || Camera.main == null || liaCapsuleTransform == null) return;
        if (PlayerStats.Instance != null && PlayerStats.Instance.IsAnyBlockingModalOpen) return;
        if (EndingCinematicController.Instance != null && EndingCinematicController.Instance.isEndingActive) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(liaCapsuleTransform.position + Vector3.up * 1.95f);
        if (screenPos.z <= 0f) return;

        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;
        bool near = IsKaelNearCapsule();
        bool ready = Time.time >= nextCapsuleSyncTime;

        float boxW = (near ? 340f : 270f) * sx;
        float boxH = (near && ready ? 52f : 32f) * sy;
        Rect r = new Rect(screenPos.x - boxW * 0.5f, (Screen.height - screenPos.y) - boxH, boxW, boxH);
        if (r.xMax < 0 || r.x > Screen.width || r.yMax < 0 || r.y > Screen.height) return;

        GUI.color = new Color(0.04f, 0.02f, 0.12f, near ? 0.90f : 0.72f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = isPhase3Siphoning ? new Color(1f, 0.25f, 0.55f, 0.95f) : new Color(0.70f, 0.35f, 1f, 0.90f);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle st = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(10, Mathf.RoundToInt(13f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        st.normal.textColor = isPhase3Siphoning ? new Color(1f, 0.55f, 0.72f) : new Color(0.65f, 0.95f, 1f);

        string label = near
            ? (ready ? "💠 CÁPSULA DE LÍA — PULSA [E] O TOCA PARA SINCRONIZAR NEXO" : $"💠 CÁPSULA DE LÍA — RECARGA ({Mathf.CeilToInt(nextCapsuleSyncTime - Time.time)}s)")
            : "💠 CÁPSULA DE BIO-CONVERSIÓN: LÍA VANE";
        GUI.Label(r, label, st);

        if (near && ready && GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            TriggerCapsuleInteraction();
        }
    }

}

/// <summary>
/// Controlador avanzado para los jefes del Nivel Final (04_Final_NexusThroneroom):
/// 1) PraetorianCustodian (Oleada 5 Mini-Jefe: Custodio Pretoriano Vorst):
///    - Gladiador biomecánico con lanza de energía y salto con impacto en picada
///      telegrafiado con círculo de advertencia en el suelo.
/// 2) SupremeSovereign (Oleada 10 Jefe Final en 3 Fases: Arconte Supremo / Soberano Vorst):
///    - Fase 1 (100%-66% HP - Duelo Marcial): Combate veloz con estoque de plasma,
///      teletransportación lateral y ráfagas cortantes en arco.
///    - Fase 2 (66%-33% HP - Juicio Psiónico): Levita en el domo y desata pilares orbitales
///      de plasma violeta telegrafiados en el suelo para esquivar o hacer Parry Perfecto (S).
///    - Fase 3 (33%-0% HP - Fusión con el Núcleo): Absorbe energía de la cápsula de Lía con un
///      Campo de Fuerza protector rompible conectando combos x30 (Sobremarcha) y disparando la
///      Habilidad Ultimate "Sobrecarga Nexo". Al llegar a 0 HP enlaza con EndingsManager.cs.
/// </summary>
public class SovereignBossAI : MonoBehaviour
{
    public enum BossRole
    {
        PraetorianCustodian,
        SupremeSovereign
    }

    public BossRole role = BossRole.SupremeSovereign;
    public int currentPhase = 1;
    public bool isForceFieldActive { get; private set; } = false;
    public bool forceFieldWasBrokenOnce { get; private set; } = false;

    public bool OverridesMovementAndAttacks => true;
    public bool IsActingExternally => isActingExternally;

    private EnemyDummy enemy;
    private SpriteRenderer sr;
    private Transform kaelTarget;

    private bool isActingExternally = false;
    private bool hasTriggeredPhase2 = false;
    private bool hasTriggeredPhase3 = false;
    private bool defeatTriggered = false;

    private float nextSpecialCooldown = 2.0f;
    private float nextPrimaryCooldown = 0.8f;
    private float nextSummonCooldown = 11.0f;
    private float forceFieldReapplyCooldown = 0f;
    private float forceFieldElapsed = 0f;

    // Escudo de Fase 3, rayo de sifón multicapa y animaciones Pixel Art HD del jefe
    private SpriteRenderer shieldDomeRenderer;
    private LineRenderer siphonOuterBeam;
    private LineRenderer siphonMidBeam;
    private LineRenderer capsuleSiphonBeam;
    private SpriteRenderer siphonCapHalo;
    private SpriteRenderer siphonBossHalo;
    private float siphonOrbTimer = 0f;
    private static Texture2D cachedBeamGlowTex;
    private int specialPatternCounter = 0;

    private Sprite[] animAttackFrames;
    private Sprite[] animEnrageFrames;
    private Sprite[] animJumpTakeoffFrames;
    private Sprite[] animJumpDiveFrames;
    private Sprite[] animJumpLandFrames;
    private Sprite[] animPhaseTransFrames;
    private Sprite[] animVortexFlameFrames;
    private Sprite[] animChestOpenFrames;
    private Sprite[] animLaserFrames;
    private Sprite[] animDieFrames;

    public void Configure(BossRole targetRole)
    {
        role = targetRole;
        enemy = GetComponent<EnemyDummy>();
        sr = GetComponent<SpriteRenderer>();
        LocateKael();

        if (enemy != null)
        {
            if (role == BossRole.PraetorianCustodian)
            {
                enemy.isMiniBoss = true;
                enemy.isBoss = false;
                if (enemy.VisualTypePublic != "PraetorianCustodian")
                {
                    enemy.SetupLevel3Visuals("PraetorianCustodian");
                }
            }
            else
            {
                enemy.isBoss = true;
                enemy.isMiniBoss = false;
                if (enemy.VisualTypePublic != "SupremeSovereign")
                {
                    enemy.SetupLevel3Visuals("SupremeSovereign");
                }
            }
        }

        if (sr != null) sr.color = Color.white;
        LoadBossSpriteSequences();

        if (role == BossRole.PraetorianCustodian)
        {
            nextSpecialCooldown = Time.time + 3.2f;
        }
        else
        {
            NexusThroneroomEnvironment env = NexusThroneroomEnvironment.EnsureExists();
            if (env != null && !env.IsBossArenaLocked)
            {
                float hintX = (kaelTarget != null) ? (kaelTarget.position.x + 4.5f) : transform.position.x;
                env.LockBossArenaAround(hintX);
            }
            if (shieldDomeRenderer == null) AttachForceFieldDome();
            if (capsuleSiphonBeam == null) AttachSiphonBeam();
            nextSpecialCooldown = Time.time + 2.6f;
            nextSummonCooldown = Time.time + 10.5f;
        }
    }

    void Awake()
    {
        enemy = GetComponent<EnemyDummy>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        LocateKael();
        if (animAttackFrames == null)
        {
            Configure(role);
        }
    }

    void LoadBossSpriteSequences()
    {
        string prefix = (role == BossRole.PraetorianCustodian) ? "PraetorianCustodian" : "SupremeSovereign";
        animAttackFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(prefix, "Attack");
        animDieFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(prefix, "Die");

        if (role == BossRole.PraetorianCustodian)
        {
            animJumpTakeoffFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(prefix, "JumpTakeoff");
            animJumpDiveFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(prefix, "JumpDive");
            animJumpLandFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(prefix, "JumpLand");
        }
        else
        {
            animEnrageFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(prefix, "EnrageAttack");
            animPhaseTransFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(prefix, "PhaseTrans");
            animVortexFlameFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(prefix, "VortexFlame");
            animChestOpenFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(prefix, "ChestOpen");
            animLaserFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(prefix, "Laser");
        }
    }

    void ApplyBossFrame(Sprite[] frames, int index)
    {
        if (sr == null || frames == null || frames.Length == 0) return;
        int clamped = Mathf.Clamp(index, 0, frames.Length - 1);
        if (frames[clamped] != null) sr.sprite = frames[clamped];
    }

    void ApplyBossFrameProgress(Sprite[] frames, float progress)
    {
        if (frames == null || frames.Length == 0) return;
        int idx = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progress) * frames.Length), 0, frames.Length - 1);
        ApplyBossFrame(frames, idx);
    }

    void LocateKael()
    {
        if (kaelTarget != null) return;
        if (KaelAutoController.Instance != null)
        {
            kaelTarget = KaelAutoController.Instance.transform;
            return;
        }
        KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
        if (kael != null) kaelTarget = kael.transform;
    }

    /// <summary>
    /// Hook invocado desde EnemyDummy.TakeDamage para gestionar las transiciones de 3 fases y el Campo de Fuerza de Fase 3.
    /// </summary>
    public void ProcessIncomingDamage(ref float dmg, ref bool isCrit)
    {
        if (role != BossRole.SupremeSovereign || enemy == null || defeatTriggered) return;

        float projectedHP = enemy.currentHP - dmg;
        float ratio = projectedHP / Mathf.Max(1f, enemy.maxHP);

        // Transición a Fase 2 (Juicio Psiónico) al 66% de vida
        if (!hasTriggeredPhase2 && ratio <= 0.66f)
        {
            hasTriggeredPhase2 = true;
            currentPhase = 2;
            enemy.currentHP = enemy.maxHP * 0.66f;
            dmg = 0f;
            StartCoroutine(EnterPhase2Routine());
            return;
        }

        // Transición a Fase 3 (Fusión con el Núcleo) al 33% de vida
        if (!hasTriggeredPhase3 && ratio <= 0.33f)
        {
            hasTriggeredPhase3 = true;
            currentPhase = 3;
            enemy.currentHP = enemy.maxHP * 0.33f;
            dmg = 0f;
            StartCoroutine(EnterPhase3Routine());
            return;
        }

        // Mecánica del Campo de Fuerza de Fase 3:
        if (currentPhase == 3 && isForceFieldActive)
        {
            bool kaelCastingUlt = (KaelAutoController.Instance != null && KaelAutoController.Instance.IsCastingUltimate);
            int combo = (PlayerStats.Instance != null) ? PlayerStats.Instance.comboCount : 0;

            // El impacto contra el escudo carga rápido el combo hacia x30 (Sobremarcha) y energía Ultimate
            if (PlayerStats.Instance != null && !kaelCastingUlt)
            {
                PlayerStats.Instance.RegisterComboHit();
                PlayerStats.Instance.RegisterComboHit();
                PlayerStats.Instance.AddUltCharge(15f);
                combo = PlayerStats.Instance.comboCount;
            }

            // ¿Se rompe el Campo de Fuerza? -> Al disparar Ultimate ("Sobrecarga Nexo") con Combo x30 / Sobremarcha
            // (o si ya superó 12s de asedio con Sobremarcha/Ultimate para evitar cualquier bloqueo)
            if (kaelCastingUlt && (combo >= 30 || (PlayerStats.Instance != null && PlayerStats.Instance.isOverdriveActive) || forceFieldElapsed >= 10f))
            {
                BreakForceField();
                isCrit = true;
                dmg *= 1.45f;
                return;
            }

            // Si aún está protegido por el campo de fuerza, mitiga daño y evita muerte antes de romper el escudo
            dmg = Mathf.Max(1f, dmg * 0.14f);
            float minCap = enemy.maxHP * 0.08f;
            if (enemy.currentHP - dmg < minCap)
            {
                dmg = Mathf.Max(0f, enemy.currentHP - minCap);
            }

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 1.0f, new Color(0.76f, 0.25f, 1f, 0.65f), 1.65f);
            }
        }
        else if (currentPhase == 3 && !isForceFieldActive)
        {
            // Escudo quebrado: vulnerabilidad aumentada
            dmg *= 1.35f;
        }
    }

    public void BreakForceField()
    {
        if (!isForceFieldActive) return;
        isForceFieldActive = false;
        forceFieldWasBrokenOnce = true;
        forceFieldReapplyCooldown = 12.5f;

        if (shieldDomeRenderer != null) shieldDomeRenderer.enabled = false;
        SetSiphonVisualsActive(false);
        if (NexusThroneroomEnvironment.Instance != null)
            NexusThroneroomEnvironment.Instance.SetPhase3SiphonState(false, transform.position);

        if (enemy != null) enemy.ApplyStun(3.8f);

        if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.TriggerScreenFlash(new Color(0.30f, 0.96f, 1f), 0.78f);
            CombatVFXManager.Instance.ShakeCamera(0.75f, 0.34f);
            CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 1.0f, Color.cyan, 5.2f);
            CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 1.0f, new Color(1f, 0.85f, 0.25f), 3.4f);
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 2.4f,
                "💥 ¡CAMPO DE FUERZA DESTROZADO POR SOBRECARGA NEXO! ¡VULNERABLE! 💥",
                new Color(0.32f, 0.98f, 1f),
                24,
                2.0f
            );
        }
    }

    public void TriggerFinalDefeatSequence()
    {
        if (defeatTriggered) return;
        defeatTriggered = true;
        StopAllCoroutines();
        isActingExternally = true;
        isForceFieldActive = false;

        if (shieldDomeRenderer != null) shieldDomeRenderer.enabled = false;
        SetSiphonVisualsActive(false);

        // Retirar refuerzos pretorianos restantes al caer el Soberano
        EnemyDummy[] allEnemies = FindObjectsByType<EnemyDummy>(FindObjectsInactive.Exclude);
        foreach (EnemyDummy other in allEnemies)
        {
            if (other != null && other != enemy && !other.IsDead)
            {
                if (CombatVFXManager.Instance != null)
                {
                    CombatVFXManager.Instance.SpawnShockwaveRing(other.transform.position + Vector3.up * 0.6f, new Color(0.8f, 0.25f, 1f), 1.6f);
                }
                Destroy(other.gameObject);
            }
        }

        VorstCodexUI.RegisterEnemyKill(enemy);
        StartCoroutine(FinalBossDefeatAndEndingRoutine());
    }

    IEnumerator FinalBossDefeatAndEndingRoutine()
    {
        Time.timeScale = 0.32f;
        if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayEnemyDie();
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.TriggerScreenFlash(Color.white, 0.88f);
            CombatVFXManager.Instance.ShakeCamera(1.6f, 0.30f);
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 2.2f,
                "★ ¡SOBERANO SUPREMO VORST DERROTADO! ★",
                new Color(1f, 0.88f, 0.25f),
                26,
                2.2f
            );
        }

        Vector3 center = transform.position + Vector3.up * 0.9f;
        for (int b = 0; b < 4; b++)
        {
            ApplyBossFrame(animDieFrames, b * 2);
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnBossExplosionBurst(center + (Vector3)(Random.insideUnitCircle * 0.8f));
                CombatVFXManager.Instance.SpawnShockwaveRing(center, (b % 2 == 0) ? Color.cyan : new Color(0.85f, 0.25f, 1f), 2.8f + b * 0.7f);
            }
            yield return new WaitForSecondsRealtime(0.28f);
        }
        ApplyBossFrame(animDieFrames, 7);

        Time.timeScale = 1f;
        if (PlayerStats.Instance != null && enemy != null)
        {
            PlayerStats.Instance.AddRewards(enemy.goldReward, enemy.expReward);
        }

        EndingsManager.TriggerEndingFromFinalBoss();
    }

    void Update()
    {
        if (defeatTriggered || enemy == null || enemy.IsDead) return;
        if (PlayerStats.Instance != null && PlayerStats.Instance.IsAnyBlockingModalOpen) return;
        if (kaelTarget == null)
        {
            LocateKael();
            if (kaelTarget == null) return;
        }

        // Actualizar efectos de Fase 3 (Campo de Fuerza + Rayo de Sifón hacia Cápsula de Lía)
        if (role == BossRole.SupremeSovereign && currentPhase == 3)
        {
            if (isForceFieldActive)
            {
                forceFieldElapsed += Time.deltaTime;
                if (shieldDomeRenderer != null)
                {
                    shieldDomeRenderer.enabled = true;
                    float s = 2.65f + Mathf.Sin(Time.time * 5.2f) * 0.14f;
                    shieldDomeRenderer.transform.localScale = new Vector3(s, s, 1f);
                    shieldDomeRenderer.transform.Rotate(0f, 0f, 65f * Time.deltaTime);
                    shieldDomeRenderer.color = new Color(0.82f, 0.28f, 1f, 0.52f + Mathf.Sin(Time.time * 7f) * 0.16f);
                }

                if (capsuleSiphonBeam != null && NexusThroneroomEnvironment.Instance != null)
                {
                    UpdateSiphonBeamVisuals();
                }

                // Regeneración muy sutil mientras canaliza a Lía (incentiva romper el escudo pronto)
                enemy.currentHP = Mathf.Min(enemy.maxHP * 0.33f, enemy.currentHP + enemy.maxHP * 0.004f * Time.deltaTime);
            }
            else if (forceFieldReapplyCooldown > 0f)
            {
                forceFieldReapplyCooldown -= Time.deltaTime;
                if (forceFieldReapplyCooldown <= 0f && !enemy.isStunned && !isActingExternally)
                {
                    ActivatePhase3ForceField();
                }
            }
        }

        if (role == BossRole.SupremeSovereign && NexusThroneroomEnvironment.Instance != null && NexusThroneroomEnvironment.Instance.IsBossArenaLocked)
        {
            Vector3 p = transform.position;
            float clampedX = NexusThroneroomEnvironment.Instance.ClampXToBossArena(p.x);
            if (!Mathf.Approximately(p.x, clampedX))
            {
                transform.position = new Vector3(clampedX, p.y, p.z);
            }
        }

        if (enemy.isStunned || isActingExternally) return;

        float diffX = kaelTarget.position.x - transform.position.x;
        float distX = Mathf.Abs(diffX);
        float dirToKael = (Mathf.Abs(diffX) > 0.02f) ? Mathf.Sign(diffX) : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        if (role == BossRole.PraetorianCustodian)
        {
            UpdatePraetorianCustodian(distX, dirToKael);
        }
        else
        {
            UpdateSupremeSovereign(distX, dirToKael);
        }
    }

    // =========================================================================
    // IA OLEADA 5 MINI-JEFE: CUSTODIO PRETORIANO VORST
    // =========================================================================
    void UpdatePraetorianCustodian(float distX, float dirToKael)
    {
        // Salto con impacto en picada telegrafiado con círculo de advertencia en el suelo
        if (Time.time >= nextSpecialCooldown && distX <= 9.5f)
        {
            StartCoroutine(PraetorianDivingJumpRoutine());
            return;
        }

        if (distX > enemy.attackRange)
        {
            transform.Translate(new Vector2(dirToKael, 0f) * (enemy.moveSpeed * Time.deltaTime), Space.World);
        }
        else if (Time.time >= nextPrimaryCooldown)
        {
            StartCoroutine(PraetorianLanceThrustRoutine());
        }
    }

    IEnumerator PraetorianLanceThrustRoutine()
    {
        isActingExternally = true;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > transform.position.x) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        // Telegrafiado breve de la lanza con cuadro de impulso
        ApplyBossFrame(animAttackFrames, 1);
        if (sr != null) sr.color = new Color(1f, 0.85f, 0.55f);
        yield return new WaitForSeconds(0.12f);
        ApplyBossFrame(animAttackFrames, 2);
        yield return new WaitForSeconds(0.12f);
        if (defeatTriggered || enemy.IsDead || enemy.isStunned) { EndAction(); yield break; }

        if (sr != null) sr.color = Color.white;
        Vector3 startPos = transform.position;
        Vector3 lungePos = startPos + new Vector3(dirToKael * 1.15f, 0f, 0f);

        float t = 0f;
        while (t < 0.14f)
        {
            transform.position = Vector3.Lerp(startPos, lungePos, t / 0.14f);
            ApplyBossFrameProgress(animAttackFrames, 0.38f + (t / 0.14f) * 0.30f);
            t += Time.deltaTime;
            yield return null;
        }
        ApplyBossFrame(animAttackFrames, 4);

        if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayEnemyAttack();
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnHitSparks(transform.position + new Vector3(dirToKael * 1.2f, 0.7f, 0f), true, 8);
        }

        if (kaelTarget != null && Mathf.Abs(kaelTarget.position.x - transform.position.x) <= enemy.attackRange + 0.9f)
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.TakeDamage(enemy.damage, enemy);
        }

        yield return new WaitForSeconds(0.10f);
        ApplyBossFrame(animAttackFrames, 6);
        yield return new WaitForSeconds(0.10f);
        ApplyBossFrame(animAttackFrames, 7);
        nextPrimaryCooldown = Time.time + (1.0f / Mathf.Max(0.4f, enemy.attackRate));
        isActingExternally = false;
    }

    IEnumerator PraetorianDivingJumpRoutine()
    {
        isActingExternally = true;
        float groundY = -2.95f;
        Vector3 startPos = new Vector3(transform.position.x, groundY, transform.position.z);
        float impactX = (kaelTarget != null) ? kaelTarget.position.x : (startPos.x - 3.5f);
        Vector3 targetGroundPos = new Vector3(impactX, groundY, 0f);
        if (sr != null) sr.flipX = (impactX > startPos.x);

        // 1. Círculo de advertencia telegrafiado en el suelo
        GameObject warningCircle = CreateGroundWarningCircle(targetGroundPos, 2.05f, new Color(1f, 0.22f, 0.42f, 0.92f));
        if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 2.1f,
                "⚠ ¡SALTO EN PICADA PRETORIANO! [ESQUIVA O PARRY S]",
                new Color(1f, 0.38f, 0.25f),
                21,
                1.3f
            );
        }

        float telegraphDur = 0.80f;
        float elapsed = 0f;
        while (elapsed < telegraphDur)
        {
            if (defeatTriggered || enemy.IsDead || enemy.isStunned)
            {
                if (warningCircle != null) Destroy(warningCircle);
                transform.position = new Vector3(transform.position.x, groundY, 0f);
                nextSpecialCooldown = Time.time + 3.2f;
                EndAction();
                yield break;
            }
            float p = elapsed / telegraphDur;
            ApplyBossFrameProgress(animJumpTakeoffFrames, p * 0.5f);
            UpdateGroundWarningCircle(warningCircle, p);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. Ascenso y caída en picada hacia el círculo telegrafiado
        float flightDur = 0.72f;
        float fTime = 0f;
        float apex = 4.6f;
        while (fTime < flightDur)
        {
            if (defeatTriggered || enemy.IsDead)
            {
                if (warningCircle != null) Destroy(warningCircle);
                EndAction();
                yield break;
            }
            float u = Mathf.Clamp01(fTime / flightDur);
            float curX = Mathf.Lerp(startPos.x, impactX, u);
            float curY = groundY + 4f * apex * u * (1f - u);
            transform.position = new Vector3(curX, curY, 0f);

            if (u < 0.45f)
            {
                ApplyBossFrameProgress(animJumpTakeoffFrames, 0.5f + (u / 0.45f) * 0.5f);
            }
            else
            {
                ApplyBossFrameProgress(animJumpDiveFrames, (u - 0.45f) / 0.55f);
            }

            if (CombatVFXManager.Instance != null && sr != null && sr.sprite != null && Random.value < 0.35f)
            {
                CombatVFXManager.Instance.SpawnAfterimageGhost(sr.sprite, transform.position, transform.localScale, sr.flipX, new Color(1f, 0.45f, 0.25f, 0.55f));
            }

            fTime += Time.deltaTime;
            yield return null;
        }

        if (warningCircle != null) Destroy(warningCircle);
        transform.position = targetGroundPos;
        ApplyBossFrame(animJumpLandFrames, 1);

        // 3. Impacto sísmico con Lanza de Energía
        if (AudioManager.Instance != null) AudioManager.Instance.PlayHeavyHitSfx();
        else if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayEnemyAttack();
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.TriggerScreenFlash(new Color(1f, 0.25f, 0.18f), 0.55f);
            CombatVFXManager.Instance.ShakeCamera(0.45f, 0.25f);
            CombatVFXManager.Instance.SpawnShockwaveRing(targetGroundPos + Vector3.up * 0.3f, new Color(1f, 0.35f, 0.15f), 4.2f);
            CombatVFXManager.Instance.SpawnEnergyPillar(targetGroundPos, new Color(0.95f, 0.25f, 0.45f));
            CombatVFXManager.Instance.SpawnHitSparks(targetGroundPos + Vector3.up * 0.4f, true, 16);
        }

        if (kaelTarget != null && Mathf.Abs(kaelTarget.position.x - impactX) <= 2.35f)
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.TakeDamage(enemy.damage * 1.85f, enemy);
        }

        float landT = 0f;
        while (landT < 0.45f)
        {
            ApplyBossFrameProgress(animJumpLandFrames, landT / 0.45f);
            landT += Time.deltaTime;
            yield return null;
        }
        nextSpecialCooldown = Time.time + 4.8f;
        nextPrimaryCooldown = Time.time + 0.6f;
        isActingExternally = false;
    }

    // =========================================================================
    // IA OLEADA 10 JEFE FINAL (3 FASES): SOBERANO SUPREMO VORST
    // =========================================================================
    void UpdateSupremeSovereign(float distX, float dirToKael)
    {
        if (currentPhase >= 2 && Time.time >= nextSummonCooldown && CountActiveMinions() < 2)
        {
            StartCoroutine(SovereignSummonTroopsRoutine());
            return;
        }

        if (currentPhase == 1)
        {
            // FASE 1 (Duelo Marcial): Teletransportación lateral + ráfagas cortantes en arco + estocadas rápidas
            if (Time.time >= nextSpecialCooldown)
            {
                StartCoroutine(SovereignTeleportAndArcSlashRoutine());
                return;
            }
            if (distX > enemy.attackRange)
            {
                transform.Translate(new Vector2(dirToKael, 0f) * (enemy.moveSpeed * 1.2f * Time.deltaTime), Space.World);
            }
            else if (Time.time >= nextPrimaryCooldown)
            {
                StartCoroutine(SovereignPlasmaEstocRoutine());
            }
        }
        else if (currentPhase == 2)
        {
            // FASE 2 (Juicio Psiónico): Levitación en el domo + Pilares orbitales de plasma violeta en el suelo
            float hoverY = -2.95f + 1.35f + Mathf.Sin(Time.time * 2.8f) * 0.22f;
            transform.position = new Vector3(transform.position.x, hoverY, 0f);

            if (Time.time >= nextSpecialCooldown)
            {
                StartCoroutine(SovereignOrbitalPillarsRoutine());
                return;
            }
            if (distX > enemy.attackRange + 0.8f)
            {
                transform.Translate(new Vector2(dirToKael, 0f) * (enemy.moveSpeed * Time.deltaTime), Space.World);
            }
            else if (Time.time >= nextPrimaryCooldown)
            {
                StartCoroutine(SovereignTeleportAndArcSlashRoutine());
            }
        }
        else
        {
            // FASE 3 (Fusión con el Núcleo): Pilares orbitales + Rayo Láser de Pecho sin corte + estocadas furiosas
            float targetY = isForceFieldActive ? (-2.95f + 0.45f + Mathf.Sin(Time.time * 3.5f) * 0.12f) : -2.95f;
            transform.position = new Vector3(transform.position.x, targetY, 0f);

            if (Time.time >= nextSpecialCooldown)
            {
                specialPatternCounter++;
                if (specialPatternCounter % 2 == 1)
                    StartCoroutine(SovereignChestLaserRoutine());
                else
                    StartCoroutine(SovereignOrbitalPillarsRoutine());
                return;
            }
            if (distX > enemy.attackRange)
            {
                transform.Translate(new Vector2(dirToKael, 0f) * (enemy.moveSpeed * 1.15f * Time.deltaTime), Space.World);
            }
            else if (Time.time >= nextPrimaryCooldown)
            {
                StartCoroutine(SovereignPlasmaEstocRoutine());
            }
        }
    }

    IEnumerator SovereignPlasmaEstocRoutine()
    {
        isActingExternally = true;
        float groundY = (currentPhase == 2) ? transform.position.y : -2.95f;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > transform.position.x) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        Sprite[] swingSet = (currentPhase == 3 && animEnrageFrames != null && animEnrageFrames.Length > 0)
            ? animEnrageFrames
            : animAttackFrames;

        // Destello de estoque previo para que el jugador pueda leer y hacer Parry (S)
        ApplyBossFrame(swingSet, 0);
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnHitSparks(transform.position + new Vector3(dirToKael * 0.9f, 0.85f, 0f), false, 5);
        }
        yield return new WaitForSeconds(0.12f);
        ApplyBossFrame(swingSet, 1);
        yield return new WaitForSeconds(0.12f);
        if (defeatTriggered || enemy.IsDead || enemy.isStunned) { EndAction(); yield break; }

        // Doble estocada rápida
        for (int s = 0; s < 2; s++)
        {
            if (defeatTriggered || enemy.IsDead || enemy.isStunned) { EndAction(); yield break; }

            Vector3 dashFrom = transform.position;
            Vector3 dashTo = new Vector3(dashFrom.x + dirToKael * 1.1f, groundY, 0f);
            float t = 0f;
            while (t < 0.09f)
            {
                transform.position = Vector3.Lerp(dashFrom, dashTo, t / 0.09f);
                ApplyBossFrame(swingSet, (s == 0) ? 2 : 5);
                t += Time.deltaTime;
                yield return null;
            }
            ApplyBossFrame(swingSet, (s == 0) ? 3 : 6);

            if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayEnemyAttack();
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + new Vector3(dirToKael * 0.9f, 0.7f, 0f), new Color(0.84f, 0.25f, 1f), 1.6f);
            }
            if (kaelTarget != null && Mathf.Abs(kaelTarget.position.x - transform.position.x) <= enemy.attackRange + 0.85f)
            {
                if (PlayerStats.Instance != null) PlayerStats.Instance.TakeDamage(enemy.damage * 0.82f, enemy);
            }
            yield return new WaitForSeconds(0.14f);
        }
        ApplyBossFrame(swingSet, 7);

        nextPrimaryCooldown = Time.time + (currentPhase == 3 ? 0.85f : 1.05f);
        isActingExternally = false;
    }

    IEnumerator SovereignTeleportAndArcSlashRoutine()
    {
        isActingExternally = true;
        float groundY = (currentPhase == 2) ? (-2.95f + 1.35f) : -2.95f;

        ApplyBossFrame(animAttackFrames, 0);
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 0.8f, new Color(0.75f, 0.20f, 1f), 2.1f);
            CombatVFXManager.Instance.SpawnAfterimageGhost(sr != null ? sr.sprite : null, transform.position, transform.localScale, sr != null && sr.flipX, new Color(0.78f, 0.25f, 1f, 0.7f));
        }

        // Desvanecimiento cuántico lateral al otro lado de Kael
        if (sr != null) sr.color = new Color(0.75f, 0.25f, 1f, 0.22f);
        yield return new WaitForSeconds(0.18f);
        if (defeatTriggered || enemy.IsDead) { EndAction(); yield break; }

        if (kaelTarget != null)
        {
            float side = (transform.position.x >= kaelTarget.position.x) ? -1f : 1f;
            float newX = kaelTarget.position.x + side * Random.Range(2.8f, 3.8f);
            if (NexusThroneroomEnvironment.Instance != null)
            {
                newX = NexusThroneroomEnvironment.Instance.ClampXToBossArena(newX);
            }
            transform.position = new Vector3(newX, groundY, 0f);
        }

        float dirToKael = (kaelTarget != null && kaelTarget.position.x > transform.position.x) ? 1f : -1f;
        if (sr != null)
        {
            sr.flipX = (dirToKael > 0f);
            sr.color = Color.white;
        }
        ApplyBossFrame(animEnrageFrames ?? animAttackFrames, 1);

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 2.1f, "⚡ ¡RÁFAGA CORTANTE EN ARCO!", new Color(0.86f, 0.35f, 1f), 19, 0.85f);
        }

        yield return new WaitForSeconds(0.24f);
        if (defeatTriggered || enemy.IsDead || enemy.isStunned) { EndAction(); yield break; }

        // Disparar 2 ondas cortantes en arco hacia Kael
        for (int w = 0; w < 2; w++)
        {
            ApplyBossFrame(animEnrageFrames ?? animAttackFrames, 3 + w * 2);
            SpawnArcSlashWave(transform.position + new Vector3(dirToKael * 0.85f, 0.75f, 0f), dirToKael, enemy.damage * 0.90f);
            yield return new WaitForSeconds(0.20f);
        }

        if (sr != null) sr.color = Color.white;
        nextSpecialCooldown = Time.time + (currentPhase == 1 ? 3.8f : 4.2f);
        nextPrimaryCooldown = Time.time + 0.55f;
        isActingExternally = false;
    }

    IEnumerator EnterPhase2Routine()
    {
        isActingExternally = true;
        if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();

        // Levitar hacia el centro/frente de la arena en el domo sin quitarle control manual a Kael
        float centerX = (kaelTarget != null) ? kaelTarget.position.x + 3.6f : transform.position.x;
        if (NexusThroneroomEnvironment.Instance != null)
        {
            centerX = NexusThroneroomEnvironment.Instance.ClampXToBossArena(centerX);
        }
        float startX = transform.position.x;
        float startY = transform.position.y;
        float endY = -2.95f + 1.35f;

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.TriggerBossWarning(
                "⚡ FASE II: JUICIO PSIÓNICO DEL SOBERANO ⚡",
                "PILARES ORBITALES EN EL SUELO — ¡ESQUIVA [A/D] O HAZ PARRY PERFECTO [S]!",
                2.8f
            );
            CombatVFXManager.Instance.TriggerScreenFlash(new Color(0.75f, 0.22f, 1f), 0.75f);
            CombatVFXManager.Instance.ShakeCamera(0.45f, 0.24f);
        }

        float t = 0f;
        while (t < 0.75f)
        {
            if (defeatTriggered || enemy.IsDead) { EndAction(); yield break; }
            float u = Mathf.Clamp01(t / 0.75f);
            ApplyBossFrameProgress(animPhaseTransFrames, u);
            transform.position = new Vector3(Mathf.Lerp(startX, centerX, u), Mathf.Lerp(startY, endY, u), 0f);
            t += Time.deltaTime;
            yield return null;
        }

        nextSpecialCooldown = Time.time + 1.0f;
        isActingExternally = false;
    }

    IEnumerator SovereignOrbitalPillarsRoutine()
    {
        isActingExternally = true;
        float groundY = -2.95f;
        float centerKael = (kaelTarget != null) ? kaelTarget.position.x : transform.position.x - 3f;

        int pillarCount = (currentPhase == 3) ? 4 : 3;
        List<Vector3> strikePositions = new List<Vector3>(pillarCount);
        List<GameObject> warnings = new List<GameObject>(pillarCount);

        // Colocar pilares orbitales: uno centrado en Kael y otros flanqueando el suelo del domo
        strikePositions.Add(new Vector3(centerKael, groundY, 0f));
        for (int i = 1; i < pillarCount; i++)
        {
            float off = (i % 2 == 1 ? 1f : -1f) * (2.2f + i * 1.1f);
            strikePositions.Add(new Vector3(centerKael + off, groundY, 0f));
        }

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 2.15f,
                "⚡ ¡JUICIO PSIÓNICO: PILARES ORBITALES! [ESQUIVA O PARRY S]",
                new Color(0.88f, 0.28f, 1f),
                20,
                1.2f
            );
        }

        for (int i = 0; i < strikePositions.Count; i++)
        {
            warnings.Add(CreateGroundWarningCircle(strikePositions[i], 1.25f, new Color(0.84f, 0.22f, 1f, 0.92f)));
        }

        float chargeDur = 0.85f;
        float elapsed = 0f;
        while (elapsed < chargeDur)
        {
            if (defeatTriggered || enemy.IsDead)
            {
                foreach (GameObject w in warnings) if (w != null) Destroy(w);
                EndAction();
                yield break;
            }
            float p = elapsed / chargeDur;
            ApplyBossFrameProgress(animVortexFlameFrames, p * 0.62f);
            foreach (GameObject w in warnings)
            {
                UpdateGroundWarningCircle(w, p);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (GameObject w in warnings) if (w != null) Destroy(w);
        ApplyBossFrame(animVortexFlameFrames, 5);

        if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();
        if (CombatVFXManager.Instance != null) CombatVFXManager.Instance.ShakeCamera(0.45f, 0.22f);

        bool hitKael = false;
        for (int i = 0; i < strikePositions.Count; i++)
        {
            Vector3 pPos = strikePositions[i];
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnEnergyPillar(pPos, new Color(0.84f, 0.20f, 1f));
                CombatVFXManager.Instance.SpawnShockwaveRing(pPos + Vector3.up * 0.2f, new Color(0.32f, 0.95f, 1f), 2.5f);
            }
            if (!hitKael && kaelTarget != null && Mathf.Abs(kaelTarget.position.x - pPos.x) <= 1.30f)
            {
                hitKael = true;
                if (PlayerStats.Instance != null) PlayerStats.Instance.TakeDamage(enemy.damage * 1.35f, enemy);
            }
        }

        yield return new WaitForSeconds(0.25f);
        ApplyBossFrame(animVortexFlameFrames, 7);
        yield return new WaitForSeconds(0.25f);
        nextSpecialCooldown = Time.time + (currentPhase == 2 ? 3.4f : 4.2f);
        isActingExternally = false;
    }

    IEnumerator SovereignChestLaserRoutine()
    {
        isActingExternally = true;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > transform.position.x) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 2.25f,
                "⚡ ¡RAYO DEL NÚCLEO SOBERANO! [ESQUIVA O PARRY S]",
                new Color(0.92f, 0.28f, 1f),
                22,
                1.1f
            );
        }

        Vector3 chestPos = transform.position + Vector3.up * 1.08f + Vector3.right * (dirToKael * 0.45f);
        for (int f = 0; f < 2; f++)
        {
            if (defeatTriggered || enemy.IsDead || enemy.isStunned) { EndAction(); yield break; }
            ApplyBossFrame(animLaserFrames, f);
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnElectricArc(chestPos, new Color(0.86f, 0.25f, 1f));
                CombatVFXManager.Instance.SpawnHitSparks(chestPos, false, 4);
            }
            yield return new WaitForSeconds(0.24f);
        }

        if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayUltimateLaser();
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnShockwaveRing(chestPos, new Color(0.88f, 0.24f, 1f), 2.8f);
            CombatVFXManager.Instance.ShakeCamera(0.32f, 0.20f);
        }

        float beamReach = 4.35f;
        for (int f = 2; f < 6; f++)
        {
            if (defeatTriggered || enemy.IsDead || enemy.isStunned) { EndAction(); yield break; }
            ApplyBossFrame(animLaserFrames, f);
            if (f == 3 && kaelTarget != null)
            {
                float diffX = kaelTarget.position.x - transform.position.x;
                bool isInFront = (dirToKael > 0f && diffX >= -0.25f) || (dirToKael < 0f && diffX <= 0.25f);
                if (isInFront && Mathf.Abs(diffX) <= beamReach && PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.TakeDamage(enemy.damage * 1.45f, enemy);
                    if (CombatVFXManager.Instance != null)
                        CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.6f, true, 12);
                }
            }
            yield return new WaitForSeconds(0.13f);
        }

        ApplyBossFrame(animLaserFrames, 6);
        yield return new WaitForSeconds(0.12f);
        ApplyBossFrame(animLaserFrames, 7);
        yield return new WaitForSeconds(0.12f);

        nextSpecialCooldown = Time.time + 3.8f;
        nextPrimaryCooldown = Time.time + 0.55f;
        isActingExternally = false;
    }

    IEnumerator EnterPhase3Routine()
    {
        isActingExternally = true;
        ActivatePhase3ForceField();

        if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.TriggerBossWarning(
                "☣ FASE III: FUSIÓN CON EL NÚCLEO DE LÍA ☣",
                "¡CONECTA COMBO x30 (SOBREMARCHA) Y DISPARA LA ULTIMATE [K] PARA QUEBRAR SU ESCUDO!",
                3.4f
            );
            CombatVFXManager.Instance.TriggerScreenFlash(new Color(0.88f, 0.18f, 1f), 0.85f);
            CombatVFXManager.Instance.ShakeCamera(0.60f, 0.28f);
        }

        float t = 0f;
        while (t < 0.75f)
        {
            if (defeatTriggered || enemy.IsDead) { EndAction(); yield break; }
            ApplyBossFrameProgress(animChestOpenFrames, t / 0.75f);
            t += Time.deltaTime;
            yield return null;
        }
        nextSpecialCooldown = Time.time + 2.4f;
        isActingExternally = false;
    }

    int CountActiveMinions()
    {
        int count = 0;
        EnemyDummy[] all = FindObjectsByType<EnemyDummy>(FindObjectsInactive.Exclude);
        foreach (EnemyDummy e in all)
        {
            if (e != null && e != enemy && !e.IsDead) count++;
        }
        return count;
    }

    IEnumerator SovereignSummonTroopsRoutine()
    {
        isActingExternally = true;
        if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 2.3f,
                "⚔ ¡GUARDIA PRETORIANA, PROTEGED EL NÚCLEO! ⚔",
                new Color(0.92f, 0.35f, 1f),
                21,
                1.5f
            );
            CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 0.9f, new Color(0.84f, 0.24f, 1f), 3.4f);
        }

        float t = 0f;
        while (t < 0.55f)
        {
            if (defeatTriggered || enemy.IsDead) { EndAction(); yield break; }
            ApplyBossFrameProgress(animPhaseTransFrames, t / 0.55f);
            t += Time.deltaTime;
            yield return null;
        }

        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.SpawnSovereignReinforcements(transform.position, 2);
        }

        nextSummonCooldown = Time.time + 15.0f;
        nextSpecialCooldown = Mathf.Max(nextSpecialCooldown, Time.time + 1.4f);
        isActingExternally = false;
    }

    void ActivatePhase3ForceField()
    {
        isForceFieldActive = true;
        forceFieldElapsed = 0f;
        if (shieldDomeRenderer != null) shieldDomeRenderer.enabled = true;
        SetSiphonVisualsActive(true);
        if (NexusThroneroomEnvironment.Instance != null)
            NexusThroneroomEnvironment.Instance.SetPhase3SiphonState(true, transform.position);
    }

    void EndAction()
    {
        isActingExternally = false;
        if (sr != null)
        {
            sr.color = Color.white;
        }
    }

    // =========================================================================
    // UTILIDADES VISUALES Y PROYECTILES DE RÁFAGA CORTANTE
    // =========================================================================
    void SpawnArcSlashWave(Vector3 origin, float dirSign, float dmg)
    {
        GameObject obj = new GameObject("SovereignArcSlash");
        obj.transform.position = origin;
        SpriteRenderer srWave = obj.AddComponent<SpriteRenderer>();
        srWave.sprite = Level3MonsterAssetGenerator.GetPsionicRingSprite();
        srWave.color = new Color(0.92f, 0.26f, 1f, 0.95f);
        srWave.sortingOrder = 14;
        obj.transform.localScale = new Vector3(0.55f, 1.35f, 1f);

        SovereignArcProjectile proj = obj.AddComponent<SovereignArcProjectile>();
        proj.Init(dirSign, 9.2f, dmg, enemy);
    }

    GameObject CreateGroundWarningCircle(Vector3 center, float radius, Color col)
    {
        GameObject root = new GameObject("TelegraphWarningCircle");
        root.transform.position = center + new Vector3(0f, 0.08f, 0f);

        LineRenderer lr = root.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.startColor = col;
        lr.endColor = col;
        lr.sortingOrder = 12;

        int segs = 28;
        lr.positionCount = segs;
        for (int i = 0; i < segs; i++)
        {
            float ang = (i / (float)segs) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius * 0.28f, 0f));
        }

        // Disco interior que se expande para indicar el instante exacto del impacto
        GameObject fill = new GameObject("InnerFill");
        fill.transform.SetParent(root.transform, false);
        SpriteRenderer fsr = fill.AddComponent<SpriteRenderer>();
        fsr.sprite = Level3MonsterAssetGenerator.GetPsionicRingSprite();
        fsr.sortingOrder = 11;
        fsr.color = new Color(col.r, col.g, col.b, 0.55f);
        fill.transform.localScale = new Vector3(0.15f, 0.05f, 1f);
        fill.name = $"Fill_{radius:F2}";

        return root;
    }

    void UpdateGroundWarningCircle(GameObject root, float progress)
    {
        if (root == null || root.transform.childCount == 0) return;
        Transform fill = root.transform.GetChild(0);
        float p = Mathf.Clamp01(progress);
        float maxScale = 2.6f;
        fill.localScale = new Vector3(maxScale * p, maxScale * p * 0.28f, 1f);
    }

    void AttachForceFieldDome()
    {
        GameObject dome = new GameObject("SovereignForceField");
        dome.transform.SetParent(transform, false);
        dome.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        shieldDomeRenderer = dome.AddComponent<SpriteRenderer>();
        shieldDomeRenderer.sortingOrder = (sr != null ? sr.sortingOrder : 10) + 3;
        shieldDomeRenderer.sprite = Level3MonsterAssetGenerator.GetHexShieldSprite(isPureza: false, isCorrupcion: true);
        shieldDomeRenderer.enabled = false;
    }

    static Texture2D GetSiphonGlowTexture()
    {
        if (cachedBeamGlowTex != null) return cachedBeamGlowTex;
        const int w = 64;
        const int h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            float radial = Mathf.Pow(Mathf.Sin(v * Mathf.PI), 2.3f);
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                float edgeFade = Mathf.SmoothStep(0f, 1f, Mathf.Min(u / 0.06f, (1f - u) / 0.06f));
                float a = radial * edgeFade;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        cachedBeamGlowTex = tex;
        return cachedBeamGlowTex;
    }

    LineRenderer CreateSiphonLayer(string layerName, int points, float startW, float endW, Color startCol, Color endCol, int sortOrder)
    {
        GameObject obj = new GameObject(layerName);
        obj.transform.SetParent(transform, false);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = GetSiphonGlowTexture();
        lr.material = mat;
        lr.textureMode = LineTextureMode.Stretch;
        lr.positionCount = points;
        lr.widthCurve = new AnimationCurve(
            new Keyframe(0f, startW * 0.55f),
            new Keyframe(0.12f, startW),
            new Keyframe(0.88f, endW),
            new Keyframe(1f, endW * 0.65f)
        );
        lr.startColor = startCol;
        lr.endColor = endCol;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 2;
        lr.sortingOrder = sortOrder;
        lr.enabled = false;
        return lr;
    }

    void AttachSiphonBeam()
    {
        // Capa 1: Aura psiónica exterior difuminada
        siphonOuterBeam = CreateSiphonLayer(
            "SiphonBeam_OuterAura",
            20,
            0.52f,
            0.68f,
            new Color(0.28f, 0.86f, 1f, 0.34f),
            new Color(0.88f, 0.18f, 0.92f, 0.42f),
            13
        );
        // Capa 2: Hélice de plasma intermedia cian-magenta
        siphonMidBeam = CreateSiphonLayer(
            "SiphonBeam_PlasmaHelix",
            22,
            0.18f,
            0.24f,
            new Color(0.24f, 0.96f, 1f, 0.86f),
            new Color(0.96f, 0.22f, 0.82f, 0.92f),
            14
        );
        // Capa 3: Núcleo eléctrico de alto voltaje
        capsuleSiphonBeam = CreateSiphonLayer(
            "CoreSiphonBeam",
            26,
            0.055f,
            0.075f,
            new Color(0.88f, 0.99f, 1f, 0.98f),
            new Color(1f, 0.80f, 0.98f, 0.98f),
            15
        );

        Sprite ringSpr = Level3MonsterAssetGenerator.GetPsionicRingSprite();
        GameObject capHaloObj = new GameObject("SiphonCapsuleHalo");
        capHaloObj.transform.SetParent(transform, false);
        siphonCapHalo = capHaloObj.AddComponent<SpriteRenderer>();
        siphonCapHalo.sprite = ringSpr;
        siphonCapHalo.color = new Color(0.26f, 0.96f, 1f, 0.85f);
        siphonCapHalo.sortingOrder = 15;
        siphonCapHalo.enabled = false;

        GameObject bossHaloObj = new GameObject("SiphonBossHalo");
        bossHaloObj.transform.SetParent(transform, false);
        siphonBossHalo = bossHaloObj.AddComponent<SpriteRenderer>();
        siphonBossHalo.sprite = ringSpr;
        siphonBossHalo.color = new Color(0.96f, 0.25f, 0.85f, 0.85f);
        siphonBossHalo.sortingOrder = 15;
        siphonBossHalo.enabled = false;
    }

    void SetSiphonVisualsActive(bool active)
    {
        if (siphonOuterBeam != null) siphonOuterBeam.enabled = active;
        if (siphonMidBeam != null) siphonMidBeam.enabled = active;
        if (capsuleSiphonBeam != null) capsuleSiphonBeam.enabled = active;
        if (siphonCapHalo != null) siphonCapHalo.enabled = active;
        if (siphonBossHalo != null) siphonBossHalo.enabled = active;
    }

    void UpdateSiphonBeamVisuals()
    {
        SetSiphonVisualsActive(true);
        Vector3 capPos = NexusThroneroomEnvironment.Instance.GetCapsuleWorldPosition();
        Vector3 bossCore = transform.position + Vector3.up * 1.05f;
        Vector3 dir = (bossCore - capPos);
        float len = Mathf.Max(0.1f, dir.magnitude);
        Vector3 perp = new Vector3(-dir.y / len, dir.x / len, 0f);
        float t = Time.time;

        if (siphonOuterBeam != null)
        {
            int n = siphonOuterBeam.positionCount;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                float env = Mathf.Sin(u * Mathf.PI);
                float off = Mathf.Sin(t * 5.5f - u * 7f) * 0.16f * env;
                siphonOuterBeam.SetPosition(i, Vector3.Lerp(capPos, bossCore, u) + perp * off);
            }
        }

        if (siphonMidBeam != null)
        {
            int n = siphonMidBeam.positionCount;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                float env = Mathf.Sin(u * Mathf.PI);
                float off = Mathf.Sin(t * 11f - u * 12f) * 0.19f * env;
                siphonMidBeam.SetPosition(i, Vector3.Lerp(capPos, bossCore, u) + perp * off);
            }
        }

        if (capsuleSiphonBeam != null)
        {
            int n = capsuleSiphonBeam.positionCount;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                float env = Mathf.Sin(u * Mathf.PI);
                float arc = (Mathf.Sin(t * 24f + i * 2.1f) * 0.11f + Mathf.Cos(t * 37f - i * 3.3f) * 0.06f) * env;
                capsuleSiphonBeam.SetPosition(i, Vector3.Lerp(capPos, bossCore, u) + perp * arc);
            }
        }

        if (siphonCapHalo != null)
        {
            siphonCapHalo.transform.position = capPos;
            float sc = 0.72f + Mathf.Sin(t * 8f) * 0.12f;
            siphonCapHalo.transform.localScale = new Vector3(sc, sc, 1f);
            siphonCapHalo.transform.Rotate(0f, 0f, 110f * Time.deltaTime);
        }

        if (siphonBossHalo != null)
        {
            siphonBossHalo.transform.position = bossCore;
            float sb = 0.65f + Mathf.Cos(t * 9f) * 0.11f;
            siphonBossHalo.transform.localScale = new Vector3(sb, sb, 1f);
            siphonBossHalo.transform.Rotate(0f, 0f, -135f * Time.deltaTime);
        }

        siphonOrbTimer -= Time.deltaTime;
        if (siphonOrbTimer <= 0f)
        {
            siphonOrbTimer = 0.12f;
            GameObject orbObj = new GameObject("SiphonOrb");
            orbObj.transform.position = capPos;
            SpriteRenderer orbSr = orbObj.AddComponent<SpriteRenderer>();
            orbSr.sprite = Level3MonsterAssetGenerator.GetPsionicRingSprite();
            orbSr.sortingOrder = 16;
            orbSr.color = new Color(0.35f, 0.98f, 1f, 0.95f);
            orbObj.transform.localScale = Vector3.one * Random.Range(0.18f, 0.28f);
            SiphonEnergyOrb orb = orbObj.AddComponent<SiphonEnergyOrb>();
            orb.Init(capPos, bossCore, perp, Random.Range(-0.22f, 0.22f));
        }
    }

    void OnGUI()
    {
        if (defeatTriggered || enemy == null || enemy.IsDead) return;
        if (PlayerStats.Instance != null && PlayerStats.Instance.IsAnyBlockingModalOpen) return;
        if (EndingCinematicController.Instance != null && EndingCinematicController.Instance.isEndingActive) return;

        if (role == BossRole.SupremeSovereign && currentPhase == 3 && isForceFieldActive)
        {
            float sx = Screen.width / 1920f;
            float sy = Screen.height / 1080f;
            int combo = (PlayerStats.Instance != null) ? PlayerStats.Instance.comboCount : 0;
            bool ultReady = (PlayerStats.Instance != null && PlayerStats.Instance.IsUltReady);

            Rect bar = new Rect(460f * sx, 92f * sy, 1000f * sx, 44f * sy);
            GUI.color = new Color(0.08f, 0.02f, 0.16f, 0.92f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = (combo >= 30 && ultReady) ? new Color(0.28f, 0.98f, 1f, 0.96f) : new Color(0.84f, 0.25f, 1f, 0.96f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(bar.x, bar.yMax - 3f, bar.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle st = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(12, Mathf.RoundToInt(17f * sy)),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            st.normal.textColor = (combo >= 30 && ultReady) ? new Color(0.32f, 1f, 1f) : new Color(1f, 0.85f, 0.35f);
            string msg = (combo >= 30 && ultReady)
                ? $"⚡ ¡COMBO SOBREMARCHA x{combo} LISTO! ¡PULSA [K] PARA QUEBRAR EL CAMPO DE FUERZA CON SOBRECARGA NEXO!"
                : $"🛡 CAMPO DE FUERZA FUSIÓN ACTIVO — COMBO: x{combo}/30  |  ULT: {Mathf.RoundToInt(PlayerStats.Instance != null ? PlayerStats.Instance.ultCharge : 0)}%  (GOLPEA Y DISPARA ULTIMATE [K])";
            GUI.Label(bar, msg, st);
        }
    }
}

public class SovereignArcProjectile : MonoBehaviour
{
    private float dir;
    private float speed;
    private float dmg;
    private EnemyDummy owner;
    private float life = 2.6f;

    public void Init(float moveDir, float spd, float damage, EnemyDummy bossOwner)
    {
        dir = Mathf.Sign(moveDir);
        speed = spd;
        dmg = damage;
        owner = bossOwner;
    }

    void Update()
    {
        if (PlayerStats.Instance != null && PlayerStats.Instance.IsAnyBlockingModalOpen) return;

        transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
        transform.Rotate(0f, 0f, 360f * Time.deltaTime);
        life -= Time.deltaTime;

        Transform kael = (KaelAutoController.Instance != null) ? KaelAutoController.Instance.transform : null;
        if (kael != null && Mathf.Abs(kael.position.x - transform.position.x) <= 0.72f)
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.TakeDamage(dmg, owner);
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnHitSparks(transform.position, true, 8);
            }
            Destroy(gameObject);
            return;
        }

        if (life <= 0f) Destroy(gameObject);
    }
}

public class SiphonEnergyOrb : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private Vector3 perpAxis;
    private float arcAmp;
    private float elapsed = 0f;
    private const float Duration = 0.34f;
    private SpriteRenderer sr;

    public void Init(Vector3 from, Vector3 to, Vector3 perp, float amp)
    {
        startPos = from;
        endPos = to;
        perpAxis = perp;
        arcAmp = amp;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float u = Mathf.Clamp01(elapsed / Duration);
        Vector3 p = Vector3.Lerp(startPos, endPos, u) + perpAxis * (Mathf.Sin(u * Mathf.PI) * arcAmp);
        transform.position = p;
        transform.Rotate(0f, 0f, 360f * Time.deltaTime);

        if (sr != null)
        {
            Color c = Color.Lerp(new Color(0.26f, 0.96f, 1f, 0.95f), new Color(1f, 0.24f, 0.82f, 0.95f), u);
            c.a *= (1f - u * 0.45f);
            sr.color = c;
        }

        if (elapsed >= Duration) Destroy(gameObject);
    }
}
