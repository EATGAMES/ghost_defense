using UnityEngine;

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
    BonusGoldReward,
    LowerGradeAdditionalAttack,
    CollisionErase,
    NextSpawnPreviewCount,
    ShrinkShot,
    RemoveBottomCharacters
}

[CreateAssetMenu(fileName = "SO_CardData", menuName = "Ghost Defense/Card Data")]
public class SO_CardData : ScriptableObject
{
    [Tooltip("카드 고유 ID입니다.")]
    [SerializeField] private string cardId;

    [Tooltip("카드 이름입니다.")]
    [SerializeField] private string cardName;

    [Tooltip("카드 설명입니다. 값이 변하는 위치에는 n 또는 N을 넣으면 현재 레벨 효과값으로 치환됩니다.")]
    [TextArea(2, 5)]
    [SerializeField] private string description;

    [Tooltip("카드 이미지로 사용할 PNG 스프라이트입니다.")]
    [SerializeField] private Sprite cardImage;

    [Tooltip("카드 효과 종류입니다. (예: 물리 데미지 카드는 PhysicalDamageBonus, 기습 카드는 NextAttackDamageMultiplier, 지우개는 CollisionErase)")]
    [SerializeField] private CardEffectType effectType = CardEffectType.None;

    [Tooltip("1레벨 카드 효과 수치입니다. 퍼센트 카드는 0.2 = 20% 형식으로 입력합니다.")]
    [SerializeField] private float baseEffectValue;

    [Tooltip("카드 레벨이 1 오를 때마다 추가되는 효과 수치입니다.")]
    [SerializeField] private float effectValuePerLevel;

    public string CardId => string.IsNullOrWhiteSpace(cardId) ? name : cardId;
    public string CardName => string.IsNullOrWhiteSpace(cardName) ? name : cardName;
    public string Description => description;
    public Sprite CardImage => cardImage;
    public CardEffectType EffectType => effectType;
    public float BaseEffectValue => baseEffectValue;
    public float EffectValuePerLevel => effectValuePerLevel;

    public float GetEffectValueForLevel(int level)
    {
        int safeLevel = Mathf.Max(1, level);
        return baseEffectValue + effectValuePerLevel * (safeLevel - 1);
    }

    public string GetResolvedDescriptionForLevel(int level)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        string resolvedValueText = GetDisplayValueText(GetEffectValueForLevel(level));
        return description.Replace("N", resolvedValueText).Replace("n", resolvedValueText);
    }

    private string GetDisplayValueText(float effectValue)
    {
        if (IsPercentDisplayEffect())
        {
            float percentValue = effectType == CardEffectType.NextAttackDamageMultiplier
                ? Mathf.Max(0f, effectValue - 1f) * 100f
                : effectValue * 100f;

            return $"{percentValue:0.##}%";
        }

        if (Mathf.Approximately(effectValue, Mathf.Round(effectValue)))
        {
            return Mathf.RoundToInt(effectValue).ToString();
        }

        return effectValue.ToString("0.##");
    }

    private bool IsPercentDisplayEffect()
    {
        switch (effectType)
        {
            case CardEffectType.PhysicalDamageBonus:
            case CardEffectType.MagicDamageBonus:
            case CardEffectType.ExplosionDamageBonus:
            case CardEffectType.MeleeDamageBonus:
            case CardEffectType.RangedDamageBonus:
            case CardEffectType.SummonDamageBonus:
            case CardEffectType.GlobalDamageBonus:
            case CardEffectType.CriticalChanceBonus:
            case CardEffectType.Grade10DamageBonus:
            case CardEffectType.NextAttackDamageMultiplier:
            case CardEffectType.BonusGoldReward:
                return true;
            default:
                return false;
        }
    }
}
