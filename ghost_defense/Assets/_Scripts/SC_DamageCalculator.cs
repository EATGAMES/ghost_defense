using UnityEngine;

[DisallowMultipleComponent]
public class SC_DamageCalculator : MonoBehaviour
{
    private const float WeaknessDamageBonusMultiplier = 1.2f;

    public readonly struct DamageResult
    {
        public readonly float FinalDamage;
        public readonly bool IsCritical;

        public DamageResult(float finalDamage, bool isCritical)
        {
            FinalDamage = Mathf.Max(0f, finalDamage);
            IsCritical = isCritical;
        }
    }

    public readonly struct DamageContext
    {
        public readonly SO_CharacterData Attacker;
        public readonly SC_MonsterHealth TargetBoss;
        public readonly int MergeGrade;
        public readonly bool ApplyFirstMergedAttackBonus;
        public readonly float NextAttackDamageMultiplier;

        public DamageContext(SO_CharacterData attacker, SC_MonsterHealth targetBoss, int mergeGrade, bool applyFirstMergedAttackBonus, float nextAttackDamageMultiplier)
        {
            Attacker = attacker;
            TargetBoss = targetBoss;
            MergeGrade = mergeGrade;
            ApplyFirstMergedAttackBonus = applyFirstMergedAttackBonus;
            NextAttackDamageMultiplier = Mathf.Max(1f, nextAttackDamageMultiplier);
        }
    }

    [Tooltip("모든 캐릭터의 크리티컬 확률에 추가할 비율입니다. 0.05는 5%입니다.")]
    [SerializeField] [Range(0f, 1f)] private float globalCriticalChanceBonus;

    [Tooltip("모든 캐릭터의 크리티컬 데미지 배수에 추가할 비율입니다. 0.5는 +50%입니다.")]
    [SerializeField] private float globalCriticalDamageMultiplierBonus;

    [Tooltip("물리 공격 데미지 증가 비율입니다. 0.2는 +20%입니다.")]
    [SerializeField] private float physicalDamageBonus;

    [Tooltip("마법 공격 데미지 증가 비율입니다. 0.2는 +20%입니다.")]
    [SerializeField] private float magicDamageBonus;

    [Tooltip("폭발 공격 데미지 증가 비율입니다. 0.2는 +20%입니다.")]
    [SerializeField] private float explosionDamageBonus;

    [Tooltip("근접 공격 데미지 증가 비율입니다. 0.2는 +20%입니다.")]
    [SerializeField] private float meleeDamageBonus;

    [Tooltip("원거리 공격 데미지 증가 비율입니다. 0.2는 +20%입니다.")]
    [SerializeField] private float rangedDamageBonus;

    [Tooltip("소환 공격 데미지 증가 비율입니다. 0.2는 +20%입니다.")]
    [SerializeField] private float summonDamageBonus;

    [Tooltip("10단계 캐릭터 추가 데미지 증가 비율입니다. 0.5는 +50%입니다.")]
    [SerializeField] private float grade10DamageBonus;

    [Tooltip("모든 데미지에 적용할 추가 증가 비율입니다. 0.1은 +10%입니다.")]
    [SerializeField] private float globalDamageBonus;

    [Tooltip("최초 합체 1회 보너스 배수입니다. 10이면 10배입니다.")]
    [SerializeField] private float firstMergedAttackDamageMultiplier = 10f;

    public DamageResult CalculateDamage(DamageContext context)
    {
        if (context.Attacker == null)
        {
            return new DamageResult(0f, false);
        }

        int safeGrade = Mathf.Clamp(context.MergeGrade, 1, 10);
        float finalDamage = context.Attacker.GetBaseDamage(safeGrade);

        finalDamage *= 1f + GetDamageTypeBonus(context.Attacker.DamageType);
        finalDamage *= 1f + GetAttackStyleBonus(context.Attacker.AttackStyle);

        if (safeGrade == 10)
        {
            finalDamage *= 1f + grade10DamageBonus;
        }

        finalDamage *= 1f + globalDamageBonus;

        if (context.ApplyFirstMergedAttackBonus)
        {
            finalDamage *= Mathf.Max(1f, firstMergedAttackDamageMultiplier);
            finalDamage *= context.NextAttackDamageMultiplier / Mathf.Max(1f, firstMergedAttackDamageMultiplier);
        }

        if (HasMatchingWeakness(context))
        {
            finalDamage *= WeaknessDamageBonusMultiplier;
        }

        float criticalChance = Mathf.Clamp01(context.Attacker.GetCriticalChance() + globalCriticalChanceBonus);
        float criticalDamageMultiplier = Mathf.Max(1f, context.Attacker.GetCriticalDamageMultiplier() + globalCriticalDamageMultiplierBonus);
        bool isCritical = criticalChance > 0f && Random.value <= criticalChance;
        if (isCritical)
        {
            finalDamage *= criticalDamageMultiplier;
        }

        return new DamageResult(finalDamage, isCritical);
    }

