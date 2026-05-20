using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class SC_CharacterPanelReorderGroup : MonoBehaviour
{
    [Tooltip("드래그로 순서를 바꿀 캐릭터 패널 목록입니다. 비워 두면 자식에서 자동으로 찾습니다.")]
    [SerializeField] private SC_CharacterPanel[] characterPanels = Array.Empty<SC_CharacterPanel>();

    [Tooltip("패널 목록이 비어 있을 때 자식 캐릭터 패널을 자동으로 찾을지 여부입니다.")]
    [SerializeField] private bool autoFindPanels = true;

    [Tooltip("시작할 때 저장된 캐릭터 편성 순서를 패널 배치에 반영할지 여부입니다.")]
    [SerializeField] private bool loadSavedOrderOnStart = true;

    [Tooltip("드래그가 끝났을 때 변경된 순서를 저장할지 여부입니다.")]
    [SerializeField] private bool saveOrderOnDrop = true;

    [Tooltip("드래그 중 다른 패널이 자리로 이동하는 시간(초)입니다.")]
    [SerializeField] private float moveAnimationDuration = 0.12f;

    private readonly List<SC_CharacterPanelDragItem> defaultItems = new List<SC_CharacterPanelDragItem>(8);
    private readonly List<SC_CharacterPanelDragItem> orderedItems = new List<SC_CharacterPanelDragItem>(8);
    private readonly List<Vector2> slotPositions = new List<Vector2>(8);
    private readonly Dictionary<RectTransform, Coroutine> moveCoroutines = new Dictionary<RectTransform, Coroutine>();
    private SC_CharacterPanelDragItem draggingItem;
    private int firstPanelSiblingIndex;
    private int previewIndex = -1;
    private bool isInitialized;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        SC_RosterSave.RosterOrderChanged += OnRosterOrderChanged;
        if (isInitialized)
        {
            ApplySavedOrder(false);
        }
    }

    private void OnDisable()
    {
        SC_RosterSave.RosterOrderChanged -= OnRosterOrderChanged;
    }

    public void InitializeIfNeeded()
    {
        if (isInitialized)
        {
            return;
        }

        ResolvePanels();
        CacheItemsAndSlots();
        ApplySavedOrderIfNeeded();
        ArrangeAllItems();
        ApplySiblingOrder();
        isInitialized = true;
    }

    public void BeginDrag(SC_CharacterPanelDragItem item, PointerEventData eventData)
    {
        InitializeIfNeeded();
        if (item == null || !orderedItems.Contains(item))
        {
            return;
        }

        draggingItem = item;
        previewIndex = Mathf.Clamp(orderedItems.IndexOf(item), 0, orderedItems.Count - 1);
        item.DragTarget.SetAsLastSibling();
        item.SetDraggingVisual(true);
        UpdateDraggedPosition(item, eventData);
        PreviewCurrentDragPosition();
    }

    public void Drag(SC_CharacterPanelDragItem item, PointerEventData eventData)
    {
        if (draggingItem == null || draggingItem != item)
        {
            return;
        }

        UpdateDraggedPosition(item, eventData);
        PreviewCurrentDragPosition();
    }

    public void EndDrag(SC_CharacterPanelDragItem item, PointerEventData eventData)
    {
        if (draggingItem == null || draggingItem != item)
        {
            return;
        }

        orderedItems.Remove(item);
        orderedItems.Insert(Mathf.Clamp(previewIndex, 0, orderedItems.Count), item);
        item.SetDraggingVisual(false);
        draggingItem = null;
        previewIndex = -1;

        ArrangeAllItems();
        ApplySiblingOrder();
        SaveCurrentOrder();
    }

    private void ResolvePanels()
    {
        if (!autoFindPanels && characterPanels != null && characterPanels.Length > 0)
        {
            return;
        }

        characterPanels = GetComponentsInChildren<SC_CharacterPanel>(true);
        Array.Sort(characterPanels, ComparePanelSiblingIndex);
    }

    private void CacheItemsAndSlots()
    {
        defaultItems.Clear();
        orderedItems.Clear();
        slotPositions.Clear();

        firstPanelSiblingIndex = int.MaxValue;
        for (int i = 0; i < characterPanels.Length; i++)
        {
            SC_CharacterPanel panel = characterPanels[i];
            if (panel == null)
            {
                continue;
            }

            SC_CharacterPanelDragItem item = panel.GetComponent<SC_CharacterPanelDragItem>();
            if (item == null)
            {
                item = panel.gameObject.AddComponent<SC_CharacterPanelDragItem>();
            }

            item.Setup(this, panel);
            defaultItems.Add(item);
            orderedItems.Add(item);

            RectTransform itemRectTransform = item.DragTarget;
            if (itemRectTransform != null)
            {
                slotPositions.Add(itemRectTransform.anchoredPosition);
                firstPanelSiblingIndex = Mathf.Min(firstPanelSiblingIndex, itemRectTransform.GetSiblingIndex());
            }
        }

        if (firstPanelSiblingIndex == int.MaxValue)
        {
            firstPanelSiblingIndex = 0;
        }
    }

    private void ApplySavedOrderIfNeeded()
    {
        if (!loadSavedOrderOnStart || defaultItems.Count <= 0)
        {
            return;
        }

        int[] savedOrder = SC_RosterSave.LoadOrder(defaultItems.Count);
        ApplyOrder(savedOrder);
    }

    private void ApplySavedOrder(bool animate)
    {
        if (defaultItems.Count <= 0)
        {
            return;
        }

        int[] savedOrder = SC_RosterSave.LoadOrder(defaultItems.Count);
        ApplyOrder(savedOrder);

        if (animate)
        {
            ArrangeItemsExceptDragging();
        }
        else
        {
            ArrangeAllItems();
        }

        ApplySiblingOrder();
    }

    private void ApplyOrder(int[] savedOrder)
    {
        if (savedOrder == null || savedOrder.Length != defaultItems.Count)
        {
            return;
        }

        orderedItems.Clear();
        for (int i = 0; i < savedOrder.Length; i++)
        {
            int itemIndex = savedOrder[i];
            if (itemIndex < 0 || itemIndex >= defaultItems.Count)
            {
                orderedItems.Clear();
                orderedItems.AddRange(defaultItems);
                return;
            }

            orderedItems.Add(defaultItems[itemIndex]);
        }
    }

    private void OnRosterOrderChanged(int[] changedOrder)
    {
        if (changedOrder == null || draggingItem != null || changedOrder.Length != defaultItems.Count)
        {
            return;
        }

        ApplyOrder(changedOrder);
        ArrangeAllItems();
        ApplySiblingOrder();
    }

    private void PreviewCurrentDragPosition()
    {
        if (draggingItem == null || draggingItem.DragTarget == null)
        {
            return;
        }

        int nextPreviewIndex = CalculateSlotIndex(draggingItem.DragTarget.anchoredPosition.y);
        if (previewIndex == nextPreviewIndex)
        {
            return;
        }

        previewIndex = nextPreviewIndex;
        ArrangeItemsExceptDragging();
    }

    private int CalculateSlotIndex(float draggedY)
    {
        if (slotPositions.Count <= 1)
        {
            return 0;
        }

        bool isDescending = slotPositions[0].y >= slotPositions[slotPositions.Count - 1].y;
        for (int i = 0; i < slotPositions.Count - 1; i++)
        {
            float midpointY = (slotPositions[i].y + slotPositions[i + 1].y) * 0.5f;
            if (isDescending && draggedY >= midpointY)
            {
                return i;
            }

            if (!isDescending && draggedY <= midpointY)
            {
                return i;
            }
        }

        return slotPositions.Count - 1;
    }

    private void ArrangeItemsExceptDragging()
    {
        int slotIndex = 0;
        for (int i = 0; i < orderedItems.Count; i++)
        {
            SC_CharacterPanelDragItem item = orderedItems[i];
            if (item == null || item == draggingItem)
            {
                continue;
            }

            if (slotIndex == previewIndex)
            {
                slotIndex++;
            }

            MoveItemToSlot(item, slotIndex, true);
            slotIndex++;
        }
    }

    private void ArrangeAllItems()
    {
        for (int i = 0; i < orderedItems.Count; i++)
        {
            MoveItemToSlot(orderedItems[i], i, false);
        }
    }

    private void MoveItemToSlot(SC_CharacterPanelDragItem item, int slotIndex, bool animate)
    {
        if (item == null || item.DragTarget == null || slotIndex < 0 || slotIndex >= slotPositions.Count)
        {
            return;
        }

        RectTransform target = item.DragTarget;
        Vector2 targetPosition = slotPositions[slotIndex];

        if (moveCoroutines.TryGetValue(target, out Coroutine runningCoroutine) && runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            moveCoroutines.Remove(target);
        }

        if (!animate || moveAnimationDuration <= 0f)
        {
            target.anchoredPosition = targetPosition;
            return;
        }

        moveCoroutines[target] = StartCoroutine(CoMoveToSlot(target, targetPosition));
    }

    private IEnumerator CoMoveToSlot(RectTransform target, Vector2 targetPosition)
    {
        if (target == null)
        {
            yield break;
        }

        Vector2 startPosition = target.anchoredPosition;
        float duration = Mathf.Max(0.01f, moveAnimationDuration);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (target == null)
            {
                yield break;
            }

            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
            float easedTime = 1f - Mathf.Pow(1f - normalizedTime, 3f);
            target.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, easedTime);
            yield return null;
        }

        target.anchoredPosition = targetPosition;
        moveCoroutines.Remove(target);
    }

    private void ApplySiblingOrder()
    {
        for (int i = 0; i < orderedItems.Count; i++)
        {
            SC_CharacterPanelDragItem item = orderedItems[i];
            if (item == null || item.DragTarget == null)
            {
                continue;
            }

            item.DragTarget.SetSiblingIndex(firstPanelSiblingIndex + i);
        }
    }

    private void UpdateDraggedPosition(SC_CharacterPanelDragItem item, PointerEventData eventData)
    {
        if (item == null || item.DragTarget == null || eventData == null)
        {
            return;
        }

        RectTransform parentRectTransform = item.DragTarget.parent as RectTransform;
        if (parentRectTransform == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            item.DragTarget.anchoredPosition = localPoint;
        }
    }

    private void SaveCurrentOrder()
    {
        if (!saveOrderOnDrop || orderedItems.Count <= 0)
        {
            return;
        }

        int[] rosterOrder = new int[orderedItems.Count];
        for (int i = 0; i < orderedItems.Count; i++)
        {
            rosterOrder[i] = Mathf.Max(0, defaultItems.IndexOf(orderedItems[i]));
        }

        SC_RosterSave.SaveOrder(rosterOrder);
    }

    private static int ComparePanelSiblingIndex(SC_CharacterPanel left, SC_CharacterPanel right)
    {
        if (left == right)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex());
    }
}
