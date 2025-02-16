using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    public SoundType type;
    public SoundPriority priority;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0f, 1f)]
    public float pitchVariance = 0.1f;
}

public enum SoundType
{
    BGM,
    SFX
}

public enum SoundPriority
{
    Low,
    Medium,
    High
}


public class SoundManager : Singleton<SoundManager>
{
    public AudioSource BGMSource;
    public AudioSource BGMSource2;
    public int SFXSourcesCount = 10;
    private List<AudioSource> SFXSources;

    public SoundBankSO currentSoundBank;

    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float bgmVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private List<AudioSource> activeSFXSources;
    private Sound currentBGM;
    private string currentBGMName;

    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    protected override void Awake()
    {
        base.Awake();
        InitializeAudioSources();
        LoadVolumeSettings();
    }

    private void InitializeAudioSources()
    {
        // BGM 소스 초기화
        BGMSource = CreateAudioSource("BGM Source");
        BGMSource2 = CreateAudioSource("BGM Source 2");

        // SFX 소스 초기화
        SFXSources = new List<AudioSource>();
        for (int i = 0; i < SFXSourcesCount; i++)
        {
            SFXSources.Add(CreateAudioSource($"SFX Source {i}"));
        }

        activeSFXSources = new List<AudioSource>();
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
    }

    private AudioSource CreateAudioSource(string name)
    {
        GameObject audioSourceObj = new GameObject(name);
        audioSourceObj.transform.SetParent(this.transform);
        AudioSource source = audioSourceObj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }

    public void LoadSoundBank(string soundBankName)
    {
        SoundBankSO newSoundBank = Resources.Load<SoundBankSO>("SoundBank/" + soundBankName);
        if (newSoundBank == null)
        {
            Debug.LogError($"Failed to load SoundBank: {soundBankName}");
            return;
        }

        // 현재 재생 중인 BGM 상태 저장
        bool wasBGMPlaying = false;
        if (currentBGM != null && (BGMSource.isPlaying || BGMSource2.isPlaying))
        {
            wasBGMPlaying = true;
        }

        currentSoundBank = newSoundBank;

        if (activeSFXSources == null)
        {
            activeSFXSources = new List<AudioSource>();
        }

        // 볼륨 설정 유지
        if (wasBGMPlaying)
        {
            UpdateBGMVolume();
        }
        UpdateSFXVolume();
    }

    public void PlaySound(string name, float fadeTime = 1f, bool loop = false)
    {
        Sound sound = currentSoundBank.sounds.Find(s => s.name == name);
        if (sound == null)
        {
            Debug.LogWarning($"Sound: {name} not found!");
            return;
        }

        if (sound.type == SoundType.BGM)
        {
            StartCoroutine(CrossfadeBGM(sound, fadeTime, loop));
        }
        else
        {
            PlaySFX(sound, loop);
        }
    }

    private IEnumerator CrossfadeBGM(Sound newBGM, float fadeTime, bool loop)
    {
        if (fadeTime <= 0f) fadeTime = 0.01f;

        AudioSource fadeOutSource = BGMSource.isPlaying ? BGMSource : BGMSource2;
        AudioSource fadeInSource = BGMSource.isPlaying ? BGMSource2 : BGMSource;

        fadeInSource.clip = newBGM.clip;
        fadeInSource.volume = 0;
        fadeInSource.loop = loop;
        fadeInSource.Play();

        float t = 0;
        float startVolume = currentBGM?.volume * bgmVolume * masterVolume ?? 0f;
        float targetVolume = newBGM.volume * bgmVolume * masterVolume;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float progress = t / fadeTime;

            if (fadeOutSource.isPlaying)
            {
                fadeOutSource.volume = Mathf.Lerp(startVolume, 0, progress);
            }
            fadeInSource.volume = Mathf.Lerp(0, targetVolume, progress);

            yield return null;
        }

        fadeOutSource.Stop();
        currentBGM = newBGM;
        currentBGMName = newBGM.name;
    }

    private void PlaySFX(Sound sound, bool loop)
    {
        AudioSource source = GetFreeSFXSource();
        if (source != null)
        {
            StartCoroutine(PlaySFXWithFade(source, sound, loop));
        }
    }

    private IEnumerator PlaySFXWithFade(AudioSource source, Sound sound, bool loop)
    {
        source.clip = sound.clip;
        source.volume = 0f;
        source.pitch = 1f + Random.Range(-sound.pitchVariance, sound.pitchVariance);
        source.loop = loop;
        source.Play();

        activeSFXSources.Add(source);
        AdjustSFXVolumes();

        float fadeTime = 0.1f;
        float targetVolume = sound.volume * sfxVolume * masterVolume;

        // Fade In
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(0, targetVolume, t / fadeTime);
            yield return null;
        }

        if (!loop)
        {
            yield return new WaitForSecondsRealtime(source.clip.length - fadeTime);

            // Fade Out
            t = 0;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(targetVolume, 0, t / fadeTime);
                yield return null;
            }

            source.Stop();
            activeSFXSources.Remove(source);
        }
    }

    private AudioSource GetFreeSFXSource()
    {
        return SFXSources.FirstOrDefault(s => !s.isPlaying) ??
               (activeSFXSources.Count < SFXSourcesCount ? SFXSources.First(s => !activeSFXSources.Contains(s)) : null);
    }

    private void AdjustSFXVolumes()
    {
        if (activeSFXSources.Count > SFXSourcesCount)
        {
            var lowPrioritySounds = activeSFXSources
                .Where(s => currentSoundBank.sounds.Find(sound => sound.clip == s.clip).priority == SoundPriority.Low)
                .ToList();

            foreach (var source in lowPrioritySounds)
            {
                source.volume *= 0.5f;
            }
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
        PlayerPrefs.Save();

        UpdateBGMVolume();
        UpdateSFXVolume();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        PlayerPrefs.Save();

        UpdateBGMVolume();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.Save();

        UpdateSFXVolume();
    }

    private void UpdateBGMVolume()
    {
        if (currentBGM != null)
        {
            float targetVolume = currentBGM.volume * bgmVolume * masterVolume;
            if (BGMSource.isPlaying) BGMSource.volume = targetVolume;
            if (BGMSource2.isPlaying) BGMSource2.volume = targetVolume;
        }
    }

    private void UpdateSFXVolume()
    {
        if(activeSFXSources == null || currentSoundBank == null) return;

        foreach (var source in activeSFXSources.Where(s => s != null && s.isPlaying))
        {
            Sound sound = currentSoundBank.sounds.Find(s => s.clip == source.clip);
            if (sound != null)
            {
                source.volume = sound.volume * sfxVolume * masterVolume;
            }
        }
    }

    public string GetCurrentBGMName() => currentBGMName;

    public bool IsBGMPlaying(string bgmName)
    {
        return currentBGMName == bgmName && (BGMSource.isPlaying || BGMSource2.isPlaying);
    }

    public void StopAllSounds()
    {
        StopAllCoroutines();
        BGMSource.Stop();
        BGMSource2.Stop();

        foreach (var source in SFXSources)
        {
            source.Stop();
        }

        activeSFXSources.Clear();
        currentBGM = null;
        currentBGMName = null;
    }

    private void OnApplicationQuit()
    {
        // 볼륨 설정 저장
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.Save();
    }
}