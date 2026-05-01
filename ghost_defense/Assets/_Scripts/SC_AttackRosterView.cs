using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_AttackRosterView : MonoBehaviour
{
    [Tooltip("상단 공격 대기열 데이터를 제공할 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("상단에 표시할 캐릭터 뷰 프리팹입니다.")]
    [SerializeField] private GameObject characterViewPrefab;

    [Tooltip("생성된 상단 캐릭터들의 부모 Transform입니다. 비워두면 현재 오브젝트를 사용합니다.")]
    [SerializeField] private Transform spawnedParent;

    [Tooltip("상단 캐릭터 대기열의 중심 오프셋입니다.")]
    [SerializeField] private Vector3 centerOffset = new Vector3(0f, -0.6f, 0f);

    [Tooltip("캐릭터 사이의 가로 간격입니다.")]
    [SerializeField] private float slotSpacing = 1.25f;

    [Tooltip("위치가 회전할 때 이동 연출 시간(초)입니다.")]
    [SerializeField] private float moveDuration = 0.15f;

    [Tooltip("대기 캐릭터 기본 크기입니다.")]
    [SerializeField] private Vector3 idleScale = new Vector3(0.85f, 0.85f, 1f);

    [Tooltip("현재 공격 차례 캐릭터 강조 크기입니다.")]
    [SerializeField] private Vector3 currentTurnScale = new Vector3(1f, 1f, 1f);

    [Tooltip("앞쪽 캐릭터부터 적용할 기본 정렬 순서입니다.")]
    [SerializeField] private int sortingOrderBase = 20;

    private readonly List<SC_AttackRosterCharacterView> spawnedViews = new List<SC_AttackRosterCharacterView>(5);
    private readonly Dictionary<Transform, Coroutine> moveCoroutines = new Dictionary<Transform, Coroutine>();

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (spawnedParent == null)
        {
            spawnedParent = transform;
        }
    }

    private void OnEnable()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.StageRosterChanged += RefreshRosterViews;
        RefreshRosterViews();
    }

    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.StageRosterChanged -= RefreshRosterViews;
        }
    }

    private void RefreshRosterViews()
    {
        if (battleManager == null)
        {
            return;
        }

        SO_CharacterData[] roster = battleManager.GetRuntimeRosterSnapshot();
        int requiredCount = roster != null ? roster.Length : 0;
        EnsureViewCount(requiredCount);

        float startX = requiredCount > 1 ? -((requiredCount - 1) * slotSpacing) * 0.5f : 0f;
        for (int i = 0; i < spawnedViews.Count; i++)
        {
            SC_AttackRosterCharacterView view = spawnedViews[i];
            if (view == null)
            {
                continue;
            }

            if (roster == null || i >= roster.Length || roster[i] == null)
            {
                view.SetCharacterData(null);
                continue;
            }

            view.gameObject.SetActive(true);
            view.SetCharacterData(roster[i]);
            view.SetHighlight(i == 0);
            view.SetSortingOrder(sortingOrderBase + (requiredCount - i));

            Vector3 targetLocalPosition = centerOffset + new Vector3(startX + (i * slotSpacing), 0f, 0f);
            Vector3 targetScale = i == 0 ? currentTurnScale : idleScale;
            StartMove(view.transform, targetLocalPosition, targetScale);
        }
    }

    private void EnsureViewCount(int requiredCount)
    {
        if (requiredCount <= 0)
        {
            for (int i = 0; i < spawnedViews.Count; i++)
            {
                if (spawnedViews[i] != null)
                {
                    spawnedViews[i].gameObject.SetActive(false);
                }
            }

            return;
        }

        while (spawnedViews.Count < requiredCount)
        {
            SC_AttackRosterCharacterView createdView = CreateViewInstance();
            if (createdView == null)
            {
                break;
            }

            spawnedViews.Add(createdView);
        }
    }

    private SC_AttackRosterCharacterView CreateViewInstance()
    {
        if (characterViewPrefab == null)
        {
            Debug.LogWarning("SC_AttackRosterView: characterViewPrefab이 비어 있습니다.");
            return null;
        }

        GameObject createdObject = Instantiate(characterViewPrefab, spawnedParent);
        createdObject.name = $"OBJ_AttackRosterCharacter_{spawnedViews.Count + 1:000}";

        SC_AttackRosterCharacterView view = createdObject.GetComponent<SC_AttackRosterCharacterView>();
        if (view == null)
        {
            Debug.LogWarning("SC_AttackRosterView: SC_AttackRosterCharacterView 컴포넌트를 찾지 못했습니다.");
            Destroy(createdObject);
            return null;
        }

        createdObject.transform.localPosition = centerOffset;
        createdObject.transform.localRotation = Quaternion.identity;
        createdObject.transform.localScale = idleScale;
        return view;
    }

    private void StartMove(Transform targetTransform, Vector3 targetLocalPosition, Vector3 targetLocalScale)
    {
        if (targetTransform == null)
        {
            return;
        }

        if (moveCoroutines.TryGetValue(targetTransform, out Coroutine runningCoroutine) && runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }

        Coroutine moveCoroutine = StartCoroutine(CoMoveTransform(targetTransform, targetLocalPosition, targetLocalScale));
        moveCoroutines[targetTransform] = moveCoroutine;
    }

    private IEnumerator CoMoveTransform(Transform targetTransform, Vector3 targetLocalPosition, Vector3 targetLocalScale)
    {
        Vector3 startPosition = targetTransform.localPosition;
        Vector3 startScale = targetTransform.localScale;

        if (moveDuration <= 0f)
        {
            targetTransform.localPosition = targetLocalPosition;
            targetTransform.localScale = targetLocalScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            targetTransform.localPosition = Vector3.Lerp(startPosition, targetLocalPosition, easedT);
            targetTransform.localScale = Vector3.Lerp(startScale, targetLocalScale, easedT);
            yield return null;
        }

        targetTransform.localPosition = targetLocalPosition;
        targetTransform.localScale = targetLocalScale;
    }
}
