using System;

public static class SC_BattleModeContext
{
    public const string ShootBattleSceneName = "SCN_Battle";
    public const string DropBattleSceneName = "SCN_Battle_Drop";

    public static bool IsDropDirection(StageBattleDirection battleDirection)
    {
        return battleDirection == StageBattleDirection.DOWN;
    }

    public static string GetBattleSceneName(StageBattleDirection battleDirection)
    {
        return IsDropDirection(battleDirection) ? DropBattleSceneName : ShootBattleSceneName;
    }

    public static StageBattleDirection GetBattleDirectionBySceneName(string sceneName)
    {
        if (string.Equals(sceneName, DropBattleSceneName, StringComparison.Ordinal))
        {
            return StageBattleDirection.DOWN;
        }

        return StageBattleDirection.UP;
    }
}
