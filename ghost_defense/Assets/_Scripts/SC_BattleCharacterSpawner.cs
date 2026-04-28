using UnityEngine;

public class SC_BattleCharacterSpawner : MonoBehaviour
{
    [Tooltip("배틀 시작 시 생성할 캐릭터 프리팹")]
    [SerializeField] private GameObject characterPrefab;

    [Tooltip("캐릭터를 생성할 기준 위치")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("생성된 캐릭터의 부모 Transform(비워두면 씬 루트)")]
    [SerializeField] private Transform spawnedParent;

    [Tooltip("생성 즉시 드래그/발사 스크립트를 자동 추가할지 여부")]
    [SerializeField] private bool addDragAndShootIfMissing = true;

    private void Start()
    {
        SpawnCharacter();
    }

    private void SpawnCharacter()
    {
        if (characterPrefab == null)
        {
            Debug.LogWarning("SC_BattleCharacterSpawner: 캐릭터 프리팹이 비어 있습니다.");
            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        Transform parent = spawnedParent;

        GameObject character = Instantiate(characterPrefab, position, rotation, parent);

        if (addDragAndShootIfMissing && character.GetComponent<SC_PlayerDragAndShoot>() == null)
        {
            character.AddComponent<SC_PlayerDragAndShoot>();
        }
    }
}
