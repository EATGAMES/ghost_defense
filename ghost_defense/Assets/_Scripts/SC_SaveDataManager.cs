using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveIntEntry
{
    public int Key;
    public int Value;
}

[Serializable]
public class SaveStringIntEntry
{
    public string Key;
    public int Value;
}

[Serializable]
public class SaveStringBoolEntry
{
    public string Key;
    public bool Value;
}

[Serializable]
public class SaveCharacterUpgradeEntry
{
    public string CharacterId;
    public int DamageLevel;
    public int CriticalChanceLevel;
    public int CriticalDamageLevel;
    public int UniqueSkillLevel;
}

[Serializable]
public class SC_GameSaveData
{
    public int Gold;
    public int TotalGold;
    public int Diamond;
    public int TotalDiamond;
    public List<SaveIntEntry> StageClearEntries = new List<SaveIntEntry>();
    public List<SaveIntEntry> StageGrade10CreatedEntries = new List<SaveIntEntry>();
    public List<SaveIntEntry> GradeSpawnCountEntries = new List<SaveIntEntry>();
    public int[] RosterOrder = Array.Empty<int>();
    public List<SaveCharacterUpgradeEntry> CharacterUpgradeEntries = new List<SaveCharacterUpgradeEntry>();
    public List<SaveStringIntEntry> CardUseCountEntries = new List<SaveStringIntEntry>();
    public List<SaveStringIntEntry> CardLevelEntries = new List<SaveStringIntEntry>();
    public bool HasVipMembership;
    public int TotalAdViewCount;
}

[DisallowMultipleComponent]
public class SC_SaveDataManager : MonoBehaviour
{
    private const string SaveDataKey = "GhostDefense.GameSaveData";

    public static SC_SaveDataManager Instance { get; private set; }

    [Tooltip("저장 데이터를 값 변경 즉시 PlayerPrefs에 반영할지 여부입니다.")]
    [SerializeField] private bool saveImmediately = true;

    private SC_GameSaveData saveData;

