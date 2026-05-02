using UnityEngine;
using UnityEngine.Serialization;

public enum CharacterAttackRole
{
    Single,
    MultiHit,
    Area,
    Support
}

public enum CharacterDamageType
{
    Physical,
    Magic,
    Explosion
}

public enum CharacterAttackStyle
{
    Ranged,
    Melee,
    Summon
}

[CreateAssetMenu(fileName = "SO_CharacterData", menuName = "Ghost Defense/Character Data")]
public class SO_CharacterData : ScriptableObject
{
    [Tooltip("캐릭터 표시 이름입니다.")]
    [SerializeField] private string characterName;

    [Tooltip("캐릭터 설명입니다.")]
    [TextArea(3, 8)]
    [SerializeField] private string characterDescription;

    [Tooltip("상단 공격 캐릭터의 1회차(1~5단계) 스프라이트입니다.")]
    [FormerlySerializedAs("characterSprite")]
    [SerializeField] private Sprite firstCycleTopCharacterSprite;

    [Tooltip("상단 공격 캐릭터의 2회차(6~10단계) 스프라이트입니다.")]
    [SerializeField] private Sprite secondCycleTopCharacterSprite;

    [Tooltip("캐릭터의 데미지 타입입니다.")]
    [SerializeField] private CharacterDamageType damageType = CharacterDamageType.Physical;

    [Tooltip("캐릭터의 공격 방식입니다.")]
    [SerializeField] private CharacterAttackStyle attackStyle = CharacterAttackStyle.Melee;

    [Tooltip("공격 역할 분류입니다.")]
    [SerializeField] private CharacterAttackRole attackRole = CharacterAttackRole.Single;

    [Tooltip("캐릭터의 기본 공격력입니다. 1단계 배율이 1이면 이 값이 그대로 들어갑니다.")]
    [SerializeField] private float baseAttackPower = 10f;

    [Tooltip("1단계부터 10단계까지 적용할 데미지 배율입니다. 1단계는 1 = 100%입니다.")]
    [SerializeField] private float[] gradeDamageMultipliers = new float[10] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

    [Tooltip("치명타 확률입니다. 0에서 1 사이 값을 사용합니다.")]
    [SerializeField] [Range(0f, 1f)] private float criticalChance = 0f;

    [Tooltip("치명타 배수입니다. 1.5이면 150%입니다.")]
    [SerializeField] private float criticalDamageMultiplier = 1.5f;

    [Tooltip("공격 큐 처리 속도 배수입니다. 1이면 기본 속도입니다.")]
    [SerializeField] private float attackQueueSpeedPercent = 1f;

    public string CharacterName => characterName;
    public string CharacterDescription => characterDescription;
    public CharacterDamageType DamageType => damageType;
    public CharacterAttackStyle AttackStyle => attackStyle;
    public CharacterAttackRole AttackRole => attackRole;
    public float BaseAttackPower => baseAttackPower;
    public float CriticalChance => criticalChance;
    public float CriticalDamageMultiplier => Mathf.Max(1f, criticalDamageMultiplier);
    public float AttackQueueSpeedPercent => Mathf.Max(0.01f, attackQueueSpeedPercent);

    private void OnValidate()
    {
        EnsureGradeDamageMultipliers();
    }

    public Sprite GetTopCharacterSpriteForGrade(int grade)
    {
        int cycleIndex = SC_GradeCharacterResolver.GetCycleIndex(grade);
        Sprite cycleSprite = cycleIndex == 0 ? firstCycleTopCharacterSprite : secondCycleTopCharacterSprite;
        if (cycleSprite != null)
        {
            return cycleSprite;
        }

        return firstCycleTopCharacterSprite;
    }

    public float GetGradeDamageMultiplier(int grade)
    {
        EnsureGradeDamageMultipliers();

        int safeIndex = Mathf.Clamp(grade - 1, 0, gradeDamageMultipliers.Length - 1);
        return Mathf.Max(0f, gradeDamageMultipliers[safeIndex]);
    }

    public float CalculateAttackDamage(int mergeGrade)
    {
        int safeGrade = Mathf.Clamp(mergeGrade, 1, 10);
        float damage = Mathf.Max(0f, baseAttackPower) * GetGradeDamageMultiplier(safeGrade);

        if (criticalChance > 0f && Random.value <= criticalChance)
        {
            damage *= CriticalDamageMultiplier;
        }

        return Mathf.Max(0f, damage);
    }

    private void EnsureGradeDamageMultipliers()
    {
        if (gradeDamageMultipliers != null && gradeDamageMultipliers.Length == 10)
        {
            return;
        }

        float[] previousValues = gradeDamageMultipliers;
        gradeDamageMultipliers = new float[10];

        for (int i = 0; i < gradeDamageMultipliers.Length; i++)
        {
            if (previousValues != null && i < previousValues.Length)
            {
                gradeDamageMultipliers[i] = previousValues[i];
            }
            else
            {
                gradeDamageMultipliers[i] = 1f;
            }
        }
    }
}
