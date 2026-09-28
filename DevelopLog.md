# Development Log (개발 일지)

이 문서는 프로젝트의 개발 타임라인, 주요 변경 사항, 마주친 버그와 해결 방법을 기록한다.

## [작성 규칙]
1. 나(AI)는 Task(예: Phase 1)를 완료하거나 중요한 버그를 수정했을 때, 이 문서의 **최상단(이 규칙 바로 아래)**에 새 로그를 추가한다.
2. 각 로그는 **날짜(YYYY-MM-DD), 제목, 작업 내용, 해결된 이슈**를 포함한다.

---

### 📅 [2026-09-29] Phase 3 (3) — 스테이지 목록 · 레벨 에디터

* **StageCatalog** (Game SO): JSON TextAsset 순서 목록 · 파일 이름으로 찾기. `PuzzleController`는 목록 첫 스테이지 · `?stage=`를 목록에서 연다(`_stageCode` · `_queryStages` 대체). `BoardSceneBuilder.BuildCatalog` — 프로토타입 9개 중 빠진 것만 채움(다시 돌려도 에디터 저장분 유지, 두 번째 실행 "추가 0" 확인).
* **Core `StageWriter`**: Stage → 스테이지 코드(JSON 한 줄, 필드 순서 name · cells · palette · minMoves, 따옴표 · 역슬래시 · 제어 문자 이스케이프). `StageWriterTests` 10/10 — 스테이지 파일 9개를 읽고 다시 쓰면 글자 하나까지 같음 + 이스케이프 왕복.
* **레벨 에디터** (`ColoringBoot.LevelEditor` — `UNITY_EDITOR` 조건 어셈블리: 빌드 제외, 씬에 붙일 수 있음 / `LevelEditor.unity` — 빌드 목록 제외):
  * `PaintGridView` — 반지름 4 육각 격자(61자리, 프로토타입 에디터와 같음), 있는 칸 = 층 색 + 다른 층 색 마커, 없는 자리 = 흐리게, 누르고 끌며 칠하기(칸 없애기 · 빈칸 · 7색). 40칸 상한(GDD §6).
  * `LevelEditorController` — IMGUI 패널(에디터 도구라 게임 폰트 아틀라스와 무관): 시작/목표 층 · 칠할 색 · 획 긋기(게임 `BoardView`를 복제해 게임과 같은 드래그 · 미리보기, `BoardView._showJudgement` 끔) → 목표로 저장/취소 · 풀이 검사 · 랜덤 생성(모양 · 시작 색 칸 · 목표 수 · 색 · 순서 · 검정, 4초 · 시드 표시) · 이름 · 파일 이름(영문) · 팔레트 이름 · 목록에서 불러오기 · 저장(솔버로 `minMoves`, 풀 수 없으면 거부, 새 파일이면 목록 끝에 등록, 게임 폰트에 없는 글자 경고). 불러올 때 프로토타입처럼 가운데로 옮김.
* **에디터 플레이 QA** (`AgentScripts/LevelEditorQa.cs` — 격자 칠하기 · 획 드래그는 실제 포인터 이벤트, IMGUI 버튼 동작은 리플렉션): 포도 불러오기 → 모두 지우기 → 4칸 칠하기(빨강 · 빈칸 2 · 파랑) → 획 2개 기록 → 목표 저장 → 풀이 검사 "최소 2수, 탐색 4개, 2가지 중 1가지" → `EditorDemo.json` 저장(`minMoves` 2, 목록 10번째, "디 · 험" 폰트 경고) → 폰트 재생성(248자 · 1장) · 프리팹 · 두 씬 재구성. 랜덤 생성: 작은 육각형 19칸 · 최소 5수 · 10/120. 패널 글자가 밝은 바탕에 흰색이라 어둡게 고침. EditMode 93/93 · `QaScene` 62항목.
* **남은 일**: WebGL 빌드에서 `?stage=EditorDemo`로 에디터 스테이지를 최소 수로 클리어(사용자), `EditorDemo`를 목록에 둘지 결정.

---

### 📅 [2026-09-29] Phase 3 (2) — 랜덤 생성기

* **Core**: `SeededRandom`(프로토타입 `rng` = mulberry32를 uint 산술로 — JS의 `Math.imul` · `>>>`와 같은 결과), `BoardShape`(작은 · 큰 육각형 · 마름모 · 삼각형 · 불규칙 · 아무거나), `GeneratorOptions`(기본값 = 프로토타입), `Generator.Generate` — 모양 → 시작 색 칸 → 무작위 획(색이 바뀐 획만 셈) → 검정 · 칠한 비율 거르기 → 솔버(상한 12만) → 최소 수 · 순서 비율 거르기. 난수를 쓰는 순서까지 프로토타입과 같게(불규칙 모양의 집합은 삽입 순서 목록으로).
* **검증**: `GeneratorTests` 5/5 — Node로 프로토타입 generate를 시드 4건(12345 기본 · 2024 불규칙 · 777 아무 모양 + 섞인 색 + 검정 금지 · 99 삼각형)으로 돌린 결과와 모양 · 칸 · 최소 수 · 순서 민감도가 모두 같다. 같은 시드 두 번 = 같은 결과.

---

### 📅 [2026-09-29] Phase 3 (1) — 데이터 형식 결정 · BFS 솔버 · 순서 민감도 · 프로토타입 스테이지 9개

* **사용자 결정(추천안)**: 스테이지 = JSON TextAsset 유지(`minMoves`는 에디터 저장 때 솔버가 채움, 순서 민감도는 저장 안 함), 레벨 에디터 = 에디터 전용 씬(플레이 모드, 게임 보드 재사용), 게임이 여는 목록 = `StageCatalog` SO → CLAUDE.md §3 · §5, Task.md Phase 3 항목 보강.
* **Core**: `BoardMove` · `Board.Moves` — 2칸 이상 줄마다 정방향 → 반대 방향, 줄 순서는 축별 칸 등장 순서(프로토타입 `buildBoard`와 같음). `Solver.Solve` — 너비 우선, 색이 안 바뀌는 획 · 막힌 상태 제외, 상한 40만, 칸당 3비트 두 워드 상태 키(최대 42칸). `Solver.MeasureOrder` — 2~7수 풀이의 모든 순서 대입.
* **이식**: 프로토타입 BUILTIN 8개를 원문 그대로 JSON으로(`TwoColors` · `BrushChanges` · `Honeycomb` · `Crossing` · `Stain` · `MakeBlack` · `Hive` · `LastStroke`) + 각 `minMoves`.
* **검증**: `SolverTests` 6/6(포도 5수 · 8/120 · 탐색 67, 이미 목표 · 시작 막힘 · 풀이 없음 · 상한 · 순서 범위), `PrototypeStageRegressionTests` 9/9 — 9개 모두 최소 수 · 순서 민감도 · 탐색 상태 수가 프로토타입 엔진과 같다. DevelopLog 2026-09-28의 걱정(동률 풀이를 다르게 고르면 순서 민감도가 달라짐)은 탐색 순서를 맞춰 해소.

---

### 📅 [2026-09-29] Phase 2 종료 — 조작감 · 필수 UI를 휴대폰 · PC WebGL에서 확인

