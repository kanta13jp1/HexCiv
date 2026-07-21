using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>矩形(odd-rオフセット)ヘクスマップ。</summary>
    public class HexMap
    {
        public readonly int Width;   // 列数
        public readonly int Height;  // 行数

        readonly Tile[] tiles;

        public HexMap(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new Tile[width * height];
            for (int row = 0; row < height; row++)
                for (int col = 0; col < width; col++)
                    tiles[row * width + col] = new Tile { Coord = HexCoord.FromOffset(col, row) };
        }

        public bool InBounds(HexCoord c)
        {
            c.ToOffset(out int col, out int row);
            return col >= 0 && col < Width && row >= 0 && row < Height;
        }

        /// <summary>範囲外なら null。</summary>
        public Tile Get(HexCoord c)
        {
            c.ToOffset(out int col, out int row);
            if (col < 0 || col >= Width || row < 0 || row >= Height) return null;
            return tiles[row * Width + col];
        }

        public IEnumerable<Tile> AllTiles => tiles;

        public IEnumerable<Tile> NeighborsOf(HexCoord c)
        {
            foreach (var n in c.Neighbors())
            {
                var t = Get(n);
                if (t != null) yield return t;
            }
        }

        /// <summary>半径内(自身含む)の存在するタイル。</summary>
        public IEnumerable<Tile> TilesInRange(HexCoord c, int radius)
        {
            foreach (var h in c.Range(radius))
            {
                var t = Get(h);
                if (t != null) yield return t;
            }
        }
    }
}
