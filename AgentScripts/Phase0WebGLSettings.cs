using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;

// Phase 0.2 — WebGL Player Settings 적용 (run_script 빌더. Assets 밖이라 임포트 · 도메인 리로드 없음)
// 사용: run_script file=AgentScripts/Phase0WebGLSettings.cs entry=Phase0WebGLSettings.Preview (조회) / Phase0WebGLSettings.Apply (적용)
public static class Phase0WebGLSettings
{
    // 제거한 패키지(Sentis · App UI)가 남긴 스크립팅 심볼과 설정 참조
    static readonly string[] StaleDefines = { "SENTIS_ANALYTICS_ENABLED", "APP_UI_EDITOR_ONLY" };
    const string StaleConfigKey = "com.unity.dt.app-ui";

    const int CanvasWidth = 540;
    const int CanvasHeight = 960;

    public static string Preview() => Describe();

    public static string Apply()
    {
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.defaultWebScreenWidth = CanvasWidth;
        PlayerSettings.defaultWebScreenHeight = CanvasHeight;
        RemoveStaleDefines(NamedBuildTarget.WebGL);
        RemoveStaleDefines(NamedBuildTarget.Standalone);
        EditorBuildSettings.RemoveConfigObject(StaleConfigKey);
        AssetDatabase.SaveAssets();
        return Describe();
    }

    static void RemoveStaleDefines(NamedBuildTarget target)
    {
        var kept = PlayerSettings.GetScriptingDefineSymbols(target)
            .Split(';')
            .Where(d => d.Length > 0 && !StaleDefines.Contains(d));
        PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", kept));
    }

    static string Describe()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"compression={PlayerSettings.WebGL.compressionFormat}");
        sb.AppendLine($"decompressionFallback={PlayerSettings.WebGL.decompressionFallback}");
        sb.AppendLine($"dataCaching={PlayerSettings.WebGL.dataCaching}");
        sb.AppendLine($"strippingLevel(WebGL)={PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL)}");
        sb.AppendLine($"il2cppCodeGeneration(WebGL)={PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL)}");
        sb.AppendLine($"canvas={PlayerSettings.defaultWebScreenWidth}x{PlayerSettings.defaultWebScreenHeight}");
        sb.AppendLine($"defines(WebGL)='{PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL)}'");
        sb.AppendLine($"defines(Standalone)='{PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone)}'");
        sb.AppendLine($"configObject({StaleConfigKey})={EditorBuildSettings.TryGetConfigObject(StaleConfigKey, out UnityEngine.Object _)}");
        return sb.ToString();
    }
}
