# 배틀 모드 구조 분석 문서

작성 기준: Unity 6.4 (6000.4.4f1), 모바일 세로형 900x1960 기준  
분석 대상: `SCN_Battle`, `SCN_Battle_Drop`, 전투 관련 스크립트/프리팹/ScriptableObject  
작성 목적: 기존 아래에서 위로 쏘는 전투와 새로 만든 위에서 아래로 떨어뜨리는 전투를 앞으로 안전하게 확장하기 위한 구조 판단

## 결론

가장 좋은 방향은 **전투 코어는 통합하고, 조작/물리/필드 규칙은 모드별로 분리하는 하이브리드 구조**입니다.

`SC_BattleManager`, 데미지 계산, 보스 체력, 카드 선택, 보상, 스테이지 저장, 상단 공격 캐릭터 연출은 두 모드가 공유해야 합니다. 반대로 캐릭터 생성 위치, 대기 상태, 발사/드롭 입력, 충돌 후 속도, 머지 후 물리 처리, 필드 정리 기준은 모드별 차이가 커서 무리하게 한 코드로 합치면 계속 예외가 늘어납니다.

현재 상태는 이미 하이브리드로 가는 중간 형태이지만, 경계가 명확하지 않아 일부 카드/팝업/프리팹에서 기능 누락 위험이 있습니다.

## 현재 구조 요약

### 공통으로 쓰는 전투 코어

| 기능 | 주요 파일 | 현재 상태 |
| --- | --- | --- |
| 스테이지 시작/종료, 공격 큐, 보상 | `Assets/_Scripts/SC_BattleManager.cs` | 두 씬이 공유 |
| 보스 체력/사망/샌드백 | `SC_BossSpawner.cs`, `SC_MonsterHealth.cs` | 두 씬이 공유 |
| 데미지 공식/약점/크리티컬 | `SC_DamageCalculator.cs` | 두 씬이 공유 |
| 카드 선택/카드 효과 누적 | `SC_BattleCardPopup.cs`, `SC_CardManager.cs` | 두 씬이 공유하지만 일부 효과는 모드별 누락 |
| 콤보/최종 머지 팝업/클리어/패배 | `SC_ComboManager.cs`, `SC_FinalMergePopup.cs`, `SC_ClearPopup.cs`, `SC_DefeatPopup.cs` | 대부분 공유 |
| 스테이지별 씬 선택 | `SO_MonsterData.cs`, `SC_LoadSceneButton.cs` | `StageBattleDirection`으로 `SCN_Battle`/`SCN_Battle_Drop` 선택 |

### 모드별로 갈라진 부분

| 기능 | 발사 모드 | 드롭 모드 |
| --- | --- | --- |
| 생성 | `SC_BattleCharacterSpawner` | `SC_DropCharacterSpawner` |
| 입력/이동 | `SC_PlayerDragAndShoot` | `SC_DropCharacterController` |
| 머지 | `SC_CharacterMergeController` | `SC_DropCharacterMergeController` |
| 프리팹 | `PFB_Character_1_1` | `PFB_DropCharacter` |
| 씬 | `SCN_Battle` | `SCN_Battle_Drop` |

## 씬/프리팹에서 확인한 사실

### `SCN_Battle`

- `BattleRoot`에 `SC_BattleCharacterSpawner`가 활성화되어 있습니다.
- `SC_BattleCharacterSpawner`는 `PFB_Character_1_1`을 생성합니다.
- `SC_BattleManager`, `SC_BossSpawner`, `SC_CardManager`, `SC_DamageCalculator`, UI/팝업/맵 관련 컴포넌트는 공통 구조입니다.
- `SC_FieldDetectTrigger`가 2개 있고, 하나는 실패 판정용, 하나는 대시 라인 표시용으로 보입니다.

### `SCN_Battle_Drop`

