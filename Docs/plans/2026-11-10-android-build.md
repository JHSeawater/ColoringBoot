# 계획 — 9.2 Android 빌드

작성: 2026-11-10 · 실행 방식: Full Plan(플랫폼 빌드 설정 · 프로젝트 공용 설정 변경) · 상태: 검수 1라운드 지적 · 질문을 추천대로 반영, 2라운드 지적(높음 없음)도 반영(2026-11-10), 구현 완료(2026-11-10 — 결과는 `Docs/DevelopLog.md`). 달라진 점: `AndroidProbe`는 `#if UNITY_ANDROID` + `Debug.isDebugBuild`, 단계 5에서 걸린 시간 수정(`PuzzleController`)

## 원래 요청(그대로)

> Android 빌드 작업을 하겠다.
> 시작하기 전에 뭘 하면 되나.
>
> 패키지 이름은 com.jhseawater.coloringboot, 회사 이름은 JHSeawater, 폰은 Galaxy S25 Edge

## 목표

같은 프로젝트에서 시험용 Android APK를 만들어 Galaxy S25 Edge에서 처음부터 플레이할 수 있게 하고, WebGL 빌드 · 배포본의 동작은 바꾸지 않는다(`Docs/Task.md` 9.2). 출시용 서명 키 · AAB · 스토어 등록은 범위 밖이다. 지금 9.2에 있는 서명 키 항목은 단계 7에서 9.3으로 옮긴다(원스토어가 먼저라 첫 출시 때 필요). AAB는 9.5에 있다.

## 지금 사실 (2026-11-09~10 조회)

