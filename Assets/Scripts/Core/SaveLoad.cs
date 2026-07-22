using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexCiv.Core
{
    // ======================================================================
    // セーブデータDTO(JsonUtility互換:publicフィールドのみ、Dictionary/HashSet不使用)。
    // HashSet由来のリストは決定的にソートして格納するため、
    // Serialize(Deserialize(json)) == json が成立する(スモークテストで検証)。
    // ======================================================================

    /// <summary>セーブファイル全体(バージョン12)。</summary>
    [Serializable]
    public class SaveData
    {
        public int version = SaveLoad.CurrentVersion;

        // ---- スロット一覧表示用メタデータ(2026-07-20 追加、バージョン2) ----
        // metaTurn / metaCivNameJa は状態から導出される(往復一致はこの導出で保たれる)。
        // savedAtIso のみ導出不能のため GameState.LastSavedAtIso(partial)として往復する。
        // バージョン1のセーブではこれらは既定値("" / 0)のままで、ロード側は依存しない。
        public int metaTurn;
        public string metaCivNameJa = "";
        public string savedAtIso = "";

        // ---- GameConfig ----
        public int mapWidth;
        public int mapHeight;
        public int numPlayers;
        public int seed;
        public int maxTurns;
        public int humanPlayerIndex;
        /// <summary>マップ種別(バージョン5追加。0=大陸/1=パンゲア/2=群島)。旧セーブは既定値0(大陸)。</summary>
        public int mapType;
        /// <summary>難易度(バージョン6追加。0=やさしい/1=普通/2=むずかしい)。
        /// 旧セーブはこのフィールドを持たず、JsonUtility が既定コンストラクタ値を保つため
        /// フィールド初期化子 1(普通=補正なし)がそのまま使われる。</summary>
        public int difficulty = 1;

        // ---- GameState ----
        public int turnNumber;
        public bool isGameOver;
        public string gameOverMessageJa = "";
        public int winnerPlayerId = -1;
        public int nextUnitId;
        public int nextCityId;

        // ---- タイル(行優先: index = row * mapWidth + col) ----
        public int[] terrain;
        public bool[] hasHill;
        public bool[] hasForest;
        public int[] resource;
        public int[] ownerPlayerId;
        public int[] ownerCityId;

        public List<PlayerDto> players = new List<PlayerDto>();
        public List<CityDto> cities = new List<CityDto>();
        public List<UnitDto> units = new List<UnitDto>();
        public List<HeritageSiteDto> heritageSites = new List<HeritageSiteDto>();
    }

    /// <summary>Player の直列化形。Visible は保存せずロード時に再計算する。</summary>
    [Serializable]
    public class PlayerDto
    {
        public int id;
        public string civilizationId = "";
        public string leaderId = "";
        public string nameJa = "";
        public float colorR;
        public float colorG;
        public float colorB;
        public bool isHuman;
        public bool isEliminated;
        public List<string> knownTechs = new List<string>();   // 序数順ソート
        public string currentResearchId = "";                  // "" = 未選択
        public int scienceStored;
        // ---- 文化進行(バージョン7追加) ----
        public List<string> knownCulturePolicies = new List<string>(); // 序数順ソート
        public string currentCulturePolicyId = "";                    // "" = 未選択
        public int cultureStored;
        public int totalCulture;
        // CulturalInfluence は Dictionary 非対応のため、相手Id昇順の並行配列で保存する。
        public List<int> cultureInfluencePlayerIds = new List<int>();
        public List<int> cultureInfluenceValues = new List<int>();
        // ---- 遺産・偉人進行(バージョン8追加) ----
        public int greatPersonPoints;
        public int totalGreatPersonPoints;
        public List<string> discoveredHeritageSites = new List<string>(); // 序数順ソート
        public List<string> recruitedGreatPeople = new List<string>();    // 序数順ソート
        // ---- 作品収蔵(バージョン9追加) ----
        public int masterpiecePoints;
        public int totalMasterpiecePoints;
        public List<string> collectedMasterpieces = new List<string>();  // 序数順ソート
        // ---- 国家運営(バージョン10追加) ----
        // 初期化子はバージョン9以前のセーブを読む際の互換既定値になる。
        public int treasury = AdministrationSystem.StartingTreasury;
        public int taxPolicy = (int)TaxPolicy.Balanced;
        public int stability = AdministrationSystem.StartingStability;
        public int warWeariness;
        public int lastRevenue;
        public int lastExpenses;
        public int capitalCityId = -1;
        // ---- 人口社会（バージョン12追加） ----
        public int socialFocus = (int)SocialFocus.Balanced;
        public List<int> atWarWith = new List<int>();          // 昇順ソート
        // ---- 開戦ターン(バージョン4追加。JsonUtility は Dictionary 非対応のため並行配列) ----
        // warStartEnemyIds は昇順ソートし、warStartTurns[i] が対応する開戦ターン(決定的)。
        public List<int> warStartEnemyIds = new List<int>();
        public List<int> warStartTurns = new List<int>();
        public List<HexCoord> explored = new List<HexCoord>(); // r→q 順ソート
    }

    /// <summary>City の直列化形。生産中項目は種別+Idで保存し、定義から復元する。</summary>
    [Serializable]
    public class CityDto
    {
        public int id;
        public int playerId;
        public string nameJa = "";
        public HexCoord coord;
        public int population;
        public int foodStored;
        public int productionStored;
        // ---- 人口階層・需要（バージョン12追加） ----
        public int farmers;
        public int artisans;
        public int scholars;
        public int education = PopulationSystem.StartingEducation;
        public int satisfaction = PopulationSystem.StartingSatisfaction;
        public int foodNeedFulfillment = 100;
        public int housingNeedFulfillment = 100;
        public int serviceNeedFulfillment = 50;
        public int lastNetMigration;
        public int hp;
        public int maxHp;
        public List<string> buildings = new List<string>();    // 建設順を保持
        public int prodKind = -1;                              // -1 = 生産未選択
        public string prodId = "";
    }

    /// <summary>Unit の直列化形。gotoPath は空リスト = 経路なし(null)。</summary>
    [Serializable]
    public class UnitDto
    {
        public int id;
        public int playerId;
        public string defId = "";
        public HexCoord coord;
        public int hp;
        public int movesLeft;
        public bool fortified;
        public bool actedThisTurn;
        // ---- 補給・兵站(バージョン11追加) ----
        // 初期化子0は旧セーブを補給良好として安全に移行する。
        public int supplyLevel = (int)SupplyLevel.Supplied;
        public int turnsOutOfSupply;
        public List<HexCoord> gotoPath = new List<HexCoord>();
    }

    /// <summary>マップ上の遺産配置と発見者。座標順で保存する。</summary>
    [Serializable]
    public class HeritageSiteDto
    {
        public string siteId = "";
        public HexCoord coord;
        public int discoveredByPlayerId = -1;
        public int discoveredTurn;
    }

    /// <summary>
    /// セーブ/ロード(JSON直列化)。Core 純度規約に従い MonoBehaviour・GameObject には触れず、
    /// UnityEngine は JsonUtility と Color のみ使用する。ヘッドレス(エディタバッチ)でも動く。
    ///
    /// 注意: Rng の内部状態は保存しない。ロード時に Seed と TurnNumber から再シードするため、
    /// セーブ/ロードをまたいだ乱数列の決定論(同一プレイの完全再現)は保証されない。
    /// 失敗時は例外を呼び出し側へ伝播させる(Core からはログ出力しない)。
    /// </summary>
    public static class SaveLoad
    {
        // 1: 初版 / 2: スロット表示用メタデータ追加 / 3: 指導者ID追加 / 4: 開戦ターン(和平)追加 /
        // 5: マップ種別(MapType)追加 / 6: 難易度(Difficulty)追加 / 7: 文化進行・政策・影響力追加 / 
        // 8: 遺産配置・発見と偉人ポイント・登用追加 / 9: 作品ポイント・収蔵追加 /
        // 10: 国庫・税制・安定度・戦争疲弊追加 / 11: ユニット補給状態・孤立ターン追加 /
        // 12: 社会重点・人口階層・需要・教育・満足度・移住追加。
        // 旧バージョンのセーブも FromData で読み込める(欠落フィールドは既定値になる)。
        public const int CurrentVersion = 12;

        // ================= 公開API =================

        /// <summary>状態をJSON文字列に直列化する(決定的:同一状態からは常に同一文字列)。</summary>
        public static string Serialize(GameState s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            return JsonUtility.ToJson(ToData(s));
        }

        /// <summary>JSON文字列から状態を完全再構築する。不正なデータは例外を投げる。</summary>
        public static GameState Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("セーブデータが空", nameof(json));
            var data = JsonUtility.FromJson<SaveData>(json);
            return FromData(data);
        }

        /// <summary>
        /// 状態をUTF-8(BOMなし)でファイルへ保存する。失敗時は例外を伝播。
        /// 保存時刻を GameState.LastSavedAtIso に記録してから直列化する
        /// (スロット一覧のメタデータ表示用。往復一致はフィールド経由で保たれる)。
        /// </summary>
        public static void SaveToFile(GameState s, string path)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            s.LastSavedAtIso = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture);
            System.IO.File.WriteAllText(path, Serialize(s), new System.Text.UTF8Encoding(false));
        }

        /// <summary>
        /// スロット一覧表示用にセーブファイルのメタデータだけを読む(2026-07-20 追加)。
        /// ファイルが無い・壊れている場合は false(例外は投げない)。
        /// バージョン1のセーブは metaTurn/metaCivNameJa を持たないため、
        /// turnNumber と人間プレイヤーの文明名から補完する。
        /// </summary>
        public static bool TryReadMeta(string path, out int turn, out string civNameJa, out string savedAtIso)
        {
            turn = 0;
            civNameJa = "";
            savedAtIso = "";
            try
            {
                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return false;
                var data = JsonUtility.FromJson<SaveData>(System.IO.File.ReadAllText(path));
                if (data == null || data.version < 1 || data.players == null || data.players.Count == 0)
                    return false;

                turn = data.turnNumber;
                civNameJa = data.metaCivNameJa ?? "";
                if (civNameJa.Length == 0)
                {
                    for (int i = 0; i < data.players.Count; i++)
                    {
                        var pd = data.players[i];
                        if (pd != null && pd.isHuman) { civNameJa = pd.nameJa ?? ""; break; }
                    }
                }
                savedAtIso = data.savedAtIso ?? "";
                return true;
            }
            catch
            {
                turn = 0;
                civNameJa = "";
                savedAtIso = "";
                return false;
            }
        }

        /// <summary>
        /// ファイルから状態を読み込む。ファイルが無ければ null。
        /// 破損データ等の例外は呼び出し側へ伝播する(呼び出し側で捕捉して処理する)。
        /// </summary>
        public static GameState LoadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return null;
            return Deserialize(System.IO.File.ReadAllText(path));
        }

        // ================= 直列化(GameState → DTO) =================

        static SaveData ToData(GameState s)
        {
            var human = s.HumanPlayer;
            var d = new SaveData
            {
                version = CurrentVersion,
                metaTurn = s.TurnNumber,
                metaCivNameJa = human != null ? (human.NameJa ?? "") : "",
                savedAtIso = s.LastSavedAtIso ?? "",
                mapWidth = s.Map.Width,
                mapHeight = s.Map.Height,
                numPlayers = s.Config != null ? s.Config.NumPlayers : s.Players.Count,
                seed = s.Config != null ? s.Config.Seed : 0,
                maxTurns = s.Config != null ? s.Config.MaxTurns : 250,
                humanPlayerIndex = s.Config != null ? s.Config.HumanPlayerIndex : -1,
                mapType = s.Config != null ? s.Config.MapType : 0,
                difficulty = s.Config != null ? s.Config.Difficulty : 1,
                turnNumber = s.TurnNumber,
                isGameOver = s.IsGameOver,
                gameOverMessageJa = s.GameOverMessageJa ?? "",
                winnerPlayerId = s.Winner != null ? s.Winner.Id : -1,
                nextUnitId = s.PeekNextUnitId(),
                nextCityId = s.PeekNextCityId(),
            };

            // ---- タイル(行優先) ----
            int n = s.Map.Width * s.Map.Height;
            d.terrain = new int[n];
            d.hasHill = new bool[n];
            d.hasForest = new bool[n];
            d.resource = new int[n];
            d.ownerPlayerId = new int[n];
            d.ownerCityId = new int[n];
            int idx = 0;
            for (int row = 0; row < s.Map.Height; row++)
            {
                for (int col = 0; col < s.Map.Width; col++)
                {
                    var t = s.Map.Get(HexCoord.FromOffset(col, row));
                    d.terrain[idx] = (int)t.Terrain;
                    d.hasHill[idx] = t.HasHill;
                    d.hasForest[idx] = t.HasForest;
                    d.resource[idx] = (int)t.Resource;
                    d.ownerPlayerId[idx] = t.OwnerPlayerId;
                    d.ownerCityId[idx] = t.OwnerCityId;
                    idx++;
                }
            }

            // ---- プレイヤー/都市/ユニット(Players リスト順で決定的) ----
            for (int i = 0; i < s.Players.Count; i++)
            {
                var p = s.Players[i];
                var pd = new PlayerDto
                {
                    id = p.Id,
                    civilizationId = p.CivilizationId ?? "",
                    leaderId = p.LeaderId ?? "",
                    nameJa = p.NameJa ?? "",
                    colorR = p.Color.r,
                    colorG = p.Color.g,
                    colorB = p.Color.b,
                    isHuman = p.IsHuman,
                    isEliminated = p.IsEliminated,
                    currentResearchId = p.CurrentResearchId ?? "",
                    scienceStored = p.ScienceStored,
                    currentCulturePolicyId = p.CurrentCulturePolicyId ?? "",
                    cultureStored = p.CultureStored,
                    totalCulture = p.TotalCulture,
                    greatPersonPoints = p.GreatPersonPoints,
                    totalGreatPersonPoints = p.TotalGreatPersonPoints,
                    masterpiecePoints = p.MasterpiecePoints,
                    totalMasterpiecePoints = p.TotalMasterpiecePoints,
                    treasury = p.Treasury,
                    taxPolicy = (int)AdministrationSystem.NormalizePolicy(p.Taxation),
                    stability = p.Stability,
                    warWeariness = p.WarWeariness,
                    lastRevenue = p.LastRevenue,
                    lastExpenses = p.LastExpenses,
                    capitalCityId = p.CapitalCityId,
                    socialFocus = (int)PopulationSystem.NormalizeFocus(p.SocialFocus),
                    knownTechs = new List<string>(p.KnownTechs),
                    knownCulturePolicies = new List<string>(p.KnownCulturePolicies),
                    discoveredHeritageSites = new List<string>(p.DiscoveredHeritageSites),
                    recruitedGreatPeople = new List<string>(p.RecruitedGreatPeople),
                    collectedMasterpieces = new List<string>(p.CollectedMasterpieces),
                    atWarWith = new List<int>(p.AtWarWith),
                    explored = new List<HexCoord>(p.Explored),
                };
                pd.knownTechs.Sort(StringComparer.Ordinal);
                pd.knownCulturePolicies.Sort(StringComparer.Ordinal);
                pd.discoveredHeritageSites.Sort(StringComparer.Ordinal);
                pd.recruitedGreatPeople.Sort(StringComparer.Ordinal);
                pd.collectedMasterpieces.Sort(StringComparer.Ordinal);
                pd.atWarWith.Sort();
                pd.explored.Sort(CompareCoord);

                // 開戦ターン(敵Id昇順の並行配列として決定的に格納)
                var warKeys = new List<int>(p.WarStartTurns.Keys);
                warKeys.Sort();
                for (int k = 0; k < warKeys.Count; k++)
                {
                    pd.warStartEnemyIds.Add(warKeys[k]);
                    pd.warStartTurns.Add(p.WarStartTurns[warKeys[k]]);
                }

                // 文化的影響力(相手Id昇順の並行配列)
                var influenceKeys = new List<int>(p.CulturalInfluence.Keys);
                influenceKeys.Sort();
                for (int k = 0; k < influenceKeys.Count; k++)
                {
                    pd.cultureInfluencePlayerIds.Add(influenceKeys[k]);
                    pd.cultureInfluenceValues.Add(p.CulturalInfluence[influenceKeys[k]]);
                }
                d.players.Add(pd);

                for (int j = 0; j < p.Cities.Count; j++)
                {
                    var c = p.Cities[j];
                    d.cities.Add(new CityDto
                    {
                        id = c.Id,
                        playerId = c.PlayerId,
                        nameJa = c.NameJa ?? "",
                        coord = c.Coord,
                        population = c.Population,
                        foodStored = c.FoodStored,
                        productionStored = c.ProductionStored,
                        farmers = c.Farmers,
                        artisans = c.Artisans,
                        scholars = c.Scholars,
                        education = c.Education,
                        satisfaction = c.Satisfaction,
                        foodNeedFulfillment = c.FoodNeedFulfillment,
                        housingNeedFulfillment = c.HousingNeedFulfillment,
                        serviceNeedFulfillment = c.ServiceNeedFulfillment,
                        lastNetMigration = c.LastNetMigration,
                        hp = c.Hp,
                        maxHp = c.MaxHp,
                        buildings = new List<string>(c.Buildings),
                        prodKind = c.CurrentProduction != null ? (int)c.CurrentProduction.Kind : -1,
                        prodId = c.CurrentProduction != null ? c.CurrentProduction.Id : "",
                    });
                }

                for (int j = 0; j < p.Units.Count; j++)
                {
                    var u = p.Units[j];
                    if (u.IsDead) continue;
                    d.units.Add(new UnitDto
                    {
                        id = u.Id,
                        playerId = u.PlayerId,
                        defId = u.DefId ?? "",
                        coord = u.Coord,
                        hp = u.Hp,
                        movesLeft = u.MovesLeft,
                        fortified = u.Fortified,
                        actedThisTurn = u.ActedThisTurn,
                        supplyLevel = (int)LogisticsSystem.LevelFromSaveValue((int)u.Supply),
                        turnsOutOfSupply = Math.Max(0, u.TurnsOutOfSupply),
                        gotoPath = u.GotoPath != null ? new List<HexCoord>(u.GotoPath) : new List<HexCoord>(),
                    });
                }
            }

            // ---- 遺産配置(座標r→q、同座標ならId順) ----
            var heritage = new List<HeritageSiteInstance>(s.HeritageSites);
            heritage.Sort((a, b) =>
            {
                int cmp = CompareCoord(a.Coord, b.Coord);
                return cmp != 0 ? cmp : string.CompareOrdinal(a.SiteId, b.SiteId);
            });
            for (int i = 0; i < heritage.Count; i++)
            {
                var site = heritage[i];
                d.heritageSites.Add(new HeritageSiteDto
                {
                    siteId = site.SiteId ?? "",
                    coord = site.Coord,
                    discoveredByPlayerId = site.DiscoveredByPlayerId,
                    discoveredTurn = site.DiscoveredTurn,
                });
            }

            return d;
        }

        // ================= 復元(DTO → GameState) =================

        static GameState FromData(SaveData d)
        {
            if (d == null) throw new InvalidOperationException("セーブデータを解析できない");
            // 旧バージョン(1)も読み込む(欠落フィールドは JsonUtility が既定値で埋める)。
            // 未来のバージョンのみ拒否する。
            if (d.version < 1 || d.version > CurrentVersion)
                throw new InvalidOperationException($"未対応のセーブバージョン: {d.version}");
            if (d.mapWidth <= 0 || d.mapHeight <= 0)
                throw new InvalidOperationException("マップサイズが不正");
            int n = d.mapWidth * d.mapHeight;
            if (d.terrain == null || d.terrain.Length != n
                || d.hasHill == null || d.hasHill.Length != n
                || d.hasForest == null || d.hasForest.Length != n
                || d.resource == null || d.resource.Length != n
                || d.ownerPlayerId == null || d.ownerPlayerId.Length != n
                || d.ownerCityId == null || d.ownerCityId.Length != n)
                throw new InvalidOperationException("タイル配列の長さが不正");
            if (d.players == null || d.players.Count == 0)
                throw new InvalidOperationException("プレイヤーデータがない");

            var config = new GameConfig
            {
                MapWidth = d.mapWidth,
                MapHeight = d.mapHeight,
                NumPlayers = d.numPlayers,
                Seed = d.seed,
                MaxTurns = d.maxTurns,
                HumanPlayerIndex = d.humanPlayerIndex,
                // バージョン4以前のセーブは mapType を持たない → JsonUtility が 0(大陸)で埋める
                MapType = d.mapType,
                // バージョン5以前のセーブは difficulty を持たない → フィールド初期化子 1(普通)が残る。
                // ここでは丸めず素通しする(Serialize(Deserialize(json))==json の不変条件を守る。
                // 範囲外の値は DifficultyRules 側が使用時に 0..2 へ丸めるため無害)
                Difficulty = d.difficulty,
            };

            var s = new GameState
            {
                Config = config,
                TurnNumber = d.turnNumber,
                IsGameOver = d.isGameOver,
                GameOverMessageJa = NullIfEmpty(d.gameOverMessageJa),
                // Rng の内部状態は保存していないため、Seed と TurnNumber から再シードする。
                // セーブ/ロードをまたいだ乱数の決定論は保証されない(仕様)。
                Rng = new System.Random(unchecked(d.seed + d.turnNumber * 7919)),
                // 導出不能な保存時刻はフィールドとして往復する(バージョン1では "" → null)
                LastSavedAtIso = NullIfEmpty(d.savedAtIso),
            };

            // ---- マップ ----
            s.Map = new HexMap(d.mapWidth, d.mapHeight);
            int idx = 0;
            for (int row = 0; row < d.mapHeight; row++)
            {
                for (int col = 0; col < d.mapWidth; col++)
                {
                    var t = s.Map.Get(HexCoord.FromOffset(col, row));
                    t.Terrain = (TerrainType)d.terrain[idx];
                    t.HasHill = d.hasHill[idx];
                    t.HasForest = d.hasForest[idx];
                    t.Resource = (ResourceType)d.resource[idx];
                    t.OwnerPlayerId = d.ownerPlayerId[idx];
                    t.OwnerCityId = d.ownerCityId[idx];
                    idx++;
                }
            }

            // ---- プレイヤー ----
            for (int i = 0; i < d.players.Count; i++)
            {
                var pd = d.players[i];
                var civilization = CivilizationCatalog.Find(pd.civilizationId) ??
                    CivilizationCatalog.FindByName(pd.nameJa);
                var leader = LeaderCatalog.Find(pd.leaderId);
                if (leader == null || civilization == null ||
                    !LeaderCatalog.BelongsTo(leader.Id, civilization.Id))
                    leader = civilization != null
                        ? LeaderCatalog.DefaultForCivilization(civilization.Id)
                        : null;
                var p = new Player
                {
                    Id = pd.id,
                    CivilizationId = civilization != null ? civilization.Id : NullIfEmpty(pd.civilizationId),
                    NameJa = pd.nameJa,
                    RegionJa = civilization != null ? civilization.RegionJa : "",
                    EraJa = civilization != null ? civilization.EraJa : "",
                    LeaderId = leader != null ? leader.Id : NullIfEmpty(pd.leaderId),
                    LeaderNameJa = leader != null ? leader.NameJa : "",
                    LeaderTitleJa = leader != null ? leader.TitleJa : "",
                    Color = new Color(pd.colorR, pd.colorG, pd.colorB),
                    IsHuman = pd.isHuman,
                    IsEliminated = pd.isEliminated,
                    CurrentResearchId = NullIfEmpty(pd.currentResearchId),
                    ScienceStored = pd.scienceStored,
                    CurrentCulturePolicyId = NullIfEmpty(pd.currentCulturePolicyId),
                    CultureStored = pd.cultureStored,
                    TotalCulture = pd.totalCulture,
                    GreatPersonPoints = pd.greatPersonPoints,
                    TotalGreatPersonPoints = pd.totalGreatPersonPoints,
                    MasterpiecePoints = pd.masterpiecePoints,
                    TotalMasterpiecePoints = pd.totalMasterpiecePoints,
                    Treasury = pd.treasury,
                    Taxation = AdministrationSystem.PolicyFromSaveValue(pd.taxPolicy),
                    Stability = pd.stability,
                    WarWeariness = pd.warWeariness,
                    LastRevenue = pd.lastRevenue,
                    LastExpenses = pd.lastExpenses,
                    CapitalCityId = pd.capitalCityId,
                    SocialFocus = PopulationSystem.FocusFromSaveValue(pd.socialFocus),
                };
                // フィールド初期化子の StartingTech を消してセーブ内容で置き換える
                p.KnownTechs.Clear();
                if (pd.knownTechs != null)
                    for (int j = 0; j < pd.knownTechs.Count; j++) p.KnownTechs.Add(pd.knownTechs[j]);
                if (pd.knownCulturePolicies != null)
                    for (int j = 0; j < pd.knownCulturePolicies.Count; j++)
                        p.KnownCulturePolicies.Add(pd.knownCulturePolicies[j]);
                if (pd.discoveredHeritageSites != null)
                    for (int j = 0; j < pd.discoveredHeritageSites.Count; j++)
                        p.DiscoveredHeritageSites.Add(pd.discoveredHeritageSites[j]);
                if (pd.recruitedGreatPeople != null)
                    for (int j = 0; j < pd.recruitedGreatPeople.Count; j++)
                        p.RecruitedGreatPeople.Add(pd.recruitedGreatPeople[j]);
                if (pd.collectedMasterpieces != null)
                    for (int j = 0; j < pd.collectedMasterpieces.Count; j++)
                        p.CollectedMasterpieces.Add(pd.collectedMasterpieces[j]);
                if (pd.cultureInfluencePlayerIds != null && pd.cultureInfluenceValues != null)
                {
                    int m = Math.Min(pd.cultureInfluencePlayerIds.Count,
                        pd.cultureInfluenceValues.Count);
                    for (int j = 0; j < m; j++)
                        p.CulturalInfluence[pd.cultureInfluencePlayerIds[j]] =
                            pd.cultureInfluenceValues[j];
                }
                if (pd.atWarWith != null)
                    for (int j = 0; j < pd.atWarWith.Count; j++) p.AtWarWith.Add(pd.atWarWith[j]);
                // 開戦ターンの復元(バージョン4追加)。バージョン3以前のセーブはこのデータを
                // 持たない(JsonUtility が空リストで埋める)ため、交戦中の相手には現在ターンを
                // 開戦扱いとして補完する。和平タイマーはロード時点から再始動するが、
                // 旧セーブ互換の代償として許容する。
                if (pd.warStartEnemyIds != null && pd.warStartTurns != null)
                {
                    int m = Math.Min(pd.warStartEnemyIds.Count, pd.warStartTurns.Count);
                    for (int j = 0; j < m; j++)
                        p.WarStartTurns[pd.warStartEnemyIds[j]] = pd.warStartTurns[j];
                }
                foreach (int enemyId in p.AtWarWith)
                    if (!p.WarStartTurns.ContainsKey(enemyId))
                        p.WarStartTurns[enemyId] = d.turnNumber;
                if (pd.explored != null)
                    for (int j = 0; j < pd.explored.Count; j++) p.Explored.Add(pd.explored[j]);
                s.Players.Add(p);
            }

            // ---- 遺産配置 ----
            if (d.heritageSites != null)
            {
                for (int i = 0; i < d.heritageSites.Count; i++)
                {
                    var hd = d.heritageSites[i];
                    if (HeritageSiteCatalog.Find(hd.siteId) == null)
                        throw new InvalidOperationException($"未知の遺産Id: {hd.siteId}");
                    var tile = s.Map.Get(hd.coord);
                    if (tile == null || !tile.IsPassable)
                        throw new InvalidOperationException($"遺産{hd.siteId}の座標が不正: {hd.coord}");
                    s.HeritageSites.Add(new HeritageSiteInstance
                    {
                        SiteId = hd.siteId,
                        Coord = hd.coord,
                        DiscoveredByPlayerId = hd.discoveredByPlayerId,
                        DiscoveredTurn = hd.discoveredTurn,
                    });
                }
            }

            // ---- 都市(Tile.City の逆参照も張る) ----
            if (d.cities != null)
            {
                for (int i = 0; i < d.cities.Count; i++)
                {
                    var cd = d.cities[i];
                    var owner = s.GetPlayer(cd.playerId);
                    if (owner == null)
                        throw new InvalidOperationException($"都市{cd.id}の所有者{cd.playerId}が存在しない");
                    var city = new City
                    {
                        Id = cd.id,
                        PlayerId = cd.playerId,
                        NameJa = cd.nameJa,
                        Coord = cd.coord,
                        Population = cd.population,
                        FoodStored = cd.foodStored,
                        ProductionStored = cd.productionStored,
                        Farmers = cd.farmers,
                        Artisans = cd.artisans,
                        Scholars = cd.scholars,
                        Education = cd.education,
                        Satisfaction = cd.satisfaction,
                        FoodNeedFulfillment = cd.foodNeedFulfillment,
                        HousingNeedFulfillment = cd.housingNeedFulfillment,
                        ServiceNeedFulfillment = cd.serviceNeedFulfillment,
                        LastNetMigration = cd.lastNetMigration,
                        Hp = cd.hp,
                        MaxHp = cd.maxHp,
                        Buildings = cd.buildings != null ? new List<string>(cd.buildings) : new List<string>(),
                        CurrentProduction = RestoreProduction(cd.prodKind, cd.prodId),
                    };
                    if (d.version <= 11) PopulationSystem.InitializeCity(city);
                    else PopulationSystem.NormalizeLoadedCity(owner, city);
                    owner.Cities.Add(city);
                    var tile = s.Map.Get(city.Coord);
                    if (tile == null)
                        throw new InvalidOperationException($"都市{cd.id}の座標{cd.coord}がマップ外");
                    tile.City = city;
                }
            }

            // ---- ユニット(Tile.Unit の逆参照も張る) ----
            if (d.units != null)
            {
                for (int i = 0; i < d.units.Count; i++)
                {
                    var ud = d.units[i];
                    var owner = s.GetPlayer(ud.playerId);
                    if (owner == null)
                        throw new InvalidOperationException($"ユニット{ud.id}の所有者{ud.playerId}が存在しない");
                    GameRules.GetUnit(ud.defId);   // 未知のIdなら例外(早期検出)
                    var u = new Unit
                    {
                        Id = ud.id,
                        PlayerId = ud.playerId,
                        DefId = ud.defId,
                        Coord = ud.coord,
                        Hp = ud.hp,
                        MovesLeft = ud.movesLeft,
                        Fortified = ud.fortified,
                        ActedThisTurn = ud.actedThisTurn,
                        Supply = LogisticsSystem.LevelFromSaveValue(ud.supplyLevel),
                        TurnsOutOfSupply = Math.Max(0, ud.turnsOutOfSupply),
                        GotoPath = (ud.gotoPath != null && ud.gotoPath.Count > 0)
                            ? new List<HexCoord>(ud.gotoPath)
                            : null,
                    };
                    owner.Units.Add(u);
                    var tile = s.Map.Get(u.Coord);
                    if (tile == null)
                        throw new InvalidOperationException($"ユニット{ud.id}の座標{ud.coord}がマップ外");
                    tile.Unit = u;
                }
            }

            // ---- カウンタ・勝者・視界 ----
            s.RestoreIdCounters(d.nextUnitId, d.nextCityId);
            if (d.winnerPlayerId >= 0) s.Winner = s.GetPlayer(d.winnerPlayerId);

            // Explored はセーブから復元済み。Visible はここで再計算する
            // (Explored |= Visible はセーブ時点で成立済みのため冪等)。
            Visibility.RecomputeAll(s);

            // Version は新規に開始(描画側の全再同期を促す)
            s.Bump();
            return s;
        }

        // ================= 内部ヘルパー =================

        /// <summary>HexCoord の決定的な順序(r → q)。</summary>
        static int CompareCoord(HexCoord a, HexCoord b)
        {
            int cmp = a.r.CompareTo(b.r);
            return cmp != 0 ? cmp : a.q.CompareTo(b.q);
        }

        /// <summary>生産中項目を種別+Idから定義テーブル経由で復元する。未選択(-1)は null。</summary>
        static ProductionItem RestoreProduction(int kind, string id)
        {
            if (kind < 0 || string.IsNullOrEmpty(id)) return null;
            switch ((ProductionKind)kind)
            {
                case ProductionKind.Unit: return ProductionItem.FromUnit(GameRules.GetUnit(id));
                case ProductionKind.Building: return ProductionItem.FromBuilding(GameRules.GetBuilding(id));
                default: throw new InvalidOperationException($"不明な生産種別: {kind}");
            }
        }

        static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;
    }

    /// <summary>
    /// セーブ/ロード用の GameState 拡張(partial)。
    /// private な Id カウンタ(nextUnitId/nextCityId)の退避・復元と、
    /// 導出不能なセーブ付帯情報(最終保存時刻)の保持を担う。
    /// </summary>
    public partial class GameState
    {
        /// <summary>
        /// 最後にセーブした時刻(ISO 8601 形式、例 "2026-07-20T18:55:00")。未セーブなら null。
        /// シミュレーションからは導出できないため partial フィールドとして往復させる
        /// (Serialize(Deserialize(json)) == json の不変条件を保つ)。シミュレーションには影響しない。
        /// </summary>
        public string LastSavedAtIso;

        internal int PeekNextUnitId() => nextUnitId;
        internal int PeekNextCityId() => nextCityId;

        internal void RestoreIdCounters(int unitId, int cityId)
        {
            nextUnitId = unitId;
            nextCityId = cityId;
        }
    }
}
