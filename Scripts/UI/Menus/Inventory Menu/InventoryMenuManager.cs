using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class InventoryMenuManager : PauseMenuTabBase
{
    PauseMenuManager pauseMenuManager => PauseMenuManager.Instance;
    float tabSelectedAlpha => pauseMenuManager.TabSelectedAlpha;
    float tabUnselectedAlpha => pauseMenuManager.TabUnselectedAlpha;
    float tabSelectFadeDuration => pauseMenuManager.tabSelectFadeDuration;

    public enum InventorytTabSelection { Weapons, Customization }

    private GridLayoutGroup grid;
    private UIAutoSelectButtons autoSelectButtons;

    private readonly List<WeaponItemData> ownedWeapons = new();
    private readonly List<CustomizationItemData> ownedCustomizations = new();

    public InventorytTabSelection currentTabSelected = InventorytTabSelection.Weapons;

    [SerializeField] private Camera PlayerPreviewCamera;    
    [SerializeField] private GameObject PlayerPreviewRoot;
    [SerializeField] private GameObject itemButtonPrefab;
    public Image weaponTabImage;
    public Image customizationTabImage;
    public TMP_Text descriptionText;

    void Awake()
    {
        grid = GetComponentInChildren<GridLayoutGroup>();
        autoSelectButtons = GetComponentInChildren<UIAutoSelectButtons>();
    }

    void OnEnable()
    {
        Initialize();
        if (PlayerPreviewCamera) PlayerPreviewCamera.enabled = true;
        if (PlayerPreviewRoot) PlayerPreviewRoot.SetActive(true);

        pauseMenuManager.tabLeft.started += OnTabLeftStarted;
        pauseMenuManager.tabRight.started += OnTabRightStarted;
    }
    private void OnDisable()
    {
        if (PlayerPreviewCamera) PlayerPreviewCamera.enabled = false;
        if (PlayerPreviewRoot) PlayerPreviewRoot.SetActive(false);

        if (pauseMenuManager)
        {
            pauseMenuManager.tabLeft.started -= OnTabLeftStarted;
            pauseMenuManager.tabRight.started -= OnTabRightStarted;
        }
    }

    public void Initialize()
    {
        CacheOwnedItems();
        BuildCurrentTabButtons();
        SetAlphasTween();
    }

    void CacheOwnedItems()
    {
        ownedWeapons.Clear();
        ownedCustomizations.Clear();

        ownedWeapons.AddRange(Player.Instance._playerInventory.GetOwnedItemsOfType<WeaponItemData>());
        ownedCustomizations.AddRange(Player.Instance._playerInventory.GetOwnedItemsOfType<CustomizationItemData>());
    }

    void BuildCurrentTabButtons()
    {
        DestroyButtons();

        List<Button> visibleButtons = new();

        if (currentTabSelected == InventorytTabSelection.Weapons)
        {
            foreach (WeaponItemData item in ownedWeapons)
            {
                GameObject buttonObj = Instantiate(itemButtonPrefab, grid.transform);
                buttonObj.name = item.name;
                UIInventoryButton inventoryButton = buttonObj.GetComponent<UIInventoryButton>();
                inventoryButton.Initialize(item);

                Button button = buttonObj.GetComponent<Button>();
                visibleButtons.Add(button);
            }
        }
        else
        {
            foreach (CustomizationItemData item in ownedCustomizations)
            {
                GameObject buttonObj = Instantiate(itemButtonPrefab, grid.transform);
                buttonObj.name = item.name;
                UIInventoryButton inventoryButton = buttonObj.GetComponent<UIInventoryButton>();
                inventoryButton.Initialize(item);

                Button button = buttonObj.GetComponent<Button>();
                visibleButtons.Add(button);
            }
        }

        autoSelectButtons.RebindButtons(visibleButtons, true);
    }

    void DestroyButtons()
    {
        for (int i = grid.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(grid.transform.GetChild(i).gameObject);
        }
    }

    void OnTabLeftStarted(InputAction.CallbackContext context)
    {
        if (currentTabSelected == InventorytTabSelection.Weapons) return;

        currentTabSelected = InventorytTabSelection.Weapons;
        SetAlphasTween();
        BuildCurrentTabButtons();
    }

    void OnTabRightStarted(InputAction.CallbackContext context)
    {
        if (currentTabSelected == InventorytTabSelection.Customization) return;

        currentTabSelected = InventorytTabSelection.Customization;
        SetAlphasTween();
        BuildCurrentTabButtons();
    }

    Tween tabSwitchTween;
    void SetAlphasTween()
    {
        float weaponAlpha = currentTabSelected == InventorytTabSelection.Weapons ? tabSelectedAlpha : tabUnselectedAlpha;
        float customizationAlpha = currentTabSelected == InventorytTabSelection.Customization ? tabSelectedAlpha : tabUnselectedAlpha;

        weaponAlpha /= 100f;
        customizationAlpha /= 100f;

        tabSwitchTween?.Kill();
        tabSwitchTween = DOTween.Sequence()
            .Join(weaponTabImage.DOFade(weaponAlpha, tabSelectFadeDuration))
            .Join(customizationTabImage.DOFade(customizationAlpha, tabSelectFadeDuration))
            .SetUpdate(true);
    }

    public void SetDescriptionText(string text) => descriptionText.text = text;
}
