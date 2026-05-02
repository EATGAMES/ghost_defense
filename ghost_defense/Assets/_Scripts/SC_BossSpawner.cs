using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class SC_BossSpawner : MonoBehaviour
{
    [Tooltip("씬에 배치한 보스를 등록하고 전투 종료를 전달할 배틀 매니저입니다.")]
    [FormerlySerializedAs("waveManager")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("씬에 미리 배치한 보스 체력 컴포넌트입니다. 비워두면 자신 또는 자식에서 자동으로 찾습니다.")]
    [SerializeField] private SC_MonsterHealth placedBossHealth;

    private void Awake()
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

    private void OnEnable()
    {
        SC_MonsterHealth.MonsterDied += OnMonsterDied;

        if (battleManager != null && placedBossHealth != null)
        {
            battleManager.RegisterBoss(placedBossHealth);
        }
    }

    private void OnDisable()
    {
        SC_MonsterHealth.MonsterDied -= OnMonsterDied;

        if (battleManager != null && placedBossHealth != null)
        {
            battleManager.UnregisterBoss(placedBossHealth);
        }
    }

    public void SpawnBoss()
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

        if (battleManager == null || placedBossHealth == null)
        {
            Debug.LogWarning("SC_BossSpawner: 씬에 배치한 보스 또는 SC_BattleManager를 찾지 못했습니다.", this);
            return;
        }

        battleManager.RegisterBoss(placedBossHealth);
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
}
