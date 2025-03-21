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
    #endregion

    #region Properties
    public bool IsPhysicsActive => isPhysicsActive;
    public bool IsBeingDragged => isBeingDragged;
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

        if (isPhysicsActive && !isBeingDragged)
        {
            UpdatePhysics();
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
        // 컴포넌트 참조 설정
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (itemImage == null) itemImage = GetComponent<Image>();
        inventoryItem = GetComponent<InventoryItem>();

        // 캔버스 참조 설정
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
            UpdateScreenBounds();
        }

        // 원본 상태 기록
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.localRotation;
        if (inventoryItem != null)
        {
            originalGridPosition = inventoryItem.GridPosition;
        }

        isInitialized = true;
    }
    #endregion

    #region Physics Methods
    /// <summary>
    /// 물리 시뮬레이션을 활성화하고 초기 속도를 설정합니다.
    /// </summary>
    /// <param name="initialVelocity">초기 속도 (없으면 기본값 사용)</param>
    public void ActivatePhysics(Vector2? initialVelocity = null)
    {
        if (!isInitialized) Initialize();

        isPhysicsActive = true;
        velocity = initialVelocity ?? initialImpulse;
        UpdateScreenBounds();

        // 물리가 활성화되면 부모를 캔버스로 변경하여 그리드에서 분리
        if (canvasRectTransform != null)
        {
            rectTransform.SetParent(canvasRectTransform, true);
        }

        // 아이템이 그리드에 있었다면 그리드 위치 초기화
        if (inventoryItem != null && inventoryItem.OnGrid)
        {
            inventoryItem.SetGridPosition(new Vector2Int(-1, -1));
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
    private void UpdatePhysics()
    {
        // 중력 적용
        velocity += Vector2.down * gravityScale * Time.deltaTime;

        // 위치 업데이트
        rectTransform.position += (Vector3)velocity * Time.deltaTime;

        // 경계 충돌 검사
        CheckBoundaryCollisions();

        // 속도가 특정 값 이하면 멈춤 (성능 최적화)
        if (velocity.sqrMagnitude < minimumVelocity * minimumVelocity)
        {
            velocity = Vector2.zero;
        }
    }

    /// <summary>
    /// 화면 경계와의 충돌을 체크하고 반응합니다.
    /// </summary>
    private void CheckBoundaryCollisions()
    {
        if (canvasRectTransform == null) return;

        // 이미지 크기 얻기
        Vector2 itemHalfSize = rectTransform.sizeDelta * rectTransform.localScale / 2;
        Vector2 itemPosition = rectTransform.position;

        // 화면 우측 경계 충돌
        if (itemPosition.x + itemHalfSize.x > screenBounds.x)
        {
            rectTransform.position = new Vector3(screenBounds.x - itemHalfSize.x, rectTransform.position.y, rectTransform.position.z);
            velocity.x = -velocity.x * bounceMultiplier;
        }
        // 화면 좌측 경계 충돌
        else if (itemPosition.x - itemHalfSize.x < -screenBounds.x)
        {
            rectTransform.position = new Vector3(-screenBounds.x + itemHalfSize.x, rectTransform.position.y, rectTransform.position.z);
            velocity.x = -velocity.x * bounceMultiplier;
        }

        // 화면 하단 경계 충돌
        if (itemPosition.y - itemHalfSize.y < -screenBounds.y)
        {
            rectTransform.position = new Vector3(rectTransform.position.x, -screenBounds.y + itemHalfSize.y, rectTransform.position.z);

            // 바닥에 닿으면 Y 속도 반전 및 감쇠, X 속도는 마찰로 감소
            velocity.y = -velocity.y * bounceMultiplier;
            velocity.x *= groundFriction;

            // Y 속도가 낮을 때 추가 감쇠 (바닥에서 진동 방지)
            if (Mathf.Abs(velocity.y) < 100f)
            {
                velocity.y *= 0.5f;
            }
        }

        // 화면 상단 경계 충돌 (필요한 경우)
        else if (itemPosition.y + itemHalfSize.y > screenBounds.y)
        {
            rectTransform.position = new Vector3(rectTransform.position.x, screenBounds.y - itemHalfSize.y, rectTransform.position.z);
            velocity.y = -velocity.y * bounceMultiplier;
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

        isBeingDragged = true;
        lastTouchPosition = touchPosition;

        // 원래 크기와 회전으로 복원
        rectTransform.localScale = originalScale;
        rectTransform.localRotation = originalRotation;

        // 물리 효과 비활성화
        DeactivatePhysics();
    }

    /// <summary>
    /// 아이템 드래그 중 위치 업데이트
    /// </summary>
    public void UpdateDragPosition(Vector2 touchPosition)
    {
        if (!isBeingDragged) return;

        lastTouchPosition = touchPosition;
        Vector2 liftedPosition = touchPosition + Vector2.up * itemLiftOffset;
        rectTransform.position = liftedPosition;
    }

    /// <summary>
    /// 아이템 드래그 종료
    /// </summary>
    public void EndDrag(ItemGrid targetGrid, Vector2 finalPosition)
    {
        isBeingDragged = false;

        if (targetGrid != null)
        {
            // 그리드 위치 확인
            Vector2Int gridPosition = targetGrid.GetGridPosition(finalPosition);

            // 유효한 그리드 위치이고 배치 가능하면 그리드에 배치
            if (targetGrid.IsValidPosition(gridPosition) &&
                targetGrid.CanPlaceItem(inventoryItem, gridPosition))
            {
                // 그리드에 배치
                rectTransform.SetParent(targetGrid.transform, false);
                targetGrid.PlaceItem(inventoryItem, gridPosition);
                DeactivatePhysics();
                return;
            }
        }

        // 유효하지 않은 위치면 물리 활성화
        Vector2 dragVelocity = (finalPosition - lastTouchPosition) * 5f; // 드래그 방향으로 약간의 초기 속도
        ActivatePhysics(dragVelocity);
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
    #endregion
}