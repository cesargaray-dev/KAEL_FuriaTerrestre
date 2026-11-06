using UnityEngine;
using System.Collections;

public class CombatVFXManager : MonoBehaviour
{
    public static CombatVFXManager Instance { get; private set; }

    private Sprite coinSprite;
    private Sprite expOrbSprite;
    private Sprite sparkSprite;
    private Sprite whitePixelSprite;
    private Sprite ringSprite;
    private Transform kaelTarget;

    // Variables de Alerta de Jefe
    private bool showWarning = false;
    private string warningTitle = "";
    private string warningSubtitle = "";
    private float warningTimer = 0f;

    // Variables de Cinemática de Ultimate y Flashes de Pantalla
    private bool ultCinematicActive = false;
    private float ultFlashAlpha = 0f;
    private Color customFlashColor = new Color(0.55f, 0.95f, 1f, 1f);

    // Variables de Contador de Combo (Sincronizadas con PlayerStats)
    private int comboHits = 0;
    private float comboResetTimer = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        coinSprite = CreatePixelCircleSprite(10, new Color(1f, 0.84f, 0.15f), new Color(0.9f, 0.55f, 0f));
        expOrbSprite = CreatePixelCircleSprite(9, new Color(0.22f, 0.88f, 1f), new Color(0.1f, 0.45f, 0.9f));
        sparkSprite = CreatePixelCircleSprite(6, Color.white, new Color(0.22f, 0.88f, 1f));
        whitePixelSprite = CreateSolidPixelSprite();
        ringSprite = CreateRingSprite(64);
    }

    void Start()
    {
        KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
        if (kael != null) kaelTarget = kael.transform;
    }

    void Update()
    {
        if (comboHits > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0f) comboHits = 0;
        }

        if (ultFlashAlpha > 0f)
        {
            ultFlashAlpha = Mathf.MoveTowards(ultFlashAlpha, 0f, Time.unscaledDeltaTime * 2.4f);
        }
    }

    public void RegisterComboHit()
    {
        comboHits++;
        comboResetTimer = 4.5f;
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.RegisterComboHit();
        }
    }

    public void TriggerScreenFlash(Color flashCol, float alpha = 0.55f)
    {
        customFlashColor = flashCol;
        ultFlashAlpha = Mathf.Max(ultFlashAlpha, alpha);
    }

    // --- EFECTOS CINEMÁTICOS DE LA ULTIMATE ---
    public void SetUltimateCinematic(bool active, bool triggerFlash = false)
    {
        ultCinematicActive = active;
        if (triggerFlash)
        {
            bool isCorrupt = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);
            bool isPureza = (PlayerStats.Instance != null && PlayerStats.Instance.compassionPoints > PlayerStats.Instance.rigorPoints);
            customFlashColor = isCorrupt
                ? new Color(1f, 0.18f, 0.48f)
                : (isPureza ? new Color(1f, 0.92f, 0.45f) : new Color(0.55f, 0.95f, 1f));
            ultFlashAlpha = 0.65f;
        }
    }

    // Fase 1 de la Ulti: Partículas de energía absorbidas hacia el Guantelete Nexo
    public void SpawnUltChargeInward(Vector3 centerPos, float duration)
    {
        StartCoroutine(ChargeInwardRoutine(centerPos, duration));
    }

    IEnumerator ChargeInwardRoutine(Vector3 centerPos, float duration)
    {
        bool isCorrupt = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);
        bool isPureza = (PlayerStats.Instance != null && PlayerStats.Instance.compassionPoints > PlayerStats.Instance.rigorPoints);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 randomDir = Random.insideUnitCircle.normalized * Random.Range(1.8f, 3.2f);
                Vector3 startPos = centerPos + new Vector3(randomDir.x, randomDir.y, 0f);

                GameObject orb = new GameObject("UltChargeOrb");
                orb.transform.position = startPos;
                SpriteRenderer sr = orb.AddComponent<SpriteRenderer>();
                sr.sprite = (i % 2 == 0) ? expOrbSprite : coinSprite;
                if (isCorrupt)
                    sr.color = (i % 2 == 0) ? new Color(1f, 0.18f, 0.45f) : new Color(0.68f, 0.15f, 1f);
                else if (isPureza)
                    sr.color = (i % 2 == 0) ? new Color(0.35f, 0.98f, 1f) : new Color(1f, 0.90f, 0.35f);
                sr.sortingOrder = 25;

                ChargeParticle cp = orb.AddComponent<ChargeParticle>();
                cp.Init(centerPos, 0.22f);
            }
            elapsed += 0.06f;
            yield return new WaitForSecondsRealtime(0.06f);
        }
    }

    // Fase 2 de la Ulti: Rayo Láser Gigante de Triple Capa que cambia según Compasión / Rigor (Bloque 4)
    public void SpawnUltimateMegaBeam(Vector3 originPos, float length, float duration)
    {
        bool isCorrupt = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);
        bool isPureza = (PlayerStats.Instance != null && PlayerStats.Instance.compassionPoints > PlayerStats.Instance.rigorPoints);
        customFlashColor = isCorrupt
            ? new Color(1f, 0.15f, 0.42f)
            : (isPureza ? new Color(0.45f, 0.98f, 1f) : new Color(0.55f, 0.95f, 1f));
        ultFlashAlpha = 0.78f;
        StartCoroutine(MegaBeamRoutine(originPos, length, duration, isPureza, isCorrupt));
    }

    IEnumerator MegaBeamRoutine(Vector3 originPos, float length, float duration, bool isPureza, bool isCorrupt)
    {
        GameObject beamRoot = new GameObject("NexoMegaBeam");
        beamRoot.transform.position = originPos;

        Color outerCol = isCorrupt
            ? new Color(0.55f, 0.06f, 0.88f, 0.65f)
            : (isPureza ? new Color(1f, 0.84f, 0.22f, 0.62f) : new Color(0.1f, 0.65f, 1f, 0.55f));

        Color midCol = isCorrupt
            ? new Color(1f, 0.14f, 0.38f, 0.92f)
            : (isPureza ? new Color(0.25f, 0.98f, 1f, 0.92f) : new Color(0.22f, 0.95f, 1f, 0.90f));

        Color coreCol = isCorrupt
            ? new Color(1f, 0.78f, 0.92f, 1f)
            : new Color(1f, 1f, 1f, 1f);

        SpriteRenderer outerBeam = CreateBeamLayer(beamRoot.transform, "OuterGlow", outerCol, 20);
        SpriteRenderer midBeam = CreateBeamLayer(beamRoot.transform, "MidBeam", midCol, 21);
        SpriteRenderer coreBeam = CreateBeamLayer(beamRoot.transform, "CoreBeam", coreCol, 22);

        float absLen = Mathf.Abs(length);
        float dirSign = (length < 0f) ? -1f : 1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 55f) * 0.25f;
            float fadeOut = (elapsed > duration * 0.75f) ? Mathf.InverseLerp(duration, duration * 0.75f, elapsed) : 1f;
            float currentThickness = pulse * fadeOut;

            outerBeam.transform.localScale = new Vector3(absLen, 1.95f * currentThickness, 1f);
            outerBeam.transform.localPosition = new Vector3(length / 2f, 0f, 0f);

            midBeam.transform.localScale = new Vector3(absLen, 1.10f * currentThickness, 1f);
            midBeam.transform.localPosition = new Vector3(length / 2f, 0f, 0f);

            coreBeam.transform.localScale = new Vector3(absLen, 0.48f * currentThickness, 1f);
            coreBeam.transform.localPosition = new Vector3(length / 2f, 0f, 0f);

            if (Random.value > 0.28f)
            {
                Vector3 sparkPos = originPos + new Vector3(dirSign * Random.Range(0.5f, absLen * 0.85f), Random.Range(-0.55f, 0.55f), 0f);
                SpawnHitSparks(sparkPos, true, 3);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(beamRoot);
    }

    SpriteRenderer CreateBeamLayer(Transform parent, string name, Color col, int sortOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = whitePixelSprite;
        sr.color = col;
        sr.sortingOrder = sortOrder;
        return sr;
    }

    // --- BLOQUE 1: DESTELLO Y ONDA DE CHOQUE RADIAL DE PARRY PERFECTO ---
    public void SpawnParryFlashAndShockwave(Vector3 pos, Color primaryCol, Color secondaryCol)
    {
        TriggerScreenFlash(primaryCol, 0.48f);
        SpawnShockwaveRing(pos, primaryCol, 2.85f);
        SpawnShockwaveRing(pos, secondaryCol, 1.95f);

        for (int i = 0; i < 14; i++)
        {
            GameObject sp = new GameObject("ParrySpark");
            sp.transform.position = pos;
            SpriteRenderer sr = sp.AddComponent<SpriteRenderer>();
            sr.sprite = sparkSprite;
            sr.color = (i % 2 == 0) ? primaryCol : secondaryCol;
            sr.sortingOrder = 26;

            SparkParticle p = sp.AddComponent<SparkParticle>();
            p.Init(true);
        }
    }

    public void SpawnShockwaveRing(Vector3 pos, Color col, float targetScale = 2.4f)
    {
        GameObject ringObj = new GameObject("NexoShockwaveRing");
        ringObj.transform.position = pos;
        SpriteRenderer sr = ringObj.AddComponent<SpriteRenderer>();
        sr.sprite = ringSprite;
        sr.color = col;
        sr.sortingOrder = 24;

        ShockwaveRingBehaviour ring = ringObj.AddComponent<ShockwaveRingBehaviour>();
        ring.Init(targetScale, 0.30f);
    }

    // --- BLOQUE 2: SOMBRAS HOLOGRÁFICAS (AFTERIMAGE GHOST TRAIL) Y ARCOS ELÉCTRICOS EN OVERDRIVE ---
    public void SpawnAfterimageGhost(Sprite spr, Vector3 pos, Vector3 worldScale, bool flipX, Color ghostColor, int sortingOrder = 9)
    {
        if (spr == null) return;
        GameObject ghost = new GameObject("KaelAfterimageGhost");
        ghost.transform.position = pos;
        ghost.transform.localScale = worldScale;

        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.flipX = flipX;
        sr.color = ghostColor;
        sr.sortingOrder = sortingOrder;

        AfterimageFade fade = ghost.AddComponent<AfterimageFade>();
        fade.Init(ghostColor, 0.22f);
    }

    public void SpawnElectricArc(Vector3 basePos, Color arcColor)
    {
        GameObject arc = new GameObject("OverdriveElectricArc");
        arc.transform.position = basePos + new Vector3(Random.Range(-0.48f, 0.48f), Random.Range(-0.2f, 0.9f), 0f);
        arc.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-35f, 35f));
        arc.transform.localScale = new Vector3(0.09f, Random.Range(0.45f, 0.95f), 1f);

        SpriteRenderer sr = arc.AddComponent<SpriteRenderer>();
        sr.sprite = whitePixelSprite;
        sr.color = arcColor;
        sr.sortingOrder = 24;

        RisingParticle rp = arc.AddComponent<RisingParticle>();
        rp.Init(2.6f, 0.22f);
    }

    // --- BLOQUE 4: PARTÍCULAS DE CURACIÓN (PUREZA NEXO) Y RUNAS / ESPORAS ---
    public void SpawnHealingParticles(Vector3 basePos)
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject hpOrb = new GameObject("PurezaHealOrb");
            hpOrb.transform.position = basePos + new Vector3(Random.Range(-0.55f, 0.55f), Random.Range(-0.2f, 0.7f), 0f);
            hpOrb.transform.localScale = Vector3.one * Random.Range(0.75f, 1.25f);

            SpriteRenderer sr = hpOrb.AddComponent<SpriteRenderer>();
            sr.sprite = sparkSprite;
            sr.color = (i % 2 == 0) ? new Color(0.25f, 1f, 0.65f, 0.95f) : new Color(1f, 0.92f, 0.35f, 0.95f);
            sr.sortingOrder = 25;

            RisingParticle rp = hpOrb.AddComponent<RisingParticle>();
            rp.Init(Random.Range(1.8f, 3.0f), Random.Range(0.45f, 0.70f));
        }
    }

    public void SpawnFloatingRuneOrSpore(Vector3 worldPos, Color col)
    {
        GameObject rune = new GameObject("FloatingRuneSpore");
        rune.transform.position = worldPos;
        rune.transform.localScale = Vector3.one * Random.Range(0.55f, 0.95f);

        SpriteRenderer sr = rune.AddComponent<SpriteRenderer>();
        sr.sprite = sparkSprite;
        sr.color = col;
        sr.sortingOrder = 18;

        RisingParticle rp = rune.AddComponent<RisingParticle>();
        rp.Init(Random.Range(0.9f, 1.6f), Random.Range(0.55f, 0.95f));
    }

    // --- BLOQUE 5: PILARES DE ENERGÍA PARA EL IMPACTO SÍSMICO DE VORTEX-OMEGA ---
    public void SpawnEnergyPillar(Vector3 groundPos, Color pillarCol)
    {
        StartCoroutine(EnergyPillarRoutine(groundPos, pillarCol));
    }

    IEnumerator EnergyPillarRoutine(Vector3 groundPos, Color pillarCol)
    {
        GameObject pillar = new GameObject("VortexEnergyPillar");
        pillar.transform.position = groundPos;

        SpriteRenderer outer = CreateBeamLayer(pillar.transform, "PillarOuter", new Color(pillarCol.r, pillarCol.g, pillarCol.b, 0.65f), 19);
        SpriteRenderer core = CreateBeamLayer(pillar.transform, "PillarCore", Color.white, 20);

        float dur = 0.42f;
        float t = 0f;
        while (t < dur)
        {
            float p = t / dur;
            float widthMul = Mathf.Sin(p * Mathf.PI);
            float height = Mathf.Lerp(0.5f, 6.2f, Mathf.Clamp01(p * 3f));

            outer.transform.localScale = new Vector3(0.95f * widthMul, height, 1f);
            outer.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);

            core.transform.localScale = new Vector3(0.38f * widthMul, height, 1f);
            core.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);

            t += Time.deltaTime;
            yield return null;
        }
        Destroy(pillar);
    }

    public void SpawnEnemyDeathBurst(Vector3 pos, bool isBoss)
    {
        SpawnHitSparks(pos, true, isBoss ? 16 : 8);
        SpawnShockwaveRing(pos, isBoss ? new Color(1f, 0.25f, 0.55f) : new Color(0.22f, 0.92f, 1f), isBoss ? 3.2f : 1.7f);
    }

    public void SpawnHitSparks(Vector3 pos, bool isHeavy, int count = 6)
    {
        bool isCorrupt = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);
        bool isPureza = (PlayerStats.Instance != null && PlayerStats.Instance.compassionPoints > PlayerStats.Instance.rigorPoints);

        Color c1 = isCorrupt
            ? new Color(1f, 0.18f, 0.45f)
            : (isPureza ? new Color(0.28f, 0.98f, 1f) : new Color(0.22f, 0.92f, 1f));
        Color c2 = isCorrupt
            ? new Color(0.68f, 0.15f, 1f)
            : (isPureza ? new Color(1f, 0.90f, 0.32f) : new Color(1f, 0.85f, 0.2f));

        for (int i = 0; i < count; i++)
        {
            GameObject sp = new GameObject("HitSpark");
            sp.transform.position = pos;
            SpriteRenderer sr = sp.AddComponent<SpriteRenderer>();
            sr.sprite = sparkSprite;
            sr.color = (i % 2 == 0) ? c1 : c2;
            sr.sortingOrder = 23;

            SparkParticle p = sp.AddComponent<SparkParticle>();
            p.Init(isHeavy);
        }
    }

    // --- ALERTAS DE JEFE Y TEXTOS FLOTANTES ---
    public void TriggerBossWarning(string title, string subtitle, float duration = 2.6f)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossMusic();
        }
        StartCoroutine(BossWarningRoutine(title, subtitle, duration));
    }

    IEnumerator BossWarningRoutine(string title, string subtitle, float duration)
    {
        warningTitle = title;
        warningSubtitle = subtitle;
        showWarning = true;
        warningTimer = 0f;
        ShakeCamera(duration * 0.8f, 0.05f);

        while (warningTimer < duration)
        {
            warningTimer += Time.deltaTime;
            yield return null;
        }
        showWarning = false;
    }

    public void SpawnBossExplosionBurst(Vector3 pos)
    {
        SpawnHitSparks(pos, true, 12);
        for (int i = 0; i < 6; i++)
        {
            CreateLootItem(pos + (Vector3)Random.insideUnitCircle * 0.8f, (i % 2 == 0) ? coinSprite : expOrbSprite, i % 2 == 0);
        }
        SpawnFloatingText(pos + (Vector3)Random.insideUnitCircle * 0.9f, "¡COLAPSO VORST!", new Color(1f, 0.35f, 0.55f), 20, 0.65f);
    }

    public void SpawnFloatingText(Vector3 worldPos, string text, Color color, int fontSize = 18, float duration = 0.85f)
    {
        GameObject go = new GameObject("FloatingText");
        go.transform.position = worldPos + new Vector3(Random.Range(-0.22f, 0.22f), Random.Range(0.15f, 0.45f), 0f);
        FloatingPopup popup = go.AddComponent<FloatingPopup>();
        popup.Setup(text, color, fontSize, duration);
    }

    public void SpawnLootDrop(Vector3 deathPos, int totalGold, int totalExp, bool isBoss = false)
    {
        if (kaelTarget == null)
        {
            KaelAutoController kael = FindAnyObjectByType<KaelAutoController>();
            if (kael != null) kaelTarget = kael.transform;
        }

        int coinCount = isBoss ? 24 : Mathf.Clamp(totalGold / 2, 3, 7);
        int expCount = isBoss ? 18 : Mathf.Clamp(totalExp / 3, 2, 5);

        for (int i = 0; i < coinCount; i++) CreateLootItem(deathPos, coinSprite, true);
        for (int i = 0; i < expCount; i++) CreateLootItem(deathPos, expOrbSprite, false);

        SpawnFloatingText(deathPos + Vector3.up * 0.95f, $"+{totalGold} ORO", new Color(1f, 0.85f, 0.2f), isBoss ? 24 : 16, 1.2f);
        SpawnFloatingText(deathPos + Vector3.up * 0.55f, $"+{totalExp} EXP", new Color(0.3f, 0.9f, 1f), isBoss ? 22 : 15, 1.2f);
    }

    void CreateLootItem(Vector3 pos, Sprite spr, bool isGold)
    {
        GameObject item = new GameObject(isGold ? "GoldCoin" : "ExpOrb");
        item.transform.position = pos + Vector3.up * 0.35f;

        SpriteRenderer sr = item.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = 15;

        LootParticle lp = item.AddComponent<LootParticle>();
        lp.Init(kaelTarget, pos.y - 0.1f);
    }

    public void ShakeCamera(float duration = 0.12f, float magnitude = 0.10f)
    {
        if (CameraFollow2D.Instance != null)
        {
            CameraFollow2D.Instance.TriggerShake(duration, magnitude);
        }
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

    void DrawStyledText(Rect r, string txt, int baseFontSize, TextAnchor align, Color col)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.richText = true;
        st.fontSize = ScaleFont(baseFontSize);
        st.fontStyle = FontStyle.Bold;
        st.alignment = align;

        string shadowTxt = System.Text.RegularExpressions.Regex.Replace(txt, "<color=[^>]+>|</color>", "");
        st.normal.textColor = new Color(0f, 0f, 0f, 0.92f);
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), shadowTxt, st);

        st.normal.textColor = col;
        GUI.Label(r, txt, st);
    }

    void OnGUI()
    {
        GUI.matrix = Matrix4x4.identity;

        // 1. Bandas Cinematográficas (Letterbox) y Cartel durante la Ultimate
        if (ultCinematicActive)
        {
            bool isCorrupt = (PlayerStats.Instance != null && PlayerStats.Instance.rigorPoints > PlayerStats.Instance.compassionPoints);
            Color bannerAccent = isCorrupt ? new Color(1f, 0.20f, 0.48f) : new Color(0.22f, 0.95f, 1f);

            GUI.color = isCorrupt ? new Color(0.14f, 0.02f, 0.16f, 0.45f) : new Color(0.02f, 0.08f, 0.18f, 0.45f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            float barH = Mathf.Round(Screen.height * 0.12f);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, barH), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - barH, Screen.width, barH), Texture2D.whiteTexture);

            GUI.color = bannerAccent;
            GUI.DrawTexture(new Rect(0, barH - 3f, Screen.width, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - barH, Screen.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string ultTitle = isCorrupt
                ? "★ SOBRECARGA DISONANTE: CAÑÓN ABISAL VORST ★"
                : "★ SOBRECARGA NEXO: CAÑÓN DE PLASMA ★";
            DrawStyledText(new Rect(0, barH * 0.18f, Screen.width, barH * 0.64f), ultTitle, 30, TextAnchor.MiddleCenter, bannerAccent);
        }

        // 2. Flashazo de impacto de Ultimate / Parry / Pulso Psiónico
        if (ultFlashAlpha > 0f)
        {
            GUI.color = new Color(customFlashColor.r, customFlashColor.g, customFlashColor.b, ultFlashAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // 3. Alerta Roja de Jefe
        if (showWarning)
        {
            float pulse = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f));

            GUI.color = new Color(0.85f, 0.05f, 0.15f, pulse * 0.24f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            Rect bannerRect = ScaleRect(0f, 320f, 1920f, 110f);

            GUI.color = new Color(0.08f, 0.02f, 0.05f, 0.94f);
            GUI.DrawTexture(bannerRect, Texture2D.whiteTexture);

            GUI.color = new Color(1f, 0.15f, 0.25f, 1f);
            GUI.DrawTexture(new Rect(bannerRect.x, bannerRect.y, bannerRect.width, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(bannerRect.x, bannerRect.yMax - 3f, bannerRect.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            Color titleCol = new Color(1f, 0.28f, 0.34f, 0.55f + pulse * 0.45f);
            DrawStyledText(ScaleRect(0f, 330f, 1920f, 50f), warningTitle, 30, TextAnchor.MiddleCenter, titleCol);
            DrawStyledText(ScaleRect(0f, 378f, 1920f, 38f), warningSubtitle, 20, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f, 1f));
        }
    }

    Sprite CreateSolidPixelSprite()
    {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int i = 0; i < 16; i++) tex.SetPixel(i % 4, i / 4, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    Sprite CreateRingSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = (size - 1) * 0.5f;
        float r = size * 0.45f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - r) / 3.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    Sprite CreatePixelCircleSprite(int size, Color coreCol, Color borderCol)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = (size - 1) / 2f;
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist > radius)
                    tex.SetPixel(x, y, Color.clear);
                else if (dist > radius - 1.4f)
                    tex.SetPixel(x, y, borderCol);
                else if (x == Mathf.FloorToInt(center - 1) && y == Mathf.FloorToInt(center + 1))
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, coreCol);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 28f);
    }
}

