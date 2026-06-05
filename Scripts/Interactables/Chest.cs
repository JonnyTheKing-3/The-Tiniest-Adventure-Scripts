using System.Collections;
using UnityEngine;

public class Chest : MonoBehaviour, IApproachable
{
    [Header("SAVE DATA")]
    [SerializeField] private string chestID;

    [SerializeField] private Animator animator;
    [SerializeField] private InventoryItemData chestItem;
    [SerializeField] private Light chestLight;
    [SerializeField] private ParticleSystem chestParticles;
    [SerializeField] private float lightFadeDuration = 1f;
    [SerializeField] private string openAnimationTrigger = "openChest";
    [SerializeField] private string closedAnimationState = "close";
    [SerializeField] private string openedAnimationState = "open";
    private bool chestIsOpen = false;
    private float initialChestListIntensity;
    private Coroutine lightFadeCoroutine;

    public string ChestID => chestID;
    public bool HasSaveID => !string.IsNullOrWhiteSpace(chestID);
    public bool IsOpen => chestIsOpen;
    [Tooltip("When chest is open, hit this to reset the chest state")]public bool resetChestTrigger = false;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        initialChestListIntensity = chestLight.intensity;
        chestIsOpen = false;
        resetChestTrigger = false;
    }


    void Update()
    {
        if (resetChestTrigger)
        {
            resetChestTrigger = false;

            if (chestIsOpen)
            {
                ResetChest();
            }
        }
    }

    public void OpenChest()
    {
        if (chestIsOpen) return;

        chestIsOpen = true;
        ChestPressTriangleManager.Instance.ForceFadeOut(this);

        Player.Instance._playerInventory.AddItem(chestItem);
        animator.SetTrigger(openAnimationTrigger);

        chestParticles.Play();
        if (lightFadeCoroutine != null) StopCoroutine(lightFadeCoroutine);
        lightFadeCoroutine = StartCoroutine(FadeLight(lightFadeDuration));

        if (chestItem.icon == null)
            ItemAddedUIManager.Instance.ShowItem(chestItem.displayName);
        else
            ItemAddedUIManager.Instance.ShowItem(chestItem.displayName, chestItem.icon);

        AudioManager.Instance.PlayChestOpen(transform);
    }

    private IEnumerator FadeLight(float duration)
    {
        if (duration <= 0f)
        {
            chestLight.intensity = 0f;
            chestLight.enabled = false;
            lightFadeCoroutine = null;
            yield break;
        }

        float startIntensity = chestLight.intensity;
        float elapsed = 0f;

        while (elapsed < duration && chestLight != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            chestLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
            yield return null;
        }

        chestLight.intensity = 0f;
        chestLight.enabled = false;
        lightFadeCoroutine = null;
    }

    public void FadeApproachUI(float targetAlpha)   // Get called by PlayerInputScript
    {
        bool isBestApproachable = Mathf.Approximately(targetAlpha, GameManager.Instance.UIApproachbleFadeBestTarget);
        float triangleTargetAlpha = !chestIsOpen && isBestApproachable ? 1f : 0f;

        ChestPressTriangleManager.Instance.FadeForChest(this, triangleTargetAlpha);
    }

    
    // Used for LoadGame() and degbugging
    public void ResetChest()
    {
        if (lightFadeCoroutine != null)
        {
            StopCoroutine(lightFadeCoroutine);
            lightFadeCoroutine = null;
        }

        chestIsOpen = false;
        animator.ResetTrigger(openAnimationTrigger);
        animator.Play(closedAnimationState, 0);
        chestLight.enabled = true;
        chestLight.intensity = initialChestListIntensity;
        chestParticles.gameObject.SetActive(true);
    }

    // Used for LoadGame()
    public void LoadFromSave(bool savedIsOpen)
    {
        if (!savedIsOpen)
        {
            ResetChest();
            return;
        }

        if (lightFadeCoroutine != null)
        {
            StopCoroutine(lightFadeCoroutine);
            lightFadeCoroutine = null;
        }

        chestIsOpen = true;
        ChestPressTriangleManager.Instance?.ForceFadeOut(this);
        animator.ResetTrigger(openAnimationTrigger);
        animator.Play(openedAnimationState, 0, 1f);
        chestLight.intensity = 0f;
        chestLight.enabled = false;
        chestParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
