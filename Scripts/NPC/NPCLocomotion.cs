using UnityEngine;
using System;

public class NPCLocomotion : FiniteStateMachine
{
    [Header("REFERENCES")]
    public LayerMask groundLayer;
    [HideInInspector]public Transform orientation;
    [HideInInspector] public CharacterController controller;

    [Space]
    [Header("STATUS")]
    [HideInInspector]public Vector2 inputDir;
    [HideInInspector] public float DirectionMagnitude = 0f;
    [HideInInspector] public Vector3 moveDirection;
    private Vector3 normal = Vector3.up;

    public float verticalVelocity = 0f;
    public Vector3 NewHorizontalVel;
    public Vector3 targetVel;
    public bool casthit = false;
    public bool stickToGround = false;
    public LayerMask wallLayer;

    // Exposed so states can adjust it
    [NonSerialized] public Vector3 currentVelocity;

    [Header("GROUNDING")]
    public bool grounded = false;
    public float groundRayDist = 1.5f;
    public float sphereRadius = 0.3f;
    public float MaxGroundStickDistance = 0.5f;
    public float groundOffset = 0.1f;
    public RaycastHit Surface;

    [Header("MOVEMENT")]
    public float MaxSpeed = 5f;
    public float acceleration = 20f;
    public float deceleration = 20f;
    public float turnSpeed = 10f;
    public float gravity = 25f;


    protected virtual void Awake()
    {    
        if (!controller) controller = GetComponent<CharacterController>();
        if (!orientation) orientation = transform;

        AddState("idle",  new NPCLocomotionIdleState(this));
        Move();
    }

    public void UpdateGroundingStatus()
    {
        Vector3 origin = transform.position + Vector3.up; // small lift to avoid starting inside ground
        casthit = Physics.SphereCast(origin, sphereRadius, Vector3.down, out Surface, groundRayDist, groundLayer, QueryTriggerInteraction.Ignore);

        Surface = casthit ? Surface : default;
        stickToGround = casthit && (transform.position.y+.1f - Surface.point.y) <= MaxGroundStickDistance;

        grounded = stickToGround || controller.isGrounded;
    }

    public void Move()
    {
        DirectionMagnitude = moveDirection.magnitude;

        Vector3 SurfaceAppliedDir = GetSlopeForward() * moveDirection.magnitude;

        Vector3 targetHorizontalVel = SurfaceAppliedDir * MaxSpeed;
        float appropriateAcceleration = moveDirection != Vector3.zero ? acceleration : deceleration;
        float rad = turnSpeed * Mathf.PI * Time.deltaTime;
        Vector3 currHorizontalVel = Vector3.ProjectOnPlane(currentVelocity, normal);
        NewHorizontalVel = Vector3.RotateTowards(currHorizontalVel, targetHorizontalVel, rad, appropriateAcceleration * Time.deltaTime);
        targetVel = NewHorizontalVel;

        if (!grounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
            // Debug.Log("In air" );
        }
        else
        {
            verticalVelocity = targetVel.y;
        }

        targetVel.y = verticalVelocity;

        // Wall slide check
        Vector3 nextPos = transform.position + targetVel * Time.deltaTime;
        float dist = Vector3.Distance(transform.position, nextPos);
        if (stickToGround && CapsuleCastForWall(targetVel.normalized, dist, out RaycastHit hit))
        {
            // Only treat steep surfaces as walls (not walkable slopes)
            if (Vector3.Angle(Vector3.up, hit.normal) >= 90f)
            {
                Vector3 wallSlideDir = Vector3.ProjectOnPlane(SurfaceAppliedDir.normalized, hit.normal.normalized);

                // The more parallel to the wall, the more speed we keep
                float dot = Vector3.Dot(SurfaceAppliedDir.normalized, hit.normal.normalized);
                float slideMagnitude = MaxSpeed * (1 - Mathf.Abs(dot));

                targetVel = wallSlideDir * slideMagnitude;
                targetVel.y = verticalVelocity;
            }
        }

        controller.Move(targetVel * Time.deltaTime);
        currentVelocity = targetVel;

        if (grounded) {controller.Move(Vector3.down * (transform.position.y +.1f - Surface.point.y) * Time.deltaTime);}
    }

    private bool CapsuleCastForWall(Vector3 direction, float distance, out RaycastHit hit)
    {
        if (!controller)
        {
            hit = default;
            return false;
        }

        Vector3 center = transform.position + controller.center;

        float bottomOffset = -controller.height * 0.5f + controller.radius;
        float topOffset = controller.height * 0.5f - controller.radius;

        Vector3 p1 = center + Vector3.up * bottomOffset;
        Vector3 p2 = center + Vector3.up * topOffset;

        if (Physics.CapsuleCast(p1, p2, controller.radius, direction, out hit, distance, wallLayer, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform)
                return false;

            return true;
        }

        return false;
    }

    public Vector3 GetSlopeForward()
    {
        normal = Surface.collider != null ? Surface.normal : Vector3.up;
        Vector3 right = Vector3.Cross(Vector3.up, moveDirection).normalized;    // Only vertical influence
        Vector3 SlopeForward = Vector3.Cross(right, normal).normalized;
        return SlopeForward;
    }

    public void ResetVelocityFactors()
    {
        currentVelocity = Vector3.zero;
        verticalVelocity = 0f;
        moveDirection = Vector3.zero;
        NewHorizontalVel = Vector3.zero;
        targetVel = Vector3.zero;
        DirectionMagnitude = 0f; // For MoveEnemy because of animation
    }

    public void SetMoveDirection(Vector3 dir) => moveDirection = dir;

    public void SetSpeed(float speed) => MaxSpeed = speed;


    public void MoveByDelta(Vector3 delta, bool deltaIncludesAirMovement)
    {
        // Make velocity
        moveDirection = delta.normalized;

        // If delta includes air movement, then we don't want ground slope influence
        Vector3 slopeDelta = deltaIncludesAirMovement ? moveDirection : Vector3.ProjectOnPlane(delta, Surface.normal); // Using the GetSlope function messes with the intended vector
        float speed = delta.magnitude / Time.deltaTime;
        Vector3 vel = slopeDelta * speed;

        // Wall slide check
        Vector3 nextPos = transform.position + vel * Time.deltaTime;
        float dist = Vector3.Distance(transform.position, nextPos);
        if (stickToGround && CapsuleCastForWall(vel.normalized, dist, out RaycastHit hit))
        {
            // Only treat steep surfaces as walls (not walkable slopes)
            if (Vector3.Angle(Vector3.up, hit.normal) >= 90f)
            {
                Vector3 wallSlideDir = Vector3.ProjectOnPlane(slopeDelta.normalized, hit.normal.normalized);

                // The more parallel to the wall, the more speed we keep
                float dot = Vector3.Dot(slopeDelta.normalized, hit.normal.normalized);
                float slideMagnitude = MaxSpeed * (1 - Mathf.Abs(dot));

                vel = wallSlideDir * slideMagnitude;
                vel.y = verticalVelocity;
            }
        }

        controller.Move(vel * Time.deltaTime);
        moveDirection = Vector3.zero;
        // currentVelocity = vel; Might need this later. Ignore for now
        // if (grounded) {controller.Move(Vector3.down * (transform.position.y +.1f - Surface.point.y) * Time.deltaTime);}
    }
}


public class NPCLocomotionIdleState : State<NPCLocomotion>
{
    public NPCLocomotionIdleState(NPCLocomotion self) : base(self) { }

    public override void Enter() { }

    public override void Update() { }

    public override void Exit() { }
}