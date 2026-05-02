using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_CurrentAttackCharacterView : MonoBehaviour
{
    [Tooltip("현재 공격 캐릭터 정보를 전달받을 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("상단에서 직접 배치해 둘 공격 캐릭터 루트 Transform입니다.")]
    [SerializeField] private Transform animatedCharacterRoot;

    [Tooltip("상단 공격 캐릭터 스프라이트를 표시할 SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer characterSpriteRenderer;

    [Tooltip("공격 시작 전 대기 시간(초)입니다.")]
    [SerializeField] private float attackStartDelay = 0.08f;

    [Tooltip("공격 시작 전에 제자리에서 흔들리는 시간(초)입니다.")]
    [SerializeField] private float attackShakeDuration = 0.14f;

    [Tooltip("공격 시작 전에 좌우로 흔들리는 최대 거리입니다.")]
    [SerializeField] private float attackShakeDistance = 16f;

    [Tooltip("공격 시작 전에 흔들리는 속도입니다.")]
    [SerializeField] private float attackShakeFrequency = 55f;

    [Tooltip("공격 때 왼쪽으로 돌진하는 거리입니다.")]
    [SerializeField] private float attackMoveDistance = 120f;

    [Tooltip("공격 때 왼쪽으로 돌진하는 시간(초)입니다.")]
    [SerializeField] private float attackMoveDuration = 0.08f;

    [Tooltip("공격 후 원래 자리로 복귀하는 시간(초)입니다.")]
    [SerializeField] private float attackReturnDuration = 0.12f;

    private Coroutine attackAnimationCoroutine;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    public float AttackStartDelay => Mathf.Max(0f, attackStartDelay);
    public float AttackAnimationDuration => Mathf.Max(0f, attackShakeDuration) + Mathf.Max(0.01f, attackMoveDuration) + Mathf.Max(0.01f, attackReturnDuration);

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (animatedCharacterRoot == null)
        {
            animatedCharacterRoot = transform;
        }

        if (characterSpriteRenderer == null)
        {
            characterSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        CacheInitialTransform();
        ApplyCharacterSprite(battleManager != null ? battleManager.CurrentAttackCharacterData : null);
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

        if (attackAnimationCoroutine != null)
        {
            StopCoroutine(attackAnimationCoroutine);
            attackAnimationCoroutine = null;
        }

        ResetCharacterTransform();
    }

    private void OnCurrentAttackCharacterChanged(SO_CharacterData characterData, bool playAttackAnimation)
    {
        ApplyCharacterSprite(characterData);

        if (animatedCharacterRoot == null)
        {
            return;
        }

        if (attackAnimationCoroutine != null)
        {
            StopCoroutine(attackAnimationCoroutine);
            attackAnimationCoroutine = null;
        }

        ResetCharacterTransform();

        if (playAttackAnimation && characterData != null)
        {
            attackAnimationCoroutine = StartCoroutine(CoPlayAttackAnimation());
        }
    }

    private void ApplyCharacterSprite(SO_CharacterData characterData)
    {
        if (characterSpriteRenderer == null)
        {
            return;
        }

        int attackGrade = battleManager != null ? battleManager.CurrentAttackGrade : 0;
        characterSpriteRenderer.sprite = characterData != null ? characterData.GetTopCharacterSpriteForGrade(attackGrade) : null;
        characterSpriteRenderer.enabled = characterSpriteRenderer.sprite != null;
    }

    private IEnumerator CoPlayAttackAnimation()
    {
        if (animatedCharacterRoot == null)
        {
            yield break;
        }

        Vector3 startPosition = initialLocalPosition;
        Vector3 dashTargetPosition = startPosition + Vector3.left * Mathf.Max(0f, attackMoveDistance);

        yield return CoShake(startPosition);
        yield return CoDash(startPosition, dashTargetPosition);
        yield return CoReturn(dashTargetPosition, startPosition);

        ResetCharacterTransform();
        attackAnimationCoroutine = null;
    }

    private IEnumerator CoShake(Vector3 startPosition)
    {
        float duration = Mathf.Max(0f, attackShakeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float shakeOffsetX = Mathf.Sin(elapsed * Mathf.Max(0f, attackShakeFrequency)) * Mathf.Max(0f, attackShakeDistance);
            animatedCharacterRoot.localPosition = startPosition + Vector3.right * shakeOffsetX;
            yield return null;
        }

        animatedCharacterRoot.localPosition = startPosition;
    }

    private IEnumerator CoDash(Vector3 startPosition, Vector3 dashTargetPosition)
    {
        float duration = Mathf.Max(0.01f, attackMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            animatedCharacterRoot.localPosition = Vector3.LerpUnclamped(startPosition, dashTargetPosition, easedT);
            yield return null;
        }
    }

    private IEnumerator CoReturn(Vector3 dashTargetPosition, Vector3 startPosition)
    {
        float duration = Mathf.Max(0.01f, attackReturnDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            animatedCharacterRoot.localPosition = Vector3.LerpUnclamped(dashTargetPosition, startPosition, easedT);
            yield return null;
        }
    }

    private void CacheInitialTransform()
    {
        if (animatedCharacterRoot == null)
        {
            return;
        }

        initialLocalPosition = animatedCharacterRoot.localPosition;
        initialLocalRotation = animatedCharacterRoot.localRotation;
    }

    private void ResetCharacterTransform()
    {
        if (animatedCharacterRoot == null)
        {
            return;
        }

        animatedCharacterRoot.localPosition = initialLocalPosition;
        animatedCharacterRoot.localRotation = initialLocalRotation;
    }
}
