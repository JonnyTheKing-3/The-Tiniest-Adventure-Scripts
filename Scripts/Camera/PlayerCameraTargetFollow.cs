using UnityEngine;

public class PlayerCameraTargetFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;

    void Update()
    {
        Vector3 desiredPosition = Vector3.Lerp(transform.position, target.position, smoothSpeed * Time.deltaTime);
        transform.position = desiredPosition;
    }
}
