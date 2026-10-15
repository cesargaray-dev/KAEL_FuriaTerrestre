using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Economía y Progresión (Nivel y EXP)")]
    public int gold = 0;
    public int playerLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 25;
    public int skillPoints = 0;

    [Header("Carga de Habilidad Ultimate (0 a 100)")]
    public float currentUltCharge = 0f;
    public float maxUltCharge = 100f;
    public bool IsUltReady => currentUltCharge >= maxUltCharge;

    [Header("Niveles de Mejora con Oro")]
    public int levelAtk = 1;
    public int levelHp = 1;
    public int levelSpd = 1;

    [Header("Estadísticas en Combate")]
    public float maxHP = 100f;
    public float currentHP = 100f;
    public float attackDamage = 12f;
    public float attackRate = 1.2f;

    public event Action OnStatsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        RecalculateStats();
        currentHP = maxHP;
    }

    public void RecalculateStats()
    {
        // El nivel del jugador y las mejoras con oro suben el poder de Kael
        maxHP = 100f + ((levelHp - 1) * 25f) + ((playerLevel - 1) * 15f);
        attackDamage = 12f + ((levelAtk - 1) * 4f) + ((playerLevel - 1) * 3f);
        attackRate = 1.2f + ((levelSpd - 1) * 0.08f);

        OnStatsChanged?.Invoke();
    }

    public int GetUpgradeCost(int currentLv)
    {
        return Mathf.RoundToInt(25f * Mathf.Pow(1.15f, currentLv - 1));
    }

    public void AddRewards(int goldAmount, int expAmount)
    {
        gold += goldAmount;
        currentExp += expAmount;

        // Cargar la Ultimate un extra al matar enemigos (+15%)
        AddUltCharge(15f);

        // Revisar si sube de nivel con la EXP ganada
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            playerLevel++;
            skillPoints++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.35f);
            RecalculateStats();
            currentHP = maxHP; // Curar al subir de nivel
            Debug.Log($"¡SUBISTE AL NIVEL {playerLevel}! (+1 Punto de Habilidad)");
        }

        OnStatsChanged?.Invoke();
    }

    public void AddUltCharge(float amount)
    {
        currentUltCharge = Mathf.Clamp(currentUltCharge + amount, 0f, maxUltCharge);
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
        currentHP = Mathf.Max(0f, currentHP - dmg);
        OnStatsChanged?.Invoke();

        if (currentHP <= 0f)
        {
            Debug.Log("¡Kael cayó! Recuperando fuerzas sin perder Oro ni EXP...");
            currentHP = maxHP;
            OnStatsChanged?.Invoke();
        }
    }

    public bool TryUpgradeAtk()
    {
        int cost = GetUpgradeCost(levelAtk);
        if (gold < cost) return false;
        gold -= cost;
        levelAtk++;
        RecalculateStats();
        return true;
    }

    public bool TryUpgradeHp()
    {
        int cost = GetUpgradeCost(levelHp);
        if (gold < cost) return false;
        gold -= cost;
        levelHp++;
        RecalculateStats();
        currentHP = Mathf.Min(currentHP + 25f, maxHP);
        OnStatsChanged?.Invoke();
        return true;
    }

    // Panel visual de HUD integrado para probar todas las mecánicas en el Nivel 1
    void OnGUI()
    {
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 14;
        boxStyle.alignment = TextAnchor.UpperLeft;

        // Panel Superior: Estado de Kael (Vida, Nivel, EXP, Oro)
        GUI.Box(new Rect(15, 15, 360, 110), "");
        GUI.Label(new Rect(25, 22, 340, 25), $"<b>KAEL Nv. {playerLevel}</b>   |   EXP: {currentExp} / {expToNextLevel}   |   ORO: {gold}");
        GUI.Label(new Rect(25, 48, 340, 25), $"VIDA (HP): {Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}   |   ATK: {attackDamage}");
        GUI.Label(new Rect(25, 74, 340, 25), $"CARGA ULTIMATE: {Mathf.FloorToInt(currentUltCharge)}%");

        // Botón de ULTIMATE (Abajo a la derecha)
        GUI.enabled = IsUltReady;
        Color oldCol = GUI.backgroundColor;
        GUI.backgroundColor = IsUltReady ? Color.cyan : Color.gray;
        if (GUI.Button(new Rect(15, 135, 360, 45), IsUltReady ? "★ ¡LANZAR ULTIMATE NEXO! (LISTA) ★" : $"Cargando Ultimate... ({Mathf.FloorToInt(currentUltCharge)}%)"))
        {
            KaelAutoController kael = FindFirstObjectByType<KaelAutoController>();
            if (kael != null) kael.TriggerUltimate();
        }

        // Botones de Mejora con Oro (Abajo)
        GUI.enabled = gold >= GetUpgradeCost(levelAtk);
        GUI.backgroundColor = Color.yellow;
        if (GUI.Button(new Rect(15, 190, 175, 40), $"+Fuerza Nv.{levelAtk} ({GetUpgradeCost(levelAtk)} Oro)"))
        {
            TryUpgradeAtk();
        }

        GUI.enabled = gold >= GetUpgradeCost(levelHp);
        if (GUI.Button(new Rect(200, 190, 175, 40), $"+Vigor Nv.{levelHp} ({GetUpgradeCost(levelHp)} Oro)"))
        {
            TryUpgradeHp();
        }
        GUI.backgroundColor = oldCol;
        GUI.enabled = true;
    }
}