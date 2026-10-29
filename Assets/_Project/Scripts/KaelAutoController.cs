using UnityEngine;
using System.Collections;

public class KaelAutoController : MonoBehaviour
{
    [Header("Movimiento y Detección")]
    public float moveSpeed = 3.6f;
    public float attackRange = 1.75f;
    public float aoeSplashRange = 2.8f;
    public float ultimateRange = 12.0f; // Abarca toda la pantalla hacia la derecha
    public LayerMask enemyLayer;

    [Header("Ajuste de Tamaño por Animación (En Vivo)")]
    [Range(0.5f, 1.5f)] public float scaleIdle = 0.88f;
    [Range(0.5f, 1.5f)] public float scaleRun = 0.78f;
    [Range(0.5f, 1.5f)] public float scaleAttack = 1.00f;
    [Range(0.5f, 1.5f)] public float scaleUlt = 1.24f; // Un poco más imponente durante la Ultimate

    private Animator anim;
    private float nextAttackTime;
    private int currentCombo = 0;
    private bool isCastingUlt = false;
    private EnemyDummy targetEnemy;
    private Vector3 startPosition;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (transform.position.y < -3.1f)
        {
            transform.position = new Vector3(transform.position.x, -2.95f, transform.position.z);
        }
        startPosition = transform.position;
    }

    public void ResetKaelPosition()
    {
        StopAllCoroutines();
        isCastingUlt = false;
        currentCombo = 0;
        transform.position = startPosition;

        if (CombatVFXManager.Instance != null)
            CombatVFXManager.Instance.SetUltimateCinematic(false);

        if (anim != null)
        {
            anim.speed = 1f;
            anim.ResetTrigger("Ultimate");
            anim.SetInteger("ComboStep", 0);
            anim.SetBool("IsRunning", true);
        }
    }

    void Update()
    {
        if (PlayerStats.Instance != null && (PlayerStats.Instance.isPaused || PlayerStats.Instance.isGameOver || PlayerStats.Instance.isLevelCompleted || PlayerStats.Instance.isChoosingDilemma))
            return;

        if (isCastingUlt) return;

        FindClosestEnemy();

        if (targetEnemy == null)
        {
            anim.SetInteger("ComboStep", 0);
            anim.SetBool("IsRunning", true);
            transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
        }
        else
        {
            anim.SetBool("IsRunning", false);

            float rate = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackRate : 1.2f;
            if (Time.time >= nextAttackTime)
            {
                ExecuteComboStep();
                nextAttackTime = Time.time + (1f / rate);
            }
        }
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

    void FindClosestEnemy()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.up * 0.5f, Vector2.right, attackRange, enemyLayer);
        targetEnemy = (hit.collider != null) ? hit.collider.GetComponent<EnemyDummy>() : null;
    }

    void ExecuteComboStep()
    {
        currentCombo = (currentCombo % 3) + 1;
        anim.SetInteger("ComboStep", currentCombo);

        float baseDmg = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackDamage : 12f;
        bool isCrit = (PlayerStats.Instance != null && Random.value <= PlayerStats.Instance.critChance);
        float critMult = isCrit ? 2.0f : 1.0f;

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.RegisterComboHit();
        }

        if (LevelAudioManager.Instance != null)
        {
            LevelAudioManager.Instance.PlayPunch(currentCombo == 3);
        }

        if (currentCombo < 3)
        {
            if (targetEnemy != null)
            {
                targetEnemy.TakeDamage(baseDmg * critMult, isCrit, knockback: 0.08f);
                if (CombatVFXManager.Instance != null)
                {
                    CombatVFXManager.Instance.SpawnHitSparks(targetEnemy.transform.position + Vector3.up * 0.5f, isCrit, isCrit ? 8 : 4);
                    if (isCrit) CombatVFXManager.Instance.ShakeCamera(0.07f, 0.05f);
                }
            }
        }
        else
        {
            // 3er Golpe al piso: Daño en área + Chispas sísmicas en el suelo
            float slamDamage = baseDmg * 1.6f * critMult;
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position + Vector3.up * 0.5f, Vector2.right, aoeSplashRange, enemyLayer);

            foreach (RaycastHit2D hit in hits)
            {
                EnemyDummy enemy = hit.collider.GetComponent<EnemyDummy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(slamDamage, isCrit, knockback: 0.35f);
                }
            }

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.SpawnHitSparks(transform.position + new Vector3(1.1f, 0.1f, 0f), true, 12);
                CombatVFXManager.Instance.ShakeCamera(0.14f, 0.09f);
            }
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddUltCharge(11f);
        }
    }

    public void TriggerUltimate()
    {
        if (isCastingUlt || PlayerStats.Instance == null) return;
        if (!PlayerStats.Instance.ConsumeUltCharge()) return;

        StartCoroutine(CastUltimateRoutine());
    }

    // SECUENCIA CINEMATOGRÁFICA DE ULTIMATE EN 3 FASES (~1.65 SEGUNDOS)
    IEnumerator CastUltimateRoutine()
    {
        isCastingUlt = true;

        anim.ResetTrigger("Ultimate");
        anim.SetBool("IsRunning", true);
        anim.SetInteger("ComboStep", 0);
        anim.SetTrigger("Ultimate");

        // Ralentizar el Animator de Kael (0.22x) para que la pose de carga del Guantelete dure y se aprecie
        anim.speed = 0.22f;

        Vector3 gauntletPos = transform.position + new Vector3(0.85f, 0.65f, 0f);

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
        anim.speed = 0.35f; // Avanza lentamente por los cuadros del disparo láser

        if (LevelAudioManager.Instance != null)
        {
            LevelAudioManager.Instance.PlayUltimateLaser();
        }

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnUltimateMegaBeam(gauntletPos, ultimateRange, 0.85f);
            CombatVFXManager.Instance.ShakeCamera(0.85f, 0.20f);
        }

        // 5 impactos consecutivos cada 0.16s (95% ATK por impacto = 475% de daño total en área)
        float hitDamage = PlayerStats.Instance.attackDamage * 0.95f;
        for (int wave = 0; wave < 5; wave++)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position + Vector3.up * 0.5f, Vector2.right, ultimateRange, enemyLayer);
            foreach (RaycastHit2D hit in hits)
            {
                EnemyDummy enemy = hit.collider.GetComponent<EnemyDummy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(hitDamage, isCrit: true, knockback: 0.22f);
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

        anim.speed = 1f; // Restaurar velocidad normal de animación
        anim.ResetTrigger("Ultimate");

        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SetUltimateCinematic(false, triggerFlash: true);
        }

        isCastingUlt = false;
    }
}