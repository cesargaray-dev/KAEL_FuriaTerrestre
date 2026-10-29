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

    // Cache estático en memoria para que no haya NINGÚN tirón ni lectura de disco al spawnear enemigos en nuevas oleadas
    private static readonly Dictionary<string, Sprite[]> _spriteFrameCache = new Dictionary<string, Sprite[]>();
    private static bool _preloaded = false;

    // Sprites 256x256 alineados para el Nivel 2 (Stalker, Brute, Archon)
    private string visualType = "";
    private Sprite[] customWalkFrames;
    private Sprite[] customAttackFrames;
    private Sprite[] customDieFrames;
    private bool useCustomSprites = false;
    private float walkAnimTimer = 0f;
    private float dieAnimTimer = 0f;
    private float baseGroundY = -2.85f;
    private float attackLungeOffsetX = 0f;

    public static void PreloadLevel2Frames()
    {
        if (_preloaded) return;
        _preloaded = true;

        PreloadSet("Stalker", 160f);
        PreloadSet("Brute", 122f);
        PreloadSet("Archon", 118f);
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

        KaelAutoController kael = FindFirstObjectByType<KaelAutoController>();
        if (kael != null) kaelTarget = kael.transform;
    }

    public void SetupCustomVisuals(string resourcePrefix)
    {
        visualType = resourcePrefix;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (anim == null) anim = GetComponent<Animator>();

        // Todos los cuadros están en lienzos 256x256 Power-of-2 para evitar cualquier distorsión
        float ppu = 160f;
        if (resourcePrefix == "Brute") ppu = 122f;
        else if (resourcePrefix == "Archon") ppu = 118f;

        // Carga O(1) instantánea desde la memoria caché estática (cero tirones en cada oleada)
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

    void Update()
    {
        if (isDead || kaelTarget == null) return;
        if (PlayerStats.Instance != null && (PlayerStats.Instance.isPaused || PlayerStats.Instance.isGameOver || PlayerStats.Instance.isLevelCompleted || PlayerStats.Instance.isChoosingDilemma))
            return;

        float distToKael = Mathf.Abs(transform.position.x - kaelTarget.position.x);

        if (distToKael > attackRange && !isChargingHeavy && !isPlayingAttackAnim)
        {
            if (anim != null && !useCustomSprites) anim.SetBool("IsAttacking", false);
            transform.Translate(Vector2.left * moveSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            if (anim != null && !useCustomSprites) anim.SetBool("IsAttacking", true);

            if (!isChargingHeavy && !isPlayingAttackAnim && Time.time >= nextAttackTime)
            {
                attackCounter++;
                if ((isMiniBoss || isBoss) && attackCounter % 3 == 0)
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
                        PlayerStats.Instance.TakeDamage(damage);
                    }
                }
            }
        }
    }

    // Secuencia de ataque fluida en 8 cuadros (preparación, embestida, impacto y recuperación)
    IEnumerator PlayVisibleAttackRoutine()
    {
        isPlayingAttackAnim = true;
        transform.localRotation = Quaternion.identity;
        float baseX = transform.position.x;
        float lungeMax = (visualType == "Archon") ? 0.14f : 0.34f;
        int totalFrames = (customAttackFrames != null && customAttackFrames.Length > 0) ? customAttackFrames.Length : 8;

        for (int f = 0; f < totalFrames; f++)
        {
            if (isDead) yield break;
            if (customAttackFrames != null && f < customAttackFrames.Length && customAttackFrames[f] != null)
                sr.sprite = customAttackFrames[f];

            float curve = Mathf.Sin((f / (float)Mathf.Max(1, totalFrames - 1)) * Mathf.PI);
            transform.position = new Vector3(baseX - lungeMax * curve, baseGroundY, transform.position.z);

            if (f == 3)
            {
                if (LevelAudioManager.Instance != null)
                    LevelAudioManager.Instance.PlayEnemyAttack();

                if (PlayerStats.Instance != null)
                    PlayerStats.Instance.TakeDamage(damage);

                if (CombatVFXManager.Instance != null && kaelTarget != null)
                    CombatVFXManager.Instance.SpawnHitSparks(kaelTarget.position + Vector3.up * 0.6f, false, 6);
            }

            yield return new WaitForSeconds(0.085f);
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
            dieAnimTimer += Time.deltaTime * 8.5f; // Recorre los 8 cuadros de desintegración de forma continua
            if (customDieFrames != null && customDieFrames.Length > 0)
            {
                int idx = Mathf.Clamp(Mathf.FloorToInt(dieAnimTimer), 0, customDieFrames.Length - 1);
                if (customDieFrames[idx] != null) sr.sprite = customDieFrames[idx];
            }
            return;
        }

        if (isPlayingAttackAnim) return;

        // Ciclo continuo de 8 cuadros donde ambos pies alternan su paso sin cortes
        walkAnimTimer += Time.deltaTime * 10.5f;
        if (customWalkFrames != null && customWalkFrames.Length > 0)
        {
            int frame = Mathf.FloorToInt(walkAnimTimer) % customWalkFrames.Length;
            if (customWalkFrames[frame] != null) sr.sprite = customWalkFrames[frame];
        }

        transform.localRotation = Quaternion.identity;
        float distToKael = (kaelTarget != null) ? Mathf.Abs(transform.position.x - kaelTarget.position.x) : 99f;
        if (distToKael > attackRange && !isChargingHeavy && visualType == "Archon")
        {
            float hoverY = Mathf.Sin(walkAnimTimer * 0.6f) * 0.12f + 0.10f;
            transform.position = new Vector3(transform.position.x, baseGroundY + hoverY, transform.position.z);
        }
        else
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
            string warnTxt = isBoss ? "⚠ ¡DESCARGA PSIÓNICA!" : "⚠ ¡PUÑO DEMOLEDOR!";
            CombatVFXManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.5f, warnTxt, new Color(1f, 0.4f, 0.1f), 18, 1.0f);
        }

        float elapsed = 0f;
        while (elapsed < 0.9f)
        {
            if (isDead) yield break;
            if (sr != null)
                sr.color = (Mathf.FloorToInt(elapsed * 12f) % 2 == 0) ? new Color(1f, 0.5f, 0.1f) : originalColor;
            if (customAttackFrames != null && customAttackFrames.Length > 0)
                sr.sprite = customAttackFrames[0];
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (sr != null) sr.color = originalColor;

        if (!isDead)
        {
            float baseX = transform.position.x;
            if (customAttackFrames != null && customAttackFrames.Length > 2)
                sr.sprite = customAttackFrames[2];
            transform.position = new Vector3(baseX - 0.45f, baseGroundY, transform.position.z);

            if (LevelAudioManager.Instance != null)
                LevelAudioManager.Instance.PlayEnemyAttack();

            if (PlayerStats.Instance != null)
                PlayerStats.Instance.TakeDamage(damage * 2.0f);

            if (CombatVFXManager.Instance != null)
                CombatVFXManager.Instance.ShakeCamera(0.22f, 0.14f);

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

        if (knockback > 0f && !isPlayingAttackAnim)
        {
            float resist = (isBoss || isMiniBoss) ? 0.25f : 0.7f;
            transform.Translate(Vector2.right * knockback * resist, Space.World);
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
            Destroy(gameObject, 0.95f); // Tiempo completo para ver los 4 cuadros de desintegración
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
