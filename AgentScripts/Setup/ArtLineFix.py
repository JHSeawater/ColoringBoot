# (기록용 — 2026-11-18 한 번 실행, 다시 실행하지 않는다) 챕터 4 완성 그림의 끊긴 선 보정
# python AgentScripts/Setup/ArtLineFix.py [--apply]  — --apply 없이는 Builds/ArtIssue/Chapter4_fixed.png만 쓴다(원본은 그대로)
# 고치기 전 원본은 git 커밋 5cd1b35의 ArtSource/Chapter4/_source/original.png
#
# 왜: ArtSplit은 선으로 닫힌 영역 하나를 한 단계에 넣는다. 원본에서 선이 닫히지 않은 곳은 두 소재가 한 영역이 되어 같은 단계에서 칠해졌다(사용자 플레이 의견 2026-11-18).
# 방법: 끊긴 선의 끝을 원본과 같은 색 · 굵기의 선분으로 잇는다.
# 챕터 2는 같은 날 사용자가 새 그림으로 바꿨다(ArtSource/Chapter2/_source/original.png) — 여기서 고치지 않는다
import os
import sys

import cv2
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
LINE_L = 68   # split.json의 lineL


class Picture:
    def __init__(self, chapter):
        self.chapter = chapter
        self.path = os.path.join(ROOT, 'ArtSource', f'Chapter{chapter}', '_source', 'original.png')
        self.rgb = np.array(Image.open(self.path).convert('RGB'))
        light = cv2.cvtColor(self.rgb, cv2.COLOR_RGB2LAB)[..., 0].astype(np.float32)
        self.color = np.median(self.rgb[light < LINE_L * 0.75], axis=0).astype(np.uint8)   # 선 색(ArtSplit과 같은 방법)
        width = cv2.distanceTransform((light < LINE_L).astype(np.uint8), cv2.DIST_L2, 5)
        ridge = (width == cv2.dilate(width, np.ones((3, 3)))) & (width > 2)
        self.half = float(np.median(width[ridge]))                                         # 선 굵기의 절반

    def stroke(self, x0, y0, x1, y1, label=''):
        # (x0, y0) → (x1, y1) 선분을 원본과 같은 색 · 굵기(끝은 둥글게)로 긋는다
        ys, xs = np.mgrid[0:self.rgb.shape[0], 0:self.rgb.shape[1]].astype(np.float32)
        dx, dy = x1 - x0, y1 - y0
        t = np.clip(((xs - x0) * dx + (ys - y0) * dy) / max(dx * dx + dy * dy, 1e-6), 0, 1)
        dist = np.hypot(xs - (x0 + t * dx), ys - (y0 + t * dy))
        a = np.clip(self.half + 0.5 - dist, 0, 1)[..., None]
        self.rgb = np.round(self.rgb * (1 - a) + np.float32(self.color) * a).astype(np.uint8)
        print(f'챕터 {self.chapter} 선 — {label}: ({x0}, {y0}) → ({x1}, {y1})')

    def save(self, apply):
        out = self.path if apply else os.path.join(ROOT, 'Builds', 'ArtIssue', f'Chapter{self.chapter}_fixed.png')
        os.makedirs(os.path.dirname(out), exist_ok=True)
        Image.fromarray(self.rgb).save(out, optimize=True)
        print('→', out)


def main(apply):
    # 챕터 4 가을 숲길 — 왼쪽 나무의 빨간 단풍 덩어리: 아래 윤곽선의 끝(319, 286)이 가지 윤곽선에 닿지 않아 노을과 한 영역이었다 → 잇는다
    p = Picture(4)
    p.stroke(320, 289, 311, 277, label='빨간 단풍 아래 윤곽선을 가지에 잇기')
    p.save(apply)


if __name__ == '__main__':
    main('--apply' in sys.argv)
