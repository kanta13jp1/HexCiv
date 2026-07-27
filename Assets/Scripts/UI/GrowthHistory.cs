using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexCiv.UI
{
    /// <summary>
    /// 「成長の記録」の永続化(2026-07-27 Claude Code 追加)。
    ///
    /// 目的: `UpdateDigest` の「今回の更新」カードは**前回起動からの差分**しか出さないため、
    /// 一度表示して基準値を確定すると、その増分は二度と戻ってこない。このクラスは確定の
    /// たびに1件を追記し、**増えたものが積み上がっていく履歴**として残す。
    ///
    /// 保存先は PlayerPrefs の1キー(JSON文字列)。件数は <see cref="MaxRecords"/> で上限を
    /// 設け、超えたら古い順に捨てる(無制限に伸びて起動が遅くなるのを防ぐ)。
    ///
    /// 契約:
    /// - **例外を投げない**。保存・読込のどこで失敗しても、呼び出し側(タイトル画面)は
    ///   従来どおり動き続ける。失敗時は「記録が無い」ものとして振る舞う。
    /// - シミュレーションには一切関与しない(Core/ を読まない・書かない)。
    /// - 記録の追加は <see cref="UpdateDigest.Commit"/> の成功時にのみ行う。
    ///   つまり**基準値の前進と1対1**に対応し、同じ更新が二重に記録されることはない。
    /// </summary>
    public static class GrowthHistory
    {
        /// <summary>保存キー。書式を変えるときは末尾の版番号を上げる(旧記録は自然に無視される)。</summary>
        const string PrefsKey = "HexCiv.Growth.records.v1";

        /// <summary>保持する最大件数。超えた分は古い順に捨てる。</summary>
        public const int MaxRecords = 120;

        /// <summary>読み込み済みの記録(起動中に何度も JSON を解析しないためのキャッシュ)。</summary>
        static List<GrowthRecord> cached;

        // ==================================================================
        // 公開API
        // ==================================================================

        /// <summary>
        /// 記録を古い順(=時系列順)で返す。まだ何も無ければ空リスト。**null を返さない**。
        /// 返すのは複製なので、呼び出し側が中身を触っても保存内容は変わらない。
        /// </summary>
        public static List<GrowthRecord> Load()
        {
            EnsureLoaded();
            return new List<GrowthRecord>(cached);
        }

        /// <summary>記録件数。</summary>
        public static int Count
        {
            get { EnsureLoaded(); return cached.Count; }
        }

        /// <summary>
        /// キャッシュを捨てて次回アクセス時に保存内容を読み直す。
        /// 通常の起動経路では不要(記録の追加はこのクラス自身が行うため常に整合している)。
        /// 保存先を外から書き換えた場合の読み直し口として公開している。
        /// </summary>
        public static void Reload()
        {
            cached = null;
        }

        /// <summary>
        /// 差分の確定に成功したときに1件記録する。
        /// 初回起動(基準値の作成)は「起点」として、増分ゼロ・総数のみの1件を残す
        /// (グラフの始点になるので、初回でも記録しておく価値がある)。
        /// 初回でも更新でもない場合(=差分ゼロ)は何も記録しない。
        /// </summary>
        /// <param name="result">Commit に成功した直後の結果。null や Snapshot 無しは無視する。</param>
        /// <param name="summaryJa">1行要約(UpdateDigest.BuildSummaryJa の結果)。null 可。</param>
        /// <returns>記録したら true。</returns>
        public static bool Record(DigestResult result, string summaryJa)
        {
            if (result == null || result.Snapshot == null) return false;
            if (!result.IsFirstRun && !result.HasChanges) return false;   // 変化なしは残さない

            try
            {
                var record = new GrowthRecord();
                record.AtIso = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                record.IsOrigin = result.IsFirstRun;

                record.LedgerTotal = Snapshot(result, "ledger.total");
                record.Categories  = Snapshot(result, "ledger.categories");
                record.RulesTotal  = Snapshot(result, "rules.techs")
                                   + Snapshot(result, "rules.units")
                                   + Snapshot(result, "rules.buildings");
                record.Panels      = Snapshot(result, "ui.panels");
                record.CoreTypes   = Snapshot(result, "core.systems")
                                   + Snapshot(result, "core.catalogs");

                if (!result.IsFirstRun)
                {
                    record.TotalDelta   = result.TotalDelta;
                    record.LedgerDelta  = result.LedgerDelta;
                    record.RulesDelta   = result.RulesDelta;
                    record.NewPanels    = result.NewPanelKeys != null ? result.NewPanelKeys.Count : 0;
                    record.NewCoreTypes = result.NewCoreTypeKeys != null
                        ? result.NewCoreTypeKeys.Count : 0;
                }
                record.SummaryJa = summaryJa ?? "";

                EnsureLoaded();
                cached.Add(record);
                // 上限超過分を古い順に捨てる(先頭が最古)
                if (cached.Count > MaxRecords) cached.RemoveRange(0, cached.Count - MaxRecords);
                return Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("GrowthHistory: 記録に失敗しました: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 「起点から今までにいくつ増えたか」。記録が1件以下なら 0。
        /// 起点の総数と最新の総数の差なので、途中で記録が上限で捨てられていても
        /// **残っている範囲での増加**を正しく表す(嘘の累計を出さない)。
        /// </summary>
        public static int GrowthSinceOldest(List<GrowthRecord> records)
        {
            if (records == null || records.Count < 2) return 0;
            var oldest = records[0];
            var newest = records[records.Count - 1];
            int delta = (newest.LedgerTotal + newest.RulesTotal + newest.Panels + newest.CoreTypes)
                      - (oldest.LedgerTotal + oldest.RulesTotal + oldest.Panels + oldest.CoreTypes);
            return Mathf.Max(delta, 0);
        }

        /// <summary>記録をすべて消す(デバッグ・やり直し用)。失敗しても例外は投げない。</summary>
        public static bool Clear()
        {
            try
            {
                PlayerPrefs.DeleteKey(PrefsKey);
                PlayerPrefs.Save();
                cached = new List<GrowthRecord>();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("GrowthHistory: 記録の消去に失敗しました: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// ISO文字列を「7/27 17:30」形式の短い日本語表記にする。
        /// 解析できない場合は元の文字列をそのまま返す(表示が空にならないようにする)。
        /// </summary>
        public static string FormatAtJa(string atIso)
        {
            if (string.IsNullOrEmpty(atIso)) return "";
            DateTime parsed;
            if (DateTime.TryParse(atIso, out parsed))
                return parsed.ToString("M/d HH:mm");
            return atIso;
        }

        // ==================================================================
        // 内部
        // ==================================================================

        static int Snapshot(DigestResult result, string metricKey)
        {
            int value;
            if (result.Snapshot != null && result.Snapshot.TryGetValue(metricKey, out value))
                return value;
            return 0;
        }

        static void EnsureLoaded()
        {
            if (cached != null) return;
            cached = new List<GrowthRecord>();
            try
            {
                string json = PlayerPrefs.GetString(PrefsKey, "");
                if (string.IsNullOrEmpty(json)) return;
                var store = JsonUtility.FromJson<GrowthStore>(json);
                if (store != null && store.Records != null)
                {
                    for (int i = 0; i < store.Records.Count; i++)
                    {
                        if (store.Records[i] != null) cached.Add(store.Records[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                // 壊れた JSON を掴んでも起動は止めない。以後は「記録なし」として続行する。
                Debug.LogWarning("GrowthHistory: 記録の読み込みに失敗しました: " + ex.Message);
                cached = new List<GrowthRecord>();
            }
        }

        static bool Save()
        {
            try
            {
                var store = new GrowthStore();
                store.Records = cached;
                PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(store));
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("GrowthHistory: 記録の保存に失敗しました: " + ex.Message);
                return false;
            }
        }

        /// <summary>JsonUtility はトップレベルが配列だと扱えないので、包むためだけの入れ物。</summary>
        [Serializable]
        class GrowthStore
        {
            public List<GrowthRecord> Records;
        }
    }

    /// <summary>
    /// 「成長の記録」1件(2026-07-27 Claude Code 追加)。JsonUtility で保存するため
    /// **public フィールドのみ**で構成する(プロパティは直列化されない)。
    /// </summary>
    [Serializable]
    public class GrowthRecord
    {
        /// <summary>記録した日時(ローカル時刻の "yyyy-MM-ddTHH:mm:ss")。</summary>
        public string AtIso;

        /// <summary>この記録が「起点」(初回起動での基準値作成)か。増分は全て0になる。</summary>
        public bool IsOrigin;

        // ---- そのときの総数(グラフの縦軸に使う) ----
        public int LedgerTotal;
        public int Categories;
        public int RulesTotal;
        public int Panels;
        public int CoreTypes;

        // ---- そのときの増分 ----
        public int TotalDelta;
        public int LedgerDelta;
        public int RulesDelta;
        public int NewPanels;
        public int NewCoreTypes;

        /// <summary>1行要約(例:「台帳 +24 / 新しいパネル 1」)。</summary>
        public string SummaryJa;

        /// <summary>グラフの縦軸に使う「世界の大きさ」。台帳+規則+パネル+仕組みの総和。</summary>
        public int Magnitude
        {
            get { return LedgerTotal + RulesTotal + Panels + CoreTypes; }
        }
    }
}
