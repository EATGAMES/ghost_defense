using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_DebugOverlay : MonoBehaviour
{
    private static SC_DebugOverlay instance;

    private Canvas overlayCanvas;
    private Image backgroundImage;
    private TMP_Text debugText;
    private bool isVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstanceOnLoad()
    {
        if (instance != null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("OBJ_DebugOverlay");
        overlayObject.AddComponent<SC_DebugOverlay>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateOverlayUI();
        SetVisible(false);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.f3Key.wasPressedThisFrame)
        {
            return;
        }

        SetVisible(!isVisible);
    }

    private void LateUpdate()
    {
        if (!isVisible)
        {
            return;
        }

        RefreshDebugText();
    }

    private void CreateOverlayUI()
    {
        overlayCanvas = gameObject.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = gameObject.AddComponent<Canvas>();
        }

        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 5000;

        if (gameObject.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900f, 1960f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        GameObject backgroundObject = new GameObject("OBJ_DebugOverlayBackground");
        backgroundObject.transform.SetParent(transform, false);
        backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.82f);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("TXT_DebugOverlay");
        textObject.transform.SetParent(backgroundObject.transform, false);
        debugText = textObject.AddComponent<TextMeshProUGUI>();
        debugText.fontSize = 28f;
        debugText.color = Color.white;
        debugText.alignment = TextAlignmentOptions.TopLeft;
        debugText.enableWordWrapping = false;
        debugText.overflowMode = TextOverflowModes.Overflow;
        debugText.text = string.Empty;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(32f, 32f);
        textRect.offsetMax = new Vector2(-32f, -32f);
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(visible);
        }

        if (visible)
        {
            RefreshDebugText();
        }
    }

    private void RefreshDebugText()
    {
        if (debugText == null)
        {
            return;
        }

        if (SC_SaveDataManager.Instance == null)
        {
            debugText.text = "SaveDataManager: null";
            return;
        }

        debugText.text = SC_SaveDataManager.Instance.BuildDebugSummary();
    }
}
