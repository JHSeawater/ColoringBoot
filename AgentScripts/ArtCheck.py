# 챕터 그림 파일 점검 (ArtSpec.md §6 중 코드로 볼 수 있는 항목) — python AgentScripts/ArtCheck.py ArtSource/Chapter1
# 읽기 전용. 실패 항목이 있으면 종료 코드 1
import os
import re
import sys

import numpy as np
from PIL import Image

W, H = 2160, 2700
NAME = re.compile(r'^(\d{2})_([a-z0-9-]+)\.png$')


def main(folder):
    sys.stdout.reconfigure(encoding='utf-8')
    fails = []
    files = sorted(f for f in os.listdir(folder) if f.endswith('.png') and f != 'reference.png')
    named = [NAME.match(f) for f in files]
    for f, m in zip(files, named):
        if not m:
            fails.append(f'파일 이름 형식 아님: {f}')
    numbers = [int(m.group(1)) for m in named if m]
    if 0 not in numbers or [m.group(2) for m in named if m and int(m.group(1)) == 0] != ['line']:
        fails.append('00_line.png 없음')
    steps = sorted(n for n in numbers if n > 0)
    if steps != list(range(1, len(steps) + 1)):
        fails.append(f'단계 번호가 1부터 빈 번호 없이 이어지지 않음: {steps}')
    if len(set(numbers)) != len(numbers):
        fails.append('같은 번호의 파일이 둘 이상')

    rows = 0
    steps_md = os.path.join(folder, 'steps.md')
    if os.path.exists(steps_md):
        rows = sum(1 for line in open(steps_md, encoding='utf-8') if re.match(r'^\|\s*\d', line))
        if rows != len(steps):
            fails.append(f'steps.md 행 {rows}개 ≠ 단계 파일 {len(steps)}개')
    else:
        fails.append('steps.md 없음')

    alpha = {}
    rgba = {}
    for f, m in zip(files, named):
        im = Image.open(os.path.join(folder, f))
        if im.size != (W, H):
            fails.append(f'{f}: 크기 {im.size[0]}×{im.size[1]} (기대 {W}×{H})')
            continue
        if im.mode != 'RGBA':
            fails.append(f'{f}: {im.mode} — 투명 채널(RGBA)이 아님')
            continue
        a = np.array(im)
        if m:
            alpha[int(m.group(1))] = a[..., 3]
            rgba[int(m.group(1))] = a
        if not (a[..., 3] > 0).any():
            fails.append(f'{f}: 빈 레이어')
        corners = [a[0, 0, 3], a[0, -1, 3], a[-1, 0, 3], a[-1, -1, 3]]
        if m and int(m.group(1)) == 0 and min(corners) > 0:
            fails.append(f'{f}: 네 귀퉁이가 모두 불투명 — 바탕을 깐 것 같음')

    coverage = []
    if 0 in alpha and steps and all(n in alpha for n in steps):
        under_line = alpha[0] > 0
        count = np.zeros((H, W), np.uint8)
        for n in steps:
            count += (alpha[n] > 0).astype(np.uint8)
            coverage.append(f'{n:02d} {100 * (alpha[n] > 0).mean():.1f}%')
        overlap = (count > 1) & ~under_line
        if overlap.any():
            ys, xs = np.nonzero(overlap)
            fails.append(f'선 밖에서 레이어가 겹침: {overlap.sum()}픽셀 (예: x {xs[0]}, y {ys[0]})')
        paper = ((count == 0) & ~under_line).mean()

        reference = os.path.join(folder, 'reference.png')
        if os.path.exists(reference):
            canvas = Image.new('RGBA', (W, H), (0, 0, 0, 0))
            for n in steps + [0]:
                canvas.alpha_composite(Image.fromarray(rgba[n]))
            ref = np.array(Image.open(reference).convert('RGBA')).astype(np.int16)
            if ref.shape == (H, W, 4):
                diff = np.abs(ref - np.array(canvas).astype(np.int16)).max(axis=2)
                if (diff > 8).mean() > 0.001:
                    fails.append(f'reference.png가 단계 + 선화 합성과 다름({100 * (diff > 8).mean():.2f}% 픽셀)')
            else:
                fails.append('reference.png 크기가 다름')
        print(f'단계 {len(steps)}개 · 칠해지는 면적 {", ".join(coverage)} · 끝까지 종이색으로 남는 곳 {100 * paper:.1f}%')

    if fails:
        print('실패:')
        for f in fails:
            print(' - ' + f)
        sys.exit(1)
    print('통과 (사람이 볼 항목: 선화만으로 그림이 읽히는지 · 폭 400 px에서 소재를 알아보는지)')


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else 'ArtSource/chapter1_test')
