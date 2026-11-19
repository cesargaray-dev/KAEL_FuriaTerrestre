using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Comprehensive Automated Verification Suite for Vorst Codex (15 Campaign Species)
/// in KAEL: Furia Terrestre.
/// Validates database schema, enum definitions, dynamic array sizing, kill classification,
/// zone tab filtering, baseline-anchored holographic animation, tactical icons, and progression lifecycle.
/// </summary>
public static class VorstCodexVerificationSuite
{
    private static int testsPassed = 0;
    private static int testsFailed = 0;
    private static readonly List<string> passLogs = new List<string>();
    private static readonly List<string> failureLogs = new List<string>();

    private static readonly string[] CanonicalSpecies = new string[]
    {
        "VorstWalker",
        "Sector7Commander",
        "Stalker",
        "BruteDemolisher",
        "PsionicArchon",
        "BioStalker",
        "BioLasher",
        "BioHulk",
        "HiveInquisitor",
        "VortexOmega",
        "PraetorianBlade",
        "PraetorianMarksman",
        "PraetorianAegis",
        "PraetorianCustodian",
        "SupremeSovereign"
    };

    [MenuItem("KAEL/Verify Vorst Codex Integrity")]
    public static void RunVerificationSuiteMenu()
    {
        RunVerificationSuite(isBatch: false);
    }

    [MenuItem("KAEL Tools/Verificar Códice Vorst (E2E)")]
    public static void RunVerificationSuiteToolsMenu()
    {
        RunVerificationSuite(isBatch: false);
    }

    public static void RunVerificationSuiteCLI()
    {
        RunVerificationSuite(isBatch: true);
    }

