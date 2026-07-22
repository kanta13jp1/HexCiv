using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HexCiv.Core
{
    /// <summary>
    /// 手続き型マップ生成(ARCHITECTURE.md §4)。
    /// 多層ノイズ+外縁フォールオフによる大陸生成(陸地率 約40〜50%)、緯度による気候帯、
    /// 沿岸判定、山岳/丘陵/森林/資源の配置、最大の大陸上での初期位置選定を行う。
    /// 乱数は渡された System.Random のみを使用する(Mathf.PerlinNoise のシードはそのオフセットに用いる)。
    /// 無限ループはせず、必ず NumPlayers 個の初期位置を返す。
    /// マップ種別(config.MapType。2026-07-20 追加): 0=大陸(従来どおり)
    /// 1=パンゲア(低い海面+中心への強い放射状重み付け → 単一の超大陸、陸地率 約44〜50%)
    /// 2=群島(高い海面+高周波ノイズ+弱い外縁減衰 → 多数の島、陸地率 約30〜35%)。
    /// 初期位置の保証(最大陸塊優先・相互距離の段階的緩和)は種別によらず共通。
    /// </summary>
    public static class MapGenerator
    {
        // ---- チューニング定数 ----
        const float MountainRatio = 0.05f;   // 陸地に対する山岳の割合(クラスター配置)
        const float HillRatio = 0.15f;       // 陸地に対する丘陵の割合
        const float ForestRatio = 0.20f;     // 陸地に対する森林の割合(砂漠/雪原には生えない)
        const float ResourceRatio = 0.08f;   // 適地タイルに対する資源の割合
        const int StartMinDistance = 10;     // 初期位置同士の最小距離(満たせない場合は1ずつ緩和)
        const int StartLandRadius = 3;       // 初期位置の周辺陸地を数える半径
        const int StartLandRequired = 8;     // 半径内に必要な通行可能陸地タイル数

        /// <summary>Perlin ノイズ1層。System.Random 由来のオフセットでシードする。</summary>
        struct NoiseLayer
        {
            readonly float ox;
            readonly float oy;

            public NoiseLayer(System.Random rng)
            {
                ox = (float)(rng.NextDouble() * 1024.0 + 64.0);
                oy = (float)(rng.NextDouble() * 1024.0 + 64.0);
            }

            /// <summary>ワールド座標を波長 wavelength でサンプリング。0..1。</summary>
            public float At(Vector3 worldPos, float wavelength)
            {
                return Mathf.Clamp01(Mathf.PerlinNoise(ox + worldPos.x / wavelength, oy + worldPos.z / wavelength));
            }
        }

        public static HexMap Generate(GameConfig config, System.Random rng, out List<HexCoord> startPositions)
        {
            int width = Math.Max(4, config.MapWidth);
            int height = Math.Max(4, config.MapHeight);
            var map = new HexMap(width, height);

            var elevA = new NoiseLayer(rng);
            var elevB = new NoiseLayer(rng);
            var elevC = new NoiseLayer(rng);
            var climateJitter = new NoiseLayer(rng);
            var moisture = new NoiseLayer(rng);
            var grassMix = new NoiseLayer(rng);
            var hillNoise = new NoiseLayer(rng);
            var forestNoise = new NoiseLayer(rng);

            // ---- 1) 標高: 多層ノイズ + 外縁フォールオフ(マップ端は必ず海になる) ----
            // マップ種別(config.MapType。2026-07-20 追加)で生成パラメータを分岐する。
            // 種別0(大陸)は従来と完全に同一の計算・乱数消費(既存シードのマップを変えない)。
            int mapType = Mathf.Clamp(config.MapType, 0, 2);
            float waveScale = mapType == 2 ? 0.55f : 1f;   // 群島: 波長を縮めて島を細かくする
            var elevation = new float[width * height];
            foreach (var tile in map.AllTiles)
            {
                tile.Coord.ToOffset(out int col, out int row);
                Vector3 p = tile.Coord.ToWorld();

                float e = elevA.At(p, 13f * waveScale) + 0.5f * elevB.At(p, 6.5f * waveScale)
                    + 0.25f * elevC.At(p, 3.2f * waveScale);
                e /= 1.75f;

                float ex = Mathf.Min(col, (width - 1) - col) / (width * 0.5f);
                float ey = Mathf.Min(row, (height - 1) - row) / (height * 0.5f);
                float edge = Mathf.Clamp01(Mathf.Min(ex, ey) / 0.35f); // 外縁35%の帯で減衰
                if (mapType == 1)
                {
                    // パンゲア: 中心からの距離による強い放射状の重み付け。最高標高が中央へ
                    // 集中するため、海面決定(分位点)後は自然と単一の超大陸にまとまる
                    float nx = width > 1 ? col / (float)(width - 1) * 2f - 1f : 0f;
                    float ny = height > 1 ? row / (float)(height - 1) * 2f - 1f : 0f;
                    float radial = Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny));   // 0=中心, 1=外縁
                    e = e * Mathf.Lerp(0.2f, 1f, edge) + (1f - radial) * 0.55f - radial * 0.35f;
                }
                else if (mapType == 2)
                {
                    // 群島: 外縁減衰を弱め中心偏重も付けない(高周波ノイズがそのまま島の分布になる)
                    e = e * Mathf.Lerp(0.55f, 1f, edge) - (1f - edge) * 0.10f;
                }
                else
                {
                    e = e * Mathf.Lerp(0.25f, 1f, edge) - (1f - edge) * 0.25f;   // 大陸(従来)
                }

                elevation[row * width + col] = e;
            }

            // ---- 2) 陸地率が種別ごとの目標になるよう分位点で海面高を決定 ----
            //   大陸: 約42〜48%(従来) / パンゲア: 約44〜50%(低い海面) / 群島: 約30〜35%(高い海面)
            float landRoll = (float)rng.NextDouble();
            float landFraction =
                mapType == 1 ? 0.44f + landRoll * 0.06f :
                mapType == 2 ? 0.30f + landRoll * 0.05f :
                0.42f + landRoll * 0.06f;
            float seaLevel = Quantile(elevation, 1f - landFraction);

            // ---- 3) 陸/海の確定と気候帯(緯度バンド+ノイズ揺らぎ) ----
            foreach (var tile in map.AllTiles)
            {
                tile.Coord.ToOffset(out int col, out int row);
                bool border = col == 0 || row == 0 || col == width - 1 || row == height - 1;
                bool isLand = !border && elevation[row * width + col] > seaLevel;
                if (!isLand)
                {
                    tile.Terrain = TerrainType.Ocean;
                    continue;
                }

                Vector3 p = tile.Coord.ToWorld();
                float lat = height > 1 ? Mathf.Abs(row / (float)(height - 1) - 0.5f) * 2f : 0f; // 0=赤道, 1=極
                lat += (climateJitter.At(p, 6f) - 0.5f) * 0.18f;
                tile.Terrain = ClimateTerrain(lat, moisture.At(p, 8f), grassMix.At(p, 5f));
            }

            // ---- 4) 沿岸: 陸に隣接する水タイルは Coast ----
            foreach (var tile in map.AllTiles)
            {
                if (tile.Terrain != TerrainType.Ocean) continue;
                foreach (var n in map.NeighborsOf(tile.Coord))
                {
                    if (n.IsLand) { tile.Terrain = TerrainType.Coast; break; }
                }
            }

            // ---- 4.5) 内陸湖: 周囲を陸に囲まれた局所低地を水面化 ----
            // 乱数を追加消費せず、同一seedの決定論を維持する。外洋とは別連結成分になる。
            NaturalGeographySystem.GenerateInlandLakes(map, elevation);

            var land = map.AllTiles.Where(t => t.IsLand).ToList();
            int landCount = land.Count;

            // ---- 5) 山岳(陸地の約5%、ランダムウォークでクラスター化) ----
            PlaceMountains(map, land, rng);

            // ---- 6) 丘陵(陸地の約15%、ノイズの高い順で選ぶことで塊になる) ----
            var hillCands = land.Where(t => t.Terrain != TerrainType.Mountain).ToList();
            int hillTarget = Mathf.Min(hillCands.Count, Mathf.RoundToInt(landCount * HillRatio));
            foreach (var t in TopByNoise(hillCands, hillNoise, 7f, rng).Take(hillTarget))
                t.HasHill = true;

            // ---- 7) 森林(陸地の約20%、砂漠/雪原/山岳を除く) ----
            var forestCands = land.Where(t =>
                t.Terrain == TerrainType.Grassland ||
                t.Terrain == TerrainType.Plains ||
                t.Terrain == TerrainType.Tundra).ToList();
            int forestTarget = Mathf.Min(forestCands.Count, Mathf.RoundToInt(landCount * ForestRatio));
            foreach (var t in TopByNoise(forestCands, forestNoise, 5.5f, rng).Take(forestTarget))
                t.HasForest = true;

            // ---- 7.5) 河川: 山麓から最寄り水域へ決定論的な河道を生成 ----
            NaturalGeographySystem.GenerateRivers(map);

            // ---- 8) 資源(適地の約8%) ----
            PlaceResources(map, land, rng);

            // ---- 9) 初期位置 ----
            startPositions = PickStartPositions(map, config.NumPlayers, rng);

            return map;
        }

        // ---------------- 気候 ----------------

        /// <summary>緯度(0=赤道,1=極)と湿度/植生ノイズから陸地の地形を決める。</summary>
        static TerrainType ClimateTerrain(float lat, float moist, float grass)
        {
            if (lat > 0.85f) return TerrainType.Snow;
            if (lat > 0.66f) return TerrainType.Tundra;
            if (lat < 0.28f && moist < 0.42f) return TerrainType.Desert; // 赤道付近の乾燥帯
            return grass > 0.52f ? TerrainType.Grassland : TerrainType.Plains;
        }

        // ---------------- 地物配置 ----------------

        static void PlaceMountains(HexMap map, List<Tile> land, System.Random rng)
        {
            if (land.Count == 0) return;
            int target = Mathf.RoundToInt(land.Count * MountainRatio);
            int placed = 0;
            int guard = target * 50 + 50; // 反復上限(無限ループ防止)
            while (placed < target && guard-- > 0)
            {
                var cur = land[rng.Next(land.Count)];
                int clusterSize = 2 + rng.Next(4); // 2..5 タイルの山脈
                for (int i = 0; i < clusterSize && placed < target; i++)
                {
                    if (cur.Terrain != TerrainType.Mountain)
                    {
                        cur.Terrain = TerrainType.Mountain;
                        cur.HasHill = false;
                        cur.HasForest = false;
                        cur.Resource = ResourceType.None;
                        placed++;
                    }

                    // 隣接する陸地(非山岳)からリザーバサンプリングで次のタイルを選ぶ
                    Tile next = null;
                    int seen = 0;
                    foreach (var n in map.NeighborsOf(cur.Coord))
                    {
                        if (!n.IsLand || n.Terrain == TerrainType.Mountain) continue;
                        seen++;
                        if (rng.Next(seen) == 0) next = n;
                    }
                    if (next == null) break;
                    cur = next;
                }
            }
        }

        /// <summary>ノイズ値(+少量の乱数ジッター)の高い順に並べる。塊状の分布になる。</summary>
        static IEnumerable<Tile> TopByNoise(List<Tile> tiles, NoiseLayer noise, float wavelength, System.Random rng)
        {
            return tiles.OrderByDescending(t => noise.At(t.Coord.ToWorld(), wavelength) + (float)rng.NextDouble() * 0.12f);
        }

        static void PlaceResources(HexMap map, List<Tile> land, System.Random rng)
        {
            var suitable = new List<(Tile tile, List<ResourceType> options)>();
            foreach (var t in land)
            {
                if (t.Terrain == TerrainType.Mountain) continue;
                var opts = SuitableResources(map, t);
                if (opts.Count > 0) suitable.Add((t, opts));
            }
            if (suitable.Count == 0) return;

            Shuffle(suitable, rng);
            int target = Mathf.Min(suitable.Count, Mathf.Max(1, Mathf.RoundToInt(suitable.Count * ResourceRatio)));
            for (int i = 0; i < target; i++)
            {
                var (tile, options) = suitable[i];
                tile.Resource = options[rng.Next(options.Count)];
            }
        }

        /// <summary>このタイルに置ける資源の候補。小麦:草原/平原、牛:草原、鹿:ツンドラ/森林、鉄:丘陵/山麓、馬:平原/草原。</summary>
        static List<ResourceType> SuitableResources(HexMap map, Tile t)
        {
            var opts = new List<ResourceType>();
            bool grassOrPlains = t.Terrain == TerrainType.Grassland || t.Terrain == TerrainType.Plains;

            if (grassOrPlains && !t.HasForest) opts.Add(ResourceType.Wheat);
            if (t.Terrain == TerrainType.Grassland && !t.HasForest) opts.Add(ResourceType.Cattle);
            if (t.Terrain == TerrainType.Tundra || t.HasForest) opts.Add(ResourceType.Deer);

            bool nearMountain = false;
            foreach (var n in map.NeighborsOf(t.Coord))
                if (n.Terrain == TerrainType.Mountain) { nearMountain = true; break; }
            if (t.HasHill || nearMountain) opts.Add(ResourceType.Iron);

            if (grassOrPlains && !t.HasForest && !t.HasHill) opts.Add(ResourceType.Horses);
            return opts;
        }

        static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ---------------- 初期位置 ----------------

        /// <summary>
        /// 初期位置を NumPlayers 個選ぶ。最大の大陸上の良質な候補から、相互距離 >= 10 を目指し、
        /// 満たせなければ距離を1ずつ緩和する。どんなマップでも必ず個数分を返す。
        /// </summary>
        static List<HexCoord> PickStartPositions(HexMap map, int numPlayers, System.Random rng)
        {
            var starts = new List<HexCoord>();
            if (numPlayers <= 0) return starts;

            // 候補: 最大の大陸の良質タイル → 全大陸の良質タイル → 全ての通行可能陸地 → 非常用に陸地を生成
            var candidates = LargestLandmass(map).Where(t => IsDecentStart(map, t)).ToList();
            if (candidates.Count < numPlayers)
                candidates = map.AllTiles.Where(t => IsDecentStart(map, t)).ToList();
            if (candidates.Count < numPlayers)
                candidates = map.AllTiles.Where(t => t.IsPassable).ToList();
            if (candidates.Count < numPlayers)
                candidates = ForceEmergencyLand(map, numPlayers);

            var scored = candidates
                .Distinct()
                .OrderByDescending(t => StartScore(map, t))
                .ToList();

            for (int minDist = StartMinDistance; minDist >= 1; minDist--)
            {
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    // 最後の試行はスキップなしの決定的貪欲法(minDist=1 では候補数さえあれば必ず成功する)
                    bool deterministic = attempt == 5;
                    var chosen = new List<HexCoord>();
                    foreach (var t in scored)
                    {
                        if (!deterministic && rng.NextDouble() < 0.25) continue;
                        bool ok = true;
                        for (int i = 0; i < chosen.Count; i++)
                            if (chosen[i].DistanceTo(t.Coord) < minDist) { ok = false; break; }
                        if (!ok) continue;
                        chosen.Add(t.Coord);
                        if (chosen.Count == numPlayers) return chosen;
                    }
                }
            }

            // 最終フォールバック: 重複なしで埋め、それでも足りなければ複製で個数を保証
            foreach (var t in scored)
            {
                if (!starts.Contains(t.Coord)) starts.Add(t.Coord);
                if (starts.Count == numPlayers) return starts;
            }
            var center = HexCoord.FromOffset(map.Width / 2, map.Height / 2);
            while (starts.Count < numPlayers)
                starts.Add(starts.Count > 0 ? starts[starts.Count - 1] : center);
            return starts;
        }

        /// <summary>通行可能陸地の連結成分をフラッドフィルし、最大のものを返す。</summary>
        static List<Tile> LargestLandmass(HexMap map)
        {
            var visited = new HashSet<HexCoord>();
            List<Tile> best = null;
            foreach (var t in map.AllTiles)
            {
                if (!t.IsPassable || !visited.Add(t.Coord)) continue;
                var component = new List<Tile>();
                var stack = new Stack<Tile>();
                stack.Push(t);
                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    component.Add(cur);
                    foreach (var n in map.NeighborsOf(cur.Coord))
                        if (n.IsPassable && visited.Add(n.Coord))
                            stack.Push(n);
                }
                if (best == null || component.Count > best.Count) best = component;
            }
            return best ?? new List<Tile>();
        }

        /// <summary>初期位置としての適性: 通行可能で、隣に通行可能タイルがあり、半径3内に通行可能陸地が8以上。</summary>
        static bool IsDecentStart(HexMap map, Tile t)
        {
            if (!t.IsPassable) return false;
            bool neighborOk = false;
            foreach (var n in map.NeighborsOf(t.Coord))
                if (n.IsPassable) { neighborOk = true; break; }
            if (!neighborOk) return false; // 開拓者の隣に戦士を置けること

            int landNearby = 0;
            foreach (var n in map.TilesInRange(t.Coord, StartLandRadius))
                if (n.IsPassable) landNearby++;
            return landNearby >= StartLandRequired;
        }

        /// <summary>周辺(半径2)の産出量に基づく初期位置スコア。食料をやや重視する。</summary>
        static int StartScore(HexMap map, Tile t)
        {
            int score = 0;
            foreach (var n in map.TilesInRange(t.Coord, 2))
            {
                var y = n.GetYields();
                score += y.Food * 3 + y.Production * 2 + y.Science;
                if (n.Resource != ResourceType.None) score += 2;
            }
            var self = t.GetYields();
            score += self.Food * 2 + self.Production;
            return score;
        }

        /// <summary>非常用: マップがほぼ全て水/山でも初期位置を保証するため、中央付近に草原を作る。</summary>
        static List<Tile> ForceEmergencyLand(HexMap map, int numPlayers)
        {
            var result = new List<Tile>();
            int needed = Mathf.Max(numPlayers * 4, 4);
            for (int row = map.Height / 2; row >= 0 && result.Count < needed; row--)
            {
                for (int col = 1; col < map.Width - 1 && result.Count < needed; col += 2)
                {
                    var t = map.Get(HexCoord.FromOffset(col, row));
                    if (t == null) continue;
                    t.Terrain = TerrainType.Grassland;
                    t.HasHill = false;
                    t.HasForest = false;
                    t.HasRiver = false;
                    t.Resource = ResourceType.None;
                    result.Add(t);
                }
            }
            return result;
        }

        // ---------------- ユーティリティ ----------------

        /// <summary>値配列の q 分位点(0..1)を返す。</summary>
        static float Quantile(float[] values, float q)
        {
            if (values.Length == 0) return 0f;
            var sorted = (float[])values.Clone();
            Array.Sort(sorted);
            int idx = Mathf.Clamp(Mathf.FloorToInt(sorted.Length * q), 0, sorted.Length - 1);
            return sorted[idx];
        }
    }
}
