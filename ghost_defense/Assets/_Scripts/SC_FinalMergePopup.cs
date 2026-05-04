using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SC_FinalMergePopup : MonoBehaviour
{
    [Tooltip("팝업 뒤 배경을 어둡게 처리할 DIM 오브젝트입니다.")]
    [SerializeField] private GameObject dimObject;

    [Tooltip("최종 머지 팝업 루트 오브젝트입니다.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("최종 머지 캐릭터 이미지를 표시할 UI Image입니다.")]
    [SerializeField] private Image characterImage;

    [Tooltip("팝업이 열린 뒤 자동으로 닫히기까지의 시간(초)입니다.")]
    [SerializeField] private float autoCloseDelay = 1f;

    private Coroutine popupCoroutine;

    public bool IsPopupOpen => popupRoot != null && popupRoot.activeInHierarchy;

    private void Awake()
    {
        SetPopupVisible(false);
    }

    private void OnDisable()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }

    public IEnumerator CoOpenAndWait()
    {
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        CancelAllPendingCharacterDrags();
        SetPopupVisible(true);
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, autoCloseDelay));
        Time.timeScale = 1f;
        SetPopupVisible(false);
        popupCoroutine = null;
    }

    public void SetCharacterSprite(Sprite characterSprite)
    {
        if (characterImage == null)
        {
            return;
        }

        characterImage.sprite = characterSprite;
        characterImage.enabled = characterSprite != null;
    }

    private void SetPopupVisible(bool isVisible)
    {
        if (dimObject != null)
        {
            dimObject.SetActive(isVisible);
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(isVisible);
        }
    }

    private static void CancelAllPendingCharacterDrags()
    {
        SC_PlayerDragAndShoot[] allShooters = FindObjectsByType<SC_PlayerDragAndShoot>(FindObjectsSortMode.None);
        for (int i = 0; i < allShooters.Length; i++)
        {
            SC_PlayerDragAndShoot shooter = allShooters[i];
            if (shooter == null || shooter.IsShot)
            {
                continue;
            }

            shooter.CancelDragAndResetToStartPosition();
        }
    }
}
