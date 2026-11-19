using UnityEngine;
using UnityEngine.SceneManagement;

public class InfiniteBackground : MonoBehaviour
{
    public static InfiniteBackground Instance { get; private set; }

    public Transform targetCamera;
    private float singleTileWidth;

    // Nivel 4: corredor sin fin sin duplicar a Lía + santuario único del Trono para la Oleada 10
    private bool isLevel4Mode = false;
    private readonly System.Collections.Generic.List<Transform> level4CorridorTiles = new System.Collections.Generic.List<Transform>();
    private float level4ModuleWidth = 10.24f;
    private GameObject level4SanctumObj;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;

        string sceneName = SceneManager.GetActiveScene().name;
        bool isLevel4 = (sceneName == "04_Final_NexusThroneroom") ||
                        (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 4);
        bool isLevel3 = !isLevel4 && ((sceneName == "03_Nivel3_Colmena") ||
                        (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 3));
        bool isLevel2 = !isLevel4 && !isLevel3 && ((sceneName == "Lab_Zahir") ||
                        (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 2));

        if ((isLevel2 || isLevel3 || isLevel4) && Camera.main != null)
        {
            Camera.main.backgroundColor = isLevel4
                ? new Color(0.03f, 0.015f, 0.08f, 1f)
                : (isLevel3
                    ? new Color(0.04f, 0.015f, 0.09f, 1f)
                    : new Color(0.05f, 0.02f, 0.12f, 1f));
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (isLevel4)
            {
                SetupLevel4CorridorAndSanctum(sr);
                return;
            }

            if (isLevel3 || isLevel2 || sr.sprite == null)
            {
                string bgPath = isLevel3 ? "Level3/BG_Hive_Level3" : "Level2/BG_BaseVorst_Level2";
                Texture2D bgTex = Resources.Load<Texture2D>(bgPath);
                if (bgTex != null)
                {
                    bgTex.wrapMode = TextureWrapMode.Repeat;
                    bgTex.filterMode = FilterMode.Point;
                    sr.sprite = Sprite.Create(
                        bgTex,
                        new Rect(0f, 0f, bgTex.width, bgTex.height),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect
                    );
                }
            }

            if (sr.sprite != null)
            {
                singleTileWidth = sr.sprite.bounds.size.x;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(singleTileWidth * 9f, sr.sprite.bounds.size.y);
            }
        }
    }

    void SetupLevel4CorridorAndSanctum(SpriteRenderer rootSr)
    {
        isLevel4Mode = true;
        Texture2D bgTex = Resources.Load<Texture2D>("Level4/BG_Throneroom_Level4");
        if (bgTex == null) return;

        bgTex.wrapMode = TextureWrapMode.Clamp;
        bgTex.filterMode = FilterMode.Point;

        // Desactivar el Tiled del sprite completo para que no repita la cápsula de Lía en bucle
        rootSr.enabled = false;

        float wingPx = Mathf.Min(512f, bgTex.width * 0.25f);
        float wingWorld = wingPx / 100f;
        level4ModuleWidth = wingWorld * 2f; // 10.24 unidades por módulo de corredor

        Sprite leftWingSpr = Sprite.Create(
            bgTex,
            new Rect(0f, 0f, wingPx, bgTex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );
        Sprite rightWingSpr = Sprite.Create(
            bgTex,
            new Rect(bgTex.width - wingPx, 0f, wingPx, bgTex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );

        float camX = (targetCamera != null) ? targetCamera.position.x : 0f;
        float baseTileX = Mathf.Round(camX / level4ModuleWidth) * level4ModuleWidth;

        // 7 módulos cubren >70 unidades alrededor de la cámara sin costuras ni cápsulas duplicadas
        for (int i = -3; i <= 3; i++)
        {
            GameObject mod = new GameObject($"Level4CorridorModule_{i + 3}");
            mod.transform.SetParent(transform, false);
            mod.transform.position = new Vector3(baseTileX + i * level4ModuleWidth, 0f, 0f);

            GameObject leftObj = new GameObject("LeftWing");
            leftObj.transform.SetParent(mod.transform, false);
            leftObj.transform.localPosition = new Vector3(-wingWorld * 0.5f, 0f, 0f);
            SpriteRenderer lsr = leftObj.AddComponent<SpriteRenderer>();
            lsr.sprite = leftWingSpr;
            lsr.sortingOrder = -10;

            GameObject rightObj = new GameObject("RightWing");
            rightObj.transform.SetParent(mod.transform, false);
            rightObj.transform.localPosition = new Vector3(wingWorld * 0.5f, 0f, 0f);
            SpriteRenderer rsr = rightObj.AddComponent<SpriteRenderer>();
            rsr.sprite = rightWingSpr;
            rsr.sortingOrder = -10;

            level4CorridorTiles.Add(mod.transform);
        }

        // Santuario único del Trono (con ventana espacial y Cápsula de Lía): se activa en la batalla del Soberano
        level4SanctumObj = new GameObject("Level4UniqueSanctumCenterpiece");
        level4SanctumObj.transform.SetParent(transform, false);
        SpriteRenderer sanctumSr = level4SanctumObj.AddComponent<SpriteRenderer>();
        sanctumSr.sprite = Sprite.Create(
            bgTex,
            new Rect(0f, 0f, bgTex.width, bgTex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );
        sanctumSr.sortingOrder = -9;
        level4SanctumObj.SetActive(false);
    }

    public void ActivateLevel4SanctumAt(float worldCenterX)
    {
        if (!isLevel4Mode || level4SanctumObj == null) return;
        level4SanctumObj.transform.position = new Vector3(worldCenterX, 0f, 0f);
        level4SanctumObj.SetActive(true);
    }

    public void ResetLevel4Sanctum()
    {
        if (level4SanctumObj != null)
        {
            level4SanctumObj.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        if (isLevel4Mode)
        {
            if (level4CorridorTiles.Count == 0 || level4ModuleWidth <= 0f) return;
            float halfSpan = level4ModuleWidth * (level4CorridorTiles.Count * 0.5f);
            float camX = targetCamera.position.x;
            for (int i = 0; i < level4CorridorTiles.Count; i++)
            {
                Transform mod = level4CorridorTiles[i];
                if (mod == null) continue;
                float dx = mod.position.x - camX;
                if (dx < -halfSpan)
                {
                    mod.position += new Vector3(level4ModuleWidth * level4CorridorTiles.Count, 0f, 0f);
                }
                else if (dx > halfSpan)
                {
                    mod.position -= new Vector3(level4ModuleWidth * level4CorridorTiles.Count, 0f, 0f);
                }
            }
            return;
        }

        if (singleTileWidth <= 0f) return;

        float dist = targetCamera.position.x - transform.position.x;

        while (dist >= singleTileWidth)
        {
            transform.position += new Vector3(singleTileWidth, 0f, 0f);
            dist = targetCamera.position.x - transform.position.x;
        }
        while (dist <= -singleTileWidth)
        {
            transform.position -= new Vector3(singleTileWidth, 0f, 0f);
            dist = targetCamera.position.x - transform.position.x;
        }
    }
}
