using UnityEngine;

[DisallowMultipleComponent]
public class SC_BattleCharacterSpawner : MonoBehaviour, IBattleCharacterSpawner
{
    public event System.Action NextSpawnPreviewChanged;

    [Tooltip("하단에서 발사할 캐릭터 오브젝트를 생성할 프리팹입니다.")]
    [SerializeField] private GameObject characterPrefab;

    [Tooltip("대기 캐릭터를 생성할 위치입니다. 비워두면 현재 오브젝트 위치를 사용합니다.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("생성한 캐릭터를 넣어둘 부모 Transform입니다. 비워두면 루트에 생성합니다.")]
    [SerializeField] private Transform spawnedParent;

    [Tooltip("프리팹에 없을 때 SC_PlayerDragAndShoot를 자동으로 추가할지 여부입니다.")]
    [SerializeField] private bool addDragAndShootIfMissing = true;

    [Tooltip("1단계 캐릭터 생성 가중치입니다.")]
    [SerializeField] private float grade1Weight = 25f;

    [Tooltip("2단계 캐릭터 생성 가중치입니다.")]
    [SerializeField] private float grade2Weight = 25f;

    [Tooltip("3단계 캐릭터 생성 가중치입니다.")]
    [SerializeField] private float grade3Weight = 20f;

    [Tooltip("4단계 캐릭터 생성 가중치입니다.")]
    [SerializeField] private float grade4Weight = 18f;

    [Tooltip("5단계 캐릭터 생성 가중치입니다.")]
    [SerializeField] private float grade5Weight = 12f;

    [Tooltip("다음 대기 캐릭터를 다시 생성하기까지의 지연 시간(초)입니다.")]
    [SerializeField] private float respawnDelay = 0.1f;

    [Tooltip("전투 중 카드 효과를 참조할 카드 매니저입니다.")]
    [SerializeField] private SC_CardManager cardManager;

    private SC_PlayerDragAndShoot currentWaitingCharacter;
    private float respawnTimer;
    private bool isRespawnScheduled;
    private int? nextSpawnGradeOverride;
    private int? preparedNextSpawnGrade;

    public StageBattleDirection BattleDirection => StageBattleDirection.UP;
    public bool IsSpawnerActive => isActiveAndEnabled;

    private void Start()
    {
        if (cardManager == null)
        {
            cardManager = FindAnyObjectByType<SC_CardManager>();
        }

        PrepareNextSpawnGradePreview();
        TrySpawnWaitingCharacter();
    }

    private void Update()
    {
        EnforceExcludeLowGradeSpawnEffect();

        // 대기 캐릭터가 발사됐거나 합체/삭제로 사라졌다면 다음 스폰을 예약한다.
        if (currentWaitingCharacter == null)
        {
            ScheduleRespawn();
            return;
        }

        if (currentWaitingCharacter.IsShot)
        {
            currentWaitingCharacter = null;
            ScheduleRespawn();
        }
    }

    private void LateUpdate()
    {
        if (!isRespawnScheduled)
        {
            return;
        }

        respawnTimer -= Time.deltaTime;
        if (respawnTimer > 0f)
        {
            return;
        }

        isRespawnScheduled = false;
        TrySpawnWaitingCharacter();
    }

    public void QueueNextSpawnGrade(int grade)
    {
        nextSpawnGradeOverride = Mathf.Clamp(grade, 1, 10);
        preparedNextSpawnGrade = null;
        PrepareNextSpawnGradePreview();
    }

    public void QueueNextCharacterGrade(int grade)
    {
        QueueNextSpawnGrade(grade);
    }

    public int GetNextSpawnPreviewGrade()
    {
        PrepareNextSpawnGradePreview();
        return Mathf.Clamp(preparedNextSpawnGrade ?? 1, 1, 10);
    }

    public void RefreshNextSpawnPreviewGrade()
    {
        preparedNextSpawnGrade = null;
        PrepareNextSpawnGradePreview();
    }

    private void ScheduleRespawn()
    {
        if (isRespawnScheduled)
        {
            return;
        }

        isRespawnScheduled = true;
        respawnTimer = Mathf.Max(0f, respawnDelay);
    }

    private void TrySpawnWaitingCharacter()
    {
        if (characterPrefab == null)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: characterPrefab가 비어 있습니다.", this);
            return;
        }

        int spawnGrade = ConsumePreparedSpawnGrade();
        nextSpawnGradeOverride = null;

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject mergeObject = Instantiate(characterPrefab, position, rotation, spawnedParent);
        ApplyMergeObjectData(mergeObject, spawnGrade);

        SC_PlayerDragAndShoot shootComponent = mergeObject.GetComponent<SC_PlayerDragAndShoot>();
        if (shootComponent == null && addDragAndShootIfMissing)
        {
            shootComponent = mergeObject.AddComponent<SC_PlayerDragAndShoot>();
        }

        if (shootComponent == null)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: SC_PlayerDragAndShoot 컴포넌트를 찾지 못했습니다.", this);
            Destroy(mergeObject);
            return;
        }

        currentWaitingCharacter = shootComponent;
        PrepareNextSpawnGradePreview();
    }