public class ShockwaveRingBehaviour : MonoBehaviour
{
    private float targetScale = 2.4f;
    private float duration = 0.30f;
    private float elapsed = 0f;
    private SpriteRenderer sr;
    private Color baseColor;

    public void Init(float scale, float dur)
    {
        targetScale = scale;
        duration = dur;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        transform.localScale = Vector3.one * 0.25f;
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        transform.localScale = Vector3.one * Mathf.Lerp(0.25f, targetScale, 1f - (1f - t) * (1f - t));
        if (sr != null)
        {
            Color c = baseColor;
            c.a = baseColor.a * (1f - t);
            sr.color = c;
        }
        if (t >= 1f) Destroy(gameObject);
    }
}

public class AfterimageFade : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color startColor;
    private float duration = 0.22f;
    private float elapsed = 0f;

    public void Init(Color col, float dur)
    {
        sr = GetComponent<SpriteRenderer>();
        startColor = col;
        duration = dur;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        if (sr != null)
        {
            Color c = startColor;
            c.a = startColor.a * (1f - t);
            sr.color = c;
        }
        if (t >= 1f) Destroy(gameObject);
    }
}

public class RisingParticle : MonoBehaviour
{
    private float riseSpeed = 2f;
    private float duration = 0.4f;
    private float elapsed = 0f;
    private SpriteRenderer sr;
    private Color baseCol;

