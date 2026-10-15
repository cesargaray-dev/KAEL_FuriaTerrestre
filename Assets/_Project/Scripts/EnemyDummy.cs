using UnityEngine;
using System.Collections;
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
        if (PlayerStats.Instance != null && (PlayerStats.Instance.isPaused || PlayerStats.Instance.isGameOver || PlayerStats.Instance.isLevelCompleted || PlayerStats.Instance.isChoosingDilemma))
            return;

        float distToKael = Mathf.Abs(transform.position.x - kaelTarget.position.x);

        if (distToKael > attackRange && !isChargingHeavy)
        {
            if (anim != null) anim.SetBool("IsAttacking", false);
            transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
        }
        else
        {
            if (anim != null) anim.SetBool("IsAttacking", true);

            if (!isChargingHeavy && Time.time >= nextAttackTime)
            {
                attackCounter++;
                // Cada 3 ataques, el Mini-Jefe o Jefe prepara una Embestida Telegrafiada (GDD Tabla 10)
                if ((isMiniBoss || isBoss) && attackCounter % 3 == 0)
                {
                    StartCoroutine(TelegraphedHeavyAttack());
                }
                else
                {
                    nextAttackTime = Time.time + (1f / attackRate);
                    if (PlayerStats.Instance != null)
                    {
                        PlayerStats.Instance.TakeDamage(damage);
                    }
                }
            }
        }
    }

    IEnumerator TelegraphedHeavyAttack()
    {
        isChargingHeavy = true;
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.4f, "⚠ ¡CARGANDO EMBESTIDA!", new Color(1f, 0.4f, 0.1f), 17, 1.0f);
        }

        // Parpadeo naranja de advertencia durante 1.0s (Señal antes de atacar del GDD)
        float elapsed = 0f;
        while (elapsed < 1.0f)
        {
            if (isDead) yield break;
            if (sr != null)
                sr.color = (Mathf.FloorToInt(elapsed * 12f) % 2 == 0) ? new Color(1f, 0.5f, 0.1f) : originalColor;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (sr != null) sr.color = originalColor;

        if (!isDead && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.TakeDamage(damage * 2.0f); // Doble de daño en embestida
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.ShakeCamera(0.18f, 0.12f);
            }
        }

        nextAttackTime = Time.time + (1.2f / attackRate);
        isChargingHeavy = false;
    }

    public void TakeDamage(float dmg, bool isCrit = false, float knockback = 0f)
    {
        if (isDead) return;

        currentHP = Mathf.Max(0f, currentHP - dmg);
        if (sr != null && !isChargingHeavy) StartCoroutine(HitFlash());

        // Ligero retroceso físico al recibir golpes fuertes (los jefes resisten más el empuje)
        if (knockback > 0f)
        {
            float resist = (isBoss || isMiniBoss) ? 0.25f : 1.0f;
            transform.Translate(Vector2.right * knockback * resist);
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

    IEnumerator BossDramaticDeathRoutine()
    {
        isDead = true;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (anim != null) anim.SetBool("IsAttacking", false);

        Time.timeScale = 0.35f;
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.ShakeCamera(1.4f, 0.20f);
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.5f, "¡COMANDANTE VORST ABATIDO!", Color.cyan, 26, 1.5f);
        }

        Vector3 basePos = transform.position;

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

        if (anim != null)
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

        if (isBoss || isMiniBoss)
        {
            float bossW = 440f;
            float bossH = 22f;
            float bx = (Screen.width / 2f) - (bossW / 2f);
            float by = 28f;

            GUI.color = new Color(0.05f, 0.02f, 0.08f, 0.95f);
            GUI.DrawTexture(new Rect(bx - 4, by - 26, bossW + 8, bossH + 34), Texture2D.whiteTexture);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 15;
            titleStyle.normal.textColor = isBoss ? new Color(1f, 0.35f, 0.55f) : new Color(1f, 0.65f, 0.2f);
            GUI.color = Color.white;
            GUI.Label(new Rect(bx, by - 24, bossW, 22), $"☠ {enemyTitle.ToUpper()} ☠", titleStyle);

            GUI.color = new Color(0.25f, 0.05f, 0.12f);
            GUI.DrawTexture(new Rect(bx, by, bossW, bossH), Texture2D.whiteTexture);

            float bossFill = Mathf.Clamp01(currentHP / maxHP);
            GUI.color = isBoss ? new Color(0.95f, 0.15f, 0.35f) : new Color(0.95f, 0.5f, 0.1f);
            GUI.DrawTexture(new Rect(bx, by, bossW * bossFill, bossH), Texture2D.whiteTexture);

            titleStyle.fontSize = 13;
            titleStyle.normal.textColor = Color.white;
            GUI.color = Color.white;
            GUI.Label(new Rect(bx, by, bossW, bossH), $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)} HP", titleStyle);
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