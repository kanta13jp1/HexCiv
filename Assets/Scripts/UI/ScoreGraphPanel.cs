using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// 戦況グラフ(2026-07-20 Claude Code 追加)。毎ターン各文明の4指標
    /// (スコア=Σ人口×3+都市数×8+技術数×5 — TurnManager のスコア勝利と同じ式 /
    /// 軍事力=Player.MilitaryPower() / 文化=TotalCulture / 技術=KnownTechs.Count)を
    /// 表示側のリングバッファ(最大300サンプル×4指標×文明数)へ記録し、
    /// 上部のタブ(スコア/軍事力/文化/技術 — 2026-07-22 Claude Code 追加)で選んだ指標を
    /// Texture2D の折れ線グラフとして描く。タブ切替は記録済みバッファからの再描画のみ。
    /// UIManager が新しいゲームごとに生成する(Canvas と共に破棄されるため、履歴も
    /// 新規ゲームで自動的にクリアされる)。開閉は「戦況」ボタン(通常プレイの二段目/観戦バー)、
    /// Esc は UIManager.CloseAllPanels 経由で閉じる。シミュレーションへは読み取り専用。
    /// </summary>
    public class ScoreGraphPanel : MonoBehaviour
    {
        const int MaxSamples = 300;
        const int TexWidth = 320;
        const int TexHeight = 160;

        // ---- 指標タブ(2026-07-22 Claude Code 追加) ----
        /// <summary>指標の数(0=スコア/1=軍事力/2=文化/3=技術)。</summary>
        const int MetricCount = 4;
        static readonly string[] MetricNames = { "スコア", "軍事力", "文化", "技術" };
        /// <summary>タブごとの説明行(グラフ上部に表示。すべて表示専用の読み取り)。</summary>
        static readonly string[] MetricFormulas =
        {
            "スコア = Σ人口×3 + 都市数×8 + 技術数×5",
            "軍事力 = Σ戦闘ユニットの(戦闘力+遠隔戦闘力)×HP/100",
            "文化 = 累計文化ポイント",
            "技術 = 研究済みの技術数",
        };

        GameState state;
        GameObject panel;
        RawImage graphImage;
        Texture2D graphTexture;
        Text rangeText;
        Text formulaText;
        readonly List<Text> legendTexts = new List<Text>();
        readonly Button[] tabButtons = new Button[MetricCount];
        /// <summary>表示中の指標(タブで切り替える。記録自体は常に全指標)。</summary>
        int selectedMetric;

        /// <summary>指標ごと×プレイヤーごとの履歴(state.Players と同じ並び。滅亡後も列を揃える
        /// ため全員毎ターン記録。300サンプル×4指標×最大8文明のintで上限約38KB)。</summary>
        readonly List<List<int>>[] metricSamples = new List<List<int>>[MetricCount];
        /// <summary>記録したターン番号(metricSamples の列と対応)。</summary>
        readonly List<int> sampleTurns = new List<int>();
        int lastSampledTurn = int.MinValue;
        bool textureDirty;

        public bool IsVisible => panel != null && panel.activeSelf;

        /// <summary>UI構築と初期サンプル。UIManager.Init から新しいゲームごとに呼ばれる。</summary>
        public void Init(GameState s)
        {
            state = s;
            sampleTurns.Clear();
            lastSampledTurn = int.MinValue;
            for (int m = 0; m < MetricCount; m++)
            {
                if (metricSamples[m] == null) metricSamples[m] = new List<List<int>>();
                metricSamples[m].Clear();
                if (state != null)
                    for (int i = 0; i < state.Players.Count; i++)
                        metricSamples[m].Add(new List<int>());
            }

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
                new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(470f, 382f));

            var title = UIStyle.CreateText(panel.transform, "Title", "戦況グラフ", 18,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-72f, 24f));

            // 指標タブ(2026-07-22 追加): スコア/軍事力/文化/技術。切替は再描画のみ。
            for (int m = 0; m < MetricCount; m++)
            {
                int metric = m;   // クロージャ用コピー
                var tab = UIStyle.CreateButton(panel.transform, "MetricTab" + m, MetricNames[m], 13,
                    () => SelectMetric(metric));
                UIStyle.SetRect(tab.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(23f + m * 108f, -34f), new Vector2(100f, 26f));
                tabButtons[m] = tab;
            }
            UpdateTabVisuals();

            formulaText = UIStyle.CreateText(panel.transform, "Formula",
                MetricFormulas[selectedMetric], 12,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(formulaText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(-40f, 18f));

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
                new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(430f, 190f));

            rangeText = UIStyle.CreateText(panel.transform, "Range", "", 12,
                TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.SetRect(rangeText.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -276f), new Vector2(220f, 18f));

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
                        new Vector2(0f, 1f), new Vector2(20f + col * 218f, -296f - row * 19f),
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
            for (int m = 0; m < MetricCount; m++)
            {
                var series = metricSamples[m];
                if (series == null) continue;
                for (int i = 0; i < series.Count && i < state.Players.Count; i++)
                    series[i].Add(MetricValueOf(state.Players[i], m));
            }
            while (sampleTurns.Count > MaxSamples)
            {
                sampleTurns.RemoveAt(0);
                for (int m = 0; m < MetricCount; m++)
                {
                    var series = metricSamples[m];
                    if (series == null) continue;
                    for (int i = 0; i < series.Count; i++)
                        if (series[i].Count > 0) series[i].RemoveAt(0);
                }
            }
            textureDirty = true;
        }

        /// <summary>指標の現在値(2026-07-22 追加。すべて読み取りのみでシミュレーションには影響しない)。</summary>
        static int MetricValueOf(Player p, int metric)
        {
            switch (metric)
            {
                case 1: return p.MilitaryPower();
                case 2: return p.TotalCulture;
                case 3: return p.KnownTechs != null ? p.KnownTechs.Count : 0;
                default: return ScoreOf(p);
            }
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

        /// <summary>タブで指標を切り替える(記録済みバッファからの再描画のみ。2026-07-22 追加)。</summary>
        void SelectMetric(int metric)
        {
            if (metric < 0 || metric >= MetricCount) return;
            selectedMetric = metric;
            if (formulaText != null) formulaText.text = MetricFormulas[metric];
            UpdateTabVisuals();
            Redraw();
            RefreshLegend();
        }

        /// <summary>選択中タブの強調表示(背景をハイライト色・ラベルをアクセント色へ)。</summary>
        void UpdateTabVisuals()
        {
            for (int m = 0; m < tabButtons.Length; m++)
            {
                var b = tabButtons[m];
                if (b == null) continue;
                var cb = b.colors;
                cb.normalColor = m == selectedMetric ? UIStyle.ButtonHover : UIStyle.ButtonNormal;
                cb.selectedColor = cb.normalColor;
                b.colors = cb;
                var label = UIStyle.ButtonLabel(b);
                if (label != null) label.color = m == selectedMetric ? UIStyle.Accent : UIStyle.TextMain;
            }
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
            var samples = metricSamples[selectedMetric];   // 表示中の指標の履歴(2026-07-22 追加)
            if (n > 0 && samples != null)
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
                    var samples = metricSamples[selectedMetric];   // 表示中の指標の最新値(2026-07-22 追加)
                    int value = samples != null && i < samples.Count && samples[i].Count > 0
                        ? samples[i][samples[i].Count - 1]
                        : 0;
                    t.text = $"■ {p.NameJa}  {value}";
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
