using System;
using UnityEngine;

namespace HexCiv.Core
{
    /// <summary>固定史実マップと8勢力から既存Core互換のGameStateを構築する。</summary>
    public static class HistoricalCampaignFactory
    {
        public static HistoricalCampaignSession Build(HistoricalCampaignDefinition definition)
        {
            HistoricalCampaignValidator.ThrowIfInvalid(definition);
            int humanIndex = FindHumanIndex(definition);
            var config = new GameConfig
            {
                MapWidth = definition.mapWidth,
                MapHeight = definition.mapHeight,
                NumPlayers = definition.factions.Length,
                Seed = definition.seed,
                MaxTurns = definition.maxTurns,
                HumanPlayerIndex = humanIndex,
                MapType = 0,
                Difficulty = 1,
            };
            var state = new GameState
            {
                Config = config,
                Map = BuildMap(definition),
                Rng = new System.Random(definition.seed),
                TurnNumber = 1,
            };

            for (int i = 0; i < definition.factions.Length; i++)
            {
                var faction = definition.factions[i];
                state.Players.Add(new Player
                {
                    Id = i,
                    CivilizationId = faction.civilizationId ?? "",
                    NameJa = faction.name.ja,
                    RegionJa = "南メソポタミア",
                    EraJa = "ウルク期（紀元前4000～3000年）",
                    LeaderId = faction.id + "_unknown_leader",
                    LeaderNameJa = faction.leaderTitle.ja,
                    LeaderTitleJa = "氏名未詳",
                    Color = ParseColor(faction.colorHex),
                    IsHuman = faction.human,
                });
            }

            for (int i = 0; i < definition.factions.Length; i++)
            {
                var faction = definition.factions[i];
                var player = state.Players[i];
                var coord = HexCoord.FromOffset(faction.startCol, faction.startRow);
                var city = state.FoundCity(player, coord);
                city.NameJa = faction.capitalName.ja;
                city.Population = Math.Max(1, faction.initialPopulation);
                PopulationSystem.InitializeCity(city);
                PopulationSystem.NormalizeLoadedCity(player, city);
                // 既存市場との互換初期値。実在物資8件の在庫接続は次段階で行う。
                player.FoodGoods = MarketSystem.StartingStock + city.Population;
                player.MaterialGoods = MarketSystem.StartingStock + 1;
            }

            Visibility.RecomputeAll(state);
            return new HistoricalCampaignSession(definition, state,
                UrukCampaignSystem.CreateInitialProgress(definition));
        }

        /// <summary>
        /// 既存SaveLoadが文明・指導者台帳から補完する表示情報を、史実キャンペーン定義へ戻す。
        /// 名未詳の指導者を後世の既定指導者へ置き換えないため、ロード直後に呼ぶ。
        /// </summary>
        public static void ApplyDefinitionMetadata(HistoricalCampaignDefinition definition,
            GameState state)
        {
            if (definition == null || state == null)
                throw new ArgumentNullException(definition == null ? nameof(definition) : nameof(state));
            if (state.Players.Count != definition.factions.Length)
                throw new InvalidOperationException("勢力数がキャンペーン定義と一致しない");
            for (int i = 0; i < definition.factions.Length; i++)
            {
                var faction = definition.factions[i];
                var player = state.Players[i];
                player.CivilizationId = faction.civilizationId ?? "";
                player.NameJa = faction.name.ja;
                player.RegionJa = "南メソポタミア";
                player.EraJa = "ウルク期（紀元前4000～3000年）";
                player.LeaderId = faction.id + "_unknown_leader";
                player.LeaderNameJa = faction.leaderTitle.ja;
                player.LeaderTitleJa = "氏名未詳";
                player.Color = ParseColor(faction.colorHex);
                player.IsHuman = faction.human;
            }
        }

        public static HexMap BuildMap(HistoricalCampaignDefinition definition)
        {
            HistoricalCampaignValidator.ThrowIfInvalid(definition);
            var map = new HexMap(definition.mapWidth, definition.mapHeight);
            for (int row = 0; row < definition.mapHeight; row++)
            {
                string line = definition.terrainRows[row];
                for (int col = 0; col < definition.mapWidth; col++)
                    ApplyTerrain(map.Get(HexCoord.FromOffset(col, row)), line[col]);
            }

            foreach (var river in definition.rivers)
            {
                for (int i = 0; i < river.points.Length - 1; i++)
                {
                    var from = HexCoord.FromOffset(river.points[i].col, river.points[i].row);
                    var to = HexCoord.FromOffset(river.points[i + 1].col, river.points[i + 1].row);
                    var tile = map.Get(from);
                    if (tile == null || tile.IsWater) continue;
                    tile.HasRiver = true;
                    tile.HasFloodplain = tile.Terrain == TerrainType.Plains ||
                        tile.Terrain == TerrainType.Grassland ||
                        tile.Terrain == TerrainType.Desert;
                    tile.RiverOutflowDirection = DirectionFromTo(from, to);
                }
            }
            return map;
        }

        static void ApplyTerrain(Tile tile, char symbol)
        {
            tile.HasHill = false;
            tile.HasForest = false;
            tile.HasRiver = false;
            tile.HasFloodplain = false;
            tile.RiverOutflowDirection = -1;
            tile.Resource = ResourceType.None;
            switch (symbol)
            {
                case '~': tile.Terrain = TerrainType.Ocean; break;
                case '=': tile.Terrain = TerrainType.Coast; break;
                case '.': tile.Terrain = TerrainType.Plains; break;
                case 'g': tile.Terrain = TerrainType.Grassland; break;
                case 'd': tile.Terrain = TerrainType.Desert; break;
                case 'h':
                    tile.Terrain = TerrainType.Plains;
                    tile.HasHill = true;
                    break;
                case 'm': tile.Terrain = TerrainType.Mountain; break;
                default: throw new ArgumentOutOfRangeException(nameof(symbol), symbol, "未知の地形記号");
            }
        }

        static int DirectionFromTo(HexCoord from, HexCoord to)
        {
            for (int direction = 0; direction < HexCoord.Directions.Length; direction++)
                if (from.Neighbor(direction) == to) return direction;
            throw new InvalidOperationException($"河川経路が隣接していない: {from} -> {to}");
        }

        static int FindHumanIndex(HistoricalCampaignDefinition definition)
        {
            for (int i = 0; i < definition.factions.Length; i++)
                if (definition.factions[i].human) return i;
            return 0;
        }

        static Color ParseColor(string hex)
        {
            if (!string.IsNullOrWhiteSpace(hex) &&
                ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var color))
                return color;
            return new Color(0.72f, 0.55f, 0.20f);
        }
    }

    /// <summary>定義と実行状態を分離して保持する史実キャンペーンのセッション。</summary>
    public sealed class HistoricalCampaignSession
    {
        public HistoricalCampaignDefinition Definition { get; }
        public GameState State { get; }
        public UrukCampaignProgress Progress { get; }

        public string CampaignId => Definition.id;
        public int CompletedTurns => Math.Max(0, State.TurnNumber - 1);
        public int CurrentYear => HistoricalCampaignCalendar.YearAtTurnStart(
            Definition, State.TurnNumber);
        public string CurrentIntervalJa => HistoricalCampaignCalendar.TurnIntervalJa(
            Definition, Math.Clamp(State.TurnNumber, 1, Definition.maxTurns));

        public HistoricalCampaignSession(HistoricalCampaignDefinition definition, GameState state,
            UrukCampaignProgress progress)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            UrukCampaignSystem.ValidateProgress(Definition, Progress);
        }
    }
}
