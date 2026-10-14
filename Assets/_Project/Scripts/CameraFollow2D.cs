using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public float offsetX = 3.5f;
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desiredPos = new Vector3(target.position.x + offsetX, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}