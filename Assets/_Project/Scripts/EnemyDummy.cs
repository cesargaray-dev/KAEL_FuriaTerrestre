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

    private float currentHP;
    private float nextAttackTime;
    private int attackCounter = 0;
    private bool isDead = false;
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
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioLasher", "Die", 140f);

        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioHulk", "Walk", 122f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioHulk", "Attack", 122f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("BioHulk", "Die", 122f);

        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("HiveInquisitor", "Walk", 122f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("HiveInquisitor", "Attack", 122f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("HiveInquisitor", "Die", 122f);

        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("VortexOmega", "Walk", 118f);
        Level3MonsterAssetGenerator.GetMonsterAnimationFrames("VortexOmega", "Attack", 118f);
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
    /// BLOQUE 5 (Issue #23): Configura los sprites y efectos exclusivos de los 3 monstruos
    /// y 2 jefes biomecánicos del Nivel 3 (BioStalker, BioLasher, BioHulk, HiveInquisitor, VortexOmega).
    /// </summary>
    public void SetupLevel3Visuals(string resourcePrefix)
    {
        visualType = resourcePrefix;
        isLevel3Monster = true;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (anim == null) anim = GetComponent<Animator>();

        float ppu = 140f;
        if (resourcePrefix == "BioStalker") ppu = 148f;
        else if (resourcePrefix == "BioLasher") ppu = 140f;
        else if (resourcePrefix == "BioHulk") ppu = 122f;
        else if (resourcePrefix == "HiveInquisitor") ppu = 122f;
        else if (resourcePrefix == "VortexOmega") ppu = 118f;

        customWalkFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(resourcePrefix, "Walk", ppu);
        customAttackFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(resourcePrefix, "Attack", ppu);
        customDieFrames = Level3MonsterAssetGenerator.GetMonsterAnimationFrames(resourcePrefix, "Die", ppu);

        if (customWalkFrames != null && customWalkFrames.Length > 0 && customWalkFrames[0] != null)
        {
            useCustomSprites = true;
            if (anim != null) anim.enabled = false;
            sr.sprite = customWalkFrames[0];
        }

        // Si es el Mini-Jefe de Oleada 5 (HiveInquisitor), creamos sus 3 anillos psiónicos rotatorios
        if (resourcePrefix == "HiveInquisitor" && psionicRings.Count == 0)
        {
            Sprite ringSpr = Level3MonsterAssetGenerator.GetPsionicRingSprite();
            for (int i = 0; i < 3; i++)
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

        if (sr != null && !isPlayingAttackAnim)
        {
            sr.flipX = (dirToKael > 0f);
        }

        if (distToKael > attackRange && !isChargingHeavy && !isPlayingAttackAnim)
        {
            if (anim != null && !useCustomSprites) anim.SetBool("IsAttacking", false);
            transform.Translate(new Vector2(dirToKael, 0f) * (moveSpeed * Time.deltaTime), Space.World);

            // El Coloso de Crisálida (BioHulk) hace vibrar ligeramente la cámara con sus pasos pesados
            if (visualType == "BioHulk")
            {
                hulkStepShakeTimer -= Time.deltaTime;
                if (hulkStepShakeTimer <= 0f)
                {
                    hulkStepShakeTimer = 0.55f;
                    if (CombatVFXManager.Instance != null)
                        CombatVFXManager.Instance.ShakeCamera(0.07f, 0.03f);
                }
            }
        }
        else
        {
            if (anim != null && !useCustomSprites) anim.SetBool("IsAttacking", true);

            if (!isChargingHeavy && !isPlayingAttackAnim && Time.time >= nextAttackTime)
            {
                attackCounter++;
                bool triggerHeavy = ((isMiniBoss || isBoss) && attackCounter % 3 == 0) || (visualType == "BioHulk" && attackCounter % 2 == 0);
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

            yield return new WaitForSeconds(0.082f);
        }

        transform.position = new Vector3(baseX, baseGroundY, transform.position.z);
        nextAttackTime = Time.time + (0.50f / attackRate);
        isPlayingAttackAnim = false;
    }

    void LateUpdate()
    {
        if (!useCustomSprites || sr == null) return;

        if (isDead)
        {
            transform.localRotation = Quaternion.identity;
            dieAnimTimer += Time.deltaTime * 8.5f;
            if (customDieFrames != null && customDieFrames.Length > 0)
            {
                int idx = Mathf.Clamp(Mathf.FloorToInt(dieAnimTimer), 0, customDieFrames.Length - 1);
                if (customDieFrames[idx] != null) sr.sprite = customDieFrames[idx];
            }
            return;
        }

        if (isPlayingAttackAnim) return;

        walkAnimTimer += Time.deltaTime * 10.5f;
        if (customWalkFrames != null && customWalkFrames.Length > 0)
        {
            int frame = Mathf.FloorToInt(walkAnimTimer) % customWalkFrames.Length;
            if (customWalkFrames[frame] != null) sr.sprite = customWalkFrames[frame];
        }

        transform.localRotation = Quaternion.identity;
        float distToKael = (kaelTarget != null) ? Mathf.Abs(transform.position.x - kaelTarget.position.x) : 99f;

        // Levitación sinusoidal para Archon, BioLasher y HiveInquisitor
        bool isLevitator = (visualType == "Archon" || visualType == "BioLasher" || visualType == "HiveInquisitor");
        if (!isChargingHeavy && isLevitator)
        {
            float amp = (visualType == "BioLasher") ? 0.22f : 0.14f;
            float hoverY = Mathf.Sin(walkAnimTimer * 0.6f) * amp + 0.14f;
            transform.position = new Vector3(transform.position.x, baseGroundY + hoverY, transform.position.z);
        }
        else if (distToKael > attackRange && !isChargingHeavy)
        {
            transform.position = new Vector3(transform.position.x, baseGroundY, transform.position.z);
        }
    }

    IEnumerator TelegraphedHeavyAttack()
    {
        isChargingHeavy = true;
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
    }

    public void TakeDamage(float dmg, bool isCrit = false, float knockback = 0f)
    {
        if (isDead) return;

        currentHP = Mathf.Max(0f, currentHP - dmg);
        if (sr != null && !isChargingHeavy) StartCoroutine(HitFlash());

        // BLOQUE 5: Fase de Furia Biológica de VORTEX-OMEGA al bajar del 45% de vida
        if (visualType == "VortexOmega" && !hasEnteredRagePhase && currentHP > 0f && (currentHP / maxHP) <= 0.45f)
        {
            TriggerVortexOmegaRagePhase();
        }

        if (knockback > 0f && !isPlayingAttackAnim)
        {
            float resist = (isBoss || isMiniBoss) ? 0.25f : 0.7f;
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

    private void TriggerVortexOmegaRagePhase()
    {
        hasEnteredRagePhase = true;
        moveSpeed *= 1.25f;
        attackRate *= 1.25f;
        originalColor = new Color(1f, 0.65f, 0.85f, 1f);
        if (sr != null) sr.color = originalColor;

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.TriggerScreenFlash(new Color(1f, 0.15f, 0.45f), 0.45f);
            CombatVFXManager.Instance.ShakeCamera(0.45f, 0.22f);
            CombatVFXManager.Instance.SpawnShockwaveRing(transform.position + Vector3.up * 1.1f, new Color(1f, 0.15f, 0.45f), 3.5f);
            CombatVFXManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 2.1f,
                "🔥 ¡FURIA BIOLÓGICA OMEGA! +25% VELOCIDAD 🔥",
                new Color(1f, 0.25f, 0.55f),
                24,
                1.6f
            );
        }

        // Invocar 2 Acechadores Quimera (Bio-Stalkers) de escolta
        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.SpawnVortexOmegaEscorts(transform.position);
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
        transform.localRotation = Quaternion.identity;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (anim != null && !useCustomSprites) anim.SetBool("IsAttacking", false);

        if (LevelAudioManager.Instance != null)
            LevelAudioManager.Instance.PlayEnemyDie();

        Time.timeScale = 0.35f;
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.ShakeCamera(1.4f, 0.20f);
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.5f, $"¡{enemyTitle.ToUpper()} ABATIDO!", Color.cyan, 26, 1.5f);
        }

        Vector3 basePos = new Vector3(transform.position.x, baseGroundY, transform.position.z);

        for (int i = 0; i < 4; i++)
        {
            if (sr != null) sr.color = (i % 2 == 0) ? Color.white : new Color(1f, 0.2f, 0.3f);
            transform.position = basePos + (Vector3)UnityEngine.Random.insideUnitCircle * 0.25f;

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnBossExplosionBurst(transform.position);
            }
            yield return new WaitForSecondsRealtime(0.32f);
        }

        Time.timeScale = 1f;
        transform.position = basePos;
        if (sr != null) sr.color = originalColor;

        int finalGold = goldReward;
        if (PlayerStats.Instance != null)
        {
            finalGold = Mathf.RoundToInt(goldReward * PlayerStats.Instance.goldMultiplier);
            PlayerStats.Instance.AddRewards(goldReward, expReward);
        }

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnLootDrop(transform.position, finalGold, expReward, true);
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
