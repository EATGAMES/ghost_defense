using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class SC_WaveUI : MonoBehaviour
{
    [Tooltip("웨이브를 표시할 TextMeshProUGUI")]
    [SerializeField] private TMP_Text waveText;

    [Tooltip("웨이브 데이터를 제공할 매니저")]
    [SerializeField] private SC_WaveManager waveManager;

    private void Awake()
    {
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<SC_WaveManager>();
        }
    }

    private void OnEnable()
    {
        if (waveManager == null)
        {
            return;
        }

        waveManager.WaveChanged += OnWaveChanged;
        OnWaveChanged(SC_WaveManager.CurrentWave, waveManager.MaxWave);
    }

    private void OnDisable()
    {
        if (waveManager == null)
        {
            return;
        }

        waveManager.WaveChanged -= OnWaveChanged;
    }

    private void OnWaveChanged(int currentWave, int maxWave)
    {
        if (waveText == null)
        {
            return;
        }

        waveText.text = string.Format("{0}/{1}", currentWave, Mathf.Max(1, maxWave));
    }
}
