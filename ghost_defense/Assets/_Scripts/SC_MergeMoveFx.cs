using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_MergeMoveFx : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("머지 위치에서 커졌다가 사라질 글로우 스프라이트입니다.")]
    [SerializeField] private SpriteRenderer mergeGlowRenderer;

    [Tooltip("목표 지점까지 이동할 메인 운석 스프라이트입니다.")]
    [SerializeField] private SpriteRenderer mainMeteorRenderer;

    [Header("재생")]
    [Tooltip("재생이 끝난 뒤 오브젝트를 제거할지 설정합니다.")]
    [SerializeField] private bool destroyOnComplete = true;

    [Tooltip("도착지와 가장 가까운 위치에서 사용할 이동 시간입니다.")]
    [SerializeField] private float minMoveDuration = 0.15f;

    [Tooltip("도착지와 가장 먼 위치에서 사용할 이동 시간입니다.")]
    [SerializeField] private float maxMoveDuration = 0.3f;

    [Tooltip("최소 이동 시간이 적용될 시작점과 도착점 사이 거리입니다.")]
    [SerializeField] private float minMoveDistance = 0.5f;

    [Tooltip("최대 이동 시간이 적용될 시작점과 도착점 사이 거리입니다.")]
    [SerializeField] private float maxMoveDistance = 6f;

    [Tooltip("메인 운석의 이동 속도 곡선입니다. 가로축은 시간, 세로축은 이동 진행도입니다.")]
    [SerializeField] private AnimationCurve moveProgressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("도착 후 제거되기 전까지 기다리는 시간입니다.")]
    [SerializeField] private float completeDelay = 0.05f;

    [Header("베지어 이동")]
    [Tooltip("베지어 제어점이 시작점과 도착점 사이에서 놓일 진행도입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float bezierControlProgress = 0.45f;

    [Tooltip("베지어 제어점의 좌우 랜덤 거리 범위입니다.")]
    [SerializeField] private Vector2 bezierSideOffsetRange = new Vector2(-0.45f, 0.45f);

    [Tooltip("베지어 제어점의 위아래 랜덤 거리 범위입니다.")]
    [SerializeField] private Vector2 bezierUpOffsetRange = new Vector2(0.15f, 0.7f);

    [Header("글로우")]
    [Tooltip("글로우가 시작할 때의 크기 배율입니다.")]
    [SerializeField] private float glowStartScale = 0.4f;

    [Tooltip("글로우가 사라질 때의 크기 배율입니다.")]
    [SerializeField] private float glowEndScale = 1.35f;

    [Tooltip("글로우가 완전히 사라지는 시간입니다.")]
    [SerializeField] private float glowDuration = 0.25f;

    [Tooltip("글로우의 시작 투명도입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float glowStartAlpha = 0.55f;

    [Tooltip("글로우의 종료 투명도입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float glowEndAlpha = 0f;

    private Vector3 startPosition;
    private Vector3 destinationPosition;
    private Vector3 bezierControlPosition;
    private Vector3 glowBaseScale;
    private Color glowBaseColor = Color.white;
    private MaterialPropertyBlock glowPropertyBlock;
    private Action arrivedCallback;
    private float playTime;
    private float currentMoveDuration;
    private bool hasArrived;
    private bool isPlaying;

    public float TotalLifetime => Mathf.Max(0f, currentMoveDuration) + Mathf.Max(0f, completeDelay);

    private void Awake()
    {
        CacheBaseValues();
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        playTime += Time.deltaTime;
        UpdateMainMeteor();
        UpdateMergeGlow();
        TryNotifyArrived();

        if (playTime >= TotalLifetime)
        {
            Complete();
        }
    }

    public void PlayAt(Vector3 worldPosition, Transform targetPoint)
    {
        PlayAt(worldPosition, targetPoint, null);
    }

    public void PlayAt(Vector3 worldPosition, Transform targetPoint, Action onArrived)
    {
        CacheBaseValues();

        transform.position = worldPosition;
        startPosition = worldPosition;
        destinationPosition = targetPoint != null ? targetPoint.position : worldPosition;
        currentMoveDuration = CalculateMoveDuration(startPosition, destinationPosition);
        bezierControlPosition = CreateBezierControlPosition(startPosition, destinationPosition);
        arrivedCallback = onArrived;
        playTime = 0f;
        hasArrived = false;
        isPlaying = true;

        if (mainMeteorRenderer != null)
        {
            mainMeteorRenderer.enabled = true;
            mainMeteorRenderer.transform.position = startPosition;
        }

        if (mergeGlowRenderer != null)
        {
            mergeGlowRenderer.enabled = true;
            mergeGlowRenderer.transform.position = startPosition;
            mergeGlowRenderer.transform.localScale = glowBaseScale * Mathf.Max(0f, glowStartScale);
            ApplyGlowVisibility(glowStartAlpha);
        }
    }

    private void UpdateMainMeteor()
    {
        if (mainMeteorRenderer == null)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, currentMoveDuration);
        float progress = Mathf.Clamp01(playTime / duration);
        float easedProgress = EvaluateMoveProgress(progress);

        mainMeteorRenderer.transform.position = CalculateBezierPosition(easedProgress);
    }

    private Vector3 CreateBezierControlPosition(Vector3 start, Vector3 end)
    {
        float controlProgress = Mathf.Clamp01(bezierControlProgress);
        Vector3 controlPosition = Vector3.Lerp(start, end, controlProgress);
        float sideOffset = UnityEngine.Random.Range(bezierSideOffsetRange.x, bezierSideOffsetRange.y);
        float upOffset = UnityEngine.Random.Range(bezierUpOffsetRange.x, bezierUpOffsetRange.y);

        return controlPosition + Vector3.right * sideOffset + Vector3.up * upOffset;
    }

    private Vector3 CalculateBezierPosition(float progress)
    {
        float inverseProgress = 1f - progress;

        return inverseProgress * inverseProgress * startPosition
            + 2f * inverseProgress * progress * bezierControlPosition
            + progress * progress * destinationPosition;
    }

    private float EvaluateMoveProgress(float progress)
    {
        if (moveProgressCurve == null || moveProgressCurve.length == 0)
        {
            return 1f - Mathf.Pow(1f - progress, 2f);
        }

        return Mathf.Clamp01(moveProgressCurve.Evaluate(progress));
    }

    private float CalculateMoveDuration(Vector3 start, Vector3 end)
    {
        float distance = Vector3.Distance(start, end);
        float distanceRange = Mathf.Max(0.01f, maxMoveDistance - minMoveDistance);
        float distanceProgress = Mathf.Clamp01((distance - minMoveDistance) / distanceRange);

        return Mathf.Lerp(Mathf.Max(0.01f, minMoveDuration), Mathf.Max(0.01f, maxMoveDuration), distanceProgress);
    }

    private void UpdateMergeGlow()
    {
        if (mergeGlowRenderer == null)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, glowDuration);
        float progress = Mathf.Clamp01(playTime / duration);
        float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);

        mergeGlowRenderer.transform.localScale = glowBaseScale * Mathf.Lerp(glowStartScale, glowEndScale, easedProgress);
        ApplyGlowVisibility(Mathf.Lerp(glowStartAlpha, glowEndAlpha, easedProgress));

        if (progress >= 1f)
        {
            mergeGlowRenderer.enabled = false;
        }
    }

    private void Complete()
    {
        TryNotifyArrived();
        isPlaying = false;

        if (mainMeteorRenderer != null)
        {
            mainMeteorRenderer.enabled = false;
        }

        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
    }

    private void TryNotifyArrived()
    {
        if (hasArrived || playTime < Mathf.Max(0.01f, currentMoveDuration))
        {
            return;
        }

        hasArrived = true;

        if (mainMeteorRenderer != null)
        {
            mainMeteorRenderer.transform.position = destinationPosition;
        }

        arrivedCallback?.Invoke();
        arrivedCallback = null;
    }

    private void CacheBaseValues()
    {
        if (mergeGlowRenderer != null && glowBaseScale == Vector3.zero)
        {
            glowBaseScale = mergeGlowRenderer.transform.localScale;
            glowBaseColor = mergeGlowRenderer.color;
        }

        if (glowBaseScale == Vector3.zero)
        {
            glowBaseScale = Vector3.one;
        }
    }

    private void ApplyGlowVisibility(float alpha)
    {
        if (mergeGlowRenderer == null)
        {
            return;
        }

        float safeAlpha = Mathf.Clamp01(alpha);
        Color visibleColor = glowBaseColor * safeAlpha;
        visibleColor.a = safeAlpha;
        mergeGlowRenderer.color = visibleColor;

        if (glowPropertyBlock == null)
        {
            glowPropertyBlock = new MaterialPropertyBlock();
        }

        mergeGlowRenderer.GetPropertyBlock(glowPropertyBlock);
        glowPropertyBlock.SetColor("_Color", visibleColor);
        glowPropertyBlock.SetColor("_BaseColor", visibleColor);
        mergeGlowRenderer.SetPropertyBlock(glowPropertyBlock);
    }
}
