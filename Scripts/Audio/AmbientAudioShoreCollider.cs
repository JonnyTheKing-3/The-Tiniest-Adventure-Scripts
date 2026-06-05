using UnityEngine;

public class AmbientAudioShoreCollider : MonoBehaviour
{
    public LayerMask playerLayer;
    [SerializeField] AmbientAudioShoreSpline ambientAudioShoreSpline;

    void OnTriggerEnter(Collider other)
    {    
        bool isPlayer = (playerLayer.value & (1 << other.gameObject.layer)) != 0;
        if (!isPlayer || ambientAudioShoreSpline.playerIsOnThisShore) return;

        ambientAudioShoreSpline.playerIsOnThisShore = true;
    }

}
