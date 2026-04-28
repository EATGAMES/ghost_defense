using UnityEngine;
using UnityEngine.SceneManagement;

public class SC_LoadSceneButton : MonoBehaviour
{
    [Tooltip("버튼 클릭 시 이동할 씬 이름")]
    [SerializeField] private string targetSceneName = "SCN_Battle";

    public void OnClickLoadScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("이동할 씬 이름이 비어 있습니다.");
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }
}
