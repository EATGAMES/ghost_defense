using UnityEngine;

[DisallowMultipleComponent]
public class SC_CameraShake : MonoBehaviour
{
    [Tooltip("흔들림 위치 오프셋에 곱할 전체 배율입니다.")]
    [SerializeField] private float shakeScale = 1f;

    [Tooltip("흔들림 노이즈가 바뀌는 속도입니다.")]
    [SerializeField] private float shakeFrequency = 48f;

    [Tooltip("흔들림이 끝난 뒤 원래 위치로 복귀하는 속도입니다.")]
    [SerializeField] private float returnSpeed = 35f;

    private Vector3 baseLocalPosition;
    private float shakePower;
    private float shakeDuration;
    private float shakeElapsed;
    private float noiseSeedX;
    private float noiseSeedY;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        noiseSeedX = Random.Range(0f, 100f);
        noiseSeedY = Random.Range(100f, 200f);
    }

    private void LateUpdate()
    {
        if (shakeElapsed < shakeDuration)
        {
            shakeElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(shakeElapsed / Mathf.Max(0.01f, shakeDuration));
            float fade = 1f - progress;
            float time = Time.time * Mathf.Max(0f, shakeFrequency);
            float offsetX = (Mathf.PerlinNoise(noiseSeedX, time) - 0.5f) * 2f;
            float offsetY = (Mathf.PerlinNoise(noiseSeedY, time) - 0.5f) * 2f;
            Vector3 offset = new Vector3(offsetX, offsetY, 0f) * Mathf.Max(0f, shakePower) * Mathf.Max(0f, shakeScale) * fade;

            transform.localPosition = baseLocalPosition + offset;
            return;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, baseLocalPosition, Time.deltaTime * Mathf.Max(0f, returnSpeed));
    }

    public void Play(float power, float duration)
    {
        float safePower = Mathf.Max(0f, power);
        float safeDuration = Mathf.Max(0f, duration);
        if (safePower <= 0f || safeDuration <= 0f)
        {
            return;
        }

        shakePower = Mathf.Max(shakePower, safePower);
        shakeDuration = Mathf.Max(shakeDuration - shakeElapsed, safeDuration);
        shakeElapsed = 0f;
        noiseSeedX = Random.Range(0f, 100f);
        noiseSeedY = Random.Range(100f, 200f);
    }
}
