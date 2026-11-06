using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generador y cargador de Sprites HD para los monstruos biomecánicos exclusivos del Nivel 3
/// (BioStalker, BioLasher, BioHulk, HiveInquisitor, VortexOmega), además del Escudo Hexagonal
/// Holográfico de Parry Nexo, Anillos Psiónicos y Cápsulas de Estasis.
/// </summary>
public static class Level3MonsterAssetGenerator
{
    private static readonly Dictionary<string, Sprite[]> spriteCache = new Dictionary<string, Sprite[]>();
    private static Sprite cachedHexShieldCyan;
    private static Sprite cachedHexShieldGold;
    private static Sprite cachedHexShieldCorrupt;
    private static Sprite cachedPsionicRing;
    private static Sprite cachedBioCableSegment;
    private static Sprite cachedStasisPod;
    private static Sprite cachedEnergyBarrier;

    /// <summary>
    /// Carga los 8 fotogramas de animación desde Resources/Level3/ o genera un fallback procedural nítido.
    /// </summary>
    public static Sprite[] GetMonsterAnimationFrames(string prefix, string state, float pixelsPerUnit = 78f)
    {
        string key = $"{prefix}_{state}_{pixelsPerUnit:F0}";
        if (spriteCache.TryGetValue(key, out Sprite[] cached) && cached != null && cached.Length > 0)
            return cached;

        List<Sprite> list = new List<Sprite>();
        for (int i = 0; i < 8; i++)
        {
            string resPath = $"Level3/{prefix}_{state}_{i}";
            Texture2D tex = Resources.Load<Texture2D>(resPath);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                Sprite sp = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.03f),
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect
                );
                list.Add(sp);
            }
        }

        if (list.Count == 0)
        {
            for (int i = 0; i < 8; i++)
            {
                list.Add(GenerateProceduralMonsterFrame(prefix, state, i, pixelsPerUnit));
            }
        }

        Sprite[] result = list.ToArray();
        spriteCache[key] = result;
        return result;
    }

    /// <summary>
    /// Escudo Hexagonal Holográfico de alta nitidez frente a Kael al mantener S (Guardia / Parry Nexo).
    /// Cambia su matriz cromática según la evolución moral (Cian Nexo, Dorado Solar o Carmesí/Púrpura Disonante).
    /// </summary>
    public static Sprite GetHexShieldSprite(bool isPureza, bool isCorrupcion)
    {
        if (isCorrupcion && cachedHexShieldCorrupt != null) return cachedHexShieldCorrupt;
        if (isPureza && cachedHexShieldGold != null) return cachedHexShieldGold;
        if (!isPureza && !isCorrupcion && cachedHexShieldCyan != null) return cachedHexShieldCyan;

        int size = 160;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color primary = isCorrupcion
            ? new Color(1f, 0.16f, 0.42f, 1f)
            : (isPureza ? new Color(0.35f, 0.98f, 1f, 1f) : new Color(0.15f, 0.92f, 1f, 1f));
        Color secondary = isCorrupcion
            ? new Color(0.68f, 0.12f, 1f, 1f)
            : (isPureza ? new Color(1f, 0.88f, 0.35f, 1f) : new Color(0.10f, 0.55f, 1f, 1f));

        float cx = size * 0.5f;
        float cy = size * 0.5f;
        float maxR = size * 0.44f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                // Distancia hexagonal (norma de hexágono regular)
                float ax = Mathf.Abs(dx);
                float ay = Mathf.Abs(dy);
                float hexDist = Mathf.Max(ax * 0.8660254f + ay * 0.5f, ay);

                if (hexDist > maxR + 4f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float norm = hexDist / maxR;
                // Borde exterior hexagonal brillante + sub-rejilla de panal interior
                float outerEdge = Mathf.Clamp01(1f - Mathf.Abs(norm - 0.95f) * 18f);
                float innerRing = Mathf.Clamp01(1f - Mathf.Abs(norm - 0.62f) * 22f) * 0.75f;
                float coreDot = Mathf.Clamp01(1f - norm * 4.5f) * 0.85f;

                // Patrón de celdas hexagonales internas
                float gridX = Mathf.Abs(((x % 20) - 10f) / 10f);
                float gridY = Mathf.Abs(((y % 20) - 10f) / 10f);
                float honeycomb = (gridX > 0.78f || gridY > 0.78f) ? 0.38f : 0.12f;

                float alpha = Mathf.Clamp01(outerEdge + innerRing + coreDot + honeycomb * (1f - norm * 0.35f));
                if (norm > 1f) alpha *= Mathf.Clamp01(1f - (norm - 1f) * 12f);

                Color c = Color.Lerp(secondary, primary, outerEdge + coreDot);
                if (outerEdge > 0.65f) c = Color.Lerp(c, Color.white, 0.55f);
                c.a = alpha * 0.88f;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        if (isCorrupcion) cachedHexShieldCorrupt = sp;
        else if (isPureza) cachedHexShieldGold = sp;
        else cachedHexShieldCyan = sp;
        return sp;
    }

    /// <summary>
    /// Anillo Psiónico rotatorio para el Mini-Jefe de Oleada 5 (HiveInquisitor).
    /// </summary>
    public static Sprite GetPsionicRingSprite()
    {
        if (cachedPsionicRing != null) return cachedPsionicRing;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = size * 0.5f;
        float cy = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / (size * 0.45f);
                float dy = (y - cy) / (size * 0.20f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(dist - 1f) * 9f);
                if (ring <= 0.01f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float angle = Mathf.Atan2(dy, dx);
                    float rune = 0.65f + 0.35f * Mathf.Sin(angle * 6f);
                    Color c = Color.Lerp(new Color(0.75f, 0.20f, 1f, 1f), new Color(0.15f, 0.95f, 1f, 1f), rune);
                    c.a = ring * 0.85f;
                    tex.SetPixel(x, y, c);
                }
            }
        }
        tex.Apply();
        cachedPsionicRing = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedPsionicRing;
    }

    /// <summary>
    /// Bio-cable orgánico-metálico con venas bioluminiscentes para las capas de paralaje del Nivel 3.
    /// </summary>
    public static Sprite GetBioCableSprite()
    {
        if (cachedBioCableSegment != null) return cachedBioCableSegment;

        int w = 256, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float waveY = h * 0.5f + Mathf.Sin((x / (float)w) * Mathf.PI * 2f) * 14f;
                float dist = Mathf.Abs(y - waveY);
                if (dist > 14f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dist < 3.2f)
                {
                    // Vena de plasma central pulsante
                    bool node = (x % 64) < 12;
                    Color vein = node ? new Color(1f, 0.25f, 0.72f, 0.95f) : new Color(0.10f, 0.95f, 1f, 0.92f);
                    tex.SetPixel(x, y, vein);
                }
                else
                {
                    // Cubierta quitinosa biomecánica
                    float shade = 1f - (dist / 14f);
                    bool rib = (x % 16) < 4;
                    Color shell = rib
                        ? new Color(0.28f * shade, 0.11f * shade, 0.45f * shade, 0.92f)
                        : new Color(0.16f * shade, 0.06f * shade, 0.28f * shade, 0.92f);
                    tex.SetPixel(x, y, shell);
                }
            }
        }
        tex.Apply();
        cachedBioCableSegment = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
        return cachedBioCableSegment;
    }

    /// <summary>
    /// Cápsula de estasis con silueta humana prisionera para la capa media del Nivel 3.
    /// </summary>
    public static Sprite GetStasisPodSprite()
    {
        if (cachedStasisPod != null) return cachedStasisPod;

        int w = 96, h = 192;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x < 10 || x > w - 11 || y < 8 || y > h - 9)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Tapas metálicas superior e inferior
                if (y < 28 || y > h - 28)
                {
                    bool trim = (y == 27 || y == h - 28 || x == 12 || x == w - 13);
                    tex.SetPixel(x, y, trim ? new Color(0.65f, 0.30f, 0.95f, 1f) : new Color(0.18f, 0.08f, 0.32f, 1f));
                    continue;
                }

                // Tubo de cristal criogénico
                bool border = (x <= 13 || x >= w - 14);
                if (border)
                {
                    tex.SetPixel(x, y, new Color(0.25f, 0.95f, 1f, 0.95f));
                    continue;
                }

                // Silueta humana suspendida en el centro
                float cx = w * 0.5f;
                float headDist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, h - 56f));
                bool isHead = headDist < 10f;
                bool isTorso = (y >= h - 102 && y <= h - 66 && Mathf.Abs(x - cx) < 13f - (h - 66 - y) * 0.12f);
                bool isLegs = (y >= h - 142 && y < h - 102 && (Mathf.Abs(x - (cx - 5f)) < 4f || Mathf.Abs(x - (cx + 5f)) < 4f));

                if (isHead || isTorso || isLegs)
                {
                    bool visor = isHead && Mathf.Abs(y - (h - 55f)) < 2.2f && Mathf.Abs(x - cx) < 6f;
                    tex.SetPixel(x, y, visor ? new Color(1f, 0.90f, 0.30f, 1f) : new Color(0.05f, 0.12f, 0.24f, 0.92f));
                }
                else
                {
                    float glow = 1f - Mathf.Abs(x - cx) / (w * 0.4f);
                    tex.SetPixel(x, y, new Color(0.06f, 0.58f + glow * 0.25f, 0.78f + glow * 0.18f, 0.55f));
                }
            }
        }

        tex.Apply();
        cachedStasisPod = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
        return cachedStasisPod;
    }

    /// <summary>
    /// Barrera de Energía Vorst destructible para el tramo jugable del Puente Nivel 1 -> Nivel 2.
    /// </summary>
    public static Sprite GetVorstEnergyBarrierSprite()
    {
        if (cachedEnergyBarrier != null) return cachedEnergyBarrier;

        int w = 96, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float cx = w * 0.5f;
                float dist = Mathf.Abs(x - cx);
                if (dist > 38f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Pilares emisor inferior y superior
                if (y < 28 || y > h - 28)
                {
                    tex.SetPixel(x, y, dist < 30f ? new Color(0.22f, 0.08f, 0.36f, 1f) : new Color(1f, 0.25f, 0.55f, 1f));
                    continue;
                }

                // Haz de contención Vorst con bandas de plasma
                bool core = dist < 10f;
                bool band = ((y + x) % 24) < 6;
                Color c = core
                    ? new Color(1f, 0.85f, 0.95f, 0.95f)
                    : (band ? new Color(1f, 0.18f, 0.48f, 0.88f) : new Color(0.62f, 0.10f, 0.95f, 0.72f));
                c.a *= Mathf.Clamp01(1f - (dist / 38f) * 0.5f);
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        cachedEnergyBarrier = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 64f);
        return cachedEnergyBarrier;
    }

    private static Sprite GenerateProceduralMonsterFrame(string prefix, string state, int frame, float ppu)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color mainCol = prefix.Contains("Vortex") ? new Color(1f, 0.2f, 0.45f, 1f) : new Color(0.65f, 0.22f, 0.95f, 1f);
        Color glowCol = new Color(0f, 0.95f, 1f, 1f);

        float cx = size * 0.5f;
        float cy = size * 0.5f + Mathf.Sin(frame * 0.8f) * 4f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                if (d > 44f) tex.SetPixel(x, y, Color.clear);
                else if (d < 12f) tex.SetPixel(x, y, glowCol);
                else tex.SetPixel(x, y, mainCol);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.05f), ppu);
    }
}
