using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_BattleCardItem : MonoBehaviour
{
    [Tooltip("카드 선택 버튼입니다.")]
    [SerializeField] private Button selectButton;

    [Tooltip("카드 이름을 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text cardTitleText;

    [Tooltip("카드 설명을 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text cardDescriptionText;

    [Tooltip("카드 등급을 표시할 TMP_Text입니다.")]
    [SerializeField] private TMP_Text cardRarityText;

    private Action<SO_CardData> onCardSelected;
    private SO_CardData currentCardData;

    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(HandleClickCard);
        }
    }

    public void Initialize(Action<SO_CardData> onSelected)
    {
        onCardSelected = onSelected;
    }

    public void BindCard(SO_CardData cardData)
    {
        currentCardData = cardData;

        if (cardTitleText != null)
        {
            cardTitleText.text = cardData != null ? cardData.CardName : "CARD";
        }

        if (cardDescriptionText != null)
        {
            cardDescriptionText.text = cardData != null ? cardData.Description : string.Empty;
        }

        if (cardRarityText != null)
        {
            cardRarityText.text = cardData != null ? cardData.Rarity.ToString() : string.Empty;
        }

        if (selectButton != null)
        {
            selectButton.interactable = cardData != null;
        }
    }

    private void HandleClickCard()
    {
        if (currentCardData == null)
        {
            return;
        }

        onCardSelected?.Invoke(currentCardData);
    }
}
