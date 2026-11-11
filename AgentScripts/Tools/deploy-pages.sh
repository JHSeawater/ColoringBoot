#!/usr/bin/env bash
# WebGL 빌드(Builds/WebGL)를 gh-pages 브랜치에 배포한다 → https://jhseawater.github.io/ColoringBoot/
# 사용: bash AgentScripts/Tools/deploy-pages.sh "커밋 메시지"
# 작업 트리 Builds/gh-pages(git 무시 폴더)를 비우고 빌드 결과물만 복사한다.
# 빌드 결과물(index.html · .nojekyll · Build/ · TemplateData/) 외 파일이 섞이면 커밋하지 않고 중단한다.
# gh-pages는 커밋 하나만 둔다(2026-10-04 사용자 결정 — 빌드 이력이 저장소 용량의 88%였다): 매번 부모 없는 커밋으로 바꾸고,
# 원격이 직전 배포 그대로일 때만 강제 push한다(--force-with-lease). 확인한 빌드는 날짜 · 빌드 바이트로 기록한다(Docs/BuildHistory.md).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SRC="$ROOT/Builds/WebGL"
WT="$ROOT/Builds/gh-pages"

[ -f "$SRC/index.html" ] || { echo "빌드가 없습니다: $SRC/index.html"; exit 1; }
# 앱인토스 SDK 빌드 값(AITTemplate · 디버그 심볼 · 캐싱 끔 등)이 남은 설정으로 만든 빌드는 배포하지 않는다 (CLAUDE.md §4)
grep -q "컬러링붓 전용 템플릿" "$SRC/index.html" || {
    echo "우리 WebGL 템플릿(ColoringBoot)으로 만든 빌드가 아니라 중단합니다 — 앱인토스 SDK 값이 남은 ProjectSettings로 빌드했을 수 있습니다."
    echo "git checkout -- ProjectSettings/ProjectSettings.asset → 에디터 재시작(또는 WebGL 설정 확인) → WebTemplate.Install → 다시 빌드"
    exit 1
}
BYTES=$(cat "$SRC"/Build/* | wc -c | tr -d ' ')
MSG="${1:-deploy: WebGL 빌드} (빌드 ${BYTES}바이트)"

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

OLD=$(git -C "$WT" rev-parse HEAD)
if git -C "$WT" diff --cached --quiet && [ "$(git -C "$WT" rev-list --count HEAD)" = 1 ]; then
    echo "변경 없음 — 배포할 것이 없습니다."
    exit 0
fi

# 스테이징한 빌드 결과물로 부모 없는 커밋 하나를 만들어 gh-pages를 바꾼다
NEW=$(git -C "$WT" commit-tree "$(git -C "$WT" write-tree)" -m "$MSG")
git -C "$WT" update-ref refs/heads/gh-pages "$NEW" "$OLD"
if ! git -C "$WT" push -q --force-with-lease=gh-pages:"$OLD" origin gh-pages; then
    git -C "$WT" update-ref refs/heads/gh-pages "$OLD" "$NEW"
    echo "push 실패 — 로컬 gh-pages를 직전 배포로 되돌렸습니다(원격이 바뀌었으면 git -C Builds/gh-pages fetch 뒤 다시)."
    exit 1
fi
echo "배포 완료: $(git -C "$WT" log --oneline -1)"
