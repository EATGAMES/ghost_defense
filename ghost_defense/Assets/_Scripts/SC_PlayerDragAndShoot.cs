using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SC_PlayerDragAndShoot : MonoBehaviour
{
    private const string DragArrowRightRootName = "OBJ_DragArrow_Right";
    private const string DragArrowLeftRootName = "OBJ_DragArrow_Left";
    private static bool hasAnyDragGuideBeenViewed;

    [Tooltip("드래그 가능한 최소 X 좌표(월드 좌표)")]
    [SerializeField] private float minX = -3.5f;

    [Tooltip("드래그 가능한 최대 X 좌표(월드 좌표)")]
    [SerializeField] private float maxX = 3.5f;

    [Tooltip("캐릭터 대신 넓은 입력 존에서 드래그 시작을 허용할지 여부")]
    [SerializeField] private bool useWideInputZone = true;

    [Tooltip("드래그 시작을 허용할 입력 존의 가로 길이(월드 좌표)")]
    [SerializeField] private float inputZoneWidth = 5.5f;

    [Tooltip("드래그 시작을 허용할 입력 존의 세로 길이(월드 좌표)")]
    [SerializeField] private float inputZoneHeight = 2.2f;

    [Tooltip("입력 존 중심 위치에 더할 오프셋(월드 좌표)")]
    [SerializeField] private Vector2 inputZoneOffset = new Vector2(0f, 0.2f);

    [Tooltip("발사 시작 속도(+Y 방향)")]
    [SerializeField] private float shootSpeed = 12f;

    [Tooltip("드래그 중 Y 좌표를 고정할지 여부")]
    [SerializeField] private bool lockYPosition = true;

    [Tooltip("드래그 고정 Y 좌표(기본값이면 시작 위치 사용)")]
    [SerializeField] private float fixedY = -7f;

    [Tooltip("발사 후 속도 감소량(값이 클수록 빨리 멈춤)")]
    [SerializeField] private float deceleration = 4.5f;

    [Tooltip("충돌 시 전체 속도 감쇠 비율(0~1)")]
    [SerializeField] [Range(0f, 1f)] private float collisionDamping = 0.65f;

    [Tooltip("충돌 시 미끄러짐(접선 속도) 감쇠 비율(0~1, 낮을수록 빨리 멈춤)")]
    [SerializeField] [Range(0f, 1f)] private float sideSlipDamping = 0.15f;

    [Tooltip("충돌 시 속도 방향을 좌우로 미세하게 랜덤 회전할지 여부")]
    [SerializeField] private bool useCollisionAngleJitter = false;

    [Tooltip("충돌 시 랜덤 회전 최대 각도(도)")]
    [SerializeField] private float collisionAngleJitterMax = 4f;

    [Tooltip("이 속도 이하로 떨어지면 정지 처리")]
    [SerializeField] private float stopSpeedThreshold = 0.2f;

    [Tooltip("아래 방향 속도(Y<0)일 때 추가로 적용할 감속 계수")]
    [SerializeField] private float downwardBrakeMultiplier = 2f;

    [Tooltip("발사 전에는 물리 충돌을 비활성화할지 여부")]
    [SerializeField] private bool disableCollisionBeforeShot = true;

    [Tooltip("주변에 다른 캐릭터가 겹치면 발사를 막을지 여부")]
    [SerializeField] private bool blockShootWhenOverlappingCharacter = true;

    [Tooltip("발사 차단을 검사할 세로 높이(월드 좌표)")]
    [SerializeField] private float shootBlockCheckHeight = 1.2f;

    [Tooltip("발사 직전 물리 좌표를 강제로 동기화할지 여부")]
    [SerializeField] private bool syncPhysicsBeforeShot = true;

    [Tooltip("드래그 중에만 표시할 가이드 오브젝트입니다.")]
    [SerializeField] private GameObject guideObject;

    [Tooltip("첫 드래그 전까지 표시할 오른쪽 화살표 이미지입니다.")]
    [SerializeField] private GameObject dragArrowRightObject;

    [Tooltip("첫 드래그 전까지 표시할 왼쪽 화살표 이미지입니다.")]
    [SerializeField] private GameObject dragArrowLeftObject;

    private Camera mainCamera;
    private Rigidbody2D rb2D;
    private Collider2D col2D;
    private bool isDragging;
    private bool isShot;
    private bool hasCollidedAfterShot;
    private float zDepthFromCamera;
    private bool wasMousePressed;
    private bool wasTouchPressed;
    private bool hasViewedDragGuide;
    private float cardShootSpeedBonus;
    private Vector3 dragStartPosition;
    private Vector3 guideOriginalLocalScale = Vector3.one;
    private readonly Collider2D[] overlapResults = new Collider2D[16];

    public bool IsShot => isShot;
    public bool HasCollidedAfterShot => hasCollidedAfterShot;

    private void Awake()
    {
        mainCamera = Camera.main;
        rb2D = GetComponent<Rigidbody2D>();
        col2D = GetComponent<Collider2D>();

        if (guideObject == null)
        {
            Transform guideTransform = transform.Find("OBJ_Guide");
            if (guideTransform != null)
            {
                guideObject = guideTransform.gameObject;
            }
        }

        if (guideObject != null)
        {
            guideOriginalLocalScale = guideObject.transform.localScale;
        }

        if (dragArrowRightObject == null)
        {
            dragArrowRightObject = FindSceneObjectByExactName(DragArrowRightRootName);
        }

        if (dragArrowLeftObject == null)
        {
            dragArrowLeftObject = FindSceneObjectByExactName(DragArrowLeftRootName);
        }

        if (lockYPosition && Mathf.Approximately(fixedY, -7f))
        {
            fixedY = transform.position.y;
        }

        if (mainCamera != null)
        {
            zDepthFromCamera = Mathf.Abs(transform.position.z - mainCamera.transform.position.z);
        }

        hasViewedDragGuide = hasAnyDragGuideBeenViewed;
        ApplyCollisionState();
        SetGuideVisible(false);
        RefreshDragArrowVisibility();
    }

    private void Start()
    {
        SC_CardManager cardManager = FindAnyObjectByType<SC_CardManager>();
        if (cardManager != null)
        {
            SetCardShootSpeedBonus(cardManager.AttackQueueSpeedBonus);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (mainCamera == null || isShot)
        {
            return;
        }

        HandleTouchInput();
        HandleMouseInput();
    }

    private void FixedUpdate()
    {
        if (rb2D == null || isDragging)
        {
            return;
        }

        Vector2 velocity = rb2D.linearVelocity;
        if (velocity.magnitude <= stopSpeedThreshold)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            return;
        }

        float currentDeceleration = deceleration;
        if (velocity.y < 0f)
        {
            currentDeceleration *= downwardBrakeMultiplier;
        }

        rb2D.linearVelocity = Vector2.MoveTowards(velocity, Vector2.zero, currentDeceleration * Time.fixedDeltaTime);
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null)
        {
            wasTouchPressed = false;
            return;
        }

        var primaryTouch = Touchscreen.current.primaryTouch;
        Vector2 screenPoint = primaryTouch.position.ReadValue();
        bool isPressed = primaryTouch.press.isPressed;
        Vector3 worldPoint = ScreenToWorldPoint(screenPoint);

        if (isPressed && !wasTouchPressed)
        {
            if (CanStartDrag(worldPoint))
            {
                isDragging = true;
                dragStartPosition = transform.position;
                HandleDragStarted();
            }
        }
        else if (isPressed && wasTouchPressed)
        {
            if (isDragging)
            {
                DragTo(worldPoint);
            }
        }
        else if (!isPressed && wasTouchPressed)
        {
            if (isDragging)
            {
                isDragging = false;
                ShootForward();
            }
        }

        wasTouchPressed = isPressed;
    }

    private void HandleMouseInput()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            wasMousePressed = false;
            return;
        }

        if (Mouse.current == null)
        {
            wasMousePressed = false;
            return;
        }

        Vector2 screenPoint = Mouse.current.position.ReadValue();
        bool isPressed = Mouse.current.leftButton.isPressed;
        Vector3 worldPoint = ScreenToWorldPoint(screenPoint);

        if (isPressed && !wasMousePressed)
        {
            if (CanStartDrag(worldPoint))
            {
                isDragging = true;
                dragStartPosition = transform.position;
                HandleDragStarted();
            }
        }
        else if (isPressed && wasMousePressed)
        {
            if (isDragging)
            {
                DragTo(worldPoint);
            }
        }
        else if (!isPressed && wasMousePressed)
        {
            if (isDragging)
            {
                isDragging = false;
                ShootForward();
            }
        }

        wasMousePressed = isPressed;
    }

    private Vector3 ScreenToWorldPoint(Vector2 screenPoint)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, zDepthFromCamera));
        world.z = transform.position.z;
        return world;
    }

    private bool IsPointerOverSelf(Vector3 worldPoint)
    {
        if (col2D == null)
        {
            return true;
        }

        return col2D.OverlapPoint(worldPoint);
    }

    private bool CanStartDrag(Vector3 worldPoint)
    {
        if (useWideInputZone && IsPointerInsideWideInputZone(worldPoint))
        {
            return true;
        }

        return IsPointerOverSelf(worldPoint);
    }

    private bool IsPointerInsideWideInputZone(Vector3 worldPoint)
    {
        Vector2 zoneCenter = GetInputZoneCenter();
        float halfWidth = Mathf.Max(0.01f, inputZoneWidth) * 0.5f;
        float halfHeight = Mathf.Max(0.01f, inputZoneHeight) * 0.5f;

        return worldPoint.x >= zoneCenter.x - halfWidth &&
            worldPoint.x <= zoneCenter.x + halfWidth &&
            worldPoint.y >= zoneCenter.y - halfHeight &&
            worldPoint.y <= zoneCenter.y + halfHeight;
    }

    private Vector2 GetInputZoneCenter()
    {
        float baseY = lockYPosition ? fixedY : transform.position.y;
        Vector2 basePosition = new Vector2(transform.position.x, baseY);
        return basePosition + inputZoneOffset;
    }

    private void DragTo(Vector3 worldPoint)
    {
        float clampedX = Mathf.Clamp(worldPoint.x, minX, maxX);
        float targetY = lockYPosition ? fixedY : worldPoint.y;
        Vector3 targetPosition = new Vector3(clampedX, targetY, transform.position.z);

        if (guideObject != null && guideObject.activeSelf && !guideObject.transform.IsChildOf(transform))
        {
            guideObject.transform.position = targetPosition;
        }

        if (rb2D == null)
        {
            transform.position = targetPosition;
            return;
        }

        // Dynamic Rigidbody2D를 transform으로 직접 끌면 발사 순간 물리 좌표와 렌더 좌표가 어긋나 끊겨 보일 수 있다.
        rb2D.linearVelocity = Vector2.zero;
        rb2D.angularVelocity = 0f;
        rb2D.position = new Vector2(targetPosition.x, targetPosition.y);
    }

    private void ShootForward()
    {
        if (blockShootWhenOverlappingCharacter && IsShootBlockedByCharacter())
        {
            return;
        }

        SetGuideVisible(false);

        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;

            if (syncPhysicsBeforeShot)
            {
                Physics2D.SyncTransforms();
            }
        }

        SetShotState(true);
        SetPostLaunchCollisionState(false);
        ReportShotGradeToPreviewUI();

        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.up * GetFinalShootSpeed();
        }
    }

    private void ReportShotGradeToPreviewUI()
    {
        SC_CharacterPresenter presenter = GetComponent<SC_CharacterPresenter>();
        if (presenter == null)
        {
            return;
        }

        SC_CharacterGradePreviewUI previewUI = FindAnyObjectByType<SC_CharacterGradePreviewUI>();
        if (previewUI == null)
        {
            return;
        }

        previewUI.ReportReachedGrade(presenter.MergeGrade);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb2D == null)
        {
            return;
        }

        if (isShot && !IsExcludedPostLaunchCollision(collision.collider))
        {
            SetPostLaunchCollisionState(true);
        }

        SC_CharacterMergeController myMerge = GetComponent<SC_CharacterMergeController>();
        if (myMerge != null && myMerge.TryMergeFromCollision(collision.collider))
        {
            return;
        }

        Vector2 velocity = rb2D.linearVelocity * collisionDamping;
        bool isCharacterCollision = collision.collider.GetComponent<SC_CharacterMergeController>() != null
            || collision.collider.GetComponentInParent<SC_CharacterMergeController>() != null;

        if (!isCharacterCollision && collision.contactCount > 0)
        {
            Vector2 normal = collision.GetContact(0).normal;
            float normalSpeed = Vector2.Dot(velocity, normal);
            Vector2 normalVelocity = normal * normalSpeed;
            Vector2 tangentVelocity = velocity - normalVelocity;
            velocity = normalVelocity + tangentVelocity * sideSlipDamping;
        }

        if (useCollisionAngleJitter && velocity.sqrMagnitude > Mathf.Epsilon)
        {
            float randomAngle = Random.Range(-collisionAngleJitterMax, collisionAngleJitterMax);
            velocity = RotateVector2(velocity, randomAngle);
        }

        rb2D.linearVelocity = velocity;

        if (rb2D.linearVelocity.magnitude <= stopSpeedThreshold)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }
    }

    public void SetShotState(bool shot)
    {
        isShot = shot;
        if (!shot)
        {
            hasCollidedAfterShot = false;
        }

        ApplyCollisionState();
    }

    public void SetPostLaunchCollisionState(bool collided)
    {
        hasCollidedAfterShot = collided;
    }

    public void SetCardShootSpeedBonus(float speedBonus)
    {
        cardShootSpeedBonus = Mathf.Max(0f, speedBonus);
    }

    public void CancelDragAndResetToStartPosition()
    {
        if (isShot)
        {
            return;
        }

        if (isDragging)
        {
            transform.position = dragStartPosition;
        }

        isDragging = false;
        wasMousePressed = false;
        wasTouchPressed = false;
        SetGuideVisible(false);
    }

    private void HandleDragStarted()
    {
        SetGuideVisible(true);

        if (hasAnyDragGuideBeenViewed)
        {
            hasViewedDragGuide = true;
            RefreshDragArrowVisibility();
            return;
        }

        hasAnyDragGuideBeenViewed = true;
        hasViewedDragGuide = true;
        RefreshDragArrowVisibility();
    }

    private void SetGuideVisible(bool isVisible)
    {
        if (guideObject == null)
        {
            return;
        }

        guideObject.SetActive(isVisible);
        if (isVisible)
        {
            if (guideObject.transform.IsChildOf(transform))
            {
                guideObject.transform.localPosition = Vector3.zero;
                guideObject.transform.localScale = new Vector3(
                    transform.lossyScale.x != 0f ? guideOriginalLocalScale.x / transform.lossyScale.x : guideOriginalLocalScale.x,
                    transform.lossyScale.y != 0f ? guideOriginalLocalScale.y / transform.lossyScale.y : guideOriginalLocalScale.y,
                    transform.lossyScale.z != 0f ? guideOriginalLocalScale.z / transform.lossyScale.z : guideOriginalLocalScale.z);
            }
            else
            {
                guideObject.transform.position = transform.position;
                guideObject.transform.localScale = guideOriginalLocalScale;
            }
        }
        else
        {
            guideObject.transform.localScale = guideOriginalLocalScale;
        }
    }

    private void RefreshDragArrowVisibility()
    {
        hasViewedDragGuide = hasAnyDragGuideBeenViewed;
        bool isVisible = !hasViewedDragGuide;
        SetSceneObjectActiveByExactName(DragArrowRightRootName, isVisible);
        SetSceneObjectActiveByExactName(DragArrowLeftRootName, isVisible);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        dragArrowRightObject = FindSceneObjectByExactName(DragArrowRightRootName);
        dragArrowLeftObject = FindSceneObjectByExactName(DragArrowLeftRootName);
        RefreshDragArrowVisibility();
        SetGuideVisible(false);
    }

    private static GameObject FindSceneObjectByExactName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject targetObject = allObjects[i];
            if (targetObject == null)
            {
                continue;
            }

            if (targetObject.name != objectName)
            {
                continue;
            }

            if (!targetObject.scene.IsValid() || !targetObject.scene.isLoaded)
            {
                continue;
            }

            return targetObject;
        }

        return null;
    }

    private static void SetSceneObjectActiveByExactName(string objectName, bool isActive)
    {
        GameObject targetObject = FindSceneObjectByExactName(objectName);
        if (targetObject == null)
        {
            return;
        }

        targetObject.SetActive(isActive);
    }

    private void ApplyCollisionState()
    {
        if (!disableCollisionBeforeShot || col2D == null)
        {
            return;
        }

        col2D.isTrigger = !isShot;
    }

    private bool IsShootBlockedByCharacter()
    {
        if (col2D == null)
        {
            return false;
        }

        float checkWidth = GetCharacterWidthForShootBlock();
        Vector2 checkSize = new Vector2(checkWidth, shootBlockCheckHeight);
        ContactFilter2D contactFilter = default;
        contactFilter.useTriggers = true;
        int hitCount = Physics2D.OverlapBox(transform.position, checkSize, 0f, contactFilter, overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = overlapResults[i];
            if (hit == null || hit == col2D)
            {
                continue;
            }

            SC_CharacterMergeController otherCharacter = hit.GetComponentInParent<SC_CharacterMergeController>();
            if (otherCharacter != null && otherCharacter.gameObject != gameObject)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExcludedPostLaunchCollision(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        return other.GetComponent<SC_FieldDetectTrigger>() != null
            || other.GetComponentInParent<SC_FieldDetectTrigger>() != null;
    }

    private static Vector2 RotateVector2(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private float GetCharacterWidthForShootBlock()
    {
        if (col2D == null)
        {
            return 1f;
        }

        return Mathf.Max(0.01f, col2D.bounds.size.x);
    }

    private float GetFinalShootSpeed()
    {
        return Mathf.Max(0f, shootSpeed + cardShootSpeedBonus);
    }

    private void OnDrawGizmosSelected()
    {
        if (useWideInputZone)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);
            Vector2 zoneCenter = GetInputZoneCenter();
            Vector3 zoneSize = new Vector3(inputZoneWidth, inputZoneHeight, 0f);
            Gizmos.DrawWireCube(zoneCenter, zoneSize);
        }

        if (!blockShootWhenOverlappingCharacter)
        {
            return;
        }

        Gizmos.color = Color.red;
        float checkWidth = GetCharacterWidthForShootBlock();
        Vector3 checkSize = new Vector3(checkWidth, shootBlockCheckHeight, 0f);
        Gizmos.DrawWireCube(transform.position, checkSize);
    }
}
