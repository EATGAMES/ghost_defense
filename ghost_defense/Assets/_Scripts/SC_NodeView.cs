using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
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

[DisallowMultipleComponent]
public class SC_NodeView : MonoBehaviour
{
    [Tooltip("노드 클릭을 받을 버튼입니다.")]
    [SerializeField] private Button button;

    [Tooltip("노드 아이콘을 표시할 이미지입니다.")]
    [SerializeField] private Image iconImage;

    [Tooltip("노드 이름을 표시할 TMP 텍스트입니다.")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("노드 타입별로 표시할 아이콘 목록입니다.")]
    [SerializeField] private NodeDungeonTypeIconSet typeIcons = new NodeDungeonTypeIconSet();

    [Header("랜덤 위치")]
    [Tooltip("노드 아이콘 생성 시 0부터 이 X값까지 랜덤하게 더할 위치입니다.")]
    [SerializeField] private float randomPositionX;

    [Tooltip("노드 아이콘 생성 시 0부터 이 Y값까지 랜덤하게 더할 위치입니다.")]
    [SerializeField] private float randomPositionY;

    [Tooltip("클리어한 노드일 때 켤 오브젝트입니다.")]
    [SerializeField] private GameObject clearedObject;

    [Tooltip("현재 진행 가능한 노드일 때 켤 오브젝트입니다.")]
    [SerializeField] private GameObject currentObject;

    [Tooltip("몬스터 데이터의 전투 방향이 UP일 때 이동할 씬 이름입니다.")]
    [SerializeField] private string upBattleSceneName = "SCN_Battle";

    [Tooltip("몬스터 데이터의 전투 방향이 DOWN일 때 이동할 씬 이름입니다.")]
    [SerializeField] private string downBattleSceneName = "SCN_Battle_Drop";

    [Header("팝업")]
    [Tooltip("카드샵 노드를 눌렀을 때 켤 팝업 오브젝트입니다. 비워두면 아무 동작도 하지 않습니다.")]
    [SerializeField] private GameObject cardShopPopup;

    [Tooltip("상인 노드를 눌렀을 때 켤 팝업 오브젝트입니다. 비워두면 아무 동작도 하지 않습니다.")]
    [SerializeField] private GameObject merchantPopup;

    [Header("치트")]
    [Tooltip("체크하면 일반 노드를 클릭했을 때 전투에 들어가지 않고 즉시 클리어 처리합니다.")]
    [SerializeField] private bool cheatClearNormalNode;

    [Tooltip("체크하면 어려움 노드를 클릭했을 때 전투에 들어가지 않고 즉시 클리어 처리합니다.")]
    [SerializeField] private bool cheatClearHardNode;

    [Tooltip("체크하면 카드샵 노드를 클릭했을 때 팝업을 열지 않고 즉시 클리어 처리합니다.")]
    [SerializeField] private bool cheatClearCardShopNode;

    [Tooltip("체크하면 상인 노드를 클릭했을 때 팝업을 열지 않고 즉시 클리어 처리합니다.")]
    [SerializeField] private bool cheatClearMerchantNode;

    [Tooltip("체크하면 이벤트 A 노드를 클릭했을 때 전투에 들어가지 않고 즉시 클리어 처리합니다.")]
    [SerializeField] private bool cheatClearEventANode;

    [Tooltip("체크하면 이벤트 B 노드를 클릭했을 때 전투에 들어가지 않고 즉시 클리어 처리합니다.")]
    [SerializeField] private bool cheatClearEventBNode;

    [Tooltip("체크하면 이벤트 C 노드를 클릭했을 때 전투에 들어가지 않고 즉시 클리어 처리합니다.")]
    [SerializeField] private bool cheatClearEventCNode;

    [Tooltip("체크하면 보스 노드를 클릭했을 때 전투에 들어가지 않고 즉시 클리어 처리합니다.")]
    [SerializeField] private bool cheatClearBossNode;

    [Tooltip("현재 진행 가능한 노드가 꿀렁일 때 커질 최대 스케일 배율입니다.")]
    [SerializeField] private float currentPulseScale = 1.08f;

    [Tooltip("현재 진행 가능한 노드가 한 번 꿀렁이는 데 걸리는 시간입니다.")]
    [SerializeField] private float currentPulseSeconds = 0.8f;

    private SO_NodeStageData stageData;
    private NodeStageEntry nodeEntry;
    private SC_NodeMapBuilder mapBuilder;
    private string nodeId = string.Empty;
    private bool isUnlocked;
    private bool isCleared;
    private bool isClickProcessing;
    private Vector3 originalScale = Vector3.one;
    private Coroutine pulseCoroutine;

    public Vector2 RandomPositionRange => new Vector2(randomPositionX, randomPositionY);

    private void Awake()
    {
        ResolveReferences();
        originalScale = transform.localScale;

        if (button != null)
        {
            button.onClick.AddListener(OnClickNode);
        }
    }

