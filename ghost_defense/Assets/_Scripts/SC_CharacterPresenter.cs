using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SC_CharacterPresenter : MonoBehaviour
{
    [Tooltip("현재 필드 캐릭터의 머지 단계입니다.")]
    [SerializeField] private int mergeGrade = 1;

    [Tooltip("현재 단계 외형을 계산할 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("배틀 매니저를 찾지 못했을 때 사용할 단계별 대체 스프라이트입니다.")]
    [FormerlySerializedAs("gradeSprites")]
    [SerializeField] private Sprite[] fallbackGradeSprites;

    [Tooltip("단계 이미지를 표시할 SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("단계별 질량을 적용할 Rigidbody2D입니다.")]
    [FormerlySerializedAs("rigidbody2D")]
    [SerializeField] private Rigidbody2D cachedRigidbody2D;

    [Tooltip("단계별 크기를 적용할 CircleCollider2D입니다.")]
    [SerializeField] private CircleCollider2D circleCollider2D;

    [Tooltip("1단계 기본 크기입니다.")]
    [SerializeField] private float baseScale = 1f;

    [Tooltip("단계가 1 증가할 때마다 더할 크기 값입니다.")]
    [SerializeField] private float scaleStep = 0.08f;

    [Tooltip("1단계 기본 질량입니다.")]
    [SerializeField] private float baseMass = 1f;

    [Tooltip("단계가 1 증가할 때마다 더할 질량 값입니다.")]
    [SerializeField] private float massStep = 0.2f;

    [Tooltip("1단계 기본 콜라이더 반지름입니다.")]
    [SerializeField] private float baseColliderRadius = 0.55f;

    [Tooltip("단계가 1 증가할 때마다 더할 콜라이더 반지름 값입니다.")]
    [SerializeField] private float colliderRadiusStep = 0.03f;

    [Tooltip("공통으로 사용할 콜라이더 오프셋입니다.")]
    [SerializeField] private Vector2 colliderOffset = new Vector2(0.02f, -0.15f);

    public int MergeGrade => Mathf.Clamp(mergeGrade, 1, 10);

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        cachedRigidbody2D = GetComponent<Rigidbody2D>();
        circleCollider2D = GetComponent<CircleCollider2D>();
    }

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        ApplyData();
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (cachedRigidbody2D == null)
        {
            cachedRigidbody2D = GetComponent<Rigidbody2D>();
        }

        if (circleCollider2D == null)
        {
            circleCollider2D = GetComponent<CircleCollider2D>();
        }

        ApplyData();
    }

    public void Configure(int grade, bool applyImmediately = true)
    {
        mergeGrade = Mathf.Clamp(grade, 1, 10);

        if (applyImmediately)
        {
            ApplyData();
        }
    }

    public void SetMergeGrade(int grade, bool applyImmediately = true)
    {
        mergeGrade = Mathf.Clamp(grade, 1, 10);

        if (applyImmediately)
        {
            ApplyData();
        }
    }

    public void ApplyData()
    {
        float scale = Mathf.Max(0.01f, baseScale + (MergeGrade - 1) * scaleStep);
        transform.localScale = new Vector3(scale, scale, 1f);

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = ResolveSpriteForCurrentGrade();
        }

        if (cachedRigidbody2D != null)
        {
            cachedRigidbody2D.mass = Mathf.Max(0.01f, baseMass + (MergeGrade - 1) * massStep);
        }

        if (circleCollider2D != null)
        {
            circleCollider2D.radius = Mathf.Max(0.01f, baseColliderRadius + (MergeGrade - 1) * colliderRadiusStep);
            circleCollider2D.offset = colliderOffset;
        }
    }

    private Sprite ResolveSpriteForCurrentGrade()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (battleManager != null)
        {
            Sprite rosterSprite = battleManager.GetFieldSpriteForGrade(MergeGrade);
            if (rosterSprite != null)
            {
                return rosterSprite;
            }
        }

        if (fallbackGradeSprites == null || fallbackGradeSprites.Length <= 0)
        {
            return null;
        }

        int spriteIndex = Mathf.Clamp(MergeGrade - 1, 0, fallbackGradeSprites.Length - 1);
        return fallbackGradeSprites[spriteIndex];
    }
}
