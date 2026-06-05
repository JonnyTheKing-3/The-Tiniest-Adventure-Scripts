using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class ItemAddedUIManager : MonoBehaviour
{
    public static ItemAddedUIManager Instance { get; private set; }

    [SerializeField] private Image image;
    [SerializeField] private TMP_Text text;
    [SerializeField] private RectTransform panel;
    private Image panelImage;   // if I get a image that can change color filling, I'll use this for a color change effect

    [Space]
    [SerializeField] private float slideInXPosition;
    [SerializeField] private float slideOutXPosition;
    [SerializeField] private float onScreenDuration = 1.5f;
    [SerializeField] private float slideDuration = 0.4f;

    private Sequence currentSequence;
    private Coroutine ShowItemCoroutine;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        panelImage = panel.GetComponent<Image>();

        panel.anchoredPosition = new Vector2(slideOutXPosition, panel.anchoredPosition.y);
        canvasGroup.alpha = 0f;
    }

    public void ShowItem(string message, Sprite itemSprite = null)
    {
        if (ShowItemCoroutine != null) 
        {
            StopCoroutine(ShowItemCoroutine);
            ShowItemCoroutine = null;
        }

        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
            currentSequence = null;
        }

        ShowItemCoroutine = StartCoroutine(ShowItemRoutine(message, itemSprite));
    }

    IEnumerator ShowItemRoutine(string message, Sprite itemSprite = null)
    {
        text.text = message;
        image.sprite = itemSprite ?? image.sprite;


        // Reset
        panel.anchoredPosition = new Vector2(slideOutXPosition, panel.anchoredPosition.y);
        canvasGroup.alpha = 0f;
        currentSequence = DOTween.Sequence().SetUpdate(true);

        // Slide/Fade in
        currentSequence.Join(panel.DOAnchorPosX(slideInXPosition, slideDuration).SetEase(Ease.OutCubic));
        currentSequence.Join(canvasGroup.DOFade(1f, slideDuration).SetEase(Ease.OutCubic));

        // Wait a bit
        yield return currentSequence.WaitForCompletion();
        yield return new WaitForSecondsRealtime(onScreenDuration);

        // Slide/Fade out
        currentSequence = DOTween.Sequence().SetUpdate(true);
        currentSequence.Join(panel.DOAnchorPosX(slideOutXPosition, slideDuration).SetEase(Ease.InCubic));
        currentSequence.Join(canvasGroup.DOFade(0f, slideDuration).SetEase(Ease.InCubic));


        yield return currentSequence.WaitForCompletion();
        currentSequence = null;
        ShowItemCoroutine = null;
    }
}
