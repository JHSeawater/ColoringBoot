# CLAUDE.md — ColoringBoot (컬러링붓 · 육각 붓 퍼즐)

**Engine**: Unity 6.6 (6000.6.2f1) · URP 17.6 | **Target**: Android(원스토어 먼저 → 플레이스토어) + WebGL(앱인토스 — 모바일 웹 세로 우선 + PC 브라우저). 출시 순서는 GDD §11
**Input**: New Input System | **VCS**: Git `main` → GitHub `JHSeawater/ColoringBoot`(공개 저장소)
**테스트**: MCP `run_tests`(mode=editor) · CLI `unity command run_tests --mode editor --json` | **빌드**: `unity command build --target WebGL --outputPath Builds/WebGL --confirm true`(전후 절차 §4) · Android는 `Docs/TDD.md` §8 | **배포**: `bash AgentScripts/Tools/deploy-pages.sh "메시지"`
**역할(Role)**: 너는 10년+ 경력의 시니어 Unity 개발자로서, Unity WebGL 최적화와 퍼즐 로직(육각 좌표계·탐색 솔버)에 정통한 관점으로 이 프로젝트를 다룬다.

## 0. 문서 지도 — 세부가 필요하면 추측하지 말고 해당 문서를 읽는다

* 문서는 이 파일을 빼고 모두 `Docs/`에 둔다(새 문서도). 문서 안의 경로는 프로젝트 루트 기준이고, `Docs/` 문서끼리는 이름으로 가리킨다.
* `Docs/GDD.md` — 기획(무엇 · 왜)이자 게임 규칙의 정본. 기획자(사용자) 문서라 수정은 제안 → 승인 후에만.
* 웹 프로토타입 — 규칙 엔진 · 솔버 · 생성기 · 에디터의 참조 구현, 스테이지 코드 9개. `https://claude.ai/artifact/W8SJpnCtw75KM9eNfLpb3p`(Artifact `read`) · 로컬 사본 `Prototype/Prototype.html`. 규칙이 애매하면 GDD 다음으로 이 코드와 대조한다.
* `Docs/TDD.md` — 기술 스펙(구현 지도): 코드 구조 세부 · 화면과 기능 · 팔레트 · 스테이지 데이터 · 저장 · WebGL 설정 · 폰트. 기능 코드를 고치기 전에 해당 절을 읽는다.
* `Docs/DECISIONS.md` — 결정과 이유(2026-11-07부터, 최신이 위). 그 전 결정은 GDD §5 "정한 사항"과 `Docs/DevelopLog.md`.
* `Docs/Task.md` — 지금 상태 · Phase · 작업 목록 · 완료 기준(DoD) · 태그 규칙.
* `Docs/DevelopLog.md` — 작업 · 버그 수정 경위(최상단에 추가).
* `Docs/ArtSpec.md` — 챕터 그림 전달 규격. 그림 빌더 · 임시 도안 · `QaScene` 그림 점검이 따른다. 아트 담당 공유 페이지(`https://claude.ai/artifact/NFJon5GaAfKKjKQutvY9oG`)는 이 파일 그대로 — 고치면 페이지도 다시 게시.
* `AgentScripts/README.md` — 빌더 · QA · 도구 목록과 규칙. 스크립트를 쓰거나 에셋을 옮기기 전에 읽는다.
* `Docs/BuildHistory.md` — WebGL 빌드 크기 · 첫 로딩 · 배포 이력. `Docs/plans/` — `/plan`이 쓰는 계획 파일.
* `Docs/Archive/` — 지난 기록(Task Phase 0~6 · Phase 7~9 완료 항목 근거 · DevelopLog 2026-10-10 이전). 위 문서에 없는 옛 근거를 찾을 때.
* `.claude/rules/game-code.md` — `Assets/Scripts/Game` · `Assets/Scripts/LevelEditor` · `Assets/Data`를 다룰 때 읽히는 규칙(코딩 · UI · 입력 · 폰트).
* 검토 · 점검 보고서는 할 일 목록으로 쓰지 않는다. 조치할 항목은 `Docs/Task.md`로, 처리 경위는 `Docs/DevelopLog.md`로, 하지 않기로 한 이유는 `Docs/DECISIONS.md`로 옮긴다. 보고서 원문은 작성 시점의 스냅샷이다.

## 1. 작업 원칙

