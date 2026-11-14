using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Variantes tácticas desbloqueables del Guantelete Nexo en el Arsenal de Vanguardia (Issue #21).
/// </summary>
public enum GauntletVariant
{
    PrototypeCyan = 0,
    PyroReactor = 1,
    IgneousPlasma = 2,
    VorstVoid = 3
}

/// <summary>
/// Especificación técnica, visual y económica de cada variante de Guantelete Nexo.
/// </summary>
public readonly struct GauntletSpec
{
    public readonly GauntletVariant Variant;
    public readonly string CodeName;
    public readonly string DisplayName;
    public readonly string ElementTag;
    public readonly int GoldCost;
    public readonly string PassiveTitle;
    public readonly string PassiveSummary;
    public readonly string CombatSpecsLine;
    public readonly Color PrimaryColor;
    public readonly Color SecondaryColor;
    public readonly Color CoreColor;
    public readonly string AccentHex;
    public readonly string TextureResourcePath;

    public GauntletSpec(
        GauntletVariant variant,
        string codeName,
        string displayName,
        string elementTag,
        int goldCost,
        string passiveTitle,
        string passiveSummary,
        string combatSpecsLine,
        Color primaryColor,
        Color secondaryColor,
        Color coreColor,
        string accentHex,
        string textureResourcePath)
    {
        Variant = variant;
        CodeName = codeName;
        DisplayName = displayName;
        ElementTag = elementTag;
        GoldCost = goldCost;
        PassiveTitle = passiveTitle;
        PassiveSummary = passiveSummary;
        CombatSpecsLine = combatSpecsLine;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        CoreColor = coreColor;
        AccentHex = accentHex;
        TextureResourcePath = textureResourcePath;
    }
}

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    public const int NEXA_DRONE_COST = 120;
    public const float IGNEOUS_CRIT_BONUS = 0.15f;
    public const float VORST_LIFESTEAL_RATIO = 0.05f;
    public const float PYRO_BURN_DURATION = 3.5f;
    public const float PYRO_BURN_TICK_INTERVAL = 0.5f;

    public static readonly GauntletSpec[] GauntletCatalog = new GauntletSpec[]
    {
        new GauntletSpec(
            GauntletVariant.PrototypeCyan,
            "MK-I PROTOTIPO",
            "Guantelete Prototipo (Cian)",
            "ELÉCTRICO NEXO",
            0,
            "Estabilidad Táctica Nexo",
            "Balance energético estándar de fábrica con descargas eléctricas cian originales de Kael.",
            "CRIT +0%  |  QUEMADURA: NO  |  ROBO VIDA: 0%  |  VFX: CIAN",
            new Color(0.20f, 0.94f, 1.00f),
            new Color(0.08f, 0.50f, 1.00f),
            Color.white,
            "#33F0FF",
            "StoryUI/Gauntlets/Gauntlet_Prototype_Cyan"
        ),
        new GauntletSpec(
            GauntletVariant.PyroReactor,
            "MK-II PIRO-REACTOR",
            "Guantelete Piro-Reactor (Carmesí)",
            "TÉRMICO / FUEGO",
            80,
            "Combustión Continua (DoT)",
            "Inyecta plasma térmico en cada golpe y Ultimate: inflige quemadura continua por tick de fuego durante 3.5s.",
            "QUEMADURA: 7 TICKS (3.5s)  |  DAÑO FUEGO CONTINUO  |  VFX: CARMESÍ",
            new Color(1.00f, 0.22f, 0.14f),
            new Color(1.00f, 0.56f, 0.06f),
            new Color(1.00f, 0.90f, 0.55f),
            "#FF4733",
            "StoryUI/Gauntlets/Gauntlet_Pyro_Crimson"
        ),
        new GauntletSpec(
            GauntletVariant.IgneousPlasma,
            "MK-III PLASMA ÍGNEO",
            "Guantelete de Plasma Ígneo (Dorado)",
            "SOBRECARGA SOLAR",
            140,
            "Calibración Letal (+15% Crítico)",
            "Sobrecarga los nudillos con filamentos de plasma dorado que elevan la probabilidad crítica en +15%.",
            "CRIT +15% PERMANENTE  |  CHISPAS SOLARES  |  VFX: DORADO",
            new Color(1.00f, 0.84f, 0.16f),
            new Color(1.00f, 0.48f, 0.04f),
            new Color(1.00f, 0.98f, 0.78f),
            "#FFD629",
            "StoryUI/Gauntlets/Gauntlet_Plasma_Gold"
        ),
        new GauntletSpec(
            GauntletVariant.VorstVoid,
            "MK-IV VACÍO VORST",
            "Guantelete de Vacío Vorst (Púrpura)",
            "SINGULARIDAD VORST",
            200,
            "Sifón Biotecnológico (5% Robo de Vida)",
            "Canaliza energía de núcleo Vorst en el impacto: absorbe el 5% del daño infligido como recuperación de HP.",
            "ROBO DE VIDA: 5% DEL DAÑO  |  IMPACTO ABISAL  |  VFX: PÚRPURA",
            new Color(0.76f, 0.22f, 1.00f),
            new Color(0.98f, 0.16f, 0.68f),
            new Color(0.95f, 0.82f, 1.00f),
            "#C947FF",
            "StoryUI/Gauntlets/Gauntlet_Void_Purple"
        )
    };

    public static GauntletSpec GetGauntletSpec(GauntletVariant variant)
    {
        int idx = Mathf.Clamp((int)variant, 0, GauntletCatalog.Length - 1);
        return GauntletCatalog[idx];
    }

    [Header("Ajuste Visual de Barra sobre Kael")]
    [Tooltip("Altura de la barra de vida sobre los pies de Kael (1.95 queda arriba del cabello)")]
    [Range(1.2f, 3.0f)] public float healthBarOffsetY = 1.95f;

    [Header("Configuración del Nivel Actual")]
    public int levelNumber = 1;
    public string zoneName = "SECTOR 7 — CIUDAD EN RUINAS";
    public string nextLevelSceneName = "Lab_Zahir";

    [Header("Estados Oficiales del Nivel")]
    public bool isPaused = false;
    public bool isGameOver = false;
    public bool isChoosingDilemma = false;
    public bool isChoosingSkill = false;
    public bool isUpgradeTreeOpen = false;
    public bool isArsenalShopOpen = false;
    public bool isLevelCompleted = false;

    [Header("Arsenal de Vanguardia (Issue #21): Guanteletes y Dron N.E.X.A.")]
    public GauntletVariant equippedGauntlet = GauntletVariant.PrototypeCyan;
    public int unlockedGauntletsMask = 1; // Bit 0 (Prototipo Cian) desbloqueado por defecto
    public bool hasPurchasedNexaDrone = false;

    public bool IsCodexModalOpen => VorstCodexUI.Instance != null && VorstCodexUI.Instance.isOpen;

    public bool IsAnyBlockingModalOpen =>
        isPaused || isGameOver || isLevelCompleted || isChoosingDilemma || isChoosingSkill || isUpgradeTreeOpen || isArsenalShopOpen || IsCodexModalOpen;

    [Header("Economía, Nivel y Moral (Compasión vs Rigor)")]
    public int gold = 0;
    public int playerLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 25;
    public int skillPoints = 0;
    public int compassionPoints = 0;
    public int rigorPoints = 0;

    [Header("Rama 2: 3 Sendas del Guantelete Nexo (Puntos de Nivel)")]
    public int branchFuria = 0;    // Senda de la Furia: Aumenta daño crítico y ataque
    public int branchGuardian = 0; // Senda del Guardián: Regeneración de vida y HP
    public int branchNexo = 0;     // Senda del Nexo: Carga más rápida de la Ultimate

    [Header("Rama 1: 5 Atributos Base (Comprables con Oro)")]
    public int levelAtk = 1;  // Fuerza Nexo
    public int levelHp = 1;   // Vigor
    public int levelSpd = 1;  // Reflejos
    public int levelCrit = 1; // Instinto
    public int levelGold = 1; // Carroñero

    [Header("Rama 3: Arsenal y Armadura (Comprables con Oro)")]
    public int levelArmor = 0;       // Coraza de Titanio (+Defensa y +HP)
    public int levelThrusters = 0;   // Propulsores de Asalto (+Velocidad de Movimiento y +Cadencia)
    public int levelOvercharge = 0;  // Módulo de Sobrecarga (+Carga de Ultimate y +Daño Ult)
    public int levelSeismicEdge = 0; // Filo Sísmico Vorst (+Daño Golpe 3 del Combo y +Crítico)

    private int selectedTreeTab = 0; // 0 = Rama 1 (Atributos), 1 = Rama 2 (Evolución Nexo), 2 = Rama 3 (Arsenal y Armadura)
    private string choiceSummary = "";
    private float levelTimer = 0f;

    [Header("Bloque 2 (Issue #17): Racha de Combos y Modo Sobremarcha Nexo (Overdrive)")]
    public int comboCount = 0;
    public float comboDecayTimer = 0f;
    public const float MAX_COMBO_DECAY = 4.5f;
    public bool isOverdriveActive => comboCount >= 20;
    private float comboPunchTimer = 0f;
    private bool wasOverdriveActive = false;

    [Header("Carga de Habilidad Ultimate (0 a 100)")]
    public float currentUltCharge = 0f;
    public float maxUltCharge = 100f;
    public float ultCharge
    {
        get => currentUltCharge;
        set => currentUltCharge = Mathf.Clamp(value, 0f, maxUltCharge);
    }
    public bool IsUltReady => currentUltCharge >= maxUltCharge;

    [Header("Estadísticas Calculadas")]
    public float maxHP = 100f;
    public float currentHP = 100f;
    public float hpRegenPerSec = 1.0f;
    public float attackDamage = 12f;
    public float attackRate = 1.2f;
    public float critChance = 0.05f;
    public float goldMultiplier = 1.0f;
    public float armorDamageReduction = 0.0f;
    public float moveSpeedMultiplier = 1.0f;
    public float ultimateDamageMultiplier = 1.0f;
    public float comboFinisherMultiplier = 1.6f;

    public event Action OnStatsChanged;
    private Transform kaelTransform;

    // Texturas UI Cyberpunk de Alta Definición
    private Texture2D texGameOverBanner;
    private Texture2D texVictoryBanner;
    private Texture2D texPortraitKael;
    private Texture2D texPortraitNexaAlert;
    private Texture2D texBtnCyan;
    private Texture2D texBtnCrimson;
    private Texture2D texBtnGold;
    private Texture2D texBtnEmerald;
    private Texture2D texBtnPurple;
    private Texture2D texPanelCyan;
    private Texture2D texPanelCrimson;
    private Texture2D texPanelGold;

    public Texture2D TexBtnCyan => texBtnCyan;
    public Texture2D TexBtnCrimson => texBtnCrimson;
    public Texture2D TexBtnGold => texBtnGold;
    public Texture2D TexBtnEmerald => texBtnEmerald;
    public Texture2D TexBtnPurple => texBtnPurple;
    public Texture2D TexPanelCyan => texPanelCyan;
    public Texture2D TexPanelCrimson => texPanelCrimson;
    public Texture2D TexPanelGold => texPanelGold;

    [Header("Catálogo de Iconos Pixel-Art HD del Árbol de Habilidades (Issue #20)")]
    private Texture2D iconFuerzaNexo;
    private Texture2D iconVigorVital;
    private Texture2D iconBlindajeTactico;
    private Texture2D iconCadenciaImpacto;
    private Texture2D iconRecoleccion;
    private Texture2D iconTsunamiSismico;
    private Texture2D iconImpactoMeteoro;
    private Texture2D iconFuriaContinua;
    private Texture2D iconVinculoFraternal;
    private Texture2D iconDisonanciaVorst;
    private Texture2D iconArmoniaHumana;
    private Texture2D iconPropulsores;
    private Texture2D iconSobrecarga;

    // Paleta Cyberpunk Nexo
    private readonly Color colBgDark   = new Color(0.04f, 0.06f, 0.12f, 0.94f);
    private readonly Color colBorder   = new Color(0.22f, 0.88f, 1.00f, 0.88f);
    private readonly Color colHpGreen  = new Color(0.18f, 0.94f, 0.50f, 1.00f);
    private readonly Color colExpPurp  = new Color(0.70f, 0.40f, 1.00f, 1.00f);
    private readonly Color colGoldYel  = new Color(1.00f, 0.82f, 0.22f, 1.00f);
    private readonly Color colCyanNexo = new Color(0.22f, 0.90f, 1.00f, 1.00f);

    public Color ColBgDark => colBgDark;
    public Color ColBorder => colBorder;
    public Color ColHpGreen => colHpGreen;
    public Color ColExpPurp => colExpPurp;
    public Color ColGoldYel => colGoldYel;
    public Color ColCyanNexo => colCyanNexo;

    // Colores diferenciados por atributo
    private readonly Color colAttrFuerza    = new Color(1.00f, 0.36f, 0.28f, 1.00f);
    private readonly Color colAttrVigor     = new Color(0.18f, 0.94f, 0.52f, 1.00f);
    private readonly Color colAttrReflejos  = new Color(0.22f, 0.90f, 1.00f, 1.00f);
    private readonly Color colAttrInstinto  = new Color(0.86f, 0.42f, 1.00f, 1.00f);
    private readonly Color colAttrCarronero = new Color(1.00f, 0.82f, 0.22f, 1.00f);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;

        LoadCyberUITextures();
        ArsenalShopUI.EnsureExists();
        VorstCodexUI.EnsureExists();

        string activeScene = SceneManager.GetActiveScene().name;
        if (activeScene == "03_Nivel3_Colmena")
        {
            levelNumber = 3;
            zoneName = "NAVE NÚCLEO VORST — COLMENA DE BIO-CABLES";
            nextLevelSceneName = "";
            ResetSkillsToBase();
            LoadProgressIfContinuing();
        }
        else if (activeScene == "Lab_Zahir")
        {
            levelNumber = 2;
            zoneName = "BASE VORST — NÚCLEO CENTRAL";
            nextLevelSceneName = "03_Nivel3_Colmena";
            ResetSkillsToBase();
            LoadProgressIfContinuing();
        }

        RecalculateStats();
        currentHP = maxHP;
        SyncEquippedGauntletVisuals();
    }

    void LoadCyberUITextures()
    {
        texGameOverBanner    = LoadResTex("StoryUI/GameOver_Banner");
        texVictoryBanner     = LoadResTex("StoryUI/Victory_Banner");
        texPortraitKael      = LoadResTex("StoryUI/Portrait_Kael_Normal");
        texPortraitNexaAlert = LoadResTex("StoryUI/Portrait_Nexa_Alert");

        texBtnCyan     = LoadResTex("StoryUI/Btn_Cyber_Cyan");
        texBtnCrimson  = LoadResTex("StoryUI/Btn_Cyber_Crimson");
        texBtnGold     = LoadResTex("StoryUI/Btn_Cyber_Gold");
        texBtnEmerald  = LoadResTex("StoryUI/Btn_Cyber_Emerald");
        texBtnPurple   = LoadResTex("StoryUI/Btn_Cyber_Purple");

        texPanelCyan    = LoadResTex("StoryUI/Panel_Cyber_Cyan");
        texPanelCrimson = LoadResTex("StoryUI/Panel_Cyber_Crimson");
        texPanelGold    = LoadResTex("StoryUI/Panel_Cyber_Gold");

        // Iconos Pixel-Art HD del Árbol de Habilidades (StoryUI/Icons/)
        iconFuerzaNexo       = LoadResTex("StoryUI/Icons/Icon_FuerzaNexo");
        iconVigorVital       = LoadResTex("StoryUI/Icons/Icon_VigorVital");
        iconBlindajeTactico  = LoadResTex("StoryUI/Icons/Icon_BlindajeTactico");
        iconCadenciaImpacto  = LoadResTex("StoryUI/Icons/Icon_CadenciaImpacto");
        iconRecoleccion      = LoadResTex("StoryUI/Icons/Icon_Recoleccion");
        iconTsunamiSismico   = LoadResTex("StoryUI/Icons/Icon_TsunamiSismico");
        iconImpactoMeteoro   = LoadResTex("StoryUI/Icons/Icon_ImpactoMeteoro");
        iconFuriaContinua    = LoadResTex("StoryUI/Icons/Icon_FuriaContinua");
        iconVinculoFraternal = LoadResTex("StoryUI/Icons/Icon_VinculoFraternal");
        iconDisonanciaVorst  = LoadResTex("StoryUI/Icons/Icon_DisonanciaVorst");
        iconArmoniaHumana    = LoadResTex("StoryUI/Icons/Icon_ArmoniaHumana");
        iconPropulsores      = LoadResTex("StoryUI/Icons/Icon_Propulsores");
        iconSobrecarga       = LoadResTex("StoryUI/Icons/Icon_Sobrecarga");
    }

    public static Texture2D LoadResTex(string path)
    {
        Texture2D t = Resources.Load<Texture2D>(path);
        if (t != null) return t;
        Sprite sp = Resources.Load<Sprite>(path);
        return (sp != null) ? sp.texture : null;
    }

    public void ResetSkillsToBase()
    {
        gold = 0;
        playerLevel = 1;
        currentExp = 0;
        expToNextLevel = 25;
        skillPoints = 0;
        compassionPoints = 0;
        rigorPoints = 0;
        branchFuria = 0;
        branchGuardian = 0;
        branchNexo = 0;
        levelAtk = 1;
        levelHp = 1;
        levelSpd = 1;
        levelCrit = 1;
        levelGold = 1;
        levelArmor = 0;
        levelThrusters = 0;
        levelOvercharge = 0;
        levelSeismicEdge = 0;
        currentUltCharge = 0f;
        equippedGauntlet = GauntletVariant.PrototypeCyan;
        unlockedGauntletsMask = 1;
        hasPurchasedNexaDrone = false;
    }

    public void SaveProgressForNextLevel()
    {
        PlayerPrefs.SetInt("KAEL_Saved", 1);
        PlayerPrefs.SetInt("KAEL_UnlockedLevel", Mathf.Max(PlayerPrefs.GetInt("KAEL_UnlockedLevel", 1), levelNumber + 1));
        PlayerPrefs.SetInt("KAEL_Gold", gold);
        PlayerPrefs.SetInt("KAEL_Lv", playerLevel);
        PlayerPrefs.SetInt("KAEL_Exp", currentExp);
        PlayerPrefs.SetInt("KAEL_ExpNext", expToNextLevel);
        PlayerPrefs.SetInt("KAEL_SkillPts", skillPoints);
        PlayerPrefs.SetInt("KAEL_Compassion", compassionPoints);
        PlayerPrefs.SetInt("KAEL_Rigor", rigorPoints);
        PlayerPrefs.SetInt("KAEL_Furia", branchFuria);
        PlayerPrefs.SetInt("KAEL_Guardian", branchGuardian);
        PlayerPrefs.SetInt("KAEL_Nexo", branchNexo);
        PlayerPrefs.SetInt("KAEL_LvAtk", levelAtk);
        PlayerPrefs.SetInt("KAEL_LvHp", levelHp);
        PlayerPrefs.SetInt("KAEL_LvSpd", levelSpd);
        PlayerPrefs.SetInt("KAEL_LvCrit", levelCrit);
        PlayerPrefs.SetInt("KAEL_LvGold", levelGold);
        PlayerPrefs.SetInt("KAEL_LvArmor", levelArmor);
        PlayerPrefs.SetInt("KAEL_LvThrusters", levelThrusters);
        PlayerPrefs.SetInt("KAEL_LvOvercharge", levelOvercharge);
        PlayerPrefs.SetInt("KAEL_LvSeismic", levelSeismicEdge);
        PlayerPrefs.SetInt("KAEL_EquippedGauntlet", (int)equippedGauntlet);
        PlayerPrefs.SetInt("KAEL_UnlockedGauntlets", unlockedGauntletsMask | 1);
        PlayerPrefs.SetInt("KAEL_NexaDroneBought", hasPurchasedNexaDrone ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadProgressIfContinuing()
    {
        if (levelNumber > 1)
        {
            if (PlayerPrefs.GetInt("KAEL_Saved", 0) == 1)
            {
                gold = PlayerPrefs.GetInt("KAEL_Gold", gold);
                playerLevel = PlayerPrefs.GetInt("KAEL_Lv", playerLevel);
                currentExp = PlayerPrefs.GetInt("KAEL_Exp", currentExp);
                expToNextLevel = PlayerPrefs.GetInt("KAEL_ExpNext", expToNextLevel);
                skillPoints = PlayerPrefs.GetInt("KAEL_SkillPts", skillPoints);
                compassionPoints = PlayerPrefs.GetInt("KAEL_Compassion", compassionPoints);
                rigorPoints = PlayerPrefs.GetInt("KAEL_Rigor", rigorPoints);
                branchFuria = PlayerPrefs.GetInt("KAEL_Furia", branchFuria);
                branchGuardian = PlayerPrefs.GetInt("KAEL_Guardian", branchGuardian);
                branchNexo = PlayerPrefs.GetInt("KAEL_Nexo", branchNexo);
                levelAtk = PlayerPrefs.GetInt("KAEL_LvAtk", levelAtk);
                levelHp = PlayerPrefs.GetInt("KAEL_LvHp", levelHp);
                levelSpd = PlayerPrefs.GetInt("KAEL_LvSpd", levelSpd);
                levelCrit = PlayerPrefs.GetInt("KAEL_LvCrit", levelCrit);
                levelGold = PlayerPrefs.GetInt("KAEL_LvGold", levelGold);
                levelArmor = PlayerPrefs.GetInt("KAEL_LvArmor", levelArmor);
                levelThrusters = PlayerPrefs.GetInt("KAEL_LvThrusters", levelThrusters);
                levelOvercharge = PlayerPrefs.GetInt("KAEL_LvOvercharge", levelOvercharge);
                levelSeismicEdge = PlayerPrefs.GetInt("KAEL_LvSeismic", levelSeismicEdge);
                unlockedGauntletsMask = PlayerPrefs.GetInt("KAEL_UnlockedGauntlets", 1) | 1;
                int eqIdx = Mathf.Clamp(PlayerPrefs.GetInt("KAEL_EquippedGauntlet", 0), 0, GauntletCatalog.Length - 1);
                equippedGauntlet = IsGauntletUnlocked((GauntletVariant)eqIdx) ? (GauntletVariant)eqIdx : GauntletVariant.PrototypeCyan;
                hasPurchasedNexaDrone = PlayerPrefs.GetInt("KAEL_NexaDroneBought", 0) == 1;
            }
            else if (playerLevel == 1 && levelAtk == 1)
            {
                playerLevel = 3;
                levelAtk = 3;
                levelHp = 3;
                gold = 120;
                skillPoints = 1;
            }
        }
    }

    void Start()
    {
        KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
        if (kael != null) kaelTransform = kael.transform;
        SyncEquippedGauntletVisuals();
    }

    void Update()
    {
        if (!IsAnyBlockingModalOpen)
        {
            levelTimer += Time.deltaTime;

            if (currentHP > 0f && currentHP < maxHP)
            {
                currentHP = Mathf.Min(maxHP, currentHP + hpRegenPerSec * Time.deltaTime);
            }

            // BLOQUE 2 (Issue #17): Decaimiento de racha de combos (4.5s)
            if (comboCount > 0)
            {
                comboDecayTimer -= Time.deltaTime;
                if (comboDecayTimer <= 0f)
                {
                    comboCount = 0;
                    comboDecayTimer = 0f;
                    wasOverdriveActive = false;
                }
            }
            if (comboPunchTimer > 0f)
            {
                comboPunchTimer = Mathf.Max(0f, comboPunchTimer - Time.unscaledDeltaTime * 3.5f);
            }
        }
    }

    public void RegisterComboHit()
    {
        comboCount++;
        comboDecayTimer = MAX_COMBO_DECAY;
        comboPunchTimer = 0.22f;

        if (isOverdriveActive && !wasOverdriveActive)
        {
            wasOverdriveActive = true;
            if (CombatVFXManager.Instance != null && kaelTransform != null)
            {
                CombatVFXManager.Instance.TriggerScreenFlash(new Color(0.22f, 0.95f, 1f), 0.38f);
                CombatVFXManager.Instance.SpawnShockwaveRing(kaelTransform.position + Vector3.up * 0.6f, new Color(1f, 0.25f, 0.85f), 2.8f);
                CombatVFXManager.Instance.SpawnFloatingText(
                    kaelTransform.position + Vector3.up * (healthBarOffsetY + 0.55f),
                    "⚡ ¡SOBREMARCHA NEXO (OVERDRIVE)! ⚡",
                    new Color(1f, 0.35f, 0.92f), 23, 1.3f
                );
            }
        }
    }

    public void HealPercent(float pct)
    {
        if (currentHP <= 0f) return;
        float healAmt = Mathf.Max(1f, Mathf.Round(maxHP * pct));
        currentHP = Mathf.Min(maxHP, currentHP + healAmt);
        OnStatsChanged?.Invoke();

        if (CombatVFXManager.Instance != null && kaelTransform != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(
                kaelTransform.position + Vector3.up * (healthBarOffsetY + 0.35f),
                $"✨ +{Mathf.RoundToInt(healAmt)} HP (PUREZA NEXO)",
                new Color(0.25f, 1f, 0.65f), 18, 0.95f
            );
        }
    }

    public void RecalculateStats()
    {
        float hpBonus = (compassionPoints > 0) ? (1f + compassionPoints * 0.20f) : 1.0f;
        float atkBonus = ((rigorPoints > 0) ? (1f + rigorPoints * 0.25f) : 1.0f) * (1f + branchFuria * 0.18f);

        maxHP = Mathf.RoundToInt((100f + ((levelHp - 1) * 35f) + ((playerLevel - 1) * 20f) + (branchGuardian * 40f) + (levelArmor * 28f)) * hpBonus);
        hpRegenPerSec = 1.0f + (branchGuardian * 3.5f) + (levelArmor * 0.6f);
        attackDamage = Mathf.RoundToInt((12f + ((levelAtk - 1) * 6f) * Mathf.Pow(1.08f, levelAtk - 1) + ((playerLevel - 1) * 4f)) * atkBonus);
        attackRate = (1.2f + ((levelSpd - 1) * 0.14f)) * (1f + levelThrusters * 0.06f);
        critChance = Mathf.Clamp(
            0.05f + ((levelCrit - 1) * 0.04f) + (branchFuria * 0.05f) + (levelSeismicEdge * 0.03f)
            + (equippedGauntlet == GauntletVariant.IgneousPlasma ? IGNEOUS_CRIT_BONUS : 0f),
            0.05f, 0.95f);
        goldMultiplier = 1.0f + ((levelGold - 1) * 0.20f);

        armorDamageReduction = Mathf.Clamp(levelArmor * 0.06f, 0f, 0.35f);
        moveSpeedMultiplier = 1.0f + (levelThrusters * 0.10f);
        ultimateDamageMultiplier = 1.0f + (levelOvercharge * 0.12f);
        comboFinisherMultiplier = 1.6f + (levelSeismicEdge * 0.20f);

        OnStatsChanged?.Invoke();
    }

    public int GetUpgradeCost(int currentLv)
    {
        return Mathf.RoundToInt(20f * Mathf.Pow(1.18f, currentLv - 1));
    }

    public int GetArsenalCost(int baseCost, int currentLv)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(1.45f, currentLv));
    }

    public int CountAvailableUpgrades()
    {
        int count = 0;
        if (gold >= GetUpgradeCost(levelAtk)) count++;
        if (gold >= GetUpgradeCost(levelHp)) count++;
        if (gold >= GetUpgradeCost(levelSpd)) count++;
        if (gold >= GetUpgradeCost(levelCrit)) count++;
        if (gold >= GetUpgradeCost(levelGold)) count++;

        if (levelArmor < 5 && gold >= GetArsenalCost(30, levelArmor)) count++;
        if (levelThrusters < 5 && gold >= GetArsenalCost(28, levelThrusters)) count++;
        if (levelOvercharge < 5 && gold >= GetArsenalCost(38, levelOvercharge)) count++;
        if (levelSeismicEdge < 5 && gold >= GetArsenalCost(42, levelSeismicEdge)) count++;

        return count;
    }

    public void AddRewards(int baseGold, int expAmount)
    {
        // BLOQUE 2 (Issue #17): +30% Bono de Oro al derrotar enemigos durante Sobremarcha Nexo (Overdrive)
        float effectiveGoldMult = goldMultiplier * (isOverdriveActive ? 1.30f : 1.0f);
        int finalGold = Mathf.RoundToInt(baseGold * effectiveGoldMult);
        gold += finalGold;
        currentExp += expAmount;

        AddUltCharge(12f);

        bool leveledUp = false;
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            playerLevel++;
            skillPoints++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.35f);
            RecalculateStats();
            currentHP = maxHP;
            leveledUp = true;

            if (CombatVFXManager.Instance != null && kaelTransform != null)
            {
                CombatVFXManager.Instance.SpawnFloatingText(
                    kaelTransform.position + Vector3.up * (healthBarOffsetY + 0.4f),
                    $"★ ¡NIVEL {playerLevel}! (+1 PTO EVOLUCIÓN) ★",
                    colCyanNexo, 22, 1.4f
                );
            }
        }

        if (leveledUp && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelUp();
        }

        OnStatsChanged?.Invoke();
    }

    public void AddUltCharge(float amount)
    {
        float nexoBoost = 1f + (branchNexo * 0.25f) + (levelOvercharge * 0.18f);
        currentUltCharge = Mathf.Clamp(currentUltCharge + (amount * nexoBoost), 0f, maxUltCharge);
        OnStatsChanged?.Invoke();
    }

    public bool ConsumeUltCharge()
    {
        if (!IsUltReady) return false;
        currentUltCharge = 0f;
        OnStatsChanged?.Invoke();
        return true;
    }

    public void TakeDamage(float dmg, EnemyDummy attacker = null)
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma || isChoosingSkill || isUpgradeTreeOpen || isArsenalShopOpen) return;

        // BLOQUE 1 (Issue #16): Verificar Ventana de Bloqueo Perfecto (Parry Nexo <= 0.35s)
        if (KaelAutoController.Instance != null && KaelAutoController.Instance.IsWithinPerfectParryWindow())
        {
            // ¡PARRY PERFECTO! Kael recibe 0 de daño y ejecuta ralentización temporal + contragolpe automático
            KaelAutoController.Instance.ExecuteParryRiposte(attacker);
            return;
        }

        // ISSUE #19 & #21: Solo si el Dron N.E.X.A. fue adquirido en el Arsenal, absorbe impactos con Protocolo Baluarte
        if (hasPurchasedNexaDrone && NexaDroneCompanion.Instance != null && NexaDroneCompanion.Instance.TryAbsorbDamage(dmg))
        {
            return;
        }

        bool guarded = (KaelAutoController.Instance != null && KaelAutoController.Instance.isGuarding);

        float finalDmg = dmg * (1f - armorDamageReduction);
        if (guarded)
        {
            // BLOQUE 1: Fuera de la ventana perfecta, mantener S reduce el daño en un 45% y reproduce sonido metálico
            finalDmg *= 0.55f;
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayParryClang(false);
        }
        else
        {
            // BLOQUE 2: Si Kael recibe daño directo sin bloquear, el contador de combo se rompe
            if (comboCount > 0)
            {
                comboCount = 0;
                comboDecayTimer = 0f;
                wasOverdriveActive = false;
            }
        }

        finalDmg = Mathf.Max(1f, finalDmg);

        bool baluarteReady = (hasPurchasedNexaDrone && NexaDroneCompanion.Instance != null && !NexaDroneCompanion.Instance.IsShieldActive && !NexaDroneCompanion.Instance.IsOverheated);
        if (baluarteReady && currentHP > maxHP * 0.25f && (currentHP - finalDmg) <= 0f)
        {
            currentHP = Mathf.Max(1f, maxHP * 0.12f);
        }
        else
        {
            currentHP = Mathf.Max(0f, currentHP - finalDmg);
        }

        OnStatsChanged?.Invoke();

        if (CombatVFXManager.Instance != null && kaelTransform != null)
        {
            string popTxt = guarded ? $"🛡 -{Mathf.RoundToInt(finalDmg)} (GUARDIA -45%)" : $"-{Mathf.RoundToInt(finalDmg)}";
            Color popCol = guarded ? colCyanNexo : new Color(1f, 0.25f, 0.3f);
            CombatVFXManager.Instance.SpawnFloatingText(
                kaelTransform.position + Vector3.up * (healthBarOffsetY + 0.2f),
                popTxt,
                popCol, guarded ? 16 : 15, 0.65f
            );
        }

        // ISSUE #19: Si la vida de Kael cae por debajo del 25% y el Dron fue comprado, despliega el Protocolo Baluarte
        if (hasPurchasedNexaDrone && currentHP > 0f && NexaDroneCompanion.Instance != null)
        {
            NexaDroneCompanion.Instance.CheckEmergencyShieldTrigger();
        }

        if (currentHP <= 0f)
        {
            isGameOver = true;
            Time.timeScale = 0f;
        }
    }

    public void RetryFromGameOver()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
            AudioManager.Instance.PlayCombatMusic();
        }

        isGameOver = false;
        isUpgradeTreeOpen = false;
        isChoosingSkill = false;
        RecalculateStats();
        currentHP = maxHP;
        Time.timeScale = 1f;

        if (NexaDroneCompanion.Instance != null)
        {
            NexaDroneCompanion.Instance.ResetCompanion();
        }

        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.RestartWavesKeepingProgress();
        }
    }

    public void TriggerEndOfZoneDilemma()
    {
        isUpgradeTreeOpen = false;
        isArsenalShopOpen = false;
        isChoosingSkill = false;
        isChoosingDilemma = true;
        Time.timeScale = 0f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVictoryDilemmaMusic();
        }
    }

    public void TogglePause()
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma) return;
        if (isUpgradeTreeOpen || isChoosingSkill || isArsenalShopOpen)
        {
            CloseAllUpgradeModals();
            return;
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ToggleUpgradeTreeModal()
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma || isPaused) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        isArsenalShopOpen = false;
        isUpgradeTreeOpen = !isUpgradeTreeOpen;
        isChoosingSkill = isUpgradeTreeOpen;
        if (isUpgradeTreeOpen && skillPoints > 0)
        {
            selectedTreeTab = 1;
        }
        Time.timeScale = isUpgradeTreeOpen ? 0f : 1f;
    }

    public void OpenUpgradeTreeAtTab(int tabIndex)
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma || isPaused) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        isArsenalShopOpen = false;
        selectedTreeTab = Mathf.Clamp(tabIndex, 0, 2);
        isUpgradeTreeOpen = true;
        isChoosingSkill = true;
        Time.timeScale = 0f;
    }

    public void ToggleArsenalShopModal()
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma || isPaused) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        isUpgradeTreeOpen = false;
        isChoosingSkill = false;
        isArsenalShopOpen = !isArsenalShopOpen;
        if (isArsenalShopOpen && ArsenalShopUI.Instance != null)
        {
            ArsenalShopUI.Instance.OnShopOpened(equippedGauntlet);
        }
        Time.timeScale = isArsenalShopOpen ? 0f : 1f;
    }

    public void OpenArsenalShopModal()
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma || isPaused) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        isUpgradeTreeOpen = false;
        isChoosingSkill = false;
        isArsenalShopOpen = true;
        if (ArsenalShopUI.Instance != null)
        {
            ArsenalShopUI.Instance.OnShopOpened(equippedGauntlet);
        }
        Time.timeScale = 0f;
    }

    public void CloseAllUpgradeModals()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        isUpgradeTreeOpen = false;
        isArsenalShopOpen = false;
        isChoosingSkill = false;
        if (VorstCodexUI.Instance != null && VorstCodexUI.Instance.isOpen)
        {
            VorstCodexUI.Instance.CloseCodex();
        }
        Time.timeScale = 1f;
    }

    public void OpenVorstCodexModal(bool returnToPauseOnClose)
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma) return;
        isUpgradeTreeOpen = false;
        isArsenalShopOpen = false;
        isChoosingSkill = false;
        isPaused = false;
        Time.timeScale = 0f;

        VorstCodexUI codex = VorstCodexUI.EnsureExists();
        if (codex != null)
        {
            codex.OpenCodex(() =>
            {
                if (returnToPauseOnClose)
                {
                    isPaused = true;
                    Time.timeScale = 0f;
                }
                else
                {
                    isPaused = false;
                    Time.timeScale = 1f;
                }
            });
        }
    }

    // =========================================================================
    // ARSENAL DE VANGUARDIA (Issue #21): API DE COMPRA, EQUIPO Y PASIVAS
    // =========================================================================

    public bool IsGauntletUnlocked(GauntletVariant variant)
    {
        if (variant == GauntletVariant.PrototypeCyan) return true;
        int bit = 1 << (int)variant;
        return (unlockedGauntletsMask & bit) != 0;
    }

    public bool PurchaseOrEquipGauntlet(GauntletVariant variant)
    {
        GauntletSpec spec = GetGauntletSpec(variant);
        bool unlocked = IsGauntletUnlocked(variant);

        if (!unlocked)
        {
            if (gold < spec.GoldCost) return false;
            gold -= spec.GoldCost;
            unlockedGauntletsMask |= (1 << (int)variant);
        }

        equippedGauntlet = variant;
        RecalculateStats();
        SyncEquippedGauntletVisuals();
        return true;
    }

    public bool PurchaseNexaDrone()
    {
        if (hasPurchasedNexaDrone || gold < NEXA_DRONE_COST) return false;

        gold -= NEXA_DRONE_COST;
        hasPurchasedNexaDrone = true;

        Transform kaelT = kaelTransform;
        if (kaelT == null && KaelAutoController.Instance != null)
        {
            kaelT = KaelAutoController.Instance.transform;
            kaelTransform = kaelT;
        }

        NexaDroneCompanion drone = NexaDroneCompanion.EnsureExists(kaelT);
        if (drone != null)
        {
            drone.ActivateFromPurchase(kaelT);
        }

        OnStatsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Calcula el daño por tick de quemadura continua del Guantelete Piro-Reactor.
    /// </summary>
    public float GetPyroBurnDamagePerTick()
    {
        return Mathf.Round(6f + (playerLevel * 2f) + (branchFuria * 2f) + (attackDamage * 0.14f));
    }

    /// <summary>
    /// Aplica el 5% de robo de vida del Guantelete de Vacío Vorst sobre el daño real infligido.
    /// </summary>
    public float ApplyGauntletLifeSteal(float actualDamageDealt)
    {
        if (equippedGauntlet != GauntletVariant.VorstVoid || actualDamageDealt <= 0f || currentHP <= 0f)
            return 0f;

        float stealAmt = actualDamageDealt * VORST_LIFESTEAL_RATIO;
        currentHP = Mathf.Min(maxHP, currentHP + stealAmt);
        OnStatsChanged?.Invoke();
        return stealAmt;
    }

    public void SyncEquippedGauntletVisuals()
    {
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SetGauntletVariant(equippedGauntlet);
        }
        if (KaelAutoController.Instance != null)
        {
            KaelAutoController.Instance.RefreshEquippedGauntletVisuals();
        }
    }

    public int CountAffordableArsenalItems()
    {
        int count = 0;
        for (int i = 0; i < GauntletCatalog.Length; i++)
        {
            GauntletSpec spec = GauntletCatalog[i];
            if (!IsGauntletUnlocked(spec.Variant) && gold >= spec.GoldCost)
                count++;
        }
        if (!hasPurchasedNexaDrone && gold >= NEXA_DRONE_COST)
            count++;
        return count;
    }

    // --- FUNCIONES AUXILIARES DE ESCALADO NATIVO (1920x1080 -> Píxeles Reales) ---
    public static Rect ScaleRect(float x, float y, float w, float h)
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

    public static int ScaleFont(int baseSize)
    {
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        return Mathf.Max(10, Mathf.RoundToInt(baseSize * scale));
    }

    // Dibuja un botón biselado con textura cyberpunk profesional + animación de iluminación en hover
    public bool DrawSkinnedCyberButton(Rect baseRect, Texture2D skinTex, string mainText, string subText, int mainFontSize, int subFontSize, Color mainTextCol, Color subTextCol, Color fallbackAccent, bool enabled = true)
    {
        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool hovered = enabled && r.Contains(Event.current.mousePosition);

        if (skinTex != null)
        {
            GUI.color = !enabled
                ? new Color(0.45f, 0.48f, 0.55f, 0.82f)
                : (hovered ? new Color(1.18f, 1.18f, 1.18f, 1f) : Color.white);
            GUI.DrawTexture(r, skinTex, ScaleMode.StretchToFill);
            GUI.color = Color.white;

            if (hovered)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.12f);
                GUI.DrawTexture(new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.height - 8f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }
        else
        {
            DrawCyberBox(r, hovered ? new Color(0.14f, 0.22f, 0.36f, 0.98f) : colBgDark, hovered ? Color.white : fallbackAccent, 2f);
        }

        if (string.IsNullOrEmpty(subText))
        {
            DrawStyledText(r, mainText, mainFontSize, TextAnchor.MiddleCenter, enabled ? (hovered ? Color.white : mainTextCol) : new Color(0.62f, 0.66f, 0.74f));
        }
        else
        {
            Rect topR = ScaleRect(baseRect.x + 14f, baseRect.y + 6f, baseRect.width - 28f, baseRect.height * 0.52f);
            Rect botR = ScaleRect(baseRect.x + 14f, baseRect.y + baseRect.height * 0.50f, baseRect.width - 28f, baseRect.height * 0.42f);
            DrawStyledText(topR, mainText, mainFontSize, TextAnchor.MiddleCenter, enabled ? (hovered ? Color.white : mainTextCol) : new Color(0.62f, 0.66f, 0.74f));
            DrawStyledText(botR, subText, subFontSize, TextAnchor.MiddleCenter, enabled ? subTextCol : new Color(0.50f, 0.54f, 0.62f));
        }

        return enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    public void DrawSkinnedCyberPanel(Rect r, Texture2D panelTex, Color fallbackBorder)
    {
        if (panelTex != null)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(r, panelTex, ScaleMode.StretchToFill);
        }
        else
        {
            DrawCyberBox(r, colBgDark, fallbackBorder, 3f);
        }
    }

    void OnGUI()
    {
        // Ocultar por completo el HUD de combate durante la Fase 1 cinemática del Puente o durante los 3 Finales Cinemáticos
        if (BridgeLevel1To2Controller.Instance != null &&
            BridgeLevel1To2Controller.Instance.isBridgeActive &&
            !BridgeLevel1To2Controller.Instance.isInPlayablePhase)
        {
            return;
        }
        if (EndingCinematicController.Instance != null &&
            EndingCinematicController.Instance.isEndingActive)
        {
            return;
        }

        GUI.matrix = Matrix4x4.identity;

        if (!isUpgradeTreeOpen && !isArsenalShopOpen)
        {
            DrawKaelWorldHealthBar();
        }

        // --- A) BARRA SUPERIOR COMPACTA: PERFIL DE KAEL ---
        Rect profileBox = ScaleRect(18, 12, 512, 98);
        DrawSkinnedCyberPanel(profileBox, texPanelCyan, colBorder);

        DrawStyledText(ScaleRect(36, 17, 175, 28), $"KAEL <color=#38e1ff>Nv.{playerLevel}</color>", 20, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(205, 18, 308, 26), $"ATK: {attackDamage}  |  CRIT: {Mathf.RoundToInt(critChance * 100)}%  |  +{hpRegenPerSec:F1}/s", 14, TextAnchor.MiddleRight, new Color(0.85f, 0.93f, 1f));

        float hpRatio = Mathf.Clamp01(currentHP / Mathf.Max(1f, maxHP));
        DrawProgressBar(ScaleRect(36, 47, 476, 22), hpRatio, new Color(0.14f, 0.05f, 0.08f), colHpGreen, $"HP: {Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}", 14);

        float expRatio = Mathf.Clamp01((float)currentExp / Mathf.Max(1, expToNextLevel));
        DrawProgressBar(ScaleRect(36, 75, 476, 18), expRatio, new Color(0.10f, 0.05f, 0.20f), colExpPurp, $"EXP: {currentExp} / {expToNextLevel}", 12);

        // Botón parpadeante cuando tienes Puntos de Habilidad disponibles
        if (skillPoints > 0 && !IsAnyBlockingModalOpen)
        {
            if (DrawSkinnedCyberButton(
                new Rect(538f, 12f, 228f, 68f),
                texBtnPurple,
                $"✦ EVOLUCIÓN (+{skillPoints})",
                "ABRIR RAMA NEXO [TAB]",
                16, 12, Color.white, colGoldYel, colExpPurp, true))
            {
                OpenUpgradeTreeAtTab(1);
            }
        }

        // --- B) PANEL CENTRAL SUPERIOR: OLEADAS ---
        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null && spawner.currentWave < spawner.totalWaves && spawner.currentWave != 5)
        {
            DrawSkinnedCyberPanel(ScaleRect(775, 12, 410, 68), texPanelCyan, colBorder);
            string waveHeader = (levelNumber == 3)
                ? $"COLMENA VORST — OLEADA {Mathf.Max(1, spawner.currentWave)} / {spawner.totalWaves}"
                : ((levelNumber == 2)
                    ? $"BASE VORST — OLEADA {Mathf.Max(1, spawner.currentWave)} / {spawner.totalWaves}"
                    : $"SECTOR 7 — OLEADA {Mathf.Max(1, spawner.currentWave)} / {spawner.totalWaves}");
            DrawStyledText(ScaleRect(775, 16, 410, 28), waveHeader, 17, TextAnchor.MiddleCenter, colCyanNexo);
            float waveRatio = Mathf.Clamp01((float)spawner.currentWave / Mathf.Max(1, spawner.totalWaves));
            DrawProgressBar(ScaleRect(798, 48, 364, 16), waveRatio, new Color(0.08f, 0.14f, 0.24f), colCyanNexo, "", 11);
        }

        // --- C) PANEL SUPERIOR DERECHO: ORO, CONTROL COMPACTO DE AUDIO Y PAUSA ---
        DrawSkinnedCyberPanel(ScaleRect(1205, 12, 240, 68), texPanelGold, colGoldYel);
        DrawStyledText(ScaleRect(1213, 12, 224, 68), $"● {gold} ORO", 23, TextAnchor.MiddleCenter, colGoldYel);

        DrawTopRightAudioWidget(1456f, 12f, 302f, 68f);

        if (DrawSkinnedCyberButton(
            new Rect(1768f, 12f, 134f, 68f),
            isPaused ? texBtnEmerald : texBtnCyan,
            isPaused ? "▶ REANUDAR" : "❚❚ PAUSA",
            "[ESC]",
            15, 11, Color.white, colCyanNexo, colBorder, !isGameOver && !isLevelCompleted && !isChoosingDilemma))
        {
            TogglePause();
        }

        // --- C.2) MEDIDOR ESTILIZADO DE RACHA DE COMBOS Y SOBREMARCHA NEXO (LATERAL DERECHO) + ESTADO MORAL ---
        DrawComboAndOverdriveHUD();

        // --- D) BARRA INFERIOR COMPACTA ESTILO ACTION RPG ---
        if (!isUpgradeTreeOpen && !isArsenalShopOpen)
        {
            DrawActionRpgBottomBar();
        }

        // --- E) VENTANAS MODALES EMERGENTES ---
        if (isUpgradeTreeOpen || isChoosingSkill) DrawUpgradeTreeModal();
        if (isArsenalShopOpen && ArsenalShopUI.Instance != null) ArsenalShopUI.Instance.DrawShopModal();
        if (isPaused) DrawPauseModal();
        if (isGameOver) DrawGameOverModal(spawner);
        if (isChoosingDilemma) DrawNarrativeDilemmaModal();
        if (isLevelCompleted) DrawVictoryModal(spawner);
    }

    // =========================================================================
    // BLOQUE 2 (Issue #17) & BLOQUE 4 (Issue #22): HUD DE COMBO / OVERDRIVE Y MORAL
    // =========================================================================
    void DrawComboAndOverdriveHUD()
    {
        if (IsAnyBlockingModalOpen)
            return;

        // 1. Indicador de Transformación Moral Dinámica bajo el perfil de Kael (Bloque 4)
        if (compassionPoints != rigorPoints)
        {
            bool isPureza = (compassionPoints > rigorPoints);
            Rect moralBadge = ScaleRect(18f, 114f, 512f, 28f);
            Color mCol = isPureza ? new Color(0.28f, 0.98f, 1f) : new Color(1f, 0.22f, 0.48f);
            DrawCyberBox(moralBadge, new Color(0.03f, 0.05f, 0.12f, 0.92f), mCol, 1.5f);
            string moralTxt = isPureza
                ? $"☀ PUREZA NEXO (Compasión {compassionPoints}) — +10% Curación en Ult/Parry"
                : $"☣ DISONANCIA VORST (Rigor {rigorPoints}) — +22% Crit + Quemadura Plasma";
            DrawStyledText(moralBadge, moralTxt, 13, TextAnchor.MiddleCenter, mCol);
        }

        // 2. Medidor Estilizado de Racha de Combos y Sobremarcha Nexo en el Lateral Derecho (Bloque 2)
        if (comboCount < 2) return;

        float punch = (comboPunchTimer > 0f) ? Mathf.Sin((comboPunchTimer / 0.22f) * Mathf.PI) * 10f : 0f;
        Rect comboCard = ScaleRect(1456f - punch * 0.5f, 92f - punch * 0.3f, 446f + punch, 108f + punch * 0.6f);

        Color rankCol = colCyanNexo;
        Texture2D cardSkin = texPanelCyan;
        string rankTitle = "CADENCIA ACTIVA";
        string buffSubtitle = "MANTÉN LA OFENSIVA SIN RECIBIR DAÑO DIRECTO";

        if (isOverdriveActive)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
            rankCol = Color.Lerp(new Color(1f, 0.22f, 0.78f), new Color(0.22f, 0.96f, 1f), pulse);
            cardSkin = texPanelCrimson;
            rankTitle = "⚡ SOBREMARCHA NEXO ⚡";
            buffSubtitle = "+30% VEL. ATAQUE  |  +20% MOVIMIENTO  |  +30% ORO";
        }
        else if (comboCount >= 12)
        {
            rankCol = colGoldYel;
            cardSkin = texPanelGold;
            rankTitle = "★ RANGO: IMPARABLE ★";
            buffSubtitle = $"A {20 - comboCount} GOLPES DE ACTIVAR SOBREMARCHA NEXO";
        }
        else if (comboCount >= 5)
        {
            rankCol = colCyanNexo;
            cardSkin = texPanelCyan;
            rankTitle = "✦ RANGO: SINCRONIZADO ✦";
            buffSubtitle = $"A {20 - comboCount} GOLPES DE ACTIVAR SOBREMARCHA NEXO";
        }

        DrawSkinnedCyberPanel(comboCard, cardSkin, rankCol);

        int hitFontSize = Mathf.Clamp(20 + Mathf.RoundToInt(punch * 0.3f), 20, 25);
        DrawStyledText(ScaleRect(1472f, 98f, 155f, 36f), $"🔥 x{comboCount} HITS", hitFontSize, TextAnchor.MiddleLeft, Color.white, false);
        DrawStyledText(ScaleRect(1630f, 100f, 256f, 34f), rankTitle, 13, TextAnchor.MiddleRight, rankCol, false);

        DrawStyledText(ScaleRect(1474f, 136f, 410f, 24f), buffSubtitle, 12, TextAnchor.MiddleCenter, new Color(0.90f, 0.96f, 1f));

        float timeRatio = Mathf.Clamp01(comboDecayTimer / MAX_COMBO_DECAY);
        DrawProgressBar(
            ScaleRect(1474f, 166f, 410f, 18f),
            timeRatio,
            new Color(0.06f, 0.08f, 0.16f, 0.95f),
            rankCol,
            $"TIEMPO DE COMBO: {comboDecayTimer:F1}s",
            11
        );
    }

    // =========================================================================
    // BARRA INFERIOR LIMPIA Y COMPACTA DE ACTION RPG CON BOTONES BISELADOS
    // =========================================================================
    void DrawActionRpgBottomBar()
    {
        Rect dockRect = ScaleRect(0f, 992f, 1920f, 88f);
        DrawCyberBox(dockRect, new Color(0.02f, 0.03f, 0.08f, 0.95f), new Color(0.18f, 0.45f, 0.68f, 0.85f), 2f);

        GUI.color = new Color(0.22f, 0.88f, 1f, 0.88f);
        GUI.DrawTexture(new Rect(dockRect.x, dockRect.y, dockRect.width, Mathf.Max(2f, Mathf.Round(2f * (Screen.height / 1080f)))), Texture2D.whiteTexture);
        GUI.color = Color.white;

        KaelAutoController kael = KaelAutoController.Instance;
        if (kael != null && Event.current.type == EventType.Repaint)
        {
            kael.hudMoveLeftHeld = false;
            kael.hudMoveRightHeld = false;
            kael.hudGuardHeld = false;
        }

        // 1. SECCIÓN IZQUIERDA: Controles Rápidos + Botones Táctiles/Mouse Opcionales (A / S / D)
        Rect ctrlBox = ScaleRect(16f, 1002f, 610f, 68f);
        DrawSkinnedCyberPanel(ctrlBox, texPanelCyan, colBorder);

        DrawHoldControlPill(new Rect(26f, 1011f, 72f, 50f), "◀ [A]", "IZQ", kael != null && (KaelAutoController.IsLeftHeld() || kael.hudMoveLeftHeld), colCyanNexo, () => { if (kael != null) kael.hudMoveLeftHeld = true; });
        DrawHoldControlPill(new Rect(104f, 1011f, 88f, 50f), "🛡 [S]", "GUARDIA", kael != null && kael.isGuarding, new Color(0.35f, 0.95f, 1f), () => { if (kael != null) kael.hudGuardHeld = true; });
        DrawHoldControlPill(new Rect(198f, 1011f, 72f, 50f), "▶ [D]", "DER", kael != null && (KaelAutoController.IsRightHeld() || kael.hudMoveRightHeld), colCyanNexo, () => { if (kael != null) kael.hudMoveRightHeld = true; });

        DrawStyledText(ScaleRect(280f, 1010f, 336f, 26f),
            "<color=#38e1ff>[A/D]</color> Mover   <color=#38e1ff>[S]</color> Guardia (-45%)",
            13, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(280f, 1036f, 336f, 26f),
            "<color=#ffd35c>[J]</color> Atacar   <color=#2ee678>[K]</color> Ult   <color=#ffd35c>[B]</color> Tienda",
            13, TextAnchor.MiddleLeft, new Color(0.85f, 0.92f, 1f));

        // 2. BOTÓN DE ACCIÓN BISELADO: ATACAR [J / ESPACIO]
        int comboStep = (kael != null) ? kael.currentCombo : 0;
        string comboLabel = (comboStep > 0) ? $"⚔ ATACAR [J] • {comboStep}/3" : "⚔ ATACAR [J / ESP]";
        string comboSub = (comboStep == 2) ? "¡SIGUIENTE: SÍSMICO!" : $"CADENCIA {attackRate:F1}/s";

        if (DrawSkinnedCyberButton(
            new Rect(636f, 1002f, 274f, 68f),
            (comboStep == 3) ? texBtnGold : texBtnCrimson,
            comboLabel, comboSub,
            16, 11, Color.white, (comboStep == 2) ? colGoldYel : new Color(1f, 0.85f, 0.80f), colAttrFuerza, !IsAnyBlockingModalOpen))
        {
            if (kael != null) kael.TriggerManualAttack();
        }

        // 3. BOTÓN DE ACCIÓN BISELADO: SOBRECARGA ULTIMATE [K / Q]
        DrawCompactUltimateButton(new Rect(920f, 1002f, 314f, 68f));

        // 4. BOTÓN TIENDA TÁCTICA: ARSENAL DE VANGUARDIA [B / T]
        DrawArsenalShopDockButton(new Rect(1246f, 1002f, 320f, 68f));

        // 5. BOTÓN DESTACADO BISELADO: 🧬 ÁRBOL DE MEJORAS [TAB / M]
        DrawUpgradeTreeDockButton(new Rect(1578f, 1002f, 326f, 68f));
    }

    void DrawHoldControlPill(Rect baseRect, string topTxt, string subTxt, bool isActive, Color accentCol, Action onHold)
    {
        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool hovered = r.Contains(Event.current.mousePosition);
        bool mouseHeld = hovered && Input.GetMouseButton(0);
        if (mouseHeld) onHold?.Invoke();

        bool lit = isActive || mouseHeld;
        if (texBtnCyan != null)
        {
            GUI.color = lit ? new Color(1.25f, 1.25f, 1.25f, 1f) : (hovered ? Color.white : new Color(0.82f, 0.88f, 0.95f, 0.95f));
            GUI.DrawTexture(r, lit ? texBtnEmerald : texBtnCyan, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }
        else
        {
            DrawCyberBox(r, colBgDark, accentCol, 1.5f);
        }

        DrawStyledText(ScaleRect(baseRect.x, baseRect.y + 4f, baseRect.width, 24f), topTxt, 14, TextAnchor.MiddleCenter, lit ? colGoldYel : accentCol);
        DrawStyledText(ScaleRect(baseRect.x, baseRect.y + 26f, baseRect.width, 20f), subTxt, 11, TextAnchor.MiddleCenter, Color.white);
    }

    void DrawCompactUltimateButton(Rect baseRect)
    {
        bool ready = IsUltReady && !IsAnyBlockingModalOpen;
        float pulse = ready ? (0.82f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.25f) : 1f;

        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool isHovered = r.Contains(Event.current.mousePosition);

        if (texBtnCyan != null)
        {
            GUI.color = ready ? new Color(pulse, pulse, 1f, 1f) : new Color(0.65f, 0.75f, 0.85f, 0.92f);
            GUI.DrawTexture(r, ready ? texBtnEmerald : texBtnCyan, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }
        else
        {
            DrawCyberBox(r, colBgDark, colCyanNexo, 2f);
        }

        // Barra de progreso de carga dentro del botón
        float chargeRatio = Mathf.Clamp01(currentUltCharge / Mathf.Max(1f, maxUltCharge));
        Rect fillRect = new Rect(r.x + 10f, r.y + r.height - 10f, (r.width - 20f) * chargeRatio, 5f);
        GUI.color = ready ? colGoldYel : colCyanNexo;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        string mainTitle = ready ? "⚡ ¡SOBRECARGA! [K/Q]" : $"⚡ SOBRECARGA [K] ({Mathf.FloorToInt(currentUltCharge)}%)";
        string subTitle = ready ? "CLIC O [K] PARA CAÑÓN NEXO" : "GOLPEA CON [J] PARA CARGAR";

        DrawStyledText(ScaleRect(baseRect.x + 10f, baseRect.y + 7f, baseRect.width - 20f, 28f), mainTitle, 15, TextAnchor.MiddleCenter, ready ? Color.white : colCyanNexo);
        DrawStyledText(ScaleRect(baseRect.x + 10f, baseRect.y + 34f, baseRect.width - 20f, 24f), subTitle, 11, TextAnchor.MiddleCenter, ready ? colGoldYel : new Color(0.78f, 0.90f, 1f));

        if (ready && isHovered)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.14f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (ready && GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (KaelAutoController.Instance != null) KaelAutoController.Instance.TriggerUltimate();
        }
    }

    void DrawArsenalShopDockButton(Rect baseRect)
    {
        int affordableArsenal = CountAffordableArsenalItems();
        GauntletSpec eqSpec = GetGauntletSpec(equippedGauntlet);
        Texture2D btnSkin = equippedGauntlet switch
        {
            GauntletVariant.PyroReactor => texBtnCrimson,
            GauntletVariant.IgneousPlasma => texBtnGold,
            GauntletVariant.VorstVoid => texBtnPurple,
            _ => texBtnCyan
        };

        string subTxt = (affordableArsenal > 0)
            ? $"¡{affordableArsenal} NUEVO(S) DESBLOQUEABLE(S)!"
            : $"ACTIVO: {eqSpec.CodeName}" + (hasPurchasedNexaDrone ? " • DRON ON" : " • DRON OFF");

        if (DrawSkinnedCyberButton(
            baseRect,
            btnSkin,
            "⚙ ARSENAL DE VANGUARDIA [B]",
            subTxt,
            15, 11,
            Color.white,
            affordableArsenal > 0 ? colGoldYel : eqSpec.PrimaryColor,
            eqSpec.PrimaryColor, !IsAnyBlockingModalOpen))
        {
            ToggleArsenalShopModal();
        }

        if (affordableArsenal > 0 && !IsAnyBlockingModalOpen)
        {
            float bounce = Mathf.Sin(Time.unscaledTime * 6.5f) * 3f;
            Rect badgeRect = ScaleRect(baseRect.x + baseRect.width - 134f, baseRect.y - 18f + bounce, 126f, 24f);
            DrawCyberBox(badgeRect, new Color(0.18f, 0.92f, 0.98f, 1f), Color.white, 1.5f);
            DrawStyledText(badgeRect, $"🛒 {affordableArsenal} EN TIENDA", 12, TextAnchor.MiddleCenter, new Color(0.02f, 0.06f, 0.10f));
        }
    }

    void DrawUpgradeTreeDockButton(Rect baseRect)
    {
        int affordableCount = CountAvailableUpgrades();
        bool hasNotification = (affordableCount > 0 || skillPoints > 0);

        string subStatus = (skillPoints > 0)
            ? $"★ ¡{skillPoints} PTO EVOLUCIÓN + {affordableCount} MEJ.!"
            : (affordableCount > 0 ? $"● ¡{affordableCount} MEJORA(S) CON ORO!" : "ATRIBUTOS Y EVOLUCIÓN [TAB]");

        if (DrawSkinnedCyberButton(
            baseRect,
            hasNotification ? texBtnGold : texBtnPurple,
            "🧬 ÁRBOL DE MEJORAS [TAB]",
            subStatus,
            16, 11,
            hasNotification ? colGoldYel : Color.white,
            hasNotification ? Color.white : new Color(0.84f, 0.88f, 1f),
            colGoldYel, !IsAnyBlockingModalOpen))
        {
            ToggleUpgradeTreeModal();
        }

        // Globo / insignia dorada flotante cuando alcanza el oro
        if (hasNotification && !IsAnyBlockingModalOpen)
        {
            float bounce = Mathf.Sin(Time.unscaledTime * 7f) * 3f;
            Rect badgeRect = ScaleRect(baseRect.x + baseRect.width - 144f, baseRect.y - 18f + bounce, 136f, 24f);
            DrawCyberBox(badgeRect, new Color(1f, 0.80f, 0.12f, 1f), Color.white, 1.5f);
            string badgeTxt = (skillPoints > 0) ? $"¡+{skillPoints} PTO NEXO!" : $"¡{affordableCount} DISPONIBLES!";
            DrawStyledText(badgeRect, badgeTxt, 12, TextAnchor.MiddleCenter, new Color(0.1f, 0.06f, 0.01f));
        }
    }

    // =========================================================================
    // VENTANA MODAL A PANTALLA COMPLETA: "ÁRBOL DE MEJORAS NEXO"
    // =========================================================================
    void DrawUpgradeTreeModal()
    {
        GUI.color = new Color(0.01f, 0.02f, 0.06f, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect panel = ScaleRect(44f, 30f, 1832f, 1020f);
        DrawSkinnedCyberPanel(panel, texPanelCyan, colCyanNexo);

        DrawStyledText(ScaleRect(82f, 48f, 840f, 40f), "🧬 MATRIZ DE EVOLUCIÓN — ÁRBOL DE MEJORAS NEXO", 26, TextAnchor.MiddleLeft, colCyanNexo);
        DrawStyledText(ScaleRect(82f, 86f, 840f, 28f),
            $"KAEL Nv.{playerLevel}   |   ATK: {attackDamage}   |   HP: {maxHP}   |   CRIT: {Mathf.RoundToInt(critChance * 100)}%   |   DEF: {Mathf.RoundToInt(armorDamageReduction * 100)}%   |   VEL: x{moveSpeedMultiplier:F2}",
            15, TextAnchor.MiddleLeft, new Color(0.84f, 0.93f, 1f));

        // Indicadores de Oro y Puntos de Evolución en la cabecera
        DrawSkinnedCyberPanel(ScaleRect(880f, 48f, 210f, 58f), texPanelGold, colGoldYel);
        DrawStyledText(ScaleRect(880f, 48f, 210f, 58f), $"● {gold} ORO", 21, TextAnchor.MiddleCenter, colGoldYel);

        DrawSkinnedCyberPanel(ScaleRect(1102f, 48f, 232f, 58f), texPanelCyan, colExpPurp);
        DrawStyledText(ScaleRect(1102f, 48f, 232f, 58f), $"✦ {skillPoints} PTOS EVOLUCIÓN", 16, TextAnchor.MiddleCenter, skillPoints > 0 ? colGoldYel : colExpPurp);

        if (DrawSkinnedCyberButton(
            new Rect(1346f, 48f, 234f, 58f),
            texBtnGold,
            "⚙ TIENDA ARSENAL [B]",
            "GUANTELETES & N.E.X.A.",
            15, 10, Color.white, colGoldYel, colGoldYel, true))
        {
            OpenArsenalShopModal();
            return;
        }

        // Botón biselado ✖ CERRAR ÁRBOL [TAB / ESC]
        if (DrawSkinnedCyberButton(
            new Rect(1592f, 48f, 250f, 58f),
            texBtnCrimson,
            "✖ CERRAR [TAB / ESC]",
            "REANUDAR COMBATE",
            15, 10, Color.white, new Color(1f, 0.82f, 0.82f), new Color(1f, 0.35f, 0.45f), true))
        {
            CloseAllUpgradeModals();
            return;
        }

        // --- SELECTOR DE LAS 3 PESTAÑAS / RAMAS DEL ÁRBOL ---
        int r1Count = (gold >= GetUpgradeCost(levelAtk) ? 1 : 0) + (gold >= GetUpgradeCost(levelHp) ? 1 : 0) +
                      (gold >= GetUpgradeCost(levelSpd) ? 1 : 0) + (gold >= GetUpgradeCost(levelCrit) ? 1 : 0) +
                      (gold >= GetUpgradeCost(levelGold) ? 1 : 0);
        int r2Count = skillPoints;
        int r3Count = (levelArmor < 5 && gold >= GetArsenalCost(30, levelArmor) ? 1 : 0) +
                      (levelThrusters < 5 && gold >= GetArsenalCost(28, levelThrusters) ? 1 : 0) +
                      (levelOvercharge < 5 && gold >= GetArsenalCost(38, levelOvercharge) ? 1 : 0) +
                      (levelSeismicEdge < 5 && gold >= GetArsenalCost(42, levelSeismicEdge) ? 1 : 0);

        DrawTreeTabButton(new Rect(76f, 134f, 564f, 66f), 0, "RAMA 1: ATRIBUTOS PRIMARIOS", "Fuerza, Vigor, Cadencia, Blindaje y Recolección (Oro)", colCyanNexo, texBtnCyan, r1Count);
        DrawTreeTabButton(new Rect(660f, 134f, 564f, 66f), 1, "RAMA 2: TÉCNICAS DE COMBATE", "Furia Continua, Tsunami Sísmico e Impacto Meteoro", colExpPurp, texBtnPurple, r2Count);
        DrawTreeTabButton(new Rect(1244f, 134f, 564f, 66f), 2, "RAMA 3: MATRIZ EVOLUTIVA & ARSENAL", "Vínculo Fraternal, Armonía Humana y Disonancia Vorst", colGoldYel, texBtnGold, r3Count);

        Rect canvasRect = ScaleRect(76f, 214f, 1732f, 804f);
        DrawCyberBox(canvasRect, new Color(0.02f, 0.03f, 0.07f, 0.94f), new Color(0.15f, 0.36f, 0.55f, 0.65f), 1.5f);
        DrawCircuitGridBackground(canvasRect);

        if (selectedTreeTab == 0) DrawBranch1BaseAttributesTree();
        else if (selectedTreeTab == 1) DrawBranch2NexoEvolutionTree();
        else DrawBranch3ArsenalArmorTree();
    }

    void DrawTreeTabButton(Rect baseRect, int tabIdx, string title, string subtitle, Color accentCol, Texture2D btnSkin, int badgeCount)
    {
        bool active = (selectedTreeTab == tabIdx);
        if (DrawSkinnedCyberButton(
            baseRect,
            active ? btnSkin : texBtnCyan,
            title, subtitle,
            18, 12,
            active ? Color.white : accentCol,
            new Color(0.84f, 0.90f, 0.98f),
            accentCol, true))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            selectedTreeTab = tabIdx;
        }

        if (badgeCount > 0)
        {
            Rect badge = ScaleRect(baseRect.x + baseRect.width - 54f, baseRect.y + 12f, 38f, 26f);
            DrawCyberBox(badge, colGoldYel, Color.white, 1.5f);
            DrawStyledText(badge, $"+{badgeCount}", 13, TextAnchor.MiddleCenter, new Color(0.1f, 0.06f, 0.01f));
        }
    }

    void DrawCircuitGridBackground(Rect canvasScaled)
    {
        GUI.color = new Color(0.12f, 0.35f, 0.55f, 0.08f);
        int vLines = 12;
        for (int i = 1; i < vLines; i++)
        {
            float x = canvasScaled.x + (canvasScaled.width / vLines) * i;
            GUI.DrawTexture(new Rect(x, canvasScaled.y, 1f, canvasScaled.height), Texture2D.whiteTexture);
        }
        int hLines = 6;
        for (int j = 1; j < hLines; j++)
        {
            float y = canvasScaled.y + (canvasScaled.height / hLines) * j;
            GUI.DrawTexture(new Rect(canvasScaled.x, y, canvasScaled.width, 1f), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    void DrawCircuitConnection(Vector2 startBase, Vector2 endBase, Color lineCol, bool activeGlow)
    {
        float pulse = activeGlow ? (0.75f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.25f) : 0.45f;
        Color c = new Color(lineCol.r, lineCol.g, lineCol.b, pulse);
        float midY = (startBase.y + endBase.y) * 0.5f;

        DrawBaseLineSegment(startBase.x, startBase.y, startBase.x, midY, 4f, c);
        DrawBaseLineSegment(startBase.x, midY, endBase.x, midY, 4f, c);
        DrawBaseLineSegment(endBase.x, midY, endBase.x, endBase.y, 4f, c);

        Rect joint1 = ScaleRect(startBase.x - 6f, midY - 6f, 12f, 12f);
        Rect joint2 = ScaleRect(endBase.x - 6f, midY - 6f, 12f, 12f);
        GUI.color = c;
        GUI.DrawTexture(joint1, Texture2D.whiteTexture);
        GUI.DrawTexture(joint2, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawBaseLineSegment(float x1, float y1, float x2, float y2, float thickness, Color col)
    {
        float minX = Mathf.Min(x1, x2) - thickness * 0.5f;
        float minY = Mathf.Min(y1, y2) - thickness * 0.5f;
        float w = Mathf.Abs(x2 - x1) + thickness;
        float h = Mathf.Abs(y2 - y1) + thickness;
        Rect r = ScaleRect(minX, minY, w, h);
        GUI.color = col;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // =========================================================================
    // RENDERIZADOR PROFESIONAL DE BADGE DE ICONO PIXEL-ART HD (ISSUE #20)
    // =========================================================================
    void DrawSkillIconBadge(Rect baseRect, Texture2D iconTex, bool isUnlocked, bool isAffordable, Color accentCol)
    {
        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool hovered = r.Contains(Event.current.mousePosition);

        // 1. Fondo sutil biselado con resguardo de margen para el marco del icono
        Color borderCol = isUnlocked ? (hovered ? Color.white : accentCol) : (isAffordable ? colGoldYel : new Color(0.20f, 0.28f, 0.38f, 0.65f));
        DrawCyberBox(r, new Color(0.02f, 0.04f, 0.08f, 0.88f), borderCol, 1.2f);

        // 2. Textura del Icono con sombreado dinámico de estado
        if (iconTex != null)
        {
            Color prevCol = GUI.color;
            if (isUnlocked)
            {
                // Adquirido / Desbloqueado: Máxima definición y brillo vibrante
                GUI.color = hovered ? new Color(1.20f, 1.20f, 1.20f, 1f) : Color.white;
            }
            else if (isAffordable)
            {
                // Disponible para comprar: Tinte dorado cálido
                GUI.color = new Color(0.92f, 0.88f, 0.74f, 0.95f);
            }
            else
            {
                // Bloqueado: Silueta holográfica metálica atenuada
                GUI.color = new Color(0.36f, 0.42f, 0.52f, 0.65f);
            }

            float pad = Mathf.Max(2f, Mathf.Round(2f * (Screen.width / 1920f)));
            Rect iconRect = new Rect(r.x + pad, r.y + pad, r.width - pad * 2f, r.height - pad * 2f);
            GUI.DrawTexture(iconRect, iconTex, ScaleMode.ScaleToFit);
            GUI.color = prevCol;
        }

        // 3. Indicador de Estado: Mini-Badge en esquina superior
        if (isUnlocked)
        {
            Rect checkBadge = new Rect(r.x + r.width - ScaleFont(16) - 2f, r.y + 2f, ScaleFont(16), ScaleFont(16));
            DrawStyledText(checkBadge, "✔", 11, TextAnchor.MiddleCenter, colHpGreen);
        }
        else
        {
            Rect lockBadge = new Rect(r.x + r.width - ScaleFont(16) - 2f, r.y + 2f, ScaleFont(16), ScaleFont(16));
            DrawStyledText(lockBadge, "🔒", 11, TextAnchor.MiddleCenter, isAffordable ? colGoldYel : new Color(0.55f, 0.62f, 0.72f));
        }
    }

    void DrawBranch1BaseAttributesTree()
    {
        Vector2 coreCenter = new Vector2(942f, 305f);
        Vector2 node1Top = new Vector2(360f, 445f);
        Vector2 node2Top = new Vector2(942f, 445f);
        Vector2 node3Top = new Vector2(1524f, 445f);
        Vector2 node4Top = new Vector2(650f, 735f);
        Vector2 node5Top = new Vector2(1234f, 735f);

        DrawCircuitConnection(new Vector2(coreCenter.x, 360f), node1Top, colAttrFuerza, true);
        DrawCircuitConnection(new Vector2(coreCenter.x, 360f), node2Top, colAttrVigor, true);
        DrawCircuitConnection(new Vector2(coreCenter.x, 360f), node3Top, colAttrReflejos, true);
        DrawCircuitConnection(new Vector2(node1Top.x, 665f), node4Top, colAttrInstinto, true);
        DrawCircuitConnection(new Vector2(node3Top.x, 665f), node5Top, colAttrCarronero, true);

        Rect coreRect = ScaleRect(662f, 236f, 560f, 124f);
        DrawSkinnedCyberPanel(coreRect, texPanelCyan, colCyanNexo);
        DrawStyledText(ScaleRect(662f, 248f, 560f, 36f), $"NÚCLEO BIOMECÁNICO KAEL (NIVEL {playerLevel})", 20, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(ScaleRect(682f, 286f, 520f, 56f),
            "Sincroniza el oro extraído de los núcleos Vorst para potenciar permanentemente los 5 atributos primarios de combate.",
            14, TextAnchor.MiddleCenter, Color.white, true);

        DrawAttributeTreeNode(new Rect(140f, 445f, 440f, 222f), "FUERZA NEXO", $"ATK Actual: {attackDamage}", "+6 Daño Base (+8% Escala)", "Puño bio-mecánico energizado que amplifica la potencia del combo y el rayo.", colAttrFuerza, ref levelAtk, false, iconFuerzaNexo);
        DrawAttributeTreeNode(new Rect(722f, 445f, 440f, 222f), "VIGOR VITAL", $"HP Máx: {maxHP}", "+35 HP Máx y Cura +40 HP", "Núcleo de corazón ciber-protegido que refuerza la fibra muscular y restaura salud.", colAttrVigor, ref levelHp, true, iconVigorVital);
        DrawAttributeTreeNode(new Rect(1304f, 445f, 440f, 222f), "CADENCIA DE IMPACTO", $"Cadencia: {attackRate:F2}/s", "+0.14 Golpes por Segundo", "Rayo de descarga rápida que acelera la respuesta al presionar [J / Espacio].", colAttrReflejos, ref levelSpd, false, iconCadenciaImpacto);

        DrawAttributeTreeNode(new Rect(430f, 735f, 440f, 222f), "BLINDAJE TÁCTICO", $"Crítico: {Mathf.RoundToInt(critChance * 100)}%", "+4% Probabilidad Crítica (x2 Daño)", "Placas de coraza hexagonal que localizan fracturas alienígenas para impactos críticos.", colAttrInstinto, ref levelCrit, false, iconBlindajeTactico);
        DrawAttributeTreeNode(new Rect(1014f, 735f, 440f, 222f), "RECOLECCIÓN CUÁNTICA", $"Multiplicador: x{goldMultiplier:F2}", "+20% Oro Extraído por Baja", "Chip cuántico multiplicador que maximiza la cosecha de créditos y núcleos Vorst.", colAttrCarronero, ref levelGold, false, iconRecoleccion);
    }

    void DrawAttributeTreeNode(Rect baseRect, string title, string currentStat, string bonusText, string loreDesc, Color attrColor, ref int level, bool healOnBuy, Texture2D iconTex)
    {
        int cost = GetUpgradeCost(level);
        bool canAfford = (gold >= cost);
        bool isUnlocked = (level > 1);

        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        DrawSkinnedCyberPanel(r, canAfford ? texPanelGold : texPanelCyan, canAfford ? colGoldYel : attrColor);

        // Icono Pixel-Art HD con margen generoso
        DrawSkillIconBadge(new Rect(baseRect.x + 16f, baseRect.y + 16f, 60f, 60f), iconTex, isUnlocked, canAfford, attrColor);

        // Textos a la derecha del icono con espacio holgado
        DrawStyledText(ScaleRect(baseRect.x + 86f, baseRect.y + 14f, baseRect.width - 180f, 26f), title, 17, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(baseRect.x + baseRect.width - 94f, baseRect.y + 14f, 78f, 26f), $"Nv.{level}", 17, TextAnchor.MiddleRight, attrColor);

        DrawStyledText(ScaleRect(baseRect.x + 86f, baseRect.y + 40f, baseRect.width - 98f, 22f), currentStat, 16, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(baseRect.x + 86f, baseRect.y + 62f, baseRect.width - 98f, 20f), $"Bono: {bonusText}", 13, TextAnchor.MiddleLeft, attrColor);

        // Lore descriptivo debajo del bloque superior
        DrawStyledText(ScaleRect(baseRect.x + 16f, baseRect.y + 86f, baseRect.width - 32f, 60f), loreDesc, 13, TextAnchor.UpperLeft, new Color(0.80f, 0.88f, 0.96f), true);

        if (DrawSkinnedCyberButton(
            new Rect(baseRect.x + 16f, baseRect.y + 152f, baseRect.width - 32f, 54f),
            canAfford ? texBtnGold : texBtnCyan,
            canAfford ? $"▲ MEJORAR NODO  •  {cost} ORO" : $"🔒 REQUIERE {cost} ORO",
            "",
            16, 11,
            canAfford ? Color.white : new Color(0.65f, 0.72f, 0.82f),
            Color.white, colGoldYel, canAfford))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            gold -= cost;
            level++;
            RecalculateStats();
            if (healOnBuy) currentHP = Mathf.Min(currentHP + 40f, maxHP);
        }
    }

    void DrawBranch2NexoEvolutionTree()
    {
        DrawStyledText(ScaleRect(120f, 232f, 1640f, 34f),
            skillPoints > 0
                ? $"✦ TIENES {skillPoints} PUNTO(S) DE EVOLUCIÓN NEXO DISPONIBLE(S) — ELIGE UNA SENDA PARA DESBLOQUEAR SU SIGUIENTE NODO ✦"
                : "✦ SENDAS EVOLUTIVAS DEL GUANTELETE NEXO — SUBE DE NIVEL EN COMBATE O SINTETIZA UN NODO CON ORO ✦",
            18, TextAnchor.MiddleCenter, skillPoints > 0 ? colGoldYel : colExpPurp);

        DrawEvolutionPathColumn(
            140f, "SENDA DE LA FURIA", "Ofensiva Crítica y Ruptura",
            new Color(1f, 0.38f, 0.36f), texBtnCrimson, ref branchFuria,
            "RANGO I: Puños de Ignición (+18% ATK, +5% CRIT)",
            "RANGO II: Fractura de Núcleo (+36% ATK, +10% CRIT)",
            "RANGO III+: Sobrecarga Letal (+54% ATK, +15% CRIT)",
            iconFuriaContinua
        );

        DrawEvolutionPathColumn(
            722f, "SENDA DEL GUARDIÁN", "Bio-Coraza y Regeneración",
            colHpGreen, texBtnEmerald, ref branchGuardian,
            "RANGO I: Tejido Nano-Activo (+40 HP, +3.5 HP/s)",
            "RANGO II: Escudo Celular (+80 HP, +7.0 HP/s)",
            "RANGO III+: Inmortalidad Nexo (+120 HP, +10.5 HP/s)",
            iconTsunamiSismico
        );

        DrawEvolutionPathColumn(
            1304f, "SENDA DEL NEXO", "Resonancia y Cañón de Plasma",
            colCyanNexo, texBtnCyan, ref branchNexo,
            "RANGO I: Condensador Ágil (+25% Carga Ultimate)",
            "RANGO II: Reactor de Flujo (+50% Carga Ultimate)",
            "RANGO III+: Singularidad Nexo (+75% Carga Ultimate)",
            iconImpactoMeteoro
        );
    }

    void DrawEvolutionPathColumn(float baseX, string pathTitle, string pathSub, Color pathColor, Texture2D btnSkin, ref int branchLv, string tier1Desc, string tier2Desc, string tier3Desc, Texture2D pathIcon)
    {
        float colW = 440f;
        float centerX = baseX + colW * 0.5f;

        DrawCircuitConnection(new Vector2(centerX, 370f), new Vector2(centerX, 435f), pathColor, branchLv >= 1);
        DrawCircuitConnection(new Vector2(centerX, 555f), new Vector2(centerX, 610f), pathColor, branchLv >= 2);
        DrawCircuitConnection(new Vector2(centerX, 730f), new Vector2(centerX, 785f), pathColor, branchLv >= 3);

        Rect header = ScaleRect(baseX, 278f, colW, 92f);
        DrawSkinnedCyberPanel(header, texPanelCyan, pathColor);

        // Icono en cabecera de la columna
        DrawSkillIconBadge(new Rect(baseX + 16f, 290f, 68f, 68f), pathIcon, branchLv > 0, skillPoints > 0, pathColor);
        DrawStyledText(ScaleRect(baseX + 90f, 288f, colW - 100f, 34f), $"{pathTitle} (Nv.{branchLv})", 19, TextAnchor.MiddleLeft, pathColor);
        DrawStyledText(ScaleRect(baseX + 90f, 324f, colW - 100f, 28f), pathSub, 13, TextAnchor.MiddleLeft, Color.white);

        DrawTierNodeBox(new Rect(baseX + 20f, 435f, colW - 40f, 120f), "NODO 1", tier1Desc, branchLv >= 1, branchLv == 0, pathColor, pathIcon);
        DrawTierNodeBox(new Rect(baseX + 20f, 610f, colW - 40f, 120f), "NODO 2", tier2Desc, branchLv >= 2, branchLv == 1, pathColor, pathIcon);
        DrawTierNodeBox(new Rect(baseX + 20f, 785f, colW - 40f, 120f), "NODO 3+", tier3Desc, branchLv >= 3, branchLv >= 2, pathColor, pathIcon);

        int goldSynthCost = 65 + (branchLv * 45);
        bool canUseSkillPt = (skillPoints > 0);
        bool canUseGold = (!canUseSkillPt && gold >= goldSynthCost);
        bool canUpgrade = canUseSkillPt || canUseGold;

        string btnTxt = canUseSkillPt
            ? "✦ EVOLUCIONAR SENDA (1 PTO NEXO)"
            : (canUseGold ? $"⚡ SINTETIZAR NODO ({goldSynthCost} ORO)" : $"🔒 REQUIERE 1 PTO NEXO O {goldSynthCost} ORO");

        if (DrawSkinnedCyberButton(
            new Rect(baseX + 20f, 922f, colW - 40f, 64f),
            canUpgrade ? btnSkin : texBtnCyan,
            btnTxt, "",
            15, 11, Color.white, Color.white, pathColor, canUpgrade))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (canUseSkillPt) skillPoints--;
            else gold -= goldSynthCost;

            branchLv++;
            RecalculateStats();
            currentHP = maxHP;
        }
    }

    void DrawTierNodeBox(Rect baseRect, string nodeTag, string desc, bool unlocked, bool isNext, Color pathColor, Texture2D iconTex)
    {
        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        DrawSkinnedCyberPanel(r, unlocked ? texPanelGold : texPanelCyan, unlocked ? pathColor : (isNext ? colGoldYel : Color.gray));

        // Icono en el nodo del tier
        DrawSkillIconBadge(new Rect(baseRect.x + 12f, baseRect.y + 14f, 54f, 54f), iconTex, unlocked, isNext, pathColor);

        string statusBadge = unlocked ? "✔ DESBLOQUEADO" : (isNext ? "◎ SIGUIENTE NODO" : "🔒 POR DESBLOQUEAR");
        Color badgeCol = unlocked ? colHpGreen : (isNext ? colGoldYel : new Color(0.55f, 0.62f, 0.72f));

        DrawStyledText(ScaleRect(baseRect.x + 76f, baseRect.y + 12f, 120f, 26f), nodeTag, 16, TextAnchor.MiddleLeft, unlocked ? Color.white : pathColor);
        DrawStyledText(ScaleRect(baseRect.x + baseRect.width - 190f, baseRect.y + 12f, 178f, 26f), statusBadge, 13, TextAnchor.MiddleRight, badgeCol);
        DrawStyledText(ScaleRect(baseRect.x + 76f, baseRect.y + 40f, baseRect.width - 88f, 68f), desc, 14, TextAnchor.MiddleLeft, unlocked ? Color.white : new Color(0.78f, 0.85f, 0.94f), true);
    }

    void DrawBranch3ArsenalArmorTree()
    {
        Vector2 coreBottom = new Vector2(942f, 360f);
        Vector2 arm1Top = new Vector2(510f, 450f);
        Vector2 arm2Top = new Vector2(1374f, 450f);
        Vector2 arm3Top = new Vector2(510f, 740f);
        Vector2 arm4Top = new Vector2(1374f, 740f);

        DrawCircuitConnection(coreBottom, arm1Top, colCyanNexo, levelArmor > 0);
        DrawCircuitConnection(coreBottom, arm2Top, colHpGreen, levelThrusters > 0);
        DrawCircuitConnection(new Vector2(arm1Top.x, 675f), arm3Top, colExpPurp, levelOvercharge > 0);
        DrawCircuitConnection(new Vector2(arm2Top.x, 675f), arm4Top, colGoldYel, levelSeismicEdge > 0);

        Rect chassisRect = ScaleRect(622f, 236f, 640f, 124f);
        DrawSkinnedCyberPanel(chassisRect, texPanelGold, colGoldYel);
        DrawStyledText(ScaleRect(622f, 248f, 640f, 36f), "MATRIZ EVOLUTIVA & ARSENAL NEXO", 20, TextAnchor.MiddleCenter, colGoldYel);
        DrawStyledText(ScaleRect(642f, 286f, 600f, 56f),
            "Sincroniza los artefactos ancestrales de Lía, la propulsión armónica y la energía Vorst para potenciar el chasis.",
            14, TextAnchor.MiddleCenter, Color.white, true);

        DrawArsenalNode(
            new Rect(230f, 450f, 560f, 225f),
            "VÍNCULO FRATERNAL (LÍA): CORAZA",
            $"Reducción Daño: {Mathf.RoundToInt(armorDamageReduction * 100)}%  |  +{levelArmor * 28} HP",
            "+6% Defensa Directa y +28 HP Máx por Nivel",
            "Colgante ancestral brillante que resuena con el Guantelete, reforzando la fibra celular ante impactos.",
            colCyanNexo, 30, ref levelArmor, true, iconVinculoFraternal
        );

        DrawArsenalNode(
            new Rect(1094f, 450f, 560f, 225f),
            "ARMONÍA HUMANA: PROPULSORES",
            $"Velocidad Mov: +{levelThrusters * 10}%  |  Cadencia: +{levelThrusters * 6}%",
            "+10% Velocidad [A / D] y +6% Velocidad de Golpe",
            "Silueta envuelta en luz dorada que activa micro-turbinas tácticas para maniobras rápidas de esquiva.",
            colHpGreen, 28, ref levelThrusters, false, iconArmoniaHumana
        );

        DrawArsenalNode(
            new Rect(230f, 740f, 560f, 225f),
            "DISONANCIA VORST: SOBRECARGA",
            $"Carga Ulti: +{levelOvercharge * 18}%  |  Daño Ulti: +{levelOvercharge * 12}%",
            "+18% Velocidad de Carga y +12% Daño del Rayo [K]",
            "Espiral alienígena corrupta que sobrecarga los inyectores de plasma en cada impacto manual.",
            colExpPurp, 38, ref levelOvercharge, false, iconDisonanciaVorst
        );

        DrawArsenalNode(
            new Rect(1094f, 740f, 560f, 225f),
            "TSUNAMI SÍSMICO: FILO VORST",
            $"Multiplicador 3er Golpe: {Mathf.RoundToInt(comboFinisherMultiplier * 100)}%  |  +{levelSeismicEdge * 3}% CRIT",
            "+20% Daño en el 3er Golpe del Combo y +3% Crítico",
            "Fisura tectónica con onda expansiva devastadora al rematar el combo de 3 golpes con [J / Espacio].",
            colGoldYel, 42, ref levelSeismicEdge, false, iconTsunamiSismico
        );
    }

    void DrawArsenalNode(Rect baseRect, string title, string currentStat, string bonusText, string desc, Color accentCol, int baseCost, ref int level, bool healOnBuy, Texture2D iconTex)
    {
        bool isMaxed = (level >= 5);
        int cost = isMaxed ? 0 : GetArsenalCost(baseCost, level);
        bool canAfford = (!isMaxed && gold >= cost);
        bool isUnlocked = (level > 0);

        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        DrawSkinnedCyberPanel(r, (canAfford || level > 0) ? texPanelGold : texPanelCyan, accentCol);

        // Icono Pixel-Art HD con margen generoso
        DrawSkillIconBadge(new Rect(baseRect.x + 18f, baseRect.y + 16f, 62f, 62f), iconTex, isUnlocked, canAfford, accentCol);

        string lvTag = (level == 0) ? "NO INSTALADO" : (isMaxed ? "Nv.5 (MÁX)" : $"Nv.{level} / 5");
        DrawStyledText(ScaleRect(baseRect.x + 92f, baseRect.y + 14f, baseRect.width - 240f, 26f), title, 17, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(baseRect.x + baseRect.width - 146f, baseRect.y + 14f, 130f, 26f), lvTag, 15, TextAnchor.MiddleRight, level > 0 ? colHpGreen : colGoldYel);

        DrawStyledText(ScaleRect(baseRect.x + 92f, baseRect.y + 42f, baseRect.width - 104f, 22f), currentStat, 16, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(baseRect.x + 92f, baseRect.y + 64f, baseRect.width - 104f, 20f), $"Efecto: {bonusText}", 14, TextAnchor.MiddleLeft, accentCol);

        DrawStyledText(ScaleRect(baseRect.x + 18f, baseRect.y + 88f, baseRect.width - 36f, 60f), desc, 13, TextAnchor.UpperLeft, new Color(0.80f, 0.88f, 0.96f), true);

        string btnTxt = isMaxed
            ? "✔ EQUIPAMIENTO AL NIVEL MÁXIMO"
            : (canAfford ? $"▲ {(level == 0 ? "FABRICAR E INSTALAR" : "MEJORAR MÓDULO")}  •  {cost} ORO" : $"🔒 REQUIERE {cost} ORO");

        if (DrawSkinnedCyberButton(
            new Rect(baseRect.x + 18f, baseRect.y + 154f, baseRect.width - 36f, 54f),
            isMaxed ? texBtnEmerald : (canAfford ? texBtnGold : texBtnCyan),
            btnTxt, "",
            15, 11, Color.white, Color.white, accentCol, canAfford))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            gold -= cost;
            level++;
            RecalculateStats();
            if (healOnBuy) currentHP = Mathf.Min(currentHP + 35f, maxHP);
        }
    }

    // =========================================================================
    // PANTALLA DE GAME OVER PROFESIONAL CON ARTE ILUSTRADO Y BOTONES BISELADOS
    // =========================================================================
    void DrawGameOverModal(WaveSpawner spawner)
    {
        // 1. Fondo atmosférico carmesí oscuro con líneas de escaneo de emergencia
        GUI.color = new Color(0.05f, 0.01f, 0.02f, 0.90f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        float scanY = Mathf.PingPong(Time.unscaledTime * 220f, 1080f);
        GUI.color = new Color(1f, 0.15f, 0.25f, 0.10f);
        GUI.DrawTexture(ScaleRect(0f, scanY, 1920f, 16f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 2. Marco Principal Biselado Cyberpunk Carmesí (1160x730 centrado)
        Rect modalRect = ScaleRect(380f, 155f, 1160f, 730f);
        DrawSkinnedCyberPanel(modalRect, texPanelCrimson, new Color(1f, 0.25f, 0.35f));

        // 3. Cabecera de Telemetría y Título de Alto Impacto
        DrawStyledText(ScaleRect(420f, 174f, 1080f, 24f),
            "[ SYSTEM_LOG: SEÑAL VITAL CRÍTICA  //  PROTOCOLO DE RECONSTRUCCIÓN NEXO ACTIVO ]",
            13, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.62f));

        DrawStyledText(ScaleRect(420f, 198f, 1080f, 52f),
            "⚠ KAEL HA CAÍDO EN COMBATE ⚠",
            36, TextAnchor.MiddleCenter, new Color(1f, 0.32f, 0.40f));

        // 4. Ilustración Widescreen HD de Game Over (Izquierda: 580x326)
        Rect artFrame = ScaleRect(424f, 262f, 584f, 328f);
        DrawCyberBox(artFrame, new Color(0.04f, 0.02f, 0.05f, 1f), new Color(1f, 0.32f, 0.45f, 0.95f), 2.5f);

        if (texGameOverBanner != null)
        {
            Rect innerArt = ScaleRect(428f, 266f, 576f, 320f);
            GUI.DrawTexture(innerArt, texGameOverBanner, ScaleMode.ScaleAndCrop);

            // Sutil línea de escaneo holográfico sobre la ilustración
            float artScanY = innerArt.y + Mathf.PingPong(Time.unscaledTime * (innerArt.height * 0.45f), Mathf.Max(1f, innerArt.height - 4f));
            GUI.color = new Color(0.22f, 0.90f, 1f, 0.28f);
            GUI.DrawTexture(new Rect(innerArt.x, artScanY, innerArt.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Etiqueta sobre la esquina inferior de la ilustración
        Rect artBadge = ScaleRect(436f, 548f, 310f, 32f);
        DrawCyberBox(artBadge, new Color(0.04f, 0.08f, 0.16f, 0.92f), colCyanNexo, 1.5f);
        DrawStyledText(artBadge, "⚡ MATRIZ DEL GUANTELETE: INTACTA", 13, TextAnchor.MiddleCenter, colCyanNexo);

        // 5. Tarjeta Derecha: Reporte Táctico y Progreso Conservado (486x328)
        Rect statsCard = ScaleRect(1026f, 262f, 470f, 328f);
        DrawSkinnedCyberPanel(statsCard, texPanelCyan, colCyanNexo);

        DrawStyledText(ScaleRect(1046f, 276f, 430f, 32f), "📊 REPORTE DE SINCRONIZACIÓN", 19, TextAnchor.MiddleCenter, colCyanNexo);

        int waveReached = (spawner != null) ? Mathf.Max(1, spawner.currentWave) : 1;
        int totalWaves = (spawner != null) ? spawner.totalWaves : 10;
        int kills = (spawner != null) ? spawner.totalKillsInLevel : 0;

        string zoneLabel = (levelNumber == 3)
            ? "NIVEL 3 — COLMENA VORST"
            : ((levelNumber == 2) ? "NIVEL 2 — BASE VORST" : "NIVEL 1 — SECTOR 7");
        DrawStatRow(1050f, 320f, 422f, "ZONA DE COMBATE:", zoneLabel, Color.white);
        DrawStatRow(1050f, 358f, 422f, "OLEADA ALCANZADA:", $"OLEADA {waveReached} / {totalWaves}", new Color(1f, 0.48f, 0.52f));
        DrawStatRow(1050f, 396f, 422f, "VORST ELIMINADOS:", $"{kills} BAJAS CONFIRMADAS", colCyanNexo);
        DrawStatRow(1050f, 434f, 422f, "NIVEL DE KAEL:", $"NIVEL {playerLevel} (100% CONSERVADO)", colHpGreen);
        DrawStatRow(1050f, 472f, 422f, "ORO RESGUARDADO:", $"● {gold} ORO DISPONIBLE", colGoldYel);

        // Mensaje de N.E.X.A. dentro del reporte
        DrawStyledText(ScaleRect(1048f, 518f, 426f, 58f),
            "\"Tu oro y tus mejoras han sido respaldados por N.E.X.A. Mejora tus nodos y vuelve a la carga.\"",
            14, TextAnchor.MiddleCenter, new Color(0.82f, 0.92f, 1f), true);

        // 6. Botones de Acción Biselados Profesionales en la Parte Inferior
        int upgradesAvail = CountAvailableUpgrades() + skillPoints;
        string treeBtnSub = (upgradesAvail > 0)
            ? $"¡TIENES {upgradesAvail} MEJORA(S) DISPONIBLE(S) AHORA MISMO!"
            : "INVIERTE TU ORO ANTES DE LA OLEADA 1";

        if (DrawSkinnedCyberButton(
            new Rect(424f, 610f, 440f, 84f),
            texBtnEmerald,
            "⚡ RECONSTRUIR Y REINTENTAR",
            "CONSERVAR ORO Y MEJORAS ACTUALES",
            19, 13, Color.white, new Color(0.80f, 1f, 0.90f), colHpGreen, true))
        {
            RetryFromGameOver();
        }

        if (DrawSkinnedCyberButton(
            new Rect(880f, 610f, 410f, 84f),
            texBtnGold,
            "🧬 ABRIR ÁRBOL Y MEJORAR",
            treeBtnSub,
            18, 12, Color.white, colGoldYel, colGoldYel, true))
        {
            RetryFromGameOver();
            ToggleUpgradeTreeModal();
        }

        if (DrawSkinnedCyberButton(
            new Rect(1306f, 610f, 190f, 84f),
            texBtnCyan,
            "⌂ MENÚ",
            "SALIR AL INICIO",
            18, 12, Color.white, colCyanNexo, colCyanNexo, true))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }

    void DrawStatRow(float x, float y, float w, string label, string val, Color valCol)
    {
        Rect rowBg = ScaleRect(x, y, w, 32f);
        GUI.color = new Color(0.03f, 0.06f, 0.13f, 0.85f);
        GUI.DrawTexture(rowBg, Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawStyledText(ScaleRect(x + 10f, y, w * 0.52f, 32f), label, 14, TextAnchor.MiddleLeft, new Color(0.75f, 0.84f, 0.95f));
        DrawStyledText(ScaleRect(x + w * 0.45f, y, w * 0.55f - 10f, 32f), val, 15, TextAnchor.MiddleRight, valCol);
    }

    // =========================================================================
    // PANTALLA DE VICTORIA PROFESIONAL CON ILUSTRACIÓN Y BOTONES BISELADOS
    // =========================================================================
    void DrawVictoryModal(WaveSpawner spawner)
    {
        GUI.color = new Color(0.01f, 0.02f, 0.06f, 0.90f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = ScaleRect(380f, 155f, 1160f, 730f);
        DrawSkinnedCyberPanel(box, texPanelGold, colGoldYel);

        int kills = (spawner != null) ? spawner.totalKillsInLevel : 60;
        int timeSec = Mathf.RoundToInt(levelTimer);
        int timeBonus = Mathf.Max(100, 1000 - (timeSec * 4));
        int totalScore = (gold * 10) + (kills * 25) + timeBonus;

        string vicTitle = (levelNumber == 3)
            ? "★ ¡COLMENA VORST CONQUISTADA! ★"
            : ((levelNumber == 2) ? "★ ¡BASE VORST CONQUISTADA! ★" : "★ ¡SECTOR 7 LIBERADO! ★");
        DrawStyledText(ScaleRect(420f, 178f, 1080f, 48f), vicTitle, 36, TextAnchor.MiddleCenter, colGoldYel);
        DrawStyledText(ScaleRect(420f, 224f, 1080f, 30f), $"Decisión registrada: <color=#38e1ff>{choiceSummary}</color>", 18, TextAnchor.MiddleCenter, Color.white);

        // Ilustración Widescreen HD de Victoria (Izquierda: 584x328)
        Rect artFrame = ScaleRect(424f, 266f, 584f, 328f);
        DrawCyberBox(artFrame, new Color(0.03f, 0.04f, 0.09f, 1f), colGoldYel, 2.5f);
        if (texVictoryBanner != null)
        {
            Rect innerArt = ScaleRect(428f, 270f, 576f, 320f);
            GUI.DrawTexture(innerArt, texVictoryBanner, ScaleMode.ScaleAndCrop);
        }

        // Tarjeta Derecha: Desglose de Puntaje (470x328)
        Rect scoreCard = ScaleRect(1026f, 266f, 470f, 328f);
        DrawSkinnedCyberPanel(scoreCard, texPanelCyan, colCyanNexo);

        DrawStyledText(ScaleRect(1046f, 282f, 430f, 32f), "🏆 REGISTRO DE COMBATE NEXO", 19, TextAnchor.MiddleCenter, colGoldYel);
        DrawStatRow(1050f, 330f, 422f, "TIEMPO EMPLEADO:", $"{timeSec / 60:00}:{timeSec % 60:00}", colCyanNexo);
        DrawStatRow(1050f, 372f, 422f, "VORST DERROTADOS:", $"{kills} ENEMIGOS", colCyanNexo);
        DrawStatRow(1050f, 414f, 422f, "NIVEL Y ORO:", $"Nv.{playerLevel} ({gold} ORO)", colGoldYel);
        DrawStatRow(1050f, 456f, 422f, "BONO DE VELOCIDAD:", $"+{timeBonus} PTS", colHpGreen);

        Rect totalBox = ScaleRect(1050f, 506f, 422f, 64f);
        DrawCyberBox(totalBox, new Color(0.06f, 0.16f, 0.12f, 0.95f), colHpGreen, 2f);
        DrawStyledText(totalBox, $"PUNTAJE TOTAL:  <color=#2ee678>{totalScore} PTS</color>", 22, TextAnchor.MiddleCenter, Color.white);

        if (levelNumber >= 3)
        {
            bool hasLevel4Scene = (levelNumber == 3) && !string.IsNullOrEmpty(nextLevelSceneName) && Application.CanStreamedLevelBeLoaded(nextLevelSceneName);
            if (hasLevel4Scene)
            {
                if (DrawSkinnedCyberButton(
                    new Rect(424f, 612f, 520f, 82f),
                    texBtnEmerald,
                    "⚡ AVANZAR AL NIVEL 4 (TRONO FINAL)",
                    "SUBIR A LA CÚPULA DONDE ESTÁ CAUTIVA LÍA",
                    18, 12, Color.white, new Color(0.82f, 1f, 0.90f), colHpGreen, true))
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                    SaveProgressForNextLevel();
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(nextLevelSceneName);
                }

                if (DrawSkinnedCyberButton(
                    new Rect(964f, 612f, 532f, 82f),
                    texBtnGold,
                    "🎬 EPÍLOGO FINAL (ISSUE #24)",
                    "VER LOS 3 FINALES DE RESCATE DE LÍA",
                    18, 12, Color.white, colGoldYel, colGoldYel, true))
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                    SaveProgressForNextLevel();
                    EndingCinematicController.LaunchEndingFromMoralState();
                }
            }
            else
            {
                // Si estamos en el Nivel 4 Final (o el Nivel 4 de Zahir aún no está integrado en Build Settings), permite disparar el Epílogo Final de Campaña (Issue #24)
                if (DrawSkinnedCyberButton(
                    new Rect(424f, 612f, 640f, 82f),
                    texBtnEmerald,
                    "🎬 VER DESENLACE FINAL (RESCATE DE LÍA)",
                    "EVALUAR COMPASIÓN VS RIGOR (3 FINALES DE CAMPAÑA)",
                    19, 13, Color.white, new Color(0.82f, 1f, 0.90f), colHpGreen, true))
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                    SaveProgressForNextLevel();
                    EndingCinematicController.LaunchEndingFromMoralState();
                }

                if (DrawSkinnedCyberButton(
                    new Rect(1086f, 612f, 410f, 82f),
                    texBtnCyan,
                    "⌂ MENÚ PRINCIPAL",
                    "GUARDAR Y VOLVER AL INICIO",
                    19, 12, Color.white, colCyanNexo, colCyanNexo, true))
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                    SaveProgressForNextLevel();
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(0);
                }
            }
            return;
        }

        bool hasNextLevel = !string.IsNullOrEmpty(nextLevelSceneName) && Application.CanStreamedLevelBeLoaded(nextLevelSceneName);

        if (hasNextLevel || levelNumber == 1)
        {
            string nextBtnTitle = (levelNumber == 1)
                ? "▶ AVANZAR AL NIVEL 2 (CINEMÁTICA PUENTE)"
                : "▶ AVANZAR AL NIVEL 3 (NAVE NÚCLEO COLMENA)";

            if (DrawSkinnedCyberButton(
                new Rect(424f, 612f, 640f, 82f),
                texBtnEmerald,
                nextBtnTitle,
                "CONSERVAR NIVEL, ORO Y ÁRBOL DE MEJORAS",
                19, 13, Color.white, new Color(0.82f, 1f, 0.90f), colHpGreen, true))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                SaveProgressForNextLevel();
                Time.timeScale = 1f;

                if (levelNumber == 1)
                {
                    // BLOQUE 3 (Issue #18): Ejecutar Cinemática Puente Interactiva y Tramo Jugable antes de cargar Lab_Zahir
                    BridgeLevel1To2Controller.StartBridgeSequence();
                }
                else
                {
                    SceneManager.LoadScene(nextLevelSceneName);
                }
            }

            if (DrawSkinnedCyberButton(
                new Rect(1086f, 612f, 410f, 82f),
                texBtnCyan,
                "⌂ MENÚ PRINCIPAL",
                "GUARDAR Y VOLVER AL INICIO",
                19, 12, Color.white, colCyanNexo, colCyanNexo, true))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                SaveProgressForNextLevel();
                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
            }
        }
        else
        {
            if (DrawSkinnedCyberButton(
                new Rect(424f, 612f, 520f, 82f),
                texBtnCyan,
                $"↻ REPETIR NIVEL {levelNumber}",
                "VOLVER A DESPLEGARSE EN ESTA ZONA",
                20, 13, Color.white, colCyanNexo, colCyanNexo, true))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (DrawSkinnedCyberButton(
                new Rect(976f, 612f, 520f, 82f),
                texBtnGold,
                "⌂ MENÚ PRINCIPAL",
                "VOLVER A LA PANTALLA DE TÍTULO",
                20, 13, Color.white, colGoldYel, colGoldYel, true))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
            }
        }
    }

    // =========================================================================
    // PAUSA Y DILEMA MORAL CON PANELES Y BOTONES BISELADOS
    // =========================================================================
    void DrawPauseModal()
    {
        GUI.color = new Color(0f, 0.01f, 0.04f, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = ScaleRect(630, 145, 660, 760);
        DrawSkinnedCyberPanel(box, texPanelCyan, colCyanNexo);

        DrawStyledText(ScaleRect(630, 168, 660, 44), "PAUSA TÁCTICA", 32, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(ScaleRect(630, 212, 660, 28), zoneName, 17, TextAnchor.MiddleCenter, new Color(0.78f, 0.88f, 0.98f));

        if (DrawSkinnedCyberButton(new Rect(750, 256, 420, 62), texBtnEmerald, "▶ CONTINUAR COMBATE", "REANUDAR MISIÓN [ESC]", 19, 12, Color.white, new Color(0.82f, 1f, 0.90f), colHpGreen, true))
        {
            TogglePause();
        }

        int unlockedCodex = VorstCodexUI.GetUnlockedSpeciesCount();
        if (DrawSkinnedCyberButton(new Rect(750, 328, 420, 62), texBtnCyan, $"📘 CÓDICE VORST ({unlockedCodex}/6) [C]", "BESTIARIO, DEBILIDADES Y BAJAS", 17, 11, Color.white, colGoldYel, colCyanNexo, true))
        {
            OpenVorstCodexModal(returnToPauseOnClose: true);
        }

        if (DrawSkinnedCyberButton(new Rect(750, 400, 420, 62), texBtnCyan, "⚙ ARSENAL DE VANGUARDIA [B]", "VARIANTES DEL GUANTELETE Y DRON N.E.X.A.", 17, 11, Color.white, colGoldYel, colCyanNexo, true))
        {
            isPaused = false;
            OpenArsenalShopModal();
        }

        if (DrawSkinnedCyberButton(new Rect(750, 472, 420, 62), texBtnGold, "🧬 ABRIR ÁRBOL DE MEJORAS", "GESTIONAR ATRIBUTOS Y EVOLUCIÓN [TAB]", 17, 11, Color.white, colGoldYel, colGoldYel, true))
        {
            isPaused = false;
            OpenUpgradeTreeAtTab(0);
        }

        if (DrawSkinnedCyberButton(new Rect(750, 544, 420, 58), texBtnCrimson, "↻ REINICIAR NIVEL", "COMENZAR DESDE LA OLEADA 1", 17, 11, Color.white, new Color(1f, 0.80f, 0.82f), colAttrFuerza, true))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (DrawSkinnedCyberButton(new Rect(750, 612, 420, 58), texBtnPurple, "⌂ SALIR AL MENÚ PRINCIPAL", "VOLVER A LA PANTALLA DE TÍTULO", 17, 11, Color.white, colCyanNexo, colCyanNexo, true))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        DrawTopRightAudioWidget(809f, 682f, 302f, 68f);
    }

    void DrawNarrativeDilemmaModal()
    {
        GUI.color = new Color(0f, 0f, 0.05f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = ScaleRect(420, 175, 1080, 660);
        DrawSkinnedCyberPanel(box, texPanelCyan, colCyanNexo);

        bool isLv3 = (levelNumber == 3);
        bool isLv2 = (levelNumber == 2);

        string title = isLv3
            ? "DECISIÓN DE LA COLMENA VORST: LAS CÁPSULAS DE ESTASIS"
            : (isLv2 ? "DECISIÓN DE LA BASE VORST: LA TORRE DE CONVERGENCIA" : "DECISIÓN DEL SECTOR 7: EL NÚCLEO DEL COMANDANTE");

        string desc = isLv3
            ? "Tras caer el Carcelero Biotecnológico VORTEX-OMEGA, el reactor maestro de estasis queda en tus manos.\nPuedes liberar de forma segura todas las cápsulas de civiles humanos o drenar el reactor hacia tu Guantelete."
            : (isLv2
                ? "Tras caer el Arconte Vorst, la esfera púrpura de la Base Vorst queda expuesta.\nPuedes liberar a los prisioneros humanos en estasis o sobrecargar tu Guantelete con sus orbes."
                : "Entre los restos del Comandante Vorst encuentras un reactor bio-energético aún activo,\nmientras un grupo de sobrevivientes atrapados en el puente pide auxilio médico.");

        string compDesc = isLv3
            ? "Liberar las Cápsulas de Estasis Civiles: Desvía la energía del núcleo de Vortex-Omega para abrir de forma segura todas las cápsulas humanas del Nivel 3."
            : (isLv2
                ? "Desactivar las cámaras de recolección para liberar a los prisioneros humanos cautivos en la Base Vorst."
                : "Usar la energía del reactor para estabilizar el refugio de los sobrevivientes del Sector 7.");

        string rigorDesc = isLv3
            ? "Drenar el Reactor de Estasis al Guantelete: Absorbe la energía bio-eléctrica de toda la cámara para sobrecargar el cañón Nexo al máximo antes del desenlace."
            : (isLv2
                ? "Drenar los 4 orbes psiónicos del Arconte directamente hacia tu brazo para abrir brecha a la Nave Núcleo."
                : "Devorar el núcleo Vorst con el Guantelete para acelerar tu marcha hacia Lía sin mirar atrás.");

        string compBonusTxt = isLv3 ? "+1 COMPASIÓN\n+25% VIDA MÁXIMA Y CURACIÓN TOTAL" : "+1 COMPASIÓN\n+20% VIDA MÁXIMA PERMANENTE";
        string rigorBonusTxt = isLv3 ? "+1 RIGOR\n+30% DAÑO DE ATAQUE Y CRÍTICO" : "+1 RIGOR\n+25% DAÑO DE ATAQUE PERMANENTE";

        DrawStyledText(ScaleRect(460, 204, 1000, 45), title, 26, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(ScaleRect(500, 256, 920, 75), desc, 18, TextAnchor.MiddleCenter, Color.white, true);

        // Opción 1: Compasión
        Rect cardComp = ScaleRect(470, 350, 460, 430);
        DrawSkinnedCyberPanel(cardComp, texPanelCyan, colCyanNexo);
        DrawStyledText(ScaleRect(490, 372, 420, 40), "♦ COMPASIÓN (PUREZA NEXO)", 22, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(ScaleRect(500, 430, 400, 120), compDesc, 17, TextAnchor.MiddleCenter, Color.white, true);
        DrawStyledText(ScaleRect(490, 565, 420, 60), compBonusTxt, 17, TextAnchor.MiddleCenter, colHpGreen, true);

        if (DrawSkinnedCyberButton(new Rect(500, 655, 400, 72), texBtnEmerald, "♦ ELEGIR COMPASIÓN", "PROTEGER LA HUMANIDAD", 19, 12, Color.white, colHpGreen, colCyanNexo, true))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
                AudioManager.Instance.PlayVictoryDilemmaMusic();
            }
            compassionPoints++;
            choiceSummary = isLv3
                ? "Liberaste todas las Cápsulas de Estasis Civiles (+1 Compasión)"
                : (isLv2 ? "Liberaste a los prisioneros de la Base Vorst (+1 Compasión)" : "Salvaste a los sobrevivientes (+1 Compasión, +20% HP)");
            RecalculateStats();
            currentHP = maxHP;
            isChoosingDilemma = false;
            isLevelCompleted = true;
            SaveProgressForNextLevel();
        }

        // Opción 2: Rigor
        Rect cardRigor = ScaleRect(990, 350, 460, 430);
        DrawSkinnedCyberPanel(cardRigor, texPanelCrimson, new Color(1f, 0.35f, 0.55f));
        DrawStyledText(ScaleRect(1010, 372, 420, 40), "❖ RIGOR (DISONANCIA VORST)", 22, TextAnchor.MiddleCenter, new Color(1f, 0.45f, 0.62f));
        DrawStyledText(ScaleRect(1020, 430, 400, 120), rigorDesc, 17, TextAnchor.MiddleCenter, Color.white, true);
        DrawStyledText(ScaleRect(1010, 565, 420, 60), rigorBonusTxt, 17, TextAnchor.MiddleCenter, colGoldYel, true);

        if (DrawSkinnedCyberButton(new Rect(1020, 655, 400, 72), texBtnCrimson, "❖ ELEGIR RIGOR", "SOBRECARGAR EL GUANTELETE", 19, 12, Color.white, colGoldYel, colAttrFuerza, true))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
                AudioManager.Instance.PlayVictoryDilemmaMusic();
            }
            rigorPoints++;
            choiceSummary = isLv3
                ? "Drenaste el Reactor de Estasis al Guantelete (+1 Rigor)"
                : (isLv2 ? "Drenaste los orbes psiónicos del Arconte (+1 Rigor)" : "Absorbiste el núcleo Vorst (+1 Rigor, +25% ATK)");
            RecalculateStats();
            isChoosingDilemma = false;
            isLevelCompleted = true;
            SaveProgressForNextLevel();
        }
    }

    // =========================================================================
    // WIDGETS DE AUDIO, BARRA SOBRE KAEL Y PRIMITIVAS DE DIBUJO
    // =========================================================================
    void DrawTopRightAudioWidget(float baseX, float baseY, float baseW, float baseH)
    {
        bool muted = (AudioManager.Instance != null) && AudioManager.Instance.isMuted;
        float vol = (AudioManager.Instance != null) ? AudioManager.Instance.masterVolume : 0.85f;
        int volPct = muted ? 0 : Mathf.RoundToInt(vol * 100f);

        Color accent = muted ? new Color(1f, 0.35f, 0.42f, 0.9f) : colCyanNexo;
        Rect panelRect = ScaleRect(baseX, baseY, baseW, baseH);
        DrawSkinnedCyberPanel(panelRect, muted ? texPanelCrimson : texPanelCyan, accent);

        if (DrawSkinnedCyberButton(
            new Rect(baseX + 8f, baseY + 8f, 64f, baseH - 16f),
            muted ? texBtnCrimson : texBtnCyan,
            muted ? "🔇" : "🔊", "", 20, 10,
            muted ? new Color(1f, 0.45f, 0.5f) : colCyanNexo, Color.white, accent, true))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }

        DrawStyledText(ScaleRect(baseX + 80f, baseY + 8f, baseW - 92f, 22f),
            muted ? "AUDIO: SILENCIADO" : $"VOLUMEN: {volPct}%",
            13, TextAnchor.MiddleLeft, muted ? new Color(1f, 0.5f, 0.55f) : Color.white);

        Rect minusBtn = ScaleRect(baseX + 80f, baseY + 34f, 32f, 24f);
        DrawCyberBox(minusBtn, new Color(0.08f, 0.13f, 0.22f), colBorder, 1f);
        DrawStyledText(minusBtn, "−", 16, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(minusBtn, GUIContent.none, GUIStyle.none) && AudioManager.Instance != null)
        {
            AudioManager.Instance.isMuted = false;
            AudioManager.Instance.masterVolume = Mathf.Clamp01(vol - 0.10f);
            AudioManager.Instance.PlayUIClick();
        }

        Rect barRect = ScaleRect(baseX + 118f, baseY + 38f, 134f, 16f);
        GUI.color = new Color(0.08f, 0.12f, 0.20f, 1f);
        GUI.DrawTexture(barRect, Texture2D.whiteTexture);
        float fillRatio = muted ? 0f : Mathf.Clamp01(vol);
        GUI.color = muted ? new Color(0.5f, 0.2f, 0.25f) : colCyanNexo;
        GUI.DrawTexture(new Rect(barRect.x, barRect.y, barRect.width * fillRatio, barRect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && barRect.Contains(Event.current.mousePosition))
        {
            if (AudioManager.Instance != null)
            {
                float clickRatio = Mathf.Clamp01((Event.current.mousePosition.x - barRect.x) / Mathf.Max(1f, barRect.width));
                AudioManager.Instance.isMuted = false;
                AudioManager.Instance.masterVolume = clickRatio;
                Event.current.Use();
            }
        }

        Rect plusBtn = ScaleRect(baseX + 258f, baseY + 34f, 32f, 24f);
        DrawCyberBox(plusBtn, new Color(0.08f, 0.13f, 0.22f), colBorder, 1f);
        DrawStyledText(plusBtn, "+", 16, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(plusBtn, GUIContent.none, GUIStyle.none) && AudioManager.Instance != null)
        {
            AudioManager.Instance.isMuted = false;
            AudioManager.Instance.masterVolume = Mathf.Clamp01(vol + 0.10f);
            AudioManager.Instance.PlayUIClick();
        }
    }

    void DrawKaelWorldHealthBar()
    {
        if (kaelTransform == null)
        {
            KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
            if (kael != null) kaelTransform = kael.transform;
        }
        if (kaelTransform == null || Camera.main == null) return;

        Vector3 headWorldPos = kaelTransform.position + new Vector3(0f, healthBarOffsetY, 0f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(headWorldPos);
        if (screenPos.z <= 0) return;

        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;
        float barW = Mathf.Round(86f * sx);
        float barH = Mathf.Round(9f * sy);
        float ultH = Mathf.Max(3f, Mathf.Round(4f * sy));
        float x = Mathf.Round(screenPos.x - (barW * 0.5f));
        float y = Mathf.Round(Screen.height - screenPos.y);

        GUI.color = new Color(0f, 0f, 0f, 0.92f);
        GUI.DrawTexture(new Rect(x - 2f, y - 2f, barW + 4f, barH + ultH + 5f), Texture2D.whiteTexture);

        GUI.color = new Color(0.12f, 0.20f, 0.12f, 1f);
        GUI.DrawTexture(new Rect(x, y, barW, barH), Texture2D.whiteTexture);

        float hpFill = Mathf.Clamp01(currentHP / Mathf.Max(1f, maxHP));
        GUI.color = (hpFill > 0.35f) ? colHpGreen : new Color(1f, 0.25f, 0.25f);
        GUI.DrawTexture(new Rect(x, y, barW * hpFill, barH), Texture2D.whiteTexture);

        float ultFill = Mathf.Clamp01(currentUltCharge / Mathf.Max(1f, maxUltCharge));
        GUI.color = new Color(0.05f, 0.15f, 0.25f, 1f);
        GUI.DrawTexture(new Rect(x, y + barH + 1f, barW, ultH), Texture2D.whiteTexture);
        GUI.color = IsUltReady ? colGoldYel : colCyanNexo;
        GUI.DrawTexture(new Rect(x, y + barH + 1f, barW * ultFill, ultH), Texture2D.whiteTexture);

        if (KaelAutoController.Instance != null && KaelAutoController.Instance.isGuarding)
        {
            Rect guardTag = new Rect(x - 30f * sx, y - 22f * sy, barW + 60f * sx, 18f * sy);
            bool inParryWin = KaelAutoController.Instance.IsWithinPerfectParryWindow();
            string guardTxt = inParryWin ? "⚡ PARRY NEXO (0.35s)" : "🛡 GUARDIA NEXO (-45%)";
            Color guardCol = inParryWin ? colGoldYel : colCyanNexo;
            DrawStyledText(guardTag, guardTxt, 12, TextAnchor.MiddleCenter, guardCol);
        }

        GUI.color = Color.white;
    }

    public void DrawCyberBox(Rect r, Color bg, Color border, float borderThickness)
    {
        float t = Mathf.Max(1f, Mathf.Round(borderThickness * Mathf.Min(Screen.width / 1920f, Screen.height / 1080f)));
        GUI.color = border;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = bg;
        GUI.DrawTexture(new Rect(r.x + t, r.y + t, r.width - t * 2f, r.height - t * 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    public void DrawProgressBar(Rect r, float ratio, Color bgCol, Color fillCol, string label, int baseFontSize)
    {
        // Marco exterior oscuro y pista interior
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f), Texture2D.whiteTexture);

        GUI.color = bgCol;
        GUI.DrawTexture(r, Texture2D.whiteTexture);

        float fillW = Mathf.Round(r.width * Mathf.Clamp01(ratio));
        if (fillW > 0f)
        {
            GUI.color = fillCol;
            GUI.DrawTexture(new Rect(r.x, r.y, fillW, r.height), Texture2D.whiteTexture);

            // Brillo especular superior tipo cristal
            GUI.color = new Color(1f, 1f, 1f, 0.26f);
            GUI.DrawTexture(new Rect(r.x, r.y, fillW, Mathf.Max(2f, r.height * 0.38f)), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
        if (!string.IsNullOrEmpty(label))
        {
            DrawStyledText(r, label, baseFontSize, TextAnchor.MiddleCenter, Color.white);
        }
    }

    public void DrawStyledText(Rect r, string txt, int baseSize, TextAnchor align, Color col, bool wordWrap = false)
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
