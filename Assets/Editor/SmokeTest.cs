using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HexCiv;
using HexCiv.Core;
using HexCiv.Core.AI;

/// <summary>
/// ヘッドレスの純シミュレーション・スモークテスト(ARCHITECTURE.md §10)。
/// シーンも GameObject も描画も使わず、GameBootstrap.BuildNewGame と同一の構築処理で
/// 状態を組み立てて 150 ターン回す(75ターン + セーブ/ロード往復検証 + 復元状態で75ターン)。
/// セーブ往復は Serialize(Deserialize(json)) == json の完全一致で検証する。
/// 成功で "SMOKE OK" + Exit(0)、失敗で "SMOKE FAIL" + Exit(1)。
/// </summary>
public static class SmokeTest
{
    public static void Run()
    {
        try
        {
            ValidateCivilizationCatalog();
            ValidateLeaderCatalog();

            var config = new GameConfig
            {
                Seed = 42,
                HumanPlayerIndex = -1,   // 全員AI
                MapWidth = 40,
                MapHeight = 24,
                NumPlayers = 4,
                // 基準値の固定(2026-07-23 追加)。既定値と同じ0だが、将来 GameConfig.GameLength の
                // 既定値が変わっても、この seed42 の基準トレースが黙って動かないよう明示する。
                // MaxTurns も既定の250のまま(150ターン走行中にターン上限判定へ入らないため)。
                GameLength = 0,          // 標準(250ターン)
            };

            var state = GameBootstrap.BuildNewGame(config);
            var turnManager = new TurnManager(state, new AIController());
            turnManager.BeginGame();

            // 全150ターンで一度でも観測した交戦ペアの累積(小Id*1000+大Id で符号化)
            var warPairsEver = new HashSet<int>();
            // 和平が成立したペアの累積と、和平検出用の直前ターン交戦ペア
            var peacePairsEver = new HashSet<int>();
            var activeWarPairs = new HashSet<int>();

            // ---- 前半75ターン ----
            RunTurns(state, turnManager, 75, warPairsEver, peacePairsEver, activeWarPairs);

            // ---- セーブ/ロード往復検証(直列化の決定性と復元の完全性) ----
            // savedAtIso(保存日時メタデータ)は状態から導出できない付帯情報のため、
            // テストでは固定値を与えて決定的にする(実セーブでは SaveLoad.SaveToFile が現在時刻を設定)。
            state.LastSavedAtIso = "2026-07-20T00:00:00";
            string json1 = SaveLoad.Serialize(state);
            var restored = SaveLoad.Deserialize(json1);
            if (restored.LastSavedAtIso != "2026-07-20T00:00:00")
                throw new Exception("savedAtIso が往復で保持されていない");
            string json2 = SaveLoad.Serialize(restored);
            if (json1 != json2)
            {
                Debug.Log($"[Smoke] roundtrip json1={json1.Length}文字 json2={json2.Length}文字");
                Debug.Log("SMOKE FAIL: save roundtrip mismatch");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log($"[Smoke] セーブ/ロード往復一致(turn={restored.TurnNumber}, {json1.Length}文字)");

            // ---- 復元した状態で後半75ターン(新しい TurnManager/AIController で継続) ----
            state = restored;
            turnManager = new TurnManager(state, new AIController());
            RunTurns(state, turnManager, 75, warPairsEver, peacePairsEver, activeWarPairs);

            // 全ターンを通して観測した交戦ペアと和平成立ペアの総数(0でもこのテスト自体は失敗にしない)
            Debug.Log($"SMOKE WARS TOTAL: {warPairsEver.Count}");
            Debug.Log($"SMOKE PEACE TOTAL: {peacePairsEver.Count}");

            // ---- ゲーム設定画面の拡張構成(大きめマップ×6文明)の健全性ミニ検証 ----
            RunMultiCivSmoke();

            // ---- マップ種別「群島」(MapType=2)の健全性ミニ検証(2026-07-20 Claude Code 追加) ----
            RunArchipelagoSmoke();

            // ---- 難易度「むずかしい」(Difficulty=2)の健全性ミニ検証(2026-07-20 Claude Code 追加) ----
            RunDifficultySmoke();

            // ---- ゲーム長「短期(100ターン)」(GameLength=1)の実効性検証(2026-07-23 Claude Code 追加) ----
            // 既存ランの後ろに置く(前に挿入すると静的バインドの順序が変わり得るため)。
            string shortModeFailure = RunShortGameSmoke();
            if (shortModeFailure != null)
            {
                Debug.Log("SMOKE FAIL: short mode " + shortModeFailure);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    /// <summary>文明台帳の一意性・必須データと、文明指定オーバーロードを検証する。</summary>
    static void ValidateCivilizationCatalog()
    {
        if (CivilizationCatalog.All.Count < 56)
            throw new Exception($"文明台帳が不足: {CivilizationCatalog.All.Count}/56");

        var ids = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < CivilizationCatalog.All.Count; i++)
        {
            var civilization = CivilizationCatalog.All[i];
            if (civilization == null || string.IsNullOrEmpty(civilization.Id) ||
                string.IsNullOrEmpty(civilization.NameJa) || civilization.CityNames == null ||
                civilization.CityNames.Length == 0)
                throw new Exception($"文明台帳の必須データ不足(index={i})");
            if (!ids.Add(civilization.Id))
                throw new Exception($"文明ID重複: {civilization.Id}");
        }

        var custom = GameBootstrap.BuildNewGame(new GameConfig
        {
            Seed = 7,
            HumanPlayerIndex = 0,
            MapWidth = 20,
            MapHeight = 12,
            NumPlayers = 1,
            GameLength = 0,          // 標準(既定値と同じ。基準値固定のため明示)
        }, new[] { "maori" });
        if (custom.HumanPlayer == null || custom.HumanPlayer.CivilizationId != "maori" ||
            custom.HumanPlayer.NameJa != "マオリ" || custom.HumanPlayer.NextCityName(custom) != "ワイタンギ")
            throw new Exception("文明指定ゲームの構築に失敗");

        Debug.Log($"[Smoke] 文明台帳OK: {CivilizationCatalog.All.Count}文明");
    }

    /// <summary>指導者台帳の一意性・文明参照・全文明カバーと指定構築を検証する。</summary>
    static void ValidateLeaderCatalog()
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var coveredCivilizations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int unknownNames = 0;

        for (int i = 0; i < LeaderCatalog.All.Count; i++)
        {
            var leader = LeaderCatalog.All[i];
            if (leader == null || string.IsNullOrEmpty(leader.Id) ||
                string.IsNullOrEmpty(leader.CivilizationId) || string.IsNullOrEmpty(leader.NameJa) ||
                string.IsNullOrEmpty(leader.TitleJa) || string.IsNullOrEmpty(leader.PeriodJa))
                throw new Exception($"指導者台帳の必須データ不足(index={i})");
            if (!ids.Add(leader.Id)) throw new Exception($"指導者ID重複: {leader.Id}");
            if (CivilizationCatalog.Find(leader.CivilizationId) == null)
                throw new Exception($"指導者{leader.Id}の文明が存在しない: {leader.CivilizationId}");
            coveredCivilizations.Add(leader.CivilizationId);
            if (!leader.NameKnown) unknownNames++;
        }

        for (int i = 0; i < CivilizationCatalog.All.Count; i++)
        {
            var civilization = CivilizationCatalog.All[i];
            if (!coveredCivilizations.Contains(civilization.Id))
                throw new Exception($"指導者未登録の文明: {civilization.Id}");
        }

        var custom = GameBootstrap.BuildNewGame(new GameConfig
        {
            Seed = 8,
            HumanPlayerIndex = 0,
            MapWidth = 20,
            MapHeight = 12,
            NumPlayers = 1,
            GameLength = 0,          // 標準(既定値と同じ。基準値固定のため明示)
        }, new[] { "maori" }, new[] { "te_rauparaha" });
        if (custom.HumanPlayer == null || custom.HumanPlayer.LeaderId != "te_rauparaha" ||
            custom.HumanPlayer.LeaderNameJa != "テ・ラウパラハ")
            throw new Exception("指導者指定ゲームの構築に失敗");

        string json = SaveLoad.Serialize(custom);
        var restored = SaveLoad.Deserialize(json);
        if (restored.HumanPlayer == null || restored.HumanPlayer.LeaderId != "te_rauparaha")
            throw new Exception("指導者のセーブ/ロード往復に失敗");

        Debug.Log($"[Smoke] 指導者台帳OK: {LeaderCatalog.All.Count}人、全{coveredCivilizations.Count}文明、名未詳{unknownNames}件");
    }

    /// <summary>
    /// ゲーム設定画面で選べる拡張構成(52×30・6文明)の短期健全性検証(2026-07-20 Claude Code 追加)。
    /// 40ターンのヘッドレス進行で例外が出ず、都市が1つ以上建設されることを確認する。
    /// 文明ID未指定なので台帳の既定順(DefaultForSlot)で6文明が重複なく割り当てられる。
    /// </summary>
    static void RunMultiCivSmoke()
    {
        var config = new GameConfig
        {
            Seed = 7,
            HumanPlayerIndex = -1,   // 全員AI
            MapWidth = 52,
            MapHeight = 30,
            NumPlayers = 6,
            GameLength = 0,          // 標準(既定値と同じ。基準値固定のため明示)
        };

        var state = GameBootstrap.BuildNewGame(config);
        if (state.Players.Count != 6)
            throw new Exception($"マルチ文明構成のプレイヤー数が不正: {state.Players.Count}");

        var civIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < state.Players.Count; i++)
            if (!civIds.Add(state.Players[i].CivilizationId))
                throw new Exception($"マルチ文明構成で文明IDが重複: {state.Players[i].CivilizationId}");

        var turnManager = new TurnManager(state, new AIController());
        turnManager.BeginGame();
        for (int i = 0; i < 40 && !state.IsGameOver; i++)
            turnManager.RunHeadlessTurn();

        int cities = state.AllCities.Count();
        if (cities <= 0)
            throw new Exception($"マルチ文明構成でターン{state.TurnNumber}まで都市が0");

        Debug.Log($"SMOKE MULTI-CIV OK (turn={state.TurnNumber} units={state.AllUnits.Count()} cities={cities})");
    }

    /// <summary>
    /// マップ種別「群島」(MapType=2)の短期健全性検証(2026-07-20 Claude Code 追加)。
    /// 高い海面+高周波ノイズの多島マップでも例外なく30ターン進行し、
    /// 都市が1つ以上建設される(初期位置の保証チェーンが機能している)ことを確認する。
    /// </summary>
    static void RunArchipelagoSmoke()
    {
        var config = new GameConfig
        {
            Seed = 11,
            HumanPlayerIndex = -1,   // 全員AI
            MapWidth = 44,
            MapHeight = 26,
            NumPlayers = 4,
            MapType = 2,             // 群島
            GameLength = 0,          // 標準(既定値と同じ。基準値固定のため明示)
        };

        var state = GameBootstrap.BuildNewGame(config);
        var turnManager = new TurnManager(state, new AIController());
        turnManager.BeginGame();
        for (int i = 0; i < 30 && !state.IsGameOver; i++)
            turnManager.RunHeadlessTurn();

        int cities = state.AllCities.Count();
        if (cities <= 0)
            throw new Exception($"群島マップでターン{state.TurnNumber}まで都市が0");

        int landTiles = state.Map.AllTiles.Count(t => t.IsLand);
        Debug.Log($"SMOKE ARCHIPELAGO OK (turn={state.TurnNumber} land={landTiles} cities={cities})");
    }

    /// <summary>
    /// 難易度「むずかしい」(Difficulty=2)の短期健全性検証(2026-07-20 Claude Code 追加)。
    /// AI補正(生産120%・科学120%・戦闘+10%。DifficultyRules 参照)を有効にした
    /// 30ターンのヘッドレス進行で例外が出ないことを確認する。
    /// メインの seed 42 スモークは既定難易度(普通=補正なしの完全 no-op)のため、
    /// 従来結果とのビット一致はそちらで別途保証される。
    /// </summary>
    static void RunDifficultySmoke()
    {
        var config = new GameConfig
        {
            Seed = 11,
            HumanPlayerIndex = -1,   // 全員AI
            MapWidth = 44,
            MapHeight = 26,
            NumPlayers = 4,
            Difficulty = 2,          // むずかしい
            GameLength = 0,          // 標準(既定値と同じ。基準値固定のため明示)
        };

        var state = GameBootstrap.BuildNewGame(config);
        var turnManager = new TurnManager(state, new AIController());
        turnManager.BeginGame();
        for (int i = 0; i < 30 && !state.IsGameOver; i++)
            turnManager.RunHeadlessTurn();

        Debug.Log($"SMOKE DIFFICULTY OK (turn={state.TurnNumber} units={state.AllUnits.Count()} cities={state.AllCities.Count()})");
    }

    /// <summary>
    /// ゲーム長「短期(100ターン)」プリセット(GameLength=1)の実効性検証(2026-07-23 Claude Code 追加)。
    /// 「歴史圧縮」(産出2.5倍・ターン依存定数0.4倍、GameSpeedRules 参照)が本当に効いていることを、
    /// 100ターンを走り切ったあとの拡張・研究・戦争で確認する。単に落ちないことではなく、
    /// 標準250ターンと同じ密度の歴史が100ターンで起きたかを見るのが目的。
    ///
    /// 判定(いずれも「40%のターン数で起きたか」の下限であって、上限は課さない):
    /// ・例外なく最後まで進行する
    /// ・全文明合計の都市が8以上(拡張が実際に進んだ)
    /// ・全文明合計の技術が60以上(研究が追随した)
    /// ・戦争が最低1回発生した(短期でも軍事が動く)
    ///
    /// 標準モードには一切触れないため、seed42 の本編スモークと既存ミニランの出力は不変。
    /// 戻り値は失敗理由(成功なら null)。出力は呼び出し側が "SMOKE FAIL: short mode ..." で行う。
    /// </summary>
    static string RunShortGameSmoke()
    {
        var config = new GameConfig
        {
            Seed = 21,
            HumanPlayerIndex = -1,   // 全員AI
            MapWidth = 44,
            MapHeight = 26,          // 標準マップ(既定サイズ・既定の大陸)
            NumPlayers = 4,
            GameLength = 1,                              // 短期(100ターン)
            MaxTurns = GameSpeedRules.MaxTurnsFor(1),    // = 100(ブートストラップと同じ導出)
        };

        if (config.MaxTurns != GameSpeedRules.ShortMaxTurns)
            return $"GameSpeedRules.MaxTurnsFor(1) が{GameSpeedRules.ShortMaxTurns}ではない: {config.MaxTurns}";

        var state = GameBootstrap.BuildNewGame(config);
        var turnManager = new TurnManager(state, new AIController());
        turnManager.BeginGame();

        // 戦争は和平で AtWarWith から消えるため、本編と同じ累積ペア方式で「一度でも起きたか」を見る。
        var warPairsEver = new HashSet<int>();
        var peacePairsEver = new HashSet<int>();
        var activeWarPairs = new HashSet<int>();

        // 自然終了(勝利決着、またはターン上限100の到達)まで走らせる。
        for (int i = 0; i < config.MaxTurns && !state.IsGameOver; i++)
        {
            turnManager.RunHeadlessTurn();
            RecordWarAndPeacePairs(state, warPairsEver, peacePairsEver, activeWarPairs);
        }

        int units = state.AllUnits.Count();
        int cities = state.AllCities.Count();
        int techs = 0;
        int bestCulture = 0;
        int bestPolicies = 0;
        for (int p = 0; p < state.Players.Count; p++)
        {
            var player = state.Players[p];
            techs += player.KnownTechs.Count;
            if (player.TotalCulture > bestCulture) bestCulture = player.TotalCulture;
            if (player.KnownCulturePolicies.Count > bestPolicies)
                bestPolicies = player.KnownCulturePolicies.Count;
        }
        int wars = warPairsEver.Count;

        // 文化勝利の到達度診断。短期の成立条件は「ターン60以降・政策14件・文化1500・全相手へ影響力」
        // (CultureSystem.VictoryTurnFor / VictoryMinimumPolicies / VictoryMinimumCulture)。
        // このseedで実際に成立しなくても失敗にはせず、人間が射程を判断できるよう数値だけ残す。
        Debug.Log($"SMOKE SHORT CULTURE: best={bestCulture} policies={bestPolicies}");

        // 失敗時の原因切り分け用の素の数値(判定には使わない)。
        var powers = string.Join("/", state.Players.Select(pl => pl.MilitaryPower().ToString()));
        var cityCounts = string.Join("/", state.Players.Select(pl => pl.Cities.Count.ToString()));
        Debug.Log($"SMOKE SHORT DIAG: turn={state.TurnNumber} over={state.IsGameOver} " +
            $"powers={powers} cities={cityCounts} peace={peacePairsEver.Count}");

        if (cities < 8)
            return $"100ターンで都市が不足: cities={cities} (>=8 必要, turn={state.TurnNumber})";
        if (techs < 60)
            return $"100ターンで研究が不足: techs={techs} (>=60 必要, turn={state.TurnNumber})";
        if (wars < 1)
            return $"100ターンで戦争が一度も起きていない: wars={wars} (>=1 必要, turn={state.TurnNumber})";

        Debug.Log($"SMOKE SHORT OK (turn={state.TurnNumber} units={units} cities={cities} techs={techs} wars={wars})");
        return null;
    }

    /// <summary>指定ターン数だけヘッドレス進行し、統計ログと基本健全性チェックを行う。</summary>
    static void RunTurns(GameState state, TurnManager turnManager, int turns,
        HashSet<int> warPairsEver, HashSet<int> peacePairsEver, HashSet<int> activeWarPairs)
    {
        for (int i = 0; i < turns; i++)
        {
            if (state.IsGameOver)
            {
                Debug.Log($"[Smoke] ゲーム終了(ターン{state.TurnNumber}): {state.GameOverMessageJa}");
                break;
            }

            turnManager.RunHeadlessTurn();
            RecordWarAndPeacePairs(state, warPairsEver, peacePairsEver, activeWarPairs);

            int totalCities = state.AllCities.Count();

            // 60ターン経過しても都市が1つも無い = AIが都市を建設できていない(実バグ)
            if (state.TurnNumber >= 60 && totalCities == 0)
                throw new Exception($"ターン{state.TurnNumber}時点で都市が0(AIが都市を建設していない)");

            if (state.TurnNumber % 25 == 0)
            {
                int totalUnits = state.AllUnits.Count();
                int totalTechs = 0;
                int warPairs = 0;
                for (int p = 0; p < state.Players.Count; p++)
                {
                    totalTechs += state.Players[p].KnownTechs.Count;
                    warPairs += state.Players[p].AtWarWith.Count;
                }
                warPairs /= 2;   // 相互登録なので2で割る
                Debug.Log($"[Smoke] turn={state.TurnNumber} units={totalUnits} cities={totalCities} techs={totalTechs} wars={warPairs}");
            }
        }
    }

    /// <summary>
    /// 現在の交戦ペアを累積セットへ記録し(相互登録なので小Id*1000+大Idの片方向キーに正規化)、
    /// 前ターンまで交戦していて今は交戦していないペアを和平成立として数える。
    /// AtWarWith は和平(MakePeaceWith)以外では消えないため、ペアの消滅=和平とみなせる。
    /// </summary>
    static void RecordWarAndPeacePairs(GameState state, HashSet<int> warPairsEver,
        HashSet<int> peacePairsEver, HashSet<int> activeWarPairs)
    {
        var current = new HashSet<int>();
        for (int p = 0; p < state.Players.Count; p++)
        {
            var player = state.Players[p];
            foreach (int enemyId in player.AtWarWith)
            {
                int a = Math.Min(player.Id, enemyId);
                int b = Math.Max(player.Id, enemyId);
                current.Add(a * 1000 + b);
            }
        }
        warPairsEver.UnionWith(current);
        foreach (int pair in activeWarPairs)
            if (!current.Contains(pair)) peacePairsEver.Add(pair);
        activeWarPairs.Clear();
        activeWarPairs.UnionWith(current);
    }
}
