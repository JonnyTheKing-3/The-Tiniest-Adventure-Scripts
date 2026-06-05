using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SkillTreeMenuManager : PauseMenuTabBase
{
    public TMP_Text descriptionText;
    public TMP_Text AttackStatText;
    public TMP_Text DefenseStatText;
    public TMP_Text SkillPointsNumberText;
    public UIAutoSelectButtons autoSelectButtons;
    public UISkillTreeButton[] skillTreeButtonList;
    private List<Button> buttonList;


    void Awake() => buttonList = skillTreeButtonList.Select(x => x.btn).ToList();
    void OnEnable() => Initialize();
    void Initialize() 
    {
        autoSelectButtons.RebindButtons(buttonList, true);
        RefreshAttackStatText();
        RefreshDefenseStatText();
        RefreshSkillTreePointsText();
    }

    public void SetDescriptionText(string text) => descriptionText.text = text;
    public void RefreshAttackStatText() 
    {
        string textToDisplay = "ATK  " + Player.Instance.combatStats.Attack.ToString();
        AttackStatText.text = textToDisplay;
    }
    public void RefreshDefenseStatText() 
    { 
        string textToDisplay = "DEF  " + Player.Instance.combatStats.Defense.ToString();
        DefenseStatText.text = textToDisplay;
    }

    public void RefreshSkillTreePointsText() => SkillPointsNumberText.text = Player.Instance.SkillTreePoints.ToString();

    public void SkillTreeButtonClicked(UISkillTreeButton.SkillTreeButtonTypes buttonType)
    {
        RefreshSkillTreePointsText();

        switch (buttonType)
        {
            case UISkillTreeButton.SkillTreeButtonTypes.DashAttack:     DashAttackButtonClicked();      break;
            case UISkillTreeButton.SkillTreeButtonTypes.PerfectDodge:   PerfectDodgeButtonClicked();    break;
            case UISkillTreeButton.SkillTreeButtonTypes.ATK:            ATKButtonClicked();             break;
            case UISkillTreeButton.SkillTreeButtonTypes.DEF:            DEFButtonClicked();             break;
        }
    }
    public void DashAttackButtonClicked() => Player.Instance.DashAttackUnlocked = true;
    public void PerfectDodgeButtonClicked() => Player.Instance.PerfectDodgeUnlocked = true;
    public void ATKButtonClicked()
    {
        Player.Instance.baseCombatStats.Attack += 1;
        RefreshAttackStatText();
    }
    public void DEFButtonClicked()
    {
        Player.Instance.baseCombatStats.Defense += 1;
        RefreshDefenseStatText();
    }

    public bool[] GetUnlockedSkills()
    {
        bool[] unlockedSkills = new bool[skillTreeButtonList.Length];

        for (int i = 0; i < skillTreeButtonList.Length; i++)
        {
            unlockedSkills[i] = skillTreeButtonList[i].isSkillUnlocked;
        }

        return unlockedSkills;
    }   
    public void LoadTabMenuSettings(bool[] boolArray)
    {
        // Set all button image alphas to their original values. Unlock/Lock methods set alpha corrections as needed
        foreach (var button in skillTreeButtonList) { button.SetOriginalButtonAlpha(); }


        // We need to lock all buttons first because if we lock/unlock in the same pass, we may lock the navigation of a button that was unlocked by the an unlock button (unlocking a button makes the button next to it navigatable)  
        for (int i = 0; i < skillTreeButtonList.Length; i++)
        {
            skillTreeButtonList[i].LockButton();
        }
        
        for (int i = 0; i < skillTreeButtonList.Length; i++)
        {
            if (boolArray[i]) skillTreeButtonList[i].UnlockButton();
        }
    }
}
    