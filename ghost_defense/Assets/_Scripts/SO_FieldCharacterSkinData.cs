using UnityEngine;

[CreateAssetMenu(fileName = "SO_FieldCharacterSkinData", menuName = "Ghost Defense/Field Character Skin Data")]
public class SO_FieldCharacterSkinData : ScriptableObject
{
    [Tooltip("필드 1순환(1~5등급)에서 사용하는 캐릭터 스프라이트입니다.")]
    [SerializeField] private Sprite firstCycleFieldSprite;

    [Tooltip("필드 2순환(6~10등급)에서 사용하는 캐릭터 스프라이트입니다.")]
    [SerializeField] private Sprite secondCycleFieldSprite;

    [Tooltip("프리뷰 1순환(1~5등급)에서 사용하는 미리보기 스프라이트입니다.")]
    [SerializeField] private Sprite firstCyclePreviewSprite;

    [Tooltip("프리뷰 2순환(6~10등급)에서 사용하는 미리보기 스프라이트입니다.")]
    [SerializeField] private Sprite secondCyclePreviewSprite;

    public Sprite GetFieldSpriteForGrade(int grade)
    {
        int cycleIndex = SC_GradeCharacterResolver.GetCycleIndex(grade);
        return cycleIndex == 0 ? firstCycleFieldSprite : secondCycleFieldSprite;
    }

    public Sprite GetPreviewSpriteForGrade(int grade)
    {
        int cycleIndex = SC_GradeCharacterResolver.GetCycleIndex(grade);
        return cycleIndex == 0 ? firstCyclePreviewSprite : secondCyclePreviewSprite;
    }
}
