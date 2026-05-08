using UnityEngine;

[DisallowMultipleComponent]
public class SC_StageGimmickController : MonoBehaviour
{
    [Header("극한 충돌")]
    [Tooltip("극한 충돌이 켜졌을 때 충돌 후 속도를 얼마나 더 유지할지에 대한 배수입니다.")]
    [SerializeField] private float extremeCollisionVelocityMultiplier = 1.2f;

    [Tooltip("극한 충돌이 켜졌을 때 측면 미끄러짐을 얼마나 더 유지할지에 대한 배수입니다.")]
    [SerializeField] private float extremeCollisionSideSlipMultiplier = 2.5f;

    [Tooltip("극한 충돌이 켜졌을 때 기본 감속을 얼마나 줄일지에 대한 배수입니다. 값이 낮을수록 더 오래 미끄러집니다.")]
    [SerializeField] private float extremeCollisionDecelerationMultiplier = 0.6f;

    [Tooltip("극한 충돌이 켜졌을 때 아래로 내려오는 캐릭터의 감속에 추가로 곱할 배수입니다.")]
    [SerializeField] private float extremeCollisionDownwardBrakeMultiplier = 2f;

    [Header("중력장")]
    [Tooltip("중력장이 켜졌을 때 필드 캐릭터를 위로 끌어올리는 가속도 크기입니다.")]
    [SerializeField] private float gravityFieldUpwardAcceleration = 6f;

    public static float CurrentCollisionVelocityMultiplier { get; private set; } = 1f;
    public static float CurrentSideSlipMultiplier { get; private set; } = 1f;
    public static float CurrentDecelerationMultiplier { get; private set; } = 1f;
    public static float CurrentDownwardBrakeMultiplier { get; private set; } = 1f;
    public static bool IsExtremeCollisionActive { get; private set; }
    public static bool IsGravityFieldActive { get; private set; }

    private void OnEnable()
    {
        ResetGimmicks();
    }

    private void OnDisable()
    {
        ResetGimmicks();
    }

    private void FixedUpdate()
    {
        if (!IsGravityFieldActive)
        {
            return;
        }

        ApplyGravityField();
    }

    public void ApplyMonsterData(SO_MonsterData monsterData)
    {
        if (monsterData == null)
        {
            ResetGimmicks();
            return;
        }

        IsExtremeCollisionActive = monsterData.UseExtremeCollision;
        IsGravityFieldActive = monsterData.UseGravityField;
        CurrentCollisionVelocityMultiplier = IsExtremeCollisionActive ? Mathf.Max(1f, extremeCollisionVelocityMultiplier) : 1f;
        CurrentSideSlipMultiplier = IsExtremeCollisionActive ? Mathf.Max(1f, extremeCollisionSideSlipMultiplier) : 1f;
        CurrentDecelerationMultiplier = IsExtremeCollisionActive ? Mathf.Clamp(extremeCollisionDecelerationMultiplier, 0.05f, 1f) : 1f;
        CurrentDownwardBrakeMultiplier = IsExtremeCollisionActive ? Mathf.Max(1f, extremeCollisionDownwardBrakeMultiplier) : 1f;
    }

    public void ResetGimmicks()
    {
        IsExtremeCollisionActive = false;
        IsGravityFieldActive = false;
        CurrentCollisionVelocityMultiplier = 1f;
        CurrentSideSlipMultiplier = 1f;
        CurrentDecelerationMultiplier = 1f;
        CurrentDownwardBrakeMultiplier = 1f;
    }

    private void ApplyGravityField()
    {
        SC_CharacterPresenter[] characterPresenters = FindObjectsByType<SC_CharacterPresenter>(FindObjectsSortMode.None);
        for (int i = 0; i < characterPresenters.Length; i++)
        {
            SC_CharacterPresenter characterPresenter = characterPresenters[i];
            if (characterPresenter == null)
            {
                continue;
            }

            SC_PlayerDragAndShoot dragAndShoot = characterPresenter.GetComponent<SC_PlayerDragAndShoot>();
            if (dragAndShoot == null || !dragAndShoot.IsShot)
            {
                continue;
            }

            Rigidbody2D rb2D = characterPresenter.GetComponent<Rigidbody2D>();
            if (rb2D == null || !rb2D.simulated)
            {
                continue;
            }

            rb2D.AddForce(Vector2.up * Mathf.Max(0f, gravityFieldUpwardAcceleration), ForceMode2D.Force);
        }
    }
}
