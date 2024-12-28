using UnityEngine;
using UnityEngine.UI;

public class CharacterUIAnimator : MonoBehaviour
{
    public Sprite[] idleSprites; // Inspector에서 Idle 애니메이션 스프라이트들을 순서대로 넣어주세요
    public float frameRate = 10f; // 애니메이션 속도 조절

    private Image characterImage;
    private int currentFrame;
    private float frameTimer;

    void Start()
    {
        characterImage = GetComponent<Image>();
        if (idleSprites.Length == 0)
        {
            Debug.LogWarning("There's no animation sprites!");
            return;
        }
    }

    void Update()
    {
        if (idleSprites.Length == 0) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % idleSprites.Length;
            characterImage.sprite = idleSprites[currentFrame];
        }
    }
}