* **빌드 · 배포**: Succeeded · 에러 0 · 5.3분(경고 7 = Phase 1과 같은 무해한 것). 압축 후 8,992,413바이트 — Phase 1 대비 +244,584(data +257,742 한글 폰트 아틀라스 등 · wasm −13,158), Phase 0 대비 +0.87 MB. 빌드 도중 값(`preloadedAssets`)은 되돌림. gh-pages `e5eef23`, 배포 주소가 새 data 파일을 주는 것 확인.
* **`/qa-scene`**: 점검 항목이 늘어 읽기 전용 스크립트 `AgentScripts/QaScene.cs`로 모음 — 53항목 + 계층 · 빌드 씬 목록 전수 통과. 작성 중 발견: 이 TMP 버전은 `AtlasPopulationMode.Static`을 폐기 예정으로 표시(지금은 정상 동작) → CLAUDE.md §8에 기록.
* **사용자 실기 확인(2026-09-29)**: 휴대폰 · PC 모두 정상 — 드래그 · 미리보기 · 되돌리기(버튼 · 막힘 안내 · Ctrl+Z) · 방향키 · 숫자키 · 한글 · 썸네일 · 조합표 · 기호 · 소리 켬/끔 · 안전영역 · `?stage=hive` 37칸에서 칸 구분. 첫 로딩은 이전과 거의 같음(약 3초).
* **사용자 요청**: 기호 · 소리 켬/끔은 나중에 옵션 화면으로 옮긴다(Phase 2에선 기능만 확인) → Task.md Phase 4에 `[Code]` · `[Editor]` 항목 추가.
* **DoD 게이트(`/phase-close 2`)**: 미완 0 · `[QA]` 완료 3 · 근거 표기 · 태그 누락 0 · EditMode 63/63 · 완료 조건 관측. 정합성: GDD 변경 없음(§6 필수 UI · 조작은 GDD대로, §14 미정 사항 중 확정된 것 없음), CLAUDE.md §3 · §4 · §8 갱신.

* **해결된 이슈**:
  * Phase 2 ✅ — 다음은 Phase 3(솔버 · 레벨 에디터). 선행(Phase 1 코어 로직) 충족.

---

### 📅 [2026-09-28] Phase 2 (2) — 한글 폰트 · 필수 UI · 안전영역 · 사운드

* **커밋**: `0754b19`(2.1).
* **한글 폰트** (`AgentScripts/Phase2Font.cs`): Pretendard 1.3.9 SemiBold TTF(OFL, `Pretendard-LICENSE.txt` 동봉) → TMP 고정(Static) 아틀라스. 글자는 씬 구성 스크립트 · Game 코드의 문자열 리터럴(주석 · 로그 · 예외 메시지 줄 제외) + 스테이지 JSON 이름 + ASCII → 225자 · 1024² 1장. 로그 문구까지 모으자 243자 · 2장이 되어 제외 규칙을 넣었다. 원본 폰트 런타임 참조(`m_SourceFontFile`)를 비워 TTF 2.6 MB가 빌드에 딸려 가지 않게 하고, TMP 기본 폰트로 지정.
* **필수 UI**: 위쪽 = 스테이지 이름 · 수 카운터("3 / 5수", `minMoves` 없으면 현재만) · 목표 썸네일(BoardView 목표 모드 — `_showTarget` · `_fitMargin`), 색 조합표(`MixTableView`, 팔레트 색 칩 4줄), 막힘 안내 띠에 되돌리기 버튼, 칸별 접근성 기호(R · Y · B, 기호 버튼으로 전환 · 노랑 칸은 어두운 글자). 문구 모두 한글.
* **안전영역 · 사운드**: 모든 UI를 `Canvas/SafeArea`(`SafeAreaFitter` — `Screen.safeArea`가 바뀔 때만 앵커 조정) 아래로. `SoundController` — 소리 켬/끔(`AudioListener.volume`) · 포커스를 잃으면 `AudioListener.pause`.
* **스테이지**: 포도 JSON에 `minMoves` 5, 프로토타입 "큰 벌집"(37칸 · `minMoves` 6)을 `Hive.json`으로 옮기고 `?stage=hive`로 여는 코드(`PuzzleController.ChooseStage`).
* **에디터 플레이 QA**: 한글 · 배치 스크린샷, 드래그 5수 클리어("5 / 5수" · "완성!"), 막힘 안내의 되돌리기로 복구, 기호 켜기, 소리 끔 → 볼륨 0 · 켬 → 1. 콘솔 에러 0 · EditMode 63/63.
* **겪은 문제**: ① 조합표가 1,058 단위로 화면 밖 → 칩 40 · 간격 줄여 886. ② BoardView가 둘(보드 · 썸네일)이 되자 QA 스크립트가 썸네일을 잡음 → 경로로 찾게. ③ SafeArea 이동 편집이 `(canvasObject,` 꼴만 바꿔 보드 · 썸네일 · 조합표가 밖에 남음 → 바로잡음. ④ 셸 heredoc 안의 삼중 따옴표로 편집 스크립트가 실행 안 됨(파일 무변경) → 파이썬 파일로 실행.
* **확인 못 한 것 (빌드에서)**: 안전영역(에디터 = 화면 전체), 백그라운드 정지, `?stage=`, 키보드.

---

### 📅 [2026-09-28] Phase 2 (1) — 드래그 붓질 · 미리보기 · 되돌리기 · 키보드(코드)

* **사용자 결정**: Phase 2부터. 한글 폰트 Pretendard(쓰는 글자만 고정 아틀라스), 37칸 스테이지는 주소 `?stage=`로 열기, 화면 배치는 설계안대로 만든 뒤 보고 조정. Task.md 2.1 · 2.2 · 2.4에 항목 추가.
* **Core**: `Board.Trace`(쓸고 지나가는 칸 순서 + 칸을 지난 뒤 붓 색, 상태 불변, 호출 쪽 배열 재사용) · `PuzzleSession.Trace`. 테스트: 포도 1수 경로(빈 붓 2칸 → 초록), 모든 칸 × 6방향에서 Trace로 예측한 결과 = `Brush` 결과 → EditMode 63/63.
* **BoardView 재작성**: 탭(클릭) → 누름 · 끌기 · 뗌. 처음 누른 포인터만 추적, 반지름 45% 넘게 끌면 가장 가까운 방향(방향 벡터 내적)으로 미리보기, 떼면 붓질(1칸 줄이면 무시), 끌지 않고 떼면 탭 = 칸 선택 → 방향 버튼. 방향 버튼에 올려도(EventTrigger) 미리보기. 미리보기 = 선분 이미지(줄 칸 수 + 1, Build 때 만들어 재사용 — 매 프레임 할당 없음) + 칸 프리팹 Ghost 층(0.72배 · 불투명도 0.92).
* **PuzzleController**: 되돌리기 버튼, Ctrl/Cmd+Z · 방향키 칸 이동(프로토타입 점수식) · 숫자키 1 3 5 7 9 0 · Esc(Input System 직접 읽기, Game asmdef에 `Unity.InputSystem` 참조 추가). 되돌리기로 클리어가 풀리면 보드 잠금도 풀린다.
* **빌더 · QA 스크립트 이름 정리**: `Phase1Scene` → `BoardSceneBuilder`(Ghost 층 · Undo 버튼 추가), `Phase1Qa` → `BoardQa`(Drag · Undo 추가) — 이후 Phase에서도 계속 쓰므로.
* **에디터 플레이 QA**: 초록 칸 1시 드래그 미리보기 = 빈 붓 가는 선 → 초록 붓 선, 꼭대기 칸 초록 예고(스크린샷). 드래그 5수로 클리어 → 되돌리기로 잠금 해제 → 다시 클리어. 0.3배 짧은 드래그는 탭으로 처리. 보라 두 획 뒤 초록 줄 드래그 미리보기에 검정 두 칸이 보이고, 그대로 떼면 막힘 → 되돌리기로 복구. 콘솔 에러 0.
* **확인 못 한 것**: 키보드(Ctrl+Z · 방향키 · 숫자키) — 에디터가 비활성이면 Input System이 키 입력을 게임에 넘기지 않는다(흉내 낸 입력도 무시). 프로젝트 설정 에셋을 QA용으로 바꾸지 않고 PC 브라우저 확인으로 넘김 → CLAUDE.md §2 기록. 방향 버튼 위 미리보기도 마우스로 PC에서 확인.

