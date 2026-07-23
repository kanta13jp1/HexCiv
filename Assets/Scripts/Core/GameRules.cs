using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HexCiv.Core
{
    /// <summary>
    /// 全ゲームデータ(地形・資源・ユニット・建物・技術)と共通の計算式。
    /// このファイルは「契約」であり、他モジュールはここのテーブルとメソッド名に依存してよい。
    /// </summary>
    public static class GameRules
    {
        // ---- 定数 ----
        public const int UnitMaxHp = 100;
        public const int CityMaxHp = 150;
        public const int CityBaseStrength = 8;
        public const int CityStrengthPerPop = 1;
        public const int CityHealPerTurn = 10;
        public const int CitySight = 2;
        public const int MinCityDistance = 3;      // 都市間の最小距離(DistanceTo >= 3)
        public const int CityBorderRadiusSmall = 1; // 建設直後の領土半径
        public const int CityBorderRadiusLarge = 2; // 人口3以上で拡張
        public const int CityBorderGrowPop = 3;
        public const int CityWorkRadius = 2;
        public const int FoodPerPop = 2;
        public const int BaseSciencePerCiv = 3;    // 首都(宮殿)ボーナス
        public const int HealOwnTerritory = 15;
        public const int HealNeutral = 10;
        public const int HealEnemyTerritory = 5;
        public const int HealFortifyBonus = 5;
        public const float FortifyDefenseBonus = 0.25f;
        public const float HillDefenseBonus = 0.25f;
        public const float ForestDefenseBonus = 0.25f;
        public const int RiverCrossingMovePenalty = 1;
        public const float RiverCrossingAttackMultiplier = 0.80f;
        public const int ImpassableCost = 9999;
        public const string StartingTech = "agriculture";

        // ---- 地形 ----
        public static readonly Dictionary<TerrainType, TerrainDef> Terrains = new Dictionary<TerrainType, TerrainDef>
        {
            { TerrainType.Ocean,     new TerrainDef { Type = TerrainType.Ocean,     NameJa = "外洋",   BaseYields = new Yields(0, 0), Passable = false, Color = new Color(0.11f, 0.25f, 0.45f) } },
            { TerrainType.Coast,     new TerrainDef { Type = TerrainType.Coast,     NameJa = "沿岸",   BaseYields = new Yields(1, 0), Passable = false, Color = new Color(0.23f, 0.42f, 0.60f) } },
            { TerrainType.Grassland, new TerrainDef { Type = TerrainType.Grassland, NameJa = "草原",   BaseYields = new Yields(2, 0), Color = new Color(0.33f, 0.55f, 0.25f) } },
            { TerrainType.Plains,    new TerrainDef { Type = TerrainType.Plains,    NameJa = "平原",   BaseYields = new Yields(1, 1), Color = new Color(0.65f, 0.58f, 0.30f) } },
            { TerrainType.Desert,    new TerrainDef { Type = TerrainType.Desert,    NameJa = "砂漠",   BaseYields = new Yields(0, 0), Color = new Color(0.85f, 0.75f, 0.48f) } },
            { TerrainType.Tundra,    new TerrainDef { Type = TerrainType.Tundra,    NameJa = "ツンドラ", BaseYields = new Yields(1, 0), Color = new Color(0.55f, 0.55f, 0.48f) } },
            { TerrainType.Snow,      new TerrainDef { Type = TerrainType.Snow,      NameJa = "雪原",   BaseYields = new Yields(0, 0), Color = new Color(0.90f, 0.92f, 0.95f) } },
            { TerrainType.Mountain,  new TerrainDef { Type = TerrainType.Mountain,  NameJa = "山岳",   BaseYields = new Yields(0, 0), Passable = false, Color = new Color(0.45f, 0.43f, 0.42f) } },
        };

        // ---- 資源 ----
        public static readonly Dictionary<ResourceType, ResourceDef> Resources = new Dictionary<ResourceType, ResourceDef>
        {
            { ResourceType.Wheat,  new ResourceDef { Type = ResourceType.Wheat,  NameJa = "小麦", Bonus = new Yields(1, 0), Color = new Color(0.95f, 0.85f, 0.20f) } },
            { ResourceType.Cattle, new ResourceDef { Type = ResourceType.Cattle, NameJa = "牛",   Bonus = new Yields(1, 0), Color = new Color(0.70f, 0.40f, 0.30f) } },
            { ResourceType.Deer,   new ResourceDef { Type = ResourceType.Deer,   NameJa = "鹿",   Bonus = new Yields(1, 0), Color = new Color(0.55f, 0.35f, 0.20f) } },
            { ResourceType.Iron,   new ResourceDef { Type = ResourceType.Iron,   NameJa = "鉄",   Bonus = new Yields(0, 2), Color = new Color(0.35f, 0.35f, 0.42f) } },
            { ResourceType.Horses, new ResourceDef { Type = ResourceType.Horses, NameJa = "馬",   Bonus = new Yields(0, 1), Color = new Color(0.60f, 0.45f, 0.25f) } },
        };

        // ---- ユニット ----
        public static readonly List<UnitDef> Units = new List<UnitDef>
        {
            new UnitDef { Id = "settler",   NameJa = "開拓者",   Glyph = "開", Cost = 80, Moves = 2, Strength = 0,  IsCivilian = true },
            new UnitDef { Id = "scout",     NameJa = "斥候",     Glyph = "斥", Cost = 25, Moves = 3, Strength = 5,  Sight = 3 },
            new UnitDef { Id = "warrior",   NameJa = "戦士",     Glyph = "戦", Cost = 40, Moves = 2, Strength = 8 },
            new UnitDef { Id = "archer",    NameJa = "弓兵",     Glyph = "弓", Cost = 60, Moves = 2, Strength = 6,  RangedStrength = 8,  Range = 2, RequiresTech = "archery" },
            new UnitDef { Id = "spearman",  NameJa = "槍兵",     Glyph = "槍", Cost = 56, Moves = 2, Strength = 11, RequiresTech = "bronze_working" },
            new UnitDef { Id = "swordsman", NameJa = "剣士",     Glyph = "剣", Cost = 75, Moves = 2, Strength = 14, RequiresTech = "iron_working" },
            new UnitDef { Id = "catapult",  NameJa = "カタパルト", Glyph = "投", Cost = 75, Moves = 2, Strength = 5,  RangedStrength = 11, Range = 2, RequiresTech = "mathematics" },
        };

        // ---- 建物 ----
        public static readonly List<BuildingDef> Buildings = new List<BuildingDef>
        {
            new BuildingDef { Id = "monument", NameJa = "記念碑", Cost = 40, Bonus = new Yields(0, 0, 1), DescJa = "科学+1" },
            new BuildingDef { Id = "granary",  NameJa = "穀物庫", Cost = 60, RequiresTech = "pottery", Bonus = new Yields(2, 0, 0), DescJa = "食料+2" },
            new BuildingDef { Id = "library",  NameJa = "図書館", Cost = 80, RequiresTech = "writing", Bonus = new Yields(0, 0, 3), DescJa = "科学+3" },
            new BuildingDef { Id = "workshop", NameJa = "作業場", Cost = 80, RequiresTech = "construction", Bonus = new Yields(0, 2, 0), DescJa = "生産+2" },
            new BuildingDef { Id = "harbor",   NameJa = "港",     Cost = 70, RequiresTech = "construction", Bonus = new Yields(1, 1, 0), DescJa = "食料+1 生産+1・海上補給" },
            new BuildingDef { Id = "walls",    NameJa = "城壁",   Cost = 70, RequiresTech = "masonry", CityDefense = 6, DescJa = "都市防御+6" },
        };

        // ---- 技術 ----
        public static readonly List<TechDef> Techs = new List<TechDef>
        {
            new TechDef { Id = "agriculture",      NameJa = "農業",   Cost = 0,  Prereqs = new string[0],                    DescJa = "開始時に習得済み" },
            new TechDef { Id = "pottery",          NameJa = "陶器",   Cost = 25, Prereqs = new[] { "agriculture" },          DescJa = "穀物庫を解禁" },
            new TechDef { Id = "animal_husbandry", NameJa = "畜産",   Cost = 25, Prereqs = new[] { "agriculture" },          DescJa = "車輪への足がかり" },
            new TechDef { Id = "archery",          NameJa = "弓術",   Cost = 35, Prereqs = new[] { "agriculture" },          DescJa = "弓兵を解禁" },
            new TechDef { Id = "mining",           NameJa = "採鉱",   Cost = 35, Prereqs = new[] { "agriculture" },          DescJa = "青銅器・石工術への足がかり" },
            new TechDef { Id = "writing",          NameJa = "筆記",   Cost = 55, Prereqs = new[] { "pottery" },              DescJa = "図書館を解禁" },
            new TechDef { Id = "wheel",            NameJa = "車輪",   Cost = 55, Prereqs = new[] { "animal_husbandry" },     DescJa = "数学への足がかり" },
            new TechDef { Id = "masonry",          NameJa = "石工術", Cost = 55, Prereqs = new[] { "mining" },               DescJa = "城壁を解禁" },
            new TechDef { Id = "bronze_working",   NameJa = "青銅器", Cost = 55, Prereqs = new[] { "mining" },               DescJa = "槍兵を解禁" },
            new TechDef { Id = "iron_working",     NameJa = "鉄器",   Cost = 85, Prereqs = new[] { "bronze_working" },       DescJa = "剣士を解禁" },
            new TechDef { Id = "mathematics",      NameJa = "数学",   Cost = 85, Prereqs = new[] { "wheel" },                DescJa = "カタパルトを解禁" },
            new TechDef { Id = "construction",     NameJa = "建築学", Cost = 85, Prereqs = new[] { "masonry" },              DescJa = "作業場を解禁" },
        };

        static Dictionary<string, UnitDef> unitsById;
        static Dictionary<string, BuildingDef> buildingsById;
        static Dictionary<string, TechDef> techsById;

        public static UnitDef GetUnit(string id)
        {
            if (unitsById == null) unitsById = Units.ToDictionary(u => u.Id);
            return unitsById[id];
        }

        public static BuildingDef GetBuilding(string id)
        {
            if (buildingsById == null) buildingsById = Buildings.ToDictionary(b => b.Id);
            return buildingsById[id];
        }

        public static TechDef GetTech(string id)
        {
            if (techsById == null) techsById = Techs.ToDictionary(t => t.Id);
            return techsById[id];
        }

        // ---- 計算式 ----

        /// <summary>タイルへの進入コスト。進入不可なら ImpassableCost。</summary>
        public static int MoveCostInto(Tile t)
        {
            if (t == null || !t.IsPassable) return ImpassableCost;
            int cost = Terrains[t.Terrain].MoveCost;
            if (t.HasHill) cost += 1;
            if (t.HasForest) cost += 1;
            return cost < 1 ? 1 : cost;
        }

        /// <summary>タイルの防御ボーナス(乗算前の加算率)。</summary>
        public static float DefenseBonusAt(Tile t)
        {
            if (t == null) return 0f;
            float bonus = Terrains[t.Terrain].DefenseBonus;
            if (t.HasHill) bonus += HillDefenseBonus;
            if (t.HasForest) bonus += ForestDefenseBonus;
            return bonus;
        }

        /// <summary>人口 pop から pop+1 に成長するのに必要な食料。</summary>
        public static int GrowthFoodNeeded(int pop)
        {
            int n = pop - 1;
            return 15 + 8 * n + (int)Mathf.Pow(n, 1.5f);
        }

        /// <summary>戦闘ダメージ。attackStr/defenseStr は補正済みの実効値。</summary>
        public static int CombatDamage(float attackStr, float defenseStr, System.Random rng)
        {
            if (defenseStr < 1f) defenseStr = 1f;
            float ratio = attackStr / defenseStr;
            float roll = 0.8f + (float)rng.NextDouble() * 0.4f;
            int dmg = Mathf.RoundToInt(30f * Mathf.Pow(ratio, 1.35f) * roll);
            return Mathf.Clamp(dmg, 4, 90);
        }

        /// <summary>HPによる強さ低下: 実効強さ = str * (0.5 + 0.5 * hp/max)。</summary>
        public static float HealthScaledStrength(int strength, int hp, int maxHp)
        {
            return strength * (0.5f + 0.5f * hp / (float)maxHp);
        }
    }
}
