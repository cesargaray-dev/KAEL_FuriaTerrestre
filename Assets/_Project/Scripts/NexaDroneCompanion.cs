using UnityEngine;
using System.Collections;

/// <summary>
/// Compañero de combate flotante: manifestación física de N.E.X.A. como un dron táctico ancestral
/// que orbita alrededor del hombro de Kael con física elástica (Spring / Lerp), provee fuego
/// de cobertura con proyectiles de plasma cian guiados y despliega el Protocolo Baluarte
/// (escudo de emergencia de 3 impactos) cuando la vida de Kael cae por debajo del 25%.
/// </summary>
public class NexaDroneCompanion : MonoBehaviour
{
    public static NexaDroneCompanion Instance { get; private set; }

    [Header("1. Órbita y Física Elástica (Spring / Lerp)")]
    public Transform kaelTarget;
    public Vector3 shoulderOffset = new Vector3(-0.72f, 1.42f, 0f);
    public float orbitRadiusX = 0.26f;
    public float orbitRadiusY = 0.15f;
    public float orbitSpeed = 2.4f;
    public float springStiffness = 48f;
    public float springDamping = 8.2f;
    public float lerpFollowFactor = 14f;
    public float droneScale = 0.85f;

    [Header("2. Lógica de Fuego Asistido")]
    public float detectionRadius = 6.0f;
    public float searchInterval = 0.20f;
    public float fireInterval = 2.5f;
    public float baseDamage = 14f;
    public float damagePerKaelLevel = 5.5f;
    public float kaelAtkScaling = 0.28f;
    public float projectileSpeed = 10.5f;
    public float projectileTurnSpeed = 460f;
    public LayerMask enemyLayer = 1 << 6;

    [Header("3. Protocolo Baluarte (Escudo de Emergencia < 25% HP)")]
    [Range(0.10f, 0.50f)] public float criticalHpThreshold = 0.25f;
    public int maxShieldImpacts = 3;
    public float overheatCooldown = 16.0f;

    // Estado interno del Escudo Baluarte
    private bool isShieldActive = false;
    private int shieldHitsRemaining = 0;
    private bool isOverheated = false;
    private float overheatTimer = 0f;
    private float shieldRippleTimer = 0f;
    private bool hasAnnouncedBulwarkThisWave = false;

    public bool IsShieldActive => isShieldActive;
    public int ShieldHitsRemaining => shieldHitsRemaining;
    public bool IsOverheated => isOverheated;
    public float OverheatRemainingRatio => isOverheated ? Mathf.Clamp01(overheatTimer / overheatCooldown) : 0f;

    // Estado de movimiento y combate
    private Vector3 springVelocity = Vector3.zero;
    private float orbitPhase = 0f;
    private float searchTimer = 0f;
    private float fireTimer = 0.8f; // Primer disparo rápido al entrar en combate
    private float thrusterParticleTimer = 0f;
    private float overheatSmokeTimer = 0f;
    private EnemyDummy currentTarget;

    // Componentes visuales del Dron y del Escudo
    private SpriteRenderer chassisRenderer;
    private Transform coreRotorTransform;
    private SpriteRenderer coreRotorRenderer;
    private Transform leftThrusterTransform;
    private Transform rightThrusterTransform;
    private SpriteRenderer leftThrusterRenderer;
    private SpriteRenderer rightThrusterRenderer;
    private Transform muzzleTransform;
    private SpriteRenderer muzzleFlashRenderer;
    private float muzzleFlashAlpha = 0f;

    // Componentes visuales de la Burbuja de Plasma (Protocolo Baluarte)
    private GameObject shieldBubbleRoot;
    private SpriteRenderer shieldBubbleRenderer;
    private SpriteRenderer shieldHexRingRenderer;

    // Sprites generados/cacheados en memoria
    private static Sprite _cachedDroneSprite;
    private static Sprite _cachedCoreRotorSprite;
    private static Sprite _cachedThrusterFlameSprite;
    private static Sprite _cachedPlasmaBoltSprite;
    private static Sprite _cachedShieldBubbleSprite;
    private static Sprite _cachedShieldRingSprite;
    private static Sprite _cachedSparkSprite;

    public static NexaDroneCompanion EnsureExists(Transform kael = null)
    {
        if (Instance != null)
        {
            if (kael != null && Instance.kaelTarget == null) Instance.kaelTarget = kael;
            return Instance;
        }

        NexaDroneCompanion existing = FindAnyObjectByType<NexaDroneCompanion>();
        if (existing != null)
        {
            Instance = existing;
            if (kael != null && existing.kaelTarget == null) existing.kaelTarget = kael;
            return existing;
        }

        GameObject droneObj = new GameObject("NexaDrone");
        NexaDroneCompanion comp = droneObj.AddComponent<NexaDroneCompanion>();
        if (kael != null) comp.kaelTarget = kael;
        return comp;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureSpritesGenerated();
        BuildDroneVisualHierarchy();
        BuildShieldBubbleHierarchy();
    }

    void Start()
    {
        if (kaelTarget == null)
        {
            KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
            if (kael != null) kaelTarget = kael.transform;
        }

        if (kaelTarget != null)
        {
            transform.position = kaelTarget.position + shoulderOffset;
        }
    }

