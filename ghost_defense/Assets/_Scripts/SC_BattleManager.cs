using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_BattleManager : MonoBehaviour
{
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

    public static int CurrentStage { get; private set; } = 1;

    public event Action<int, int> StageChanged;
    public event Action<float, float> BossHealthChanged;
    public event Action<int, int> MergeAttackGaugeChanged;
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

    [Tooltip("카드 선택 팝업을 열기까지 필요한 공격 횟수입니다.")]
    [SerializeField] private int attackCountPerCard = 20;

    [Tooltip("공격 요청 처리 사이 기본 간격(초)입니다.")]
    [SerializeField] private float baseAttackInterval = 0.2f;

    [Tooltip("카드 선택 중 전투를 일시 정지할지 여부입니다.")]
    [SerializeField] private bool pauseWhenSelectingCard = true;

    [Tooltip("일정 공격 횟수마다 열릴 카드 선택 팝업입니다.")]
    [SerializeField] private SC_BattleCardPopup battleCardPopup;

    [Tooltip("상단 공격 캐릭터의 연출 시간을 참조할 뷰입니다.")]
    [SerializeField] private SC_CurrentAttackCharacterView currentAttackCharacterView;

    [Tooltip("최종 전투 데미지 공식을 계산할 계산기입니다.")]
    [SerializeField] private SC_DamageCalculator damageCalculator;

    private readonly Queue<AttackRequest> pendingAttackRequests = new Queue<AttackRequest>();

    private SC_MonsterHealth currentBoss;
    private Coroutine attackQueueCoroutine;
    private SO_CharacterData currentAttackCharacterData;
    private SO_CharacterData[] defaultEquippedRoster;
    private SO_FieldCharacterSkinData[] defaultEquippedFieldSkins;
    private int currentAttackGrade;
    private int currentAttackCount;
    private int openedCardSelectionCount;
    private bool isCardSelectionOpen;
    private bool isBattleFinished;
    private bool isBattleClosing;
    private bool isStageClearPending;
    private bool isNextMergedAttackBonusArmed;
    private SC_MonsterHealth pendingDefeatedBoss;

    public int MaxStage => Mathf.Max(1, maxStage);
    public int CurrentMergeAttackCount => currentAttackCount;
    public int MergeAttackCountPerCard => Mathf.Max(1, attackCountPerCard);
    public bool IsCardSelectionOpen => isCardSelectionOpen;
    public bool IsBattleFinished => isBattleFinished;
    public int PendingAttackQueueCount => pendingAttackRequests.Count;
    public SO_CharacterData CurrentAttackCharacterData => currentAttackCharacterData;
    public int CurrentAttackGrade => Mathf.Clamp(currentAttackGrade, 0, 10);

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
    }

    private void Start()
    {
        CurrentStage = Mathf.Clamp(startStage, 1, MaxStage);
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

        SO_CharacterData targetCharacterData = GetCharacterDataForGrade(mergedGrade);
        bool applyFirstMergedAttackBonus = isNextMergedAttackBonusArmed;
        isNextMergedAttackBonusArmed = false;
        pendingAttackRequests.Enqueue(new AttackRequest(Mathf.Clamp(mergedGrade, 1, 10), targetCharacterData, applyFirstMergedAttackBonus));
        TryStartAttackQueueProcessing();
        RaiseMergeAttackGaugeChanged();
    }

    public void ArmNextMergedAttackDamageBonus()
    {
        isNextMergedAttackBonusArmed = true;
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
        isCardSelectionOpen = false;
        pendingAttackRequests.Clear();
        currentAttackCount = 0;

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

    public void NotifyCardSelected()
    {
        if (!isCardSelectionOpen)
        {
            return;
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

            float attackInterval = Mathf.Max(0.01f, baseAttackInterval / Mathf.Max(0.01f, attacker.AttackQueueSpeedPercent));
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
        isBattleClosing = false;
        isStageClearPending = false;
        currentAttackCount = 0;
        RaiseMergeAttackGaugeChanged();
        RaiseBossHealthChanged(0f, pendingDefeatedBoss != null ? pendingDefeatedBoss.MaxHp : 0f);
        StageCleared?.Invoke(CurrentStage);
        pendingDefeatedBoss = null;
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
            new SC_DamageCalculator.DamageContext(attacker, currentBoss, mergeGrade, applyFirstMergedAttackBonus);

        SC_DamageCalculator.DamageResult damageResult = damageCalculator.CalculateDamage(damageContext);
        return damageResult.FinalDamage;
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
