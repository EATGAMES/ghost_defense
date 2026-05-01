using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SC_BossSpawner : MonoBehaviour
{
    [Tooltip("생성된 보스를 등록하고 종료를 처리할 배틀 매니저입니다.")]
    [FormerlySerializedAs("waveManager")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("생성할 보스 프리팹입니다.")]
    [SerializeField] private GameObject monsterPrefab;

    [Tooltip("보스를 생성할 위치입니다. 비워두면 현재 오브젝트 위치를 사용합니다.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("생성된 보스의 부모 Transform입니다. 비워두면 루트에 생성됩니다.")]
    [SerializeField] private Transform spawnedMonsterParent;

    [Tooltip("전투 시작 시 보스를 자동으로 생성할지 여부입니다.")]
    [SerializeField] private bool spawnBossOnStart = true;

    private GameObject currentSpawnedBossObject;
    private SC_MonsterHealth currentSpawnedBossHealth;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }
    }

    private void OnEnable()
    {
        SC_MonsterHealth.MonsterDied += OnMonsterDied;
    }

    private void Start()
    {
        if (spawnBossOnStart)
        {
            SpawnBoss();
        }
    }

    private void OnDisable()
    {
        SC_MonsterHealth.MonsterDied -= OnMonsterDied;
    }

    public void SpawnBoss()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("SC_BossSpawner: monsterPrefab이 비어 있습니다.");
            return;
        }

        ClearCurrentBossReference();

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        currentSpawnedBossObject = Instantiate(monsterPrefab, spawnPosition, spawnRotation, spawnedMonsterParent);

        if (currentSpawnedBossObject == null)
        {
            return;
        }

        currentSpawnedBossHealth = currentSpawnedBossObject.GetComponent<SC_MonsterHealth>();
        if (currentSpawnedBossHealth == null)
        {
            currentSpawnedBossHealth = currentSpawnedBossObject.GetComponentInChildren<SC_MonsterHealth>();
        }

        if (currentSpawnedBossHealth != null && battleManager != null)
        {
            battleManager.RegisterBoss(currentSpawnedBossHealth);
        }
    }

    private void OnMonsterDied(SC_MonsterHealth deadMonster)
    {
        if (deadMonster == null || deadMonster != currentSpawnedBossHealth)
        {
            return;
        }

        if (battleManager != null)
        {
            battleManager.NotifyBossDefeated(deadMonster);
        }

        ClearCurrentBossReference();
    }

    private void ClearCurrentBossReference()
    {
        if (battleManager != null && currentSpawnedBossHealth != null)
        {
            battleManager.UnregisterBoss(currentSpawnedBossHealth);
        }

        currentSpawnedBossObject = null;
        currentSpawnedBossHealth = null;
    }
}
