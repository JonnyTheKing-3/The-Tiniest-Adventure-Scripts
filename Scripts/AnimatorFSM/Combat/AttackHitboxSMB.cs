using UnityEngine;

public class AttackHitboxSMB : StateMachineBehaviour
{
    [Header("Animator Assigned")]
    public int attackIndex = -1;

    [Header("Runtime (Debug)")]
    [SerializeField] private float hitstartTime;
    [SerializeField] private float hitEndTime;
    [SerializeField] private bool hitActive;
    [SerializeField] private bool trailActive;
    [SerializeField] private bool playedSwingSound;

    private IWeaponHolder weaponHolder;
    private Weapon weaponData;
    private AttackData attackData;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hitActive = false;
        trailActive = false;
        playedSwingSound = false;

        if (attackIndex < 0)
        {
            Debug.LogWarning("Attack index not set for this attack. Check animator attack index assignment.");
            return;
        }


        // model + animator are children of the main object
        Transform parentTransform = animator.transform.parent;
        if (parentTransform == null)
        {
            Debug.LogError("Animator has no parent transform. Cannot find IWeaponHolder.");
            return;
        }
        if (!parentTransform.TryGetComponent(out weaponHolder))
        {
            Debug.LogError("No IWeaponHolder found on player weapon holder.");
            return;
        }
        

        // Prepere weapon data
        weaponData = weaponHolder.GetWeapon();
        if (weaponData == null)
        {
            Debug.LogError("WeaponData is null.");
            return;
        }

        // Prepare attack data
        weaponData.currentAttackIndex = attackIndex;
        weaponData.currentMultiHitIndex = 0; // only used if multi-hit but just to be safe
        attackData = weaponData.CurrentAttackData();
        if (attackData == null)
        {
            Debug.LogError("CurrentAttackData is null. Check weapon template attack datas.");
            return;
        }

        
        MultiHitEnterErrorCheck();
        CacheCurrentHitWindow();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (attackIndex < 0 || weaponData == null || AttackIsMultiHitAndExceededHitLength()) return;

        float t = Mathf.Repeat(stateInfo.normalizedTime, 1f);
        weaponData.canBePerfectDodged = t > (hitstartTime - Player.Instance.perfectDodgeExtraWindow) && t < hitEndTime;

        // Enable hitbox and slash trail during the configured active hit window.
        if (!hitActive && t >= hitstartTime && t <= hitEndTime)
        {
            StartTrails();

            if (!playedSwingSound)
            {
                playedSwingSound = true;

                AudioClip audio = attackData.attackType == AttackData.AttackType.MultiHit ? 
                                weaponData.CurrentHitData().swingSound : 
                                attackData.hit.swingSound;

                if (audio == null)
                {
                    Debug.LogWarning($"Missing swing sound on {attackData.name}", attackData);
                }
                else
                {
                    AudioManager.Instance.PlaySFX(audio, animator.transform, false);
                }

            }

            weaponData.EnableWeaponColliders(true);
            hitActive = true;
            return;
        }

        // Disable hitbox/trail + advance multihit
        if (hitActive && t > hitEndTime)
        {
            StopTrails();
            playedSwingSound = false;
            
            weaponData.EnableWeaponColliders(false);
            weaponHolder.ResetHitList();
            hitActive = false;

            if (AttackIsMultiHit())
            {
                weaponData.currentMultiHitIndex++;
                if (weaponData.currentMultiHitIndex < attackData.multiHit.hits.Length) 
                    CacheCurrentHitWindow();
            }
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        StopTrails();

        hitActive = false;
        weaponHolder?.ResetHitList();
        weaponData?.EnableWeaponColliders(false);
        if (weaponData != null)
        {
            weaponData.canBePerfectDodged = false;
            weaponData.currentMultiHitIndex = 0;
        }
    }



    // HELPERS
    private void CacheCurrentHitWindow()
    {
        HitData hit = weaponData.CurrentHitData();
        if (hit == null)
        {
            Debug.LogError("CurrentHitData returned null. Check attack configuration.");
            return;
        }

        hitstartTime = hit.hitstartTime;
        hitEndTime = hit.hitEndTime;
    }
    private bool AttackIsMultiHit() => attackData != null && attackData.attackType == AttackData.AttackType.MultiHit;
    private bool AttackIsMultiHitAndExceededHitLength() => AttackIsMultiHit() && weaponData.currentMultiHitIndex >= attackData.multiHit.hits.Length;
    private void StartTrails()
    {
        if (trailActive) return;
        if (weaponData.swordSlashTrails.Count < 1) return;

        foreach (SwordSlashTrail trail in weaponData.swordSlashTrails)
        {
            trail.BeginTrail(this);
        }

        trailActive = true;
    }

    private void StopTrails()
    {
        if (!trailActive) return;
        if (weaponData.swordSlashTrails.Count < 1) return;

        foreach (SwordSlashTrail trail in weaponData.swordSlashTrails)
        {
            trail.EndTrail(this);
        }

        trailActive = false;
    }

    private void MultiHitEnterErrorCheck()
    {
        if (AttackIsMultiHit())
        {
            if (attackData.multiHit == null)
            {
                Debug.LogError("MultiHitData is null or has no hits");
                return;
            }
            if (attackData.multiHit.hits == null)
            {
                Debug.LogError("MultiHitData hits array is null.");
                return;
            }
            if (attackData.multiHit.hits.Length == 0)
            {
                Debug.LogError("MultiHitData hits array length is 0.");
                return;
            }
        }
    }
}
