using System.Collections.Generic;
using UnityEngine;

public static class SC_FieldCharacterRegistry
{
    private static readonly HashSet<IFieldCharacterRuntime> registeredCharacters = new HashSet<IFieldCharacterRuntime>();
    private static readonly List<IFieldCharacterRuntime> snapshotBuffer = new List<IFieldCharacterRuntime>();

    public static int Count => registeredCharacters.Count;

    public static void Register(IFieldCharacterRuntime runtime)
    {
        if (!IsValidRuntime(runtime))
        {
            return;
        }

        registeredCharacters.Add(runtime);
    }

    public static void Unregister(IFieldCharacterRuntime runtime)
    {
        if (runtime == null)
        {
            return;
        }

        registeredCharacters.Remove(runtime);
    }

    public static List<IFieldCharacterRuntime> GetSnapshot()
    {
        CleanupDestroyedEntries();
        snapshotBuffer.Clear();
        snapshotBuffer.AddRange(registeredCharacters);
        return snapshotBuffer;
    }

    public static List<IFieldCharacterRuntime> GetSnapshot(StageBattleDirection battleDirection)
    {
        CleanupDestroyedEntries();
        snapshotBuffer.Clear();

        foreach (IFieldCharacterRuntime runtime in registeredCharacters)
        {
            if (!IsValidRuntime(runtime) || runtime.BattleDirection != battleDirection)
            {
                continue;
            }

            snapshotBuffer.Add(runtime);
        }

        return snapshotBuffer;
    }

    private static void CleanupDestroyedEntries()
    {
        registeredCharacters.RemoveWhere(runtime => !IsValidRuntime(runtime));
    }

    private static bool IsValidRuntime(IFieldCharacterRuntime runtime)
    {
        return runtime != null && runtime.RuntimeObject != null;
    }
}