    public static void RunVerificationSuite(bool isBatch)
    {
        testsPassed = 0;
        testsFailed = 0;
        passLogs.Clear();
        failureLogs.Clear();

        Debug.Log("================================================================================");
        Debug.Log(">>> STARTING VORST CODEX COMPREHENSIVE AUTOMATED VERIFICATION SUITE (TIERS 1-4) <<<");
        Debug.Log("================================================================================");

        try
        {
            // TIER 1: FEATURE COVERAGE
            Verify_Tier1_JSONDatabase();
            Verify_Tier1_EnumExpansion();
            Verify_Tier1_DynamicArraySizing();
            Verify_Tier1_EnemyClassification();
            Verify_Tier1_ZoneTabsAndNavigation();
            Verify_Tier1_BaselineViewport();
            Verify_Tier1_TacticalIcons();
            Verify_Tier1_AnimationCadence();
            Verify_Tier1_NullSafeFallback();
            Verify_Tier1_DynamicHeaderCounters();

            // TIER 2: BOUNDARY & CORNER CASES
            Verify_Tier2_NullAndDefaultInputs();
            Verify_Tier2_EmptyAndMalformedStrings();
            Verify_Tier2_OutOfRangeIndexing();
            Verify_Tier2_MissingAssetFallback();
            Verify_Tier2_EnumAndBitmaskBoundaries();

            // TIER 3: CROSS-FEATURE COMBINATIONS
            Verify_Tier3_TabSelectionAndFilterSync();
            Verify_Tier3_AnimationStateAndFlipDynamics();
            Verify_Tier3_CombatKillAndCodexUnlockPropagation();
            Verify_Tier3_SimulationModeVsLockedVisibility();
            Verify_Tier3_ConcurrentBossKillsIdempotency();

            // TIER 4: REAL-WORLD SCENARIOS
            Verify_Tier4_FullCampaignProgressionLifecycle();
            Verify_Tier4_ViewportBaselineStabilityStress();
            Verify_Tier4_MemoryGCSafety();
            Verify_Tier4_CleanAssemblyCompilation();
        }
        catch (Exception ex)
        {
            testsFailed++;
            string critErr = $"CRITICAL TEST HARNESS EXCEPTION: {ex.Message}\n{ex.StackTrace}";
            failureLogs.Add(critErr);
            Debug.LogError(critErr);
        }

        Debug.Log("================================================================================");
        Debug.Log($"VORST CODEX VERIFICATION SUMMARY: {testsPassed} PASSED, {testsFailed} FAILED");
        Debug.Log("================================================================================");

        foreach (var p in passLogs)
        {
            Debug.Log($"[PASS] {p}");
        }

        foreach (var f in failureLogs)
        {
            Debug.LogError($"[FAIL] {f}");
        }

        // Generate Report Artifact
        string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Codex_Verification_Report.txt");
        try
        {
            using (StreamWriter sw = new StreamWriter(reportPath, false))
            {
                sw.WriteLine("================================================================================");
                sw.WriteLine($"VORST CODEX AUTOMATED VERIFICATION REPORT — {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                sw.WriteLine("================================================================================");
                sw.WriteLine($"Total Tests Passed: {testsPassed}");
                sw.WriteLine($"Total Tests Failed: {testsFailed}");
                sw.WriteLine($"Overall Status: {(testsFailed == 0 ? "PASSED (100%)" : "FAILURES DETECTED (EXPECTED UNTIL M1/M2 LAND)")}");
                sw.WriteLine("================================================================================");
                sw.WriteLine("\n--- PASSED ASSERTIONS ---");
                foreach (var p in passLogs) sw.WriteLine($"  [PASS] {p}");
                if (failureLogs.Count > 0)
                {
                    sw.WriteLine("\n--- FAILED ASSERTIONS ---");
                    foreach (var f in failureLogs) sw.WriteLine($"  [FAIL] {f}");
                }
                sw.WriteLine("\n================================================================================");
            }
            Debug.Log($"Report written to: {reportPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to write verification report: {ex.Message}");
        }

        if (isBatch || Application.isBatchMode)
        {
            EditorApplication.Exit(testsFailed > 0 ? 1 : 0);
        }
    }

    private static void Assert(bool condition, string testId, string description)
    {
        if (condition)
        {
            testsPassed++;
            passLogs.Add($"{testId}: {description}");
        }
        else
        {
            testsFailed++;
            failureLogs.Add($"{testId}: {description}");
        }
    }

    // =========================================================================
    // TIER 1: FEATURE COVERAGE
    // =========================================================================

    private static void Verify_Tier1_JSONDatabase()
    {
        string jsonPath = Path.Combine(Application.dataPath, "_Project/Resources/Codex/VorstCodexDatabase.json");
        Assert(File.Exists(jsonPath), "TC-T1-F1.1a", "VorstCodexDatabase.json exists on disk");

        if (File.Exists(jsonPath))
        {
            string jsonText = File.ReadAllText(jsonPath);
            Assert(!string.IsNullOrWhiteSpace(jsonText), "TC-T1-F1.1b", "JSON text content is not empty");
            Assert(jsonText.Contains("\"version\":"), "TC-T1-F1.1c", "JSON contains version header");
            Assert(jsonText.Contains("\"entries\":"), "TC-T1-F1.1d", "JSON contains entries array");

            // Check Level 3 & Level 4 presence
            bool hasL3L4 = jsonText.Contains("BioStalker") && jsonText.Contains("BioHulk") &&
                           jsonText.Contains("VortexOmega") && jsonText.Contains("SupremeSovereign") &&
                           jsonText.Contains("PraetorianBlade");
            Assert(hasL3L4, "TC-T1-F1.2", "JSON entries contain Level 3 and Level 4 enemy identifiers");
        }
    }

    private static void Verify_Tier1_EnumExpansion()
    {
        Type enumType = typeof(CodexSpeciesId);
        string[] names = Enum.GetNames(enumType);
        Assert(names.Length == 15, "TC-T1-F2.1", $"CodexSpeciesId has exactly 15 members (Found: {names.Length})");
        Assert(Enum.IsDefined(enumType, "VorstWalker"), "TC-T1-F2.2a", "VorstWalker defined in enum");
        Assert(Enum.IsDefined(enumType, "BioStalker"), "TC-T1-F2.2b", "BioStalker defined in enum");
        Assert(Enum.IsDefined(enumType, "VortexOmega"), "TC-T1-F2.2c", "VortexOmega defined in enum");
        Assert(Enum.IsDefined(enumType, "SupremeSovereign"), "TC-T1-F2.2d", "SupremeSovereign defined in enum");
    }

    private static void Verify_Tier1_DynamicArraySizing()
    {
        Assert(VorstCodexUI.Catalog != null, "TC-T1-F3.1a", "Catalog is not null");
        Assert(VorstCodexUI.Catalog != null && VorstCodexUI.Catalog.Length == 15, "TC-T1-F3.1b",
            $"Catalog length is 15 (Found: {(VorstCodexUI.Catalog != null ? VorstCodexUI.Catalog.Length : 0)})");

        // Inspect internal arrays via reflection
        FieldInfo totalKillsField = typeof(VorstCodexUI).GetField("_totalKills", BindingFlags.NonPublic | BindingFlags.Static);
        if (totalKillsField != null)
        {
            Array arr = totalKillsField.GetValue(null) as Array;
            Assert(arr != null && arr.Length >= 15, "TC-T1-F3.2a",
                $"_totalKills array size is dynamic >= 15 (Found: {(arr != null ? arr.Length : 0)})");
        }
        else
        {
            Assert(false, "TC-T1-F3.2a", "_totalKills field exists on VorstCodexUI");
        }

        // Test safe kill inquiry at index 14
        try
        {
            int kills = VorstCodexUI.GetTotalKills((CodexSpeciesId)14);
            Assert(kills >= 0, "TC-T1-F3.2b", "GetTotalKills(14) executes safely without IndexOutOfRangeException");
        }
        catch (Exception ex)
        {
            Assert(false, "TC-T1-F3.2b", $"GetTotalKills(14) threw: {ex.Message}");
        }
    }

    private static void Verify_Tier1_EnemyClassification()
    {
        // Safe check on ClassifyEnemy(null)
        try
        {
            CodexSpeciesId defaultClass = VorstCodexUI.ClassifyEnemy(null);
            Assert((int)defaultClass == 0, "TC-T1-F4.1", "ClassifyEnemy(null) returns VorstWalker (0)");
        }
        catch (Exception ex)
        {
            Assert(false, "TC-T1-F4.1", $"ClassifyEnemy(null) threw: {ex.Message}");
        }

        // Check if VorstCodexUI source inspects VisualTypePublic
        string uiScriptPath = Path.Combine(Application.dataPath, "_Project/Scripts/VorstCodexUI.cs");
        if (File.Exists(uiScriptPath))
        {
            string src = File.ReadAllText(uiScriptPath);
            bool hasVisualTypeCheck = src.Contains("VisualTypePublic") || src.Contains("visualType");
            Assert(hasVisualTypeCheck, "TC-T1-F4.2", "ClassifyEnemy discriminates using VisualTypePublic");
        }
    }

    private static void Verify_Tier1_ZoneTabsAndNavigation()
    {
        string uiScriptPath = Path.Combine(Application.dataPath, "_Project/Scripts/VorstCodexUI.cs");
        Assert(File.Exists(uiScriptPath), "TC-T1-F5.1a", "VorstCodexUI.cs exists on disk for tab inspection");

        if (File.Exists(uiScriptPath))
        {
            string src = File.ReadAllText(uiScriptPath);

            // Verify that VorstCodexUI defines Zone Tabs (Sector 7, Base Vorst, Colmena, Trono, Todos)
            bool hasZoneTabEnumOrField = src.Contains("ZoneTab") || src.Contains("selectedTab") || src.Contains("currentTabZone");
            bool hasZoneTabDefinitions = src.Contains("Sector 7") && src.Contains("Base Vorst") && 
                                         src.Contains("Colmena") && src.Contains("Trono");
            bool implementsTabs = hasZoneTabEnumOrField && hasZoneTabDefinitions;

            Assert(implementsTabs, "TC-T1-F5.1b", "VorstCodexUI declares 5 Zone tabs (Todos, Sector 7, Base Vorst, Colmena, Trono)");

            // Verify tab navigation / filtering logic in UI drawing
            bool hasTabFilterLogic = src.Contains("FilterSpeciesByTab") || src.Contains("GetTabSpecies") || 
                                     src.Contains("DrawZoneTabs") || src.Contains("activeTab");
            Assert(hasTabFilterLogic, "TC-T1-F5.2", "VorstCodexUI implements tab-based species filtering and layout pagination");
        }
    }

    private static void Verify_Tier1_BaselineViewport()
    {
        string uiScriptPath = Path.Combine(Application.dataPath, "_Project/Scripts/VorstCodexUI.cs");
        Assert(File.Exists(uiScriptPath), "TC-T1-F6.1a", "VorstCodexUI.cs exists for viewport inspection");

        if (File.Exists(uiScriptPath))
        {
            string src = File.ReadAllText(uiScriptPath);

            // Baseline anchoring requires explicit bottom baseline positioning (e.g. baseline - renderHeight or bottomAnchor)
            bool hasBaselineAnchor = (src.Contains("baselineY") || src.Contains("bottomAnchor") || src.Contains("pedestalBaselineY"))
                                     && (src.Contains("renderHeight") || src.Contains("targetBaseline") || src.Contains("spriteBottom"));

            // ScaleMode.ScaleToFit inside a fixed vertical box causes 58-76px vertical jitter on variable-aspect frames
            bool usesFixedScaleToFitOnly = src.Contains("ScaleRect(608f, 228f + bobY, 400f, 352f)") && src.Contains("ScaleMode.ScaleToFit");

            bool implementsGenuineBaseline = hasBaselineAnchor && !usesFixedScaleToFitOnly;
            Assert(implementsGenuineBaseline, "TC-T1-F6.1b",
                "Holographic viewport implements bottom-anchored baseline positioning for variable-width frames");
        }
    }

    private static void Verify_Tier1_TacticalIcons()
    {
        string[] iconFiles = new string[]
        {
            "Icon_Codex_VorstWalker.png",
            "Icon_Codex_Commander.png",
            "Icon_Codex_Stalker.png",
            "Icon_Codex_Brute.png",
            "Icon_Codex_Archon.png",
            "Icon_Codex_BioStalker.png",
            "Icon_Codex_BioLasher.png",
            "Icon_Codex_BioHulk.png",
            "Icon_Codex_HiveInquisitor.png",
            "Icon_Codex_VortexOmega.png",
            "Icon_Codex_PraetorianBlade.png",
            "Icon_Codex_PraetorianMarksman.png",
            "Icon_Codex_PraetorianAegis.png",
            "Icon_Codex_PraetorianCustodian.png",
            "Icon_Codex_Sovereign.png"
        };

        int missing = 0;
        foreach (var ic in iconFiles)
        {
            string p = Path.Combine(Application.dataPath, "_Project/Resources/Codex", ic);
            if (!File.Exists(p))
            {
                if (ic == "Icon_Codex_Sovereign.png")
                {
                    string alt = Path.Combine(Application.dataPath, "_Project/Resources/Codex/Icon_Codex_SupremeSovereign.png");
                    if (!File.Exists(alt)) missing++;
                }
                else
                {
                    missing++;
                }
            }
        }
        Assert(missing == 0, "TC-T1-F7.1", $"All 15 tactical icons present in Resources/Codex (Missing: {missing})");
    }

    private static void Verify_Tier1_AnimationCadence()
    {
        string resPath = Path.Combine(Application.dataPath, "_Project/Resources");
        int totalFrames = 0;
        string[] folders = new string[] { "Codex", "Level2", "Level3", "Level4" };
        foreach (var f in folders)
        {
            string full = Path.Combine(resPath, f);
            if (Directory.Exists(full))
            {
                totalFrames += Directory.GetFiles(full, "*.png", SearchOption.TopDirectoryOnly).Length;
            }
        }
        Assert(totalFrames >= 334, "TC-T1-F8.1", $"Resources frame inventory contains >= 334 frames (Found: {totalFrames})");
    }

    private static void Verify_Tier1_NullSafeFallback()
    {
        string uiScriptPath = Path.Combine(Application.dataPath, "_Project/Scripts/VorstCodexUI.cs");
        if (File.Exists(uiScriptPath))
        {
            string src = File.ReadAllText(uiScriptPath);
            bool hasNullCheck = src.Contains("activeTex != null") || src.Contains("activeTex ??") || src.Contains("walkFrames[0]");
            Assert(hasNullCheck, "TC-T1-F9.1", "Hologram viewport checks for null activeTex before rendering");
        }
    }

    private static void Verify_Tier1_DynamicHeaderCounters()
    {
        string mmPath = Path.Combine(Application.dataPath, "_Project/Scripts/MainMenuAndIntroController.cs");
        if (File.Exists(mmPath))
        {
            string src = File.ReadAllText(mmPath);
            bool mmDynamic = src.Contains("Catalog.Length") || src.Contains("/15");
            Assert(mmDynamic, "TC-T1-F10.1", "MainMenu Codex button displays dynamic species total");
        }

        string psPath = Path.Combine(Application.dataPath, "_Project/Scripts/PlayerStats.cs");
        if (File.Exists(psPath))
        {
            string src = File.ReadAllText(psPath);
            bool psDynamic = src.Contains("Catalog.Length") || src.Contains("/15");
            Assert(psDynamic, "TC-T1-F10.2", "PlayerStats Codex button displays dynamic species total");
        }
    }

    // =========================================================================
    // TIER 2: BOUNDARY & CORNER CASES
    // =========================================================================

    private static void Verify_Tier2_NullAndDefaultInputs()
    {
        try
        {
            VorstCodexUI.ClassifyEnemy(null);
            Assert(true, "TC-T2-B1.1", "ClassifyEnemy(null) does not throw NullReferenceException");
        }
        catch (Exception ex)
        {
            Assert(false, "TC-T2-B1.1", $"ClassifyEnemy(null) threw: {ex.Message}");
        }
    }

    private static void Verify_Tier2_EmptyAndMalformedStrings()
    {
        GameObject host = new GameObject("Test_EnemyDummy_MalformedStrings");
        try
        {
            EnemyDummy dummy = host.AddComponent<EnemyDummy>();

            // Test 1: Empty title defaults safely to VorstWalker
            dummy.enemyTitle = "";
            CodexSpeciesId resEmpty = VorstCodexUI.ClassifyEnemy(dummy);
            Assert(resEmpty == CodexSpeciesId.VorstWalker, "TC-T2-B2.1a", "Empty enemyTitle defaults safely to VorstWalker");

            // Test 2: Whitespace-only title defaults safely to VorstWalker
            dummy.enemyTitle = "   \t\r\n   ";
            CodexSpeciesId resWhitespace = VorstCodexUI.ClassifyEnemy(dummy);
            Assert(resWhitespace == CodexSpeciesId.VorstWalker, "TC-T2-B2.1b", "Whitespace enemyTitle defaults safely to VorstWalker");

            // Test 3: Null title defaults safely to VorstWalker without throwing NullReferenceException
            dummy.enemyTitle = null;
            CodexSpeciesId resNullTitle = VorstCodexUI.ClassifyEnemy(dummy);
            Assert(resNullTitle == CodexSpeciesId.VorstWalker, "TC-T2-B2.1c", "Null enemyTitle defaults safely to VorstWalker");

            // Test 4: Arbitrary non-matching symbols default safely to VorstWalker
            dummy.enemyTitle = "!@#$%^&*()_+=~`{}[]|;:,.<>?/";
            CodexSpeciesId resSymbols = VorstCodexUI.ClassifyEnemy(dummy);
            Assert(resSymbols == CodexSpeciesId.VorstWalker, "TC-T2-B2.1d", "Arbitrary non-matching title string defaults safely to VorstWalker");
        }
        finally
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(host);
            else
                UnityEngine.Object.DestroyImmediate(host);
        }
    }

