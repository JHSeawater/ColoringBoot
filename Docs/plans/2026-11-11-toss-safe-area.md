# 계획 — 앱인토스 상단바 · 닫기 버튼 겹침 수정 (SDK 안전영역)

작성: 2026-11-11 · 실행 방식: Full Plan(게임 코드의 첫 앱인토스 브리지 호출 — 플랫폼 SDK) · 상태: 검수 2라운드 · 검토 보완 3건 반영, 사용자 승인(2026-11-11) — 구현은 새 세션에서

## 원래 요청(그대로)

> 샌드박스 앱에서 확인했다. 정상적으로 토스 공통 종료 확인창이 뜬다.
> 다만 샌드박스 테스트 과정에서 상단의 제목과 옵션, 목록 버튼들이 상단바와 끄기 버튼에 겹쳐져 눌리지 않는다.
> 해당 위치를 바꾸거나 내려야 할 것 같다. @Screenshot/ 에 캡쳐 사진을 넣어두었다.

방식 선택: "A로 가자." — SDK가 주는 실제 값으로 게임 UI 전체를 닫기 버튼 아래로 내린다.

## 목표

토스 안에서 어떤 화면의 게임 버튼도 상태바와 토스 `⋯ ✕` 버튼에 겹치지 않고 눌린다. GitHub Pages WebGL · Android · 에디터의 화면 배치는 바뀌지 않는다. 예외는 3단계의 목록 화면 수치(작은 그림 · 챕터 넘기기 버튼 · 격자)뿐이다.

## 지금 사실 (2026-11-11 조회)

코드 · 문서로 확인:
- `SafeAreaFitter`는 `Screen.safeArea`만 읽어 `Canvas/SafeArea` 패널의 앵커를 맞춘다. 모든 화면 · 패널이 이 패널 아래에 있어서(`BoardSceneBuilder`) 앵커만 바꾸면 UI 전체가 내려간다. 패널 밖 배경은 카메라 배경색이다. 보드는 `OnRectTransformDimensionsChange`로 다시 배치된다.
- 게임 코드에 `AIT` 호출과 `DllImport`가 없다. 프로젝트의 첫 jslib이 된다. EditMode 테스트 asmdef는 Core만 참조한다.
- SDK 3.9.0(SHA `c289228…` 고정): `window.AppsInToss.SafeAreaInsets.get()`은 CSS 픽셀 값(`top` · `bottom` · `left` · `right`)을 동기로 돌려준다(SDK jslib 주석과 호출 방식).
- `window.AppsInToss`는 `AITTemplate`의 `unity-bridge`에만 있다. 토스 밖(우리 템플릿)에서는 접근이 예외를 던진다.
- SDK의 `AIT.GetDevicePixelRatio()`는 `unityInstance`가 없으면 `window.devicePixelRatio`로 떨어진다(템플릿에 `unityConfig`가 없다). `window.unityInstance`는 로더 약속이 끝난 뒤 정해지고, 그 약속은 엔진의 초기화 완료를 기다린다. 첫 `Update`가 그보다 빠른지는 확인하지 못했다.
- `AITTemplate`은 모바일에서 `config.devicePixelRatio`를 낮춘다. 화면 40만 CSS 픽셀 미만 iPhone은 2, 메모리 4GB 이하 Android는 2 이하다. 이때 `window.devicePixelRatio`와 실제 캔버스 배율이 다르다.
- 엔진은 캔버스 배율을 `Module.matchWebGLToCanvasSize == false ? 1 : Module.devicePixelRatio || window.devicePixelRatio || 1`로 정한다(`build.framework.js`). jslib 안에서는 `Module`을 바로 읽을 수 있다.
- `AITTemplate`의 `#unity-container`는 `position:fixed` · `top:0` · 높이 100%이고 캔버스도 100%다. 그래서 캔버스 CSS 크기가 뷰포트 크기와 같고, 인셋을 캔버스 CSS 크기로 나누면 배율을 쓰지 않아도 된다. `Module.canvas`는 프레임워크가 쓰는 이름이다(릴리스 빌드에서 유지됨).
- 목록 화면: 격자는 위에서 850부터 3줄(230 × 3 + 16 × 2 = 722)이라 아래 끝이 위에서 1572다. `무한 모드` 버튼 위 끝은 아래에서 135다. 패널 높이가 1707 미만이면 겹친다(14% 축소인 1651에서 56단위). 잠김 안내 띠는 아래에서 145~255이고 `raycastTarget`이 꺼져 있다.
- 마지막 WebGL 빌드의 `ManagedStripped`에는 `AppsInToss*` · `Newtonsoft`가 없다. 게임 코드가 SDK C# API를 참조하면 SDK `link.xml`의 `preserve="all"`로 전체가 들어올 수 있다(`Newtonsoft.Json.dll`만 691,712바이트).
- 공식 Safe Area 문서: `✕` 위치는 X `right + 10`, Y는 iOS `top + 5` · Android `top + 10`. 크기는 적혀 있지 않다. 겹치면 검수 반려 가능. `top`은 상태바 여백으로 설명되고, 위 공식상 내비게이션 바 높이는 포함되지 않는 것으로 읽힌다.
- 캔버스는 기준 1080×1920 · `Expand`라 세로가 긴 폰에서는 캔버스가 1920보다 길다.

