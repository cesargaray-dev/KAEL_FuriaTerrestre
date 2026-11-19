using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Interfaz modal del Arsenal de Vanguardia (Issue #21):
/// Permite previsualizar en un visor holográfico 3D/2D, adquirir con oro acumulado y equipar
/// las 4 variantes tácticas del Guantelete Nexo además de comprar el Dron Centinela N.E.X.A.
/// </summary>
public class ArsenalShopUI : MonoBehaviour
{
    public static ArsenalShopUI Instance { get; private set; }

    private enum PreviewSelectionType
    {
        Gauntlet,
        NexaDrone
    }

    private struct VisorSpark
    {
        public Vector2 pos;   // Normalizado respecto al centro del visor (-1..1)
        public Vector2 vel;
        public float age;
        public float lifetime;
        public float size;
        public Gradient gradient;
    }

    private PreviewSelectionType selectedType = PreviewSelectionType.Gauntlet;
    private GauntletVariant previewGauntlet = GauntletVariant.PrototypeCyan;

    private readonly Texture2D[] gauntletTextures = new Texture2D[4];
    private Texture2D nexaDroneTexture;
    private Texture2D holoGlowTex;
    private Texture2D holoRingTex;
    private Texture2D sparkTex;
    private Texture2D shockRingTex;
    private Texture2D gradientBarTex;
    private int cachedGradientTexVariant = -1;

    private readonly List<VisorSpark> visorSparks = new List<VisorSpark>(64);
    private float ambientSparkTimer = 0f;
    private float previewRingPulse = 0f;
    private string statusBannerMsg = "";
    private Color statusBannerColor = Color.cyan;
    private float statusBannerTimer = 0f;

    public static ArsenalShopUI EnsureExists()
    {
        if (Instance != null) return Instance;

        ArsenalShopUI existing = FindAnyObjectByType<ArsenalShopUI>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject root = (PlayerStats.Instance != null)
            ? PlayerStats.Instance.gameObject
            : new GameObject("ArsenalShopUI");

        return root.AddComponent<ArsenalShopUI>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        LoadAllTextures();
        BuildProceduralVisorTextures();
    }

    void OnDestroy()
    {
        if (holoGlowTex != null) Destroy(holoGlowTex);
        if (holoRingTex != null) Destroy(holoRingTex);
        if (sparkTex != null) Destroy(sparkTex);
        if (shockRingTex != null) Destroy(shockRingTex);
        if (gradientBarTex != null) Destroy(gradientBarTex);
        if (Instance == this) Instance = null;
    }

    void LoadAllTextures()
    {
        for (int i = 0; i < PlayerStats.GauntletCatalog.Length; i++)
        {
            gauntletTextures[i] = PlayerStats.LoadResTex(PlayerStats.GauntletCatalog[i].TextureResourcePath);
        }
        nexaDroneTexture = PlayerStats.LoadResTex("StoryUI/NexaDrone");
    }

    void BuildProceduralVisorTextures()
    {
        if (holoGlowTex == null)
        {
            const int s = 128;
            holoGlowTex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float half = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    holoGlowTex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * 0.85f));
                }
            }
            holoGlowTex.Apply();
        }

        if (holoRingTex == null)
        {
            const int s = 128;
            holoRingTex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float half = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    bool mainArc = Mathf.Abs(d - 0.88f) < 0.032f && Mathf.Sin(angle * 4f) > -0.45f;
                    bool innerArc = Mathf.Abs(d - 0.72f) < 0.020f && Mathf.Cos(angle * 6f) > 0.15f;
                    float alpha = (mainArc || innerArc) ? 0.92f : 0f;
                    holoRingTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            holoRingTex.Apply();
        }

        if (sparkTex == null)
        {
            const int s = 16;
            sparkTex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float half = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    sparkTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            sparkTex.Apply();
        }

        if (shockRingTex == null)
        {
            const int s = 128;
            shockRingTex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float half = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = 1f - Mathf.Clamp01(Mathf.Abs(d - 0.85f) / 0.08f);
                    shockRingTex.SetPixel(x, y, new Color(1f, 1f, 1f, ring));
                }
            }
            shockRingTex.Apply();
        }

        if (gradientBarTex == null)
        {
            gradientBarTex = new Texture2D(64, 1, TextureFormat.RGBA32, false);
            gradientBarTex.wrapMode = TextureWrapMode.Clamp;
        }
    }

    public void OnShopOpened(GauntletVariant currentlyEquipped)
    {
        selectedType = PreviewSelectionType.Gauntlet;
        previewGauntlet = currentlyEquipped;
        visorSparks.Clear();
        TriggerVisorTestBurst();
    }

    void Update()
    {
        if (PlayerStats.Instance == null || !PlayerStats.Instance.isArsenalShopOpen)
            return;

        float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);

        if (previewRingPulse > 0f)
        {
            previewRingPulse = Mathf.Max(0f, previewRingPulse - dt * 2.2f);
        }

        if (statusBannerTimer > 0f)
        {
            statusBannerTimer = Mathf.Max(0f, statusBannerTimer - dt);
        }

        ambientSparkTimer -= dt;
        if (ambientSparkTimer <= 0f)
        {
            ambientSparkTimer = 0.085f;
            SpawnVisorSpark(isBurst: false);
        }

        for (int i = visorSparks.Count - 1; i >= 0; i--)
        {
            VisorSpark s = visorSparks[i];
            s.age += dt;
            if (s.age >= s.lifetime)
            {
                visorSparks.RemoveAt(i);
                continue;
            }
            s.pos += s.vel * dt;
            s.vel *= 0.96f;
            visorSparks[i] = s;
        }
    }

    Gradient GetSelectedGradient()
    {
        GauntletVariant gradVariant = (selectedType == PreviewSelectionType.Gauntlet)
            ? previewGauntlet
            : GauntletVariant.PrototypeCyan;

        if (CombatVFXManager.Instance != null)
            return CombatVFXManager.Instance.BuildGradientForVariant(gradVariant, false);

        GauntletSpec spec = PlayerStats.GetGauntletSpec(gradVariant);
        Gradient fallback = new Gradient();
        fallback.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(spec.CoreColor, 0f),
                new GradientColorKey(spec.PrimaryColor, 0.45f),
                new GradientColorKey(spec.SecondaryColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        return fallback;
    }

    void TriggerVisorTestBurst()
    {
        previewRingPulse = 1f;
        for (int i = 0; i < 24; i++)
        {
            SpawnVisorSpark(isBurst: true);
        }
    }

    void SpawnVisorSpark(bool isBurst)
    {
        if (visorSparks.Count > 60) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float speed = isBurst ? Random.Range(0.45f, 1.35f) : Random.Range(0.14f, 0.40f);
        Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
        if (!isBurst) vel.y += 0.22f;

        VisorSpark s = new VisorSpark
        {
            pos = isBurst ? vel.normalized * 0.08f : Random.insideUnitCircle * 0.28f,
            vel = vel,
            age = 0f,
            lifetime = isBurst ? Random.Range(0.32f, 0.58f) : Random.Range(0.55f, 0.95f),
            size = isBurst ? Random.Range(10f, 18f) : Random.Range(6f, 12f),
            gradient = GetSelectedGradient()
        };
        visorSparks.Add(s);
    }

    void RefreshGradientBarTexture(GauntletVariant variant)
    {
        if (gradientBarTex == null || cachedGradientTexVariant == (int)variant) return;
        cachedGradientTexVariant = (int)variant;

        Gradient g = CombatVFXManager.Instance != null
            ? CombatVFXManager.Instance.BuildGradientForVariant(variant, false)
            : GetSelectedGradient();

        for (int x = 0; x < 64; x++)
        {
            Color c = g.Evaluate(x / 63f);
            c.a = 1f;
            gradientBarTex.SetPixel(x, 0, c);
        }
        gradientBarTex.Apply();
    }

    public void DrawShopModal()
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null) return;

        GUI.color = new Color(0.01f, 0.02f, 0.06f, 0.93f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect panel = PlayerStats.ScaleRect(44f, 28f, 1832f, 1024f);
        stats.DrawSkinnedCyberPanel(panel, stats.TexPanelCyan, stats.ColCyanNexo);

        // --- CABECERA ---
        GauntletSpec eqSpec = PlayerStats.GetGauntletSpec(stats.equippedGauntlet);
        stats.DrawStyledText(
            PlayerStats.ScaleRect(80f, 46f, 820f, 38f),
            "⚙ ARSENAL DE VANGUARDIA — FORJA NEXO",
            26, TextAnchor.MiddleLeft, stats.ColCyanNexo);

        stats.DrawStyledText(
            PlayerStats.ScaleRect(80f, 84f, 820f, 26f),
            $"EQUIPADO: <color={eqSpec.AccentHex}>{eqSpec.DisplayName.ToUpperInvariant()}</color>   |   DRON N.E.X.A.: {(stats.hasPurchasedNexaDrone ? "<color=#2ee678>EN LÍNEA</color>" : "<color=#ff6666>NO ADQUIRIDO</color>")}",
            14, TextAnchor.MiddleLeft, new Color(0.86f, 0.94f, 1f));

        // Panel Oro acumulado
        stats.DrawSkinnedCyberPanel(PlayerStats.ScaleRect(1020f, 46f, 240f, 58f), stats.TexPanelGold, stats.ColGoldYel);
        stats.DrawStyledText(PlayerStats.ScaleRect(1020f, 46f, 240f, 58f), $"● {stats.gold} ORO", 22, TextAnchor.MiddleCenter, stats.ColGoldYel);

        // Acceso rápido al Árbol de Mejoras
        if (stats.DrawSkinnedCyberButton(
            new Rect(1276f, 46f, 272f, 58f),
            stats.TexBtnPurple,
            "🧬 ÁRBOL MEJORAS [TAB]",
            "ATRIBUTOS Y EVOLUCIÓN",
            15, 10, Color.white, stats.ColGoldYel, stats.ColExpPurp, true))
        {
            stats.OpenUpgradeTreeAtTab(0);
            return;
        }

        // Botón Cerrar
        if (stats.DrawSkinnedCyberButton(
            new Rect(1564f, 46f, 276f, 58f),
            stats.TexBtnCrimson,
            "✖ CERRAR [B / ESC]",
            "VOLVER AL COMBATE",
            16, 11, Color.white, new Color(1f, 0.84f, 0.84f), new Color(1f, 0.35f, 0.45f), true))
        {
            stats.CloseAllUpgradeModals();
            return;
        }

        // --- COLUMNA IZQUIERDA: VISOR HOLOGRÁFICO 3D/2D & TELEMETRÍA ---
        DrawHolographicVisorPanel(stats);

        // --- COLUMNA DERECHA: CATÁLOGO 4 GUANTELETES + TARJETA DRON N.E.X.A. ---
        DrawCatalogPanel(stats);
    }

    void DrawHolographicVisorPanel(PlayerStats stats)
    {
        bool isGauntlet = (selectedType == PreviewSelectionType.Gauntlet);
        GauntletSpec spec = PlayerStats.GetGauntletSpec(previewGauntlet);
        Color accent = isGauntlet ? spec.PrimaryColor : new Color(0.22f, 0.94f, 1f);
        Color secondary = isGauntlet ? spec.SecondaryColor : new Color(0.10f, 0.55f, 1f);

        Rect visorCard = PlayerStats.ScaleRect(78f, 128f, 696f, 892f);
        stats.DrawCyberBox(visorCard, new Color(0.02f, 0.04f, 0.09f, 0.96f), accent, 2.2f);

        // Subcabecera del visor
        stats.DrawStyledText(
            PlayerStats.ScaleRect(98f, 140f, 430f, 26f),
            "VISOR HOLOGRÁFICO 3D/2D",
            14, TextAnchor.MiddleLeft, accent);

        string badgeStatus;
        Color badgeColor;
        if (isGauntlet)
        {
            bool unlocked = stats.IsGauntletUnlocked(previewGauntlet);
            bool equipped = stats.equippedGauntlet == previewGauntlet;
            badgeStatus = equipped ? "✔ EQUIPADO" : (unlocked ? "✦ DESBLOQUEADO" : $"{spec.GoldCost} ORO");
            badgeColor = equipped ? stats.ColHpGreen : (unlocked ? stats.ColCyanNexo : stats.ColGoldYel);
        }
        else
        {
            badgeStatus = stats.hasPurchasedNexaDrone ? "✔ DESPLEGADO" : $"{PlayerStats.NEXA_DRONE_COST} ORO";
            badgeColor = stats.hasPurchasedNexaDrone ? stats.ColHpGreen : stats.ColGoldYel;
        }
        stats.DrawCyberBox(PlayerStats.ScaleRect(584f, 138f, 172f, 28f), new Color(0.05f, 0.08f, 0.14f, 0.95f), badgeColor, 1.5f);
        stats.DrawStyledText(PlayerStats.ScaleRect(584f, 138f, 172f, 28f), badgeStatus, 13, TextAnchor.MiddleCenter, badgeColor);

        // Escenario Holográfico 3D/2D
        Rect stageBase = new Rect(102f, 176f, 648f, 360f);
        Rect stageScaled = PlayerStats.ScaleRect(stageBase.x, stageBase.y, stageBase.width, stageBase.height);
        stats.DrawCyberBox(stageScaled, new Color(0.01f, 0.02f, 0.05f, 0.98f), new Color(accent.r, accent.g, accent.b, 0.55f), 1.5f);

        // Grid sutil dentro del visor
        float t = Time.unscaledTime;
        GUI.color = new Color(accent.r, accent.g, accent.b, 0.08f);
        for (int row = 1; row < 8; row++)
        {
            float ly = stageScaled.y + (stageScaled.height / 8f) * row;
            GUI.DrawTexture(new Rect(stageScaled.x + 4f, ly, stageScaled.width - 8f, 1f), Texture2D.whiteTexture);
        }
        for (int col = 1; col < 10; col++)
        {
            float lx = stageScaled.x + (stageScaled.width / 10f) * col;
            GUI.DrawTexture(new Rect(lx, stageScaled.y + 4f, 1f, stageScaled.height - 8f), Texture2D.whiteTexture);
        }

        // Haz de escaneo holográfico vertical
        float scanY = stageScaled.y + Mathf.Repeat(t * 0.42f, 1f) * stageScaled.height;
        GUI.color = new Color(accent.r, accent.g, accent.b, 0.24f);
        GUI.DrawTexture(new Rect(stageScaled.x + 6f, scanY, stageScaled.width - 12f, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Vector2 centerScaled = new Vector2(stageScaled.x + stageScaled.width * 0.5f, stageScaled.y + stageScaled.height * 0.48f);

        // Halo radial de fondo
        if (holoGlowTex != null)
        {
            float glowPulse = 1f + Mathf.Sin(t * 3.2f) * 0.08f;
            float glowSz = Mathf.Min(stageScaled.width, stageScaled.height) * 0.85f * glowPulse;
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.36f);
            GUI.DrawTexture(new Rect(centerScaled.x - glowSz * 0.5f, centerScaled.y - glowSz * 0.5f, glowSz, glowSz), holoGlowTex);
        }

        // Anillos 3D/2D rotatorios en perspectiva inclinada + base holográfica
        if (holoRingTex != null)
        {
            float outerSize = Mathf.Min(stageScaled.width, stageScaled.height) * 0.78f;
            Matrix4x4 prevMatrix = GUI.matrix;

            GUIUtility.RotateAroundPivot(t * 28f, centerScaled);
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.52f);
            GUI.DrawTexture(new Rect(centerScaled.x - outerSize * 0.5f, centerScaled.y - outerSize * 0.5f, outerSize, outerSize), holoRingTex);

            GUI.matrix = prevMatrix;
            Vector2 pedestalCenter = new Vector2(centerScaled.x, stageScaled.y + stageScaled.height * 0.83f);
            float pedW = outerSize * 0.92f;
            float pedH = pedW * 0.24f;
            GUI.color = new Color(secondary.r, secondary.g, secondary.b, 0.65f);
            GUI.DrawTexture(new Rect(pedestalCenter.x - pedW * 0.5f, pedestalCenter.y - pedH * 0.5f, pedW, pedH), holoRingTex);
            GUI.matrix = prevMatrix;
        }

        // Onda expansiva al pulsar Probar Impacto VFX
        if (previewRingPulse > 0f && shockRingTex != null)
        {
            float expand = 1f - previewRingPulse;
            float rSize = Mathf.Lerp(stageScaled.height * 0.25f, stageScaled.height * 1.05f, expand);
            GUI.color = new Color(accent.r, accent.g, accent.b, previewRingPulse * 0.85f);
            GUI.DrawTexture(new Rect(centerScaled.x - rSize * 0.5f, centerScaled.y - rSize * 0.5f, rSize, rSize), shockRingTex);
            GUI.color = Color.white;
        }

        // Sprite principal con levitación e ilusión 3D (escala en X suavemente cosenoidal + inclinación)
        Texture2D previewTex = isGauntlet ? gauntletTextures[(int)previewGauntlet] : nexaDroneTexture;
        if (previewTex != null)
        {
            float bobY = Mathf.Sin(t * 2.5f) * (stageScaled.height * 0.025f);
            float tiltAngle = Mathf.Sin(t * 1.4f) * 4.5f;
            float rot3DForeshorten = 0.90f + 0.10f * Mathf.Cos(t * 1.8f);
            float spriteSize = Mathf.Min(stageScaled.width, stageScaled.height) * 0.68f;
            float drawW = spriteSize * rot3DForeshorten;
            float drawH = spriteSize;

            Matrix4x4 prevM = GUI.matrix;
            Vector2 pivot = new Vector2(centerScaled.x, centerScaled.y + bobY);
            GUIUtility.RotateAroundPivot(tiltAngle, pivot);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(pivot.x - drawW * 0.5f, pivot.y - drawH * 0.5f, drawW, drawH), previewTex, ScaleMode.ScaleToFit);
            GUI.matrix = prevM;
        }

        // Render de partículas en vivo dentro del visor usando el Gradient de CombatVFXManager
        if (sparkTex != null)
        {
            float halfW = stageScaled.width * 0.42f;
            float halfH = stageScaled.height * 0.42f;
            for (int i = 0; i < visorSparks.Count; i++)
            {
                VisorSpark s = visorSparks[i];
                float lifeT = Mathf.Clamp01(s.age / s.lifetime);
                Color col = s.gradient != null ? s.gradient.Evaluate(lifeT) : accent;
                float px = centerScaled.x + s.pos.x * halfW;
                float py = centerScaled.y - s.pos.y * halfH;
                float sz = s.size * (Screen.height / 1080f) * (1f - lifeT * 0.55f);
                GUI.color = col;
                GUI.DrawTexture(new Rect(px - sz * 0.5f, py - sz * 0.5f, sz, sz), sparkTex);
            }
            GUI.color = Color.white;
        }

        // Botón interactivo de prueba VFX + Barra de Gradiente en Tiempo Real
        GauntletVariant gradVariant = isGauntlet ? previewGauntlet : GauntletVariant.PrototypeCyan;
        RefreshGradientBarTexture(gradVariant);

        stats.DrawStyledText(
            PlayerStats.ScaleRect(stageBase.x + 12f, stageBase.y + stageBase.height - 56f, 280f, 20f),
            "GRADIENTE VFX ACTIVO:", 11, TextAnchor.MiddleLeft, new Color(0.84f, 0.92f, 1f));

        if (gradientBarTex != null)
        {
            Rect gBar = PlayerStats.ScaleRect(stageBase.x + 12f, stageBase.y + stageBase.height - 32f, 290f, 16f);
            stats.DrawCyberBox(new Rect(gBar.x - 1f, gBar.y - 1f, gBar.width + 2f, gBar.height + 2f), Color.black, accent, 1f);
            GUI.color = Color.white;
            GUI.DrawTexture(gBar, gradientBarTex, ScaleMode.StretchToFill);
        }

        if (stats.DrawSkinnedCyberButton(
            new Rect(stageBase.x + stageBase.width - 216f, stageBase.y + stageBase.height - 46f, 204f, 36f),
            stats.TexBtnCyan,
            "⚡ PROBAR IMPACTO VFX",
            "",
            12, 10, Color.white, accent, accent, true))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            TriggerVisorTestBurst();
        }

        // --- ESPECIFICACIONES DE COMBATE Y COMPARATIVA ---
        string itemName = isGauntlet ? spec.DisplayName : "Dron Centinela Táctico N.E.X.A.";
        string elementTag = isGauntlet ? spec.ElementTag : "AUTÓNOMO / SOPORTE";
        string passiveTitle = isGauntlet ? spec.PassiveTitle : "Plasma Guiado + Escudo Baluarte";
        string passiveDesc = isGauntlet
            ? spec.PassiveSummary
            : "Orbita sobre el hombro de Kael disparando proyectiles de plasma teledirigidos cada 1.3s y despliega burbuja de protección al caer debajo del 25% HP.";
        string specsLine = isGauntlet
            ? spec.CombatSpecsLine
            : "DISPARO: PLASMA TELEDIRIGIDO  |  ESCUDO: 3 IMPACTOS (<25% HP)  |  RECARGA: 18s";

        stats.DrawStyledText(PlayerStats.ScaleRect(102f, 548f, 648f, 34f), itemName.ToUpperInvariant(), 21, TextAnchor.MiddleLeft, accent);
        stats.DrawStyledText(PlayerStats.ScaleRect(102f, 580f, 648f, 22f), $"MÓDULO: {elementTag}   •   PROPIEDAD: {passiveTitle.ToUpperInvariant()}", 13, TextAnchor.MiddleLeft, stats.ColGoldYel);
        stats.DrawStyledText(PlayerStats.ScaleRect(102f, 606f, 648f, 58f), passiveDesc, 14, TextAnchor.UpperLeft, new Color(0.88f, 0.94f, 1f), wordWrap: true);

        // Caja técnica comparativa de combate
        Rect telemetryRect = PlayerStats.ScaleRect(102f, 672f, 648f, 178f);
        stats.DrawCyberBox(telemetryRect, new Color(0.03f, 0.06f, 0.12f, 0.96f), new Color(accent.r, accent.g, accent.b, 0.65f), 1.5f);
        stats.DrawStyledText(PlayerStats.ScaleRect(116f, 678f, 620f, 24f), "ESPECIFICACIONES DE COMBATE EN VIVO", 13, TextAnchor.MiddleLeft, accent);

        int projectedCrit = Mathf.RoundToInt(CalculateProjectedCrit(stats, isGauntlet ? previewGauntlet : stats.equippedGauntlet) * 100f);
        int currentCrit = Mathf.RoundToInt(stats.critChance * 100f);
        string critLine = (projectedCrit != currentCrit)
            ? $"Prob. Crítico: {currentCrit}%  →  <color=#FFD629>{projectedCrit}%</color>"
            : $"Prob. Crítico actual: {currentCrit}%";

        string burnLine = (isGauntlet && previewGauntlet == GauntletVariant.PyroReactor)
            ? $"Quemadura Fuego: <color=#FF5533>+{Mathf.RoundToInt(stats.GetPyroBurnDamagePerTick())} daño / 0.5s (3.5s)</color>"
            : "Quemadura Elemental: Inactiva (solo estándar)";

        string stealLine = (isGauntlet && previewGauntlet == GauntletVariant.VorstVoid)
            ? "Robo de Vida: <color=#D966FF>5% del daño real como HP</color>"
            : "Robo de Vida: 0%";

        stats.DrawStyledText(PlayerStats.ScaleRect(118f, 706f, 616f, 26f), $"Daño Base Kael: {Mathf.RoundToInt(stats.attackDamage)} ATK   |   {critLine}", 14, TextAnchor.MiddleLeft, Color.white);
        stats.DrawStyledText(PlayerStats.ScaleRect(118f, 734f, 616f, 26f), burnLine, 14, TextAnchor.MiddleLeft, new Color(0.90f, 0.95f, 1f));
        stats.DrawStyledText(PlayerStats.ScaleRect(118f, 762f, 616f, 26f), stealLine, 14, TextAnchor.MiddleLeft, new Color(0.90f, 0.95f, 1f));
        stats.DrawStyledText(PlayerStats.ScaleRect(118f, 794f, 616f, 44f), specsLine, 12, TextAnchor.MiddleCenter, accent, wordWrap: true);

        // Banner breve de confirmación o botón principal de compra/equipo
        if (statusBannerTimer > 0f)
        {
            stats.DrawStyledText(PlayerStats.ScaleRect(102f, 856f, 648f, 26f), statusBannerMsg, 14, TextAnchor.MiddleCenter, statusBannerColor);
        }

        DrawPrimaryVisorActionButton(stats, isGauntlet, spec, accent);
    }

    void DrawPrimaryVisorActionButton(PlayerStats stats, bool isGauntlet, GauntletSpec spec, Color accent)
    {
        Rect btnRect = new Rect(102f, 888f, 648f, 108f);

        if (isGauntlet)
        {
            bool unlocked = stats.IsGauntletUnlocked(previewGauntlet);
            bool equipped = stats.equippedGauntlet == previewGauntlet;
            bool canAfford = stats.gold >= spec.GoldCost;

            if (equipped)
            {
                stats.DrawSkinnedCyberButton(
                    btnRect, stats.TexBtnEmerald,
                    $"✔ {spec.DisplayName.ToUpperInvariant()} EQUIPADO",
                    "PARTÍCULAS Y PASIVA ACTIVAS EN COMBATE",
                    18, 12, Color.white, new Color(0.84f, 1f, 0.90f), stats.ColHpGreen, false);
            }
            else if (unlocked)
            {
                if (stats.DrawSkinnedCyberButton(
                    btnRect, stats.TexBtnCyan,
                    $"⚡ EQUIPAR {spec.CodeName}",
                    "ACTUALIZA AL INSTANTE LOS GRADIENTES VFX Y MODIFICADORES",
                    18, 12, Color.white, accent, accent, true))
                {
                    ExecuteGauntletAction(stats, previewGauntlet);
                }
            }
            else
            {
                string title = canAfford
                    ? $"🛒 COMPRAR Y EQUIPAR ({spec.GoldCost} ORO)"
                    : $"🔒 BLOQUEADO — NECESITAS {spec.GoldCost} ORO (TIENES {stats.gold})";
                string sub = canAfford
                    ? $"DESBLOQUEA {spec.DisplayName.ToUpperInvariant()} PERMANENTE"
                    : $"DERROTA MÁS OLEADAS VORST PARA REUNIR {spec.GoldCost - stats.gold} ORO EXTRA";

                if (stats.DrawSkinnedCyberButton(
                    btnRect,
                    canAfford ? stats.TexBtnGold : stats.TexBtnCrimson,
                    title, sub,
                    18, 12,
                    canAfford ? Color.white : new Color(1f, 0.72f, 0.72f),
                    canAfford ? stats.ColGoldYel : new Color(1f, 0.85f, 0.85f),
                    canAfford ? stats.ColGoldYel : Color.red,
                    canAfford))
                {
                    ExecuteGauntletAction(stats, previewGauntlet);
                }
            }
        }
        else
        {
            if (stats.hasPurchasedNexaDrone)
            {
                stats.DrawSkinnedCyberButton(
                    btnRect, stats.TexBtnEmerald,
                    "✔ DRON N.E.X.A. DESPLEGADO EN ESCUADRÓN",
                    "SOPORTE AÉREO Y PROTOCOLO BALUARTE ACTIVOS",
                    18, 12, Color.white, new Color(0.84f, 1f, 0.90f), stats.ColHpGreen, false);
            }
            else
            {
                bool canAfford = stats.gold >= PlayerStats.NEXA_DRONE_COST;
                string title = canAfford
                    ? $"🤖 ADQUIRIR Y DESPLEGAR DRON N.E.X.A. ({PlayerStats.NEXA_DRONE_COST} ORO)"
                    : $"🔒 DRON N.E.X.A. — NECESITAS {PlayerStats.NEXA_DRONE_COST} ORO (TIENES {stats.gold})";
                string sub = canAfford
                    ? "ACTIVA DISPAROS DE PLASMA AUTÓNOMOS Y ESCUDO BALUARTE (<25% HP)"
                    : $"FALTAN {PlayerStats.NEXA_DRONE_COST - stats.gold} ORO PARA ADQUIRIR ESTA UNIDAD";

                if (stats.DrawSkinnedCyberButton(
                    btnRect,
                    canAfford ? stats.TexBtnGold : stats.TexBtnCrimson,
                    title, sub,
                    17, 12,
                    canAfford ? Color.white : new Color(1f, 0.72f, 0.72f),
                    canAfford ? stats.ColGoldYel : new Color(1f, 0.85f, 0.85f),
                    canAfford ? stats.ColGoldYel : Color.red,
                    canAfford))
                {
                    ExecuteNexaDronePurchase(stats);
                }
            }
        }
    }

    void DrawCatalogPanel(PlayerStats stats)
    {
        stats.DrawStyledText(
            PlayerStats.ScaleRect(798f, 126f, 1044f, 28f),
            "CATÁLOGO DE VARIANTES DEL GUANTELETE NEXO (CLIC PARA PREVISUALIZAR, COMPRAR O EQUIPAR)",
            15, TextAnchor.MiddleLeft, stats.ColGoldYel);

        // Grid 2x2 de las 4 variantes de guantelete
        float cardW = 510f;
        float cardH = 312f;
        float gapX = 24f;
        float gapY = 20f;
        float startX = 798f;
        float startY = 160f;

        for (int i = 0; i < PlayerStats.GauntletCatalog.Length; i++)
        {
            int col = i % 2;
            int row = i / 2;
            float cx = startX + col * (cardW + gapX);
            float cy = startY + row * (cardH + gapY);

            DrawGauntletCard(stats, new Rect(cx, cy, cardW, cardH), PlayerStats.GauntletCatalog[i]);
        }

        // Sección inferior derecha: Compra del Dron Centinela N.E.X.A.
        stats.DrawStyledText(
            PlayerStats.ScaleRect(798f, 814f, 1044f, 26f),
            "SOPORTE TÁCTICO AUTÓNOMO (UNIDAD ROBOT DE COMBATE)",
            14, TextAnchor.MiddleLeft, stats.ColCyanNexo);

        DrawNexaDroneCard(stats, new Rect(798f, 844f, 1044f, 176f));
    }

    void DrawGauntletCard(PlayerStats stats, Rect baseR, GauntletSpec spec)
    {
        bool isSelected = (selectedType == PreviewSelectionType.Gauntlet && previewGauntlet == spec.Variant);
        bool isEquipped = (stats.equippedGauntlet == spec.Variant);
        bool isUnlocked = stats.IsGauntletUnlocked(spec.Variant);
        bool canAfford = stats.gold >= spec.GoldCost;

        Rect r = PlayerStats.ScaleRect(baseR.x, baseR.y, baseR.width, baseR.height);
        bool hovered = r.Contains(Event.current.mousePosition);

        Color borderCol = isSelected ? Color.white : (isEquipped ? stats.ColHpGreen : spec.PrimaryColor);
        Color bgCol = isSelected
            ? new Color(0.05f, 0.09f, 0.17f, 0.98f)
            : (hovered ? new Color(0.04f, 0.07f, 0.14f, 0.96f) : new Color(0.02f, 0.04f, 0.09f, 0.95f));

        stats.DrawCyberBox(r, bgCol, borderCol, isSelected ? 3f : 1.8f);

        // Franja superior de color elemental / gradiente
        GUI.color = new Color(spec.PrimaryColor.r, spec.PrimaryColor.g, spec.PrimaryColor.b, 0.20f);
        GUI.DrawTexture(PlayerStats.ScaleRect(baseR.x + 4f, baseR.y + 4f, baseR.width - 8f, 44f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        stats.DrawStyledText(
            PlayerStats.ScaleRect(baseR.x + 14f, baseR.y + 8f, baseR.width - 150f, 36f),
            spec.DisplayName.ToUpperInvariant(), 16, TextAnchor.MiddleLeft, spec.PrimaryColor);

        string pillTxt = isEquipped ? "✔ EQUIPADO" : (isUnlocked ? "DESBLOQUEADO" : $"● {spec.GoldCost} ORO");
        Color pillCol = isEquipped ? stats.ColHpGreen : (isUnlocked ? stats.ColCyanNexo : (canAfford ? stats.ColGoldYel : new Color(1f, 0.45f, 0.45f)));
        stats.DrawStyledText(
            PlayerStats.ScaleRect(baseR.x + baseR.width - 144f, baseR.y + 8f, 130f, 36f),
            pillTxt, 13, TextAnchor.MiddleRight, pillCol);

        // Miniatura del guantelete
        Rect thumbFrame = PlayerStats.ScaleRect(baseR.x + 14f, baseR.y + 58f, 128f, 128f);
        stats.DrawCyberBox(thumbFrame, new Color(0.01f, 0.02f, 0.05f, 0.96f), new Color(spec.PrimaryColor.r, spec.PrimaryColor.g, spec.PrimaryColor.b, 0.7f), 1.4f);

        Texture2D tex = gauntletTextures[(int)spec.Variant];
        if (tex != null)
        {
            if (holoGlowTex != null)
            {
                GUI.color = new Color(spec.PrimaryColor.r, spec.PrimaryColor.g, spec.PrimaryColor.b, 0.28f);
                GUI.DrawTexture(thumbFrame, holoGlowTex);
                GUI.color = Color.white;
            }
            GUI.DrawTexture(PlayerStats.ScaleRect(baseR.x + 20f, baseR.y + 64f, 116f, 116f), tex, ScaleMode.ScaleToFit);
        }

        // Texto descriptivo y efecto
        stats.DrawStyledText(
            PlayerStats.ScaleRect(baseR.x + 154f, baseR.y + 58f, baseR.width - 168f, 24f),
            $"★ {spec.PassiveTitle}", 14, TextAnchor.MiddleLeft, stats.ColGoldYel);
        stats.DrawStyledText(
            PlayerStats.ScaleRect(baseR.x + 154f, baseR.y + 84f, baseR.width - 168f, 88f),
            spec.PassiveSummary, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.93f, 1f), wordWrap: true);

        // Botones inferiores de la tarjeta (Visor + Comprar/Equipar)
        if (stats.DrawSkinnedCyberButton(
            new Rect(baseR.x + 14f, baseR.y + 202f, 166f, 92f),
            stats.TexBtnCyan,
            isSelected ? "👁 EN VISOR" : "👁 PROBAR / VER",
            spec.ElementTag,
            13, 10,
            isSelected ? stats.ColGoldYel : Color.white,
            spec.PrimaryColor, spec.PrimaryColor, true))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            selectedType = PreviewSelectionType.Gauntlet;
            previewGauntlet = spec.Variant;
            TriggerVisorTestBurst();
        }

        Rect buyBtnRect = new Rect(baseR.x + 190f, baseR.y + 202f, baseR.width - 204f, 92f);
        if (isEquipped)
        {
            if (stats.DrawSkinnedCyberButton(
                buyBtnRect, stats.TexBtnEmerald,
                "✔ ACTIVO EN COMBATE",
                "CLIC PARA REVISAR DETALLES",
                14, 11, Color.white, new Color(0.82f, 1f, 0.90f), stats.ColHpGreen, true))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                selectedType = PreviewSelectionType.Gauntlet;
                previewGauntlet = spec.Variant;
                TriggerVisorTestBurst();
            }
        }
        else if (isUnlocked)
        {
            if (stats.DrawSkinnedCyberButton(
                buyBtnRect, stats.TexBtnCyan,
                "⚡ EQUIPAR GUANTELETE",
                "APLICA GRADIENTES VFX AL INSTANTE",
                15, 11, Color.white, spec.PrimaryColor, spec.PrimaryColor, true))
            {
                ExecuteGauntletAction(stats, spec.Variant);
            }
        }
        else
        {
            string bTitle = canAfford ? $"🛒 COMPRAR ({spec.GoldCost} ORO)" : $"🔒 COSTO: {spec.GoldCost} ORO";
            string bSub = canAfford ? "DESBLOQUEAR Y EQUIPAR AHORA" : $"TE FALTAN {spec.GoldCost - stats.gold} ORO";
            if (stats.DrawSkinnedCyberButton(
                buyBtnRect, canAfford ? stats.TexBtnGold : stats.TexBtnCrimson,
                bTitle, bSub,
                15, 11,
                canAfford ? Color.white : new Color(1f, 0.72f, 0.72f),
                canAfford ? stats.ColGoldYel : new Color(1f, 0.84f, 0.84f),
                canAfford ? stats.ColGoldYel : Color.red,
                canAfford))
            {
                ExecuteGauntletAction(stats, spec.Variant);
            }
            else if (!canAfford && GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                // Permite hacer clic en cualquier parte de la tarjeta para previsualizar en el Visor 3D/2D
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                selectedType = PreviewSelectionType.Gauntlet;
                previewGauntlet = spec.Variant;
                TriggerVisorTestBurst();
            }
        }

        // Selección por clic en la zona superior de la tarjeta
        Rect cardHeaderZone = PlayerStats.ScaleRect(baseR.x, baseR.y, baseR.width, 194f);
        if (GUI.Button(cardHeaderZone, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            selectedType = PreviewSelectionType.Gauntlet;
            previewGauntlet = spec.Variant;
            TriggerVisorTestBurst();
        }
    }

    void DrawNexaDroneCard(PlayerStats stats, Rect baseR)
    {
        bool isSelected = (selectedType == PreviewSelectionType.NexaDrone);
        bool purchased = stats.hasPurchasedNexaDrone;
        bool canAfford = stats.gold >= PlayerStats.NEXA_DRONE_COST;

        Rect r = PlayerStats.ScaleRect(baseR.x, baseR.y, baseR.width, baseR.height);
        Color accent = new Color(0.22f, 0.94f, 1f);
        Color border = isSelected ? Color.white : (purchased ? stats.ColHpGreen : accent);

        stats.DrawCyberBox(r, new Color(0.02f, 0.05f, 0.11f, 0.97f), border, isSelected ? 3f : 2f);

        // Miniatura N.E.X.A.
        Rect iconRect = PlayerStats.ScaleRect(baseR.x + 18f, baseR.y + 18f, 140f, 140f);
        stats.DrawCyberBox(iconRect, new Color(0.01f, 0.02f, 0.05f, 0.95f), accent, 1.4f);
        if (nexaDroneTexture != null)
        {
            if (holoGlowTex != null)
            {
                GUI.color = new Color(accent.r, accent.g, accent.b, 0.28f);
                GUI.DrawTexture(iconRect, holoGlowTex);
                GUI.color = Color.white;
            }
            GUI.DrawTexture(PlayerStats.ScaleRect(baseR.x + 26f, baseR.y + 26f, 124f, 124f), nexaDroneTexture, ScaleMode.ScaleToFit);
        }

        if (GUI.Button(PlayerStats.ScaleRect(baseR.x + 12f, baseR.y + 12f, 680f, 152f), GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            selectedType = PreviewSelectionType.NexaDrone;
            TriggerVisorTestBurst();
        }

        stats.DrawStyledText(
            PlayerStats.ScaleRect(baseR.x + 174f, baseR.y + 16f, 520f, 34f),
            "🤖 DRON CENTINELA N.E.X.A. — UNIDAD DE ASISTENCIA",
            19, TextAnchor.MiddleLeft, purchased ? stats.ColHpGreen : accent);

        stats.DrawStyledText(
            PlayerStats.ScaleRect(baseR.x + 174f, baseR.y + 52f, 520f, 108f),
            "Compañero robot orbital que dispara proyectiles de plasma teledirigidos contra enemigos cercanos y activa el Protocolo Baluarte (escudo que absorbe 3 impactos cuando tu HP cae debajo del 25%). Debe adquirirse en tienda para desplegarse.",
            14, TextAnchor.UpperLeft, new Color(0.88f, 0.94f, 1f), wordWrap: true);

        Rect actionR = new Rect(baseR.x + 706f, baseR.y + 34f, baseR.width - 724f, 108f);
        if (purchased)
        {
            stats.DrawSkinnedCyberButton(
                actionR, stats.TexBtnEmerald,
                "✔ UNIDAD N.E.X.A. ACTIVA",
                "DESPLEGADA JUNTO A KAEL",
                16, 12, Color.white, new Color(0.82f, 1f, 0.90f), stats.ColHpGreen, false);
        }
        else
        {
            string title = canAfford
                ? $"🛒 COMPRAR DRON ({PlayerStats.NEXA_DRONE_COST} ORO)"
                : $"🔒 BLOQUEADO ({PlayerStats.NEXA_DRONE_COST} ORO)";
            string sub = canAfford
                ? "DESPLEGAR UNIDAD ROBOT EN COMBATE"
                : $"TE FALTAN {PlayerStats.NEXA_DRONE_COST - stats.gold} ORO";

            if (stats.DrawSkinnedCyberButton(
                actionR, canAfford ? stats.TexBtnGold : stats.TexBtnCrimson,
                title, sub,
                16, 12,
                canAfford ? Color.white : new Color(1f, 0.75f, 0.75f),
                canAfford ? stats.ColGoldYel : new Color(1f, 0.84f, 0.84f),
                canAfford ? stats.ColGoldYel : Color.red,
                canAfford))
            {
                ExecuteNexaDronePurchase(stats);
            }
        }
    }

    void ExecuteGauntletAction(PlayerStats stats, GauntletVariant variant)
    {
        selectedType = PreviewSelectionType.Gauntlet;
        previewGauntlet = variant;

        bool wasUnlocked = stats.IsGauntletUnlocked(variant);
        if (stats.PurchaseOrEquipGauntlet(variant))
        {
            if (AudioManager.Instance != null)
            {
                if (!wasUnlocked) AudioManager.Instance.PlayLevelUp();
                else AudioManager.Instance.PlayUIClick();
            }

            GauntletSpec spec = PlayerStats.GetGauntletSpec(variant);
            TriggerVisorTestBurst();
            statusBannerMsg = wasUnlocked
                ? $"⚡ {spec.DisplayName} equipado — Gradientes VFX y pasivas actualizados"
                : $"🛒 ¡{spec.DisplayName} adquirido y equipado en el Guantelete Nexo!";
            statusBannerColor = spec.PrimaryColor;
            statusBannerTimer = 2.6f;

            if (CombatVFXManager.Instance != null && KaelAutoController.Instance != null)
            {
                Vector3 kaelCenter = KaelAutoController.Instance.transform.position + Vector3.up * 0.8f;
                CombatVFXManager.Instance.SpawnShockwaveRing(kaelCenter, spec.PrimaryColor, 1.9f);
                CombatVFXManager.Instance.SpawnHitSparks(kaelCenter, true, 12);
            }
        }
    }

    void ExecuteNexaDronePurchase(PlayerStats stats)
    {
        selectedType = PreviewSelectionType.NexaDrone;
        if (stats.PurchaseNexaDrone())
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelUp();
            TriggerVisorTestBurst();
            statusBannerMsg = "🤖 ¡Dron N.E.X.A. adquirido y desplegado en el escuadrón!";
            statusBannerColor = new Color(0.22f, 0.96f, 1f);
            statusBannerTimer = 2.8f;
        }
    }

    float CalculateProjectedCrit(PlayerStats stats, GauntletVariant candidateVariant)
    {
        float baseWithoutGauntlet = 0.05f + ((stats.levelCrit - 1) * 0.04f) + (stats.branchFuria * 0.05f) + (stats.levelSeismicEdge * 0.03f);
        float gauntletBonus = (candidateVariant == GauntletVariant.IgneousPlasma) ? PlayerStats.IGNEOUS_CRIT_BONUS : 0f;
        return Mathf.Clamp(baseWithoutGauntlet + gauntletBonus, 0.05f, 0.95f);
    }
}
