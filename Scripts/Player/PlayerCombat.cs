using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour, IHittable, IWeaponHolder
{
    private PlayerLocomotionFSM p_loco => Player.Instance._playerLocomotion;
    private PlayerAnimation p_anim => Player.Instance._playerAnimation;
    
    public Weapon currentWeapon;
    [Tooltip("Whenever a hit is the last hit, we use this attack's data")]
    public AttackData lastHitAttackData;
    public Weapon GetWeapon() => currentWeapon;

    private void Awake()
    {
        if (currentWeapon == null) currentWeapon = new Weapon();
    }


    // Weapons have the colliders which are child objects of the player (the hands, which are named weapon_r and weapon_l)
    private readonly HashSet<IHittable> enemiesHit = new HashSet<IHittable>();
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == gameObject) return;
        // Debug.Log("Player hit something with a trigger: " + other.name);

        if (other.TryGetComponent<IHittable>(out IHittable hittable))
        {
            // Debug.Log("Player hit an IHittable: " + other.name);
            if (enemiesHit.Add(hittable))
            {
                // Debug.Log("Player hit an IHittable that hasn't been hit yet: " + other.name);

                HitData hitdata = currentWeapon.CurrentHitData();

                p_loco.ApplyHitstop(hitdata);

                // Camera Shake
                if (hitdata.cameraShakeDirection.magnitude < .01f)
                    CameraImpulseManager.Instance.Shake(hitdata.cameraShakeDirection, hitdata.cameraShakeIntensity);
                else if (hitdata.cameraShakeIntensity >= .01)
                    CameraImpulseManager.Instance.Shake(hitdata.cameraShakeIntensity);


                Vector3 hitPoint = other.ClosestPoint(transform.position);
                hittable.GotHit(gameObject, hitdata, Player.Instance.combatStats.Attack, hitPoint, (transform.position - hitPoint).normalized); // Hitstop is applied in GotHit if the hittable is knockbackable
            }
        }
    }
    public void ResetHitList() => enemiesHit.Clear();

    bool blockingAndSuccessful = false;
    public void GotHit(GameObject attacker, HitData attackData, float AttackerAttackStat, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (Player.Instance.startedDeathRoutine) return;
        if (SaveGameManager.Instance.loadRoutine != null) return; // If we are loading, don't interact

        AudioManager.Instance.PlayHitImpact(transform);

        float AttackAngle = Vector3.Angle(p_anim.transform.forward, (attacker.transform.position - transform.position).normalized);
        blockingAndSuccessful = p_loco.CurrentStateName.Contains("Block") && AttackAngle < 90f;

        if (!blockingAndSuccessful)
            Player.Instance.health.TakeDamage(attackData.damage + AttackerAttackStat - Player.Instance.combatStats.Defense);
        
        HitData passData = Player.Instance.healthHasDepleted && lastHitAttackData?.hit != null ? lastHitAttackData.hit : attackData;

        // VFX
        GameObject hitVFX = GameManager.Instance.GetHitImpactVFX();
        hitVFX.transform.position = hitPoint;
        hitVFX.transform.rotation = Quaternion.LookRotation(hitNormal);
        hitVFX.SetActive(true);

        if (blockingAndSuccessful)
            AnimationEvents.Instance.DisableNonEssentialActionsExceptBlock();
        else
            AnimationEvents.Instance.DisableNonEssentialActions();

        p_anim.TriggerCorrectKnockbackAnimation(passData, blockingAndSuccessful);

        p_loco.ApplyHitstop( passData, 
            onFinished:() => KnockbackTriggered(attacker, passData)
        );

        // Camera Shake
        if (passData.cameraShakeDirection.magnitude < .01f)
            CameraImpulseManager.Instance.Shake(passData.cameraShakeDirection, passData.cameraShakeIntensity);
        else if (passData.cameraShakeIntensity >= .01)
            CameraImpulseManager.Instance.Shake(passData.cameraShakeIntensity);
    }
    public void KnockbackTriggered(GameObject attacker, HitData attackData)
    {
        float dist = attackData.distance;
        float dur = attackData.duration;
        Vector3 targetPosition = attacker.transform.position;
        targetPosition += attacker.transform.GetChild(0).TryGetComponent(out Animator animator)? animator.transform.forward * dist : attacker.transform.forward * dist; // Use the forward of the attacker's model if it has one
        targetPosition.y = transform.position.y; // Keep the player's y position

        if (blockingAndSuccessful)  // If in any blocking state, use blocked knockback
            p_loco.StartBlockedKnockback(targetPosition, dur, attackData, attacker.transform);
        else
            p_loco.StartKnockback(targetPosition, dur, attackData, attacker.transform);
    }
}
