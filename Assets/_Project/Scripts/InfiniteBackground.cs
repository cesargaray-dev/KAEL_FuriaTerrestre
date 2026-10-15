using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    public Transform targetCamera;
    private float singleTileWidth;

    void Start()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            // Calcula el ancho exacto de 1 sola imagen del escenario (ej. 17.92 unidades)
            singleTileWidth = sr.sprite.bounds.size.x;

            // Repite el escenario 5 veces a lo ancho para cubrir toda la vista de sobra
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(singleTileWidth * 5f, sr.sprite.bounds.size.y);
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null || singleTileWidth <= 0f) return;

        float dist = targetCamera.position.x - transform.position.x;

        // Cuando la cámara avanza exactamente 1 bloque completo, recorremos el fondo 1 bloque exacto
        // Como se mueve exactamente la medida de 1 imagen, el ojo humano no nota ningún salto
        if (dist >= singleTileWidth)
        {
            transform.position += new Vector3(singleTileWidth, 0f, 0f);
        }
        else if (dist <= -singleTileWidth)
        {
            transform.position -= new Vector3(singleTileWidth, 0f, 0f);
        }
    }
}