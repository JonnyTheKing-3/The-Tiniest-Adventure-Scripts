using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "GiveBowEndAction", menuName = "Conversation/End Actions/Give Bow")]
public class GiveBowEndAction : ConversationEndAction
{
    public override void Execute(Talker content)
    {
        Player.Instance.OwnsBow = true;
        content.SetApproachIcon(TalkerApproachIconState.HasAlreadyGivenSomething);
    }
}