코드 · 설정으로 확인:
- Android 모듈 · SDK(android-34 · 36 · 37.0) · NDK 27.2 · OpenJDK 폴더가 에디터(`C:/UnityEditor/6000.6.2f1`)에 있고 `list_build_targets`가 설치됨으로 보고한다. adb는 PATH에 없고 `.../AndroidPlayer/SDK/platform-tools/adb.exe`에 있다.
- 활성 타깃 WebGL · 빌드 프로필 0개 · 빌드 씬은 `Board.unity` 하나.
- Android 쪽 값: IL2CPP · ARM64만 · 최소 API 26 · 타깃 API 자동 · Vulkan + GLES3(수동) · GameActivity 진입(`androidApplicationEntry: 2`) · 전체 화면 · 안전영역 밖 렌더 켜짐 · 품질 `Mobile` · 기본 텍스처 압축 ASTC · 스트리핑 설정 없음(WebGL만 High).
- 패키지 이름은 템플릿 값(`com.UnityTechnologies.com.unity.template.urpblank`), 회사 이름 `DefaultCompany`. 화면 방향은 자동 회전(`defaultScreenOrientation: 4`, 네 방향 허용).
- `Application.targetFrameRate`를 어디서도 정하지 않는다. Unity 문서 기준 모바일 기본은 30fps다.
- 코드에 `UNITY_WEBGL` · `UNITY_ANDROID` 분기가 없고, 게임 asmdef는 앱인토스 SDK를 참조하지 않는다. SDK 런타임의 `DllImport`가 있는 파일 28개는 모두 `UNITY_WEBGL`을 포함한다. SDK의 빌드 처리기 2개(`AITSentryBuildProcessor` · `AITSentryDsnInjector`)는 WebGL 외 플랫폼을 건너뛴다. SDK WebGL 빌더는 활성 타깃이 WebGL이 아니면 전환 대화상자를 띄운다(`AITWebGLBuilder.cs:65-81`).
- SDK 런타임 asmdef(`AppsInTossSDK` · `AppsInToss.Helpers`)는 `includePlatforms: []`라 APK에도 들어가고, `RuntimeInitializeOnLoadMethod` 3개(`AITPerformanceLogger` · `AITVisibilityHelper` · `AITVersion`)가 시작 때 돌며 감시용 오브젝트도 만든다. 읽어 본 범위에서 웹 호출은 `UNITY_WEBGL`로 막혀 있다.
- `Application.absoluteURL`이 비면 `UrlQuery.IsQaHost`가 false라 Android에서 QA 주소 기능은 꺼진다.
- `ChapterArtBuilder`는 WebGL에만 ASTC 재정의를 넣는다(선화 4×4 · 색칠 6×6). Android는 재정의 없음.
- `SafeAreaFitter`는 `Screen.safeArea`를 읽어 플랫폼과 무관하게 동작한다. `PlayerPrefsStore`도 플랫폼 분기가 없다.
- 시스템 뒤로가기를 처리하는 코드가 없다. `PuzzleController`는 `Keyboard.escapeKey`를 선택 해제로 쓴다.
- `GameFlow`에서 본 `StatsView` 호출은 `?stats`뿐이라 Android에서는 열리지 않는다(구현 때 다시 확인).
- `packages-lock.json`의 미커밋 변경은 앱인토스 SDK의 해시다(`release/v3.5.0` 브랜치가 새 커밋 `b54421e`로 움직임).
- Android 16 이상에서 타깃 API 36 이상 앱에는 시스템이 `KEYCODE_BACK`을 보내지 않는다(Android 공식 문서). 타깃이 자동이고 설치된 SDK의 최고 버전이 37.0이라 해당될 수 있다.
- 이 프로젝트는 `androidPredictiveBackSupport: 1`(`ProjectSettings.asset:86`)이다. Unity 문서는 이 설정이 Android 13 이상에서 `OnBackInvokedCallback`을 쓰게 한다고 설명한다. 따라서 S25 Edge는 OS · 타깃 API와 상관없이 아래 합성 경로를 탈 가능성이 높다.
- Unity 6.6 런타임(`classes.jar`)은 API 33 이상에서 초기화 때 `OnBackInvokedCallback`을 바로 등록하고, 콜백이 오면 `KEYCODE_BACK`의 눌림 · 뗌을 연달아 합성해 `Activity.dispatchKeyEvent`로 넣는다. 해제는 네이티브가 `UnityPlayer.setBackButtonLeavesApp(true)`를 부를 때다.
- `wasPressedThisFrame`은 처음 호출할 때만 아주 빠른 입력을 놓칠 수 있다(Input System 1.20 `ButtonControl.cs:247` 주석). 그 뒤로는 눌림을 업데이트 번호로 기록한다(`ButtonControl.cs:349`).
- Unity 문서: Web의 `persistentDataPath` 해시는 페이지가 있는 디렉터리의 URL로 만든다. 로더의 캐시 이름에는 회사 이름이 들어가므로 빌드 파일을 한 번 다시 받을 수 있다.
- 챕터 그림은 Chapter1 · Chapter2 두 폴더가 있다. 뒤로가기 표 밖의 패널은 2초 뒤 사라지는 잠금 안내뿐이고, 기록 패널은 Android에서 열리지 않는다.

폰 실측(2026-11-10 adb — 구현 때 참고):
- 기기: SM-S937N(Galaxy S25 Edge) · 시리얼 `R5CY50WWKLD` · `arm64-v8a`(ARM64 전용 빌드와 맞음) · `device` 상태 · USB 디버깅 켜짐 · 무선 디버깅 꺼짐.
- OS: **Android 17(API 37)** · One UI 9.0 · 빌드 `CP2A.260605.016.S937NKSUADZIF`. 위 "Android 16 이상 · 타깃 36 이상" 조건에서 OS 쪽은 이미 해당한다. 타깃이 36 이상으로 빌드되면 시스템이 `KEYCODE_BACK`을 보내지 않으므로 뒤로가기는 Unity 합성 경로에 기댄다.
- 화면: 1440×3120(비율 약 2.17, `androidMaxAspectRatio: 2.4` 안) · 기본 600dpi, 현재 560dpi로 재정의됨 · 세로.
- 저장 공간: `/data` 여유 326GB. `com.jhseawater.coloringboot` 설치본 없음(패키지 이름 충돌 없음).
- **adb에 `emulator-5562`(`offline`)가 함께 잡힌다.** 실제 에뮬레이터가 아니라 이 PC의 `NTKDaemon.exe`가 쓰는 5563 포트를 adb가 에뮬레이터로 착각한 것이다. 폰에는 문제가 없고 프로그램을 끌 필요도 없지만, 시리얼 없이 실행하면 `more than one device/emulator`로 실패한다.
- Git Bash는 `/`로 시작하는 인자를 Windows 경로로 바꾼다(`adb shell df -h /data`가 실패함). `/` 경로를 넘기는 adb 명령은 `MSYS_NO_PATHCONV=1`을 붙인다.
- 자동 차단(Auto Blocker) 상태는 adb 설정 목록에 없어 읽지 못했다. USB 명령은 막히지 않는다.

