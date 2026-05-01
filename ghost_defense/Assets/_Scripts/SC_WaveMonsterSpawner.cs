using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_WaveMonsterSpawner : MonoBehaviour
{
    [Tooltip("웨이브 흐름을 제어할 매니저")]
    [SerializeField] private SC_WaveManager waveManager;

    [Tooltip("웨이브마다 생성할 몬스터 프리팹")]
    [SerializeField] private GameObject monsterPrefab;

    [Tooltip("몬스터 생성 위치 목록(비어 있으면 이 오브젝트 위치 사용)")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("생성된 몬스터 부모 Transform(비어 있으면 루트)")]
    [SerializeField] private Transform spawnedMonsterParent;

    [Tooltip("웨이브당 몬스터 생성 수")]
    [SerializeField] private int monstersPerWave = 1;

    private readonly HashSet<int> aliveMonsterHealthIds = new HashSet<int>();
    private int activeWave;

    private void Awake()
    {
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<SC_WaveManager>();
        }
    }

    private void OnEnable()
    {
        if (waveManager != null)
        {
            waveManager.WaveStarted += OnWaveStarted;
        }

        SC_MonsterHealth.MonsterDied += OnMonsterDied;
    }

    private void OnDisable()
    {
        if (waveManager != null)
        {
            waveManager.WaveStarted -= OnWaveStarted;
        }

        SC_MonsterHealth.MonsterDied -= OnMonsterDied;
    }

    private void OnWaveStarted(int wave)
    {
        activeWave = wave;
        SpawnWaveMonsters();
    }

    private void SpawnWaveMonsters()
    {
        aliveMonsterHealthIds.Clear();

        if (monsterPrefab == null)
        {
            Debug.LogWarning("SC_WaveMonsterSpawner: monsterPrefab이 비어 있습니다.");
            return;
        }

        int spawnCount = Mathf.Max(1, monstersPerWave);
        for (int i = 0; i < spawnCount; i++)
        {
            Transform point = null;
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                point = spawnPoints[i % spawnPoints.Length];
            }

            Vector3 spawnPosition = point != null ? point.position : transform.position;
            Quaternion spawnRotation = point != null ? point.rotation : Quaternion.identity;
            GameObject spawnedMonster = Instantiate(monsterPrefab, spawnPosition, spawnRotation, spawnedMonsterParent);

            if (spawnedMonster == null)
            {
                continue;
            }

            SC_MonsterHealth monsterHealth = spawnedMonster.GetComponent<SC_MonsterHealth>();
            if (monsterHealth == null)
            {
                monsterHealth = spawnedMonster.GetComponentInChildren<SC_MonsterHealth>();
            }

            if (monsterHealth != null)
            {
                aliveMonsterHealthIds.Add(monsterHealth.GetInstanceID());
            }
        }

        if (aliveMonsterHealthIds.Count <= 0 && waveManager != null)
        {
            waveManager.NotifyWaveCleared(activeWave);
        }
    }

    private void OnMonsterDied(SC_MonsterHealth deadMonster)
    {
        if (deadMonster == null)
        {
            return;
        }

        int healthId = deadMonster.GetInstanceID();
        if (!aliveMonsterHealthIds.Remove(healthId))
        {
            return;
        }

        if (aliveMonsterHealthIds.Count <= 0 && waveManager != null)
        {
            waveManager.NotifyWaveCleared(activeWave);
        }
    }
}
