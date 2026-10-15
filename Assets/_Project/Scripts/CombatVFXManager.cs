using UnityEngine;
using System.Collections;

public class CombatVFXManager : MonoBehaviour
{
    public static CombatVFXManager Instance { get; private set; }

    private Sprite coinSprite;
    private Sprite expOrbSprite;
    private Sprite sparkSprite;
    private Sprite whitePixelSprite;
    private Transform kaelTarget;

    // Variables de Alerta de Jefe
    private bool showWarning = false;
    private string warningTitle = "";
    private string warningSubtitle = "";
    private float warningTimer = 0f;

    // Variables de Cinemática de Ultimate
    private bool ultCinematicActive = false;
    private float ultFlashAlpha = 0f;

    // Variables de Contador de Combo
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
    }

    void Start()
    {
        KaelAutoController kael = FindFirstObjectByType<KaelAutoController>();
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
            ultFlashAlpha = Mathf.MoveTowards(ultFlashAlpha, 0f, Time.unscaledDeltaTime * 2.2f);
        }
    }

    public void RegisterComboHit()
    {
        comboHits++;
        comboResetTimer = 2.5f; // El combo se mantiene mientras Kael siga golpeando antes de 2.5s
    }

    // --- EFECTOS CINEMÁTICOS DE LA ULTIMATE ---
    public void SetUltimateCinematic(bool active, bool triggerFlash = false)
    {
        ultCinematicActive = active;
        if (triggerFlash) ultFlashAlpha = 0.65f;
    }

    // Fase 1 de la Ulti: Partículas de energía absorbidas hacia el Guantelete Nexo
    public void SpawnUltChargeInward(Vector3 centerPos, float duration)
    {
        StartCoroutine(ChargeInwardRoutine(centerPos, duration));
    }

    IEnumerator ChargeInwardRoutine(Vector3 centerPos, float duration)
    {
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
                sr.sortingOrder = 25;

                ChargeParticle cp = orb.AddComponent<ChargeParticle>();
                cp.Init(centerPos, 0.22f);
            }
            elapsed += 0.06f;
            yield return new WaitForSeconds(0.06f);
        }
    }

    // Fase 2 de la Ulti: Rayo Láser Gigante de Triple Capa que cruza toda la pantalla
    public void SpawnUltimateMegaBeam(Vector3 originPos, float length, float duration)
    {
        ultFlashAlpha = 0.75f; // Destello inicial al disparar el cañón
        StartCoroutine(MegaBeamRoutine(originPos, length, duration));
    }

    IEnumerator MegaBeamRoutine(Vector3 originPos, float length, float duration)
    {
        // Crear contenedor del Mega Rayo
        GameObject beamRoot = new GameObject("NexoMegaBeam");
        beamRoot.transform.position = originPos;

        // Capa 1: Aura Exterior Azul/Dorada
        SpriteRenderer outerBeam = CreateBeamLayer(beamRoot.transform, "OuterGlow", new Color(0.1f, 0.65f, 1f, 0.55f), 20);
        // Capa 2: Cuerpo Principal Cian Nexo
        SpriteRenderer midBeam = CreateBeamLayer(beamRoot.transform, "MidCyan", new Color(0.22f, 0.95f, 1f, 0.90f), 21);
        // Capa 3: Núcleo Incandescente Blanco
        SpriteRenderer coreBeam = CreateBeamLayer(beamRoot.transform, "CoreWhite", new Color(1f, 1f, 1f, 1f), 22);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // El rayo vibra y pulsa con enorme potencia
            float pulse = 1f + Mathf.Sin(Time.time * 55f) * 0.25f;
            float fadeOut = (elapsed > duration * 0.75f) ? Mathf.InverseLerp(duration, duration * 0.75f, elapsed) : 1f;
            float currentThickness = pulse * fadeOut;

            outerBeam.transform.localScale = new Vector3(length, 1.85f * currentThickness, 1f);
            outerBeam.transform.localPosition = new Vector3(length / 2f, 0f, 0f);

            midBeam.transform.localScale = new Vector3(length, 1.05f * currentThickness, 1f);
            midBeam.transform.localPosition = new Vector3(length / 2f, 0f, 0f);

            coreBeam.transform.localScale = new Vector3(length, 0.45f * currentThickness, 1f);
            coreBeam.transform.localPosition = new Vector3(length / 2f, 0f, 0f);

            // Lanzar chispas de plasma a lo largo del rayo
            if (Random.value > 0.3f)
            {
                Vector3 sparkPos = originPos + new Vector3(Random.Range(0.5f, length * 0.85f), Random.Range(-0.5f, 0.5f), 0f);
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

    // Chispas de impacto cuando Kael golpea enemigos o estrella el piso
    public void SpawnHitSparks(Vector3 pos, bool isHeavy, int count = 6)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject sp = new GameObject("HitSpark");
            sp.transform.position = pos;
            SpriteRenderer sr = sp.AddComponent<SpriteRenderer>();
            sr.sprite = sparkSprite;
            sr.color = (i % 2 == 0) ? new Color(0.22f, 0.92f, 1f) : new Color(1f, 0.85f, 0.2f);
            sr.sortingOrder = 23;

            SparkParticle p = sp.AddComponent<SparkParticle>();
            p.Init(isHeavy);
        }
    }

    // --- ALERTAS DE JEFE Y TEXTOS FLOTANTES ---
    public void TriggerBossWarning(string title, string subtitle, float duration = 2.6f)
    {
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
            KaelAutoController kael = FindFirstObjectByType<KaelAutoController>();
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

    void OnGUI()
    {
        // 1. Contador de Combo en pantalla (Arriba a la izquierda debajo del HUD)
        if (comboHits >= 2)
        {
            GUIStyle comboStyle = new GUIStyle(GUI.skin.label);
            comboStyle.fontSize = Mathf.Clamp(20 + (comboHits / 2), 20, 34);
            comboStyle.fontStyle = FontStyle.Bold;
            comboStyle.normal.textColor = (comboHits >= 10) ? new Color(1f, 0.82f, 0.2f) : new Color(0.22f, 0.92f, 1f);

            GUI.color = Color.black;
            GUI.Label(new Rect(27, 122, 350, 45), $"⚡ COMBO x{comboHits} GOLPES!", comboStyle);
            GUI.color = Color.white;
            GUI.Label(new Rect(25, 120, 350, 45), $"⚡ COMBO x{comboHits} GOLPES!", comboStyle);
        }

        // 2. Bandas Cinematográficas (Letterbox) y Cartel durante la Ultimate
        if (ultCinematicActive)
        {
            // Oscurecer ligeramente el fondo para que el rayo láser brille al máximo
            GUI.color = new Color(0.02f, 0.08f, 0.18f, 0.45f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // Franjas negras de cine arriba y abajo
            float barH = Screen.height * 0.12f;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, barH), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - barH, Screen.width, barH), Texture2D.whiteTexture);

            // Línea neón cian en el borde de la franja
            GUI.color = new Color(0.22f, 0.92f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(0, barH - 3f, Screen.width, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - barH, Screen.width, 3f), Texture2D.whiteTexture);

            GUIStyle ultStyle = new GUIStyle(GUI.skin.label);
            ultStyle.alignment = TextAnchor.MiddleCenter;
            ultStyle.fontSize = 28;
            ultStyle.fontStyle = FontStyle.Bold;
            ultStyle.normal.textColor = new Color(0.22f, 0.95f, 1f);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, barH * 0.2f, Screen.width, barH * 0.6f), "★ SOBRECARGA NEXO: CAÑÓN DE PLASMA ★", ultStyle);
        }

        // 3. Flashazo de impacto de la Ultimate
        if (ultFlashAlpha > 0f)
        {
            GUI.color = new Color(0.55f, 0.95f, 1f, ultFlashAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // 4. Alerta Roja de Jefe
        if (showWarning)
        {
            float pulse = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f));

            GUI.color = new Color(0.85f, 0.05f, 0.15f, pulse * 0.24f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            float bannerH = 86f;
            float bannerY = (Screen.height * 0.34f) - (bannerH / 2f);

            GUI.color = new Color(0.08f, 0.02f, 0.05f, 0.92f);
            GUI.DrawTexture(new Rect(0, bannerY, Screen.width, bannerH), Texture2D.whiteTexture);

            GUI.color = new Color(1f, 0.15f, 0.25f, 1f);
            GUI.DrawTexture(new Rect(0, bannerY, Screen.width, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, bannerY + bannerH - 3f, Screen.width, 3f), Texture2D.whiteTexture);

            GUIStyle warnStyle = new GUIStyle(GUI.skin.label);
            warnStyle.alignment = TextAnchor.MiddleCenter;
            warnStyle.fontSize = 26;
            warnStyle.fontStyle = FontStyle.Bold;
            warnStyle.normal.textColor = new Color(1f, 0.25f, 0.3f, 0.5f + pulse * 0.5f);

            GUI.color = Color.white;
            GUI.Label(new Rect(0, bannerY + 8, Screen.width, 40), warningTitle, warnStyle);

            warnStyle.fontSize = 17;
            warnStyle.normal.textColor = new Color(1f, 0.85f, 0.3f, 1f);
            GUI.Label(new Rect(0, bannerY + 46, Screen.width, 30), warningSubtitle, warnStyle);
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

// Partícula de carga que viaja hacia adentro del Guantelete durante la Fase 1 de la Ultimate
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
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        transform.position = Vector3.Lerp(startPos, targetPos, t * t);
        if (t >= 1f) Destroy(gameObject);
    }
}

// Chispas de impacto en golpes y explosiones
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
        elapsed += Time.deltaTime;
        velocity.y -= 12f * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
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
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * 1.25f * Time.deltaTime;
        if (elapsed >= duration) Destroy(gameObject);
    }

    void OnGUI()
    {
        if (Camera.main == null) return;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z <= 0) return;

        float guiY = Screen.height - screenPos.y;
        float alpha = Mathf.Clamp01(1f - (elapsed / duration));

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        Rect rect = new Rect(screenPos.x - 110, guiY - 20, 220, 40);

        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);

        GUI.color = new Color(color.r, color.g, color.b, alpha);
        GUI.Label(rect, text, style);
        GUI.color = Color.white;
    }
}