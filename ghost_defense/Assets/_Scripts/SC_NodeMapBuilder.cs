using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class SC_NodeMapBuilder : MonoBehaviour
{
    private readonly struct NodeBuildInfo
    {
        public readonly NodeStageEntry Entry;
        public readonly Vector2 Position;

        public NodeBuildInfo(NodeStageEntry entry, Vector2 position)
        {
            Entry = entry;
            Position = position;
        }
    }

    [Tooltip("스테이지 번호별 노드 배치 데이터 목록입니다.")]
    [SerializeField] private SO_NodeStageData[] nodeStageDataList;

    [Tooltip("생성할 노드 버튼 프리팹입니다.")]
    [SerializeField] private SC_NodeView nodeViewPrefab;

    [Tooltip("생성된 노드 버튼들을 넣을 부모 RectTransform입니다. 비워두면 이 오브젝트를 사용합니다.")]
    [SerializeField] private RectTransform nodeParent;

    [Tooltip("생성된 연결 라인을 넣을 부모 RectTransform입니다. 비워두면 Node Parent를 사용합니다.")]
    [SerializeField] private RectTransform lineParent;

    [Tooltip("노드맵 이동을 적용할 루트 RectTransform입니다. 비워두면 Node Parent와 Line Parent를 함께 이동합니다.")]
    [SerializeField] private RectTransform mapMoveRoot;

    [Tooltip("노드맵과 함께 움직일 배경 RectTransform입니다. 비워두면 배경은 움직이지 않습니다.")]
    [FormerlySerializedAs("movingBackground")]
    [SerializeField] private RectTransform movingBackground;

    [Tooltip("노드맵과 함께 움직일 추가 배경 RectTransform 목록입니다.")]
    [SerializeField] private RectTransform[] movingBackgrounds = Array.Empty<RectTransform>();

    [Tooltip("노드맵 이동량 대비 배경이 움직일 비율입니다. 0.5는 노드맵의 절반만 움직입니다.")]
    [SerializeField] private float movingBackgroundMoveRatio = 0.5f;

    [Tooltip("노드맵 화면 위치 계산에 사용할 기준 RectTransform입니다. 비워두면 Node Parent의 부모를 사용합니다.")]
    [SerializeField] private RectTransform viewportRect;

    [Tooltip("자동 배치 시 첫 노드의 아래쪽 시작 Y 위치입니다.")]
    [SerializeField] private float startY = -620f;

    [Tooltip("자동 배치 시 노드 사이의 세로 간격입니다.")]
    [SerializeField] private float verticalSpacing = 220f;

    [Tooltip("같은 레이어 안에서 노드 사이의 가로 간격입니다.")]
    [SerializeField] private float horizontalSpacing = 240f;

    [Tooltip("시작할 때 자동으로 노드를 생성할지 여부입니다.")]
    [SerializeField] private bool buildOnStart = true;

    [Tooltip("테스트용 치트입니다. 켜면 노드 클릭 시 씬 이동 없이 즉시 클리어 처리합니다.")]
    [SerializeField] private bool cheatClearNodeWithoutEntering;

    [Tooltip("노드 사이 연결 라인을 자동으로 생성할지 여부입니다.")]
    [SerializeField] private bool drawConnectionLines = true;

    [Header("기본라인")]
    [Tooltip("기본라인의 바깥쪽 색상입니다.")]
    [FormerlySerializedAs("connectionLineOuterColor")]
    [SerializeField] private Color defaultConnectionLineOuterColor = new Color(0f, 0f, 0f, 0.6f);

    [Tooltip("기본라인의 안쪽 색상입니다.")]
    [FormerlySerializedAs("connectionLineColor")]
    [SerializeField] private Color defaultConnectionLineInnerColor = new Color(1f, 1f, 1f, 0.45f);

    [Tooltip("연결 라인의 바깥쪽 두께입니다.")]
    [SerializeField] private float connectionLineOuterThickness = 10f;

    [Tooltip("연결 라인의 안쪽 두께입니다.")]
    [SerializeField] private float connectionLineThickness = 6f;

    [Header("활성라인")]
    [Tooltip("활성라인의 바깥쪽 색상입니다.")]
    [FormerlySerializedAs("passedConnectionLineOuterColor")]
    [SerializeField] private Color activeConnectionLineOuterColor = new Color(0.2f, 0.9f, 1f, 0.8f);

    [Tooltip("활성라인의 안쪽 색상입니다.")]
    [FormerlySerializedAs("passedConnectionLineColor")]
    [SerializeField] private Color activeConnectionLineInnerColor = new Color(1f, 1f, 1f, 0.95f);

    [Tooltip("연결 라인의 기본 S자 꺾임 강도입니다.")]
    [SerializeField] private float connectionLineCurveOffset = 18f;

    [Tooltip("연결 라인의 S자 랜덤 흔들림 폭입니다.")]
    [SerializeField] private float connectionLineRandomOffset = 24f;

    [Tooltip("연결 라인이 노드 사이를 넓게 돌아가는 추가 폭입니다.")]
    [SerializeField] private float connectionLineDetourWidth = 0f;

    [Tooltip("연결 라인을 몇 조각으로 나눠 그릴지 정합니다.")]
    [SerializeField] private int connectionLineSegmentCount = 32;

    [Tooltip("연결 라인 랜덤 모양을 고정하기 위한 시드입니다.")]
    [SerializeField] private int connectionLineRandomSeed = 37;

    [Tooltip("연결 라인 생성 로그를 출력할지 여부입니다.")]
    [SerializeField] private bool logConnectionLineBuild;

    [Header("노드맵 이동")]
    [Tooltip("씬 입장 시 노드맵 인트로 이동을 재생할지 여부입니다.")]
    [SerializeField] private bool playMapIntroOnStart = true;

    [Tooltip("드래그 최상단에서 마지막 레이어를 화면 위에서 몇 비율 지점에 둘지 정합니다. 0.2는 5분의 1 지점입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float introLastLayerViewportRatio = 0.2f;

    [Tooltip("입장 시작과 드래그 최하단에서 첫 레이어를 화면 위에서 몇 비율 지점에 둘지 정합니다. 0.8은 5분의 4 지점입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float focusLayerViewportRatio = 0.8f;

    [Tooltip("진행 노드를 자동으로 보여주기 전에 첫 레이어 위치에서 머무는 시간입니다.")]
    [SerializeField] private float introHoldSeconds = 1f;

    [Tooltip("진행 노드 위치로 이동하는 데 걸리는 시간입니다.")]
    [SerializeField] private float introMoveSeconds = 0.5f;

    [Tooltip("진행 노드 위치로 이동할 때 사용할 보간 곡선입니다.")]
    [SerializeField] private AnimationCurve introMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("입장 후 진행 노드가 이 화면 비율보다 위쪽에 있으면 해당 위치까지 자동 이동합니다. 0.4는 5분의 2 지점입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float introFocusedNodeViewportRatio = 0.4f;

    [Tooltip("노드 클리어 후 다음 레이어로 이동하는 시간입니다.")]
    [SerializeField] private float clearMoveSeconds = 0.45f;

    [Tooltip("노드 클리어 후 다음 레이어로 이동할 때 사용할 보간 곡선입니다.")]
    [SerializeField] private AnimationCurve clearMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("자동 이동이 끝난 뒤 드래그로 노드맵을 움직일 수 있을지 여부입니다.")]
    [SerializeField] private bool enableMapDrag = true;

    [Tooltip("드래그한 거리 대비 노드맵이 움직이는 배율입니다.")]
    [SerializeField] private float mapDragSensitivity = 1f;

    private static readonly Dictionary<int, int> LastFocusedLayerByStage = new Dictionary<int, int>();

    private SO_NodeStageData currentStageData;
    private Coroutine mapMoveCoroutine;
    private bool hasCachedMapBasePositions;
    private Vector2 baseNodeParentAnchoredPosition;
    private Vector2 baseLineParentAnchoredPosition;
    private Vector2 baseMapMoveRootAnchoredPosition;
    private Vector2 baseMovingBackgroundAnchoredPosition;
    private Vector2[] baseMovingBackgroundAnchoredPositions = Array.Empty<Vector2>();
    private Vector2 currentMapOffset;
    private bool isMapDragAllowed;
    private bool isDraggingMap;
    private float previousDragPointerY;

    public bool CheatClearNodeWithoutEntering => cheatClearNodeWithoutEntering;

    private void Start()
    {
        if (buildOnStart)
        {
            Build();
        }
    }

    private void OnDisable()
    {
        StopMapMove();
        isDraggingMap = false;
        isMapDragAllowed = false;
    }

    private void Update()
    {
        UpdateMapDragInputSystem();
    }

    public void Build()
    {
        Build(true);
    }

    private void Build(bool allowIntroMove)
    {
        ResolveNodeParent();
        ResolveLineParent();
        ResolveViewportRect();
        CacheMapBasePositionsIfNeeded();
        ClearGeneratedNodes();

        currentStageData = ResolveCurrentStageData();
        if (currentStageData == null)
        {
            Debug.LogWarning("SC_NodeMapBuilder: 현재 스테이지에 맞는 노드 배치 데이터를 찾지 못했습니다.", this);
            return;
        }

        if (nodeViewPrefab == null)
        {
            Debug.LogWarning("SC_NodeMapBuilder: Node View Prefab이 비어 있어 노드를 생성할 수 없습니다.", this);
            return;
        }

        if (SC_SaveDataManager.Instance != null)
        {
            SC_SaveDataManager.Instance.EnsureNodeGraphStarted(currentStageData.StageId, currentStageData.GetFirstLayerNodeIds());
        }

        int currentPlayableLayerIndex = ResolveCurrentPlayableLayerIndex();
        int highestClearedLayerIndex = ResolveHighestClearedLayerIndex();
        Dictionary<string, NodeBuildInfo> nodeBuildInfos = BuildNodeInfoLookup();
        int focusedLayerIndex = ResolveFocusedLayerIndex(currentPlayableLayerIndex, highestClearedLayerIndex);

        if (drawConnectionLines)
        {
            BuildConnectionLines(nodeBuildInfos);
        }

        for (int layerIndex = 0; layerIndex < currentStageData.LayerCount; layerIndex++)
        {
            NodeStageLayer layer = currentStageData.GetLayer(layerIndex);
            if (layer == null || layer.NodeCount <= 0)
            {
                continue;
            }

            for (int nodeIndex = 0; nodeIndex < layer.NodeCount; nodeIndex++)
            {
                NodeStageEntry entry = layer.GetNode(nodeIndex);
                if (entry == null)
                {
                    continue;
                }

                SC_NodeView nodeView = Instantiate(nodeViewPrefab, nodeParent);
                RectTransform nodeRectTransform = nodeView.transform as RectTransform;
                if (nodeRectTransform != null)
                {
                    string resolvedNodeId = currentStageData.GetNodeId(layerIndex, nodeIndex);
                    nodeRectTransform.anchoredPosition = nodeBuildInfos.TryGetValue(resolvedNodeId, out NodeBuildInfo buildInfo)
                        ? buildInfo.Position
                        : ResolveNodePosition(entry, layerIndex, nodeIndex, layer.NodeCount);
                }

                string nodeId = currentStageData.GetNodeId(layerIndex, nodeIndex);
                bool isCleared = SC_SaveDataManager.Instance != null && SC_SaveDataManager.Instance.IsNodeCleared(currentStageData.StageId, nodeId);
                bool isUnlocked = SC_SaveDataManager.Instance != null && SC_SaveDataManager.Instance.IsNodeUnlocked(currentStageData.StageId, nodeId);
                bool isDimmed = ResolveNodeDimmed(layerIndex, isUnlocked, isCleared, currentPlayableLayerIndex, highestClearedLayerIndex);
                nodeView.Setup(currentStageData, entry, nodeId, isUnlocked, isCleared, isDimmed, this);
            }
        }

        PlayMapMove(focusedLayerIndex, allowIntroMove);
    }

    public void CompleteNode(string nodeId, NodeStageEntry nodeEntry)
    {
        if (currentStageData == null || SC_SaveDataManager.Instance == null)
        {
            return;
        }

        SC_SaveDataManager.Instance.CompleteNode(currentStageData.StageId, nodeId, nodeEntry != null ? nodeEntry.NextNodeIds : null);
        if (nodeEntry != null && nodeEntry.IsBossNode)
        {
            SC_SaveDataManager.Instance.SetStageCleared(currentStageData.StageId, true);
        }

        Build(false);
    }

    private void PlayMapMove(int focusedLayerIndex, bool allowIntroMove)
    {
        if (currentStageData == null)
        {
            return;
        }

        int stageId = currentStageData.StageId;
        int firstLayerIndex = currentStageData.GetFirstUsedLayerIndex();
        Vector2 entryOffset = ClampMapOffset(CalculateFocusLayerMapOffset(firstLayerIndex));
        Vector2 targetOffset = ClampMapOffset(CalculateFocusLayerMapOffset(focusedLayerIndex));

        if (allowIntroMove && playMapIntroOnStart)
        {
            LastFocusedLayerByStage[stageId] = focusedLayerIndex;
            Vector2 finalIntroOffset = ResolveIntroFinalMapOffset(entryOffset, focusedLayerIndex);
            StartIntroMapMove(entryOffset, finalIntroOffset, finalIntroOffset != entryOffset);
            return;
        }

        if (LastFocusedLayerByStage.TryGetValue(stageId, out int previousFocusedLayerIndex))
        {
            if (focusedLayerIndex > previousFocusedLayerIndex)
            {
                Vector2 startOffset = ClampMapOffset(CalculateFocusLayerMapOffset(previousFocusedLayerIndex));
                LastFocusedLayerByStage[stageId] = focusedLayerIndex;
                StartMapMove(startOffset, targetOffset, 0f, clearMoveSeconds, clearMoveCurve);
                return;
            }

            LastFocusedLayerByStage[stageId] = focusedLayerIndex;
            SetMapOffset(targetOffset);
            isMapDragAllowed = true;
            return;
        }

        LastFocusedLayerByStage.Add(stageId, focusedLayerIndex);
        SetMapOffset(targetOffset);
        isMapDragAllowed = true;
    }

    private void StartIntroMapMove(Vector2 startOffset, Vector2 targetOffset, bool shouldMoveAfterHold)
    {
        StopMapMove();
        isMapDragAllowed = false;
        isDraggingMap = false;
        mapMoveCoroutine = StartCoroutine(MoveMapIntroRoutine(startOffset, targetOffset, shouldMoveAfterHold));
    }

    private void StartMapMove(Vector2 startOffset, Vector2 targetOffset, float holdSeconds, float moveSeconds, AnimationCurve moveCurve)
    {
        StopMapMove();
        isMapDragAllowed = false;
        isDraggingMap = false;
        mapMoveCoroutine = StartCoroutine(MoveMapRoutine(startOffset, targetOffset, holdSeconds, moveSeconds, moveCurve));
    }

    private void StopMapMove()
    {
        if (mapMoveCoroutine == null)
        {
            return;
        }

        StopCoroutine(mapMoveCoroutine);
        mapMoveCoroutine = null;
    }

    private bool CanDragMap()
    {
        return enableMapDrag && isMapDragAllowed && currentStageData != null;
    }

    private void UpdateMapDragInputSystem()
    {
#if ENABLE_INPUT_SYSTEM
        if (!CanDragMap())
        {
            return;
        }

        if (!TryGetCurrentPointerState(out bool isPressed, out float pointerY))
        {
            isDraggingMap = false;
            return;
        }

        if (isPressed && !isDraggingMap)
        {
            isDraggingMap = true;
            previousDragPointerY = pointerY;
            return;
        }

        if (isPressed && isDraggingMap)
        {
            ApplyMapDragDelta(pointerY - previousDragPointerY);
            previousDragPointerY = pointerY;
            return;
        }

        isDraggingMap = false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private bool TryGetCurrentPointerState(out bool isPressed, out float pointerY)
    {
        if (Touchscreen.current != null)
        {
            var primaryTouch = Touchscreen.current.primaryTouch;
            if (primaryTouch.press.isPressed)
            {
                isPressed = true;
                pointerY = primaryTouch.position.ReadValue().y;
                return true;
            }
        }

        if (Mouse.current != null)
        {
            isPressed = Mouse.current.leftButton.isPressed;
            pointerY = Mouse.current.position.ReadValue().y;
            return true;
        }

        isPressed = false;
        pointerY = 0f;
        return false;
    }
#endif

    private void ApplyMapDragDelta(float screenDeltaY)
    {
        float scaledDeltaY = screenDeltaY * Mathf.Max(0f, mapDragSensitivity);
        SetMapOffset(currentMapOffset + new Vector2(0f, scaledDeltaY));
    }

    private IEnumerator MoveMapRoutine(Vector2 startOffset, Vector2 targetOffset, float holdSeconds, float moveSeconds, AnimationCurve moveCurve)
    {
        SetMapOffset(startOffset);

        float safeHoldSeconds = Mathf.Max(0f, holdSeconds);
        if (safeHoldSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(safeHoldSeconds);
        }

        float safeMoveSeconds = Mathf.Max(0f, moveSeconds);
        if (safeMoveSeconds <= 0f)
        {
            SetMapOffset(targetOffset);
            isMapDragAllowed = true;
            mapMoveCoroutine = null;
            yield break;
        }

        float elapsedSeconds = 0f;
        while (elapsedSeconds < safeMoveSeconds)
        {
            float normalizedTime = Mathf.Clamp01(elapsedSeconds / safeMoveSeconds);
            float curveTime = moveCurve != null ? moveCurve.Evaluate(normalizedTime) : normalizedTime;
            SetMapOffset(Vector2.LerpUnclamped(startOffset, targetOffset, curveTime));
            elapsedSeconds += Time.unscaledDeltaTime;
            yield return null;
        }

        SetMapOffset(targetOffset);
        isMapDragAllowed = true;
        mapMoveCoroutine = null;
    }

    private IEnumerator MoveMapIntroRoutine(Vector2 startOffset, Vector2 targetOffset, bool shouldMoveAfterHold)
    {
        if (shouldMoveAfterHold)
        {
            yield return MoveMapStepRoutine(startOffset, targetOffset, introHoldSeconds, introMoveSeconds, introMoveCurve);
        }
        else
        {
            yield return MoveMapStepRoutine(startOffset, startOffset, introHoldSeconds, 0f, introMoveCurve);
        }

        isMapDragAllowed = true;
        mapMoveCoroutine = null;
    }

    private IEnumerator MoveMapStepRoutine(Vector2 startOffset, Vector2 targetOffset, float holdSeconds, float moveSeconds, AnimationCurve moveCurve)
    {
        SetMapOffset(startOffset);

        float safeHoldSeconds = Mathf.Max(0f, holdSeconds);
        if (safeHoldSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(safeHoldSeconds);
        }

        float safeMoveSeconds = Mathf.Max(0f, moveSeconds);
        if (safeMoveSeconds <= 0f)
        {
            SetMapOffset(targetOffset);
            yield break;
        }

        float elapsedSeconds = 0f;
        while (elapsedSeconds < safeMoveSeconds)
        {
            float normalizedTime = Mathf.Clamp01(elapsedSeconds / safeMoveSeconds);
            float curveTime = moveCurve != null ? moveCurve.Evaluate(normalizedTime) : normalizedTime;
            SetMapOffset(Vector2.LerpUnclamped(startOffset, targetOffset, curveTime));
            elapsedSeconds += Time.unscaledDeltaTime;
            yield return null;
        }

        SetMapOffset(targetOffset);
    }

    private SO_NodeStageData ResolveCurrentStageData()
    {
        int selectedStage = SC_SaveDataManager.Instance != null ? SC_SaveDataManager.Instance.SelectedStage : 1;
        if (nodeStageDataList == null || nodeStageDataList.Length <= 0)
        {
            return null;
        }

        SO_NodeStageData fallbackData = null;
        for (int i = 0; i < nodeStageDataList.Length; i++)
        {
            SO_NodeStageData stageData = nodeStageDataList[i];
            if (stageData == null)
            {
                continue;
            }

            if (fallbackData == null)
            {
                fallbackData = stageData;
            }

            if (stageData.StageId == selectedStage)
            {
                return stageData;
            }
        }

        return fallbackData;
    }

    private Vector2 ResolveNodePosition(NodeStageEntry entry, int layerIndex, int nodeIndex, int nodeCountInLayer)
    {
        int safeNodeCount = Mathf.Max(1, nodeCountInLayer);
        float x = (nodeIndex - (safeNodeCount - 1) * 0.5f) * horizontalSpacing;
        Vector2 basePosition = new Vector2(x, startY + verticalSpacing * layerIndex);
        return basePosition + ResolveRandomNodeOffset(entry, layerIndex, nodeIndex);
    }

    private Vector2 ResolveRandomNodeOffset(NodeStageEntry entry, int layerIndex, int nodeIndex)
    {
        if (currentStageData == null)
        {
            return Vector2.zero;
        }

        Vector2 randomRange = currentStageData.RandomPositionRange;
        if (Mathf.Approximately(randomRange.x, 0f) && Mathf.Approximately(randomRange.y, 0f))
        {
            return Vector2.zero;
        }

        string nodeId = entry != null ? entry.NodeId : string.Empty;
        int hash = 17;
        hash = hash * 31 + layerIndex;
        hash = hash * 31 + nodeIndex;
        hash = hash * 31 + (nodeId != null ? nodeId.GetHashCode() : 0);

        return new Vector2(
            Mathf.Lerp(0f, randomRange.x, GetStableRandom01(hash, 11)),
            Mathf.Lerp(0f, randomRange.y, GetStableRandom01(hash, 12)));
    }

    private Dictionary<string, NodeBuildInfo> BuildNodeInfoLookup()
    {
        Dictionary<string, NodeBuildInfo> nodeBuildInfos = new Dictionary<string, NodeBuildInfo>();
        if (currentStageData == null)
        {
            return nodeBuildInfos;
        }

        for (int layerIndex = 0; layerIndex < currentStageData.LayerCount; layerIndex++)
        {
            NodeStageLayer layer = currentStageData.GetLayer(layerIndex);
            if (layer == null)
            {
                continue;
            }

            for (int nodeIndex = 0; nodeIndex < layer.NodeCount; nodeIndex++)
            {
                NodeStageEntry entry = layer.GetNode(nodeIndex);
                if (entry == null)
                {
                    continue;
                }

                string nodeId = currentStageData.GetNodeId(layerIndex, nodeIndex);
                if (string.IsNullOrWhiteSpace(nodeId) || nodeBuildInfos.ContainsKey(nodeId))
                {
                    continue;
                }

                Vector2 position = ResolveNodePosition(entry, layerIndex, nodeIndex, layer.NodeCount);
                nodeBuildInfos.Add(nodeId, new NodeBuildInfo(entry, position));
            }
        }

        return nodeBuildInfos;
    }

    private void BuildConnectionLines(Dictionary<string, NodeBuildInfo> nodeBuildInfos)
    {
        if (lineParent == null || nodeBuildInfos == null)
        {
            return;
        }

        foreach (KeyValuePair<string, NodeBuildInfo> pair in nodeBuildInfos)
        {
            string[] nextNodeIds = pair.Value.Entry != null ? pair.Value.Entry.NextNodeIds : Array.Empty<string>();
            for (int i = 0; i < nextNodeIds.Length; i++)
            {
                string nextNodeId = string.IsNullOrWhiteSpace(nextNodeIds[i]) ? string.Empty : nextNodeIds[i].Trim();
                if (string.IsNullOrWhiteSpace(nextNodeId) || !nodeBuildInfos.TryGetValue(nextNodeId, out NodeBuildInfo nextBuildInfo))
                {
                    continue;
                }

                CreateConnectionLine(pair.Key, nextNodeId, pair.Value.Position, nextBuildInfo.Position);
            }
        }
    }

    private void CreateConnectionLine(string fromNodeId, string toNodeId, Vector2 start, Vector2 end)
    {
        Vector2 lineStart = ConvertNodeParentPointToLineParentPoint(start);
        Vector2 lineEnd = ConvertNodeParentPointToLineParentPoint(end);
        Vector2[] pathPoints = ResolveCurvePathPoints(fromNodeId, toNodeId, lineStart, lineEnd);

        Rect lineBounds = CalculateLineBounds(pathPoints);
        Vector2 boundsCenter = lineBounds.center;

        GameObject lineObject = new GameObject($"OBJ_NodeLine_{fromNodeId}_{toNodeId}", typeof(RectTransform), typeof(CanvasGroup), typeof(SC_NodeConnectionLine));
        RectTransform lineRectTransform = lineObject.transform as RectTransform;
        lineRectTransform.SetParent(lineParent, false);
        lineRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        lineRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        lineRectTransform.pivot = new Vector2(0.5f, 0.5f);
        lineRectTransform.anchoredPosition = boundsCenter;
        lineRectTransform.sizeDelta = lineBounds.size;
        lineRectTransform.SetAsFirstSibling();

        CanvasGroup lineCanvasGroup = lineObject.GetComponent<CanvasGroup>();
        lineCanvasGroup.interactable = false;
        lineCanvasGroup.blocksRaycasts = false;

        Vector2[] localPathPoints = new Vector2[pathPoints.Length];
        for (int i = 0; i < pathPoints.Length; i++)
        {
            localPathPoints[i] = pathPoints[i] - boundsCenter;
        }

        bool isActiveConnection = IsActiveConnection(fromNodeId, toNodeId);
        Color outerColor = isActiveConnection ? activeConnectionLineOuterColor : defaultConnectionLineOuterColor;
        Color innerColor = isActiveConnection ? activeConnectionLineInnerColor : defaultConnectionLineInnerColor;

        SC_NodeConnectionLine outerLine = lineObject.GetComponent<SC_NodeConnectionLine>();
        outerLine.Setup(localPathPoints, connectionLineOuterThickness, outerColor);
        CreateInnerConnectionLine(lineRectTransform, localPathPoints, innerColor);

        if (logConnectionLineBuild)
        {
            Debug.Log($"SC_NodeMapBuilder: 라인 생성 {fromNodeId} -> {toNodeId}, active={isActiveConnection}, center={boundsCenter}, size={lineRectTransform.sizeDelta}, pointCount={localPathPoints.Length}", this);
        }
    }

    private void CreateInnerConnectionLine(RectTransform parent, Vector2[] localPathPoints, Color lineColor)
    {
        GameObject lineObject = new GameObject("OBJ_NodeLine_Inner", typeof(RectTransform), typeof(SC_NodeConnectionLine));
        RectTransform lineRectTransform = lineObject.transform as RectTransform;
        lineRectTransform.SetParent(parent, false);
        lineRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        lineRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        lineRectTransform.pivot = new Vector2(0.5f, 0.5f);
        lineRectTransform.anchoredPosition = Vector2.zero;
        lineRectTransform.sizeDelta = parent.sizeDelta;
        lineRectTransform.SetAsLastSibling();

        SC_NodeConnectionLine line = lineObject.GetComponent<SC_NodeConnectionLine>();
        line.Setup(localPathPoints, connectionLineThickness, lineColor);
    }

    private bool IsActiveConnection(string fromNodeId, string toNodeId)
    {
        if (currentStageData == null || SC_SaveDataManager.Instance == null)
        {
            return false;
        }

        bool isFromCleared = SC_SaveDataManager.Instance.IsNodeCleared(currentStageData.StageId, fromNodeId);
        bool isToCleared = SC_SaveDataManager.Instance.IsNodeCleared(currentStageData.StageId, toNodeId);
        bool isToUnlocked = SC_SaveDataManager.Instance.IsNodeUnlocked(currentStageData.StageId, toNodeId);
        return isFromCleared && (isToCleared || isToUnlocked);
    }

    private Vector2 ConvertNodeParentPointToLineParentPoint(Vector2 point)
    {
        if (nodeParent == null || lineParent == null || nodeParent == lineParent)
        {
            return point;
        }

        Vector3 worldPoint = nodeParent.TransformPoint(point);
        Vector3 lineLocalPoint = lineParent.InverseTransformPoint(worldPoint);
        return lineLocalPoint;
    }

    private Rect CalculateLineBounds(Vector2[] points)
    {
        float maxLineThickness = Mathf.Max(connectionLineThickness, connectionLineOuterThickness);
        float padding = Mathf.Max(12f, maxLineThickness * 2f);
        if (points == null || points.Length <= 0)
        {
            return new Rect(-padding, -padding, padding * 2f, padding * 2f);
        }

        float minX = points[0].x;
        float maxX = points[0].x;
        float minY = points[0].y;
        float maxY = points[0].y;
        for (int i = 1; i < points.Length; i++)
        {
            minX = Mathf.Min(minX, points[i].x);
            maxX = Mathf.Max(maxX, points[i].x);
            minY = Mathf.Min(minY, points[i].y);
            maxY = Mathf.Max(maxY, points[i].y);
        }

        minX -= padding;
        maxX += padding;
        minY -= padding;
        maxY += padding;
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private Vector2[] ResolveCurvePathPoints(string fromNodeId, string toNodeId, Vector2 start, Vector2 end)
    {
        int hash = connectionLineRandomSeed;
        hash = hash * 31 + (fromNodeId != null ? fromNodeId.GetHashCode() : 0);
        hash = hash * 31 + (toNodeId != null ? toNodeId.GetHashCode() : 0);

        Vector2 direction = end - start;
        Vector2 perpendicular = direction.sqrMagnitude > 0.001f
            ? new Vector2(-direction.y, direction.x).normalized
            : Vector2.right;

        bool useThreeBends = GetStableRandom01(hash, 1) >= 0.5f;
        int bendCount = useThreeBends ? 3 : 2;
        int sampleCount = Mathf.Max(8, connectionLineSegmentCount);
        Vector2[] points = new Vector2[sampleCount + 1];

        float directionSign = GetStableRandom01(hash, 2) < 0.5f ? -1f : 1f;
        float amplitudeRandom = Mathf.Lerp(-connectionLineRandomOffset, connectionLineRandomOffset, GetStableRandom01(hash, 3));
        float amplitude = Mathf.Max(0f, connectionLineCurveOffset + connectionLineDetourWidth + amplitudeRandom);
        float phase = Mathf.Lerp(-0.2f, 0.2f, GetStableRandom01(hash, 4));

        for (int i = 0; i <= sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float envelope = Mathf.Sin(Mathf.PI * t);
            float wave = Mathf.Sin(Mathf.PI * (bendCount * t + phase));
            Vector2 basePoint = Vector2.Lerp(start, end, t);
            points[i] = basePoint + perpendicular * directionSign * wave * envelope * amplitude;
        }

        points[0] = start;
        points[points.Length - 1] = end;
        return points;
    }

    private static float GetStableRandom01(int hash, int salt)
    {
        return Mathf.Abs(Mathf.Sin((hash + salt * 101) * 12.9898f) * 43758.5453f) % 1f;
    }

    private bool ResolveNodeDimmed(int layerIndex, bool isUnlocked, bool isCleared, int currentPlayableLayerIndex, int highestClearedLayerIndex)
    {
        if (isCleared)
        {
            return true;
        }

        if (isUnlocked)
        {
            return false;
        }

        bool isBelowCurrentPlayableLayer = currentPlayableLayerIndex != int.MaxValue && layerIndex < currentPlayableLayerIndex;
        bool isInClearedPassedLayer = highestClearedLayerIndex >= 0 && layerIndex <= highestClearedLayerIndex;
        return isBelowCurrentPlayableLayer || isInClearedPassedLayer;
    }

    private int ResolveCurrentPlayableLayerIndex()
    {
        if (currentStageData == null || SC_SaveDataManager.Instance == null)
        {
            return int.MaxValue;
        }

        for (int layerIndex = 0; layerIndex < currentStageData.LayerCount; layerIndex++)
        {
            NodeStageLayer layer = currentStageData.GetLayer(layerIndex);
            if (layer == null)
            {
                continue;
            }

            for (int nodeIndex = 0; nodeIndex < layer.NodeCount; nodeIndex++)
            {
                string nodeId = currentStageData.GetNodeId(layerIndex, nodeIndex);
                bool isUnlocked = SC_SaveDataManager.Instance.IsNodeUnlocked(currentStageData.StageId, nodeId);
                bool isCleared = SC_SaveDataManager.Instance.IsNodeCleared(currentStageData.StageId, nodeId);
                if (isUnlocked && !isCleared)
                {
                    return layerIndex;
                }
            }
        }

        return int.MaxValue;
    }

    private int ResolveHighestClearedLayerIndex()
    {
        if (currentStageData == null || SC_SaveDataManager.Instance == null)
        {
            return -1;
        }

        int highestClearedLayerIndex = -1;
        for (int layerIndex = 0; layerIndex < currentStageData.LayerCount; layerIndex++)
        {
            NodeStageLayer layer = currentStageData.GetLayer(layerIndex);
            if (layer == null)
            {
                continue;
            }

            for (int nodeIndex = 0; nodeIndex < layer.NodeCount; nodeIndex++)
            {
                string nodeId = currentStageData.GetNodeId(layerIndex, nodeIndex);
                if (SC_SaveDataManager.Instance.IsNodeCleared(currentStageData.StageId, nodeId))
                {
                    highestClearedLayerIndex = Mathf.Max(highestClearedLayerIndex, layerIndex);
                }
            }
        }

        return highestClearedLayerIndex;
    }

    private int ResolveFocusedLayerIndex(int currentPlayableLayerIndex, int highestClearedLayerIndex)
    {
        if (currentPlayableLayerIndex != int.MaxValue)
        {
            return currentPlayableLayerIndex;
        }

        if (highestClearedLayerIndex >= 0)
        {
            return highestClearedLayerIndex;
        }

        return currentStageData != null ? currentStageData.GetFirstUsedLayerIndex() : 0;
    }

    private Vector2 CalculateIntroStartMapOffset()
    {
        int lastUsedLayerIndex = ResolveLastUsedLayerIndex();
        float targetY = ResolveViewportRatioLocalY(introLastLayerViewportRatio);
        float layerY = ResolveLayerCenterY(lastUsedLayerIndex);
        return new Vector2(0f, targetY - layerY);
    }

    private Vector2 CalculateFocusLayerMapOffset(int layerIndex)
    {
        float targetY = ResolveViewportRatioLocalY(focusLayerViewportRatio);
        float layerY = ResolveLayerCenterY(layerIndex);
        return new Vector2(0f, targetY - layerY);
    }

    private Vector2 ResolveIntroFinalMapOffset(Vector2 entryOffset, int focusedLayerIndex)
    {
        float focusedLayerScreenY = ResolveLayerCenterY(focusedLayerIndex) + entryOffset.y;
        float focusedNodeLimitY = ResolveViewportRatioLocalY(introFocusedNodeViewportRatio);
        if (focusedLayerScreenY <= focusedNodeLimitY)
        {
            return entryOffset;
        }

        Vector2 targetOffset = new Vector2(0f, focusedNodeLimitY - ResolveLayerCenterY(focusedLayerIndex));
        return ClampMapOffset(targetOffset);
    }

    private float ResolveViewportRatioLocalY(float viewportRatio)
    {
        float clampedRatio = Mathf.Clamp01(viewportRatio);
        return ResolveViewportHeight() * (0.5f - clampedRatio);
    }

    private float ResolveViewportHeight()
    {
        if (viewportRect != null && viewportRect.rect.height > 0f)
        {
            return viewportRect.rect.height;
        }

        return Screen.height;
    }

    private float ResolveLayerCenterY(int layerIndex)
    {
        if (currentStageData == null)
        {
            return startY + verticalSpacing * layerIndex;
        }

        NodeStageLayer layer = currentStageData.GetLayer(layerIndex);
        if (layer == null || layer.NodeCount <= 0)
        {
            return startY + verticalSpacing * layerIndex;
        }

        float totalY = 0f;
        int validNodeCount = 0;
        for (int nodeIndex = 0; nodeIndex < layer.NodeCount; nodeIndex++)
        {
            NodeStageEntry entry = layer.GetNode(nodeIndex);
            if (entry == null)
            {
                continue;
            }

            totalY += ResolveNodePosition(entry, layerIndex, nodeIndex, layer.NodeCount).y;
            validNodeCount++;
        }

        return validNodeCount > 0 ? totalY / validNodeCount : startY + verticalSpacing * layerIndex;
    }

    private int ResolveLastUsedLayerIndex()
    {
        if (currentStageData == null)
        {
            return 0;
        }

        for (int layerIndex = currentStageData.LayerCount - 1; layerIndex >= 0; layerIndex--)
        {
            NodeStageLayer layer = currentStageData.GetLayer(layerIndex);
            if (layer != null && layer.NodeCount > 0)
            {
                return layerIndex;
            }
        }

        return currentStageData.GetFirstUsedLayerIndex();
    }

    private void SetMapOffset(Vector2 offset)
    {
        Vector2 clampedOffset = ClampMapOffset(offset);
        currentMapOffset = clampedOffset;

        if (mapMoveRoot != null)
        {
            mapMoveRoot.anchoredPosition = baseMapMoveRootAnchoredPosition + clampedOffset;
            SetMovingBackgroundOffset(clampedOffset, mapMoveRoot);
            return;
        }

        if (lineParent != null && nodeParent != null && nodeParent.IsChildOf(lineParent))
        {
            lineParent.anchoredPosition = baseLineParentAnchoredPosition + clampedOffset;
            SetMovingBackgroundOffset(clampedOffset, lineParent);
            return;
        }

        if (nodeParent != null)
        {
            nodeParent.anchoredPosition = baseNodeParentAnchoredPosition + clampedOffset;
        }

        if (lineParent != null && lineParent != nodeParent && (nodeParent == null || !lineParent.IsChildOf(nodeParent)))
        {
            lineParent.anchoredPosition = baseLineParentAnchoredPosition + clampedOffset;
        }

        SetMovingBackgroundOffset(clampedOffset, null);
    }

    private void SetMovingBackgroundOffset(Vector2 offset, RectTransform movedRoot)
    {
        Vector2 backgroundOffset = offset * movingBackgroundMoveRatio;

        SetSingleMovingBackgroundOffset(movingBackground, baseMovingBackgroundAnchoredPosition, backgroundOffset, movedRoot);

        if (movingBackgrounds == null)
        {
            return;
        }

        for (int i = 0; i < movingBackgrounds.Length; i++)
        {
            RectTransform background = movingBackgrounds[i];
            Vector2 basePosition = i < baseMovingBackgroundAnchoredPositions.Length
                ? baseMovingBackgroundAnchoredPositions[i]
                : Vector2.zero;
            SetSingleMovingBackgroundOffset(background, basePosition, backgroundOffset, movedRoot);
        }
    }

    private void SetSingleMovingBackgroundOffset(RectTransform background, Vector2 basePosition, Vector2 offset, RectTransform movedRoot)
    {
        if (background == null)
        {
            return;
        }

        if (movedRoot != null && (background == movedRoot || background.IsChildOf(movedRoot)))
        {
            return;
        }

        background.anchoredPosition = basePosition + offset;
    }

    private Vector2 ClampMapOffset(Vector2 offset)
    {
        Vector2 topLimitOffset = CalculateIntroStartMapOffset();
        Vector2 bottomLimitOffset = CalculateIntroBottomMapOffset();
        float minY = Mathf.Min(topLimitOffset.y, bottomLimitOffset.y);
        float maxY = Mathf.Max(topLimitOffset.y, bottomLimitOffset.y);
        offset.y = Mathf.Clamp(offset.y, minY, maxY);
        return offset;
    }

    private Vector2 CalculateIntroBottomMapOffset()
    {
        if (currentStageData == null)
        {
            return Vector2.zero;
        }

        return CalculateFocusLayerMapOffset(currentStageData.GetFirstUsedLayerIndex());
    }

    private void CacheMapBasePositionsIfNeeded()
    {
        if (hasCachedMapBasePositions)
        {
            return;
        }

        baseNodeParentAnchoredPosition = nodeParent != null ? nodeParent.anchoredPosition : Vector2.zero;
        baseLineParentAnchoredPosition = lineParent != null ? lineParent.anchoredPosition : Vector2.zero;
        baseMapMoveRootAnchoredPosition = mapMoveRoot != null ? mapMoveRoot.anchoredPosition : Vector2.zero;
        baseMovingBackgroundAnchoredPosition = movingBackground != null ? movingBackground.anchoredPosition : Vector2.zero;
        baseMovingBackgroundAnchoredPositions = CacheMovingBackgroundBasePositions();
        hasCachedMapBasePositions = true;
    }

    private Vector2[] CacheMovingBackgroundBasePositions()
    {
        if (movingBackgrounds == null || movingBackgrounds.Length <= 0)
        {
            return Array.Empty<Vector2>();
        }

        Vector2[] basePositions = new Vector2[movingBackgrounds.Length];
        for (int i = 0; i < movingBackgrounds.Length; i++)
        {
            basePositions[i] = movingBackgrounds[i] != null ? movingBackgrounds[i].anchoredPosition : Vector2.zero;
        }

        return basePositions;
    }

    private void ResolveNodeParent()
    {
        if (nodeParent == null)
        {
            nodeParent = transform as RectTransform;
        }
    }

    private void ResolveLineParent()
    {
        if (lineParent == null)
        {
            lineParent = nodeParent;
        }
    }

    private void ResolveViewportRect()
    {
        if (viewportRect != null)
        {
            return;
        }

        if (mapMoveRoot != null && mapMoveRoot.parent is RectTransform mapMoveRootParent)
        {
            viewportRect = mapMoveRootParent;
            return;
        }

        if (nodeParent != null && nodeParent.parent is RectTransform nodeParentParent)
        {
            viewportRect = nodeParentParent;
            return;
        }

        viewportRect = transform as RectTransform;
    }

    private void ClearGeneratedNodes()
    {
        if (nodeParent == null)
        {
            return;
        }

        ClearChildren(nodeParent);

        if (lineParent != null && lineParent != nodeParent)
        {
            ClearChildren(lineParent);
        }
    }

    private static void ClearChildren(RectTransform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
