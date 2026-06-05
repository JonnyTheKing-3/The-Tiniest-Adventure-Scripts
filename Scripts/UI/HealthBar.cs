using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class HealthBar : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public MonoBehaviour healthSource;      // Is MonoBehavior because IHasHealth is not assignable because it's an interface field 
    public Slider healthSlider;
    public Slider easeHealthSlider;
    [Space]
    public float EaseSliderWaitTime = .5f;
    public float EaseSliderCatchUpDuration = .5f;
    public float canvasAlphaFadeInDuration = .5f;
    public float canvasAlphaFadeOutDelay = 5f;
    public float canvasAlphaFadeOutDuration = 0.5f;

    [Tooltip("If this is on, the above variables won't have effect")] [SerializeField] private bool alwaysShowHealth = false;

    Health health;
    Tween easeSliderTween;
    Tween canvasFadeTween;
    Coroutine easeSliderCoroutine;
    Coroutine fadeCoroutine;

    void Awake()
    {
        if (healthSource is not IHasHealth hasHealth)
        {
            Debug.LogError($"{name}: Assigned healthSource does not implement IHasHealth.", this);
            return;
        }

        health = hasHealth.Health;

        if (!gameObject.CompareTag("PlayerHealthCanvas")) 
            canvasGroup.alpha = alwaysShowHealth ? 1f: 0f; // Load and Intro Convo will take care of fading in the player health canvas

        healthSlider.value = 1f;
        easeHealthSlider.value = 1f;
    }

    void OnEnable() => health.OnHealthChange += UpdateHealthBar;
    void OnDisable() 
    {
        health.OnHealthChange -= UpdateHealthBar;

        // Stop all routines and tweens when disabled
        easeSliderTween?.Kill();
        canvasFadeTween?.Kill();
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (easeSliderCoroutine != null) StopCoroutine(easeSliderCoroutine);
    }


    public void UpdateHealthBar(float healthNormalized)
    {
        healthSlider.value = healthNormalized;

        if (easeSliderCoroutine != null) StopCoroutine(easeSliderCoroutine);
        easeSliderCoroutine = StartCoroutine(EaseSliderDelay(healthNormalized)); // Coroutine calls the fade function
    }

    IEnumerator EaseSliderDelay(float targetValue)
    {
        // Stop all routines
        easeSliderTween?.Kill();
        canvasFadeTween?.Kill();
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        canvasFadeTween = canvasGroup.DOFade(1, canvasAlphaFadeInDuration).SetEase(Ease.OutCubic);

        yield return new WaitForSeconds(EaseSliderWaitTime);
        
        easeSliderTween = easeHealthSlider.DOValue(targetValue, EaseSliderCatchUpDuration).SetEase(Ease.OutQuad);
        yield return easeSliderTween.WaitForCompletion();

        if (!alwaysShowHealth)
        {
            fadeCoroutine = StartCoroutine(FadedOutAfterDelay(canvasAlphaFadeOutDelay));
        }

    }

    IEnumerator FadedOutAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        canvasFadeTween = canvasGroup.DOFade(0, canvasAlphaFadeOutDuration).SetEase(Ease.InCubic);
    }

}