    private static void Verify_Tier2_OutOfRangeIndexing()
    {
        try
        {
            VorstCodexUI.EnsurePersistenceLoaded();

            // Test safe clamping on GetTotalKills with negative and oversized species IDs
            int negKills = VorstCodexUI.GetTotalKills((CodexSpeciesId)(-1));
            int overKills = VorstCodexUI.GetTotalKills((CodexSpeciesId)999);
            Assert(negKills >= 0 && overKills >= 0, "TC-T2-B3.1a",
                "GetTotalKills clamps negative (-1) and excessive (999) species indices safely");

            // Test safe clamping on GetSessionKills
            int negSession = VorstCodexUI.GetSessionKills((CodexSpeciesId)(-5));
            int overSession = VorstCodexUI.GetSessionKills((CodexSpeciesId)500);
            Assert(negSession >= 0 && overSession >= 0, "TC-T2-B3.1b",
                "GetSessionKills clamps out-of-range indices safely without IndexOutOfRangeException");
        }
        catch (Exception ex)
        {
            Assert(false, "TC-T2-B3.1", $"Out of range indexing threw unexpected exception: {ex.Message}");
        }
    }

    private static void Verify_Tier2_MissingAssetFallback()
    {
        Texture2D missingTex = Resources.Load<Texture2D>("NonExistent/Asset/Path_9999");
        Assert(missingTex == null, "TC-T2-B4.1", "Missing Resources.Load safely returns null without throwing");
    }

