using UnityEngine;
using UnityEngine.UI;

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
        SetPopupVisible(false);
    }

    private void OnDestroy()
    {
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
