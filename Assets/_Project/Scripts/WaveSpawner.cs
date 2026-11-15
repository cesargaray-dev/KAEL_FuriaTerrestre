using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public Transform kaelTransform;
    public GameObject vorstWalkerPrefab;
    public GameObject sovereignBossPrefab;

    [Header("Configuración de Hordas")]
    public int totalWaves = 10;
    public int baseEnemiesPerWave = 4; // Sube hasta 9 enemigos en las últimas oleadas
    public float spawnDistanceAhead = 10.5f;
    public float enemySpacing = 1.35f;
    public float groundY = -2.85f;     // Alineado con Kael por encima de la barra de botones

    [Header("Configuración Niveles 2, 3 y 4")]
    public bool isLevel2BaseVorst = false;
    public bool isLevel3Hive = false;
    public bool isLevel4Throneroom = false;
    public float difficultyMultiplier = 1.0f;

    [Header("Estado Actual")]
    public int currentWave = 0;
    public int totalKillsInLevel = 0;
    private int enemiesAlive = 0;
    private bool spawningWave = false;

    public static bool IsWaitingForSanctumBreach { get; private set; } = false;
    public static float SanctumGateBlockX { get; private set; } = 0f;
    private bool isBreachPromptActive = false;
    private bool breachRequested = false;
    private float sanctumGatePosX = 0f;

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "04_Final_NexusThroneroom" || (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 4))
        {
            isLevel4Throneroom = true;
            isLevel3Hive = false;
            isLevel2BaseVorst = false;
            groundY = -2.95f;
            EnemyDummy.PreloadLevel2Frames();
            EnemyDummy.PreloadLevel3Frames();
            NexusThroneroomEnvironment.EnsureExists();
            EndingsManager.EnsureExists();
        }
        else if (sceneName == "03_Nivel3_Colmena" || (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 3))
        {
            isLevel3Hive = true;
            isLevel2BaseVorst = false;
            groundY = -2.95f;
            EnemyDummy.PreloadLevel3Frames();
        }
        else if (sceneName == "Lab_Zahir" || (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 2))
        {
            isLevel2BaseVorst = true;
            groundY = -2.95f;
            EnemyDummy.PreloadLevel2Frames();
        }

        // Asegurar que exista el gestor de Audio en la escena
        if (FindAnyObjectByType<LevelAudioManager>() == null)
        {
            gameObject.AddComponent<LevelAudioManager>();
        }

        if (kaelTransform == null)
        {
            KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
            if (kael != null) kaelTransform = kael.transform;
        }

        if (groundY < -3.05f) groundY = -2.95f;

        StartCoroutine(SpawnNextWaveRoutine());
    }

    void Update()
    {
        if (PlayerStats.Instance != null && PlayerStats.Instance.IsAnyBlockingModalOpen)
            return;

        if (BridgeLevel1To2Controller.Instance != null && BridgeLevel1To2Controller.Instance.isBridgeActive)
            return;

        if (!spawningWave && enemiesAlive <= 0 && currentWave < totalWaves)
        {
            StartCoroutine(SpawnNextWaveRoutine());
        }
    }

    public void RestartWavesKeepingProgress()
    {
        StopAllCoroutines();

        EnemyDummy[] remaining = FindObjectsByType<EnemyDummy>(FindObjectsInactive.Exclude);
        foreach (EnemyDummy e in remaining)
        {
            Destroy(e.gameObject);
        }

        currentWave = 0;
        enemiesAlive = 0;
        spawningWave = false;
        IsWaitingForSanctumBreach = false;
        isBreachPromptActive = false;
        breachRequested = false;

        if (NexusThroneroomEnvironment.Instance != null)
        {
            NexusThroneroomEnvironment.Instance.ResetSanctumArena();
        }

        if (kaelTransform != null)
        {
            KaelAutoController kael = kaelTransform.GetComponent<KaelAutoController>();
            if (kael != null) kael.ResetKaelPosition();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCombatMusic();
        }

        StartCoroutine(SpawnNextWaveRoutine());
    }

    /// <summary>
    /// Devuelve el arquetipo (0: BioStalker, 1: BioLasher, 2: BioHulk, 3: HiveInquisitor)
    /// con composiciones temáticas y no repetitivas para cada oleada del Nivel 3.
    /// </summary>
    public static int GetLevel3EnemyArchetype(int wave, int enemyIndex, int totalInWave)
    {
        // 0: BioStalker (Pantera ágil melee)
        // 1: BioLasher (Alienígena levitador a media distancia)
        // 2: BioHulk (Coloso blindado tanque)
        // 3: HiveInquisitor (Hechicero psiónico a distancia)

        int[][] waveCompositions = new int[][]
        {
            // Oleada 1: Incursión rápida de reconocimiento (Panteras con un tentacular de apoyo)
            new int[] { 0, 0, 0, 1 },
            // Oleada 2: Tentáculos en la penumbra (Predominio de tentaculares con panteras flanqueando)
            new int[] { 0, 1, 1, 0, 1 },
            // Oleada 3: Primera Muralla Biomecánica (Aparición del primer Coloso tanque)
            new int[] { 2, 0, 0, 1, 0 },
            // Oleada 4: Ecos Psiónicos (Lanzadores a distancia Inquisidor + vanguardia de panteras)
            new int[] { 0, 3, 1, 0, 3 },
            // Oleada 5: Escolta del Inquisidor Supremo (Coloso protector + acechadores)
            new int[] { 2, 0, 1, 0, 2 },
            // Oleada 6: Asedio Blindado (Doble Coloso al frente con soporte de látigos)
            new int[] { 2, 2, 0, 1, 0, 1 },
            // Oleada 7: Bombardeo Psiónico Cruzado (Múltiples hechiceros y tentáculos)
            new int[] { 3, 1, 3, 0, 3, 1 },
            // Oleada 8: Enjambre Quimérico Equilibrado (Composición híbrida variada)
            new int[] { 0, 2, 3, 1, 0, 2, 3 },
            // Oleada 9: Guardia Pretoriana de la Colmena (Tanques colosales + hechiceros psiónicos pesados)
            new int[] { 2, 3, 2, 1, 3, 0, 2 },
            // Oleada 10: Escolta del Mecha Vortex-Omega
            new int[] { 0, 3, 2, 1, 0 }
        };

        int waveIdx = Mathf.Clamp(wave - 1, 0, waveCompositions.Length - 1);
        int[] comp = waveCompositions[waveIdx];
        if (comp != null && comp.Length > 0)
        {
            return comp[enemyIndex % comp.Length];
        }
        return enemyIndex % 4;
    }

    /// <summary>
    /// BLOQUE 5 (Issue #23): Cuando el Jefe Final VORTEX-OMEGA entra en Fase de Furia (<45% HP),
    /// invoca 2 Acechadores Quimera (BioStalkers) de escolta.
    /// </summary>
    public void SpawnVortexOmegaEscorts(Vector3 bossPos)
    {
        if (vorstWalkerPrefab == null) return;

        for (int i = 0; i < 2; i++)
        {
            Vector3 escortPos = new Vector3(bossPos.x + 1.8f + i * 1.4f, groundY, 0f);
            GameObject obj = Instantiate(vorstWalkerPrefab, escortPos, Quaternion.identity);
            EnemyDummy enemy = obj.GetComponent<EnemyDummy>();
            if (enemy != null)
            {
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Color.white;
                enemy.enemyTitle = "Acechador Quimera [Escolta Omega]";
                enemy.SetupLevel3Visuals("BioStalker");
                obj.transform.localScale = Vector3.one * 1.05f;
                enemy.moveSpeed = 2.85f;
                enemy.attackRange = 1.5f;
                enemy.maxHP = 125f;
                enemy.currentHP = 125f;
                enemy.damage = 11f;
                enemy.goldReward = 22;
                enemy.expReward = 28;

                enemiesAlive++;
                enemy.OnEnemyDied += HandleEnemyDied;
            }
        }
    }

    /// <summary>
    /// Invoca refuerzos de la Guardia Pretoriana Vorst dentro del Santuario durante la batalla del Soberano Supremo.
    /// </summary>
    public void SpawnSovereignReinforcements(Vector3 bossPos, int count = 2)
    {
        if (vorstWalkerPrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            float offsetX = (i % 2 == 0) ? -2.2f - i * 0.7f : 2.2f + i * 0.7f;
            float spawnX = bossPos.x + offsetX;
            if (NexusThroneroomEnvironment.Instance != null)
            {
                spawnX = NexusThroneroomEnvironment.Instance.ClampXToBossArena(spawnX);
            }
            Vector3 spawnPos = new Vector3(spawnX, groundY, 0f);
            GameObject obj = Instantiate(vorstWalkerPrefab, spawnPos, Quaternion.identity);
            EnemyDummy enemy = obj.GetComponent<EnemyDummy>();
            if (enemy != null)
            {
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                SetupLevel4PraetorianUnit(enemy, obj, sr, totalWaves, i + Random.Range(0, 3), count);
                enemy.goldReward = 30;
                enemy.expReward = 35;

                if (CombatVFXManager.Instance != null)
                {
                    CombatVFXManager.Instance.SpawnShockwaveRing(spawnPos + Vector3.up * 0.6f, new Color(0.84f, 0.24f, 1f), 2.4f);
                    CombatVFXManager.Instance.SpawnElectricArc(spawnPos + Vector3.up * 0.8f, new Color(0.28f, 0.96f, 1f));
                }

                enemiesAlive++;
                enemy.OnEnemyDied += HandleEnemyDied;
            }
        }
    }

    IEnumerator PlaySanctumBreachTransitionRoutine()
    {
        if (kaelTransform == null) yield break;

        float targetCenter = Mathf.Round((kaelTransform.position.x + 8.5f) / 10.24f) * 10.24f;
        float gateX = targetCenter - 7.4f;
        sanctumGatePosX = gateX;
        Vector3 gatePos = new Vector3(gateX, groundY + 2.2f, 0f);

        IsWaitingForSanctumBreach = true;
        SanctumGateBlockX = gateX - 1.2f;
        isBreachPromptActive = true;
        breachRequested = false;

        // Construir una Compuerta de Contención Vorst multicapa visible e inconfundible
        GameObject gateObj = new GameObject("VorstSanctumContainmentGate");
        gateObj.transform.position = gatePos;

        Texture2D gateTex = Resources.Load<Texture2D>("Level4/SanctumGate_Level4");
        if (gateTex != null)
        {
            gateTex.filterMode = FilterMode.Point;
            GameObject gateArt = new GameObject("SanctumGateArt");
            gateArt.transform.SetParent(gateObj.transform, false);
            SpriteRenderer gsr = gateArt.AddComponent<SpriteRenderer>();
            gsr.sprite = Sprite.Create(gateTex, new Rect(0f, 0f, gateTex.width, gateTex.height), new Vector2(0.5f, 0f), 100f);
            float targetHeight = 6.1f;
            float scale = targetHeight / Mathf.Max(0.1f, gsr.sprite.bounds.size.y);
            gateArt.transform.localScale = new Vector3(scale, scale, 1f);
            gateArt.transform.position = new Vector3(gateX, groundY - 0.15f, 0f);
            gsr.sortingOrder = 15;
        }

        LineRenderer gateBeam = gateObj.AddComponent<LineRenderer>();
        gateBeam.material = new Material(Shader.Find("Sprites/Default"));
        gateBeam.positionCount = 2;
        gateBeam.startWidth = 0.72f;
        gateBeam.endWidth = 0.72f;
        gateBeam.startColor = new Color(0.96f, 0.20f, 0.65f, 0.90f);
        gateBeam.endColor = new Color(0.45f, 0.14f, 1f, 0.90f);
        gateBeam.sortingOrder = 15;
        gateBeam.SetPosition(0, new Vector3(gateX, groundY - 0.1f, 0f));
        gateBeam.SetPosition(1, new Vector3(gateX, groundY + 5.8f, 0f));

        LineRenderer gateCore = new GameObject("GateCore").AddComponent<LineRenderer>();
        gateCore.transform.SetParent(gateObj.transform, false);
        gateCore.material = new Material(Shader.Find("Sprites/Default"));
        gateCore.positionCount = 2;
        gateCore.startWidth = 0.16f;
        gateCore.endWidth = 0.16f;
        gateCore.startColor = new Color(1f, 0.85f, 0.98f, 0.98f);
        gateCore.endColor = new Color(0.28f, 0.96f, 1f, 0.98f);
        gateCore.sortingOrder = 16;
        gateCore.SetPosition(0, new Vector3(gateX, groundY - 0.1f, 0f));
        gateCore.SetPosition(1, new Vector3(gateX, groundY + 5.8f, 0f));

        Sprite hexSpr = Level3MonsterAssetGenerator.GetHexShieldSprite(isPureza: false, isCorrupcion: true);
        for (int h = 0; h < 3; h++)
        {
            GameObject hexNode = new GameObject($"GateHex_{h}");
            hexNode.transform.SetParent(gateObj.transform, false);
            hexNode.transform.position = new Vector3(gateX, groundY + 0.9f + h * 1.65f, 0f);
            hexNode.transform.localScale = new Vector3(1.1f, 1.35f, 1f);
            SpriteRenderer hsr = hexNode.AddComponent<SpriteRenderer>();
            hsr.sprite = hexSpr;
            hsr.color = new Color(0.95f, 0.25f, 0.85f, 0.78f);
            hsr.sortingOrder = 16;
        }

        if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();

        // Esperar a que el jugador decida derrumbar la compuerta ([J], [Espacio], [E] o clic en el banner)
        while (!breachRequested)
        {
            if (PlayerStats.Instance == null || !PlayerStats.Instance.IsAnyBlockingModalOpen)
            {
                bool keySmash = false;
                if (Keyboard.current != null &&
                    (Keyboard.current.jKey.wasPressedThisFrame ||
                     Keyboard.current.spaceKey.wasPressedThisFrame ||
                     Keyboard.current.eKey.wasPressedThisFrame))
                {
                    keySmash = true;
                }
                else
                {
                    try
                    {
                        keySmash = Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E);
                    }
                    catch { }
                }

                // Permitir romperla al pulsar ataque/interactuar cuando está en el pasillo frente al Santuario
                if (keySmash && kaelTransform.position.x >= gateX - 8.5f)
                {
                    breachRequested = true;
                }
            }
            yield return null;
        }

        IsWaitingForSanctumBreach = false;
        isBreachPromptActive = false;

        // Kael carga con determinación hasta la compuerta del Santuario
        Vector3 kaelStart = kaelTransform.position;
        Vector3 kaelStrike = new Vector3(gateX - 1.15f, groundY, 0f);
        KaelAutoController kaelCtrl = kaelTransform.GetComponent<KaelAutoController>();
        SpriteRenderer kaelSr = kaelTransform.GetComponent<SpriteRenderer>();
        if (kaelSr != null) kaelSr.flipX = (kaelStrike.x < kaelStart.x);

        float moveT = 0f;
        float moveDur = Mathf.Clamp(Mathf.Abs(kaelStrike.x - kaelStart.x) / 9.5f, 0.18f, 0.55f);
        while (moveT < moveDur)
        {
            kaelTransform.position = Vector3.Lerp(kaelStart, kaelStrike, moveT / moveDur);
            moveT += Time.deltaTime;
            yield return null;
        }
        kaelTransform.position = kaelStrike;

        // Golpe demoledor con breve hit-stop y explosión de barrera
        if (AudioManager.Instance != null) AudioManager.Instance.PlayHeavyHitSfx();
        else if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();

        Time.timeScale = 0.35f;
        if (CombatVFXManager.Instance != null)
        {
            CombatVFXManager.Instance.TriggerScreenFlash(new Color(0.35f, 0.96f, 1f), 0.92f);
            CombatVFXManager.Instance.ShakeCamera(0.85f, 0.35f);
            CombatVFXManager.Instance.SpawnBossExplosionBurst(gatePos);
            CombatVFXManager.Instance.SpawnShockwaveRing(gatePos, new Color(0.28f, 0.98f, 1f), 5.2f);
            CombatVFXManager.Instance.SpawnShockwaveRing(gatePos, new Color(0.92f, 0.25f, 1f), 3.8f);
            CombatVFXManager.Instance.SpawnHitSparks(gatePos, true, 24);
        }
        yield return new WaitForSecondsRealtime(0.22f);
        if (PlayerStats.Instance == null || !PlayerStats.Instance.IsAnyBlockingModalOpen)
        {
            Time.timeScale = 1f;
        }

        Destroy(gateObj);

        // Al romperse la barrera, se revela y bloquea simétricamente el Santuario del Trono
        NexusThroneroomEnvironment env = NexusThroneroomEnvironment.EnsureExists();
        if (env != null)
        {
            env.LockBossArenaAround(targetCenter);
        }

        // Kael entra al borde izquierdo del Santuario listo para el duelo final
        Vector3 insidePos = new Vector3(targetCenter - 5.0f, groundY, 0f);
        if (kaelCtrl != null && kaelSr != null) kaelSr.flipX = false;
        float enterT = 0f;
        while (enterT < 0.42f)
        {
            kaelTransform.position = Vector3.Lerp(kaelStrike, insidePos, enterT / 0.42f);
            enterT += Time.deltaTime;
            yield return null;
        }
        kaelTransform.position = insidePos;
    }

    void OnGUI()
    {
        if (!isBreachPromptActive) return;
        if (PlayerStats.Instance != null && PlayerStats.Instance.IsAnyBlockingModalOpen) return;

        float sx = Screen.width / 1920f;
        float sy = Screen.height / 1080f;
        float w = 880f * sx;
        float h = 92f * sy;
        Rect banner = new Rect((Screen.width - w) * 0.5f, 130f * sy, w, h);

        GUI.color = new Color(0.08f, 0.02f, 0.16f, 0.94f);
        GUI.DrawTexture(banner, Texture2D.whiteTexture);
        float pulse = 0.72f + Mathf.Sin(Time.unscaledTime * 6f) * 0.28f;
        GUI.color = new Color(0.96f, 0.25f, 0.75f, pulse);
        GUI.DrawTexture(new Rect(banner.x, banner.y, banner.width, 4f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(banner.x, banner.yMax - 4f, banner.width, 4f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(14, Mathf.RoundToInt(20f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };
        titleSt.normal.textColor = new Color(1f, 0.48f, 0.85f);
        GUI.Label(new Rect(banner.x + 12f, banner.y + 10f * sy, banner.width - 24f, 34f * sy),
            "🚧 COMPUERTA DEL SANTUARIO DEL SOBERANO DETECTADA 🚧", titleSt);

        GUIStyle subSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(12, Mathf.RoundToInt(16f * sy)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.LowerCenter
        };
        subSt.normal.textColor = new Color(0.32f, 0.98f, 1f);
        GUI.Label(new Rect(banner.x + 12f, banner.y + 38f * sy, banner.width - 24f, 38f * sy),
            "PULSA [J], [ESPACIO], [E] O HAZ CLIC AQUÍ PARA DERRIBAR LA BARRERA Y ENTRAR", subSt);

        if (GUI.Button(banner, GUIContent.none, GUIStyle.none))
        {
            breachRequested = true;
        }
    }

    IEnumerator SpawnNextWaveRoutine()
    {
        spawningWave = true;
        currentWave++;

        bool isMiniBossWave = (currentWave == 5);
        bool isBossWave = (currentWave == totalWaves);

        if (isMiniBossWave || isBossWave)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBossMusic();
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCombatMusic();
        }

        if (isMiniBossWave && CombatVFXManager.Instance != null)
        {
            if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();
            string warnTitle = isLevel4Throneroom
                ? "⚠ ¡CUSTODIO PRETORIANO VORST! ⚠"
                : (isLevel3Hive
                    ? "⚠ ¡ANOMALÍA PSIÓNICA EN LA COLMENA! ⚠"
                    : (isLevel2BaseVorst ? "⚠ ¡COLOSO DE NÚCLEO ROJO! ⚠" : "⚠ ¡ALERTA DE TANQUE PESADO! ⚠"));
            string warnSub = isLevel4Throneroom
                ? "MINI-JEFE DE OLEADA 5 — IMPACTO EN PICADA CON LANZA DE ENERGÍA [PARRY S]"
                : (isLevel3Hive
                    ? "MINI-JEFE DE OLEADA 5: INQUISIDOR PSIÓNICO SUPREMO"
                    : (isLevel2BaseVorst ? "PRIMER JEFE: BRUTO VORST EN LA OLEADA 5" : "BRUTO VORST DETECTADO EN LA OLEADA 5"));
            CombatVFXManager.Instance.TriggerBossWarning(warnTitle, warnSub, 2.4f);
            yield return new WaitForSeconds(2.0f);
        }
        else if (isBossWave && CombatVFXManager.Instance != null)
        {
            if (isLevel4Throneroom)
            {
                yield return StartCoroutine(PlaySanctumBreachTransitionRoutine());
            }

            if (LevelAudioManager.Instance != null) LevelAudioManager.Instance.PlayBossAlert();
            string warnTitle = isLevel4Throneroom
                ? "☣ ¡ARCONTE SUPREMO — SOBERANO VORST! ☣"
                : (isLevel3Hive
                    ? "⚠ AMENAZA EXTREMA: MECHA BERSERKER VORTEX-OMEGA ⚠"
                    : (isLevel2BaseVorst ? "⚠ ¡SOBERANO DE LA BASE VORST! ⚠" : "⚠ ¡ADVERTENCIA: LÍDER DE ZONA! ⚠"));
            string warnSub = isLevel4Throneroom
                ? "JEFE FINAL EN 3 FASES — DUELO MARCIAL // JUICIO PSIÓNICO // FUSIÓN CON LÍA"
                : (isLevel3Hive
                    ? "JEFE FINAL DE LA COLMENA — GUARDIÁN DE LAS CÁPSULAS DE ESTASIS"
                    : (isLevel2BaseVorst ? "JEFE FINAL: ARCONTE PSIÓNICO VORST" : "COMANDANTE VORST BLOQUEA LA SALIDA DEL SECTOR 7"));
            CombatVFXManager.Instance.TriggerBossWarning(warnTitle, warnSub, 2.8f);
            yield return new WaitForSeconds(2.2f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        int hordeCount = baseEnemiesPerWave + Mathf.FloorToInt(currentWave * 0.50f);
        if (isMiniBossWave || isBossWave) hordeCount += 1;
        if (isLevel4Throneroom && isBossWave) hordeCount = 3; // 2 guardias de cobertura + Soberano Supremo en la batalla final

        enemiesAlive = hordeCount;

        for (int i = 0; i < hordeCount; i++)
        {
            if (kaelTransform == null || vorstWalkerPrefab == null) yield break;

            float jitterX = Random.Range(-0.12f, 0.12f);
            float spawnX = kaelTransform.position.x + spawnDistanceAhead + (i * enemySpacing) + jitterX;
            if (isLevel4Throneroom && isBossWave && NexusThroneroomEnvironment.Instance != null && NexusThroneroomEnvironment.Instance.IsBossArenaLocked)
            {
                spawnX = Mathf.Clamp(
                    NexusThroneroomEnvironment.Instance.ArenaMaxX - 1.5f - (hordeCount - 1 - i) * 1.4f,
                    NexusThroneroomEnvironment.Instance.ArenaMinX + 1.0f,
                    NexusThroneroomEnvironment.Instance.ArenaMaxX - 0.8f
                );
            }
            Vector3 spawnPos = new Vector3(spawnX, groundY, 0f);

            bool spawningSovereignBoss = (isLevel4Throneroom && isBossWave && i == hordeCount - 1);
            GameObject prefabToUse = (spawningSovereignBoss && sovereignBossPrefab != null) ? sovereignBossPrefab : vorstWalkerPrefab;
            GameObject enemyObj = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
            EnemyDummy enemy = enemyObj.GetComponent<EnemyDummy>();

            if (enemy != null)
            {
                SpriteRenderer sr = enemyObj.GetComponent<SpriteRenderer>();

                if (isBossWave && i == hordeCount - 1)
                {
                    enemy.isBoss = true;
                    if (isLevel4Throneroom)
                    {
                        // NIVEL 4 FINAL: ARCONTE SUPREMO / SOBERANO VORST EN 3 FASES
                        enemy.enemyTitle = "Soberano Supremo — Arconte Vorst";
                        enemy.SetupLevel3Visuals("SupremeSovereign");
                        enemyObj.transform.localScale = Vector3.one * 1.12f;
                        if (sr != null) sr.color = Color.white;
                        enemy.maxHP = 3400f;
                        enemy.currentHP = 3400f;
                        enemy.damage = 28f;
                        enemy.moveSpeed = 2.35f;
                        enemy.attackRange = 2.35f;
                        enemy.attackRate = 1.25f;
                        enemy.goldReward = 500;
                        enemy.expReward = 420;

                        SovereignBossAI sovAI = enemyObj.GetComponent<SovereignBossAI>();
                        if (sovAI == null) sovAI = enemyObj.AddComponent<SovereignBossAI>();
                        sovAI.Configure(SovereignBossAI.BossRole.SupremeSovereign);
                    }
                    else if (isLevel3Hive)
                    {
                        // BLOQUE 5: JEFE FINAL DEL NIVEL 3 (VORTEX-OMEGA)
                        if (sr != null) sr.color = Color.white;
                        enemy.SetupLevel3Monster(0, currentWave, boss: true, miniBoss: false);
                    }
                    else if (isLevel2BaseVorst)
                    {
                        enemy.enemyTitle = "Arconte Vorst — Soberano Psiónico";
                        enemy.SetupCustomVisuals("Archon");
                        enemyObj.transform.localScale = Vector3.one * 1.35f;
                        if (sr != null) sr.color = Color.white;
                        enemy.attackRange = 2.4f;
                        enemy.attackRate = 1.0f;
                        enemy.maxHP = 2200f;
                        enemy.damage = 22f;
                        enemy.goldReward = 250;
                        enemy.expReward = 220;
                    }
                    else
                    {
                        enemy.enemyTitle = "Comandante Vorst — Líder Sector 7";
                        enemyObj.transform.localScale *= 1.75f;
                        if (sr != null) sr.color = new Color(1f, 0.35f, 0.65f);
                        enemy.attackRange = 1.9f;
                        enemy.maxHP = 450f;
                        enemy.damage = 18f;
                        enemy.goldReward = 150;
                        enemy.expReward = 140;
                    }
                }
                else if (isMiniBossWave && i == hordeCount - 1)
                {
                    enemy.isMiniBoss = true;
                    if (isLevel4Throneroom)
                    {
                        // NIVEL 4 OLEADA 5 MINI-JEFE: CUSTODIO PRETORIANO VORST
                        enemy.enemyTitle = "Custodio Pretoriano Vorst — Lanza Imperial";
                        enemy.SetupLevel3Visuals("PraetorianCustodian");
                        enemyObj.transform.localScale = Vector3.one * 1.08f;
                        if (sr != null) sr.color = Color.white;
                        enemy.maxHP = 1550f;
                        enemy.currentHP = 1550f;
                        enemy.damage = 22f;
                        enemy.moveSpeed = 2.0f;
                        enemy.attackRange = 2.25f;
                        enemy.attackRate = 1.0f;
                        enemy.goldReward = 220;
                        enemy.expReward = 200;

                        SovereignBossAI custAI = enemyObj.GetComponent<SovereignBossAI>();
                        if (custAI == null) custAI = enemyObj.AddComponent<SovereignBossAI>();
                        custAI.Configure(SovereignBossAI.BossRole.PraetorianCustodian);
                    }
                    else if (isLevel3Hive)
                    {
                        // BLOQUE 5: MINI-JEFE OLEADA 5 DEL NIVEL 3 (Inquisidor Psiónico Supremo)
                        if (sr != null) sr.color = Color.white;
                        enemy.SetupLevel3Monster(3, currentWave, boss: false, miniBoss: true);
                    }
                    else if (isLevel2BaseVorst)
                    {
                        enemy.enemyTitle = "Bruto Vorst — Coloso de Núcleo Rojo";
                        enemy.SetupCustomVisuals("Brute");
                        enemyObj.transform.localScale = Vector3.one * 1.30f;
                        if (sr != null) sr.color = Color.white;
                        enemy.maxHP = 950f;
                        enemy.damage = 15f;
                        enemy.moveSpeed = 1.55f;
                        enemy.attackRange = 1.65f;
                        enemy.goldReward = 120;
                        enemy.expReward = 110;
                    }
                    else
                    {
                        enemy.enemyTitle = "Bruto Vorst — Coloso Blindado";
                        enemyObj.transform.localScale *= 1.45f;
                        if (sr != null) sr.color = new Color(1f, 0.65f, 0.25f);
                        enemy.maxHP = 240f;
                        enemy.damage = 14f;
                        enemy.moveSpeed = 1.6f;
                        enemy.attackRange = 1.7f;
                        enemy.goldReward = 65;
                        enemy.expReward = 75;
                    }
                }
                else
                {
                    if (isLevel4Throneroom)
                    {
                        SetupLevel4PraetorianUnit(enemy, enemyObj, sr, currentWave, i, hordeCount);
                    }
                    else if (isLevel3Hive)
                    {
                        // BLOQUE 5: COMPOSICIONES TEMÁTICAS Y VARIADAS EN CADA OLEADA DEL NIVEL 3
                        int archetype = GetLevel3EnemyArchetype(currentWave, i, hordeCount);
                        if (sr != null) sr.color = Color.white;
                        enemy.SetupLevel3Monster(archetype, currentWave, boss: false, miniBoss: false);
                    }
                    else if (isLevel2BaseVorst)
                    {
                        enemy.enemyTitle = "Acechador Vorst";
                        enemy.SetupCustomVisuals("Stalker");
                        enemyObj.transform.localScale = Vector3.one * 1.0f;
                        if (sr != null) sr.color = Color.white;
                        enemy.attackRange = 1.45f;
                        enemy.moveSpeed = 2.0f;
                        enemy.maxHP = Mathf.RoundToInt(58f * Mathf.Pow(1.18f, currentWave - 1));
                        enemy.damage = Mathf.RoundToInt(5f * Mathf.Pow(1.14f, currentWave - 1));
                        enemy.goldReward = Mathf.RoundToInt(8f + (currentWave * 2.5f));
                        enemy.expReward = 12 + (currentWave * 3);
                    }
                    else
                    {
                        enemy.maxHP = Mathf.RoundToInt(30f * Mathf.Pow(1.24f, currentWave - 1));
                        enemy.damage = Mathf.RoundToInt(4f * Mathf.Pow(1.18f, currentWave - 1));
                        enemy.goldReward = Mathf.RoundToInt(6f + (currentWave * 2f));
                        enemy.expReward = 10 + (currentWave * 2);
                    }
                }

                enemy.OnEnemyDied += HandleEnemyDied;
            }
        }

        spawningWave = false;
    }

    void HandleEnemyDied()
    {
        enemiesAlive--;
        totalKillsInLevel++;

        if (enemiesAlive <= 0)
        {
            if (currentWave >= totalWaves)
            {
                if (isLevel4Throneroom)
                {
                    EndingsManager.TriggerEndingFromFinalBoss();
                    return;
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayVictoryDilemmaMusic();
                }

                if (PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.TriggerEndOfZoneDilemma();
                }
            }
            else if (PlayerStats.Instance != null && kaelTransform != null)
            {
                if (currentWave == 5 && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayCombatMusic();
                }

                float heal = Mathf.RoundToInt(PlayerStats.Instance.maxHP * 0.15f);
                PlayerStats.Instance.currentHP = Mathf.Min(PlayerStats.Instance.maxHP, PlayerStats.Instance.currentHP + heal);

                if (CombatVFXManager.Instance != null)
                {
                    CombatVFXManager.Instance.SpawnFloatingText(
                        kaelTransform.position + Vector3.up * 1.5f,
                        $"¡OLEADA {currentWave} SUPERADA! (+{heal} HP)",
                        new Color(0.2f, 0.95f, 0.5f),
                        20,
                        1.2f
                    );
                }
            }
        }
    }

    /// <summary>
    /// Configura unidades de la Guardia Pretoriana de Élite Vorst en formaciones tácticas de cobertura
    /// (vanguardia blindada al frente cubriendo a tiradores psiónicos y acechadores cibernéticos detrás).
    /// </summary>
    private void SetupLevel4PraetorianUnit(EnemyDummy enemy, GameObject enemyObj, SpriteRenderer sr, int wave, int index, int totalCount)
    {
        // Formación de cobertura: vanguardia con escudo (0), hostigadores con hoja (1, 2) y francotirador en retaguardia (3)
        int roleSlot = index % 4;
        if (roleSlot == 0)
        {
            // Vanguardia de Cobertura Acorazada Pretoriana (Escudo Aegis + Maza)
            enemy.enemyTitle = "Baluarte Aegis Pretoriano";
            enemy.SetupLevel3Visuals("PraetorianAegis");
            enemyObj.transform.localScale = Vector3.one * 1.04f;
            if (sr != null) sr.color = Color.white;
            enemy.maxHP = Mathf.RoundToInt(175f * Mathf.Pow(1.14f, Mathf.Max(0, wave - 1)));
            enemy.damage = Mathf.RoundToInt(14f * Mathf.Pow(1.11f, Mathf.Max(0, wave - 1)));
            enemy.moveSpeed = 1.45f;
            enemy.attackRange = 2.25f;
        }
        else if (roleSlot == 1 || roleSlot == 2)
        {
            // Guardia de Asalto con Hoja de Plasma (Alabarda / Espada Vorst)
            bool fastFlanker = (roleSlot == 2);
            enemy.enemyTitle = "Hoja Pretoriana Vorst";
            enemy.SetupLevel3Visuals("PraetorianBlade");
            enemyObj.transform.localScale = Vector3.one;
            if (sr != null) sr.color = Color.white;
            enemy.maxHP = Mathf.RoundToInt((fastFlanker ? 112f : 126f) * Mathf.Pow(1.14f, Mathf.Max(0, wave - 1)));
            enemy.damage = Mathf.RoundToInt((fastFlanker ? 12f : 13f) * Mathf.Pow(1.11f, Mathf.Max(0, wave - 1)));
            enemy.moveSpeed = fastFlanker ? 2.45f : 2.10f;
            enemy.attackRange = 1.95f;
        }
        else
        {
            // Francotirador Pretoriano con Rifle de Plasma ubicado detrás de la vanguardia
            enemy.enemyTitle = "Francotirador Pretoriano";
            enemy.SetupLevel3Visuals("PraetorianMarksman");
            enemyObj.transform.localScale = Vector3.one;
            enemyObj.transform.position += new Vector3(1.85f, 0f, 0f);
            if (sr != null) sr.color = Color.white;
            enemy.maxHP = Mathf.RoundToInt(128f * Mathf.Pow(1.14f, Mathf.Max(0, wave - 1)));
            enemy.damage = Mathf.RoundToInt(13f * Mathf.Pow(1.11f, Mathf.Max(0, wave - 1)));
            enemy.moveSpeed = 1.75f;
            enemy.attackRange = 2.65f;
        }

        enemy.goldReward = Mathf.RoundToInt(16f + (wave * 4f));
        enemy.expReward = 20 + (wave * 5);
        enemy.currentHP = enemy.maxHP;
    }
}
