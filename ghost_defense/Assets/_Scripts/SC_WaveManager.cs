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

    [Tooltip("최대 웨이브 수")]
    [SerializeField] private int maxWave = 10;

    [Tooltip("시작 웨이브 번호")]
    [SerializeField] private int startWave = 1;

    [Tooltip("다음 웨이브 시작까지 대기 시간(초)")]
    [SerializeField] private float nextWaveDelaySeconds = 3f;

    [Tooltip("웨이브 클리어 시 슛 카운트를 추가할 스포너")]
    [SerializeField] private SC_BattleCharacterSpawner battleCharacterSpawner;

    [Tooltip("웨이브 클리어마다 추가할 슛 카운트")]
    [SerializeField] private int addShootCountPerWaveClear = 10;

    public int MaxWave => Mathf.Max(1, maxWave);

    private bool isWaitingNextWave;

    private void Start()
    {
        CurrentWave = Mathf.Clamp(startWave, 1, MaxWave);
        RaiseWaveChanged();
        WaveStarted?.Invoke(CurrentWave);
    }

    public void NotifyWaveCleared(int clearedWave)
    {
        if (clearedWave != CurrentWave)
        {
            return;
        }

        if (battleCharacterSpawner != null)
        {
            battleCharacterSpawner.AddShootCount(Mathf.Max(0, addShootCountPerWaveClear));
        }

        WaveCleared?.Invoke(clearedWave);

        if (CurrentWave >= MaxWave || isWaitingNextWave)
        {
            return;
        }

        StartCoroutine(CoStartNextWaveAfterDelay());
    }

    private IEnumerator CoStartNextWaveAfterDelay()
    {
        isWaitingNextWave = true;
        yield return new WaitForSeconds(Mathf.Max(0f, nextWaveDelaySeconds));

        CurrentWave = Mathf.Min(CurrentWave + 1, MaxWave);
        RaiseWaveChanged();
        WaveStarted?.Invoke(CurrentWave);
        isWaitingNextWave = false;
    }

    private void RaiseWaveChanged()
    {
        WaveChanged?.Invoke(CurrentWave, MaxWave);
    }
}
