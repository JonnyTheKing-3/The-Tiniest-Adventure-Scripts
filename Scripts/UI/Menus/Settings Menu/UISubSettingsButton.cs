using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using UnityEngine.Rendering;

public class UISubSettingsButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    SettingsMenuManager settingsMenuManager => PauseMenuManager.Instance.GetComponentInChildren<SettingsMenuManager>();
    float slideDuration => PauseMenuManager.Instance.CursorSlideDuration;
    public enum SettingsButtonTypes { Music, SFX, Graphics, None }
    
    private Button btn;
    [SerializeField] private TMP_Text text;
    [SerializeField] private TMP_Text value;
    [SerializeField] private Image leftarrow;
    [SerializeField] private Image rightarrow;

    int currentValue = 2;
    [SerializeField] private SettingsButtonTypes settingsButtonType = SettingsButtonTypes.None;
    public SettingsButtonTypes SettingsButtonType => settingsButtonType;

    void Start()
    {
        btn = GetComponent<Button>();
        text = GetComponentInChildren<TMP_Text>();

        if (settingsButtonType != SettingsButtonTypes.None)
        {
            value.text = settingsButtonType == SettingsButtonTypes.Graphics ? "High" : "10";
            currentValue = settingsButtonType == SettingsButtonTypes.Graphics ? 2 : 10;   
        }
    }

    void OnEnable() 
    {
        if (settingsMenuManager.currentPanel != SettingsMenuManager.PanelButtons.Settings) return;

        PauseMenuManager.Instance.navigate.started += OnNavigateStarted;
    }

    void OnDisable() 
    {
        PauseMenuManager.Instance.navigate.started -= OnNavigateStarted; 
    }

    void OnNavigateStarted(InputAction.CallbackContext ctx)
    {
        if (!isSelected) return;

        Vector2 input = ctx.ReadValue<Vector2>();
        if (Mathf.Abs(input.y) > .25f) return;
        
        AudioManager.Instance.PlayUIButtonClick(transform);

        int threshold = settingsButtonType == SettingsButtonTypes.Graphics ? 2 : 10;
        if (input.x > .25f)         currentValue = Mathf.Clamp(currentValue + 1, 0, threshold);
        else if (input.x < -.25f)   currentValue = Mathf.Clamp(currentValue - 1, 0, threshold);

        switch (settingsButtonType)
        {
            case SettingsButtonTypes.Music:     settingsMenuManager.UpdateVolume(value, currentValue, true);     break;
            case SettingsButtonTypes.SFX:       settingsMenuManager.UpdateVolume(value, currentValue, false);    break;
            case SettingsButtonTypes.Graphics:  settingsMenuManager.UpdateGraphics(value, currentValue);         break;

            default: break;
        }
    }
    
    private bool isSelected = false;
    public void OnSelect(BaseEventData eventData) 
    {
        isSelected = true;
        settingsMenuManager.TweenCursorPosition(settingsMenuManager.SubCursor, GetComponent<RectTransform>(), slideDuration);
        TweenColor(Color.black, slideDuration);

        if (!PauseMenuManager.Instance.suppressNextSelectSound)
        {
            AudioManager.Instance.PlayUIButtonClick(transform);
        }
    }

    public void OnDeselect(BaseEventData eventData)  
    {
        isSelected = false;
        TweenColor(Color.white, slideDuration);
    }

    Tween textColorTween, numberColorTween, leftArrowColorTween, rightArrowColorTween;
    public void TweenColor(Color targetColor, float duration)
    {
        textColorTween?.Kill();
        textColorTween = text.DOColor(targetColor, duration).SetUpdate(true);

        if (value != null) 
        {
            numberColorTween?.Kill();
            numberColorTween = value.DOColor(targetColor, duration).SetUpdate(true);
        }
        if (leftarrow != null) 
        {
            leftArrowColorTween?.Kill();
            leftArrowColorTween = leftarrow.DOColor(targetColor, duration).SetUpdate(true);
        }
        if (rightarrow != null) 
        {
            rightArrowColorTween?.Kill();
            rightArrowColorTween = rightarrow.DOColor(targetColor, duration).SetUpdate(true);
        }
    }



    public void LoadValue(float savedValue)
    {
        float threshold = settingsButtonType == SettingsButtonTypes.Graphics ? 2 : 10;
        currentValue = (int)Mathf.Clamp(savedValue, 0f, threshold);

        switch (settingsButtonType)
        {
            case SettingsButtonTypes.Music:
            case SettingsButtonTypes.SFX:
                value.text = currentValue.ToString();
                break;

            case SettingsButtonTypes.Graphics:
                switch (currentValue)
                {
                    case 0: value.text = "Low"; break;
                    case 1: value.text = "Medium"; break;
                    case 2: value.text = "High"; break;
                }
                break;
        }
    }
}
