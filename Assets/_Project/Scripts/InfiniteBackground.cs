using UnityEngine;
using UnityEngine.SceneManagement;

public class InfiniteBackground : MonoBehaviour
{
    public Transform targetCamera;
    private float singleTileWidth;

    void Start()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;

        string sceneName = SceneManager.GetActiveScene().name;
        bool isLevel3 = (sceneName == "03_Nivel3_Colmena") ||
                        (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 3);
        bool isLevel2 = !isLevel3 && ((sceneName == "Lab_Zahir") ||
                        (PlayerStats.Instance != null && PlayerStats.Instance.levelNumber == 2));

        if ((isLevel2 || isLevel3) && Camera.main != null)
        {
            Camera.main.backgroundColor = isLevel3
                ? new Color(0.04f, 0.015f, 0.09f, 1f)
                : new Color(0.05f, 0.02f, 0.12f, 1f);
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (isLevel3 || isLevel2 || sr.sprite == null)
            {
                string bgPath = isLevel3 ? "Level3/BG_Hive_Level3" : "Level2/BG_BaseVorst_Level2";
                Texture2D bgTex = Resources.Load<Texture2D>(bgPath);
                if (bgTex != null)
                {
                    bgTex.wrapMode = TextureWrapMode.Repeat;
                    bgTex.filterMode = FilterMode.Bilinear;
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

    void LateUpdate()
    {
        if (targetCamera == null || singleTileWidth <= 0f) return;

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
