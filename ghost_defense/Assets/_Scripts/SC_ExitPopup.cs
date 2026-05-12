using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_ExitPopup : MonoBehaviour
{
    [Tooltip("팝업 뒤 배경 입력을 막는 DIM 오브젝트입니다.")]
    [SerializeField] private GameObject dimObject;

    [Tooltip("실제로 표시할 종료 팝업 루트 오브젝트입니다.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("팝업 루트에서 사용하는 CanvasGroup입니다. 비워두면 popupRoot에서 자동으로 찾습니다.")]
    [SerializeField] private CanvasGroup popupCanvasGroup;

    [Tooltip("클리어 후 상태를 확인할 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("클리어 후 다시 보여줄 클리어 팝업입니다.")]
    [SerializeField] private SC_ClearPopup clearPopup;

    [Tooltip("로비로 이동하는 확인 버튼입니다.")]
    [SerializeField] private Button yesButton;

    [Tooltip("팝업을 닫고 게임으로 돌아가는 취소 버튼입니다.")]
    [SerializeField] private Button noButton;

    [Tooltip("확인 시 이동할 로비 씬 이름입니다.")]
    [SerializeField] private string lobbySceneName = "SCN_Lobby";

    public bool IsPopupOpen => popupRoot != null && popupRoot.activeInHierarchy;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (clearPopup == null)
        {
            clearPopup = FindClearPopupIncludingInactive();
        }

        if (popupCanvasGroup == null && popupRoot != null)
        {
            popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();
        }

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnClickYes);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(ClosePopup);
        }

        SetPopupVisible(false);
        RestoreGameState();
    }

    private void OnDestroy()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(OnClickYes);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(ClosePopup);
        }

        RestoreGameState();
    }

    public void OpenPopup()
    {
        if (ShouldRedirectToClearPopup())
        {
            clearPopup.OpenPopup();
            return;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        PauseGameState();
        SetPopupVisible(true);
    }

    public void ClosePopup()
    {
        RestoreGameState();
        SetPopupVisible(false);
    }

    private void OnClickYes()
    {
        RestoreGameState();

        if (!string.IsNullOrWhiteSpace(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    private bool ShouldRedirectToClearPopup()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (clearPopup == null)
        {
            clearPopup = FindClearPopupIncludingInactive();
        }

        return battleManager != null && battleManager.IsBattleClearedThisSession && clearPopup != null;
    }

    private static SC_ClearPopup FindClearPopupIncludingInactive()
    {
        SC_ClearPopup activePopup = FindAnyObjectByType<SC_ClearPopup>();
        if (activePopup != null)
        {
            return activePopup;
        }

        SC_ClearPopup[] allPopups = Resources.FindObjectsOfTypeAll<SC_ClearPopup>();
        for (int i = 0; i < allPopups.Length; i++)
        {
            SC_ClearPopup popup = allPopups[i];
            if (popup == null)
            {
                continue;
            }

            if (!popup.gameObject.scene.IsValid())
            {
                continue;
            }

            return popup;
        }

        return null;
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

    private static void PauseGameState()
    {
        Time.timeScale = 0f;
    }

    private static void RestoreGameState()
    {
        Time.timeScale = 1f;
    }
}
