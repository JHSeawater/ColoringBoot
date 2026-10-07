using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

// 안 쓰는 프로젝트 전체 입력(InputSystem_Actions) 정리 (Phase 8 구조 정리, 2026-10-07 앞당겨 함)
// UI 입력 모듈은 패키지 기본 액션(DefaultInputActions)을 쓰고 키보드는 Keyboard.current를 바로 읽어, 이 에셋은 등록만 되어 있었다.
// 등록돼 있으면 빌드할 때마다 ProjectSettings의 preloadedAssets에 들어가 되돌려야 했다.
// Preview(읽기 전용) → Apply(등록 해제 · 에셋 삭제 — ProjectSettings · 에셋 쓰기라 git으로만 되돌림)
public static class Phase8ProjectWideActions
{
    private const string ConfigKey = "com.unity.input.settings.actions";
    private const string AssetPath = "Assets/InputSystem_Actions.inputactions";

    public static string Preview()
    {
        bool registered = EditorBuildSettings.TryGetConfigObject(ConfigKey, out InputActionAsset asset);
        var module = UnityEngine.Object.FindAnyObjectByType<InputSystemUIInputModule>();
        string moduleAsset = module == null || module.actionsAsset == null ? "없음" : module.actionsAsset.name;
        return $"등록: {(registered ? AssetDatabase.GetAssetPath(asset) : "없음")} · 에셋 파일: {(AssetDatabase.LoadMainAssetAtPath(AssetPath) != null ? AssetPath : "없음")} · UI 모듈 액션: {moduleAsset}" +
               " → Apply: EditorBuildSettings 등록 해제 + 에셋(.meta 포함) 삭제";
    }

    public static string Apply()
    {
        bool removed = EditorBuildSettings.RemoveConfigObject(ConfigKey);
        bool deleted = AssetDatabase.DeleteAsset(AssetPath);
        AssetDatabase.SaveAssets();
        return $"등록 해제 {removed} · 에셋 삭제 {deleted} · 지금 프로젝트 전체 입력: {(InputSystem.actions == null ? "없음" : InputSystem.actions.name)}";
    }
}
