using UnityEditor;

// Phase 7.6 웹 템플릿 · 제품 이름 적용 기록 — run_script(file=AgentScripts/Phase7WebTemplate.cs, entry=Phase7WebTemplate.Apply)
// 템플릿 = Assets/WebGLTemplates/ColoringBoot(로딩 화면 · 아이콘 · PC 레터박스). 회사 이름은 아직 정하지 않아 그대로(2026-10-03 사용자)
public static class Phase7WebTemplate
{
    private const string Template = "PROJECT:ColoringBoot";
    private const string ProductName = "컬러링붓";

    public static string Apply()
    {
        AssetDatabase.Refresh();
        string before = $"productName {PlayerSettings.productName} · template {PlayerSettings.WebGL.template}";
        PlayerSettings.productName = ProductName;
        PlayerSettings.WebGL.template = Template;
        AssetDatabase.SaveAssets();
        return $"{before} → productName {PlayerSettings.productName} · template {PlayerSettings.WebGL.template} (companyName {PlayerSettings.companyName} 그대로)";
    }
}
