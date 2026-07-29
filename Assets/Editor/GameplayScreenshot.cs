using System;
using System.IO;
using HexCiv;
using HexCiv.Core;
using HexCiv.Core.AI;
using HexCiv.Render;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 実ゲーム画面をヘッドレスで PNG に焼く (2026-07-29 Claude Code 追加)。
///
/// 目的: **商品ページに画面が1枚も無い**状態を解消する。ゲームを売るのに
/// 見た目が見えないのは、転換率以前に商品として成立していない。
/// itch.io の掲載素材にも同じ絵を使う。
///
/// 仕組み: `UI/UiScreenshot.cs` と同じ考え方で、Unity のライフサイクルに頼らず
/// **`Init(GameState)` を直接呼んで**描画対象を組み立て、RenderTexture へ描く。
/// MapRenderer / EntityRenderer が初期化を Awake でなく Init に分離しているので
/// この手が使える。
///
/// 盤面は `SmokeTest` と同じ経路で作る (GameBootstrap.BuildNewGame → TurnManager)。
/// **数ターン進めてから撮る**のが要点で、初期盤面は都市も国境も無く商品写真に
/// ならない。育った盤面こそがこのゲームの見どころ (「育っていくのを見る」)。
///
/// 出力は `Logs/`(.gitignore 済み)。リポジトリを汚さない。
/// </summary>
public static class GameplayScreenshot
{
    const int Width = 1920;
    const int Height = 1080;

    /// <summary>撮影する時点。育ち具合の違う絵を並べたいので複数撮る。</summary>
    static readonly int[] CaptureTurns = { 30, 80, 150 };

    public static void Capture()
    {
        try
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Directory.CreateDirectory(dir);

            var config = new GameConfig
            {
                Seed = 42,
                HumanPlayerIndex = -1,   // 全員AI。観戦の絵を撮る
                MapWidth = 40,
                MapHeight = 24,
                NumPlayers = 4,
                GameLength = 0,
            };

            var state = GameBootstrap.BuildNewGame(config);
            var turnManager = new TurnManager(state, new AIController());
            turnManager.BeginGame();

            int played = 0;
            int shots = 0;
            foreach (int target in CaptureTurns)
            {
                while (played < target)
                {
                    turnManager.EndTurn();
                    played++;
                }
                string path = Path.Combine(dir, $"gameplay_turn{target}.png");
                if (CaptureCurrent(state, path)) shots++;
            }

            Debug.Log($"GAMEPLAY SCREENSHOT OK: {shots}/{CaptureTurns.Length} 枚");
            EditorApplication.Exit(shots > 0 ? 0 : 1);
        }
        catch (Exception ex)
        {
            Debug.Log("GAMEPLAY SCREENSHOT FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// いまの盤面を1枚焼く。描画物とカメラは撮るたびに作り直して確実に片付ける
    /// (使い回すと前の状態が残り、どのターンの絵か分からなくなる)。
    /// </summary>
    static bool CaptureCurrent(GameState state, string path)
    {
        GameObject mapGo = null;
        GameObject entityGo = null;
        GameObject camGo = null;
        GameObject lightGo = null;
        RenderTexture rt = null;
        Texture2D texture = null;
        var previousActive = RenderTexture.active;

        try
        {
            mapGo = new GameObject("ShotMapRenderer");
            var map = mapGo.AddComponent<MapRenderer>();
            map.Init(state);

            entityGo = new GameObject("ShotEntityRenderer");
            var entities = entityGo.AddComponent<EntityRenderer>();
            entities.Init(state);

            // 光が無いと地形が真っ黒に潰れる。実ゲームと同じ向きの平行光を1つ置く。
            lightGo = new GameObject("ShotLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            camGo = new GameObject("ShotCamera", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.05f, 0.09f, 1f);

            // 盤面が画面いっぱいに収まる位置を**実際の広がりから計算**する。
            // 距離をマジックナンバーで決めると、マップ寸法を変えた途端に
            // 余白だらけの絵になる (最初の実測がまさにそれで、盤面が画面の半分しか
            // 占めずストアのサムネイルとして使えなかった)。
            var bounds = WorldBounds(state);
            var center = bounds.center;

            const float VerticalFov = 45f;
            const float PitchDegrees = 48f;   // 見下ろし角。起伏と都市が見える角度
            const float Margin = 1.06f;       // 端が切れないための余白

            float aspect = (float)Width / Height;
            float halfV = VerticalFov * 0.5f * Mathf.Deg2Rad;
            float halfH = Mathf.Atan(Mathf.Tan(halfV) * aspect);

            // 幅がちょうど収まる距離と、奥行きが収まる距離の大きい方を採る。
            // 斜めから見るので奥行きは cos(pitch) ぶん縮んで見える。
            float pitch = PitchDegrees * Mathf.Deg2Rad;
            float distForWidth = (bounds.size.x * 0.5f) / Mathf.Tan(halfH);
            float distForDepth =
                (bounds.size.z * 0.5f * Mathf.Cos(pitch)) / Mathf.Tan(halfV);
            float dist = Mathf.Max(distForWidth, distForDepth) * Margin;

            cam.transform.position = center + new Vector3(
                0f,
                Mathf.Sin(pitch) * dist,
                -Mathf.Cos(pitch) * dist);
            cam.transform.LookAt(center);
            cam.fieldOfView = VerticalFov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = dist * 4f;

            rt = new RenderTexture(Width, Height, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Debug.Log("[GameplayScreenshot] 保存: " + path);
            return true;
        }
        catch (Exception ex)
        {
            // 1枚失敗しても他のターンは撮る。全滅かどうかは呼び出し元が枚数で判定する。
            Debug.Log("[GameplayScreenshot] 1枚失敗: " + ex.Message);
            return false;
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            if (camGo != null)
            {
                var cam = camGo.GetComponent<Camera>();
                if (cam != null) cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(camGo);
            }
            if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
            if (entityGo != null) UnityEngine.Object.DestroyImmediate(entityGo);
            if (mapGo != null) UnityEngine.Object.DestroyImmediate(mapGo);
            if (lightGo != null) UnityEngine.Object.DestroyImmediate(lightGo);
        }
    }

    /// <summary>
    /// 生成された地形メッシュから盤面の広がりを求める。
    /// マップ寸法からの計算だとヘックスの配置規約に依存するので、
    /// **実際に描かれた物の範囲**を使う方が壊れにくい。
    /// </summary>
    static Bounds WorldBounds(GameState state)
    {
        var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
            FindObjectsSortMode.None);
        Bounds? acc = null;
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            if (acc == null) acc = r.bounds;
            else
            {
                var b = acc.Value;
                b.Encapsulate(r.bounds);
                acc = b;
            }
        }
        if (acc != null && acc.Value.size.magnitude > 0.01f) return acc.Value;

        // 描画物が拾えなかった場合の保険。マップ寸法から概算する。
        float w = Mathf.Max(state.Map != null ? state.Map.Width : 40, 1);
        float h = Mathf.Max(state.Map != null ? state.Map.Height : 24, 1);
        return new Bounds(new Vector3(w * 0.5f, 0f, h * 0.5f), new Vector3(w, 1f, h));
    }
}
