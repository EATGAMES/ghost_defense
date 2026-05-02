using UnityEngine;
using UnityEngine.Serialization;

public enum CharacterAttackRole
{
    Single,
    MultiHit,
    Area,
    Support
}

[CreateAssetMenu(fileName = "SO_CharacterData", menuName = "Ghost Defense/Character Data")]
public class SO_CharacterData : ScriptableObject
{
    [Tooltip("캐릭터 표시 이름입니다.")]
    [SerializeField] private string characterName;

    [Tooltip("캐릭터 설명입니다.")]
    [TextArea(3, 8)]
    [SerializeField] private string characterDescription;

    [Tooltip("상단 공격 캐릭터로 표시할 기본 스프라이트입니다.")]
    [FormerlySerializedAs("characterSprite")]
    [SerializeField] private Sprite topCharacterSprite;

    [Tooltip("이 캐릭터가 1회차로 등장할 때 사용할 필드 스프라이트입니다.")]
    [SerializeField] private Sprite firstCycleFieldSprite;

    [Tooltip("이 캐릭터가 2회차로 등장할 때 사용할 필드 스프라이트입니다.")]
    [SerializeField] private Sprite secondCycleFieldSprite;

    [Tooltip("공격 역할 분류입니다.")]
    [SerializeField] private CharacterAttackRole attackRole = CharacterAttackRole.Single;

    [Tooltip("캐릭터 기본 공격력입니다.")]
    [SerializeField] private float baseAttackPower = 10f;

    [Tooltip("머지 단계 1당 적용할 공격 배수입니다.")]
    [SerializeField] private float mergeGradeMultiplierPerStep = 1f;

    [Tooltip("최종 공격력 보정 배율입니다. 1이면 100%입니다.")]
    [SerializeField] private float attackPowerPercent = 1f;

    [Tooltip("고단계 보너스를 적용하기 시작할 단계입니다.")]
    [SerializeField] private int highGradeStart = 5;

    [Tooltip("고단계 공격 시 추가할 보너스 배율입니다. 0.3이면 30%입니다.")]
    [SerializeField] private float highGradeBonusPercent = 0f;

    [Tooltip("10단계 공격 시 추가할 보너스 배율입니다. 0.5이면 50%입니다.")]
    [SerializeField] private float finalGradeBonusPercent = 0f;

    [Tooltip("치명타 확률입니다. 0에서 1 사이 값을 사용합니다.")]
    [SerializeField] [Range(0f, 1f)] private float criticalChance = 0f;

    [Tooltip("치명타 배율입니다. 1.5이면 150%입니다.")]
    [SerializeField] private float criticalDamageMultiplier = 1.5f;

    [Tooltip("공격 큐 처리 속도 배율입니다. 1이면 기본 속도입니다.")]
    [SerializeField] private float attackQueueSpeedPercent = 1f;

    public string CharacterName => characterName;
    public string CharacterDescription => characterDescription;
    public Sprite TopCharacterSprite => topCharacterSprite;
    public CharacterAttackRole AttackRole => attackRole;
    public float BaseAttackPower => baseAttackPower;
    public float MergeGradeMultiplierPerStep => mergeGradeMultiplierPerStep;
    public float AttackPowerPercent => attackPowerPercent;
    public int HighGradeStart => Mathf.Max(1, highGradeStart);
    public float HighGradeBonusPercent => highGradeBonusPercent;
    public float FinalGradeBonusPercent => finalGradeBonusPercent;
    public float CriticalChance => criticalChance;
    public float CriticalDamageMultiplier => Mathf.Max(1f, criticalDamageMultiplier);
    public float AttackQueueSpeedPercent => Mathf.Max(0.01f, attackQueueSpeedPercent);

    public Sprite GetFieldSpriteForGrade(int grade)
    {
        int cycleIndex = SC_GradeCharacterResolver.GetCycleIndex(grade);
        return cycleIndex == 0 ? firstCycleFieldSprite : secondCycleFieldSprite;
    }

    public float CalculateAttackDamage(int mergeGrade)
    {
        int safeGrade = Mathf.Clamp(mergeGrade, 1, 10);
        float gradeMultiplier = Mathf.Max(1f, safeGrade * Mathf.Max(0.01f, mergeGradeMultiplierPerStep));
        float damage = Mathf.Max(0f, baseAttackPower) * gradeMultiplier * Mathf.Max(0f, attackPowerPercent);

        if (safeGrade >= HighGradeStart)
        {
            damage *= 1f + Mathf.Max(0f, highGradeBonusPercent);
        }

        if (safeGrade >= 10)
        {
            damage *= 1f + Mathf.Max(0f, finalGradeBonusPercent);
        }

        if (criticalChance > 0f && Random.value <= criticalChance)
        {
            damage *= CriticalDamageMultiplier;
        }

        return Mathf.Max(0f, damage);
    }
}
