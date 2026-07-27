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
            ValidateEnsureOrigin();
            ValidateNewNamesArePreserved();
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
    /// 起点の自動記録。記録が空のときだけ1件置き、既に記録があれば何もしないこと。
    /// これが効かないと、基準値だけあって履歴が空の環境で「成長の記録」がずっと空になる。
    /// </summary>
    static void ValidateEnsureOrigin()
    {
        GrowthHistory.Clear();
        if (!GrowthHistory.EnsureOrigin(Update(1500, 7)))
            throw new Exception("空の状態で起点を記録できない");
        if (GrowthHistory.Count != 1) throw new Exception("起点が1件になっていない");

        var records = GrowthHistory.Load();
        if (!records[0].IsOrigin) throw new Exception("起点として記録されていない");
        // 実際には何も増えていないので、増分は 0 でなければならない
        if (records[0].TotalDelta != 0 || records[0].LedgerDelta != 0)
            throw new Exception("起点に増分が入っている: " + records[0].TotalDelta);
        if (records[0].LedgerTotal != 1500) throw new Exception("起点の総数が不正");

        // 既に記録があるときは何もしない(起点が量産されない)
        if (GrowthHistory.EnsureOrigin(Update(1600, 9)))
            throw new Exception("記録があるのに起点を追加した");
        if (GrowthHistory.Count != 1) throw new Exception("起点が増えた: " + GrowthHistory.Count);

        // null を渡しても例外を出さず false
        GrowthHistory.Clear();
        if (GrowthHistory.EnsureOrigin(null)) throw new Exception("null で起点を記録した");
    }

    /// <summary>
    /// 「何が増えたか」の名前が保存され、読み戻せること。
    /// 件数だけを残すと基準値が進んだ時点でどれが増えたのか永久に分からなくなるため、
    /// ここは往復(保存→読み込み)で確認する。
    /// </summary>
    static void ValidateNewNamesArePreserved()
    {
        GrowthHistory.Clear();

        var result = Update(1200, 3);
        result.NewPanelKeys.Add("TimelapsePanel");
        result.NewPanelKeys.Add("UrukRegionalPanel");
        result.NewCoreTypeKeys.Add("UrukRegionalSystem");
        if (!GrowthHistory.Record(result, "台帳 +3 / 新しいパネル 2"))
            throw new Exception("名前付きの更新を記録できない");

        // キャッシュではなく保存された内容から読み戻す(直列化の往復を確認する)
        GrowthHistory.Reload();
        var records = GrowthHistory.Load();
        if (records.Count != 1) throw new Exception("記録が読み戻せない");

        var record = records[0];
        if (record.NewPanelNames == null || record.NewPanelNames.Count != 2)
            throw new Exception("パネル名が保存されていない");
        if (record.NewPanelNames[0] != "TimelapsePanel" || record.NewPanelNames[1] != "UrukRegionalPanel")
            throw new Exception("パネル名が壊れている: " + string.Join(",", record.NewPanelNames.ToArray()));
        if (record.NewCoreTypeNames == null || record.NewCoreTypeNames.Count != 1)
            throw new Exception("Core型名が保存されていない");
        if (record.NewCoreTypeNames[0] != "UrukRegionalSystem")
            throw new Exception("Core型名が壊れている: " + record.NewCoreTypeNames[0]);

        // 件数フィールドは名前から導出されるので必ず一致する
        if (record.NewPanels != 2 || record.NewCoreTypes != 1)
            throw new Exception("件数と名前の数が食い違う: " + record.NewPanels + "/" + record.NewCoreTypes);
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

        // ---- 記録が空の状態で開く(= 起点が自動で置かれ、空のまま終わらないこと) ----
        GrowthHistory.Clear();
        var emptyView = GrowthHistoryView.Open();
        if (emptyView == null) throw new Exception("記録なしで閲覧画面を開けなかった");
        AssertViewHierarchy(emptyView, "記録なし");
        if (GrowthHistory.Count != 1)
            throw new Exception("空の状態で開いても起点が置かれない: " + GrowthHistory.Count);
        var seeded = GrowthHistory.Load()[0];
        if (!seeded.IsOrigin) throw new Exception("置かれた1件が起点になっていない");
        if (seeded.Magnitude <= 0)
            throw new Exception("起点の世界の大きさが 0 — 実行時カウントが取れていない");
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
