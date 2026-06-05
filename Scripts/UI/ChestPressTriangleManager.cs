using DG.Tweening;
using UnityEngine;

public class ChestPressTriangleManager : MonoBehaviour
{
    public static ChestPressTriangleManager Instance;

    public CanvasGroup canvasGroup;
    private Chest currentChest;
    private Tween fadeTween;
    private float currentTargetAlpha;

    void Awake()
    {
        Instance = this;

        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        currentTargetAlpha = canvasGroup.alpha;
    }

    void OnDisable() => fadeTween?.Kill();

    public void FadeForChest(Chest chest, float targetAlpha)
    {
        if (targetAlpha > 0f)
        {
            currentChest = chest;
        }
        else
        {
            if (currentChest != null && currentChest != chest) return;

            currentChest = null;
        }

        FadeTo(targetAlpha);
    }

    public void ForceFadeOut(Chest chest)
    {
        if (currentChest != null && currentChest != chest) return;

        currentChest = null;
        FadeTo(0f);
    }

    private void FadeTo(float targetAlpha)
    {
        if (Mathf.Approximately(currentTargetAlpha, targetAlpha)) return;

        currentTargetAlpha = targetAlpha;
        fadeTween?.Kill();
        fadeTween = canvasGroup
            .DOFade(targetAlpha, GameManager.Instance.UIApproachbleFadeDuration)
            .SetEase(Ease.InOutQuad);
    }
}
