using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ColoringBoot.EditorTools
{
    // 앱인토스 SDK 빌드 뒤 ProjectSettings 정리 (CLAUDE.md §4 · DECISIONS.md 2026-11-11 "AIT 빌드 뒤 설정").
    // SDK(v3.9.0 AITConvertCore)는 빌드 직전 PlayerSettings를 스냅샷하고 빌드 뒤 메모리 값만 되돌린다 — 디스크 ProjectSettings.asset에는
    // 토스 값(AITTemplate · 메모리 1024 · 디버그 심볼 등)이 남고, WebGL 그래픽 API(WebGL 2 전용)는 되돌리지 않는다.
    // 그래서 토스 템플릿으로 WebGL 빌드가 끝나면 SDK가 템플릿을 되돌릴 때까지 기다렸다가 그래픽 API를 기본값으로 돌리고 디스크에 저장한다.
    // SDK를 올리면 이 동작(템플릿 이름 · 빌드 뒤 메모리 복원)을 다시 확인한다. 이 정리가 빠져도 deploy-pages.sh와 커밋 훅이 막는다
    [InitializeOnLoad]
    internal sealed class AitSettingsCleanup : IPostprocessBuildWithReport
    {
        private const string AitTemplate = "PROJECT:AITTemplate";
        private const string PendingKey = "ColoringBoot.AitCleanupPending";
        private const string DeadlineKey = "ColoringBoot.AitCleanupDeadline";
        // SDK는 웹 빌드 뒤 npm 패키징까지 끝내고 되돌릴 수 있어 넉넉히 기다린다(초)
        private const float WaitSeconds = 1800f;

        static AitSettingsCleanup()
        {
            // 빌드 중 도메인 리로드가 일어나도 기다림을 이어 간다(SessionState는 리로드 뒤에도 남는다)
            if (SessionState.GetBool(PendingKey, false)) EditorApplication.update += WaitForRestore;
        }

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL || PlayerSettings.WebGL.template != AitTemplate) return;
            if (SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, true);
            SessionState.SetFloat(DeadlineKey, (float)EditorApplication.timeSinceStartup + WaitSeconds);
            EditorApplication.update += WaitForRestore;
        }

        private static void WaitForRestore()
        {
            if (PlayerSettings.WebGL.template == AitTemplate)
            {
                if (EditorApplication.timeSinceStartup < SessionState.GetFloat(DeadlineKey, 0f)) return;
                Stop();
                Debug.LogWarning("[AIT 정리] 앱인토스 빌드 뒤 30분이 지나도 WebGL 템플릿이 AITTemplate 그대로라 정리하지 않았습니다 — git checkout -- ProjectSettings/ProjectSettings.asset 뒤 에디터를 다시 켜세요(CLAUDE.md §4).");
                return;
            }
            Stop();
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, true);
            // 디스크에 남은 토스 값을 메모리 값(SDK가 되돌린 값)으로 덮는다. PlayerSettings만 골라 저장하는 SaveAssetIfDirty는
            // ProjectSettings.asset을 쓰지 않아(2026-11-11 확인) 전체 저장을 쓴다 — 저장 안 된 다른 에셋도 함께 저장된다
            AssetDatabase.SaveAssets();
            Debug.Log("[AIT 정리] 앱인토스 빌드 뒤 ProjectSettings를 원래 값으로 저장했습니다(WebGL 그래픽 API 기본값).");
        }

        private static void Stop()
        {
            EditorApplication.update -= WaitForRestore;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseFloat(DeadlineKey);
        }
    }
}
