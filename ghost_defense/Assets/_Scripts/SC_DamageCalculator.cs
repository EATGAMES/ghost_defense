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
        public readonly float ComboDamageMultiplier;

        public DamageContext(SO_CharacterData attacker, SC_MonsterHealth targetBoss, int mergeGrade, float comboDamageMultiplier)
        {
            Attacker = attacker;
            TargetBoss = targetBoss;
            MergeGrade = mergeGrade;
            ComboDamageMultiplier = Mathf.Max(1f, comboDamageMultiplier);
        }
    }

    [Tooltip("모든 캐릭터의 크리티컬 데미지 배수에 추가할 비율입니다. 0.5는 +50%입니다.")]
    [SerializeField] private float globalCriticalDamageMultiplierBonus;

    public DamageResult CalculateDamage(DamageContext context)
    {
        if (context.Attacker == null)
        {
            return new DamageResult(0f, false);
        }

        int safeGrade = Mathf.Clamp(context.MergeGrade, 1, 10);
        float finalDamage = context.Attacker.GetBaseDamage(safeGrade);

        if (HasMatchingWeakness(context))
        {
            finalDamage *= WeaknessDamageBonusMultiplier;
        }

        finalDamage *= context.ComboDamageMultiplier;

        float criticalChance = Mathf.Clamp01(context.Attacker.GetCriticalChance());
        float criticalDamageMultiplier = Mathf.Max(1f, context.Attacker.GetCriticalDamageMultiplier() + globalCriticalDamageMultiplierBonus);
        bool isCritical = criticalChance > 0f && Random.value <= criticalChance;
        if (isCritical)
        {
            finalDamage *= criticalDamageMultiplier;
        }

        return new DamageResult(finalDamage, isCritical);
    }

    public void SetGlobalCriticalDamageMultiplierBonus(float bonus)
    {
        globalCriticalDamageMultiplierBonus = bonus;
    }

    public void ResetAllModifiers()
    {
        globalCriticalDamageMultiplierBonus = 0f;
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
