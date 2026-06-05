using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISkillTreeButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    // Goes on button

    SkillTreeMenuManager skillTreeManager => PauseMenuManager.Instance.GetComponentInChildren<SkillTreeMenuManager>();
    public enum SkillTreeButtonTypes { DashAttack, PerfectDodge, ATK, DEF }


    public SkillTreeButtonTypes buttonType;
    public bool isFirstButtonInRow;
    [HideInInspector] public Button btn;
    private Image img;
    [SerializeField] private Button nextButtonInRow;
    [SerializeField] private GameObject UnlockedImageGameObject;
    [TextArea] public string buttonDescription;
    [HideInInspector] public bool isSkillUnlocked;


    void Awake()
    {
        btn = GetComponent<Button>();
        img = GetComponent<Image>();
        btn.onClick.AddListener(OnButtonClicked);
    }


    void OnButtonClicked()
    {
        if (isSkillUnlocked || Player.Instance.SkillTreePoints <= 0) 
        {
            AudioManager.Instance.PlayUIButtonClick(transform);
            return;
        }
        else
        {
            AudioManager.Instance.PlayUIEquip(transform);
        }

        Player.Instance.SkillTreePoints--;
        isSkillUnlocked = true;
        UnlockButton();
        skillTreeManager.SkillTreeButtonClicked(buttonType);
    }
    public void OnSelect(BaseEventData eventData) 
    { 
        skillTreeManager.SetDescriptionText(buttonDescription);
        SetbuttonVisuals(PauseMenuManager.Instance.buttonSelectedSprite);

        if (!PauseMenuManager.Instance.suppressNextSelectSound)
        {
            AudioManager.Instance.PlayUIButtonClick(transform);
        }
    }
    public void OnDeselect(BaseEventData eventData)  
    {
        SetbuttonVisuals(PauseMenuManager.Instance.buttonUnselectedSprite);
    }
    
    
    void SetbuttonVisuals(Sprite sprite) => img.sprite = sprite;
    public void SetOriginalButtonAlpha() // Used for SkillTreeMenuManager.LoadTabMenuSettings
    {
        Color c = img.color;
        c.a = isFirstButtonInRow ? 1 : .55f;
        img.color = c;
    }
    public void UnlockButton() // Also used for SkillTreeMenuManager.LoadTabMenuSettings
    {
        isSkillUnlocked = true;
        UnlockedImageGameObject.SetActive(true);
        
        Color c = img.color;
        c.a = 100f;
        img.color = c;

        btn.navigation = new Navigation { mode = isFirstButtonInRow ? Navigation.Mode.Automatic : Navigation.Mode.Horizontal };
        if (nextButtonInRow != null)     
        {
            nextButtonInRow.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = btn };
            Color nbc = nextButtonInRow.image.color;
            nbc.a = 100f;
            nextButtonInRow.image.color = nbc;
        };
    }
    public void LockButton() // Used for SkillTreeMenuManager.LoadTabMenuSettings
    {
        isSkillUnlocked = false;
        UnlockedImageGameObject.SetActive(false);

        btn.navigation = new Navigation { mode = isFirstButtonInRow ? Navigation.Mode.Vertical : Navigation.Mode.None };
    }
}