- `BattleRoot`에 `SC_BattleCharacterSpawner`가 남아 있지만 `m_Enabled: 0`입니다.
- 같은 `BattleRoot`에 `SC_DropCharacterSpawner`가 활성화되어 있습니다.
- `SC_DropCharacterSpawner`는 `PFB_DropCharacter`를 생성합니다.
- 공통 UI/보스/카드/데미지 구조는 `SCN_Battle`과 거의 같습니다.

### `PFB_Character_1_1`

- `SC_PlayerDragAndShoot`
- `SC_CharacterPresenter`
- `SC_CharacterMergeController`
- `SC_CharacterYSort`
- Rigidbody2D gravityScale 0

발사 모드 전용 프리팹으로 볼 수 있습니다.

### `PFB_DropCharacter`

- `SC_DropCharacterController`
- `SC_DropCharacterMergeController`
- `SC_CharacterPresenter`
- `SC_CharacterYSort`
- Rigidbody2D gravityScale 5
- 그런데 `SC_PlayerDragAndShoot`도 붙어 있고 비활성화되어 있습니다.
- `SC_CharacterMergeController`도 활성화된 상태로 붙어 있습니다.

이 프리팹은 드롭 전용으로 보이지만, 발사 모드 컴포넌트가 일부 남아 있어 카드/필드 정리/충돌 판정에서 애매한 상태를 만듭니다.

## 기능별 영향 분석

### 1. 기본 전투 루프

두 모드 모두 머지가 발생하면 `SC_BattleManager.NotifyMergeAttack()`으로 공격 요청이 들어갑니다. 이후 공격 큐, 상단 공격 캐릭터 변경, 데미지 계산, 보스 체력 감소, 클리어 팝업은 공통 흐름입니다.

이 부분은 통합 유지가 맞습니다. 모드별로 복제하면 보상, 카드, 저장, 공격 큐 버그를 두 번 고쳐야 합니다.

### 2. 입력/발사/드롭

`SC_PlayerDragAndShoot`와 `SC_DropCharacterController`는 구조가 비슷합니다.

- 터치/마우스 입력
- 넓은 입력 존
- 드래그 중 가이드
- 첫 드래그 화살표
- 팝업 중 입력 차단
- 카드 축소 효과

하지만 물리 결과는 다릅니다.

- 발사: `Vector2.up * shootSpeed`, 감속, 충돌 후 속도 감쇠, 아래 방향 브레이크
- 드롭: `Vector2.down * dropSpeed`, 중력, 대기 중 gravity 0, 드롭 후 gravity 복구

입력 공통부는 묶을 수 있지만, 물리 실행부는 모드별 전략으로 분리하는 것이 안전합니다.

### 3. 머지

`SC_CharacterMergeController`와 `SC_DropCharacterMergeController`는 대부분 같은 책임을 가집니다.

- 같은 등급끼리 충돌했는지 확인
- 다음 등급 생성
- `SC_CharacterPresenter.Configure()` 호출
- 콤보 증가
- 10단계 최종 머지 팝업
- `SC_BattleManager.NotifyMergeAttack()` 호출

차이는 머지 조건과 머지 후 물리입니다.

- 발사 모드: 양쪽 모두 `SC_PlayerDragAndShoot.IsShot`이어야 함
- 드롭 모드: 양쪽 모두 `SC_DropCharacterController.IsActiveDrop`이어야 함
- 발사 모드: 상속 속도/밀치기 효과/발사 상태 유지
- 드롭 모드: 머지 후 속도 0, 드롭 활성 유지

최종적으로는 하나의 공통 머지 코어와 모드별 런타임 어댑터로 정리하는 것이 좋습니다. 단, 바로 하나의 파일로 합치기보다는 중간 인터페이스를 먼저 두는 편이 안전합니다.

### 4. 카드 효과

현재 카드 풀은 두 모드에서 같은 방식으로 제공됩니다. 문제는 카드 효과 중 일부가 발사 모드 구현에 직접 묶여 있다는 점입니다.

