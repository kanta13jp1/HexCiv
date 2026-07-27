using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// 「今回の更新」差分検出エンジン(2026-07-24 Claude Code 第19弾 追加)。
    ///
    /// このプロジェクトの価値は「育っていくのを見ること」にあるが、台帳1303件・
    /// Coreシステム10種・UIパネル14種という規模になると、新しく増えたものが
    /// 既存の物量に埋もれて見えなくなる。そこで起動時に「前回起動からの差分」を
    /// 実行時カウントで自動検出し、タイトル画面のカードとして提示する。
    ///
    /// 本ファイルは**検出のみ**で、描画は一切行わない(カードの描画は TitleScreen 側)。
    ///
    /// 設計方針:
    ///   1. Core/ には一切触れない。読むだけ(GlobalHistoryIndex / GameRules /
    ///      TechnologyCatalog の公開静的APIのみ)。シミュレーションへは無干渉で、
    ///      乱数も一切消費しない。
    ///   2. **一覧をハードコードしない**。UIパネル数とCoreの仕組み数は
    ///      System.Reflection で現アセンブリの型を走査して数える。これにより
    ///      Codex が COLLABORATION.md に未記載のまま機能を追加しても自動で拾える
    ///      (実際に未記載のまま入った機能が複数あったため、この方式が必要)。
    ///   3. **例外を外へ出さない**。カタログ・リフレクション・PlayerPrefs の
    ///      すべてのアクセスを try/catch で包む。Codex 側の変更や壊れた型が
    ///      あってもタイトル画面(=起動)を巻き込んで落とすことは絶対にない。
    ///      失敗した系統は静かに欠測扱いにするだけ。
    ///   4. **毎フレーム処理なし**。MonoBehaviour ですらない純静的クラスで、
    ///      リフレクション結果と算出済み差分は static にキャッシュして
    ///      起動1回分しか計算しない。
    ///   5. ヘッドレス(batchmode / HumanPlayer null / シーンにパネルが1枚も無い)
    ///      でも例外なく完走する。TryOpenPanel は対象が見つからなければ false を返すだけ。
    ///
    /// == 算出と確定を分けている理由(重要) ==
    /// ComputeOnly() は**読むだけ**で PlayerPrefs を一切書かない。書き込みは
    /// Commit(result) を明示的に呼んだ時だけ行う。これはカードの表示中に例外や
    /// 強制終了が起きた場合に「差分を見せないまま基準値だけ進んで、増えたものが
    /// 永久に見えなくなる」事故を防ぐため。呼び出し側(タイトル画面)の推奨手順は:
    ///
    ///     var digest = UpdateDigest.ComputeOnly();      // 起動直後
    ///     if (digest.HasChanges) { カードを表示する… }
    ///     UpdateDigest.Commit(digest);                  // 表示が成立した後で確定
    ///
    /// 一気に済ませたい場合のために ComputeAndCommit() も用意してあるが、
    /// 上記の理由から**タイトル画面では ComputeOnly + Commit の2段を推奨**する。
    ///
    /// == PlayerPrefs のキー ==
    /// すべて "HexCiv.Digest.&lt;metricKey&gt;" (int)。metricKey は永続的な識別子なので
    /// 一度決めたら改名しない(改名すると「新規指標」と誤検出され、次回起動で
    /// 巨大な偽の差分カードが出る)。metricKey の体系:
    ///     __baseline            … 1 = 一度でも確定済み(初回起動判定に使う)
    ///     ledger.total          … 世界史台帳の総件数
    ///     ledger.categories     … 台帳の分類数
    ///     cat.&lt;分類ID&gt;          … 分類ごとの件数(GlobalHistoryIndexEntry.Id をそのまま使う)
    ///     rules.techs / rules.units / rules.buildings
    ///     core.systems / core.catalogs        … 集計値(行を出す)
    ///     core.type.&lt;型名&gt;      … 存在フラグ(行は出さない。新規Core型の特定に使う)
    ///     ui.panels             … 集計値(行は出さない。内訳の方が有用なため)
    ///     ui.panel.&lt;型名&gt;       … 存在フラグ(新規パネルの特定とジャンプに使う)
    ///
    /// == 差分の出し方 ==
    /// ・**初回起動(基準値が無い)** は HasChanges=false で行を1本も出さず、
    ///   基準値の記録だけ行う。初回に「+1303」の偽カードを出さないため。
    /// ・**増えた指標だけ**が行になる。減った指標は静かに記録するだけで行にしない
    ///   (Codex のリファクタで一時的に数が減ってもノイズを出さない)。
    /// ・分類の内訳行は増分の大きい順に最大 MaxCategoryLines 本まで(物量で埋もれさせない)。
    /// </summary>
    public static class UpdateDigest
    {
        // ==================================================================
        // 定数
        // ==================================================================

        /// <summary>PlayerPrefs キーの共通接頭辞。</summary>
        const string PrefsPrefix = "HexCiv.Digest.";

        /// <summary>初回起動判定に使う番人キー(値1 = 一度でも Commit 済み)。</summary>
        const string BaselineKey = "__baseline";

        /// <summary>台帳の内訳行の上限(増分の大きい順)。カードが縦に伸びすぎるのを防ぐ。</summary>
        const int MaxCategoryLines = 8;

        /// <summary>パネルを開くために探す公開メソッド名(先に見つかった1つだけを呼ぶ)。</summary>
        static readonly string[] OpenMethodNames = { "Show", "Toggle", "Open", "OpenAndPlayFromStart" };

        /// <summary>既知パネルの日本語名。未知の型名はそのまま表示名に使う(=Codexの新パネルも動く)。</summary>
        static readonly Dictionary<string, string> PanelLabels = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "AchievementPanel",        "実績" },
            { "AdministrationPanel",     "国家運営" },
            { "ChroniclePanel",          "年表" },
            { "CulturePanel",            "文化" },
            { "HistoricalCampaignPanel", "史実キャンペーン" },
            { "LegacyPanel",             "遺産・偉人・作品" },
            { "LogisticsPanel",          "兵站" },
            { "MarketPanel",             "市場" },
            { "MinimapPanel",            "ミニマップ" },
            { "PoliticsPanel",           "政治" },
            { "PopulationPanel",         "人口・社会" },
            { "ScoreGraphPanel",         "戦況グラフ" },
            { "TimelapsePanel",          "領土の変遷" },
            { "UrukRegionalPanel",       "南メソポタミア地域管理" },
            { "WorldHistoryPanel",       "世界史図鑑" },
        };

        // ==================================================================
        // キャッシュ(起動1回分だけ計算する)
        // ==================================================================

        static bool scanned;
        static List<string> panelTypeNames;
        static Dictionary<string, Type> panelTypesByName;
        static List<string> coreSystemNames;
        static List<string> coreCatalogNames;

        /// <summary>算出済みの差分(起動中は同じスナップショットを返し続ける)。</summary>
        static DigestResult cachedResult;

        // ==================================================================
        // 公開API
        // ==================================================================

        /// <summary>
        /// 現在の規模を数え、前回起動時の記録と比較して差分を返す。**PlayerPrefs は書かない**。
        /// 起動中に何度呼んでも同じインスタンス(同じスナップショット)を返す。
        /// どんな失敗をしても例外は投げず、最悪でも「差分なし」の結果を返す。
        /// </summary>
        public static DigestResult ComputeOnly()
        {
            if (cachedResult != null) return cachedResult;
            DigestResult result;
            try
            {
                result = ComputeInternal();
            }
            catch (Exception ex)
            {
                // ここに来るのは想定外だが、起動を巻き込まないことを最優先にする。
                Debug.LogWarning("UpdateDigest: 差分算出に失敗したため差分なしとして続行します: " + ex.Message);
                result = new DigestResult();
                result.IsFirstRun = false;
                result.HasChanges = false;
                // Snapshot を null にして Commit を無効化する。中途半端な基準値を書くと
                // 次回起動で「全指標が新規」に見え、巨大な偽カードが出てしまうため。
                result.Snapshot = null;
            }
            cachedResult = result;
            return result;
        }

        /// <summary>
        /// ComputeOnly() の結果を PlayerPrefs へ確定する(次回起動の基準値になる)。
        /// カードを表示し終えてから呼ぶこと。二重呼び出しは無害(同じ値を書くだけ)。
        /// </summary>
        /// <returns>書き込めたら true。null や失敗時は false(例外は投げない)。</returns>
        public static bool Commit(DigestResult result)
        {
            if (result == null || result.Snapshot == null) return false;
            try
            {
                foreach (var pair in result.Snapshot)
                {
                    if (string.IsNullOrEmpty(pair.Key)) continue;
                    PlayerPrefs.SetInt(PrefsPrefix + pair.Key, pair.Value);
                }
                PlayerPrefs.SetInt(PrefsPrefix + BaselineKey, 1);
                PlayerPrefs.Save();
                result.Committed = true;

                // 「成長の記録」へ1件残す(2026-07-27 追加)。基準値の前進と1対1に対応させたい
                // ので、確定に成功したこの位置でのみ呼ぶ。記録の失敗は確定の成否に影響させない
                // (GrowthHistory.Record は例外を投げず、失敗しても false を返すだけ)。
                GrowthHistory.Record(result, BuildSummaryJa(result));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("UpdateDigest: 基準値の保存に失敗しました: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 算出と確定を一度に行う簡便版。表示前に基準値が進むため、表示が失敗すると
        /// その回の差分は失われる。タイトル画面では ComputeOnly + Commit の2段を推奨。
        /// </summary>
        public static DigestResult ComputeAndCommit()
        {
            var result = ComputeOnly();
            Commit(result);
            return result;
        }

        /// <summary>
        /// パネル型名(DigestLine.PanelKey / DigestResult.NewPanelKeys の値)から
        /// 実行中のパネルを探して開く。公開の Show / Toggle / Open のいずれかを呼ぶ。
        /// 解決できなければ**静かに false**を返す(ヘッドレスや未生成のパネルを含む)。
        /// 例外は投げない。
        /// </summary>
        public static bool TryOpenPanel(string panelKey)
        {
            if (string.IsNullOrEmpty(panelKey)) return false;
            try
            {
                EnsureScan();
                if (panelTypesByName == null) return false;
                Type type;
                if (!panelTypesByName.TryGetValue(panelKey, out type) || type == null) return false;

                var instance = UnityEngine.Object.FindFirstObjectByType(type);
                if (instance == null) return false;

                for (int i = 0; i < OpenMethodNames.Length; i++)
                {
                    var method = type.GetMethod(OpenMethodNames[i],
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                        null, Type.EmptyTypes, null);
                    if (method == null || method.ReturnType != typeof(void)) continue;
                    method.Invoke(instance, null);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                // 開けないこと自体は異常ではない(未初期化・ヘッドレス等)。静かに失敗する。
                return false;
            }
        }

        /// <summary>パネル型名の日本語表示名。未知の型名はそのまま返す(新パネルでも空にならない)。</summary>
        public static string PanelLabelJa(string panelKey)
        {
            if (string.IsNullOrEmpty(panelKey)) return "";
            string label;
            if (PanelLabels.TryGetValue(panelKey, out label)) return label;
            return panelKey;
        }

        /// <summary>実行時に検出できたUIパネル型名の一覧(順序は序数昇順で決定的)。複製を返す。</summary>
        public static List<string> AllPanelKeys()
        {
            EnsureScan();
            return panelTypeNames != null ? new List<string>(panelTypeNames) : new List<string>();
        }

        /// <summary>
        /// ログ・テレメトリ用の1行日本語要約。例:「台帳 +24 / 新しいパネル 1」。
        /// 差分が無い場合や null でも必ず非 null の文字列を返す。
        /// </summary>
        public static string BuildSummaryJa(DigestResult result)
        {
            if (result == null) return "更新なし";
            if (result.IsFirstRun) return "初回起動のため差分なし(基準値を記録)";
            if (!result.HasChanges) return "更新なし";

            var parts = new List<string>();
            if (result.LedgerDelta > 0) parts.Add("台帳 +" + result.LedgerDelta);
            if (result.RulesDelta > 0) parts.Add("技術・ユニット・建物 +" + result.RulesDelta);
            if (result.NewPanelKeys != null && result.NewPanelKeys.Count > 0)
                parts.Add("新しいパネル " + result.NewPanelKeys.Count);
            if (result.NewCoreTypeKeys != null && result.NewCoreTypeKeys.Count > 0)
                parts.Add("新しい仕組み " + result.NewCoreTypeKeys.Count);

            if (parts.Count == 0) parts.Add("更新 " + Mathf.Max(result.TotalDelta, 1) + "件");
            return string.Join(" / ", parts);
        }

        // ==================================================================
        // 差分算出の本体
        // ==================================================================

        static DigestResult ComputeInternal()
        {
            var result = new DigestResult();
            bool firstRun = !HasPref(BaselineKey);
            result.IsFirstRun = firstRun;

            // ---- 現在値を集める(どの系統が失敗しても他は続行する) ----
            var numeric = new List<Metric>();
            var categories = new List<Metric>();
            CollectLedgerMetrics(numeric, categories);
            CollectRulesMetrics(numeric);
            CollectReflectionMetrics(numeric);

            // 集計指標のスナップショット(初回でも必ず記録する)
            for (int i = 0; i < numeric.Count; i++) result.Snapshot[numeric[i].Key] = numeric[i].Current;
            for (int i = 0; i < categories.Count; i++) result.Snapshot[categories[i].Key] = categories[i].Current;

            // Core 走査の世代。記録済みの世代が違う起動では Core 系を報告せず基準値化だけする。
            bool coreBaselineOnly = ReadPref(CoreScanGenerationKey) != CoreScanGeneration;
            result.Snapshot[CoreScanGenerationKey] = CoreScanGeneration;

            if (firstRun)
            {
                // 初回は基準値を作るだけ。巨大な偽カードを出さない。
                result.HasChanges = false;
                result.TotalDelta = 0;
                result.LedgerDelta = 0;
                result.RulesDelta = 0;
                return result;
            }

            // ---- 集計指標の比較 ----
            // 記録が無い集計指標は「新規に増えた」ではなく「まだ測っていなかった」なので、
            // 静かに基準値だけ作る(前回の集計が一部失敗していた場合や、将来ここへ指標を
            // 追加した場合に、いきなり「+1303」のような偽の巨大差分を出さないため)。
            // 個別の存在フラグ(ui.panel.* / core.type.*)はこの経路では行を作らず、
            // 後段の専用ループが名前付きで新規判定する。
            var headLines = new List<DigestLine>();
            for (int i = 0; i < numeric.Count; i++)
            {
                var metric = numeric[i];
                if (!HasPref(metric.Key)) continue;
                // 走査世代が変わった直後の Core 指標は、増えたのではなく「拾えるようになった」
                // だけなので報告しない(Snapshot には既に入っているので基準値化はされる)。
                if (coreBaselineOnly && metric.Key.StartsWith("core.", StringComparison.Ordinal))
                    continue;
                int previous = ReadPref(metric.Key);
                int delta = metric.Current - previous;
                if (delta > 0)
                {
                    if (metric.CountsToTotal) result.TotalDelta += delta;
                    if (metric.Key == LedgerTotalKey) result.LedgerDelta += delta;
                    else if (metric.IsRules) result.RulesDelta += delta;
                    if (!metric.Silent) headLines.Add(MakeLine(metric.LabelJa, previous, metric.Current, metric.PanelKey));
                }
                // 減少・同数は静かに記録するだけ(Snapshot には既に入っている)。
            }

            // ---- 分類の内訳(増分の大きい順に上限本数まで) ----
            // 新設された分類は初回だけ静かに基準値化する(件数は ledger.total 側の増分に
            // 含まれているので、見出しの数字は正しいまま)。
            var categoryLines = new List<RankedLine>();
            for (int i = 0; i < categories.Count; i++)
            {
                var metric = categories[i];
                if (!HasPref(metric.Key)) continue;
                int previous = ReadPref(metric.Key);
                int delta = metric.Current - previous;
                if (delta <= 0) continue;
                var ranked = new RankedLine();
                ranked.Order = i;
                ranked.Delta = delta;
                ranked.Line = MakeLine(metric.LabelJa, previous, metric.Current, metric.PanelKey);
                categoryLines.Add(ranked);
            }
            categoryLines.Sort(CompareRanked);

            // ---- 新パネル(存在フラグの有無で判定。集計値ではなく名前が取れるのが利点) ----
            var panelLines = new List<DigestLine>();
            if (panelTypeNames != null)
            {
                for (int i = 0; i < panelTypeNames.Count; i++)
                {
                    string name = panelTypeNames[i];
                    if (HasPref(PanelFlagKey(name))) continue;
                    result.NewPanelKeys.Add(name);
                    result.TotalDelta += 1;
                    panelLines.Add(MakeLine(NewPanelLineLabel(name), 0, 1, name));
                }
            }

            // ---- 新しいCoreの仕組み(名前だけ拾う。行は core.systems / core.catalogs の集計行が担当) ----
            // 走査世代が変わった直後は、既存の型が一斉に「新しい仕組み」として湧いてしまうので飛ばす。
            if (!coreBaselineOnly)
            {
                AppendNewCoreTypes(coreSystemNames, result);
                AppendNewCoreTypes(coreCatalogNames, result);
            }

            // ---- 行の並び(決定的): 集計 → 分類の内訳 → 新パネル(ジャンプ可能なので末尾) ----
            result.Lines.AddRange(headLines);
            int categoryCount = Mathf.Min(categoryLines.Count, MaxCategoryLines);
            for (int i = 0; i < categoryCount; i++) result.Lines.Add(categoryLines[i].Line);
            result.Lines.AddRange(panelLines);

            result.HasChanges = result.Lines.Count > 0;
            return result;
        }

        /// <summary>存在フラグが無いCore型を「新しい仕組み」として拾う。</summary>
        static void AppendNewCoreTypes(List<string> names, DigestResult result)
        {
            if (names == null) return;
            for (int i = 0; i < names.Count; i++)
            {
                if (HasPref(CoreFlagKey(names[i]))) continue;
                result.NewCoreTypeKeys.Add(names[i]);
            }
        }

        // ==================================================================
        // 現在値の収集
        // ==================================================================

        const string LedgerTotalKey = "ledger.total";

        /// <summary>
        /// Core 型の走査世代(2026-07-27 追加)。走査条件を変えて拾える型が変わったときに上げる。
        /// 記録済みの世代がこの値と違う起動では、Core 系の指標(core.systems / core.catalogs と
        /// core.type.* の存在フラグ)を**静かに基準値化するだけ**にし、差分としては報告しない。
        ///
        /// これが無いと、走査条件の修正だけで「新しい仕組み +24」のような**実際には何も増えて
        /// いない偽の更新**が既存ユーザーへ出てしまう(世代2 = static class を拾えるようにした修正)。
        /// </summary>
        const string CoreScanGenerationKey = "core.scan.generation";
        const int CoreScanGeneration = 2;

        /// <summary>世界史台帳(全地域)の総件数・分類数・分類ごとの件数。</summary>
        static void CollectLedgerMetrics(List<Metric> numeric, List<Metric> categories)
        {
            try
            {
                // GlobalHistoryIndex.AllRegions は "すべて"。WorldHistoryPanel も同じ経路を使う。
                var entries = GlobalHistoryIndex.Entries(GlobalHistoryIndex.AllRegions);
                if (entries == null) return;

                int total = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry == null) continue;
                    total += entry.Count;
                    if (string.IsNullOrEmpty(entry.Id)) continue;
                    categories.Add(Metric.Category("cat." + entry.Id,
                        string.IsNullOrEmpty(entry.NameJa) ? entry.Id : entry.NameJa,
                        entry.Count, "WorldHistoryPanel"));
                }

                // 台帳の増分だけを TotalDelta へ算入する(内訳は同じものの再掲なので二重計上しない)。
                numeric.Add(Metric.Counted(LedgerTotalKey, "世界史台帳", total, "WorldHistoryPanel"));
                numeric.Add(Metric.Plain("ledger.categories", "台帳の分類", entries.Count, "WorldHistoryPanel"));
            }
            catch (Exception ex)
            {
                // Codex 側のカタログ変更で落ちても起動は続ける。
                Debug.LogWarning("UpdateDigest: 世界史台帳の集計を省略しました: " + ex.Message);
            }
        }

        /// <summary>研究できる技術・ユニット種別・建物種別。</summary>
        static void CollectRulesMetrics(List<Metric> numeric)
        {
            try
            {
                var techs = TechnologyCatalog.All;
                if (techs != null) numeric.Add(Metric.Rules("rules.techs", "研究できる技術", techs.Count, "WorldHistoryPanel"));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("UpdateDigest: 技術数の集計を省略しました: " + ex.Message);
            }
            try
            {
                if (GameRules.Units != null) numeric.Add(Metric.Rules("rules.units", "ユニット種別", GameRules.Units.Count, null));
                if (GameRules.Buildings != null) numeric.Add(Metric.Rules("rules.buildings", "建物種別", GameRules.Buildings.Count, null));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("UpdateDigest: ユニット・建物数の集計を省略しました: " + ex.Message);
            }
        }

        /// <summary>UIパネル数・Coreシステム数・Coreカタログ数と、それぞれの存在フラグ。</summary>
        static void CollectReflectionMetrics(List<Metric> numeric)
        {
            EnsureScan();

            if (panelTypeNames != null)
            {
                // 集計行は出さない(「どのパネルが増えたか」の内訳行の方が有用なため)。
                numeric.Add(Metric.SilentMetric("ui.panels", panelTypeNames.Count));
                for (int i = 0; i < panelTypeNames.Count; i++)
                    numeric.Add(Metric.SilentMetric(PanelFlagKey(panelTypeNames[i]), 1));
            }

            if (coreSystemNames != null)
            {
                numeric.Add(Metric.Counted("core.systems", "Coreの仕組み(System)", coreSystemNames.Count, null));
                for (int i = 0; i < coreSystemNames.Count; i++)
                    numeric.Add(Metric.SilentMetric(CoreFlagKey(coreSystemNames[i]), 1));
            }

            if (coreCatalogNames != null)
            {
                numeric.Add(Metric.Counted("core.catalogs", "Coreの台帳(Catalog)", coreCatalogNames.Count, null));
                for (int i = 0; i < coreCatalogNames.Count; i++)
                    numeric.Add(Metric.SilentMetric(CoreFlagKey(coreCatalogNames[i]), 1));
            }
        }

        // ==================================================================
        // リフレクション走査(1回だけ・結果は static キャッシュ)
        // ==================================================================

        /// <summary>
        /// 現アセンブリの型を1回だけ走査し、UIパネルとCoreの System / Catalog を数える。
        /// 一覧をハードコードしないので、Codex が追加した型もそのまま検出される。
        /// ReflectionTypeLoadException でも取得できた型だけで続行する。
        /// </summary>
        static void EnsureScan()
        {
            if (scanned) return;
            scanned = true;

            panelTypeNames = new List<string>();
            panelTypesByName = new Dictionary<string, Type>(StringComparer.Ordinal);
            coreSystemNames = new List<string>();
            coreCatalogNames = new List<string>();

            Type[] types;
            try
            {
                types = typeof(UpdateDigest).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // 一部の型がロードできなくても、取得できた分だけで続行する(要素に null が混ざる)。
                types = ex.Types;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("UpdateDigest: 型の走査に失敗しました: " + ex.Message);
                return;
            }
            if (types == null) return;

            var monoBehaviourType = typeof(MonoBehaviour);
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (type == null) continue;
                try
                {
                    if (type.IsNested || type.IsInterface) continue;
                    // C# の `static class` は IL 上 abstract sealed になる。単純に IsAbstract で
                    // 弾くと Core の System / Catalog(すべて static class)が**1件も拾えない**
                    // (2026-07-27 実測: core.systems / core.catalogs が常に 0 だった)。
                    // 除きたいのは継承前提の抽象基底だけなので、sealed でない abstract に限る。
                    if (type.IsAbstract && !type.IsSealed) continue;
                    string ns = type.Namespace;
                    if (string.IsNullOrEmpty(ns)) continue;
                    string name = type.Name;
                    if (string.IsNullOrEmpty(name)) continue;

                    if (ns == "HexCiv.UI")
                    {
                        if (!name.EndsWith("Panel", StringComparison.Ordinal)) continue;
                        if (!monoBehaviourType.IsAssignableFrom(type)) continue;
                        if (panelTypesByName.ContainsKey(name)) continue;
                        panelTypesByName[name] = type;
                        panelTypeNames.Add(name);
                    }
                    else if (ns == "HexCiv.Core")
                    {
                        if (name.EndsWith("System", StringComparison.Ordinal)) coreSystemNames.Add(name);
                        else if (name.EndsWith("Catalog", StringComparison.Ordinal)) coreCatalogNames.Add(name);
                    }
                }
                catch (Exception)
                {
                    // 壊れた型1件で走査全体を止めない。
                }
            }

            // 走査順はランタイム依存なので、決定的な順序へ揃える。
            panelTypeNames.Sort(StringComparer.Ordinal);
            coreSystemNames.Sort(StringComparer.Ordinal);
            coreCatalogNames.Sort(StringComparer.Ordinal);
        }

        // ==================================================================
        // 補助
        // ==================================================================

        /// <summary>
        /// 新パネル行の表示名。既知の日本語名があるときだけ「〜パネル」を付ける
        /// (Codex が追加した未知のパネルは型名をそのまま出し、「TradeRoutePanelパネル」のような
        /// 重複表記にしない)。
        /// </summary>
        static string NewPanelLineLabel(string typeName)
        {
            string label;
            if (!string.IsNullOrEmpty(typeName) && PanelLabels.TryGetValue(typeName, out label))
                return label + "パネル";
            return typeName;
        }

        static string PanelFlagKey(string typeName) { return "ui.panel." + typeName; }
        static string CoreFlagKey(string typeName) { return "core.type." + typeName; }

        static bool HasPref(string metricKey)
        {
            try { return PlayerPrefs.HasKey(PrefsPrefix + metricKey); }
            catch (Exception) { return false; }
        }

        static int ReadPref(string metricKey)
        {
            try { return PlayerPrefs.GetInt(PrefsPrefix + metricKey, 0); }
            catch (Exception) { return 0; }
        }

        static DigestLine MakeLine(string labelJa, int previous, int current, string panelKey)
        {
            var line = new DigestLine();
            line.LabelJa = labelJa;
            line.Previous = previous;
            line.Current = current;
            line.PanelKey = panelKey;
            return line;
        }

        static int CompareRanked(RankedLine a, RankedLine b)
        {
            if (a.Delta != b.Delta) return b.Delta.CompareTo(a.Delta);   // 増分の大きい順
            return a.Order.CompareTo(b.Order);                            // 同値は台帳の並び順で安定化
        }

        struct RankedLine
        {
            public int Order;
            public int Delta;
            public DigestLine Line;
        }

        /// <summary>1つの計測指標。Silent な指標は記録するだけで行を作らない。</summary>
        sealed class Metric
        {
            public string Key;
            public string LabelJa;
            public int Current;
            public string PanelKey;
            public bool Silent;
            public bool CountsToTotal;
            public bool IsRules;

            public static Metric Plain(string key, string labelJa, int current, string panelKey)
            {
                return new Metric { Key = key, LabelJa = labelJa, Current = current, PanelKey = panelKey };
            }

            /// <summary>台帳の分類ごとの内訳。行は出すが TotalDelta には算入しない(総件数の再掲のため)。</summary>
            public static Metric Category(string key, string labelJa, int current, string panelKey)
            {
                return new Metric { Key = key, LabelJa = labelJa, Current = current, PanelKey = panelKey };
            }

            /// <summary>行も出し、TotalDelta にも算入する指標。</summary>
            public static Metric Counted(string key, string labelJa, int current, string panelKey)
            {
                return new Metric { Key = key, LabelJa = labelJa, Current = current, PanelKey = panelKey, CountsToTotal = true };
            }

            /// <summary>GameRules 由来の指標(要約の「技術・ユニット・建物」欄に集計される)。</summary>
            public static Metric Rules(string key, string labelJa, int current, string panelKey)
            {
                return new Metric { Key = key, LabelJa = labelJa, Current = current, PanelKey = panelKey, CountsToTotal = true, IsRules = true };
            }

            /// <summary>記録だけして行を作らない指標(存在フラグや、内訳の方が有用な集計値)。</summary>
            public static Metric SilentMetric(string key, int current)
            {
                return new Metric { Key = key, LabelJa = "", Current = current, Silent = true };
            }
        }
    }

    /// <summary>
    /// 差分カード1行分。増えた指標だけが行になる。
    /// PanelKey は「その行から直接開けるパネルの型名」で、開けない行では null。
    /// </summary>
    public struct DigestLine
    {
        /// <summary>日本語の表示名(例:「世界史台帳」「書籍」「市場パネル」)。</summary>
        public string LabelJa;
        /// <summary>前回起動時の値。新規に現れた指標では 0。</summary>
        public int Previous;
        /// <summary>今回の値。必ず Previous より大きい。</summary>
        public int Current;
        /// <summary>UpdateDigest.TryOpenPanel に渡せるパネル型名。該当が無ければ null。</summary>
        public string PanelKey;

        /// <summary>増分(Current - Previous)。カウントアップ演出の目標値に使う。</summary>
        public int Delta { get { return Current - Previous; } }
    }

    /// <summary>
    /// 起動1回分の差分スナップショット。UpdateDigest.ComputeOnly() が返す。
    /// 表示が済んだら UpdateDigest.Commit(this) で基準値を確定する。
    /// </summary>
    public class DigestResult
    {
        /// <summary>表示すべき差分があるか。false ならカード自体を出さない。</summary>
        public bool HasChanges;

        /// <summary>差分の行(集計 → 台帳の内訳 → 新パネルの順)。null にはならない。</summary>
        public List<DigestLine> Lines;

        /// <summary>今回新しく現れたパネルの型名。TryOpenPanel にそのまま渡せる。</summary>
        public List<string> NewPanelKeys;

        /// <summary>
        /// この更新で「増えたものの総数」。台帳の増分 + 規則(技術/ユニット/建物)の増分
        /// + 新パネル数 + Core の仕組みの増分。**内訳行は二重計上しない**ため、
        /// Lines の増分の単純合計とは一致しない(見出しの数字にはこちらを使う)。
        /// </summary>
        public int TotalDelta;

        // ---- 以下は描画側の補助情報(追加フィールド) ----

        /// <summary>初回起動(基準値が無かった)か。true のとき HasChanges は必ず false。</summary>
        public bool IsFirstRun;

        /// <summary>Commit() で基準値を書き込み済みか。</summary>
        public bool Committed;

        /// <summary>世界史台帳の総件数の増分。</summary>
        public int LedgerDelta;

        /// <summary>技術・ユニット・建物の増分の合計。</summary>
        public int RulesDelta;

        /// <summary>今回新しく現れた Core の System / Catalog 型名。</summary>
        public List<string> NewCoreTypeKeys;

        /// <summary>
        /// Commit() が書き込む metricKey → 現在値。触らないこと。
        /// 算出が根本的に失敗したときだけ null になり、その場合 Commit() は何も書かず false を返す。
        /// </summary>
        public Dictionary<string, int> Snapshot;

        public DigestResult()
        {
            Lines = new List<DigestLine>();
            NewPanelKeys = new List<string>();
            NewCoreTypeKeys = new List<string>();
            Snapshot = new Dictionary<string, int>(StringComparer.Ordinal);
        }
    }
}
