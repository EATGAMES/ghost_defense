using UnityEngine;

[DisallowMultipleComponent]
public class SC_MonsterHealthBarView : MonoBehaviour
{
    [Tooltip("체력 스크립트(비우면 같은 오브젝트에서 자동 탐색)")]
    [SerializeField] private SC_MonsterHealth monsterHealth;

    [Tooltip("체력바 기준으로 따라갈 타겟 Transform(비우면 자동 탐색)")]
    [SerializeField] private Transform followTarget;

    [Tooltip("체력바를 타겟 머리 위에 표시할 로컬 오프셋")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("검은 배경 바 크기")]
    [SerializeField] private Vector2 backgroundSize = new Vector2(0.8f, 0.12f);

    [Tooltip("빨간 체력 바 크기(배경보다 조금 작게)")]
    [SerializeField] private Vector2 foregroundSize = new Vector2(0.74f, 0.08f);

    [Tooltip("배경과 체력바 사이 Z 간격")]
    [SerializeField] private float foregroundZOffset = -0.01f;

    [Tooltip("체력바 스프라이트 정렬 레이어")]
    [SerializeField] private string sortingLayerName = "Character";

    [Tooltip("체력바 Order in Layer")]
    [SerializeField] private int sortingOrder = 50;

    private static Sprite cachedWhiteSprite;
    private Transform barRoot;
    private Transform foregroundTransform;

    private void Awake()
    {
        if (monsterHealth == null)
        {
            monsterHealth = GetComponent<SC_MonsterHealth>();
        }

        if (followTarget == null)
        {
            Rigidbody2D rb = GetComponentInChildren<Rigidbody2D>();
            if (rb != null)
            {
                followTarget = rb.transform;
            }
            else
            {
                SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                followTarget = spriteRenderer != null ? spriteRenderer.transform : transform;
            }
        }

        CreateOrResetBar();
        RefreshBar();
    }

    private void LateUpdate()
    {
        RefreshBar();
    }

    private void OnDestroy()
    {
        if (barRoot != null)
        {
            Destroy(barRoot.gameObject);
        }
    }

    private void CreateOrResetBar()
    {
        if (cachedWhiteSprite == null)
        {
            cachedWhiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        Transform parentTarget = followTarget != null ? followTarget : transform;

        GameObject rootObject = new GameObject("OBJ_MonsterHpBar");
        barRoot = rootObject.transform;
        barRoot.SetParent(parentTarget, false);
        barRoot.localPosition = localOffset;

        GameObject backgroundObject = new GameObject("OBJ_HpBarBackground");
        backgroundObject.transform.SetParent(barRoot, false);
        backgroundObject.transform.localPosition = Vector3.zero;
        backgroundObject.transform.localScale = new Vector3(backgroundSize.x, backgroundSize.y, 1f);

        SpriteRenderer backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = cachedWhiteSprite;
        backgroundRenderer.color = Color.black;
        backgroundRenderer.sortingLayerName = sortingLayerName;
        backgroundRenderer.sortingOrder = sortingOrder;

        GameObject foregroundObject = new GameObject("OBJ_HpBarForeground");
        foregroundObject.transform.SetParent(barRoot, false);
        foregroundObject.transform.localPosition = new Vector3(-backgroundSize.x * 0.5f + foregroundSize.x * 0.5f, 0f, foregroundZOffset);
        foregroundObject.transform.localScale = new Vector3(foregroundSize.x, foregroundSize.y, 1f);

        SpriteRenderer foregroundRenderer = foregroundObject.AddComponent<SpriteRenderer>();
        foregroundRenderer.sprite = cachedWhiteSprite;
        foregroundRenderer.color = Color.red;
        foregroundRenderer.sortingLayerName = sortingLayerName;
        foregroundRenderer.sortingOrder = sortingOrder + 1;

        foregroundTransform = foregroundObject.transform;
    }

    private void RefreshBar()
    {
        if (monsterHealth == null || foregroundTransform == null)
        {
            return;
        }

        float hp01 = monsterHealth.NormalizedHp;
        Vector3 localScale = foregroundTransform.localScale;
        localScale.x = foregroundSize.x * hp01;
        foregroundTransform.localScale = localScale;

        Vector3 localPosition = foregroundTransform.localPosition;
        localPosition.x = -backgroundSize.x * 0.5f + localScale.x * 0.5f;
        foregroundTransform.localPosition = localPosition;
    }
}
