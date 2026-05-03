using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_CurrencyManager : MonoBehaviour
{
    public static SC_CurrencyManager Instance { get; private set; }

    public event Action<int, int> GoldChanged;
    public event Action<int, int> DiamondChanged;

    public int Gold => SC_SaveDataManager.Instance != null ? SC_SaveDataManager.Instance.Gold : 0;
    public int TotalGold => SC_SaveDataManager.Instance != null ? SC_SaveDataManager.Instance.TotalGold : 0;
    public int Diamond => SC_SaveDataManager.Instance != null ? SC_SaveDataManager.Instance.Diamond : 0;
    public int TotalDiamond => SC_SaveDataManager.Instance != null ? SC_SaveDataManager.Instance.TotalDiamond : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstanceOnLoad()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("OBJ_CurrencyManager");
        managerObject.AddComponent<SC_CurrencyManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RaiseCurrencyEvents();
    }

    public void AddGold(int amount)
    {
        if (SC_SaveDataManager.Instance == null)
        {
            return;
        }

        SC_SaveDataManager.Instance.AddGold(amount);
        RaiseGoldChanged();
    }

    public bool SpendGold(int amount)
    {
        if (SC_SaveDataManager.Instance == null)
        {
            return false;
        }

        bool spendSucceeded = SC_SaveDataManager.Instance.SpendGold(amount);
        if (spendSucceeded)
        {
            RaiseGoldChanged();
        }

        return spendSucceeded;
    }

    public void AddDiamond(int amount)
    {
        if (SC_SaveDataManager.Instance == null)
        {
            return;
        }

        SC_SaveDataManager.Instance.AddDiamond(amount);
        RaiseDiamondChanged();
    }

    public bool SpendDiamond(int amount)
    {
        if (SC_SaveDataManager.Instance == null)
        {
            return false;
        }

        bool spendSucceeded = SC_SaveDataManager.Instance.SpendDiamond(amount);
        if (spendSucceeded)
        {
            RaiseDiamondChanged();
        }

        return spendSucceeded;
    }

    public void Refresh()
    {
        RaiseCurrencyEvents();
    }

    private void RaiseCurrencyEvents()
    {
        RaiseGoldChanged();
        RaiseDiamondChanged();
    }

    private void RaiseGoldChanged()
    {
        GoldChanged?.Invoke(Gold, TotalGold);
    }

    private void RaiseDiamondChanged()
    {
        DiamondChanged?.Invoke(Diamond, TotalDiamond);
    }
}
