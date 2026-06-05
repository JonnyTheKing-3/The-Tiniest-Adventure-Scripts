using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;


public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public TMP_Text textbox;
    public Conversation conversation;
    public RectTransform Fade1, Fade2, Image;
    public Image ContinueIndicator;
    public CanvasGroup ChoiceCanvasGroup;
    public GameObject buttonPrefab;
    public GridLayoutGroup gridLayoutGroup;
    private UIAutoSelectButtons dialogueChoiceUI;
    public GameObject ChoiceIndicators;

    [Header("UI Animation Settings")]
    public float canvasEntrnaceDuration;
    public float canvasEntrnaceStartingScale;

    [Space]
    public float ContinueIndicatorEnterScale;
    public float ContinueIndicatorPressScale = .5f;
    public float ContinueIndicatorExitScale = .8f;

    [Space]
    public float ContinueIndicatorEnterDuration = .2f;
    public float ContinueIndicatorPressDuration = .1f;
    public float ContinueIndicatorExitDuration;

    [Header("Status")]
    public Sentence currentSentence = null;
    public bool sentenceInProgress;
    [Space]
    public bool playConvo;

    [Header("Audio")]
    [SerializeField, Min(1)] private int lettersPerDialogueBleep = 3;

    private Vector3 Fade1StartingSacle, Fade2StartingSacle, ImageStartingSacle, ContinueIndicatorStartingSacle;
    private CanvasGroup canvasGroup;
    InputAction submit;
    private bool skip = false;
    private Transform PersonB;
    private Animator talkerAnimator;
    private AnimationClip talkerAnimationClipBeforeConvo;
    private Vector3 ChoiceCanvasStartingSacle;
    private float ChoiceCanvasEntrnaceScale;
    private Talker currentTalker; // Need this to keep track of end action context for cutom conversation end types


    void Start()
    {
        Instance = this;
        textbox.alpha = 0f;
        canvasGroup = GetComponent<CanvasGroup>();
        gridLayoutGroup = transform.root.GetComponentInChildren<GridLayoutGroup>();
        dialogueChoiceUI = gridLayoutGroup.GetComponent<UIAutoSelectButtons>();
        submit = Player.Instance._playerUIInputManager.SubmitAction;
        submit.started += OnSubmitStarted;
        skip = false;
        ChoiceIndicators.SetActive(false);

        Color tempCol = Color.white;
        tempCol.a = 0f;
        ContinueIndicator.color = tempCol;


        canvasGroup.alpha = 0f;
        ChoiceCanvasGroup.alpha = 0f;
        Fade1StartingSacle = Fade1.localScale;
        Fade2StartingSacle = Fade2.localScale;
        ImageStartingSacle = Image.localScale;
        ContinueIndicatorStartingSacle = ContinueIndicator.GetComponent<RectTransform>().localScale;

        ChoiceCanvasStartingSacle = ChoiceCanvasGroup.GetComponent<RectTransform>().localScale;
        ChoiceCanvasEntrnaceScale = canvasEntrnaceStartingScale;
        ChoiceCanvasGroup.GetComponent<RectTransform>().localScale *= ChoiceCanvasEntrnaceScale;

        Fade1.localScale *= canvasEntrnaceStartingScale;
        Fade2.localScale *= canvasEntrnaceStartingScale;
        Image.localScale *= canvasEntrnaceStartingScale;
        ContinueIndicator.GetComponent<RectTransform>().localScale *= ContinueIndicatorEnterScale;

    }
    void OnSubmitStarted(InputAction.CallbackContext context)
    {
        if (sentenceInProgress) skip = true;
    }

    public void SetupConvo(Conversation convo, Transform otherPerson, Animator animator, bool StoreLastClip = true, Talker talker = null)
    {
        conversation = convo;
        currentTalker = talker;
        talkerAnimator = animator;

        if (currentTalker) currentTalker.FadeApproachUI(0f);

        AnimationEvents.Instance.DisablePlayerActionMap();
        Player.Instance._playerUIInputManager.SwitchToUI();
        GameManager.Instance._currPlayMode = GameManager.PlayMode.Dialogue;
        Player.Instance.playerState = Player.PlayerStates.dialogue;

        // rotate player
        PersonB = otherPerson;
        Player.Instance._playerAnimation.SetLockOnForward(PersonB);
        StartCoroutine(Player.Instance._playerAnimation.RotateModelFully(.2f));

        // rotate NPC
        if (animator.TryGetComponent(out VillagerAnimation villagerAnim))
        {
            villagerAnim.SetLockOnForward(Player.Instance.transform);
            Coroutine c = villagerAnim.RotateModelFully(()=>{}, duration:0.2f);
        }

        if (animator != null && StoreLastClip)
        {
            talkerAnimationClipBeforeConvo = animator.GetCurrentAnimatorClipInfo(0)[0].clip;
        }

        StartCoroutine(PlayConversation(conversation));
    }

    public IEnumerator PlayConversation(Conversation convo)
    {
        // Fade in and scale up canvas
        if (!DialogueCanvasOnScreen) yield return DialogueCanvasVisibility(true);

        foreach (var sentence in convo.sentences)
        {
            if (sentence.PlayNewAnimation && talkerAnimator != null && sentence.newAnimation != null)
                talkerAnimator.CrossFade(sentence.newAnimation.name, .1f);

            if (sentence.PlayNewPlayerAnimation && sentence.newPlayerAnimation != null)
                Player.Instance._playerAnimation.animator.CrossFade(sentence.newPlayerAnimation.name, .1f, 0); // all new player animations MUST be in the base layer to avoid confusion

            if (!sentence.KeepLastCamPos)
            {
                switch (sentence.sentenceCamera)
                {
                    case Sentence.SentenceCamera.Closest:
                        if (CamerasManager.Instance.CameraState != CamerasManager.CameraStates.LockOn)
                        {
                            var FR_closestCam = CamerasManager.Instance.VC_FreeRoam;
                            CamerasManager.Instance.GetFreeRoamDialogueClosestPose(PersonB, out Vector3 closestPos, out Vector3 closestRot);

                            if (CamerasManager.Instance.CameraState != CamerasManager.CameraStates.FreeRoam)
                            {
                                CamerasManager.Instance.SwitchToFreeRoam(
                                    CamerasManager.Instance._currentCam.State.FinalPosition,
                                    CamerasManager.Instance._currentCam.State.FinalOrientation.eulerAngles
                                );
                            }

                            FR_closestCam.transform.DOMove(closestPos, sentence.CamTransitionDuration).SetEase(Ease.InOutQuad);
                            FR_closestCam.transform.DORotate(closestRot, sentence.CamTransitionDuration).SetEase(Ease.InOutQuad);
                        }
                        break;


                    case Sentence.SentenceCamera.LockOnBoth:
                        var LO_cam = CamerasManager.Instance.c_LockOn;

                        if (CamerasManager.Instance.CameraState != CamerasManager.CameraStates.LockOn)
                        {
                            CamerasManager.Instance.SwitchToLockOnDialogueFromCurrentCamera(PersonB);
                        }

                        DOTween.To(() => LO_cam.currentYaw, x => LO_cam.currentYaw = x, sentence.startingYawPitch.x, sentence.CamTransitionDuration).SetEase(Ease.InOutQuad);
                        DOTween.To(() => LO_cam.currentPitch, y => LO_cam.currentPitch = y, sentence.startingYawPitch.y, sentence.CamTransitionDuration).SetEase(Ease.InOutQuad);

                        break;


                    case Sentence.SentenceCamera.Custom:
                        var FR_cam = CamerasManager.Instance.VC_FreeRoam;

                        if (CamerasManager.Instance.CameraState != CamerasManager.CameraStates.FreeRoam)
                        {
                            CamerasManager.Instance.SwitchToFreeRoam(
                                CamerasManager.Instance._currentCam.State.FinalPosition,
                                CamerasManager.Instance._currentCam.State.FinalOrientation.eulerAngles
                            );
                        }

                        FR_cam.transform.DOMove(sentence.CameraPos, sentence.CamTransitionDuration).SetEase(Ease.InOutQuad);
                        FR_cam.transform.DORotate(sentence.CameraRot, sentence.CamTransitionDuration).SetEase(Ease.InOutQuad);
                        break;


                    default: break;
                }
            }

            currentSentence = sentence;
            sentenceInProgress = true;

            if (sentence.sentenceEntrance == Sentence.SentenceEntrance.AllAtOnce)
            {
                textbox.alignment = TextAlignmentOptions.Center;
                textbox.enableAutoSizing = true;
                textbox.text = sentence.sentence;
                AudioManager.Instance.PlayUIDialogueBleep(transform);
                yield return textbox.DOFade(1f, sentence.SentenceFadeInDuration).SetEase(Ease.Linear).SetUpdate(false).WaitForCompletion();
            }
            else
            {
                textbox.alignment = TextAlignmentOptions.Left;

                // Calculate font size. Might change later to manual font size setting per sentence depending on how this looks throughout the development
                textbox.enableAutoSizing = true;
                textbox.text = sentence.sentence;
                float fontSize = textbox.fontSize;

                textbox.enableAutoSizing = false;
                textbox.text = "";
                textbox.fontSize = fontSize;

                // Start typewriter effect
                textbox.alpha = 1f;
                int lettersSinceLastBleep = 0;
                foreach (char c in sentence.sentence)
                {
                    if (skip)
                    {
                        textbox.text = sentence.sentence;
                        break;
                    }

                    textbox.text += c;
                    lettersSinceLastBleep++;
                    if (lettersSinceLastBleep >= lettersPerDialogueBleep)
                    {
                        AudioManager.Instance.PlayUIDialogueBleep(transform);
                        lettersSinceLastBleep = 0;
                    }

                    yield return new WaitForSeconds(sentence.letterDelay);
                }
            }

            sentenceInProgress = false;
            if (sentence.sentenceEndMethod == Sentence.SentenceEndMethod.Timer)
                yield return new WaitForSeconds(sentence.SentenceDuration);
            else
            {
                RectTransform indicatorTransform = ContinueIndicator.GetComponent<RectTransform>();

                // Fade in and scale up icon
                var CIFadeIn = ContinueIndicator.DOFade(1f, ContinueIndicatorEnterDuration).SetEase(Ease.InOutQuad).SetUpdate(false);
                var CIScaleUp = indicatorTransform.DOScale(ContinueIndicatorStartingSacle, ContinueIndicatorPressDuration).SetEase(Ease.InOutQuad).SetUpdate(false);
                yield return DOTween.Sequence().Join(CIFadeIn).Join(CIScaleUp).WaitForCompletion();

                yield return new WaitUntil(() => submit.triggered);
                AudioManager.Instance.PlayUIButtonClick(transform);

                // scale down and up icon to give the illusion it was pressed
                yield return indicatorTransform.DOScale(ContinueIndicatorStartingSacle * ContinueIndicatorPressScale, ContinueIndicatorPressDuration).SetEase(Ease.InOutQuad).SetUpdate(false).WaitForCompletion();
                yield return indicatorTransform.DOScale(ContinueIndicatorStartingSacle, ContinueIndicatorPressDuration).SetEase(Ease.InOutQuad).SetUpdate(false).WaitForCompletion();

                var CIFadeOut = ContinueIndicator.DOFade(0f, ContinueIndicatorExitDuration).SetEase(Ease.InOutQuad).SetUpdate(false);
                var CISecondScaleDown = indicatorTransform.DOScale(ContinueIndicatorStartingSacle * ContinueIndicatorExitScale, ContinueIndicatorExitDuration).SetEase(Ease.InOutQuad).SetUpdate(false);
                yield return DOTween.Sequence().Join(CIFadeOut).Join(CISecondScaleDown).WaitForCompletion();

            }

            // if this is the last sentence of the conversation and the end type is choice, keep the text
            if (conversation.sentences[conversation.sentences.Length - 1] != currentSentence || conversation.ConversationEnd != Conversation.ConversationEndType.Choice)
            {
                yield return textbox.DOFade(0f, sentence.SentenceFadeOutDuration).SetEase(Ease.InOutQuad).SetUpdate(false).WaitForCompletion();
                textbox.text = "";
            }

            skip = false;
            sentenceInProgress = false;
            yield return new WaitForSeconds(sentence.durationBeforeNextSentence);
        }

        switch (conversation.ConversationEnd)
        {
            case Conversation.ConversationEndType.Defeult:

                // Fade out and scale down canvas
                yield return DialogueCanvasVisibility(false);
                yield return ExitConversation();
                break;

            case Conversation.ConversationEndType.Choice:
                yield return CreateButtons(conversation.choiceResults.Length);
                yield return ChoiceCanvasVisibility(true, true);
                ChoiceIndicators.SetActive(true);
                // The button will call the PlayerPicksAChoice function
                yield break;

            case Conversation.ConversationEndType.Custom:

                conversation.customEndAction.Execute(currentTalker);
                
                yield return DialogueCanvasVisibility(false);
                yield return ExitConversation();
                break;
        }
    }

    private bool DialogueCanvasOnScreen = false;
    IEnumerator DialogueCanvasVisibility(bool enter)
    {
        float fade = enter ? 1f : 0f;
        Vector3 scale1 = enter ? Fade1StartingSacle : (Fade1StartingSacle * canvasEntrnaceStartingScale);
        Vector3 scale2 = enter ? Fade2StartingSacle : (Fade2StartingSacle * canvasEntrnaceStartingScale);
        Vector3 image = enter ? ImageStartingSacle : (ImageStartingSacle * canvasEntrnaceStartingScale);

        var Fade = canvasGroup.DOFade(fade, canvasEntrnaceDuration).SetEase(Ease.InOutQuad).SetUpdate(false);
        var Fade1Scale = Fade1.DOScale(scale1, canvasEntrnaceDuration).SetEase(Ease.InOutQuad).SetUpdate(false);
        var Fade2Scale = Fade2.DOScale(scale2, canvasEntrnaceDuration).SetEase(Ease.InOutQuad).SetUpdate(false);
        var ImageScale = Image.DOScale(image, canvasEntrnaceDuration).SetEase(Ease.InOutQuad).SetUpdate(false);
        DialogueCanvasOnScreen = true;
        yield return DOTween.Sequence()
            .Join(Fade)
            .Join(Fade1Scale)
            .Join(Fade2Scale)
            .Join(ImageScale)
            .WaitForCompletion();
    }

    IEnumerator ExitConversation()
    {
        if (talkerAnimator != null && talkerAnimationClipBeforeConvo != null) 
        {
            talkerAnimator.CrossFade(talkerAnimationClipBeforeConvo.name, .1f);
        }
        
        if (currentTalker != null) currentTalker.HasTalked = true;

        Player.Instance._playerUIInputManager.SwitchToPlayer();
        GameManager.Instance._currPlayMode = GameManager.PlayMode.FreeRoam;
        Player.Instance.playerState = Player.PlayerStates.normal;
        CamerasManager.Instance.SwitchToThirdPerson();
        Player.Instance._playerAnimation.animator.CrossFade("Moving", .1f, 0);
        DialogueCanvasOnScreen = false;

        if (currentTalker) currentTalker.FadeApproachUI(0f);

        yield break;
    }

    IEnumerator ChoiceCanvasVisibility(bool enter, bool fadeAndScaleSynced)
    {
        float fade = enter ? 1f : 0f;
        Vector3 scale = enter ? ChoiceCanvasStartingSacle : (ChoiceCanvasStartingSacle * ChoiceCanvasEntrnaceScale);
        var rt = ChoiceCanvasGroup.GetComponent<RectTransform>();

        var s = DOTween.Sequence().SetUpdate(false).SetEase(Ease.InOutQuad);

        if (fadeAndScaleSynced)
        {
            s.Join(ChoiceCanvasGroup.DOFade(fade, canvasEntrnaceDuration))
             .Join(rt.DOScale(scale, canvasEntrnaceDuration));
        }
        else
        {
            s.Append(ChoiceCanvasGroup.DOFade(fade, canvasEntrnaceDuration))
             .Append(rt.DOScale(scale, canvasEntrnaceDuration));
        }

        yield return s.WaitForCompletion();
    }


    IEnumerator CreateButtons(int n)
    {
        List<Button> newButtons = new List<Button>();

        for (int i = 0; i < n; i++)
        {
            GameObject button = Instantiate(buttonPrefab, gridLayoutGroup.transform);
            DialogueButtonChoice dbc = button.GetComponent<DialogueButtonChoice>();

            dbc.buttonIndex = i;
            dbc.choice = conversation.choiceResults[i];
            dbc.TMPtext.text = conversation.choiceResults[i].choiceNameText;

            newButtons.Add(button.GetComponent<Button>());
        }

        dialogueChoiceUI.enabled = true;
        dialogueChoiceUI.RebindButtons(newButtons);
        yield break;
    }


    public IEnumerator PlayerPicksAChoice(Choice choice)
    {
        submit.Disable();
        AudioManager.Instance.PlayUIButtonClick(transform);
        dialogueChoiceUI.enabled = false;
        ChoiceIndicators.SetActive(false);
        switch (choice.choiceResult)
        {
            case Choice.ChoiceResult.Conversation:
                if (choice.newPerson) PersonB = choice.newPerson;
                yield return ChoiceCanvasVisibility(false, false);

                if (choice.ConvoStartsWithFreshTextBox)
                {
                    yield return textbox.DOFade(0f, conversation.sentences[conversation.sentences.Length-1].SentenceFadeOutDuration).SetEase(Ease.InOutQuad).SetUpdate(false).WaitForCompletion();
                    textbox.text = "";
                }
                yield return new WaitForSeconds(ContinueIndicatorExitDuration);
                SetupConvo(choice.NextConversation, PersonB, talkerAnimator, false);
                break;

            case Choice.ChoiceResult.End:
                yield return ChoiceCanvasVisibility(false, false);
                yield return textbox.DOFade(0f, conversation.sentences[conversation.sentences.Length-1].SentenceFadeOutDuration).SetEase(Ease.InOutQuad).SetUpdate(false).WaitForCompletion();
                textbox.text = "";
                yield return DialogueCanvasVisibility(false);
                yield return ExitConversation();
                break;

            case Choice.ChoiceResult.Custom:
                // I'll do this later
                break;
        }
        yield return DestroyButtons();
        submit.Enable();
    }


    IEnumerator DestroyButtons()
    {
        foreach (Transform child in gridLayoutGroup.transform)
            Destroy(child.gameObject);

        yield break;
    }

}
