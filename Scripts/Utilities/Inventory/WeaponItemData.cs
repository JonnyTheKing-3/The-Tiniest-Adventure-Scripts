using UnityEngine;

[CreateAssetMenu(fileName = "WeaponItem", menuName = "Inventory/Weapon Item")]
public class WeaponItemData : InventoryItemData
{//
    [Header("Weapon Data")
    ]
    public WeaponData weaponData;

    //WE DON'T NEED THESE BELOW, but I might use this code base for future projects, so I'm leaving it in for now because in other projects I might want some weapons on either hand and stuff.
    [Header("Equip Rules")]
    public bool canEquipInMainHand = true;
    public bool canEquipInOffHand = false;
}