* **해결된 이슈**:
  * 2.1 드래그 · 미리보기 · 배선 · Core 경로 조회 `[x]` (되돌리기 항목은 Ctrl+Z 확인 전이라 `[ ]`)

---

### 📅 [2026-09-28] Phase 1 종료 — 포도를 휴대폰 WebGL에서 끝까지 플레이

* **사용자 실기 확인**: 휴대폰 · PC 모두 첫 로딩 약 3초(Phase 0과 같음), 세로 화면 · 칸 선택 · 방향 버튼 · 클리어 · 막힘 표시 모두 문제 없음.
* **DoD 게이트(`/phase-close 1`)**: 미완 항목 0 · `[QA]` 완료 4 · 모든 `[x]`에 근거 표기 · 태그 누락 0 · EditMode 전체 61/61 통과(MCP가 끊겨 CLI `unity command run_tests`로 실행) · 완료 조건 ①(로직 테스트 · 포도 회귀) ②(휴대폰 WebGL에서 포도 처음부터 클리어) 모두 관측.
* **정합성 점검**: GDD 변경 필요 없음(이번 Phase의 규칙 결정 — 색이 안 바뀌는 붓질 · 처음부터 — 은 이미 §2.2 · §2.5에 반영). GDD §14 미정 사항 중 확정된 것 없음(줄 중간 빈자리는 여전히 프로토타입 규칙으로 구현). CLAUDE.md §4 현황에 Phase 1 빌드 용량을 더하고 기준선 합계 "7.76 MB"가 단위가 어긋난 값임(실제 8.12 MB)을 밝힘 → Task.md `[Doc]` 항목.

* **해결된 이슈**:
  * Phase 1 ✅ — 다음은 Phase 2(조작감). 선행 조건(Phase 1 완료) 충족. Phase 3(솔버 · 레벨 에디터)도 선행(코어 로직) 충족 — 순서 선택 가능.

---

### 📅 [2026-09-28] Phase 1.2 (2) — WebGL 빌드 · gh-pages 배포 · `/qa-scene`

* **커밋**: `8ecad8e`(1.2 코드 · 에셋 · 씬).
* **빌드**: Succeeded · 에러 0 · 4.2분. 경고 7 = Phase 0부터 있던 3건(Pipeline 런타임 설정 없음 · URP 디버그 셰이더 2개 제외) + TMP 셰이더의 폐기 예정 pragma 1건 + IL2CPP가 TMP의 큰 메서드를 따로 뺐다는 안내 3건 — 모두 무해.
* **용량 (압축 후, 바이트)**:

| | Phase 0 | 1.2 | 차이 |
|---|---|---|---|
| data | 2,271,844 | 2,802,101 | +530,257 (TMP 폰트 · 스프라이트 등) |
| wasm | 5,662,304 | 5,761,012 | +98,708 (uGUI · TMP · 게임 코드) |
| framework · loader | 184,716 | 184,716 | 0 |
| 합계 | 8,118,864 (7.74 MiB) | 8,747,829 (8.34 MiB) | **+628,965** |

  * 기준선 합계 "7.76 MB"는 단위가 어긋난 값이다 — wasm 5.66 · data 2.27은 10진 MB(바이트 그대로)인데, 바이트 합 8,118,864는 8.12 MB(7.74 MiB)다. 앞으로 크기는 바이트로 비교한다.
* **빌드 도중 값**: `preloadedAssets`에 입력 액션이 다시 들어감 → 커밋하지 않고 되돌림(DevelopLog 2026-09-25와 같은 현상).
* **`/qa-scene`** (`.claude/skills/qa-scene`, 읽기 전용): 빌드 뒤 실행 → 26항목 전수 통과(계층 · Canvas Scaler · 보드 영역 · 참조 누락 0 · 포도 JSON 10칸 · 팔레트 7색 · 스프라이트 · TMP Settings · 빌드 씬 목록).
* **배포**: `deploy-pages.sh` → gh-pages `f6236c9`, 배포 주소가 새 data 파일(2,802,101 바이트)을 제공하는 것 확인.
* **남은 일**: (사용자) 휴대폰에서 포도 클리어 · 막힘 표시 · 첫 로딩 시간 → `/phase-close 1` — 같은 날 완료(위 로그)

---

### 📅 [2026-09-28] Phase 1.2 (1) — 보드 씬(uGUI) · 에디터 플레이로 포도 클리어

* **사용자 결정**: 보드는 uGUI로 그린다(월드 스프라이트 대신). WebGL용 `Mobile_RPAsset`이 렌더 스케일 0.8 · MSAA 꺼짐이라 월드 공간은 흐려지고 메시 가장자리가 계단진다. 오버레이 UI는 원래 해상도로 그려지고, 버튼 위 터치가 보드로 새지 않으며, 화면 맞춤이 사각형 계산으로 끝난다. 색은 프로토타입 라이트 테마.
* **Core**: `Board.LineLength`(1칸짜리 줄 방향 버튼 숨김용) + `BoardTests.LineLength_CountsAllCellsOnTheLine` → EditMode 61/61.
* **Game** (`Assets/Scripts/Game`, `ColoringBoot.Game` — 참조 Core · UnityEngine.UI):
  * `ColorPalette`(SO, 값 1~7) · `CellView`(칸 프리팹 층: 테두리 · 채움 · 목표 마커 · 막힘 링 · 선택 링) · `BoardView`(배치 공식 y 반전 · 보드 영역에 자동 맞춤(여백 = 방향 버튼 자리) · 탭 → 가장 가까운 칸(반지름 95% 이내) · 방향 버튼 6개(1칸 줄 숨김) · 창 크기 변화 시 다시 배치) · `PuzzleController`(JSON 읽기 · 세션 · 클리어/막힘 안내 · 처음부터).
  * 클리어하면 보드 입력을 잠그고, 처음부터 버튼은 둔 수가 있을 때만 켠다. 색은 즉시 바뀜(애니메이션 없음).
* **에셋** (빌더 스크립트 — 다시 실행해도 같은 결과):
  * `Phase1Sprites.cs` → `Assets/Art/Sprites/` HexFill · HexRing · Circle · Arrow(256px 흰색, 4×4 슈퍼샘플링 안티앨리어싱, Sprite · 밉맵 없음 · 무압축).
  * `Phase1Assets.cs` → TMP Essential Resources 임포트(`Assets/TextMesh Pro` 4.0 MB, 그중 `Resources/LiberationSans SDF` 2.2 MB — 빌드 증가량은 빌드 QA에서) · `Assets/Data/Palettes/DefaultPalette.asset`(`#D8402F` … `#25262D`). 포도는 `Assets/Data/Stages/Grape.json`(GDD §3 원문).
  * `Phase1Scene.cs` → `Assets/Prefabs/Cell` · `DirectionButton`, 씬: `SampleScene` → `Board`로 이름 변경(GUID 유지 → 빌드 씬 목록 자동 갱신) · EventSystem(InputSystemUIInputModule) · Canvas(Overlay, Scale With Screen Size 1080×1920, **Expand**) · BoardArea(투명 Image + BoardView) · Clear/Stuck 안내 · Restart 버튼 · Puzzle(PuzzleController). 참조 누락 0(`get_serialized_fields`).
