using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SC_DropCharacterController : MonoBehaviour, IFieldCharacterRuntime
{
    private const float ShrinkShotScaleMultiplier = 0.75f;
    private const float PoweredDropSpeedBonus = 8f;
    private const string DragArrowRightRootName = "OBJ_DragArrow_Right";
    private const string DragArrowLeftRootName = "OBJ_DragArrow_Left";
    private static bool hasAnyDragGuideBeenViewed;

    [Tooltip("드래그 가능한 최소 X 좌표(월드 좌표)입니다.")]
    [SerializeField] private float minX = -3.5f;

    [Tooltip("드래그 가능한 최대 X 좌표(월드 좌표)입니다.")]
    [SerializeField] private float maxX = 3.5f;

    [Tooltip("캐릭터 대신 넓은 입력 존에서 드래그 시작을 허용할지 여부입니다.")]
    [SerializeField] private bool useWideInputZone = true;

    [Tooltip("드래그 시작을 허용할 입력 존의 가로 길이(월드 좌표)입니다.")]
    [SerializeField] private float inputZoneWidth = 5.5f;

    [Tooltip("드래그 시작을 허용할 입력 존의 세로 길이(월드 좌표)입니다.")]
    [SerializeField] private float inputZoneHeight = 2.2f;

    [Tooltip("입력 존 중심 위치에 더할 오프셋(월드 좌표)입니다.")]
    [SerializeField] private Vector2 inputZoneOffset = new Vector2(0f, 0.2f);

    [Tooltip("드래그 중 Y 좌표를 고정할지 여부입니다.")]
    [SerializeField] private bool lockYPosition = true;

    [Tooltip("드래그 고정 Y 좌표와 입력 존 중심 Y를 대기 시작 위치로 자동 맞출지 여부입니다.")]
    [SerializeField] private bool useWaitingPositionY = true;

    [Tooltip("드래그 고정 Y 좌표입니다. 0이면 대기 시작 위치의 Y를 사용합니다.")]
    [SerializeField] private float fixedY;

    [Tooltip("캐릭터가 아래로 떨어지는 기본 속도입니다.")]
    [SerializeField] private float dropSpeed = 8f;

    [Tooltip("드롭 전에는 물리 충돌을 비활성화할지 여부입니다.")]
    [SerializeField] private bool disableCollisionBeforeDrop = true;

    [Tooltip("드롭 직전 물리 좌표를 강제로 동기화할지 여부입니다.")]
    [SerializeField] private bool syncPhysicsBeforeDrop = true;

    [Tooltip("드래그 중에만 표시할 가이드 오브젝트입니다.")]
    [SerializeField] private GameObject guideObject;

    [Tooltip("첫 드래그 전까지 표시할 오른쪽 화살표 이미지입니다.")]
    [SerializeField] private GameObject dragArrowRightObject;

    [Tooltip("첫 드래그 전까지 표시할 왼쪽 화살표 이미지입니다.")]
    [SerializeField] private GameObject dragArrowLeftObject;

    [Tooltip("낙하에 사용할 Rigidbody2D입니다. 비워두면 현재 오브젝트에서 자동으로 찾습니다.")]
    [SerializeField] private Rigidbody2D cachedRigidbody2D;

    [Tooltip("드래그 입력을 받을 Collider2D입니다. 비워두면 현재 오브젝트에서 자동으로 찾습니다.")]
    [SerializeField] private Collider2D cachedCollider2D;

    private Camera mainCamera;
    private bool isDragging;
    private bool isDropped;
    private bool wasMousePressed;
    private bool wasTouchPressed;
    private bool hasViewedDragGuide;
    private bool suppressDragUntilPointerReleased;
    private float zDepthFromCamera;
    private Vector3 waitingPosition;
    private Vector3 guideOriginalLocalScale = Vector3.one;
    private Vector3 defaultCharacterScale = Vector3.one;
    private Vector2 dropVelocity;
    private float defaultGravityScale;
    private bool isShrinkShotVisualApplied;
    private SC_CardManager cardManager;
    private SC_BattleManager battleManager;
    private SC_FinalMergePopup finalMergePopup;
    private SC_ClearPopup clearPopup;
    private int remainingCollisionEraseCount;

    public bool IsDropped => isDropped;
    public bool IsActiveDrop => isDropped && gameObject.activeInHierarchy;
    public bool HasCollisionEraseRemaining => remainingCollisionEraseCount > 0;
    public Vector2 CurrentVelocity => cachedRigidbody2D != null ? cachedRigidbody2D.linearVelocity : dropVelocity;
    public StageBattleDirection BattleDirection => StageBattleDirection.DOWN;
    public GameObject RuntimeObject => gameObject;
    public Transform RuntimeTransform => transform;
    public int MergeGrade
    {
        get
        {
            SC_CharacterPresenter presenter = GetComponent<SC_CharacterPresenter>();
            return presenter != null ? presenter.MergeGrade : 1;
        }
    }
    public bool IsWaiting => !isDropped;
    public bool IsLaunched => isDropped;
    public bool IsDragging => isDragging;
    public bool IsActiveFieldCharacter => IsActiveDrop;

    private void Awake()
    {
        mainCamera = Camera.main;
        EnsureReferences();
        EnsureGuideReferences();
        EnsureDragArrowReferences();
        EnsureCardManagerReference();
        ResolvePopupReferences();
        defaultCharacterScale = transform.localScale;
        if (cachedRigidbody2D != null)
        {
            defaultGravityScale = Mathf.Max(0f, cachedRigidbody2D.gravityScale);
        }

        if (mainCamera != null)
        {
            zDepthFromCamera = Mathf.Abs(transform.position.z - mainCamera.transform.position.z);
        }

        waitingPosition = transform.position;
        if (lockYPosition && ShouldUseWaitingPositionY())
        {
            fixedY = waitingPosition.y;
        }

        hasViewedDragGuide = hasAnyDragGuideBeenViewed;
        SetDropVelocity(Vector2.down * Mathf.Max(0f, dropSpeed));
        ApplyCollisionState();
        SetGuideVisible(false);
        RefreshDragArrowVisibility();
    }

    private void OnEnable()
    {
        SC_FieldCharacterRegistry.Register(this);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SC_FieldCharacterRegistry.Unregister(this);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (isDropped)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        if (IsInputBlockedByPopup())
        {
            CancelDragAndSuppressUntilRelease();
            return;
        }

        HandleTouchInput();
        HandleMouseInput();
    }

    public void ResetToWaitingState(Vector3 startPosition)
    {
        EnsureReferences();
        EnsureGuideReferences();
        EnsureDragArrowReferences();
        EnsureCardManagerReference();

        isDragging = false;
        isDropped = false;
        wasMousePressed = false;
        wasTouchPressed = false;
        suppressDragUntilPointerReleased = false;
        remainingCollisionEraseCount = 0;
        waitingPosition = startPosition;
        SetGuideVisible(false);
        RefreshDragArrowVisibility();

        if (lockYPosition && ShouldUseWaitingPositionY())
        {
            fixedY = waitingPosition.y;
        }

        transform.position = waitingPosition;
        SetShrinkShotVisual(cardManager != null && cardManager.IsShrinkShotActive());
        if (cachedRigidbody2D != null)
        {
            if (cachedRigidbody2D.gravityScale > 0f)
            {
                defaultGravityScale = cachedRigidbody2D.gravityScale;
            }

            cachedRigidbody2D.gravityScale = 0f;
            cachedRigidbody2D.linearVelocity = Vector2.zero;
            cachedRigidbody2D.angularVelocity = 0f;
        }

        ApplyCollisionState();
    }

    public void SetDropActive(bool isActive)
    {
        isDropped = isActive;
        if (!isActive)
        {
            remainingCollisionEraseCount = 0;
        }

        if (cachedRigidbody2D != null)
        {
            if (isActive)
            {
                cachedRigidbody2D.simulated = true;
                cachedRigidbody2D.gravityScale = ResolveActiveGravityScale();
                cachedRigidbody2D.linearVelocity = dropVelocity;
            }
            else
            {
                cachedRigidbody2D.gravityScale = 0f;
                cachedRigidbody2D.linearVelocity = Vector2.zero;
                cachedRigidbody2D.angularVelocity = 0f;
            }
        }

        ApplyCollisionState();
    }

    public void SetDropVelocity(Vector2 velocity)
    {
        dropVelocity = velocity.sqrMagnitude > 0f ? velocity : Vector2.down * Mathf.Max(0f, dropSpeed);

        if (isDropped && cachedRigidbody2D != null)
        {
            cachedRigidbody2D.linearVelocity = dropVelocity;
        }
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null)
        {
            wasTouchPressed = false;
            suppressDragUntilPointerReleased = false;
            return;
        }

        Vector2 screenPoint = Touchscreen.current.primaryTouch.position.ReadValue();
        bool isPressed = Touchscreen.current.primaryTouch.press.isPressed;
        if (suppressDragUntilPointerReleased)
        {
            wasTouchPressed = isPressed;
            if (!isPressed)
            {
                suppressDragUntilPointerReleased = false;
            }

            return;
        }

        HandlePointerInput(screenPoint, isPressed, ref wasTouchPressed);
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
        if (suppressDragUntilPointerReleased)
        {
            wasMousePressed = isPressed;
            if (!isPressed)
            {
                suppressDragUntilPointerReleased = false;
            }

            return;
        }

        HandlePointerInput(screenPoint, isPressed, ref wasMousePressed);
    }

    private void HandlePointerInput(Vector2 screenPoint, bool isPressed, ref bool wasPressed)
    {
        Vector3 worldPoint = ScreenToWorldPoint(screenPoint);
        if (isPressed && !wasPressed)
        {
            if (CanStartDrag(worldPoint))
            {
                isDragging = true;
                HandleDragStarted();
            }
        }
        else if (isPressed && wasPressed)
        {
            if (!isDragging && CanStartDrag(worldPoint))
            {
                isDragging = true;
                HandleDragStarted();
            }

            if (isDragging)
            {
                DragTo(worldPoint);
            }
        }
        else if (!isPressed && wasPressed)
        {
            if (isDragging)
            {
                isDragging = false;
                DropDown();
            }
        }

        wasPressed = isPressed;
    }

    private Vector3 ScreenToWorldPoint(Vector2 screenPoint)
    {
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, zDepthFromCamera));
        worldPosition.z = transform.position.z;
        return worldPosition;
    }

    private bool CanStartDrag(Vector3 worldPoint)
    {
        if (useWideInputZone && IsPointerInsideWideInputZone(worldPoint))
        {
            return true;
        }

        if (cachedCollider2D == null)
        {
            return true;
        }

        return cachedCollider2D.OverlapPoint(worldPoint);
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
        return (Vector2)transform.position + inputZoneOffset;
    }

    private float GetLockedDragY()
    {
        return ShouldUseWaitingPositionY() ? waitingPosition.y : fixedY;
    }

    private bool ShouldUseWaitingPositionY()
    {
        return useWaitingPositionY || Mathf.Approximately(fixedY, 0f);
    }

    private void DragTo(Vector3 worldPoint)
    {
        float clampedX = Mathf.Clamp(worldPoint.x, minX, maxX);
        float targetY = lockYPosition ? GetLockedDragY() : worldPoint.y;
        Vector3 targetPosition = new Vector3(clampedX, targetY, transform.position.z);

        if (guideObject != null && guideObject.activeSelf && !guideObject.transform.IsChildOf(transform))
        {
            guideObject.transform.position = targetPosition;
        }

        if (cachedRigidbody2D == null)
        {
            transform.position = targetPosition;
            return;
        }

        cachedRigidbody2D.linearVelocity = Vector2.zero;
        cachedRigidbody2D.angularVelocity = 0f;
        cachedRigidbody2D.position = new Vector2(targetPosition.x, targetPosition.y);
    }

    private void DropDown()
    {
        SetGuideVisible(false);

        if (cachedRigidbody2D != null)
        {
            cachedRigidbody2D.linearVelocity = Vector2.zero;
            cachedRigidbody2D.angularVelocity = 0f;

            if (syncPhysicsBeforeDrop)
            {
                Physics2D.SyncTransforms();
            }
        }

        isDropped = true;
        remainingCollisionEraseCount = cardManager != null ? cardManager.ConsumeCollisionEraseCount() : 0;
        ApplyCollisionState();
        SC_ComboManager.NotifyShotStartedGlobal();

        if (cachedRigidbody2D != null)
        {
            cachedRigidbody2D.gravityScale = ResolveActiveGravityScale();
            cachedRigidbody2D.linearVelocity = GetFinalDropVelocity();
        }

        if (cardManager != null && cardManager.IsShrinkShotActive())
        {
            cardManager.ConsumeShrinkShot();
        }

        if (cardManager != null && cardManager.IsAttackQueueSpeedBonusActive())
        {
            cardManager.ConsumeAttackQueueSpeedBonusShot();
        }
    }

    public void SetShrinkShotVisual(bool shouldShrink)
    {
        if (shouldShrink)
        {
            if (!isShrinkShotVisualApplied)
            {
                defaultCharacterScale = transform.localScale;
            }

            transform.localScale = defaultCharacterScale * ShrinkShotScaleMultiplier;
            isShrinkShotVisualApplied = true;
            return;
        }

        if (!isShrinkShotVisualApplied)
        {
            return;
        }

        transform.localScale = defaultCharacterScale;
        isShrinkShotVisualApplied = false;
    }

    public void CancelInputAndReset()
    {
        if (isDropped)
        {
            return;
        }

        CancelDragAndResetToWaitingPosition(true);
        wasMousePressed = false;
        wasTouchPressed = false;
        suppressDragUntilPointerReleased = false;
    }

    public void CancelDragAndSuppressUntilRelease()
    {
        CancelDragAndResetToWaitingPosition(false);
        suppressDragUntilPointerReleased = true;
    }

    public void CancelInputAndSuppressUntilRelease()
    {
        CancelDragAndSuppressUntilRelease();
    }

    public void SetShrinkVisual(bool shouldShrink)
    {
        SetShrinkShotVisual(shouldShrink);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDropped || collision == null)
        {
            return;
        }

        TryEraseCollidedCharacter(collision.collider);
    }

    private void CancelDragAndResetToWaitingPosition(bool resetEvenWhenNotDragging)
    {
        if (!resetEvenWhenNotDragging && !isDragging)
        {
            return;
        }

        isDragging = false;
        SetGuideVisible(false);

        if (cachedRigidbody2D != null)
        {
            cachedRigidbody2D.linearVelocity = Vector2.zero;
            cachedRigidbody2D.angularVelocity = 0f;
            cachedRigidbody2D.position = waitingPosition;
            return;
        }

        transform.position = waitingPosition;
    }

    private float ResolveActiveGravityScale()
    {
        if (defaultGravityScale > 0f)
        {
            return defaultGravityScale;
        }

        if (cachedRigidbody2D != null && cachedRigidbody2D.gravityScale > 0f)
        {
            return cachedRigidbody2D.gravityScale;
        }

        return 1f;
    }

    private Vector2 GetFinalDropVelocity()
    {
        Vector2 baseVelocity = dropVelocity.sqrMagnitude > 0f ? dropVelocity : Vector2.down * Mathf.Max(0f, dropSpeed);
        float poweredDropBonus = cardManager != null && cardManager.IsAttackQueueSpeedBonusActive()
            ? PoweredDropSpeedBonus
            : 0f;

        if (baseVelocity.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector2.down * poweredDropBonus;
        }

        return baseVelocity.normalized * Mathf.Max(0f, baseVelocity.magnitude + poweredDropBonus);
    }

    private bool TryEraseCollidedCharacter(Collider2D otherCollider)
    {
        if (remainingCollisionEraseCount <= 0 || otherCollider == null)
        {
            return false;
        }

        SC_DropCharacterController targetDropController = otherCollider.GetComponentInParent<SC_DropCharacterController>();
        if (targetDropController != null && targetDropController != this)
        {
            EraseCharacter(targetDropController.gameObject);
            remainingCollisionEraseCount--;
            return true;
        }

        SC_CharacterPresenter targetPresenter = otherCollider.GetComponentInParent<SC_CharacterPresenter>();
        if (targetPresenter == null || targetPresenter.gameObject == gameObject)
        {
            return false;
        }

        EraseCharacter(targetPresenter.gameObject);
        remainingCollisionEraseCount--;
        return true;
    }

    private static void EraseCharacter(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        IFieldCharacterRuntime runtime = targetObject.GetComponent<IFieldCharacterRuntime>();
        if (runtime != null && runtime.IsWaiting)
        {
            runtime.CancelInputAndReset();
        }

        Destroy(targetObject);
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
                    transform.lossyScale.y != 0f ? -guideOriginalLocalScale.y / transform.lossyScale.y : -guideOriginalLocalScale.y,
                    transform.lossyScale.z != 0f ? guideOriginalLocalScale.z / transform.lossyScale.z : guideOriginalLocalScale.z);
            }
            else
            {
                guideObject.transform.position = transform.position;
                guideObject.transform.localScale = new Vector3(guideOriginalLocalScale.x, -guideOriginalLocalScale.y, guideOriginalLocalScale.z);
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
        SetDragArrowVisible(dragArrowRightObject, DragArrowRightRootName, isVisible);
        SetDragArrowVisible(dragArrowLeftObject, DragArrowLeftRootName, isVisible);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        EnsureDragArrowReferences();
        RefreshDragArrowVisibility();
        SetGuideVisible(false);
    }

    private void SetDragArrowVisible(GameObject arrowObject, string fallbackName, bool isVisible)
    {
        GameObject targetObject = arrowObject != null ? arrowObject : FindSceneObjectByExactName(fallbackName);
        if (targetObject != null)
        {
            targetObject.SetActive(isVisible);
        }
    }

    private void ApplyCollisionState()
    {
        if (!disableCollisionBeforeDrop || cachedCollider2D == null)
        {
            return;
        }

        cachedCollider2D.isTrigger = !isDropped;
    }

    private void EnsureReferences()
    {
        if (cachedRigidbody2D == null)
        {
            cachedRigidbody2D = GetComponent<Rigidbody2D>();
        }

        if (cachedCollider2D == null)
        {
            cachedCollider2D = GetComponent<Collider2D>();
        }
    }

    private void EnsureGuideReferences()
    {
        if (guideObject == null)
        {
            Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < childTransforms.Length; i++)
            {
                Transform childTransform = childTransforms[i];
                if (childTransform != null && childTransform.name == "OBJ_Guide")
                {
                    guideObject = childTransform.gameObject;
                    break;
                }
            }
        }

        if (guideObject != null)
        {
            guideOriginalLocalScale = guideObject.transform.localScale;
        }
    }

    private void EnsureDragArrowReferences()
    {
        if (dragArrowRightObject == null)
        {
            dragArrowRightObject = FindSceneObjectByExactName(DragArrowRightRootName);
        }

        if (dragArrowLeftObject == null)
        {
            dragArrowLeftObject = FindSceneObjectByExactName(DragArrowLeftRootName);
        }
    }

    private void EnsureCardManagerReference()
    {
        if (cardManager == null)
        {
            cardManager = FindAnyObjectByType<SC_CardManager>();
        }
    }

    private bool IsInputBlockedByPopup()
    {
        ResolvePopupReferences();

        return SC_BattleRuntimeUtility.IsBattleInputBlocked(battleManager, finalMergePopup, clearPopup);
    }

    private void ResolvePopupReferences()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (finalMergePopup == null)
        {
            finalMergePopup = FindAnyObjectByType<SC_FinalMergePopup>();
        }

        if (clearPopup == null)
        {
            clearPopup = FindAnyObjectByType<SC_ClearPopup>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!useWideInputZone)
        {
            return;
        }

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);
        Vector2 zoneCenter = GetInputZoneCenter();
        Vector3 zoneSize = new Vector3(inputZoneWidth, inputZoneHeight, 0f);
        Gizmos.DrawWireCube(zoneCenter, zoneSize);
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
}
