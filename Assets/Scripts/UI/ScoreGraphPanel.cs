using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// 戦況グラフ(2026-07-20 Claude Code 追加)。毎ターン各文明のスコア
    /// (Σ人口×3 + 都市数×8 + 技術数×5 — TurnManager のスコア勝利と同じ式)を
    /// 表示側のリングバッファ(最大300サンプル)へ記録し、Texture2D の折れ線グラフとして描く。
    /// UIManager が新しいゲームごとに生成する(Canvas と共に破棄されるため、履歴も
    /// 新規ゲームで自動的にクリアされる)。開閉は「戦況」ボタン(通常プレイの二段目/観戦バー)、
    /// Esc は UIManager.CloseAllPanels 経由で閉じる。シミュレーションへは読み取り専用。
    /// </summary>
    public class ScoreGraphPanel : MonoBehaviour
    {
        const int MaxSamples = 300;
        const int TexWidth = 320;
        const int TexHeight = 160;

        GameState state;
        GameObject panel;
        RawImage graphImage;
        Texture2D graphTexture;
        Text rangeText;
        readonly List<Text> legendTexts = new List<Text>();

        /// <summary>プレイヤーごとのスコア履歴(state.Players と同じ並び。滅亡後も列を揃えるため全員毎ターン記録)。</summary>
        readonly List<List<int>> samples = new List<List<int>>();
        /// <summary>記録したターン番号(samples の列と対応)。</summary>
        readonly List<int> sampleTurns = new List<int>();
        int lastSampledTurn = int.MinValue;
        bool textureDirty;

        public bool IsVisible => panel != null && panel.activeSelf;

        /// <summary>UI構築と初期サンプル。UIManager.Init から新しいゲームごとに呼ばれる。</summary>
        public void Init(GameState s)
        {
            state = s;
            samples.Clear();
            sampleTurns.Clear();
            lastSampledTurn = int.MinValue;
            if (state != null)
                for (int i = 0; i < state.Players.Count; i++)
                    samples.Add(new List<int>());

            UIStyle.StretchFull(gameObject);   // 子パネルのアンカー基準を Canvas 全面にする
            BuildPanel();
            TakeSample();
        }

        void OnDestroy()
        {
            if (graphTexture != null) Destroy(graphTexture);
        }

        void Update()
        {
            if (state == null) return;
            // ターンが進んだらサンプリング(観戦・通常プレイ共通。Version の変化は
            // ターン内の細かい更新も含むため、記録はターン番号の変化に揃える)
            if (state.TurnNumber != lastSampledTurn) TakeSample();
            if (IsVisible && textureDirty)
            {
                Redraw();
                RefreshLegend();
            }
        }

        public void Toggle()
        {
            if (panel == null) return;
            if (panel.activeSelf) Hide();
            else Show();
        }

        public void Show()
        {
            if (panel == null) return;
            transform.SetAsLastSibling();   // 他パネルより手前へ(Canvas 直下の並び順)
            panel.SetActive(true);
            Redraw();
            RefreshLegend();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        void BuildPanel()
        {
            if (panel != null) Destroy(panel);
            panel = UIStyle.CreatePanel(transform, "Panel", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(470f, 344f));

            var title = UIStyle.CreateText(panel.transform, "Title", "戦況グラフ", 18,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-72f, 24f));

            var formula = UIStyle.CreateText(panel.transform, "Formula",
                "スコア = Σ人口×3 + 都市数×8 + 技術数×5", 12,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(formula.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(-40f, 18f));

            var close = UIStyle.CreateButton(panel.transform, "CloseButton", "×", 16, Hide);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-4f, -4f), new Vector2(26f, 26f));

            graphTexture = new Texture2D(TexWidth, TexHeight, TextureFormat.RGBA32, false);
            graphTexture.wrapMode = TextureWrapMode.Clamp;
            graphTexture.filterMode = FilterMode.Bilinear;

            var imgGo = new GameObject("Graph", typeof(RectTransform), typeof(RawImage));
            imgGo.transform.SetParent(panel.transform, false);
            graphImage = imgGo.GetComponent<RawImage>();
            graphImage.texture = graphTexture;
            graphImage.raycastTarget = false;
            UIStyle.SetRect(imgGo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(430f, 190f));

            rangeText = UIStyle.CreateText(panel.transform, "Range", "", 12,
                TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.SetRect(rangeText.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -246f), new Vector2(220f, 18f));

            legendTexts.Clear();
            if (state != null)
            {
                // 文明名+現在値の凡例(2列。最大8文明で4行)
                for (int i = 0; i < state.Players.Count; i++)
                {
                    int col = i % 2;
                    int row = i / 2;
                    var t = UIStyle.CreateText(panel.transform, "Legend" + i, "", 13,
                        TextAnchor.MiddleLeft, UIStyle.TextMain);
                    UIStyle.SetRect(t.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(0f, 1f), new Vector2(20f + col * 218f, -266f - row * 19f),
                        new Vector2(214f, 18f));
                    legendTexts.Add(t);
                }
            }

            panel.SetActive(false);
        }

        void TakeSample()
        {
            if (state == null) return;
            lastSampledTurn = state.TurnNumber;
            sampleTurns.Add(state.TurnNumber);
            for (int i = 0; i < samples.Count && i < state.Players.Count; i++)
                samples[i].Add(ScoreOf(state.Players[i]));
            while (sampleTurns.Count > MaxSamples)
            {
                sampleTurns.RemoveAt(0);
                for (int i = 0; i < samples.Count; i++)
                    if (samples[i].Count > 0) samples[i].RemoveAt(0);
            }
            textureDirty = true;
        }

        /// <summary>TurnManager のスコア勝利と同じ式(表示専用に複製。シミュレーションには影響しない)。</summary>
        static int ScoreOf(Player p)
        {
            int score = 0;
            for (int i = 0; i < p.Cities.Count; i++)
                score += p.Cities[i].Population * 3;
            score += p.Cities.Count * 8 + p.KnownTechs.Count * 5;
            return score;
        }

        void Redraw()
        {
            textureDirty = false;
            if (graphTexture == null || state == null) return;

            var px = new Color32[TexWidth * TexHeight];
            var bg = new Color32(8, 12, 20, 220);   // 暗い半透明の背景
            for (int i = 0; i < px.Length; i++) px[i] = bg;

            // 目安の横罫線(1/4刻み)
            var grid = new Color32(60, 68, 84, 120);
            for (int g = 1; g <= 3; g++)
            {
                int gy = TexHeight * g / 4;
                for (int x = 0; x < TexWidth; x++) px[gy * TexWidth + x] = grid;
            }

            int n = sampleTurns.Count;
            if (n > 0)
            {
                int maxScore = 1;
                for (int p = 0; p < samples.Count; p++)
                    for (int i = 0; i < samples[p].Count; i++)
                        if (samples[p][i] > maxScore) maxScore = samples[p][i];

                for (int p = 0; p < samples.Count && p < state.Players.Count; p++)
                {
                    var player = state.Players[p];
                    if (player.IsEliminated) continue;   // 生存文明のみ折れ線を描く
                    var line = samples[p];
                    Color32 c = LineColor(player.Color);
                    int prevX = 0, prevY = 0;
                    for (int i = 0; i < line.Count; i++)
                    {
                        int x = line.Count <= 1 ? 0 : (int)((long)i * (TexWidth - 1) / (line.Count - 1));
                        int y = (int)((float)line[i] / maxScore * (TexHeight - 3)) + 1;
                        if (i > 0) DrawLine(px, prevX, prevY, x, y, c);
                        else SetPx(px, x, y, c);
                        prevX = x;
                        prevY = y;
                    }
                }
            }

            graphTexture.SetPixels32(px);
            graphTexture.Apply(false);

            if (rangeText != null)
                rangeText.text = n > 0 ? $"ターン {sampleTurns[0]}〜{sampleTurns[n - 1]}" : "";
        }

        void RefreshLegend()
        {
            if (state == null) return;
            for (int i = 0; i < legendTexts.Count; i++)
            {
                var t = legendTexts[i];
                if (t == null) continue;
                if (i >= state.Players.Count) { t.text = ""; continue; }
                var p = state.Players[i];
                if (p.IsEliminated)
                {
                    t.text = $"☠ {p.NameJa} 滅亡";
                    t.color = UIStyle.TextDim;
                }
                else
                {
                    int score = i < samples.Count && samples[i].Count > 0
                        ? samples[i][samples[i].Count - 1]
                        : 0;
                    t.text = $"■ {p.NameJa}  {score}";
                    t.color = LineColor(p.Color);
                }
            }
        }

        /// <summary>折れ線・凡例の色(暗い文明色でも見えるよう少し明るくし、不透明にする)。</summary>
        static Color LineColor(Color c)
        {
            var l = Color.Lerp(c, Color.white, 0.2f);
            l.a = 1f;
            return l;
        }

        static void DrawLine(Color32[] px, int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                SetPx(px, x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        static void SetPx(Color32[] px, int x, int y, Color32 c)
        {
            if (x < 0 || x >= TexWidth || y < 0 || y >= TexHeight) return;
            px[y * TexWidth + x] = c;
        }
    }
}
