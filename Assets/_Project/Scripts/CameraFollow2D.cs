using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public static CameraFollow2D Instance { get; private set; }

    public Transform target;
    public float offsetX = 3.2f;
    public float smoothSpeed = 6f;

    private float fixedY;
    private float fixedZ;
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0f;

    void Awake()
    {
        Instance = this;
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
    }

    void Start()
    {
        // Alinear la cámara instantáneamente al inicio para que no se mueva sola al dar Play
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
        if (target == null) return;

        float targetX = target.position.x + offsetX;
        float newX = Mathf.Lerp(transform.position.x, targetX, smoothSpeed * Time.deltaTime);

        Vector2 shakeOffset = Vector2.zero;
        if (shakeTimer > 0f && Time.timeScale > 0f)
        {
            shakeOffset = Random.insideUnitCircle * shakeMagnitude;
            shakeTimer -= Time.unscaledDeltaTime;
            if (shakeTimer <= 0f) shakeMagnitude = 0f;
        }

        // La altura base (fixedY) jamás se pierde, evitando que la cámara se baje sola
        transform.position = new Vector3(newX + shakeOffset.x, fixedY + shakeOffset.y, fixedZ);
    }
}