using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_MonsterHealth : MonoBehaviour
{
    public static event Action<SC_MonsterHealth> MonsterDied;

    public event Action<float, float> HealthChanged;
    public event Action<float, Vector3> DamageTaken;

    [Tooltip("보스가 사용할 몬스터 데이터입니다.")]
    [SerializeField] private SO_MonsterData monsterData;

    [Tooltip("체력이 0 이하가 되면 오브젝트를 자동으로 제거할지 여부입니다.")]
    [SerializeField] private bool destroyOnDeath = true;

    private float runtimeMaxHp;
    private float currentHp;
    private bool isDeathNotified;

    public float MaxHp => runtimeMaxHp;
    public float CurrentHp => currentHp;
    public float NormalizedHp => runtimeMaxHp > 0f ? Mathf.Clamp01(currentHp / runtimeMaxHp) : 0f;
    public SO_MonsterData MonsterData => monsterData;
    public MonsterWeaknessDamageType WeaknessDamageType => monsterData != null ? monsterData.WeaknessDamageType : MonsterWeaknessDamageType.None;
    public MonsterWeaknessAttackStyle WeaknessAttackStyle => monsterData != null ? monsterData.WeaknessAttackStyle : MonsterWeaknessAttackStyle.None;

    private void Awake()
    {
        ApplyMonsterData(monsterData);
    }

    public void SetMonsterData(SO_MonsterData newMonsterData)
    {
        ApplyMonsterData(newMonsterData);
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || currentHp <= 0f)
        {
            return;
        }

        float appliedDamage = Mathf.Min(currentHp, Mathf.Max(0f, damage));
        currentHp = Mathf.Max(0f, currentHp - appliedDamage);
        RaiseHealthChanged();
        DamageTaken?.Invoke(appliedDamage, transform.position);

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

    private void ApplyMonsterData(SO_MonsterData newMonsterData)
    {
        monsterData = newMonsterData;
        runtimeMaxHp = monsterData != null ? Mathf.Max(0f, monsterData.MaxHp) : 0f;
        currentHp = runtimeMaxHp;
        isDeathNotified = false;
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
