using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon
{
    /// <summary>
    /// Runtime data for a weapon instance. Contains references to the weapon template and manages weapon colliders. 
    /// 
    /// Bonus tip: make a new instance when used because this is a C# reference type and Unity does not automatically create instances for it
    /// </summary>

    public WeaponData weaponTemplate;
    public int currentAttackIndex; // Set by the AttackSMB on the animator inspector. I set it manually depending on which attack it is
    public int currentMultiHitIndex; // Runtime index for multihit (set by SMB). Only used if attack is multi-hit. Do NOT store this in ScriptableObjects.
    public bool canBePerfectDodged; // Gets turned on and off by the AttackSMB + EnableWeaponColliders() below
    public bool isAttacking;
    public AttackData CurrentAttackData() 
    {
        if (weaponTemplate == null)
        {
            Debug.LogWarning("Weapon template is null. Returning null for attack data.");
            return null;
        }
        if ( weaponTemplate.attackDatas == null)
        {
            Debug.LogWarning("Weapon template attack data is null. Returning null for attack data.");
            return null;
        }
        if (weaponTemplate.attackDatas.Length == 0)
        {
            Debug.LogWarning("Weapon template attack data length is 0. Returning null for attack data.");
            return null;
        }
       if (currentAttackIndex < 0 || currentAttackIndex >= weaponTemplate.attackDatas.Length)
       {
           Debug.LogWarning($"CurrentAttackIndex {currentAttackIndex} out of range. Returning null for attack data.");
           return null;
       }

        return weaponTemplate.attackDatas[currentAttackIndex];                                            
    }

    public HitData CurrentHitData() 
    {
        if (weaponTemplate == null)
        {
            Debug.LogWarning("Weapon template is null. Returning null for attack data.");
            return null;
        }
        if ( weaponTemplate.attackDatas == null)
        {
            Debug.LogWarning("Weapon template attack data is null. Returning null for attack data.");
            return null;
        }
        if (weaponTemplate.attackDatas.Length == 0)
        {
            Debug.LogWarning("Weapon template attack data length is 0. Returning null for attack data.");
            return null;
        }

        if (weaponTemplate.attackDatas[currentAttackIndex].attackType == AttackData.AttackType.SingleHit)
        {
            return weaponTemplate.attackDatas[currentAttackIndex].hit;                                            
        }
        else
        {
            var multihit = weaponTemplate.attackDatas[currentAttackIndex].multiHit;
            if (multihit == null || multihit.hits == null || multihit.hits.Length == 0)
            {
                Debug.LogWarning("MultiHitData missing stuff. Returning null for hit data.");
                return null;
            }
     
            return multihit.hits[currentMultiHitIndex];                                          
        }

    }

    private readonly HashSet<Collider> weaponColliders = new HashSet<Collider>();
    [HideInInspector] public HashSet<SwordSlashTrail> swordSlashTrails = new HashSet<SwordSlashTrail>();

    public void RegisterWeaponColliders(params Transform[] parentsOfWeaponHolders)      // Also registers vfx trails
    {
        ClearWeaponColliders();
        swordSlashTrails.Clear();
        foreach (var parent in parentsOfWeaponHolders)
        {
            var col = parent.GetComponentInChildren<Collider>();
            SwordSlashTrail trail = parent.GetComponentInChildren<SwordSlashTrail>();

            if (col) weaponColliders.Add(col);
            if (trail) swordSlashTrails.Add(trail);
        }

        // Debug.Log("Registered colliders:");
        // DebugLogColliders();
    }
    public void EnableWeaponColliders(bool enabled)
    {
        isAttacking = enabled;
        canBePerfectDodged = enabled;

        foreach (var col in weaponColliders)
            if (col) col.enabled = enabled;
    }
    public void ClearWeaponColliders() => weaponColliders.Clear();

    public void DebugLogColliders()
    {
        foreach (var col in weaponColliders)
            Debug.Log(col.name);

        Debug.Log("Finished listing. Total colliders: " + weaponColliders.Count);
        // Debug.Log("----");
    }
}
