using UnityEngine;

public interface IFieldCharacterRuntime
{
    StageBattleDirection BattleDirection { get; }
    GameObject RuntimeObject { get; }
    Transform RuntimeTransform { get; }
    int MergeGrade { get; }
    bool IsWaiting { get; }
    bool IsLaunched { get; }
    bool IsDragging { get; }
    bool IsActiveFieldCharacter { get; }
    Vector2 CurrentVelocity { get; }

    void CancelInputAndReset();
    void CancelInputAndSuppressUntilRelease();
    void SetShrinkVisual(bool shouldShrink);
}