확인 못 함(첫 빌드 · 실기에서 확인):
- 실제 Android 빌드가 성공하는지, Gradle이 이 환경에서 도는지.
- 합성된 뒤로가기가 GameActivity를 거쳐 Input System의 `Keyboard.escapeKey`까지 놓치지 않고 들어오는지, GameActivity에서 `Keyboard.current`가 있는지. 입력 처리가 Input System만인 이 프로젝트에서 네이티브가 `setBackButtonLeavesApp(true)`를 불러 콜백을 해제하는지.
- `predictiveBackSupport`가 APK 매니페스트의 `enableOnBackInvokedCallback`에 어떻게 쓰이는지, 타깃 36 이상에서 이 값을 끄면 옛 경로로 돌아가는지.
- 실제 타깃 API(폰의 Android 버전은 위 실측에서 확인).
- 자동 차단이 APK 설치를 막지 않는지(단계 3의 `adb install`에서 확인).
- S25 Edge의 `Screen.safeArea` 값 · 실제 프레임 · 첫 실행 시간 · APK 크기.
- `companyName`이 WebGL 저장 경로에 영향이 없다는 것(문서상 URL 기반이지만 PlayerPrefs가 같은 경로를 쓰는지는 추정 — 단계 6 배포본에서 기존 진행으로 확인).

## 요구사항

1. 패키지 이름 `com.jhseawater.coloringboot`(Android만) · 회사 이름 `JHSeawater`.
2. 시험 APK는 Unity 디버그 키로 서명한다. 출시용 키는 만들지 않는다.
3. WebGL과 Android를 한 프로젝트에서 낸다. 작업이 끝나면 활성 타깃은 WebGL로 돌려 놓아 기존 절차(`CLAUDE.md` §4)를 바꾸지 않는다.
4. 앱은 세로로 고정된다.
5. 시스템 뒤로가기로 화면을 빠져나갈 수 있다. PC WebGL의 Esc 동작은 그대로다.
6. 저장 · 백그라운드 정지 · 안전영역이 S25 Edge에서 동작한다(문제가 나올 때만 코드를 고친다).
7. 드래그가 30fps로 묶이지 않는다.
8. 챕터 그림 화질이 WebGL 배포본과 같다.

## 정한 방식과 이유

**빌드 전환 = 활성 타깃을 오간다.** `switch_build_target` → `build`로 하고 끝나면 WebGL로 되돌린다. 처음 전환은 재임포트가 오래 걸리지만 그 뒤는 캐시를 쓴다. 빌드 프로필은 지금 플랫폼별로 다르게 할 값이 PlayerSettings의 플랫폼별 칸으로 이미 나뉘어 있어 쓸 일이 없고, 프로젝트 사본은 git 밖 음원 · 템플릿을 두 곳에서 관리해야 해서 뺀다.

**세로 고정 = PlayerSettings `Portrait`.** 프로젝트 공용 값이지만 앱 시작 전에 적용되어 가로 화면이 비치지 않고, 코드 분기가 필요 없다. WebGL은 브라우저가 방향을 정해 영향이 없을 것으로 보며 단계 6에서 확인한다.