    public void SetGlobalCriticalChanceBonus(float bonus)
    {
        globalCriticalChanceBonus = Mathf.Clamp01(bonus);
    }

    public void SetGlobalCriticalDamageMultiplierBonus(float bonus)
    {
        globalCriticalDamageMultiplierBonus = bonus;
    }

    public void SetDamageTypeBonus(CharacterDamageType damageType, float bonus)
    {
        switch (damageType)
        {
            case CharacterDamageType.Physical:
                physicalDamageBonus = bonus;
                break;
            case CharacterDamageType.Magic:
                magicDamageBonus = bonus;
                break;
            case CharacterDamageType.Explosion:
                explosionDamageBonus = bonus;
                break;
        }
    }

    public void SetAttackStyleBonus(CharacterAttackStyle attackStyle, float bonus)
    {
        switch (attackStyle)
        {
            case CharacterAttackStyle.Melee:
                meleeDamageBonus = bonus;
                break;
            case CharacterAttackStyle.Ranged:
                rangedDamageBonus = bonus;
                break;
            case CharacterAttackStyle.Summon:
                summonDamageBonus = bonus;
                break;
        }
    }

    public void SetGrade10DamageBonus(float bonus)
    {
        grade10DamageBonus = bonus;
    }

    public void SetGlobalDamageBonus(float bonus)
    {
        globalDamageBonus = bonus;
    }

    public void SetFirstMergedAttackDamageMultiplier(float multiplier)
    {
        firstMergedAttackDamageMultiplier = Mathf.Max(1f, multiplier);
    }

    public void ResetAllModifiers()
    {
        globalCriticalChanceBonus = 0f;
        globalCriticalDamageMultiplierBonus = 0f;
        physicalDamageBonus = 0f;
        magicDamageBonus = 0f;
        explosionDamageBonus = 0f;
        meleeDamageBonus = 0f;
        rangedDamageBonus = 0f;
        summonDamageBonus = 0f;
        grade10DamageBonus = 0f;
        globalDamageBonus = 0f;
        firstMergedAttackDamageMultiplier = 10f;
    }

    private float GetDamageTypeBonus(CharacterDamageType damageType)
    {
        switch (damageType)
        {
            case CharacterDamageType.Physical:
                return physicalDamageBonus;
            case CharacterDamageType.Magic:
                return magicDamageBonus;
            case CharacterDamageType.Explosion:
                return explosionDamageBonus;
            default:
                return 0f;
        }
    }

    private float GetAttackStyleBonus(CharacterAttackStyle attackStyle)
    {
        switch (attackStyle)
        {
            case CharacterAttackStyle.Melee:
                return meleeDamageBonus;
            case CharacterAttackStyle.Ranged:
                return rangedDamageBonus;
            case CharacterAttackStyle.Summon:
                return summonDamageBonus;
            default:
                return 0f;
        }
    }

    private static bool HasMatchingWeakness(DamageContext context)
    {
        if (context.Attacker == null || context.TargetBoss == null)
        {
            return false;
        }

        return IsMatchingWeaknessDamageType(context.Attacker.DamageType, context.TargetBoss.WeaknessDamageType)
            || IsMatchingWeaknessAttackStyle(context.Attacker.AttackStyle, context.TargetBoss.WeaknessAttackStyle);
    }

    private static bool IsMatchingWeaknessDamageType(CharacterDamageType damageType, MonsterWeaknessDamageType weaknessDamageType)
    {
        switch (weaknessDamageType)
        {
            case MonsterWeaknessDamageType.Physical:
                return damageType == CharacterDamageType.Physical;
            case MonsterWeaknessDamageType.Magic:
                return damageType == CharacterDamageType.Magic;
            case MonsterWeaknessDamageType.Explosion:
                return damageType == CharacterDamageType.Explosion;
            default:
                return false;
        }
    }

    private static bool IsMatchingWeaknessAttackStyle(CharacterAttackStyle attackStyle, MonsterWeaknessAttackStyle weaknessAttackStyle)
    {
        switch (weaknessAttackStyle)
        {
            case MonsterWeaknessAttackStyle.Ranged:
                return attackStyle == CharacterAttackStyle.Ranged;
            case MonsterWeaknessAttackStyle.Melee:
                return attackStyle == CharacterAttackStyle.Melee;
            case MonsterWeaknessAttackStyle.Summon:
                return attackStyle == CharacterAttackStyle.Summon;
            default:
                return false;
        }
    }
}
