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
            Debug.Log("OptionController: Debug button listener added");
        }
        else
        {
            Debug.LogWarning("OptionController: Debug button is null!");
        }

        // 디버그 패널 확인 (Hierarchy에서 이미 비활성화 상태)
        if (xTierDebugPanel != null)
        {
            Debug.Log("OptionController: X-Tier debug panel found");
        }
        else
        {
            Debug.LogWarning("OptionController: X-Tier debug panel is null!");
        }
    }

    /// <summary>
    /// 디버그 버튼 클릭 시 X-Tier 무기 디버그 패널 토글
    /// </summary>
    public void OnDebugButtonClicked()
    {
        Debug.Log("OptionController: Debug button clicked!");
        
        if (soundManager.currentSoundBank != null)
        {
            soundManager.PlaySound("Button_sfx", 0f, false);
        }

        if (xTierDebugPanel != null)
        {
            // 디버그 버튼을 누르면 항상 옵션 패널 -> 디버그 패널로 전환
            if (optionPanel != null)
            {
                optionPanel.SetActive(false);
                Debug.Log("OptionController: Option panel disabled");
            }
            
            xTierDebugPanel.SetActive(true);
            Debug.Log("OptionController: X-Tier debug panel activated");
        }
        else
        {
            Debug.LogError("OptionController: X-Tier debug panel is null when trying to toggle!");
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
            Debug.Log("OptionController: X-Tier debug panel closed");
        }

        // 디버그 패널을 닫을 때 옵션 패널 다시 활성화
        if (optionPanel != null)
        {
            optionPanel.SetActive(true);
            Debug.Log("OptionController: Option panel reactivated after debug close");
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