**뒤로가기 = `GameFlow.HandleBack()`.** 함수 본문은 모든 플랫폼에서 컴파일하고(에디터 QA에서 부를 수 있게), `Update`에서 부르는 곳만 `#if UNITY_ANDROID`로 묶어 WebGL의 Esc에 영향을 주지 않는다. `Update`는 화면 상태와 상관없이 매 프레임 `Keyboard.escapeKey.wasPressedThisFrame`을 읽어, 첫 호출에서만 생기는 놓침을 첫 프레임으로 한정한다.

| 지금 상태 | 뒤로가기 | 재사용 |
|---|---|---|
| 옵션 열림 | 옵션 닫기 | `OptionsView.Hide`(공개로) |
| 보드(따라 하기 · 무한 포함) | 화면의 목록 버튼과 같음 | `LeaveBoard` |
| 그림 화면 | 목록으로 | `ShowSelect` + `_toNextChapter` 지움 |
| 무한 모드 난이도 | 화면의 뒤로와 같음 | `LeaveEndless` |
| 스테이지 목록 | 타이틀로 | `ShowTitle` |
| 타이틀 | 앱 종료 | `Application.Quit` |

타이틀에서는 확인 없이 바로 종료한다(붓질마다 저장하므로 잃는 것이 없다). 스테이지 목록 → 타이틀은 이번에 새로 생기는 경로다.

**프레임 = `#if UNITY_ANDROID`에서 `Application.targetFrameRate = 60`**(`GameFlow.Awake`). 120Hz는 배터리 · 발열 부담 대비 이 게임에서 얻는 것이 적다.

**측정 빌드 = Development.** 단계 3 · 4는 Development 빌드에 임시 측정 코드를 넣어 값을 읽는다. 최종 크기와 첫 실행 시간은 측정 코드를 지운 일반 빌드로 단계 5 끝에 잰다.

## 수정 대상

기존 파일:
- `ProjectSettings/ProjectSettings.asset` — `companyName` · `applicationIdentifier.Android` · `defaultScreenOrientation`
- `AgentScripts/Build/ChapterArtBuilder.cs` — Android에도 같은 ASTC 재정의
- `Assets/Art/Chapters/**/*.png.meta` — 위 빌더를 다시 돌린 결과
- `Assets/Scripts/Game/Screens/GameFlow.cs` — 뒤로가기 · 프레임
- `Assets/Scripts/Game/Screens/OptionsView.cs` — `Hide` 공개
- `AgentScripts/README.md` · `Docs/TDD.md` · `Docs/BuildHistory.md` · `Docs/Task.md` · `Docs/DevelopLog.md` · `Docs/DECISIONS.md` · `CLAUDE.md`(빌드 명령 한 줄)

새 파일:
- `AgentScripts/Setup/Phase9AndroidSettings.cs` — 설정 적용 기록(기존 `Setup/` 관례)

임시 파일(단계 5 끝에 지우고 커밋하지 않음):
- `Assets/Scripts/Game/Platform/AndroidProbe.cs` — 측정용. 전체를 `#if UNITY_ANDROID && DEVELOPMENT_BUILD`로 묶고 `[RuntimeInitializeOnLoadMethod]`로 스스로 만들어 씬 · 프리팹을 건드리지 않는다.

다시 돌릴 빌더: `ChapterArtBuilder`만(그림 임포트, `Build(1)` · `Build(2)`). 씬 · 프리팹 · 폰트는 건드리지 않는다(새 직렬화 필드 · 화면 문구 없음).

## 구현 단계

공통: `build`는 `dry_run=true` 결과를 보여 주고 승인받은 뒤 `confirm=true`로 실행한다(`CLAUDE.md` §3). `outputPath`는 기본값(마지막 경로)에 맡기지 않고 Android는 `Builds/Android/ColoringBoot.apk`로 준다. `switch_build_target`은 `dry_run`이 없으므로 승인만 받고 `confirm=true`로 실행한다. Development 빌드는 `options: ["Development", "DetailedBuildReport"]`로 준다(`options`를 주면 기본 `DetailedBuildReport`가 빠진다).