* **에디터 플레이 QA** (`Phase1Qa.cs` — 보드 영역 클릭 이벤트(화면 좌표) → 칸 판정 → 방향 버튼 → 컨트롤러 → 세션, 실제 입력 경로):
  * 첫 화면 모양 · 목표 마커가 프로토타입 스크린샷의 포도와 같다(y축 부호 정상). 초록 칸 선택 → 1 · 5 · 7 · 11시 버튼만(3 · 9시는 1칸 줄), 위치도 방향과 일치.
  * GDD 5수 → 수 5 · 성공 · Clear 안내. 클리어 뒤 칸 탭 → 버튼 안 나옴(잠김). 처음부터 → 시작 상태 · 수 0 · 버튼 꺼짐.
  * 보라 두 획 뒤 초록 획 → 꼭대기 두 칸 검정 · 막힘 링 · Stuck 안내.
  * 플레이 중 콘솔 에러 0.
* **겪은 문제 → CLAUDE.md §2 기록**:
  * Write로 쓴 `Grape.json`이 임포트 전이라 씬 참조가 조용히 null → `AgentScripts/Refresh.cs`로 임포트 후 재실행, 빌더는 null 참조면 멈추게 고침.
  * TMP 패키지 임포트가 메인 스레드를 1분 넘게 잡아 MCP 시간 초과(에디터는 스스로 회복, 임포트는 완료). 폐기 예정 `AssetDatabase.ImportPackage` → `AssetPackage.Package.Import`.
  * 첫 캡처가 하늘색 사각형 → 비동기 셰이더 컴파일 대체 셰이더. 캡처가 1280×720으로 늘어남 → 세로 크기 지정.
  * 에디터가 비활성이라 Play Mode 프레임이 2에서 멈춤(`Application.runInBackground` 꺼짐) → 플레이 세션에서만 켜서 해결.
  * TMP 폰트 `.meta` 구버전 경고 → 메타데이터 다시 저장.

* **해결된 이슈**:
  * 1.2의 코드 · 에셋 · 씬 배선 · 에디터 플레이 QA — Task.md 10항목 `[x]`
* **남은 일**: 커밋(승인 대기) → WebGL 빌드 · 배포 · 용량 비교 → (사용자) 휴대폰 확인 → `/qa-scene`

---

### 📅 [2026-09-28] Phase 1.1 — 코어 로직(순수 C#) · 포도 회귀

* **착수 전 점검**: 에디터 ready · `projectPath` 일치 · 컴파일 정상 · 콘솔 에러 0 · 씬 루트 Main Camera만 · EditMode 1/1 · `main` 깨끗(`a356520`).
* **사용자 결정**:
  * 스테이지 코드(JSON)는 Core 전용 파서로 읽는다. `JsonUtility`는 UnityEngine이라 Core에서 쓸 수 없고 중첩 배열(`[[q,r,a,b],…]`)도 못 읽는다. Newtonsoft(`com.unity.pipeline`의 간접 의존 3.2.2)는 직접 의존 추가 · WebGL 용량 · Stripping 부담이 있어 쓰지 않는다.
  * 색이 하나도 바뀌지 않는 획은 무시한다(수 · 되돌리기 기록 없음). 프로토타입은 1수로 세고 1칸짜리 줄은 긋지 못하게 막는다.
  * 가정(이의 없음): 처음부터는 되돌릴 수 없다(프로토타입과 같음) · 파서는 `name` · `cells` 필수, 모르는 키 · 0~7 밖 색 · 좌표 중복은 `FormatException` · 시작부터 막힌 스테이지는 형식 오류가 아니라 보드 판정으로 알린다.
* **구현** (`Assets/Scripts/Core`, 네임스페이스 `ColoringBoot.Core`):

| 타입 | 역할 |
|---|---|
| `HexDirection` | 6방향 `Clock1`~`Clock11`(프로토타입 `DIRS` 순서 — 0~2가 축의 정방향) · `Delta` · `Axis` |
| `HexCoord` | 축 좌표 · `LineKey`(같은 줄 판정, 프로토타입 `lineKey`) · `Along`(방향 쪽 정렬 키) |
| `Stage` · `StageParser` | 스테이지 데이터(`name` · `cells` · `palette` · `minMoves`), `Stage.Parse` |
| `Board` | 줄 구성(같은 키끼리 묶어 `Along`으로 정렬 → 빈자리 건너감) · `Brush`(바뀐 칸이 있으면 true) · 막힘 `IsDead`/`IsDeadCell` · 성공 `IsSolved`. 색 상태는 인자로 받는다 — Phase 3 솔버가 그대로 쓴다 |
| `PuzzleSession` | 현재 색 · 획 단위 스냅샷 되돌리기 · 처음부터 · `MoveCount` |

* **검증**: 단계마다 `recompile` → 컴파일 확인 → `run_tests`(범위 좁힘). EditMode 전체 **60/60 통과**(HexCoord 12 · StageParser 19 · Board 21 · PuzzleSession 5 · 포도 회귀 2 · 기존 1). 포도는 GDD §3의 5수로 클리어되고 순서 120가지 중 8가지가 성공한다. Core에 `UnityEngine` 참조 없음 · 콘솔 에러/경고 0 · 씬 변경 없음.
* **프로토타입 엔진 기준값** — `Prototype.html`의 스크립트를 Node로 실행(세션 임시 스크립트, 저장소에 없음). Phase 3 솔버 회귀용:

| 스테이지 | 칸 | 최소 수 | 순서 민감도 |
|---|---|---|---|
| 포도 | 10 | 5 | 8/120 |
| 두 가지 색 | 15 | 3 | 1/6 |
| 붓 색이 바뀐다 | 16 | 3 | 1/6 |
| 벌집 | 19 | 4 | 2/24 |
| 엇갈림 | 19 | 5 | 8/120 |
| 얼룩 | 14 | 5 | 3/120 |
| 검정 만들기 | 19 | 5 | 2/120 |
| 큰 벌집 | 37 | 6 | 20/720 |
| 마지막 한 획 | 37 | 7 | 100/5040 |

* 기준값은 스크린샷 3장의 값(포도 · 얼룩 · 마지막 한 획)과 일치한다. GDD §3 풀이를 (칸, 방향) 그대로 두면 클리어 · 8/120.
* **빈자리 규칙은 실제 스테이지로 검증되지 않는다**: 줄 중간에 빈자리가 있는 스테이지는 얼룩(5줄)뿐이고, 얼룩의 최적 풀이는 그 줄을 쓰지 않는다("멈춤" 규칙으로 바꿔도 그 풀이는 클리어). 그래서 빈자리 테스트는 작은 보드로 했다(`Brush_CrossesGapsInLine`).
* **순서 민감도는 풀이마다 다르다**: 포도의 5수 풀이는 3가지이고 순서 민감도가 8/120 · 6/120 · 2/120이다. 8/120은 GDD §3 풀이(= 프로토타입 BFS가 고른 풀이)의 값 → Phase 3 솔버 회귀 기준을 정할 때 반영(Task.md 주석).
* **발견 → CLAUDE.md 반영**:
  * 테스트 중 씬 조회(§8): EditMode `run_tests`와 `get_scene_hierarchy`를 동시에 부르자 이름 없는 씬(Main Camera + Directional Light)이 잡혔다 — 테스트 러너의 임시 씬. 테스트 뒤 다시 조회하니 정상(루트 Main Camera만).
  * 유니코드 이스케이프 변환(§8 Windows 도구 환경 ④): Write 내용의 역슬래시 + u + 16진 4자리가 실제 글자로 저장됐다(테스트 JSON의 이스케이프가 "포도"로). `\\` · `\n` 등은 그대로. 테스트를 `%` 치환 방식으로 고치고, 컴파일된 테스트 DLL에 고친 문자열이 들어간 것을 확인했다.
  * `recompile`이 `completed`를 돌려준 직후 부른 `run_tests`가 연결 오류(도메인 리로드와 겹침). 이후 `editor_status`(ready)를 확인하고 실행해 문제없음(§2 루프 2번).
  * §3에 규칙 세부(색이 안 바뀌는 획 무시 · 재시작은 되돌릴 수 없음) · 파서 규칙 · 회귀 테스트 이름을 기록.

