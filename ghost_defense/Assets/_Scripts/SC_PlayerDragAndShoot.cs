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

    [Tooltip("이 속도 이하로 떨어지면 정지 처리")]
    [SerializeField] private float stopSpeedThreshold = 0.2f;

    private Camera mainCamera;
    private Rigidbody2D rb2D;
    private Collider2D col2D;
    private bool isDragging;
    private bool isShot;
    private float zDepthFromCamera;
    private bool wasMousePressed;
    private bool wasTouchPressed;

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

        rb2D.linearVelocity = Vector2.MoveTowards(velocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
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
        isShot = true;

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

        if (collision.contactCount > 0)
        {
            Vector2 normal = collision.GetContact(0).normal;
            float normalSpeed = Vector2.Dot(velocity, normal);
            Vector2 normalVelocity = normal * normalSpeed;
            Vector2 tangentVelocity = velocity - normalVelocity;
            velocity = normalVelocity + tangentVelocity * sideSlipDamping;
        }

        rb2D.linearVelocity = velocity;

        if (rb2D.linearVelocity.magnitude <= stopSpeedThreshold)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }
    }
}
