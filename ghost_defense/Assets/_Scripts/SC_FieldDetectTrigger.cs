using UnityEngine;

using System.Collections.Generic;
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

    [Tooltip("필드 캐릭터가 멈추지 않아도 감지 영역 안에 이 시간 이상 머무르면 차오른 것으로 판단하는 시간(초)입니다.")]
    [SerializeField] private float requiredInsideDuration = 0.35f;

    [Tooltip("겹침 검사에 사용할 최대 콜라이더 수입니다.")]
    [SerializeField] private int overlapBufferSize = 32;

    private Collider2D detectorCollider;
    private Collider2D[] overlapResults;
    private IFieldCharacterRuntime detectedFieldRuntime;
    private float detectedFieldRuntimeTimer;
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
        bool hasDetectedFieldCharacterInside = HasDetectedFieldCharacterInside();
        RefreshDashLineState(hasDetectedFieldCharacterInside);

        if (!hasDetectedFieldCharacterInside || !failOnShotEnter || isBattleFailTriggered)
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

    private bool HasDetectedFieldCharacterInside()
    {
        if (detectorCollider == null)
        {
            ClearDetectedFieldRuntime();
            return false;
        }

        bool hasStoppedFieldCharacter = false;
        IFieldCharacterRuntime highestFieldRuntime = null;
        float highestY = float.NegativeInfinity;

        CollectDetectedFieldCharacterFromRegistry(ref highestFieldRuntime, ref highestY, ref hasStoppedFieldCharacter);
        if (highestFieldRuntime == null)
        {
            CollectDetectedFieldCharacterFromOverlap(ref highestFieldRuntime, ref highestY, ref hasStoppedFieldCharacter);
        }

        if (highestFieldRuntime == null)
        {
            ClearDetectedFieldRuntime();
            return false;
        }

        UpdateDetectedFieldRuntimeTimer(highestFieldRuntime);
        return hasStoppedFieldCharacter || detectedFieldRuntimeTimer >= Mathf.Max(0f, requiredInsideDuration);
    }

    private void CollectDetectedFieldCharacterFromRegistry(ref IFieldCharacterRuntime highestFieldRuntime, ref float highestY, ref bool hasStoppedFieldCharacter)
    {
        List<IFieldCharacterRuntime> fieldRuntimes = SC_FieldCharacterRegistry.GetSnapshot();
        for (int i = 0; i < fieldRuntimes.Count; i++)
        {
            IFieldCharacterRuntime fieldRuntime = fieldRuntimes[i];
            if (!IsDetectableFieldCharacter(fieldRuntime) || !IsRuntimeOverlappingDetector(fieldRuntime))
            {
                continue;
            }

            CollectDetectedFieldCharacter(fieldRuntime, ref highestFieldRuntime, ref highestY, ref hasStoppedFieldCharacter);
        }
    }

    private void CollectDetectedFieldCharacterFromOverlap(ref IFieldCharacterRuntime highestFieldRuntime, ref float highestY, ref bool hasStoppedFieldCharacter)
    {
        ContactFilter2D contactFilter = ContactFilter2D.noFilter;
        int hitCount = detectorCollider.Overlap(contactFilter, overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            IFieldCharacterRuntime fieldRuntime = SC_BattleRuntimeUtility.GetFieldRuntime(overlapResults[i]);
            if (!IsDetectableFieldCharacter(fieldRuntime))
            {
                continue;
            }

            CollectDetectedFieldCharacter(fieldRuntime, ref highestFieldRuntime, ref highestY, ref hasStoppedFieldCharacter);
        }
    }

    private void CollectDetectedFieldCharacter(IFieldCharacterRuntime fieldRuntime, ref IFieldCharacterRuntime highestFieldRuntime, ref float highestY, ref bool hasStoppedFieldCharacter)
    {
        if (IsStoppedFieldCharacter(fieldRuntime))
        {
            hasStoppedFieldCharacter = true;
        }

        float runtimeY = GetRuntimeY(fieldRuntime);
        if (highestFieldRuntime == null || runtimeY > highestY)
        {
            highestFieldRuntime = fieldRuntime;
            highestY = runtimeY;
        }
    }

    private bool IsDetectableFieldCharacter(IFieldCharacterRuntime fieldRuntime)
    {
        if (fieldRuntime == null || !fieldRuntime.IsActiveFieldCharacter || !fieldRuntime.IsLaunched || fieldRuntime.IsDragging)
        {
            return false;
        }

        return true;
    }

    private bool IsRuntimeOverlappingDetector(IFieldCharacterRuntime fieldRuntime)
    {
        if (fieldRuntime == null || fieldRuntime.RuntimeObject == null)
        {
            return false;
        }

        Bounds detectorBounds = detectorCollider.bounds;
        Collider2D[] runtimeColliders = fieldRuntime.RuntimeObject.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < runtimeColliders.Length; i++)
        {
            Collider2D runtimeCollider = runtimeColliders[i];
            if (runtimeCollider == null || runtimeCollider == detectorCollider || !runtimeCollider.enabled || !runtimeCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (detectorBounds.Intersects(runtimeCollider.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private float GetRuntimeY(IFieldCharacterRuntime fieldRuntime)
    {
        Transform runtimeTransform = fieldRuntime != null ? fieldRuntime.RuntimeTransform : null;
        return runtimeTransform != null ? runtimeTransform.position.y : float.NegativeInfinity;
    }

    private bool IsStoppedFieldCharacter(IFieldCharacterRuntime fieldRuntime)
    {
        if (!IsDetectableFieldCharacter(fieldRuntime))
        {
            return false;
        }

        float safeStopSpeed = Mathf.Max(0f, stoppedCharacterSpeedThreshold);
        return fieldRuntime.CurrentVelocity.sqrMagnitude <= safeStopSpeed * safeStopSpeed;
    }

    private void UpdateDetectedFieldRuntimeTimer(IFieldCharacterRuntime fieldRuntime)
    {
        if (detectedFieldRuntime != fieldRuntime)
        {
            detectedFieldRuntime = fieldRuntime;
            detectedFieldRuntimeTimer = 0f;
        }

        detectedFieldRuntimeTimer += Time.fixedDeltaTime;
    }

    private void ClearDetectedFieldRuntime()
    {
        detectedFieldRuntime = null;
        detectedFieldRuntimeTimer = 0f;
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
