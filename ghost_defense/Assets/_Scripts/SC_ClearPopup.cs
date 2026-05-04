using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_ClearPopup : MonoBehaviour
{
    [Tooltip("팝업 뒤 배경을 어둡게 처리할 DIM 오브젝트입니다.")]
    [SerializeField] private GameObject dimObject;

    [Tooltip("실제로 켜고 끌 클리어 팝업 루트 오브젝트입니다.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("팝업 루트에 사용할 CanvasGroup입니다. 비워두면 popupRoot에서 자동으로 찾습니다.")]
    [SerializeField] private CanvasGroup popupCanvasGroup;

    [Tooltip("클리어 보상 정보를 가져올 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("기본 골드 획득량을 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text goldText;

    [Tooltip("추가 골드 획득량을 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text goldBonusText;

    [Tooltip("기본 다이아 획득량을 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text diamondText;

    [Tooltip("추가 다이아 획득량을 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text diamondBonusText;

    [Tooltip("10단계 보너스 안내 문구를 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text bonusText;

    [Tooltip("계속 진행 버튼입니다.")]
    [SerializeField] private Button againButton;

    [Tooltip("로비로 이동하는 하단 닫기 버튼입니다.")]
    [SerializeField] private Button closeButton;

    [Tooltip("10단계 달성 시 표시할 중앙 닫기 버튼입니다.")]
    [SerializeField] private Button closeCenterButton;

    [Tooltip("닫기 시 이동할 로비 씬 이름입니다.")]
    [SerializeField] private string lobbySceneName = "SCN_Lobby";

    public bool IsPopupOpen => popupRoot != null && popupRoot.activeInHierarchy;
    private bool isExitConfirmMode;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (popupCanvasGroup == null && popupRoot != null)
        {
            popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();
        }

        if (againButton != null)
        {
            againButton.onClick.AddListener(OnClickAgain);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnClickClose);
        }

        if (closeCenterButton != null)
        {
            closeCenterButton.onClick.AddListener(OnClickClose);
        }

    }

    private void OnDestroy()
    {
        if (againButton != null)
        {
            againButton.onClick.RemoveListener(OnClickAgain);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnClickClose);
        }

        if (closeCenterButton != null)
        {
            closeCenterButton.onClick.RemoveListener(OnClickClose);
        }
    }

    public void OpenPopup()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (battleManager == null)
        {
            Debug.LogWarning("SC_ClearPopup: SC_BattleManager를 찾지 못해서 팝업을 열 수 없습니다.", this);
            return;
        }

        isExitConfirmMode = false;
        SC_BattleManager.ClearRewardResult rewardResult = battleManager.BuildAndGrantClearRewardResult();
        RefreshTexts(rewardResult);
        RefreshButtons(rewardResult.ShowCloseCenterOnly);
        CancelAllPendingCharacterDrags();
        Time.timeScale = 0f;
        SetPopupVisible(true);
        Debug.Log(
            $"SC_ClearPopup: 클리어 팝업을 열었습니다. rootActive={popupRoot != null && popupRoot.activeSelf}, rootHierarchy={popupRoot != null && popupRoot.activeInHierarchy}",
            this);
    }

    public void OpenExitConfirmPopup()
    {
        isExitConfirmMode = true;
        RefreshTexts(new SC_BattleManager.ClearRewardResult(0, 0, 0, 0, false));
        RefreshButtons(false);
        CancelAllPendingCharacterDrags();
        Time.timeScale = 0f;
        SetPopupVisible(true);
    }

    public void ClosePopup()
    {
        Time.timeScale = 1f;
        isExitConfirmMode = false;
        SetPopupVisible(false);
    }

    private void OnClickAgain()
    {
        ClosePopup();
    }

    private void OnClickClose()
    {
        Time.timeScale = 1f;
        isExitConfirmMode = false;

        if (!string.IsNullOrWhiteSpace(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    private void RefreshTexts(SC_BattleManager.ClearRewardResult rewardResult)
    {
        if (goldText != null)
        {
            goldText.text = rewardResult.BaseGold.ToString("N0");
        }

        if (goldBonusText != null)
        {
            goldBonusText.text = FormatBonusText(rewardResult.BonusGold);
            goldBonusText.gameObject.SetActive(rewardResult.BonusGold > 0);
        }

        if (diamondText != null)
        {
            diamondText.text = rewardResult.BaseDiamond.ToString("N0");
        }

        if (diamondBonusText != null)
        {
            diamondBonusText.text = FormatBonusText(rewardResult.BonusDiamond);
            diamondBonusText.gameObject.SetActive(rewardResult.BonusDiamond > 0);
        }
    }

    private void RefreshButtons(bool showCloseCenterOnly)
    {
        if (againButton != null)
        {
            againButton.gameObject.SetActive(!showCloseCenterOnly);
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(!showCloseCenterOnly);
        }

        if (bonusText != null)
        {
            bonusText.gameObject.SetActive(!showCloseCenterOnly);
        }

        if (closeCenterButton != null)
        {
            closeCenterButton.gameObject.SetActive(showCloseCenterOnly);
        }
    }

    private void SetPopupVisible(bool isVisible)
    {
        if (dimObject != null)
        {
            dimObject.SetActive(isVisible);
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(isVisible);

            RectTransform popupRectTransform = popupRoot.GetComponent<RectTransform>();
            if (popupRectTransform != null)
            {
                popupRectTransform.localScale = Vector3.one;
            }
        }

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = isVisible ? 1f : 0f;
            popupCanvasGroup.interactable = isVisible;
            popupCanvasGroup.blocksRaycasts = isVisible;
        }
    }

    private static string FormatBonusText(int amount)
    {
        return amount > 0 ? $"+{amount:N0}" : string.Empty;
    }

    private static void CancelAllPendingCharacterDrags()
    {
        SC_PlayerDragAndShoot[] allShooters = FindObjectsByType<SC_PlayerDragAndShoot>(FindObjectsSortMode.None);
        for (int i = 0; i < allShooters.Length; i++)
        {
            SC_PlayerDragAndShoot shooter = allShooters[i];
            if (shooter == null || shooter.IsShot)
            {
                continue;
            }

            shooter.CancelDragAndResetToStartPosition();
        }
    }
}
