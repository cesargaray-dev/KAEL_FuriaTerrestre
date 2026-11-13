using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Automated verification suite for Issue #21 ("Arsenal de Vanguardia"):
/// Validates gauntlet asset integrity, shop economy & N.E.X.A. drone purchase gating,
/// dynamic CombatVFXManager particle gradients, passive modifiers (crit, burn DoT, 5% lifesteal),
/// and PlayerPrefs save/load round-tripping.
/// </summary>
public static class ArsenalVanguardiaVerificationSuite
{
    private static int passed = 0;
    private static int failed = 0;
    private static readonly List<string> logs = new List<string>();

    [MenuItem("KAEL Tools/Verificar Arsenal de Vanguardia (Issue #21)")]
    public static void RunVerificationSuite()
    {
        passed = 0;
        failed = 0;
        logs.Clear();

        Debug.Log("================================================================================");
        Debug.Log(">>> STARTING ARSENAL DE VANGUARDIA VERIFICATION SUITE (ISSUE #21) <<<");
        Debug.Log("================================================================================");

        try
        {
            ConfigureAndVerifyGauntletAssets();
            VerifyShopEconomyVFXAndPassives();
            VerifySaveLoadPersistence();
        }
        catch (Exception ex)
        {
            failed++;
            string err = $"[CRITICAL] Exception during Arsenal verification: {ex.Message}\n{ex.StackTrace}";
            logs.Add(err);
            Debug.LogError(err);
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        Debug.Log("================================================================================");
        Debug.Log($"ARSENAL VERIFICATION SUMMARY: {passed} PASSED, {failed} FAILED");
        Debug.Log("================================================================================");
        foreach (string line in logs)
        {
            if (line.StartsWith("[FAIL]")) Debug.LogError(line);
            else Debug.Log(line);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(failed > 0 ? 1 : 0);
        }
    }

    static void Assert(bool cond, string id, string message)
    {
        if (cond)
        {
            passed++;
            logs.Add($"[PASS] {id}: {message}");
        }
        else
        {
            failed++;
            logs.Add($"[FAIL] {id}: {message}");
        }
    }

    static void InvokeNonPublic(object target, string methodName, params object[] args)
    {
        if (target == null) return;
        MethodInfo mi = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (mi != null) mi.Invoke(target, args);
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;
        FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (fi != null) fi.SetValue(target, value);
    }

    static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return fi != null ? (T)fi.GetValue(target) : default;
    }

    static void ConfigureAndVerifyGauntletAssets()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        string[] directories =
        {
            "Assets/_Project/Resources/StoryUI/Gauntlets",
            "Assets/_Project/Sprites/UI/Gauntlets"
        };

