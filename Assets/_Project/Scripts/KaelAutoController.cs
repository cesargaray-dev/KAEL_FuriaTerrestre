using UnityEngine;
using System.Collections;

public class KaelAutoController : MonoBehaviour
{
    [Header("Movimiento y Detección")]
    public float moveSpeed = 3.5f;
    public float attackRange = 1.8f;
    public float ultimateRange = 9.0f;
    public LayerMask enemyLayer;

    [Header("Ajuste de Tamaño por Animación (En Vivo)")]
    [Range(0.5f, 1.5f)] public float scaleIdle = 0.88f;
    [Range(0.5f, 1.5f)] public float scaleRun = 0.78f;    // Reducido para que no se vea grande al correr
    [Range(0.5f, 1.5f)] public float scaleAttack = 1.00f; // Tamaño base al golpear
    [Range(0.5f, 1.5f)] public float scaleUlt = 1.18f;    // Aumentado para compensar la fila 6

    private Animator anim;
    private float nextAttackTime;
    private int currentCombo = 0;
    private bool isCastingUlt = false;
    private EnemyDummy targetEnemy;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
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

        // Detecta qué animación se está reproduciendo (o a cuál está cambiando) en este instante exacto
        AnimatorStateInfo state = anim.IsInTransition(0) 
            ? anim.GetNextAnimatorStateInfo(0) 
            : anim.GetCurrentAnimatorStateInfo(0);

        float s = scaleAttack;

        if (state.IsName("Kael_Run"))
        {
            s = scaleRun;
        }
        else if (state.IsName("Kael_Ultimate"))
        {
            s = scaleUlt;
        }
        else if (state.IsName("Kael_Idle"))
        {
            s = scaleIdle;
        }

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

        float dmg = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackDamage : 12f;
        float multiplier = (currentCombo == 3) ? 1.5f : 1.0f;

        if (targetEnemy != null)
        {
            targetEnemy.TakeDamage(dmg * multiplier);

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddUltCharge(12f);
            }
        }
    }

    public void TriggerUltimate()
    {
        if (isCastingUlt || PlayerStats.Instance == null) return;
        if (!PlayerStats.Instance.ConsumeUltCharge()) return;

        StartCoroutine(CastUltimateRoutine());
    }

    IEnumerator CastUltimateRoutine()
    {
        isCastingUlt = true;

        anim.ResetTrigger("Ultimate");
        anim.SetBool("IsRunning", true);
        anim.SetInteger("ComboStep", 0);
        anim.SetTrigger("Ultimate");

        yield return new WaitForSeconds(0.22f);

        float ultDamage = PlayerStats.Instance.attackDamage * 3.5f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position + Vector3.up * 0.5f, Vector2.right, ultimateRange, enemyLayer);

        foreach (RaycastHit2D hit in hits)
        {
            EnemyDummy enemy = hit.collider.GetComponent<EnemyDummy>();
            if (enemy != null)
            {
                enemy.TakeDamage(ultDamage);
            }
        }

        yield return new WaitForSeconds(0.25f);

        anim.ResetTrigger("Ultimate");
        isCastingUlt = false;
    }
}