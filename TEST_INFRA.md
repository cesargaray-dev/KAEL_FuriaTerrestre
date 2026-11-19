# TEST_INFRA — Vorst Codex Automated E2E Testing Infrastructure

## Overview
This document defines the automated, opaque-box end-to-end testing infrastructure for the interactive Vorst Codex system in **KAEL: Furia Terrestre**.
The test suite validates data completeness, biomechanical schema integrity, tactical icon availability, animation frame inventories, baseline-anchored holographic rendering contracts, dynamic memory allocation, enemy kill classification, and campaign progression across all 15 campaign enemies.

The test infrastructure is designed under **Development Integrity Mode** (zero facades, zero self-certifying tests, zero hardcoded passes). It acts as an authoritative, adversarial verification gate for Milestones M1, M2, and M3.

---

## 1. Dual-Layer Testing Architecture

To guarantee 100% reliable unattended CLI automation without colliding with open Unity Editor instances (which lock `Temp\UnityLockfile` and cause `Unity.exe -batchmode` to abort), the infrastructure implements a **Dual-Layer Architecture**:

```
+---------------------------------------------------------------------------------------------------+
|                                LAYER 1: AUTOMATED CLI MASTER RUNNER                               |
|                             `Tests/Run-CodexE2ETests.ps1`                                         |
|  Invocation: `powershell -ExecutionPolicy Bypass -File .\run_codex_tests.ps1`                     |
|  Execution Duration: ~0.5s (-SkipBuild) to ~5.0s (Full dotnet build)                              |
|  Exit Code Contract: 0 = 100% PASS | 1 = FAIL                                                     |
|  Dependencies: .NET SDK (dotnet build), PowerShell Core/Desktop. ZERO UnityLockfile collisions.   |
|  Total Automated Assertions: 40 discrete checks across 5 suites                                   |
+---------------------------------------------------------------------------------------------------+
       |                                                                             |
       v                                                                             v
+-----------------------------+                               +-------------------------------------+
|  Suite 1: C# Build (4)      |                               |  Suite 2: Database Schema (11)      |
|  `dotnet build Assembly-...`|                               |  15 species, 17 schema fields, hex  |
+-----------------------------+                               +-------------------------------------+
       |                                                                             |
       v                                                                             v
+-----------------------------+                               +-------------------------------------+
|  Suite 3: Asset Pipeline (5)|                               |  Suite 4: Code Contracts (9)        |
|  15 Icons (PNG magic bytes),|                               |  CodexSpeciesId (15), dynamic arrays|
|  334+ action frames (L3/L4) |                               |  VisualTypePublic, baseline anchor  |
+-----------------------------+                               +-------------------------------------+
                                              |
                                              v
                             +-----------------------------------+
                             |  Suite 5: Business Logic (11)     |
                             |  Zone tabs, bounds clamping,      |
                             |  bitmask math, progression states |
                             +-----------------------------------+
                                              |
                                              v
                             +-----------------------------------+
                             |  Artifacts Generated:             |
                             |  - Console ANSI log               |
                             |  - `TestResults_CodexE2E.log`     |
                             +-----------------------------------+

+---------------------------------------------------------------------------------------------------+
|                             LAYER 2: IN-EDITOR COMPANION SUITE                                    |
|                   `Assets/_Project/Scripts/Editor/VorstCodexVerificationSuite.cs`                 |
|  Invocation: Unity Menu `KAEL > Verify Vorst Codex Integrity` or batchmode `-executeMethod`       |
|  Total Automated Assertions: 38 discrete checks across Tiers 1–4                                  |
|  Artifact: `Codex_Verification_Report.txt`                                                        |
+---------------------------------------------------------------------------------------------------+
```

### Architectural Delineation: Layer 1 vs. Layer 2
- **Layer 1 (CLI Master Runner)**: Operates strictly outside Unity. Inspects serialized assets on disk (JSON parsing, binary PNG headers, directory recursion), invokes the `.NET` compiler against `.csproj` targets, and performs Roslyn-grade AST / regex contract verification on C# source scripts. This is the **primary gate** for CI/CD and pre-commit checks.
- **Layer 2 (In-Editor Companion Suite)**: Operates inside the Unity Editor domain. Uses C# Reflection to inspect private static fields (`_totalKills`, `speciesIcons`), checks `UnityEngine` component contracts (`Resources.Load`, procedural texture allocations, `OnDestroy` cleanup), and outputs an editor verification report.

