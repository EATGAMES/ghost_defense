using UnityEngine;

[DisallowMultipleComponent]
public class SC_AttackRosterCharacterView : MonoBehaviour
{
    [Tooltip("상단 캐릭터 이미지를 표시할 SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("대기 상태일 때 적용할 색상입니다.")]
    [SerializeField] private Color idleColor = Color.white;

    [Tooltip("현재 공격 차례일 때 적용할 색상입니다.")]
    [SerializeField] private Color currentTurnColor = new Color(1f, 0.95f, 0.65f, 1f);

    [Tooltip("캐릭터 데이터가 없을 때 숨길지 여부입니다.")]
    [SerializeField] private bool hideWhenEmpty = true;

    public SO_CharacterData CurrentCharacterData { get; private set; }

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetCharacterData(SO_CharacterData characterData)
    {
        CurrentCharacterData = characterData;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = characterData != null ? characterData.CharacterSprite : null;
        }

        if (hideWhenEmpty)
        {
            gameObject.SetActive(characterData != null);
        }
    }

    public void SetHighlight(bool isCurrentTurn)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = isCurrentTurn ? currentTurnColor : idleColor;
    }

    public void SetSortingOrder(int sortingOrder)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sortingOrder = sortingOrder;
    }
}
