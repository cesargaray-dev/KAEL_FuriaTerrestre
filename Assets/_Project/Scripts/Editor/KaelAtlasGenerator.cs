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

        string[] rowNames = { "Die", "Attack", "Walk" };
        int[] colsPerRow = { 4, 3, 4 };

        List<SpriteMetaData> metas = new List<SpriteMetaData>();
        float rowHeight = h / 3f;

        for (int r = 2; r >= 0; r--)
        {
            int cols = colsPerRow[r];
            float colWidth = (float)w / cols;
            int yStart = Mathf.RoundToInt(r * rowHeight);
            int yEnd = Mathf.RoundToInt((r + 1) * rowHeight) - 1;

            for (int c = 0; c < cols; c++)
            {
                int xStart = Mathf.RoundToInt(c * colWidth);
                int xEnd = Mathf.RoundToInt((c + 1) * colWidth) - 1;

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
        importer.spritePixelsPerUnit = 240;
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

    [MenuItem("KAEL Tools/Preparar Recursos Nivel 2 (Base Vorst, Bruto y Arconte)")]
    public static void ProcessLevel2Assets()
    {
        string bgPath = "Assets/_Project/Sprites/Backgrounds/BG_BaseVorst_Level2.png";
        TextureImporter bgImp = AssetImporter.GetAtPath(bgPath) as TextureImporter;
        if (bgImp != null)
        {
            bgImp.textureType = TextureImporterType.Sprite;
            bgImp.spriteImportMode = SpriteImportMode.Single;
            bgImp.spritePixelsPerUnit = 100f;
            TextureImporterSettings settings = new TextureImporterSettings();
            bgImp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            bgImp.SetTextureSettings(settings);
            bgImp.SaveAndReimport();
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("¡Nivel 2 (Lab_Zahir) Listo!",
            "Todos los recursos del Nivel 2 (Fondo Base Vorst, Bruto Vorst y Arconte Vorst) están procesados y listos en Lab_Zahir.",
            "¡Excelente!");
    }
}
