using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public static CameraFollow2D Instance { get; private set; }

    public Transform target;
    public float offsetX = 2.8f;
    public float smoothSpeed = 6.5f;

    private float currentOffsetX;
    private float fixedY;
    private float fixedZ;
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0f;

    void Awake()
    {
        Instance = this;
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
        currentOffsetX = offsetX;
    }

    void Start()
    {
        if (target == null && KaelAutoController.Instance != null)
        {
            target = KaelAutoController.Instance.transform;
        }

        if (target != null)
        {
            transform.position = new Vector3(target.position.x + offsetX, fixedY, fixedZ);
        }
    }

    public void TriggerShake(float duration, float magnitude)
    {
        shakeTimer = Mathf.Max(shakeTimer, duration);
        shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
    }

    void LateUpdate()
    {
        if (target == null)
        {
            if (KaelAutoController.Instance != null)
                target = KaelAutoController.Instance.transform;
            else
                return;
        }

        // Ajustar suavemente el encuadre según si Kael mira/avanza a la derecha o retrocede a la izquierda con A
        float desiredOffset = offsetX;
        if (KaelAutoController.Instance != null && KaelAutoController.Instance.isFacingLeft)
        {
            desiredOffset = offsetX * 0.45f;
        }
        currentOffsetX = Mathf.Lerp(currentOffsetX, desiredOffset, 4.5f * Time.deltaTime);

        float targetX = target.position.x + currentOffsetX;
        float newX = Mathf.Lerp(transform.position.x, targetX, smoothSpeed * Time.deltaTime);

        Vector2 shakeOffset = Vector2.zero;
        if (shakeTimer > 0f && Time.timeScale > 0f)
        {
            shakeOffset = Random.insideUnitCircle * shakeMagnitude;
            shakeTimer -= Time.unscaledDeltaTime;
            if (shakeTimer <= 0f) shakeMagnitude = 0f;
        }

        // La altura base (fixedY) jamás se pierde
        transform.position = new Vector3(newX + shakeOffset.x, fixedY + shakeOffset.y, fixedZ);
    }
}
