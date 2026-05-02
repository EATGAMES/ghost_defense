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

    [Tooltip("Dim Image의 Raycast Target을 강제로 켤지 여부입니다.")]
    [SerializeField] private bool blockBackgroundInput = true;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
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

        SetPopupVisible(true);
    }

    public void ClosePopup()
    {
        SetPopupVisible(false);
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
        }
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
}