    private void ApplyMergeObjectData(GameObject mergeObject, int mergeGrade)
    {
        if (mergeObject == null)
        {
            return;
        }

        SC_CharacterPresenter presenter = mergeObject.GetComponent<SC_CharacterPresenter>();
        if (presenter == null)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: SC_CharacterPresenter를 찾지 못했습니다.", this);
            return;
        }

        presenter.Configure(mergeGrade, true);
    }

    private int PickWeightedSpawnGrade()
    {
        bool isExcludeLowGradeSpawnActive = cardManager != null && cardManager.IsExcludeLowGradeSpawnActive();
        float grade1EffectiveWeight = isExcludeLowGradeSpawnActive ? 0f : Mathf.Max(0f, grade1Weight);
        float grade2EffectiveWeight = Mathf.Max(0f, grade2Weight);
        float grade3EffectiveWeight = Mathf.Max(0f, grade3Weight);
        float grade4EffectiveWeight = Mathf.Max(0f, grade4Weight);
        float grade5EffectiveWeight = Mathf.Max(0f, grade5Weight);

        float totalWeight =
            grade1EffectiveWeight +
            grade2EffectiveWeight +
            grade3EffectiveWeight +
            grade4EffectiveWeight +
            grade5EffectiveWeight;

        if (totalWeight <= 0f)
        {
            return isExcludeLowGradeSpawnActive ? 2 : 1;
        }

        float roll = Random.Range(0f, totalWeight);

        float accumulatedWeight = grade1EffectiveWeight;
        if (roll < accumulatedWeight)
        {
            return 1;
        }

        accumulatedWeight += grade2EffectiveWeight;
        if (roll < accumulatedWeight)
        {
            return 2;
        }

        accumulatedWeight += grade3EffectiveWeight;
        if (roll < accumulatedWeight)
        {
            return 3;
        }

        accumulatedWeight += grade4EffectiveWeight;
        if (roll < accumulatedWeight)
        {
            return 4;
        }

        return 5;
    }

    private int ConsumePreparedSpawnGrade()
    {
        PrepareNextSpawnGradePreview();
        int spawnGrade = Mathf.Clamp(preparedNextSpawnGrade ?? 1, 1, 10);
        preparedNextSpawnGrade = null;
        return spawnGrade;
    }

    private void PrepareNextSpawnGradePreview()
    {
        int previousPreviewGrade = preparedNextSpawnGrade ?? 0;
        int nextPreviewGrade = nextSpawnGradeOverride ?? PickWeightedSpawnGrade();
        preparedNextSpawnGrade = Mathf.Clamp(nextPreviewGrade, 1, 10);

        if (previousPreviewGrade != preparedNextSpawnGrade.Value)
        {
            NextSpawnPreviewChanged?.Invoke();
        }
    }

    private void EnforceExcludeLowGradeSpawnEffect()
    {
        if (currentWaitingCharacter == null || cardManager == null || !cardManager.IsExcludeLowGradeSpawnActive())
        {
            return;
        }

        SC_CharacterPresenter presenter = currentWaitingCharacter.GetComponent<SC_CharacterPresenter>();
        if (presenter == null || presenter.MergeGrade != 1)
        {
            return;
        }

        Destroy(currentWaitingCharacter.gameObject);
        currentWaitingCharacter = null;
        isRespawnScheduled = false;
        preparedNextSpawnGrade = null;
        PrepareNextSpawnGradePreview();
        TrySpawnWaitingCharacter();
    }
}
