using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_CardUpgradePopup : MonoBehaviour
{
    [Tooltip("팝업 배경을 어둡게 처리하는 딤 오브젝트입니다.")]
    [SerializeField] private GameObject dimObject;

    [Tooltip("실제로 표시할 카드 업그레이드 팝업 루트입니다.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("팝업을 닫는 버튼입니다.")]
    [SerializeField] private Button closeButton;

    [Tooltip("카드 이름을 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("카드 레벨을 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text levelText;

    [Tooltip("카드 설명을 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text descriptionText;

    [Tooltip("카드 이미지를 표시하는 Image입니다.")]
    [SerializeField] private Image cardImage;

    [Tooltip("딤 이미지의 Raycast Target을 켤지 여부입니다.")]
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

    public void OpenPopup(SO_CardData cardData)
    {
        if (cardData == null)
        {
            return;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        RefreshPopup(cardData);
        SetPopupVisible(true);
    }

    public void ClosePopup()
    {
        SetPopupVisible(false);
    }

    private void RefreshPopup(SO_CardData cardData)
    {
        int displayLevel = GetDisplayLevel(cardData);

        if (titleText != null)
        {
            titleText.text = cardData.CardName;
        }

        if (levelText != null)
        {
            levelText.text = $"Lv.{displayLevel}";
        }

        if (descriptionText != null)
        {
            descriptionText.text = cardData.GetResolvedDescriptionForLevel(displayLevel);
        }

        if (cardImage != null)
        {
            cardImage.sprite = cardData.CardImage;
            cardImage.enabled = cardData.CardImage != null;
        }
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
            Debug.LogWarning("SC_CardUpgradePopup: Dim 오브젝트에 Image 컴포넌트가 없어서 배경 입력 차단을 적용할 수 없습니다.", this);
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

        // 팝업 루트 바로 아래 자식들을 함께 켜고 끄도록 처리한다.
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

    private static int GetDisplayLevel(SO_CardData cardData)
    {
        if (cardData == null)
        {
            return 1;
        }

        if (SC_SaveDataManager.Instance == null)
        {
            return 1;
        }

        return Mathf.Max(1, SC_SaveDataManager.Instance.GetCardLevel(cardData.CardId));
    }
}
