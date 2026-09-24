# Development Log (개발 일지)

이 문서는 프로젝트의 개발 타임라인, 주요 변경 사항, 마주친 버그와 해결 방법을 기록한다.

## [작성 규칙]
1. 나(AI)는 Task(예: Phase 1)를 완료하거나 중요한 버그를 수정했을 때, 이 문서의 **최상단(이 규칙 바로 아래)**에 새 로그를 추가한다.
2. 각 로그는 **날짜(YYYY-MM-DD), 제목, 작업 내용, 해결된 이슈**를 포함한다.

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