    public void ResetCompanion()
    {
        isShieldActive = false;
        shieldHitsRemaining = 0;
        isOverheated = false;
        overheatTimer = 0f;
        springVelocity = Vector3.zero;
        currentTarget = null;
        fireTimer = 1.0f;
        hasAnnouncedBulwarkThisWave = false;

        if (shieldBubbleRoot != null) shieldBubbleRoot.SetActive(false);
        if (kaelTarget != null) transform.position = kaelTarget.position + shoulderOffset;
    }

    void Update()
    {
        if (PlayerStats.Instance != null && (PlayerStats.Instance.isPaused || PlayerStats.Instance.isGameOver || PlayerStats.Instance.isLevelCompleted || PlayerStats.Instance.isChoosingDilemma || PlayerStats.Instance.isChoosingSkill || PlayerStats.Instance.isUpgradeTreeOpen))
            return;

        if (kaelTarget == null)
        {
            KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
            if (kael != null)
            {
                kaelTarget = kael.transform;
                transform.position = kaelTarget.position + shoulderOffset;
            }
            else return;
        }

        // 1. Verificar activación del Protocolo Baluarte si la vida de Kael cae por debajo del 25%
        CheckEmergencyShieldTrigger();
        UpdateOverheatState();

        // 2. Búsqueda periódica del enemigo más cercano en un radio de 6 unidades
        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            searchTimer = searchInterval;
            FindClosestEnemyInRadius();
        }

        // 3. Movimiento orbital suave con amortiguación elástica (Spring / Lerp) y orientación al enemigo
        UpdateOrbitalSpringMovement();

        // 4. Animación del núcleo giratorio cian, propulsores antigravedad y burbuja de escudo
        UpdateVisualAnimations();

