using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_BattleManager : MonoBehaviour
{
    public readonly struct ClearRewardResult
    {
        public readonly int BaseGold;
        public readonly int BonusGold;
        public readonly int BaseDiamond;
        public readonly int BonusDiamond;
        public readonly bool ShowCloseCenterOnly;

        public ClearRewardResult(int baseGold, int bonusGold, int baseDiamond, int bonusDiamond, bool showCloseCenterOnly)
        {
            BaseGold = Mathf.Max(0, baseGold);
            BonusGold = Mathf.Max(0, bonusGold);
            BaseDiamond = Mathf.Max(0, baseDiamond);
            BonusDiamond = Mathf.Max(0, bonusDiamond);
            ShowCloseCenterOnly = showCloseCenterOnly;
        }
    }

    private readonly struct AttackRequest
    {
        public readonly int Grade;
        public readonly SO_CharacterData CharacterData;
        public readonly bool ApplyFirstMergedAttackBonus;

        public AttackRequest(int grade, SO_CharacterData characterData, bool applyFirstMergedAttackBonus)
        {
            Grade = grade;
            CharacterData = characterData;
            ApplyFirstMergedAttackBonus = applyFirstMergedAttackBonus;
        }
    }

    private const int FinalMergeClearBonusDiamondReward = 50;

    public static int CurrentStage { get; private set; } = 1;

    public event Action<int, int> StageChanged;
    public event Action<float, float> BossHealthChanged;
    public event Action<int, int> MergeAttackGaugeChanged;
    public event Action<SO_CharacterData, bool> CurrentAttackCharacterChanged;
    public event Action<int> StageCleared;
    public event Action<int> StageFailed;

    [Tooltip("理쒕? ?ㅽ뀒?댁? ?섏엯?덈떎.")]
    [SerializeField] private int maxStage = 10;

    [Tooltip("?꾪닾 ?쒖옉 ???곸슜???쒖옉 ?ㅽ뀒?댁? 踰덊샇?낅땲??")]
    [SerializeField] private int startStage = 1;

    [Tooltip("?곷떒 怨듦꺽 罹먮┃?곗쓽 ?곕?吏 怨꾩궛???ъ슜??怨듦꺽 罹먮┃???곗씠??紐⑸줉?낅땲??")]
    [SerializeField] private SO_CharacterData[] equippedRoster = new SO_CharacterData[5];

    [Tooltip("?섎떒 ?꾨뱶 罹먮┃???ㅽ봽?쇱씠?몄뿉 ?ъ슜???꾨뱶 ?ㅽ궓 ?곗씠??紐⑸줉?낅땲??")]
    [SerializeField] private SO_FieldCharacterSkinData[] equippedFieldSkins = new SO_FieldCharacterSkinData[5];

    [Tooltip("移대뱶 ?좏깮 ?앹뾽???대━湲곌퉴吏 ?꾩슂??怨듦꺽 ?잛닔?낅땲??")]
    [SerializeField] private int attackCountPerCard = 20;

    [Tooltip("怨듦꺽 ?붿껌 泥섎━ ?ъ씠 湲곕낯 媛꾧꺽(珥??낅땲??")]
    [SerializeField] private float baseAttackInterval = 0.2f;

    [Tooltip("移대뱶 ?좏깮 以??꾪닾瑜??쇱떆 ?뺤??좎? ?щ??낅땲??")]
    [SerializeField] private bool pauseWhenSelectingCard = true;

    [Tooltip("?쇱젙 怨듦꺽 ?잛닔留덈떎 ?대┫ 移대뱶 ?좏깮 ?앹뾽?낅땲??")]
    [SerializeField] private SC_BattleCardPopup battleCardPopup;

    [Tooltip("?곷떒 怨듦꺽 罹먮┃?곗쓽 ?곗텧 ?쒓컙??李몄“??酉곗엯?덈떎.")]
    [SerializeField] private SC_CurrentAttackCharacterView currentAttackCharacterView;

    [Tooltip("理쒖쥌 ?꾪닾 ?곕?吏 怨듭떇??怨꾩궛??怨꾩궛湲곗엯?덈떎.")]
    [SerializeField] private SC_DamageCalculator damageCalculator;

    [Tooltip("?꾪닾 以?移대뱶 ?④낵瑜?愿由ы븷 移대뱶 留ㅻ땲??낅땲??")]
    [SerializeField] private SC_CardManager cardManager;

    [Tooltip("10?④퀎 理쒖쥌 ?⑹꽦 ?곗텧 ?앹뾽?낅땲??")]
    [SerializeField] private SC_FinalMergePopup finalMergePopup;

    [Tooltip("?ㅽ뀒?댁? ?대━??蹂댁긽怨?踰꾪듉???쒖떆?섎뒗 ?대━???앹뾽?낅땲??")]
    [SerializeField] private SC_ClearPopup clearPopup;

    private readonly Queue<AttackRequest> pendingAttackRequests = new Queue<AttackRequest>();

    private SC_MonsterHealth currentBoss;
    private Coroutine attackQueueCoroutine;
    private SO_CharacterData currentAttackCharacterData;
    private SO_CharacterData[] defaultEquippedRoster;
    private SO_FieldCharacterSkinData[] defaultEquippedFieldSkins;
    private SO_MonsterData clearedMonsterData;
    private int currentAttackGrade;
    private int currentAttackCount;
    private int openedCardSelectionCount;
    private bool isCardSelectionOpen;
    private bool isBattleFinished;
    private bool isBattleClosing;
    private bool isStageClearPending;
    private bool isBattleClearedThisSession;
    private bool isNextMergedAttackBonusArmed;
    private bool wasStageClearedOnBattleStart;
    private bool hasGrantedBaseClearRewardThisBattle;
    private bool hasCreatedGrade10ThisBattle;
    private bool hasGrantedGrade10RewardThisBattle;
    private float nextAttackDamageMultiplier = 1f;
    private float cardAttackQueueSpeedBonus;
    private int battleMergeCount;
    private float battleDamageDealt;
    private bool hasPersistedBattleStatistics;
    private SC_MonsterHealth pendingDefeatedBoss;

    public int MaxStage => Mathf.Max(1, maxStage);
    public int CurrentMergeAttackCount => currentAttackCount;
    public int MergeAttackCountPerCard => Mathf.Max(1, attackCountPerCard);
    public int BattleMergeCount => Mathf.Max(0, battleMergeCount);
    public float BattleDamageDealt => Mathf.Max(0f, battleDamageDealt);
    public bool IsCardSelectionOpen => isCardSelectionOpen;
    public bool IsBattleFinished => isBattleFinished;
    public bool IsBattleClearedThisSession => isBattleClearedThisSession;
    public int PendingAttackQueueCount => pendingAttackRequests.Count;
    public SO_CharacterData CurrentAttackCharacterData => currentAttackCharacterData;
    public int CurrentAttackGrade => Mathf.Clamp(currentAttackGrade, 0, 10);
    public bool HasAliveBoss => currentBoss != null && currentBoss.CurrentHp > 0f && !isBattleClosing && !isBattleFinished;

    private void Awake()
    {
        defaultEquippedRoster = CloneRoster(equippedRoster);
        defaultEquippedFieldSkins = CloneFieldSkins(equippedFieldSkins);
        ApplySavedRosterOrder();

        if (battleCardPopup == null)
        {
            battleCardPopup = FindAnyObjectByType<SC_BattleCardPopup>();
        }

        if (currentAttackCharacterView == null)
        {
            currentAttackCharacterView = FindAnyObjectByType<SC_CurrentAttackCharacterView>();
        }

        if (damageCalculator == null)
        {
            damageCalculator = GetComponent<SC_DamageCalculator>();
        }

        if (cardManager == null)
        {
            cardManager = FindAnyObjectByType<SC_CardManager>();
        }

        if (finalMergePopup == null)
        {
            finalMergePopup = FindFinalMergePopupIncludingInactive();
        }

        if (clearPopup == null)
        {
            clearPopup = FindClearPopupIncludingInactive();
        }
    }

    private void Start()
    {
        isBattleClearedThisSession = false;
        int savedSelectedStage = SC_SaveDataManager.Instance != null ? SC_SaveDataManager.Instance.SelectedStage : startStage;
        CurrentStage = Mathf.Clamp(savedSelectedStage, 1, MaxStage);
        wasStageClearedOnBattleStart =
            SC_SaveDataManager.Instance != null && SC_SaveDataManager.Instance.IsStageCleared(CurrentStage);
        currentAttackCharacterData = GetStartingAttackCharacterData();
        currentAttackGrade = currentAttackCharacterData != null ? 1 : 0;

        RefreshGradePreviewUI();
        RaiseStageChanged();
        RaiseBossHealthChanged();
        RaiseMergeAttackGaugeChanged();
        RaiseCurrentAttackCharacterChanged(false);
    }

    private void OnDisable()
    {
        if (pauseWhenSelectingCard && Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }

    private void OnDestroy()
    {
        PersistBattleStatisticsIfNeeded();
    }

    public void RegisterBoss(SC_MonsterHealth boss)
    {
        if (currentBoss == boss)
        {
            RaiseBossHealthChanged();
            return;
        }

        if (currentBoss != null)
        {
            currentBoss.HealthChanged -= OnBossHealthChanged;
        }

        currentBoss = boss;
        if (currentBoss != null)
        {
            currentBoss.HealthChanged += OnBossHealthChanged;
            clearedMonsterData = currentBoss.MonsterData;
        }

        RaiseBossHealthChanged();
    }

    public void UnregisterBoss(SC_MonsterHealth boss)
    {
        if (currentBoss != boss)
        {
            return;
        }

        if (currentBoss != null)
        {
            currentBoss.HealthChanged -= OnBossHealthChanged;
        }

        currentBoss = null;
        RaiseBossHealthChanged();
    }

    public void NotifyMergeAttack(int mergedGrade)
    {
        if (isBattleFinished || isBattleClosing || isCardSelectionOpen)
        {
            return;
        }

        battleMergeCount++;

        SO_CharacterData targetCharacterData = GetCharacterDataForGrade(mergedGrade);
        bool applyFirstMergedAttackBonus = isNextMergedAttackBonusArmed;
        isNextMergedAttackBonusArmed = false;
        pendingAttackRequests.Enqueue(new AttackRequest(Mathf.Clamp(mergedGrade, 1, 10), targetCharacterData, applyFirstMergedAttackBonus));
        TryStartAttackQueueProcessing();
        RaiseMergeAttackGaugeChanged();
    }

    public void NotifyFinalMergeAttack(int mergedGrade)
    {
        if (isBattleClearedThisSession)
        {
            OpenClearPopup();
            return;
        }

        NotifyMergeAttack(mergedGrade);
    }

    public void NotifyCreatedGrade10ThisBattle()
    {
        hasCreatedGrade10ThisBattle = true;
    }

    public void ArmNextMergedAttackDamageBonus()
    {
        ArmCardNextAttackDamageMultiplier(10f);
    }

    public void ArmCardNextAttackDamageMultiplier(float damageMultiplier)
    {
        isNextMergedAttackBonusArmed = true;
        nextAttackDamageMultiplier = Mathf.Max(1f, damageMultiplier);
    }

    public void SetCardAttackQueueSpeedBonus(float speedBonus)
    {
        cardAttackQueueSpeedBonus = Mathf.Max(0f, speedBonus);
    }

    public void NotifyBossDefeated(SC_MonsterHealth defeatedBoss)
    {
        if (isBattleFinished || isBattleClosing)
        {
            return;
        }

        if (currentBoss != null && defeatedBoss != null && currentBoss != defeatedBoss)
        {
            return;
        }

        isBattleClosing = true;
        isCardSelectionOpen = false;
        pendingDefeatedBoss = defeatedBoss;
        clearedMonsterData = defeatedBoss != null ? defeatedBoss.MonsterData : clearedMonsterData;

        if (pauseWhenSelectingCard && Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        if (currentBoss != null)
        {
            currentBoss.HealthChanged -= OnBossHealthChanged;
        }

        currentBoss = null;

        if (attackQueueCoroutine != null || pendingAttackRequests.Count > 0)
        {
            isStageClearPending = true;
            return;
        }

        FinalizeBossDefeat();
    }

    public void NotifyBattleFailed()
    {
        if (isBattleFinished)
        {
            return;
        }

        isBattleFinished = true;
        isBattleClearedThisSession = false;
        isCardSelectionOpen = false;
        pendingAttackRequests.Clear();
        currentAttackCount = 0;
        PersistBattleStatisticsIfNeeded();

        if (pauseWhenSelectingCard && Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        if (attackQueueCoroutine != null)
        {
            StopCoroutine(attackQueueCoroutine);
            attackQueueCoroutine = null;
        }

        RaiseMergeAttackGaugeChanged();
        StageFailed?.Invoke(CurrentStage);
    }

    public void NotifyCardSelected(SO_CardData selectedCardData)
    {
        if (!isCardSelectionOpen)
        {
            return;
        }

        if (selectedCardData != null && cardManager != null)
        {
            cardManager.ApplySelectedCard(selectedCardData);
        }

        isCardSelectionOpen = false;
        currentAttackCount = 0;
        RaiseMergeAttackGaugeChanged();

        if (pauseWhenSelectingCard)
        {
            Time.timeScale = 1f;
        }

        TryStartAttackQueueProcessing();
    }

    public SO_CharacterData GetCharacterDataForGrade(int grade)
    {
        return SC_GradeCharacterResolver.GetCharacterDataForGrade(equippedRoster, grade);
    }

    public Sprite GetFieldSpriteForGrade(int grade)
    {
        int safeGrade = Mathf.Clamp(grade, 1, 10);
        SO_FieldCharacterSkinData skinData = GetEquippedFieldSkinDataForGrade(safeGrade);
        return skinData != null ? skinData.GetFieldSpriteForGrade(safeGrade) : null;
    }

    public Sprite GetPreviewSpriteForGrade(int grade)
    {
        int safeGrade = Mathf.Clamp(grade, 1, 10);
        SO_FieldCharacterSkinData skinData = GetEquippedFieldSkinDataForGrade(safeGrade);
        return skinData != null ? skinData.GetPreviewSpriteForGrade(safeGrade) : null;
    }

    public SO_CharacterData[] GetEquippedRosterSnapshot()
    {
        if (equippedRoster == null)
        {
            return Array.Empty<SO_CharacterData>();
        }

        SO_CharacterData[] copied = new SO_CharacterData[equippedRoster.Length];
        Array.Copy(equippedRoster, copied, equippedRoster.Length);
        return copied;
    }

    public void OpenClearPopup()
    {
        if (clearPopup == null)
        {
            clearPopup = FindClearPopupIncludingInactive();
        }

        if (clearPopup == null)
        {
            Debug.LogWarning("SC_BattleManager: SC_ClearPopup??李얠? 紐삵빐???대━???앹뾽???????놁뒿?덈떎.", this);
            return;
        }

        clearPopup.OpenPopup();
    }

    public SC_FinalMergePopup GetFinalMergePopup()
    {
        if (finalMergePopup == null)
        {
            finalMergePopup = FindFinalMergePopupIncludingInactive();
        }

        return finalMergePopup;
    }

    public ClearRewardResult BuildAndGrantClearRewardResult()
    {
        bool isFirstClearReward = !wasStageClearedOnBattleStart;
        int baseGold = 0;
        int baseDiamond = 0;
        int bonusGold = 0;
        int bonusDiamond = 0;

        if (!hasGrantedBaseClearRewardThisBattle && clearedMonsterData != null)
        {
            baseGold = isFirstClearReward ? clearedMonsterData.FirstClearGoldReward : clearedMonsterData.RepeatClearGoldReward;
            baseDiamond = isFirstClearReward ? clearedMonsterData.FirstClearDiamondReward : clearedMonsterData.RepeatClearDiamondReward;
            bonusGold = CalculateGoldCardBonus(baseGold);
            bonusDiamond = CalculateDiamondCardBonus();

            GrantCurrencyReward(baseGold + bonusGold, baseDiamond + bonusDiamond);
            hasGrantedBaseClearRewardThisBattle = true;

            if (SC_SaveDataManager.Instance != null)
            {
                SC_SaveDataManager.Instance.SetStageCleared(CurrentStage, true);
            }
        }

        if (CanGrantFinalMergeClearBonus())
        {
            bonusDiamond += FinalMergeClearBonusDiamondReward;
            GrantCurrencyReward(0, FinalMergeClearBonusDiamondReward);
            hasGrantedGrade10RewardThisBattle = true;

            if (SC_SaveDataManager.Instance != null)
            {
                SC_SaveDataManager.Instance.SetCreatedGrade10InStage(CurrentStage, true);
            }
        }

        return new ClearRewardResult(
            baseGold,
            bonusGold,
            baseDiamond,
            bonusDiamond,
            HasCreatedGrade10HistoryForCurrentStage());
    }

    private SO_CharacterData GetStartingAttackCharacterData()
    {
        if (equippedRoster == null)
        {
            return null;
        }

        for (int i = 0; i < equippedRoster.Length; i++)
        {
            if (equippedRoster[i] != null)
            {
                return equippedRoster[i];
            }
        }

        return null;
    }

    private void TryStartAttackQueueProcessing()
    {
        if (attackQueueCoroutine != null || isCardSelectionOpen || isBattleFinished)
        {
            return;
        }

        if (pendingAttackRequests.Count <= 0)
        {
            return;
        }

        attackQueueCoroutine = StartCoroutine(CoProcessAttackQueue());
    }

    private IEnumerator CoProcessAttackQueue()
    {
        while (!isCardSelectionOpen && pendingAttackRequests.Count > 0)
        {
            AttackRequest request = pendingAttackRequests.Dequeue();
            SO_CharacterData attacker = request.CharacterData != null ? request.CharacterData : currentAttackCharacterData;
            if (attacker == null)
            {
                continue;
            }

            float attackStartDelay = currentAttackCharacterView != null ? currentAttackCharacterView.AttackStartDelay : 0f;
            if (attackStartDelay > 0f)
            {
                yield return new WaitForSeconds(attackStartDelay);
            }

            currentAttackCharacterData = attacker;
            currentAttackGrade = request.Grade;
            RaiseCurrentAttackCharacterChanged(true);

            float attackImpactDelay = currentAttackCharacterView != null ? currentAttackCharacterView.AttackImpactDelay : 0f;
            if (attackImpactDelay > 0f)
            {
                yield return new WaitForSeconds(attackImpactDelay);
            }

            float finalDamage = CalculateFinalDamage(attacker, request.Grade, request.ApplyFirstMergedAttackBonus);
            ApplyDamageToBoss(finalDamage);

            if (isBattleFinished)
            {
                break;
            }

            currentAttackCount++;
            RaiseMergeAttackGaugeChanged();

            float presentationDuration = currentAttackCharacterView != null ? currentAttackCharacterView.AttackAnimationDuration : 0f;
            float remainingPresentationDuration = Mathf.Max(0f, presentationDuration - attackImpactDelay);

            if (!isBattleClosing && currentAttackCount >= MergeAttackCountPerCard)
            {
                if (remainingPresentationDuration > 0f)
                {
                    yield return new WaitForSeconds(remainingPresentationDuration);
                }

                OpenCardSelection();
                break;
            }

            float attackSpeedMultiplier = Mathf.Max(0.01f, attacker.AttackQueueSpeedPercent + cardAttackQueueSpeedBonus);
            float attackInterval = Mathf.Max(0.01f, baseAttackInterval / attackSpeedMultiplier);
            float delay = remainingPresentationDuration + attackInterval;
            yield return new WaitForSeconds(delay);
        }

        attackQueueCoroutine = null;

        if (isStageClearPending && pendingAttackRequests.Count <= 0)
        {
            FinalizeBossDefeat();
            yield break;
        }

        if (!isBattleFinished && !isCardSelectionOpen && pendingAttackRequests.Count > 0)
        {
            TryStartAttackQueueProcessing();
        }
    }

    private void FinalizeBossDefeat()
    {
        if (isBattleFinished)
        {
            return;
        }

        isBattleFinished = true;
        isBattleClearedThisSession = true;
        isBattleClosing = false;
        isStageClearPending = false;
        currentAttackCount = 0;
        RaiseMergeAttackGaugeChanged();
        RaiseBossHealthChanged(0f, pendingDefeatedBoss != null ? pendingDefeatedBoss.MaxHp : 0f);
        StageCleared?.Invoke(CurrentStage);
        pendingDefeatedBoss = null;
        PersistBattleStatisticsIfNeeded();
        OpenClearPopup();
    }

    private void ApplyDamageToBoss(float damage)
    {
        SC_MonsterHealth targetBoss = currentBoss;
        if (targetBoss == null || targetBoss.CurrentHp <= 0f)
        {
            return;
        }

        float finalDamage = Mathf.Max(0f, damage);
        if (finalDamage <= 0f)
        {
            return;
        }

        float appliedDamage = Mathf.Min(targetBoss.CurrentHp, finalDamage);
        battleDamageDealt += appliedDamage;

        targetBoss.TakeDamage(finalDamage);
        if (targetBoss.CurrentHp <= 0f)
        {
            NotifyBossDefeated(targetBoss);
        }
    }

    private void OpenCardSelection()
    {
        if (isCardSelectionOpen || isBattleFinished)
        {
            return;
        }

        isCardSelectionOpen = true;
        openedCardSelectionCount++;
        CancelAllPendingCharacterDrags();

        if (pauseWhenSelectingCard)
        {
            Time.timeScale = 0f;
        }

        if (battleCardPopup == null)
        {
            battleCardPopup = FindAnyObjectByType<SC_BattleCardPopup>();
        }

        if (battleCardPopup != null)
        {
            battleCardPopup.OpenCardSelection(openedCardSelectionCount);
        }
    }

    private float CalculateFinalDamage(SO_CharacterData attacker, int mergeGrade, bool applyFirstMergedAttackBonus)
    {
        if (attacker == null)
        {
            return 0f;
        }

        if (damageCalculator == null)
        {
            damageCalculator = GetComponent<SC_DamageCalculator>();
        }

        if (damageCalculator == null)
        {
            return attacker.GetBaseDamage(mergeGrade);
        }

        SC_DamageCalculator.DamageContext damageContext =
            new SC_DamageCalculator.DamageContext(attacker, currentBoss, mergeGrade, applyFirstMergedAttackBonus, nextAttackDamageMultiplier);

        SC_DamageCalculator.DamageResult damageResult = damageCalculator.CalculateDamage(damageContext);
        if (applyFirstMergedAttackBonus)
        {
            nextAttackDamageMultiplier = 1f;
        }

        return damageResult.FinalDamage;
    }

    private void PersistBattleStatisticsIfNeeded()
    {
        if (hasPersistedBattleStatistics || SC_SaveDataManager.Instance == null)
        {
            return;
        }

        if (battleMergeCount > 0)
        {
            SC_SaveDataManager.Instance.AddTotalMergeCount(battleMergeCount);
        }

        if (battleDamageDealt > 0f)
        {
            SC_SaveDataManager.Instance.AddTotalBattleDamage(battleDamageDealt);
        }

        hasPersistedBattleStatistics = true;
    }

    private void OnBossHealthChanged(float currentHp, float maxHp)
    {
        RaiseBossHealthChanged(currentHp, maxHp);
    }

    private void RaiseStageChanged()
    {
        StageChanged?.Invoke(CurrentStage, MaxStage);
    }

    private void RaiseBossHealthChanged()
    {
        if (currentBoss == null)
        {
            RaiseBossHealthChanged(0f, 0f);
            return;
        }

        RaiseBossHealthChanged(currentBoss.CurrentHp, currentBoss.MaxHp);
    }

    private void RaiseBossHealthChanged(float currentHp, float maxHp)
    {
        BossHealthChanged?.Invoke(Mathf.Max(0f, currentHp), Mathf.Max(0f, maxHp));
    }

    private void RaiseMergeAttackGaugeChanged()
    {
        MergeAttackGaugeChanged?.Invoke(currentAttackCount, MergeAttackCountPerCard);
    }

    private void RaiseCurrentAttackCharacterChanged(bool playAttackAnimation)
    {
        CurrentAttackCharacterChanged?.Invoke(currentAttackCharacterData, playAttackAnimation);
    }

    private static void CancelAllPendingCharacterDrags()
    {
        SC_PlayerDragAndShoot[] allShooters = FindObjectsByType<SC_PlayerDragAndShoot>();
        for (int i = 0; i < allShooters.Length; i++)
        {
            SC_PlayerDragAndShoot shooter = allShooters[i];
            if (shooter == null || shooter.IsShot)
            {
                continue;
            }

            shooter.CancelDragAndResetToStartPosition();
        }
    }

    private SO_FieldCharacterSkinData GetEquippedFieldSkinDataForGrade(int grade)
    {
        if (equippedFieldSkins == null || equippedFieldSkins.Length <= 0)
        {
            return null;
        }

        int skinIndex = (Mathf.Clamp(grade, 1, 10) - 1) % equippedFieldSkins.Length;
        return skinIndex >= 0 && skinIndex < equippedFieldSkins.Length ? equippedFieldSkins[skinIndex] : null;
    }

    private void ApplySavedRosterOrder()
    {
        int slotCount = Mathf.Max(
            defaultEquippedRoster != null ? defaultEquippedRoster.Length : 0,
            defaultEquippedFieldSkins != null ? defaultEquippedFieldSkins.Length : 0);

        int[] savedOrder = SC_RosterSave.LoadOrder(slotCount);
        equippedRoster = ReorderRoster(defaultEquippedRoster, savedOrder);
        equippedFieldSkins = ReorderFieldSkins(defaultEquippedFieldSkins, savedOrder);
    }

    private void RefreshGradePreviewUI()
    {
        SC_CharacterGradePreviewUI gradePreviewUI = FindAnyObjectByType<SC_CharacterGradePreviewUI>();
        if (gradePreviewUI == null)
        {
            return;
        }

        gradePreviewUI.RefreshPreviewImages();
        gradePreviewUI.RefreshPointerPosition();
    }

    private static SC_ClearPopup FindClearPopupIncludingInactive()
    {
        SC_ClearPopup activePopup = FindAnyObjectByType<SC_ClearPopup>();
        if (activePopup != null)
        {
            return activePopup;
        }

        SC_ClearPopup[] allPopups = Resources.FindObjectsOfTypeAll<SC_ClearPopup>();
        for (int i = 0; i < allPopups.Length; i++)
        {
            SC_ClearPopup popup = allPopups[i];
            if (popup == null || popup.hideFlags != HideFlags.None)
            {
                continue;
            }

            if (!popup.gameObject.scene.IsValid())
            {
                continue;
            }

            return popup;
        }

        return null;
    }

    private static SC_FinalMergePopup FindFinalMergePopupIncludingInactive()
    {
        SC_FinalMergePopup activePopup = FindAnyObjectByType<SC_FinalMergePopup>();
        if (activePopup != null)
        {
            return activePopup;
        }

        SC_FinalMergePopup[] allPopups = Resources.FindObjectsOfTypeAll<SC_FinalMergePopup>();
        for (int i = 0; i < allPopups.Length; i++)
        {
            SC_FinalMergePopup popup = allPopups[i];
            if (popup == null || popup.hideFlags != HideFlags.None)
            {
                continue;
            }

            if (!popup.gameObject.scene.IsValid())
            {
                continue;
            }

            return popup;
        }

        return null;
    }

    private int CalculateGoldCardBonus(int baseGold)
    {
        if (cardManager == null || baseGold <= 0)
        {
            return 0;
        }

        float rawBonus = Mathf.Max(0f, cardManager.BonusGoldReward);
        float bonusRate = rawBonus > 1f ? rawBonus * 0.01f : rawBonus;
        return Mathf.Max(0, Mathf.RoundToInt(baseGold * bonusRate));
    }

    private int CalculateDiamondCardBonus()
    {
        if (cardManager == null)
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.RoundToInt(cardManager.BonusDiamondReward));
    }

    private bool CanGrantFinalMergeClearBonus()
    {
        if (!hasCreatedGrade10ThisBattle || hasGrantedGrade10RewardThisBattle || wasStageClearedOnBattleStart)
        {
            return false;
        }

        if (SC_SaveDataManager.Instance == null)
        {
            return true;
        }

        return !SC_SaveDataManager.Instance.HasCreatedGrade10InStage(CurrentStage);
    }

    private bool HasCreatedGrade10HistoryForCurrentStage()
    {
        if (hasCreatedGrade10ThisBattle)
        {
            return true;
        }

        return SC_SaveDataManager.Instance != null && SC_SaveDataManager.Instance.HasCreatedGrade10InStage(CurrentStage);
    }

    private static void GrantCurrencyReward(int goldAmount, int diamondAmount)
    {
        if (goldAmount > 0)
        {
            if (SC_CurrencyManager.Instance != null)
            {
                SC_CurrencyManager.Instance.AddGold(goldAmount);
            }
            else if (SC_SaveDataManager.Instance != null)
            {
                SC_SaveDataManager.Instance.AddGold(goldAmount);
            }
        }

        if (diamondAmount > 0)
        {
            if (SC_CurrencyManager.Instance != null)
            {
                SC_CurrencyManager.Instance.AddDiamond(diamondAmount);
            }
            else if (SC_SaveDataManager.Instance != null)
            {
                SC_SaveDataManager.Instance.AddDiamond(diamondAmount);
            }
        }
    }

    private static SO_CharacterData[] CloneRoster(SO_CharacterData[] source)
    {
        if (source == null)
        {
            return Array.Empty<SO_CharacterData>();
        }

        SO_CharacterData[] copied = new SO_CharacterData[source.Length];
        Array.Copy(source, copied, source.Length);
        return copied;
    }

    private static SO_FieldCharacterSkinData[] CloneFieldSkins(SO_FieldCharacterSkinData[] source)
    {
        if (source == null)
        {
            return Array.Empty<SO_FieldCharacterSkinData>();
        }

        SO_FieldCharacterSkinData[] copied = new SO_FieldCharacterSkinData[source.Length];
        Array.Copy(source, copied, source.Length);
        return copied;
    }

    private static SO_CharacterData[] ReorderRoster(SO_CharacterData[] source, int[] order)
    {
        if (source == null || source.Length <= 0)
        {
            return Array.Empty<SO_CharacterData>();
        }

        SO_CharacterData[] reordered = new SO_CharacterData[source.Length];
        for (int i = 0; i < reordered.Length; i++)
        {
            int sourceIndex = order != null && i < order.Length ? order[i] : i;
            reordered[i] = sourceIndex >= 0 && sourceIndex < source.Length ? source[sourceIndex] : null;
        }

        return reordered;
    }

    private static SO_FieldCharacterSkinData[] ReorderFieldSkins(SO_FieldCharacterSkinData[] source, int[] order)
    {
        if (source == null || source.Length <= 0)
        {
            return Array.Empty<SO_FieldCharacterSkinData>();
        }

        SO_FieldCharacterSkinData[] reordered = new SO_FieldCharacterSkinData[source.Length];
        for (int i = 0; i < reordered.Length; i++)
        {
            int sourceIndex = order != null && i < order.Length ? order[i] : i;
            reordered[i] = sourceIndex >= 0 && sourceIndex < source.Length ? source[sourceIndex] : null;
        }

        return reordered;
    }
}
