using UnityEngine;

public enum CustomizationSlot { Head, Body, Back, Accessory }

//
[CreateAssetMenu(fileName = "CustomizationItem", menuName = "Inventory/Customization Item")]
public class CustomizationItemData : InventoryItemData
{
    [Header("Customization")]
    public CustomizationSlot slot;

    [Tooltip("Prefab that gets attached to the socket when equipped")]
    public GameObject visualPrefab;
}