* **해결된 이슈**:
  * Phase 1.1 코어 로직 · 포도 회귀 테스트 — Task.md 1.1 7항목 `[x]`
  * 규칙 세부(색이 안 바뀌는 획 · 처음부터) 결정과 기록
* **GDD 반영** (사용자 승인): §2.2 붓질에 "색이 바뀌는 칸이 하나도 없는 붓질은 수로 세지 않는다.", §2.5에 "처음부터 다시 하기는 되돌릴 수 없다." — CLAUDE.md §3의 해당 서술이 GDD를 가리키게 함.
* **남은 일**: 1.2 최소 표현 계층 계획

---

### 📅 [2026-09-28] 기획 수정(색 팔레트 · GDD 정리) · 문서 3종 정합성 점검

* **GDD 정리** (`15dc45e` · `cb975c0`): §12 유사 게임과 차별점 절과 리스크 표의 유사 게임 행 삭제 → **§13~15를 §12~14로 당김**(리스크 §12 · 개발 계획 §13 · 미정 사항 §14). CLAUDE.md · Task.md · `/phase-close`의 참조를 새 번호로 갱신했다. 이 날짜 이전 로그 본문의 GDD 절 번호는 옛 번호다. 그 밖에 §2.5 막힘 판정식 괄호, §6 화면 방향(고정 → 세로 기준 + 레터박스), §3 포도 풀이 3번 명확화(2에서 보라가 된 두 칸 중 위쪽 칸 — 스테이지 코드로 풀이를 따라가 확인), §13 단계 1·2를 Task.md와 맞춤. 프로토타입 로컬 사본 `Prototype/`(HTML + 스크린샷 3장)을 저장소에 추가.
* **색 팔레트 도입** (`6641dbe`, 사용자 결정): 규칙은 3원색 비트 혼합(0~7) 그대로, 보이는 색만 스테이지마다 고르는 팔레트(값 1~7 → 표시 색, ScriptableObject)가 정한다. 팔레트 단위 = 스테이지. 7번 색은 팔레트마다 어두운 계열로 따로 정한다(기본 = 검정, 파스텔 예시 = 차콜). 필수 UI에 색 조합표 추가. 로직 · 솔버 · 포도 회귀는 영향 없음.
* **문서 정합성 점검** (CLAUDE.md · GDD.md · Task.md): 규칙 · 좌표 · Phase ↔ GDD §13 단계 · 결정 대기 표는 일치. 불일치 수정:
  * CLAUDE.md §6 필수 UI에 색 조합표 누락 → 추가.
  * 스테이지 데이터 선택 필드가 문서마다 달랐음(GDD · CLAUDE.md = `palette`만, Task.md = `minMoves`만) → 세 문서 모두 `palette` · `minMoves`. 로직은 `palette`를 이름으로만 들고 에셋 선택은 표현 계층.
  * 스테이지 크기(사용자 결정): "20~40칸" → "형식마다 다르고 최대 40칸 정도"(포도 10칸이 범위 밖이던 문제). GDD §6 · CLAUDE.md §3·§6 · Task.md 반영.
  * GDD §10 색 표현에 팔레트 분리, §12에 팔레트 리스크 행 추가(승인). Task.md Phase 3 솔버 회귀의 프로토타입 엔진 출처를 `Prototype/Prototype.html`로 명시.

* **해결된 이슈**:
  * 스테이지 데이터 필드 불일치 · 스테이지 크기 규정과 포도(10칸) 충돌 · 색 조합표 누락 · 절 번호 변경 미기록

---

### 📅 [2026-09-27] Task.md 구성 검토 — 기획(GDD) 대비

* **검토**: Phase 1~6이 GDD §14 단계 1~6과 하나씩 대응하고 학기 목표 · 출시 트랙 분리도 맞다. 다만 재작업이나 막힘을 부를 곳 11건을 찾았다.
* **사용자 결정**: 챕터 구조 확정을 Phase 4 착수 전으로 당김(스테이지가 챕터 그림의 일부를 담당하므로 — GDD §13 리스크) · 막힘 표시(최소)를 Phase 1에 넣음 · 발표 일정은 정해진 것 없음.
* **반영**:
  * 최소 수: 프로토타입 포맷에 최소 수가 없어 Phase 2 수 카운터가 Phase 3 형식 결정을 기다려야 했다 → Phase 1 파싱에 선택 필드 `minMoves`를 넣고, 솔버가 채움(Phase 3).
  * Phase 1: `ColoringBoot.Game` asmdef 항목(CLAUDE.md §3) · 보드 자동 맞춤(20~40칸 대비) · 막힘 표시 최소 · 클리어/막힘 글자는 영문·아이콘(한글 폰트는 Phase 2) · WebGL 빌드·배포·기준선 비교 `[QA]`(`preloadedAssets` 주의 포함).
  * Phase 2: 15개 항목을 2.1 조작 · 2.2 필수 UI · 2.3 플랫폼 · 2.4 검증으로 나누고 소절마다 배선 항목(DoD 규칙 3). 휴대폰 확인에 칸이 가장 많은 프로토타입 스테이지로 칸 구분 확인(GDD §6)을 추가.
  * Phase 3: 데이터 형식 결정을 맨 앞으로 · 스테이지 이식을 솔버 회귀 앞으로 · 스테이지 불러오기 `[Code]`/`[Editor]` 추가(완료 조건 "만든 스테이지를 게임에서 플레이"의 경로).
  * Phase 4: 챕터 구조 확정을 선행 겸 첫 항목으로(Phase 5에서 이동) · 광고 인터페이스 자리(GDD §6).
  * 결정 대기 표: 챕터 구조 시점 변경 · 최소 수 보상(GDD §5) · 발표 일정 행 추가.
* **검증**: 81개 항목 모두 태그 1개 · 모든 Phase에 선행 · 완료 조건 · `[QA]` 존재(스크립트 검사).

* **해결된 이슈**:
  * Phase 2 ↔ Phase 3 최소 수 데이터 의존 순환 · 챕터 구조 결정 시점에 따른 스테이지 재작업 위험 · 20~40칸 보드 실기 확인 부재 · Phase 3 완료 조건의 확인 경로 부재

---

### 📅 [2026-09-25] Phase 1 착수 전 환경 점검 · 남은 스크립팅 심볼 정리

* **점검(읽기 전용)**: MCP 연결(projectPath 일치) · 컴파일 정상 · 콘솔 창 에러 0 · EditMode 1/1 통과 · asmdef(`noEngineReferences: true`) · 활성 타깃 WebGL · WebGL Player 설정(Brotli · Fallback · 캐싱 · 스레드 끔 · Stripping High · 540×960) · 씬 루트 Main Camera만 · 패키지 직접 7개 · `main`/`gh-pages`가 원격과 일치 · 배포 URL 200 · 편집 가드 훅 파이프 테스트(`.unity` exit 2 / `.cs` exit 0) · 루트 `.csproj` 2개 모두 솔루션이 참조 — 모두 정상.
* **발견**: WebGL 스크립팅 심볼에 `APP_UI_EDITOR_ONLY`가 남아 있었다. 0.2 커밋(`58fde2f`)은 Standalone을 비우고 WebGL은 `SENTIS_ANALYTICS_ENABLED`만 지웠다(App UI 패키지가 제거되는 도중에 다시 넣은 것으로 보인다). 쓰는 코드가 없어서 동작에는 영향이 없었다.
* **조치**: `AgentScripts/Phase0WebGLSettings.cs`를 다시 실행(Preview → Apply, 멱등) → `defines(WebGL)=''`. 재컴파일 완료 · 에러 0 · EditMode 1/1 통과.
* **함께 바뀐 값 — 0.3 기록 정정**: `preloadedAssets`에서 `InputSystem_Actions`가 빠졌다. Input System은 빌드 전처리에서 이 에셋을 사전 로드 목록에 넣고 후처리에서 뺀다(`BuildProviderHelpers.cs`). 그러니 0.3에서 "정상 상태"로 커밋한 값은 **빌드 도중에 저장된 임시 값**이었고, 빈 목록이 평소 상태다. WebGL 빌드 뒤 이 항목이 다시 diff에 나타나면 커밋하지 않는다.

