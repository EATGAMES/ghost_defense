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
    [Tooltip("CSV와 노드 데이터에서 참조할 몬스터 ID입니다.")]
    [SerializeField] private string monsterId;

    [Tooltip("몬스터 표시 이름입니다.")]
    [SerializeField] private string monsterName;

    [Tooltip("몬스터의 기본 최대 체력입니다. 실제 전투 체력은 노드 배율을 곱해 계산합니다.")]
    [SerializeField] private float maxHp = 10f;

    [Tooltip("몬스터의 약점 데미지 타입입니다.")]
    [SerializeField] private MonsterWeaknessDamageType weaknessDamageType = MonsterWeaknessDamageType.None;

    [Tooltip("몬스터의 약점 공격 스타일입니다.")]
    [SerializeField] private MonsterWeaknessAttackStyle weaknessAttackStyle = MonsterWeaknessAttackStyle.None;

    [Tooltip("스테이지를 최초 클리어했을 때 지급할 골드 보상입니다.")]
    [SerializeField] private int firstClearGoldReward;

    [Tooltip("스테이지를 재클리어했을 때 지급할 골드 보상입니다.")]
    [SerializeField] private int repeatClearGoldReward;

    [Tooltip("스테이지를 최초 클리어했을 때 지급할 다이아 보상입니다.")]
    [SerializeField] private int firstClearDiamondReward;

    [Tooltip("스테이지를 재클리어했을 때 지급할 다이아 보상입니다.")]
    [SerializeField] private int repeatClearDiamondReward;

    [Tooltip("스테이지를 표시할 맵 이미지입니다.")]
    [SerializeField] private Sprite stageMapSprite;

    public string MonsterId => string.IsNullOrWhiteSpace(monsterId) ? name : monsterId.Trim();
    public string MonsterName => monsterName;
    public float BaseHp => Mathf.Max(0f, maxHp);
    public float MaxHp => Mathf.Max(0f, maxHp);
    public MonsterWeaknessDamageType WeaknessDamageType => weaknessDamageType;
    public MonsterWeaknessAttackStyle WeaknessAttackStyle => weaknessAttackStyle;
    public int FirstClearGoldReward => Mathf.Max(0, firstClearGoldReward);
    public int RepeatClearGoldReward => Mathf.Max(0, repeatClearGoldReward);
    public int FirstClearDiamondReward => Mathf.Max(0, firstClearDiamondReward);
    public int RepeatClearDiamondReward => Mathf.Max(0, repeatClearDiamondReward);
    public Sprite StageMapSprite => stageMapSprite;
}
