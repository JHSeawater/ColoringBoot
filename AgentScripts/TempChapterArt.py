# 임시 챕터 1 도안 (Phase 5.2) — ArtSpec.md 규격의 파일을 만든다(진짜 그림이 오면 교체): python AgentScripts/TempChapterArt.py
# 라벨 지도(픽셀마다 "단계 * 10 + 색 번호")를 뒤에서 앞으로 칠한다 → 레이어가 겹치지 않고, 선화는 라벨 경계에서 뽑아 모든 영역이 닫힌다
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

W, H = 2160, 2700          # ArtSpec §3 캔버스
S = 2                      # 선을 매끄럽게 하려고 두 배로 그린 뒤 줄인다
LINE_RGB = (43, 38, 34)
LINE_WIDTH = 12            # ArtSpec §4 권장 10~14 px
BLEED = LINE_WIDTH // 2    # 색 영역이 선 아래로 들어가는 폭 (ArtSpec §4)

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
OUT = os.path.join(ROOT, 'ArtSource', 'Chapter1')
PREVIEW = os.path.join(ROOT, 'Builds', 'ChapterArtPreview.png')

# (번호, 파일 소재, 한글, 메모) — 번호 = 칠하는 순서 = 스테이지 순서
STEPS = [
    (1, 'sun', '해', '오른쪽 위, 햇살은 선만'),
    (2, 'cloud', '구름', '하늘에 두 덩이'),
    (3, 'bee', '벌', '벌집 둘레 네 마리(몸통 · 날개)'),
    (4, 'grape', '포도', '덩굴에 달린 세 송이 + 땅에 딴 포도 한 무더기'),
    (5, 'hill', '언덕', '뒤 언덕 둘 + 앞 땅'),
    (6, 'path', '길', '가운데로 굽어 올라가는 보랏빛 길'),
    (7, 'vine', '포도 덩굴', '덩굴 시렁 기둥 · 들보 + 잎'),
    (8, 'fence', '울타리', '왼쪽 울타리 · 엇갈린 버팀목 · 벌집 받침'),
    (9, 'butterfly', '나비', '검은 날개 나비 세 마리(분홍 점)'),
    (10, 'beehive', '벌집', '받침 위 큰 벌집'),
    (11, 'sky', '하늘', '마지막에 배경 전체'),
]

COLORS = {
    10: (246, 199, 68),
    20: (247, 248, 251),
    30: (245, 197, 24), 31: (221, 239, 252),
    40: (123, 79, 184),
    50: (156, 210, 124), 51: (127, 191, 98),
    60: (183, 155, 224),
    70: (122, 82, 54), 71: (95, 163, 90),
    80: (200, 154, 106),
    90: (46, 42, 51), 91: (242, 167, 195),
    100: (232, 176, 74), 101: (90, 59, 30),
    110: (191, 227, 245),
}


def sc(points):
    return [(x * S, y * S) for x, y in points]


class Painter:
    def __init__(self):
        self.labels = Image.new('L', (W * S, H * S), 0)
        self.d = ImageDraw.Draw(self.labels)
        self.lines = Image.new('L', (W * S, H * S), 0)   # 경계 말고 따로 긋는 선(줄무늬 · 잎맥 등)
        self.ld = ImageDraw.Draw(self.lines)

    def ellipse(self, cx, cy, rx, ry, v):
        self.d.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S], fill=v)

    def rect(self, x0, y0, x1, y1, v, radius=0):
        self.d.rounded_rectangle([x0 * S, y0 * S, x1 * S, y1 * S], radius=radius * S, fill=v)

    def poly(self, points, v):
        self.d.polygon(sc(points), fill=v)

    def bar(self, x0, y0, x1, y1, width, v):
        # 두께 있는 막대(버팀목)
        dx, dy = x1 - x0, y1 - y0
        n = math.hypot(dx, dy)
        ox, oy = -dy / n * width / 2, dx / n * width / 2
        self.poly([(x0 + ox, y0 + oy), (x1 + ox, y1 + oy), (x1 - ox, y1 - oy), (x0 - ox, y0 - oy)], v)

    def line(self, points, width=LINE_WIDTH):
        self.ld.line(sc(points), fill=255, width=width * S, joint='curve')
        for x, y in (points[0], points[-1]):
            r = width / 2
            self.ld.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=255)

    def dot(self, cx, cy, rx, ry):
        self.ld.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S], fill=255)


