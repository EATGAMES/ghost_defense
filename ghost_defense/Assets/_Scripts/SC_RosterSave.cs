using System;
using UnityEngine;

public static class SC_RosterSave
{
    public static event Action<int[]> RosterOrderChanged;

    public static int[] LoadOrder(int slotCount)
    {
        int safeSlotCount = Mathf.Max(0, slotCount);
        int[] defaultOrder = CreateDefaultOrder(safeSlotCount);

        if (safeSlotCount <= 0 || SC_SaveDataManager.Instance == null)
        {
            return defaultOrder;
        }

        int[] loadedOrder = SC_SaveDataManager.Instance.GetRosterOrder(safeSlotCount);
        if (loadedOrder == null || loadedOrder.Length != safeSlotCount)
        {
            return defaultOrder;
        }

        bool[] usedFlags = new bool[safeSlotCount];
        for (int i = 0; i < loadedOrder.Length; i++)
        {
            int parsedIndex = loadedOrder[i];
            if (parsedIndex < 0 || parsedIndex >= safeSlotCount || usedFlags[parsedIndex])
            {
                return defaultOrder;
            }

            usedFlags[parsedIndex] = true;
        }

        return loadedOrder;
    }

    public static void SaveOrder(int[] rosterOrder)
    {
        int[] copiedOrder = CopyOrder(rosterOrder);

        if (SC_SaveDataManager.Instance != null)
        {
            SC_SaveDataManager.Instance.SetRosterOrder(copiedOrder);
        }

        RosterOrderChanged?.Invoke(CopyOrder(copiedOrder));
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

    private static int[] CopyOrder(int[] rosterOrder)
    {
        if (rosterOrder == null || rosterOrder.Length <= 0)
        {
            return Array.Empty<int>();
        }

        int[] copiedOrder = new int[rosterOrder.Length];
        Array.Copy(rosterOrder, copiedOrder, rosterOrder.Length);
        return copiedOrder;
    }
}
