using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class StartMenuManager : MonoBehaviour
{
    public static StartMenuManager Instance { get; private set; }

    CanvasGroup startCanvasGroup;
    [SerializeField] private PlayableDirector StartCutsceneDirector;
    public bool StartMenuIntro = false;


    void OnDisable() => Player.Instance._playerUIInputManager.SubmitAction.started -= OnSubmitStarted;

    void Start()
    {
        Instance = this;
        startCanvasGroup = GetComponent<CanvasGroup>();
        startCanvasGroup.alpha = StartMenuIntro ? 1f : 0f;

        if (StartMenuIntro) 
        {
            Player.Instance.gameObject.transform.root.position = GameManager.Instance.StartMenuPlayerPosition;
            StartCoroutine(Player.Instance._playerLocomotion.TeleportPlayer(Player.Instance.transform.root.position)); // reset local position
            Player.Instance.gameObject.transform.root.rotation = Quaternion.Euler(0f,0f,0f);
            AnimationEvents.Instance.DisablePlayerActionMap();
            Player.Instance._playerUIInputManager.SwitchToUI();
            Player.Instance._playerUIInputManager.SubmitAction.started += OnSubmitStarted;
            CamerasManager.Instance.MakeCameraStartReady();
        }                

    }

    void OnSubmitStarted(InputAction.CallbackContext context)
    {
        // Debug.Log("Submit pressed");
        Player.Instance._playerUIInputManager.SubmitAction.started -= OnSubmitStarted;
        
        if (SaveSystem.SaveExists())
            SaveGameManager.Instance.LoadGame();
        else 
            PlayStartCutscene();
    }

    void PlayStartCutscene()
    {
        StartCutsceneDirector.time = 0;
        StartCutsceneDirector.Evaluate();
        StartCutsceneDirector.Play();
    }

    public void CleanUpStartMenu()
    {
        startCanvasGroup.alpha = 0f;
        StartMenuIntro = false;
    }
}
