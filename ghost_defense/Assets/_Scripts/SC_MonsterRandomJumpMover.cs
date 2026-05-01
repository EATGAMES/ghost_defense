using UnityEngine;

[DisallowMultipleComponent]
public class SC_MonsterRandomJumpMover : MonoBehaviour
{
    [Tooltip("실제로 이동시킬 타겟 Transform(비우면 자동 탐색)")]
    [SerializeField] private Transform moveTarget;

    [Tooltip("좌우 이동 기준점으로 사용할 시작 위치")]
    [SerializeField] private Vector3 startPosition;

    [Tooltip("시작 시 현재 위치를 기준점으로 다시 설정할지 여부")]
    [SerializeField] private bool useCurrentPositionAsStart = true;

    [Tooltip("기준점 X를 카메라 화면 정중앙으로 사용할지 여부")]
    [SerializeField] private bool useScreenCenterAsStartX = true;

    [Tooltip("좌우 이동 반경(기준점에서 좌우 최대 거리)")]
    [SerializeField] private float moveRange = 2.5f;

    [Tooltip("좌우 이동 최소 속도")]
    [SerializeField] private float minMoveSpeed = 1.2f;

    [Tooltip("좌우 이동 최대 속도")]
    [SerializeField] private float maxMoveSpeed = 2.4f;

    [Tooltip("이동 방향/속도를 다시 랜덤으로 뽑는 최소 간격(초)")]
    [SerializeField] private float minDirectionChangeInterval = 0.8f;

    [Tooltip("이동 방향/속도를 다시 랜덤으로 뽑는 최대 간격(초)")]
    [SerializeField] private float maxDirectionChangeInterval = 1.8f;

    [Tooltip("점프 최소 대기 시간(초)")]
    [SerializeField] private float minJumpInterval = 0.7f;

    [Tooltip("점프 최대 대기 시간(초)")]
    [SerializeField] private float maxJumpInterval = 1.6f;

    [Tooltip("점프 높이")]
    [SerializeField] private float jumpHeight = 0.7f;

    [Tooltip("점프 1회에 걸리는 시간(초)")]
    [SerializeField] private float jumpDuration = 0.45f;

    private float currentMoveSpeed;
    private int moveDirection = 1;
    private float directionChangeTimer;
    private float jumpTimer;
    private float jumpElapsed;
    private bool isJumping;

    private Rigidbody2D targetRigidbody2D;

    private void Awake()
    {
        if (moveTarget == null)
        {
            Rigidbody2D rb = GetComponentInChildren<Rigidbody2D>();
            moveTarget = rb != null ? rb.transform : transform;
        }

        targetRigidbody2D = moveTarget.GetComponent<Rigidbody2D>();

        if (useCurrentPositionAsStart)
        {
            startPosition = moveTarget.position;
        }

        if (useScreenCenterAsStartX)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                startPosition.x = mainCamera.transform.position.x;
            }
        }

        PickRandomMoveState();
        ResetDirectionChangeTimer();
        ResetJumpTimer();
    }

    private void Update()
    {
        if (moveTarget == null)
        {
            return;
        }

        UpdateJumpTimer();
    }

    private void FixedUpdate()
    {
        if (moveTarget == null)
        {
            return;
        }

        float nextX = CalculateNextX(Time.fixedDeltaTime);
        float nextY = CalculateNextY();
        MoveTarget(new Vector2(nextX, nextY));
    }

    private float CalculateNextX(float deltaTime)
    {
        directionChangeTimer -= deltaTime;
        if (directionChangeTimer <= 0f)
        {
            PickRandomMoveState();
            ResetDirectionChangeTimer();
        }

        float safeRange = Mathf.Max(0.01f, moveRange);
        float leftX = startPosition.x - safeRange;
        float rightX = startPosition.x + safeRange;
        float nextX = moveTarget.position.x + moveDirection * currentMoveSpeed * deltaTime;

        if (nextX <= leftX)
        {
            nextX = leftX;
            TurnImmediate(1);
        }
        else if (nextX >= rightX)
        {
            nextX = rightX;
            TurnImmediate(-1);
        }

        return nextX;
    }

    private float CalculateNextY()
    {
        if (!isJumping)
        {
            return startPosition.y;
        }

        float duration = Mathf.Max(0.05f, jumpDuration);
        float t = Mathf.Clamp01(jumpElapsed / duration);
        float yOffset = Mathf.Sin(t * Mathf.PI) * Mathf.Max(0f, jumpHeight);
        return startPosition.y + yOffset;
    }

    private void UpdateJumpTimer()
    {
        if (isJumping)
        {
            jumpElapsed += Time.deltaTime;
            if (jumpElapsed >= Mathf.Max(0.05f, jumpDuration))
            {
                isJumping = false;
                jumpElapsed = 0f;
                ResetJumpTimer();
            }

            return;
        }

        jumpTimer -= Time.deltaTime;
        if (jumpTimer <= 0f)
        {
            isJumping = true;
            jumpElapsed = 0f;
        }
    }

    private void MoveTarget(Vector2 nextPosition)
    {
        if (targetRigidbody2D != null)
        {
            targetRigidbody2D.MovePosition(nextPosition);
            return;
        }

        moveTarget.position = nextPosition;
    }

    private void TurnImmediate(int newDirection)
    {
        moveDirection = newDirection >= 0 ? 1 : -1;
        PickRandomSpeedOnly();
        ResetDirectionChangeTimer();
    }

    private void PickRandomMoveState()
    {
        moveDirection = Random.value < 0.5f ? -1 : 1;
        PickRandomSpeedOnly();
    }

    private void PickRandomSpeedOnly()
    {
        float minSpeed = Mathf.Max(0.01f, minMoveSpeed);
        float maxSpeed = Mathf.Max(minSpeed, maxMoveSpeed);
        currentMoveSpeed = Random.Range(minSpeed, maxSpeed);
    }

    private void ResetDirectionChangeTimer()
    {
        float minInterval = Mathf.Max(0.05f, minDirectionChangeInterval);
        float maxInterval = Mathf.Max(minInterval, maxDirectionChangeInterval);
        directionChangeTimer = Random.Range(minInterval, maxInterval);
    }

    private void ResetJumpTimer()
    {
        float minInterval = Mathf.Max(0.05f, minJumpInterval);
        float maxInterval = Mathf.Max(minInterval, maxJumpInterval);
        jumpTimer = Random.Range(minInterval, maxInterval);
    }
}
