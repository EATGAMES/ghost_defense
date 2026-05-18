using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class SC_StagePopup : MonoBehaviour
{
    [Tooltip("팝업 배경을 어둡게 처리하는 Dim 오브젝트입니다.")]
    [SerializeField] private GameObject dimObject;

    [Tooltip("실제로 표시할 팝업 루트 오브젝트입니다.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("클릭 시 팝업을 닫는 버튼입니다.")]
    [SerializeField] private Button closeButton;

    [Tooltip("팝업 안 편성 슬롯 UI를 새로고침할 편성 관리자입니다.")]
    [SerializeField] private SC_StageRosterEditor stageRosterEditor;

    [Tooltip("스테이지 좌우 버튼 선택 변경을 알려줄 로비 스테이지 선택자입니다.")]
    [SerializeField] private SC_LobbyStageSelector lobbyStageSelector;

    [Tooltip("선택 스테이지의 몬스터 데이터를 가져올 씬 로드 버튼입니다.")]
    [SerializeField] private SC_LoadSceneButton loadSceneButton;

    [Tooltip("선택한 스테이지 몬스터 이름을 표시할 TMP 텍스트입니다.")]
    [SerializeField] private TMP_Text monsterNameText;

    [Tooltip("선택한 스테이지 몬스터 체력을 표시할 TMP 텍스트입니다.")]
    [SerializeField] private TMP_Text monsterHpText;

    [Tooltip("선택한 스테이지 몬스터 약점을 표시할 TMP 텍스트입니다.")]
    [SerializeField] private TMP_Text monsterWeaknessText;

    [Tooltip("선택한 스테이지 전투 방향 이미지를 표시할 Image입니다.")]
    [SerializeField] private Image battleDirectionImage;

    [Tooltip("전투 방향이 UP일 때 표시할 스프라이트입니다.")]
    [SerializeField] private Sprite upBattleDirectionSprite;

    [Tooltip("전투 방향이 DOWN일 때 표시할 스프라이트입니다.")]
    [SerializeField] private Sprite downBattleDirectionSprite;

    [Tooltip("Dim Image의 Raycast Target을 강제로 켤지 여부입니다.")]
    [SerializeField] private bool blockBackgroundInput = true;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
        }

        if (stageRosterEditor != null)
        {
            stageRosterEditor.InitializeIfNeeded();
            stageRosterEditor.RefreshRosterUI();
        }

        ApplyDimRaycastSetting();
        RefreshMonsterInfo();
        SetPopupVisible(false);
    }

    private void OnEnable()
    {
        SubscribeStageSelector();
    }

    private void OnDisable()
    {
        UnsubscribeStageSelector();
    }

    private void OnDestroy()
    {
        UnsubscribeStageSelector();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePopup);
        }
    }

    public void OpenPopup()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (stageRosterEditor != null)
        {
            stageRosterEditor.RefreshRosterUI();
        }

        RefreshMonsterInfo();
        SetPopupVisible(true);
    }

    public void ClosePopup()
    {
        SetPopupVisible(false);
    }

    private void SetPopupVisible(bool isVisible)
    {
        SetDirectChildObjectsVisible(isVisible);
    }

    private void ApplyDimRaycastSetting()
    {
        if (!blockBackgroundInput || dimObject == null)
        {
            return;
        }

        Image dimImage = dimObject.GetComponent<Image>();
        if (dimImage == null)
        {
            Debug.LogWarning("Dim Object에 Image 컴포넌트가 없어 뒤 UI 입력 차단을 적용할 수 없습니다.");
            return;
        }

        dimImage.raycastTarget = true;
    }

    private void RefreshMonsterInfo()
    {
        SO_MonsterData monsterData = GetSelectedStageMonsterData();

        if (monsterNameText != null)
        {
            monsterNameText.text = monsterData != null ? monsterData.MonsterName : string.Empty;
        }

        if (monsterHpText != null)
        {
            monsterHpText.text = monsterData != null ? FormatHp(monsterData.MaxHp) : string.Empty;
        }

        if (monsterWeaknessText != null)
        {
            monsterWeaknessText.text = monsterData != null ? FormatWeakness(monsterData) : string.Empty;
        }

        RefreshBattleDirectionImage(monsterData);
    }

    private void RefreshBattleDirectionImage(SO_MonsterData monsterData)
    {
        if (battleDirectionImage == null)
        {
            return;
        }

        Sprite directionSprite = ResolveBattleDirectionSprite(monsterData);
        battleDirectionImage.sprite = directionSprite;
        battleDirectionImage.enabled = directionSprite != null;
    }

    private Sprite ResolveBattleDirectionSprite(SO_MonsterData monsterData)
    {
        if (monsterData == null)
        {
            return null;
        }

        return monsterData.StageBattleDirection == StageBattleDirection.DOWN
            ? downBattleDirectionSprite
            : upBattleDirectionSprite;
    }

    private void SubscribeStageSelector()
    {
        if (lobbyStageSelector == null)
        {
            lobbyStageSelector = FindAnyObjectByType<SC_LobbyStageSelector>();
        }

        if (lobbyStageSelector == null)
        {
            return;
        }

        lobbyStageSelector.SelectedStageChanged -= OnSelectedStageChanged;
        lobbyStageSelector.SelectedStageChanged += OnSelectedStageChanged;
    }

    private void UnsubscribeStageSelector()
    {
        if (lobbyStageSelector == null)
        {
            return;
        }

        lobbyStageSelector.SelectedStageChanged -= OnSelectedStageChanged;
    }

    private void OnSelectedStageChanged(int selectedStage)
    {
        RefreshMonsterInfo();
    }

    private SO_MonsterData GetSelectedStageMonsterData()
    {
        if (loadSceneButton == null)
        {
            loadSceneButton = FindAnyObjectByType<SC_LoadSceneButton>();
        }

        return loadSceneButton != null ? loadSceneButton.GetSelectedStageMonsterData() : null;
    }

    private static string FormatHp(float hp)
    {
        float safeHp = Mathf.Max(0f, hp);
        return Mathf.Approximately(safeHp, Mathf.Round(safeHp)) ? $"{safeHp:0}" : $"{safeHp:0.#}";
    }

    private static string FormatWeakness(SO_MonsterData monsterData)
    {
        if (monsterData == null)
        {
            return string.Empty;
        }

        string damageTypeText = GetDamageTypeText(monsterData.WeaknessDamageType);
        string attackStyleText = GetAttackStyleText(monsterData.WeaknessAttackStyle);
        if (string.IsNullOrWhiteSpace(damageTypeText) && string.IsNullOrWhiteSpace(attackStyleText))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(damageTypeText))
        {
            return attackStyleText;
        }

        if (string.IsNullOrWhiteSpace(attackStyleText))
        {
            return damageTypeText;
        }

        return $"{damageTypeText}, {attackStyleText}";
    }

    private static string GetDamageTypeText(MonsterWeaknessDamageType weaknessDamageType)
    {
        switch (weaknessDamageType)
        {
            case MonsterWeaknessDamageType.Physical:
                return "물리";
            case MonsterWeaknessDamageType.Magic:
                return "마법";
            case MonsterWeaknessDamageType.Explosion:
                return "폭발";
            default:
                return string.Empty;
        }
    }

    private static string GetAttackStyleText(MonsterWeaknessAttackStyle weaknessAttackStyle)
    {
        switch (weaknessAttackStyle)
        {
            case MonsterWeaknessAttackStyle.Ranged:
                return "원거리";
            case MonsterWeaknessAttackStyle.Melee:
                return "근거리";
            case MonsterWeaknessAttackStyle.Summon:
                return "소환";
            default:
                return string.Empty;
        }
    }

    private void SetDirectChildObjectsVisible(bool isVisible)
    {
        RectTransform rootTransform = transform as RectTransform;
        if (rootTransform == null)
        {
            if (dimObject != null)
            {
                dimObject.SetActive(isVisible);
            }

            if (popupRoot != null)
            {
                popupRoot.SetActive(isVisible);
            }

            return;
        }

        // 팝업 루트 바로 아래의 자식들을 함께 켜고 꺼서 형제 오브젝트로 배치된 UI도 같이 숨긴다.
        for (int i = 0; i < rootTransform.childCount; i++)
        {
            Transform childTransform = rootTransform.GetChild(i);
            if (childTransform == null)
            {
                continue;
            }

            childTransform.gameObject.SetActive(isVisible);
        }
    }
}
