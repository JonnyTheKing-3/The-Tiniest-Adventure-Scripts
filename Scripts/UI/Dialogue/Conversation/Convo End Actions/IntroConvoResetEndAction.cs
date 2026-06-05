using DG.Tweening;
using UnityEngine;


[CreateAssetMenu(fileName = "IntroConvoResetEndAction", menuName = "Conversation/End Actions/Intro Convo End Action")]
public class IntroConvoResetEndAction : ConversationEndAction   // THIS IS A ONE OF
{
    public override void Execute(Talker content)
    {
        AudioManager.Instance.FadeToOverworldTheme();
        Player.Instance._playerHealtBar.canvasGroup.DOFade(1f, .15f).SetUpdate(true);
    }
}
