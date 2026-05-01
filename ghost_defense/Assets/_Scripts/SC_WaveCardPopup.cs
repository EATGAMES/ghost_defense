using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_WaveCardPopup : MonoBehaviour
{
    [Tooltip("배경 어둡게 처리용 DIM 오브젝트")]
    [SerializeField] private GameObject dimObject;

    [Tooltip("카드 선택 팝업 루트 오브젝트")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("웨이브 데이터를 제공할 매니저")]
    [SerializeField] private SC_WaveManager waveManager;

    [Tooltip("왼쪽 카드 버튼")]
    [SerializeField] private Button leftCardButton;

    [Tooltip("오른쪽 카드 버튼")]
    [SerializeField] private Button rightCardButton;

    [Tooltip("왼쪽 카드 이름 텍스트")]
    [SerializeField] private TMP_Text leftCardTitleText;

    [Tooltip("오른쪽 카드 이름 텍스트")]
    [SerializeField] private TMP_Text rightCardTitleText;

    [Tooltip("카드 후보 이름 목록")]
    [SerializeField] private string[] cardNamePool =
    {
        "공격력 강화",
        "공격 속도 강화",
        "탄속 강화",
        "합체 보너스",
        "치명타 강화"
    };

    private int leftCardIndex = -1;
    private int rightCardIndex = -1;

    private void Awake()
    {
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<SC_WaveManager>();
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

    private void OnEnable()
    {
        if (waveManager == null)
        {
            return;
        }

        waveManager.CardSelectionRequested += OnCardSelectionRequested;
    }

    private void OnDisable()
    {
        if (waveManager == null)
        {
            return;
        }

        waveManager.CardSelectionRequested -= OnCardSelectionRequested;
    }

    private void OnCardSelectionRequested(int nextWave)
    {
        OpenCardSelection(nextWave);
    }

    public void OpenCardSelection(int nextWave)
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
            SetCardTexts("카드 A", "카드 B");
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

        string leftName = cardNamePool[leftCardIndex];
        string rightName = cardNamePool[rightCardIndex];
        SetCardTexts(leftName, rightName);
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
        SetPopupVisible(false);

        if (waveManager != null)
        {
            waveManager.NotifyCardSelected();
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