캡처로 확인(Galaxy S25 Edge · Android, 1440×3120):
- 게임 UI가 상태바 아래까지 올라가 있고, `Screen.safeArea`에 따른 여백이 없다.
- 게임 화면 높이는 약 2952px, `✕` 영역은 약 142~260px(약 119px 높이, 아래 끝이 화면 높이의 약 8.8%). 캔버스로 약 1080×2214이고 약 195 단위가 잘린다.
- 하단 버튼은 탐색 바에 가려지지 않는다. 웹뷰가 탐색 바 위에서 끝난다.

확인 못 함: 실제 `top` 값, 짧은 화면의 실제 배치. iPhone(노치 · Dynamic Island · 하단 인셋)은 지금 확인 수단이 없다(사용자 — 2026-11-11).

## 요구사항

1. 토스 안에서 타이틀 · 목록 · 보드 · 그림 · 무한 · 옵션의 제목과 버튼이 상태바 · `⋯ ✕`에 겹치지 않고 눌린다.
2. 값은 SDK가 주는 실제 안전영역으로 정한다(기기별 고정값 금지). 네 방향 모두 적용한다(iPhone 하단 포함).
3. 화면 크기가 바뀌면 다시 맞춘다.
4. SDK 호출이 실패하면 `Screen.safeArea`로 동작하고, 실패를 경고 로그로 드러낸다(성공처럼 처리하지 않음).
5. 토스 밖 WebGL · Android · 에디터는 지금과 같은 배치로 동작한다(3단계의 목록 화면 수치 변경은 예외로 모든 플랫폼에 적용).
6. WebGL 빌드 크기 변화를 재서 보고한다.

## 정한 사항 (검수 1라운드, 2026-11-11 사용자 선택)

- 안전영역 호출은 SDK C# API 대신 자체 jslib으로 한다. 빌드 크기 증가와 배율 경합을 함께 피한다.
- `✕` 높이 여유값은 `CloseBarHeight 44` + `Gap 8` CSS px다. 캡처 추정 높이는 약 34 CSS px(배율 3.5 가정)다.
- 토스 밖(Pages)에서는 호출이 실패하므로 경고 로그가 로드마다 1회 남는 것을 허용한다.
- iPhone은 이번 작업에서 확인하지 못한다. 하단 인셋과 노치는 "확인 못 함"으로 남기고 출시 전 실기 항목으로 둔다.

## 정한 사항 (검수 2라운드, 2026-11-11 사용자 선택)

- 목록 화면 겹침은 빌더 숫자만 고쳐 해결한다. 동적 배치 코드는 넣지 않는다. 대가로 모든 플랫폼의 목록 화면 그림이 조금 작아진다.
- 인셋은 배율을 곱하지 않고 캔버스 CSS 크기로 나눈다. 배율 조회와 배율 제한 시험 빌드를 뺀다.
- 4단계 모의 값에 좌우 인셋을 넣어 네 방향을 함께 확인한다.