* **해결된 이슈**:
  * 제거한 패키지의 스크립팅 심볼 잔여(WebGL) — 삭제
  * 빌드 도중 값(`preloadedAssets`)이 커밋되어 있던 것 — 평소 상태로 복귀
* **남은 일**: 커밋(승인 대기) → Phase 1.1 계획

---

### 📅 [2026-09-24] Phase 0 종료 — WebGL 기준선 확보

* **기준선**: 압축 후 7.76 MB(wasm 5.66 MB · data 2.27 MB), 첫 로딩 약 3초(PC · 휴대폰, gh-pages 배포본, 사용자 측정). 앱인토스 심사 기준(10초)보다 넉넉하다. 이후 Phase마다 이 값과 비교한다.
* **DoD 게이트(`/phase-close 0`)**: 미완 항목 0 · `[QA]` 완료 5 · 모든 `[x]`에 근거 표기 · 태그 누락 0 · EditMode 테스트 전체 통과(1/1) · 완료 조건 ①~③ 모두 관측.
* **정합성 점검**: GDD 변경 필요 없음(Phase 0의 결정은 모두 기술 사항). CLAUDE.md §4 현황에 로딩 시간 반영.

* **해결된 이슈**:
  * Phase 0 ✅ — 다음은 Phase 1(코어 로직 + 포도 플레이), 선행 조건(asmdef 골격 · WebGL 빌드 경로) 충족

---

### 📅 [2026-09-24] Phase 0.3 — 첫 WebGL 빌드 · gh-pages 배포

#### 1. 빌드

| 항목 | 결과 |
|---|---|
| 결과 | Succeeded · 에러 0 · 경고 3 · 6.1분(22:36~22:42) |
| 용량(압축 후) | 7.76 MB — wasm 5.66 MB · data 2.27 MB · framework 66 KB · loader 119 KB |
| 경고 | Pipeline 런타임 설정 없음(정식 빌드에서 원격 제어가 꺼짐 — 의도한 동작) · URP 디버그 셰이더 2개 제외(미사용) |
| 빌드 후 자동 변경 | URP 셰이더 사전 필터링 값 · 볼륨 컴포넌트 새 필드 · GraphicsSettings 기본 필드 · 입력 액션 사전 로드 등록 — Unity/URP가 빌드하며 저장한 정상 상태라 커밋 |
| 빌드 산출물 | Burst가 프로젝트 루트에 `Data/Plugins/lib_burst_generated.*`를 남김 → 해당 파일만 `.gitignore`에 추가 |

#### 2. 대기 스크립트가 완료를 못 잡음 (30분 허비)

CLI `unity command build_status --json`은 결과를 `data.result`에 이스케이프된 JSON 문자열로 담는다. 원문을 `"status": "completed"`로 grep하던 대기 루프가 끝내 매치하지 못해 30분 시간 제한까지 돌았다(빌드는 6분 만에 완료). CLAUDE.md §2에 "파싱해서 판정" 규칙을 추가했다.

#### 3. gh-pages 배포 — 첫 시도의 결함과 정정

* **첫 push(`a08ab76`)**: `git worktree add --no-checkout` → `checkout --orphan` 뒤 인덱스를 비우는 명령이 에러 출력을 숨긴 채 실패해, **프로젝트 소스 71개가 함께 올라갔다.** 게다가 작업 트리에 딸려 온 main의 `.gitignore`(`/Build/`) 때문에 **빌드 핵심 파일(`Build/` 4개)이 빠졌다.** 저장소가 이미 공개라 새로 노출된 정보는 없다.
* **정정(`bde5385`)**: 강제 push 대신 소스 71개 삭제 + `Build/` 4개 추가 커밋. 원격 트리가 빌드 결과물 18개뿐인 것을 확인했고, Pages도 최신 배포를 제공하는 것을 확인했다(`/` · `Build/` 200, `GDD.md` 404).
* **재발 방지**: `AgentScripts/deploy-pages.sh` — 작업 트리를 비우고 빌드 결과물만 복사하며, 빌드 결과물 외 파일이 있으면 중단한다. 시험: 같은 빌드 → "변경 없음", 가짜 파일을 넣고 실행 → 중단(exit 1) 후 원상복구. `.gitattributes`에 `*.sh eol=lf` 추가(CRLF면 bash가 실행하지 못함).
* GitHub Pages는 `gh-pages` 브랜치 push로 자동 활성화됐다 — https://jhseawater.github.io/ColoringBoot/

#### 4. IDE 프로젝트 파일 찌꺼기 정리

사용자가 프로젝트 루트에 `Unity.AI.*` 파일이 대거 생긴 것을 발견했다. 확인해 보니 스크립트가 아니라 **IDE 프로젝트 파일(`.csproj`)**이었다. 루트의 `.csproj` 81개 중 솔루션(`ColoringBoot.slnx`)이 참조하는 것은 2개(`ColoringBoot.Core` · `ColoringBoot.Core.Tests`)뿐이었고, 나머지 79개는 제거한 패키지의 것(AI 42 · AppUI 9 · Visual Scripting 9 · Sentis 8 · Plastic/Collab 4 · Timeline 2 · 기타 3)과 템플릿 스크립트가 사라져 더는 쓰이지 않는 `Assembly-CSharp` 2개였다. 모두 패키지 제거 도중(22:20) 생성됐다. `.gitignore`의 `*.csproj` 규칙 때문에 `git status`에 보이지 않았고, 커밋된 적도 없다. 사용자 승인 후 79개를 삭제했다(CLAUDE.md §8에 기록).

* **해결된 이슈**:
  * 첫 WebGL 빌드 성공 · 용량 기준선 확보
  * gh-pages에 소스가 섞이고 빌드 파일이 빠진 배포 — 정정 + 배포 스크립트로 재발 방지
  * 제거한 패키지의 IDE 프로젝트 파일 79개 — 삭제
* **남은 일**: (사용자) PC · 휴대폰 브라우저 실행 확인과 첫 로딩 시간 측정 → 기준선 기록 → `/phase-close 0`

---

### 📅 [2026-09-24] Phase 0.2 — WebGL 전환 확인 · 프로젝트 정리 · asmdef 골격

#### 1. 빌드 타깃

`switch_build_target(WebGL)`을 실행하자 "Already on build target 'WebGL'"이 돌아왔다 — 이미 WebGL이었다. `Library/EditorUserBuildSettings.asset` 수정 시각이 19:40(모듈 설치 후 에디터 재시작 19:42 직전)이라 그때 전환된 것으로 보인다. git 추적 파일 변화는 없다. Task.md 0.2 첫 항목 `[x]`.

#### 2. 기록 정정

아래 로그의 "활성 타깃은 아직 StandaloneWindows64"와 CLAUDE.md §4의 "활성 빌드 타깃은 아직 Windows"는 에디터 재시작 후 다시 조회하지 않고 적은 것이었다. 둘 다 정정하고, CLAUDE.md §8에 "낡은 조회 결과로 기록" 항목을 추가했다.

