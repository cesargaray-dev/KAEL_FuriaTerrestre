using UnityEngine;
using System.Collections;

public class EnemyDummy : MonoBehaviour
{
    [Header("Estadísticas Vorst (GDD Tabla 2.3)")]
    public float maxHP = 30f;      // Vida base del Caminante Vorst
    public float moveSpeed = 2.2f; // Velocidad rápida hacia la izquierda
    public float damage = 3f;      // Daño por mordida
    public float attackRange = 1.4f;
    public float attackRate = 1.0f;
    public int goldReward = 5;     // Oro que suelta al morir

    private float currentHP;
    private float nextAttackTime;
    private bool isDead = false;
    private Transform kaelTarget;
    private SpriteRenderer sr;
    private Animator anim;
    private Color originalColor;

    void Awake()
    {
        currentHP = maxHP;
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        if (sr != null) originalColor = sr.color;

        KaelAutoController kael = FindFirstObjectByType<KaelAutoController>();
        if (kael != null) kaelTarget = kael.transform;
    }

    void Update()
    {
        if (isDead || kaelTarget == null) return;

        float distToKael = Mathf.Abs(transform.position.x - kaelTarget.position.x);

        if (distToKael > attackRange)
        {
            // Camina hacia la izquierda buscando a Kael
            if (anim != null) anim.SetBool("IsAttacking", false);
            transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
        }
        else
        {
            // Está en rango de mordida: se detiene y ataca a Kael
            if (anim != null) anim.SetBool("IsAttacking", true);

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + (1f / attackRate);
                // Aquí conectaremos el daño a la barra de vida de Kael en el siguiente paso
            }
        }
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHP -= dmg;
        if (sr != null) StartCoroutine(HitFlash());

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // Para que Kael pueda seguir avanzando de inmediato

        if (anim != null)
        {
            anim.SetTrigger("Die");
            Destroy(gameObject, 0.5f); // Espera medio segundo para mostrar la animación de muerte
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator HitFlash()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (sr != null) sr.color = originalColor;
    }
}