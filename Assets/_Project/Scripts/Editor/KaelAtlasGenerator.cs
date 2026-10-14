using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class KaelAtlasGenerator
{
    [MenuItem("KAEL Tools/Limpiar y Recortar los 29 Cuadros de Kael (Perfecto)")]
    public static void CleanAndAutoSliceKael()
    {
        string path = "Assets/_Project/Sprites/Characters/Kael_Atlas_Pro.png";

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Archivo no encontrado",
                "No se encontró: " + path + "\nVerifica que el nombre sea exactamente Kael_Atlas_Pro.png", "OK");
            return;
        }

        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        Texture2D src = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int w = src.width;
        int h = src.height;
        Color[] pixels = src.GetPixels();

        // 1. Limpieza agresiva de fondo blanco y píxeles fantasma/semitransparentes
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            float brightness = (c.r + c.g + c.b) / 3f;
            float maxDiff = Mathf.Max(Mathf.Abs(c.r - c.g), Mathf.Abs(c.r - c.b), Mathf.Abs(c.g - c.b));

            // Borrar todo lo que sea muy claro/grisáceo o casi transparente
            if (c.a < 0.25f || (brightness > 0.82f && maxDiff < 0.12f))
            {
                pixels[i] = new Color(0, 0, 0, 0);
            }
        }

        Texture2D cleanTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        cleanTex.SetPixels(pixels);
        cleanTex.Apply();
        File.WriteAllBytes(path, cleanTex.EncodeToPNG());

        // 2. Encontrar las 6 filas horizontales buscando dónde hay píxeles opacos
        List<Vector2Int> rowBands = FindActiveBands(pixels, w, h, isHorizontalRows: true, minThickness: 30);

        // Nombres de las filas de abajo hacia arriba (en coordenadas de textura Y=0 es abajo)
        string[] rowNames = { "Fila6_HurtUlt", "Fila5_Atk3", "Fila4_Atk2", "Fila3_Atk1", "Fila2_Run", "Fila1_Idle" };

        List<SpriteMetaData> metas = new List<SpriteMetaData>();

        for (int r = 0; r < rowBands.Count; r++)
        {
            int yMin = rowBands[r].x;
            int yMax = rowBands[r].y;

            // Dentro de cada fila, encontrar cada personaje de izquierda a derecha
            List<Vector2Int> colBands = FindColumnsInRow(pixels, w, yMin, yMax, minWidth: 25, mergeGap: 14);

            string rName = (r < rowNames.Length) ? rowNames[r] : $"Fila_{r}";

            for (int c = 0; c < colBands.Count; c++)
            {
                int xMin = colBands[c].x;
                int xMax = colBands[c].y;

                // Ajustar la altura exacta de este cuadro individual
                int tightYMin = yMax;
                int tightYMax = yMin;
                for (int y = yMin; y <= yMax; y++)
                {
                    for (int x = xMin; x <= xMax; x++)
                    {
                        if (pixels[y * w + x].a > 0.5f)
                        {
                            if (y < tightYMin) tightYMin = y;
                            if (y > tightYMax) tightYMax = y;
                        }
                    }
                }

                if (tightYMax > tightYMin)
                {
                    SpriteMetaData smd = new SpriteMetaData();
                    smd.name = $"Kael_{rName}_{c}";
                    smd.rect = new Rect(xMin, tightYMin, (xMax - xMin) + 1, (tightYMax - tightYMin) + 1);
                    smd.alignment = (int)SpriteAlignment.BottomCenter;
                    smd.pivot = new Vector2(0.5f, 0f);
                    metas.Add(smd);
                }
            }
        }

        // 3. Guardar todos los recortes en el archivo .meta de Unity
        importer.isReadable = false;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 120;
        importer.filterMode = FilterMode.Point;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritesheet = metas.ToArray();

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("¡Recorte Inteligente Completado!",
            $"Se detectaron y recortaron {metas.Count} cuadros individuales de Kael.\n\n" +
            "Mira la parte inferior en tu ventana Project: ahora cada cuadro tiene a un solo Kael perfectamente separado.",
            "¡A verlo!");
    }

    static List<Vector2Int> FindActiveBands(Color[] pixels, int w, int h, bool isHorizontalRows, int minThickness)
    {
        List<Vector2Int> bands = new List<Vector2Int>();
        bool inBand = false;
        int start = 0;

        for (int y = 0; y < h; y++)
        {
            int opaqueCount = 0;
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a > 0.5f) opaqueCount++;
            }

            bool active = opaqueCount > 8;
            if (active && !inBand)
            {
                inBand = true;
                start = y;
            }
            else if (!active && inBand)
            {
                inBand = false;
                if (y - start >= minThickness)
                    bands.Add(new Vector2Int(start, y - 1));
            }
        }
        if (inBand && (h - start) >= minThickness)
            bands.Add(new Vector2Int(start, h - 1));

        return bands;
    }

    static List<Vector2Int> FindColumnsInRow(Color[] pixels, int w, int yMin, int yMax, int minWidth, int mergeGap)
    {
        List<Vector2Int> rawCols = new List<Vector2Int>();
        bool inCol = false;
        int start = 0;

        for (int x = 0; x < w; x++)
        {
            int opaqueCount = 0;
            for (int y = yMin; y <= yMax; y++)
            {
                if (pixels[y * w + x].a > 0.5f) opaqueCount++;
            }

            bool active = opaqueCount > 2;
            if (active && !inCol)
            {
                inCol = true;
                start = x;
            }
            else if (!active && inCol)
            {
                inCol = false;
                rawCols.Add(new Vector2Int(start, x - 1));
            }
        }
        if (inCol) rawCols.Add(new Vector2Int(start, w - 1));

        // Unir chispas cercanas al cuerpo del mismo Kael
        List<Vector2Int> merged = new List<Vector2Int>();
        for (int i = 0; i < rawCols.Count; i++)
        {
            Vector2Int current = rawCols[i];
            while (i + 1 < rawCols.Count && (rawCols[i + 1].x - current.y) <= mergeGap)
            {
                current.y = rawCols[i + 1].y;
                i++;
            }
            if ((current.y - current.x) >= minWidth)
            {
                merged.Add(current);
            }
        }
        return merged;
    }
}