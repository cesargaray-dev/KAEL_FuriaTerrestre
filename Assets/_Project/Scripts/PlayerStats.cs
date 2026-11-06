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
    private readonly Color colBgDark   = new Color(0.04f, 0.06f, 0.12f, 0.94f);
    private readonly Color colBorder   = new Color(0.22f, 0.88f, 1.00f, 0.88f);
    private readonly Color colHpGreen  = new Color(0.18f, 0.94f, 0.50f, 1.00f);
    private readonly Color colExpPurp  = new Color(0.70f, 0.40f, 1.00f, 1.00f);
    private readonly Color colGoldYel  = new Color(1.00f, 0.82f, 0.22f, 1.00f);
    private readonly Color colCyanNexo = new Color(0.22f, 0.90f, 1.00f, 1.00f);

    // Colores diferenciados por atributo para las 5 tarjetas de mejora
    private readonly Color colAttrFuerza    = new Color(1.00f, 0.36f, 0.28f, 1.00f);
    private readonly Color colAttrVigor     = new Color(0.18f, 0.94f, 0.52f, 1.00f);
    private readonly Color colAttrReflejos  = new Color(0.22f, 0.90f, 1.00f, 1.00f);
    private readonly Color colAttrInstinto  = new Color(0.86f, 0.42f, 1.00f, 1.00f);
    private readonly Color colAttrCarronero = new Color(1.00f, 0.82f, 0.22f, 1.00f);

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

        bool leveledUp = false;
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            playerLevel++;
            skillPoints++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.35f);
            RecalculateStats();
            currentHP = maxHP;
            leveledUp = true;

            if (CombatVFXManager.Instance != null && kaelTransform != null)
            {
                CombatVFXManager.Instance.SpawnFloatingText(
                    kaelTransform.position + Vector3.up * (healthBarOffsetY + 0.4f),
                    $"★ ¡NIVEL {playerLevel}! (+1 PTO HABILIDAD) ★",
                    colCyanNexo, 22, 1.4f
                );
            }
        }

        if (leveledUp && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelUp();
        }

        OnStatsChanged?.Invoke();
    }

    public void AddUltCharge(float amount)
    {
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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
            AudioManager.Instance.PlayCombatMusic();
        }

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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVictoryDilemmaMusic();
        }
    }

    public void TogglePause()
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma || isChoosingSkill) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    void ToggleSkillTreeModal(bool open)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        isChoosingSkill = open;
        Time.timeScale = open ? 0f : 1f;
    }

    // --- FUNCIONES AUXILIARES DE ESCALADO NATIVO (1920x1080 -> Píxeles Reales) ---
    public static Rect ScaleRect(float x, float y, float w, float h)
    {
        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;
        return new Rect(
            Mathf.Round(x * sx),
            Mathf.Round(y * sy),
            Mathf.Round(w * sx),
            Mathf.Round(h * sy)
        );
    }

    public static int ScaleFont(int baseSize)
    {
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        return Mathf.Max(10, Mathf.RoundToInt(baseSize * scale));
    }

    void OnGUI()
    {
        // Mantener GUI.matrix = Matrix4x4.identity para que todas las fuentes se rendericen a resolución nativa nítida
        GUI.matrix = Matrix4x4.identity;

        DrawKaelWorldHealthBar();

        // --- A) BARRA SUPERIOR COMPACTA: PERFIL DE KAEL ---
        DrawCyberBox(ScaleRect(18, 12, 505, 98), colBgDark, colBorder, 2f);
        DrawStyledText(ScaleRect(32, 16, 175, 28), $"KAEL <color=#38e1ff>Nv.{playerLevel}</color>", 20, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(205, 17, 305, 26), $"ATK: {attackDamage}  |  CRIT: {Mathf.RoundToInt(critChance * 100)}%  |  +{hpRegenPerSec:F1}/s", 14, TextAnchor.MiddleRight, new Color(0.85f, 0.93f, 1f));

        float hpRatio = Mathf.Clamp01(currentHP / Mathf.Max(1f, maxHP));
        DrawProgressBar(ScaleRect(32, 47, 476, 22), hpRatio, new Color(0.14f, 0.05f, 0.08f), colHpGreen, $"HP: {Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}", 14);

        float expRatio = Mathf.Clamp01((float)currentExp / Mathf.Max(1, expToNextLevel));
        DrawProgressBar(ScaleRect(32, 75, 476, 18), expRatio, new Color(0.10f, 0.05f, 0.20f), colExpPurp, $"EXP: {currentExp} / {expToNextLevel}", 12);

        // Botón parpadeante cuando tienes Puntos de Habilidad disponibles al subir de nivel
        if (skillPoints > 0 && !isPaused && !isGameOver && !isLevelCompleted && !isChoosingDilemma)
        {
            float pulse = 0.7f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.3f;
            Rect skillBtn = ScaleRect(535, 12, 225, 68);
            bool hoverSkill = skillBtn.Contains(Event.current.mousePosition);
            DrawCyberBox(skillBtn, hoverSkill ? new Color(0.26f, 0.12f, 0.44f, 0.98f) : new Color(0.18f, 0.08f, 0.32f, 0.95f), new Color(0.78f, 0.48f, 1f, pulse), 2.5f);
            DrawStyledText(ScaleRect(535, 16, 225, 32), $"✦ RAMAS NEXO (+{skillPoints})", 16, TextAnchor.MiddleCenter, Color.white);
            DrawStyledText(ScaleRect(535, 44, 225, 26), "TOCA PARA EVOLUCIONAR", 13, TextAnchor.MiddleCenter, colGoldYel);

            if (GUI.Button(skillBtn, GUIContent.none, GUIStyle.none))
            {
                ToggleSkillTreeModal(true);
            }
        }

        // --- B) PANEL CENTRAL SUPERIOR: OLEADAS ---
        WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
        if (spawner != null && spawner.currentWave < spawner.totalWaves && spawner.currentWave != 5)
        {
            DrawCyberBox(ScaleRect(775, 12, 410, 68), colBgDark, colBorder, 2f);
            string waveHeader = (levelNumber == 2)
                ? $"BASE VORST — OLEADA {Mathf.Max(1, spawner.currentWave)} / {spawner.totalWaves}"
                : $"SECTOR 7 — OLEADA {Mathf.Max(1, spawner.currentWave)} / {spawner.totalWaves}";
            DrawStyledText(ScaleRect(775, 16, 410, 28), waveHeader, 17, TextAnchor.MiddleCenter, colCyanNexo);
            float waveRatio = Mathf.Clamp01((float)spawner.currentWave / Mathf.Max(1, spawner.totalWaves));
            DrawProgressBar(ScaleRect(798, 49, 364, 16), waveRatio, new Color(0.08f, 0.14f, 0.24f), colCyanNexo, "", 11);
        }

        // --- C) PANEL SUPERIOR DERECHO: ORO, CONTROL COMPACTO DE AUDIO Y PAUSA ---
        DrawCyberBox(ScaleRect(1210, 12, 235, 68), colBgDark, colGoldYel, 2f);
        DrawStyledText(ScaleRect(1218, 12, 219, 68), $"● {gold} ORO", 24, TextAnchor.MiddleCenter, colGoldYel);

        DrawTopRightAudioWidget(1456f, 12f, 302f, 68f);

        if (DrawMobileButton(ScaleRect(1770, 12, 132, 68), isPaused ? "▶" : "| |", isPaused ? "REANUDAR" : "PAUSA", isPaused ? colHpGreen : colBorder, true))
        {
            TogglePause();
        }

        // --- D) DOCK INFERIOR DE MEJORAS Y ULTIMATE REDISEÑADO ---
        Rect dockRect = ScaleRect(0, 954, 1920, 126);
        DrawCyberBox(dockRect, new Color(0.02f, 0.03f, 0.07f, 0.94f), new Color(0.18f, 0.45f, 0.68f, 0.75f), 2f);
        // Línea superior luminosa del dock
        GUI.color = new Color(0.22f, 0.88f, 1f, 0.85f);
        GUI.DrawTexture(new Rect(dockRect.x, dockRect.y, dockRect.width, Mathf.Max(2f, Mathf.Round(2f * (Screen.height / 1080f)))), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float cardW = 264f;
        float cardH = 106f;
        float cardY = 964f;
        float startX = 18f;
        float gap = 12f;

        DrawCompactUpgradeCard(new Rect(startX + (cardW + gap) * 0, cardY, cardW, cardH), "⚔ FUERZA", $"ATK: {attackDamage}", "+DAÑO FÍSICO", colAttrFuerza, ref levelAtk, false);
        DrawCompactUpgradeCard(new Rect(startX + (cardW + gap) * 1, cardY, cardW, cardH), "♥ VIGOR", $"HP: {maxHP}", "+35 HP / CURA", colAttrVigor, ref levelHp, true);
        DrawCompactUpgradeCard(new Rect(startX + (cardW + gap) * 2, cardY, cardW, cardH), "⚡ REFLEJOS", $"VEL: {attackRate:F1}/s", "+CADENCIA", colAttrReflejos, ref levelSpd, false);
        DrawCompactUpgradeCard(new Rect(startX + (cardW + gap) * 3, cardY, cardW, cardH), "✦ INSTINTO", $"CRIT: {Mathf.RoundToInt(critChance * 100)}%", "+4% CRÍTICO", colAttrInstinto, ref levelCrit, false);
        DrawCompactUpgradeCard(new Rect(startX + (cardW + gap) * 4, cardY, cardW, cardH), "⛃ CARROÑERO", $"ORO: x{goldMultiplier:F2}", "+20% BOTÍN", colAttrCarronero, ref levelGold, false);

        DrawCompactUltimateButton(new Rect(1404f, 964f, 498f, 106f));

        // --- E) VENTANAS EMERGENTES ---
        if (isChoosingSkill) DrawSkillTreeModal();
        if (isPaused) DrawPauseModal();
        if (isGameOver) DrawGameOverModal(spawner);
        if (isChoosingDilemma) DrawNarrativeDilemmaModal();
        if (isLevelCompleted) DrawVictoryModal(spawner);
    }

    void DrawTopRightAudioWidget(float baseX, float baseY, float baseW, float baseH)
    {
        bool muted = (AudioManager.Instance != null) && AudioManager.Instance.isMuted;
        float vol = (AudioManager.Instance != null) ? AudioManager.Instance.masterVolume : 0.85f;
        int volPct = muted ? 0 : Mathf.RoundToInt(vol * 100f);

        Color accent = muted ? new Color(1f, 0.35f, 0.42f, 0.9f) : colCyanNexo;
        Rect panelRect = ScaleRect(baseX, baseY, baseW, baseH);
        DrawCyberBox(panelRect, colBgDark, accent, 2f);

        // Botón compacto de Mute (🔊 / 🔇)
        Rect muteBtn = ScaleRect(baseX + 8f, baseY + 8f, 64f, baseH - 16f);
        bool hoverMute = muteBtn.Contains(Event.current.mousePosition);
        DrawCyberBox(muteBtn, hoverMute ? new Color(0.14f, 0.22f, 0.34f, 0.98f) : new Color(0.07f, 0.11f, 0.18f, 0.95f), accent, 1.5f);
        DrawStyledText(muteBtn, muted ? "🔇" : "🔊", 22, TextAnchor.MiddleCenter, muted ? new Color(1f, 0.4f, 0.45f) : colCyanNexo);
        if (GUI.Button(muteBtn, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }

        // Etiqueta de porcentaje de volumen
        DrawStyledText(ScaleRect(baseX + 80f, baseY + 8f, baseW - 92f, 22f),
            muted ? "AUDIO: SILENCIADO" : $"VOLUMEN: {volPct}%",
            13, TextAnchor.MiddleLeft, muted ? new Color(1f, 0.5f, 0.55f) : Color.white);

        // Barra deslizable interactiva de volumen (- / Barra / +)
        Rect minusBtn = ScaleRect(baseX + 80f, baseY + 34f, 32f, 24f);
        DrawCyberBox(minusBtn, new Color(0.08f, 0.13f, 0.22f), colBorder, 1f);
        DrawStyledText(minusBtn, "−", 16, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(minusBtn, GUIContent.none, GUIStyle.none) && AudioManager.Instance != null)
        {
            AudioManager.Instance.isMuted = false;
            AudioManager.Instance.masterVolume = Mathf.Clamp01(vol - 0.10f);
            AudioManager.Instance.PlayUIClick();
        }

        Rect barRect = ScaleRect(baseX + 118f, baseY + 38f, 134f, 16f);
        GUI.color = new Color(0.08f, 0.12f, 0.20f, 1f);
        GUI.DrawTexture(barRect, Texture2D.whiteTexture);
        float fillRatio = muted ? 0f : Mathf.Clamp01(vol);
        GUI.color = muted ? new Color(0.5f, 0.2f, 0.25f) : colCyanNexo;
        GUI.DrawTexture(new Rect(barRect.x, barRect.y, barRect.width * fillRatio, barRect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Permitir clic o arrastre directo sobre la barra de volumen
        if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && barRect.Contains(Event.current.mousePosition))
        {
            if (AudioManager.Instance != null)
            {
                float clickRatio = Mathf.Clamp01((Event.current.mousePosition.x - barRect.x) / Mathf.Max(1f, barRect.width));
                AudioManager.Instance.isMuted = false;
                AudioManager.Instance.masterVolume = clickRatio;
                Event.current.Use();
            }
        }

        Rect plusBtn = ScaleRect(baseX + 258f, baseY + 34f, 32f, 24f);
        DrawCyberBox(plusBtn, new Color(0.08f, 0.13f, 0.22f), colBorder, 1f);
        DrawStyledText(plusBtn, "+", 16, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(plusBtn, GUIContent.none, GUIStyle.none) && AudioManager.Instance != null)
        {
            AudioManager.Instance.isMuted = false;
            AudioManager.Instance.masterVolume = Mathf.Clamp01(vol + 0.10f);
            AudioManager.Instance.PlayUIClick();
        }
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

        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;
        float barW = Mathf.Round(84f * sx);
        float barH = Mathf.Round(8f * sy);
        float ultH = Mathf.Max(3f, Mathf.Round(4f * sy));
        float x = Mathf.Round(screenPos.x - (barW * 0.5f));
        float y = Mathf.Round(Screen.height - screenPos.y);

        GUI.color = new Color(0f, 0f, 0f, 0.92f);
        GUI.DrawTexture(new Rect(x - 2f, y - 2f, barW + 4f, barH + ultH + 5f), Texture2D.whiteTexture);

        GUI.color = new Color(0.12f, 0.20f, 0.12f, 1f);
        GUI.DrawTexture(new Rect(x, y, barW, barH), Texture2D.whiteTexture);

        float hpFill = Mathf.Clamp01(currentHP / Mathf.Max(1f, maxHP));
        GUI.color = (hpFill > 0.35f) ? colHpGreen : new Color(1f, 0.25f, 0.25f);
        GUI.DrawTexture(new Rect(x, y, barW * hpFill, barH), Texture2D.whiteTexture);

        float ultFill = Mathf.Clamp01(currentUltCharge / Mathf.Max(1f, maxUltCharge));
        GUI.color = new Color(0.05f, 0.15f, 0.25f, 1f);
        GUI.DrawTexture(new Rect(x, y + barH + 1f, barW, ultH), Texture2D.whiteTexture);
        GUI.color = IsUltReady ? colGoldYel : colCyanNexo;
        GUI.DrawTexture(new Rect(x, y + barH + 1f, barW * ultFill, ultH), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    void DrawCompactUpgradeCard(Rect baseRect, string title, string statInfo, string bonusHint, Color attrColor, ref int level, bool healOnBuy)
    {
        int cost = GetUpgradeCost(level);
        bool canAfford = (gold >= cost) && !isPaused && !isGameOver && !isLevelCompleted && !isChoosingDilemma && !isChoosingSkill;

        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool isHovered = r.Contains(Event.current.mousePosition);

        float pulse = canAfford ? (0.78f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 0.22f) : 1f;

        Color borderCol = canAfford
            ? (isHovered ? Color.white : new Color(colGoldYel.r, colGoldYel.g, colGoldYel.b, pulse))
            : (isHovered ? new Color(attrColor.r, attrColor.g, attrColor.b, 0.85f) : new Color(0.22f, 0.28f, 0.38f, 0.75f));

        Color bgCol = canAfford
            ? (isHovered ? new Color(0.12f, 0.18f, 0.30f, 0.98f) : new Color(0.07f, 0.11f, 0.20f, 0.96f))
            : (isHovered ? new Color(0.07f, 0.09f, 0.15f, 0.94f) : new Color(0.04f, 0.05f, 0.09f, 0.92f));

        DrawCyberBox(r, bgCol, borderCol, canAfford ? 2.5f : 1.5f);

        // Cabecera de color diferenciada por atributo + Barra de acento superior brillante
        Rect headerBg = ScaleRect(baseRect.x + 2f, baseRect.y + 2f, baseRect.width - 4f, 34f);
        GUI.color = new Color(attrColor.r * 0.32f, attrColor.g * 0.32f, attrColor.b * 0.32f, 0.90f);
        GUI.DrawTexture(headerBg, Texture2D.whiteTexture);

        Rect accentBar = ScaleRect(baseRect.x + 2f, baseRect.y + 2f, baseRect.width - 4f, 5f);
        GUI.color = canAfford ? colGoldYel : attrColor;
        GUI.DrawTexture(accentBar, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Título de atributo y Nivel con alto contraste
        DrawStyledText(ScaleRect(baseRect.x + 10f, baseRect.y + 7f, baseRect.width - 88f, 26f), title, 17, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(baseRect.x + baseRect.width - 82f, baseRect.y + 7f, 72f, 26f), $"Nv.{level}", 16, TextAnchor.MiddleRight, attrColor);

        // Estadística actual y bonificación
        DrawStyledText(ScaleRect(baseRect.x + 10f, baseRect.y + 38f, baseRect.width * 0.56f, 24f), statInfo, 16, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(baseRect.x + baseRect.width * 0.54f, baseRect.y + 38f, baseRect.width * 0.42f, 24f), bonusHint, 12, TextAnchor.MiddleRight, new Color(0.78f, 0.88f, 1f));

        // Botón inferior de compra con indicador visual claro cuando alcanza el oro
        Rect buyBtn = ScaleRect(baseRect.x + 8f, baseRect.y + 66f, baseRect.width - 16f, 32f);
        Color buyBtnBg = canAfford
            ? (isHovered ? new Color(1.00f, 0.86f, 0.28f, 1f) : new Color(0.95f, 0.75f, 0.14f, 1f))
            : new Color(0.12f, 0.14f, 0.20f, 0.92f);

        GUI.color = buyBtnBg;
        GUI.DrawTexture(buyBtn, Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (canAfford)
        {
            // Sutil borde interior brillante y texto claro con sombra oscura para máxima legibilidad
            DrawCyberBox(buyBtn, buyBtnBg, Color.white, 1f);
            DrawStyledText(buyBtn, $"▲ COMPRAR • {cost} ORO", 15, TextAnchor.MiddleCenter, new Color(0.12f, 0.08f, 0.02f));
        }
        else
        {
            DrawStyledText(buyBtn, $"COSTO: {cost} ORO", 14, TextAnchor.MiddleCenter, new Color(0.62f, 0.66f, 0.74f));
        }

        if (canAfford && GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

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

    void DrawCompactUltimateButton(Rect baseRect)
    {
        bool ready = IsUltReady && !isPaused && !isGameOver && !isLevelCompleted && !isChoosingDilemma && !isChoosingSkill;
        float pulse = ready ? (0.75f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.25f) : 1f;

        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool isHovered = r.Contains(Event.current.mousePosition);

        Color borderCol = ready
            ? (isHovered ? Color.white : new Color(0.22f, 0.95f, 1f, pulse))
            : new Color(0.22f, 0.42f, 0.58f, 0.85f);

        Color bgCol = ready
            ? (isHovered ? new Color(0.08f, 0.36f, 0.52f, 0.98f) : new Color(0.05f, 0.24f, 0.38f, 0.96f))
            : new Color(0.04f, 0.07f, 0.13f, 0.94f);

        DrawCyberBox(r, bgCol, borderCol, ready ? 3f : 2f);

        // Relleno progresivo de carga
        float chargeRatio = Mathf.Clamp01(currentUltCharge / Mathf.Max(1f, maxUltCharge));
        Rect fillRect = new Rect(r.x + 3f, r.y + 3f, (r.width - 6f) * chargeRatio, r.height - 6f);
        GUI.color = ready ? new Color(0.18f, 0.82f, 1f, 0.36f * pulse) : new Color(0.12f, 0.48f, 0.72f, 0.28f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

        // Barra de acento superior brillante en el botón Ultimate
        Rect topAccent = ScaleRect(baseRect.x + 2f, baseRect.y + 2f, baseRect.width - 4f, 6f);
        GUI.color = ready ? colGoldYel : colCyanNexo;
        GUI.DrawTexture(topAccent, Texture2D.whiteTexture);
        GUI.color = Color.white;

        string mainTitle = ready ? "★ ¡LANZAR SOBRECARGA NEXO! ★" : "⚡ SOBRECARGA NEXO (ULTIMATE)";
        string subTitle = ready
            ? "LISTA PARA DISPARAR • BARRE LA HORDA (475% ATK)"
            : $"CARGANDO CAÑÓN DE PLASMA... {Mathf.FloorToInt(currentUltCharge)}%";

        DrawStyledText(ScaleRect(baseRect.x + 12f, baseRect.y + 14f, baseRect.width - 24f, 42f), mainTitle, ready ? 22 : 19, TextAnchor.MiddleCenter, ready ? colCyanNexo : Color.white);
        DrawStyledText(ScaleRect(baseRect.x + 12f, baseRect.y + 58f, baseRect.width - 24f, 34f), subTitle, 15, TextAnchor.MiddleCenter, ready ? colGoldYel : new Color(0.75f, 0.88f, 1f));

        if (ready && GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            KaelAutoController kael = FindFirstObjectByType<KaelAutoController>();
            if (kael != null) kael.TriggerUltimate();
        }
    }

    bool DrawMobileButton(Rect scaledRect, string bigIcon, string subLabel, Color borderCol, bool enabled)
    {
        bool hovered = scaledRect.Contains(Event.current.mousePosition);
        Color bg = hovered ? new Color(0.10f, 0.15f, 0.26f, 0.98f) : colBgDark;
        DrawCyberBox(scaledRect, bg, borderCol, 2f);

        Rect topHalf = new Rect(scaledRect.x, scaledRect.y + scaledRect.height * 0.06f, scaledRect.width, scaledRect.height * 0.50f);
        Rect botHalf = new Rect(scaledRect.x, scaledRect.y + scaledRect.height * 0.52f, scaledRect.width, scaledRect.height * 0.42f);

        DrawStyledText(topHalf, bigIcon, 20, TextAnchor.MiddleCenter, borderCol);
        DrawStyledText(botHalf, subLabel, 13, TextAnchor.MiddleCenter, Color.white);
        return enabled && GUI.Button(scaledRect, GUIContent.none, GUIStyle.none);
    }

    void DrawSkillTreeModal()
    {
        GUI.color = new Color(0f, 0f, 0.05f, 0.86f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = ScaleRect(360, 210, 1200, 560);
        DrawCyberBox(box, colBgDark, colExpPurp, 3f);

        DrawStyledText(ScaleRect(360, 238, 1200, 45), $"✦ EVOLUCIÓN DEL GUANTELETE NEXO ({skillPoints} PUNTOS DISPONIBLES) ✦", 28, TextAnchor.MiddleCenter, colExpPurp);
        DrawStyledText(ScaleRect(360, 288, 1200, 35), "Elige una de las 3 ramas oficiales para potenciar permanentemente a Kael:", 18, TextAnchor.MiddleCenter, Color.white);

        // Rama 1: Furia
        Rect rFuria = ScaleRect(410, 350, 340, 310);
        DrawCyberBox(rFuria, new Color(0.24f, 0.08f, 0.12f), new Color(1f, 0.35f, 0.4f), 2.5f);
        DrawStyledText(ScaleRect(410, 370, 340, 35), $"🔥 RAMA FURIA (Nv.{branchFuria})", 22, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.4f));
        DrawStyledText(ScaleRect(430, 425, 300, 120), "Sobrecarga los servomotores de impacto para destrozar blindajes.\n\n+18% Daño Total\n+5% Probabilidad Crítica", 16, TextAnchor.MiddleCenter, Color.white, true);
        DrawStyledText(ScaleRect(430, 585, 300, 45), "TOCAR PARA MEJORAR", 17, TextAnchor.MiddleCenter, colGoldYel);
        if (GUI.Button(rFuria, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            branchFuria++;
            skillPoints--;
            RecalculateStats();
            if (skillPoints <= 0) ToggleSkillTreeModal(false);
        }

        // Rama 2: Guardián
        Rect rGuard = ScaleRect(790, 350, 340, 310);
        DrawCyberBox(rGuard, new Color(0.06f, 0.22f, 0.16f), colHpGreen, 2.5f);
        DrawStyledText(ScaleRect(790, 370, 340, 35), $"🛡 RAMA GUARDIÁN (Nv.{branchGuardian})", 22, TextAnchor.MiddleCenter, colHpGreen);
        DrawStyledText(ScaleRect(810, 425, 300, 120), "Genera un campo bio-protector que repara tejidos en combate.\n\n+40 Vida Máxima\n+3.5 Regeneración HP/seg", 16, TextAnchor.MiddleCenter, Color.white, true);
        DrawStyledText(ScaleRect(810, 585, 300, 45), "TOCAR PARA MEJORAR", 17, TextAnchor.MiddleCenter, colGoldYel);
        if (GUI.Button(rGuard, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            branchGuardian++;
            skillPoints--;
            RecalculateStats();
            currentHP = maxHP;
            if (skillPoints <= 0) ToggleSkillTreeModal(false);
        }

        // Rama 3: Nexo
        Rect rNexo = ScaleRect(1170, 350, 340, 310);
        DrawCyberBox(rNexo, new Color(0.06f, 0.18f, 0.30f), colCyanNexo, 2.5f);
        DrawStyledText(ScaleRect(1170, 370, 340, 35), $"⚡ RAMA NEXO (Nv.{branchNexo})", 22, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(ScaleRect(1190, 425, 300, 120), "Sincroniza la disonancia del Guantelete para disparar el cañón más seguido.\n\n+25% Velocidad de Carga de Ultimate", 16, TextAnchor.MiddleCenter, Color.white, true);
        DrawStyledText(ScaleRect(1190, 585, 300, 45), "TOCAR PARA MEJORAR", 17, TextAnchor.MiddleCenter, colGoldYel);
        if (GUI.Button(rNexo, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            branchNexo++;
            skillPoints--;
            RecalculateStats();
            if (skillPoints <= 0) ToggleSkillTreeModal(false);
        }

        Rect btnClose = ScaleRect(810, 685, 300, 55);
        DrawCyberBox(btnClose, new Color(0.15f, 0.18f, 0.25f), Color.gray, 2f);
        DrawStyledText(btnClose, "CERRAR", 18, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(btnClose, GUIContent.none, GUIStyle.none)) ToggleSkillTreeModal(false);
    }

    void DrawPauseModal()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = ScaleRect(660, 270, 600, 460);
        DrawCyberBox(box, colBgDark, colCyanNexo, 3f);

        DrawStyledText(ScaleRect(660, 298, 600, 50), "PAUSA TÁCTICA", 32, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(ScaleRect(660, 348, 600, 32), zoneName, 18, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.95f));

        Rect btnResume = ScaleRect(780, 400, 360, 60);
        DrawCyberBox(btnResume, new Color(0.1f, 0.35f, 0.25f), colHpGreen, 2.5f);
        DrawStyledText(btnResume, "▶ CONTINUAR COMBATE", 20, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(btnResume, GUIContent.none, GUIStyle.none)) TogglePause();

        Rect btnRestart = ScaleRect(780, 476, 360, 55);
        DrawCyberBox(btnRestart, new Color(0.25f, 0.1f, 0.15f), new Color(1f, 0.35f, 0.4f), 2f);
        DrawStyledText(btnRestart, "↻ REINICIAR NIVEL", 17, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(btnRestart, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        Rect btnMenu = ScaleRect(780, 546, 360, 55);
        DrawCyberBox(btnMenu, new Color(0.08f, 0.18f, 0.30f), colCyanNexo, 2f);
        DrawStyledText(btnMenu, "⌂ SALIR AL MENÚ PRINCIPAL", 17, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(btnMenu, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        // Control de Audio dentro del menú de Pausa
        DrawTopRightAudioWidget(809f, 622f, 302f, 68f);
    }

    void DrawGameOverModal(WaveSpawner spawner)
    {
        GUI.color = new Color(0.08f, 0f, 0.02f, 0.84f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = ScaleRect(560, 250, 800, 460);
        DrawCyberBox(box, colBgDark, new Color(1f, 0.25f, 0.35f), 3f);

        int waveReached = (spawner != null) ? spawner.currentWave : 1;
        DrawStyledText(ScaleRect(560, 285, 800, 50), "⚠ KAEL HA CAÍDO EN COMBATE ⚠", 34, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.35f));
        DrawStyledText(ScaleRect(620, 350, 680, 75),
            "\"El Guantelete Nexo te reconstruye. Devuélveles el error a los Vorst.\"\n" +
            $"Caíste en la Oleada {waveReached}. Conservas TODO tu Oro ({gold}) y tus Mejoras actuales.",
            20, TextAnchor.MiddleCenter, Color.white, true);

        DrawStyledText(ScaleRect(620, 440, 680, 40),
            "Tip: Usa tu oro acumulado en las tarjetas de abajo al reintentar para superar el muro.",
            17, TextAnchor.MiddleCenter, colGoldYel, true);

        Rect btnRetry = ScaleRect(730, 515, 460, 75);
        DrawCyberBox(btnRetry, new Color(0.08f, 0.35f, 0.25f), colHpGreen, 3f);
        DrawStyledText(btnRetry, "⚡ RECONSTRUIR Y REINTENTAR (CONSERVAR PROGRESO)", 19, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(btnRetry, GUIContent.none, GUIStyle.none))
        {
            RetryFromGameOver();
        }
    }

    void DrawNarrativeDilemmaModal()
    {
        GUI.color = new Color(0f, 0f, 0.05f, 0.86f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = ScaleRect(460, 200, 1000, 560);
        DrawCyberBox(box, colBgDark, colCyanNexo, 3f);

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

        DrawStyledText(ScaleRect(460, 230, 1000, 45), title, 27, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(ScaleRect(520, 285, 880, 80), desc, 19, TextAnchor.MiddleCenter, Color.white, true);

        Rect btnCompassion = ScaleRect(520, 395, 420, 280);
        DrawCyberBox(btnCompassion, new Color(0.06f, 0.22f, 0.32f), colCyanNexo, 2.5f);
        DrawStyledText(ScaleRect(540, 415, 380, 40), "♦ COMPASIÓN (HUMANIDAD)", 22, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(ScaleRect(545, 470, 370, 110), compDesc, 17, TextAnchor.MiddleCenter, Color.white, true);
        DrawStyledText(ScaleRect(540, 595, 380, 50), "+1 COMPASIÓN\n+20% VIDA MÁXIMA PERMANENTE", 17, TextAnchor.MiddleCenter, colHpGreen, true);

        if (GUI.Button(btnCompassion, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
                AudioManager.Instance.PlayVictoryDilemmaMusic();
            }
            compassionPoints++;
            choiceSummary = isLv2 ? "Liberaste a los prisioneros de la Base Vorst (+1 Compasión)" : "Salvaste a los sobrevivientes (+1 Compasión, +20% HP)";
            RecalculateStats();
            currentHP = maxHP;
            isChoosingDilemma = false;
            isLevelCompleted = true;
            SaveProgressForNextLevel();
        }

        Rect btnRigor = ScaleRect(980, 395, 420, 280);
        DrawCyberBox(btnRigor, new Color(0.25f, 0.08f, 0.18f), new Color(1f, 0.35f, 0.55f), 2.5f);
        DrawStyledText(ScaleRect(1000, 415, 380, 40), "❖ RIGOR (DISONANCIA NEXO)", 22, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.6f));
        DrawStyledText(ScaleRect(1005, 470, 370, 110), rigorDesc, 17, TextAnchor.MiddleCenter, Color.white, true);
        DrawStyledText(ScaleRect(1000, 595, 380, 50), "+1 RIGOR\n+25% DAÑO DE ATAQUE PERMANENTE", 17, TextAnchor.MiddleCenter, colGoldYel, true);

        if (GUI.Button(btnRigor, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
                AudioManager.Instance.PlayVictoryDilemmaMusic();
            }
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
        GUI.color = new Color(0f, 0f, 0f, 0.84f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect box = ScaleRect(510, 200, 900, 560);
        DrawCyberBox(box, colBgDark, colGoldYel, 3.5f);

        int kills = (spawner != null) ? spawner.totalKillsInLevel : 60;
        int timeSec = Mathf.RoundToInt(levelTimer);
        int timeBonus = Mathf.Max(100, 1000 - (timeSec * 4));
        int totalScore = (gold * 10) + (kills * 25) + timeBonus;

        string vicTitle = (levelNumber == 2) ? "★ ¡BASE VORST CONQUISTADA! ★" : "★ ¡SECTOR 7 LIBERADO! ★";
        DrawStyledText(ScaleRect(510, 230, 900, 50), vicTitle, 36, TextAnchor.MiddleCenter, colGoldYel);
        DrawStyledText(ScaleRect(560, 285, 800, 35), $"Decisión tomada: <color=#38e1ff>{choiceSummary}</color>", 19, TextAnchor.MiddleCenter, Color.white);

        DrawCyberBox(ScaleRect(620, 340, 680, 240), new Color(0.04f, 0.05f, 0.09f), colBorder, 1.5f);
        DrawStyledText(ScaleRect(650, 355, 620, 38), $"TIEMPO EMPLEADO:   <color=#38e1ff>{timeSec / 60:00}:{timeSec % 60:00}</color>", 21, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(650, 400, 620, 38), $"VORST DERROTADOS:   <color=#38e1ff>{kills} enemigos</color>", 21, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(650, 445, 620, 38), $"NIVEL DE KAEL Y ORO:   <color=#ffd35c>Nv.{playerLevel}  ({gold} Oro)</color>", 21, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(650, 500, 620, 45), $"PUNTAJE TOTAL DEL NIVEL:   <color=#2ee678>{totalScore} PTS</color>", 24, TextAnchor.MiddleLeft, Color.white);

        bool hasNextLevel = !string.IsNullOrEmpty(nextLevelSceneName) && Application.CanStreamedLevelBeLoaded(nextLevelSceneName);

        if (hasNextLevel)
        {
            Rect btnNext = ScaleRect(570, 620, 450, 75);
            DrawCyberBox(btnNext, new Color(0.08f, 0.38f, 0.25f), colHpGreen, 3f);
            DrawStyledText(btnNext, $"▶ AVANZAR AL NIVEL {levelNumber + 1} (BASE VORST)", 20, TextAnchor.MiddleCenter, Color.white);
            if (GUI.Button(btnNext, GUIContent.none, GUIStyle.none))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                SaveProgressForNextLevel();
                Time.timeScale = 1f;
                SceneManager.LoadScene(nextLevelSceneName);
            }

            Rect btnMenu = ScaleRect(1040, 620, 310, 75);
            DrawCyberBox(btnMenu, new Color(0.08f, 0.25f, 0.38f), colCyanNexo, 2.5f);
            DrawStyledText(btnMenu, "⌂ MENÚ PRINCIPAL", 19, TextAnchor.MiddleCenter, Color.white);
            if (GUI.Button(btnMenu, GUIContent.none, GUIStyle.none))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                SaveProgressForNextLevel();
                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
            }
        }
        else
        {
            Rect btnReplay = ScaleRect(600, 620, 350, 75);
            DrawCyberBox(btnReplay, new Color(0.08f, 0.32f, 0.45f), colCyanNexo, 3f);
            DrawStyledText(btnReplay, $"↻ REPETIR NIVEL {levelNumber}", 20, TextAnchor.MiddleCenter, Color.white);
            if (GUI.Button(btnReplay, GUIContent.none, GUIStyle.none))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            Rect btnMenu = ScaleRect(970, 620, 350, 75);
            DrawCyberBox(btnMenu, new Color(0.08f, 0.25f, 0.38f), colGoldYel, 2.5f);
            DrawStyledText(btnMenu, "⌂ MENÚ PRINCIPAL", 20, TextAnchor.MiddleCenter, Color.white);
            if (GUI.Button(btnMenu, GUIContent.none, GUIStyle.none))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
            }
        }
    }

    void DrawCyberBox(Rect r, Color bg, Color border, float borderThickness)
    {
        float t = Mathf.Max(1f, Mathf.Round(borderThickness * Mathf.Min(Screen.width / 1920f, Screen.height / 1080f)));
        GUI.color = border;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = bg;
        GUI.DrawTexture(new Rect(r.x + t, r.y + t, r.width - t * 2f, r.height - t * 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawProgressBar(Rect r, float ratio, Color bgCol, Color fillCol, string label, int baseFontSize)
    {
        GUI.color = bgCol;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = fillCol;
        GUI.DrawTexture(new Rect(r.x, r.y, Mathf.Round(r.width * ratio), r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (!string.IsNullOrEmpty(label))
        {
            DrawStyledText(r, label, baseFontSize, TextAnchor.MiddleCenter, Color.white);
        }
    }

    void DrawStyledText(Rect r, string txt, int baseSize, TextAnchor align, Color col, bool wordWrap = false)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.richText = true;
        st.fontSize = ScaleFont(baseSize);
        st.fontStyle = FontStyle.Bold;
        st.alignment = align;
        st.wordWrap = wordWrap;

        string shadowTxt = System.Text.RegularExpressions.Regex.Replace(txt, "<color=[^>]+>|</color>", "");
        st.normal.textColor = new Color(0f, 0f, 0f, 0.92f);
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), shadowTxt, st);

        st.normal.textColor = col;
        GUI.Label(r, txt, st);
    }
}
