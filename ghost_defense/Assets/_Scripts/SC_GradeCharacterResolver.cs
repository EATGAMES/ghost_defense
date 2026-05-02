using UnityEngine;

public static class SC_GradeCharacterResolver
{
    public static SO_CharacterData GetCharacterDataForGrade(SO_CharacterData[] equippedRoster, int grade)
    {
        if (equippedRoster == null || equippedRoster.Length <= 0)
        {
            return null;
        }

        int safeGrade = Mathf.Clamp(grade, 1, 10);
        int rosterIndex = (safeGrade - 1) % equippedRoster.Length;
        return rosterIndex >= 0 && rosterIndex < equippedRoster.Length ? equippedRoster[rosterIndex] : null;
    }

    public static int GetCycleIndex(int grade)
    {
        int safeGrade = Mathf.Clamp(grade, 1, 10);
        return safeGrade <= 5 ? 0 : 1;
    }
}