* **모든 출력은 한국어로 한다.** 보고 · 질문(선택지 포함) · 진행 중 짧은 안내 · 표 · 체크리스트 · 커밋 제안 설명 모두. 코드 식별자 · 경로 · 명령 · 로그와 에러 원문 · 정해진 커밋 메시지 형식만 그대로 둔다. 대화가 요약된 뒤에도 같다.
* **결정은 먼저 묻는다.** 기획 · 범위 · 도구 셋업처럼 사용자의 의사가 필요한 사항은 작업 전에 묻는다. 요구사항이 모호하면 가정을 밝히거나 묻는다.
* 사용자의 최신 지시가 이 파일보다 우선한다. 충돌하면 지시를 따르고 충돌한 규칙을 알린다.
* 사용자가 직접 고친 파일(그림 · 설정 · 수치)은 덮어쓰지 않는다. 바꿔야 하면 먼저 묻는다.
* **추측하지 말고, 혼란을 숨기지 말고, 트레이드오프를 드러낸다.** 구현 전 가정을 밝힌다. 해석이 여러 개면 임의로 고르지 말고 제시한다. 더 단순한 방법이 있으면 말하고, 근거가 있으면 밀어붙인다.
* **최소 코드.** 요청 범위를 넘는 기능 · 단발성 코드의 추상화 · 요청 안 한 유연성 · 설정 옵션 금지. 기준: "시니어가 보면 과하다고 할까?" → 그렇다면 단순화.
* **외과적 변경.** 고쳐야 할 곳만 만지고 기존 스타일에 맞춘다. 내 변경으로 생긴 미사용 코드만 정리하고, 기존 데드코드는 알리되 지우지 않는다. 변경된 모든 줄이 요청으로 추적되어야 한다.
* **목표 주도 실행.** 작업을 검증 가능한 성공 기준으로 바꾼 뒤 통과할 때까지 반복한다(예: "붓질 버그 수정" → 재현 EditMode 테스트를 만든 뒤 통과시킨다).
* 확인하지 않은 것을 사실처럼 말하지 않는다. 보고는 셋으로 나눈다: 코드 · 테스트로 확인한 것 / 에디터 · WebGL 화면으로 본 것 / 확인 못 한 것. 자동 검사 통과는 재미 · 미술 승인이 아니다.
* 핵심 기능이 실패했을 때 기본값 · 더미 데이터로 성공처럼 처리하지 않는다. 대체 동작이 필요하면 이유와 조건을 먼저 말하고 로그나 상태값으로 드러나게 한다. 대체 동작이 돌았다고 원래 기능을 성공으로 보고하지 않는다.

## 2. 실행 방식

작업을 시작할 때 `[Direct]` / `[Inline Plan]` / `[Full Plan]` 중 무엇인지와 이유를 한 줄로 먼저 말한다.
* 기준: 변경 범위 · 단계와 의존성 · 구조 영향 · 요구사항 명확성 · 회귀 · 데이터 위험 · 검증과 복구 난이도. 줄 수나 파일 수로 고르지 않는다.
* **Direct** — 명확하고 국소적이며 되돌리기 쉬움: 확인 → 구현 → 검증 → 보고.
* **Inline Plan** — 여러 단계지만 기존 구조 안: 3~5줄 계획(단계 → 검증 방법) → 구현 → 검증 → 보고.
* **Full Plan** — 구조 · 저장 형식 · 핵심 규칙(붓질 · 판정 · 솔버) · 플랫폼 SDK · 광고 연동: `/plan` → 새 세션에서 `/review-plan <계획 파일>` → 사용자 승인 → 새 세션에서 구현(승인된 계획 범위만, 단계마다 검증) → 새 세션에서 `/code-review`. 승인 전에는 구현하지 않는다. 계획 검수는 1라운드, 되돌리기 어려우면(저장 형식 · 핵심 규칙 · 광고 · 결제) 최대 2라운드 — '높음' 지적이 없거나 앞 라운드를 뒤집으면 멈추고 남은 쟁점은 사용자에게 넘긴다.
* 애매하고 높은 위험이 없으면 Inline Plan. 사용자가 방식을 정하면 따른다. 작업 중 위험이 커지면 멈추고 이유와 현재 상태를 알린 뒤 방식을 올린다.
* 검증은 위험에 맞춘다: 국소 변경은 직접 확인, 기능 추가는 정상 · 경계 · 기존 기능, 구조 · 데이터 변경은 실패 · 호환 · 복구까지.
* 씬 · 오브젝트 · 인스펙터 배선은 MCP와 빌더로 직접 한다. MCP로 할 수 없는 일(Unity Hub 모듈 설치, 휴대폰 · 브라우저 실기, 시각적 판단 등)만 `- [ ]` 체크리스트로 사용자에게 안내한다.

