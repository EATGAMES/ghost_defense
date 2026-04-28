using UnityEngine;
using UnityEngine.InputSystem;

public class SC_PlayerDragAndShoot : MonoBehaviour
{
    [Tooltip("드래그 가능한 최소 X 좌표(월드 좌표)")]
    [SerializeField] private float minX = -3.5f;

    [Tooltip("드래그 가능한 최대 X 좌표(월드 좌표)")]
    [SerializeField] private float maxX = 3.5f;

    [Tooltip("발사 시 위쪽(+Y) 속도")]
    [SerializeField] private float shootSpeed = 12f;

    [Tooltip("드래그 시 Y 위치를 고정할지 여부")]
    [SerializeField] private bool lockYPosition = true;

    [Tooltip("드래그 시 고정할 Y 좌표(비워두면 시작 위치 사용)")]
    [SerializeField] private float fixedY = -7f;

    private Camera mainCamera;
    private Rigidbody2D rb2D;
    private Rigidbody rb3D;
    private Collider2D col2D;
    private Collider col3D;
    private bool isDragging;
    private bool isShot;
    private float zDepthFromCamera;
    private bool useTransformMoveAfterShot;
    private bool wasMousePressed;
    private bool wasTouchPressed;

    private void Awake()
    {
        mainCamera = Camera.main;
        rb2D = GetComponent<Rigidbody2D>();
        rb3D = GetComponent<Rigidbody>();
        col2D = GetComponent<Collider2D>();
        col3D = GetComponent<Collider>();

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
        if (isShot || mainCamera == null)
        {
            if (isShot && useTransformMoveAfterShot)
            {
                transform.position += Vector3.up * shootSpeed * Time.deltaTime;
            }
            return;
        }

        HandleTouchInput();
        HandleMouseInput();
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
            if (IsPointerOverSelf(worldPoint, screenPoint))
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
            if (IsPointerOverSelf(worldPoint, screenPoint))
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

    private bool IsPointerOverSelf(Vector3 worldPoint, Vector2 screenPoint)
    {
        if (col2D != null)
        {
            return col2D.OverlapPoint(worldPoint);
        }

        if (col3D != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPoint);
            return col3D.Raycast(ray, out _, 1000f);
        }

        return true;
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
            return;
        }

        if (rb3D != null)
        {
            rb3D.linearVelocity = Vector3.up * shootSpeed;
            return;
        }

        useTransformMoveAfterShot = true;
    }
}
