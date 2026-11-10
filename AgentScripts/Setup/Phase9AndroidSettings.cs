using UnityEditor;
using UnityEditor.Build;

// Phase 9.2 Android 빌드 설정 적용 기록 — run_script(file=AgentScripts/Setup/Phase9AndroidSettings.cs, entry=Phase9AndroidSettings.Preview → Apply)
// 패키지 이름은 Android만, 회사 이름 · 화면 방향은 WebGL과 함께 쓰는 값이다(계획 Docs/plans/2026-11-10-android-build.md)
public static class Phase9AndroidSettings
{
    private const string PackageName = "com.jhseawater.coloringboot";
    private const string CompanyName = "JHSeawater";

    public static string Preview() => $"지금: {Current()} → 적용하면: android {PackageName} · company {CompanyName} · orientation {UIOrientation.Portrait}";

    public static string Apply()
    {
        string before = Current();
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        AssetDatabase.SaveAssets();
        return $"{before} → {Current()}";
    }

    private static string Current() =>
        $"android {PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)} · company {PlayerSettings.companyName} · orientation {PlayerSettings.defaultInterfaceOrientation}";
}
