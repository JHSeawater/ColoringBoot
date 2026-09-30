using System.Linq;
using System.Text;
using UnityEditor;

// 빌드 용량 정리 (Phase 4.1 · 점검 F3, 사용자 결정 2026-09-30) — run_script(file=AgentScripts/Phase4BuildSize.cs, entry=Phase4BuildSize.Apply, args=[dryRun])
// ① Unity 스플래시 끄기(Unity 6은 Personal도 가능) ② 안 쓰는 TMP 기본 폰트 LiberationSans 빼기 — Resources 폴더라 참조가 없어도 빌드에 통째로 들어갔다.
// TMP 기본 폰트는 Pretendard SDF(Phase2Font), 폴백 목록은 비어 있다
public static class Phase4BuildSize
{
    private static readonly string[] _unusedFontAssets =
    {
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset",
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Drop Shadow.mat",
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat",
        "Assets/TextMesh Pro/Fonts/LiberationSans.ttf",
        "Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt",
        "Assets/TextMesh Pro/Fonts",                          // 위 파일을 빼면 비는 폴더
        "Assets/TextMesh Pro/Resources/Fonts & Materials",
    };

    public static string Apply(bool dryRun)
    {
        var report = new StringBuilder();
        report.Append($"스플래시 {PlayerSettings.SplashScreen.show} → false, 로고 {PlayerSettings.SplashScreen.showUnityLogo} → false\n");
        foreach (string path in _unusedFontAssets)
            report.Append($"{(AssetDatabase.LoadMainAssetAtPath(path) != null ? "삭제" : "없음")}: {path}\n");
        if (dryRun) return "[dry run]\n" + report;

        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        var failed = _unusedFontAssets.Where(p => AssetDatabase.LoadMainAssetAtPath(p) != null && !AssetDatabase.DeleteAsset(p)).ToList();
        AssetDatabase.SaveAssets();
        return report + $"적용 — 스플래시 {PlayerSettings.SplashScreen.show} · 삭제 실패 {failed.Count}개 {string.Join(", ", failed)}";
    }
}