#### 3. 0.2 나머지 계획을 위한 실측 (읽기 전용)

| 항목 | 결과 |
|---|---|
| 템플릿 에셋 | `Assets/TutorialInfo`(7개) · `Readme.asset` — 서로만 참조, 다른 곳 참조 0 |
| 입력 | `InputSystem_Actions`는 프로젝트 전역 입력 액션으로 등록됨(EditorBuildSettings) → 유지 |
| 패키지 후보 | AI Assistant 535.7MB(asmdef 40) · Timeline 38.7MB · Collab 37.3MB(에디터 전용) · Visual Scripting 24.8MB · Sentis 15.0MB · AI Navigation 12.7MB. 다른 패키지가 의존하지 않음. 함께 빠지는 의존: 2d.sprite · mathematics(AI Assistant), dt.app-ui(Sentis) |
| WebGL 설정 | 압축 Brotli · 데이터 캐싱 · 엔진 코드 제거 · 스레드 끔은 이미 설정됨 / Decompression Fallback 꺼짐 · Managed Stripping 기본값 · IL2CPP OptimizeSpeed · 기본 캔버스 960×600(가로) |
| 렌더링 | WebGL 기본 품질 레벨 = `Mobile` → `Mobile_RPAsset`(URP Universal Renderer). GraphicsSettings 기본 RP 없음(품질 레벨로 지정) |
| MCP 제약 | `set_player_settings`는 WebGL 항목(Decompression Fallback · Stripping · 캔버스 크기)을 지원하지 않음 → `run_script` 빌더로 설정. IL2CPP 코드 생성은 `set_build_settings` |

#### 4. 실행 (사용자 결정: URP 유지 · 패키지 6개 전부 제거 · Stripping High · `Assets/Scripts` 구조)