        // 5. Lógica de fuego asistido cada 2.5 segundos
        UpdateAssistedFire();
    }

    void UpdateOrbitalSpringMovement()
    {
        orbitPhase += Time.deltaTime * orbitSpeed;

        // Trayectoria orbital en ocho suave alrededor del punto del hombro de Kael
        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(orbitPhase) * orbitRadiusX,
            Mathf.Sin(orbitPhase * 1.7f) * orbitRadiusY,
            0f
        );

        bool kaelFacingLeft = (KaelAutoController.Instance != null && KaelAutoController.Instance.isFacingLeft);
        Vector3 dynamicShoulder = new Vector3(
            kaelFacingLeft ? -shoulderOffset.x : shoulderOffset.x,
            shoulderOffset.y,
            shoulderOffset.z
        );

        Vector3 targetAnchor = kaelTarget.position + dynamicShoulder + orbitOffset;

        // Física de resorte con amortiguación (Hooke's Law: F = -k*x - c*v) combinada con Lerp
        Vector3 displacement = targetAnchor - transform.position;
        Vector3 springAccel = (displacement * springStiffness) - (springVelocity * springDamping);
        springVelocity += springAccel * Time.deltaTime;

        Vector3 springStepPos = transform.position + springVelocity * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, springStepPos, Mathf.Clamp01(lerpFollowFactor * Time.deltaTime));

        // Orientación suave hacia el enemigo objetivo (o inclinación orgánica de vuelo si no hay enemigo)
        float targetAngleZ = 0f;
        if (currentTarget != null && !currentTarget.IsDead)
        {
            Vector3 aimPoint = currentTarget.transform.position + Vector3.up * 0.55f;
            Vector3 dir = aimPoint - transform.position;

            if (dir.x < -0.1f)
            {
                // Si el enemigo quedó a la izquierda, voltear en X manteniendo rotación natural
                transform.localScale = new Vector3(-droneScale, droneScale, 1f);
                targetAngleZ = Mathf.Clamp(Mathf.Atan2(dir.y, -dir.x) * Mathf.Rad2Deg, -26f, 26f);
            }
            else
            {
                transform.localScale = new Vector3(droneScale, droneScale, 1f);
                targetAngleZ = Mathf.Clamp(Mathf.Atan2(dir.y, Mathf.Max(0.1f, dir.x)) * Mathf.Rad2Deg, -26f, 26f);
            }
        }
        else
        {
            transform.localScale = new Vector3(droneScale, droneScale, 1f);
            targetAngleZ = Mathf.Clamp(-springVelocity.x * 4.2f, -14f, 14f);
        }

        float currentAngleZ = transform.eulerAngles.z;
        float smoothAngleZ = Mathf.LerpAngle(currentAngleZ, targetAngleZ, 10f * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, smoothAngleZ);
    }

    void FindClosestEnemyInRadius()
    {
        EnemyDummy bestEnemy = null;
        float bestDistSq = detectionRadius * detectionRadius;
        Vector3 scanOrigin = transform.position;

        // 1) Búsqueda rápida por colisionadores en la capa de enemigos
        Collider2D[] hits = Physics2D.OverlapCircleAll(scanOrigin, detectionRadius, enemyLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            EnemyDummy enemy = hits[i].GetComponent<EnemyDummy>();
            if (enemy != null && !enemy.IsDead)
            {
                float dSq = (enemy.transform.position - scanOrigin).sqrMagnitude;
                if (dSq <= bestDistSq)
                {
                    bestDistSq = dSq;
                    bestEnemy = enemy;
                }
            }
        }

        // 2) Respaldo directo con EnemyDummy activos por si algún enemigo personalizado usa otra capa
        if (bestEnemy == null)
        {
            EnemyDummy[] allEnemies = FindObjectsByType<EnemyDummy>();
            for (int i = 0; i < allEnemies.Length; i++)
            {
                EnemyDummy enemy = allEnemies[i];
                if (enemy != null && !enemy.IsDead)
                {
                    // Medir distancia tanto desde el dron como desde Kael para cubrir los 6 metros tácticos
                    float dSqDrone = (enemy.transform.position - scanOrigin).sqrMagnitude;
                    float dSqKael = (kaelTarget != null) ? (enemy.transform.position - kaelTarget.position).sqrMagnitude : dSqDrone;
                    float dSq = Mathf.Min(dSqDrone, dSqKael);
                    if (dSq <= bestDistSq)
                    {
                        bestDistSq = dSq;
                        bestEnemy = enemy;
                    }
                }
            }
        }

        currentTarget = bestEnemy;
    }

    void UpdateAssistedFire()
    {
        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        if (currentTarget == null || currentTarget.IsDead)
        {
            FindClosestEnemyInRadius();
        }

        if (currentTarget != null && !currentTarget.IsDead)
        {
            FireGuidedPlasmaProjectile(currentTarget);
            fireTimer = fireInterval; // Disparo cada 2.5 segundos
        }
    }

    public float GetScaledPlasmaDamage()
    {
        int kaelLv = (PlayerStats.Instance != null) ? PlayerStats.Instance.playerLevel : 1;
        float kaelAtk = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackDamage : 12f;
        int nexoBranch = (PlayerStats.Instance != null) ? PlayerStats.Instance.branchNexo : 0;

        float rawDamage = baseDamage + ((kaelLv - 1) * damagePerKaelLevel) + (kaelAtk * kaelAtkScaling);
        float nexoMultiplier = 1f + (nexoBranch * 0.15f);
        return Mathf.RoundToInt(rawDamage * nexoMultiplier);
    }

    void FireGuidedPlasmaProjectile(EnemyDummy target)
    {
        Vector3 spawnPos = (muzzleTransform != null) ? muzzleTransform.position : (transform.position + new Vector3(0.32f, 0.02f, 0f));

        // Retroceso elástico en el resorte del dron al disparar
        float facingDir = (transform.localScale.x >= 0f) ? 1f : -1f;
        springVelocity += new Vector3(-1.45f * facingDir, 0.42f, 0f);
        muzzleFlashAlpha = 1.0f;

        if (LevelAudioManager.Instance != null)
        {
            LevelAudioManager.Instance.PlayDronePlasmaShot();
        }

        float dmg = GetScaledPlasmaDamage();
        bool isCrit = (PlayerStats.Instance != null) && (Random.value <= PlayerStats.Instance.critChance);
        if (isCrit) dmg *= 1.75f;

        GameObject boltObj = new GameObject("NexaPlasmaBolt");
        boltObj.transform.position = spawnPos;

        SpriteRenderer sr = boltObj.AddComponent<SpriteRenderer>();
        sr.sprite = _cachedPlasmaBoltSprite;
        sr.sortingOrder = 22;

        NexaGuidedPlasmaBolt bolt = boltObj.AddComponent<NexaGuidedPlasmaBolt>();
        bolt.Init(target, dmg, isCrit, projectileSpeed, projectileTurnSpeed, detectionRadius, _cachedSparkSprite);
    }

    // --- 3. PROTOCOLO BALUARTE (ESCUDO DE EMERGENCIA) ---

    public void CheckEmergencyShieldTrigger()
    {
        if (PlayerStats.Instance == null || isShieldActive || isOverheated) return;
        if (PlayerStats.Instance.currentHP <= 0f || PlayerStats.Instance.maxHP <= 0f) return;

        float hpRatio = PlayerStats.Instance.currentHP / PlayerStats.Instance.maxHP;
        if (hpRatio <= criticalHpThreshold)
        {
            ActivateBulwarkProtocol();
        }
    }

    public void ActivateBulwarkProtocol()
    {
        if (isShieldActive || isOverheated) return;

        isShieldActive = true;
        shieldHitsRemaining = maxShieldImpacts;
        shieldRippleTimer = 0.35f;

        if (shieldBubbleRoot != null)
        {
            shieldBubbleRoot.SetActive(true);
            UpdateShieldBubbleTransform();
        }

        if (LevelAudioManager.Instance != null)
        {
            LevelAudioManager.Instance.PlayShieldDeploy();
        }

        if (CombatVFXManager.Instance != null && kaelTarget != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(
                kaelTarget.position + Vector3.up * 2.25f,
                $"🛡 ¡PROTOCOLO BALUARTE! ({shieldHitsRemaining} IMPACTOS)",
                new Color(0.22f, 0.95f, 1f),
                20,
                1.25f
            );
            CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.8f, false, 8);
        }

        if (!hasAnnouncedBulwarkThisWave && CompanionDialogueManager.Instance != null)
        {
            hasAnnouncedBulwarkThisWave = true;
            CompanionDialogueManager.Instance.EnqueueLine(
                "N.E.X.A. [PROTOCOLO BALUARTE]",
                "¡Signos vitales críticos (<25%)! Desplegando burbuja de plasma protectora: absorberé los próximos 3 impactos antes de sobrecalentarme.",
                "nexa_alert"
            );
        }
    }

    /// <summary>
    /// Intercepta el daño entrante hacia Kael cuando el Protocolo Baluarte está activo.
    /// Devuelve true si el impacto fue absorbido por la burbuja de plasma.
    /// </summary>
    public bool TryAbsorbDamage(float incomingDamage)
    {
        if (!isShieldActive || shieldHitsRemaining <= 0)
            return false;

        shieldHitsRemaining--;
        shieldRippleTimer = 0.30f;

        if (LevelAudioManager.Instance != null)
        {
            LevelAudioManager.Instance.PlayShieldAbsorb();
        }

        if (CombatVFXManager.Instance != null && kaelTarget != null)
        {
            CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.85f, true, 10);
            CombatVFXManager.Instance.ShakeCamera(0.10f, 0.06f);

            if (shieldHitsRemaining > 0)
            {
                CombatVFXManager.Instance.SpawnFloatingText(
                    kaelTarget.position + Vector3.up * 2.15f,
                    $"🛡 ¡ESCUDO NEXO! ({shieldHitsRemaining}/{maxShieldImpacts})",
                    new Color(0.25f, 0.95f, 1f),
                    18,
                    0.85f
                );
            }
        }

        if (shieldHitsRemaining <= 0)
        {
            TriggerShieldOverheat();
        }

        return true;
    }

    void TriggerShieldOverheat()
    {
        isShieldActive = false;
        isOverheated = true;
        overheatTimer = overheatCooldown;

        if (shieldBubbleRoot != null)
        {
            shieldBubbleRoot.SetActive(false);
        }

        if (LevelAudioManager.Instance != null)
        {
            LevelAudioManager.Instance.PlayShieldOverheat();
        }

        if (CombatVFXManager.Instance != null && kaelTarget != null)
        {
            CombatVFXManager.Instance.SpawnHitSparks(transform.position, true, 14);
            CombatVFXManager.Instance.SpawnFloatingText(
                kaelTarget.position + Vector3.up * 2.30f,
                "⚠ ¡BALUARTE SOBRECALENTADO!",
                new Color(1f, 0.45f, 0.15f),
                19,
                1.3f
            );
        }
    }

    void UpdateOverheatState()
    {
        if (!isOverheated) return;

        overheatTimer -= Time.deltaTime;
        overheatSmokeTimer -= Time.deltaTime;

        // Chispas térmicas mientras el dron se enfría
        if (overheatSmokeTimer <= 0f)
        {
            overheatSmokeTimer = 0.28f;
            SpawnDroneParticle(transform.position + new Vector3(Random.Range(-0.15f, 0.15f), 0.1f, 0f),
                new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0.8f, 1.6f)),
                new Color(1f, 0.45f, 0.15f, 0.85f), 0.35f, 0.65f);
        }

        if (overheatTimer <= 0f)
        {
            isOverheated = false;
            overheatTimer = 0f;

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnFloatingText(
                    transform.position + Vector3.up * 0.55f,
                    "⚡ BALUARTE ENFRIADO Y LISTO",
                    new Color(0.22f, 0.95f, 1f),
                    15,
                    1.0f
                );
            }

            // Si Kael sigue por debajo del 25% de vida al terminar el enfriamiento, reactivar escudo
            CheckEmergencyShieldTrigger();
        }
    }

    void UpdateVisualAnimations()
    {
        // 1. Giro continuo del núcleo de energía cian
        if (coreRotorTransform != null)
        {
            float spinSpeed = (currentTarget != null) ? 340f : 165f;
            if (isOverheated) spinSpeed = 95f;
            coreRotorTransform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);

            float corePulse = 1f + Mathf.Sin(Time.time * 9f) * 0.10f;
            coreRotorTransform.localScale = Vector3.one * corePulse;
        }

        if (coreRotorRenderer != null)
        {
            if (isOverheated)
            {
                float heatPulse = 0.6f + Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.4f;
                coreRotorRenderer.color = new Color(1f, 0.38f, 0.12f, heatPulse);
            }
            else if (isShieldActive)
            {
                coreRotorRenderer.color = new Color(0.45f, 1f, 1f, 0.95f);
            }
            else
            {
                coreRotorRenderer.color = new Color(0.22f, 0.94f, 1f, 0.90f);
            }
        }

        if (chassisRenderer != null)
        {
            chassisRenderer.color = isOverheated
                ? Color.Lerp(Color.white, new Color(1f, 0.65f, 0.55f), Mathf.PingPong(Time.time * 6f, 0.6f))
                : Color.white;
        }

        // 2. Oscilación de los propulsores antigravedad
        float jetScaleY = 0.85f + Mathf.Abs(Mathf.Sin(Time.time * 36f)) * 0.45f;
        float jetScaleX = 0.92f + Mathf.Sin(Time.time * 24f) * 0.10f;
        if (leftThrusterTransform != null) leftThrusterTransform.localScale = new Vector3(jetScaleX, jetScaleY, 1f);
        if (rightThrusterTransform != null) rightThrusterTransform.localScale = new Vector3(jetScaleX, jetScaleY, 1f);

        // Partículas sutiles de plasma antigravedad bajo los propulsores
        thrusterParticleTimer -= Time.deltaTime;
        if (thrusterParticleTimer <= 0f)
        {
            thrusterParticleTimer = 0.09f;
            Vector3 jetPos = transform.position + new Vector3(Random.value > 0.5f ? -0.14f : 0.10f, -0.26f, 0f);
            SpawnDroneParticle(jetPos, new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-1.4f, -0.7f)),
                new Color(0.22f, 0.92f, 1f, 0.70f), 0.22f, 0.55f);
        }

        // 3. Destello de la lente frontal al disparar
        if (muzzleFlashRenderer != null)
        {
            if (muzzleFlashAlpha > 0f)
            {
                muzzleFlashAlpha = Mathf.MoveTowards(muzzleFlashAlpha, 0f, Time.deltaTime * 5.5f);
            }
            float idleGlow = (currentTarget != null) ? 0.42f : 0.18f;
            float a = Mathf.Max(idleGlow, muzzleFlashAlpha);
            muzzleFlashRenderer.color = new Color(0.55f, 0.98f, 1f, a);
            muzzleFlashRenderer.transform.localScale = Vector3.one * (0.55f + muzzleFlashAlpha * 0.65f);
        }

        // 4. Actualizar burbuja del Protocolo Baluarte alrededor de Kael
        if (isShieldActive && shieldBubbleRoot != null)
        {
            if (!shieldBubbleRoot.activeSelf) shieldBubbleRoot.SetActive(true);
            UpdateShieldBubbleTransform();
        }
    }

    void UpdateShieldBubbleTransform()
    {
        if (shieldBubbleRoot == null || kaelTarget == null) return;

        shieldBubbleRoot.transform.position = kaelTarget.position + new Vector3(0f, 0.82f, 0f);

        if (shieldRippleTimer > 0f)
        {
            shieldRippleTimer = Mathf.MoveTowards(shieldRippleTimer, 0f, Time.deltaTime);
        }

        float pulse = 1.0f + Mathf.Sin(Time.time * 7.5f) * 0.04f + (shieldRippleTimer * 0.45f);
        shieldBubbleRoot.transform.localScale = new Vector3(pulse * 1.15f, pulse * 1.15f, 1f);

        if (shieldHexRingRenderer != null)
        {
            shieldHexRingRenderer.transform.Rotate(0f, 0f, 75f * Time.deltaTime);
            float ringAlpha = 0.45f + (shieldRippleTimer * 1.2f);
            shieldHexRingRenderer.color = new Color(0.35f, 0.96f, 1f, Mathf.Clamp01(ringAlpha));
        }

        if (shieldBubbleRenderer != null)
        {
            float baseAlpha = 0.28f + (shieldHitsRemaining * 0.08f) + (shieldRippleTimer * 1.1f);
            Color bubbleCol = (shieldHitsRemaining == 1)
                ? new Color(1f, 0.72f, 0.22f, Mathf.Clamp01(baseAlpha))
                : new Color(0.18f, 0.90f, 1f, Mathf.Clamp01(baseAlpha));
            shieldBubbleRenderer.color = bubbleCol;
        }
    }

    void SpawnDroneParticle(Vector3 pos, Vector2 vel, Color col, float lifetime, float scale)
    {
        GameObject go = new GameObject("DroneMote");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _cachedSparkSprite;
        sr.color = col;
        sr.sortingOrder = 13;

        NexaMoteParticle mote = go.AddComponent<NexaMoteParticle>();
        mote.Init(vel, lifetime);
    }

    void OnGUI()
    {
        if (kaelTarget == null || Camera.main == null) return;
        if (PlayerStats.Instance != null && (PlayerStats.Instance.isPaused || PlayerStats.Instance.isGameOver || PlayerStats.Instance.isLevelCompleted || PlayerStats.Instance.isChoosingDilemma || PlayerStats.Instance.isChoosingSkill))
            return;

        // Mostrar indicador táctico sobre Kael cuando el Protocolo Baluarte está activo o recargándose
        if (!isShieldActive && !isOverheated) return;

        float offsetY = (PlayerStats.Instance != null) ? PlayerStats.Instance.healthBarOffsetY + 0.34f : 2.29f;
        Vector3 worldPos = kaelTarget.position + new Vector3(0f, offsetY, 0f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        if (screenPos.z <= 0f) return;

        float guiY = Screen.height - screenPos.y;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 13;

        string label;
        Color col;
        if (isShieldActive)
        {
            string pips = new string('♦', shieldHitsRemaining) + new string('♢', Mathf.Max(0, maxShieldImpacts - shieldHitsRemaining));
            label = $"🛡 BALUARTE [{pips}]";
            col = new Color(0.25f, 0.96f, 1f);
        }
        else
        {
            label = $"⚠ DRON ENFRIANDO ({Mathf.CeilToInt(overheatTimer)}s)";
            col = new Color(1f, 0.50f, 0.20f);
        }

        Rect r = new Rect(screenPos.x - 95f, guiY - 12f, 190f, 22f);
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), label, style);
        GUI.color = col;
        GUI.Label(r, label, style);
        GUI.color = Color.white;
    }

    // --- CONSTRUCCIÓN VISUAL Y PROCEDURAL DEL DRON Y ESCUDO ---

    void BuildDroneVisualHierarchy()
    {
        chassisRenderer = GetComponent<SpriteRenderer>();
        if (chassisRenderer == null) chassisRenderer = gameObject.AddComponent<SpriteRenderer>();
        chassisRenderer.sprite = _cachedDroneSprite;
        chassisRenderer.sortingOrder = 14;

        // Rotor del núcleo de energía cian giratorio
        Transform existingRotor = transform.Find("CoreRotor");
        if (existingRotor == null)
        {
            GameObject rotorObj = new GameObject("CoreRotor");
            rotorObj.transform.SetParent(transform, false);
            rotorObj.transform.localPosition = new Vector3(-0.032f, 0.079f, 0f);
            coreRotorTransform = rotorObj.transform;
            coreRotorRenderer = rotorObj.AddComponent<SpriteRenderer>();
        }
        else
        {
            coreRotorTransform = existingRotor;
            coreRotorRenderer = existingRotor.GetComponent<SpriteRenderer>();
        }
        coreRotorRenderer.sprite = _cachedCoreRotorSprite;
        coreRotorRenderer.sortingOrder = 15;

        // Propulsor antigravedad izquierdo
        Transform existingLeft = transform.Find("ThrusterLeft");
        if (existingLeft == null)
        {
            GameObject leftObj = new GameObject("ThrusterLeft");
            leftObj.transform.SetParent(transform, false);
            leftObj.transform.localPosition = new Vector3(-0.107f, -0.145f, 0f);
            leftThrusterTransform = leftObj.transform;
            leftThrusterRenderer = leftObj.AddComponent<SpriteRenderer>();
        }
        else
        {
            leftThrusterTransform = existingLeft;
            leftThrusterRenderer = existingLeft.GetComponent<SpriteRenderer>();
        }
        leftThrusterRenderer.sprite = _cachedThrusterFlameSprite;
        leftThrusterRenderer.sortingOrder = 13;

        // Propulsor antigravedad derecho
        Transform existingRight = transform.Find("ThrusterRight");
        if (existingRight == null)
        {
            GameObject rightObj = new GameObject("ThrusterRight");
            rightObj.transform.SetParent(transform, false);
            rightObj.transform.localPosition = new Vector3(0.092f, -0.145f, 0f);
            rightThrusterTransform = rightObj.transform;
            rightThrusterRenderer = rightObj.AddComponent<SpriteRenderer>();
        }
        else
        {
            rightThrusterTransform = existingRight;
            rightThrusterRenderer = existingRight.GetComponent<SpriteRenderer>();
        }
        rightThrusterRenderer.sprite = _cachedThrusterFlameSprite;
        rightThrusterRenderer.sortingOrder = 13;

        // Cañón / Lente emisor de plasma frontal
        Transform existingMuzzle = transform.Find("MuzzlePoint");
        if (existingMuzzle == null)
        {
            GameObject muzzleObj = new GameObject("MuzzlePoint");
            muzzleObj.transform.SetParent(transform, false);
            muzzleObj.transform.localPosition = new Vector3(0.32f, 0.02f, 0f);
            muzzleTransform = muzzleObj.transform;
            muzzleFlashRenderer = muzzleObj.AddComponent<SpriteRenderer>();
        }
        else
        {
            muzzleTransform = existingMuzzle;
            muzzleFlashRenderer = existingMuzzle.GetComponent<SpriteRenderer>();
        }
        muzzleFlashRenderer.sprite = _cachedSparkSprite;
        muzzleFlashRenderer.sortingOrder = 16;
    }

    void BuildShieldBubbleHierarchy()
    {
        shieldBubbleRoot = new GameObject("NexaBulwarkShieldBubble");
        shieldBubbleRenderer = shieldBubbleRoot.AddComponent<SpriteRenderer>();
        shieldBubbleRenderer.sprite = _cachedShieldBubbleSprite;
        shieldBubbleRenderer.sortingOrder = 18;

        GameObject ringObj = new GameObject("ShieldHexRing");
        ringObj.transform.SetParent(shieldBubbleRoot.transform, false);
        shieldHexRingRenderer = ringObj.AddComponent<SpriteRenderer>();
        shieldHexRingRenderer.sprite = _cachedShieldRingSprite;
        shieldHexRingRenderer.sortingOrder = 19;

        shieldBubbleRoot.SetActive(false);
    }

    static void EnsureSpritesGenerated()
    {
        if (_cachedDroneSprite == null)
        {
            _cachedDroneSprite = LoadOrGenerateDroneSprite();
        }
        if (_cachedCoreRotorSprite == null)
        {
            _cachedCoreRotorSprite = CreateCoreRotorSprite(48);
        }
        if (_cachedThrusterFlameSprite == null)
        {
            _cachedThrusterFlameSprite = CreateThrusterFlameSprite(16, 28);
        }
        if (_cachedPlasmaBoltSprite == null)
        {
            _cachedPlasmaBoltSprite = CreatePlasmaBoltSprite(24, 14);
        }
        if (_cachedShieldBubbleSprite == null)
        {
            _cachedShieldBubbleSprite = CreateShieldBubbleSprite(96);
        }
        if (_cachedShieldRingSprite == null)
        {
            _cachedShieldRingSprite = CreateShieldHexRingSprite(96);
        }
        if (_cachedSparkSprite == null)
        {
            _cachedSparkSprite = CreateSparkOrbSprite(10);
        }
    }

    static Sprite LoadOrGenerateDroneSprite()
    {
        Texture2D tex = Resources.Load<Texture2D>("StoryUI/NexaDrone");
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 320f);
        }

        Sprite sp = Resources.Load<Sprite>("StoryUI/NexaDrone");
        if (sp != null) return sp;

        return CreateFallbackPixelDroneSprite(64);
    }

    static Sprite CreateCoreRotorSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = (size - 1) * 0.5f;
        float outerR = size * 0.44f;
        float innerR = size * 0.30f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;

                Color c = Color.clear;

                // Anillo rúnico segmentado en 4 arcos tácticos
                bool inArc = (angle % 90f) > 16f && (angle % 90f) < 74f;
                if (dist >= innerR && dist <= outerR && inArc)
                {
                    c = (dist > outerR - 1.5f) ? Color.white : new Color(0.22f, 0.95f, 1f, 0.85f);
                }
                // Puntas de energía en los 4 ejes cardinales
                else if (dist >= innerR * 0.45f && dist <= outerR + 1.5f && ((angle % 90f) < 7f || (angle % 90f) > 83f))
                {
                    c = new Color(0.85f, 1f, 1f, 0.92f);
                }

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 260f);
    }

    static Sprite CreateThrusterFlameSprite(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float centerX = (w - 1) * 0.5f;

        for (int y = 0; y < h; y++)
        {
            float tY = (float)y / (h - 1); // 0 = punta inferior, 1 = base superior junto a la tobera
            float maxHalfW = Mathf.Lerp(0.8f, w * 0.42f, tY);

            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - centerX);
                if (dx > maxHalfW)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float coreRatio = 1f - (dx / Mathf.Max(0.5f, maxHalfW));
                    Color col = (coreRatio > 0.55f && tY > 0.35f)
                        ? new Color(0.90f, 1f, 1f, tY)
                        : new Color(0.18f, 0.92f, 1f, tY * 0.85f);
                    tex.SetPixel(x, y, col);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1.0f), 140f);
    }

    static Sprite CreatePlasmaBoltSprite(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float cx = w * 0.62f;
        float cy = (h - 1) * 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / (x < cx ? 1.45f : 0.85f);
                float dy = (y - cy) * 1.15f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > h * 0.48f)
                    tex.SetPixel(x, y, Color.clear);
                else if (dist < h * 0.20f)
                    tex.SetPixel(x, y, Color.white);
                else if (dist < h * 0.34f)
                    tex.SetPixel(x, y, new Color(0.22f, 0.96f, 1f, 1f));
                else
                    tex.SetPixel(x, y, new Color(0.08f, 0.58f, 0.95f, 0.75f));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 42f);
    }

    static Sprite CreateShieldBubbleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = (size - 1) * 0.5f;
        float maxR = size * 0.47f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > maxR)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dist > maxR - 2.2f)
                {
                    tex.SetPixel(x, y, new Color(0.65f, 0.98f, 1f, 0.95f));
                }
                else if (dist > maxR - 5.0f)
                {
                    tex.SetPixel(x, y, new Color(0.20f, 0.88f, 1f, 0.55f));
                }
                else
                {
                    // Patrón hexagonal holográfico sutil en el interior de la burbuja
                    bool hexGrid = ((x + y) % 8 == 0) || ((x - y + 128) % 8 == 0);
                    float fillAlpha = hexGrid ? 0.24f : 0.10f;
                    tex.SetPixel(x, y, new Color(0.15f, 0.82f, 1f, fillAlpha));
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 44f);
    }

    static Sprite CreateShieldHexRingSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = (size - 1) * 0.5f;
        float rOuter = size * 0.49f;
        float rInner = size * 0.44f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;

                bool segment = (angle % 60f) > 10f && (angle % 60f) < 50f;
                if (dist >= rInner && dist <= rOuter && segment)
                {
                    tex.SetPixel(x, y, new Color(0.55f, 0.98f, 1f, 0.85f));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 44f);
    }

    static Sprite CreateSparkOrbSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = (size - 1) * 0.5f;
        float r = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (d > r) tex.SetPixel(x, y, Color.clear);
                else if (d < r * 0.45f) tex.SetPixel(x, y, Color.white);
                else tex.SetPixel(x, y, new Color(0.22f, 0.92f, 1f, 1f - (d / r)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    static Sprite CreateFallbackPixelDroneSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float cx = size * 0.5f;
        float cy = size * 0.52f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / 1.35f;
                float dy = (y - cy);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > size * 0.32f)
                    tex.SetPixel(x, y, Color.clear);
                else if (dist < size * 0.10f)
                    tex.SetPixel(x, y, Color.white);
                else if (dist < size * 0.18f)
                    tex.SetPixel(x, y, new Color(0.22f, 0.95f, 1f));
                else if (dist > size * 0.28f)
                    tex.SetPixel(x, y, new Color(0.08f, 0.10f, 0.16f));
                else
                    tex.SetPixel(x, y, new Color(0.26f, 0.32f, 0.42f));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }
}

