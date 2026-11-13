using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class KaelAutoController : MonoBehaviour
{
    public static KaelAutoController Instance { get; private set; }

    [Header("Movimiento y Detección Manual")]
    public float moveSpeed = 4.2f;
    public float attackRange = 2.05f;
    public float aoeSplashRange = 3.10f;
    public float ultimateRange = 12.5f;
    public LayerMask enemyLayer;

    [Header("Ajuste de Tamaño por Animación (En Vivo)")]
    [Range(0.5f, 1.5f)] public float scaleIdle = 0.88f;
    [Range(0.5f, 1.5f)] public float scaleRun = 0.78f;
    [Range(0.5f, 1.5f)] public float scaleAttack = 1.00f;
    [Range(0.5f, 1.5f)] public float scaleUlt = 1.24f;

    [Header("Estado de Control Manual (En Vivo)")]
    public bool isFacingLeft = false;
    public bool isGuarding = false;
    public bool isMovingManual = false;
    public int currentCombo = 0;

    // --- BLOQUE 1 (Issue #16): Bloqueo Perfecto ("Parry Nexo") y Escudo Hexagonal ---
    public bool isBlocking => isGuarding;
    public float lastBlockStartTime = -99f;
    public const float PARRY_WINDOW_SECONDS = 0.35f;
    private bool wasGuardingLastFrame = false;
    private bool isExecutingRiposte = false;
    private float riposteSpriteTimer = 0f;
    private Sprite[] kaelBlockSprites = new Sprite[4];
    private Sprite[] kaelRiposteSprites = new Sprite[4];
    private bool extraSpritesLoaded = false;

    private struct GauntletSpriteSet
    {
        public Dictionary<string, Sprite> ByBaseName;
    }

    private readonly Dictionary<GauntletVariant, GauntletSpriteSet> gauntletSpriteSets =
        new Dictionary<GauntletVariant, GauntletSpriteSet>();
    private readonly Dictionary<Sprite, Sprite> variantSpriteToBase =
        new Dictionary<Sprite, Sprite>();
    private readonly HashSet<Sprite> knownVariantSprites =
        new HashSet<Sprite>();

    private static readonly string[] KAEL_ATLAS_FRAME_NAMES =
    {
        "Kael_Atlas_Pro_0", "Kael_Atlas_Pro_1", "Kael_Atlas_Pro_2", "Kael_Atlas_Pro_3",
        "Kael_Atlas_Pro_4", "Kael_Atlas_Pro_5", "Kael_Atlas_Pro_6", "Kael_Atlas_Pro_7", "Kael_Atlas_Pro_8", "Kael_Atlas_Pro_9",
        "Kael_Atlas_Pro_10", "Kael_Atlas_Pro_11", "Kael_Atlas_Pro_12", "Kael_Atlas_Pro_13",
        "Kael_Atlas_Pro_14", "Kael_Atlas_Pro_15", "Kael_Atlas_Pro_16", "Kael_Atlas_Pro_17",
        "Kael_Atlas_Pro_18", "Kael_Atlas_Pro_19", "Kael_Atlas_Pro_21", "Kael_Atlas_Pro_22", "Kael_Atlas_Pro_23",
        "Kael_Atlas_Pro_26", "Kael_Atlas_Pro_27", "Kael_Atlas_Pro_28", "Kael_Atlas_Pro_29", "Kael_Atlas_Pro_30", "Kael_Atlas_Pro_31"
    };

    private static readonly (string atlasName, string runAlias)[] KAEL_RUN_ALIASES =
    {
        ("Kael_Atlas_Pro_4", "Kael_Run_Strip_0"),
        ("Kael_Atlas_Pro_5", "Kael_Run_Strip_6"),
        ("Kael_Atlas_Pro_6", "Kael_Run_Strip_7"),
        ("Kael_Atlas_Pro_7", "Kael_Run_Strip_9"),
        ("Kael_Atlas_Pro_8", "Kael_Run_Strip_10"),
        ("Kael_Atlas_Pro_9", "Kael_Run_Strip_17")
    };

    private GameObject hexShieldObj;
    private SpriteRenderer hexShieldRenderer;

    // --- BLOQUE 2 (Issue #17) y BLOQUE 4 (Issue #22): Temporizadores de Afterimages y Auras ---
    private float afterimageTimer = 0f;
    private float overdriveArcTimer = 0f;
    private float moralAuraTimer = 0f;
    private float lastLifestealPopupTime = -10f;

    // Controles opcionales desde botones del HUD
    [HideInInspector] public bool hudMoveLeftHeld = false;
    [HideInInspector] public bool hudMoveRightHeld = false;
    [HideInInspector] public bool hudGuardHeld = false;

    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Color baseSpriteColor = Color.white;

    private float nextAttackTime = 0f;
    private float lastAttackTime = -99f;
    private float attackAnimEndTimer = 0f;
    private const float COMBO_WINDOW = 1.2f;

    private bool isCastingUlt = false;
    private Vector3 startPosition;
    private float maxReachedX;

    public bool IsCastingUltimate => isCastingUlt;
    public float ComboTimeRemaining => Mathf.Max(0f, (lastAttackTime + COMBO_WINDOW) - Time.time);

    void Awake()
    {
        Instance = this;
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) baseSpriteColor = spriteRenderer.color;

        if (transform.position.y < -3.1f)
        {
            transform.position = new Vector3(transform.position.x, -2.85f, transform.position.z);
        }
        startPosition = transform.position;
        maxReachedX = startPosition.x;

        CreateHexShieldObject();
        LoadExtraKaelSprites();
    }

    void Start()
    {
        if (PlayerStats.Instance != null && PlayerStats.Instance.hasPurchasedNexaDrone)
        {
            NexaDroneCompanion.EnsureExists(transform);
        }
    }

    private void LoadExtraKaelSprites()
    {
        EnsureExtraKaelSpritesLoaded();
    }

    public void EnsureExtraKaelSpritesLoaded()
    {
        if (extraSpritesLoaded) return;
        extraSpritesLoaded = true;

        for (int i = 0; i < 4; i++)
        {
            Texture2D bTex = Resources.Load<Texture2D>($"KaelExtra/Kael_Block_{i}");
            if (bTex != null)
            {
                kaelBlockSprites[i] = Sprite.Create(bTex, new Rect(0, 0, bTex.width, bTex.height), new Vector2(0.5f, 0.12f), 115f);
                kaelBlockSprites[i].name = $"Kael_Block_{i}";
            }
            Texture2D rTex = Resources.Load<Texture2D>($"KaelExtra/Kael_Riposte_{i}");
            if (rTex != null)
            {
                kaelRiposteSprites[i] = Sprite.Create(rTex, new Rect(0, 0, rTex.width, rTex.height), new Vector2(0.5f, 0.12f), 115f);
                kaelRiposteSprites[i].name = $"Kael_Riposte_{i}";
            }
        }

        LoadGauntletVariantSet(GauntletVariant.PyroReactor, "Pyro", 98.79f, 95.19f);
        LoadGauntletVariantSet(GauntletVariant.IgneousPlasma, "Gold", 100.0f, 96.45f);
        LoadGauntletVariantSet(GauntletVariant.VorstVoid, "Void", 101.82f, 98.54f);
    }

    private void LoadGauntletVariantSet(GauntletVariant variant, string folder, float mainPpu, float runPpu)
    {
        var map = new Dictionary<string, Sprite>(System.StringComparer.Ordinal);
        foreach (string frameName in KAEL_ATLAS_FRAME_NAMES)
        {
            Texture2D tex = Resources.Load<Texture2D>($"KaelGauntlets/{folder}/{frameName}");
            if (tex == null) continue;
            Sprite spr = CreateCrispVariantSprite(tex, new Vector2(0.5f, 0f), mainPpu, $"{frameName}_{folder}");
            map[frameName] = spr;
        }

        foreach (var (atlasName, runAlias) in KAEL_RUN_ALIASES)
        {
            Texture2D tex = Resources.Load<Texture2D>($"KaelGauntlets/{folder}/{atlasName}");
            if (tex == null) continue;
            Sprite runSpr = CreateCrispVariantSprite(tex, new Vector2(0.5f, 0f), runPpu, $"{runAlias}_{folder}");
            map[runAlias] = runSpr;
        }

        for (int i = 0; i < 4; i++)
        {
            Texture2D bTex = Resources.Load<Texture2D>($"KaelGauntlets/{folder}/Kael_Block_{i}");
            if (bTex != null)
            {
                Sprite bSpr = CreateCrispVariantSprite(bTex, new Vector2(0.5f, 0.12f), 115f, $"Kael_Block_{i}_{folder}");
                map[$"Kael_Block_{i}"] = bSpr;
                if (kaelBlockSprites[i] != null) variantSpriteToBase[bSpr] = kaelBlockSprites[i];
            }

            Texture2D rTex = Resources.Load<Texture2D>($"KaelGauntlets/{folder}/Kael_Riposte_{i}");
            if (rTex != null)
            {
                Sprite rSpr = CreateCrispVariantSprite(rTex, new Vector2(0.5f, 0.12f), 115f, $"Kael_Riposte_{i}_{folder}");
                map[$"Kael_Riposte_{i}"] = rSpr;
                if (kaelRiposteSprites[i] != null) variantSpriteToBase[rSpr] = kaelRiposteSprites[i];
            }
        }

        gauntletSpriteSets[variant] = new GauntletSpriteSet { ByBaseName = map };
    }

    private Sprite CreateCrispVariantSprite(Texture2D tex, Vector2 pivot, float ppu, string spriteName)
    {
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Sprite spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu, 0, SpriteMeshType.FullRect);
        spr.name = spriteName;
        knownVariantSprites.Add(spr);
        return spr;
    }

    /// <summary>
    /// Devuelve el sprite correspondiente al guantelete equipado (Pyro/Plasma/Vorst) preservando el frame base
    /// para revertir o alternar limpiamente incluso cuando el juego está pausado en tienda.
    /// </summary>
    public Sprite ResolveGauntletVariantSprite(Sprite currentSprite)
    {
        if (currentSprite == null) return null;
        EnsureExtraKaelSpritesLoaded();

        Sprite baseSprite = currentSprite;
        if (knownVariantSprites.Contains(currentSprite))
        {
            if (variantSpriteToBase.TryGetValue(currentSprite, out Sprite mappedBase) && mappedBase != null)
            {
                baseSprite = mappedBase;
            }
            else
            {
                return currentSprite;
            }
        }

        GauntletVariant equipped = (PlayerStats.Instance != null)
            ? PlayerStats.Instance.equippedGauntlet
            : GauntletVariant.PrototypeCyan;

        if (equipped == GauntletVariant.PrototypeCyan ||
            !gauntletSpriteSets.TryGetValue(equipped, out GauntletSpriteSet set) ||
            set.ByBaseName == null)
        {
            return baseSprite;
        }

        if (set.ByBaseName.TryGetValue(baseSprite.name, out Sprite variantSprite) && variantSprite != null)
        {
            variantSpriteToBase[variantSprite] = baseSprite;
            return variantSprite;
        }

        return baseSprite;
    }

    public void RefreshEquippedGauntletVisuals()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        Sprite resolved = ResolveGauntletVariantSprite(spriteRenderer.sprite);
        if (resolved != null && spriteRenderer.sprite != resolved)
        {
            spriteRenderer.sprite = resolved;
        }
    }

    private void CreateHexShieldObject()
    {
        if (hexShieldObj != null) return;
        hexShieldObj = new GameObject("NexoHexShield_Hologram");
        hexShieldObj.transform.SetParent(transform, false);
        hexShieldObj.transform.localPosition = new Vector3(0.75f, 0.58f, 0f);

        hexShieldRenderer = hexShieldObj.AddComponent<SpriteRenderer>();
        hexShieldRenderer.sprite = Level3MonsterAssetGenerator.GetHexShieldSprite(false, false);
        hexShieldRenderer.sortingOrder = 22;
        hexShieldObj.SetActive(false);
    }

    public void ResetKaelPosition()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        isCastingUlt = false;
        isGuarding = false;
        wasGuardingLastFrame = false;
        isExecutingRiposte = false;
        isFacingLeft = false;
        isMovingManual = false;
        currentCombo = 0;
        lastAttackTime = -99f;
        attackAnimEndTimer = 0f;

        transform.position = startPosition;
        transform.localRotation = Quaternion.identity;
        maxReachedX = startPosition.x;

        if (hexShieldObj != null) hexShieldObj.SetActive(false);

        if (NexaDroneCompanion.Instance != null)
            NexaDroneCompanion.Instance.ResetCompanion();

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
            spriteRenderer.color = baseSpriteColor;
        }

        if (CombatVFXManager.Instance != null)
            CombatVFXManager.Instance.SetUltimateCinematic(false);

        if (anim != null)
        {
            anim.speed = 1f;
            anim.ResetTrigger("Ultimate");
            anim.SetInteger("ComboStep", 0);
            anim.SetBool("IsRunning", false);
        }
    }

    /// <summary>
    /// BLOQUE 1 (Issue #16): Verifica si Kael está bloqueando dentro de la ventana de 0.35s de Parry Perfecto.
    /// </summary>
    public bool IsWithinPerfectParryWindow()
    {
        return isGuarding && (Time.unscaledTime - lastBlockStartTime <= PARRY_WINDOW_SECONDS);
    }

    /// <summary>
    /// BLOQUE 1 (Issue #16): Ejecuta la Ralentización Temporal (Bullet-Time) y el Contragolpe Automático (Riposte).
    /// </summary>
    public void ExecuteParryRiposte(EnemyDummy attacker)
    {
        if (!isExecutingRiposte)
        {
            StartCoroutine(ParryRiposteRoutine(attacker));
        }
    }

    private IEnumerator ParryRiposteRoutine(EnemyDummy attacker)
    {
        isExecutingRiposte = true;

        bool isPureza = (PlayerStats.Instance != null && PlayerStats.Instance.compassionPoints > PlayerStats.Instance.rigorPoints);
        bool isCorrupcion = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);

        Color primaryParryCol = GetEffectiveGauntletColor(
            isCorrupcion ? new Color(1f, 0.18f, 0.45f) : (isPureza ? new Color(1f, 0.90f, 0.32f) : new Color(0.25f, 0.98f, 1f)),
            useSecondary: false);
        Color secondaryParryCol = GetEffectiveGauntletColor(
            isCorrupcion ? new Color(0.68f, 0.12f, 1f) : (isPureza ? new Color(0.25f, 0.98f, 1f) : Color.white),
            useSecondary: true);

        float dirSign = isFacingLeft ? -1f : 1f;
        Vector3 shieldPos = transform.position + new Vector3(dirSign * 0.78f, 0.62f, 0f);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayParryClang(true);

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnParryFlashAndShockwave(shieldPos, primaryParryCol, secondaryParryCol);
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 1.55f,
                "⚡ ¡PARRY PERFECTO! +20% ULT",
                primaryParryCol,
                22,
                1.1f
            );
            CombatVFXManager.Instance.RegisterComboHit();
        }

        // Recargar +20% de barra de Ultimate y curar +10% si está en Pureza Nexo (Bloque 4)
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddUltCharge(20f);
            if (isPureza)
            {
                PlayerStats.Instance.HealPercent(0.10f);
                if (CombatVFXManager.Instance != null)
                    CombatVFXManager.Instance.SpawnHealingParticles(transform.position + Vector3.up * 0.4f);
            }
        }

        // Bullet-Time (Time.timeScale = 0.22f durante 0.20s reales)
        Time.timeScale = 0.22f;
        yield return new WaitForSecondsRealtime(0.20f);
        if (PlayerStats.Instance == null || !PlayerStats.Instance.IsAnyBlockingModalOpen)
        {
            Time.timeScale = 1f;
        }

        // Animación y ejecución del Contragolpe Automático (Riposte) con 180% del daño base
        if (anim != null)
        {
            anim.SetBool("IsRunning", false);
            anim.SetInteger("ComboStep", 3);
            anim.Play("Kael_Atk3", 0, 0f);
        }
        attackAnimEndTimer = 0.34f;
        riposteSpriteTimer = 0.36f;

        if (CombatVFXManager.Instance != null && spriteRenderer != null)
        {
            CombatVFXManager.Instance.SpawnAfterimageGhost(
                ResolveGauntletVariantSprite(spriteRenderer.sprite),
                transform.position,
                transform.localScale,
                isFacingLeft,
                new Color(primaryParryCol.r, primaryParryCol.g, primaryParryCol.b, 0.75f)
            );
        }

        float baseDmg = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackDamage : 12f;
        float riposteDmg = baseDmg * 1.80f;

        // Golpear, empujar y aturdir durante 1.8s al atacante y enemigos frontales cercanos
        HashSet<EnemyDummy> struck = new HashSet<EnemyDummy>();
        if (attacker != null)
        {
            struck.Add(attacker);
            DealGauntletHitDamage(attacker, riposteDmg, isCrit: true, knockback: 2.2f, isCorrupcion: isCorrupcion);
            attacker.ApplyStun(1.8f);
        }

        Vector2 facingDir = isFacingLeft ? Vector2.left : Vector2.right;
        List<EnemyDummy> frontEnemies = FindEnemiesInAttackArc(attackRange + 0.65f, facingDir);
        foreach (EnemyDummy e in frontEnemies)
        {
            if (e != null && !struck.Contains(e))
            {
                struck.Add(e);
                DealGauntletHitDamage(e, riposteDmg * 0.85f, isCrit: true, knockback: 1.4f, isCorrupcion: isCorrupcion);
                e.ApplyStun(1.8f);
            }
        }

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.ShakeCamera(0.18f, 0.14f);
        }

        // Permitir encadenar otro Parry inmediato sin esperar a soltar y volver a presionar S
        lastBlockStartTime = Time.unscaledTime;
        isExecutingRiposte = false;
    }

    void Update()
    {
        // Atajos globales para abrir/cerrar el Árbol de Mejoras (TAB/M), Tienda Arsenal (B/T) o Pausa (ESC)
        if (PlayerStats.Instance != null)
        {
            if (WasArsenalShopShortcutPressed())
            {
                PlayerStats.Instance.ToggleArsenalShopModal();
                return;
            }
            if (WasUpgradeTreeShortcutPressed())
            {
                PlayerStats.Instance.ToggleUpgradeTreeModal();
                return;
            }
            if (WasEscapePressed())
            {
                if (PlayerStats.Instance.isUpgradeTreeOpen || PlayerStats.Instance.isChoosingSkill || PlayerStats.Instance.isArsenalShopOpen)
                {
                    PlayerStats.Instance.CloseAllUpgradeModals();
                    return;
                }
                else if (!PlayerStats.Instance.isGameOver && !PlayerStats.Instance.isLevelCompleted && !PlayerStats.Instance.isChoosingDilemma)
                {
                    PlayerStats.Instance.TogglePause();
                    return;
                }
            }
        }

        if (PlayerStats.Instance != null && PlayerStats.Instance.IsAnyBlockingModalOpen)
        {
            if (anim != null) anim.SetBool("IsRunning", false);
            return;
        }

        // Si estamos en la Fase 1 narrativa del Puente Nivel 1 -> Nivel 2, pausar input de Kael
        if (BridgeLevel1To2Controller.Instance != null &&
            BridgeLevel1To2Controller.Instance.isBridgeActive &&
            !BridgeLevel1To2Controller.Instance.isInPlayablePhase)
        {
            if (anim != null) anim.SetBool("IsRunning", false);
            return;
        }

        if (isCastingUlt) return;

        bool isOverdrive = (PlayerStats.Instance != null && PlayerStats.Instance.isOverdriveActive);
        bool isPureza = (PlayerStats.Instance != null && PlayerStats.Instance.compassionPoints > PlayerStats.Instance.rigorPoints);
        bool isCorrupcion = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);

        // Efectos pasivos de Sobremarcha Nexo (Overdrive - Bloque 2) y Evolución Moral (Bloque 4)
        UpdateOverdriveAndMoralVisuals(isOverdrive, isPureza, isCorrupcion);

        if (attackAnimEndTimer > 0f)
        {
            attackAnimEndTimer -= Time.deltaTime;
            if (attackAnimEndTimer <= 0f && anim != null)
            {
                anim.SetInteger("ComboStep", 0);
            }
        }

        if (currentCombo > 0 && Time.time - lastAttackTime > COMBO_WINDOW && attackAnimEndTimer <= 0f)
        {
            currentCombo = 0;
        }

        // 1. HABILIDAD ULTIMATE MANUAL: Tecla K o Q
        if (WasUltimatePressed())
        {
            if (PlayerStats.Instance != null && PlayerStats.Instance.ultCharge >= 100f && !isCastingUlt)
            {
                TriggerUltimate();
                return;
            }
        }

        // 2. ATAQUE MANUAL (COMBO DE 3 GOLPES): Tecla J o Espacio
        if (WasAttackPressed())
        {
            TriggerManualAttack();
        }

        // 3. POSTURA DEFENSIVA / PARRY NEXO (BLOQUE 1): Mantener S o Flecha Abajo
        bool guardInput = IsGuardHeld() || hudGuardHeld;
        if (guardInput)
        {
            if (!wasGuardingLastFrame)
            {
                // Registrar el instante exacto en que se presionó S para la ventana de Parry Perfecto (0.35s)
                lastBlockStartTime = Time.unscaledTime;
            }
            wasGuardingLastFrame = true;
            isGuarding = true;
            isMovingManual = false;

            if (anim != null)
            {
                if (attackAnimEndTimer <= 0f) anim.SetInteger("ComboStep", 0);
                anim.SetBool("IsRunning", false);
            }

            // Si tenemos los sprites pixel-art dedicados de Kael bloqueando con escudo (Kael_Block_0..3), usarlos sin inclinar ni duplicar escudo
            bool hasCustomBlockSprites = (kaelBlockSprites != null && kaelBlockSprites[0] != null);
            float dirSign = isFacingLeft ? -1f : 1f;
            transform.localRotation = hasCustomBlockSprites ? Quaternion.identity : Quaternion.Euler(0f, 0f, isFacingLeft ? 5.5f : -5.5f);

            if (hexShieldObj != null && hexShieldRenderer != null)
            {
                if (hasCustomBlockSprites)
                {
                    hexShieldObj.SetActive(false);
                }
                else
                {
                    hexShieldObj.SetActive(true);
                    hexShieldRenderer.sprite = Level3MonsterAssetGenerator.GetHexShieldSprite(isPureza, isCorrupcion);
                    hexShieldObj.transform.localPosition = new Vector3(dirSign * 0.76f, 0.58f, 0f);
                    float shieldBreath = 0.95f + Mathf.Sin(Time.unscaledTime * 9f) * 0.08f;
                    bool inParryWindow = IsWithinPerfectParryWindow();
                    if (inParryWindow) shieldBreath *= 1.14f;
                    hexShieldObj.transform.localScale = new Vector3(shieldBreath, shieldBreath, 1f);
                    hexShieldObj.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 3.5f) * 12f);
                }
            }

            if (spriteRenderer != null)
            {
                float pulse = 0.85f + Mathf.Sin(Time.unscaledTime * 12f) * 0.12f;
                if (isCorrupcion)
                    spriteRenderer.color = new Color(1f, 0.45f, 0.72f, pulse);
                else if (isPureza)
                    spriteRenderer.color = new Color(0.55f, 1.0f, 0.95f, pulse);
                else
                    spriteRenderer.color = new Color(0.45f, 0.94f, 1.0f, pulse);
            }
            return;
        }
        else if (isGuarding || wasGuardingLastFrame)
        {
            wasGuardingLastFrame = false;
            isGuarding = false;
            transform.localRotation = Quaternion.identity;
            if (hexShieldObj != null) hexShieldObj.SetActive(false);
            if (spriteRenderer != null) spriteRenderer.color = GetMoralBaseTint(isPureza, isCorrupcion);
        }

        // 4. MOVIMIENTO MANUAL: A / Flecha Izquierda y D / Flecha Derecha
        float moveDir = 0f;
        if (IsLeftHeld() || hudMoveLeftHeld) moveDir -= 1f;
        if (IsRightHeld() || hudMoveRightHeld) moveDir += 1f;

        bool lockedInAttackSwing = (attackAnimEndTimer > 0.16f);

        if (Mathf.Abs(moveDir) > 0.01f && !lockedInAttackSwing)
        {
            isMovingManual = true;
            isFacingLeft = (moveDir < 0f);

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = isFacingLeft;
            }

            float speedBonus = (PlayerStats.Instance != null) ? PlayerStats.Instance.moveSpeedMultiplier : 1.0f;
            // BLOQUE 2: +20% Velocidad de movimiento durante Sobremarcha Nexo (Overdrive)
            if (isOverdrive) speedBonus *= 1.20f;
            float effectiveSpeed = moveSpeed * speedBonus;

            Vector3 delta = (moveDir < 0f ? Vector3.left : Vector3.right) * (effectiveSpeed * Time.deltaTime);
            Vector3 nextPos = transform.position + delta;

            float minLeftX = Mathf.Max(startPosition.x - 4.5f, maxReachedX - 11.5f);
            if (nextPos.x < minLeftX) nextPos.x = minLeftX;

            transform.position = nextPos;
            if (transform.position.x > maxReachedX) maxReachedX = transform.position.x;

            if (anim != null)
            {
                if (attackAnimEndTimer <= 0f) anim.SetInteger("ComboStep", 0);
                anim.SetBool("IsRunning", true);
            }
        }
        else
        {
            isMovingManual = false;
            if (anim != null)
            {
                anim.SetBool("IsRunning", false);
            }
        }
    }

    private Color GetMoralBaseTint(bool isPureza, bool isCorrupcion)
    {
        if (PlayerStats.Instance != null && PlayerStats.Instance.equippedGauntlet != GauntletVariant.PrototypeCyan)
        {
            return baseSpriteColor;
        }
        if (isCorrupcion) return new Color(1f, 0.88f, 0.95f, 1f);
        if (isPureza) return new Color(0.92f, 1f, 1f, 1f);
        return baseSpriteColor;
    }

    /// <summary>
    /// Devuelve el color del Guantelete equipado (prioritario si no es el Prototipo estándar),
    /// manteniendo los colores morales/Base cuando el Prototipo Cian está equipado.
    /// </summary>
    Color GetEffectiveGauntletColor(Color fallbackMoralOrDefault, bool useSecondary = false)
    {
        if (PlayerStats.Instance != null && PlayerStats.Instance.equippedGauntlet != GauntletVariant.PrototypeCyan)
        {
            GauntletSpec spec = PlayerStats.GetGauntletSpec(PlayerStats.Instance.equippedGauntlet);
            return useSecondary ? spec.SecondaryColor : spec.PrimaryColor;
        }
        return fallbackMoralOrDefault;
    }

    /// <summary>
    /// Aplica daño de combate y dispara las pasivas activas del Guantelete equipado
    /// (quemadura Piro-Reactor, 5% robo de vida Vorst) sin duplicar lógica entre ataques.
    /// </summary>
    public void DealGauntletHitDamage(EnemyDummy target, float damageAmount, bool isCrit, float knockback, bool isCorrupcion, bool showLifestealPopup = true)
    {
        if (target == null) return;

        float hpBefore = target.CurrentHP;
        target.TakeDamage(damageAmount, isCrit, knockback);
        float actualDealt = target.isInvulnerable ? 0f : Mathf.Max(0f, hpBefore - target.CurrentHP);

        if (PlayerStats.Instance != null)
        {
            if (PlayerStats.Instance.equippedGauntlet == GauntletVariant.PyroReactor)
            {
                target.ApplyFireBurn(
                    PlayerStats.Instance.GetPyroBurnDamagePerTick(),
                    PlayerStats.PYRO_BURN_DURATION,
                    PlayerStats.PYRO_BURN_TICK_INTERVAL);
            }
            else if (PlayerStats.Instance.equippedGauntlet == GauntletVariant.VorstVoid && actualDealt > 0f)
            {
                float stolen = PlayerStats.Instance.ApplyGauntletLifeSteal(actualDealt);
                if (stolen > 0f && CombatVFXManager.Instance != null)
                {
                    CombatVFXManager.Instance.SpawnVoidLifestealVFX(target.transform.position, transform.position);
                    if (showLifestealPopup && Time.time - lastLifestealPopupTime >= 0.45f)
                    {
                        lastLifestealPopupTime = Time.time;
                        CombatVFXManager.Instance.SpawnFloatingText(
                            transform.position + Vector3.up * 2.15f,
                            $"+{Mathf.Max(1, Mathf.RoundToInt(stolen))} HP (VACÍO)",
                            new Color(0.84f, 0.38f, 1f), 14, 0.65f);
                    }
                }
            }
        }

        if (isCorrupcion)
        {
            target.ApplyPlasmaBurn(damageAmount * 0.35f, 3.2f);
        }
    }

    private void UpdateOverdriveAndMoralVisuals(bool isOverdrive, bool isPureza, bool isCorrupcion)
    {
        if (CombatVFXManager.Instance == null) return;

        // BLOQUE 2 (Issue #17): Sombras holográficas (Afterimage Ghost Trail) y arcos eléctricos en Overdrive
        if (isOverdrive)
        {
            if (isMovingManual || attackAnimEndTimer > 0f)
            {
                afterimageTimer -= Time.deltaTime;
                if (afterimageTimer <= 0f && spriteRenderer != null)
                {
                    afterimageTimer = 0.055f;
                    Color ghostBase = GetEffectiveGauntletColor(
                        isCorrupcion ? new Color(1f, 0.18f, 0.52f) : new Color(0.18f, 0.95f, 1f));
                    Color ghostCol = new Color(ghostBase.r, ghostBase.g, ghostBase.b, 0.58f);
                    CombatVFXManager.Instance.SpawnAfterimageGhost(
                        ResolveGauntletVariantSprite(spriteRenderer.sprite),
                        transform.position,
                        transform.localScale,
                        isFacingLeft,
                        ghostCol
                    );
                }
            }

            overdriveArcTimer -= Time.deltaTime;
            if (overdriveArcTimer <= 0f)
            {
                overdriveArcTimer = 0.085f;
                Color arcBase = GetEffectiveGauntletColor(
                    isCorrupcion ? new Color(1f, 0.20f, 0.55f) : new Color(0.22f, 0.96f, 1f));
                Color arcCol = new Color(arcBase.r, arcBase.g, arcBase.b, 0.9f);
                CombatVFXManager.Instance.SpawnElectricArc(transform.position + Vector3.up * 0.55f, arcCol);
            }
        }

        // BLOQUE 4 (Issue #22) & Aura sutil del Guantelete elemental equipado (Issue #21)
        bool hasElementalGauntlet = (PlayerStats.Instance != null && PlayerStats.Instance.equippedGauntlet != GauntletVariant.PrototypeCyan);
        if (isPureza || isCorrupcion || hasElementalGauntlet)
        {
            moralAuraTimer -= Time.deltaTime;
            if (moralAuraTimer <= 0f)
            {
                moralAuraTimer = 0.26f;
                if (hasElementalGauntlet)
                {
                    Color gCol = CombatVFXManager.Instance.GetGauntletPrimaryColor();
                    gCol.a = 0.85f;
                    CombatVFXManager.Instance.SpawnFloatingRuneOrSpore(
                        transform.position + new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(0.25f, 1.05f), 0f),
                        gCol
                    );
                }
                else if (isPureza)
                {
                    Color runeCol = (Random.value > 0.5f)
                        ? new Color(0.30f, 0.98f, 1f, 0.85f)
                        : new Color(1f, 0.90f, 0.35f, 0.85f);
                    CombatVFXManager.Instance.SpawnFloatingRuneOrSpore(
                        transform.position + new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(0.2f, 1.1f), 0f),
                        runeCol
                    );
                }
                else if (isCorrupcion)
                {
                    CombatVFXManager.Instance.SpawnElectricArc(
                        transform.position + Vector3.up * 0.5f,
                        new Color(0.72f, 0.12f, 1f, 0.88f)
                    );
                }
            }
        }
    }

    public void TriggerManualAttack()
    {
        if (isCastingUlt) return;
        if (PlayerStats.Instance != null && PlayerStats.Instance.IsAnyBlockingModalOpen)
            return;

        float rate = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackRate : 1.2f;
        // BLOQUE 2: +30% Velocidad de ataque durante Sobremarcha Nexo (Overdrive)
        if (PlayerStats.Instance != null && PlayerStats.Instance.isOverdriveActive)
        {
            rate *= 1.30f;
        }
        if (Time.time < nextAttackTime) return;

        isGuarding = false;
        wasGuardingLastFrame = false;
        transform.localRotation = Quaternion.identity;
        if (hexShieldObj != null) hexShieldObj.SetActive(false);

        bool isPureza = (PlayerStats.Instance != null && PlayerStats.Instance.compassionPoints > PlayerStats.Instance.rigorPoints);
        bool isCorrupcion = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);
        if (spriteRenderer != null) spriteRenderer.color = GetMoralBaseTint(isPureza, isCorrupcion);

        ExecuteComboStep(rate, isPureza, isCorrupcion);
        nextAttackTime = Time.time + (1f / Mathf.Max(0.5f, rate));
    }

    void LateUpdate()
    {
        if (anim != null)
        {
            AnimatorStateInfo state = anim.IsInTransition(0)
                ? anim.GetNextAnimatorStateInfo(0)
                : anim.GetCurrentAnimatorStateInfo(0);

            float s = scaleAttack;

            if (isCastingUlt || state.IsName("Kael_Ultimate"))
                s = scaleUlt;
            else if (state.IsName("Kael_Run"))
                s = scaleRun;
            else if (state.IsName("Kael_Idle"))
                s = scaleIdle;

            transform.localScale = new Vector3(s, s, 1f);
        }

        if (spriteRenderer == null) return;

        if (!isCastingUlt)
        {
            if (riposteSpriteTimer > 0f)
            {
                riposteSpriteTimer -= Time.unscaledDeltaTime;
                float prog = 1f - Mathf.Clamp01(riposteSpriteTimer / 0.36f);
                int idx = Mathf.Clamp(Mathf.FloorToInt(prog * 4f), 0, 3);
                if (kaelRiposteSprites != null && kaelRiposteSprites[idx] != null)
                {
                    spriteRenderer.sprite = kaelRiposteSprites[idx];
                }
            }
            else if (isGuarding)
            {
                float heldTime = Mathf.Max(0f, Time.unscaledTime - lastBlockStartTime);
                int idx = (heldTime < 0.08f) ? 0 : (1 + (Mathf.FloorToInt(heldTime * 10f) % 3));
                if (kaelBlockSprites != null && kaelBlockSprites[idx] != null)
                {
                    spriteRenderer.sprite = kaelBlockSprites[idx];
                }
            }
        }

        RefreshEquippedGauntletVisuals();
    }

    void ExecuteComboStep(float effectiveRate, bool isPureza, bool isCorrupcion)
    {
        if (Time.time - lastAttackTime <= COMBO_WINDOW && currentCombo > 0)
        {
            currentCombo = (currentCombo % 3) + 1;
        }
        else
        {
            currentCombo = 1;
        }

        lastAttackTime = Time.time;
        attackAnimEndTimer = Mathf.Clamp(0.42f / Mathf.Max(1f, effectiveRate * 0.75f), 0.20f, 0.44f);

        if (anim != null)
        {
            anim.SetBool("IsRunning", false);
            anim.SetInteger("ComboStep", currentCombo);
            string stateName = (currentCombo == 1) ? "Kael_Atk1" : (currentCombo == 2 ? "Kael_Atk2" : "Kael_Atk3");
            anim.Play(stateName, 0, 0f);
        }

        float baseDmg = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackDamage : 12f;
        bool isCrit = (PlayerStats.Instance != null && Random.value <= PlayerStats.Instance.critChance);
        // BLOQUE 4: En Corrupción / Disonancia Vorst, los críticos otorgan +22% de daño adicional
        float critMult = isCrit ? (isCorrupcion ? 2.22f : 2.0f) : 1.0f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHitImpact(currentCombo == 3);
        }
        else if (LevelAudioManager.Instance != null)
        {
            LevelAudioManager.Instance.PlayPunch(currentCombo == 3);
        }

        Vector2 facingDir = isFacingLeft ? Vector2.left : Vector2.right;
        float dirSign = isFacingLeft ? -1f : 1f;

        transform.position += new Vector3(dirSign * (currentCombo == 3 ? 0.14f : 0.06f), 0f, 0f);
        if (transform.position.x > maxReachedX) maxReachedX = transform.position.x;

        bool hitAnyEnemy = false;

        if (currentCombo < 3)
        {
            List<EnemyDummy> targets = FindEnemiesInAttackArc(attackRange, facingDir);
            if (targets.Count > 0)
            {
                int maxTargets = (currentCombo == 1) ? 2 : 3;
                int hitCount = Mathf.Min(targets.Count, maxTargets);
                for (int i = 0; i < hitCount; i++)
                {
                    EnemyDummy target = targets[i];
                    if (target != null)
                    {
                        DealGauntletHitDamage(target, baseDmg * critMult, isCrit, knockback: 0.12f, isCorrupcion: isCorrupcion);
                        hitAnyEnemy = true;
                        if (CombatVFXManager.Instance != null)
                        {
                            CombatVFXManager.Instance.RegisterComboHit();
                            CombatVFXManager.Instance.SpawnHitSparks(target.transform.position + Vector3.up * 0.5f, isCrit, isCrit ? 8 : 4);
                        }
                    }
                }
                if (isCrit && CombatVFXManager.Instance != null)
                {
                    CombatVFXManager.Instance.ShakeCamera(0.07f, 0.05f);
                }
            }
            else
            {
                if (CombatVFXManager.Instance != null)
                {
                    CombatVFXManager.Instance.SpawnHitSparks(transform.position + new Vector3(dirSign * 0.95f, 0.55f, 0f), false, 3);
                }
            }
        }
        else
        {
            float slamBonus = (PlayerStats.Instance != null) ? PlayerStats.Instance.comboFinisherMultiplier : 1.6f;
            float slamDamage = baseDmg * slamBonus * critMult;

            List<EnemyDummy> targets = FindEnemiesInAttackArc(aoeSplashRange, facingDir);
            foreach (EnemyDummy enemy in targets)
            {
                if (enemy != null)
                {
                    DealGauntletHitDamage(enemy, slamDamage, isCrit, knockback: 0.42f, isCorrupcion: isCorrupcion);
                    hitAnyEnemy = true;
                    if (CombatVFXManager.Instance != null)
                    {
                        CombatVFXManager.Instance.RegisterComboHit();
                    }
                }
            }

            if (CombatVFXManager.Instance != null)
            {
                Color waveCol = GetEffectiveGauntletColor(
                    isCorrupcion ? new Color(1f, 0.18f, 0.48f) : (isPureza ? new Color(1f, 0.88f, 0.32f) : new Color(0.22f, 0.92f, 1f)));
                CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + new Vector3(dirSign * 1.05f, 0.35f, 0f), waveCol, 2.1f);
                CombatVFXManager.Instance.SpawnHitSparks(transform.position + new Vector3(dirSign * 1.15f, 0.15f, 0f), true, 14);
                CombatVFXManager.Instance.ShakeCamera(0.15f, 0.09f);
            }
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddUltCharge(hitAnyEnemy ? 11f : 3f);
        }
    }

    List<EnemyDummy> FindEnemiesInAttackArc(float range, Vector2 facingDir)
    {
        List<EnemyDummy> result = new List<EnemyDummy>();
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.5f;

        RaycastHit2D[] rayHits = Physics2D.RaycastAll(origin, facingDir, range, enemyLayer);
        foreach (RaycastHit2D hit in rayHits)
        {
            if (hit.collider != null)
            {
                EnemyDummy e = hit.collider.GetComponent<EnemyDummy>();
                if (e != null && !result.Contains(e)) result.Add(e);
            }
        }

        Vector2 boxCenter = origin + facingDir * (range * 0.5f);
        Collider2D[] boxHits = Physics2D.OverlapBoxAll(boxCenter, new Vector2(range + 0.5f, 2.4f), 0f);
        foreach (Collider2D col in boxHits)
        {
            if (col != null)
            {
                EnemyDummy e = col.GetComponent<EnemyDummy>();
                if (e != null && !result.Contains(e)) result.Add(e);
            }
        }

        EnemyDummy[] allEnemies = FindObjectsByType<EnemyDummy>(FindObjectsInactive.Exclude);
        foreach (EnemyDummy e in allEnemies)
        {
            if (e == null || result.Contains(e)) continue;
            float dx = e.transform.position.x - transform.position.x;
            bool inFrontOrOverlapping = isFacingLeft ? (dx <= 0.45f && dx >= -range) : (dx >= -0.45f && dx <= range);
            if (inFrontOrOverlapping)
            {
                result.Add(e);
            }
        }

        result.Sort((a, b) =>
            Mathf.Abs(a.transform.position.x - transform.position.x)
            .CompareTo(Mathf.Abs(b.transform.position.x - transform.position.x)));

        return result;
    }

    public void TriggerUltimate()
    {
        if (isCastingUlt || PlayerStats.Instance == null || PlayerStats.Instance.IsAnyBlockingModalOpen) return;
        if (PlayerStats.Instance.ultCharge < 100f) return;
        if (!PlayerStats.Instance.ConsumeUltCharge()) return;

        StartCoroutine(CastUltimateRoutine());
    }

    // SECUENCIA CINEMATOGRÁFICA DE ULTIMATE EN 3 FASES (~1.65 SEGUNDOS)
    public IEnumerator CastUltimateRoutine()
    {
        isCastingUlt = true;
        isGuarding = false;
        wasGuardingLastFrame = false;
        isMovingManual = false;
        transform.localRotation = Quaternion.identity;
        if (hexShieldObj != null) hexShieldObj.SetActive(false);

        bool isPureza = (PlayerStats.Instance != null && PlayerStats.Instance.compassionPoints > PlayerStats.Instance.rigorPoints);
        bool isCorrupcion = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);

        if (spriteRenderer != null) spriteRenderer.color = GetMoralBaseTint(isPureza, isCorrupcion);

        // BLOQUE 4: En Pureza Nexo, activar la Ultimate restaura 10% de la vida máxima con partículas verdes/doradas
        if (isPureza && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.HealPercent(0.10f);
            if (CombatVFXManager.Instance != null)
                CombatVFXManager.Instance.SpawnHealingParticles(transform.position + Vector3.up * 0.5f);
        }

        if (anim != null)
        {
            anim.ResetTrigger("Ultimate");
            anim.SetBool("IsRunning", false);
            anim.SetInteger("ComboStep", 0);
            anim.SetTrigger("Ultimate");
            anim.speed = 0.22f;
        }

        float dirSign = isFacingLeft ? -1f : 1f;
        Vector2 facingDir = isFacingLeft ? Vector2.left : Vector2.right;
        Vector3 gauntletPos = transform.position + new Vector3(dirSign * 0.85f, 0.65f, 0f);

        // --- FASE 1: CONCENTRACIÓN DE ENERGÍA NEXO (0.55s) ---
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SetUltimateCinematic(true, triggerFlash: false);
            CombatVFXManager.Instance.SpawnUltChargeInward(gauntletPos, 0.50f);
            string chargeLabel = isCorrupcion
                ? "⚡ ¡CANALIZANDO DISONANCIA ABISAL! ⚡"
                : (isPureza ? "☀ ¡CANALIZANDO PUREZA SOLAR NEXO! ☀" : "⚡ ¡CARGANDO CAÑÓN NEXO! ⚡");
            Color chargeCol = GetEffectiveGauntletColor(
                isCorrupcion ? new Color(1f, 0.22f, 0.52f) : (isPureza ? new Color(1f, 0.90f, 0.35f) : Color.cyan));
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.55f, chargeLabel, chargeCol, 22, 0.7f);
            CombatVFXManager.Instance.ShakeCamera(0.55f, 0.05f);
        }

        yield return new WaitForSeconds(0.55f);

        // --- FASE 2: DISPARO DEL MEGA RAYO DE PLASMA (0.85s CON 5 MULTI-IMPACTOS) ---
        if (anim != null) anim.speed = 0.35f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUltimateBeam();
        }
        else if (LevelAudioManager.Instance != null)
        {
            LevelAudioManager.Instance.PlayUltimateLaser();
        }

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnUltimateMegaBeam(gauntletPos, ultimateRange * dirSign, 0.85f);
            CombatVFXManager.Instance.ShakeCamera(0.85f, 0.20f);
        }

        float ultBonus = (PlayerStats.Instance != null) ? PlayerStats.Instance.ultimateDamageMultiplier : 1.0f;
        if (isCorrupcion) ultBonus *= 1.22f;
        float hitDamage = ((PlayerStats.Instance != null) ? PlayerStats.Instance.attackDamage : 12f) * 0.95f * ultBonus;

        for (int wave = 0; wave < 5; wave++)
        {
            List<EnemyDummy> hits = FindEnemiesInAttackArc(ultimateRange, facingDir);
            foreach (EnemyDummy enemy in hits)
            {
                if (enemy != null)
                {
                    DealGauntletHitDamage(enemy, hitDamage, isCrit: true, knockback: 0.24f, isCorrupcion: isCorrupcion, showLifestealPopup: (wave == 0));
                    if (CombatVFXManager.Instance != null)
                    {
                        CombatVFXManager.Instance.RegisterComboHit();
                    }
                }
            }
            yield return new WaitForSeconds(0.16f);
        }

        // --- FASE 3: RECUPERACIÓN Y CIERRE ---
        yield return new WaitForSeconds(0.15f);

        if (anim != null)
        {
            anim.speed = 1f;
            anim.ResetTrigger("Ultimate");
        }

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SetUltimateCinematic(false, triggerFlash: true);
        }

        isCastingUlt = false;
    }

    // --- LECTURA HÍBRIDA DE TECLADO (Compatible con Input System nuevo y Input Manager clásico) ---
    public static bool IsLeftHeld()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) return true;
        }
        try { return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow); }
        catch { return false; }
    }

    public static bool IsRightHeld()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) return true;
        }
        try { return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow); }
        catch { return false; }
    }

    public static bool IsGuardHeld()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) return true;
        }
        try { return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow); }
        catch { return false; }
    }

    public static bool WasAttackPressed()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.jKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        }
        try { return Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Space); }
        catch { return false; }
    }

    public static bool WasUltimatePressed()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame) return true;
        }
        try { return Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Q); }
        catch { return false; }
    }

    public static bool WasUpgradeTreeShortcutPressed()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.mKey.wasPressedThisFrame) return true;
        }
        try { return Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.M); }
        catch { return false; }
    }

    public static bool WasArsenalShopShortcutPressed()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.bKey.wasPressedThisFrame || Keyboard.current.tKey.wasPressedThisFrame) return true;
        }
        try { return Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.T); }
        catch { return false; }
    }

    public static bool WasEscapePressed()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) return true;
        }
        try { return Input.GetKeyDown(KeyCode.Escape); }
        catch { return false; }
    }
}