## 수정 대상

기존 파일:
- `Assets/Scripts/Game/Screens/SafeAreaFitter.cs` — SDK 값 적용.
- `AgentScripts/Build/BoardSceneBuilder.cs` — 3단계의 목록 화면 수치. 다른 화면은 겹침이 나온 경우만 조정(`BuildPrefabs` → `BuildScene`).
- `Docs/TDD.md`(§5 · §6), `Docs/BuildHistory.md`, `Docs/Task.md`, `Docs/DevelopLog.md`, `Docs/DECISIONS.md`.

새 파일:
- `Assets/Plugins/WebGL/ColoringBootSafeArea.jslib`. `Assets/Plugins/`가 아직 없으므로 `Assets/Plugins.meta` · `Assets/Plugins/WebGL.meta` · jslib의 `.meta`를 함께 커밋한다(빠지면 다른 PC에서 GUID가 달라진다). 안전영역 호출은 이 컴포넌트 한 곳에서만 쓰므로 인터페이스를 만들지 않는다.

`ColoringBoot.Game.asmdef`는 바꾸지 않는다.

## 설계

`SafeAreaFitter`는 지금처럼 화면 크기 · `Screen.safeArea`가 바뀔 때만 동작한다. 먼저 `Screen.safeArea`를 적용하고, `#if UNITY_WEBGL && !UNITY_EDITOR`에서만 jslib 값을 읽어 덮어쓴다. 호출은 동기라 비동기 대기 · 시간 제한 · 요청 번호가 필요 없다. Android APK에는 이 호출 코드가 들어가지 않는다.

```text
// jslib — 예외 없이 값 또는 -1을 돌려준다
CbSafeAreaInset(side)   // 0 top · 1 bottom · 2 left · 3 right
    try { v = window.AppsInToss.SafeAreaInsets.get()[이름]; 유한한 숫자면 v, 아니면 -1 }
    catch { -1 }
CbCanvasCssSize(axis)   // 0 width · 1 height
    try { Module.canvas.clientWidth 또는 clientHeight } catch { -1 }

// SafeAreaFitter — 인셋이 하나라도 -1이거나 캔버스 크기가 0 이하면 경고 1회 후 Screen.safeArea 유지
topCss = top + CloseButtonOffset(10) + CloseBarHeight(44) + Gap(8)
anchorMax.y = 1 - topCss / cssHeight
anchorMin.y = bottom / cssHeight
anchorMin.x = left / cssWidth
anchorMax.x = 1 - right / cssWidth
```

- 오프셋은 iOS · Android 분기 없이 큰 쪽(10)을 쓴다. iOS에서 5 CSS px가 더 비는 대신 분기가 없다.
- `SafeAreaInsets.subscribe`는 쓰지 않는다. 세로 고정이고 크기가 바뀌면 다시 조회한다. 매 프레임 호출 · 할당은 없다.
- 경고는 플래그로 로드당 1회만 남긴다(`Debug.LogWarning(..., this)`).
- 코드 분기가 하나 늘어 `TDD.md`에 적는다. `UNITY_ANDROID` 두 곳 규칙(§8)은 그대로다.

## 구현 단계

### 1. 기준 상태 정리

- 목표: SDK 빌드 값을 지우고 3.9.0의 크기 기준을 만든다.
- 수정 대상: `ProjectSettings/ProjectSettings.asset`(되돌리기) · `Docs/BuildHistory.md`.
- 선행: 없음.
- 완료 기준: `git diff ProjectSettings`가 비고, 코드 변경 전 WebGL 빌드 크기를 기록했다. SDK 3.5.0(9,875,730바이트) 대비 차이를 업그레이드 몫으로 분리했다.
- 검증: `WebTemplate.Install` → `SetUseDefaultGraphicsAPIs(WebGL, true)` → WebGL 빌드 → 압축 바이트.

### 2. 안전영역 적용

