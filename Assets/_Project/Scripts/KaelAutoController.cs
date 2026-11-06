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
    }

    public void ResetKaelPosition()
    {
        StopAllCoroutines();
        isCastingUlt = false;
        isGuarding = false;
        isFacingLeft = false;
        isMovingManual = false;
        currentCombo = 0;
        lastAttackTime = -99f;
        attackAnimEndTimer = 0f;

        transform.position = startPosition;
        maxReachedX = startPosition.x;

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

    void Update()
    {
        // Atajos globales para abrir/cerrar el Árbol de Mejoras (TAB o M) o Pausa (ESC)
        if (PlayerStats.Instance != null)
        {
            if (WasUpgradeTreeShortcutPressed())
            {
                PlayerStats.Instance.ToggleUpgradeTreeModal();
                return;
            }
            if (WasEscapePressed())
            {
                if (PlayerStats.Instance.isUpgradeTreeOpen || PlayerStats.Instance.isChoosingSkill)
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

        if (PlayerStats.Instance != null &&
            (PlayerStats.Instance.isPaused ||
             PlayerStats.Instance.isGameOver ||
             PlayerStats.Instance.isLevelCompleted ||
             PlayerStats.Instance.isChoosingDilemma ||
             PlayerStats.Instance.isChoosingSkill ||
             PlayerStats.Instance.isUpgradeTreeOpen))
        {
            if (anim != null) anim.SetBool("IsRunning", false);
            return;
        }

        if (isCastingUlt) return;

        // Reiniciar el parámetro ComboStep del Animator cuando concluye el swing actual
        if (attackAnimEndTimer > 0f)
        {
            attackAnimEndTimer -= Time.deltaTime;
            if (attackAnimEndTimer <= 0f && anim != null)
            {
                anim.SetInteger("ComboStep", 0);
            }
        }

        // Si pasó la ventana de 1.2s desde el último golpe, el contador de combo vuelve a 0
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

        // 3. POSTURA DEFENSIVA / GUARDIA NEXO: Mantener S o Flecha Abajo
        bool guardInput = IsGuardHeld() || hudGuardHeld;
        if (guardInput)
        {
            isGuarding = true;
            isMovingManual = false;
            if (anim != null)
            {
                if (attackAnimEndTimer <= 0f) anim.SetInteger("ComboStep", 0);
                anim.SetBool("IsRunning", false);
            }

            if (spriteRenderer != null)
            {
                float pulse = 0.82f + Mathf.Sin(Time.time * 10f) * 0.12f;
                spriteRenderer.color = new Color(0.45f, 0.94f, 1.0f, pulse);
            }
            return;
        }
        else if (isGuarding)
        {
            isGuarding = false;
            if (spriteRenderer != null) spriteRenderer.color = baseSpriteColor;
        }

        // 4. MOVIMIENTO MANUAL: A / Flecha Izquierda y D / Flecha Derecha
        float moveDir = 0f;
        if (IsLeftHeld() || hudMoveLeftHeld) moveDir -= 1f;
        if (IsRightHeld() || hudMoveRightHeld) moveDir += 1f;

        // Permitir moverse siempre que no esté a mitad del impacto inicial del golpe
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
            float effectiveSpeed = moveSpeed * speedBonus;

            Vector3 delta = (moveDir < 0f ? Vector3.left : Vector3.right) * (effectiveSpeed * Time.deltaTime);
            Vector3 nextPos = transform.position + delta;

            // Limitar posición izquierda para que no se salga demasiado atrás de la cámara
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

    public void TriggerManualAttack()
    {
        if (isCastingUlt) return;
        if (PlayerStats.Instance != null &&
            (PlayerStats.Instance.isPaused ||
             PlayerStats.Instance.isGameOver ||
             PlayerStats.Instance.isLevelCompleted ||
             PlayerStats.Instance.isChoosingDilemma ||
             PlayerStats.Instance.isChoosingSkill ||
             PlayerStats.Instance.isUpgradeTreeOpen))
            return;

        float rate = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackRate : 1.2f;
        if (Time.time < nextAttackTime) return;

        isGuarding = false;
        if (spriteRenderer != null) spriteRenderer.color = baseSpriteColor;

        ExecuteComboStep();
        nextAttackTime = Time.time + (1f / Mathf.Max(0.5f, rate));
    }

    void LateUpdate()
    {
        if (anim == null) return;

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

    void ExecuteComboStep()
    {
        // Si presiona dentro de la ventana de 1.2s tras el golpe anterior, avanza el combo (1 -> 2 -> 3); si no, reinicia a 1
        if (Time.time - lastAttackTime <= COMBO_WINDOW && currentCombo > 0)
        {
            currentCombo = (currentCombo % 3) + 1;
        }
        else
        {
            currentCombo = 1;
        }

        lastAttackTime = Time.time;

        float rate = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackRate : 1.2f;
        attackAnimEndTimer = Mathf.Clamp(0.42f / Mathf.Max(1f, rate * 0.75f), 0.24f, 0.44f);

        if (anim != null)
        {
            anim.SetBool("IsRunning", false);
            anim.SetInteger("ComboStep", currentCombo);
            string stateName = (currentCombo == 1) ? "Kael_Atk1" : (currentCombo == 2 ? "Kael_Atk2" : "Kael_Atk3");
            anim.Play(stateName, 0, 0f);
        }

        float baseDmg = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackDamage : 12f;
        bool isCrit = (PlayerStats.Instance != null && Random.value <= PlayerStats.Instance.critChance);
        float critMult = isCrit ? 2.0f : 1.0f;

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

        //Pequeño avance táctico al golpear para dar sensación de peso Action RPG
        transform.position += new Vector3(dirSign * (currentCombo == 3 ? 0.14f : 0.06f), 0f, 0f);
        if (transform.position.x > maxReachedX) maxReachedX = transform.position.x;

        bool hitAnyEnemy = false;

        if (currentCombo < 3)
        {
            List<EnemyDummy> targets = FindEnemiesInAttackArc(attackRange, facingDir);
            if (targets.Count > 0)
            {
                // Golpe 1 golpea hasta 2 enemigos cercanos al frente; Golpe 2 hasta 3 enemigos
                int maxTargets = (currentCombo == 1) ? 2 : 3;
                int hitCount = Mathf.Min(targets.Count, maxTargets);
                for (int i = 0; i < hitCount; i++)
                {
                    EnemyDummy target = targets[i];
                    if (target != null)
                    {
                        target.TakeDamage(baseDmg * critMult, isCrit, knockback: 0.12f);
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
                // Chispas de energía del Guantelete al golpear al aire para dar respuesta inmediata
                if (CombatVFXManager.Instance != null)
                {
                    CombatVFXManager.Instance.SpawnHitSparks(transform.position + new Vector3(dirSign * 0.95f, 0.55f, 0f), false, 3);
                }
            }
        }
        else
        {
            // 3er Golpe Sísmico en área + empuje fuerte
            float slamBonus = (PlayerStats.Instance != null) ? PlayerStats.Instance.comboFinisherMultiplier : 1.6f;
            float slamDamage = baseDmg * slamBonus * critMult;

            List<EnemyDummy> targets = FindEnemiesInAttackArc(aoeSplashRange, facingDir);
            foreach (EnemyDummy enemy in targets)
            {
                if (enemy != null)
                {
                    enemy.TakeDamage(slamDamage, isCrit, knockback: 0.42f);
                    hitAnyEnemy = true;
                    if (CombatVFXManager.Instance != null)
                    {
                        CombatVFXManager.Instance.RegisterComboHit();
                    }
                }
            }

            if (CombatVFXManager.Instance != null)
            {
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

        // 1. RaycastAll y OverlapBoxAll en la dirección en la que mira Kael
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

        // 2. Verificación directa por posición sobre todos los enemigos activos en la escena (garantiza 100% compatibilidad en Nivel 1 y Nivel 2)
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

        // Ordenar por cercanía a Kael
        result.Sort((a, b) =>
            Mathf.Abs(a.transform.position.x - transform.position.x)
            .CompareTo(Mathf.Abs(b.transform.position.x - transform.position.x)));

        return result;
    }

    public void TriggerUltimate()
    {
        if (isCastingUlt || PlayerStats.Instance == null) return;
        if (PlayerStats.Instance.ultCharge < 100f) return;
        if (!PlayerStats.Instance.ConsumeUltCharge()) return;

        StartCoroutine(CastUltimateRoutine());
    }

    // SECUENCIA CINEMATOGRÁFICA DE ULTIMATE EN 3 FASES (~1.65 SEGUNDOS)
    public IEnumerator CastUltimateRoutine()
    {
        isCastingUlt = true;
        isGuarding = false;
        isMovingManual = false;
        if (spriteRenderer != null) spriteRenderer.color = baseSpriteColor;

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
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.55f, "⚡ ¡CARGANDO CAÑÓN NEXO! ⚡", Color.cyan, 22, 0.7f);
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
        float hitDamage = ((PlayerStats.Instance != null) ? PlayerStats.Instance.attackDamage : 12f) * 0.95f * ultBonus;

        for (int wave = 0; wave < 5; wave++)
        {
            List<EnemyDummy> hits = FindEnemiesInAttackArc(ultimateRange, facingDir);
            foreach (EnemyDummy enemy in hits)
            {
                if (enemy != null)
                {
                    enemy.TakeDamage(hitDamage, isCrit: true, knockback: 0.24f);
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
