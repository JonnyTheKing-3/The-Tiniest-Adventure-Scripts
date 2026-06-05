using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

public class SettingsMenuManager : PauseMenuTabBase
{
    // The functionality functions for the OnClick events are at the bottom
    // UISettingsButton scripts take care of visuals, and settings buttons call these events 

    public enum PanelButtons { Main, Settings, System }


    public Image MainCursor;
    public Image SubCursor;
    public UIAutoSelectButtons autoSelectButtons;
    public UIAutoSelectButtons SettingsAutoSelectButtons;
    public UIAutoSelectButtons SystemAutoSelectButtons;
    [Space]
    public List<Button> MainContainerButtons = new List<Button>();
    public List<Button> SettingsPanelButtons = new List<Button>();
    public List<Button> SystemPanelButtons = new List<Button>();
    [Space]
    public GameObject panelOpen;
    [HideInInspector] public PanelButtons currentPanel;


    Navigation enabledNav, disabledNav;

    void Awake()
    {
        enabledNav = new Navigation { mode = Navigation.Mode.Vertical };
        disabledNav = new Navigation { mode = Navigation.Mode.None };
    }

    void OnEnable() 
    {
        SubCursor.enabled = false;
        RebindButtonsToPanel(PanelButtons.Main);
        PauseMenuManager.Instance.cancel.started += OnCancelStarted;
    }

    void OnDisable()
    {
        PauseMenuManager.Instance.cancel.started -= OnCancelStarted;
    }

    void OnCancelStarted(InputAction.CallbackContext ctx)
    {
        if (currentPanel == PanelButtons.Main) return;

        RebindButtonsToPanel(PanelButtons.Main);
        AudioManager.Instance.PlayUIButtonClick(transform);
    }

    Tween cursorMoveTween;
    public void TweenCursorPosition(Image cursor, RectTransform target, float duration)
    {
        cursorMoveTween?.Kill();

        // Translate target world position to local position relative to cursor's parent
        Vector2 targetPos;
        float Yoffset = -10f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cursor.rectTransform.parent as RectTransform,
            RectTransformUtility.WorldToScreenPoint(null, target.position + (Vector3.up * Yoffset)),
            null,
            out targetPos
        );

        cursorMoveTween = cursor.rectTransform.DOAnchorPos(targetPos, duration).SetUpdate(true).SetEase(Ease.OutQuad);
    }

    public void RebindButtonsToPanel(PanelButtons panel)
    {
        currentPanel = panel;

        switch (panel)
        {
            case PanelButtons.Main:      
                foreach (var btn in MainContainerButtons) btn.navigation = enabledNav;
                foreach (var btn in SettingsPanelButtons) btn.navigation = disabledNav;
                foreach (var btn in SystemPanelButtons) btn.navigation = disabledNav;
                autoSelectButtons.RebindButtons(MainContainerButtons, true, () => CursorVisibility(panel)); 
                break;
            case PanelButtons.Settings:  
                foreach (var btn in MainContainerButtons) btn.navigation = disabledNav;
                foreach (var btn in SettingsPanelButtons) btn.navigation = enabledNav;
                foreach (var btn in SystemPanelButtons) btn.navigation = disabledNav;
                SettingsAutoSelectButtons.RebindButtons(SettingsPanelButtons, true, () => CursorVisibility(panel)); 
                break;
            case PanelButtons.System:    
                foreach (var btn in MainContainerButtons) btn.navigation = disabledNav;
                foreach (var btn in SettingsPanelButtons) btn.navigation = disabledNav;
                foreach (var btn in SystemPanelButtons) btn.navigation = enabledNav;

                SystemAutoSelectButtons.RebindButtons(SystemPanelButtons, true, () => CursorVisibility(panel));   
                break;
        }
    }

    void CursorVisibility(PanelButtons panel)
    {
        MainCursor.enabled = panel == PanelButtons.Main; 
        SubCursor.enabled = panel != PanelButtons.Main;
    }


    public void LoadSettingsUI(float musicVolume, float sfxVolume, int graphicsQuality)
    {
        foreach (Button button in SettingsPanelButtons)
        {
            UISubSettingsButton settingsButton = button.GetComponent<UISubSettingsButton>();
            if (settingsButton == null) continue;

            switch (settingsButton.SettingsButtonType)
            {
                case UISubSettingsButton.SettingsButtonTypes.Music:
                    settingsButton.LoadValue(musicVolume);
                    break;

                case UISubSettingsButton.SettingsButtonTypes.SFX:
                    settingsButton.LoadValue(sfxVolume);
                    break;

                case UISubSettingsButton.SettingsButtonTypes.Graphics:
                    settingsButton.LoadValue(graphicsQuality);
                    break;
            }
        }
    }


    // Settings Button effects
    public void UpdateVolume(TMP_Text text, int value, bool musicVolume)
    {
        text.text = value.ToString();
        
        AudioManager.Instance.SetVolume(value, musicVolume);
    }

    public void UpdateGraphics(TMP_Text text, int value)
    {
        switch (value)
        {
            case 0: text.text = "Low"; break;
            case 1: text.text = "Medium"; break;
            case 2: text.text = "High"; break;
        }

        GameManager.Instance.SetGraphicsQuality(value);
    }


    // System Button effects
    public void SaveButtonClicked() { AudioManager.Instance.PlayUIButtonClick(transform); SaveGameManager.Instance.SaveGame(); }
    public void LoadButtonClicked() { AudioManager.Instance.PlayUIButtonClick(transform); SaveGameManager.Instance.LoadGame(); }
    public void DeleteSaveButtonClicked() { AudioManager.Instance.PlayUIEquip(transform); SaveSystem.DeleteSave(); }
    public void ExitButtonClicked() => GameManager.Instance.QuitGame();
}
