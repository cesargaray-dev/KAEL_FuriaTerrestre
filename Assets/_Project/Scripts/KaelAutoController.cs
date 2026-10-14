using UnityEngine;

public class KaelAutoController : MonoBehaviour
{
    [Header("Movimiento y Detección")]
    public float moveSpeed = 3.5f;
    public float attackRange = 1.8f;
    public LayerMask enemyLayer;

    [Header("Estadísticas de Combate")]
    public float baseDamage = 12f;
    public float attackRate = 1.2f; // Ataques por segundo

    private Animator anim;
    private float nextAttackTime;
    private int currentCombo = 0;
    private EnemyDummy targetEnemy;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        FindClosestEnemy();

        if (targetEnemy == null)
        {
            // No hay enemigo en rango: Kael avanza hacia la derecha
            anim.SetInteger("ComboStep", 0);
            anim.SetBool("IsRunning", true);
            transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
        }
        else
        {
            // Enemigo enfrente: se detiene y ejecuta el combo 1-2-3
            anim.SetBool("IsRunning", false);

            if (Time.time >= nextAttackTime)
            {
                ExecuteComboStep();
                nextAttackTime = Time.time + (1f / attackRate);
            }
        }
    }

    void FindClosestEnemy()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.up * 0.5f, Vector2.right, attackRange, enemyLayer);
        if (hit.collider != null)
        {
            targetEnemy = hit.collider.GetComponent<EnemyDummy>();
        }
        else
        {
            targetEnemy = null;
        }
    }

    void ExecuteComboStep()
    {
        currentCombo = (currentCombo % 3) + 1; // Cicla 1 -> 2 -> 3 -> 1
        anim.SetInteger("ComboStep", currentCombo);

        if (targetEnemy != null)
        {
            // El 3er golpe (golpe al suelo) hace 50% más de daño
            float multiplier = (currentCombo == 3) ? 1.5f : 1.0f;
            targetEnemy.TakeDamage(baseDamage * multiplier);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawLine(start, start + Vector3.right * attackRange);
    }
}