using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_LobbyStageSelector : MonoBehaviour
{
    [Tooltip("스테이지를 낮추는 왼쪽 버튼입니다.")]
    [SerializeField] private Button leftButton;

    [Tooltip("스테이지를 올리는 오른쪽 버튼입니다.")]
    [SerializeField] private Button rightButton;

    [Tooltip("현재 선택한 스테이지를 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text stageText;

    [Tooltip("선택 가능한 최대 스테이지입니다.")]
    [SerializeField] private int maxStage = 10;

    [Tooltip("클리어하지 않았지만 현재 입장 가능한 스테이지 색상입니다.")]
    [SerializeField] private Color playableStageColor = Color.white;

    [Tooltip("이미 클리어한 스테이지 색상입니다.")]
    [SerializeField] private Color clearedStageColor = Color.gray;

    [Tooltip("아직 입장 불가능한 스테이지 색상입니다.")]
    [SerializeField] private Color lockedStageColor = Color.red;

    private int selectedStage = 1;

    private void Awake()
    {
        if (leftButton != null)
        {
            leftButton.onClick.AddListener(OnClickLeft);
        }

        if (rightButton != null)
        {
            rightButton.onClick.AddListener(OnClickRight);
        }
    }

    private void Start()
    {
        selectedStage = Mathf.Clamp(GetHighestUnlockedStage(), 1, Mathf.Max(1, maxStage));
        SaveSelectedStage();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(OnClickLeft);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(OnClickRight);
        }
    }

    private void OnClickLeft()
    {
        SetSelectedStage(selectedStage - 1);
    }

    private void OnClickRight()
    {
        SetSelectedStage(selectedStage + 1);
    }

    private void SetSelectedStage(int stage)
    {
        selectedStage = Mathf.Clamp(stage, 1, Mathf.Max(1, maxStage));
        SaveSelectedStage();
        RefreshUI();
    }

    private void SaveSelectedStage()
    {
        if (SC_SaveDataManager.Instance == null)
        {
            return;
        }

        SC_SaveDataManager.Instance.SetSelectedStage(selectedStage);
    }

    private void RefreshUI()
    {
        if (stageText != null)
        {
            stageText.text = $"STAGE {selectedStage}";
            stageText.color = ResolveStageColor(selectedStage);
        }
    }

    private Color ResolveStageColor(int stage)
    {
        if (SC_SaveDataManager.Instance != null && SC_SaveDataManager.Instance.IsStageCleared(stage))
        {
            return clearedStageColor;
        }

        int highestUnlockedStage = GetHighestUnlockedStage();
        return stage <= highestUnlockedStage ? playableStageColor : lockedStageColor;
    }

    private int GetHighestUnlockedStage()
    {
        int safeMaxStage = Mathf.Max(1, maxStage);
        int highestUnlockedStage = 1;

        if (SC_SaveDataManager.Instance == null)
        {
            return highestUnlockedStage;
        }

        for (int stage = 1; stage <= safeMaxStage; stage++)
        {
            if (!SC_SaveDataManager.Instance.IsStageCleared(stage))
            {
                return Mathf.Clamp(stage, 1, safeMaxStage);
            }

            highestUnlockedStage = Mathf.Clamp(stage + 1, 1, safeMaxStage);
        }

        return highestUnlockedStage;
    }
}
