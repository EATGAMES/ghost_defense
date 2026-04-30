using UnityEngine;

public class SC_CharacterYSort : MonoBehaviour
{
    [Tooltip("정렬할 SpriteRenderer")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Tooltip("Y 정렬 민감도(클수록 촘촘하게 분리)")]
    [SerializeField] private int sortPrecision = 100;

    [Tooltip("기준 오프셋(같은 레이어 내 우선순위 보정)")]
    [SerializeField] private int baseOrderOffset = 0;

    [Tooltip("Y 변경 감지 최소값(이 값보다 작으면 갱신하지 않음)")]
    [SerializeField] private float yChangeThreshold = 0.001f;

    private float cachedY;
    private bool isInitialized;

    private void Reset()
    {
        targetRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        ForceRefresh();
    }

    private void LateUpdate()
    {
        if (targetRenderer == null)
        {
            return;
        }

        float currentY = transform.position.y;
        if (isInitialized && Mathf.Abs(currentY - cachedY) < yChangeThreshold)
        {
            return;
        }

        cachedY = currentY;
        isInitialized = true;
        targetRenderer.sortingOrder = baseOrderOffset - Mathf.RoundToInt(currentY * sortPrecision);
    }

    public void ForceRefresh()
    {
        isInitialized = false;
        cachedY = float.NaN;
        LateUpdate();
    }
}
