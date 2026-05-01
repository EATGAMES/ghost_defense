using UnityEngine;

public class SC_AutoHomingProjectile : MonoBehaviour
{
    [Tooltip("추적할 대상 태그")]
    [SerializeField] private string targetTag = "Monster";

    [Tooltip("발사체 이동 속도")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("발사체 1회 공격력")]
    [SerializeField] private float damage = 1f;

    [Tooltip("발사체 최대 생존 시간(초)")]
    [SerializeField] private float lifeTime = 8f;

    private float lifeTimer;
    private Transform currentTarget;
    private SC_MonsterHealth currentTargetHealth;
    private bool hasLockedTarget;

    private void OnEnable()
    {
        lifeTimer = lifeTime;
        currentTarget = null;
        currentTargetHealth = null;
        hasLockedTarget = false;
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // 발사 직후 한 번만 가장 가까운 타겟을 고정한다.
        if (!hasLockedTarget)
        {
            currentTarget = FindNearestTarget(out currentTargetHealth);
            hasLockedTarget = true;

            if (currentTarget == null || currentTargetHealth == null)
            {
                Destroy(gameObject);
                return;
            }
        }

        // 고정한 타겟이 사라지거나 죽으면 재탐색 없이 즉시 소멸한다.
        if (currentTarget == null || currentTargetHealth == null || currentTargetHealth.CurrentHp <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        MoveTowardTarget();
    }

    public void Initialize(float speed, string monsterTag = "Monster", float projectileDamage = 1f)
    {
        moveSpeed = Mathf.Max(0f, speed);
        damage = Mathf.Max(0f, projectileDamage);

        if (!string.IsNullOrWhiteSpace(monsterTag))
        {
            targetTag = monsterTag;
        }
    }

    private void MoveTowardTarget()
    {
        Vector3 targetPosition = currentTarget.position;
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        transform.position = nextPosition;
    }

    private Transform FindNearestTarget(out SC_MonsterHealth nearestHealth)
    {
        nearestHealth = null;

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
        SC_MonsterHealth nearestMonsterHealth = null;
        float nearestSqrDistance = float.MaxValue;
        Vector3 currentPosition = transform.position;

        for (int i = 0; i < targets.Length; i++)
        {
            GameObject candidate = targets[i];
            if (candidate == null)
            {
                continue;
            }

            SC_MonsterHealth candidateHealth = candidate.GetComponent<SC_MonsterHealth>();
            if (candidateHealth == null)
            {
                candidateHealth = candidate.GetComponentInChildren<SC_MonsterHealth>();
            }

            if (candidateHealth == null || candidateHealth.CurrentHp <= 0f)
            {
                continue;
            }

            Transform candidateTarget = ResolveTargetTransform(candidate);
            if (candidateTarget == null)
            {
                continue;
            }

            float sqrDistance = (candidateTarget.position - currentPosition).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = candidateTarget;
                nearestMonsterHealth = candidateHealth;
            }
        }

        nearestHealth = nearestMonsterHealth;
        return nearest;
    }

    private Transform ResolveTargetTransform(GameObject candidate)
    {
        if (candidate == null)
        {
            return null;
        }

        Rigidbody2D childRigidbody = candidate.GetComponentInChildren<Rigidbody2D>();
        if (childRigidbody != null)
        {
            return childRigidbody.transform;
        }

        SpriteRenderer childSpriteRenderer = candidate.GetComponentInChildren<SpriteRenderer>();
        if (childSpriteRenderer != null)
        {
            return childSpriteRenderer.transform;
        }

        return candidate.transform;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryProcessTargetHit(other))
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (TryProcessTargetHit(collision.collider))
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

    private bool TryProcessTargetHit(Collider2D other)
    {
        if (!IsTargetCollider(other))
        {
            return false;
        }

        SC_MonsterHealth monsterHealth = other.GetComponent<SC_MonsterHealth>();
        if (monsterHealth == null)
        {
            monsterHealth = other.GetComponentInParent<SC_MonsterHealth>();
        }

        if (monsterHealth != null)
        {
            monsterHealth.TakeDamage(damage);
        }

        return true;
    }
}
