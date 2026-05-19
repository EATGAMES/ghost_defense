using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class SC_ButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    [Tooltip("크기를 변경할 대상입니다. 비워두면 이 오브젝트의 Transform을 사용합니다.")]
    private Transform targetTransform;

    [SerializeField]
    [Tooltip("버튼을 누르는 순간 적용할 크기 비율입니다.")]
    private float pressedScale = 0.95f;

    [SerializeField]
    [Tooltip("손을 뗀 뒤 원래 크기로 돌아오는 시간입니다.")]
    private float returnDuration = 0.2f;

    [SerializeField]
    [Tooltip("게임 시간이 멈춰도 버튼 복귀 애니메이션을 재생할지 여부입니다.")]
    private bool useUnscaledTime = true;

    private Button button;
    private Vector3 originScale;
    private Coroutine returnCoroutine;
    private bool isPressed;

    private void Awake()
    {
        button = GetComponent<Button>();
        ResolveTargetTransform();
        originScale = targetTransform.localScale;
    }

    private void OnDisable()
    {
        StopReturnCoroutine();

        if (targetTransform != null)
        {
            targetTransform.localScale = originScale;
        }

        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || !button.interactable || targetTransform == null)
        {
            return;
        }

        StopReturnCoroutine();
        isPressed = true;
        targetTransform.localScale = originScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed || targetTransform == null)
        {
            return;
        }

        isPressed = false;
        returnCoroutine = StartCoroutine(ReturnToOriginScale());
    }

    private IEnumerator ReturnToOriginScale()
    {
        Vector3 startScale = targetTransform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < returnDuration)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsedTime += deltaTime;

            float progress = returnDuration > 0f ? Mathf.Clamp01(elapsedTime / returnDuration) : 1f;
            targetTransform.localScale = Vector3.Lerp(startScale, originScale, progress);

            yield return null;
        }

        targetTransform.localScale = originScale;
        returnCoroutine = null;
    }

    private void ResolveTargetTransform()
    {
        if (targetTransform == null)
        {
            targetTransform = transform;
        }
    }

    private void StopReturnCoroutine()
    {
        if (returnCoroutine == null)
        {
            return;
        }

        StopCoroutine(returnCoroutine);
        returnCoroutine = null;
    }
}
