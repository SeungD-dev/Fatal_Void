using UnityEngine;

/// <summary>
/// PlayerStats 클래스를 확장하여 X-티어 업그레이드 시스템에 필요한 레벨 차감 기능을 추가합니다.
/// </summary>
public static class PlayerStatsExtension
{
    /// <summary>
    /// 플레이어 레벨을 지정된 양만큼 차감합니다.
    /// </summary>
    /// <param name="playerStats">플레이어 스탯 인스턴스</param>
    /// <param name="levels">차감할 레벨 수</param>
    /// <returns>차감 성공 여부</returns>
    public static bool SubtractLevels(this PlayerStats playerStats, int levels)
    {
        if (playerStats == null || levels <= 0)
        {
            return false;
        }

        // 현재 레벨이 차감할 레벨보다 큰지 확인
        if (playerStats.Level <= levels)
        {
            Debug.LogWarning("레벨이 부족하여 차감할 수 없습니다.");
            return false;
        }

        // 새 레벨 계산 (최소 1 유지)
        int newLevel = Mathf.Max(1, playerStats.Level - levels);

        // 비공개 필드 접근을 위한 리플렉션 사용
        var levelField = typeof(PlayerStats).GetField("level",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);

        if (levelField != null)
        {
            // 필드 값 변경
            levelField.SetValue(playerStats, newLevel);

            // 스탯 업데이트 메서드 호출
            var updateStatsMethod = typeof(PlayerStats).GetMethod("UpdateStats",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            if (updateStatsMethod != null)
            {
                updateStatsMethod.Invoke(playerStats, null);
            }

            // 레벨 변경 이벤트 발생 (PlayerStats에 정의된 이벤트)
            playerStats.OnLevelUp?.Invoke(newLevel);

            Debug.Log($"플레이어 레벨이 {levels}만큼 감소했습니다. 새 레벨: {newLevel}");
            return true;
        }
        else
        {
            Debug.LogError("PlayerStats의 level 필드에 접근할 수 없습니다.");
            return false;
        }
    }
}