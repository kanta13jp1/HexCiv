using System;
using System.Collections.Generic;
using HexCiv.UI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 「成長の記録」(UI/GrowthHistory.cs)をヘッドレス検証する(2026-07-27 Claude Code 追加)。
///
/// 検証するのは**永続化層の契約**だけで、描画は対象外(UI は実起動テストで確認する)。
/// - 起点/更新の記録と時系列順の保持
/// - 差分ゼロを記録しないこと(履歴が無意味に膨らまない)
/// - 上限件数を超えたら古い順に捨てること
/// - 壊れた保存データを掴んでも例外を投げず「記録なし」として続行すること
///
/// 注意: エディタの PlayerPrefs はビルド済みプレイヤーとは別の保存先(Unity Editor 配下)
/// なので、この検証がユーザーの実際の記録を壊すことはない。それでも念のため、
/// 実行前の内容を退避し、最後に必ず元へ戻す。
/// </summary>
public static class GrowthHistorySmokeTest
{
    const string PrefsKey = "HexCiv.Growth.records.v1";

    public static void Run()
    {
        string saved = PlayerPrefs.GetString(PrefsKey, "");
        try
        {
            ValidateOriginAndUpdates();
            ValidateNoChangeIsNotRecorded();
            ValidateBoundedToMaxRecords();
            ValidateCorruptedDataIsSurvivable();
            ValidateViewBuilds();
            Debug.Log("GROWTH HISTORY SMOKE OK");
            Restore(saved);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("GROWTH HISTORY SMOKE FAIL: " + ex);
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
            Debug.Log("GROWTH HISTORY SMOKE: 退避データの復元に失敗: " + ex.Message);
        }
    }

    // ==================================================================

    static void ValidateOriginAndUpdates()
    {
        GrowthHistory.Clear();
        if (GrowthHistory.Count != 0) throw new Exception("Clear 後に記録が残っている");

        if (!GrowthHistory.Record(FirstRun(1000), "初回")) throw new Exception("起点を記録できない");
        if (!GrowthHistory.Record(Update(1024, 24), "台帳 +24")) throw new Exception("更新を記録できない");
        if (!GrowthHistory.Record(Update(1030, 6), "台帳 +6")) throw new Exception("更新2を記録できない");

        var records = GrowthHistory.Load();
        if (records.Count != 3) throw new Exception("記録件数が不正: " + records.Count);

        // 時系列順(古い→新しい)であること
        if (!records[0].IsOrigin) throw new Exception("先頭が起点でない");
        if (records[1].IsOrigin || records[2].IsOrigin) throw new Exception("更新が起点扱いになっている");
        if (records[0].LedgerTotal != 1000 || records[2].LedgerTotal != 1030)
            throw new Exception("総数が時系列順に並んでいない");

        // 起点の増分は必ず 0(初回に巨大な偽の増分を出さないための不変条件)
        if (records[0].TotalDelta != 0 || records[0].LedgerDelta != 0)
            throw new Exception("起点に増分が入っている: " + records[0].TotalDelta);

        if (records[1].TotalDelta != 24) throw new Exception("増分が保存されていない");
        if (records[1].SummaryJa != "台帳 +24") throw new Exception("要約が保存されていない");

        // 世界の大きさ = 台帳 + 規則 + パネル + 仕組み
        int expected = 1030 + (140 + 9 + 8) + 15 + 12;
        if (records[2].Magnitude != expected)
            throw new Exception("Magnitude が不正: " + records[2].Magnitude + " != " + expected);

        // 起点から最新までの増加(残っている範囲での増加を正しく表すこと)
        int growth = GrowthHistory.GrowthSinceOldest(records);
        if (growth != 30) throw new Exception("GrowthSinceOldest が不正: " + growth);

        // 記録が1件以下なら 0(嘘の累計を出さない)
        if (GrowthHistory.GrowthSinceOldest(new List<GrowthRecord>()) != 0)
            throw new Exception("空リストで 0 以外を返した");
    }

    static void ValidateNoChangeIsNotRecorded()
    {
        GrowthHistory.Clear();
        GrowthHistory.Record(FirstRun(1000), "初回");

        var unchanged = Update(1000, 0);
        unchanged.HasChanges = false;   // 差分ゼロ
        if (GrowthHistory.Record(unchanged, "更新なし"))
            throw new Exception("差分ゼロを記録してしまった");
        if (GrowthHistory.Count != 1)
            throw new Exception("差分ゼロで件数が増えた: " + GrowthHistory.Count);

        // null / Snapshot 無しも黙って無視する(例外を投げない)
        if (GrowthHistory.Record(null, "null")) throw new Exception("null を記録してしまった");
        var noSnapshot = Update(1010, 10);
        noSnapshot.Snapshot = null;
        if (GrowthHistory.Record(noSnapshot, "snapshot なし"))
            throw new Exception("Snapshot 無しを記録してしまった");
    }

