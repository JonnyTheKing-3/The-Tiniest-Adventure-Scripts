using UnityEngine;

public enum InventoryItemType { Weapon, Customization, KeyItem }

public abstract class InventoryItemData : ScriptableObject
{
    [Header("Core Item Info")]
    public string itemId;
    public string displayName;//
     [TextArea] public string description;

    public Sprite icon;
    public InventoryItemType itemType;


    // private void OnValidate() / 
    // {
    //     if (this is WeaponItemData)             itemType = InventoryItemType.Weapon;
    //     else if (this is CustomizationItemData) itemType = InventoryItemType.Customization;
    //     else if (this is KeyItemData)           itemType = InventoryItemType.KeyItem;

    //     if (string.IsNullOrWhiteSpace(itemId))  itemId = name.Replace(" ", "_").ToLower();
    // }
}
