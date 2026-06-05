using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;

public class UIInventoryButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    // Goes on button
    InventoryMenuManager inventoryMenuManager => PauseMenuManager.Instance.GetComponentInChildren<InventoryMenuManager>();
    PlayerEquipment player => Player.Instance._playerEquipment;
    private TMP_Text TMPtext;
    private Button btn;
    private Image img;
    [SerializeField] private Image iconImage;
    [SerializeField] private InventoryItemData itemData;

    void Awake()
    {
        btn = GetComponent<Button>();
        img = GetComponent<Image>();
        TMPtext = GetComponentInChildren<TMP_Text>();
        btn.onClick.AddListener(OnButtonClicked);
    }

    void Start()
    {
        if (itemData.icon == null)
        {
            Color c = iconImage.color;
            c.a = 0f;
            iconImage.color = c;

            TMPtext.text = itemData.name;
        }
        else
        {
            iconImage.sprite = itemData.icon;
            TMPtext.text = "";
        }

    }

    public void Initialize(InventoryItemData data)
    {
        itemData = data;
    }

    void OnButtonClicked() => player.SmartEquip(itemData);

    public void OnSelect(BaseEventData eventData) 
    { 
        SetbuttonVisuals(PauseMenuManager.Instance.buttonSelectedAlpha, PauseMenuManager.Instance.buttonSelectedSprite);
        inventoryMenuManager.SetDescriptionText(itemData.description);

        if (!PauseMenuManager.Instance.suppressNextSelectSound)
        {
            AudioManager.Instance.PlayUIButtonClick(transform);
        }
    }
    public void OnDeselect(BaseEventData eventData)  
    {
        SetbuttonVisuals(PauseMenuManager.Instance.buttonUnselectedAlpha, PauseMenuManager.Instance.buttonUnselectedSprite);
    }

    void SetbuttonVisuals(float alpha, Sprite sprite)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
        img.sprite = sprite;
    }
}
