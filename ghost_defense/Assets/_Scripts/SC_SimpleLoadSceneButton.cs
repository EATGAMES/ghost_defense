using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class SC_SimpleLoadSceneButton : MonoBehaviour
{
    [Tooltip("씬 이동 클릭을 받을 버튼입니다. 비워두면 같은 오브젝트의 Button을 자동으로 찾습니다.")]
    [SerializeField] private Button button;

    [Tooltip("버튼 클릭 시 이동할 씬 이름입니다.")]
    [SerializeField] private string targetSceneName = "SCN_Node";

    private bool isListenerRegistered;

    private void Awake()
    {
        EnsureButtonReference();
    }

    private void OnEnable()
    {
        RegisterButtonListener();
    }

    private void OnDisable()
    {
        UnregisterButtonListener();
    }

    private void Reset()
    {
        EnsureButtonReference();
    }

    private void OnValidate()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void EnsureButtonReference()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void RegisterButtonListener()
    {
        EnsureButtonReference();

        if (button == null)
        {
            Debug.LogWarning("씬 이동 버튼 컴포넌트를 찾지 못했습니다.", this);
            return;
        }

        if (isListenerRegistered)
        {
            button.onClick.RemoveListener(OnClickLoadScene);
        }

        button.onClick.AddListener(OnClickLoadScene);
        isListenerRegistered = true;
    }

    private void UnregisterButtonListener()
    {
        if (button != null && isListenerRegistered)
        {
            button.onClick.RemoveListener(OnClickLoadScene);
        }

        isListenerRegistered = false;
    }

    private void OnDestroy()
    {
        UnregisterButtonListener();
    }

    public void OnClickLoadScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("이동할 씬 이름이 비어 있습니다.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogWarning($"씬 '{targetSceneName}'이(가) Build Profiles에 없어 로드할 수 없습니다.", this);
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }
}
