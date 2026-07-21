using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexCiv.Core
{
    /// <summary>
    /// Axial hex coordinate (pointy-top orientation). Storage layout is odd-r offset.
    /// World mapping: x = sqrt(3)*size*(q + r/2), z = 1.5*size*r, y = 0 (flat map on XZ plane).
    /// </summary>
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        public int q;
        public int r;

        public int S => -q - r;

        public const float Sqrt3 = 1.7320508f;

        public HexCoord(int q, int r) { this.q = q; this.r = r; }

        public static readonly HexCoord[] Directions =
        {
            new HexCoord(1, 0), new HexCoord(1, -1), new HexCoord(0, -1),
            new HexCoord(-1, 0), new HexCoord(-1, 1), new HexCoord(0, 1)
        };

        public static HexCoord operator +(HexCoord a, HexCoord b) => new HexCoord(a.q + b.q, a.r + b.r);
        public static HexCoord operator -(HexCoord a, HexCoord b) => new HexCoord(a.q - b.q, a.r - b.r);
        public static HexCoord operator *(HexCoord a, int k) => new HexCoord(a.q * k, a.r * k);
        public static bool operator ==(HexCoord a, HexCoord b) => a.q == b.q && a.r == b.r;
        public static bool operator !=(HexCoord a, HexCoord b) => !(a == b);

        public HexCoord Neighbor(int dir) => this + Directions[((dir % 6) + 6) % 6];

        public IEnumerable<HexCoord> Neighbors()
        {
            for (int i = 0; i < 6; i++) yield return this + Directions[i];
        }

        public int DistanceTo(HexCoord other)
        {
            return (Math.Abs(q - other.q) + Math.Abs(r - other.r) + Math.Abs(S - other.S)) / 2;
        }

        /// <summary>All coords within radius (includes this coord itself).</summary>
        public IEnumerable<HexCoord> Range(int radius)
        {
            for (int dq = -radius; dq <= radius; dq++)
                for (int dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                    yield return new HexCoord(q + dq, r + dr);
        }

        /// <summary>Coords exactly at the given radius.</summary>
        public IEnumerable<HexCoord> Ring(int radius)
        {
            if (radius <= 0) { yield return this; yield break; }
            var hex = this + Directions[4] * radius;
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < radius; j++)
                {
                    yield return hex;
                    hex = hex.Neighbor(i);
                }
        }

        public Vector3 ToWorld(float size = 1f)
        {
            return new Vector3(Sqrt3 * size * (q + r * 0.5f), 0f, 1.5f * size * r);
        }

        public static HexCoord FromWorld(Vector3 pos, float size = 1f)
        {
            float qf = (Sqrt3 / 3f * pos.x - 1f / 3f * pos.z) / size;
            float rf = (2f / 3f * pos.z) / size;
            return RoundAxial(qf, rf);
        }

        public static HexCoord RoundAxial(float qf, float rf)
        {
            float sf = -qf - rf;
            int qi = Mathf.RoundToInt(qf);
            int ri = Mathf.RoundToInt(rf);
            int si = Mathf.RoundToInt(sf);
            float dq = Mathf.Abs(qi - qf);
            float dr = Mathf.Abs(ri - rf);
            float ds = Mathf.Abs(si - sf);
            if (dq > dr && dq > ds) qi = -ri - si;
            else if (dr > ds) ri = -qi - si;
            return new HexCoord(qi, ri);
        }

        /// <summary>odd-r offset -> axial</summary>
        public static HexCoord FromOffset(int col, int row)
        {
            return new HexCoord(col - (row - (row & 1)) / 2, row);
        }

        /// <summary>axial -> odd-r offset</summary>
        public void ToOffset(out int col, out int row)
        {
            row = r;
            col = q + (r - (r & 1)) / 2;
        }

        public bool Equals(HexCoord other) => q == other.q && r == other.r;
        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
        public override int GetHashCode() => q * 397 ^ r;
        public override string ToString() => $"({q},{r})";
    }
}
