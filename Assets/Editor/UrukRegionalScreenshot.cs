using System;
using System.IO;
using HexCiv.Campaigns;
using HexCiv.Core;
using HexCiv.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stage 4F/4G/4H の実キャンペーン状態から、地域外交UIを撮影する。
/// 出力は販売素材候補だが、H10開始前は公開・計測へ使用しない。
/// </summary>
public static class UrukRegionalScreenshot
{
    const int Width = 760;
    const int Height = 446;

    public static void CaptureArbitrationCandidate()
    {
        Capture("uruk_stage4f_water_arbitration.png",
            PrepareMultipleDisputes);
    }

    public static void CaptureLandRightsCandidate()
    {
        Capture("uruk_stage4g_land_rights.png", PrepareLandDispute);
    }

    public static void CaptureWaterAgreementCandidate()
    {
        Capture("uruk_stage4h_water_agreement_selection.png",
            PrepareMultipleAgreements);
    }

    static void Capture(string fileName,
        Action<HistoricalCampaignSession> prepare)
    {
        GameObject host = null;
        GameObject cameraGo = null;
        RenderTexture target = null;
        Texture2D texture = null;
        var previousActive = RenderTexture.active;
        try
        {
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new Exception(
                    "販促画像の撮影には描画デバイスが必要。-nographicsを外し-force-d3d11で実行してください。");
            var definition = HistoricalCampaignRepository.LoadBuiltIn(
                HistoricalCampaignRepository.Uruk4000Id);
            var session = HistoricalCampaignFactory.Build(definition);
            prepare(session);

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
            string path = Path.Combine(directory, fileName);
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

    static void PrepareLandDispute(HistoricalCampaignSession session)
    {
        var plot = FindFarm(session.Progress, "uruk_west_farm");
        plot.crop = "barley";
        session.Progress.selectedFarmId = plot.id;
        SetSegment(session.Progress, "uruk_intake_segment", 80, true);
        SetSegment(session.Progress, "uruk_west_branch", 80, true);
        session.State.TurnNumber = 15;
        UrukCampaignSystem.AdvanceAfterTurn(session);
        if (UrukRegionalSystem.FirstOpenLandDispute(session.Progress) == null)
            throw new Exception("撮影用の土地・耕作権紛争を再現できない");
    }

    static void PrepareMultipleAgreements(HistoricalCampaignSession session)
    {
        PrepareMultipleDisputes(session);
        SetGood(session.Progress, "barley", 5);
        if (!UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.ArbitrateWaterDisputesAction, out _))
            throw new Exception("撮影用の第三者仲裁を開始できない");
        if (!UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.NextWaterDisputeAction, out _))
            throw new Exception("撮影用の履行中合意を切り替えられない");
        if (UrukRegionalSystem.ActionableWaterCaseCount(session.Progress) != 3 ||
            UrukRegionalSystem.SelectedWaterCaseOrdinal(session.Progress) != 2 ||
            UrukRegionalSystem.SelectedActiveWaterAgreement(session.Progress) ==
                null)
            throw new Exception("撮影用の個別水利対象を再現できない");
    }

    static UrukFarmPlotState FindFarm(UrukCampaignProgress progress, string id)
    {
        foreach (var farm in progress.farmPlots)
            if (farm.id == id) return farm;
        throw new Exception("撮影対象農地が見つからない: " + id);
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

    static void SetGood(UrukCampaignProgress progress, string id, int amount)
    {
        foreach (var good in progress.stockpiles)
            if (good.id == id)
            {
                good.amount = amount;
                return;
            }
        throw new Exception("撮影用物資が見つからない: " + id);
    }
}