    public int Gold => saveData != null ? Mathf.Max(0, saveData.Gold) : 0;
    public int TotalGold => saveData != null ? Mathf.Max(0, saveData.TotalGold) : 0;
    public int Diamond => saveData != null ? Mathf.Max(0, saveData.Diamond) : 0;
    public int TotalDiamond => saveData != null ? Mathf.Max(0, saveData.TotalDiamond) : 0;
    public bool HasVipMembership => saveData != null && saveData.HasVipMembership;
    public int TotalAdViewCount => saveData != null ? Mathf.Max(0, saveData.TotalAdViewCount) : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstanceOnLoad()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("OBJ_SaveDataManager");
        managerObject.AddComponent<SC_SaveDataManager>();
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
        Load();
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(SaveDataKey))
        {
            saveData = CreateDefaultSaveData();
            return;
        }

        string savedJson = PlayerPrefs.GetString(SaveDataKey, string.Empty);
        if (string.IsNullOrWhiteSpace(savedJson))
        {
            saveData = CreateDefaultSaveData();
            return;
        }

        saveData = JsonUtility.FromJson<SC_GameSaveData>(savedJson);
        if (saveData == null)
        {
            saveData = CreateDefaultSaveData();
        }

        EnsureCollections();
    }

    public void Save()
    {
        EnsureCollections();
        string savedJson = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SaveDataKey, savedJson);
        PlayerPrefs.Save();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        saveData.Gold += amount;
        saveData.TotalGold += amount;
        SaveIfNeeded();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (saveData.Gold < amount)
        {
            return false;
        }

        saveData.Gold -= amount;
        SaveIfNeeded();
        return true;
    }

    public void AddDiamond(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        saveData.Diamond += amount;
        saveData.TotalDiamond += amount;
        SaveIfNeeded();
    }

    public bool SpendDiamond(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (saveData.Diamond < amount)
        {
            return false;
        }

        saveData.Diamond -= amount;
        SaveIfNeeded();
        return true;
    }

    public bool IsStageCleared(int stageId)
    {
        return GetIntEntryValue(saveData.StageClearEntries, stageId) > 0;
    }

    public void SetStageCleared(int stageId, bool isCleared)
    {
        SetIntEntryValue(saveData.StageClearEntries, stageId, isCleared ? 1 : 0);
        SaveIfNeeded();
    }

    public bool HasCreatedGrade10InStage(int stageId)
    {
        return GetIntEntryValue(saveData.StageGrade10CreatedEntries, stageId) > 0;
    }

    public void SetCreatedGrade10InStage(int stageId, bool hasCreated)
    {
        SetIntEntryValue(saveData.StageGrade10CreatedEntries, stageId, hasCreated ? 1 : 0);
        SaveIfNeeded();
    }

    public int GetGradeSpawnCount(int grade)
    {
        return Mathf.Max(0, GetIntEntryValue(saveData.GradeSpawnCountEntries, grade));
    }

    public void AddGradeSpawnCount(int grade, int amount = 1)
    {
        if (grade <= 0 || amount <= 0)
        {
            return;
        }

        int currentValue = GetIntEntryValue(saveData.GradeSpawnCountEntries, grade);
        SetIntEntryValue(saveData.GradeSpawnCountEntries, grade, currentValue + amount);
        SaveIfNeeded();
    }

    public int[] GetRosterOrder(int slotCount)
    {
        int safeSlotCount = Mathf.Max(0, slotCount);
        if (safeSlotCount <= 0)
        {
            return Array.Empty<int>();
        }

        if (saveData.RosterOrder == null || saveData.RosterOrder.Length != safeSlotCount)
        {
            return CreateDefaultOrder(safeSlotCount);
        }

        int[] copiedOrder = new int[safeSlotCount];
        Array.Copy(saveData.RosterOrder, copiedOrder, safeSlotCount);
        return copiedOrder;
    }

    public void SetRosterOrder(int[] rosterOrder)
    {
        if (rosterOrder == null || rosterOrder.Length <= 0)
        {
            saveData.RosterOrder = Array.Empty<int>();
            SaveIfNeeded();
            return;
        }

        saveData.RosterOrder = new int[rosterOrder.Length];
        Array.Copy(rosterOrder, saveData.RosterOrder, rosterOrder.Length);
        SaveIfNeeded();
    }

    public SaveCharacterUpgradeEntry GetCharacterUpgradeEntry(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return CreateCharacterUpgradeEntry(string.Empty);
        }

        SaveCharacterUpgradeEntry existingEntry = saveData.CharacterUpgradeEntries.Find(entry => entry.CharacterId == characterId);
        if (existingEntry != null)
        {
            return existingEntry;
        }

        SaveCharacterUpgradeEntry newEntry = CreateCharacterUpgradeEntry(characterId);
        saveData.CharacterUpgradeEntries.Add(newEntry);
        SaveIfNeeded();
        return newEntry;
    }

    public void SetCharacterUpgradeLevels(string characterId, int damageLevel, int criticalChanceLevel, int criticalDamageLevel, int uniqueSkillLevel)
    {
        SaveCharacterUpgradeEntry upgradeEntry = GetCharacterUpgradeEntry(characterId);
        upgradeEntry.DamageLevel = Mathf.Max(0, damageLevel);
        upgradeEntry.CriticalChanceLevel = Mathf.Max(0, criticalChanceLevel);
        upgradeEntry.CriticalDamageLevel = Mathf.Max(0, criticalDamageLevel);
        upgradeEntry.UniqueSkillLevel = Mathf.Max(0, uniqueSkillLevel);
        SaveIfNeeded();
    }

    public int GetCardUseCount(string cardId)
    {
        return Mathf.Max(0, GetStringIntEntryValue(saveData.CardUseCountEntries, cardId));
    }

    public void AddCardUseCount(string cardId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(cardId) || amount <= 0)
        {
            return;
        }

        int currentValue = GetStringIntEntryValue(saveData.CardUseCountEntries, cardId);
        SetStringIntEntryValue(saveData.CardUseCountEntries, cardId, currentValue + amount);
        SaveIfNeeded();
    }

    public int GetCardLevel(string cardId)
    {
        return Mathf.Max(0, GetStringIntEntryValue(saveData.CardLevelEntries, cardId));
    }

    public void SetCardLevel(string cardId, int level)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return;
        }

        SetStringIntEntryValue(saveData.CardLevelEntries, cardId, Mathf.Max(0, level));
        SaveIfNeeded();
    }

    public void SetVipMembership(bool hasVipMembership)
    {
        saveData.HasVipMembership = hasVipMembership;
        SaveIfNeeded();
    }

    public void AddAdViewCount(int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        saveData.TotalAdViewCount += amount;
        SaveIfNeeded();
    }

    public void ResetAllSaveData()
    {
        saveData = CreateDefaultSaveData();
        Save();
    }

    public string BuildDebugSummary()
    {
        EnsureCollections();

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[Currency]");
        builder.AppendLine($"Gold: {Gold}");
        builder.AppendLine($"Total Gold: {TotalGold}");
        builder.AppendLine($"Diamond: {Diamond}");
        builder.AppendLine($"Total Diamond: {TotalDiamond}");
        builder.AppendLine();

        builder.AppendLine("[Stage]");
        AppendIntEntryList(builder, "Cleared Stages", saveData.StageClearEntries);
        AppendIntEntryList(builder, "Grade10 Created", saveData.StageGrade10CreatedEntries);
        builder.AppendLine();

        builder.AppendLine("[Grade Spawn Count]");
        AppendIntEntryList(builder, "Spawn Counts", saveData.GradeSpawnCountEntries);
        builder.AppendLine();

        builder.AppendLine("[Roster]");
        builder.AppendLine($"Order: {FormatRosterOrder(saveData.RosterOrder)}");
        builder.AppendLine();

        builder.AppendLine("[Character Upgrade]");
        AppendCharacterUpgradeEntries(builder, saveData.CharacterUpgradeEntries);
        builder.AppendLine();

        builder.AppendLine("[Card]");
        AppendStringIntEntryList(builder, "Use Counts", saveData.CardUseCountEntries);
        AppendStringIntEntryList(builder, "Levels", saveData.CardLevelEntries);
        builder.AppendLine();

        builder.AppendLine("[Account]");
        builder.AppendLine($"VIP: {(saveData.HasVipMembership ? 1 : 0)}");
        builder.AppendLine($"Total Ad Views: {TotalAdViewCount}");

        return builder.ToString();
    }

    private void SaveIfNeeded()
    {
        if (!saveImmediately)
        {
            return;
        }

        Save();
    }

    private static SC_GameSaveData CreateDefaultSaveData()
    {
        SC_GameSaveData defaultData = new SC_GameSaveData
        {
            RosterOrder = Array.Empty<int>()
        };
        return defaultData;
    }

    private void EnsureCollections()
    {
        if (saveData == null)
        {
            saveData = CreateDefaultSaveData();
        }

        saveData.StageClearEntries ??= new List<SaveIntEntry>();
        saveData.StageGrade10CreatedEntries ??= new List<SaveIntEntry>();
        saveData.GradeSpawnCountEntries ??= new List<SaveIntEntry>();
        saveData.RosterOrder ??= Array.Empty<int>();
        saveData.CharacterUpgradeEntries ??= new List<SaveCharacterUpgradeEntry>();
        saveData.CardUseCountEntries ??= new List<SaveStringIntEntry>();
        saveData.CardLevelEntries ??= new List<SaveStringIntEntry>();
    }

    private static int[] CreateDefaultOrder(int slotCount)
    {
        int[] defaultOrder = new int[Mathf.Max(0, slotCount)];
        for (int i = 0; i < defaultOrder.Length; i++)
        {
            defaultOrder[i] = i;
        }

        return defaultOrder;
    }

    private static int GetIntEntryValue(List<SaveIntEntry> entries, int key)
    {
        if (entries == null)
        {
            return 0;
        }

        SaveIntEntry targetEntry = entries.Find(entry => entry.Key == key);
        return targetEntry != null ? targetEntry.Value : 0;
    }

    private static void SetIntEntryValue(List<SaveIntEntry> entries, int key, int value)
    {
        if (entries == null)
        {
            return;
        }

        SaveIntEntry targetEntry = entries.Find(entry => entry.Key == key);
        if (targetEntry != null)
        {
            targetEntry.Value = value;
            return;
        }

        entries.Add(new SaveIntEntry
        {
            Key = key,
            Value = value
        });
    }

    private static int GetStringIntEntryValue(List<SaveStringIntEntry> entries, string key)
    {
        if (entries == null || string.IsNullOrWhiteSpace(key))
        {
            return 0;
        }

        SaveStringIntEntry targetEntry = entries.Find(entry => entry.Key == key);
        return targetEntry != null ? targetEntry.Value : 0;
    }

    private static void SetStringIntEntryValue(List<SaveStringIntEntry> entries, string key, int value)
    {
        if (entries == null || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        SaveStringIntEntry targetEntry = entries.Find(entry => entry.Key == key);
        if (targetEntry != null)
        {
            targetEntry.Value = value;
            return;
        }

        entries.Add(new SaveStringIntEntry
        {
            Key = key,
            Value = value
        });
    }

    private static SaveCharacterUpgradeEntry CreateCharacterUpgradeEntry(string characterId)
    {
        return new SaveCharacterUpgradeEntry
        {
            CharacterId = characterId ?? string.Empty,
            DamageLevel = 0,
            CriticalChanceLevel = 0,
            CriticalDamageLevel = 0,
            UniqueSkillLevel = 0
        };
    }

    private static void AppendIntEntryList(StringBuilder builder, string label, List<SaveIntEntry> entries)
    {
        builder.Append(label);
        builder.Append(": ");

        if (entries == null || entries.Count <= 0)
        {
            builder.AppendLine("-");
            return;
        }

        bool hasValue = false;
        for (int i = 0; i < entries.Count; i++)
        {
            SaveIntEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (hasValue)
            {
                builder.Append(", ");
            }

            builder.Append(entry.Key);
            builder.Append('=');
            builder.Append(entry.Value);
            hasValue = true;
        }

        if (!hasValue)
        {
            builder.Append('-');
        }

        builder.AppendLine();
    }

    private static void AppendStringIntEntryList(StringBuilder builder, string label, List<SaveStringIntEntry> entries)
    {
        builder.Append(label);
        builder.Append(": ");

        if (entries == null || entries.Count <= 0)
        {
            builder.AppendLine("-");
            return;
        }

        bool hasValue = false;
        for (int i = 0; i < entries.Count; i++)
        {
            SaveStringIntEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
            {
                continue;
            }

            if (hasValue)
            {
                builder.Append(", ");
            }

            builder.Append(entry.Key);
            builder.Append('=');
            builder.Append(entry.Value);
            hasValue = true;
        }

        if (!hasValue)
        {
            builder.Append('-');
        }

        builder.AppendLine();
    }

    private static void AppendCharacterUpgradeEntries(StringBuilder builder, List<SaveCharacterUpgradeEntry> entries)
    {
        if (entries == null || entries.Count <= 0)
        {
            builder.AppendLine("-");
            return;
        }

        bool hasValue = false;
        for (int i = 0; i < entries.Count; i++)
        {
            SaveCharacterUpgradeEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.CharacterId))
            {
                continue;
            }

            builder.Append(entry.CharacterId);
            builder.Append(": ");
            builder.Append("DMG=");
            builder.Append(entry.DamageLevel);
            builder.Append(", CRI%");
            builder.Append('=');
            builder.Append(entry.CriticalChanceLevel);
            builder.Append(", CRI DMG=");
            builder.Append(entry.CriticalDamageLevel);
            builder.Append(", SKILL=");
            builder.Append(entry.UniqueSkillLevel);
            builder.AppendLine();
            hasValue = true;
        }

        if (!hasValue)
        {
            builder.AppendLine("-");
        }
    }

    private static string FormatRosterOrder(int[] rosterOrder)
    {
        if (rosterOrder == null || rosterOrder.Length <= 0)
        {
            return "-";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < rosterOrder.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(rosterOrder[i]);
        }

        return builder.ToString();
    }
}