/// <summary>
/// Proyectil de plasma cian guiado disparado por el dron N.E.X.A.
/// Persigue dinámicamente al enemigo objetivo e impacta a caminantes, acechadores, brutos y jefes.
/// </summary>
public class NexaGuidedPlasmaBolt : MonoBehaviour
{
    private EnemyDummy target;
    private float damage;
    private bool isCrit;
    private float speed;
    private float turnSpeed;
    private float searchRadius;
    private Sprite trailSprite;

    private Vector2 moveDir = Vector2.right;
    private float lifetime = 3.5f;
    private float elapsed = 0f;
    private float trailTimer = 0f;

    public void Init(EnemyDummy initialTarget, float dmg, bool crit, float spd, float turnSpd, float radius, Sprite sparkSpr)
    {
        target = initialTarget;
        damage = dmg;
        isCrit = crit;
        speed = spd;
        turnSpeed = turnSpd;
        searchRadius = radius;
        trailSprite = sparkSpr;

        if (target != null)
        {
            Vector2 toTarget = (target.transform.position + Vector3.up * 0.55f) - transform.position;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                moveDir = toTarget.normalized;
            }
        }

        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        if (PlayerStats.Instance != null && (PlayerStats.Instance.isPaused || PlayerStats.Instance.isGameOver || PlayerStats.Instance.isLevelCompleted || PlayerStats.Instance.isChoosingDilemma || PlayerStats.Instance.isChoosingSkill))
            return;

        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Si el enemigo objetivo murió en pleno vuelo, redirigir al siguiente enemigo vivo más cercano
        if (target == null || target.IsDead)
        {
            target = FindAlternativeTarget();
        }

