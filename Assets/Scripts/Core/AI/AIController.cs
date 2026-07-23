using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexCiv.Core.AI
{
    /// <summary>
    /// AI文明の1ターン分の思考。順序:研究選択 → 都市生産 → 宣戦判断 → ユニット行動。
    /// 乱数は決定論のため state.Rng のみを使用する。
    /// どんなエッジケース(都市なし・袋小路・滅亡寸前など)でも例外を出さず、必ずターンを完了させる。
    /// </summary>
    public class AIController : IAIController
    {
        // ---- 調整用定数 ----
        const int EarlyMonumentTurn = 40;     // このターンまでは記念碑を優先
        const int WarMinTurn = 25;            // これ以前は宣戦しない
        const int WarProximityDistance = 8;   // 近接とみなす自都市〜既知の敵都市/敵兵の距離
        const float WarProximityPowerRatio = 1.15f;  // 近接宣戦に必要な軍事力比
        const int WarProximityChance = 12;    // 近接+優勢時の宣戦確率(%/ターン)
        const int WarFrictionDistance = 5;    // 首都圏の国境摩擦距離
        const float WarFrictionPowerRatio = 0.95f;   // 摩擦宣戦に必要な軍事力比
        const int WarFrictionChance = 6;      // 国境摩擦の追加宣戦確率(%/ターン)
        const int WarOpportunismChance = 8;   // 既知の最弱文明への便乗宣戦確率(%/ターン)
        const int WarWallsChance = 20;        // 戦時に軍事より城壁を選ぶ確率(%)
        const int PeaceStalemateMinTurns = 25;      // 膠着和平に必要な戦争継続ターン
        const float PeaceStalemateRatioLow = 0.7f;  // 膠着とみなす軍事力比の下限
        const float PeaceStalemateRatioHigh = 1.4f; // 膠着とみなす軍事力比の上限
        const int PeaceStalemateChance = 15;        // 膠着時の和平確率(%/ターン)
        const int PeaceLosingMinTurns = 15;         // 劣勢和平に必要な戦争継続ターン
        const float PeaceLosingPowerRatio = 0.4f;   // 劣勢とみなす軍事力比(自軍<相手×0.4)
        const int PeaceLosingChance = 25;           // 大敗中の和平嘆願確率(%/ターン)
        const int MaxSiegePathTries = 6;      // 前線位置取りの経路探索回数上限
        const int SettlerSearchRadius = 8;    // 開拓者の入植地探索半径
        const int SettlerScoreMargin = 6;     // 現在地で妥協するスコア差
        const int MaxSettlerPathTries = 5;    // 開拓者の経路探索回数上限
        const int MaxScoutPathTries = 20;     // 斥候の経路探索回数上限
        const int MaxScoutRingRadius = 14;    // 斥候の未踏地探索半径上限
        const int MaxUnitsPerTurn = 500;      // 1ターンに処理するユニット数上限(安全弁)

        public void PlayTurn(GameState state, Player player)
        {
            if (state == null || player == null) return;
            if (state.IsGameOver || player.IsEliminated) return;

            // 各フェーズは失敗してもターン全体を止めない
            try
            {
                AdministrationSystem.SetTaxPolicy(state, player,
                    AdministrationSystem.RecommendTaxPolicy(player), writeLog: false);
            }
            catch (Exception) { }
            try { ChooseResearch(state, player); } catch (Exception) { }
            bool madePeace = false;
            try { madePeace = ConsiderPeace(state, player); } catch (Exception) { }
            try { ChooseAllProduction(state, player); } catch (Exception) { }
            // 和平した直後のターンは新たな宣戦を検討しない(即時再宣戦の防止)
            if (!madePeace)
            {
                try { ConsiderWarDeclarations(state, player); } catch (Exception) { }
            }
            PlayUnits(state, player);
        }

        // ================= 研究 =================

        /// <summary>研究が未選択なら選ぶ。最安を基本に、少しだけ乱数で揺らす。</summary>
        void ChooseResearch(GameState s, Player p)
        {
            if (!string.IsNullOrEmpty(p.CurrentResearchId)) return;
            var avail = p.AvailableTechs();
            if (avail == null || avail.Count == 0) return;

            avail.Sort((a, b) =>
            {
                int c = a.Cost.CompareTo(b.Cost);
                return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
            });

            int idx = (avail.Count > 1 && s.Rng.Next(100) < 25) ? 1 : 0;
            p.SetResearch(avail[idx].Id);
        }

        // ================= 都市生産 =================

        void ChooseAllProduction(GameState s, Player p)
        {
            bool atWar = AnyActiveWar(s, p);
            var cities = new List<City>(p.Cities);
            for (int i = 0; i < cities.Count; i++)
            {
                var city = cities[i];
                if (city == null || city.PlayerId != p.Id) continue;
                if (city.CurrentProduction != null) continue;
                try { ChooseProductionFor(s, p, city, atWar); } catch (Exception) { }
            }
        }

        /// <summary>
        /// 生産ヒューリスティック:戦時は軍事最優先(たまに城壁) → 序盤の記念碑
        /// → 開拓者(平時・都市4未満・開拓者なし) → 軍事(兵力不足) → 建物/ユニットのミックス。
        /// </summary>
        void ChooseProductionFor(GameState s, Player p, City city, bool atWar)
        {
            var avail = city.AvailableProduction(s);
            if (avail == null || avail.Count == 0) return;

            ProductionItem pick = null;

            // 0) 戦時は軍事を最優先(記念碑などより先)。2割で城壁に切り替えて守りを固める
            if (atWar)
            {
                pick = BestMilitaryItem(s, p, city, avail);
                if (pick != null && s.Rng.Next(100) < WarWallsChance)
                {
                    var walls = FindItem(avail, ProductionKind.Building, "walls");
                    if (walls != null) pick = walls;
                }
            }

            // 1) 序盤は記念碑(未建設なら)
            if (pick == null && s.TurnNumber <= EarlyMonumentTurn && !city.Buildings.Contains("monument"))
                pick = FindItem(avail, ProductionKind.Building, "monument");

            // 2) 開拓者:都市4未満・生存開拓者なし・平時
            if (pick == null && !atWar && p.Cities.Count < 4 && !HasLiveSettler(p))
                pick = FindItem(avail, ProductionKind.Unit, "settler");

            // 3) 軍事:戦時または兵力不足
            if (pick == null && (atWar || CountMilitary(p) < p.Cities.Count + 1))
                pick = BestMilitaryItem(s, p, city, avail);

            // 4) ミックス:建物優先、たまにユニット
            if (pick == null)
            {
                bool wantUnit = s.Rng.Next(100) < 30;
                if (!wantUnit)
                {
                    string[] priority =
                    {
                        "library", "granary", "harbor", "convoy_office",
                        "bridgeworks", "workshop", "walls", "monument"
                    };
                    for (int i = 0; i < priority.Length && pick == null; i++)
                        pick = FindItem(avail, ProductionKind.Building, priority[i]);
                }
                if (pick == null) pick = BestMilitaryItem(s, p, city, avail);
                if (pick == null) pick = avail[0];
            }

            if (pick != null) city.SetProduction(pick);
        }

        static ProductionItem FindItem(List<ProductionItem> avail, ProductionKind kind, string id)
        {
            for (int i = 0; i < avail.Count; i++)
                if (avail[i].Kind == kind && avail[i].Id == id) return avail[i];
            return null;
        }

        /// <summary>
        /// 軍構成バランス(2026-07-21 追加):生存する遠隔ユニット数が近接ユニット数以上なら、
        /// 近接ユニットを優先して選ぶ(カタパルト偏重で白兵が壊滅する構成の抑制)。
        /// 遠隔ユニットを新たに作るのは近接が遠隔より多い時だけ。
        /// 近接が1種も生産できない場合のみ従来どおり全軍事から選ぶ(生産停止の防止)。
        /// </summary>
        ProductionItem BestMilitaryItem(GameState s, Player p, City city, List<ProductionItem> avail)
        {
            if (city != null && LogisticsSystem.HasHarbor(city) &&
                CountNavalMilitary(p) < CountHarbors(p) && s.Rng.Next(100) < 40)
            {
                var navalPick = BestMilitaryItemCore(s, avail, false, 1);
                if (navalPick != null) return navalPick;
            }
            if (CountRangedMilitary(p) >= CountMeleeMilitary(p))
            {
                var meleePick = BestMilitaryItemCore(s, avail, true, 0);
                if (meleePick != null) return meleePick;
            }
            return BestMilitaryItemCore(s, avail, false, 0);
        }

        /// <summary>生産可能な最強の軍事ユニット。domain: 0=陸、1=海、その他=不問。</summary>
        ProductionItem BestMilitaryItemCore(GameState s, List<ProductionItem> avail,
            bool meleeOnly, int domain)
        {
            ProductionItem best = null, second = null;
            for (int i = 0; i < avail.Count; i++)
            {
                var item = avail[i];
                if (item.Kind != ProductionKind.Unit) continue;
                var def = GameRules.GetUnit(item.Id);
                if (def == null || def.IsCivilian) continue;
                if (meleeOnly && def.IsRanged) continue;
                if (domain == 0 && def.IsNaval) continue;
                if (domain == 1 && !def.IsNaval) continue;

                if (best == null || CompareMilitary(item, best) < 0)
                {
                    second = best;
                    best = item;
                }
                else if (second == null || CompareMilitary(item, second) < 0)
                {
                    second = item;
                }
            }
            if (best == null) return null;
            if (second != null && s.Rng.Next(100) < 20) return second;
            return best;
        }

        /// <summary>負なら a が優れる(強い→安い→Id順)。</summary>
        static int CompareMilitary(ProductionItem a, ProductionItem b)
        {
            var da = GameRules.GetUnit(a.Id);
            var db = GameRules.GetUnit(b.Id);
            int pa = da.Strength + da.RangedStrength;
            int pb = db.Strength + db.RangedStrength;
            if (pa != pb) return pb - pa;
            if (a.Cost != b.Cost) return a.Cost - b.Cost;
            return string.CompareOrdinal(a.Id, b.Id);
        }

        static bool HasLiveSettler(Player p)
        {
            for (int i = 0; i < p.Units.Count; i++)
            {
                var u = p.Units[i];
                if (u != null && !u.IsDead && u.DefId == "settler") return true;
            }
            return false;
        }

        static int CountMilitary(Player p)
        {
            int n = 0;
            for (int i = 0; i < p.Units.Count; i++)
            {
                var u = p.Units[i];
                if (u != null && !u.IsDead && !u.Def.IsCivilian) n++;
            }
            return n;
        }

        static int CountNavalMilitary(Player p)
        {
            int n = 0;
            for (int i = 0; i < p.Units.Count; i++)
            {
                var u = p.Units[i];
                if (u != null && !u.IsDead && !u.Def.IsCivilian && u.Def.IsNaval) n++;
            }
            return n;
        }

        static int CountHarbors(Player p)
        {
            int n = 0;
            for (int i = 0; i < p.Cities.Count; i++)
                if (LogisticsSystem.HasHarbor(p.Cities[i])) n++;
            return n;
        }

        /// <summary>生存している遠隔軍事ユニット数(非民間人かつ Def.IsRanged)。</summary>
        static int CountRangedMilitary(Player p)
        {
            int n = 0;
            for (int i = 0; i < p.Units.Count; i++)
            {
                var u = p.Units[i];
                if (u != null && !u.IsDead && !u.Def.IsCivilian && u.Def.IsRanged) n++;
            }
            return n;
        }

        /// <summary>生存している近接軍事ユニット数(非民間人かつ非遠隔)。</summary>
        static int CountMeleeMilitary(Player p)
        {
            int n = 0;
            for (int i = 0; i < p.Units.Count; i++)
            {
                var u = p.Units[i];
                if (u != null && !u.IsDead && !u.Def.IsCivilian && !u.Def.IsRanged) n++;
            }
            return n;
        }

        // ================= 和平判断 =================

        /// <summary>
        /// 和平判断(毎ターン、乱数は state.Rng。2026-07-20 追加)。和平が成立したら true。
        /// ・劣勢: 自軍事力が相手の0.4倍未満 かつ 開戦から15ターン以上 → 25%/ターンで和平を嘆願。
        ///   一方向の条件なので二重判定は起きない。相手が人間でも自動成立する(嘆願ログで明示)。
        /// ・膠着: 開戦から25ターン以上 かつ 軍事力比が0.7〜1.4倍 → 15%/ターンで和平。
        ///   両者AIの二重判定を避けるため、自Id < 相手Id の側だけが判定する。
        /// 判定順は Players のリスト順で決定的。除算を避け乗算で比較する(軍事力0でも安全)。
        /// </summary>
        bool ConsiderPeace(GameState s, Player p)
        {
            bool made = false;
            int myPower = p.MilitaryPower();

            for (int i = 0; i < s.Players.Count; i++)
            {
                var rival = s.Players[i];
                if (rival == p || rival.IsEliminated || !p.IsAtWarWith(rival.Id)) continue;

                int start;
                if (!p.WarStartTurns.TryGetValue(rival.Id, out start)) start = s.TurnNumber;
                int duration = s.TurnNumber - start;
                int theirPower = rival.MilitaryPower();

                // 劣勢:大敗が続くなら和平を嘆願する(自動成立。人間相手にも適用)
                if (duration >= PeaceLosingMinTurns && myPower < PeaceLosingPowerRatio * theirPower)
                {
                    if (s.Rng.Next(100) < PeaceLosingChance)
                    {
                        s.EmitLog($"「{p.NameJa}」が「{rival.NameJa}」に和平を嘆願した");
                        p.MakePeaceWith(s, rival);
                        made = true;
                    }
                    continue;   // 劣勢(比<0.4)は膠着範囲(0.7〜1.4)と重ならない
                }

                // 膠着:長期戦かつ戦力拮抗なら和平する(小さいId側だけが判定)
                if (p.Id < rival.Id && duration >= PeaceStalemateMinTurns &&
                    myPower >= PeaceStalemateRatioLow * theirPower &&
                    myPower <= PeaceStalemateRatioHigh * theirPower)
                {
                    if (s.Rng.Next(100) < PeaceStalemateChance)
                    {
                        p.MakePeaceWith(s, rival);
                        made = true;
                    }
                }
            }
            return made;
        }

        // ================= 宣戦判断 =================

        /// <summary>
        /// ターン25以降、交戦中でない場合のみ宣戦を検討する(二正面戦争・多重宣戦はしない)。
        /// 確率は要因の合算(いずれも%/ターン、判定は state.Rng):
        /// ・近接: 自都市が既知の敵都市/敵兵から8タイル以内 かつ 軍事力1.15倍超 → +12
        /// ・国境摩擦: 既知の敵都市が自首都から5タイル以内 かつ 軍事力0.95倍超 → +6
        /// ・便乗: 相手が既知の最弱文明 かつ 都市数が自分未満 かつ 自軍事ユニット数 > 自都市数+2 → +8
        /// </summary>
        void ConsiderWarDeclarations(GameState s, Player p)
        {
            if (s.TurnNumber < WarMinTurn) return;
            if (p.Cities.Count == 0) return;
            if (AnyActiveWar(s, p)) return;   // 交戦中は新たな宣戦をしない(多方面戦争による自滅の回避)

            int myPower = p.MilitaryPower();
            int myMilitary = CountMilitary(p);
            var capital = GetCapital(p);
            var weakest = WeakestKnownRival(s, p);

            for (int i = 0; i < s.Players.Count; i++)
            {
                var rival = s.Players[i];
                if (rival == p || rival.IsEliminated || p.IsAtWarWith(rival.Id)) continue;

                int theirPower = rival.MilitaryPower();
                int chance = 0;

                // 近接:自都市が既知の敵都市/敵兵の8タイル以内 かつ 軍事力が1.15倍超
                if (myPower > WarProximityPowerRatio * theirPower && HasRivalProximity(p, rival))
                    chance += WarProximityChance;

                // 国境摩擦:既知の敵都市が自首都の5タイル以内 かつ 軍事力が0.95倍超
                if (capital != null && myPower > WarFrictionPowerRatio * theirPower &&
                    HasCapitalFriction(p, rival, capital))
                    chance += WarFrictionChance;

                // 便乗:相手が既知の最弱文明で、都市数が自分より少なく、自軍に余剰兵力がある
                if (rival == weakest && rival.Cities.Count < p.Cities.Count &&
                    myMilitary > p.Cities.Count + 2)
                    chance += WarOpportunismChance;

                if (chance > 0 && s.Rng.Next(100) < chance)
                {
                    p.DeclareWarOn(s, rival);
                    return;   // 宣戦は1ターン1件まで(以降は交戦中となり冒頭のガードで弾かれる)
                }
            }
        }

        /// <summary>自都市のいずれかが、探索済みのライバル都市または生存ユニットから8タイル以内にあるか。</summary>
        static bool HasRivalProximity(Player p, Player rival)
        {
            for (int i = 0; i < p.Cities.Count; i++)
            {
                var mine = p.Cities[i].Coord;
                for (int j = 0; j < rival.Cities.Count; j++)
                {
                    var c = rival.Cities[j].Coord;
                    if (p.Explored.Contains(c) && mine.DistanceTo(c) <= WarProximityDistance) return true;
                }
                for (int j = 0; j < rival.Units.Count; j++)
                {
                    var ru = rival.Units[j];
                    if (ru == null || ru.IsDead) continue;
                    if (p.Explored.Contains(ru.Coord) && mine.DistanceTo(ru.Coord) <= WarProximityDistance) return true;
                }
            }
            return false;
        }

        /// <summary>探索済みのライバル都市が自首都から5タイル以内にあるか(国境摩擦)。</summary>
        static bool HasCapitalFriction(Player p, Player rival, City capital)
        {
            for (int j = 0; j < rival.Cities.Count; j++)
            {
                var c = rival.Cities[j].Coord;
                if (p.Explored.Contains(c) && capital.Coord.DistanceTo(c) <= WarFrictionDistance) return true;
            }
            return false;
        }

        /// <summary>既知(都市か兵のタイルを探索済み)のライバルのうち軍事力最小の文明。同値は Players の並び順で先勝ち(決定的)。</summary>
        static Player WeakestKnownRival(GameState s, Player p)
        {
            Player weakest = null;
            int weakestPower = int.MaxValue;
            for (int i = 0; i < s.Players.Count; i++)
            {
                var rival = s.Players[i];
                if (rival == p || rival.IsEliminated) continue;
                if (!IsKnownRival(p, rival)) continue;
                int power = rival.MilitaryPower();
                if (power < weakestPower)
                {
                    weakestPower = power;
                    weakest = rival;
                }
            }
            return weakest;
        }

        /// <summary>ライバルの都市または生存ユニットのタイルを1つでも探索済みか。</summary>
        static bool IsKnownRival(Player p, Player rival)
        {
            for (int j = 0; j < rival.Cities.Count; j++)
                if (p.Explored.Contains(rival.Cities[j].Coord)) return true;
            for (int j = 0; j < rival.Units.Count; j++)
            {
                var ru = rival.Units[j];
                if (ru != null && !ru.IsDead && p.Explored.Contains(ru.Coord)) return true;
            }
            return false;
        }

        // ================= ユニット行動 =================

        void PlayUnits(GameState s, Player p)
        {
            bool atWar = AnyActiveWar(s, p);
            var units = new List<Unit>(p.Units);   // 行動中の増減に備えたスナップショット
            int processed = 0;

            for (int i = 0; i < units.Count; i++)
            {
                if (s.IsGameOver) return;
                if (processed++ >= MaxUnitsPerTurn) return;

                var u = units[i];
                if (u == null || u.IsDead || u.PlayerId != p.Id) continue;

                try
                {
                    if (u.DefId == "settler") HandleSettler(s, p, u);
                    else if (u.DefId == "scout") HandleScout(s, p, u);
                    else if (u.Def.IsCivilian) u.Fortified = true;   // その他の民間人は待機
                    else if (u.Def.IsNaval) HandleNaval(s, p, u, atWar);
                    else HandleMilitary(s, p, u, atWar);
                }
                catch (Exception)
                {
                    // ターンは必ず完了させる:失敗したユニットは防御態勢で放置
                    if (!u.IsDead) u.Fortified = true;
                }
            }
        }

        // ---------------- 開拓者 ----------------

        /// <summary>
        /// 開拓者:最良の入植地(半径1の産出合計・首都への近さ・敵兵の有無で採点)へ移動し、
        /// 到着したら都市を建設する。最初の都市はスタート地点に即建設。
        /// </summary>
        void HandleSettler(GameState s, Player p, Unit u)
        {
            bool currentValid = s.CanFoundCityAt(p, u.Coord);

            // 最初の都市はその場で即建設(スモーク条件:序盤に必ず都市ができる)
            if (currentValid && p.Cities.Count == 0)
            {
                FoundCityNow(s, p, u);
                return;
            }

            var candidates = CollectSettleSites(s, p, u);

            if (candidates.Count == 0)
            {
                if (currentValid) { FoundCityNow(s, p, u); return; }
                if (u.MovesLeft > 0) WanderStep(s, u);
                else u.Fortified = true;
                return;
            }

            if (currentValid)
            {
                int currentScore = SiteScore(s, p, u.Coord);
                var top = candidates[0];
                if (top.Coord == u.Coord || currentScore + SettlerScoreMargin >= top.Score)
                {
                    FoundCityNow(s, p, u);
                    return;
                }
            }

            if (u.MovesLeft <= 0) { u.Fortified = true; return; }

            int tries = 0;
            for (int i = 0; i < candidates.Count && tries < MaxSettlerPathTries; i++)
            {
                var cand = candidates[i];
                if (cand.Coord == u.Coord)
                {
                    if (currentValid) { FoundCityNow(s, p, u); return; }
                    continue;
                }
                tries++;
                var path = Pathfinder.FindPath(s, u, cand.Coord);
                if (path == null) continue;

                u.OrderMove(s, path);
                if (u.Coord == cand.Coord && s.CanFoundCityAt(p, u.Coord))
                    FoundCityNow(s, p, u);
                return;
            }

            // どの候補にも到達できない → 現在地が有効ならここで妥協、無理なら少し動く
            if (currentValid) { FoundCityNow(s, p, u); return; }
            WanderStep(s, u);
        }

        void FoundCityNow(GameState s, Player p, Unit u)
        {
            if (u == null || u.IsDead) return;
            if (!s.CanFoundCityAt(p, u.Coord)) { u.Fortified = true; return; }
            s.FoundCity(p, u.Coord);
            s.KillUnit(u);   // 開拓者は消費される
        }

        struct Site
        {
            public HexCoord Coord;
            public int Score;
        }

        List<Site> CollectSettleSites(GameState s, Player p, Unit u)
        {
            var list = new List<Site>();
            foreach (var t in s.Map.TilesInRange(u.Coord, SettlerSearchRadius))
            {
                if (!p.Explored.Contains(t.Coord)) continue;       // 既知の土地のみ
                if (!s.CanFoundCityAt(p, t.Coord)) continue;
                int score = SiteScore(s, p, t.Coord) - u.Coord.DistanceTo(t.Coord) * 3;
                list.Add(new Site { Coord = t.Coord, Score = score });
            }
            list.Sort((a, b) =>
            {
                int c = b.Score.CompareTo(a.Score);
                if (c != 0) return c;
                c = a.Coord.q.CompareTo(b.Coord.q);
                return c != 0 ? c : a.Coord.r.CompareTo(b.Coord.r);
            });
            return list;
        }

        /// <summary>入植地の採点:半径1の産出合計×4 − 首都からの距離 − 近くの敵兵ペナルティ。</summary>
        int SiteScore(GameState s, Player p, HexCoord coord)
        {
            int yieldSum = 0;
            foreach (var t in s.Map.TilesInRange(coord, 1))
            {
                var y = t.GetYields();
                yieldSum += y.Food + y.Production;
            }
            int score = yieldSum * 4;

            var capital = GetCapital(p);
            if (capital != null) score -= coord.DistanceTo(capital.Coord);

            foreach (var t in s.Map.TilesInRange(coord, 2))
                if (t.Unit != null && t.Unit.PlayerId != p.Id && !t.Unit.Def.IsCivilian)
                    score -= 12;

            return score;
        }

        static City GetCapital(Player p)
        {
            if (p.CapitalCityId < 0) return null;
            for (int i = 0; i < p.Cities.Count; i++)
                if (p.Cities[i].Id == p.CapitalCityId) return p.Cities[i];
            return null;
        }

        // ---------------- 斥候 ----------------

        /// <summary>斥候:最寄りの未踏タイルへ向かう。見つからなければランダムに歩く。</summary>
        void HandleScout(GameState s, Player p, Unit u)
        {
            if (u.MovesLeft <= 0) return;

            int tries = 0;
            for (int radius = 1; radius <= MaxScoutRingRadius && tries < MaxScoutPathTries; radius++)
            {
                foreach (var c in u.Coord.Ring(radius))
                {
                    if (tries >= MaxScoutPathTries) break;
                    if (!s.Map.InBounds(c)) continue;
                    if (p.Explored.Contains(c)) continue;
                    var t = s.Map.Get(c);
                    if (t == null || !t.IsPassable || t.Unit != null) continue;
                    if (t.City != null && t.City.PlayerId != p.Id) continue;

                    tries++;
                    var path = Pathfinder.FindPath(s, u, c);
                    if (path != null)
                    {
                        u.OrderMove(s, path);
                        return;
                    }
                }
            }

            // 未踏地に到達できない → うろつく(動けなければ防御態勢)
            WanderStep(s, u);
        }

        // ---------------- 軍事ユニット ----------------

        /// <summary>
        /// 艦船AI。交戦中は敵艦を迎撃し、敵艦がいなければ敵港の沖へ進出して封鎖する。
        /// 平時は最寄りの自国港沖へ戻る。候補は水域だけに限定する。
        /// </summary>
        void HandleNaval(GameState s, Player p, Unit u, bool atWar)
        {
            if (u.MovesLeft <= 0) { u.Fortified = true; return; }
            if (atWar && TryAttackBest(s, p, u)) return;

            if (atWar)
            {
                Unit target = NearestEnemyNaval(s, p, u);
                if (target != null)
                {
                    var attackPath = Pathfinder.FindPath(s, u, target.Coord, true);
                    if (attackPath != null)
                    {
                        u.OrderMove(s, attackPath);
                        if (!u.IsDead) TryAttackBest(s, p, u);
                        return;
                    }
                }

                HexCoord? blockade = NearestEnemyHarborWater(s, p, u);
                if (blockade.HasValue)
                {
                    if (u.Coord == blockade.Value) { u.Fortified = true; return; }
                    var blockadePath = Pathfinder.FindPath(s, u, blockade.Value);
                    if (blockadePath != null)
                    {
                        u.OrderMove(s, blockadePath);
                        u.Fortified = true;
                        return;
                    }
                }
            }
            else
            {
                HexCoord? home = NearestOwnHarborWater(s, p, u);
                if (home.HasValue)
                {
                    if (u.Coord == home.Value) { u.Fortified = true; return; }
                    var homePath = Pathfinder.FindPath(s, u, home.Value);
                    if (homePath != null)
                    {
                        u.OrderMove(s, homePath);
                        u.Fortified = true;
                        return;
                    }
                }
            }
            WanderStep(s, u);
        }

        static Unit NearestEnemyNaval(GameState s, Player p, Unit u)
        {
            Unit best = null;
            int bestKey = int.MaxValue;
            for (int i = 0; i < s.Players.Count; i++)
            {
                Player rival = s.Players[i];
                if (rival == p || rival.IsEliminated || !p.IsAtWarWith(rival.Id)) continue;
                for (int j = 0; j < rival.Units.Count; j++)
                {
                    Unit candidate = rival.Units[j];
                    if (candidate == null || candidate.IsDead || !candidate.Def.IsNaval) continue;
                    int distance = u.Coord.DistanceTo(candidate.Coord);
                    int key = p.Explored.Contains(candidate.Coord) ? distance : distance + 100;
                    if (key < bestKey || (key == bestKey &&
                        (best == null || candidate.Id < best.Id)))
                    {
                        bestKey = key;
                        best = candidate;
                    }
                }
            }
            return best;
        }

        static HexCoord? NearestEnemyHarborWater(GameState s, Player p, Unit u)
        {
            HexCoord? best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < s.Players.Count; i++)
            {
                Player rival = s.Players[i];
                if (rival == p || rival.IsEliminated || !p.IsAtWarWith(rival.Id)) continue;
                for (int j = 0; j < rival.Cities.Count; j++)
                {
                    City city = rival.Cities[j];
                    if (!LogisticsSystem.HasHarbor(city)) continue;
                    foreach (Tile water in s.Map.NeighborsOf(city.Coord))
                    {
                        if (!water.IsWater || water.Unit != null || water.City != null) continue;
                        int distance = u.Coord.DistanceTo(water.Coord);
                        if (distance < bestDistance || (distance == bestDistance &&
                            (!best.HasValue || CompareCoord(water.Coord, best.Value) < 0)))
                        {
                            bestDistance = distance;
                            best = water.Coord;
                        }
                    }
                }
            }
            return best;
        }

        static HexCoord? NearestOwnHarborWater(GameState s, Player p, Unit u)
        {
            HexCoord? best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < p.Cities.Count; i++)
            {
                City city = p.Cities[i];
                if (!LogisticsSystem.HasHarbor(city)) continue;
                foreach (Tile water in s.Map.NeighborsOf(city.Coord))
                {
                    if (!water.IsWater || (water.Unit != null && water.Unit != u) ||
                        water.City != null) continue;
                    int distance = u.Coord.DistanceTo(water.Coord);
                    if (distance < bestDistance || (distance == bestDistance &&
                        (!best.HasValue || CompareCoord(water.Coord, best.Value) < 0)))
                    {
                        bestDistance = distance;
                        best = water.Coord;
                    }
                }
            }
            return best;
        }

        static int CompareCoord(HexCoord a, HexCoord b)
        {
            int comparison = a.r.CompareTo(b.r);
            return comparison != 0 ? comparison : a.q.CompareTo(b.q);
        }

        void HandleMilitary(GameState s, Player p, Unit u, bool atWar)
        {
            if (atWar)
            {
                // 1) 今すぐ攻撃できるなら最良の目標を叩く
                if (TryAttackBest(s, p, u)) return;

                // 2) 前線での位置取り(遠隔=非隣接の射撃位置 / 近接=都市隣接タイル)。
                //    位置取りできなければ従来どおり最寄りの敵都市/敵ユニットへ進軍し、届けば攻撃
                if (u.MovesLeft > 0 && MoveToWarFront(s, p, u))
                {
                    if (!s.IsGameOver && !u.IsDead) TryAttackBest(s, p, u);
                    return;
                }

                u.Fortified = true;
                return;
            }

            // ---- 平時 ----

            // 護衛が付いていない開拓者がいれば随伴する
            var settler = FindSettlerNeedingEscort(s, p, u);
            if (settler != null)
            {
                if (u.Coord.DistanceTo(settler.Coord) <= 1) { u.Fortified = true; return; }
                if (u.MovesLeft > 0)
                {
                    foreach (var t in s.Map.NeighborsOf(settler.Coord))
                    {
                        if (!t.IsPassable || t.Unit != null) continue;
                        if (t.City != null && t.City.PlayerId != p.Id) continue;
                        var path = Pathfinder.FindPath(s, u, t.Coord);
                        if (path != null)
                        {
                            u.OrderMove(s, path);
                            u.Fortified = true;
                            return;
                        }
                    }
                }
            }

            // 自国領なら防御態勢
            var tile = s.Map.Get(u.Coord);
            if (tile != null && tile.OwnerPlayerId == p.Id) { u.Fortified = true; return; }

            // 領外にいるなら最寄りの自都市へ戻る
            var home = NearestOwnCity(p, u.Coord);
            if (home != null && u.MovesLeft > 0)
            {
                var path = Pathfinder.FindPath(s, u, home.Coord);
                if (path == null)
                {
                    foreach (var t in s.Map.NeighborsOf(home.Coord))
                    {
                        if (!t.IsPassable || t.Unit != null) continue;
                        path = Pathfinder.FindPath(s, u, t.Coord);
                        if (path != null) break;
                    }
                }
                if (path != null) u.OrderMove(s, path);
            }
            u.Fortified = true;
        }

        /// <summary>
        /// 攻撃可能な交戦相手の中から最良の目標を選んで攻撃する。
        /// 優先:占領確実な都市 > 民間人捕獲 > 撃破確実なユニット > 高ダメージ。
        /// 反撃で自滅する近接攻撃は避ける。和平中の相手は決して攻撃しない。
        /// </summary>
        bool TryAttackBest(GameState s, Player p, Unit u)
        {
            if (u == null || u.IsDead || u.MovesLeft <= 0) return false;
            var targets = Pathfinder.AttackableTiles(s, u);
            if (targets == null || targets.Count == 0) return false;

            bool melee = !u.Def.IsRanged;
            int atkBase = melee ? u.Def.Strength : u.Def.RangedStrength;
            float atkEff = GameRules.HealthScaledStrength(atkBase, u.Hp, GameRules.UnitMaxHp);
            atkEff = LogisticsSystem.ScaleCombat(u, atkEff);

            Tile best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < targets.Count; i++)
            {
                var t = s.Map.Get(targets[i]);
                if (t == null) continue;
                float targetAtkEff = atkEff *
                    NaturalGeographySystem.RiverCrossingAttackMultiplier(s, u, t.Coord);

                float score;
                if (t.City != null && t.City.PlayerId != u.PlayerId)
                {
                    if (!p.IsAtWarWith(t.City.PlayerId)) continue;   // 和平中は攻撃しない
                    var city = t.City;
                    float defEff = GameRules.HealthScaledStrength(city.DefenseStrength(s), city.Hp, city.MaxHp);
                    int dmg = EstimateDamage(targetAtkEff, defEff);
                    if (melee && city.Hp - dmg <= 1)
                    {
                        score = 10000f;   // 占領確実
                    }
                    else
                    {
                        score = 300f + dmg;
                        if (melee)
                        {
                            int back = EstimateDamage(defEff, targetAtkEff);
                            score -= back;
                            if (back >= u.Hp) score -= 6000f;   // 自滅回避
                            // 城壁付き・無傷の都市への単独近接攻撃は消耗するだけなので、
                            // 味方の遠隔支援が射程内に無い限り見送る(自滅回避と同じ拒否点)
                            if (city.Hp >= city.MaxHp && city.Buildings.Contains("walls") &&
                                !HasRangedSupportNear(p, u, city.Coord))
                                score -= 6000f;
                        }
                    }
                }
                else if (t.Unit != null && t.Unit.PlayerId != u.PlayerId)
                {
                    if (!p.IsAtWarWith(t.Unit.PlayerId)) continue;   // 和平中は攻撃しない
                    var e = t.Unit;
                    if (melee && e.Def.Strength <= 0)
                    {
                        score = 5000f;   // 民間人捕獲
                    }
                    else
                    {
                        float defEff = Combat.EffectiveDefense(s, t);
                        int dmg = EstimateDamage(targetAtkEff, defEff);
                        bool kill = dmg >= e.Hp;
                        score = kill ? 2000f + e.Def.Strength + e.Def.RangedStrength : dmg * 2f;
                        if (melee)
                        {
                            int back = EstimateDamage(defEff, targetAtkEff);
                            score -= back;
                            if (!kill && back >= u.Hp) score -= 6000f;   // 自滅回避
                        }
                    }
                }
                else
                {
                    continue;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            if (best == null || bestScore <= -1000f) return false;
            Combat.PerformAttack(s, u, best);
            return true;
        }

        /// <summary>期待ダメージ(GameRules.CombatDamage の中央値。乱数は消費しない)。</summary>
        static int EstimateDamage(float attackStr, float defenseStr)
        {
            if (defenseStr < 1f) defenseStr = 1f;
            float ratio = attackStr / defenseStr;
            int dmg = Mathf.RoundToInt(30f * Mathf.Pow(ratio, 1.35f));
            return Mathf.Clamp(dmg, 4, 90);
        }

        /// <summary>最寄りの交戦相手の都市(既知を優先。d+100 で未知の都市を後回しにする)。</summary>
        City NearestEnemyCity(GameState s, Player p, Unit u)
        {
            City targetCity = null;
            int bestKey = int.MaxValue;
            for (int i = 0; i < s.Players.Count; i++)
            {
                var pl = s.Players[i];
                if (pl == p || pl.IsEliminated || !p.IsAtWarWith(pl.Id)) continue;
                for (int j = 0; j < pl.Cities.Count; j++)
                {
                    var c = pl.Cities[j];
                    int d = u.Coord.DistanceTo(c.Coord);
                    int key = p.Explored.Contains(c.Coord) ? d : d + 100;   // 既知の都市を優先
                    if (key < bestKey)
                    {
                        bestKey = key;
                        targetCity = c;
                    }
                }
            }
            return targetCity;
        }

        /// <summary>
        /// 戦線での位置取り(2026-07-20 追加)。最寄りの敵都市に対して、遠隔ユニットは
        /// 隣接しない射撃位置(距離=射程、可能なら味方近接の後方)へ、近接ユニットは
        /// 都市の隣接タイルへ移動する。適切な位置取りができなければ従来の進軍にフォールバック。
        /// 追加の状態は持たず、探索回数は MaxSiegePathTries で制限する(決定的)。
        /// </summary>
        bool MoveToWarFront(GameState s, Player p, Unit u)
        {
            var city = NearestEnemyCity(s, p, u);
            if (city != null)
            {
                if (u.Def.IsRanged)
                {
                    if (MoveRangedToFiringPosition(s, p, u, city)) return true;
                }
                else if (MoveMeleeToSiegeTile(s, p, u, city))
                {
                    return true;
                }
            }
            return MarchTowardEnemy(s, p, u);
        }

        /// <summary>
        /// 近接ユニット:目標都市の隣接タイルを1つ確保する。すでに隣接していれば保持
        /// (攻撃するかは TryAttackBest 側の判断。城壁都市への単独突撃は見送られ得る)。
        /// </summary>
        bool MoveMeleeToSiegeTile(GameState s, Player p, Unit u, City city)
        {
            if (u.Coord.DistanceTo(city.Coord) <= 1)
            {
                u.Fortified = true;   // 包囲位置を保持して支援を待つ
                return true;
            }

            // 空いている隣接タイルを近い順(同距離は座標順)に試す
            var candidates = new List<HexCoord>();
            foreach (var t in s.Map.NeighborsOf(city.Coord))
            {
                if (!t.IsPassable || t.Unit != null || t.City != null) continue;
                candidates.Add(t.Coord);
            }
            candidates.Sort((a, b) =>
            {
                int c = u.Coord.DistanceTo(a).CompareTo(u.Coord.DistanceTo(b));
                if (c != 0) return c;
                c = a.q.CompareTo(b.q);
                return c != 0 ? c : a.r.CompareTo(b.r);
            });

            int tries = 0;
            for (int i = 0; i < candidates.Count && tries < MaxSiegePathTries; i++)
            {
                tries++;
                var path = Pathfinder.FindPath(s, u, candidates[i]);
                if (path == null) continue;
                u.OrderMove(s, path);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 遠隔ユニット:目標都市に隣接しない射撃位置(距離=射程)へ移動する。
        /// 候補は味方近接ユニットの隣(前衛の後方)を優先し、次に近さ、最後に座標順で決定的に選ぶ。
        /// すでに距離2〜射程内にいれば陣地を保持する。
        /// </summary>
        bool MoveRangedToFiringPosition(GameState s, Player p, Unit u, City city)
        {
            int range = u.Def.Range;
            if (range < 2) return false;   // 射程1以下では隣接回避の立ち位置を作れない

            int dist = u.Coord.DistanceTo(city.Coord);
            if (dist >= 2 && dist <= range)
            {
                u.Fortified = true;   // すでに非隣接の射撃位置(攻撃は呼び出し側で実施済み/実施される)
                return true;
            }

            // 距離=射程のリング上から通行可能・空きのタイルを収集
            var candidates = new List<HexCoord>();
            foreach (var c in city.Coord.Ring(range))
            {
                if (!s.Map.InBounds(c)) continue;
                var t = s.Map.Get(c);
                if (t == null || !t.IsPassable || t.Unit != null || t.City != null) continue;
                candidates.Add(c);
            }
            if (candidates.Count == 0) return false;

            candidates.Sort((a, b) =>
            {
                int sa = HasAdjacentFriendlyMelee(s, p, a, u) ? 0 : 1;
                int sb = HasAdjacentFriendlyMelee(s, p, b, u) ? 0 : 1;
                if (sa != sb) return sa - sb;                  // 味方近接の後方を優先
                int c = u.Coord.DistanceTo(a).CompareTo(u.Coord.DistanceTo(b));
                if (c != 0) return c;
                c = a.q.CompareTo(b.q);
                return c != 0 ? c : a.r.CompareTo(b.r);
            });

            int tries = 0;
            for (int i = 0; i < candidates.Count && tries < MaxSiegePathTries; i++)
            {
                tries++;
                var path = Pathfinder.FindPath(s, u, candidates[i]);
                if (path == null) continue;
                u.OrderMove(s, path);
                return true;
            }
            return false;
        }

        /// <summary>指定タイルの隣に自軍の近接戦闘ユニット(自分以外)がいるか。</summary>
        static bool HasAdjacentFriendlyMelee(GameState s, Player p, HexCoord coord, Unit self)
        {
            foreach (var t in s.Map.NeighborsOf(coord))
            {
                var fu = t.Unit;
                if (fu != null && fu != self && fu.PlayerId == p.Id &&
                    !fu.Def.IsCivilian && !fu.Def.IsRanged)
                    return true;
            }
            return false;
        }

        /// <summary>味方の遠隔ユニット(自分以外)が目標座標を射程に収めているか(近接支援判定)。</summary>
        static bool HasRangedSupportNear(Player p, Unit self, HexCoord target)
        {
            for (int i = 0; i < p.Units.Count; i++)
            {
                var ru = p.Units[i];
                if (ru == null || ru.IsDead || ru == self) continue;
                if (ru.Def.IsCivilian || !ru.Def.IsRanged) continue;
                if (ru.Coord.DistanceTo(target) <= ru.Def.Range) return true;
            }
            return false;
        }

        /// <summary>最寄りの敵都市(既知を優先)へ進軍。都市が無ければ敵ユニットへ。移動したら true。</summary>
        bool MarchTowardEnemy(GameState s, Player p, Unit u)
        {
            // 敵都市
            City targetCity = NearestEnemyCity(s, p, u);
            if (targetCity != null)
            {
                var path = Pathfinder.FindPath(s, u, targetCity.Coord, true);
                if (path != null)
                {
                    u.OrderMove(s, path);
                    return true;
                }
            }

            // 敵ユニット
            Unit targetUnit = null;
            int bestKey = int.MaxValue;
            for (int i = 0; i < s.Players.Count; i++)
            {
                var pl = s.Players[i];
                if (pl == p || pl.IsEliminated || !p.IsAtWarWith(pl.Id)) continue;
                for (int j = 0; j < pl.Units.Count; j++)
                {
                    var e = pl.Units[j];
                    if (e == null || e.IsDead) continue;
                    int d = u.Coord.DistanceTo(e.Coord);
                    int key = p.Explored.Contains(e.Coord) ? d : d + 100;
                    if (key < bestKey)
                    {
                        bestKey = key;
                        targetUnit = e;
                    }
                }
            }
            if (targetUnit != null)
            {
                var path = Pathfinder.FindPath(s, u, targetUnit.Coord, true);
                if (path != null)
                {
                    u.OrderMove(s, path);
                    return true;
                }
            }
            return false;
        }

        /// <summary>護衛(隣接する自軍の戦闘ユニット)が付いていない最寄りの開拓者。距離10まで。</summary>
        Unit FindSettlerNeedingEscort(GameState s, Player p, Unit self)
        {
            Unit best = null;
            int bestD = 11;
            for (int i = 0; i < p.Units.Count; i++)
            {
                var su = p.Units[i];
                if (su == null || su.IsDead || su.DefId != "settler") continue;

                bool escorted = false;
                foreach (var t in s.Map.NeighborsOf(su.Coord))
                {
                    if (t.Unit != null && t.Unit != self && t.Unit.PlayerId == p.Id && !t.Unit.Def.IsCivilian)
                    {
                        escorted = true;
                        break;
                    }
                }
                if (escorted) continue;

                int d = self.Coord.DistanceTo(su.Coord);
                if (d < bestD)
                {
                    bestD = d;
                    best = su;
                }
            }
            return best;
        }

        static City NearestOwnCity(Player p, HexCoord from)
        {
            City best = null;
            int bestD = int.MaxValue;
            for (int i = 0; i < p.Cities.Count; i++)
            {
                int d = from.DistanceTo(p.Cities[i].Coord);
                if (d < bestD)
                {
                    bestD = d;
                    best = p.Cities[i];
                }
            }
            return best;
        }

        // ---------------- 共通ヘルパー ----------------

        /// <summary>生存している交戦相手がいるか(HashSet の列挙順に依存しないよう Players を走査)。</summary>
        static bool AnyActiveWar(GameState s, Player p)
        {
            for (int i = 0; i < s.Players.Count; i++)
            {
                var q = s.Players[i];
                if (q == p || q.IsEliminated) continue;
                if (p.IsAtWarWith(q.Id)) return true;
            }
            return false;
        }

        /// <summary>隣接タイルへランダムに歩く(移動力の続く限り、上限8歩)。動けなければ防御態勢。</summary>
        bool WanderStep(GameState s, Unit u)
        {
            bool moved = false;
            int guard = 8;
            while (u.MovesLeft > 0 && guard-- > 0)
            {
                var options = new List<HexCoord>();
                foreach (var t in s.Map.NeighborsOf(u.Coord))
                {
                    if (!GameRules.CanUnitEnter(u.Def, t) || t.Unit != null) continue;
                    if (t.City != null && t.City.PlayerId != u.PlayerId) continue;
                    options.Add(t.Coord);
                }
                if (options.Count == 0) break;

                var next = options[s.Rng.Next(options.Count)];
                if (!u.TryStepTo(s, next)) break;
                moved = true;
            }
            if (!moved) u.Fortified = true;
            return moved;
        }
    }
}
