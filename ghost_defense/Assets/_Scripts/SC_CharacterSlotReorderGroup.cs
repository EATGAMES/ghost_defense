using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_CharacterSlotReorderGroup : MonoBehaviour
{
    [Tooltip("캐릭터 순서를 보여주고 드래그할 슬롯 목록입니다. 비워 두면 OBJ_Slot1~5를 자동으로 찾습니다.")]
    [SerializeField] private SC_CharacterSlotDragItem[] slotItems = Array.Empty<SC_CharacterSlotDragItem>();

    [Tooltip("슬롯 목록이 비어 있을 때 OBJ_Slot1~5를 자동으로 찾을지 여부입니다.")]
    [SerializeField] private bool autoFindSlots = true;

    [Tooltip("슬롯에 표시할 캐릭터 데이터 원본 순서입니다.")]
    [SerializeField] private SO_CharacterData[] rosterCharacters = new SO_CharacterData[5];

    [Tooltip("시작할 때 저장된 캐릭터 순서를 불러올지 여부입니다.")]
    [SerializeField] private bool loadSavedOrderOnStart = true;

    [Tooltip("드래그가 끝났을 때 변경된 순서를 저장할지 여부입니다.")]
    [SerializeField] private bool saveOrderOnDrop = true;

    private int[] rosterOrder = Array.Empty<int>();
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
            LoadSavedOrder();
            RefreshSlots();
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

        ResolveSlots();
        InitializeSlots();
        LoadSavedOrder();
        isInitialized = true;
        RefreshSlots();
    }

    public bool CanDragSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < rosterOrder.Length;
    }

    public void SwapSlots(int fromSlotIndex, int toSlotIndex)
    {
        EnsureRosterOrder();
        if (!CanDragSlot(fromSlotIndex) || !CanDragSlot(toSlotIndex) || fromSlotIndex == toSlotIndex)
        {
            return;
        }

        (rosterOrder[fromSlotIndex], rosterOrder[toSlotIndex]) = (rosterOrder[toSlotIndex], rosterOrder[fromSlotIndex]);
        if (saveOrderOnDrop)
        {
            SC_RosterSave.SaveOrder(rosterOrder);
        }

        RefreshSlots();
    }

    private void ResolveSlots()
    {
        if (!autoFindSlots && slotItems != null && slotItems.Length > 0)
        {
            return;
        }

        SC_CharacterSlotDragItem[] resolvedSlots = new SC_CharacterSlotDragItem[5];
        for (int i = 0; i < resolvedSlots.Length; i++)
        {
            Transform slotTransform = FindDirectChild($"OBJ_Slot{i + 1}") ?? FindDescendant($"OBJ_Slot{i + 1}");
            if (slotTransform == null)
            {
                continue;
            }

            SC_CharacterSlotDragItem slotItem = slotTransform.GetComponent<SC_CharacterSlotDragItem>();
            if (slotItem == null)
            {
                slotItem = slotTransform.gameObject.AddComponent<SC_CharacterSlotDragItem>();
            }

            resolvedSlots[i] = slotItem;
        }

        slotItems = resolvedSlots;
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < slotItems.Length; i++)
        {
            SC_CharacterSlotDragItem slotItem = slotItems[i];
            if (slotItem == null)
            {
                continue;
            }

            slotItem.Setup(this, i);
        }
    }

    private void LoadSavedOrder()
    {
        int slotCount = GetSlotCount();
        rosterOrder = loadSavedOrderOnStart ? SC_RosterSave.LoadOrder(slotCount) : CreateDefaultOrder(slotCount);
    }

    private void EnsureRosterOrder()
    {
        int slotCount = GetSlotCount();
        if (rosterOrder != null && rosterOrder.Length == slotCount)
        {
            return;
        }

        rosterOrder = SC_RosterSave.LoadOrder(slotCount);
    }

    private void RefreshSlots()
    {
        EnsureRosterOrder();
        for (int i = 0; i < slotItems.Length; i++)
        {
            SC_CharacterSlotDragItem slotItem = slotItems[i];
            if (slotItem == null)
            {
                continue;
            }

            slotItem.SetIcon(ResolveCharacterSprite(i));
        }
    }

    private Sprite ResolveCharacterSprite(int slotIndex)
    {
        if (!CanDragSlot(slotIndex))
        {
            return null;
        }

        int rosterIndex = rosterOrder[slotIndex];
        if (rosterCharacters == null || rosterIndex < 0 || rosterIndex >= rosterCharacters.Length)
        {
            return null;
        }

        SO_CharacterData characterData = rosterCharacters[rosterIndex];
        if (characterData == null)
        {
            return null;
        }

        return characterData.GetTopCharacterSpriteForGrade(rosterIndex + 6);
    }

    private void OnRosterOrderChanged(int[] changedOrder)
    {
        if (changedOrder == null || changedOrder.Length != GetSlotCount())
        {
            return;
        }

        rosterOrder = new int[changedOrder.Length];
        Array.Copy(changedOrder, rosterOrder, changedOrder.Length);
        RefreshSlots();
    }

    private Transform FindDirectChild(string childName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null && child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private Transform FindDescendant(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child != transform && child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private int GetSlotCount()
    {
        return slotItems != null ? slotItems.Length : 0;
    }

    private static int[] CreateDefaultOrder(int slotCount)
    {
        int[] defaultOrder = new int[Mathf.Max(0, slotCount)];
        for (int i = 0; i < defaultOrder.Length; i++)
        {
            defaultOrder[i] = i;
        }

        return defaultOrder;
    }
}
