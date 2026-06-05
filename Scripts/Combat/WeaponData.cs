using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/Weapon Data")]
public class WeaponData : ScriptableObject
{
    /// <summary>
    /// Immutable weapon data for the weapon. Used by Weapon so that multiple instances can share the same data.
    /// </summary>
    
    public enum WeaponType { SingleHanded, SecondWeapon, TwoHanded, Spear, Wand }
    public AnimatorOverrideController AOC;
    public WeaponType weaponType = WeaponType.SingleHanded;
    public GameObject weaponPrefab;
    public AttackData[] attackDatas;
    public float dashSpeed, counterAttackDashSpeed;
    public float DashStopOffset, CounterAttackDashStopOffset, CounterAttackSideOffset;
    public CombatStats combatStats;    
}
