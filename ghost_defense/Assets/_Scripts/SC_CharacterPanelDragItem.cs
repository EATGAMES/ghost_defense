using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class SC_CharacterPanelDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("드래그 순서를 관리할 캐릭터 패널 정렬 그룹입니다.")]
    [SerializeField] private SC_CharacterPanelReorderGroup reorderGroup;

    [Tooltip("드래그할 때 실제로 움직일 RectTransform입니다. 비워 두면 현재 오브젝트를 사용합니다.")]
    [SerializeField] private RectTransform dragTarget;

    private CanvasGroup canvasGroup;
    private RectTransform cachedRectTransform;
    private SC_CharacterPanel characterPanel;

    public RectTransform DragTarget => dragTarget != null ? dragTarget : cachedRectTransform;
    public SC_CharacterPanel CharacterPanel => characterPanel;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Setup(SC_CharacterPanelReorderGroup group, SC_CharacterPanel panel)
    {
        ResolveReferences();
        reorderGroup = group;
        characterPanel = panel;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ResolveReferences();
        if (reorderGroup == null)
        {
            reorderGroup = GetComponentInParent<SC_CharacterPanelReorderGroup>();
        }

        if (reorderGroup == null)
        {
            return;
        }

        reorderGroup.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (reorderGroup == null)
        {
            return;
        }

        reorderGroup.Drag(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (reorderGroup == null)
        {
            return;
        }

        reorderGroup.EndDrag(this, eventData);
    }

    public void SetDraggingVisual(bool isDragging)
    {
        ResolveReferences();
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.blocksRaycasts = !isDragging;
        canvasGroup.alpha = 1f;
    }

    private void ResolveReferences()
    {
        if (cachedRectTransform == null)
        {
            cachedRectTransform = transform as RectTransform;
        }

        if (dragTarget == null)
        {
            dragTarget = cachedRectTransform;
        }

        if (characterPanel == null)
        {
            characterPanel = GetComponent<SC_CharacterPanel>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
}
