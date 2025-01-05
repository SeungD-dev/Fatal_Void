using UnityEngine;

public class BulletDestroyVFX : MonoBehaviour, IPooledObject
{
    private Animator animator;
    private string poolTag;
    private Vector3 originalScale;
    private float animationLength;
    private float currentTime;
    private bool isPlaying;
    private static readonly int DestroyHash = Animator.StringToHash("Bullet_Destroy");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;

        // 애니메이션 길이를 정확히 가져오기
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo != null && clipInfo.Length > 0)
        {
            animationLength = clipInfo[0].clip.length;
            Debug.Log($"Animation Length: {animationLength}");
        }
    }

    public void OnObjectSpawn()
    {
        if (animator != null)
        {
            currentTime = 0f;
            isPlaying = true;
            animator.Rebind();
            animator.Play(DestroyHash, 0, 0f);
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        currentTime += Time.deltaTime;
        if (currentTime >= animationLength)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (!string.IsNullOrEmpty(poolTag))
        {
            isPlaying = false;
            currentTime = 0f;
            transform.localScale = originalScale;
            //Debug.Log($"Returning to pool with tag: {poolTag}");
            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            Debug.LogError("Pool tag is not set!");
        }
    }

    public void SetPoolTag(string tag)
    {
        poolTag = tag;
        Debug.Log($"Pool tag set to: {tag}");
    }

    public void SetEffectScale(Vector3 bulletScale)
    {
        transform.localScale = originalScale * Mathf.Max(bulletScale.x, bulletScale.y);
    }

    protected void OnDisable()
    {
        transform.localScale = originalScale;
        isPlaying = false;
        currentTime = 0f;
    }
}