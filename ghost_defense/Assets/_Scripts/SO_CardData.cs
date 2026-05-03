using UnityEngine;

public enum CardRarity
{
    C,
    B,
    A,
    S
}

public enum CardEffectType
{
    None,
    PhysicalDamageBonus,
    MagicDamageBonus,
    ExplosionDamageBonus,
    MeleeDamageBonus,
    RangedDamageBonus,
    SummonDamageBonus,
    GlobalDamageBonus,
    CriticalChanceBonus,
    Grade10DamageBonus,
    NextAttackDamageMultiplier,
    FieldClear,
    ExcludeLowGradeSpawn,
    AttackQueueSpeedBonus,
    BonusDiamondReward,
    BonusGoldReward
}

[CreateAssetMenu(fileName = "SO_CardData", menuName = "Ghost Defense/Card Data")]
public class SO_CardData : ScriptableObject
{
    [Tooltip("카드 고유 ID입니다.")]
    [SerializeField] private string cardId;

    [Tooltip("카드 이름입니다.")]
    [SerializeField] private string cardName;

    [Tooltip("카드 설명입니다.")]
    [TextArea(2, 5)]
    [SerializeField] private string description;

    [Tooltip("카드 효과 종류입니다. (예: 물리 데미지 카드는 PhysicalDamageBonus, 기습 카드는 NextAttackDamageMultiplier, 파워샷 카드는 AttackQueueSpeedBonus)")]
    [SerializeField] private CardEffectType effectType = CardEffectType.None;

    [Tooltip("카드 등급입니다.")]
    [SerializeField] private CardRarity rarity = CardRarity.C;

    [Tooltip("카드 수치입니다. 퍼센트 카드는 0.2 = 20% 형식으로 입력합니다. (예: 물리/마법/폭발/근접/원거리/소환/전체 데미지 +10%는 0.1, 크리티컬 확률 +5%는 0.05, 10단계 데미지 +50%는 0.5, 기습 10배는 10, 깔끔한 성격 2단계 이하는 2, 건망증 1단계 제외는 1, 파워샷 발사 속도 +20%는 0.2, 다이아 +3은 3)")]
    [SerializeField] private float effectValue;

    [Tooltip("한 번 선택하면 같은 전투 중 다시 등장하지 않는 카드인지 여부입니다. (예: 깔끔한 성격, 건망증 1회, 파워샷 1회, 기습은 체크)")]
    [SerializeField] private bool isOneTimeCard;

    public string CardId => string.IsNullOrWhiteSpace(cardId) ? name : cardId;
    public string CardName => string.IsNullOrWhiteSpace(cardName) ? name : cardName;
    public string Description => description;
    public CardEffectType EffectType => effectType;
    public CardRarity Rarity => rarity;
    public float EffectValue => effectValue;
    public bool IsOneTimeCard => isOneTimeCard;
}
