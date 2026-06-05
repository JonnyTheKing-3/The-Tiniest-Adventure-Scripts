using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<InventoryItemData> allItems = new(); // Need this to fill up lookup

    private Dictionary<string, InventoryItemData> lookup; // Need this for quick lookups by itemId

    private void OnEnable()
    {
        BuildLookup();
    }

    public void BuildLookup()
    {
        lookup = new Dictionary<string, InventoryItemData>();

        foreach (InventoryItemData item in allItems)
        {
            if (item == null) continue;
            if (string.IsNullOrWhiteSpace(item.itemId))
            {
                Debug.LogWarning($"Item with null or whitespace itemId found in ItemDatabase. Item name: {item.name}. Skipping item.");
                continue;
            }

            if (!lookup.ContainsKey(item.itemId))
                lookup.Add(item.itemId, item);
            else
                Debug.LogWarning($"Duplicate itemId in ItemDatabase: {item.itemId}");
        }
    }

    public InventoryItemData GetItemById(string itemId)
    {
        if (lookup == null || lookup.Count == 0)
            BuildLookup();

        if (string.IsNullOrWhiteSpace(itemId))
        {
            Debug.LogWarning($"GetItemById called with null or whitespace itemId. Returning null.");
            return null;
        }

        lookup.TryGetValue(itemId, out InventoryItemData item);
        return item;
    }

    public T GetItemById<T>(string itemId) where T : InventoryItemData
    {
        return GetItemById(itemId) as T;
    }
}