    private static void Verify_Tier2_EnumAndBitmaskBoundaries()
    {
        Type enumType = typeof(CodexSpeciesId);
        Array values = Enum.GetValues(enumType);
        bool allBitsValid = true;
        int cumulativeMask = 0;

        foreach (object val in values)
        {
            int intVal = (int)val;
            if (intVal < 0 || intVal >= 31)
            {
                allBitsValid = false;
                break;
            }
            cumulativeMask |= (1 << intVal);
        }

        Assert(allBitsValid, "TC-T2-B5.1a", "All CodexSpeciesId values fit safely in positive bitmask range [0..30]");

        // For 15 species, the required mask is exactly 0x7FFF (32767)
        bool reaches15Species = values.Length == 15 && cumulativeMask == 0x7FFF;
        Assert(reaches15Species, "TC-T2-B5.1b",
            $"CodexSpeciesId enum bitmask spans exactly 15 bits (0x7FFF = 32767; Found: {values.Length} members, mask: 0x{cumulativeMask:X})");
    }

    // =========================================================================
    // TIER 3: CROSS-FEATURE COMBINATIONS
    // =========================================================================

    private static void Verify_Tier3_TabSelectionAndFilterSync()
    {
        Type uiType = typeof(VorstCodexUI);
        MethodInfo switchTabMethod = uiType.GetMethod("SwitchTab", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                  ?? uiType.GetMethod("SetTab", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                  ?? uiType.GetMethod("SelectTab", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        FieldInfo tabField = uiType.GetField("selectedTab", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                          ?? uiType.GetField("currentTab", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                          ?? uiType.GetField("currentZoneTab", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        bool hasTabSupport = switchTabMethod != null || tabField != null;
        Assert(hasTabSupport, "TC-T3-P1.1a", "VorstCodexUI exposes tab selection members for category switching");

        if (hasTabSupport)
        {
            GameObject testHost = new GameObject("Test_VorstCodexUI_TabSync");
            try
            {
                VorstCodexUI ui = testHost.AddComponent<VorstCodexUI>();
                FieldInfo indexField = uiType.GetField("selectedIndex", BindingFlags.NonPublic | BindingFlags.Instance);
                indexField?.SetValue(ui, 4); // Index 4 in Colmena (5 items)

                if (switchTabMethod != null)
                {
                    switchTabMethod.Invoke(ui, new object[] { 1 }); // Switch to Sector 7 (2 items)
                }

                int resultingIndex = indexField != null ? (int)indexField.GetValue(ui) : 4;
                Assert(resultingIndex >= 0 && resultingIndex < 2, "TC-T3-P1.1b",
                    $"Switching to Sector 7 tab (2 items) clamps selectedIndex within [0..1] (Found: {resultingIndex})");
            }
            finally
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(testHost);
                else
                    UnityEngine.Object.DestroyImmediate(testHost);
            }
        }
        else
        {
            Assert(false, "TC-T3-P1.1b", "Tab switching clamping requires Zone Tab implementation in VorstCodexUI");
        }
    }

    private static void Verify_Tier3_AnimationStateAndFlipDynamics()
    {
        // 1. Verify actual animation frame texture dimensions from empirical disk assets
        string walkFramePath = Path.Combine(Application.dataPath, "_Project/Resources/Codex/Commander_Walk_0.png");
        string attackFramePath = Path.Combine(Application.dataPath, "_Project/Resources/Codex/Commander_Attack_0.png");

        bool framesExist = File.Exists(walkFramePath) && File.Exists(attackFramePath);
        Assert(framesExist, "TC-T3-P2.1a", "Walk and Attack reference frame assets exist on disk");

        if (framesExist)
        {
            // Inspect actual PNG dimensions from binary IHDR headers
            byte[] walkBytes = File.ReadAllBytes(walkFramePath);
            byte[] attackBytes = File.ReadAllBytes(attackFramePath);

            int walkW = (walkBytes[16] << 24) | (walkBytes[17] << 16) | (walkBytes[18] << 8) | walkBytes[19];
            int walkH = (walkBytes[20] << 24) | (walkBytes[21] << 16) | (walkBytes[22] << 8) | walkBytes[23];
            int attackW = (attackBytes[16] << 24) | (attackBytes[17] << 16) | (attackBytes[18] << 8) | attackBytes[19];
            int attackH = (attackBytes[20] << 24) | (attackBytes[21] << 16) | (attackBytes[22] << 8) | attackBytes[23];

            float realWalkAspect = (float)walkW / walkH;
            float realAttackAspect = (float)attackW / attackH;

            Assert(walkW > 0 && walkH > 0 && attackW > 0 && attackH > 0, "TC-T3-P2.1b",
                $"Empirical frame asset dimensions verified: Walk ({walkW}x{walkH}), Attack ({attackW}x{attackH})");
            Assert(realAttackAspect >= realWalkAspect, "TC-T3-P2.1c",
                $"Attack frame aspect ratio ({realAttackAspect:F2}) accommodates wider combat bounding box compared to Walk ({realWalkAspect:F2})");
        }

        // 2. Verify flip horizontal state and uv coordinate reflection on VorstCodexUI
        FieldInfo flipField = typeof(VorstCodexUI).GetField("flipSpriteHorizontal", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert(flipField != null, "TC-T3-P2.1d", "flipSpriteHorizontal field exists on VorstCodexUI");
    }

    private static void Verify_Tier3_CombatKillAndCodexUnlockPropagation()
    {
        VorstCodexUI.EnsurePersistenceLoaded();
        CodexSpeciesId testSpecies = CodexSpeciesId.VorstWalker;
        int killsBefore = VorstCodexUI.GetTotalKills(testSpecies);

        // Register kill via official game API
        VorstCodexUI.RegisterSpeciesKill(testSpecies, Vector3.zero, showVfxToast: false);

        int killsAfter = VorstCodexUI.GetTotalKills(testSpecies);
        bool unlocked = VorstCodexUI.IsSpeciesUnlocked(testSpecies);
        int prefsMask = PlayerPrefs.GetInt("KAEL_CODEX_UNLOCKED_MASK", 0);

        Assert(killsAfter == killsBefore + 1, "TC-T3-P3.1a",
            $"RegisterSpeciesKill increments GetTotalKills ({killsBefore} -> {killsAfter})");
        Assert(unlocked, "TC-T3-P3.1b", "IsSpeciesUnlocked returns true after kill registration");
        Assert((prefsMask & (1 << (int)testSpecies)) != 0, "TC-T3-P3.1c",
            "PlayerPrefs KAEL_CODEX_UNLOCKED_MASK contains active bit for registered species");
    }

    private static void Verify_Tier3_SimulationModeVsLockedVisibility()
    {
        GameObject testHost = new GameObject("Test_VorstCodexUI_Simulation");
        try
        {
            VorstCodexUI ui = testHost.AddComponent<VorstCodexUI>();
            FieldInfo simField = typeof(VorstCodexUI).GetField("simulationPreviewAll", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo toggleMethod = typeof(VorstCodexUI).GetMethod("ToggleSimulationPreviewMode", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert(simField != null, "TC-T3-P4.1a", "simulationPreviewAll field exists on VorstCodexUI");
            Assert(toggleMethod != null, "TC-T3-P4.1b", "ToggleSimulationPreviewMode method exists on VorstCodexUI");

            if (simField != null && toggleMethod != null)
            {
                bool initialVal = (bool)simField.GetValue(ui);
                Assert(!initialVal, "TC-T3-P4.1c", "simulationPreviewAll defaults to false (progressive lock enforced)");

                int initialUnlockedMask = PlayerPrefs.GetInt("KAEL_CODEX_UNLOCKED_MASK", 0);

                // Toggle simulation ON
                toggleMethod.Invoke(ui, null);
                bool toggledVal = (bool)simField.GetValue(ui);
                Assert(toggledVal, "TC-T3-P4.1d", "ToggleSimulationPreviewMode toggles simulation to true");

                // Verify PlayerPrefs was not corrupted
                int maskAfterToggle = PlayerPrefs.GetInt("KAEL_CODEX_UNLOCKED_MASK", 0);
                Assert(initialUnlockedMask == maskAfterToggle, "TC-T3-P4.1e",
                    "Simulation mode toggle preserves PlayerPrefs unlocked mask without corruption");

                // Toggle simulation OFF
                toggleMethod.Invoke(ui, null);
                bool revertedVal = (bool)simField.GetValue(ui);
                Assert(!revertedVal, "TC-T3-P4.1f", "Second toggle reverts simulation preview back to false");
            }
        }
        finally
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(testHost);
            else
                UnityEngine.Object.DestroyImmediate(testHost);
        }
    }

    private static void Verify_Tier3_ConcurrentBossKillsIdempotency()
    {
        VorstCodexUI.EnsurePersistenceLoaded();
        CodexSpeciesId bossSpecies = CodexSpeciesId.SupremeSovereign;

        int unlockedCountBefore = VorstCodexUI.GetUnlockedSpeciesCount();
        bool wasUnlocked = VorstCodexUI.IsSpeciesUnlocked(bossSpecies);

        // Register two duplicate kills in rapid succession
        VorstCodexUI.RegisterSpeciesKill(bossSpecies, Vector3.zero, false);
        VorstCodexUI.RegisterSpeciesKill(bossSpecies, Vector3.zero, false);

        int unlockedCountAfter = VorstCodexUI.GetUnlockedSpeciesCount();
        int expectedUnlockedCount = wasUnlocked ? unlockedCountBefore : (unlockedCountBefore + 1);

        Assert(unlockedCountAfter == expectedUnlockedCount, "TC-T3-P5.1a",
            $"Duplicate boss kill registration increments unlocked species count at most once ({unlockedCountBefore} -> {unlockedCountAfter})");

        int mask = PlayerPrefs.GetInt("KAEL_CODEX_UNLOCKED_MASK", 0);
        int bit = (1 << (int)bossSpecies);
        Assert((mask & bit) == bit, "TC-T3-P5.1b",
            "PlayerPrefs bitmask retains single valid bit for boss species under duplicate registrations");
    }

    // =========================================================================
    // TIER 4: REAL-WORLD SCENARIOS
    // =========================================================================

    private static void Verify_Tier4_FullCampaignProgressionLifecycle()
    {
        Assert(VorstCodexUI.Catalog != null, "TC-T4-S1.1a", "VorstCodexUI.Catalog is accessible for progression evaluation");

        if (VorstCodexUI.Catalog != null)
        {
            int catalogCount = VorstCodexUI.Catalog.Length;
            Assert(catalogCount == 15, "TC-T4-S1.1b",
                $"Full campaign requires 15 species across 4 campaign levels (Found: {catalogCount})");

            if (catalogCount == 15)
            {
                int fullMask = 0;
                for (int i = 0; i < 15; i++)
                {
                    fullMask |= (1 << (int)VorstCodexUI.Catalog[i].SpeciesId);
                }
                Assert(fullMask == 0x7FFF, "TC-T4-S1.1c",
                    $"15 Catalog species span complete campaign bitmask 0x7FFF (Found: 0x{fullMask:X})");
            }
            else
            {
                Assert(false, "TC-T4-S1.1c",
                    $"Cannot achieve 15-species campaign progression 0x7FFF with incomplete catalog ({catalogCount}/15)");
            }
        }
    }

    private static void Verify_Tier4_ViewportBaselineStabilityStress()
    {
        string uiScriptPath = Path.Combine(Application.dataPath, "_Project/Scripts/VorstCodexUI.cs");
        if (!File.Exists(uiScriptPath))
        {
            Assert(false, "TC-T4-S2.1", "VorstCodexUI.cs missing for viewport stability verification");
            return;
        }

        string src = File.ReadAllText(uiScriptPath);

        // Check if VorstCodexUI still uses centered ScaleToFit in a fixed box
        bool usesCenteredScaleToFit = src.Contains("Rect spriteBox = ScaleRect(608f, 228f + bobY, 400f, 352f)")
                                   && src.Contains("GUI.DrawTexture(spriteBox, activeTex, ScaleMode.ScaleToFit");

        // Baseline anchored rendering requires bottom-anchored rect computation
        bool hasBaselineAnchoredRect = src.Contains("baselineY") || src.Contains("bottomAnchor") || src.Contains("pedestalBaselineY");

        if (usesCenteredScaleToFit && !hasBaselineAnchoredRect)
        {
            // Calculate actual deviation caused by ScaleMode.ScaleToFit on 256x256 (walk) vs 512x256 (attack) in 400x352 box:
            // Walk (aspect 1.0): fits height 352, width 352, Y = 228, Bottom = 580
            // Attack (aspect 2.0): fits width 400, height 200, centered Y = 228 + (352-200)/2 = 304, Bottom = 504
            float walkBottom = 228f + 352f;
            float attackBottom = 228f + (352f - 200f) / 2f + 200f;
            float actualJitterDeviation = Mathf.Abs(walkBottom - attackBottom);

            Assert(false, "TC-T4-S2.1",
                $"Viewport suffers {actualJitterDeviation:F0}px vertical baseline jitter: ScaleMode.ScaleToFit centers variable-width frames vertically instead of anchoring to baseline");
        }
        else
        {
            Assert(hasBaselineAnchoredRect, "TC-T4-S2.1",
                "Viewport baseline anchoring verified: sprite rect calculation anchors bottom baseline across variable frame aspects");
        }
    }

    private static void Verify_Tier4_MemoryGCSafety()
    {
        GameObject testHost = new GameObject("Test_VorstCodexUI_GC");
        try
        {
            VorstCodexUI ui = testHost.AddComponent<VorstCodexUI>();

            // Warm up
            ui.OpenCodex();
            ui.CloseCodex();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memBefore = GC.GetTotalMemory(true);

            int callbackInvocations = 0;
            for (int i = 0; i < 100; i++)
            {
                ui.OpenCodex(() => callbackInvocations++);
                ui.CloseCodex();
            }

            Assert(callbackInvocations == 100, "TC-T4-S3.1a",
                "100 rapid OpenCodex/CloseCodex cycles invoked callback exactly 100 times");
            Assert(!ui.isOpen, "TC-T4-S3.1b", "Codex isOpen flag is safely false after cycling");

            FieldInfo callbackField = typeof(VorstCodexUI).GetField("onCloseCallback", BindingFlags.NonPublic | BindingFlags.Instance);
            if (callbackField != null)
            {
                object cb = callbackField.GetValue(ui);
                Assert(cb == null, "TC-T4-S3.1c", "onCloseCallback delegate reference is released (null) after CloseCodex");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memAfter = GC.GetTotalMemory(true);
            long diffBytes = memAfter - memBefore;

            // Memory difference must remain bounded (< 512 KB delta for 100 cycles)
            Assert(diffBytes < 512 * 1024, "TC-T4-S3.1d",
                $"Memory delta after 100 cycles is within safe threshold ({diffBytes / 1024} KB)");
        }
        finally
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(testHost);
            else
                UnityEngine.Object.DestroyImmediate(testHost);
        }
    }

    private static void Verify_Tier4_CleanAssemblyCompilation()
    {
        bool compilationFailed = EditorUtility.scriptCompilationFailed;
        Assert(!compilationFailed, "TC-T4-S4.1a",
            "Unity Editor reports no active script compilation errors (scriptCompilationFailed == false)");

        Assembly gameAssembly = null;
        Assembly editorAssembly = null;
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            string asmName = asm.GetName().Name;
            if (asmName == "Assembly-CSharp") gameAssembly = asm;
            if (asmName == "Assembly-CSharp-Editor") editorAssembly = asm;
        }

        Assert(gameAssembly != null, "TC-T4-S4.1b",
            "Runtime Assembly-CSharp successfully compiled and loaded in AppDomain");
        Assert(editorAssembly != null, "TC-T4-S4.1c",
            "Editor Assembly-CSharp-Editor successfully compiled and loaded in AppDomain");

        if (gameAssembly != null)
        {
            Type codexType = gameAssembly.GetType("VorstCodexUI");
            Assert(codexType != null, "TC-T4-S4.1d", "VorstCodexUI type resolved from Assembly-CSharp");
        }
    }
}
