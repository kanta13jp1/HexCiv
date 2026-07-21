using UnityEngine;

namespace HexCiv.Core
{
    public class TerrainDef
    {
        public TerrainType Type;
        public string NameJa;
        public Yields BaseYields;
        public int MoveCost = 1;
        public bool Passable = true;
        public float DefenseBonus;
        public Color Color;
    }

    public class ResourceDef
    {
        public ResourceType Type;
        public string NameJa;
        public Yields Bonus;
        public Color Color;
    }

    public class UnitDef
    {
        public string Id;
        public string NameJa;
        /// <summary>1文字の表示グリフ(マップ上のユニット記号)</summary>
        public string Glyph;
        public int Cost;
        public int Moves;
        public int Strength;
        public int RangedStrength;
        public int Range;
        public bool IsCivilian;
        /// <summary>必要技術のId。null なら最初から生産可能。</summary>
        public string RequiresTech;
        public int Sight = 2;

        public bool IsRanged => RangedStrength > 0;
    }

    public class BuildingDef
    {
        public string Id;
        public string NameJa;
        public int Cost;
        public string RequiresTech;
        public Yields Bonus;
        public int CityDefense;
        public string DescJa;
    }

    public class TechDef
    {
        public string Id;
        public string NameJa;
        public int Cost;
        public string[] Prereqs;
        public string DescJa;
    }

    /// <summary>都市の生産項目(ユニットまたは建物)。</summary>
    public class ProductionItem
    {
        public ProductionKind Kind;
        public string Id;
        public string NameJa;
        public int Cost;

        public static ProductionItem FromUnit(UnitDef d)
            => new ProductionItem { Kind = ProductionKind.Unit, Id = d.Id, NameJa = d.NameJa, Cost = d.Cost };

        public static ProductionItem FromBuilding(BuildingDef d)
            => new ProductionItem { Kind = ProductionKind.Building, Id = d.Id, NameJa = d.NameJa, Cost = d.Cost };
    }
}