    public void Init(float speed, float dur)
    {
        riseSpeed = speed;
        duration = dur;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseCol = sr.color;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
        float t = Mathf.Clamp01(elapsed / duration);
        if (sr != null)
        {
            Color c = baseCol;
            c.a = baseCol.a * (1f - t);
            sr.color = c;
        }
        if (t >= 1f) Destroy(gameObject);
    }
}

public class ChargeParticle : MonoBehaviour
{
    private Vector3 targetPos;
    private float duration;
    private float elapsed = 0f;
    private Vector3 startPos;

    public void Init(Vector3 center, float dur)
    {
        startPos = transform.position;
        targetPos = center;
        duration = dur;
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        transform.position = Vector3.Lerp(startPos, targetPos, t * t);
        if (t >= 1f) Destroy(gameObject);
    }
}

public class SparkParticle : MonoBehaviour
{
    private Vector2 velocity;
    private float lifetime = 0.28f;
    private float elapsed = 0f;

    public void Init(bool isHeavy)
    {
        float speed = isHeavy ? Random.Range(3.5f, 7.5f) : Random.Range(2.0f, 4.5f);
        velocity = Random.insideUnitCircle.normalized * speed;
        if (velocity.y < 0f) velocity.y = -velocity.y * 0.8f;
        transform.localScale = Vector3.one * (isHeavy ? 1.35f : 0.9f);
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        velocity.y -= 12f * Time.unscaledDeltaTime;
        transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, elapsed / lifetime);
        if (elapsed >= lifetime) Destroy(gameObject);
    }
}

