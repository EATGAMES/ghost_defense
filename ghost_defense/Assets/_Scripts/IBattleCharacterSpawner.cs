using System;

public interface IBattleCharacterSpawner
{
    event Action NextSpawnPreviewChanged;

    StageBattleDirection BattleDirection { get; }
    bool IsSpawnerActive { get; }

    int GetNextSpawnPreviewGrade();
    void RefreshNextSpawnPreviewGrade();
    void QueueNextCharacterGrade(int grade);
}
