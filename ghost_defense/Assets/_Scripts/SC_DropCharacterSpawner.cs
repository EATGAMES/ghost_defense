using UnityEngine;

[DisallowMultipleComponent]
public class SC_DropCharacterSpawner : MonoBehaviour
{
    [Tooltip("드래그해서 떨어뜨릴 드롭 캐릭터 프리팹입니다.")]
    [SerializeField] private GameObject dropCharacterPrefab;

    [Tooltip("대기 캐릭터를 생성할 위치입니다. 비워두면 현재 오브젝트 위치를 사용합니다.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("생성한 캐릭터를 넣어둘 부모 Transform입니다. 비워두면 루트에 생성합니다.")]
    [SerializeField] private Transform spawnedParent;

    [Tooltip("프리팹에 없을 때 SC_DropCharacterController를 자동으로 추가할지 여부입니다.")]
    [SerializeField] private bool addDropControllerIfMissing = true;

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
    [SerializeField] private float respawnDelay = 0.4f;

    private SC_DropCharacterController currentWaitingCharacter;
    private float respawnTimer;
    private bool isRespawnScheduled;
    private void Start()
    {
        TrySpawnWaitingCharacter();
    }

    private void Update()
    {
        if (currentWaitingCharacter == null)
        {
            ScheduleRespawn();
            return;
        }

        if (currentWaitingCharacter.IsDropped)
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
        if (dropCharacterPrefab == null)
        {
            Debug.LogWarning("SC_DropCharacterSpawner: dropCharacterPrefab이 비어 있습니다.", this);
            return;
        }

        int spawnGrade = PickWeightedSpawnGrade();

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject dropObject = Instantiate(dropCharacterPrefab, position, rotation, spawnedParent);
        ApplyCharacterGrade(dropObject, spawnGrade);

        SC_DropCharacterController dropController = dropObject.GetComponent<SC_DropCharacterController>();
        if (dropController == null && addDropControllerIfMissing)
        {
            dropController = dropObject.AddComponent<SC_DropCharacterController>();
        }

        if (dropController == null)
        {
            Debug.LogWarning("SC_DropCharacterSpawner: SC_DropCharacterController 컴포넌트를 찾지 못했습니다.", this);
            Destroy(dropObject);
            return;
        }

        dropController.ResetToWaitingState(position);
        currentWaitingCharacter = dropController;
    }

    private void ApplyCharacterGrade(GameObject targetObject, int grade)
    {
        if (targetObject == null)
        {
            return;
        }

        SC_CharacterPresenter presenter = targetObject.GetComponent<SC_CharacterPresenter>();
        if (presenter == null)
        {
            Debug.LogWarning("SC_DropCharacterSpawner: SC_CharacterPresenter를 찾지 못했습니다.", this);
            return;
        }

        presenter.Configure(grade, true, true);
    }

    private int PickWeightedSpawnGrade()
    {
        float grade1EffectiveWeight = Mathf.Max(0f, grade1Weight);
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
            return 1;
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

}
