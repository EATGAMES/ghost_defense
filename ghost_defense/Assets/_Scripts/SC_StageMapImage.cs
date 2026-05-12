using UnityEngine;

[DisallowMultipleComponent]
public class SC_StageMapImage : MonoBehaviour
{
    [Tooltip("스테이지 맵 이미지를 표시할 SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer stageMapSpriteRenderer;

    [Tooltip("맵 이미지가 없을 때 대신 표시할 기본 스프라이트입니다.")]
    [SerializeField] private Sprite fallbackStageMapSprite;

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
            RefreshStageMapImage(SC_BattleManager.CurrentStage);
            return;
        }

        battleManager.StageChanged += OnStageChanged;
        RefreshStageMapImage(SC_BattleManager.CurrentStage);
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
        RefreshStageMapImage(currentStage);
    }

    private void RefreshStageMapImage(int stage)
    {
        if (stageMapSpriteRenderer == null)
        {
            return;
        }

        if (bossSpawner == null)
        {
            bossSpawner = FindAnyObjectByType<SC_BossSpawner>();
        }

        SO_MonsterData monsterData = bossSpawner != null ? bossSpawner.GetMonsterDataForStage(stage) : null;
        Sprite stageMapSprite = monsterData != null ? monsterData.StageMapSprite : fallbackStageMapSprite;
        stageMapSpriteRenderer.sprite = stageMapSprite;
        stageMapSpriteRenderer.enabled = stageMapSprite != null;
    }
}
