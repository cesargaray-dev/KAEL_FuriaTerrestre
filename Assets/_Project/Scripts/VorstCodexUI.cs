using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Identificadores oficiales de las 6 especies del Códice Vorst.
/// </summary>
public enum CodexSpeciesId
{
    VorstWalker = 0,
    Stalker = 1,
    BruteDemolisher = 2,
    PsionicArchon = 3,
    Sector7Commander = 4,
    SupremeSovereign = 5
}

/// <summary>
/// Estados de animación inspeccionables en el visor holográfico del Códice.
/// </summary>
public enum CodexAnimState
{
    Walk = 0,
    Attack = 1,
    Die = 2
}

/// <summary>
/// Ficha técnica y narrativa de cada espécimen alienígena Vorst.
/// </summary>
[Serializable]
public class CodexSpeciesEntry
{
    public CodexSpeciesId SpeciesId;
    public string CodeName;
    public string DisplayName;
    public string Classification;
    public string AlienOrigin;
    public string ThreatLevelText;
    public int ThreatRating; // 1..6
    public string ArmorType;
    public string BodyWeakPoints;
    public string ElementalWeakness;
    public string TacticalStatsSummary;
    public string DefenseDiaryTitle;
    public string DefenseDiaryFragment;
    public Color ThemeColor;
    public string IconResourcePath;
}

/// <summary>
/// Enciclopedia Táctica Interactiva (Códice Vorst) accesible desde el Menú Principal (00_MainMenu)
/// y desde el Menú de Pausa en cualquier nivel de combate.
/// Soporta desbloqueo progresivo al derrotar criaturas, conteo de bajas de sesión e histórico,
/// visor animado en tiempo real (Caminar / Ataque / Colapso) y navegación completa con teclado y mouse.
/// </summary>
public class VorstCodexUI : MonoBehaviour
{
    public static VorstCodexUI Instance { get; private set; }

    private const string PREFS_UNLOCKED_MASK = "KAEL_CODEX_UNLOCKED_MASK";
    private const string PREFS_KILL_PREFIX = "KAEL_CODEX_KILLS_";

    public static readonly CodexSpeciesEntry[] Catalog = new CodexSpeciesEntry[]
    {
        new CodexSpeciesEntry
        {
            SpeciesId = CodexSpeciesId.VorstWalker,
            CodeName = "VRST-01 // INFANTERÍA",
            DisplayName = "Caminante Vorst",
            Classification = "Unidad de Infantería Biomecánica Estándar",
            AlienOrigin = "Cámara de Incubación Orbital — Flota de Recolección Vorst (Sector 7)",
            ThreatLevelText = "NIVEL II — MODERADO (ENJAMBRE URBANO)",
            ThreatRating = 2,
            ArmorType = "Quitina Sintética Ligera (Aleación Bio-Carbono Fase 1)",
            BodyWeakPoints = "Articulación cervical expuesta y conducto torácico de plasma violeta.",
            ElementalWeakness = "Guantelete Prototipo (Eléctrico Cian) y Piro-Reactor (Combustión Térmica)",
            TacticalStatsSummary = "HP BASE: 30-160  |  DAÑO: 4-15  |  VEL: 2.2 m/s  |  BOTÍN: 6-24 ORO",
            DefenseDiaryTitle = "DIARIO DE DEFENSA TERRESTRE — ENTRADA #04 (CAPITÁN V. MORALES)",
            DefenseDiaryFragment = "\"Cayeron en cápsulas de impacto sobre las avenidas del Sector 7. No respiran ni dudan: avanzan en formación cerrada drenando la red eléctrica urbana. Un golpe sísmico directo al tórax basta para quebrar su exoesqueleto, pero jamás atacan solos.\"",
            ThemeColor = new Color(0.24f, 0.92f, 1.00f),
            IconResourcePath = "Codex/Icon_Codex_VorstWalker"
        },
        new CodexSpeciesEntry
        {
            SpeciesId = CodexSpeciesId.Stalker,
            CodeName = "VRST-02 // ACECHADOR",
            DisplayName = "Stalker",
            Classification = "Depredador de Intercepción Rápida y Flanqueo",
            AlienOrigin = "Fosas de Mutación Acelerada — Perímetro Interior de la Base Vorst",
            ThreatLevelText = "NIVEL III — ALTO (ALTA MOVILIDAD Y EMBOSCADA)",
            ThreatRating = 3,
            ArmorType = "Placas Flexibles de Silicio-Quitina con Camuflaje Refractivo",
            BodyWeakPoints = "Núcleo dorsal de propulsión y tendones hidráulicos de las extremidades traseras.",
            ElementalWeakness = "Guantelete Piro-Reactor (Fuego Continuo DoT) y Bloqueo Perfecto (Parry Nexo)",
            TacticalStatsSummary = "HP BASE: 58-260  |  DAÑO: 5-22  |  VEL: 2.85 m/s  |  BOTÍN: 10-32 ORO",
            DefenseDiaryTitle = "DIARIO DE DEFENSA TERRESTRE — ENTRADA #11 (TENIENTE S. KOWALSKI)",
            DefenseDiaryFragment = "\"Los llamamos Stalkers porque escuchas el zumbido de sus garras antes de verlos saltar entre los escombros. Mi escuadrón cayó intentando apuntarles con rifles convencionales; solo un reflejo de guardia en la fracción de segundo previa al impacto logra aturdir su sistema nervioso.\"",
            ThemeColor = new Color(0.22f, 0.98f, 0.66f),
            IconResourcePath = "Codex/Icon_Codex_Stalker"
        },
        new CodexSpeciesEntry
        {
            SpeciesId = CodexSpeciesId.BruteDemolisher,
            CodeName = "VRST-03 // COLOSO",
            DisplayName = "Demoledor Bruto",
            Classification = "Ariete Blindado de Asedio y Ruptura de Líneas",
            AlienOrigin = "Forjas Biológicas de Gravedad Aumentada — Sector Industrial Vorst",
            ThreatLevelText = "NIVEL IV — SEVERO (TANQUE PESADO DE ASEDIO)",
            ThreatRating = 4,
            ArmorType = "Coraza Compuesta de Titanio Asimilado y Placas Óseas de Alta Densidad",
            BodyWeakPoints = "Reactor térmico pectoral (expuesto al cargar su Puño Demoledor) y fisuras dorsales.",
            ElementalWeakness = "Guantelete de Plasma Ígneo (+15% Crítico Solar) para perforar blindaje pesado",
            TacticalStatsSummary = "HP BASE: 240-950  |  DAÑO: 14-30  |  VEL: 1.55 m/s  |  BOTÍN: 65-120 ORO",
            DefenseDiaryTitle = "DIARIO DE DEFENSA TERRESTRE — ENTRADA #19 (INGENIERO EN JEFE R. SALAZAR)",
            DefenseDiaryFragment = "\"Los Demoledores Brutos fueron diseñados con un único propósito: derribar nuestros búnkeres de titanio a puñetazo limpio. Cuando su núcleo rojo parpadea, el suelo tiembla. No intentes absorber su golpe cargado sin guardia Nexo o acabarás reducido a cenizas.\"",
            ThemeColor = new Color(1.00f, 0.44f, 0.18f),
            IconResourcePath = "Codex/Icon_Codex_Brute"
        },
        new CodexSpeciesEntry
        {
            SpeciesId = CodexSpeciesId.PsionicArchon,
            CodeName = "VRST-04 // SOBERANO BASE",
            DisplayName = "Arconte Psiónico",
            Classification = "Canalizador de Singularidad y Comandante de Convergencia",
            AlienOrigin = "Cámara del Orbe Púrpura — Cúspide de la Base Vorst (Nivel 2)",
            ThreatLevelText = "NIVEL V — CRÍTICO (COMANDANTE PSIÓNICO DE ZONA)",
            ThreatRating = 5,
            ArmorType = "Manto de Fase Cuántica y Escudo de Resonancia Telequinética",
            BodyWeakPoints = "Anillo de levitación craneal y los 4 orbes psiónicos durante su descarga.",
            ElementalWeakness = "Guantelete de Vacío Vorst (Sifón Biotecnológico) y Sobrecarga Ultimate Nexo",
            TacticalStatsSummary = "HP BASE: 2200  |  DAÑO: 22-44  |  ALCANCE: 2.4 m  |  BOTÍN: 250 ORO",
            DefenseDiaryTitle = "DIARIO DE DEFENSA TERRESTRE — ENTRADA #27 (ARCHIVO N.E.X.A. DESCLASIFICADO)",
            DefenseDiaryFragment = "\"El Arconte no combate con fuerza bruta: curva la gravedad local mediante cuatro esferas de plasma abisal alimentadas por prisioneros en estasis. Romper su concentración exige acortar distancia entre cada pulso psiónico y descargar toda la reserva del reactor Nexo.\"",
            ThemeColor = new Color(0.82f, 0.34f, 1.00f),
            IconResourcePath = "Codex/Icon_Codex_Archon"
        },
        new CodexSpeciesEntry
        {
            SpeciesId = CodexSpeciesId.Sector7Commander,
            CodeName = "VRST-05 // LÍDER ZONA 1",
            DisplayName = "Comandante del Sector 7",
            Classification = "Ejecutor Táctico de Vanguardia y Guardián del Puente",
            AlienOrigin = "Crucero de Asalto Orbital 'Heraldo del Vacío' — Destacamento Sector 7",
            ThreatLevelText = "NIVEL IV+ — ÉLITE (JEFE DE OCUPACIÓN URBANA)",
            ThreatRating = 5,
            ArmorType = "Exoesqueleto Imperial Carmesí con Revestimiento Deflector de Impactos",
            BodyWeakPoints = "Núcleo bio-energético del peto central tras ejecutar su embestida pesada.",
            ElementalWeakness = "Guantelete Piro-Reactor (Fuego Carmesí) y Contragolpe Sísmico (Combo Paso 3)",
            TacticalStatsSummary = "HP BASE: 450  |  DAÑO: 18-36  |  ALCANCE: 1.9 m  |  BOTÍN: 150 ORO",
            DefenseDiaryTitle = "DIARIO DE DEFENSA TERRESTRE — ENTRADA #09 (REGISTRO PERSONAL DE KAEL)",
            DefenseDiaryFragment = "\"Él dirigió la escuadra de recolección que secuestró a Lía entre las llamas del Sector 7. Su armadura carmesí resiste impactos ordinarios, pero su arrogancia al cargar ataques pesados deja al descubierto el reactor que mantiene cerrado el puente hacia la Base Vorst.\"",
            ThemeColor = new Color(1.00f, 0.26f, 0.56f),
            IconResourcePath = "Codex/Icon_Codex_Commander"
        },
        new CodexSpeciesEntry
        {
            SpeciesId = CodexSpeciesId.SupremeSovereign,
            CodeName = "VRST-06 // OMEGA-APEX",
            DisplayName = "Soberano Supremo",
            Classification = "Mecha Berserker Biomecánico VORTEX-OMEGA (Regente de la Colmena)",
            AlienOrigin = "Trono de Bio-Cables — Corazón de la Nave Núcleo Vorst (Nivel 3)",
            ThreatLevelText = "NIVEL OMEGA — EXTINCIÓN PLANETARIA (JEFE SUPREMO)",
            ThreatRating = 6,
            ArmorType = "Chasis Acorazado de Hiper-Aleación Vorst con Regeneración de Fase Dual",
            BodyWeakPoints = "Cámara torácica del Cañón Omega (al abrir el pecho) y enfriadores tras el Salto Meteoro.",
            ElementalWeakness = "Guantelete de Vacío Vorst (5% Robo de Vida) y Plasma Ígneo en Sobremarcha (Overdrive)",
            TacticalStatsSummary = "HP BASE: 3400  |  DAÑO: 28-64  |  FASE 2: <50% HP  |  BOTÍN: 450 ORO",
            DefenseDiaryTitle = "DIARIO DE DEFENSA TERRESTRE — ENTRADA FINAL #33 (TRANSMISIÓN DE N.E.X.A.)",
            DefenseDiaryFragment = "\"Designación oficial: Soberano Supremo VORTEX-OMEGA. Una aberración colosal mitad titán viviente, mitad acorazado estelar que custodia la cúpula donde Lía permanece cautiva. Al caer por debajo del 50% de integridad entra en Furia Berserker, invocando escoltas y saltos meteóricos capaces de partir la cubierta en dos.\"",
            ThemeColor = new Color(1.00f, 0.82f, 0.22f),
            IconResourcePath = "Codex/Icon_Codex_Sovereign"
        }
    };

