using UnityEngine;

public class DaggerMechanism : WeaponMechanism
{
    private Vector2 lastMoveDirection = Vector2.right;
    private Rigidbody2D playerRb;
    private bool isFirstDaggerFired = false;
    private float secondDaggerDelay = 0.1f; // 두 번째 단검 발사 딜레이
    private float firstDaggerTime = 0f;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb == null)
        {
            Debug.LogError("Rigidbody2D not found on player!");
        }
    }

    public override void UpdateMechanism()
    {
        float currentTime = Time.time;

        // 첫 번째 단검 발사 체크
        if (!isFirstDaggerFired && currentTime >= lastAttackTime + weaponData.attackDelay)
        {
            Attack();
            lastAttackTime = currentTime;
            firstDaggerTime = currentTime;
            isFirstDaggerFired = true;
        }
        // 두 번째 단검 발사 체크
        else if (isFirstDaggerFired && currentTime >= firstDaggerTime + secondDaggerDelay)
        {
            Attack();
            isFirstDaggerFired = false; // 다음 사이클을 위해 리셋
        }
    }

    protected override void Attack()
    {
        // 현재 이동 방향 가져오기
        Vector2 currentVelocity = playerRb.linearVelocity;

        // 이동 중이면 현재 방향을 저장
        if (currentVelocity.sqrMagnitude > 0.1f)
        {
            lastMoveDirection = currentVelocity.normalized;
        }

        FireDagger(lastMoveDirection);
    }

    private void FireDagger(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject daggerObj = Object.Instantiate(weaponData.projectilePrefab,
            playerTransform.position,
            Quaternion.Euler(0, 0, angle));

        DaggerProjectile dagger = daggerObj.GetComponent<DaggerProjectile>();
        dagger.Initialize(weaponData.weaponDamage, direction, weaponData.projectileSpeed);

        Debug.Log($"Firing dagger in direction: {direction}, Angle: {angle}");
    }
}