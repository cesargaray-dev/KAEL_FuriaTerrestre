using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class KaelAtlasGenerator
{
    [MenuItem("KAEL Tools/Limpiar y Recortar Caminante Vorst (11 Cuadros)")]
    public static void CleanAndSliceVorstWalker()
    {
        string path = "Assets/_Project/Sprites/Characters/Vorst_Walker_Atlas.png";

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Error", "Guarda la imagen como:\n" + path, "OK");
            return;
        }

        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        Texture2D src = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int w = src.width;
        int h = src.height;
        Color[] pixels = src.GetPixels();

        // 1. Quitar fondo blanco
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            float brightness = (c.r + c.g + c.b) / 3f;
            float maxDiff = Mathf.Max(Mathf.Abs(c.r - c.g), Mathf.Abs(c.r - c.b), Mathf.Abs(c.g - c.b));
            if (c.a < 0.2f || (brightness > 0.85f && maxDiff < 0.10f))
            {
                pixels[i] = new Color(0, 0, 0, 0);
            }
        }

        Texture2D cleanTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        cleanTex.SetPixels(pixels);
        cleanTex.Apply();
        File.WriteAllBytes(path, cleanTex.EncodeToPNG());

        // 2. Dividir exactamente en 3 filas y sus respectivas columnas (4, 3, 4)
        // En coordenadas de Unity Y=0 es abajo (Fila 3: Muerte), Y=arriba es Fila 1: Caminar
        string[] rowNames = { "Die", "Attack", "Walk" };
        int[] colsPerRow = { 4, 3, 4 };

        List<SpriteMetaData> metas = new List<SpriteMetaData>();
        float rowHeight = h / 3f;

        for (int r = 2; r >= 0; r--) // De arriba (Walk) hacia abajo (Die)
        {
            int cols = colsPerRow[r];
            float colWidth = (float)w / cols;
            int yStart = Mathf.RoundToInt(r * rowHeight);
            int yEnd = Mathf.RoundToInt((r + 1) * rowHeight) - 1;

            for (int c = 0; c < cols; c++)
            {
                int xStart = Mathf.RoundToInt(c * colWidth);
                int xEnd = Mathf.RoundToInt((c + 1) * colWidth) - 1;

                // Ajustar la caja a los píxeles reales dentro de esta celda (incluyendo chispas de explosión)
                int minX = xEnd, maxX = xStart, minY = yEnd, maxY = yStart;
                bool hasPixels = false;

                for (int y = yStart; y <= yEnd; y++)
                {
                    for (int x = xStart; x <= xEnd; x++)
                    {
                        if (pixels[y * w + x].a > 0.25f)
                        {
                            hasPixels = true;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                if (hasPixels)
                {
                    SpriteMetaData smd = new SpriteMetaData();
                    smd.name = $"VorstWalker_{rowNames[r]}_{c}";
                    smd.rect = new Rect(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
                    smd.alignment = (int)SpriteAlignment.BottomCenter;
                    smd.pivot = new Vector2(0.5f, 0f);
                    metas.Add(smd);
                }
            }
        }

        importer.isReadable = false;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 240; // Escala para que llegue a la cintura/pecho de Kael
        importer.filterMode = FilterMode.Point;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritesheet = metas.ToArray();

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("¡Caminante Vorst Listo!",
            $"Se limpiaron y recortaron exactamente los {metas.Count} cuadros (4 Walk, 3 Attack, 4 Die) sin separar las partículas de muerte.",
            "¡Excelente!");
    }
}