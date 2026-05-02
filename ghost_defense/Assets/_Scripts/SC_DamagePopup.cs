using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_DamagePopup : MonoBehaviour
{
    [Tooltip("데미지 숫자를 표시할 TMP 텍스트입니다.")]
    [SerializeField] private TMP_Text damageText;

    [Tooltip("숫자가 떠오르는 총 거리입니다.")]
    [SerializeField] private float riseDistance = 0.8f;

    [Tooltip("숫자가 유지되는 총 시간입니다.")]
    [SerializeField] private float lifetime = 0.6f;

    [Tooltip("기본 숫자 색상입니다.")]
    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("월드 텍스트일 때 사용할 정렬 레이어 이름입니다.")]
    [SerializeField] private string sortingLayerName = "Character";

    [Tooltip("월드 텍스트일 때 사용할 Order in Layer 값입니다.")]
    [SerializeField] private int sortingOrder = 80;

    private Vector3 startPosition;
    private Color startColor;
    private float elapsedTime;
    private Renderer cachedRenderer;

    private void Reset()
    {
        damageText = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TMP_Text>();
        }

        if (damageText != null)
        {
            cachedRenderer = damageText.GetComponent<Renderer>();
            damageText.alignment = TextAlignmentOptions.Center;
            damageText.color = normalColor;

            if (cachedRenderer != null)
            {
                cachedRenderer.sortingLayerName = sortingLayerName;
                cachedRenderer.sortingOrder = sortingOrder;
            }
        }
    }

    private void OnEnable()
    {
        startPosition = transform.position;
        elapsedTime = 0f;
        startColor = damageText != null ? damageText.color : normalColor;
    }

    private void Update()
    {
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        elapsedTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(elapsedTime / lifetime);

        transform.position = startPosition + Vector3.up * (riseDistance * normalizedTime);

        if (damageText != null)
        {
            Color nextColor = startColor;
            nextColor.a = 1f - normalizedTime;
            damageText.color = nextColor;
        }

        if (elapsedTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public void Play(float damageAmount)
    {
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TMP_Text>();
        }

        if (damageText != null)
        {
            damageText.text = Mathf.CeilToInt(Mathf.Max(0f, damageAmount)).ToString();
            damageText.color = normalColor;
            damageText.ForceMeshUpdate();

            if (cachedRenderer == null)
            {
                cachedRenderer = damageText.GetComponent<Renderer>();
            }

            if (cachedRenderer != null)
            {
                cachedRenderer.sortingLayerName = sortingLayerName;
                cachedRenderer.sortingOrder = sortingOrder;
            }
        }

        startPosition = transform.position;
        elapsedTime = 0f;
    }
}
