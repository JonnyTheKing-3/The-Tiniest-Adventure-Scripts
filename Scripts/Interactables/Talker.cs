using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public enum TalkerApproachIconState
{
    Regular,
    HasSomethingToGive,
    HasAlreadyGivenSomething
}

public class Talker : MonoBehaviour, IApproachable
{
    [Tooltip("Which convo is used is based on talkerID and world conditions. We use the first by default")] public Conversation[] conversationOptions; 
    public Animator animator;
    public string talkerID;
    public bool isDummy = false;
    public bool HasTalked;

    [Space]
    public CanvasGroup UIApproachCanvasGroup;
    [HideInInspector] public Image ApproachIcon;
    public TalkerApproachIconState approachIconState;
    Tween approachUIFadeTween;
    [HideInInspector] public float currentUIAlpha;


    void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        if (UIApproachCanvasGroup)
        {
            UIApproachCanvasGroup.alpha = 0f;
            currentUIAlpha = UIApproachCanvasGroup.alpha;
            ApproachIcon = UIApproachCanvasGroup.GetComponentInChildren<Image>();
        }
    }

    void Start()
    {
        SyncApproachIconStateFromSprite();
        SetApproachIcon(approachIconState);
    }

    public virtual void OnHasTalked() {}

    public void SetApproachIcon(TalkerApproachIconState state)
    {
        approachIconState = state;

        if (ApproachIcon == null) return;

        ApproachIcon.sprite = state switch
        {
            TalkerApproachIconState.HasSomethingToGive => GameManager.Instance.HasSomethingToGiveIcon,
            TalkerApproachIconState.HasAlreadyGivenSomething => GameManager.Instance.HasAlreadyGivenSomthingIcon,
            _ => GameManager.Instance.RegularConvoIcon
        };
    }

    void SyncApproachIconStateFromSprite()
    {
        if (ApproachIcon == null || ApproachIcon.sprite == null) return;

        if (ApproachIcon.sprite == GameManager.Instance.HasSomethingToGiveIcon)
            approachIconState = TalkerApproachIconState.HasSomethingToGive;
        else if (ApproachIcon.sprite == GameManager.Instance.HasAlreadyGivenSomthingIcon)
            approachIconState = TalkerApproachIconState.HasAlreadyGivenSomething;
        else if (ApproachIcon.sprite == GameManager.Instance.RegularConvoIcon)
            approachIconState = TalkerApproachIconState.Regular;
    }

    void OnDisable() => approachUIFadeTween?.Kill();
    public void FadeApproachUI(float targetAlpha)   // Get called by PlayerInputScript
    {
        if (UIApproachCanvasGroup == null) return;
        if (currentUIAlpha == targetAlpha) return; // Need this because tween will keep reseting otherwise

        currentUIAlpha = targetAlpha;

        approachUIFadeTween?.Kill();
        approachUIFadeTween = UIApproachCanvasGroup
            .DOFade(targetAlpha, GameManager.Instance.UIApproachbleFadeDuration)
            .SetEase(Ease.InOutQuad);
    }
}