- 목표: 요구사항 2 · 3 · 4 · 5의 코드.
- 수정 대상: 새 jslib · `SafeAreaFitter.cs`.
- 선행: 1.
- 완료 기준: 컴파일 경고 0(WebGL · Android 플레이어 스크립트 모두). EditMode 전체 통과. 에디터 플레이에서 앵커가 지금과 같다.
- 검증: `recompile` · `CompilePlayerScripts`(WebGL · Android — 새 호출 코드는 에디터 컴파일과 Android 컴파일에 포함되지 않으므로 WebGL을 빠뜨리지 않는다) · `run_tests` · 에디터 플레이 조회. 앵커 계산은 런타임 전용 분기라 EditMode 테스트 대상이 아니다 → 4단계 브라우저와 5단계 실기로 확인한다.

### 3. 줄어든 높이의 화면 배치

- 목표: UI가 내려가도 겹치거나 잘리는 화면이 없다.
- 수정 대상: `BoardSceneBuilder.cs`의 `SelectScreen` · `ChapterArrow`. 시작값은 작은 그림 480×600 → 416×520(위치 -210 유지), 챕터 넘기기 버튼 y -440 → -400(그림 세로 가운데 유지, 주석도 갱신), 격자 y -850 → -770이다. 1651 패널에서 격자 아래 끝은 위에서 1492, `무한 모드` 위 끝은 1516이라 24단위가 빈다. 다른 화면은 겹침이 나온 경우만 고친다.
- 선행: 2.
- 완료 기준: Game 뷰 1080×1920(가장 짧은 경우)에서 패널 위쪽을 14% 줄인 상태로 타이틀 · 목록(`무한 모드` 버튼을 켠 상태, 3줄인 챕터 1) · 보드(따라 하기 · 무한의 건너뛰기) · 그림 · 무한 · 옵션과 클리어 · 막힘 띠에 겹침 · 잘림이 없다. 옵션 패널이 위 띠에서 어색하지 않다. 잠김 안내 띠는 짧은 패널에서 격자 3번째 줄 일부를 덮는다(입력은 막지 않음) — 캡처로 보고 보고에 적는다. 줄이지 않은 1080×1920 패널에서 목록 화면 외 위치 변화가 없다.
- 검증: 에디터 플레이에서 `run_script`로 앵커를 강제하고 `capture_game_view`로 화면별 확인(화면으로 본 것으로 보고). `무한 모드` 버튼은 따라 하기를 본 상태에서만 켜지므로 그 상태로 맞추고, 끝나면 에디터 저장 키를 지운다. 빌더 수정 뒤 `BuildPrefabs` → `BuildScene` → `/qa-scene`.

### 4. 토스 밖 WebGL 회귀와 성공 경로

- 목표: Pages 빌드가 지금과 같게 동작하고, 실패 · 성공 경로와 크기 변화를 안다.
- 수정 대상: `Docs/BuildHistory.md`.
- 선행: 2 · 3.
- 완료 기준: 3단계의 목록 화면 수치 외 배치 불변, 경고는 로드당 1회뿐, 예외 없음. 모의 값을 넣고 창 크기를 바꾸면 패널이 계산대로 다시 맞춰진다. 1단계 대비 압축 바이트를 기록 · 보고했다.
- 사전 조건: Claude in Chrome 연결이 필요하다(헤드리스 Chrome은 이 PC에서 WebGL을 끝까지 못 돌린다). 연결이 없으면 사용자에게 요청하고, 이 단계를 통과로 보고하지 않는다. 시험 빌드를 GitHub Pages에 올린다면 `deploy-pages.sh`가 강제 push하므로 사용자 승인을 받은 뒤에 한다.
- 검증: WebGL 빌드 → 브라우저 확인(Chrome). 실패 경로를 본 뒤 `javascript_tool`로 `window.AppsInToss = {SafeAreaInsets:{get:()=>({top:30,bottom:20,left:12,right:16})}}`를 넣고 창 크기를 바꿔 캔버스 기준 위 92 · 아래 20 · 왼쪽 12 · 오른쪽 16 CSS px 여백을 화면으로 본다. 이 모의 값 확인은 계산 경로 확인이며 토스 실기를 대신하지 않는다.

