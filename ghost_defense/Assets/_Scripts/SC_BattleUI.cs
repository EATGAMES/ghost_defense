using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SC_BattleUI : MonoBehaviour
{
    [Tooltip("상단 보스 스테이지를 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text stageText;

    [Tooltip("카드 선택까지 남은 공격 횟수를 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text attackGaugeText;

    [Tooltip("현재 상단 공격 캐릭터 이름을 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text currentAttackCharacterText;

    [Tooltip("UI에 전투 정보를 전달할 배틀 매니저입니다.")]
    [FormerlySerializedAs("waveManager")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("게임오버 시 표시할 패배 팝업입니다.")]
    [SerializeField] private SC_DefeatPopup defeatPopup;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }
    }

    private void OnEnable()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.StageChanged += OnStageChanged;
        battleManager.MergeAttackGaugeChanged += OnMergeAttackGaugeChanged;
        battleManager.CurrentAttackCharacterChanged += OnCurrentAttackCharacterChanged;
        battleManager.StageCleared += OnStageCleared;
        battleManager.StageFailed += OnStageFailed;

        OnStageChanged(SC_BattleManager.CurrentStage, battleManager.MaxStage);
        OnMergeAttackGaugeChanged(battleManager.CurrentMergeAttackCount, battleManager.MergeAttackCountPerCard);
        OnCurrentAttackCharacterChanged(battleManager.CurrentAttackCharacterData, false);
    }

    private void OnDisable()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.StageChanged -= OnStageChanged;
        battleManager.MergeAttackGaugeChanged -= OnMergeAttackGaugeChanged;
        battleManager.CurrentAttackCharacterChanged -= OnCurrentAttackCharacterChanged;
        battleManager.StageCleared -= OnStageCleared;
        battleManager.StageFailed -= OnStageFailed;
    }

    private void OnStageChanged(int currentStage, int maxStage)
    {
        if (stageText != null)
        {
            stageText.text = $"BOSS {currentStage}";
        }
    }

    private void OnMergeAttackGaugeChanged(int currentCount, int requiredCount)
    {
        if (attackGaugeText != null)
        {
            attackGaugeText.text = $"{currentCount}/{Mathf.Max(1, requiredCount)}";
        }
    }

    private void OnCurrentAttackCharacterChanged(SO_CharacterData characterData, bool playAttackAnimation)
    {
        if (currentAttackCharacterText == null)
        {
            return;
        }

        currentAttackCharacterText.text = characterData != null ? characterData.CharacterName : "-";
    }

    private void OnStageCleared(int clearedStage)
    {
        if (attackGaugeText != null)
        {
            attackGaugeText.text = "CLEAR";
        }
    }

    private void OnStageFailed(int failedStage)
    {
        if (attackGaugeText != null)
        {
            attackGaugeText.text = "FAIL";
        }

        if (defeatPopup == null)
        {
            Debug.LogWarning("SC_BattleUI: Defeat Popup 인스펙터 연결이 비어 있습니다.", this);
            return;
        }

        defeatPopup.OpenPopup();
    }
}
