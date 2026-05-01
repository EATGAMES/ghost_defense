using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_WaveManager : MonoBehaviour
{
    public static int CurrentWave { get; private set; } = 1;

    public event Action<int, int> WaveChanged;
    public event Action<int> WaveStarted;
    public event Action<int> WaveCleared;
    public event Action<float, float> WaveTimeChanged;
    public event Action<int> CardSelectionRequested;

    [Tooltip("최대 웨이브 수")]
    [SerializeField] private int maxWave = 10;

    [Tooltip("시작 웨이브 번호")]
    [SerializeField] private int startWave = 1;

    [Tooltip("다음 웨이브 시작까지 대기 시간(초)")]
    [SerializeField] private float nextWaveDelaySeconds = 3f;

    [Tooltip("웨이브마다 적용할 제한 시간(초)")]
    [SerializeField] private float waveDurationSeconds = 30f;

    [Tooltip("홀수 웨이브 클리어 후 카드 선택 팝업을 사용할지 여부")]
    [SerializeField] private bool useCardSelectionOnOddWaveClear = true;

    [Tooltip("카드 선택 팝업 컨트롤러")]
    [SerializeField] private SC_WaveCardPopup waveCardPopup;

    public int MaxWave => Mathf.Max(1, maxWave);
    public float WaveDurationSeconds => Mathf.Max(0f, waveDurationSeconds);
    public float RemainingWaveTime => Mathf.Max(0f, remainingWaveTime);

    private bool isWaitingNextWave;
    private float remainingWaveTime;
    private bool isWaveTimerRunning;
    private bool isWaitingCardSelection;

    private void Start()
    {
        CurrentWave = Mathf.Clamp(startWave, 1, MaxWave);
        RaiseWaveChanged();
        ResetWaveTimer();
        WaveStarted?.Invoke(CurrentWave);
    }

    private void Update()
    {
        if (!isWaveTimerRunning)
        {
            return;
        }

        if (isWaitingNextWave)
        {
            return;
        }

        if (remainingWaveTime <= 0f)
        {
            isWaveTimerRunning = false;
            RaiseWaveTimeChanged();
            return;
        }

        remainingWaveTime = Mathf.Max(0f, remainingWaveTime - Time.deltaTime);
        RaiseWaveTimeChanged();
    }

    public void NotifyWaveCleared(int clearedWave)
    {
        if (clearedWave != CurrentWave)
        {
            return;
        }

        isWaveTimerRunning = false;
        remainingWaveTime = 0f;
        RaiseWaveTimeChanged();

        WaveCleared?.Invoke(clearedWave);

        if (CurrentWave >= MaxWave || isWaitingNextWave)
        {
            return;
        }

        StartCoroutine(CoStartNextWaveAfterDelay());
    }

    public void NotifyCardSelected()
    {
        if (!isWaitingCardSelection)
        {
            return;
        }

        isWaitingCardSelection = false;
        Time.timeScale = 1f;
        StartNextWave();
    }

    private IEnumerator CoStartNextWaveAfterDelay()
    {
        isWaitingNextWave = true;
        yield return new WaitForSeconds(Mathf.Max(0f, nextWaveDelaySeconds));
        isWaitingNextWave = false;

        int nextWave = Mathf.Min(CurrentWave + 1, MaxWave);
        if (ShouldOpenCardSelection(CurrentWave, nextWave))
        {
            if (waveCardPopup == null)
            {
                waveCardPopup = FindFirstObjectByType<SC_WaveCardPopup>();
            }

            if (waveCardPopup == null && CardSelectionRequested == null)
            {
                StartNextWave();
                yield break;
            }

            isWaitingCardSelection = true;
            CancelAllPendingCharacterDrags();
            Time.timeScale = 0f;
            if (waveCardPopup != null)
            {
                waveCardPopup.OpenCardSelection(nextWave);
            }
            CardSelectionRequested?.Invoke(nextWave);
            yield break;
        }

        StartNextWave();
    }

    private void RaiseWaveChanged()
    {
        WaveChanged?.Invoke(CurrentWave, MaxWave);
    }

    private void ResetWaveTimer()
    {
        remainingWaveTime = Mathf.Max(0f, waveDurationSeconds);
        isWaveTimerRunning = remainingWaveTime > 0f;
        RaiseWaveTimeChanged();
    }

    private void RaiseWaveTimeChanged()
    {
        WaveTimeChanged?.Invoke(RemainingWaveTime, WaveDurationSeconds);
    }

    private bool ShouldOpenCardSelection(int clearedWave, int nextWave)
    {
        if (!useCardSelectionOnOddWaveClear)
        {
            return false;
        }

        if (nextWave > MaxWave)
        {
            return false;
        }

        return clearedWave % 2 == 1;
    }

    private void StartNextWave()
    {
        CurrentWave = Mathf.Min(CurrentWave + 1, MaxWave);
        RaiseWaveChanged();
        ResetWaveTimer();
        WaveStarted?.Invoke(CurrentWave);
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
