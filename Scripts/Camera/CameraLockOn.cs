using UnityEngine;
using Cinemachine;

public class CameraLockOn : MonoBehaviour
{
    [Header("REFERENCES")]
    public Transform targetA;
    public Transform targetB;
    public CinemachineVirtualCamera vcam;

    [Header("INPUT")]
    public Vector2 look;

    [Header("COMPOSITION")]
    [Tooltip("Horizontal screen offset from center for each target.\n0.30 means targets tend to sit around +/-30% from center.")]
    [Range(0.05f, 0.45f)] public float screenOffsetX = 0.30f;

    [Tooltip("Vertical screen offset from center for each target.\n0.40 means targets must fit within +/-40% vertically (pretty generous).")]
    [Range(0.15f, 0.48f)] public float verticalHalfExtent = 0.40f;

    [Tooltip("Extra distance added after solving framing.")]
    public float distanceOffset = 0.0f;
    public float counterAttackDistanceOffset = 3f; // extra distance added when counter-attacking, to help show both player and enemy clearly

    public float minDistance = 4f;
    public float maxDistance = 25f;
    public float currentYaw;
    public float currentPitch;

    [Header("ORBIT")]
    public float orbitSensitivity = 140f; // degrees/sec at look=1
    public float minPitch = -10f;
    public float maxPitch = 55f;

    [Header("SMOOTHING")]
    [Tooltip("How quickly the midpoint catches up to targets (seconds). Higher = more lag.")]
    public float midpointSmoothTime = 0.18f;

    [Tooltip("Maximum speed the midpoint can move (units/sec). Prevents knockback snaps.")]
    public float midpointMaxSpeed = 18f;

    [Tooltip("How quickly distance changes (seconds). Higher = smoother zoom.")]
    public float distanceSmoothTime = 0.20f;

    [Tooltip("Maximum zoom speed (units/sec). Prevents sudden zoom pops.")]
    public float distanceMaxSpeed = 30f;

    [Tooltip("How quickly rotation aligns (seconds). Higher = smoother rotation.")]
    public float rotationSmoothTime = 0.12f;

    [HideInInspector] public bool snapThisFrame; // set true by your other script when switching to this camera

    Vector3 _midSmoothed;
    Vector3 _midVel;
    float _distSmoothed;
    float _distVel;
    Quaternion _rotSmoothed;
    float desiredDistanceOffset;
    float desiredMidpointSmoothTime;

    void Awake()
    {
        if (!vcam) vcam = GetComponent<CinemachineVirtualCamera>();

        _midSmoothed = transform.position;
        _rotSmoothed = transform.rotation;
        desiredDistanceOffset = distanceOffset;
    }

    void Update()
    {
        // If the target we're locked on to dies, unlock from it
        if (targetB == null)  
        {
            Player.Instance.playerState = Player.PlayerStates.normal;
            LockOnUIManager.Instance.SetUILockOnTarget(null);
            CamerasManager.Instance.SwitchToThirdPerson();
            return;
        }
    }

