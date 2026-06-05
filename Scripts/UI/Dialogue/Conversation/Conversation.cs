using UnityEngine;

[CreateAssetMenu(fileName = "Conversation", menuName = "Convo")]
public class Conversation : ScriptableObject
{
    public enum ConversationEndType { Defeult, Choice, Custom }

    public ConversationEndType ConversationEnd = ConversationEndType.Defeult;
    public Sentence[] sentences;

    [ConditionalField(nameof(ConversationEnd), (int)ConversationEndType.Choice)] public Choice[] choiceResults;

    [ConditionalField(nameof(ConversationEnd), (int)ConversationEndType.Custom)] public ConversationEndAction customEndAction;
}


[System.Serializable]
public class Sentence
{
    public enum SentenceEntrance { AllAtOnce, OneLetterAtATime }
    public enum SentenceEndMethod { Input, Timer }
    public enum SentenceCamera { Closest, LockOnBoth, Custom, None }

    [TextArea]
    public string sentence;
    public SentenceEntrance sentenceEntrance;

    [ConditionalField(nameof(sentenceEntrance), (int)SentenceEntrance.AllAtOnce)] public float SentenceFadeInDuration;       // Only show when sentenceEntrance == AllAtOnce
    [ConditionalField(nameof(sentenceEntrance), (int)SentenceEntrance.OneLetterAtATime)] public float letterDelay;       // Only show when sentenceEntrance == AllAtOnce
    [Space]

    public SentenceEndMethod sentenceEndMethod;
    [ConditionalField(nameof(sentenceEndMethod), (int)SentenceEndMethod.Timer)] public float SentenceDuration;    // Only show when sentenceEndMethod == Timer
    [Space]

    public float SentenceFadeOutDuration;

    public float durationBeforeNextSentence;

    [Space]
    public SentenceCamera sentenceCamera = SentenceCamera.LockOnBoth;

    [ConditionalField(nameof(sentenceCamera), (int)SentenceCamera.Closest, (int)SentenceCamera.Custom, (int)SentenceCamera.LockOnBoth),]
    public bool KeepLastCamPos = false;

    [ConditionalField(nameof(sentenceCamera), (int)SentenceCamera.Closest, (int)SentenceCamera.Custom, (int)SentenceCamera.LockOnBoth)]
    [ConditionalField("KeepLastCamPos", false, ConditionalFieldAttribute.DrawMode.Disable)]
    public float CamTransitionDuration = .3f;

    [ConditionalField(nameof(sentenceCamera), (int)SentenceCamera.Custom)]
    [ConditionalField(nameof(KeepLastCamPos), false, ConditionalFieldAttribute.DrawMode.Disable)]
    public Vector3 CameraPos, CameraRot;

    [ConditionalField(nameof(sentenceCamera), (int)SentenceCamera.LockOnBoth)]
    [ConditionalField("KeepLastCamPos", false, ConditionalFieldAttribute.DrawMode.Disable)]
    public Vector2 startingYawPitch;

    public bool PlayNewAnimation;
    [ConditionalField("PlayNewAnimation", true, ConditionalFieldAttribute.DrawMode.HideCompletely)] public AnimationClip newAnimation;

    public bool PlayNewPlayerAnimation;
    [ConditionalField("PlayNewPlayerAnimation", true, ConditionalFieldAttribute.DrawMode.HideCompletely)] public AnimationClip newPlayerAnimation;

}

[System.Serializable]
public class Choice
{
    public enum ChoiceResult { Conversation, End, Custom }

    public ChoiceResult choiceResult;
    public string choiceNameText = "";

    [ConditionalField(nameof(choiceResult), (int)ChoiceResult.Conversation)] public Conversation NextConversation;
    [ConditionalField(nameof(choiceResult), (int)ChoiceResult.Conversation)] public Transform newPerson;
    [ConditionalField(nameof(choiceResult), (int)ChoiceResult.Conversation)] public bool ConvoStartsWithFreshTextBox = false;
}