        if (target != null && !target.IsDead)
        {
            Vector3 aimPos = target.transform.position + Vector3.up * 0.55f;
            Vector2 desiredDir = ((Vector2)(aimPos - transform.position)).normalized;
            float currentAngle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            float targetAngle = Mathf.Atan2(desiredDir.y, desiredDir.x) * Mathf.Rad2Deg;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
            float rad = newAngle * Mathf.Deg2Rad;
            moveDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            transform.rotation = Quaternion.Euler(0f, 0f, newAngle);

            // Verificar impacto directo con el enemigo objetivo
            if (Vector2.Distance(transform.position, aimPos) <= 0.45f)
            {
                ImpactEnemy(target);
                return;
            }
        }

        transform.position += (Vector3)(moveDir * speed * Time.deltaTime);

        // Estela de plasma cian
        trailTimer -= Time.deltaTime;
        if (trailTimer <= 0f && trailSprite != null)
        {
            trailTimer = 0.03f;
            GameObject trail = new GameObject("PlasmaTrail");
            trail.transform.position = transform.position;
            trail.transform.localScale = Vector3.one * 0.65f;
            SpriteRenderer sr = trail.AddComponent<SpriteRenderer>();
            sr.sprite = trailSprite;
            sr.color = new Color(0.22f, 0.95f, 1f, 0.75f);
            sr.sortingOrder = 21;
            NexaMoteParticle mote = trail.AddComponent<NexaMoteParticle>();
            mote.Init(-moveDir * 0.6f, 0.16f);
        }

