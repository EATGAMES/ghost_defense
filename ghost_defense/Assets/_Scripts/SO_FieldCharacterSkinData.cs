using UnityEngine;

[CreateAssetMenu(fileName = "SO_FieldCharacterSkinData", menuName = "Ghost Defense/Field Character Skin Data")]
public class SO_FieldCharacterSkinData : ScriptableObject
{
    [Tooltip("필드 1순환(1~5단계)에서 사용할 스프라이트입니다.")]
    [SerializeField] private Sprite firstCycleFieldSprite;

    [Tooltip("필드 2순환(6~10단계)에서 사용할 스프라이트입니다.")]
    [SerializeField] private Sprite secondCycleFieldSprite;

    public Sprite GetFieldSpriteForGrade(int grade)
    {
        int cycleIndex = SC_GradeCharacterResolver.GetCycleIndex(grade);
        return cycleIndex == 0 ? firstCycleFieldSprite : secondCycleFieldSprite;
    }
}
