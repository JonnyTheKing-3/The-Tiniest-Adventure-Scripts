using UnityEngine;

public abstract class ConversationEndAction : ScriptableObject
{
    public abstract void Execute(Talker context);
}

