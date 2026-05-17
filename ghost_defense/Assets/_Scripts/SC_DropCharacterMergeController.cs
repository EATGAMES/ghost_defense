using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_DropCharacterMergeController : MonoBehaviour
{
    [Tooltip("머지 결과로 생성할 드롭 캐릭터 프리팹입니다. 비워두면 자기 자신 프리팹을 사용합니다.")]
    [SerializeField] private GameObject mergeObjectPrefab;

    [Tooltip("머지 결과 오브젝트를 생성할 부모 Transform입니다.")]
    [SerializeField] private Transform spawnParent;

    [Tooltip("현재 드롭 캐릭터의 단계와 이미지를 표시하는 프레젠터입니다.")]
    [SerializeField] private SC_CharacterPresenter presenter;

    [Tooltip("머지 성공 후 공격 요청을 전달할 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("겹침 판정에 허용할 추가 거리입니다. 0이면 실제 접촉할 때만 머지됩니다.")]
    [SerializeField] private float mergeContactTolerance = 0f;

    [Tooltip("10단계 최종 머지 오브젝트를 제거하기 전까지의 지연 시간(초)입니다.")]
    [SerializeField] private float finalMergeCleanupDelay = 0.15f;

    [Tooltip("10단계 최종 머지 팝업이 뜨기 전 대기 시간(초)입니다.")]
    [SerializeField] private float finalMergePopupDelay = 0.3f;

    [Tooltip("10단계 최종 머지 연출 팝업입니다.")]
    [SerializeField] private SC_FinalMergePopup finalMergePopup;

    private bool isMerged;
    private bool isFinalMergeSequenceRunning;
    private float pendingComboDamageMultiplier = 1f;

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

        if (finalMergePopup == null)
        {
            finalMergePopup = battleManager != null ? battleManager.GetFinalMergePopup() : FindAnyObjectByType<SC_FinalMergePopup>();
        }

        if (mergeObjectPrefab == null)
        {
            mergeObjectPrefab = gameObject;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryMerge(collision.collider, true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryMerge(other);
    }

    private bool TryMerge(Collider2D otherCollider, bool skipTouchCheck = false)
    {
        if (isMerged || otherCollider == null)
        {
            return false;
        }

        SC_DropCharacterMergeController otherMerge = otherCollider.GetComponent<SC_DropCharacterMergeController>();
        if (otherMerge == null)
        {
            otherMerge = otherCollider.GetComponentInParent<SC_DropCharacterMergeController>();
        }

        if (otherMerge == null || otherMerge == this || otherMerge.isMerged)
        {
            return false;
        }

        if (presenter == null || otherMerge.presenter == null)
        {
            return false;
        }

        SC_DropCharacterController myDrop = GetComponent<SC_DropCharacterController>();
        SC_DropCharacterController otherDrop = otherMerge.GetComponent<SC_DropCharacterController>();
        if (myDrop == null || otherDrop == null || !myDrop.IsActiveDrop || !otherDrop.IsActiveDrop)
        {
            return false;
        }

        int myGrade = presenter.MergeGrade;
        int otherGrade = otherMerge.presenter.MergeGrade;
        if (myGrade != otherGrade)
        {
            return false;
        }

        if (!skipTouchCheck && !IsActuallyTouching(otherMerge))
        {
            return false;
        }

        int nextGrade = Mathf.Clamp(myGrade + 1, 1, 10);
        DisableForMerge(this);
        DisableForMerge(otherMerge);

        isMerged = true;
        otherMerge.isMerged = true;

        Vector3 spawnPosition = (transform.position + otherMerge.transform.position) * 0.5f;
        Transform parent = spawnParent != null ? spawnParent : transform.parent;
        GameObject mergedObject = Instantiate(mergeObjectPrefab, spawnPosition, Quaternion.identity, parent);
        ConfigureMergedObject(mergedObject, nextGrade);

        SC_ComboManager.ComboMergeResult comboMergeResult = SC_ComboManager.NotifyMergeCreatedGlobal();
        ReportMergedGradeToPreviewUI(nextGrade);

        if (nextGrade >= 10)
        {
            DisablePhysicsForFinalMerge(mergedObject);

            SC_DropCharacterMergeController mergedMergeController = mergedObject.GetComponent<SC_DropCharacterMergeController>();
            if (mergedMergeController != null)
            {
                mergedMergeController.SetPendingComboDamageMultiplier(comboMergeResult.DamageMultiplier);
                mergedMergeController.BeginFinalMergeSequence();
            }
            else
            {
                Destroy(mergedObject, Mathf.Max(0f, finalMergeCleanupDelay));
            }
        }
        else
        {
            NotifyBattleMerge(nextGrade, comboMergeResult.DamageMultiplier);
        }

        Destroy(otherMerge.gameObject);
        Destroy(gameObject);
        return true;
    }

    public void BeginFinalMergeSequence()
    {
        if (isFinalMergeSequenceRunning)
        {
            return;
        }

        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (battleManager != null)
        {
            battleManager.NotifyCreatedGrade10ThisBattle();
        }

        isFinalMergeSequenceRunning = true;
        StartCoroutine(CoHandleFinalMergeSequence());
    }

    public void SetPendingComboDamageMultiplier(float damageMultiplier)
    {
        pendingComboDamageMultiplier = Mathf.Max(1f, damageMultiplier);
    }

    private void ConfigureMergedObject(GameObject mergedObject, int grade)
    {
        if (mergedObject == null)
        {
            return;
        }

        SC_CharacterPresenter mergedPresenter = mergedObject.GetComponent<SC_CharacterPresenter>();
        if (mergedPresenter != null)
        {
            mergedPresenter.Configure(grade, true, true);
        }

        SC_DropCharacterController mergedDrop = mergedObject.GetComponent<SC_DropCharacterController>();
        Rigidbody2D mergedRb2D = mergedObject.GetComponent<Rigidbody2D>();
        if (mergedRb2D != null)
        {
            mergedRb2D.angularVelocity = 0f;
        }

        EnablePhysicsForMergedObject(mergedObject);

        if (mergedDrop != null)
        {
            mergedDrop.SetDropVelocity(Vector2.zero);
            mergedDrop.SetDropActive(true);
        }
    }

    private void NotifyBattleMerge(int mergedGrade, float comboDamageMultiplier)
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (battleManager == null)
        {
            return;
        }

        battleManager.NotifyMergeAttack(mergedGrade, comboDamageMultiplier);
    }

    private IEnumerator CoHandleFinalMergeSequence()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, finalMergePopupDelay));

        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (finalMergePopup == null)
        {
            finalMergePopup = battleManager != null ? battleManager.GetFinalMergePopup() : FindAnyObjectByType<SC_FinalMergePopup>();
        }

        if (finalMergePopup != null)
        {
            Sprite finalMergeSprite = ResolveFinalMergePopupSprite();
            finalMergePopup.SetCharacterSprite(finalMergeSprite);
            yield return finalMergePopup.CoOpenAndWait();
        }

        yield return new WaitForSeconds(Mathf.Max(0f, finalMergeCleanupDelay));

        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (battleManager != null)
        {
            battleManager.NotifyFinalMergeAttack(10, pendingComboDamageMultiplier);
        }

        Destroy(gameObject);
    }

    private Sprite ResolveFinalMergePopupSprite()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (battleManager == null)
        {
            return null;
        }

        return battleManager.GetFieldSpriteForGrade(10);
    }

    private bool IsActuallyTouching(SC_DropCharacterMergeController otherMerge)
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

    private static void DisableForMerge(SC_DropCharacterMergeController mergeController)
    {
        if (mergeController == null)
        {
            return;
        }

        SC_DropCharacterController dropController = mergeController.GetComponent<SC_DropCharacterController>();
        if (dropController != null)
        {
            dropController.SetDropActive(false);
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

    private static void DisablePhysicsForFinalMerge(GameObject mergedObject)
    {
        if (mergedObject == null)
        {
            return;
        }

        SC_DropCharacterController dropController = mergedObject.GetComponent<SC_DropCharacterController>();
        if (dropController != null)
        {
            dropController.SetDropActive(false);
        }

        Rigidbody2D rb2D = mergedObject.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            rb2D.simulated = false;
        }

        Collider2D[] colliders = mergedObject.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private static void EnablePhysicsForMergedObject(GameObject mergedObject)
    {
        if (mergedObject == null)
        {
            return;
        }

        Rigidbody2D rb2D = mergedObject.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.simulated = true;
        }

        Collider2D[] colliders = mergedObject.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = true;
            }
        }
    }

    private static void ReportMergedGradeToPreviewUI(int mergedGrade)
    {
        SC_CharacterGradePreviewUI previewUI = FindAnyObjectByType<SC_CharacterGradePreviewUI>();
        if (previewUI != null)
        {
            previewUI.ReportReachedGrade(mergedGrade);
        }
    }
}
