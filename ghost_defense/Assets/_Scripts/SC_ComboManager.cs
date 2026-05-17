using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_ComboManager : MonoBehaviour
{
    public readonly struct ComboMergeResult
    {
        public readonly int ComboCount;
        public readonly float DamageMultiplier;

        public ComboMergeResult(int comboCount, float damageMultiplier)
        {
            ComboCount = Mathf.Max(0, comboCount);
            DamageMultiplier = Mathf.Max(1f, damageMultiplier);
        }
    }

    private static readonly ComboMergeResult EmptyMergeResult = new ComboMergeResult(0, 1f);

    public static SC_ComboManager Instance { get; private set; }

    [Tooltip("콤보가 표시되기 시작하는 최소 합성 횟수입니다.")]
    [SerializeField] private int displayStartCombo = 3;

    [Tooltip("콤보 숫자 하나를 최소로 보여줄 시간(초)입니다.")]
    [SerializeField] private float minimumComboDisplayDuration = 0.15f;

    [Tooltip("콤보 데미지 증가가 시작되는 최소 합성 횟수입니다.")]
    [SerializeField] private int damageStartCombo = 3;

    [Tooltip("콤보 1회당 데미지 증가 비율입니다. 0.1이면 3콤보에서 +30%, 4콤보에서 +40%입니다.")]
    [SerializeField] private float damageBonusPerCombo = 0.1f;

    [Tooltip("콤보가 발생했을 때 생성할 텍스트 팝업 프리팹입니다.")]
    [SerializeField] private SC_ComboTextPopup comboTextPrefab;

    [Tooltip("콤보 텍스트를 생성할 부모 RectTransform입니다. 비워두면 현재 오브젝트 아래에 생성합니다.")]
    [SerializeField] private RectTransform comboTextParent;

    private readonly Queue<int> pendingDisplayCombos = new Queue<int>();
    private SC_ComboTextPopup currentPopup;
    private Coroutine displayCoroutine;
    private int comboCount;
    private int currentActionMergeCount;
    private bool isComboSessionActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void NotifyShotStartedGlobal()
    {
        if (Instance != null)
        {
            Instance.NotifyShotStarted();
        }
    }

    public static ComboMergeResult NotifyMergeCreatedGlobal()
    {
        return Instance != null ? Instance.NotifyMergeCreated() : EmptyMergeResult;
    }

    public void NotifyShotStarted()
    {
        if (isComboSessionActive && currentActionMergeCount <= 0)
        {
            ResetCombo();
        }

        isComboSessionActive = true;
        currentActionMergeCount = 0;
    }

    public ComboMergeResult NotifyMergeCreated()
    {
        if (!isComboSessionActive)
        {
            isComboSessionActive = true;
            currentActionMergeCount = 0;
        }

        currentActionMergeCount++;
        comboCount++;

        if (comboCount >= Mathf.Max(1, displayStartCombo))
        {
            pendingDisplayCombos.Enqueue(comboCount);
            if (displayCoroutine == null)
            {
                displayCoroutine = StartCoroutine(CoDisplayQueuedCombos());
            }
        }

        return new ComboMergeResult(comboCount, CalculateDamageMultiplier(comboCount));
    }

    private float CalculateDamageMultiplier(int targetComboCount)
    {
        if (targetComboCount < Mathf.Max(1, damageStartCombo))
        {
            return 1f;
        }

        return 1f + Mathf.Max(0f, damageBonusPerCombo) * targetComboCount;
    }

    private IEnumerator CoDisplayQueuedCombos()
    {
        while (pendingDisplayCombos.Count > 0)
        {
            int displayCombo = pendingDisplayCombos.Dequeue();
            ShowComboPopup(displayCombo);

            float remainTime = Mathf.Max(0f, minimumComboDisplayDuration);
            while (remainTime > 0f)
            {
                remainTime -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        displayCoroutine = null;
    }

    private void ResetCombo()
    {
        comboCount = 0;
        currentActionMergeCount = 0;
        isComboSessionActive = false;
        pendingDisplayCombos.Clear();

        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        ClearCurrentPopup();
    }

    private void ShowComboPopup(int displayCombo)
    {
        if (comboTextPrefab == null)
        {
            return;
        }

        ClearCurrentPopup();

        Transform parent = comboTextParent != null ? comboTextParent : transform;
        currentPopup = Instantiate(comboTextPrefab, parent);
        currentPopup.ShowCombo(displayCombo);
    }

    private void ClearCurrentPopup()
    {
        if (currentPopup == null)
        {
            return;
        }

        Destroy(currentPopup.gameObject);
        currentPopup = null;
    }
}
