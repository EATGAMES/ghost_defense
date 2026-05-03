using UnityEngine;

[DisallowMultipleComponent]
public class SC_FieldDetectTrigger : MonoBehaviour
{
    [Tooltip("감지 중일 때 켜둘 대시 라인 오브젝트입니다.")]
    [SerializeField] private GameObject dashLineObject;

    [Tooltip("게임오버를 전달할 배틀 매니저입니다.")]
    [SerializeField] private SC_BattleManager battleManager;

    [Tooltip("발사된 캐릭터가 닿으면 게임오버를 발생시킬지 여부입니다.")]
    [SerializeField] private bool failOnShotEnter;

    [Tooltip("겹침 검사에 사용할 최대 콜라이더 수입니다.")]
    [SerializeField] private int overlapBufferSize = 32;

    private Collider2D detectorCollider;
    private Collider2D[] overlapResults;
    private bool isBattleFailTriggered;

    private void Awake()
    {
        detectorCollider = GetComponent<Collider2D>();

        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<SC_BattleManager>();
        }

        int bufferSize = Mathf.Max(8, overlapBufferSize);
        overlapResults = new Collider2D[bufferSize];
        RefreshDashLineState(false);
    }

    private void FixedUpdate()
    {
        bool hasShotCharacterInside = HasShotCharacterInside();
        RefreshDashLineState(hasShotCharacterInside);

        if (!hasShotCharacterInside || !failOnShotEnter || isBattleFailTriggered)
        {
            return;
        }

        isBattleFailTriggered = true;
        if (battleManager == null)
        {
            Debug.LogWarning("SC_FieldDetectTrigger: SC_BattleManager를 찾지 못했습니다.", this);
            return;
        }

        battleManager.NotifyBattleFailed();
    }

    private bool HasShotCharacterInside()
    {
        if (detectorCollider == null)
        {
            return false;
        }

        ContactFilter2D contactFilter = ContactFilter2D.noFilter;
        contactFilter.useTriggers = true;

        int hitCount = detectorCollider.Overlap(contactFilter, overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            SC_PlayerDragAndShoot shotCharacter = GetShotCharacter(overlapResults[i]);
            if (shotCharacter != null && shotCharacter.HasCollidedAfterShot)
            {
                return true;
            }
        }

        return false;
    }

    private static SC_PlayerDragAndShoot GetShotCharacter(Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        SC_PlayerDragAndShoot shotCharacter = other.GetComponent<SC_PlayerDragAndShoot>();
        if (shotCharacter == null)
        {
            shotCharacter = other.GetComponentInParent<SC_PlayerDragAndShoot>();
        }

        if (shotCharacter == null || !shotCharacter.IsShot)
        {
            return null;
        }

        return shotCharacter;
    }

    private void RefreshDashLineState(bool isActive)
    {
        if (dashLineObject == null)
        {
            return;
        }

        if (dashLineObject.activeSelf == isActive)
        {
            return;
        }

        dashLineObject.SetActive(isActive);
    }
}
