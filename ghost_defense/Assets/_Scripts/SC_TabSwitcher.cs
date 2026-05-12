using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SC_TabSwitcher : MonoBehaviour
{
    [Serializable]
    private class TabItem
    {
        [Tooltip("탭 선택에 사용할 버튼")]
        [SerializeField] private Button button;

        [Tooltip("선택 해제 상태에서 표시할 오브젝트")]
        [SerializeField] private GameObject normalStateObject;

        [Tooltip("선택 상태에서 즉시 표시할 오브젝트")]
        [SerializeField] private GameObject selectedStateObject;

        [Tooltip("선택했을 때만 표시할 텍스트 오브젝트")]
        [SerializeField] private GameObject selectedTextObject;

        [Tooltip("탭을 선택했을 때 표시할 콘텐츠 루트 오브젝트")]
        [SerializeField] private GameObject contentRootObject;

        [Tooltip("선택 시 위로 올라가고 커질 탭 이미지 RectTransform")]
        [SerializeField] private RectTransform imageRectTransform;

        [Tooltip("선택 시 이미지가 위로 이동할 Y 오프셋")]
        [SerializeField] private float selectedImageOffsetY = 18f;

        [Tooltip("선택 시 이미지에 곱해질 스케일 값")]
        [SerializeField] private Vector3 selectedImageScale = new Vector3(1.1f, 1.1f, 1f);

        [Tooltip("선택 시 바운스 애니메이션 총 시간")]
        [SerializeField] private float bounceDuration = 0.22f;

        [Tooltip("선택 시 목표 위치보다 추가로 튀어오를 Y 값")]
        [SerializeField] private float bounceExtraOffsetY = 10f;

        private Vector2 cachedImageAnchoredPosition;
        private Vector3 cachedImageScale = Vector3.one;
        private bool hasCachedImageTransform;

        public Button Button => button;
        public GameObject NormalStateObject => normalStateObject;
        public GameObject SelectedStateObject => selectedStateObject;
        public GameObject SelectedTextObject => selectedTextObject;
        public GameObject ContentRootObject => contentRootObject;
        public RectTransform ImageRectTransform => imageRectTransform;
        public float BounceDuration => bounceDuration;
        public float BounceExtraOffsetY => bounceExtraOffsetY;

        public void CacheImageTransform()
        {
            if (hasCachedImageTransform || imageRectTransform == null)
            {
                return;
            }

            cachedImageAnchoredPosition = imageRectTransform.anchoredPosition;
            cachedImageScale = imageRectTransform.localScale;
            hasCachedImageTransform = true;
        }

        public Vector2 GetSelectedAnchoredPosition()
        {
            CacheImageTransform();
            Vector2 targetAnchoredPosition = cachedImageAnchoredPosition;
            targetAnchoredPosition.y += selectedImageOffsetY;
            return targetAnchoredPosition;
        }

        public Vector3 GetSelectedScale()
        {
            CacheImageTransform();
            return Vector3.Scale(cachedImageScale, selectedImageScale);
        }

        public Vector2 GetDefaultAnchoredPosition()
        {
            CacheImageTransform();
            return cachedImageAnchoredPosition;
        }

        public Vector3 GetDefaultScale()
        {
            CacheImageTransform();
            return cachedImageScale;
        }

        public void ApplyImmediateState(bool isSelected)
        {
            if (imageRectTransform == null)
            {
                return;
            }

            imageRectTransform.anchoredPosition = isSelected ? GetSelectedAnchoredPosition() : GetDefaultAnchoredPosition();
            imageRectTransform.localScale = isSelected ? GetSelectedScale() : GetDefaultScale();
        }
    }

    [Tooltip("왼쪽에서 오른쪽 순서대로 등록할 탭 목록")]
    [SerializeField] private TabItem[] tabs;

    [Tooltip("시작 시 선택할 탭 번호(1부터 시작)")]
    [SerializeField] private int defaultSelectedTabNumber = 3;

    private int currentSelectedIndex = -1;
    private UnityAction[] cachedTabActions;
    private Coroutine[] tabAnimationCoroutines;

    private void Awake()
    {
        CacheTabImageTransforms();
        BindButtonEvents();
        SelectTabByNumber(defaultSelectedTabNumber);
    }

    private void OnDestroy()
    {
        UnbindButtonEvents();
    }

    public void SelectTabByNumber(int tabNumber)
    {
        SelectTab(tabNumber - 1);
    }

    public void SelectTab(int tabIndex)
    {
        if (tabs == null || tabs.Length == 0)
        {
            return;
        }

        if (tabIndex < 0 || tabIndex >= tabs.Length)
        {
            return;
        }

        currentSelectedIndex = tabIndex;
        RefreshVisuals();
    }

    private void BindButtonEvents()
    {
        if (tabs == null)
        {
            return;
        }

        cachedTabActions = new UnityAction[tabs.Length];
        tabAnimationCoroutines = new Coroutine[tabs.Length];

        for (int i = 0; i < tabs.Length; i++)
        {
            int capturedIndex = i;

            if (tabs[i] == null || tabs[i].Button == null)
            {
                continue;
            }

            UnityAction action = () => SelectTab(capturedIndex);
            cachedTabActions[i] = action;
            tabs[i].Button.onClick.AddListener(action);
        }
    }

    private void UnbindButtonEvents()
    {
        if (tabs == null)
        {
            return;
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null || tabs[i].Button == null)
            {
                continue;
            }

            if (cachedTabActions != null && i < cachedTabActions.Length && cachedTabActions[i] != null)
            {
                tabs[i].Button.onClick.RemoveListener(cachedTabActions[i]);
            }
        }
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null)
            {
                continue;
            }

            bool isSelected = i == currentSelectedIndex;

            if (tabs[i].NormalStateObject != null)
            {
                tabs[i].NormalStateObject.SetActive(!isSelected);
            }

            if (tabs[i].SelectedStateObject != null)
            {
                tabs[i].SelectedStateObject.SetActive(isSelected);
            }

            if (tabs[i].SelectedTextObject != null)
            {
                tabs[i].SelectedTextObject.SetActive(isSelected);
            }

            if (tabs[i].ContentRootObject != null)
            {
                tabs[i].ContentRootObject.SetActive(isSelected);
            }

            PlayTabAnimation(i, isSelected);
        }
    }

    private void PlayTabAnimation(int tabIndex, bool isSelected)
    {
        if (tabs == null || tabIndex < 0 || tabIndex >= tabs.Length || tabs[tabIndex] == null)
        {
            return;
        }

        if (tabAnimationCoroutines != null && tabIndex < tabAnimationCoroutines.Length && tabAnimationCoroutines[tabIndex] != null)
        {
            StopCoroutine(tabAnimationCoroutines[tabIndex]);
            tabAnimationCoroutines[tabIndex] = null;
        }

        if (tabs[tabIndex].ImageRectTransform == null)
        {
            return;
        }

        tabAnimationCoroutines[tabIndex] = StartCoroutine(AnimateTabVisual(tabIndex, isSelected));
    }

    private IEnumerator AnimateTabVisual(int tabIndex, bool isSelected)
    {
        TabItem tab = tabs[tabIndex];
        RectTransform imageRectTransform = tab.ImageRectTransform;

        if (imageRectTransform == null)
        {
            yield break;
        }

        Vector2 startPosition = imageRectTransform.anchoredPosition;
        Vector3 startScale = imageRectTransform.localScale;
        Vector2 targetPosition = isSelected ? tab.GetSelectedAnchoredPosition() : tab.GetDefaultAnchoredPosition();
        Vector3 targetScale = isSelected ? tab.GetSelectedScale() : tab.GetDefaultScale();
        float duration = Mathf.Max(0.01f, tab.BounceDuration);

        if (isSelected)
        {
            Vector2 overshootPosition = targetPosition + new Vector2(0f, tab.BounceExtraOffsetY);
            Vector3 overshootScale = Vector3.LerpUnclamped(startScale, targetScale, 1.08f);
            float halfDuration = duration * 0.5f;

            yield return AnimateTransform(imageRectTransform, startPosition, overshootPosition, startScale, overshootScale, halfDuration, EaseOutCubic);
            yield return AnimateTransform(imageRectTransform, overshootPosition, targetPosition, overshootScale, targetScale, halfDuration, EaseOutBack);
        }
        else
        {
            yield return AnimateTransform(imageRectTransform, startPosition, targetPosition, startScale, targetScale, duration, EaseOutCubic);
        }

        imageRectTransform.anchoredPosition = targetPosition;
        imageRectTransform.localScale = targetScale;
        tabAnimationCoroutines[tabIndex] = null;
    }

    private IEnumerator AnimateTransform(
        RectTransform targetRect,
        Vector2 fromPosition,
        Vector2 toPosition,
        Vector3 fromScale,
        Vector3 toScale,
        float duration,
        Func<float, float> easingFunction)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
            float easedTime = easingFunction(normalizedTime);

            targetRect.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, easedTime);
            targetRect.localScale = Vector3.LerpUnclamped(fromScale, toScale, easedTime);
            yield return null;
        }

        targetRect.anchoredPosition = toPosition;
        targetRect.localScale = toScale;
    }

    private float EaseOutCubic(float value)
    {
        float inverse = 1f - value;
        return 1f - (inverse * inverse * inverse);
    }

    private float EaseOutBack(float value)
    {
        const float overshoot = 1.70158f;
        float offset = value - 1f;
        return 1f + ((overshoot + 1f) * offset * offset * offset) + (overshoot * offset * offset);
    }

    private void CacheTabImageTransforms()
    {
        if (tabs == null)
        {
            return;
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null)
            {
                continue;
            }

            // 원본 위치와 스케일을 저장해두고 선택 해제 시 그대로 복원한다.
            tabs[i].CacheImageTransform();
            tabs[i].ApplyImmediateState(false);
        }
    }
}
