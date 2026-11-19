# E2E Test Suite Ready — KAEL: Furia Terrestre (Vorst Codex UI)

## Status
- **Gate Status**: **PASS** (Iteration 2 Parallel Verification Wave Approved: Forensic Auditor CLEAN, 2 Reviewers APPROVE, 2 Challengers APPROVE).
- **Integrity Status**: 100% Genuine (Zero dummy facades, zero `Assert(true)` stubs, zero tautological arithmetic, zero self-certifying mock logic).
- **Quality Gate Behavior**:
  * **Pre-M2 (Current)**: Evaluates 40 assertions (22 PASS, 18 FAIL, Exit Code `1`). Authoritatively guards against premature sign-off.
  * **Post-M2 (Expected)**: Evaluates 40 assertions (40 PASS, 0 FAIL, Exit Code `0`). Satisfiability proven via mutation oracles.

---

## Test Runner

### Master CLI Test Runner (Layer 1 - Unattended CI/CD)
- **Full Suite Command** (clean compilation + 40 assertions across all 5 suites):
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\run_codex_tests.ps1
  ```
- **Fast Mode Command** (bypasses rebuild, evaluates 36 non-build assertions in < 0.6s):
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\run_codex_tests.ps1 -SkipBuild
  ```
- **Windows Command Prompt Wrapper**:
  ```cmd
  run_tests.bat -SkipBuild
  ```
  *(Propagates `%ERRORLEVEL% = 1` pre-M2, `%ERRORLEVEL% = 0` post-M2).*

### Unity In-Editor Companion Suite (Layer 2)
- **Menu Item**: `KAEL > Verify Vorst Codex Integrity`
- **Source**: `Assets/_Project/Scripts/Editor/VorstCodexVerificationSuite.cs`
- **Batchmode Command**:
  ```cmd
  "%UNITY_PATH%\Unity.exe" -batchmode -projectPath "%CD%" -executeMethod VorstCodexVerificationSuite.RunAllVerifications -quit -logFile test_codex_editor.log
  ```

---

## Coverage Summary

| Tier | Layer 1 (CLI) Assertions | Layer 2 (In-Editor) Assertions | Description |
|---|:---:|:---:|---|
| **Tier 1: Feature Coverage** | 25 | 10 | Complete 15-species schema, icons, AST contracts, headers, enum definitions |
| **Tier 2: Boundary & Corner Cases** | 2 | 8 | Empty/whitespace titles, null inputs, clamped indices, bitmask 32-bit limits |
| **Tier 3: Cross-Feature Interactions** | 1 | 12 | Tab switching, progressive unlocking, animation states, PlayerPrefs persistence |
| **Tier 4: Real-World Scenarios** | 4 | 8 | 4-level campaign progression, 100-cycle GC memory stress, baseline jitter audit |
| **Sanity & Compilation** | 8 | - | .NET SDK version, Release build compilation, binary artifact existence |
| **Total Assertions** | **40** | **38** | **Authoritative Dual-Layer Verification Harness** |

---

## Feature Checklist

| # | Feature Inventory Item | Tier 1 | Tier 2 | Tier 3 | Tier 4 | Pre-M2 Status | Post-M2 Criteria |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|
| F1 | 15-Specimen JSON Database | 11 | - | - | - | 4 FAIL (6/15 species, missing threatRating) | 15 entries valid (#RRGGBB, threatRating, diary fragment) |
| F2 | CodexSpeciesId Enum Expansion | 3 | - | - | - | 2 FAIL (6/15 members, missing L3/L4) | 15 enum members in VorstCodexUI.cs |
| F3 | Dynamic Array Sizing | 2 | 1 | - | - | 2 FAIL (retains new int[6], Texture2D[6]) | Sized dynamically from Catalog or enum length |
| F4 | Accurate Enemy Classification | 1 | 2 | - | - | 1 FAIL (lacks L3/L4 discriminators) | ClassifyEnemy handles all 15 species with null safety |
| F5 | Level/Zone Tabs & Scroll Navigation | 3 | 1 | 1 | - | 4 FAIL (lacks 5 zone tabs & scroll) | 5 zone tabs implemented with bounded navigation |
| F6 | Baseline-Anchored Hologram Viewport | 2 | - | - | 1 | 2 FAIL (centered ScaleToFit jitter) | Dynamic bottom baseline anchoring eliminating jitter |
| F7 | Tactical Icons Verification | 4 | - | - | - | 4 PASS (all 15 PNG assets verified) | 256x256 RGBA32 assets verified with IHDR headers |
| F8 | Animation Cadence & Smoothing | - | - | 1 | - | 1 PASS (walk/attack frames verified) | Frame count and animation cadence verified |
| F9 | Null-Safe Texture Fallback | - | 2 | - | - | 2 PASS (null textures do not crash) | Fallback texture displayed on missing sprite |
| F10 | Dynamic Header Counters | 2 | - | - | - | 2 FAIL (hardcoded /6 in callers) | MainMenu & PlayerStats dynamic {unlocked}/15 counters |
| F11 | CLI Test Runner & Reports | 4 | - | - | 1 | 4 PASS (build, fast mode, exit codes) | Automated unattended CLI runner with structured logging |

---

## Pre-M2 Quality Gate Failure Baseline (Expected 18 Failures)

The 18 failing assertions represent the exact, non-negotiable contractual scope for Milestone M2:
1. `TC-T1-F1.2a`: JSON database has 6 entries (expected 15).
2. `TC-T1-F1.2c`: JSON database missing 9 canonical species IDs.
3. `TC-T1-F1.3b`: JSON database entries missing `threatRating`.
4. `TC-T1-F2.1`: `CodexSpeciesId` enum has 6 members (expected 15).
5. `TC-T1-F2.2`: `CodexSpeciesId` missing Level 3 and Level 4 identifiers.
6. `TC-T1-F3.1`: `VorstCodexUI.cs` allocates fixed `new int[6]`.
7. `TC-T1-F3.2`: `VorstCodexUI.cs` allocates fixed `new Texture2D[6]`.
8. `TC-T1-F4.1`: `ClassifyEnemy` lacks discriminators for Level 3/4 enemies.
9. `TC-T1-F6.1`: `VorstCodexUI.cs` retains centered `ScaleToFit` viewport.
10. `TC-T1-F10.1`: `MainMenuAndIntroController.cs` has hardcoded `({unlockedCodex}/6)`.
11. `TC-T1-F10.2`: `PlayerStats.cs` has hardcoded `({unlockedCodex}/6)`.
12. `TC-T1-F5.1`: `VorstCodexUI.cs` lacks zone tab declarations.
13. `TC-T1-F5.2`: `VorstCodexUI.cs` lacks zone tab filtering logic.
14. `TC-T1-F5.3`: `VorstCodexUI.cs` lacks scroll view or tab-bounded listing.
15. `TC-T3-P1.1`: `VorstCodexUI.cs` lacks tab switch clamping.
16. `TC-T1-F6.4`: `VorstCodexUI.cs` lacks dynamic aspect baseline calculation.
17. `TC-T4-S1.3`: Colmena Level 3 species not registered in catalog.
18. `TC-T4-S1.4`: Trono Level 4 species not registered in catalog.

Once Milestone M2 delivers these implementations, the test runner will automatically transition to **40 PASS / 0 FAIL (Exit Code 0)**.
