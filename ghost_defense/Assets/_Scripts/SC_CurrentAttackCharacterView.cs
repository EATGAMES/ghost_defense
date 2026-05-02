using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_CurrentAttackCharacterView : MonoBehaviour
{
    [Tooltip("현재 공격 캐릭터 정보를 전달할 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("상단 현재 공격 캐릭터로 사용할 프리팹입니다.")]
    [SerializeField] private GameObject characterViewPrefab;

    [Tooltip("캐릭터를 생성하고 복귀 위치로 사용할 기준 Transform입니다.")]
    [SerializeField] private Transform spawnAnchor;

    [Tooltip("머지 후 상단 공격 캐릭터가 바뀌기 전 대기 시간(초)입니다.")]
    [SerializeField] private float attackStartDelay = 0.08f;

    [Tooltip("공격 시작 전에 제자리에서 흔들리는 시간(초)입니다.")]
    [SerializeField] private float attackShakeDuration = 0.14f;

    [Tooltip("공격 시작 전에 좌우로 흔들리는 최대 거리입니다.")]
    [SerializeField] private float attackShakeDistance = 16f;

    [Tooltip("공격 시작 전에 흔들리는 속도입니다.")]
    [SerializeField] private float attackShakeFrequency = 55f;

    [Tooltip("공격 시 왼쪽으로 돌진하는 거리입니다.")]
    [SerializeField] private float attackMoveDistance = 120f;

    [Tooltip("공격 시 왼쪽으로 돌진하는 시간(초)입니다.")]
    [SerializeField] private float attackMoveDuration = 0.08f;

    [Tooltip("공격 후 원래 자리로 복귀하는 시간(초)입니다.")]
    [SerializeField] private float attackReturnDuration = 0.12f;

    [Tooltip("상단 캐릭터 SpriteRenderer의 정렬 순서입니다.")]
    [SerializeField] private int sortingOrder = 25;

    private GameObject spawnedCharacterObject;
    private SpriteRenderer spawnedSpriteRenderer;
    private Coroutine attackAnimationCoroutine;

    public float AttackStartDelay => Mathf.Max(0f, attackStartDelay);
    public float AttackAnimationDuration => Mathf.Max(0f, attackShakeDuration) + Mathf.Max(0.01f, attackMoveDuration) + Mathf.Max(0.01f, attackReturnDuration);

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (spawnAnchor == null)
        {
            spawnAnchor = transform;
        }

        EnsureSpawnedCharacter();
    }

    private void OnEnable()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.CurrentAttackCharacterChanged += OnCurrentAttackCharacterChanged;
        OnCurrentAttackCharacterChanged(battleManager.CurrentAttackCharacterData, false);
    }

    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.CurrentAttackCharacterChanged -= OnCurrentAttackCharacterChanged;
        }
    }

    private void EnsureSpawnedCharacter()
    {
        if (spawnedCharacterObject != null)
        {
            return;
        }

        if (characterViewPrefab == null)
        {
            Debug.LogWarning("SC_CurrentAttackCharacterView: characterViewPrefab이 비어 있습니다.");
            return;
        }

        if (spawnAnchor == null)
        {
            Debug.LogWarning("SC_CurrentAttackCharacterView: spawnAnchor가 비어 있습니다.");
            return;
        }

        spawnedCharacterObject = Instantiate(characterViewPrefab, spawnAnchor);
        spawnedCharacterObject.name = "OBJ_CurrentAttackCharacter";
        spawnedCharacterObject.transform.localPosition = Vector3.zero;
        spawnedCharacterObject.transform.localRotation = Quaternion.identity;

        spawnedSpriteRenderer = spawnedCharacterObject.GetComponentInChildren<SpriteRenderer>();
        if (spawnedSpriteRenderer != null)
        {
            spawnedSpriteRenderer.sortingOrder = sortingOrder;
        }
    }

    private void OnCurrentAttackCharacterChanged(SO_CharacterData characterData, bool playAttackAnimation)
    {
        EnsureSpawnedCharacter();
        if (spawnedCharacterObject == null)
        {
            return;
        }

        if (spawnedSpriteRenderer != null)
        {
            spawnedSpriteRenderer.sprite = characterData != null ? characterData.TopCharacterSprite : null;
        }

        spawnedCharacterObject.SetActive(characterData != null);
        ResetCharacterTransform();

        if (attackAnimationCoroutine != null)
        {
            StopCoroutine(attackAnimationCoroutine);
            attackAnimationCoroutine = null;
        }

        if (playAttackAnimation && characterData != null)
        {
            attackAnimationCoroutine = StartCoroutine(CoPlayAttackAnimation());
        }
    }

    private IEnumerator CoPlayAttackAnimation()
    {
        if (spawnedCharacterObject == null)
        {
            yield break;
        }

        Transform characterTransform = spawnedCharacterObject.transform;
        Vector3 startPosition = Vector3.zero;
        Vector3 dashTargetPosition = Vector3.left * Mathf.Max(0f, attackMoveDistance);

        yield return CoShake(characterTransform, startPosition);
        yield return CoDash(characterTransform, startPosition, dashTargetPosition);
        yield return CoReturn(characterTransform, dashTargetPosition, startPosition);

        ResetCharacterTransform();
        attackAnimationCoroutine = null;
    }

    private IEnumerator CoShake(Transform characterTransform, Vector3 startPosition)
    {
        float duration = Mathf.Max(0f, attackShakeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float shakeOffsetX = Mathf.Sin(elapsed * Mathf.Max(0f, attackShakeFrequency)) * Mathf.Max(0f, attackShakeDistance);
            characterTransform.localPosition = startPosition + Vector3.right * shakeOffsetX;
            yield return null;
        }

        characterTransform.localPosition = startPosition;
    }

    private IEnumerator CoDash(Transform characterTransform, Vector3 startPosition, Vector3 dashTargetPosition)
    {
        float duration = Mathf.Max(0.01f, attackMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // 빠르게 치고 나가는 느낌을 위해 초반 가속을 강하게 준다.
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            characterTransform.localPosition = Vector3.LerpUnclamped(startPosition, dashTargetPosition, easedT);
            yield return null;
        }
    }

    private IEnumerator CoReturn(Transform characterTransform, Vector3 dashTargetPosition, Vector3 startPosition)
    {
        float duration = Mathf.Max(0.01f, attackReturnDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // 복귀는 살짝 부드럽게 돌아오도록 감속 곡선을 사용한다.
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            characterTransform.localPosition = Vector3.LerpUnclamped(dashTargetPosition, startPosition, easedT);
            yield return null;
        }
    }

    private void ResetCharacterTransform()
    {
        if (spawnedCharacterObject == null)
        {
            return;
        }

        spawnedCharacterObject.transform.localPosition = Vector3.zero;
        spawnedCharacterObject.transform.localRotation = Quaternion.identity;
    }
}