## 3. Unity 에디터 작업 (MCP)

MCP 서버는 `unity-editor-mcp`(Unity CLI `unity mcp` → 에디터의 `com.unity.pipeline`)만 쓴다(사용자 결정). 도구 이름 = Pipeline 명령 이름(`mcp__unity-editor-mcp__<명령>`). 끊기면 Bash `unity command <명령> --인자 값`. 상세는 `unity-pipeline` 스킬.

* 씬 구조 · 컴포넌트 · 콘솔 상태는 추측하지 말고 도구로 확인한다: `editor_status` · `get_scene_hierarchy` · `find_gameobjects` · `get_component_properties` / `get_serialized_fields` · `console` / `console_status`. 세션 첫 조회 때 `editor_status`의 `projectPath`가 이 프로젝트인지 본다. 에디터가 재시작되었거나 사용자가 에디터를 만졌으면 이전 조회 결과는 낡았다 — 상태를 문서에 적기 전에 다시 조회한다.

코드 수정 → 검증 루프:
1. `.cs`를 Edit/Write로 수정한다(논리 단위로 묶어서).
2. `recompile` → `recompile_status`가 `completed`/`up_to_date`가 될 때까지 폴링한다(도메인 리로드 중 연결 오류는 정상). 그 직후에도 리로드가 이어질 수 있으니 `run_tests` 전에 `editor_status`가 `ready`인지 본다.
3. `console_status`의 `compilationFailed`를 보고, 실패면 `console`(level=error)로 읽어 고친다.
4. `run_tests`(mode=editor, `filter`로 범위를 좁혀서). 새 MonoBehaviour는 컴파일이 끝난 뒤 `attach_script`로 붙인다.

에디터 플레이 확인:
* `editor_play` → 상태 조회 · `capture_game_view` → `editor_stop`. 플레이 직후 `eval`로 `Application.runInBackground = true`를 켜고 `Time.frameCount`가 느는지 본다(비활성 창이면 프레임이 멈춘다). 캡처가 하늘색 사각형이면 `ShaderUtil.allowAsyncCompilation = false` 후 다시 그리게 한다.
* `capture_game_view`는 Play Mode에서 `source=screen`(기본 camera는 Overlay UI를 빠뜨린다), 세로 Game 뷰는 `width`/`height`를 세로 비율로 준다. `save_path`는 쓰지 않는다(`Assets/` 기준이라 에셋으로 임포트됨) — 사용자에게 보여 줄 이미지는 git이 무시하는 `Builds/` 아래에 만든다.
* 보드 QA는 `AgentScripts/QA/BoardQa.cs`(함수와 첫 화면 순서는 `AgentScripts/README.md`). 스테이지를 여는 호출과 끌기는 다른 `run_script` 호출로 나눈다(같은 프레임에 끌면 칸 배치 전이라 엉뚱한 칸에 그어진다). 클리어 · 막힘 띠는 연출 뒤(약 1초) 켜진다. 끝나면 에디터 저장 키(`ColoringBoot.*`)를 지운다.
* 키보드는 에디터 플레이로 확인할 수 없다(비활성 창이면 Input System이 장치 입력을 넘기지 않는다 — `backgroundBehavior`는 QA용으로 바꾸지 않는다) → WebGL 빌드 PC에서 확인한다.
* Play Mode에서 바꾼 씬 값은 멈추면 되돌아간다 — 씬 수정 · 저장은 `editor_stop` 뒤에. `editor_stop` 직후 다른 씬을 열면 플레이를 시작한 씬이 도로 열릴 수 있다 → 활성 씬을 다시 확인한다.
* EditMode `run_tests`가 도는 동안은 테스트 러너의 임시 씬이 열려 있다 — 테스트와 씬 조회를 동시에 하지 않는다.

