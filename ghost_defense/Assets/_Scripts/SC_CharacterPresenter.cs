using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SC_CharacterPresenter : MonoBehaviour
{
    [Tooltip("현재 필드 캐릭터의 합체 단계입니다.")]
    [SerializeField] private int mergeGrade = 1;

    [Tooltip("현재 단계 스프라이트를 계산할 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("배틀 매니저를 찾지 못했을 때 사용할 대체 스프라이트 목록입니다.")]
    [FormerlySerializedAs("gradeSprites")]
    [SerializeField] private Sprite[] fallbackGradeSprites;

    [Tooltip("단계 이미지를 표시할 스프라이트 렌더러입니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("단계별 질량을 적용할 리지드바디입니다.")]
    [FormerlySerializedAs("rigidbody2D")]
    [SerializeField] private Rigidbody2D cachedRigidbody2D;

    [Tooltip("기본 설정을 함께 맞출 원형 콜라이더입니다.")]
    [SerializeField] private CircleCollider2D circleCollider2D;

    [Tooltip("1단계 기본 크기입니다.")]
    [SerializeField] private float baseScale = 1f;

    [Tooltip("단계가 1 증가할 때마다 더할 크기 값입니다.")]
    [SerializeField] private float scaleStep = 0.08f;

    [Tooltip("1~10단계별로 직접 사용할 스케일 값 목록입니다. 비워두면 기본 크기와 증가값을 사용합니다.")]
    [SerializeField] private float[] scaleByGrade = new float[10];

    [Tooltip("1단계 기본 질량입니다.")]
    [SerializeField] private float baseMass = 1f;

    [Tooltip("단계가 1 증가할 때마다 더할 질량 값입니다.")]
    [SerializeField] private float massStep = 0.2f;

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
        float scale = ResolveScaleForCurrentGrade();
        transform.localScale = new Vector3(scale, scale, 1f);

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = ResolveSpriteForCurrentGrade();
        }

        if (cachedRigidbody2D != null)
        {
            cachedRigidbody2D.mass = Mathf.Max(0.01f, baseMass + (MergeGrade - 1) * massStep);
        }
    }

    private float ResolveScaleForCurrentGrade()
    {
        int gradeIndex = MergeGrade - 1;
        if (scaleByGrade != null && gradeIndex >= 0 && gradeIndex < scaleByGrade.Length && scaleByGrade[gradeIndex] > 0f)
        {
            return Mathf.Max(0.01f, scaleByGrade[gradeIndex]);
        }

        return Mathf.Max(0.01f, baseScale + gradeIndex * scaleStep);
    }

    private Sprite ResolveSpriteForCurrentGrade()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (battleManager != null)
        {
            Sprite fieldSprite = battleManager.GetFieldSpriteForGrade(MergeGrade);
            if (fieldSprite != null)
            {
                return fieldSprite;
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