    void LateUpdate()
    {
        if (GameManager.Instance._currPlayMode == GameManager.PlayMode.Menu) return;
        
        desiredDistanceOffset = Player.Instance.CounterAttacking || Player.Instance.PerfectDodgeActive ? counterAttackDistanceOffset : distanceOffset;
        desiredMidpointSmoothTime = Player.Instance.CounterAttacking || Player.Instance.PerfectDodgeActive ? 1f : midpointSmoothTime;

        Vector3 A = targetA.position;
        Vector3 B = targetB.position;

        Vector3 midRaw = (A + B) * 0.5f;

        // --- Smooth the midpoint to reduce snappy knockback camera yanks ---
        if (snapThisFrame || _midSmoothed == Vector3.zero)
        {
            _midSmoothed = midRaw;
            _midVel = Vector3.zero;
        }
        else
        {
            _midSmoothed = Vector3.SmoothDamp( _midSmoothed, midRaw, ref _midVel, desiredMidpointSmoothTime, midpointMaxSpeed, Time.unscaledDeltaTime);
        }

        // --- Orbit angles from input (absolute angles, not "rotate by currentYaw each frame") ---
        Vector2 delta = look * orbitSensitivity * Time.unscaledDeltaTime;
        currentYaw += delta.x;
        currentPitch = Mathf.Clamp(currentPitch - delta.y, minPitch, maxPitch);

        // --- Build an orbit frame that behaves well when targets have different Y ---
        // Use a *horizontal* baseline as the "right" reference so vertical separation doesn't twist the framing.
        Vector3 baseline = B - A;
        Vector3 baselineHoriz = Vector3.ProjectOnPlane(baseline, Vector3.up);
        if (baselineHoriz.sqrMagnitude < 1e-6f)
            baselineHoriz = Vector3.right; // fallback when baseline is mostly vertical

        Vector3 rightRef = baselineHoriz.normalized;

        // Yaw rotates around world up, starting from the baseline reference.
        Quaternion qYaw = Quaternion.AngleAxis(currentYaw, Vector3.up);
        Vector3 right = (qYaw * rightRef).normalized;

        // Forward is perpendicular to right/up, then pitch rotates around "right".
        Vector3 forwardFlat = Vector3.Cross(right, Vector3.up).normalized; // points "toward" the midpoint
        Quaternion qPitch = Quaternion.AngleAxis(currentPitch, right);
        Vector3 forward = (qPitch * forwardFlat).normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;

        // Camera looks at midpoint: position will be mid - forward * distance
        // But: points can be in front/behind the midpoint in this orbit frame, so we solve using z = dist + dot(p, forward)

        // --- Solve distance so BOTH targets land near desired horizontal composition AND fit vertically ---
        float vFovRad = vcam.m_Lens.FieldOfView * Mathf.Deg2Rad;
        float tanV = Mathf.Tan(vFovRad * 0.5f);
        float tanH = tanV * Camera.main.aspect;

        // Express target offsets in the orbit basis centered on midpoint
        Vector3 pA = A - _midSmoothed;
        Vector3 pB = B - _midSmoothed;

        float xA = Vector3.Dot(pA, right);
        float yA = Vector3.Dot(pA, up);
        float zA = Vector3.Dot(pA, forward);

        float xB = Vector3.Dot(pB, right);
        float yB = Vector3.Dot(pB, up);
        float zB = Vector3.Dot(pB, forward);

        // Desired: targets tend toward +/-screenOffsetX horizontally.
        // Projection: x_ndc ~= (x / (dist + z)) / tanH
        // Want |x_ndc| <= screenOffsetX  -> dist >= |x|/(screenOffsetX*tanH) - z
        // Also enforce vertical fit: |y_ndc| <= verticalHalfExtent -> dist >= |y|/(verticalHalfExtent*tanV) - z
        float distFromX_A = RequiredDistanceForAxis(Mathf.Abs(xA), screenOffsetX, tanH, zA);
        float distFromX_B = RequiredDistanceForAxis(Mathf.Abs(xB), screenOffsetX, tanH, zB);

        float distFromY_A = RequiredDistanceForAxis(Mathf.Abs(yA), verticalHalfExtent, tanV, zA);
        float distFromY_B = RequiredDistanceForAxis(Mathf.Abs(yB), verticalHalfExtent, tanV, zB);

        float distSolved = Mathf.Max(distFromX_A, distFromX_B, distFromY_A, distFromY_B);
        distSolved += desiredDistanceOffset;
        distSolved = Mathf.Clamp(distSolved, minDistance, maxDistance);

        // --- Smooth distance so vertical changes / knockback don't pop zoom ---
        if (snapThisFrame || _distSmoothed <= 0.001f)
        {
            _distSmoothed = distSolved;
            _distVel = 0f;
        }
        else
        {
            _distSmoothed = Mathf.SmoothDamp(
                _distSmoothed,
                distSolved,
                ref _distVel,
                distanceSmoothTime,
                distanceMaxSpeed,
                Time.unscaledDeltaTime
            );
        }

        Vector3 desiredPos = _midSmoothed - forward * _distSmoothed;

        Quaternion desiredRot = Quaternion.LookRotation((_midSmoothed - desiredPos).normalized, Vector3.up);

        // --- Smooth rotation (helps with snappy moments too) ---
        if (snapThisFrame)
        {
            transform.position = desiredPos;
            transform.rotation = desiredRot;
            _rotSmoothed = desiredRot;
            snapThisFrame = false;
            return;
        }

        transform.position = desiredPos;

        // Exponential-ish smoothing for rotation without overshoot
        float rotT = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.0001f, rotationSmoothTime));
        _rotSmoothed = Quaternion.Slerp(_rotSmoothed, desiredRot, rotT);
        transform.rotation = _rotSmoothed;
    }

    public void SetOrbitFromCameraPose(Vector3 cameraPosition)
    {
        Vector3 A = targetA.position;
        Vector3 B = targetB.position;
        Vector3 mid = (A + B) * 0.5f;

        Vector3 baseline = B - A;
        Vector3 baselineHoriz = Vector3.ProjectOnPlane(baseline, Vector3.up);
        if (baselineHoriz.sqrMagnitude < 1e-6f)
            baselineHoriz = Vector3.right;

        Vector3 rightRef = baselineHoriz.normalized;
        Vector3 baseForwardFlat = Vector3.Cross(rightRef, Vector3.up).normalized;

        Vector3 cameraToMid = mid - cameraPosition;
        Vector3 forwardFlat = Vector3.ProjectOnPlane(cameraToMid, Vector3.up);

        if (forwardFlat.sqrMagnitude < 1e-6f)
            forwardFlat = baseForwardFlat;

        forwardFlat.Normalize();

        currentYaw = Vector3.SignedAngle(baseForwardFlat, forwardFlat, Vector3.up);

        Quaternion qYaw = Quaternion.AngleAxis(currentYaw, Vector3.up);
        Vector3 right = (qYaw * rightRef).normalized;

        currentPitch = Mathf.Clamp(
            Vector3.SignedAngle(forwardFlat, cameraToMid.normalized, right),
            minPitch,
            maxPitch
        );
    }

    static float RequiredDistanceForAxis(float absAxis, float desiredHalfNdc, float tanFovHalf, float pointForwardOffset)
    {
        // If desiredHalfNdc is tiny, avoid blowups.
        desiredHalfNdc = Mathf.Max(0.001f, desiredHalfNdc);

        // dist >= absAxis/(desiredHalfNdc * tanFovHalf) - pointForwardOffset
        float dist = (absAxis / (desiredHalfNdc * tanFovHalf)) - pointForwardOffset;

        // Keep positive-ish; if points are behind mid in forward axis, this can go negative.
        return Mathf.Max(0.01f, dist);
    }
}
