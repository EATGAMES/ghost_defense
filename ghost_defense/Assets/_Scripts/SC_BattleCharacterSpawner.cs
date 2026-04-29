using UnityEngine;

public class SC_BattleCharacterSpawner : MonoBehaviour
{
    [Tooltip("발사 대기 캐릭터로 생성할 프리팹")]
    [SerializeField] private GameObject characterPrefab;

    [Tooltip("하단 중앙 발사 대기 위치(비우면 현재 오브젝트 위치 사용)")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("생성된 캐릭터의 부모 Transform(비우면 루트)")]
    [SerializeField] private Transform spawnedParent;

    [Tooltip("프리팹에 없을 때 드래그 발사 스크립트를 자동 추가할지 여부")]
    [SerializeField] private bool addDragAndShootIfMissing = true;

    [Tooltip("발사 가능한 총 캐릭터 수량")]
    [SerializeField] private int availableShootCount = 10;

    [Tooltip("대기 캐릭터 재생성 대기 시간(초)")]
    [SerializeField] private float respawnDelay = 0.1f;

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
        Transform parent = spawnedParent;

        GameObject character = Instantiate(characterPrefab, position, rotation, parent);
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
}
