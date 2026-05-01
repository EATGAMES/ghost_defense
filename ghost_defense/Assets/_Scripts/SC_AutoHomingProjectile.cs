using UnityEngine;

public class SC_AutoHomingProjectile : MonoBehaviour
{
    [Tooltip("추적할 대상 태그")]
    [SerializeField] private string targetTag = "Monster";

    [Tooltip("발사체 이동 속도")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("발사체 최대 생존 시간(초)")]
    [SerializeField] private float lifeTime = 8f;

    [Tooltip("타겟 재탐색 주기(초)")]
    [SerializeField] private float retargetInterval = 0.2f;

    private float lifeTimer;
    private float retargetTimer;
    private Transform currentTarget;

    private void OnEnable()
    {
        lifeTimer = lifeTime;
        retargetTimer = 0f;
        currentTarget = null;
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        retargetTimer -= Time.deltaTime;
        if (currentTarget == null || retargetTimer <= 0f)
        {
            currentTarget = FindNearestTarget();
            retargetTimer = retargetInterval;
        }

        MoveTowardTarget();
    }

    public void Initialize(float speed, string monsterTag = "Monster")
    {
        moveSpeed = Mathf.Max(0f, speed);
        if (!string.IsNullOrWhiteSpace(monsterTag))
        {
            targetTag = monsterTag;
        }
    }

    private void MoveTowardTarget()
    {
        if (currentTarget == null)
        {
            return;
        }

        Vector3 targetPosition = currentTarget.position;
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        transform.position = nextPosition;
    }

    private Transform FindNearestTarget()
    {
        if (string.IsNullOrEmpty(targetTag))
        {
            return null;
        }

        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
        if (targets == null || targets.Length == 0)
        {
            return null;
        }

        Transform nearest = null;
        float nearestSqrDistance = float.MaxValue;
        Vector3 currentPosition = transform.position;

        for (int i = 0; i < targets.Length; i++)
        {
            GameObject candidate = targets[i];
            if (candidate == null)
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - currentPosition).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = candidate.transform;
            }
        }

        return nearest;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsTargetCollider(other))
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsTargetCollider(collision.collider))
        {
            Destroy(gameObject);
        }
    }

    private bool IsTargetCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag(targetTag))
        {
            return true;
        }

        Transform otherTransform = other.transform;
        if (otherTransform.parent != null && otherTransform.parent.CompareTag(targetTag))
        {
            return true;
        }

        return otherTransform.root != null && otherTransform.root.CompareTag(targetTag);
    }
}
