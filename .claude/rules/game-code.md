---
paths:
  - "Assets/Scripts/Game/**"
  - "Assets/Scripts/LevelEditor/**"
  - "Assets/Data/**"
---

# 게임 코드 · 데이터 규칙 (표현 계층 · 레벨 에디터 · 데이터)

> 이 파일들을 다룰 때만 읽힌다. 순수 로직(Core) · 게임 규칙은 `CLAUDE.md` §5, 구현 지도는 `TDD.md`.

## 유니티 코딩

* **No GC / Allocations**: `Update` / `LateUpdate` 등 매 프레임 코드에서 `new` · LINQ · 문자열 조합 금지. `WaitForSeconds` 등은 루프 밖에서 캐싱.
* **메모리 누수 가드**: `event` / `Action` 구독(`+=`)은 반드시 `OnDisable()` 또는 `OnDestroy()`에서 해제(`-=`).
* **static 상태**: 이 프로젝트는 플레이 모드 진입 때 도메인 · 씬 리로드를 끈다(Enter Play Mode Options, `ProjectSettings/EditorSettings.asset`). 값이 바뀌는 static 필드 · static 이벤트 · 싱글턴은 에디터에서 플레이를 다시 시작해도 값이 남으므로 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`에서 초기화한다(빌드에는 영향 없음).
* **컴포넌트 캐싱**: `GetComponent<T>()` · `Find` 계열은 `Awake()` / `Start()`에서만. 태그 비교는 `CompareTag("Tag")`.
* **캡슐화**: 인스펙터 노출 필드는 `[SerializeField] private`. 매직 넘버/스트링 하드코딩 금지(`const` 또는 필드).
* **튜닝 값**: 애니메이션 속도 · 색 팔레트처럼 자주 바꾸는 값은 `ScriptableObject`로 분리한다(`UiTheme` · `MotionSettings` · 팔레트). 스테이지 데이터는 JSON TextAsset + `StageCatalog`(`TDD.md` §4).
* **로깅**: `Debug.LogWarning("Msg", this)`처럼 컨텍스트를 포함한다.
* **Unity 6 API**: Obsolete API(`FindObjectOfType` 등)는 쓰지 않는다 → `FindFirstObjectByType` / `FindAnyObjectByType`.
* **스타일**: 식별자는 영어, 주석은 한국어, private 필드는 `_camelCase`. 기존 코드 스타일을 따른다.

## UI · 입력 (필수 UI 목록은 GDD §6)

* **화면**: 세로 기준 `1080 × 1920`, `Canvas Scaler` = `Scale With Screen Size`. 보드는 화면 가운데, 조작 버튼은 한 손이 닿는 아래쪽. 스테이지는 최대 40칸 정도 — 손가락으로 칸을 구분할 수 있는 크기를 유지한다.
* **안전영역**: 노치 · Dynamic Island를 침범하지 않는다(최상단 UI 패널에 Safe Area 대응). WebGL에서 `Screen.safeArea`가 실제 노치 값을 주는지는 실기로 확인한다(웹 표준은 CSS `env(safe-area-inset-*)`).
* **입력**: 칸을 누른 채 끌기(터치 · 마우스 공통)가 기본. 보조 입력은 모바일 탭 → 여섯 방향 버튼, PC 방향키(칸 선택) + 숫자키 `1·3·5·7·9·0`(방향, `0` = 11시 — 프로토타입 기준). 되돌리기는 버튼 + Ctrl+Z. 멀티터치 시 **처음 닿은 손가락만** 추적하고, UI 위의 터치는 보드 입력으로 처리하지 않는다.
* **사운드**: 켜고 끄기 + 백그라운드 전환 시 정지. 브라우저는 첫 사용자 입력 전에는 소리를 막는다(자동재생 정책) — 버그가 아니다.

## 화면 문구 · 폰트

* 화면 문구나 스테이지 이름을 새로 쓰면 `AgentScripts/Build/FontBuilder.cs`(`FontBuilder.Build`)를 다시 실행하고 `BoardSceneBuilder`의 `BuildPrefabs` → `BuildScene`도 다시 돌린다. 안 하면 게임에 □로 나온다. 다시 만들지 않고 모을 글자만 보려면 `FontBuilder.Preview`.
* 문구는 문자열 리터럴로 쓴다(빌더가 리터럴을 모은다 — 로그 · 예외 메시지 · `[Tooltip]` · `[Header]` 줄은 모으지 않는다).
* 아틀라스 장 수가 늘면 빌드 용량과 함께 보고한다. 폰트 방식 · 수집 범위 · 지금 장 수는 `TDD.md` §7.