    private void OnDestroy()
    {
        StopPulse();

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
        this.mapBuilder = mapBuilder;

        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        if (nameText != null)
        {
            nameText.text = nodeEntry != null ? GetNodeTypeName(nodeEntry.NodeType) : string.Empty;
        }

        if (iconImage != null)
        {
            Sprite icon = nodeEntry != null && typeIcons != null ? typeIcons.GetIcon(nodeEntry.NodeType) : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (button != null)
        {
            button.interactable = true;
        }

        if (clearedObject != null)
        {
            clearedObject.SetActive(isCleared);
        }

        if (currentObject != null)
        {
            currentObject.SetActive(isUnlocked && !isCleared);
        }

        RefreshPulseState();
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

        if (!isUnlocked)
        {
            isClickProcessing = false;
            return;
        }

        if (TryCheatClearNode())
        {
            return;
        }

        switch (nodeEntry.NodeType)
        {
            case NodeDungeonType.Normal:
            case NodeDungeonType.Hard:
            case NodeDungeonType.EventA:
            case NodeDungeonType.EventB:
            case NodeDungeonType.EventC:
            case NodeDungeonType.Boss:
                LoadBattleScene();
                return;
            case NodeDungeonType.CardShop:
                OpenPopup(cardShopPopup);
                return;
            case NodeDungeonType.Merchant:
                OpenPopup(merchantPopup);
                return;
            default:
                isClickProcessing = false;
                return;
        }
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
    }

    private void RefreshPulseState()
    {
        if (isUnlocked && !isCleared)
        {
            StartPulse();
            return;
        }

        StopPulse();
    }

    private void StartPulse()
    {
        if (pulseCoroutine != null)
        {
            return;
        }

        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        transform.localScale = originalScale;
    }

    private IEnumerator PulseRoutine()
    {
        float safePulseSeconds = Mathf.Max(0.01f, currentPulseSeconds);
        float safePulseScale = Mathf.Max(0f, currentPulseScale);

        while (true)
        {
            float normalizedTime = Mathf.PingPong(Time.unscaledTime / safePulseSeconds * 2f, 1f);
            float scale = Mathf.Lerp(1f, safePulseScale, Mathf.SmoothStep(0f, 1f, normalizedTime));
            transform.localScale = originalScale * scale;
            yield return null;
        }
    }

    private void LoadBattleScene()
    {
        SC_NodeRunContext.SelectNode(stageData, nodeEntry, nodeId);
        StageBattleDirection battleDirection = SC_NodeRunContext.CurrentBattleDirection;
        bool shouldUseDropScene = battleDirection == StageBattleDirection.DOWN;
        string targetSceneName = shouldUseDropScene
            ? downBattleSceneName
            : upBattleSceneName;

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogWarning($"노드 이동 씬 '{targetSceneName}'이(가) Build Profiles에 없어 로드할 수 없습니다.", this);
            isClickProcessing = false;
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private void OpenPopup(GameObject popupObject)
    {
        if (popupObject != null)
        {
            popupObject.SetActive(true);
        }

        isClickProcessing = false;
    }

    private bool TryCheatClearNode()
    {
        if (mapBuilder == null || !IsCheatClearEnabled(nodeEntry.NodeType))
        {
            return false;
        }

        SC_NodeRunContext.Clear();
        mapBuilder.CompleteNode(nodeId, nodeEntry);
        return true;
    }

    private bool IsCheatClearEnabled(NodeDungeonType nodeType)
    {
        switch (nodeType)
        {
            case NodeDungeonType.Normal:
                return cheatClearNormalNode;
            case NodeDungeonType.Hard:
                return cheatClearHardNode;
            case NodeDungeonType.CardShop:
                return cheatClearCardShopNode;
            case NodeDungeonType.Merchant:
                return cheatClearMerchantNode;
            case NodeDungeonType.EventA:
                return cheatClearEventANode;
            case NodeDungeonType.EventB:
                return cheatClearEventBNode;
            case NodeDungeonType.EventC:
                return cheatClearEventCNode;
            case NodeDungeonType.Boss:
                return cheatClearBossNode;
            default:
                return false;
        }
    }

    private string GetNodeTypeName(NodeDungeonType nodeType)
    {
        switch (nodeType)
        {
            case NodeDungeonType.Normal:
                return "보통";
            case NodeDungeonType.Hard:
                return "어려움";
            case NodeDungeonType.CardShop:
                return "카드점";
            case NodeDungeonType.Merchant:
                return "상인";
            case NodeDungeonType.EventA:
            case NodeDungeonType.EventB:
            case NodeDungeonType.EventC:
                return "이벤트";
            case NodeDungeonType.Boss:
                return "보스";
            default:
                return string.Empty;
        }
    }
}
