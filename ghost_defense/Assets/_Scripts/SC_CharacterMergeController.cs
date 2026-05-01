using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SC_CharacterMergeController : MonoBehaviour
{
    [Tooltip("머지 결과로 생성할 프리팹입니다. 비워두면 자기 자신 프리팹을 재사용합니다.")]
    [SerializeField] private GameObject mergeObjectPrefab;

    [Tooltip("머지 결과 오브젝트를 생성할 부모 Transform입니다.")]
    [SerializeField] private Transform spawnParent;

    [Tooltip("현재 머지 오브젝트의 단계와 이미지를 표시하는 프레젠터입니다.")]
    [SerializeField] private SC_CharacterPresenter presenter;

    [Tooltip("머지 성공 시 공격 큐를 전달할 배틀 매니저입니다.")]
    [FormerlySerializedAs("waveManager")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("머지 후 이어받을 속도에 곱할 배율입니다.")]
    [SerializeField] private float mergeSpeedMultiplier = 0.6667f;

    [Tooltip("머지 후 이어받을 최대 속도입니다. 0 이하면 제한하지 않습니다.")]
    [SerializeField] private float maxInheritedSpeed = 0f;

    [Tooltip("겹침 판정 시 허용할 추가 거리입니다. 0이면 실제 접촉일 때만 머지됩니다.")]
    [SerializeField] private float mergeContactTolerance = 0f;

    [Tooltip("10단계 완성 오브젝트를 제거하기 전까지의 지연 시간(초)입니다.")]
    [SerializeField] private float finalMergeCleanupDelay = 0.15f;

    private bool isMerged;

    private void Reset()
    {
        presenter = GetComponent<SC_CharacterPresenter>();
    }

    private void Awake()
    {
        if (presenter == null)
        {
            presenter = GetComponent<SC_CharacterPresenter>();
        }

        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (mergeObjectPrefab == null)
        {
            mergeObjectPrefab = gameObject;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryMerge(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryMerge(other);
    }

    private void TryMerge(Collider2D otherCollider)
    {
        if (isMerged || otherCollider == null)
        {
            return;
        }

        SC_CharacterMergeController otherMerge = otherCollider.GetComponent<SC_CharacterMergeController>();
        if (otherMerge == null || otherMerge.isMerged || otherMerge == this)
        {
            return;
        }

        if (presenter == null || otherMerge.presenter == null)
        {
            return;
        }

        SC_PlayerDragAndShoot myShoot = GetComponent<SC_PlayerDragAndShoot>();
        SC_PlayerDragAndShoot otherShoot = otherMerge.GetComponent<SC_PlayerDragAndShoot>();
        if (myShoot == null || otherShoot == null || !myShoot.IsShot || !otherShoot.IsShot)
        {
            return;
        }

        int myGrade = presenter.MergeGrade;
        int otherGrade = otherMerge.presenter.MergeGrade;
        if (myGrade != otherGrade)
        {
            return;
        }

        if (!IsActuallyTouching(otherMerge))
        {
            return;
        }

        int nextGrade = Mathf.Clamp(myGrade + 1, 1, 10);
        Vector2 inheritedVelocity = CalculateInheritedVelocity(GetComponent<Rigidbody2D>(), otherMerge.GetComponent<Rigidbody2D>());

        DisablePhysicsForMerge(this);
        DisablePhysicsForMerge(otherMerge);

        isMerged = true;
        otherMerge.isMerged = true;

        Vector3 spawnPosition = (transform.position + otherMerge.transform.position) * 0.5f;
        Transform parent = spawnParent != null ? spawnParent : transform.parent;
        GameObject mergedObject = Instantiate(mergeObjectPrefab, spawnPosition, Quaternion.identity, parent);

        SC_CharacterPresenter mergedPresenter = mergedObject.GetComponent<SC_CharacterPresenter>();
        if (mergedPresenter != null)
        {
            mergedPresenter.Configure(nextGrade, true);
        }

        Rigidbody2D mergedRb2D = mergedObject.GetComponent<Rigidbody2D>();
        if (mergedRb2D != null)
        {
            Vector2 mergedVelocity = inheritedVelocity * mergeSpeedMultiplier;
            if (maxInheritedSpeed > 0f)
            {
                mergedVelocity = Vector2.ClampMagnitude(mergedVelocity, maxInheritedSpeed);
            }

            mergedRb2D.simulated = true;
            mergedRb2D.linearVelocity = mergedVelocity;
        }

        SC_PlayerDragAndShoot mergedShoot = mergedObject.GetComponent<SC_PlayerDragAndShoot>();
        if (mergedShoot != null)
        {
            mergedShoot.SetShotState(true);
        }

        EnablePhysicsForMergedCharacter(mergedObject);
        NotifyMergeCreated(nextGrade);

        if (nextGrade >= 10)
        {
            DisablePhysicsForFinalMerge(mergedObject);
            Destroy(mergedObject, Mathf.Max(0f, finalMergeCleanupDelay));
        }

        Destroy(otherMerge.gameObject);
        Destroy(gameObject);
    }

    private void NotifyMergeCreated(int mergedGrade)
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (battleManager == null)
        {
            return;
        }

        battleManager.NotifyMergeAttack(mergedGrade);
    }

    private bool IsActuallyTouching(SC_CharacterMergeController otherMerge)
    {
        if (otherMerge == null)
        {
            return false;
        }

        Collider2D myCollider = GetComponent<Collider2D>();
        Collider2D otherCollider = otherMerge.GetComponent<Collider2D>();
        if (myCollider == null || otherCollider == null)
        {
            return false;
        }

        ColliderDistance2D colliderDistance = myCollider.Distance(otherCollider);
        return colliderDistance.distance <= Mathf.Max(0f, mergeContactTolerance);
    }

    private static Vector2 CalculateInheritedVelocity(Rigidbody2D myRb2D, Rigidbody2D otherRb2D)
    {
        if (myRb2D != null && otherRb2D != null)
        {
            bool isMyFaster = myRb2D.linearVelocity.magnitude >= otherRb2D.linearVelocity.magnitude;
            Rigidbody2D fastRb2D = isMyFaster ? myRb2D : otherRb2D;
            Rigidbody2D upperRb2D = myRb2D.position.y >= otherRb2D.position.y ? myRb2D : otherRb2D;

            Vector2 direction = upperRb2D.linearVelocity.normalized;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = fastRb2D.linearVelocity.normalized;
            }

            float inheritedSpeed = fastRb2D.linearVelocity.magnitude;
            return direction * inheritedSpeed;
        }

        if (myRb2D != null)
        {
            return myRb2D.linearVelocity;
        }

        if (otherRb2D != null)
        {
            return otherRb2D.linearVelocity;
        }

        return Vector2.zero;
    }

    private static void DisablePhysicsForMerge(SC_CharacterMergeController mergeController)
    {
        if (mergeController == null)
        {
            return;
        }

        Rigidbody2D rb2D = mergeController.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            rb2D.simulated = false;
        }

        Collider2D[] colliders = mergeController.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private static void EnablePhysicsForMergedCharacter(GameObject mergedCharacter)
    {
        if (mergedCharacter == null)
        {
            return;
        }

        Rigidbody2D rb2D = mergedCharacter.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.simulated = true;
        }

        Collider2D[] colliders = mergedCharacter.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
        }
    }

    private static void DisablePhysicsForFinalMerge(GameObject mergedCharacter)
    {
        if (mergedCharacter == null)
        {
            return;
        }

        Rigidbody2D rb2D = mergedCharacter.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            rb2D.simulated = false;
        }

        Collider2D[] colliders = mergedCharacter.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }
}
