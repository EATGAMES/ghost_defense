using UnityEngine;

public class SC_CharacterMergeController : MonoBehaviour
{
    [Tooltip("이 캐릭터 프리팹 원본")]
    [SerializeField] private GameObject characterPrefab;

    [Tooltip("합체 후 생성 오브젝트의 부모")]
    [SerializeField] private Transform spawnParent;

    [Tooltip("같은 캐릭터 합체를 처리할 프리젠터")]
    [SerializeField] private SC_CharacterPresenter presenter;

    [Tooltip("합체 후 상속 속도 배율")]
    [SerializeField] private float mergeSpeedMultiplier = 0.4f;

    [Tooltip("합체 후 상속 속도의 최대값(0 이하면 제한 없음)")]
    [SerializeField] private float maxInheritedSpeed = 5f;

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

        if (characterPrefab == null)
        {
            characterPrefab = gameObject;
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
        if (otherMerge == null || otherMerge.isMerged)
        {
            return;
        }

        if (otherMerge == this)
        {
            return;
        }

        if (presenter == null || otherMerge.presenter == null)
        {
            return;
        }

        SO_CharacterData myData = presenter.CharacterData;
        SO_CharacterData otherData = otherMerge.presenter.CharacterData;
        if (myData == null || otherData == null)
        {
            return;
        }

        if (myData.CharacterKind != otherData.CharacterKind)
        {
            return;
        }

        if (myData.CharacterGrade != otherData.CharacterGrade)
        {
            return;
        }

        SO_CharacterData nextData = myData.NextGradeCharacterData;
        if (nextData == null)
        {
            return;
        }

        Rigidbody2D myRb2D = GetComponent<Rigidbody2D>();
        Rigidbody2D otherRb2D = otherMerge.GetComponent<Rigidbody2D>();
        Vector2 inheritedVelocity = Vector2.zero;

        if (myRb2D != null && otherRb2D != null)
        {
            inheritedVelocity = myRb2D.linearVelocity.magnitude >= otherRb2D.linearVelocity.magnitude
                ? myRb2D.linearVelocity
                : otherRb2D.linearVelocity;
        }
        else if (myRb2D != null)
        {
            inheritedVelocity = myRb2D.linearVelocity;
        }
        else if (otherRb2D != null)
        {
            inheritedVelocity = otherRb2D.linearVelocity;
        }

        DisablePhysicsForMerge(this);
        DisablePhysicsForMerge(otherMerge);

        isMerged = true;
        otherMerge.isMerged = true;

        Vector3 spawnPosition = (transform.position + otherMerge.transform.position) * 0.5f;
        Quaternion spawnRotation = Quaternion.identity;
        Transform parent = spawnParent != null ? spawnParent : transform.parent;

        GameObject mergedCharacter = Instantiate(characterPrefab, spawnPosition, spawnRotation, parent);
        SC_CharacterPresenter mergedPresenter = mergedCharacter.GetComponent<SC_CharacterPresenter>();
        if (mergedPresenter != null)
        {
            mergedPresenter.SetCharacterData(nextData, true);
        }

        Rigidbody2D mergedRb2D = mergedCharacter.GetComponent<Rigidbody2D>();
        if (mergedRb2D != null)
        {
            mergedRb2D.simulated = true;

            Vector2 mergedVelocity = inheritedVelocity * mergeSpeedMultiplier;
            if (maxInheritedSpeed > 0f)
            {
                mergedVelocity = Vector2.ClampMagnitude(mergedVelocity, maxInheritedSpeed);
            }

            mergedRb2D.linearVelocity = mergedVelocity;
        }

        EnablePhysicsForMergedCharacter(mergedCharacter);

        Destroy(otherMerge.gameObject);
        Destroy(gameObject);
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
}

