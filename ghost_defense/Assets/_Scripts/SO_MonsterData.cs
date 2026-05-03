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

    [Tooltip("몬스터의 공격 스타일 약점입니다.")]
    [SerializeField] private MonsterWeaknessAttackStyle weaknessAttackStyle = MonsterWeaknessAttackStyle.None;

    [Tooltip("이 스테이지를 최초 클리어했을 때 지급할 골드 보상입니다.")]
    [SerializeField] private int firstClearGoldReward;

    [Tooltip("이 스테이지를 재클리어했을 때 지급할 골드 보상입니다.")]
    [SerializeField] private int repeatClearGoldReward;

    [Tooltip("이 스테이지를 최초 클리어했을 때 지급할 다이아 보상입니다.")]
    [SerializeField] private int firstClearDiamondReward;

    [Tooltip("이 스테이지를 재클리어했을 때 지급할 다이아 보상입니다.")]
    [SerializeField] private int repeatClearDiamondReward;

    public string MonsterName => monsterName;
    public float MaxHp => Mathf.Max(0f, maxHp);
    public MonsterWeaknessDamageType WeaknessDamageType => weaknessDamageType;
    public MonsterWeaknessAttackStyle WeaknessAttackStyle => weaknessAttackStyle;
    public int FirstClearGoldReward => Mathf.Max(0, firstClearGoldReward);
    public int RepeatClearGoldReward => Mathf.Max(0, repeatClearGoldReward);
    public int FirstClearDiamondReward => Mathf.Max(0, firstClearDiamondReward);
    public int RepeatClearDiamondReward => Mathf.Max(0, repeatClearDiamondReward);
}
