using UnityEngine;

public class SC_CharacterPresenter : MonoBehaviour
{
    [Tooltip("이 캐릭터가 사용할 데이터")]
    [SerializeField] private SO_CharacterData characterData;

    [Tooltip("스프라이트를 적용할 렌더러")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("무게를 적용할 Rigidbody2D")]
    [SerializeField] private Rigidbody2D rigidbody2D;

    [Tooltip("반지름/오프셋을 적용할 CircleCollider2D")]
    [SerializeField] private CircleCollider2D circleCollider2D;

    public SO_CharacterData CharacterData => characterData;

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        circleCollider2D = GetComponent<CircleCollider2D>();
    }

    private void Awake()
    {
        ApplyData();
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (rigidbody2D == null)
        {
            rigidbody2D = GetComponent<Rigidbody2D>();
        }

        if (circleCollider2D == null)
        {
            circleCollider2D = GetComponent<CircleCollider2D>();
        }

        ApplyData();
    }

    public void SetCharacterData(SO_CharacterData newData, bool applyImmediately = true)
    {
        characterData = newData;

        if (applyImmediately)
        {
            ApplyData();
        }
    }

    public void ApplyData()
    {
        if (characterData == null)
        {
            return;
        }

        float sizePercent = Mathf.Max(0.01f, characterData.SizePercent);
        transform.localScale = new Vector3(sizePercent, sizePercent, 1f);

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = characterData.CharacterSprite;
        }

        if (rigidbody2D != null)
        {
            rigidbody2D.mass = characterData.Weight;
        }

        if (circleCollider2D != null)
        {
            circleCollider2D.radius = characterData.CircleColliderRadius;
            circleCollider2D.offset = characterData.CircleColliderOffset;
        }
    }
}

