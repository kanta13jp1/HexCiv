using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HexCiv.UI
{
    /// <summary>
    /// 「成長の記録」閲覧画面(2026-07-27 Claude Code 追加)。
    ///
    /// タイトル画面の「今回の更新」カードが**前回起動からの差分**しか出さないのに対し、
    /// この画面は <see cref="GrowthHistory"/> に積み上がった記録を通しで見せる。
    /// 上段が「世界の大きさ」の推移グラフ、下段が更新履歴の一覧(ページ送り)。
    ///
    /// 契約:
    /// - 自前の Canvas(sortingOrder 210 = タイトルの 200 より手前)を持つ独立したオーバーレイ。
    ///   タイトル画面の階層には一切触らないので、閉じれば完全に元通りになる。
    /// - 開けない・記録が壊れている等の失敗で**タイトル画面を巻き込まない**
    ///   (呼び出し側は <see cref="Open"/> の戻り値 null を見て黙って何もしないだけでよい)。
    /// - シミュレーションには関与しない。Core/ を読まない・書かない。
    /// </summary>
    public class GrowthHistoryView : MonoBehaviour
    {
        // ---- レイアウト定数 ----
        const float PanelWidth = 880f;
        const float PanelHeight = 580f;
        const float Pad = 22f;
        const float HeaderHeight = 34f;
        const float SummaryHeight = 22f;
        const float ChartHeight = 158f;
        const float RowHeight = 30f;
        const int RowsPerPage = 8;
        const float FooterHeight = 30f;

        static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.72f);
        static readonly Color PanelBg = new Color(0.07f, 0.09f, 0.13f, 0.98f);
        static readonly Color ChartBg = new Color(0.11f, 0.14f, 0.19f, 1f);
        static readonly Color BarColor = new Color(0.44f, 1f, 0.62f, 0.92f);
        static readonly Color BarOriginColor = new Color(0.55f, 0.62f, 0.75f, 0.85f);
        static readonly Color DeltaColor = new Color(0.44f, 1f, 0.62f, 1f);
        static readonly Color RowAltBg = new Color(1f, 1f, 1f, 0.035f);

        /// <summary>開いている唯一のインスタンス(二重に開かないための番人)。</summary>
        static GrowthHistoryView open;

        List<GrowthRecord> records;
        GameObject listRoot;
        Text pageLabel;
        Button prevButton;
        Button olderButton;
        int page;          // 0 = 最新ページ
        int pageCount = 1;

        /// <summary>現在開いているか。</summary>
        public static bool IsOpen { get { return open != null; } }

        /// <summary>
        /// 画面を開く。すでに開いていれば何もせず既存のインスタンスを返す。
        /// 生成のどこかで失敗した場合は**作りかけを片付けて null を返す**
        /// (呼び出し側のタイトル画面は従来どおり動き続ける)。
        /// </summary>
        public static GrowthHistoryView Open()
        {
            if (open != null) return open;
            GameObject host = null;
            try
            {
                host = new GameObject("GrowthHistoryView");
                var view = host.AddComponent<GrowthHistoryView>();
                view.Build();
                open = view;
                return view;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[GrowthHistoryView] 成長の記録を開けませんでした: " + ex.Message);
                if (host != null) Destroy(host);
                return null;
            }
        }

        /// <summary>
        /// 開いていれば閉じる。開いていなければ何もしない。
        /// タイトル画面がゲームへ遷移するときに呼び、記録画面がゲーム上に残るのを防ぐ。
        /// </summary>
        public static void CloseIfOpen()
        {
            if (open != null) open.Close();
        }

        /// <summary>画面を閉じる(GameObject ごと破棄する)。</summary>
        public void Close()
        {
            if (open == this) open = null;
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }

        void Update()
        {
            // Esc で閉じる。タイトル画面側の Esc(=そのまま遊ぶ)より手前で消費したいので、
            // この画面が開いている間はタイトル側が Esc を見ないよう TitleScreen 側で
            // IsOpen を確認している。
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        // ==================================================================
        // 構築
        // ==================================================================

        void Build()
        {
            records = GrowthHistory.Load();

            // 記録がまだ1件も無いなら、その時点の総数を「起点」として残す。
            // 基準値が既にあるのに履歴が空、という状態は普通に起こる(この仕組みが入る前から
            // 遊んでいた場合など)。起点を置かないと、次に何かが増えるまでこの画面が
            // ずっと空のままになり、「育っていくのを見る」入口として機能しない。
            if (records.Count == 0)
            {
                try
                {
                    // ComputeOnly は読むだけで基準値を動かさない(起動中はキャッシュを返す)
                    if (GrowthHistory.EnsureOrigin(UpdateDigest.ComputeOnly()))
                        records = GrowthHistory.Load();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[GrowthHistoryView] 起点を記録できませんでした: " + ex.Message);
                }
            }

            var cgo = new GameObject("GrowthHistoryCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;   // タイトル画面(200)より手前

            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("GrowthEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }

            // 背面の暗幕。クリックで閉じる(下のタイトル画面へクリックを通さない役目も兼ねる)
            var backdrop = UIStyle.CreatePanel(cgo.transform, "Backdrop", Backdrop);
            UIStyle.StretchFull(backdrop);
            var backdropButton = backdrop.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.onClick.AddListener(Close);

            var panel = UIStyle.CreatePanel(cgo.transform, "Panel", PanelBg);
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, PanelHeight));
            // パネル自体はクリックを受け止める(暗幕まで貫通して閉じてしまうのを防ぐ)
            var panelBlocker = panel.AddComponent<Button>();
            panelBlocker.transition = Selectable.Transition.None;

            var accent = UIStyle.CreatePanel(panel.transform, "AccentBar", UIStyle.Accent);
            var accentImage = accent.GetComponent<Image>();
            if (accentImage != null) accentImage.raycastTarget = false;
            UIStyle.SetRect(accent, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3f, 0f));

            float y = Pad;

            var header = UIStyle.CreateText(panel.transform, "Header", "成長の記録", 22,
                TextAnchor.MiddleLeft, UIStyle.Accent);
            header.fontStyle = FontStyle.Bold;
            Place(header.gameObject, y, HeaderHeight);

            var close = UIStyle.CreateButton(panel.transform, "CloseButton", "閉じる", 14, Close);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-Pad, -y), new Vector2(92f, HeaderHeight));
            y += HeaderHeight + 4f;

            var summary = UIStyle.CreateText(panel.transform, "Summary", BuildSummaryJa(), 13,
                TextAnchor.MiddleLeft, UIStyle.TextDim);
            Place(summary.gameObject, y, SummaryHeight);
            y += SummaryHeight + 10f;

            BuildChart(panel.transform, y);
            y += ChartHeight + 14f;

            listRoot = UIStyle.CreateContainer(panel.transform, "List");
            UIStyle.SetRect(listRoot, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -y),
                new Vector2(-Pad * 2f, RowsPerPage * RowHeight));
            var listRect = (RectTransform)listRoot.transform;
            listRect.anchoredPosition = new Vector2(0f, -y);
            y += RowsPerPage * RowHeight + 6f;

            BuildFooter(panel.transform, y);
            RefreshList();
        }

        /// <summary>パネル上端からの距離 topOffset に、左右 Pad の余白で子を置く。</summary>
        void Place(GameObject go, float topOffset, float height)
        {
            UIStyle.SetRect(go, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -topOffset), new Vector2(-Pad * 2f, height));
        }

        string BuildSummaryJa()
        {
            if (records.Count == 0) return "まだ記録がありません";

            var newest = records[records.Count - 1];
            int updates = 0;
            for (int i = 0; i < records.Count; i++) { if (!records[i].IsOrigin) updates++; }

            string since = GrowthHistory.FormatAtJa(records[0].AtIso);
            int growth = GrowthHistory.GrowthSinceOldest(records);

            string head = since + " から " + updates + " 回の更新";
            if (growth > 0) head += " / この間に +" + Num(growth);
            head += "   —   今の世界: 台帳 " + Num(newest.LedgerTotal)
                  + " ・ 技術/ユニット/建物 " + Num(newest.RulesTotal)
                  + " ・ パネル " + Num(newest.Panels)
                  + " ・ 仕組み " + Num(newest.CoreTypes);
            return head;
        }

        // ==================================================================
        // 推移グラフ
        // ==================================================================

        /// <summary>
        /// 「世界の大きさ」(台帳+規則+パネル+仕組み)の推移を棒グラフで描く。
        ///
        /// 縦軸は 0 始まりではなく**記録中の最小値の少し下**を底にする。総数が1400前後で
        /// 増分が数十という比率のため、0 始まりだと成長が視覚的に潰れてしまうため。
        /// ただし切り詰めた軸は誤読を招くので、**上端と下端の実数値をグラフ内に明示**する。
        /// </summary>
        void BuildChart(Transform parent, float topOffset)
        {
            var chart = UIStyle.CreatePanel(parent, "Chart", ChartBg);
            var chartImage = chart.GetComponent<Image>();
            if (chartImage != null) chartImage.raycastTarget = false;
            Place(chart, topOffset, ChartHeight);

            if (records.Count == 0)
            {
                var empty = UIStyle.CreateText(chart.transform, "ChartEmpty",
                    "ゲームを起動して新しい要素が増えると、ここに成長が積み上がっていきます。", 13,
                    TextAnchor.MiddleCenter, UIStyle.TextDim);
                UIStyle.StretchFull(empty.gameObject, 12f);
                return;
            }

            int min = int.MaxValue, max = int.MinValue;
            for (int i = 0; i < records.Count; i++)
            {
                int m = records[i].Magnitude;
                if (m < min) min = m;
                if (m > max) max = m;
            }

            // 底は最小値より少し下に取り、最小値の棒も存在が見えるようにする
            int span = Mathf.Max(max - min, 1);
            float floor = min - span * 0.25f;
            float ceiling = max + span * 0.10f;
            float range = Mathf.Max(ceiling - floor, 1f);

            const float insetX = 14f;
            const float insetTop = 20f;
            const float insetBottom = 20f;
            float plotHeight = ChartHeight - insetTop - insetBottom;
            // 実効プロット幅(パネル幅 - 左右パディング - グラフ内左右余白)
            float plotWidth = PanelWidth - Pad * 2f - insetX * 2f;
            int count = records.Count;
            float slot = plotWidth / count;
            float barWidth = Mathf.Max(slot - 2f, 2f);

            for (int i = 0; i < count; i++)
            {
                var record = records[i];
                float t = (record.Magnitude - floor) / range;
                float h = Mathf.Max(plotHeight * Mathf.Clamp01(t), 2f);

                var bar = UIStyle.CreatePanel(chart.transform, "Bar" + i,
                    record.IsOrigin ? BarOriginColor : BarColor);
                var barImage = bar.GetComponent<Image>();
                if (barImage != null) barImage.raycastTarget = false;
                UIStyle.SetRect(bar, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(insetX + slot * i + (slot - barWidth) * 0.5f, insetBottom),
                    new Vector2(barWidth, h));
            }

            // 軸の実数値(切り詰めた軸であることを隠さない)
            var top = UIStyle.CreateText(chart.transform, "ChartMax", Num(max), 11,
                TextAnchor.UpperRight, UIStyle.TextDim);
            UIStyle.SetRect(top.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-8f, -4f), new Vector2(120f, 16f));

            var bottom = UIStyle.CreateText(chart.transform, "ChartMin",
                "最小 " + Num(min) + "(縦軸は0始まりではありません)", 11,
                TextAnchor.LowerLeft, UIStyle.TextDim);
            UIStyle.SetRect(bottom.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(8f, 3f), new Vector2(400f, 16f));

            var caption = UIStyle.CreateText(chart.transform, "ChartCaption",
                "世界の大きさ(台帳+技術/ユニット/建物+パネル+仕組み)の推移", 11,
                TextAnchor.UpperLeft, UIStyle.TextDim);
            UIStyle.SetRect(caption.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(8f, -4f), new Vector2(460f, 16f));
        }

        // ==================================================================
        // 一覧(ページ送り)
        // ==================================================================

        void BuildFooter(Transform parent, float topOffset)
        {
            var footer = UIStyle.CreateContainer(parent, "Footer");
            Place(footer, topOffset, FooterHeight);

            olderButton = UIStyle.CreateButton(footer.transform, "OlderButton", "◀ 古い", 13,
                delegate { ChangePage(1); });
            UIStyle.SetRect(olderButton.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(96f, FooterHeight - 4f));

            prevButton = UIStyle.CreateButton(footer.transform, "NewerButton", "新しい ▶", 13,
                delegate { ChangePage(-1); });
            UIStyle.SetRect(prevButton.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(104f, 0f), new Vector2(96f, FooterHeight - 4f));

            pageLabel = UIStyle.CreateText(footer.transform, "PageLabel", "", 12,
                TextAnchor.MiddleRight, UIStyle.TextDim);
            UIStyle.SetRect(pageLabel.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(320f, FooterHeight - 4f));
        }

        void ChangePage(int direction)
        {
            page = Mathf.Clamp(page + direction, 0, Mathf.Max(pageCount - 1, 0));
            RefreshList();
        }

        /// <summary>現在ページの行を作り直す。ページは 0 = 最新側。</summary>
        void RefreshList()
        {
            if (listRoot == null) return;

            for (int i = listRoot.transform.childCount - 1; i >= 0; i--)
                Destroy(listRoot.transform.GetChild(i).gameObject);

            pageCount = Mathf.Max(1, Mathf.CeilToInt(records.Count / (float)RowsPerPage));
            page = Mathf.Clamp(page, 0, pageCount - 1);

            if (records.Count == 0)
            {
                var empty = UIStyle.CreateText(listRoot.transform, "Empty",
                    "記録はまだありません。次に新しい要素が増えたときから積み上がっていきます。", 13,
                    TextAnchor.UpperLeft, UIStyle.TextDim);
                UIStyle.SetRect(empty.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, RowHeight));
                if (pageLabel != null) pageLabel.text = "";
                UpdateFooterButtons();
                return;
            }

            // 新しい順に並べたいので末尾から数える
            int endExclusive = records.Count - page * RowsPerPage;
            int start = Mathf.Max(endExclusive - RowsPerPage, 0);
            int row = 0;
            for (int i = endExclusive - 1; i >= start; i--, row++)
                BuildRow(records[i], row);

            if (pageLabel != null)
            {
                pageLabel.text = "全 " + records.Count + " 件"
                    + (pageCount > 1 ? "(" + (page + 1) + " / " + pageCount + " ページ)" : "");
            }
            UpdateFooterButtons();
        }

        void UpdateFooterButtons()
        {
            if (olderButton != null) olderButton.interactable = page < pageCount - 1;
            if (prevButton != null) prevButton.interactable = page > 0;
        }

        void BuildRow(GrowthRecord record, int row)
        {
            float y = row * RowHeight;

            var container = row % 2 == 1
                ? UIStyle.CreatePanel(listRoot.transform, "Row" + row, RowAltBg)
                : UIStyle.CreateContainer(listRoot.transform, "Row" + row);
            var rowImage = container.GetComponent<Image>();
            if (rowImage != null) rowImage.raycastTarget = false;
            UIStyle.SetRect(container, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -y), new Vector2(0f, RowHeight));

            var when = UIStyle.CreateText(container.transform, "When",
                GrowthHistory.FormatAtJa(record.AtIso), 13, TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.SetRect(when.gameObject, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(96f, 0f));

            string deltaJa = record.IsOrigin ? "起点" : "+" + Num(record.TotalDelta);
            var delta = UIStyle.CreateText(container.transform, "Delta", deltaJa, 14,
                TextAnchor.MiddleRight, record.IsOrigin ? UIStyle.TextDim : DeltaColor);
            delta.fontStyle = record.IsOrigin ? FontStyle.Normal : FontStyle.Bold;
            UIStyle.SetRect(delta.gameObject, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(104f, 0f), new Vector2(72f, 0f));

            var summary = UIStyle.CreateText(container.transform, "Summary", DescribeJa(record), 13,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(summary.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0.5f), new Vector2(188f, 0f), new Vector2(-320f, 0f));

            var total = UIStyle.CreateText(container.transform, "Total",
                "世界の大きさ " + Num(record.Magnitude), 12, TextAnchor.MiddleRight, UIStyle.TextDim);
            UIStyle.SetRect(total.gameObject, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(160f, 0f));
        }

        /// <summary>
        /// 1件の記録を1行の日本語にする。**何が増えたかを名前で言う**のがこのメソッドの主眼で、
        /// 「新しいパネル 1」ではなく「新しいパネル: 領土の変遷」と出す。
        ///
        /// 名前を持たない古い形式の記録(名前を残す前に保存されたもの)は件数表示へ自動的に
        /// 落ちるので、既存の記録が読めなくなることはない。
        /// </summary>
        static string DescribeJa(GrowthRecord record)
        {
            if (record.IsOrigin)
            {
                string head = string.IsNullOrEmpty(record.SummaryJa)
                    ? "ここから記録を始めました" : record.SummaryJa;
                return head + "(世界の大きさ " + Num(record.Magnitude) + ")";
            }

            var parts = new List<string>();
            if (record.LedgerDelta > 0) parts.Add("台帳 +" + Num(record.LedgerDelta));
            if (record.RulesDelta > 0) parts.Add("技術・ユニット・建物 +" + Num(record.RulesDelta));

            if (record.NewPanelNames != null && record.NewPanelNames.Count > 0)
                parts.Add("新しいパネル: " + JoinPanelLabels(record.NewPanelNames));
            else if (record.NewPanels > 0)
                parts.Add("新しいパネル " + record.NewPanels);          // 名前を持たない旧記録

            if (record.NewCoreTypeNames != null && record.NewCoreTypeNames.Count > 0)
                parts.Add("新しい仕組み: " + JoinNames(record.NewCoreTypeNames));
            else if (record.NewCoreTypes > 0)
                parts.Add("新しい仕組み " + record.NewCoreTypes);        // 名前を持たない旧記録

            if (parts.Count == 0)
                return string.IsNullOrEmpty(record.SummaryJa) ? "更新" : record.SummaryJa;
            return string.Join(" / ", parts.ToArray());
        }

        /// <summary>パネル型名を日本語表示名へ直して連結する(上限を超えたら「ほか N」)。</summary>
        static string JoinPanelLabels(List<string> names)
        {
            var labels = new List<string>();
            for (int i = 0; i < names.Count; i++) labels.Add(UpdateDigest.PanelLabelJa(names[i]));
            return JoinNames(labels);
        }

        /// <summary>名前を「A・B・C ほか2」の形に連結する(行からあふれさせない)。</summary>
        static string JoinNames(List<string> names)
        {
            const int MaxShown = 3;
            int shown = Mathf.Min(names.Count, MaxShown);
            var head = new string[shown];
            for (int i = 0; i < shown; i++) head[i] = names[i];
            string joined = string.Join("・", head);
            int rest = names.Count - shown;
            return rest > 0 ? joined + " ほか" + rest : joined;
        }

        static string Num(int value)
        {
            return value.ToString("#,##0");
        }
    }
}