        foreach (string dir in directories)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (string file in Directory.GetFiles(dir, "*.png"))
            {
                string assetPath = file.Replace("\\", "/");
                TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (imp != null &&
                    (imp.textureType != TextureImporterType.Sprite ||
                     imp.filterMode != FilterMode.Point ||
                     imp.textureCompression != TextureImporterCompression.Uncompressed))
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.filterMode = FilterMode.Point;
                    imp.alphaIsTransparency = true;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.SaveAndReimport();
                }
            }
        }

        for (int i = 0; i < PlayerStats.GauntletCatalog.Length; i++)
        {
            GauntletSpec spec = PlayerStats.GauntletCatalog[i];
            Texture2D tex = PlayerStats.LoadResTex(spec.TextureResourcePath);
            Assert(
                tex != null && tex.width >= 128 && tex.height >= 128,
                $"A1_GauntletTex_{spec.Variant}",
                $"Loaded '{spec.TextureResourcePath}' ({(tex != null ? $"{tex.width}x{tex.height}" : "null")})");
        }

        Texture2D nexaTex = PlayerStats.LoadResTex("StoryUI/NexaDrone");
        Assert(nexaTex != null, "A1_NexaDroneTex", "Loaded 'StoryUI/NexaDrone' for N.E.X.A. shop card & visor.");
    }

    static void VerifyShopEconomyVFXAndPassives()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject mgrGo = new GameObject("TestManagers");
        CombatVFXManager vfx = mgrGo.AddComponent<CombatVFXManager>();
        InvokeNonPublic(vfx, "Awake");

        PlayerStats stats = mgrGo.AddComponent<PlayerStats>();
        InvokeNonPublic(stats, "Awake");
        stats.gold = 0;

        ArsenalShopUI shop = ArsenalShopUI.EnsureExists();
        if (shop != null && ArsenalShopUI.Instance == null) InvokeNonPublic(shop, "Awake");
        Assert(shop != null && ArsenalShopUI.Instance == shop, "A2_ShopInstance", "ArsenalShopUI auto-created and singleton bound.");

        // Check default state: Prototipo Cian unlocked, others locked, N.E.X.A. locked
        Assert(stats.IsGauntletUnlocked(GauntletVariant.PrototypeCyan) && stats.equippedGauntlet == GauntletVariant.PrototypeCyan,
            "A2_DefaultGauntlet", "Prototype Cyan is unlocked and equipped by default.");
        Assert(!stats.IsGauntletUnlocked(GauntletVariant.PyroReactor)
            && !stats.IsGauntletUnlocked(GauntletVariant.IgneousPlasma)
            && !stats.IsGauntletUnlocked(GauntletVariant.VorstVoid),
            "A2_LockedVariants", "Pyro, Igneous Plasma, and Vorst Void start locked.");
        Assert(!stats.hasPurchasedNexaDrone, "A2_NexaLockedByDefault", "N.E.X.A. companion starts locked until bought.");

        // Spawn N.E.X.A. instance like existing scenes have, verify it remains locked until bought
        GameObject droneGo = new GameObject("NexaDrone");
        NexaDroneCompanion drone = droneGo.AddComponent<NexaDroneCompanion>();
        InvokeNonPublic(drone, "Awake");
        InvokeNonPublic(drone, "Start");
        Assert(!drone.IsDroneUnlocked && !drone.TryAbsorbDamage(10f),
            "A2_NexaDormantUntilPurchased", "Scene-placed NexaDrone stays inactive and cannot absorb damage prior to shop purchase.");

        // Purchases must fail cleanly with insufficient gold
        Assert(!stats.PurchaseOrEquipGauntlet(GauntletVariant.PyroReactor), "A2_InsufficientGoldGauntlet", "Cannot buy Pyro-Reactor with 0 gold.");
        Assert(!stats.PurchaseNexaDrone(), "A2_InsufficientGoldNexa", "Cannot buy N.E.X.A. Drone with 0 gold.");

        // Spawn Kael + EnemyDummy to verify passives and purchase deployment
        GameObject kaelGo = new GameObject("KaelTest");
        kaelGo.transform.position = Vector3.zero;
        KaelAutoController kael = kaelGo.AddComponent<KaelAutoController>();
        InvokeNonPublic(kael, "Awake");
        InvokeNonPublic(kael, "Start");
        SetPrivateField(stats, "kaelTransform", kael.transform);

        GameObject enemyGo = new GameObject("TestEnemyDummy");
        enemyGo.transform.position = new Vector3(1.5f, 0f, 0f);
        EnemyDummy enemy = enemyGo.AddComponent<EnemyDummy>();
        enemy.maxHP = 500f;
        SetPrivateField(enemy, "currentHP", 500f);
        SetPrivateField(enemy, "kaelTarget", kael.transform);

        // Give sufficient gold (80 + 140 + 200 + 120 = 540g) and test buying all items
        stats.gold = 600;
        float baseCritBefore = stats.critChance;

        // 1) Buy & equip Pyro-Reactor (Crimson, 80g) -> check VFX gradient + Fire Burn DoT application
        Assert(stats.PurchaseOrEquipGauntlet(GauntletVariant.PyroReactor) && stats.gold == 520,
            "A3_BuyPyro", "Purchased Pyro-Reactor for 80 gold (600 -> 520).");
        Assert(vfx.ActiveGauntletVariant == GauntletVariant.PyroReactor
            && Vector4.Distance(vfx.activeAttackGradient.Evaluate(0.32f), PlayerStats.GetGauntletSpec(GauntletVariant.PyroReactor).PrimaryColor) < 0.02f,
            "A3_PyroGradient", "CombatVFXManager dynamic attack gradient immediately switched to Pyro Crimson.");
        Assert(Vector4.Distance(vfx.activeUltimateGradient.Evaluate(0.32f), PlayerStats.GetGauntletSpec(GauntletVariant.PyroReactor).PrimaryColor) < 0.02f,
            "A3_PyroUltGradient", "CombatVFXManager dynamic ultimate gradient immediately switched to Pyro Crimson.");

        vfx.SpawnHitSparks(Vector3.zero, true, 4);
        GameObject spawnedSpark = GameObject.Find("HitSpark");
        SpriteRenderer sparkSr = spawnedSpark != null ? spawnedSpark.GetComponent<SpriteRenderer>() : null;
        Color pyroExpected = PlayerStats.GetGauntletSpec(GauntletVariant.PyroReactor).CoreColor;
        Assert(sparkSr != null && Vector4.Distance(sparkSr.color, pyroExpected) < 0.02f,
            "A3_HitSparkUsesSparkParticle", "SpawnHitSparks creates SparkParticle tinted with active Pyro-Reactor gradient.");

        kael.DealGauntletHitDamage(enemy, 25f, isCrit: false, knockback: 0f, isCorrupcion: false);
        Assert(enemy.HasActiveFireBurn, "A4_PyroBurnApplied", "Pyro-Reactor hit applies continuous fire burn DoT onto EnemyDummy.");

        float hpBeforeBurnTick = enemy.CurrentHP;
        FieldInfo fbField = typeof(EnemyDummy).GetField("fireBurn", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fbField != null)
        {
            object fbVal = fbField.GetValue(enemy);
            fbVal.GetType().GetField("tickTimer").SetValue(fbVal, 0f);
            fbField.SetValue(enemy, fbVal);
        }
        InvokeNonPublic(enemy, "Update");
        Assert(enemy.CurrentHP < hpBeforeBurnTick,
            "A4_PyroBurnTickReducesHP", $"Pyro-Reactor burn tick reduces enemy HP ({hpBeforeBurnTick:F1} -> {enemy.CurrentHP:F1}).");

        // 2) Buy & equip Igneous Plasma (Gold, 140g) -> check VFX gradient + +15% Crit Chance
        Assert(stats.PurchaseOrEquipGauntlet(GauntletVariant.IgneousPlasma) && stats.gold == 380,
            "A3_BuyIgneous", "Purchased Igneous Plasma for 140 gold (520 -> 380).");
        Assert(vfx.ActiveGauntletVariant == GauntletVariant.IgneousPlasma
            && Vector4.Distance(vfx.activeAttackGradient.Evaluate(0.32f), PlayerStats.GetGauntletSpec(GauntletVariant.IgneousPlasma).PrimaryColor) < 0.02f,
            "A3_IgneousGradient", "CombatVFXManager dynamic gradient immediately switched to Igneous Gold.");
        Assert(Mathf.Abs((stats.critChance - baseCritBefore) - 0.15f) < 0.001f,
            "A4_IgneousCritBonus", $"Igneous Plasma increases critChance by exact +15% ({baseCritBefore:P0} -> {stats.critChance:P0}).");

        // 3) Buy & equip Vorst Void (Purple, 200g) -> check VFX gradient + 5% Lifesteal on real damage dealt
        Assert(stats.PurchaseOrEquipGauntlet(GauntletVariant.VorstVoid) && stats.gold == 180,
            "A3_BuyVorstVoid", "Purchased Vorst Void for 200 gold (380 -> 180).");
        Assert(vfx.ActiveGauntletVariant == GauntletVariant.VorstVoid
            && Vector4.Distance(vfx.activeAttackGradient.Evaluate(0.32f), PlayerStats.GetGauntletSpec(GauntletVariant.VorstVoid).PrimaryColor) < 0.02f,
            "A3_VoidGradient", "CombatVFXManager dynamic gradient immediately switched to Vorst Void Purple.");

        stats.maxHP = 200f;
        stats.currentHP = 100f;
        kael.DealGauntletHitDamage(enemy, 40f, isCrit: false, knockback: 0f, isCorrupcion: false);
        Assert(Mathf.Abs(stats.currentHP - 102f) < 0.01f,
            "A4_VorstVoidLifesteal", $"Vorst Void recovers 5% of 40 damage dealt (100.0 -> {stats.currentHP:F2} HP).");

        // Switch back to Prototype Cyan for 0 gold (already unlocked) -> immediate Crit normalization + Cyan gradient
        Assert(stats.PurchaseOrEquipGauntlet(GauntletVariant.PrototypeCyan) && stats.gold == 180,
            "A3_ReEquipPrototype", "Re-equipped Prototype Cyan at zero extra gold cost.");
        Assert(vfx.ActiveGauntletVariant == GauntletVariant.PrototypeCyan
            && Mathf.Abs(stats.critChance - baseCritBefore) < 0.001f,
            "A3_PrototypeReset", "Re-equipping Prototype restores Cyan gradient and removes Igneous +15% crit bonus.");

        // 4) Buy N.E.X.A. Drone (120g) -> verify drone unlocks and Bulwark shield activates
        Assert(stats.PurchaseNexaDrone() && stats.gold == 60 && stats.hasPurchasedNexaDrone && drone.IsDroneUnlocked,
            "A4_PurchaseNexaDrone", "Purchased N.E.X.A. Drone for 120g (180 -> 60) and unlocked companion.");
        drone.ActivateBulwarkProtocol();
        Assert(drone.TryAbsorbDamage(15f), "A4_NexaShieldWorksAfterBuy", "Purchased N.E.X.A. Drone activates Bulwark shield and absorbs incoming hit.");
    }

    static void VerifySaveLoadPersistence()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Snapshot PlayerPrefs keys touched during persistence test so user save isn't dirtied
        int prevSaved = PlayerPrefs.GetInt("KAEL_Saved", 0);
        int prevEq = PlayerPrefs.GetInt("KAEL_EquippedGauntlet", 0);
        int prevMask = PlayerPrefs.GetInt("KAEL_UnlockedGauntlets", 1);
        int prevDrone = PlayerPrefs.GetInt("KAEL_NexaDroneBought", 0);
        int prevLv = PlayerPrefs.GetInt("KAEL_UnlockedLevel", 1);

        try
        {
            GameObject go = new GameObject("PersistTest");
            PlayerStats writer = go.AddComponent<PlayerStats>();
            InvokeNonPublic(writer, "Awake");
            writer.levelNumber = 1;
            writer.gold = 500;
            writer.PurchaseOrEquipGauntlet(GauntletVariant.VorstVoid);
            writer.PurchaseNexaDrone();
            writer.SaveProgressForNextLevel();

            GameObject go2 = new GameObject("PersistLoadTest");
            PlayerStats reader = go2.AddComponent<PlayerStats>();
            reader.ResetSkillsToBase();
            reader.levelNumber = 2;
            reader.LoadProgressIfContinuing();

            Assert(reader.IsGauntletUnlocked(GauntletVariant.VorstVoid)
                && reader.equippedGauntlet == GauntletVariant.VorstVoid
                && reader.hasPurchasedNexaDrone,
                "A5_PersistenceRoundTrip", "Unlocked/equipped gauntlet variant and N.E.X.A. purchase state persist across levels via PlayerPrefs.");
        }
        finally
        {
            PlayerPrefs.SetInt("KAEL_Saved", prevSaved);
            PlayerPrefs.SetInt("KAEL_EquippedGauntlet", prevEq);
            PlayerPrefs.SetInt("KAEL_UnlockedGauntlets", prevMask);
            PlayerPrefs.SetInt("KAEL_NexaDroneBought", prevDrone);
            PlayerPrefs.SetInt("KAEL_UnlockedLevel", prevLv);
            PlayerPrefs.Save();
        }
    }
}
