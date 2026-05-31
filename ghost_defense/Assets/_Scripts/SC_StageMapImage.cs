using System;
using UnityEngine;

[Serializable]
public class StageMapStyleEntry
{
    [Tooltip("CSV에서 넘어오는 맵 스타일 ID입니다. 예: Up_001, Down_001")]
    [SerializeField] private string mapStyle;

    [Tooltip("이 맵 스타일일 때 표시할 스테이지 맵 이미지입니다.")]
    [SerializeField] private Sprite stageMapSprite;

    [Tooltip("이 맵 스타일일 때 활성화할 맵 타입 루트 오브젝트입니다.")]
    [SerializeField] private GameObject mapTypeObject;

    public string MapStyle => string.IsNullOrWhiteSpace(mapStyle) ? string.Empty : mapStyle.Trim();
    public Sprite StageMapSprite => stageMapSprite;
    public GameObject MapTypeObject => mapTypeObject;
}

[DisallowMultipleComponent]
public class SC_StageMapImage : MonoBehaviour
{
    [Tooltip("스테이지 맵 이미지를 표시할 SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer stageMapSpriteRenderer;

    [Tooltip("맵 이미지가 없을 때 대신 표시할 기본 스프라이트입니다.")]
    [SerializeField] private Sprite fallbackStageMapSprite;

    [Tooltip("맵 스타일 ID별 이미지와 맵 타입 오브젝트 목록입니다.")]
    [SerializeField] private StageMapStyleEntry[] mapStyles = Array.Empty<StageMapStyleEntry>();

    [Tooltip("현재 스테이지 변화를 전달할 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("현재 스테이지의 몬스터 데이터를 조회할 보스 스포너입니다.")]
    [SerializeField] private SC_BossSpawner bossSpawner;

    private void Awake()
    {
        if (stageMapSpriteRenderer == null)
        {
            stageMapSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (bossSpawner == null)
        {
            bossSpawner = FindAnyObjectByType<SC_BossSpawner>();
        }
    }

    private void OnEnable()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        if (battleManager == null)
        {
            RefreshStageMap(SC_BattleManager.CurrentStage);
            return;
        }

        battleManager.StageChanged += OnStageChanged;
        RefreshStageMap(SC_BattleManager.CurrentStage);
    }

    private void OnDisable()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.StageChanged -= OnStageChanged;
    }

    private void OnStageChanged(int currentStage, int maxStage)
    {
        RefreshStageMap(currentStage);
    }

    private void RefreshStageMap(int stage)
    {
        if (stageMapSpriteRenderer == null)
        {
            return;
        }

        if (bossSpawner == null)
        {
            bossSpawner = FindAnyObjectByType<SC_BossSpawner>();
        }

        StageMapStyleEntry mapStyleEntry = ResolveNodeMapStyleEntry();
        Sprite stageMapSprite = mapStyleEntry != null ? mapStyleEntry.StageMapSprite : null;
        if (stageMapSprite == null)
        {
            stageMapSprite = bossSpawner != null ? bossSpawner.GetStageMapSpriteForStage(stage) : null;
        }

        if (stageMapSprite == null)
        {
            stageMapSprite = fallbackStageMapSprite;
        }

        stageMapSpriteRenderer.sprite = stageMapSprite;
        stageMapSpriteRenderer.enabled = stageMapSprite != null;
        RefreshMapTypeObject(mapStyleEntry);
    }

    private StageMapStyleEntry ResolveNodeMapStyleEntry()
    {
        if (!SC_NodeRunContext.HasActiveNode || string.IsNullOrWhiteSpace(SC_NodeRunContext.CurrentMapStyle) || mapStyles == null)
        {
            return null;
        }

        string targetMapStyle = SC_NodeRunContext.CurrentMapStyle.Trim();
        for (int i = 0; i < mapStyles.Length; i++)
        {
            StageMapStyleEntry entry = mapStyles[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.MapStyle))
            {
                continue;
            }

            if (string.Equals(entry.MapStyle, targetMapStyle, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }

    private void RefreshMapTypeObject(StageMapStyleEntry activeEntry)
    {
        if (mapStyles == null)
        {
            return;
        }

        for (int i = 0; i < mapStyles.Length; i++)
        {
            GameObject mapTypeObject = mapStyles[i] != null ? mapStyles[i].MapTypeObject : null;
            if (mapTypeObject != null)
            {
                mapTypeObject.SetActive(mapStyles[i] == activeEntry);
            }
        }
    }
}
