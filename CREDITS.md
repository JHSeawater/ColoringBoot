# 출처 · 라이선스

게임에 들어간 외부 자료와 그 라이선스입니다. 자료를 더하거나 바꾸면 이 파일도 함께 고칩니다.

## 폰트

| 자료 | 출처 | 라이선스 | 위치 |
|---|---|---|---|
| Pretendard 1.3.9 SemiBold | Kil Hyung-jin, https://github.com/orioncactus/pretendard | SIL Open Font License 1.1 (전문은 `Assets/Art/Fonts/Pretendard-LICENSE.txt`) | `Assets/Art/Fonts/` — 게임에 쓰는 글자만 TextMesh Pro 아틀라스로 굽는다 |

## 효과음

대부분 Pixabay의 로열티 프리 효과음이고, 나머지는 출처 표기 의무가 없는 저작권 없는 음원이다(개발자 확인, 2026-11-18). 파일별 원본 페이지는 기록하지 않았다. 음원 파일 단독 재배포를 피하려고 파일은 이 저장소에 넣지 않았고(2026-11-09), 빌드에만 들어간다.

| 파일(`Assets/Sound/Effect/`) | 쓰는 곳 |
|---|---|
| `move-1.mp3` ~ `move-8.mp3` | 획 소리(최소 수에 맞춰 차례로) |
| `pop.ogg` · `pop1.wav` · `pop4.wav` · `pop5.wav` · `pop6.wav` · `pop7.wav` | 칸이 칠해질 때 · 클리어 연출 |
| `ui_button.wav` | 버튼 |
| `wrong.mp3` | 막힘 |

## 그림

| 자료 | 출처 | 위치 |
|---|---|---|
| 챕터 1~5 그림 | AI(ChatGPT)로 만든 그림을 개발자가 수정 · 가공(개발자 확인, 2026-11-18) | 원본 `ArtSource/ChapterN/` · 게임용 `Assets/Art/Chapters/` |

## 엔진 · 패키지

Unity 6.6(URP · TextMesh Pro · Input System)과 앱인토스 Unity SDK를 쓴다. 각 패키지의 라이선스는 해당 패키지를 따른다.
