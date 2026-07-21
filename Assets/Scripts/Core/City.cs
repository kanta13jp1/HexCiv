using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>都市。人口・食料・生産・建物・防御を管理する。</summary>
    public class City
    {
        public int Id;
        public int PlayerId;
        public string NameJa;
        public HexCoord Coord;

        /// <summary>人口(開始時1)。</summary>
        public int Population = 1;
        public int FoodStored;
        public int ProductionStored;

        public int Hp = GameRules.CityMaxHp;
        public int MaxHp = GameRules.CityMaxHp;

        /// <summary>建設済み建物のId。</summary>
        public List<string> Buildings = new List<string>();
        /// <summary>現在の生産項目。null = 未選択。</summary>
        public ProductionItem CurrentProduction;

        /// <summary>都市防御力 = 基本 + 人口 + 城壁 + 駐留ユニット(戦闘力/4)。</summary>
        public int DefenseStrength(GameState s)
        {
            int str = GameRules.CityBaseStrength + Population * GameRules.CityStrengthPerPop;
            for (int i = 0; i < Buildings.Count; i++)
                str += GameRules.GetBuilding(Buildings[i]).CityDefense;
            var tile = s.Map.Get(Coord);
            if (tile != null && tile.Unit != null && tile.Unit.PlayerId == PlayerId && !tile.Unit.Def.IsCivilian)
                str += tile.Unit.Def.Strength / 4;
            return str;
        }

        /// <summary>
        /// 都市の産出 = 中心タイル + 領内(この都市所有)の最良タイルを人口数だけ労働 + 建物ボーナス。
        /// 科学には人口を加算する。
        /// </summary>
        public Yields ComputeYields(GameState s)
        {
            var center = s.Map.Get(Coord);
            var total = center != null ? center.GetYields() : new Yields(0, 0);

            var candidates = new List<Tile>();
            foreach (var t in s.Map.TilesInRange(Coord, GameRules.CityWorkRadius))
            {
                if (t.Coord == Coord) continue;
                if (t.OwnerCityId != Id) continue;   // 他都市の労働タイルは使えない
                candidates.Add(t);
            }
            candidates.Sort((a, b) =>
            {
                var ya = a.GetYields();
                var yb = b.GetYields();
                int cmp = (yb.Food + yb.Production).CompareTo(ya.Food + ya.Production);
                if (cmp != 0) return cmp;
                cmp = yb.Food.CompareTo(ya.Food);
                if (cmp != 0) return cmp;
                cmp = a.Coord.q.CompareTo(b.Coord.q);
                if (cmp != 0) return cmp;
                return a.Coord.r.CompareTo(b.Coord.r);
            });

            int worked = Math.Min(Population, candidates.Count);
            for (int i = 0; i < worked; i++)
                total += candidates[i].GetYields();

            for (int i = 0; i < Buildings.Count; i++)
                total += GameRules.GetBuilding(Buildings[i]).Bonus;

            total.Science += Population;
            return total;
        }

        /// <summary>生産可能な項目(技術条件を満たすユニット + 未建設の建物)。</summary>
        public List<ProductionItem> AvailableProduction(GameState s)
        {
            var list = new List<ProductionItem>();
            var owner = s.GetPlayer(PlayerId);
            if (owner == null) return list;
            foreach (var u in GameRules.Units)
                if (owner.HasTech(u.RequiresTech)) list.Add(ProductionItem.FromUnit(u));
            foreach (var b in GameRules.Buildings)
                if (owner.HasTech(b.RequiresTech) && !Buildings.Contains(b.Id)) list.Add(ProductionItem.FromBuilding(b));
            return list;
        }

        public void SetProduction(ProductionItem item)
        {
            CurrentProduction = item;
        }

        /// <summary>現在の生産が完了するまでのターン数。完了しないなら99。</summary>
        public int TurnsToComplete(GameState s)
        {
            if (CurrentProduction == null) return 99;
            int prod = ComputeYields(s).Production;
            if (prod <= 0) return 99;
            int remaining = CurrentProduction.Cost - ProductionStored;
            if (remaining <= 0) return 1;
            return Math.Min(99, (remaining + prod - 1) / prod);
        }

        /// <summary>次の人口成長までのターン数。成長しないなら99。</summary>
        public int TurnsToGrow(GameState s)
        {
            int surplus = ComputeYields(s).Food - Population * GameRules.FoodPerPop;
            if (surplus <= 0) return 99;
            int remaining = GameRules.GrowthFoodNeeded(Population) - FoodStored;
            if (remaining <= 0) return 1;
            return Math.Min(99, (remaining + surplus - 1) / surplus);
        }

        /// <summary>ターン開始処理:食料(成長/飢餓)、生産進行(完成→配置)、都市HP回復。</summary>
        public void ProcessTurnStart(GameState s)
        {
            var owner = s.GetPlayer(PlayerId);
            var y = ComputeYields(s);

            // ---- 食料 ----
            int surplus = y.Food - Population * GameRules.FoodPerPop;
            FoodStored += surplus;
            int guard = 0;
            while (FoodStored >= GameRules.GrowthFoodNeeded(Population) && guard++ < 10)
            {
                FoodStored -= GameRules.GrowthFoodNeeded(Population);
                Population++;
            }
            if (FoodStored < 0)
            {
                // 飢餓:赤字なら人口減(最低1)、貯蔵は0に戻す
                if (surplus < 0 && Population > 1) Population--;
                FoodStored = 0;
            }

            // ---- 生産 ----
            if (CurrentProduction != null)
            {
                int culturalProduction = CultureSystem.ScaleProduction(owner, y.Production);
                // AI都市は難易度に応じて生産蓄積を補正(普通=100%で無変換。2026-07-20 追加)
                ProductionStored += DifficultyRules.ScaleForAI(s, owner, culturalProduction, DifficultyRules.AIProductionPercent);
                if (ProductionStored >= CurrentProduction.Cost)
                {
                    if (CurrentProduction.Kind == ProductionKind.Building)
                    {
                        if (!Buildings.Contains(CurrentProduction.Id)) Buildings.Add(CurrentProduction.Id);
                        ProductionStored -= CurrentProduction.Cost;
                        s.EmitLog($"「{NameJa}」で{CurrentProduction.NameJa}が完成した");
                        CurrentProduction = null;
                    }
                    else
                    {
                        var spawn = FindSpawnTile(s);
                        if (spawn != null && owner != null)
                        {
                            s.CreateUnit(owner, CurrentProduction.Id, spawn.Coord);
                            ProductionStored -= CurrentProduction.Cost;
                            s.EmitLog($"「{NameJa}」で{CurrentProduction.NameJa}が完成した");
                            CurrentProduction = null;
                        }
                        // 空きタイルが無ければ保留(次ターンに再試行)
                    }
                }
            }

            // ---- HP回復 ----
            Hp = Math.Min(MaxHp, Hp + GameRules.CityHealPerTurn);
        }

        /// <summary>人口が閾値以上なら半径2の未所有タイルを領土化する。</summary>
        public void ExpandBordersIfNeeded(GameState s)
        {
            if (Population < GameRules.CityBorderGrowPop) return;
            foreach (var t in s.Map.TilesInRange(Coord, GameRules.CityBorderRadiusLarge))
            {
                if (t.OwnerPlayerId == -1)
                {
                    t.OwnerPlayerId = PlayerId;
                    t.OwnerCityId = Id;
                }
            }
        }

        /// <summary>生産ユニットの配置先:都市タイルが空いていれば都市、なければ最初の空き通行可能隣接タイル。</summary>
        Tile FindSpawnTile(GameState s)
        {
            var center = s.Map.Get(Coord);
            if (center != null && center.Unit == null) return center;
            foreach (var t in s.Map.NeighborsOf(Coord))
                if (t.IsPassable && t.Unit == null && t.City == null) return t;
            return null;
        }
    }
}
