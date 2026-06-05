using System.Data;
using UnityEngine;

public interface IWeaponHolder
{
    /// <summary>
    /// Used to get the weapon data from the weapon holder. This structure allows player and enemies to share the same weapon logic and events.
    /// </summary>

    public Weapon GetWeapon();
    public void ResetHitList();
}