| 카드 효과 | 현재 발사 모드 | 현재 드롭 모드 | 위험 |
| --- | --- | --- | --- |
| 데미지 증가류 | 정상 | 정상 | 낮음 |
| 10단계 데미지 | 정상 | 정상 | 낮음 |
| 기습/다음 공격 배수 | 정상 | 정상 | 낮음 |
| 필드 클리어 | 대체로 정상 | 동작 가능하나 발사 컴포넌트 잔재에 의존 | 중간 |
| 낮은 등급 제외 | 현재 대기 캐릭터까지 교체 | 다음 생성부터 적용, 현재 대기 1단계는 유지될 수 있음 | 중간 |
| 파워샷/공격 속도 보너스 | `SC_PlayerDragAndShoot`의 속도 증가/소모로 동작 | `SC_DropCharacterController`에서 소모/적용 없음 | 높음 |
| 낮은 등급 추가 공격 | 공통 BattleManager에서 동작 | 공통 BattleManager에서 동작 | 낮음 |
| 지우개/충돌 삭제 | 발사 충돌에서 동작 | 드롭 컨트롤러에 구현 없음 | 높음 |
| 예지몽/다음 캐릭터 미리보기 | 두 스포너를 찾아 동작 | 동작 가능 | 낮음 |
| 축소 | 양쪽 대기 캐릭터에 적용 | 양쪽 대기 캐릭터에 적용 | 낮음 |
| 위기 탈출/하단 제거 | 발사 대기 캐릭터는 제외 | 드롭 프리팹에 비활성 `SC_PlayerDragAndShoot`가 있어 드롭 캐릭터가 제거 후보에서 빠질 수 있음 | 높음 |
| 보상 증가 | 공통 보상 계산 | 공통 보상 계산 | 낮음 |

카드는 앞으로 늘어날 가능성이 높기 때문에, 카드 효과가 특정 컴포넌트를 직접 찾는 방식은 위험합니다. 카드 효과마다 “어떤 모드에서 지원되는가”, “필드 캐릭터를 어떤 기준으로 찾고 제거하는가”를 공통 API로 빼야 합니다.

### 5. 필드 감지/패배

`SC_FieldDetectTrigger`는 발사 캐릭터와 드롭 캐릭터를 둘 다 감지하도록 확장되어 있습니다.

- 발사: `SC_PlayerDragAndShoot.IsStoppedAfterShot`
- 드롭: `SC_DropCharacterController.IsActiveDrop` + 속도 기준

이 방향은 맞습니다. 다만 이름과 필드명이 아직 `Shot` 중심이라, 장기적으로는 `LaunchedCharacter` 또는 `FieldCharacterRuntime` 같은 중립 명칭이 좋습니다.

### 6. 팝업/입력 차단

각 컨트롤러는 카드 팝업, 최종 머지 팝업, 클리어 팝업이 열려 있으면 입력을 막습니다. 이 부분은 양쪽 모두 구현되어 있습니다.

다만 `SC_BattleManager.CancelAllPendingCharacterDrags()`, `SC_ClearPopup.CancelAllPendingCharacterDrags()`, `SC_FinalMergePopup.CancelAllPendingCharacterDrags()`는 `SC_PlayerDragAndShoot`만 대상으로 합니다. 드롭 컨트롤러는 자기 `Update()`에서 팝업을 감지해 취소할 수 있지만, 공통 팝업 코드가 발사 캐릭터만 알고 있는 것은 구조상 위험합니다.

## 주요 문제점

### P0: 드롭 프리팹에 발사 모드 컴포넌트가 남아 있음

`PFB_DropCharacter`에 `SC_PlayerDragAndShoot`와 `SC_CharacterMergeController`가 함께 있습니다.

