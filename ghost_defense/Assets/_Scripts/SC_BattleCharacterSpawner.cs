using UnityEngine;

public class SC_BattleCharacterSpawner : MonoBehaviour
{
    [Tooltip("발사 대기 캐릭터로 생성할 공용 프리팹")]
    [SerializeField] private GameObject characterPrefab;

    [Tooltip("발사 대기 위치(비우면 현재 오브젝트 위치 사용)")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("생성 캐릭터 부모 Transform(비우면 루트)")]
    [SerializeField] private Transform spawnedParent;

    [Tooltip("프리팹에 없을 때 드래그 발사 스크립트 자동 추가 여부")]
    [SerializeField] private bool addDragAndShootIfMissing = true;

    [Tooltip("발사 가능한 총 캐릭터 수")]
    [SerializeField] private int availableShootCount = 10;

    [Tooltip("다음 캐릭터 생성 대기 시간(초)")]
    [SerializeField] private float respawnDelay = 0.1f;

    [Tooltip("랜덤 발사에 사용할 캐릭터 데이터 목록(예: 5종)")]
    [SerializeField] private SO_CharacterData[] randomCharacterDataList;

    private SC_PlayerDragAndShoot currentWaitingCharacter;
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
            return;
        }

        if (!currentWaitingCharacter.IsShot)
        {
            return;
        }

        currentWaitingCharacter = null;
        ScheduleRespawn();
    }

    private void ScheduleRespawn()
    {
        if (isRespawnScheduled)
        {
            return;
        }

        isRespawnScheduled = true;
        respawnTimer = respawnDelay;
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

    private void TrySpawnWaitingCharacter()
    {
        if (availableShootCount <= 0)
        {
            return;
        }

        if (characterPrefab == null)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: characterPrefab이 비어 있습니다.");
            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject character = Instantiate(characterPrefab, position, rotation, spawnedParent);
        ApplyRandomCharacterData(character);

        SC_PlayerDragAndShoot shootComponent = character.GetComponent<SC_PlayerDragAndShoot>();
        if (shootComponent == null && addDragAndShootIfMissing)
        {
            shootComponent = character.AddComponent<SC_PlayerDragAndShoot>();
        }

        if (shootComponent == null)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: SC_PlayerDragAndShoot 컴포넌트를 찾지 못했습니다.");
            Destroy(character);
            return;
        }

        currentWaitingCharacter = shootComponent;
        availableShootCount--;
    }

    private void ApplyRandomCharacterData(GameObject character)
    {
        if (character == null)
        {
            return;
        }

        if (randomCharacterDataList == null || randomCharacterDataList.Length == 0)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: randomCharacterDataList가 비어 있습니다.");
            return;
        }

        SC_CharacterPresenter presenter = character.GetComponent<SC_CharacterPresenter>();
        if (presenter == null)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: SC_CharacterPresenter를 찾지 못했습니다.");
            return;
        }

        SO_CharacterData randomData = randomCharacterDataList[Random.Range(0, randomCharacterDataList.Length)];
        if (randomData == null)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: randomCharacterDataList에 비어 있는 데이터가 있습니다.");
            return;
        }

        presenter.SetCharacterData(randomData, true);
    }
}
