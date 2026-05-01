using System.Collections.Generic;
using UnityEngine;

public class SC_BattleCharacterSpawner : MonoBehaviour
{
    [Tooltip("발사 대기 캐릭터로 생성할 공용 프리팹")]
    [SerializeField] private GameObject characterPrefab;

    [Tooltip("발사 대기 위치(비우면 현재 오브젝트 위치 사용)")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("생성 캐릭터 부모 Transform(비우면 루트)")]
    [SerializeField] private Transform spawnedParent;

    [Tooltip("프리팹에 없을 때 드래그 발사 스크립트 자동 추가 여부")]
    [SerializeField] private bool addDragAndShootIfMissing = true;

    [Tooltip("프리팹에 없을 때 자동 발사 스크립트 자동 추가 여부")]
    [SerializeField] private bool addAutoProjectileShooterIfMissing = true;

    [Tooltip("발사 가능한 총 캐릭터 수")]
    [SerializeField] private int availableShootCount = 10;

    [Tooltip("다음 캐릭터 생성 대기 시간(초)")]
    [SerializeField] private float respawnDelay = 0.1f;

    [Tooltip("랜덤 발사에 사용할 캐릭터 데이터 목록(예: 5종)")]
    [SerializeField] private SO_CharacterData[] randomCharacterDataList;

    [Tooltip("다음 발사 순서를 표시할 프리뷰 프리팹(PFB_Preview)")]
    [SerializeField] private GameObject previewPrefab;

    [Tooltip("프리뷰 시작 위치(왼쪽 첫 칸 기준, 비우면 현재 오브젝트 위치 사용)")]
    [SerializeField] private Transform previewStartPoint;

    [Tooltip("프리뷰 오브젝트 부모 Transform(비우면 루트)")]
    [SerializeField] private Transform previewParent;

    [Tooltip("프리뷰 간격(X, Y)")]
    [SerializeField] private Vector2 previewSpacing = new Vector2(0.9f, 0f);

    [Tooltip("프리뷰 크기 배율")]
    [SerializeField] private Vector3 previewScale = new Vector3(0.35f, 0.35f, 1f);

    [Tooltip("프리뷰에 표시할 최대 개수")]
    [SerializeField] private int maxPreviewCount = 10;

    private SC_PlayerDragAndShoot currentWaitingCharacter;
    private float respawnTimer;
    private bool isRespawnScheduled;
    private readonly Queue<SO_CharacterData> shootQueue = new Queue<SO_CharacterData>();
    private readonly List<GameObject> previewInstances = new List<GameObject>();

    private void Start()
    {
        BuildInitialShootQueue();
        RefreshPreview();
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
        ConsumeShotCharacter();
        RefreshPreview();
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
        if (shootQueue.Count <= 0)
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

        GameObject character = Instantiate(characterPrefab, position, rotation, spawnedParent);
        ApplyCharacterData(character, shootQueue.Peek());

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

        SC_CharacterAutoProjectileShooter autoProjectileShooter = character.GetComponent<SC_CharacterAutoProjectileShooter>();
        if (autoProjectileShooter == null && addAutoProjectileShooterIfMissing)
        {
            character.AddComponent<SC_CharacterAutoProjectileShooter>();
        }

        currentWaitingCharacter = shootComponent;
    }

    private void ApplyCharacterData(GameObject character, SO_CharacterData characterData)
    {
        if (character == null)
        {
            return;
        }

        if (characterData == null)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: 캐릭터 데이터가 비어 있습니다.");
            return;
        }

        SC_CharacterPresenter presenter = character.GetComponent<SC_CharacterPresenter>();
        if (presenter != null)
        {
            presenter.SetCharacterData(characterData, true);
            return;
        }

        // 프리뷰 프리팹에 SC_CharacterPresenter가 없을 때 스프라이트만 직접 적용한다.
        SpriteRenderer spriteRenderer = character.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = characterData.CharacterSprite;
            return;
        }

        Debug.LogWarning("SC_BattleCharacterSpawner: SC_CharacterPresenter 또는 SpriteRenderer를 찾지 못했습니다.");
    }

    private void BuildInitialShootQueue()
    {
        shootQueue.Clear();

        if (randomCharacterDataList == null || randomCharacterDataList.Length == 0)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: randomCharacterDataList가 비어 있습니다.");
            return;
        }

        int totalCount = Mathf.Max(0, availableShootCount);
        for (int i = 0; i < totalCount; i++)
        {
            SO_CharacterData randomData = randomCharacterDataList[Random.Range(0, randomCharacterDataList.Length)];
            if (randomData == null)
            {
                Debug.LogWarning("SC_BattleCharacterSpawner: randomCharacterDataList에 비어 있는 데이터가 있습니다.");
                continue;
            }

            shootQueue.Enqueue(randomData);
        }
    }

    private void ConsumeShotCharacter()
    {
        if (shootQueue.Count > 0)
        {
            shootQueue.Dequeue();
        }
    }

    private void RefreshPreview()
    {
        ClearPreviewInstances();

        if (previewPrefab == null)
        {
            return;
        }

        Vector3 basePosition = previewStartPoint != null ? previewStartPoint.position : transform.position;
        int previewCount = Mathf.Min(Mathf.Max(0, maxPreviewCount), Mathf.Max(0, shootQueue.Count - 1));
        if (previewCount <= 0)
        {
            return;
        }

        SO_CharacterData[] queueArray = shootQueue.ToArray();
        for (int i = 0; i < previewCount; i++)
        {
            Vector3 position = basePosition + new Vector3(previewSpacing.x * i, previewSpacing.y * i, 0f);
            GameObject preview = Instantiate(previewPrefab, position, Quaternion.identity, previewParent);
            preview.transform.localScale = previewScale;
            ApplyCharacterData(preview, queueArray[i + 1]);
            previewInstances.Add(preview);
        }
    }

    private void ClearPreviewInstances()
    {
        for (int i = 0; i < previewInstances.Count; i++)
        {
            if (previewInstances[i] != null)
            {
                Destroy(previewInstances[i]);
            }
        }

        previewInstances.Clear();
    }
}