    static void ValidateBoundedToMaxRecords()
    {
        GrowthHistory.Clear();
        int over = GrowthHistory.MaxRecords + 15;
        for (int i = 0; i < over; i++) GrowthHistory.Record(Update(1000 + i, 1), "更新" + i);

        var records = GrowthHistory.Load();
        if (records.Count != GrowthHistory.MaxRecords)
            throw new Exception("上限を超えて保持している: " + records.Count);

        // 捨てられるのは**古い順**。最新は必ず残り、最古は捨てられた分だけ進んでいる。
        if (records[records.Count - 1].LedgerTotal != 1000 + over - 1)
            throw new Exception("最新の記録が失われている");
        if (records[0].LedgerTotal != 1000 + (over - GrowthHistory.MaxRecords))
            throw new Exception("古い順に捨てていない: " + records[0].LedgerTotal);
    }

    static void ValidateCorruptedDataIsSurvivable()
    {
        PlayerPrefs.SetString(PrefsKey, "{ これは JSON ではない ][");
        PlayerPrefs.Save();
        // キャッシュを捨てて、壊れた保存データを実際に読ませる
        // (これをしないとプロセス内キャッシュが素通りして何も検証できない)
        GrowthHistory.Reload();

        // 壊れたデータを読んでも例外を投げず、「記録なし」として続行できること
        var records = GrowthHistory.Load();
        if (records == null) throw new Exception("Load が null を返した");
        if (records.Count != 0)
            throw new Exception("壊れたデータから記録が復元された: " + records.Count);

        // 壊れた後も新しい記録を積み直せること(行き止まりにならない)
        if (!GrowthHistory.Record(Update(2000, 5), "壊れた後の更新"))
            throw new Exception("壊れたデータの後に記録できない");
        GrowthHistory.Reload();
        if (GrowthHistory.Count != 1)
            throw new Exception("壊れた後の記録が保存されていない: " + GrowthHistory.Count);
    }

    /// <summary>
    /// 閲覧画面(GrowthHistoryView)が例外なく組み上がることを確認する。
    ///
    /// 描画の見た目までは検証できないが、レイアウト計算・グラフ生成・行生成は実際に走るので、
    /// クリックして初めて出るような NullReference の類はここで落ちる。
    /// 記録あり/なしの両方を通す(空のときの分岐は実運用で最初に踏まれる経路)。
    /// </summary>
    static void ValidateViewBuilds()
    {
        // ---- 記録あり ----
        GrowthHistory.Clear();
        GrowthHistory.Record(FirstRun(1000), "初回");
        for (int i = 0; i < 12; i++) GrowthHistory.Record(Update(1000 + i * 3, 3), "台帳 +3");

        var view = GrowthHistoryView.Open();
        if (view == null) throw new Exception("記録ありで閲覧画面を開けなかった");
        AssertViewHierarchy(view, "記録あり");
        DisposeView(view);

        // ---- 記録なし(空の状態) ----
        GrowthHistory.Clear();
        var emptyView = GrowthHistoryView.Open();
        if (emptyView == null) throw new Exception("記録なしで閲覧画面を開けなかった");
        AssertViewHierarchy(emptyView, "記録なし");
        DisposeView(emptyView);

        if (GrowthHistoryView.IsOpen)
            throw new Exception("破棄後も IsOpen が true のまま");
    }

    static void AssertViewHierarchy(GrowthHistoryView view, string caseJa)
    {
        var canvas = view.GetComponentInChildren<Canvas>(true);
        if (canvas == null) throw new Exception(caseJa + ": Canvas が生成されていない");
        if (canvas.sortingOrder <= 200)
            throw new Exception(caseJa + ": タイトル(200)より手前になっていない: " + canvas.sortingOrder);

        var texts = view.GetComponentsInChildren<UnityEngine.UI.Text>(true);
        bool hasHeader = false;
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].text == "成長の記録") { hasHeader = true; break; }
        }
        if (!hasHeader) throw new Exception(caseJa + ": 見出しが生成されていない");
    }

    /// <summary>
    /// エディタ(編集モード)では Object.Destroy が使えないため、Close() ではなく
    /// DestroyImmediate で片付ける。破棄済みの UnityEngine.Object は null 比較が true に
    /// なるので、GrowthHistoryView.IsOpen も正しく false へ戻る。
    /// </summary>
    static void DisposeView(GrowthHistoryView view)
    {
        if (view != null) UnityEngine.Object.DestroyImmediate(view.gameObject);
    }

    // ==================================================================
    // テスト用の DigestResult 生成
    // ==================================================================

    static DigestResult FirstRun(int ledgerTotal)
    {
        var result = Base(ledgerTotal);
        result.IsFirstRun = true;
        result.HasChanges = false;
        result.TotalDelta = 0;
        result.LedgerDelta = 0;
        return result;
    }

    static DigestResult Update(int ledgerTotal, int delta)
    {
        var result = Base(ledgerTotal);
        result.IsFirstRun = false;
        result.HasChanges = true;
        result.TotalDelta = delta;
        result.LedgerDelta = delta;
        return result;
    }

    static DigestResult Base(int ledgerTotal)
    {
        var result = new DigestResult();
        result.Snapshot["ledger.total"] = ledgerTotal;
        result.Snapshot["ledger.categories"] = 32;
        result.Snapshot["rules.techs"] = 140;
        result.Snapshot["rules.units"] = 9;
        result.Snapshot["rules.buildings"] = 8;
        result.Snapshot["ui.panels"] = 15;
        result.Snapshot["core.systems"] = 9;
        result.Snapshot["core.catalogs"] = 3;
        return result;
    }
}
