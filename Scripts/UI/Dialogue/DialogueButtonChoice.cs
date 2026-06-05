using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class DialogueButtonChoice : MonoBehaviour, ISelectHandler
{
    // Goes on button

    public int buttonIndex;
    public TMP_Text TMPtext;
    public Choice choice;
    private Button btn;
    private RectTransform rt;
    RectTransform CI_rectTransform;

    void Awake()
    {
        btn = GetComponent<Button>();
        TMPtext = GetComponentInChildren<TMP_Text>();
        rt = GetComponent<RectTransform>();
        btn.onClick.AddListener(OnButtonClicked);
        CI_rectTransform = DialogueManager.Instance.ChoiceIndicators.GetComponent<RectTransform>();
    }

    void OnButtonClicked() => StartCoroutine(DialogueManager.Instance.PlayerPicksAChoice(choice));

    // Move choice indicator towards button
    public void OnSelect(BaseEventData eventData)
    {
        // 1. Get root canvas and camera
        var canvas = CI_rectTransform.GetComponentInParent<Canvas>();
        var rootCanvas = canvas.rootCanvas;
        var rootRect = rootCanvas.transform as RectTransform;
        Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : rootCanvas.worldCamera;
    
        // 2. Get the button's center in world space
        Vector3 btnWorld = rt.TransformPoint(rt.rect.center);
    
        // 3. Convert world -> screen
        Vector2 btnScreen = RectTransformUtility.WorldToScreenPoint(cam, btnWorld);
    
        // 4. Convert screen -> local in the ROOT canvas rect
        Vector2 localOnRoot;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootRect,
            btnScreen,
            cam,
            out localOnRoot
        );
    
        // 5. Convert root-local -> world
        Vector3 worldFromRoot = rootRect.TransformPoint(localOnRoot);
    
        // 6. Convert world -> local in CI's parent space
        RectTransform indicatorParent = CI_rectTransform.parent as RectTransform;
        Vector2 localOnIndicatorParent = indicatorParent.InverseTransformPoint(worldFromRoot);
    
        // 7. Move CI's Y only
        Vector2 target = CI_rectTransform.anchoredPosition;
        target.y = localOnIndicatorParent.y;
    
        CI_rectTransform.DOAnchorPos(target, 0.15f).SetEase(Ease.OutQuad);
    }


}
