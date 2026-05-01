using UnityEngine;

[DisallowMultipleComponent]
public class SC_BattleCharacterSpawner : MonoBehaviour
{
    [Tooltip("발사 대기 머지 오브젝트를 생성할 프리팹입니다.")]
    [SerializeField] private GameObject characterPrefab;

    [Tooltip("대기 오브젝트 생성 위치입니다. 비워두면 현재 오브젝트 위치를 사용합니다.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("생성된 오브젝트의 부모 Transform입니다. 비워두면 루트에 생성됩니다.")]
    [SerializeField] private Transform spawnedParent;

    [Tooltip("프리팹에 없을 때 SC_PlayerDragAndShoot를 자동으로 추가할지 여부입니다.")]
    [SerializeField] private bool addDragAndShootIfMissing = true;

    [Tooltip("1단계가 생성될 가중치입니다.")]
    [SerializeField] private float grade1Weight = 25f;

    [Tooltip("2단계가 생성될 가중치입니다.")]
    [SerializeField] private float grade2Weight = 25f;

    [Tooltip("3단계가 생성될 가중치입니다.")]
    [SerializeField] private float grade3Weight = 20f;

    [Tooltip("4단계가 생성될 가중치입니다.")]
    [SerializeField] private float grade4Weight = 18f;

    [Tooltip("5단계가 생성될 가중치입니다.")]
    [SerializeField] private float grade5Weight = 12f;

    [Tooltip("다음 대기 오브젝트를 다시 생성하기까지의 지연 시간(초)입니다.")]
    [SerializeField] private float respawnDelay = 0.1f;

    private SC_PlayerDragAndShoot currentWaitingCharacter;
    private float respawnTimer;
    private bool isRespawnScheduled;
    private int? nextSpawnGradeOverride;

    private void Start()
    {
        TrySpawnWaitingCharacter();
    }

    private void Update()
    {
        if (currentWaitingCharacter != null && currentWaitingCharacter.IsShot)
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
            Debug.LogWarning("SC_BattleCharacterSpawner: characterPrefab이 비어 있습니다.");
            return;
        }

        int spawnGrade = nextSpawnGradeOverride ?? PickWeightedSpawnGrade();
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
            Debug.LogWarning("SC_BattleCharacterSpawner: SC_PlayerDragAndShoot 컴포넌트를 찾지 못했습니다.");
            Destroy(mergeObject);
            return;
        }

        currentWaitingCharacter = shootComponent;
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
            Debug.LogWarning("SC_BattleCharacterSpawner: SC_CharacterPresenter를 찾지 못했습니다.");
            return;
        }

        presenter.Configure(mergeGrade, true);
    }

    private int PickWeightedSpawnGrade()
    {
        float totalWeight =
            Mathf.Max(0f, grade1Weight) +
            Mathf.Max(0f, grade2Weight) +
            Mathf.Max(0f, grade3Weight) +
            Mathf.Max(0f, grade4Weight) +
            Mathf.Max(0f, grade5Weight);

        if (totalWeight <= 0f)
        {
            return 1;
        }

        float roll = Random.Range(0f, totalWeight);

        float accumulatedWeight = Mathf.Max(0f, grade1Weight);
        if (roll < accumulatedWeight)
        {
            return 1;
        }

        accumulatedWeight += Mathf.Max(0f, grade2Weight);
        if (roll < accumulatedWeight)
        {
            return 2;
        }

        accumulatedWeight += Mathf.Max(0f, grade3Weight);
        if (roll < accumulatedWeight)
        {
            return 3;
        }

        accumulatedWeight += Mathf.Max(0f, grade4Weight);
        if (roll < accumulatedWeight)
        {
            return 4;
        }

        return 5;
    }
}
