using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }
    private CanvasGroup pauseCanvasGroup;
    
    // In case I need them
    [HideInInspector] public InventoryMenuManager inventoryMenuManager;
    [HideInInspector] public SkillTreeMenuManager skillTreeMenuManager;
    [HideInInspector] public SettingsMenuManager settingsMenuManager;
    [HideInInspector] public MapMenuManager mapMenuManager;
    

    [Header("Menus")]
    [SerializeField] private CanvasGroup[] menus;
    public int currentMenuIndex = 0;

    [Header("Sliding")]
    [SerializeField] private RectTransform menuContainer;
    [SerializeField] private float pageWidth = 1920f;
    [SerializeField] private float slideDuration = 0.35f;

    [Header("Extra")]
    public Sprite buttonSelectedSprite;
    public Sprite buttonUnselectedSprite;
    public float buttonSelectedAlpha = .7f;
    public float buttonUnselectedAlpha = 0.55f;
    [Space]
    public float TabSelectedAlpha = .7f;
    public float TabUnselectedAlpha = 0.55f;
    public float tabSelectFadeDuration = 0.3f;
    [Space]
    public float CursorSlideDuration = 0.25f;


    [HideInInspector] public InputAction navigate;
    [HideInInspector] public InputAction submit;
    [HideInInspector] public InputAction cancel; 
    [HideInInspector] public InputAction scrollLeft;
    [HideInInspector] public InputAction scrollRight;
    [HideInInspector] public InputAction tabLeft;
    [HideInInspector] public InputAction tabRight;
    [HideInInspector] public InputAction exit;
    private Tween slideTween;

    [HideInInspector] public bool suppressNextSelectSound = false; // Need this in order to not play the UIClickSound when scrolling tab menu pages (Skill Tree and Settings Menu, more specifically, their respective button scripts)

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        pauseCanvasGroup = GetComponent<CanvasGroup>();
        mapMenuManager = GetComponentInChildren<MapMenuManager>();
        inventoryMenuManager = GetComponentInChildren<InventoryMenuManager>();
        skillTreeMenuManager = GetComponentInChildren<SkillTreeMenuManager>();
        settingsMenuManager = GetComponentInChildren<SettingsMenuManager>();


        navigate = Player.Instance._playerUIInputManager.NavigateNonInputModuleAction;
        submit = Player.Instance._playerUIInputManager.SubmitAction;
        cancel = Player.Instance._playerUIInputManager.CancelAction;
        scrollLeft = Player.Instance._playerUIInputManager.ScrollLeftAction;
        scrollRight = Player.Instance._playerUIInputManager.ScrollRightAction;
        tabLeft = Player.Instance._playerUIInputManager.TabLeftAction;
        tabRight = Player.Instance._playerUIInputManager.TabRightAction;
        exit = Player.Instance._playerUIInputManager.ExitAction;

        scrollLeft.started += OnScrollLeftStarted;
        scrollRight.started += OnScrollRightStarted;
        exit.started += OnExitStarted;
        
        // Ensure all menus are inactive at start except the first one
        menuTweens = new Tween[menus.Length];
        pauseCanvasGroup.alpha = 0f;
        SetCanvasGroupActive(pauseCanvasGroup, false);
        SetCurrentMenu(0);
    }

    public void TogglePause(bool pause)
    {
        pauseCanvasGroup.alpha = pause ? 1f : 0f;
        SetCanvasGroupActive(pauseCanvasGroup, pause);

        if (pause) SetCurrentMenu(currentMenuIndex);
        else       SetCanvasGroupActive(menus[currentMenuIndex], false);

        if (menus[currentMenuIndex].TryGetComponent(out PauseMenuTabBase menu)) 
            menu.enableMenuManagerScript(pause);
        
        GameManager.Instance.RunInBackgroundEditorToggle(pause);
    }

    void OnScrollLeftStarted(InputAction.CallbackContext context) { if (GameManager.Instance._currPlayMode == GameManager.PlayMode.Menu) PreviousMenu(); }
    void OnScrollRightStarted(InputAction.CallbackContext context) { if (GameManager.Instance._currPlayMode == GameManager.PlayMode.Menu) NextMenu(); }
    void OnExitStarted(InputAction.CallbackContext context) { if (GameManager.Instance._currPlayMode == GameManager.PlayMode.Menu) GameManager.Instance.PauseRoutine(); }

    public void NextMenu()
    {
        AudioManager.Instance.PlayUIPageScroll(transform);
        int nextIndex = (currentMenuIndex + 1) % menus.Length; // Wrap around to first menu if at the end
        SetCurrentMenu(nextIndex, slideDuration);
    }

    public void PreviousMenu()
    {
        AudioManager.Instance.PlayUIPageScroll(transform);
        int prevIndex = (currentMenuIndex - 1 + menus.Length) % menus.Length; // Wrap around to last menu if at the beginning
        SetCurrentMenu(prevIndex, slideDuration);
    }

    private void SetCurrentMenu(int index, float duration = 0f)
    {
        currentMenuIndex = index;
        UpdateMenuStates();

        // Move container
        if (duration == 0f)
        {
            Vector2 anchoredPos = menuContainer.anchoredPosition;
            anchoredPos.x = -currentMenuIndex * pageWidth; // Sliding left makes the menus scroll right :) We need the 1.5 because if we slide the menu only page width amount, then hald of it will still show because we slid the midpoint of it to the middle, so half it still shows
            menuContainer.anchoredPosition = anchoredPos;
        }
        else
        {
            SlideToCurrentMenu(duration);
        }
    }

    private void UpdateMenuStates() // Sets the active menu's CanvasGroup to interactable and visible, while disabling others
    {
        for (int i = 0; i < menus.Length; i++)
        {
            bool isActiveMenu = i == currentMenuIndex;
            SetCanvasGroupActive(menus[i], isActiveMenu);
            SetAlphaSmoothly(menus[i], isActiveMenu, i);

            if (menus[i].TryGetComponent(out PauseMenuTabBase menu))
            {
                menu.enableMenuManagerScript(isActiveMenu);
            }
        }
    }

    private void SlideToCurrentMenu(float duration)
    {
        slideTween?.Kill();

        float targetX = -currentMenuIndex * pageWidth;
        slideTween = menuContainer.DOAnchorPosX(targetX, duration).SetEase(Ease.OutCubic).SetUpdate(true);
    }


    private void SetCanvasGroupActive(CanvasGroup cg, bool active)
    {
        cg.interactable = active;
        cg.blocksRaycasts = active;
    }

    private Tween[] menuTweens; // Assuming 2 menus, adjust if more
    private void SetAlphaSmoothly(CanvasGroup cg, bool active, int menuIndex)
    {
        menuTweens[menuIndex]?.Kill(); // Kill any existing tween for this menu
        menuTweens[menuIndex] = cg.DOFade(active ? 1f : 0f, slideDuration).SetEase(Ease.OutCubic).SetUpdate(true);
    }
}