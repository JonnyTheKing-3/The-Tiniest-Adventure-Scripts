using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private List<InventoryItemData> ownedItems = new(); // Need this to serialize owned items in inspector and for easy iteration
    private readonly HashSet<string> ownedItemIds = new(); // Need this for quick lookups to prevent duplicates and check ownership. Should be kept in sync with ownedItems

    public IReadOnlyList<InventoryItemData> OwnedItems => ownedItems;

    private void Awake()
    {
        RebuildLookup();
    }

    public void RebuildLookup()     // Cleans up ownedItems and rebuilds ownedItemIds to match. Must be called whenever ownedItems is modified outside of Add/RemoveItem methods
    {
        // Debug.Log($"Rebuilding inventory lookup");
        ownedItemIds.Clear();

        for (int i = ownedItems.Count - 1; i >= 0; i--)
        {
            InventoryItemData item = ownedItems[i];

            if (item == null)
            {
                ownedItems.RemoveAt(i);
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.itemId))
            {
                Debug.LogWarning($"Inventory item on {name} is missing an itemId: {item.name}");
                ownedItems.RemoveAt(i);
                continue;
            }

            if (!ownedItemIds.Add(item.itemId))
            {
                Debug.LogWarning($"Duplicate unique item found in inventory and removed: {item.itemId}");
                ownedItems.RemoveAt(i);
            }
        }
    }

    public bool OwnsItem(InventoryItemData item)
    {
        return item != null && ownedItemIds.Contains(item.itemId);
    }

    public bool OwnsItem(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && ownedItemIds.Contains(itemId);
    }

    public bool AddItem(InventoryItemData item)
    {
        if (item == null) return false;

        if (string.IsNullOrWhiteSpace(item.itemId))
        {
            Debug.LogWarning($"Tried to add item with no itemId: {item.name}");
            return false;
        }

        if (ownedItemIds.Contains(item.itemId))
            return false;

        ownedItems.Add(item);
        ownedItemIds.Add(item.itemId);
        return true;
    }

    public bool RemoveItem(InventoryItemData item)
    {
        if (item == null) return false;
        if (!ownedItemIds.Remove(item.itemId)) return false;

        ownedItems.Remove(item);
        return true;
    }

    public List<T> GetOwnedItemsOfType<T>() where T : InventoryItemData
    {
        List<T> results = new();

        foreach (InventoryItemData item in ownedItems)
        {
            if (item is T typedItem)
                results.Add(typedItem);
        }

        return results;
    }

    public int GetDatabaseItemCountOfType<T>() where T : InventoryItemData
    {
        ItemDatabase database = SaveGameManager.Instance.itemDatabase;
        if (database == null || database.allItems == null) 
        {
            Debug.LogWarning("Database not found when getting county for Item of type in PlayerInventory class");
            return -1;
        }

        int count = 0;
        foreach (InventoryItemData item in database.allItems)
        {
            if (item is T) count++;
        }

        return count;
    }

    public List<string> GetOwnedItemIds()
    {
        List<string> ids = new();

        foreach (InventoryItemData item in ownedItems)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.itemId))
                ids.Add(item.itemId);
        }

        return ids;
    }

    public void LoadFromItemIds(List<string> itemIds, ItemDatabase database)
    {
        ownedItems.Clear();
        ownedItemIds.Clear();

        if (itemIds == null || database == null)
            return;

        foreach (string id in itemIds)
        {
            InventoryItemData item = database.GetItemById(id);
            if (item == null)
            {
                Debug.LogWarning($"Could not find inventory item for saved id: {id}");
                continue;
            }

            ownedItems.Add(item);
            ownedItemIds.Add(item.itemId);
        }

        RebuildLookup();
    }
}