측정 수단(단계 3 · 4 · 5 공용, adb는 `.../AndroidPlayer/SDK/platform-tools/adb.exe`). 이 계획의 `adb` 명령은 모두 `adb -s R5CY50WWKLD`로 실행한다(`emulator-5562` 오인식 — 위 폰 실측):

| 항목 | 방법 | 기록 |
|---|---|---|
| Android 버전 · 타깃 API | `adb shell getprop ro.build.version.release` · `adb shell dumpsys package com.jhseawater.coloringboot`의 `targetSdk` | 값 |
| 예측 뒤로가기 매니페스트 | `.../AndroidPlayer/SDK/build-tools/36.0.0/aapt2.exe dump xmltree --file AndroidManifest.xml Builds/Android/ColoringBoot.apk`의 `enableOnBackInvokedCallback` | 값(없으면 "없음") |
| 예외 로그 | `adb logcat -s Unity AndroidRuntime CRASH DEBUG` | C# · Java · 네이티브 예외 유무, SDK 시작 로그(`[AIT]`) 뒤 예외 유무 |
| 뒤로가기 전달 | 보드에서 칸을 탭해 선택 → 뒤로가기 → 선택이 풀리는지(`PuzzleController.cs:187`, 같은 `wasPressedThisFrame`). 제스처 · 3버튼 각 10회 | 풀림 · 놓침 · 앱이 나감 횟수 |
| 안전영역 | `AndroidProbe`가 `Screen.safeArea` · 화면 크기를 logcat에 남김 | 값 |
| 프레임 | `AndroidProbe`가 5초 평균 FPS를 logcat에 남김. 끌기를 계속하는 동안 읽음 | 값(단계 3 기준값 · 단계 4 뒤 값) |
| 걸린 시간 | 홈으로 나감(값 A 저장) → 60초 대기 → 복귀 → 약 5초 뒤 다시 나감(값 B). `adb shell run-as com.jhseawater.coloringboot cat shared_prefs/com.jhseawater.coloringboot.v2.playerprefs.xml`의 `ColoringBoot.PlayStats`로 비교. `run-as`가 안 되면 `AndroidProbe`에서 복귀 때 이 키를 로그로 남긴다 | B − A가 약 5초 |

### 0. 시작 전 정리
- 목표: 설정 변경의 차이를 깨끗하게 볼 수 있는 상태.
- 수정 대상: 미커밋 문서 변경 `Docs/Task.md` · `CLAUDE.md` · `Docs/DECISIONS.md` · `.claude/skills/review-plan/SKILL.md` 커밋(사용자 승인 후). `packages-lock.json`은 그대로 두고 단계 6 뒤 별도 커밋한다.
- 선행: 없음.
- 완료 기준: `git status`에 이 작업과 무관한 변경이 `_setup/` · `packages-lock.json`(앱인토스 SDK 해시 한 줄) 외에 없다.
- 검증: `git status` · `git diff Packages/packages-lock.json`.

### 1. 플레이어 설정
- 목표: 요구사항 1 · 4.
- 수정 대상: `Phase9AndroidSettings.cs`(`PlayerSettings.SetApplicationIdentifier` · `companyName` · `defaultInterfaceOrientation`), `ProjectSettings.asset`.
- 선행: 0.
- 완료 기준: `git diff ProjectSettings/`가 이 세 값 외에 없다. WebGL 값(압축 · 스트리핑 · 템플릿)이 그대로다.
- 검증: `get_player_settings` · `git diff`.

### 2. 챕터 그림 Android 압축
- 목표: 요구사항 8.
- 수정 대상: `ChapterArtBuilder.cs`, Chapter1 · Chapter2 그림 `.meta`(`Build(1)` · `Build(2)`).
- 선행: 1.
- 완료 기준: 두 챕터 모두 `.meta` 변화가 Android 칸뿐이고 WebGL 칸은 같다. `ChapterArt.asset` 변화 없음.
- 검증: `git diff --stat` · `.meta` 샘플 대조.

