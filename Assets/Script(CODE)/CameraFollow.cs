using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟隨目標")]
    public Transform target; 

    [Header("鏡頭移動速度")]
    public float smoothSpeed = 5f;

    [Header("鏡頭活動範圍 (請手動調整)")]
    public Vector2 minPosition; 
    public Vector2 maxPosition; 

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);

        float clampedX = Mathf.Clamp(desiredPosition.x, minPosition.x, maxPosition.x);
        float clampedY = Mathf.Clamp(desiredPosition.y, minPosition.y, maxPosition.y);

        Vector3 finalPosition = new Vector3(clampedX, clampedY, transform.position.z);

        transform.position = Vector3.Lerp(transform.position, finalPosition, smoothSpeed * Time.deltaTime);
    }
}