using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_CurrencyUI : MonoBehaviour
{
    [Tooltip("골드 값을 표시할 TMP_Text입니다. 비어 있으면 이름이 TXT_Gold인 오브젝트를 찾습니다.")]
    [SerializeField] private TMP_Text goldText;

    [Tooltip("다이아 값을 표시할 TMP_Text입니다. 비어 있으면 이름이 TXT_Dia인 오브젝트를 찾습니다.")]
    [SerializeField] private TMP_Text diamondText;

    [Tooltip("재화 변경 이벤트를 받을 재화 매니저입니다. 비어 있으면 자동으로 전역 인스턴스를 사용합니다.")]
    [SerializeField] private SC_CurrencyManager currencyManager;

    private void Awake()
    {
        if (currencyManager == null)
        {
            currencyManager = SC_CurrencyManager.Instance;
        }

        if (goldText == null)
        {
            goldText = FindTextByName("TXT_Gold");
        }

        if (diamondText == null)
        {
            diamondText = FindTextByName("TXT_Dia");
        }
    }

    private void OnEnable()
    {
        if (currencyManager == null)
        {
            currencyManager = SC_CurrencyManager.Instance;
        }

        if (currencyManager == null)
        {
            RefreshTexts(0, 0);
            return;
        }

        currencyManager.GoldChanged += OnGoldChanged;
        currencyManager.DiamondChanged += OnDiamondChanged;
        RefreshTexts(currencyManager.Gold, currencyManager.Diamond);
    }

    private void OnDisable()
    {
        if (currencyManager == null)
        {
            return;
        }

        currencyManager.GoldChanged -= OnGoldChanged;
        currencyManager.DiamondChanged -= OnDiamondChanged;
    }

    private void OnGoldChanged(int currentGold, int totalGold)
    {
        if (goldText != null)
        {
            goldText.text = currentGold.ToString("N0");
        }
    }

    private void OnDiamondChanged(int currentDiamond, int totalDiamond)
    {
        if (diamondText != null)
        {
            diamondText.text = currentDiamond.ToString("N0");
        }
    }

    private void RefreshTexts(int currentGold, int currentDiamond)
    {
        if (goldText != null)
        {
            goldText.text = currentGold.ToString("N0");
        }

        if (diamondText != null)
        {
            diamondText.text = currentDiamond.ToString("N0");
        }
    }

    private static TMP_Text FindTextByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text targetText = allTexts[i];
            if (targetText != null && targetText.name == targetName)
            {
                return targetText;
            }
        }

        return null;
    }
}
