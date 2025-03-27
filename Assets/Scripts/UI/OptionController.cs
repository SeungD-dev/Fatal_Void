using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임의 옵션 설정을 관리하는 컨트롤러
/// </summary>
public class OptionController : MonoBehaviour
{
    [Header("Audio Controls")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI bgmVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;

    [Header("UI References")]
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button quitButton;

    private SoundManager soundManager;
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    private void Awake()
    {
        soundManager = SoundManager.Instance;
        InitializeVolumeSettings();
        SetupSliderListeners();
        SetupQuitButton();
    }

    private void InitializeVolumeSettings()
    {
        float savedBGMVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        float savedSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);

        bgmSlider.value = savedBGMVolume;
        sfxSlider.value = savedSFXVolume;

        soundManager.SetBGMVolume(savedBGMVolume);
        soundManager.SetSFXVolume(savedSFXVolume);
    }

    private void SetupSliderListeners()
    {
        bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void SetupQuitButton()
    {
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
    }

    private void OnBGMVolumeChanged(float volume)
    {
        soundManager.SetBGMVolume(volume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, volume);
        PlayerPrefs.Save();

        if (soundManager.currentSoundBank != null)
        {
            soundManager.PlaySound("SFX_VolumeChange", 0f, false);
        }
    }

    private void OnSFXVolumeChanged(float volume)
    {
        soundManager.SetSFXVolume(volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        PlayerPrefs.Save();    
    }
    /// <summary>
    /// 옵션 패널을 토글하고 게임 상태를 관리
    /// </summary>
    public void CloseOptionPanel()
    {
        if (soundManager.currentSoundBank != null)
        {
            soundManager.PlaySound("Button_sfx", 0f, false);
        }

        optionPanel.SetActive(false);

        if (GameManager.Instance.currentGameState == GameState.Paused)
        {
            GameManager.Instance.SetGameState(GameState.Playing);
        }
    }

    /// <summary>
    /// 게임 종료 처리
    /// </summary>
    public void OnQuitButtonClicked()
    {
        if (soundManager.currentSoundBank != null)
        {
            soundManager.PlaySound("SFX_ButtonClick", 0f, false);
        }
  
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveAllListeners();
        if (quitButton != null) quitButton.onClick.RemoveAllListeners();
    }
}