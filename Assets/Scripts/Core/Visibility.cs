namespace HexCiv.Core
{
    /// <summary>
    /// 視界計算。Visible = ユニット視界(丘陵で+1)と都市視界の合併。Explored には累積する。
    /// </summary>
    public static class Visibility
    {
        public static void Recompute(GameState s, Player p)
        {
            if (s == null || p == null) return;
            p.Visible.Clear();
            if (p.IsEliminated) return;

            for (int i = 0; i < p.Units.Count; i++)
            {
                var u = p.Units[i];
                if (u.IsDead) continue;
                var tile = s.Map.Get(u.Coord);
                int sight = u.Def.Sight;
                if (tile != null && tile.HasHill) sight += 1;
                foreach (var c in u.Coord.Range(sight))
                    if (s.Map.InBounds(c)) p.Visible.Add(c);
            }

            for (int i = 0; i < p.Cities.Count; i++)
            {
                var city = p.Cities[i];
                foreach (var c in city.Coord.Range(GameRules.CitySight))
                    if (s.Map.InBounds(c)) p.Visible.Add(c);
            }

            p.Explored.UnionWith(p.Visible);
        }

        public static void RecomputeAll(GameState s)
        {
            if (s == null) return;
            for (int i = 0; i < s.Players.Count; i++)
                Recompute(s, s.Players[i]);
        }
    }
}