### 3. 첫 시험 빌드 · 설치 (뒤로가기 · 프레임 코드 수정 전)
- 목표: 요구사항 2 · 3의 전환 경로와 빌드 환경을 확인하고, 뒤로가기 · 안전영역 · 프레임의 실제 값을 본다.
- 수정 대상: 임시 `AndroidProbe.cs`(`Builds/Android/`는 git이 무시).
- 선행: 2, 사용자의 폰 준비.
- 완료 기준: `switch_build_target` 완료 → 에러 0으로 Development APK 생성 → `adb install` 성공 → 앱이 세로로 뜨고 logcat에 예외가 없다. 측정 수단 표의 Android 버전 · 타깃 API · 예측 뒤로가기 매니페스트 · 뒤로가기 전달 · 안전영역 · 프레임 기준값과 빌드 시간을 기록한다.
- 검증: `switch_build_target_status` · `build_status`(`status` · `result` · `totalErrors`만) · 측정 수단 표의 예외 로그 · 사용자 실기. 활성 타깃이 Android인 동안 EditMode 테스트 1회. `adb install`이 보안 정책으로 막히면 폰의 자동 차단부터 확인한다.

### 4. 뒤로가기 · 프레임
- 목표: 요구사항 5 · 7.
- 수정 대상: `GameFlow.cs` · `OptionsView.cs`.
- 선행: 3(뒤로가기가 어떻게 들어오는지 알아야 함).
- 완료 기준: 표의 모든 상태에서 의도한 화면으로 간다. 타이틀에서 앱이 종료된다. 끌기 중 5초 평균 FPS가 50 이상이다(30에 묶이지 않음). 컴파일 에러 0 · EditMode 전부 통과.
- 검증: 에디터 플레이에서 `run_script`로 화면마다 `HandleBack` 호출(`BoardQa`와 같은 방식) · 다시 Development 빌드해 실기(제스처 · 3버튼 각각) · logcat FPS.
- 멈춤 조건: 단계 3 또는 이 단계의 실기에서 ① `Keyboard.escapeKey`로 들어오지 않거나 ② 10회 중 한 번이라도 놓치거나 ③ 앱이 화면을 건너뛰고 그냥 나가면 구현을 멈춘다. 대안은 측정 결과와 함께 사용자에게 먼저 묻는다. 먼저 검토할 대안은 `PlayerSettings.Android.predictiveBackSupport = false`다(Android 전용 값이고 매니페스트를 직접 고치지 않는다. 타깃 36 이상에서 듣는지는 실기로 확인). 그다음이 공용 입력 설정이나 Android 매니페스트를 바꾸는 레거시 입력 함께 켜기 · `Input.backButtonLeavesApp` · 매니페스트 `enableOnBackInvokedCallback="false"`다.

### 5. 실기 검수 (`Docs/Task.md` 9.2 QA)
- 목표: 요구사항 6 포함 전체.
- 수정 대상: 문제가 나온 곳만. 끝에서 임시 `AndroidProbe.cs`와 `.meta`를 지운다.
- 선행: 4.
- 완료 기준: 아래 항목이 모두 확인되거나 문제가 기록된다. `AndroidProbe`가 없다(`git status`).
- 검증: 사용자 실기 + adb. 먼저 Development 빌드로:
  - 끌기 · 탭 · 방향 버튼 · 되돌리기 · 처음부터 · 힌트
  - 그림 화면에서 선화 · 색칠 화질을 WebGL 배포본과 비교(요구사항 8)
  - 세로 고정(폰을 돌려도 유지)
  - 전면 카메라 구멍 · 위아래 UI가 겹치지 않음(`Screen.safeArea` 값 기록), 화면 끝 제스처와 끌기가 충돌하지 않음
  - 끌기 중 프레임(측정 수단 표)
  - 강제 종료 뒤 · `adb install -r` 뒤 진행 유지
  - 홈으로 나갔다 돌아옴: 소리 멈춤 · 재개, 끌던 획 취소, 걸린 시간에 나가 있던 시간이 더해지지 않음(측정 수단 표)
  - 소리 켜고 끄기

  `AndroidProbe`를 지운 뒤 일반 빌드(`options` 없음)로:
  - 설치 · 실행 · 측정 수단 표의 예외 로그에 예외 없음 · 첫 실행 시간 · APK 크기

