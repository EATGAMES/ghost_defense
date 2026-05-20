using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_CharacterSlotDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Tooltip("슬롯에 캐릭터 이미지를 표시할 Image입니다.")]
    [SerializeField] private Image iconImage;

    [Tooltip("드래그할 때 실제로 움직일 RectTransform입니다. 비워 두면 아이콘 이미지를 사용합니다.")]
    [SerializeField] private RectTransform dragTarget;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>(16);
    private SC_CharacterSlotReorderGroup reorderGroup;
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
        ResolveReferences();
    }

    public void Setup(SC_CharacterSlotReorderGroup group, int index)
    {
        ResolveReferences();
        reorderGroup = group;
        slotIndex = index;
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            rootCanvas = rootCanvas.rootCanvas;
        }
    }

    public void SetIcon(Sprite iconSprite)
    {
        ResolveReferences();
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = iconSprite;
        iconImage.enabled = iconSprite != null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (reorderGroup == null || !reorderGroup.CanDragSlot(slotIndex) || dragTarget == null || rootCanvas == null)
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
        canvasGroup.alpha = 1f;
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
        if (reorderGroup == null || eventData == null)
        {
            return;
        }

        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null)
        {
            return;
        }

        SC_CharacterSlotDragItem draggedSlot = draggedObject.GetComponentInParent<SC_CharacterSlotDragItem>();
        if (draggedSlot == null || draggedSlot == this)
        {
            return;
        }

        reorderGroup.SwapSlots(draggedSlot.SlotIndex, slotIndex);
    }

    private void ResolveReferences()
    {
        if (iconImage == null)
        {
            iconImage = GetComponentInChildren<Image>(true);
        }

        if (dragTarget == null && iconImage != null)
        {
            dragTarget = iconImage.rectTransform;
        }

        if (dragTarget == null)
        {
            dragTarget = transform as RectTransform;
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

    private void UpdateDragPosition(PointerEventData eventData)
    {
        RectTransform canvasRectTransform = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
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
        if (reorderGroup == null || eventData == null || EventSystem.current == null)
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

            SC_CharacterSlotDragItem hoveredSlot = hoveredObject.GetComponentInParent<SC_CharacterSlotDragItem>();
            if (hoveredSlot == null || hoveredSlot == this)
            {
                continue;
            }

            reorderGroup.SwapSlots(slotIndex, hoveredSlot.SlotIndex);
            return;
        }
    }
}
