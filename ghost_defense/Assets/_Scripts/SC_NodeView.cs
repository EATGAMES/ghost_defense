using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_NodeView : MonoBehaviour
{
    [Tooltip("노드 클릭을 받을 버튼입니다.")]
    [SerializeField] private Button button;

    [Tooltip("노드 아이콘을 표시할 이미지입니다.")]
    [SerializeField] private Image iconImage;

    [Tooltip("노드 이름을 표시할 TMP 텍스트입니다.")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("지나간 노드나 선택하지 않은 지나간 갈래일 때 노드 그래픽 색상에 곱할 어둡기 값입니다.")]
    [SerializeField] private Color dimColorMultiplier = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Tooltip("클리어한 노드일 때 켤 오브젝트입니다.")]
    [SerializeField] private GameObject clearedObject;

    [Tooltip("현재 진행 가능한 노드일 때 켤 오브젝트입니다.")]
    [SerializeField] private GameObject currentObject;

    [Tooltip("몬스터 데이터의 전투 방향이 UP일 때 이동할 씬 이름입니다.")]
    [SerializeField] private string upBattleSceneName = "SCN_Battle";

    [Tooltip("몬스터 데이터의 전투 방향이 DOWN일 때 이동할 씬 이름입니다.")]
    [SerializeField] private string downBattleSceneName = "SCN_Battle_Drop";

    private SO_NodeStageData stageData;
    private NodeStageEntry nodeEntry;
    private SC_NodeMapBuilder mapBuilder;
    private string nodeId = string.Empty;
    private bool isUnlocked;
    private bool isCleared;
    private bool isDimmed;
    private Graphic[] cachedGraphics;
    private Color[] originalGraphicColors;
    private bool isClickProcessing;

    private void Awake()
    {
        ResolveReferences();

        if (button != null)
        {
            button.onClick.AddListener(OnClickNode);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClickNode);
        }
    }

    public void Setup(SO_NodeStageData stageData, NodeStageEntry nodeEntry, string nodeId, bool isUnlocked, bool isCleared, bool isDimmed, SC_NodeMapBuilder mapBuilder)
    {
        this.stageData = stageData;
        this.nodeEntry = nodeEntry;
        this.nodeId = string.IsNullOrWhiteSpace(nodeId) ? string.Empty : nodeId.Trim();
        this.isUnlocked = isUnlocked;
        this.isCleared = isCleared;
        this.isDimmed = isDimmed;
        this.mapBuilder = mapBuilder;

        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        if (nameText != null)
        {
            nameText.text = nodeEntry != null ? nodeEntry.DisplayName : string.Empty;
        }

        if (iconImage != null)
        {
            Sprite icon = nodeEntry != null ? nodeEntry.Icon : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (button != null)
        {
            button.interactable = true;
        }

        RefreshDimState();

        if (clearedObject != null)
        {
            clearedObject.SetActive(isCleared);
        }

        if (currentObject != null)
        {
            currentObject.SetActive(isUnlocked && !isCleared);
        }
    }

    private void OnClickNode()
    {
        if (isClickProcessing)
        {
            return;
        }

        isClickProcessing = true;

        if (nodeEntry == null)
        {
            isClickProcessing = false;
            return;
        }

        if (isCleared)
        {
            isClickProcessing = false;
            return;
        }

        if (mapBuilder != null && mapBuilder.CheatClearNodeWithoutEntering)
        {
            mapBuilder.CompleteNode(nodeId, nodeEntry);
            return;
        }

        if (!isUnlocked)
        {
            isClickProcessing = false;
            return;
        }

        SC_NodeRunContext.SelectNode(stageData, nodeEntry, nodeId);
        string targetSceneName = ResolveTargetSceneName();

        if (!string.IsNullOrWhiteSpace(targetSceneName))
        {
            if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                Debug.LogWarning($"노드 이동 씬 '{targetSceneName}'이(가) Build Profiles에 없어 로드할 수 없습니다.", this);
                isClickProcessing = false;
                return;
            }

            SceneManager.LoadScene(targetSceneName);
            return;
        }

        if (nodeEntry.ClearImmediatelyWhenNoScene && mapBuilder != null)
        {
            SC_NodeRunContext.Clear();
            mapBuilder.CompleteNode(nodeId, nodeEntry);
            return;
        }

        isClickProcessing = false;
    }

    private void ResolveReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        CacheGraphicColorsIfNeeded();
    }

    private void RefreshDimState()
    {
        CacheGraphicColorsIfNeeded();

        if (cachedGraphics == null || originalGraphicColors == null)
        {
            return;
        }

        for (int i = 0; i < cachedGraphics.Length; i++)
        {
            Graphic graphic = cachedGraphics[i];
            if (graphic == null || i >= originalGraphicColors.Length)
            {
                continue;
            }

            graphic.color = isDimmed ? MultiplyColor(originalGraphicColors[i], dimColorMultiplier) : originalGraphicColors[i];
        }
    }

    private void CacheGraphicColorsIfNeeded()
    {
        if (cachedGraphics != null && originalGraphicColors != null && cachedGraphics.Length == originalGraphicColors.Length)
        {
            return;
        }

        cachedGraphics = GetComponentsInChildren<Graphic>(true);
        originalGraphicColors = new Color[cachedGraphics.Length];
        for (int i = 0; i < cachedGraphics.Length; i++)
        {
            originalGraphicColors[i] = cachedGraphics[i] != null ? cachedGraphics[i].color : Color.white;
        }
    }

    private static Color MultiplyColor(Color baseColor, Color multiplier)
    {
        return new Color(
            baseColor.r * multiplier.r,
            baseColor.g * multiplier.g,
            baseColor.b * multiplier.b,
            baseColor.a * multiplier.a);
    }

    private string ResolveTargetSceneName()
    {
        string targetSceneName = nodeEntry.ResolveTargetSceneName();
        if (!nodeEntry.IsBattleNode || nodeEntry.MonsterData == null)
        {
            return targetSceneName;
        }

        if (!string.IsNullOrWhiteSpace(targetSceneName))
        {
            return targetSceneName;
        }

        return nodeEntry.MonsterData.StageBattleDirection == StageBattleDirection.DOWN
            ? downBattleSceneName
            : upBattleSceneName;
    }
}