### 6. WebGL 회귀 · 타깃 복구
- 목표: 요구사항 3 · 5(WebGL 쪽). 세로 고정 설정이 WebGL 화면에 영향이 없음을 확인한다.
- 수정 대상: 없음. WebGL 빌드가 통과하면 `packages-lock.json`을 별도 커밋으로 제안한다.
- 선행: 5.
- 완료 기준: 활성 타깃 WebGL · `WebTemplate.Install` 뒤 빌드 에러 0 · `ProjectSettings`에 단계 1 외 변화 없음 · `/qa-scene` 통과 · EditMode 전부 통과 · 크기 변화 보고(기준 9,875,993바이트) · 아래 화면 항목 확인.
- 검증: 빌드 뒤 PC Chrome에서 Esc가 선택 해제만 함 · PC 가로 창이 레터박스로 깨지지 않음 · 휴대폰 세로 화면이 그대로임.
  - 사용자가 배포를 승인하면 배포본에서 위 항목과 기존 진행 유지(회사 이름 영향)를 확인한다.
  - 승인하지 않으면 `Builds/WebGL`을 로컬 서버로 열어 위 항목을 확인한다. 로컬 주소는 저장 경로가 달라 기존 진행은 볼 수 없으므로 "확인 못 함"으로 기록하고 다음 배포 때 확인한다.

### 7. 문서
- 목표: 결정 · 결과를 남긴다.
- 수정 대상: `Docs/TDD.md`(Android 설정 절) · `Docs/BuildHistory.md`(Android 칸) · `Docs/Task.md` · `Docs/DevelopLog.md` · `Docs/DECISIONS.md` · `CLAUDE.md`.
- 선행: 6.
- 완료 기준: 9.2 항목에 근거가 적히고, 9.2의 서명 키 항목이 9.3으로 옮겨져 남은 일로 보인다. 9.1의 회사 이름 항목이 `JHSeawater`로 갱신된다. 아래 "정한 사항"이 `Docs/DECISIONS.md`에 있다.
- 검증: 문서 대조.

## 위험

