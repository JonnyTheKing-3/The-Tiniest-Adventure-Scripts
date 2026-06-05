using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [System.Serializable]
    public class EquippedCustomizationEntry
    {
        public CustomizationSlot slot;
        public CustomizationItemData item;
    }

    [Header("References")]
    [SerializeField] private PlayerCustomizationSockets customizationSockets;

    [Header("Equipped Weapons")]
    public WeaponItemData equippedMainHandWeapon;
    public WeaponItemData equippedOffHandWeapon;

    [Header("Equipped Customization")]
    public List<EquippedCustomizationEntry> equippedCustomization = new();

    private Player player;
    private PlayerInventory inventory;
    private PlayerCombat playerCombat;
    private PlayerAnimation playerAnimation;

    private CombatStats appliedEquipmentStats;
    private readonly Dictionary<CustomizationSlot, GameObject> spawnedCustomizationVisuals = new(); // Managed in RefreshCustomizationVisual
    private readonly Dictionary<CustomizationSlot, GameObject> spawnedPreviewCustomizationVisuals = new();

    private void Awake()
    {
        player = GetComponent<Player>();
        inventory = GetComponent<PlayerInventory>();
        playerCombat = GetComponentInChildren<PlayerCombat>();
        playerAnimation = GetComponentInChildren<PlayerAnimation>();

        if (customizationSockets == null)
            customizationSockets = GetComponent<PlayerCustomizationSockets>();

        if (playerCombat != null && playerCombat.currentWeapon == null)
            playerCombat.currentWeapon = new Weapon();
    }

    private void Start()
    {
        // RefreshAllEquipment();
    }

    public void RefreshAllEquipment()
    {
        RefreshWeaponEquipment();
        RefreshAllCustomizationVisuals();
    }

    public void SmartEquip(InventoryItemData itemData)
    {
        // Debug.Log("Smart equipping item: " + itemData.name);
        if (itemData is WeaponItemData wpnItmData)
        {
            // Unequip if already equipped
            WeaponItemData currentWeapon = GetActiveCombatWeaponItem();
            if (currentWeapon == wpnItmData)
            {
                if (currentWeapon.weaponData.weaponType == WeaponData.WeaponType.SecondWeapon)
                    UnequipOffHandWeapon();
                else
                    UnequipMainHandWeapon();

                return;
            }

            // Equip if not equipped
            if (wpnItmData.canEquipInMainHand && wpnItmData.weaponData.weaponType != WeaponData.WeaponType.SecondWeapon)
                EquipMainHandWeapon(wpnItmData);   
            else if (wpnItmData.canEquipInOffHand && wpnItmData.weaponData.weaponType == WeaponData.WeaponType.SecondWeapon)
                EquipOffHandWeapon(wpnItmData);

        }

        else if (itemData is CustomizationItemData customItmData)
        {
            if (GetEquippedCustomization(customItmData.slot) == customItmData)
                UnequipCustomization(customItmData.slot);
            else
                EquipCustomization(customItmData);
        }
    }

    // Weapons
    public bool EquipMainHandWeapon(WeaponItemData item)
    {
        // Debug.Log($"Attempting to equip {item?.name} in main hand from PlayerEquipment");
        if (!CanEquipMainHand(item))
        {
            AudioManager.Instance.PlayUIButtonClick(transform);
            return false;
        }

        AudioManager.Instance.PlayUIEquip(transform);
        equippedMainHandWeapon = item;
        equippedOffHandWeapon = null;

        RefreshWeaponEquipment();
        return true;
    }

    public bool EquipOffHandWeapon(WeaponItemData item)
    {
        // Debug.Log($"Attempting to equip {item?.name} in off hand from PlayerEquipment");
        if (!CanEquipOffHand(item))
        {
            AudioManager.Instance.PlayUIButtonClick(transform);
            return false;
        }

        AudioManager.Instance.PlayUIEquip(transform);
        equippedOffHandWeapon = item;
        RefreshWeaponEquipment();
        return true;
    }

    public void UnequipMainHandWeapon()
    {
        // Debug.Log("Unequipping main hand weapon in PlayerEquipment");
        AudioManager.Instance.PlayUIEquip(transform);
        equippedMainHandWeapon = null;
        equippedOffHandWeapon = null;
        RefreshWeaponEquipment();
    }

    public void UnequipOffHandWeapon()
    {
        // Debug.Log("Unequipping off hand weapon in PlayerEquipment");
        AudioManager.Instance.PlayUIEquip(transform);
        equippedOffHandWeapon = null;
        RefreshWeaponEquipment();
    }

    public WeaponItemData GetActiveCombatWeaponItem()
    {
        return equippedOffHandWeapon != null ? equippedOffHandWeapon : equippedMainHandWeapon;
    }

    private bool CanEquipMainHand(WeaponItemData item)
    {
        return item != null
            && item.weaponData != null
            && item.canEquipInMainHand
            && item.weaponData.weaponType != WeaponData.WeaponType.SecondWeapon
            && inventory != null
            && inventory.OwnsItem(item);
    }

    private bool CanEquipOffHand(WeaponItemData item)
    {
        if (item == null || item.weaponData == null || !item.canEquipInOffHand)
            return false;

        if (inventory == null || !inventory.OwnsItem(item))
            return false;

        if (item.weaponData.weaponType != WeaponData.WeaponType.SecondWeapon)
            return false;

        if (equippedMainHandWeapon == null || equippedMainHandWeapon.weaponData == null)
            return false;

        if (equippedMainHandWeapon.weaponData.weaponType != WeaponData.WeaponType.SingleHanded)
            return false;

        return true;
    }

    private bool CanKeepCurrentOffHand() // Might use later
    {
        return equippedOffHandWeapon != null && CanEquipOffHand(equippedOffHandWeapon);
    }

    private void RefreshWeaponEquipment()
    {
        // Debug.Log("Refreshing weapon");
        if (player == null || playerCombat == null || playerAnimation == null)
            return;

        RemovePreviouslyAppliedEquipmentStats();
        DeleteWeaponVisuals();

        WeaponItemData activeCombatWeaponItem = GetActiveCombatWeaponItem();

        // If no weapon equipped, clear weapon template and colliders, then exit
        if (activeCombatWeaponItem == null || activeCombatWeaponItem.weaponData == null)
        {
            // Debug.Log("No active combat weapon equipped in PlayerEquipment. Clearing weapon template and colliders.");
            playerCombat.currentWeapon.weaponTemplate = null;
            playerCombat.currentWeapon.ClearWeaponColliders();
            playerCombat.currentWeapon.EnableWeaponColliders(false);
            return;
        }

        // Debug.Log($"Equipped weapon: {activeCombatWeaponItem.name} in PlayerEquipment. Applying stats and visuals.");

        // Weapon is equpped, so spawn visuals, set template, register colliders, and apply stats below
        InventoryMenuPlayerPreviewManager previewManager = InventoryMenuPlayerPreviewManager.Instance;

        if (equippedMainHandWeapon != null && equippedMainHandWeapon.weaponData != null)
        {
            SpawnWeaponVisual(equippedMainHandWeapon.weaponData, player.R_Hand, rotateOffHand: false);
            if (previewManager != null)
                SpawnWeaponVisual(equippedMainHandWeapon.weaponData, previewManager.R_Hand, rotateOffHand: false, SetSameLayerAsParent: true);
        }

        if (equippedOffHandWeapon != null && equippedOffHandWeapon.weaponData != null)
        {
            SpawnWeaponVisual(equippedOffHandWeapon.weaponData, player.L_Hand, rotateOffHand: true);
            if (previewManager != null)
                SpawnWeaponVisual(equippedOffHandWeapon.weaponData, previewManager.L_Hand, rotateOffHand: true, SetSameLayerAsParent: true);
        }

        playerCombat.currentWeapon.weaponTemplate = activeCombatWeaponItem.weaponData;
        playerAnimation.animator.runtimeAnimatorController = activeCombatWeaponItem.weaponData.AOC;

        // Making sure the Player Preview in the Inventory Menu has the right animation
        previewManager?.SmartSetIdleType();

        playerCombat.currentWeapon.ClearWeaponColliders();
        playerCombat.currentWeapon.RegisterWeaponColliders(player.weaponHoldPoints);
        playerCombat.currentWeapon.EnableWeaponColliders(false);

        appliedEquipmentStats = activeCombatWeaponItem.weaponData.combatStats;
        player.weaponCombatStats += appliedEquipmentStats;
    }

    private void RemovePreviouslyAppliedEquipmentStats()
    {
        if (player == null) return;

        player.weaponCombatStats -= appliedEquipmentStats;
        appliedEquipmentStats = default;
    }

    private void SpawnWeaponVisual(WeaponData weaponData, Transform parent, bool rotateOffHand, bool SetSameLayerAsParent = false)
    {
        if (weaponData == null || weaponData.weaponPrefab == null || parent == null)
            return;

        GameObject newWeaponObj = Instantiate(weaponData.weaponPrefab, parent);

        newWeaponObj.transform.localPosition = Vector3.zero;
        newWeaponObj.transform.localRotation = rotateOffHand ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;

        newWeaponObj.transform.SetAsFirstSibling();

        Player.Instance._playerAnimation.bowAnimator.gameObject.SetActive(false); // Need this in case we are wielding bow
        SetPreviewBowActive(false);

        if (SetSameLayerAsParent) newWeaponObj.layer = parent.gameObject.layer; // Need to set as the same layer because if not, the camera won't render it
    }

    public void DeleteWeaponVisuals()
    {
        DeleteChildren(player.R_Hand);
        DeleteChildren(player.L_Hand);

        InventoryMenuPlayerPreviewManager previewManager = InventoryMenuPlayerPreviewManager.Instance;
        if (previewManager == null) return;

        DeleteChildren(previewManager.R_Hand);
        DeleteChildren(previewManager.L_Hand);
    }

    public void SetPreviewBowActive(bool active)
    {
        InventoryMenuPlayerPreviewManager.Instance?.SetBowActive(active);
    }

    private void DeleteChildren(Transform parent)
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;

            if (!child.CompareTag("Bow")) Destroy(child.gameObject);
        }
    }


    // Customization
    public bool EquipCustomization(CustomizationItemData item)
    {
        if (item == null || inventory == null || !inventory.OwnsItem(item))
            return false;

        // Find the right slot and equip the item there. If there's already an item in that slot, replace it and return
        for (int i = 0; i < equippedCustomization.Count; i++)
        {
            if (equippedCustomization[i].slot == item.slot)
            {
                equippedCustomization[i].item = item;
                RefreshCustomizationVisual(item.slot);
                return true;
            }
        }


        // Slot wasn't found in currently equipped customization, so add a new entry for it
        equippedCustomization.Add(new EquippedCustomizationEntry
        {
            slot = item.slot,
            item = item
        });

        AudioManager.Instance.PlayUIEquip(transform);
        RefreshCustomizationVisual(item.slot);
        return true;
    }

    public void UnequipCustomization(CustomizationSlot slot)
    {
        for (int i = equippedCustomization.Count - 1; i >= 0; i--)
        {
            if (equippedCustomization[i].slot == slot)
                equippedCustomization.RemoveAt(i);
        }

        AudioManager.Instance.PlayUIEquip(transform);
        RefreshCustomizationVisual(slot);
    }

    public CustomizationItemData GetEquippedCustomization(CustomizationSlot slot)
    {
        foreach (EquippedCustomizationEntry entry in equippedCustomization)
        {
            if (entry.slot == slot)
                return entry.item;
        }

        return null;
    }

    private void RefreshAllCustomizationVisuals()
    {
        foreach (CustomizationSlot slot in System.Enum.GetValues(typeof(CustomizationSlot)))
            RefreshCustomizationVisual(slot);
    }

    private void RefreshCustomizationVisual(CustomizationSlot slot)
    {
        RefreshCustomizationVisualOnSockets(slot, customizationSockets, spawnedCustomizationVisuals);

        InventoryMenuPlayerPreviewManager previewManager = InventoryMenuPlayerPreviewManager.Instance;
        if (previewManager != null)
            RefreshCustomizationVisualOnSockets(slot, previewManager.customizationSockets, spawnedPreviewCustomizationVisuals, true);
    }

    private void RefreshCustomizationVisualOnSockets(CustomizationSlot slot, PlayerCustomizationSockets sockets, Dictionary<CustomizationSlot, GameObject> spawnedVisuals, bool setSameLayerAsSocket = false)
    {
        if (sockets == null)
            return;

        // Delete old visual for this slot if it exists
        if (spawnedVisuals.TryGetValue(slot, out GameObject oldObj) && oldObj != null)
            Destroy(oldObj);

        spawnedVisuals.Remove(slot);

        // if no item equipped in this slot, we're done after deleting old visual
        CustomizationItemData equippedItem = GetEquippedCustomization(slot);
        if (equippedItem == null || equippedItem.visualPrefab == null)
            return;

        Transform socket = sockets.GetSocket(slot);
        if (socket == null)
        {
            Debug.LogWarning($"No socket assigned for customization slot {slot} on {name}");
            return;
        }

        // Spawn new visual for this slot and add it to the dictionary so we can delete it later when we unequip/replace the item in this slot
        GameObject newObj = Instantiate(equippedItem.visualPrefab, socket);
        newObj.transform.localPosition = Vector3.zero;
        newObj.transform.localRotation = Quaternion.Euler(90f, 0f, 90f);
        newObj.transform.localScale = Vector3.one;

        if (setSameLayerAsSocket)
            SetLayerRecursively(newObj, socket.gameObject.layer);

        spawnedVisuals[slot] = newObj;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    public string GetEquippedMainHandWeaponId()
    {
        return equippedMainHandWeapon != null ? equippedMainHandWeapon.itemId : null;
    }

    public string GetEquippedOffHandWeaponId()
    {
        return equippedOffHandWeapon != null ? equippedOffHandWeapon.itemId : null;
    }

    public List<CustomizationSaveEntry> GetCustomizationSaveEntries()
    {
        List<CustomizationSaveEntry> saveEntries = new();

        foreach (EquippedCustomizationEntry entry in equippedCustomization)
        {
            if (entry.item == null || string.IsNullOrWhiteSpace(entry.item.itemId))
                continue;

            saveEntries.Add(new CustomizationSaveEntry
            {
                slot = entry.slot.ToString(),
                itemId = entry.item.itemId
            });
        }

        return saveEntries;
    }


    // Load from save data
    public void LoadEquipmentFromIds(string mainHandId, string offHandId, List<CustomizationSaveEntry> customizationEntries, ItemDatabase database)
    {
        equippedMainHandWeapon = null;
        equippedOffHandWeapon = null;
        equippedCustomization.Clear();

        if (database == null)
        {
            RefreshAllEquipment();
            return;
        }

        if (!string.IsNullOrWhiteSpace(mainHandId))
            equippedMainHandWeapon = database.GetItemById<WeaponItemData>(mainHandId);

        if (!string.IsNullOrWhiteSpace(offHandId))
            equippedOffHandWeapon = database.GetItemById<WeaponItemData>(offHandId);

        if (customizationEntries != null)
        {
            foreach (CustomizationSaveEntry entry in customizationEntries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.itemId) || string.IsNullOrWhiteSpace(entry.slot))
                    continue;

                CustomizationItemData item = database.GetItemById<CustomizationItemData>(entry.itemId);
                if (item == null)
                    continue;

                if (System.Enum.TryParse(entry.slot, out CustomizationSlot slot))
                {
                    equippedCustomization.Add(new EquippedCustomizationEntry // this list management already makes sure we only save/load one item per slot, so no need to check for duplicates here
                    {
                        slot = slot,
                        item = item
                    });
                }
            }
        }

        RefreshAllEquipment();
    }
}
