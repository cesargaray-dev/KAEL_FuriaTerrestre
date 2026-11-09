using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class EnemyDummy : MonoBehaviour
{
    [Header("Categoría y Estadísticas Vorst")]
    public bool isMiniBoss = false;
    public bool isBoss = false;
    public string enemyTitle = "Caminante Vorst";
    public float maxHP = 30f;
    public float moveSpeed = 2.2f;
    public float damage = 4f;
    public float attackRange = 1.4f;
    public float attackRate = 1.0f;

    [Header("Recompensas al Morir")]
    public int goldReward = 6;
    public int expReward = 10;

    public event Action OnEnemyDied;

    public float currentHP;
    private float nextAttackTime;
    private int attackCounter = 0;
    private bool isDead = false;
    public bool IsDead => isDead;
    public float CurrentHP => currentHP;
    private bool isChargingHeavy = false;
    private bool isPlayingAttackAnim = false;
    private Transform kaelTarget;
    private SpriteRenderer sr;
    private Animator anim;
    private Color originalColor;

    // --- BLOQUE 1 (Issue #16): Estado Stunned (Aturdido por Parry Perfecto) ---
    public bool isStunned => stunTimer > 0f;
    private float stunTimer = 0f;

    // --- BLOQUE 4 (Issue #22): Quemadura de Plasma (DoT por Disonancia Vorst) ---
    private float burnTimer = 0f;
    private float burnTickTimer = 0f;
    private float burnDamagePerTick = 4f;

    // --- BLOQUE 5 (Issue #23): Mecánicas exclusivas de Monstruos y Jefes del Nivel 3 ---
    public bool isLevel3Monster = false;
    private bool hasEnteredRagePhase = false;
    private float hulkStepShakeTimer = 0f;
    private float nextHulkLaserTime = 0f;
    private readonly List<Transform> psionicRings = new List<Transform>();

    // Cache estático en memoria para que no haya NINGÚN tirón ni lectura de disco al spawnear enemigos
    private static readonly Dictionary<string, Sprite[]> _spriteFrameCache = new Dictionary<string, Sprite[]>();
    private static bool _preloaded = false;
    private static bool _preloadedLevel3 = false;

    // Sprites 256x256 alineados para Nivel 2 y Nivel 3
    private string visualType = "";
    private Sprite[] customWalkFrames;
    private Sprite[] customAttackFrames;
    private Sprite[] customDieFrames;
    private Sprite[] customLaserFrames;
    private Sprite[] customPhaseTransFrames;
    private Sprite[] customEnrageAttackFrames;
    private Sprite[] customMissileFrames;
    private Sprite[] customVortexFlameFrames;
    private Sprite[] customTornadoFrames;
    public bool isInvulnerable = false;
    private float nextLaserTime = 0f;
    private float nextRocketLungeTime = 0f;
    private bool useCustomSprites = false;
    private float walkAnimTimer = 0f;
    private float dieAnimTimer = 0f;
    private float baseGroundY = -2.95f;

    public static void PreloadLevel2Frames()
    {
        if (_preloaded) return;
        _preloaded = true;

        PreloadSet("Stalker", 160f);
        PreloadSet("Brute", 122f);
        PreloadSet("Archon", 118f);
    }

    public static void PreloadLevel3Frames()
    {
        if (_preloadedLevel3) return;
        _preloadedLevel3 = true;

        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioStalker", "Walk", 148f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioStalker", "Attack", 148f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioStalker", "Die", 148f);

        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioLasher", "Walk", 140f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioLasher", "Attack", 140f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioLasher", "Tornado", 140f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioLasher", "Die", 140f);

        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioHulk", "Walk", 122f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioHulk", "Attack", 122f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioHulk", "Laser", 122f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioHulk", "Die", 122f);

        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("HiveInquisitor", "Walk", 122f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("HiveInquisitor", "Attack", 122f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("HiveInquisitor", "Missiles", 122f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("HiveInquisitor", "VortexFlame", 122f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("HiveInquisitor", "Die", 122f);

        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("VortexOmega", "Walk", 118f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("VortexOmega", "Attack", 118f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("VortexOmega", "Laser", 118f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("VortexOmega", "PhaseTrans", 118f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("VortexOmega", "EnrageAttack", 118f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("VortexOmega", "Die", 118f);
    }

    private static void PreloadSet(string prefix, float ppu)
    {
        GetCachedFrameSet(prefix, "Walk", ppu);
        GetCachedFrameSet(prefix, "Attack", ppu);
        GetCachedFrameSet(prefix, "Die", ppu);
    }

    private static Sprite[] GetCachedFrameSet(string prefix, string state, float ppu)
    {
        string cacheKey = $"{prefix}_{state}";
        if (_spriteFrameCache.TryGetValue(cacheKey, out Sprite[] cached) && cached != null && cached.Length > 0)
        {
            return cached;
        }

        List<Sprite> list = new List<Sprite>();
        for (int i = 0; i < 8; i++)
        {
            string path = $"Level2/{prefix}_{state}_{i}";
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                Sprite sp = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.04f),
                    ppu
                );
                list.Add(sp);
            }
        }
        Sprite[] arr = list.ToArray();
        _spriteFrameCache[cacheKey] = arr;
        return arr;
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        currentHP = maxHP;
        baseGroundY = transform.position.y;

        if (isLevel3Monster || visualType == "BioStalker" || visualType == "BioLasher" || visualType == "BioHulk" || visualType == "HiveInquisitor" || visualType == "VortexOmega")
        {
            isLevel3Monster = true;
            if (baseGroundY < -3.05f || baseGroundY > -2.85f) baseGroundY = -2.95f;
            transform.position = new Vector3(transform.position.x, baseGroundY, transform.position.z);
            AlignColliderToGround(Level3MonsterAssetGenerator.GetDefaultColliderSize(visualType));
        }

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (anim == null) anim = GetComponent<Animator>();
        if (sr != null) originalColor = sr.color;

        LocateKaelTarget();

        // Intro de caída desde el techo para el Jefe Final VORTEX-OMEGA (Bloque 5)
        if (visualType == "VortexOmega")
        {
            StartCoroutine(VortexOmegaDropIntroRoutine());
        }
    }

    IEnumerator VortexOmegaDropIntroRoutine()
    {
        isPlayingAttackAnim = true;
        float startY = baseGroundY + 5.8f;
        float dur = 0.42f;
        float t = 0f;
        while (t < dur)
        {
            float p = t / dur;
            float curY = Mathf.Lerp(startY, baseGroundY, p * p);
            transform.position = new Vector3(transform.position.x, curY, transform.position.z);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = new Vector3(transform.position.x, baseGroundY, transform.position.z);

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.ShakeCamera(0.55f, 0.24f);
            CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 0.4f, new Color(1f, 0.20f, 0.55f), 3.4f);
            CombatVFXManager.Instance.SpawnEnergyPillar(transform.position + Vector3.left * 1.6f, new Color(0f, 0.95f, 1f));
            CombatVFXManager.Instance.SpawnEnergyPillar(transform.position + Vector3.right * 1.6f, new Color(1f, 0.20f, 0.55f));
        }
        isPlayingAttackAnim = false;
    }

    void LocateKaelTarget()
    {
        if (KaelAutoController.Instance != null)
        {
            kaelTarget = KaelAutoController.Instance.transform;
            return;
        }
        KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
        if (kael != null) kaelTarget = kael.transform;
    }

    public void SetupCustomVisuals(string resourcePrefix)
    {
        visualType = resourcePrefix;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (anim == null) anim = GetComponent<Animator>();

        float ppu = 160f;
        if (resourcePrefix == "Brute") ppu = 122f;
        else if (resourcePrefix == "Archon") ppu = 118f;

        customWalkFrames = GetCachedFrameSet(resourcePrefix, "Walk", ppu);
        customAttackFrames = GetCachedFrameSet(resourcePrefix, "Attack", ppu);
        customDieFrames = GetCachedFrameSet(resourcePrefix, "Die", ppu);

        if (customWalkFrames != null && customWalkFrames.Length > 0 && customWalkFrames[0] != null)
        {
            useCustomSprites = true;
            if (anim != null) anim.enabled = false;
            sr.sprite = customWalkFrames[0];
        }
    }

    /// <summary>
    /// Ajusta las dimensiones del BoxCollider2D y posiciona su offset tal que su base descanse
    /// exactamente en y = 0 local (alineado con la suela del sprite a groundY = -2.95f global).
    /// </summary>
    public void AlignColliderToGround(Vector2 size)
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
        box.size = size;
        box.offset = new Vector2(0f, size.y * 0.5f);
        box.isTrigger = false;
    }

    /// <summary>
    /// BLOQUE 5 (Issue #23): Configura los sprites y efectos exclusivos de los 4 monstruos
    /// y jefes biomecánicos del Nivel 3 (BioStalker, BioLasher, BioHulk, HiveInquisitor, VortexOmega).
    /// </summary>
    public void SetupLevel3Visuals(string resourcePrefix)
    {
        visualType = resourcePrefix;
        isLevel3Monster = true;
        baseGroundY = -2.95f;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (anim == null) anim = GetComponent<Animator>();

        float ppu = Level3MonsterAssetGenerator.GetDefaultPPU(resourcePrefix);
        Vector2 colSize = Level3MonsterAssetGenerator.GetDefaultColliderSize(resourcePrefix);

        customWalkFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(resourcePrefix, "Walk", ppu);
        customAttackFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(resourcePrefix, "Attack", ppu);
        customDieFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(resourcePrefix, "Die", ppu);

        if (resourcePrefix == "BioLasher")
        {
            customTornadoFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioLasher", "Tornado", ppu);
        }
        else if (resourcePrefix == "BioHulk")
        {
            customLaserFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioHulk", "Laser", ppu);
            attackRange = 3.35f;
            moveSpeed = 1.15f;
        }
        else if (resourcePrefix == "HiveInquisitor")
        {
            customMissileFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames("HiveInquisitor", "Missiles", ppu);
            customVortexFlameFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames("HiveInquisitor", "VortexFlame", ppu);
        }
        else if (resourcePrefix == "VortexOmega")
        {
            customLaserFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames("VortexOmega", "Laser", ppu);
            customPhaseTransFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames("VortexOmega", "PhaseTrans", ppu);
            customEnrageAttackFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames("VortexOmega", "EnrageAttack", ppu);
        }

        if (customWalkFrames != null && customWalkFrames.Length > 0 && customWalkFrames[0] != null)
        {
            useCustomSprites = true;
            if (anim != null) anim.enabled = false;
            sr.sprite = customWalkFrames[0];
        }

        AlignColliderToGround(colSize);

        // Si es HiveInquisitor, creamos sus anillos psiónicos rotatorios (3 para Mini-Jefe, 2 para regular)
        if (resourcePrefix == "HiveInquisitor" && psionicRings.Count == 0)
        {
            int ringCount = isMiniBoss ? 3 : 2;
            Sprite ringSpr = Level3MonsterAssetGenerator.GetPsionicRingSprite();
            for (int i = 0; i < ringCount; i++)
            {
                GameObject ringGo = new GameObject($"PsionicRing_{i}");
                ringGo.transform.SetParent(transform, false);
                ringGo.transform.localPosition = new Vector3(0f, 0.85f + i * 0.15f, 0f);
                ringGo.transform.localScale = new Vector3(1.1f + i * 0.22f, 0.85f + i * 0.15f, 1f);

                SpriteRenderer rsr = ringGo.AddComponent<SpriteRenderer>();
                rsr.sprite = ringSpr;
                rsr.sortingOrder = (sr != null ? sr.sortingOrder : 10) + (i % 2 == 0 ? 1 : -1);
                rsr.color = (i % 2 == 0) ? new Color(0.20f, 0.95f, 1f, 0.78f) : new Color(0.95f, 0.25f, 0.85f, 0.78f);
                psionicRings.Add(ringGo.transform);
            }
        }
    }

    /// <summary>
    /// Configura completamente un monstruo biomecánico del Nivel 3 según su arquetipo (0..3) o rol de jefe.
    /// Incorpora rotación de 4 enemigos neón en oleadas 1-9, mini-jefe en oleada 5 y jefe en oleada 10.
    /// </summary>
    public void SetupLevel3Monster(int archetype, int wave = 1, bool boss = false, bool miniBoss = false)
    {
        isBoss = boss;
        isMiniBoss = miniBoss;
        baseGroundY = -2.95f;

        if (boss)
        {
            enemyTitle = "Mecha Berserker VORTEX-OMEGA";
            SetupLevel3Visuals("VortexOmega");
            transform.localScale = Vector3.one * 1.68f;
            attackRange = 2.65f;
            attackRate = 0.85f;
            moveSpeed = 1.25f; // Marcha mecánica pesada, firme y deliberada en Fase 1
            maxHP = 3400f;
            damage = 28f;
            goldReward = 400;
            expReward = 360;
            currentHP = maxHP;
            hasEnteredRagePhase = false;
            isInvulnerable = false;
            nextLaserTime = Time.time + 4.0f;
            nextRocketLungeTime = Time.time + 6.5f;
            return;
        }

        if (miniBoss)
        {
            enemyTitle = "Inquisidor Psiónico Supremo";
            SetupLevel3Visuals("HiveInquisitor");
            transform.localScale = Vector3.one * 1.52f;
            moveSpeed = 1.35f;
            attackRange = 6.80f; // Hechicero psiónico a media-larga distancia
            attackRate = 0.40f;  // Cadencia táctica legible (un ataque cada ~2.5s - 3.2s)
            maxHP = 1450f;
            damage = 20f;
            goldReward = 200;
            expReward = 180;
            currentHP = maxHP;
            return;
        }

        int type = ((archetype % 4) + 4) % 4;
        switch (type)
        {
            case 0:
                // 1. Pantera Biomecánica Neón (BioStalker): velocidad máxima, tajo doble, agilidad depredadora
                enemyTitle = "Pantera Biomecánica Neón";
                SetupLevel3Visuals("BioStalker");
                transform.localScale = Vector3.one * 1.05f;
                moveSpeed = 2.75f;
                attackRange = 1.50f;
                attackRate = 1.15f;
                maxHP = Mathf.RoundToInt(75f * Mathf.Pow(1.15f, Mathf.Max(0, wave - 1)));
                damage = Mathf.RoundToInt(7f * Mathf.Pow(1.13f, Mathf.Max(0, wave - 1)));
                break;

            case 1:
                // 2. Alienígena Neón Tentacular (BioLasher): levitación sinusoidal, látigos psiónicos a media distancia
                enemyTitle = "Alienígena Neón Tentacular";
                SetupLevel3Visuals("BioLasher");
                transform.localScale = Vector3.one * 1.12f;
                moveSpeed = 1.85f;
                attackRange = 2.45f;
                attackRate = 0.90f;
                maxHP = Mathf.RoundToInt(90f * Mathf.Pow(1.15f, Mathf.Max(0, wave - 1)));
                damage = Mathf.RoundToInt(9f * Mathf.Pow(1.13f, Mathf.Max(0, wave - 1)));
                break;

            case 2:
                // 3. Coloso Blindado Neón (BioHulk): pesado, sacude suelo con pasos, aplastamiento telegrafiado masivo
                enemyTitle = "Coloso Blindado Neón";
                SetupLevel3Visuals("BioHulk");
                transform.localScale = Vector3.one * 1.28f;
                moveSpeed = 1.15f; // Tanque imparable de marcha pesada
                attackRange = 3.35f; // Rango amplio y calibrado para iniciar el impacto sin colisionar con Kael
                attackRate = 0.70f;
                maxHP = Mathf.RoundToInt(150f * Mathf.Pow(1.16f, Mathf.Max(0, wave - 1)));
                damage = Mathf.RoundToInt(13f * Mathf.Pow(1.14f, Mathf.Max(0, wave - 1)));
                break;

            case 3:
                // 4. Inquisidor Psiónico Neón (HiveInquisitor regular): hechicero de energía oscura, bombardeo a distancia
                enemyTitle = "Inquisidor Psiónico Neón";
                SetupLevel3Visuals("HiveInquisitor");
                transform.localScale = Vector3.one * 1.18f;
                moveSpeed = 1.30f;
                attackRange = 6.20f; // Combate a distancia con orbes y rayos psiónicos
                attackRate = 0.35f;  // Ataque telegrafiado cada ~2.8s - 3.5s
                maxHP = Mathf.RoundToInt(110f * Mathf.Pow(1.15f, Mathf.Max(0, wave - 1)));
                damage = Mathf.RoundToInt(11f * Mathf.Pow(1.13f, Mathf.Max(0, wave - 1)));
                break;
        }

        goldReward = Mathf.RoundToInt(12f + (wave * 3.5f));
        expReward = 16 + (wave * 4);
        currentHP = maxHP;
    }

    /// <summary>
    /// Sobrecarga para asignar arquetipo consultando la oleada activa de WaveSpawner.
    /// </summary>
    public void SetupLevel3Monster(int archetype, bool boss, bool miniBoss)
    {
        WaveSpawner ws = FindAnyObjectByType<WaveSpawner>();
        int wave = (ws != null) ? Mathf.Max(1, ws.currentWave) : 1;
        SetupLevel3Monster(archetype, wave, boss, miniBoss);
    }

    /// <summary>
    /// Sobrecarga para asignar visuales y arquetipo directamente por nombre de prefijo.
    /// </summary>
    public void SetupLevel3Monster(string prefix, bool boss = false, bool miniBoss = false)
    {
        isBoss = boss;
        isMiniBoss = miniBoss;
        int arch = 0;
        if (prefix == "BioStalker") arch = 0;
        else if (prefix == "BioLasher") arch = 1;
        else if (prefix == "BioHulk") arch = 2;
        else if (prefix == "HiveInquisitor") arch = 3;
        else if (prefix == "VortexOmega") { arch = 0; isBoss = true; }
        WaveSpawner ws = FindAnyObjectByType<WaveSpawner>();
        int wave = (ws != null) ? Mathf.Max(1, ws.currentWave) : 1;
        SetupLevel3Monster(arch, wave, isBoss, isMiniBoss);
    }

    /// <summary>
    /// BLOQUE 1 (Issue #16): Aturde al enemigo tras recibir el Contragolpe (Riposte) de un Parry Perfecto.
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (isDead) return;
        stunTimer = Mathf.Max(stunTimer, duration);
        isChargingHeavy = false;
        isPlayingAttackAnim = false;
        nextAttackTime = Time.time + duration + 0.25f;
        if (sr != null) sr.color = new Color(0.45f, 0.95f, 1f);
    }

    /// <summary>
    /// BLOQUE 4 (Issue #22): Aplica Quemadura de Plasma (DoT) cuando Kael está en Disonancia Vorst.
    /// </summary>
    public void ApplyPlasmaBurn(float dps, float duration)
    {
        if (isDead) return;
        burnDamagePerTick = Mathf.Max(2f, dps * 0.45f);
        burnTimer = Mathf.Max(burnTimer, duration);
    }

    void Update()
    {
        if (isDead) return;
        if (kaelTarget == null)
        {
            LocateKaelTarget();
            if (kaelTarget == null) return;
        }

        if (PlayerStats.Instance != null &&
            (PlayerStats.Instance.isPaused ||
             PlayerStats.Instance.isGameOver ||
             PlayerStats.Instance.isLevelCompleted ||
             PlayerStats.Instance.isChoosingDilemma ||
             PlayerStats.Instance.isChoosingSkill ||
             PlayerStats.Instance.isUpgradeTreeOpen))
            return;

        // Procesar Quemadura de Plasma (DoT - Bloque 4)
        if (burnTimer > 0f)
        {
            burnTimer -= Time.deltaTime;
            burnTickTimer -= Time.deltaTime;
            if (burnTickTimer <= 0f)
            {
                burnTickTimer = 0.45f;
                TakeDamage(burnDamagePerTick, false, 0f);
                if (CombatVFXManager.Instance != null && !isDead)
                {
                    CombatVFXManager.Instance.SpawnElectricArc(transform.position + Vector3.up * 0.7f, new Color(0.85f, 0.15f, 1f));
                }
            }
        }

        // Rotar anillos psiónicos del Mini-Jefe HiveInquisitor
        for (int i = 0; i < psionicRings.Count; i++)
        {
            if (psionicRings[i] != null)
            {
                float speed = (i % 2 == 0) ? 110f : -135f;
                psionicRings[i].Rotate(0f, 0f, speed * Time.deltaTime);
            }
        }

        // Procesar Estado Aturdido (Stunned por Parry Perfecto - Bloque 1)
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (sr != null)
            {
                float flash = Mathf.PingPong(Time.time * 10f, 1f);
                sr.color = Color.Lerp(originalColor, new Color(0.35f, 0.95f, 1f), flash * 0.65f);
            }
            if (stunTimer <= 0f && sr != null)
            {
                sr.color = originalColor;
            }
            return;
        }

        float diffX = kaelTarget.position.x - transform.position.x;
        float distToKael = Mathf.Abs(diffX);
        float dirToKael = (Mathf.Abs(diffX) > 0.02f) ? Mathf.Sign(diffX) : -1f;

        if (visualType == "VortexOmega")
        {
            UpdateVortexOmegaBoss(distToKael, dirToKael);
            return;
        }

        if (sr != null && !isPlayingAttackAnim && !isChargingHeavy)
        {
            sr.flipX = (dirToKael > 0f);
        }

        if (distToKael > attackRange && !isChargingHeavy && !isPlayingAttackAnim)
        {
            // Cañón Láser de Pecho a Media Distancia para BioHulk:
            // Si marcha hacia Kael y está entre 3.2m y 7.5m, se detiene y desata su rayo verde neón pesado.
            if (visualType == "BioHulk" && distToKael >= 3.2f && distToKael <= 7.5f && Time.time >= nextHulkLaserTime)
            {
                nextHulkLaserTime = Time.time + UnityEngine.Random.Range(5.5f, 8.0f);
                StartCoroutine(BioHulkChestLaserRoutine());
                return;
            }

            if (anim != null && !useCustomSprites) anim.SetBool("IsAttacking", false);
            transform.Translate(new Vector2(dirToKael, 0f) * (moveSpeed * Time.deltaTime), Space.World);

            // El Coloso de Crisálida (BioHulk) hace vibrar ligeramente la cámara con sus pasos pesados
            if (visualType == "BioHulk")
            {
                hulkStepShakeTimer -= Time.deltaTime;
                if (hulkStepShakeTimer <= 0f)
                {
                    hulkStepShakeTimer = 0.65f;
                    if (CombatVFXManager.Instance != null)
                        CombatVFXManager.Instance.ShakeCamera(0.06f, 0.03f);
                }
            }
        }
        else
        {
            if (anim != null && !useCustomSprites) anim.SetBool("IsAttacking", true);

            if (!isChargingHeavy && !isPlayingAttackAnim && Time.time >= nextAttackTime)
            {
                if (visualType == "HiveInquisitor")
                {
                    attackCounter++;
                    int mode = attackCounter % 3;
                    if (mode == 0)
                        StartCoroutine(HiveInquisitorPsionicAttackRoutine());
                    else if (mode == 1)
                        StartCoroutine(HiveInquisitorMissileBarrageRoutine());
                    else
                        StartCoroutine(HiveInquisitorVortexFlameRoutine());
                }
                else if (visualType == "BioHulk")
                {
                    attackCounter++;
                    StartCoroutine(BioHulkGroundSmashRoutine());
                }
                else if (visualType == "BioLasher")
                {
                    attackCounter++;
                    if (attackCounter % 2 == 0)
                        StartCoroutine(BioLasherTornadoRoutine());
                    else
                        StartCoroutine(PlayVisibleAttackRoutine());
                }
                else
                {
                    attackCounter++;
                    bool triggerHeavy = ((isMiniBoss || isBoss) && attackCounter % 3 == 0);
                    if (triggerHeavy)
                    {
                        StartCoroutine(TelegraphedHeavyAttack());
                    }
                    else if (useCustomSprites)
                    {
                        StartCoroutine(PlayVisibleAttackRoutine());
                    }
                    else
                    {
                        nextAttackTime = Time.time + (1f / attackRate);
                        if (PlayerStats.Instance != null)
                        {
                            PlayerStats.Instance.TakeDamage(damage, this);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Ataque a distancia del Inquisidor Psiónico Vorst: canaliza energía oscura, acelera sus anillos psiónicos,
    /// genera arcos eléctricos en su mano y dispara rayos o proyectiles psiónicos de largo alcance.
    /// </summary>
    private IEnumerator HiveInquisitorPsionicAttackRoutine()
    {
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        float baseX = transform.position.x;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > baseX) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        if (CombatVFXManager.Instance != null)
        {
            string warn = isMiniBoss ? "⚠ ¡VÓRTICE PSIÓNICO SUPREMO!" : "⚠ ¡DESCARGA PSIÓNICA VORST!";
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.8f, warn, new Color(0.85f, 0.25f, 1f), 19, 0.9f);
        }

        Vector3 handPos = transform.position + Vector3.up * 1.15f + Vector3.right * (dirToKael * 0.45f);

        // Cuadros 0, 1, 2: Concentración de energía psiónica
        int totalFrames = (customAttackFrames != null && customAttackFrames.Length >= 8) ? 8 : (customAttackFrames != null ? customAttackFrames.Length : 0);
        for (int f = 0; f < 3; f++)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (customAttackFrames != null && f < customAttackFrames.Length && customAttackFrames[f] != null)
                sr.sprite = customAttackFrames[f];

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnElectricArc(handPos, new Color(0.80f, 0.15f, 1f));
                CombatVFXManager.Instance.SpawnHitSparks(handPos, false, 3);
            }
            yield return new WaitForSeconds(0.16f);
        }

        // Cuadro 3: Disparo psiónico a distancia
        if (customAttackFrames != null && customAttackFrames.Length > 3 && customAttackFrames[3] != null)
            sr.sprite = customAttackFrames[3];

        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayBossAlert();

        float beamLength = 8.5f;
        if (isMiniBoss)
        {
            // Mini-Jefe: Rayo continuo psiónico de plasma violeta a distancia
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnBossLaserBeam(handPos, dirToKael * beamLength, 0.50f);
                CombatVFXManager.Instance.SpawnShockwaveRing(handPos, new Color(0.75f, 0.15f, 1f), 3.2f);
                CombatVFXManager.Instance.ShakeCamera(0.28f, 0.18f);
                CombatVFXManager.Instance.TriggerScreenFlash(new Color(0.65f, 0.15f, 0.95f), 0.35f);
            }
        }
        else
        {
            // Inquisidor Regular: Disparo de 3 ondas/orbes psiónicos con arcos eléctricos
            if (CombatVFXManager.Instance != null)
            {
                for (int p = 1; p <= 3; p++)
                {
                    Vector3 burstPos = handPos + Vector3.right * (dirToKael * p * 1.9f);
                    CombatVFXManager.Instance.SpawnElectricArc(burstPos, new Color(0.85f, 0.20f, 1f));
                    CombatVFXManager.Instance.SpawnShockwaveRing(burstPos, new Color(0.80f, 0.15f, 0.95f), 1.6f + p * 0.4f);
                }
                CombatVFXManager.Instance.ShakeCamera(0.18f, 0.12f);
            }
        }

        // Impacto a Kael si está en el sector frontal del ataque
        if (kaelTarget != null)
        {
            float diffX = kaelTarget.position.x - transform.position.x;
            bool isInFront = (dirToKael > 0f && diffX > 0f) || (dirToKael < 0f && diffX < 0f);
            float dist = Mathf.Abs(diffX);

            if (isInFront && dist <= beamLength)
            {
                if (PlayerStats.Instance != null)
                {
                    float dmg = isMiniBoss ? damage * 1.25f : damage;
                    PlayerStats.Instance.TakeDamage(dmg, this);
                }
                if (CombatVFXManager.Instance != null)
                {
                    CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.6f, true, 8);
                }
            }
        }

        yield return new WaitForSeconds(0.26f);

        // Cuadros 4..7: Recuperación suave de la postura
        for (int f = 4; f < totalFrames; f++)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (customAttackFrames != null && f < customAttackFrames.Length && customAttackFrames[f] != null)
                sr.sprite = customAttackFrames[f];
            yield return new WaitForSeconds(0.12f);
        }

        transform.position = new Vector3(baseX, baseGroundY, transform.position.z);
        nextAttackTime = Time.time + UnityEngine.Random.Range(2.5f, 3.4f);
        isPlayingAttackAnim = false;
    }

    /// <summary>
    /// Ataque de Bombardeo de Proyectiles Psiónicos del Inquisidor Vorst (Fila 0):
    /// Carga orbes de plasma oscuro en ambas manos y dispara 5 dardos psiónicos en abanico telegrafiado.
    /// </summary>
    private IEnumerator HiveInquisitorMissileBarrageRoutine()
    {
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        float baseX = transform.position.x;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > baseX) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        if (CombatVFXManager.Instance != null)
        {
            string txt = isMiniBoss ? "⚠ ¡MISILES PSIÓNICOS DEL ARCONTE!" : "⚠ ¡DESCARGA DE DARDOS VORST!";
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.8f, txt, new Color(0.90f, 0.20f, 1f), 19, 0.9f);
        }

        Vector3 handPos = transform.position + Vector3.up * 1.15f + Vector3.right * (dirToKael * 0.45f);
        int totalFrames = (customMissileFrames != null && customMissileFrames.Length >= 8) ? 8 : (customMissileFrames != null ? customMissileFrames.Length : 0);

        // Cuadros 0, 1, 2: Concentración de energía en esferas gemelas
        for (int f = 0; f < 3; f++)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (customMissileFrames != null && f < customMissileFrames.Length && customMissileFrames[f] != null)
                sr.sprite = customMissileFrames[f];

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnElectricArc(handPos, new Color(0.85f, 0.20f, 1f));
                CombatVFXManager.Instance.SpawnHitSparks(handPos, false, 2);
            }
            yield return new WaitForSeconds(0.14f);
        }

        // Cuadro 3 y 4: Lanzamiento en abanico de 5 misiles psiónicos con estela
        if (customMissileFrames != null && customMissileFrames.Length > 3 && customMissileFrames[3] != null)
            sr.sprite = customMissileFrames[3];

        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayEnemyAttack();

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.ShakeCamera(0.24f, 0.16f);
            for (int m = 1; m <= 5; m++)
            {
                Vector3 dartPos = handPos + Vector3.right * (dirToKael * m * 1.25f) + Vector3.up * (Mathf.Sin(m * 0.8f) * 0.35f);
                CombatVFXManager.Instance.SpawnElectricArc(dartPos, new Color(0.85f, 0.25f, 1f));
                CombatVFXManager.Instance.SpawnShockwaveRing(dartPos, new Color(0.95f, 0.25f, 0.85f), 1.2f + m * 0.2f);
            }
        }

        yield return new WaitForSeconds(0.22f);

        if (customMissileFrames != null && customMissileFrames.Length > 4 && customMissileFrames[4] != null)
            sr.sprite = customMissileFrames[4];

        // Impacto frontal a Kael
        if (kaelTarget != null)
        {
            float diffX = kaelTarget.position.x - transform.position.x;
            bool isInFront = (dirToKael > 0f && diffX > 0f) || (dirToKael < 0f && diffX < 0f);
            float dist = Mathf.Abs(diffX);
            if (isInFront && dist <= 7.2f && PlayerStats.Instance != null)
            {
                float hitDmg = isMiniBoss ? damage * 1.30f : damage * 1.10f;
                PlayerStats.Instance.TakeDamage(hitDmg, this);
                if (CombatVFXManager.Instance != null)
                    CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.6f, true, 8);
            }
        }

        yield return new WaitForSeconds(0.20f);

        // Cuadros 5..7: Recuperación suave de la postura
        for (int f = 5; f < totalFrames; f++)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (customMissileFrames != null && f < customMissileFrames.Length && customMissileFrames[f] != null)
                sr.sprite = customMissileFrames[f];
            yield return new WaitForSeconds(0.12f);
        }

        transform.position = new Vector3(baseX, baseGroundY, transform.position.z);
        nextAttackTime = Time.time + UnityEngine.Random.Range(2.2f, 3.0f);
        isPlayingAttackAnim = false;
    }

    /// <summary>
    /// Ataque de Torbellino Psiónico y Muro de Fuego Terrestre del Inquisidor Vorst (Fila 1):
    /// Alza sus brazos canalizando un torbellino púrpura bajo sus pies y luego libera una ola de fuego
    /// y relámpagos oscuros que avanza reptando por el piso hacia Kael.
    /// </summary>
    private IEnumerator HiveInquisitorVortexFlameRoutine()
    {
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        float baseX = transform.position.x;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > baseX) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        if (CombatVFXManager.Instance != null)
        {
            string txt = isMiniBoss ? "⚠ ¡TORBELLINO Y OLA DE PLASMA TERRESTRE!" : "⚠ ¡LLAMA OSCURA TERRESTRE VORST!";
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.9f, txt, new Color(0.95f, 0.15f, 0.70f), 20, 1.0f);
        }

        int totalFrames = (customVortexFlameFrames != null && customVortexFlameFrames.Length >= 8) ? 8 : (customVortexFlameFrames != null ? customVortexFlameFrames.Length : 0);

        // Cuadros 0, 1, 2: Torbellino giratorio en los pies
        for (int f = 0; f < 3; f++)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (customVortexFlameFrames != null && f < customVortexFlameFrames.Length && customVortexFlameFrames[f] != null)
                sr.sprite = customVortexFlameFrames[f];

            if (CombatVFXManager.Instance != null)
            {
                Vector3 feetPos = transform.position + Vector3.up * 0.25f;
                CombatVFXManager.Instance.SpawnShockwaveRing(feetPos, new Color(0.85f, 0.15f, 0.95f), 1.6f + f * 0.5f);
                CombatVFXManager.Instance.SpawnElectricArc(feetPos, new Color(0.90f, 0.20f, 1f));
            }
            yield return new WaitForSeconds(0.18f);
        }

        // Cuadro 3: DESPLIEGUE MASIVO DE LLAMA Y RELÁMPAGOS TERRESTRES
        if (customVortexFlameFrames != null && customVortexFlameFrames.Length > 3 && customVortexFlameFrames[3] != null)
            sr.sprite = customVortexFlameFrames[3];

        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayBossAlert();

        float waveLength = 8.0f;
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.ShakeCamera(0.36f, 0.24f);
            CombatVFXManager.Instance.TriggerScreenFlash(new Color(0.70f, 0.10f, 0.85f), 0.40f);

            // Muro de fuego reptante por el suelo hacia Kael
            for (int p = 1; p <= 4; p++)
            {
                Vector3 pillarPos = new Vector3(baseX + dirToKael * (p * 1.85f), baseGroundY, 0f);
                CombatVFXManager.Instance.SpawnEnergyPillar(pillarPos, (p % 2 == 0) ? new Color(0.85f, 0.15f, 1f) : new Color(0.45f, 0.10f, 0.95f));
                CombatVFXManager.Instance.SpawnElectricArc(pillarPos + Vector3.up * 0.5f, new Color(1f, 0.35f, 0.90f));
            }
        }

        // Impacto a Kael si se encuentra en el suelo en el rango de la ola
        if (kaelTarget != null)
        {
            float diffX = kaelTarget.position.x - transform.position.x;
            bool isInFront = (dirToKael > 0f && diffX > 0f) || (dirToKael < 0f && diffX < 0f);
            float dist = Mathf.Abs(diffX);
            if (isInFront && dist <= waveLength && kaelTarget.position.y <= baseGroundY + 1.2f && PlayerStats.Instance != null)
            {
                float hitDmg = isMiniBoss ? damage * 1.45f : damage * 1.20f;
                PlayerStats.Instance.TakeDamage(hitDmg, this);
                if (CombatVFXManager.Instance != null)
                    CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.5f, true, 10);
            }
        }

        yield return new WaitForSeconds(0.32f);

        // Cuadros 4..7: Recuperación de la postura
        for (int f = 4; f < totalFrames; f++)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (customVortexFlameFrames != null && f < customVortexFlameFrames.Length && customVortexFlameFrames[f] != null)
                sr.sprite = customVortexFlameFrames[f];
            yield return new WaitForSeconds(0.14f);
        }

        transform.position = new Vector3(baseX, baseGroundY, transform.position.z);
        nextAttackTime = Time.time + UnityEngine.Random.Range(2.4f, 3.2f);
        isPlayingAttackAnim = false;
    }

    /// <summary>
    /// Ataque de Impacto Sísmico del Coloso de Crisálida (BioHulk): alza ambos puños, sobrecarga
    /// sus tanques dorsales y estampa el suelo levantando ondas sísmicas púrpuras y verdes.
    /// </summary>
    private IEnumerator BioHulkGroundSmashRoutine()
    {
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        float baseX = transform.position.x;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > baseX) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        int totalFrames = (customAttackFrames != null && customAttackFrames.Length >= 8) ? 8 : (customAttackFrames != null ? customAttackFrames.Length : 0);

        // Cuadros 0, 1, 2: Alza los brazos y los tanques brillan
        for (int f = 0; f < 3; f++)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (customAttackFrames != null && f < customAttackFrames.Length && customAttackFrames[f] != null)
                sr.sprite = customAttackFrames[f];

            if (f == 1 && CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnElectricArc(transform.position + Vector3.up * 1.5f, new Color(0.15f, 0.95f, 0.35f));
            }
            yield return new WaitForSeconds(0.16f);
        }

        // Cuadro 3: IMPACTO BRUTAL CONTRA EL SUELO
        if (customAttackFrames != null && customAttackFrames.Length > 3 && customAttackFrames[3] != null)
            sr.sprite = customAttackFrames[3];

        transform.position = new Vector3(baseX + dirToKael * 0.35f, baseGroundY, transform.position.z);

        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayEnemyAttack();

        if (CombatVFXManager.Instance != null)
        {
            Vector3 impactPos = transform.position + Vector3.right * (dirToKael * 0.7f) + Vector3.up * 0.1f;
            CombatVFXManager.Instance.SpawnShockwaveRing(impactPos, new Color(0.75f, 0.15f, 0.95f), 2.9f);
            CombatVFXManager.Instance.SpawnShockwaveRing(impactPos, new Color(0.20f, 0.95f, 0.40f), 1.9f);
            CombatVFXManager.Instance.ShakeCamera(0.32f, 0.22f);
            CombatVFXManager.Instance.SpawnHitSparks(impactPos, true, 8);
        }

        bool inReach = (kaelTarget == null) || (Mathf.Abs(transform.position.x - kaelTarget.position.x) <= attackRange + 0.65f);
        if (inReach && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.TakeDamage(damage * 1.35f, this);
        }

        yield return new WaitForSeconds(0.25f);

        // Cuadros 4..7: Recuperación pesada
        for (int f = 4; f < totalFrames; f++)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (customAttackFrames != null && f < customAttackFrames.Length && customAttackFrames[f] != null)
                sr.sprite = customAttackFrames[f];
            yield return new WaitForSeconds(0.14f);
        }

        transform.position = new Vector3(baseX, baseGroundY, transform.position.z);
        nextAttackTime = Time.time + UnityEngine.Random.Range(1.2f, 1.8f);
        isPlayingAttackAnim = false;
    }

    /// <summary>
    /// Ataque de Cañón Láser de Pecho del Coloso de Crisálida (BioHulk - Fila 2):
    /// Los tanques dorsales brillan verde neón, la placa pectoral se abre y dispara un potente
    /// haz de plasma pesado hacia el jugador.
    /// </summary>
    private IEnumerator BioHulkChestLaserRoutine()
    {
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        float baseX = transform.position.x;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > baseX) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 2.0f,
                "⚠ ¡CAÑÓN LÁSER DE CRISÁLIDA!",
                new Color(0.20f, 0.95f, 0.40f),
                21,
                1.0f
            );
        }

        Vector3 chestPos = transform.position + Vector3.up * 1.15f + Vector3.right * (dirToKael * 0.5f);
        int totalFrames = (customLaserFrames != null && customLaserFrames.Length >= 8) ? 8 : (customLaserFrames != null ? customLaserFrames.Length : 0);

        // Cuadros 0, 1, 2: Sobrecarga de tanques dorsales y apertura de compuerta de pecho
        for (int f = 0; f < 3; f++)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (customLaserFrames != null && f < customLaserFrames.Length && customLaserFrames[f] != null)
                sr.sprite = customLaserFrames[f];

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnElectricArc(chestPos, new Color(0.20f, 0.95f, 0.35f));
                CombatVFXManager.Instance.SpawnHitSparks(chestPos, false, 3);
            }
            yield return new WaitForSeconds(0.16f);
        }

        // Cuadro 3: DISPARO MASIVO DEL RAYO LÁSER VERDE
        if (customLaserFrames != null && customLaserFrames.Length > 3 && customLaserFrames[3] != null)
            sr.sprite = customLaserFrames[3];

        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayUltimateLaser();

        float beamLength = 7.5f;
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnBossLaserBeam(chestPos, dirToKael * beamLength, 0.45f);
            CombatVFXManager.Instance.SpawnShockwaveRing(chestPos, new Color(0.20f, 0.95f, 0.40f), 3.0f);
            CombatVFXManager.Instance.ShakeCamera(0.36f, 0.22f);
            CombatVFXManager.Instance.TriggerScreenFlash(new Color(0.15f, 0.95f, 0.35f), 0.35f);
        }

        // Impacto a Kael si se encuentra en la trayectoria del haz
        if (kaelTarget != null)
        {
            float diffX = kaelTarget.position.x - transform.position.x;
            bool isInFront = (dirToKael > 0f && diffX > 0f) || (dirToKael < 0f && diffX < 0f);
            float dist = Mathf.Abs(diffX);
            if (isInFront && dist <= beamLength && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.TakeDamage(damage * 1.50f, this);
                if (CombatVFXManager.Instance != null)
                    CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.6f, true, 10);
            }
        }

        yield return new WaitForSeconds(0.30f);

        // Cuadros 4..7: Enfriamiento de ventilación de vapor
        for (int f = 4; f < totalFrames; f++)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (customLaserFrames != null && f < customLaserFrames.Length && customLaserFrames[f] != null)
                sr.sprite = customLaserFrames[f];
            yield return new WaitForSeconds(0.12f);
        }

        transform.position = new Vector3(baseX, baseGroundY, transform.position.z);
        nextAttackTime = Time.time + 0.45f;
        isPlayingAttackAnim = false;
    }

    /// <summary>
    /// Ataque de Ciclón de Tentáculos de la Alienígena Neón Tentacular (BioLasher - Fila 2):
    /// Hace girar violentamente sus tentáculos en un vórtice cortante con chispas de energía psiónica.
    /// </summary>
    private IEnumerator BioLasherTornadoRoutine()
    {
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        float baseX = transform.position.x;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > baseX) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 1.7f,
                "⚠ ¡CICLÓN DE TENTÁCULOS!",
                new Color(0.20f, 0.90f, 1f),
                19,
                0.9f
            );
        }

        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayEnemyAttack();

        int totalFrames = (customTornadoFrames != null && customTornadoFrames.Length >= 8) ? 8 : (customTornadoFrames != null ? customTornadoFrames.Length : 0);
        float cycloneReach = 2.80f;

        for (int f = 0; f < totalFrames; f++)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (customTornadoFrames != null && f < customTornadoFrames.Length && customTornadoFrames[f] != null)
                sr.sprite = customTornadoFrames[f];

            if (CombatVFXManager.Instance != null && (f == 2 || f == 4))
            {
                CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 0.9f, new Color(0.15f, 0.95f, 1f), 2.4f);
                CombatVFXManager.Instance.SpawnElectricArc(transform.position + Vector3.up * 0.9f, new Color(0.85f, 0.20f, 1f));
                CombatVFXManager.Instance.ShakeCamera(0.18f, 0.12f);
            }

            // Impactos en fotogramas de máxima expansión
            if (f == 3 || f == 5)
            {
                bool inReach = (kaelTarget == null) || (Mathf.Abs(transform.position.x - kaelTarget.position.x) <= cycloneReach);
                if (inReach && PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.TakeDamage(damage * 0.75f, this);
                    if (CombatVFXManager.Instance != null && kaelTarget != null)
                        CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.6f, true, 6);
                }
            }

            yield return new WaitForSeconds(0.18f);
        }

        transform.position = new Vector3(baseX, baseGroundY, transform.position.z);
        nextAttackTime = Time.time + UnityEngine.Random.Range(2.0f, 2.8f);
        isPlayingAttackAnim = false;
    }

    // Secuencia de ataque fluida en 8 cuadros (incluye salto parabólico en BioStalker y latigazo en BioLasher)
    IEnumerator PlayVisibleAttackRoutine()
    {
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        float baseX = transform.position.x;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > baseX) ? 1f : -1f;
        float lungeMax = (visualType == "Archon" || visualType == "HiveInquisitor") ? 0.16f : ((visualType == "BioStalker") ? 0.62f : 0.34f);
        float leapMaxY = (visualType == "BioStalker") ? 0.48f : 0f;
        int totalFrames = (customAttackFrames != null && customAttackFrames.Length > 0) ? customAttackFrames.Length : 8;

        for (int f = 0; f < totalFrames; f++)
        {
            if (isDead || isStunned)
            {
                isPlayingAttackAnim = false;
                yield break;
            }

            if (customAttackFrames != null && f < customAttackFrames.Length && customAttackFrames[f] != null)
                sr.sprite = customAttackFrames[f];

            float curve = Mathf.Sin((f / (float)Mathf.Max(1, totalFrames - 1)) * Mathf.PI);
            transform.position = new Vector3(
                baseX + dirToKael * lungeMax * curve,
                baseGroundY + leapMaxY * curve,
                transform.position.z
            );

            if (f == 3 || (visualType == "BioStalker" && f == 5))
            {
                if (LevelAudioManager.Instance != null)
                    LevelAudioManager.Instance.PlayEnemyAttack();

                bool inStrikeReach = (kaelTarget == null) || (Mathf.Abs(transform.position.x - kaelTarget.position.x) <= attackRange + 0.72f);
                if (inStrikeReach && PlayerStats.Instance != null)
                {
                    float hitDmg = (visualType == "BioStalker" && f == 5) ? damage * 0.55f : damage;
                    PlayerStats.Instance.TakeDamage(hitDmg, this);
                }

                if (inStrikeReach && CombatVFXManager.Instance != null && kaelTarget != null)
                    CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.6f, false, 6);
            }

            float frameWait = (visualType == "VortexOmega") ? 0.14f : ((visualType == "BioLasher") ? 0.16f : 0.088f);
            yield return new WaitForSeconds(frameWait);
        }

        transform.position = new Vector3(baseX, baseGroundY, transform.position.z);
        nextAttackTime = Time.time + (0.50f / attackRate);
        isPlayingAttackAnim = false;
    }

    // =========================================================================
    // BLOQUE 5 (Issue #23 & Milestone M3): MÁQUINA DE ESTADOS Y COMBATE MULTI-FASE VORTEX-OMEGA
    // =========================================================================

    private void UpdateVortexOmegaBoss(float distToKael, float dirToKael)
    {
        if (isInvulnerable || isPlayingAttackAnim || isChargingHeavy) return;

        if (sr != null) sr.flipX = (dirToKael > 0f);

        if (hasEnteredRagePhase)
        {
            // FASE 2: COMBATE DE FURIA BIOMECÁNICA (50% -> 0% HP)
            if (distToKael > attackRange)
            {
                if (distToKael <= 5.5f && Time.time >= nextRocketLungeTime)
                {
                    StartCoroutine(VortexOmegaRocketPunchLungeRoutine());
                }
                else if (distToKael <= 7.5f && Time.time >= nextLaserTime)
                {
                    StartCoroutine(VortexOmegaChestLaserRoutine(true));
                }
                else
                {
                    transform.Translate(new Vector2(dirToKael, 0f) * (moveSpeed * Time.deltaTime), Space.World);
                }
            }
            else
            {
                if (Time.time >= nextAttackTime)
                {
                    attackCounter++;
                    if (attackCounter % 3 == 0)
                    {
                        StartCoroutine(VortexOmegaRocketPunchLungeRoutine());
                    }
                    else
                    {
                        StartCoroutine(VortexOmegaEnragedDoubleSlashRoutine());
                    }
                }
            }
        }
        else
        {
            // FASE 1: COMBATE TÁCTICO MECHA (100% -> 50% HP)
            if (distToKael > attackRange)
            {
                if (distToKael <= 7.5f && Time.time >= nextLaserTime)
                {
                    StartCoroutine(VortexOmegaChestLaserRoutine(false));
                }
                else
                {
                    transform.Translate(new Vector2(dirToKael, 0f) * (moveSpeed * Time.deltaTime), Space.World);
                }
            }
            else
            {
                if (Time.time >= nextAttackTime)
                {
                    attackCounter++;
                    if (attackCounter % 3 == 0)
                    {
                        StartCoroutine(TelegraphedHeavyAttack());
                    }
                    else
                    {
                        StartCoroutine(PlayVisibleAttackRoutine());
                    }
                }
            }
        }
    }

    private IEnumerator VortexOmegaChestLaserRoutine(bool isEnraged)
    {
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        float baseX = transform.position.x;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > baseX) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        Vector3 chestPos = transform.position + Vector3.up * 1.40f + Vector3.right * (dirToKael * 0.45f);

        if (CombatVFXManager.Instance != null)
        {
            string txt = isEnraged ? "⚠ ¡CAÑÓN LÁSER PESADO OVERCLOCK!" : "⚠ ¡CAÑÓN LÁSER DE PECHO OMEGA!";
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 2.1f, txt, new Color(1f, 0.15f, 0.45f), 22, 1.1f);
        }
        if (LevelAudioManager.Instance != null)
        {
            LevelAudioManager.Instance.PlayBossAlert();
        }

        int totalFrames = (customLaserFrames != null && customLaserFrames.Length >= 8) ? 8 : (customLaserFrames != null ? customLaserFrames.Length : 0);
        float chargeDelay = isEnraged ? 0.14f : 0.18f;
        for (int f = 0; f < 3; f++)
        {
            if (isDead || isInvulnerable) { isPlayingAttackAnim = false; yield break; }
            if (customLaserFrames != null && f < customLaserFrames.Length && customLaserFrames[f] != null)
                sr.sprite = customLaserFrames[f];

            if (CombatVFXManager.Instance != null)
                CombatVFXManager.Instance.SpawnElectricArc(chestPos, new Color(1f, 0.20f, 0.55f));

            yield return new WaitForSeconds(chargeDelay);
        }

        float beamDuration = isEnraged ? 0.60f : 0.50f;
        float beamLength = 8.5f;
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnBossLaserBeam(chestPos, dirToKael * beamLength, beamDuration);
            CombatVFXManager.Instance.ShakeCamera(0.38f, 0.22f);
            CombatVFXManager.Instance.TriggerScreenFlash(new Color(1f, 0.15f, 0.45f), 0.42f);
        }
        if (LevelAudioManager.Instance != null)
        {
            LevelAudioManager.Instance.PlayUltimateLaser();
        }

        float minX = Mathf.Min(chestPos.x, chestPos.x + dirToKael * beamLength);
        float maxX = Mathf.Max(chestPos.x, chestPos.x + dirToKael * beamLength);
        if (kaelTarget != null)
        {
            float kx = kaelTarget.position.x;
            float ky = kaelTarget.position.y;
            if (kx >= minX && kx <= maxX && ky >= chestPos.y - 1.6f && ky <= chestPos.y + 0.8f)
            {
                float laserDmg = damage * (isEnraged ? 1.45f : 1.20f);
                if (PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.TakeDamage(laserDmg, this);
                }
                if (CombatVFXManager.Instance != null)
                {
                    CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.6f, true, 8);
                }
            }
        }

        for (int f = 3; f <= 5; f++)
        {
            if (isDead) { isPlayingAttackAnim = false; yield break; }
            if (customLaserFrames != null && f < customLaserFrames.Length && customLaserFrames[f] != null)
                sr.sprite = customLaserFrames[f];
            yield return new WaitForSeconds(0.14f);
        }

        for (int f = 6; f < totalFrames; f++)
        {
            if (isDead) { isPlayingAttackAnim = false; yield break; }
            if (customLaserFrames != null && f < customLaserFrames.Length && customLaserFrames[f] != null)
                sr.sprite = customLaserFrames[f];
            yield return new WaitForSeconds(0.12f);
        }

        transform.position = new Vector3(baseX, baseGroundY, transform.position.z);
        nextLaserTime = Time.time + (isEnraged ? 3.2f : 4.6f);
        nextAttackTime = Time.time + (0.55f / attackRate);
        isPlayingAttackAnim = false;
    }

    private IEnumerator VortexOmegaPhaseTransitionRoutine()
    {
        hasEnteredRagePhase = true;
        isInvulnerable = true;
        isPlayingAttackAnim = true;
        isChargingHeavy = false;
        stunTimer = 0f;

        float baseX = transform.position.x;
        transform.position = new Vector3(baseX, baseGroundY, transform.position.z);

        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayBossAlert();

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.TriggerBossWarning(
                "⚠ ¡SOBRECARGA DEL NÚCLEO: FASE 2 MECHA BERSERKER! ⚠",
                "VORTEX-OMEGA DESATA SU PROTOCOLO DE EXTERMINIO OVERCLOCK",
                3.2f
            );
            CombatVFXManager.Instance.TriggerScreenFlash(new Color(1f, 0.15f, 0.45f), 0.85f);
            CombatVFXManager.Instance.ShakeCamera(0.45f, 0.35f);
        }

        int totalFrames = (customPhaseTransFrames != null && customPhaseTransFrames.Length >= 8) ? 8 : (customPhaseTransFrames != null ? customPhaseTransFrames.Length : 0);

        // ETAPA 1: COMPRESIÓN Y SOBRECARGA DEL REACTOR (Cuadros 0..2, ~0.9s)
        for (int f = 0; f < 3; f++)
        {
            if (isDead) yield break;
            if (customPhaseTransFrames != null && f < customPhaseTransFrames.Length && customPhaseTransFrames[f] != null)
                sr.sprite = customPhaseTransFrames[f];

            if (sr != null)
                sr.color = (f % 2 == 0) ? new Color(1f, 0.5f, 0.2f) : new Color(0.2f, 0.95f, 1f);

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnElectricArc(transform.position + Vector3.up * UnityEngine.Random.Range(0.6f, 1.8f), new Color(1f, 0.3f, 0.1f));
                CombatVFXManager.Instance.ShakeCamera(0.15f, 0.10f);
            }
            yield return new WaitForSeconds(0.30f);
        }

        // ETAPA 2: ESTALLIDO RADIAL Y APERTURA DE VÁLVULAS (Cuadro 3..5, ~1.2s)
        for (int f = 3; f < 6; f++)
        {
            if (isDead) yield break;
            if (customPhaseTransFrames != null && f < customPhaseTransFrames.Length && customPhaseTransFrames[f] != null)
                sr.sprite = customPhaseTransFrames[f];

            if (sr != null)
                sr.color = (f == 4) ? Color.white : new Color(1f, 0.2f, 0.5f);

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 1.0f, new Color(1.00f, 0.15f, 0.45f), 5.2f);
                CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 1.0f, new Color(0.15f, 0.95f, 1.00f), 3.6f);
                CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 1.0f, new Color(1.00f, 0.85f, 0.20f), 2.2f);
                CombatVFXManager.Instance.SpawnEnergyPillar(transform.position + Vector3.left * 2.2f, new Color(1f, 0.20f, 0.55f));
                CombatVFXManager.Instance.SpawnEnergyPillar(transform.position + Vector3.right * 2.2f, new Color(0f, 0.95f, 1f));
                CombatVFXManager.Instance.SpawnHitSparks(transform.position + Vector3.up * 1.2f, true, 20);
                CombatVFXManager.Instance.ShakeCamera(0.65f, 0.35f);
                CombatVFXManager.Instance.TriggerScreenFlash(new Color(1f, 0.2f, 0.5f), 0.70f);
            }
            yield return new WaitForSeconds(0.40f);
        }

        // ETAPA 3: RUGIDO BERSERKER Y ACTIVACIÓN DE POSTQUEMADORES (Cuadro 6..7, ~0.8s)
        for (int f = 6; f < totalFrames; f++)
        {
            if (isDead) yield break;
            if (customPhaseTransFrames != null && f < customPhaseTransFrames.Length && customPhaseTransFrames[f] != null)
                sr.sprite = customPhaseTransFrames[f];

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnElectricArc(transform.position + Vector3.up * 1.4f, new Color(1f, 0.1f, 0.4f));
            }
            yield return new WaitForSeconds(0.35f);
        }

        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.SpawnVortexOmegaEscorts(transform.position);
        }

        moveSpeed = 2.00f; // Furia biomecánica en Fase 2
        attackRate = 1.35f;
        damage = 32f;
        originalColor = new Color(1f, 0.70f, 0.85f, 1f);
        if (sr != null) sr.color = originalColor;

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 2.2f,
                "¡PROTOCOLO OVERCLOCK INICIADO!",
                new Color(1f, 0.2f, 0.4f),
                22,
                1.2f
            );
        }

        yield return new WaitForSeconds(0.40f);

        isInvulnerable = false;
        isPlayingAttackAnim = false;
        nextAttackTime = Time.time + 0.35f;
        nextLaserTime = Time.time + 2.5f;
        nextRocketLungeTime = Time.time + 3.0f;
    }

    private IEnumerator VortexOmegaEnragedDoubleSlashRoutine()
    {
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        float baseX = transform.position.x;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > baseX) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        int totalFrames = (customEnrageAttackFrames != null && customEnrageAttackFrames.Length >= 8) ? 8 : (customEnrageAttackFrames != null ? customEnrageAttackFrames.Length : 0);
        float lungeMax = 0.58f;

        for (int f = 0; f < totalFrames; f++)
        {
            if (isDead || isStunned)
            {
                isPlayingAttackAnim = false;
                yield break;
            }

            if (customEnrageAttackFrames != null && f < customEnrageAttackFrames.Length && customEnrageAttackFrames[f] != null)
                sr.sprite = customEnrageAttackFrames[f];

            float curve = Mathf.Sin((f / (float)Mathf.Max(1, totalFrames - 1)) * Mathf.PI);
            transform.position = new Vector3(baseX + dirToKael * lungeMax * curve, baseGroundY, transform.position.z);

            if (f == 2)
            {
                if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayEnemyAttack();
                bool inReach = (kaelTarget == null) || (Mathf.Abs(transform.position.x - kaelTarget.position.x) <= attackRange + 0.80f);
                if (inReach && PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.TakeDamage(damage * 0.75f, this);
                }
                if (inReach && CombatVFXManager.Instance != null && kaelTarget != null)
                {
                    CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.6f, false, 5);
                }
            }
            else if (f == 5)
            {
                if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayEnemyAttack();
                bool inReach = (kaelTarget == null) || (Mathf.Abs(transform.position.x - kaelTarget.position.x) <= attackRange + 0.95f);
                if (inReach && PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.TakeDamage(damage * 0.85f, this);
                }
                if (inReach && CombatVFXManager.Instance != null && kaelTarget != null)
                {
                    CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.6f, true, 7);
                    CombatVFXManager.Instance.ShakeCamera(0.22f, 0.14f);
                }
            }

            yield return new WaitForSeconds(0.13f);
        }

        transform.position = new Vector3(baseX, baseGroundY, transform.position.z);
        nextAttackTime = Time.time + (0.75f / attackRate);
        isPlayingAttackAnim = false;
    }

    private IEnumerator VortexOmegaRocketPunchLungeRoutine()
    {
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        float baseX = transform.position.x;
        float dirToKael = (kaelTarget != null && kaelTarget.position.x > baseX) ? 1f : -1f;
        if (sr != null) sr.flipX = (dirToKael > 0f);

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 2.1f,
                "⚠ ¡EMBESTIDA PUÑO COHETE!",
                new Color(1f, 0.40f, 0.12f),
                22,
                0.9f
            );
        }
        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayBossAlert();

        float elapsed = 0f;
        while (elapsed < 0.45f)
        {
            if (isDead || isStunned) { isPlayingAttackAnim = false; yield break; }
            if (sr != null)
                sr.color = (Mathf.FloorToInt(elapsed * 14f) % 2 == 0) ? new Color(1f, 0.45f, 0.15f) : originalColor;

            if (customEnrageAttackFrames != null && customEnrageAttackFrames.Length > 1 && customEnrageAttackFrames[1] != null)
                sr.sprite = customEnrageAttackFrames[1];

            if (CombatVFXManager.Instance != null)
                CombatVFXManager.Instance.SpawnElectricArc(transform.position + Vector3.up * 1.0f - Vector3.right * (dirToKael * 0.7f), new Color(0f, 0.95f, 1f));

            elapsed += Time.deltaTime;
            yield return null;
        }
        if (sr != null) sr.color = originalColor;

        float lungeDist = 2.25f;
        float lungeDuration = 0.22f;
        float lt = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(baseX + dirToKael * lungeDist, baseGroundY, transform.position.z);

        if (customEnrageAttackFrames != null && customEnrageAttackFrames.Length > 3 && customEnrageAttackFrames[3] != null)
            sr.sprite = customEnrageAttackFrames[3];

        while (lt < lungeDuration)
        {
            if (isDead) { isPlayingAttackAnim = false; yield break; }
            float p = lt / lungeDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, p);
            lt += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        if (customEnrageAttackFrames != null && customEnrageAttackFrames.Length > 4 && customEnrageAttackFrames[4] != null)
            sr.sprite = customEnrageAttackFrames[4];

        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayEnemyAttack();

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.ShakeCamera(0.42f, 0.26f);
            CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 0.8f, new Color(1f, 0.35f, 0.15f), 3.4f);
        }

        bool inImpactReach = (kaelTarget == null) || (Mathf.Abs(transform.position.x - kaelTarget.position.x) <= attackRange + 1.25f);
        if (inImpactReach && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.TakeDamage(damage * 1.85f, this);
            if (CombatVFXManager.Instance != null && kaelTarget != null)
            {
                CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.6f, true, 10);
            }
        }

        yield return new WaitForSeconds(0.35f);

        nextAttackTime = Time.time + (0.75f / attackRate);
        nextRocketLungeTime = Time.time + 4.2f;
        isPlayingAttackAnim = false;
    }

    void LateUpdate()
    {
        if (!useCustomSprites || sr == null) return;

        if (isDead)
        {
            if (isBoss) return;
            transform.localRotation = Quaternion.identity;
            dieAnimTimer += Time.deltaTime * 8.5f;
            if (customDieFrames != null && customDieFrames.Length > 0)
            {
                int idx = Mathf.Clamp(Mathf.FloorToInt(dieAnimTimer), 0, customDieFrames.Length - 1);
                if (customDieFrames[idx] != null) sr.sprite = customDieFrames[idx];
            }
            return;
        }

        if (isPlayingAttackAnim || isChargingHeavy) return;

        transform.localRotation = Quaternion.identity;
        float distToKael = (kaelTarget != null) ? Mathf.Abs(transform.position.x - kaelTarget.position.x) : 99f;
        // En sincronía con la decisión de Update(): si está en rango de ataque se detiene y adopta postura firme
        bool isMoving = (distToKael > attackRange);

        if (isMoving)
        {
            float animSpeed = 8.5f;
            if (visualType == "VortexOmega")
            {
                // Marcha mecha imponente y deliberada, sincronizada con su velocidad de desplazamiento
                animSpeed = hasEnteredRagePhase ? 4.2f : 3.2f;
            }
            else if (visualType == "BioHulk")
            {
                // Pasos pesados y firmes de coloso acorazado
                animSpeed = 4.2f;
            }
            else if (visualType == "HiveInquisitor")
            {
                // Flotación psiónica suave
                animSpeed = 4.0f;
            }
            else if (visualType == "BioStalker")
            {
                animSpeed = 9.0f;
            }
            else if (visualType == "BioLasher")
            {
                animSpeed = 5.5f;
            }

            walkAnimTimer += Time.deltaTime * animSpeed;
            if (customWalkFrames != null && customWalkFrames.Length > 0)
            {
                int frame = Mathf.FloorToInt(walkAnimTimer) % customWalkFrames.Length;
                if (customWalkFrames[frame] != null) sr.sprite = customWalkFrames[frame];
            }
        }
        else
        {
            // En rango de ataque esperando cooldown: postura firme de guardia sin correr en el sitio
            walkAnimTimer = 0f;
            if (customWalkFrames != null && customWalkFrames.Length > 0 && customWalkFrames[0] != null)
            {
                sr.sprite = customWalkFrames[0];
            }
        }

        // Levitación sinusoidal para Archon, BioLasher y HiveInquisitor
        bool isLevitator = (visualType == "Archon" || visualType == "BioLasher" || visualType == "HiveInquisitor");
        if (isLevitator)
        {
            float amp = (visualType == "BioLasher") ? 0.22f : 0.14f;
            float hoverY = Mathf.Sin(Time.time * 3.5f) * amp + amp;
            transform.position = new Vector3(transform.position.x, baseGroundY + hoverY, transform.position.z);
        }
        else
        {
            transform.position = new Vector3(transform.position.x, baseGroundY, transform.position.z);
        }
    }

    IEnumerator TelegraphedHeavyAttack()
    {
        isChargingHeavy = true;
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;

        if (CombatVFXManager.Instance != null)
        {
            string warnTxt = "⚠ ¡PUÑO DEMOLEDOR!";
            if (visualType == "HiveInquisitor") warnTxt = "⚠ ¡PULSO PSIÓNICO VORST!";
            else if (visualType == "VortexOmega") warnTxt = "⚠ ¡IMPACTO SÍSMICO DE CADENAS DE ESTASIS!";
            else if (visualType == "BioHulk") warnTxt = "⚠ ¡APLASTAMIENTO DE CRISÁLIDA!";
            else if (isBoss) warnTxt = "⚠ ¡DESCARGA PSIÓNICA!";

            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.5f, warnTxt, new Color(1f, 0.55f, 0.1f), 19, 1.0f);
        }

        float elapsed = 0f;
        while (elapsed < 0.9f)
        {
            if (isDead || isStunned)
            {
                if (sr != null) sr.color = originalColor;
                isChargingHeavy = false;
                isPlayingAttackAnim = false;
                yield break;
            }
            if (sr != null)
                sr.color = (Mathf.FloorToInt(elapsed * 12f) % 2 == 0) ? new Color(1f, 0.52f, 0.12f) : originalColor;
            if (customAttackFrames != null && customAttackFrames.Length > 0)
                sr.sprite = customAttackFrames[0];
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (sr != null) sr.color = originalColor;

        if (!isDead && !isStunned)
        {
            float baseX = transform.position.x;
            float dirToKael = (kaelTarget != null && kaelTarget.position.x > baseX) ? 1f : -1f;
            if (customAttackFrames != null && customAttackFrames.Length > 3)
                sr.sprite = customAttackFrames[3];
            transform.position = new Vector3(baseX + dirToKael * 0.45f, baseGroundY, transform.position.z);

            if (LevelAudioManager.Instance != null)
                LevelAudioManager.Instance.PlayEnemyAttack();

            // Efectos especiales por jefe del Nivel 3
            if (CombatVFXManager.Instance != null)
            {
                if (visualType == "HiveInquisitor")
                {
                    CombatVFXManager.Instance.TriggerScreenFlash(new Color(0.75f, 0.18f, 1f), 0.36f);
                    CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 0.9f, new Color(0.85f, 0.25f, 1f), 3.1f);
                }
                else if (visualType == "VortexOmega")
                {
                    // Levanta 3 pilares de energía en el suelo hacia Kael
                    for (int p = 1; p <= 3; p++)
                    {
                        Vector3 pillarPos = new Vector3(baseX + dirToKael * (p * 1.65f), baseGroundY - 0.1f, 0f);
                        CombatVFXManager.Instance.SpawnEnergyPillar(pillarPos, (p % 2 == 0) ? new Color(0f, 0.95f, 1f) : new Color(1f, 0.20f, 0.55f));
                    }
                }
                CombatVFXManager.Instance.ShakeCamera(0.26f, 0.16f);
            }

            bool inHeavyReach = (kaelTarget == null) || (Mathf.Abs(transform.position.x - kaelTarget.position.x) <= attackRange + 1.15f);
            if (inHeavyReach && PlayerStats.Instance != null)
                PlayerStats.Instance.TakeDamage(damage * 2.0f, this);

            yield return new WaitForSeconds(0.28f);
            if (!isDead) transform.position = new Vector3(baseX, baseGroundY, transform.position.z);
        }

        nextAttackTime = Time.time + (1.1f / attackRate);
        isChargingHeavy = false;
        isPlayingAttackAnim = false;
    }

    public void TakeDamage(float dmg, bool isCrit = false, float knockback = 0f)
    {
        if (isDead || isInvulnerable) return;

        // BLOQUE 5 (Issue #23 & Milestone M3): Transición de Fase 2 de VORTEX-OMEGA al 50% de HP
        if (visualType == "VortexOmega" && !hasEnteredRagePhase && (currentHP - dmg) <= (maxHP * 0.50f))
        {
            currentHP = maxHP * 0.50f;
            if (sr != null && !isChargingHeavy) StartCoroutine(HitFlash());
            StartCoroutine(VortexOmegaPhaseTransitionRoutine());
            return;
        }

        currentHP = Mathf.Max(0f, currentHP - dmg);
        if (sr != null && !isChargingHeavy) StartCoroutine(HitFlash());

        if (knockback > 0f && !isPlayingAttackAnim)
        {
            float resist = (isBoss || isMiniBoss) ? 0.25f : ((visualType == "BioHulk") ? 0.05f : 0.7f);
            float pushDir = (kaelTarget != null && transform.position.x < kaelTarget.position.x) ? -1f : 1f;
            transform.Translate(Vector2.right * (pushDir * knockback * resist), Space.World);
        }

        if (CombatVFXManager.Instance != null)
        {
            int roundedDmg = Mathf.RoundToInt(dmg);
            string txt = isCrit ? $"{roundedDmg}!" : $"{roundedDmg}";
            Color col = isCrit ? new Color(1f, 0.60f, 0.1f) : Color.white;
            int size = isCrit ? 23 : 16;
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 0.9f, txt, col, size);
        }

        if (currentHP <= 0f)
        {
            if (isBoss)
                StartCoroutine(BossDramaticDeathRoutine());
            else
                DieNormal();
        }
    }

    void DieNormal()
    {
        isDead = true;
        dieAnimTimer = 0f;
        transform.localRotation = Quaternion.identity;
        transform.position = new Vector3(transform.position.x, baseGroundY, transform.position.z);

        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayEnemyDie();

        int finalGold = goldReward;
        if (PlayerStats.Instance != null)
        {
            finalGold = Mathf.RoundToInt(goldReward * PlayerStats.Instance.goldMultiplier);
            PlayerStats.Instance.AddRewards(goldReward, expReward);
        }

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnLootDrop(transform.position, finalGold, expReward, isMiniBoss);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        OnEnemyDied?.Invoke();

        if (anim != null && !useCustomSprites)
        {
            anim.SetTrigger("Die");
            Destroy(gameObject, 0.5f);
        }
        else
        {
            Destroy(gameObject, 0.95f);
        }
    }

    IEnumerator BossDramaticDeathRoutine()
    {
        isDead = true;
        dieAnimTimer = 0f;
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (anim != null && !useCustomSprites) anim.SetBool("IsAttacking", false);

        if (visualType == "VortexOmega")
        {
            // Catastrophic Core Meltdown Routine for VortexOmega
            Time.timeScale = 0.28f;
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.TriggerScreenFlash(Color.white, 0.85f);
                CombatVFXManager.Instance.ShakeCamera(2.5f, 0.28f);
                CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 2.0f, "☠ ¡COLAPSO CATASTRÓFICO DE NÚCLEO! ☠", new Color(1f, 0.2f, 0.35f), 26, 2.2f);
            }
            if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();

            Vector3 basePos = new Vector3(transform.position.x, baseGroundY, transform.position.z);
            float facingDir = (sr != null && sr.flipX) ? 1f : -1f;

            // 4 sequential localized detonations across right shoulder, left vent, core reactor blowout, and ground collapse mapped to customDieFrames[0..4]
            // Burst 1: Right Shoulder / Upper Arm
            if (customDieFrames != null && customDieFrames.Length > 1 && customDieFrames[1] != null)
                sr.sprite = customDieFrames[1];
            Vector3 burst1 = basePos + new Vector3(facingDir * 0.5f, 1.3f, 0f);
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnBossExplosionBurst(burst1);
                CombatVFXManager.Instance.SpawnShockwaveRing(burst1, Color.cyan, 2.2f);
                CombatVFXManager.Instance.ShakeCamera(0.35f, 0.16f);
            }
            yield return new WaitForSecondsRealtime(0.45f);

            // Burst 2: Left Exhaust Flank
            if (customDieFrames != null && customDieFrames.Length > 2 && customDieFrames[2] != null)
                sr.sprite = customDieFrames[2];
            Vector3 burst2 = basePos + new Vector3(-facingDir * 0.45f, 0.85f, 0f);
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnBossExplosionBurst(burst2);
                CombatVFXManager.Instance.SpawnShockwaveRing(burst2, new Color(1f, 0.2f, 0.55f), 2.6f);
                CombatVFXManager.Instance.SpawnElectricArc(burst2, new Color(0.2f, 0.95f, 1f));
                CombatVFXManager.Instance.ShakeCamera(0.40f, 0.20f);
            }
            yield return new WaitForSecondsRealtime(0.45f);

            // Burst 3: Central Torso Core Reactor Blowout
            if (customDieFrames != null && customDieFrames.Length > 3 && customDieFrames[3] != null)
                sr.sprite = customDieFrames[3];
            Vector3 burst3 = basePos + new Vector3(0f, 1.05f, 0f);
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.TriggerScreenFlash(new Color(1f, 0.35f, 0.1f), 0.60f);
                CombatVFXManager.Instance.SpawnBossExplosionBurst(burst3);
                CombatVFXManager.Instance.SpawnHitSparks(burst3, true, 18);
                CombatVFXManager.Instance.ShakeCamera(0.50f, 0.25f);
            }
            yield return new WaitForSecondsRealtime(0.45f);

            // Burst 4: Ground Impact & Full Chassis Collapse
            if (customDieFrames != null && customDieFrames.Length > 4 && customDieFrames[4] != null)
                sr.sprite = customDieFrames[4];
            Vector3 burst4 = basePos + Vector3.up * 0.4f;
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.TriggerScreenFlash(new Color(1f, 0.95f, 0.70f), 0.75f);
                CombatVFXManager.Instance.SpawnShockwaveRing(burst4, new Color(1f, 0.85f, 0.2f), 4.8f);
                CombatVFXManager.Instance.SpawnEnergyPillar(basePos + Vector3.left * 1.8f, new Color(1f, 0.2f, 0.55f));
                CombatVFXManager.Instance.SpawnEnergyPillar(basePos + Vector3.right * 1.8f, new Color(0f, 0.95f, 1f));
                CombatVFXManager.Instance.SpawnHitSparks(burst4, true, 24);
                CombatVFXManager.Instance.ShakeCamera(0.85f, 0.35f);
            }
            if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayEnemyDie();
            yield return new WaitForSecondsRealtime(0.50f);

            // Time restoration & permanent settle into customDieFrames[7] resting stably at floor baseline y = -2.95f
            Time.timeScale = 1.0f;
            if (customDieFrames != null && customDieFrames.Length > 7 && customDieFrames[7] != null)
            {
                sr.sprite = customDieFrames[7];
            }
            transform.position = basePos;
            if (sr != null) sr.color = new Color(0.45f, 0.40f, 0.42f, 0.95f); // Charred metal tint

            int finalGold = goldReward;
            if (PlayerStats.Instance != null)
            {
                finalGold = Mathf.RoundToInt(goldReward * PlayerStats.Instance.goldMultiplier);
                PlayerStats.Instance.AddRewards(goldReward, expReward);
            }
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnLootDrop(transform.position, finalGold, expReward, true);
                CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.8f, "★ VORTEX-OMEGA ANIQUILADO ★", new Color(1f, 0.85f, 0.2f), 26, 2.5f);
            }

            // Smoldering Wreckage Hold: subtle rising embers for 1.5 seconds
            float holdTime = 0f;
            while (holdTime < 1.5f)
            {
                if (CombatVFXManager.Instance != null && UnityEngine.Random.value > 0.4f)
                {
                    CombatVFXManager.Instance.SpawnElectricArc(basePos + new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), UnityEngine.Random.Range(0.2f, 0.6f), 0f), new Color(1f, 0.4f, 0.1f));
                }
                holdTime += 0.3f;
                yield return new WaitForSeconds(0.3f);
            }

            // Trigger completion without destroying the smoldering chassis
            OnEnemyDied?.Invoke();
            this.enabled = false;
            yield break;
        }

        // Generic boss death routine fallback (e.g. Level 2 Boss)
        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayEnemyDie();

        Time.timeScale = 0.35f;
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.ShakeCamera(1.4f, 0.20f);
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.5f, $"¡{enemyTitle.ToUpper()} ABATIDO!", Color.cyan, 26, 1.5f);
        }

        Vector3 genericBasePos = new Vector3(transform.position.x, baseGroundY, transform.position.z);

        for (int i = 0; i < 4; i++)
        {
            if (sr != null) sr.color = (i % 2 == 0) ? Color.white : new Color(1f, 0.2f, 0.3f);
            transform.position = genericBasePos + (Vector3)UnityEngine.Random.insideUnitCircle * 0.25f;

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnBossExplosionBurst(transform.position);
            }
            yield return new WaitForSecondsRealtime(0.32f);
        }

        Time.timeScale = 1f;
        transform.position = genericBasePos;
        if (sr != null) sr.color = originalColor;

        int genGold = goldReward;
        if (PlayerStats.Instance != null)
        {
            genGold = Mathf.RoundToInt(goldReward * PlayerStats.Instance.goldMultiplier);
            PlayerStats.Instance.AddRewards(goldReward, expReward);
        }

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnLootDrop(transform.position, genGold, expReward, true);
        }

        if (anim != null && !useCustomSprites)
        {
            anim.SetTrigger("Die");
        }

        yield return new WaitForSeconds(1.2f);
        OnEnemyDied?.Invoke();
        Destroy(gameObject);
    }

    IEnumerator HitFlash()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.07f);
        if (sr != null && !isDead && !isChargingHeavy) sr.color = originalColor;
    }

    void OnGUI()
    {
        if (isDead || Camera.main == null) return;
        GUI.matrix = Matrix4x4.identity;

        // Indicador flotante de Estado Aturdido (Stunned por Parry Perfecto - Bloque 1)
        if (stunTimer > 0f)
        {
            Vector3 stunWorld = transform.position + Vector3.up * (isBoss ? 2.4f : 1.55f);
            Vector3 stunScreen = Camera.main.WorldToScreenPoint(stunWorld);
            if (stunScreen.z > 0)
            {
                GUIStyle stunStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = PlayerStats.ScaleFont(15),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                stunStyle.normal.textColor = new Color(0.25f, 0.98f, 1f);
                float sy = Screen.height - stunScreen.y;
                GUI.Label(new Rect(stunScreen.x - 110f, sy - 16f, 220f, 32f), $"⚡ ATURDIDO ({stunTimer:F1}s) ⚡", stunStyle);
            }
        }

        if (isBoss || isMiniBoss)
        {
            if (PlayerStats.Instance != null &&
                (PlayerStats.Instance.isPaused ||
                 PlayerStats.Instance.isGameOver ||
                 PlayerStats.Instance.isLevelCompleted ||
                 PlayerStats.Instance.isChoosingDilemma ||
                 PlayerStats.Instance.isChoosingSkill ||
                 PlayerStats.Instance.isUpgradeTreeOpen))
                return;

            Rect bgBox = PlayerStats.ScaleRect(720f, 10f, 480f, 74f);
            Rect titleRect = PlayerStats.ScaleRect(732f, 14f, 456f, 26f);
            Rect barRect = PlayerStats.ScaleRect(742f, 44f, 436f, 24f);

            Texture2D bossPanel = PlayerStats.LoadResTex(isBoss ? "StoryUI/Panel_Cyber_Crimson" : "StoryUI/Panel_Cyber_Gold");
            if (bossPanel != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.98f);
                GUI.DrawTexture(bgBox, bossPanel, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.05f, 0.02f, 0.08f, 0.96f);
                GUI.DrawTexture(bgBox, Texture2D.whiteTexture);
            }

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = PlayerStats.ScaleFont(15);

            string bossTxt = hasEnteredRagePhase ? $"🔥 {enemyTitle.ToUpper()} [FURIA] 🔥" : $"☠ {enemyTitle.ToUpper()} ☠";
            titleStyle.normal.textColor = new Color(0f, 0f, 0f, 0.92f);
            GUI.Label(new Rect(titleRect.x + 1f, titleRect.y + 1f, titleRect.width, titleRect.height), bossTxt, titleStyle);
            titleStyle.normal.textColor = isBoss ? new Color(1f, 0.35f, 0.55f) : new Color(1f, 0.72f, 0.22f);
            GUI.Label(titleRect, bossTxt, titleStyle);

            GUI.color = new Color(0.18f, 0.04f, 0.09f, 0.96f);
            GUI.DrawTexture(barRect, Texture2D.whiteTexture);

            float bossFill = Mathf.Clamp01(currentHP / Mathf.Max(1f, maxHP));
            float fillW = Mathf.Round(barRect.width * bossFill);
            if (fillW > 0f)
            {
                GUI.color = isBoss ? new Color(0.95f, 0.15f, 0.35f) : new Color(0.95f, 0.52f, 0.12f);
                GUI.DrawTexture(new Rect(barRect.x, barRect.y, fillW, barRect.height), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, 0.24f);
                GUI.DrawTexture(new Rect(barRect.x, barRect.y, fillW, Mathf.Max(2f, barRect.height * 0.32f)), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            titleStyle.fontSize = PlayerStats.ScaleFont(13);
            string hpTxt = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)} HP";
            titleStyle.normal.textColor = new Color(0f, 0f, 0f, 0.92f);
            GUI.Label(new Rect(barRect.x + 1f, barRect.y + 1f, barRect.width, barRect.height), hpTxt, titleStyle);
            titleStyle.normal.textColor = Color.white;
            GUI.Label(barRect, hpTxt, titleStyle);
            return;
        }

        if (currentHP >= maxHP) return;

        Vector3 worldPos = transform.position + Vector3.up * 1.15f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        if (screenPos.z <= 0) return;

        float barW = 46f;
        float barH = 6f;
        float x = screenPos.x - (barW / 2f);
        float y = Screen.height - screenPos.y;

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(x - 1, y - 1, barW + 2, barH + 2), Texture2D.whiteTexture);

        GUI.color = new Color(0.25f, 0.05f, 0.1f);
        GUI.DrawTexture(new Rect(x, y, barW, barH), Texture2D.whiteTexture);

        float fill = Mathf.Clamp01(currentHP / maxHP);
        GUI.color = new Color(0.95f, 0.2f, 0.3f);
        GUI.DrawTexture(new Rect(x, y, barW * fill, barH), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
