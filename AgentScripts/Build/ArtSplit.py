# 완성 그림 한 장 → 챕터 그림 레이어 (ArtSpec §7 방법 A) — 선화 00_line + 단계 NN_<소재> + reference.png + steps.md
# python AgentScripts/Build/ArtSplit.py ArtSource/ChapterN  — 원본과 설정은 ArtSource/ChapterN/_source/(original.png · split.json)
# 다음은 ArtCheck.py → ChapterArtExport.py → ChapterArtBuilder.Build. 결과를 손으로 고치지 않는다 — split.json을 고쳐 다시 실행한다
#
# 방법: 어두운 픽셀(LAB 밝기 < lineL) = 선. 선으로 나뉜 영역마다 안쪽 색의 중앙값을 그 영역 색으로 쓰고(평탄화),
# 가장 가까운 기준 색(colors) → rules의 첫 일치(색 + 영역 중심이 area 안 — 원본 크기에 대한 0~1 비율)로 단계를 정한다.
# 선 · 작은 조각 밑은 가장 가까운 영역으로 채워 단계끼리 선 아래에서만 맞닿게 한다. 캔버스는 2160 × 2700을 덮도록 키워 가운데를 자른다
import json
import os
import sys

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))   # 프로젝트 루트 — 미리보기 위치의 기준
W, H = 2160, 2700      # ArtSpec §3
MIN_AREA = 60          # 이보다 작은 영역은 색으로 정하지 않고 둘레 영역으로 채운다
WARN_DIST = 20         # 영역 색이 기준 색에서 이만큼(LAB) 넘게 떨어지면 알린다 — 규칙이 맞는지 사람이 본다
SUPER = 2              # 선 가장자리를 부드럽게: 2배로 그려 줄인다


def lab(rgb):
    return cv2.cvtColor(np.uint8([[rgb]]), cv2.COLOR_RGB2LAB)[0, 0].astype(float)


def fit(img, interp, scale=1):
    # 캔버스를 덮도록 키우고 가운데를 자른다
    h0, w0 = img.shape[:2]
    s = max(W / w0, H / h0) * scale
    sw, sh = round(w0 * s), round(h0 * s)
    r = cv2.resize(img, (sw, sh), interpolation=interp)
    x, y = (sw - W * scale) // 2, (sh - H * scale) // 2
    return r[y:y + H * scale, x:x + W * scale]


def stop_if(fails):
    if fails:
        print('실패:')
        for f in fails:
            print(' - ' + f)
        sys.exit(1)