def leaf(p, cx, cy, length, width, angle):
    a = math.radians(angle)
    ca, sa = math.cos(a), math.sin(a)
    pts = []
    for i in range(25):
        t = i / 24
        pts.append((t - 0.5, 0.5 * math.sin(math.pi * t)))
    for i in range(24, -1, -1):
        t = i / 24
        pts.append((t - 0.5, -0.5 * math.sin(math.pi * t)))
    world = [(cx + (u * length) * ca - (v * width) * sa, cy + (u * length) * sa + (v * width) * ca) for u, v in pts]
    p.poly(world, 71)
    p.line([(cx - 0.4 * length * ca, cy - 0.4 * length * sa), (cx + 0.4 * length * ca, cy + 0.4 * length * sa)], 8)


def draw(p):
    p.rect(0, 0, W, H, 110)                                    # 11 하늘
    for i in range(12):                                         # 1 해 + 햇살(선)
        a = math.radians(i * 30 + 15)
        p.line([(1760 + 250 * math.cos(a), 430 + 250 * math.sin(a)), (1760 + 330 * math.cos(a), 430 + 330 * math.sin(a))])
    p.ellipse(1760, 430, 200, 200, 10)
    for cx, cy, s in ((560, 420, 1.0), (1250, 720, 0.8)):      # 2 구름
        p.ellipse(cx, cy + 70 * s, 270 * s, 80 * s, 20)
        for dx, dy, r in ((-150, 10, 110), (0, -40, 150), (150, 10, 115)):
            p.ellipse(cx + dx * s, cy + dy * s, r * s, r * s, 20)

    p.ellipse(650, 1900, 1250, 520, 50)                         # 5 언덕
    p.ellipse(1850, 2000, 1000, 480, 50)
    ground = [(x, 2160 + 60 * math.sin(x / 380)) for x in range(0, W + 1, 40)]
    p.poly(ground + [(W, H), (0, H)], 51)

    left, right = [], []                                        # 6 길
    for i in range(41):
        t = i / 40
        y = 1560 + t * (H - 1560 + 20)
        half = 30 + 300 * t ** 1.6
        cx = 1210 - 190 * math.sin(t * 3.0)
        left.append((cx - half, y))
        right.append((cx + half, y))
    p.poly(left + right[::-1], 60)

    for x in (1380, 2040):                                      # 7 포도 덩굴: 기둥 · 들보 · 잎
        p.rect(x, 1480, x + 56, 2330, 70, 10)
    p.rect(1290, 1440, W + 20, 1510, 70, 16)
    for i, x in enumerate(range(1340, W + 60, 120)):
        leaf(p, x, 1435 + (25 if i % 2 else -25), 170, 85, -25 if i % 2 else 20)

    # 알 반지름 42 > 간격 72 / √3 → 알 사이 틈이 없다(틈이 남으면 선화에 작은 점이 생긴다)
    for cx in (1520, 1770, 2020):                               # 4 포도: 송이
        p.line([(cx, 1500), (cx, 1590)], 10)
        for row, count in enumerate((3, 3, 2, 1)):
            for k in range(count):
                p.ellipse(cx + (k - (count - 1) / 2) * 72, 1610 + row * 62, 42, 42, 40)
    for row, count in enumerate((1, 2, 3)):                     #   딴 포도 무더기
        for k in range(count):
            p.ellipse(560 + (k - (count - 1) / 2) * 72, 2390 + row * 62, 42, 42, 40)

    posts = range(60, 940, 210)                                 # 8 울타리 · 버팀목 · 벌집 받침
    for x in posts:
        p.rect(x, 1950, x + 56, 2250, 80, 12)
    for y in (2005, 2135):
        p.rect(30, y, 980, y + 44, 80, 10)
    for x in list(posts)[:-1]:
        p.bar(x + 60, 2060, x + 206, 2140, 30, 80)
    p.rect(270, 1720, 400, 1960, 80, 10)

    bands = ((200, 300), (170, 340), (140, 320), (110, 260), (80, 180))  # 10 벌집 (밑에서부터 띠 높이 · 폭)
    y = 1725
    edges = []
    for h, w in bands:
        p.rect(335 - w / 2, y - h - 6, 335 + w / 2, y, 100, 40)
        edges.append((y - h, w))
        y -= h
    p.ellipse(335, 1660, 42, 30, 101)
    for ey, w in edges[:-1]:
        p.line([(335 - w / 2 + 35, ey), (335 + w / 2 - 35, ey)], 10)

    for cx, cy in ((600, 1560), (150, 1470), (760, 1800), (980, 1400)):  # 3 벌
        p.ellipse(cx - 22, cy - 46, 36, 28, 31)
        p.ellipse(cx + 26, cy - 50, 36, 28, 31)
        p.ellipse(cx, cy, 62, 38, 30)
        for dx in (-14, 18):
            p.line([(cx + dx, cy - 34), (cx + dx, cy + 34)], 10)

    for cx, cy, s in ((1000, 1150, 1.0), (1600, 960, 0.85), (1240, 1330, 0.7)):  # 9 나비
        for sx in (-1, 1):
            p.ellipse(cx + sx * 72 * s, cy - 42 * s, 76 * s, 62 * s, 90)
            p.ellipse(cx + sx * 52 * s, cy + 50 * s, 50 * s, 44 * s, 90)
            p.ellipse(cx + sx * 80 * s, cy - 48 * s, 20 * s, 20 * s, 91)
        p.dot(cx, cy, 16 * s, 70 * s)
        for sx in (-1, 1):
            p.line([(cx, cy - 60 * s), (cx + sx * 40 * s, cy - 120 * s)], 8)


