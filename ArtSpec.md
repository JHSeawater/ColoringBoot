# 챕터 그림 전달 규격 (ColoringBoot · 챕터 1)

> 버전 0.2 (2026-10-03 — 세계관 · 아트 스타일 추가) · 0.1 초안 (2026-10-02). 정본은 이 파일이고, 아트 담당용 공유 페이지는 같은 내용이다.
> 이 문서는 사람(아트 담당)과 AI(이미지 생성 AI · 코드를 쓰는 AI) 모두에게 그대로 줄 수 있게 썼다. AI에게 줄 때는 문서 전체를 붙여 넣고 "§7의 방법 ○로 만들어 줘"처럼 요청한다.

---

## 1. 이 그림이 게임에서 하는 일

- 게임은 육각 칸을 붓으로 칠하는 모바일 웹 퍼즐이다. 챕터마다 컬러링북 도안 한 장을 완성한다.
- 세계관은 그림책 풍경이다. 챕터마다 장소와 계절이 있는 한 장면(예: 포도밭 오후, 바닷가 아침, 눈 오는 마을)을 그린다. 이야기나 인물 중심 장면은 없다.
- 처음에는 **선화만** 보인다. 스테이지를 하나 클리어할 때마다 그 스테이지가 맡은 **소재 하나**(예: 포도)가 그림 전체에서 **한꺼번에** 칠해진다. 나무에 달린 포도와 바구니 속 포도가 함께 칠해지는 식이다.
- 마지막 스테이지를 클리어하면 그림이 완성된다.
- 퍼즐 모양과 그림 모양은 상관없다. 스테이지와 그림은 소재로만 이어진다. 그림의 색도 퍼즐 색과 맞출 필요가 없다.

## 2. 받을 파일

| 파일 | 내용 | 필수 |
|---|---|---|
| `00_line.png` | 선화. 선만 있고 나머지는 투명 | 필수 |
| `01_<소재>.png` … `NN_<소재>.png` | 색칠 단계 레이어. 그 소재의 색 영역만 있고 나머지는 투명. 번호 = 칠해지는 순서 | 필수 |
| `steps.md` | 단계 목록 표(§5 양식) | 필수 |
| `reference.png` | 완성 그림(모든 단계 + 선화를 합친 모습) | 권장 |
| 원본(PSD · SVG 등) | 나중에 고칠 때 쓰는 원본. 게임에는 넣지 않는다 | 있으면 |

- `<소재>`는 영어 소문자 · 숫자 · 하이픈만 쓴다. 예: `04_grape.png`, `11_sky.png`
- 번호는 두 자리(`01`~)이고 빠지는 번호가 없다.

## 3. 공통 형식

| 항목 | 값 |
|---|---|
| 스타일 | **굵은 선 플랫 카툰** — 진한 갈색 · 검정의 굵은 외곽선, 선 안은 단색(음영 1~2단계까지). 예: `ArtSource/chapter1_test/reference.png` |
| 캔버스 | **2160 × 2700 px** (세로 4:5). 모든 PNG가 정확히 같은 크기 |
| 게임에서 쓰는 크기 | 절반(1080 × 1350). 작은 미리보기는 폭 약 400 px |
| 형식 | PNG · RGBA 8비트 · sRGB |
| 바탕 | 투명. 흰 바탕을 깔지 않는다(종이색은 게임이 그린다) |
| 단계 수 | **10~12개**. 지금 챕터 1 스테이지는 11개 — 그림에 맞춰 스테이지 수를 조정할 수 있으니 그림을 우선한다 |

## 4. 레이어 규칙

### 선화 (`00_line.png`)
- 선 색은 진한 한 가지 색(검정 · 진한 갈색 등). 선 밖은 완전 투명.
- 선 두께 **8 px 이상, 권장 10~14 px**(게임 크기에서 그 절반). 작은 미리보기에서도 형태가 읽혀야 한다.
- 영역은 선으로 닫혀 있게 그린다. 선화만 봐도 무엇을 그린 그림인지 알 수 있어야 한다(색칠 전 화면이 선화뿐이다).
- 선화는 항상 모든 색 위에 그려진다.

