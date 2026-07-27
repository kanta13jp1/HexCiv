using System;
using System.Collections.Generic;
using System.IO;
using HexCiv.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// uGUI で組んだ画面をヘッドレスで PNG に焼く(2026-07-28 Claude Code 追加)。
///
/// 目的: このプロジェクトの UI は実行時にコードで組み立てるため、**実際にゲームを起動して
/// 目で見るまで見た目が分からない**。文字の重なり・はみ出し・空白過多といった不具合は
/// 「例外が出ない」ことをいくら確認しても捕まらない。ここでヘッドレスに絵を出せるように
/// しておくと、実起動できない状況でもレイアウトを確認できる。
///
/// 仕組み: 対象の Canvas を ScreenSpaceOverlay から ScreenSpaceCamera へ差し替え、
/// RenderTexture へ描くだけの専用カメラで 1 フレーム描画して読み出す
/// (Overlay のままだと画面が無いヘッドレスでは何も焼けない)。
///
/// 出力先は `Logs/`(.gitignore 済み)。リポジトリを汚さない。
/// </summary>
public static class UiScreenshot
{
    const int Width = 1280;
    const int Height = 720;
    const string PrefsKey = "HexCiv.Growth.records.v1";

    /// <summary>
    /// 「成長の記録」を2枚焼く。
    /// - `ui_growth_history_actual.png`: 起点1件だけ(= 実機で今まさに見える状態)
    /// - `ui_growth_history_sample.png`: 記録が溜まった状態(= レイアウト確認用のサンプル)
    /// </summary>
    public static void CaptureGrowthHistory()
    {
        string saved = PlayerPrefs.GetString(PrefsKey, "");
        try
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Directory.CreateDirectory(dir);

            // ---- 1枚目: 起点だけ(実機の現状と同じ値になる) ----
            GrowthHistory.Clear();
            GrowthHistory.EnsureOrigin(UpdateDigest.ComputeOnly());
            Capture(Path.Combine(dir, "ui_growth_history_actual.png"));

            // ---- 2枚目: 記録が溜まった状態 ----
            GrowthHistory.Clear();
            SeedSampleRecords();
            Capture(Path.Combine(dir, "ui_growth_history_sample.png"));

            Debug.Log("UI SCREENSHOT OK");
            Restore(saved);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("UI SCREENSHOT FAIL: " + ex);
            Restore(saved);
            EditorApplication.Exit(1);
        }
    }

    static void Restore(string saved)
    {
        try
        {
            if (string.IsNullOrEmpty(saved)) PlayerPrefs.DeleteKey(PrefsKey);
            else PlayerPrefs.SetString(PrefsKey, saved);
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.Log("UI SCREENSHOT: 退避データの復元に失敗: " + ex.Message);
        }
    }

    /// <summary>
    /// レイアウト確認用の記録を積む。**実機の履歴とは無関係の作り物**で、
    /// 行の詰まり方・グラフの伸び方・名前付き行の折り返しを見るためだけに使う。
    /// </summary>
    static void SeedSampleRecords()
    {
        int ledger = 1180;
        var steps = new[]
        {
            new SampleStep { Ledger = 24, Rules = 0,  Panels = new string[0] },
            new SampleStep { Ledger = 12, Rules = 3,  Panels = new string[0] },
            new SampleStep { Ledger = 0,  Rules = 0,  Panels = new[] { "TimelapsePanel" } },
            new SampleStep { Ledger = 36, Rules = 0,  Panels = new string[0] },
            new SampleStep { Ledger = 8,  Rules = 2,  Panels = new string[0] },
            new SampleStep { Ledger = 0,  Rules = 0,  Panels = new[] { "UrukRegionalPanel", "PoliticsPanel" } },
            new SampleStep { Ledger = 43, Rules = 0,  Panels = new string[0] },
            new SampleStep { Ledger = 15, Rules = 5,  Panels = new string[0] },
            new SampleStep { Ledger = 0,  Rules = 1,  Panels = new string[0] },
            new SampleStep { Ledger = 27, Rules = 0,  Panels = new string[0] },
        };

        int panels = 13;
        int rules = 0;   // 累積。ここを累積させないと「世界の大きさ」が前後して見え、実物と挙動が変わる

        var origin = MakeResult(ledger, panels, rules);
        origin.IsFirstRun = true;
        GrowthHistory.Record(origin, "初回");

        for (int i = 0; i < steps.Length; i++)
        {
            ledger += steps[i].Ledger;
            panels += steps[i].Panels.Length;
            rules += steps[i].Rules;
            var result = MakeResult(ledger, panels, rules);
            result.LedgerDelta = steps[i].Ledger;
            result.RulesDelta = steps[i].Rules;
            for (int p = 0; p < steps[i].Panels.Length; p++)
                result.NewPanelKeys.Add(steps[i].Panels[p]);
            result.TotalDelta = steps[i].Ledger + steps[i].Rules + steps[i].Panels.Length;
            GrowthHistory.Record(result, UpdateDigest.BuildSummaryJa(result));
        }
    }

    struct SampleStep
    {
        public int Ledger;
        public int Rules;
        public string[] Panels;
    }

    static DigestResult MakeResult(int ledgerTotal, int panels, int rulesTotal)
    {
        var result = new DigestResult();
        result.HasChanges = true;
        result.Snapshot["ledger.total"] = ledgerTotal;
        result.Snapshot["ledger.categories"] = 32;
        result.Snapshot["rules.techs"] = 140 + rulesTotal;
        result.Snapshot["rules.units"] = 9;
        result.Snapshot["rules.buildings"] = 8;
        result.Snapshot["ui.panels"] = panels;
        result.Snapshot["core.systems"] = 12;
        result.Snapshot["core.catalogs"] = 12;
        return result;
    }

    // ==================================================================
    // 描画
    // ==================================================================

    static void Capture(string path)
    {
        GrowthHistoryView view = null;
        GameObject cameraGo = null;
        RenderTexture rt = null;
        Texture2D texture = null;
        var previousActive = RenderTexture.active;

        try
        {
            view = GrowthHistoryView.Open();
            if (view == null) throw new Exception("閲覧画面を開けなかった");

            var canvas = view.GetComponentInChildren<Canvas>(true);
            if (canvas == null) throw new Exception("Canvas が見つからない");

            // 画面の無いヘッドレスでは Overlay は焼けないので、専用カメラ経由へ切り替える
            cameraGo = new GameObject("UiScreenshotCamera", typeof(Camera));
            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            // タイトル画面の暗幕に近い背景(パネルの角や影が背景に溶けないことを確認したい)
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);
            camera.cullingMask = ~0;

            rt = new RenderTexture(Width, Height, 24);
            camera.targetTexture = rt;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;

            // レイアウトを確定させてから描く(1フレーム待てないので明示的に更新する)
            Canvas.ForceUpdateCanvases();
            var root = canvas.transform as RectTransform;
            if (root != null) LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Canvas.ForceUpdateCanvases();

            camera.Render();

            RenderTexture.active = rt;
            texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Debug.Log("[UiScreenshot] 保存: " + path);
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            if (cameraGo != null)
            {
                var cam = cameraGo.GetComponent<Camera>();
                if (cam != null) cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(cameraGo);
            }
            if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
            if (view != null) UnityEngine.Object.DestroyImmediate(view.gameObject);
        }
    }
}
