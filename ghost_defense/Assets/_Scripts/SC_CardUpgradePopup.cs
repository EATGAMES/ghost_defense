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

    [Tooltip("카드 업그레이드를 실행하는 버튼입니다. 비어 있으면 이름이 BTN_Upgrade인 오브젝트를 찾습니다.")]
    [SerializeField] private Button upgradeButton;

    [Tooltip("카드 이름을 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("카드 레벨을 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text levelText;

    [Tooltip("카드 설명을 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text descriptionText;

    [Tooltip("카드 이미지를 표시하는 Image입니다.")]
    [SerializeField] private Image cardImage;

    [Tooltip("현재 카드 효과 값을 표시할 TMP_Text입니다. 비어 있으면 이름이 TXT_Before인 오브젝트를 찾습니다.")]
    [SerializeField] private TMP_Text beforeText;

    [Tooltip("업그레이드 후 카드 효과 값을 표시할 TMP_Text입니다. 비어 있으면 이름이 TXT_After인 오브젝트를 찾습니다.")]
    [SerializeField] private TMP_Text afterText;

    [Tooltip("업그레이드 비용을 표시할 TMP_Text입니다. 비어 있으면 이름이 TXT_Cost인 오브젝트를 찾습니다.")]
    [SerializeField] private TMP_Text costText;

    [Tooltip("업그레이드 재화 이미지를 표시할 Image입니다. 비어 있으면 이름이 IMG_Cost인 오브젝트를 찾습니다.")]
    [SerializeField] private Image costImage;

    [Tooltip("골드 업그레이드 비용에 표시할 스프라이트입니다.")]
    [SerializeField] private Sprite goldCostSprite;

    [Tooltip("다이아 업그레이드 비용에 표시할 스프라이트입니다.")]
    [SerializeField] private Sprite diamondCostSprite;

    [Tooltip("딤 이미지의 Raycast Target을 켤지 여부입니다.")]
    [SerializeField] private bool blockBackgroundInput = true;

    private SO_CardData currentCardData;

    private void Awake()
    {
        CacheNamedReferences();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
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

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(OnUpgradeButtonClicked);
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

        currentCardData = cardData;
        RefreshPopup(cardData);
        SetPopupVisible(true);
    }

    public void ClosePopup()
    {
        SetPopupVisible(false);
    }

    private void OnUpgradeButtonClicked()
    {
        if (currentCardData == null || SC_SaveDataManager.Instance == null)
        {
            return;
        }

        int currentLevel = GetDisplayLevel(currentCardData);
        int upgradeCost = GetUpgradeCost(currentCardData, currentLevel);
        if (!SpendUpgradeCost(currentCardData.UpgradeCurrency, upgradeCost))
        {
            Debug.Log("SC_CardUpgradePopup: 카드 업그레이드 재화가 부족합니다.", this);
            return;
        }

        SC_SaveDataManager.Instance.SetCardLevel(currentCardData.CardId, currentLevel + 1);
        RefreshPopup(currentCardData);
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

        RefreshUpgradePreview(cardData, displayLevel);
    }

    private void RefreshUpgradePreview(SO_CardData cardData, int currentLevel)
    {
        int safeCurrentLevel = Mathf.Max(1, currentLevel);
        int nextLevel = safeCurrentLevel + 1;

        if (beforeText != null)
        {
            beforeText.text = FormatEffectValue(cardData, safeCurrentLevel);
        }

        if (afterText != null)
        {
            afterText.text = FormatEffectValue(cardData, nextLevel);
        }

        if (costText != null)
        {
            costText.text = GetUpgradeCost(cardData, safeCurrentLevel).ToString("N0");
        }

        if (costImage != null)
        {
            Sprite costSprite = GetCostSprite(cardData.UpgradeCurrency);
            costImage.sprite = costSprite;
            costImage.enabled = costSprite != null;
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

    private void CacheNamedReferences()
    {
        if (beforeText == null)
        {
            beforeText = FindChildComponentByName<TMP_Text>("TXT_Before");
        }

        if (afterText == null)
        {
            afterText = FindChildComponentByName<TMP_Text>("TXT_After");
        }

        if (costText == null)
        {
            costText = FindChildComponentByName<TMP_Text>("TXT_Cost");
        }

        if (costImage == null)
        {
            costImage = FindChildComponentByName<Image>("IMG_Cost");
        }

        if (upgradeButton == null)
        {
            upgradeButton = FindChildComponentByName<Button>("BTN_Upgrade");
        }
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

    private static int GetUpgradeCost(SO_CardData cardData, int currentLevel)
    {
        if (cardData == null)
        {
            return 0;
        }

        int safeCurrentLevel = Mathf.Max(1, currentLevel);
        return Mathf.Max(0, cardData.BaseUpgradeCost + cardData.UpgradeCostPerLevel * (safeCurrentLevel - 1));
    }

    private static string FormatEffectValue(SO_CardData cardData, int level)
    {
        if (cardData == null)
        {
            return "-";
        }

        float effectValue = cardData.GetEffectValueForLevel(level);
        if (IsPercentDisplayEffect(cardData.EffectType))
        {
            float percentValue = cardData.EffectType == CardEffectType.NextAttackDamageMultiplier
                ? Mathf.Max(0f, effectValue - 1f) * 100f
                : effectValue * 100f;

            return $"{percentValue:0.##}%";
        }

        if (Mathf.Approximately(effectValue, Mathf.Round(effectValue)))
        {
            return Mathf.RoundToInt(effectValue).ToString("N0");
        }

        return effectValue.ToString("0.##");
    }

    private Sprite GetCostSprite(CardUpgradeCurrency upgradeCurrency)
    {
        switch (upgradeCurrency)
        {
            case CardUpgradeCurrency.Diamond:
                return diamondCostSprite;
            default:
                return goldCostSprite;
        }
    }

    private static bool SpendUpgradeCost(CardUpgradeCurrency upgradeCurrency, int upgradeCost)
    {
        int safeUpgradeCost = Mathf.Max(0, upgradeCost);
        if (SC_CurrencyManager.Instance != null)
        {
            return upgradeCurrency == CardUpgradeCurrency.Diamond
                ? SC_CurrencyManager.Instance.SpendDiamond(safeUpgradeCost)
                : SC_CurrencyManager.Instance.SpendGold(safeUpgradeCost);
        }

        if (SC_SaveDataManager.Instance == null)
        {
            return false;
        }

        return upgradeCurrency == CardUpgradeCurrency.Diamond
            ? SC_SaveDataManager.Instance.SpendDiamond(safeUpgradeCost)
            : SC_SaveDataManager.Instance.SpendGold(safeUpgradeCost);
    }

    private static bool IsPercentDisplayEffect(CardEffectType effectType)
    {
        switch (effectType)
        {
            case CardEffectType.PhysicalDamageBonus:
            case CardEffectType.MagicDamageBonus:
            case CardEffectType.ExplosionDamageBonus:
            case CardEffectType.MeleeDamageBonus:
            case CardEffectType.RangedDamageBonus:
            case CardEffectType.SummonDamageBonus:
            case CardEffectType.GlobalDamageBonus:
            case CardEffectType.CriticalChanceBonus:
            case CardEffectType.Grade10DamageBonus:
            case CardEffectType.NextAttackDamageMultiplier:
            case CardEffectType.BonusGoldReward:
                return true;
            default:
                return false;
        }
    }

    private T FindChildComponentByName<T>(string targetName) where T : Component
    {
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        T[] childComponents = GetComponentsInChildren<T>(true);
        for (int i = 0; i < childComponents.Length; i++)
        {
            T childComponent = childComponents[i];
            if (childComponent != null && childComponent.name == targetName)
            {
                return childComponent;
            }
        }

        return null;
    }
}
