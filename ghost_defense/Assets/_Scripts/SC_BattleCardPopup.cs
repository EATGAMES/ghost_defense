using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_BattleCardPopup : MonoBehaviour
{
    [Tooltip("팝업 뒤 배경을 어둡게 처리할 DIM 오브젝트입니다.")]
    [SerializeField] private GameObject dimObject;

    [Tooltip("카드 선택 팝업 루트 오브젝트입니다.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("카드 선택 결과를 전달할 배틀 매니저입니다.")]
    [FormerlySerializedAs("waveManager")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("왼쪽 카드 버튼입니다.")]
    [SerializeField] private Button leftCardButton;

    [Tooltip("오른쪽 카드 버튼입니다.")]
    [SerializeField] private Button rightCardButton;

    [Tooltip("왼쪽 카드 이름을 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text leftCardTitleText;

    [Tooltip("오른쪽 카드 이름을 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text rightCardTitleText;

    [Tooltip("임시로 사용할 카드 이름 목록입니다.")]
    [SerializeField] private string[] cardNamePool =
    {
        "ATTACK DAMAGE UP",
        "HIGH GRADE BONUS",
        "FIELD CLEAR",
        "NEXT OBJECT UP",
        "CLEAR REWARD UP"
    };

    private int leftCardIndex = -1;
    private int rightCardIndex = -1;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (leftCardButton != null)
        {
            leftCardButton.onClick.AddListener(OnClickLeftCard);
        }

        if (rightCardButton != null)
        {
            rightCardButton.onClick.AddListener(OnClickRightCard);
        }

        SetPopupVisible(false);
    }

    public void OpenCardSelection(int selectionCount)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        RefreshCardOptions();
        SetPopupVisible(true);
    }

    private void RefreshCardOptions()
    {
        int count = cardNamePool != null ? cardNamePool.Length : 0;
        if (count <= 0)
        {
            SetCardTexts("CARD A", "CARD B");
            leftCardIndex = 0;
            rightCardIndex = 1;
            return;
        }

        leftCardIndex = Random.Range(0, count);
        rightCardIndex = leftCardIndex;
        if (count > 1)
        {
            while (rightCardIndex == leftCardIndex)
            {
                rightCardIndex = Random.Range(0, count);
            }
        }

        SetCardTexts(cardNamePool[leftCardIndex], cardNamePool[rightCardIndex]);
    }

    private void SetCardTexts(string leftText, string rightText)
    {
        if (leftCardTitleText != null)
        {
            leftCardTitleText.text = leftText;
        }

        if (rightCardTitleText != null)
        {
            rightCardTitleText.text = rightText;
        }
    }

    private void OnClickLeftCard()
    {
        SelectCard(leftCardIndex);
    }

    private void OnClickRightCard()
    {
        SelectCard(rightCardIndex);
    }

    private void SelectCard(int selectedCardIndex)
    {
        if (cardNamePool != null && selectedCardIndex >= 0 && selectedCardIndex < cardNamePool.Length)
        {
            Debug.Log($"Selected Card: {cardNamePool[selectedCardIndex]}");
        }

        SetPopupVisible(false);

        if (battleManager != null)
        {
            battleManager.NotifyCardSelected();
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
        }
    }
}
