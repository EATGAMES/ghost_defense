using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SC_BattleUI : MonoBehaviour
{
    [Tooltip("상단 보스 스테이지를 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text stageText;

    [Tooltip("카드 선택까지 남은 공격 횟수를 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text attackGaugeText;

    [Tooltip("상단 5인 공격 대기열 이름을 표시할 TMP_Text 목록입니다.")]
    [SerializeField] private TMP_Text[] rosterTexts;

    [Tooltip("UI에 전투 정보를 전달할 배틀 매니저입니다.")]
    [FormerlySerializedAs("waveManager")]
    [SerializeField] private SC_BattleManager battleManager;

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
        battleManager.StageCleared += OnStageCleared;
        battleManager.StageFailed += OnStageFailed;
        battleManager.StageRosterChanged += RefreshRosterTexts;

        OnStageChanged(SC_BattleManager.CurrentStage, battleManager.MaxStage);
        OnMergeAttackGaugeChanged(battleManager.CurrentMergeAttackCount, battleManager.MergeAttackCountPerCard);
        RefreshRosterTexts();
    }

    private void OnDisable()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.StageChanged -= OnStageChanged;
        battleManager.MergeAttackGaugeChanged -= OnMergeAttackGaugeChanged;
        battleManager.StageCleared -= OnStageCleared;
        battleManager.StageFailed -= OnStageFailed;
        battleManager.StageRosterChanged -= RefreshRosterTexts;
    }

    private void OnStageChanged(int currentStage, int maxStage)
    {
        if (stageText == null)
        {
            return;
        }

        stageText.text = $"BOSS {currentStage}";
    }

    private void OnMergeAttackGaugeChanged(int currentCount, int requiredCount)
    {
        if (attackGaugeText == null)
        {
            return;
        }

        attackGaugeText.text = $"{currentCount}/{Mathf.Max(1, requiredCount)}";
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
    }

    private void RefreshRosterTexts()
    {
        if (rosterTexts == null || rosterTexts.Length <= 0 || battleManager == null)
        {
            return;
        }

        SO_CharacterData[] rosterSnapshot = battleManager.GetRuntimeRosterSnapshot();
        for (int i = 0; i < rosterTexts.Length; i++)
        {
            TMP_Text targetText = rosterTexts[i];
            if (targetText == null)
            {
                continue;
            }

            if (rosterSnapshot == null || i >= rosterSnapshot.Length || rosterSnapshot[i] == null)
            {
                targetText.text = "-";
                continue;
            }

            string prefix = i == 0 ? "> " : string.Empty;
            targetText.text = $"{prefix}{rosterSnapshot[i].CharacterName}";
        }
    }
}
