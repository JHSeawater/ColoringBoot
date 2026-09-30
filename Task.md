# 프로젝트 작업 목록 (Task List)

> 기획 근거는 `GDD.md`(무엇/왜), 작업 규칙 · 아키텍처 · 기술 스펙은 `CLAUDE.md`(TDD 없음 — CLAUDE.md §0), 작업 기록은 `DevelopLog.md`.
> **현재 위치: Phase 4 (스테이지 10개 내외 + 플레이테스트)** — 챕터 구조 확정(2026-09-30: 소재 단위 색칠 · 차례 해금 · "완벽" 표시, GDD §5). Phase 3 완료(2026-09-29: BFS 솔버 · 순서 민감도 · 생성기가 프로토타입과 일치, 레벨 에디터 · `StageCatalog`, WebGL 9.03 MB). Phase 2 완료(2026-09-29) · Phase 1 완료(2026-09-28).
> **이번 학기 목표 (GDD §13)**: 임시 아트로 챕터 1을 끝까지 플레이할 수 있는 WebGL 빌드 + 동아리원 플레이테스트 = **Phase 0~5**.
> 최초 작성: 2026-09-24 (Labyrinth Task.md 체계 이식) · 개정: 2026-09-27 (기획 대비 구성 검토 반영 — DevelopLog) · 2026-09-28 (색 팔레트 · 문서 정합성 점검 — DevelopLog) · 2026-09-29 (Phase 0~3 점검 반영 — DevelopLog) · 2026-09-30 (챕터 구조 확정 · Phase 4 · 5 재구성 — DevelopLog)

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

### 트랙 — 이번 학기 코어 vs 출시 전용

* 표시 없는 Phase/항목 = **코어**: 이번 학기 목표(챕터 1 WebGL 빌드 + 플레이테스트)에 필요한 것. Phase 0~5.
* **`🚀[출시]`** = 출시 전용(앱인토스 행정 절차 · SDK · 광고 등). GDD §11에 따라 게임이 완성 단계에 가까워진 뒤 진행하며, 개발과 병행하지 않는다.

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

## Phase 4 — 스테이지 10개 내외 + 플레이테스트 (GDD §13 단계 4)

> **선행**: Phase 2 · Phase 3 완료, 챕터 구조 확정(GDD §5 — 2026-09-30 확정: 스테이지는 그림 모양에 맞추지 않고 소재로만 이어진다 → 스테이지 제작에 그림 제약 없음)
> **완료 조건**: 스테이지 10개 내외를 순서대로 이어서 플레이할 수 있고(차례 해금 · 진행 저장 포함), 동아리원 플레이테스트 결과가 정리되어 있다.

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

- [ ] [Code] 스테이지 선택 화면 · 클리어 후 "다음 스테이지" · 차례 해금(클리어한 스테이지는 다시 하기) · 최고 기록과 "완벽" 표시 — 씬은 `Board.unity` 하나에 패널 전환(씬 다시 불러오기 없음)
- [ ] [Editor] 스테이지 선택 화면 배선
- [ ] [Code] 옵션 화면 — 접근성 기호 · 소리 켜고 끄기를 보드 아래 버튼에서 옮김(Phase 2에선 기능만 확인) — 사용자 요청 2026-09-29 · 버튼 배치: 위 ← 목록 · ⚙ 옵션 / 아래 되돌리기 · 처음부터 — 사용자 결정 2026-09-30
- [ ] [Editor] 옵션 화면 배선 · 위아래 버튼 정리 · `QaScene` · `/qa-scene` 갱신

### 4.4 팔레트

- [ ] [Asset] 파스텔 팔레트(GDD §2.3 예시 · 제작 규칙) — 색값 초안 제안 → 빈칸 포함 모든 색이 휴대폰에서 구분되는지 사용자 확인. 목록은 기본 + 파스텔 2종 — 사용자 결정 2026-09-30
- [ ] [Code] 스테이지 팔레트 적용 — 스테이지 `palette` 이름으로 팔레트 에셋을 골라 게임 · 레벨 에디터에 표시(이름이 없거나 모르는 이름이면 기본 팔레트), 레벨 에디터는 이름 입력 대신 있는 팔레트에서 고르기(GDD §8), 접근성 기호 글자색은 칸 색의 밝기로 정하기(지금은 노랑 칸만 어두운 글자) — 점검 F2
- [ ] [Editor] 팔레트 목록 에셋 · 보드 씬 · 레벨 에디터 씬 배선 — 점검 F2

### 4.5 스테이지

- [ ] [Doc] 스테이지 구성안 — 이식 9개 + 새로 1~3개(5수가 이어지는 구간 완화 · 파스텔 팔레트 사용), 순서 · 난이도 곡선(최소 수 · 순서 민감도), 첫 스테이지 규칙 안내 없음(GDD §9 설명 최소화) → 사용자 확정
- [ ] [Editor] 스테이지 제작 · 등록 · 목록 순서 — 회귀 테스트 기준 파일 분리(4.1)를 먼저 한다(점검 F6). 새 문구 · 이름이 생기면 폰트 재생성

### 4.6 검증 · 플레이테스트