`SC_PlayerDragAndShoot`는 비활성화되어 있어 직접 입력은 하지 않지만, `GetComponent<SC_PlayerDragAndShoot>()`에는 잡힙니다. 이 때문에 카드 효과나 필드 정리 로직이 “이 오브젝트는 발사 캐릭터인가?”라고 오해할 수 있습니다.

특히 `RemoveBottomFieldCharacters()`는 `SC_PlayerDragAndShoot`가 있고 `IsShot == false`이면 제거 후보에서 제외합니다. 드롭 프리팹의 비활성 발사 컴포넌트는 항상 `IsShot == false`라서 드롭 캐릭터 제거가 막힐 수 있습니다.

### P0: 일부 카드 효과가 드롭 모드에서 빠져 있음

`CollisionErase`, `AttackQueueSpeedBonus`, `RemoveBottomCharacters`는 발사 모드 컴포넌트 기준으로 구현되어 있습니다. 드롭 모드에서는 적용되지 않거나 의도와 다르게 적용될 가능성이 높습니다.

카드는 전투의 핵심 기능이므로, 두 모드에서 같은 카드가 등장한다면 같은 수준의 효과가 보장되어야 합니다. 만약 특정 카드를 드롭 모드에서 다르게 쓰려면 카드 데이터에 모드 지원/대체 효과를 명시해야 합니다.

### P1: BattleManager가 두 스포너를 직접 알고 있음

`SC_BattleManager`가 `SC_BattleCharacterSpawner`와 `SC_DropCharacterSpawner`를 모두 직접 참조합니다. 지금은 미리보기 정도라 괜찮지만, 앞으로 새 모드가 하나 더 늘면 BattleManager가 계속 커집니다.

`IBattleCharacterSpawner` 같은 공통 인터페이스가 필요합니다.

### P1: 머지 로직 중복

두 머지 컨트롤러는 최종 머지 팝업, 콤보, 공격 통지, 등급 보고, 프리팹 생성 흐름이 중복되어 있습니다. 한쪽에 버그 수정을 하면 다른 쪽을 놓칠 가능성이 큽니다.

다만 물리 차이가 있으므로 완전 통합보다 “공통 머지 서비스 + 모드별 조건/후처리”가 좋습니다.

### P1: 스포너 로직 중복

두 스포너는 가중치, 예지몽 미리보기, 낮은 등급 제외, 리스폰 타이머가 거의 같습니다. 현재 발사 스포너에는 낮은 등급 제외 카드가 켜졌을 때 현재 대기 1단계 캐릭터를 즉시 교체하는 로직이 있지만, 드롭 스포너에는 없습니다.

### P2: 명칭이 기존 발사 방향에 묶여 있음

`Shot`, `Shoot`, `Bottom`, `Upward`, `failOnShotEnter` 같은 이름이 많습니다. 드롭 모드가 들어온 뒤에도 동작은 확장되었지만, 이름이 코드 의도와 어긋나기 시작했습니다.

당장 전부 바꿀 필요는 없지만, 새 공통 API를 만들 때는 중립 명칭을 써야 합니다.

### P2: 씬 복제 유지보수 비용

두 씬은 공통 UI/보스/카드/팝업 구조가 매우 비슷합니다. 지금은 씬이 2개라 관리 가능하지만, UI 하나를 바꾸면 양쪽 씬을 모두 확인해야 합니다.

씬은 유지하되, 공통 Canvas/Popup/BattleCore 프리팹화를 검토하는 것이 좋습니다.

## 선택 가능한 구조 대안

### 대안 A: 완전 통합

하나의 `SC_BattleScene` 또는 하나의 씬에서 `BattleMode` enum으로 발사/드롭을 전부 처리합니다.

장점:
- 씬/코드 중복이 가장 적음
- 스테이지별 모드 전환이 쉽다

단점:
- 입력/물리/머지/필드 규칙 분기가 한 파일에 몰릴 위험이 큼
- 지금 상태에서 바로 통합하면 기능 누락 가능성이 높음

판단: 지금 바로 선택하기에는 위험합니다.

