using System;
using HexCiv.UI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 「今回の更新」の差分検出(UI/UpdateDigest.cs)をヘッドレス検証する(2026-07-27 Claude Code 追加)。
///
/// 主眼は**指標が黙って 0 のまま死んでいないこと**。
/// 2026-07-27 に、Core の System / Catalog を数える指標が実測で常に 0 になっていた。
/// 原因は走査条件の `type.IsAbstract` で、C# の `static class` は IL 上 abstract sealed に
/// なるため Core の該当クラス24件すべてが除外されていた。エラーも警告も出ず、カードに
/// 「Coreの仕組み」行が出ないだけなので、**実測しない限り気づけない**種類の欠陥だった。
/// 同じ壊れ方を再発させないため、件数が正の値であることをここで固定する。
/// </summary>
public static class UpdateDigestSmokeTest
{
    public static void Run()
    {
        try
        {
            ValidateScanFindsRealCounts();
            ValidateSummaryIsAlwaysUsable();
            Debug.Log("UPDATE DIGEST SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("UPDATE DIGEST SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// 実行時カウントが実際に対象を拾えていること。ComputeOnly は PlayerPrefs を読むだけで
    /// 書かないので、この検証がユーザーの基準値を動かすことはない。
    /// </summary>
    static void ValidateScanFindsRealCounts()
    {
        var result = UpdateDigest.ComputeOnly();
        if (result == null) throw new Exception("ComputeOnly が null を返した");
        if (result.Snapshot == null) throw new Exception("Snapshot が null(算出が根本的に失敗)");

        // ---- Core(かつて全滅していた箇所) ----
        int systems = Require(result, "core.systems");
        int catalogs = Require(result, "core.catalogs");
        if (systems <= 0)
            throw new Exception("core.systems が 0 — static class を拾えていない疑い");
        if (catalogs <= 0)
            throw new Exception("core.catalogs が 0 — static class を拾えていない疑い");

        // ---- UI パネル ----
        int panels = Require(result, "ui.panels");
        if (panels <= 0) throw new Exception("ui.panels が 0 — パネルを拾えていない");
        var panelKeys = UpdateDigest.AllPanelKeys();
        if (panelKeys.Count != panels)
            throw new Exception("ui.panels(" + panels + ") と AllPanelKeys(" + panelKeys.Count
                + ")が食い違う");

        // ---- 台帳・規則 ----
        if (Require(result, "ledger.total") <= 0) throw new Exception("ledger.total が 0");
        if (Require(result, "ledger.categories") <= 0) throw new Exception("ledger.categories が 0");
        if (Require(result, "rules.techs") <= 0) throw new Exception("rules.techs が 0");
        if (Require(result, "rules.units") <= 0) throw new Exception("rules.units が 0");
        if (Require(result, "rules.buildings") <= 0) throw new Exception("rules.buildings が 0");

        // ---- 走査世代(移行ガード)が記録されること ----
        Require(result, "core.scan.generation");

        Debug.Log("[Digest] core.systems=" + systems + " core.catalogs=" + catalogs
            + " ui.panels=" + panels + " ledger.total=" + Require(result, "ledger.total"));
    }

    /// <summary>要約は入力が何であれ非 null の文字列を返すこと(表示側が空文字で崩れない)。</summary>
    static void ValidateSummaryIsAlwaysUsable()
    {
        if (string.IsNullOrEmpty(UpdateDigest.BuildSummaryJa(null)))
            throw new Exception("null に対する要約が空");

        var empty = new DigestResult();
        empty.HasChanges = false;
        if (string.IsNullOrEmpty(UpdateDigest.BuildSummaryJa(empty)))
            throw new Exception("差分なしに対する要約が空");

        var firstRun = new DigestResult();
        firstRun.IsFirstRun = true;
        if (string.IsNullOrEmpty(UpdateDigest.BuildSummaryJa(firstRun)))
            throw new Exception("初回に対する要約が空");
    }

    static int Require(DigestResult result, string metricKey)
    {
        int value;
        if (!result.Snapshot.TryGetValue(metricKey, out value))
            throw new Exception("指標が存在しない: " + metricKey);
        return value;
    }
}
