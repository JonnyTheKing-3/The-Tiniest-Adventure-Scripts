using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/Attack Data")]
public class AttackData : ScriptableObject
{
   public enum AttackType { SingleHit, MultiHit }
    public AttackType attackType;

    [ConditionalField(nameof(attackType), (int)AttackType.SingleHit)]
    public HitData hit;

    [ConditionalField(nameof(attackType), (int)AttackType.MultiHit)]
    public MultiHitData multiHit;

}

[System.Serializable]
public class HitData
{
    public enum HitEffect { Regular, SendFlying, Stun }

    public float damage = 3f, hitstop, attackBuffer;

    [Space]
    public float cameraShakeIntensity;
    [Tooltip("If cameraShakeDireciton is zero vector, then shake directions is randomized")] public Vector3 cameraShakeDirection;

    [Space]
    [Tooltip("The knockback distance travels")] public float distance;
    public float duration;

    [Space]
    public HitEffect effect;

    [ConditionalField(nameof(effect), (int)HitEffect.SendFlying)]
    public float SendFlyingHeight;

    [Space]
    [Range(0, 1)] public float hitstartTime, hitEndTime;
    [Range(0, 1)] public float CanFollowUpAttackTime, EndAttackAndEnableActionMapTime;

    public AudioClip swingSound;
}


[System.Serializable]
public class MultiHitData
{
    public HitData[] hits;
}