### 색칠 단계 (`01_…` ~ `NN_…`)
- 한 레이어 = 한 소재의 **모든** 영역. 같은 소재가 그림 여러 곳에 있으면 전부 이 레이어에 넣는다.
- 영역 안쪽은 불투명(알파 255). 영역 밖은 완전 투명.
- **레이어끼리 겹치지 않는다.** 한 영역은 한 단계에서만 칠해진다. 예외로 경계는 선 아래로 선 두께의 절반까지 넘어가도 된다(선과 색 사이 틈을 막기 위함).
- 어느 레이어에도 속하지 않는 곳은 끝까지 종이색으로 남는다. 하늘 · 땅 같은 배경까지 칠하고 싶으면 배경도 하나의 단계로 만든다.
- 흰색 · 종이색에 가까운 색은 칠해져도 달라진 것이 보이지 않는다. 흰 구름 · 눈처럼 원래 흰 소재도 옅은 색(연한 회청색 등)을 넣는다.
- 색은 자유. 단색 + 음영 1~2단계를 권한다. 거친 질감 · 사진 같은 그라데이션은 피한다(용량이 커지고 컬러링북 느낌이 약해진다).
- 아주 작은 조각(게임 크기에서 몇 px)은 미리보기에서 사라진다. 소재는 작게 봐도 알아볼 수 있는 크기로.

### 칠하는 순서 (번호)
- 1번부터 차례로 칠해진다(스테이지 순서와 같다).
- 권장: 앞쪽은 작고 단순한 소재, 마지막은 그림을 완성하는 느낌이 큰 소재(주인공 소재나 넓은 배경). 중간 단계에서도 그림이 어색하지 않게.

## 5. 단계 목록 양식 (`steps.md`)

```markdown
| 번호 | 파일 | 소재(한글) | 그림 속 위치 · 메모 |
|---|---|---|---|
| 01 | 01_sun.png | 해 | 오른쪽 위 |
| 02 | 02_grape.png | 포도 | 덩굴 3송이 + 바구니 속 포도 |
| … | … | … | … |
```
(위 행은 예시다. 실제 소재는 그림에 맞춰 정한다.)

## 6. 검수 체크리스트

아트 담당과 게임 쪽 빌더가 같은 기준으로 확인한다. AI에게 파일 점검을 맡길 때도 이 목록을 쓴다.

- [ ] 모든 PNG가 2160 × 2700이다
- [ ] `00_line.png`와 `01`부터 빈 번호 없이 이어진 단계 파일이 있고, 단계 수가 `steps.md`의 행 수와 같다
- [ ] 파일 이름이 `NN_영어소문자.png` 형식이다
- [ ] 각 레이어에 투명이 아닌 픽셀이 있다(빈 레이어 없음)
- [ ] 선 밖 · 영역 밖이 완전 투명이다(흰 바탕 없음)
- [ ] 두 단계 레이어가 겹치는 곳은 선 아래(경계)뿐이다
- [ ] 선화만 봐도 그림이 읽히고, 폭 400 px로 줄여도 소재를 알아볼 수 있다
- [ ] `reference.png`가 "단계 전부 + 선화"를 겹친 결과와 같다

## 7. AI로 만들 때

이미지 생성 AI는 레이어 · 투명 배경을 안정적으로 만들지 못하고, 다시 생성하면 구도가 바뀐다. 그래서 **한 장을 원본으로 정하고 나눈다**는 원칙을 지킨다.

