using UnityEngine;

[DisallowMultipleComponent]
public class SC_StageRosterEditor : MonoBehaviour
{
    [Tooltip("편성 순서를 표시하고 드래그할 슬롯 목록입니다.")]
    [SerializeField] private SC_StageSlotUI[] slotUIs = new SC_StageSlotUI[5];

    [Tooltip("편성에 사용하는 캐릭터 데이터 원본 순서입니다.")]
    [SerializeField] private SO_CharacterData[] rosterCharacters = new SO_CharacterData[5];

    [Tooltip("편성에 사용하는 필드 스킨 데이터 원본 순서입니다.")]
    [SerializeField] private SO_FieldCharacterSkinData[] rosterFieldSkins = new SO_FieldCharacterSkinData[5];

    private int[] rosterOrder = new int[0];
    private bool isInitialized;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    public void InitializeIfNeeded()
    {
        if (isInitialized)
        {
            return;
        }

        InitializeSlots();
        LoadSavedOrder();
        isInitialized = true;
        RefreshRosterUI();
    }

    public void RefreshRosterUI()
    {
        InitializeIfNeeded();
        EnsureRosterOrder();

        for (int i = 0; i < slotUIs.Length; i++)
        {
            SC_StageSlotUI slotUI = slotUIs[i];
            if (slotUI == null)
            {
                continue;
            }

            slotUI.SetIcon(ResolvePreviewSprite(i));
        }
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
        SC_RosterSave.SaveOrder(rosterOrder);
        RefreshRosterUI();
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null)
            {
                continue;
            }

            slotUIs[i].Setup(this, i);
        }
    }

    private void LoadSavedOrder()
    {
        rosterOrder = SC_RosterSave.LoadOrder(GetSlotCount());
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

    private Sprite ResolvePreviewSprite(int slotIndex)
    {
        if (!CanDragSlot(slotIndex))
        {
            return null;
        }

        int rosterIndex = rosterOrder[slotIndex];

        if (rosterFieldSkins != null && rosterIndex >= 0 && rosterIndex < rosterFieldSkins.Length)
        {
            SO_FieldCharacterSkinData skinData = rosterFieldSkins[rosterIndex];
            if (skinData != null)
            {
                Sprite previewSprite = skinData.GetPreviewSpriteForGrade(rosterIndex + 1);
                if (previewSprite != null)
                {
                    return previewSprite;
                }
            }
        }

        if (rosterCharacters != null && rosterIndex >= 0 && rosterIndex < rosterCharacters.Length)
        {
            SO_CharacterData characterData = rosterCharacters[rosterIndex];
            if (characterData != null)
            {
                return characterData.GetTopCharacterSpriteForGrade(rosterIndex + 1);
            }
        }

        return null;
    }

    private int GetSlotCount()
    {
        return slotUIs != null ? slotUIs.Length : 0;
    }
}
