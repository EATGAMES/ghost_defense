using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SC_LoadSceneButton : MonoBehaviour
{
    [Tooltip("버튼 클릭 시 이동할 씬 이름입니다.")]
    [SerializeField] private string targetSceneName = "SCN_Battle";

    [Tooltip("스테이지별 몬스터 데이터 목록입니다. 비워두면 Target Scene Name으로 이동합니다.")]
    [SerializeField] private SO_MonsterData[] stageMonsterDataList;

    [Tooltip("몬스터 데이터의 전투 방향이 UP일 때 이동할 씬 이름입니다.")]
    [SerializeField] private string upBattleSceneName = "SCN_Battle";

    [Tooltip("몬스터 데이터의 전투 방향이 DOWN일 때 이동할 씬 이름입니다.")]
    [SerializeField] private string downBattleSceneName = "SCN_Battle_Drop";

    public void OnClickLoadScene()
    {
        string resolvedSceneName = ResolveTargetSceneName();
        if (TryOpenBattleExitPopup(resolvedSceneName))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(resolvedSceneName))
        {
            Debug.LogWarning("이동할 씬 이름이 비어 있습니다.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(resolvedSceneName))
        {
            Debug.LogWarning($"씬 '{resolvedSceneName}'이(가) Build Profiles에 없어 로드할 수 없습니다. 기본 씬으로 폴백합니다.");

            if (!string.Equals(resolvedSceneName, upBattleSceneName) && Application.CanStreamedLevelBeLoaded(upBattleSceneName))
            {
                SceneManager.LoadScene(upBattleSceneName);
                return;
            }

            return;
        }

        SceneManager.LoadScene(resolvedSceneName);
    }

    private string ResolveTargetSceneName()
    {
        SO_MonsterData monsterData = GetSelectedStageMonsterData();
        if (monsterData == null)
        {
            return targetSceneName;
        }

        return monsterData.StageBattleDirection == StageBattleDirection.DOWN ? downBattleSceneName : upBattleSceneName;
    }

    private SO_MonsterData GetSelectedStageMonsterData()
    {
        if (stageMonsterDataList == null || stageMonsterDataList.Length <= 0)
        {
            return null;
        }

        int selectedStage = SC_SaveDataManager.Instance != null ? SC_SaveDataManager.Instance.SelectedStage : 1;
        int stageIndex = Mathf.Clamp(selectedStage - 1, 0, stageMonsterDataList.Length - 1);
        return stageMonsterDataList[stageIndex];
    }

    private bool TryOpenBattleExitPopup(string resolvedSceneName)
    {
        if (!string.Equals(resolvedSceneName, "SCN_Lobby"))
        {
            return false;
        }

        SC_BattleManager battleManager = FindAnyObjectByType<SC_BattleManager>();
        if (battleManager == null)
        {
            return false;
        }

        SC_ClearPopup clearPopup = FindClearPopupIncludingInactive();
        if (clearPopup == null)
        {
            return false;
        }

        if (clearPopup.IsPopupOpen)
        {
            return true;
        }

        if (battleManager.IsBattleClearedThisSession)
        {
            clearPopup.OpenPopup();
            return true;
        }

        return false;
    }

    private static SC_ClearPopup FindClearPopupIncludingInactive()
    {
        SC_ClearPopup activePopup = FindAnyObjectByType<SC_ClearPopup>();
        if (activePopup != null)
        {
            return activePopup;
        }

        SC_ClearPopup[] allPopups = Resources.FindObjectsOfTypeAll<SC_ClearPopup>();
        for (int i = 0; i < allPopups.Length; i++)
        {
            SC_ClearPopup popup = allPopups[i];
            if (popup == null || popup.hideFlags != HideFlags.None)
            {
                continue;
            }

            if (!popup.gameObject.scene.IsValid())
            {
                continue;
            }

            return popup;
        }

        return null;
    }
}
