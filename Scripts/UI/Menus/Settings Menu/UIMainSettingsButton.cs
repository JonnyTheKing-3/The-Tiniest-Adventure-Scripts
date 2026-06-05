using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using NUnit.Framework;

public class UIMainSettingsButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    SettingsMenuManager settingsMenuManager => PauseMenuManager.Instance.GetComponentInChildren<SettingsMenuManager>();
    float slideDuration => PauseMenuManager.Instance.CursorSlideDuration;
    

    private Button btn;
    private TMP_Text text;
    public GameObject associatedPanel; // Panel to show when this button is selected
    public SettingsMenuManager.PanelButtons associatedPanelButtons; // Enum value to identify which panel this button is associated with

    void Start()
    {
        btn = GetComponent<Button>();
        text = GetComponentInChildren<TMP_Text>();
        btn.onClick.AddListener(OnClick);
    }
    
    void OnClick()
    {
        // Debug.Log("Clicked: " + gameObject.name);
        AudioManager.Instance.PlayUIButtonClick(transform);

        ActivatePanel();
        settingsMenuManager.RebindButtonsToPanel(associatedPanelButtons);
    }

    public void OnSelect(BaseEventData eventData) 
    {
        settingsMenuManager.TweenCursorPosition(settingsMenuManager.MainCursor, GetComponent<RectTransform>(), slideDuration);
        TweenTextColor(Color.black, slideDuration);

        if (GameManager.Instance._currPlayMode == GameManager.PlayMode.Menu && PauseMenuManager.Instance.currentMenuIndex == 3) // Only play sound if this is the current menu
        {
            if (!PauseMenuManager.Instance.suppressNextSelectSound)
            {
                AudioManager.Instance.PlayUIButtonClick(transform);
            }
        }
    }

    public void OnDeselect(BaseEventData eventData)  
    {
        TweenTextColor(Color.white, slideDuration);
    }

    Tween textColorTween;
    public void TweenTextColor(Color targetColor, float duration)
    {
        textColorTween?.Kill();
        textColorTween = text.DOColor(targetColor, duration).SetUpdate(true);
    }

    void ActivatePanel()
    {
        if (settingsMenuManager.panelOpen != null) settingsMenuManager.panelOpen.SetActive(false);
        
        if (associatedPanel == null) return;
        settingsMenuManager.panelOpen = associatedPanel;
        settingsMenuManager.currentPanel = associatedPanelButtons;
        associatedPanel.SetActive(true);
    }
}
