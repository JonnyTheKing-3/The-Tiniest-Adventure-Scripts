using UnityEngine;

public abstract class GiveOnFirstTimeTalkingBaseEndAction : ConversationEndAction
{
    public override void Execute(Talker context)
    {
        if (!context.HasTalked) 
        {
            ExecuteFirstTimeTalkAction(context);
        }
    }

    public abstract void ExecuteFirstTimeTalkAction(Talker context);
}