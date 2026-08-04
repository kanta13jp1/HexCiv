using System;
using System.IO;
using HexCiv.Campaigns;
using HexCiv.Core;
using HexCiv.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stage 4F の実キャンペーン状態から、複数水利要求が並ぶ地域UIを撮影する。
/// 出力は販売素材候補だが、H10開始前は公開・計測へ使用しない。
/// </summary>
public static class UrukRegionalScreenshot
{
    const int Width = 760;
    const int Height = 446;

    public static void CaptureArbitrationCandidate()
    {
        GameObject host = null;
        GameObject cameraGo = null;
        RenderTexture target = null;
        Texture2D texture = null;
        var previousActive = RenderTexture.active;
        try
        {
            var definition = HistoricalCampaignRepository.LoadBuiltIn(
                HistoricalCampaignRepository.Uruk4000Id);
            var session = HistoricalCampaignFactory.Build(definition);
            PrepareMultipleDisputes(session);

            host = new GameObject("UrukRegionalScreenshotHost");
            var panel = host.AddComponent<UrukRegionalPanel>();
            panel.Init(session, action =>
                UrukCampaignSystem.TryApplyAction(session, action, out _));
            var canvas = host.GetComponentInChildren<Canvas>(true);
            if (canvas == null) throw new Exception("地域UI Canvasが見つからない");
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.referenceResolution =
                new Vector2(Width, Height);

            cameraGo = new GameObject("UrukRegionalScreenshotCamera",
                typeof(Camera));
            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.075f, 1f);
            camera.cullingMask = ~0;

            target = new RenderTexture(Width, Height, 24);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            Canvas.ForceUpdateCanvases();
            var root = canvas.transform as RectTransform;
            if (root != null) LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture.active = target;
            texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();
            string directory = Path.Combine(Directory.GetCurrentDirectory(),
                "Logs", "marketing");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory,
                "uruk_stage4f_water_arbitration.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Debug.Log("URUK REGIONAL SCREENSHOT OK: " + path);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("URUK REGIONAL SCREENSHOT FAIL: " + ex);
            EditorApplication.Exit(1);
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            if (cameraGo != null)
            {
                var camera = cameraGo.GetComponent<Camera>();
                if (camera != null) camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(cameraGo);
            }
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
        }
    }

    static void PrepareMultipleDisputes(HistoricalCampaignSession session)
    {
        SetSegment(session.Progress, "uruk_intake_segment", 80, true);
        SetSegment(session.Progress, "uruk_north_branch", 80, true);
        SetSegment(session.Progress, "lagash_tigris_branch", 0, true);
        SetSegment(session.Progress, "eridu_wetland_intake", 0, true);
        SetSegment(session.Progress, "ur_marsh_branch", 0, true);
        session.State.TurnNumber = 13;
        UrukCampaignSystem.AdvanceAfterTurn(session);
        if (UrukRegionalSystem.OpenWaterDisputeCount(session.Progress) != 3)
            throw new Exception("撮影用の水利要求3件を再現できない");
    }

    static void SetSegment(UrukCampaignProgress progress, string id,
        int condition, bool completed)
    {
        foreach (var segment in progress.canalSegments)
            if (segment.id == id)
            {
                segment.condition = condition;
                segment.completed = completed;
                return;
            }
        throw new Exception("撮影対象水路が見つからない: " + id);
    }
}