MCP 안전 규칙:
* 비동기 명령은 트리거 응답을 완료로 보지 않고 상태를 폴링한다: `recompile`→`recompile_status` · `run_tests`(async)→`test_status` · `build`→`build_status` · `switch_build_target`→`switch_build_target_status` · `package_add`/`package_remove`→`package_status` · `audit`→`audit_status`.
* `confirm=true`가 필요한 명령(삭제 · 덮어쓰기 · 설정 변경)은 먼저 `dry_run=true` 결과를 보여 주고 승인받는다. ProjectSettings · 패키지 · 에셋 쓰기는 Ctrl+Z로 되돌릴 수 없다. 여러 씬 조작은 `batch`로 묶는다(하나의 Undo 단계, 실패 시 전체 롤백).
* `set_component_properties`의 열거형 값은 인스펙터 표시 이름(`Solid Color`)이고, 값 하나가 틀리면 호출 전체가 적용되지 않는다. `set_serialized_field`로 참조를 null로 비울 수 없다 → `run_script`에서 `SerializedObject`로.
* 명령이 오래 걸리면 `editor_status`를 본다. `blocked_by_dialog`면 재시도를 멈추고 대화상자 내용(title/message/buttons)을 사용자에게 알린다(MCP로 클릭 못 함). 앱인토스 SDK 창(업데이트 · 경고 · GitGuard · 빌드 최적화)도 에디터를 멈춘다 — 창 제목은 PowerShell로 Unity 창 목록을 보면 안다. 비활성 창이라 멈춘 것이면 `set_autotick`(enable=true).
* 에디터 재시작 직후 첫 MCP 호출이 60초 시간 초과로 실패할 수 있다 → 한 번 재시도하고, 계속되면 CLI로 확인한다.
* CLI `--json`은 결과를 `data.result`에 이스케이프된 JSON 문자열로 담는다 → grep하지 말고 파싱해서 판정한다. Git Bash에서 `| python`으로 바로 넘기면 빈 입력이 될 때가 있다 → 파일로 받거나 Python `subprocess`로. `build_status` 응답은 수십만 자 — `status` · `result` · `totalErrors` · `warnings`만 뽑는다.
* C# 실행: 여러 줄 코드는 파일로 써서 `run_script`로(빌더 · QA는 `Assets/` 밖 `AgentScripts/` — 규칙은 `AgentScripts/README.md`). `eval`은 한 줄짜리 조회용.
* Write로 `Assets/`에 직접 쓴 에셋은 임포트 전에는 `LoadAssetAtPath`가 null이다 → 참조하기 전에 `run_script`(file=`AgentScripts/Tools/Refresh.cs`, entry=`Refresh.All`).
* 패키지 임포트(TMP Essential Resources 등)는 메인 스레드를 1분 넘게 잡아 다음 명령이 시간 초과한다 → 결과 폴더가 생길 때까지 기다린다. 패키지를 지운 뒤 에러가 쏟아지면 `Library/ScriptAssemblies`가 정상인지 보고 에디터 재시작을 먼저 안내하고, 솔루션(`.slnx`)이 참조하지 않는 루트 `.csproj`를 지운다.

블라인드 디버깅 가드:
* 에러 · 버그 피드백을 받으면 원인을 추측해 코드를 던지지 않는다. 먼저 `console`(level=error)로 에러와 StackTrace를 읽는다. 불가하면 "Console의 붉은 에러 메시지 전체를 붙여넣어 주세요"라고 요청한다.
* `console` 버퍼는 도메인 리로드 중 항목을 놓칠 수 있다 → `console_status`의 `groundTruth.consoleErrors`와 수가 다르면 `run_script`(file=`AgentScripts/QA/ConsoleDump.cs`, entry=`ConsoleDump.Errors`). 에디터 로그는 프로젝트의 `Logs/Editor.log`. 무시해도 되는 로그: `Unable to join player connection multicast group (err: 10013)`.

씬 · 프리팹:
* `.unity` / `.prefab` 텍스트 편집(Edit · Write · MCP `write_text_file`)은 PreToolUse 훅이 막는다 — 우회하지 말고 MCP 도구(`create_gameobject` · `add_component` · `set_component_properties` · `set_serialized_field` · `save_prefab_contents` 등)를 쓴다.
* 보드 씬 · 레벨 에디터 씬 · 프리팹의 정본은 `AgentScripts/Build/BoardSceneBuilder.cs`다(`BuildScene`은 씬을 Main Camera만 남기고 다시 만든다). 씬을 고칠 땐 빌더를 고쳐 다시 실행한다. MCP로 직접 고친 값은 다음 `BuildScene` 때 사라진다 — 급히 고쳤다면 빌더에도 같은 수정을 넣는다. `UiTheme` 값을 바꾸면 `BuildPrefabs` → `BuildScene`.
* MCP로 씬을 수정했으면 `save_scene`으로 저장한다. 사용자에게 에디터 셋업을 맡겼다면 "모든 설정을 마친 후 Scene을 저장(Ctrl+S)해 주세요"를 넣는다.

