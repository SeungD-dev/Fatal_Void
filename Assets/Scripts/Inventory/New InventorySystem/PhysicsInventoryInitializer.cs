using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 물리 인벤토리 시스템의 초기화를 담당하는 매니저 클래스
/// </summary>
public class PhysicsInventoryInitializer : MonoBehaviour
{
    private static PhysicsInventoryInitializer instance;
    public static PhysicsInventoryInitializer Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("PhysicsInventoryInitializer");
                instance = go.AddComponent<PhysicsInventoryInitializer>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("Physics Settings")]
    [SerializeField] private float defaultGravityScale = 980f;
    [SerializeField] private float defaultDragDamping = 0.92f;
    [SerializeField] private float defaultBounceMultiplier = 0.4f;
    [SerializeField] private float defaultGroundFriction = 0.8f;
    [SerializeField] private float defaultMinimumVelocity = 10f;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int maxPoolSize = 50;
    [SerializeField] private int poolGrowSize = 5;

    // 물리 시스템 초기화 상태
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    // 씬 로드 이벤트 리스너
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // CombatScene이 로드되면 물리 시스템 초기화
        if (GameManager.Instance != null &&
            GameManager.Instance.currentGameState == GameState.Playing &&
            !isInitialized)
        {
            StartCoroutine(InitializePhysicsSystemDelayed());
        }
    }

    /// <summary>
    /// 물리 인벤토리 시스템 초기화 코루틴
    /// </summary>
    public IEnumerator InitializePhysicsSystemDelayed()
    {
        // 씬이 완전히 로드될 때까지 대기
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Initializing Physics Inventory System in scene...");

        // 인벤토리 컨트롤러 찾기
        InventoryController inventoryController = FindAnyObjectByType<InventoryController>();
        if (inventoryController == null)
        {
            Debug.LogWarning("InventoryController not found in scene. Physics Inventory System initialization skipped.");
            yield break;
        }

        // 물리 인벤토리 매니저 추가 또는 가져오기
        PhysicsInventoryManager physicsManager = inventoryController.GetComponent<PhysicsInventoryManager>();
        if (physicsManager == null)
        {
            physicsManager = inventoryController.gameObject.AddComponent<PhysicsInventoryManager>();
            Debug.Log("PhysicsInventoryManager added to InventoryController");
        }

        // 오브젝트 풀 초기화
        InitializePhysicsItemPool(physicsManager);

        // 기본 설정 적용
        yield return ApplyDefaultSettings(physicsManager);

        // 초기화 완료
        isInitialized = true;
        Debug.Log("Physics Inventory System initialized in scene");
    }

    /// <summary>
    /// 물리 인벤토리 아이템을 위한 오브젝트 풀 초기화
    /// </summary>
    private void InitializePhysicsItemPool(PhysicsInventoryManager physicsManager)
    {
        if (ObjectPool.Instance == null)
        {
            Debug.LogWarning("ObjectPool instance not found. Physics item pool initialization skipped.");
            return;
        }

        // 무기 프리팹 가져오기
        GameObject weaponPrefab = null;

        // 먼저 PhysicsInventoryManager에서 직접 가져오기 시도
        if (physicsManager != null)
        {
            // 리플렉션으로 weaponPrefab 필드 접근 (필요한 경우)
            var weaponPrefabField = typeof(PhysicsInventoryManager).GetField("weaponPrefab",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            if (weaponPrefabField != null)
            {
                weaponPrefab = weaponPrefabField.GetValue(physicsManager) as GameObject;
            }
        }

        // 리소스에서 로드 시도
        if (weaponPrefab == null)
        {
            weaponPrefab = Resources.Load<GameObject>("Prefabs/UI/WeaponItem");
        }

        // 백업 계획: 인벤토리 컨트롤러에서 찾기
        if (weaponPrefab == null)
        {
            var inventoryController = FindAnyObjectByType<InventoryController>();
            if (inventoryController != null)
            {
                var weaponPrefabField = typeof(InventoryController).GetField("weaponPrefab",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);

                if (weaponPrefabField != null)
                {
                    weaponPrefab = weaponPrefabField.GetValue(inventoryController) as GameObject;
                }
            }
        }

        // 풀 생성
        if (weaponPrefab != null)
        {
            string poolTag = "PhysicsInventoryItem";

            if (!ObjectPool.Instance.DoesPoolExist(poolTag))
            {
                // 새 풀 생성
                ObjectPool.Pool physicsItemPool = new ObjectPool.Pool
                {
                    tag = poolTag,
                    prefab = weaponPrefab,
                    initialSize = initialPoolSize,
                    maxSize = maxPoolSize,
                    growSize = poolGrowSize
                };

                // 풀 생성 메서드는 공개 API인 CreatePool 사용
                ObjectPool.Instance.CreatePool(poolTag, weaponPrefab, initialPoolSize);
                Debug.Log($"Physics inventory item pool created with {initialPoolSize} items");
            }
            else
            {
                // 기존 풀의 크기 확장
                int currentCount = ObjectPool.Instance.GetAvailableCount(poolTag);
                if (currentCount < initialPoolSize)
                {
                    ObjectPool.Instance.ExpandPool(poolTag, initialPoolSize - currentCount);
                    Debug.Log($"Physics inventory item pool expanded to {initialPoolSize} items");
                }
            }
        }
        else
        {
            Debug.LogWarning("Failed to find weapon prefab for physics item pool initialization");
        }
    }

    /// <summary>
    /// 기본 물리 설정 적용
    /// </summary>
    private IEnumerator ApplyDefaultSettings(PhysicsInventoryManager physicsManager)
    {
        // 리플렉션을 통한 기본 설정 적용 (직접 필드 노출이 불가능한 경우)
        // 실제로는 SerializeField로 노출된 공용 설정을 사용하는 것이 더 좋습니다.
        try
        {
            // 예: 리플렉션을 사용한 설정 적용
            var gravityField = typeof(PhysicsInventoryManager).GetField("gravityScale",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (gravityField != null)
            {
                gravityField.SetValue(physicsManager, defaultGravityScale);
            }

            // 기타 설정들도 비슷하게 적용
            // ...
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error applying default physics settings: {e.Message}");
        }

        yield return null;
    }

    /// <summary>
    /// GameManager에서 호출될 수 있는 공용 초기화 메서드
    /// </summary>
    public static IEnumerator InitializeInLoadingScreen()
    {
        // 인스턴스 생성 보장
        var initializer = Instance;

        Debug.Log("Pre-initializing Physics Inventory System during loading...");

        // 풀 초기화 설정 미리 준비
        var poolInstance = ObjectPool.Instance;
        if (poolInstance != null)
        {
            string poolTag = "PhysicsInventoryItem";
            GameObject weaponPrefab = Resources.Load<GameObject>("Prefabs/UI/WeaponItem");

            if (weaponPrefab != null && !poolInstance.DoesPoolExist(poolTag))
            {
                // 풀 생성
                poolInstance.CreatePool(poolTag, weaponPrefab, initializer.initialPoolSize);
                Debug.Log($"Physics item pool pre-initialized with {initializer.initialPoolSize} items");
            }
        }

        // 애셋 미리 로드
        yield return initializer.PreloadPhysicsAssets();

        // 물리 처리 관련 리소스 미리 준비
        // ...

        Debug.Log("Physics Inventory System pre-initialization completed");
    }

    /// <summary>
    /// 물리 인벤토리 매니저를 리소스로부터 로드하고 설정
    /// </summary>
    public IEnumerator PreloadPhysicsAssets()
    {
        // 필요한 프리팹 미리 로드
        GameObject physicsItemPrefab = Resources.Load<GameObject>("Prefabs/Weapons/Item");
        if (physicsItemPrefab != null)
        {
            // 프리팹 로드 성공
            Debug.Log("Physics item prefab preloaded");
        }
        else
        {
            // 대체 프리팹 로드 시도
            physicsItemPrefab = Resources.Load<GameObject>("Prefabs/Weapons/Item");
            if (physicsItemPrefab != null)
            {
                Debug.Log("Using default weapon item as physics item prefab");
            }
            else
            {
                Debug.LogWarning("No suitable physics item prefab found");
            }
        }

        yield return null;

        // 기타 필요한 애셋 로드
        // ...

        yield return null;
    }

}