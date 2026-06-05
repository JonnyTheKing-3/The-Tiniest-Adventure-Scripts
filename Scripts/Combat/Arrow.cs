using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Arrow : MonoBehaviour, INotModelForwardTarget // To Do: Object pooling later, but for now this is fine since arrows are destroyed on hit and not instantiated every frame or anything
{
    public bool flyArrow = false;
    public float speed;
    public AttackData arrowAttackData;


    [HideInInspector]public Vector3 targetPos;
    private Vector3 startPos;
    private TrailRenderer trailRenderer;
    private LayerMask hittableLayerMask;

    void Start()
    {
        flyArrow = false;
        trailRenderer = GetComponent<TrailRenderer>();
        trailRenderer.emitting = false;
    }

    public void ArrowShot(Vector3 target, float speed, AttackData attackData, LayerMask hittables)
    {
        targetPos = target;
        this.speed = speed;
        arrowAttackData = attackData;
        startPos = transform.position;
        hittableLayerMask = hittables;

        flyArrow = true;
        transform.SetParent(null);
        transform.LookAt(targetPos);
        trailRenderer.emitting = true;
        trailRenderer.Clear();
    }

    void Update()   // Apply arc later... maybe. I kinda like that it goes in a straight line. Feels cartoonish
    {
        if (flyArrow)
        {
            // Use movetowards unclamped because lerped goes faster/slower depending on distance
            Vector3 newPos = MoveTowardsUnclamped(startPos, targetPos, speed * Time.deltaTime, transform.position);
            
            // hit detection
            Vector3 step = newPos - transform.position;
            Vector3 direction = step.normalized;
            
            if (direction != Vector3.zero)transform.rotation = Quaternion.LookRotation(direction);

            if (Physics.SphereCast(transform.position, .001f, direction, out RaycastHit hit, step.magnitude, hittableLayerMask, QueryTriggerInteraction.Ignore))
                ArrowHit(hit);
            else
                transform.position = newPos;
        }
    }

    public static Vector3 MoveTowardsUnclamped(Vector3 start, Vector3 target, float maxDistanceDelta, Vector3 currentPos)
    {
        Vector3 direction = (target - start).normalized;
        return currentPos + direction * maxDistanceDelta;
    }

    public void ArrowHit(RaycastHit hit)
    {
        // Debug.Log("Arrow hit: " + hit.collider.gameObject.name);
        flyArrow = false;

        if (hit.collider.gameObject.TryGetComponent<IHittable>(out IHittable hittable))
        {
            // Debug.Log("Arrow hit IHittable: " + hit.collider.gameObject.name);
            hittable.GotHit(gameObject, arrowAttackData.hit, 0f, hit.point, hit.normal); // arrows are always only one hit
            StartCoroutine(DestroyAfterDelay(arrowAttackData.hit.hitstop));
        }

        // Stick the arrow to the surface it hit if it didn't hit an IHittable
        else
        {
            bool isMeshCollider = hit.collider is MeshCollider;
            float offset = hit.collider is MeshCollider ? .3f : 0f; // Small offset to prevent clipping into the surface

            // Debug.Log("collider: " + (isMeshCollider ? "MeshCollider" : "OtherCollider"));
            transform.position = hit.point - ((hit.point - transform.position).normalized * offset);
        }
    }

    IEnumerator DestroyAfterDelay(float delay)
    {
        // wait hitstop duration + small buffer to ensure hit object processes hitstop and initialization of knockback properly
        yield return new WaitForSeconds(delay +.001f);
        yield return null;

        /* Read to see why we don't need to wait a frame before destroying the arrow
            So we got two scenarios:
                1. the hit object enters knockback state on this frame:
                    Arrow is destroyed safely the frame after it's no longer needed

                2. The hit object enters knockback state on the next frame (same frame as arrow is destroyed). 
                    Update() runs before returning to this coroutine, so other enters knockback state first. 
                    It uses arrow for whataever it needs before we destroy arrow, and then arrow is destroyed safely immeadeatly after.

            Either way, the arrow is destroyed safely without interfering with the hit object's hitstop or knockback.
        */
        Destroy(gameObject);
    }
}
