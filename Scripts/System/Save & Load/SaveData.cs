using System;
using System.Collections.Generic;

// Add menu save data

[Serializable]
public class GameSaveData
{
    public PlayerSaveData player = new PlayerSaveData();
    public PauseMenuSaveData pauseMenu = new PauseMenuSaveData();
    public GameSettingsSaveData gameSettings = new GameSettingsSaveData();
    public List<EnemySaveData> enemies = new();
    public List<ChestSaveData> chests = new();
}

[Serializable]
public class PlayerSaveData     // Add fields here as needed for saving/loading player data
{
    public SerializableVector3 position;
    public bool playerUnlockedDashAttack;
    public bool playerUnlockedPerfectDodge;
    public bool playerCanObtainBow;
    public bool playerOwnsBow;

    public CombatStats playerBaseCombatStats;

    public List<string> ownedItemIds = new();
    public string equippedMainHandWeaponId;
    public string equippedOffHandWeaponId;
    public List<CustomizationSaveEntry> equippedCustomization = new();
    public int playerSkillTreePoints;
}

[Serializable]
public class PauseMenuSaveData
{
    // MAP SETTINGS


    // INVENTORY SETTINGS


    // SKILL TREE SETTINGS
    public bool[] unlockedSkills;

    // SETTINGS MENU
}

[Serializable]
public class GameSettingsSaveData
{
    public int GraphicsQualityLevel;

    public bool HasAudioSettings;       // Need this because old save files will not have these fields. Without a gaurd, ints can become = 0
    public float MusicVolume = 10;
    public float SFXVolume = 10;

    public List<TalkerSaveData> talkerNPCs = new();
}

[Serializable]
public class TalkerSaveData
{
    public string talkerID;
    public bool hasTalked;
    public TalkerApproachIconState approachIconState;
}

[Serializable]
public class EnemySaveData
{
    public string enemyID;
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
}

[Serializable]
public class ChestSaveData
{
    public string chestID;
    public bool isOpen;
}

[Serializable]
public class CustomizationSaveEntry
{
    public string slot;
    public string itemId;
}

[Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public SerializableVector3(UnityEngine.Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public UnityEngine.Vector3 ToVector3()
    {
        return new UnityEngine.Vector3(x, y, z);
    }
}

[Serializable]
public struct SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public SerializableQuaternion(UnityEngine.Quaternion q)
    {
        x = q.x;
        y = q.y;
        z = q.z;
        w = q.w;
    }

    public UnityEngine.Quaternion ToQuaternion()
    {
        return new UnityEngine.Quaternion(x, y, z, w);
    }
}