def split(chapter_dir):
    sys.stdout.reconfigure(encoding='utf-8')
    source = os.path.join(chapter_dir, '_source')
    cfg = json.load(open(os.path.join(source, 'split.json'), encoding='utf-8'))
    rgb = np.array(Image.open(os.path.join(source, cfg['image'])).convert('RGB'))
    h0, w0 = rgb.shape[:2]
    light = cv2.cvtColor(rgb, cv2.COLOR_RGB2LAB)[..., 0].astype(np.float32)
    line_l = cfg['lineL']
    line = light < line_l

    steps = [s['id'] for s in cfg['steps']]
    refs = {k: lab(v) for k, v in cfg['colors'].items()}
    # 설정 오류는 영역을 보기 전에 규칙마다 한 번씩 알린다
    fails = [f'규칙 {n}: 단계 {r["step"]}가 steps에 없음' for n, r in enumerate(cfg['rules'], 1) if r['step'] not in steps]
    fails += [f'규칙 {n}: 기준 색 {r["color"]}가 colors에 없음' for n, r in enumerate(cfg['rules'], 1) if r['color'] not in refs]
    if not line.any():
        fails.append(f'선 픽셀 없음 — lineL {line_l}보다 어두운 픽셀이 없다')
    stop_if(fails)

    count, regions, stats, _ = cv2.connectedComponentsWithStats((~line).astype(np.uint8), connectivity=4)
    step_of = np.zeros(count, np.int32)          # 영역 → 단계 번호(1~), 0 = 둘레로 채움
    color_of = np.zeros((count, 3), np.uint8)
    small = 0
    for i in range(1, count):
        x, y, w, h, area = stats[i]
        if area < MIN_AREA:
            small += 1
            continue
        # 영역 상자만 잘라 본다 — 여유 2픽셀(5×5 erode가 보는 폭)이면 그림 전체로 본 결과와 같다
        x0, y0, x1, y1 = max(x - 2, 0), max(y - 2, 0), min(x + w + 2, w0), min(y + h + 2, h0)
        mask = regions[y0:y1, x0:x1] == i
        inner = cv2.erode(mask.astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool)
        box = rgb[y0:y1, x0:x1]
        color = np.median(box[inner] if inner.sum() > 10 else box[mask], axis=0).astype(np.uint8)
        name, dist = min(((k, np.linalg.norm(lab(color.tolist()) - v)) for k, v in refs.items()), key=lambda t: t[1])
        cx, cy = (x + w / 2) / w0, (y + h / 2) / h0
        rule = next((r for r in cfg['rules'] if r['color'] == name
                     and (lambda a: a[0] <= cx <= a[2] and a[1] <= cy <= a[3])(r.get('area', [0, 0, 1, 1]))), None)
        where = f'영역 {i} (x {x}, y {y}, {w}×{h}, 색 {color.tolist()})'
        if rule is None:
            fails.append(f'{where}: 기준 색 {name}에 맞는 규칙 없음')
            continue
        if dist > WARN_DIST:
            print(f'확인: {where} → {name} (거리 {dist:.0f}) → {rule["step"]}')
        step_of[i] = steps.index(rule['step']) + 1
        color_of[i] = color
    stop_if(fails)

    # 선 · 작은 조각 밑을 가장 가까운 영역으로
    owner = np.where(step_of[regions] > 0, regions, 0)
    nearest = ndimage.distance_transform_edt(owner == 0, return_distances=False, return_indices=True)
    owner = owner[nearest[0], nearest[1]]
    owner = fit(owner.astype(np.float32), cv2.INTER_NEAREST).astype(np.int32)
    step_map, color_map = step_of[owner], color_of[owner]
    # 파일을 쓰기 전에 모든 검사를 끝낸다 — 실패하면 원본 폴더를 건드리지 않는다
    stop_if([f'{n:02d}_{step}: 이 단계에 배정된 영역 없음' for n, step in enumerate(steps, 1) if not (step_map == n).any()])

    # 선화: 2배 크기에서 선을 그리고 줄여 가장자리 알파를 만든다. 선 색 = 가장 어두운 쪽의 중앙값(그런 픽셀이 없으면 선 전체의 중앙값 — 덜 어두운 색 선)
    big = fit(light, cv2.INTER_CUBIC, SUPER) < line_l
    alpha = cv2.resize(big.astype(np.float32), (W, H), interpolation=cv2.INTER_AREA)
    line_rgba = np.zeros((H, W, 4), np.uint8)
    darkest = light < line_l * 0.75
    line_rgba[..., :3] = np.median(rgb[darkest if darkest.any() else line], axis=0).astype(np.uint8)
    line_rgba[..., 3] = np.round(np.clip(alpha, 0, 1) * 255).astype(np.uint8)

    written = ['00_line.png']
    Image.fromarray(line_rgba).save(os.path.join(chapter_dir, '00_line.png'), optimize=True)
    reference = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    layers = []
    for n, step in enumerate(steps, 1):
        mask = step_map == n
        layer = np.zeros((H, W, 4), np.uint8)
        layer[mask, :3] = color_map[mask]
        layer[mask, 3] = 255
        file = f'{n:02d}_{step}.png'
        image = Image.fromarray(layer)
        image.save(os.path.join(chapter_dir, file), optimize=True)
        reference.alpha_composite(image)
        layers.append(image)
        written.append(file)
        print(f'{file}: {100 * mask.mean():.1f}%')
    reference.alpha_composite(Image.fromarray(line_rgba))
    reference.save(os.path.join(chapter_dir, 'reference.png'), optimize=True)

    for file in os.listdir(chapter_dir):   # 단계가 줄거나 이름이 바뀌어 남은 옛 레이어
        if file.endswith('.png') and file[:2].isdigit() and file not in written:
            os.remove(os.path.join(chapter_dir, file))
            print(f'옛 레이어 삭제: {file}')

    with open(os.path.join(chapter_dir, 'steps.md'), 'w', encoding='utf-8', newline='\n') as f:
        f.write('| 번호 | 파일 | 소재(한글) | 그림 속 위치 · 메모 |\n|---|---|---|---|\n')
        for n, s in enumerate(cfg['steps'], 1):
            f.write(f'| {n:02d} | {n:02d}_{s["id"]}.png | {s["label"]} | {s.get("memo", "")} |\n')

    # 사람이 볼 색칠 과정(선화만 → 1단계 → … → 완성) — Builds/는 git이 무시한다
    preview = os.path.join(ROOT, 'Builds', 'ArtSplit', f'{os.path.basename(os.path.normpath(chapter_dir))}_progress.png')
    os.makedirs(os.path.dirname(preview), exist_ok=True)
    tw, th = 270, 338
    cols = 4
    frames = len(steps) + 1
    sheet = Image.new('RGB', (cols * tw, -(-frames // cols) * th), (250, 245, 235))
    canvas = Image.new('RGBA', (W, H), (250, 245, 235, 255))
    for k in range(frames):
        if k > 0:
            canvas.alpha_composite(layers[k - 1])
        shot = canvas.copy()
        shot.alpha_composite(Image.fromarray(line_rgba))
        sheet.paste(shot.convert('RGB').resize((tw, th), Image.LANCZOS), ((k % cols) * tw, (k // cols) * th))
    sheet.save(preview)

    if small:
        print(f'작은 조각 {small}개는 둘레 영역으로 채움')
    print(f'{len(steps)}단계 + 선화 → {os.path.normpath(chapter_dir)} · 색칠 과정 미리보기 {preview}')


if __name__ == '__main__':
    split(sys.argv[1] if len(sys.argv) > 1 else 'ArtSource/Chapter1')
