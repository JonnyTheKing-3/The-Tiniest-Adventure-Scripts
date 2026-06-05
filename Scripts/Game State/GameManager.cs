using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public enum PlayMode { FreeRoam, Dialogue, Menu, Cutscene, QuickEvent, GameOver };
    public PlayMode _currPlayMode;
    public LayerMask playerLayer;


    public List<Talker> talkerDataBase = new List<Talker>();    // Assign this in the editor. Talkers must have ID's to save properly. Exclude dummies
    public float ApproachableActiveDistance = 4f;
    public float UIApproachbleFadeBestTarget;
    public float UIApproachbleFadeNormalTarget;
    public float UIApproachbleFadeDuration = 0.2f;
    public Sprite HasSomethingToGiveIcon;
    public Sprite HasAlreadyGivenSomthingIcon;
    public Sprite RegularConvoIcon;

    [Space]
    public Volume pp_PerfectDodgeVolume;
    public Volume pp_PauseVolume;

    [Space]
    public Vector3 StartMenuCameraPosition; // Vector3(-30.8507462,5.38663816,-66.9862671)
    public Vector3 StartMenuCameraRotation; // Vector3(2.95734286,71.8817139,359.886932)
    public Vector3 StartMenuPlayerPosition; // Vector3(-14.5299997,1.29999995,-80)

    [Space]
    public GameObject hitImpactVFXprefab;
    public int hitImpactVFXPoolSize;
    private List<GameObject> HitImpactPool = new();

    [Space]
    [SerializeField] private GameObject enemyDeathVFXPrefab;
    [SerializeField] private GameObject HitGroundKnockbackVFXPrefab;

    [Header("Combat Music")]
    [SerializeField] private float returnToOverworldMusicDelay = 4f;
    public readonly HashSet<Enemy> enemiesInCombat = new();
    private Coroutine returnToOverworldMusicCoroutine;


    void TalkerDataBaseCountCheck()
    {
        // Make sure talkerDataBase count matches the talkers in the scene
        Talker[] talkersInScene = FindObjectsByType<Talker>(FindObjectsSortMode.None);
        int talkerCounter = 0;
        
        foreach(Talker t in talkersInScene)
        {
            if (!t.isDummy) talkerCounter++;

            // Debug.Log("Talker in scene: " + t.name);
        }
        
        // Debug.Log("Number of talkers in scene: " + talkerCount + " --- Number of talkers in database: " + talkerDataBase.Count);

        if (talkerCounter != talkerDataBase.Count) 
            Debug.Log("talkerDataBase count does not match the number of talkers in scene!!!");
    }

    void Awake()
    {
        Instance = this;

        TalkerDataBaseCountCheck();

        for (int i = 0; i < hitImpactVFXPoolSize; i++)
        {
            GameObject obj = Instantiate(hitImpactVFXprefab, transform);
            obj.SetActive(false);
            HitImpactPool.Add(obj);
        }
    }

    void Start()
    {
        _currPlayMode = PlayMode.FreeRoam;
        lockOnAction = Player.Instance._playerInputScript._playerInput.actions["LockOn"];
        lockOnButton = lockOnAction.controls[0] as ButtonControl;
        lockOnAction.wantsInitialStateCheck = true;

        // if (!AudioManager.Instance.OverworldMusicIsPlaying()) AudioManager.Instance.FadeToOverworldTheme();
    }

    // Enemies get registered in EnemyBrain combat state
    public void RegisterEnemyInCombat(Enemy enemy)
    {
        if (enemy == null) return;
        if (!enemiesInCombat.Add(enemy)) return;

        if (returnToOverworldMusicCoroutine != null)
        {
            StopCoroutine(returnToOverworldMusicCoroutine);
            returnToOverworldMusicCoroutine = null;
        }

        if (enemiesInCombat.Count > 0 && !AudioManager.Instance.BattleMusicIsPlaying())
        {
            AudioManager.Instance.FadeToBattleTheme();
        }
    }

    public void UnregisterEnemyInCombat(Enemy enemy)
    {
        if (enemy == null) return;
        if (!enemiesInCombat.Remove(enemy)) return;

        if (enemiesInCombat.Count == 0)
        {
            if (returnToOverworldMusicCoroutine != null) StopCoroutine(returnToOverworldMusicCoroutine);
            returnToOverworldMusicCoroutine = StartCoroutine(ReturnToOverworldMusicAfterDelay());
        }
    }

    private IEnumerator ReturnToOverworldMusicAfterDelay()
    {
        yield return new WaitForSeconds(returnToOverworldMusicDelay);
        returnToOverworldMusicCoroutine = null;

        if (enemiesInCombat.Count == 0 && AudioManager.Instance.BattleMusicIsPlaying())
        {
            AudioManager.Instance.FadeToOverworldTheme();
        }
    }

    public void RunInBackgroundEditorToggle(bool runInBackground)
    {
         // Need this because if we use controller, we need the UI Input Module to focus so that controller works
        #if UNITY_EDITOR
        Application.runInBackground = runInBackground;
        #endif
    }

    public void GameOver(bool entering)
    {
        if (entering) 
        {
            _currPlayMode = PlayMode.GameOver;
            Player.Instance._playerInputScript._playerInput.SwitchCurrentActionMap("UI");
            GameOverUIManager.Instance.GameOverUIRoutine(entering);
            AudioManager.Instance.FadeOutMusic();
        }
        else
        {
            SaveGameManager.Instance.LoadGame();
        }
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public GameObject GetHitImpactVFX()
    {
        foreach (GameObject obj in HitImpactPool)
        {
            if (!obj.activeInHierarchy)
                return obj;
        }

        Debug.LogWarning("No Hit Impact VFX available ");
        return null;
    }

    public void PlayDeathVFX(Vector3 position) => Instantiate(enemyDeathVFXPrefab, position, Quaternion.identity);
    public void PlayHitGroundKnockbackVFX(Vector3 position, Transform t)
    {
        Instantiate(HitGroundKnockbackVFXPrefab, position, Quaternion.identity);
        AudioManager.Instance.PlayhitGround(t);
    }

    public void SetGraphicsQuality(int qualityLevel)
    {
        if (qualityLevel < 0 || qualityLevel >= QualitySettings.names.Length)
        {
            Debug.LogWarning("Invalid quality level: " + qualityLevel);
            return;
        }
        
        QualitySettings.SetQualityLevel(qualityLevel);
    }


    public void SetTimeScale(float scale) => Time.timeScale = scale;

    [HideInInspector] public Coroutine timeScaleCoroutine;
    public float GetTimeScale() => Time.timeScale;
    public void SetTimeScale(float scale, float duration = 0f)
    {
        if (duration <= 0f)
        {
            Time.timeScale = scale;
            return;
        }

        if (timeScaleCoroutine != null) StopCoroutine(timeScaleCoroutine);

        timeScaleCoroutine = StartCoroutine(SetTimeScaleSmoothly(scale, duration));
    }
    public IEnumerator SetTimeScaleSmoothly(float scale, float duration)
    {
        float startScale = Time.timeScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (_currPlayMode == PlayMode.Menu) yield return null;

            Time.timeScale = Mathf.Lerp(startScale, scale, elapsed / duration);
            elapsed += Time.unscaledDeltaTime; // Use unscaled time to ensure it works even when timeScale is changing
            yield return null;
        }

        Time.timeScale = scale; // Ensure it ends at the exact target scale
    }


    // Perfect Dodge Volume Controls
    [HideInInspector] public Coroutine setPerfectDodgeVolumeWeightCoroutine;
    public float GetPerfectDodgeVolumeWeight() => pp_PerfectDodgeVolume.weight;
    public void SetPerfectDodgeVolumeWeight(float weight, float duration = 0f)
    {
        if (duration <= 0f)
        {
            pp_PerfectDodgeVolume.weight = weight;
            return;
        }

        if (setPerfectDodgeVolumeWeightCoroutine != null) StopCoroutine(setPerfectDodgeVolumeWeightCoroutine);

        setPerfectDodgeVolumeWeightCoroutine = StartCoroutine(SetPerfectDodgeVolumeWeightSmoothly(weight, duration));
    }
    public IEnumerator SetPerfectDodgeVolumeWeightSmoothly(float targetWeight, float duration)
    {
        float startWeight = pp_PerfectDodgeVolume.weight;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (_currPlayMode == PlayMode.Menu) yield return null;

            pp_PerfectDodgeVolume.weight = Mathf.Lerp(startWeight, targetWeight, elapsed / duration);
            elapsed += Time.unscaledDeltaTime; // Use unscaled time to ensure it works even when timeScale is changing
            yield return null;
        }

        pp_PerfectDodgeVolume.weight = targetWeight; // Ensure it ends at the exact target weight
    }


    [HideInInspector] public Coroutine setPerfectDodgeVolumeVignetteColorCoroutine;
    public Color GetPerfectDodgeVolumeVignetteColor()
    {
        if (pp_PerfectDodgeVolume.profile.TryGet<Vignette>(out Vignette vignette))
        {
            return vignette.color.value;
        }
        return Color.clear;
    }
    public void SetPerfectDodgeVolumeVignetteColor(Color color, float duration = 0f)
    {
        if (duration <= 0f)
        {
            if (pp_PerfectDodgeVolume.profile.TryGet<Vignette>(out Vignette vignette))
            {
                vignette.color.value = color;
            }
            return;
        }
        
        if (setPerfectDodgeVolumeVignetteColorCoroutine != null) StopCoroutine(setPerfectDodgeVolumeVignetteColorCoroutine);

        setPerfectDodgeVolumeVignetteColorCoroutine = StartCoroutine(SetPerfectDodgeVolumeVignetteColorSmoothly(color, duration));
    }
    public IEnumerator SetPerfectDodgeVolumeVignetteColorSmoothly(Color color, float duration)
    {
        if (pp_PerfectDodgeVolume.profile.TryGet<Vignette>(out Vignette vignette))
        {
            Color startColor = vignette.color.value;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_currPlayMode == PlayMode.Menu) yield return null;

                vignette.color.value = Color.Lerp(startColor, color, elapsed / duration);
                elapsed += Time.unscaledDeltaTime; // Use unscaled time to ensure it works even when timeScale is changing
                yield return null;
            }

            vignette.color.value = color; // Ensure it ends at the exact target color            
        }
    }

    [HideInInspector] public Coroutine setPerfectDodgeVolumeVignetteIntensityCoroutine;
    public float GetPerfectDodgeVolumeVignetteIntensity()
    {
        if (pp_PerfectDodgeVolume.profile.TryGet<Vignette>(out Vignette vignette))
        {
            return vignette.intensity.value;
        }
        return -1f;
    }
    public void SetPerfectDodgeVolumeVignetteIntensity(float intensity, float duration = 0f)
    {
        if (duration <= 0f)
        {
            if (pp_PerfectDodgeVolume.profile.TryGet<Vignette>(out Vignette vignette))
            {
                vignette.intensity.value = intensity;
            }
            return;
        }
        
        if (setPerfectDodgeVolumeVignetteIntensityCoroutine != null) StopCoroutine(setPerfectDodgeVolumeVignetteIntensityCoroutine);

        setPerfectDodgeVolumeVignetteIntensityCoroutine = StartCoroutine(SetPerfectDodgeVolumeVignetteIntensitySmoothly(intensity, duration));
    }
    public IEnumerator SetPerfectDodgeVolumeVignetteIntensitySmoothly(float intensity, float duration)
    {
        if (pp_PerfectDodgeVolume.profile.TryGet<Vignette>(out Vignette vignette))
        {
            float startIntensity = vignette.intensity.value;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_currPlayMode == PlayMode.Menu) yield return null;

                vignette.intensity.value = Mathf.Lerp(startIntensity, intensity, elapsed / duration);
                elapsed += Time.unscaledDeltaTime; // Use unscaled time to ensure it works even when timeScale is changing
                yield return null;
            }

            vignette.intensity.value = intensity; // Ensure it ends at the exact target intensity            
        }
    }

    [HideInInspector] public Coroutine setPerfectDodgeVolumeVignetteSmoothnessCoroutine;
    public float GetPerfectDodgeVolumeVignetteSmoothness()
    {
        if (pp_PerfectDodgeVolume.profile.TryGet<Vignette>(out Vignette vignette))
        {
            return vignette.smoothness.value;
        }
        return -1f;
    }
    public void SetPerfectDodgeVolumeVignetteSmoothness(float smoothness, float duration = 0f)
    {
        if (duration <= 0f)
        {
            if (pp_PerfectDodgeVolume.profile.TryGet<Vignette>(out Vignette vignette))
            {
                vignette.smoothness.value = smoothness;
            }
            return;
        }
        
        if (setPerfectDodgeVolumeVignetteSmoothnessCoroutine != null) StopCoroutine(setPerfectDodgeVolumeVignetteSmoothnessCoroutine);

        setPerfectDodgeVolumeVignetteSmoothnessCoroutine = StartCoroutine(SetPerfectDodgeVolumeVignetteSmoothnessSmoothly(smoothness, duration));
    }
    public IEnumerator SetPerfectDodgeVolumeVignetteSmoothnessSmoothly(float smoothness, float duration)
    {
        if (pp_PerfectDodgeVolume.profile.TryGet<Vignette>(out Vignette vignette))
        {
            float startSmoothness = vignette.smoothness.value;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_currPlayMode == PlayMode.Menu) yield return null;

                vignette.smoothness.value = Mathf.Lerp(startSmoothness, smoothness, elapsed / duration);
                elapsed += Time.unscaledDeltaTime; // Use unscaled time to ensure it works even when timeScale is changing
                yield return null;
            }

            vignette.smoothness.value = smoothness; // Ensure it ends at the exact target smoothness            
        }
    }


    [HideInInspector] public Coroutine setPerfectDodgeVolumeRadialBlurSmoothnessCoroutine;
    public float GetPerfectDodgeVolumeRadialBlursmoothness()
    {
        if (pp_PerfectDodgeVolume.profile.TryGet<RadialBlurEffect.RadialBlurVolume>(out RadialBlurEffect.RadialBlurVolume radialBlur))
        {
            return radialBlur.smoothness.value;
        }
        return -1f;
    }
    public void SetPerfectDodgeVolumeRadialBlursmoothness(float smoothness, float duration = 0f)
    {
        if (duration <= 0f)
        {
            if (pp_PerfectDodgeVolume.profile.TryGet<RadialBlurEffect.RadialBlurVolume>(out RadialBlurEffect.RadialBlurVolume radialBlur))
            {
                radialBlur.smoothness.value = smoothness;
            }
            return;
        }
        
        if (setPerfectDodgeVolumeRadialBlurSmoothnessCoroutine != null) StopCoroutine(setPerfectDodgeVolumeRadialBlurSmoothnessCoroutine);

        setPerfectDodgeVolumeRadialBlurSmoothnessCoroutine = StartCoroutine(SetPerfectDodgeVolumeRadialBlursmoothnessSmoothly(smoothness, duration));
    }
    public IEnumerator SetPerfectDodgeVolumeRadialBlursmoothnessSmoothly(float smoothness, float duration)
    {
        if (pp_PerfectDodgeVolume.profile.TryGet<RadialBlurEffect.RadialBlurVolume>(out RadialBlurEffect.RadialBlurVolume radialBlur))
        {
            float startSmoothness = radialBlur.smoothness.value;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_currPlayMode == PlayMode.Menu) yield return null;

                radialBlur.smoothness.value = Mathf.Lerp(startSmoothness, smoothness, elapsed / duration);
                elapsed += Time.unscaledDeltaTime; // Use unscaled time to ensure it works even when timeScale is changing
                yield return null;
            }

            radialBlur.smoothness.value = smoothness; // Ensure it ends at the exact target smoothness            
        }
    }



    // Pause Controls
    [HideInInspector] public Coroutine setPauseVolumeWeightCoroutine;
    public float GetPauseVolumeWeight() => pp_PauseVolume.weight;
    public void PauseRoutine() // Gets called by PlayerInputScript & PauseMenuManager when pause input is triggered
    {
        if (StartMenuManager.Instance.StartMenuIntro) return;
        if (_currPlayMode == PlayMode.Dialogue) return;

        // Get pause action
        bool? pauseAction = _currPlayMode switch 
        {
            PlayMode.FreeRoam => true, 
            PlayMode.Menu => false, 
            _ => null
        };

        // Act upon pause action
        switch (pauseAction)
        {
            case true:  SetPauseState(true); break;
            case false: SetPauseState(false); break;
        }
    }
    public void SetPauseState(bool pause)
    {
        if (pause) CachePauseStateSettings();

        SetTimeScale(pause ? 0f : cachedTimeScale);
        SetPauseVolumeWeight(pause ? 1f : 0f);
        _currPlayMode = pause ? PlayMode.Menu : PlayMode.FreeRoam;

        if (pause) 
        {
            Player.Instance._playerInputScript._playerInput.SwitchCurrentActionMap("UI");
        }
        else 
        {

            // If we load from a menu scene, we don't reactive input. We let the load do that istead
            if (SaveGameManager.Instance.loadRoutine == null)
            {
                Player.Instance._playerInputScript._playerInput.SwitchCurrentActionMap("Player");
            }

            if (cachedCameraState == CamerasManager.CameraStates.LockOn && !lockOnButton.isPressed) Player.Instance._playerInputScript.OnLockOnCanceled(new InputAction.CallbackContext());
        }

        PauseMenuManager.Instance.TogglePause(pause);
    }
    float cachedTimeScale; 
    [HideInInspector] public CamerasManager.CameraStates cachedCameraState; 
    [HideInInspector]public InputAction lockOnAction;
    ButtonControl lockOnButton;
    public void CachePauseStateSettings() // Use this to cache current state of things like timescale, camera mode, etc
    {
        cachedTimeScale = Time.timeScale;
        cachedCameraState = CamerasManager.Instance.CameraState;
    }
    public void SetPauseVolumeWeight(float weight, float duration = 0f)
    {
        if (duration <= 0f)
        {
            pp_PauseVolume.weight = weight;
            return;
        }

        if (setPauseVolumeWeightCoroutine != null) StopCoroutine(setPauseVolumeWeightCoroutine);

        setPauseVolumeWeightCoroutine = StartCoroutine(SetPauseVolumeWeightSmoothly(weight, duration));
    }
    public IEnumerator SetPauseVolumeWeightSmoothly(float targetWeight, float duration)
    {
        float startWeight = pp_PauseVolume.weight;
        float elapsed = 0f;

        while (elapsed < duration)
        {            
            pp_PauseVolume.weight = Mathf.Lerp(startWeight, targetWeight, elapsed / duration);
            elapsed += Time.unscaledDeltaTime; // Use unscaled time to ensure it works even when timeScale is changing
            yield return null;
        }

        pp_PauseVolume.weight = targetWeight; // Ensure it ends at the exact target weight
    }

}
