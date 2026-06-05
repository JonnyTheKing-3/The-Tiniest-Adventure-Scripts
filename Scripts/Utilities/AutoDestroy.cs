using UnityEngine;

public class AutoDestroyParticleVFX : MonoBehaviour
{
    [SerializeField] private float destroyAfter = 1.5f;

    private void Awake()
    {
        Destroy(gameObject, destroyAfter);
    }
}