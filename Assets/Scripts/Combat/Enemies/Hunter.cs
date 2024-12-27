using UnityEngine;

public class Hunter : EnemyAI
{
    [Header("Aura Settings")]
    [SerializeField] private Transform auraTransform;  // 오라 오브젝트의 Transform
    [SerializeField] private float rotationSpeed = 100f;  // 회전 속도 (도/초)

    protected override void InitializeStates()
    {
        base.InitializeStates();
    }

    protected override void Update()
    {
        base.Update();

        // 오라 회전
        if (auraTransform != null && IsGamePlaying())
        {
            auraTransform.Rotate(Vector3.forward * (rotationSpeed * Time.deltaTime));
        }
    }
}