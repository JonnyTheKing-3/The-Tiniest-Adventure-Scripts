using Cinemachine;
using UnityEngine;

public class CameraOrientation : MonoBehaviour
{
    public Transform player;
    public Transform orientation;
    public Transform AimCameraPivot;
    

    private void Update()
    {
        Vector3 viewDir = Vector3.zero;
        switch (CamerasManager.Instance.CameraState)
        {
            case CamerasManager.CameraStates.ThirdPerson:
                viewDir = player.transform.position - transform.position;
                orientation.forward = viewDir.normalized;
                break;

            case CamerasManager.CameraStates.LockOn:
                viewDir = player.transform.position - transform.position;
                orientation.forward = viewDir.normalized;
                break;

            case CamerasManager.CameraStates.Aim:
                Vector3 aimDir =  AimCameraPivot.transform.position - transform.position;
                orientation.forward = aimDir.normalized;

                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, Player.Instance._playerAnimation.aimIconRectTransform.position);
                Ray ray = Camera.main.ScreenPointToRay(screenPos);
                float shotDist = Player.Instance.shotDistance;

                // TO DO: comment debugs and add arrow functionality
                // Need to ignore the aim target's trigger collider, otherwise it will always hit itself and never hit anything else
                // Use sperecast instead of raycast to give the player a larger hitbox to aim at, otherwise it can be frustrating in edges of walls and small targets
                if (Physics.SphereCast(ray, Player.Instance.aimRadius, out RaycastHit hit, shotDist, Player.Instance.aimLayerMask, QueryTriggerInteraction.Ignore))
                {
                    // Debug.Log("Hit: " + hit.collider.name);
                    Player.Instance.AimTarget.position = hit.point;
                }
                else
                {
                    // Debug.Log("Hit nothing.");
                    Player.Instance.AimTarget.position = ray.origin + ray.direction * shotDist;
                }       

                // Debugging variables for the spherecast
                // debugRay = ray;
                // debugRayDistance = shotDist;
                // debugRayRadius = Player.Instance.aimRadius;
                ///////////////////////////////////////////         
                break;
        }
    }

    // private Ray debugRay;
    // private float debugRayDistance;
    // private float debugRayRadius;
    // void OnDrawGizmos()
    // {
    //     if (debugRay.direction == Vector3.zero) return;

    //     Gizmos.color = Color.cyan;

    //     // Draw start + end spheres
    //     Gizmos.DrawWireSphere(debugRay.origin, debugRayRadius);
    //     Gizmos.DrawWireSphere(
    //         debugRay.origin + debugRay.direction * debugRayDistance,
    //         debugRayRadius
    //     );

    //     // Draw connecting lines (gives cylinder illusion)
    //     Vector3 right = Vector3.Cross(debugRay.direction, Vector3.up).normalized * debugRayRadius;
    //     Vector3 up = Vector3.Cross(debugRay.direction, right).normalized * debugRayRadius;

    //     Gizmos.DrawLine(debugRay.origin + right, debugRay.origin + right + debugRay.direction * debugRayDistance);
    //     Gizmos.DrawLine(debugRay.origin - right, debugRay.origin - right + debugRay.direction * debugRayDistance);
    //     Gizmos.DrawLine(debugRay.origin + up, debugRay.origin + up + debugRay.direction * debugRayDistance);
    //     Gizmos.DrawLine(debugRay.origin - up, debugRay.origin - up + debugRay.direction * debugRayDistance);
    // }

}
