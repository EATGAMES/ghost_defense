using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_UpgradePopup : MonoBehaviour
{
    [Tooltip("팝업이 열릴 때 함께 표시할 딤 오브젝트입니다.")]
    [SerializeField] private GameObject dimObject;

    [Tooltip("실제로 표시할 팝업 루트 오브젝트입니다.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("팝업을 닫는 버튼입니다.")]
    [SerializeField] private Button closeButton;

    [Tooltip("팝업 제목을 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("캐릭터 이미지를 표시하는 Image입니다.")]
    [SerializeField] private Image characterImage;

    [Tooltip("공격력 업그레이드를 실행하는 버튼입니다.")]
    [SerializeField] private Button upgrade1Button;

    [Tooltip("딤 이미지로 배경 입력을 차단할지 여부입니다.")]
    [SerializeField] private bool blockBackgroundInput = true;

    private SO_CharacterData currentCharacterData;
    private Action onDamageUpgradeCompleted;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
        }

        if (upgrade1Button != null)
        {
            upgrade1Button.onClick.AddListener(HandleClickUpgradeDamage);
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

        if (upgrade1Button != null)
        {
            upgrade1Button.onClick.RemoveListener(HandleClickUpgradeDamage);
        }
    }

    public void OpenPopup()
    {
        OpenPopup(null, null, null, null);
    }

    public void OpenPopup(string popupTitle, Sprite characterSprite)
    {
        OpenPopup(null, popupTitle, characterSprite, null);
    }

    public void OpenPopup(SO_CharacterData characterData, string popupTitle, Sprite characterSprite, Action damageUpgradeCompleted)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        currentCharacterData = characterData;
        onDamageUpgradeCompleted = damageUpgradeCompleted;
        SetTitle(popupTitle);
        SetCharacterImage(characterSprite);
        UpdateUpgradeButtonState();
        SetPopupVisible(true);
    }

    public void SetTitle(string popupTitle)
    {
        if (titleText == null)
        {
            return;
        }

        titleText.text = popupTitle ?? string.Empty;
    }

    public void SetCharacterImage(Sprite characterSprite)
    {
        if (characterImage == null)
        {
            return;
        }

        characterImage.sprite = characterSprite;
        characterImage.enabled = characterSprite != null;
    }

    public void ClosePopup()
    {
        SetPopupVisible(false);
    }

    private void HandleClickUpgradeDamage()
    {
        if (currentCharacterData == null)
        {
            return;
        }

        if (SC_SaveDataManager.Instance == null)
        {
            Debug.LogWarning("SC_UpgradePopup: SaveDataManager가 없어 공격력 업그레이드를 저장할 수 없습니다.", this);
            return;
        }

        string characterSaveKey = currentCharacterData.GetSaveKey();
        if (string.IsNullOrWhiteSpace(characterSaveKey))
        {
            Debug.LogWarning("SC_UpgradePopup: 캐릭터 저장 키가 없어 공격력 업그레이드를 저장할 수 없습니다.", this);
            return;
        }

        SaveCharacterUpgradeEntry upgradeEntry = SC_SaveDataManager.Instance.GetCharacterUpgradeEntry(characterSaveKey);
        SC_SaveDataManager.Instance.SetCharacterUpgradeLevels(
            characterSaveKey,
            upgradeEntry.DamageLevel + 1,
            upgradeEntry.CriticalChanceLevel,
            upgradeEntry.CriticalDamageLevel,
            upgradeEntry.UniqueSkillLevel);

        onDamageUpgradeCompleted?.Invoke();
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
            Debug.LogWarning("SC_UpgradePopup: 딤 오브젝트에 Image 컴포넌트가 없어 배경 입력 차단을 적용할 수 없습니다.", this);
            return;
        }

        dimImage.raycastTarget = true;
    }

    private void SetPopupVisible(bool isVisible)
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

        // 팝업 바로 아래 자식들을 함께 켜고 끈다.
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

    private void UpdateUpgradeButtonState()
    {
        if (upgrade1Button == null)
        {
            return;
        }

        bool canUpgrade = currentCharacterData != null && !string.IsNullOrWhiteSpace(currentCharacterData.GetSaveKey());
        upgrade1Button.interactable = canUpgrade;
    }
}