### 방법 A — 이미지 생성 AI로 완성 그림 → 나누기 (그림 품질 우선)
1. 아래 프롬프트로 **완성된 컬러 그림** 한 장을 만든다(굵은 외곽선 · 평면 채색).
2. 그 그림에서 선을 뽑아 `00_line.png`를 만든다(편집 도구의 색 범위 선택 · 임계값으로 진한 선만 남기기). 선화를 따로 다시 생성하지 않는다(구도가 어긋난다).
3. 소재마다 영역을 선택해(자동 선택 · 영역 채우기 선택) 단계 레이어로 나눠 내보낸다.
4. §6 체크리스트로 점검한다.

프롬프트 틀 (영어가 결과가 안정적이다):
```text
A children's coloring book illustration, portrait 4:5, of {THEME}.
Bold clean dark outlines (uniform thick line weight), every shape fully enclosed by outlines.
Flat colors with at most one soft shade per color, no texture, no gradients, no text.
Clearly separated subjects that each appear in several places: {SUBJECTS}.
Simple readable shapes that remain recognizable when shown small. Plain background.
```
- `{THEME}`: 장면 한 줄. 예: `a sunny vineyard afternoon with a beehive and a small path`
- `{SUBJECTS}`: 단계 소재 목록. 예: `grapes, bees, beehive, path, butterflies, leaves, sun, hills, sky`

### 방법 B — 코드를 쓰는 AI로 SVG → PNG (레이어 정확성 우선)
AI에게 SVG 코드를 쓰게 하면 레이어가 정확히 나뉜다. 그림은 단순한 도형 위주가 된다(임시 도안에도 적합).

AI에게 줄 지시:
```text
Write one SVG file, viewBox="0 0 2160 2700", no background rectangle.
Structure:
  <g id="line">  — all outlines only: stroke dark (#2B2622), stroke-width 12, fill="none", round joins.
  <g id="step-01-{subject}"> … <g id="step-NN-{subject}"> — filled shapes only (no stroke), one group per subject,
     containing EVERY region of that subject in the picture.
Rules: groups must not overlap except within 6px under outlines — background groups (sky, ground) must leave holes
where other subjects are (use a <mask> or fill-rule="evenodd"), never a full rectangle behind them; every region is closed by an outline;
flat colors (max 2 tones per subject); no text, no images, no filters, no gradients.
Draw the line group last (on top). Subjects in order: {SUBJECTS_IN_ORDER}. Theme: {THEME}.
```
하늘처럼 뒤에 깔리는 배경을 큰 사각형 하나로 그리면 앞 소재들과 겹쳐 규칙에 어긋난다(§6 겹침 항목). 배경에는 앞 소재 자리를 비우라고 꼭 지시한다.
그다음 그룹마다 하나씩만 보이게 해서 2160 × 2700 PNG로 내보낸다(Inkscape · 브라우저 등). `line` → `00_line.png`, `step-01-sun` → `01_sun.png`.

### AI에게 점검을 맡길 때
"§6 체크리스트로 이 파일들을 점검하고, 걸린 항목과 파일 이름을 알려 줘"라고 요청한다. 픽셀 단위 점검(크기 · 투명 · 겹침)은 코드를 실행할 수 있는 AI가 정확하다.

## 8. 게임 쪽에서 하는 일 (아트 담당이 할 필요 없음)

- 받은 파일은 점검 스크립트(`AgentScripts/ArtCheck.py`)로 §6의 크기 · 이름 · 번호 · 빈 레이어 · 바탕 · 겹침 · reference 항목을 자동으로 확인한다. 걸리면 위치(픽셀 좌표)와 함께 알려 드린다.
- 레이어마다 빈 여백을 잘라 내고, 여러 레이어를 한 장으로 묶고, 게임 크기로 줄인다(빌더).
- 색칠 연출(서서히 나타남 · 완성 강조)과 종이색 바탕은 게임이 그린다.
- 단계 ↔ 스테이지 연결은 게임 데이터에서 정한다. 그림의 단계 수에 맞춰 스테이지를 더하거나 뺄 수 있다.
- 그림 추가분은 압축 후 1.5 MB 이하를 목표로 한다(모바일 첫 로딩 10초 기준). 평면 채색이면 충분히 들어간다.