    // Estado estático en memoria + persistencia en PlayerPrefs
    private static bool _persistenceLoaded = false;
    private static int _unlockedMask = 0;
    private static readonly int[] _totalKills = new int[6];
    private static readonly int[] _sessionKills = new int[6];

    public bool isOpen = false;
    private Action onCloseCallback;

    private int selectedIndex = 0;
    private CodexAnimState selectedAnimState = CodexAnimState.Walk;
    private bool animPaused = false;
    private bool flipSpriteHorizontal = false;
    private bool simulationPreviewAll = false;
    private float animTimer = 0f;
    private int currentFrameIndex = 0;

    private string notificationBanner = "";
    private Color notificationColor = Color.cyan;
    private float notificationTimer = 0f;

    // Texturas de interfaz y frames de animación por especie
    private Texture2D texPanelCyan;
    private Texture2D texPanelGold;
    private Texture2D texPanelCrimson;
    private Texture2D texBtnCyan;
    private Texture2D texBtnGold;
    private Texture2D texBtnCrimson;
    private Texture2D texBtnEmerald;
    private Texture2D texBtnPurple;
    private Texture2D holoGridTex;
    private Texture2D holoGlowTex;

    private readonly Texture2D[] speciesIcons = new Texture2D[6];
    private readonly Dictionary<int, Texture2D[]> walkFramesBySpecies = new Dictionary<int, Texture2D[]>();
    private readonly Dictionary<int, Texture2D[]> attackFramesBySpecies = new Dictionary<int, Texture2D[]>();
    private readonly Dictionary<int, Texture2D[]> dieFramesBySpecies = new Dictionary<int, Texture2D[]>();
    private bool assetsLoaded = false;

    // Cooldown para evitar doble rebote de teclas en la misma pulsación
    private float lastNavInputTime = -10f;

    public static VorstCodexUI EnsureExists()
    {
        EnsurePersistenceLoaded();
        if (Instance != null) return Instance;

        VorstCodexUI existing = FindAnyObjectByType<VorstCodexUI>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject host = new GameObject("VorstCodexUI");
        DontDestroyOnLoad(host);
        Instance = host.AddComponent<VorstCodexUI>();
        return Instance;
    }

    public static void EnsurePersistenceLoaded()
    {
        if (_persistenceLoaded) return;
        _persistenceLoaded = true;

        _unlockedMask = PlayerPrefs.GetInt(PREFS_UNLOCKED_MASK, 0);
        for (int i = 0; i < Catalog.Length; i++)
        {
            _totalKills[i] = Mathf.Max(0, PlayerPrefs.GetInt(PREFS_KILL_PREFIX + i, 0));
            if (_totalKills[i] > 0)
            {
                _unlockedMask |= (1 << i);
            }
        }
    }

    /// <summary>
    /// Determina a cuál de las 6 entradas del Códice Vorst corresponde un EnemyDummy derrotado.
    /// </summary>
    public static CodexSpeciesId ClassifyEnemy(EnemyDummy enemy)
    {
        if (enemy == null) return CodexSpeciesId.VorstWalker;

        string title = (enemy.enemyTitle ?? "").ToLowerInvariant();

        if (enemy.isBoss)
        {
            if (title.Contains("vortex") || title.Contains("omega") || title.Contains("soberano supremo") || enemy.isLevel3Monster)
                return CodexSpeciesId.SupremeSovereign;
            if (title.Contains("arconte"))
                return CodexSpeciesId.PsionicArchon;
            if (title.Contains("comandante") || title.Contains("sector 7"))
                return CodexSpeciesId.Sector7Commander;
        }

        if (enemy.isMiniBoss)
        {
            if (title.Contains("inquisidor") || title.Contains("arconte"))
                return CodexSpeciesId.PsionicArchon;
            return CodexSpeciesId.BruteDemolisher;
        }

        if (title.Contains("soberano") || title.Contains("vortex") || title.Contains("omega"))
            return CodexSpeciesId.SupremeSovereign;
        if (title.Contains("comandante"))
            return CodexSpeciesId.Sector7Commander;
        if (title.Contains("arconte") || title.Contains("inquisidor"))
            return CodexSpeciesId.PsionicArchon;
        if (title.Contains("bruto") || title.Contains("coloso") || title.Contains("demoledor") || title.Contains("hulk"))
            return CodexSpeciesId.BruteDemolisher;
        if (title.Contains("stalker") || title.Contains("acechador") || title.Contains("lasher") || title.Contains("quimera"))
            return CodexSpeciesId.Stalker;

        return CodexSpeciesId.VorstWalker;
    }

