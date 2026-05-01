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

    [Tooltip("발사 후보로 사용할 캐릭터 데이터 목록")]
    [SerializeField] private SO_CharacterData[] spawnableCharacterDataList;

    [Tooltip("랜덤 생성 최소 단계")]
    [SerializeField] private CharacterGrade minSpawnGrade = CharacterGrade.Grade1;

    [Tooltip("랜덤 생성 최대 단계")]
    [SerializeField] private CharacterGrade maxSpawnGrade = CharacterGrade.Grade5;

    [Tooltip("1단계 등장 확률 가중치(%)")]
    [SerializeField] private float grade1Weight = 25f;

    [Tooltip("2단계 등장 확률 가중치(%)")]
    [SerializeField] private float grade2Weight = 25f;

    [Tooltip("3단계 등장 확률 가중치(%)")]
    [SerializeField] private float grade3Weight = 20f;

    [Tooltip("4단계 등장 확률 가중치(%)")]
    [SerializeField] private float grade4Weight = 18f;

    [Tooltip("5단계 등장 확률 가중치(%)")]
    [SerializeField] private float grade5Weight = 12f;

    [Tooltip("다음 캐릭터 생성 대기 시간(초)")]
    [SerializeField] private float respawnDelay = 0.1f;

    private SC_PlayerDragAndShoot currentWaitingCharacter;
    private float respawnTimer;
    private bool isRespawnScheduled;
    private readonly List<SO_CharacterData> cachedSpawnCandidates = new List<SO_CharacterData>();

    private void Start()
    {
        RebuildSpawnCandidates();
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
        ScheduleRespawn();
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

    private void ScheduleRespawn()
    {
        if (isRespawnScheduled)
        {
            return;
        }

        isRespawnScheduled = true;
        respawnTimer = Mathf.Max(0f, respawnDelay);
    }

    private void TrySpawnWaitingCharacter()
    {
        if (characterPrefab == null)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: characterPrefab이 비어 있습니다.");
            return;
        }

        SO_CharacterData spawnData = GetRandomSpawnData();
        if (spawnData == null)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: 현재 설정으로 생성 가능한 캐릭터 데이터가 없습니다.");
            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject character = Instantiate(characterPrefab, position, rotation, spawnedParent);
        ApplyCharacterData(character, spawnData);

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

        SpriteRenderer spriteRenderer = character.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = characterData.CharacterSprite;
            return;
        }

        Debug.LogWarning("SC_BattleCharacterSpawner: SC_CharacterPresenter 또는 SpriteRenderer를 찾지 못했습니다.");
    }

    private SO_CharacterData GetRandomSpawnData()
    {
        RebuildSpawnCandidates();
        if (cachedSpawnCandidates.Count <= 0)
        {
            return null;
        }

        CharacterGrade selectedGrade = PickWeightedGrade();
        List<SO_CharacterData> gradeCandidates = new List<SO_CharacterData>();
        for (int i = 0; i < cachedSpawnCandidates.Count; i++)
        {
            SO_CharacterData candidate = cachedSpawnCandidates[i];
            if (candidate != null && candidate.CharacterGrade == selectedGrade)
            {
                gradeCandidates.Add(candidate);
            }
        }

        if (gradeCandidates.Count > 0)
        {
            int gradeRandomIndex = Random.Range(0, gradeCandidates.Count);
            return gradeCandidates[gradeRandomIndex];
        }

        int fallbackIndex = Random.Range(0, cachedSpawnCandidates.Count);
        return cachedSpawnCandidates[fallbackIndex];
    }

    private CharacterGrade PickWeightedGrade()
    {
        float minGrade = Mathf.Min((int)minSpawnGrade, (int)maxSpawnGrade);
        float maxGrade = Mathf.Max((int)minSpawnGrade, (int)maxSpawnGrade);

        float w1 = IsGradeEnabled(CharacterGrade.Grade1, minGrade, maxGrade) ? Mathf.Max(0f, grade1Weight) : 0f;
        float w2 = IsGradeEnabled(CharacterGrade.Grade2, minGrade, maxGrade) ? Mathf.Max(0f, grade2Weight) : 0f;
        float w3 = IsGradeEnabled(CharacterGrade.Grade3, minGrade, maxGrade) ? Mathf.Max(0f, grade3Weight) : 0f;
        float w4 = IsGradeEnabled(CharacterGrade.Grade4, minGrade, maxGrade) ? Mathf.Max(0f, grade4Weight) : 0f;
        float w5 = IsGradeEnabled(CharacterGrade.Grade5, minGrade, maxGrade) ? Mathf.Max(0f, grade5Weight) : 0f;

        float total = w1 + w2 + w3 + w4 + w5;
        if (total <= 0f)
        {
            return CharacterGrade.Grade1;
        }

        float roll = Random.Range(0f, total);
        if (roll < w1)
        {
            return CharacterGrade.Grade1;
        }

        roll -= w1;
        if (roll < w2)
        {
            return CharacterGrade.Grade2;
        }

        roll -= w2;
        if (roll < w3)
        {
            return CharacterGrade.Grade3;
        }

        roll -= w3;
        if (roll < w4)
        {
            return CharacterGrade.Grade4;
        }

        return CharacterGrade.Grade5;
    }

    private static bool IsGradeEnabled(CharacterGrade grade, float minGrade, float maxGrade)
    {
        float gradeValue = (int)grade;
        return gradeValue >= minGrade && gradeValue <= maxGrade;
    }

    private void RebuildSpawnCandidates()
    {
        cachedSpawnCandidates.Clear();
        if (spawnableCharacterDataList == null || spawnableCharacterDataList.Length == 0)
        {
            return;
        }

        int minGradeValue = Mathf.Min((int)minSpawnGrade, (int)maxSpawnGrade);
        int maxGradeValue = Mathf.Max((int)minSpawnGrade, (int)maxSpawnGrade);

        for (int i = 0; i < spawnableCharacterDataList.Length; i++)
        {
            SO_CharacterData candidate = spawnableCharacterDataList[i];
            if (candidate == null)
            {
                continue;
            }

            int gradeValue = candidate.GradeValue;
            if (gradeValue < minGradeValue || gradeValue > maxGradeValue)
            {
                continue;
            }

            cachedSpawnCandidates.Add(candidate);
        }
    }
}
