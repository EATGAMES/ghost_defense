using System;
using UnityEngine;

public enum NodeDungeonType
{
    Normal,
    Hard,
    CardShop,
    Merchant,
    EventA,
    EventB,
    EventC,
    Boss
}

[Serializable]
public class NodeStageEntry
{
    [Tooltip("노드를 구분할 ID입니다. 001, 002처럼 3자리 숫자 형식을 권장합니다.")]
    [SerializeField] private string nodeId = "001";

    [Tooltip("노드 타입입니다.")]
    [SerializeField] private NodeDungeonType nodeType = NodeDungeonType.Normal;

    [Tooltip("노드에 표시할 이름입니다. 비워두면 노드 타입 이름을 사용합니다.")]
    [SerializeField] private string displayName;

    [Tooltip("노드 버튼에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite icon;

    [Tooltip("전투 노드에서 사용할 몬스터 데이터입니다.")]
    [SerializeField] private SO_MonsterData monsterData;

    [Tooltip("클릭 시 직접 이동할 씬 이름입니다. 상점이나 이벤트처럼 전투가 아닌 노드에 사용합니다.")]
    [SerializeField] private string targetSceneName;

    [Tooltip("이동할 씬이 없을 때 클릭 즉시 클리어 처리할지 여부입니다.")]
    [SerializeField] private bool clearImmediatelyWhenNoScene = true;

    [Tooltip("이 노드를 클리어한 뒤 열릴 다음 노드 ID 목록입니다.")]
    [SerializeField] private string[] nextNodeIds = Array.Empty<string>();

    [Tooltip("이 노드만 직접 지정한 UI 위치를 사용할지 여부입니다.")]
    [SerializeField] private bool useCustomAnchoredPosition;

    [Tooltip("노드 버튼의 RectTransform Anchored Position입니다.")]
    [SerializeField] private Vector2 anchoredPosition;

    public string NodeId => nodeId;
    public NodeDungeonType NodeType => nodeType;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? GetDefaultDisplayName(nodeType) : displayName;
    public Sprite Icon => icon;
    public SO_MonsterData MonsterData => monsterData;
    public bool ClearImmediatelyWhenNoScene => clearImmediatelyWhenNoScene;
    public string[] NextNodeIds => nextNodeIds ?? Array.Empty<string>();
    public bool UseCustomAnchoredPosition => useCustomAnchoredPosition;
    public Vector2 AnchoredPosition => anchoredPosition;
    public bool IsBattleNode => monsterData != null || nodeType == NodeDungeonType.Normal || nodeType == NodeDungeonType.Hard || nodeType == NodeDungeonType.Boss;
    public bool IsBossNode => nodeType == NodeDungeonType.Boss;

    public string ResolveTargetSceneName()
    {
        return string.IsNullOrWhiteSpace(targetSceneName) ? string.Empty : targetSceneName;
    }

    public static string GetDefaultDisplayName(NodeDungeonType type)
    {
        switch (type)
        {
            case NodeDungeonType.Normal:
                return "일반";
            case NodeDungeonType.Hard:
                return "어려움";
            case NodeDungeonType.CardShop:
                return "카드점";
            case NodeDungeonType.Merchant:
                return "거래상";
            case NodeDungeonType.EventA:
                return "이벤트 A";
            case NodeDungeonType.EventB:
                return "이벤트 B";
            case NodeDungeonType.EventC:
                return "이벤트 C";
            case NodeDungeonType.Boss:
                return "보스";
            default:
                return type.ToString();
        }
    }
}

[Serializable]
public class NodeStageLayer
{
    [Tooltip("이 레이어에 배치할 노드 목록입니다.")]
    [SerializeField] private NodeStageEntry[] nodes = Array.Empty<NodeStageEntry>();

    public int NodeCount => nodes != null ? nodes.Length : 0;

    public NodeStageEntry GetNode(int index)
    {
        if (nodes == null || index < 0 || index >= nodes.Length)
        {
            return null;
        }

        return nodes[index];
    }
}

[CreateAssetMenu(fileName = "SO_NodeStageData", menuName = "Ghost Defense/Node Stage Data")]
public class SO_NodeStageData : ScriptableObject
{
    [Tooltip("이 노드 배치를 사용할 스테이지 번호입니다.")]
    [SerializeField] private int stageId = 1;

    [Tooltip("아래에서 위로 사용할 10개 레이어입니다. 비어 있는 레이어는 무시됩니다.")]
    [SerializeField] private NodeStageLayer[] layers =
    {
        new NodeStageLayer(),
        new NodeStageLayer(),
        new NodeStageLayer(),
        new NodeStageLayer(),
        new NodeStageLayer(),
        new NodeStageLayer(),
        new NodeStageLayer(),
        new NodeStageLayer(),
        new NodeStageLayer(),
        new NodeStageLayer()
    };

    public int StageId => Mathf.Max(1, stageId);
    public int LayerCount => layers != null ? layers.Length : 0;

    public NodeStageLayer GetLayer(int layerIndex)
    {
        if (layers == null || layerIndex < 0 || layerIndex >= layers.Length)
        {
            return null;
        }

        return layers[layerIndex];
    }

    public NodeStageEntry GetNode(int layerIndex, int nodeIndex)
    {
        NodeStageLayer layer = GetLayer(layerIndex);
        return layer != null ? layer.GetNode(nodeIndex) : null;
    }

    public string GetNodeId(int layerIndex, int nodeIndex)
    {
        NodeStageEntry node = GetNode(layerIndex, nodeIndex);
        if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
        {
            return ((layerIndex + 1) * 100 + nodeIndex + 1).ToString("000");
        }

        return node.NodeId.Trim();
    }

    public int GetFirstUsedLayerIndex()
    {
        for (int i = 0; i < LayerCount; i++)
        {
            NodeStageLayer layer = GetLayer(i);
            if (layer != null && layer.NodeCount > 0)
            {
                return i;
            }
        }

        return 0;
    }

    public string[] GetFirstLayerNodeIds()
    {
        int firstLayerIndex = GetFirstUsedLayerIndex();
        NodeStageLayer firstLayer = GetLayer(firstLayerIndex);
        if (firstLayer == null || firstLayer.NodeCount <= 0)
        {
            return Array.Empty<string>();
        }

        string[] firstLayerNodeIds = new string[firstLayer.NodeCount];
        for (int i = 0; i < firstLayer.NodeCount; i++)
        {
            firstLayerNodeIds[i] = GetNodeId(firstLayerIndex, i);
        }

        return firstLayerNodeIds;
    }
}
