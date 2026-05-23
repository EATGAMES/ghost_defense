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

    [Tooltip("전투 노드에서 사용할 몬스터 데이터입니다.")]
    [SerializeField] private SO_MonsterData monsterData;

    [Tooltip("이 노드를 클리어한 뒤 열릴 다음 노드 ID 목록입니다.")]
    [SerializeField] private string[] nextNodeIds = Array.Empty<string>();

    public string NodeId => nodeId;
    public NodeDungeonType NodeType => nodeType;
    public SO_MonsterData MonsterData => monsterData;
    public string[] NextNodeIds => nextNodeIds ?? Array.Empty<string>();
    public bool IsBattleNode => monsterData != null || nodeType == NodeDungeonType.Normal || nodeType == NodeDungeonType.Hard || nodeType == NodeDungeonType.Boss;
    public bool IsBossNode => nodeType == NodeDungeonType.Boss;
}

[Serializable]
public class NodeDungeonTypeIconSet
{
    [Tooltip("일반 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite normalIcon;

    [Tooltip("어려움 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite hardIcon;

    [Tooltip("카드점 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite cardShopIcon;

    [Tooltip("거래상 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite merchantIcon;

    [Tooltip("이벤트 A 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite eventAIcon;

    [Tooltip("이벤트 B 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite eventBIcon;

    [Tooltip("이벤트 C 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite eventCIcon;

    [Tooltip("보스 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite bossIcon;

    public Sprite GetIcon(NodeDungeonType type)
    {
        switch (type)
        {
            case NodeDungeonType.Normal:
                return normalIcon;
            case NodeDungeonType.Hard:
                return hardIcon;
            case NodeDungeonType.CardShop:
                return cardShopIcon;
            case NodeDungeonType.Merchant:
                return merchantIcon;
            case NodeDungeonType.EventA:
                return eventAIcon;
            case NodeDungeonType.EventB:
                return eventBIcon;
            case NodeDungeonType.EventC:
                return eventCIcon;
            case NodeDungeonType.Boss:
                return bossIcon;
            default:
                return null;
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

    [Tooltip("노드 타입별로 공통 사용될 아이콘 목록입니다.")]
    [SerializeField] private NodeDungeonTypeIconSet typeIcons = new NodeDungeonTypeIconSet();

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

    [Header("랜덤 위치")]
    [Tooltip("노드 아이콘 생성 시 0부터 이 X값까지 랜덤하게 더할 위치입니다.")]
    [SerializeField] private float randomPositionX;

    [Tooltip("노드 아이콘 생성 시 0부터 이 Y값까지 랜덤하게 더할 위치입니다.")]
    [SerializeField] private float randomPositionY;

    public int StageId => Mathf.Max(1, stageId);
    public int LayerCount => layers != null ? layers.Length : 0;
    public Vector2 RandomPositionRange => new Vector2(randomPositionX, randomPositionY);

    public Sprite GetNodeIcon(NodeDungeonType type)
    {
        return typeIcons != null ? typeIcons.GetIcon(type) : null;
    }

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
