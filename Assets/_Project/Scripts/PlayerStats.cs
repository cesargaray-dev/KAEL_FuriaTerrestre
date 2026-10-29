using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Ajuste Visual de Barra sobre Kael")]
    [Tooltip("Altura de la barra de vida sobre los pies de Kael (1.95 queda arriba del cabello)")]
    [Range(1.2f, 3.0f)] public float healthBarOffsetY = 1.95f;

    [Header("Configuración del Nivel Actual")]
    public int levelNumber = 1;
    public string zoneName = "SECTOR 7 — CIUDAD EN RUINAS";
    public string nextLevelSceneName = "Lab_Zahir";

    [Header("Estados Oficiales del Nivel")]
    public bool isPaused = false;
    public bool isGameOver = false;
    public bool isChoosingDilemma = false;
    public bool isChoosingSkill = false;
    public bool isLevelCompleted = false;

    [Header("Economía, Nivel y Moral (Compasión vs Rigor)")]
    public int gold = 0;
    public int playerLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 25;
    public int skillPoints = 0;
    public int compassionPoints = 0;
    public int rigorPoints = 0;

    [Header("3 Ramas del Guantelete Nexo (Puntos de Nivel)")]
    public int branchFuria = 0;    // Aumenta daño crítico y ataque
    public int branchGuardian = 0; // Regeneración de vida por segundo
    public int branchNexo = 0;     // Carga más rápida de la Ultimate

    private string choiceSummary = "";
    private float levelTimer = 0f;

    [Header("Carga de Habilidad Ultimate (0 a 100)")]
    public float currentUltCharge = 0f;
    public float maxUltCharge = 100f;
    public bool IsUltReady => currentUltCharge >= maxUltCharge;

    [Header("5 Niveles de Mejora con Oro")]
    public int levelAtk = 1;  // Fuerza Nexo
    public int levelHp = 1;   // Vigor
    public int levelSpd = 1;  // Reflejos
    public int levelCrit = 1; // Instinto
    public int levelGold = 1; // Carroñero

    [Header("Estadísticas Calculadas")]
    public float maxHP = 100f;
    public float currentHP = 100f;
    public float hpRegenPerSec = 1.0f;
    public float attackDamage = 12f;
    public float attackRate = 1.2f;
    public float critChance = 0.05f;
    public float goldMultiplier = 1.0f;

    public event Action OnStatsChanged;
    private Transform kaelTransform;

    // Paleta Cyberpunk Nexo
    private readonly Color colBgDark   = new Color(0.05f, 0.07f, 0.13f, 0.90f);
    private readonly Color colBorder   = new Color(0.22f, 0.88f, 1.00f, 0.85f);
    private readonly Color colHpGreen  = new Color(0.18f, 0.92f, 0.48f, 1.00f);
    private readonly Color colExpPurp  = new Color(0.68f, 0.38f, 1.00f, 1.00f);
    private readonly Color colGoldYel  = new Color(1.00f, 0.82f, 0.22f, 1.00f);
    private readonly Color colCyanNexo = new Color(0.22f, 0.88f, 1.00f, 1.00f);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;

        if (SceneManager.GetActiveScene().name == "Lab_Zahir")
        {
            levelNumber = 2;
            zoneName = "BASE VORST — NÚCLEO CENTRAL";
            nextLevelSceneName = "";
            ResetSkillsToBase();
        }

        RecalculateStats();
        currentHP = maxHP;
    }

    public void ResetSkillsToBase()
    {
        gold = 0;
        playerLevel = 1;
        currentExp = 0;
        expToNextLevel = 25;
        skillPoints = 0;
        compassionPoints = 0;
        rigorPoints = 0;
        branchFuria = 0;
        branchGuardian = 0;
        branchNexo = 0;
        levelAtk = 1;
        levelHp = 1;
        levelSpd = 1;
        levelCrit = 1;
        levelGold = 1;
        currentUltCharge = 0f;
    }

    public void SaveProgressForNextLevel()
    {
        PlayerPrefs.SetInt("KAEL_Saved", 1);
        PlayerPrefs.SetInt("KAEL_UnlockedLevel", Mathf.Max(PlayerPrefs.GetInt("KAEL_UnlockedLevel", 1), levelNumber + 1));
        PlayerPrefs.SetInt("KAEL_Gold", gold);
        PlayerPrefs.SetInt("KAEL_Lv", playerLevel);
        PlayerPrefs.SetInt("KAEL_Exp", currentExp);
        PlayerPrefs.SetInt("KAEL_ExpNext", expToNextLevel);
        PlayerPrefs.SetInt("KAEL_SkillPts", skillPoints);
        PlayerPrefs.SetInt("KAEL_Compassion", compassionPoints);
        PlayerPrefs.SetInt("KAEL_Rigor", rigorPoints);
        PlayerPrefs.SetInt("KAEL_Furia", branchFuria);
        PlayerPrefs.SetInt("KAEL_Guardian", branchGuardian);
        PlayerPrefs.SetInt("KAEL_Nexo", branchNexo);
        PlayerPrefs.SetInt("KAEL_LvAtk", levelAtk);
        PlayerPrefs.SetInt("KAEL_LvHp", levelHp);
        PlayerPrefs.SetInt("KAEL_LvSpd", levelSpd);
        PlayerPrefs.SetInt("KAEL_LvCrit", levelCrit);
        PlayerPrefs.SetInt("KAEL_LvGold", levelGold);
        PlayerPrefs.Save();
    }

    public void LoadProgressIfContinuing()
    {
        if (levelNumber > 1)
        {
            if (PlayerPrefs.GetInt("KAEL_Saved", 0) == 1)
            {
                gold = PlayerPrefs.GetInt("KAEL_Gold", gold);
                playerLevel = PlayerPrefs.GetInt("KAEL_Lv", playerLevel);
                currentExp = PlayerPrefs.GetInt("KAEL_Exp", currentExp);
                expToNextLevel = PlayerPrefs.GetInt("KAEL_ExpNext", expToNextLevel);
                skillPoints = PlayerPrefs.GetInt("KAEL_SkillPts", skillPoints);
                compassionPoints = PlayerPrefs.GetInt("KAEL_Compassion", compassionPoints);
                rigorPoints = PlayerPrefs.GetInt("KAEL_Rigor", rigorPoints);
                branchFuria = PlayerPrefs.GetInt("KAEL_Furia", branchFuria);
                branchGuardian = PlayerPrefs.GetInt("KAEL_Guardian", branchGuardian);
                branchNexo = PlayerPrefs.GetInt("KAEL_Nexo", branchNexo);
                levelAtk = PlayerPrefs.GetInt("KAEL_LvAtk", levelAtk);
                levelHp = PlayerPrefs.GetInt("KAEL_LvHp", levelHp);
                levelSpd = PlayerPrefs.GetInt("KAEL_LvSpd", levelSpd);
                levelCrit = PlayerPrefs.GetInt("KAEL_LvCrit", levelCrit);
                levelGold = PlayerPrefs.GetInt("KAEL_LvGold", levelGold);
            }
            else if (playerLevel == 1 && levelAtk == 1)
            {
                // Base de prueba equilibrada si se abre Lab_Zahir directamente en el Editor
                playerLevel = 3;
                levelAtk = 3;
                levelHp = 3;
                gold = 120;
                skillPoints = 1;
            }
        }
    }

    void Start()
    {
        KaelAutoController kael = FindFirstObjectByType<KaelAutoController>();
        if (kael != null) kaelTransform = kael.transform;
    }

    void Update()
    {
        if (!isPaused && !isGameOver && !isLevelCompleted && !isChoosingDilemma && !isChoosingSkill)
        {
            levelTimer += Time.deltaTime;

            // Regeneración pasiva de vida en combate (aumenta con la Rama Guardián)
            if (currentHP > 0f && currentHP < maxHP)
            {
                currentHP = Mathf.Min(maxHP, currentHP + hpRegenPerSec * Time.deltaTime);
            }
        }
    }

    public void RecalculateStats()
    {
        float hpBonus = (compassionPoints > 0) ? 1.20f : 1.0f;
        float atkBonus = ((rigorPoints > 0) ? 1.25f : 1.0f) * (1f + branchFuria * 0.18f);

        maxHP = Mathf.RoundToInt((100f + ((levelHp - 1) * 35f) + ((playerLevel - 1) * 20f) + (branchGuardian * 40f)) * hpBonus);
        hpRegenPerSec = 1.0f + (branchGuardian * 3.5f);
        attackDamage = Mathf.RoundToInt((12f + ((levelAtk - 1) * 6f) * Mathf.Pow(1.08f, levelAtk - 1) + ((playerLevel - 1) * 4f)) * atkBonus);
        attackRate = 1.2f + ((levelSpd - 1) * 0.14f);
        critChance = Mathf.Clamp(0.05f + ((levelCrit - 1) * 0.04f) + (branchFuria * 0.05f), 0.05f, 0.80f);
        goldMultiplier = 1.0f + ((levelGold - 1) * 0.20f);

        OnStatsChanged?.Invoke();
    }

    public int GetUpgradeCost(int currentLv)
    {
        return Mathf.RoundToInt(20f * Mathf.Pow(1.18f, currentLv - 1));
    }

    public void AddRewards(int baseGold, int expAmount)
    {
        int finalGold = Mathf.RoundToInt(baseGold * goldMultiplier);
        gold += finalGold;
        currentExp += expAmount;

        AddUltCharge(12f);

        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            playerLevel++;
            skillPoints++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.35f);
            RecalculateStats();
            currentHP = maxHP;

            if (CombatVFXManager.Instance != null && kaelTransform != null)
            {
                CombatVFXManager.Instance.SpawnFloatingText(
                    kaelTransform.position + Vector3.up * (healthBarOffsetY + 0.4f),
                    $"★ ¡NIVEL {playerLevel}! (+1 PTO HABILIDAD) ★",
                    colCyanNexo, 22, 1.4f
                );
            }
        }

        OnStatsChanged?.Invoke();
    }

    public void AddUltCharge(float amount)
    {
        // La Rama Nexo acelera la carga de la Ultimate un 25% por punto
        float nexoBoost = 1f + (branchNexo * 0.25f);
        currentUltCharge = Mathf.Clamp(currentUltCharge + (amount * nexoBoost), 0f, maxUltCharge);
        OnStatsChanged?.Invoke();
    }

    public bool ConsumeUltCharge()
    {
        if (!IsUltReady) return false;
        currentUltCharge = 0f;
        OnStatsChanged?.Invoke();
        return true;
    }

    public void TakeDamage(float dmg)
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma || isChoosingSkill) return;

        currentHP = Mathf.Max(0f, currentHP - dmg);
        OnStatsChanged?.Invoke();

        if (CombatVFXManager.Instance != null && kaelTransform != null)
        {
            CombatVFXManager.Instance.SpawnFloatingText(
                kaelTransform.position + Vector3.up * (healthBarOffsetY + 0.2f),
                $"-{Mathf.RoundToInt(dmg)}",
                new Color(1f, 0.25f, 0.3f), 15, 0.65f
            );
        }

        if (currentHP <= 0f)
        {
            isGameOver = true;
            Time.timeScale = 0f;
        }
    }

    public void RetryFromGameOver()
    {
        isGameOver = false;
        RecalculateStats();
        currentHP = maxHP;
        Time.timeScale = 1f;

        WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.RestartWavesKeepingProgress();
        }
    }

    public void TriggerEndOfZoneDilemma()
    {
        isChoosingDilemma = true;
        Time.timeScale = 0f;
    }

    public void TogglePause()
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma || isChoosingSkill) return;
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    void ToggleSkillTreeModal(bool open)
    {
        isChoosingSkill = open;
        Time.timeScale = open ? 0f : 1f;
    }

    void OnGUI()
    {
        DrawKaelWorldHealthBar();

        Matrix4x4 oldMatrix = GUI.matrix;
        float scaleX = Screen.width / 1920f;
        float scaleY = Screen.height / 1080f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleX, scaleY, 1f));

        // --- A) BARRA SUPERIOR COMPACTA: PERFIL DE KAEL ---
        DrawCyberBox(new Rect(20, 14, 480, 96), colBgDark, colBorder, 2f);
        DrawStyledText(new Rect(34, 18, 455, 26), $"KAEL  <color=#38e1ff>Nv.{playerLevel}</color>   <size=15><color=#dddddd>| ATK: {attackDamage} | CRIT: {Mathf.RoundToInt(critChance * 100)}% | REG: +{hpRegenPerSec:F1}/s</color></size>", 19, TextAnchor.MiddleLeft, Color.white);

        float hpRatio = Mathf.Clamp01(currentHP / maxHP);
        DrawProgressBar(new Rect(34, 48, 450, 20), hpRatio, new Color(0.15f, 0.05f, 0.08f), colHpGreen, $"HP: {Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}", 14);

        float expRatio = Mathf.Clamp01((float)currentExp / expToNextLevel);
        DrawProgressBar(new Rect(34, 74, 450, 16), expRatio, new Color(0.1f, 0.05f, 0.2f), colExpPurp, $"EXP: {currentExp} / {expToNextLevel}", 12);

        // Botón parpadeante cuando tienes Puntos de Habilidad disponibles al subir de nivel
        if (skillPoints > 0 && !isPaused && !isGameOver && !isLevelCompleted && !isChoosingDilemma)
        {
            float pulse = 0.7f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.3f;
            Rect skillBtn = new Rect(512, 14, 220, 62);
            DrawCyberBox(skillBtn, new Color(0.18f, 0.08f, 0.32f, 0.95f), new Color(0.75f, 0.45f, 1f, pulse), 3f);
            DrawStyledText(skillBtn, $"✦ RAMAS NEXO (+{skillPoints})\n<size=13><color=#ffd35c>TOCA PARA EVOLUCIONAR</color></size>", 16, TextAnchor.MiddleCenter, Color.white);

            if (GUI.Button(skillBtn, GUIContent.none, GUIStyle.none))
            {
                ToggleSkillTreeModal(true);
            }
        }

        // --- B) PANEL CENTRAL SUPERIOR: OLEADAS ---
        WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
        if (spawner != null && spawner.currentWave < spawner.totalWaves && spawner.currentWave != 5)
        {
            DrawCyberBox(new Rect(750, 14, 420, 62), colBgDark, colBorder, 2f);
            string waveHeader = (levelNumber == 2) ? $"BASE VORST — OLEADA {spawner.currentWave} / {spawner.totalWaves}" : $"SECTOR 7 — OLEADA {spawner.currentWave} / {spawner.totalWaves}";
            DrawStyledText(new Rect(750, 18, 420, 26), waveHeader, 18, TextAnchor.MiddleCenter, colCyanNexo);
            float waveRatio = Mathf.Clamp01((float)spawner.currentWave / spawner.totalWaves);
            DrawProgressBar(new Rect(775, 48, 370, 14), waveRatio, new Color(0.1f, 0.15f, 0.25f), colCyanNexo, "", 11);
        }

        // --- C) PANEL SUPERIOR DERECHO: ORO Y PAUSA ---
        DrawCyberBox(new Rect(1470, 14, 280, 62), colBgDark, colGoldYel, 2f);
        DrawStyledText(new Rect(1480, 14, 260, 62), $"● {gold} ORO", 25, TextAnchor.MiddleCenter, colGoldYel);

        if (DrawMobileButton(new Rect(1765, 14, 135, 62), isPaused ? "▶" : "| |", isPaused ? "REANUDAR" : "PAUSA", isPaused ? colHpGreen : colBorder, true))
        {
            TogglePause();
        }

        // --- D) DOCK INFERIOR ULTRA-COMPACTO ---
        DrawCyberBox(new Rect(0, 976, 1920, 104), new Color(0.03f, 0.04f, 0.08f, 0.82f), new Color(0.15f, 0.35f, 0.55f, 0.5f), 1.5f);

        float cardW = 265f;
        float cardH = 86f;
        float cardY = 985f;
        float startX = 20f;
        float gap = 14f;

        DrawCompactUpgradeCard(new Rect(startX + (cardW + gap) * 0, cardY, cardW, cardH), "⚔ FUERZA", $"ATK: {attackDamage}", ref levelAtk, false);
        DrawCompactUpgradeCard(new Rect(startX + (cardW + gap) * 1, cardY, cardW, cardH), "♥ VIGOR", $"HP: {maxHP}", ref levelHp, true);
        DrawCompactUpgradeCard(new Rect(startX + (cardW + gap) * 2, cardY, cardW, cardH), "⚡ REFLEJOS", $"VEL: {attackRate:F1}/s", ref levelSpd, false);
        DrawCompactUpgradeCard(new Rect(startX + (cardW + gap) * 3, cardY, cardW, cardH), "✦ INSTINTO", $"CRIT: {Mathf.RoundToInt(critChance * 100)}%", ref levelCrit, false);
        DrawCompactUpgradeCard(new Rect(startX + (cardW + gap) * 4, cardY, cardW, cardH), "⛃ CARROÑERO", $"ORO: x{goldMultiplier:F2}", ref levelGold, false);

        DrawCompactUltimateButton(new Rect(1425, 982, 475, 92));

        // --- E) VENTANAS EMERGENTES ---
        if (isChoosingSkill) DrawSkillTreeModal();
        if (isPaused) DrawPauseModal();
        if (isGameOver) DrawGameOverModal(spawner);
        if (isChoosingDilemma) DrawNarrativeDilemmaModal();
        if (isLevelCompleted) DrawVictoryModal(spawner);

        GUI.matrix = oldMatrix;
    }

    void DrawKaelWorldHealthBar()
    {
        if (kaelTransform == null)
        {
            KaelAutoController kael = FindFirstObjectByType<KaelAutoController>();
            if (kael != null) kaelTransform = kael.transform;
        }
        if (kaelTransform == null || Camera.main == null) return;

        Vector3 headWorldPos = kaelTransform.position + new Vector3(0f, healthBarOffsetY, 0f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(headWorldPos);
        if (screenPos.z <= 0) return;

        float barW = 70f;
        float barH = 7f;
        float x = screenPos.x - (barW / 2f);
        float y = Screen.height - screenPos.y;

        GUI.color = new Color(0f, 0f, 0f, 0.9f);
        GUI.DrawTexture(new Rect(x - 2, y - 2, barW + 4, barH + 8), Texture2D.whiteTexture);

        GUI.color = new Color(0.1f, 0.2f, 0.1f, 1f);
        GUI.DrawTexture(new Rect(x, y, barW, barH), Texture2D.whiteTexture);

        float hpFill = Mathf.Clamp01(currentHP / maxHP);
        GUI.color = (hpFill > 0.35f) ? colHpGreen : new Color(1f, 0.25f, 0.25f);
        GUI.DrawTexture(new Rect(x, y, barW * hpFill, barH), Texture2D.whiteTexture);

        float ultFill = Mathf.Clamp01(currentUltCharge / maxUltCharge);
        GUI.color = new Color(0.05f, 0.15f, 0.25f, 1f);
        GUI.DrawTexture(new Rect(x, y + barH + 1, barW, 3f), Texture2D.whiteTexture);
        GUI.color = IsUltReady ? colGoldYel : colCyanNexo;
        GUI.DrawTexture(new Rect(x, y + barH + 1, barW * ultFill, 3f), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    void DrawSkillTreeModal()
    {
        GUI.color = new Color(0f, 0f, 0.05f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = new Rect(360, 210, 1200, 560);
        DrawCyberBox(box, colBgDark, colExpPurp, 3.5f);

        DrawStyledText(new Rect(box.x, box.y + 28, box.width, 45), $"✦ EVOLUCIÓN DEL GUANTELETE NEXO ({skillPoints} PUNTOS DISPONIBLES) ✦", 28, TextAnchor.MiddleCenter, colExpPurp);
        DrawStyledText(new Rect(box.x, box.y + 78, box.width, 35), "Elige una de las 3 ramas oficiales para potenciar permanentemente a Kael:", 18, TextAnchor.MiddleCenter, Color.white);

        // Rama 1: Furia
        Rect rFuria = new Rect(box.x + 50, box.y + 140, 340, 310);
        DrawCyberBox(rFuria, new Color(0.24f, 0.08f, 0.12f), new Color(1f, 0.35f, 0.4f), 2.5f);
        DrawStyledText(new Rect(rFuria.x, rFuria.y + 20, rFuria.width, 35), $"🔥 RAMA FURIA (Nv.{branchFuria})", 22, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.4f));
        DrawStyledText(new Rect(rFuria.x + 20, rFuria.y + 75, rFuria.width - 40, 120), "Sobrecarga los servomotores de impacto para destrozar blindajes.\n\n+18% Daño Total\n+5% Probabilidad Crítica", 16, TextAnchor.MiddleCenter, Color.white);
        DrawStyledText(new Rect(rFuria.x + 20, rFuria.y + 235, rFuria.width - 40, 45), "TOCAR PARA MEJORAR", 17, TextAnchor.MiddleCenter, colGoldYel);
        if (GUI.Button(rFuria, GUIContent.none, GUIStyle.none))
        {
            branchFuria++;
            skillPoints--;
            RecalculateStats();
            if (skillPoints <= 0) ToggleSkillTreeModal(false);
        }

        // Rama 2: Guardián
        Rect rGuard = new Rect(box.x + 430, box.y + 140, 340, 310);
        DrawCyberBox(rGuard, new Color(0.06f, 0.22f, 0.16f), colHpGreen, 2.5f);
        DrawStyledText(new Rect(rGuard.x, rGuard.y + 20, rGuard.width, 35), $"🛡 RAMA GUARDIÁN (Nv.{branchGuardian})", 22, TextAnchor.MiddleCenter, colHpGreen);
        DrawStyledText(new Rect(rGuard.x + 20, rGuard.y + 75, rGuard.width - 40, 120), "Genera un campo bio-protector que repara tejidos en combate.\n\n+40 Vida Máxima\n+3.5 Regeneración HP/seg", 16, TextAnchor.MiddleCenter, Color.white);
        DrawStyledText(new Rect(rGuard.x + 20, rGuard.y + 235, rGuard.width - 40, 45), "TOCAR PARA MEJORAR", 17, TextAnchor.MiddleCenter, colGoldYel);
        if (GUI.Button(rGuard, GUIContent.none, GUIStyle.none))
        {
            branchGuardian++;
            skillPoints--;
            RecalculateStats();
            currentHP = maxHP;
            if (skillPoints <= 0) ToggleSkillTreeModal(false);
        }

        // Rama 3: Nexo
        Rect rNexo = new Rect(box.x + 810, box.y + 140, 340, 310);
        DrawCyberBox(rNexo, new Color(0.06f, 0.18f, 0.30f), colCyanNexo, 2.5f);
        DrawStyledText(new Rect(rNexo.x, rNexo.y + 20, rNexo.width, 35), $"⚡ RAMA NEXO (Nv.{branchNexo})", 22, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(new Rect(rNexo.x + 20, rNexo.y + 75, rNexo.width - 40, 120), "Sincroniza la disonancia del Guantelete para disparar el cañón más seguido.\n\n+25% Velocidad de Carga de Ultimate", 16, TextAnchor.MiddleCenter, Color.white);
        DrawStyledText(new Rect(rNexo.x + 20, rNexo.y + 235, rNexo.width - 40, 45), "TOCAR PARA MEJORAR", 17, TextAnchor.MiddleCenter, colGoldYel);
        if (GUI.Button(rNexo, GUIContent.none, GUIStyle.none))
        {
            branchNexo++;
            skillPoints--;
            RecalculateStats();
            if (skillPoints <= 0) ToggleSkillTreeModal(false);
        }

        Rect btnClose = new Rect(box.x + 450, box.y + 475, 300, 55);
        DrawCyberBox(btnClose, new Color(0.15f, 0.18f, 0.25f), Color.gray, 2f);
        DrawStyledText(btnClose, "CERRAR", 18, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(btnClose, GUIContent.none, GUIStyle.none)) ToggleSkillTreeModal(false);
    }

    void DrawCompactUpgradeCard(Rect r, string title, string statInfo, ref int level, bool healOnBuy)
    {
        int cost = GetUpgradeCost(level);
        bool canAfford = (gold >= cost) && !isPaused && !isGameOver && !isLevelCompleted && !isChoosingDilemma && !isChoosingSkill;

        Color borderCol = canAfford ? colGoldYel : new Color(0.25f, 0.3f, 0.4f, 0.65f);
        Color bgCol = canAfford ? new Color(0.09f, 0.13f, 0.22f, 0.95f) : new Color(0.05f, 0.06f, 0.10f, 0.85f);

        DrawCyberBox(r, bgCol, borderCol, canAfford ? 2.5f : 1.5f);

        // Cajas separadas a la izquierda y derecha para evitar que el Nv. y la estadística se encimen
        float leftW = r.width * 0.58f;
        float rightW = r.width - leftW - 16f;

        DrawStyledText(new Rect(r.x + 8, r.y + 6, leftW, 24), $"{title} <color=#38e1ff>Nv.{level}</color>", 16, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(new Rect(r.x + leftW + 4, r.y + 6, rightW, 24), statInfo, 13, TextAnchor.MiddleRight, new Color(0.82f, 0.92f, 1f));

        Rect buyBtn = new Rect(r.x + 8, r.y + 38, r.width - 16, 40);
        GUI.color = canAfford ? new Color(0.95f, 0.74f, 0.14f, 1f) : new Color(0.16f, 0.18f, 0.22f, 0.9f);
        GUI.DrawTexture(buyBtn, Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawStyledText(buyBtn, $"MEJORAR: {cost} ORO", 15, TextAnchor.MiddleCenter, canAfford ? Color.black : new Color(0.55f, 0.55f, 0.6f));

        if (canAfford && GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            gold -= cost;
            level++;
            RecalculateStats();
            if (healOnBuy) currentHP = Mathf.Min(currentHP + 40f, maxHP);

            if (CombatVFXManager.Instance != null && kaelTransform != null)
            {
                CombatVFXManager.Instance.SpawnFloatingText(
                    kaelTransform.position + Vector3.up * (healthBarOffsetY + 0.35f),
                    $"▲ {title} Nv.{level}!",
                    colGoldYel, 18, 0.9f
                );
            }
        }
    }

    void DrawCompactUltimateButton(Rect r)
    {
        bool ready = IsUltReady && !isPaused && !isGameOver && !isLevelCompleted && !isChoosingDilemma && !isChoosingSkill;
        float pulse = ready ? (0.75f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.25f) : 1f;

        Color borderCol = ready ? new Color(0.22f, 0.92f, 1f, pulse) : new Color(0.2f, 0.35f, 0.45f, 0.8f);
        Color bgCol = ready ? new Color(0.06f, 0.28f, 0.42f, 0.96f) : new Color(0.05f, 0.08f, 0.14f, 0.9f);

        DrawCyberBox(r, bgCol, borderCol, ready ? 3.5f : 1.5f);

        float chargeRatio = Mathf.Clamp01(currentUltCharge / maxUltCharge);
        GUI.color = ready ? new Color(0.15f, 0.75f, 0.95f, 0.35f * pulse) : new Color(0.12f, 0.45f, 0.65f, 0.25f);
        GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, (r.width - 6) * chargeRatio, r.height - 6), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string mainTitle = ready ? "★ ¡LANZAR SOBRECARGA NEXO! ★" : "SOBRECARGA NEXO (ULTIMATE)";
        string subTitle = ready ? "TOCA PARA BARRER LA HORDA (475% ATK)" : $"CARGANDO ENERGÍA... {Mathf.FloorToInt(currentUltCharge)}%";

        DrawStyledText(new Rect(r.x, r.y + 12, r.width, 36), mainTitle, ready ? 21 : 18, TextAnchor.MiddleCenter, ready ? colCyanNexo : Color.white);
        DrawStyledText(new Rect(r.x, r.y + 48, r.width, 30), subTitle, 15, TextAnchor.MiddleCenter, ready ? colGoldYel : new Color(0.65f, 0.75f, 0.85f));

        if (ready && GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            KaelAutoController kael = FindFirstObjectByType<KaelAutoController>();
            if (kael != null) kael.TriggerUltimate();
        }
    }

    bool DrawMobileButton(Rect r, string bigIcon, string subLabel, Color borderCol, bool enabled)
    {
        DrawCyberBox(r, colBgDark, borderCol, 2f);
        DrawStyledText(new Rect(r.x, r.y + 4, r.width, 30), bigIcon, 20, TextAnchor.MiddleCenter, borderCol);
        DrawStyledText(new Rect(r.x, r.y + 32, r.width, 22), subLabel, 13, TextAnchor.MiddleCenter, Color.white);
        return enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    void DrawPauseModal()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = new Rect(660, 300, 600, 410);
        DrawCyberBox(box, colBgDark, colCyanNexo, 3f);

        DrawStyledText(new Rect(box.x, box.y + 30, box.width, 50), "PAUSA TÁCTICA", 32, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(new Rect(box.x, box.y + 82, box.width, 35), zoneName, 18, TextAnchor.MiddleCenter, Color.gray);

        Rect btnResume = new Rect(box.x + 120, box.y + 140, 360, 60);
        DrawCyberBox(btnResume, new Color(0.1f, 0.35f, 0.25f), colHpGreen, 2.5f);
        DrawStyledText(btnResume, "▶ CONTINUAR COMBATE", 20, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(btnResume, GUIContent.none, GUIStyle.none)) TogglePause();

        Rect btnRestart = new Rect(box.x + 120, box.y + 218, 360, 55);
        DrawCyberBox(btnRestart, new Color(0.25f, 0.1f, 0.15f), new Color(1f, 0.35f, 0.4f), 2f);
        DrawStyledText(btnRestart, "↻ REINICIAR NIVEL", 17, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(btnRestart, GUIContent.none, GUIStyle.none))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        Rect btnMenu = new Rect(box.x + 120, box.y + 290, 360, 55);
        DrawCyberBox(btnMenu, new Color(0.08f, 0.18f, 0.30f), colCyanNexo, 2f);
        DrawStyledText(btnMenu, "⌂ SALIR AL MENÚ PRINCIPAL", 17, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(btnMenu, GUIContent.none, GUIStyle.none))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }

    void DrawGameOverModal(WaveSpawner spawner)
    {
        GUI.color = new Color(0.08f, 0f, 0.02f, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = new Rect(560, 250, 800, 460);
        DrawCyberBox(box, colBgDark, new Color(1f, 0.25f, 0.35f), 3.5f);

        int waveReached = (spawner != null) ? spawner.currentWave : 1;
        DrawStyledText(new Rect(box.x, box.y + 35, box.width, 50), "⚠ KAEL HA CAÍDO EN COMBATE ⚠", 34, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.35f));
        DrawStyledText(new Rect(box.x + 60, box.y + 100, box.width - 120, 75),
            "\"El Guantelete Nexo te reconstruye. Devuélveles el error a los Vorst.\"\n" +
            $"Caíste en la Oleada {waveReached}. Conservas TODO tu Oro ({gold}) y tus Mejoras actuales.",
            20, TextAnchor.MiddleCenter, Color.white);

        DrawStyledText(new Rect(box.x + 60, box.y + 190, box.width - 120, 40),
            "Tip: Usa tu oro acumulado en las tarjetas de abajo al reintentar para superar el muro.",
            17, TextAnchor.MiddleCenter, colGoldYel);

        Rect btnRetry = new Rect(box.x + 170, box.y + 265, 460, 75);
        DrawCyberBox(btnRetry, new Color(0.08f, 0.35f, 0.25f), colHpGreen, 3f);
        DrawStyledText(btnRetry, "⚡ RECONSTRUIR Y REINTENTAR (CONSERVAR PROGRESO)", 19, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(btnRetry, GUIContent.none, GUIStyle.none))
        {
            RetryFromGameOver();
        }
    }

    void DrawNarrativeDilemmaModal()
    {
        GUI.color = new Color(0f, 0f, 0.05f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = new Rect(460, 200, 1000, 560);
        DrawCyberBox(box, colBgDark, colCyanNexo, 3.5f);

        bool isLv2 = (levelNumber == 2);
        string title = isLv2 ? "DECISIÓN DE LA BASE VORST: LA TORRE DE CONVERGENCIA" : "DECISIÓN DEL SECTOR 7: EL NÚCLEO DEL COMANDANTE";
        string desc = isLv2
            ? "Tras caer el Arconte Vorst, la esfera púrpura de la Base Vorst queda expuesta.\nPuedes liberar a los prisioneros humanos en estasis o sobrecargar tu Guantelete con sus orbes."
            : "Entre los restos del Comandante Vorst encuentras un reactor bio-energético aún activo,\nmientras un grupo de sobrevivientes atrapados en el puente pide auxilio médico.";

        string compDesc = isLv2
            ? "Desactivar las cámaras de recolección para liberar a los prisioneros humanos cautivos en la Base Vorst."
            : "Usar la energía del reactor para estabilizar el refugio de los sobrevivientes del Sector 7.";

        string rigorDesc = isLv2
            ? "Drenar los 4 orbes psiónicos del Arconte directamente hacia tu brazo para abrir brecha a la Nave Núcleo."
            : "Devorar el núcleo Vorst con el Guantelete para acelerar tu marcha hacia Lía sin mirar atrás.";

        DrawStyledText(new Rect(box.x, box.y + 30, box.width, 45), title, 27, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(new Rect(box.x + 60, box.y + 85, box.width - 120, 80), desc, 19, TextAnchor.MiddleCenter, Color.white);

        Rect btnCompassion = new Rect(box.x + 60, box.y + 195, 420, 280);
        DrawCyberBox(btnCompassion, new Color(0.06f, 0.22f, 0.32f), colCyanNexo, 2.5f);
        DrawStyledText(new Rect(btnCompassion.x + 20, btnCompassion.y + 20, btnCompassion.width - 40, 40), "♦ COMPASIÓN (HUMANIDAD)", 22, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(new Rect(btnCompassion.x + 25, btnCompassion.y + 75, btnCompassion.width - 50, 110), compDesc, 17, TextAnchor.MiddleCenter, Color.white);
        DrawStyledText(new Rect(btnCompassion.x + 20, btnCompassion.y + 200, btnCompassion.width - 40, 50), "+1 COMPASIÓN\n+20% VIDA MÁXIMA PERMANENTE", 17, TextAnchor.MiddleCenter, colHpGreen);

        if (GUI.Button(btnCompassion, GUIContent.none, GUIStyle.none))
        {
            compassionPoints++;
            choiceSummary = isLv2 ? "Liberaste a los prisioneros de la Base Vorst (+1 Compasión)" : "Salvaste a los sobrevivientes (+1 Compasión, +20% HP)";
            RecalculateStats();
            currentHP = maxHP;
            isChoosingDilemma = false;
            isLevelCompleted = true;
            SaveProgressForNextLevel();
        }

        Rect btnRigor = new Rect(box.x + 520, box.y + 195, 420, 280);
        DrawCyberBox(btnRigor, new Color(0.25f, 0.08f, 0.18f), new Color(1f, 0.35f, 0.55f), 2.5f);
        DrawStyledText(new Rect(btnRigor.x + 20, btnRigor.y + 20, btnRigor.width - 40, 40), "❖ RIGOR (DISONANCIA NEXO)", 22, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.6f));
        DrawStyledText(new Rect(btnRigor.x + 25, btnRigor.y + 75, btnRigor.width - 50, 110), rigorDesc, 17, TextAnchor.MiddleCenter, Color.white);
        DrawStyledText(new Rect(btnRigor.x + 20, btnRigor.y + 200, btnRigor.width - 40, 50), "+1 RIGOR\n+25% DAÑO DE ATAQUE PERMANENTE", 17, TextAnchor.MiddleCenter, colGoldYel);

        if (GUI.Button(btnRigor, GUIContent.none, GUIStyle.none))
        {
            rigorPoints++;
            choiceSummary = isLv2 ? "Drenaste los orbes psiónicos del Arconte (+1 Rigor)" : "Absorbiste el núcleo Vorst (+1 Rigor, +25% ATK)";
            RecalculateStats();
            isChoosingDilemma = false;
            isLevelCompleted = true;
            SaveProgressForNextLevel();
        }
    }

    void DrawVictoryModal(WaveSpawner spawner)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = new Rect(510, 200, 900, 560);
        DrawCyberBox(box, colBgDark, colGoldYel, 4f);

        int kills = (spawner != null) ? spawner.totalKillsInLevel : 60;
        int timeSec = Mathf.RoundToInt(levelTimer);
        int timeBonus = Mathf.Max(100, 1000 - (timeSec * 4));
        int totalScore = (gold * 10) + (kills * 25) + timeBonus;

        string vicTitle = (levelNumber == 2) ? "★ ¡BASE VORST CONQUISTADA! ★" : "★ ¡SECTOR 7 LIBERADO! ★";
        DrawStyledText(new Rect(box.x, box.y + 30, box.width, 50), vicTitle, 36, TextAnchor.MiddleCenter, colGoldYel);
        DrawStyledText(new Rect(box.x + 50, box.y + 85, box.width - 100, 35), $"Decisión tomada: <color=#38e1ff>{choiceSummary}</color>", 19, TextAnchor.MiddleCenter, Color.white);

        DrawCyberBox(new Rect(box.x + 110, box.y + 140, box.width - 220, 240), new Color(0.04f, 0.05f, 0.09f), colBorder, 1.5f);
        DrawStyledText(new Rect(box.x + 140, box.y + 155, box.width - 280, 38), $"TIEMPO EMPLEADO:   <color=#38e1ff>{timeSec / 60:00}:{timeSec % 60:00}</color>", 21, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(new Rect(box.x + 140, box.y + 200, box.width - 280, 38), $"VORST DERROTADOS:   <color=#38e1ff>{kills} enemigos</color>", 21, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(new Rect(box.x + 140, box.y + 245, box.width - 280, 38), $"NIVEL DE KAEL Y ORO:   <color=#ffd35c>Nv.{playerLevel}  ({gold} Oro)</color>", 21, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(new Rect(box.x + 140, box.y + 300, box.width - 280, 45), $"PUNTAJE TOTAL DEL NIVEL:   <color=#2ee678>{totalScore} PTS</color>", 24, TextAnchor.MiddleLeft, Color.white);

        bool hasNextLevel = !string.IsNullOrEmpty(nextLevelSceneName) && Application.CanStreamedLevelBeLoaded(nextLevelSceneName);

        if (hasNextLevel)
        {
            Rect btnNext = new Rect(box.x + 60, box.y + 420, 450, 75);
            DrawCyberBox(btnNext, new Color(0.08f, 0.38f, 0.25f), colHpGreen, 3f);
            DrawStyledText(btnNext, $"▶ AVANZAR AL NIVEL {levelNumber + 1} (BASE VORST)", 20, TextAnchor.MiddleCenter, Color.white);
            if (GUI.Button(btnNext, GUIContent.none, GUIStyle.none))
            {
                SaveProgressForNextLevel();
                Time.timeScale = 1f;
                SceneManager.LoadScene(nextLevelSceneName);
            }

            Rect btnMenu = new Rect(box.x + 530, box.y + 420, 310, 75);
            DrawCyberBox(btnMenu, new Color(0.08f, 0.25f, 0.38f), colCyanNexo, 2.5f);
            DrawStyledText(btnMenu, "⌂ MENÚ PRINCIPAL", 19, TextAnchor.MiddleCenter, Color.white);
            if (GUI.Button(btnMenu, GUIContent.none, GUIStyle.none))
            {
                SaveProgressForNextLevel();
                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
            }
        }
        else
        {
            Rect btnReplay = new Rect(box.x + 90, box.y + 420, 350, 75);
            DrawCyberBox(btnReplay, new Color(0.08f, 0.32f, 0.45f), colCyanNexo, 3f);
            DrawStyledText(btnReplay, $"↻ REPETIR NIVEL {levelNumber}", 20, TextAnchor.MiddleCenter, Color.white);
            if (GUI.Button(btnReplay, GUIContent.none, GUIStyle.none))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            Rect btnMenu = new Rect(box.x + 460, box.y + 420, 350, 75);
            DrawCyberBox(btnMenu, new Color(0.08f, 0.25f, 0.38f), colGoldYel, 2.5f);
            DrawStyledText(btnMenu, "⌂ MENÚ PRINCIPAL", 20, TextAnchor.MiddleCenter, Color.white);
            if (GUI.Button(btnMenu, GUIContent.none, GUIStyle.none))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
            }
        }
    }

    void DrawCyberBox(Rect r, Color bg, Color border, float borderThickness)
    {
        GUI.color = border;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = bg;
        GUI.DrawTexture(new Rect(r.x + borderThickness, r.y + borderThickness, r.width - borderThickness * 2f, r.height - borderThickness * 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawProgressBar(Rect r, float ratio, Color bgCol, Color fillCol, string label, int fontSize)
    {
        GUI.color = bgCol;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = fillCol;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * ratio, r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (!string.IsNullOrEmpty(label))
        {
            DrawStyledText(r, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        }
    }

    void DrawStyledText(Rect r, string txt, int size, TextAnchor align, Color col)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.richText = true;
        st.fontSize = size;
        st.fontStyle = FontStyle.Bold;
        st.alignment = align;

        // Limpiar etiquetas <color=...> del texto de sombra para que sea 100% negra y no duplique el color cian/amarillo desplazado
        string shadowTxt = System.Text.RegularExpressions.Regex.Replace(txt, "<color=[^>]+>|</color>", "");
        st.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), shadowTxt, st);

        st.normal.textColor = col;
        GUI.Label(r, txt, st);
    }
}
