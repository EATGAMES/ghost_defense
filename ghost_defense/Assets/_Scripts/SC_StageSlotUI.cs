using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class SC_StageSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Tooltip("슬롯에 표시할 아이콘 이미지입니다.")]
    [SerializeField] private Image iconImage;

    [Tooltip("드래그할 때 실제로 움직일 RectTransform입니다.")]
    [SerializeField] private RectTransform dragTarget;

    private SC_StageRosterEditor rosterEditor;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>(16);
    private RectTransform cachedRectTransform;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalAnchoredPosition;
    private int originalSiblingIndex;
    private int slotIndex = -1;
    private bool isDragging;

    public int SlotIndex => slotIndex;

    private void Awake()
    {
        cachedRectTransform = transform as RectTransform;
        if (dragTarget == null)
        {
            dragTarget = cachedRectTransform;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Setup(SC_StageRosterEditor editor, int index)
    {
        rosterEditor = editor;
        slotIndex = index;
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            rootCanvas = rootCanvas.rootCanvas;
        }
    }

    public void SetIcon(Sprite iconSprite)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = iconSprite;
        iconImage.enabled = iconSprite != null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rosterEditor == null || !rosterEditor.CanDragSlot(slotIndex) || dragTarget == null || rootCanvas == null)
        {
            return;
        }

        isDragging = true;
        originalParent = dragTarget.parent;
        originalSiblingIndex = dragTarget.GetSiblingIndex();
        originalAnchoredPosition = dragTarget.anchoredPosition;

        dragTarget.SetParent(rootCanvas.transform, true);
        dragTarget.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.85f;
        UpdateDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        UpdateDragPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        TrySwapWithHoveredSlot(eventData);
        RestoreDragTarget();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (rosterEditor == null)
        {
            return;
        }

        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null)
        {
            return;
        }

        SC_StageSlotUI draggedSlot = draggedObject.GetComponentInParent<SC_StageSlotUI>();
        if (draggedSlot == null || draggedSlot == this)
        {
            return;
        }

        rosterEditor.SwapSlots(draggedSlot.SlotIndex, slotIndex);
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        RectTransform canvasRectTransform = rootCanvas.transform as RectTransform;
        if (canvasRectTransform == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            dragTarget.localPosition = localPoint;
        }
    }

    private void RestoreDragTarget()
    {
        dragTarget.SetParent(originalParent, true);
        dragTarget.SetSiblingIndex(originalSiblingIndex);
        dragTarget.anchoredPosition = originalAnchoredPosition;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        isDragging = false;
    }

    private void TrySwapWithHoveredSlot(PointerEventData eventData)
    {
        if (rosterEditor == null || eventData == null || EventSystem.current == null)
        {
            return;
        }

        raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            GameObject hoveredObject = raycastResults[i].gameObject;
            if (hoveredObject == null)
            {
                continue;
            }

            SC_StageSlotUI hoveredSlot = hoveredObject.GetComponentInParent<SC_StageSlotUI>();
            if (hoveredSlot == null || hoveredSlot == this)
            {
                continue;
            }

            rosterEditor.SwapSlots(slotIndex, hoveredSlot.SlotIndex);
            return;
        }
    }
}
