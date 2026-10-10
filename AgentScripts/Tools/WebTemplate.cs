using System.IO;
using UnityEditor;

// gh-pages 빌드의 웹 템플릿(PROJECT:ColoringBoot)을 원본에서 Assets/WebGLTemplates/ColoringBoot로 복사한다 (2026-10-11).
// 앱인토스 SDK(GitGuard)가 Assets/WebGLTemplates/를 빌드 산출물로 보고 git에서 빼므로, 원본은 AgentScripts/Setup/WebTemplate/에서 관리한다.
// 템플릿을 고칠 땐 원본을 고친 뒤 다시 실행한다. 실행: run_script(file=AgentScripts/Tools/WebTemplate.cs, entry=WebTemplate.Install)
// — 저장소를 새로 받았을 때 · gh-pages 빌드 전에 템플릿이 없거나 원본과 다를 때
public static class WebTemplate
{
    private const string Source = "AgentScripts/Setup/WebTemplate/ColoringBoot";
    private const string Target = "Assets/WebGLTemplates/ColoringBoot";

    public static string Install()
    {
        int copied = 0;
        foreach (string file in Directory.GetFiles(Source, "*", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(Target, file.Substring(Source.Length + 1));
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(file, destination, true);
            copied++;
        }
        AssetDatabase.Refresh();
        return $"템플릿 파일 {copied}개 → {Target} · 지금 WebGL 템플릿 설정 {PlayerSettings.WebGL.template}";
    }
}
