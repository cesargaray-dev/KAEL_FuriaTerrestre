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
    public bool isUpgradeTreeOpen = false;
    public bool isLevelCompleted = false;

    [Header("Economía, Nivel y Moral (Compasión vs Rigor)")]
    public int gold = 0;
    public int playerLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 25;
    public int skillPoints = 0;
    public int compassionPoints = 0;
    public int rigorPoints = 0;

    [Header("Rama 2: 3 Sendas del Guantelete Nexo (Puntos de Nivel)")]
    public int branchFuria = 0;    // Senda de la Furia: Aumenta daño crítico y ataque
    public int branchGuardian = 0; // Senda del Guardián: Regeneración de vida y HP
    public int branchNexo = 0;     // Senda del Nexo: Carga más rápida de la Ultimate

    [Header("Rama 1: 5 Atributos Base (Comprables con Oro)")]
    public int levelAtk = 1;  // Fuerza Nexo
    public int levelHp = 1;   // Vigor
    public int levelSpd = 1;  // Reflejos
    public int levelCrit = 1; // Instinto
    public int levelGold = 1; // Carroñero

    [Header("Rama 3: Arsenal y Armadura (Comprables con Oro)")]
    public int levelArmor = 0;       // Coraza de Titanio (+Defensa y +HP)
    public int levelThrusters = 0;   // Propulsores de Asalto (+Velocidad de Movimiento y +Cadencia)
    public int levelOvercharge = 0;  // Módulo de Sobrecarga (+Carga de Ultimate y +Daño Ult)
    public int levelSeismicEdge = 0; // Filo Sísmico Vorst (+Daño Golpe 3 del Combo y +Crítico)

    private int selectedTreeTab = 0; // 0 = Rama 1 (Atributos), 1 = Rama 2 (Evolución Nexo), 2 = Rama 3 (Arsenal y Armadura)
    private string choiceSummary = "";
    private float levelTimer = 0f;

    [Header("Carga de Habilidad Ultimate (0 a 100)")]
    public float currentUltCharge = 0f;
    public float maxUltCharge = 100f;
    public float ultCharge
    {
        get => currentUltCharge;
        set => currentUltCharge = Mathf.Clamp(value, 0f, maxUltCharge);
    }
    public bool IsUltReady => currentUltCharge >= maxUltCharge;

    [Header("Estadísticas Calculadas")]
    public float maxHP = 100f;
    public float currentHP = 100f;
    public float hpRegenPerSec = 1.0f;
    public float attackDamage = 12f;
    public float attackRate = 1.2f;
    public float critChance = 0.05f;
    public float goldMultiplier = 1.0f;
    public float armorDamageReduction = 0.0f;
    public float moveSpeedMultiplier = 1.0f;
    public float ultimateDamageMultiplier = 1.0f;
    public float comboFinisherMultiplier = 1.6f;

    public event Action OnStatsChanged;
    private Transform kaelTransform;

    // Paleta Cyberpunk Nexo
    private readonly Color colBgDark   = new Color(0.04f, 0.06f, 0.12f, 0.94f);
    private readonly Color colBorder   = new Color(0.22f, 0.88f, 1.00f, 0.88f);
    private readonly Color colHpGreen  = new Color(0.18f, 0.94f, 0.50f, 1.00f);
    private readonly Color colExpPurp  = new Color(0.70f, 0.40f, 1.00f, 1.00f);
    private readonly Color colGoldYel  = new Color(1.00f, 0.82f, 0.22f, 1.00f);
    private readonly Color colCyanNexo = new Color(0.22f, 0.90f, 1.00f, 1.00f);

    // Colores diferenciados por atributo
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
            LoadProgressIfContinuing();
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
        levelArmor = 0;
        levelThrusters = 0;
        levelOvercharge = 0;
        levelSeismicEdge = 0;
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
        PlayerPrefs.SetInt("KAEL_LvArmor", levelArmor);
        PlayerPrefs.SetInt("KAEL_LvThrusters", levelThrusters);
        PlayerPrefs.SetInt("KAEL_LvOvercharge", levelOvercharge);
        PlayerPrefs.SetInt("KAEL_LvSeismic", levelSeismicEdge);
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
                levelArmor = PlayerPrefs.GetInt("KAEL_LvArmor", levelArmor);
                levelThrusters = PlayerPrefs.GetInt("KAEL_LvThrusters", levelThrusters);
                levelOvercharge = PlayerPrefs.GetInt("KAEL_LvOvercharge", levelOvercharge);
                levelSeismicEdge = PlayerPrefs.GetInt("KAEL_LvSeismic", levelSeismicEdge);
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
        KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
        if (kael != null) kaelTransform = kael.transform;
    }

    void Update()
    {
        if (!isPaused && !isGameOver && !isLevelCompleted && !isChoosingDilemma && !isChoosingSkill && !isUpgradeTreeOpen)
        {
            levelTimer += Time.deltaTime;

            // Regeneración pasiva de vida en combate (aumenta con la Senda del Guardián)
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

        maxHP = Mathf.RoundToInt((100f + ((levelHp - 1) * 35f) + ((playerLevel - 1) * 20f) + (branchGuardian * 40f) + (levelArmor * 28f)) * hpBonus);
        hpRegenPerSec = 1.0f + (branchGuardian * 3.5f) + (levelArmor * 0.6f);
        attackDamage = Mathf.RoundToInt((12f + ((levelAtk - 1) * 6f) * Mathf.Pow(1.08f, levelAtk - 1) + ((playerLevel - 1) * 4f)) * atkBonus);
        attackRate = (1.2f + ((levelSpd - 1) * 0.14f)) * (1f + levelThrusters * 0.06f);
        critChance = Mathf.Clamp(0.05f + ((levelCrit - 1) * 0.04f) + (branchFuria * 0.05f) + (levelSeismicEdge * 0.03f), 0.05f, 0.85f);
        goldMultiplier = 1.0f + ((levelGold - 1) * 0.20f);

        // Bonificaciones de la Rama 3: Arsenal y Armadura
        armorDamageReduction = Mathf.Clamp(levelArmor * 0.06f, 0f, 0.35f);
        moveSpeedMultiplier = 1.0f + (levelThrusters * 0.10f);
        ultimateDamageMultiplier = 1.0f + (levelOvercharge * 0.12f);
        comboFinisherMultiplier = 1.6f + (levelSeismicEdge * 0.20f);

        OnStatsChanged?.Invoke();
    }

    public int GetUpgradeCost(int currentLv)
    {
        return Mathf.RoundToInt(20f * Mathf.Pow(1.18f, currentLv - 1));
    }

    public int GetArsenalCost(int baseCost, int currentLv)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(1.45f, currentLv));
    }

    public int CountAvailableUpgrades()
    {
        int count = 0;
        if (gold >= GetUpgradeCost(levelAtk)) count++;
        if (gold >= GetUpgradeCost(levelHp)) count++;
        if (gold >= GetUpgradeCost(levelSpd)) count++;
        if (gold >= GetUpgradeCost(levelCrit)) count++;
        if (gold >= GetUpgradeCost(levelGold)) count++;

        if (levelArmor < 5 && gold >= GetArsenalCost(30, levelArmor)) count++;
        if (levelThrusters < 5 && gold >= GetArsenalCost(28, levelThrusters)) count++;
        if (levelOvercharge < 5 && gold >= GetArsenalCost(38, levelOvercharge)) count++;
        if (levelSeismicEdge < 5 && gold >= GetArsenalCost(42, levelSeismicEdge)) count++;

        return count;
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
                    $"★ ¡NIVEL {playerLevel}! (+1 PTO EVOLUCIÓN) ★",
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
        float nexoBoost = 1f + (branchNexo * 0.25f) + (levelOvercharge * 0.18f);
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
        if (isLevelCompleted || isGameOver || isChoosingDilemma || isChoosingSkill || isUpgradeTreeOpen) return;

        bool guarded = (KaelAutoController.Instance != null && KaelAutoController.Instance.isGuarding);

        // Reducción de daño por Coraza de Titanio (Rama 3)
        float finalDmg = dmg * (1f - armorDamageReduction);

        // Postura Defensiva / Guardia Nexo (Tecla S): Reduce el daño recibido en un 40%
        if (guarded)
        {
            finalDmg *= 0.60f;
        }

        finalDmg = Mathf.Max(1f, finalDmg);
        currentHP = Mathf.Max(0f, currentHP - finalDmg);
        OnStatsChanged?.Invoke();

        if (CombatVFXManager.Instance != null && kaelTransform != null)
        {
            string popTxt = guarded ? $"🛡 -{Mathf.RoundToInt(finalDmg)} (GUARDIA)" : $"-{Mathf.RoundToInt(finalDmg)}";
            Color popCol = guarded ? colCyanNexo : new Color(1f, 0.25f, 0.3f);
            CombatVFXManager.Instance.SpawnFloatingText(
                kaelTransform.position + Vector3.up * (healthBarOffsetY + 0.2f),
                popTxt,
                popCol, guarded ? 16 : 15, 0.65f
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
        isUpgradeTreeOpen = false;
        isChoosingSkill = false;
        RecalculateStats();
        currentHP = maxHP;
        Time.timeScale = 1f;

        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.RestartWavesKeepingProgress();
        }
    }

    public void TriggerEndOfZoneDilemma()
    {
        isUpgradeTreeOpen = false;
        isChoosingSkill = false;
        isChoosingDilemma = true;
        Time.timeScale = 0f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVictoryDilemmaMusic();
        }
    }

    public void TogglePause()
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma) return;
        if (isUpgradeTreeOpen || isChoosingSkill)
        {
            CloseAllUpgradeModals();
            return;
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ToggleUpgradeTreeModal()
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma || isPaused) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        isUpgradeTreeOpen = !isUpgradeTreeOpen;
        isChoosingSkill = isUpgradeTreeOpen;
        if (isUpgradeTreeOpen && skillPoints > 0)
        {
            selectedTreeTab = 1; // Si tiene puntos de habilidad pendientes, abre directo en Evolución Nexo
        }
        Time.timeScale = isUpgradeTreeOpen ? 0f : 1f;
    }

    public void OpenUpgradeTreeAtTab(int tabIndex)
    {
        if (isLevelCompleted || isGameOver || isChoosingDilemma || isPaused) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        selectedTreeTab = Mathf.Clamp(tabIndex, 0, 2);
        isUpgradeTreeOpen = true;
        isChoosingSkill = true;
        Time.timeScale = 0f;
    }

    public void CloseAllUpgradeModals()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        isUpgradeTreeOpen = false;
        isChoosingSkill = false;
        Time.timeScale = 1f;
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
        if (skillPoints > 0 && !isPaused && !isGameOver && !isLevelCompleted && !isChoosingDilemma && !isUpgradeTreeOpen)
        {
            float pulse = 0.7f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.3f;
            Rect skillBtn = ScaleRect(535, 12, 225, 68);
            bool hoverSkill = skillBtn.Contains(Event.current.mousePosition);
            DrawCyberBox(skillBtn, hoverSkill ? new Color(0.26f, 0.12f, 0.44f, 0.98f) : new Color(0.18f, 0.08f, 0.32f, 0.95f), new Color(0.78f, 0.48f, 1f, pulse), 2.5f);
            DrawStyledText(ScaleRect(535, 16, 225, 32), $"✦ EVOLUCIÓN (+{skillPoints})", 16, TextAnchor.MiddleCenter, Color.white);
            DrawStyledText(ScaleRect(535, 44, 225, 26), "ABRIR RAMA NEXO [TAB]", 13, TextAnchor.MiddleCenter, colGoldYel);

            if (GUI.Button(skillBtn, GUIContent.none, GUIStyle.none))
            {
                OpenUpgradeTreeAtTab(1);
            }
        }

        // --- B) PANEL CENTRAL SUPERIOR: OLEADAS ---
        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
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

        // --- D) BARRA INFERIOR COMPACTA ESTILO ACTION RPG (Reemplaza las 5 tarjetas fijas) ---
        DrawActionRpgBottomBar();

        // --- E) VENTANAS MODALES EMERGENTES ---
        if (isUpgradeTreeOpen || isChoosingSkill) DrawUpgradeTreeModal();
        if (isPaused) DrawPauseModal();
        if (isGameOver) DrawGameOverModal(spawner);
        if (isChoosingDilemma) DrawNarrativeDilemmaModal();
        if (isLevelCompleted) DrawVictoryModal(spawner);
    }

    // =========================================================================
    // BARRA INFERIOR LIMPIA Y COMPACTA DE ACTION RPG
    // =========================================================================
    void DrawActionRpgBottomBar()
    {
        Rect dockRect = ScaleRect(0f, 994f, 1920f, 86f);
        DrawCyberBox(dockRect, new Color(0.02f, 0.03f, 0.08f, 0.94f), new Color(0.18f, 0.45f, 0.68f, 0.80f), 2f);

        // Línea superior luminosa del dock
        GUI.color = new Color(0.22f, 0.88f, 1f, 0.85f);
        GUI.DrawTexture(new Rect(dockRect.x, dockRect.y, dockRect.width, Mathf.Max(2f, Mathf.Round(2f * (Screen.height / 1080f)))), Texture2D.whiteTexture);
        GUI.color = Color.white;

        KaelAutoController kael = KaelAutoController.Instance;
        if (kael != null && Event.current.type == EventType.Repaint)
        {
            kael.hudMoveLeftHeld = false;
            kael.hudMoveRightHeld = false;
            kael.hudGuardHeld = false;
        }

        // 1. SECCIÓN IZQUIERDA: Controles Rápidos + Botones Táctiles/Mouse Opcionales (A / S / D)
        Rect ctrlBox = ScaleRect(18f, 1004f, 675f, 66f);
        DrawCyberBox(ctrlBox, new Color(0.04f, 0.07f, 0.14f, 0.95f), new Color(0.20f, 0.55f, 0.78f, 0.75f), 1.5f);

        // Mini-botones interactivos [◀ A], [🛡 S], [▶ D] para soporte híbrido Teclado + Mouse
        DrawHoldControlPill(new Rect(28f, 1012f, 76f, 50f), "◀ [A]", "IZQ", kael != null && (KaelAutoController.IsLeftHeld() || kael.hudMoveLeftHeld), colCyanNexo, () => { if (kael != null) kael.hudMoveLeftHeld = true; });
        DrawHoldControlPill(new Rect(110f, 1012f, 94f, 50f), "🛡 [S]", "GUARDIA", kael != null && kael.isGuarding, new Color(0.35f, 0.95f, 1f), () => { if (kael != null) kael.hudGuardHeld = true; });
        DrawHoldControlPill(new Rect(210f, 1012f, 76f, 50f), "▶ [D]", "DER", kael != null && (KaelAutoController.IsRightHeld() || kael.hudMoveRightHeld), colCyanNexo, () => { if (kael != null) kael.hudMoveRightHeld = true; });

        // Leyenda clara de controles rápidos solicitada
        DrawStyledText(ScaleRect(298f, 1010f, 385f, 26f),
            "<color=#38e1ff>[A / D]</color> Mover    <color=#38e1ff>[S]</color> Guardia (-40% Daño)",
            14, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(298f, 1036f, 385f, 26f),
            "<color=#ffd35c>[J / Espacio]</color> Atacar    <color=#2ee678>[K / Q]</color> Ultimate",
            14, TextAnchor.MiddleLeft, new Color(0.85f, 0.92f, 1f));

        // 2. BOTÓN DE ACCIÓN: ATACAR [J / ESPACIO] (con indicador de Combo 1-2-3)
        int comboStep = (kael != null) ? kael.currentCombo : 0;
        Rect atkRect = ScaleRect(708f, 1004f, 315f, 66f);
        bool hoverAtk = atkRect.Contains(Event.current.mousePosition);
        Color atkAccent = (comboStep == 3) ? colGoldYel : colAttrFuerza;
        DrawCyberBox(atkRect, hoverAtk ? new Color(0.26f, 0.10f, 0.12f, 0.98f) : new Color(0.16f, 0.06f, 0.09f, 0.96f), hoverAtk ? Color.white : atkAccent, 2.5f);

        Rect atkTopBar = ScaleRect(710f, 1006f, 311f, 5f);
        GUI.color = atkAccent;
        GUI.DrawTexture(atkTopBar, Texture2D.whiteTexture);
        GUI.color = Color.white;

        string comboLabel = (comboStep > 0) ? $"⚔ ATACAR [J] • COMBO {comboStep}/3" : "⚔ ATACAR [J / ESPACIO]";
        string comboSub = (comboStep == 2) ? "¡SIGUIENTE GOLPE: SÍSMICO EN ÁREA!" : "GOLPE MANUAL • CADENCIA " + $"{attackRate:F1}/s";
        DrawStyledText(ScaleRect(714f, 1010f, 303f, 30f), comboLabel, 17, TextAnchor.MiddleCenter, Color.white);
        DrawStyledText(ScaleRect(714f, 1038f, 303f, 24f), comboSub, 12, TextAnchor.MiddleCenter, (comboStep == 2) ? colGoldYel : new Color(1f, 0.78f, 0.72f));

        if (GUI.Button(atkRect, GUIContent.none, GUIStyle.none) && kael != null)
        {
            kael.TriggerManualAttack();
        }

        // 3. BOTÓN DE ACCIÓN: SOBRECARGA ULTIMATE [K / Q]
        DrawCompactUltimateButton(new Rect(1038f, 1004f, 385f, 66f));

        // 4. BOTÓN DESTACADO: 🧬 ÁRBOL DE MEJORAS [TAB / M] (Con globo/destello dorado cuando alcanza el oro)
        DrawUpgradeTreeDockButton(new Rect(1438f, 1004f, 464f, 66f));
    }

    void DrawHoldControlPill(Rect baseRect, string topTxt, string subTxt, bool isActive, Color accentCol, Action onHold)
    {
        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool hovered = r.Contains(Event.current.mousePosition);
        bool mouseHeld = hovered && Input.GetMouseButton(0);
        if (mouseHeld) onHold?.Invoke();

        bool lit = isActive || mouseHeld;
        Color bg = lit ? new Color(0.12f, 0.28f, 0.42f, 0.98f) : (hovered ? new Color(0.08f, 0.15f, 0.25f, 0.96f) : new Color(0.05f, 0.08f, 0.15f, 0.92f));
        Color border = lit ? Color.white : accentCol;
        DrawCyberBox(r, bg, border, lit ? 2f : 1.5f);

        DrawStyledText(ScaleRect(baseRect.x, baseRect.y + 4f, baseRect.width, 24f), topTxt, 14, TextAnchor.MiddleCenter, lit ? colGoldYel : accentCol);
        DrawStyledText(ScaleRect(baseRect.x, baseRect.y + 26f, baseRect.width, 20f), subTxt, 11, TextAnchor.MiddleCenter, Color.white);
    }

    void DrawCompactUltimateButton(Rect baseRect)
    {
        bool ready = IsUltReady && !isPaused && !isGameOver && !isLevelCompleted && !isChoosingDilemma && !isUpgradeTreeOpen;
        float pulse = ready ? (0.75f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.25f) : 1f;

        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool isHovered = r.Contains(Event.current.mousePosition);

        Color borderCol = ready
            ? (isHovered ? Color.white : new Color(0.22f, 0.95f, 1f, pulse))
            : new Color(0.22f, 0.42f, 0.58f, 0.85f);

        Color bgCol = ready
            ? (isHovered ? new Color(0.08f, 0.36f, 0.52f, 0.98f) : new Color(0.05f, 0.24f, 0.38f, 0.96f))
            : new Color(0.04f, 0.07f, 0.13f, 0.94f);

        DrawCyberBox(r, bgCol, borderCol, ready ? 2.5f : 1.5f);

        float chargeRatio = Mathf.Clamp01(currentUltCharge / Mathf.Max(1f, maxUltCharge));
        Rect fillRect = new Rect(r.x + 3f, r.y + 3f, (r.width - 6f) * chargeRatio, r.height - 6f);
        GUI.color = ready ? new Color(0.18f, 0.82f, 1f, 0.36f * pulse) : new Color(0.12f, 0.48f, 0.72f, 0.28f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

        Rect topAccent = ScaleRect(baseRect.x + 2f, baseRect.y + 2f, baseRect.width - 4f, 5f);
        GUI.color = ready ? colGoldYel : colCyanNexo;
        GUI.DrawTexture(topAccent, Texture2D.whiteTexture);
        GUI.color = Color.white;

        string mainTitle = ready ? "⚡ ¡SOBRECARGA LISTA! [K / Q]" : $"⚡ SOBRECARGA [K] ({Mathf.FloorToInt(currentUltCharge)}%)";
        string subTitle = ready ? "CLIC O [K] PARA DISPARAR CAÑÓN NEXO" : "GOLPEA CON [J] PARA CARGAR ENERGÍA";

        DrawStyledText(ScaleRect(baseRect.x + 8f, baseRect.y + 8f, baseRect.width - 16f, 28f), mainTitle, 16, TextAnchor.MiddleCenter, ready ? colCyanNexo : Color.white);
        DrawStyledText(ScaleRect(baseRect.x + 8f, baseRect.y + 36f, baseRect.width - 16f, 24f), subTitle, 12, TextAnchor.MiddleCenter, ready ? colGoldYel : new Color(0.75f, 0.88f, 1f));

        if (ready && GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (KaelAutoController.Instance != null) KaelAutoController.Instance.TriggerUltimate();
        }
    }

    void DrawUpgradeTreeDockButton(Rect baseRect)
    {
        int affordableCount = CountAvailableUpgrades();
        bool hasNotification = (affordableCount > 0 || skillPoints > 0);
        float pulse = hasNotification ? (0.72f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5.5f)) * 0.28f) : 1f;

        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool isHovered = r.Contains(Event.current.mousePosition);

        Color borderCol = hasNotification
            ? (isHovered ? Color.white : new Color(1f, 0.84f, 0.22f, pulse))
            : (isHovered ? colCyanNexo : new Color(0.65f, 0.42f, 1f, 0.9f));

        Color bgCol = hasNotification
            ? (isHovered ? new Color(0.26f, 0.20f, 0.06f, 0.98f) : new Color(0.18f, 0.13f, 0.04f, 0.96f))
            : (isHovered ? new Color(0.14f, 0.10f, 0.26f, 0.98f) : new Color(0.08f, 0.06f, 0.18f, 0.95f));

        DrawCyberBox(r, bgCol, borderCol, hasNotification ? 3f : 2f);

        // Barra superior brillante
        Rect topAccent = ScaleRect(baseRect.x + 2f, baseRect.y + 2f, baseRect.width - 4f, 5f);
        GUI.color = hasNotification ? colGoldYel : colExpPurp;
        GUI.DrawTexture(topAccent, Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawStyledText(ScaleRect(baseRect.x + 12f, baseRect.y + 8f, baseRect.width - 24f, 28f),
            "🧬 ÁRBOL DE MEJORAS [TAB / M]", 18, TextAnchor.MiddleCenter, hasNotification ? colGoldYel : Color.white);

        string subStatus = (skillPoints > 0)
            ? $"★ ¡{skillPoints} PTO(S) EVOLUCIÓN + {affordableCount} MEJORAS DISPONIBLES!"
            : (affordableCount > 0 ? $"● ¡{affordableCount} MEJORA(S) DISPONIBLE(S) CON TU ORO!" : "MATRIZ DE ATRIBUTOS, EVOLUCIÓN Y ARSENAL");

        DrawStyledText(ScaleRect(baseRect.x + 12f, baseRect.y + 36f, baseRect.width - 24f, 24f),
            subStatus, 12, TextAnchor.MiddleCenter, hasNotification ? Color.white : new Color(0.80f, 0.86f, 1f));

        // Globo / insignia dorada flotante en la esquina superior derecha del botón cuando alcanza el oro
        if (hasNotification)
        {
            float bounce = Mathf.Sin(Time.unscaledTime * 7f) * 3f;
            Rect badgeRect = ScaleRect(baseRect.x + baseRect.width - 138f, baseRect.y - 18f + bounce, 130f, 24f);
            DrawCyberBox(badgeRect, new Color(1f, 0.80f, 0.12f, 1f), Color.white, 1.5f);
            string badgeTxt = (skillPoints > 0) ? $"¡+{skillPoints} PTO NEXO!" : $"¡{affordableCount} DISPONIBLES!";
            DrawStyledText(badgeRect, badgeTxt, 12, TextAnchor.MiddleCenter, new Color(0.1f, 0.06f, 0.01f));
        }

        if (GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            ToggleUpgradeTreeModal();
        }
    }

    // =========================================================================
    // VENTANA MODAL A PANTALLA COMPLETA: "ÁRBOL DE MEJORAS NEXO"
    // =========================================================================
    void DrawUpgradeTreeModal()
    {
        // Fondo translúcido cyberpunk a pantalla completa
        GUI.color = new Color(0.01f, 0.02f, 0.06f, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect panel = ScaleRect(48f, 34f, 1824f, 1012f);
        DrawCyberBox(panel, new Color(0.03f, 0.05f, 0.11f, 0.98f), colCyanNexo, 3f);

        // Barra superior decorativa del modal
        Rect topHeader = ScaleRect(52f, 38f, 1816f, 84f);
        GUI.color = new Color(0.06f, 0.11f, 0.22f, 0.95f);
        GUI.DrawTexture(topHeader, Texture2D.whiteTexture);
        GUI.color = colCyanNexo;
        GUI.DrawTexture(ScaleRect(52f, 120f, 1816f, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawStyledText(ScaleRect(76f, 46f, 820f, 40f), "🧬 MATRIZ DE EVOLUCIÓN — ÁRBOL DE MEJORAS NEXO", 26, TextAnchor.MiddleLeft, colCyanNexo);
        DrawStyledText(ScaleRect(76f, 84f, 820f, 28f),
            $"KAEL Nv.{playerLevel}   |   ATK: {attackDamage}   |   HP: {maxHP}   |   CRIT: {Mathf.RoundToInt(critChance * 100)}%   |   DEF: {Mathf.RoundToInt(armorDamageReduction * 100)}%   |   VEL: x{moveSpeedMultiplier:F2}",
            15, TextAnchor.MiddleLeft, new Color(0.82f, 0.92f, 1f));

        // Indicadores de Oro y Puntos de Evolución en la cabecera
        DrawCyberBox(ScaleRect(960f, 50f, 240f, 56f), new Color(0.12f, 0.09f, 0.02f, 0.95f), colGoldYel, 2f);
        DrawStyledText(ScaleRect(960f, 50f, 240f, 56f), $"● {gold} ORO", 22, TextAnchor.MiddleCenter, colGoldYel);

        DrawCyberBox(ScaleRect(1216f, 50f, 270f, 56f), new Color(0.12f, 0.06f, 0.22f, 0.95f), colExpPurp, 2f);
        DrawStyledText(ScaleRect(1216f, 50f, 270f, 56f), $"✦ {skillPoints} PTOS EVOLUCIÓN", 18, TextAnchor.MiddleCenter, skillPoints > 0 ? colGoldYel : colExpPurp);

        // Botón claro ✖ CERRAR ÁRBOL [TAB / ESC] en la esquina superior derecha
        Rect closeBtn = ScaleRect(1506f, 50f, 340f, 56f);
        bool hoverClose = closeBtn.Contains(Event.current.mousePosition);
        DrawCyberBox(closeBtn, hoverClose ? new Color(0.45f, 0.10f, 0.16f, 0.98f) : new Color(0.28f, 0.07f, 0.12f, 0.96f), new Color(1f, 0.35f, 0.45f), 2.5f);
        DrawStyledText(closeBtn, "✖ CERRAR ÁRBOL [TAB / ESC]", 18, TextAnchor.MiddleCenter, Color.white);
        if (GUI.Button(closeBtn, GUIContent.none, GUIStyle.none))
        {
            CloseAllUpgradeModals();
            return;
        }

        // --- SELECTOR DE LAS 3 PESTAÑAS / RAMAS DEL ÁRBOL ---
        int r1Count = (gold >= GetUpgradeCost(levelAtk) ? 1 : 0) + (gold >= GetUpgradeCost(levelHp) ? 1 : 0) +
                      (gold >= GetUpgradeCost(levelSpd) ? 1 : 0) + (gold >= GetUpgradeCost(levelCrit) ? 1 : 0) +
                      (gold >= GetUpgradeCost(levelGold) ? 1 : 0);
        int r2Count = skillPoints;
        int r3Count = (levelArmor < 5 && gold >= GetArsenalCost(30, levelArmor) ? 1 : 0) +
                      (levelThrusters < 5 && gold >= GetArsenalCost(28, levelThrusters) ? 1 : 0) +
                      (levelOvercharge < 5 && gold >= GetArsenalCost(38, levelOvercharge) ? 1 : 0) +
                      (levelSeismicEdge < 5 && gold >= GetArsenalCost(42, levelSeismicEdge) ? 1 : 0);

        DrawTreeTabButton(new Rect(76f, 138f, 564f, 64f), 0, "⚔ RAMA 1: ATRIBUTOS BASE", "5 Nodos de Estadísticas Conectados (Oro)", colCyanNexo, r1Count);
        DrawTreeTabButton(new Rect(660f, 138f, 564f, 64f), 1, "✦ RAMA 2: EVOLUCIÓN NEXO", "Sendas Activas y Pasivas del Guantelete", colExpPurp, r2Count);
        DrawTreeTabButton(new Rect(1244f, 138f, 564f, 64f), 2, "🛡 RAMA 3: ARSENAL Y ARMADURA", " blindaje, Propulsores y Módulos (Oro)", colGoldYel, r3Count);

        // Área interior del lienzo de circuitos
        Rect canvasRect = ScaleRect(76f, 216f, 1732f, 804f);
        DrawCyberBox(canvasRect, new Color(0.02f, 0.03f, 0.07f, 0.96f), new Color(0.15f, 0.36f, 0.55f, 0.65f), 1.5f);
        DrawCircuitGridBackground(canvasRect);

        if (selectedTreeTab == 0)
        {
            DrawBranch1BaseAttributesTree();
        }
        else if (selectedTreeTab == 1)
        {
            DrawBranch2NexoEvolutionTree();
        }
        else
        {
            DrawBranch3ArsenalArmorTree();
        }
    }

    void DrawTreeTabButton(Rect baseRect, int tabIdx, string title, string subtitle, Color accentCol, int badgeCount)
    {
        bool active = (selectedTreeTab == tabIdx);
        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool hovered = r.Contains(Event.current.mousePosition);

        Color bg = active
            ? new Color(accentCol.r * 0.28f, accentCol.g * 0.28f, accentCol.b * 0.28f, 0.98f)
            : (hovered ? new Color(0.08f, 0.12f, 0.22f, 0.96f) : new Color(0.04f, 0.06f, 0.12f, 0.92f));
        Color border = active ? Color.white : (hovered ? accentCol : new Color(accentCol.r, accentCol.g, accentCol.b, 0.55f));

        DrawCyberBox(r, bg, border, active ? 3f : 1.5f);

        if (active)
        {
            Rect topBar = ScaleRect(baseRect.x + 2f, baseRect.y + 2f, baseRect.width - 4f, 5f);
            GUI.color = accentCol;
            GUI.DrawTexture(topBar, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 8f, baseRect.width - 28f, 28f), title, 18, TextAnchor.MiddleCenter, active ? Color.white : accentCol);
        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 34f, baseRect.width - 28f, 22f), subtitle, 13, TextAnchor.MiddleCenter, new Color(0.78f, 0.86f, 0.96f));

        if (badgeCount > 0)
        {
            Rect badge = ScaleRect(baseRect.x + baseRect.width - 48f, baseRect.y + 10f, 36f, 26f);
            DrawCyberBox(badge, colGoldYel, Color.white, 1f);
            DrawStyledText(badge, $"+{badgeCount}", 13, TextAnchor.MiddleCenter, new Color(0.1f, 0.06f, 0.01f));
        }

        if (GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            selectedTreeTab = tabIdx;
        }
    }

    void DrawCircuitGridBackground(Rect canvasScaled)
    {
        GUI.color = new Color(0.12f, 0.35f, 0.55f, 0.08f);
        int vLines = 12;
        for (int i = 1; i < vLines; i++)
        {
            float x = canvasScaled.x + (canvasScaled.width / vLines) * i;
            GUI.DrawTexture(new Rect(x, canvasScaled.y, 1f, canvasScaled.height), Texture2D.whiteTexture);
        }
        int hLines = 6;
        for (int j = 1; j < hLines; j++)
        {
            float y = canvasScaled.y + (canvasScaled.height / hLines) * j;
            GUI.DrawTexture(new Rect(canvasScaled.x, y, canvasScaled.width, 1f), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    // Dibuja líneas de circuito en ángulo recto (ortogonales) con nodos de unión brillantes en coordenadas 1920x1080
    void DrawCircuitConnection(Vector2 startBase, Vector2 endBase, Color lineCol, bool activeGlow)
    {
        float pulse = activeGlow ? (0.75f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.25f) : 0.45f;
        Color c = new Color(lineCol.r, lineCol.g, lineCol.b, pulse);
        float midY = (startBase.y + endBase.y) * 0.5f;

        DrawBaseLineSegment(startBase.x, startBase.y, startBase.x, midY, 4f, c);
        DrawBaseLineSegment(startBase.x, midY, endBase.x, midY, 4f, c);
        DrawBaseLineSegment(endBase.x, midY, endBase.x, endBase.y, 4f, c);

        // Nodos de unión luminosos en los codos del circuito
        Rect joint1 = ScaleRect(startBase.x - 6f, midY - 6f, 12f, 12f);
        Rect joint2 = ScaleRect(endBase.x - 6f, midY - 6f, 12f, 12f);
        GUI.color = c;
        GUI.DrawTexture(joint1, Texture2D.whiteTexture);
        GUI.DrawTexture(joint2, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawBaseLineSegment(float x1, float y1, float x2, float y2, float thickness, Color col)
    {
        float minX = Mathf.Min(x1, x2) - thickness * 0.5f;
        float minY = Mathf.Min(y1, y2) - thickness * 0.5f;
        float w = Mathf.Abs(x2 - x1) + thickness;
        float h = Mathf.Abs(y2 - y1) + thickness;
        Rect r = ScaleRect(minX, minY, w, h);
        GUI.color = col;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // -------------------------------------------------------------------------
    // RAMA 1: ATRIBUTOS BASE (5 Nodos conectados en árbol tecnológico)
    // -------------------------------------------------------------------------
    void DrawBranch1BaseAttributesTree()
    {
        Vector2 coreCenter = new Vector2(942f, 305f);
        Vector2 node1Top = new Vector2(360f, 445f);
        Vector2 node2Top = new Vector2(942f, 445f);
        Vector2 node3Top = new Vector2(1524f, 445f);
        Vector2 node4Top = new Vector2(650f, 735f);
        Vector2 node5Top = new Vector2(1234f, 735f);

        // Líneas de circuito conectando el Núcleo con la Fila 1 y la Fila 1 con la Fila 2
        DrawCircuitConnection(new Vector2(coreCenter.x, 360f), node1Top, colAttrFuerza, true);
        DrawCircuitConnection(new Vector2(coreCenter.x, 360f), node2Top, colAttrVigor, true);
        DrawCircuitConnection(new Vector2(coreCenter.x, 360f), node3Top, colAttrReflejos, true);
        DrawCircuitConnection(new Vector2(node1Top.x, 665f), node4Top, colAttrInstinto, true);
        DrawCircuitConnection(new Vector2(node3Top.x, 665f), node5Top, colAttrCarronero, true);

        // Nodo Raíz Central
        Rect coreRect = ScaleRect(662f, 240f, 560f, 120f);
        DrawCyberBox(coreRect, new Color(0.05f, 0.12f, 0.24f, 0.98f), colCyanNexo, 2.5f);
        DrawStyledText(ScaleRect(662f, 250f, 560f, 36f), $"⚙ NÚCLEO BIOMECÁNICO KAEL (NIVEL {playerLevel})", 20, TextAnchor.MiddleCenter, colCyanNexo);
        DrawStyledText(ScaleRect(682f, 288f, 520f, 56f),
            "Sincroniza el oro extraído de los núcleos Vorst para potenciar permanentemente los 5 atributos primarios de combate.",
            14, TextAnchor.MiddleCenter, Color.white, true);

        // Fila Superior (3 Nodos Primarios)
        DrawAttributeTreeNode(new Rect(140f, 445f, 440f, 220f), "⚔ FUERZA NEXO", $"ATK Actual: {attackDamage}", "+6 Daño Base (+8% Escala)", "Incrementa la potencia de los 3 golpes del combo y el rayo.", colAttrFuerza, ref levelAtk, false);
        DrawAttributeTreeNode(new Rect(722f, 445f, 440f, 220f), "♥ VIGOR CELULAR", $"HP Máx: {maxHP}", "+35 HP Máx y Cura +40 HP", "Refuerza la fibra muscular y restaura salud al instante.", colAttrVigor, ref levelHp, true);
        DrawAttributeTreeNode(new Rect(1304f, 445f, 440f, 220f), "⚡ REFLEJOS NEURALES", $"Cadencia: {attackRate:F2}/s", "+0.14 Golpes por Segundo", "Acelera la respuesta de ataque al presionar [J / Espacio].", colAttrReflejos, ref levelSpd, false);

        // Fila Inferior (2 Nodos Especializados conectados)
        DrawAttributeTreeNode(new Rect(430f, 735f, 440f, 220f), "✦ INSTINTO LETAL", $"Crítico: {Mathf.RoundToInt(critChance * 100)}%", "+4% Probabilidad Crítica (x2 Daño)", "Detecta fracturas en el blindaje Vorst para impactos dobles.", colAttrInstinto, ref levelCrit, false);
        DrawAttributeTreeNode(new Rect(1014f, 735f, 440f, 220f), "⛃ CARROÑERO VORST", $"Multiplicador: x{goldMultiplier:F2}", "+20% Oro Extraído por Baja", "Maximiza la cosecha de oro de cada enemigo y jefe.", colAttrCarronero, ref levelGold, false);
    }

    void DrawAttributeTreeNode(Rect baseRect, string title, string currentStat, string bonusText, string loreDesc, Color attrColor, ref int level, bool healOnBuy)
    {
        int cost = GetUpgradeCost(level);
        bool canAfford = (gold >= cost);

        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool hovered = r.Contains(Event.current.mousePosition);

        Color border = canAfford ? (hovered ? Color.white : colGoldYel) : (hovered ? attrColor : new Color(attrColor.r, attrColor.g, attrColor.b, 0.65f));
        Color bg = canAfford ? new Color(0.07f, 0.11f, 0.22f, 0.98f) : new Color(0.04f, 0.06f, 0.12f, 0.95f);

        DrawCyberBox(r, bg, border, canAfford ? 2.5f : 1.5f);

        // Cabecera del nodo
        Rect header = ScaleRect(baseRect.x + 2f, baseRect.y + 2f, baseRect.width - 4f, 42f);
        GUI.color = new Color(attrColor.r * 0.30f, attrColor.g * 0.30f, attrColor.b * 0.30f, 0.92f);
        GUI.DrawTexture(header, Texture2D.whiteTexture);
        GUI.color = canAfford ? colGoldYel : attrColor;
        GUI.DrawTexture(ScaleRect(baseRect.x + 2f, baseRect.y + 2f, baseRect.width - 4f, 5f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 8f, baseRect.width - 110f, 30f), title, 18, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(baseRect.x + baseRect.width - 104f, baseRect.y + 8f, 90f, 30f), $"Nv.{level}", 18, TextAnchor.MiddleRight, attrColor);

        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 52f, baseRect.width - 28f, 28f), currentStat, 18, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 80f, baseRect.width - 28f, 24f), $"Bono: {bonusText}", 14, TextAnchor.MiddleLeft, attrColor);
        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 108f, baseRect.width - 28f, 42f), loreDesc, 13, TextAnchor.UpperLeft, new Color(0.76f, 0.85f, 0.95f), true);

        // Botón MEJORAR
        Rect btn = ScaleRect(baseRect.x + 14f, baseRect.y + 158f, baseRect.width - 28f, 48f);
        bool hoverBtn = btn.Contains(Event.current.mousePosition);
        Color btnBg = canAfford
            ? (hoverBtn ? new Color(1f, 0.86f, 0.25f, 1f) : new Color(0.92f, 0.72f, 0.12f, 1f))
            : new Color(0.12f, 0.15f, 0.22f, 0.95f);

        DrawCyberBox(btn, btnBg, canAfford ? Color.white : new Color(0.3f, 0.35f, 0.45f), 1.5f);
        DrawStyledText(btn, canAfford ? $"▲ MEJORAR NODO  •  {cost} ORO" : $"🔒 REQUIERE {cost} ORO", 16, TextAnchor.MiddleCenter, canAfford ? new Color(0.1f, 0.06f, 0.01f) : new Color(0.62f, 0.68f, 0.78f));

        if (canAfford && GUI.Button(btn, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            gold -= cost;
            level++;
            RecalculateStats();
            if (healOnBuy) currentHP = Mathf.Min(currentHP + 40f, maxHP);
        }
    }

    // -------------------------------------------------------------------------
    // RAMA 2: EVOLUCIÓN NEXO (3 Sendas Evolutivas con Nodos Desbloqueados / Por Desbloquear)
    // -------------------------------------------------------------------------
    void DrawBranch2NexoEvolutionTree()
    {
        DrawStyledText(ScaleRect(120f, 232f, 1640f, 34f),
            skillPoints > 0
                ? $"✦ TIENES {skillPoints} PUNTO(S) DE EVOLUCIÓN NEXO DISPONIBLE(S) — ELIGE UNA SENDA PARA DESBLOQUEAR SU SIGUIENTE NODO ✦"
                : "✦ SENDAS EVOLUTIVAS DEL GUANTELETE NEXO — SUBE DE NIVEL EN COMBATE O SINTETIZA UN NODO CON ORO ✦",
            18, TextAnchor.MiddleCenter, skillPoints > 0 ? colGoldYel : colExpPurp);

        DrawEvolutionPathColumn(
            140f, "🔥 SENDA DE LA FURIA", "Ofensiva Crítica y Ruptura",
            new Color(1f, 0.38f, 0.36f), ref branchFuria,
            "RANGO I: Puños de Ignición (+18% ATK, +5% CRIT)",
            "RANGO II: Fractura de Núcleo (+36% ATK, +10% CRIT)",
            "RANGO III+: Sobrecarga Letal (+54% ATK, +15% CRIT)"
        );

        DrawEvolutionPathColumn(
            722f, "🛡 SENDA DEL GUARDIÁN", "Bio-Coraza y Regeneración",
            colHpGreen, ref branchGuardian,
            "RANGO I: Tejido Nano-Activo (+40 HP, +3.5 HP/s)",
            "RANGO II: Escudo Celular (+80 HP, +7.0 HP/s)",
            "RANGO III+: Inmortalidad Nexo (+120 HP, +10.5 HP/s)"
        );

        DrawEvolutionPathColumn(
            1304f, "⚡ SENDA DEL NEXO", "Resonancia y Cañón de Plasma",
            colCyanNexo, ref branchNexo,
            "RANGO I: Condensador Ágil (+25% Carga Ultimate)",
            "RANGO II: Reactor de Flujo (+50% Carga Ultimate)",
            "RANGO III+: Singularidad Nexo (+75% Carga Ultimate)"
        );
    }

    void DrawEvolutionPathColumn(float baseX, string pathTitle, string pathSub, Color pathColor, ref int branchLv, string tier1Desc, string tier2Desc, string tier3Desc)
    {
        float colW = 440f;
        float centerX = baseX + colW * 0.5f;

        // Líneas verticales de circuito entre los 3 rangos de la senda
        DrawCircuitConnection(new Vector2(centerX, 370f), new Vector2(centerX, 435f), pathColor, branchLv >= 1);
        DrawCircuitConnection(new Vector2(centerX, 555f), new Vector2(centerX, 610f), pathColor, branchLv >= 2);
        DrawCircuitConnection(new Vector2(centerX, 730f), new Vector2(centerX, 785f), pathColor, branchLv >= 3);

        // Cabecera de la Senda
        Rect header = ScaleRect(baseX, 280f, colW, 90f);
        DrawCyberBox(header, new Color(0.05f, 0.08f, 0.16f, 0.98f), pathColor, 2.5f);
        DrawStyledText(ScaleRect(baseX, 290f, colW, 34f), $"{pathTitle} (Nv.{branchLv})", 20, TextAnchor.MiddleCenter, pathColor);
        DrawStyledText(ScaleRect(baseX, 326f, colW, 28f), pathSub, 14, TextAnchor.MiddleCenter, Color.white);

        // 3 Nodos Evolutivos conectados (Desbloqueados vs Por Desbloquear)
        DrawTierNodeBox(new Rect(baseX + 20f, 435f, colW - 40f, 120f), "NODO 1", tier1Desc, branchLv >= 1, branchLv == 0, pathColor);
        DrawTierNodeBox(new Rect(baseX + 20f, 610f, colW - 40f, 120f), "NODO 2", tier2Desc, branchLv >= 2, branchLv == 1, pathColor);
        DrawTierNodeBox(new Rect(baseX + 20f, 785f, colW - 40f, 120f), "NODO 3+", tier3Desc, branchLv >= 3, branchLv >= 2, pathColor);

        // Botón inferior para Evolucionar con Punto de Habilidad (o con Oro si no tiene puntos)
        int goldSynthCost = 65 + (branchLv * 45);
        bool canUseSkillPt = (skillPoints > 0);
        bool canUseGold = (!canUseSkillPt && gold >= goldSynthCost);
        bool canUpgrade = canUseSkillPt || canUseGold;

        Rect btn = ScaleRect(baseX + 20f, 924f, colW - 40f, 62f);
        bool hover = btn.Contains(Event.current.mousePosition);
        Color btnBg = canUpgrade
            ? (hover ? new Color(pathColor.r * 0.55f, pathColor.g * 0.55f, pathColor.b * 0.55f, 1f) : new Color(pathColor.r * 0.35f, pathColor.g * 0.35f, pathColor.b * 0.35f, 0.98f))
            : new Color(0.10f, 0.12f, 0.18f, 0.92f);

        DrawCyberBox(btn, btnBg, canUpgrade ? Color.white : new Color(0.3f, 0.35f, 0.45f), 2f);
        string btnTxt = canUseSkillPt
            ? "✦ EVOLUCIONAR SENDA (1 PTO NEXO)"
            : (canUseGold ? $"⚡ SINTETIZAR NODO ({goldSynthCost} ORO)" : $"🔒 REQUIERE 1 PTO NEXO O {goldSynthCost} ORO");

        DrawStyledText(btn, btnTxt, 15, TextAnchor.MiddleCenter, canUpgrade ? Color.white : new Color(0.62f, 0.68f, 0.78f));

        if (canUpgrade && GUI.Button(btn, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (canUseSkillPt) skillPoints--;
            else gold -= goldSynthCost;

            branchLv++;
            RecalculateStats();
            currentHP = maxHP;
        }
    }

    void DrawTierNodeBox(Rect baseRect, string nodeTag, string desc, bool unlocked, bool isNext, Color pathColor)
    {
        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        Color bg = unlocked
            ? new Color(pathColor.r * 0.22f, pathColor.g * 0.22f, pathColor.b * 0.22f, 0.96f)
            : (isNext ? new Color(0.07f, 0.10f, 0.18f, 0.95f) : new Color(0.03f, 0.04f, 0.08f, 0.90f));
        Color border = unlocked ? pathColor : (isNext ? colGoldYel : new Color(0.25f, 0.30f, 0.40f, 0.6f));

        DrawCyberBox(r, bg, border, unlocked ? 2.5f : 1.5f);

        string statusBadge = unlocked ? "✔ DESBLOQUEADO" : (isNext ? "◎ SIGUIENTE NODO" : "🔒 POR DESBLOQUEAR");
        Color badgeCol = unlocked ? colHpGreen : (isNext ? colGoldYel : new Color(0.55f, 0.62f, 0.72f));

        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 10f, 150f, 26f), nodeTag, 16, TextAnchor.MiddleLeft, unlocked ? Color.white : pathColor);
        DrawStyledText(ScaleRect(baseRect.x + baseRect.width - 210f, baseRect.y + 10f, 196f, 26f), statusBadge, 13, TextAnchor.MiddleRight, badgeCol);
        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 44f, baseRect.width - 28f, 64f), desc, 15, TextAnchor.MiddleLeft, unlocked ? Color.white : new Color(0.75f, 0.82f, 0.92f), true);
    }

    // -------------------------------------------------------------------------
    // RAMA 3: ARSENAL Y ARMADURA (4 Nodos Tecnológicos Comprables con Oro)
    // -------------------------------------------------------------------------
    void DrawBranch3ArsenalArmorTree()
    {
        Vector2 coreBottom = new Vector2(942f, 360f);
        Vector2 arm1Top = new Vector2(510f, 450f);
        Vector2 arm2Top = new Vector2(1374f, 450f);
        Vector2 arm3Top = new Vector2(510f, 740f);
        Vector2 arm4Top = new Vector2(1374f, 740f);

        DrawCircuitConnection(coreBottom, arm1Top, colCyanNexo, levelArmor > 0);
        DrawCircuitConnection(coreBottom, arm2Top, colHpGreen, levelThrusters > 0);
        DrawCircuitConnection(new Vector2(arm1Top.x, 675f), arm3Top, colExpPurp, levelOvercharge > 0);
        DrawCircuitConnection(new Vector2(arm2Top.x, 675f), arm4Top, colGoldYel, levelSeismicEdge > 0);

        // Nodo Central de Chasis
        Rect chassisRect = ScaleRect(622f, 240f, 640f, 120f);
        DrawCyberBox(chassisRect, new Color(0.08f, 0.08f, 0.16f, 0.98f), colGoldYel, 2.5f);
        DrawStyledText(ScaleRect(622f, 250f, 640f, 36f), "🛡 CHASIS DE COMBATE Y ARSENAL NEXO", 20, TextAnchor.MiddleCenter, colGoldYel);
        DrawStyledText(ScaleRect(642f, 288f, 600f, 56f),
            "Instala módulos de blindaje pesado, propulsores tácticos y condensadores de plasma usando el oro recolectado.",
            14, TextAnchor.MiddleCenter, Color.white, true);

        // 4 Nodos de Equipamiento y Técnicas Comprables con Oro
        DrawArsenalNode(
            new Rect(230f, 450f, 560f, 225f),
            "🛡 CORAZA DE TITANIO",
            $"Reducción Daño: {Mathf.RoundToInt(armorDamageReduction * 100)}%  |  +{levelArmor * 28} HP",
            "+6% Defensa Directa y +28 HP Máx por Nivel",
            "Placas de aleación Vorst que reducen todo el daño recibido (acumulable con el -40% de Guardia [S]).",
            colCyanNexo, 30, ref levelArmor, true
        );

        DrawArsenalNode(
            new Rect(1094f, 450f, 560f, 225f),
            "🚀 PROPULSORES DE ASALTO",
            $"Velocidad Mov: +{levelThrusters * 10}%  |  Cadencia: +{levelThrusters * 6}%",
            "+10% Velocidad [A / D] y +6% Velocidad de Golpe",
            "Micro-turbinas en las botas que permiten reposicionarse y esquivar ataques pesados de jefes.",
            colHpGreen, 28, ref levelThrusters, false
        );

        DrawArsenalNode(
            new Rect(230f, 740f, 560f, 225f),
            "🔋 MÓDULO DE SOBRECARGA",
            $"Carga Ulti: +{levelOvercharge * 18}%  |  Daño Ulti: +{levelOvercharge * 12}%",
            "+18% Velocidad de Carga y +12% Daño del Rayo [K]",
            "Inyectores de plasma que aceleran la recarga de la Sobrecarga Nexo en cada golpe manual.",
            colExpPurp, 38, ref levelOvercharge, false
        );

        DrawArsenalNode(
            new Rect(1094f, 740f, 560f, 225f),
            "⚔ FILO SÍSMICO VORST",
            $"Multiplicador 3er Golpe: {Mathf.RoundToInt(comboFinisherMultiplier * 100)}%  |  +{levelSeismicEdge * 3}% CRIT",
            "+20% Daño en el 3er Golpe del Combo y +3% Crítico",
            "Amplificador de onda de choque al completar el combo de 3 golpes con [J / Espacio].",
            colGoldYel, 42, ref levelSeismicEdge, false
        );
    }

    void DrawArsenalNode(Rect baseRect, string title, string currentStat, string bonusText, string desc, Color accentCol, int baseCost, ref int level, bool healOnBuy)
    {
        bool isMaxed = (level >= 5);
        int cost = isMaxed ? 0 : GetArsenalCost(baseCost, level);
        bool canAfford = (!isMaxed && gold >= cost);

        Rect r = ScaleRect(baseRect.x, baseRect.y, baseRect.width, baseRect.height);
        bool hovered = r.Contains(Event.current.mousePosition);

        Color border = isMaxed ? colHpGreen : (canAfford ? (hovered ? Color.white : colGoldYel) : accentCol);
        Color bg = (level > 0) ? new Color(0.06f, 0.11f, 0.20f, 0.98f) : new Color(0.04f, 0.05f, 0.10f, 0.95f);

        DrawCyberBox(r, bg, border, (canAfford || level > 0) ? 2.5f : 1.5f);

        Rect header = ScaleRect(baseRect.x + 2f, baseRect.y + 2f, baseRect.width - 4f, 42f);
        GUI.color = new Color(accentCol.r * 0.28f, accentCol.g * 0.28f, accentCol.b * 0.28f, 0.92f);
        GUI.DrawTexture(header, Texture2D.whiteTexture);
        GUI.color = isMaxed ? colHpGreen : (canAfford ? colGoldYel : accentCol);
        GUI.DrawTexture(ScaleRect(baseRect.x + 2f, baseRect.y + 2f, baseRect.width - 4f, 5f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string lvTag = (level == 0) ? "NO INSTALADO" : (isMaxed ? "Nv.5 (MÁX)" : $"Nv.{level} / 5");
        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 8f, baseRect.width - 170f, 30f), title, 18, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(baseRect.x + baseRect.width - 160f, baseRect.y + 8f, 146f, 30f), lvTag, 15, TextAnchor.MiddleRight, level > 0 ? colHpGreen : colGoldYel);

        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 50f, baseRect.width - 28f, 26f), currentStat, 16, TextAnchor.MiddleLeft, Color.white);
        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 76f, baseRect.width - 28f, 24f), $"Efecto: {bonusText}", 14, TextAnchor.MiddleLeft, accentCol);
        DrawStyledText(ScaleRect(baseRect.x + 14f, baseRect.y + 104f, baseRect.width - 28f, 48f), desc, 13, TextAnchor.UpperLeft, new Color(0.76f, 0.85f, 0.95f), true);

        Rect btn = ScaleRect(baseRect.x + 14f, baseRect.y + 162f, baseRect.width - 28f, 48f);
        Color btnBg = isMaxed
            ? new Color(0.08f, 0.30f, 0.20f, 0.95f)
            : (canAfford ? new Color(0.95f, 0.75f, 0.14f, 1f) : new Color(0.12f, 0.15f, 0.22f, 0.95f));

        DrawCyberBox(btn, btnBg, (canAfford || isMaxed) ? Color.white : new Color(0.3f, 0.35f, 0.45f), 1.5f);
        string btnTxt = isMaxed
            ? "✔ EQUIPAMIENTO AL NIVEL MÁXIMO"
            : (canAfford ? $"▲ {(level == 0 ? "FABRICAR E INSTALAR" : "MEJORAR MÓDULO")}  •  {cost} ORO" : $"🔒 REQUIERE {cost} ORO");

        DrawStyledText(btn, btnTxt, 15, TextAnchor.MiddleCenter, canAfford ? new Color(0.1f, 0.06f, 0.01f) : Color.white);

        if (canAfford && GUI.Button(btn, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            gold -= cost;
            level++;
            RecalculateStats();
            if (healOnBuy) currentHP = Mathf.Min(currentHP + 35f, maxHP);
        }
    }

    // =========================================================================
    // WIDGETS SUPERIORES Y DEMÁS MODALES DEL JUEGO
    // =========================================================================
    void DrawTopRightAudioWidget(float baseX, float baseY, float baseW, float baseH)
    {
        bool muted = (AudioManager.Instance != null) && AudioManager.Instance.isMuted;
        float vol = (AudioManager.Instance != null) ? AudioManager.Instance.masterVolume : 0.85f;
        int volPct = muted ? 0 : Mathf.RoundToInt(vol * 100f);

        Color accent = muted ? new Color(1f, 0.35f, 0.42f, 0.9f) : colCyanNexo;
        Rect panelRect = ScaleRect(baseX, baseY, baseW, baseH);
        DrawCyberBox(panelRect, colBgDark, accent, 2f);

        Rect muteBtn = ScaleRect(baseX + 8f, baseY + 8f, 64f, baseH - 16f);
        bool hoverMute = muteBtn.Contains(Event.current.mousePosition);
        DrawCyberBox(muteBtn, hoverMute ? new Color(0.14f, 0.22f, 0.34f, 0.98f) : new Color(0.07f, 0.11f, 0.18f, 0.95f), accent, 1.5f);
        DrawStyledText(muteBtn, muted ? "🔇" : "🔊", 22, TextAnchor.MiddleCenter, muted ? new Color(1f, 0.4f, 0.45f) : colCyanNexo);
        if (GUI.Button(muteBtn, GUIContent.none, GUIStyle.none))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }

        DrawStyledText(ScaleRect(baseX + 80f, baseY + 8f, baseW - 92f, 22f),
            muted ? "AUDIO: SILENCIADO" : $"VOLUMEN: {volPct}%",
            13, TextAnchor.MiddleLeft, muted ? new Color(1f, 0.5f, 0.55f) : Color.white);

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
            KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
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

        // Indicador flotante de Guardia Nexo cuando mantiene S
        if (KaelAutoController.Instance != null && KaelAutoController.Instance.isGuarding)
        {
            Rect guardTag = new Rect(x - 18f * sx, y - 22f * sy, barW + 36f * sx, 18f * sy);
            DrawStyledText(guardTag, "🛡 GUARDIA NEXO (-40%)", 12, TextAnchor.MiddleCenter, colCyanNexo);
        }

        GUI.color = Color.white;
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
            "Tip: Abre el Árbol de Mejoras [TAB / M] al reintentar para invertir tu oro acumulado.",
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