### 대안 B: 완전 분리

발사 전투와 드롭 전투를 별도 BattleManager, 별도 카드, 별도 UI 흐름으로 나눕니다.

장점:
- 각 모드 구현이 단순해 보임
- 물리 튜닝이 독립적

단점:
- 보상, 카드, 데미지, 저장, UI 버그를 두 번 고쳐야 함
- 같은 카드가 모드마다 다르게 동작할 가능성이 커짐
- 장기적으로 개발 속도가 떨어짐

판단: 현재 게임의 공통 기능이 많아서 비추천입니다.

### 대안 C: 전투 코어 통합 + 모드별 런타임 분리

공통 전투 흐름은 하나로 유지하고, 필드 캐릭터/스포너/머지/카드 대상 선택만 공통 인터페이스 뒤로 숨깁니다.

장점:
- 기능 누락 위험이 낮음
- 기존 코드와 씬을 크게 부수지 않고 단계적으로 정리 가능
- 새 모드가 생겨도 BattleManager가 덜 커짐
- 카드/보상/공격 큐는 하나의 규칙으로 유지 가능

단점:
- 인터페이스와 어댑터를 설계해야 함
- 초기에 정리할 파일 수가 많음

판단: 추천안입니다.

## 추천 최종 구조

### 1. 공통 전투 코어

유지:
- `SC_BattleManager`
- `SC_DamageCalculator`
- `SC_CardManager`
- `SC_BattleCardPopup`
- `SC_BossSpawner`
- `SC_MonsterHealth`
- `SC_BattleUI`
- `SC_ClearPopup`
- `SC_DefeatPopup`
- `SC_FinalMergePopup`
- `SC_ComboManager`

단, `SC_BattleManager`는 구체 스포너 2개를 직접 참조하지 않고 공통 스포너 인터페이스만 보도록 바꿉니다.

### 2. 모드별 필드 런타임

새 공통 개념:
- `IBattleCharacterSpawner`
- `IFieldCharacterRuntime`
- `IFieldMergeRuntime`
- `BattlePlayMode` 또는 `StageBattleDirection`
- `SC_BattleModeContext`

예상 책임:
- 현재 대기 캐릭터인지
- 이미 발사/드롭되었는지
- 필드에 놓인 캐릭터인지
- 팝업 때문에 입력을 취소할 수 있는지
- 특정 카드 효과를 적용할 수 있는지
- 머지 후 새 오브젝트를 어떤 물리 상태로 둘지

### 3. 카드 효과 처리

`SC_CardManager`가 직접 `SC_PlayerDragAndShoot`나 `SC_DropCharacterController`를 찾기보다, 공통 필드 런타임 목록을 대상으로 처리해야 합니다.

권장:
- 카드 데이터에 `지원 모드` 또는 `모드별 대체 동작`을 추가
- 카드 선택 팝업은 현재 모드에서 지원되지 않는 카드를 제외
- 제거/축소/충돌삭제/파워샷 같은 필드 조작 카드는 `IFieldCharacterRuntime`을 통해 처리

### 4. 프리팹 정리

`PFB_DropCharacter`에서 발사 모드 전용 컴포넌트는 제거하는 것이 맞습니다.

다만 Unity Project 창에서 직접 프리팹 컴포넌트 제거가 필요한 작업이므로, 실제 수정 단계에서는 사용자가 Unity에서 직접 처리하거나 별도 지시 후 진행해야 합니다.

권장 최종 프리팹:
- `PFB_Character_001_Shoot`: 발사 전용 런타임
- `PFB_Character_001_Drop`: 드롭 전용 런타임
- 공통 자식/스프라이트/Presenter 설정은 가능한 한 동일 프리팹 Variant 또는 공통 기준 프리팹에서 관리

## 단계별 이행 계획

### 1단계: 기능 누락 방지용 안정화

