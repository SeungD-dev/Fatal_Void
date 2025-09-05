using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ������ �ɼ� ������ �����ϴ� ��Ʈ�ѷ�
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("Debug UI")]
    [SerializeField] private Button debugButton;
    [SerializeField] private GameObject xTierDebugPanel;
#endif

    private SoundManager soundManager;
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    private void Awake()
    {
        soundManager = SoundManager.Instance;
        InitializeVolumeSettings();
        SetupSliderListeners();
        SetupQuitButton();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        SetupDebugButton();
#endif
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void SetupDebugButton()
    {
        if (debugButton != null)
        {
            debugButton.onClick.AddListener(OnDebugButtonClicked);
        }

        // 디버그 패널은 처음에 비활성화
        if (xTierDebugPanel != null)
        {
            xTierDebugPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 디버그 버튼 클릭 시 X-Tier 무기 디버그 패널 토글
    /// </summary>
    public void OnDebugButtonClicked()
    {
        if (soundManager.currentSoundBank != null)
        {
            soundManager.PlaySound("Button_sfx", 0f, false);
        }

        if (xTierDebugPanel != null)
        {
            bool isActive = xTierDebugPanel.activeSelf;
            xTierDebugPanel.SetActive(!isActive);
        }
    }

    /// <summary>
    /// X-Tier 디버그 패널 닫기 (패널의 닫기 버튼에서 호출됨)
    /// </summary>
    public void CloseXTierDebugPanel()
    {
        if (soundManager.currentSoundBank != null)
        {
            soundManager.PlaySound("Button_sfx", 0f, false);
        }

        if (xTierDebugPanel != null)
        {
            xTierDebugPanel.SetActive(false);
        }
    }
#endif

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
    /// �ɼ� �г��� ����ϰ� ���� ���¸� ����
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
    /// ���� ���� ó��
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugButton != null) debugButton.onClick.RemoveAllListeners();
#endif
    }
}