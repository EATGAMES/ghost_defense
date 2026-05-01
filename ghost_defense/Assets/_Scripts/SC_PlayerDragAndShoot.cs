using UnityEngine;
using UnityEngine.InputSystem;

public class SC_PlayerDragAndShoot : MonoBehaviour
{
    [Tooltip("드래그 가능한 최소 X 좌표(월드 좌표)")]
    [SerializeField] private float minX = -3.5f;

    [Tooltip("드래그 가능한 최대 X 좌표(월드 좌표)")]
    [SerializeField] private float maxX = 3.5f;

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

    private Camera mainCamera;
    private Rigidbody2D rb2D;
    private Collider2D col2D;
    private bool isDragging;
    private bool isShot;
    private float zDepthFromCamera;
    private bool wasMousePressed;
    private bool wasTouchPressed;
    private Vector3 dragStartPosition;
    private readonly Collider2D[] overlapResults = new Collider2D[16];

    public bool IsShot => isShot;

    private void Awake()
    {
        mainCamera = Camera.main;
        rb2D = GetComponent<Rigidbody2D>();
        col2D = GetComponent<Collider2D>();

        if (lockYPosition && Mathf.Approximately(fixedY, -7f))
        {
            fixedY = transform.position.y;
        }

        if (mainCamera != null)
        {
            zDepthFromCamera = Mathf.Abs(transform.position.z - mainCamera.transform.position.z);
        }

        ApplyCollisionState();
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
            if (IsPointerOverSelf(worldPoint))
            {
                isDragging = true;
                dragStartPosition = transform.position;
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
            if (IsPointerOverSelf(worldPoint))
            {
                isDragging = true;
                dragStartPosition = transform.position;
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

    private void DragTo(Vector3 worldPoint)
    {
        float clampedX = Mathf.Clamp(worldPoint.x, minX, maxX);
        float targetY = lockYPosition ? fixedY : worldPoint.y;
        transform.position = new Vector3(clampedX, targetY, transform.position.z);
    }

    private void ShootForward()
    {
        if (blockShootWhenOverlappingCharacter && IsShootBlockedByCharacter())
        {
            return;
        }

        SetShotState(true);

        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.up * shootSpeed;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb2D == null)
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
        ApplyCollisionState();
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
        int hitCount = Physics2D.OverlapBoxNonAlloc(transform.position, checkSize, 0f, overlapResults);
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

    private void OnDrawGizmosSelected()
    {
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
