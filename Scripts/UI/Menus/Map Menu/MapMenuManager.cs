using TMPro;
using UnityEngine;

public class MapMenuManager : PauseMenuTabBase
{
    PlayerInventory p_inventory => Player.Instance._playerInventory;
    public Camera mapCamera;
    [SerializeField] private Color completedTextColor = Color.yellow;
    [SerializeField] private TMP_Text ObtainedBowAnswerTxt;
    [SerializeField] private TMP_Text NumberOfWeaponsTxt;
    [SerializeField] private TMP_Text NumberOfCustomiationsTxt;
    private Color obtainedBowDefaultColor;
    private Color numberOfWeaponsDefaultColor;
    private Color numberOfCustomizationsDefaultColor;

    void Awake()
    {
        obtainedBowDefaultColor = ObtainedBowAnswerTxt.color;
        numberOfWeaponsDefaultColor = NumberOfWeaponsTxt.color;
        numberOfCustomizationsDefaultColor = NumberOfCustomiationsTxt.color;
    }
    
    void OnEnable()
    {
        mapCamera.enabled = true;
        mapCamera.Render();
        mapCamera.enabled = false;

        // Set Quest Log
        ObtainedBowAnswerTxt.text = Player.Instance.OwnsBow ? "YES!" : "no...";
        ObtainedBowAnswerTxt.color = Player.Instance.OwnsBow ? completedTextColor : obtainedBowDefaultColor;

        int ownedWeapons = p_inventory.GetOwnedItemsOfType<WeaponItemData>().Count;
        int totalWeapons = p_inventory.GetDatabaseItemCountOfType<WeaponItemData>();
        int ownedCustomizations = p_inventory.GetOwnedItemsOfType<CustomizationItemData>().Count;
        int totalCustomizations = p_inventory.GetDatabaseItemCountOfType<CustomizationItemData>();

        NumberOfWeaponsTxt.text = ownedWeapons.ToString();
        NumberOfWeaponsTxt.color = totalWeapons > 0 && ownedWeapons == totalWeapons ? completedTextColor : numberOfWeaponsDefaultColor;
        NumberOfCustomiationsTxt.text = ownedCustomizations.ToString();
        NumberOfCustomiationsTxt.color = totalCustomizations > 0 && ownedCustomizations == totalCustomizations ? completedTextColor : numberOfCustomizationsDefaultColor;
    }
}
