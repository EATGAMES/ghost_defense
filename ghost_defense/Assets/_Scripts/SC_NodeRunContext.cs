public static class SC_NodeRunContext
{
    public const string NodeSceneName = "SCN_Node";

    public static SO_NodeStageData CurrentStageData { get; private set; }
    public static NodeStageEntry CurrentNodeEntry { get; private set; }
    public static int CurrentStageId { get; private set; }
    public static string CurrentNodeId { get; private set; } = string.Empty;
    public static SO_MonsterData CurrentMonsterData => CurrentNodeEntry != null ? CurrentNodeEntry.MonsterData : null;
    public static bool HasActiveNode => CurrentStageData != null && CurrentNodeEntry != null && !string.IsNullOrWhiteSpace(CurrentNodeId);

    public static void SelectNode(SO_NodeStageData stageData, NodeStageEntry nodeEntry, string nodeId)
    {
        CurrentStageData = stageData;
        CurrentNodeEntry = nodeEntry;
        CurrentStageId = stageData != null ? stageData.StageId : 1;
        CurrentNodeId = string.IsNullOrWhiteSpace(nodeId) ? string.Empty : nodeId.Trim();
    }

    public static void Clear()
    {
        CurrentStageData = null;
        CurrentNodeEntry = null;
        CurrentStageId = 1;
        CurrentNodeId = string.Empty;
    }

    public static bool IsCurrentNodeCleared()
    {
        return HasActiveNode
            && SC_SaveDataManager.Instance != null
            && SC_SaveDataManager.Instance.IsNodeCleared(CurrentStageId, CurrentNodeId);
    }

    public static void MarkCurrentNodeCleared()
    {
        if (!HasActiveNode || SC_SaveDataManager.Instance == null)
        {
            return;
        }

        SC_SaveDataManager.Instance.CompleteNode(CurrentStageId, CurrentNodeId, CurrentNodeEntry.NextNodeIds);

        if (CurrentNodeEntry != null && CurrentNodeEntry.IsBossNode)
        {
            SC_SaveDataManager.Instance.SetStageCleared(CurrentStageId, true);
        }
    }
}
