using UnityEngine;

[CreateAssetMenu(fileName = "GiveBowObtainmentPermissionEndAction", menuName = "Conversation/End Actions/Give Permission For Bow Obtainment")]

public class GiveBowPermissionEndAction : GiveOnFirstTimeTalkingBaseEndAction
{
    public override void ExecuteFirstTimeTalkAction(Talker talker) 
    {
        Player.Instance.CanObtainBow = true;
        talker.SetApproachIcon(TalkerApproachIconState.HasAlreadyGivenSomething);
    }
}
