using UnityEngine;

public enum MonsterWeaknessDamageType
{
    None,
    Physical,
    Magic,
    Explosion
}

public enum MonsterWeaknessAttackStyle
{
    None,
    Ranged,
    Melee,
    Summon
}

[CreateAssetMenu(fileName = "SO_MonsterData", menuName = "Ghost Defense/Monster Data")]
public class SO_MonsterData : ScriptableObject
{
    [Tooltip("몬스터 표시 이름입니다.")]
    [SerializeField] private string monsterName;

    [Tooltip("몬스터의 최대 체력입니다.")]
    [SerializeField] private float maxHp = 10f;

    [Tooltip("몬스터의 데미지 타입 약점입니다.")]
    [SerializeField] private MonsterWeaknessDamageType weaknessDamageType = MonsterWeaknessDamageType.None;

    [Tooltip("몬스터의 공격 방식 약점입니다.")]
    [SerializeField] private MonsterWeaknessAttackStyle weaknessAttackStyle = MonsterWeaknessAttackStyle.None;

    public string MonsterName => monsterName;
    public float MaxHp => Mathf.Max(0f, maxHp);
    public MonsterWeaknessDamageType WeaknessDamageType => weaknessDamageType;
    public MonsterWeaknessAttackStyle WeaknessAttackStyle => weaknessAttackStyle;
}
