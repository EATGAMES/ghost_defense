using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

    [Tooltip("카드 레벨을 표시할 TMP_Text입니다.")]
    [FormerlySerializedAs("cardRarityText")]
    [SerializeField] private TMP_Text cardLevelText;

    [Tooltip("카드 이미지를 표시할 Image입니다.")]
    [SerializeField] private Image cardImage;

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

    public void BindCard(SO_CardData cardData, int currentLevel)
    {
        currentCardData = cardData;
        int nextLevel = Mathf.Max(1, currentLevel + 1);

        if (cardTitleText != null)
        {
            cardTitleText.text = cardData != null ? cardData.CardName : "CARD";
        }

        if (cardDescriptionText != null)
        {
            cardDescriptionText.text = cardData != null ? BuildDescription(cardData, nextLevel) : string.Empty;
        }

        if (cardLevelText != null)
        {
            cardLevelText.text = cardData != null ? BuildLevelText(currentLevel, nextLevel) : string.Empty;
        }

        if (cardImage != null)
        {
            cardImage.sprite = cardData != null ? cardData.CardImage : null;
            cardImage.enabled = cardData != null && cardData.CardImage != null;
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

    private static string BuildLevelText(int currentLevel, int nextLevel)
    {
        if (currentLevel <= 0)
        {
            return $"Lv.{nextLevel}";
        }

        return $"Lv.{currentLevel} > {nextLevel}";
    }

    private static string BuildDescription(SO_CardData cardData, int nextLevel)
    {
        return cardData.GetResolvedDescriptionForLevel(nextLevel);
    }
}