- **회귀**: 붓질 · 판정 · 솔버 · 스테이지를 바꾸지 않는다. `GrapeRegressionTests` · `PrototypeStageRegressionTests` · `ContentRegressionTests`는 단계 4 · 6에서 전체 EditMode로 돈다.
- **저장 호환**: 저장 형식 · 키는 바꾸지 않는다. `companyName`을 바꾸면 에디터 PlayerPrefs(레지스트리 경로가 회사 이름을 포함)의 11/11 기록은 새 경로에서 보이지 않는다. 시험 기록이라 옮기지 않으며 옛 경로에 남는다. WebGL 저장 경로는 Unity 문서상 URL로 정해져 영향이 없을 것으로 보고, 단계 6 배포본에서 기존 진행으로 확인한다. 패키지 이름은 지금 바꿔도 설치본이 없어 영향이 없고, 출시 뒤에는 바꿀 수 없다.
- **ProjectSettings 공용 값**: 회사 이름 · 화면 방향은 WebGL과 함께 쓴다. 앱인토스 SDK 빌드가 값을 남기는 문제(`CLAUDE.md` §4)는 이번에 SDK 빌드를 돌리지 않아 해당 없다.
- **앱인토스 SDK**: 활성 타깃이 Android인 동안 `AIT` 메뉴 빌드를 쓰면 전환 대화상자가 에디터를 멈춘다. 단계 6까지 쓰지 않는다. Android IL2CPP 링크에서 SDK 네이티브 호출이 문제가 되는지와 시작 때 자동 실행되는 SDK 코드의 예외는 단계 3에서 드러난다. SDK 런타임이 차지하는 크기는 단계 5의 APK 크기와 함께 본다.
- **빌드 용량**: WebGL은 단계 6에서 측정한다(코드 몇 줄 · 회사 이름이라 변화가 작을 것으로 본다). Android는 스트리핑 기본값으로 첫 크기를 재고, 크다고 판단되면 사용자에게 묻는다.
- **WebGL 제약**: 스레드 · 파일 시스템을 쓰지 않는다. 플랫폼 분기는 뒤로가기 호출과 프레임 두 곳뿐이다.
- **뒤로가기 전달**: `predictiveBackSupport`가 켜져 있어 Android 13 이상이면 Unity의 합성 이벤트에 기댈 가능성이 높다(Android 16 이상 · 타깃 36 이상은 이 설정과 상관없이 해당). 시험 폰이 Android 17이라 타깃이 36 이상이면 합성 경로가 유일한 전달 경로가 된다. 이때 `predictiveBackSupport = false` 대안이 듣는지는 실기로 확인해야 한다. 합성 이벤트가 `Keyboard.escapeKey`까지 오지 않거나 콜백이 해제되어 앱이 그냥 나갈 수 있다 → 단계 3 측정 · 단계 4 멈춤 조건.
- **임시 측정 코드**: `AndroidProbe`가 일반 빌드나 커밋에 섞이면 안 된다. 전체를 조건부 컴파일로 묶고 단계 5 끝에 지운 뒤 `git status`로 확인한다.
- **시간 · 에디터 점유**: 첫 전환 · IL2CPP 빌드 동안 에디터가 오래 묶인다. 그동안 사용자가 에디터를 쓰지 않아야 한다.
- **음원**: `Assets/Sound`는 이 PC에만 있어 다른 PC에서는 Android 빌드도 같은 준비가 필요하다.

## 후보 기획(GDD §7)

이동 횟수 제한 · 흰색에 영향이 없다. 규칙 · 솔버 · 스테이지 형식을 건드리지 않는다.

## 정한 사항 (사용자, 2026-11-10 — 검수 추천대로)

1. 타이틀에서 뒤로가기를 누르면 바로 종료한다.
2. 스테이지 목록의 뒤로가기는 타이틀로 간다.
3. 프레임은 60으로 한다.
4. `packages-lock.json`의 앱인토스 SDK 해시 변경은 단계 6의 WebGL 빌드가 통과한 뒤 별도 커밋으로 받는다.
5. 에디터의 11/11 저장 기록은 새 회사 이름 경로로 옮기지 않는다.
6. 측정용 빌드는 Development로 하고, 최종 크기와 첫 실행 시간은 일반 빌드로 잰다.

## 사용자가 준비할 것 (단계 3 전)

- [x] S25 Edge: 설정 → 휴대전화 정보 → 소프트웨어 정보 → 빌드번호 7번 탭 → 개발자 옵션 → USB 디버깅 켜기 — 2026-11-10 adb 확인(`development_settings_enabled=1` · `adb_enabled=1`)
- [x] 보안 설정의 "자동 차단"(Auto Blocker)이 켜져 있으면 테스트 동안 끄기(USB 명령과 앱 설치를 막는다) — 사용자가 완료했다고 알림(2026-11-10). adb로는 상태를 읽을 수 없어 단계 3의 `adb install` 성공을 확인한 뒤 체크한다
- [x] USB로 PC에 연결하고 폰에 뜨는 "USB 디버깅 허용"에서 허용 — 2026-11-10 adb 확인(`device` 상태, `unauthorized` 아님)
- [x] 연결과 허용을 마쳤다고 알려 주기. `adb devices` 확인은 Claude가 전체 경로로 한다(adb가 PATH에 없음). `unauthorized`면 폰에서 다시 허용, 목록에 없으면 Samsung USB 드라이버 설치 — 2026-11-10 확인(시리얼 `R5CY50WWKLD`)
