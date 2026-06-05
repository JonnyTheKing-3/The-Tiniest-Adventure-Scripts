using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public class PooledHitImpactVFX : MonoBehaviour
{
    [SerializeField] private VisualEffect vfx;
    [SerializeField] private float lifetime = 1f;

    private Coroutine disableRoutine;

    private void OnEnable()
    {
        if (disableRoutine != null) StopCoroutine(disableRoutine);

        vfx.Reinit();
        vfx.Play();

        disableRoutine = StartCoroutine(DisableAfterLifetime());
    }

    private IEnumerator DisableAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);

        vfx.Stop();
        gameObject.SetActive(false);
    }
}