using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>
    /// GameState の操作(ロジック)部分。ユニット/都市の生成・破壊、都市の建設・占領、
    /// 滅亡・制覇勝利の判定を担う。
    /// </summary>
    public partial class GameState
    {
        /// <summary>全プレイヤーの生存ユニット(スナップショット)。</summary>
        public IEnumerable<Unit> AllUnits
        {
            get
            {
                var list = new List<Unit>();
                for (int i = 0; i < Players.Count; i++)
                {
                    var units = Players[i].Units;
                    for (int j = 0; j < units.Count; j++)
                        if (!units[j].IsDead) list.Add(units[j]);
                }
                return list;
            }
        }

        /// <summary>全プレイヤーの都市(スナップショット)。</summary>
        public IEnumerable<City> AllCities
        {
            get
            {
                var list = new List<City>();
                for (int i = 0; i < Players.Count; i++)
                    list.AddRange(Players[i].Cities);
                return list;
            }
        }

        /// <summary>ユニットを生成してタイルに配置する。tile.Unit も更新する。</summary>
        public Unit CreateUnit(Player owner, string unitDefId, HexCoord coord)
        {
            var def = GameRules.GetUnit(unitDefId);
            var u = new Unit
            {
                Id = TakeNextUnitId(),
                PlayerId = owner.Id,
                DefId = unitDefId,
                Coord = coord,
                Hp = GameRules.UnitMaxHp,
                MovesLeft = def.Moves,
            };
            owner.Units.Add(u);
            var tile = Map.Get(coord);
            if (tile != null) tile.Unit = u;
            Visibility.Recompute(this, owner);
            return u;
        }

        /// <summary>ユニットを死亡させ、タイルとプレイヤーのリストから除去する。</summary>
        public void KillUnit(Unit u)
        {
            if (u == null) return;
            var tile = Map.Get(u.Coord);
            if (tile != null && tile.Unit == u) tile.Unit = null;
            var owner = GetPlayer(u.PlayerId);
            if (owner != null) owner.Units.Remove(u);
            if (u.Hp > 0) u.Hp = 0;
            u.GotoPath = null;
        }

        /// <summary>都市を建設できるか(通行可能な陸地、他都市と距離3以上、他文明の領土でない)。</summary>
        public bool CanFoundCityAt(Player owner, HexCoord coord)
        {
            if (owner == null) return false;
            var tile = Map.Get(coord);
            if (tile == null || !tile.IsPassable) return false;
            if (tile.City != null) return false;
            if (tile.OwnerPlayerId != -1 && tile.OwnerPlayerId != owner.Id) return false;
            foreach (var c in AllCities)
                if (c.Coord.DistanceTo(coord) < GameRules.MinCityDistance) return false;
            return true;
        }

        /// <summary>都市を建設し、半径1の領土を主張する。命名は owner.NextCityName(this)。</summary>
        public City FoundCity(Player owner, HexCoord coord)
        {
            var tile = Map.Get(coord);
            var city = new City
            {
                Id = TakeNextCityId(),
                PlayerId = owner.Id,
                NameJa = owner.NextCityName(this),
                Coord = coord,
                Population = 1,
                MaxHp = GameRules.CityMaxHp,
                Hp = GameRules.CityMaxHp,
            };
            owner.Cities.Add(city);
            if (tile != null) tile.City = city;

            foreach (var t in Map.TilesInRange(coord, GameRules.CityBorderRadiusSmall))
            {
                if (t.Coord == coord || t.OwnerPlayerId == -1)
                {
                    t.OwnerPlayerId = owner.Id;
                    t.OwnerCityId = city.Id;
                }
            }

            if (owner.CapitalCityId < 0) owner.CapitalCityId = city.Id;
            Visibility.Recompute(this, owner);
            EmitLog($"「{owner.NameJa}」が都市「{city.NameJa}」を建設した");
            Bump();
            return city;
        }

        /// <summary>都市を占領し、領土ごと新所有者に移す。HPは50%。首都・滅亡処理を行う。</summary>
        public void CaptureCity(City city, Player newOwner)
        {
            if (city == null || newOwner == null || city.PlayerId == newOwner.Id) return;
            var oldOwner = GetPlayer(city.PlayerId);
            var tile = Map.Get(city.Coord);

            // 都市タイル上の旧所有者のユニット(守備隊)は破壊される。
            if (tile != null && tile.Unit != null && tile.Unit.PlayerId == city.PlayerId)
                KillUnit(tile.Unit);

            if (oldOwner != null)
            {
                oldOwner.Cities.Remove(city);
                if (oldOwner.CapitalCityId == city.Id)
                    oldOwner.CapitalCityId = oldOwner.Cities.Count > 0 ? oldOwner.Cities[0].Id : -1;
            }

            city.PlayerId = newOwner.Id;
            newOwner.Cities.Add(city);
            if (newOwner.CapitalCityId < 0) newOwner.CapitalCityId = city.Id;
            city.Hp = Math.Max(1, city.MaxHp / 2);
            city.CurrentProduction = null;
            city.ProductionStored = 0;

            foreach (var t in Map.AllTiles)
                if (t.OwnerCityId == city.Id) t.OwnerPlayerId = newOwner.Id;

            EmitLog($"都市「{city.NameJa}」が陥落した!");
            RaiseCityCaptured(city, oldOwner, newOwner);   // 型付きイベント(2026-07-20 追加)
            Visibility.Recompute(this, newOwner);
            if (oldOwner != null)
            {
                Visibility.Recompute(this, oldOwner);
                EliminateIfDefeated(oldOwner);
            }
            CheckDominationVictory();
            Bump();
        }

        /// <summary>都市0かつ開拓者0のプレイヤーを滅亡させる。滅亡したら true。</summary>
        internal bool EliminateIfDefeated(Player p)
        {
            if (p == null || p.IsEliminated) return false;
            if (p.Cities.Count > 0) return false;
            for (int i = 0; i < p.Units.Count; i++)
                if (p.Units[i].DefId == "settler") return false;

            foreach (var u in new List<Unit>(p.Units))
                KillUnit(u);
            p.IsEliminated = true;
            p.Visible.Clear();
            p.CurrentResearchId = null;
            EmitLog($"「{p.NameJa}」は滅亡した");
            RaisePlayerEliminated(p);   // 型付きイベント(2026-07-20 追加)
            return true;
        }

        /// <summary>生存プレイヤーが1人だけなら制覇勝利でゲーム終了にする。</summary>
        internal void CheckDominationVictory()
        {
            if (IsGameOver) return;
            Player alive = null;
            int count = 0;
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].IsEliminated) continue;
                alive = Players[i];
                count++;
            }
            if (count == 1 && alive != null)
            {
                IsGameOver = true;
                Winner = alive;
                GameOverMessageJa = $"「{alive.NameJa}」が制覇による勝利を収めた!他の文明はすべて滅んだ";
                EmitLog(GameOverMessageJa);
                RaiseGameEnded(alive, GameOverMessageJa);   // 型付きイベント(2026-07-20 追加)
            }
        }
    }
}
