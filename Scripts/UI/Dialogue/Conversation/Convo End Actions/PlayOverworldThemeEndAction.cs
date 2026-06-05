using UnityEngine;

[CreateAssetMenu(fileName = "PlayOverworldThemeEndAction", menuName = "Conversation/End Actions/Play Overworld Theme")]
public class PlayOverworldThemeEndAction : ConversationEndAction
{
    public override void Execute(Talker content) => AudioManager.Instance.FadeToOverworldTheme();
}
