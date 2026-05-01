using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_MonsterHealth : MonoBehaviour
{
    // 몬스터가 사망했을 때 웨이브 시스템에 알리기 위한 전역 이벤트다.
    public static event Action<SC_MonsterHealth> MonsterDied;

    [Tooltip("몬스터 최대 체력")]
    [SerializeField] private float maxHp = 10f;

    [Tooltip("웨이브별 체력 증가율(0.3 = 30%)")]
    [SerializeField] private float hpIncreasePerWave = 0.3f;

    [Tooltip("체력이 0 이하일 때 오브젝트를 자동 파괴할지 여부")]
    [SerializeField] private bool destroyOnDeath = true;

    private float runtimeMaxHp;
    private float currentHp;
    private bool isDeathNotified;

    public float MaxHp => runtimeMaxHp;
    public float CurrentHp => currentHp;
    public float NormalizedHp => runtimeMaxHp > 0f ? Mathf.Clamp01(currentHp / runtimeMaxHp) : 0f;

    private void Awake()
    {
        int wave = Mathf.Max(1, SC_WaveManager.CurrentWave);
        float increaseRate = Mathf.Max(0f, hpIncreasePerWave);
        float multiplier = Mathf.Pow(1f + increaseRate, wave - 1);

        runtimeMaxHp = Mathf.Max(0f, maxHp * multiplier);
        currentHp = runtimeMaxHp;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || currentHp <= 0f)
        {
            return;
        }

        currentHp = Mathf.Max(0f, currentHp - damage);
        if (currentHp <= 0f)
        {
            NotifyDeathOnce();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }

    public void HealFull()
    {
        currentHp = runtimeMaxHp;
    }

    private void NotifyDeathOnce()
    {
        if (isDeathNotified)
        {
            return;
        }

        isDeathNotified = true;
        MonsterDied?.Invoke(this);
    }
}
