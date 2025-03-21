using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

/// <summary>
/// 물리 기반 아이템을 관리하는 매니저 클래스 - 오브젝트 풀 활용 버전
/// </summary>
public class PhysicsInventoryManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private ItemGrid mainGrid;
    [SerializeField] private Transform itemSpawnPoint;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private GameObject weaponPrefab;

    [Header("Physics Settings")]
    [SerializeField] private float dragThreshold = 0.3f;
    [SerializeField] private float holdDelay = 0.3f;
    [SerializeField] private float physicsSpawnOffset = 50f;
    [SerializeField] private float physicsRandomVariance = 30f;

    [Header("Pool Settings")]
    [SerializeField] private bool useObjectPool = true;
    [SerializeField] private string poolTag = "PhysicsInventoryItem";
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int ensurePoolSize = 10; // 최소한 유지할 풀 크기
    #endregion

    #region Private Fields
    private List<PhysicsInventoryItem> physicsItems = new List<PhysicsInventoryItem>();
    private PhysicsInventoryItem selectedPhysicsItem;
    private TouchActions touchActions;
    private InputAction touchPosition;
    private InputAction touchPress;
    private bool isHolding = false;
    private bool isDragging = false;
    private Vector2 touchStartPosition;
    private Coroutine holdCoroutine;
    private Camera mainCamera;
    private bool isInitialized = false;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        StartCoroutine(DelayedInitialization());
    }

    private void OnEnable()
    {
        if (touchActions != null)
        {
            touchActions.Enable();
        }
    }

    private void OnDisable()
    {
        if (touchActions != null)
        {
            touchActions.Disable();
        }

        // 진행 중인 코루틴 정지
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    private void Update()
    {
        // 선택된 아이템이 있고 드래그 중이면 위치 업데이트
        if (selectedPhysicsItem != null && isDragging)
        {
            Vector2 currentTouchPos = touchPosition.ReadValue<Vector2>();
            selectedPhysicsItem.UpdateDragPosition(currentTouchPos);
        }

        // 성능 최적화: 활성 물리 아이템 업데이트
        UpdateActivePhysicsItems();
    }

    private void OnDestroy()
    {
        if (touchActions != null)
        {
            touchActions.Dispose();
        }

        // 풀에 아이템 반환
        ReturnAllItemsToPool();
    }
    #endregion

    #region Initialization
    private IEnumerator DelayedInitialization()
    {
        // 첫 프레임은 건너뛰어 다른 초기화가 완료되도록 함
        yield return null;

        // 오브젝트 풀 사용시 미리 초기화
        if (useObjectPool && ObjectPool.Instance != null)
        {
            InitializePhysicsItemPool();
        }

        SetupInputSystem();
        isInitialized = true;
    }

    private void InitializeComponents()
    {
        if (inventoryController == null)
        {
            inventoryController = GetComponent<InventoryController>();
        }

        if (mainGrid == null && inventoryController != null)
        {
            mainGrid = inventoryController.GetComponentInChildren<ItemGrid>();
        }

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        if (itemSpawnPoint == null)
        {
            // InventoryController에서 spawnPoint 가져오기
            var field = typeof(InventoryController).GetField("itemSpawnPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                itemSpawnPoint = field.GetValue(inventoryController) as Transform;
            }

            if (itemSpawnPoint == null)
            {
                Debug.LogWarning("ItemSpawnPoint not found! Creating a default one.");
                GameObject spawnObj = new GameObject("DefaultItemSpawnPoint");
                spawnObj.transform.SetParent(transform);
                spawnObj.transform.localPosition = Vector3.zero;
                itemSpawnPoint = spawnObj.transform;
            }
        }

        if (weaponPrefab == null && inventoryController != null)
        {
            // InventoryController의 weaponPrefab 필드 가져오기
            var field = typeof(InventoryController).GetField("weaponPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                weaponPrefab = field.GetValue(inventoryController) as GameObject;
            }
        }

        mainCamera = Camera.main;
    }

    /// <summary>
    /// 물리 인벤토리 아이템을 위한 오브젝트 풀 초기화
    /// </summary>
    private void InitializePhysicsItemPool()
    {
        if (ObjectPool.Instance == null || weaponPrefab == null) return;

        // 풀이 이미 존재하는지 확인
        if (!ObjectPool.Instance.DoesPoolExist(poolTag))
        {
            // 새 풀 생성
            ObjectPool.Instance.CreatePool(poolTag, weaponPrefab, initialPoolSize);
            Debug.Log($"Physics inventory item pool created with {initialPoolSize} items");
        }
        else
        {
            // 풀 크기가 충분한지 확인
            int availableCount = ObjectPool.Instance.GetAvailableCount(poolTag);
            if (availableCount < ensurePoolSize)
            {
                ObjectPool.Instance.ExpandPool(poolTag, ensurePoolSize - availableCount);
                Debug.Log($"Physics inventory item pool expanded. Added {ensurePoolSize - availableCount} items");
            }
        }
    }

    private void SetupInputSystem()
    {
        touchActions = new TouchActions();
        touchPosition = touchActions.Touch.Position;
        touchPress = touchActions.Touch.Press;

        touchPress.started += OnTouchStarted;
        touchPress.canceled += OnTouchEnded;

        touchActions.Enable();
    }
    #endregion

    #region Item Creation
    /// <summary>
    /// 물리 기반 인벤토리 아이템 생성 - 오브젝트 풀 활용
    /// </summary>
    public InventoryItem CreatePhysicsItem(WeaponData weaponData)
    {
        if (weaponData == null || weaponPrefab == null) return null;

        // 랜덤한 스폰 위치 계산
        Vector3 spawnPosition = itemSpawnPoint.position;
        spawnPosition += new Vector3(
            Random.Range(-physicsRandomVariance, physicsRandomVariance),
            physicsSpawnOffset + Random.Range(0, physicsRandomVariance),
            0
        );

        GameObject itemObj;

        // 오브젝트 풀에서 가져오기
        if (useObjectPool && ObjectPool.Instance != null && ObjectPool.Instance.DoesPoolExist(poolTag))
        {
            // 가용 오브젝트가 충분한지 확인
            int availableCount = ObjectPool.Instance.GetAvailableCount(poolTag);
            if (availableCount < 1)
            {
                // 풀 확장
                ObjectPool.Instance.ExpandPool(poolTag, ensurePoolSize);
                Debug.Log($"Physics item pool expanded by {ensurePoolSize} items");
            }

            // 풀에서 오브젝트 가져오기
            itemObj = ObjectPool.Instance.SpawnFromPool(poolTag, spawnPosition, Quaternion.identity);
        }
        else
        {
            // 오브젝트 풀을 사용하지 않거나 사용할 수 없는 경우 직접 생성
            itemObj = Instantiate(weaponPrefab, spawnPosition, Quaternion.identity, parentCanvas.transform);
        }

        if (itemObj == null)
        {
            Debug.LogError("Failed to create physics inventory item");
            return null;
        }

        // 인벤토리 아이템 컴포넌트 가져오기
        InventoryItem inventoryItem = itemObj.GetComponent<InventoryItem>();
        if (inventoryItem == null)
        {
            Debug.LogError("InventoryItem component not found on prefab");
            if (useObjectPool && ObjectPool.Instance != null)
            {
                ObjectPool.Instance.ReturnToPool(poolTag, itemObj);
            }
            else
            {
                Destroy(itemObj);
            }
            return null;
        }

        // 무기 데이터 초기화
        inventoryItem.Initialize(weaponData);
        inventoryItem.SetGridPosition(new Vector2Int(-1, -1)); // 그리드 외부 표시

        // 물리 컴포넌트 추가 또는 가져오기
        PhysicsInventoryItem physicsItem = itemObj.GetComponent<PhysicsInventoryItem>();
        if (physicsItem == null)
        {
            physicsItem = itemObj.AddComponent<PhysicsInventoryItem>();
        }

        // 위치 설정 및 물리 활성화
        physicsItem.SetSpawnPosition(spawnPosition);
        physicsItem.ActivatePhysics();

        // 관리 목록에 추가
        physicsItems.Add(physicsItem);

        return inventoryItem;
    }

    /// <summary>
    /// 인벤토리 그리드에 빈 공간이 없을 때 물리 아이템으로 생성
    /// </summary>
    public void HandleFullInventory(WeaponData weaponData)
    {
        CreatePhysicsItem(weaponData);

        // 효과음 재생 (있는 경우)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("ItemDrop_sfx", 0.8f, false);
        }
    }
    #endregion

    #region Touch Handling
    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        // 터치 위치 가져오기
        Vector2 touchPos = touchPosition.ReadValue<Vector2>();
        touchStartPosition = touchPos;

        // 그리드 내 아이템 먼저 체크
        Vector2Int gridPosition = mainGrid?.GetGridPosition(touchPos) ?? new Vector2Int(-1, -1);
        if (mainGrid != null && mainGrid.IsValidPosition(gridPosition))
        {
            // 그리드 내 아이템 터치 처리는 InventoryController가 담당
            return;
        }

        // 물리 아이템 터치 체크
        PhysicsInventoryItem touchedItem = GetPhysicsItemAtPosition(touchPos);
        if (touchedItem != null)
        {
            // 물리 아이템 선택
            selectedPhysicsItem = touchedItem;

            // 홀드 체크 시작
            if (holdCoroutine != null)
            {
                StopCoroutine(holdCoroutine);
            }
            holdCoroutine = StartCoroutine(CheckForHold(touchedItem, touchPos));
        }
    }

    private IEnumerator CheckForHold(PhysicsInventoryItem item, Vector2 startPosition)
    {
        float elapsedTime = 0f;

        while (touchPress.IsPressed())
        {
            elapsedTime += Time.deltaTime;
            Vector2 currentPos = touchPosition.ReadValue<Vector2>();
            float distance = Vector2.Distance(startPosition, currentPos);

            // 홀드 시간이 지났거나, 일정 거리 이상 움직였을 경우
            if (elapsedTime >= holdDelay || distance > dragThreshold)
            {
                StartDragging(item, currentPos);
                break;
            }

            yield return null;
        }

        holdCoroutine = null;
    }

    private void StartDragging(PhysicsInventoryItem item, Vector2 position)
    {
        if (item == null) return;

        isHolding = true;
        isDragging = true;
        selectedPhysicsItem = item;

        // 물리 비활성화하고 드래그 시작
        item.StartDrag(position);

        // 효과음 재생 (SoundManager가 있는 경우)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("ItemLift_sfx", 1f, false);
        }
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (selectedPhysicsItem != null && isDragging)
        {
            Vector2 finalPosition = touchPosition.ReadValue<Vector2>();

            // 드래그 종료 처리
            selectedPhysicsItem.EndDrag(mainGrid, finalPosition);

            // 상태 초기화
            ResetDragState();
        }

        // 홀드 코루틴 중지
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    private void ResetDragState()
    {
        isHolding = false;
        isDragging = false;
        selectedPhysicsItem = null;
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// 특정 위치에 있는 물리 아이템 찾기
    /// </summary>
    private PhysicsInventoryItem GetPhysicsItemAtPosition(Vector2 screenPosition)
    {
        // 레이캐스트 수행
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 결과 처리
        foreach (RaycastResult result in results)
        {
            PhysicsInventoryItem physicsItem = result.gameObject.GetComponent<PhysicsInventoryItem>();
            if (physicsItem != null)
            {
                return physicsItem;
            }

            // 부모 객체에서도 확인
            Transform parentTransform = result.gameObject.transform.parent;
            if (parentTransform != null)
            {
                physicsItem = parentTransform.GetComponent<PhysicsInventoryItem>();
                if (physicsItem != null)
                {
                    return physicsItem;
                }
            }
        }

        // 아이템을 찾지 못함
        return null;
    }

    /// <summary>
    /// 그리드 밖으로 드래그된 아이템을 물리 아이템으로 변환
    /// </summary>
    public void ConvertToPhysicsItem(InventoryItem item, Vector2 position)
    {
        if (item == null) return;

        GameObject itemObj = item.gameObject;

        // 이미 PhysicsInventoryItem 컴포넌트가 있는지 확인
        PhysicsInventoryItem physicsItem = itemObj.GetComponent<PhysicsInventoryItem>();
        if (physicsItem == null)
        {
            // 없으면 추가
            physicsItem = itemObj.AddComponent<PhysicsInventoryItem>();
        }

        // 캔버스로 부모 변경
        itemObj.transform.SetParent(parentCanvas.transform);

        // 물리 활성화
        Vector2 dragVelocity = (position - touchStartPosition) * 2f;
        physicsItem.ActivatePhysics(dragVelocity);

        // 관리 목록에 추가
        if (!physicsItems.Contains(physicsItem))
        {
            physicsItems.Add(physicsItem);
        }
    }

    /// <summary>
    /// 모든 드랍된 아이템 수집 (자석 효과 등에 사용)
    /// </summary>
    public void CollectAllItems(Transform target)
    {
        foreach (var item in physicsItems.ToList())
        {
            if (item != null && !item.IsBeingDragged)
            {
                // 대상 방향으로 이동하는 물리 효과
                Vector2 direction = (target.position - item.transform.position).normalized;
                item.ActivatePhysics(direction * 500f);
            }
        }
    }

    /// <summary>
    /// 인벤토리 그리드에 특정 아이템을 위한 공간이 있는지 확인
    /// </summary>
    public bool HasSpaceForItem(WeaponData weaponData)
    {
        if (mainGrid == null || weaponData == null || weaponPrefab == null)
        {
            return false;
        }

        // 풀에서 아이템 임시 대여 또는 임시 생성
        GameObject tempObj;
        bool fromPool = false;

        if (useObjectPool && ObjectPool.Instance != null && ObjectPool.Instance.DoesPoolExist(poolTag) &&
            ObjectPool.Instance.GetAvailableCount(poolTag) > 0)
        {
            // 풀에서 아이템 가져오기 (화면 밖 위치)
            tempObj = ObjectPool.Instance.SpawnFromPool(poolTag, new Vector3(-10000, -10000, 0), Quaternion.identity);
            fromPool = true;
        }
        else
        {
            // 임시 아이템 생성
            tempObj = Instantiate(weaponPrefab);
        }

        if (tempObj == null)
        {
            return false;
        }

        InventoryItem tempItem = tempObj.GetComponent<InventoryItem>();
        if (tempItem == null)
        {
            // 사용 후 처리
            if (fromPool)
            {
                ObjectPool.Instance.ReturnToPool(poolTag, tempObj);
            }
            else
            {
                Destroy(tempObj);
            }
            return false;
        }

        // 임시로 무기 데이터 초기화
        tempItem.Initialize(weaponData);

        // 그리드에서 공간 찾기
        Vector2Int? freePosition = mainGrid.FindSpaceForObject(tempItem);

        // 사용 후 아이템 처리
        if (fromPool)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, tempObj);
        }
        else
        {
            Destroy(tempObj);
        }

        return freePosition.HasValue;
    }

    /// <summary>
    /// 화면 밖 아이템 물리 비활성화 및 성능 최적화
    /// </summary>
    private void UpdateActivePhysicsItems()
    {
        if (mainCamera == null || physicsItems.Count == 0) return;

        // 화면 경계 계산
        Vector2 screenBounds = new Vector2(Screen.width, Screen.height);

        // 화면 밖 여백 (아이템이 완전히 화면 밖으로 나가도록)
        float margin = 100f;

        foreach (var item in physicsItems)
        {
            if (item == null || item.IsBeingDragged) continue;

            // 아이템 위치 확인
            Vector2 viewportPosition = mainCamera.WorldToScreenPoint(item.transform.position);

            // 화면에서 너무 멀리 떨어졌는지 체크
            bool isFarOutside =
                viewportPosition.x < -margin ||
                viewportPosition.x > screenBounds.x + margin ||
                viewportPosition.y < -margin ||
                viewportPosition.y > screenBounds.y + margin;

            // 너무 멀리 있는 아이템은 풀로 반환
            if (isFarOutside)
            {
                ReturnItemToPool(item);
            }
        }

        // 죽은 참조 제거
        physicsItems.RemoveAll(item => item == null);
    }

    /// <summary>
    /// 아이템을 풀로 반환
    /// </summary>
    private void ReturnItemToPool(PhysicsInventoryItem item)
    {
        if (item == null) return;

        physicsItems.Remove(item);

        // 물리 비활성화
        item.DeactivatePhysics();

        // 풀로 반환
        if (useObjectPool && ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, item.gameObject);
        }
        else
        {
            Destroy(item.gameObject);
        }
    }

    /// <summary>
    /// 모든 아이템을 풀로 반환
    /// </summary>
    private void ReturnAllItemsToPool()
    {
        if (!useObjectPool || ObjectPool.Instance == null) return;

        foreach (var item in physicsItems.ToList())
        {
            if (item != null)
            {
                ReturnItemToPool(item);
            }
        }

        physicsItems.Clear();
    }
    #endregion
}