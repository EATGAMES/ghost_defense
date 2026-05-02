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

        public AttackRequest(int grade, SO_CharacterData characterData)
        {
            Grade = grade;
            CharacterData = characterData;
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

    [Tooltip("전투 시작 시 적용할 시작 스테이지 번호입니다.")]
    [SerializeField] private int startStage = 1;

    [Tooltip("로비에서 설정한 5종 캐릭터 순서입니다.")]
    [SerializeField] private SO_CharacterData[] equippedRoster = new SO_CharacterData[5];

    [Tooltip("카드 선택 팝업이 열리기까지 필요한 공격 횟수입니다.")]
    [SerializeField] private int attackCountPerCard = 20;

    [Tooltip("공격 큐를 처리할 때 각 공격 사이의 기본 간격(초)입니다.")]
    [SerializeField] private float baseAttackInterval = 0.2f;

    [Tooltip("카드 선택 중 전투를 일시정지할지 여부입니다.")]
    [SerializeField] private bool pauseWhenSelectingCard = true;

    [Tooltip("20회 공격마다 열리는 카드 선택 팝업입니다.")]
    [SerializeField] private SC_BattleCardPopup battleCardPopup;

    [Tooltip("상단 공격 캐릭터 연출 시간을 참조할 뷰입니다.")]
    [SerializeField] private SC_CurrentAttackCharacterView currentAttackCharacterView;

    private readonly Queue<AttackRequest> pendingAttackRequests = new Queue<AttackRequest>();

    private SC_MonsterHealth currentBoss;
    private Coroutine attackQueueCoroutine;
    private SO_CharacterData currentAttackCharacterData;
    private int currentAttackCount;
    private int openedCardSelectionCount;
    private bool isCardSelectionOpen;
    private bool isBattleFinished;

    public int MaxStage => Mathf.Max(1, maxStage);
    public int CurrentMergeAttackCount => currentAttackCount;
    public int MergeAttackCountPerCard => Mathf.Max(1, attackCountPerCard);
    public bool IsCardSelectionOpen => isCardSelectionOpen;
    public bool IsBattleFinished => isBattleFinished;
    public int PendingAttackQueueCount => pendingAttackRequests.Count;
    public SO_CharacterData CurrentAttackCharacterData => currentAttackCharacterData;

    private void Awake()
    {
        if (battleCardPopup == null)
        {
            battleCardPopup = FindAnyObjectByType<SC_BattleCardPopup>();
        }

        if (currentAttackCharacterView == null)
        {
            currentAttackCharacterView = FindAnyObjectByType<SC_CurrentAttackCharacterView>();
        }
    }

    private void Start()
    {
        CurrentStage = Mathf.Clamp(startStage, 1, MaxStage);
        currentAttackCharacterData = GetStartingAttackCharacterData();

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
        if (isBattleFinished || isCardSelectionOpen)
        {
            return;
        }

        SO_CharacterData targetCharacterData = GetCharacterDataForGrade(mergedGrade);
        pendingAttackRequests.Enqueue(new AttackRequest(Mathf.Clamp(mergedGrade, 1, 10), targetCharacterData));
        TryStartAttackQueueProcessing();
        RaiseMergeAttackGaugeChanged();
    }

    public void NotifyBossDefeated(SC_MonsterHealth defeatedBoss)
    {
        if (isBattleFinished)
        {
            return;
        }

        if (currentBoss != null && defeatedBoss != null && currentBoss != defeatedBoss)
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

        if (currentBoss != null)
        {
            currentBoss.HealthChanged -= OnBossHealthChanged;
        }

        currentBoss = null;
        RaiseMergeAttackGaugeChanged();
        RaiseBossHealthChanged(0f, defeatedBoss != null ? defeatedBoss.MaxHp : 0f);
        StageCleared?.Invoke(CurrentStage);
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
        SO_CharacterData characterData = GetCharacterDataForGrade(grade);
        return characterData != null ? characterData.GetFieldSpriteForGrade(grade) : null;
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
        while (!isBattleFinished && !isCardSelectionOpen && pendingAttackRequests.Count > 0)
        {
            if (currentBoss == null || currentBoss.CurrentHp <= 0f)
            {
                pendingAttackRequests.Clear();
                RaiseMergeAttackGaugeChanged();
                break;
            }

            AttackRequest request = pendingAttackRequests.Dequeue();
            SO_CharacterData attacker = request.CharacterData != null ? request.CharacterData : currentAttackCharacterData;
            if (attacker == null)
            {
                break;
            }

            float attackStartDelay = currentAttackCharacterView != null ? currentAttackCharacterView.AttackStartDelay : 0f;
            if (attackStartDelay > 0f)
            {
                yield return new WaitForSeconds(attackStartDelay);
            }

            currentAttackCharacterData = attacker;
            RaiseCurrentAttackCharacterChanged(true);

            float finalDamage = attacker.CalculateAttackDamage(request.Grade);
            ApplyDamageToBoss(finalDamage);

            if (isBattleFinished)
            {
                break;
            }

            currentAttackCount++;
            RaiseMergeAttackGaugeChanged();

            if (currentAttackCount >= MergeAttackCountPerCard)
            {
                OpenCardSelection();
                break;
            }

            float attackInterval = Mathf.Max(0.01f, baseAttackInterval / Mathf.Max(0.01f, attacker.AttackQueueSpeedPercent));
            float presentationDuration = currentAttackCharacterView != null ? currentAttackCharacterView.AttackAnimationDuration : 0f;
            float delay = presentationDuration + attackInterval;
            yield return new WaitForSeconds(delay);
        }

        attackQueueCoroutine = null;

        if (!isBattleFinished && !isCardSelectionOpen && pendingAttackRequests.Count > 0)
        {
            TryStartAttackQueueProcessing();
        }
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
}
