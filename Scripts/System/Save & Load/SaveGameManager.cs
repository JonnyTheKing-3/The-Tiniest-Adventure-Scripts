using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SaveGameManager : MonoBehaviour
{
    public static SaveGameManager Instance;

    public ItemDatabase itemDatabase;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerEquipment playerEquipment;

    // Helpers
    Coroutine TeleportCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        loadRoutine = null;
    }

    public void SaveGame()
    {
        if (playerTransform == null || playerInventory == null || playerEquipment == null)
        {
            Debug.LogWarning("SaveGame failed: missing references.");
            return;
        }

        GameSaveData saveData = new GameSaveData();

        saveData.player.position = new SerializableVector3(playerTransform.position);
        saveData.player.playerUnlockedDashAttack = Player.Instance.DashAttackUnlocked;
        saveData.player.playerUnlockedPerfectDodge = Player.Instance.PerfectDodgeUnlocked;
        saveData.player.playerCanObtainBow = Player.Instance.CanObtainBow;
        saveData.player.playerOwnsBow = Player.Instance.OwnsBow;
        saveData.player.playerBaseCombatStats = Player.Instance.baseCombatStats;
        saveData.player.ownedItemIds = playerInventory.GetOwnedItemIds();
        saveData.player.equippedMainHandWeaponId = playerEquipment.GetEquippedMainHandWeaponId();
        saveData.player.equippedOffHandWeaponId = playerEquipment.GetEquippedOffHandWeaponId();
        saveData.player.equippedCustomization = playerEquipment.GetCustomizationSaveEntries();
        saveData.player.playerSkillTreePoints = Player.Instance.SkillTreePoints;

        saveData.gameSettings.HasAudioSettings = true;
        saveData.gameSettings.MusicVolume = AudioManager.Instance.MusicVolumeSetting;
        saveData.gameSettings.SFXVolume = AudioManager.Instance.SFXVolumeSetting;
        saveData.gameSettings.GraphicsQualityLevel = QualitySettings.GetQualityLevel();

        saveData.pauseMenu.unlockedSkills = PauseMenuManager.Instance.skillTreeMenuManager.GetUnlockedSkills();

        SaveEnemies(saveData);
        SaveChests(saveData);

        // Talkers must have ID's for it to save properly
        saveData.gameSettings.talkerNPCs.Clear();
        foreach (Talker talker in GameManager.Instance.talkerDataBase)
        {
            if (talker == null) continue;

            saveData.gameSettings.talkerNPCs.Add(new TalkerSaveData
            {
                talkerID = talker.talkerID,
                hasTalked = talker.HasTalked,
                approachIconState = talker.approachIconState
            });
        }


        SaveSystem.Save(saveData);
    }

    public Coroutine loadRoutine;
    public void LoadGame()
    {
        if (loadRoutine != null) StopCoroutine(loadRoutine);
        loadRoutine = StartCoroutine(LoadGameRoutine());  
    }
    public IEnumerator LoadGameRoutine()
    {

        if (itemDatabase == null)
        {
            Debug.LogWarning("LoadGame failed: ItemDatabase is missing.");
            yield break;
        }

        if (playerTransform == null || playerInventory == null || playerEquipment == null)
        {
            Debug.LogWarning("LoadGame failed: missing references.");
            yield break;
        }

        GameSaveData saveData = SaveSystem.Load();
        if (saveData == null) yield break;


        // Debug.Log("Loading passed null check. Save file exists");
        yield return null; // We need to return to assign this coroutine to loadRoutine
        AnimationEvents.Instance.DisablePlayerActionMap();

        if (GameManager.Instance._currPlayMode == GameManager.PlayMode.Menu)
            GameManager.Instance.PauseRoutine();

        AudioManager.Instance.FadeOutMusic();
        yield return StartCoroutine(LoadUIManager.Instance.FadeBlackLoadImage(true));
        GameOverUIManager.Instance.GameOverUIRoutine(false, 0f); // In case we load from game over

        // LOAD EVERYTHING --------------------

        // Game State
        CamerasManager.Instance.MakeCameraLoadReady();
        StartMenuManager.Instance.CleanUpStartMenu();   // Need this in case we started from start menu
        LoadEnemies(saveData);
        LoadChests(saveData);


        // Player
        Player.Instance.baseCombatStats = saveData.player.playerBaseCombatStats;
        playerInventory.LoadFromItemIds(saveData.player.ownedItemIds, itemDatabase);
        playerEquipment.LoadEquipmentFromIds(saveData.player.equippedMainHandWeaponId, saveData.player.equippedOffHandWeaponId, saveData.player.equippedCustomization, itemDatabase);
        Player.Instance.Health.RestoreToFull();
        if (Player.Instance._playerHealtBar.canvasGroup.alpha < 1f)
        {
            Player.Instance._playerHealtBar.canvasGroup.alpha = 1f;
        }
        Player.Instance.healthHasDepleted = false;
        Player.Instance.startedDeathRoutine = false;
        Player.Instance._playerAnimation.animator.CrossFade("Moving", .1f, 0);
        
        if (TeleportCoroutine != null) StopCoroutine(TeleportCoroutine);  // We need a teleport coroutine because we need locomotion state change to go to idle so movement logic doesn't interfere with teleporting the player
        TeleportCoroutine = StartCoroutine(Player.Instance._playerLocomotion.TeleportPlayer(saveData.player.position.ToVector3()));

        Player.Instance.DashAttackUnlocked = saveData.player.playerUnlockedDashAttack;
        Player.Instance.PerfectDodgeUnlocked = saveData.player.playerUnlockedPerfectDodge;
        Player.Instance.CanObtainBow = saveData.player.playerCanObtainBow;
        Player.Instance.OwnsBow = saveData.player.playerOwnsBow;
        Player.Instance.SkillTreePoints = saveData.player.playerSkillTreePoints;


        // MENU SETTINGS
        PauseMenuManager.Instance.skillTreeMenuManager.LoadTabMenuSettings(saveData.pauseMenu.unlockedSkills);
         // TO DO: Load menu settings here when we get the Music and SFX


        // GAME SETTINGS
        float musicVolume = saveData.gameSettings.HasAudioSettings ? saveData.gameSettings.MusicVolume : 10;
        float sfxVolume = saveData.gameSettings.HasAudioSettings ? saveData.gameSettings.SFXVolume : 10;
        AudioManager.Instance.SetVolume(musicVolume, true);
        AudioManager.Instance.SetVolume(sfxVolume, false);
        QualitySettings.SetQualityLevel(saveData.gameSettings.GraphicsQualityLevel, true);
        PauseMenuManager.Instance.settingsMenuManager.LoadSettingsUI(
            AudioManager.Instance.MusicVolumeSetting,
            AudioManager.Instance.SFXVolumeSetting,
            QualitySettings.GetQualityLevel()
        );
        
        foreach (TalkerSaveData savedTalker in saveData.gameSettings.talkerNPCs)
        {
            Talker talker = GameManager.Instance.talkerDataBase.Find(t => t != null && t.talkerID == savedTalker.talkerID);
            if (talker == null)
            {
                Debug.LogWarning($"No Talker found with ID: {savedTalker.talkerID}");
                continue;
            }

            talker.HasTalked = savedTalker.hasTalked;
            talker.SetApproachIcon(savedTalker.approachIconState);
        }

        GameManager.Instance._currPlayMode = GameManager.PlayMode.FreeRoam;


        yield return new WaitForSeconds(1f); // Give it a bit of time so that everything is setup
        AudioManager.Instance.FadeToOverworldTheme();
        yield return StartCoroutine(LoadUIManager.Instance.FadeBlackLoadImage(false));

        Player.Instance._playerInputScript._playerInput.SwitchCurrentActionMap("Player");
        AnimationEvents.Instance.EnablePlayerActionMap();

        loadRoutine = null;
    }

    void SaveEnemies(GameSaveData saveData)
    {
        if (saveData.enemies == null) saveData.enemies = new List<EnemySaveData>();
        saveData.enemies.Clear();
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        HashSet<string> savedEnemyIDs = new();

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null) continue;

            if (!enemy.HasSaveID)
            {
                Debug.LogWarning($"Enemy '{enemy.name}' has no enemyID. It will not be restored by save/load.", enemy);
                continue;
            }

            if (enemy.Health.currentHealth <= 0f || enemy.healthDepleted || enemy.startedDeathRoutine) continue;

            if (!savedEnemyIDs.Add(enemy.EnemyID))
            {
                Debug.LogWarning($"Duplicate enemyID '{enemy.EnemyID}' found while saving. Only the first enemy with this ID was saved.", enemy);
                continue;
            }

            saveData.enemies.Add(new EnemySaveData
            {
                enemyID = enemy.EnemyID,
                position = new SerializableVector3(enemy.transform.position),
                rotation = new SerializableQuaternion(enemy.transform.rotation)
            });
        }
    }

    void LoadEnemies(GameSaveData saveData)
    {
        if (saveData.enemies == null)
        {
            Debug.LogWarning("Save file has no enemy data. Skipping enemy restore.");
            return;
        }


        // Get all enemies in the scene with their ID's
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Dictionary<string, Enemy> enemiesByID = new();
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !enemy.HasSaveID) continue;

            if (enemiesByID.ContainsKey(enemy.EnemyID))
            {
                Debug.LogWarning($"Duplicate enemyID '{enemy.EnemyID}' found while loading. Only the first enemy with this ID will be restored.", enemy);
                continue;
            }

            enemiesByID.Add(enemy.EnemyID, enemy);
        }


        // Get all saved enemy ID's
        HashSet<string> savedEnemyIDs = new();
        foreach (EnemySaveData savedEnemy in saveData.enemies)
        {
            if (savedEnemy == null || string.IsNullOrWhiteSpace(savedEnemy.enemyID)) continue;
            savedEnemyIDs.Add(savedEnemy.enemyID);
        }

        // Set inactive all enemies in the scene that died BEFORE the last save via ID checking. Just in case
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !enemy.HasSaveID) continue;
            if (!savedEnemyIDs.Contains(enemy.EnemyID)) enemy.gameObject.SetActive(false);
        }

        // Reset enemy state for all saved enemies
        foreach (EnemySaveData savedEnemy in saveData.enemies)
        {
            if (savedEnemy == null || string.IsNullOrWhiteSpace(savedEnemy.enemyID)) continue;

            if (!enemiesByID.TryGetValue(savedEnemy.enemyID, out Enemy enemy))
            {
                Debug.LogWarning($"No Enemy found with ID: {savedEnemy.enemyID}");
                continue;
            }

            enemy.ResetFromSave(savedEnemy.position.ToVector3(), savedEnemy.rotation.ToQuaternion());
        }
    }

    void SaveChests(GameSaveData saveData)
    {
        if (saveData.chests == null) saveData.chests = new List<ChestSaveData>();
        saveData.chests.Clear();
        Chest[] chests = FindObjectsByType<Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        HashSet<string> savedChestIDs = new();

        foreach (Chest chest in chests)
        {
            if (chest == null) continue;

            if (!chest.HasSaveID)
            {
                Debug.LogWarning($"Chest '{chest.name}' has no chestID. It will not be restored by save/load.", chest);
                continue;
            }

            if (!savedChestIDs.Add(chest.ChestID))
            {
                Debug.LogWarning($"Duplicate chestID '{chest.ChestID}' found while saving. Only the first chest with this ID was saved.", chest);
                continue;
            }

            saveData.chests.Add(new ChestSaveData
            {
                chestID = chest.ChestID,
                isOpen = chest.IsOpen
            });
        }
    }

    void LoadChests(GameSaveData saveData)
    {
        Chest[] chests = FindObjectsByType<Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (saveData.chests == null)
        {
            Debug.LogWarning("Save file has no chest data. Resetting chests to their default closed state.");

            foreach (Chest chest in chests)
            {
                if (chest == null) continue;
                chest.ResetChest();
            }

            return;
        }

        // Collect all chests IDs
        Dictionary<string, Chest> chestsByID = new();
        foreach (Chest chest in chests)
        {
            if (chest == null) continue;

            if (!chest.HasSaveID)
            {
                Debug.LogWarning($"Chest '{chest.name}' has no chestID. It cannot be restored by save/load.", chest);
                chest.ResetChest();
                continue;
            }

            if (chestsByID.ContainsKey(chest.ChestID))
            {
                Debug.LogWarning($"Duplicate chestID '{chest.ChestID}' found while loading. Only the first chest with this ID will be restored.", chest);
                chest.ResetChest();
                continue;
            }

            chestsByID.Add(chest.ChestID, chest);
        }

        // Collect all saved chest IDs
        HashSet<string> savedChestIDs = new();
        foreach (ChestSaveData savedChest in saveData.chests)
        {
            if (savedChest == null || string.IsNullOrWhiteSpace(savedChest.chestID)) continue;
            savedChestIDs.Add(savedChest.chestID);
        }

        // Reset all chests not saved (just in case)
        foreach (Chest chest in chests)
        {
            if (chest == null || !chest.HasSaveID) continue;
            if (!savedChestIDs.Contains(chest.ChestID)) chest.ResetChest();
        }


        // Load proper state for all saved chests
        foreach (ChestSaveData savedChest in saveData.chests)
        {
            if (savedChest == null || string.IsNullOrWhiteSpace(savedChest.chestID)) continue;

            if (!chestsByID.TryGetValue(savedChest.chestID, out Chest chest))
            {
                Debug.LogWarning($"No Chest found with ID: {savedChest.chestID}");
                continue;
            }

            chest.LoadFromSave(savedChest.isOpen);
        }
    }
}