public class LootParticle : MonoBehaviour
{
    private Transform target;
    private Vector2 velocity;
    private float groundY;
    private float timer = 0f;
    private bool magnetActive = false;

    public void Init(Transform kael, float floorY)
    {
        target = kael;
        groundY = floorY + Random.Range(-0.12f, 0.12f);
        velocity = new Vector2(Random.Range(-2.6f, 2.6f), Random.Range(3.2f, 5.8f));
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (!magnetActive)
        {
            velocity.y -= 14f * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);

            if (transform.position.y <= groundY && velocity.y < 0f)
            {
                transform.position = new Vector3(transform.position.x, groundY, 0f);
                velocity.y = -velocity.y * 0.45f;
                velocity.x *= 0.65f;
            }

            if (timer >= 0.42f) magnetActive = true;
        }
        else
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 dest = target.position + Vector3.up * 0.6f;
            transform.position = Vector3.MoveTowards(transform.position, dest, 17f * Time.deltaTime);

            if (Vector3.Distance(transform.position, dest) < 0.25f)
            {
                Destroy(gameObject);
            }
        }
    }
}

public class FloatingPopup : MonoBehaviour
{
    private string text;
    private Color color;
    private int fontSize;
    private float duration;
    private float elapsed = 0f;

    public void Setup(string txt, Color col, int size, float dur)
    {
        text = txt;
        color = col;
        fontSize = size;
        duration = dur;
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        transform.position += Vector3.up * 1.25f * Time.unscaledDeltaTime;
        if (elapsed >= duration) Destroy(gameObject);
    }

    void OnGUI()
    {
        if (Camera.main == null) return;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z <= 0) return;

        GUI.matrix = Matrix4x4.identity;

        float guiY = Screen.height - screenPos.y;
        float alpha = Mathf.Clamp01(1f - (elapsed / duration));

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = CombatVFXManager.ScaleFont(fontSize);
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        float w = Mathf.Round(300f * (Screen.width / 1920f));
        float h = Mathf.Round(48f * (Screen.height / 1080f));
        Rect rect = new Rect(Mathf.Round(screenPos.x - w * 0.5f), Mathf.Round(guiY - h * 0.5f), w, h);

        style.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.92f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);

        style.normal.textColor = new Color(color.r, color.g, color.b, alpha);
        GUI.Label(rect, text, style);
    }
}
