using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_CharacterGradePreviewUI : MonoBehaviour
{
    [Tooltip("등급별 프리뷰 스프라이트를 가져올 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("1등급부터 10등급까지 순서대로 연결하는 UI 이미지 목록입니다.")]
    [SerializeField] private Image[] gradePreviewImages = new Image[10];

    [Tooltip("이번 판의 최고 도달 등급을 표시할 포인터 오브젝트입니다.")]
    [SerializeField] private RectTransform gradePointer;

    [Tooltip("프리뷰 스프라이트가 없을 때 이미지를 숨길지 여부입니다.")]
    [SerializeField] private bool hideImageWhenSpriteMissing = true;

    private int highestReachedGrade = 1;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        RefreshPreviewImages();
        ResetReachedGrade();
    }

    private void OnEnable()
    {
        RefreshPreviewImages();
        ResetReachedGrade();
    }

    public void ResetReachedGrade()
    {
        highestReachedGrade = 1;
        RefreshPointerPosition();
    }

    public void ReportReachedGrade(int grade)
    {
        highestReachedGrade = Mathf.Max(highestReachedGrade, Mathf.Clamp(grade, 1, 10));
        RefreshPointerPosition();
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
            Sprite previewSprite = battleManager.GetPreviewSpriteForGrade(grade);
            targetImage.sprite = previewSprite;

            if (hideImageWhenSpriteMissing)
            {
                targetImage.enabled = previewSprite != null;
            }
        }
    }

    public void RefreshPointerPosition()
    {
        if (gradePointer == null || gradePreviewImages == null || gradePreviewImages.Length <= 0)
        {
            return;
        }

        int gradeIndex = Mathf.Clamp(highestReachedGrade, 1, gradePreviewImages.Length) - 1;
        Image targetImage = gradePreviewImages[gradeIndex];
        if (targetImage == null)
        {
            return;
        }

        RectTransform targetRect = targetImage.rectTransform;
        RectTransform pointerParent = gradePointer.parent as RectTransform;
        if (targetRect == null || pointerParent == null)
        {
            return;
        }

        Vector3 targetWorldCenter = targetRect.TransformPoint(targetRect.rect.center);
        Vector3 targetLocalPoint = pointerParent.InverseTransformPoint(targetWorldCenter);
        Vector2 pointerPosition = gradePointer.anchoredPosition;
        pointerPosition.x = targetLocalPoint.x;
        gradePointer.anchoredPosition = pointerPosition;
    }
}
