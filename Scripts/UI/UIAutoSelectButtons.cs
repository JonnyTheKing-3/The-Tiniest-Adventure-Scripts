using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIAutoSelectButtons : MonoBehaviour
{
    // Component must on an object wheret the buttons are the child of it
    // Whatever calls this must be after OnEnable
    [SerializeField] private List<Button> buttons = new();

    private void OnEnable()
    {
        if (buttons == null || buttons.Count == 0)
            buttons = new List<Button>(GetComponentsInChildren<Button>(true));

        if (EventSystem.current == null)
        {
            Debug.LogError("No EventSystem in scene.");
            return;
        }

        StartCoroutine(SelectFirstNextFrame());
    }

    private void OnDisable()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private IEnumerator SelectFirstNextFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        SelectFirst();
    }

   public void SelectFirst()
    {
        if (buttons == null || buttons.Count == 0)
        {
            // Debug.LogWarning("No buttons to select.");
            return;
        }
    
        foreach (Button button in buttons)
        {
            // Debug.Log("Checking button: " + button.gameObject.name);
            if (button != null && button.isActiveAndEnabled && button.interactable)
            {
                EventSystem.current.SetSelectedGameObject(null);
                PauseMenuManager.Instance.suppressNextSelectSound = true;
                button.Select();
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                PauseMenuManager.Instance.suppressNextSelectSound = false;
                // Debug.Log("Selected first inventory button: " + button.gameObject.name);
                
                onFinishedAction?.Invoke();
                onFinishedAction = null;
                return;
            }
        }
    
        Debug.LogWarning("No valid interactable inventory button found.");
    }

    Action onFinishedAction;
    public void RebindButtons(List<Button> newButtons, bool selectFirst = true, Action onFinished = null)
    {
        // Debug.Log("Rebinding buttons. New button count: " + newButtons.Count);
        // Debug.Log("New buttons: " + string.Join(", ", newButtons.ConvertAll(b => b.gameObject.name)));

        onFinishedAction ??= onFinished;
        buttons = newButtons;

        

        if (selectFirst && isActiveAndEnabled)
            StartCoroutine(SelectFirstNextFrame());
    }
}