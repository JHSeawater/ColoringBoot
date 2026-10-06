# 프로젝트 작업 목록 (Task List)

> 기획 근거는 `GDD.md`(무엇/왜), 작업 규칙 · 아키텍처 · 기술 스펙은 `CLAUDE.md`(TDD 없음 — CLAUDE.md §0), 작업 기록은 `DevelopLog.md`.
> **현재 위치: Phase 7 (화면 · 연출 완성도 · 튜토리얼 · 힌트 · 소규모 플레이테스트)** — 주변인 비공식 테스트(2026-10-07: 설명 없이는 규칙을 모름 · 난이도 체감 차이 · 시간 때우기 수요) → 튜토리얼 · 힌트를 7.8에 넣고 그 뒤 소규모 플레이테스트, 무한 모드는 Phase 8(GDD §5 · §6 · §9). Phase 6 완료(2026-10-03: "컬러링붓" · 그림책 풍경 · 굵은 선 플랫 카툰 · 앱인토스 · 한국어 · 챕터 5개 · 스테이지 55개 안팎 · 기믹 3~4개). Phase 5 완료(2026-10-03: 챕터 그림 — 단계별 칠하기 · 완성 연출 · 작은 그림, 그림 규격 `ArtSpec.md` · 임시 도안 · 관찰지, WebGL 8.74 MB 휴대폰 · PC 확인). Phase 4 완료(2026-10-02: 챕터 1 스테이지 11개 · 차례 해금 · 진행 저장 · 선택 화면 · 옵션 · 파스텔 팔레트, WebGL 8.42 MB 휴대폰 · PC 연속 플레이 확인). 챕터 구조 확정(2026-09-30: 소재 단위 색칠 · 차례 해금 · "완벽" 표시, GDD §5). Phase 3 완료(2026-09-29: BFS 솔버 · 순서 민감도 · 생성기가 프로토타입과 일치, 레벨 에디터 · `StageCatalog`, WebGL 9.03 MB). Phase 2 완료(2026-09-29) · Phase 1 완료(2026-09-28).
> **목표 (2026-10-03 사용자 결정 · GDD §13)**: 발표 · 공개 테스트까지 시간이 남아, 그 전에 출시할 수 있는 품질까지 다듬는다 = **Phase 6~9**, 동아리원 플레이테스트 · 발표 = **Phase 10**. 처음 목표(임시 아트로 챕터 1을 끝까지 플레이하는 WebGL 빌드)는 Phase 0~5로 달성.
> 최초 작성: 2026-09-24 (Labyrinth Task.md 체계 이식) · 개정: 2026-09-27 (기획 대비 구성 검토 반영 — DevelopLog) · 2026-09-28 (색 팔레트 · 문서 정합성 점검 — DevelopLog) · 2026-09-29 (Phase 0~3 점검 반영 — DevelopLog) · 2026-09-30 (챕터 구조 확정 · Phase 4 · 5 재구성 — DevelopLog) · 2026-10-02 (플레이테스트를 Phase 5 끝으로 이동 — DevelopLog) · 2026-10-03 (출시 품질 목표 · Phase 6~10 재구성 · 플레이테스트를 Phase 10으로 이동 — DevelopLog)

---

## 📐 문서 사용 규칙 (필독)

### 완료 기준 (Definition of Done)

1. **모든 항목은 태그를 하나 갖는다.** 태그 없는 항목은 추가하지 않는다.
2. **`[x]`는 "MCP 실측 · 테스트 통과 · 플레이로 확인됨"일 때만 찍는다.** 코드만 작성한 상태 · 설정만 바꿔 둔 상태는 `[ ]` 유지. 체크할 때 근거(테스트 이름 · 실측값 · 커밋 · DevelopLog 날짜)를 항목 끝에 짧게 남긴다.
3. **코드 작성과 씬/프리팹 배선은 반드시 별도 항목으로 쪼갠다.** Unity의 가장 흔한 실패는 "코드는 맞는데 인스펙터가 비어 있음"이다. 한 줄에 뭉치면 구조적으로 잡을 수 없다.
4. **`[QA]` 항목이 하나도 없는 Phase는 닫지 않는다.** Phase 종료는 `/phase-close`로 한다.
5. **"정의 · 설정한 것"과 "적용되어 동작하는 것"은 다른 항목이다.** 예: Player Settings 값을 바꾼 것과 그 설정으로 빌드해 확인한 것, 스테이지 데이터를 만든 것과 게임 목록에 등록한 것.
6. **Task가 설계 문서와 어긋나면 설계 문서가 우선**: 기획(무엇/왜) 충돌은 `GDD.md`, 구현 규칙(어떻게) 충돌은 `CLAUDE.md`를 따른다. 발견 즉시 사용자에게 알리고, 정정 사실은 `[Doc]` 항목으로 남긴다. GDD 수정은 승인 후에만(CLAUDE.md §0).
7. **WebGL · 실기에 관한 완료 조건은 WebGL 빌드(브라우저 · 휴대폰)에서 확인한다.** 에디터 확인만으로 닫지 않는다(CLAUDE.md §4).

### 태그

| 태그 | 의미 |
|---|---|
| `[Code]` | 스크립트 작성 · 수정 (테스트 코드 포함) |
| `[Editor]` | 씬 · 프리팹 · ProjectSettings · 패키지 · 개발 환경 설정(Git · `.claude/`) |
| `[Asset]` | 아트 · 오디오 · 폰트 등 외부 리소스 확보 |
| `[QA]` | 실측 · 테스트 · 플레이 · WebGL 빌드 검증 |
| `[Doc]` | 문서 갱신 · 설계 결정 기록 · 스킬 작성 |

태그 뒤의 **(사용자)** 표시 = 사용자가 직접 하는 일(Unity Hub 설치, 휴대폰 실기 확인, 기획 결정 등). Claude는 체크리스트로 안내하고, 결과를 받아 기록한다.

### 트랙 — 코어 vs 출시 전용

* 표시 없는 Phase/항목 = **코어**: 게임 자체(규칙 · 화면 · 콘텐츠 · 연출)에 필요한 것. Phase 0~8, 10.
* **`🚀[출시]`** = 출시 전용(플랫폼 행정 절차 · SDK · 광고 · 스토어 자료 등). GDD §11에 따라 게임이 완성 단계에 가까워진 뒤(Phase 9) 진행하며, 개발과 병행하지 않는다.

### Phase 헤더 규약

각 Phase는 **선행**(들어가기 전 만족해야 할 조건)과 **완료 조건**(관측 가능한 종료 상태)을 헤더에 적는다. 의존성을 나중에 인라인 메모로 덧붙이지 않는다. 완료되면 헤더에 `✅ (YYYY-MM-DD 완료)`를 붙이고 문서 상단 "현재 위치"를 갱신한다.

---

## Phase 0 — 개발 환경 · WebGL 파이프라인 ✅ (2026-09-24 완료)

> **선행**: 없음
> **완료 조건**: ① 순수 로직 · 테스트 asmdef 골격이 컴파일되고 스모크 EditMode 테스트가 `run_tests`로 통과한다. ② 최소 씬의 WebGL 빌드가 PC 브라우저와 휴대폰 브라우저에서 열린다. ③ 빌드 용량 · 첫 로딩 시간 기준선이 DevelopLog에 기록되어 있다.

### 0.1 작업 환경

- [x] [Doc] CLAUDE.md를 이 프로젝트 기준으로 재작성 + 최종 점검(프로토타입 엔진 코드와 규칙 대조) — DevelopLog 2026-09-24
- [x] [Editor] Git 초기화 · `.gitignore` · GitHub 원격(`origin`) 연결 · push — `5aa49dd`, `d67a934`
- [x] [Editor] 씬/프리팹 텍스트 편집 차단 훅(`.claude/settings.json`) — 파이프 테스트 8종 통과 + 실제 세션에서 차단 확인
- [x] [Editor] 프로젝트 스킬 `unity-pipeline`(공식 사본) · `/phase-close` — 세션 스킬 목록에 인식됨
- [x] [QA] (사용자) WebGL Build Support 설치 → `list_build_targets`에서 WebGL `isInstalled: true`

### 0.2 WebGL 전환 · 프로젝트 정리

