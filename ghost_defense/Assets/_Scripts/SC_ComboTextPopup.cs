using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_ComboTextPopup : MonoBehaviour
{
    [Tooltip("콤보 문구를 표시할 TMP_Text입니다. 비워두면 자식 오브젝트에서 자동으로 찾습니다.")]
    [SerializeField] private TMP_Text comboText;

    [Tooltip("팝업 전체 투명도를 제어할 CanvasGroup입니다. 비워두면 현재 오브젝트에서 자동으로 찾거나 추가합니다.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("처음 등장할 때의 시작 스케일입니다.")]
    [SerializeField] private float startScale = 0f;

    [Tooltip("바운스 중간에 크게 커질 스케일입니다. 1.1이면 110%입니다.")]
    [SerializeField] private float overshootScale = 1.1f;

    [Tooltip("바운스가 끝난 뒤 유지할 기본 스케일입니다.")]
    [SerializeField] private float settleScale = 1f;

    [Tooltip("시작 스케일에서 최대 스케일까지 커지는 시간(초)입니다.")]
    [SerializeField] private float popInDuration = 0.08f;

    [Tooltip("최대 스케일에서 기본 스케일까지 줄어드는 시간(초)입니다.")]
    [SerializeField] private float settleDuration = 0.06f;

    [Tooltip("콤보 텍스트가 유지되는 시간(초)입니다.")]
    [SerializeField] private float visibleDuration = 0.45f;

    [Tooltip("사라지는 페이드아웃 시간(초)입니다.")]
    [SerializeField] private float fadeOutDuration = 0.12f;

    private Coroutine showCoroutine;

    private void Awake()
    {
        EnsureReferences();
    }

    public void ShowCombo(int comboCount)
    {
        EnsureReferences();
        SetText($"{Mathf.Max(0, comboCount)} COMBO");

        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
        }

        showCoroutine = StartCoroutine(CoShow());
    }

    private IEnumerator CoShow()
    {
        transform.localScale = Vector3.one * Mathf.Max(0f, startScale);
        SetAlpha(1f);

        yield return CoScaleTo(overshootScale, popInDuration);
        yield return CoScaleTo(settleScale, settleDuration);

        float remainTime = Mathf.Max(0f, visibleDuration);
        while (remainTime > 0f)
        {
            remainTime -= Time.unscaledDeltaTime;
            yield return null;
        }

        float fadeTime = Mathf.Max(0f, fadeOutDuration);
        float elapsedTime = 0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = fadeTime > 0f ? Mathf.Clamp01(elapsedTime / fadeTime) : 1f;
            SetAlpha(1f - progress);
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator CoScaleTo(float targetScale, float duration)
    {
        Vector3 startLocalScale = transform.localScale;
        Vector3 targetLocalScale = Vector3.one * Mathf.Max(0f, targetScale);
        float safeDuration = Mathf.Max(0f, duration);
        float elapsedTime = 0f;

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = safeDuration > 0f ? Mathf.Clamp01(elapsedTime / safeDuration) : 1f;
            transform.localScale = Vector3.LerpUnclamped(startLocalScale, targetLocalScale, progress);
            yield return null;
        }

        transform.localScale = targetLocalScale;
    }

    private void SetText(string text)
    {
        if (comboText != null)
        {
            comboText.text = text;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    private void EnsureReferences()
    {
        if (comboText == null)
        {
            comboText = GetComponentInChildren<TMP_Text>(true);
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
}
