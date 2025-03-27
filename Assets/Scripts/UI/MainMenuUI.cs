using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Image characterImage;
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private float idleFrameRate = 10f;
    [SerializeField] private float walkFrameRate = 10f;
    [SerializeField] private float walkSpeed = 300f;

    [Header("UI References")]
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button optionButton;

    private bool isWalking = false;
    private float frameTimer;
    private int currentFrame;
    private Coroutine idleCoroutine;

    private void Start()
    {
        

        if (GameManager.Instance != null && optionPanel != null)
        {
            GameManager.Instance.SetStartSceneReferences(optionPanel);
        }
        //추후 사운드 추가시 해제
        //if (SoundManager.Instance != null && !SoundManager.Instance.IsBGMPlaying("BGM_Title"))
        //{
        //    SoundManager.Instance.PlaySound("BGM_Title", 1f, true);
        //}

        if (optionButton != null)
        {
            optionButton.onClick.AddListener(OnOptionButtonClick);
        }

        idleCoroutine = StartCoroutine(PlayIdleAnimation());
    }

    private void OnOptionButtonClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleOptionPanel();
        }
    }

    private IEnumerator PlayIdleAnimation()
    {
        while (!isWalking)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / idleFrameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % idleSprites.Length;
                characterImage.sprite = idleSprites[currentFrame];
            }
            yield return null;
        }
    }

    public void OnStartButtonClick()
    {
        if (SoundManager.Instance.currentSoundBank == null)
        {
            SoundManager.Instance.LoadSoundBank("IntroSoundBank");
        }
        if (SoundManager.Instance.currentSoundBank != null)
        {
            SoundManager.Instance.PlaySound("Button_sfx", 1f, false);
        }
        if (!isWalking)
        {
            isWalking = true;
            if (idleCoroutine != null)
            {
                StopCoroutine(idleCoroutine); // Idle 애니메이션 중지
            }
            StartCoroutine(WalkAndStartGame());
        }
    }

    private IEnumerator WalkAndStartGame()
    {
        RectTransform rectTransform = characterImage.rectTransform;
        currentFrame = 0;
        frameTimer = 0f;
        float targetX = 460f;
        // 현재 위치가 목표 위치에 도달할 때까지만 이동
        while (rectTransform.anchoredPosition.x < targetX)
        {
            // 캐릭터 이동
            Vector2 position = rectTransform.anchoredPosition;
            position.x += walkSpeed * Time.deltaTime;
            // 목표 위치를 넘어가지 않도록 제한
            position.x = Mathf.Min(position.x, targetX);
            rectTransform.anchoredPosition = position;
            // 걷기 애니메이션 프레임 업데이트
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / walkFrameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % walkSprites.Length;
                characterImage.sprite = walkSprites[currentFrame];
            }
            yield return null;
        }
        // 목표 위치에 도달하면 게임 시작
        GameManager.Instance.StartGame();
    }

    private void OnDestroy()
    {
        // Clean up event listeners
        if (optionButton != null)
        {
            optionButton.onClick.RemoveListener(OnOptionButtonClick);
        }
    }
}