    /// <summary>
    /// Registra la baja de un enemigo en combate, desbloquea progresivamente su entrada en el Códice
    /// y persiste el récord de bajas.
    /// </summary>
    public static void RegisterEnemyKill(EnemyDummy enemy)
    {
        EnsurePersistenceLoaded();
        CodexSpeciesId species = ClassifyEnemy(enemy);
        RegisterSpeciesKill(species, enemy != null ? enemy.transform.position : Vector3.zero, showVfxToast: true);
    }

    public static void RegisterSpeciesKill(CodexSpeciesId species, Vector3 worldPos = default, bool showVfxToast = false)
    {
        EnsurePersistenceLoaded();
        int idx = Mathf.Clamp((int)species, 0, Catalog.Length - 1);
        bool wasUnlocked = IsSpeciesUnlocked(species);

        _sessionKills[idx]++;
        _totalKills[idx]++;
        _unlockedMask |= (1 << idx);

        PlayerPrefs.SetInt(PREFS_UNLOCKED_MASK, _unlockedMask);
        PlayerPrefs.SetInt(PREFS_KILL_PREFIX + idx, _totalKills[idx]);
        PlayerPrefs.Save();

        if (!wasUnlocked && showVfxToast && CombatVFXManager.Instance != null)
        {
            CodexSpeciesEntry entry = Catalog[idx];
            CombatVFXManager.Instance.SpawnFloatingText(
                worldPos + Vector3.up * 2.25f,
                $"📘 CÓDICE DESBLOQUEADO: {entry.DisplayName.ToUpper()}",
                entry.ThemeColor,
                18,
                1.55f
            );
        }
    }

    public static bool IsSpeciesUnlocked(CodexSpeciesId species)
    {
        EnsurePersistenceLoaded();
        int bit = 1 << (int)species;
        return (_unlockedMask & bit) != 0 || _totalKills[(int)species] > 0;
    }

    public static int GetTotalKills(CodexSpeciesId species)
    {
        EnsurePersistenceLoaded();
        return _totalKills[Mathf.Clamp((int)species, 0, Catalog.Length - 1)];
    }

    public static int GetSessionKills(CodexSpeciesId species)
    {
        EnsurePersistenceLoaded();
        return _sessionKills[Mathf.Clamp((int)species, 0, Catalog.Length - 1)];
    }

    public static int GetUnlockedSpeciesCount()
    {
        EnsurePersistenceLoaded();
        int count = 0;
        for (int i = 0; i < Catalog.Length; i++)
        {
            if (IsSpeciesUnlocked((CodexSpeciesId)i)) count++;
        }
        return count;
    }

    public static int GetAllSpeciesTotalKills()
    {
        EnsurePersistenceLoaded();
        int sum = 0;
        for (int i = 0; i < Catalog.Length; i++) sum += _totalKills[i];
        return sum;
    }

    public void OpenCodex(Action onClose = null)
    {
        EnsurePersistenceLoaded();
        EnsureAssetsLoaded();

        onCloseCallback = onClose;
        isOpen = true;
        animTimer = 0f;
        currentFrameIndex = 0;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
    }

    public void CloseCodex()
    {
        if (!isOpen) return;
        isOpen = false;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        onCloseCallback?.Invoke();
        onCloseCallback = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        if (GetComponent<Camera>() == null && transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        EnsurePersistenceLoaded();
        EnsureAssetsLoaded();
    }

    void OnDestroy()
    {
        if (holoGridTex != null) Destroy(holoGridTex);
        if (holoGlowTex != null) Destroy(holoGlowTex);
        if (Instance == this) Instance = null;
    }

    void EnsureAssetsLoaded()
    {
        if (assetsLoaded) return;
        assetsLoaded = true;

        texPanelCyan = LoadResTex("StoryUI/Panel_Cyber_Cyan");
        texPanelGold = LoadResTex("StoryUI/Panel_Cyber_Gold");
        texPanelCrimson = LoadResTex("StoryUI/Panel_Cyber_Crimson");
        texBtnCyan = LoadResTex("StoryUI/Btn_Cyber_Cyan");
        texBtnGold = LoadResTex("StoryUI/Btn_Cyber_Gold");
        texBtnCrimson = LoadResTex("StoryUI/Btn_Cyber_Crimson");
        texBtnEmerald = LoadResTex("StoryUI/Btn_Cyber_Emerald");
        texBtnPurple = LoadResTex("StoryUI/Btn_Cyber_Purple");

        for (int i = 0; i < Catalog.Length; i++)
        {
            speciesIcons[i] = LoadResTex(Catalog[i].IconResourcePath);
        }

        // 0: Caminante Vorst (Resources/Codex/VorstWalker_*)
        walkFramesBySpecies[(int)CodexSpeciesId.VorstWalker] = LoadFrameSequence("Codex/VorstWalker_Walk_", 4);
        attackFramesBySpecies[(int)CodexSpeciesId.VorstWalker] = LoadFrameSequence("Codex/VorstWalker_Attack_", 3);
        dieFramesBySpecies[(int)CodexSpeciesId.VorstWalker] = LoadFrameSequence("Codex/VorstWalker_Die_", 4);

        // 1: Stalker (Resources/Level2/Stalker_*)
        walkFramesBySpecies[(int)CodexSpeciesId.Stalker] = LoadFrameSequence("Level2/Stalker_Walk_", 8);
        attackFramesBySpecies[(int)CodexSpeciesId.Stalker] = LoadFrameSequence("Level2/Stalker_Attack_", 8);
        dieFramesBySpecies[(int)CodexSpeciesId.Stalker] = LoadFrameSequence("Level2/Stalker_Die_", 8);

        // 2: Demoledor Bruto (Resources/Level2/Brute_*)
        walkFramesBySpecies[(int)CodexSpeciesId.BruteDemolisher] = LoadFrameSequence("Level2/Brute_Walk_", 8);
        attackFramesBySpecies[(int)CodexSpeciesId.BruteDemolisher] = LoadFrameSequence("Level2/Brute_Attack_", 8);
        dieFramesBySpecies[(int)CodexSpeciesId.BruteDemolisher] = LoadFrameSequence("Level2/Brute_Die_", 8);

        // 3: Arconte Psiónico (Resources/Level2/Archon_*)
        walkFramesBySpecies[(int)CodexSpeciesId.PsionicArchon] = LoadFrameSequence("Level2/Archon_Walk_", 8);
        attackFramesBySpecies[(int)CodexSpeciesId.PsionicArchon] = LoadFrameSequence("Level2/Archon_Attack_", 8);
        dieFramesBySpecies[(int)CodexSpeciesId.PsionicArchon] = LoadFrameSequence("Level2/Archon_Die_", 8);

        // 4: Comandante del Sector 7 (Resources/Codex/Commander_*)
        walkFramesBySpecies[(int)CodexSpeciesId.Sector7Commander] = LoadFrameSequence("Codex/Commander_Walk_", 4);
        attackFramesBySpecies[(int)CodexSpeciesId.Sector7Commander] = LoadFrameSequence("Codex/Commander_Attack_", 3);
        dieFramesBySpecies[(int)CodexSpeciesId.Sector7Commander] = LoadFrameSequence("Codex/Commander_Die_", 4);

        // 5: Soberano Supremo VORTEX-OMEGA (Resources/Level3/VortexOmega_*)
        walkFramesBySpecies[(int)CodexSpeciesId.SupremeSovereign] = LoadFrameSequence("Level3/VortexOmega_Walk_", 8);
        attackFramesBySpecies[(int)CodexSpeciesId.SupremeSovereign] = LoadFrameSequence("Level3/VortexOmega_Attack_", 8);
        dieFramesBySpecies[(int)CodexSpeciesId.SupremeSovereign] = LoadFrameSequence("Level3/VortexOmega_Die_", 8);

        BuildProceduralTextures();
    }

    Texture2D[] LoadFrameSequence(string resourcePrefix, int maxFrames)
    {
        List<Texture2D> list = new List<Texture2D>(maxFrames);
        for (int i = 0; i < maxFrames; i++)
        {
            Texture2D tex = LoadResTex(resourcePrefix + i);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                list.Add(tex);
            }
        }
        return list.ToArray();
    }

