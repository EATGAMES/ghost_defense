using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_CharacterGradePreviewUI : MonoBehaviour
{
    [Tooltip("단계별 미리보기 스프라이트를 가져올 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("1단계부터 10단계까지 순서대로 연결할 UI 이미지 목록입니다.")]
    [SerializeField] private Image[] gradePreviewImages = new Image[10];

    [Tooltip("스프라이트가 없을 때 이미지를 숨길지 여부입니다.")]
    [SerializeField] private bool hideImageWhenSpriteMissing = true;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        RefreshPreviewImages();
    }

    private void OnEnable()
    {
        RefreshPreviewImages();
    }

    public void RefreshPreviewImages()
    {
        if (battleManager == null || gradePreviewImages == null)
        {
            return;
        }

        for (int i = 0; i < gradePreviewImages.Length; i++)
        {
            Image targetImage = gradePreviewImages[i];
            if (targetImage == null)
            {
                continue;
            }

            int grade = i + 1;
            Sprite previewSprite = battleManager.GetFieldSpriteForGrade(grade);
            targetImage.sprite = previewSprite;

            if (hideImageWhenSpriteMissing)
            {
                targetImage.enabled = previewSprite != null;
            }
        }
    }
}
