using System;
using System.Collections.Generic;
using UnityEngine;

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

    [Tooltip("연결 라인의 색상입니다.")]
    [SerializeField] private Color connectionLineColor = new Color(1f, 1f, 1f, 0.45f);

    [Tooltip("연결 라인의 두께입니다.")]
    [SerializeField] private float connectionLineThickness = 6f;

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

    private SO_NodeStageData currentStageData;

    public bool CheatClearNodeWithoutEntering => cheatClearNodeWithoutEntering;

    private void Start()
    {
        if (buildOnStart)
        {
            Build();
        }
    }

    public void Build()
    {
        ResolveNodeParent();
        ResolveLineParent();
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

        Build();
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
        if (entry.UseCustomAnchoredPosition)
        {
            return entry.AnchoredPosition;
        }

        int safeNodeCount = Mathf.Max(1, nodeCountInLayer);
        float x = (nodeIndex - (safeNodeCount - 1) * 0.5f) * horizontalSpacing;
        return new Vector2(x, startY + verticalSpacing * layerIndex);
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

        SC_NodeConnectionLine line = lineObject.GetComponent<SC_NodeConnectionLine>();
        line.Setup(localPathPoints, connectionLineThickness, connectionLineColor);

        if (logConnectionLineBuild)
        {
            Debug.Log($"SC_NodeMapBuilder: 라인 생성 {fromNodeId} -> {toNodeId}, center={boundsCenter}, size={lineRectTransform.sizeDelta}, pointCount={localPathPoints.Length}", this);
        }
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
        float padding = Mathf.Max(12f, connectionLineThickness * 2f);
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