    static Texture2D LoadResTex(string path)
    {
        Texture2D t = Resources.Load<Texture2D>(path);
        if (t != null) return t;
        Sprite sp = Resources.Load<Sprite>(path);
        return (sp != null) ? sp.texture : null;
    }

    void BuildProceduralTextures()
    {
        if (holoGridTex == null)
        {
            const int size = 128;
            holoGridTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool gridLine = (x % 16 == 0) || (y % 16 == 0);
                    bool subLine = (x % 8 == 0) || (y % 8 == 0);
                    float a = gridLine ? 0.16f : (subLine ? 0.06f : 0f);
                    holoGridTex.SetPixel(x, y, new Color(0.22f, 0.90f, 1f, a));
                }
            }
            holoGridTex.Apply();
        }

        if (holoGlowTex == null)
        {
            const int size = 128;
            holoGlowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    holoGlowTex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * 0.75f));
                }
            }
            holoGlowTex.Apply();
        }
    }

    void Update()
    {
        if (!isOpen) return;

        float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);

        if (notificationTimer > 0f)
        {
            notificationTimer = Mathf.Max(0f, notificationTimer - dt);
        }

        Texture2D[] activeFrames = GetActiveAnimationFrames();
        int frameCount = (activeFrames != null && activeFrames.Length > 0) ? activeFrames.Length : 1;

        if (!animPaused)
        {
            float fps = selectedAnimState switch
            {
                CodexAnimState.Attack => 9.5f,
                CodexAnimState.Die => 6.5f,
                _ => 8.5f
            };
            animTimer += dt * fps;
            currentFrameIndex = Mathf.FloorToInt(animTimer) % frameCount;
        }
        else
        {
            currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, frameCount - 1);
        }

        HandleKeyboardNavigation();
    }

    void HandleKeyboardNavigation()
    {
        if (Time.unscaledTime - lastNavInputTime < 0.11f) return;

        // Cerrar con Escape
        if (WasKeyPressed(KeyCode.Escape, k => k.escapeKey.wasPressedThisFrame))
        {
            lastNavInputTime = Time.unscaledTime;
            CloseCodex();
            return;
        }

        // Navegación vertical de especies: W / S o Flecha Arriba / Abajo
        if (WasKeyPressed(KeyCode.UpArrow, k => k.upArrowKey.wasPressedThisFrame) ||
            WasKeyPressed(KeyCode.W, k => k.wKey.wasPressedThisFrame))
        {
            lastNavInputTime = Time.unscaledTime;
            SelectSpecies((selectedIndex - 1 + Catalog.Length) % Catalog.Length);
            return;
        }

        if (WasKeyPressed(KeyCode.DownArrow, k => k.downArrowKey.wasPressedThisFrame) ||
            WasKeyPressed(KeyCode.S, k => k.sKey.wasPressedThisFrame))
        {
            lastNavInputTime = Time.unscaledTime;
            SelectSpecies((selectedIndex + 1) % Catalog.Length);
            return;
        }

        // Navegación horizontal de estado de animación: A / D o Flecha Izquierda / Derecha
        if (WasKeyPressed(KeyCode.LeftArrow, k => k.leftArrowKey.wasPressedThisFrame) ||
            WasKeyPressed(KeyCode.A, k => k.aKey.wasPressedThisFrame))
        {
            lastNavInputTime = Time.unscaledTime;
            int nextState = ((int)selectedAnimState + 2) % 3;
            SetAnimState((CodexAnimState)nextState);
            return;
        }

        if (WasKeyPressed(KeyCode.RightArrow, k => k.rightArrowKey.wasPressedThisFrame) ||
            WasKeyPressed(KeyCode.D, k => k.dKey.wasPressedThisFrame))
        {
            lastNavInputTime = Time.unscaledTime;
            int nextState = ((int)selectedAnimState + 1) % 3;
            SetAnimState((CodexAnimState)nextState);
            return;
        }

        // Atajos directos 1 / 2 / 3 para las animaciones
        if (WasKeyPressed(KeyCode.Alpha1, k => k.digit1Key.wasPressedThisFrame))
        {
            lastNavInputTime = Time.unscaledTime;
            SetAnimState(CodexAnimState.Walk);
            return;
        }
        if (WasKeyPressed(KeyCode.Alpha2, k => k.digit2Key.wasPressedThisFrame))
        {
            lastNavInputTime = Time.unscaledTime;
            SetAnimState(CodexAnimState.Attack);
            return;
        }
        if (WasKeyPressed(KeyCode.Alpha3, k => k.digit3Key.wasPressedThisFrame))
        {
            lastNavInputTime = Time.unscaledTime;
            SetAnimState(CodexAnimState.Die);
            return;
        }

        // Espacio: Pausar / Reanudar animación
        if (WasKeyPressed(KeyCode.Space, k => k.spaceKey.wasPressedThisFrame))
        {
            lastNavInputTime = Time.unscaledTime;
            animPaused = !animPaused;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            return;
        }

        // F: Voltear orientación horizontal del modelo
        if (WasKeyPressed(KeyCode.F, k => k.fKey.wasPressedThisFrame))
        {
            lastNavInputTime = Time.unscaledTime;
            flipSpriteHorizontal = !flipSpriteHorizontal;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            return;
        }

        // Tab / U: Alternar modo Simulación Desclasificada N.E.X.A.
        if (WasKeyPressed(KeyCode.Tab, k => k.tabKey.wasPressedThisFrame) ||
            WasKeyPressed(KeyCode.U, k => k.uKey.wasPressedThisFrame))
        {
            lastNavInputTime = Time.unscaledTime;
            ToggleSimulationPreviewMode();
        }
    }

    static bool WasKeyPressed(KeyCode legacyKey, Func<Keyboard, bool> inputSystemPredicate)
    {
        if (Keyboard.current != null && inputSystemPredicate != null)
        {
            if (inputSystemPredicate(Keyboard.current)) return true;
        }
        try { return Input.GetKeyDown(legacyKey); }
        catch { return false; }
    }

    void SelectSpecies(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, Catalog.Length - 1);
        animTimer = 0f;
        currentFrameIndex = 0;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
    }

    void SetAnimState(CodexAnimState state)
    {
        selectedAnimState = state;
        animPaused = false;
        animTimer = 0f;
        currentFrameIndex = 0;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
    }

    void ToggleSimulationPreviewMode()
    {
        simulationPreviewAll = !simulationPreviewAll;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        ShowNotification(
            simulationPreviewAll
                ? "🔓 SIMULACIÓN N.E.X.A. ACTIVA: ARCHIVOS Y ANIMACIONES PREVISUALIZABLES"
                : "🔒 MODO PROGRESIVO ESTRICTO: SOLO ESPECÍMENES DERROTADOS EN COMBATE",
            simulationPreviewAll ? new Color(0.25f, 0.96f, 1f) : new Color(1f, 0.82f, 0.24f)
        );
    }

    void ShowNotification(string msg, Color col)
    {
        notificationBanner = msg;
        notificationColor = col;
        notificationTimer = 3.2f;
    }

    Texture2D[] GetActiveAnimationFrames()
    {
        Dictionary<int, Texture2D[]> dict = selectedAnimState switch
        {
            CodexAnimState.Attack => attackFramesBySpecies,
            CodexAnimState.Die => dieFramesBySpecies,
            _ => walkFramesBySpecies
        };

        if (dict.TryGetValue(selectedIndex, out Texture2D[] frames) && frames != null && frames.Length > 0)
        {
            return frames;
        }
        return null;
    }

    void OnGUI()
    {
        if (!isOpen) return;
        EnsureAssetsLoaded();

        GUI.matrix = Matrix4x4.identity;
        GUI.depth = -50;

        DrawCodexModal();
    }

    public void DrawCodexModal()
    {
        // 1. Fondo oscurecido estilo centro de mando militar N.E.X.A.
        GUI.color = new Color(0.01f, 0.02f, 0.06f, 0.95f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        float scanY = Mathf.PingPong(Time.unscaledTime * 150f, 1080f);
        GUI.color = new Color(0.18f, 0.88f, 1f, 0.07f);
        GUI.DrawTexture(ScaleRect(0f, scanY, 1920f, 14f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 2. Marco Principal del Códice Vorst (1840x1020)
        Rect mainRect = ScaleRect(40f, 30f, 1840f, 1020f);
        DrawSkinnedPanel(mainRect, texPanelCyan, new Color(0.22f, 0.90f, 1f));

        // 3. Cabecera de Base de Datos Militar
        DrawHeaderBar();

        // 4. Columna Izquierda: Selector Lateral de las 6 Especies Vorst (460px ancho)
        DrawLeftSpeciesList();

        // 5. Columna Central: Visor Holográfico de Animaciones en Vivo (540px ancho)
        DrawCenterAnimationViewport();

        // 6. Columna Derecha: Ficha Técnica Detallada, Debilidades, Bajas y Diario Terrestre (752px ancho)
        DrawRightTechnicalDossier();

        // 7. Pie de Página: Guía de Controles Híbridos Teclado + Mouse
        DrawFooterControlsBar();
    }

    void DrawHeaderBar()
    {
        DrawStyledText(ScaleRect(76f, 48f, 920f, 36f),
            "📘 BASE DE DATOS MILITAR TERRESTRE // CÓDICE XENOBIOLÓGICO VORST",
            25, TextAnchor.MiddleLeft, new Color(0.22f, 0.94f, 1f));

        int unlockedCount = GetUnlockedSpeciesCount();
        int totalKillsAll = GetAllSpeciesTotalKills();

        DrawStyledText(ScaleRect(76f, 84f, 920f, 24f),
            $"ARCHIVO DE INTELIGENCIA N.E.X.A. v2.4   •   ESPECÍMENES REGISTRADOS: <color=#2ee678>{unlockedCount} / {Catalog.Length}</color>   •   BAJAS TOTALES CONFIRMADAS: <color=#ffd35c>{totalKillsAll}</color>",
            14, TextAnchor.MiddleLeft, new Color(0.82f, 0.92f, 1f));

        // Barra de progreso de desbloqueo global del Códice
        Rect progRect = ScaleRect(1000f, 54f, 250f, 52f);
        DrawCyberBox(progRect, new Color(0.03f, 0.06f, 0.14f, 0.95f), new Color(0.22f, 0.88f, 1f, 0.8f), 1.5f);
        DrawStyledText(ScaleRect(1012f, 58f, 226f, 20f), $"PROGRESO DEL CÓDICE: {Mathf.RoundToInt((unlockedCount / (float)Catalog.Length) * 100f)}%", 12, TextAnchor.MiddleCenter, new Color(0.22f, 0.94f, 1f));
        DrawProgressBar(ScaleRect(1014f, 82f, 222f, 14f), unlockedCount / (float)Catalog.Length, new Color(0.08f, 0.12f, 0.22f), new Color(0.18f, 0.94f, 0.54f));

        // Botón para alternar Previsualización Táctica N.E.X.A. (Permite inspeccionar modelos bloqueados en evaluación)
        if (DrawCyberButton(
            new Rect(1264f, 48f, 326f, 58f),
            simulationPreviewAll ? texBtnEmerald : texBtnGold,
            simulationPreviewAll ? "🔓 SIMULACIÓN N.E.X.A.: ACTIVA" : "🔒 MODO PROGRESIVO [TAB]",
            simulationPreviewAll ? "MOSTRANDO TODAS LAS FICHAS" : "CLIC O [TAB] PARA PREVISUALIZAR TODOS",
            14, 10, Color.white, simulationPreviewAll ? new Color(0.80f, 1f, 0.90f) : new Color(1f, 0.86f, 0.32f),
            new Color(1f, 0.82f, 0.22f)))
        {
            ToggleSimulationPreviewMode();
        }

        // Botón Cerrar Códice [ESC]
        if (DrawCyberButton(
            new Rect(1604f, 48f, 240f, 58f),
            texBtnCrimson,
            "✖ CERRAR CÓDICE [ESC]",
            "VOLVER AL MENÚ ANTERIOR",
            15, 10, Color.white, new Color(1f, 0.82f, 0.84f), new Color(1f, 0.32f, 0.42f)))
        {
            CloseCodex();
        }
    }

    void DrawLeftSpeciesList()
    {
        Rect listPanel = ScaleRect(68f, 122f, 456f, 846f);
        DrawCyberBox(listPanel, new Color(0.02f, 0.04f, 0.09f, 0.94f), new Color(0.20f, 0.78f, 0.96f, 0.85f), 2f);

        DrawStyledText(ScaleRect(84f, 134f, 424f, 28f),
            "❖ SELECTOR DE ESPECÍMENES [▲/▼]",
            16, TextAnchor.MiddleLeft, new Color(0.22f, 0.92f, 1f));
        DrawStyledText(ScaleRect(84f, 158f, 424f, 20f),
            "Selecciona una criatura para analizar su anatomía y récords:",
            12, TextAnchor.MiddleLeft, new Color(0.70f, 0.84f, 0.96f));

        float startY = 188f;
        float rowH = 122f;
        float gapY = 8f;

        for (int i = 0; i < Catalog.Length; i++)
        {
            CodexSpeciesEntry entry = Catalog[i];
            bool selected = (i == selectedIndex);
            bool unlocked = IsSpeciesUnlocked(entry.SpeciesId);
            bool inspectable = unlocked || simulationPreviewAll;
            int kills = GetTotalKills(entry.SpeciesId);

            float y = startY + i * (rowH + gapY);
            Rect cardBase = new Rect(84f, y, 424f, rowH);
            Rect cardScreen = ScaleRect(cardBase.x, cardBase.y, cardBase.width, cardBase.height);
            bool hovered = cardScreen.Contains(Event.current.mousePosition);

            Color borderCol = selected
                ? entry.ThemeColor
                : (hovered ? Color.white : (unlocked ? new Color(entry.ThemeColor.r, entry.ThemeColor.g, entry.ThemeColor.b, 0.55f) : new Color(0.25f, 0.32f, 0.44f, 0.65f)));
            Color bgCol = selected
                ? new Color(0.08f, 0.16f, 0.28f, 0.98f)
                : (hovered ? new Color(0.06f, 0.12f, 0.22f, 0.95f) : new Color(0.03f, 0.05f, 0.11f, 0.92f));

            DrawCyberBox(cardScreen, bgCol, borderCol, selected ? 2.5f : 1.5f);

            // Barra lateral luminosa de selección
            if (selected)
            {
                GUI.color = entry.ThemeColor;
                GUI.DrawTexture(ScaleRect(cardBase.x, cardBase.y, 7f, cardBase.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // Recuadro de miniatura del espécimen
            Rect iconBox = ScaleRect(cardBase.x + 14f, cardBase.y + 14f, 92f, 94f);
            DrawCyberBox(iconBox, new Color(0.01f, 0.02f, 0.05f, 0.98f), borderCol, 1.5f);

            Texture2D iconTex = speciesIcons[i];
            if (iconTex != null)
            {
                Rect innerIcon = ScaleRect(cardBase.x + 20f, cardBase.y + 20f, 80f, 82f);
                GUI.color = inspectable ? Color.white : new Color(0.06f, 0.18f, 0.28f, 0.92f);
                GUI.DrawTexture(innerIcon, iconTex, ScaleMode.ScaleToFit);
                GUI.color = Color.white;
            }

            // Textos de la tarjeta lateral
            DrawStyledText(ScaleRect(cardBase.x + 118f, cardBase.y + 12f, 292f, 20f),
                entry.CodeName, 12, TextAnchor.MiddleLeft, entry.ThemeColor);

            string titleDisplay = inspectable ? entry.DisplayName.ToUpper() : $"[CLASIFICADO] {entry.DisplayName.ToUpper()}";
            DrawStyledText(ScaleRect(cardBase.x + 118f, cardBase.y + 32f, 292f, 28f),
                titleDisplay, 17, TextAnchor.MiddleLeft, selected ? Color.white : new Color(0.90f, 0.95f, 1f));

            DrawStyledText(ScaleRect(cardBase.x + 118f, cardBase.y + 60f, 292f, 20f),
                entry.Classification, 11, TextAnchor.MiddleLeft, new Color(0.72f, 0.84f, 0.96f));

            // Insignia inferior: Estado de Desbloqueo + Conteo de Bajas
            Rect statusPill = ScaleRect(cardBase.x + 118f, cardBase.y + 84f, 292f, 26f);
            Color statusCol = unlocked
                ? new Color(0.18f, 0.95f, 0.56f)
                : (simulationPreviewAll ? new Color(0.24f, 0.92f, 1f) : new Color(1f, 0.56f, 0.24f));
            DrawCyberBox(statusPill, new Color(0.02f, 0.05f, 0.10f, 0.92f), statusCol, 1f);

            string statusTxt = unlocked
                ? $"✔ DESBLOQUEADO  •  ☠ {kills} BAJA(S)"
                : (simulationPreviewAll ? $"🔓 SIMULACIÓN N.E.X.A.  •  0 BAJAS" : "🔒 DERROTA EN COMBATE PARA DESBLOQUEAR");
            DrawStyledText(statusPill, statusTxt, 11, TextAnchor.MiddleCenter, statusCol);

            if (GUI.Button(cardScreen, GUIContent.none, GUIStyle.none))
            {
                SelectSpecies(i);
            }
        }
    }

    void DrawCenterAnimationViewport()
    {
        CodexSpeciesEntry entry = Catalog[selectedIndex];
        bool unlocked = IsSpeciesUnlocked(entry.SpeciesId);
        bool inspectable = unlocked || simulationPreviewAll;

        Rect viewPanel = ScaleRect(538f, 122f, 540f, 846f);
        DrawCyberBox(viewPanel, new Color(0.02f, 0.04f, 0.09f, 0.95f), entry.ThemeColor, 2f);

        DrawStyledText(ScaleRect(556f, 134f, 504f, 28f),
            "🔬 VISOR BIOMÉTRICO EN TIEMPO REAL",
            17, TextAnchor.MiddleCenter, entry.ThemeColor);

        string subHeader = unlocked
            ? "TELEMETRÍA DE ESPECIMEN VERIFICADA EN CAMPO"
            : (simulationPreviewAll ? "RECONSTRUCCIÓN HOLOGRÁFICA N.E.X.A. (MODO EVALUACIÓN)" : "SEÑAL ENCRIPTADA — ABATE AL OBJETIVO EN COMBATE");
        DrawStyledText(ScaleRect(556f, 160f, 504f, 22f),
            subHeader, 12, TextAnchor.MiddleCenter, unlocked ? new Color(0.22f, 0.95f, 0.58f) : new Color(1f, 0.78f, 0.28f));

        // Cámara / Ventana del Holograma (496x454)
        Rect stageRect = ScaleRect(560f, 190f, 496f, 454f);
        DrawCyberBox(stageRect, new Color(0.01f, 0.02f, 0.05f, 0.98f), new Color(entry.ThemeColor.r, entry.ThemeColor.g, entry.ThemeColor.b, 0.75f), 2f);

        if (holoGridTex != null)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.65f);
            GUI.DrawTextureWithTexCoords(stageRect, holoGridTex, new Rect(0, 0, 4f, 4f));
            GUI.color = Color.white;
        }

        // Resplandor radial de fondo con el color temático de la especie
        if (holoGlowTex != null)
        {
            float pulse = 0.85f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.12f;
            Rect glowRect = ScaleRect(618f, 236f, 380f * pulse, 360f * pulse);
            GUI.color = new Color(entry.ThemeColor.r, entry.ThemeColor.g, entry.ThemeColor.b, inspectable ? 0.28f : 0.14f);
            GUI.DrawTexture(glowRect, holoGlowTex, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }

        // Plataforma de proyección inferior
        Rect pedestalRect = ScaleRect(628f, 578f, 360f, 18f);
        GUI.color = new Color(entry.ThemeColor.r, entry.ThemeColor.g, entry.ThemeColor.b, 0.55f);
        GUI.DrawTexture(pedestalRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Renderizado del Frame Activo de la Animación del Enemigo
        Texture2D[] frames = GetActiveAnimationFrames();
        int totalFrames = (frames != null && frames.Length > 0) ? frames.Length : 1;
        int safeFrameIdx = Mathf.Clamp(currentFrameIndex, 0, totalFrames - 1);
        Texture2D activeTex = (frames != null && frames.Length > 0) ? frames[safeFrameIdx] : speciesIcons[selectedIndex];

        if (activeTex != null)
        {
            float bobY = (selectedAnimState == CodexAnimState.Walk && !animPaused)
                ? Mathf.Sin(Time.unscaledTime * 8f) * 4f
                : 0f;

            Rect spriteBox = ScaleRect(608f, 228f + bobY, 400f, 352f);

            // Si está bloqueado y no está en simulación, mostrar silueta cian oscura encriptada
            GUI.color = inspectable ? Color.white : new Color(0.05f, 0.22f, 0.34f, 0.94f);

            if (flipSpriteHorizontal)
            {
                GUI.DrawTextureWithTexCoords(spriteBox, activeTex, new Rect(1f, 0f, -1f, 1f), true);
            }
            else
            {
                GUI.DrawTexture(spriteBox, activeTex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        // Línea de escaneo láser vertical sobre el modelo
        float laserY = 194f + Mathf.PingPong(Time.unscaledTime * 165f, 444f);
        GUI.color = new Color(entry.ThemeColor.r, entry.ThemeColor.g, entry.ThemeColor.b, 0.38f);
        GUI.DrawTexture(ScaleRect(564f, laserY, 488f, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Etiquetas de telemetría en las esquinas del visor
        string stateLabel = selectedAnimState switch
        {
            CodexAnimState.Attack => "SECUENCIA: ATAQUE OFENSIVO",
            CodexAnimState.Die => "SECUENCIA: COLAPSO ESTRUCTURAL",
            _ => "SECUENCIA: LOCOMOCIÓN / PATRULLA"
        };
        DrawStyledText(ScaleRect(574f, 200f, 300f, 22f), stateLabel, 12, TextAnchor.MiddleLeft, entry.ThemeColor);
        DrawStyledText(ScaleRect(850f, 200f, 194f, 22f),
            $"CUADRO {safeFrameIdx + 1:00} / {totalFrames:00}",
            12, TextAnchor.MiddleRight, new Color(0.84f, 0.94f, 1f));

        // Si el espécimen aún no ha sido derrotado y la simulación está apagada, mostrar aviso + botón directo
        if (!inspectable)
        {
            Rect lockOverlay = ScaleRect(584f, 360f, 448f, 126f);
            DrawCyberBox(lockOverlay, new Color(0.02f, 0.04f, 0.10f, 0.95f), new Color(1f, 0.68f, 0.22f), 2f);
            DrawStyledText(ScaleRect(596f, 370f, 424f, 44f),
                "🔒 ESPÉCIMEN NO ABATIDO EN COMBATE\nDerrota a esta criatura en campaña o activa la Simulación N.E.X.A.",
                13, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.35f), true);

            if (DrawCyberButton(
                new Rect(638f, 426f, 340f, 46f),
                texBtnEmerald,
                "🔓 PREVISUALIZAR CON SIMULACIÓN N.E.X.A.",
                "", 13, 10, Color.white, Color.white, new Color(0.22f, 0.94f, 0.62f)))
            {
                ToggleSimulationPreviewMode();
            }
        }

        // Controles del Reproductor de Animaciones (Debajo del Visor)
        DrawStyledText(ScaleRect(560f, 654f, 496f, 24f),
            "🎬 CONTROL DE SECUENCIA DE ANIMACIÓN [◀ A/D ▶  |  1-2-3]",
            13, TextAnchor.MiddleCenter, new Color(0.80f, 0.92f, 1f));

        if (DrawCyberButton(
            new Rect(560f, 684f, 158f, 54f),
            selectedAnimState == CodexAnimState.Walk ? texBtnEmerald : texBtnCyan,
            "1. CAMINAR", "LOCOMOCIÓN", 14, 10,
            Color.white, new Color(0.82f, 0.96f, 1f), entry.ThemeColor))
        {
            SetAnimState(CodexAnimState.Walk);
        }

        if (DrawCyberButton(
            new Rect(729f, 684f, 158f, 54f),
            selectedAnimState == CodexAnimState.Attack ? texBtnCrimson : texBtnCyan,
            "2. ATAQUE", "PATRÓN OFENSIVO", 14, 10,
            Color.white, new Color(1f, 0.84f, 0.84f), entry.ThemeColor))
        {
            SetAnimState(CodexAnimState.Attack);
        }

        if (DrawCyberButton(
            new Rect(898f, 684f, 158f, 54f),
            selectedAnimState == CodexAnimState.Die ? texBtnGold : texBtnCyan,
            "3. COLAPSO", "NEUTRALIZACIÓN", 14, 10,
            Color.white, new Color(1f, 0.88f, 0.45f), entry.ThemeColor))
        {
            SetAnimState(CodexAnimState.Die);
        }

        // Controles secundarios: Pausar/Reanudar, Paso de Cuadro y Espejar
        if (DrawCyberButton(
            new Rect(560f, 748f, 158f, 46f),
            animPaused ? texBtnGold : texBtnPurple,
            animPaused ? "▶ REANUDAR [ESP]" : "❚❚ PAUSAR [ESP]",
            "", 13, 10, Color.white, Color.white, entry.ThemeColor))
        {
            animPaused = !animPaused;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        }

        if (DrawCyberButton(
            new Rect(729f, 748f, 158f, 46f),
            texBtnCyan,
            "⏭ SIG. CUADRO",
            "", 13, 10, Color.white, Color.white, entry.ThemeColor))
        {
            animPaused = true;
            currentFrameIndex = (currentFrameIndex + 1) % Mathf.Max(1, totalFrames);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        }

        if (DrawCyberButton(
            new Rect(898f, 748f, 158f, 46f),
            texBtnCyan,
            flipSpriteHorizontal ? "⇄ VISTA: IZQ [F]" : "⇄ VISTA: DER [F]",
            "", 13, 10, Color.white, Color.white, entry.ThemeColor))
        {
            flipSpriteHorizontal = !flipSpriteHorizontal;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        }

        // Medidor de Nivel de Amenaza Militar en la parte inferior de la columna central
        Rect threatBox = ScaleRect(560f, 808f, 496f, 142f);
        DrawCyberBox(threatBox, new Color(0.03f, 0.05f, 0.12f, 0.96f), entry.ThemeColor, 1.5f);
        DrawStyledText(ScaleRect(576f, 818f, 464f, 24f),
            $"⚠ CLASIFICACIÓN DE AMENAZA: {entry.ThreatLevelText}",
            13, TextAnchor.MiddleLeft, entry.ThemeColor);

        // 6 bloques de intensidad de amenaza
        for (int t = 0; t < 6; t++)
        {
            Rect seg = ScaleRect(576f + t * 78f, 848f, 70f, 18f);
            bool active = (t < entry.ThreatRating);
            GUI.color = active
                ? Color.Lerp(new Color(0.22f, 0.94f, 0.62f), new Color(1f, 0.22f, 0.38f), t / 5f)
                : new Color(0.10f, 0.14f, 0.22f, 0.9f);
            GUI.DrawTexture(seg, Texture2D.whiteTexture);
        }
        GUI.color = Color.white;

        DrawStyledText(ScaleRect(576f, 876f, 464f, 24f),
            entry.TacticalStatsSummary,
            12, TextAnchor.MiddleCenter, new Color(0.90f, 0.96f, 1f));

        // Botón rápido para simular registro de baja en pruebas
        if (DrawCyberButton(
            new Rect(576f, 904f, 464f, 36f),
            texBtnEmerald,
            $"⚔ REGISTRAR BAJA DE PRUEBA EN CÓDICE (+1 {entry.DisplayName.ToUpper()})",
            "", 12, 9, Color.white, Color.white, new Color(0.20f, 0.92f, 0.56f)))
        {
            RegisterSpeciesKill(entry.SpeciesId, Vector3.zero, false);
            ShowNotification($"✔ Baja registrada: {entry.DisplayName} desbloqueado y actualizado en el Códice.", entry.ThemeColor);
        }
    }

    void DrawRightTechnicalDossier()
    {
        CodexSpeciesEntry entry = Catalog[selectedIndex];
        bool unlocked = IsSpeciesUnlocked(entry.SpeciesId);
        bool inspectable = unlocked || simulationPreviewAll;
        int totalKills = GetTotalKills(entry.SpeciesId);
        int sessionKills = GetSessionKills(entry.SpeciesId);

        Rect dossierPanel = ScaleRect(1092f, 122f, 756f, 846f);
        DrawCyberBox(dossierPanel, new Color(0.02f, 0.04f, 0.09f, 0.95f), entry.ThemeColor, 2f);

        // Cabecera de la Ficha Técnica
        Rect headerBanner = ScaleRect(1112f, 138f, 716f, 82f);
        DrawCyberBox(headerBanner, new Color(0.04f, 0.08f, 0.16f, 0.96f), entry.ThemeColor, 1.5f);

        DrawStyledText(ScaleRect(1128f, 144f, 460f, 22f),
            $"EXPEDIENTE XENOBIOLÓGICO // {entry.CodeName}",
            13, TextAnchor.MiddleLeft, entry.ThemeColor);

        DrawStyledText(ScaleRect(1128f, 166f, 520f, 32f),
            entry.DisplayName.ToUpper(),
            26, TextAnchor.MiddleLeft, Color.white);

        DrawStyledText(ScaleRect(1128f, 196f, 520f, 20f),
            entry.Classification,
            13, TextAnchor.MiddleLeft, new Color(0.76f, 0.88f, 1f));

        // Contador Destacado de Récord de Bajas (Esquina Superior Derecha de la Ficha)
        Rect killsBadge = ScaleRect(1608f, 146f, 208f, 66f);
        DrawCyberBox(killsBadge, new Color(0.02f, 0.05f, 0.11f, 0.98f), new Color(1f, 0.82f, 0.22f), 1.5f);
        DrawStyledText(ScaleRect(1614f, 150f, 196f, 20f), "☠ RÉCORD DE BAJAS", 12, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.22f));
        DrawStyledText(ScaleRect(1614f, 170f, 196f, 24f), $"{totalKills} ABATIDOS", 18, TextAnchor.MiddleCenter, Color.white);
        DrawStyledText(ScaleRect(1614f, 192f, 196f, 16f), $"Partida actual: {sessionKills}", 11, TextAnchor.MiddleCenter, new Color(0.24f, 0.94f, 0.62f));

        // Sección 1: Origen Alienígena
        DrawDossierFieldCard(
            new Rect(1112f, 232f, 716f, 92f),
            "🌌 ORIGEN ALIENÍGENA Y PROCEDENCIA",
            inspectable ? entry.AlienOrigin : "[DATOS ENCRIPTADOS — DERROTA AL ESPÉCIMEN EN COMBATE O ACTIVA SIMULACIÓN N.E.X.A.]",
            entry.ThemeColor,
            inspectable ? Color.white : new Color(0.55f, 0.68f, 0.82f)
        );

        // Sección 2: Tipo de Blindaje
        DrawDossierFieldCard(
            new Rect(1112f, 334f, 716f, 92f),
            "🛡 COMPOSICIÓN DE BLINDAJE Y DEFENSA",
            inspectable ? entry.ArmorType : "[ANÁLISIS ESTRUCTURAL PENDIENTE DE CAPTURA EN COMBATE]",
            new Color(0.22f, 0.90f, 1f),
            inspectable ? Color.white : new Color(0.55f, 0.68f, 0.82f)
        );

        // Sección 3: Puntos Débiles Corporales
        DrawDossierFieldCard(
            new Rect(1112f, 436f, 716f, 98f),
            "🎯 PUNTOS DÉBILES CORPORALES DETECTADOS",
            inspectable ? entry.BodyWeakPoints : "[ESCÁNER ANATÓMICO BLOQUEADO — REQUIERE MUESTRA DE COMBATE]",
            new Color(1f, 0.42f, 0.32f),
            inspectable ? new Color(1f, 0.92f, 0.86f) : new Color(0.55f, 0.68f, 0.82f)
        );

        // Sección 4: Debilidades Elementales (Guantelete Nexo)
        DrawDossierFieldCard(
            new Rect(1112f, 544f, 716f, 98f),
            "⚡ DEBILIDAD ELEMENTAL Y CONTRAESTRATEGIA NEXO",
            inspectable ? entry.ElementalWeakness : "[CONTRAESTRATEGIA ELEMENTAL CLASIFICADA]",
            new Color(1f, 0.82f, 0.22f),
            inspectable ? new Color(1f, 0.94f, 0.68f) : new Color(0.55f, 0.68f, 0.82f)
        );

        // Sección 5: Fragmento del Diario de Defensa Terrestre (Lore)
        Rect diaryBox = ScaleRect(1112f, 654f, 716f, 294f);
        DrawCyberBox(diaryBox, new Color(0.03f, 0.06f, 0.13f, 0.98f), new Color(0.22f, 0.92f, 1f, 0.9f), 2f);

        // Decoración superior de transmisión de archivo militar
        Rect diaryHeader = ScaleRect(1114f, 656f, 712f, 38f);
        GUI.color = new Color(0.06f, 0.16f, 0.28f, 0.95f);
        GUI.DrawTexture(diaryHeader, Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawStyledText(ScaleRect(1128f, 660f, 684f, 30f),
            $"📡 {entry.DefenseDiaryTitle}",
            13, TextAnchor.MiddleLeft, new Color(0.24f, 0.94f, 1f));

        string diaryBody = inspectable
            ? entry.DefenseDiaryFragment
            : "\"[REGISTRO DE AUDIO CORRUPTO // Códice de Defensa Terrestre: Abate al menos un ejemplar de esta especie en combate real o presiona [TAB] para desclasificar la transcripción táctica de N.E.X.A.]\"";

        DrawStyledText(ScaleRect(1132f, 706f, 676f, 178f),
            diaryBody,
            15, TextAnchor.UpperLeft, inspectable ? new Color(0.90f, 0.96f, 1f) : new Color(0.62f, 0.74f, 0.86f), true);

        // Pie del Diario con firma de verificación y estado
        Rect diaryFooter = ScaleRect(1132f, 896f, 676f, 36f);
        DrawCyberBox(diaryFooter, new Color(0.02f, 0.04f, 0.09f, 0.92f), entry.ThemeColor, 1f);
        string footerStatus = unlocked
            ? $"ESTADO DE INTELIGENCIA: VERIFICADO EN CAMPO ({totalKills} BAJA(S) REGISTRADAS POR KAEL)"
            : "ESTADO DE INTELIGENCIA: SIMULACIÓN PREVENTIVA — DERROTA AL ENEMIGO EN CAMPAÑA PARA CONFIRMAR";
        DrawStyledText(diaryFooter, footerStatus, 12, TextAnchor.MiddleCenter, unlocked ? new Color(0.22f, 0.96f, 0.58f) : new Color(1f, 0.82f, 0.28f));
    }

    void DrawDossierFieldCard(Rect baseRect, string title, string content, Color accentCol, Color textCol)
    {
        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        DrawCyberBox(r, new Color(0.03f, 0.06f, 0.13f, 0.94f), new Color(accentCol.r, accentCol.g, accentCol.b, 0.78f), 1.5f);

        GUI.color = accentCol;
        GUI.DrawTexture(ScaleRect(baseRect.x, baseRect.y, 5f, baseRect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawStyledText(ScaleRect(baseRect.x + 18f, baseRect.y + 8f, baseRect.width - 32f, 24f),
            title, 13, TextAnchor.MiddleLeft, accentCol);

        DrawStyledText(ScaleRect(baseRect.x + 18f, baseRect.y + 34f, baseRect.width - 32f, baseRect.height - 40f),
            content, 14, TextAnchor.UpperLeft, textCol, true);
    }

    void DrawFooterControlsBar()
    {
        Rect footerRect = ScaleRect(68f, 978f, 1780f, 54f);
        DrawCyberBox(footerRect, new Color(0.02f, 0.04f, 0.10f, 0.96f), new Color(0.22f, 0.88f, 1f, 0.75f), 1.5f);

        if (notificationTimer > 0f && !string.IsNullOrEmpty(notificationBanner))
        {
            DrawStyledText(footerRect, notificationBanner, 15, TextAnchor.MiddleCenter, notificationColor);
            return;
        }

        DrawStyledText(footerRect,
            "<color=#38e1ff>[▲/▼ o W/S]</color> Seleccionar Especie   •   <color=#ffd35c>[◀/▶ o A/D o 1-2-3]</color> Cambiar Animación   •   <color=#2ee678>[ESPACIO]</color> Pausar/Reanudar   •   <color=#38e1ff>[F]</color> Voltear   •   <color=#ffd35c>[TAB]</color> Simulación N.E.X.A.   •   <color=#ff5c77>[ESC]</color> Cerrar",
            13, TextAnchor.MiddleCenter, Color.white);
    }

    // =========================================================================
    // PRIMITIVAS DE RENDERIZADO NATIVO 1920x1080
    // =========================================================================
    static Rect ScaleRect(float x, float y, float w, float h)
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

    static int ScaleFont(int baseSize)
    {
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        return Mathf.Max(10, Mathf.RoundToInt(baseSize * scale));
    }

    void DrawSkinnedPanel(Rect r, Texture2D panelTex, Color fallbackBorder)
    {
        if (panelTex != null)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(r, panelTex, ScaleMode.StretchToFill);
        }
        else
        {
            DrawCyberBox(r, new Color(0.03f, 0.05f, 0.11f, 0.96f), fallbackBorder, 3f);
        }
    }

    void DrawCyberBox(Rect r, Color bg, Color border, float borderThickness)
    {
        float t = Mathf.Max(1f, Mathf.Round(borderThickness * Mathf.Min(Screen.width / 1920f, Screen.height / 1080f)));
        GUI.color = border;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = bg;
        GUI.DrawTexture(new Rect(r.x + t, r.y + t, r.width - t * 2f, r.height - t * 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawProgressBar(Rect r, float ratio, Color bgCol, Color fillCol)
    {
        GUI.color = bgCol;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        float w = Mathf.Round(r.width * Mathf.Clamp01(ratio));
        if (w > 0f)
        {
            GUI.color = fillCol;
            GUI.DrawTexture(new Rect(r.x, r.y, w, r.height), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    bool DrawCyberButton(Rect baseRect, Texture2D skinTex, string mainText, string subText, int mainFontSize, int subFontSize, Color mainCol, Color subCol, Color fallbackBorder)
    {
        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool hovered = r.Contains(Event.current.mousePosition);

        if (skinTex != null)
        {
            GUI.color = hovered ? new Color(1.18f, 1.18f, 1.18f, 1f) : Color.white;
            GUI.DrawTexture(r, skinTex, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }
        else
        {
            DrawCyberBox(r, hovered ? new Color(0.12f, 0.22f, 0.36f, 0.98f) : new Color(0.04f, 0.07f, 0.14f, 0.96f), hovered ? Color.white : fallbackBorder, 2f);
        }

        if (string.IsNullOrEmpty(subText))
        {
            DrawStyledText(r, mainText, mainFontSize, TextAnchor.MiddleCenter, hovered ? Color.white : mainCol);
        }
        else
        {
            Rect topR = ScaleRect(baseRect.x + 8f, baseRect.y + 4f, baseRect.width - 16f, baseRect.height * 0.54f);
            Rect botR = ScaleRect(baseRect.x + 8f, baseRect.y + baseRect.height * 0.50f, baseRect.width - 16f, baseRect.height * 0.44f);
            DrawStyledText(topR, mainText, mainFontSize, TextAnchor.MiddleCenter, hovered ? Color.white : mainCol);
            DrawStyledText(botR, subText, subFontSize, TextAnchor.MiddleCenter, subCol);
        }

        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    void DrawStyledText(Rect r, string txt, int baseSize, TextAnchor align, Color col, bool wordWrap = false)
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