- [x] [Editor] 활성 빌드 타깃 WebGL — 이미 전환되어 있었음(2026-09-24 19:40경, WebGL 모듈 설치 후 에디터 재시작 직전). `switch_build_target` 응답 "Already on build target 'WebGL'" · `get_build_settings`로 확인
- [x] [Doc] 렌더링 구성 결정 — URP(Universal Renderer, WebGL은 품질 레벨 `Mobile`) 유지, 후처리는 걷어냄(사용자 결정 2026-09-24)
- [x] [Editor] 템플릿 정리 — `Assets/TutorialInfo`(7개) · `Readme.asset` 삭제(참조 0 확인 후) — git 삭제 내역
- [x] [Editor] 메인 씬 준비 — Directional Light · Global Volume 삭제, Main Camera 직교 · 단색 배경(#D9DFDC) · 위치 (0,0,-10) · 카메라 후처리 끔, 씬 저장 — `get_scene_hierarchy` 루트 = Main Camera만
- [x] [Editor] 불필요 패키지 정리 — AI Assistant · Sentis · AI Navigation · Visual Scripting · Timeline · Collab 제거 + 딸린 의존 3개(2d.sprite · mathematics · dt.app-ui) 자동 제거, 남은 스크립팅 심볼 · App UI 설정 참조 · AI Assistant 설정 파일 정리 — lock 파일 · 캐시에서 제거 확인. 제거 후 에디터 재시작 필요했음(DevelopLog)
- [x] [Editor] WebGL Player Settings — 압축 Brotli(기존) + Decompression Fallback · Managed Stripping High · IL2CPP OptimizeSize · 기본 캔버스 540×960 — `ProjectSettings.asset` 저장 확인(빌드 확인은 0.3)
- [x] [Editor] 파이프라인 볼륨 프로필 정리 — `SampleSceneProfile.asset`(Bloom · Vignette · Tonemapping): 사용자 결정(색 정확도 보호)으로 URP 에셋 2개(`Mobile_RPAsset` · `PC_RPAsset`)의 참조를 비운 뒤 삭제 — 참조 0 확인, `AgentScripts/Phase0ClearVolumeProfile.cs`
- [x] [Doc] 폴더 · 네임스페이스 · asmdef 구조 결정 — `Assets/Scripts/Core` · `Assets/Scripts/Game`(Phase 1) · `Assets/Tests/EditMode`(사용자 결정 2026-09-24) → CLAUDE.md §3 반영
- [x] [Code] asmdef 골격 생성 + 스모크 EditMode 테스트 1개 — `ColoringBoot.Core`(`noEngineReferences: true`) · `ColoringBoot.Core.Tests` · `PaintColor` · `PaintColorTests`
- [x] [QA] `recompile` → `run_tests`(mode=editor)로 스모크 테스트 통과 — `PaintColorTests.RedOrYellow_IsOrange` 1/1 Passed(에디터 재시작 후)

### 0.3 첫 WebGL 빌드 · 기준선

- [x] [Doc] 테스트 배포 경로 결정 — GitHub Pages · `gh-pages` 브랜치(빌드 결과물 전용, main 기록과 분리). 저장소 공개 확인(비로그인 HTTP 200) — 사용자 결정 2026-09-24
- [x] [QA] WebGL 빌드 성공(`build` → `build_status`) — 압축 후 빌드 용량 기록 → Succeeded · 에러 0 · 경고 3(무해) · 6.1분 · 압축 후 7.76 MB(wasm 5.66 MB · data 2.27 MB · framework 66 KB · loader 119 KB). gh-pages 배포 `bde5385` → https://jhseawater.github.io/ColoringBoot/ (HTTP 200 확인)
- [x] [QA] (사용자) PC 브라우저에서 실행 확인 — 로컬 서버나 배포 URL로 연다(파일을 직접 열면 동작하지 않음) → 사용자 보고(2026-09-24): 배포 URL에서 표시, 첫 로딩 약 3초
- [x] [QA] (사용자) 휴대폰 브라우저에서 실행 확인 — 세로 화면 · 첫 로딩 시간 측정 → 사용자 보고(2026-09-24): 표시, 첫 로딩 약 3초(이상 보고 없음)
- [x] [Doc] 기준선 기록(빌드 용량 · 첫 로딩 시간 PC/휴대폰) → DevelopLog. 이후 Phase마다 비교한다 → 압축 후 7.76 MB · 첫 로딩 약 3초(PC · 휴대폰) — DevelopLog 2026-09-24 Phase 0 종료 · CLAUDE.md §4

---

## Phase 1 — 코어 로직 + 포도 플레이 (GDD §13 단계 1) ✅ (2026-09-28 완료)

> **선행**: Phase 0 완료(asmdef 골격 · WebGL 빌드 경로)
> **완료 조건**: ① 순수 C# 로직의 EditMode 테스트가 모두 통과한다(포도 회귀 포함). ② 포도 스테이지를 처음부터 클리어까지 플레이할 수 있는 WebGL 빌드가 휴대폰에서 동작한다.

### 1.1 코어 로직 (순수 C# — UnityEngine 비참조)

- [x] [Code] 육각 좌표 · 6방향 · 같은 줄 판정(CLAUDE.md §3 표) — `HexDirection` · `HexCoord`(`LineKey` · `Along`), `HexCoordTests` 12/12
- [x] [Code] 보드 상태 · 색 비트마스크 · 붓질 처리(GDD §2.4 의사코드, 줄 중간 빈자리 건너감) — `Board.Brush`(색 상태는 인자 — 솔버 재사용), `BoardTests` 붓질 12건
- [x] [Code] 막힘 · 성공 판정 — `Board.IsDead` · `IsDeadCell` · `IsSolved`, `BoardTests` 8건
- [x] [Code] 되돌리기(획 단위 스냅샷) · 처음부터 — `PuzzleSession`(색이 안 바뀌는 획은 무시 · 처음부터는 되돌릴 수 없음 — 사용자 결정 2026-09-28), `PuzzleSessionTests` 5/5
- [x] [Code] 스테이지 코드 파싱(프로토타입 JSON 포맷) + 선택 필드 `minMoves`(최소 수, 포도 = 5 — 없으면 미표시. 값 채우기는 Phase 3 솔버) · `palette`(이름만 읽음, 없으면 기본 팔레트 — CLAUDE.md §3) — `Stage.Parse`(Core 전용 파서 `StageParser`, 사용자 결정 2026-09-28), `StageParserTests` 19/19
- [x] [Code] EditMode 테스트 — 방향 · 줄 판정, 붓질 규칙(빈 붓 · 섞임 · 같은 색 · 빈자리), 막힘 · 성공, 되돌리기 — EditMode 전체 60/60 통과(DevelopLog 2026-09-28)
- [x] [QA] 포도 회귀 — GDD §3 풀이 5수로 클리어 + 5수 순서 120가지 중 8가지만 성공(`run_tests`) — `GrapeRegressionTests` 2/2. 프로토타입 엔진(Node)으로 같은 값 확인

### 1.2 최소 표현 계층

- [x] [Editor] `ColoringBoot.Game` asmdef 생성(`ColoringBoot.Core` 참조) — CLAUDE.md §3 — 참조 Core · UnityEngine.UI, 컴파일 에러 0
- [x] [Asset] 임시 스프라이트 4종(육각 · 육각 테두리 · 원 · 화살표) — `AgentScripts` 빌더로 흰색 PNG 생성, 색은 코드가 입힘(보드는 uGUI — 사용자 결정 2026-09-28) — `Phase1Sprites.cs`, 256px · 4×4 슈퍼샘플링, Sprite · 밉맵 없음 · 무압축 실측
- [x] [Editor] TMP Essential Resources 임포트 — Resources 폴더는 빌드에 통째로 들어가므로 용량은 1.2 빌드 QA에서 보고 — `Assets/TextMesh Pro` 4.0 MB(LiberationSans SDF 2.2 MB)
- [x] [Editor] 기본 팔레트 에셋(프로토타입 라이트 테마 7색) · 포도 스테이지 TextAsset(최종 형식은 Phase 3) · 칸 / 방향 버튼 프리팹 — `Assets/Data/Palettes/DefaultPalette.asset` · `Assets/Data/Stages/Grape.json` · `Assets/Prefabs/Cell` · `DirectionButton`, 참조 누락 0(`get_serialized_fields`)
- [x] [Code] 보드 렌더링 — 육각 칸 · 색(기본 팔레트 ScriptableObject에서 읽기, GDD §2.3) · 목표 색 마커(화면 배치 공식, y축 반전 — CLAUDE.md §3) · 보드 크기에 맞춰 화면에 자동 맞춤(스테이지마다 크기가 다름, 최대 40칸 정도 — GDD §6) — `BoardView` · `CellView` · `ColorPalette`, 에디터 플레이 스크린샷이 프로토타입 포도와 같은 모양
- [x] [Code] 최소 입력 — 칸 탭 → 여섯 방향 버튼으로 붓질(드래그는 Phase 2). 1칸짜리 줄 방향은 버튼 숨김(`Board.LineLength`, `BoardTests.LineLength_*`) — 초록 칸 선택 시 1·5·7·11시만 표시 실측
- [x] [Code] 클리어 표시 · 막힘 표시(막힌 칸 강조, 최소) · 처음부터 버튼 — 글자는 영문 · 아이콘(한글 폰트는 Phase 2, CLAUDE.md §8) — `PuzzleController`, 클리어 후 입력 잠금 · 처음부터는 둔 수가 있을 때만
- [x] [Editor] 보드 씬 구성 · 인스펙터 배선(카메라 · 보드 · 입력 · Canvas Scaler 1080×1920) — `SampleScene` → `Board` 이름 변경 — `Phase1Scene.cs`, Canvas Scaler Expand, 빌드 씬 목록 `Board.unity`(GUID 유지), 씬 저장 · 참조 누락 0
- [x] [QA] 에디터 플레이로 포도 클리어 — `editor_play` · 상태 실측 · 스크린샷(source=screen) — `Phase1Qa.cs`로 실제 탭 경로(보드 클릭 → 방향 버튼)를 따라 GDD 5수 클리어 · 클리어 후 입력 잠김 · 처음부터 · 막힘(검정 2칸 링 + 안내) 확인, 콘솔 에러 0(DevelopLog 2026-09-28)
- [x] [QA] WebGL 빌드 · gh-pages 배포 — 용량 · 첫 로딩을 Phase 0 기준선과 비교. 빌드 뒤 `preloadedAssets` 변경은 빌드 도중 값이라 커밋하지 않음(DevelopLog 2026-09-25) — Succeeded · 에러 0 · 4.2분, 압축 후 8.75 MB(8.34 MiB, 기준선 +0.63 MB: data +530 KB · wasm +99 KB), gh-pages `f6236c9` 제공 확인. 첫 로딩 시간은 휴대폰 확인 항목에서 측정
- [x] [QA] (사용자) 휴대폰 WebGL 빌드로 포도 클리어 · 막힘 표시 확인 — 사용자 보고(2026-09-28): 휴대폰 · PC 모두 첫 로딩 약 3초, 세로 화면 · 선택 · 방향 버튼 · 클리어 · 막힘 표시 모두 문제 없음
- [x] [Doc] Phase 1 종료 정합성 점검 — CLAUDE.md §4 현황에 Phase 1 빌드 용량을 더하고 기준선 합계 7.76 MB가 단위가 어긋난 값(실제 8,118,864바이트 = 8.12 MB)임을 밝힘. GDD 변경 필요 없음(DevelopLog 2026-09-28 Phase 1 종료)
- [x] [Doc] `/qa-scene` 스킬 작성 — 보드 씬 셋업 전수 실측(읽기 전용, CLAUDE.md §7 예정 항목) — `.claude/skills/qa-scene`, 빌드 뒤 실행해 26항목 전수 통과

---

## Phase 2 — 조작감 (GDD §13 단계 2) ✅ (2026-09-29 완료)

> **선행**: Phase 1 완료
> **완료 조건**: 휴대폰에서 드래그로 붓질하고, 미리보기로 결과를 예측하고, 되돌리기로 복구할 수 있다. PC에서는 마우스 드래그 · 키보드 · Ctrl+Z로 같은 조작이 된다(모두 WebGL 빌드에서 확인).

### 2.1 조작 — 드래그 · 미리보기 · 되돌리기

- [x] [Code] Core 붓 경로 조회(쓸고 지나가는 칸 순서 · 칸마다 붓 색, 상태 불변) + 테스트 — 미리보기가 규칙을 복제하지 않게 — `Board.Trace` · `PuzzleSession.Trace`, `GrapeRegressionTests.Trace_*`(모든 칸 × 6방향이 `Brush` 결과와 일치), EditMode 63/63
- [x] [Code] 드래그 붓질 — 칸을 누른 채 끌기 → 가장 가까운 방향 판정 · 임계 거리 · 첫 손가락만 추적 · UI 위 터치 무시 — `BoardView`(누름 · 끌기 · 뗌, 반지름 45% 임계), 에디터 플레이에서 드래그 5수로 포도 클리어 · 0.3배 드래그는 탭 처리(`BoardQa.Drag`)
- [x] [Code] 붓질 미리보기 — 경로 · 경로 위 붓 색 변화 · 칠해질 결과를 반투명으로(GDD §6) — 드래그 중 · 방향 버튼 위에서 표시, 막히는 획(보라 → 초록 줄)이 검정으로 미리 보임(스크린샷)
- [x] [Code] 되돌리기 버튼 · Ctrl+Z(처음부터 버튼은 Phase 1) — 버튼은 에디터 확인(클리어 뒤 되돌리면 잠금 해제 · 막힘 복구). Ctrl+Z는 WebGL PC에서 사용자 보고(2026-09-29): 휴대폰 · PC 모두 정상
- [x] [Editor] 드래그 입력 · 미리보기 · 되돌리기 버튼 씬 배선 — `BoardSceneBuilder`(칸 프리팹 Ghost 층 · UndoButton · `_undoButton` 참조), 플레이로 확인

### 2.2 필수 UI (GDD §6)

- [x] [Code] 막힘 표시 다듬기 — Phase 1의 최소 강조에 되돌리기 안내 추가 — 막힘 안내 띠("목표에 없는 색이 섞였어요") 안에 되돌리기 버튼, 눌러 복구 확인
- [x] [Code] 수 카운터(현재 / 최소) — 최소 수는 스테이지 데이터 `minMoves`(Phase 1 파싱, 솔버로 채우기는 Phase 3) — "3 / 5수" 표시 · 되돌리면 줄어듦 확인, `minMoves` 없으면 현재 수만
- [x] [Code] 목표 그림 썸네일 · 색 조합표(스테이지 팔레트) · 접근성 기호(R · Y · B) — 썸네일 = BoardView 목표 모드, `MixTableView`, 칸 기호(기호 버튼으로 전환, 썸네일에도) — 스크린샷 확인
- [x] [Asset] 한글 폰트 선정 · 글자 범위 결정 — 용량 보고(CLAUDE.md §8) — Pretendard(OFL, 라이선스 파일 포함) · 쓰는 글자만 고정 아틀라스(UI 문구 + 스테이지 이름, 빌더가 수집) — 사용자 결정 2026-09-28 — Pretendard 1.3.9 SemiBold TTF + `Pretendard-LICENSE.txt`, 225자(ASCII 95 포함) · 1024² 1장. 빌드 용량은 2.4 QA에서
- [x] [Editor] TMP 한글 폰트 에셋 생성 · UI 적용 — `AgentScripts/Phase2Font.cs`(고정 아틀라스 · 원본 폰트 참조 비움 · TMP 기본 폰트 지정), 모든 문구 한글 표시 스크린샷 확인
- [x] [Editor] 막힘 안내 · 수 카운터 · 썸네일 · 색 조합표 · 기호 옵션 씬 배선 — `BoardSceneBuilder`, 플레이로 확인

### 2.3 플랫폼 — PC 입력 · 안전영역 · 사운드

- [x] [Code] PC 보조 입력 — 방향키로 칸 선택 · 숫자키 1 · 3 · 5 · 7 · 9 · 0(방향) — `PuzzleController.Update` · `BoardView.MoveSelection/BrushSelected`, WebGL PC 사용자 보고(2026-09-29): 휴대폰 · PC 모두 정상
- [x] [Code] 안전영역(Safe Area) 대응 — `SafeAreaFitter`(에디터에선 안전영역 = 전체), 휴대폰 WebGL에서 위쪽 글자 가림 없음 — 사용자 보고(2026-09-29): 휴대폰 · PC 모두 정상. 앱인토스 웹뷰의 노치 값은 🚀 연동 때 다시 확인
- [x] [Code] 사운드 구조 — 켜고 끄기 · 백그라운드 전환 시 정지(소리 에셋 없이 구조만) — `SoundController`, 켜고 끄기는 에디터(볼륨 0/1) · WebGL 확인, 다른 탭 · 앱 전환 뒤 이상 없음 — 사용자 보고(2026-09-29): 휴대폰 · PC 모두 정상. 실제 소리 정지는 소리 에셋이 생기면 다시 확인
- [x] [Editor] 키보드 입력 · Safe Area 패널 · 사운드 켜고 끄기 씬 배선 — `Canvas/SafeArea` 아래로 UI 이동 · 소리 버튼 · `Sound` 오브젝트, 키보드는 컨트롤러가 직접 읽음(배선 없음)

### 2.4 검증

- [x] [Code] 주소 `?stage=<이름>`으로 스테이지 열기(QA용, 없으면 포도) — 사용자 결정 2026-09-28 — `PuzzleController.ChooseStage`, `?stage=hive`로 37칸 보드 열림 — 사용자 보고(2026-09-29): 휴대폰 · PC 모두 정상
- [x] [Editor] 37칸 프로토타입 스테이지 JSON(큰 벌집) 추가 · 컨트롤러에 등록 · 포도 JSON에 `minMoves` 5(GDD §3) — `Hive.json`(프로토타입 원문 37칸 · `minMoves` 6) · `_queryStages`
- [x] [QA] (사용자) 휴대폰 WebGL — 드래그 · 미리보기 · 되돌리기 · 세로 화면 · 안전영역 · 한글 표시 · **칸이 가장 많은 프로토타입 스테이지**에서 손가락으로 칸 구분(GDD §6 최대 40칸 정도) — 사용자 보고(2026-09-29): 모두 정상(37칸 `?stage=hive` 포함)
- [x] [QA] (사용자) PC 브라우저 — 마우스 드래그 · 키보드 · Ctrl+Z · 사운드 켜고 끄기 — 사용자 보고(2026-09-29): 모두 정상
- [x] [QA] 빌드 용량 · 첫 로딩 시간 재측정 — Phase 0 기준선 대비(폰트 추가 영향) — 압축 후 8,992,413바이트(Phase 1 대비 +245 KB, 한글 폰트 아틀라스 등 · Phase 0 대비 +0.87 MB), 첫 로딩 이전과 거의 같음(사용자 보고 2026-09-29), gh-pages `e5eef23`
- [x] [Doc] `/qa-scene` 갱신(SafeArea · 새 UI · 폰트) + 읽기 전용 점검 스크립트 `AgentScripts/QaScene.cs` — 빌드 뒤 53항목 + 계층 · 빌드 씬 목록 전수 통과
- [x] [Doc] Phase 2 종료 정합성 점검 — CLAUDE.md §4 현황(Phase 2 용량) · §8(TMP 고정 아틀라스 폐기 예정 표시) · §3 AgentScripts 목록 갱신, GDD 변경 없음(DevelopLog 2026-09-29)

---

## Phase 3 — 솔버 · 레벨 에디터 (GDD §13 단계 3) ✅ (2026-09-29 완료)

> **선행**: Phase 1 완료(코어 로직). Phase 2와 순서를 바꾸거나 병행할 수 있다.
> **완료 조건**: 에디터 도구로 스테이지를 만들고(시작 칠하기 → 획 기록 → 목표 저장), 솔버가 최소 수와 순서 민감도를 계산하며, 그렇게 만든 스테이지를 게임에서 플레이할 수 있다(동아리 발표 시연 기준).

- [x] [Doc] 스테이지 데이터 형식 확정 — JSON TextAsset vs ScriptableObject, `minMoves` · `palette` 저장 방식 → CLAUDE.md §3 · §5 갱신(에디터 저장 · 스테이지 로딩보다 먼저) — 사용자 결정(2026-09-29): JSON TextAsset 유지 · `minMoves`는 에디터 저장 때 솔버가 채움 · 순서 민감도는 저장 안 함 · 목록은 `StageCatalog` SO. 레벨 에디터는 에디터 전용 씬(플레이 모드, 게임 보드 재사용)
- [x] [Code] BFS 솔버 — 막힘 상태 제외 · 탐색 상한 · 최소 수(순수 C#) — `Solver.Solve`(상한 40만 · 칸당 3비트 상태 키 · 수 순서 = 프로토타입 `Board.Moves`), `SolverTests` 6/6
- [x] [Code] 순서 민감도 — 최적 풀이의 순서 바꾸기(7수 이하) — `Solver.MeasureOrder`, 포도 8/120
- [x] [Editor] 프로토타입 스테이지 9개 이식(포도 + 8개) — `Assets/Data/Stages/*.json`(원문 그대로 + `minMoves`)
- [x] [QA] 솔버 회귀 — 포도 최소 5수 · 120가지 중 8가지, 프로토타입 스테이지 9개의 최소 수가 프로토타입 엔진(`Prototype/Prototype.html`의 스크립트, Node로 실행 가능) 계산값과 일치. 주의: 순서 민감도는 솔버가 고른 풀이에 따라 다르다(포도의 5수 풀이 3가지 = 8 · 6 · 2/120, 기준값 표 — DevelopLog 2026-09-28) — `PrototypeStageRegressionTests` 9/9: 9개 모두 최소 수 · 순서 민감도 · **탐색 상태 수까지** 프로토타입과 일치(수 나열 순서를 프로토타입과 같게 맞춤)
- [x] [Code] 레벨 에디터(에디터 전용) — 팔레트 선택 · 칸 추가 · 삭제 · 시작 색 칠하기 · 획 기록으로 목표 만들기 · 저장 — `ColoringBoot.LevelEditor`(UNITY_EDITOR 전용 어셈블리): `PaintGridView`(반지름 4 격자 칠하기) · `LevelEditorController`(IMGUI 패널, 획 기록은 게임 보드 복제 · 팔레트 이름 입력) + Core `StageWriter`(`StageWriterTests` 10/10). 에디터 플레이로 4칸 스테이지 칠하기 → 2획 기록 → 목표 저장 → 저장까지 확인(`AgentScripts/LevelEditorQa.cs`)
- [x] [Code] 에디터에서 솔버 실행 · 결과(최소 수 · 순서 민감도) 표시 · `minMoves` 기록 — 풀이 검사("최소 2수, 탐색 4개, 2가지 중 1가지") · 저장 시 `minMoves` 기록 · 풀 수 없으면 저장 거부 · 게임 폰트에 없는 글자 경고, 랜덤 생성 패널(모양 · 시작 색 칸 · 목표 수 · 색 · 순서 · 검정)도 동작
- [x] [Editor] 레벨 에디터 씬(`LevelEditor.unity`, 빌드 제외) 구성 · 배선 — `BoardSceneBuilder.BuildEditorScene`, 빌드 씬 목록은 `Board.unity` 하나 유지
- [x] [Code] 랜덤 생성기 — 무작위 시작 색 · 무작위 획 → 최소 수 · 순서 민감도로 거르기(GDD §8) — `Generator` · `SeededRandom`(프로토타입 mulberry32) · `BoardShape` 6종, 프로토타입 generate를 난수 사용 순서까지 옮김
- [x] [QA] 생성기 테스트 — 같은 시드 → 같은 결과, 생성된 스테이지가 조건(최소 수 · 순서 민감도 · 검정 허용)을 만족 — `GeneratorTests` 5/5: 시드 4건(기본 · 불규칙 · 아무 모양 + 섞인 색 + 검정 금지 · 삼각형)이 프로토타입 생성기(Node)와 칸까지 같음
- [x] [Code] 게임이 지정한 스테이지 데이터를 불러오기(스테이지 선택 화면은 Phase 4) — `StageCatalog`(첫 스테이지 · `?stage=`), 에디터 저장 시 자동 등록 — 에디터 플레이에서 첫 스테이지 = 포도 확인, 에디터 저장분이 10번째로 등록됨
- [x] [Editor] 보드 씬에 불러올 스테이지 지정 배선 — `StageCatalog` 에셋(9개 등록) · 컨트롤러 참조 — `BoardSceneBuilder.BuildCatalog`(빠진 것만 채움 — 다시 돌려도 에디터 저장분 유지), `QaScene` 62항목 통과
- [x] [QA] 에디터로 만든 스테이지를 게임에서 플레이 · 솔버 최소 수로 클리어 가능 확인 — 에디터로 만든 `EditorDemo`(4칸 · 최소 2수)를 WebGL `?stage=EditorDemo`로 2수 클리어, 기본 주소 포도 — 사용자 보고(2026-09-29): 문제 없음. 확인 뒤 `EditorDemo`는 목록 · 파일에서 뺌(사용자 결정, `AgentScripts/RemoveStage.cs`)
- [x] [QA] WebGL 빌드 · gh-pages 배포 — 압축 후 9,025,674바이트(Phase 2 대비 +33 KB), 빌드에 레벨 에디터 어셈블리 없음, gh-pages `5a8647b`
- [x] [Doc] Phase 3 종료 정합성 점검 — CLAUDE.md §4 현황(Phase 3 용량) · §8(플레이 멈춘 직후 씬 전환이 되돌려짐) 추가, GDD 변경 없음(DevelopLog 2026-09-29)

---

## Phase 4 — 스테이지 10개 내외 (GDD §13 단계 4) ✅ (2026-10-02 완료)

> **선행**: Phase 2 · Phase 3 완료, 챕터 구조 확정(GDD §5 — 2026-09-30 확정: 스테이지는 그림 모양에 맞추지 않고 소재로만 이어진다 → 스테이지 제작에 그림 제약 없음)
> **완료 조건**: 스테이지 10개 내외를 순서대로 이어서 플레이할 수 있다(차례 해금 · 진행 저장 포함, WebGL 휴대폰 · PC). 동아리원 플레이테스트는 Phase 5 끝으로 옮겼다(2026-10-02 사용자 결정 — 발표 일정 전이고, "요청 없이 다음으로 가는가"는 챕터 그림 보상이 있어야 제대로 관찰된다).

### 4.1 결정 · 점검 정리

- [x] [Doc] (사용자) 챕터 구조 확정 — 챕터 1 = 스테이지 10개 안팎(= 색칠 단계 수) · 소재 단위 색칠(스테이지를 클리어하면 그 소재의 모든 영역이 한꺼번에 칠해짐, 스테이지 모양과 무관) · 최소 수 보상 = "완벽" 표시 · 차례 해금 — 사용자 결정 2026-09-30, GDD §5 · §14 반영(DevelopLog 2026-09-30)
- [x] [Doc] Phase 0~3 전체 점검 · 결과 반영 — 발견 사항 F1~F10 분류(사용자 승인 2026-09-29). 조치할 것은 이 Phase의 "점검 Fn" 항목으로 옮기고, 처리 결과와 하지 않는 이유는 DevelopLog에 기록. CLAUDE.md §0 규칙(검토 보고서는 할 일 목록이 아님) 추가 · §3 · §4 · §5 정정 — DevelopLog 2026-09-29 "Phase 0~3 전체 점검"
- [x] [Doc] 점검 결과 교차 검토 · 반영 — F1~F10 · H1 · H2를 코드 · 설정 · 빌드 리포트 · 프로토타입과 다시 대조(모두 사실과 맞음). F4를 결함으로 확정해 [Code] 항목으로, F5를 저장 거부로 확정, F6에 기준 파일 분리 [Code] 항목 추가(사용자 승인 2026-09-29) — DevelopLog 2026-09-29 "점검 결과 교차 검토"
- [x] [Doc] (사용자) 빌드 용량 선택지 결정 — Unity 스플래시 끄기(로고 텍스처 압축 전 2,796,376바이트) · 안 쓰는 TMP 기본 폰트 빼기(`TextMesh Pro/Resources`의 LiberationSans SDF + 딸린 TTF, 압축 전 1,431,198바이트) 둘 다 — 사용자 결정 2026-09-30 — 점검 F3
- [x] [Editor] 스플래시 끄기 · LiberationSans SDF(+ Fallback · Drop Shadow · Outline 머티리얼) 빼기 — 참조 0 확인 후 — 점검 F3 → `AgentScripts/Phase4BuildSize.cs`(dry run 후 적용): `m_ShowUnitySplashScreen` 0, 에셋 6개(+ TTF · OFL 라이선스)와 빈 폴더 2개 삭제, 참조 0(GUID 검색), `QaScene` 61항목 · 에디터 플레이 한글 표시 정상
- [x] [QA] 빌드 전후 크기 바이트 비교 · 한글 표시 이상 없음 — 점검 F3 → 압축 후 8,506,560바이트(Phase 3 대비 −519,114: data 3,082,788 → 2,557,243 · wasm +6,431 = 끌기 취소 코드), 에러 0 · 경고 7(이전과 같음), 빌드 도중 값(`preloadedAssets`) 되돌림, gh-pages `5d274d4`(HTTP 200). 한글은 에디터 플레이 스크린샷으로 확인 — 휴대폰 · 첫 로딩은 4.6 QA
- [x] [Code] 회귀 테스트 기준 파일 분리 — 이식 9개 JSON을 테스트 쪽으로 옮겨 `PrototypeStageRegressionTests` · `StageWriterTests`가 게임 데이터(`Assets/Data/Stages`)를 읽지 않게. 칸 순서만 바뀌어도 탐색 수 · 순서 민감도 기대값이 달라지고(검토 실측: 포도 순서 8→2/120 · 엇갈림 탐색 250→252 등, 최소 수는 그대로), `RemoveStage`로 이식 스테이지를 빼면 파일이 지워져 테스트가 실패한다. 스테이지 제작 전에 한다 — 사용자 결정 2026-09-29 — 점검 F6 → `Assets/Tests/EditMode/PrototypeStages/`(사본 9개) · `TestStages.ReadPrototype`, EditMode 93/93(DevelopLog 2026-09-30)
- [x] [Code] 레벨 에디터 풀이 검사 상한을 프로토타입 에디터의 풀이 검사와 같게(40만 → 60만), 상한에 걸려 풀이를 확인하지 못하면 **저장 거부**(지금은 `minMoves` 없이 저장됨, `LevelEditorController.Save`) — 사용자 결정 2026-09-29 — 점검 F5 → 에디터 플레이 확인: 상한에 걸리는 37칸 스테이지는 "저장하지 않았어요"(상태 600,001개, 1.5초) · 파일 안 생김, 포도는 최소 5수로 저장(확인 뒤 `RemoveStage`로 치움)
- [x] [Code] 끌기 취소 처리 — 끌기 도중 터치가 취소되면(알림 · 시스템 제스처 · 앱 전환) 획을 버리고 미리보기를 지운다(프로토타입 `Prototype/Prototype.html:745`와 같게). 지금은 그어진다: WebGL이 브라우저 `touchcancel`을 받고, Input System 1.20 UI 모듈은 취소된 터치에도 `OnPointerUp`을 보내며(`InputSystemUIInputModule.cs` 2165행 주석), `BoardView.OnPointerUp`은 취소를 구분하지 않고 긋는다 — 검토에서 코드 경로 확인(2026-09-29) — 점검 F4 → `BoardView`: 터치가 Canceled이거나 포커스 없이 떼어지면 획을 버림 + `OnApplicationFocus(false)`에서 끌기 취소. 에디터 확인: 끌던 중 포커스를 잃으면 수 0 → 0 · 미리보기 지워짐, 정상 드래그 5수로 포도 클리어 · 짧게 끌면 탭. 실제 터치 취소는 4.6 휴대폰 QA

### 4.2 진행 · 저장

- [x] [Code] 진행 로직(Core, 순수 C#) — 스테이지별 클리어 · 최고 기록(최소로 둔 수) · "완벽"(최고 기록 = `minMoves`) · 차례 해금 판정(바로 앞 스테이지 클리어 — 사용자 결정 2026-09-30) · 문자열로 저장 · 복원. 스테이지는 파일 이름으로 구분(표시 이름을 바꿔도 기록 유지) + EditMode 테스트 → `Progress` · `PlayStats`, `ProgressTests` 10 · `PlayStatsTests` 4, EditMode 107/107
- [x] [Code] 저장 인터페이스 + PlayerPrefs 구현 — 앱인토스 저장소로 교체 가능하게(CLAUDE.md §3), WebGL은 쓸 때마다 `PlayerPrefs.Save()` · 광고 인터페이스 자리(구현 없음, GDD §6) → `IKeyValueStore` · `PlayerPrefsStore` · `IAdService` · `NoAdService`(호출은 4.3 "다음 스테이지"), `PuzzleController`가 클리어 때 기록. 에디터 플레이: 포도 5수 클리어 → `progress 1 / Grape 5` 저장, 플레이를 다시 시작해도 클리어 · 최고 5 · 완벽 · 다음 스테이지 열림으로 읽힘. WebGL 새로고침 유지는 4.3 끝 빌드에서
- [x] [Code] 플레이테스트 기록 — 스테이지별 둔 수 · 되돌리기 · 처음부터 횟수 · 걸린 시간을 저장, 주소 `?stats`로 보기(GDD §9 "추론인가 찍기인가") — 사용자 결정 2026-09-30 → `PlayStats`(첫 클리어까지만 셈 · 시간은 앱이 앞에 있을 때만) · `StatsView` · `?reset`(진행 · 기록 지우기) · `UrlQuery`. 에디터 플레이: 붓질 7 · 되돌리기 1 · 처음부터 1 · 클리어 5수로 저장, 패널 열기 · 닫기 스크린샷. 시간 누적은 에디터가 비활성 창이라(`isFocused` false) 4.3 끝 WebGL에서 확인
- [x] [Editor] 플레이테스트 기록 패널 배선 · 폰트 재생성 — `BoardSceneBuilder.StatsPanel`(불투명 바탕 · 닫기 버튼, 처음엔 꺼짐) · `_statsView`, 폰트 254자 · 샘플링 64 → 56으로 1024² 한 장 유지, `QaScene` 67항목 통과

### 4.3 화면

- [x] [Code] 스테이지 선택 화면 · 클리어 후 "다음 스테이지" · 차례 해금(클리어한 스테이지는 다시 하기) · 최고 기록과 "완벽" 표시 — 씬은 `Board.unity` 하나에 패널 전환(씬 다시 불러오기 없음) → `GameFlow` · `StageSelectView` · `StageButtonView` · `SaveData` · `PuzzleController.Open/Close`(보드 · 조합표는 스테이지마다 다시 만듦) · Core `Progress.NextStage`(+ 테스트, EditMode 108/108). 에디터 플레이: 선택 화면(1번만 열림 · 강조) → 잠긴 버튼 안내 → 포도 5수 클리어 "완벽! 최소 5수 [다음]" → 2번 → 목록: 1번 클리어 + 별 · 2번 강조 · 나머지 잠김, 마지막 스테이지는 "목록"
- [x] [Editor] 스테이지 선택 화면 배선 → `BoardSceneBuilder.SelectScreen`(육각 번호 버튼 3열 · 템플릿 · 안내 띠) · 자물쇠 · 별 아이콘(`Phase1Sprites.BuildIcons`, 128px), 스크린샷 확인
- [x] [Code] 옵션 화면 — 접근성 기호 · 소리 켜고 끄기를 보드 아래 버튼에서 옮김(Phase 2에선 기능만 확인) — 사용자 요청 2026-09-29 · 버튼 배치: 위 ← 목록 · ⚙ 옵션 / 아래 되돌리기 · 처음부터 — 사용자 결정 2026-09-30 → `OptionsView` · 설정 저장(`SaveData.Symbols/Sound`), `SoundController`는 버튼 없이 `SetSoundOn`만. 위 버튼은 글자 "목록" · "옵션"(←· ⚙ 글리프 대신). 에디터 플레이: 기호 켬 · 소리 끔 → 보드에 기호 · 볼륨 0, 플레이를 다시 시작해도 유지
- [x] [Editor] 옵션 화면 배선 · 위아래 버튼 정리 · `QaScene` · `/qa-scene` 갱신 → `OptionsPanel` · 위 버튼 줄 · 아래 버튼 2개(되돌리기 · 처음부터) · 클리어 띠 다음 버튼, `QaScene` 106항목 통과, `BoardQa` 경로 갱신 + 흐름 진입점(`Flow` · `ChooseStage` · `Press`)
- [x] [QA] (사용자) WebGL(휴대폰 · PC, gh-pages `6ca211e` · 압축 후 8,489,105바이트 — Phase 4.1 대비 −17,455) — 선택 → 플레이 → 다음 흐름 · 새로고침 뒤 진행 · 설정 유지 · `?stats` 걸린 시간 누적 · `?reset` 초기화 (4.2 · 4.3 WebGL 확인) → 사용자 보고(2026-09-30): 모두 확인, 문제 없음

### 4.4 팔레트

- [x] [Asset] 파스텔 팔레트(GDD §2.3 예시 · 제작 규칙) — 색값 초안 제안 → 빈칸 포함 모든 색이 휴대폰에서 구분되는지 사용자 확인. 목록은 기본 + 파스텔 2종 — 사용자 결정 2026-09-30 → 초안 `PastelPalette.asset`(분홍 #F08BA8 · 레몬 #EFD95A · 살구 #F5A870 · 하늘 #7DB6EC · 연보라 #B49AE0 · 연두 #98D07C · 차콜 #4A4643, `AgentScripts/Phase4Palettes.cs`) — 휴대폰 확인은 주소 `?palette=pastel` → 사용자 보고(2026-10-01, gh-pages `240ddce`): 색 구분 · 기호 모두 문제 없음(초안 그대로 확정)
- [x] [Code] 기호와 목표 색 마커 겹침 — 칠해졌지만 아직 목표 색이 아닌 칸에서 기호가 마커와 겹쳐 읽기 어려움(Phase 2부터, 4.4 확인 중 발견) → 마커가 보이면 기호를 마커 위(칸 앵커 y 0.64~0.88)로 올리고 자동 크기로 줄임(`CellView.PlaceSymbol`, 사용자 결정 A 2026-10-01). 에디터 스크린샷: 파스텔 검정 만들기 · 기본 큰 벌집(37칸)
- [x] [Code] 스테이지 팔레트 적용 — 스테이지 `palette` 이름으로 팔레트 에셋을 골라 게임 · 레벨 에디터에 표시(이름이 없거나 모르는 이름이면 기본 팔레트), 레벨 에디터는 이름 입력 대신 있는 팔레트에서 고르기(GDD §8), 접근성 기호 글자색은 칸 색의 밝기로 정하기(지금은 노랑 칸만 어두운 글자) — 점검 F2 → `ColorPalette._id` · `PaletteCatalog` · `PuzzleController.ChoosePalette` · `GameFlow` `?palette=` · `BoardView` 기호 글자색(`Color.grayscale` > 0.6 → 어두운 글자, 기본 팔레트는 전처럼 노랑만) · 레벨 에디터 "팔레트:" 순환 버튼. 에디터 플레이: 파스텔로 포도 · 검정 만들기 · 두 가지 색(보드 · 썸네일 · 조합표 · 기호), 모르는 이름 "neon" → 기본 + 경고, 레벨 에디터 포도 → pastel로 저장하면 `"palette":"pastel"` 기록 · 다시 불러오면 pastel(확인 파일은 `RemoveStage`로 치움)
- [x] [Editor] 팔레트 목록 에셋 · 보드 씬 · 레벨 에디터 씬 배선 — 점검 F2 → `Assets/Data/PaletteCatalog.asset`(default · pastel), `BoardSceneBuilder` 두 씬 `_palettes`, `QaScene` 119항목(팔레트 목록 · 이름 · 7색 · 스테이지 팔레트가 목록에 있음)

### 4.5 스테이지

- [x] [Doc] 스테이지 구성안 — 이식 9개 + 새로 1~3개(5수가 이어지는 구간 완화 · 파스텔 팔레트 사용), 순서 · 난이도 곡선(최소 수 · 순서 민감도), 첫 스테이지 규칙 안내 없음(GDD §9 설명 최소화) → 사용자 확정 → 사용자 결정(2026-10-02, 추천안): 개념 순서 11개 — 두 가지 색 → 붓 색이 바뀐다 → 벌집 → 포도 → 얼룩 → **보랏빛 길**(새, 파스텔 5수) → 검정 만들기 → 엇갈림 → **검은 날개**(새, 6수 · 19칸) → 큰 벌집 → 마지막 한 획(파스텔). 새 스테이지는 생성기 후보 3개씩 비교(`Builds/StageCandidates.png`)해 A-2 · B-3 선택
- [x] [Editor] 스테이지 제작 · 등록 · 목록 순서 — 회귀 테스트 기준 파일 분리(4.1)를 먼저 한다(점검 F6). 새 문구 · 이름이 생기면 폰트 재생성 → 레벨 에디터 저장 경로로 `VioletPath.json`(최소 5수 · pastel) · `BlackWing.json`(최소 6수) 저장 · `LastStroke.json` pastel로 다시 저장, 목록 순서 `AgentScripts/Phase4Stages.cs`, 폰트 199자 1장(인스펙터 설명 `[Tooltip]` 글자를 수집에서 뺌 — 278 → 199), `QaScene` 133항목(목록 11개 모두 솔버로 풀리고 `minMoves` 일치 점검 추가), 에디터 플레이: 선택 화면 0 / 11 · 6 · 9 · 11번 보드 스크린샷, 회귀 테스트 그대로 통과(기준 파일 분리 효과). WebGL 압축 후 8,415,508바이트(−72,459, 폰트 글자 감소) · gh-pages `2e7e301`

### 4.6 검증

- [x] [QA] 에디터 플레이로 1번부터 끝까지 · 해금 · 저장 복원 → 선택 화면 0 / 11(1번만 열림)에서 시작해 솔버 풀이를 실제 끌기로 두고 "다음"으로 이어서 11개 모두 "완벽!" 클리어(3 · 3 · 4 · 5 · 5 · 5 · 5 · 5 · 6 · 6 · 7수), 마지막은 [목록] → 11 / 11 · 별 11개(스크린샷). 5개 클리어 뒤 목록: 6번만 다음 표시 · 7번 이후 잠김 · 잠긴 7번 누르면 안내만. 플레이를 다시 켜면 5 / 11 · 11 / 11 그대로 복원. 기록: 스테이지마다 클리어 수 = 최소 수, 보랏빛 길은 열기 2 · 붓질 6(1수 뒤 처음부터 1회 포함). 콘솔 에러 0, 끝난 뒤 에디터 저장 키 삭제. 재사용용 `BoardQa.SolveByDrag` 추가
- [x] [QA] (사용자) 휴대폰 WebGL에서 끌기 도중 끊기면(알림 · 시스템 제스처 · 앱 전환) 획이 그어지지 않고 미리보기가 사라지는지, 그 뒤 보드 입력이 정상인지 확인 — 점검 F4 → 사용자 보고(2026-09-30): gh-pages `5d274d4` 휴대폰에서 확인 완료, 문제 없음
- [x] [QA] (사용자) WebGL(휴대폰 · PC)에서 처음부터 끝까지 연속 플레이 · 새로고침 후 진행 유지 · 빌드 용량 · 첫 로딩 → gh-pages `2e7e301`(압축 후 8,415,508바이트, 4.5 빌드) — 사용자 보고(2026-10-02): 휴대폰 · PC 모두 확인, 문제 없음(첫 로딩 시간 수치는 따로 보고받지 않음)
- [x] [Doc] Phase 4 종료 정합성 점검 — CLAUDE.md §4 현황(Phase 4 완료 빌드 · WebGL 확인) · §2 연속 플레이 QA 방법 갱신, GDD §13 단계 4 · 5(플레이테스트 이동 — 사용자 승인 2026-10-02), 결정 대기 표(이동 횟수 제한 시점 · 파스텔 확정) 갱신, GDD §14 확정 항목 없음(DevelopLog 2026-10-02 Phase 4 종료)

> 플레이테스트 3항목(관찰지 · 동아리원 플레이테스트 · 결과 정리)은 Phase 5 끝으로 옮김(2026-10-02 사용자 결정). 주변인 몇 명에게 비공식으로 보여 줬으나 정리할 만한 데이터는 없음

---

## Phase 5 — 챕터 그림 완성 구조 (GDD §13 단계 5) ✅ (2026-10-03 완료)

> **선행**: Phase 4 완료. 도안 · 색칠 단계 전달 규격(첫 항목)은 Phase 4와 함께 진행해도 된다(아트 준비에 시간이 걸림)
> **완료 조건**: 챕터 1의 스테이지를 클리어할 때마다 그 소재의 색칠 단계가 도안에 한꺼번에 칠해지고, 마지막 스테이지를 클리어하면 그림이 완성되는 흐름을 WebGL 빌드에서 끝까지 플레이할 수 있다. 동아리원 플레이테스트 2항목은 Phase 10으로 옮겼다(2026-10-03 사용자 결정 — 발표 · 공개 테스트까지 시간이 남아 출시 품질 작업을 먼저 한다. 관찰지는 완성되어 Phase 7 끝 소규모 플레이테스트에서도 쓴다).

- [x] [Doc] 도안 · 색칠 단계 전달 규격(아트 담당용) — 선화 1장 + 색칠 단계 N장(단계마다 그 소재의 모든 영역, 레이어별 PSD 또는 같은 캔버스의 투명 PNG), 칠하는 순서 = 스테이지 순서, 캔버스 크기 · 용량 기준(첫 로딩 10초). 여백 자르기 · 줄이기는 빌더가 한다(레이어를 한 장으로 묶지는 않는다 — 2026-10-04 점검 정정) → `ArtSpec.md` v0.1(2160 × 2700 투명 PNG · `00_line` + `NN_<소재>` · 레이어 겹침 금지 · 검수 체크리스트 · AI 제작 방법 A/B) + 공유 페이지(같은 Markdown을 넣어 렌더, "Markdown 복사" 버튼). 사용자 결정(2026-10-03): 이 초안으로 진행하고 작업하며 갱신
- [x] [Asset] 임시 도안(단순 도형 · 색칠 단계 10개 안팎) — 아트가 오면 교체 → `ArtSource/Chapter1/`(`AgentScripts/TempChapterArt.py` — 라벨 지도를 뒤에서 앞으로 칠하고 선화는 경계에서 뽑음): "포도밭 오후" 11단계(해 · 구름 · 벌 · 포도 · 언덕 · 길 · 포도 덩굴 · 울타리 · 나비 · 벌집 · 하늘) + 선화 · `reference.png` · `steps.md`, 합계 552 KB. 규격 점검 `AgentScripts/ArtCheck.py` 통과(종이색으로 남는 곳 0%), 일부러 망가뜨린 사본에서 이름 · 번호 · 행 수 · 크기 · 겹침 · reference 실패를 모두 잡음. 미리보기 `Builds/ChapterArtPreview.png`(선화 → 4단계 → 8단계 → 완성)
- [x] [Code] 챕터 데이터(스테이지 파일 ↔ 색칠 단계) · 진행 표시 · 칠하는 연출 · 완성 연출 → `ChapterArt`(선화 + 단계 조각 · 캔버스 자리, 단계 i ↔ 목록 i번째) · `ChapterView`(종이 · 단계 · 선화를 겹침, 막 칠한 단계는 0.6초 동안 나타나며 1.06배까지 커졌다 돌아옴, 완성이면 그림 전체 1.04배) · `GameFlow`(처음 클리어 → "다음" → 그림 화면 "색칠 n / 11" → 다음 스테이지, 다 칠하면 "그림 완성!" [목록] · 목록 버튼으로 나가면 작은 그림에서 칠함 · 다시 클리어는 그림 화면 없이 다음) · `PuzzleController.FirstClear`. 가공: `ChapterArtExport.py`(절반 크기 · 여백 자르기 · 4의 배수, 12장 440 KB) → `ChapterArtBuilder.cs`. 에디터 플레이: 빈 저장에서 1번 클리어 → 그림 화면(해 칠함, 스크린샷) → 2번 클리어 후 목록 → 작은 그림 01 · 02 · 1번 다시 클리어 → 바로 2번 → 3~11번 이어서(그림 화면 "색칠 3 / 11" … "그림 완성!" [목록], 스크린샷) → 목록 11 / 11 · 작은 그림 11단계. 연출 값 측정: 알파 0 → 0.09 → 0.15 · 크기 1.016 → 1.028, 끝나면 알파 1 · 크기 1
- [x] [Editor] 챕터 화면 배선 → `BoardSceneBuilder`: 선택 화면 위 작은 그림(480 × 600) + 번호 버튼 4열(200 × 230), `ChapterScreen`(진행 글자 · 큰 그림 960 × 1200 · 다음 버튼), `GameFlow` 그림 참조 7개, 폰트 201자 1장, `QaScene` 148항목(그림 단계 수 = 스테이지 수 · 조각 모두 있음 · 캔버스 4:5 · ChapterView 두 곳), `/qa-scene` 갱신. 임시 도안의 구름 색이 종이색과 거의 같아 칠해져도 변화가 안 보여 옅은 회청색으로 바꾸고 `ArtSpec.md`에 규칙 추가
- [x] [QA] 빌드 용량 · 첫 로딩 — 도안 레이어 추가 영향(Phase 4 대비 바이트) → 첫 로딩: 사용자 확인(2026-10-03) 문제 없음(수치는 따로 받지 않음). 용량: 압축 후 8,744,391바이트(Phase 4 대비 +328,883: data 2,464,006 → 2,787,183 · wasm +5,706), 에러 0 · 경고 7(이전과 같음), 빌드 도중 값(`preloadedAssets`) 되돌림, gh-pages `9bc17db`(HTTP 200). 첫 로딩은 사용자 WebGL 확인에서
- [x] [QA] (사용자) WebGL로 챕터 1 처음부터 완성까지 플레이 → gh-pages `9bc17db` — 사용자 보고(2026-10-03): 휴대폰 · PC에서 칠하기 연출 · 작은 그림 · 4열 버튼 · "그림 완성!" · 새로고침 유지 확인, 문제 없음
- [x] [Doc] 플레이테스트 관찰지 — GDD §9 관찰 항목 + `?stats` 기록 읽는 법 (Phase 4.6에서 이동, 2026-10-02 — 진행 대본 · 관찰 표 · 끝난 뒤 질문 초안은 DevelopLog 같은 날짜) → 공유 페이지 `https://claude.ai/artifact/5cRnbjihuRdgSYZcbafzTk`(진행 순서 · 대본 · 기록 칸 보는 법 · `?stats` 읽는 법 + 참가자별 관찰 기록 — 페이지 db `sessions/<코드>`에 자동 저장, 결과 정리 때 그대로 읽음). 사용자 확인(2026-10-03) — 휴대폰 저장 시험 기록(빈 `P2`)은 지움, "새 사람"이 손대지 않은 P1을 건너뛰던 것 수정
- [x] [Doc] Phase 5 종료 정합성 점검 — EditMode 108/108(CLI `unity command run_tests`, 2026-10-03). 출시 품질 목표 · Phase 6~10 재구성(사용자 결정 2026-10-03: Phase 5를 닫고 플레이테스트는 Phase 10 · Phase 7 뒤 소규모 플레이테스트 · 첫 출시 플랫폼은 Phase 6에서 · 사운드는 무료 에셋부터). GDD §13 · §14 개정(2026-10-03 승인), CLAUDE.md 변경 없음(DevelopLog 2026-10-03 Phase 5 종료)

> 동아리원 플레이테스트 · 결과 정리 2항목은 Phase 10으로 옮김(2026-10-03 사용자 결정)

---

## Phase 6 — 출시 방향 결정 (GDD §13 단계 6) ✅ (2026-10-03 완료)

> **선행**: Phase 5 완료
> **완료 조건**: 정식 제목 · 세계관 · 아트 스타일 · 첫 출시 플랫폼(과 언어) · 출시 범위(챕터 수 · 기믹 후보)가 정해져 GDD에 반영되어 있고(승인 후), Phase 7의 세부 항목이 확정되어 있다. Phase 8 · 9 세부 항목은 착수 때 확정한다(8은 소규모 플레이테스트 결과, 9는 그때의 앱인토스 조건에 달려 있음 — 2026-10-03 조정).

- [x] [Doc] 결정 문서(공유 페이지) — 결정마다 선택지 · 영향 · 추천 → `https://claude.ai/artifact/1dxtht2m6McLSaju3fvJSq`(D1 세계관 · D2 아트 스타일 · D3 첫 출시 플랫폼 · 언어 · D4 출시 범위 · D5 기믹 후보 · D6 정식 제목, 2026-10-03 게시 · 사용자 검토 후 결정)
- [x] [QA] 아트 스타일 후보 확인 — 사용자 시험 그림 `ArtSource/chapter1_test/`(11단계)를 `ArtCheck.py`로 점검하고 칠하는 과정 미리보기. 게임 데이터 · 임시 도안은 바꾸지 않는다 → 규격 점검 통과(규격 밖 파일 `preview_progress.png`만 걸려, 뺀 사본으로 다시 돌려 통과 · 종이색으로 남는 곳 0%). 에디터 대신 게임과 같은 순서(종이 → 단계 → 선화)로 합성한 미리보기 `Builds/Chapter1TestPreview.png`(0 · 3 · 6 · 9 · 11단계, 폭 300 px) — 작게 봐도 소재가 읽힘. 가공 결과 12장 1,130,634바이트(임시 도안 440,823) · 텍스처 면적 6,987,232 px(임시 도안 4,574,304 — 약 1.5배)
- [x] [Doc] (사용자) 결정 — 제목 · 세계관 · 아트 스타일 · 첫 출시 플랫폼 · 언어 · 출시 범위 · 기믹 후보 → 사용자 결정(2026-10-03): 세계관 = 그림책 풍경(챕터마다 장소 · 계절 한 장면, 이야기 없이 챕터 제목만) · 아트 = 굵은 선 플랫 카툰 · 첫 출시 = 앱인토스 · 한국어(GDD §11 그대로) · 범위 = 챕터 5개 · 스테이지 55개 안팎 · 기믹 3~4개 · 기믹 후보 = 벽 · 코팅 칸 · 물 칸(확정은 Phase 8) · 제목 = "컬러링붓" 유지, 영어 이름은 따로
- [x] [Doc] 결정 반영 — GDD §1 · §5 · §7 · §11 · §14(승인 후) · `ArtSpec.md`(아트 스타일) · CLAUDE.md(플랫폼이 바뀌면 §4 · §6) → 사용자 승인(2026-10-03): GDD §1(제목) · §5(정한 사항 2026-10-03) · §7(후보) · §11(한국어) · §13(1~5 완료 · 6~10 단계) · §14 체크. `ArtSpec.md` v0.2(세계관 · 스타일 행) + 공유 페이지 다시 게시. CLAUDE.md는 플랫폼이 그대로라 변경 없음
- [x] [Doc] Phase 7~9 세부 항목 확정 → Phase 7 세부 항목 사용자 승인(2026-10-03, 챕터 1 그림을 시험 그림으로 교체 포함). Phase 8 · 9는 완료 조건만 결정에 맞춤(챕터 5개 · 앱인토스), 세부는 착수 때

---

## Phase 7 — 화면 · 연출 완성도 · 튜토리얼 · 힌트 · 소규모 플레이테스트

> **선행**: Phase 6 완료(아트 스타일 · 언어가 화면 디자인을 정한다)
> **완료 조건**: 타이틀부터 그림 완성까지 디자인 기준에 맞는 화면 · 전환 · 연출로 플레이할 수 있고(소리는 Phase 8로 이동, 2026-10-04)(WebGL 휴대폰 · PC), 처음 하는 사람이 튜토리얼로 규칙을 익히고 막히면 힌트를 받을 수 있으며(2026-10-07 추가), 소규모 플레이테스트 결과가 정리되어 있다. 세부 항목 사용자 승인(2026-10-03 · 7.8은 2026-10-07).

### 7.1 디자인 기준

- [x] [Doc] 디자인 기준 — 그림의 진한 갈색 선 · 종이색에 맞춘 색 · 버튼 · 간격 · 글꼴. 시안 페이지로 사용자 확인 → 시안 페이지 `https://claude.ai/artifact/2WuvcLMwo6afFVsdWKF2r1`(A 종이와 갈색 선 · B 스티커 · C 차분한 갈색) — 사용자 결정(2026-10-03): **A**. 색은 그림에서 뽑음(선 #382008 · 바탕 #F1EBDD · 버튼 바탕 #FFFDF6 · 보조 글자 #7B6650 · 잠김 #E3DAC6 · 다음 스테이지 테두리 = 포도색 #8C58A0), 글꼴은 Pretendard 그대로. 선택 화면에 챕터 제목 줄("포도밭 오후", GDD §5) 추가
- [x] [Code] 디자인 기준 값을 ScriptableObject로(튜닝 값 — CLAUDE.md §5) · 화면 요소가 읽게 → `UiTheme`(`Assets/Data/UiTheme.asset`) — 빌더가 읽어 씬 · 프리팹 · 컴포넌트 색(`StageSelectView` · `BoardView` 빈칸 · `MixTableView` · `ChapterView` 테두리)에 넣는다(런타임 참조 없음). `ChapterView`에 그림 테두리(9-slice, 완성 연출 때 그림과 함께 커짐). 버튼 모양 스프라이트 4종 `Phase1Sprites.BuildRound`(둥근 사각형 바탕 · 외곽선 반지름 32 선 7 · 그림 테두리 반지름 12 · 육각 외곽선). 컴파일 에러 0
- [x] [Editor] 모든 화면에 적용(`BoardSceneBuilder`) · `QaScene` 갱신 → 위 버튼 · 아래 버튼 · 패널 버튼 · 띠 · 안내 띠 · 옵션 · 기록 패널을 둥근 모양으로, 그림 화면 "다음"만 갈색으로 채움, 카메라 바탕 · 칸 테두리 · 방향 버튼 · 스테이지 버튼(외곽선 · 포도색 다음 표시) 색. 선택 화면 그림 −150 → −210 · 격자 −790 → −850(제목 줄 자리). 폰트 204자 1장, `QaScene` 149항목 통과(점검 코드는 바꿀 것 없음 — `/qa-scene` 카메라 색 문구만 갱신). 에디터 플레이 스크린샷: 선택 화면 · 보드(끌기 미리보기) · 클리어 띠 · 그림 화면 · 옵션(`Builds/Ui71_A.png` · `Ui71_options.png`), 콘솔 에러 0. WebGL 확인은 7.7

### 7.2 챕터 1 그림 교체

- [x] [Asset] 챕터 1 그림을 사용자 시험 그림(`ArtSource/chapter1_test`, 굵은 선 플랫 카툰 11단계)으로 — 이것도 시험용이라 나중에 다시 바꿀 수 있다. 임시 도안 `ArtSource/Chapter1`은 그대로 둔다(사용자 결정 2026-10-03) → 원본 커밋 `9a8f884`(작업 도구 `_tools/`는 `.gitignore`, 규격 밖 `preview_progress.png`는 `_tools/`로 옮김). `ChapterArtExport.py` · `ArtCheck.py` 기본 원본을 이 폴더로
- [x] [Editor] 가공 · 그림 에셋 다시 만들기(`ChapterArtExport.py ArtSource/chapter1_test` → `ChapterArtBuilder`) · `QaScene` · 용량 비교 → 12장 1,130,634바이트(옛 레이어 10장 · `.meta` 삭제), `Chapter1Art.asset` GUID 그대로라 씬 재구성 없음, `QaScene` 148항목 통과. 에디터 플레이(CLI): 1번부터 11번까지 그림 화면 "색칠 1 / 11" … "그림 완성!" [목록] · 작은 그림 11단계 · 콘솔 에러 0(스크린샷 `Builds/Art72_pair.png`). WebGL 압축 후 9,106,992바이트(Phase 5.3 대비 +362,601 — 전부 data, wasm 같음), 에러 0 · 경고 4, `preloadedAssets` 되돌림. gh-pages 배포 · 휴대폰 확인은 7.7에서

### 7.3 타이틀 · 화면 전환

- [x] [Code] 타이틀 화면("컬러링붓" 로고 · 시작) · 화면 사이 짧은 페이드 → `GameFlow`: 실행하면 타이틀(주소 `?stage=`면 건너뜀) → [시작] → 선택 화면. `ScreenFade`(CanvasGroup 투명도 0 → 1, 0.2초, 입력은 막지 않음)를 각 화면 · 패널에 붙여 켜질 때마다 나타남 — 화면 전환 코드는 그대로. 로고 그림은 아직 없어 제목은 글자(Pretendard 160) + 기본색 세 칸. 에디터 플레이: 타이틀 → 시작 직후 선택 화면 투명도 0 → 잠시 뒤 1 → 1번 클리어, 콘솔 에러 0
- [x] [Editor] 타이틀 화면 배선 · `QaScene` 갱신 → `BoardSceneBuilder.TitleScreen`(기본색 세 칸 · 제목 · "붓으로 칠하는 육각 퍼즐" · 채운 [시작]) · 화면 6개에 `ScreenFade`, `GameFlow` 참조 2개, 폰트 217자 1장, `QaScene` 161항목(타이틀 패널 · 화면마다 ScreenFade · 참조), `BoardQa.Flow`에 타이틀 표시. 스크린샷 `Builds/Ui73_title.png`

### 7.4 연출

- [x] [Code] 붓질 애니메이션(경로를 따라 칠해짐) · 클리어 반응 · 막힘 흔들림 · ~~진동~~ → 진동은 뺌(사용자 결정 2026-10-03 — 심사 기준에 없고 iOS 웹은 지원 안 함). 붓질: 긋기 전 `PuzzleSession.Trace`로 경로 · 원래 색을 받아 칸마다 0.04초 간격으로 새 색 + 1.12배 튐(`BoardView.PlayStroke` — 규칙 복제 없음, 세션 상태는 즉시 반영). 클리어: 물결 뒤 모든 칸이 차례로 튐 → 클리어 띠. 새로 막힘: 물결 뒤 보드 좌우 흔들림(18 · 0.35초) + 막힘 띠(띠 표시는 연출 뒤 — 사용자 결정 ②A). 다음 획 · 되돌리기 · 처음부터 · 기호 전환은 연출을 끝난 모습으로 정리. 붓 모양 아이콘 없음(③A). 측정(에디터 플레이, 측정 동안만 연출 시간을 늘림): 획 직후 경로 칸 색이 아직 이전 색 3칸 → 0, 튐 3 → 1 → 0, 흔들림 x −10.1 → −0.5 → 0과 동시에 막힘 띠, 클리어 띠는 획 직후 꺼짐 → 반응 뒤 켜짐, 끌고 바로 되돌리기 → 화면 = 상태. 1~11번 연속 풀이(빠른 연속 끌기) 11 / 11, EditMode 108/108, 콘솔 에러 0
- [x] [Editor] 연출 값(속도 · 크기) 에셋 배선 → `MotionSettings`(`Assets/Data/MotionSettings.asset` — 값을 바꾸면 바로 반영) · 플레이 보드만 참조(목표 썸네일 · 레벨 에디터는 연출 없음), 클리어 · 막힘 띠에 `ScreenFade`, `QaScene` 165항목. 스크린샷 `Builds/Ui74_pair.png`(클리어 반응)

### 7.5 사운드 → Phase 8로 이동

> 효과음 · 배경음 · 재생 연결 3항목은 Phase 8로 옮김(2026-10-04 사용자 결정 — 소리는 사용자가 직접 찾아오기로 했고, 소규모 플레이테스트는 소리 없이 먼저 한다)

### 7.6 로딩 화면 · 제품 정보

- [x] [Editor] 전용 WebGL 로딩 화면(템플릿) · 아이콘 · 제품 이름 "컬러링붓" · 회사 이름(지금은 Unity 기본 템플릿 · `DefaultCompany`) → `Assets/WebGLTemplates/ColoringBoot`(종이 바탕 · 기본색 세 칸 · "컬러링붓" · 둥근 갈색 진행 막대 · "불러오는 중 n%", 그림 · 웹 글꼴 없음, 오류는 로딩 화면에 한국어로) · PC는 창에 맞춘 9:16 영역 + 양옆 여백(사용자 결정 A), 휴대폰은 기존 뷰포트 그대로 · favicon 32 · 홈 화면 아이콘 180(육각 세 칸) · 제품 이름 "컬러링붓"(`AgentScripts/Phase7WebTemplate.cs`), 회사 이름은 나중에(사용자). 빌드 성공 · 압축 후 9,127,968바이트(7.2 대비 +20,976 — 7.1~7.4 포함, 템플릿 파일은 Build 밖 약 14 KB). 헤드리스 Chrome: 로딩 화면 · PC 1280×720 · 1280×500 레터박스 확인. **로드 뒤 화면 · 휴대폰은 헤드리스에서 WebGL이 끝까지 돌지 않아 실기 확인 대기(7.7)** · gh-pages `dcd10eb`(HTTP 200, 탭 제목 "컬러링붓"). PC 크롬에서 `RangeError: Maximum call stack size exceeded`(사용자 보고) → 원인: 빌드 파일 이름이 매번 같아 브라우저가 옛 배포 파일(캐시 10분)과 새 파일을 섞음(휴대폰 · 로컬 · 가로 화면 강제는 정상, 몇 분 뒤 저절로 풀림 — 사용자 확인). 수정: `Name Files As Hashes` 켬 · 템플릿 CSS를 `index.html` 안으로 → 9,128,611바이트, gh-pages `da45b01` — 이 PC 크롬에서 처음 열기 · 캐시로 다시 열기 모두 타이틀까지 정상, 콘솔 오류 0. 사용자 확인(2026-10-04): 휴대폰 · PC 로딩 화면 → 타이틀 · 진행 유지 정상, 첫 로딩 시간 이전과 거의 같음

### 7.7 검증

- [x] [QA] 에디터 연속 플레이 — 타이틀 → 챕터 1 그림 완성, 콘솔 에러 0 → 7.3(타이틀 → 선택 → 1번) · 7.4(1~11번 연속 풀이 · 그림 완성, 붓질 연출 포함) 에디터 플레이, 콘솔 에러 0
- [x] [QA] WebGL 빌드 용량 · 첫 로딩(그림 · 사운드 · 폰트 추가 영향, Phase 5 대비 바이트) → 사운드 전까지: 9,128,611바이트(Phase 5.3 8,744,391 대비 +384,220 — 챕터 1 그림 교체 +362,601 · 화면 · 연출 · 템플릿), 첫 로딩 이전과 거의 같음(사용자 2026-10-04). 사운드는 Phase 8에서 다시 잼
- [x] [QA] (사용자) 휴대폰 · PC WebGL — 화면 · 전환 · 연출 · ~~소리~~ · ~~진동~~ → 화면 · 전환 · 연출 · 로딩 화면 · 진행 유지는 사용자 확인(2026-10-04, gh-pages `da45b01`). 소리는 Phase 8
- [ ] [QA] (사용자) 소규모 플레이테스트 3~5명 — **7.8 튜토리얼 · 힌트 뒤에 한다**(2026-10-07 사용자 결정 — 비공식으로 먼저 보여 준 결과는 GDD §9 · DevelopLog 2026-10-07). 관찰지(Phase 5) 사용, GDD §9 관찰 항목(2026-10-03 사용자 결정) — 소리 없이 지금 빌드로 진행(2026-10-04 사용자 결정 A). 관찰지를 지금 빌드에 맞게 고침(타이틀 [시작] · 클리어 반응 뒤 띠 · 소리 없음 안내) — 지금 배포본(9,128,611바이트, 2026-10-03 빌드 — gh-pages 정리 뒤 `6b74274`)
- [x] [Doc] 관찰지 · 기록 화면을 따라 하기 · 힌트에 맞게 고침(2026-10-07 사용자 승인) → 관찰지 공유 페이지 5판: 진행 순서([시작] → 따라 하기 5판 → 챕터 1) · 대본 "색칠 퍼즐이에요. 처음엔 게임이 방법을 알려 줘요."(목표 그림 설명은 뺌) · 관찰 항목 5 "따라 하기만으로 규칙을 이해하는가"(GDD §9에도 추가) · 따라 하기 기록 칸(헤맴 · 그림을 봄 · 복습: 혼자 풂 · 막혔다가 되돌림 · 힌트 씀) · 스테이지 "힌트 씀" 칸 · 끝난 뒤 질문 2개(따라 하기에서 가장 헷갈린 것 · 힌트가 도움이 됐나) · `?stats` 힌트 읽는 법. 저장 형식은 칸만 더함(옛 기록도 읽음, 저장된 기록 0건). `?stats` 기록 화면에 따라 하기 5판도 표시(`StatsView.Show(목록, 따라 하기, 기록)` · 글자 자동 크기 20~34 — 16판이 한 화면, 에디터 플레이 확인) · WebGL 9,212,003바이트 배포(HTTP 200) · Claude in Chrome `?stats` 확인
- [ ] [Doc] 소규모 플레이테스트 결과 정리 → 이동 횟수 제한 · 기믹 결정 근거(GDD §9)
- [x] [Editor] (사용자) gh-pages를 커밋 하나로 정리 — 배포 스크립트는 바꿨으나(2026-10-04 구조 정리 B⑤) 강제 push를 자동 모드 안전 장치가 막아 사용자가 직접 실행: `! bash AgentScripts/Tools/deploy-pages.sh "deploy: 배포 기록을 커밋 하나로 정리"` → 사용자 실행(2026-10-04): gh-pages `6b74274` 커밋 1개(부모 없음, 이전 `da45b01`과 트리 같음) · 원격 반영 · 배포 주소 index와 빌드 파일 4개 HTTP 200 · 크기 같음
- [ ] [Doc] 지난 기록 보관 — Task.md Phase 0~6 · DevelopLog 2026-10-03 이전 기록을 `Docs/Archive/`로 옮기고 Task.md에는 Phase마다 한 줄 요약(2026-10-04 구조 정리 C) — Phase 7 종료 점검 때 함께
- [x] [Code] 코드 점검 수정 2건(2026-10-05 코드 총점검 8 · 9, 사용자 결정 "지금 고쳐라") — ⑧ 옵션 · 기록 패널이 보드를 덮어도 PC 키보드(숫자 · 방향키 · Ctrl+Z)가 뒤의 보드를 움직이던 것: `PuzzleController._overlays`(보드를 덮는 패널) 중 하나라도 열려 있으면 키보드 무시 · ⑨ 끄는 도중 되돌리기 · 처음부터(Ctrl+Z · 다른 손가락)를 하면 옛 미리보기가 남고 떼면 새 상태에 그어지던 것: 되돌리기 · 처음부터가 끌기를 취소(`BoardView.CancelDrag`). 에디터 플레이: 끄는 중 되돌리기 → 미리보기 6구간 · 4칸 → 0 · 0, 손을 떼도 수 0 그대로 · 처음부터도 같음 · 그 뒤 정상 끌기 1수, 옵션 · 기록 패널을 열면 "덮임" 참 · 닫으면 거짓(키보드 자체는 에디터에서 못 눌러 판정 함수로 확인), EditMode 108/108, 콘솔 에러 0
- [x] [Editor] 보드를 덮는 패널 배선 — `BoardSceneBuilder`가 `PuzzleController._overlays` = 옵션 · 기록 패널(배열 연결 함수 `SetArray` 추가) → `BuildScene` 다시 실행, `QaScene` 172항목(패널 연결 점검 추가)
### 7.8 튜토리얼 · 힌트 (2026-10-07 추가 — GDD §6)

- [x] [Doc] 설계 확정 — 따라 하기 3개(붓질 · 섞기 · 되돌리기) · 한 줄 문구 · 힌트 탐색 상한 · 화면 배치(아래 버튼 3개 · 안내 띠) → 4칸짜리 판 3개(각 1수, 3번은 일부러 막히는 획 → 되돌리기 → 본 풀이) · 문구는 `TutorialLessons`(코드 문자열 — 폰트 빌더가 모음) · 힌트 상한 상태 20만 개 · 아래 버튼 되돌리기 · 처음부터 · 힌트 · 안내 띠는 막힘 띠 자리(보라). "처음 섞일 때 · 막힐 때" 한 줄 안내는 레슨 2 · 3이 같은 순간을 가르쳐 따로 만들지 않음 — 처음 겪는 안내는 기믹이 생길 때(DevelopLog 2026-10-07)
- [x] [Code] 힌트 계산(Core) — 다음 한 수 · 남은 수, 지금 상태로 풀 수 없으면 몇 수 되돌려야 하는지, 탐색 상한 + EditMode 테스트 → `PuzzleSession.FindHint` · `Hint`, `HintTests` 4/4(힌트만 따라 하면 최소 수로 풀림 · 막다른 길 = 1수 · 막힘 = 2수 · 상한 = 모름)
- [x] [Code] 플레이 기록에 힌트 횟수 — 저장 형식 `stats 2`(`stats 1`도 읽음) + 테스트 · 기록 패널 표시 → `PlayStats.Hinted`, `PlayStatsTests` 4/4(옛 형식 읽기 포함), `StatsView` "힌트 n", 에디터 플레이 저장 값 `stats 2 … TwoColors 1 5 2 0 3 …`
- [x] [Code] 힌트 버튼 · 안내 띠 · 보드 손가락 표시(미리보기 재사용) · 광고 자리(보상형 — 지금은 바로 끝남). "완벽"은 힌트와 무관(GDD §5) → `PuzzleController.RequestHint` → `IAdService.ShowRewarded` → `FindHint` → `BoardView.ShowGuide`(줄의 출발 끝에서 끄는 손가락 + 미리보기) · 안내 띠(막힘 띠는 가림). 에디터 플레이: 1번 시작 "남은 3수" · (0,0) 5시 뒤 "지금은 풀 수 없어요 · 1수 되돌려 보세요" · 막힌 뒤도 같음 · 힌트 3번 쓰고 "완벽! 최소 3수"
- [x] [Code] 튜토리얼 — 따라 하기(정해진 획만 · 손가락 표시 · 한 줄 설명) · 첫 실행 때 자동 · 옵션 "규칙 다시 보기" · `?reset`이면 다시 보임 → `TutorialLessons` · `PuzzleController.OpenLesson`(안내한 줄 · 방향만 받음) · `GameFlow.StartGame`(`SaveData.TutorialSeen`) · `OptionsView.TutorialRequested` · `ClearRecords`가 봤음 기록도 지움. 따라 하기 중 힌트 버튼은 흐리게
- [x] [Editor] 따라 하기 스테이지 3개 · 목록 · 배선(힌트 버튼 · 안내 띠 · 옵션 버튼) · 폰트 · `QaScene` → `Assets/Data/Stages/Tutorial/`(붓질 · 섞기 · 되돌리기) · `TutorialCatalog`(`StageOrder.SetTutorialOrder`) · 빌더 배선 · 폰트 256자 1장 · `QaScene` 214항목(레슨 수 · 안내대로 풀림 · 일부러 막히는 획 막힘 · 문구 글자 포함) · `/qa-scene` 문서
- [x] [QA] 에디터 플레이 — 튜토리얼 처음부터 끝까지 · 힌트(다음 한 수 · 막다른 길 · 막힘 상태) · 힌트 계산 시간 실측 → 저장 키 없이 [시작] → 레슨 1(다른 방향은 무시 · 같은 줄 다른 칸은 받음) → 2 → 3(일부러 막힘 → 막힌 동안 다른 획 무시 → 되돌리기 → 본 풀이) → 챕터 1 목록 · 봤음 저장, 옵션 "규칙 다시 보기" → 레슨 1. 힌트 시간(에디터): 챕터 1 최악 21 ms · 시험 목록 하늘과 언덕 시작 265 ms. EditMode 112/112, 콘솔 에러 0
- [x] [QA] WebGL 빌드 · 배포 · 브라우저 확인 → 압축 후 9,192,735바이트(+42,448: 따라 하기 · 힌트 · 솔버가 빌드에 들어감 · 폰트 256자), 에러 0 · 경고 7(이전과 같음), 빌드 도중 값(`preloadedAssets`) 되돌림, 배포 index · 빌드 파일 4개 HTTP 200. Claude in Chrome(PC): [시작] → 레슨 1 안내 띠 · 손가락 · 미리보기 → 방향 버튼으로 풀어 "처음 만난 색으로 칠해요", 힌트 계산 시간 하늘과 언덕(시험 목록) 시작 **250 ms** · 벌 · 검은 날개 50 ms 이하(50 ms를 넘을 때만 로그), 콘솔 오류 0
- [x] [Doc] 따라 하기 보강 설계(2026-10-07 사용자 — 테스터가 붓이 줄 전체를 지나는 것 · 섞인 뒤 붓 색이 바뀌어 끝까지 칠하는 것 · 목표가 오른쪽 위 그림이라는 것을 몰랐음, 마지막에 전체 복습) → 레슨 5개: 1 붓질(클리어 때 "오른쪽 위 그림처럼 칠하면 성공!" + 목표 그림 강조) · 2 끝에서 끝까지(새로 — 손가락이 줄 가운데 칸에서 시작, 뒤쪽 칸도 칠해짐) · 3 섞기(섞인 뒤 빈칸 2개 · 노랑 칸에서 시작 · "노랑을 만난 뒤로는 주황으로 칠해져요") · 4 되돌리기 · 5 복습(새로 — 혼자 풀기 3수, 힌트 사용 가능). GDD §6 · §9 반영(사용자 승인)
- [x] [Code] [Editor] 따라 하기 보강 → `TutorialSweep` · `TutorialReview` 새로 · `TutorialMix` 5칸, `TutorialLessons`(혼자 풀기 레슨 `Note` · 목표 강조 `Goal`), `PuzzleController`(혼자 풀기 레슨은 아무 획 · 힌트 켬, 레슨 1 클리어 띠가 떠 있는 동안 목표 그림이 커졌다 작아짐), 클리어 띠 글자 한 줄 고정 + 자동 크기(32~52), 폰트 270자 1장, `QaScene` 218항목(혼자 풀기 레슨 = 솔버 최소 수). EditMode 112/112. 에디터 플레이: 1 클리어 → 그림 강조(크기 1.11) · 문구 한 줄 / 2 가운데 칸 끌기 → 5칸 모두 빨강 / 3 노랑 칸 끌기 → 미리보기가 노랑부터 주황 · 결과 [빨 빨 주 주 주] · 문구 한 줄(처음엔 두 줄로 꺾여 한 줄 고정으로 고침) / 4 일부러 막힘 → 되돌리기 → 본 풀이 / 5 대각선 먼저 → 위 줄 → 막힘 → 힌트 "1수 되돌려 보세요" → 되돌리기 → 힌트 "남은 2수" → 3수로 클리어 "준비 끝! 이제 시작해요" → 목록 · 봤음 저장, 콘솔 에러 0. WebGL 9,211,827바이트(+19,092, 에러 0 · 경고 7, `preloadedAssets` 되돌림) 배포 · 빌드 파일 HTTP 200, Claude in Chrome(PC): 옵션 "규칙 다시 보기" → 레슨 1 ~ 5(그림 강조 · 클리어 문구 한 줄 · 복습 안내 · 힌트 "남은 3수"), 콘솔 오류 0. 레슨 2 · 3 클리어 문구를 보이는 결과로 바꿈(사용자 — "붓"은 화면에 안 보임): "어느 칸에서 끌어도 줄 끝부터 칠해져요" · "노랑을 만난 뒤로는 주황으로 칠해져요"(한 줄 44pt 이상), 폰트 274자 · `QaScene` 218항목 · WebGL 9,214,852바이트 배포(HTTP 200)
- [x] [QA] (사용자) 휴대폰 · PC WebGL — 튜토리얼 처음부터 끝까지(레슨 5개) · 힌트(다음 한 수 · 막혔을 때) · 옵션 "규칙 다시 보기". 휴대폰에서 `?lab&stage=LabSkyHill` 시작 힌트가 멈칫하면 계산을 여러 프레임에 나눈다(PC 250 ms — 챕터 1은 50 ms 이하) → 휴대폰: "살짝 멈칫하지만 의식하고 봐야 느껴질 정도, 거의 문제없음"(2026-10-07 사용자) → 한 프레임 계산 유지(큰 맵이 늘면 다시 본다). 레슨 5개 빌드(9,214,852바이트): "확인했다. 이제 튜토리얼은 적절한 것 같다"(2026-10-07 사용자)
- [ ] [Doc] Phase 7 종료 정합성 점검

---

## Phase 8 — 콘텐츠 확장 · 사운드

> **선행**: Phase 7 완료(소규모 플레이테스트 결과)
> **완료 조건**: 챕터 5개 · 스테이지 55개 안팎 · 기믹 3~4개(2026-10-03 결정)를 처음부터 끝까지 플레이할 수 있다(WebGL). 세부 항목은 착수 때(소규모 플레이테스트 결과로) 확정한다.

### 사운드 (Phase 7.5에서 이동, 2026-10-04)

- [ ] [Asset] (사용자) 효과음 · 배경음 — 후보 페이지 `https://claude.ai/artifact/5KVFJXPZhQcXi71yTz1Nn4`(Kenney CC0 효과음 상황별 3개 · 배경음 3개)는 마음에 드는 것이 없어 **사용자가 직접 찾아오기로 미룸**(2026-10-03). 받으면 `ArtSource/Audio/`에 + 출처 · 라이선스. 원래 범위: 효과음 7종 안팎(붓질 · 섞임 · 클리어 · 막힘 · 버튼 · 그림 칠하기 · 완성) · 배경음 1~2곡 — 무료 에셋(CC0 등), 출처 · 라이선스는 루트 `CREDITS.md`(2026-10-04 구조 정리 — 에셋으로 임포트되지 않게 `Assets/` 밖), 나중에 교체 가능(사용자 결정 2026-10-03)
- [ ] [Code] 소리 재생 연결 — 켜고 끄기 · 백그라운드 전환 시 정지를 실제 소리로 확인(Phase 2는 구조만)
- [ ] [Editor] 오디오 임포트 설정(압축 · 모노 등) · 배선 — 용량 보고(배경음이 가장 큼)
- [ ] [QA] 사운드 넣은 빌드 용량 · 첫 로딩 · 휴대폰 · PC 소리 확인(켜고 끄기 · 백그라운드 정지 · 첫 입력 전 무음)

### 구조 — 착수 때 먼저 (2026-10-04 구조 정리 C)

- [x] [Code] 폰트 빌더(`AgentScripts/Build/FontBuilder.cs`)가 하위 폴더까지 읽게(Game 코드 · 스테이지) + `QaScene`에 "스테이지 이름 · 씬 글자가 모두 폰트에 있음" 점검 — 폴더를 나누기 전에 한다(지금은 바로 아래 파일만 읽어, 나누면 글자가 빠져 게임에 □) → 2026-10-05 앞당겨 함(Phase 7 중, 스크립트 폴더 정리의 선행): Game 코드 · 스테이지 폴더를 하위 폴더까지 읽음 · `[Header]` 줄도 제외(화면에 안 나오는 인스펙터 머리글 17자가 모이던 것) · 읽기 전용 `Preview`, `QaScene`에 씬 글자 · 스테이지 이름 점검 2개 → 171항목. 모으는 글자 215자가 폰트(217자 — 남는 타 · 틀은 다음 폰트 재생성 때 빠짐) 안에 모두 있음
- [ ] [Editor] 챕터 데이터 자리 — `Assets/Data/Chapters/ChapterN/`(Stages · 그림 에셋 · 챕터 정보) · `Palettes/`(+ PaletteCatalog) · `Settings/`(UiTheme · MotionSettings)로 옮기고 빌더 경로 상수를 함께 바꾼다. 빌더는 챕터마다 복사하지 않고 챕터 번호를 인자로(그림 빌더 · 순서 · 레벨 에디터 저장 경로 · `RemoveStage`). 챕터 구조 코드(`GameFlow` 여러 챕터 · 목록)는 착수 때 세부 확정
- [ ] [Code] 콘텐츠 회귀 테스트(EditMode) — 모든 게임 스테이지가 읽히고 · 풀리고 · `minMoves`가 맞고 · 파일 이름(저장 키)이 챕터끼리 겹치지 않음 → `/phase-close` 테스트 관문이 콘텐츠도 지킨다
- [x] [Code] Game 스크립트 하위 폴더 — `Board/` · `Screens/` · `Data/` · `Platform/`(저장 · 광고 · 소리 · 주소 — Phase 9 앱인토스 교체 범위), `.meta`와 함께 옮김. 폰트 빌더 수정 뒤 → 2026-10-05 앞당겨 함(사용자 결정 — Core도 함께): Core `Rules/` · `Stages/` · `Solver/` · `Records/`, Game `Board/` · `Screens/` · `Data/` · `Platform/` — 36개를 `AssetDatabase.MoveAsset`으로 옮김(내용 그대로 R100 · 스크립트 GUID 36개 같음 · 네임스페이스 그대로). 재컴파일 에러 0 · EditMode 108/108 · `QaScene` 171항목 · 모으는 글자 그대로
- [x] [Code] `BoardView`를 partial 파일 4개로 나눔(동작 그대로) — `BoardView.cs`(필드 · 칸 만들기 · 그리기 · 배치 · 선택) · `.Input` · `.Preview` · `.Motion`. 2026-10-05 사용자 결정("프로젝트 관리에 유리한 쪽" → 기믹 변경과 섞이지 않게 지금) — 메서드 블록을 스크립트로 그대로 옮김(원본 내용 줄이 빠짐없이 한 번씩 · 바뀐 줄은 클래스 선언 하나 · GUID 같음). 에디터 플레이: 미리보기 6구간 · 결과 4칸, 끌기 · 탭 · 방향 버튼 · 되돌리기 · 처음부터, 막힘 · 클리어 안내가 연출 뒤 켜짐, 그림 화면 → 2번, 키보드 칸 이동 · 긋기, 콘솔 에러 0
- [ ] [Editor] `InputSystem_Actions` 정리 — 프로젝트 전체 입력 등록만 돼 있고 코드 · UI 모듈은 쓰지 않음. 등록 해제 · 삭제 뒤 사운드 빌드에서 입력 정상 · 빌드마다 되돌리던 `preloadedAssets` 변화가 사라지는지 확인(ProjectSettings 변경 — git으로만 되돌림)
- [ ] [Code] 붓질 계산을 하나로 — 지금 `Board.Brush`(실제 붓질)와 `Board.Trace`(미리보기)가 같은 순회를 따로 한다. 기믹을 넣으면 두 곳을 똑같이 고쳐야 하고, 어긋나면 미리보기와 결과가 달라진다 → 계산은 한 곳(예: Brush가 Trace 결과를 적용), 같은지 보는 테스트(지금 포도 하나)를 모든 스테이지로. 기믹 착수 전 (2026-10-05 코드 총점검 4)
- [ ] [Code] 진행 규칙을 Core로 — 다음 스테이지 · 칠한 단계 · 챕터 완성 · 챕터 사이 해금을 `Progress`처럼 순수 C# + 테스트로, `GameFlow`는 화면 전환만. 챕터 1 고정 4곳 정리: 선택 화면 제목 `StageSelectView._chapterName`("챕터 1") · 빌더 챕터 제목("포도밭 오후") · 그림 에셋 경로(`ChapterArtBuilder` · 빌더) · `StageOrder` (2026-10-05 코드 총점검 5)
- [ ] [QA] (사용자) 다음 WebGL 빌드에서 코드 점검 수정 확인 — PC: 옵션 · 기록 패널이 열린 채 숫자 · 방향키 · Ctrl+Z가 보드에 안 먹힘 / 휴대폰: 끄는 도중 다른 손가락으로 되돌리기 · 처음부터 → 미리보기가 사라지고 떼도 안 그어짐 (Phase 7.7 수정 2건 — 키보드 · 실제 터치는 에디터에서 확인 못 함) — 2026-10-06 배포본(`dcd2527`)에 들어 있음

### 소재 맵 시험 — 기획 검증 (2026-10-06 앞당겨 함)

> 스테이지와 그림을 어떻게 이을지(그림 모양 맵 vs 자유 맵) 논의하고, 소재가 떠오르는 자유 맵을 직접 플레이해 보려고 만든 시험 목록. 근거 · 시험 결과는 DevelopLog 2026-10-06
- [x] [Doc] 방향 논의 · 생성기 시험 — 그림 모양 맵은 그림이 커질수록 수가 면적을 따라 늘고 디테일 · 넓은 면에서 풀이가 거의 없음(포도만 한 작은 그림은 됨) → 자유 맵 + 소재 단서(테두리 · 색 · 무늬), 소재 색을 섞인 색으로 · 기본색 소재는 색 하나 더 · 묶기는 자연스러운 짝에만 · 그림 맵은 챕터마다 1~2개(사용자: 직접 플레이해 보고 판단) — `Builds/PictureStageTest.png` · `SubjectStageTest.png` · `MixedSubjectTest.png`
- [x] [Code] 시험 목록(주소 `?lab`) — `GameFlow.UseList`(여는 목록 전환: 챕터 1 / 시험) · 시험 목록은 그림 화면 없이 다음으로, `StageSelectView.Show`(목록 이름 · 모두 열림 · 챕터 제목 줄 숨김, 목록이 짧아지면 남는 버튼 숨김) → 에디터 플레이 확인(아래 QA)
- [x] [Editor] 소재 맵 8개 · 목록 · 배선 — `Assets/Data/Stages/Lab/`(바구니 · 길 · 잎 · 나비(파스텔) · 해 · 하늘과 언덕 · 벌 · 포도(그림 맵), 5~7수) · `Assets/Data/LabCatalog.asset`(`StageOrder.SetLabOrder`) · 빌더(`GameFlow._labCatalog` · `StageSelectView._subtitle`) · 폰트 226자 1장 → `QaScene` 193항목(시험 8개 모두 C# 솔버로 풀리고 `minMoves` 일치 · 이름 글자 · 파일 이름이 챕터 목록과 안 겹침)
- [x] [QA] 에디터 플레이 — 시험 목록 "시험 · 0 / 8"(모두 열림 · 그림 · 챕터 제목 줄 숨김) → 1~8번 솔버 풀이를 실제 끌기로 모두 "완벽!"(5 · 5 · 6 · 6 · 6 · 6 · 7 · 7수), "다음"은 그림 화면 없이 다음 스테이지 · 마지막은 [목록] → 8 / 8. `?lab` 없이 시작하면 챕터 1 그대로(타이틀 → "챕터 1 · 0 / 11" · 잠김 · 처음 클리어 뒤 그림 화면 "색칠 1 / 11"), EditMode 108/108, 콘솔 에러 0, 저장 키 삭제
- [x] [QA] WebGL 빌드 · 배포 — 성공 · 에러 0 · 경고 7(이전과 같음), 압축 후 9,150,287바이트(Phase 7.6 대비 +21,676), `preloadedAssets` 되돌림, gh-pages `dcd2527`(index · 빌드 파일 4개 HTTP 200 · 크기 같음). Claude in Chrome: `https://jhseawater.github.io/ColoringBoot/?lab` → "시험 · 0 / 8" · 4번 나비(파스텔) 바로 열림 · 새로고침 뒤 콘솔 오류 0
- [x] [QA] (사용자) 휴대폰 · PC WebGL에서 `?lab`으로 소재 맵 체감 — 소재가 떠오르는지 · 섞기가 충분한지 · 그림 맵(포도)과 자유 맵의 차이 → 사용자 보고(2026-10-06): 소재 연상 · 섞기 적절, 그림 맵 · 자유 맵 각각 맛이 있음, 나비 · 하늘과 언덕 · 벌(난이도 · 풀이가 특히 좋음) · 포도는 좋고 바구니 · 잎은 단색 채우기라 풀이가 겹치고 너무 쉬움(챕터 앞쪽 입문용 또는 무늬 추가) — 실측: 바구니는 막힐 수 있는 칸 0, 잎은 2칸뿐(DevelopLog 2026-10-06)
- [x] [Doc] (사용자 승인) 체감 결과로 방향 확정 → GDD §5(스테이지 ↔ 소재 연결 방식) · Phase 8 콘텐츠 항목(소재 색 기준 · 그림 맵 수 · 생성기 조건 추가) → 사용자 승인(2026-10-06): GDD §5 "정한 사항 (2026-10-06)" 추가, 아래 콘텐츠 항목 2개 추가. 무늬를 넣은 바구니 · 잎 비교는 Phase 8로 미룸(사용자 결정)

### 콘텐츠

- [ ] [Doc] (사용자) 채택할 기믹 · 이동 횟수 제한 결정(GDD §7 · §9, 소규모 플레이테스트 결과) → 기믹마다 규칙 · 솔버 · 레벨 에디터 · 테스트 항목 — 기믹을 정하면 스테이지 데이터 형식(지금 `[q, r, 시작, 목표]` 고정) · 파서 · 작성기 · 레벨 에디터 · 칸 그림 · 테스트를 한 번에 바꾼다. 미리 만들지 않는다 (2026-10-05 코드 총점검 6)
- [ ] [Code] 레벨 에디터 생성기 조건 — 직접 그린 맵 모양 · 시작 색 · 목표 색 조건 · 섞인 색 비율 · 막힐 수 있는 칸 비율(단색 채우기 거르기), 그림 맵용 "목표 그림 → 시작 칸 후보 찾기"(GDD §5 2026-10-06 — 시험 스크립트가 원형, DevelopLog 같은 날짜)
- [ ] [Editor] 무한 모드 퍼즐 묶음 — 생성기 조건으로 수천 개 생성 · 난이도 3단계(쉬움 · 보통 · 어려움)로 나누기 · 용량 보고 (생성기 조건 뒤, GDD §5 2026-10-07)
- [ ] [Code] 무한 모드 화면 · 흐름 — 난이도 선택 · 연속 클리어 수 · 저장(그림 보상 없음)
- [ ] [QA] 무한 모드 — 빌드 용량 · 에디터 · WebGL 플레이
- [ ] [Editor] 챕터 1 스테이지 ↔ 소재 다시 짝짓기 — 소재마다 맵(테두리 · 색 · 무늬), 그림 맵 1~2개, 단색 채우기는 앞쪽 입문용만(바구니 · 잎은 무늬로 대비 추가 검토), 스테이지 순서 또는 그림 단계 순서 조정 · 이름을 소재로
- [ ] [Asset] 챕터 2 이후 그림(`ArtSpec.md`)
- [ ] [Editor] 챕터 2 이후 스테이지 제작 · 등록
- [ ] [QA] 챕터 그림 5장의 빌드 용량 · 첫 로딩 · 휴대폰 메모리 — 모두 첫 로딩에 넣을지, 챕터 2부터는 나중에 받을지 실측으로 정한다(실측: 시험 그림 챕터 1장 = 빌드 data 약 +686 KB(임시 도안 +323 KB → 시험 그림 +363 KB 더, Phase 7.2) → 5장이면 +3.4 MB 안팎). 그림 전체 예산도 이때 정한다(`ArtSpec.md` §8의 옛 목표 1.5 MB는 챕터 1만 있을 때 값 — 2026-10-04 점검). WebGL 텍스처 압축 형식은 정한 기록이 없다(기본값) — 휴대폰이 그 형식을 지원하지 않으면 압축을 풀어 메모리를 더 쓰므로 함께 확인. 가능하면 "전부 빌드에 포함"(가장 단순)을 유지한다 — 나중에 받기로 하면 Addressables 같은 큰 구조가 들어온다 (2026-10-05 코드 총점검 7)
- [ ] [QA] 처음부터 끝까지 연속 플레이(에디터 · WebGL)

---

## Phase 9 — 🚀[출시] 출시 준비

> **선행**: Phase 8 완료
> **완료 조건**: 앱인토스(첫 출시 플랫폼, 2026-10-03 결정) 심사 기준을 통과할 수 있는 빌드와 제출 자료가 준비되어 있다.

- [ ] [Doc] 🚀[출시] (사용자) 행정 절차 — 사업자 등록 · 게임제작업 등록 · 등급분류(GDD §11)
- [ ] [Code] 🚀[출시] 앱인토스 연동 — 네이티브 저장소 · 광고(힌트 보상형 광고 포함 — 2026-10-07) · 안전영역 · 종료 확인 모달 (햅틱은 필요하면 검토 — 7.4에서 웹 진동은 빼기로 함, 2026-10-03)
- [ ] [Doc] 🚀[출시] 스토어 자료(설명 · 스크린샷 · 아이콘) · 개인정보 처리방침
- [ ] [Doc] 🚀[출시] (사용자) 회사(개발자) 이름 — 지금 Unity 기본값 `DefaultCompany`(Phase 7.6에서 미룸, 2026-10-04 점검). 앱인토스 · 스토어 표기와 맞춘다
- [ ] [Asset] 🚀[출시] (사용자) 로고 그림 — 타이틀 · 로딩 화면의 글자 제목 자리(Phase 7.3에서 미룸, 2026-10-04 점검) · 스토어 아이콘과 함께
- [ ] [Doc] 🚀[출시] 루트 `README.md`(소개 · 플레이 링크 · 폴더 지도 · 문서 지도) · `CREDITS.md`(Pretendard OFL · 사운드 출처 정리) — 공개 저장소 · 스토어 자료용(2026-10-04 구조 정리 C)
- [ ] [Code] 🚀[출시] QA용 주소 기능(`?stage=` · `?stats` · `?reset` · `?palette=` · `?lab`)을 테스트 빌드에서만 켜지게 — 지금은 릴리스에도 남아, `?reset`이 붙은 링크 하나로 그 사람의 진행이 지워지고 `?stage=`로 해금을 건너뛴다. 플레이테스트에는 필요하므로 빌드 정의 기호 등으로 나눈다 (2026-10-05 코드 총점검 1)
- [ ] [Code] 🚀[출시] 저장소 교체 준비 — `IKeyValueStore`는 값을 바로 돌려받는 동기 방식이다. 앱인토스 저장소가 비동기면(확인 필요) 시작할 때 한 번 읽어 메모리에 두는 방식으로 감싼다. 플레이 기록은 지금 붓질마다 저장(`PlayerPrefs.Save`) → 화면을 떠날 때 · 백그라운드로 갈 때만(클리어 기록은 즉시). 출시 뒤 저장 형식(머리줄 `progress 1` · `stats 1`)을 바꿀 때는 옛 형식 읽기를 남긴다 — 지금 코드는 모르는 형식이면 빈 기록으로 시작한다 (2026-10-05 코드 총점검 2 · 3)
- [ ] [QA] 🚀[출시] 저사양 휴대폰 성능 · 첫 로딩
- [ ] [QA] 🚀[출시] 앱인토스 심사 체크리스트 — 10초 이내 로딩 · 풀스크린과 안전영역 · 사운드 켜고 끄기와 백그라운드 정지 · 진행 기록 유지 · 종료 확인 모달

---

## Phase 10 — 공개 테스트 · 발표

> **선행**: Phase 9 완료(또는 발표 일정이 먼저 오면 그때까지의 빌드)
> **완료 조건**: 동아리원 플레이테스트 결과가 정리되어 있고, 발표 자료가 준비되어 있다.

- [ ] [QA] (사용자) 동아리원 플레이테스트 — GDD §9 관찰 항목(요청 없이 다음 스테이지로 가는가 · 힌트 없이 5수를 푸는가 · 추론인가 찍기인가 · 미리보기 없이 예측하는가) (Phase 5에서 이동, 2026-10-03)
- [ ] [Doc] 플레이테스트 결과 정리 · 후속 조치 제안(Phase 5에서 이동, 2026-10-03)
- [ ] [Doc] 발표 자료(`anthropic-skills:pptx` 또는 슬라이드 페이지) — 발표 일정이 정해지면

---

## 결정 대기 (GDD §14 미정 사항 ↔ Phase)

| 미정 사항 | 필요한 시점 | 현재 |
|---|---|---|
| 줄 중간 빈자리 처리 | Phase 1 | 확정(2026-10-04): 건너감 — 프로토타입 규칙 그대로(GDD §2.2). 6번 보랏빛 길 · 9번 검은 날개가 빈자리를 건너는 획을 쓴다(검은 날개는 멈추는 규칙이면 최소 8수) |
| 이동 횟수 제한 여부 | Phase 8(Phase 7 끝 소규모 플레이테스트 후) | 미정 |
| 챕터 · 스테이지 수, 그림 배치 방식 | Phase 4 착수 전(스테이지 제작 전) | 확정(2026-09-30, GDD §5): 챕터 1 = 10개 안팎 · 소재 단위 색칠(스테이지 모양과 무관). 출시 범위 = 챕터 5개(2026-10-03) |
| 최소 수 달성 보상(별점 등) (GDD §5) | Phase 4 착수 전(저장 형태에 영향) | 확정(2026-09-30): "완벽" 표시. 최고 기록을 저장하므로 방식은 나중에 바꿀 수 있음. 힌트 사용과 무관(2026-10-07). "되돌리기 없이 최소 수"로 바꾸는 안은 검토 중 |
| 도안 · 색칠 단계 준비 (GDD §5) | 챕터 1: Phase 5 · 챕터 2~5: Phase 8 | 챕터 1 = 사용자 시험 그림(Phase 7.2 — 나중에 바꿀 수 있음, 임시 도안은 `ArtSource/_archive/Chapter1-temp`에 보관). 챕터 2~5는 미정 — 사용자가 직접 또는 아트 담당 |
| 팔레트 목록과 각 팔레트의 색 (GDD §2.3 · §14) | Phase 4 스테이지 제작 전 | 목록 = 기본 + 파스텔 2종(2026-09-30). 파스텔 색값 확정(2026-10-01 휴대폰 확인, Phase 4.4). 그 밖의 팔레트는 미정 |
| 동아리 발표 일정 (GDD §1 · §13) | 정해지면 | 정해진 일정 없음 — 발표 자료는 Phase 10. 일정이 먼저 오면 그때까지의 빌드로 |
| 채택할 기믹 · 기본색 외 색(흰색 · 지우개 등) | 후보는 Phase 6, 확정은 Phase 8 | 후보 = 벽 · 코팅 칸 · 물 칸(2026-10-03). 출시에 3~4개 |
| 정식 제목 · 세계관 · 아트 스타일 | Phase 6 | 확정(2026-10-03): "컬러링붓" · 그림책 풍경 · 굵은 선 플랫 카툰. 영어 이름은 미정(웹 포털로 넓힐 때) |
| 첫 출시 플랫폼 · 언어 (GDD §11) | Phase 6 | 확정(2026-10-03): 앱인토스 · 한국어(GDD §11 그대로) |
| 출시 범위(챕터 수) | Phase 6 | 확정(2026-10-03): 챕터 5개 · 스테이지 55개 안팎 · 기믹 3~4개 |
| 개발 형태(1인/팀) · 전체 개발 기간 | 수시 | 미정 |
| 등급분류 방식 · 수익 모델 | 🚀[출시] | 미정 — 수익 모델 후보: 힌트 보상형 광고(2026-10-07) |
