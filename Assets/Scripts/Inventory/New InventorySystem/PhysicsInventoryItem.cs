using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 인벤토리 아이템에 물리적 특성을 추가하는 컴포넌트
/// 그리드와 물리적 환경 사이의 전환을 관리합니다.
/// 오브젝트 풀링을 지원합니다.
/// </summary>
public class PhysicsInventoryItem : MonoBehaviour, IPooledObject
{
    #region Serialized Fields
    [Header("Physics Settings")]
    [SerializeField] private float gravityScale = 980f;
    [SerializeField] private float dragDamping = 0.92f;
    [SerializeField] private float bounceMultiplier = 0.4f;
    [SerializeField] private float groundFriction = 0.8f;
    [SerializeField] private float minimumVelocity = 10f;
    [SerializeField] private Vector2 initialImpulse = new Vector2(0f, 150f);
    [SerializeField] private float visualFeedbackDuration = 0.1f;

    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image itemImage;

    [Header("Pool Settings")]
    [SerializeField] private bool useObjectPool = true;
    [SerializeField] private string poolTag = "PhysicsInventoryItem";
    #endregion

    #region Private Fields
    private InventoryItem inventoryItem;
    private Canvas parentCanvas;
    private RectTransform canvasRectTransform;
    private Vector2 velocity;
    private bool isPhysicsActive = false;
    private bool isBeingDragged = false;
    private Vector2 screenBounds;
    private Vector2 lastTouchPosition;
    private float itemLiftOffset = 350f; // InventoryController와 동일한 값
    private Vector2Int originalGridPosition;
    private bool isInitialized = false;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector3 spawnPosition;
    private bool isProtected = false;
    private bool isPaused = false;
    private Color originalColor;
    private float protectionEndTime = 0f;
    private float lastMoveTime = 0f;
    private bool isSleeping = false;
    #endregion

    #region Properties
    public bool IsPhysicsActive => isPhysicsActive;
    public bool IsBeingDragged => isBeingDragged;
    public int ActivationFrame { get; private set; }
    public float ActivationTime { get; private set; }
    public bool IsRecentlyActivated => Time.frameCount - ActivationFrame < 30;

    public void SetProtected(bool state, float duration = 0f)
    {
        isProtected = state;

        if (duration > 0f)
        {
            // 시간 기반 보호 설정
            protectionEndTime = Time.time + duration;
            Debug.Log($"Item {gameObject.name} protected for {duration} seconds (until {protectionEndTime})");
        }
    }
    public bool IsProtected
    {
        get
        {
            return isProtected || Time.time < protectionEndTime;
        }
    }

    public void PausePhysics(bool pause)
    {
        isPaused = pause;
    }
    #endregion