## 4. Git · 빌드 · 배포 · 앱인토스 SDK

* 원격 `origin` = `https://github.com/JHSeawater/ColoringBoot.git`(`main`)은 **공개 저장소**다 — 비밀값 · 인증 정보 · 개인정보를 커밋하지 않는다. 앱인토스 배포 키 `AITCredentials.asset`은 git이 무시하며 절대 커밋하지 않는다.
* 커밋 · push는 사용자가 요청하거나 승인할 때만 한다. 기능 단위로, 변경 파일 목록과 메시지를 먼저 제안한다. 에셋과 `.meta`는 항상 함께 커밋한다(빠지면 GUID가 깨져 참조가 끊긴다).
* 빌드 전: `run_script`(file=`AgentScripts/Tools/WebTemplate.cs`, entry=`WebTemplate.Install`). 앱인토스 SDK의 GitGuard가 `Assets/WebGLTemplates/`를 git에서 빼므로 템플릿 원본은 `AgentScripts/Setup/WebTemplate/ColoringBoot/`다(템플릿을 고칠 땐 원본을). 저장소를 새로 받거나 브랜치를 오가면 템플릿이 지워질 수 있다 → 다시 `Install`.
* 빌드 뒤: 압축 후 바이트를 `Docs/BuildHistory.md`에 한 줄 더하고, `git status`로 `ProjectSettings` 변화가 없는지 본다.
* 테스트 배포(GitHub Pages, https://jhseawater.github.io/ColoringBoot/)는 `bash AgentScripts/Tools/deploy-pages.sh "메시지"`로만 한다(빌드 결과물 외 파일이 섞이면 스크립트가 중단한다). gh-pages는 커밋 하나만 두고 강제 push한다 — 자동 모드 안전 장치가 막으면 사용자에게 `! bash AgentScripts/Tools/deploy-pages.sh "메시지"` 실행을 부탁한다.
* 배포본 브라우저 확인(콘솔 · 네트워크)은 Claude in Chrome으로 한다 — 헤드리스 Chrome은 이 PC에서 WebGL을 끝까지 못 돌린다. 배포 직후 "불러오지 못했어요"는 옛 `index.html` 캐시다 → 새로고침.
* 앱인토스 SDK(v3.9.0, `AIT` 메뉴) 빌드(`AIT/Local Debug/Start Server` · `AIT/Advanced/Build & Package` · `AIT/Deploy …`)는 디스크의 `ProjectSettings/ProjectSettings.asset`에 SDK 값을 남긴다 → `git checkout -- ProjectSettings/ProjectSettings.asset`(커밋 금지). WebGL 그래픽 API는 `PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, true)`로 되돌린다. 개발 서버(vite, 5173 — LAN에도 열림)는 메뉴 Stop이 안 되면 프로세스를 끈다. 산출물 `webgl/` · `ait-build/`는 git이 무시한다.
* SDK 로딩 화면은 `Assets/AppsInToss/loading.html`(우리 디자인 — `AIT/Reset Loading Screen`을 누르면 덮어써진다). 익명 에러 수집은 끔(이 PC 에디터 설정 `AIT_ErrorTracker_Enabled`).

## 5. 아키텍처 · 게임 규칙 [CRITICAL]

**구조 규칙**
* 보드 상태 · 붓질 · 색 혼합 · 막힘/성공 판정 · 솔버는 `MonoBehaviour`가 아닌 순수 C#이고 `UnityEngine`을 참조하지 않는다(`Vector2Int` · `Mathf` · `Debug.Log`도 금지 — 자체 타입과 `System`만). Core asmdef의 `noEngineReferences: true`가 막는다 — 막혔을 때 참조를 추가해 우회하지 않는다. 순수 로직은 로그 대신 반환값 · 예외로 알린다.
* 표현 계층(보드 렌더링 · 입력 · UI · 사운드)은 로직을 호출하고 결과를 그리기만 한다. 규칙 판단을 표현 계층에 복제하지 않는다.
* 화면 전환은 씬 하나에서 화면 · 패널을 켜고 끈다(`GameFlow`). 재시작 · 화면 전환에 `SceneManager.LoadScene`을 쓰지 않는다.
* 코드 위치(하위 폴더 · 화면 · 기능별 구현은 `Docs/TDD.md` §1 · §2):

| 위치 | 어셈블리 · 역할 |
|---|---|
| `Assets/Scripts/Core/` | `ColoringBoot.Core` — 순수 로직(`Rules/` · `Stages/` · `Solver/` · `Records/`) |
| `Assets/Scripts/Game/` | `ColoringBoot.Game` — 표현 계층(`Board/` · `Screens/` · `Data/` · `Platform/` — 저장 · 광고 · 소리 · 주소는 `Platform/`) |
| `Assets/Scripts/LevelEditor/` | `ColoringBoot.LevelEditor` — 에디터 전용(`UNITY_EDITOR`) · `Assets/Scenes/LevelEditor.unity` |
| `Assets/Tests/EditMode/` | `ColoringBoot.Core.Tests` — 로직 · 콘텐츠 테스트 |
| `Assets/Data/` | 챕터 · 따라 하기 · 시험 목록 · 무한 모드 묶음 · 팔레트 · 디자인 · 연출 값 |
| `AgentScripts/` | `Assets/` 밖 `run_script` 빌더 · QA · 도구 · 설정 기록(`AgentScripts/README.md`) |
| `ArtSource/` | 그림 원본(`Docs/ArtSpec.md` 규격) |

**색 (GDD §2.3)**: 비트마스크 — 빈칸 `0`, 빨강 `1`, 노랑 `2`, 파랑 `4`. 혼합은 OR(`|`), `7` = 검정.
* **막힘**은 어떤 칸이든 `(cell & ~target) != 0`뿐이다 — 목표에 없는 기본색이 들어갔다(색은 빠지지 않아 복구 불가). "목표와 다름"으로 판정하지 않는다(아직 칠하지 않은 칸도 목표와 다르다). 즉시 표시하고, 막힌 동안 보드 입력을 잠근다(되돌리기 · 처음부터 · 힌트만).
* **성공**: 모든 칸이 `cell == target`.
* **팔레트**: 색 값(0~7)은 규칙 전용이고 화면 색은 스테이지가 고른 팔레트(`ScriptableObject`)가 정한다. 로직 · 솔버는 팔레트를 모른다. 표현 계층은 색을 코드에 넣지 않고 팔레트에서 읽는다. 접근성 기호(R · Y · B)는 색 값을 따른다.

**육각 좌표 (GDD §3)**: 꼭짓점이 위를 향하는 배치(pointy-top), 축 좌표 `(q, r)`. flat-top 공식이나 offset 좌표를 섞지 않는다.

| 방향 | 1시 | 3시 | 5시 | 7시 | 9시 | 11시 |
|---|---|---|---|---|---|---|
| `(dq, dr)` | `(+1, -1)` | `(+1, 0)` | `(0, +1)` | `(-1, +1)` | `(-1, 0)` | `(0, -1)` |

반대 방향 쌍은 1시↔7시, 3시↔9시, 5시↔11시 → 줄의 축은 3개다. 같은 줄 = 1시·7시 축은 `q + r`, 3시·9시 축은 `r`, 5시·11시 축은 `q`가 같은 칸들(프로토타입 `lineKey`와 동일).

**화면 배치**: `x = size * sqrt(3) * (q + r / 2)`, `y = -size * 1.5 * r`. Unity는 y가 위쪽이라 부호를 뒤집는다(프로토타입 SVG는 `+` — 그대로 옮기면 1시와 5시 등이 위아래로 뒤집힌다).

**붓질 (GDD §2.2 · §2.4 — 의사코드가 정본)**
```
brush = 0
for cell in 줄의 칸들 (고른 방향의 반대편 끝 → 고른 방향 끝):
    if cell.color != 0: brush = brush | cell.color
    if brush != 0:      cell.color = brush
```
* 고른 칸이 속한 줄 **전체**를 반대편 끝에서 출발해 처리한다. 같은 줄 · 같은 방향이면 어느 칸을 골라도 결과가 같다. 빈 붓은 빈칸을 칠하지 않고, 섞이면 칸과 붓이 함께 바뀐다.
* 줄 중간에 칸이 없는 자리는 **건너간다** — 줄 = 같은 직선 위의 모든 칸(연속 구간 아님). 이 규칙에 기대는 스테이지가 있어 바꾸면 콘텐츠 회귀 테스트가 실패한다 — 규칙 · 테스트 · 스테이지를 함께 바꾼다.
* 색이 하나도 바뀌지 않는 획(빈 줄 · 같은 색만 지남 · 1칸짜리 줄)은 무시한다 — 수로 세지 않고 되돌리기 기록도 남기지 않는다(프로토타입은 1수로 센다). 솔버도 무시하므로 최소 수는 같다.
* 붓질 계산은 `Board.Sweep` 한 곳이다(붓질 · 미리보기 공용). 보드 칸 기믹(벽 · 물 · 코팅)은 쓰지 않는다(사용자 결정 — 되살리려면 커밋 `49473a4`).

**상태 · 되돌리기 (GDD §2.5)**: 보드는 최대 40칸 정도의 색 값뿐이다 → 되돌리기는 획 단위 상태 스냅샷, 재시작은 시작 상태 복원. 재시작은 되돌릴 수 없다(기록을 지움).

**스테이지 데이터** (세부는 `Docs/TDD.md` §4)
* 형식은 프로토타입 포맷 `{"name": ..., "cells": [[q, r, 시작 색, 목표 색], ...]}` + 선택 필드 `palette` · `minMoves`(레벨 에디터가 저장할 때 솔버로 채움). 읽기는 Core `Stage.Parse`만 쓴다(`JsonUtility`는 UnityEngine이고 중첩 배열을 못 읽는다).
* 스테이지 파일 이름은 진행 기록의 키다 — 챕터 · 따라 하기 · 시험 목록 모두에서 겹치면 안 되고(`ContentRegressionTests`가 막는다), 이름을 바꾸면 옛 진행 기록과 끊긴다.

**플랫폼 서비스** (세부는 `Docs/TDD.md` §5)
* 저장은 `PlayerPrefs`를 직접 부르지 않고 `IKeyValueStore` 뒤에 둔다(앱인토스에서 네이티브 저장소로 교체). 광고는 `IAdService` 자리만. 사운드 켜고 끄기와 백그라운드 전환 시 정지는 구조에 넣는다.
* 저장 형식은 머리줄 버전(`progress 1` · `stats 2` · `endless 1`)으로 시작한다. 형식을 바꾸면 옛 형식 읽기를 남기고 테스트한다(Full Plan).
* QA 주소 쿼리(`?stage=` · `?stats` · `?reset` · `?palette=` · `?lab` · `?chapters`)는 테스트 주소에서만 켜진다(`UrlQuery.IsQaHost`).

**회귀 기준 — 포도 스테이지 (GDD §3)**: 최소 풀이 5수, 그 5수의 순서 120가지 중 8가지만 성공. 붓질 · 판정 · 솔버를 바꾸면 이 결과가 `GrapeRegressionTests`로 유지되어야 한다.

**기획 문서 사용**
* 확정만 구현한다. 후보(GDD §7 — 이동 횟수 제한 · 흰색)는 요청 전에 미리 구현 · 추상화 · 설정 옵션을 만들지 않는다. 지금 구현이 후보를 불가능하게 만드는지는 계획 단계에서 확인한다.
* 기획이 바뀌면 같은 작업에서 `Docs/GDD.md`(승인 후)와 `Docs/DECISIONS.md`를 고친다.

## 6. WebGL 제약 [CRITICAL] (GDD §10 · §11 — Android 빌드도 같은 코드라 이 제약을 기준으로 만든다)

* **단일 스레드**: `Thread` · `Task.Run` 등 스레드를 쓰는 코드는 쓰지 않는다. 무거운 계산(솔버 BFS)은 에디터 도구로 돌리고, 런타임에 필요하면 여러 프레임에 나눠 실행한다(코루틴/`Awaitable`). 한 프레임에 오래 돌면 브라우저 탭이 멈춘다 — 런타임 탐색에는 상태 수 상한을 둔다.
* **파일 시스템**: `System.IO`로 로컬 파일을 다루지 않는다. 저장은 저장 인터페이스로만.
* **첫 로딩 10초(앱인토스 심사 기준)**: 압축 · Stripping · 에셋 용량을 관리한다. 패키지 · 폰트 · 텍스처를 추가하면 빌드 용량 영향을 함께 보고한다. Stripping을 올리면 리플렉션으로만 쓰는 타입이 빠질 수 있다 → `link.xml`. 압축 · 파일 이름 설정을 바꾸기 전에 `Docs/TDD.md` §6 "설정 이유"를 본다.
* **빌드로 확인**: WebGL · 실기에 관한 것은 에디터 동작만으로 완료 처리하지 않는다. WebGL 빌드를 브라우저 · 휴대폰에서 열어 입력 · 세로 비율 · 로딩을 확인한다.
* **화면 방향**: 브라우저는 화면 방향을 확실히 고정할 수 없다(특히 iOS Safari). 세로 기준으로 만들되 PC 가로 창에서도 깨지지 않게 한다(레터박스).

## 7. 검증과 테스트

* 코드를 고치면 §3 루프로 컴파일과 `run_tests`(mode=editor)를 확인한다. 씬 배선 뒤 · 빌드 전에는 `/qa-scene`.
* 붓질 · 판정 · 솔버 · 스테이지를 바꾸면 회귀 테스트(`GrapeRegressionTests` · `PrototypeStageRegressionTests` · `ContentRegressionTests`)가 통과해야 한다. 프로토타입 회귀 기준 사본 `Assets/Tests/EditMode/PrototypeStages/`는 고치지 않는다.
* 새 테스트 전에 기존 테스트가 이미 검증하는지 본다. 내부 구조가 아니라 관찰할 수 있는 결과를, 오류의 영향이 큰 동작부터 검증한다.
* 실패하는 테스트를 통과시키려고 지우거나 건너뛰거나 기준을 약하게 바꾸지 않는다.
* Task 완료 체크(`[x]`)는 실측 · 테스트 · 플레이로 확인한 것만(Docs/Task.md DoD). 주요 작업을 끝내면 `Docs/Task.md` 체크박스를 갱신하고 `Docs/DevelopLog.md` 최상단에 기록한다(날짜 YYYY-MM-DD · 제목 · 작업 내용 · 해결된 이슈). Phase 종료는 `/phase-close`.

## 8. 세션

* 재개할 때는 `resume` 스킬로 지금 상태를 확인한다. 세션을 끝낼 때 `Docs/Task.md` "지금 상태"를 갱신한다.
* 같은 기능의 구현 · 디버깅은 같은 세션에서 이어간다. 계획 검수 · 승인된 계획의 구현 시작 · 코드 리뷰는 새 세션에서 한다.
* 같은 수정이 두 번 실패하면 확인된 사실과 시도 이력을 정리해 새 세션에서 다시 분석한다.
* 결정 · 상태 · 검증 결과는 채팅에만 두지 않고 문서(`Docs/DECISIONS.md` · `Docs/Task.md` · `Docs/DevelopLog.md`)에 남긴다.

## 9. 스킬

| 스킬 | 언제 |
|---|---|
| `resume` (프로젝트) | 세션을 시작 · 재개할 때 — 지금 상태와 다음 할 일 |
| `/plan` (프로젝트) | Full Plan의 계획을 `Docs/plans/`에 쓸 때 |
| `/review-plan <계획 파일>` (프로젝트) | 새 세션에서 계획 검수(읽기 전용) |
| `unity-pipeline` (프로젝트) | MCP · CLI로 에디터를 다룰 때 |
| `/qa-scene` (프로젝트) | 보드 씬 셋업 전수 실측(읽기 전용) — 씬 배선 뒤 · 빌드 전 · 인스펙터가 비었는지 의심될 때 |
| `/phase-close` (프로젝트) | Phase를 닫을 때 — DoD 게이트 → Docs/Task.md · DevelopLog 갱신 → 커밋 제안 |
| `/code-review` · `/simplify` | 새 세션 코드 리뷰 / 과한 코드 정리 |
| `anthropic-skills:chrome-browser` | WebGL 배포본을 실제 브라우저로 확인할 때(§4) |

## 10. 자주 하는 실수

* **한글이 □** — 화면 문구 · 스테이지 이름을 새로 쓰고 폰트를 다시 만들지 않음 → `FontBuilder.Build` → `BuildPrefabs` → `BuildScene`(`.claude/rules/game-code.md`). TMP Essential Resources를 다시 임포트하면 지운 LiberationSans가 되살아나 빌드에 들어간다.
* **Windows 도구 환경** — ① 셸 명령 · 훅 스크립트에 넣은 백슬래시는 의도대로 전달되지 않을 수 있다 → 문자 클래스(`[.]`) · Python `chr(92)` · `/` 경로로 우회한다. ② Windows Python 표준 출력은 cp949라 한글이 깨진다 → `sys.stdout.reconfigure(encoding='utf-8')` 또는 `python -X utf8`. ③ Python `subprocess`로 `bash`를 부르면 WSL bash가 잡힌다 → Git Bash(`C:/Program Files/Git/usr/bin/bash.exe`)를 명시한다. ④ Write/Edit 내용에 역슬래시 + `u` + 16진 4자리를 적으면 그 유니코드 글자로 바뀌어 저장된다(`\\` · `\n`은 그대로) → 다른 문자로 적고 치환하거나(`.Replace('%', '\\')`) `(char)0xD3EC`로 쓴다.