def dilate(mask, radius):
    img = Image.fromarray(mask.astype(np.uint8) * 255)
    for _ in range(radius):
        img = img.filter(ImageFilter.MaxFilter(3))
    return np.array(img) > 0


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    os.makedirs(OUT, exist_ok=True)
    p = Painter()
    draw(p)

    lab2 = np.array(p.labels)
    edge = np.zeros_like(lab2, dtype=bool)
    edge[:, :-1] |= lab2[:, :-1] != lab2[:, 1:]
    edge[:-1, :] |= lab2[:-1, :] != lab2[1:, :]
    lines2 = dilate(edge, LINE_WIDTH - 1) | (np.array(p.lines) > 0)
    line_alpha = np.array(Image.fromarray(lines2.astype(np.uint8) * 255).resize((W, H), Image.BOX))

    line_rgba = np.zeros((H, W, 4), np.uint8)
    line_rgba[..., :3] = LINE_RGB
    line_rgba[..., 3] = line_alpha
    Image.fromarray(line_rgba).save(os.path.join(OUT, '00_line.png'), optimize=True)

    lab1 = lab2[::S, ::S]
    under_line = line_alpha > 0
    layers = []
    for number, name, _, _ in STEPS:
        own = (lab1 // 10) == number
        bleed = dilate(own, BLEED) & under_line & ~own
        rgba = np.zeros((H, W, 4), np.uint8)
        for value, rgb in COLORS.items():
            if value // 10 == number:
                rgba[lab1 == value, :3] = rgb
        rgba[bleed, :3] = COLORS[number * 10]
        rgba[own | bleed, 3] = 255
        Image.fromarray(rgba).save(os.path.join(OUT, f'{number:02d}_{name}.png'), optimize=True)
        layers.append(rgba)

    def composite(count, paper):
        canvas = Image.new('RGBA', (W, H), paper)
        for rgba in layers[:count]:
            canvas.alpha_composite(Image.fromarray(rgba))
        canvas.alpha_composite(Image.fromarray(line_rgba))
        return canvas

    composite(len(layers), (0, 0, 0, 0)).save(os.path.join(OUT, 'reference.png'), optimize=True)

    with open(os.path.join(OUT, 'steps.md'), 'w', encoding='utf-8', newline='\n') as f:
        f.write('| 번호 | 파일 | 소재(한글) | 그림 속 위치 · 메모 |\n|---|---|---|---|\n')
        for number, name, korean, memo in STEPS:
            f.write(f'| {number:02d} | {number:02d}_{name}.png | {korean} | {memo} |\n')

    # 확인용 미리보기(git 제외 Builds/): 선화만 → 4단계 → 8단계 → 완성
    tw, th = 540, 675
    strip = Image.new('RGB', (tw * 4 + 60, th + 40), (230, 233, 230))
    for i, count in enumerate((0, 4, 8, len(layers))):
        strip.paste(composite(count, (251, 250, 247, 255)).convert('RGB').resize((tw, th), Image.LANCZOS), (20 + i * (tw + 7), 20))
    os.makedirs(os.path.dirname(PREVIEW), exist_ok=True)
    strip.save(PREVIEW)
    print(f'{len(layers)}단계 + 선화 → {os.path.normpath(OUT)}')


if __name__ == '__main__':
    main()
