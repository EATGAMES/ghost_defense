using System;
using System.Text;
using UnityEngine;

public static class SC_RosterSave
{
    private const string RosterOrderKey = "StageRosterOrder";

    public static int[] LoadOrder(int slotCount)
    {
        int safeSlotCount = Mathf.Max(0, slotCount);
        int[] defaultOrder = CreateDefaultOrder(safeSlotCount);

        if (safeSlotCount <= 0 || !PlayerPrefs.HasKey(RosterOrderKey))
        {
            return defaultOrder;
        }

        string savedValue = PlayerPrefs.GetString(RosterOrderKey, string.Empty);
        if (string.IsNullOrWhiteSpace(savedValue))
        {
            return defaultOrder;
        }

        string[] splitValues = savedValue.Split(',');
        if (splitValues.Length != safeSlotCount)
        {
            return defaultOrder;
        }

        int[] loadedOrder = new int[safeSlotCount];
        bool[] usedFlags = new bool[safeSlotCount];

        for (int i = 0; i < safeSlotCount; i++)
        {
            if (!int.TryParse(splitValues[i], out int parsedIndex))
            {
                return defaultOrder;
            }

            if (parsedIndex < 0 || parsedIndex >= safeSlotCount || usedFlags[parsedIndex])
            {
                return defaultOrder;
            }

            usedFlags[parsedIndex] = true;
            loadedOrder[i] = parsedIndex;
        }

        return loadedOrder;
    }

    public static void SaveOrder(int[] rosterOrder)
    {
        if (rosterOrder == null || rosterOrder.Length <= 0)
        {
            PlayerPrefs.DeleteKey(RosterOrderKey);
            PlayerPrefs.Save();
            return;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < rosterOrder.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(rosterOrder[i]);
        }

        PlayerPrefs.SetString(RosterOrderKey, builder.ToString());
        PlayerPrefs.Save();
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
}
