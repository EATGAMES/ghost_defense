using UnityEngine;

public enum CharacterKind
{
    방깨비,
    구미오,
    유우령,
    미희라,
    도라큘,
    강시희,
    천여귀
}

public enum CharacterAttackType
{
    원거리,
    소환,
    버프
}

public enum CharacterGrade
{
    Grade1 = 1,
    Grade2 = 2,
    Grade3 = 3,
    Grade4 = 4,
    Grade5 = 5
}

[CreateAssetMenu(fileName = "SO_CharacterData", menuName = "Ghost Defense/Character Data")]
public class SO_CharacterData : ScriptableObject
{
    [Tooltip("캐릭터 이름")]
    [SerializeField] private string characterName;

    [Tooltip("캐릭터 설명")]
    [TextArea(3, 8)]
    [SerializeField] private string characterDescription;

    [Tooltip("캐릭터 공용 스프라이트")]
    [SerializeField] private Sprite characterSprite;

    [Tooltip("캐릭터 종류")]
    [SerializeField] private CharacterKind characterKind;

    [Tooltip("캐릭터 등급")]
    [SerializeField] private CharacterGrade characterGrade = CharacterGrade.Grade1;

    [Tooltip("공격 방식")]
    [SerializeField] private CharacterAttackType attackType;

    [Tooltip("캐릭터 무게")]
    [SerializeField] private float weight = 1f;

    [Tooltip("CircleCollider2D 반지름")]
    [SerializeField] private float circleColliderRadius = 0.55f;

    [Tooltip("CircleCollider2D 오프셋")]
    [SerializeField] private Vector2 circleColliderOffset = new Vector2(0.02f, -0.15f);

    [Tooltip("합체 시 생성할 다음 등급 캐릭터 데이터")]
    [SerializeField] private SO_CharacterData nextGradeCharacterData;

    [Tooltip("자동 발사로 생성할 발사체 프리팹")]
    [SerializeField] private GameObject autoProjectilePrefab;

    [Tooltip("자동 발사 간격(초)")]
    [SerializeField] private float autoProjectileSpawnDelay = 1f;

    [Tooltip("자동 발사체 이동 속도")]
    [SerializeField] private float autoProjectileSpeed = 6f;

    public string CharacterName => characterName;
    public string CharacterDescription => characterDescription;
    public Sprite CharacterSprite => characterSprite;
    public CharacterKind CharacterKind => characterKind;
    public CharacterGrade CharacterGrade => characterGrade;
    public CharacterAttackType AttackType => attackType;
    public float Weight => weight;
    public float CircleColliderRadius => circleColliderRadius;
    public Vector2 CircleColliderOffset => circleColliderOffset;
    public SO_CharacterData NextGradeCharacterData => nextGradeCharacterData;
    public GameObject AutoProjectilePrefab => autoProjectilePrefab;
    public float AutoProjectileSpawnDelay => autoProjectileSpawnDelay;
    public float AutoProjectileSpeed => autoProjectileSpeed;
}

