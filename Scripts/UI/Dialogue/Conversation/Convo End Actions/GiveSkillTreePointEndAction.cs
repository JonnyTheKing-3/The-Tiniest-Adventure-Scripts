using UnityEngine;

[CreateAssetMenu(fileName = "UpSkillPointEndAction", menuName = "Conversation/End Actions/Up Skill Point")]
public class GiveSkillTreePointEndAction : GiveOnFirstTimeTalkingBaseEndAction
{
    public override void ExecuteFirstTimeTalkAction(Talker context) => Player.Instance.SkillTreePoints++;
}