목표: 지금 있는 두 모드를 깨지 않으면서 명확한 버그 위험만 막습니다.

- 드롭 모드에서 `RemoveBottomCharacters`, `CollisionErase`, `AttackQueueSpeedBonus`, `ExcludeLowGradeSpawn` 동작 정책 확정
- 카드 선택 시 현재 모드에서 미지원 카드가 나오지 않도록 임시 필터 추가
- `PFB_DropCharacter`의 발사 컴포넌트 잔재가 카드 로직에 영향을 주지 않도록 코드에서 먼저 방어
- 드롭 스포너에도 현재 대기 1단계 교체 로직 추가

### 2단계: 공통 인터페이스 추가

목표: BattleManager/CardManager가 구체 모드 클래스를 직접 모르는 구조로 바꿉니다.

- `IBattleCharacterSpawner` 작성
- `SC_BattleCharacterSpawner`, `SC_DropCharacterSpawner`가 같은 인터페이스 구현
- `SC_BattleManager`의 `battleCharacterSpawner/dropCharacterSpawner` 직접 참조를 공통 참조로 대체
- `IFieldCharacterRuntime` 작성
- 발사/드롭 컨트롤러가 대기/발사됨/입력취소/축소/제거 가능 여부를 공통으로 제공

### 3단계: 카드 효과 공통화

목표: 카드가 모드별 구현 파일을 직접 찾지 않게 만듭니다.

- `SC_CardManager`의 필드 조작 로직을 `IFieldCharacterRuntime` 기반으로 변경
- `CollisionErase`는 발사/드롭 각각에서 “다음 충돌 대상 제거”로 정의할지, 드롭에서는 다른 효과로 바꿀지 결정
- `AttackQueueSpeedBonus`는 이름과 실제 효과를 정리합니다. 현재는 공격 큐 속도라기보다 발사 속도 보너스처럼 동작합니다.
- 카드 데이터에 현재 모드 지원 여부를 넣을지 결정

### 4단계: 머지 공통 코어 추출

목표: 두 머지 컨트롤러의 중복을 줄입니다.

- 등급 검사, 새 오브젝트 생성, 콤보 증가, 최종 머지 팝업, BattleManager 통지를 공통화
- 머지 가능 조건과 머지 후 물리만 모드별로 남김
- `SC_CharacterMergeController`와 `SC_DropCharacterMergeController`를 바로 합치지 말고, 먼저 공통 서비스로 중복을 줄이는 방식 추천

### 5단계: 씬/프리팹 정리

목표: 두 씬을 유지하되 공통 구조를 프리팹화합니다.

- 공통 UI/팝업/보스/카드 루트를 프리팹화
- `SCN_Battle`과 `SCN_Battle_Drop`은 배치/벽/스폰 위치/모드별 루트만 다르게 유지
- `PFB_DropCharacter`에서 발사 전용 컴포넌트 제거
- 빌드 설정에 두 씬이 계속 포함되는지 확인

## 최종 추천 판단

지금 당장 “완전 통합”이나 “완전 분리”로 크게 갈아엎기보다, **공통 전투 코어를 유지한 상태에서 모드별 런타임 경계를 명확히 세우는 것**이 최적입니다.

가장 먼저 해야 할 실전 작업은 카드 효과 안정화입니다. 플레이어 입장에서는 모드 구조보다 “같은 카드가 어떤 씬에서는 안 먹히는 문제”가 더 크게 느껴질 가능성이 높습니다. 그다음 스포너 인터페이스, 필드 캐릭터 인터페이스, 머지 공통 코어 순서로 가면 기능을 잃지 않고 토대를 정리할 수 있습니다.

## 현재 검증 결과

- `dotnet build ghost_defense.slnx` 성공
- 경고 0개, 오류 0개
- `ProjectSettings/EditorBuildSettings.asset`에 `SCN_Lobby`, `SCN_Battle`, `SCN_Battle_Drop` 모두 포함됨