---

## 2. Test Execution Commands & Operator Guide

### 2.1 Primary Automated CLI Runner (Unattended CI/CD)
Full build and test run (compiles C# assemblies, then executes all suites):
```powershell
powershell -ExecutionPolicy Bypass -File .\run_codex_tests.ps1
```
Fast mode (skips `dotnet build`, executes asset, schema, code contract, and logic suites in < 0.5s):
```powershell
powershell -ExecutionPolicy Bypass -File .\run_codex_tests.ps1 -SkipBuild
```
Direct runner script invocation:
```powershell
powershell -ExecutionPolicy Bypass -File .\Tests\Run-CodexE2ETests.ps1 -SkipBuild -LogPath "TestResults_CodexE2E.log"
```

### 2.2 Command Prompt (`cmd.exe`) Wrapper
```cmd
run_tests.bat
```
With skip build:
```cmd
run_tests.bat -SkipBuild
```
- **Exit Code Contract**:
  - `0`: All assertions passed (100% success).
  - `1`: One or more assertions failed. Exits immediately with error code for CI/CD pipeline halting.

### 2.3 C# Assembly Compilation Verification
```powershell
dotnet build Assembly-CSharp.csproj -c Release
dotnet build Assembly-CSharp-Editor.csproj -c Release
```
- **Success Criteria**: Exit code `0`, `0 Warning(s)`, `0 Error(s)`.

### 2.4 Unity In-Editor Companion Suite
Inside the Unity Editor:
1. Open the project in Unity (`Unity 6000.6.0f1`).
2. Navigate to menu: **KAEL > Verify Vorst Codex Integrity** (or **KAEL Tools > Verificar Códice Vorst (E2E)**).
3. Inspect results in Console and generated report: `Codex_Verification_Report.txt`.

### 2.5 Optional Headless Unity Batch Mode (When Unity is Closed)
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" `
  -batchmode -nographics -projectPath . `
  -executeMethod VorstCodexVerificationSuite.RunVerificationSuiteCLI `
  -logFile CodexTestRun.log -quit
```

---

## 3. Layer 1: Headless CLI Master Runner — Full Assertion Inventory (40 Assertions)

The CLI master runner (`Tests/Run-CodexE2ETests.ps1`) evaluates **40 discrete automated assertions** divided across 5 suites:

### Suite 1: C# Build Integrity & Assembly Compilation (4 Assertions)
| Test ID | Target Component | Verification Logic | Expected Outcome | Pre-M2 Status |
|---|---|---|---|---|
| `TC-S1-BUILD-01` | Environment | `dotnet --version` execution | dotnet CLI available in system path | **PASS** |
| `TC-S1-BUILD-02` | `Assembly-CSharp.csproj` | `dotnet build -c Release` | Compiles with 0 errors | **PASS** |
| `TC-S1-BUILD-03` | `Assembly-CSharp-Editor.csproj` | `dotnet build -c Release` | Compiles with 0 errors | **PASS** |
| `TC-S1-BUILD-04` | Compiled Binaries | Path check in `Temp/bin/Debug/` or `Library/` | Binary `.dll` artifact exists | **PASS** |

### Suite 2: Database Schema & Data Completeness (11 Assertions)
| Test ID | Target Component | Verification Logic | Expected Outcome | Pre-M2 Status |
|---|---|---|---|---|
| `TC-T1-F1.1a` | `VorstCodexDatabase.json` | File system check in `Resources/Codex/` | File exists on disk | **PASS** |
| `TC-T1-F1.1b` | JSON Parser | `ConvertFrom-Json` UTF-8 stream parse | Parses into valid JSON object | **PASS** |
| `TC-T1-F1.1c` | Database Header | Regex `^2\.[0-9]+\.[0-9]+-NEXO$` | Schema version matches specification | **PASS** |
| `TC-T1-F1.1d` | Military Title | Substring `"CÓDICE XENOBIOLÓGICO VORST"` | Authoritative archive designation present | **PASS** |
| `TC-T1-F1.2a` | Entries Array | Array count comparison | Exactly 15 species entries (found: 6) | **FAIL (Pre-M2)** |
| `TC-T1-F1.2b` | Specimen IDs | Unique set comparison against count | Zero duplicate specimen keys | **PASS** |
| `TC-T1-F1.2c` | Canonical Registry | Set difference against 15 canonical IDs | All 15 canonical IDs present (missing 9) | **FAIL (Pre-M2)** |
| `TC-T1-F1.3a` | Specimen Metadata | String check: `name, origin, armor, weak` | All fields non-empty across all entries | **PASS** |
| `TC-T1-F1.3b` | Threat Rating | Integer range check `[1..6]` | All entries have valid threat level | **FAIL (Pre-M2)** |
| `TC-T1-F1.3c` | Military Diary | String length verification | `diaryFragment.Length >= 40` characters | **PASS** |
| `TC-T1-F1.3d` | Theme Color Hex | Regex `^#[0-9A-Fa-f]{6}$` | Valid 7-character RGB hex code | **PASS** |

### Suite 3: Asset Pipeline & Tactical Icons (5 Assertions)
| Test ID | Target Component | Verification Logic | Expected Outcome | Pre-M2 Status |
|---|---|---|---|---|
| `TC-T1-F7.1` | Tactical Icons | Disk existence of 15 `Icon_Codex_*.png` | All 15 icons present in Resources | **PASS** |
| `TC-T1-F7.2` | PNG Magic Header | Binary byte stream check: `89 50 4E 47` | Valid PNG magic signature on all 15 icons | **PASS** |
| `TC-T1-F7.3` | Icon Dimensions | Big Endian IHDR header parsing | Width and Height $\ge 200\text{px}$ | **PASS** |
| `TC-T1-F8.1` | Frame Inventory | Regex enumeration in Resources folders | $\ge 334$ action animation frames | **PASS** |
| `TC-T1-F8.2` | Level 3 & 4 Sequences | 8-frame check across Walk/Attack/Die | 240 uniform frames for 10 L3/L4 species | **PASS** |

### Suite 4: Code Contracts & Architectural Invariants (9 Assertions)
| Test ID | Target Component | Verification Logic | Expected Outcome | Pre-M2 Status |
|---|---|---|---|---|
| `TC-CODE-01` | `VorstCodexUI.cs` | File system check in `Assets/_Project/Scripts/` | File exists on disk | **PASS** |
| `TC-T1-F2.1` | `CodexSpeciesId` Enum | Regex parsing of enum body | Exactly 15 enum members (found: 6) | **FAIL (Pre-M2)** |
| `TC-T1-F2.2` | Enum Members | Presence of `BioStalker, VortexOmega, etc.` | Canonical Level 3 & 4 IDs defined | **FAIL (Pre-M2)** |
| `TC-T1-F3.1` | Kill Arrays | Negative regex for `new int[6]` | Fixed [6] allocation eliminated | **FAIL (Pre-M2)** |
| `TC-T1-F3.2` | Icon Cache | Negative regex for `new Texture2D[6]` | Fixed [6] allocation eliminated | **FAIL (Pre-M2)** |
| `TC-T1-F4.1` | `ClassifyEnemy` | Regex for `VisualTypePublic` and L3/L4 names | Distinct Level 3 & 4 classification | **FAIL (Pre-M2)** |
| `TC-T1-F6.1` | Viewport Anchoring | AST/Regex on `spriteBox` render rect calculation | Sprite renders anchored to bottom baseline | **FAIL (Pre-M2)** |
| `TC-T1-F10.1` | MainMenu Codex Button | Regex check on button label text | Dynamic count, no hardcoded `"/6"` | **FAIL (Pre-M2)** |
| `TC-T1-F10.2` | PlayerStats Pause UI | Regex check on button label text | Dynamic count, no hardcoded `"/6"` | **FAIL (Pre-M2)** |

### Suite 5: Business Logic, Zone Tabs & State Invariants (11 Assertions)
| Test ID | Target Component | Verification Logic | Expected Outcome | Pre-M2 Status |
|---|---|---|---|---|
| `TC-T1-F5.1` | Zone Tab Definitions | Regex/AST on `VorstCodexUI.cs` for 5 tabs | 5 Zone tabs declared in UI code | **FAIL (Pre-M2)** |
| `TC-T1-F5.2` | Tab Partitioning | Regex/AST on `VorstCodexUI.cs` filter logic | Level filtering logic maps $15 = 2+3+5+5$ | **FAIL (Pre-M2)** |
| `TC-T1-F5.3` | Modal Panel Layout | Height math ($5 \times 130\text{px} = 650\text{px} \le 846\text{px}$) | List fits inside modal without screen overflow | **FAIL (Pre-M2)** |
| `TC-T2-B3.1` | Index Boundary Clamping | AST check in `VorstCodexUI.cs` for index clamping | Selection index clamped safely to `[0..Catalog.Length-1]` | **PASS** |
| `TC-T3-P1.1` | Tab Switch Clamping | AST check in `VorstCodexUI.cs` for tab change clamp | Selection clamped to target tab item count | **FAIL (Pre-M2)** |
| `TC-T2-B5.1` | Unlock Bitmask Space | Math check: `1 << 14` and `(1 << 15) - 1` | Full mask ($0x7FFF = 32,767$) fits in signed 32-bit int | **PASS** |
| `TC-T1-F6.4` | Viewport Bottom Anchor | Math proof: $(Y_{base} - H) + H \equiv Y_{base}$ | Dynamic aspect ratio anchored to baseline | **FAIL (Pre-M2)** |
| `TC-T4-S1.1` | Progression Level 1 | Query L1 species in Enum and Catalog | Exactly 2 species registered in Sector 7 | **PASS** |
| `TC-T4-S1.2` | Progression Level 2 | Query L1+L2 species in Enum and Catalog | Cumulative 5 species registered in Base Vorst | **PASS** |
| `TC-T4-S1.3` | Progression Level 3 | Query L1+L2+L3 species in Enum and Catalog | Cumulative 10 species registered in Colmena | **FAIL (Pre-M2)** |
| `TC-T4-S1.4` | Progression Level 4 | Query all 15 species in Enum and Catalog | 100% campaign completion (15/15 species) | **FAIL (Pre-M2)** |

**Summary Layer 1 Assertions**: 40 Evaluated (22 Passed, 18 Failed Pre-M2). Overall Exit Code: `1`.

---

## 4. Layer 2: In-Editor Companion Suite — Full Assertion Inventory (38 Assertions)

Located in `Assets/_Project/Scripts/Editor/VorstCodexVerificationSuite.cs`. Evaluates **38 discrete assertions** across 4 tiers:

### Tier 1: Feature Coverage (24 Assertions)
- `TC-T1-F1.1a`: `VorstCodexDatabase.json` file existence on disk via `Application.dataPath`.
- `TC-T1-F1.1b`: Database text content is non-empty.
- `TC-T1-F1.1c`: JSON contains `"version":` key.
- `TC-T1-F1.1d`: JSON contains `"entries":` array key.
- `TC-T1-F1.2`: JSON entries text contains Level 3 and Level 4 species identifiers.
- `TC-T1-F2.1`: `CodexSpeciesId` enum has exactly 15 members via `Enum.GetNames()`.
- `TC-T1-F2.2a`–`d`: `Enum.IsDefined()` returns true for `VorstWalker`, `BioStalker`, `VortexOmega`, `SupremeSovereign`.
- `TC-T1-F3.1a`: `VorstCodexUI.Catalog` is not null.
- `TC-T1-F3.1b`: `VorstCodexUI.Catalog.Length == 15`.
- `TC-T1-F3.2a`: Reflection on private field `_totalKills` confirms dynamic array length $\ge 15$.
- `TC-T1-F3.2b`: `VorstCodexUI.GetTotalKills((CodexSpeciesId)14)` executes without throwing.
- `TC-T1-F4.1`: `VorstCodexUI.ClassifyEnemy(null)` returns default species `VorstWalker (0)` safely.
- `TC-T1-F4.2`: Source inspection confirms `ClassifyEnemy` discriminates using `VisualTypePublic`.
- `TC-T1-F5.1a`: `VorstCodexUI.cs` file exists on disk for tab inspection.
- `TC-T1-F5.1b`: Source inspection verifies 5 Zone tab definitions (Todos, Sector 7, Base Vorst, Colmena, Trono).
- `TC-T1-F5.2`: Source inspection verifies tab-based species filtering and layout pagination logic.
- `TC-T1-F6.1a`: `VorstCodexUI.cs` file exists for viewport inspection.
- `TC-T1-F6.1b`: Source inspection verifies bottom-anchored baseline calculation eliminating fixed centered `ScaleToFit`.
- `TC-T1-F7.1`: File system audit verifies all 15 tactical icons in `Resources/Codex/`.
- `TC-T1-F8.1`: File system audit verifies $\ge 334$ animation action frames across Resources folders.
- `TC-T1-F9.1`: Source inspection verifies null-texture fallback guard (`activeTex != null` or null-coalescing).
- `TC-T1-F10.1`: MainMenu controller button string format uses dynamic total count without hardcoded `/6`.
- `TC-T1-F10.2`: PlayerStats controller button string format uses dynamic total count without hardcoded `/6`.

### Tier 2: Boundary & Corner Cases (5 Assertions)
- `TC-T2-B1.1`: `ClassifyEnemy(null)` executes without `NullReferenceException`.
- `TC-T2-B2.1a–d`: Malformed string handling: Empty, whitespace, null, and symbol titles default safely to VorstWalker without exceptions.
- `TC-T2-B3.1a–b`: Selection boundary clamping: `GetTotalKills` and `GetSessionKills` clamp negative (-1) and excessive (999) species indices safely.
- `TC-T2-B4.1`: Missing asset query `Resources.Load<Texture2D>("NonExistent/Path")` returns `null` safely.
- `TC-T2-B5.1a–b`: Enum reflection verifies all `CodexSpeciesId` members fit in positive range [0..30] and cumulative bitmask spans exactly 15 bits (`0x7FFF = 32,767`).

### Tier 3: Cross-Feature Combinations (5 Assertions)
- `TC-T3-P1.1a–b`: Tab switch selection clamping: verifies tab members exist on `VorstCodexUI` and switching tabs clamps selection within target item count.
- `TC-T3-P2.1a–d`: Empirical frame inspection: binary IHDR headers verify Walk and Attack frame dimensions and aspect ratios; reflection confirms `flipSpriteHorizontal` field.
- `TC-T3-P3.1a–c`: Kill and unlock propagation: `RegisterSpeciesKill` increments kill counters, unlocks species, and updates PlayerPrefs bitmask.
- `TC-T3-P4.1a–f`: Simulation preview mode contract: reflection validates `simulationPreviewAll` defaults to false, toggles state correctly, preserves PlayerPrefs, and reverts safely.
- `TC-T3-P5.1a–b`: Idempotent kill registration: duplicate kill events on boss species increment unlocked species count at most once and retain single valid bit.

### Tier 4: Real-World Scenarios (4 Assertions)
- `TC-T4-S1.1a–c`: Full campaign progression lifecycle: verifies `Catalog` contains 15 species spanning complete campaign bitmask `0x7FFF`.
- `TC-T4-S2.1`: Viewport baseline stability stress: validates bottom-anchored rect computation across variable frame aspects without centered `ScaleToFit` vertical jitter.
- `TC-T4-S3.1a–d`: Memory GC and lifecycle safety: executes 100 rapid `OpenCodex()` / `CloseCodex()` cycles, verifying callback invocation, `onCloseCallback` delegate release, and bounded memory delta (< 512 KB).
- `TC-T4-S4.1a–d`: Assembly compilation and runtime integrity: inspects `EditorUtility.scriptCompilationFailed`, verifies `Assembly-CSharp` and `Assembly-CSharp-Editor` in `AppDomain`, and resolves `VorstCodexUI` type.

**Summary Layer 2 Assertions**: 38 Evaluated.

---

## 5. Requirement Traceability Matrix

The table below maps the high-level project requirements from `PROJECT.md` and `ORIGINAL_REQUEST.md` to the discrete assertions in Layer 1 (CLI) and Layer 2 (In-Editor):

| Feature # | Feature Name | Target Milestone | Layer 1 CLI Assertion(s) | Layer 2 Editor Assertion(s) |
|---|---|---|---|---|
| **F1** | 15-Specimen JSON Database | M2 | `TC-T1-F1.1a–d`, `F1.2a–c`, `F1.3a–d` (11 checks) | `TC-T1-F1.1a–d`, `TC-T1-F1.2` (5 checks) |
| **F2** | `CodexSpeciesId` Enum Expansion | M2 | `TC-T1-F2.1`, `TC-T1-F2.2` (2 checks) | `TC-T1-F2.1`, `TC-T1-F2.2a–d` (5 checks) |
| **F3** | Dynamic Array Sizing | M2 | `TC-T1-F3.1`, `TC-T1-F3.2` (2 checks) | `TC-T1-F3.1a–b`, `TC-T1-F3.2a–b` (4 checks) |
| **F4** | Accurate Enemy Classification | M2 | `TC-T1-F4.1` (1 check) | `TC-T1-F4.1`, `TC-T1-F4.2`, `TC-T2-B1.1` (3 checks) |
| **F5** | Level/Zone Tabs & Navigation | M2 | `TC-T1-F5.1`, `F5.2`, `F5.3` (3 checks) | `TC-T1-F5.1a–b`, `TC-T1-F5.2`, `TC-T3-P1.1a–b` (5 checks) |
| **F6** | Baseline-Anchored Viewport | M2 | `TC-T1-F6.1`, `TC-T1-F6.4` (2 checks) | `TC-T1-F6.1a–b`, `TC-T3-P2.1a–d`, `TC-T4-S2.1` (7 checks) |
| **F7** | Tactical Icons Verification | M1 | `TC-T1-F7.1`, `F7.2`, `F7.3` (3 checks) | `TC-T1-F7.1` (1 check) |
| **F8** | Animation Cadence & Inventory | M1 | `TC-T1-F8.1`, `TC-T1-F8.2` (2 checks) | `TC-T1-F8.1` (1 check) |
| **F9** | Null-Safe Texture Fallback | M2 | `TC-CODE-01` (prereq) | `TC-T1-F9.1`, `TC-T2-B4.1` (2 checks) |
| **F10** | Dynamic Header Counters | M2 | `TC-T1-F10.1`, `TC-T1-F10.2` (2 checks) | `TC-T1-F10.1`, `TC-T1-F10.2` (2 checks) |
| **F11** | Build & Progression Lifecycle | E2E | `TC-S1-BUILD-01–04`, `TC-T2-B5.1`, `TC-T4-S1.1–S1.4` (9 checks) | `TC-T2-B5.1a–b`, `TC-T3-P3.1a–c`, `TC-T3-P4.1a–f`, `TC-T3-P5.1a–b`, `TC-T4-S1.1a–c`, `TC-T4-S3.1a–d`, `TC-T4-S4.1a–d` (22 checks) |

---

## 6. Honest Scope Declarations & Anti-Circumvention Policy

Under the Project Integrity Charter and Development Mode rules, this test infrastructure enforces:

1. **Zero Facade Testing**:
   - Every assertion executes real verification logic (binary file reading, JSON object conversion, Roslyn-grade C# AST inspection, or reflection).
   - No method is permitted to call `Assert(true)` without evaluating genuine system properties.
2. **Honest Simulation Scope**:
   - **Memory & GC Safety (`TC-T4-S3`)**: Evaluates 100 rapid `OpenCodex()` / `CloseCodex()` cycles, verifies that `onCloseCallback` delegate references are properly nullified, and asserts that total managed memory growth remains bounded (< 512 KB delta). Dynamic runtime heap profiling is conducted during interactive playtesting.
   - **Simulation Preview Mode (`TC-T3-P4`)**: Verifies reflection contracts on `simulationPreviewAll`, validates progressive lock enforcement by default, validates state toggles, and proves PlayerPrefs integrity is preserved.
   - **Baseline Bottom Anchoring (`TC-T1-F6`)**: Proves the mathematical invariance of the bottom baseline calculation and verifies that `VorstCodexUI.cs` computes the sprite render box using the fixed bottom anchor formula rather than `ScaleMode.ScaleToFit`.
3. **Pre-M2 Failing Gate**:
   - The test runner must authentically **FAIL** on missing features prior to Milestone M2 implementation. Any test that artificially passes green by testing its own internal mock variables is classified as an **INTEGRITY VIOLATION** and rejected.

---

## 7. Artifact Outputs & Reporting

Execution of the test harness generates:
1. **Console Stream**: Real-time ANSI-colored assertion passes and failures.
2. **`TestResults_CodexE2E.log`**: Timestamped execution trace including failure diagnostics and exit code.
3. **`Codex_Verification_Report.txt`**: Unity in-editor companion report generated when executed from the Unity menu.
4. **`TEST_READY.md`**: Official milestone certification document published once all 40 CLI assertions achieve 100% PASS.