* **씬**: Directional Light · Global Volume 삭제, Main Camera 직교 · 단색 배경(#D9DFDC, 프로토타입 바탕색) · (0,0,-10) · 카메라 후처리 끔. 첫 시도는 열거형 값을 `SolidColor`로 넣어 호출 전체가 거부됨 → 표시 이름 `Solid Color`로 재시도(CLAUDE.md §2에 기록).
* **템플릿**: `Assets/TutorialInfo` · `Readme.asset` 삭제. `SampleSceneProfile.asset`은 계획 때 "Global Volume 전용"이라고 했으나, 참조를 확인해 보니 **URP 에셋 2개의 파이프라인 볼륨 프로필**이었다. 전제가 틀렸으므로 삭제하지 않고 결정 항목으로 남겼다 → 사용자 결정(색 정확도 보호)으로 참조를 비운 뒤 삭제(아래 볼륨 프로필).
* **볼륨 프로필**: URP 에셋 2개의 `m_VolumeProfile` 참조를 비우고 `SampleSceneProfile.asset` 삭제(참조 0 확인). `set_serialized_field`에 null을 넣으면 "null"이라는 경로로 해석되어 실패 → `run_script`(`AgentScripts/Phase0ClearVolumeProfile.cs`)의 `SerializedObject`로 처리(CLAUDE.md §2에 기록).
* **패키지**: `manifest.json`에서 6줄 삭제 → 에디터가 변경을 감지해 갱신(도중 `package_resolve`는 연결 끊김 — 리로드 중 정상). 딸린 의존 3개도 함께 제거됨. 남은 스크립팅 심볼(`SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY`) · App UI 설정 참조 · AI Assistant 설정 파일 정리.
* **WebGL 설정**: `set_build_settings`(IL2CPP OptimizeSize) + `run_script` 빌더 `AgentScripts/Phase0WebGLSettings.cs`(`set_player_settings`가 WebGL 항목을 지원하지 않아서) — Decompression Fallback · Stripping High · 캔버스 540×960. `ProjectSettings.asset` diff로 저장 확인.
* **asmdef**: `ColoringBoot.Core`(`noEngineReferences: true`) + `PaintColor`(색 비트마스크 — Phase 1의 첫 조각), `ColoringBoot.Core.Tests` + `PaintColorTests`.

#### 5. 패키지 제거 후 에러 폭주 → 에디터 재시작으로 해결

패키지 제거 직후 콘솔 에러 37건, asmdef 추가 후 재컴파일에서 119건. MCP `console` 버퍼에는 일부만 잡혀서(도메인 리로드 중 누락) 콘솔 창을 직접 읽는 `AgentScripts/ConsoleDump.cs`를 만들어 확인했다.

* 제거 과정의 일회성 에러: `Failed to determine dll type`(32건) · collab-proxy DLL `FileNotFoundException` · 임포트 워커의 `[WorkerRecoverable] ... asmdef ... has been deleted`(로그의 "Aborting batchmode" 줄의 정체).
* 원인이 된 에러: `[Tool Permissions] ... Unity.AI.Assistant.Tools.Editor.dll을 찾을 수 없음` — 지워진 AI Assistant 코드가 **메모리에 남아** 지워진 자기 DLL을 찾고 있었다. 디스크의 컴파일 결과(`Library/ScriptAssemblies`)는 정상(AI DLL 0개, 새 어셈블리 빌드됨)이었고 `compilationFailed` 플래그만 남아 있었다 → 도메인 리로드가 제대로 끝나지 않은 상태로 판단.
* 사용자가 프로젝트를 새로 만들지 고민 → 재시작만 권함. **재시작 후 콘솔 에러는 무해 로그 1건, `compilationFailed: false`, "Account API" 경고도 사라짐.** CLAUDE.md §8에 "패키지 제거 뒤 옛 코드가 남음" 추가.
* 에디터 로그 위치: 이 설치에서는 프로젝트의 `Logs/Editor.log`(사용자 폴더의 `Editor.log`는 19:55 이후 갱신 없음) — CLAUDE.md §2에 기록.

#### 6. 검증

* `run_tests`(mode=editor, filter=PaintColorTests) → `RedOrYellow_IsOrange` 1/1 Passed.
* 씬 루트 = Main Camera만, 씬 저장됨. 제거한 패키지 9개가 lock 파일 · 캐시에서 사라진 것 확인.

* **해결된 이슈**:
  * 활성 빌드 타깃 기록 오류 정정
  * 템플릿 에셋 · 3D 씬 요소 · 불필요 패키지 정리, WebGL 용량 설정 적용
  * 패키지 제거 후 남은 옛 코드의 에러 폭주 — 에디터 재시작으로 해결
  * 후처리 효과가 걸린 파이프라인 볼륨 프로필 — 참조 해제 후 삭제
* **저장소 공개 확인**: 비로그인 요청에 HTTP 200 → 공개 저장소. 자동 모드 설정의 "비공개로 간주" 항목을 "공개 — 비밀값 · 개인정보 커밋 금지"로 정정했고, 이메일 등 개인정보가 커밋되지 않은 것을 확인했다. 테스트 배포 경로는 GitHub Pages `gh-pages` 브랜치로 결정(사용자).
* **남은 일**: 0.3 첫 WebGL 빌드 · gh-pages 배포 · 기준선

---

### 📅 [2026-09-24] 작업 환경 구축 — CLAUDE.md 재작성 · Git · 편집 가드 훅 · 스킬

> **다른 AI 세션을 위한 요지**: 이 프로젝트는 Unity CLI 공식 MCP(`unity-editor-mcp`)로 에디터와 연결된다. Labyrinth의 `UnityMCP`(`manage_scene` 등)와 `coplay-mcp`는 쓰지 않는다. `.unity`/`.prefab` 텍스트 편집은 훅이 차단한다(의도된 가드).

#### 1. 배경

Labyrinth(2D 회전 미로) 프로젝트의 CLAUDE.md를 가져와 이 프로젝트(육각 붓 퍼즐, WebGL)에 맞게 다시 썼다. 사용자 결정: TDD.md는 당분간 두지 않음 · Git 초기화 · 편집 가드 훅과 `unity-pipeline` · `/phase-close` 스킬 셋업 · `/qa-scene`은 보드 씬이 생긴 뒤 작성 · Task.md는 Labyrinth 체계(DoD · 태그 · Phase 헤더) 이식.

#### 2. 환경 실측

| 항목 | 결과 |
|---|---|
| Unity | 6000.6.2f1, URP 17.6, Input System 1.20(New 전용), Test Framework 1.8 |
| MCP | `unity-editor-mcp` = Unity CLI 1.0.0-beta.9의 `unity mcp` → `com.unity.pipeline` 0.7.0-exp.1 HTTP 서버. `editor_status` ready, projectPath 일치 |
| `coplay-mcp` | 사용자 레벨 등록, 프로젝트에 Coplay 패키지 없음. `list_unity_project_roots` 응답 120초 초과 → 사용 안 함 |
| 빌드 | 처음엔 WebGL Build Support 미설치(Android · Windows만) → 사용자가 설치, 에디터 재시작 후 `list_build_targets`에서 WebGL `isInstalled: true` 확인. 활성 타깃은 아직 StandaloneWindows64(→ **정정: 이미 WebGL이었음**, 위 로그 참조). 설치 직후 Unity가 ProjectSettings에 WebGL용 스크립팅 심볼 `SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY`(AI 패키지가 넣는 심볼, Standalone과 동일)를 자동 추가 |
| 씬 | URP 3D 템플릿 `SampleScene`(Main Camera · Directional Light · Global Volume), 스크립트 0개 |
| 콘솔 | 에러 1건 `Unable to join player connection multicast group (err: 10013)` — Windows 네트워크 권한 관련, 무해 |

#### 3. 작업 내용

* **CLAUDE.md 재작성**: 회전 물리(B방식) · Box2D · 풀링 등 Labyrinth 전용 내용을 삭제. 새 MCP 도구 체계(코드 수정 → `recompile` → `run_tests` 루프, 비동기 폴링, `confirm`/`dry_run`), 게임 규칙 핵심(비트마스크 색 · 막힘 판정식 · 육각 방향 표 · 붓질 의사코드), WebGL 제약, 스킬 표, 자주 하는 실수를 신설. 원본의 이스케이프된 마크다운(`\#`, `\*\*`)도 정상화.
* **Git**: `git init -b main`. `.gitignore`는 Labyrinth 것에서 그 프로젝트 전용 항목(스크린샷 폴더 · `.utmp/`)만 뺀 Unity 표준 + `/.claude/settings.local.json`.
* **편집 가드 훅** (`.claude/settings.json`): Labyrinth 훅을 이식하고 매처에 `mcp__unity-editor-mcp__write_text_file`을 추가(인자 `path`도 검사), 안내 문구를 새 MCP 도구로 교체. 백슬래시를 직접 입력하지 않도록 JSON은 Python으로 생성.
* **스킬**: `unity-pipeline`(패키지 동봉 공식 스킬 사본 + 출처 · MCP 대응 안내 3줄), `/phase-close`(Labyrinth판에서 TDD 점검 · 이월 선례를 빼고, GDD는 승인 후 수정, EditMode 테스트 전체 통과 게이트 추가).
* **사용자 결정 반영**: MCP는 `unity-editor-mcp`만 사용(다른 MCP 불필요) → CLAUDE.md에 명시하고 스킬 표에서 `claude-in-chrome` 삭제. GitHub 원격 `origin` = `JHSeawater/ColoringBoot` 등록 — 원격에 GitHub 초기 커밋 3개(최종 파일 0개)가 있어 그 위에 초기 커밋을 얹었다(강제 push 불필요).
* **push · 사용자 설정**: GitHub에 push(`f3ce685..5aa49dd`). 사용자 설정(`~/.claude/settings.json`)의 자동 모드 환경 설명에 ColoringBoot를 신뢰 저장소로 추가(Labyrinth 항목 유지, JSON 유효성 확인).
* **CLAUDE.md 최종 점검**: 웹 프로토타입(GDD §9)을 열람해 §3 규칙 서술(방향 표 · 같은 줄 판정 · 처리 순서 · 막힘/성공)과 대조 — 일치. 문서 맵에 프로토타입(참조 구현 · 스테이지 코드 9개)과 GDD §5 · §9 · §14 추가. 화면 배치 공식(Unity y축 반전) · 런타임 탐색 상한 · 숫자키 매핑 · 런타임 확인 절차, 자주 하는 실수 3건(y축 부호 · 플레이 모드 중 수정 · Windows 도구 환경) 보강.

#### 4. 검증

* **훅**: Git Bash로 가짜 입력 8종 파이프 테스트 — 차단 4종(`.unity` Windows 경로 · `.prefab` · MCP `write_text_file` · 대문자 `.UNITY`) exit 2, 통과 4종(`.cs` · 내용에 ".unity"가 든 `.md` · `.unity.meta` · 깨진 JSON) exit 0. 이어 실제 세션에서 임시 폴더의 더미 `.unity` Write → 차단 확인(재시작 없이 즉시 작동, 한글 메시지 정상).
  * 주의: Python `subprocess`로 `bash`를 부르면 WSL bash가 잡혀 `node: command not found`(exit 127)가 난다 — 훅 결함이 아니라 테스트 환경 문제. Git Bash 경로를 명시해야 한다.
* **붓질 규칙 서술**: CLAUDE.md §3 규칙(줄 전체 · 반대편 끝 출발 · 빈자리 건너감)으로 포도 스테이지를 시뮬레이션 → 최소 5수, 120가지 순서 중 8가지 성공. GDD §3 수치와 일치.
* **MCP 재시작 직후 시간 초과**: WebGL 모듈 설치로 에디터가 재시작된 뒤 첫 `list_build_targets` MCP 호출이 60초 시간 초과. `editor_status`는 ready · 대화상자 없음, 같은 명령을 CLI로 1.6초, MCP 재시도도 즉시 성공 → 일시적 현상으로 보고 CLAUDE.md §2에 재시도 규칙으로 기록.

* **해결된 이슈**:
  * CLAUDE.md가 다른 프로젝트(Labyrinth) 기준이던 문제 — 이 프로젝트 기준으로 재작성
  * 버전 관리 부재 — Git 초기화
  * 씬/프리팹 텍스트 직접 편집 사고 가능성 — 훅으로 차단(새 MCP의 `write_text_file` 경로 포함)
  * WebGL Build Support 미설치 — 사용자 설치 후 에디터 인식 확인
* **Task.md 작성** (사용자가 Phase 구성 확정): Labyrinth 체계(완료 기준 7조 · 태그 5종 · 트랙 · Phase 헤더 규약) 이식. Phase 0(개발 환경 · WebGL 파이프라인) ~ Phase 5(챕터 그림 완성)가 이번 학기 목표, Phase 6 이후는 기믹 · 아트 · 🚀출시. 0.1 작업 환경 5항목은 확인 근거와 함께 `[x]`. GDD §15 미정 사항을 필요한 Phase에 연결한 "결정 대기" 표 추가. 기계 검사: 74항목 모두 태그 1개 · 모든 Phase에 선행/완료 조건과 `[QA]` 존재.
* **남은 일**: Task.md 커밋(승인 대기) → Phase 0.2 착수(빌드 타깃 WebGL 전환, 승인 후)
