using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class SC_MergeFx : MonoBehaviour
{
    [Header("파티클 시스템")]
    [Tooltip("결합 순간에 짧게 터지는 중심 플래시 파티클입니다.")]
    [SerializeField] private ParticleSystem centerFlash;

    [Tooltip("위로 빨려 올라가는 메인 빛줄기 파티클입니다.")]
    [SerializeField] private ParticleSystem mainStreak;

    [Tooltip("메인 빛줄기가 지나간 자리에 뒤늦게 생기는 서브 잔광 파티클입니다.")]
    [SerializeField] private ParticleSystem afterGlow;

    [Tooltip("결합 위치 주변에서 느리게 따라 올라가는 작은 스파크 파티클입니다.")]
    [SerializeField] private ParticleSystem smallSpark;

    [Tooltip("이펙트 전체를 감싸는 부드러운 발광 파티클입니다.")]
    [SerializeField] private ParticleSystem softGlow;

    [Header("재생")]
    [Tooltip("오브젝트가 활성화될 때 자동으로 결합 이펙트를 재생할지 여부입니다.")]
    [SerializeField] private bool playOnEnable = true;

    [Tooltip("이펙트 재생이 끝난 뒤 오브젝트를 비활성화할지 여부입니다.")]
    [SerializeField] private bool deactivateOnComplete;

    [Tooltip("전체 이펙트가 종료되었다고 판단할 시간입니다.")]
    [SerializeField] private float totalLifetime = 0.65f;

    [Header("도착 지점")]
    [Tooltip("메인 빛줄기가 향할 도착 지점입니다. 비워두면 도착 지점 이름으로 씬에서 찾습니다.")]
    [SerializeField] private Transform destinationPoint;

    [Tooltip("도착 지점 Transform이 비어 있을 때 씬에서 찾을 오브젝트 이름입니다.")]
    [SerializeField] private string destinationPointName = "OBJ_ParticlePoint";

    [Tooltip("재생 시작 시 도착 지점 방향으로 이펙트 루트를 회전할지 여부입니다.")]
    [SerializeField] private bool rotateToDestination = true;

    [Tooltip("현재 메인 빛줄기 속도 커브가 기본 배율일 때 도달하는 기준 거리입니다.")]
    [SerializeField] private float baseMainTravelDistance = 2.05f;

    [Tooltip("도착 지점까지 메인 빛줄기가 도달하는 데 걸리는 시간입니다.")]
    [SerializeField] private float mainArrivalTime = 0.65f;

    [Tooltip("도착 거리 보정에 사용할 최소 속도 배율입니다.")]
    [SerializeField] private float minMainSpeedMultiplier = 0.35f;

    [Tooltip("도착 거리 보정에 사용할 최대 속도 배율입니다.")]
    [SerializeField] private float maxMainSpeedMultiplier = 8f;

    [Tooltip("메인 파티클을 코드로 곡선 궤적에 맞춰 도착 지점까지 이동시킬지 여부입니다.")]
    [SerializeField] private bool driveMainParticlesToDestination = true;

    [Tooltip("메인 파티클 시작점이 머지 위치 주변으로 퍼지는 반경입니다.")]
    [SerializeField] private float burstRadius = 0.34f;

    [Tooltip("메인 파티클이 시작할 때 좌우로 튀는 최소 거리입니다.")]
    [SerializeField] private float firstSideOffsetMin = 0.75f;

    [Tooltip("메인 파티클이 시작할 때 좌우로 튀는 최대 거리입니다.")]
    [SerializeField] private float firstSideOffsetMax = 1.8f;

    [Tooltip("메인 파티클이 중간에 반대 방향으로 꺾이는 최소 거리입니다.")]
    [SerializeField] private float secondSideOffsetMin = 0.7f;

    [Tooltip("메인 파티클이 중간에 반대 방향으로 꺾이는 최대 거리입니다.")]
    [SerializeField] private float secondSideOffsetMax = 2f;

    [Tooltip("도착 지점 주변에 살짝 흩어지는 반경입니다.")]
    [SerializeField] private float destinationScatterRadius = 0.18f;

    [Header("잔광 생성")]
    [Tooltip("메인 파티클이 이 거리 이상 이동했을 때 지나간 위치에 잔광 생성을 예약합니다.")]
    [SerializeField] private float trailSampleDistance = 0.055f;

    [Tooltip("한 메인 파티클이 잔광을 다시 예약하기 전까지 기다리는 최소 시간입니다.")]
    [SerializeField] private float trailSampleInterval = 0.01f;

    [Tooltip("잔광이 메인 궤적보다 늦게 생성되는 최소 지연 시간입니다.")]
    [SerializeField] private float afterGlowDelayMin = 0.03f;

    [Tooltip("잔광이 메인 궤적보다 늦게 생성되는 최대 지연 시간입니다.")]
    [SerializeField] private float afterGlowDelayMax = 0.08f;

    [Tooltip("메인 파티클 한 번의 샘플에서 생성할 잔광 최소 개수입니다.")]
    [SerializeField] private int afterGlowCountMin = 3;

    [Tooltip("메인 파티클 한 번의 샘플에서 생성할 잔광 최대 개수입니다.")]
    [SerializeField] private int afterGlowCountMax = 6;

    [Tooltip("동시에 보이거나 예약될 수 있는 잔광 최대 개수입니다.")]
    [SerializeField] private int maxAfterGlowCount = 80;

    [Header("잔광 움직임")]
    [Tooltip("잔광 생성 위치의 가로 랜덤 오프셋입니다.")]
    [SerializeField] private Vector2 afterGlowOffsetX = new Vector2(-0.08f, 0.08f);

    [Tooltip("잔광 생성 위치의 세로 랜덤 오프셋입니다.")]
    [SerializeField] private Vector2 afterGlowOffsetY = new Vector2(-0.05f, 0.10f);

    [Tooltip("잔광의 가로 이동 속도 범위입니다.")]
    [SerializeField] private Vector2 afterGlowVelocityX = new Vector2(-0.15f, 0.15f);

    [Tooltip("잔광의 위쪽 이동 속도 범위입니다.")]
    [SerializeField] private Vector2 afterGlowVelocityY = new Vector2(0.20f, 0.60f);

    [Tooltip("메인 이동 방향을 따라 잔광이 이어지는 속도 비율입니다.")]
    [SerializeField] private float afterGlowFollowVelocityScale = 0.28f;

    [Tooltip("잔광의 최소 생존 시간입니다.")]
    [SerializeField] private float afterGlowLifetimeMin = 0.18f;

    [Tooltip("잔광의 최대 생존 시간입니다.")]
    [SerializeField] private float afterGlowLifetimeMax = 0.35f;

    [Tooltip("잔광의 최소 크기입니다.")]
    [SerializeField] private float afterGlowSizeMin = 0.035f;

    [Tooltip("잔광의 최대 크기입니다.")]
    [SerializeField] private float afterGlowSizeMax = 0.085f;

    [Tooltip("잔광이 깜빡이는 횟수의 최소값입니다.")]
    [SerializeField] private int afterGlowBlinkCountMin = 2;

    [Tooltip("잔광이 깜빡이는 횟수의 최대값입니다.")]
    [SerializeField] private int afterGlowBlinkCountMax = 4;

    [Tooltip("깜빡임이 가장 밝을 때의 크기 배율입니다.")]
    [SerializeField] private float afterGlowBlinkSizeMultiplier = 1.35f;

    [Header("잔광 색상")]
    [Tooltip("잔광에서 가장 자주 사용하는 노란색입니다.")]
    [SerializeField] private Color mainAfterGlowColor = new Color32(255, 225, 90, 204);

    [Tooltip("잔광에서 보조로 사용하는 따뜻한 주황색입니다.")]
    [SerializeField] private Color warmAfterGlowColor = new Color32(255, 177, 59, 204);

    [Tooltip("낮은 확률로 섞이는 분홍색 잔광입니다.")]
    [SerializeField] private Color rarePinkAfterGlowColor = new Color32(255, 122, 200, 204);

    [Tooltip("낮은 확률로 섞이는 보라색 잔광입니다.")]
    [SerializeField] private Color rarePurpleAfterGlowColor = new Color32(184, 108, 255, 204);

    [Tooltip("분홍색 또는 보라색 잔광이 선택될 확률입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float rareColorChance = 0.22f;

    [Header("경로 꼬리")]
    [Tooltip("메인 빛줄기의 밝은 중심 꼬리 LineRenderer입니다. 비워두면 런타임에 자동 생성합니다.")]
    [SerializeField] private LineRenderer coreTrail;

    [Tooltip("메인 빛줄기의 넓은 발광 꼬리 LineRenderer입니다. 비워두면 런타임에 자동 생성합니다.")]
    [SerializeField] private LineRenderer glowTrail;

    [Tooltip("메인 빛줄기에 핑크와 보라색을 섞어주는 보조 꼬리 LineRenderer입니다. 비워두면 런타임에 자동 생성합니다.")]
    [SerializeField] private LineRenderer accentTrail;

    [Tooltip("경로 꼬리에 사용할 가산 블렌딩 머티리얼입니다. 비워두면 메인 파티클 머티리얼을 사용합니다.")]
    [SerializeField] private Material trailMaterial;

    [Tooltip("밝은 중심 꼬리의 두께입니다.")]
    [SerializeField] private float coreTrailWidth = 0.16f;

    [Tooltip("넓은 발광 꼬리의 두께입니다.")]
    [SerializeField] private float glowTrailWidth = 0.42f;

    [Tooltip("핑크와 보라색 보조 꼬리의 두께입니다.")]
    [SerializeField] private float accentTrailWidth = 0.28f;

    [Tooltip("꼬리 점을 새로 기록할 최소 이동 거리입니다.")]
    [SerializeField] private float trailPointMinDistance = 0.012f;

    [Tooltip("헤드 바로 뒤가 아니라 실제 지나간 경로에만 꼬리를 그리기 위한 지연 시간입니다.")]
    [SerializeField] private float minTrailRenderAge = 0.035f;

    [Tooltip("기록된 꼬리 점이 화면에 남아 있는 시간입니다.")]
    [SerializeField] private float trailVisibleLifetime = 0.38f;

    [Tooltip("메인 경로 중 꼬리로 표시할 진행도 길이입니다.")]
    [SerializeField] private float trailProgressLength = 0.24f;

    [Tooltip("곡선 꼬리를 부드럽게 그릴 샘플 개수입니다.")]
    [SerializeField] private int trailCurveSampleCount = 28;

    [Tooltip("메인 파티클이 사라진 뒤 꼬리가 부드럽게 사라지는 시간입니다.")]
    [SerializeField] private float trailFadeOutDuration = 0.28f;

    [Tooltip("넓은 글로우 꼬리의 최대 투명도 비율입니다. 낮을수록 흐림이 줄어듭니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float glowTrailAlphaScale = 0.35f;

    [Tooltip("핑크와 보라 보조 꼬리의 최대 투명도 비율입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float accentTrailAlphaScale = 0.5f;

    [Tooltip("꼬리에 저장할 최대 위치 개수입니다.")]
    [SerializeField] private int maxTrailPointCount = 80;

    private readonly Dictionary<uint, Vector3> previousMainPositions = new Dictionary<uint, Vector3>();
    private readonly Dictionary<uint, float> nextSampleTimes = new Dictionary<uint, float>();
    private readonly Dictionary<uint, MainParticlePath> mainParticlePaths = new Dictionary<uint, MainParticlePath>();
    private readonly List<uint> aliveParticleSeeds = new List<uint>();
    private readonly List<PendingAfterGlow> pendingAfterGlows = new List<PendingAfterGlow>();
    private readonly List<BlinkingAfterGlow> blinkingAfterGlows = new List<BlinkingAfterGlow>();
    private readonly List<MainTrailPoint> mainTrailPoints = new List<MainTrailPoint>();

    private ParticleSystem.Particle[] mainParticles;
    private Vector3[] trailPositionBuffer;
    private ParticleSystem.MinMaxCurve baseMainVelocityY;
    private ParticleSystem.MinMaxCurve baseMainStartLifetime;
    private Vector3 cachedDestinationWorldPosition;
    private MainParticlePath activeMainPath;
    private float activeMainProgress;
    private bool hasDestination;
    private bool hasActiveMainPath;
    private float playStartTime;
    private float lastMainParticleSeenTime;
    private bool isPlaying;

    public float TotalLifetime => Mathf.Max(0f, totalLifetime)
        + Mathf.Max(0f, Mathf.Max(afterGlowDelayMin, afterGlowDelayMax))
        + Mathf.Max(0f, afterGlowLifetimeMax)
        + Mathf.Max(0f, trailVisibleLifetime)
        + Mathf.Max(0f, trailFadeOutDuration);

    private struct PendingAfterGlow
    {
        public float EmitTime;
        public Vector3 WorldPosition;
        public Vector3 WorldVelocity;
        public float Lifetime;
        public float Size;
        public Color Color;
    }

    private struct BlinkingAfterGlow
    {
        public float StartTime;
        public float Lifetime;
        public float NextEmitTime;
        public int BlinkCount;
        public Vector3 StartWorldPosition;
        public Vector3 WorldVelocity;
        public float Size;
        public Color Color;
    }

    private struct MainParticlePath
    {
        public Vector3 StartPosition;
        public Vector3 FirstControlPosition;
        public Vector3 SecondControlPosition;
        public Vector3 EndPosition;
    }

    private struct MainTrailPoint
    {
        public Vector3 Position;
        public float CreatedTime;
    }

    private void Reset()
    {
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        if (particleSystems.Length > 0)
        {
            centerFlash = particleSystems[0];
        }

        if (particleSystems.Length > 1)
        {
            mainStreak = particleSystems[1];
        }

        if (particleSystems.Length > 2)
        {
            afterGlow = particleSystems[2];
        }

        if (particleSystems.Length > 3)
        {
            smallSpark = particleSystems[3];
        }

        if (particleSystems.Length > 4)
        {
            softGlow = particleSystems[4];
        }
    }

    private void Awake()
    {
        CacheMainStreakDefaults();
        EnsureMainParticleBuffer();
        PrepareMainTrailRenderers();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        TrackMainStreakParticles();
        EmitPendingAfterGlows();
        UpdateBlinkingAfterGlows();
        UpdateMainTrailRenderers();

        if (deactivateOnComplete && Time.time - playStartTime >= TotalLifetime)
        {
            isPlaying = false;
            gameObject.SetActive(false);
        }
    }

    public void Play()
    {
        Transform targetPoint = ResolveDestinationPoint();
        AlignToDestination(targetPoint);
        ApplyDestinationTravel(targetPoint);
        hasDestination = targetPoint != null;
        cachedDestinationWorldPosition = hasDestination ? targetPoint.position : transform.position;

        playStartTime = Time.time;
        lastMainParticleSeenTime = playStartTime;
        isPlaying = true;

        previousMainPositions.Clear();
        nextSampleTimes.Clear();
        mainParticlePaths.Clear();
        aliveParticleSeeds.Clear();
        pendingAfterGlows.Clear();
        blinkingAfterGlows.Clear();
        ClearMainTrail();
        hasActiveMainPath = false;

        EnsureMainParticleBuffer();
        PrepareMainTrailRenderers();
        ClearParticleSystem(centerFlash);
        ClearParticleSystem(mainStreak);
        ClearParticleSystem(afterGlow);
        ClearParticleSystem(smallSpark);
        ClearParticleSystem(softGlow);

        PlayParticleSystem(centerFlash);
        PlayParticleSystem(mainStreak);
        PlayParticleSystem(afterGlow);
        PlayParticleSystem(smallSpark);
        PlayParticleSystem(softGlow);
    }

    public void PlayAt(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        Play();
    }

    public void PlayAt(Vector3 worldPosition, Transform targetPoint)
    {
        transform.position = worldPosition;
        destinationPoint = targetPoint;
        Play();
    }

    public void StopAndClear()
    {
        isPlaying = false;

        previousMainPositions.Clear();
        nextSampleTimes.Clear();
        mainParticlePaths.Clear();
        aliveParticleSeeds.Clear();
        pendingAfterGlows.Clear();
        blinkingAfterGlows.Clear();
        ClearMainTrail();
        hasActiveMainPath = false;

        ClearParticleSystem(centerFlash);
        ClearParticleSystem(mainStreak);
        ClearParticleSystem(afterGlow);
        ClearParticleSystem(smallSpark);
        ClearParticleSystem(softGlow);
    }

    private void TrackMainStreakParticles()
    {
        if (mainStreak == null)
        {
            return;
        }

        EnsureMainParticleBuffer();

        int particleCount = mainStreak.GetParticles(mainParticles);
        aliveParticleSeeds.Clear();
        if (particleCount > 0)
        {
            lastMainParticleSeenTime = Time.time;
        }

        for (int i = 0; i < particleCount; i++)
        {
            ParticleSystem.Particle particle = mainParticles[i];
            uint seed = particle.randomSeed;
            DriveMainParticleAlongPath(seed, ref particle);
            Vector3 worldPosition = ParticlePositionToWorld(mainStreak, particle.position);
            aliveParticleSeeds.Add(seed);

            if (!previousMainPositions.TryGetValue(seed, out Vector3 previousWorldPosition))
            {
                previousMainPositions[seed] = worldPosition;
                nextSampleTimes[seed] = Time.time + trailSampleInterval;
                AddMainTrailPoint(worldPosition, true);
                continue;
            }

            Vector3 mainDelta = worldPosition - previousWorldPosition;
            float nextSampleTime = nextSampleTimes.TryGetValue(seed, out float storedNextSampleTime) ? storedNextSampleTime : 0f;
            if (Time.time >= nextSampleTime && mainDelta.magnitude >= Mathf.Max(0.001f, trailSampleDistance))
            {
                if (afterGlow != null)
                {
                    EmitAfterGlowsAlongSegment(previousWorldPosition, worldPosition, mainDelta);
                }

                nextSampleTimes[seed] = Time.time + Mathf.Max(0f, trailSampleInterval);
            }

            AddMainTrailPoint(worldPosition, false);
            previousMainPositions[seed] = worldPosition;
            mainParticles[i] = particle;
        }

        if (driveMainParticlesToDestination && hasDestination)
        {
            mainStreak.SetParticles(mainParticles, particleCount);
        }

        RemoveDeadParticleSeeds();
    }

    private void EmitAfterGlowsAlongSegment(Vector3 segmentStart, Vector3 segmentEnd, Vector3 mainDelta)
    {
        if (afterGlow == null || afterGlow.particleCount >= maxAfterGlowCount)
        {
            return;
        }

        int minCount = Mathf.Max(0, afterGlowCountMin);
        int maxCount = Mathf.Max(minCount, afterGlowCountMax);
        int spawnCount = Random.Range(minCount, maxCount + 1);
        Vector3 inheritedVelocity = GetWorldVelocity(mainDelta);

        for (int i = 0; i < spawnCount; i++)
        {
            if (afterGlow.particleCount >= maxAfterGlowCount)
            {
                return;
            }

            BlinkingAfterGlow afterGlowData = new BlinkingAfterGlow
            {
                StartTime = Time.time,
                Lifetime = RandomRange(afterGlowLifetimeMin, afterGlowLifetimeMax),
                NextEmitTime = Time.time,
                BlinkCount = Random.Range(Mathf.Max(1, afterGlowBlinkCountMin), Mathf.Max(afterGlowBlinkCountMin, afterGlowBlinkCountMax) + 1),
                StartWorldPosition = Vector3.Lerp(segmentStart, segmentEnd, Random.value) + GetWorldOffset(),
                WorldVelocity = inheritedVelocity + GetWorldSparkleScatterVelocity(),
                Size = RandomRange(afterGlowSizeMin, afterGlowSizeMax),
                Color = PickAfterGlowColor()
            };

            blinkingAfterGlows.Add(afterGlowData);
        }
    }

    private void UpdateBlinkingAfterGlows()
    {
        if (afterGlow == null || blinkingAfterGlows.Count == 0)
        {
            return;
        }

        for (int i = blinkingAfterGlows.Count - 1; i >= 0; i--)
        {
            BlinkingAfterGlow glow = blinkingAfterGlows[i];
            float elapsed = Time.time - glow.StartTime;
            if (elapsed >= glow.Lifetime)
            {
                blinkingAfterGlows.RemoveAt(i);
                continue;
            }

            if (Time.time < glow.NextEmitTime)
            {
                continue;
            }

            float normalizedTime = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, glow.Lifetime));
            float blinkWave = Mathf.Abs(Mathf.Sin(normalizedTime * Mathf.PI * Mathf.Max(1, glow.BlinkCount)));
            float alpha = (1f - normalizedTime) * Mathf.Lerp(0.35f, 1f, blinkWave);
            float size = glow.Size * Mathf.Lerp(0.65f, Mathf.Max(0.65f, afterGlowBlinkSizeMultiplier), blinkWave);
            Color color = glow.Color;
            color.a *= alpha;

            PendingAfterGlow emitGlow = new PendingAfterGlow
            {
                EmitTime = Time.time,
                WorldPosition = glow.StartWorldPosition + glow.WorldVelocity * elapsed,
                WorldVelocity = Vector3.zero,
                Lifetime = 0.045f,
                Size = size,
                Color = color
            };

            EmitAfterGlow(emitGlow);
            glow.NextEmitTime = Time.time + 0.035f;
            blinkingAfterGlows[i] = glow;
        }
    }

    private void EmitPendingAfterGlows()
    {
        if (afterGlow == null || pendingAfterGlows.Count == 0)
        {
            return;
        }

        for (int i = pendingAfterGlows.Count - 1; i >= 0; i--)
        {
            PendingAfterGlow pendingAfterGlow = pendingAfterGlows[i];
            if (Time.time < pendingAfterGlow.EmitTime)
            {
                continue;
            }

            if (afterGlow.particleCount < maxAfterGlowCount)
            {
                EmitAfterGlow(pendingAfterGlow);
            }

            pendingAfterGlows.RemoveAt(i);
        }
    }

    private void EmitAfterGlow(PendingAfterGlow pendingAfterGlow)
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = WorldToParticlePosition(afterGlow, pendingAfterGlow.WorldPosition),
            velocity = WorldToParticleVector(afterGlow, pendingAfterGlow.WorldVelocity),
            startLifetime = Mathf.Max(0.01f, pendingAfterGlow.Lifetime),
            startSize = Mathf.Max(0.001f, pendingAfterGlow.Size),
            startColor = pendingAfterGlow.Color
        };

        afterGlow.Emit(emitParams, 1);
    }

    private void DriveMainParticleAlongPath(uint seed, ref ParticleSystem.Particle particle)
    {
        if (!driveMainParticlesToDestination || !hasDestination || mainStreak == null)
        {
            return;
        }

        MainParticlePath path = GetOrCreateMainParticlePath(seed, particle);
        float lifetime = Mathf.Max(0.01f, particle.startLifetime);
        float progress = Mathf.Clamp01(1f - particle.remainingLifetime / lifetime);
        float easedProgress = EaseAbsorbProgress(progress);
        Vector3 worldPosition = EvaluateCubicBezier(path.StartPosition, path.FirstControlPosition, path.SecondControlPosition, path.EndPosition, easedProgress);
        particle.position = WorldToParticlePosition(mainStreak, worldPosition);
        activeMainPath = path;
        activeMainProgress = easedProgress;
        hasActiveMainPath = true;
    }

    private MainParticlePath GetOrCreateMainParticlePath(uint seed, ParticleSystem.Particle particle)
    {
        if (mainParticlePaths.TryGetValue(seed, out MainParticlePath path))
        {
            return path;
        }

        Vector3 startPosition = transform.position;
        Vector3 targetDirection = cachedDestinationWorldPosition - transform.position;
        targetDirection.z = 0f;
        if (targetDirection.sqrMagnitude <= 0.0001f)
        {
            targetDirection = transform.up;
        }

        Vector3 forward = targetDirection.normalized;
        Vector3 side = new Vector3(-forward.y, forward.x, 0f);
        System.Random random = new System.Random(unchecked((int)seed));
        float sideSign = random.NextDouble() < 0.5 ? -1f : 1f;

        float distance = Vector3.Distance(transform.position, cachedDestinationWorldPosition);
        float firstSideOffset = Mathf.Lerp(firstSideOffsetMin, Mathf.Max(firstSideOffsetMin, firstSideOffsetMax), (float)random.NextDouble()) * sideSign;
        float secondSideOffset = Mathf.Lerp(secondSideOffsetMin, Mathf.Max(secondSideOffsetMin, secondSideOffsetMax), (float)random.NextDouble()) * -sideSign;
        float firstForwardOffset = distance * Mathf.Lerp(0.08f, 0.16f, (float)random.NextDouble());
        float secondForwardOffset = distance * Mathf.Lerp(0.44f, 0.62f, (float)random.NextDouble());
        Vector3 endPosition = cachedDestinationWorldPosition + side * Mathf.Lerp(-destinationScatterRadius, destinationScatterRadius, (float)random.NextDouble());
        Vector3 firstControlPosition = transform.position + forward * firstForwardOffset + side * (firstSideOffset + Mathf.Max(0f, burstRadius) * sideSign);
        Vector3 secondControlPosition = transform.position + forward * secondForwardOffset + side * secondSideOffset;

        path = new MainParticlePath
        {
            StartPosition = startPosition,
            FirstControlPosition = firstControlPosition,
            SecondControlPosition = secondControlPosition,
            EndPosition = endPosition
        };

        mainParticlePaths[seed] = path;
        return path;
    }

    private Vector3 GetWorldOffset()
    {
        Vector3 localOffset = new Vector3(
            RandomRange(afterGlowOffsetX),
            RandomRange(afterGlowOffsetY),
            0f);

        return transform.TransformVector(localOffset);
    }

    private Vector3 GetWorldVelocity(Vector3 mainDelta)
    {
        Vector3 followVelocity = Vector3.zero;
        float deltaTime = Mathf.Max(0.001f, Time.deltaTime);
        if (mainDelta.sqrMagnitude > 0.000001f)
        {
            followVelocity = mainDelta / deltaTime * Mathf.Max(0f, afterGlowFollowVelocityScale);
        }

        return followVelocity;
    }

    private Vector3 GetWorldSparkleScatterVelocity()
    {
        Vector3 localVelocity = new Vector3(
            RandomRange(afterGlowVelocityX),
            RandomRange(afterGlowVelocityY),
            0f);

        return transform.TransformVector(localVelocity);
    }

    private Color PickAfterGlowColor()
    {
        if (Random.value < rareColorChance)
        {
            return Random.value < 0.5f ? rarePinkAfterGlowColor : rarePurpleAfterGlowColor;
        }

        return Random.value < 0.65f ? mainAfterGlowColor : warmAfterGlowColor;
    }

    private static Vector3 EvaluateCubicBezier(Vector3 start, Vector3 firstControl, Vector3 secondControl, Vector3 end, float progress)
    {
        float inverseProgress = 1f - progress;
        return inverseProgress * inverseProgress * inverseProgress * start
            + 3f * inverseProgress * inverseProgress * progress * firstControl
            + 3f * inverseProgress * progress * progress * secondControl
            + progress * progress * progress * end;
    }

    private static float EaseInCubic(float value)
    {
        return value * value * value;
    }

    private static float EaseAbsorbProgress(float value)
    {
        float delayedProgress = Mathf.Clamp01((value - 0.04f) / 0.96f);
        float smoothProgress = delayedProgress * delayedProgress * (3f - 2f * delayedProgress);
        return Mathf.Pow(smoothProgress, 1.18f);
    }

    private void PrepareMainTrailRenderers()
    {
        DisableMainParticleTrailModule();

        Material resolvedTrailMaterial = ResolveTrailMaterial();
        coreTrail = EnsureTrailRenderer(coreTrail, "VFX_MainTrail_Core", 2014, resolvedTrailMaterial);
        accentTrail = EnsureTrailRenderer(accentTrail, "VFX_MainTrail_Accent", 2013, resolvedTrailMaterial);
        glowTrail = EnsureTrailRenderer(glowTrail, "VFX_MainTrail_Glow", 2012, resolvedTrailMaterial);

        ConfigureTrailRenderer(coreTrail, Mathf.Max(0.001f, coreTrailWidth), CreateCoreTrailGradient(1f));
        ConfigureTrailRenderer(accentTrail, Mathf.Max(0.001f, accentTrailWidth), CreateAccentTrailGradient(accentTrailAlphaScale));
        ConfigureTrailRenderer(glowTrail, Mathf.Max(0.001f, glowTrailWidth), CreateGlowTrailGradient(glowTrailAlphaScale));
        ApplyTrailPositions(0);
    }

    private void DisableMainParticleTrailModule()
    {
        if (mainStreak == null)
        {
            return;
        }

        ParticleSystem.TrailModule trailModule = mainStreak.trails;
        trailModule.enabled = false;
    }

    private Material ResolveTrailMaterial()
    {
        if (trailMaterial != null)
        {
            return trailMaterial;
        }

        if (mainStreak == null)
        {
            return null;
        }

        ParticleSystemRenderer particleRenderer = mainStreak.GetComponent<ParticleSystemRenderer>();
        return particleRenderer != null ? particleRenderer.sharedMaterial : null;
    }

    private LineRenderer EnsureTrailRenderer(LineRenderer renderer, string objectName, int sortingOrder, Material resolvedTrailMaterial)
    {
        if (renderer == null)
        {
            Transform existingChild = transform.Find(objectName);
            renderer = existingChild != null ? existingChild.GetComponent<LineRenderer>() : null;
        }

        if (renderer == null)
        {
            GameObject trailObject = new GameObject(objectName);
            trailObject.transform.SetParent(transform, false);
            renderer = trailObject.AddComponent<LineRenderer>();
        }

        renderer.sharedMaterial = resolvedTrailMaterial;
        renderer.sortingOrder = sortingOrder;
        renderer.sortingLayerID = 0;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.useWorldSpace = true;
        renderer.alignment = LineAlignment.View;
        renderer.textureMode = LineTextureMode.Stretch;
        renderer.numCapVertices = 6;
        renderer.numCornerVertices = 6;
        renderer.positionCount = 0;
        return renderer;
    }

    private static void ConfigureTrailRenderer(LineRenderer renderer, float width, Gradient gradient)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.widthMultiplier = width;
        renderer.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.08f),
            new Keyframe(0.18f, 0.9f),
            new Keyframe(0.58f, 1f),
            new Keyframe(0.88f, 0.72f),
            new Keyframe(1f, 0.18f));
        renderer.colorGradient = gradient;
    }

    private static Gradient CreateCoreTrailGradient(float alphaScale)
    {
        alphaScale = Mathf.Clamp01(alphaScale);
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color32(255, 122, 200, 255), 0f),
                new GradientColorKey(new Color32(255, 154, 46, 255), 0.28f),
                new GradientColorKey(new Color32(255, 225, 90, 255), 0.62f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f * alphaScale, 0.18f),
                new GradientAlphaKey(0.95f * alphaScale, 0.66f),
                new GradientAlphaKey(0.42f * alphaScale, 1f)
            });
        return gradient;
    }

    private static Gradient CreateAccentTrailGradient(float alphaScale)
    {
        alphaScale = Mathf.Clamp01(alphaScale);
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color32(184, 108, 255, 255), 0f),
                new GradientColorKey(new Color32(255, 122, 200, 255), 0.42f),
                new GradientColorKey(new Color32(255, 225, 90, 255), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.55f * alphaScale, 0.2f),
                new GradientAlphaKey(0.42f * alphaScale, 0.7f),
                new GradientAlphaKey(0.08f * alphaScale, 1f)
            });
        return gradient;
    }

    private static Gradient CreateGlowTrailGradient(float alphaScale)
    {
        alphaScale = Mathf.Clamp01(alphaScale);
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color32(184, 108, 255, 255), 0f),
                new GradientColorKey(new Color32(255, 122, 200, 255), 0.25f),
                new GradientColorKey(new Color32(255, 177, 59, 255), 0.64f),
                new GradientColorKey(new Color32(255, 243, 160, 255), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.5f * alphaScale, 0.22f),
                new GradientAlphaKey(0.52f * alphaScale, 0.66f),
                new GradientAlphaKey(0.08f * alphaScale, 1f)
            });
        return gradient;
    }

    private void AddMainTrailPoint(Vector3 worldPosition, bool force)
    {
        int maxPointCount = Mathf.Max(2, maxTrailPointCount);
        float minDistance = Mathf.Max(0.001f, trailPointMinDistance);
        if (!force && mainTrailPoints.Count > 0 && Vector3.Distance(mainTrailPoints[mainTrailPoints.Count - 1].Position, worldPosition) < minDistance)
        {
            return;
        }

        mainTrailPoints.Add(new MainTrailPoint
        {
            Position = worldPosition,
            CreatedTime = Time.time
        });

        while (mainTrailPoints.Count > maxPointCount)
        {
            mainTrailPoints.RemoveAt(0);
        }
    }

    private void UpdateMainTrailRenderers()
    {
        if (!hasActiveMainPath || activeMainProgress <= 0.001f)
        {
            ApplyTrailPositions(0);
            return;
        }

        float lengthScale = GetTrailLengthScale();
        if (lengthScale <= 0.001f)
        {
            ApplyTrailPositions(0);
            return;
        }

        ApplyTrailFade(1f);
        int pointCount = Mathf.Max(2, trailCurveSampleCount);
        EnsureTrailPositionBuffer(pointCount);
        float endProgress = activeMainProgress;
        float visibleLength = Mathf.Clamp01(trailProgressLength) * lengthScale;
        float startProgress = Mathf.Max(0f, endProgress - visibleLength);
        for (int i = 0; i < pointCount; i++)
        {
            float lerp = pointCount <= 1 ? 1f : (float)i / (pointCount - 1);
            float progress = Mathf.Lerp(startProgress, endProgress, lerp);
            trailPositionBuffer[i] = EvaluateCubicBezier(
                activeMainPath.StartPosition,
                activeMainPath.FirstControlPosition,
                activeMainPath.SecondControlPosition,
                activeMainPath.EndPosition,
                progress);
        }

        ApplyTrailPositions(pointCount);
    }

    private float GetTrailLengthScale()
    {
        float fadeDuration = Mathf.Max(0.001f, trailFadeOutDuration);
        float fadeElapsed = Mathf.Max(0f, Time.time - lastMainParticleSeenTime);
        return fadeElapsed <= Mathf.Max(0f, minTrailRenderAge)
            ? 1f
            : Mathf.Clamp01(1f - (fadeElapsed - Mathf.Max(0f, minTrailRenderAge)) / fadeDuration);
    }

    private void ApplyTrailFade(float fadeScale)
    {
        ConfigureTrailRenderer(coreTrail, Mathf.Max(0.001f, coreTrailWidth), CreateCoreTrailGradient(fadeScale));
        ConfigureTrailRenderer(accentTrail, Mathf.Max(0.001f, accentTrailWidth), CreateAccentTrailGradient(accentTrailAlphaScale * fadeScale));
        ConfigureTrailRenderer(glowTrail, Mathf.Max(0.001f, glowTrailWidth), CreateGlowTrailGradient(glowTrailAlphaScale * fadeScale));
    }

    private int CountRenderableTrailPoints()
    {
        int count = 0;
        float minCreatedTime = Time.time - Mathf.Max(0f, minTrailRenderAge);
        for (int i = 0; i < mainTrailPoints.Count; i++)
        {
            if (mainTrailPoints[i].CreatedTime <= minCreatedTime)
            {
                count++;
            }
        }

        return count;
    }

    private void CullExpiredMainTrailPoints()
    {
        float lifetime = Mathf.Max(0.01f, trailVisibleLifetime);
        float minCreatedTime = Time.time - lifetime;
        while (mainTrailPoints.Count > 0 && mainTrailPoints[0].CreatedTime < minCreatedTime)
        {
            mainTrailPoints.RemoveAt(0);
        }
    }

    private void EnsureTrailPositionBuffer(int pointCount)
    {
        if (trailPositionBuffer == null || trailPositionBuffer.Length < pointCount)
        {
            trailPositionBuffer = new Vector3[Mathf.NextPowerOfTwo(pointCount)];
        }
    }

    private void ApplyTrailPositions(int pointCount)
    {
        ApplyTrailPositions(coreTrail, pointCount);
        ApplyTrailPositions(accentTrail, pointCount);
        ApplyTrailPositions(glowTrail, pointCount);
    }

    private void ApplyTrailPositions(LineRenderer renderer, int pointCount)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.positionCount = pointCount;
        for (int i = 0; i < pointCount; i++)
        {
            renderer.SetPosition(i, trailPositionBuffer[i]);
        }
    }

    private void ClearMainTrail()
    {
        mainTrailPoints.Clear();
        ApplyTrailPositions(0);
    }

    private void AlignToDestination(Transform targetPoint)
    {
        if (!rotateToDestination)
        {
            return;
        }

        if (targetPoint == null)
        {
            return;
        }

        Vector3 direction = targetPoint.position - transform.position;
        direction.z = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void ApplyDestinationTravel(Transform targetPoint)
    {
        if (mainStreak == null)
        {
            return;
        }

        ParticleSystem.MainModule mainModule = mainStreak.main;
        ParticleSystem.VelocityOverLifetimeModule velocityModule = mainStreak.velocityOverLifetime;
        if (!velocityModule.enabled)
        {
            return;
        }

        if (targetPoint == null)
        {
            velocityModule.y = baseMainVelocityY;
            mainModule.startLifetime = baseMainStartLifetime;
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPoint.position);
        float safeBaseDistance = Mathf.Max(0.01f, baseMainTravelDistance);
        float minMultiplier = Mathf.Max(0.01f, minMainSpeedMultiplier);
        float maxMultiplier = Mathf.Max(minMultiplier, maxMainSpeedMultiplier);
        float speedMultiplier = Mathf.Clamp(distance / safeBaseDistance, minMultiplier, maxMultiplier);
        ParticleSystem.MinMaxCurve velocityY = baseMainVelocityY;
        velocityY.curveMultiplier = Mathf.Max(0.01f, baseMainVelocityY.curveMultiplier) * speedMultiplier;
        velocityModule.y = velocityY;
        mainModule.startLifetime = Mathf.Max(0.05f, mainArrivalTime);
    }

    private Transform ResolveDestinationPoint()
    {
        if (destinationPoint != null)
        {
            return destinationPoint;
        }

        if (string.IsNullOrWhiteSpace(destinationPointName))
        {
            return null;
        }

        GameObject pointObject = GameObject.Find(destinationPointName);
        if (pointObject == null)
        {
            return null;
        }

        destinationPoint = pointObject.transform;
        return destinationPoint;
    }

    private static float RandomRange(Vector2 range)
    {
        return RandomRange(range.x, range.y);
    }

    private static float RandomRange(float first, float second)
    {
        float min = Mathf.Min(first, second);
        float max = Mathf.Max(first, second);
        return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
    }

    private void RemoveDeadParticleSeeds()
    {
        if (previousMainPositions.Count == aliveParticleSeeds.Count)
        {
            return;
        }

        List<uint> removeTargets = null;
        foreach (uint seed in previousMainPositions.Keys)
        {
            if (!aliveParticleSeeds.Contains(seed))
            {
                if (removeTargets == null)
                {
                    removeTargets = new List<uint>();
                }

                removeTargets.Add(seed);
            }
        }

        if (removeTargets == null)
        {
            return;
        }

        for (int i = 0; i < removeTargets.Count; i++)
        {
            previousMainPositions.Remove(removeTargets[i]);
            nextSampleTimes.Remove(removeTargets[i]);
            mainParticlePaths.Remove(removeTargets[i]);
        }
    }

    private void EnsureMainParticleBuffer()
    {
        if (mainStreak == null)
        {
            return;
        }

        int maxParticles = Mathf.Max(1, mainStreak.main.maxParticles);
        if (mainParticles == null || mainParticles.Length < maxParticles)
        {
            mainParticles = new ParticleSystem.Particle[maxParticles];
        }
    }

    private void CacheMainStreakDefaults()
    {
        if (mainStreak == null)
        {
            return;
        }

        ParticleSystem.MainModule mainModule = mainStreak.main;
        ParticleSystem.VelocityOverLifetimeModule velocityModule = mainStreak.velocityOverLifetime;
        baseMainStartLifetime = mainModule.startLifetime;
        baseMainVelocityY = velocityModule.y;
    }

    private static void PlayParticleSystem(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
        {
            return;
        }

        particleSystem.Play(true);
    }

    private static void ClearParticleSystem(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
        {
            return;
        }

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static Vector3 ParticlePositionToWorld(ParticleSystem particleSystem, Vector3 particlePosition)
    {
        ParticleSystem.MainModule mainModule = particleSystem.main;
        if (mainModule.simulationSpace == ParticleSystemSimulationSpace.World)
        {
            return particlePosition;
        }

        Transform simulationTransform = ResolveSimulationTransform(particleSystem);
        return simulationTransform != null ? simulationTransform.TransformPoint(particlePosition) : particlePosition;
    }

    private static Vector3 WorldToParticlePosition(ParticleSystem particleSystem, Vector3 worldPosition)
    {
        ParticleSystem.MainModule mainModule = particleSystem.main;
        if (mainModule.simulationSpace == ParticleSystemSimulationSpace.World)
        {
            return worldPosition;
        }

        Transform simulationTransform = ResolveSimulationTransform(particleSystem);
        return simulationTransform != null ? simulationTransform.InverseTransformPoint(worldPosition) : worldPosition;
    }

    private static Vector3 WorldToParticleVector(ParticleSystem particleSystem, Vector3 worldVector)
    {
        ParticleSystem.MainModule mainModule = particleSystem.main;
        if (mainModule.simulationSpace == ParticleSystemSimulationSpace.World)
        {
            return worldVector;
        }

        Transform simulationTransform = ResolveSimulationTransform(particleSystem);
        return simulationTransform != null ? simulationTransform.InverseTransformVector(worldVector) : worldVector;
    }

    private static Transform ResolveSimulationTransform(ParticleSystem particleSystem)
    {
        ParticleSystem.MainModule mainModule = particleSystem.main;
        if (mainModule.simulationSpace == ParticleSystemSimulationSpace.Custom && mainModule.customSimulationSpace != null)
        {
            return mainModule.customSimulationSpace;
        }

        return particleSystem.transform;
    }
}
