using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_CardManager : MonoBehaviour
{
    [Tooltip("전투 데미지 계산기에 카드 보너스를 반영합니다.")]
    [SerializeField] private SC_DamageCalculator damageCalculator;

    [Tooltip("카드 효과를 전투 매니저에 전달합니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    private readonly List<SO_CardData> ownedCards = new List<SO_CardData>();
    private readonly Dictionary<SO_CardData, int> selectedCardLevels = new Dictionary<SO_CardData, int>();
    private readonly Dictionary<CardEffectType, float> additiveEffectTotals = new Dictionary<CardEffectType, float>();

    private float bonusDiamondReward;
    private float bonusGoldReward;
    private int excludeLowGradeSpawnRemainingShots;
    private int fieldClearMaxGrade;
    private int attackQueueSpeedRemainingShots;
    private int lowerGradeAdditionalAttackRemainingShots;
    private int collisionErasePendingCount;
    private int nextSpawnPreviewCount;
    private int shrinkShotRemainingShots;
    private int removeBottomCharacterCount;

    public IReadOnlyList<SO_CardData> OwnedCards => ownedCards;
    public float BonusDiamondReward => bonusDiamondReward;
    public float BonusGoldReward => bonusGoldReward;
    public int ExcludeLowGradeSpawnRemainingShots => excludeLowGradeSpawnRemainingShots;
    public int FieldClearMaxGrade => fieldClearMaxGrade;
    public int AttackQueueSpeedRemainingShots => attackQueueSpeedRemainingShots;
    public int LowerGradeAdditionalAttackRemainingShots => lowerGradeAdditionalAttackRemainingShots;
    public int CollisionErasePendingCount => collisionErasePendingCount;
    public int NextSpawnPreviewCount => nextSpawnPreviewCount;
    public int ShrinkShotRemainingShots => shrinkShotRemainingShots;
    public int RemoveBottomCharacterCount => removeBottomCharacterCount;

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

        int currentLevel = Mathf.Max(1, GetCardLevel(cardData));

        if (!ownedCards.Contains(cardData))
        {
            ownedCards.Add(cardData);
        }

        selectedCardLevels[cardData] = currentLevel;
        RebuildRuntimeEffects();
        float effectValue = cardData.GetEffectValueForLevel(currentLevel);

        switch (cardData.EffectType)
        {
            case CardEffectType.NextAttackDamageMultiplier:
                if (battleManager != null)
                {
                    battleManager.ArmCardNextAttackDamageMultiplier(effectValue);
                }
                break;
            case CardEffectType.FieldClear:
                ClearFieldCharactersUpToGrade(Mathf.RoundToInt(effectValue));
                break;
            case CardEffectType.RemoveBottomCharacters:
                RemoveBottomFieldCharacters(Mathf.RoundToInt(effectValue));
                break;
            case CardEffectType.LowerGradeAdditionalAttack:
                lowerGradeAdditionalAttackRemainingShots += Mathf.Max(0, Mathf.RoundToInt(effectValue));
                break;
            case CardEffectType.CollisionErase:
                collisionErasePendingCount += Mathf.Max(0, Mathf.RoundToInt(effectValue));
                break;
            case CardEffectType.ShrinkShot:
                shrinkShotRemainingShots += Mathf.Max(0, Mathf.RoundToInt(effectValue));
                ApplyShrinkShotToWaitingCharacters(true);
                break;
            case CardEffectType.AttackQueueSpeedBonus:
                attackQueueSpeedRemainingShots += Mathf.Max(0, Mathf.RoundToInt(effectValue));
                break;
        }
    }

    public void ResetRuntimeState()
    {
        ownedCards.Clear();
        selectedCardLevels.Clear();
        additiveEffectTotals.Clear();
        bonusDiamondReward = 0f;
        bonusGoldReward = 0f;
        excludeLowGradeSpawnRemainingShots = 0;
        fieldClearMaxGrade = 0;
        attackQueueSpeedRemainingShots = 0;
        lowerGradeAdditionalAttackRemainingShots = 0;
        collisionErasePendingCount = 0;
        nextSpawnPreviewCount = 0;
        shrinkShotRemainingShots = 0;
        removeBottomCharacterCount = 0;
        ApplyAllEffects();
    }

    public bool CanOfferCard(SO_CardData cardData)
    {
        return cardData != null;
    }

    public int GetCardLevel(SO_CardData cardData)
    {
        if (cardData == null)
        {
            return 0;
        }

        if (SC_SaveDataManager.Instance != null)
        {
            return SC_SaveDataManager.Instance.GetCardLevel(cardData.CardId);
        }

        if (selectedCardLevels.TryGetValue(cardData, out int runtimeLevel))
        {
            return runtimeLevel;
        }

        return 0;
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

    }

    private void RebuildRuntimeEffects()
    {
        additiveEffectTotals.Clear();
        bonusDiamondReward = 0f;
        bonusGoldReward = 0f;
        excludeLowGradeSpawnRemainingShots = 0;
        fieldClearMaxGrade = 0;
        attackQueueSpeedRemainingShots = 0;
        lowerGradeAdditionalAttackRemainingShots = 0;
        collisionErasePendingCount = 0;
        nextSpawnPreviewCount = 0;
        shrinkShotRemainingShots = 0;
        removeBottomCharacterCount = 0;

        foreach (KeyValuePair<SO_CardData, int> pair in selectedCardLevels)
        {
            SO_CardData cardData = pair.Key;
            if (cardData == null)
            {
                continue;
            }

            float effectValue = cardData.GetEffectValueForLevel(pair.Value);
            switch (cardData.EffectType)
            {
                case CardEffectType.FieldClear:
                    fieldClearMaxGrade = Mathf.Max(fieldClearMaxGrade, Mathf.RoundToInt(effectValue));
                    break;
                case CardEffectType.ExcludeLowGradeSpawn:
                    excludeLowGradeSpawnRemainingShots = Mathf.Max(excludeLowGradeSpawnRemainingShots, Mathf.RoundToInt(effectValue));
                    break;
                case CardEffectType.BonusDiamondReward:
                    bonusDiamondReward += effectValue;
                    break;
                case CardEffectType.BonusGoldReward:
                    bonusGoldReward += effectValue;
                    break;
                case CardEffectType.LowerGradeAdditionalAttack:
                    lowerGradeAdditionalAttackRemainingShots = Mathf.Max(lowerGradeAdditionalAttackRemainingShots, Mathf.RoundToInt(effectValue));
                    break;
                case CardEffectType.NextSpawnPreviewCount:
                    nextSpawnPreviewCount = Mathf.Max(nextSpawnPreviewCount, Mathf.RoundToInt(effectValue));
                    break;
                case CardEffectType.ShrinkShot:
                    break;
                case CardEffectType.AttackQueueSpeedBonus:
                    attackQueueSpeedRemainingShots = Mathf.Max(attackQueueSpeedRemainingShots, Mathf.RoundToInt(effectValue));
                    break;
                case CardEffectType.NextAttackDamageMultiplier:
                    break;
                default:
                    AddAdditiveEffect(cardData.EffectType, effectValue);
                    break;
            }
        }

        ApplyAllEffects();
    }

    private float GetEffectTotal(CardEffectType effectType)
    {
        if (additiveEffectTotals.TryGetValue(effectType, out float totalValue))
        {
            return totalValue;
        }

        return 0f;
    }

    public bool IsExcludeLowGradeSpawnActive()
    {
        return excludeLowGradeSpawnRemainingShots > 0;
    }

    public void ConsumeExcludeLowGradeSpawnShot()
    {
        if (excludeLowGradeSpawnRemainingShots <= 0)
        {
            return;
        }

        excludeLowGradeSpawnRemainingShots--;
    }

    public bool IsLowerGradeAdditionalAttackActive()
    {
        return lowerGradeAdditionalAttackRemainingShots > 0;
    }

    public void ConsumeLowerGradeAdditionalAttackShot()
    {
        if (lowerGradeAdditionalAttackRemainingShots <= 0)
        {
            return;
        }

        lowerGradeAdditionalAttackRemainingShots--;
    }

    public int ConsumeCollisionEraseCount()
    {
        int pendingCount = Mathf.Max(0, collisionErasePendingCount);
        collisionErasePendingCount = 0;
        return pendingCount;
    }

    public bool IsShrinkShotActive()
    {
        return shrinkShotRemainingShots > 0;
    }

    public void ConsumeShrinkShot()
    {
        if (shrinkShotRemainingShots <= 0)
        {
            return;
        }

        shrinkShotRemainingShots--;
        ApplyShrinkShotToWaitingCharacters(shrinkShotRemainingShots > 0);
    }

    public bool IsAttackQueueSpeedBonusActive()
    {
        return attackQueueSpeedRemainingShots > 0;
    }

    public void ConsumeAttackQueueSpeedBonusShot()
    {
        if (attackQueueSpeedRemainingShots <= 0)
        {
            return;
        }

        attackQueueSpeedRemainingShots--;
    }

    private static void ApplyShrinkShotToWaitingCharacters(bool isShrinkShotActive)
    {
        SC_PlayerDragAndShoot[] shooters = FindObjectsByType<SC_PlayerDragAndShoot>();
        for (int i = 0; i < shooters.Length; i++)
        {
            SC_PlayerDragAndShoot shooter = shooters[i];
            if (shooter == null || shooter.IsShot)
            {
                continue;
            }

            shooter.SetShrinkShotVisual(isShrinkShotActive);
        }
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

    private static void RemoveBottomFieldCharacters(int removeCount)
    {
        int safeRemoveCount = Mathf.Max(0, removeCount);
        if (safeRemoveCount <= 0)
        {
            return;
        }

        SC_CharacterPresenter[] presenters = FindObjectsByType<SC_CharacterPresenter>();
        List<SC_CharacterPresenter> removablePresenters = new List<SC_CharacterPresenter>();

        for (int i = 0; i < presenters.Length; i++)
        {
            SC_CharacterPresenter presenter = presenters[i];
            if (presenter == null)
            {
                continue;
            }

            SC_PlayerDragAndShoot shooter = presenter.GetComponent<SC_PlayerDragAndShoot>();
            if (shooter != null && !shooter.IsShot)
            {
                continue;
            }

            removablePresenters.Add(presenter);
        }

        removablePresenters.Sort((left, right) => left.transform.position.y.CompareTo(right.transform.position.y));

        int targetCount = Mathf.Min(safeRemoveCount, removablePresenters.Count);
        for (int i = 0; i < targetCount; i++)
        {
            SC_CharacterPresenter presenter = removablePresenters[i];
            if (presenter == null)
            {
                continue;
            }

            Object.Destroy(presenter.gameObject);
        }
    }
}
