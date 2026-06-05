using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class GameOverUIManager : MonoBehaviour
{
    public static GameOverUIManager Instance;
    CanvasGroup canvasGroup;
    Tween canvasFadeTween;
    UIAutoSelectButtons UI_AutoSelectButtons;


    public float fadeDuration = 2f;
    public List<Button> GameOverButtons = new List<Button>();
    [Range(0f, 1f)] public float DeSelectedButtonAlpha;
    public float buttonAlphaFadeDuration;

    void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        UI_AutoSelectButtons = GetComponentInChildren<UIAutoSelectButtons>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }


    public void GameOverUIRoutine(bool EnteringGameOver, float? duration = null)
    {
        float dur = duration.HasValue ? duration.Value : fadeDuration;
        FadeCanvasGroupAndActivateButtons(EnteringGameOver, dur);
    }

    void FadeCanvasGroupAndActivateButtons(bool fadeIn, float duration)
    {
        float targetAlpha = fadeIn ? 1f : 0f;

        canvasFadeTween?.Kill();

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            canvasGroupCleanUp(fadeIn);
            return;
        }

        canvasFadeTween = canvasGroup.DOFade(targetAlpha, duration).SetEase(Ease.InOutQuad).OnComplete(() => canvasGroupCleanUp(fadeIn));
    }

    void canvasGroupCleanUp(bool fadeIn)
    {
        canvasGroup.interactable = fadeIn;
        canvasGroup.blocksRaycasts = fadeIn;
            
        if (fadeIn)
            UI_AutoSelectButtons.RebindButtons(GameOverButtons, true);

        GameManager.Instance.RunInBackgroundEditorToggle(fadeIn);
    }

    public void ContinueButtonClicked() => GameManager.Instance.GameOver(false);
    public void GiveUpButtonClicked() => GameManager.Instance.QuitGame();

}
