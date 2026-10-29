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

        bool isLevel2 = SceneManager.GetActiveScene().name == "Lab_Zahir";
        if (isLevel2 && Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.05f, 0.02f, 0.12f, 1f);
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // En Nivel 2 (o si falta el sprite en el prefab/escena), cargamos la textura simétrica sin costuras (2048x1024)
            if (isLevel2 || sr.sprite == null)
            {
                Texture2D bgTex = Resources.Load<Texture2D>("Level2/BG_BaseVorst_Level2");
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
                // Calcula el ancho exacto de 1 ciclo completo sin costuras
                singleTileWidth = sr.sprite.bounds.size.x;

                // Repite el escenario 9 veces a lo ancho en modo FullRect Tiled para un bucle infinito 100% continuo
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
