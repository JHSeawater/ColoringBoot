using UnityEditor;

// Phase 0.2 — URP 에셋 2개의 파이프라인 볼륨 프로필 참조 해제 (run_script 빌더)
// MCP set_serialized_field는 값이 문자열이라 오브젝트 참조를 null로 비울 수 없어서 SerializedObject로 처리한다.
// 사용: run_script file=AgentScripts/Phase0ClearVolumeProfile.cs entry=Phase0ClearVolumeProfile.Apply
public static class Phase0ClearVolumeProfile
{
    static readonly string[] RpAssets = { "Assets/Settings/Mobile_RPAsset.asset", "Assets/Settings/PC_RPAsset.asset" };
    const string Field = "m_VolumeProfile";

    public static string Apply()
    {
        var report = "";
        foreach (var path in RpAssets)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var prop = asset != null ? new SerializedObject(asset).FindProperty(Field) : null;
            if (prop == null)
            {
                report += $"{path}: 에셋 또는 필드 없음; ";
                continue;
            }
            var before = prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "null";
            prop.objectReferenceValue = null;
            prop.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            report += $"{path}: {before} -> null; ";
        }
        AssetDatabase.SaveAssets();
        return report;
    }
}
