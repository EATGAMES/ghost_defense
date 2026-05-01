using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_MonsterHealth : MonoBehaviour
{
    public static event Action<SC_MonsterHealth> MonsterDied;

    public event Action<float, float> HealthChanged;

    [Tooltip("보스의 기본 최대 체력입니다.")]
    [SerializeField] private float maxHp = 10f;

    [Tooltip("스테이지가 올라갈 때 적용할 체력 증가 비율입니다. 0.3이면 30%입니다.")]
    [SerializeField] private float hpIncreasePerStage = 0.3f;

    [Tooltip("체력이 0 이하가 되면 오브젝트를 자동 파괴할지 여부입니다.")]
    [SerializeField] private bool destroyOnDeath = true;

    private float runtimeMaxHp;
    private float currentHp;
    private bool isDeathNotified;

    public float MaxHp => runtimeMaxHp;
    public float CurrentHp => currentHp;
    public float NormalizedHp => runtimeMaxHp > 0f ? Mathf.Clamp01(currentHp / runtimeMaxHp) : 0f;

    private void Awake()
    {
        int stage = Mathf.Max(1, SC_BattleManager.CurrentStage);
        float increaseRate = Mathf.Max(0f, hpIncreasePerStage);
        float multiplier = Mathf.Pow(1f + increaseRate, stage - 1);

        runtimeMaxHp = Mathf.Max(0f, maxHp * multiplier);
        currentHp = runtimeMaxHp;
        RaiseHealthChanged();
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || currentHp <= 0f)
        {
            return;
        }

        currentHp = Mathf.Max(0f, currentHp - damage);
        RaiseHealthChanged();

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
        RaiseHealthChanged();
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

    private void RaiseHealthChanged()
    {
        HealthChanged?.Invoke(currentHp, runtimeMaxHp);
    }
}
