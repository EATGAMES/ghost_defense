using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_CharacterPanel : MonoBehaviour
{
    [Tooltip("패널에 표시할 캐릭터 데이터입니다.")]
    [SerializeField] private SO_CharacterData characterData;

    [Tooltip("캐릭터 프리뷰 이미지를 표시하는 Image입니다.")]
    [SerializeField] private Image previewCharacterImage;

    [Tooltip("캐릭터 이름을 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text characterNameText;

    [Tooltip("캐릭터의 기본 공격력을 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text baseAttackPowerText;

    [Tooltip("캐릭터의 크리티컬 확률을 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text criticalChanceText;

    [Tooltip("캐릭터의 크리티컬 데미지 퍼센트를 표시하는 TMP_Text입니다.")]
    [SerializeField] private TMP_Text criticalDamagePercentText;

    [Tooltip("캐릭터 업그레이드 팝업을 여는 버튼입니다.")]
    [SerializeField] private Button characterUpgradeButton;

    [Tooltip("캐릭터 업그레이드 버튼 클릭 시 열 팝업입니다.")]
    [SerializeField] private SC_UpgradePopup upgradePopup;

    [Tooltip("캐릭터 사용 여부를 바꾸는 체크 버튼입니다.")]
    [SerializeField] private Button checkButton;

    [Tooltip("캐릭터 사용 ON 상태일 때 표시할 체크 이미지입니다.")]
    [SerializeField] private Image checkImage;

    private void Awake()
    {
        ResolveCheckReferences();

        if (characterUpgradeButton != null)
        {
            characterUpgradeButton.onClick.AddListener(OpenUpgradePopup);
        }

        if (checkButton != null)
        {
            checkButton.onClick.AddListener(ToggleCharacterUseState);
        }

        Refresh();
    }

    public SO_CharacterData CharacterData => characterData;

    private void OnDestroy()
    {
        if (characterUpgradeButton != null)
        {
            characterUpgradeButton.onClick.RemoveListener(OpenUpgradePopup);
        }

        if (checkButton != null)
        {
            checkButton.onClick.RemoveListener(ToggleCharacterUseState);
        }
    }

    private void OnValidate()
    {
        ResolveCheckReferences();
        Refresh();
    }

    public void SetCharacterData(SO_CharacterData newCharacterData)
    {
        characterData = newCharacterData;
        Refresh();
    }

    public void Refresh()
    {
        ApplyPreviewImage();
        ApplyCharacterName();
        ApplyBaseAttackPower();
        ApplyCriticalChance();
        ApplyCriticalDamagePercent();
        ApplyCheckState();
    }

    private void ApplyPreviewImage()
    {
        if (previewCharacterImage == null)
        {
            return;
        }

        Sprite previewSprite = characterData != null ? characterData.PreviewCharacterSprite : null;
        previewCharacterImage.sprite = previewSprite;
        previewCharacterImage.enabled = previewSprite != null;
    }

    private void ApplyCharacterName()
    {
        if (characterNameText == null)
        {
            return;
        }

        characterNameText.text = characterData != null ? characterData.CharacterName : string.Empty;
    }

    private void ApplyBaseAttackPower()
    {
        if (baseAttackPowerText == null)
        {
            return;
        }

        baseAttackPowerText.text = characterData != null ? characterData.GetCurrentBaseAttackPower().ToString("0.##") : string.Empty;
    }

    private void ApplyCriticalChance()
    {
        if (criticalChanceText == null)
        {
            return;
        }

        float criticalChancePercent = characterData != null ? characterData.CriticalChance * 100f : 0f;
        criticalChanceText.text = characterData != null ? $"{criticalChancePercent:0.##}%" : string.Empty;
    }

    private void ApplyCriticalDamagePercent()
    {
        if (criticalDamagePercentText == null)
        {
            return;
        }

        float criticalDamagePercent = characterData != null ? characterData.CriticalDamageMultiplier * 100f : 0f;
        criticalDamagePercentText.text = characterData != null ? $"{criticalDamagePercent:0.##}%" : string.Empty;
    }

    public void OpenUpgradePopup()
    {
        if (upgradePopup == null)
        {
            return;
        }

        string popupTitle = characterData != null ? characterData.CharacterName : string.Empty;
        Sprite characterSprite = characterData != null ? characterData.GetTopCharacterSpriteForGrade(6) : null;
        upgradePopup.OpenPopup(characterData, popupTitle, characterSprite, Refresh);
    }

    private void ToggleCharacterUseState()
    {
        if (characterData == null || SC_SaveDataManager.Instance == null)
        {
            return;
        }

        string characterSaveKey = characterData.GetSaveKey();
        if (string.IsNullOrWhiteSpace(characterSaveKey))
        {
            return;
        }

        bool nextUseState = !GetCurrentUseState();
        SC_SaveDataManager.Instance.SetCharacterUseState(characterSaveKey, nextUseState);
        ApplyCheckState();
    }

    private void ApplyCheckState()
    {
        if (checkImage == null)
        {
            return;
        }

        bool isUseEnabled = characterData != null && GetCurrentUseState();
        checkImage.gameObject.SetActive(isUseEnabled);
    }

    private bool GetCurrentUseState()
    {
        if (characterData == null)
        {
            return false;
        }

        string characterSaveKey = characterData.GetSaveKey();
        if (string.IsNullOrWhiteSpace(characterSaveKey) || SC_SaveDataManager.Instance == null)
        {
            return characterData.DefaultUseEnabled;
        }

        return SC_SaveDataManager.Instance.GetCharacterUseState(characterSaveKey, characterData.DefaultUseEnabled);
    }

    private void ResolveCheckReferences()
    {
        if (checkButton == null)
        {
            checkButton = FindChildComponentByName<Button>("BTN_Check");
        }

        if (checkImage == null)
        {
            checkImage = FindChildComponentByName<Image>("IMG_Check");
        }
    }

    private T FindChildComponentByName<T>(string childName) where T : Component
    {
        if (string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        T[] childComponents = GetComponentsInChildren<T>(true);
        for (int i = 0; i < childComponents.Length; i++)
        {
            T childComponent = childComponents[i];
            if (childComponent != null && childComponent.gameObject.name == childName)
            {
                return childComponent;
            }
        }

        return null;
    }
}
