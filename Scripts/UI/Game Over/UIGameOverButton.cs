using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIGameOverButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    // Goes on button
    float DeselectedAlpha => GameOverUIManager.Instance.DeSelectedButtonAlpha;
    float AlphaFadeDuration => GameOverUIManager.Instance.buttonAlphaFadeDuration;

    TMP_Text text;
    Tween alphaFadeTween;

    void Awake()
    {
        text = GetComponentInChildren<TMP_Text>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        alphaFadeTween?.Kill();
        alphaFadeTween = text.DOFade(1f, AlphaFadeDuration);
    }

    public void OnDeselect(BaseEventData eventData)  
    {
        alphaFadeTween?.Kill();
        alphaFadeTween = text.DOFade(DeselectedAlpha, AlphaFadeDuration);
    }

    void OnDestroy()
    {
        alphaFadeTween?.Kill();
    }
}