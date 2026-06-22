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
        public readonly int BaseDiamond;
        public readonly int BonusDiamond;
        public readonly bool ShowCloseCenterOnly;

        public ClearRewardResult(int baseGold, int baseDiamond, int bonusDiamond, bool showCloseCenterOnly)
        {
            BaseGold = Mathf.Max(0, baseGold);
            BaseDiamond = Mathf.Max(0, baseDiamond);
            BonusDiamond = Mathf.Max(0, bonusDiamond);
            ShowCloseCenterOnly = showCloseCenterOnly;
        }
    }

    private readonly struct AttackRequest
    {
        public readonly int Grade;
        public readonly SO_CharacterData CharacterData;
        public readonly float ComboDamageMultiplier;

        public AttackRequest(int grade, SO_CharacterData characterData, float comboDamageMultiplier)
        {
            Grade = grade;
            CharacterData = characterData;
            ComboDamageMultiplier = Mathf.Max(1f, comboDamageMultiplier);
        }
    }

    private readonly struct PendingMergeFxAttack
    {
        public readonly int Grade;
        public readonly float ComboDamageMultiplier;

        public PendingMergeFxAttack(int grade, float comboDamageMultiplier)
        {
            Grade = Mathf.Clamp(grade, 1, 10);
            ComboDamageMultiplier = Mathf.Max(1f, comboDamageMultiplier);
        }
    }

    private const int FinalMergeClearBonusDiamondReward = 50;

    public static int CurrentStage { get; private set; } = 1;

    public event Action<int, int> StageChanged;
    public event Action<float, float> BossHealthChanged;
    public event Action<SO_CharacterData, bool> CurrentAttackCharacterChanged;
    public event Action<int> StageCleared;
    public event Action<int> StageFailed;

    [Tooltip("최대 스테이지 수입니다.")]
    [SerializeField] private int maxStage = 10;

    [Tooltip("전투 시작 때 적용할 시작 스테이지 번호입니다.")]
    [SerializeField] private int startStage = 1;

    [Tooltip("상단 공격 캐릭터의 데미지 계산에 사용할 공격 캐릭터 데이터 목록입니다.")]
    [SerializeField] private SO_CharacterData[] equippedRoster = new SO_CharacterData[5];

    [Tooltip("하단 필드 캐릭터 스프라이트에 사용할 필드 스킨 데이터 목록입니다.")]
    [SerializeField] private SO_FieldCharacterSkinData[] equippedFieldSkins = new SO_FieldCharacterSkinData[5];

    [Tooltip("공격 요청 처리 사이 기본 간격(초)입니다.")]
    [SerializeField] private float baseAttackInterval = 0.2f;

    [Tooltip("상단 공격 캐릭터의 연출 시간 참조용 뷰입니다.")]
    [SerializeField] private SC_CurrentAttackCharacterView currentAttackCharacterView;

    [Tooltip("최종 전투 데미지 공식을 계산할 계산기입니다.")]
    [SerializeField] private SC_DamageCalculator damageCalculator;

    [Tooltip("10단계 최종 합성 연출 팝업입니다.")]
    [SerializeField] private SC_FinalMergePopup finalMergePopup;

    [Tooltip("스테이지 클리어 보상과 버튼을 표시하는 클리어 팝업입니다.")]
    [SerializeField] private SC_ClearPopup clearPopup;

    [Tooltip("클리어 후 계속하기에서 샌드백 생성을 맡을 보스 스포너입니다.")]
    [SerializeField] private SC_BossSpawner bossSpawner;

    private readonly Queue<AttackRequest> pendingAttackRequests = new Queue<AttackRequest>();
    private readonly SortedDictionary<long, PendingMergeFxAttack> arrivedMergeFxAttacks = new SortedDictionary<long, PendingMergeFxAttack>();

    private SC_MonsterHealth currentBoss;
    private Coroutine attackQueueCoroutine;
    private SO_CharacterData currentAttackCharacterData;
    private SO_CharacterData[] defaultEquippedRoster;
    private SO_FieldCharacterSkinData[] defaultEquippedFieldSkins;
    private SO_MonsterData clearedMonsterData;
    private int currentAttackGrade;
    private bool isBattleFinished;
    private bool isBattleClosing;
    private bool isStageClearPending;
    private bool isBattleClearedThisSession;
    private bool isPostClearContinueMode;
    private bool wasStageClearedOnBattleStart;
    private bool hasGrantedBaseClearRewardThisBattle;
    private bool hasCreatedGrade10ThisBattle;
    private bool hasGrantedGrade10RewardThisBattle;
    private int battleMergeCount;
    private long nextMergeFxAttackSequence = 1;
    private long nextMergeFxAttackDeliverySequence = 1;
    private float battleDamageDealt;
    private bool hasPersistedBattleStatistics;
    private SC_MonsterHealth pendingDefeatedBoss;

    public int MaxStage => Mathf.Max(1, maxStage);
    public int BattleMergeCount => Mathf.Max(0, battleMergeCount);
    public float BattleDamageDealt => Mathf.Max(0f, battleDamageDealt);
    public bool IsBattleFinished => isBattleFinished;
    public bool IsBattleClearedThisSession => isBattleClearedThisSession;
    public bool IsPostClearContinueMode => isPostClearContinueMode;
    public int PendingAttackQueueCount => pendingAttackRequests.Count;
    public SO_CharacterData CurrentAttackCharacterData => currentAttackCharacterData;
    public int CurrentAttackGrade => Mathf.Clamp(currentAttackGrade, 0, 10);
    public bool HasAliveBoss => currentBoss != null && currentBoss.CurrentHp > 0f && !isBattleClosing && !isBattleFinished;

    private void Awake()
    {
        defaultEquippedRoster = CloneRoster(equippedRoster);
        defaultEquippedFieldSkins = CloneFieldSkins(equippedFieldSkins);
        ApplySavedRosterOrder();

        if (currentAttackCharacterView == null)
        {
            currentAttackCharacterView = FindAnyObjectByType<SC_CurrentAttackCharacterView>();
        }

        if (damageCalculator == null)
        {
            damageCalculator = GetComponent<SC_DamageCalculator>();
        }

        if (finalMergePopup == null)
        {
            finalMergePopup = FindFinalMergePopupIncludingInactive();
        }

        if (clearPopup == null)
        {
            clearPopup = FindClearPopupIncludingInactive();
        }

        if (bossSpawner == null)
        {
            bossSpawner = FindAnyObjectByType<SC_BossSpawner>();
        }
    }

    private void Start()
    {
        isBattleClearedThisSession = false;
        isPostClearContinueMode = false;
        battleDamageDealt = 0f;
        int savedSelectedStage = SC_SaveDataManager.Instance != null ? SC_SaveDataManager.Instance.SelectedStage : startStage;
        CurrentStage = Mathf.Clamp(savedSelectedStage, 1, MaxStage);
        wasStageClearedOnBattleStart = SC_SaveDataManager.Instance != null && SC_SaveDataManager.Instance.IsStageCleared(CurrentStage);
        currentAttackCharacterData = GetStartingAttackCharacterData();
        currentAttackGrade = currentAttackCharacterData != null ? 1 : 0;

        RefreshGradePreviewUI();
        RaiseStageChanged();
        RaiseBossHealthChanged();
        RaiseCurrentAttackCharacterChanged(false);
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
            if (currentBoss.MonsterData != null)
            {
                clearedMonsterData = currentBoss.MonsterData;
            }
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

    public void NotifyMergeAttack(int mergedGrade, float comboDamageMultiplier = 1f)
    {
        NotifyMergeAttackInternal(mergedGrade, comboDamageMultiplier);
    }

    public long ReserveMergeFxAttackSequence()
    {
        return nextMergeFxAttackSequence++;
    }

    public void NotifyMergeFxAttackArrived(long sequence, int mergedGrade, float comboDamageMultiplier = 1f)
    {
        if (sequence <= 0)
        {
            NotifyMergeAttackInternal(mergedGrade, comboDamageMultiplier);
            return;
        }

        if (sequence < nextMergeFxAttackDeliverySequence)
        {
            return;
        }

        arrivedMergeFxAttacks[sequence] = new PendingMergeFxAttack(mergedGrade, comboDamageMultiplier);
        FlushArrivedMergeFxAttacks();
    }

    private void FlushArrivedMergeFxAttacks()
    {
        while (arrivedMergeFxAttacks.TryGetValue(nextMergeFxAttackDeliverySequence, out PendingMergeFxAttack pendingAttack))
        {
            arrivedMergeFxAttacks.Remove(nextMergeFxAttackDeliverySequence);
            nextMergeFxAttackDeliverySequence++;
            NotifyMergeAttackInternal(pendingAttack.Grade, pendingAttack.ComboDamageMultiplier);
        }
    }

    private void NotifyMergeAttackInternal(int mergedGrade, float comboDamageMultiplier)
    {
        if (isBattleFinished || isBattleClosing)
        {
            return;
        }

        battleMergeCount++;

        SO_CharacterData targetCharacterData = GetCharacterDataForGrade(mergedGrade);
        pendingAttackRequests.Enqueue(new AttackRequest(Mathf.Clamp(mergedGrade, 1, 10), targetCharacterData, comboDamageMultiplier));

        TryStartAttackQueueProcessing();
    }

    public void NotifyFinalMergeAttack(int mergedGrade, float comboDamageMultiplier = 1f)
    {
        if (isBattleClearedThisSession && !isPostClearContinueMode)
        {
            OpenClearPopup();
            return;
        }

        NotifyMergeAttack(mergedGrade, comboDamageMultiplier);
    }

    public void NotifyCreatedGrade10ThisBattle()
    {
        hasCreatedGrade10ThisBattle = true;
    }

    public void StartPostClearContinueMode()
    {
        if (!isBattleClearedThisSession)
        {
            return;
        }

        if (bossSpawner == null)
        {
            bossSpawner = FindAnyObjectByType<SC_BossSpawner>();
        }

        if (bossSpawner == null || !bossSpawner.StartPostClearTrainingMode())
        {
            Debug.LogWarning("SC_BattleManager: 샌드백 전환에 필요한 SC_BossSpawner를 찾지 못했거나 샌드백 생성에 실패했습니다.", this);
            return;
        }

        isBattleFinished = false;
        isBattleClosing = false;
        isStageClearPending = false;
        isPostClearContinueMode = true;
        pendingDefeatedBoss = null;
        RaiseBossHealthChanged();
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
        pendingDefeatedBoss = defeatedBoss;
        clearedMonsterData = defeatedBoss != null ? defeatedBoss.MonsterData : clearedMonsterData;

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

        bool wasBattleCleared = isBattleClearedThisSession;
        isBattleFinished = true;
        isBattleClearedThisSession = wasBattleCleared;
        pendingAttackRequests.Clear();
        arrivedMergeFxAttacks.Clear();
        nextMergeFxAttackSequence = 1;
        nextMergeFxAttackDeliverySequence = 1;
        PersistBattleStatisticsIfNeeded();
        if (attackQueueCoroutine != null)
        {
            StopCoroutine(attackQueueCoroutine);
            attackQueueCoroutine = null;
        }

        StageFailed?.Invoke(CurrentStage);
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

    public Sprite GetAttackCharacterPreviewSpriteForGrade(int grade)
    {
        int safeGrade = Mathf.Clamp(grade, 1, 10);
        SO_CharacterData characterData = GetCharacterDataForGrade(safeGrade);
        if (characterData == null)
        {
            return null;
        }

        Sprite previewSprite = characterData.PreviewCharacterSprite;
        return previewSprite != null ? previewSprite : characterData.GetTopCharacterSpriteForGrade(safeGrade);
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
            Debug.LogWarning("SC_BattleManager: SC_ClearPopup을 찾지 못해 클리어 팝업을 열 수 없습니다.", this);
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
        int bonusDiamond = 0;

        if (!hasGrantedBaseClearRewardThisBattle && clearedMonsterData != null)
        {
            baseGold = isFirstClearReward ? clearedMonsterData.FirstClearGoldReward : clearedMonsterData.RepeatClearGoldReward;
            baseDiamond = isFirstClearReward ? clearedMonsterData.FirstClearDiamondReward : clearedMonsterData.RepeatClearDiamondReward;
            GrantCurrencyReward(baseGold, baseDiamond);
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
        if (attackQueueCoroutine != null || isBattleFinished)
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
        while (pendingAttackRequests.Count > 0)
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

            float finalDamage = CalculateFinalDamage(attacker, request.Grade, request.ComboDamageMultiplier);
            ApplyDamageToBoss(finalDamage);

            if (isBattleFinished)
            {
                break;
            }

            float presentationDuration = currentAttackCharacterView != null ? currentAttackCharacterView.AttackAnimationDuration : 0f;
            float remainingPresentationDuration = Mathf.Max(0f, presentationDuration - attackImpactDelay);

            float attackSpeedMultiplier = Mathf.Max(0.01f, attacker.AttackQueueSpeedPercent);
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

        if (!isBattleFinished && pendingAttackRequests.Count > 0)
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
        isPostClearContinueMode = false;
        isBattleClosing = false;
        isStageClearPending = false;
        RaiseBossHealthChanged(0f, pendingDefeatedBoss != null ? pendingDefeatedBoss.MaxHp : 0f);
        StageCleared?.Invoke(CurrentStage);
        pendingDefeatedBoss = null;
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

        float appliedDamage = targetBoss.IsImmortalTarget ? finalDamage : Mathf.Min(targetBoss.CurrentHp, finalDamage);
        battleDamageDealt += appliedDamage;

        targetBoss.TakeDamage(finalDamage);
        if (targetBoss.CurrentHp <= 0f)
        {
            NotifyBossDefeated(targetBoss);
        }
    }

    private float CalculateFinalDamage(SO_CharacterData attacker, int mergeGrade, float comboDamageMultiplier)
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
            return attacker.GetBaseDamage(mergeGrade) * Mathf.Max(1f, comboDamageMultiplier);
        }

        SC_DamageCalculator.DamageContext damageContext =
            new SC_DamageCalculator.DamageContext(attacker, currentBoss, mergeGrade, comboDamageMultiplier);

        SC_DamageCalculator.DamageResult damageResult = damageCalculator.CalculateDamage(damageContext);
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

        if (currentBoss.IsImmortalTarget)
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

    private void RaiseCurrentAttackCharacterChanged(bool playAttackAnimation)
    {
        CurrentAttackCharacterChanged?.Invoke(currentAttackCharacterData, playAttackAnimation);
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
        SO_CharacterData[] orderedRoster = ReorderRoster(defaultEquippedRoster, savedOrder);
        SO_FieldCharacterSkinData[] orderedFieldSkins = ReorderFieldSkins(defaultEquippedFieldSkins, savedOrder);
        ApplyCharacterUseSettings(orderedRoster, orderedFieldSkins);
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

    private void ApplyCharacterUseSettings(SO_CharacterData[] orderedRoster, SO_FieldCharacterSkinData[] orderedFieldSkins)
    {
        if (orderedRoster == null || orderedRoster.Length <= 0)
        {
            equippedRoster = Array.Empty<SO_CharacterData>();
            equippedFieldSkins = Array.Empty<SO_FieldCharacterSkinData>();
            return;
        }

        List<SO_CharacterData> enabledRoster = new List<SO_CharacterData>(orderedRoster.Length);
        List<SO_FieldCharacterSkinData> enabledFieldSkins = new List<SO_FieldCharacterSkinData>(orderedRoster.Length);

        for (int i = 0; i < orderedRoster.Length; i++)
        {
            SO_CharacterData characterData = orderedRoster[i];
            if (characterData == null || !IsCharacterUseEnabled(characterData))
            {
                continue;
            }

            enabledRoster.Add(characterData);
            enabledFieldSkins.Add(orderedFieldSkins != null && i < orderedFieldSkins.Length ? orderedFieldSkins[i] : null);
        }

        equippedRoster = enabledRoster.ToArray();
        equippedFieldSkins = enabledFieldSkins.ToArray();
    }

    private static bool IsCharacterUseEnabled(SO_CharacterData characterData)
    {
        if (characterData == null)
        {
            return false;
        }

        string characterSaveKey = characterData.GetSaveKey();
        if (string.IsNullOrWhiteSpace(characterSaveKey) || SC_SaveDataManager.Instance == null)
        {
            return characterData.DefaultUseEnabled;
        }

        return SC_SaveDataManager.Instance.GetCharacterUseState(characterSaveKey, characterData.DefaultUseEnabled);
    }
}
