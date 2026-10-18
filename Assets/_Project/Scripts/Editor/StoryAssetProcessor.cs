using UnityEngine;
using UnityEditor;
using System.IO;

public class StoryAssetProcessor
{
    [MenuItem("KAEL Tools/1. Forzar Procesamiento de Historia a Resources")]
    public static void ProcessAllStoryAssets()
    {
        string uiDir = "Assets/_Project/Sprites/UI";
        string resDir = "Assets/_Project/Resources/StoryUI";

        if (!Directory.Exists(resDir))
        {
            Directory.CreateDirectory(resDir);
        }

        int successCount = 0;

        // 1. Procesar Menú Principal
        string menuSrc = Path.Combine(uiDir, "Menu_KeyArt.png");
        string menuDst = Path.Combine(resDir, "MenuBG.png");
        if (File.Exists(menuSrc))
        {
            File.Copy(menuSrc, menuDst, true);
            ConfigureSprite(menuDst);
            successCount++;
        }
        else
        {
            Debug.LogError("No se encontró Menu_KeyArt.png en Sprites/UI");
        }

        // 2. Procesar la Cinemática (Intro_Storyboard de 4 paneles)
        string introSrc = Path.Combine(uiDir, "Intro_Storyboard.png");
        Texture2D introTex = LoadReadableTexture(introSrc);
        if (introTex != null)
        {
            int w = introTex.width;
            int h = introTex.height;
            int halfW = w / 2;
            int halfH = h / 2;
            int margin = Mathf.RoundToInt(w * 0.012f);

            SaveTextureAsPNG(CropTexture(introTex, 0, halfH + margin, halfW - margin, halfH - margin), Path.Combine(resDir, "Intro_1.png"));
            SaveTextureAsPNG(CropTexture(introTex, halfW + margin, halfH + margin, halfW - margin, halfH - margin), Path.Combine(resDir, "Intro_2.png"));
            SaveTextureAsPNG(CropTexture(introTex, 0, 0, halfW - margin, halfH - margin), Path.Combine(resDir, "Intro_3.png"));
            SaveTextureAsPNG(CropTexture(introTex, halfW + margin, 0, halfW - margin, halfH - margin), Path.Combine(resDir, "Intro_4.png"));
            successCount += 4;
        }
        else
        {
            Debug.LogError("No se encontró Intro_Storyboard.png en Sprites/UI");
        }

        // 3. Procesar los Retratos de Diálogo
        string portSrc = Path.Combine(uiDir, "Dialogue_Portraits.png");
        Texture2D portTex = LoadReadableTexture(portSrc);
        if (portTex != null)
        {
            CleanWhiteBackground(portTex);
            int pw = portTex.width / 4;
            int ph = portTex.height;
            string[] names = { "Portrait_Kael_Normal.png", "Portrait_Kael_Shout.png", "Portrait_Nexa_Normal.png", "Portrait_Nexa_Alert.png" };

            for (int i = 0; i < 4; i++)
            {
                Texture2D sub = CropTexture(portTex, i * pw, 0, pw, ph);
                SaveTextureAsPNG(sub, Path.Combine(resDir, names[i]));
            }
            successCount += 4;
        }
        else
        {
            Debug.LogError("No se encontró Dialogue_Portraits.png en Sprites/UI");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("¡Proceso Completado!", 
            $"Se generaron y guardaron {successCount} archivos listos en:\nAssets/_Project/Resources/StoryUI\n\n¡Ya puedes dar Play!", "¡Perfecto!");
    }

    static Texture2D LoadReadableTexture(string path)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return null;
        imp.isReadable = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void CleanWhiteBackground(Texture2D tex)
    {
        Color[] px = tex.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            Color c = px[i];
            float minChannel = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            float maxDiff = Mathf.Max(Mathf.Abs(c.r - c.g), Mathf.Abs(c.r - c.b), Mathf.Abs(c.g - c.b));
            if (minChannel > 0.88f && maxDiff < 0.08f)
            {
                px[i] = new Color(0, 0, 0, 0);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
    }

    static Texture2D CropTexture(Texture2D src, int x, int y, int w, int h)
    {
        x = Mathf.Clamp(x, 0, src.width - 1);
        y = Mathf.Clamp(y, 0, src.height - 1);
        w = Mathf.Clamp(w, 1, src.width - x);
        h = Mathf.Clamp(h, 1, src.height - y);

        Color[] block = src.GetPixels(x, y, w, h);
        Texture2D dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
        dst.SetPixels(block);
        dst.Apply();
        return dst;
    }

    static void SaveTextureAsPNG(Texture2D tex, string path)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        ConfigureSprite(path);
    }

    static void ConfigureSprite(string path)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
    }
}