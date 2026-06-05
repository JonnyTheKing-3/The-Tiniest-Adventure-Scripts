using UnityEngine;

[CreateAssetMenu(fileName = "GiveItemEndAction", menuName = "Conversation/End Actions/Give Item")]
public class GiveItemEndAction : ConversationEndAction
{
    public InventoryItemData testItem;

    public override void Execute(Talker context = null)
    {
        Player.Instance._playerInventory.AddItem(testItem);
        context.SetApproachIcon(TalkerApproachIconState.HasAlreadyGivenSomething);
    }
}
