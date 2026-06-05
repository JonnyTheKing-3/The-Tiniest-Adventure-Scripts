using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    // Waves are played by the audio sources on the shore objects

    public static AudioManager Instance { get; private set; }

    public enum CurrentMusic { Overworld, Battle, None }

    public CurrentMusic currentMusic = CurrentMusic.None;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource dialogueBleepSource;
    [SerializeField] private float defaultMusicFadeDuration = 1.5f;

    [Space]
    [Header("MUSIC")]
    public AudioObject overWorldTheme;
    public AudioObject battleTheme;
    public AudioObject perfectDogde;

    [Header("SFX")]
    public AudioObject UIPageScroll;
    public AudioObject UIButtonClick;
    public AudioObject UIequip;
    public AudioObject UIDialogueBleep;
    [Space]
    public AudioObject smokePoof;
    public AudioObject bowRelease;
    public AudioObject hitImpact;
    public AudioObject dogde;
    public AudioObject counterConfirm;
    [Space]
    public AudioObject hitGround;
    public AudioObject swim;
    public AudioObject chestOpen1;
    public AudioObject chestOpen2;
    [Space]   // I'd use an random audio container, but it's honestly cleaner to use this array instead of having to modify the codein AudioObject
    public AudioObject[] grassyFootsteps;
    public AudioObject[] hardFootsteps;

    private Coroutine musicFadeCoroutine;
    private Coroutine perfectDodgeSFXFadeCoroutine;
    private readonly List<GameObject> activeOneShotSFX = new();     // Need this to clean up any active one shot vfx on quitting the game
    private AudioSource perfectDodgeSFXSource;
    private bool isShuttingDown;
    private float musicVolume;
    private bool perfectDodgeMusicDucked;
    private float prePerfectDodgeMusicVolume;
    private const string MUSIC_VOLUME = "Music_Volume";
    private const string SFX_VOLUME = "SFX_Volume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        musicVolume = musicSource.volume;
        sfxSource.volume = 1f;
        musicSource.volume = 1f;

        if (dialogueBleepSource == null)
        {
            dialogueBleepSource = gameObject.AddComponent<AudioSource>();
        }

        CopySFXSettings(dialogueBleepSource);
        dialogueBleepSource.spatialBlend = 0f;
    }

    private void OnApplicationQuit()
    {
        isShuttingDown = true;
    }

    private void OnDestroy()
    {
        isShuttingDown = true;
    }


    public float MusicVolumeSetting { get; private set; } = 10;
    public float SFXVolumeSetting { get; private set; } = 10;
    public void SetVolume(float volume, bool musicVolume)
    {
        if (musicVolume) MusicVolumeSetting = volume;
        else             SFXVolumeSetting   = volume;

        // Audio passes in a number between 0-10. We need the value to be between 0-1
        volume /= 10f;

        // Audio volume is a logarithmic function, not linear. So we make sure to fit our value in accordance (no 0 value because log doesn't hit zero + times 20 so it hits the lower decibels)
        volume = Mathf.Max(volume, 0.0001f); // protect from a zero value that isn't actually zero

        string correctVolume = musicVolume ? MUSIC_VOLUME : SFX_VOLUME;
        audioMixer.SetFloat(correctVolume, Mathf.Log(volume) * 20);
    }

    // Music
    public bool isPlayingMusic() => musicSource.isPlaying;
    public bool OverworldMusicIsPlaying() => currentMusic == CurrentMusic.Overworld;
    public bool BattleMusicIsPlaying() => currentMusic == CurrentMusic.Battle;
    public void PlayMusic(AudioObject music, bool loop = true)
    {
        if (music?.clip == null) return;

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
            musicSource.volume = musicVolume;
        }

        currentMusic = GetCurrentMusicType(music);
        musicSource.volume = musicVolume * music.volume;
        musicSource.clip = music.clip;
        musicSource.loop = loop;
        musicSource.Play();
    }
    public void StopMusic()
    {
        currentMusic = CurrentMusic.None;
        musicSource.Stop();
    }

    public void DuckMusicForPerfectDodge(float volumeMultiplier = 0.2f)
    {
        if (perfectDodgeMusicDucked) return;

        perfectDodgeMusicDucked = true;
        prePerfectDodgeMusicVolume = musicSource.volume;
        musicSource.volume *= Mathf.Clamp01(volumeMultiplier);
    }
    public void RestoreMusicAfterPerfectDodge()
    {
        if (!perfectDodgeMusicDucked) return;

        musicSource.volume = prePerfectDodgeMusicVolume;
        perfectDodgeMusicDucked = false;
    }


    public void FadeToMusic(AudioObject music, bool loop = true) => FadeToMusic(music, defaultMusicFadeDuration, loop);
    public void FadeToMusic(AudioObject music, float fadeDuration, bool loop = true)
    {
        if (music?.clip == null) return;

        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeToMusicRoutine(music, Mathf.Max(0f, fadeDuration), loop));
    }
    public void FadeOutMusic() => FadeOutMusic(defaultMusicFadeDuration);
    public void FadeOutMusic(float fadeDuration)
    {
        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeOutMusicRoutine(Mathf.Max(0f, fadeDuration)));
    }
    private IEnumerator FadeToMusicRoutine(AudioObject music, float fadeDuration, bool loop)
    {
        float targetVolume = musicVolume * music.volume;

        if (fadeDuration <= 0f)
        {
            musicSource.volume = targetVolume;
            musicSource.clip = music.clip;
            musicSource.loop = loop;
            musicSource.Play();
            musicFadeCoroutine = null;
            currentMusic = GetCurrentMusicType(music);
            yield break;
        }


        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }
        }

        musicSource.volume = 0f;
        musicSource.clip = music.clip;
        musicSource.loop = loop;
        musicSource.Play();

        float fadeInElapsed = 0f;
        while (fadeInElapsed < fadeDuration)
        {
            fadeInElapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, fadeInElapsed / fadeDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        musicFadeCoroutine = null;
        currentMusic = GetCurrentMusicType(music);
    }
    private IEnumerator FadeOutMusicRoutine(float fadeDuration)
    {
        if (fadeDuration <= 0f || !musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = musicVolume;
            musicFadeCoroutine = null;
            currentMusic = CurrentMusic.None;
            yield break;
        }

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = musicVolume;
        musicFadeCoroutine = null;
        currentMusic = CurrentMusic.None;
    }

    private CurrentMusic GetCurrentMusicType(AudioObject music)
    {
        if (music == overWorldTheme) return CurrentMusic.Overworld;
        if (music == battleTheme) return CurrentMusic.Battle;
        return CurrentMusic.None;
    }

    public void PlayOverworldTheme() => PlayMusic(overWorldTheme, true);
    public void PlayBattleTheme() => PlayMusic(battleTheme, true);

    public void FadeToOverworldTheme() => FadeToMusic(overWorldTheme, true);
    public void FadeToBattleTheme() => FadeToMusic(battleTheme, true);


    // SFX
    public void PlaySFX(AudioClip sfxClip, Transform playFrom, bool makeChild) => PlaySFX(sfxClip, playFrom, makeChild, 1f);
    public void PlaySFX(AudioClip sfxClip, Transform playFrom, bool makeChild, float volumeMultiplier)
    {
        PlaySFX(new AudioObject { clip = sfxClip, volume = 1f }, playFrom, makeChild, volumeMultiplier);
    }
    public void PlaySFX(AudioObject sfx, Transform playFrom, bool makeChild) => PlaySFX(sfx, playFrom, makeChild, 1f);
    public void PlaySFX(AudioObject sfx, Transform playFrom, bool makeChild, float volumeMultiplier)
    {
        if (isShuttingDown) return;
        if (sfx?.clip == null) return;

        // Prepare audio
        playFrom ??= Player.Instance.transform;
        AudioSource source = CreateSFXSource(playFrom);
        if (makeChild) source.transform.SetParent(playFrom);

        source.volume *= sfx.volume * volumeMultiplier;
        source.clip = sfx.clip;
        source.loop = false;
        source.Play();

        StartCoroutine(DestroyOneShotSFXAfterDelay(source.gameObject, sfx.clip.length));
    }
    private void PlayUISFX(AudioObject sfx)
    {
        if (isShuttingDown) return;
        if (sfx?.clip == null) return;

        AudioSource source = CreateSFXSource(transform);
        source.spatialBlend = 0f;
        source.volume *= sfx.volume;
        source.clip = sfx.clip;
        source.loop = false;
        source.Play();

        StartCoroutine(DestroyOneShotSFXAfterDelay(source.gameObject, sfx.clip.length));
    }
    private void PlayDialogueBleep()
    {
        if (UIDialogueBleep?.clip == null) return;

        dialogueBleepSource.Stop();
        dialogueBleepSource.volume = sfxSource.volume * UIDialogueBleep.volume;
        dialogueBleepSource.clip = UIDialogueBleep.clip;
        dialogueBleepSource.loop = false;
        dialogueBleepSource.Play();
    }
    private void PlayTrackedPerfectDodgeSFX(Transform playFrom, bool makeChild)
    {
        if (isShuttingDown) return;
        if (perfectDogde?.clip == null) return;

        StopPerfectDodgeSFXFade();
        CleanupPerfectDodgeSFXSource();

        playFrom ??= Player.Instance.transform;
        perfectDodgeSFXSource = CreatePerfectDodgeSFXSource(playFrom);
        if (makeChild) perfectDodgeSFXSource.transform.SetParent(playFrom);

        perfectDodgeSFXSource.volume *= perfectDogde.volume;
        perfectDodgeSFXSource.clip = perfectDogde.clip;
        perfectDodgeSFXSource.loop = false;
        perfectDodgeSFXSource.Play();

        StartCoroutine(DestroyOneShotSFXAfterDelay(perfectDodgeSFXSource.gameObject, perfectDogde.clip.length));
    }
    private AudioSource CreateSFXSource(Transform playFrom)
    {
        GameObject sfxObject = new GameObject("One Shot SFX");
        activeOneShotSFX.Add(sfxObject);
        sfxObject.transform.SetPositionAndRotation(playFrom.position, playFrom.rotation);
        sfxObject.transform.SetParent(transform, true);

        AudioSource source = sfxObject.AddComponent<AudioSource>();
        CopySFXSettings(source);

        return source;
    }
    private AudioSource CreatePerfectDodgeSFXSource(Transform playFrom)
    {
        GameObject sfxObject = new GameObject("Perfect Dodge SFX");
        activeOneShotSFX.Add(sfxObject);
        sfxObject.transform.SetPositionAndRotation(playFrom.position, playFrom.rotation);
        sfxObject.transform.SetParent(transform, true);

        AudioSource source = sfxObject.AddComponent<AudioSource>();
        CopyMusicSettings(source);
        source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        source.volume = 1f;

        return source;
    }
    private IEnumerator DestroyOneShotSFXAfterDelay(GameObject sfxObject, float delay)
    {
        yield return new WaitForSeconds(delay);

        activeOneShotSFX.Remove(sfxObject);

        if (sfxObject != null)
            Destroy(sfxObject);
    }
    
    private void StopPerfectDodgeSFXFade()
    {
        if (perfectDodgeSFXFadeCoroutine == null) return;

        StopCoroutine(perfectDodgeSFXFadeCoroutine);
        perfectDodgeSFXFadeCoroutine = null;
    }
    private void CleanupPerfectDodgeSFXSource()
    {
        if (perfectDodgeSFXSource == null) return;

        GameObject sfxObject = perfectDodgeSFXSource.gameObject;
        activeOneShotSFX.Remove(sfxObject);
        Destroy(sfxObject);
        perfectDodgeSFXSource = null;
    }
    private IEnumerator FadeOutPerfectDodgeSFXRoutine(float duration)
    {
        if (perfectDodgeSFXSource == null)
        {
            perfectDodgeSFXFadeCoroutine = null;
            yield break;
        }

        if (duration <= 0f)
        {
            CleanupPerfectDodgeSFXSource();
            perfectDodgeSFXFadeCoroutine = null;
            yield break;
        }

        AudioSource source = perfectDodgeSFXSource;
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration && source != null)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        if (source == perfectDodgeSFXSource)
            CleanupPerfectDodgeSFXSource();

        perfectDodgeSFXFadeCoroutine = null;
    }
    private void CopySFXSettings(AudioSource source)
    {
        source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        source.volume = sfxSource.volume;
        source.pitch = sfxSource.pitch;
        source.priority = sfxSource.priority;

        // 3D audio settings copied from your template source
        source.spatialBlend = sfxSource.spatialBlend;
        source.rolloffMode = sfxSource.rolloffMode;
        source.minDistance = sfxSource.minDistance;
        source.maxDistance = sfxSource.maxDistance;
        source.dopplerLevel = sfxSource.dopplerLevel;
        source.spread = sfxSource.spread;
        source.reverbZoneMix = sfxSource.reverbZoneMix;
    }
    private void CopyMusicSettings(AudioSource source)
    {
        source.outputAudioMixerGroup = musicSource.outputAudioMixerGroup;
        source.volume = musicSource.volume;
        source.pitch = musicSource.pitch;
        source.priority = musicSource.priority;

        source.spatialBlend = musicSource.spatialBlend;
        source.rolloffMode = musicSource.rolloffMode;
        source.minDistance = musicSource.minDistance;
        source.maxDistance = musicSource.maxDistance;
        source.dopplerLevel = musicSource.dopplerLevel;
        source.spread = musicSource.spread;
        source.reverbZoneMix = musicSource.reverbZoneMix;
    }

    private AudioObject GetRandomSFX(AudioObject[] sfxClips) => sfxClips[Random.Range(0, sfxClips.Length)];
    
    public void PlaySmokePoof(Transform playFrom, bool makeChild = false) => PlaySFX(smokePoof, playFrom, makeChild);
    public void PlayBowRelease(Transform playFrom, bool makeChild = false) => PlaySFX(bowRelease, playFrom, makeChild);
    public void PlayUIPageScroll(Transform playFrom, bool makeChild = false) => PlayUISFX(UIPageScroll);
    public void PlayUIButtonClick(Transform playFrom, bool makeChild = false) => PlayUISFX(UIButtonClick);
    public void PlayUIEquip(Transform playFrom, bool makeChild = false) => PlayUISFX(UIequip);
    public void PlayUIDialogueBleep(Transform playFrom, bool makeChild = false) => PlayDialogueBleep();
    public void PlayHitImpact(Transform playFrom, bool makeChild = false) => PlaySFX(hitImpact, playFrom, makeChild);
    public void PlayDodge(Transform playFrom, bool makeChild = false) => PlaySFX(dogde, playFrom, makeChild);
    public void PlayPerfectDodge(Transform playFrom, bool makeChild = false) => PlayTrackedPerfectDodgeSFX(playFrom, makeChild);
    public void PlayCounterConfirm(Transform playFrom, bool makeChild = false) => PlaySFX(counterConfirm, playFrom, makeChild);
    public void FadeOutPerfectDodge(float duration = 0.12f)
    {
        StopPerfectDodgeSFXFade();
        perfectDodgeSFXFadeCoroutine = StartCoroutine(FadeOutPerfectDodgeSFXRoutine(duration));
    }
    public void PlayhitGround(Transform playFrom, bool makeChild = false) => PlaySFX(hitGround, playFrom, makeChild);
    public void PlaySwim(Transform playFrom, bool makeChild = false) => PlaySFX(swim, playFrom, makeChild);
    public void PlayChestOpen(Transform playFrom, bool makeChild = false)
    {
        PlaySFX(chestOpen1, playFrom, makeChild);
        PlaySFX(chestOpen2, playFrom, makeChild);
    }
    public void PlayGrassyFootstep(Transform playFrom, bool makeChild = false) => PlaySFX(GetRandomSFX(grassyFootsteps), playFrom, makeChild);
    public void PlayHardFootstep(Transform playFrom, bool makeChild = false) => PlaySFX(GetRandomSFX(hardFootsteps), playFrom, makeChild);
}
