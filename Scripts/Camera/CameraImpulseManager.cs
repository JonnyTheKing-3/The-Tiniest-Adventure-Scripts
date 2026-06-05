using Cinemachine;
using UnityEngine;

public class CameraImpulseManager : MonoBehaviour
{
    public static CameraImpulseManager Instance { get; private set; }

    private CinemachineImpulseSource impulseSource;

    [Tooltip("Using prevents camera shake stackiing. Turn it off if you want camera shakes to stack")]
    [SerializeField] private bool useCooldown = true;
    [SerializeField] private float shakeCooldown = 0.12f;
    [SerializeField] private Vector3 defaultDirection = Vector3.up;

    private float nextShakeAllowedTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void Shake(float intensity)
    {
        if (!CanShake()) return;

        impulseSource.GenerateImpulse(intensity);
        StartCooldown();
    }

    public void Shake(Vector3 direction, float intensity)
    {
        if (!CanShake()) return;

        if (direction.sqrMagnitude < 0.001f) direction = defaultDirection;

        impulseSource.GenerateImpulse(direction.normalized * intensity);
        StartCooldown();
    }

    private bool CanShake()
    {
        if (!useCooldown) return true;

        return Time.unscaledTime >= nextShakeAllowedTime;
    }

    private void StartCooldown()
    {
        if (!useCooldown) return;

        nextShakeAllowedTime = Time.unscaledTime + shakeCooldown;
    }
}