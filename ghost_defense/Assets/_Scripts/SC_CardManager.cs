using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_CardManager : MonoBehaviour
{
    [Tooltip("전투 데미지 계산기에 카드 보너스를 반영합니다.")]
    [SerializeField] private SC_DamageCalculator damageCalculator;

    [Tooltip("1회성 카드 효과를 전투 매니저에 전달합니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    private readonly List<SO_CardData> ownedCards = new List<SO_CardData>();
    private readonly Dictionary<CardEffectType, float> additiveEffectTotals = new Dictionary<CardEffectType, float>();

    private float bonusDiamondReward;
    private float bonusGoldReward;
    private int excludeLowGradeSpawnMaxGrade;
    private int fieldClearMaxGrade;
    private float attackQueueSpeedBonus;

    public IReadOnlyList<SO_CardData> OwnedCards => ownedCards;
    public float BonusDiamondReward => bonusDiamondReward;
    public float BonusGoldReward => bonusGoldReward;
    public int ExcludeLowGradeSpawnMaxGrade => excludeLowGradeSpawnMaxGrade;
    public int FieldClearMaxGrade => fieldClearMaxGrade;
    public float AttackQueueSpeedBonus => attackQueueSpeedBonus;

    private void Awake()
    {
        if (damageCalculator == null)
        {
            damageCalculator = FindAnyObjectByType<SC_DamageCalculator>();
        }

        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        ApplyAllEffects();
    }

    public void ApplySelectedCard(SO_CardData cardData)
    {
        if (cardData == null)
        {
            return;
        }

        ownedCards.Add(cardData);

        switch (cardData.EffectType)
        {
            case CardEffectType.NextAttackDamageMultiplier:
                if (battleManager != null)
                {
                    battleManager.ArmCardNextAttackDamageMultiplier(cardData.EffectValue);
                }
                break;
            case CardEffectType.FieldClear:
                fieldClearMaxGrade = Mathf.Max(fieldClearMaxGrade, Mathf.RoundToInt(cardData.EffectValue));
                ClearFieldCharactersUpToGrade(fieldClearMaxGrade);
                break;
            case CardEffectType.ExcludeLowGradeSpawn:
                excludeLowGradeSpawnMaxGrade = Mathf.Max(excludeLowGradeSpawnMaxGrade, Mathf.RoundToInt(cardData.EffectValue));
                break;
            case CardEffectType.BonusDiamondReward:
                bonusDiamondReward += cardData.EffectValue;
                break;
            case CardEffectType.BonusGoldReward:
                bonusGoldReward += cardData.EffectValue;
                break;
            default:
                AddAdditiveEffect(cardData.EffectType, cardData.EffectValue);
                break;
        }

        ApplyAllEffects();
    }

    public void ResetRuntimeState()
    {
        ownedCards.Clear();
        additiveEffectTotals.Clear();
        bonusDiamondReward = 0f;
        bonusGoldReward = 0f;
        excludeLowGradeSpawnMaxGrade = 0;
        fieldClearMaxGrade = 0;
        attackQueueSpeedBonus = 0f;
        ApplyAllEffects();
    }

    public bool CanOfferCard(SO_CardData cardData)
    {
        if (cardData == null)
        {
            return false;
        }

        if (!cardData.IsOneTimeCard)
        {
            return true;
        }

        return !ownedCards.Contains(cardData);
    }

    private void AddAdditiveEffect(CardEffectType effectType, float value)
    {
        if (effectType == CardEffectType.None)
        {
            return;
        }

        if (additiveEffectTotals.TryGetValue(effectType, out float currentValue))
        {
            additiveEffectTotals[effectType] = currentValue + value;
            return;
        }

        additiveEffectTotals.Add(effectType, value);
    }

    private void ApplyAllEffects()
    {
        if (damageCalculator == null)
        {
            return;
        }

        damageCalculator.ResetAllModifiers();
        damageCalculator.SetDamageTypeBonus(CharacterDamageType.Physical, GetEffectTotal(CardEffectType.PhysicalDamageBonus));
        damageCalculator.SetDamageTypeBonus(CharacterDamageType.Magic, GetEffectTotal(CardEffectType.MagicDamageBonus));
        damageCalculator.SetDamageTypeBonus(CharacterDamageType.Explosion, GetEffectTotal(CardEffectType.ExplosionDamageBonus));
        damageCalculator.SetAttackStyleBonus(CharacterAttackStyle.Melee, GetEffectTotal(CardEffectType.MeleeDamageBonus));
        damageCalculator.SetAttackStyleBonus(CharacterAttackStyle.Ranged, GetEffectTotal(CardEffectType.RangedDamageBonus));
        damageCalculator.SetAttackStyleBonus(CharacterAttackStyle.Summon, GetEffectTotal(CardEffectType.SummonDamageBonus));
        damageCalculator.SetGlobalDamageBonus(GetEffectTotal(CardEffectType.GlobalDamageBonus));
        damageCalculator.SetGlobalCriticalChanceBonus(GetEffectTotal(CardEffectType.CriticalChanceBonus));
        damageCalculator.SetGrade10DamageBonus(GetEffectTotal(CardEffectType.Grade10DamageBonus));

        attackQueueSpeedBonus = GetEffectTotal(CardEffectType.AttackQueueSpeedBonus);
        ApplyShootSpeedBonusToActiveCharacters(attackQueueSpeedBonus);
    }

    private float GetEffectTotal(CardEffectType effectType)
    {
        if (additiveEffectTotals.TryGetValue(effectType, out float totalValue))
        {
            return totalValue;
        }

        return 0f;
    }

    private static void ClearFieldCharactersUpToGrade(int maxGrade)
    {
        int safeMaxGrade = Mathf.Clamp(maxGrade, 1, 10);
        SC_CharacterPresenter[] presenters = FindObjectsByType<SC_CharacterPresenter>();
        for (int i = 0; i < presenters.Length; i++)
        {
            SC_CharacterPresenter presenter = presenters[i];
            if (presenter == null || presenter.MergeGrade > safeMaxGrade)
            {
                continue;
            }

            SC_PlayerDragAndShoot dragAndShoot = presenter.GetComponent<SC_PlayerDragAndShoot>();
            if (dragAndShoot != null && !dragAndShoot.IsShot)
            {
                dragAndShoot.CancelDragAndResetToStartPosition();
            }

            Object.Destroy(presenter.gameObject);
        }
    }

    private static void ApplyShootSpeedBonusToActiveCharacters(float speedBonus)
    {
        SC_PlayerDragAndShoot[] shooters = FindObjectsByType<SC_PlayerDragAndShoot>();
        for (int i = 0; i < shooters.Length; i++)
        {
            SC_PlayerDragAndShoot shooter = shooters[i];
            if (shooter == null)
            {
                continue;
            }

            shooter.SetCardShootSpeedBonus(speedBonus);
        }
    }
}
