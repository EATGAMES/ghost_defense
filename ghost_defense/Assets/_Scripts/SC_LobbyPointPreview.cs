using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_LobbyPointPreview : MonoBehaviour
{
    [Tooltip("로비 표시 순서의 원본이 되는 필드 스킨 데이터 목록입니다.")]
    [SerializeField] private SO_FieldCharacterSkinData[] rosterFieldSkins = new SO_FieldCharacterSkinData[5];

    [Tooltip("OBJ_Point1~5에 표시할 이미지 목록입니다.")]
    [SerializeField] private Image[] pointImages = new Image[5];

    private void Awake()
    {
        RefreshPreview();
    }

    private void OnEnable()
    {
        RefreshPreview();
    }

    public void RefreshPreview()
    {
        if (rosterFieldSkins == null || pointImages == null)
        {
            return;
        }

        int slotCount = Mathf.Min(rosterFieldSkins.Length, pointImages.Length);
        int[] rosterOrder = SC_RosterSave.LoadOrder(slotCount);

        for (int i = 0; i < slotCount; i++)
        {
            Image pointImage = pointImages[i];
            if (pointImage == null)
            {
                continue;
            }

            int rosterIndex = rosterOrder[i];
            Sprite displaySprite = ResolvePointSprite(rosterIndex);
            pointImage.sprite = displaySprite;
            pointImage.enabled = displaySprite != null;
        }
    }

    private Sprite ResolvePointSprite(int rosterIndex)
    {
        if (rosterIndex < 0 || rosterIndex >= rosterFieldSkins.Length)
        {
            return null;
        }

        SO_FieldCharacterSkinData skinData = rosterFieldSkins[rosterIndex];
        if (skinData == null)
        {
            return null;
        }

        return skinData.GetSecondCycleFieldSprite();
    }
}
