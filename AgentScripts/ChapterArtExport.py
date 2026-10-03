# 챕터 그림 → 게임용 (Phase 5.3) — 규격 점검(ArtCheck) → 절반 크기 → 레이어마다 여백 자르기 → PNG + layout.json
# python AgentScripts/ChapterArtExport.py [원본 폴더] [출력 폴더]  — 다음은 run_script(file=AgentScripts/ChapterArtBuilder.cs, entry=ChapterArtBuilder.Build)
import json
import os
import re
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ArtCheck  # noqa: E402

SCALE = 2          # 2160 × 2700 → 1080 × 1350 (ArtSpec §3)
PAD = 2            # 자른 가장자리가 필터링에 닿지 않게
BLOCK = 4          # 압축 텍스처는 가로 · 세로가 4의 배수여야 한다 — 캔버스 밖으로 넓혀 맞춘다(투명)
NAME = re.compile(r'^(\d{2})_([a-z0-9-]+)\.png$')


def export(source, target):
    sys.stdout.reconfigure(encoding='utf-8')
    ArtCheck.main(source)   # 실패하면 여기서 끝난다
    os.makedirs(target, exist_ok=True)
    width, height = ArtCheck.W // SCALE, ArtCheck.H // SCALE
    layout = {'width': width, 'height': height, 'line': None, 'steps': []}
    written = set()
    for file in sorted(os.listdir(source)):
        m = NAME.match(file)
        if not m:
            continue
        im = Image.open(os.path.join(source, file)).convert('RGBA')
        # 알파를 곱한 상태로 줄여야 투명한 가장자리에 검은 테두리가 생기지 않는다
        im = im.convert('RGBa').resize((width, height), Image.LANCZOS).convert('RGBA')
        x0, y0, x1, y1 = im.getchannel('A').getbbox()
        x0, y0, x1, y1 = x0 - PAD, y0 - PAD, x1 + PAD, y1 + PAD
        x1 += -(x1 - x0) % BLOCK
        y1 += -(y1 - y0) % BLOCK
        piece = Image.new('RGBA', (x1 - x0, y1 - y0), (0, 0, 0, 0))
        piece.paste(im.crop((max(x0, 0), max(y0, 0), min(x1, width), min(y1, height))), (max(-x0, 0), max(-y0, 0)))
        piece.save(os.path.join(target, file), optimize=True)
        written.add(file)
        entry = {'file': file, 'x': x0, 'y': y0, 'w': x1 - x0, 'h': y1 - y0}
        if int(m.group(1)) == 0:
            layout['line'] = entry
        else:
            layout['steps'].append(entry)
    for file in os.listdir(target):   # 이름이 바뀌어 남은 옛 레이어
        if NAME.match(file) and file not in written:
            os.remove(os.path.join(target, file))
            if os.path.exists(os.path.join(target, file + '.meta')):
                os.remove(os.path.join(target, file + '.meta'))
            print(f'옛 레이어 삭제: {file}')
    with open(os.path.join(target, 'layout.json'), 'w', encoding='utf-8', newline='\n') as f:
        json.dump(layout, f, indent=1)
    total = sum(os.path.getsize(os.path.join(target, f)) for f in written)
    print(f'{len(written)}장 → {os.path.normpath(target)} ({total:,}바이트, 캔버스 {width}×{height})')


if __name__ == '__main__':
    # 챕터 1 게임 그림 = 시험 그림(Phase 7.2, 2026-10-03). 임시 도안은 ArtSource/Chapter1에 그대로 있다
    export(sys.argv[1] if len(sys.argv) > 1 else 'ArtSource/chapter1_test', sys.argv[2] if len(sys.argv) > 2 else 'Assets/Art/Chapters/Chapter1')
