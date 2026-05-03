using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SC_LoadSceneButton : MonoBehaviour
{
    [Tooltip("버튼 클릭 시 이동할 씬 이름입니다.")]
    [SerializeField] private string targetSceneName = "SCN_Battle";

    public void OnClickLoadScene()
    {
        if (TryOpenBattleExitPopup())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("이동할 씬 이름이 비어 있습니다.");
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private bool TryOpenBattleExitPopup()
    {
        if (!string.Equals(targetSceneName, "SCN_Lobby"))
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
