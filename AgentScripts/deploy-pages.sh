#!/usr/bin/env bash
# WebGL 빌드(Builds/WebGL)를 gh-pages 브랜치에 배포한다 → https://jhseawater.github.io/ColoringBoot/
# 사용: bash AgentScripts/deploy-pages.sh "커밋 메시지"
# 작업 트리 Builds/gh-pages(git 무시 폴더)를 비우고 빌드 결과물만 복사한다.
# 빌드 결과물(index.html · .nojekyll · Build/ · TemplateData/) 외 파일이 섞이면 커밋하지 않고 중단한다.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$ROOT/Builds/WebGL"
WT="$ROOT/Builds/gh-pages"
MSG="${1:-deploy: WebGL 빌드}"

[ -f "$SRC/index.html" ] || { echo "빌드가 없습니다: $SRC/index.html"; exit 1; }

if [ ! -e "$WT/.git" ]; then
    git -C "$ROOT" worktree prune
    git -C "$ROOT" worktree add "$WT" gh-pages
fi

# 작업 트리를 비우고(.git 제외) 빌드 결과물만 복사
find "$WT" -mindepth 1 -maxdepth 1 ! -name .git -exec rm -rf {} +
cp -r "$SRC"/. "$WT"/
touch "$WT/.nojekyll"
git -C "$WT" add -A

extra=$(git -C "$WT" ls-files | grep -v -E '^(index[.]html|[.]nojekyll|Build/|TemplateData/)' || true)
if [ -n "$extra" ]; then
    echo "빌드 결과물이 아닌 파일이 있어 중단합니다:"
    echo "$extra"
    exit 1
fi

if git -C "$WT" diff --cached --quiet; then
    echo "변경 없음 — 배포할 것이 없습니다."
    exit 0
fi

git -C "$WT" commit -q -m "$MSG"
git -C "$WT" push
echo "배포 완료: $(git -C "$WT" log --oneline -1)"
