using System;

namespace HexCiv.Core
{
    /// <summary>マップの1タイル。ユニットは1タイル1体(1UPT)。</summary>
    public class Tile
    {
        public HexCoord Coord;
        public TerrainType Terrain = TerrainType.Ocean;
        public bool HasHill;
        public bool HasForest;
        public ResourceType Resource = ResourceType.None;

        /// <summary>領有プレイヤーId(-1 = 無所属)</summary>
        public int OwnerPlayerId = -1;
        /// <summary>領有都市Id(-1 = 無所属)</summary>
        public int OwnerCityId = -1;

        /// <summary>このタイルにいるユニット(null可)</summary>
        public Unit Unit;
        /// <summary>このタイルにある都市(null可)</summary>
        public City City;

        public TerrainDef Def => GameRules.Terrains[Terrain];
        public bool IsWater => Terrain == TerrainType.Ocean || Terrain == TerrainType.Coast;
        public bool IsLand => !IsWater;
        public bool IsPassable => IsLand && Def.Passable;
        public int MoveCost => GameRules.MoveCostInto(this);
        public float DefenseBonus => GameRules.DefenseBonusAt(this);

        /// <summary>タイル産出(地形+丘陵+森林+資源。都市タイルは最低 食料2/生産2)。</summary>
        public Yields GetYields()
        {
            var y = Def.BaseYields;
            if (HasHill) y += new Yields(0, 1);
            if (HasForest) y += new Yields(0, 1);
            if (Resource != ResourceType.None) y += GameRules.Resources[Resource].Bonus;
            if (City != null)
            {
                y.Food = Math.Max(y.Food, 2);
                y.Production = Math.Max(y.Production, 2);
            }
            return y;
        }
    }
}
