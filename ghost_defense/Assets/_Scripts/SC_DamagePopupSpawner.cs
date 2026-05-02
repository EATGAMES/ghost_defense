using UnityEngine;

[DisallowMultipleComponent]
public class SC_DamagePopupSpawner : MonoBehaviour
{
    [Tooltip("데미지 이벤트를 보낼 몬스터 체력 스크립트입니다. 비우면 자동 탐색합니다.")]
    [SerializeField] private SC_MonsterHealth monsterHealth;

    [Tooltip("생성할 플로팅 데미지 프리팹입니다.")]
    [SerializeField] private SC_DamagePopup damagePopupPrefab;

    [Tooltip("숫자 생성 위치에 더할 기본 오프셋입니다.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("숫자가 좌우로 퍼질 랜덤 범위입니다.")]
    [SerializeField] private float randomXOffset = 0.2f;

    [Tooltip("숫자가 위아래로 퍼질 랜덤 범위입니다.")]
    [SerializeField] private float randomYOffset = 0.1f;

    private void Awake()
    {
        if (monsterHealth == null)
        {
            monsterHealth = GetComponent<SC_MonsterHealth>();
        }

        if (monsterHealth == null)
        {
            monsterHealth = GetComponentInChildren<SC_MonsterHealth>();
        }
    }

    private void OnEnable()
    {
        if (monsterHealth != null)
        {
            monsterHealth.DamageTaken += OnDamageTaken;
        }
    }

    private void OnDisable()
    {
        if (monsterHealth != null)
        {
            monsterHealth.DamageTaken -= OnDamageTaken;
        }
    }

    private void OnDamageTaken(float damageAmount, Vector3 worldPosition)
    {
        if (damagePopupPrefab == null || damageAmount <= 0f)
        {
            return;
        }

        Vector3 randomOffset = new Vector3(
            Random.Range(-randomXOffset, randomXOffset),
            Random.Range(-randomYOffset, randomYOffset),
            0f);

        SC_DamagePopup popupInstance = Instantiate(damagePopupPrefab, worldPosition + worldOffset + randomOffset, Quaternion.identity);
        popupInstance.Play(damageAmount);
    }
}
