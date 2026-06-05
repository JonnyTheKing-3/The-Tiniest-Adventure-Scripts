using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadUIManager : MonoBehaviour
{
    public static LoadUIManager Instance;
    CanvasGroup canvasGroup;

    [SerializeField] private float fadeDuration = 1;


    void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public  IEnumerator FadeBlackLoadImage(bool fadeIn)
    {
        float targetAlpha = fadeIn ? 1f : 0f;
        if (fadeDuration == 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime <= fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        yield break;        
    }

}
