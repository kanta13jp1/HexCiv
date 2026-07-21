using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// メインシーンをプログラムで生成する(ARCHITECTURE.md §10)。
/// Assets/Scenes/Main.unity を新規作成・保存し、ビルド設定に登録して Exit(0)。
/// (ゲーム本体は GameBootstrap の RuntimeInitializeOnLoadMethod が自動生成するため、
/// シーン内容はデフォルトのカメラ+ライトのみでよい。)
/// </summary>
public static class SceneSetup
{
    public static void EnsureScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
        }

        bool saved = EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        if (!saved)
        {
            Debug.Log("SCENE SETUP FAIL: シーンの保存に失敗した");
            EditorApplication.Exit(1);
            return;
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true),
        };
        AssetDatabase.SaveAssets();

        Debug.Log("SCENE SETUP OK: Assets/Scenes/Main.unity");
        EditorApplication.Exit(0);
    }
}
