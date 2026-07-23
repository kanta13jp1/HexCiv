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
        /// <summary>手続き生成された河道。川は陸上タイルだけに置かれる。</summary>
        public bool HasRiver;
        /// <summary>河川の下流方向（HexCoord.Directions の0～5）。河口は隣接水域を指し、未設定は-1。</summary>
        public int RiverOutflowDirection = -1;
        /// <summary>河川沿いの低平地。肥沃な堆積土を抽象化した追加食料地形。</summary>
        public bool HasFloodplain;
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
            if (HasRiver && IsPassable) y += new Yields(1, 0);
            if (HasFloodplain && IsPassable) y += new Yields(1, 0);
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
