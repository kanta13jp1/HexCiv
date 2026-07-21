using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Windows スタンドアロンのビルド(ARCHITECTURE.md §10)。
/// Assets/Scenes/Main.unity を Build/HexCiv.exe に出力し、結果に応じて Exit(0/1)。
/// </summary>
public static class BuildScript
{
    public static void PerformBuild()
    {
        // アプリアイコンの生成・PlayerSettings割り当て(2026-07-21 Claude Code 追加。冪等)。
        // 万一失敗してもアイコン無しで実行可能なため、ビルド自体は従来どおり続行する。
        try { AppIconGenerator.EnsureIcon(); }
        catch (System.Exception ex) { Debug.LogWarning("AppIconGenerator failed: " + ex.Message); }

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = "Build/HexCiv.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"BUILD OK: {summary.totalSize} bytes, {summary.totalTime.TotalSeconds:F1}s");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.Log($"BUILD FAIL: {summary.result} (errors={summary.totalErrors})");
            EditorApplication.Exit(1);
        }
    }
}
