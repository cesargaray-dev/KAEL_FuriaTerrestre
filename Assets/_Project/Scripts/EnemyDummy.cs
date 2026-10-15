using UnityEngine;
using System.Collections;
using System;

public class EnemyDummy : MonoBehaviour
{
    [Header("Estadísticas Vorst")]
    public float maxHP = 30f;
    public float moveSpeed = 2.2f;
    public float damage = 3f;
    public float attackRange = 1.4f;
    public float attackRate = 1.0f;

    [Header("Recompensas al Morir")]
    public int goldReward = 5;
    public int expReward = 10;

    // Evento para avisarle al WaveSpawner que este enemigo murió
    public event Action OnEnemyDied;

    private float currentHP;
    private float nextAttackTime;
    private bool isDead = false;
    private Transform kaelTarget;
    private SpriteRenderer sr;
    private Animator anim;
    private Color originalColor;

    void Start()
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
            // Avanza hacia la izquierda buscando a Kael
            if (anim != null) anim.SetBool("IsAttacking", false);
            transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
        }
        else
        {
            // Se detiene y ataca a Kael cuando está en rango
            if (anim != null) anim.SetBool("IsAttacking", true);

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + (1f / attackRate);
                if (PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.TakeDamage(damage);
                }
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

        // Entregar Oro y EXP al jugador (+15% de carga de Ultimate incluida en AddRewards)
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddRewards(goldReward, expReward);
        }

        // Avisar al generador de oleadas que murió un enemigo
        OnEnemyDied?.Invoke();

        // Desactivar el collider de inmediato para que Kael pueda seguir caminando
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Reproducir animación de explosión/muerte antes de destruir el objeto
        if (anim != null)
        {
            anim.SetTrigger("Die");
            Destroy(gameObject, 0.5f);
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