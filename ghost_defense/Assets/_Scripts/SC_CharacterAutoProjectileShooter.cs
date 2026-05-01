using UnityEngine;

[DisallowMultipleComponent]
public class SC_CharacterAutoProjectileShooter : MonoBehaviour
{
    [Tooltip("첫 충돌 이후에만 자동 발사를 시작할지 여부")]
    [SerializeField] private bool requireFirstCollisionBeforeAutoShoot = true;

    [Tooltip("추적 대상 몬스터 태그")]
    [SerializeField] private string monsterTag = "Monster";

    [Tooltip("발사체 생성 위치 오프셋")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Tooltip("발사체 부모 Transform(비우면 루트)")]
    [SerializeField] private Transform projectileParent;

    private SC_PlayerDragAndShoot dragAndShoot;
    private SC_CharacterPresenter presenter;
    private bool hasCollidedAfterShot;
    private float spawnTimer;

    private void Awake()
    {
        dragAndShoot = GetComponent<SC_PlayerDragAndShoot>();
        presenter = GetComponent<SC_CharacterPresenter>();
        hasCollidedAfterShot = !requireFirstCollisionBeforeAutoShoot;
        spawnTimer = 0f;
    }

    private void Update()
    {
        SO_CharacterData data = presenter != null ? presenter.CharacterData : null;
        if (!CanAutoShoot(data))
        {
            return;
        }

        if (!hasCollidedAfterShot)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        SpawnProjectile(data);
        spawnTimer = Mathf.Max(0.01f, data.AutoProjectileSpawnDelay);
    }

    private bool CanAutoShoot(SO_CharacterData data)
    {
        if (dragAndShoot == null || !dragAndShoot.IsShot)
        {
            return false;
        }

        if (data == null)
        {
            return false;
        }

        if (data.AutoProjectilePrefab == null)
        {
            return false;
        }

        if (data.AutoProjectileSpawnDelay <= 0f)
        {
            return false;
        }

        if (data.AutoProjectileSpeed <= 0f)
        {
            return false;
        }

        return true;
    }

    private void SpawnProjectile(SO_CharacterData data)
    {
        if (!HasAnyMonsterTarget())
        {
            return;
        }

        Vector3 spawnPosition = transform.position + spawnOffset;
        GameObject projectile = Instantiate(data.AutoProjectilePrefab, spawnPosition, Quaternion.identity, projectileParent);

        SC_AutoHomingProjectile homingProjectile = projectile.GetComponent<SC_AutoHomingProjectile>();
        if (homingProjectile != null)
        {
            float finalDamage = CalculateProjectileDamage(data);
            homingProjectile.Initialize(data.AutoProjectileSpeed, monsterTag, finalDamage);
        }
    }

    private float CalculateProjectileDamage(SO_CharacterData data)
    {
        if (data == null)
        {
            return 0f;
        }

        float damagePercent = Mathf.Max(0f, data.AutoProjectileDamagePercent);
        return Mathf.Max(0f, data.AutoProjectileDamage * damagePercent);
    }

    private bool HasAnyMonsterTarget()
    {
        if (string.IsNullOrEmpty(monsterTag))
        {
            return false;
        }

        GameObject[] targets = GameObject.FindGameObjectsWithTag(monsterTag);
        return targets != null && targets.Length > 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!requireFirstCollisionBeforeAutoShoot)
        {
            return;
        }

        if (dragAndShoot == null || !dragAndShoot.IsShot)
        {
            return;
        }

        if (hasCollidedAfterShot)
        {
            return;
        }

        hasCollidedAfterShot = true;
        SO_CharacterData data = presenter != null ? presenter.CharacterData : null;
        spawnTimer = data != null ? Mathf.Max(0.01f, data.AutoProjectileSpawnDelay) : 0f;
    }

    public void SetRequireFirstCollisionBeforeAutoShoot(bool required)
    {
        requireFirstCollisionBeforeAutoShoot = required;
        if (!required)
        {
            hasCollidedAfterShot = true;

            SO_CharacterData data = presenter != null ? presenter.CharacterData : null;
            spawnTimer = data != null ? Mathf.Max(0.01f, data.AutoProjectileSpawnDelay) : 0f;
        }
        else
        {
            hasCollidedAfterShot = false;
            spawnTimer = 0f;
        }
    }
}
