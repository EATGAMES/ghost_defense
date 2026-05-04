using UnityEngine;
using UnityEngine.Serialization;

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

    [Tooltip("카드 UI 프리팹입니다.")]
    [SerializeField] private SC_BattleCardItem cardItemPrefab;

    [Tooltip("왼쪽 카드 UI가 생성될 부모 오브젝트입니다.")]
    [SerializeField] private Transform leftCardParent;

    [Tooltip("오른쪽 카드 UI가 생성될 부모 오브젝트입니다.")]
    [SerializeField] private Transform rightCardParent;

    [Tooltip("전투 중 등장시킬 카드 데이터 목록입니다.")]
    [SerializeField] private SO_CardData[] cardDataPool;

    [Tooltip("전투 중 카드 상태를 참조할 카드 매니저입니다.")]
    [SerializeField] private SC_CardManager cardManager;

    private SC_BattleCardItem leftCardItem;
    private SC_BattleCardItem rightCardItem;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (cardManager == null)
        {
            cardManager = FindAnyObjectByType<SC_CardManager>();
        }

        EnsureCardItems();
        SetPopupVisible(false);
    }

    public void OpenCardSelection(int selectionCount)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        EnsureCardItems();
        RefreshCardOptions();
        SetPopupVisible(true);
    }

    private void EnsureCardItems()
    {
        if (cardItemPrefab == null)
        {
            return;
        }

        if (leftCardItem == null && leftCardParent != null)
        {
            leftCardItem = Instantiate(cardItemPrefab, leftCardParent);
            leftCardItem.Initialize(OnCardSelected);
        }

        if (rightCardItem == null && rightCardParent != null)
        {
            rightCardItem = Instantiate(cardItemPrefab, rightCardParent);
            rightCardItem.Initialize(OnCardSelected);
        }
    }

    private void RefreshCardOptions()
    {
        SO_CardData[] selectableCards = GetSelectableCards();
        int count = selectableCards.Length;
        if (count <= 0)
        {
            if (leftCardItem != null)
            {
                leftCardItem.BindCard(null, 0);
            }

            if (rightCardItem != null)
            {
                rightCardItem.BindCard(null, 0);
            }

            return;
        }

        int leftCardIndex = Random.Range(0, count);
        int rightCardIndex = leftCardIndex;

        if (count > 1)
        {
            while (rightCardIndex == leftCardIndex)
            {
                rightCardIndex = Random.Range(0, count);
            }
        }

        if (leftCardItem != null)
        {
            SO_CardData leftCardData = selectableCards[leftCardIndex];
            leftCardItem.BindCard(leftCardData, GetCurrentCardLevel(leftCardData));
        }

        if (rightCardItem != null)
        {
            SO_CardData rightCardData = selectableCards[rightCardIndex];
            rightCardItem.BindCard(rightCardData, GetCurrentCardLevel(rightCardData));
        }
    }

    private SO_CardData[] GetSelectableCards()
    {
        if (cardDataPool == null || cardDataPool.Length <= 0)
        {
            return System.Array.Empty<SO_CardData>();
        }

        if (cardManager == null)
        {
            return cardDataPool;
        }

        System.Collections.Generic.List<SO_CardData> selectableCards = new System.Collections.Generic.List<SO_CardData>();
        for (int i = 0; i < cardDataPool.Length; i++)
        {
            SO_CardData cardData = cardDataPool[i];
            if (!cardManager.CanOfferCard(cardData))
            {
                continue;
            }

            selectableCards.Add(cardData);
        }

        return selectableCards.ToArray();
    }

    private int GetCurrentCardLevel(SO_CardData cardData)
    {
        if (cardManager == null)
        {
            return 0;
        }

        return cardManager.GetCardLevel(cardData);
    }

    private void OnCardSelected(SO_CardData selectedCardData)
    {
        SetPopupVisible(false);

        if (battleManager != null)
        {
            battleManager.NotifyCardSelected(selectedCardData);
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
