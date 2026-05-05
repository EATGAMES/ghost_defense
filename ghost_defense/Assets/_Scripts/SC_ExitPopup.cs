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

    [Tooltip("팝업 루트에 사용하는 CanvasGroup입니다. 비워두면 popupRoot에서 자동으로 찾습니다.")]
    [SerializeField] private CanvasGroup popupCanvasGroup;

    [Tooltip("로비로 이동하는 확인 버튼입니다.")]
    [SerializeField] private Button yesButton;

    [Tooltip("팝업을 닫고 게임으로 돌아가는 취소 버튼입니다.")]
    [SerializeField] private Button noButton;

    [Tooltip("확인 시 이동할 로비 씬 이름입니다.")]
    [SerializeField] private string lobbySceneName = "SCN_Lobby";

    public bool IsPopupOpen => popupRoot != null && popupRoot.activeInHierarchy;

    private void Awake()
    {
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