### 5. 샌드박스 실기

- 목표: 요구사항 1 확정.
- 수정 대상: 임시 값 표시(화면 구석에 `top` · `bottom` · `left` · `right` · 캔버스 CSS 크기 · `Screen.height`를 이 순서로) — 폰트에 쓰는 글자만 들어 있어 영문은 □로 나올 수 있으므로 숫자와 구분 기호만 쓴다. 이 단계가 끝나면 제거.
- 선행: 4.
- 완료 기준: (사용자) Android 샌드박스에서 모든 화면의 상단 버튼이 눌리고 겹치지 않는다. 임시 표시 값이 "공식 위치 + 높이" 계산과 맞는다. `bottom`이 0이 아니면 하단 여백이 과하지 않은지 본다. 캔버스 CSS 높이가 뷰포트 높이와 같다. 임시 코드 제거 뒤 재빌드해 같은 결과.
- 검증: `AIT` 빌드 → 샌드박스(사용자) → 캡처. 뒤에 `ProjectSettings` 되돌리기. iPhone은 확인 못 함으로 보고한다.

### 6. 문서 정리

- 목표: 구현 지도 · 상태 · 결정 갱신.
- 수정 대상: `Docs/TDD.md` · `Docs/Task.md`(232 · 233행의 안전영역 항목, iPhone 실기는 출시 전 미확인 항목으로 남김) · `Docs/DevelopLog.md` · `Docs/DECISIONS.md`(검수 1 · 2라운드의 "정한 사항" 전부).
- 선행: 5.
- 완료 기준: 실측으로 확인한 항목만 체크했다.
- 검증: 문서 대조.

## 위험

- 회귀: 붓질 · 판정 · 솔버를 건드리지 않는다. 포도 · 프로토타입 · 콘텐츠 회귀 테스트는 2단계 전체 실행에 포함된다.
- 목록 화면: 3단계 수치는 모든 플랫폼에 적용되어 작은 그림이 약 13% 작아진다. 시작값은 계산으로 정했고 3단계 캡처로 확정한다.
- 저장 데이터: 변경 없음.
- 빌드 용량 · 첫 로딩: jslib은 크기 영향이 거의 없다(4단계에서 측정). 뒤에 광고 · 저장소를 SDK C# API로 붙이면 Newtonsoft 비용은 그때 생기므로 그 작업에서 재측정한다.
- SDK 의존: 자체 jslib은 `window.AppsInToss.SafeAreaInsets` 브리지 이름에 기댄다. SDK는 SHA로 고정되어 있고, 올릴 때 이름이 바뀌면 실패 경로와 경고로 드러난다. SDK 업그레이드 때 5단계와 같은 실기를 다시 한다.
- 변환 오차: 인셋과 캔버스 크기가 모두 CSS 픽셀이라 배율 제한 · 초기화 순서의 영향을 받지 않는다. 전제는 캔버스가 뷰포트를 덮는 것이고(`AITTemplate` CSS) 5단계 임시 표시로 확인한다. 틀리면 멈추고 보고한다.
- 초기화 순서: `window.AppsInToss`가 Unity 첫 호출보다 늦게 준비되면 실패 경로를 탄다. 5단계에서 겹침으로 드러나며, 그 경우 재시도 방식을 사용자와 정한다.
- iPhone: 노치 · 하단 인셋의 실제 동작을 이번에 보지 못한다. Android 샌드박스 통과를 iPhone 통과로 보고하지 않는다.
- SDK 범위: `%AIT_LOADING_SCREEN%` 미치환 경고가 AIT 빌드 로그에 있다(3.9.0). 이 계획 범위 밖이지만 로딩 화면이 우리 디자인으로 나오는지 5단계에서 함께 본다.
- WebGL 제약: 스레드 · 파일 시스템을 쓰지 않는다. jslib 호출은 메인 스레드에서 동기로 끝난다.

## 후보 기획(GDD §7)을 불가능하게 만드는지

아니다. 표현 계층의 패널 위치만 바꾼다.
