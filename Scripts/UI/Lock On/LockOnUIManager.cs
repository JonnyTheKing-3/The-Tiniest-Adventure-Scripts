using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LockOnUIManager : MonoBehaviour
{
    public static LockOnUIManager Instance { get; private set; }

    [Header("References")]
    private Camera mainCamera;
    private Canvas canvas;

    [Header("Lock On Bob")]
    [SerializeField] private float bobHeight = 16f;
    [SerializeField] private float bobSpeed = 32f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.15f;

    private CanvasGroup canvasGroup;
    private RectTransform lockOnRect;
    private RectTransform canvasRect;

    private Transform currentLockOnTarget;
    private Coroutine fadeCoroutine;
    private float bobTimer;

    private void Awake()
    {
        Instance = this;

        canvasGroup = GetComponent<CanvasGroup>();
        lockOnRect = transform.GetChild(0).GetComponent<RectTransform>(); // I don't want to set it in editor and GetComponentInChildren actually also searches parent

        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        canvasGroup.alpha = 0f;
    }

    public void SetUILockOnTarget(Transform target)
    {
        currentLockOnTarget = target;
        bobTimer = 0f;

        if (currentLockOnTarget != null)
        {
            SetLockOnPosition(0f);
            FadeTo(1f);
        }
        else
        {
            FadeTo(0f);
        }
    }

    private void LateUpdate()
    {
        if (currentLockOnTarget == null) return;

        bobTimer += Time.deltaTime;
        float bobProgress = Mathf.PingPong(bobTimer * bobSpeed / Mathf.Max(bobHeight, 0.01f), 1f);
        float verticalOffset = Mathf.SmoothStep(0f, bobHeight, bobProgress);

        SetLockOnPosition(verticalOffset);
    }

    private void SetLockOnPosition(float verticalOffset)
    {
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(currentLockOnTarget.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasRect, screenPoint, canvas.worldCamera, out Vector2 localPoint);

        lockOnRect.anchoredPosition = localPoint + Vector2.up * verticalOffset;
    }

    private void FadeTo(float targetAlpha)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(targetAlpha));
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            fadeCoroutine = null;
            yield break;
        }

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
    }
}
