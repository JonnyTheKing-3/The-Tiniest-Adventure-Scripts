
using UnityEngine;
using Cinemachine;

public class AmbientAudioShoreSpline : MonoBehaviour
{
    // The idea is to give a sense of ambience while the player is near the water by moving the audio source towards the player
    // and if the player is in the ocean, than we keep the audio source at player position so that we always hear the sound

    // Gets activated by a trigger collider object. The reference below is there just so we can see the relationship

    PlayerLocomotionFSM p_loco => Player.Instance._playerLocomotion;

    [SerializeField] private GameObject ShoreAudioObject;
    [SerializeField] private CinemachineSmoothPath path;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Collider _collider;

    [Space]
    [SerializeField] private LayerMask shoreLayer;
    [SerializeField] private LayerMask oceanLayer;
    [SerializeField, Range(1, 100)] private int closestPointStepsPerSegment = 20;
    [SerializeField] private bool disableDoppler = true;
    public bool playerIsOnThisShore = false;
    
    void Awake()
    {
        path = ShoreAudioObject.GetComponentInChildren<CinemachineSmoothPath>();
        audioSource = ShoreAudioObject.GetComponentInChildren<AudioSource>();
        playerIsOnThisShore = false;

        if (disableDoppler && audioSource != null)
            audioSource.dopplerLevel = 0f;

        ResetAudioSourcePosition();
    }

    // Only runs when player is on this shore because player turns on/off this script upon touching the shore
    void Update()
    {
        if (!playerIsOnThisShore) return;
        
        int surfaceLayer = p_loco.Surface.collider.gameObject.layer;
        if (p_loco.Surface.collider != null && !IsInLayerMask(surfaceLayer, shoreLayer))
        {
            playerIsOnThisShore = false;
            ResetAudioSourcePosition();
            return;
        }



        if (p_loco.IsInState<PlayerLocomotionSwimState>())
        {
            if (p_loco.waterRayHit.collider == null) return;

            // If on ocean, move the audio to player
            int waterSurfaceLayer = p_loco.waterRayHit.collider.gameObject.layer;
            if (IsInLayerMask(waterSurfaceLayer, oceanLayer))
            {
                audioSource.transform.position = Player.Instance.transform.position;
            }
        }
        else
        {
            if (p_loco.Surface.collider == null) return;

            // If on shore, move the audio to the closest point to the player on the spline
            if (IsInLayerMask(surfaceLayer, shoreLayer))
            {
                float closestPathPosition = path.FindClosestPoint(p_loco.transform.position, 0, -1, closestPointStepsPerSegment);   // 0 = start from the beginning, -1 = ignore radius limit. Together = search the entire spline from the beginning
                audioSource.transform.position = path.EvaluatePosition(closestPathPosition);
            }   
        }
    }

    void ResetAudioSourcePosition()
    {
        audioSource.transform.position = path.EvaluatePositionAtUnit(0.5f, CinemachinePathBase.PositionUnits.Normalized);
    }

    bool IsInLayerMask(int layer, LayerMask layerMask) => (layerMask.value & (1 << layer)) != 0;
}
