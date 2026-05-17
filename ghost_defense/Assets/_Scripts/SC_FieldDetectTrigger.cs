using UnityEngine;

using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SC_FieldDetectTrigger : MonoBehaviour
{
    [Tooltip("감지 중일 때 켜둘 대시 라인 오브젝트입니다.")]
    [SerializeField] private GameObject dashLineObject;

    [Tooltip("게임오버를 전달할 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("발사된 캐릭터가 닿으면 게임오버를 발생시킬지 여부입니다.")]
    [SerializeField] private bool failOnShotEnter;

    [Tooltip("필드 캐릭터가 이 속도 이하이면 멈춘 것으로 판단합니다.")]
    [FormerlySerializedAs("dropStopSpeedThreshold")]
    [SerializeField] private float stoppedCharacterSpeedThreshold = 0.2f;

    [Tooltip("겹침 검사에 사용할 최대 콜라이더 수입니다.")]
    [SerializeField] private int overlapBufferSize = 32;

    private Collider2D detectorCollider;
    private Collider2D[] overlapResults;
    private bool isBattleFailTriggered;

    private void Awake()
    {
        detectorCollider = GetComponent<Collider2D>();

        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        int bufferSize = Mathf.Max(8, overlapBufferSize);
        overlapResults = new Collider2D[bufferSize];
        RefreshDashLineState(false);
    }

    private void FixedUpdate()
    {
        bool hasStoppedFieldCharacterInside = HasStoppedFieldCharacterInside();
        RefreshDashLineState(hasStoppedFieldCharacterInside);

        if (!hasStoppedFieldCharacterInside || !failOnShotEnter || isBattleFailTriggered)
        {
            return;
        }

        isBattleFailTriggered = true;
        if (battleManager == null)
        {
            Debug.LogWarning("SC_FieldDetectTrigger: SC_BattleManager를 찾지 못했습니다.", this);
            return;
        }

        battleManager.NotifyBattleFailed();
    }

    private bool HasStoppedFieldCharacterInside()
    {
        if (detectorCollider == null)
        {
            return false;
        }

        ContactFilter2D contactFilter = ContactFilter2D.noFilter;
        contactFilter.useTriggers = true;

        int hitCount = detectorCollider.Overlap(contactFilter, overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            IFieldCharacterRuntime fieldRuntime = SC_BattleRuntimeUtility.GetFieldRuntime(overlapResults[i]);
            if (IsStoppedFieldCharacter(fieldRuntime))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsStoppedFieldCharacter(IFieldCharacterRuntime fieldRuntime)
    {
        if (fieldRuntime == null || !fieldRuntime.IsActiveFieldCharacter || !fieldRuntime.IsLaunched || fieldRuntime.IsDragging)
        {
            return false;
        }

        float safeStopSpeed = Mathf.Max(0f, stoppedCharacterSpeedThreshold);
        return fieldRuntime.CurrentVelocity.sqrMagnitude <= safeStopSpeed * safeStopSpeed;
    }

    private void RefreshDashLineState(bool isActive)
    {
        if (dashLineObject == null)
        {
            return;
        }

        if (dashLineObject.activeSelf == isActive)
        {
            return;
        }

        dashLineObject.SetActive(isActive);
    }
}
