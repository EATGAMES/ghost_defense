using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SC_BossSpawner : MonoBehaviour
{
    [Tooltip("씬에 배치된 보스를 등록하고 전투 종료를 전달할 배틀 매니저입니다.")]
    [FormerlySerializedAs("waveManager")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("씬에 미리 배치된 보스 체력 컴포넌트입니다. 비워두면 자신 또는 자식에서 자동으로 찾습니다.")]
    [SerializeField] private SC_MonsterHealth placedBossHealth;

    [Tooltip("클리어 후 계속하기에서 생성할 샌드백 프리팹입니다.")]
    [SerializeField] private GameObject sandbagPrefab;

    [Tooltip("스테이지 순서대로 쓸 몬스터 데이터 목록입니다. 1스테이지는 0번 인덱스를 사용합니다.")]
    [SerializeField] private SO_MonsterData[] stageMonsterDataList;

    private SC_DamagePopupSpawner damagePopupTemplate;
    private Transform cachedSpawnParent;
    private Vector3 cachedSpawnPosition;
    private Quaternion cachedSpawnRotation;
    private GameObject spawnedSandbagObject;
    private SC_MonsterHealth spawnedSandbagHealth;

    private void Awake()
    {
        ResolveReferences();
        CacheSpawnTransform();
        CacheDamagePopupTemplate();
    }

    private void OnEnable()
    {
        SC_MonsterHealth.MonsterDied += OnMonsterDied;

        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (battleManager != null)
        {
            battleManager.StageChanged += OnStageChanged;
        }
    }

    private void Start()
    {
        ApplyStageMonsterDataAndRegisterBoss();
    }

    private void OnDisable()
    {
        SC_MonsterHealth.MonsterDied -= OnMonsterDied;

        if (battleManager != null)
        {
            battleManager.StageChanged -= OnStageChanged;
        }

        if (battleManager != null && placedBossHealth != null)
        {
            battleManager.UnregisterBoss(placedBossHealth);
        }

        if (battleManager != null && spawnedSandbagHealth != null)
        {
            battleManager.UnregisterBoss(spawnedSandbagHealth);
        }
    }

    public void SpawnBoss()
    {
        ResolveReferences();

        if (battleManager == null || placedBossHealth == null)
        {
            Debug.LogWarning("SC_BossSpawner: 씬에 배치된 보스 또는 SC_BattleManager를 찾지 못했습니다.", this);
            return;
        }

        ApplyStageMonsterDataAndRegisterBoss();
    }

    public bool StartPostClearTrainingMode()
    {
        ResolveReferences();
        CacheSpawnTransform();
        CacheDamagePopupTemplate();

        if (sandbagPrefab == null)
        {
            Debug.LogWarning("SC_BossSpawner: Sandbag Prefab 인스펙터 연결이 비어 있습니다.", this);
            return false;
        }

        if (spawnedSandbagHealth == null)
        {
            spawnedSandbagObject = Instantiate(sandbagPrefab, cachedSpawnPosition, cachedSpawnRotation, cachedSpawnParent);
            spawnedSandbagHealth = spawnedSandbagObject.GetComponent<SC_MonsterHealth>();
            if (spawnedSandbagHealth == null)
            {
                spawnedSandbagHealth = spawnedSandbagObject.AddComponent<SC_MonsterHealth>();
            }

            spawnedSandbagHealth.ConfigureAsImmortalTarget();
            EnsureDamagePopupSpawner(spawnedSandbagObject, spawnedSandbagHealth);
        }
        else
        {
            spawnedSandbagHealth.ConfigureAsImmortalTarget();
            if (spawnedSandbagObject != null)
            {
                spawnedSandbagObject.SetActive(true);
            }
        }

        if (battleManager != null && spawnedSandbagHealth != null)
        {
            battleManager.RegisterBoss(spawnedSandbagHealth);
        }

        return spawnedSandbagHealth != null;
    }

    private void OnMonsterDied(SC_MonsterHealth deadMonster)
    {
        if (deadMonster == null || deadMonster != placedBossHealth)
        {
            return;
        }

        if (battleManager != null)
        {
            battleManager.NotifyBossDefeated(deadMonster);
        }
    }

    private void ApplyStageMonsterData()
    {
        if (placedBossHealth == null || stageMonsterDataList == null || stageMonsterDataList.Length <= 0)
        {
            return;
        }

        int stageIndex = Mathf.Clamp(SC_BattleManager.CurrentStage - 1, 0, stageMonsterDataList.Length - 1);
        SO_MonsterData targetMonsterData = stageMonsterDataList[stageIndex];
        if (targetMonsterData == null)
        {
            return;
        }

        placedBossHealth.SetMonsterData(targetMonsterData);
    }

    private void ApplyStageMonsterDataAndRegisterBoss()
    {
        ResolveReferences();
        ApplyStageMonsterData();

        if (battleManager != null && placedBossHealth != null)
        {
            battleManager.RegisterBoss(placedBossHealth);
        }
    }

    private void OnStageChanged(int currentStage, int maxStage)
    {
        ApplyStageMonsterDataAndRegisterBoss();
    }

    private void ResolveReferences()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (placedBossHealth == null)
        {
            placedBossHealth = GetComponent<SC_MonsterHealth>();
        }

        if (placedBossHealth == null)
        {
            placedBossHealth = GetComponentInChildren<SC_MonsterHealth>();
        }
    }

    private void CacheSpawnTransform()
    {
        if (placedBossHealth == null)
        {
            return;
        }

        cachedSpawnParent = placedBossHealth.transform.parent;
        cachedSpawnPosition = placedBossHealth.transform.position;
        cachedSpawnRotation = placedBossHealth.transform.rotation;
    }

    private void CacheDamagePopupTemplate()
    {
        if (damagePopupTemplate == null && placedBossHealth != null)
        {
            damagePopupTemplate = placedBossHealth.GetComponent<SC_DamagePopupSpawner>();
        }
    }

    private void EnsureDamagePopupSpawner(GameObject sandbagObject, SC_MonsterHealth sandbagHealth)
    {
        if (sandbagObject == null || sandbagHealth == null)
        {
            return;
        }

        SC_DamagePopupSpawner popupSpawner = sandbagObject.GetComponent<SC_DamagePopupSpawner>();
        if (popupSpawner == null)
        {
            popupSpawner = sandbagObject.AddComponent<SC_DamagePopupSpawner>();
        }

        popupSpawner.BindMonsterHealth(sandbagHealth);

        if (damagePopupTemplate != null)
        {
            popupSpawner.ConfigurePopup(
                damagePopupTemplate.DamagePopupPrefab,
                damagePopupTemplate.WorldOffset,
                damagePopupTemplate.RandomXOffset,
                damagePopupTemplate.RandomYOffset);
        }
    }
}