        // Verificar si impacta a cualquier otro enemigo en su trayectoria
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.28f);
        if (hit != null)
        {
            EnemyDummy hitEnemy = hit.GetComponent<EnemyDummy>();
            if (hitEnemy != null && !hitEnemy.IsDead)
            {
                ImpactEnemy(hitEnemy);
            }
        }
    }

    EnemyDummy FindAlternativeTarget()
    {
        EnemyDummy[] enemies = FindObjectsByType<EnemyDummy>();
        EnemyDummy closest = null;
        float bestDist = searchRadius * 1.4f;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && !enemies[i].IsDead)
            {
                float d = Vector2.Distance(transform.position, enemies[i].transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    closest = enemies[i];
                }
            }
        }
        return closest;
    }

    void ImpactEnemy(EnemyDummy enemy)
    {
        if (enemy != null && !enemy.IsDead)
        {
            enemy.TakeDamage(damage, isCrit, knockback: 0.06f);

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnHitSparks(transform.position, isCrit, isCrit ? 7 : 4);
                CombatVFXManager.Instance.RegisterComboHit();
            }

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddUltCharge(3.5f);
            }
        }

        Destroy(gameObject);
    }
}

/// <summary>
/// Partícula ligera para estelas de plasma y propulsores antigravedad del Dron N.E.X.A.
/// </summary>
public class NexaMoteParticle : MonoBehaviour
{
    private Vector2 velocity;
    private float lifetime;
    private float elapsed = 0f;
    private Vector3 initialScale;
    private SpriteRenderer sr;
    private Color initialColor;

    public void Init(Vector2 vel, float life)
    {
        velocity = vel;
        lifetime = Mathf.Max(0.05f, life);
        initialScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) initialColor = sr.color;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
        if (sr != null)
        {
            sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, initialColor.a * (1f - t));
        }
        if (t >= 1f) Destroy(gameObject);
    }
}