    #region Unity Methods
    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            Initialize();
            return;
        }

        if (isPhysicsActive && !isBeingDragged && !isPaused)
        {
            // Use unscaledDeltaTime instead of deltaTime for timeScale independence
            UpdatePhysics(Time.unscaledDeltaTime);
        }
    }

    private void OnDisable()
    {
        // 풀로 반환될 때 설정 초기화
        isPhysicsActive = false;
        isBeingDragged = false;
        velocity = Vector2.zero;
    }

    /// <summary>
    /// 컴포넌트 강제 초기화
    /// </summary>
    public void ForceInitialize()
    {
        // Components
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (itemImage == null) itemImage = GetComponent<Image>();
        inventoryItem = GetComponent<InventoryItem>();

        // 색상 저장
        if (itemImage != null)
        {
            originalColor = itemImage.color;
        }

        // Find canvas in multiple ways
        if (parentCanvas == null)
        {
            // Try parent first
            parentCanvas = GetComponentInParent<Canvas>();

            // If not found in hierarchy, find in scene
            if (parentCanvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (var canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        parentCanvas = canvas;
                        break;
                    }
                }

                // As a last resort, take any canvas
                if (parentCanvas == null && canvases.Length > 0)
                {
                    parentCanvas = canvases[0];
                }
            }

            Debug.Log($"Found canvas: {(parentCanvas ? parentCanvas.name : "NONE")}");
        }

        if (parentCanvas != null)
        {
            canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
            UpdateScreenBounds();
        }

        // Original state
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.localRotation;
        if (inventoryItem != null)
        {
            originalGridPosition = inventoryItem.GridPosition;
        }

        isInitialized = true;
    }

    /// <summary>
    /// IPooledObject 인터페이스의 OnObjectSpawn 구현
    /// 오브젝트 풀에서 가져올 때 호출됨
    /// </summary>
    public void OnObjectSpawn()
    {
        // 상태 초기화
        isPhysicsActive = false;
        isBeingDragged = false;
        velocity = Vector2.zero;

        // 초기화 확인
        if (!isInitialized)
        {
            Initialize();
        }

        // 원본 상태 저장
        if (rectTransform != null)
        {
            originalScale = rectTransform.localScale;
            originalRotation = rectTransform.localRotation;
        }

        // 컬러 복원
        if (itemImage != null)
        {
            itemImage.color = originalColor;
        }

        // 인벤토리 아이템 컴포넌트 가져오기
        if (inventoryItem != null)
        {
            originalGridPosition = inventoryItem.GridPosition;
        }
    }
    #endregion

    #region Initialization
    private void Initialize()
    {
        try
        {
            // Components
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (itemImage == null) itemImage = GetComponent<Image>();
            if (inventoryItem == null) inventoryItem = GetComponent<InventoryItem>();

            // 색상 저장
            if (itemImage != null)
            {
                originalColor = itemImage.color;
            }

            // Canvas reference
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
                // ... 기존 코드 ...
            }

            if (parentCanvas != null)
            {
                canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
                UpdateScreenBounds();
            }
            else
            {
                Debug.LogWarning("No canvas found for physics item!");
            }

            // Grid의 Scale 확인 및 저장
            ItemGrid grid = FindAnyObjectByType<ItemGrid>();
            if (grid != null)
            {
                RectTransform gridRectTransform = grid.GetComponent<RectTransform>();
                if (gridRectTransform != null)
                {
                    // Grid의 Scale 값을 저장
                    originalScale = gridRectTransform.localScale;
                    Debug.Log($"Saved grid scale: {originalScale}");
                }
            }
            else if (rectTransform != null && rectTransform.localScale != Vector3.zero)
            {
                // Grid를 찾지 못했다면 현재 스케일 저장
                originalScale = rectTransform.localScale;
            }
            else
            {
                // 기본값으로 설정
                originalScale = new Vector3(6, 6, 1); // Grid의 Scale이 6인 경우
            }

            // 원래 회전값 저장
            originalRotation = rectTransform.localRotation;

            if (inventoryItem != null)
            {
                originalGridPosition = inventoryItem.GridPosition;
            }

            isInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing PhysicsInventoryItem: {e.Message}");
        }
    }
    #endregion

    #region Physics Methods
    /// <summary>
    /// 물리 시스템 활성화 및 초기 속도 설정
    /// </summary>
    public void ActivatePhysics(Vector2? initialVelocity = null, Vector2? position = null)
    {
        if (!isInitialized) Initialize();

        try
        {
            // 오브젝트 활성화 상태 보장
            gameObject.SetActive(true);

            // 캔버스 참조 확인 및 초기화
            EnsureCanvasReference();

            // 초기 상태 설정
            isPhysicsActive = true;
            velocity = initialVelocity ?? initialImpulse;
            isSleeping = false;
            lastMoveTime = Time.time;
            UpdateScreenBounds();

            // 위치가 지정된 경우 명시적으로 설정
            if (position.HasValue && rectTransform != null)
            {
                Vector2 safePosisiton = EnsurePositionWithinScreen(position.Value);
                rectTransform.position = new Vector3(safePosisiton.x, safePosisiton.y, rectTransform.position.z);
            }

            // Grid 밖에 있을 때는 Scale을 6,6,1로 설정
            rectTransform.localScale = new Vector3(6, 6, 1);

            // 아이템 이미지 컴포넌트 상태 확인
            EnsureItemVisibility();

            // 활성화 시간 기록
            ActivationFrame = Time.frameCount;
            ActivationTime = Time.time;

            // 자동 보호 설정 - 10초 동안 보호
            SetProtected(false, 10f);

            Debug.Log($"Physics activated on {gameObject.name}, frame: {ActivationFrame}, position: {rectTransform.position}, scale: {rectTransform.localScale}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ActivatePhysics: {e.Message}");
        }
    }
    // 위치가 화면 안에 있는지 확인하고 필요시 조정하는 메서드
    private Vector2 EnsurePositionWithinScreen(Vector2 position)
    {
        if (canvasRectTransform == null)
        {
            // 캔버스 참조가 없으면 먼저 초기화 시도
            EnsureCanvasReference();

            // 여전히 없으면 원래 위치 반환
            if (canvasRectTransform == null) return position;
        }

        // 캔버스 좌표계에서 화면 경계 계산
        Vector3[] corners = new Vector3[4];
        canvasRectTransform.GetWorldCorners(corners);

        // 왼쪽 아래(0)와 오른쪽 위(2) 코너 사용
        float minX = corners[0].x + rectTransform.sizeDelta.x * 0.5f;
        float maxX = corners[2].x - rectTransform.sizeDelta.x * 0.5f;
        float minY = corners[0].y + rectTransform.sizeDelta.y * 0.5f;
        float maxY = corners[2].y - rectTransform.sizeDelta.y * 0.5f;

        // 안전 마진 추가
        float safeMargin = 10f;
        minX += safeMargin;
        maxX -= safeMargin;
        minY += safeMargin;
        maxY -= safeMargin;

        // 위치 조정
        float safeX = Mathf.Clamp(position.x, minX, maxX);
        float safeY = Mathf.Clamp(position.y, minY, maxY);

        return new Vector2(safeX, safeY);
    }
    private void EnsureCanvasReference()
    {
        if (parentCanvas != null) return;

        // 캔버스 검색 로직
        Transform current = transform;
        while (current != null)
        {
            Canvas canvas = current.GetComponent<Canvas>();
            if (canvas != null)
            {
                parentCanvas = canvas;
                Debug.Log($"Found parent canvas in hierarchy: {canvas.name}");
                break;
            }
            current = current.parent;
        }

        // 계층에서 찾지 못했다면 씬에서 검색
        if (parentCanvas == null)
        {
            // 먼저 UI 계층 구조에서 찾기 시도
            GameObject combatUI = GameObject.Find("CombatUI");
            Transform canvasTransform = null;

            if (combatUI != null)
            {
                // CombatUI/Canvas 경로 찾기
                canvasTransform = combatUI.transform.Find("Canvas");
                if (canvasTransform != null)
                {
                    Canvas canvas = canvasTransform.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        parentCanvas = canvas;
                        Debug.Log($"Found canvas in CombatUI: {canvas.name}");
                    }
                }
            }

            // 못 찾았으면 모든 캔버스 검색
            if (parentCanvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

                // 오버레이 모드인 캔버스 우선 찾기
                foreach (var canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        parentCanvas = canvas;
                        Debug.Log($"Found ScreenSpaceOverlay canvas: {canvas.name}");
                        break;
                    }
                }

                // 못 찾았으면 첫 번째 캔버스 사용
                if (parentCanvas == null && canvases.Length > 0)
                {
                    parentCanvas = canvases[0];
                    Debug.Log($"Using first available canvas: {parentCanvas.name}");
                }
            }
        }

        // 캔버스 찾았으면 rectTransform 설정
        if (parentCanvas != null)
        {
            canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
            UpdateScreenBounds();
        }
    }
    private void EnsureItemVisibility()
    {
        // 이미지 컴포넌트 확인
        if (itemImage != null)
        {
            // 비활성화된 경우 활성화
            if (!itemImage.enabled)
            {
                itemImage.enabled = true;
                Debug.Log("Enabled item image component");
            }

            // 투명도 확인
            if (itemImage.color.a < 0.5f)
            {
                Color color = itemImage.color;
                color.a = 1.0f;
                itemImage.color = color;
                Debug.Log("Restored item image opacity");
            }
        }

        // 캔버스 그룹도 확인
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null && group.alpha < 0.5f)
        {
            group.alpha = 1.0f;
            Debug.Log("Restored canvas group alpha");
        }
    }

    /// <summary>
    /// 물리 시뮬레이션을 비활성화합니다.
    /// </summary>
    public void DeactivatePhysics()
    {
        isPhysicsActive = false;
        velocity = Vector2.zero;
    }

    /// <summary>
    /// 물리 상태를 매 프레임 업데이트합니다.
    /// </summary>
    private void UpdatePhysics(float deltaTime)
    {
        try
        {
            // 극단적인 위치 검사 및 보정
            ResetPositionIfExtreme();

            // 이미지 표시 상태 간헐적 체크 (모든 프레임마다 할 필요 없음)
            if (Time.frameCount % 120 == 0)
            {
                EnsureItemVisibility();
            }

            // 'Sleep' 상태 체크 - 속도가 매우 낮고 일정 시간 동안 큰 변화가 없으면
            if (!isSleeping && velocity.sqrMagnitude < minimumVelocity * 0.5f)
            {
                if (Time.time - lastMoveTime > 1.0f)
                {
                    isSleeping = true;
                    Debug.Log($"Physics item {gameObject.name} entered sleep state");
                }
            }

            // 'Sleep' 상태면 물리 계산 최소화
            if (isSleeping)
            {
                // Sleep 상태에서는 충돌 검사만 가끔 수행
                if (Time.frameCount % 30 == 0)
                {
                    CheckBoundaryCollisions();
                }
                return;
            }

            // Apply gravity
            velocity += Vector2.down * gravityScale * deltaTime;

            // 속도 한계 설정 (너무 빠르지 않도록)
            float maxSpeed = 2000f;
            float currentSpeed = velocity.magnitude;
            if (currentSpeed > maxSpeed)
            {
                velocity = velocity.normalized * maxSpeed;
            }

            // 이전 위치 저장 (움직임 감지용)
            Vector3 oldPosition = rectTransform.position;

            // Update position
            rectTransform.position += (Vector3)velocity * deltaTime;

            // 움직임 감지
            float movement = Vector3.Distance(oldPosition, rectTransform.position);
            if (movement > 0.5f)
            {
                lastMoveTime = Time.time;
                if (isSleeping)
                {
                    isSleeping = false;
                    Debug.Log($"Physics item {gameObject.name} woke from sleep state");
                }
            }

            // Check boundary collisions
            CheckBoundaryCollisions();

            // 극단적인 위치 다시 검사 (충돌 처리 후)
            ResetPositionIfExtreme();

            // 공기 저항 적용 (감속)
            velocity *= dragDamping;

            // Stop if velocity is too low
            if (velocity.sqrMagnitude < minimumVelocity * minimumVelocity)
            {
                velocity = Vector2.zero;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in UpdatePhysics: {e.Message}");
            velocity = Vector2.zero;
        }
    }
    private void ResetPositionIfExtreme()
    {
        if (rectTransform == null || !isPhysicsActive) return;

        // 현재 위치 가져오기
        Vector3 currentPos = rectTransform.position;

        // 극단적인 값 체크 (±100,000 이상)
        bool isExtremePosition = Mathf.Abs(currentPos.x) > 100000f ||
                                 Mathf.Abs(currentPos.y) > 100000f ||
                                 Mathf.Abs(currentPos.z) > 100000f;

        // NaN 체크
        bool isNaN = float.IsNaN(currentPos.x) ||
                    float.IsNaN(currentPos.y) ||
                    float.IsNaN(currentPos.z);

        if (isExtremePosition || isNaN)
        {
            Debug.LogWarning($"Extreme position detected: {currentPos}. Resetting position.");

            // 캔버스가 있으면 중앙으로 리셋, 없으면 원점으로
            if (canvasRectTransform != null)
            {
                Vector3[] corners = new Vector3[4];
                canvasRectTransform.GetWorldCorners(corners);

                // 캔버스 중앙 계산
                Vector3 center = new Vector3(
                    (corners[0].x + corners[2].x) * 0.5f,
                    (corners[0].y + corners[2].y) * 0.5f,
                    currentPos.z
                );

                rectTransform.position = center;
            }
            else
            {
                rectTransform.position = new Vector3(Screen.width / 2, Screen.height / 2, currentPos.z);
            }

            // 속도 초기화
            velocity = Vector2.zero;

            // Sleep 상태 해제
            isSleeping = false;
            lastMoveTime = Time.time;
        }
    }

    /// <summary>
    /// 화면 경계와의 충돌을 체크하고 반응합니다.
    /// </summary>
    private void CheckBoundaryCollisions()
    {
        // 캔버스가 없거나 RectTransform이 없으면 충돌 처리 불가
        if (canvasRectTransform == null || rectTransform == null) return;

        try
        {
            // 캔버스 좌표계에서 화면 경계 계산
            Vector3[] corners = new Vector3[4];
            canvasRectTransform.GetWorldCorners(corners);

            // 경계 값
            float minX = corners[0].x;
            float maxX = corners[2].x;
            float minY = corners[0].y;
            float maxY = corners[2].y;

            // RectTransform의 크기 절반 (충돌 감지용)
            Vector2 halfSize = rectTransform.sizeDelta * rectTransform.localScale / 2f;
            Vector3 pos = rectTransform.position;

            // 경계 충돌 및 바운스 로직
            bool collided = false;

            // X축 충돌 처리
            if (pos.x + halfSize.x > maxX)
            {
                pos.x = maxX - halfSize.x;
                velocity.x = -velocity.x * bounceMultiplier;
                collided = true;

                // 속도가 매우 낮으면 부드럽게 멈춤
                if (Mathf.Abs(velocity.x) < 50f)
                {
                    velocity.x *= 0.5f;
                }
            }
            else if (pos.x - halfSize.x < minX)
            {
                pos.x = minX + halfSize.x;
                velocity.x = -velocity.x * bounceMultiplier;
                collided = true;

                if (Mathf.Abs(velocity.x) < 50f)
                {
                    velocity.x *= 0.5f;
                }
            }

            // Y축 충돌 처리
            if (pos.y + halfSize.y > maxY)
            {
                pos.y = maxY - halfSize.y;
                velocity.y = -velocity.y * bounceMultiplier;
                collided = true;

                if (Mathf.Abs(velocity.y) < 50f)
                {
                    velocity.y *= 0.5f;
                }
            }
            else if (pos.y - halfSize.y < minY)
            {
                pos.y = minY + halfSize.y;
                velocity.y = -velocity.y * bounceMultiplier;
                velocity.x *= groundFriction; // 바닥에 닿았을 때 X축 마찰 적용
                collided = true;

                // 바닥에 닿으면 X축 속도를 더 많이 감소시킴 (빨리 정지하도록)
                if (Mathf.Abs(velocity.y) < 50f)
                {
                    velocity.y *= 0.3f;
                }

                // 바닥에 닿을 때 일정 확률로 효과음 재생
                if (velocity.magnitude > 300f)
                {
                    float volume = Mathf.Clamp01(velocity.magnitude / 1000f) * 0.3f;
                    SoundManager.Instance?.PlaySound("ItemBounce_sfx", volume, false);
                }
            }

            // 충돌이 있었다면 위치 업데이트
            if (collided)
            {
                rectTransform.position = pos;
                lastMoveTime = Time.time; // 충돌은 '움직임'으로 간주
                isSleeping = false; // 충돌이 있으면 Sleep 상태 해제
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in CheckBoundaryCollisions: {e.Message}");
        }
    }
    /// <summary>
    /// 화면 경계 정보를 업데이트합니다.
    /// </summary>
    private void UpdateScreenBounds()
    {
        if (canvasRectTransform != null)
        {
            // 캔버스의 경계 계산
            Vector3[] corners = new Vector3[4];
            canvasRectTransform.GetWorldCorners(corners);

            // 좌하단과 우상단 코너 기준으로 경계 계산
            Vector3 bottomLeft = corners[0];
            Vector3 topRight = corners[2];

            // 중앙 기준 화면 크기의 절반 값
            screenBounds = new Vector2(
                (topRight.x - bottomLeft.x) * 0.5f,
                (topRight.y - bottomLeft.y) * 0.5f
            );
        }
    }
    #endregion

    #region Interaction Methods
    /// <summary>
    /// 아이템 드래그 시작
    /// </summary>
    public void StartDrag(Vector2 touchPosition)
    {
        if (!isInitialized) Initialize();

        try
        {
            isBeingDragged = true;
            lastTouchPosition = touchPosition;

            // 물리 아이템을 드래그할 때는 Scale을 6,6,1로 유지
            rectTransform.localScale = new Vector3(6, 6, 1);
            rectTransform.rotation = originalRotation;

            // 물리 효과 비활성화
            DeactivatePhysics();

            // 터치 위치 + 오프셋으로 이동 (위로 살짝 띄움)
            Vector2 liftedPosition = touchPosition + Vector2.up * itemLiftOffset;
            rectTransform.position = liftedPosition;
            Debug.Log($"Started dragging physics item: {gameObject.name}, scale: {rectTransform.localScale}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in StartDrag: {e.Message}");
        }
    }
    /// <summary>
    /// 아이템 드래그 중 위치 업데이트
    /// </summary>
    public void UpdateDragPosition(Vector2 touchPosition)
    {
        if (!isBeingDragged) return;

        try
        {
            lastTouchPosition = touchPosition;
            Vector2 liftedPosition = touchPosition + Vector2.up * itemLiftOffset;
            rectTransform.position = liftedPosition;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in UpdateDragPosition: {e.Message}");
        }
    }

    /// <summary>
    /// 아이템 드래그 종료
    /// </summary>
    public void EndDrag(ItemGrid targetGrid, Vector2 finalPosition)
    {
        if (!isBeingDragged) return;

        try
        {
            isBeingDragged = false;

            // 그리드 배치 시도
            if (targetGrid != null)
            {
                // 그리드 위치 확인
                Vector2Int gridPosition = targetGrid.GetGridPosition(finalPosition);
                Debug.Log($"Grid position: {gridPosition}, can place: {targetGrid.IsValidPosition(gridPosition) && targetGrid.CanPlaceItem(inventoryItem, gridPosition)}");

                // 유효한 그리드 위치이고 배치 가능하면 그리드에 배치
                if (targetGrid.IsValidPosition(gridPosition) &&
                    targetGrid.CanPlaceItem(inventoryItem, gridPosition))
                {
                    // 그리드에 배치하기 전에 Scale을 1,1,1로 변경
                    rectTransform.localScale = Vector3.one;

                    // 그리드에 배치
                    rectTransform.SetParent(targetGrid.transform, false);
                    targetGrid.PlaceItem(inventoryItem, gridPosition);
                    DeactivatePhysics();
                    return;
                }
            }

            // 유효하지 않은 위치면 물리 활성화 (Scale은 ActivatePhysics에서 처리)
            Vector2 dragVelocity = (finalPosition - lastTouchPosition) * 5f;
            ActivatePhysics(dragVelocity);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in EndDrag: {e.Message}");
            // 오류 발생 시 물리 효과 활성화
            ActivatePhysics();
        }
    }
    /// <summary>
    /// 아이템 회전
    /// </summary>
    public void Rotate()
    {
        if (inventoryItem != null)
        {
            inventoryItem.Rotate();
        }
    }

    /// <summary>
    /// 출현 위치 설정
    /// </summary>
    public void SetSpawnPosition(Vector3 position)
    {
        spawnPosition = position;
        rectTransform.position = position;
    }

    /// <summary>
    /// 오브젝트 풀로 아이템 반환
    /// </summary>
    public void ReturnToPool()
    {
        if (!useObjectPool || ObjectPool.Instance == null) return;

        // 물리 비활성화
        DeactivatePhysics();

        // 풀로 반환
        ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
    }
}
    #endregion