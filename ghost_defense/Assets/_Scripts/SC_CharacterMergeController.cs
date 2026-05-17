using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SC_CharacterMergeController : MonoBehaviour
{
    [Tooltip("머지 결과로 생성할 프리팹입니다. 비워두면 자기 자신 프리팹을 사용합니다.")]
    [SerializeField] private GameObject mergeObjectPrefab;

    [Tooltip("머지 결과 오브젝트를 생성할 부모 Transform입니다.")]
    [SerializeField] private Transform spawnParent;

    [Tooltip("현재 머지 오브젝트의 단계와 이미지를 표시하는 프레젠터입니다.")]
    [SerializeField] private SC_CharacterPresenter presenter;

    [Tooltip("머지 성공 후 공격 요청을 전달할 배틀 매니저입니다.")]
    [FormerlySerializedAs("waveManager")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("머지 후 이어받을 속도에 곱할 배수입니다.")]
    [SerializeField] private float mergeSpeedMultiplier = 0.6667f;

    [Tooltip("머지 후 이어받을 최대 속도입니다. 0 이하면 제한하지 않습니다.")]
    [SerializeField] private float maxInheritedSpeed = 0f;

    [Tooltip("겹침 판정에 허용할 추가 거리입니다. 0이면 실제 접촉할 때만 머지됩니다.")]
    [SerializeField] private float mergeContactTolerance = 0f;

    [Tooltip("10단계 완성 오브젝트를 제거하기 전까지의 지연 시간(초)입니다.")]
    [SerializeField] private float finalMergeCleanupDelay = 0.15f;

    [Tooltip("10단계 최종 머지 팝업이 뜨기 전 대기 시간(초)입니다.")]
    [SerializeField] private float finalMergePopupDelay = 0.3f;

    [Tooltip("10단계 최종 머지 연출 팝업입니다.")]
    [SerializeField] private SC_FinalMergePopup finalMergePopup;

    [Tooltip("주변 밀치기 반경을 Circle Collider 2D 반지름 대비 몇 배로 사용할지 설정합니다.")]
    [FormerlySerializedAs("pushEffectRadius")]
    [SerializeField] private float pushEffectRadiusMultiplier = 1.75f;

    [Tooltip("6단계 합체 후 주변 캐릭터를 밀어낼 힘의 크기입니다.")]
    [SerializeField] private float pushEffectForceGrade6 = 5f;

    [Tooltip("7단계 합체 후 주변 캐릭터를 밀어낼 힘의 크기입니다.")]
    [SerializeField] private float pushEffectForceGrade7 = 6f;

    [Tooltip("8단계 합체 후 주변 캐릭터를 밀어낼 힘의 크기입니다.")]
    [SerializeField] private float pushEffectForceGrade8 = 7f;

    [Tooltip("9단계 합체 후 주변 캐릭터를 밀어낼 힘의 크기입니다.")]
    [SerializeField] private float pushEffectForceGrade9 = 8f;

    [Tooltip("10단계 합체 후 주변 캐릭터를 밀어낼 힘의 크기입니다.")]
    [SerializeField] private float pushEffectForceGrade10 = 9f;

    [Tooltip("주변 밀치기 방향에 추가할 위쪽 보정값입니다.")]
    [SerializeField] private float pushEffectUpwardBias = 0.2f;

    private bool isMerged;
    private bool isFinalMergeSequenceRunning;
    private float pendingComboDamageMultiplier = 1f;
    private readonly Collider2D[] pushEffectResults = new Collider2D[16];

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

    public bool TryMergeFromCollision(Collider2D otherCollider)
    {
        return TryMerge(otherCollider, true);
    }

    private bool TryMerge(Collider2D otherCollider, bool skipTouchCheck = false)
    {
        if (isMerged || otherCollider == null)
        {
            return false;
        }

        if (!SC_BattleMergeService.TryResolveMergeTarget(otherCollider, this, targetMerge => targetMerge.isMerged, out SC_CharacterMergeController otherMerge))
        {
            return false;
        }

        if (presenter == null || otherMerge.presenter == null)
        {
            return false;
        }

        SC_PlayerDragAndShoot myShoot = GetComponent<SC_PlayerDragAndShoot>();
        SC_PlayerDragAndShoot otherShoot = otherMerge.GetComponent<SC_PlayerDragAndShoot>();
        if (myShoot == null || otherShoot == null || !myShoot.IsShot || !otherShoot.IsShot)
        {
            return false;
        }

        if (myShoot.HasCollisionEraseRemaining || otherShoot.HasCollisionEraseRemaining)
        {
            return false;
        }

        if (!SC_BattleMergeService.TryCalculateNextGrade(presenter, otherMerge.presenter, out int nextGrade))
        {
            return false;
        }

        if (!skipTouchCheck && !SC_BattleMergeService.AreCollidersTouching(this, otherMerge, mergeContactTolerance))
        {
            return false;
        }

        Vector2 inheritedVelocity = CalculateInheritedVelocity(GetComponent<Rigidbody2D>(), otherMerge.GetComponent<Rigidbody2D>());

        DisablePhysicsForMerge(this);
        DisablePhysicsForMerge(otherMerge);

        isMerged = true;
        otherMerge.isMerged = true;

        GameObject mergedObject = SC_BattleMergeService.CreateMergedObject(mergeObjectPrefab, transform, otherMerge.transform, spawnParent, nextGrade, false);

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
            mergedShoot.SetPostLaunchCollisionState(true);
        }

        SC_ComboManager.ComboMergeResult comboMergeResult = SC_BattleMergeService.NotifyMergeCreated(nextGrade);
        SC_BattleMergeService.SetPhysicsEnabled(mergedObject, true, false);
        ApplyMergePushEffect(mergedObject, nextGrade);

        if (nextGrade >= 10)
        {
            SC_BattleMergeService.SetPhysicsEnabled(mergedObject, false, true);

            SC_CharacterMergeController mergedMergeController = mergedObject.GetComponent<SC_CharacterMergeController>();
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
            SC_BattleMergeService.NotifyBattleMergeAttack(battleManager, nextGrade, comboMergeResult.DamageMultiplier);
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

    private void ApplyMergePushEffect(GameObject mergedObject, int mergedGrade)
    {
        float pushForce = GetPushEffectForce(mergedGrade);
        float pushRadius = ResolvePushEffectRadius(mergedObject);
        if (mergedObject == null || pushRadius <= 0f || pushForce <= 0f)
        {
            return;
        }

        int hitCount = Physics2D.OverlapCircle(mergedObject.transform.position, pushRadius, ContactFilter2D.noFilter, pushEffectResults);
        Vector2 center = mergedObject.transform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = pushEffectResults[i];
            if (hitCollider == null)
            {
                continue;
            }

            Rigidbody2D targetRb2D = hitCollider.attachedRigidbody;
            if (targetRb2D == null || targetRb2D.gameObject == mergedObject)
            {
                continue;
            }

            SC_CharacterMergeController targetMerge = targetRb2D.GetComponent<SC_CharacterMergeController>();
            if (targetMerge == null || targetMerge == this || targetMerge.isMerged)
            {
                continue;
            }

            Vector2 pushDirection = (targetRb2D.position - center) + Vector2.up * pushEffectUpwardBias;
            if (pushDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                pushDirection = Vector2.up;
            }

            targetRb2D.AddForce(pushDirection.normalized * pushForce, ForceMode2D.Impulse);
        }
    }

    private float ResolvePushEffectRadius(GameObject mergedObject)
    {
        if (mergedObject == null || pushEffectRadiusMultiplier <= 0f)
        {
            return 0f;
        }

        CircleCollider2D circleCollider2D = mergedObject.GetComponent<CircleCollider2D>();
        if (circleCollider2D == null)
        {
            return pushEffectRadiusMultiplier;
        }

        Vector3 lossyScale = mergedObject.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
        float worldRadius = circleCollider2D.radius * Mathf.Max(0.01f, maxScale);
        return worldRadius * pushEffectRadiusMultiplier;
    }

    private float GetPushEffectForce(int mergedGrade)
    {
        switch (mergedGrade)
        {
            case 6:
                return pushEffectForceGrade6;
            case 7:
                return pushEffectForceGrade7;
            case 8:
                return pushEffectForceGrade8;
            case 9:
                return pushEffectForceGrade9;
            case 10:
                return pushEffectForceGrade10;
            default:
                return 0f;
        }
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

    private static Vector2 CalculateInheritedVelocity(Rigidbody2D myRb2D, Rigidbody2D otherRb2D)
    {
        if (myRb2D != null && otherRb2D != null)
        {
            Rigidbody2D lowerRb2D = myRb2D.position.y <= otherRb2D.position.y ? myRb2D : otherRb2D;
            return lowerRb2D.linearVelocity;
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

        SC_BattleMergeService.SetPhysicsEnabled(mergeController.gameObject, false, true);
    }
}
