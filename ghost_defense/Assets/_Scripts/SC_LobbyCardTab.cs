using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_LobbyCardTab : MonoBehaviour
{
    [Tooltip("카드 목록이 생성될 Content 루트입니다.")]
    [SerializeField] private Transform cardContentRoot;

    [Tooltip("목록에 복제해서 사용할 카드 프리팹입니다.")]
    [SerializeField] private SC_BattleCardItem cardItemPrefab;

    [Tooltip("로비 카드 탭에 표시할 카드 데이터 목록입니다.")]
    [SerializeField] private SO_CardData[] cardDataList;

    [Tooltip("카드 클릭 시 열 카드 업그레이드 팝업입니다.")]
    [SerializeField] private SC_CardUpgradePopup cardUpgradePopup;

    private readonly List<SC_BattleCardItem> spawnedCardItems = new List<SC_BattleCardItem>();

    private void Start()
    {
        RebuildCardList();
    }

    public void RebuildCardList()
    {
        ClearSpawnedCardItems();

        if (cardContentRoot == null || cardItemPrefab == null || cardDataList == null)
        {
            return;
        }

        for (int i = 0; i < cardDataList.Length; i++)
        {
            SO_CardData cardData = cardDataList[i];
            if (cardData == null)
            {
                continue;
            }

            SC_BattleCardItem spawnedItem = Instantiate(cardItemPrefab, cardContentRoot);
            spawnedItem.Initialize(OpenPopup);
            spawnedItem.BindCard(cardData, GetDisplayLevel(cardData));
            spawnedCardItems.Add(spawnedItem);
        }
    }

    public void RefreshCardList()
    {
        if (spawnedCardItems.Count <= 0)
        {
            RebuildCardList();
            return;
        }

        int bindIndex = 0;
        for (int i = 0; i < cardDataList.Length && bindIndex < spawnedCardItems.Count; i++)
        {
            SO_CardData cardData = cardDataList[i];
            if (cardData == null)
            {
                continue;
            }

            SC_BattleCardItem cardItem = spawnedCardItems[bindIndex];
            if (cardItem != null)
            {
                cardItem.BindCard(cardData, GetDisplayLevel(cardData));
            }

            bindIndex++;
        }
    }

    public void OpenPopup(SO_CardData cardData)
    {
        if (cardData == null || cardUpgradePopup == null)
        {
            return;
        }

        cardUpgradePopup.OpenPopup(cardData);
    }

    private void ClearSpawnedCardItems()
    {
        for (int i = 0; i < spawnedCardItems.Count; i++)
        {
            SC_BattleCardItem cardItem = spawnedCardItems[i];
            if (cardItem == null)
            {
                continue;
            }

            Destroy(cardItem.gameObject);
        }

        spawnedCardItems.Clear();
    }

    private int GetDisplayLevel(SO_CardData cardData)
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