- [ ] [QA] 에디터 플레이로 1번부터 끝까지 · 해금 · 저장 복원
- [x] [QA] (사용자) 휴대폰 WebGL에서 끌기 도중 끊기면(알림 · 시스템 제스처 · 앱 전환) 획이 그어지지 않고 미리보기가 사라지는지, 그 뒤 보드 입력이 정상인지 확인 — 점검 F4 → 사용자 보고(2026-09-30): gh-pages `5d274d4` 휴대폰에서 확인 완료, 문제 없음
- [ ] [QA] (사용자) WebGL(휴대폰 · PC)에서 처음부터 끝까지 연속 플레이 · 새로고침 후 진행 유지 · 빌드 용량 · 첫 로딩
- [ ] [Doc] 플레이테스트 관찰지 — GDD §9 관찰 항목 + `?stats` 기록 읽는 법
- [ ] [QA] (사용자) 동아리원 플레이테스트 — GDD §9 관찰 항목(요청 없이 다음 스테이지로 가는가 · 힌트 없이 5수를 푸는가 · 추론인가 찍기인가 · 미리보기 없이 예측하는가)
- [ ] [Doc] 플레이테스트 결과 정리 · 후속 조치 제안(이동 횟수 제한 · 기믹 등, GDD §9)

---

## Phase 5 — 챕터 그림 완성 구조 (GDD §13 단계 5)

> **선행**: Phase 4 완료. 도안 · 색칠 단계 전달 규격(첫 항목)은 Phase 4와 함께 진행해도 된다(아트 준비에 시간이 걸림)
> **완료 조건**: 챕터 1의 스테이지를 클리어할 때마다 그 소재의 색칠 단계가 도안에 한꺼번에 칠해지고, 마지막 스테이지를 클리어하면 그림이 완성되는 흐름을 WebGL 빌드에서 끝까지 플레이할 수 있다(이번 학기 목표 달성).

- [ ] [Doc] 도안 · 색칠 단계 전달 규격(아트 담당용) — 선화 1장 + 색칠 단계 N장(단계마다 그 소재의 모든 영역, 레이어별 PSD 또는 같은 캔버스의 투명 PNG), 칠하는 순서 = 스테이지 순서, 캔버스 크기 · 용량 기준(첫 로딩 10초). 여백 자르기 · 한 장 묶기는 빌더가 한다(GDD §5)
- [ ] [Asset] 임시 도안(단순 도형 · 색칠 단계 10개 안팎) — 아트가 오면 교체
- [ ] [Code] 챕터 데이터(스테이지 파일 ↔ 색칠 단계) · 진행 표시 · 칠하는 연출 · 완성 연출
- [ ] [Editor] 챕터 화면 배선
- [ ] [QA] 빌드 용량 · 첫 로딩 — 도안 레이어 추가 영향(Phase 4 대비 바이트)
- [ ] [QA] (사용자) WebGL로 챕터 1 처음부터 완성까지 플레이

---

## Phase 6 이후 — 기믹 · 아트/사운드 · 출시 준비 (GDD §13 단계 6)

> **선행**: Phase 5 완료
> **완료 조건**: 착수할 때 세부 Phase로 나누며 정한다.

- [ ] [Doc] (사용자) 채택할 기믹 결정(GDD §7 후보) → 기믹별 Phase 신설
- [ ] [Asset] 아트 · 사운드 에셋
- [ ] [Doc] 🚀[출시] (사용자) 앱인토스 행정 절차 — 사업자 등록 · 게임제작업 등록 · 등급분류(GDD §11)
- [ ] [Code] 🚀[출시] 앱인토스 연동 — 네이티브 저장소 · 광고 · 안전영역 · 종료 확인 모달
- [ ] [QA] 🚀[출시] 앱인토스 심사 체크리스트 — 10초 이내 로딩 · 풀스크린과 안전영역 · 사운드 켜고 끄기와 백그라운드 정지 · 진행 기록 유지 · 종료 확인 모달

---

## 결정 대기 (GDD §14 미정 사항 ↔ Phase)

| 미정 사항 | 필요한 시점 | 현재 |
|---|---|---|
| 줄 중간 빈자리 처리 | Phase 1 | 프로토타입 규칙(건너감)으로 구현. 바뀌면 CLAUDE.md §3과 테스트를 함께 갱신 |
| 이동 횟수 제한 여부 | Phase 4 플레이테스트 후 | 미정 |
| 챕터 · 스테이지 수, 그림 배치 방식 | Phase 4 착수 전(스테이지 제작 전) | 확정(2026-09-30, GDD §5): 챕터 1 = 10개 안팎 · 소재 단위 색칠(스테이지 모양과 무관). 챕터 2 이후 수는 미정 |
| 최소 수 달성 보상(별점 등) (GDD §5) | Phase 4 착수 전(저장 형태에 영향) | 확정(2026-09-30): "완벽" 표시. 최고 기록을 저장하므로 방식은 나중에 바꿀 수 있음 |
| 도안 · 색칠 단계 준비 (GDD §5) | Phase 5(규격은 Phase 4와 병행) | 사용자가 아트 담당을 구하거나 직접 준비. 오기 전엔 임시 도안 |
| 팔레트 목록과 각 팔레트의 색 (GDD §2.3 · §14) | Phase 4 스테이지 제작 전 | 목록 = 기본 + 파스텔 2종(2026-09-30). 파스텔 색값은 Phase 4.4에서 제안 → 휴대폰 확인 |
| 동아리 발표 일정 (GDD §1 · §13) | 정해지면 | 정해진 일정 없음 — 정해지면 해당 Phase에 `[Doc]` 발표 자료 항목 추가 |
| 채택할 기믹 · 기본색 외 색(흰색 · 지우개 등) | Phase 6 | 미정 |
| 정식 제목 · 세계관 · 아트 스타일 | Phase 6 | 미정 |
| 개발 형태(1인/팀) · 전체 개발 기간 | 수시 | 미정 |
| 등급분류 방식 · 수익 모델 | 🚀[출시] | 미정 |
