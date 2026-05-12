using UnityEngine;

[DisallowMultipleComponent]
public class SC_StageMapTypeActivator : MonoBehaviour
{
    [Tooltip("맵 타입 변경을 전달받을 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("현재 스테이지의 몬스터 데이터를 조회할 보스 스포너입니다.")]
    [SerializeField] private SC_BossSpawner bossSpawner;

    [Header("맵 타입 오브젝트")]
    [Tooltip("Stage Map Type이 Type1일 때 활성화할 루트 오브젝트입니다.")]
    [SerializeField] private GameObject mapType1Object;

    [Tooltip("Stage Map Type이 Type2일 때 활성화할 루트 오브젝트입니다.")]
    [SerializeField] private GameObject mapType2Object;

    [Tooltip("Stage Map Type이 Type3일 때 활성화할 루트 오브젝트입니다.")]
    [SerializeField] private GameObject mapType3Object;

    private void Awake()
    {
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

        if (battleManager != null)
        {
            battleManager.StageChanged += OnStageChanged;
        }

        RefreshMapTypeObjects(SC_BattleManager.CurrentStage);
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
        RefreshMapTypeObjects(currentStage);
    }

    private void RefreshMapTypeObjects(int stage)
    {
        if (bossSpawner == null)
        {
            bossSpawner = FindAnyObjectByType<SC_BossSpawner>();
        }

        StageMapType stageMapType = bossSpawner != null ? bossSpawner.GetStageMapTypeForStage(stage) : StageMapType.Type1;

        if (mapType1Object != null)
        {
            mapType1Object.SetActive(stageMapType == StageMapType.Type1);
        }

        if (mapType2Object != null)
        {
            mapType2Object.SetActive(stageMapType == StageMapType.Type2);
        }

        if (mapType3Object != null)
        {
            mapType3Object.SetActive(stageMapType == StageMapType.Type3);
        }
    }
}
